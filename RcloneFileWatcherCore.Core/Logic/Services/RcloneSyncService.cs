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
                // Optional quiet-period debounce: while changes keep arriving (e.g. a long copy),
                // wait until they settle (no new change for QuietPeriodSeconds) before syncing,
                // capped by QuietPeriodMaxWaitSeconds so continuous activity can't starve the sync.
                if (configDTO.QuietPeriodSeconds > 0)
                {
                    long newest = long.MinValue, oldest = long.MaxValue;
                    var count = 0;
                    foreach (var dto in _fileDTOs.Values)
                    {
                        count++;
                        if (dto.EnqueuedUtcTicks > newest) newest = dto.EnqueuedUtcTicks;
                        if (dto.EnqueuedUtcTicks < oldest) oldest = dto.EnqueuedUtcTicks;
                    }
                    if (count > 0)
                    {
                        var nowTicks = DateTime.UtcNow.Ticks;
                        var quietS = (nowTicks - newest) / (double)TimeSpan.TicksPerSecond;
                        var waitedS = (nowTicks - oldest) / (double)TimeSpan.TicksPerSecond;
                        var maxWait = configDTO.QuietPeriodMaxWaitSeconds > 0 ? configDTO.QuietPeriodMaxWaitSeconds : int.MaxValue;
                        if (quietS < configDTO.QuietPeriodSeconds && waitedS < maxWait)
                        {
                            var capText = configDTO.QuietPeriodMaxWaitSeconds > 0 ? $"{configDTO.QuietPeriodMaxWaitSeconds}s" : "none";
                            _logger.Log(Enums.LogLevel.Information,
                                $"Deferring sync (quiet period): {count} change(s) queued; newest change {quietS:F0}s ago (need {configDTO.QuietPeriodSeconds}s of quiet); oldest has waited {waitedS:F0}s (max-wait cap {capText}).");
                            return true;
                        }
                    }
                }

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

                    // Optionally collapse whole-directory changes into a single "dir/**" rule.
                    if (configDTO.CollapseDirectoryChanges)
                        files = FilePrepareService.CollapseDirectoryRules(files);

                    var path = configDTO.Path?.FirstOrDefault(p => p.WatchingPath == sourcePath);
                    if (path == null || !path.Enabled)
                        continue;

                    // Visibility: how many entries this cycle hands to rclone (after collapsing).
                    _logger.Log(Enums.LogLevel.Information, $"Sync {sourcePath}: passing {files.Count} item(s) to rclone");

                    if (path.SyncMode == Enums.SyncMode.Managed)
                    {
                        var cmd = path.RcloneCommand;
                        if (cmd == null)
                        {
                            _logger.Log(Enums.LogLevel.Error, $"Managed sync mode set but no rclone command configured for {sourcePath}");
                        }
                        else if (cmd.IncludeFrom && cmd.IncludeFromStdin)
                        {
                            // Pipe the list to rclone via stdin (--include-from -) - no file on disk.
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