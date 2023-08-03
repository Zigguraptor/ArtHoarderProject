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
            {
                if (TryGetTaskFromPipe(streamString, out var task))
                {
                    _taskManager.EnqueueTask(task!);
                }
            }

            serverStream.Disconnect();
        }
    }

    private bool InitConnection(StreamString streamString)
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

    private bool TryGetTaskFromPipe(StreamString streamString, out ArtHoarderTask? task)
    {
        try
        {
            var command = streamString.ReadString();
            if (command != null)
            {
                var parsedTuple = _commandsParser.ParsCommand(command);
                var artHoarderTask = new ArtHoarderTask(parsedTuple.path, parsedTuple.verb, streamString);
                if (artHoarderTask.TaskStatus == TaskStatus.Broken)
                {
                    task = null;
                    return false;
                }

                task = artHoarderTask;
                return true;
            }
            else
            {
                task = null;
                return false;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            task = null;
            return false;
        }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NamedPipeCommunicator started");
        throw new NotImplementedException();
    }
}
