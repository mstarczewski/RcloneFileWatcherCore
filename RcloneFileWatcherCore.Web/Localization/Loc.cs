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
    /// languages are discovered at startup from JSON files in the locales folder — drop a
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

        // English baseline — the guaranteed fallback. Keys here define the full string set.
        private static readonly Dictionary<string, string> English = new()
        {
            ["nav.dashboard"] = "Dashboard",
            ["nav.config"] = "Configuration",
            ["nav.rclone"] = "Rclone",
            ["nav.logs"] = "Logs",
            ["nav.logout"] = "Log out",
            ["nav.security"] = "Security",
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
            ["sec.needPassword"] = "Set a password before enabling the requirement.",
            ["sec.tlsHint"] = "Over a network, put the GUI behind an HTTPS reverse proxy (the password and cookie travel in clear text over HTTP).",

            ["dash.watcher"] = "Watcher",
            ["dash.running"] = "Running",
            ["dash.stopped"] = "Stopped",
            ["dash.queued"] = "Queued changes",
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
            ["dash.msgSyncRequested"] = "Sync requested (check Logs).",
            ["dash.msgFullSyncRequested"] = "Full sync requested (check Logs).",

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

            ["rcl.intro"] = "Preview of the rclone invocation for each watched path. Editing is done under Configuration. In Managed mode the command is built from fields (with automatic --include-from); in Script mode the given .bat/.sh file is run.",
            ["rcl.managed"] = "Managed",
            ["rcl.script"] = "Script",
            ["rcl.noCommand"] = "Managed mode without a configured command.",
            ["rcl.scriptLabel"] = "Script:",
            ["rcl.notSet"] = "— (not set)",
            ["rcl.noPaths"] = "No configured paths.",

            ["log.level"] = "Level:",
            ["log.all"] = "All",
            ["log.autoScroll"] = "Auto-scroll",
            ["log.entries"] = "{0} / {1} entries",
            ["log.clear"] = "Clear view",
            ["log.noEntries"] = "(no entries)",

            ["login.password"] = "Password",
            ["login.submit"] = "Log in",
            ["login.wrong"] = "Wrong password.",
        };

        /// <summary>The full set of translation keys (defined by the English baseline).</summary>
        public static IReadOnlyCollection<string> Keys => English.Keys;
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
