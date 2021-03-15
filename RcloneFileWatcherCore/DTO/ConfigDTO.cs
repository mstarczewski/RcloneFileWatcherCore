using System.Collections.Generic;

namespace RcloneFileWatcherCore.DTO
{
    class ConfigDTO
    {
        public bool ConsoleWriter { get; set; }
        public List<PathDTO> Path { get; set; }
        public UpdateRcloneDTO UpdateRclone { get; set; }
    }
}