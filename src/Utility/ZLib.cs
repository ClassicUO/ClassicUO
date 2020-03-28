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

namespace ClassicUO.Utility
{
    internal static class ZLib
    {
        private static readonly byte[] m_ZLibCompatibleHeader = { 0x78, 0x01 };
        public static unsafe void Decompress(IntPtr source, int sourceLength, int offset, IntPtr dest, int length)
        {
            using (UnmanagedMemoryStream stream = new UnmanagedMemoryStream((byte*)source.ToPointer(), sourceLength))
            {
                stream.Seek(m_ZLibCompatibleHeader.Length, SeekOrigin.Begin);
                using (DeflateStream ds = new DeflateStream(stream, CompressionMode.Decompress, false))
                {
                    byte* dstPtr = (byte*)dest.ToPointer();
                    for (int i = 0; i < length; i++)
                    {
                        dstPtr[i] = (byte)ds.ReadByte();
                    }
                }
            }
        }

        public static void Compress(byte[] dest, ref int destLength, byte[] source)
        {
            using(MemoryStream stream = new MemoryStream(source, true))
            {
                stream.Write(m_ZLibCompatibleHeader, 0, m_ZLibCompatibleHeader.Length);
                using (DeflateStream ds = new DeflateStream(stream, CompressionMode.Compress, false))
                {
                    int b;
                    destLength = 0;
                    while((b = ds.ReadByte()) >= 0 && destLength < dest.Length)
                    {
                        dest[destLength] = (byte)b;
                        destLength++;
                    }
                }
            }
        }
    }
}