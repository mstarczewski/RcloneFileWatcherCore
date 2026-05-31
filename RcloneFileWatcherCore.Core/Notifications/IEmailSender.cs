using System.Threading;
using System.Threading.Tasks;

namespace RcloneFileWatcherCore.Notifications
{
    /// <summary>Sends one email to one recipient, encrypting the body with OpenPGP when the
    /// recipient is configured for it.</summary>
    public interface IEmailSender
    {
        Task SendAsync(SmtpSettings smtp, NotificationRecipient recipient, string subject, string body, CancellationToken ct = default);
    }
}
