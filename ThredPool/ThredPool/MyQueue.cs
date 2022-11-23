using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThredPool
{
    public class MyQueue<T>
    {
        private readonly LinkedList<T> myqueue;
        private readonly object _lock = new();      
        public int Count => myqueue.Count;
        public MyQueue() => myqueue = new LinkedList<T>();

        public void Enqueue(T value)
        {
            lock (_lock)
            {
                myqueue.AddLast(value);
            }
        }

        public bool TryDequeueHead(out T value)
        {
            lock (_lock)
            {
                value = default;
                if (myqueue.Count == 0)
                    return false;
                value = myqueue.First();
                myqueue.RemoveFirst();
                return true;
            }
        }

        public bool TryDequeueTail(out T value)
        {
            lock (_lock)
            {
                value = default;
                if (myqueue.Count == 0)
                    return false;
                value = myqueue.Last();
                myqueue.RemoveLast();
                return true;
            }
        }
    }
}
