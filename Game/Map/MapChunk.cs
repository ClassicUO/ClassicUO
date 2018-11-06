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
using System.ComponentModel;
using System.Runtime.InteropServices;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.GameObjects.Managers;
using ClassicUO.IO.Resources;

namespace ClassicUO.Game.Map
{
    public struct MapChunk
    {
        public static readonly MapChunk Invalid = new MapChunk(0xFFFF, 0xFFFF);

        public MapChunk(ushort x, ushort y)
        {
            _x = x;
            _y = y;

            Tiles = new Tile[8][];

            for (int i = 0; i < 8; i++)
            {
                Tiles[i] = new Tile[8];
                for (int j = 0; j < 8; j++)
                {
                    Tiles[i][j] = new Tile( (ushort) (i + x * 8), (ushort)(j + y * 8));
                }
            }

            LastAccessTime = CoreGame.Ticks;
        }

        private ushort? _x, _y;

        public ushort X
        {
            get => _x ?? 0xFFFF;
            set => _x = value;
        }
        public ushort Y
        {
            get => _y ?? 0xFFFF;
            set => _y = value;
        }

        public Tile[][] Tiles { get; }

        public long LastAccessTime { get; set; }

        public static bool operator ==(MapChunk p1, MapChunk p2)
        {
            return p1.X == p2.X && p1.Y == p2.Y;
        }

        public static bool operator !=(MapChunk p1, MapChunk p2)
        {
            return p1.X != p2.X || p1.Y != p2.Y;
        }
        public override int GetHashCode()
        {
            return X ^ Y;
        }

        public override bool Equals(object obj)
        {
            return obj is MapChunk mapChunk && this == mapChunk;
        }

        public unsafe void Load(int map)
        {
            IndexMap im = GetIndex(map);

            if (im.MapAddress != 0)
            {
                MapBlock* block = (MapBlock*)im.MapAddress;
                MapCells* cells = (MapCells*)&block->Cells;

                int bx = X * 8;
                int by = Y * 8;

                for (int x = 0; x < 8; x++)
                {
                    for (int y = 0; y < 8; y++)
                    {
                        int pos = y * 8 + x;
                        ushort tileID = (ushort) (cells[pos].TileID & 0x3FFF);
                        sbyte z = cells[pos].Z;

                        LandTiles info = TileData.LandData[tileID];

                        Land land = new Land(tileID)
                        {
                            Graphic = tileID,
                            AverageZ = z,
                            MinZ = z,
                            IsStretched = info.TexID == 0 && TileData.IsWet((long) info.Flags),
                            Position = new Position((ushort) (bx + x), (ushort) (by + y), z)
                        };
                        land.Calculate();
                    }
                }

                if (im.StaticAddress != 0)
                {
                    StaticsBlock* sb = (StaticsBlock*) im.StaticAddress;

                    if (sb != null)
                    {
                        int count = (int) im.StaticCount;

                        for (int i = 0; i < count; i++, sb++)
                        {
                            if (sb->Color > 0 && sb->Color != 0xFFFF)
                            {
                                ushort x = sb->X;
                                ushort y = sb->Y;
                                int pos = y * 8 + x;

                                if (pos >= 64)
                                    continue;
                                sbyte z = sb->Z;

                                Static staticObject = new Static(sb->Color, sb->Hue, i)
                                {
                                    Position = new Position((ushort) (bx + x), (ushort) (by + y), z)
                                };

                                if (TileData.IsAnimated((long)staticObject.ItemData.Flags))
                                    staticObject.Effect = new AnimatedItemEffect(staticObject, staticObject.Graphic, staticObject.Hue, -1);

                                //Tiles[x][y].AddGameObject(staticObject);
                            }
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
            {
                for (int j = 0; j < 8; j++)
                {
                    Tiles[i][j].Dispose();
                    Tiles[i][j] = Tile.Invalid;
                }
            }

        }

        public bool HasNoExternalData()
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    Tile tile = Tiles[i][j];

                    foreach (GameObject o in tile.ObjectsOnTiles)
                    {
                        if (!(o is Land) && !(o is Static))
                            return false;
                    }
                }
            }

            return true;
        }
    }
}