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

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Map;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;
using ClassicUO.Assets;
using ClassicUO.Network;
using ClassicUO.Utility.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Threading.Tasks;

namespace ClassicUO.Game
{
    public class UltimaLive
    {
        private const int STATICS_MEMORY_SIZE = 200000000;
        private const int CRC_LENGTH = 25;
        private const int LAND_BLOCK_LENGTH = 192;

        private static UltimaLive _UL;

        private static readonly char[] _pathSeparatorChars = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        private uint[] _EOF;
        private ULFileMul[] _filesIdxStatics;
        private ULFileMul[] _filesMap;
        private ULFileMul[] _filesStatics;
        private uint _SentWarning;
        private ULMapLoader _ULMap;
        private List<int> _ValidMaps = new List<int>();
        private ConcurrentQueue<(int, long, byte[])> _writequeue;
        private ushort[][] MapCRCs; //caching, to avoid excessive cpu & memory use
        //WrapMapSize includes 2 different kind of values at each side of the array:
        //left - mapId (zero based value), so first map is at ZERO
        //right- we have the size of the map, values in index 0 and 1 are map REAL size x and y
        //       values in index 2 and 3 is for the wrap size of map (virtual size), x and y
        private ushort[,] MapSizeWrapSize;
        public static bool UltimaLiveActive => _UL != null && !string.IsNullOrEmpty(_UL.ShardName);
        protected string RealShardName;
        protected string ShardName;

        public static void Enable()
        {
            Log.Trace("Setup packet for UltimaLive");
            PacketHandlers.Handler.Add(0x3F, OnUltimaLivePacket);
            PacketHandlers.Handler.Add(0x40, OnUpdateTerrainPacket);
        }

