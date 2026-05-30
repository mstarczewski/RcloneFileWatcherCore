using RcloneFileWatcherCore.Enums;
using System.Collections.Generic;

namespace RcloneFileWatcherCore.DTO
{
    public class ConfigDTO
    {
        public string LogLevel { get; set; }
        public string LogPath { get; set; }
        public List<PathDTO> Path { get; set; }
        public UpdateRcloneDTO UpdateRclone { get; set; }
        public int SyncIntervalSeconds { get; set; } = 60000;
        public bool RunOneTimeFullStartupSync { get; set; } = true;
        public string RunStartupScriptEveryDayAt { get; set; } = "05:30";

        /// <summary>Whether the full sync runs an external script or managed rclone command(s).</summary>
        public SyncMode FullSyncMode { get; set; } = SyncMode.Script;

        /// <summary>Full-sync script (Script mode) — runs the whole-tree reconcile.</summary>
        public string RunOneTimeFullStartupSyncBatch { get; set; }

        /// <summary>Managed full-sync rclone commands (Managed mode), run in order. Each is a
        /// whole-tree reconcile (no --include-from filter).</summary>
        public List<RcloneCommandDTO> FullSyncCommands { get; set; }

        public ConfigDTO()
        {
            Path = new List<PathDTO>();
            UpdateRclone = new UpdateRcloneDTO();
            FullSyncCommands = new List<RcloneCommandDTO>();
        }
    }
}