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
using ClassicUO.Game;
using ClassicUO.Game.Map;
using ClassicUO.Game.GameObjects;
using ClassicUO.Network;

namespace ClassicUO.IO
{
    class UltimaLive
    {
        public static bool IsUltimaLiveActive = false;
        public static string ShardName = null;
        private const int CRCLength = 25;
        private const int LandBlockLenght = 192;
        public static UInt16[][] MapCRCs;//caching, to avoid excessive cpu & memory use
        public static UInt16[,] WrapSize;
        public static void OnUltimaLivePacket(Packet p)
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
                        mapWidthInBlocks = blockX < (WrapSize[mapID, 0] >> 3) ? WrapSize[mapID, 0] >> 3 : mapWidthInBlocks;
                        mapHeightInBlocks = blockY < (WrapSize[mapID, 1] >> 3) ? WrapSize[mapID, 1] >> 3 : mapHeightInBlocks;
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

                                Int32 blocknum = (xBlockItr * mapHeightInBlocks) + yBlockItr;
                                if (blocknum >= 0 && blocknum < blocks)
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
                        int index = 0;
                        if (block >= 0 && block < (FileManager.Map.MapBlocksSize[mapID, 0] * FileManager.Map.MapBlocksSize[mapID, 1]))
                        {
                            var chunk = World.Map.Chunks[block];
                            for (int i = 0; i < 8; i++)
                            {
                                for (int j = 0; j < 8; j++)
                                {
                                    for (GameObject obj = chunk.Tiles[i, j].FirstNode; obj != null; obj = obj.Right)
                                    {
                                        if (obj is Static)
                                            chunk.Tiles[i, j].RemoveGameObject(obj);
                                    }
                                }
                            }
                            for (int k = 0; k < length; k++)
                            {
                                Tile t = chunk.Tiles[staticsData[index + 2], staticsData[index + 3]];
                                new Static((ushort)(staticsData[index] | (staticsData[index + 1] << 8)), (ushort)(staticsData[index + 5] | (staticsData[index + 6] << 8)), k)
                                {
                                    Position = new Position(t.X, t.Y, (sbyte)staticsData[index + 4])
                                }.AddToTile();
                                index += 7;
                            }
                            //instead of recalculating the CRC block 2 times, in case of terrain + statics update, we only set the actual block to ushort maxvalue, so it will be recalculated on next hash query
                            MapCRCs[mapID][block] = UInt16.MaxValue;
                        }
                        //TODO: write staticdata changes directly to disk, use packets only if server sent out where we should save files (see Live Login Confirmation)
                        break;
                    }
                case 0x01://map definition update
                    {
                        if (string.IsNullOrEmpty(ShardName) || p.Length < 15)
                        {
                            //we cannot validate the pathfolder or packet is not correct
                            return;
                        }

                        p.Seek(7);
                        uint maps = (p.ReadUInt() * 7) / 9;
                        if (WrapSize == null)
                            WrapSize = new UInt16[maps, 2];
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
                            ushort dimX = Math.Min((ushort)FileManager.Map.MapsDefaultSize[0, 0], p.ReadUShort());
                            ushort dimY = Math.Min((ushort)FileManager.Map.MapsDefaultSize[0, 1], p.ReadUShort());
                            WrapSize[mapnum,0] = p.ReadUShort();
                            WrapSize[mapnum,1] = p.ReadUShort();
                        }
                        IsUltimaLiveActive = true;//after receiving the shardname and map defs, we can consider the system as fully active
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

        public static void OnUpdateTerrainPacket(Packet p)
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
            int index = 0;
            if (block >= 0 && block < (FileManager.Map.MapBlocksSize[mapID, 0] * FileManager.Map.MapBlocksSize[mapID, 1]))
            {
                for (int i = 0; i < 8; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        for (GameObject obj = World.Map.Chunks[block].Tiles[j, i].FirstNode; obj != null; obj = obj.Right)
                        {
                            if (obj is Land ln)
                            {
                                ln.Graphic = (ushort)(landData[index++] | (landData[index++] << 8));
                                ln.Z = (sbyte)landData[index++];
                            }
                        }
                    }
                }
                //instead of recalculating the CRC block 2 times, in case of terrain + statics update, we only set the actual block to ushort maxvalue, so it will be recalculated on next hash query
                MapCRCs[mapID][block] = UInt16.MaxValue;
            }
        }

        public static UInt16 GetBlockCrc(int block, int xblock, int yblock)
        {
            byte[] landdata = new byte[LandBlockLenght];
            int stcount = 0;
            int blockByteIdx = 0;
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    for (GameObject obj = World.Map.GetMapChunk(block, xblock, yblock).Tiles[j, i].FirstNode; obj != null; obj = obj.Right)
                    {
                        if (obj is Land ln)
                        {
                            landdata[blockByteIdx] = (byte)(ln.Graphic & 0x00FF);
                            landdata[blockByteIdx + 1] = (byte)((ln.Graphic & 0xFF00) >> 8);
                            landdata[blockByteIdx + 2] = (byte)ln.Z;
                            blockByteIdx += 3;
                        }
                        else if (obj is Static)
                        {
                            stcount++;
                        }
                    }
                }
            }
            byte[] staticdata = new byte[stcount * 7];
            blockByteIdx = 0;
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    for (GameObject obj = World.Map.Chunks[block].Tiles[i, j].FirstNode; obj != null; obj = obj.Right)
                    {
                        if (obj is Static st)
                        {
                            staticdata[blockByteIdx] = (byte)(st.Graphic & 0x00FF);
                            staticdata[blockByteIdx + 1] = (byte)((st.Graphic & 0xFF00) >> 8);
                            staticdata[blockByteIdx + 2] = (byte)i;
                            staticdata[blockByteIdx + 3] = (byte)j;
                            staticdata[blockByteIdx + 4] = (byte)st.Z;
                            staticdata[blockByteIdx + 5] = (byte)(st.Hue & 0x00FF);
                            staticdata[blockByteIdx + 6] = (byte)((st.Hue & 0xFF00) >> 8);
                            blockByteIdx += 7;
                        }
                    }
                }
            }
            byte[] blockData = new byte[landdata.Length + staticdata.Length];
            Array.Copy(landdata, 0, blockData, 0, landdata.Length);
            Array.Copy(staticdata, 0, blockData, landdata.Length, staticdata.Length);
            ushort crc = Fletcher16(blockData);
            landdata = staticdata = blockData = null;
            return crc;
        }

        public static UInt16 Fletcher16(byte[] data)
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
    }
}
