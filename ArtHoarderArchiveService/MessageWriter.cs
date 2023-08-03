using ArtHoarderArchiveService.PipeCommunications;

namespace ArtHoarderArchiveService;

public class MessageWriter : IMessageWriter
{
    private const string RewriteCommand = "#rewrite ";
    private readonly StreamString _streamString;

    public MessageWriter(StreamString streamString)
    {
        _streamString = streamString;
    }

    public void Write(string message)
    {
        _streamString.WriteString(message);
    }

    public void Rewrite(string line)
    {
        _streamString.WriteString(RewriteCommand + line);
    }
}
