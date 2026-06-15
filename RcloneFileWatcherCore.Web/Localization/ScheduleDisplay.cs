using RcloneFileWatcherCore.Enums;
using System.Collections.Generic;
using System.Globalization;

namespace RcloneFileWatcherCore.Web.Localization
{
    /// <summary>
    /// Renders a <see cref="ScheduleDays"/> bit set as a short, localized label - the common
    /// presets (every day / weekdays / weekend) get their own phrase, otherwise the selected days
    /// are listed using the current culture's abbreviated day names, in the shared Monday-first
    /// <see cref="ScheduleWeek.Ordered"/> order the editor also uses.
    /// </summary>
    public static class ScheduleDisplay
    {
        public static string Days(ScheduleDays days, Loc l)
        {
            if (days == ScheduleDays.EveryDay)
                return l["sched.everyDay"];
            if (days == ScheduleDays.Weekdays)
                return l["sched.weekdays"];
            if (days == ScheduleDays.Weekend)
                return l["sched.weekend"];

            var abbreviated = CultureInfo.CurrentUICulture.DateTimeFormat.AbbreviatedDayNames;
            var selected = new List<string>();
            foreach (var (day, flag) in ScheduleWeek.Ordered)
            {
                if (ScheduleWeek.Has(days, flag))
                    selected.Add(abbreviated[(int)day]);
            }
            return selected.Count > 0 ? string.Join(", ", selected) : l["sched.none"];
        }
    }
}
