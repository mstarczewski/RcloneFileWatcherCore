using System;
using System.Collections.Generic;
using System.Text;

namespace RcloneFileWatcherCore.Logic.Interfaces
{
    interface ILogger
    {
        bool Enable { get; set; }
        void Write(string text);
    }
}
