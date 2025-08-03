using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Logic.Interfaces;
using System;

namespace RcloneFileWatcherCore.Logic
{
    public class FullProcessSyncRclone : IProcess
    {
        private readonly ILogger _logger;
        private readonly IRcloneRunner _rcloneRunner;
        public FullProcessSyncRclone(ILogger logger, IRcloneRunner rcloneRunner)
        {
            _logger = logger;
            _rcloneRunner = rcloneRunner;
        }
        public bool Start(ConfigDTO configDTO)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(configDTO.RunOneTimeFullStartupSyncBatch))
                {
                    _logger.Log(Enums.LogLevel.Information, "Running full sync.");
                    _rcloneRunner.RunBatch(configDTO.RunOneTimeFullStartupSyncBatch);
                    return true;
                }
                else
                {
                    _logger.Log(Enums.LogLevel.Information, "Skipping full sync.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Log(Enums.LogLevel.Error, "Exception during full sync start", ex);
                return false;
            }
        }
    }
}