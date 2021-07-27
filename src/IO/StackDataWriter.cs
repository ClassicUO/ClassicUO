using System;
using System.Buffers.Binary;
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

            BinaryPrimitives.WriteUInt16LittleEndian(_buffer.Slice(Position), b);

            Position += 2;
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteInt16LE(short b)
        {
            EnsureSize(2);

            BinaryPrimitives.WriteInt16LittleEndian(_buffer.Slice(Position), b);

            Position += 2;
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteUInt32LE(uint b)
        {
            EnsureSize(4);

            BinaryPrimitives.WriteUInt32LittleEndian(_buffer.Slice(Position), b);

            Position += 4;
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteInt32LE(int b)
        {
            EnsureSize(4);

            BinaryPrimitives.WriteInt32LittleEndian(_buffer.Slice(Position), b);

            Position += 4;
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteUInt64LE(ulong b)
        {
            EnsureSize(8);

            BinaryPrimitives.WriteUInt64LittleEndian(_buffer.Slice(Position), b);

            Position += 8;
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteInt64LE(long b)
        {
            EnsureSize(8);

            BinaryPrimitives.WriteInt64LittleEndian(_buffer.Slice(Position), b);

            Position += 8;
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteUnicodeLE(string str)
        {
            WriteString(Encoding.Unicode, str, (str.Length + 1) * 2);
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteUnicodeLE(string str, int length)
        {
            WriteString(Encoding.Unicode, str, length * 2);
        }




        /* Big Endian */

        [MethodImpl(IMPL_OPTION)]
        public void WriteUInt16BE(ushort b)
        {
            EnsureSize(2);

            BinaryPrimitives.WriteUInt16BigEndian(_buffer.Slice(Position), b);

            Position += 2;
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteInt16BE(short b)
        {
            EnsureSize(2);

            BinaryPrimitives.WriteInt16BigEndian(_buffer.Slice(Position), b);

            Position += 2;
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteUInt32BE(uint b)
        {
            EnsureSize(4);

            BinaryPrimitives.WriteUInt32BigEndian(_buffer.Slice(Position), b);

            Position += 4;
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteInt32BE(int b)
        {
            EnsureSize(4);

            BinaryPrimitives.WriteInt32BigEndian(_buffer.Slice(Position), b);

            Position += 4;
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteUInt64BE(ulong b)
        {
            EnsureSize(8);

            BinaryPrimitives.WriteUInt64BigEndian(_buffer.Slice(Position), b);

            Position += 8;
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteInt64BE(long b)
        {
            EnsureSize(8);

            BinaryPrimitives.WriteInt64BigEndian(_buffer.Slice(Position), b);

            Position += 8;
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteUnicodeBE(string str)
        {
            WriteString(Encoding.BigEndianUnicode, str, (str.Length + 1) * 2);
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteUnicodeBE(string str, int length)
        {
            WriteString(Encoding.BigEndianUnicode, str, length * 2);
        }

        



        [MethodImpl(IMPL_OPTION)]
        public void WriteUTF8(string str, int len)
        {
            WriteString(Encoding.UTF8, str, len);
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteASCII(string str)
        {
            WriteString(Encoding.ASCII, str, str.Length + 1);
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteASCII(string str, int length)
        {
            WriteString(Encoding.ASCII, str, length);
        }


        [MethodImpl(IMPL_OPTION)]
        public void WriteZero(int count)
        {
            if (count > 0)
            {
                EnsureSize(count);

                _buffer.Slice(Position, count).Fill(0);

                Position += count;
            }
        }

        [MethodImpl(IMPL_OPTION)]
        public void Write(ReadOnlySpan<byte> span)
        {
            EnsureSize(span.Length);

            span.CopyTo(_buffer.Slice(Position));

            Position += span.Length;
        }

        private void WriteString(Encoding encoding, string str, int length)
        {
            if (str == null)
            {
                str = string.Empty;
            }

            EnsureSize(length);

            int size = Math.Min(length, str.Length);

            int processed = encoding.GetBytes
            (
                str,
                0,
                size,
                _allocatedBuffer,
                Position
            );

            Position += processed;

            WriteZero(length - processed);
        }

        [MethodImpl(IMPL_OPTION)]
        private void EnsureSize(int size)
        {
            if (Position + size > _buffer.Length)
            {
                Rent(Math.Max(BytesWritten + size, _buffer.Length * 2));
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
