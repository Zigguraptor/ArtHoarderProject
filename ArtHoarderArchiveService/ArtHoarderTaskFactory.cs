using ArtHoarderArchiveService.PipeCommunications;
using ArtHoarderArchiveService.PipeCommunications.Verbs;

namespace ArtHoarderArchiveService;

public static class ArtHoarderTaskFactory
{
    public static Task Create(string path, BaseVerb parsedTupleVerb, StreamString streamString,
        CancellationToken cancellationToken)
    {
        var messager = new Messager(streamString);
        return new Task(() =>
        {
            parsedTupleVerb.Invoke(messager, path, cancellationToken);
            messager.Close();
        }, cancellationToken);
    }
}
