using RcloneFileWatcherCore.Infrastructure.Logging.Interfaces;
using System;

namespace RcloneFileWatcherCore.Infrastructure.Logging
{
    public class ConsoleLogWriter : ILogWriter
    {
        public void Write(string message)
        {
            Console.WriteLine(message);
        }
    }
}