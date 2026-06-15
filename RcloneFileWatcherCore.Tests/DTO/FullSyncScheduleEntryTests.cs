using Microsoft.VisualStudio.TestTools.UnitTesting;
using RcloneFileWatcherCore.DTO;
using System;

namespace RcloneFileWatcherCore.Tests.DTO
{
    [TestClass]
    public class FullSyncScheduleEntryTests
    {
        [DataTestMethod]
        [DataRow("06:00", 6, 0)]
        [DataRow("6:00", 6, 0)]
        [DataRow("18:30", 18, 30)]
        [DataRow("00:00", 0, 0)]
        [DataRow("23:59", 23, 59)]
        public void TryParseTime_AcceptsValid24hTimes(string text, int h, int m)
        {
            Assert.IsTrue(FullSyncScheduleEntry.TryParseTime(text, out var tod));
            Assert.AreEqual(new TimeSpan(h, m, 0), tod);
        }

        [DataTestMethod]
        [DataRow("5")]        // bare integer - TimeSpan.TryParse would read this as 5 DAYS
        [DataRow("24:00")]    // hour out of range
        [DataRow("06:60")]    // minute out of range
        [DataRow("6.00")]
        [DataRow("not-a-time")]
        [DataRow("")]
        [DataRow(null)]
        public void TryParseTime_RejectsInvalidOrAmbiguous(string text)
        {
            Assert.IsFalse(FullSyncScheduleEntry.TryParseTime(text, out _));
        }
    }
}
