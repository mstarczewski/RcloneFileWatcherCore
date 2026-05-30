using System.Collections.Generic;

namespace RcloneFileWatcherCore.Status
{
    public interface IStatusService
    {
        AppStatus GetStatus();
        void MarkWatcherStarted(IReadOnlyList<string> watchedPaths);
        void MarkWatcherStopped();
    }
}
