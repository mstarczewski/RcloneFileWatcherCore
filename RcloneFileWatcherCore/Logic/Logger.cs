using System;
using System.Collections.Generic;
using System.Text;

namespace RcloneFileWatcherCore.Logic
{
    class Logger
    {
        private  bool _consoleWriter;
        public void WriteToConsole(bool consoleWriter)
        {
            _consoleWriter = consoleWriter;
        }
        public void ConsoleWriter(string text)
        {
            if (_consoleWriter)
            {
                Console.WriteLine($@"{DateTime.Now} {text}");
            }
        }
    }
}
