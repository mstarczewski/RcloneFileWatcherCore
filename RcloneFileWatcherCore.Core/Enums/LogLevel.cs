using System;

namespace RcloneFileWatcherCore.Enums
{
    [Flags]
    public enum LogLevel
    {
        None = 0,
        Trace = 1,
        Debug = 2,
        Information = 4,
        Warning = 8,
        Error = 16,
        Critical = 32,
        Always = 64,
        All = Trace | Debug | Information | Warning | Error | Critical | Always
    }
}
