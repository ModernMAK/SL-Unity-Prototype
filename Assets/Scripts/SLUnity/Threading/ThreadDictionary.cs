using System.Collections;
using System.Collections.Generic;

namespace SLUnity.Threading
{
    public class ThreadDictionary<TKey, TValue> : ThreadableUnsafe<IDictionary<TKey, TValue>>, IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
    {
        private readonly IDictionary<TKey, TValue> _backingDict;
        public override IDictionary<TKey, TValue> Unsynchronized => _backingDict;

        /// <summary>
        /// Wraps a dictionary with a thread-lock.
        /// Should be avoided if possible; thread safety cannot be garunteed.
        /// </summary>
        /// <param name="backingDict"></param>
        internal ThreadDictionary(IDictionary<TKey, TValue> backingDict)
        {
            _backingDict = backingDict;
        }

        public ThreadDictionary()
        {
            _backingDict = new Dictionary<TKey, TValue>();
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
        
            lock (SyncRoot)
            {
                var array = new KeyValuePair<TKey, TValue>[_backingDict.Count];
                var i = 0;
                foreach (var item in _backingDict)
                {
                    array[i] = item;
                    i++;
                }

                return ((IEnumerable<KeyValuePair<TKey, TValue>>)array).GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_backingDict).GetEnumerator();
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            lock (SyncRoot)
            {
                _backingDict.Add(item);
            }
        }

        public void Clear()
        {
            lock (SyncRoot)
            {
                _backingDict.Clear();
            }
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            lock (SyncRoot)
            {
                return _backingDict.Contains(item);
            }
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            lock (SyncRoot)
            {
                _backingDict.CopyTo(array, arrayIndex);
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            lock (SyncRoot)
            {
                return _backingDict.Remove(item);
            }
        }

        public int Count
        {
            get
            {
                lock (SyncRoot)
                {
                    return _backingDict.Count;
                }
            }
        }

        public bool IsReadOnly
        {
            get
            {
                lock (SyncRoot)
                {
                    return _backingDict.IsReadOnly;
                }
            }
        }

        public void Add(TKey key, TValue value)
        {
            lock (SyncRoot)
            {
                _backingDict.Add(key, value);
            }
        }

        public bool ContainsKey(TKey key)
        {
            lock (SyncRoot)
            {
                return _backingDict.ContainsKey(key);
            }
        }

        public bool Remove(TKey key)
        {
            lock (SyncRoot)
            {
                return _backingDict.Remove(key);
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (SyncRoot)
            {
                return _backingDict.TryGetValue(key, out value);
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                lock (SyncRoot)
                {
                    return _backingDict[key];
                }
            }
            set
            {
                lock (SyncRoot)
                {
                    _backingDict[key] = value;
                }
            }
        }

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

        public ICollection<TKey> Keys
        {
            get
            {
                lock (SyncRoot)
                {
                    var array = new TKey[_backingDict.Count];
                    _backingDict.Keys.CopyTo(array,0);
                    return array;
                }
            }
        }

        public ICollection<TValue> Values 
        {
            get
            {
                lock (SyncRoot)
                {
                    var array = new TValue[_backingDict.Count];
                    _backingDict.Values.CopyTo(array,0);
                    return array;
                }
            
            }
        }
    }
}