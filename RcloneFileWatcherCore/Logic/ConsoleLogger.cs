using RcloneFileWatcherCore.Logic.Interfaces;
using System;

namespace RcloneFileWatcherCore.Logic
{
    class ConsoleLogger:ILogger
    {
        public bool Enable { get; set; }

        public void Write(string text)
        {
            if (Enable)
            {
                Console.WriteLine($@"{DateTime.Now} {text}");
            }
        }
    }
}
