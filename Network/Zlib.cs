using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;

namespace ClassicUO.Network
{
    public static class Zlib
    {
        private static readonly byte[] _zlibHeader = { 0x78, 0x9C };

        public static void Decompress(byte[] source, int offset, byte[] dest, int length)
        {
            using (MemoryStream ms = new MemoryStream(source, offset, source.Length - offset))
            {
                ms.Seek(2, SeekOrigin.Begin);
                using (DeflateStream stream = new DeflateStream(ms, CompressionMode.Decompress))
                {
                    stream.Read(dest, 0, length);
                }
            }
        }
    }
}