        //The UltimaLive packets could be also used for other things than maps and statics
        private static void OnUltimaLivePacket(World world, ref StackDataReader p)
        {
            p.Seek(13);
            byte command = p.ReadUInt8();

            switch (command)
            {
                case 0xFF: //hash query, for the blocks around us
                {
                    if (_UL == null || p.Length < 15)
                    {
                        return;
                    }

                    p.Seek(3);
                    int block = (int) p.ReadUInt32BE();
                    p.Seek(14);
                    int mapId = p.ReadUInt8();

                    if (mapId >= _UL._filesMap.Length)
                    {
                        if (Time.Ticks >= _UL._SentWarning)
                        {
                            Log.Trace($"The server is requesting access to MAP: {mapId} but we only have {_UL._filesMap.Length} maps!");

                            _UL._SentWarning = Time.Ticks + 100000;
                        }

                        return;
                    }

                    if (world.Map == null || mapId != world.Map.Index)
                    {
                        return;
                    }

                    int mapWidthInBlocks = MapLoader.Instance.MapBlocksSize[mapId, 0];
                    int mapHeightInBlocks = MapLoader.Instance.MapBlocksSize[mapId, 1];
                    int blocks = mapWidthInBlocks * mapHeightInBlocks;

                    if (block < 0 || block >= blocks)
                    {
                        return;
                    }

                    if (_UL.MapCRCs[mapId] == null)
                    {
                        _UL.MapCRCs[mapId] = new ushort[blocks];

                        for (int j = 0; j < blocks; j++)
                        {
                            _UL.MapCRCs[mapId][j] = ushort.MaxValue;
                        }
                    }

                    int blockX = block / mapHeightInBlocks;
                    int blockY = block % mapHeightInBlocks;

                    //this will avoid going OVER the wrapsize, so that we have the ILLUSION of never going over the main world
                    mapWidthInBlocks = blockX < _UL.MapSizeWrapSize[mapId, 2] >> 3 ? _UL.MapSizeWrapSize[mapId, 2] >> 3 : mapWidthInBlocks;

                    mapHeightInBlocks = blockY < _UL.MapSizeWrapSize[mapId, 3] >> 3 ? _UL.MapSizeWrapSize[mapId, 3] >> 3 : mapHeightInBlocks;

                    ushort[] checkSumsToBeSent = new ushort[CRC_LENGTH]; //byte 015 through 64   -  25 block CRCs

                    for (int x = -2; x <= 2; x++)
                    {
                        int xBlockItr = (blockX + x) % mapWidthInBlocks;

                        if (xBlockItr < 0 && xBlockItr > -3)
                        {
                            xBlockItr += mapWidthInBlocks;
                        }

                        for (int y = -2; y <= 2; y++)
                        {
                            int yBlockItr = (blockY + y) % mapHeightInBlocks;

                            if (yBlockItr < 0)
                            {
                                yBlockItr += mapHeightInBlocks;
                            }

                            uint blockNumber = (uint) (xBlockItr * mapHeightInBlocks + yBlockItr);

                            if (blockNumber < blocks)
                            {
                                ushort crc = _UL.MapCRCs[mapId][blockNumber];

                                if (crc == ushort.MaxValue)
                                {
                                    if (xBlockItr >= mapWidthInBlocks || yBlockItr >= mapHeightInBlocks)
                                    {
                                        crc = 0;
                                    }
                                    else
                                    {
                                        crc = GetBlockCrc(world, blockNumber);
                                    }

                                    _UL.MapCRCs[mapId][blockNumber] = crc;
                                }

                                checkSumsToBeSent[(x + 2) * 5 + y + 2] = crc;
                            }
                            else
                            {
                                checkSumsToBeSent[(x + 2) * 5 + y + 2] = 0;
                            }
                        }
                    }

                    NetClient.Socket.Send_UOLive_HashResponse((uint) block, (byte) mapId, checkSumsToBeSent.AsSpan(0, CRC_LENGTH));

                    break;
                }

                case 0x00: //statics update
                {
                    if (_UL == null || p.Length < 15)
                    {
                        return;
                    }

                    p.Seek(3);
                    int block = (int) p.ReadUInt32BE();
                    int length = (int) p.ReadUInt32BE();
                    int totalLength = length * 7;

                    if (p.Length < totalLength + 15)
                    {
                        return;
                    }

                    p.Seek(14);
                    int mapId = p.ReadUInt8();

                    if (mapId >= _UL._filesMap.Length)
                    {
                        if (Time.Ticks >= _UL._SentWarning)
                        {
                            Log.Trace($"The server is requesting access to MAP: {mapId} but we only have {_UL._filesMap.Length} maps!");

                            _UL._SentWarning = Time.Ticks + 100000;
                        }

                        return;
                    }

                    if (world.Map == null || mapId != world.Map.Index)
                    {
                        return;
                    }

                        // TODO(andrea): using a struct range instead of allocate the array to the heap?
                    byte[] staticsData = new byte[totalLength];
                    p.Buffer.Slice(p.Position, totalLength).CopyTo(staticsData);


                    if (block >= 0 && block < MapLoader.Instance.MapBlocksSize[mapId, 0] * MapLoader.Instance.MapBlocksSize[mapId, 1])
                    {
                        int index = block * 12;

                        if (totalLength <= 0)
                        {
                            //update index lookup AND static size on disk (first 4 bytes lookup, next 4 is statics size)
                            _UL._filesIdxStatics[mapId].WriteArray(index, new byte[8] { 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00 });

                            Log.Trace($"writing zero length statics to index at 0x{index:X8}");
                        }
                        else
                        {
                            _UL._filesIdxStatics[mapId].Seek(index);

                            uint lookup = _UL._filesIdxStatics[mapId].ReadUInt();

                            uint existingStaticsLength = _UL._filesIdxStatics[mapId].ReadUInt();

                            //Do we have enough room to write the statics into the existing location?
                            if (existingStaticsLength >= totalLength && lookup != 0xFFFFFFFF)
                            {
                                Log.Trace($"writing statics to existing file location at 0x{lookup:X8}, length:{totalLength}");
                            }
                            else
                            {
                                lookup = _UL._EOF[mapId];
                                _UL._EOF[mapId] += (uint) totalLength;
                                Log.Trace($"writing statics to end of file at 0x{lookup:X8}, length:{totalLength}");
                            }

                            _UL._filesStatics[mapId].WriteArray(lookup, staticsData);

                            _UL._writequeue.Enqueue((mapId, lookup, staticsData));

                            // TODO: stackalloc
                            //update lookup AND index length on disk
                            byte[] idxData = new byte[8];
                            idxData[0] = (byte) lookup;
                            idxData[1] = (byte) (lookup >> 8);
                            idxData[2] = (byte) (lookup >> 16);
                            idxData[3] = (byte) (lookup >> 24);
                            idxData[4] = (byte) totalLength;
                            idxData[5] = (byte) (totalLength >> 8);
                            idxData[6] = (byte) (totalLength >> 16);
                            idxData[7] = (byte) (totalLength >> 24);

                            //update lookup AND index length on disk
                            _UL._filesIdxStatics[mapId].WriteArray(block * 12, idxData);

                            Chunk mapChunk = world.Map.GetChunk(block);

                            if (mapChunk == null)
                            {
                                return;
                            }

                            LinkedList<int> linkedList = mapChunk.Node?.List;
                            List<GameObject> gameObjects = new List<GameObject>();

                            for (int x = 0; x < 8; x++)
                            {
                                for (int y = 0; y < 8; y++)
                                {
                                    GameObject gameObject = mapChunk.GetHeadObject(x, y);

                                    while (gameObject != null)
                                    {
                                        GameObject currentGameObject = gameObject;
                                        gameObject = gameObject.TNext;

                                        if (!(currentGameObject is Land) && !(currentGameObject is Static))
                                        {
                                            gameObjects.Add(currentGameObject);
                                            currentGameObject.RemoveFromTile();
                                        }
                                    }
                                }
                            }

                            mapChunk.Clear();
                            _UL._ULMap.ReloadBlock(mapId, block);
                            mapChunk.Load(mapId);

                            //linkedList?.AddLast(c.Node);

                            foreach (GameObject gameObject in gameObjects)
                            {
                                mapChunk.AddGameObject(gameObject, gameObject.X % 8, gameObject.Y % 8);
                            }
                        }


                        UIManager.GetGump<MiniMapGump>()?.RequestUpdateContents();

                        //UIManager.GetGump<WorldMapGump>()?.UpdateMap();
                        //instead of recalculating the CRC block 2 times, in case of terrain + statics update, we only set the actual block to ushort maxvalue, so it will be recalculated on next hash query
                        //also the server should always send FIRST the landdata packet, and only AFTER land the statics packet
                        _UL.MapCRCs[mapId][block] = ushort.MaxValue;
                    }

                    break;
                }

                case 0x01: //map definition update
                {
                    if (_UL == null)
                    {
                        return;
                    }

                    if (string.IsNullOrEmpty(_UL.ShardName) || p.Length < 15)
                    {
                        //we cannot validate the pathfolder or packet is not correct
                        return;
                    }

                    if (!Directory.Exists(_UL.ShardName))
                    {
                        Directory.CreateDirectory(_UL.ShardName);

                        if (!Directory.Exists(_UL.ShardName))
                        {
                            _UL = null;

                            return;
                        }
                    }

                    p.Seek(7);
                    uint maps = p.ReadUInt32BE() * 7 / 9;

                    if (p.Length < maps * 9 + 15) //the packet has padding inside, so it's usually larger or equal than what we expect
                    {
                        return;
                    }

                    /*if (_UL.MapCRCs != null)
                        oldlen = _UL.MapCRCs.Length;
                    if (_UL.MapCRCs == null || _UL.MapCRCs.Length < maps)*/
                    _UL.MapCRCs = new ushort[sbyte.MaxValue][];

                    _UL.MapSizeWrapSize = new ushort[sbyte.MaxValue, 4]; //we always need to reinitialize this, as it could change from login to login even on the same server, in case of map changes (a change could happen on the fly with a client kick or on reboot)

                    p.Seek(15); //byte 15 to end of packet, the map definitions
                    List<int> validMaps = new List<int>();

                    for (int i = 0; i < maps; i++)
                    {
                        int mapNumber = p.ReadUInt8();
                        validMaps.Add(mapNumber);

                        _UL.MapSizeWrapSize[mapNumber, 0] = Math.Min((ushort) MapLoader.Instance.MapsDefaultSize[0, 0], p.ReadUInt16BE());

                        _UL.MapSizeWrapSize[mapNumber, 1] = Math.Min((ushort) MapLoader.Instance.MapsDefaultSize[0, 1], p.ReadUInt16BE());

                        _UL.MapSizeWrapSize[mapNumber, 2] = Math.Min(p.ReadUInt16BE(), _UL.MapSizeWrapSize[mapNumber, 0]);
                        _UL.MapSizeWrapSize[mapNumber, 3] = Math.Min(p.ReadUInt16BE(), _UL.MapSizeWrapSize[mapNumber, 1]);
                    }

                    //previously there were a minor amount of maps
                    if (_UL._ValidMaps.Count == 0 || validMaps.Count > _UL._ValidMaps.Count || !validMaps.TrueForAll(i => _UL._ValidMaps.Contains(i)))
                    {
                        _UL._ValidMaps = validMaps;
                        MapLoader.MAPS_COUNT = sbyte.MaxValue;
                        ULMapLoader mapLoader = new ULMapLoader((uint)MapLoader.MAPS_COUNT);

                        //for (int i = 0; i < maps; i++)
                        for (int i = 0; i < validMaps.Count; i++)
                        {
                            mapLoader.CheckForShardMapFile(validMaps[i]);
                        }

                        mapLoader.Load().Wait();

                        _UL._ULMap = mapLoader;
                        _UL._filesMap = new ULFileMul[MapLoader.MAPS_COUNT];
                        _UL._filesIdxStatics = new ULFileMul[MapLoader.MAPS_COUNT];
                        _UL._filesStatics = new ULFileMul[MapLoader.MAPS_COUNT];
                        (UOFile[], UOFileMul[], UOFileMul[]) refs = mapLoader.GetFilesReference;

                        for (int i = 0; i < validMaps.Count; i++)
                        {
                            _UL._filesMap[validMaps[i]] = refs.Item1[validMaps[i]] as ULFileMul;
                            _UL._filesIdxStatics[validMaps[i]] = refs.Item2[validMaps[i]] as ULFileMul;
                            _UL._filesStatics[validMaps[i]] = refs.Item3[validMaps[i]] as ULFileMul;
                        }

                        _UL._writequeue = mapLoader._writer._toWrite;
                    }

                    break;
                }

                case 0x02: //Live login confirmation
                {
                    if (p.Length < 43) //fixed size
                    {
                        return;
                    }

                    //from byte 0x03 to 0x14 data is unused
                    p.Seek(15);
                    string name = ValidatePath(p.ReadASCII());

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        _UL = null;

                        return;
                    }

                    if (_UL != null && _UL.ShardName == name)
                    {
                        return;
                    }

                    string[] split = name.Split(_pathSeparatorChars, StringSplitOptions.RemoveEmptyEntries);

                    _UL = new UltimaLive
                    {
                        ShardName = name,
                        RealShardName = split[split.Length - 1]
                    };

                    //TODO: create shard directory, copy map and statics to that directory, use that files instead of the original ones
                    break;
                }

                /*case 0x03://Refresh client VIEW - after an update the server will usually send this packet to refresh the client view, this packet has been discontinued after ultimalive 0.96 and isn't necessary anymore
                    {
                        break;
                    }*/
            }
        }

