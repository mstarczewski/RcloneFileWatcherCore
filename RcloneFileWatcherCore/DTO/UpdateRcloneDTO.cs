using System.Collections.Generic;

namespace RcloneFileWatcherCore.DTO
{
    class UpdateRcloneDTO
    {
        public bool Update { get; set; }
        public string RclonePath { get; set; }
        public string RcloneWebsiteCurrentVersionAddress { get; set; }
        public int ChceckUpdateHours { get; set; }
    }
}
