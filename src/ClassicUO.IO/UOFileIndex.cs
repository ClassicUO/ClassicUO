// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Runtime.InteropServices;

namespace ClassicUO.IO
{
    public struct UOFileIndex : IEquatable<UOFileIndex>
    {
        public UOFileIndex
        (
            UOFile file,
            long offset,
            int length,
            int decompressed,
            CompressionType compressionFlag = 0,
            int width = 0,
            int height = 0,
            ushort hue = 0
        )
        {
            File = file;
            Offset = offset;
            Length = length;
            DecompressedLength = decompressed;
            CompressionFlag = compressionFlag;
            Width = width;
            Height = height;
            Hue = hue;

            AnimOffset = 0;
        }

        public UOFile File;
        public long Offset;
        public int Length;
        public int DecompressedLength;
        public CompressionType CompressionFlag;
        public int Width;
        public int Height;
        public ushort Hue;
        public sbyte AnimOffset;


        public static UOFileIndex Invalid = new UOFileIndex
        (
            null,
            0,
            0,
            0,
            0,
            0
        );

        public bool Equals(UOFileIndex other)
        {
            return (File, Offset, Length, DecompressedLength) == (File, other.Offset, other.Length, other.DecompressedLength);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UOFileIndex5D
    {
        public UOFileIndex5D(uint file, uint index, uint offset, uint length, uint extra = 0)
        {
            FileID = file;
            BlockID = index;
            Position = offset;
            Length = length;
            GumpData = extra;
        }

        public uint FileID;
        public uint BlockID;
        public uint Position;
        public uint Length;
        public uint GumpData;
    }
}