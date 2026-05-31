using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using PgpCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RcloneFileWatcherCore.Notifications
{
    /// <summary>SMTP sender (MailKit) with optional per-recipient OpenPGP body encryption (PgpCore).</summary>
    public class MailKitEmailSender : IEmailSender
    {
        public async Task SendAsync(SmtpSettings smtp, NotificationRecipient recipient, string subject, string body, CancellationToken ct = default)
        {
            if (smtp == null || string.IsNullOrWhiteSpace(smtp.Host))
                throw new InvalidOperationException("SMTP host is not configured.");
            if (recipient == null || string.IsNullOrWhiteSpace(recipient.Email))
                throw new InvalidOperationException("Recipient email is not set.");

            var finalBody = body;
            if (recipient.Encrypt)
            {
                if (string.IsNullOrWhiteSpace(recipient.PublicKey))
                    throw new InvalidOperationException($"Encryption is enabled for {recipient.Email} but no public key is set.");

                var keys = new EncryptionKeys(recipient.PublicKey);
                var pgp = new PGP(keys);
                finalBody = await pgp.EncryptAsync(body);
            }

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(string.IsNullOrWhiteSpace(smtp.From) ? smtp.User : smtp.From));
            message.To.Add(MailboxAddress.Parse(recipient.Email));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = finalBody };

            var secure = smtp.Security switch
            {
                SmtpSecurity.SslOnConnect => SecureSocketOptions.SslOnConnect,
                SmtpSecurity.StartTls => SecureSocketOptions.StartTls,
                SmtpSecurity.Auto => SecureSocketOptions.Auto,   // SSL on 465, STARTTLS otherwise
                _ => SecureSocketOptions.None
            };

            using var client = new SmtpClient { Timeout = 30000 }; // fail fast (30s) instead of hanging
            await client.ConnectAsync(smtp.Host, smtp.Port, secure, ct);
            if (!string.IsNullOrWhiteSpace(smtp.User))
                await client.AuthenticateAsync(smtp.User, smtp.Password ?? string.Empty, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);
        }
    }
}
