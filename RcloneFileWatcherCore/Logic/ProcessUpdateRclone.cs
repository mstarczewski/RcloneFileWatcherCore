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
        private readonly Process _process;
        private const string yoursVersion = "yours:";
        private const string latestVersion = "latest:";
        private const string rcloneFileNameToUpdate = "rclone";
        private const int charsForVersion = 20;
        private const string rCloneSelfUpdateVersion = "1.55";
        private const string rCloneversionChceckArgument = "version --check";
        private const string rCloneselfUpdateArgument = "selfupdate";
        private const string rCloneSuccesUpdate = "Successfully updated";
        public ProcessUpdateRclone(ILogger logger, Process process)
        {
            _logger = logger;
            _process = process ?? new Process();
        }
        public ProcessUpdateRclone(ILogger logger) : this(logger, null) { }
        public bool Start(ConfigDTO configDTO)
        {
            string pathToRclone = configDTO.UpdateRclone.RclonePath;
            try
            {
                if (IsNewVersionAvailable(pathToRclone))
                {
                    if (IsSelfUpdateVersionRunning(pathToRclone, IsVersionWithSelfUpdate))
                    {
                        return (ExecuteProc(pathToRclone, rCloneselfUpdateArgument, SelfUpdateExecutionChceck) == true
                                && new Func<bool>(() =>
                                {
                                    _logger.Write($"Rclone updated");
                                    return true;
                                })());
                    }
                    else
                    {
                        string rcloneWebsiteCurrentVersionAddress = configDTO.UpdateRclone.RcloneWebsiteCurrentVersionAddress;
                        return OldUpdater(rcloneWebsiteCurrentVersionAddress, pathToRclone);
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Write(ex.ToString());
                return false;
            }
        }
        private bool OldUpdater(string rcloneWebsiteCurrentVersionAddress, string pathToRclone)
        {
            int filesUpdated = 0;
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
            return filesUpdated > 0;
        }
        private bool IsSelfUpdateVersionRunning(string pathToRclone, Func<string, bool> funcProcHelper = null)
        {
            return ExecuteProc(pathToRclone, rCloneversionChceckArgument, IsVersionWithSelfUpdate);
        }
        private bool IsNewVersionAvailable(string pathToRclone, Func<string, bool> funcProcHelper = null)
        {
            return ExecuteProc(pathToRclone, rCloneversionChceckArgument, CompareVersions);
        }
        public bool ExecuteProc(string pathToRclone, string argument, Func<string, bool> func)
        {
            var startInfo = PrepareProc(pathToRclone, argument);
            _process.StartInfo = startInfo;
            try
            {
                string dataReceivedRclone = string.Empty;
                string errorReceived = string.Empty;
                DataReceivedEventHandler handlerOutput = (s, e) => dataReceivedRclone += e.Data;
                DataReceivedEventHandler handlerError = (s, e) => errorReceived += e.Data;
                _process.OutputDataReceived += handlerOutput;
                _process.ErrorDataReceived += handlerError;
                _process.Start();
                _process.BeginErrorReadLine();
                _process.BeginOutputReadLine();
                _process.WaitForExit();
                _process.OutputDataReceived -= handlerOutput;
                _process.ErrorDataReceived -= handlerError;
                if (!String.IsNullOrWhiteSpace(errorReceived))
                {
                    _logger.Write($"Proc Error DataReceived {errorReceived}");
                    return false;
                }
                return func(dataReceivedRclone);
            }
            catch (Exception ex)
            {
                _logger.Write(ex.ToString());
                return false;
            }
            finally
            {
                _process.CancelErrorRead();
                _process.CancelOutputRead();
                _process.CloseMainWindow();
                _process.Close();

                //if (_process != null)
                //    ((IDisposable)_process).Dispose();
            }
        }
               
        private ProcessStartInfo PrepareProc(string pathToRclone, string arguments)
        {
            return new ProcessStartInfo
            {
                FileName = pathToRclone,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
        }

        private bool CompareVersions(string versionRclone)
        {
            return GetVersion(versionRclone, latestVersion).CompareTo(GetVersion(versionRclone, yoursVersion)) > 0;
        }
        private bool SelfUpdateExecutionChceck(string dataReceived)
        {
            return dataReceived.Contains(rCloneSuccesUpdate);
        }
        private bool IsVersionWithSelfUpdate(string dataReceived)
        {
            return rCloneSelfUpdateVersion.CompareTo(GetVersion(dataReceived, yoursVersion)) <= 0;
        }
        private string GetVersion(string versionRclone, string version)
        {
            return versionRclone.Substring(versionRclone.IndexOf(version) + version.Length, charsForVersion - version.Length).Trim();
        }
    }
}