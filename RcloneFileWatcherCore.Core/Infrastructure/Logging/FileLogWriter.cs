using RcloneFileWatcherCore.Infrastructure.Logging.Interfaces;
using System;
using System.IO;
using System.Text;

namespace RcloneFileWatcherCore.Infrastructure.Logging
{
    public class FileLogWriter : ILogWriter, IDisposable
    {
        private readonly StreamWriter _writer;
        // StreamWriter is not thread-safe; this guards concurrent writers so the logger no longer
        // needs to serialize all sinks behind one lock just to protect the file.
        private readonly object _lock = new object();

        public FileLogWriter(string filePath)
        {
            _writer = new StreamWriter(filePath, true, Encoding.UTF8, 4096)
            {
                AutoFlush = true
            };
        }

        public void Write(string message)
        {
            lock (_lock)
                _writer.WriteLine(message);
        }

        public void Dispose()
        {
            lock (_lock)
                _writer?.Dispose();
        }
    }
}