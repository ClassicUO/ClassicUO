#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
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
using System.IO.MemoryMappedFiles;
using ClassicUO.Game;
using ClassicUO.Game.Map;
using ClassicUO.Game.GameObjects;
using ClassicUO.Network;
using ClassicUO.Utility.Logging;

namespace ClassicUO.IO
{
    internal class UltimaLive
    {
        internal static void Load()
        {
            Log.Message(LogTypes.Trace, "Setup packet for Ultima live", ConsoleColor.DarkGreen);
            PacketHandlers.ToClient.Add(0x3F, OnUltimaLivePacket);
            PacketHandlers.ToClient.Add(0x40, OnUpdateTerrainPacket);
        }

        internal static bool IsUltimaLiveActive = false;
        internal static string ShardName = null;
        private const int CRCLength = 25;
        private const int LandBlockLenght = 192;

        private static UInt16[][] MapCRCs;//caching, to avoid excessive cpu & memory use
        private static UOFile[] _filesMap;
        private static UOFileMul[] _filesStatics;
        private static UOFileMul[] _filesIdxStatics;
        private static FileStream[] _filesStaticsStream;
        private static uint[] _EOF;
        //WrapMapSize includes 2 different kind of values at each side of the array:
        //left - mapID (zero based value), so first map is at ZERO
        //right- we have the size of the map, values in index 0 and 1 are wrapsize x and y
        //       values in index 2 and 3 is for the total size of map, x and y
        private static UInt16[,] WrapMapSize;
        private static void OnUltimaLivePacket(Packet p)
        {
            p.Seek(13);
            byte command = p.ReadByte();
            switch (command)
            {
                case 0xFF://hash query, for the blocks around us
                    {
                        if (!IsUltimaLiveActive || p.Length < 15)
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

                        if (MapCRCs[mapID] == null)
                        {
                            MapCRCs[mapID] = new UInt16[blocks];
                            for (int j = 0; j < blocks; j++)
                            {
                                MapCRCs[mapID][j] = UInt16.MaxValue;
                            }
                        }

                        int blockX = block / mapHeightInBlocks;
                        int blockY = block % mapHeightInBlocks;
                        //this will avoid going OVER the wrapsize, so that we have the ILLUSION of never going over the main world
                        mapWidthInBlocks = blockX < (WrapMapSize[mapID, 2] >> 3) ? WrapMapSize[mapID, 2] >> 3 : mapWidthInBlocks;
                        mapHeightInBlocks = blockY < (WrapMapSize[mapID, 3] >> 3) ? WrapMapSize[mapID, 3] >> 3 : mapHeightInBlocks;
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
                                    UInt16 crc = MapCRCs[mapID][blocknum];
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
                                        MapCRCs[mapID][blocknum] = crc;
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
                        if (!IsUltimaLiveActive || p.Length < 15)
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
                            var chunk = World.Map.Chunks[block];
                            if (chunk != null)
                            {
                                for (int x = 0; x < 8; x++)
                                {
                                    for (int y = 0; y < 8; y++)
                                    {
                                        for (GameObject obj = chunk.Tiles[x, y].FirstNode; obj != null; obj = obj.Right)
                                        {
                                            if (obj is Land || obj is Static)
                                                obj.Dispose();
                                        }
                                    }
                                }
                            }
                            if (totallen <= 0)
                            {
                                //update index lookup AND static size on disk (first 4 bytes lookup, next 4 is statics size)
                                _filesIdxStatics[mapID].WriteArray(block * 12, new byte[8] { 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00 });
                            }
                            else
                            {
                                _filesIdxStatics[mapID].Seek(block * 12);
                                uint lookup = _filesIdxStatics[mapID].ReadUInt();
                                uint existingStaticsLength = _filesIdxStatics[mapID].ReadUInt();

                                //Do we have enough room to write the statics into the existing location?
                                if (existingStaticsLength >= totallen && lookup != 0xFFFFFFFF)
                                {
                                    Log.Message(LogTypes.Trace, $"writing statics to existing file location at 0x{lookup:X8}, length:{totallen}");
                                }
                                else
                                {
                                    lookup = _EOF[mapID];
                                    _EOF[mapID] += (uint)totallen;
                                    Log.Message(LogTypes.Trace, $"writing statics to end of file at 0x{lookup:X8}, length:{totallen}");
                                }
                                _filesStatics[mapID].WriteArray(lookup, staticsData);
                                _filesStaticsStream[mapID].Seek(lookup, SeekOrigin.Begin);
                                _filesStaticsStream[mapID].Write(staticsData, 0, staticsData.Length);
                                _filesStaticsStream[mapID].Flush();
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
                                _filesIdxStatics[mapID].WriteArray(block * 12, idxdata);
                            }
                            FileManager.Map.ReloadBlock(mapID, block);
                            chunk?.Load(mapID);
                            //instead of recalculating the CRC block 2 times, in case of terrain + statics update, we only set the actual block to ushort maxvalue, so it will be recalculated on next hash query
                            //also the server should always send FIRST the landdata packet, and only AFTER land the statics packet
                            MapCRCs[mapID][block] = UInt16.MaxValue;
                        }
                        break;
                    }
                case 0x01://map definition update
                    {
                        if (string.IsNullOrEmpty(ShardName) || p.Length < 15)
                        {
                            //we cannot validate the pathfolder or packet is not correct
                            return;
                        }
                        else if(!Directory.Exists(ShardName))
                        {
                            Directory.CreateDirectory(ShardName);
                        }

                        p.Seek(7);
                        uint maps = (p.ReadUInt() * 7) / 9;
                        WrapMapSize = new UInt16[maps, 4];//we always need to reinitialize this, as it could change from login to login even on the same server, in case of map changes.
                        if (p.Length < (maps * 9 + 15))//the packet has padding inside, so it's usually larger or equal than what we expect
                        {
                            return;
                        }
                        if (MapCRCs == null || MapCRCs.Length < maps)
                        {
                            MapCRCs = new UInt16[maps][];
                        }
                        p.Seek(15);//byte 15 to end of packet, the map definitions
                        for (int i = 0; i < maps; i++)
                        {
                            int mapnum = p.ReadByte();
                            WrapMapSize[mapnum,0] = Math.Min((ushort)FileManager.Map.MapsDefaultSize[0, 0], p.ReadUShort());
                            WrapMapSize[mapnum,1] = Math.Min((ushort)FileManager.Map.MapsDefaultSize[0, 1], p.ReadUShort());
                            WrapMapSize[mapnum,2] = Math.Min(p.ReadUShort(), WrapMapSize[mapnum,0]);
                            WrapMapSize[mapnum,3] = Math.Min(p.ReadUShort(), WrapMapSize[mapnum,1]);
                        }
                        var refs = FileManager.Map.GetFileReferences;
                        _filesMap = refs.Item1;
                        _filesStatics = refs.Item2;
                        _filesIdxStatics = refs.Item3;
                        _EOF = new uint[maps];
                        _filesStaticsStream = new FileStream[maps];
                        IsUltimaLiveActive = Directory.Exists(ShardName);
                        for (int i = 0; i < maps && IsUltimaLiveActive; i++)
                        {
                            _filesMap[i] = CheckForShardFiles(i);
                            _filesStaticsStream[i] = File.Open(_filesStatics[i].FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                            IsUltimaLiveActive = _filesMap[i].UltimaLiveReloader(null) > 0 && _filesIdxStatics[i].UltimaLiveReloader(null) > 0 && (_EOF[i] = _filesStatics[i].UltimaLiveReloader(_filesStaticsStream[i])) > 0;
                            FileManager.Map.LoadMap(i);
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
                        ShardName = ValidatePath(p.ReadASCII());
                        //TODO: create shard directory, copy map and statics to that directory, use that files instead of the original ones
                        break;
                    }
                    /*case 0x03://Refresh client VIEW - after an update the server will usually send this packet to refresh the client view, this packet has been discontinued after ultimalive 0.96 and isn't necessary anymore
                        {
                            break;
                        }*/
            }
        }

        private static UOFile CheckForShardFiles(int mapID)
        {
            bool valid = false;
            if (_filesMap[mapID] is UOFileUop uop)
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
                valid = _filesMap[mapID] is UOFileMul;
            }
            else
            {
                valid = CheckFilePresence(_filesMap[mapID]);
            }
            valid = valid && CheckFilePresence(_filesIdxStatics[mapID]) && CheckFilePresence(_filesStatics[mapID]);

            return valid ? _filesMap[mapID] : null;
        }

