using ArtHoarderArchiveService.PipeCommunications.Verbs;

namespace ArtHoarderArchiveService;

public class ArtHoarderTask
{
    private readonly string _path;
    private readonly BaseVerb _verb;
    private readonly IMessageWriter _statusWriter;
    private Action<ArtHoarderTask>? _endCallback;

    public readonly Guid TaskGuide = Guid.NewGuid();
    public readonly DateTime CreationDateTime = Time.NowUtcDataTime();

    private CancellationToken _cancellationToken;
    public TaskStatus TaskStatus { get; private set; } = TaskStatus.Created;
    public DateTime ExecutionDateTime { get; private set; }

    public ArtHoarderTask(string path, BaseVerb verb, IMessageWriter statusWriter)
    {
        _path = path;
        _verb = verb;
        _statusWriter = statusWriter;
    }

    public void Cancel()
    {
        _cancellationToken = new CancellationToken(true);
        //If need cancellation delegate. Then you need to add it here.
        //And register Cansel() in CancellationToken in Start()
    }

    public void Start(Action<ArtHoarderTask> endCallback)
    {
        Start(endCallback, new CancellationToken(false));
    }

    public void Start(Action<ArtHoarderTask> endCallback, CancellationToken cancellationToken)
    {
        _endCallback = endCallback;
        _verb.Invoke(_statusWriter, _path, cancellationToken);

        EndTask();
        endCallback.Invoke(this);
    }

    private void WriteStatus(string message)
    {
        _statusWriter.Write(message);
    }

    private void EndTask()
    {
        _statusWriter.Write("#End");
    }

    public override string ToString()
    {
        return TaskGuide + " " + CreationDateTime + " " + _path + " " + _verb + " " + TaskStatus;
    }
}
