#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

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

        public bool IsEOF => Position >= Length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleaseData()
        {
            if (_handle.IsAllocated)
            {
                _handle.Free();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetData(byte* data, long length)
        {
            ReleaseData();

            _data = data;
            Length = length;
            Position = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetData(IntPtr data, long length)
        {
            SetData((byte*) data, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetData(IntPtr data)
        {
            SetData((byte*) data, Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Seek(long idx)
        {
            Position = idx;
            EnsureSize(0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Seek(int idx)
        {
            Position = idx;
            EnsureSize(0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Skip(int count)
        {
            EnsureSize(count);
            Position += count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal byte ReadByte()
        {
            EnsureSize(1);

            return _data[Position++];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal sbyte ReadSByte()
        {
            return (sbyte) ReadByte();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool ReadBool()
        {
            return ReadByte() != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal short ReadShort()
        {
            EnsureSize(2);

            short v = *(short*) (_data + Position);
            Position += 2;

            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ushort ReadUShort()
        {
            EnsureSize(2);

            ushort v = *(ushort*) (_data + Position);
            Position += 2;

            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int ReadInt()
        {
            EnsureSize(4);

            int v = *(int*) (_data + Position);

            Position += 4;

            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal uint ReadUInt()
        {
            EnsureSize(4);

            uint v = *(uint*) (_data + Position);
            Position += 4;

            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal long ReadLong()
        {
            EnsureSize(8);

            long v = *(long*) (_data + Position);
            Position += 8;

            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ulong ReadULong()
        {
            EnsureSize(8);

            ulong v = *(ulong*) (_data + Position);
            Position += 8;

            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        internal string ReadASCII(int size)
        {
            EnsureSize(size);

            Span<char> span = stackalloc char[size];
            ValueStringBuilder sb = new ValueStringBuilder(span);

            for (int i = 0; i < size; i++)
            {
                char c = (char)ReadByte();

                if (c != 0)
                {
                    sb.Append(c);
                }
            }

            string ss = sb.ToString();

            sb.Dispose();

            return ss;
        }

        [Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureSize(int size)
        {
            if (Position + size > Length)
            {
#if DEBUG
                throw new IndexOutOfRangeException();
#else
                Log.Error($"size out of range. {Position + size} > {Length}");
#endif
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUShortReversed()
        {
            EnsureSize(2);

            return (ushort) ((ReadByte() << 8) | ReadByte());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUIntReversed()
        {
            EnsureSize(4);

            return (uint) ((ReadByte() << 24) | (ReadByte() << 16) | (ReadByte() << 8) | ReadByte());
        }
    }
}