using System;
using System.Collections;
using System.Collections.Generic;

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