using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Logic.Interfaces;
using System;
using System.Collections.Generic;
using System.Timers;

namespace RcloneFileWatcherCore.Logic
{
    class Scheduler
    {
        private Timer _timer;
        private readonly ILogger _logger;
        private const int timeOut = 1000 * 30;
        private DateTime dateTimeUpdate = DateTime.Now;
        private readonly ConfigDTO _configDTO;
        private readonly Dictionary<Enums.ProcessCode, IProcess> _processDictionary;
        public Scheduler(ILogger logger, Dictionary<Enums.ProcessCode, IProcess> processDictionary, ConfigDTO configDTO)
        {
            _processDictionary = processDictionary;
            _logger = logger;
            _configDTO = configDTO;
        }
        public void SetTimer()
        {
            _timer = new Timer(timeOut);
            _timer.Elapsed += OnTimedEvent;
            _timer.AutoReset = false;
            _timer.Enabled = true;
        }

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            try
            {
                if ((_configDTO?.UpdateRclone?.Update ?? false) && dateTimeUpdate.AddHours(_configDTO.UpdateRclone.ChceckUpdateHours) < DateTime.Now)
                {
                    dateTimeUpdate = DateTime.Now;
                    _processDictionary[Enums.ProcessCode.UpdateRclone].Start(_configDTO);
                }
                _processDictionary[Enums.ProcessCode.SyncRclone].Start(_configDTO);
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
