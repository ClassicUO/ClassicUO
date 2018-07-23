using System;
using System.Runtime.InteropServices;
using ClassicUO.AssetsLoader;
using ClassicUO.Game.WorldObjects;

namespace ClassicUO.Game.Map
{
    public sealed class FacetChunk
    {
        public FacetChunk(in Position location) : this(location.X, location.Y)
        {
        }

        public FacetChunk(in ushort x, in ushort y)
        {
            X = x;
            Y = y;

            Tiles = new Tile[8][];
            for (var i = 0; i < 8; i++)
            {
                Tiles[i] = new Tile[8];
                for (var j = 0; j < 8; j++)
                    Tiles[i][j] = new Tile();
            }
        }

        public ushort X { get; private set; }
        public ushort Y { get; private set; }
        public Tile[][] Tiles { get; }


        public void Load(in int map)
        {
            var im = GetIndex(map);
            if (im.MapAddress == 0)
                throw new Exception();

            unsafe
            {
                var block = Marshal.PtrToStructure<MapBlock>((IntPtr) im.MapAddress);

                var bx = X * 8;
                var by = Y * 8;

                for (var x = 0; x < 8; x++)
                for (var y = 0; y < 8; y++)
                {
                    var pos = y * 8 + x;

                    var tileID = (ushort) (block.Cells[pos].TileID & 0x3FFF);
                    var z = block.Cells[pos].Z;

                    Tiles[x][y].Graphic = tileID;
                    Tiles[x][y].Position = new Position((ushort) (bx + x), (ushort) (by + y), z);
                }

                var sb = (StaticsBlock*) im.StaticAddress;
                if (sb != null)
                {
                    var count = (int) im.StaticCount;

                    for (var i = 0; i < count; i++, sb++)
                        if (sb->Color > 0 && sb->Color != 0xFFFF)
                        {
                            ushort x = sb->X;
                            ushort y = sb->Y;

                            var pos = y * 8 + x;
                            if (pos >= 64)
                                continue;

                            var z = sb->Z;

                            var staticObject = new Static(sb->Color, sb->Hue, pos)
                            {
                                Position = new Position((ushort) (bx + x), (ushort) (by + y), z)
                            };

                            Tiles[x][y].AddWorldObject(staticObject);
                        }
                }
            }
        }

        /*public float GetTileZ(in int map, in short x, in short y)
        {
            if (x < 0 || y < 0)
                return -125;

            var blockIndex = GetIndex(map, x / 8, y / 8);
            if (blockIndex.MapAddress == 0)
                return -125;

            int mx = x % 8;
            int my = y % 8;

            return Marshal.PtrToStructure<AssetsLoader.MapBlock>((IntPtr)blockIndex.MapAddress).Cells[my * 8 + mx].Z;
        }*/

        private IndexMap GetIndex(in int map)
        {
            return GetIndex(map, X, Y);
        }

        private IndexMap GetIndex(in int map, in int x, in int y)
        {
            var block = x * AssetsLoader.Map.MapBlocksSize[map][1] + y;
            return AssetsLoader.Map.BlockData[map][block];
        }

        // we wants to avoid reallocation, so use a reset method
        public void SetTo(in ushort x, in ushort y)
        {
            X = x;
            Y = y;
        }

        public void Unload()
        {
            for (var i = 0; i < 8; i++)
            for (var j = 0; j < 8; j++)
                Tiles[i][j].Clear();
        }
    }
}