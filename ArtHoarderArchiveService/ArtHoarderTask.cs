namespace ArtHoarderArchiveService;

public class ArtHoarderTask
{
    private readonly string _path;
    private readonly object _parsedObject;
    public readonly Guid TaskGuide = Guid.NewGuid();
    public readonly DateTime CreationDateTime = Time.NowUtcDataTime();

    private CancellationToken _cancellationToken;
    public TaskStatus TaskStatus { get; private set; } = TaskStatus.Created;
    public DateTime ExecutionDateTime { get; private set; }

    public ArtHoarderTask(string path, object parsedObject)
    {
        _path = path;
        _parsedObject = parsedObject;
    }

    public void Cancel()
    {
        _cancellationToken = new CancellationToken(true);
        //If need cancellation delegate. Then you need to add it here.
        //And register Cansel() in CancellationToken in Start()
    }

    public void Start()
    {
        Start(new CancellationToken(false));
    }

    public void Start(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
