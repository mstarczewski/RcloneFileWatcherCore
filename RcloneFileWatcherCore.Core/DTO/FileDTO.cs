using System.IO;

namespace RcloneFileWatcherCore.DTO
{
    public class FileDTO
    {
        public string PathPreparedToSync { get; set; }
        public string SourcePath { get; set; }
        public string FullPath { get; set; }
        public NotifyFilters NotifyFilters { get; set; }
        public WatcherChangeTypes WatcherChangeTypes { get; set; }
        public long TimeStampTicks { get; set; }

        /// <summary>Wall-clock time (UTC ticks) the change was enqueued — used by the optional
        /// quiet-period debounce to tell how long ago changes arrived.</summary>
        public long EnqueuedUtcTicks { get; set; }
    }
}
