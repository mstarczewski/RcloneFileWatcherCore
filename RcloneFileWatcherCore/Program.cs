using RcloneFileWatcherCore.Logic;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Timers;


namespace RcloneFileWatcherCore
{
    class Program
    {
        static void Main(string[] args)
        {
            Controller controller = new Controller();
            new System.Threading.AutoResetEvent(false).WaitOne();
        }
    }
}

