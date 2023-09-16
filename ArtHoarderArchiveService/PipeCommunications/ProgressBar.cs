using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace ArtHoarderArchiveService.PipeCommunications;

//Они используются. Для сериализации.
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
[SuppressMessage("ReSharper", "CollectionNeverQueried.Global")]
public sealed class ProgressBar : IProgressWriter
{
    [JsonIgnore] private readonly object _syncRoot = new();
    [JsonIgnore] private readonly IMessager _messager;
    [JsonIgnore] private readonly Action<ProgressBar> _dispose;
    public string Name { get; }
    public int Max { get; }
    public int Current { get; set; }
    public string Msg { get; set; } = string.Empty;
    public List<ProgressBar> SubBars { get; } = new();

    public ProgressBar(IMessager messager, string name, int max, Action<ProgressBar> dispose)
    {
        Name = name;
        Max = max;
        _messager = messager;
        _dispose = dispose;
    }

    public ProgressBar(IMessager messager, string name, int max, string msg, Action<object> dispose)
        : this(messager, name, max, dispose)
    {
        Msg = msg;
    }

    public void Dispose() => _dispose.Invoke(this);
    public void WriteMessage(string message) => _messager.WriteMessage(message);
    public void WriteMessage(MessageType messageType, string message) => _messager.WriteMessage(messageType, message);
    public void WriteLog(string message, LogLevel logLevel) => _messager.WriteLog(message, logLevel);

    public void UpdateBar()
    {
        lock (_syncRoot)
            Current++;
        _messager.UpdateProgressBar();
    }

    public void UpdateBar(string msg)
    {
        lock (_syncRoot)
        {
            Msg = msg;
            Current++;
        }

        _messager.UpdateProgressBar();
    }

    public ProgressBar CreateSubProgressBar(string name, int max)
    {
        var progressBar = new ProgressBar(_messager, name, max, DisposeSubBar);
        lock (_syncRoot)
            SubBars.Add(progressBar);
        return progressBar;
    }

    private void DisposeSubBar(ProgressBar progressBar)
    {
        lock (_syncRoot)
            SubBars.Remove(progressBar);
        _messager.UpdateProgressBar();
    }
}
