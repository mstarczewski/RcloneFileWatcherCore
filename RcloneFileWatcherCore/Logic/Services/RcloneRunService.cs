using RcloneFileWatcherCore.Infrastructure.Logging.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RcloneFileWatcherCore.Logic.Services
{
    public class RcloneRunService : IBatchExecutionService
    {
        private readonly ILogger _logger;

        public RcloneRunService(ILogger logger)
        {
            _logger = logger;
        }
        public bool ExecuteBatch(string batchPath)
        {
            if (string.IsNullOrWhiteSpace(batchPath))
            {
                _logger.Log(Enums.LogLevel.Error, "Rclone batch file is empty or null.");
                return false;
            }

            using (var process = new Process())
            {
                process.StartInfo.FileName = batchPath;
                process.StartInfo.CreateNoWindow = false;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                _logger.Log(Enums.LogLevel.Information, $"Starting Rclone with batch file: {batchPath}");
                process.Start();
                process.WaitForExit();
                _logger.Log(Enums.LogLevel.Information, $"Rclone process exited with code: {process.ExitCode}");
                return process.ExitCode == 0;
            }
        }
    }
}
