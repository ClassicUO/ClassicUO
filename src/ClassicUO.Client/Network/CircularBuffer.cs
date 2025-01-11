// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Runtime.CompilerServices;

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
        public CircularBuffer(int size = 4096)
        {
            _buffer = new byte[size];
        }

        /// <summary>
        ///     Gets the length of the byte queue
        /// </summary>
        public int Length { get; private set; }

        public byte this[int index] => _buffer[(_head + index) % _buffer.Length];

        /// <summary>
        ///     Clears the byte queue
        /// </summary>
        public void Clear()
        {
            _head = 0;
            _tail = 0;
            Length = 0;
        }

        /// <summary>
        ///     Extends the capacity of the bytequeue
        /// </summary>
        private void SetCapacity(int capacity)
        {
            byte[] newBuffer = new byte[capacity];

            if (Length > 0)
            {
                if (_head < _tail)
                {
                    _buffer.AsSpan(_head, Length).CopyTo(newBuffer.AsSpan());
                }
                else
                {
                    _buffer.AsSpan(_head, _buffer.Length - _head).CopyTo(newBuffer.AsSpan());
                    _buffer.AsSpan(0, _tail).CopyTo(newBuffer.AsSpan(_buffer.Length - _head));
                }
            }

            _head = 0;
            _tail = Length;
            _buffer = newBuffer;
        }

        public void Enqueue(Span<byte> buffer) => Enqueue(buffer, 0, buffer.Length);

        /// <summary>
        ///     Enqueues a buffer to the queue and inserts it to a correct position
        /// </summary>
        /// <param name="buffer">Buffer to enqueue</param>
        /// <param name="offset">The zero-based byte offset in the buffer</param>
        /// <param name="size">The number of bytes to enqueue</param>
        public void Enqueue(Span<byte> buffer, int offset, int size)
        {
            if (Length + size >= _buffer.Length)
            {
                SetCapacity((Length + size + 2047) & ~2047);
            }

            if (_head < _tail)
            {
                int rightLength = _buffer.Length - _tail;

                if (rightLength >= size)
                {
                    buffer.Slice(offset, size).CopyTo(_buffer.AsSpan(_tail));
                }
                else
                {
                    buffer.Slice(offset, rightLength).CopyTo(_buffer.AsSpan(_tail));
                    buffer.Slice(offset + rightLength, size - rightLength).CopyTo(_buffer.AsSpan());
                }
            }
            else
            {
                buffer.Slice(offset, size).CopyTo(_buffer.AsSpan(_tail));
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
        public int Dequeue(Span<byte> buffer, int offset, int size)
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
                _buffer.AsSpan(_head, size).CopyTo(buffer.Slice(offset));
            }
            else
            {
                int rightLength = _buffer.Length - _head;

                if (rightLength >= size)
                {
                    _buffer.AsSpan(_head, size).CopyTo(buffer.Slice(offset));
                }
                else
                {
                    _buffer.AsSpan(_head, rightLength).CopyTo(buffer.Slice(offset));
                    _buffer.AsSpan(0, size - rightLength).CopyTo(buffer.Slice(offset + rightLength));
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

        public int DequeSegment(int size, out ArraySegment<byte> segment)
        {
            if (size > Length)
            {
                size = Length;
            }

            if (size == 0)
            {
                segment = new ArraySegment<byte>();

                return 0;
            }

            if (_head >= _tail)
            {
                int rightLength = _buffer.Length - _head;
                size = Math.Min(size, rightLength);
            }

            segment = new ArraySegment<byte>(_buffer, _head, size);

            _head = (_head + size) % _buffer.Length;
            Length -= size;

            if (Length == 0)
            {
                _head = 0;
                _tail = 0;
            }

            return size;
        }
    }
}