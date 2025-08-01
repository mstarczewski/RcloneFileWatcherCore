using RcloneFileWatcherCore.Logic.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RcloneFileWatcherCore.Globals
{
    public static class RcloneProcess
    {
        public static bool RunRcloneProcess(string rcloneBatch, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(rcloneBatch))
            {
                logger.Write("Rclone batch file is empty or null.");
                return false;
            }   
            using (var process = new Process())
            {
                process.StartInfo.FileName = rcloneBatch;
                process.StartInfo.CreateNoWindow = false;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                logger.Write("Starting rclone");
                process.Start();
                process.WaitForExit();
                logger.Write("Finished rclone");
            }
            return true;
        }
    }
}
