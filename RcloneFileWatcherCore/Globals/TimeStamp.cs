using System;
using System.Threading;

namespace RcloneFileWatcherCore.Globals
{
    public static class TimeStamp
    {
        private static long _ticks = DateTime.Now.Ticks;

        public static void SetTimestampTicks()
        {
            Interlocked.Exchange(ref _ticks, DateTime.Now.Ticks);
        }

        public static long GetTimestampTicks()
        {
            return Interlocked.Read(ref _ticks);
        }
    }
}