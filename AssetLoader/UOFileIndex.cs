using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.AssetsLoader
{
    public struct UOFileIndex3D
    {
        public UOFileIndex3D(in long offset, in int length, in int extra = 0, in int decLength = 0, in int animID = 0)
        {
            Offset = offset; Length = length; Extra = extra;
            DecompressedLength = decLength; AnimID = animID;
        }

        public long Offset;
        public int Length;
        public int Extra;

        public int DecompressedLength;
        public int AnimID;

        public bool IsUOP => DecompressedLength > 0;
    }

    public struct UOFileIndex5D
    {
        public UOFileIndex5D(in int file, in int index, in int offset, in int length, in int extra = 0)
        {
            File = file; Index = index; Offset = offset; Length = length; Extra = extra;
        }

        public int File;
        public int Index;
        public int Offset;
        public int Length;
        public int Extra;
    }
}
