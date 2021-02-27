using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RcloneFileWatcherCore.DTO
{
    class FileDTO
    {
        public string PathPreparedToSync { get; set; }
        public string SourcePath { get; set; }
        public string FullPath { get; set; }
        public NotifyFilters NotifyFilters { get; set; }
        public WatcherChangeTypes WatcherChangeTypes { get; set; }
    }
}
