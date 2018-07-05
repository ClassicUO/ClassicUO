using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ClassicUO.Utility
{
    public static class Zlib
    {
        internal enum ZLibError : int
        {
            Z_OK = 0,
            Z_STREAM_END = 1,
            Z_NEED_DICT = 2,
            Z_ERRNO = (-1),
            Z_STREAM_ERROR = (-2),
            Z_DATA_ERROR = (-3), // Data was corrupt
            Z_MEM_ERROR = (-4), //  Not Enough Memory
            Z_BUF_ERROR = (-5), // Not enough buffer space
            Z_VERSION_ERROR = (-6),
        }

        private static readonly byte[] _zlibHeader = { 0x78, 0x9C };

        public static bool Decompress(byte[] source, int offset, byte[] dest, int length)
        {
            /*using (MemoryStream ms = new MemoryStream(source, offset, source.Length - offset))
            {
                ms.Seek(2, SeekOrigin.Begin);
                using (DeflateStream stream = new DeflateStream(ms, CompressionMode.Decompress))
                {
                    stream.Read(dest, 0, length);
                }
            }*/

            return Uncompress(dest, ref length, source, source.Length) == ZLibError.Z_OK;
        }


        [DllImport("zlib")]
        private static extern ZLibError Uncompress(byte[] dest, ref int destLen, byte[] source, int sourceLen);

    }
}
