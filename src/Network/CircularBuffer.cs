#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

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

        /// <summary>
        ///     Enqueues a buffer to the queue and inserts it to a correct position
        /// </summary>
        /// <param name="buffer">Buffer to enqueue</param>
        /// <param name="offset">The zero-based byte offset in the buffer</param>
        /// <param name="size">The number of bytes to enqueue</param>
        internal void Enqueue(Span<byte> buffer, int offset, int size)
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
        internal int Dequeue(Span<byte> buffer, int offset, int size)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetID()
        {
            if (Length >= 1)
            {
                return _buffer[_head];
            }

            return 0xFF;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetLength()
        {
            if (Length >= 3)
            {
                return _buffer[(_head + 2) % _buffer.Length] | (_buffer[(_head + 1) % _buffer.Length] << 8);
            }

            return 0;
        }
    }
}