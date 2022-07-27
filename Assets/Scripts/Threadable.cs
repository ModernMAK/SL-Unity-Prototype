using System;
using System.Collections;
using System.Collections.Generic;

public interface IThreadable
{
    /// <summary>
    /// Exposes the 'key' to be used in 
    /// </summary>
    public object SyncRoot { get; }
}
public class Threadable : IThreadable
{
    public Threadable()
    {
        _syncRoot = new object();
    }

    private readonly object _syncRoot;
    public object SyncRoot => _syncRoot;
}

public abstract class ThreadableUnsafe<TBacking> : Threadable
{
    public abstract TBacking Unsynchronized { get; }
}
public class ThreadDictionary<TKey, TValue> : ThreadableUnsafe<IDictionary<TKey, TValue>>, IDictionary<TKey, TValue>
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
public class ThreadSet<TValue> : ThreadableUnsafe<ISet<TValue>>, ISet<TValue>
{
    private readonly ISet<TValue> _backingSet;
    public override ISet<TValue> Unsynchronized => _backingSet;

    public ThreadSet()
    {
        _backingSet = new HashSet<TValue>();
    }

    public IEnumerator<TValue> GetEnumerator()
    {
        throw new NotImplementedException();
        return _backingSet.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
        return ((IEnumerable)_backingSet).GetEnumerator();
    }

    public void ExceptWith(IEnumerable<TValue> other)
    {
        throw new NotImplementedException();
        _backingSet.ExceptWith(other);
    }

    public void IntersectWith(IEnumerable<TValue> other)
    {
        throw new NotImplementedException();
        _backingSet.IntersectWith(other);
    }

    public bool IsProperSubsetOf(IEnumerable<TValue> other)
    {
        throw new NotImplementedException();
        return _backingSet.IsProperSubsetOf(other);
    }

    public bool IsProperSupersetOf(IEnumerable<TValue> other)
    {
        throw new NotImplementedException();
        return _backingSet.IsProperSupersetOf(other);
    }

    public bool IsSubsetOf(IEnumerable<TValue> other)
    {
        throw new NotImplementedException();
        return _backingSet.IsSubsetOf(other);
    }

    public bool IsSupersetOf(IEnumerable<TValue> other)
    {
        throw new NotImplementedException();
        return _backingSet.IsSupersetOf(other);
    }

    public bool Overlaps(IEnumerable<TValue> other)
    {
        throw new NotImplementedException();
        return _backingSet.Overlaps(other);
    }

    public bool SetEquals(IEnumerable<TValue> other)
    {
        throw new NotImplementedException();
        return _backingSet.SetEquals(other);
    }

    public void SymmetricExceptWith(IEnumerable<TValue> other)
    {
        throw new NotImplementedException();
        _backingSet.SymmetricExceptWith(other);
    }

    public void UnionWith(IEnumerable<TValue> other)
    {
        throw new NotImplementedException();
        _backingSet.UnionWith(other);
    }

    public bool Add(TValue item)
    {
        lock (SyncRoot)
        {
            return _backingSet.Add(item);
        }
    }

    void ICollection<TValue>.Add(TValue item)
    {
        lock (SyncRoot)
        {
            _backingSet.Add(item);
        }
    }

    public void Clear()
    {
        throw new NotImplementedException();
        _backingSet.Clear();
    }

    public bool Contains(TValue item)
    {
        lock (SyncRoot)
        {
            return _backingSet.Contains(item);
        }
    }

    public void CopyTo(TValue[] array, int arrayIndex)
    {
        throw new NotImplementedException();
        _backingSet.CopyTo(array, arrayIndex);
    }

    public bool Remove(TValue item)
    {
        lock (SyncRoot)
        {
            return _backingSet.Remove(item);
        }
    }

    public int Count =>
        throw new NotImplementedException();// _backingSet.Count;

    public bool IsReadOnly =>
        throw new NotImplementedException();// _backingSet.IsReadOnly;
}

public interface IQueue<TValue>: ICollection, IReadOnlyCollection<TValue>
{
    void Enqueue(TValue value);
    TValue Dequeue();

    TValue Peek();
    void Clear();
    

}
public class ThreadQueue<TValue> : Threadable, IQueue<TValue>
{
    private readonly Queue<TValue> _backingQueue;
    public ThreadQueue()
    {
        _backingQueue = new Queue<TValue>();
    }


    public TValue[] CreateCopy()
    {
        lock (SyncRoot)
        {
            var array = new TValue[_backingQueue.Count];
            ((ICollection)_backingQueue).CopyTo(array, 0);
            return array;
        }
    }
    public void CopyTo(Array array, int index)
    {
        lock (SyncRoot)
        {
            ((ICollection)_backingQueue).CopyTo(array, index);
        }
    }

    public int Count
    {
        get
        {
            lock (SyncRoot)
            {
                return  _backingQueue.Count;
            }
        }
    }
    public bool IsSynchronized => true;

    public void Enqueue(TValue value)
    {
        lock (SyncRoot)
        {
            _backingQueue.Enqueue(value);
        }
    }

    public TValue Dequeue()
    {
        lock (SyncRoot)
        {
            return _backingQueue.Dequeue();
        }
    }

    public TValue Peek()
    {
        lock (SyncRoot)
        {
          return  _backingQueue.Peek();
        }
    }

    public void Clear()
    {
        lock (SyncRoot)
        {
              _backingQueue.Clear();
        }
    }

    public IEnumerator<TValue> GetEnumerator()
    {
        lock (SyncRoot)
        {
            var temp = new TValue[_backingQueue.Count];
            _backingQueue.CopyTo(temp,0);
            return ((IEnumerable<TValue>)temp).GetEnumerator();
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}