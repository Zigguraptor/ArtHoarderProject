using ArtHoarderArchiveService.PipeCommunications;

namespace ArtHoarderArchiveService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    services.AddSingleton<ICommandsParser, CommandsParser>();
                    services.AddSingleton<ITaskManager, TaskManager>();
                    services.AddHostedService<NamedPipeCommunicator>();
                })
                .Build();

            host.Run();
        }
    }
}
