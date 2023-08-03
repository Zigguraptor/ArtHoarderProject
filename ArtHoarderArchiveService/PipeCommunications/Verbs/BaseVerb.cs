using ArtHoarderArchiveService.Archive;

namespace ArtHoarderArchiveService.PipeCommunications.Verbs;

public abstract class BaseVerb
{
    public abstract bool Validate(out List<string>? errors);
    public abstract void Invoke(IMessageWriter statusWriter, string path, CancellationToken cancellationToken);
}
