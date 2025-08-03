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
                    _logger.Write("Running full sync.");
                    _rcloneRunner.RunBatch(configDTO.RunOneTimeFullStartupSyncBatch);
                    return true;
                }
                else
                {
                    _logger.Write("Skipping full sync.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Write(ex.ToString());
                return false;
            }
        }
    }
}