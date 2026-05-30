using RcloneFileWatcherCore.Enums;
using System.Collections.Generic;

namespace RcloneFileWatcherCore.DTO
{
    public class PathDTO
    {
        public string WatchingPath { get; set; }
        public string RcloneFilesFromPath { get; set; }
        public string RcloneBatch { get; set; }
        public List<string> ExcludeContains { get; set; }

        /// <summary>Whether this path runs an external script or a managed rclone command.</summary>
        public SyncMode SyncMode { get; set; } = SyncMode.Script;

        /// <summary>Managed rclone command used when <see cref="SyncMode"/> is Managed.</summary>
        public RcloneCommandDTO RcloneCommand { get; set; }

        public PathDTO()
        {
            ExcludeContains = new List<string>();
        }
    }
}
