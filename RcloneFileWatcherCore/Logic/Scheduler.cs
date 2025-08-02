using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Globals;
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
        private DateTime nextfullSyncAfter = new DateTime();

        public Scheduler(ILogger logger, Dictionary<Enums.ProcessCode, IProcess> processDictionary, ConfigDTO configDTO)
        {
            _processDictionary = processDictionary;
            _logger = logger;
            _configDTO = configDTO;
            var intervalMs = Math.Max(1000, _configDTO.SyncIntervalSeconds * 1000);
            _timer = new Timer(intervalMs);
            _timer.Elapsed += OnTimedEvent;
            _timer.AutoReset = false;
        }

        public void RunStartupSyncIfNeeded()
        {
            TryFullSyncAtStart();
        }
        private void TryFullSyncAtStart()
        {
            if ((_configDTO?.RunOneTimeFullStartupSync ?? false) && !string.IsNullOrWhiteSpace(_configDTO.RunOneTimeFullStartupSyncBatch))
            {
                if (_processDictionary.TryGetValue(Enums.ProcessCode.FullSyncRclone, out var fullsyncProcess))
                {
                    _logger.Write("Running full sync at startup.");
                    fullsyncProcess.Start(_configDTO);
                }
            }
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
                TryFullSyncRclone();
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

        private void TryFullSyncRclone()
        {
            var now = DateTime.Now;

            if (nextfullSyncAfter <= now)
            {
                if (TimeSpan.TryParse(_configDTO?.RunStartupScriptEveryDayAt, out var scheduledTime))
                {
                    _logger.Write($"Checking for full sync at {scheduledTime} every day.");
                    DateTime todayRunTime = DateTime.Today.Add(scheduledTime);
                    if (now >= todayRunTime && _processDictionary.TryGetValue(Enums.ProcessCode.FullSyncRclone, out var fullsyncProcess))
                    {
                        _logger.Write($"Running full sync as per schedule.");
                        nextfullSyncAfter = DateTime.Today.AddDays(1).Add(scheduledTime);
                        fullsyncProcess.Start(_configDTO);
                    }
                }
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
