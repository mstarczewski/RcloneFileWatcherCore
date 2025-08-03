using System.Reflection;

namespace RcloneFileWatcherCore.Globals
{
    public static class AppVersion
    {
        public static string GetVersion()
        {
            var asm = Assembly.GetExecutingAssembly();
            var version = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            var author = asm.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company;
            var product = asm.GetCustomAttribute<AssemblyProductAttribute>()?.Product;
            return ($"{product} v{version} by {author}") ?? "Unknown Version";
        }
    }
}
