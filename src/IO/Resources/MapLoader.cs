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
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using ClassicUO.Game;
using ClassicUO.Network;
using ClassicUO.Utility;

namespace ClassicUO.IO.Resources
{
    internal class MapLoader : ResourceLoader
    {
        internal const int MAPS_COUNT = 6;
        private protected readonly UOFileMul[] _filesIdxStatics = new UOFileMul[MAPS_COUNT];
        private protected readonly UOFile[] _filesMap = new UOFile[MAPS_COUNT];
        private protected readonly UOFileMul[] _filesStatics = new UOFileMul[MAPS_COUNT];
        private readonly UOFileMul[] _mapDif = new UOFileMul[MAPS_COUNT];
        private readonly UOFileMul[] _mapDifl = new UOFileMul[MAPS_COUNT];
        private readonly UOFileMul[] _staDif = new UOFileMul[MAPS_COUNT];
        private readonly UOFileMul[] _staDifi = new UOFileMul[MAPS_COUNT];
        private readonly UOFileMul[] _staDifl = new UOFileMul[MAPS_COUNT];

        public new UOFileIndex[][] Entries = new UOFileIndex[6][]; 

        public IndexMap[][] BlockData { get; private protected set; } = new IndexMap[MAPS_COUNT][];

        public int[,] MapBlocksSize { get; private protected set; } = new int[MAPS_COUNT, 2];

        public int[,] MapsDefaultSize { get; private protected set; } = new int[MAPS_COUNT, 2]
        {
            {
                7168, 4096
            },
            {
                7168, 4096
            },
            {
                2304, 1600
            },
            {
                2560, 2048
            },
            {
                1448, 1448
            },
            {
                1280, 4096
            }
        };

        public int PatchesCount { get; private set; }

        public int[] MapPatchCount { get; } = new int[6];
        public int[] StaticPatchCount { get; } = new int[6];

        protected static UOFile GetMapFile(int map)
        {
            if (map < FileManager.Map._filesMap.Length)
                return FileManager.Map._filesMap[map];

            return null;
        }

        public override Task Load()
        {
            return Task.Run(() =>
            {
                bool foundOneMap = false;

                for (int i = 0; i < MAPS_COUNT; i++)
                {
                    string path = Path.Combine(FileManager.UoFolderPath, $"map{i}LegacyMUL.uop");

                    if (File.Exists(path))
                    {
                        _filesMap[i] = new UOFileUop(path, $"build/map{i}legacymul/{{0:D8}}.dat");
                        Entries[i] = new UOFileIndex[((UOFileUop) _filesMap[i]).TotalEntriesCount];
                        ((UOFileUop)_filesMap[i]).FillEntries(ref Entries[i], false);
                        foundOneMap = true;
                    }
                    else
                    {
                        path = Path.Combine(FileManager.UoFolderPath, $"map{i}.mul");

                        if (File.Exists(path))
                        {
                            _filesMap[i] = new UOFileMul(path);

                            foundOneMap = true;
                        }

                        path = Path.Combine(FileManager.UoFolderPath, $"mapdifl{i}.mul");

                        if (File.Exists(path))
                        {
                            _mapDifl[i] = new UOFileMul(path);
                            _mapDif[i] = new UOFileMul(Path.Combine(FileManager.UoFolderPath, $"mapdif{i}.mul"));
                            _staDifl[i] = new UOFileMul(Path.Combine(FileManager.UoFolderPath, $"stadifl{i}.mul"));
                            _staDifi[i] = new UOFileMul(Path.Combine(FileManager.UoFolderPath, $"stadifi{i}.mul"));
                            _staDif[i] = new UOFileMul(Path.Combine(FileManager.UoFolderPath, $"stadif{i}.mul"));
                        }
                    }
                    
                    path = Path.Combine(FileManager.UoFolderPath, $"statics{i}.mul");
                    if (File.Exists(path)) _filesStatics[i] = new UOFileMul(path);
                    path = Path.Combine(FileManager.UoFolderPath, $"staidx{i}.mul");
                    if (File.Exists(path)) _filesIdxStatics[i] = new UOFileMul(path);
                }

                if (!foundOneMap)
                    throw new FileNotFoundException("No maps found.");

                int mapblocksize = UnsafeMemoryManager.SizeOf<MapBlock>();

                if (_filesMap[0].Length / mapblocksize == 393216 || FileManager.ClientVersion < ClientVersions.CV_4011D)
                    MapsDefaultSize[0, 0] = MapsDefaultSize[1, 0] = 6144;

                //for (int i = 0; i < MAPS_COUNT; i++)
                Parallel.For(0, MAPS_COUNT, i =>
                {
                    MapBlocksSize[i, 0] = MapsDefaultSize[i, 0] >> 3;
                    MapBlocksSize[i, 1] = MapsDefaultSize[i, 1] >> 3;
                    LoadMap(i);
                });

                Entries = null;
            });
        }

