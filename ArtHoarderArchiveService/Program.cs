using ArtHoarderArchiveService.Archive;
using ArtHoarderArchiveService.Archive.Networking;
using ArtHoarderArchiveService.PipeCommunications;

namespace ArtHoarderArchiveService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((_, builder) =>
                {
                    var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData,
                        Environment.SpecialFolderOption.Create);
                    appData = Path.Combine(appData, "ArtHoarderArchive");

                    var settingsPath = Path.Combine(appData, "appsettings.json");
                    if (!File.Exists(settingsPath))
                        AppSettings.CreateDefault(settingsPath);

                    builder.SetBasePath(appData);
                    builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    var configuration = context.Configuration;
                    var appSettings = new AppSettings();
                    configuration.GetSection("AppSettings").Bind(appSettings);
                    services.AddSingleton(appSettings);

                    services.AddSingleton<ArchiveContextFactory>();
                    services.AddSingleton<ArtHoarderTaskFactory>();
                    services.AddSingleton<HttpMessageHandler, HttpClientRateLimitedHandler>();
                    services.AddSingleton<IWebDownloader, WebDownloader>();
                    services.AddSingleton<GalleryAnalyzer>();
                    services.AddSingleton<ICommandsParser, CommandsParser>();
                    services.AddSingleton<ITaskManager, TaskManager>();
                    services.AddHostedService<NamedPipeCommunicator>();
                })
                .Build();

            host.Run();
        }
    }
}
