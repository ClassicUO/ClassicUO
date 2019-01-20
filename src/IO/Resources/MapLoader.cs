using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Network;
using ClassicUO.Utility;

namespace ClassicUO.IO.Resources
{
    class MapLoader : ResourceLoader
    {
        internal (UOFile[], UOFileMul[], UOFileMul[]) GetFileReferences => (_filesMap, _filesStatics, _filesIdxStatics);
        internal const int MAPS_COUNT = 6;
        private readonly UOFile[] _filesMap = new UOFile[MAPS_COUNT];
        private readonly UOFileMul[] _filesStatics = new UOFileMul[MAPS_COUNT];
        private readonly UOFileMul[] _filesIdxStatics = new UOFileMul[MAPS_COUNT];

        private readonly UOFileMul[] _mapDifl = new UOFileMul[MAPS_COUNT];
        private readonly UOFileMul[] _mapDif = new UOFileMul[MAPS_COUNT];

        private readonly UOFileMul[] _staDifl = new UOFileMul[MAPS_COUNT];
        private readonly UOFileMul[] _staDifi = new UOFileMul[MAPS_COUNT];
        private readonly UOFileMul[] _staDif = new UOFileMul[MAPS_COUNT];


        public IndexMap[][] BlockData { get; } = new IndexMap[MAPS_COUNT][];

        public int[,] MapBlocksSize { get; } = new int[MAPS_COUNT, 2];

        public int[,] MapsDefaultSize { get; } = new int[MAPS_COUNT, 2]
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

        public override void Load()
        {
            bool foundedOneMap = false;

            for (int i = 0; i < MAPS_COUNT; i++)
            {
                string path = Path.Combine(FileManager.UoStaticsMapPath, $"map{i}LegacyMUL.uop");

                if (File.Exists(path))
                {
                    _filesMap[i] = new UOFileUop(path, ".dat", loadentries: false);
                    foundedOneMap = true;
                }
                else
                {
                    path = Path.Combine(FileManager.UoStaticsMapPath, $"map{i}.mul");

                    if (File.Exists(path))
                    {
                        _filesMap[i] = new UOFileMul(path, false);
                        foundedOneMap = true;
                    }

                    path = Path.Combine(FileManager.UoStaticsMapPath, $"mapdifl{i}.mul");

                    if (File.Exists(path))
                    {
                        _mapDifl[i] = new UOFileMul(path);
                        _mapDif[i] = new UOFileMul(Path.Combine(FileManager.UoStaticsMapPath, $"mapdif{i}.mul"));

                        _staDifl[i] = new UOFileMul(Path.Combine(FileManager.UoStaticsMapPath, $"stadifl{i}.mul"));
                        _staDifi[i] = new UOFileMul(Path.Combine(FileManager.UoStaticsMapPath, $"stadifi{i}.mul"));
                        _staDif[i] = new UOFileMul(Path.Combine(FileManager.UoStaticsMapPath, $"stadif{i}.mul"));
                    }
                }

                path = Path.Combine(FileManager.UoStaticsMapPath, $"statics{i}.mul");
                if (File.Exists(path)) _filesStatics[i] = new UOFileMul(path, false);
                path = Path.Combine(FileManager.UoStaticsMapPath, $"staidx{i}.mul");
                if (File.Exists(path)) _filesIdxStatics[i] = new UOFileMul(path, false);
            }

            if (!foundedOneMap)
                throw new FileNotFoundException("No maps founded.");
            int mapblocksize = UnsafeMemoryManager.SizeOf<MapBlock>();

            if (_filesMap[0].Length / mapblocksize == 393216 || FileManager.ClientVersion < ClientVersions.CV_4011D)
                MapsDefaultSize[0, 0] = MapsDefaultSize[1, 0] = 6144;

            for (int i = 0; i < MAPS_COUNT; i++)
            {
                MapBlocksSize[i, 0] = MapsDefaultSize[i, 0] >> 3;
                MapBlocksSize[i, 1] = MapsDefaultSize[i, 1] >> 3;

                if (Engine.GlobalSettings.PreloadMaps)
                    LoadMap(i);
            }
        }

        internal UOFile UltimaLiveReloadMaps(int mapID)
        {
            if(_filesMap[mapID] is UOFileUop uop)
            {
                string newpath = Path.Combine(UltimaLive.ShardName, $"map{mapID}.mul");
                if (!File.Exists(newpath))
                {
                    Utility.Logging.Log.Message(Utility.Logging.LogTypes.Trace, $"UltimaLive -> converting file:\t{newpath} from {uop.FilePath}");
                    using (FileStream stream = File.Create(newpath))
                    {
                        for (int x = 0; x < uop.Entries.Length; x++)
                        {
                            uop.Seek(uop.Entries[x].Offset);
                            stream.Write(uop.ReadArray(uop.Entries[x].Length), 0, uop.Entries[x].Length);
                        }
                        stream.Flush();
                    }
                }
                _filesMap[mapID].Dispose();
                _filesMap[mapID] = new UOFileMul(newpath);
            }
            return _filesMap[mapID];
        }

        private bool LoadDif(ref UOFileMul mul, string path)
        {
            if (!File.Exists(path))
                return false;

            return true;
        }

        protected override void CleanResources()
        {
            throw new NotImplementedException();
            /*for (int i = 0; i < MAPS_COUNT; i++)
                UnloadMap(i);*/
        }


