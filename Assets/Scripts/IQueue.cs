using System.Collections;
using System.Collections.Generic;

public interface IQueue<TValue>: ICollection, IReadOnlyCollection<TValue>
{
    void Enqueue(TValue value);
    TValue Dequeue();

    TValue Peek();
    void Clear();
    

}