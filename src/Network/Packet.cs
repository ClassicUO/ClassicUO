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
using System.Text;
using ClassicUO.Utility;

namespace ClassicUO.Network
{
    unsafe ref struct BufferReaderUnmanaged<T> where T : unmanaged
    {
        public BufferReaderUnmanaged(T* data, int length)
        {
            ptr = data;
            Length = length;
        }

        public T* ptr;
        public int Length;

        public T this[int index] => ptr[index];
    }

    unsafe ref struct BufferWrapper<T> where T : struct
    {
        public BufferWrapper(T[] data, int length)
        {
            ptr = data;
            Length = length;
        }

        public BufferWrapper(T[] data) : this(data, data.Length)
        {
        }


        public T[] ptr;
        public int Length;

        public ref T this[int index] => ref ptr[index];
    }

    ref struct PacketBufferReader
    {
        private readonly BufferWrapper<byte> _buffer;


        public PacketBufferReader(byte[] data) : this(data, data.Length)
        {
        }

        public PacketBufferReader(byte[] data, int length)
        {
            _buffer = new BufferWrapper<byte>(data, length);
            Position = 0;
            Length = length;
        }


        public int Position;
        public int Length;

        public int Remains => Length - Position;
        public ref byte this[int index] => ref _buffer[index];
        public byte[] Buffer => _buffer.ptr;
        public byte ID => this[0];


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte() => Position + 1 > Length ? (byte) 0 : _buffer[Position++];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte ReadSByte() => (sbyte) ReadByte();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadBool() => ReadByte() != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUShort() => (ushort) (Position + 2 > Length ? 0 : ((ReadByte() << 8) | ReadByte()));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt() => (uint) (Position + 4 > Length ? 0 : ((ReadByte() << 24) | (ReadByte() << 16) | (ReadByte() << 8) | ReadByte()));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadASCII()
        {
            if (Position + 1 > Length)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();

            char c;

            while ((c = (char) ReadByte()) != 0)
            {
                sb.Append(c);
            }

            return sb.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadASCII(int length)
        {
            if (Position + length > Length)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < length; ++i)
            {
                char b = (char) ReadByte();

                if (b == '\0')
                {
                    Skip(length - i - 1);

                    break;
                }

                sb.Append(b);
            }

            return sb.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadUnicode()
        {
            if (Position + 1 > Length)
            {
                return string.Empty;
            }

            int start = Position;

            while (ReadUShort() != 0)
            {
            }

            return Position == start ? string.Empty : Encoding.BigEndianUnicode.GetString(_buffer.ptr, start, Position - start);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadUnicode(int length)
        {
            if (Position + length > Length)
            {
                return string.Empty;
            }

            int start = Position;
            Position += length;

            return length <= 0 ? string.Empty : Encoding.BigEndianUnicode.GetString(_buffer.ptr, start, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadUnicodeReversed()
        {
            if (Position + 2 > Length)
            {
                return string.Empty;
            }

            int start = Position;

            while (ReadUShortReversed() != 0)
            {
            }

            return start == Position ? string.Empty : Encoding.Unicode.GetString(_buffer.ptr, start, Position - start);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadUnicodeReversed(int length)
        {
            if (Position + length > Length)
            {
                return string.Empty;
            }

            int start = Position;
            int i = 0;

            for (; i < length; i += 2)
            {
                if (ReadUShortReversed() == 0)
                {
                    break;
                }
            }

            Seek(start + length);

            return i <= 0 ? string.Empty : Encoding.Unicode.GetString(_buffer.ptr, start, i);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUShortReversed() =>
            (ushort) (Position + 2 > Length ? 0 : (ReadByte() | (ReadByte() << 8)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadUTF8StringSafe()
        {
            if (Position >= Length)
            {
                return string.Empty;
            }

            int index = Position;

            while (index < Length)
            {
                byte b = _buffer[index++];

                if (b == 0)
                {
                    break;
                }
            }

            string s = Encoding.UTF8.GetString(_buffer.ptr, Position, index - Position - 1);

            Seek(index);

            index = 0;

            for (int i = 0; i < s.Length && StringHelper.IsSafeChar(s[i]); i++, index++)
            {
            }

            if (index == s.Length)
            {
                return s;
            }

            StringBuilder sb = new StringBuilder(s.Length);

            for (int i = 0; i < s.Length; i++)
            {
                if (StringHelper.IsSafeChar(s[i]))
                {
                    sb.Append(s[i]);
                }
            }

            return sb.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadUTF8StringSafe(int length)
        {
            if (Position + length > Length)
            {
                return string.Empty;
            }

            int index = Position;
            int toRead = Position + length;

            while (index < toRead)
            {
                byte b = _buffer[index++];

                if (b == 0)
                {
                    break;
                }
            }

            string s = Encoding.UTF8.GetString(_buffer.ptr, Position, length - 1);

            Skip(length);

            index = 0;

            for (int i = 0; i < s.Length && StringHelper.IsSafeChar(s[i]); i++, index++)
            {
            }

            if (index == s.Length)
            {
                return s;
            }

            StringBuilder sb = new StringBuilder(s.Length);

            for (int i = 0; i < s.Length; i++)
            {
                if (StringHelper.IsSafeChar(s[i]))
                {
                    sb.Append(s[i]);
                }
            }

            return sb.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] ReadArray(int count)
        {
            byte[] data = new byte[count];

            for (int i = 0; i < count; i++)
            {
                data[i] = ReadByte();
            }

            return data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArraySegment<byte> Slice(int count)
        {
            return Slice(Position, Math.Min(count, Length - 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArraySegment<byte> Slice(int start, int count)
        {
            if (count >= Length)
            {
                count = Length - 1;
            }

            return new ArraySegment<byte>(_buffer.ptr, start, count);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Skip(int length) => Position += length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Seek(int position) => Position = position;
    }
}