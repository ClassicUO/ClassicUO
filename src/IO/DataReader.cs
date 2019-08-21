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
using System.Runtime.InteropServices;

namespace ClassicUO.IO
{
    /// <summary>
    ///     A fast Little Endian data reader.
    /// </summary>
    internal unsafe class DataReader
    {
        private byte* _data;

        private GCHandle _handle;

        internal long Position { get; set; }

        internal long Length { get; private set; }

        internal IntPtr StartAddress => (IntPtr) _data;

        internal IntPtr PositionAddress => (IntPtr) (_data + Position);


        [MethodImpl(256)]
        public void ReleaseData()
        {
            if (_handle.IsAllocated)
                _handle.Free();
        }

        [MethodImpl(256)]
        internal void SetData(byte* data, long length)
        {
            ReleaseData();

            _data = data;
            Length = length;
            Position = 0;
        }

        [MethodImpl(256)]
        internal void SetData(byte[] data, long length)
        {
            //fixed (byte* d = data)
            //    SetData(d, length);
            ReleaseData();
            _handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            _data = (byte*) _handle.AddrOfPinnedObject();
            Length = length;
            Position = 0;
        }

        [MethodImpl(256)]
        internal void SetData(IntPtr data, long length)
        {
            SetData((byte*) data, length);
        }

        [MethodImpl(256)]
        internal void SetData(IntPtr data)
        {
            SetData((byte*) data, Length);
        }

        [MethodImpl(256)]
        internal void Seek(long idx)
        {
            Position = idx;
            EnsureSize(0);
        }

        [MethodImpl(256)]
        internal void Seek(int idx)
        {
            Position = idx;
            EnsureSize(0);
        }

        [MethodImpl(256)]
        internal void Skip(int count)
        {
            EnsureSize(count);
            Position += count;
        }

        [MethodImpl(256)]
        internal byte ReadByte()
        {
            EnsureSize(1);

            return _data[Position++];
        }

        [MethodImpl(256)]
        internal sbyte ReadSByte()
        {
            return (sbyte) ReadByte();
        }

        [MethodImpl(256)]
        internal bool ReadBool()
        {
            return ReadByte() != 0;
        }

        [MethodImpl(256)]
        internal short ReadShort()
        {
            EnsureSize(2);

            short v = *(short*) (_data + Position);
            Position += 2;

            return v;
        }

        [MethodImpl(256)]
        internal ushort ReadUShort()
        {
            EnsureSize(2);

            ushort v = *(ushort*) (_data + Position);
            Position += 2;

            return v;
        }

        [MethodImpl(256)]
        internal int ReadInt()
        {
            EnsureSize(4);

            int v = *(int*) (_data + Position);

            Position += 4;

            return v;
        }

        [MethodImpl(256)]
        internal uint ReadUInt()
        {
            EnsureSize(4);

            uint v = *(uint*) (_data + Position);

            Position += 4;

            return v;
        }

        [MethodImpl(256)]
        internal long ReadLong()
        {
            EnsureSize(8);

            long v = *(long*) (_data + Position);

            Position += 8;

            return v;
        }

        [MethodImpl(256)]
        internal ulong ReadULong()
        {
            EnsureSize(8);

            ulong v = *(ulong*) (_data + Position);

            Position += 8;

            return v;
        }

        [MethodImpl(256)]
        internal byte[] ReadArray(int count)
        {
            EnsureSize(count);

            byte[] data = new byte[count];

            fixed (byte* ptr = data)
                Buffer.MemoryCopy(&_data[Position], ptr, count, count);

            Position += count;

            return data;
        }

        [MethodImpl(256)]
        private void EnsureSize(int size)
        {
            if (Position + size > Length)
                throw new IndexOutOfRangeException();
        }


        [MethodImpl(256)]
        public ushort ReadUShortReversed()
        {
            EnsureSize(2);

            return (ushort)((ReadByte() << 8) | ReadByte());
        }

        [MethodImpl(256)]
        public uint ReadUIntReversed()
        {
            EnsureSize(4);

            return (uint)((ReadByte() << 24) | (ReadByte() << 16) | (ReadByte() << 8) | ReadByte());
        }
    }
}