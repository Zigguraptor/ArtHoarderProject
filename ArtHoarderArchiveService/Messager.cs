using System.Text.Json;
using System.Timers;
using ArtHoarderArchiveService.PipeCommunications;
using Timer = System.Timers.Timer;

namespace ArtHoarderArchiveService;

public class Messager : IMessageWriter
{
    private const char Separator = ' ';
    private const char Insulator = '\"';
    private const string UpdatePbCommand = "#Update ";
    private const string LogCommand = "#Log ";
    private const string ReadLineCommand = "#ReadLine";
    private const string ClosingCommand = "#END";

    private readonly object _writerSyncRoot = new();
    private readonly object _updateTimerSyncRoot = new();
    private readonly Timer _updateProgressBarLimiter;
    private readonly StreamString _streamString;

    private bool _upgradePlanned;
    private ProgressBar? _progressBar;

    public Messager(StreamString streamString)
    {
        _streamString = streamString;
        _updateProgressBarLimiter = new Timer(250);
        _updateProgressBarLimiter.AutoReset = false;
        _updateProgressBarLimiter.Elapsed += SendProgressBars;
    }

    public string? ReadLine()
    {
        lock (_writerSyncRoot)
        {
            _streamString.WriteString(ReadLineCommand);
            return _streamString.ReadString();
        }
    }

    public void Write(string message)
    {
        lock (_writerSyncRoot)
            _streamString.WriteString(message);
    }

    public void WriteLog(string message, LogLevel logLevel)
    {
        Write(LogCommand + logLevel + ' ' + message);
    }

    public ProgressBar CreateNewProgressBar(string name, int max)
    {
        _progressBar = new ProgressBar(this, name, max, _ => ClearProgressBars());
        UpdateProgressBar();
        return _progressBar;
    }

    public ProgressBar CreateNewProgressBar(string name, int max, string msg)
    {
        _progressBar = new ProgressBar(this, name, max, msg, _ => ClearProgressBars());
        UpdateProgressBar();
        return _progressBar;
    }

    public void ClearProgressBars()
    {
        _progressBar = null;
        UpdateProgressBar();
    }

    public void UpdateProgressBar() //TODO test it
    {
        if (_upgradePlanned) return;
        lock (_updateTimerSyncRoot)
        {
            if (_upgradePlanned) return;
            _upgradePlanned = true;
            _updateProgressBarLimiter.Start();
        }
    }

    private void SendProgressBars(object? sender, ElapsedEventArgs elapsedEventArgs)
    {
        if (_progressBar == null) return;
        var progressBarJson = JsonSerializer.Serialize(_progressBar);
        Write(UpdatePbCommand + progressBarJson);
    }

    private static string Escape(string s)
    {
        s = s.Replace("\\", "\\\\");
        return s.Replace("\"", "\\\"");
    }

    public void Close() => Write(ClosingCommand);
}
