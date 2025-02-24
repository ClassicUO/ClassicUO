// SPDX-License-Identifier: BSD-2-Clause

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