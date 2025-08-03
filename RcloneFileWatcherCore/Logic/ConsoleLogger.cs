using RcloneFileWatcherCore.Enums;
using RcloneFileWatcherCore.Logic.Interfaces;
using System;

namespace RcloneFileWatcherCore.Logic
{
    public class ConsoleLogger : ILogger
    {
        public LogLevel EnabledLevels { get; set; } = LogLevel.All;

        public void Log(LogLevel level, string message, Exception exception = null)
        {
            if (level != LogLevel.Always && (EnabledLevels & level) == 0)
            {
                return;
            }

            var output = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
            if (exception != null)
                output += Environment.NewLine + exception;

            Console.WriteLine(output);
        }

        public void Trace(string message) => Log(LogLevel.Trace, message);
        public void Debug(string message) => Log(LogLevel.Debug, message);
        public void Info(string message) => Log(LogLevel.Information, message);
        public void Warn(string message) => Log(LogLevel.Warning, message);
        public void Error(string message, Exception ex = null) => Log(LogLevel.Error, message, ex);
        public void Critical(string message, Exception ex = null) => Log(LogLevel.Critical, message, ex);
        public void Always(string message) => Log(LogLevel.Always, message);
    }
}