        public unsafe void LoadMap(int i)
        {
            if (Engine.GlobalSettings.PreloadMaps && BlockData[i] != null)
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
                StaidxBlock* bb = (StaidxBlock*)stidxaddress;

                if (stidxaddress < endstaticidxaddress && bb->Size > 0 && bb->Position != 0xFFFFFFFF)
                {
                    ulong address1 = staticaddress + bb->Position;

                    if (address1 < endstaticaddress)
                    {
                        realstaticaddress = address1;
                        realstaticcount = (uint)(bb->Size / staticblocksize);

                        if (realstaticcount > 1024)
                            realstaticcount = 1024;
                    }
                }

                BlockData[i][block] = new IndexMap(realmapaddress, realstaticaddress, realstaticcount, realmapaddress, realstaticaddress, realstaticcount);
            }
        }

        internal unsafe void ReloadBlock(int map, int blocknum)
        {
            int mapblocksize = UnsafeMemoryManager.SizeOf<MapBlock>();
            int staticidxblocksize = UnsafeMemoryManager.SizeOf<StaidxBlock>();
            int staticblocksize = UnsafeMemoryManager.SizeOf<StaticsBlock>();
            UOFile file = _filesMap[map];
            UOFile fileidx = _filesIdxStatics[map];
            UOFile staticfile = _filesStatics[map];
            ulong staticidxaddress = (ulong)fileidx.StartAddress;
            ulong endstaticidxaddress = staticidxaddress + (ulong)fileidx.Length;
            ulong staticaddress = (ulong)staticfile.StartAddress;
            ulong endstaticaddress = staticaddress + (ulong)staticfile.Length;
            ulong mapddress = (ulong)file.StartAddress;
            ulong endmapaddress = mapddress + (ulong)file.Length;
            ulong uopoffset = 0;
            int fileNumber = -1;
            bool isuop = file is UOFileUop;
            ulong realmapaddress = 0, realstaticaddress = 0;
            uint realstaticcount = 0;
            int block = blocknum;

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
            StaidxBlock* bb = (StaidxBlock*)stidxaddress;

            if (stidxaddress < endstaticidxaddress && bb->Size > 0 && bb->Position != 0xFFFFFFFF)
            {
                ulong address1 = staticaddress + bb->Position;

                if (address1 < endstaticaddress)
                {
                    realstaticaddress = address1;
                    realstaticcount = (uint)(bb->Size / staticblocksize);

                    if (realstaticcount > 1024)
                        realstaticcount = 1024;
                }
            }
            BlockData[map][block] = new IndexMap(realmapaddress, realstaticaddress, realstaticcount, realmapaddress, realstaticaddress, realstaticcount);
        }

        public void UnloadMap(int i)
        {
            if (Engine.GlobalSettings.PreloadMaps)
                return;

            if (BlockData[i] != null)
            {
                BlockData[i] = null;
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

        public int PatchesCount { get; private set; }

        public int[] MapPatchCount { get; } = new int[6];
        public int[] StaticPatchCount { get; } = new int[6];

        public unsafe bool ApplyPatches(Packet reader)
        {
            ResetPatchesInBlockTable();

            PatchesCount = (int) reader.ReadUInt();

            if (PatchesCount < 0)
                PatchesCount = 0;

            if (PatchesCount > MAPS_COUNT)
                PatchesCount = MAPS_COUNT;

            MapPatchCount.ForEach(s => s = 0);
            StaticPatchCount.ForEach(s => s = 0);

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

                    mapPatchesCount = Math.Min(mapPatchesCount, (int) (difl.Length / 4));

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
                    ulong startAddress = (ulong) _staDif[i].StartAddress;

                    staticPatchesCount = Math.Min(staticPatchesCount, (int) (difl.Length / 4));

                    difl.Seek(0);
                    difi.Seek(0);

                    int sizeOfStaicsBlock = UnsafeMemoryManager.SizeOf<StaticsBlock>();

                    for (int j = 0; j < staticPatchesCount; j++)
                    {
                        uint blockIndex = difl.ReadUInt();
                        StaidxBlock* sidx = (StaidxBlock*)difi.PositionAddress;

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

        public unsafe RadarMapBlock? GetRadarMapBlock(int map, int blockX, int blockY)
        {
            IndexMap indexMap = GetIndex(map, blockX, blockY);

            if (indexMap.MapAddress == 0)
                return null;
            MapBlock* mp = (MapBlock*)indexMap.MapAddress;
            MapCells* cells = (MapCells*)&mp->Cells;

            RadarMapBlock mb = new RadarMapBlock
            {
                Cells = new RadarMapcells[8, 8]
            };

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    ref MapCells cell = ref cells[y * 8 + x];
                    ref RadarMapcells outcell = ref mb.Cells[x, y];
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
                    if (sb->Color > 0 && sb->Color != 0xFFFF && !GameObjectHelper.IsNoDrawable(sb->Color))
                    {
                        ref RadarMapcells outcell = ref mb.Cells[sb->X, sb->Y];

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

        public IndexMap GetIndex(int map, int x, int y)
        {
            int block = x * MapBlocksSize[map, 1] + y;

            return BlockData[map][block];
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
        public IndexMap(ulong mapAddress, ulong staticAddress, uint staticCount, ulong originalMapAddress, ulong originalStaticAddress, uint originalStaticCount)
        {
            MapAddress = mapAddress;
            StaticAddress = staticAddress;
            StaticCount = staticCount;
            OriginalMapAddress = originalMapAddress;
            OriginalStaticAddress = originalStaticAddress;
            OriginalStaticCount = originalStaticCount;
        }

        public ulong MapAddress;
        public ulong OriginalMapAddress;
        public ulong OriginalStaticAddress;
        public uint OriginalStaticCount;
        public ulong StaticAddress;
        public uint StaticCount;
        public static readonly IndexMap Invalid = new IndexMap();
    }
}
