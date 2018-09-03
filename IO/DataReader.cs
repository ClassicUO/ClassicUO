using System;

namespace ClassicUO.IO
{
    public unsafe class DataReader
    {
        private byte* _data;

        internal long Position { get; set; }
        internal long Length { get; private set; }

        internal IntPtr StartAddress => (IntPtr)_data;
        internal IntPtr PositionAddress => (IntPtr)(_data + Position);

        internal void SetData(byte* data, long length)
        {
            _data = data;
            Length = length;
            Position = 0;
        }

        internal void SetData(byte[] data, long length)
        {
            fixed (byte* ptr = data)
            {
                SetData(ptr, length);
            }
        }

        internal void SetData(IntPtr data, long length)
        {
            SetData((byte*)data, length);
        }

        internal void SetData(IntPtr data)
        {
            SetData((byte*)data, Length);
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

        internal byte ReadByte()
        {
            return _data[Position++];
        }

        internal sbyte ReadSByte()
        {
            return (sbyte)ReadByte();
        }

        internal bool ReadBool()
        {
            return ReadByte() != 0;
        }

        internal short ReadShort()
        {
            return (short)(ReadByte() | (ReadByte() << 8));
        }

        internal ushort ReadUShort()
        {
            return (ushort)ReadShort();
        }

        internal int ReadInt()
        {
            return ReadByte() | (ReadByte() << 8) | (ReadByte() << 16) | (ReadByte() << 24);
        }

        internal uint ReadUInt()
        {
            return (uint)ReadInt();
        }

        internal long ReadLong()
        {
            return ReadByte() | ((long)ReadByte() << 8) | ((long)ReadByte() << 16) | ((long)ReadByte() << 24) | ((long)ReadByte() << 32) | ((long)ReadByte() << 40) | ((long)ReadByte() << 48) | ((long)ReadByte() << 56);
        }

        internal ulong ReadULong()
        {
            return (ulong)ReadLong();
        }
    }
}