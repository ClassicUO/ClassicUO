using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.IO
{
    ref struct StackDataWriter
    {
        private const MethodImplOptions IMPL_OPTION = MethodImplOptions.AggressiveInlining
#if !NETFRAMEWORK && !NETSTANDARD2_0
                                                      | MethodImplOptions.AggressiveOptimization
#endif
            ;


        private byte[] _allocatedBuffer;
        private Span<byte> _buffer;
        private int _position;


        public StackDataWriter(int initialCapacity)
        {
            this = default;

            Position = 0;

            EnsureSize(initialCapacity);
        }

        public StackDataWriter(Span<byte> span)
        {
            this = default;

            Write(span);
        }


        public byte[] AllocatedBuffer => _allocatedBuffer;
        public Span<byte> RawBuffer => _buffer;
        public ReadOnlySpan<byte> Buffer => _buffer.Slice(0, Position);
        public int Position
        {
            [MethodImpl(IMPL_OPTION)]
            get => _position;

            [MethodImpl(IMPL_OPTION)]
            set
            {
                _position = value;
                BytesWritten = Math.Max(value, BytesWritten);
            }
        }

        public int BytesWritten { get; private set; }



        [MethodImpl(IMPL_OPTION)]
        public void Seek(int position, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:

                    Position = position;

                    break;

                case SeekOrigin.Current:

                    Position += position;

                    break;

                case SeekOrigin.End:

                    Position = BytesWritten + position;

                    break;
            }

            EnsureSize(Position - _buffer.Length + 1);
        }


        [MethodImpl(IMPL_OPTION)]
        public void WriteUInt8(byte b)
        {
            EnsureSize(1);

            _buffer[Position] = b;

            Position += 1;
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteInt8(sbyte b)
        {
            EnsureSize(1);

            _buffer[Position] = (byte) b;

            Position += 1;
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteBool(bool b)
        {
            WriteUInt8(b ? (byte) 0x01 : (byte) 0x00);
        }



        /* Little Endian */

        [MethodImpl(IMPL_OPTION)]
        public void WriteUInt16LE(ushort b)
        {
            EnsureSize(2);

            _buffer[Position] = (byte) b;
            _buffer[Position + 1] = (byte) (b >> 8);

            Position += 2;
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteInt16LE(short b)
        {
            EnsureSize(2);

            _buffer[Position] = (byte)b;
            _buffer[Position + 1] = (byte)(b >> 8);

            Position += 2;
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteUInt32LE(uint b)
        {
            EnsureSize(4);

            _buffer[Position] = (byte)b;
            _buffer[Position + 1] = (byte)(b >> 8);
            _buffer[Position + 2] = (byte)(b >> 16);
            _buffer[Position + 3] = (byte)(b >> 24);

            Position += 4;
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteInt32LE(int b)
        {
            EnsureSize(4);

            _buffer[Position] = (byte)b;
            _buffer[Position + 1] = (byte)(b >> 8);
            _buffer[Position + 2] = (byte)(b >> 16);
            _buffer[Position + 3] = (byte)(b >> 24);

            Position += 4;
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteUnicodeLE(string str)
        {
            EnsureSize((str.Length + 1) * 2);

            for (int i = 0; i < str.Length; ++i)
            {
                char c = str[i];

                if (c != '\0')
                {
                    WriteUInt16LE(c);
                }
            }

            WriteUInt16LE(0x00);
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteUnicodeLE(string str, int length)
        {
            EnsureSize(length);

            for (int i = 0; i < length && i < str.Length; ++i)
            {
                char c = str[i];

                if (c != '\0')
                {
                    WriteUInt16LE(c);
                }
            }

            for (int i = str.Length; i < length; ++i)
            {
                WriteUInt16LE(0x00);
            }
        }




        /* Big Endian */

        [MethodImpl(IMPL_OPTION)]
        public void WriteUInt16BE(ushort b)
        {
            EnsureSize(2);

            _buffer[Position] = (byte)(b >> 8);
            _buffer[Position + 1] = (byte)b;

            Position += 2;
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteInt16BE(short b)
        {
            EnsureSize(2);

            _buffer[Position] = (byte)(b >> 8);
            _buffer[Position + 1] = (byte)b;

            Position += 2;
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteUInt32BE(uint b)
        {
            EnsureSize(4);

            _buffer[Position] = (byte)(b >> 24);
            _buffer[Position + 1] = (byte)(b >> 16);
            _buffer[Position + 2] = (byte)(b >> 8);
            _buffer[Position + 3] = (byte)b;

            Position += 4;
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteInt32BE(int b)
        {
            EnsureSize(4);

            _buffer[Position] = (byte)(b >> 24);
            _buffer[Position + 1] = (byte)(b >> 16);
            _buffer[Position + 2] = (byte)(b >> 8);
            _buffer[Position + 3] = (byte)b;

            Position += 4;
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteUnicodeBE(string str)
        {
            EnsureSize((str.Length + 1) * 2);

            for (int i = 0; i < str.Length; ++i)
            {
                char c = str[i];

                if (c != '\0')
                {
                    WriteUInt16BE(c);
                }
            }

            WriteUInt16BE(0x00);
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteUnicodeBE(string str, int length)
        {
            EnsureSize(length);

            for (int i = 0; i < length && i < str.Length; ++i)
            {
                char c = str[i];

                if (c != '\0')
                {
                    WriteUInt16BE(c);
                }
            }

            for (int i = str.Length; i < length; ++i)
            {
                WriteUInt16BE(0x00);
            }
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteUTF8(string str, int len)
        {
            if (str == null)
            {
                str = string.Empty;
            }

            int size = Math.Min(len, str.Length);

            byte[] buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(size);

            try
            {
                EnsureSize(len);

                Encoding.UTF8.GetBytes
                (
                    str,
                    0,
                    size,
                    buffer,
                    0
                );

                for (int i = 0; i < len; ++i)
                {
                    if (i < size)
                    {
                        WriteUInt8(buffer[i]);
                    }
                    else
                    {
                        WriteUInt8(0x00);
                    }
                }
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
            }
        }




        [MethodImpl(IMPL_OPTION)]
        public void WriteZero(int count)
        {
            if (count > 0)
            {
                EnsureSize(count);

                for (int i = 0; i < count; ++i)
                {
                    WriteUInt8(0x00);
                }
            }
        }


        [MethodImpl(IMPL_OPTION)]
        public void Write(ReadOnlySpan<byte> span)
        {
            EnsureSize(span.Length);

            span.CopyTo(_buffer.Slice(Position));

            Position += span.Length;
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteASCII(string str)
        {
            EnsureSize(str.Length + 1);

            for (int i = 0; i < str.Length; ++i)
            {
                char c = str[i];

                if (c != '\0')
                {
                    WriteUInt8((byte)c);
                }
            }

            WriteUInt8(0x00);
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteASCII(string str, int length)
        {
            EnsureSize(length);

            for (int i = 0; i < length && i < str.Length; ++i)
            {
                char c = str[i];

                if (c != '\0')
                {
                    WriteUInt8((byte) c);
                }
            }

            for (int i = str.Length; i < length; ++i)
            {
                WriteUInt8(0x00);
            }
        }

        [MethodImpl(IMPL_OPTION)]
        private void EnsureSize(int size)
        {
            if (Position + size > _buffer.Length)
            {
                Rent(Math.Max(size, _buffer.Length * 2));
            }
        }

        [MethodImpl(IMPL_OPTION)]
        private void Rent(int size)
        {
            byte[] newBuffer = System.Buffers.ArrayPool<byte>.Shared.Rent(size);

            if (_allocatedBuffer != null)
            {
                _buffer.Slice(0, BytesWritten).CopyTo(newBuffer);

                Return();
            }

            _buffer = _allocatedBuffer = newBuffer;
        }

        [MethodImpl(IMPL_OPTION)]
        private void Return()
        {
            if (_allocatedBuffer != null)
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(_allocatedBuffer);

                _allocatedBuffer = null;
            }
        }

        [MethodImpl(IMPL_OPTION)]
        public void Dispose()
        {
            Return();
        }
    }
}
