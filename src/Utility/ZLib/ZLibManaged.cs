#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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
using System.IO;
using System.IO.Compression;

using ZLibNative;

namespace ClassicUO.Utility
{
    public static class ZLibManaged
    {
        public static void Decompress(byte[] source, int sourceStart, int sourceLength, int offset, byte[] dest, int length)
        {
            using (MemoryStream stream = new MemoryStream(source, sourceStart, sourceLength - offset, true))
            {
                using (ZLIBStream ds = new ZLIBStream(stream, CompressionMode.Decompress))
                {
                    for (int i = 0, b = ds.ReadByte(); i < length && b >= 0; i++, b = ds.ReadByte())
                    {
                        dest[i] = (byte)b;
                    }
                }
            }
        }

        public static unsafe void Decompress(IntPtr source, int sourceLength, int offset, IntPtr dest, int length)
        {
            using (UnmanagedMemoryStream stream = new UnmanagedMemoryStream((byte*)source.ToPointer(), sourceLength - offset))
            {
                using (ZLIBStream ds = new ZLIBStream(stream, CompressionMode.Decompress))
                {
                    byte* dstPtr = (byte*)dest.ToPointer();
                    for (int i = 0, b = ds.ReadByte(); i < length && b >= 0; i++, b = ds.ReadByte())
                    {
                        dstPtr[i] = (byte)b;
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
                destLength = (int)stream.Position;
            }
        }
    }
}