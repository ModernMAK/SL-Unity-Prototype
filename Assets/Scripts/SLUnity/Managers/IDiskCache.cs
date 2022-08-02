namespace SLUnity.Managers
{
    public interface IDiskCache<in TKey, TValue>
    {
        public void Store(TKey key, TValue value);
        public bool Load(TKey key, out TValue value);

    }
}