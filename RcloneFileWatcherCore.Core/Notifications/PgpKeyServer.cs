using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace RcloneFileWatcherCore.Notifications
{
    /// <summary>Fetches an OpenPGP public key from the keys.openpgp.org keyserver by email address.</summary>
    public class PgpKeyServer
    {
        private const string BaseUrl = "https://keys.openpgp.org/vks/v1/by-email/";

        /// <summary>Returns the recipient's ASCII-armored public key, or null if the keyserver has
        /// none for that address.</summary>
        public async Task<string> FetchByEmailAsync(string email, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            var resp = await http.GetAsync(BaseUrl + Uri.EscapeDataString(email.Trim()), ct);
            if (resp.StatusCode == HttpStatusCode.NotFound)
                return null;
            resp.EnsureSuccessStatusCode();

            var key = await resp.Content.ReadAsStringAsync(ct);
            return string.IsNullOrWhiteSpace(key) ? null : key.Trim();
        }
    }
}
