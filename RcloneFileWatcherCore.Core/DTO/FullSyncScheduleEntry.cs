using RcloneFileWatcherCore.Enums;
using System;
using System.Globalization;

namespace RcloneFileWatcherCore.DTO
{
    /// <summary>
    /// One trigger in the full-sync schedule: a set of weekdays and a time of day (HH:mm, local
    /// time). The full sync runs on each selected day at that time. Several entries give several
    /// runs (e.g. weekdays at 06:00 and 18:00, weekend at 03:00).
    /// </summary>
    public class FullSyncScheduleEntry
    {
        /// <summary>Weekdays this entry fires on.</summary>
        public ScheduleDays Days { get; set; } = ScheduleDays.EveryDay;

        /// <summary>Time of day in 24h HH:mm (local time).</summary>
        public string Time { get; set; } = "05:30";

        /// <summary>True when <see cref="Time"/> is a valid 24h HH:mm value.</summary>
        public bool HasValidTime => TryParseTime(Time, out _);

        /// <summary>
        /// Parses a 24h "HH:mm" (or "H:mm") time of day using the invariant culture, so a stored
        /// config value parses identically regardless of the running thread's culture. Rejects
        /// out-of-range or non-time input such as "5" (which TimeSpan.TryParse would read as 5 days)
        /// or "24:00". Single source of truth for time validation across the GUI, scheduler and
        /// calculator.
        /// </summary>
        public static bool TryParseTime(string time, out TimeSpan timeOfDay)
        {
            timeOfDay = default;
            if (DateTime.TryParseExact(time, new[] { "HH:mm", "H:mm" },
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                timeOfDay = dt.TimeOfDay;
                return true;
            }
            return false;
        }
    }
}