        private static void OnUpdateTerrainPacket(World world, ref StackDataReader p)
        {
            int block = (int) p.ReadUInt32BE();
            // TODO: stackalloc
            byte[] landData = new byte[LAND_BLOCK_LENGTH];

            for (int i = 0; i < LAND_BLOCK_LENGTH; i++)
            {
                landData[i] = p.ReadUInt8();
            }

            p.Seek(200);
            byte mapId = p.ReadUInt8();

            if (world.Map == null || mapId != world.Map.Index)
            {
                return;
            }

            ushort mapWidthInBlocks = (ushort) MapLoader.Instance.MapBlocksSize[mapId, 0];
            ushort mapHeightInBlocks = (ushort) MapLoader.Instance.MapBlocksSize[mapId, 1];

            if (block >= 0 && block < mapWidthInBlocks * mapHeightInBlocks)
            {
                _UL._filesMap[mapId].WriteArray(block * 196 + 4, landData);

                //instead of recalculating the CRC block 2 times, in case of terrain + statics update, we only set the actual block to ushort maxvalue, so it will be recalculated on next hash query
                _UL.MapCRCs[mapId][block] = ushort.MaxValue;
                int blockX = block / mapHeightInBlocks, blockY = block % mapHeightInBlocks;
                int minx = Math.Max(0, blockX - 1), miny = Math.Max(0, blockY - 1);
                blockX = Math.Min(mapWidthInBlocks, blockX + 1);
                blockY = Math.Min(mapHeightInBlocks, blockY + 1);

                for (; blockX >= minx; --blockX)
                {
                    for (int by = blockY; by >= miny; --by)
                    {
                        Chunk mapChunk = world.Map.GetChunk(blockX * mapHeightInBlocks + by);

                        if (mapChunk == null)
                        {
                            continue;
                        }

                        List<GameObject> gameObjects = new List<GameObject>();

                        for (int x = 0; x < 8; x++)
                        {
                            for (int y = 0; y < 8; y++)
                            {
                                GameObject gameObject = mapChunk.GetHeadObject(x, y);

                                while (gameObject != null)
                                {
                                    GameObject currentGameObject = gameObject;
                                    gameObject = gameObject.TNext;

                                    if (!(currentGameObject is Land) && !(currentGameObject is Static))
                                    {
                                        gameObjects.Add(currentGameObject);
                                        currentGameObject.RemoveFromTile();
                                    }
                                }
                            }
                        }

                        mapChunk.Clear();
                        mapChunk.Load(mapId);

                        //linkedList?.AddLast(c.Node);

                        foreach (GameObject obj in gameObjects)
                        {
                            mapChunk.AddGameObject(obj, obj.X % 8, obj.Y % 8);
                        }
                    }
                }

                UIManager.GetGump<MiniMapGump>()?.RequestUpdateContents();

                //UIManager.GetGump<WorldMapGump>()?.UpdateMap();
            }
        }

