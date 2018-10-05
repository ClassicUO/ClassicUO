#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace ClassicUO.Utility
{
    public static class Zlib
    {
        private static readonly byte[] _zlibHeader = {0x78, 0x9C};

        public static bool Decompress(byte[] source, int offset, byte[] dest, int length)
        {
            using (MemoryStream ms = new MemoryStream(source, offset, source.Length - offset))
            {
                ms.Seek(2, SeekOrigin.Begin);
                using (DeflateStream stream = new DeflateStream(ms, CompressionMode.Decompress))
                    stream.Read(dest, 0, length);
            }

            return true;
            //return Uncompress(dest, ref length, source, source.Length) == ZLibError.Z_OK;
        }


        [DllImport("zlib1", EntryPoint = "uncompress")]
        private static extern ZLibError Uncompress(byte[] dest, ref int destLen, byte[] source, int sourceLen);

        internal enum ZLibError
        {
            Z_OK = 0,
            Z_STREAM_END = 1,
            Z_NEED_DICT = 2,
            Z_ERRNO = -1,
            Z_STREAM_ERROR = -2,
            Z_DATA_ERROR = -3, // Data was corrupt
            Z_MEM_ERROR = -4, //  Not Enough Memory
            Z_BUF_ERROR = -5, // Not enough buffer space
            Z_VERSION_ERROR = -6
        }
    }
}