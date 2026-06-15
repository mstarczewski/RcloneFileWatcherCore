using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Enums;
using System;
using System.Collections.Generic;

namespace RcloneFileWatcherCore.App
{
    /// <summary>
    /// Pure schedule math for the full sync: given a weekly schedule and a reference instant,
    /// works out the next time the full sync should run. No timers, no logging, no clock reads -
    /// so it is deterministic and unit-testable. All times are local wall-clock; the firing
    /// resolution is the scheduler's tick interval, and DST transitions follow local time.
    /// </summary>
    public static class FullSyncScheduleCalculator
    {
        /// <summary>
        /// The soonest scheduled instant strictly after <paramref name="after"/>, or null if the
        /// schedule is empty / has no usable entry. Entries with no days or an unparseable time are
        /// skipped. Looks ahead a full week so any weekday combination resolves.
        /// </summary>
        public static DateTime? NextOccurrence(IEnumerable<FullSyncScheduleEntry> schedule, DateTime after)
        {
            if (schedule == null)
                return null;

            DateTime? best = null;
            foreach (var entry in schedule)
            {
                if (entry == null || entry.Days == ScheduleDays.None)
                    continue;
                if (!FullSyncScheduleEntry.TryParseTime(entry.Time, out var timeOfDay))
                    continue;

                // Walk today + the next 7 days; first matching weekday whose time is still ahead wins.
                for (var dayOffset = 0; dayOffset <= 7; dayOffset++)
                {
                    var day = after.Date.AddDays(dayOffset);
                    if (!DayMatches(entry.Days, day.DayOfWeek))
                        continue;

                    var candidate = day.Add(timeOfDay);
                    if (candidate <= after)
                        continue;

                    if (best == null || candidate < best.Value)
                        best = candidate;
                    break;
                }
            }

            return best;
        }

        public static bool DayMatches(ScheduleDays days, DayOfWeek dayOfWeek)
        {
            foreach (var (day, flag) in ScheduleWeek.Ordered)
                if (day == dayOfWeek)
                    return ScheduleWeek.Has(days, flag);
            return false;
        }
    }
}
