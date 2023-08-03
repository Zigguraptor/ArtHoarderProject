using ArtHoarderArchiveService.PipeCommunications;

namespace ArtHoarderArchiveService;

public interface IProgressWriter : IDisposable
{
    public void Write(string message);
    public void Write(string message, LogLevel logLevel);
    public void UpdateBar();
    public void UpdateBar(string msg);
    public ProgressBar CreateSubProgressBar(string name, int max);
}
