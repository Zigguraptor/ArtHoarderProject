using ArtHoarderArchiveService.Archive;
using ArtHoarderArchiveService.PipeCommunications;
using ArtHoarderArchiveService.PipeCommunications.Verbs;

namespace ArtHoarderArchiveService;

public class ArtHoarderTaskFactory
{
    private readonly ArchiveContextFactory _archiveContextFactory;

    public ArtHoarderTaskFactory(ArchiveContextFactory archiveContextFactory)
    {
        _archiveContextFactory = archiveContextFactory;
    }

    public Task Create(string path, BaseVerb parsedTupleVerb, StreamString streamString,
        CancellationToken cancellationToken)
    {
        var messager = new Messager(streamString);
        return new Task(() =>
        {
            parsedTupleVerb.Invoke(messager, _archiveContextFactory, path, cancellationToken);
            messager.Close();
        }, cancellationToken);
    }
}
