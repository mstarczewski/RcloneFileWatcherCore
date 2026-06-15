using Microsoft.VisualStudio.TestTools.UnitTesting;
using RcloneFileWatcherCore.Config;
using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Enums;
using System.Collections.Generic;

namespace RcloneFileWatcherCore.Tests.Config
{
    [TestClass]
    public class ConfigNormalizerTests
    {
        [TestMethod]
        public void LegacyDailyTime_IsMigrated_ToEveryDayEntry_AndCleared()
        {
            var config = new ConfigDTO
            {
                FullSyncSchedule = new List<FullSyncScheduleEntry>(),
                RunStartupScriptEveryDayAt = "05:30"
            };

            ConfigNormalizer.Normalize(config);

            Assert.AreEqual(1, config.FullSyncSchedule.Count);
            Assert.AreEqual(ScheduleDays.EveryDay, config.FullSyncSchedule[0].Days);
            Assert.AreEqual("05:30", config.FullSyncSchedule[0].Time);
            Assert.IsNull(config.RunStartupScriptEveryDayAt);
        }

        [TestMethod]
        public void ExistingSchedule_IsNotOverwrittenByLegacyField()
        {
            var config = new ConfigDTO
            {
                FullSyncSchedule = new List<FullSyncScheduleEntry>
                {
                    new FullSyncScheduleEntry { Days = ScheduleDays.Weekdays, Time = "18:00" }
                },
                RunStartupScriptEveryDayAt = "05:30"
            };

            ConfigNormalizer.Normalize(config);

            Assert.AreEqual(1, config.FullSyncSchedule.Count);
            Assert.AreEqual(ScheduleDays.Weekdays, config.FullSyncSchedule[0].Days);
            Assert.AreEqual("18:00", config.FullSyncSchedule[0].Time);
            Assert.IsNull(config.RunStartupScriptEveryDayAt);
        }

        [TestMethod]
        public void EmptySchedule_WithNoLegacyField_StaysEmpty()
        {
            // The user removed every schedule entry: normalization must not resurrect one.
            var config = new ConfigDTO
            {
                FullSyncSchedule = new List<FullSyncScheduleEntry>(),
                RunStartupScriptEveryDayAt = null
            };

            ConfigNormalizer.Normalize(config);

            Assert.AreEqual(0, config.FullSyncSchedule.Count);
        }

        [TestMethod]
        public void IsIdempotent()
        {
            var config = new ConfigDTO { RunStartupScriptEveryDayAt = "05:30" };

            ConfigNormalizer.Normalize(config);
            ConfigNormalizer.Normalize(config);

            Assert.AreEqual(1, config.FullSyncSchedule.Count);
            Assert.IsNull(config.RunStartupScriptEveryDayAt);
        }

        [TestMethod]
        public void NullSchedule_IsInitialized()
        {
            var config = new ConfigDTO { FullSyncSchedule = null, RunStartupScriptEveryDayAt = null };

            ConfigNormalizer.Normalize(config);

            Assert.IsNotNull(config.FullSyncSchedule);
            Assert.AreEqual(0, config.FullSyncSchedule.Count);
        }
    }
}
