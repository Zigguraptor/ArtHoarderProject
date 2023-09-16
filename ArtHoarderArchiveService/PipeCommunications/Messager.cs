using System.Text;
using System.Text.Json;
using System.Timers;
using Timer = System.Timers.Timer;

namespace ArtHoarderArchiveService.PipeCommunications;

public class Messager : IMessager
{
    private const char Separator = ' ';
    private const char Insulator = '\"';
    private const string MsgCommand = "#Msg ";
    private const string PrintFileCommand = "#PrintFile ";
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
        _updateProgressBarLimiter = new Timer(150);
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

    public bool Confirmation(string message)
    {
        lock (_writerSyncRoot)
        {
            while (true)
            {
                _streamString.WriteString(message + " y or n?");
                _streamString.WriteString(ReadLineCommand);
                var resp = _streamString.ReadString();
                switch (resp)
                {
                    case "y" or "Y":
                        return true;
                    case "n" or "N":
                        return false;
                }

                _streamString.WriteString("Enter Y or N");
            }
        }
    }

    public void Write(string message)
    {
        lock (_writerSyncRoot)
            _streamString.WriteString(message);
    }

    public void WriteMessage(string message)
    {
        lock (_writerSyncRoot)
        {
            _streamString.WriteString(message);
            _streamString.WriteString("\n");
        }
    }

    public void WriteMessage(MessageType messageType, string message)
    {
        Write(MsgCommand + ' ' + messageType + ' ' + message);
    }

    public void WriteLog(string message, LogLevel logLevel)
    {
        message = LogCommand + Escape(logLevel.ToString(), message);
        WriteMessage(LogCommand + logLevel + ' ' + message);
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

    public void WriteFile(string path)
    {
        Write(PrintFileCommand + path);
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
        WriteMessage(UpdatePbCommand + progressBarJson);
    }

    private static string Escape(string s)
    {
        s = s.Replace("\\", "\\\\");
        return s.Replace("\"", "\\\"");
    }

    private static string Escape(string[] strings)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < strings.Length; i++)
        {
            var s = strings[i];
            sb.Append(Insulator);
            sb.Append(Escape(s));
            sb.Append(Insulator);
            if (i + 1 < strings.Length)
                sb.Append(Separator);
        }

        return sb.ToString();
    }

    private static string Escape(string s1, string s2) => Escape(new[] { s1, s2 });

    public void Close()
    {
        if (_upgradePlanned)
        {
            _updateProgressBarLimiter.Stop();
            SendProgressBars(null, null!);
        }

        Write(ClosingCommand);
    }
}
