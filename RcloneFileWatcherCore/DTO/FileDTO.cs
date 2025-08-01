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
    }
}
