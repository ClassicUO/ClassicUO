// SPDX-License-Identifier: BSD-2-Clause

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
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace ClassicUO.Game
{
    public sealed class UltimaLive
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

                    int mapWidthInBlocks = Client.Game.UO.FileManager.Maps.MapBlocksSize[mapId, 0];
                    int mapHeightInBlocks = Client.Game.UO.FileManager.Maps.MapBlocksSize[mapId, 1];
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


                    if (block >= 0 && block < Client.Game.UO.FileManager.Maps.MapBlocksSize[mapId, 0] * Client.Game.UO.FileManager.Maps.MapBlocksSize[mapId, 1])
                    {
                        int index = block * 12;

                        if (totalLength <= 0)
                        {
                            //update index lookup AND static size on disk (first 4 bytes lookup, next 4 is statics size)
                            _UL._filesIdxStatics[mapId].WriteArray(index, [0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00]);

                            Log.Trace($"writing zero length statics to index at 0x{index:X8}");
                        }
                        else
                        {
                            var reader = _UL._filesIdxStatics[mapId];
                            reader.Seek(index, SeekOrigin.Begin);

                            uint lookup = reader.ReadUInt32();
                            uint existingStaticsLength = reader.ReadUInt32();

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

                            //update lookup AND index length on disk
                            Span<byte> idxData =
                            [
                                (byte) lookup,
                                (byte) (lookup >> 8),
                                (byte) (lookup >> 16),
                                (byte) (lookup >> 24),
                                (byte) totalLength,
                                (byte) (totalLength >> 8),
                                (byte) (totalLength >> 16),
                                (byte) (totalLength >> 24),
                            ];

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

                        _UL.MapSizeWrapSize[mapNumber, 0] = Math.Min((ushort) Client.Game.UO.FileManager.Maps.MapsDefaultSize[0, 0], p.ReadUInt16BE());

                        _UL.MapSizeWrapSize[mapNumber, 1] = Math.Min((ushort) Client.Game.UO.FileManager.Maps.MapsDefaultSize[0, 1], p.ReadUInt16BE());

                        _UL.MapSizeWrapSize[mapNumber, 2] = Math.Min(p.ReadUInt16BE(), _UL.MapSizeWrapSize[mapNumber, 0]);
                        _UL.MapSizeWrapSize[mapNumber, 3] = Math.Min(p.ReadUInt16BE(), _UL.MapSizeWrapSize[mapNumber, 1]);
                    }

                    //previously there were a minor amount of maps
                    if (_UL._ValidMaps.Count == 0 || validMaps.Count > _UL._ValidMaps.Count || !validMaps.TrueForAll(i => _UL._ValidMaps.Contains(i)))
                    {
                        _UL._ValidMaps = validMaps;
                        MapLoader.MAPS_COUNT = sbyte.MaxValue;
                        var mapLoader = new ULMapLoader(Client.Game.UO.FileManager, (uint)MapLoader.MAPS_COUNT);

                        //for (int i = 0; i < maps; i++)
                        for (int i = 0; i < validMaps.Count; i++)
                        {
                            mapLoader.CheckForShardMapFile(validMaps[i]);
                        }

                        mapLoader.Load();

                        _UL._ULMap = mapLoader;
                        _UL._filesMap = new ULFileMul[MapLoader.MAPS_COUNT];
                        _UL._filesIdxStatics = new ULFileMul[MapLoader.MAPS_COUNT];
                        _UL._filesStatics = new ULFileMul[MapLoader.MAPS_COUNT];
                        (FileReader[], FileReader[], FileReader[]) refs = mapLoader.GetFilesReference;

                        for (int i = 0; i < validMaps.Count; i++)
                        {
                            _UL._filesMap[validMaps[i]] = refs.Item1[validMaps[i]] as ULFileMul;
                            _UL._filesIdxStatics[validMaps[i]] = refs.Item2[validMaps[i]] as ULFileMul;
                            _UL._filesStatics[validMaps[i]] = refs.Item3[validMaps[i]] as ULFileMul;
                        }
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
            Span<byte> landData = stackalloc byte[LAND_BLOCK_LENGTH];

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

            ushort mapWidthInBlocks = (ushort) Client.Game.UO.FileManager.Maps.MapBlocksSize[mapId, 0];
            ushort mapHeightInBlocks = (ushort) Client.Game.UO.FileManager.Maps.MapBlocksSize[mapId, 1];

            if (block >= 0 && block < mapWidthInBlocks * mapHeightInBlocks)
            {
                _UL._filesMap[mapId].WriteArray(block * 196 + 4, landData);

                //instead of recalculating the CRC block 2 times, in case of terrain + statics update, we only set the actual block to ushort maxvalue, so it will be recalculated on next hash query
                _UL.MapCRCs[mapId][block] = ushort.MaxValue;
                int blockX = block / mapHeightInBlocks, blockY = block % mapHeightInBlocks;
                int minx = Math.Max(0, blockX - 1), miny = Math.Max(0, blockY - 1);
                blockX = Math.Min(mapWidthInBlocks, blockX + 1);
                blockY = Math.Min(mapHeightInBlocks, blockY + 1);

                var gameObjects = new List<GameObject>();

                for (; blockX >= minx; --blockX)
                {
                    for (int by = blockY; by >= miny; --by)
                    {
                        Chunk mapChunk = world.Map.GetChunk(blockX * mapHeightInBlocks + by);

                        if (mapChunk == null)
                        {
                            continue;
                        }

                        gameObjects.Clear();

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

                        foreach (GameObject obj in gameObjects)
                        {
                            mapChunk.AddGameObject(obj, obj.X % 8, obj.Y % 8);
                        }

                        foreach (var headObj in mapChunk.Tiles)
                        {
                            var next = headObj.TNext;
                            while (next != null)
                            {
                                next.AlphaHue = byte.MaxValue;
                                next = next.TNext;
                            }
                            headObj.AlphaHue = byte.MaxValue;
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

            var staidxReader = _UL._filesIdxStatics[mapId];
            staidxReader.Seek(block * 12, SeekOrigin.Begin);

            uint lookup = staidxReader.ReadUInt32();

            int byteCount = Math.Max(0, staidxReader.ReadInt32());

            byte[] blockData = new byte[LAND_BLOCK_LENGTH + byteCount];

            //we prevent the system from reading beyond the end of file, causing an exception, if the data isn't there, we don't read it and leave the array blank, simple...
            var mapReader = _UL._filesMap[mapId];
            mapReader.Seek(block * 196 + 4, SeekOrigin.Begin);

            var staticsReader = _UL._filesStatics[mapId];

            for (int x = 0; x < 192; x++)
            {
                if (mapReader.Position + 1 >= mapReader.Length)
                {
                    break;
                }

                blockData[x] = mapReader.ReadUInt8();
            }

            if (lookup != 0xFFFFFFFF && byteCount > 0)
            {
                if (lookup < staticsReader.Length)
                {
                    staticsReader.Seek(lookup, SeekOrigin.Begin);

                    for (int x = LAND_BLOCK_LENGTH; x < blockData.Length; x++)
                    {
                        if (staticsReader.Position + 1 >= staticsReader.Length)
                        {
                            break;
                        }

                        blockData[x] = staticsReader.ReadUInt8();
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

        private class ULFileMul : FileReader
        {
            private readonly BinaryReader _reader;
            private readonly BinaryWriter _writer;

            public ULFileMul(FileStream stream) : base(stream)
            {
                _reader = new BinaryReader(stream);
                _writer = new BinaryWriter(stream);
            }

            public override BinaryReader Reader => _reader;

            public void WriteArray(long position, ReadOnlySpan<byte> array)
            {
                _writer.Seek((int)position, SeekOrigin.Begin);
                _writer.Write(array);
                _writer.Flush();
            }

            public override void Dispose()
            {
                base.Dispose();
                Client.Game.UO.FileManager.Maps.Dispose();
            }
        }

        // private sealed class ULFileMul : UOFileMul
        // {
        //     private readonly BinaryWriter _writer;

        //     public ULFileMul(string file, bool isStaticMul) : base(file)
        //     {
        //         _writer = new BinaryWriter(File.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite));
        //     }

        //     public override void FillEntries()
        //     {
        //     }

        //     public override void Dispose()
        //     {
        //         Client.Game.UO.FileManager.Maps.Dispose();
        //     }

        //     public void WriteArray(long position, byte[] array)
        //     {
        //         _writer.Seek((int)position, SeekOrigin.Begin);
        //         _writer.Write(array, 0, array.Length);
        //         _writer.Flush();
        //     }
        // }

        public class ULMapLoader : MapLoader
        {
            private readonly CancellationTokenSource _feedCancel;
            private FileStream[] _filesStaticsStream;
            private readonly Task _writerTask;

            public ULMapLoader(UOFileManager fileManager, uint maps) : base(fileManager)
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

                    MapBlocksSize[i, 0] = MapsDefaultSize[i, 0] >> 3;
                    MapBlocksSize[i, 1] = MapsDefaultSize[i, 1] >> 3;
                }

                _currentMapFiles = new FileReader[maps];
                _currentIdxStaticsFiles = new FileReader[maps];
                _currentStaticsFiles = new FileReader[maps];

                _writer = new AsyncWriterTasked(this, _feedCancel);

                _writerTask = Task.Run(_writer.Loop); // new Thread(_writer.Loop) {Name = "UL_File_Writer", IsBackground = true};
            }

            public (FileReader[], FileReader[], FileReader[]) GetFilesReference =>
                (_currentMapFiles, _currentIdxStaticsFiles, _currentStaticsFiles);

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

            public override void Load()
            {
                if (Client.Game.UO.FileManager.Maps is ULMapLoader)
                {
                    return;
                }

                Client.Game.UO.FileManager.Maps?.Dispose();
                Client.Game.UO.FileManager.Maps = this;

                _UL._EOF = new uint[NumMaps];
                _filesStaticsStream = new FileStream[NumMaps];
                bool foundOneMap = false;

                for (int x = 0; x < _UL._ValidMaps.Count; x++)
                {
                    int i = _UL._ValidMaps[x];
                    string path = Path.Combine(_UL.ShardName, $"map{i}.mul");

                    if (File.Exists(path))
                    {
                        _filesMap[i] = new ULFileMul(File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite));
                        foundOneMap = true;
                    }

                    path = Path.Combine(_UL.ShardName, $"statics{i}.mul");

                    if (!File.Exists(path))
                    {
                        foundOneMap = false;

                        break;
                    }

                    _filesStatics[i] = new ULFileMul(File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite));

                    _filesStaticsStream[i] = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

                    _UL._EOF[i] = (uint)new FileInfo(path).Length;

                    path = Path.Combine(_UL.ShardName, $"staidx{i}.mul");

                    if (!File.Exists(path))
                    {
                        foundOneMap = false;

                        break;
                    }

                    _filesIdxStatics[i] = new ULFileMul(File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite));
                }

                if (!foundOneMap)
                {
                    throw new FileNotFoundException($"No maps, staidx or statics found on {_UL.ShardName}.");
                }

                _filesMap.CopyTo(_currentMapFiles, 0);
                _filesIdxStatics.CopyTo(_currentIdxStaticsFiles, 0);
                _filesStatics.CopyTo(_currentStaticsFiles, 0);

                for (int x = 0; x < _UL._ValidMaps.Count; x++)
                {
                    int i = _UL._ValidMaps[x];
                    MapBlocksSize[i, 0] = MapsDefaultSize[i, 0] >> 3;
                    MapBlocksSize[i, 1] = MapsDefaultSize[i, 1] >> 3;
                    //on ultimalive map always preload
                    LoadMap(i);
                }
            }

            public void CheckForShardMapFile(int mapId)
            {
                string oldMap = FileManager.GetUOFilePath($"map{mapId}.mul");
                string oldStaIdx = FileManager.GetUOFilePath($"staidx{mapId}.mul");
                string oldStatics = FileManager.GetUOFilePath($"statics{mapId}.mul");

                //create file names
                string mapPath = Path.Combine(_UL.ShardName, $"map{mapId}.mul");
                string staIdxPath = Path.Combine(_UL.ShardName, $"staidx{mapId}.mul");
                string staticsPath = Path.Combine(_UL.ShardName, $"statics{mapId}.mul");

                if (!File.Exists(mapPath))
                {
                    var mapFile = GetMapFile(mapId);

                    if (mapFile == null)
                    {
                        // if (!File.Exists(staticsPath) && File.Exists(oldStatics))
                        // {
                        //     CopyFile(oldStatics, staticsPath);
                        // }

                        // if (!File.Exists(staIdxPath) && File.Exists(oldStaIdx))
                        // {
                        //     CopyFile(oldStaIdx, staIdxPath);
                        // }

                        CreateNewPersistentMap(mapId, mapPath, staIdxPath, staticsPath);
                    }
                    else
                    {
                        if (mapFile is UOFileUop uop)
                        {
                            //Entries[mapId] = new UOFileIndex[uop.TotalEntriesCount];
                            //uop.FillEntries(ref Entries[mapId]);

                            //Log.Trace($"UltimaLive -> converting file:\t{mapPath} from {uop.FilePath}");

                            //using (FileStream stream = File.Create(mapPath))
                            //{
                            //    var reader = uop.GetReader();
                            //    for (int x = 0; x < Entries[mapId].Length; x++)
                            //    {
                            //        reader.Seek(Entries[mapId][x].Offset);

                            //        stream.Write(reader.ReadArray(Entries[mapId][x].Length), 0, Entries[mapId][x].Length);
                            //    }

                            //    stream.Flush();
                            //}
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

            private void CreateNewPersistentMap(int mapId, string mapPath, string staIdxPath, string staticsPath)
            {
                int mapWidthInBlocks = MapBlocksSize[MapBlocksSize.GetLength(0) > mapId ? mapId : 0, 0]; //horizontal

                int mapHeightInBlocks = MapBlocksSize[MapBlocksSize.GetLength(0) > mapId ? mapId : 0, 1]; //vertical

                int numberOfBytesInStrip = 196 * mapHeightInBlocks;
                byte[] pVerticalBlockStrip = new byte[numberOfBytesInStrip];

                // TODO: stackalloc
                // ReSharper disable once RedundantExplicitArraySize
                Span<byte> block = stackalloc byte[196]
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
                    block.CopyTo(pVerticalBlockStrip.AsSpan(196 * y, 196));
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
                block = stackalloc byte[12] { 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

                for (int y = 0; y < mapHeightInBlocks; y++)
                {
                    block.CopyTo(pVerticalBlockStrip.AsSpan(12 * y, 12));
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
                int mapblocksize = sizeof(MapBlock);
                int staticidxblocksize = sizeof(StaidxBlock);
                int staticblocksize = sizeof(StaticsBlock);
                var file = _currentMapFiles[map];
                var fileidx = _currentIdxStaticsFiles[map];
                var staticfile = _currentStaticsFiles[map];

                ulong uopoffset = 0;
                int fileNumber = -1;
                bool isUop = file is UOFileUop;
                int block = blockNumber;

                if (isUop)
                {
                    blockNumber &= 4095;
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

                var mapPos = uopoffset + (ulong)(blockNumber * mapblocksize);
                var staticIdxPos = (ulong)(block * staticidxblocksize);
                var staticPos = 0ul;
                var staticCount = 0u;

                fileidx.Seek(block * staticidxblocksize, SeekOrigin.Begin);
                var st = fileidx.Read<StaidxBlock>();

                if (st.Size > 0 && st.Position != 0xFFFF_FFFF)
                {
                    staticPos = st.Position;
                    staticCount = Math.Min(1024, (uint)(st.Size / staticblocksize));
                }

                ref IndexMap data = ref BlockData[map][block];
                data.MapAddress = mapPos;
                data.StaticAddress = staticPos;
                data.StaticCount = staticCount;
                data.OriginalMapAddress = mapPos;
                data.OriginalStaticAddress = staticPos;
                data.OriginalStaticCount = staticCount;
                data.MapFile = file;
                data.StaticFile = staticfile;
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