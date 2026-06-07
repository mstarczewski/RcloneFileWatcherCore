using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RcloneFileWatcherCore.Tray;

// Windows system-tray companion for RcloneFileWatcher. Runs with no window (it lives in the tray
// next to the clock), colours its icon by the watcher state polled from the web app's /api/status:
//   green  = rclone is syncing
//   gray   = idle (watcher running, nothing in progress)
//   red    = there are kept errors
//   hollow = the web app is unreachable (offline)
// Right-click menu: Open GUI / Re-check now / Exit. If RcloneFileWatcherCore.Web(.exe) sits next to
// this exe and nothing answers on the URL at start-up, the tray launches it (and stops it on exit).
internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.Run(new TrayContext(args));
    }
}

internal sealed class TrayContext : ApplicationContext
{
    private readonly string _url;
    private readonly NotifyIcon _icon;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(4) };

    private readonly Icon _green = Dot(Color.FromArgb(76, 175, 120));
    private readonly Icon _gray = Dot(Color.FromArgb(150, 150, 150));
    private readonly Icon _red = Dot(Color.FromArgb(200, 70, 60));
    private readonly Icon _offline = Dot(Color.FromArgb(70, 70, 70));

    private Process _launchedWeb;

    public TrayContext(string[] args)
    {
        // URL: first non-flag arg, else default.
        _url = "http://localhost:5005";
        foreach (var a in args)
            if (!a.StartsWith("-", StringComparison.Ordinal))
                _url = a.TrimEnd('/');

        var menu = new ContextMenuStrip();
        menu.Items.Add("Open GUI", null, (_, _) => OpenGui());
        menu.Items.Add("Re-check now", null, async (_, _) => await PollAsync());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitApp());

        _icon = new NotifyIcon
        {
            Visible = true,
            Icon = _offline,
            Text = "RcloneFileWatcher",
            ContextMenuStrip = menu
        };
        _icon.DoubleClick += (_, _) => OpenGui();

        TryLaunchWebIfBundled();

        _timer = new System.Windows.Forms.Timer { Interval = 3000 };
        _timer.Tick += async (_, _) => await PollAsync();
        _timer.Start();
        _ = PollAsync();
    }

    private async Task PollAsync()
    {
        try
        {
            using var resp = await _http.GetAsync(_url + "/api/status");
            resp.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var root = doc.RootElement;

            bool watcher = GetBool(root, "watcherRunning");
            bool rclone = GetBool(root, "rcloneRunning");
            int errors = GetInt(root, "errors");
            int pending = GetInt(root, "pendingChanges");

            if (errors > 0)
                SetState(_red, $"RcloneFileWatcher — {errors} error(s) kept");
            else if (rclone)
                SetState(_green, "RcloneFileWatcher — syncing");
            else if (watcher)
                SetState(_gray, pending > 0 ? $"RcloneFileWatcher — idle ({pending} queued)" : "RcloneFileWatcher — idle");
            else
                SetState(_gray, "RcloneFileWatcher — watcher stopped");
        }
        catch
        {
            SetState(_offline, "RcloneFileWatcher — offline");
        }
    }

    private void SetState(Icon icon, string tooltip)
    {
        _icon.Icon = icon;
        _icon.Text = tooltip.Length > 63 ? tooltip.Substring(0, 63) : tooltip; // NotifyIcon.Text cap
    }

    private void OpenGui()
    {
        try { Process.Start(new ProcessStartInfo(_url) { UseShellExecute = true }); }
        catch { /* best effort */ }
    }

    private void TryLaunchWebIfBundled()
    {
        try
        {
            var dir = AppContext.BaseDirectory;
            var exe = Path.Combine(dir, "RcloneFileWatcherCore.Web.exe");
            var dll = Path.Combine(dir, "RcloneFileWatcherCore.Web.dll");
            ProcessStartInfo psi = null;
            if (File.Exists(exe))
                psi = new ProcessStartInfo(exe) { WorkingDirectory = dir, UseShellExecute = false, CreateNoWindow = true };
            else if (File.Exists(dll))
                psi = new ProcessStartInfo("dotnet", $"\"{dll}\"") { WorkingDirectory = dir, UseShellExecute = false, CreateNoWindow = true };

            if (psi != null)
                _launchedWeb = Process.Start(psi);
        }
        catch
        {
            // No bundled web app, or it's already running as a service - just monitor the URL.
        }
    }

    private void ExitApp()
    {
        _timer.Stop();
        _icon.Visible = false;
        _icon.Dispose();
        try
        {
            if (_launchedWeb is { HasExited: false })
                _launchedWeb.Kill(entireProcessTree: true);
        }
        catch { /* best effort */ }
        ExitThread();
    }

    private static bool GetBool(JsonElement e, string name)
        => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.True;

    private static int GetInt(JsonElement e, string name)
        => e.TryGetProperty(name, out var v) && v.TryGetInt32(out var i) ? i : 0;

    private static Icon Dot(Color color)
    {
        using var bmp = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using var brush = new SolidBrush(color);
            g.FillEllipse(brush, 2, 2, 12, 12);
        }
        return Icon.FromHandle(bmp.GetHicon());
    }
}
