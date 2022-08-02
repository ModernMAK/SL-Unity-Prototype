public interface IThreadable
{
    /// <summary>
    /// Exposes the 'key' to be used in 
    /// </summary>
    public object SyncRoot { get; }
}