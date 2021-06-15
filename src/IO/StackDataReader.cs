using System;
using System.Buffers.Binary;
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
        private const MethodImplOptions IMPL_OPTION = MethodImplOptions.AggressiveInlining
#if !NETFRAMEWORK && !NETSTANDARD2_0
                                                      | MethodImplOptions.AggressiveOptimization
#endif
                                                      ;

        public StackDataReader(byte* data, long len)
        {
            this = default;

            Data = data;
            Length = len;
            Position = 0;
        }

        public StackDataReader(
#if NETFRAMEWORK || NETSTANDARD2_0
            byte[] data,
#else
            Span<byte> data,
#endif
            long len)
        {
            this = default;

            Data = (byte*)UnsafeMemoryManager.AsPointer(ref data[0]);
            Length = len;
            Position = 0;
        }

        public byte* Data;
        public long Position;
        public long Length;


        public IntPtr StartAddress => (IntPtr)Data;

        public IntPtr PositionAddress
        {
            [MethodImpl(IMPL_OPTION)]
            get => (IntPtr)(Data + Position);
        }


        [MethodImpl(IMPL_OPTION)]
        public void Release()
        {
            // do nothing right now.
        }

        [MethodImpl(IMPL_OPTION)]
        public void Seek(long p)
        {
            Position = p;
        }

        [MethodImpl(IMPL_OPTION)]
        public void Skip(int count)
        {
            Position += count;
        }

        [MethodImpl(IMPL_OPTION)]
        public byte ReadUInt8()
        {
            byte v = *(Data + Position);

            Skip(1);

            return v;
        }

        [MethodImpl(IMPL_OPTION)]
        public sbyte ReadInt8()
        {
            sbyte v = *(sbyte*)(Data + Position);

            Skip(1);

            return v;
        }

        [MethodImpl(IMPL_OPTION)]
        public ushort ReadUInt16LE()
        {
            ushort v = (ushort)(Data[Position] |
                                 (Data[Position + 1] << 8));

            Skip(2);

            return v;
        }

        [MethodImpl(IMPL_OPTION)]
        public short ReadInt16LE()
        {
            short v = (short)(Data[Position] |
                              (Data[Position + 1] << 8));

            Skip(2);

            return v;
        }

        [MethodImpl(IMPL_OPTION)]
        public uint ReadUInt32LE()
        {
            uint v = (uint)(Data[Position] |
                            (Data[Position + 1] << 8) |
                            (Data[Position + 2] << 16) |
                            (Data[Position + 3] << 24));

            Skip(4);

            return v;
        }

        [MethodImpl(IMPL_OPTION)]
        public int ReadInt32LE()
        {
            int v = (int)(Data[Position] |
                            (Data[Position + 1] << 8) |
                            (Data[Position + 2] << 16) |
                            (Data[Position + 3] << 24));

            Skip(4);

            return v;
        }

        [MethodImpl(IMPL_OPTION)]
        public ulong ReadUInt64LE()
        {
            ulong v = (ulong)((ulong)Data[Position] |
                              ((ulong)Data[Position + 1] << 8) |
                              ((ulong)Data[Position + 2] << 16) |
                              ((ulong)Data[Position + 3] << 24) |
                              ((ulong)Data[Position + 4] << 32) |
                              ((ulong)Data[Position + 5] << 40) |
                              ((ulong)Data[Position + 6] << 48) |
                              ((ulong)Data[Position + 7] << 56));

            Skip(8);

            return v;
        }

        [MethodImpl(IMPL_OPTION)]
        public long ReadInt64LE()
        {
            long v = (long)((ulong)Data[Position] |
                              ((ulong)Data[Position + 1] << 8) |
                              ((ulong)Data[Position + 2] << 16) |
                              ((ulong)Data[Position + 3] << 24) |
                              ((ulong)Data[Position + 4] << 32) |
                              ((ulong)Data[Position + 5] << 40) |
                              ((ulong)Data[Position + 6] << 48) |
                              ((ulong)Data[Position + 7] << 56));

            Skip(8);

            return v;
        }





        [MethodImpl(IMPL_OPTION)]
        public ushort ReadUInt16BE()
        {
            ushort v = (ushort)((Data[Position] << 8) |
                                 (Data[Position + 1]));

            Skip(2);

            return v;
        }

        [MethodImpl(IMPL_OPTION)]
        public short ReadInt16BE()
        {
            short v = (short)((Data[Position] << 8) |
                              (Data[Position + 1]));

            Skip(2);

            return v;
        }

        [MethodImpl(IMPL_OPTION)]
        public uint ReadUInt32BE()
        {
            uint v = (uint)((Data[Position] << 24) |
                            (Data[Position + 1] << 16) |
                            (Data[Position + 2] << 8) |
                            (Data[Position + 3]));

            Skip(4);

            return v;
        }

        [MethodImpl(IMPL_OPTION)]
        public int ReadInt32BE()
        {
            int v = (int)((Data[Position] << 24) |
                          (Data[Position + 1] << 16) |
                          (Data[Position + 2] << 8) |
                          (Data[Position + 3]));

            Skip(4);

            return v;
        }

        [MethodImpl(IMPL_OPTION)]
        public ulong ReadUInt64BE()
        {
            ulong v = (ulong)(((ulong)Data[Position] << 56) |
                              ((ulong)Data[Position + 1] << 48) |
                              ((ulong)Data[Position + 2] << 40) |
                              ((ulong)Data[Position + 3] << 32) |
                              ((ulong)Data[Position + 4] << 24) |
                              ((ulong)Data[Position + 5] << 16) |
                              ((ulong)Data[Position + 6] << 8) |
                              ((ulong)Data[Position + 7]));

            Skip(8);

            return v;
        }

        [MethodImpl(IMPL_OPTION)]
        public long ReadInt64BE()
        {
            long v = (long)(((ulong)Data[Position] << 56) |
                              ((ulong)Data[Position + 1] << 48) |
                              ((ulong)Data[Position + 2] << 40) |
                              ((ulong)Data[Position + 3] << 32) |
                              ((ulong)Data[Position + 4] << 24) |
                              ((ulong)Data[Position + 5] << 16) |
                              ((ulong)Data[Position + 6] << 8) |
                              ((ulong)Data[Position + 7]));

            Skip(8);

            return v;
        }



        [MethodImpl(IMPL_OPTION)]
        public T* CastTo<T>() where T : unmanaged
        {
            return (T*)PositionAddress;
        }

        public string ReadASCII(int size)
        {
            ValueStringBuilder sb = new ValueStringBuilder(size);
           
            for (int i = 0; i < size; ++i)
            {
                char c = (char)ReadUInt8();

                if (c != 0)
                {
                    sb.Append(c);
                }
            }

            string ss = sb.ToString();
            sb.Dispose();
            return ss;
        }
    }
}
