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
using System.Runtime.InteropServices;

using ClassicUO.Game.GameObjects;
using ClassicUO.IO.Resources;

namespace ClassicUO.Game.Map
{
    public sealed class MapChunk
    {
        public MapChunk(ushort x, ushort y)
        {
            X = x;
            Y = y;
            Tiles = new Tile[8][];

            for (int i = 0; i < 8; i++)
            {
                Tiles[i] = new Tile[8];
                for (int j = 0; j < 8; j++) Tiles[i][j] = new Tile();
            }

            LastAccessTime = CoreGame.Ticks;
        }

        public ushort X { get; }

        public ushort Y { get; }

        public Tile[][] Tiles { get; }

        public long LastAccessTime { get; set; }

        public unsafe void Load(int map)
        {
            IndexMap im = GetIndex(map);

            if (im.MapAddress != 0)
            {
                MapBlock block = Marshal.PtrToStructure<MapBlock>((IntPtr) im.MapAddress);
                int bx = X * 8;
                int by = Y * 8;

                for (int x = 0; x < 8; x++)
                    for (int y = 0; y < 8; y++)
                    {
                        int pos = y * 8 + x;
                        ushort tileID = (ushort) (block.Cells[pos].TileID & 0x3FFF);
                        sbyte z = block.Cells[pos].Z;
                        Tiles[x][y].Graphic = tileID;
                        Tiles[x][y].Position = new Position((ushort) (bx + x), (ushort) (by + y), z);
                        Tiles[x][y].AverageZ = z;
                        Tiles[x][y].MinZ = z;
                        Tiles[x][y].AddGameObject(Tiles[x][y]);
                    }

                if (im.StaticAddress != 0)
                {
                    StaticsBlock* sb = (StaticsBlock*) im.StaticAddress;

                    if (sb != null)
                    {
                        int count = (int) im.StaticCount;

                        for (int i = 0; i < count; i++, sb++)
                            if (sb->Color > 0 && sb->Color != 0xFFFF)
                            {
                                ushort x = sb->X;
                                ushort y = sb->Y;
                                int pos = y * 8 + x;

                                if (pos >= 64)
                                    continue;
                                sbyte z = sb->Z;
                                Static staticObject = new Static(sb->Color, sb->Hue, pos) {Position = new Position((ushort) (bx + x), (ushort) (by + y), z)};
                                Tiles[x][y].AddGameObject(staticObject);
                            }
                    }
                }
            }
        }

        private IndexMap GetIndex(int map)
        {
            return GetIndex(map, X, Y);
        }

        private static IndexMap GetIndex(int map, int x, int y)
        {
            return IO.Resources.Map.GetIndex(map, x, y);
        }

        public void Unload()
        {
            for (int i = 0; i < 8; i++)
                for (int j = 0; j < 8; j++)
                {
                    Tiles[i][j].Dispose();
                    Tiles[i][j] = null;
                }
        }

        public bool HasNoExternalData()
        {
            for (int i = 0; i < 8; i++)
                for (int j = 0; j < 8; j++)
                {
                    ref Tile tile = ref Tiles[i][j];

                    foreach (GameObject o in tile.ObjectsOnTiles)
                        if (!(o is Tile) && !(o is Static))
                            return false;
                }

            return true;
        }
    }
}