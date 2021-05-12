using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Utility;

namespace ClassicUO.IO
{
    unsafe ref struct StackDataReader
    {
        private GCHandle _handle;

        public StackDataReader(byte* data, long len)
        {
            this = default;

            Data = data;
            Length = len;
            Position = 0;
        }

        public StackDataReader(byte[] data, long len)
        {
            this = default;

            _handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            Data = (byte*) _handle.AddrOfPinnedObject();
            Length = len;
            Position = 0;
        }

        public byte* Data;
        public long Position;
        public long Length;


        public IntPtr StartAddress => (IntPtr) Data;

        public IntPtr PositionAddress
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (IntPtr)(Data + Position);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release()
        {
            if (_handle.IsAllocated)
            {
                _handle.Free();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Seek(long p)
        {
            Position = p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Skip(int count)
        {
            Position += count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Read<T>() where T : unmanaged
        {
            int size = sizeof(T);

            T v = *(T*) PositionAddress;
            
            Position += size;

            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T ReadLE<T>() where T : unmanaged
        {
            int size = sizeof(T);

            byte* data = stackalloc byte[size];
            byte* buffer = (byte*) PositionAddress;

            for (int lo = 0, hi = size - 1; hi > lo; ++lo, --hi)
            {
                data[lo] = buffer[hi];
                data[hi] = buffer[lo];
            }

            Position += size;

            return *(T*) data;
        }

        public string ReadASCII(int size)
        {
            StringBuilder sb = new StringBuilder(size);

            for (int i = 0; i < size; ++i)
            {
                char c = Read<char>();

                if (c != 0)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }
}
