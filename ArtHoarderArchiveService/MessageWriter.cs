using ArtHoarderArchiveService.PipeCommunications;

namespace ArtHoarderArchiveService;

public class MessageWriter : IMessageWriter
{
    private const string RewriteCommand = "#UpdateLine ";
    private const string LogCommand = "#Log";
    private readonly StreamString _streamString;

    public MessageWriter(StreamString streamString)
    {
        _streamString = streamString;
    }

    public void Write(string message)
    {
        _streamString.WriteString(message);
    }

    public void Write(string message, LogLevel logLevel)
    {
        _streamString.WriteString(LogCommand + logLevel + ' ' + message);
    }

    public void UpdateLine(string line)
    {
        _streamString.WriteString(RewriteCommand + line);
    }
}
