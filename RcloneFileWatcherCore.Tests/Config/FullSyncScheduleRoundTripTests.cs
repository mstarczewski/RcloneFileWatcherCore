using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RcloneFileWatcherCore.Config;
using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Enums;
using RcloneFileWatcherCore.Infrastructure.Logging.Interfaces;
using System.Collections.Generic;
using System.IO;

namespace RcloneFileWatcherCore.Tests.Config
{
    [TestClass]
    public class FullSyncScheduleRoundTripTests
    {
        [TestMethod]
        public void Schedule_SurvivesWriteThenLoad_WithFlagsAsExpected()
        {
            var file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                var saved = new ConfigDTO
                {
                    FullSyncSchedule = new List<FullSyncScheduleEntry>
                    {
                        new FullSyncScheduleEntry { Days = ScheduleDays.Weekdays, Time = "06:00" },
                        new FullSyncScheduleEntry { Days = ScheduleDays.Weekend, Time = "03:00" }
                    }
                };

                ConfigWriter.Save(file, saved);
                var loaded = new ConfigLoader(file, Mock.Of<ILogger>()).LoadConfig();

                Assert.IsNotNull(loaded);
                Assert.AreEqual(2, loaded.FullSyncSchedule.Count);
                Assert.AreEqual(ScheduleDays.Weekdays, loaded.FullSyncSchedule[0].Days);
                Assert.AreEqual("06:00", loaded.FullSyncSchedule[0].Time);
                Assert.AreEqual(ScheduleDays.Weekend, loaded.FullSyncSchedule[1].Days);
                Assert.AreEqual("03:00", loaded.FullSyncSchedule[1].Time);
            }
            finally
            {
                if (File.Exists(file)) File.Delete(file);
            }
        }

        [TestMethod]
        public void LegacyConfigFile_IsMigratedOnLoad()
        {
            // An old config file that predates FullSyncSchedule: only the single daily time.
            var file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                File.WriteAllText(file, "{ \"RunStartupScriptEveryDayAt\": \"05:30\" }");

                var loaded = new ConfigLoader(file, Mock.Of<ILogger>()).LoadConfig();

                Assert.IsNotNull(loaded);
                Assert.AreEqual(1, loaded.FullSyncSchedule.Count);
                Assert.AreEqual(ScheduleDays.EveryDay, loaded.FullSyncSchedule[0].Days);
                Assert.AreEqual("05:30", loaded.FullSyncSchedule[0].Time);
                Assert.IsNull(loaded.RunStartupScriptEveryDayAt);
            }
            finally
            {
                if (File.Exists(file)) File.Delete(file);
            }
        }
    }
}
