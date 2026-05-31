using Microsoft.VisualStudio.TestTools.UnitTesting;
using RcloneFileWatcherCore.Infrastructure.Logging;
using System.Linq;

namespace RcloneFileWatcherCore.Tests.Infrastructure
{
    [TestClass]
    public class BroadcastLogWriterTests
    {
        [TestMethod]
        public void IsErrorLine_DetectsErrorAndCriticalOnly()
        {
            Assert.IsTrue(BroadcastLogWriter.IsErrorLine("2026-05-31 10:00:00 [Error] boom"));
            Assert.IsTrue(BroadcastLogWriter.IsErrorLine("2026-05-31 10:00:00 [Critical] boom"));
            Assert.IsFalse(BroadcastLogWriter.IsErrorLine("2026-05-31 10:00:00 [Information] ok"));
            Assert.IsFalse(BroadcastLogWriter.IsErrorLine("no brackets here"));
            Assert.IsFalse(BroadcastLogWriter.IsErrorLine(null));
        }

        [TestMethod]
        public void Errors_SurviveRollingEviction_UntilCleared()
        {
            var writer = new BroadcastLogWriter(capacity: 3, errorCapacity: 100);

            writer.Write("2026-05-31 10:00:00 [Error] first problem");
            for (var i = 0; i < 10; i++)
                writer.Write($"2026-05-31 10:00:0{i % 10} [Information] line {i}");

            // The rolling buffer only kept the last 3 lines, so the error scrolled off it…
            Assert.AreEqual(3, writer.GetRecent().Count);
            Assert.IsFalse(writer.GetRecent().Any(l => l.Contains("first problem")));

            // …but it is retained in the dedicated error buffer.
            Assert.AreEqual(1, writer.GetErrors().Count);
            Assert.IsTrue(writer.GetErrors()[0].Contains("first problem"));

            writer.ClearErrors();
            Assert.AreEqual(0, writer.GetErrors().Count);
        }

        [TestMethod]
        public void ErrorBuffer_IsBounded()
        {
            var writer = new BroadcastLogWriter(capacity: 10, errorCapacity: 2);

            writer.Write("t [Error] one");
            writer.Write("t [Error] two");
            writer.Write("t [Error] three");

            var errors = writer.GetErrors();
            Assert.AreEqual(2, errors.Count);
            Assert.IsTrue(errors[0].Contains("two"));   // oldest ("one") evicted
            Assert.IsTrue(errors[1].Contains("three"));
        }
    }
}
