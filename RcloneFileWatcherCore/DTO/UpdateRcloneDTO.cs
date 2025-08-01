using System.Collections.Generic;

namespace RcloneFileWatcherCore.DTO
{
    public class UpdateRcloneDTO
    {
        public bool Update { get; set; }
        public string RclonePath { get; set; }
        public int CheckUpdateHours { get; set; }
    }
}
