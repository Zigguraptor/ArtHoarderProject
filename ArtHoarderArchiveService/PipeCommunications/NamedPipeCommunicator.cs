using System.IO.Pipes;

namespace ArtHoarderArchiveService.PipeCommunications;

public class NamedPipeCommunicator : BackgroundService, INamedPipeCommunicator
{
    private const int NumThreads = 1;
    private readonly ILogger<TaskManager> _logger;
    private readonly ICommandsParser _commandsParser;
    private readonly ITaskManager _taskManager;

    public NamedPipeCommunicator(ILogger<TaskManager> logger, ICommandsParser commandsParser, ITaskManager taskManager)
    {
        _logger = logger;
        _commandsParser = commandsParser;
        _taskManager = taskManager;
    }


    public async Task StartCommunicationAsync(CancellationToken cancellationToken)
    {
        await using var serverStream = new NamedPipeServerStream("ArtHoarderArchive_Tasks", PipeDirection.InOut,
            NumThreads, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 4096, 4096);
        while (!cancellationToken.IsCancellationRequested)
        {
            await serverStream.WaitForConnectionAsync(cancellationToken);
            var streamString = new StreamString(serverStream);
            if (InitConnection(streamString))
                RegTaskFromPipe(streamString);

            serverStream.Disconnect();
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

    private void RegTaskFromPipe(StreamString streamString)
    {
        try
        {
            var command = streamString.ReadString();
            if (command != null)
            {
                var parsedTuple = _commandsParser.ParsCommand(command);

                if (parsedTuple.verb.Validate(out var errors))
                {
                    foreach (var error in errors!)
                        streamString.WriteString(error);
                    return;
                }

                var tokenSource = new CancellationTokenSource();
                var task = ArtHoarderTaskFactory.Create(parsedTuple.path, parsedTuple.verb, streamString,
                    tokenSource.Token);

                if (parsedTuple.verb.IsParallel)
                {
                    _taskManager.StartParallelTask(task, tokenSource);
                }
                else
                {
                    _taskManager.EnqueueTask(task, tokenSource);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NamedPipeCommunicator started");
        throw new NotImplementedException();
    }
}
