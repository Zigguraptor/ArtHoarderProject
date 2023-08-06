using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace ArtHoarderArchiveService.PipeCommunications;

//Они используются. Для сериализации.
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
[SuppressMessage("ReSharper", "CollectionNeverQueried.Global")]
public sealed class ProgressBar : IProgressWriter
{
    [JsonIgnore] private readonly object _syncRoot = new();
    [JsonIgnore] private readonly IMessageWriter _messageWriter;
    [JsonIgnore] private readonly Action<ProgressBar> _dispose;
    public string Name { get; }
    public int Max { get; }
    public int Current { get; set; }
    public string Msg { get; set; } = string.Empty;
    public List<ProgressBar> SubBars { get; } = new();

    public ProgressBar(IMessageWriter messageWriter, string name, int max, Action<ProgressBar> dispose)
    {
        Name = name;
        Max = max;
        _messageWriter = messageWriter;
        _dispose = dispose;
    }

    public ProgressBar(IMessageWriter messageWriter, string name, int max, string msg, Action<object> dispose)
        : this(messageWriter, name, max, dispose)
    {
        Msg = msg;
    }

    public void Dispose() => _dispose.Invoke(this);

    public void Write(string message) => _messageWriter.Write(message);

    public void Write(string message, LogLevel logLevel) => _messageWriter.Write(message, logLevel);

    public void UpdateBar()
    {
        lock (_syncRoot)
            Current++;
        _messageWriter.UpdateProgressBar();
    }

    public void UpdateBar(string msg)
    {
        lock (_syncRoot)
        {
            Msg = msg;
            Current++;
        }

        _messageWriter.UpdateProgressBar();
    }

    public ProgressBar CreateSubProgressBar(string name, int max)
    {
        var progressBar = new ProgressBar(_messageWriter, name, max, DisposeSubBar);
        lock (_syncRoot)
            SubBars.Add(progressBar);
        return progressBar;
    }

    private void DisposeSubBar(ProgressBar progressBar)
    {
        lock (_syncRoot)
            SubBars.Remove(progressBar);
        _messageWriter.UpdateProgressBar();
    }
}
