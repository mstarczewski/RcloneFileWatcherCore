using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RcloneFileWatcherCore.Notifications
{
    /// <summary>How the SMTP connection is secured.</summary>
    public enum SmtpSecurity
    {
        None = 0,
        StartTls = 1,
        SslOnConnect = 2,
        /// <summary>Let the client choose by port (SSL/TLS on 465, STARTTLS otherwise).</summary>
        Auto = 3
    }

    /// <summary>
    /// Email-on-error settings, persisted (separately from the main config) in notifications.json.
    /// The SMTP password is stored encrypted at rest (Data Protection) — only the protected blob
    /// is written; the plaintext lives in memory at run time.
    /// </summary>
    public class NotificationSettings
    {
        /// <summary>Master switch for error email notifications.</summary>
        public bool Enabled { get; set; }

        /// <summary>How long to wait after the first error before sending, so a burst of errors is
        /// collected into a single email. Seconds.</summary>
        public int DelaySeconds { get; set; } = 60;

        public SmtpSettings Smtp { get; set; } = new SmtpSettings();

        public List<NotificationRecipient> Recipients { get; set; } = new List<NotificationRecipient>();
    }

    public class SmtpSettings
    {
        public string Host { get; set; }
        public int Port { get; set; } = 587;
        public SmtpSecurity Security { get; set; } = SmtpSecurity.Auto;
        public string User { get; set; }
        public string From { get; set; }

        /// <summary>Plaintext password — only in memory, never serialized.</summary>
        [JsonIgnore]
        public string Password { get; set; }

        /// <summary>Data-Protection-encrypted password as persisted in notifications.json.</summary>
        public string PasswordProtected { get; set; }
    }

    public class NotificationRecipient
    {
        public string Email { get; set; }

        /// <summary>Encrypt the email body to this recipient with OpenPGP.</summary>
        public bool Encrypt { get; set; }

        /// <summary>The recipient's ASCII-armored OpenPGP public key (fetched or pasted).</summary>
        public string PublicKey { get; set; }
    }
}
