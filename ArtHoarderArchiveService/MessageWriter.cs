using System.Text;
using System.Text.Json;
using ArtHoarderArchiveService.PipeCommunications;

namespace ArtHoarderArchiveService;

public class MessageWriter : IMessageWriter
{
    public const char Separator = ' ';
    public const char Insulator = '\"';
    private const string UpdatePbCommand = "#Update ";
    private const string LogCommand = "#Log ";

    private readonly StreamString _streamString;
    private ProgressBar? _progressBar = null;

    public MessageWriter(StreamString streamString)
    {
        _streamString = streamString;
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


    public void UpdateProgressBar()
    {
        if (_progressBar != null)
        {
            var progressBar = JsonSerializer.Serialize(_progressBar);
            _streamString.WriteString(UpdatePbCommand + progressBar);
            return;
        }

        _streamString.WriteString(UpdatePbCommand);
    }

    private static string Escape(string s)
    {
        s = s.Replace("\\", "\\\\");
        return s.Replace("\"", "\\\"");
    }
}
