using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Threading;

namespace ArtHoarderClient.Infrastructure;

public class ThreadSafeObservableCollection<T> : ObservableCollection<T>
{
    private readonly Dispatcher _dispatcher;

    public ThreadSafeObservableCollection()
    {
        _dispatcher = Dispatcher.CurrentDispatcher;
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (_dispatcher.CheckAccess())
        {
            base.OnCollectionChanged(e);
        }
        else
        {
            _dispatcher.Invoke(() => base.OnCollectionChanged(e));
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (_dispatcher.CheckAccess())
        {
            base.OnPropertyChanged(e);
        }
        else
        {
            _dispatcher.Invoke(() => base.OnPropertyChanged(e));
        }
    }
}