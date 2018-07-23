using System.IO;
using System.Runtime.InteropServices;

namespace ClassicUO.AssetsLoader
{
    public static class Multi
    {
        private static UOFileMul _file;
        private static int _itemOffset;

        public static void Load()
        {
            var path = Path.Combine(FileManager.UoFolderPath, "Multi.mul");
            var pathidx = Path.Combine(FileManager.UoFolderPath, "Multi.idx");

            if (File.Exists(path) && File.Exists(pathidx))
                _file = new UOFileMul(path, pathidx, 0x2000, 14);
            else
                throw new FileNotFoundException();

            _itemOffset = FileManager.ClientVersion >= ClientVersions.CV_7090
                ? Marshal.SizeOf<MultiBlockNew>()
                : Marshal.SizeOf<MultiBlock>();
        }

        public static unsafe MultiBlock GetMulti(int index)
        {
            return *((MultiBlock*) (_file.PositionAddress + index * _itemOffset));
        }


        public static int GetCount(int graphic)
        {
            graphic &= FileManager.GraphicMask;
            var (length, extra, patcher) = _file.SeekByEntryIndex(graphic);
            var count = length / _itemOffset;
            return count;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MultiBlock
    {
        public ushort ID;
        public short X;
        public short Y;
        public short Z;
        public uint Flags;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MultiBlockNew
    {
        public ushort ID;
        public short X;
        public short Y;
        public short Z;
        public uint Flags;
        public int Unknown;
    }
}