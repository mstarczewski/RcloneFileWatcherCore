using System.Collections.Generic;

namespace RcloneFileWatcherCore.Status
{
    public interface IStatusService
    {
        AppStatus GetStatus();

        /// <summary>
        /// Returns the total queue size and a bounded sample (up to <paramref name="max"/> entries,
        /// sorted by path) for a human-readable preview of pending changes.
        /// </summary>
        QueueSnapshot GetQueuedSample(int max);

        void MarkWatcherStarted(IReadOnlyList<string> watchedPaths);
        void MarkWatcherStopped();
    }
}
