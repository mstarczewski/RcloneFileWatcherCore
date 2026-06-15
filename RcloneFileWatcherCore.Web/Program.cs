using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using RcloneFileWatcherCore.App;
using RcloneFileWatcherCore.Config;
using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Infrastructure.Logging;
using RcloneFileWatcherCore.Web.Auth;
using RcloneFileWatcherCore.Web.Components;
using RcloneFileWatcherCore.Web.Localization;
using System.Globalization;
using System.Security.Claims;

const string ConfigFileName = "RcloneFileWatcherCoreConfig.cfg";

// How long a "remember me" login is kept (persistent cookie). One source of truth for both the
// cookie lifetime and the number shown in the login label, so they can't drift.
const int RememberMeDays = 30;

var builder = WebApplication.CreateBuilder(args);

// Bind to localhost by default; override with the "Gui:Urls" setting (appsettings / env /
// command line) e.g. "http://0.0.0.0:5005" for LAN access. When exposing on the network,
// enable the password in the Security page and put the GUI behind an HTTPS reverse proxy.
builder.WebHost.UseUrls(builder.Configuration["Gui:Urls"] ?? "http://localhost:5005");

// Serve wwwroot via the static web assets manifest in every environment (not just
// Development), so the CSS works when running the built binary directly, not only via
// `dotnet run`. Published builds carry a physical wwwroot and are unaffected.
builder.WebHost.UseStaticWebAssets();

// Bootstrap the Core logger + config the same way the console app does. If the config is
// missing or invalid we still start (with an empty config) so the GUI stays reachable and the
// user can create/fix it from the browser.
var logger = new Logger();
var config = new ConfigLoader(ConfigFileName, logger).LoadConfig() ?? new ConfigDTO();

// If Gui:Urls requests an https endpoint, serve it with a certificate (self-signed by default,
// persisted next to the app). This lets the reverse proxy ↔ backend hop be encrypted too; the
// exported gui-cert.crt can be handed to the proxy (Caddy tls_trusted_ca_certs) to trust it.
var guiUrls = builder.Configuration["Gui:Urls"] ?? "http://localhost:5005";
if (guiUrls.Contains("https", StringComparison.OrdinalIgnoreCase))
{
    var cert = RcloneFileWatcherCore.Web.HttpsSetup.LoadOrCreate(builder.Environment.ContentRootPath, builder.Configuration, logger);
    builder.WebHost.ConfigureKestrel(o => o.ConfigureHttpsDefaults(h => h.ServerCertificate = cert));
}

builder.Services.AddRcloneFileWatcherCore(config, ConfigFileName, logger);
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSingleton(new Loc(Path.Combine(builder.Environment.ContentRootPath, "locales")));

// GUI access control is managed at runtime in the Security page (stored hashed in
// gui-auth.json). Cookie auth + a dynamic policy are always registered; the policy reads the
// current on/off state per request, so the toggle takes effect without a restart. By default
// (no gui-auth.json) access is open - set a password in the Security page to require login.
builder.Services.AddSingleton<IAuthService>(
    new AuthService(Path.Combine(builder.Environment.ContentRootPath, "gui-auth.json")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.SlidingExpiration = true;
        // Harden the auth cookie: never exposed to JS, sent only same-site, and marked Secure
        // automatically when served over HTTPS (e.g. behind the recommended TLS reverse proxy).
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .AddRequirements(new GuiAuthRequirement())
        .Build();
});
builder.Services.AddSingleton<IAuthorizationHandler, GuiAuthHandler>();
builder.Services.AddSingleton<LoginThrottle>();

var app = builder.Build();

// Supported cultures = English baseline plus every language discovered from locales/*.json.
var supportedCultures = app.Services.GetRequiredService<Loc>().Languages.Select(l => l.Code).ToArray();
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(Loc.DefaultCulture)
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);
// English is the default; only an explicit choice (culture cookie set by the language switch)
// changes it - do not auto-negotiate from the browser's Accept-Language header.
localizationOptions.RequestCultureProviders = new List<IRequestCultureProvider>
{
    new CookieRequestCultureProvider()
};
app.UseRequestLocalization(localizationOptions);

app.UseStaticFiles();

// Language switch: store the choice in the culture cookie and reload the target page.
app.MapGet("/set-culture", (string culture, string redirect, HttpContext ctx) =>
{
    if (!string.IsNullOrWhiteSpace(culture))
    {
        ctx.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });
    }
    return Results.LocalRedirect(string.IsNullOrWhiteSpace(redirect) ? "/" : redirect);
});

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/login", (string error, HttpContext ctx) =>
{
    var auth = ctx.RequestServices.GetRequiredService<IAuthService>();
    if (!auth.Enabled)
        return Results.LocalRedirect("/");
    return Results.Content(LoginPage.Render(error != null, ctx.RequestServices.GetRequiredService<Loc>(), RememberMeDays), "text/html");
});

