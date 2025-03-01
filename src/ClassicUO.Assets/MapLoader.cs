// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.IO;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ClassicUO.Assets
{
    public class MapLoader : UOFileLoader
    {
        private UOFileMul[] _mapDif;
        private UOFileMul[] _mapDifl;
        private UOFileMul[] _staDif;
        private UOFileMul[] _staDifi;
        private UOFileMul[] _staDifl;

        // cannot be a const, due to UOLive implementation
        public static int MAPS_COUNT = 6;

        public MapLoader(UOFileManager fileManager) : base(fileManager)
        {
        }

        public string MapsLayouts { get; set; }

        public IndexMap[][] BlockData { get; private set; }

        public int[,] MapBlocksSize { get; private set; }

        // ReSharper disable RedundantExplicitArraySize
        public int[,] MapsDefaultSize { get; protected set; } = new int[6, 2]
            // ReSharper restore RedundantExplicitArraySize
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
        public int[] MapPatchCount { get; private set; }
        public int[] StaticPatchCount { get; private set; }

        protected FileReader[] _filesStatics, _filesIdxStatics, _filesStaticsX, _filesIdxStaticsX;
        protected FileReader[] _filesMap, _filesMapX;

        protected FileReader[] _currentMapFiles;
        protected FileReader[] _currentStaticsFiles, _currentIdxStaticsFiles;

        public FileReader GetMapFile(int map)
        {
            return map < _currentMapFiles.Length ? _currentMapFiles[map] : null;
        }

        public FileReader GetStaticFile(int map)
        {
            return map < _currentStaticsFiles.Length ? _currentStaticsFiles[map] : null;
        }

        protected void Initialize()
        {
            _filesMap = new FileReader[MAPS_COUNT];
            _filesStatics = new FileReader[MAPS_COUNT];
            _filesIdxStatics = new FileReader[MAPS_COUNT];

            _filesMapX = new FileReader[MAPS_COUNT];
            _filesStaticsX = new FileReader[MAPS_COUNT];
            _filesIdxStaticsX = new FileReader[MAPS_COUNT];

            MapPatchCount = new int[MAPS_COUNT];
            StaticPatchCount = new int[MAPS_COUNT];
            MapBlocksSize = new int[MAPS_COUNT, 2];

            BlockData = new IndexMap[MAPS_COUNT][];

            _mapDif = new UOFileMul[MAPS_COUNT];
            _mapDifl = new UOFileMul[MAPS_COUNT];
            _staDif = new UOFileMul[MAPS_COUNT];
            _staDifi = new UOFileMul[MAPS_COUNT];
            _staDifl = new UOFileMul[MAPS_COUNT];
        }

        public override unsafe void Load()
        {
            bool foundOneMap = false;

            if (!string.IsNullOrEmpty(MapsLayouts))
            {
                string[] values = MapsLayouts.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                MAPS_COUNT = values.Length;
                MapsDefaultSize = new int[values.Length, 2];

                Log.Trace($"default maps size overraided. [count: {MAPS_COUNT}]");


                int index = 0;

                char[] splitchar = new char[1] { ',' };

                foreach (string s in values)
                {
                    string[] v = s.Split(splitchar, StringSplitOptions.RemoveEmptyEntries);

                    if (v.Length >= 2 && int.TryParse(v[0], out int width) && int.TryParse(v[1], out int height))
                    {
                        MapsDefaultSize[index, 0] = width;
                        MapsDefaultSize[index, 1] = height;

                        Log.Trace($"overraided map size: {width},{height}  [index: {index}]");
                    }
                    else
                    {
                        Log.Error($"Error parsing 'width,height' values: '{s}'");
                    }

                    ++index;
                }
            }


            Initialize();

            for (var i = 0; i < MAPS_COUNT; ++i)
            {
                var path = FileManager.GetUOFilePath($"map{i}LegacyMUL.uop");

                if (FileManager.IsUOPInstallation && File.Exists(path))
                {
                    var uopFile = new UOFileUop(path, $"build/map{i}legacymul/{{0:D8}}.dat");
                    uopFile.FillEntries();

                    _filesMap[i] = uopFile;

                    path = FileManager.GetUOFilePath($"map{i}xLegacyMUL.uop");
                    if (File.Exists(path))
                    {
                        var uopFileX = new UOFileUop(path, $"build/map{i}legacymul/{{0:D8}}.dat");
                        uopFileX.FillEntries();
                        _filesMapX[i] = uopFileX;
                    }


                    foundOneMap = true;
                }
                else
                {
                    path = FileManager.GetUOFilePath($"map{i}.mul");

                    if (File.Exists(path))
                    {
                        _filesMap[i] = new UOFileMul(path);

                        path = FileManager.GetUOFilePath($"map{i}x.mul");
                        if (File.Exists(path))
                        {
                            _filesMapX[i] = new UOFileMul(path);
                        }

                        foundOneMap = true;
                    }

                    path = FileManager.GetUOFilePath($"mapdifl{i}.mul");

                    if (File.Exists(path))
                    {
                        _mapDifl[i] = new UOFileMul(path);
                        _mapDif[i] = new UOFileMul(FileManager.GetUOFilePath($"mapdif{i}.mul"));
                        _staDifl[i] = new UOFileMul(FileManager.GetUOFilePath($"stadifl{i}.mul"));
                        _staDifi[i] = new UOFileMul(FileManager.GetUOFilePath($"stadifi{i}.mul"));
                        _staDif[i] = new UOFileMul(FileManager.GetUOFilePath($"stadif{i}.mul"));
                    }
                }

                path = FileManager.GetUOFilePath($"statics{i}.mul");
                if (File.Exists(path))
                {
                    _filesStatics[i] = new UOFileMul(path);
                }

                path = FileManager.GetUOFilePath($"staidx{i}.mul");
                if (File.Exists(path))
                {
                    _filesIdxStatics[i] = new UOFileMul(path);
                }


                path = FileManager.GetUOFilePath($"statics{i}x.mul");
                if (File.Exists(path))
                {
                    _filesStaticsX[i] = new UOFileMul(path);
                }

                path = FileManager.GetUOFilePath($"staidx{i}x.mul");
                if (File.Exists(path))
                {
                    _filesIdxStaticsX[i] = new UOFileMul(path);
                }
            }

            if (!foundOneMap)
            {
                throw new FileNotFoundException("No maps found.");
            }


            int mapblocksize = sizeof(MapBlock);

            if (_filesMap[0].Length / mapblocksize == 393216 || FileManager.Version < ClientVersion.CV_4011D)
            {
                MapsDefaultSize[0, 0] = MapsDefaultSize[1, 0] = 6144;
            }

            // This is an hack to patch correctly all maps when you have to fake map1
            if (_filesMap[1] == null || _filesMap[1].Length == 0)
            {
                _filesMap[1] = _filesMap[0];
                _filesStatics[1] = _filesStatics[0];
                _filesIdxStatics[1] = _filesIdxStatics[0];
            }

            if (_filesMapX[1] == null || _filesMapX[1].Length == 0)
            {
                _filesMapX[1] = _filesMapX[0];
                _filesStaticsX[1] = _filesStaticsX[0];
                _filesIdxStaticsX[1] = _filesIdxStaticsX[0];
            }


            _currentMapFiles = new FileReader[_filesMap.Length];
            _currentIdxStaticsFiles = new FileReader[_filesIdxStatics.Length];
            _currentStaticsFiles = new FileReader[_filesStatics.Length];

            _filesMap.CopyTo(_currentMapFiles, 0);
            _filesIdxStatics.CopyTo(_currentIdxStaticsFiles, 0);
            _filesStatics.CopyTo(_currentStaticsFiles, 0);
        }

        public unsafe void LoadMap(int i, bool useXFiles = false)
        {
            if (i < 0 || i + 1 > MAPS_COUNT || _currentMapFiles[i] == null)
            {
                i = 0;
            }

            if (useXFiles)
            {
                if (_currentMapFiles[i] != _filesMapX[i])
                {
                    BlockData[i] = null;
                }
            }
            else
            {
                if (_currentMapFiles[i] != _filesMap[i])
                {
                    BlockData[i] = null;
                }
            }

            if (BlockData[i] != null)
            {
                return;
            }

            _currentMapFiles[i] = _filesMap[i];
            _currentStaticsFiles[i] = _filesStatics[i];
            _currentIdxStaticsFiles[i] = _filesIdxStatics[i];

            if (useXFiles)
            {
                if (_filesMapX[i] != null)
                    _currentMapFiles[i] = _filesMapX[i];

                if (_filesIdxStaticsX[i] != null)
                    _currentIdxStaticsFiles[i] = _filesIdxStaticsX[i];

                if (_filesStaticsX[i] != null)
                    _currentStaticsFiles[i] = _filesStaticsX[i];
            }

            if (_currentMapFiles[i] == null)
            {
                return;
            }

            MapBlocksSize[i, 0] = MapsDefaultSize[i, 0] >> 3;
            MapBlocksSize[i, 1] = MapsDefaultSize[i, 1] >> 3;

            var mapblocksize = sizeof(MapBlock);
            var staticidxblocksize = sizeof(StaidxBlock);
            var staticblocksize = sizeof(StaticsBlock);
            var width = MapBlocksSize[i, 0];
            var height = MapBlocksSize[i, 1];
            var maxblockcount = width * height;
            BlockData[i] = new IndexMap[maxblockcount];
            var file = _currentMapFiles[i];
            var fileidx = _currentIdxStaticsFiles[i];
            var staticfile = _currentStaticsFiles[i];

            if (fileidx == null && i == 1)
            {
                fileidx = _currentIdxStaticsFiles[0];
            }

            if (staticfile == null && i == 1)
            {
                staticfile = _currentStaticsFiles[0];
            }

            ulong uopoffset = 0;
            int fileNumber = -1;
            bool isuop = file is UOFileUop;

            for (int block = 0; block < maxblockcount; block++)
            {
                int blocknum = block;

                if (isuop)
                {
                    blocknum &= 4095;
                    int shifted = block >> 12;

                    if (fileNumber != shifted)
                    {
                        fileNumber = shifted;
                        var uop = file as UOFileUop;

                        if (shifted < uop.Entries.Length)
                        {
                            uopoffset = (ulong)uop.Entries[shifted].Offset;
                        }
                    }
                }

                var mapPos = uopoffset + (ulong)(blocknum * mapblocksize);
                var staticPos = 0ul;
                var staticCount = 0u;

                fileidx.Seek(block * staticidxblocksize, SeekOrigin.Begin);

                var st = fileidx.Read<StaidxBlock>();

                if (st.Size > 0 && st.Position != 0xFFFF_FFFF)
                {
                    staticPos = st.Position;
                    staticCount = Math.Min(1024, (uint)(st.Size / staticblocksize));
                }

                ref var data = ref BlockData[i][block];
                data.MapAddress = mapPos;
                data.StaticAddress = staticPos;
                data.StaticCount = staticCount;
                data.OriginalMapAddress = mapPos;
                data.OriginalStaticAddress = staticPos;
                data.OriginalStaticCount = staticCount;

                data.MapFile = file;
                data.StaticFile = staticfile;
            }

            if (isuop)
            {
                // TODO: UOLive needs hashes! we need to find out a better solution, but keep 'em for the moment
                //((UOFileUop)file)?.ClearHashes();
            }
        }

        public void PatchMapBlock(UOFile file, ulong block, ulong address)
        {
            int w = MapBlocksSize[0, 0];
            int h = MapBlocksSize[0, 1];

            int maxBlockCount = w * h;

            if (maxBlockCount < 1)
            {
                return;
            }

            BlockData[0][block].MapFile = file;
            BlockData[0][block].OriginalMapAddress = address;
            BlockData[0][block].MapAddress = address;
        }

        public unsafe void PatchStaticBlock(UOFile file, ulong block, ulong address, uint count)
        {
            int w = MapBlocksSize[0, 0];
            int h = MapBlocksSize[0, 1];

            int maxBlockCount = w * h;

            if (maxBlockCount < 1)
            {
                return;
            }

            BlockData[0][block].StaticFile = file;
            BlockData[0][block].StaticAddress = BlockData[0][block].OriginalStaticAddress = address;

            count = (uint) (count / (sizeof(StaidxBlockVerdata)));

            if (count > 1024)
            {
                count = 1024;
            }

            BlockData[0][block].StaticCount = BlockData[0][block].OriginalStaticCount = count;
        }

        public unsafe bool ApplyPatches(ref StackDataReader reader)
        {
            ResetPatchesInBlockTable();

            PatchesCount = (int) reader.ReadUInt32BE();

            if (PatchesCount < 0)
            {
                PatchesCount = 0;
            }

            if (PatchesCount > MAPS_COUNT)
            {
                PatchesCount = MAPS_COUNT;
            }

            Array.Clear(MapPatchCount, 0, MapPatchCount.Length);
            Array.Clear(StaticPatchCount, 0, StaticPatchCount.Length);

            bool result = false;

            for (int i = 0; i < PatchesCount; i++)
            {
                int idx = i;

                //SanitizeMapIndex(ref idx);

                if (_currentMapFiles[idx] == null || _currentMapFiles[idx].Length == 0)
                {
                    reader.Skip(8);

                    continue;
                }

                int mapPatchesCount = (int)reader.ReadUInt32BE();
                MapPatchCount[i] = mapPatchesCount;
                int staticPatchesCount = (int)reader.ReadUInt32BE();
                StaticPatchCount[i] = staticPatchesCount;

                int w = MapBlocksSize[i, 0];
                int h = MapBlocksSize[i, 1];

                int maxBlockCount = w * h;

                if (mapPatchesCount != 0)
                {
                    UOFileMul difl = _mapDifl[i];
                    UOFileMul dif = _mapDif[i];

                    if (difl == null || dif == null || difl.Length == 0 || dif.Length == 0)
                    {
                        continue;
                    }

                    mapPatchesCount = Math.Min(mapPatchesCount, (int)difl.Length >> 2);

                    difl.Seek(0, SeekOrigin.Begin);
                    dif.Seek(0, SeekOrigin.Begin);

                    for (int j = 0; j < mapPatchesCount; j++)
                    {
                        uint blockIndex = difl.ReadUInt32();

                        if (blockIndex < maxBlockCount)
                        {
                            BlockData[idx][blockIndex].MapFile = dif;
                            BlockData[idx][blockIndex].MapAddress = (ulong)dif.Position;

                            result = true;
                        }

                        dif.Seek(sizeof(MapBlock), SeekOrigin.Current);
                    }
                }

                if (staticPatchesCount != 0)
                {
                    UOFileMul difl = _staDifl[i];
                    UOFileMul difi = _staDifi[i];

                    if (difl == null || difi == null || _staDif[i] == null || difl.Length == 0 || difi.Length == 0 || _staDif[i].Length == 0)
                    {
                        continue;
                    }

                    staticPatchesCount = Math.Min(staticPatchesCount, (int)difl.Length >> 2);

                    difl.Seek(0, SeekOrigin.Begin);
                    difi.Seek(0, SeekOrigin.Begin);

                    int sizeOfStaicsBlock = sizeof(StaticsBlock);
                    int sizeOfStaidxBlock = sizeof(StaidxBlock);

                    for (int j = 0; j < staticPatchesCount; j++)
                    {
                        if (difl.Position >= difl.Length || difi.Position >= difi.Length)
                        {
                            break;
                        }

                        uint blockIndex = difl.ReadUInt32();
                        var st = difi.Read<StaidxBlock>();

                        if (blockIndex < maxBlockCount)
                        {
                            ulong realStaticAddress = 0;
                            uint realStaticCount = 0;

                            if (st.Size > 0 && st.Position != 0xFFFF_FFFF)
                            {
                                realStaticAddress = st.Position;
                                realStaticCount = Math.Min(1024, (uint)(st.Size / sizeOfStaicsBlock));
                            }

                            BlockData[idx][blockIndex].StaticFile = _staDif[i];
                            BlockData[idx][blockIndex].StaticAddress = realStaticAddress;
                            BlockData[idx][blockIndex].StaticCount = realStaticCount;

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
                IndexMap[] list = BlockData[i];

                if (list == null)
                {
                    continue;
                }

                int w = MapBlocksSize[i, 0];
                int h = MapBlocksSize[i, 1];

                int maxBlockCount = w * h;

                if (maxBlockCount < 1)
                {
                    return;
                }

                if (_currentMapFiles[i] is UOFileMul mul && mul.Length != 0)
                {
                    if (_currentIdxStaticsFiles[i] is UOFileMul stIdxMul && stIdxMul.Length != 0)
                    {
                        if (_currentStaticsFiles[i] is UOFileMul stMul && stMul.Length != 0)
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
            if (map == 1 && (_currentMapFiles[1] == null || _currentMapFiles[1].Length == 0 || _currentStaticsFiles[1] == null ||
                _currentStaticsFiles[1].Length == 0 || _currentIdxStaticsFiles[1] == null || _currentIdxStaticsFiles[1].Length == 0))
            {
                map = 0;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref IndexMap GetIndex(int map, int x, int y)
        {
            int block = x * MapBlocksSize[map, 1] + y;

            return ref BlockData[map][block];
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
    public ref struct StaidxBlockVerdata
    {
        public uint Position;
        public ushort Size;
        public byte Unknown;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MapCells
    {
        public ushort TileID;
        public sbyte Z;
    }

    [InlineArray(64)]
    public struct MapCellsArray
    {
        private MapCells _a0;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MapBlock
    {
        public uint Header;
        public unsafe MapCellsArray Cells;
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

    public struct IndexMap
    {
        public FileReader MapFile, StaticFile;
        public ulong MapAddress;
        public ulong OriginalMapAddress;
        public ulong OriginalStaticAddress;
        public uint OriginalStaticCount;
        public ulong StaticAddress;
        public uint StaticCount;

        public static IndexMap Invalid = new IndexMap() { MapAddress = ulong.MaxValue };

        public readonly bool IsValid() => MapAddress != ulong.MaxValue;
    }
}
