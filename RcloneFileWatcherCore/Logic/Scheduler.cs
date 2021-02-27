using RcloneFileWatcherCore.Logic.Interfaces;
using System;
using System.Timers;

namespace RcloneFileWatcherCore.Logic
{
    class Scheduler
    {
        private System.Timers.Timer _timer;
        private readonly ProcessRunner _processRunner;
        private readonly ILogger _logger;

        public Scheduler(ILogger logger, ProcessRunner processRunner)
        {
            _processRunner = processRunner;
            _logger = logger;
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
                _processRunner.StartProcess();
            }

            catch (Exception ex)
            {
                _logger.Write(ex.ToString());
            }
            finally
            {
                _timer.Start();
            }
        }
    }
}
