using RcloneFileWatcherCore.Enums;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RcloneFileWatcherCore.DTO
{
    public class ConfigDTO
    {
        public string LogLevel { get; set; }
        public string LogPath { get; set; }
        public List<PathDTO> Path { get; set; }
        public UpdateRcloneDTO UpdateRclone { get; set; }
        public int SyncIntervalSeconds { get; set; } = 60;

        /// <summary>
        /// When true, a whole newly-created/renamed/deleted directory is passed to rclone as a single
        /// "<c>dir/**</c>" include rule instead of listing every file under it - collapsing thousands
        /// of per-file entries into one (rclone then walks only that subtree). Off by default.
        /// </summary>
        public bool CollapseDirectoryChanges { get; set; }

        /// <summary>
        /// Quiet-period debounce: when &gt; 0, a live sync is deferred while changes are still
        /// arriving - it runs only once no new change has appeared for this many seconds (so a long
        /// copy is synced after it settles, not chopped into many partial runs). 0 = off.
        /// </summary>
        public int QuietPeriodSeconds { get; set; }

        /// <summary>
        /// Safety cap for the quiet period: sync anyway once the oldest pending change has waited
        /// this long, even if changes keep arriving (prevents starvation under continuous activity).
        /// 0 = no cap.
        /// </summary>
        public int QuietPeriodMaxWaitSeconds { get; set; } = 300;
        public bool RunOneTimeFullStartupSync { get; set; } = true;

        /// <summary>
        /// Legacy single daily full-sync time (HH:mm). Superseded by <see cref="FullSyncSchedule"/>;
        /// kept only so old config files still load. On load it is migrated into a single
        /// every-day schedule entry and then cleared (see ConfigNormalizer). Do not read it
        /// directly - use <see cref="FullSyncSchedule"/>. Still deserialized for migration, but
        /// not written back once cleared (WhenWritingDefault), so it ages out of saved configs.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string RunStartupScriptEveryDayAt { get; set; }

        /// <summary>
        /// Weekly full-sync schedule: each entry fires on its selected weekdays at its time of day
        /// (local time). Empty = no scheduled full sync (the startup sync and the manual button
        /// still work). Several entries give several runs per day/week.
        /// </summary>
        public List<FullSyncScheduleEntry> FullSyncSchedule { get; set; }

        /// <summary>Whether the full sync runs an external script or managed rclone command(s).</summary>
        public SyncMode FullSyncMode { get; set; } = SyncMode.Script;

        /// <summary>Full-sync script (Script mode) - runs the whole-tree reconcile.</summary>
        public string RunOneTimeFullStartupSyncBatch { get; set; }

        /// <summary>Managed full-sync rclone commands (Managed mode), run in order. Each is a
        /// whole-tree reconcile (no --include-from filter).</summary>
        public List<RcloneCommandDTO> FullSyncCommands { get; set; }

        public ConfigDTO()
        {
            Path = new List<PathDTO>();
            UpdateRclone = new UpdateRcloneDTO();
            FullSyncCommands = new List<RcloneCommandDTO>();
            FullSyncSchedule = new List<FullSyncScheduleEntry>();
        }
    }
}