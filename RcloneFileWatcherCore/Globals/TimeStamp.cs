using System;
using System.Threading;

namespace RcloneFileWatcherCore.Globals
{
    public static class TimeStamp
    {
        private static long _ticks = DateTime.Now.Ticks;
        static ReaderWriterLockSlim readWriteLock = new ReaderWriterLockSlim();
        public const int timeoutLock = 1000 * 10;
        public static void SetTimestampTicks()
        {
            if (readWriteLock.TryEnterWriteLock(timeoutLock))
            {
                try
                {
                    _ticks = DateTime.Now.Ticks;
                }
                finally
                {
                    readWriteLock.ExitWriteLock();
                }
            }
            else
            {
                _ticks = -1;
            }
        }
        public static long GetTimestampTicks()
        {
            if (readWriteLock.TryEnterReadLock(timeoutLock))
            {
                try
                {
                    return _ticks;
                }
                finally
                {
                    readWriteLock.ExitReadLock();
                }
            }
            else
            {
                return -1;
            }
        }
    }
}