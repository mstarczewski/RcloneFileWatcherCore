namespace RcloneFileWatcherCore.Web.Auth
{
    public interface IAuthService
    {
        /// <summary>Whether a password is required to access the GUI.</summary>
        bool Enabled { get; }

        /// <summary>Whether a password hash has been set.</summary>
        bool HasPassword { get; }

        /// <summary>Constant-time check of a candidate password against the stored hash.</summary>
        bool Verify(string password);

        /// <summary>Hashes and stores a new password.</summary>
        void SetPassword(string password);

        /// <summary>Enables/disables the password requirement (persisted).</summary>
        void SetEnabled(bool enabled);
    }
}
