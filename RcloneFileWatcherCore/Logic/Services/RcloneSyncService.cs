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
                    string rcloneBatch = _filePrepare.PrepareFilesToSync(sourcePath, lastTimeStamp);
                    if (!string.IsNullOrWhiteSpace(sourcePath) && !string.IsNullOrWhiteSpace(rcloneBatch))
                    {
                        _rcloneRunner.ExecuteBatch(rcloneBatch);
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