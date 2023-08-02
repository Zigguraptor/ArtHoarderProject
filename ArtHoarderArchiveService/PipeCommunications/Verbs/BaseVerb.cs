namespace ArtHoarderArchiveService.PipeCommunications.Verbs;

public abstract class BaseVerb
{
    public abstract bool Validate(out List<string> errors);
    public abstract bool Invoke(CancellationToken cancellationToken);
}
