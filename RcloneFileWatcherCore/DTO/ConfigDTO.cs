using System.Collections.Generic;

namespace RcloneFileWatcherCore.DTO
{
    public class ConfigDTO
    {
        public string LogLevel { get; set; }
        public List<PathDTO> Path { get; set; }
        public UpdateRcloneDTO UpdateRclone { get; set; }
        public int SyncIntervalSeconds { get; set; } = 60000;
        public bool RunOneTimeFullStartupSync { get; set; } = true;
        public string RunOneTimeFullStartupSyncBatch { get; set; }
        public string RunStartupScriptEveryDayAt { get; set; } = "05:30";


        public ConfigDTO()
        {
            Path = new List<PathDTO>();
            UpdateRclone = new UpdateRcloneDTO();
        }
    }
}