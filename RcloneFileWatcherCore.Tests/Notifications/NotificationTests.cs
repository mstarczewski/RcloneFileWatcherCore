using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RcloneFileWatcherCore.Infrastructure.Logging;
using RcloneFileWatcherCore.Infrastructure.Logging.Interfaces;
using RcloneFileWatcherCore.Notifications;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RcloneFileWatcherCore.Tests.Notifications
{
    [TestClass]
    public class NotificationTests
    {
        private static ILogger Logger() => new Mock<ILogger>().Object;

        [TestMethod]
        public void SettingsStore_RoundTrips_AndEncryptsPasswordAtRest()
        {
            var dir = Directory.CreateTempSubdirectory().FullName;
            try
            {
                var file = Path.Combine(dir, "notifications.json");
                var keys = Path.Combine(dir, "keys");

                var store = new NotificationSettingsStore(file, keys, Logger());
                store.Save(new NotificationSettings
                {
                    Enabled = true,
                    Smtp = new SmtpSettings { Host = "smtp.example.com", User = "u", Password = "s3cretPw", From = "a@b.c" },
                    Recipients = new List<NotificationRecipient> { new NotificationRecipient { Email = "ops@b.c" } }
                });

                // The password must not be on disk in clear text.
                var raw = File.ReadAllText(file);
                Assert.IsFalse(raw.Contains("s3cretPw"), "plaintext password leaked to disk");

                // A fresh store (same key ring) decrypts it back.
                var reloaded = new NotificationSettingsStore(file, keys, Logger());
                Assert.IsTrue(reloaded.Current.Enabled);
                Assert.AreEqual("s3cretPw", reloaded.Current.Smtp.Password);
                Assert.AreEqual("smtp.example.com", reloaded.Current.Smtp.Host);
            }
            finally { Directory.Delete(dir, recursive: true); }
        }

        [TestMethod]
        public async Task Notifier_SendsBatchedErrors_ButIgnoresNonErrorsAndWhenDisabled()
        {
            var broadcast = new BroadcastLogWriter();
            var sender = new FakeSender();
            var store = new FakeStore
            {
                Current = new NotificationSettings
                {
                    Enabled = true,
                    DelaySeconds = 0,
                    Recipients = new List<NotificationRecipient> { new NotificationRecipient { Email = "ops@b.c" } }
                }
            };
            var notifier = new ErrorMailNotifier(broadcast, store, sender, Logger());
            await notifier.StartAsync(default);

            // Non-error line → nothing sent.
            broadcast.Write("2026-01-01 00:00:00 [Information] all good");
            Assert.IsFalse(sender.Sent.Wait(400), "should not email for an Information line");

            // Error line → one email with the error text.
            broadcast.Write("2026-01-01 00:00:01 [Error] disk on fire");
            Assert.IsTrue(sender.Sent.Wait(5000), "should email for an Error line");
            StringAssert.Contains(sender.LastBody, "disk on fire");

            // Disabled → nothing sent.
            sender.Reset();
            store.Current.Enabled = false;
            broadcast.Write("2026-01-01 00:00:02 [Error] again");
            Assert.IsFalse(sender.Sent.Wait(400), "should not email when disabled");

            await notifier.StopAsync(default);
        }

        private sealed class FakeSender : IEmailSender
        {
            public readonly ManualResetEventSlim Sent = new ManualResetEventSlim(false);
            public string LastBody;

            public Task SendAsync(SmtpSettings smtp, NotificationRecipient recipient, string subject, string body, CancellationToken ct = default)
            {
                LastBody = body;
                Sent.Set();
                return Task.CompletedTask;
            }

            public void Reset() { LastBody = null; Sent.Reset(); }
        }

        private sealed class FakeStore : INotificationSettingsStore
        {
            public NotificationSettings Current { get; set; }
            public void Save(NotificationSettings settings) => Current = settings;
        }
    }
}
