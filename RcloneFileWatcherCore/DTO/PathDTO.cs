using System;
using System.Collections.Generic;
using System.Text;

namespace RcloneFileWatcherCore.DTO
{
    class PathDTO
    {
        public string WatchingPath { get; set; }
        public string RcloneFilesFromPath { get; set; }
        public string RcloneBatch { get; set; }
        public List<string> ExcludeContains { get; set; }
    }
}
