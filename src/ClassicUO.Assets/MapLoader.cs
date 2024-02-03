#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

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
        private static MapLoader _instance;
        private UOFileMul[] _mapDif;
        private UOFileMul[] _mapDifl;
        private UOFileMul[] _staDif;
        private UOFileMul[] _staDifi;
        private UOFileMul[] _staDifl;

        // cannot be a const, due to UOLive implementation
        public static int MAPS_COUNT = 6;

        protected MapLoader()
        {
        }

        public static MapLoader Instance
        {
            get => _instance ?? (_instance = new MapLoader());
            set
            {
                _instance?.Dispose();
                _instance = value;
            }
        }

        public static string MapsLayouts { get; set; }

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

        protected UOFileMul[] _filesIdxStatics;
        protected UOFile[] _filesMap;
        protected UOFileMul[] _filesStatics;

        protected static UOFile GetMapFile(int map)
        {
            return map < Instance._filesMap.Length ? Instance._filesMap[map] : null;
        }

        protected void Initialize()
        {
            _filesMap = new UOFile[MAPS_COUNT];
            _filesStatics = new UOFileMul[MAPS_COUNT];
            _filesIdxStatics = new UOFileMul[MAPS_COUNT];

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

        public override unsafe Task Load()
        {
            return Task.Run
            (
                () =>
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
                        string path = UOFileManager.GetUOFilePath($"map{i}LegacyMUL.uop");

                        if (UOFileManager.IsUOPInstallation && File.Exists(path))
                        {
                            _filesMap[i] = new UOFileUop(path, $"build/map{i}legacymul/{{0:D8}}.dat");
                            foundOneMap = true;
                        }
                        else
                        {
                            path = UOFileManager.GetUOFilePath($"map{i}.mul");

                            if (File.Exists(path))
                            {
                                _filesMap[i] = new UOFileMul(path);

                                foundOneMap = true;
                            }

                            path = UOFileManager.GetUOFilePath($"mapdifl{i}.mul");

                            if (File.Exists(path))
                            {
                                _mapDifl[i] = new UOFileMul(path);
                                _mapDif[i] = new UOFileMul(UOFileManager.GetUOFilePath($"mapdif{i}.mul"));
                                _staDifl[i] = new UOFileMul(UOFileManager.GetUOFilePath($"stadifl{i}.mul"));
                                _staDifi[i] = new UOFileMul(UOFileManager.GetUOFilePath($"stadifi{i}.mul"));
                                _staDif[i] = new UOFileMul(UOFileManager.GetUOFilePath($"stadif{i}.mul"));
                            }
                        }

                        path = UOFileManager.GetUOFilePath($"statics{i}.mul");

                        if (File.Exists(path))
                        {
                            _filesStatics[i] = new UOFileMul(path);
                        }

                        path = UOFileManager.GetUOFilePath($"staidx{i}.mul");

                        if (File.Exists(path))
                        {
                            _filesIdxStatics[i] = new UOFileMul(path);
                        }
                    }

                    if (!foundOneMap)
                    {
                        throw new FileNotFoundException("No maps found.");
                    }


                    int mapblocksize = sizeof(MapBlock);

                    if (_filesMap[0].Length / mapblocksize == 393216 || UOFileManager.Version < ClientVersion.CV_4011D)
                    {
                        MapsDefaultSize[0, 0] = MapsDefaultSize[1, 0] = 6144;
                    }

                    // This is an hack to patch correctly all maps when you have to fake map1
                    if (_filesMap[1] == null || _filesMap[1].StartAddress == IntPtr.Zero)
                    {
                        _filesMap[1] = _filesMap[0];
                        _filesStatics[1] = _filesStatics[0];
                        _filesIdxStatics[1] = _filesIdxStatics[0];
                    }

                    var res = Parallel.For(0, MAPS_COUNT, i =>
                    {
                        MapBlocksSize[i, 0] = MapsDefaultSize[i, 0] >> 3;
                        MapBlocksSize[i, 1] = MapsDefaultSize[i, 1] >> 3;
                        LoadMap(i);
                    });          
                }
            );
        }

        public unsafe void LoadMap(int i)
        {
            if (i < 0 || i + 1 > MAPS_COUNT || _filesMap[i] == null)
            {
                i = 0;
            }

            if (BlockData[i] != null || _filesMap[i] == null)
            {
                return;
            }

            int mapblocksize = sizeof(MapBlock);
            int staticidxblocksize = sizeof(StaidxBlock);
            int staticblocksize = sizeof(StaticsBlock);
            int width = MapBlocksSize[i, 0];
            int height = MapBlocksSize[i, 1];
            int maxblockcount = width * height;
            BlockData[i] = new IndexMap[maxblockcount];
            UOFile file = _filesMap[i];
            UOFile fileidx = _filesIdxStatics[i];
            UOFile staticfile = _filesStatics[i];

            if (fileidx == null && i == 1)
            {
                fileidx = _filesIdxStatics[0];
            }

            if (staticfile == null && i == 1)
            {
                staticfile = _filesStatics[0];
            }

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
                        var uop = file as UOFileUop;

                        if (shifted < uop.TotalEntriesCount)
                        {
                            var hash = UOFileUop.CreateHash(string.Format(uop.Pattern, shifted));

                            if (uop.TryGetUOPData(hash, out var dataIndex))
                            {
                                uopoffset = (ulong)dataIndex.Offset;
                            }
                        }
                    }
                }

                ulong address = mapddress + uopoffset + (ulong) (blocknum * mapblocksize);

                if (address < endmapaddress)
                {
                    realmapaddress = address;
                }

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
                        {
                            realstaticcount = 1024;
                        }
                    }
                }

                ref IndexMap data = ref BlockData[i][block];
                data.MapAddress = realmapaddress;
                data.StaticAddress = realstaticaddress;
                data.StaticCount = realstaticcount;
                data.OriginalMapAddress = realmapaddress;
                data.OriginalStaticAddress = realstaticaddress;
                data.OriginalStaticCount = realstaticcount;
            }

            if (isuop)
            {
                // TODO: UOLive needs hashes! we need to find out a better solution, but keep 'em for the moment
                //((UOFileUop)file)?.ClearHashes();
            }
        }

        public void PatchMapBlock(ulong block, ulong address)
        {
            int w = MapBlocksSize[0, 0];
            int h = MapBlocksSize[0, 1];

            int maxBlockCount = w * h;

            if (maxBlockCount < 1)
            {
                return;
            }

            BlockData[0][block].OriginalMapAddress = address;
            BlockData[0][block].MapAddress = address;
        }


        public unsafe void PatchStaticBlock(ulong block, ulong address, uint count)
        {
            int w = MapBlocksSize[0, 0];
            int h = MapBlocksSize[0, 1];

            int maxBlockCount = w * h;

            if (maxBlockCount < 1)
            {
                return;
            }

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

                if (_filesMap[idx] == null || _filesMap[idx].StartAddress == IntPtr.Zero)
                {
                    reader.Skip(8);

                    continue;
                }

                int mapPatchesCount = (int) reader.ReadUInt32BE();
                MapPatchCount[i] = mapPatchesCount;
                int staticPatchesCount = (int) reader.ReadUInt32BE();
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

                    mapPatchesCount = Math.Min(mapPatchesCount, (int) difl.Length >> 2);

                    difl.Seek(0);
                    dif.Seek(0);

                    for (int j = 0; j < mapPatchesCount; j++)
                    {
                        uint blockIndex = difl.ReadUInt();

                        if (blockIndex < maxBlockCount)
                        {
                            BlockData[idx][blockIndex].MapAddress = (ulong) dif.PositionAddress;

                            result = true;
                        }

                        dif.Skip(sizeof(MapBlock));
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

                    ulong startAddress = (ulong) _staDif[i].StartAddress;

                    staticPatchesCount = Math.Min(staticPatchesCount, (int) difl.Length >> 2);

                    difl.Seek(0);
                    difi.Seek(0);

                    int sizeOfStaicsBlock = sizeof(StaticsBlock);
                    int sizeOfStaidxBlock = sizeof(StaidxBlock);

                    for (int j = 0; j < staticPatchesCount; j++)
                    {
                        if (difl.IsEOF || difi.IsEOF)
                        {
                            break;
                        }

                        uint blockIndex = difl.ReadUInt();

                        StaidxBlock* sidx = (StaidxBlock*) difi.PositionAddress;

                        difi.Skip(sizeOfStaidxBlock);

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
                                    {
                                        realStaticCount = 1024;
                                    }
                                }
                            }

                            BlockData[idx][blockIndex].StaticAddress = realStaticAddress;

                            BlockData[idx][blockIndex].StaticCount = (uint) realStaticCount;

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
    public ref struct StaticsBlock
    {
        public ushort Color;
        public byte X;
        public byte Y;
        public sbyte Z;
        public ushort Hue;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public ref struct StaidxBlock
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
    public ref struct MapCells
    {
        public ushort TileID;
        public sbyte Z;
    }

    //[StructLayout(LayoutKind.Sequential, Pack = 1)]
    //public struct MapBlock
    //{
    //    public readonly uint Header;
    //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
    //    public MapCells[] Cells;
    //}

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 4 + 64 * 3)]
    public ref struct MapBlock
    {
        public uint Header;
        public unsafe MapCells* Cells;
    }

    //[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 4 + 64 * 3)]
    //public struct MapBlock2
    //{
    //    public readonly uint Header;
    //    public IntPtr Cells;
    //}

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
        public ulong MapAddress;
        public ulong OriginalMapAddress;
        public ulong OriginalStaticAddress;
        public uint OriginalStaticCount;
        public ulong StaticAddress;
        public uint StaticCount;
        public static IndexMap Invalid = new IndexMap();
    }
}