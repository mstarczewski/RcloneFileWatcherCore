namespace RcloneFileWatcherCore.App
{
    public interface IRuntimeController
    {
        bool IsRunning { get; }
        void Start();
        void Stop();
        void SyncNow();
        void FullSyncNow();
    }
}
