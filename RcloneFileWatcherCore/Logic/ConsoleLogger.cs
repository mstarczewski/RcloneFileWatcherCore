﻿using RcloneFileWatcherCore.Logic.Interfaces;
using System;

namespace RcloneFileWatcherCore.Logic
{
    class ConsoleLogger : ILogger
    {
        public bool Enable { get; set; }

        public void Write(string text)
        {
            if (Enable)
            {
                WriteAlways(text);
            }
        }
        public void WriteAlways(string text)
        {
            Console.WriteLine($@"{DateTime.Now} {text}");
        }
    }
}
