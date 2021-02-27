using RcloneFileWatcherCore.DTO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace RcloneFileWatcherCore.Logic
{
    class ProcessLogic
    {
        private readonly Logger _logger;
        public ProcessLogic(Logger logger)
        {
            _logger = logger;
        }
        public void StartProcess(string rcloneBatch)
        {
            Process process = new Process();
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.CreateNoWindow = false;

            _logger.ConsoleWriter("Starting rclone");
            process.StartInfo.FileName = rcloneBatch;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.Start();
            process.WaitForExit();
            _logger.ConsoleWriter("Finished rclone");
        }

    }
}