using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Globals;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RcloneFileWatcherCore.Status
{
    public class StatusService : IStatusService
    {
        private readonly ConcurrentDictionary<string, FileDTO> _fileDTOs;
        private readonly object _lock = new object();
        private DateTime? _startedAtUtc;
        private bool _watcherRunning;
        private IReadOnlyList<string> _watchedPaths = Array.Empty<string>();

        public StatusService(ConcurrentDictionary<string, FileDTO> fileDTOs)
        {
            _fileDTOs = fileDTOs;
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
                    PendingChanges = _fileDTOs.Count
                };
            }
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
