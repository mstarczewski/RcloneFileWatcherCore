using RcloneFileWatcherCore.DTO;

namespace RcloneFileWatcherCore.Logic.Interfaces
{
    interface IProcess
    {
        bool Start(ConfigDTO configDTO);
    }
}
