using System;
using System.Collections.Generic;

namespace RcloneFileWatcherCore.Status
{
    public class AppStatus
    {
        public string Version { get; set; }
        public DateTime? StartedAtUtc { get; set; }
        public bool WatcherRunning { get; set; }
        public IReadOnlyList<string> WatchedPaths { get; set; } = Array.Empty<string>();
        public int PendingChanges { get; set; }
    }
}
