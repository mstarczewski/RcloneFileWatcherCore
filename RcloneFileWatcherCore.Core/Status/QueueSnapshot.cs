using System;
using System.Collections.Generic;

namespace RcloneFileWatcherCore.Status
{
    /// <summary>
    /// A point-in-time view of the pending-changes queue: the total size plus a bounded,
    /// readable sample of entries for display in the GUI.
    /// </summary>
    public class QueueSnapshot
    {
        public int Total { get; set; }
        public IReadOnlyList<QueuedChange> Sample { get; set; } = Array.Empty<QueuedChange>();
    }

    public class QueuedChange
    {
        /// <summary>Path as it will be passed to rclone (relative, dirs end with /**).</summary>
        public string Path { get; set; }

        /// <summary>Watched root the change belongs to.</summary>
        public string SourcePath { get; set; }

        /// <summary>Change kind (Created/Changed/Deleted/Renamed).</summary>
        public string Change { get; set; }
    }
}
