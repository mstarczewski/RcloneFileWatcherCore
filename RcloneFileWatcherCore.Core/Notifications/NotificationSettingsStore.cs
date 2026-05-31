using Microsoft.AspNetCore.DataProtection;
using RcloneFileWatcherCore.Enums;
using RcloneFileWatcherCore.Infrastructure.Logging.Interfaces;
using System;
using System.IO;
using System.Text.Json;

namespace RcloneFileWatcherCore.Notifications
{
    /// <summary>
    /// Persists notification settings to a JSON file. The SMTP password is encrypted at rest with
    /// ASP.NET Core Data Protection (key ring kept in a local folder), so it is not stored in clear
    /// text and the background process can still decrypt it without anyone being logged in.
    /// </summary>
    public class NotificationSettingsStore : INotificationSettingsStore
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { WriteIndented = true };

        private readonly string _filePath;
        private readonly IDataProtector _protector;
        private readonly ILogger _logger;
        private readonly object _lock = new object();
        private NotificationSettings _current;

        public NotificationSettingsStore(string filePath, string keysDirectory, ILogger logger)
        {
            _filePath = filePath;
            _logger = logger;
            Directory.CreateDirectory(keysDirectory);
            _protector = DataProtectionProvider
                .Create(new DirectoryInfo(keysDirectory))
                .CreateProtector("RcloneFileWatcher.Notifications.Smtp.v1");
            _current = Load();
        }

        public NotificationSettings Current
        {
            get { lock (_lock) return _current; }
        }

        public void Save(NotificationSettings settings)
        {
            lock (_lock)
            {
                settings.Smtp ??= new SmtpSettings();
                // Encrypt the plaintext password into the persisted blob; never write it in clear.
                settings.Smtp.PasswordProtected = Protect(settings.Smtp.Password);

                var json = JsonSerializer.Serialize(settings, Options);
                var tempPath = _filePath + ".tmp";
                File.WriteAllText(tempPath, json);
                if (File.Exists(_filePath))
                    File.Replace(tempPath, _filePath, null);
                else
                    File.Move(tempPath, _filePath);

                _current = settings; // keep the in-memory copy (with plaintext password) current
            }
        }

        private NotificationSettings Load()
        {
            try
            {
                if (!File.Exists(_filePath))
                    return new NotificationSettings();

                var settings = JsonSerializer.Deserialize<NotificationSettings>(File.ReadAllText(_filePath))
                               ?? new NotificationSettings();
                settings.Smtp ??= new SmtpSettings();
                settings.Smtp.Password = Unprotect(settings.Smtp.PasswordProtected);
                return settings;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "Could not read notifications.json; using defaults", ex);
                return new NotificationSettings();
            }
        }

        private string Protect(string plaintext)
        {
            if (string.IsNullOrEmpty(plaintext))
                return null;
            try { return _protector.Protect(plaintext); }
            catch (Exception ex) { _logger.Log(LogLevel.Error, "Failed to encrypt SMTP password", ex); return null; }
        }

        private string Unprotect(string protectedValue)
        {
            if (string.IsNullOrEmpty(protectedValue))
                return null;
            try { return _protector.Unprotect(protectedValue); }
            catch (Exception ex)
            {
                // Most likely the key ring changed/moved — the password must be re-entered in the GUI.
                _logger.Log(LogLevel.Warning, "Could not decrypt the stored SMTP password (key ring changed?); re-enter it in the GUI", ex);
                return null;
            }
        }
    }
}