        public override void CleanResources()
        {
           
        }


        internal unsafe void LoadMap(int i)
        {
            if (i < 0 || i > 5 || _filesMap[i] == null)
                i = 0;

            if (BlockData[i] != null || _filesMap[i] == null)
                return;

            int mapblocksize = UnsafeMemoryManager.SizeOf<MapBlock>();
            int staticidxblocksize = UnsafeMemoryManager.SizeOf<StaidxBlock>();
            int staticblocksize = UnsafeMemoryManager.SizeOf<StaticsBlock>();
            int width = MapBlocksSize[i, 0];
            int height = MapBlocksSize[i, 1];
            int maxblockcount = width * height;
            BlockData[i] = new IndexMap[maxblockcount];
            UOFile file = _filesMap[i];
            UOFile fileidx = _filesIdxStatics[i];
            UOFile staticfile = _filesStatics[i];

            if (fileidx == null && i == 1)
                fileidx = _filesIdxStatics[0];

            if (staticfile == null && i == 1)
                staticfile = _filesStatics[0];

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

                        if (shifted < Entries[i].Length)
                            uopoffset = (ulong) Entries[i][shifted].Offset;
                    }
                }

                ulong address = mapddress + uopoffset + (ulong) (blocknum * mapblocksize);

                if (address < endmapaddress)
                    realmapaddress = address;
                ulong stidxaddress = staticidxaddress + (ulong) (block * staticidxblocksize);
                StaidxBlock* bb = (StaidxBlock*) stidxaddress;

                if (stidxaddress < endstaticidxaddress && bb->Size > 0 && bb->Position != 0xFFFFFFFF)
                {
                    ulong address1 = staticaddress + bb->Position;

                    if (address1 < endstaticaddress)
                    {
                        realstaticaddress = address1;
                        realstaticcount = (uint) (bb->Size / staticblocksize);

                        if (realstaticcount > 1024)
                            realstaticcount = 1024;
                    }
                }

