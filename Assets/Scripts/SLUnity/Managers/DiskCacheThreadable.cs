using System;
using SLUnity.Serialization;
using SLUnity.Threading;

namespace SLUnity.Managers
{
    public class DiskCacheThreadable<TKey, TValue> : DiskCache<TKey, TValue>, IThreadable
    {
        public DiskCacheThreadable(string cacheLocation, Func<TKey, string> pathFunc, ISerializer<TValue> serializer) : base(cacheLocation, pathFunc, serializer)
        {
            SyncRoot = new object();
        }

        public object SyncRoot {
            get; private set;
        }

        public override bool Load(TKey key, out TValue value)
        {
            lock (SyncRoot)
            {
                return base.Load(key, out value);
            }
        }

        public override void Store(TKey key, TValue value)
        {
            lock (SyncRoot)
            {
                base.Store(key, value);
            }
        }
    }
}