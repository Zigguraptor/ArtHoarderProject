namespace ArtHoarderArchiveService
{
    public class ArtHoarderService : BackgroundService
    {
        private readonly ILogger<ArtHoarderService> _logger;
        private readonly ITaskManager _taskManager;

        public ArtHoarderService(ILogger<ArtHoarderService> logger, ITaskManager taskManager)
        {
            _logger = logger;
            _taskManager = taskManager;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