        private static ushort GetBlockCrc(World world, uint block)
        {
            int mapId = world.Map.Index;

            _UL._filesIdxStatics[mapId].Seek(block * 12);

            uint lookup = _UL._filesIdxStatics[mapId].ReadUInt();

            int byteCount = Math.Max(0, _UL._filesIdxStatics[mapId].ReadInt());

            byte[] blockData = new byte[LAND_BLOCK_LENGTH + byteCount];

            //we prevent the system from reading beyond the end of file, causing an exception, if the data isn't there, we don't read it and leave the array blank, simple...
            _UL._filesMap[mapId].Seek(block * 196 + 4);

            for (int x = 0; x < 192; x++)
            {
                if (_UL._filesMap[mapId].Position + 1 >= _UL._filesMap[mapId].Length)
                {
                    break;
                }

                blockData[x] = _UL._filesMap[mapId].ReadByte();
            }

            if (lookup != 0xFFFFFFFF && byteCount > 0)
            {
                if (lookup < _UL._filesStatics[mapId].Length)
                {
                    _UL._filesStatics[mapId].Seek(lookup);

                    for (int x = LAND_BLOCK_LENGTH; x < blockData.Length; x++)
                    {
                        if (_UL._filesStatics[mapId].Position + 1 >= _UL._filesStatics[mapId].Length)
                        {
                            break;
                        }

                        blockData[x] = _UL._filesStatics[mapId].ReadByte();
                    }
                }
            }

            ushort crc = Fletcher16(blockData);

            return crc;
        }

