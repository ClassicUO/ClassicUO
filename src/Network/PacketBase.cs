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

namespace ClassicUO.Network
{
    internal abstract class PacketBase
    {
        protected abstract byte this[int index] { get; set; }

        public abstract int Length { get; }

        public byte ID => this[0];

        public bool IsDynamic { get; protected set; }

        public int Position { get; protected set; }

        protected abstract bool EnsureSize(int length);
        public abstract ref byte[] ToArray();

        public void Skip(int length)
        {
            EnsureSize(length);
            Position += length;
        }

        public void Seek(int index)
        {
            Position = index;
            EnsureSize(0);
        }

        public void WriteByte(byte v)
        {
            EnsureSize(1);
            this[Position++] = v;
        }

        public void WriteBytes(byte[] buffer, int offset, int length)
        {
            EnsureSize(length);

            for (int i = offset; i < length; i++)
                this[Position++] = buffer[i];
        }

        public void WriteSByte(sbyte v)
        {
            WriteByte((byte) v);
        }

        public void WriteBool(bool v)
        {
            WriteByte(v ? (byte) 0x01 : (byte) 0x00);
        }

        public void WriteUShort(ushort v)
        {
            EnsureSize(2);
            WriteByte((byte) (v >> 8));
            WriteByte((byte) v);
        }

        public void WriteUInt(uint v)
        {
            EnsureSize(4);
            WriteByte((byte) (v >> 24));
            WriteByte((byte) (v >> 16));
            WriteByte((byte) (v >> 8));
            WriteByte((byte) v);
        }

        public unsafe void WriteASCII(string value)
        {
            EnsureSize(value.Length + 1);

            fixed (char* ptr = value)
            {
                char* buff = ptr;

                while (*buff != 0)
                    WriteByte((byte) *buff++);
            }

            WriteByte(0);
        }

        public unsafe void WriteASCII(string value, int length)
        {
            EnsureSize(length);

            if (value.Length > length) throw new ArgumentOutOfRangeException();

            fixed (char* ptr = value)
            {
                char* buff = ptr;
                byte* end = (byte*) ptr + length;

                while (*buff != 0 && &buff != &end)
                    WriteByte((byte) *buff++);
            }

            if (value.Length < length)
            {
                WriteByte(0);
                Position += length - value.Length - 1;
            }
        }

        public unsafe void WriteUnicode(string value)
        {
            EnsureSize((value.Length + 1) * 2);

            fixed (char* ptr = value)
            {
                short* buff = (short*) ptr;

                while (*buff != 0)
                    WriteUShort((ushort) *buff++);
            }

            WriteUShort(0);
        }

        public unsafe void WriteUnicode(string value, int length)
        {
            EnsureSize(length);

            //the string is automatically resized based on length provided
            /*if (value.Length > length)
                throw new ArgumentOutOfRangeException();*/

            fixed (char* ptr = value)
            {
                short* buff = (short*) ptr;
                int pos = 0;

                while (*buff != 0 && pos < length)
                {
                    WriteUShort((ushort) *buff++);
                    pos++;
                }
            }

            if (value.Length < length)
            {
                WriteUShort(0);
                Position += (length - value.Length - 1) * 2;
            }
        }
    }
}