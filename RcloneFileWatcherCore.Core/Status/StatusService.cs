using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Globals;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace RcloneFileWatcherCore.Status
{
    public class StatusService : IStatusService
    {
        private readonly ConcurrentDictionary<string, FileDTO> _fileDTOs;
        private readonly IBatchExecutionService _rcloneRunner;
        private readonly object _lock = new object();
        private DateTime? _startedAtUtc;
        private bool _watcherRunning;
        private IReadOnlyList<string> _watchedPaths = Array.Empty<string>();

        public StatusService(ConcurrentDictionary<string, FileDTO> fileDTOs, IBatchExecutionService rcloneRunner)
        {
            _fileDTOs = fileDTOs;
            _rcloneRunner = rcloneRunner;
        }

        public AppStatus GetStatus()
        {
            lock (_lock)
            {
                return new AppStatus
                {
                    Version = AppVersion.GetVersion(),
                    StartedAtUtc = _startedAtUtc,
                    WatcherRunning = _watcherRunning,
                    WatchedPaths = _watchedPaths,
                    PendingChanges = _fileDTOs.Count,
                    RcloneRunning = _rcloneRunner.AnyRunning
                };
            }
        }

        public QueueSnapshot GetQueuedSample(int max)
        {
            if (max < 0)
                max = 0;

            // Take a bounded slice without materializing the whole (possibly huge) queue, then
            // sort just the sample so the preview is stable and readable. Total uses the dictionary's
            // own count; it may differ slightly from the sample under concurrent writes — fine for a
            // live preview.
            var total = _fileDTOs.Count;
            var sample = _fileDTOs.Values
                .Take(max)
                .Select(dto => new QueuedChange
                {
                    Path = dto.PathPreparedToSync,
                    SourcePath = dto.SourcePath,
                    Change = dto.WatcherChangeTypes.ToString()
                })
                .OrderBy(c => c.Path, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new QueueSnapshot { Total = total, Sample = sample };
        }

        public void MarkWatcherStarted(IReadOnlyList<string> watchedPaths)
        {
            lock (_lock)
            {
                _watcherRunning = true;
                _watchedPaths = watchedPaths ?? Array.Empty<string>();
                _startedAtUtc ??= DateTime.UtcNow;
            }
        }

        public void MarkWatcherStopped()
        {
            lock (_lock)
            {
                _watcherRunning = false;
            }
        }
    }
}
