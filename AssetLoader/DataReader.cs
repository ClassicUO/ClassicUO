using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.AssetsLoader
{
    public unsafe class DataReader
    {
        private byte* _data;

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
            SetData((byte*)data, length);
        }

        internal void SetData(IntPtr data)
        {
            SetData((byte*)data, Length);
        }

        internal long Position { get; set; }
        internal long Length { get; private set; }


        internal void Seek(long idx) => Position = idx;
        internal void Seek(int idx) => Position = idx;
        internal void Skip(int count) => Position += count;

        internal byte ReadByte() => _data[Position++];
        internal sbyte ReadSByte() => (sbyte)ReadByte();
        internal bool ReadBool() => ReadByte() != 0;
        internal short ReadShort() => (short)(ReadByte() | (ReadByte() << 8));
        internal ushort ReadUShort() => (ushort)ReadShort();
        internal int ReadInt() => (ReadByte() | (ReadByte() << 8) | (ReadByte() << 16) | (ReadByte() << 24));
        internal uint ReadUInt() => (uint)ReadInt();
        internal long ReadLong() => (ReadByte() | ((long)ReadByte() << 8) | ((long)ReadByte() << 16) | ((long)ReadByte() << 24) | ((long)ReadByte() << 32) | ((long)ReadByte() << 40) | ((long)ReadByte() << 48) | ((long)ReadByte() << 56));
        internal ulong ReadULong() => (ulong)ReadLong();

        internal IntPtr StartAddress => (IntPtr)_data;
        internal IntPtr PositionAddress => (IntPtr)(_data + Position);

    }
}
