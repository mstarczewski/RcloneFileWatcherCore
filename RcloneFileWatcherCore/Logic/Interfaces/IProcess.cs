using RcloneFileWatcherCore.DTO;
using System.Diagnostics;

namespace RcloneFileWatcherCore.Logic.Interfaces
{
    interface IProcess
    {
        bool Start(ConfigDTO configDTO);
    }
}
