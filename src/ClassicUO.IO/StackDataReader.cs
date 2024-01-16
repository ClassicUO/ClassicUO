using ClassicUO.Utility;
using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ClassicUO.IO
{
    public unsafe ref struct StackDataReader
    {
        private const MethodImplOptions IMPL_OPTION = MethodImplOptions.AggressiveInlining
#if !NETFRAMEWORK && !NETSTANDARD2_0
                                                      | MethodImplOptions.AggressiveOptimization
#endif
                                                      ;

        private readonly ReadOnlySpan<byte> _data;


        public StackDataReader(ReadOnlySpan<byte> data)
        {
            _data = data;
            Length = data.Length;
            Position = 0;
        }

        public int Position { get; private set; }
        public long Length { get; }
        public readonly int Remaining => (int)(Length - Position);

        public readonly IntPtr StartAddress => (IntPtr)Unsafe.AsPointer(ref GetPinnableReference());
        public readonly IntPtr PositionAddress
        {
            [MethodImpl(IMPL_OPTION)]
            get => (IntPtr)((byte*)Unsafe.AsPointer(ref GetPinnableReference()) + Position);
        }

        public readonly byte this[int index] => _data[index];

        public ReadOnlySpan<byte> Buffer => _data;


        [MethodImpl(IMPL_OPTION)]
        public readonly ref byte GetPinnableReference() => ref MemoryMarshal.GetReference(_data);


        [MethodImpl(IMPL_OPTION)]
        public void Release()
        {
            // do nothing right now.
        }

        [MethodImpl(IMPL_OPTION)]
        public void Seek(long p)
        {
            Position = (int)p;
        }

        [MethodImpl(IMPL_OPTION)]
        public void Skip(int count)
        {
            Position += count;
        }

        [MethodImpl(IMPL_OPTION)]
        public byte ReadUInt8()
        {
            if (Position + 1 > Length)
            {
                return 0;
            }

            return _data[Position++];
        }

        [MethodImpl(IMPL_OPTION)]
        public sbyte ReadInt8()
        {
            if (Position + 1 > Length)
            {
                return 0;
            }

            return (sbyte)_data[Position++];
        }

        public bool ReadBool() => ReadUInt8() != 0;

        [MethodImpl(IMPL_OPTION)]
        public ushort ReadUInt16LE()
        {
            if (Position + 2 > Length)
            {
                return 0;
            }

            BinaryPrimitives.TryReadUInt16LittleEndian(_data.Slice(Position), out ushort v);

            Skip(2);

            return v;
        }

        [MethodImpl(IMPL_OPTION)]
        public short ReadInt16LE()
        {
            if (Position + 2 > Length)
            {
                return 0;
            }

            BinaryPrimitives.TryReadInt16LittleEndian(_data.Slice(Position), out short v);

            Skip(2);

            return v;
        }

        [MethodImpl(IMPL_OPTION)]
        public uint ReadUInt32LE()
        {
            if (Position + 4 > Length)
            {
                return 0;
            }

            BinaryPrimitives.TryReadUInt32LittleEndian(_data.Slice(Position), out uint v);

            Skip(4);

            return v;
        }

        [MethodImpl(IMPL_OPTION)]
        public int ReadInt32LE()
        {
            if (Position + 4 > Length)
            {
                return 0;
            }

            int v = BinaryPrimitives.ReadInt32LittleEndian(_data.Slice(Position));

            Skip(4);

            return v;
        }

        [MethodImpl(IMPL_OPTION)]
        public ulong ReadUInt64LE()
        {
            if (Position + 8 > Length)
            {
                return 0;
            }

            BinaryPrimitives.TryReadUInt64LittleEndian(_data.Slice(Position), out ulong v);

            Skip(8);

            return v;
        }

        [MethodImpl(IMPL_OPTION)]
        public long ReadInt64LE()
        {
            if (Position + 8 > Length)
            {
                return 0;
            }

            BinaryPrimitives.TryReadInt64LittleEndian(_data.Slice(Position), out long v);

            Skip(8);

            return v;
        }





        [MethodImpl(IMPL_OPTION)]
        public ushort ReadUInt16BE()
        {
            if (Position + 2 > Length)
            {
                return 0;
            }

            BinaryPrimitives.TryReadUInt16BigEndian(_data.Slice(Position), out ushort v);

            Skip(2);

            return v;
        }

        [MethodImpl(IMPL_OPTION)]
        public short ReadInt16BE()
        {
            if (Position + 2 > Length)
            {
                return 0;
            }

            BinaryPrimitives.TryReadInt16BigEndian(_data.Slice(Position), out short v);

            Skip(2);

            return v;
        }

        [MethodImpl(IMPL_OPTION)]
        public uint ReadUInt32BE()
        {
            if (Position + 4 > Length)
            {
                return 0;
            }

            BinaryPrimitives.TryReadUInt32BigEndian(_data.Slice(Position), out uint v);

            Skip(4);

            return v;
        }

        [MethodImpl(IMPL_OPTION)]
        public int ReadInt32BE()
        {
            if (Position + 4 > Length)
            {
                return 0;
            }

            BinaryPrimitives.TryReadInt32BigEndian(_data.Slice(Position), out int v);

            Skip(4);

            return v;
        }

        [MethodImpl(IMPL_OPTION)]
        public ulong ReadUInt64BE()
        {
            if (Position + 8 > Length)
            {
                return 0;
            }

            BinaryPrimitives.TryReadUInt64BigEndian(_data.Slice(Position), out ulong v);

            Skip(8);

            return v;
        }

        [MethodImpl(IMPL_OPTION)]
        public long ReadInt64BE()
        {
            if (Position + 8 > Length)
            {
                return 0;
            }

            BinaryPrimitives.TryReadInt64BigEndian(_data.Slice(Position), out long v);

            Skip(8);

            return v;
        }

        private string ReadRawString(int length, int sizeT, bool safe)
        {
            if (length == 0 || Position + sizeT > Length)
            {
                return string.Empty;
            }

            bool fixedLength = length > 0;
            int remaining = Remaining;
            int size;

            if (fixedLength)
            {
                size = length * sizeT;

                if (size > remaining)
                {
                    size = remaining;
                }
            }
            else
            {
                size = remaining - (remaining & (sizeT - 1));
            }

            ReadOnlySpan<byte> slice = _data.Slice(Position, size);

            int index = GetIndexOfZero(slice, sizeT);
            size = index < 0 ? size : index;

            string result;

            if (size <= 0)
            {
                result = String.Empty;
            }
            else
            {
                result = StringHelper.Cp1252ToString(slice.Slice(0, size));

                if (safe)
                {
                    Span<char> buff = stackalloc char[256];
                    ReadOnlySpan<char> chars = result.AsSpan();

                    ValueStringBuilder sb = new ValueStringBuilder(buff);

                    bool hasDoneAnyReplacements = false;
                    int last = 0;
                    for (int i = 0; i < chars.Length; i++)
                    {
                        if (!StringHelper.IsSafeChar(chars[i]))
                        {
                            hasDoneAnyReplacements = true;
                            sb.Append(chars.Slice(last, i - last));
                            last = i + 1; // Skip the unsafe char
                        }
                    }

                    if (hasDoneAnyReplacements)
                    {
                        // append the rest of the string
                        if (last < chars.Length)
                        {
                            sb.Append(chars.Slice(last, chars.Length - last));
                        }

                        result = sb.ToString();
                    }

                    sb.Dispose();
                }
            }

            Position += Math.Max(size + (!fixedLength && index >= 0 ? sizeT : 0), length * sizeT);

            return result;
        }

        public string ReadASCII(bool safe = false)
        {
            return ReadRawString(-1, 1, safe);
            //return ReadString(StringHelper.Cp1252Encoding, -1, 1, safe);
        }

        public string ReadASCII(int length, bool safe = false)
        {
            return ReadRawString(length, 1, safe);

            //return ReadString(StringHelper.Cp1252Encoding, length, 1, safe);
        }

        public string ReadUnicodeBE(bool safe = false)
        {
            return ReadString(Encoding.BigEndianUnicode, -1, 2, safe);
        }

        public string ReadUnicodeBE(int length, bool safe = false)
        {
            return ReadString(Encoding.BigEndianUnicode, length, 2, safe);
        }

        public string ReadUnicodeLE(bool safe = false)
        {
            return ReadString(Encoding.Unicode, -1, 2, safe);
        }

        public string ReadUnicodeLE(int length, bool safe = false)
        {
            return ReadString(Encoding.Unicode, length, 2, safe);
        }

        public string ReadUTF8(bool safe = false)
        {
            return ReadString(Encoding.UTF8, -1, 1, safe);
        }

        public string ReadUTF8(int length, bool safe = false)
        {
            return ReadString(Encoding.UTF8, length, 1, safe);
        }

        // from modernuo <3
        private string ReadString(Encoding encoding, int length, int sizeT, bool safe)
        {
            if (length == 0 || Position + sizeT > Length)
            {
                return string.Empty;
            }

            bool fixedLength = length > 0;
            int remaining = Remaining;
            int size;

            if (fixedLength)
            {
                size = length * sizeT;

                if (size > remaining)
                {
                    size = remaining;
                }
            }
            else
            {
                size = remaining - (remaining & (sizeT - 1));
            }

            ReadOnlySpan<byte> slice = _data.Slice(Position, size);

            int index = GetIndexOfZero(slice, sizeT);
            size = index < 0 ? size : index;

            string result;

            fixed (byte* ptr = slice)
            {
                result = encoding.GetString(ptr, size);
            }

            if (safe)
            {
                Span<char> buff = stackalloc char[256];
                ReadOnlySpan<char> chars = result.AsSpan();

                ValueStringBuilder sb = new ValueStringBuilder(buff);

                bool hasDoneAnyReplacements = false;
                int last = 0;
                for (int i = 0; i < chars.Length; i++)
                {
                    if (!StringHelper.IsSafeChar(chars[i]))
                    {
                        hasDoneAnyReplacements = true;
                        sb.Append(chars.Slice(last, i - last));
                        last = i + 1; // Skip the unsafe char
                    }
                }

                if (hasDoneAnyReplacements)
                {
                    // append the rest of the string
                    if (last < chars.Length)
                    {
                        sb.Append(chars.Slice(last, chars.Length - last));
                    }

                    result = sb.ToString();
                }

                sb.Dispose();
            }

            Position += Math.Max(size + (!fixedLength && index >= 0 ? sizeT : 0), length * sizeT);

            return result;
        }

        [MethodImpl(IMPL_OPTION)]
        private static int GetIndexOfZero(ReadOnlySpan<byte> span, int sizeT)
        {
            switch (sizeT)
            {
                case 2: return MemoryMarshal.Cast<byte, char>(span).IndexOf('\0') * 2;
                case 4: return MemoryMarshal.Cast<byte, uint>(span).IndexOf((uint)0) * 4;
                default: return span.IndexOf((byte)0);
            }
        }
    }
}
