using System.Text;
using System.Text.Json;
using System.Timers;
using ArtHoarderArchiveService.PipeCommunications;
using Timer = System.Timers.Timer;

namespace ArtHoarderArchiveService;

public class MessageWriter : IMessageWriter
{
    private const char Separator = ' ';
    private const char Insulator = '\"';
    private const string UpdatePbCommand = "#Update ";
    private const string LogCommand = "#Log ";

    private readonly object _syncRoot = new();
    private readonly Timer _updateProgressBarLimiter;
    private readonly StreamString _streamString;

    private bool _upgradePlanned;
    private ProgressBar? _progressBar;

    public MessageWriter(StreamString streamString)
    {
        _streamString = streamString;
        _updateProgressBarLimiter = new Timer(250);
        _updateProgressBarLimiter.AutoReset = false;
        _updateProgressBarLimiter.Elapsed += SendProgressBars;
    }

    public void Write(string message)
    {
        _streamString.WriteString('/' + message);
    }

    public void Write(string message, LogLevel logLevel)
    {
        _streamString.WriteString(LogCommand + logLevel + ' ' + message);
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
        lock (_syncRoot)
        {
            if (_upgradePlanned) return;
            _upgradePlanned = true;
            _updateProgressBarLimiter.Start();
        }
    }

    private void SendProgressBars(object? sender, ElapsedEventArgs elapsedEventArgs)
    {
        if (_progressBar != null)
        {
            var progressBar = JsonSerializer.Serialize(_progressBar);
            _streamString.WriteString(UpdatePbCommand + progressBar);
            return;
        }

        _streamString.WriteString(UpdatePbCommand);
    }

    private void Write(string[] messages)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < messages.Length;)
        {
            messages[i] = Escape(messages[i]);

            sb.Append(Insulator);
            sb.Append(messages[i]);
            sb.Append(Insulator);
            if (++i < messages.Length)
                sb.Append(Separator);
        }

        _streamString.WriteString(sb.ToString());
    }

    private static string Escape(string s)
    {
        s = s.Replace("\\", "\\\\");
        return s.Replace("\"", "\\\"");
    }
}
