using System;
using System.Collections.Generic;

namespace RcloneFileWatcherCore.Enums
{
    /// <summary>
    /// Days of the week a scheduled full sync may run on, as a bit set so one schedule entry can
    /// cover several days (e.g. weekdays only). Serialized as a single integer by the default
    /// System.Text.Json options the config uses.
    /// </summary>
    [Flags]
    public enum ScheduleDays
    {
        None = 0,
        Monday = 1,
        Tuesday = 2,
        Wednesday = 4,
        Thursday = 8,
        Friday = 16,
        Saturday = 32,
        Sunday = 64,

        Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday,
        Weekend = Saturday | Sunday,
        EveryDay = Weekdays | Weekend
    }

    /// <summary>
    /// The week in display/iteration order (Monday-first), pairing each day with its
    /// <see cref="ScheduleDays"/> flag. Single source of truth so the editor chips, the schedule
    /// summary and the fire-time calculator can't drift on day order or membership.
    /// </summary>
    public static class ScheduleWeek
    {
        public static readonly IReadOnlyList<(DayOfWeek Day, ScheduleDays Flag)> Ordered = new[]
        {
            (DayOfWeek.Monday, ScheduleDays.Monday),
            (DayOfWeek.Tuesday, ScheduleDays.Tuesday),
            (DayOfWeek.Wednesday, ScheduleDays.Wednesday),
            (DayOfWeek.Thursday, ScheduleDays.Thursday),
            (DayOfWeek.Friday, ScheduleDays.Friday),
            (DayOfWeek.Saturday, ScheduleDays.Saturday),
            (DayOfWeek.Sunday, ScheduleDays.Sunday)
        };

        /// <summary>True when <paramref name="flag"/> is set in <paramref name="days"/>.</summary>
        public static bool Has(ScheduleDays days, ScheduleDays flag) => (days & flag) != 0;
    }
}
