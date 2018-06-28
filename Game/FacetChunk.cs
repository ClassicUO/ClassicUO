using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game
{
    public sealed class FacetChunk
    {
        public FacetChunk(Position location) : this(location.X, location.Y)
        {

        }

        public FacetChunk(ushort x, ushort y)
        {
            X = x; Y = y;

            Tiles = new Tile[64];
            for (int i = 0; i < Tiles.Length; i++)
                Tiles[i] = new Tile();
        }

        public ushort X { get; private set; }
        public ushort Y { get; private set; }
        public Tile[] Tiles { get; private set; }


        public void Load(in int index)
        {
            var im = GetIndex(index);
            if (im.MapAddress == 0)
                throw new Exception();

            unsafe
            {
                Assets.MapBlock* block = (Assets.MapBlock*)im.MapAddress;

                int bx = X * 8;
                int by = Y * 8;

                for (int x = 0; x < 8; x++)
                {
                    for (int y = 0; y < 8; y++)
                    {
                        int pos = y * 8 + x;
                        var pp = block->Cells[pos].TileID & 0x3FFF;

                        Tiles[pos].Location.Set((ushort)(bx + x), (ushort)(by + y));
                    }
                }

                Assets.StaticsBlock* sb = (Assets.StaticsBlock*)im.StaticAddress;
                if (sb != null)
                {
                    int count = (int)im.StaticCount;

                    for (int i = 0; i < count; i++, sb++)
                    {
                        if (sb->Color > 0 && sb->Color != 0xFFFF)
                        {
                            int x = sb->X;
                            int y = sb->Y;

                            int pos = (y * 8) + x;
                            if (pos >= 64)
                                continue;


                        }
                    }
                }
            }
        }

        private Assets.IndexMap GetIndex(in int map)
        {
            uint block = (uint)(X * Assets.Map.MapBlocksSize[map][1]) + Y;
            return Assets.Map.BlockData[map][block];
        }

        // we wants to avoid reallocation, so use a reset method
        public void SetTo(ushort x, ushort y)
        {
            X = x; Y = y;
        }

        public void Unload()
        {
            for (int i = 0; i < Tiles.Length; i++)
                Tiles[i].Clear();        
            
        }

    }
}
