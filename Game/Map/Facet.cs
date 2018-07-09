using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game.Map
{
    public sealed class Facet
    {
        private readonly FacetChunk[] _chunks;

        public Facet(in int index)
        {
            Index = index;

            _chunks = new FacetChunk[AssetsLoader.Map.MapBlocksSize[Index][0] * AssetsLoader.Map.MapBlocksSize[Index][1]];
        }

        public int Index { get; }
        public FacetChunk[] Chunks => _chunks;


        public Tile GetTile(in short x, in short y)
        {
            int cellX = x / 8;
            int cellY = y / 8;
            int cellindex = (cellX * AssetsLoader.Map.MapBlocksSize[Index][1]) + cellY;

            FacetChunk cell = _chunks[cellindex];
            if (cell == null)
            {
                cell = new FacetChunk((ushort)cellX, (ushort)cellY);
                cell.Load(Index);
                _chunks[cellindex] = cell;
            }
            if (cell.X != cellX || cell.Y != cellY)
                return null;

            return cell.Tiles[ (y % 8) * 8 + (x % 8) ];
        }

        public float GetTileZ(in short x, in short y)
        {
            var tile = GetTile(x, y);

            return tile.Position.Z;
        }

        public int GetAverageZ(in int top, in int left, in int right, in int bottom, ref int low, ref int high)
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
            else
                return FloorAverage(top, bottom);
        }

        public int GetAverageZ(short x, short y, ref int low, ref int top)
        {
            return GetAverageZ(
                (int)GetTileZ(x, y),
                (int)GetTileZ(x, (short)(y + 1)),
                (int)GetTileZ((short)(x + 1), y),
                (int)GetTileZ((short)(x + 1), (short)(y + 1)),
                ref low, ref top);
        }

        private static int FloorAverage(in int a, in int b)
        {
            int v = a + b;

            if (v < 0)
                --v;

            return (v / 2);
        }

        public void LoadChunks(in Position center, in int distance)
            => LoadChunks(center.X, center.Y, distance);

        public void LoadChunks(in ushort centerX, in ushort centerY, in int distance)
        {
            const int XY_OFFSET = 30;

            int minBlockX = ((centerX - XY_OFFSET) / 8 - 1);
            int minBlockY = ((centerY - XY_OFFSET) / 8 - 1);
            int maxBlockX = (((centerX + XY_OFFSET) / 8) + 1);
            int maxBlockY = (((centerY + XY_OFFSET) / 8) + 1);

            if (minBlockX < 0)
                minBlockX = 0;
            if (minBlockY < 0)
                minBlockY = 0;
            if (maxBlockX >= AssetsLoader.Map.MapBlocksSize[Index][0])
                maxBlockX = (AssetsLoader.Map.MapBlocksSize[Index][0] - 1);
            if (maxBlockY >= AssetsLoader.Map.MapBlocksSize[Index][1])
                maxBlockY = (AssetsLoader.Map.MapBlocksSize[Index][1] - 1);

            for (int i = minBlockX; i <= maxBlockX; i++)
            {
                //ushort cellX = (ushort)(i % AssetsLoader.Map.MapBlocksSize[Index][0]);

                int index = i * AssetsLoader.Map.MapBlocksSize[Index][1];

                for (int j = minBlockY; j <= maxBlockY; j++)
                {
                    //ushort cellY = (ushort)(j % AssetsLoader.Map.MapBlocksSize[Index][1]);

                    int cellindex = index + j; // (j % 8) * 8 + (i % 8);

                    if (_chunks[cellindex] == null ||
                        _chunks[cellindex].X != i ||
                        _chunks[cellindex].Y != j)
                    {

                        //if (_chunks[cellindex] != null)
                        //    _chunks[cellindex].Unload();

                        //_chunks[cellindex] = new FacetChunk((ushort)i, (ushort)j);


                        if (_chunks[cellindex] == null)
                            _chunks[cellindex] = new FacetChunk((ushort)i, (ushort)j);
                        else
                        {
                            _chunks[cellindex].Unload();
                            _chunks[cellindex].SetTo((ushort)i, (ushort)j);
                        }

                        _chunks[cellindex].Load(Index);

                    }
                }
            }

            /*centerX /= 8; centerY /= 8;

            for (int y = -distance; y <= distance; y++)
            {
                ushort cellY = (ushort)((centerY + y) % AssetsLoader.Map.MapBlocksSize[Index][1]); 
                for (int x = -distance; x <= distance; x++)
                {
                    ushort cellX = (ushort)((centerX + x) % AssetsLoader.Map.MapBlocksSize[Index][0]);

                    int cellindex = (cellY % 11) * 11 + (cellX % 11);
                    if (_chunks[cellindex] == null ||
                        _chunks[cellindex].X != cellX ||
                        _chunks[cellindex].Y != cellY)
                    {
                        if (_chunks[cellindex] == null)
                            _chunks[cellindex] = new FacetChunk(cellX, cellY);
                        else
                        {
                            _chunks[cellindex].Unload();
                            _chunks[cellindex].SetTo(cellX, cellY);
                        }

                        _chunks[cellindex].Load(Index);

                    }
                }
            }*/
        }

    }
}
