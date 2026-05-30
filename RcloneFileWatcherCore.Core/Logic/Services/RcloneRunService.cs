using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Infrastructure.Logging.Interfaces;
using RcloneFileWatcherCore.Logic.Rclone;
using System;
using System.Diagnostics;
using System.Linq;

namespace RcloneFileWatcherCore.Logic.Services
{
    public class RcloneRunService : IBatchExecutionService
    {
        private readonly ILogger _logger;

        public RcloneRunService(ILogger logger)
        {
            _logger = logger;
        }
        public bool ExecuteBatch(string batchPath)
        {
            if (string.IsNullOrWhiteSpace(batchPath))
            {
                _logger.Log(Enums.LogLevel.Error, "Rclone batch file is empty or null.");
                return false;
            }

            using (var process = new Process())
            {
                process.StartInfo.FileName = batchPath;
                process.StartInfo.CreateNoWindow = false;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                _logger.Log(Enums.LogLevel.Information, $"Starting Rclone with batch file: {batchPath}");
                process.Start();
                process.WaitForExit();
                _logger.Log(Enums.LogLevel.Information, $"Rclone process exited with code: {process.ExitCode}");
                return process.ExitCode == 0;
            }
        }

        public bool ExecuteCommand(RcloneCommandDTO command, string includeFromPath)
        {
            if (command == null)
            {
                _logger.Log(Enums.LogLevel.Error, "Rclone command is null.");
                return false;
            }

            var exe = string.IsNullOrWhiteSpace(command.RclonePath) ? "rclone" : command.RclonePath.Trim();
            // Expand {datetime}/{year}/... once per run so --suffix/--backup-dir/--log-file get a
            // consistent timestamp across all arguments of this invocation.
            var now = DateTime.Now;
            var arguments = RclonePlaceholders.Expand(
                RcloneCommandBuilder.BuildArguments(command, includeFromPath), now);

            var startInfo = new ProcessStartInfo
            {
                FileName = RclonePlaceholders.Expand(exe, now),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            foreach (var arg in arguments)
                startInfo.ArgumentList.Add(arg);

            try
            {
                using var process = new Process { StartInfo = startInfo };
                process.OutputDataReceived += (s, e) => { if (e.Data != null) _logger.Log(Enums.LogLevel.Information, $"[rclone] {e.Data}"); };
                process.ErrorDataReceived += (s, e) => { if (e.Data != null) _logger.Log(Enums.LogLevel.Information, $"[rclone] {e.Data}"); };

                var commandLine = string.Join(" ", new[] { startInfo.FileName }
                    .Concat(arguments)
                    .Select(a => a.Contains(' ') ? $"\"{a}\"" : a));
                _logger.Log(Enums.LogLevel.Information, $"Starting rclone: {commandLine}");
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                _logger.Log(Enums.LogLevel.Information, $"Rclone process exited with code: {process.ExitCode}");
                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                _logger.Log(Enums.LogLevel.Error, $"Failed to run rclone ({exe})", ex);
                return false;
            }
        }
    }
}
