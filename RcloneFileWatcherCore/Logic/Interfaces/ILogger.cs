using RcloneFileWatcherCore.Enums;
using System;

namespace RcloneFileWatcherCore.Logic.Interfaces
{
    public interface ILogger
    {
        LogLevel EnabledLevels { get; set; }

        void Log(LogLevel level, string message, Exception exception = null);

        void Trace(string message);
        void Debug(string message);
        void Info(string message);
        void Warn(string message);
        void Error(string message, Exception exception = null);
        void Critical(string message, Exception exception = null);
    }
}
