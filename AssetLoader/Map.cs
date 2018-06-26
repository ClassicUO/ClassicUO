using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace ClassicUO.Assets
{
    public static class Map
    {
        const int MAPS_COUNT = 6;

        private static readonly int[][] _mapsDefaultSize = new int[MAPS_COUNT][]
        {
            new int[2] { 7168, 4096 },
            new int[2] { 7168, 4096 },
            new int[2] { 2304, 1600 },
            new int[2] { 2560, 2048 },
            new int[2] { 1448, 1448 },
            new int[2] { 1280, 4096 },
        };

        private static readonly int[][] _mapsSize = new int[MAPS_COUNT][];
        private static readonly int[][] _mapsBlockSize = new int[MAPS_COUNT][];
        private static readonly UOFile[] _filesMap = new UOFile[MAPS_COUNT];
        private static readonly UOFileMul[] _filesStatics = new UOFileMul[MAPS_COUNT];
        private static readonly UOFileMul[] _filesIdxStatics = new UOFileMul[MAPS_COUNT];

        private static readonly IndexMap[][] _blockData = new IndexMap[MAPS_COUNT][];

        public static IndexMap[][] BlockData => _blockData;

        public unsafe static void Load()
        {
            string path = string.Empty;

            for (int i = 0; i < MAPS_COUNT; i++)
            {
                path = Path.Combine(FileManager.UoFolderPath, string.Format("map{0}LegacyMUL.uop", i));
                if (File.Exists(path))
                {
                    _filesMap[i] = new UOFileUop(path, ".dat");
                }
                else
                {
                    path = Path.Combine(FileManager.UoFolderPath, string.Format("map{0}.mul", i));
                    if (!File.Exists(path))
                        throw new FileNotFoundException();

                    _filesMap[i] = new UOFileMul(path);
                }


                path = Path.Combine(FileManager.UoFolderPath, string.Format("statics{0}.mul", i));               
                if (File.Exists(path))
                {
                    _filesStatics[i] = new UOFileMul(path);
                }

                path = Path.Combine(FileManager.UoFolderPath, string.Format("staidx{0}.mul", i));
                if (File.Exists(path))
                {
                    _filesIdxStatics[i] = new UOFileMul(path);
                }
            }


            if (FileManager.ClientVersion < ClientVersions.CV_4011D)
            {
                _mapsDefaultSize[0][0] = _mapsDefaultSize[1][0] = 6144;
            }

            int mapblocksize = Marshal.SizeOf<MapBlock>();
            int staticidxblocksize = Marshal.SizeOf<StaidxBlock>();
            int staticblocksize = Marshal.SizeOf<StaticsBlock>();

            for (int i = 0; i < MAPS_COUNT; i++)
            {
                _mapsBlockSize[i] = new int[2] { _mapsDefaultSize[i][0] / 8, _mapsDefaultSize[i][1] / 8 } ;


                int width = _mapsBlockSize[i][0];
                int height = _mapsBlockSize[i][1];

                int maxblockcount = width * height;

                _blockData[i] = new IndexMap[maxblockcount];

                UOFile file = _filesMap[i];
                UOFile fileidx = _filesIdxStatics[i];
                UOFile staticfile = _filesStatics[i];

                ulong staticidxaddress = (ulong)fileidx.StartAddress;
                ulong endstaticidxaddress = staticidxaddress + (ulong)fileidx.Length;

                ulong staticaddress = (ulong)staticfile.StartAddress;
                ulong endstaticaddress = staticaddress + (ulong)staticfile.Length;

                ulong mapddress = (ulong)file.StartAddress;
                ulong endmapaddress = mapddress + (ulong)file.Length;

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
                                uopoffset = (ulong)file.Entries[shifted].Offset;
                        }
                    }

                    ulong address = mapddress + uopoffset + (ulong)(blocknum * mapblocksize);

                    //MapBlock mapblock = Marshal.PtrToStructure<MapBlock>((IntPtr)address);

                     //file.Accessor.Read<MapBlock>((long)address, out var mapblock);

                    if (address < endmapaddress)
                        realmapaddress = address;


                    ulong stidxaddress = (staticidxaddress + (ulong)(block * staticidxblocksize));
                    StaidxBlock bb = fileidx.ReadStruct<StaidxBlock>(block * staticidxblocksize); //Marshal.PtrToStructure<StaidxBlock>((IntPtr)stidxaddress);

                    if (stidxaddress < endstaticidxaddress
                        && bb.Size > 0 && bb.Position != 0xFFFFFFFF)
                    {
                        ulong address1 = staticaddress + bb.Position;

                        if (address1 < endstaticaddress)
                        {
                            //StaticsBlock sss = Marshal.PtrToStructure<StaticsBlock>((IntPtr)address1);
                            StaticsBlock sss = staticfile.ReadStruct<StaticsBlock>(bb.Position);
                            realstaticaddress = address1;
                            realstaticcount = (uint)(bb.Size / staticblocksize);

                            if (realstaticcount > 1024)
                                realstaticcount = 1024;
                        }
                    }


                    _blockData[i][block].OriginalMapAddress = realmapaddress;
                    _blockData[i][block].OriginalStaticAddress = realstaticaddress;
                    _blockData[i][block].OriginalStaticCount = realstaticcount;

                    _blockData[i][block].MapAddress = realmapaddress;
                    _blockData[i][block].StaticAddress = realstaticaddress;
                    _blockData[i][block].StaticCount = realstaticcount;
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct StaticsBlock
    {
        public ushort Color;
        public byte X;
        public byte Y;
        public sbyte Z;
        public ushort Hue;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct StaidxBlock
    {
        public uint Position;
        public uint Size;
        public uint Unknown;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MapCells
    {
        public ushort TileID;
        public sbyte Z;
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
