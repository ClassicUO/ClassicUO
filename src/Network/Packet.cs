#region license

// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

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
        public ushort ReadUShort() => (ushort) (Position + 2 > Length ?  0 : ((ReadByte() << 8) | ReadByte()));

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

            return Position == start ?
                string.Empty :
                Encoding.BigEndianUnicode.GetString(_buffer.ptr, start, Position - start);
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