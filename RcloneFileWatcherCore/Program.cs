using RcloneFileWatcherCore.Globals;
using RcloneFileWatcherCore.Logic;
using RcloneFileWatcherCore.Logic.Interfaces;
using System;
using System.Linq;
using RcloneFileWatcherCore.App;

namespace RcloneFileWatcherCore
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Contains("--version") || args.Contains("-v"))
                {
                    Console.WriteLine(AppVersion.GetVersion());
                    return;
                }
                var startupManager = new StartupManager(args.Contains("--generateConfig") || args.Contains("-generateConfig"));
                startupManager.Start();
                new System.Threading.AutoResetEvent(false).WaitOne();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex}");
            }
        }
    }
}

