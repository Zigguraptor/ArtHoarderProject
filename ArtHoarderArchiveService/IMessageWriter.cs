namespace ArtHoarderArchiveService;

public interface IMessageWriter
{
    public void Write(string message);
    public void Write(string message, LogLevel logLevel);
    public void UpdateProgressBar(string[] path, string msg);
    public void DeleteProgressBar(string[] path);
    public void ClearProgressBars();
}
