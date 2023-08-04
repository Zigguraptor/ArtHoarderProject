using ArtHoarderArchiveService.PipeCommunications.Verbs;

namespace ArtHoarderArchiveService;

public class ArtHoarderTask
{
    private readonly string _path;
    private readonly BaseVerb _verb;
    private readonly IMessageWriter _statusWriter;
    private CancellationTokenSource? _tokenSource;

    public readonly Guid TaskGuide = Guid.NewGuid();
    public readonly DateTime CreationDateTime = Time.NowUtcDataTime();
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
        _tokenSource?.Cancel();
        _tokenSource = null;
    }

    public Task Start()
    {
        _tokenSource?.Cancel();
        _tokenSource = new CancellationTokenSource();
        return Start(_tokenSource.Token);
    }

    private async Task Start(CancellationToken cancellationToken)
    {
        await _verb.Invoke(_statusWriter, _path, cancellationToken).ConfigureAwait(false);
        EndTask();
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
