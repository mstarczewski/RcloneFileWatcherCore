using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Infrastructure.Logging.Interfaces;
using RcloneFileWatcherCore.Logic.Interfaces;
using System;

namespace RcloneFileWatcherCore.Logic.Services
{
    public class RcloneFullSyncService : IRcloneJobService
    {
        private readonly ILogger _logger;
        private readonly IBatchExecutionService _rcloneRunner;
        public RcloneFullSyncService(ILogger logger, IBatchExecutionService rcloneRunner)
        {
            _logger = logger;
            _rcloneRunner = rcloneRunner;
        }
        public bool Execute(ConfigDTO configDTO)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(configDTO.RunOneTimeFullStartupSyncBatch))
                {
                    _logger.Log(Enums.LogLevel.Information, "Running full sync.");
                    _rcloneRunner.ExecuteBatch(configDTO.RunOneTimeFullStartupSyncBatch);
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