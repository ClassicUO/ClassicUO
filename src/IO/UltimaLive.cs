﻿#region license
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
using System.Threading;
using System.IO.MemoryMappedFiles;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using ClassicUO.Game;
using ClassicUO.Game.Map;
using ClassicUO.Game.GameObjects;
using ClassicUO.Network;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using ClassicUO.IO.Resources;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.IO
{
    internal class UltimaLive
    {
        private const int STATICS_MEMORY_SIZE = 200000000;

        internal static void Enable()
        {
            Log.Message(LogTypes.Trace, "Setup packet for Ultima live");
            PacketHandlers.ToClient.Add(0x3F, OnUltimaLivePacket);
            PacketHandlers.ToClient.Add(0x40, OnUpdateTerrainPacket);
        }

        private static UltimaLive _UL;
        protected string ShardName = null;
        private const int CRCLength = 25;
        private const int LandBlockLenght = 192;
        private ULFileMul[] _filesMap;
        private ULFileMul[] _filesStatics;
        private ULFileMul[] _filesIdxStatics;
        private ConcurrentQueue<(int, long, byte[])> _writequeue;
        private ULMapLoader _ULMap;
        private UInt16[][] MapCRCs;//caching, to avoid excessive cpu & memory use
        private uint[] _EOF;
        //WrapMapSize includes 2 different kind of values at each side of the array:
        //left - mapID (zero based value), so first map is at ZERO
        //right- we have the size of the map, values in index 0 and 1 are map REAL size x and y
        //       values in index 2 and 3 is for the wrap size of map (virtual size), x and y
        private UInt16[,] MapSizeWrapSize;

        //The UltimaLive packets could be also used for other things than maps and statics
        private static void OnUltimaLivePacket(Packet p)
        {
            p.Seek(13);
            byte command = p.ReadByte();
            switch (command)
            {
                case 0xFF://hash query, for the blocks around us
                    {
                        if (_UL == null || p.Length < 15)
                        {
                            return;
                        }

                        p.Seek(3);
                        int block = (int)p.ReadUInt();
                        p.Seek(14);
                        int mapID = p.ReadByte();
                        if (World.Map == null || mapID != World.Map.Index)
                        {
                            return;
                        }

                        int mapWidthInBlocks = FileManager.Map.MapBlocksSize[mapID, 0];
                        int mapHeightInBlocks = FileManager.Map.MapBlocksSize[mapID, 1];
                        int blocks = mapWidthInBlocks * mapHeightInBlocks;
                        if (block < 0 || block >= blocks)
                        {
                            return;
                        }

                        if (_UL.MapCRCs[mapID] == null)
                        {
                            _UL.MapCRCs[mapID] = new UInt16[blocks];
                            for (int j = 0; j < blocks; j++)
                            {
                                _UL.MapCRCs[mapID][j] = UInt16.MaxValue;
                            }
                        }

                        int blockX = block / mapHeightInBlocks;
                        int blockY = block % mapHeightInBlocks;
                        //this will avoid going OVER the wrapsize, so that we have the ILLUSION of never going over the main world
                        mapWidthInBlocks = blockX < (_UL.MapSizeWrapSize[mapID, 2] >> 3) ? _UL.MapSizeWrapSize[mapID, 2] >> 3 : mapWidthInBlocks;
                        mapHeightInBlocks = blockY < (_UL.MapSizeWrapSize[mapID, 3] >> 3) ? _UL.MapSizeWrapSize[mapID, 3] >> 3 : mapHeightInBlocks;
                        ushort[] tosendCRCs = new ushort[CRCLength];     //byte 015 through 64   -  25 block CRCs
                        for (int x = -2; x <= 2; x++)
                        {
                            int xBlockItr = (blockX + x) % mapWidthInBlocks;
                            if (xBlockItr < 0 && xBlockItr > -3)
                            {
                                
                                {
                                    xBlockItr += mapWidthInBlocks;
                                }
                            }

                            for (int y = -2; y <= 2; y++)
                            {
                                int yBlockItr = (blockY + y) % mapHeightInBlocks;
                                if (yBlockItr < 0)
                                {
                                    yBlockItr += mapHeightInBlocks;
                                }

                                uint blocknum = (uint)((xBlockItr * mapHeightInBlocks) + yBlockItr);
                                if (blocknum < blocks)
                                {
                                    UInt16 crc = _UL.MapCRCs[mapID][blocknum];
                                    if (crc == UInt16.MaxValue)
                                    {
                                        if (xBlockItr >= mapWidthInBlocks || yBlockItr >= mapHeightInBlocks)
                                        {
                                            crc = 0;
                                        }
                                        else
                                        {

                                            crc = GetBlockCrc(blocknum, xBlockItr, yBlockItr);
                                        }
                                        _UL.MapCRCs[mapID][blocknum] = crc;
                                    }
                                    tosendCRCs[((x + 2) * 5) + (y + 2)] = crc;
                                }
                                else
                                    tosendCRCs[((x + 2) * 5) + (y + 2)] = 0;
                            }
                        }
                        NetClient.Socket.Send(new UltimaLiveHashResponse((uint)block, (byte)mapID, tosendCRCs));
                        break;
                    }
                case 0x00://statics update
                    {
                        if (_UL == null || p.Length < 15)
                        {
                            return;
                        }

                        p.Seek(3);
                        int block = (int)p.ReadUInt();
                        int length = (int)p.ReadUInt();
                        int totallen = length * 7;
                        if (p.Length < totallen + 15)
                        {
                            return;
                        }

                        p.Seek(14);
                        int mapID = p.ReadByte();
                        if (World.Map == null || mapID != World.Map.Index)
                        {
                            return;
                        }

                        byte[] staticsData = new byte[totallen];
                        for (int i = 0; i < totallen; i++)
                        {
                            staticsData[i] = p.ReadByte();
                        }
                        if (block >= 0 && block < (FileManager.Map.MapBlocksSize[mapID, 0] * FileManager.Map.MapBlocksSize[mapID, 1]))
                        {
                            Chunk chunk = World.Map.Chunks[block];
                            if (chunk != null)
                            {
                                for (int x = 0; x < 8; x++)
                                {
                                    for (int y = 0; y < 8; y++)
                                    {
                                        GameObject obj = chunk.Tiles[x, y].FirstNode;
                                        for(GameObject right = obj.Right; obj!=null; obj = right, right = right?.Right)
                                        {
                                            if (obj is Static || (obj is AnimatedItemEffect ef && ef.Source is Static))
                                                obj.Dispose();
                                        }
                                    }
                                }
                            }
                            int index = block * 12;
                            if (totallen <= 0)
                            {
                                //update index lookup AND static size on disk (first 4 bytes lookup, next 4 is statics size)
                                _UL._filesIdxStatics[mapID].WriteArray(index, new byte[8] { 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00 });
                                Log.Message(LogTypes.Trace, $"writing zero length statics to index at 0x{index:X8}");
                            }
                            else
                            {
                                _UL._filesIdxStatics[mapID].Seek(index);
                                uint lookup = _UL._filesIdxStatics[mapID].ReadUInt();
                                uint existingStaticsLength = _UL._filesIdxStatics[mapID].ReadUInt();

                                //Do we have enough room to write the statics into the existing location?
                                if (existingStaticsLength >= totallen && lookup != 0xFFFFFFFF)
                                {
                                    Log.Message(LogTypes.Trace, $"writing statics to existing file location at 0x{lookup:X8}, length:{totallen}");
                                }
                                else
                                {
                                    lookup = _UL._EOF[mapID];
                                    _UL._EOF[mapID] += (uint)totallen;
                                    Log.Message(LogTypes.Trace, $"writing statics to end of file at 0x{lookup:X8}, length:{totallen}");
                                }
                                
                                _UL._filesStatics[mapID].WriteArray(lookup, staticsData);
                                _UL._writequeue.Enqueue((mapID, lookup, staticsData));
                                //update lookup AND index length on disk
                                byte[] idxdata = new byte[8];
                                idxdata[0] = (byte)lookup;
                                idxdata[1] = (byte)(lookup >> 8);
                                idxdata[2] = (byte)(lookup >> 16);
                                idxdata[3] = (byte)(lookup >> 24);
                                idxdata[4] = (byte)totallen;
                                idxdata[5] = (byte)(totallen >> 8);
                                idxdata[6] = (byte)(totallen >> 16);
                                idxdata[7] = (byte)(totallen >> 24);
                                //update lookup AND index length on disk
                                _UL._filesIdxStatics[mapID].WriteArray(block * 12, idxdata);
                            }
                            _UL._ULMap.ReloadBlock(mapID, block);
                            chunk?.LoadStatics(mapID);
                            Engine.UI.GetByLocalSerial<MiniMapGump>()?.ForceUpdate();
                            //instead of recalculating the CRC block 2 times, in case of terrain + statics update, we only set the actual block to ushort maxvalue, so it will be recalculated on next hash query
                            //also the server should always send FIRST the landdata packet, and only AFTER land the statics packet
                            _UL.MapCRCs[mapID][block] = UInt16.MaxValue;
                        }
                        break;
                    }
                case 0x01://map definition update
                    {
                        if (_UL == null)
                            return;
                        if (string.IsNullOrEmpty(_UL.ShardName) || p.Length < 15)
                        {
                            //we cannot validate the pathfolder or packet is not correct
                            return;
                        }
                        else if(!Directory.Exists(_UL.ShardName))
                        {
                            Directory.CreateDirectory(_UL.ShardName);
                            if (!Directory.Exists(_UL.ShardName))
                            {
                                _UL = null;
                                return;
                            }
                        }
                        p.Seek(7);
                        uint maps = (p.ReadUInt() * 7) / 9;
                        if (p.Length < (maps * 9 + 15))//the packet has padding inside, so it's usually larger or equal than what we expect
                        {
                            return;
                        }
                        int oldlen = 0;
                        if (_UL.MapCRCs != null)
                            oldlen = _UL.MapCRCs.Length;
                        if (_UL.MapCRCs == null || _UL.MapCRCs.Length < maps)
                        {
                            _UL.MapCRCs = new UInt16[maps][];
                        }
                        _UL.MapSizeWrapSize = new UInt16[maps, 4];//we always need to reinitialize this, as it could change from login to login even on the same server, in case of map changes (a change could happen on the fly with a client kick or on reboot)
                        p.Seek(15);//byte 15 to end of packet, the map definitions
                        for (int i = 0; i < maps; i++)
                        {
                            int mapnum = p.ReadByte();
                            _UL.MapSizeWrapSize[mapnum,0] = Math.Min((ushort)FileManager.Map.MapsDefaultSize[0, 0], p.ReadUShort());
                            _UL.MapSizeWrapSize[mapnum,1] = Math.Min((ushort)FileManager.Map.MapsDefaultSize[0, 1], p.ReadUShort());
                            _UL.MapSizeWrapSize[mapnum,2] = Math.Min(p.ReadUShort(), _UL.MapSizeWrapSize[mapnum,0]);
                            _UL.MapSizeWrapSize[mapnum,3] = Math.Min(p.ReadUShort(), _UL.MapSizeWrapSize[mapnum,1]);
                        }
                        //previously there were a minor amount of maps
                        if (oldlen == 0 || maps > oldlen)
                        {
                            ULMapLoader loader = new ULMapLoader(maps);
                            for (int i = 0; i < maps; i++)
                            {
                                ULMapLoader.CheckForShardMapFile(i);
                            }
                            loader.Load();
                            _UL._ULMap = loader;
                            _UL._filesMap = new ULFileMul[maps];
                            _UL._filesIdxStatics = new ULFileMul[maps];
                            _UL._filesStatics = new ULFileMul[maps];
                            var refs = loader.GetFilesReference;
                            for (int i = 0; i < maps; i++)
                            {
                                _UL._filesMap[i] = refs.Item1[i] as ULFileMul;
                                _UL._filesIdxStatics[i] = refs.Item2[i] as ULFileMul;
                                _UL._filesStatics[i] = refs.Item3[i] as ULFileMul;
                            }
                            _UL._writequeue = loader._writer._toWrite;
                        }
                        break;
                    }
                case 0x02://Live login confirmation
                    {
                        if (p.Length < 43)//fixed size
                        {
                            return;
                        }

                        //from byte 0x03 to 0x14 data is unused
                        p.Seek(15);
                        string name = ValidatePath(p.ReadASCII());
                        if (string.IsNullOrWhiteSpace(name))
                            _UL = null;
                        if (_UL != null && _UL.ShardName == name)
                            return;
                        _UL = new UltimaLive
                        {
                            ShardName = name
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void OnUpdateTerrainPacket(Packet p)
        {
            int block = (int)p.ReadUInt();
            byte[] landData = new byte[LandBlockLenght];
            for (int i = 0; i < LandBlockLenght; i++)
            {
                landData[i] = p.ReadByte();
            }
            p.Seek(200);
            byte mapID = p.ReadByte();
            if (World.Map == null || mapID != World.Map.Index)
                return;
            int mapWidthInBlocks = FileManager.Map.MapBlocksSize[mapID, 0];
            int mapHeightInBlocks = FileManager.Map.MapBlocksSize[mapID, 1];
            if (block >= 0 && block < (mapWidthInBlocks * mapHeightInBlocks))
            {
                _UL._filesMap[mapID].WriteArray((block * 196) + 4, landData);
                //instead of recalculating the CRC block 2 times, in case of terrain + statics update, we only set the actual block to ushort maxvalue, so it will be recalculated on next hash query
                _UL.MapCRCs[mapID][block] = UInt16.MaxValue;
                Chunk[] chunks = new Chunk[9];
                int blockX = block / mapHeightInBlocks, blockY = block % mapHeightInBlocks;
                int minx = Math.Max(0, blockX - 1), miny = Math.Max(0, blockY - 1);
                blockX = Math.Min(mapWidthInBlocks, blockX + 1);
                blockY = Math.Min(mapHeightInBlocks, blockY + 1);
                int pos = 0;
                for (; blockX >= minx; --blockX)
                {
                    for (int y = blockY; y >= miny; --y)
                    {
                        block = blockX * mapHeightInBlocks + y;
                        chunks[pos++] = World.Map.Chunks[block];
                    }
                }
                for (--pos; pos>=0; --pos)
                {
                    Chunk c = chunks[pos];
                    if (c != null)
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            for (int j = 0; j < 8; j++)
                            {
                                for (GameObject obj = c.Tiles[i, j].FirstNode; obj != null; obj = obj.Right)
                                {
                                    if (obj is Land ln)
                                        ln.Dispose();
                                }
                            }
                        }
                        c.LoadLand(mapID);
                    }
                }
                Engine.UI.GetByLocalSerial<MiniMapGump>()?.ForceUpdate();
            }
        }

        internal static UInt16 GetBlockCrc(uint block, int xblock, int yblock)
        {
            int mapID = World.Map.Index;
            _UL._filesIdxStatics[mapID].Seek(block * 12);
            uint lookup = _UL._filesIdxStatics[mapID].ReadUInt();
            int bytecount = Math.Max(0, _UL._filesIdxStatics[mapID].ReadInt());
            byte[] blockData = new byte[LandBlockLenght + bytecount];
            //we prevent the system from reading beyond the end of file, causing an exception, if the data isn't there, we don't read it and leave the array blank, simple...
            _UL._filesMap[mapID].Seek((block * 196) + 4);
            for(int x = 0; x < 192; x++)
            {
                if (_UL._filesMap[mapID].Position + 1 >= _UL._filesMap[mapID].Length)
                    break;
                blockData[x] = _UL._filesMap[mapID].ReadByte();
            }

            if (lookup != 0xFFFFFFFF && bytecount > 0)
            {
                if(lookup < _UL._filesStatics[mapID].Length)
                {
                    _UL._filesStatics[mapID].Seek(lookup);
                    for(int x = LandBlockLenght; x < blockData.Length; x++)
                    {
                        if (_UL._filesStatics[mapID].Position + 1 >= _UL._filesStatics[mapID].Length)
                            break;
                        blockData[x] = _UL._filesStatics[mapID].ReadByte();
                    }
                }
            }
            ushort crc = Fletcher16(blockData);
            blockData = null;
            return crc;
        }

        internal static UInt16 Fletcher16(byte[] data)
        {
            UInt16 sum1 = 0;
            UInt16 sum2 = 0;
            int index;
            for (index = 0; index < data.Length; index++)
            {
                sum1 = (UInt16)((sum1 + data[index]) % 255);
                sum2 = (UInt16)((sum2 + sum1) % 255);
            }

            return (UInt16)((sum2 << 8) | sum1);
        }

        internal sealed class UltimaLiveHashResponse : PacketWriter
        {
            public UltimaLiveHashResponse(uint block, byte mapid, ushort[] crcs) : base(0x3F)
            {
                WriteUInt(block);
                Seek(13);
                WriteByte(0xFF);
                WriteByte(mapid);
                for (int i = 0; i < CRCLength; i++)
                    WriteUShort(crcs[i]);
            }
        }

        private static readonly char[] _pathSeparatorChars = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        private static string ValidatePath(string shardname)
        {
            try
            {
                string fullPath = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "UltimaLive", shardname));

                if (shardname.IndexOfAny(_pathSeparatorChars) == -1 && !string.IsNullOrEmpty(fullPath))//we cannot allow directory separator inside our name
                    return fullPath;
            }
            catch
            {
            }
            //if invalid 'path', we get an exception, if uncorrectly formatted, we'll be here also, maybe wrong characters are sent?
            //since we are using only ascii (8bit) charset, send only normal letters! in this case we return null and invalidate ultimalive request
            return null;
        }

        internal class ULFileMul : UOFileMul
        {
            public ULFileMul(string file, bool isstaticmul) : base(file, isstaticmul)
            {
            }

            protected override void Load(bool loadentries = false)//loadentries here is for staticmul particular memory preloading
            {
                LoadFile(loadentries);
            }

            private unsafe void LoadFile(bool isstaticmul)
            {
                FileInfo fileInfo = new FileInfo(FilePath);
                if (!fileInfo.Exists)
                    throw new FileNotFoundException(fileInfo.FullName);
                uint size = (uint)fileInfo.Length;
                Log.Message(LogTypes.Trace, $"UltimaLive -> ReLoading file:\t{FilePath}");
                if (size > 0 || isstaticmul)//if new map is generated automatically, staticX.mul size is equal to ZERO, other files should always be major than zero!
                {
                    if (isstaticmul)
                    {
                        using (FileStream stream = File.OpenRead(fileInfo.FullName))
                        {
                            _file = MemoryMappedFile.CreateNew(null, STATICS_MEMORY_SIZE, MemoryMappedFileAccess.ReadWrite);
                            using (Stream s = _file.CreateViewStream(0, stream.Length, MemoryMappedFileAccess.Write))
                                stream.CopyTo(s);
                        }
                    }
                    else
                        _file = MemoryMappedFile.CreateFromFile(File.Open(FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite), null, size, MemoryMappedFileAccess.ReadWrite, null, HandleInheritability.None, false);

                    _accessor = _file.CreateViewAccessor(0, isstaticmul ? STATICS_MEMORY_SIZE : size, MemoryMappedFileAccess.ReadWrite);
                    byte* ptr = null;
                    try
                    {
                        _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
                        SetData(ptr, (long)_accessor.SafeMemoryMappedViewHandle.ByteLength);
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
                    throw new Exception($"{FilePath} size must be > 0");
            }

            public override void Dispose()
            {
                FileManager.Map.Dispose();
            }

            internal void WriteArray(long position, byte[] array)
            {
                if (!_accessor.CanWrite)
                    return;
                _accessor.WriteArray(position, array, 0, array.Length);
                _accessor.Flush();
            }
        }

        internal class ULMapLoader : MapLoader
        {
            private Thread _twriter;
            internal AsyncWriterThread _writer;
            private FileStream[] _filesStaticsStream;
            internal (UOFile[], UOFileMul[], UOFileMul[]) GetFilesReference => (_filesMap, _filesIdxStatics, _filesStatics);
            internal uint NumMaps { get; private set; }
            public ULMapLoader(uint maps)
            {
                NumMaps = maps;
                int[,] old = MapsDefaultSize;
                MapsDefaultSize = new int[NumMaps, 2];
                MapBlocksSize = new int[NumMaps, 2];
                BlockData = new IndexMap[NumMaps][];
                for (int i = 0; i < NumMaps; i++)
                {
                    for (int x = 0; x < 2; x++)
                        MapsDefaultSize[i, x] = i < old.Length ? old[i, x] : old[0, x];
                }
                _writer = new AsyncWriterThread(this);
                _twriter = new Thread(new ThreadStart(_writer.Loop)) {Name = "UL_File_Writer", IsBackground = true };
                _twriter.Start();
            }

            protected override void CleanResources()
            {
                if (_filesStaticsStream != null)
                {
                    for (int i = _filesStaticsStream.Length - 1; i >= 0; --i)
                    {
                        _filesStaticsStream[i]?.Dispose();
                    }
                    _filesStaticsStream = null;
                }
                try { _twriter?.Abort(); }catch{ }
            }
            
            internal class AsyncWriterThread
            {
                private ULMapLoader _Map;
                internal ConcurrentQueue<(int, long, byte[])> _toWrite = new ConcurrentQueue<(int, long, byte[])>();
                private AutoResetEvent m_Signal = new AutoResetEvent(false);
                public AsyncWriterThread(ULMapLoader map)
                {
                    _Map = map;
                }

                public void Loop()
                {
                    while (_UL != null && !_Map.IsDisposed)
                    {
                        while (_toWrite.TryDequeue(out (int, long, byte[]) deq))
                            WriteArray(deq.Item1, deq.Item2, deq.Item3);
                        m_Signal.WaitOne(25, false);
                    }
                }

                internal void WriteArray(int map, long position, byte[] array)
                {
                    _Map._filesStaticsStream[map].Seek(position, SeekOrigin.Begin);
                    _Map._filesStaticsStream[map].Write(array, 0, array.Length);
                    _Map._filesStaticsStream[map].Flush();
                }
            }

            public override void Load()
            {
                if (FileManager.Map is ULMapLoader)
                    return;
                FileManager.MapLoaderReLoad(this);
                _UL._EOF = new uint[NumMaps];
                _filesStaticsStream = new FileStream[NumMaps];
                bool foundedOneMap = false;

                for (int i = 0; i < NumMaps; i++)
                {
                    string path = Path.Combine(_UL.ShardName, $"map{i}.mul");

                    if (File.Exists(path))
                    {
                        _filesMap[i] = new ULFileMul(path, false);
                        foundedOneMap = true;
                    }

                    path = Path.Combine(_UL.ShardName, $"statics{i}.mul");
                    if (!File.Exists(path))
                    {
                        foundedOneMap = false;
                        break;
                    }
                    _filesStatics[i] = new ULFileMul(path, true);
                    _filesStaticsStream[i] = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                    _UL._EOF[i] = (uint)new FileInfo(path).Length;
                        
                    path = Path.Combine(_UL.ShardName, $"staidx{i}.mul");
                    if (!File.Exists(path))
                    {
                        foundedOneMap = false;
                        break;
                    }
                    _filesIdxStatics[i] = new ULFileMul(path, false);
                }

                if (!foundedOneMap)
                    throw new FileNotFoundException($"No maps, staidx or statics found on {_UL.ShardName}.");
                int mapblocksize = UnsafeMemoryManager.SizeOf<MapBlock>();

                for (int i = 0; i < NumMaps; i++)
                {
                    MapBlocksSize[i, 0] = MapsDefaultSize[i, 0] >> 3;
                    MapBlocksSize[i, 1] = MapsDefaultSize[i, 1] >> 3;

                    if (Engine.GlobalSettings.PreloadMaps)
                        LoadMap(i);
                }
            }

            internal static void CheckForShardMapFile(int mapID)
            {
                string oldmap = Path.Combine(FileManager.UoFolderPath, $"map{mapID}.mul");
                string oldstaidx = Path.Combine(FileManager.UoFolderPath, $"staidx{mapID}.mul");
                string oldstatics = Path.Combine(FileManager.UoFolderPath, $"statics{mapID}.mul");
                //create file names
                string mapPath = Path.Combine(_UL.ShardName, $"map{mapID}.mul");
                string staidxPath = Path.Combine(_UL.ShardName, $"staidx{mapID}.mul");
                string staticsPath = Path.Combine(_UL.ShardName, $"statics{mapID}.mul");
                if (!File.Exists(mapPath))
                {
                    UOFile mapfile = GetMapFile(mapID);
                    
                    if(mapfile==null)
                    {
                        CreateNewPersistantMap(mapID, mapPath, staidxPath, staticsPath);
                    }
                    else
                    {
                        if (mapfile is UOFileUop uop)
                        {
                            Utility.Logging.Log.Message(Utility.Logging.LogTypes.Trace, $"UltimaLive -> converting file:\t{mapPath} from {uop.FilePath}");
                            using (FileStream stream = File.Create(mapPath))
                            {
                                for (int x = 0; x < uop.Entries.Length; x++)
                                {
                                    uop.Seek(uop.Entries[x].Offset);
                                    stream.Write(uop.ReadArray(uop.Entries[x].Length), 0, uop.Entries[x].Length);
                                }
                                stream.Flush();
                            }
                        }
                        else
                        {
                            CopyFile(oldmap, mapPath);
                        }
                    }
                }
                if (!File.Exists(staticsPath))
                    CopyFile(oldstatics, staticsPath);
                if (!File.Exists(staidxPath))
                    CopyFile(oldstaidx, staidxPath);
            }

            private static void CreateNewPersistantMap(int mapID, string mapPath, string staidxPath, string staticsPath)
            {
                int mapWidthInBlocks = FileManager.Map.MapBlocksSize[mapID, 0];//orizontal
                int mapHeightInBlocks = FileManager.Map.MapBlocksSize[mapID, 1];//vertical
                int numberOfBytesInStrip = 196 * mapHeightInBlocks;
                byte[] pVerticalBlockStrip = new byte[numberOfBytesInStrip];
                byte[] block = new byte[196]{
                          0x00, 0x00, 0x00, 0x00, //header
                          0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00,
                          0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00,
                          0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00,
                          0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00,
                          0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00,
                          0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00,
                          0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00,
                          0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00, 0x44, 0x02, 0x00 };
                for (int y = 0; y < mapHeightInBlocks; y++)
                {
                    Array.Copy(block, 0, pVerticalBlockStrip, 196 * y, 196);
                }

                //create map new file
                using (FileStream stream = File.Create(mapPath))
                {
                    Log.Message(LogTypes.Trace, $"UltimaLive -> creating new blank map:\t{mapPath}");
                    Log.Message(LogTypes.Trace, $"Writing {mapWidthInBlocks} blocks by {mapHeightInBlocks} blocks");
                    for (int x = 0; x < mapWidthInBlocks; x++)
                    {
                        stream.Write(pVerticalBlockStrip, 0, numberOfBytesInStrip);
                    }
                    stream.Flush();
                }
                numberOfBytesInStrip = 12 * mapHeightInBlocks;
                pVerticalBlockStrip = new byte[numberOfBytesInStrip];
                block = new byte[12] { 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                for (int y = 0; y < mapHeightInBlocks; y++)
                {
                    Array.Copy(block, 0, pVerticalBlockStrip, 12 * y, 12);
                }
                //create map new file
                using (FileStream stream = File.Create(staidxPath))
                {
                    Log.Message(LogTypes.Trace, $"UltimaLive -> creating new index file");
                    for (int x = 0; x < mapWidthInBlocks; x++)
                    {
                        stream.Write(pVerticalBlockStrip, 0, numberOfBytesInStrip);
                    }
                    stream.Flush();
                }
                using (FileStream stream = File.Create(staticsPath)) { Log.Message(LogTypes.Trace, $"UltimaLive -> creating empty static file"); }
            }

            private static void CopyFile(string fromfile, string tofile)
            {
                if (!File.Exists(tofile) || new FileInfo(tofile).Length == 0)
                {
                    Log.Message(LogTypes.Trace, $"UltimaLive -> copying file:\t{tofile} from {fromfile}");
                    File.Copy(fromfile, tofile, true);
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
        }
    }
}
