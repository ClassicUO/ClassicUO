#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
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

namespace ClassicUO.IO
{
    public unsafe class DataReader
    {
        private byte* _data;

        internal long Position { get; set; }
        internal long Length { get; private set; }

        internal IntPtr StartAddress => (IntPtr) _data;
        internal IntPtr PositionAddress => (IntPtr) (_data + Position);

        internal void SetData(byte* data, long length)
        {
            _data = data;
            Length = length;
            Position = 0;
        }

        internal void SetData(byte[] data, long length)
        {
            fixed (byte* ptr = data) SetData(ptr, length);
        }

        internal void SetData(IntPtr data, long length)
        {
            SetData((byte*) data, length);
        }

        internal void SetData(IntPtr data)
        {
            SetData((byte*) data, Length);
        }


        internal void Seek(long idx)
        {
            Position = idx;
        }

        internal void Seek(int idx)
        {
            Position = idx;
        }

        internal void Skip(int count)
        {
            Position += count;
        }

        internal byte ReadByte() => _data[Position++];

        internal sbyte ReadSByte() => (sbyte) ReadByte();

        internal bool ReadBool() => ReadByte() != 0;

        internal short ReadShort() => (short) (ReadByte() | (ReadByte() << 8));

        internal ushort ReadUShort() => (ushort) ReadShort();

        internal int ReadInt() => ReadByte() | (ReadByte() << 8) | (ReadByte() << 16) | (ReadByte() << 24);

        internal uint ReadUInt() => (uint) ReadInt();

        internal long ReadLong() => ReadByte() | ((long) ReadByte() << 8) | ((long) ReadByte() << 16) |
                                    ((long) ReadByte() << 24) | ((long) ReadByte() << 32) | ((long) ReadByte() << 40) |
                                    ((long) ReadByte() << 48) | ((long) ReadByte() << 56);

        internal ulong ReadULong() => (ulong) ReadLong();
    }
}