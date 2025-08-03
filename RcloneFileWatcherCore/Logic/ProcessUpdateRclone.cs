using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Enums;
using RcloneFileWatcherCore.Logic.Interfaces;
using System;
using System.Diagnostics;

namespace RcloneFileWatcherCore.Logic
{
    public class ProcessUpdateRclone : IProcess
    {
        private readonly ILogger _logger;
        private const string RCLONE_SELFUPDATE_ARGUMENT = "selfupdate";
        private const string RCLONE_SUCCESS_UPDATE = "Successfully updated";

        public ProcessUpdateRclone(ILogger logger)
        {
            _logger = logger;
        }

        public bool Start(ConfigDTO configDTO)
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
                string output = string.Empty;
                string error = string.Empty;
                process.OutputDataReceived += (s, e) => { if (e.Data != null) output += e.Data; };
                process.ErrorDataReceived += (s, e) => { if (e.Data != null) error += e.Data; };
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                process.OutputDataReceived -= null;
                process.ErrorDataReceived -= null;
                if (!string.IsNullOrWhiteSpace(error))
                {
                    _logger.Log(LogLevel.Information, $"Update proc DataReceived {error}");
                    return false;
                }
                return resultChecker(output);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "Error during update", ex);
                return false;
            }
        }

        private bool SelfUpdateExecutionCheck(string output)
        {
            return output.Contains(RCLONE_SUCCESS_UPDATE, StringComparison.OrdinalIgnoreCase);
        }
    }
}