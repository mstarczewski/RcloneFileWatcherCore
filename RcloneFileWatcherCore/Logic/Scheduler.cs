using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Logic.Interfaces;
using System;
using System.Collections.Generic;
using System.Timers;

namespace RcloneFileWatcherCore.Logic
{
    public class Scheduler : IDisposable
    {
        private readonly Timer _timer;
        private readonly ILogger _logger;
        private DateTime _dateTimeUpdate = DateTime.Now;
        private readonly ConfigDTO _configDTO;
        private readonly Dictionary<Enums.ProcessCode, IProcess> _processDictionary;

        public Scheduler(ILogger logger, Dictionary<Enums.ProcessCode, IProcess> processDictionary, ConfigDTO configDTO)
        {
            _processDictionary = processDictionary;
            _logger = logger;
            _configDTO = configDTO;
            _timer = new Timer(_configDTO.SyncIntervalSeconds*1000);
            _timer.Elapsed += OnTimedEvent;
            _timer.AutoReset = false;
        }

        public void SetTimer()
        {
            _timer.Enabled = true;
        }

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            try
            {
                TryUpdateRclone();
                TrySyncRclone();
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

        private void TryUpdateRclone()
        {
            var updateRclone = _configDTO?.UpdateRclone;
            if (updateRclone?.Update == true &&
                _dateTimeUpdate.AddHours(updateRclone.CheckUpdateHours) < DateTime.Now &&
                _processDictionary.TryGetValue(Enums.ProcessCode.UpdateRclone, out var updateProcess))
            {
                _dateTimeUpdate = DateTime.Now;
                updateProcess.Start(_configDTO);
            }
        }

        private void TrySyncRclone()
        {
            if (_processDictionary.TryGetValue(Enums.ProcessCode.SyncRclone, out var syncProcess))
            {
                syncProcess.Start(_configDTO);
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
