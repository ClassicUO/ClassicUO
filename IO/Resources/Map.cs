#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
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
using ClassicUO.IO;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ClassicUO.IO.Resources
{
    public static class Map
    {
        private const int MAPS_COUNT = 6;

        private static readonly UOFile[] _filesMap = new UOFile[MAPS_COUNT];
        private static readonly UOFileMul[] _filesStatics = new UOFileMul[MAPS_COUNT];
        private static readonly UOFileMul[] _filesIdxStatics = new UOFileMul[MAPS_COUNT];

        public static IndexMap[][] BlockData { get; } = new IndexMap[MAPS_COUNT][];
        public static int[][] MapBlocksSize { get; } = new int[MAPS_COUNT][];
        public static int[][] MapsDefaultSize { get; } = new int[MAPS_COUNT][] { new int[2] { 7168, 4096 }, new int[2] { 7168, 4096 }, new int[2] { 2304, 1600 }, new int[2] { 2560, 2048 }, new int[2] { 1448, 1448 }, new int[2] { 1280, 4096 } };



        public unsafe static void Load()
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


            int mapblocksize = Marshal.SizeOf<MapBlock>();
         
            if (_filesMap[0].Length / mapblocksize == 393216 || FileManager.ClientVersion < ClientVersions.CV_4011D)
                MapsDefaultSize[0][0] = MapsDefaultSize[1][0] = 6144;


            for (int i = 0; i < MAPS_COUNT; i++)
            {
                MapBlocksSize[i] = new int[2] { MapsDefaultSize[i][0] / 8, MapsDefaultSize[i][1] / 8 };               
            }
        }


        public static void LoadMap(int i)
        {
            int mapblocksize = Marshal.SizeOf<MapBlock>();
            int staticidxblocksize = Marshal.SizeOf<StaidxBlock>();
            int staticblocksize = Marshal.SizeOf<StaticsBlock>();


            int width = MapBlocksSize[i][0];
            int height = MapBlocksSize[i][1];

            int maxblockcount = width * height;

            BlockData[i] = new IndexMap[maxblockcount];

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

                if (address < endmapaddress)
                    realmapaddress = address;

                ulong stidxaddress = staticidxaddress + (ulong)(block * staticidxblocksize);
                StaidxBlock bb = fileidx.ReadStruct<StaidxBlock>(block * staticidxblocksize);

                if (stidxaddress < endstaticidxaddress && bb.Size > 0 && bb.Position != 0xFFFFFFFF)
                {
                    ulong address1 = staticaddress + bb.Position;

                    if (address1 < endstaticaddress)
                    {
                        StaticsBlock sss = staticfile.ReadStruct<StaticsBlock>(bb.Position);
                        realstaticaddress = address1;
                        realstaticcount = (uint)(bb.Size / staticblocksize);

                        if (realstaticcount > 1024)
                            realstaticcount = 1024;
                    }
                }

                BlockData[i][block] = new IndexMap
                {
                    OriginalMapAddress = realmapaddress,
                    OriginalStaticAddress = realstaticaddress,
                    OriginalStaticCount = realstaticcount,

                    MapAddress = realmapaddress,
                    StaticAddress = realstaticaddress,
                    StaticCount = realstaticcount
                };
            }
        }

        public static void UnloadMap(int i)
        {
            BlockData[i] = null;
        }

        public static unsafe RadarMapBlock? GetRadarMapBlock(int map, int blockX, int blockY)
        {
            var indexMap = GetIndex(map, blockX, blockY);
            if (indexMap == null || indexMap.MapAddress == 0)
                return null;

            MapBlock block = Marshal.PtrToStructure<MapBlock>((IntPtr)indexMap.MapAddress);
            RadarMapBlock mb = new RadarMapBlock();
            mb.Cells = new RadarMapcells[8, 8];

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    ref var cell = ref block.Cells[(y * 8) + x];
                    ref var outcell = ref mb.Cells[x, y];
                    outcell.Graphic = cell.TileID;
                    outcell.Z = cell.Z;
                    outcell.IsLand = true;
                }
            }

            StaticsBlock* sb = (StaticsBlock*)indexMap.StaticAddress;

            if (sb != null)
            {
                int count = (int)indexMap.StaticCount;

                for (int c = 0; c < count; c++)
                {
                    if (sb->Color > 0 && sb->Color != 0xFFFF && !Game.Views.View.IsNoDrawable(sb->Color))
                    {
                        ref var outcell = ref mb.Cells[sb->X, sb->Y];
                        if (outcell.Z <= sb->Z)
                        {
                            outcell.Graphic = sb->Color;
                            outcell.Z = sb->Z;
                            outcell.IsLand = false;
                        }
                    }
                    sb++;
                }
            }

            return mb;
        }

        public static IndexMap GetIndex(int map,int x, int y)
        {
            int block = x * MapBlocksSize[map][1] + y;
            return BlockData[map][block];
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct StaticsBlock
    {
        public readonly ushort Color;
        public readonly byte X;
        public readonly byte Y;
        public readonly sbyte Z;
        public readonly ushort Hue;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct StaidxBlock
    {
        public readonly uint Position;
        public readonly uint Size;
        public readonly uint Unknown;
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
        public readonly uint Header;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public MapCells[] Cells;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RadarMapcells
    {
        public ushort Graphic;
        public sbyte Z;
        public bool IsLand;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RadarMapBlock
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public RadarMapcells[,] Cells;
    }

    public class IndexMap
    {
        public ulong OriginalMapAddress;
        public ulong OriginalStaticAddress;
        public uint OriginalStaticCount;

        public ulong MapAddress;
        public ulong StaticAddress;
        public uint StaticCount;
    }
}