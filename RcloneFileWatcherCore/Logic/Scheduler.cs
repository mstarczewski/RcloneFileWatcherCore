using RcloneFileWatcherCore.DTO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

namespace RcloneFileWatcherCore.Logic
{
    class Scheduler
    {
        private System.Timers.Timer _timer;
        private readonly ProcessLogic _process;
        private readonly FilePrepare _setUpLogic;
        private ConcurrentDictionary<string, FileDTO> _fileList;
        private readonly Logger _logger;

        public Scheduler(Logger logger, ProcessLogic process, FilePrepare setUpLogic, ConcurrentDictionary<string, FileDTO> fileList)
        {
            _process = process;
            _setUpLogic = setUpLogic;
            _logger = logger;
            _fileList = fileList;
        }
        public void SetTimer()
        {
            _timer = new System.Timers.Timer(30000);
            _timer.Elapsed += OnTimedEvent;
            _timer.AutoReset = false;
            _timer.Enabled = true;
        }

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            try
            {
                var sourePathList = _fileList.Select(x => x.Value.SourcePath).Distinct().ToList();
                foreach (var sourcePath in sourePathList)
                {
                    string rcloneBatch =_setUpLogic.PrepareFilesToSync(sourcePath);
                    if (!string.IsNullOrWhiteSpace(sourcePath))
                    {
                        _process.StartProcess(rcloneBatch);
                    }
                }
            }

            catch (Exception ex)
            {
                _logger.ConsoleWriter(ex.ToString());
            }
            finally
            {
                _timer.Start();
            }
        }
    }
}
