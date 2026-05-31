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

            // If the executable carries no version attributes (e.g. some test/host contexts),
            // fall back to a fixed label instead of rendering an empty " v by ".
            if (string.IsNullOrWhiteSpace(product) && string.IsNullOrWhiteSpace(version) && string.IsNullOrWhiteSpace(author))
                return "Unknown Version";

            return $"{product} v{version} by {author}";
        }
    }
}
