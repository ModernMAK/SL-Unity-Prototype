namespace SLUnity.Threading
{
    public class ThreadVar<T> : Threadable, IThreadableUnsafe<T>
    {
    
        public ThreadVar(T initialValue = default)
        {
            Unsynchronized = initialValue;
        }
    
        public T Unsynchronized { get; set; }
        public T Synchronized{
            get
            {
                lock (SyncRoot)
                {
                    return Unsynchronized;
                }
            }    
            set
            {
                lock (SyncRoot)
                {
                    Unsynchronized = value;
                }
            }    
        }
    }
}