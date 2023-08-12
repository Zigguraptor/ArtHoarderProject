namespace ArtHoarderArchiveService.PipeCommunications;

public interface IMessager
{
    public string? ReadLine();
    public bool Confirmation(string message);
    public void Write(string message);
    public void WriteLine(string message);
    public void WriteLog(string message, LogLevel logLevel);
    public ProgressBar CreateNewProgressBar(string name, int max);
    public ProgressBar CreateNewProgressBar(string name, int max, string msg);
    public void UpdateProgressBar();
    public void ClearProgressBars();
}
