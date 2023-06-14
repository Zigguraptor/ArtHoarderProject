using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ArtHoarderCore.Infrastructure;

public class SubProgressInfo : INotifyPropertyChanged
{
    public string Title { get; }
    public double TotalValue { get; }


    private double _value;

    public double Value
    {
        get => _value;
        set
        {
            if (value >= TotalValue)
            {
                _remove(this);
            }
            else
            {
                SetField(ref _value, value);
            }
        }
    }

    private readonly Action<string> _report;
    private readonly Action<SubProgressInfo> _remove;

    public SubProgressInfo(string title, double totalValue, Action<string> report, Action<SubProgressInfo> remove)
    {
        Title = title;
        TotalValue = totalValue;
        _report = report;
        _remove = remove;
    }

    internal void Report(string message)
    {
        _report(message);
    }

    internal void Progress()
    {
        Value++;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        OnPropertyChanged(propertyName);
    }
}