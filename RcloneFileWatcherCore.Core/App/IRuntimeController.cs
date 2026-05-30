using System.Threading.Tasks;

namespace RcloneFileWatcherCore.App
{
    public interface IRuntimeController
    {
        bool IsRunning { get; }
        void Start();
        void Stop();
        Task<bool> SyncNowAsync();
        Task<bool> FullSyncNowAsync();
    }
}
