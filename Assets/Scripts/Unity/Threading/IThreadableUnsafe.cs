public interface IThreadableUnsafe<out TBacking> : IThreadable
{
    TBacking Unsynchronized { get; }
}