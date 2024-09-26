using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotwordDetectionApp
{
    using System.Collections.Concurrent; // Add this using directive
    using System.Runtime.Intrinsics.X86;

    // Add this class definition to the file
    public class RingBuffer<T>
    {
        private readonly ConcurrentQueue<T> _queue = new();
        private readonly int _size;

        public RingBuffer(int size)
        {
            _size=size;
        }

        public void Write(T item)
        {
            _queue.Enqueue(item);
            while (_queue.Count>_size)
            {
                _queue.TryDequeue(out _);
            }
        }

        public T[] ToArray()
        {
            return _queue.ToArray();
        }
    }
}

    
