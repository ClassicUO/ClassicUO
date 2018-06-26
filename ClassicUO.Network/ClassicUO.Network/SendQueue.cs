using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Network
{
    public class SendQueue
    {
        private const int PendingCap = 256 * 1024;

        private static readonly int _CoalesceBufferSize = 512;
        private static readonly BufferPool _UnusedBuffers = new BufferPool(2048, _CoalesceBufferSize);

        private readonly Queue<Gram> _pending;

        private Gram _buffered;

        public SendQueue()
        {
            _pending = new Queue<Gram>();
        }

        public bool IsFlushReady
        {
            get { return _pending.Count == 0 && _buffered != null; }
        }

        public bool IsEmpty
        {
            get { return _pending.Count == 0 && _buffered == null; }
        }

        public static byte[] AcquireBuffer()
        {
            lock (_UnusedBuffers)
                return _UnusedBuffers.GetFreeSegment();
        }

        public static void ReleaseBuffer(byte[] buffer)
        {
            lock (_UnusedBuffers)
                if (buffer != null && buffer.Length == _CoalesceBufferSize)
                {
                    _UnusedBuffers.AddFreeSegment(buffer);
                }
        }

        public Gram CheckFlushReady()
        {
            Gram gram = _buffered;
            _pending.Enqueue(_buffered);
            _buffered = null;
            return gram;
        }

        public Gram Dequeue()
        {
            Gram gram = null;

            if (_pending.Count > 0)
            {
                _pending.Dequeue().Release();

                if (_pending.Count > 0)
                {
                    gram = _pending.Peek();
                }
            }

            return gram;
        }

        public Gram Enqueue(byte[] buffer, int length)
        {
            return Enqueue(buffer, 0, length);
        }

        public Gram Enqueue(byte[] buffer, int offset, int length)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (!(offset >= 0 && offset < buffer.Length))
            {
                throw new ArgumentOutOfRangeException(
                    "offset", offset,
                    "Offset must be greater than or equal to zero and less than the size of the buffer.");
            }
            if (length < 0 || length > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(
                    "length", length, "Length cannot be less than zero or greater than the size of the buffer.");
            }
            if (buffer.Length - offset < length)
            {
                throw new ArgumentException("Offset and length do not point to a valid segment within the buffer.");
            }

            int existingBytes = _pending.Count * _CoalesceBufferSize + (_buffered?.Length ?? 0);

            if (existingBytes + length > PendingCap)
            {
                throw new CapacityExceededException();
            }

            Gram gram = null;

            while (length > 0)
            {
                if (_buffered == null)
                {
                    // nothing yet buffered
                    _buffered = Gram.Acquire();
                }

                int bytesWritten = _buffered.Write(buffer, offset, length);

                offset += bytesWritten;
                length -= bytesWritten;

                if (_buffered.IsFull)
                {
                    if (_pending.Count == 0)
                    {
                        gram = _buffered;
                    }

                    _pending.Enqueue(_buffered);
                    _buffered = null;
                }
            }

            return gram;
        }

        public void Clear()
        {
            if (_buffered != null)
            {
                _buffered.Release();
                _buffered = null;
            }

            while (_pending.Count > 0)
            {
                _pending.Dequeue().Release();
            }
        }

        public class Gram
        {
            private static readonly Stack<Gram> _Pool = new Stack<Gram>();

            private Gram()
            {
            }

            public byte[] Buffer { get; private set; }

            public int Length { get; private set; }

            public int Available
            {
                get { return Buffer.Length - Length; }
            }

            public bool IsFull
            {
                get { return Length == Buffer.Length; }
            }

            public static Gram Acquire()
            {
                lock (_Pool)
                {
                    Gram gram;

                    if (_Pool.Count > 0)
                    {
                        gram = _Pool.Pop();
                    }
                    else
                    {
                        gram = new Gram();
                    }

                    gram.Buffer = AcquireBuffer();
                    gram.Length = 0;

                    return gram;
                }
            }

            public int Write(byte[] buffer, int offset, int length)
            {
                int write = Math.Min(length, Available);

                System.Buffer.BlockCopy(buffer, offset, Buffer, Length, write);

                Length += write;

                return write;
            }

            public void Release()
            {
                lock (_Pool)
                {
                    _Pool.Push(this);
                    ReleaseBuffer(Buffer);
                }
            }
        }
    }

    [Serializable]
    public sealed class CapacityExceededException : Exception
    {
        public CapacityExceededException()
            : base("Too much data pending.")
        {
        }
    }
}
