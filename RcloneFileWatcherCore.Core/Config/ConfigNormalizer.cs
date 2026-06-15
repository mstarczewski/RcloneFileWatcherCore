using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Enums;
using System.Collections.Generic;

namespace RcloneFileWatcherCore.Config
{
    /// <summary>
    /// Brings a freshly loaded <see cref="ConfigDTO"/> to the current shape so the rest of the app
    /// can rely on the new fields. Idempotent: running it twice changes nothing.
    /// </summary>
    public static class ConfigNormalizer
    {
        public static ConfigDTO Normalize(ConfigDTO config)
        {
            if (config == null)
                return null;

            config.FullSyncSchedule ??= new List<FullSyncScheduleEntry>();

            // Migrate the legacy single daily time into the schedule (once): an old config has a
            // RunStartupScriptEveryDayAt value and no schedule yet.
            if (config.FullSyncSchedule.Count == 0 && !string.IsNullOrWhiteSpace(config.RunStartupScriptEveryDayAt))
            {
                config.FullSyncSchedule.Add(new FullSyncScheduleEntry
                {
                    Days = ScheduleDays.EveryDay,
                    Time = config.RunStartupScriptEveryDayAt.Trim()
                });
            }

            // The schedule list is now the single source of truth. Clear the legacy field so that
            // an empty schedule (the user removed every entry) is not re-populated on the next load.
            config.RunStartupScriptEveryDayAt = null;

            return config;
        }
    }
}
