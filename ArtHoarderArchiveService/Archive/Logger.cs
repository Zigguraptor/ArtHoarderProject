namespace ArtHoarderArchiveService.Archive;

public class Logger : IDisposable
{
    private const string Debug = "DEBUG";
    private const string Info = "INFO";
    private const string Warning = "WARNING";
    private const string Error = "ERROR";

    private StreamWriter _streamWriter = null!;

    public delegate void MessageWriter(string message);

    public Logger(string workDirectory)
    {
        NewLogFile(workDirectory);
    }

    public Logger(string workDirectory, string logSource)
    {
        NewLogFile(workDirectory);
        InfoLog($"Logs from \"{logSource}\"");
    }

    public void NewLogFile(string workDirectory)
    {
        var dir = Path.Combine(workDirectory, Constants.LogsDirectory);
        Directory.CreateDirectory(dir);

        _streamWriter = File.AppendText(Path.Combine(dir,
            "Log_" + Time.NowUtcDataTime().ToString("yyyy_MM_dd__HH_mm_ss") + ".txt"));
        InfoLog("Logger is start.");
    }

    public void DebugLog(string message)
    {
        WritMessage($"[{Time.NowUtcDataTime():HH:mm:ss}] [{Debug}] {message}");
    }

    public void InfoLog(string message)
    {
        WritMessage($"[{Time.NowUtcDataTime():HH:mm:ss}] [{Info}] {message}");
    }

    public void WarningLog(string message)
    {
        WritMessage($"[{Time.NowUtcDataTime():HH:mm:ss}] [{Warning}] {message}");
    }

    public void ErrorLog(string message)
    {
        WritMessage($"[{Time.NowUtcDataTime():HH:mm:ss}] [{Error}] {message}");
    }

    private void WritMessage(string text)
    {
        _streamWriter.WriteLine(text);
        _streamWriter.Flush();
    }

    public void Dispose()
    {
        _streamWriter.Dispose();
        GC.SuppressFinalize(this);
    }
}
