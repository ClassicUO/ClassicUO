using System;
using System.Collections.Generic;
using ClassicUO.Game;
using ClassicUO.Game.Map;
using ClassicUO.Game.GameObjects;
using ClassicUO.Network;

namespace ClassicUO.IO
{
    class UltimaLive
    {
        private const int CRCLength = 25;
        public static UInt16[][] MapCRCs = new UInt16[256][];//caching, to avoid excessive cpu & memory use
        public static void OnUltimaLivePacket(Packet p)
        {
            p.Seek(13);
            byte command = p.ReadByte();
            switch (command)
            {
                case 0xFF://hash query, for the blocks around us
                    {
                        if (p.Length < 15)
                            return;
                        p.Seek(3);
                        int block = (int)p.ReadUInt();
                        p.Seek(14);
                        int mapID = p.ReadByte();
                        if (World.Map==null || mapID != World.Map.Index)
                            return;
                        MapChunk chunk = World.Map.Chunks[block];
                        int mapWidthInBlocks = IO.Resources.Map.MapBlocksSize[mapID][0];
                        int mapHeightInBlocks = IO.Resources.Map.MapBlocksSize[mapID][1];
                        int blocks = mapWidthInBlocks * mapHeightInBlocks;
                        if (MapCRCs[mapID] == null)
                        {
                            MapCRCs[mapID] = new UInt16[blocks];
                            for (int j = 0; j < blocks; j++)
                                MapCRCs[mapID][j] = UInt16.MaxValue;
                        }
                        if (block < 0 || block >= blocks)
                            return;
                        int blockX = block / mapHeightInBlocks;
                        int blockY = block % mapHeightInBlocks;
                        ushort[] tosendCRCs = new ushort[CRCLength];     //byte 015 through 64   -  25 block CRCs
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

                                Int32 blocknum = (xBlockItr * mapHeightInBlocks) + yBlockItr;
                                if (blocknum >= 0 && blocknum <= (mapHeightInBlocks * mapWidthInBlocks))
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

                                            crc = GetBlockCrc(blocknum, mapID);
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
            }
        }
        public static UInt16 GetBlockCrc(int block, int mapID)
        {
            byte[] landdata = new byte[64 * 3];
            int stcount = 0;
            int blockByteIdx = 0;
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    IReadOnlyList<GameObject> list = World.Map.Chunks[block].Tiles[j][i].ObjectsOnTiles;
                    for (int k = 0; k < list.Count; k++)
                    {
                        GameObject o = list[k];
                        if (o is Land ln)
                        {
                            landdata[blockByteIdx] = (byte)(ln.Graphic & 0x00FF);
                            landdata[blockByteIdx + 1] = (byte)((ln.Graphic & 0xFF00) >> 8);
                            landdata[blockByteIdx + 2] = (byte)ln.Z;
                            blockByteIdx += 3;
                        }
                        else if (o is Static)
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
                    IReadOnlyList<GameObject> list = World.Map.Chunks[block].Tiles[i][j].ObjectsOnTiles;
                    for (int k = 0; k < list.Count; k++)
                    {
                        GameObject o = list[k];
                        if (o is Static st)
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

        public sealed class UltimaLiveHashResponse : PacketWriter
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
    }
}
