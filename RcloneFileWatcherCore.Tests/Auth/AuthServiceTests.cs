using Microsoft.VisualStudio.TestTools.UnitTesting;
using RcloneFileWatcherCore.Web.Auth;
using System.IO;

namespace RcloneFileWatcherCore.Tests.Auth
{
    [TestClass]
    public class AuthServiceTests
    {
        private string _path;

        [TestInitialize]
        public void Setup() => _path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        [TestCleanup]
        public void Cleanup()
        {
            if (File.Exists(_path))
                File.Delete(_path);
        }

        [TestMethod]
        public void NoFile_DefaultsToOpenAccess()
        {
            var auth = new AuthService(_path);

            Assert.IsFalse(auth.Enabled);
            Assert.IsFalse(auth.HasPassword);
            Assert.IsFalse(auth.Verify("anything"));
        }

        [TestMethod]
        public void SetPassword_ThenVerify_AcceptsOnlyTheCorrectPassword()
        {
            var auth = new AuthService(_path);
            auth.SetPassword("s3cret");

            Assert.IsTrue(auth.HasPassword);
            Assert.IsTrue(auth.Verify("s3cret"));
            Assert.IsFalse(auth.Verify("wrong"));
            Assert.IsFalse(auth.Verify(""));
            Assert.IsFalse(auth.Verify(null));
        }

        [TestMethod]
        public void State_PersistsAcrossInstances()
        {
            var a = new AuthService(_path);
            a.SetPassword("pw");
            a.SetEnabled(true);

            var b = new AuthService(_path);

            Assert.IsTrue(b.Enabled);
            Assert.IsTrue(b.HasPassword);
            Assert.IsTrue(b.Verify("pw"));
            Assert.IsFalse(b.Verify("nope"));
        }

        [TestMethod]
        public void CorruptFile_FallsBackToDefaults()
        {
            File.WriteAllText(_path, "{ this is not valid json ");

            var auth = new AuthService(_path);

            Assert.IsFalse(auth.Enabled);
            Assert.IsFalse(auth.HasPassword);
        }

        [TestMethod]
        public void EachHash_UsesAUniqueSalt()
        {
            var auth = new AuthService(_path);

            auth.SetPassword("same");
            var first = File.ReadAllText(_path);
            auth.SetPassword("same");
            var second = File.ReadAllText(_path);

            // Same password, different salt → different stored hash, but both still verify.
            Assert.AreNotEqual(first, second);
            Assert.IsTrue(auth.Verify("same"));
        }
    }
}
