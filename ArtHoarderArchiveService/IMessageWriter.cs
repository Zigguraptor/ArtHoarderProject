using ArtHoarderArchiveService.PipeCommunications;

namespace ArtHoarderArchiveService;

public interface IMessageWriter
{
    public void Write(string message);
    public void Write(string message, LogLevel logLevel);
    public ProgressBar CreateNewProgressBar(string name, int max);
    public ProgressBar CreateNewProgressBar(string name, int max, string msg);
    public void UpdateProgressBar();
    public void ClearProgressBars();
}
