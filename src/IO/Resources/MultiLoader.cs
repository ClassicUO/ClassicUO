using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Utility;

namespace ClassicUO.IO.Resources
{
    class MultiLoader : ResourceLoader
    {
        private UOFileMul _file;
        private int _itemOffset;

        public override void Load()
        {
            string path = Path.Combine(FileManager.UoFolderPath, "multi.mul");
            string pathidx = Path.Combine(FileManager.UoFolderPath, "multi.idx");

            if (File.Exists(path) && File.Exists(pathidx))
                _file = new UOFileMul(path, pathidx, 0x2000, 14);
            else
                throw new FileNotFoundException();
            _itemOffset = FileManager.ClientVersion >= ClientVersions.CV_7090 ? UnsafeMemoryManager.SizeOf<MultiBlockNew>() : UnsafeMemoryManager.SizeOf<MultiBlock>();
        }

        protected override void CleanResources()
        {
            throw new NotImplementedException();
        }

        public unsafe MultiBlock GetMulti(int index)
        {
            return *(MultiBlock*)(_file.PositionAddress + index * _itemOffset);
        }

        public int GetCount(int graphic)
        {
            (int length, int extra, bool patcher) = _file.SeekByEntryIndex(graphic);
            int count = length / _itemOffset;

            return count;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct MultiBlock
    {
        public readonly ushort ID;
        public readonly short X;
        public readonly short Y;
        public readonly short Z;
        public readonly uint Flags;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct MultiBlockNew
    {
        public readonly ushort ID;
        public readonly short X;
        public readonly short Y;
        public readonly short Z;
        public readonly uint Flags;
        public readonly int Unknown;
    }
}
