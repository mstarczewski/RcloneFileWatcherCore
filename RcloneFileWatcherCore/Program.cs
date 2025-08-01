using RcloneFileWatcherCore.Globals;
using RcloneFileWatcherCore.Logic;
using RcloneFileWatcherCore.Logic.Interfaces;
using System;
using System.Linq;

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
                var controller = new Controller();
                bool generateConfig = args.Contains("--generateConfig") || args.Contains("-generateConfig");
                controller.Start(generateConfig);
                new System.Threading.AutoResetEvent(false).WaitOne();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex}");
            }
        }
    }
}

