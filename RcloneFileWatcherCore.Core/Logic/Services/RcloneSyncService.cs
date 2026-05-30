using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Infrastructure.Logging.Interfaces;
using RcloneFileWatcherCore.Logic.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;

namespace RcloneFileWatcherCore.Logic.Services
{
    public class RcloneSyncService : IRcloneJobService
    {
        private readonly ILogger _logger;
        private readonly FilePrepareService _filePrepare;
        private readonly ConcurrentDictionary<string, FileDTO> _fileDTOs;
        private readonly IBatchExecutionService _rcloneRunner;

        public RcloneSyncService(ILogger logger, FilePrepareService filePrepare, ConcurrentDictionary<string, FileDTO> fileDTOs, IBatchExecutionService rcloneRunner)
        {
            _logger = logger;
            _filePrepare = filePrepare;
            _fileDTOs = fileDTOs;
            _rcloneRunner = rcloneRunner;
        }
        public bool Execute(ConfigDTO configDTO)
        {
            try
            {
                long lastTimeStamp = Globals.TimeStamp.GetTimestampTicks();
                Globals.TimeStamp.SetTimestampTicks();
                var sourcePathList = _fileDTOs
                    .Where(x => x.Value.TimeStampTicks <= lastTimeStamp)
                    .Select(x => x.Value.SourcePath)
                    .Distinct()
                    .ToList();

                foreach (var sourcePath in sourcePathList)
                {
                    if (string.IsNullOrWhiteSpace(sourcePath))
                        continue;

                    bool hadChanges = _filePrepare.PrepareFilesToSync(sourcePath, lastTimeStamp);
                    if (!hadChanges)
                        continue;

                    var path = configDTO.Path?.FirstOrDefault(p => p.WatchingPath == sourcePath);
                    if (path == null)
                        continue;

                    if (path.SyncMode == Enums.SyncMode.Managed)
                    {
                        if (path.RcloneCommand != null)
                            _rcloneRunner.ExecuteCommand(path.RcloneCommand, path.RcloneFilesFromPath);
                        else
                            _logger.Log(Enums.LogLevel.Error, $"Managed sync mode set but no rclone command configured for {sourcePath}");
                    }
                    else if (!string.IsNullOrWhiteSpace(path.RcloneBatch))
                    {
                        _rcloneRunner.ExecuteBatch(path.RcloneBatch);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.Log(Enums.LogLevel.Error, "Exception during sync process", ex); 
                return false;
            }
        }
    }
}