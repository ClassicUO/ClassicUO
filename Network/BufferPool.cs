using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Network
{
    internal class BufferPool
    {
        private readonly int _arraySize;
        private readonly int _capacity;
        private readonly Queue<byte[]> _freeSegment;

        public BufferPool(in int capacity, in int arraysize)
        {
            _capacity = capacity;
            _arraySize = arraysize;
            _freeSegment = new Queue<byte[]>(capacity);
            for (int i = 0; i < capacity; i++)
                _freeSegment.Enqueue(new byte[arraysize]);
        }

        public byte[] GetFreeSegment()
        {
            lock (this)
            {
                if (_freeSegment.Count > 0)
                    return _freeSegment.Dequeue();
                else
                {
                    for (int i = 0; i < _capacity; i++)
                        _freeSegment.Enqueue(new byte[_arraySize]);
                    return _freeSegment.Dequeue();
                }
            }
        }

        public void AddFreeSegment(in byte[] segment)
        {
            if (segment == null)
                return;
            lock (this)
                _freeSegment.Enqueue(segment);
        }
    }
}
