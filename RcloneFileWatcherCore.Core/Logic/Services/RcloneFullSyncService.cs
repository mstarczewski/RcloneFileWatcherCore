using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Enums;
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
                if (configDTO.FullSyncMode == SyncMode.Managed)
                {
                    var commands = configDTO.FullSyncCommands;
                    if (commands == null || commands.Count == 0)
                    {
                        _logger.Log(LogLevel.Information, "Skipping full sync (no managed commands configured).");
                        return false;
                    }

                    _logger.Log(LogLevel.Information, $"Running full sync ({commands.Count} managed command(s)).");
                    foreach (var command in commands)
                    {
                        // Full sync = whole-tree reconcile: no --include-from filter.
                        _rcloneRunner.ExecuteCommand(command, includeFromPath: null);
                    }
                    return true;
                }

                if (!string.IsNullOrWhiteSpace(configDTO.RunOneTimeFullStartupSyncBatch))
                {
                    _logger.Log(LogLevel.Information, "Running full sync.");
                    _rcloneRunner.ExecuteBatch(configDTO.RunOneTimeFullStartupSyncBatch);
                    return true;
                }

                _logger.Log(LogLevel.Information, "Skipping full sync.");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "Exception during full sync start", ex);
                return false;
            }
        }
    }
}