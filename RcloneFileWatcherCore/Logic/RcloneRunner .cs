using RcloneFileWatcherCore.Logic.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RcloneFileWatcherCore.Logic
{
    public class RcloneRunner : IRcloneRunner
    {
        private readonly ILogger _logger;

        public RcloneRunner(ILogger logger)
        {
            _logger = logger;
        }
        public bool RunBatch(string batchPath)
        {
            if (string.IsNullOrWhiteSpace(batchPath))
            {
                _logger.Write("Rclone batch file is empty or null.");
                return false;
            }

            using (var process = new Process())
            {
                process.StartInfo.FileName = batchPath;
                process.StartInfo.CreateNoWindow = false;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                _logger.Write($"Starting Rclone with batch file: {batchPath}");
                process.Start();
                process.WaitForExit();
                _logger.Write($"Rclone process exited with code: {process.ExitCode}");
                return process.ExitCode == 0;
            }
        }
    }
}
