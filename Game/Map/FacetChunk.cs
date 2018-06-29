using ClassicUO.Game.WorldObjects;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ClassicUO.Game.Map
{
    public sealed class FacetChunk
    {
        public FacetChunk(in Position location) : this(location.X, location.Y)
        {

        }

        public FacetChunk(in ushort x, in ushort y)
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
                AssetsLoader.MapBlock block = Marshal.PtrToStructure<AssetsLoader.MapBlock>((IntPtr)im.MapAddress);

                int bx = X * 8;
                int by = Y * 8;

                for (int x = 0; x < 8; x++)
                {
                    for (int y = 0; y < 8; y++)
                    {
                        int pos = y * 8 + x;

                        ushort tileID = (ushort)(block.Cells[pos].TileID & 0x3FFF);
                        sbyte z = block.Cells[pos].Z;

                        Tiles[pos].TileID = tileID;
                        Tiles[pos].Location = new Position((ushort)(bx + x), (ushort)(by + y), z);
                    }
                }

                AssetsLoader.StaticsBlock* sb = (AssetsLoader.StaticsBlock*)im.StaticAddress;
                if (sb != null)
                {
                    int count = (int)im.StaticCount;

                    for (int i = 0; i < count; i++, sb++)
                    {
                        if (sb->Color > 0 && sb->Color != 0xFFFF)
                        {
                            ushort x = sb->X;
                            ushort y = sb->Y;

                            int pos = (y * 8) + x;
                            if (pos >= 64)
                                continue;

                            sbyte z = sb->Z;

                            StaticObject staticObject = new StaticObject(sb->Color, sb->Hue, i)
                            {
                                Position = new Position((ushort)(bx + x), (ushort)(by + y), z)
                            };

                            Tiles[pos].AddWorldObject(staticObject);
                        }
                    }
                }
            }
        }

        private AssetsLoader.IndexMap GetIndex(in int map)
        {
            uint block = (uint)(X * AssetsLoader.Map.MapBlocksSize[map][1]) + Y;
            return AssetsLoader.Map.BlockData[map][block];
        }

        // we wants to avoid reallocation, so use a reset method
        public void SetTo(in ushort x, in ushort y)
        {
            X = x; Y = y;
        }

        public void Unload()
        {
            for (int i = 0; i < Tiles.Length; i++)
            {
                Tiles[i].Clear();
            }
        }

    }
}
