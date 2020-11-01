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
using System.Runtime.InteropServices;
using System.Text;
using ClassicUO.Utility;
using static System.String;

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

    unsafe ref struct BufferReader<T> where T : struct
    {
        public BufferReader(T[] data, int length)
        {
            ptr = data;
            Length = length;
        }

        public BufferReader(T[] data) : this(data, data.Length)
        {
            
        }


        public T[] ptr;
        public int Length;

        public ref T this[int index] => ref ptr[index];
    }

    unsafe ref struct PacketBufferReader
    {
        private readonly BufferReader<byte> _buffer;


        public PacketBufferReader(byte[] data) : this(data, data.Length)
        {
        }

        public PacketBufferReader(byte[] data, int length)
        {
            _buffer = new BufferReader<byte>(data, length);
            Position = 0;
            Length = length;
        }

       

        public int Position;
        public int Length;
        public int Remains => Length - Position;


        public ref byte this[int index] => ref _buffer[index];


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte() => Position + sizeof(byte) > Length ? (byte) 0 : _buffer[Position++];

        public sbyte ReadSByte() => (sbyte) ReadByte();

        public bool ReadBool() => ReadByte() != 0;

        public ushort ReadUShort() => (ushort) (Position + sizeof(ushort) > Length ?  0 : ((ReadByte() << 8) | ReadByte()));

        public uint ReadUInt() => (uint) (Position + sizeof(uint) > Length ? 0 : ((ReadByte() << 24) | (ReadByte() << 16) | (ReadByte() << 8) | ReadByte()));

        public string ReadASCII()
        {
            if (Position + sizeof(char) > Length)
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

        public string ReadASCII(int length)
        {
            if (Position + length >= Length)
            {
                return string.Empty;
            }

            if (Position + sizeof(char) * length > Length)
            {
                length = Length - Position - sizeof(char);
            }

            if (length <= 0)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < length; ++i)
            {
                byte b = ReadByte();

                if (b == 0)
                {
                    Skip(length - i - sizeof(char));

                    break;
                }

                sb.Append((char) b);
            }

            return sb.ToString();
        }

        public string ReadUnicode()
        {
            if (Position + sizeof(ushort) > Length)
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

        public string ReadUnicode(int length)
        {
            if (Position + length >= Length)
            {
                return string.Empty;
            }

            if (Position + length * sizeof(ushort) > Length)
            {
                length = Length - Position - sizeof(ushort);
            }

            int start = Position;
            Position += length;

            return length <= 0 ? string.Empty : Encoding.BigEndianUnicode.GetString(_buffer.ptr, start, length);
        }

        public string ReadUnicodeReversed()
        {
            if (Position + sizeof(ushort) > Length)
            {
                return string.Empty;
            }

            int start = Position;

            while (ReadUShortReversed() != 0)
            {

            }

            return start == Position ? string.Empty : Encoding.Unicode.GetString(_buffer.ptr, start, Position - start);
        }

        public string ReadUnicodeReversed(int length)
        {
            if (Position + length >= Length)
            {
                return string.Empty;
            }

            if (Position + length * sizeof(ushort) > Length)
            {
                length = Length - Position - sizeof(ushort);
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

        public ushort ReadUShortReversed() =>
            (ushort) (Position + sizeof(ushort) > Length ? 0 : (ReadByte() | (ReadByte() << 8)));



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Skip(int length) => Position += length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Seek(int position) => Position = position;
    }

    internal sealed class Packet : PacketBase
    {
        private static readonly StringBuilder _sb = new StringBuilder();
        private byte[] _data;

        public Packet(byte[] data, int length)
        {
            PacketBufferReader buff = new PacketBufferReader(data, length);
            var b = buff.ReadByte();

            _data = data;
            Length = length;
            IsDynamic = PacketsTable.GetPacketLength(ID) < 0;
        }

        public override byte this[int index]
        {
            [MethodImpl(256)]
            get
            {
                if (index < 0 || index >= Length)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                return _data[index];
            }
            [MethodImpl(256)]
            set
            {
                if (index < 0 || index >= Length)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                _data[index] = value;
                IsChanged = true;
            }
        }

        public override int Length { get; }

        public bool IsChanged { get; private set; }

        public bool Filter { get; set; }

        public override ref byte[] ToArray()
        {
            return ref _data;
        }

        [MethodImpl(256)]
        public void MoveToData()
        {
            Seek(IsDynamic ? 3 : 1);
        }

        [MethodImpl(256)]
        protected override bool EnsureSize(int length)
        {
            return length < 0 || Position + length > Length;
        }

        [MethodImpl(256)]
        public byte ReadByte()
        {
            if (EnsureSize(1))
            {
                return 0;
            }

            return _data[Position++];
        }

        public sbyte ReadSByte()
        {
            return (sbyte) ReadByte();
        }

        public bool ReadBool()
        {
            return ReadByte() != 0;
        }

        public ushort ReadUShort()
        {
            if (EnsureSize(2))
            {
                return 0;
            }

            return (ushort) ((ReadByte() << 8) | ReadByte());
        }

        public uint ReadUInt()
        {
            if (EnsureSize(4))
            {
                return 0;
            }

            return (uint) ((ReadByte() << 24) | (ReadByte() << 16) | (ReadByte() << 8) | ReadByte());
        }

        public ulong ReadULong()
        {
            if (EnsureSize(8))
            {
                return 0;
            }

            return (ulong) ((ReadByte() << 56) | (ReadByte() << 48) | (ReadByte() << 40) | (ReadByte() << 32) |
                            (ReadByte() << 24) | (ReadByte() << 16) | (ReadByte() << 8) | ReadByte());
        }

        public string ReadASCII()
        {
            if (EnsureSize(1))
            {
                return Empty;
            }

            _sb.Clear();

            char c;

            while ((c = (char) ReadByte()) != 0)
            {
                _sb.Append(c);
            }

            return _sb.ToString();
        }

        public string ReadASCII(int length)
        {
            if (EnsureSize(length))
            {
                return Empty;
            }

            if (Position + length > Length)
            {
                length = Length - Position - 1;
            }


            _sb.Clear();

            if (length <= 0)
            {
                return Empty;
            }

            for (int i = 0; i < length; i++)
            {
                char c = (char) ReadByte();

                if (c == '\0')
                {
                    Skip(length - i - 1);

                    break;
                }

                _sb.Append(c);
            }

            return _sb.ToString();
        }

        public string ReadUTF8StringSafe()
        {
            _sb.Clear();

            if (Position >= Length)
            {
                return Empty;
            }

            int index = Position;

            while (index < Length)
            {
                byte b = _data[index++];

                if (b == 0)
                {
                    break;
                }
            }

            string s = Encoding.UTF8.GetString(_data, Position, index - Position - 1);

            Seek(index);

            index = 0;

            for (int i = 0; i < s.Length && StringHelper.IsSafeChar(s[i]); i++, index++)
            {
            }

            if (index == s.Length)
            {
                return s;
            }

            for (int i = 0; i < s.Length; i++)
            {
                if (StringHelper.IsSafeChar(s[i]))
                {
                    _sb.Append(s[i]);
                }
            }

            return _sb.ToString();
        }

        public string ReadUTF8StringSafe(int length)
        {
            _sb.Clear();

            if (length <= 0 || EnsureSize(length))
            {
                return Empty;
            }

            if (Position + length > Length)
            {
                length = Length - Position - 1;
            }

            int index = Position;
            int toread = Position + length;

            while (index < toread)
            {
                byte b = _data[index++];

                if (b == 0)
                {
                    break;
                }
            }

            string s = Encoding.UTF8.GetString(_data, Position, length - 1);

            Skip(length);

            index = 0;

            for (int i = 0; i < s.Length && StringHelper.IsSafeChar(s[i]); i++, index++)
            {
            }

            if (index == s.Length)
            {
                return s;
            }

            for (int i = 0; i < s.Length; i++)
            {
                if (StringHelper.IsSafeChar(s[i]))
                {
                    _sb.Append(s[i]);
                }
            }

            return _sb.ToString();
        }

        public string ReadUnicode()
        {
            if (EnsureSize(2))
            {
                return Empty;
            }

            int start = Position;
            int end = 0;

            while (Position < Length)
            {
                if (ReadUShort() == 0)
                {
                    break;
                }

                end += 2;
            }

            return end == 0 ? Empty : Encoding.BigEndianUnicode.GetString(_data, start, end);
        }

        public string ReadUnicode(int length)
        {
            if (EnsureSize(length))
            {
                return Empty;
            }

            if (Position + length >= Length)
            {
                length = Length - Position - 2;
            }

            int start = Position;
            Position += length;

            return length <= 0 ? Empty : Encoding.BigEndianUnicode.GetString(_data, start, length);
        }

        public byte[] ReadArray(int count)
        {
            if (EnsureSize(count))
            {
                return null;
            }

            byte[] array = new byte[count];
            Buffer.BlockCopy(_data, Position, array, 0, count);
            Position += count;

            return array;
        }

        public string ReadUnicodeReversed(int length, bool safe = true)
        {
            if (EnsureSize(length))
            {
                return Empty;
            }

            if (Position + length >= Length)
            {
                length = Length - Position - 2;
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

            return i <= 0 ? Empty : Encoding.Unicode.GetString(_data, start, i);
        }

        public string ReadUnicodeReversed()
        {
            if (EnsureSize(2))
            {
                return Empty;
            }

            int start = Position;
            int end = 0;

            while (Position < Length)
            {
                if (ReadUShortReversed() == 0)
                {
                    break;
                }

                end += 2;
            }

            return end == 0 ? Empty : Encoding.Unicode.GetString(_data, start, end);
        }

        public ushort ReadUShortReversed()
        {
            if (EnsureSize(2))
            {
                return 0;
            }

            return (ushort) (ReadByte() | (ReadByte() << 8));
        }
    }
}