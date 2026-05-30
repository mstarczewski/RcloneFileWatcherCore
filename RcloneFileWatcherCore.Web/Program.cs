using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using RcloneFileWatcherCore.App;
using RcloneFileWatcherCore.Config;
using RcloneFileWatcherCore.DTO;
using RcloneFileWatcherCore.Infrastructure.Logging;
using RcloneFileWatcherCore.Web.Components;
using RcloneFileWatcherCore.Web.Localization;
using System.Globalization;
using System.Security.Claims;

const string ConfigFileName = "RcloneFileWatcherCoreConfig.cfg";

var builder = WebApplication.CreateBuilder(args);

// Bind to localhost by default; override with the "Gui:Urls" setting (appsettings / env /
// command line) e.g. "http://0.0.0.0:5005" for LAN access. When exposing on the network,
// also set "Gui:Password" to require login (and prefer putting it behind an HTTPS proxy).
builder.WebHost.UseUrls(builder.Configuration["Gui:Urls"] ?? "http://localhost:5005");

// Bootstrap the Core logger + config the same way the console app does. If the config is
// missing or invalid we still start (with an empty config) so the GUI stays reachable and the
// user can create/fix it from the browser.
var logger = new Logger();
var config = new ConfigLoader(ConfigFileName, logger).LoadConfig() ?? new ConfigDTO();

builder.Services.AddRcloneFileWatcherCore(config, ConfigFileName, logger);
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSingleton<Loc>();

var supportedCultures = new[] { "en", "pl" };

var guiPassword = builder.Configuration["Gui:Password"];
var authEnabled = !string.IsNullOrWhiteSpace(guiPassword);
if (authEnabled)
{
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/login";
            options.ExpireTimeSpan = TimeSpan.FromHours(12);
            options.SlidingExpiration = true;
        });
    builder.Services.AddAuthorization();
}

var app = builder.Build();

var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("en")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);
// English is the default; only an explicit choice (culture cookie set by the language switch)
// changes it — do not auto-negotiate from the browser's Accept-Language header.
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

if (authEnabled)
{
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapGet("/login", (string error) =>
        Results.Content(LoginPage.Render(error != null), "text/html"));

    app.MapPost("/login", async (HttpContext ctx) =>
    {
        var form = await ctx.Request.ReadFormAsync();
        if (form["password"].ToString() == guiPassword)
        {
            var identity = new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, "admin") },
                CookieAuthenticationDefaults.AuthenticationScheme);
            await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
            return Results.Redirect("/");
        }
        return Results.Redirect("/login?error=1");
    }).DisableAntiforgery();

    app.MapPost("/logout", async (HttpContext ctx) =>
    {
        await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Redirect("/login");
    }).DisableAntiforgery();
}

app.UseAntiforgery();

var components = app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
if (authEnabled)
    components.RequireAuthorization();

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

internal static class LoginPage
{
    public static string Render(bool error)
    {
        var loc = new Loc();
        var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var errorHtml = error ? $"<div class=\"alert alert-err\">{loc["login.wrong"]}</div>" : "";
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
            <button class="btn" type="submit">{{loc["login.submit"]}}</button>
        </form>
    </div>
</body>
</html>
""";
    }
}
