using RcloneFileWatcherCore.Config;
using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Enums;
using RcloneFileWatcherCore.Infrastructure.Logging.Interfaces;
using RcloneFileWatcherCore.Logic.Interfaces;
using RcloneFileWatcherCore.Logic.Services;
using RcloneFileWatcherCore.Status;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RcloneFileWatcherCore.App
{
    /// <summary>
    /// Owns the lifecycle of the config-dependent runtime graph (file watcher + scheduler and
    /// the rclone job services they drive). Building the graph here — rather than in the DI
    /// container — lets the whole graph be torn down and rebuilt against a new configuration,
    /// which is how config changes are applied without restarting the process.
    /// </summary>
    public class RuntimeController : IRuntimeController, IDisposable
    {
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IBatchExecutionService _rcloneRunner;
        private readonly IStatusService _status;
        private readonly IConfigService _configService;
        private readonly ConcurrentDictionary<string, FileDTO> _fileDTOs;
        private readonly object _lock = new object();
        // Serializes rclone job execution so a scheduled run and a manual GUI run never overlap.
        // Shared with the Scheduler. Separate from _lock so a long sync doesn't block Start/Stop.
        private readonly object _jobGate = new object();

        private FileWatcherService _watcher;
        private Scheduler _scheduler;
        private Dictionary<ProcessCode, IRcloneJobService> _processes;
        private bool _running;

        public bool IsRunning
        {
            get { lock (_lock) { return _running; } }
        }

        public RuntimeController(
            ILogger logger,
            IFileSystem fileSystem,
            IBatchExecutionService rcloneRunner,
            IStatusService status,
            IConfigService configService,
            ConcurrentDictionary<string, FileDTO> fileDTOs)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _rcloneRunner = rcloneRunner;
            _status = status;
            _configService = configService;
            _fileDTOs = fileDTOs;
            _configService.Changed += OnConfigChanged;
        }

        public void Start() => StartInternal(runStartupSync: true);

        public void Stop()
        {
            lock (_lock)
            {
                if (!_running)
                    return;

                _scheduler?.Dispose();
                _watcher?.Stop();
                _scheduler = null;
                _watcher = null;
                _processes = null;
                _running = false;
                _status.MarkWatcherStopped();
                _logger.Log(LogLevel.Information, "Watcher stopped");
            }
        }

        /// <summary>Run a one-off live sync now; the task completes when rclone finishes so the
        /// GUI can keep its button disabled until then.</summary>
        public Task<bool> SyncNowAsync() => RunJobAsync(ProcessCode.SyncRclone, "Manual sync requested");

        /// <summary>Run the full sync now; the task completes when rclone finishes.</summary>
        public Task<bool> FullSyncNowAsync() => RunJobAsync(ProcessCode.FullSyncRclone, "Manual full sync requested");

        /// <summary>Hard-stop the rclone process(es) currently running (manual or scheduled).</summary>
        public void StopRclone()
        {
            _logger.Log(LogLevel.Information, "Stopping rclone (requested from GUI)");
            _rcloneRunner.CancelRunning();
        }

        private Task<bool> RunJobAsync(ProcessCode code, string message)
        {
            Dictionary<ProcessCode, IRcloneJobService> processes;
            ConfigDTO config;
            lock (_lock)
            {
                if (!_running || _processes == null)
                {
                    _logger.Log(LogLevel.Warning, "Cannot run job: runtime is not running.");
                    return Task.FromResult(false);
                }
                processes = _processes;
                config = _configService.Current;
            }

            if (processes.TryGetValue(code, out var job))
            {
                _logger.Log(LogLevel.Information, message);
                return Task.Run(() =>
                {
                    try { lock (_jobGate) return job.Execute(config); }
                    catch (Exception ex) { _logger.Log(LogLevel.Error, "Manual job failed", ex); return false; }
                });
            }
            return Task.FromResult(false);
        }

        private void StartInternal(bool runStartupSync)
        {
            lock (_lock)
            {
                if (_running)
                    return;

                var config = _configService.Current;
                // Disabled paths stay in the config but are neither watched nor synced.
                var enabledPaths = config.Path?.Where(p => p.Enabled).ToList() ?? new List<PathDTO>();
                var processes = BuildProcesses(config, enabledPaths);
                var watcher = new FileWatcherService(_logger, _fileDTOs, enabledPaths);
                var scheduler = new Scheduler(_logger, processes, config, _jobGate);

                // Start the watcher first: if a watched path is invalid it throws here, before
                // we publish the instances or enable the timer. Initial start lets it propagate
                // (the console treats a bad config as fatal); the reload path catches it.
                watcher.Start();

                _watcher = watcher;
                _scheduler = scheduler;
                _processes = processes;
                _status.MarkWatcherStarted(enabledPaths.Select(p => p.WatchingPath).ToList());
                _logger.Log(LogLevel.Information, "Watcher started");

                if (runStartupSync)
                    _scheduler.RunStartupSyncIfNeeded();

                _scheduler.SetTimer();
                _running = true;
            }
        }

        private Dictionary<ProcessCode, IRcloneJobService> BuildProcesses(ConfigDTO config, List<PathDTO> enabledPaths)
        {
            var filePrepare = new FilePrepareService(_logger, enabledPaths, _fileDTOs, _fileSystem);
            return new Dictionary<ProcessCode, IRcloneJobService>
            {
                { ProcessCode.SyncRclone, new RcloneSyncService(_logger, filePrepare, _fileDTOs, _rcloneRunner) },
                { ProcessCode.UpdateRclone, new RcloneUpdateService(_logger) },
                { ProcessCode.FullSyncRclone, new RcloneFullSyncService(_logger, _rcloneRunner) },
            };
        }

        private void OnConfigChanged(ConfigDTO newConfig)
        {
            _logger.Log(LogLevel.Information, "Applying new configuration (live reload)");
            lock (_lock)
            {
                Stop();
                // Drop queued changes that referenced the previous configuration so a reload
                // does not sync paths that may no longer be watched.
                _fileDTOs.Clear();
                try
                {
                    StartInternal(runStartupSync: false);
                }
                catch (Exception ex)
                {
                    // A bad path in the new config must not tear down the GUI/host; log and stay
                    // stopped so the user can correct it and save again.
                    _logger.Log(LogLevel.Error, "Failed to apply new configuration; watcher is stopped", ex);
                }
            }
        }

        public void Dispose()
        {
            _configService.Changed -= OnConfigChanged;
            Stop();
        }
    }
}
