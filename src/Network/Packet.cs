#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
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

namespace ClassicUO.Network
{
    public sealed class Packet : PacketBase
    {
        private readonly byte[] _data;

        public Packet(byte[] data, int length)
        {
            _data = data;
            Length = length;
            IsDynamic = PacketsTable.GetPacketLength(ID) < 0;
        }

        protected override byte this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (index < 0 || index >= Length) throw new ArgumentOutOfRangeException("index");

                return _data[index];
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        public override byte[] ToArray()
        {
            return _data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MoveToData()
        {
            Seek(IsDynamic ? 3 : 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void EnsureSize(int length)
        {
            if (length < 0 || Position + length > Length) throw new ArgumentOutOfRangeException("length");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte()
        {
            EnsureSize(1);

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
            EnsureSize(2);
            return (ushort) ((ReadByte() << 8) | ReadByte());
        }

        public uint ReadUInt()
        {
            EnsureSize(4);
            return (uint) ((ReadByte() << 24) | (ReadByte() << 16) | (ReadByte() << 8) | ReadByte());
        }

        public string ReadASCII()
        {
            EnsureSize(1);
            StringBuilder sb = new StringBuilder();
            char c;
            while ((c = (char) ReadByte()) != '\0') sb.Append(c);

            return sb.ToString();
        }

        public string ReadASCII(int length, bool exitIfNull = false)
        {
            EnsureSize(length);
            StringBuilder sb = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                char c = (char) ReadByte();

                if (c != '\0')
                    sb.Append(c);
                else if (exitIfNull)
                    break;
            }

            return sb.ToString();
        }

        public string ReadUnicode()
        {
            EnsureSize(2);
            StringBuilder sb = new StringBuilder();
            char c;
            while ((c = (char) ReadUShort()) != '\0') sb.Append(c);

            return sb.ToString();
        }

        public string ReadUnicode(int length)
        {
            EnsureSize(length);
            StringBuilder sb = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                char c = (char) ReadUShort();
                if (c != '\0') sb.Append(c);
            }

            return sb.ToString();
        }

        public string ReadUTF8String()
        {
            StringBuilder sb = new StringBuilder();

            char c;
            while ((c = (char)ReadByte()) != '\0') sb.Append(c);

            return sb.ToString();
        }

        public byte[] ReadArray(int count)
        {
            EnsureSize(count);
            byte[] array = new byte[count];
            Buffer.BlockCopy(_data, Position, array, 0, count);
            Position += count;

            return array;
        }

        public string ReadUnicodeReversed(int length)
        {
            EnsureSize(length);
            length /= 2;
            StringBuilder sb = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                char c = (char) ReadUShortReversed();
                if (c != '\0') sb.Append(c);
            }

            return sb.ToString();
        }

        public string ReadUnicodeReversed()
        {
            EnsureSize(2);
            StringBuilder sb = new StringBuilder();
            char c;
            while ((c = (char) ReadUShortReversed()) != '\0') sb.Append(c);

            return sb.ToString();
        }

        public ushort ReadUShortReversed()
        {
            return (ushort) (ReadByte() | (ReadByte() << 8));
        }
    }
}