using System;

namespace ClassicUO.Network
{
    internal sealed class CircularBuffer
    {
        private byte[] _buffer;
        private int _head;
        private int _tail;


        /// <summary>
        ///     Constructs a new instance of a byte queue.
        /// </summary>
        public CircularBuffer()
        {
            _buffer = new byte[0x10000];
        }

        /// <summary>
        ///     Gets the length of the byte queue
        /// </summary>
        public int Length { get; private set; }

        /// <summary>
        ///     Clears the byte queue
        /// </summary>
        internal void Clear()
        {
            _head = 0;
            _tail = 0;
            Length = 0;
        }


        /// <summary>
        ///     Extends the capacity of the bytequeue
        /// </summary>
        private void SetCapacity(in int capacity)
        {
            byte[] newBuffer = new byte[capacity];

            if (Length > 0)
            {
                if (_head < _tail)
                {
                    Buffer.BlockCopy(_buffer, _head, newBuffer, 0, Length);
                }
                else
                {
                    Buffer.BlockCopy(_buffer, _head, newBuffer, 0, _buffer.Length - _head);
                    Buffer.BlockCopy(_buffer, 0, newBuffer, _buffer.Length - _head, _tail);
                }
            }

            _head = 0;
            _tail = Length;
            _buffer = newBuffer;
        }

        /// <summary>
        ///     Enqueues a buffer to the queue and inserts it to a correct position
        /// </summary>
        /// <param name="buffer">Buffer to enqueue</param>
        /// <param name="offset">The zero-based byte offset in the buffer</param>
        /// <param name="size">The number of bytes to enqueue</param>
        internal void Enqueue(in byte[] buffer, in int offset, in int size)
        {
            if (Length + size > _buffer.Length)
            {
                SetCapacity((Length + size + 2047) & ~2047);
            }

            if (_head < _tail)
            {
                int rightLength = _buffer.Length - _tail;

                if (rightLength >= size)
                {
                    Buffer.BlockCopy(buffer, offset, _buffer, _tail, size);
                }
                else
                {
                    Buffer.BlockCopy(buffer, offset, _buffer, _tail, rightLength);
                    Buffer.BlockCopy(buffer, offset + rightLength, _buffer, 0, size - rightLength);
                }
            }
            else
            {
                Buffer.BlockCopy(buffer, offset, _buffer, _tail, size);
            }

            _tail = (_tail + size) % _buffer.Length;
            Length += size;
        }

        /// <summary>
        ///     Dequeues a buffer from the queue
        /// </summary>
        /// <param name="buffer">Buffer to enqueue</param>
        /// <param name="offset">The zero-based byte offset in the buffer</param>
        /// <param name="size">The number of bytes to dequeue</param>
        /// <returns>Number of bytes dequeued</returns>
        internal int Dequeue(in byte[] buffer, in int offset, int size)
        {
            if (size > Length)
            {
                size = Length;
            }

            if (size == 0)
            {
                return 0;
            }

            if (_head < _tail)
            {
                Buffer.BlockCopy(_buffer, _head, buffer, offset, size);
            }
            else
            {
                int rightLength = _buffer.Length - _head;

                if (rightLength >= size)
                {
                    Buffer.BlockCopy(_buffer, _head, buffer, offset, size);
                }
                else
                {
                    Buffer.BlockCopy(_buffer, _head, buffer, offset, rightLength);
                    Buffer.BlockCopy(_buffer, 0, buffer, offset + rightLength, size - rightLength);
                }
            }

            _head = (_head + size) % _buffer.Length;
            Length -= size;

            if (Length == 0)
            {
                _head = 0;
                _tail = 0;
            }

            return size;
        }

        public byte GetID()
        {
            if (Length >= 1)
            {
                return _buffer[_head];
            }

            return 0xFF;
        }

        public int GetLength()
        {
            if (Length >= 3)
            {
                return _buffer[(_head + 2) % _buffer.Length] | (_buffer[(_head + 1) % _buffer.Length] << 8);
            }
            // return (_buffer[(_head + 1) % _buffer.Length] << 8) | _buffer[(_head + 2) % _buffer.Length];
            return 0;
        }
    }
}