        private static ushort Fletcher16(byte[] data)
        {
            ushort sum1 = 0;
            ushort sum2 = 0;
            int index;

            for (index = 0; index < data.Length; index++)
            {
                sum1 = (ushort) ((sum1 + data[index]) % 255);
                sum2 = (ushort) ((sum2 + sum1) % 255);
            }

            return (ushort) ((sum2 << 8) | sum1);
        }

        private static string ValidatePath(string shardName)
        {
            try
            {
                //we cannot allow directory separator inside our name
                if (!string.IsNullOrEmpty(shardName) && shardName.IndexOfAny(_pathSeparatorChars) == -1)
                {
                    string folderPath = Environment.GetFolderPath(CUOEnviroment.IsUnix ? Environment.SpecialFolder.LocalApplicationData : Environment.SpecialFolder.CommonApplicationData);

                    string fullPath = Path.GetFullPath(Path.Combine(folderPath, shardName));

                    if (!string.IsNullOrEmpty(fullPath))
                    {
                        return fullPath;
                    }
                }
            }
            catch
            {
            }

            //if invalid 'path', we get an exception, if uncorrectly formatted, we'll be here also, maybe wrong characters are sent?
            //since we are using only ascii (8bit) charset, send only normal letters! in this case we return null and invalidate ultimalive request
            return null;
        }
        
        private class ULFileMul : UOFileMul
        {
            public ULFileMul(string file, bool isStaticMul) : base(file)
            {
                LoadFile(isStaticMul);
            }

            protected override void Load() //loadentries here is for staticmul particular memory preloading
            {
            }

