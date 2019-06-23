﻿#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
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

using System.Runtime.CompilerServices;

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.IO;
using ClassicUO.IO.Resources;

namespace ClassicUO.Game.Map
{
    internal sealed class Chunk 
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
                {
                    Tile t = new Tile((ushort)(i + x), (ushort)(j + y));
                    Tiles[i, j] = t;
                }
            }

            LastAccessTime = Engine.Ticks + Constants.CLEAR_TEXTURES_DELAY;
        }


        public ushort X { get; private set; }
        public ushort Y { get; private set; }

        public Tile[,] Tiles { get; private set; }

        public long LastAccessTime { get; set; }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Load(int map)
        {
            ref readonly IndexMap im = ref GetIndex(map);

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
                            MinZ = z
                        };

                        ushort tileX = (ushort) (bx + x);
                        ushort tileY = (ushort) (by + y);

                        land.Calculate(tileX, tileY, z);
                        land.Position = new Position(tileX, tileY, z);

                        land.AddToTile(Tiles[x, y]);
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
                            if (sb->Color != 0 && sb->Color != 0xFFFF)
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

                                if (staticObject.ItemData.IsAnimated)
                                    World.AddEffect(new AnimatedItemEffect(staticObject, staticObject.Graphic, staticObject.Hue, -1));
                                else
                                    staticObject.AddToTile(Tiles[x, y]);
                            }
                        }
                    }
                }


                //CreateLand();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void LoadStatics(int map)
        {
            ref readonly IndexMap im = ref GetIndex(map);

            if (im.MapAddress != 0)
            {
                int bx = X * 8;
                int by = Y * 8;

                if (im.StaticAddress != 0)
                {
                    StaticsBlock* sb = (StaticsBlock*) im.StaticAddress;

                    if (sb != null)
                    {
                        int count = (int) im.StaticCount;

                        for (int i = 0; i < count; i++, sb++)
                        {
                            if (sb->Color != 0 && sb->Color != 0xFFFF)
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

                                if (staticObject.ItemData.IsAnimated)
                                    World.AddEffect(new AnimatedItemEffect(staticObject, staticObject.Graphic, staticObject.Hue, -1));
                                else
                                    staticObject.AddToTile(Tiles[x, y]);
                            }
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void LoadLand(int map)
        {
            ref readonly IndexMap im = ref GetIndex(map);

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
                            MinZ = z
                        };

                        
                        ushort tileX = (ushort) (bx + x);
                        ushort tileY = (ushort) (by + y);

                        land.Calculate(tileX, tileY, z);
                        land.Position = new Position(tileX, tileY, z);

                        land.AddToTile(Tiles[x, y]);
                    }
                }
            }
        }

        //private void CreateLand()
        //{
        //    for (int x = 0; x < 8; x++)
        //    {
        //        for (int y = 0; y < 8; y++)
        //        {
        //            Land tile = null;
        //            Tile t = Tiles[x, y];
        //            GameObject obj = t.FirstNode;

        //            while (obj != null)
        //            {
        //                if (obj is Land land)
        //                {
        //                    tile = land;
        //                    break;
        //                }

        //                obj = obj.Right;
        //            }

        //            if (tile != null)
        //            {
        //                int tileX = tile.X;
        //                int tileY = tile.Y;
        //                sbyte tileZ = tile.Z;

        //                tile.Calculate(tileX, tileY, tileZ);

        //                t.AddGameObject(tile);
        //            }

        //        }
        //    }
        //}

        private ref IndexMap GetIndex(int map)
        {
            FileManager.Map.SanitizeMapIndex(ref map);

            return ref FileManager.Map.GetIndex(map, X, Y);
        }

        public void Destroy()
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    GameObject obj = Tiles[i, j].FirstNode;

                    while (obj.Left != null)
                        obj = obj.Left;

                    for (GameObject right = obj.Right; obj != null; obj = right, right = right?.Right)
                    {
                        if (obj != World.Player)
                            obj.Destroy();
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

                    GameObject obj = tile.FirstNode;

                    while (obj.Left != null)
                        obj = obj.Left;

                    for (; obj != null; obj = obj.Right)
                    {
                        if (obj is GameEffect effect)
                        {
                            switch (effect.Source)
                            {
                                case Static _: continue;
                                case Item _: return false;
                                default: continue;
                            }
                        }


                        if (!(obj is Land) && !(obj is Static) /*&& !(obj is Multi)*/)
                            return false;
                    }
                }
            }

            return true;
        }
    }
}