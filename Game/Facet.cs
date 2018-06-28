using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game
{
    public sealed class Facet
    {
        private readonly FacetChunk[,] _chunks;

        public Facet(in int index)
        {
            Index = index;

            _chunks = new FacetChunk[8,8];
        }

        public int Index { get; }


        public void LoadChunks(Position center, in int distance)
            => LoadChunks(center.X, center.Y, distance);

        public void LoadChunks(ushort centerX, ushort centerY, int distance)
        {
            centerX /= 8; centerY /= 8;

            for (int y = -distance; y <= distance; y++)
            {
                ushort cellY = (ushort)((centerY + y) & Assets.Map.MapBlocksSize[Index][1]); 
                for (int x = -distance; x <= distance; x++)
                {
                    ushort cellX = (ushort)((centerX + x) & Assets.Map.MapBlocksSize[Index][0]);

                    if (_chunks[cellX, cellY] == null ||
                        _chunks[cellX, cellY].X != cellX ||
                        _chunks[cellX, cellY].Y != cellY)
                    {
                        if (_chunks[cellX, cellY] == null)
                            _chunks[cellX, cellY] = new FacetChunk(cellX, cellY);
                        else
                        {
                            _chunks[cellX, cellY].Unload();
                            _chunks[cellX, cellY].SetTo(cellX, cellY);
                        }

                        _chunks[cellX, cellY].Load(Index);

                    }
                }
            }
        }
    }
}
