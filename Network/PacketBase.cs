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

namespace ClassicUO.Network
{
    public abstract class PacketBase
    {
        protected abstract byte this[int index] { get; set; }

        public abstract int Length { get; }
        public byte ID => this[0];
        public bool IsDynamic { get; protected set; }
        public int Position { get; protected set; }
        protected abstract void EnsureSize(int length);
        public abstract byte[] ToArray();

        public void Skip(int lengh)
        {
            EnsureSize(lengh);
            Position += lengh;
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

        public void WriteASCII(string value)
        {
            EnsureSize(value.Length + 1);
            foreach (char c in value) WriteByte((byte) c);

            WriteByte(0);
        }

        public void WriteASCII(string value, int length)
        {
            EnsureSize(length);
            if (value.Length > length) throw new ArgumentOutOfRangeException();

            for (int i = 0; i < value.Length; i++) WriteByte((byte) value[i]);

            if (value.Length < length)
            {
                WriteByte(0);
                Position += length - value.Length - 1;
            }
        }

        public void WriteUnicode(string value)
        {
            EnsureSize((value.Length + 1) * 2);
            foreach (char c in value)
            {
                WriteByte((byte) (c >> 8));
                WriteByte((byte) c);
            }

            WriteUShort(0);
        }

        public void WriteUnicode(string value, int length)
        {
            EnsureSize(length);
            if (value.Length > length) throw new ArgumentOutOfRangeException();

            for (int i = 0; i < value.Length; i++)
            {
                WriteByte((byte) (value[i] >> 8));
                WriteByte((byte) value[i]);
            }

            if (value.Length < length)
            {
                WriteUShort(0);
                Position += (length - value.Length - 1) * 2;
            }
        }
    }
}