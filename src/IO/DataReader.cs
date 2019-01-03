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
using System.Diagnostics;
using System.IO;
using System.Security;

namespace ClassicUO.IO
{
    /// <summary>
    /// A fast Little Endian data reader.
    /// </summary>
    [SecurityCritical]
    internal unsafe class DataReader
    {
        [SecurityCritical]
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
            fixed (byte* ptr = data)
                SetData(ptr, length);
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
            EnsureSize(0);
        }

        internal void Seek(int idx)
        {
            Position = idx;
            EnsureSize(0);
        }

        internal void Skip(int count)
        {
            EnsureSize(count);
            Position += count;
        }

        internal byte ReadByte()
        {
            EnsureSize(1);

            return _data[Position++];
        }

        internal sbyte ReadSByte()
        {
            return (sbyte) ReadByte();
        }

        internal bool ReadBool()
        {
            return ReadByte() != 0;
        }

        internal short ReadShort()
        {
            EnsureSize(2);

            short v = *(short*)(_data + Position);
            Position += 2;

            return v;
        }

        internal ushort ReadUShort()
        {
            EnsureSize(2);

            ushort v = *(ushort*)(_data + Position);
            Position += 2;

            return v;
        }

        internal int ReadInt()
        {
            EnsureSize(4);

            int v = *(int*) (_data + Position);

            Position += 4;

            return v;
        }

        internal uint ReadUInt()
        {
            EnsureSize(4);

            uint v = *(uint*)(_data + Position);

            Position += 4;

            return v;
        }

        internal long ReadLong()
        {
            EnsureSize(8);

            long v = *(long*) (_data + Position);

            Position += 8;

            return v;
        }

        internal ulong ReadULong()
        {
            EnsureSize(8);

            ulong v = *(ulong*)(_data + Position);

            Position += 8;

            return v;
        }

        internal byte[] ReadArray(int count)
        {
            EnsureSize(count);

            byte[] data = new byte[count];

            fixed (byte* ptr = data)
            {
                Buffer.MemoryCopy(&_data[Position], ptr, count, count);
            }

            Position += count;

            return data;
        }

        private void EnsureSize(int size)
        {
            if (Position + size > Length)
                throw new IndexOutOfRangeException();
        }
    }
}