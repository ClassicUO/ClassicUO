#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
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

using System;
using System.Runtime.InteropServices;

namespace ClassicUO.Utility
{
    internal static class ZLib
    {
        // thanks ServUO :)

        private static readonly ICompressor _compressor;

        static ZLib()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                _compressor = new Compressor64();
            else if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
                _compressor = new CompressorUnix64();
            else
                throw new NotSupportedException("Zlib not support this platform");
        }


        public static void Decompress(byte[] source, int offset, byte[] dest, int length)
        {
            _compressor.Decompress(dest, ref length, source, source.Length - offset);
        }

        public static void Decompress(IntPtr source, int sourceLength, int offset, IntPtr dest, int length)
        {
            _compressor.Decompress(dest, ref length, source, sourceLength - offset);
        }

        private enum ZLibQuality
        {
            Default = -1,

            None = 0,

            Speed = 1,
            Size = 9
        }

        private enum ZLibError
        {
            VersionError = -6,
            BufferError = -5,
            MemoryError = -4,
            DataError = -3,
            StreamError = -2,
            FileError = -1,

            Okay = 0,

            StreamEnd = 1,
            NeedDictionary = 2
        }


        private interface ICompressor
        {
            string Version { get; }

            ZLibError Compress(byte[] dest, ref int destLength, byte[] source, int sourceLength);
            ZLibError Compress(byte[] dest, ref int destLength, byte[] source, int sourceLength, ZLibQuality quality);

            ZLibError Decompress(byte[] dest, ref int destLength, byte[] source, int sourceLength);
            ZLibError Decompress(IntPtr dest, ref int destLength, IntPtr source, int sourceLength);

        }



        private sealed class Compressor64 : ICompressor
        {
            public string Version => SafeNativeMethods.zlibVersion();

            public ZLibError Compress(byte[] dest, ref int destLength, byte[] source, int sourceLength)
            {
                return SafeNativeMethods.compress(dest, ref destLength, source, sourceLength);
            }

            public ZLibError Compress(byte[] dest, ref int destLength, byte[] source, int sourceLength, ZLibQuality quality)
            {
                return SafeNativeMethods.compress2(dest, ref destLength, source, sourceLength, quality);
            }

            public ZLibError Decompress(byte[] dest, ref int destLength, byte[] source, int sourceLength)
            {
                return SafeNativeMethods.uncompress(dest, ref destLength, source, sourceLength);
            }

            public ZLibError Decompress(IntPtr dest, ref int destLength, IntPtr source, int sourceLength)
            {
                return SafeNativeMethods.uncompress(dest, ref destLength, source, sourceLength);
            }

            class SafeNativeMethods
            {
                [DllImport("zlib")]
                internal static extern string zlibVersion();

                [DllImport("zlib")]
                internal static extern ZLibError compress(byte[] dest, ref int destLength, byte[] source, int sourceLength);

                [DllImport("zlib")]
                internal static extern ZLibError compress2(
                    byte[] dest, ref int destLength, byte[] source, int sourceLength, ZLibQuality quality);

                [DllImport("zlib")]
                internal static extern ZLibError uncompress(byte[] dest, ref int destLen, byte[] source, int sourceLen);

                [DllImport("zlib")]
                internal static extern ZLibError uncompress(IntPtr dest, ref int destLen, IntPtr source, int sourceLen);
            }
        }

        private sealed class CompressorUnix64 : ICompressor
        {
            public string Version => SafeNativeMethods.zlibVersion();

            public ZLibError Compress(byte[] dest, ref int destLength, byte[] source, int sourceLength)
            {
                long destLengthLong = destLength;
                ZLibError z = SafeNativeMethods.compress(dest, ref destLengthLong, source, sourceLength);
                destLength = (int) destLengthLong;

                return z;
            }

            public ZLibError Compress(byte[] dest, ref int destLength, byte[] source, int sourceLength, ZLibQuality quality)
            {
                long destLengthLong = destLength;
                ZLibError z = SafeNativeMethods.compress2(dest, ref destLengthLong, source, sourceLength, quality);
                destLength = (int) destLengthLong;

                return z;
            }

            public ZLibError Decompress(byte[] dest, ref int destLength, byte[] source, int sourceLength)
            {
                long destLengthLong = destLength;
                ZLibError z = SafeNativeMethods.uncompress(dest, ref destLengthLong, source, sourceLength);
                destLength = (int) destLengthLong;

                return z;
            }
            public ZLibError Decompress(IntPtr dest, ref int destLength, IntPtr source, int sourceLength)
            {
                return SafeNativeMethods.uncompress(dest, ref destLength, source, sourceLength);
            }

            class SafeNativeMethods
            {
                [DllImport("libz")]
                internal static extern string zlibVersion();

                [DllImport("libz")]
                internal static extern ZLibError compress(byte[] dest, ref long destLength, byte[] source, long sourceLength);

                [DllImport("libz")]
                internal static extern ZLibError compress2(byte[] dest, ref long destLength, byte[] source, long sourceLength, ZLibQuality quality);

                [DllImport("libz")]
                internal static extern ZLibError uncompress(byte[] dest, ref long destLen, byte[] source, long sourceLen);

                [DllImport("libz")]
                internal static extern ZLibError uncompress(IntPtr dest, ref int destLen, IntPtr source, int sourceLen);
            }
        }
    }
}