                ref var data = ref BlockData[i][block];
                data.MapAddress = realmapaddress;
                data.StaticAddress = realstaticaddress;
                data.StaticCount = realstaticcount;
                data.OriginalMapAddress = realmapaddress;
                data.OriginalStaticAddress = realstaticaddress;
                data.OriginalStaticCount = realstaticcount;
            }
        }


        public void PatchMapBlock(ulong block, ulong address)
        {
            int w = MapBlocksSize[0, 0];
            int h = MapBlocksSize[0, 1];

            int maxBlockCount = w * h;

            if (maxBlockCount < 1)
                return;

            BlockData[0][block].OriginalMapAddress = address;
            BlockData[0][block].MapAddress = address;
        }

        public unsafe bool ApplyPatches(Packet reader)
        {
            ResetPatchesInBlockTable();

            PatchesCount = (int) reader.ReadUInt();

            if (PatchesCount < 0)
                PatchesCount = 0;

            if (PatchesCount > MAPS_COUNT)
                PatchesCount = MAPS_COUNT;

            Array.Clear(MapPatchCount, 0, MapPatchCount.Length);
            Array.Clear(StaticPatchCount, 0, StaticPatchCount.Length);

            bool result = false;

            for (int i = 0; i < PatchesCount; i++)
            {
                if (_filesMap[i] == null || _filesMap[i].StartAddress == IntPtr.Zero)
                {
                    reader.Skip(8);

                    continue;
                }

                int mapPatchesCount = (int) reader.ReadUInt();
                MapPatchCount[i] = mapPatchesCount;
                int staticPatchesCount = (int) reader.ReadUInt();
                StaticPatchCount[i] = staticPatchesCount;

                int w = MapBlocksSize[i, 0];
                int h = MapBlocksSize[i, 1];

                int maxBlockCount = w * h;

                if (mapPatchesCount != 0)
                {
                    var difl = _mapDifl[i];
                    var dif = _mapDif[i];

                    if (difl == null || dif == null || difl.Length == 0 || dif.Length == 0)
                        continue;

                    mapPatchesCount = Math.Min(mapPatchesCount, (int) difl.Length >> 2);

                    difl.Seek(0);
                    dif.Seek(0);

                    for (int j = 0; j < mapPatchesCount; j++)
                    {
                        uint blockIndex = difl.ReadUInt();

                        if (blockIndex < maxBlockCount)
                        {
                            BlockData[i][blockIndex].MapAddress = (ulong) dif.PositionAddress;
                            result = true;
                        }

                        dif.Skip(UnsafeMemoryManager.SizeOf<MapBlock>());
                    }
                }

                if (staticPatchesCount != 0)
                {
                    var difl = _staDifl[i];
                    var difi = _staDifi[i];

                    if (difl == null || difi == null || _staDif[i] == null || difl.Length == 0 || difi.Length == 0 || _staDif[i].Length == 0)
                        continue;

                    ulong startAddress = (ulong) _staDif[i].StartAddress;

                    staticPatchesCount = Math.Min(staticPatchesCount, (int) difl.Length >> 2);

                    difl.Seek(0);
                    difi.Seek(0);

                    int sizeOfStaicsBlock = UnsafeMemoryManager.SizeOf<StaticsBlock>();

                    for (int j = 0; j < staticPatchesCount; j++)
                    {
                        uint blockIndex = difl.ReadUInt();

                        StaidxBlock* sidx = (StaidxBlock*) difi.PositionAddress;

                        difi.Skip(sizeof(StaidxBlock));

                        if (blockIndex < maxBlockCount)
                        {
                            ulong realStaticAddress = 0;
                            int realStaticCount = 0;

                            if (sidx->Size > 0 && sidx->Position != 0xFFFF_FFFF)
                            {
                                realStaticAddress = startAddress + sidx->Position;
                                realStaticCount = (int) (sidx->Size / sizeOfStaicsBlock);

                                if (realStaticCount > 0)
                                {
                                    if (realStaticCount > 1024)
                                        realStaticCount = 1024;
                                }
                            }

                            BlockData[i][blockIndex].StaticAddress = realStaticAddress;
                            BlockData[i][blockIndex].StaticCount = (uint) realStaticCount;

                            result = true;
                        }
                    }
                }
            }

            return result;
        }

        private void ResetPatchesInBlockTable()
        {
            for (int i = 0; i < MAPS_COUNT; i++)
            {
                var list = BlockData[i];

                if (list == null)
                    continue;

                int w = MapBlocksSize[i, 0];
                int h = MapBlocksSize[i, 1];

                int maxBlockCount = w * h;

                if (maxBlockCount < 1)
                    return;

                if (_filesMap[i] is UOFileMul mul && mul.StartAddress != IntPtr.Zero)
                {
                    if (_filesIdxStatics[i] is UOFileMul stIdxMul && stIdxMul.StartAddress != IntPtr.Zero)
                    {
                        if (_filesStatics[i] is UOFileMul stMul && stMul.StartAddress != IntPtr.Zero)
                        {
                            for (int block = 0; block < maxBlockCount; block++)
                            {
                                ref IndexMap index = ref list[block];
                                index.MapAddress = index.OriginalMapAddress;
                                index.StaticAddress = index.OriginalStaticAddress;
                                index.StaticCount = index.OriginalStaticCount;
                            }
                        }
                    }
                }
            }
        }

        public void SanitizeMapIndex(ref int map)
        {
            if (map == 1 && (_filesMap[1] == null || _filesMap[1].StartAddress == IntPtr.Zero || _filesStatics[1] == null || _filesStatics[1].StartAddress == IntPtr.Zero || _filesIdxStatics[1] == null || _filesIdxStatics[1].StartAddress == IntPtr.Zero))
                map = 0;
        }

        public unsafe RadarMapBlock? GetRadarMapBlock(int map, int blockX, int blockY)
        {
            SanitizeMapIndex(ref map);

            ref IndexMap indexMap = ref GetIndex(map, blockX, blockY);

            if (indexMap.MapAddress == 0)
                return null;

            MapBlock* mp = (MapBlock*) indexMap.MapAddress;
            MapCells* cells = (MapCells*) &mp->Cells;

            RadarMapBlock mb = new RadarMapBlock
            {
                Cells = new RadarMapcells[8, 8]
            };

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    ref MapCells cell = ref cells[(y << 3) + x];
                    ref RadarMapcells outcell = ref mb.Cells[x, y];
                    outcell.Graphic = cell.TileID;
                    outcell.Z = cell.Z;
                    outcell.IsLand = true;
                }
            }

            StaticsBlock* sb = (StaticsBlock*) indexMap.StaticAddress;

            if (sb != null)
            {
                int count = (int) indexMap.StaticCount;

                for (int c = 0; c < count; c++)
                {
                    if (sb->Color > 0 && sb->Color != 0xFFFF && !GameObjectHelper.IsNoDrawable(sb->Color))
                    {
                        ref RadarMapcells outcell = ref mb.Cells[sb->X, sb->Y];

                        if (outcell.Z <= sb->Z)
                        {
                            outcell.Graphic = sb->Hue > 0 ? (ushort)(sb->Hue + 0x4000) : sb->Color;
                            outcell.Z = sb->Z;
                            outcell.IsLand = sb->Hue > 0;
                        }
                    }

                    sb++;
                }
            }

            return mb;
        }

        [MethodImpl(256)]
        public ref IndexMap GetIndex(int map, int x, int y)
        {
            int block = x * MapBlocksSize[map, 1] + y;

            return ref BlockData[map][block];
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal readonly struct StaticsBlock
    {
        public readonly ushort Color;
        public readonly byte X;
        public readonly byte Y;
        public readonly sbyte Z;
        public readonly ushort Hue;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal readonly struct StaidxBlock
    {
        public readonly uint Position;
        public readonly uint Size;
        public readonly uint Unknown;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct MapCells
    {
        public ushort TileID;
        public sbyte Z;
    }

    //[StructLayout(LayoutKind.Sequential, Pack = 1)]
    //internal struct MapBlock
    //{
    //    public readonly uint Header;
    //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
    //    public MapCells[] Cells;
    //}

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 4 + 64 * 3)]
    internal struct MapBlock
    {
        public readonly uint Header;
        public unsafe MapCells* Cells;
    }

    //[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 4 + 64 * 3)]
    //internal struct MapBlock2
    //{
    //    public readonly uint Header;
    //    public IntPtr Cells;
    //}

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct RadarMapcells
    {
        public ushort Graphic;
        public sbyte Z;
        public bool IsLand;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct RadarMapBlock
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public RadarMapcells[,] Cells;
    }

    internal struct IndexMap
    {
        public ulong MapAddress;
        public ulong OriginalMapAddress;
        public ulong OriginalStaticAddress;
        public uint OriginalStaticCount;
        public ulong StaticAddress;
        public uint StaticCount;
        public static readonly IndexMap Invalid = new IndexMap();
    }
}