using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RcloneFileWatcherCore.App;
using RcloneFileWatcherCore.Config;
using RcloneFileWatcherCore.Globals;
using RcloneFileWatcherCore.Infrastructure.Logging;
using System;
using System.Linq;

namespace RcloneFileWatcherCore
{
    class Program
    {
        private const string ConfigFileName = "RcloneFileWatcherCoreConfig.cfg";
        private const int ExitCodeConfigError = 2;

        static void Main(string[] args)
        {
            try
            {
                if (args.Contains("--version") || args.Contains("-v"))
                {
                    Console.WriteLine(AppVersion.GetVersion());
                    return;
                }

                if (args.Contains("--generateConfig") || args.Contains("-generateConfig"))
                {
                    GenerateConfig();
                    return;
                }

                // Bootstrap logger: writes to the console so config-loading errors are visible,
                // and gets its enabled levels set from the config during LoadConfig().
                var logger = new Logger();
                var config = new ConfigLoader(ConfigFileName, logger).LoadConfig();
                if (config?.Path == null || !config.Path.Any())
                {
                    logger.Log(Enums.LogLevel.Error, "Error in config file");
                    Environment.Exit(ExitCodeConfigError);
                    return;
                }

                // No default logging providers: the application's own Logger remains the only
                // console output, matching the previous console behaviour. host.Run() blocks
                // until Ctrl+C / SIGTERM, replacing the old AutoResetEvent.WaitOne().
                var host = new HostBuilder()
                    .ConfigureServices(services => services.AddRcloneFileWatcherCore(config, logger))
                    .Build();

                host.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex}");
            }
        }

        private static void GenerateConfig()
        {
            new ConfigGenerator(ConfigFileName).GenerateConfig();
            Console.WriteLine($"Example config generated:{ConfigFileName}");
        }
    }
}
