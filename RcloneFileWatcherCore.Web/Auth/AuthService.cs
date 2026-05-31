using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace RcloneFileWatcherCore.Web.Auth
{
    /// <summary>
    /// GUI access control. The on/off flag and the salted PBKDF2 password hash live in a small
    /// JSON file (gui-auth.json) so the password is never stored in plain text and the toggle
    /// survives restarts. With no file, access is open by default until a password is set in
    /// the Security page.
    /// </summary>
    public class AuthService : IAuthService
    {
        private const int Iterations = 100_000;
        private const int SaltSize = 16;
        private const int HashSize = 32;

        private readonly string _filePath;
        private readonly object _lock = new object();
        private State _state;

        public AuthService(string filePath)
        {
            _filePath = filePath;
            _state = Load() ?? new State();
        }

        public bool Enabled
        {
            get { lock (_lock) return _state.Enabled; }
        }

        public bool HasPassword
        {
            get { lock (_lock) return !string.IsNullOrEmpty(_state.PasswordHash); }
        }

        public bool Verify(string password)
        {
            if (string.IsNullOrEmpty(password))
                return false;

            // Snapshot the hash under the lock so a concurrent SetPassword can't be observed mid-update.
            string hash;
            lock (_lock)
                hash = _state.PasswordHash;

            return !string.IsNullOrEmpty(hash) && VerifyHash(password, hash);
        }

        public void SetPassword(string password)
        {
            lock (_lock)
            {
                _state.PasswordHash = Hash(password);
                Save();
            }
        }

        public void SetEnabled(bool enabled)
        {
            lock (_lock)
            {
                _state.Enabled = enabled;
                Save();
            }
        }

        private State Load()
        {
            try
            {
                if (!File.Exists(_filePath))
                    return null;
                return JsonSerializer.Deserialize<State>(File.ReadAllText(_filePath));
            }
            catch
            {
                return null;
            }
        }

        private void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(_state, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
            catch
            {
                // Persisting is best-effort; in-memory state still applies for this run.
            }
        }

        private static string Hash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
            return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        private static bool VerifyHash(string password, string stored)
        {
            try
            {
                var parts = stored.Split('.');
                if (parts.Length != 3)
                    return false;
                var iterations = int.Parse(parts[0]);
                var salt = Convert.FromBase64String(parts[1]);
                var expected = Convert.FromBase64String(parts[2]);
                var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
                return CryptographicOperations.FixedTimeEquals(actual, expected);
            }
            catch
            {
                return false;
            }
        }

        private sealed class State
        {
            public bool Enabled { get; set; }
            public string PasswordHash { get; set; }
        }
    }
}
