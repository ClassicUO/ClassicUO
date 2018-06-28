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

        public void LoadChunks(ushort centerX, ushort centerY, int distance)
        {
            centerX /= 8; centerY /= 8;

            for (int y = -distance; y <= distance; y++)
            {
                short cellY = (short)((centerY + y) % Assets.Map.MapBlocksSize[Index][1]); 
                for (int x = -distance; x <= distance; x++)
                {
                    short cellX = (short)((centerX + x) % Assets.Map.MapBlocksSize[Index][0]);

                    int cellindex = (cellY % 11) * 11 + (cellX % 11);

                    if (_chunks[cellindex] == null ||
                        _chunks[cellindex].X != cellX ||
                        _chunks[cellindex].Y != cellY)
                    {
                        if (_chunks[cellindex] == null)
                            _chunks[cellindex] = new FacetChunk((ushort)cellX, (ushort)cellY);
                        else
                        {
                            _chunks[cellindex].Unload();
                            _chunks[cellindex].SetTo((ushort)cellX, (ushort)cellY);
                        }

                        _chunks[cellindex].Load(Index);

                    }
                }
            }
        }

    }
}
