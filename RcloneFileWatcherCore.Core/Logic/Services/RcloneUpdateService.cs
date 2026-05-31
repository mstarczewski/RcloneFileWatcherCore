using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Enums;
using RcloneFileWatcherCore.Infrastructure.Logging.Interfaces;
using RcloneFileWatcherCore.Logic.Interfaces;
using System;
using System.Diagnostics;
using System.Text;

namespace RcloneFileWatcherCore.Logic.Services
{
    public class RcloneUpdateService : IRcloneJobService
    {
        private readonly ILogger _logger;
        private const string RCLONE_SELFUPDATE_ARGUMENT = "selfupdate";
        private const string RCLONE_SUCCESS_UPDATE = "Successfully updated";

        public RcloneUpdateService(ILogger logger)
        {
            _logger = logger;
        }

        public bool Execute(ConfigDTO configDTO)
        {
            try
            {
                bool updated = ExecuteProc(configDTO.UpdateRclone.RclonePath, RCLONE_SELFUPDATE_ARGUMENT, SelfUpdateExecutionCheck);
                if (updated)
                {
                    _logger.Log(LogLevel.Information, "Rclone updated");
                }
                return updated;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error,"Error during update", ex);
                return false;
            }
        }

        private bool ExecuteProc(string pathToRclone, string argument, Func<string, bool> resultChecker)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = pathToRclone,
                    Arguments = argument,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            try
            {
                var output = new StringBuilder();
                var error = new StringBuilder();
                process.OutputDataReceived += (s, e) => { if (e.Data != null) output.AppendLine(e.Data); };
                process.ErrorDataReceived += (s, e) => { if (e.Data != null) error.AppendLine(e.Data); };
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                // rclone writes its normal NOTICE/INFO output (including "Successfully updated")
                // to stderr, so stderr is NOT an error signal — surface it as info and judge the
                // outcome by the exit code plus the success marker found in either stream.
                if (error.Length > 0)
                    _logger.Log(LogLevel.Information, $"rclone selfupdate: {error.ToString().Trim()}");

                if (process.ExitCode != 0)
                {
                    _logger.Log(LogLevel.Warning, $"rclone selfupdate exited with code {process.ExitCode}");
                    return false;
                }

                return resultChecker(output + error.ToString());
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "Error during update", ex);
                return false;
            }
        }

        private bool SelfUpdateExecutionCheck(string combinedOutput)
        {
            return combinedOutput.Contains(RCLONE_SUCCESS_UPDATE, StringComparison.OrdinalIgnoreCase);
        }
    }
}