using RcloneFileWatcherCore.DTO;

namespace RcloneFileWatcherCore.Logic.Interfaces
{
    public interface IRcloneJobService
    {
        bool Execute(ConfigDTO configDTO);
    }
}
