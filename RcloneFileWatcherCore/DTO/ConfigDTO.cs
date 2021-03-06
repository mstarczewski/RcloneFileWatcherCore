using System;
using System.Collections.Generic;
using System.Text;

namespace RcloneFileWatcherCore.DTO
{
    class ConfigDTO
    {
        public bool ConsoleWriter { get; set; }
        public List<PathDTO> Path { get; set; }
    }
}