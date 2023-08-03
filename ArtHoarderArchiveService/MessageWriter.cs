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

    public void UpdateProgressBar(string[] path, string msg)
    {
        _progressBar?.UpdateBar(path, msg);
        UpdateProgressBar();
    }

    public void DeleteProgressBar(string[] path)
    {
        if (_progressBar == null) return;
        if (path.Length <= 0) return;
        if (_progressBar.Name != path[0]) return;
        if (path.Length == 1)
        {
            _progressBar = null;
        }
        else
        {
            _progressBar.Delete(path);
        }

        UpdateProgressBar();
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


    private void UpdateProgressBar()
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
