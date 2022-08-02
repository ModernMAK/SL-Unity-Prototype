using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SLUnity.Threading
{
    public class ThreadList<TValue> : ThreadableUnsafe<IList<TValue>>, IList<TValue>
    {
        private readonly IList<TValue> _backingList;
        public override IList<TValue> Unsynchronized => _backingList;

        public ThreadList()
        {
            _backingList = new List<TValue>();
        }
        public ThreadList(params TValue[] values)
        {
            _backingList = new List<TValue>(values);
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            IList<TValue> copy;
            lock (SyncRoot)
            {
                copy = _backingList.ToArray();
            }
            return copy.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


        public void Add(TValue item)
        {
            lock (SyncRoot)
            {
                _backingList.Add(item);
            }
        }

        void ICollection<TValue>.Add(TValue item)
        {
            lock (SyncRoot)
            {
                _backingList.Add(item);
            }
        }

        public void Clear()
        {
            lock (SyncRoot)
            {
                _backingList.Clear();
            }
        }

        public bool Contains(TValue item)
        {
            lock (SyncRoot)
            {
                return _backingList.Contains(item);
            }
        }

        public void CopyTo(TValue[] array, int arrayIndex)
        {
            throw new NotImplementedException();
            _backingList.CopyTo(array, arrayIndex);
        }

        public bool Remove(TValue item)
        {
            lock (SyncRoot)
            {
                return _backingList.Remove(item);
            }
        }

        public int Count =>
            throw new NotImplementedException();// _backingSet.Count;

        public bool IsReadOnly =>
            throw new NotImplementedException();// _backingSet.IsReadOnly;

        public int IndexOf(TValue item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, TValue item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public TValue this[int index]
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
    }
    public class ThreadArray<TValue> : ThreadableUnsafe<IList<TValue>>, IList<TValue>
    {
        private readonly IList<TValue> _backingList;
        public override IList<TValue> Unsynchronized => _backingList;

        public ThreadArray(int size)
        {
            _backingList = new TValue[size];
        }
        public ThreadArray(params TValue[] values)
        {
            _backingList = values;
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            IList<TValue> copy;
            lock (SyncRoot)
            {
                copy = _backingList.ToArray();
            }
            return copy.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


        public void Add(TValue item)
        {
            lock (SyncRoot)
            {
                _backingList.Add(item);
            }
        }

        void ICollection<TValue>.Add(TValue item)
        {
            lock (SyncRoot)
            {
                _backingList.Add(item);
            }
        }

        public void Clear()
        {
            lock (SyncRoot)
            {
                _backingList.Clear();
            }
        }

        public bool Contains(TValue item)
        {
            lock (SyncRoot)
            {
                return _backingList.Contains(item);
            }
        }

        public void CopyTo(TValue[] array, int arrayIndex)
        {
            throw new NotImplementedException();
            _backingList.CopyTo(array, arrayIndex);
        }

        public bool Remove(TValue item)
        {
            lock (SyncRoot)
            {
                return _backingList.Remove(item);
            }
        }

        public int Count
        {
            get
            {
                lock (SyncRoot)
                {
                    return _backingList.Count;
                }
            }
        }

        public bool IsReadOnly =>
            throw new NotImplementedException();// _backingSet.IsReadOnly;

        public int IndexOf(TValue item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, TValue item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public TValue this[int index]
        {
            get
            {
                lock (SyncRoot)
                {
                    return _backingList[index];
                }
            }
            set
            {
                lock (SyncRoot)
                {
                    _backingList[index] = value;
                }
            }
        }
    }
}