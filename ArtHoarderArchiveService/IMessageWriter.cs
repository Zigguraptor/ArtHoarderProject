namespace ArtHoarderArchiveService;

public interface IMessageWriter
{
    public void Write(string message);
    public void Write(string message, LogLevel logLevel);
    public void UpdateLine(string line);
}
