using System.IO.Pipes;
using ArtHoarderArchiveService.PipeCommunications;
using ArtHoarderArchiveService.PipeCommunications.Verbs;

namespace ArtHoarderArchiveService;

public static class ArtHoarderTaskFactory
{
    public static Task Create(string path, BaseVerb parsedTupleVerb, NamedPipeServerStream serverStream,
        StreamString streamString,
        CancellationToken cancellationToken)
    {
        var messageWriter = new MessageWriter(streamString);
        return new Task(() =>
        {
            parsedTupleVerb.Invoke(messageWriter, path, cancellationToken);
            messageWriter.Close();
        }, cancellationToken);
    }
}
