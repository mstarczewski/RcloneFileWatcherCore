using RcloneFileWatcherCore.Enums;
using RcloneFileWatcherCore.Infrastructure.Logging.Interfaces;
using System;

namespace RcloneFileWatcherCore.Infrastructure.Logging
{
    public class Logger : ILogger, IDisposable
    {
        private readonly object _lock = new object();
        private ILogWriter _logWriter;

        public LogLevel EnabledLevels { get; set; } = LogLevel.All;

        public Logger()
        {
            _logWriter = new ConsoleLogWriter();
        }

        public Logger(ILogWriter logWriter)
        {
            _logWriter = logWriter ?? throw new ArgumentNullException(nameof(logWriter), "Log writer cannot be null");
        }

        public void SetLogWriter(ILogWriter logWriter)
        {
            if (logWriter == null)
                throw new ArgumentNullException(nameof(logWriter), "Log writer cannot be null");

            lock (_lock)
            {
                _logWriter = logWriter;
            }
        }

        public void Log(LogLevel level, string message, Exception exception = null)
        {
            if (level != LogLevel.Always && (EnabledLevels & level) == 0)
                return;

            var output = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
            if (exception != null)
                output += Environment.NewLine + exception;

            try
            {
                lock (_lock)
                {
                    _logWriter.Write(output);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[LoggerError] {_logWriter.GetType().Name} failed: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_logWriter is IDisposable disposable)
            {
                disposable.Dispose();
            }
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