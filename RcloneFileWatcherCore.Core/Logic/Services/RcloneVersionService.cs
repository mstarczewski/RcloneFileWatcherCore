using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Enums;
using RcloneFileWatcherCore.Infrastructure.Logging.Interfaces;
using RcloneFileWatcherCore.Logic.Interfaces;
using System;
using System.Diagnostics;
using System.Linq;

namespace RcloneFileWatcherCore.Logic.Services
{
    /// <summary>
    /// Runs "<rclone> version" to report whether an rclone executable exists at a given path and
    /// which version it is. Used by the dashboard, which may show several distinct paths (each
    /// managed command and the auto-update setting can point at a different rclone binary).
    /// </summary>
    public class RcloneVersionService : IRcloneVersionService
    {
        private const int TimeoutMs = 5000;
        private readonly ILogger _logger;

        public RcloneVersionService(ILogger logger)
        {
            _logger = logger;
        }

        public RcloneVersionInfo Probe(string rclonePath)
        {
            var exe = string.IsNullOrWhiteSpace(rclonePath) ? "rclone" : rclonePath.Trim();
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = exe,
                        Arguments = "version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                // "version" output is tiny, so reading to end before waiting can't deadlock.
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                if (!process.WaitForExit(TimeoutMs))
                {
                    try { process.Kill(entireProcessTree: true); } catch { }
                    return new RcloneVersionInfo { Path = exe, Available = false, Error = "timed out" };
                }

                var firstLine = (output ?? string.Empty)
                    .Split('\n')
                    .Select(l => l.Trim())
                    .FirstOrDefault(l => l.Length > 0);

                if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(firstLine))
                    return new RcloneVersionInfo { Path = exe, Available = true, Version = firstLine };

                var reason = !string.IsNullOrWhiteSpace(error) ? error.Trim() : $"exit code {process.ExitCode}";
                return new RcloneVersionInfo { Path = exe, Available = false, Error = reason };
            }
            catch (Exception ex)
            {
                // Most commonly the file doesn't exist at that path (Win32Exception) - report as unavailable.
                _logger.Log(LogLevel.Debug, $"rclone version probe failed for '{exe}': {ex.Message}");
                return new RcloneVersionInfo { Path = exe, Available = false, Error = ex.Message };
            }
        }
    }
}
