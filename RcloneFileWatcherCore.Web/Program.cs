using RcloneFileWatcherCore.App;
using RcloneFileWatcherCore.Config;
using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Infrastructure.Logging;
using RcloneFileWatcherCore.Web.Components;

const string ConfigFileName = "RcloneFileWatcherCoreConfig.cfg";

var builder = WebApplication.CreateBuilder(args);

// Bind to localhost by default; override with the "Gui:Urls" setting (appsettings / env /
// command line) e.g. "http://0.0.0.0:5005" for LAN access. Network exposure plus
// authentication is hardened in Phase 4.
builder.WebHost.UseUrls(builder.Configuration["Gui:Urls"] ?? "http://localhost:5005");

// Bootstrap the Core logger + config the same way the console app does. If the config is
// missing or invalid we still start (with an empty config) so the GUI stays reachable and the
// user can create/fix it from the browser (config editing arrives in Phase 2).
var logger = new Logger();
var config = new ConfigLoader(ConfigFileName, logger).LoadConfig() ?? new ConfigDTO();

builder.Services.AddRcloneFileWatcherCore(config, logger);
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
