#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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
using System.Runtime.CompilerServices;

using ClassicUO.Game.GameObjects;
using ClassicUO.IO.Resources;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Map
{
    internal sealed class Map
    {
        private readonly bool[] _blockAccessList = new bool[0x1000];
        private readonly List<int> _usedIndices = new List<int>();


        private const int CELL_NUM = 16;
        private const int CELL_SPAN = CELL_NUM * 2;

        //private static readonly Chunk[] _chunks = new Chunk[CELL_SPAN * CELL_SPAN];


        public Map(int index)
        {
            Index = index;
            BlocksCount = MapLoader.Instance.MapBlocksSize[Index, 0] * MapLoader.Instance.MapBlocksSize[Index, 1];
            Chunks = new Chunk[BlocksCount];
        }

        public readonly int Index;
        public Chunk[] Chunks;
        public readonly int BlocksCount;
        public Point Center;


        public Chunk GetChunk(int x, int y)
        {
            int cellX = x >> 3;
            int cellY = y >> 3;
            int block = GetBlock(cellX, cellY);

            if (block >= Chunks.Length)
                return null;

            ref Chunk chunk = ref Chunks[block];

            if (chunk == null)
            {
                _usedIndices.Add(block);
                chunk = Chunk.Create((ushort) cellX, (ushort) cellY);
                chunk.Load(Index);
            }
            else if (chunk.X != cellX || chunk.Y != cellY || chunk.Tiles[x % 8, y % 8] == null)
            {
                if (chunk.IsDestroyed)
                {
                    Console.WriteLine("RELOAD CHUNK!");

                    _usedIndices.Add(block);
                    chunk.X = (ushort) cellX;
                    chunk.Y = (ushort) cellY;
                }
                chunk.Load(Index);
            }

            chunk.LastAccessTime = Time.Ticks;

            return chunk;
        }

        public GameObject GetTile(short x, short y, bool load = true)
        {
            if (x < 0 || y < 0)
                return null;

            int cellX = x >> 3;
            int cellY = y >> 3;
            int block = GetBlock(cellX, cellY);

            if (block >= Chunks.Length)
                return null;

            ref Chunk chunk = ref Chunks[block];

            int dx = x % 8;
            int dy = y % 8;

            if (chunk == null)
            {
                if (load)
                {
                    _usedIndices.Add(block);
                    chunk = Chunk.Create((ushort) cellX, (ushort) cellY);
                    chunk.Load(Index);
                }
                else
                    return null;
            }
            else if (chunk.X != cellX || chunk.Y != cellY || chunk.Tiles[dx, dy] == null)
            {
                Console.WriteLine("RELOAD CHUNK!");

                if (chunk.IsDestroyed)
                {
                    _usedIndices.Add(block);
                    chunk.X = (ushort) cellX;
                    chunk.Y = (ushort) cellY;
                }
                
                chunk.Load(Index);
            }

            chunk.LastAccessTime = Time.Ticks;

            return chunk.Tiles[dx, dy];
        }

        public GameObject GetTile(int x, int y, bool load = true)
        {
            return GetTile((short) x, (short) y, load);
        }

        public sbyte GetTileZ(int x, int y)
        {
            if (x < 0 || y < 0)
                return -125;

            ref IndexMap blockIndex = ref GetIndex(x >> 3, y >> 3);

            if (blockIndex.MapAddress == 0)
                return -125;

            int mx = x % 8;
            int my = y % 8;

            unsafe
            {
                MapBlock* mp = (MapBlock*) blockIndex.MapAddress;
                MapCells* cells = (MapCells*) &mp->Cells;

                return cells[(my << 3) + mx].Z;
            }
        }

        public void GetMapZ(int x, int y, out sbyte groundZ, out sbyte staticZ)
        {
            var tile = GetTile(x, y);
            groundZ = staticZ = 0;

            if (tile == null)
            {
                return;
            }
            
            var obj = tile;

            while (obj != null)
            {
                if (obj is Land)
                    groundZ = obj.Z;
                else if (staticZ < obj.Z)
                    staticZ = obj.Z;
                obj = obj.Right;
            }
        }

        public void ClearBockAccess()
        {
            Array.Clear(_blockAccessList, 0, _blockAccessList.Length);
        }

        public sbyte CalculateNearZ(sbyte defaultZ, int x, int y, int z)
        {
            ref bool access = ref _blockAccessList[(x & 0x3F) + ((y & 0x3F) << 6)];

            if (access)
                return defaultZ;

            access = true;
            var tile = GetTile(x, y, false);

            if (tile != null)
            {
                GameObject obj = tile;

                while (obj.Left != null)
                    obj = obj.Left;

                for (; obj != null; obj = obj.Right)
                {
                    if (!(obj is Static) && !(obj is Multi))
                        continue;

                    if (obj.Graphic >= TileDataLoader.Instance.StaticData.Length)
                        continue;

                    if (!TileDataLoader.Instance.StaticData[obj.Graphic].IsRoof || Math.Abs(z - obj.Z) > 6)
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


        public ref IndexMap GetIndex(int blockX, int blockY)
        {
            int block = GetBlock(blockX, blockY);
            int map = Index;
            MapLoader.Instance.SanitizeMapIndex(ref map);
            IndexMap[] list = MapLoader.Instance.BlockData[map];

            return ref block >= list.Length ? ref IndexMap.Invalid : ref list[block];
        }

        [MethodImpl(256)]
        private int GetBlock(int blockX, int blockY)
        {
            return blockX * MapLoader.Instance.MapBlocksSize[Index, 1] + blockY;
        }


        public IEnumerable<int> GetUsedChunks()
        {
            foreach (int i in _usedIndices)
                yield return i;
        }


        public void ClearUnusedBlocks()
        {
            int count = 0;
            long ticks = Time.Ticks - Constants.CLEAR_TEXTURES_DELAY;

            for (int i = 0; i < _usedIndices.Count; i++)
            {
                ref Chunk block = ref Chunks[_usedIndices[i]];

                if (block.LastAccessTime < ticks && block.HasNoExternalData())
                {
                    block.Clear();
                    block.IsDestroyed = true;
                    //block = null;
                    _usedIndices.RemoveAt(i--);

                    if (++count >= Constants.MAX_MAP_OBJECT_REMOVED_BY_GARBAGE_COLLECTOR)
                        break;
                }
            }
        }

        public void Destroy()
        {
            for (int i = 0; i < _usedIndices.Count; i++)
            {
                ref Chunk block = ref Chunks[_usedIndices[i]];
                block.Destroy();
                block.IsDestroyed = true;
                //block = null;
            }

            _usedIndices.Clear();
            //Chunks = null;
        }

        public void Initialize()
        {
            return;
            const int XY_OFFSET = 30;

            int minBlockX = ((Center.X - XY_OFFSET) >> 3) - 1;
            int minBlockY = ((Center.Y - XY_OFFSET) >> 3) - 1;
            int maxBlockX = ((Center.X + XY_OFFSET) >> 3) + 1;
            int maxBlockY = ((Center.Y + XY_OFFSET) >> 3) + 1;

            if (minBlockX < 0)
                minBlockX = 0;

            if (minBlockY < 0)
                minBlockY = 0;

            if (maxBlockX >= MapLoader.Instance.MapBlocksSize[Index, 0])
                maxBlockX = MapLoader.Instance.MapBlocksSize[Index, 0] - 1;

            if (maxBlockY >= MapLoader.Instance.MapBlocksSize[Index, 1])
                maxBlockY = MapLoader.Instance.MapBlocksSize[Index, 1] - 1;
            long tick = Time.Ticks;
            long maxDelay = 100; /*Engine.FrameDelay[1] >> 1*/;

            for (int i = minBlockX; i <= maxBlockX; i++)
            {
                int index = i * MapLoader.Instance.MapBlocksSize[Index, 1];

                for (int j = minBlockY; j <= maxBlockY; j++)
                {
                    int cellindex = index + j;
                    ref Chunk chunk = ref Chunks[cellindex];

                    if (chunk == null)
                    {
                        if (Time.Ticks - tick >= maxDelay)
                            return;

                        _usedIndices.Add(cellindex);
                        chunk = Chunk.Create((ushort) i, (ushort) j);
                        chunk.Load(Index);
                    }

                    chunk.LastAccessTime = Time.Ticks;
                }
            }
        }
    }
}