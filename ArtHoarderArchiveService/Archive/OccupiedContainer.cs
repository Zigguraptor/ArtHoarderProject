namespace ArtHoarderArchiveService.Archive;

public class OccupiedContainer<T> : IDisposable where T : IDisposable
{
    private readonly T _item;
    private readonly Action<OccupiedContainer<T>> _realise;
    private readonly SortedSet<object> _owners = new();
    private bool _isDisposed = false;
    public int OwnersCount => _owners.Count;

    public OccupiedContainer(T item, object firstOwner, Action<OccupiedContainer<T>> realise)
    {
        _item = item;
        _realise = realise;
        _owners.Add(firstOwner);
    }

    public T TakeItem(object owner)
    {
        if (_isDisposed) throw new ObjectDisposedException(null);
        _owners.Add(owner);
        return _item;
    }

    public void Realise(object owner)
    {
        _owners.Remove(owner);
        if (_owners.Count == 0)
            Dispose();
    }

    public void Dispose()
    {
        _isDisposed = true;
        _item.Dispose();
        _realise.Invoke(this);
    }
}
