using ArtHoarderArchiveService.Archive;

namespace ArtHoarderArchiveService.PipeCommunications.Verbs;

public abstract class BaseVerb
{
    public abstract bool Validate(out List<string>? errors);
    public abstract Task Invoke(IMessageWriter messageWriter, string path, CancellationToken cancellationToken);
}
