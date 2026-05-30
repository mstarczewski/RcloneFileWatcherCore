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

                    var files = _filePrepare.PrepareFilesToSync(sourcePath, lastTimeStamp);
                    if (files == null || files.Count == 0)
                        continue;

                    var path = configDTO.Path?.FirstOrDefault(p => p.WatchingPath == sourcePath);
                    if (path == null)
                        continue;

                    if (path.SyncMode == Enums.SyncMode.Managed)
                    {
                        var cmd = path.RcloneCommand;
                        if (cmd == null)
                        {
                            _logger.Log(Enums.LogLevel.Error, $"Managed sync mode set but no rclone command configured for {sourcePath}");
                        }
                        else if (cmd.IncludeFrom && cmd.IncludeFromStdin)
                        {
                            // Pipe the list to rclone via stdin (--include-from -) — no file on disk.
                            _rcloneRunner.ExecuteCommand(cmd, "-", files);
                        }
                        else
                        {
                            // Managed but reading the filter from a file (or no filter at all).
                            if (cmd.IncludeFrom)
                                _filePrepare.WriteIncludeFromFile(path.RcloneFilesFromPath, files);
                            _rcloneRunner.ExecuteCommand(cmd, path.RcloneFilesFromPath);
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(path.RcloneBatch))
                    {
                        // Script mode: the .bat/.sh references the --include-from file, so write it.
                        _filePrepare.WriteIncludeFromFile(path.RcloneFilesFromPath, files);
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