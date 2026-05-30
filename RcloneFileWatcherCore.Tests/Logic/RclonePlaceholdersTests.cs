using Microsoft.VisualStudio.TestTools.UnitTesting;
using RcloneFileWatcherCore.Logic.Rclone;
using System;

namespace RcloneFileWatcherCore.Tests.Logic
{
    [TestClass]
    public class RclonePlaceholdersTests
    {
        private static readonly DateTime Now = new DateTime(2026, 5, 30, 9, 8, 7);

        [TestMethod]
        public void Expand_ReplacesDatetimeAndYear()
        {
            var result = RclonePlaceholders.Expand("--suffix \" [{datetime}]\"", Now);
            Assert.AreEqual("--suffix \" [2026-05-30-09-08-07]\"", result);
        }

        [TestMethod]
        public void Expand_ReplacesYearAndDate()
        {
            Assert.AreEqual("remote:Archive/2026", RclonePlaceholders.Expand("remote:Archive/{year}", Now));
            Assert.AreEqual("log-2026-05-30.txt", RclonePlaceholders.Expand("log-{date}.txt", Now));
        }

        [TestMethod]
        public void Expand_LeavesPlainValuesUnchanged()
        {
            Assert.AreEqual("remote:backup", RclonePlaceholders.Expand("remote:backup", Now));
        }
    }
}
