using RcloneFileWatcherCore.Infrastructure.Logging.Interfaces;
using System;
using System.IO;
using System.Text;

namespace RcloneFileWatcherCore.Infrastructure.Logging
{
    public class FileLogWriter : ILogWriter, IDisposable
    {
        private readonly StreamWriter _writer;

        public FileLogWriter(string filePath)
        {
            _writer = new StreamWriter(filePath, true, Encoding.UTF8, 4096)
            {
                AutoFlush = true
            };
        }

        public void Write(string message)
        {
            _writer.WriteLine(message);
        }

        public void Dispose()
        {
            _writer?.Dispose();
        }
    }
}