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
using System.Runtime.InteropServices;
using ClassicUO.IO.Resources;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Map
{
    public sealed class Facet
    {
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
        public MapChunk[] Chunks { get; }
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


        public Tile GetTile(short x, short y)
        {
            if (x < 0 || y < 0) return null;

            int cellX = x / 8;
            int cellY = y / 8;

            int block = GetBlock(cellX, cellY);

            ref MapChunk chuck = ref Chunks[block];

            if (chuck == null)
            {
                _usedIndices.Add(block);
                chuck = new MapChunk((ushort) cellX, (ushort) cellY);
                chuck.Load(Index);
            }

            chuck.LastAccessTime = CoreGame.Ticks;
            return chuck.Tiles[x % 8][y % 8];

            //int cellindex = cellY % MAX_CHUNKS * MAX_CHUNKS + cellX % MAX_CHUNKS;
            //// int cellindex = (cellX * AssetsLoader.Map.MapBlocksSize[Index][1]) + cellY;


            //ref var tile = ref Chunks[cellindex];

            //if (tile == null || tile.X != cellX || tile.Y != cellY)
            //{
            //    return null;
            //}

            //return tile.Tiles[x % 8][y % 8];
        }

        public Tile GetTile(int x, int y)
            => GetTile((short) x, (short) y);

        public sbyte GetTileZ(short x, short y)
        {
            Tile tile = GetTile(x, y);
            if (tile == null)
            {
                //int cellX = x / 8;
                //int cellY = y / 8;

                //int index = (cellX * AssetsLoader.Map.MapBlocksSize[Index][1]) + cellY;

                //Chunks[index] = new FacetChunk((ushort)cellX, (ushort)cellY);
                //Chunks[index].Load(Index);
                //return Chunks[index].Tiles[x % 8][y % 8].Position.Z;

                if (x < 0 || y < 0) return -125;

                IndexMap blockIndex = GetIndex(x / 8, y / 8);
                if (blockIndex.MapAddress == 0) return -125;

                int mx = x % 8;
                int my = y % 8;

                return Marshal.PtrToStructure<MapBlock>((IntPtr) blockIndex.MapAddress).Cells[my * 8 + mx].Z;
            }

            return tile.Position.Z;
        }


        public IndexMap GetIndex(int blockX, int blockY) => IO.Resources.Map.BlockData[Index][GetBlock(blockX, blockY)];

        private int GetBlock(int blockX, int blockY) => blockX * IO.Resources.Map.MapBlocksSize[Index][1] + blockY;


        public int GetAverageZ(sbyte top, sbyte left, sbyte right, sbyte bottom, ref sbyte low, ref sbyte high)
        {
            high = top;
            if (left > high) high = left;

            if (right > high) high = right;

            if (bottom > high) high = bottom;

            low = high;
            if (left < low) low = left;

            if (right < low) low = right;

            if (bottom < low) low = bottom;

            //if (top < low)
            //    low = top;
            //if (right < low)
            //    low = right;
            //if (bottom < low)
            //    low = bottom;

            //if (Math.Abs(high - right) <= Math.Abs(bottom - top))
            //    return (high + right) >> 1;
            //else
            //    return (bottom + top) >> 1;


            //if (Math.Abs(left - right) <= Math.Abs(bottom - top))
            //    return (left + right) >> 1;
            //else
            //    return (bottom + top) >> 1;

            if (Math.Abs(top - bottom) > Math.Abs(left - right)) return FloorAverage(left, right);

            return FloorAverage(top, bottom);
        }

        public int GetAverageZ(short x, short y, ref sbyte low, ref sbyte top) => GetAverageZ(GetTileZ(x, y),
            GetTileZ(x, (short) (y + 1)), GetTileZ((short) (x + 1), y), GetTileZ((short) (x + 1), (short) (y + 1)),
            ref low, ref top);

        private static int FloorAverage(int a, int b)
        {
            int v = a + b;

            if (v < 0)
                --v;
            return v / 2;
        }


        public void ClearUnusedBlocks()
        {
            int count = 0;
            for (int i = 0; i < _usedIndices.Count; i++)
            {
                ref MapChunk block = ref Chunks[_usedIndices[i]];
                if (CoreGame.Ticks - block.LastAccessTime >= 3000 && block.HasNoExternalData())
                {
                    block.Unload();
                    block = null;
                    _usedIndices.RemoveAt(i++);
                    if (++count >= 5)
                        break;
                }
            }
        }


        private void LoadChunks(ushort centerX, ushort centerY)
        {
            //for (int y = -CHUNKS_NUM; y <= CHUNKS_NUM; y++)
            //{
            //    int cellY = centerY / 8 + y;
            //    if (cellY < 0)
            //    {
            //        cellY += IO.Resources.Map.MapBlocksSize[Index][1];
            //    }

            //    for (int x = -CHUNKS_NUM; x <= CHUNKS_NUM; x++)
            //    {
            //        int cellX = centerX / 8 + x;
            //        if (cellX < 0)
            //        {
            //            cellX += IO.Resources.Map.MapBlocksSize[Index][0];
            //        }

            //        int cellindex = cellY % MAX_CHUNKS * MAX_CHUNKS + cellX % MAX_CHUNKS;

            //        ref var tile = ref Chunks[cellindex];

            //        if (tile == null || tile.X != cellX || tile.Y != cellY)
            //        {
            //            if (tile == null)
            //            {
            //                tile = new FacetChunk((ushort)cellX, (ushort)cellY);
            //            }
            //            else
            //            {
            //                tile.Unload();
            //                tile.SetTo((ushort)cellX, (ushort)cellY);
            //            }


            //            tile.Load(Index);
            //        }
            //    }
            //}


            const int XY_OFFSET = 30;

            int minBlockX = (centerX - XY_OFFSET) / 8 - 1;
            int minBlockY = (centerY - XY_OFFSET) / 8 - 1;
            int maxBlockX = (centerX + XY_OFFSET) / 8 + 2;
            int maxBlockY = (centerY + XY_OFFSET) / 8 + 2;

            if (minBlockX < 0)
                minBlockX = 0;
            if (minBlockY < 0)
                minBlockY = 0;
            if (maxBlockX >= IO.Resources.Map.MapBlocksSize[Index][0])
                maxBlockX = IO.Resources.Map.MapBlocksSize[Index][0] - 1;
            if (maxBlockY >= IO.Resources.Map.MapBlocksSize[Index][1])
                maxBlockY = IO.Resources.Map.MapBlocksSize[Index][1] - 1;

            for (int i = minBlockX; i <= maxBlockX; i++)
            {
                int index = i * IO.Resources.Map.MapBlocksSize[Index][1];

                for (int j = minBlockY; j <= maxBlockY; j++)
                {
                    int cellindex = index + j;

                    ref MapChunk tile = ref Chunks[cellindex];

                    if (tile == null)
                    {
                        _usedIndices.Add(cellindex);
                        tile = new MapChunk((ushort) i, (ushort) j);
                        tile.Load(Index);
                    }

                    tile.LastAccessTime = CoreGame.Ticks;
                }
            }
        }
    }
}