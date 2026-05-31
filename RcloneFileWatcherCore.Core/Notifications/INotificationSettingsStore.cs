namespace RcloneFileWatcherCore.Notifications
{
    /// <summary>Loads/saves <see cref="NotificationSettings"/> (notifications.json), handling
    /// at-rest encryption of the SMTP password.</summary>
    public interface INotificationSettingsStore
    {
        /// <summary>Current settings, with the SMTP password decrypted in memory.</summary>
        NotificationSettings Current { get; }

        /// <summary>Persist the given settings (encrypts the SMTP password) and make them current.</summary>
        void Save(NotificationSettings settings);
    }
}
