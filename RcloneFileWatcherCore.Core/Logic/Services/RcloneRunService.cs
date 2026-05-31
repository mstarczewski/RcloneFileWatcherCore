using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Enums;
using RcloneFileWatcherCore.Infrastructure.Logging.Interfaces;
using RcloneFileWatcherCore.Logic.Rclone;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RcloneFileWatcherCore.Logic.Services
{
    public class RcloneRunService : IBatchExecutionService
    {
        private readonly ILogger _logger;
        // Tracks rclone/script processes currently running so the GUI can abort them.
        private readonly ConcurrentDictionary<int, Process> _running = new ConcurrentDictionary<int, Process>();

        public RcloneRunService(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>True while at least one rclone/script process is running.</summary>
        public bool AnyRunning => !_running.IsEmpty;

        /// <summary>Hard-stops every running rclone/script process (and its children).</summary>
        public void CancelRunning()
        {
            foreach (var process in _running.Values)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        _logger.Log(LogLevel.Information, $"Stopping rclone process (PID {process.Id})");
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, "Error stopping rclone process", ex);
                }
            }
        }

        public bool ExecuteBatch(string batchPath)
        {
            if (string.IsNullOrWhiteSpace(batchPath))
            {
                _logger.Log(LogLevel.Error, "Rclone batch file is empty or null.");
                return false;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = batchPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = startInfo })
            {
                // Capture the script's output (the rclone it runs) so it shows in the GUI log too,
                // matching managed mode — previously script-mode output was lost.
                process.OutputDataReceived += (s, e) => { if (e.Data != null) LogRcloneLine(e.Data); };
                process.ErrorDataReceived += (s, e) => { if (e.Data != null) LogRcloneLine(e.Data); };

                _logger.Log(LogLevel.Information, $"Starting Rclone with batch file: {batchPath}");
                process.Start();
                _running[process.Id] = process;
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                try
                {
                    process.WaitForExit();
                    _logger.Log(LogLevel.Information, $"Rclone process exited with code: {process.ExitCode}");
                    return process.ExitCode == 0;
                }
                finally
                {
                    _running.TryRemove(process.Id, out _);
                }
            }
        }

        public bool ExecuteCommand(RcloneCommandDTO command, string includeFromPath, IReadOnlyList<string> includeFromStdin = null)
        {
            if (command == null)
            {
                _logger.Log(LogLevel.Error, "Rclone command is null.");
                return false;
            }

            var exe = string.IsNullOrWhiteSpace(command.RclonePath) ? "rclone" : command.RclonePath.Trim();
            // Expand {datetime}/{year}/... once per run so --suffix/--backup-dir/--log-file get a
            // consistent timestamp across all arguments of this invocation.
            var now = DateTime.Now;
            var arguments = RclonePlaceholders.Expand(
                RcloneCommandBuilder.BuildArguments(command, includeFromPath), now);

            // rclone sends its output to --log-file when set (so nothing reaches stdout/stderr);
            // otherwise it logs to stderr. To surface rclone's messages in our log/GUI either way,
            // we capture stdout/stderr AND, when a --log-file is configured, tail that file live.
            var logFilePath = ExtractFlagValue(arguments, "--log-file");

            var startInfo = new ProcessStartInfo
            {
                FileName = RclonePlaceholders.Expand(exe, now),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = includeFromStdin != null,
                CreateNoWindow = true
            };
            foreach (var arg in arguments)
                startInfo.ArgumentList.Add(arg);

            int pid = -1;
            try
            {
                using var process = new Process { StartInfo = startInfo };
                process.OutputDataReceived += (s, e) => { if (e.Data != null) LogRcloneLine(e.Data); };
                process.ErrorDataReceived += (s, e) => { if (e.Data != null) LogRcloneLine(e.Data); };

                var commandLine = string.Join(" ", new[] { startInfo.FileName }
                    .Concat(arguments)
                    .Select(a => a.Contains(' ') ? $"\"{a}\"" : a));
                _logger.Log(LogLevel.Information, $"Starting rclone: {commandLine}");
                process.Start();
                pid = process.Id;
                _running[pid] = process;
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Pipe the changed-files list to rclone's stdin (--include-from -) on a separate
                // task: a very large list can fill the stdin pipe buffer, so writing it off the
                // calling thread (while stdout/stderr drain asynchronously) avoids any back-pressure
                // stall. Closing the stream signals EOF so rclone proceeds.
                Task stdinTask = null;
                if (includeFromStdin != null)
                {
                    stdinTask = Task.Run(() =>
                    {
                        using var stdin = process.StandardInput;
                        foreach (var line in includeFromStdin)
                            stdin.WriteLine(line);
                    });
                }

                Task tailTask = null;
                CancellationTokenSource tailCts = null;
                if (!string.IsNullOrWhiteSpace(logFilePath) && logFilePath != "-")
                {
                    tailCts = new CancellationTokenSource();
                    var token = tailCts.Token;
                    tailTask = Task.Run(() => TailLogFile(logFilePath, token));
                }

                process.WaitForExit();
                try { stdinTask?.Wait(2000); } catch { /* best effort */ }

                if (tailCts != null)
                {
                    // Let the tail drain the last lines rclone flushed, then stop it.
                    Thread.Sleep(300);
                    tailCts.Cancel();
                    try { tailTask?.Wait(2000); } catch { /* best effort */ }
                    tailCts.Dispose();
                }

                _logger.Log(LogLevel.Information, $"Rclone process exited with code: {process.ExitCode}");
                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Failed to run rclone ({exe})", ex);
                return false;
            }
            finally
            {
                if (pid >= 0)
                    _running.TryRemove(pid, out _);
            }
        }

        private void TailLogFile(string path, CancellationToken ct)
        {
            try
            {
                // The file name carries {datetime}, so it's unique per run — wait for rclone to
                // create it, then read it from the start until the process exits.
                var waited = 0;
                while (!File.Exists(path) && !ct.IsCancellationRequested && waited < 5000)
                {
                    Thread.Sleep(200);
                    waited += 200;
                }
                if (!File.Exists(path))
                    return;

                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);
                using var reader = new StreamReader(stream);

                while (true)
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                        LogRcloneLine(line);

                    if (ct.IsCancellationRequested)
                    {
                        while ((line = reader.ReadLine()) != null)
                            LogRcloneLine(line);
                        break;
                    }
                    Thread.Sleep(300);
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Error reading rclone log file {path}", ex);
            }
        }

        private void LogRcloneLine(string line)
        {
            _logger.Log(MapRcloneLevel(line), $"[rclone] {line}");
        }

        /// <summary>Maps rclone's own log severity (ERROR/NOTICE/INFO/DEBUG) to our LogLevel so
        /// the GUI level filter works and errors stand out.</summary>
        private static LogLevel MapRcloneLevel(string line)
        {
            if (line.Contains(" ERROR ") || line.Contains(" CRITICAL ") || line.StartsWith("ERROR", StringComparison.Ordinal))
                return LogLevel.Error;
            if (line.Contains(" WARNING "))
                return LogLevel.Warning;
            if (line.Contains(" DEBUG "))
                return LogLevel.Debug;
            // NOTICE / INFO and lines without a level (e.g. transfer stats) -> Information.
            return LogLevel.Information;
        }

        private static string ExtractFlagValue(IReadOnlyList<string> args, string flag)
        {
            for (var i = 0; i < args.Count - 1; i++)
            {
                if (string.Equals(args[i], flag, StringComparison.Ordinal))
                    return args[i + 1];
            }
            return null;
        }
    }
}
