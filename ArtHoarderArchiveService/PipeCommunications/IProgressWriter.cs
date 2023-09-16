namespace ArtHoarderArchiveService.PipeCommunications;

public interface IProgressWriter : IDisposable
{
    public void WriteMessage(string message);
    public void WriteMessage(MessageType messageType, string message);
    public void WriteLog(string message, LogLevel logLevel);
    public void UpdateBar();
    public void UpdateBar(string msg);
    public ProgressBar CreateSubProgressBar(string name, int max);
}
