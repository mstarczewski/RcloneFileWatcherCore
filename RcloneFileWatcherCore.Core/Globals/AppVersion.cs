using System.Reflection;

namespace RcloneFileWatcherCore.Globals
{
    public static class AppVersion
    {
        public static string GetVersion()
        {
            // Prefer the entry assembly (the console/web executable that carries the product
            // version attributes) now that this helper lives in the shared Core library.
            var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var version = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            var author = asm.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company;
            var product = asm.GetCustomAttribute<AssemblyProductAttribute>()?.Product;
            return ($"{product} v{version} by {author}") ?? "Unknown Version";
        }
    }
}
