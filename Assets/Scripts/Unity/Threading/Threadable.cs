public class Threadable : IThreadable
{
    public Threadable()
    {
        _syncRoot = new object();
    }

    private readonly object _syncRoot;
    public object SyncRoot => _syncRoot;
}