            private unsafe void LoadFile(bool isStaticMul)
            {
                FileInfo fileInfo = new FileInfo(FilePath);

                if (!fileInfo.Exists)
                {
                    throw new FileNotFoundException(fileInfo.FullName);
                }

                uint size = (uint) fileInfo.Length;
                Log.Trace($"UltimaLive -> ReLoading file:\t{FilePath}");

                if (size > 0 || isStaticMul) //if new map is generated automatically, staticX.mul size is equal to ZERO, other files should always be major than zero!
                {
                    MemoryMappedFile mmf;

                    if (isStaticMul)
                    {
                        try
                        {
#pragma warning disable CA1416 // This call site is reachable on all platforms. 'MemoryMappedFile.OpenExisting(string)' is only supported on: 'windows'.
                            mmf = MemoryMappedFile.OpenExisting(_UL.RealShardName + fileInfo.Name);
#pragma warning restore CA1416
                        }
                        catch
                        {
                            mmf = MemoryMappedFile.CreateNew(_UL.RealShardName + fileInfo.Name, STATICS_MEMORY_SIZE, MemoryMappedFileAccess.ReadWrite);

                            using (FileStream stream = File.Open(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            using (Stream s = mmf.CreateViewStream(0, stream.Length, MemoryMappedFileAccess.Write))
                            {
                                stream.CopyTo(s);
                            }
                        }

                        _file = mmf;
                    }
                    else
                    {
                        try
                        {
#pragma warning disable CA1416 // This call site is reachable on all platforms. 'MemoryMappedFile.OpenExisting(string)' is only supported on: 'windows'.
                            mmf = MemoryMappedFile.OpenExisting(_UL.RealShardName + fileInfo.Name);
#pragma warning restore CA1416

                        }
                        catch
                        {
                            mmf = MemoryMappedFile.CreateFromFile
                            (
                                File.Open(FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite),
                                _UL.RealShardName + fileInfo.Name,
                                size,
                                MemoryMappedFileAccess.ReadWrite,
                                HandleInheritability.None,
                                false
                            );
                        }

                        _file = mmf;
                    }

                    _accessor = _file.CreateViewAccessor(0, isStaticMul ? STATICS_MEMORY_SIZE : size, MemoryMappedFileAccess.ReadWrite);

                    byte* ptr = null;

                    try
                    {
                        _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
                        SetData(ptr, (long) _accessor.SafeMemoryMappedViewHandle.ByteLength);
                    }
                    catch
                    {
                        _file.Dispose();
                        _accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                        _accessor.Dispose();

                        throw new Exception("Something goes wrong...");
                    }
                }
                else
                {
                    throw new Exception($"{FilePath} size must be > 0");
                }
            }

            public override void Dispose()
            {
                MapLoader.Instance.Dispose();
            }

            public void WriteArray(long position, ArraySegment<byte> seg)
            {
                if (!_accessor.CanWrite || seg.Array == null)
                {
                    return;
                }

                _accessor.WriteArray(position, seg.Array, seg.Offset, seg.Count);
                _accessor.Flush();
            }

            public void WriteArray(long position, byte[] array)
            {
                if (!_accessor.CanWrite)
                {
                    return;
                }

                _accessor.WriteArray(position, array, 0, array.Length);
                _accessor.Flush();
            }
        }

        public class ULMapLoader : MapLoader
        {
            private readonly CancellationTokenSource _feedCancel;
            private FileStream[] _filesStaticsStream;
            private readonly Task _writerTask;

            private new UOFileIndex[][] Entries;

            public ULMapLoader(uint maps)
            {
                Initialize();

                _feedCancel = new CancellationTokenSource();
                NumMaps = maps;
                var old = _UL.MapSizeWrapSize;
                MapsDefaultSize = new int[NumMaps, 2];

                for (int i = 0; i < NumMaps; i++)
                {
                    for (int x = 0; x < 2; x++)
                    {
                        MapsDefaultSize[i, x] = i < old.GetLength(0) ? old[i, x] : old[0, x];
                    }
                }

                _writer = new AsyncWriterTasked(this, _feedCancel);

                _writerTask = Task.Run(_writer.Loop); // new Thread(_writer.Loop) {Name = "UL_File_Writer", IsBackground = true};
            }

            public (UOFile[], UOFileMul[], UOFileMul[]) GetFilesReference =>
                (_filesMap, _filesIdxStatics, _filesStatics);

            private uint NumMaps { get; }
            public readonly AsyncWriterTasked _writer;

            public override void ClearResources()
            {
                try
                {
                    _feedCancel?.Cancel();
                    _writerTask?.Wait();

                    _feedCancel?.Dispose();
                    _writerTask?.Dispose();
                }
                catch
                {
                }

                if (_filesStaticsStream != null)
                {
                    for (int i = _filesStaticsStream.Length - 1; i >= 0; --i)
                    {
                        _filesStaticsStream[i]?.Dispose();
                    }

                    _filesStaticsStream = null;
                }
            }

            public override Task Load()
            {
                return Task.Run
                (
                    () =>
                    {
                        if (Instance is ULMapLoader)
                        {
                            return;
                        }

                        UOFileManager.MapLoaderReLoad(this);
                        _UL._EOF = new uint[NumMaps];
                        _filesStaticsStream = new FileStream[NumMaps];
                        bool foundOneMap = false;

                        for (int x = 0; x < _UL._ValidMaps.Count; x++)
                        {
                            int i = _UL._ValidMaps[x];
                            string path = Path.Combine(_UL.ShardName, $"map{i}.mul");

                            if (File.Exists(path))
                            {
                                _filesMap[i] = new ULFileMul(path, false);
                                foundOneMap = true;
                            }

                            path = Path.Combine(_UL.ShardName, $"statics{i}.mul");

                            if (!File.Exists(path))
                            {
                                foundOneMap = false;

                                break;
                            }

                            _filesStatics[i] = new ULFileMul(path, true);

                            _filesStaticsStream[i] = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

                            _UL._EOF[i] = (uint) new FileInfo(path).Length;

                            path = Path.Combine(_UL.ShardName, $"staidx{i}.mul");

                            if (!File.Exists(path))
                            {
                                foundOneMap = false;

                                break;
                            }

                            _filesIdxStatics[i] = new ULFileMul(path, false);
                        }

                        if (!foundOneMap)
                        {
                            throw new FileNotFoundException($"No maps, staidx or statics found on {_UL.ShardName}.");
                        }

                        for (int x = 0; x < _UL._ValidMaps.Count; x++)
                        {
                            int i = _UL._ValidMaps[x];
                            MapBlocksSize[i, 0] = MapsDefaultSize[i, 0] >> 3;
                            MapBlocksSize[i, 1] = MapsDefaultSize[i, 1] >> 3;
                            //on ultimalive map always preload
                            LoadMap(i);
                        }
                    }
                );
            }

            public void CheckForShardMapFile(int mapId)
            {
                if (Entries == null)
                {
                    Entries = new UOFileIndex[MapLoader.MAPS_COUNT][];
                }

                string oldMap = UOFileManager.GetUOFilePath($"map{mapId}.mul");
                string oldStaIdx = UOFileManager.GetUOFilePath($"staidx{mapId}.mul");
                string oldStatics = UOFileManager.GetUOFilePath($"statics{mapId}.mul");

                //create file names
                string mapPath = Path.Combine(_UL.ShardName, $"map{mapId}.mul");
                string staIdxPath = Path.Combine(_UL.ShardName, $"staidx{mapId}.mul");
                string staticsPath = Path.Combine(_UL.ShardName, $"statics{mapId}.mul");

                if (!File.Exists(mapPath))
                {
                    UOFile mapFile = GetMapFile(mapId);

                    if (mapFile == null)
                    {
                        CreateNewPersistentMap(mapId, mapPath, staIdxPath, staticsPath);
                    }
                    else
                    {
                        if (mapFile is UOFileUop uop)
                        {
                            Entries[mapId] = new UOFileIndex[uop.TotalEntriesCount];
                            uop.FillEntries(ref Entries[mapId]);

                            Log.Trace($"UltimaLive -> converting file:\t{mapPath} from {uop.FilePath}");

                            using (FileStream stream = File.Create(mapPath))
                            {
                                for (int x = 0; x < Entries[mapId].Length; x++)
                                {
                                    uop.Seek(Entries[mapId][x].Offset);

                                    stream.Write(uop.ReadArray(Entries[mapId][x].Length), 0, Entries[mapId][x].Length);
                                }

                                stream.Flush();
                            }
                        }
                        else
                        {
                            CopyFile(oldMap, mapPath);
                        }
                    }
                }

                if (!File.Exists(staticsPath))
                {
                    CopyFile(oldStatics, staticsPath);
                }

                if (!File.Exists(staIdxPath))
                {
                    CopyFile(oldStaIdx, staIdxPath);
                }
            }

            private static void CreateNewPersistentMap(int mapId, string mapPath, string staIdxPath, string staticsPath)
            {
                int mapWidthInBlocks = Instance.MapBlocksSize[Instance.MapBlocksSize.GetLength(0) > mapId ? mapId : 0, 0]; //horizontal

                int mapHeightInBlocks = Instance.MapBlocksSize[Instance.MapBlocksSize.GetLength(0) > mapId ? mapId : 0, 1]; //vertical

                int numberOfBytesInStrip = 196 * mapHeightInBlocks;
                byte[] pVerticalBlockStrip = new byte[numberOfBytesInStrip];

                // TODO: stackalloc
                // ReSharper disable once RedundantExplicitArraySize
                byte[] block = new byte[196]
                {
                    0x00, 0x00, 0x00, 0x00, //header
                    0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44,
                    0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00,
                    0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44,
                    0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00,
                    0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44,
                    0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00,
                    0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44,
                    0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00,
                    0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44,
                    0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00,
                    0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44,
                    0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00,
                    0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44,
                    0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00,
                    0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44,
                    0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00
                };

                for (int y = 0; y < mapHeightInBlocks; y++)
                {
                    Array.Copy
                    (
                        block,
                        0,
                        pVerticalBlockStrip,
                        196 * y,
                        196
                    );
                }

                //create map new file
                using (FileStream stream = File.Create(mapPath))
                {
                    Log.Trace($"UltimaLive -> creating new blank map:\t{mapPath}");
                    Log.Trace($"Writing {mapWidthInBlocks} blocks by {mapHeightInBlocks} blocks");

                    for (int x = 0; x < mapWidthInBlocks; x++)
                    {
                        stream.Write(pVerticalBlockStrip, 0, numberOfBytesInStrip);
                    }

                    stream.Flush();
                }

                numberOfBytesInStrip = 12 * mapHeightInBlocks;
                pVerticalBlockStrip = new byte[numberOfBytesInStrip];

                // TODO: stackalloc
                // ReSharper disable once RedundantExplicitArraySize
                block = new byte[12] { 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

                for (int y = 0; y < mapHeightInBlocks; y++)
                {
                    Array.Copy
                    (
                        block,
                        0,
                        pVerticalBlockStrip,
                        12 * y,
                        12
                    );
                }

                //create map new file
                using (FileStream stream = File.Create(staIdxPath))
                {
                    Log.Trace("UltimaLive -> creating new index file");

                    for (int x = 0; x < mapWidthInBlocks; x++)
                    {
                        stream.Write(pVerticalBlockStrip, 0, numberOfBytesInStrip);
                    }

                    stream.Flush();
                }

                using (FileStream stream = File.Create(staticsPath))
                {
                    Log.Trace("UltimaLive -> creating empty static file");
                }
            }

            //TODO: pull out into a FileHelper
            private static void CopyFile(string fromFilePath, string toFilePath)
            {
                if (!File.Exists(toFilePath) || new FileInfo(toFilePath).Length == 0)
                {
                    Log.Trace($"UltimaLive -> copying file:\t{toFilePath} from {fromFilePath}");
                    File.Copy(fromFilePath, toFilePath, true);
                }
            }

            public unsafe void ReloadBlock(int map, int blockNumber)
            {
                int mapBlockSize = sizeof(MapBlock);
                int staticIdxBlockSize = sizeof(StaidxBlock);
                int staticblockSize = sizeof(StaticsBlock);
                UOFile file = _filesMap[map];
                UOFile fileIdx = _filesIdxStatics[map];
                UOFile staticFile = _filesStatics[map];
                ulong staticIdxAddress = (ulong) fileIdx.StartAddress;
                ulong endStaticIdxAddress = staticIdxAddress + (ulong) fileIdx.Length;
                ulong staticAddress = (ulong) staticFile.StartAddress;
                ulong endStaticAddress = staticAddress + (ulong) staticFile.Length;
                ulong mapAddress = (ulong) file.StartAddress;
                ulong endMapAddress = mapAddress + (ulong) file.Length;
                ulong uopOffset = 0;
                int fileNumber = -1;
                bool isUop = file is UOFileUop;
                ulong realMapAddress = 0;
                ulong realStaticAddress = 0;
                uint realStaticCount = 0;
                int block = blockNumber;

                if (isUop)
                {
                    blockNumber &= 4095;
                    int shifted = block >> 12;

                    if (fileNumber != shifted)
                    {
                        fileNumber = shifted;

                        if (shifted < Entries.Length)
                        {
                            uopOffset = (ulong) Entries[map][shifted].Offset;
                        }
                    }
                }

                ulong address = mapAddress + uopOffset + (ulong) (blockNumber * mapBlockSize);

                if (address < endMapAddress)
                {
                    realMapAddress = address;
                }

                ulong stidxaddress = staticIdxAddress + (ulong) (block * staticIdxBlockSize);
                StaidxBlock* bb = (StaidxBlock*) stidxaddress;

                if (stidxaddress < endStaticIdxAddress && bb->Size > 0 && bb->Position != 0xFFFFFFFF)
                {
                    ulong address1 = staticAddress + bb->Position;

                    if (address1 < endStaticAddress)
                    {
                        realStaticAddress = address1;
                        realStaticCount = (uint) (bb->Size / staticblockSize);

                        if (realStaticCount > 1024)
                        {
                            realStaticCount = 1024;
                        }
                    }
                }

                ref IndexMap data = ref BlockData[map][block];
                data.MapAddress = realMapAddress;
                data.StaticAddress = realStaticAddress;
                data.StaticCount = realStaticCount;
                data.OriginalMapAddress = realMapAddress;
                data.OriginalStaticAddress = realStaticAddress;
                data.OriginalStaticCount = realStaticCount;
            }

            public class AsyncWriterTasked
            {
                private readonly ULMapLoader _Map;
                private readonly CancellationTokenSource _token;
                private readonly AutoResetEvent m_Signal = new AutoResetEvent(false);

                public AsyncWriterTasked(ULMapLoader map, CancellationTokenSource token)
                {
                    _Map = map;
                    _token = token;
                }

                public readonly ConcurrentQueue<(int, long, byte[])> _toWrite = new ConcurrentQueue<(int, long, byte[])>();

                public void Loop()
                {
                    while (_UL != null && !_Map.IsDisposed && !_token.IsCancellationRequested)
                    {
                        while (_toWrite.TryDequeue(out (int, long, byte[]) deq))
                        {
                            WriteArray(deq.Item1, deq.Item2, deq.Item3);
                        }

                        m_Signal.WaitOne(10, false);
                    }
                }

                public void WriteArray(int map, long position, byte[] array)
                {
                    _Map._filesStaticsStream[map].Seek(position, SeekOrigin.Begin);

                    _Map._filesStaticsStream[map].Write(array, 0, array.Length);

                    _Map._filesStaticsStream[map].Flush();
                }
            }
        }
    }
}