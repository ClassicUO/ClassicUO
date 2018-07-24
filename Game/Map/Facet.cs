using System;
using System.Runtime.InteropServices;
using ClassicUO.AssetsLoader;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Map
{
    public sealed class Facet
    {
        private const int MAX_CHUNKS = 11;

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
                    LoadChunks((ushort) _center.X, (ushort) _center.Y);
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
                return null;

            if (Chunks[cellindex] == null || Chunks[cellindex].X != cellX || Chunks[cellindex].Y != cellY) return null;

            return Chunks[cellindex].Tiles[x % 8][y % 8];
        }


        public Tile GetTile(int x, int y) => GetTile((short)x, (short)y);



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
                    return -125;

                IndexMap blockIndex = GetIndex(x / 8, y / 8);
                if (blockIndex.MapAddress == 0)
                    return -125;

                int mx = x % 8;
                int my = y % 8;

                return Marshal.PtrToStructure<MapBlock>((IntPtr) blockIndex.MapAddress).Cells[my * 8 + mx].Z;
            }

            return tile.Position.Z;
        }

        private IndexMap GetIndex(in int blockX, in int blockY)
        {
            int block = blockX * AssetsLoader.Map.MapBlocksSize[Index][1] + blockY;
            return AssetsLoader.Map.BlockData[Index][block];
        }

        public int GetAverageZ(in sbyte top, in sbyte left, in sbyte right, in sbyte bottom, ref sbyte low,
            ref sbyte high)
        {
            high = top;
            if (left > high)
                high = left;
            if (right > high)
                high = right;
            if (bottom > high)
                high = bottom;

            low = high;
            if (left < low)
                low = left;
            if (right < low)
                low = right;
            if (bottom < low)
                low = bottom;

            if (Math.Abs(top - bottom) > Math.Abs(left - right))
                return FloorAverage(left, right);
            return FloorAverage(top, bottom);
        }

        public int GetAverageZ(short x, short y, ref sbyte low, ref sbyte top)
        {
            return GetAverageZ(
                GetTileZ(x, y),
                GetTileZ(x, (short) (y + 1)),
                GetTileZ((short) (x + 1), y),
                GetTileZ((short) (x + 1), (short) (y + 1)),
                ref low, ref top);
        }

        private static int FloorAverage(in int a, in int b)
        {
            int v = a + b;

            if (v < 0)
                --v;

            return v / 2;
        }

        private void LoadChunks(in ushort centerX, in ushort centerY)
        {
            const int XY_OFFSET = 30;

            int minBlockX = (centerX - XY_OFFSET) / 8 - 1;
            int minBlockY = (centerY - XY_OFFSET) / 8 - 1;
            int maxBlockX = (centerX + XY_OFFSET) / 8 + 2;
            int maxBlockY = (centerY + XY_OFFSET) / 8 + 2;

            if (minBlockX < 0)
                minBlockX = 0;
            if (minBlockY < 0)
                minBlockY = 0;
            if (maxBlockX >= AssetsLoader.Map.MapBlocksSize[Index][0])
                maxBlockX = AssetsLoader.Map.MapBlocksSize[Index][0] - 1;
            if (maxBlockY >= AssetsLoader.Map.MapBlocksSize[Index][1])
                maxBlockY = AssetsLoader.Map.MapBlocksSize[Index][1] - 1;

            for (int i = minBlockX; i <= maxBlockX; i++)
                // int index = i * AssetsLoader.Map.MapBlocksSize[Index][1];

            for (int j = minBlockY; j <= maxBlockY; j++)
            {
                // int cellindex = index + j; 

                int cellindex = j % MAX_CHUNKS * MAX_CHUNKS + i % MAX_CHUNKS;

                if (Chunks[cellindex] == null ||
                    Chunks[cellindex].X != i ||
                    Chunks[cellindex].Y != j)
                {
                    if (Chunks[cellindex] == null)
                    {
                        Chunks[cellindex] = new FacetChunk((ushort) i, (ushort) j);
                    }
                    else
                    {
                        Chunks[cellindex].Unload();
                        Chunks[cellindex].SetTo((ushort) i, (ushort) j);
                    }

                    Chunks[cellindex].Load(Index);
                }
            }
        }
    }
}