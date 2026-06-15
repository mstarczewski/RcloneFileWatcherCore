using Microsoft.VisualStudio.TestTools.UnitTesting;
using RcloneFileWatcherCore.App;
using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Enums;
using System;
using System.Collections.Generic;

namespace RcloneFileWatcherCore.Tests.App
{
    [TestClass]
    public class FullSyncScheduleCalculatorTests
    {
        // 2026-06-15 is a Monday; fixed reference points keep these deterministic.
        private static readonly DateTime MondayNoon = new DateTime(2026, 6, 15, 12, 0, 0);

        private static List<FullSyncScheduleEntry> Schedule(params FullSyncScheduleEntry[] entries)
            => new List<FullSyncScheduleEntry>(entries);

        [TestMethod]
        public void EveryDay_TimeLaterToday_ReturnsToday()
        {
            var next = FullSyncScheduleCalculator.NextOccurrence(
                Schedule(new FullSyncScheduleEntry { Days = ScheduleDays.EveryDay, Time = "18:00" }),
                MondayNoon);

            Assert.AreEqual(new DateTime(2026, 6, 15, 18, 0, 0), next);
        }

        [TestMethod]
        public void EveryDay_TimeAlreadyPassed_ReturnsTomorrow()
        {
            var next = FullSyncScheduleCalculator.NextOccurrence(
                Schedule(new FullSyncScheduleEntry { Days = ScheduleDays.EveryDay, Time = "06:00" }),
                MondayNoon);

            Assert.AreEqual(new DateTime(2026, 6, 16, 6, 0, 0), next);
        }

        [TestMethod]
        public void Weekdays_OnFridayAfternoon_SkipsWeekend_ReturnsMonday()
        {
            var fridayAfternoon = new DateTime(2026, 6, 19, 15, 0, 0); // Friday
            var next = FullSyncScheduleCalculator.NextOccurrence(
                Schedule(new FullSyncScheduleEntry { Days = ScheduleDays.Weekdays, Time = "06:00" }),
                fridayAfternoon);

            Assert.AreEqual(new DateTime(2026, 6, 22, 6, 0, 0), next); // next Monday
        }

        [TestMethod]
        public void Weekend_OnWednesday_ReturnsSaturday()
        {
            var wednesday = new DateTime(2026, 6, 17, 9, 0, 0);
            var next = FullSyncScheduleCalculator.NextOccurrence(
                Schedule(new FullSyncScheduleEntry { Days = ScheduleDays.Weekend, Time = "03:00" }),
                wednesday);

            Assert.AreEqual(new DateTime(2026, 6, 20, 3, 0, 0), next); // Saturday
        }

        [TestMethod]
        public void MultipleEntries_PicksSoonest()
        {
            var next = FullSyncScheduleCalculator.NextOccurrence(
                Schedule(
                    new FullSyncScheduleEntry { Days = ScheduleDays.EveryDay, Time = "06:00" },
                    new FullSyncScheduleEntry { Days = ScheduleDays.EveryDay, Time = "18:00" }),
                MondayNoon);

            Assert.AreEqual(new DateTime(2026, 6, 15, 18, 0, 0), next); // 06:00 already passed, 18:00 is next
        }

        [TestMethod]
        public void EmptySchedule_ReturnsNull()
        {
            Assert.IsNull(FullSyncScheduleCalculator.NextOccurrence(Schedule(), MondayNoon));
            Assert.IsNull(FullSyncScheduleCalculator.NextOccurrence(null, MondayNoon));
        }

        [TestMethod]
        public void EntryWithNoDays_IsIgnored()
        {
            var next = FullSyncScheduleCalculator.NextOccurrence(
                Schedule(new FullSyncScheduleEntry { Days = ScheduleDays.None, Time = "06:00" }),
                MondayNoon);

            Assert.IsNull(next);
        }

        [TestMethod]
        public void InvalidTime_IsSkipped_OtherEntriesStillResolve()
        {
            var next = FullSyncScheduleCalculator.NextOccurrence(
                Schedule(
                    new FullSyncScheduleEntry { Days = ScheduleDays.EveryDay, Time = "not-a-time" },
                    new FullSyncScheduleEntry { Days = ScheduleDays.EveryDay, Time = "18:00" }),
                MondayNoon);

            Assert.AreEqual(new DateTime(2026, 6, 15, 18, 0, 0), next);
        }

        [TestMethod]
        public void BareIntegerTime_IsRejected_NotTreatedAsDays()
        {
            // "5" must not be accepted as 5 days (the old TimeSpan.TryParse trap); the entry is
            // skipped, so a schedule with only that entry yields no occurrence.
            var next = FullSyncScheduleCalculator.NextOccurrence(
                Schedule(new FullSyncScheduleEntry { Days = ScheduleDays.EveryDay, Time = "5" }),
                MondayNoon);

            Assert.IsNull(next);
        }

        [TestMethod]
        public void SingleDay_ExactlyAtTime_ReturnsNextWeek_NotNow()
        {
            // Candidate must be strictly after the reference, so the slot at the current instant
            // rolls to the same weekday next week (avoids double-firing with the just-run sync).
            var next = FullSyncScheduleCalculator.NextOccurrence(
                Schedule(new FullSyncScheduleEntry { Days = ScheduleDays.Monday, Time = "12:00" }),
                MondayNoon);

            Assert.AreEqual(new DateTime(2026, 6, 22, 12, 0, 0), next);
        }
    }
}
