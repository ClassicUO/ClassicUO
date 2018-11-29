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
using System.Collections.Generic;

using ClassicUO.Game.GameObjects;
using ClassicUO.Interfaces;
using ClassicUO.IO.Resources;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Map
{
    public sealed class Facet : IDisposable
    {
       // private static Tile _invalid = Tile.Invalid;
        private readonly bool[] _blockAccessList = new bool[0x1000];
        //private const int CHUNKS_NUM = 5;
        //private const int MAX_CHUNKS = CHUNKS_NUM * 2 + 1;
        private readonly List<int> _usedIndices = new List<int>();
        private Point _center;

        public Facet(int index)
        {
            Index = index;
            IO.Resources.Map.LoadMap(index);
            MapBlockIndex = IO.Resources.Map.MapBlocksSize[Index][0] * IO.Resources.Map.MapBlocksSize[Index][1];
            Chunks = new MapChunk[MapBlockIndex];
        }

        public int Index { get; }

        public MapChunk[] Chunks { get; private set; }

        public int MapBlockIndex { get; set; }

        public Point Center
        {
            get => _center;
            set
            {
                if (_center != value)
                {
                    _center = value;
                    LoadChunks((ushort) _center.X, (ushort) _center.Y);
                }
            }
        }

        public  Tile GetTile(short x, short y, bool load = true)
        {
            if (x < 0 || y < 0) return null;
            int cellX = x / 8;
            int cellY = y / 8;
            int block = GetBlock(cellX, cellY);

            if (block >= Chunks.Length)
                return null;
            ref MapChunk chuck = ref Chunks[block];

            if (chuck == null /*MapChunk.Invalid*/)
            {
                if (load)
                {
                    _usedIndices.Add(block);
                    chuck = new MapChunk((ushort) cellX, (ushort) cellY);
                    chuck.Load(Index);
                }
                else
                    return null;
            }
            else
                chuck.LastAccessTime = CoreGame.Ticks;

            return chuck.Tiles[x % 8][y % 8];
        }

        public  Tile GetTile(int x, int y, bool load = true)
        {
            return  GetTile((short) x, (short) y, load);
        }

        public sbyte GetTileZ(int x, int y)
        {
            if (x < 0 || y < 0)
                return -125;
            IndexMap blockIndex = GetIndex(x / 8, y / 8);

            if (blockIndex.MapAddress == 0)
                return -125;
            int mx = x % 8;
            int my = y % 8;

            unsafe
            {
                MapBlock* mp = (MapBlock*) blockIndex.MapAddress;
                MapCells* cells = (MapCells*) &mp->Cells;

                return cells[my * 8 + mx].Z;
            }
        }

        public void ClearBockAccess()
        {
            unsafe
            {
                fixed (bool* ptr = _blockAccessList)
                {
                    byte* start = (byte*) ptr;
                    byte* end = start + _blockAccessList.Length;
                    while (&start[0] != &end[0]) *start++ = 0;
                }
            }
        }

        public sbyte CalculateNearZ(sbyte defaultZ, int x, int y, int z)
        {
            ref bool access = ref _blockAccessList[(x & 0x3F) + ((y & 0x3F) << 6)];

            if (access)
                return defaultZ;
            access = true;
             Tile tile =  GetTile(x, y);

            if (tile != null)
            {
                var objects = tile.ObjectsOnTiles;
                GameObject obj = null;

                for (int i = 0; i < objects.Count; i++)
                {
                    obj = objects[i];

                    if (!(obj is Static) && obj is Item item && !item.IsMulti)
                        continue;

                    if (obj is Mobile)
                        continue;

                    if (obj is IDynamicItem dyn && (!TileData.IsRoof(dyn.ItemData.Flags) || Math.Abs(z - obj.Z) > 6))
                        continue;

                    break;
                }

                if (obj == null)
                    return defaultZ;
                sbyte tileZ = obj.Z;

                if (tileZ < defaultZ)
                    defaultZ = tileZ;
                defaultZ = CalculateNearZ(defaultZ, x - 1, y, tileZ);
                defaultZ = CalculateNearZ(defaultZ, x + 1, y, tileZ);
                defaultZ = CalculateNearZ(defaultZ, x, y - 1, tileZ);
                defaultZ = CalculateNearZ(defaultZ, x, y + 1, tileZ);
            }

            return defaultZ;
        }

        public IndexMap GetIndex(int blockX, int blockY)
        {
            int block = GetBlock(blockX, blockY);
            ref IndexMap[] list = ref IO.Resources.Map.BlockData[Index];

            return block >= list.Length ? IndexMap.Invalid : list[block];
        }

        private int GetBlock(int blockX, int blockY)
        {
            return blockX * IO.Resources.Map.MapBlocksSize[Index][1] + blockY;
        }

        public void ClearUnusedBlocks()
        {
            int count = 0;
            long ticks = CoreGame.Ticks - 3000;

            for (int i = 0; i < _usedIndices.Count; i++)
            {
                ref MapChunk block = ref Chunks[_usedIndices[i]];

                if (block.LastAccessTime < ticks && block.HasNoExternalData())
                {
                    block.Unload();
                    block = null; // MapChunk.Invalid;
                    _usedIndices.RemoveAt(i--);

                    if (++count >= 50)
                        break;
                }
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < _usedIndices.Count; i++)
            {
                ref MapChunk block = ref Chunks[_usedIndices[i]];
                block.Unload();
                block = null;// MapChunk.Invalid;
                _usedIndices.RemoveAt(i--);
            }

            IO.Resources.Map.UnloadMap(Index);
            Chunks = null;
        }

        private void LoadChunks(ushort centerX, ushort centerY)
        {
            const int XY_OFFSET = 30;
            int minBlockX = (centerX - XY_OFFSET) / 8 - 1;
            int minBlockY = (centerY - XY_OFFSET) / 8 - 1;
            int maxBlockX = (centerX + XY_OFFSET) / 8 + 1;
            int maxBlockY = (centerY + XY_OFFSET) / 8 + 1;

            if (minBlockX < 0)
                minBlockX = 0;

            if (minBlockY < 0)
                minBlockY = 0;

            if (maxBlockX >= IO.Resources.Map.MapBlocksSize[Index][0])
                maxBlockX = IO.Resources.Map.MapBlocksSize[Index][0] - 1;

            if (maxBlockY >= IO.Resources.Map.MapBlocksSize[Index][1])
                maxBlockY = IO.Resources.Map.MapBlocksSize[Index][1] - 1;
            long tick = CoreGame.Ticks;
            long maxDelay = CoreGame.FrameDelay[1] / 2;

            for (int i = minBlockX; i <= maxBlockX; i++)
            {
                int index = i * IO.Resources.Map.MapBlocksSize[Index][1];

                for (int j = minBlockY; j <= maxBlockY; j++)
                {
                    int cellindex = index + j;
                    ref MapChunk chunk = ref Chunks[cellindex];

                    if (chunk == null /*MapChunk.Invalid*/)
                    {
                        if (CoreGame.Ticks - tick >= maxDelay)
                            return;
                        _usedIndices.Add(cellindex);
                        chunk = new MapChunk((ushort) i, (ushort) j);
                        chunk.Load(Index);
                    }

                    chunk.LastAccessTime = CoreGame.Ticks;
                }
            }
        }
    }
}