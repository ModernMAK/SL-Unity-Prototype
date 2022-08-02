public abstract class ThreadableUnsafe<TBacking> : Threadable, IThreadableUnsafe<TBacking>
{
    public abstract TBacking Unsynchronized { get; }
}