app.MapPost("/login", async (HttpContext ctx) =>
{
    var auth = ctx.RequestServices.GetRequiredService<IAuthService>();
    var throttle = ctx.RequestServices.GetRequiredService<LoginThrottle>();
    var clientKey = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    // Throttle brute-force guessing: after too many failures from one client, refuse for a while.
    if (throttle.IsLocked(clientKey))
        return Results.Redirect("/login?error=1");

    var form = await ctx.Request.ReadFormAsync();
    if (auth.Enabled && auth.Verify(form["password"].ToString()))
    {
        throttle.Reset(clientKey);
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, "admin") },
            CookieAuthenticationDefaults.AuthenticationScheme);

        // "Remember me": issue a persistent cookie that survives a browser restart and lasts 30 days
        // (still sliding). Unchecked keeps the default - a session cookie cleared when the browser
        // closes, with the global 12h sliding window.
        var props = new AuthenticationProperties();
        if (form["remember"].ToString() == "true")
        {
            props.IsPersistent = true;
            props.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(RememberMeDays);
        }
        await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), props);
        return Results.LocalRedirect("/");
    }

    throttle.RegisterFailure(clientKey);
    return Results.Redirect("/login?error=1");
}).DisableAntiforgery();

app.MapPost("/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.LocalRedirect("/");
}).DisableAntiforgery();

// Lightweight status for the Windows tray companion (anonymous - only non-sensitive booleans/counts).
app.MapGet("/api/status", (RcloneFileWatcherCore.Status.IStatusService status, RcloneFileWatcherCore.Infrastructure.Logging.BroadcastLogWriter logBuffer) =>
{
    var s = status.GetStatus();
    return Results.Json(new
    {
        watcherRunning = s.WatcherRunning,
        rcloneRunning = s.RcloneRunning,
        pendingChanges = s.PendingChanges,
        errors = logBuffer.GetErrors().Count
    });
});

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .RequireAuthorization();

// Optional convenience for desktop use: open the browser once the server is up. Off by
// default so headless/service deployments (systemd/NSSM) don't try to launch a browser.
if (string.Equals(builder.Configuration["Gui:OpenBrowser"], "true", StringComparison.OrdinalIgnoreCase))
{
    var url = (builder.Configuration["Gui:Urls"] ?? "http://localhost:5005").Split(';')[0];
    app.Lifetime.ApplicationStarted.Register(() => OpenBrowser.TryOpen(url, logger));
}

app.Run();

internal static class OpenBrowser
{
    public static void TryOpen(string url, RcloneFileWatcherCore.Infrastructure.Logging.Interfaces.ILogger logger)
    {
        try
        {
            System.Diagnostics.ProcessStartInfo psi;
            if (OperatingSystem.IsWindows())
                psi = new System.Diagnostics.ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true };
            else if (OperatingSystem.IsLinux())
                psi = new System.Diagnostics.ProcessStartInfo("xdg-open", url);
            else if (OperatingSystem.IsMacOS())
                psi = new System.Diagnostics.ProcessStartInfo("open", url);
            else
                return;

            System.Diagnostics.Process.Start(psi);
        }
        catch (Exception ex)
        {
            logger.Log(RcloneFileWatcherCore.Enums.LogLevel.Warning, $"Could not open browser at {url}", ex);
        }
    }
}

// Simple in-memory brute-force guard for the login endpoint: after MaxFailures failed attempts
// from one client (keyed by remote IP), further attempts are refused for LockoutDuration.
internal sealed class LoginThrottle
{
    private const int MaxFailures = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromSeconds(30);
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, Entry> _entries = new();

    private sealed class Entry
    {
        public int Failures;
        public DateTime LockedUntilUtc;
    }

    public bool IsLocked(string key)
        => _entries.TryGetValue(key, out var e) && DateTime.UtcNow < e.LockedUntilUtc;

    public void RegisterFailure(string key)
    {
        var e = _entries.GetOrAdd(key, _ => new Entry());
        lock (e)
        {
            e.Failures++;
            if (e.Failures >= MaxFailures)
            {
                e.LockedUntilUtc = DateTime.UtcNow.Add(LockoutDuration);
                e.Failures = 0;
            }
        }
    }

    public void Reset(string key) => _entries.TryRemove(key, out _);
}

internal static class LoginPage
{
    public static string Render(bool error, Loc loc, int rememberDays)
    {
        var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var errorHtml = error ? $"<div class=\"alert alert-err\">{loc["login.wrong"]}</div>" : "";
        var rememberLabel = loc.F("login.remember", rememberDays);
        return $$"""
<!DOCTYPE html>
<html lang="{{lang}}">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>RcloneFileWatcher</title>
    <link rel="stylesheet" href="app.css" />
</head>
<body>
    <div class="login-box">
        <h1>RcloneFileWatcher</h1>
        {{errorHtml}}
        <form method="post" action="/login">
            <label>{{loc["login.password"]}}
                <input type="password" name="password" autofocus />
            </label>
            <label class="checkbox">
                <input type="checkbox" name="remember" value="true" />
                {{rememberLabel}}
            </label>
            <button class="btn" type="submit">{{loc["login.submit"]}}</button>
        </form>
    </div>
</body>
</html>
""";
    }
}
