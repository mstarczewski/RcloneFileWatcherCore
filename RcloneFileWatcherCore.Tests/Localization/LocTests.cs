using Microsoft.VisualStudio.TestTools.UnitTesting;
using RcloneFileWatcherCore.Web.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace RcloneFileWatcherCore.Tests.Localization
{
    [TestClass]
    public class LocTests
    {
        [TestMethod]
        public void MissingPath_UsesEnglishBaseline()
        {
            var loc = new Loc(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            Assert.AreEqual("Dashboard", loc["nav.dashboard"]);
        }

        [TestMethod]
        public void UnknownKey_ReturnsKeyItself()
        {
            var loc = new Loc(null);
            Assert.AreEqual("does.not.exist", loc["does.not.exist"]);
        }

        [TestMethod]
        public void DiscoveredLanguage_IsUsed_WithEnglishFallbackPerKey()
        {
            var dir = Directory.CreateTempSubdirectory().FullName;
            try
            {
                File.WriteAllText(Path.Combine(dir, "pl.json"), "{ \"nav.dashboard\": \"Pulpit\" }");
                var loc = new Loc(dir);

                Assert.IsTrue(loc.Languages.Any(l => l.Code == "en"));
                Assert.IsTrue(loc.Languages.Any(l => l.Code == "pl"));

                var prev = CultureInfo.CurrentUICulture;
                try
                {
                    CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("pl");
                    Assert.AreEqual("Pulpit", loc["nav.dashboard"]);     // from pl.json
                    Assert.AreEqual("Configuration", loc["nav.config"]); // missing in pl.json → English
                }
                finally { CultureInfo.CurrentUICulture = prev; }
            }
            finally { Directory.Delete(dir, recursive: true); }
        }

        [TestMethod]
        public void MalformedLocaleFile_IsSkipped_NotFatal()
        {
            var dir = Directory.CreateTempSubdirectory().FullName;
            try
            {
                File.WriteAllText(Path.Combine(dir, "xx.json"), "{ broken ");
                var loc = new Loc(dir); // must not throw
                Assert.AreEqual("Dashboard", loc["nav.dashboard"]);
            }
            finally { Directory.Delete(dir, recursive: true); }
        }

        [TestMethod]
        public void CommittedLocales_CoverEveryEnglishKey()
        {
            var localesDir = FindLocalesDir();
            if (localesDir == null)
            {
                Assert.Inconclusive("Could not locate the Web locales folder from the test output.");
                return;
            }

            foreach (var file in Directory.EnumerateFiles(localesDir, "*.json"))
            {
                var keys = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(file)).Keys;
                var missing = Loc.Keys.Except(keys).ToList();
                Assert.AreEqual(0, missing.Count,
                    $"{Path.GetFileName(file)} is missing keys: {string.Join(", ", missing)}");
            }
        }

        private static string FindLocalesDir()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "RcloneFileWatcherCore.Web", "locales");
                if (Directory.Exists(candidate))
                    return candidate;
                dir = dir.Parent;
            }
            return null;
        }
    }
}
