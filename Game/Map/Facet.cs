using ClassicUO.IO.Resources;
using Microsoft.Xna.Framework;
using System;
using System.Runtime.InteropServices;

namespace ClassicUO.Game.Map
{
    public sealed class Facet
    {
        private const int CHUNKS_NUM = 5;
        private const int MAX_CHUNKS = CHUNKS_NUM * 2 + 1;

        private Point _center;

        public Facet(in int index)
        {
            Index = index;

            Chunks = new FacetChunk[MAX_CHUNKS * MAX_CHUNKS];
        }

        public int Index { get; }
        public FacetChunk[] Chunks { get; }

        public Point Center
        {
            get => _center;
            set
            {
                if (_center != value)
                {
                    _center = value;
                    LoadChunks((ushort)_center.X, (ushort)_center.Y);
                }
            }
        }

        public Tile GetTile(in short x, in short y)
        {
            int cellX = x / 8;
            int cellY = y / 8;
            int cellindex = cellY % MAX_CHUNKS * MAX_CHUNKS + cellX % MAX_CHUNKS;
            // int cellindex = (cellX * AssetsLoader.Map.MapBlocksSize[Index][1]) + cellY;

            if (x < 0 || y < 0)
            {
                return null;
            }

            ref var tile = ref Chunks[cellindex];

            if (tile == null || tile.X != cellX || tile.Y != cellY)
            {
                return null;
            }

            return tile.Tiles[x % 8][y % 8];
        }

        public Tile GetTile(int x, int y)
        {
            return GetTile((short)x, (short)y);
        }


        public sbyte GetTileZ(in short x, in short y)
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

                if (x < 0 || y < 0)
                {
                    return -125;
                }

                IndexMap blockIndex = GetIndex(x / 8, y / 8);
                if (blockIndex.MapAddress == 0)
                {
                    return -125;
                }

                int mx = x % 8;
                int my = y % 8;

                return Marshal.PtrToStructure<MapBlock>((IntPtr)blockIndex.MapAddress).Cells[my * 8 + mx].Z;
            }

            return tile.Position.Z;
        }

        private IndexMap GetIndex(in int blockX, in int blockY)
        {
            int block = blockX * IO.Resources.Map.MapBlocksSize[Index][1] + blockY;
            return IO.Resources.Map.BlockData[Index][block];
        }

        public int GetAverageZ(in sbyte top, in sbyte left, in sbyte right, in sbyte bottom, ref sbyte low, ref sbyte high)
        {
            high = top;
            if (left > high)
            {
                high = left;
            }

            if (right > high)
            {
                high = right;
            }

            if (bottom > high)
            {
                high = bottom;
            }

            low = high;
            if (left < low)
            {
                low = left;
            }

            if (right < low)
            {
                low = right;
            }

            if (bottom < low)
            {
                low = bottom;
            }

            if (Math.Abs(top - bottom) > Math.Abs(left - right))
            {
                return FloorAverage(left, right);
            }

            return FloorAverage(top, bottom);
        }

        public int GetAverageZ(short x, short y, ref sbyte low, ref sbyte top)
        {
            return GetAverageZ(GetTileZ(x, y), GetTileZ(x, (short)(y + 1)), GetTileZ((short)(x + 1), y), GetTileZ((short)(x + 1), (short)(y + 1)), ref low, ref top);
        }

        private static int FloorAverage(in int a, in int b)
        {
            int v = a + b;

            if (v < 0)
            {
                --v;
            }

            return v / 2;
        }

        private void LoadChunks(in ushort centerX, in ushort centerY)
        {
            for (int y = -CHUNKS_NUM; y <= CHUNKS_NUM; y++)
            {
                int cellY = centerY / 8 + y;
                if (cellY < 0)
                {
                    cellY += IO.Resources.Map.MapBlocksSize[Index][1];
                }

                for (int x = -CHUNKS_NUM; x <= CHUNKS_NUM; x++)
                {
                    int cellX = centerX / 8 + x;
                    if (cellX < 0)
                    {
                        cellX += IO.Resources.Map.MapBlocksSize[Index][0];
                    }

                    int cellindex = cellY % MAX_CHUNKS * MAX_CHUNKS + cellX % MAX_CHUNKS;

                    ref var tile = ref Chunks[cellindex];

                    if (tile == null || tile.X != cellX || tile.Y != cellY)
                    {
                        tile?.Unload();
                        tile = new FacetChunk((ushort)cellX, (ushort)cellY);

                        //if (Chunks[cellindex] == null)
                        //{
                        //    Chunks[cellindex] = new FacetChunk((ushort)i, (ushort)j);
                        //}
                        //else
                        //{
                        //    Chunks[cellindex].Unload();
                        //    Chunks[cellindex].SetTo((ushort)i, (ushort)j);
                        //}


                        tile.Load(Index);
                    }
                }
            }


            //const int XY_OFFSET = 30;

            //int minBlockX = (centerX - XY_OFFSET) / 8 - 1;
            //int minBlockY = (centerY - XY_OFFSET) / 8 - 1;
            //int maxBlockX = (centerX + XY_OFFSET) / 8 + 2;
            //int maxBlockY = (centerY + XY_OFFSET) / 8 + 2;

            //if (minBlockX < 0)
            //    minBlockX = 0;
            //if (minBlockY < 0)
            //    minBlockY = 0;
            //if (maxBlockX >= AssetsLoader.Map.MapBlocksSize[Index][0])
            //    maxBlockX = AssetsLoader.Map.MapBlocksSize[Index][0] - 1;
            //if (maxBlockY >= AssetsLoader.Map.MapBlocksSize[Index][1])
            //    maxBlockY = AssetsLoader.Map.MapBlocksSize[Index][1] - 1;

            //for (int i = minBlockX; i <= maxBlockX; i++)
            //    // int index = i * AssetsLoader.Map.MapBlocksSize[Index][1];

            //for (int j = minBlockY; j <= maxBlockY; j++)
            //{
            //    // int cellindex = index + j; 

            //    int cellindex = (j % MAX_CHUNKS) * MAX_CHUNKS + (i % MAX_CHUNKS);

            //    ref var tile = ref Chunks[cellindex];

            //    if (tile == null || tile.X != i || tile.Y != j)
            //    {
            //        tile?.Unload();
            //        tile = new FacetChunk((ushort)i, (ushort)j);

            //            //if (Chunks[cellindex] == null)
            //            //{
            //            //    Chunks[cellindex] = new FacetChunk((ushort)i, (ushort)j);
            //            //}
            //            //else
            //            //{
            //            //    Chunks[cellindex].Unload();
            //            //    Chunks[cellindex].SetTo((ushort)i, (ushort)j);
            //            //}


            //        tile.Load(Index);
            //    }
            //}
        }
    }
}