#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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

using static System.String;

namespace ClassicUO.Network
{
    internal sealed class Packet : PacketBase
    {
        private byte[] _data;

        public Packet(byte[] data, int length)
        {
            _data = data;
            Length = length;
            IsDynamic = PacketsTable.GetPacketLength(ID) < 0;
        }

        protected override byte this[int index]
        {
            [MethodImpl(256)]
            get
            {
                if (index < 0 || index >= Length) throw new ArgumentOutOfRangeException("index");

                return _data[index];
            }
            [MethodImpl(256)]
            set
            {
                if (index < 0 || index >= Length) throw new ArgumentOutOfRangeException("index");

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
                return 0;

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
                return 0;

            return (ushort) ((ReadByte() << 8) | ReadByte());
        }

        public uint ReadUInt()
        {
            if (EnsureSize(4))
                return 0;

            return (uint) ((ReadByte() << 24) | (ReadByte() << 16) | (ReadByte() << 8) | ReadByte());
        }

        public string ReadASCII()
        {
            if (EnsureSize(1))
                return Empty;

            int start = Position;
            int end = 0;
            while (Position < Length)
            {
                if (_data[Position++] == 0)
                    break;
                end++;
            }
            return end == 0 ? Empty : StringHelper.AsciiEncoding.GetString(_data, start, end);
        }

        public string ReadASCII(int length, bool exitIfNull = false)
        {
            if (EnsureSize(length))
                return Empty;

            if (Position + length >= Length)
            {
                length = Length - Position - 1;
            }
            int start = Position;
            Position += length;

            if (length <= 0)
                return string.Empty;

            var str = StringHelper.AsciiEncoding.GetString(_data, start, length);
            var nulIndex = str.IndexOf('\0');
            return nulIndex <= 0 ? str : str.Substring(0, nulIndex);
        }

        public string ReadUTF8StringSafe()
        {
            if (Position >= Length) return Empty;

            int count = 0;
            int index = Position;

            while (index < Length && _data[index++] != 0) ++count;

            index = 0;

            var buffer = new byte[count];
            int val = 0;

            while (Position < Length && (val = _data[Position++]) != 0) buffer[index++] = (byte) val;

            string s = Encoding.UTF8.GetString(buffer);

            bool isSafe = true;

            for (int i = 0; isSafe && i < s.Length; ++i) isSafe = StringHelper.IsSafeChar(s[i]);

            if (isSafe) return s;

            StringBuilder sb = new StringBuilder(s.Length);

            for (int i = 0; i < s.Length; ++i)
                if (StringHelper.IsSafeChar(s[i]))
                    sb.Append(s[i]);

            return sb.ToString();
        }

        public string ReadUnicode()
        {
            if (EnsureSize(2))
                return Empty;

            int start = Position;
            int end = 0;
            while (Position < Length)
            {
                if (ReadUShort() == 0)
                    break;
                end += 2;
            }

            return end == 0 ? Empty : Encoding.BigEndianUnicode.GetString(_data, start, end);
        }

        public string ReadUnicode(int length)
        {
            if (EnsureSize(length))
                return Empty;

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
                return null;

            byte[] array = new byte[count];
            Buffer.BlockCopy(_data, Position, array, 0, count);
            Position += count;

            return array;
        }

        public string ReadUnicodeReversed(int length)
        {
            if (EnsureSize(length))
                return Empty;

            if (Position + length >= Length)
            {
                length = Length - Position - 2;
            }
            int start = Position;
            Position += length;

            return length <= 0 ? Empty : Encoding.Unicode.GetString(_data, start, length);
        }

        public string ReadUnicodeReversed()
        {
            if (EnsureSize(2))
                return Empty;

            int start = Position;
            int end = 0;
            while (Position < Length)
            {
                if (ReadUShortReversed() == 0)
                    break;
                end += 2;
            }

            return end == 0 ? Empty : Encoding.Unicode.GetString(_data, start, end);
        }

        public ushort ReadUShortReversed()
        {
            if (EnsureSize(2))
                return 0;

            return (ushort) (ReadByte() | (ReadByte() << 8));
        }
    }
}