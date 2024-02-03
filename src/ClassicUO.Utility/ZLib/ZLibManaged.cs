#region license

// Copyright (c) 2024, andreakarasho
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
using System.IO;
using System.IO.Compression;
using ZLibNative;

namespace ClassicUO.Utility
{
    public static class ZLibManaged
    {
        public static void Decompress
        (
            byte[] source,
            int sourceStart,
            int sourceLength,
            int offset,
            byte[] dest,
            int length
        )
        {
            using (MemoryStream stream = new MemoryStream(source, sourceStart, sourceLength - offset, true))
            {
                using (ZLIBStream ds = new ZLIBStream(stream, CompressionMode.Decompress))
                {
                    for (int i = 0, b = ds.ReadByte(); i < length && b >= 0; i++, b = ds.ReadByte())
                    {
                        dest[i] = (byte) b;
                    }
                }
            }
        }

        public static unsafe void Decompress(IntPtr source, int sourceLength, int offset, IntPtr dest, int length)
        {
            using (UnmanagedMemoryStream stream = new UnmanagedMemoryStream((byte*) source.ToPointer(), sourceLength - offset))
            {
                using (ZLIBStream ds = new ZLIBStream(stream, CompressionMode.Decompress))
                {
                    byte* dstPtr = (byte*) dest.ToPointer();

                    for (int i = 0, b = ds.ReadByte(); i < length && b >= 0; i++, b = ds.ReadByte())
                    {
                        dstPtr[i] = (byte) b;
                    }
                }
            }
        }

        public static void Compress(byte[] dest, ref int destLength, byte[] source)
        {
            using (MemoryStream stream = new MemoryStream(dest, true))
            {
                using (ZLIBStream ds = new ZLIBStream(stream, CompressionMode.Compress, true))
                {
                    ds.Write(source, 0, source.Length);
                    ds.Flush();
                }

                destLength = (int) stream.Position;
            }
        }
    }
}