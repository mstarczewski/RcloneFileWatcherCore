using Microsoft.Extensions.Configuration;
using System;
using ILogger = RcloneFileWatcherCore.Infrastructure.Logging.Interfaces.ILogger;
using LogLevel = RcloneFileWatcherCore.Enums.LogLevel;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace RcloneFileWatcherCore.Web
{
    /// <summary>
    /// Supplies the TLS certificate Kestrel serves when <c>Gui:Urls</c> contains an https endpoint.
    /// Uses an explicit PFX (<c>Gui:CertPath</c> / <c>Gui:CertPassword</c>) when given; otherwise
    /// generates and persists a self-signed certificate next to the app and exports its public part
    /// (<c>gui-cert.crt</c>) so a reverse proxy (e.g. Caddy) can be told to trust it — which lets the
    /// proxy↔backend hop be encrypted too instead of plain HTTP.
    /// </summary>
    public static class HttpsSetup
    {
        public static X509Certificate2 LoadOrCreate(string contentRoot, IConfiguration cfg, ILogger logger)
        {
            var password = string.IsNullOrEmpty(cfg["Gui:CertPassword"]) ? "rclonefilewatcher" : cfg["Gui:CertPassword"];

            var explicitPath = cfg["Gui:CertPath"];
            if (!string.IsNullOrWhiteSpace(explicitPath))
            {
                logger.Log(LogLevel.Information, $"HTTPS: using certificate from {explicitPath}");
                return new X509Certificate2(explicitPath, password);
            }

            var pfxPath = Path.Combine(contentRoot, "gui-cert.pfx");
            var crtPath = Path.Combine(contentRoot, "gui-cert.crt");

            if (File.Exists(pfxPath))
            {
                try
                {
                    var existing = new X509Certificate2(pfxPath, password);
                    if (existing.NotAfter > DateTime.UtcNow)
                    {
                        logger.Log(LogLevel.Information, $"HTTPS: using self-signed certificate {pfxPath} (valid to {existing.NotAfter:yyyy-MM-dd})");
                        return existing;
                    }
                }
                catch (Exception ex)
                {
                    logger.Log(LogLevel.Warning, "HTTPS: existing gui-cert.pfx could not be read; regenerating", ex);
                }
            }

            return CreateSelfSigned(cfg, password, pfxPath, crtPath, logger);
        }

        private static X509Certificate2 CreateSelfSigned(IConfiguration cfg, string password, string pfxPath, string crtPath, ILogger logger)
        {
            using var rsa = RSA.Create(2048);
            var req = new CertificateRequest("CN=RcloneFileWatcher", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            req.CertificateExtensions.Add(new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, critical: false));
            req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
                new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, critical: false)); // serverAuth

            // SANs so a proxy verifying the name (Caddy tls_server_name) can match. Add a custom
            // upstream hostname via Gui:CertHost when the proxy dials something other than localhost.
            var san = new SubjectAlternativeNameBuilder();
            san.AddDnsName("localhost");
            try { var h = Dns.GetHostName(); if (!string.IsNullOrWhiteSpace(h)) san.AddDnsName(h); } catch { }
            var extra = cfg["Gui:CertHost"];
            if (!string.IsNullOrWhiteSpace(extra)) san.AddDnsName(extra);
            san.AddIpAddress(IPAddress.Loopback);
            req.CertificateExtensions.Add(san.Build());

            using var generated = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(10));

            // Round-trip through a PFX so Kestrel reliably gets a usable private key on all platforms.
            var pfxBytes = generated.Export(X509ContentType.Pfx, password);
            try
            {
                File.WriteAllBytes(pfxPath, pfxBytes);
                var pem = "-----BEGIN CERTIFICATE-----\n"
                    + Convert.ToBase64String(generated.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks)
                    + "\n-----END CERTIFICATE-----\n";
                File.WriteAllText(crtPath, pem);
                logger.Log(LogLevel.Information,
                    $"HTTPS: generated self-signed certificate -> {pfxPath}. Public cert for the reverse proxy: {crtPath} " +
                    $"(Caddy: reverse_proxy https://host:port {{ transport http {{ tls_trusted_ca_certs {crtPath} }} }}).");
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Warning, "HTTPS: could not persist the self-signed certificate; using it in-memory for this run", ex);
            }

            return new X509Certificate2(pfxBytes, password);
        }
    }
}
