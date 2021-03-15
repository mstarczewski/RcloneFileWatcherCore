using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Logic.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace RcloneFileWatcherCore.Logic
{
    class ProcessUpdateRclone : IProcess
    {
        private readonly ILogger _logger;
        private const string yoursVersion = "yours:";
        private const string latestVersion = "latest:";
        private const string rcloneFileNameToUpdate = "rclone";
        private const int charsForVersion = 20;
        public ProcessUpdateRclone(ILogger logger)
        {
            _logger = logger;
        }
        public bool Start(ConfigDTO configDTO)
        {
            string rcloneWebsiteCurrentVersionAddress = configDTO.UpdateRclone.RcloneWebsiteCurrentVersionAddress;
            string pathToRclone = configDTO.UpdateRclone.RclonePath;
            try
            {
                int filesUpdated = 0;
                if (IsNewVersion(pathToRclone))
                {
                    using (WebClient webClient = new WebClient())
                    {
                        using (MemoryStream stream = new MemoryStream(webClient.DownloadData(rcloneWebsiteCurrentVersionAddress)))
                        {
                            ZipArchive archive = new ZipArchive(stream);
                            foreach (ZipArchiveEntry entry in archive.Entries)
                            {

                                if (!String.IsNullOrEmpty(Path.GetFileName(entry.Name)) && Path.GetFileName(entry.Name).Contains(rcloneFileNameToUpdate, StringComparison.OrdinalIgnoreCase))
                                {
                                    var pathToUnzip = Path.Combine(Path.GetDirectoryName(pathToRclone), entry.Name);
                                    entry.ExtractToFile(pathToUnzip, true);
                                    filesUpdated++;
                                    _logger.Write($"Rclone updated: {entry.Name}");
                                }
                            }
                        }
                    }
                }
                return filesUpdated > 0;
            }
            catch (Exception ex)
            {
                _logger.Write(ex.ToString());
                return false;
            }

        }
        private bool IsNewVersion(string pathToRclone)
        {
            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = pathToRclone,
                    Arguments = "version --check",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            try
            {
                string versionRclone = string.Empty;
                string errorVersionChceck = string.Empty;
                proc.OutputDataReceived += (s, e) => versionRclone += e.Data;
                proc.ErrorDataReceived += (s, e) => errorVersionChceck += e.Data;
                proc.Start();
                proc.BeginErrorReadLine();
                proc.BeginOutputReadLine();
                proc.WaitForExit();
                if (!String.IsNullOrWhiteSpace(errorVersionChceck))
                {
                    _logger.Write($"Update ErrorDataReceived {errorVersionChceck}");
                }
                return CompareVersions(versionRclone);
            }
            catch (Exception ex)
            {
                _logger.Write(ex.ToString());
                return false;
            }
            finally
            {
                proc.CloseMainWindow();
                proc.Close();
                if (proc != null)
                    ((IDisposable)proc).Dispose();
            }
        }

        private bool CompareVersions(string versionRclone)
        {
            return GetVersion(versionRclone, latestVersion).CompareTo(GetVersion(versionRclone, yoursVersion)) > 0;
        }

        private static string GetVersion(string versionRclone, string version)
        {
            return versionRclone.Substring(versionRclone.IndexOf(version) + version.Length, charsForVersion - version.Length).Trim();
        }
    }
}