using System.IO.Pipes;

namespace ArtHoarderArchiveService.PipeCommunications;

public class NamedPipeCommunicator : BackgroundService, INamedPipeCommunicator
{
    private const int NumThreads = 8;
    private readonly ILogger<TaskManager> _logger;
    private readonly ICommandsParser _commandsParser;
    private readonly ITaskManager _taskManager;
    private readonly ArtHoarderTaskFactory _artHoarderTaskFactory;

    public NamedPipeCommunicator(ILogger<TaskManager> logger, ICommandsParser commandsParser, ITaskManager taskManager,
        ArtHoarderTaskFactory artHoarderTaskFactory)
    {
        _logger = logger;
        _commandsParser = commandsParser;
        _taskManager = taskManager;
        _artHoarderTaskFactory = artHoarderTaskFactory;
    }

    public async Task StartCommunicationAsync(CancellationToken cancellationToken)
    {
        await using var serverStream = new NamedPipeServerStream("ArtHoarderArchive_Tasks", PipeDirection.InOut,
            NumThreads, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 4096, 4096);
        while (!cancellationToken.IsCancellationRequested)
        {
            await serverStream.WaitForConnectionAsync(cancellationToken);
            await TaskFromPipe(serverStream).ConfigureAwait(false);
        }
    }

    private static bool InitConnection(StreamString streamString)
    {
        try
        {
            streamString.WriteString("OK");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }

        return true;
    }

    private Task TaskFromPipe(NamedPipeServerStream serverStream)
    {
        var streamString = new StreamString(serverStream);
        if (!InitConnection(streamString))
        {
            serverStream.Disconnect();
            return Task.CompletedTask;
        }

        try
        {
            var command = streamString.ReadString();
            if (command == null) return Task.CompletedTask;

            var parsedTuple = _commandsParser.ParsCommand(command);

            if (!parsedTuple.verb.Validate(out var errors))
            {
                foreach (var error in errors!)
                    streamString.WriteString(error);

                return Task.CompletedTask;
            }

            var tokenSource = new CancellationTokenSource();
            var task = _artHoarderTaskFactory.Create(parsedTuple.path, parsedTuple.verb, streamString,
                tokenSource.Token);

            return parsedTuple.verb.IsParallel
                ? _taskManager.StartParallelTask(task, tokenSource)
                : _taskManager.EnqueueTask(task, tokenSource);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return Task.CompletedTask;
        }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tasks = new Task[NumThreads];
        for (var i = 0; i < NumThreads; i++)
            tasks[i] = StartCommunicationAsync(stoppingToken);
        _logger.LogInformation($"NamedPipeCommunicator started in {NumThreads} thread.");

        return Task.CompletedTask;
    }
}
