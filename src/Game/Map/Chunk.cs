#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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

using System.Collections.Generic;
using System.Runtime.CompilerServices;

using ClassicUO.Game.GameObjects;
using ClassicUO.IO.Resources;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Map
{
    internal sealed class Chunk
    {
        private static readonly Queue<Chunk> _pool = new Queue<Chunk>();

        static Chunk()
        {
            //for (int i = 0; i < Constants.PREDICTABLE_CHUNKS; i++)
            //    _pool.Enqueue(new Chunk(0xFFFF, 0xFFFF));
        }

        public static Chunk Create(ushort x, ushort y)
        {
            Chunk c;

            if (_pool.Count != 0)
            {
                c = _pool.Dequeue();
                c.X = x;
                c.Y = y;
                c.LastAccessTime = Time.Ticks + Constants.CLEAR_TEXTURES_DELAY;
            }
            else
            {
                Log.Debug(string.Intern("Created new Chunk"));

                c = new Chunk(x, y);
            }

            x <<= 3;
            y <<= 3;

            for (int i = 0; i < 8; i++)
            {
                ushort tileX = (ushort) (i + x);

                for (int j = 0; j < 8; j++)
                {
                    c.Tiles[i, j] = Tile.Create(tileX, (ushort) (j + y));
                }
            }

            return c;
        }



        private Chunk(ushort x, ushort y)
        {
            X = x;
            Y = y;
            Tiles = new Tile[8, 8];
            LastAccessTime = Time.Ticks + Constants.CLEAR_TEXTURES_DELAY;
        }

        public ushort X { get; set; }
        public ushort Y { get; set; }

        public Tile[,] Tiles { get; private set; }

        public long LastAccessTime { get; set; }


        [MethodImpl(256)]
        public unsafe void Load(int map)
        {
            ref IndexMap im = ref GetIndex(map);

            if (im.MapAddress != 0)
            {
                MapBlock* block = (MapBlock*) im.MapAddress;
                MapCells* cells = (MapCells*) &block->Cells;
                int bx = X << 3;
                int by = Y << 3;

                for (int x = 0; x < 8; x++)
                {
                    ushort tileX = (ushort) (bx + x);

                    for (int y = 0; y < 8; y++)
                    {
                        int pos = (y << 3) + x;
                        ushort tileID = (ushort) (cells[pos].TileID & 0x3FFF);
                        sbyte z = cells[pos].Z;

                        Land land = Land.Create(tileID);
                        land.AverageZ = z;
                        land.MinZ = z;

                        ushort tileY = (ushort) (by + y);

                        land.ApplyStrech(tileX, tileY, z);
                        land.X = tileX;
                        land.Y = tileY;
                        land.Z = z;
                        land.UpdateScreenPosition();
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
                                int pos = (y << 3) + x;

                                if (pos >= 64)
                                    continue;

                                sbyte z = sb->Z;

                                ushort staticX = (ushort) (bx + x);
                                ushort staticY = (ushort) (by + y);

                                Static staticObject = Static.Create(sb->Color, sb->Hue, pos);
                                staticObject.X = staticX;
                                staticObject.Y = staticY;
                                staticObject.Z = z;
                                staticObject.UpdateScreenPosition();
                                staticObject.AddToTile(Tiles[x, y]);
                            }
                        }
                    }
                }
            }
        }

        [MethodImpl(256)]
        public unsafe void LoadStatics(int map)
        {
            ref IndexMap im = ref GetIndex(map);

            if (im.MapAddress != 0)
            {
                int bx = X << 3;
                int by = Y << 3;

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
                                int pos = (y << 3) + x;

                                if (pos >= 64)
                                    continue;

                                sbyte z = sb->Z;

                                ushort staticX = (ushort) (bx + x);
                                ushort staticY = (ushort) (by + y);

                                Static staticObject = Static.Create(sb->Color, sb->Hue, pos);
                                staticObject.X = staticX;
                                staticObject.Y = staticY;
                                staticObject.Z = z;
                                staticObject.UpdateScreenPosition();
                                staticObject.AddToTile(Tiles[x, y]);
                            }
                        }
                    }
                }
            }
        }

        [MethodImpl(256)]
        public unsafe void LoadLand(int map)
        {
            ref IndexMap im = ref GetIndex(map);

            if (im.MapAddress != 0)
            {
                MapBlock* block = (MapBlock*) im.MapAddress;
                MapCells* cells = (MapCells*) &block->Cells;
                int bx = X << 3;
                int by = Y << 3;

                for (int x = 0; x < 8; x++)
                {
                    ushort tileX = (ushort) (bx + x);

                    for (int y = 0; y < 8; y++)
                    {
                        int pos = (y << 3) + x;
                        ushort tileID = (ushort) (cells[pos].TileID & 0x3FFF);
                        sbyte z = cells[pos].Z;

                        Land land = Land.Create(tileID);
                        land.AverageZ = z;
                        land.MinZ = z;

                        ushort tileY = (ushort) (by + y);

                        land.ApplyStrech(tileX, tileY, z);
                        land.X = tileX;
                        land.Y = tileY;
                        land.Z = z;
                        land.UpdateScreenPosition();
                        land.AddToTile(Tiles[x, y]);
                    }
                }
            }
        }

        private ref IndexMap GetIndex(int map)
        {
            MapLoader.Instance.SanitizeMapIndex(ref map);

            return ref MapLoader.Instance.GetIndex(map, X, Y);
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

                    Tiles[i, j].Destroy();
                    Tiles[i, j] = null;
                }
            }

            _pool.Enqueue(this);
            //Tiles = null;
        }

        public void Clear()
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    GameObject obj = Tiles[i, j].FirstNode;

                    if (obj == null)
                        return;

                    while (obj.Left != null)
                        obj = obj.Left;

                    for (GameObject right = obj.Right; obj != null; obj = right, right = right?.Right)
                    {
                        if (obj != World.Player)
                            obj.Destroy();
                    }

                    Tiles[i, j].Destroy();
                }
            }
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
                        if (!(obj is Land) && !(obj is Static) /*&& !(obj is Multi)*/)
                            return false;
                    }
                }
            }

            return true;
        }
    }
}