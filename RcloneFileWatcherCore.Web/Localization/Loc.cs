using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace RcloneFileWatcherCore.Web.Localization
{
    /// <summary>
    /// Translation lookup keyed by the current UI culture. English is baked in as the always
    /// available default/fallback (the app works even with no translation files). Additional
    /// languages are discovered at startup from JSON files in the locales folder - drop a
    /// <c>pl.json</c> / <c>de.json</c> (or <c>pt-BR.json</c>) and that language appears in the
    /// switcher automatically, no recompilation needed. An <c>en.json</c>, if present, may
    /// override individual baked-in English strings.
    /// </summary>
    public class Loc
    {
        public const string DefaultCulture = "en";

        private readonly Dictionary<string, Dictionary<string, string>> _tables;
        private readonly List<LanguageInfo> _languages;

        public Loc(string localesPath)
        {
            _tables = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                [DefaultCulture] = new Dictionary<string, string>(English)
            };

            LoadFromFiles(localesPath);

            _languages = _tables.Keys
                .Select(LanguageInfo.For)
                .OrderBy(l => l.Code == DefaultCulture ? 0 : 1)
                .ThenBy(l => l.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>Languages available in the switcher (English first, then discovered ones).</summary>
        public IReadOnlyList<LanguageInfo> Languages => _languages;

        public string this[string key]
        {
            get
            {
                foreach (var code in CandidateCultures())
                {
                    if (_tables.TryGetValue(code, out var table) && table.TryGetValue(key, out var value))
                        return value;
                }
                return _tables[DefaultCulture].TryGetValue(key, out var en) ? en : key;
            }
        }

        public string F(string key, params object[] args) => string.Format(this[key], args);

        private static IEnumerable<string> CandidateCultures()
        {
            var ui = CultureInfo.CurrentUICulture;
            yield return ui.Name;                       // e.g. "pt-BR"
            yield return ui.TwoLetterISOLanguageName;   // e.g. "pt"
            yield return DefaultCulture;
        }

        private void LoadFromFiles(string localesPath)
        {
            if (string.IsNullOrWhiteSpace(localesPath) || !Directory.Exists(localesPath))
                return;

            foreach (var file in Directory.EnumerateFiles(localesPath, "*.json"))
            {
                var code = Path.GetFileNameWithoutExtension(file);
                if (string.IsNullOrWhiteSpace(code))
                    continue;

                try
                {
                    var json = File.ReadAllText(file);
                    var entries = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (entries == null)
                        continue;

                    if (!_tables.TryGetValue(code, out var table))
                    {
                        table = new Dictionary<string, string>();
                        _tables[code] = table;
                    }
                    foreach (var kv in entries)
                        table[kv.Key] = kv.Value;
                }
                catch
                {
                    // A malformed translation file must not take the GUI down; skip it.
                }
            }
        }

        // English baseline - the guaranteed fallback. Keys here define the full string set.
        private static readonly Dictionary<string, string> English = new()
        {
            ["nav.dashboard"] = "Dashboard",
            ["nav.config"] = "Configuration",
            ["nav.rclone"] = "Rclone",
            ["nav.logs"] = "Logs",
            ["nav.notifications"] = "Notifications",
            ["nav.logout"] = "Log out",
            ["nav.security"] = "Security",
            ["nav.menu"] = "Menu",
            ["theme.toggle"] = "Light/dark theme",
            ["lang.label"] = "Language",

            ["sec.intro"] = "Control access to this GUI. The password is stored hashed (PBKDF2) in gui-auth.json.",
            ["sec.requirePassword"] = "Require a password to access the GUI",
            ["sec.newPassword"] = "New password",
            ["sec.confirmPassword"] = "Confirm password",
            ["sec.changeHint"] = "Leave blank to keep the current password.",
            ["sec.passwordIsSet"] = "A password is set.",
            ["sec.save"] = "Save",
            ["sec.saved"] = "Security settings saved.",
            ["sec.mismatch"] = "Passwords do not match.",
            ["sec.weakPassword"] = "Password must be at least 8 characters and include a letter and a digit.",
            ["sec.passwordRule"] = "At least 8 characters, including a letter and a digit.",
            ["sec.needPassword"] = "Set a password before enabling the requirement.",
            ["sec.tlsHint"] = "Over a network, put the GUI behind an HTTPS reverse proxy (the password and cookie travel in clear text over HTTP).",

            ["dash.watcher"] = "Watcher",
            ["dash.running"] = "Running",
            ["dash.stopped"] = "Stopped",
            ["dash.queued"] = "Queued changes",
            ["dash.queuePreview"] = "Queue preview",
            ["dash.queueShowing"] = "showing {0} of {1}",
            ["dash.queueEmpty"] = "The queue is empty.",
            ["dash.queueToggleShow"] = "Show queue",
            ["dash.queueToggleHide"] = "Hide queue",
            ["dash.change.created"] = "Created",
            ["dash.change.changed"] = "Changed",
            ["dash.change.deleted"] = "Deleted",
            ["dash.change.renamed"] = "Renamed",
            ["dash.watchedPaths"] = "Watched paths",
            ["dash.startedUtc"] = "Started (UTC)",
            ["dash.watchedDirs"] = "Watched directories",
            ["dash.noPaths"] = "No watched paths configured yet.",
            ["dash.startWatcher"] = "Start watcher",
            ["dash.stopWatcher"] = "Stop watcher",
            ["dash.syncNow"] = "Sync now",
            ["dash.fullSync"] = "Full sync",
            ["dash.loading"] = "Loading status…",
            ["dash.msgStarted"] = "Watcher started.",
            ["dash.msgStopped"] = "Watcher stopped.",
            ["dash.msgStartFailed"] = "Could not start: {0}",
            ["dash.jobRunning"] = "Running… (see Logs)",
            ["dash.jobDone"] = "Done.",
            ["dash.jobFailed"] = "Failed — see Logs.",
            ["dash.dryRunWarning"] = "Dry-run is enabled on at least one managed command — syncs will only report what would happen and will NOT transfer anything. Uncheck --dry-run in Configuration to sync for real.",
            ["dash.disabledWarning"] = "{0} sync(s) are disabled and will not run. Re-enable them in Configuration.",
            ["dash.stopRclone"] = "Stop rclone",
            ["dash.rcloneStopping"] = "Stopping rclone…",
            ["dash.rcloneStopped"] = "rclone stopped.",
            ["dash.rcloneTitle"] = "rclone availability",
            ["dash.rcloneChecking"] = "Checking rclone…",
            ["dash.rcloneNone"] = "No managed rclone path configured (script mode embeds its own path).",
            ["dash.rcloneNotFound"] = "Not found",
            ["dash.rcloneRefresh"] = "Re-check",

            ["cfg.intro"] = "Changes are saved to {0} and applied live (the watcher is reloaded). Changing LogPath requires a restart.",
            ["cfg.general"] = "General",
            ["cfg.logLevel"] = "Log level",
            ["cfg.logLevelHint"] = "None selected = all levels.",
            ["cfg.logPath"] = "Log file path (empty = console)",
            ["cfg.syncInterval"] = "Sync interval (s)",
            ["cfg.fullSyncDaily"] = "Full sync daily at (HH:mm)",
            ["cfg.fullSyncStartup"] = "Full sync at startup",
            ["cfg.fullSyncScript"] = "Full sync script",
            ["cfg.fullSyncTitle"] = "Full sync",
            ["cfg.fullSyncCommands"] = "Full-sync commands ({0})",
            ["cfg.addCommand"] = "+ Add command",
            ["cfg.commandN"] = "Command #{0}",
            ["cfg.noFullSyncCommands"] = "No commands. Add at least one for the full sync.",
            ["cfg.autoUpdate"] = "rclone auto-update",
            ["cfg.enableAutoUpdate"] = "Enable auto-update",
            ["cfg.rclonePath"] = "rclone path",
            ["cfg.checkEvery"] = "Check every (h)",
            ["cfg.watchedPaths"] = "Watched paths ({0})",
            ["cfg.importScripts"] = "Import scripts → managed",
            ["cfg.addPath"] = "+ Add path",
            ["cfg.noPaths"] = "No paths. Add at least one for the watcher to monitor.",
            ["cfg.pathN"] = "Path #{0}",
            ["cfg.remove"] = "Remove",
            ["cfg.enabled"] = "Enabled",
            ["cfg.watchDir"] = "Watched directory",
            ["cfg.includeFromFile"] = "--include-from file",
            ["cfg.runMode"] = "rclone run mode",
            ["cfg.modeScript"] = "Script (.bat/.sh)",
            ["cfg.modeManaged"] = "Managed (command)",
            ["cfg.scriptFile"] = "rclone script (.bat/.sh)",
            ["cfg.importThisScript"] = "Import this script to managed",
            ["cfg.cmdSource"] = "Source",
            ["cfg.cmdDest"] = "Destination (remote:path)",
            ["cfg.cmdCommand"] = "Command",
            ["cfg.includeFromInject"] = "Inject --include-from",
            ["cfg.includeFromStdin"] = "Pass the list via stdin (no --include-from file)",
            ["cfg.extraArgs"] = "Extra arguments (one token per line)",
            ["cfg.preview"] = "Command preview:",
            ["cfg.runtimeVars"] = "Variables substituted at run time:",
            ["cfg.exclude"] = "Exclusions (one per line)",
            ["cfg.save"] = "Save & apply",
            ["cfg.reset"] = "Reset",
            ["cfg.savedApplied"] = "Configuration saved and applied.",
            ["cfg.restored"] = "Current configuration restored.",
            ["cfg.notSaved"] = "Not saved — fix the errors below.",
            ["cfg.saveError"] = "Save error: {0}",
            ["cfg.importedOne"] = "Imported the command from the script for path #{0}. Review the fields and save.",
            ["cfg.scriptNotFound"] = "Script not found: {0}",
            ["cfg.noPathPlaceholder"] = "(no path)",
            ["cfg.importError"] = "Import error: {0}",
            ["cfg.importedFullSync"] = "Imported {0} command(s) from the full-sync script to managed. Review the fields and save.",
            ["cfg.importedFullSyncNone"] = "No rclone command found in the script: {0}",
            ["cfg.importedMany"] = "Imported {0} script(s) to managed. Review the fields and save.",
            ["cfg.importedManySkipped"] = "Imported {0} script(s) to managed, skipped {1} (missing file or error). Review the fields and save.",
            ["cfg.vInterval"] = "Sync interval must be at least 1 second.",
            ["cfg.vTime"] = "Daily full sync time must be in HH:mm format (e.g. 05:30).",
            ["cfg.vStartupScript"] = "Full sync at startup is enabled but no script is set.",
            ["cfg.vFullSyncCommands"] = "Full sync at startup is enabled but no managed commands are configured.",
            ["cfg.vRclonePath"] = "rclone auto-update is enabled but no rclone path is set.",
            ["cfg.vCheckHours"] = "Update check frequency cannot be negative.",
            ["cfg.vWatchDir"] = "Path #{0}: watched directory is required.",
            ["cfg.vIncludeFrom"] = "Path #{0}: --include-from file is required.",

            ["cfg.tabGeneral"] = "General",
            ["cfg.tabFullSync"] = "Full sync",
            ["cfg.tabFullSyncN"] = "Full sync ({0})",
            ["cfg.tabPaths"] = "Watched paths ({0})",

            // Tooltips (the "?" badges).
            ["cfg.logLevel.h"] = "Which severities the app writes to its own log. None selected = log every level.",
            ["cfg.logPath.h"] = "File the app writes its own log to. Empty = log to the console only. Changing this needs a restart.",
            ["cfg.syncInterval.h"] = "How often (seconds) the watcher batches detected changes and runs rclone.",
            ["cfg.collapseDirs"] = "Collapse whole-directory changes",
            ["cfg.collapseDirs.h"] = "When a whole directory is created/renamed/deleted, pass rclone a single 'dir/**' rule instead of every file under it — rclone then walks just that subtree. Helps with bursts of thousands of files. Off by default.",
            ["cfg.quietPeriod"] = "Quiet period (s)",
            ["cfg.quietPeriod.h"] = "Wait until no new change has arrived for this many seconds before syncing, so a long copy is synced once it settles instead of in many partial runs. 0 = off (sync every interval).",
            ["cfg.quietPeriodMax"] = "Quiet period max wait (s)",
            ["cfg.quietPeriodMax.h"] = "Safety cap: sync anyway once the oldest pending change has waited this long, even if changes keep arriving. 0 = no cap.",
            ["cfg.enableAutoUpdate.h"] = "Periodically run 'rclone selfupdate' to keep the rclone binary current.",
            ["cfg.rclonePath.h"] = "Full path to the rclone executable used for the self-update check.",
            ["cfg.checkEvery.h"] = "How often (hours) to check for an rclone update. 0 = only at startup.",
            ["cfg.fullSyncStartup.h"] = "Run a complete sync once when the app starts, in addition to the live and daily syncs.",
            ["cfg.fullSyncDaily.h"] = "Time of day (24h HH:mm) to run a full sync automatically. Empty = no daily full sync.",
            ["cfg.runMode.h"] = "Script: run an external .bat/.sh file. Managed: build the rclone command from the fields below.",
            ["cfg.fullSyncScript.h"] = "Path to the .bat/.sh script executed for the full sync.",
            ["cfg.watchDir.h"] = "Directory watched for changes. Any change here is queued and synced by rclone.",
            ["cfg.includeFromFile.h"] = "File listing changed paths passed to rclone via --include-from. In managed + stdin mode it is sent through stdin instead of being written to disk.",
            ["cfg.scriptFile.h"] = "Path to the .bat/.sh script run for this path's live sync.",
            ["cfg.exclude.h"] = "Substrings; any changed path containing one is ignored (not synced). One per line.",
            ["cfg.cmdCommand.h"] = "rclone subcommand: sync (make destination match source, deletes extras), copy (add/update only), move (copy then delete source).",
            ["cfg.cmdSource.h"] = "Path or remote rclone reads from. For watched paths this is usually the watched directory.",
            ["cfg.cmdDest.h"] = "rclone destination in remote:path form (the configured remote and target folder).",
            ["cfg.h.rclonePath"] = "Path to the rclone executable for this command. Empty = use 'rclone' from PATH.",
            ["cfg.h.config"] = "Path to the rclone config file (rclone.conf) when not in the default location.",
            ["cfg.h.bwlimit"] = "Bandwidth limit, e.g. 15M, or 'upload:download' like 15M:off. Empty = unlimited.",
            ["cfg.h.transfers"] = "Number of files transferred in parallel. 0 = rclone default (4).",
            ["cfg.h.checkers"] = "Number of checks run in parallel when comparing files. 0 = rclone default (8).",
            ["cfg.h.retries"] = "How many times to retry the whole operation on error. 0 = rclone default (3).",
            ["cfg.h.retriesSleep"] = "Wait between retries, e.g. 1m or 10s. Empty = no extra wait.",
            ["cfg.h.backupDir"] = "Move overwritten/deleted files here instead of discarding them. Supports {year} etc.",
            ["cfg.h.suffix"] = "Suffix appended to backed-up files, e.g. ' [{datetime}]'. A leading space is kept.",
            ["cfg.h.logFile"] = "Write rclone's own output to this file. The GUI tails it to show rclone logs live.",
            ["cfg.h.logLevel"] = "rclone log verbosity: ERROR, NOTICE, INFO or DEBUG.",
            ["cfg.includeFromInject.h"] = "Pass the list of changed files to rclone with --include-from so only those are synced.",
            ["cfg.includeFromStdin.h"] = "Send the changed-files list through stdin instead of writing a temporary file. Handles very large lists.",
            ["cfg.h.createEmptySrcDirs"] = "Also create empty source directories on the destination.",
            ["cfg.h.useMmap"] = "Allocate transfer buffers via mmap so memory is returned to the OS promptly. Lowers and stabilises memory use.",
            ["cfg.h.fastList"] = "List the remote recursively in one bulk pass — fewer API calls and often faster on large trees, at the cost of more memory. Mainly helps full syncs.",
            ["cfg.h.update"] = "Skip a file when the destination copy is newer than the source, so a newer remote version is never overwritten. Safe one-way-mirror guard.",
            ["cfg.h.dryRun"] = "Report what would be transferred/deleted without making any changes. Use to test a command before running it for real.",
            ["cfg.extraArgs.h"] = "Any additional rclone flags, one token per line (e.g. --exclude on one line, then its value on the next).",

            ["rcl.intro"] = "Preview of the rclone invocation for each watched path (live sync) and for the full sync. Editing is done under Configuration. In Managed mode the command is built from fields (with automatic --include-from for watched paths); in Script mode the given .bat/.sh file is run.",
            ["rcl.liveSyncTitle"] = "Live sync (watched paths)",
            ["rcl.managed"] = "Managed",
            ["rcl.script"] = "Script",
            ["rcl.noCommand"] = "Managed mode without a configured command.",
            ["rcl.scriptLabel"] = "Script:",
            ["rcl.notSet"] = "— (not set)",
            ["rcl.noPaths"] = "No configured paths.",
            ["rcl.fullSyncEmpty"] = "Managed full sync without any configured commands.",
            ["rcl.enabled"] = "Enabled",
            ["rcl.disabled"] = "Disabled",
            ["rcl.dailyAt"] = "Daily at {0}",
            ["rcl.atStartup"] = "At startup",
            ["rcl.manualOnly"] = "Manual only",

            ["log.level"] = "Level:",
            ["log.all"] = "All",
            ["log.autoScroll"] = "Auto-scroll",
            ["log.entries"] = "{0} / {1} entries",
            ["log.clear"] = "Clear view",
            ["log.download"] = "Download log",
            ["log.noEntries"] = "(no entries)",
            ["log.errorsTitle"] = "Errors kept ({0})",
            ["log.clearErrors"] = "Clear errors",

            ["notif.intro"] = "Email an alert when an error is logged. Errors are batched: the first error opens a window (below) and all errors within it are sent as one message. The body can be encrypted to each recipient with OpenPGP.",
            ["notif.enabled"] = "Send error emails",
            ["notif.delay"] = "Batch window (s)",
            ["notif.delay.h"] = "How long to wait after the first error before sending, to collect more errors into one email.",
            ["notif.smtpTitle"] = "SMTP server",
            ["notif.host"] = "Host",
            ["notif.port"] = "Port",
            ["notif.security"] = "Security",
            ["notif.secAuto"] = "Auto (by port)",
            ["notif.secNone"] = "None",
            ["notif.from"] = "From address",
            ["notif.user"] = "Username",
            ["notif.password"] = "Password",
            ["notif.password.h"] = "Stored encrypted at rest (Data Protection). Leave blank to keep the current password.",
            ["notif.passwordSet"] = "(unchanged)",
            ["notif.recipients"] = "Recipients ({0})",
            ["notif.addRecipient"] = "+ Add recipient",
            ["notif.noRecipients"] = "No recipients. Add at least one to receive error emails.",
            ["notif.recipientN"] = "Recipient #{0}",
            ["notif.email"] = "Email",
            ["notif.encrypt"] = "Encrypt with OpenPGP",
            ["notif.encrypt.h"] = "Encrypt the email body to this recipient's public key. Fetch it from keys.openpgp.org or paste it below.",
            ["notif.publicKey"] = "Public key (ASCII-armored)",
            ["notif.keySet"] = "Key set",
            ["notif.fetchKey"] = "Fetch from keyserver",
            ["notif.fetchNoEmail"] = "Enter the recipient email first.",
            ["notif.fetchNone"] = "No public key found on the keyserver for {0}.",
            ["notif.fetchOk"] = "Public key fetched for {0}. Review and save.",
            ["notif.fetchErr"] = "Key fetch error: {0}",
            ["notif.save"] = "Save",
            ["notif.saved"] = "Notification settings saved.",
            ["notif.test"] = "Send test email",
            ["notif.testSent"] = "Test email sent to {0} recipient(s).",
            ["notif.testFailed"] = "Test failed: {0}",

            ["login.password"] = "Password",
            ["login.submit"] = "Log in",
            ["login.wrong"] = "Wrong password.",
        };

        /// <summary>The full set of translation keys (defined by the English baseline).</summary>
        public static IReadOnlyCollection<string> Keys => English.Keys;

        /// <summary>Selectable log levels shown in the GUI (Configuration multi-select and the
        /// Logs filter). Single source of truth so the two pages can't drift apart.</summary>
        public static readonly string[] LogLevels =
            { "Trace", "Debug", "Information", "Warning", "Error", "Critical", "Always" };
    }

    public sealed record LanguageInfo(string Code, string Label, string Name)
    {
        public static LanguageInfo For(string code)
        {
            try
            {
                var ci = CultureInfo.GetCultureInfo(code);
                var native = ci.NativeName;
                if (!string.IsNullOrEmpty(native))
                    native = char.ToUpper(native[0]) + native.Substring(1);
                return new LanguageInfo(code, code.ToUpperInvariant(), string.IsNullOrEmpty(native) ? code : native);
            }
            catch
            {
                return new LanguageInfo(code, code.ToUpperInvariant(), code);
            }
        }
    }
}
