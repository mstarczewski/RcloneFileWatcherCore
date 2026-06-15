using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Globals;
using RcloneFileWatcherCore.Infrastructure.Logging.Interfaces;
using RcloneFileWatcherCore.Logic.Interfaces;
using System;
using System.Collections.Generic;
using System.Timers;

namespace RcloneFileWatcherCore.App
{
    public class Scheduler : IDisposable
    {
        private readonly Timer _timer;
        private readonly ILogger _logger;
        private DateTime _dateTimeUpdate = DateTime.Now;
        private readonly ConfigDTO _configDTO;
        private readonly Dictionary<Enums.ProcessCode, IRcloneJobService> _processDictionary;
        private readonly object _jobGate;
        private DateTime? _nextfullSyncAfter;

        public Scheduler(ILogger logger, Dictionary<Enums.ProcessCode, IRcloneJobService> processDictionary, ConfigDTO configDTO, object jobGate)
        {
            _processDictionary = processDictionary;
            _jobGate = jobGate;
            _logger = logger;
            _configDTO = configDTO;
            var intervalMs = Math.Max(1000, _configDTO.SyncIntervalSeconds * 1000);
            _timer = new Timer(intervalMs);
            _timer.Elapsed += OnTimedEvent;
            _timer.AutoReset = false;
            LogInvalidScheduleTimes();
            _nextfullSyncAfter = FullSyncScheduleCalculator.NextOccurrence(_configDTO.FullSyncSchedule, DateTime.Now);
        }

        // Surface a bad hand-edited schedule time once at startup; the calculator silently skips
        // unparseable entries so one typo can't stop the others from firing.
        private void LogInvalidScheduleTimes()
        {
            if (_configDTO?.FullSyncSchedule == null)
                return;
            foreach (var entry in _configDTO.FullSyncSchedule)
            {
                if (entry != null && entry.Days != Enums.ScheduleDays.None && !FullSyncScheduleEntry.TryParseTime(entry.Time, out _))
                    _logger.Log(Enums.LogLevel.Error, $"Invalid full-sync schedule time, entry ignored: {entry.Time}");
            }
        }

        public void RunStartupSyncIfNeeded()
        {
            TryFullSyncAtStart();
        }
        private void TryFullSyncAtStart()
        {
            // Works for both Script and Managed full-sync modes: the full-sync service itself
            // decides whether there is anything to run (a script path or managed commands).
            if ((_configDTO?.RunOneTimeFullStartupSync ?? false)
                && _processDictionary.TryGetValue(Enums.ProcessCode.FullSyncRclone, out var fullsyncProcess))
            {
                _logger.Log(Enums.LogLevel.Information, "Running full sync at startup.");
                lock (_jobGate)
                    fullsyncProcess.Execute(_configDTO);
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
                // Serialize scheduled jobs against manual GUI-triggered runs (RuntimeController
                // shares this gate) so the same rclone job can't run twice concurrently.
                lock (_jobGate)
                {
                    TryUpdateRclone();
                    TrySyncRclone();
                    TryFullSyncRclone();
                }
            }
            catch (Exception ex)
            {
                _logger.Log(Enums.LogLevel.Error, "Exception in scheduler timer event", ex);
            }
            finally
            {
                _timer.Start();
            }
        }

        private void TryFullSyncRclone()
        {
            if (_nextfullSyncAfter is DateTime due && due <= DateTime.Now
                && _processDictionary.TryGetValue(Enums.ProcessCode.FullSyncRclone, out var fullsyncProcess))
            {
                _logger.Log(Enums.LogLevel.Information, $"Running full sync as per schedule ({due:yyyy-MM-dd HH:mm}).");
                // Advance to the next slot BEFORE running. NextOccurrence is strictly-after, so
                // computing from now excludes the slot we're about to run. Doing this first means a
                // throwing or long-running full sync can't busy-loop (re-fire every tick) and a
                // second slot later today still fires on a later tick instead of being skipped.
                _nextfullSyncAfter = FullSyncScheduleCalculator.NextOccurrence(_configDTO.FullSyncSchedule, DateTime.Now);
                fullsyncProcess.Execute(_configDTO);
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
                updateProcess.Execute(_configDTO);
            }
        }

        private void TrySyncRclone()
        {
            if (_processDictionary.TryGetValue(Enums.ProcessCode.SyncRclone, out var syncProcess))
            {
                syncProcess.Execute(_configDTO);
            }
        }
        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
