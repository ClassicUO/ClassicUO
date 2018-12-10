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
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using ClassicUO.Game.GameObjects;
using ClassicUO.IO.Resources;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Map
{
    public sealed class Chunk : IDisposable
    {
        public Chunk(ushort x, ushort y)
        {
            X = x;
            Y = y;
            Tiles = new Tile[8, 8];

            x *= 8;
            y *= 8;

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                    Tiles[i, j] = new Tile((ushort) (i + x), (ushort) (j + y));
            }

            LastAccessTime = Engine.Ticks + Constants.CLEAR_TEXTURES_DELAY;
        }


        public ushort X { get; }
        public ushort Y { get; }

        public Tile[,] Tiles { get; private set; }

        public long LastAccessTime { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Load(int map)
        {
            IndexMap im = GetIndex(map);

            if (im.MapAddress != 0)
            {
                MapBlock* block = (MapBlock*) im.MapAddress;
                MapCells* cells = (MapCells*) &block->Cells;
                int bx = X * 8;
                int by = Y * 8;

                for (int x = 0; x < 8; x++)
                {
                    for (int y = 0; y < 8; y++)
                    {
                        int pos = y * 8 + x;
                        ushort tileID = (ushort) (cells[pos].TileID & 0x3FFF);
                        sbyte z = cells[pos].Z;

                        Land land = new Land(tileID)
                        {
                            Graphic = tileID,
                            AverageZ = z,
                            MinZ = z,
                        };

                        ushort tileX = (ushort) (bx + x);
                        ushort tileY = (ushort) (by + y);
                        land.Calculate(tileX, tileY, z);
                        land.Position = new Position(tileX, tileY, z);

                        Tiles[x, y].AddGameObject(land);
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

                                ushort staticX = (ushort) (bx + x);
                                ushort staticY = (ushort) (by + y);

                                Static staticObject = new Static(sb->Color, sb->Hue, pos)
                                {
                                    Position = new Position(staticX, staticY, z)
                                };                  

                                if (TileData.IsAnimated(staticObject.ItemData.Flags))
                                    World.AddEffect(new AnimatedItemEffect(staticObject, staticObject.Graphic, staticObject.Hue, -1));
                                else
                                    Tiles[x, y].AddGameObject(staticObject);
                            }
                        }
                    }
                }
            }
        }

        private IndexMap GetIndex(int map) => GetIndex(map, X, Y);

        private static IndexMap GetIndex(int map, int x, int y) => IO.Resources.Map.GetIndex(map, x, y);

        public void Dispose()
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    Tile tile = Tiles[i, j];

                    for (GameObject obj = tile.FirstNode; obj != null; obj = obj.Right)
                    {
                        if (obj != World.Player)
                        {
                           tile.RemoveGameObject(obj);
                        }
                    }

                    Tiles[i, j] = null;
                }
            }

            Tiles = null;
        }

        public bool HasNoExternalData()
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    Tile tile = Tiles[i, j];

                    for (GameObject obj = tile.FirstNode; obj != null; obj = obj.Right)
                    {
                        if (!(obj is Land) && !(obj is Static))
                            return false;
                    }
                }
            }

            return true;
        }
    }
}