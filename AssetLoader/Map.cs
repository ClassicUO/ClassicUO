using System.IO;
using System.Runtime.InteropServices;

namespace ClassicUO.AssetsLoader
{
    public static class Map
    {
        private const int MAPS_COUNT = 6;

        private static readonly UOFile[] _filesMap = new UOFile[MAPS_COUNT];
        private static readonly UOFileMul[] _filesStatics = new UOFileMul[MAPS_COUNT];
        private static readonly UOFileMul[] _filesIdxStatics = new UOFileMul[MAPS_COUNT];

        public static IndexMap[][] BlockData { get; } = new IndexMap[MAPS_COUNT][];

        public static int[][] MapBlocksSize { get; } = new int[MAPS_COUNT][];

        public static int[][] MapsDefaultSize { get; } = new int[MAPS_COUNT][]
        {
            new int[2] {7168, 4096}, new int[2] {7168, 4096}, new int[2] {2304, 1600}, new int[2] {2560, 2048}, new int[2] {1448, 1448}, new int[2] {1280, 4096}
        };

        public static void Load()
        {
            string path = string.Empty;

            for (int i = 0; i < MAPS_COUNT; i++)
            {
                path = Path.Combine(FileManager.UoFolderPath, string.Format("map{0}LegacyMUL.uop", i));
                if (File.Exists(path))
                    _filesMap[i] = new UOFileUop(path, ".dat");
                else
                {
                    path = Path.Combine(FileManager.UoFolderPath, string.Format("map{0}.mul", i));
                    if (!File.Exists(path))
                        throw new FileNotFoundException();

                    _filesMap[i] = new UOFileMul(path);
                }


                path = Path.Combine(FileManager.UoFolderPath, string.Format("statics{0}.mul", i));
                if (File.Exists(path)) _filesStatics[i] = new UOFileMul(path);

                path = Path.Combine(FileManager.UoFolderPath, string.Format("staidx{0}.mul", i));
                if (File.Exists(path)) _filesIdxStatics[i] = new UOFileMul(path);
            }


            /*if (FileManager.ClientVersion < ClientVersions.CV_4011D)
            {
                _mapsDefaultSize[0][0] = _mapsDefaultSize[1][0] = 6144;
            }*/


            int mapblocksize = Marshal.SizeOf<MapBlock>();
            int staticidxblocksize = Marshal.SizeOf<StaidxBlock>();
            int staticblocksize = Marshal.SizeOf<StaticsBlock>();


            if (MapsDefaultSize[0][0] / 8 * (MapsDefaultSize[0][1] / 8) != _filesMap[0].Length / mapblocksize)
                MapsDefaultSize[0][0] = MapsDefaultSize[1][0] = 6144;

            for (int i = 0; i < MAPS_COUNT; i++)
            {
                MapBlocksSize[i] = new int[2] {MapsDefaultSize[i][0] / 8, MapsDefaultSize[i][1] / 8};


                int width = MapBlocksSize[i][0];
                int height = MapBlocksSize[i][1];

                int maxblockcount = width * height;

                BlockData[i] = new IndexMap[maxblockcount];

                UOFile file = _filesMap[i];
                UOFile fileidx = _filesIdxStatics[i];
                UOFile staticfile = _filesStatics[i];

                ulong staticidxaddress = (ulong) fileidx.StartAddress;
                ulong endstaticidxaddress = staticidxaddress + (ulong) fileidx.Length;

                ulong staticaddress = (ulong) staticfile.StartAddress;
                ulong endstaticaddress = staticaddress + (ulong) staticfile.Length;

                ulong mapddress = (ulong) file.StartAddress;
                ulong endmapaddress = mapddress + (ulong) file.Length;

                ulong uopoffset = 0;
                int fileNumber = -1;

                bool isuop = file is UOFileUop;

                for (int block = 0; block < maxblockcount; block++)
                {
                    ulong realmapaddress = 0, realstaticaddress = 0;
                    uint realstaticcount = 0;

                    int blocknum = block;

                    if (isuop)
                    {
                        blocknum &= 4095;
                        int shifted = block >> 12;

                        if (fileNumber != shifted)
                        {
                            fileNumber = shifted;

                            if (shifted < file.Entries.Length)
                                uopoffset = (ulong) file.Entries[shifted].Offset;
                        }
                    }

                    ulong address = mapddress + uopoffset + (ulong) (blocknum * mapblocksize);

                    if (address < endmapaddress)
                        realmapaddress = address;

                    ulong stidxaddress = staticidxaddress + (ulong) (block * staticidxblocksize);
                    StaidxBlock bb = fileidx.ReadStruct<StaidxBlock>(block * staticidxblocksize);

                    if (stidxaddress < endstaticidxaddress && bb.Size > 0 && bb.Position != 0xFFFFFFFF)
                    {
                        ulong address1 = staticaddress + bb.Position;

                        if (address1 < endstaticaddress)
                        {
                            StaticsBlock sss = staticfile.ReadStruct<StaticsBlock>(bb.Position);
                            realstaticaddress = address1;
                            realstaticcount = (uint) (bb.Size / staticblocksize);

                            if (realstaticcount > 1024)
                                realstaticcount = 1024;
                        }
                    }

                    BlockData[i][block].OriginalMapAddress = realmapaddress;
                    BlockData[i][block].OriginalStaticAddress = realstaticaddress;
                    BlockData[i][block].OriginalStaticCount = realstaticcount;

                    BlockData[i][block].MapAddress = realmapaddress;
                    BlockData[i][block].StaticAddress = realstaticaddress;
                    BlockData[i][block].StaticCount = realstaticcount;
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct StaticsBlock
    {
        public ushort Color { get; }
        public byte X { get; }
        public byte Y { get; }
        public sbyte Z { get; }
        public ushort Hue { get; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct StaidxBlock
    {
        public uint Position { get; }
        public uint Size { get; }
        public uint Unknown { get; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct MapCells
    {
        public ushort TileID { get; }
        public sbyte Z { get; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MapBlock
    {
        public uint Header;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct, SizeConst = 64)]
        public MapCells[] Cells;
    }

    public struct IndexMap
    {
        public ulong OriginalMapAddress;
        public ulong OriginalStaticAddress;
        public uint OriginalStaticCount;

        public ulong MapAddress;
        public ulong StaticAddress;
        public uint StaticCount;
    }
}