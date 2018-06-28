using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game
{
    public sealed class Facet
    {
        private readonly FacetChunk[] _chunks;

        public Facet(in int index)
        {
            Index = index;

            _chunks = new FacetChunk[11 * 11];
        }

        public int Index { get; }


        public void LoadChunks(Position center, in int distance)
            => LoadChunks(center.X, center.Y, distance);

        public void LoadChunks(ushort centerX, ushort centerY, in int distance)
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
            if (maxBlockX >= Assets.Map.MapsDefaultSize[Index][0])
                maxBlockX = Assets.Map.MapsDefaultSize[Index][0] - 1;
            if (maxBlockY >= Assets.Map.MapsDefaultSize[Index][1])
                maxBlockY = Assets.Map.MapsDefaultSize[Index][1] - 1;

            for (int i = minBlockX; i <= maxBlockX; i++)
            {
                ushort cellY = (ushort)(i % Assets.Map.MapBlocksSize[Index][1]);
                for (int j = minBlockY; j <= maxBlockY; j++)
                {
                    ushort cellX = (ushort)(j % Assets.Map.MapBlocksSize[Index][0]);

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
            }

           /* centerX /= 8; centerY /= 8;

            for (int y = -distance; y <= distance; y++)
            {
                ushort cellY = (ushort)((centerY + y) % Assets.Map.MapBlocksSize[Index][1]); 
                for (int x = -distance; x <= distance; x++)
                {
                    ushort cellX = (ushort)((centerX + x) % Assets.Map.MapBlocksSize[Index][0]);

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