        private static bool CheckFilePresence(UOFile file)
        {
            string oldfile = file.FilePath;
            file.FilePath = Path.Combine(UltimaLive.ShardName, Path.GetFileName(file.FilePath));
            if (!Directory.Exists(UltimaLive.ShardName))
                return false;
            if (!File.Exists(file.FilePath) || new FileInfo(file.FilePath).Length == 0)
            {
                Log.Message(LogTypes.Trace, $"UltimaLive -> copying file:\t{file.FilePath} from {oldfile}");
                File.Copy(oldfile, file.FilePath, true);
            }
            return new FileInfo(file.FilePath).Length > 0;
        }

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

            if (block >= 0 && block < (FileManager.Map.MapBlocksSize[mapID, 0] * FileManager.Map.MapBlocksSize[mapID, 1]))
            {
                var chunk = World.Map.Chunks[block];
                if (chunk != null)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        for (int y = 0; y < 8; y++)
                        {
                            for (GameObject obj = chunk.Tiles[x, y].FirstNode; obj != null; obj = obj.Right)
                            {
                                if (obj is Land || obj is Static)
                                    obj.Dispose();
                            }
                        }
                    }
                }
                _filesMap[mapID].WriteArray((block * 196) + 4, landData);
                FileManager.Map.ReloadBlock(mapID, block);
                chunk?.Load(mapID);
                //instead of recalculating the CRC block 2 times, in case of terrain + statics update, we only set the actual block to ushort maxvalue, so it will be recalculated on next hash query
                MapCRCs[mapID][block] = UInt16.MaxValue;
            }
        }

        internal static UInt16 GetBlockCrc(uint block, int xblock, int yblock)
        {
            int mapID = World.Map.Index;
            _filesIdxStatics[mapID].Seek(block * 12);
            uint lookup = _filesIdxStatics[mapID].ReadUInt();
            int bytecount = Math.Max(0, _filesIdxStatics[mapID].ReadInt());
            byte[] blockData = new byte[LandBlockLenght + bytecount];
            //we prevent the system from reading beyond the end of file, causing an exception, if the data isn't there, we don't read it and leave the array blank, simple...
            _filesMap[mapID].Seek((block * 196) + 4);
            for(int x = 0; x < 192; x++)
            {
                if (_filesMap[mapID].Position + 1 >= _filesMap[mapID].Length)
                    break;
                blockData[x] = _filesMap[mapID].ReadByte();
            }
            /*_filesMap[mapID]._Stream.Seek((block * 196) + 4, SeekOrigin.Begin);
            _filesMap[mapID]._Stream.Read(blockData, 0, LandBlockLenght);*/
            if (lookup != 0xFFFFFFFF && bytecount > 0)
            {
                if(lookup < _filesStatics[mapID].Length)
                {
                    _filesStatics[mapID].Seek(lookup);
                    for(int x = LandBlockLenght; x < blockData.Length; x++)
                    {
                        if (_filesStatics[mapID].Position + 1 >= _filesStatics[mapID].Length)
                            break;
                        blockData[x] = _filesStatics[mapID].ReadByte();
                    }
                }
                /*_filesStatics[mapID]._Stream.Seek(lookup, SeekOrigin.Begin);
                _filesStatics[mapID]._Stream.Read(blockData, LandBlockLenght, stcount);*/
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

        public static void Dispose()
        {
            if(_filesStaticsStream!=null)
            {
                for (int i = _filesStaticsStream.Length - 1; i >= 0; --i)
                {
                    _filesStaticsStream[i]?.Dispose();
                }
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
    }
}
