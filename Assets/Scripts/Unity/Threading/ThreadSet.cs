using System;
using System.Collections;
using System.Collections.Generic;

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