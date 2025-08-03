using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Logic.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;

namespace RcloneFileWatcherCore.Logic
{
    public class ProcessSyncRclone : IProcess
    {
        private readonly ILogger _logger;
        private readonly FilePrepare _filePrepare;
        private readonly ConcurrentDictionary<string, FileDTO> _fileDTOs;
        private readonly IRcloneRunner _rcloneRunner;

        public ProcessSyncRclone(ILogger logger, FilePrepare filePrepare, ConcurrentDictionary<string, FileDTO> fileDTOs, IRcloneRunner rcloneRunner)
        {
            _logger = logger;
            _filePrepare = filePrepare;
            _fileDTOs = fileDTOs;
            _rcloneRunner = rcloneRunner;
        }
        public bool Start(ConfigDTO configDTO)
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
                        _rcloneRunner.RunBatch(rcloneBatch);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.Write(ex.ToString());
                return false;
            }
        }
    }
}