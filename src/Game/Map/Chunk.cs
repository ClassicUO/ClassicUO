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
using ClassicUO.Game.Managers;
using ClassicUO.IO.Resources;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Map
{
    internal sealed class Chunk
    {
        private static readonly Queue<Chunk> _pool = new Queue<Chunk>();

        static Chunk()
        {
            for (int i = 0; i < Constants.PREDICTABLE_CHUNKS; i++)
                _pool.Enqueue(new Chunk(0xFFFF, 0xFFFF));
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
                c.IsDestroyed = false;
            }
            else
            {
                Log.Debug(string.Intern("Created new Chunk"));

                c = new Chunk(x, y);
            }

            return c;
        }



        private Chunk(ushort x, ushort y)
        {
            X = x;
            Y = y;
            Tiles = new GameObject[8, 8];
            LastAccessTime = Time.Ticks + Constants.CLEAR_TEXTURES_DELAY;
        }

        public ushort X { get; set; }
        public ushort Y { get; set; }

        public bool IsDestroyed;

        public GameObject[,] Tiles { get; }

        public long LastAccessTime { get; set; }

        public LinkedListNode<int> Node;


        [MethodImpl(256)]
        public unsafe void Load(int map)
        {
            IsDestroyed = false;

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
                            
                        AddGameObject(land, x, y);
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

                                AddGameObject(staticObject, x, y);
                            }
                        }
                    }
                }
            }
        }


        private ref IndexMap GetIndex(int map)
        {
            MapLoader.Instance.SanitizeMapIndex(ref map);

            return ref MapLoader.Instance.GetIndex(map, X, Y);
        }

        public GameObject GetHeadObject(int x, int y)
        {
            var obj = Tiles[x, y];

            while (obj?.TPrevious != null)
                obj = obj.TPrevious;

            return obj;
        }

        public void AddGameObject(GameObject obj, int x, int y)
        {
            obj.RemoveFromTile();

            short priorityZ = obj.Z;
            sbyte state = -1;

            ushort graphic = obj.Graphic;

            switch (obj)
            {
                case Land tile:

                    if (tile.IsStretched)
                        priorityZ = (short) (tile.AverageZ - 1);
                    else
                        priorityZ--;

                    state = 0;

                    break;

                case Mobile _:
                    priorityZ++;

                    break;

                case Item item:

                    if (item.IsCorpse)
                    {
                        priorityZ++;
                        break;
                    }
                    else if (item.IsMulti)
                    {
                        graphic = item.MultiGraphic;
                    }

                    goto default;

                case GameEffect _:
                    priorityZ += 2;

                    break;

                case Multi m:

                    state = 1;

                    if ((m.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL) != 0)
                    {
                        priorityZ--;
                        break;
                    }

                    if ((m.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_PREVIEW) != 0)
                    {
                        state = 2;
                        priorityZ++;
                    }

                    if (m.IsMovable)
                        priorityZ++;

                    goto default;

                default:
                    ref StaticTiles data = ref TileDataLoader.Instance.StaticData[graphic];

                    if (data.IsBackground)
                        priorityZ--;

                    if (data.Height != 0)
                        priorityZ++;

                    if (data.IsMultiMovable)
                        priorityZ++;

                    break;
            }

            obj.PriorityZ = priorityZ;

            if (Tiles[x, y] == null)
            {
                Tiles[x, y] = obj;
                obj.TPrevious = null;
                obj.TNext = null;

                return;
            }


            GameObject o = Tiles[x, y];

            if (o == obj)
            {
                if (o.Previous != null)
                    o = (GameObject) o.Previous;
                else if (o.Next != null)
                    o = (GameObject) o.Next;
                else
                    return;
            }

            while (o?.TPrevious != null)
                o = o.TPrevious;

            GameObject found = null;
            GameObject start = o;

            while (o != null)
            {
                int testPriorityZ = o.PriorityZ;

                if (testPriorityZ > priorityZ ||
                    (testPriorityZ == priorityZ &&
                    (state == 0 || (state == 1 && !(o is Land)))))
                    break;

                found = o;
                o = o.TNext;
            }

            if (found != null)
            {
                obj.TPrevious = found;
                GameObject next = found.TNext;
                obj.TNext = next;
                found.TNext = obj;

                if (next != null)
                    next.TPrevious = obj;
            }
            else if (start != null)
            {
                obj.TNext = start;
                start.TPrevious = obj;
                obj.TPrevious = null;
            }
        }

        public void RemoveGameObject(GameObject obj, int x, int y)
        {
            ref var firstNode = ref Tiles[x, y];

            if (firstNode == null || obj == null)
                return;

            if (firstNode == obj)
                firstNode = obj.TNext;

            if (obj.TNext != null)
                obj.TNext.TPrevious = obj.TPrevious;

            if (obj.TPrevious != null)
                obj.TPrevious.TNext = obj.TNext;

            obj.TPrevious = null;
            obj.TNext = null;
        }


        public void Destroy()
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    GameObject obj = Tiles[i, j];

                    if (obj == null)
                        continue;

                    var first = GetHeadObject(i, j);

                    while (first != null)
                    {
                        var next = first.TNext;

                        if (first != World.Player)
                            first.Destroy();

                        first.TPrevious = null;
                        first.TNext = null;
                        first = next;
                    }

                    Tiles[i, j] = null;
                }
            }

            if (Node.Next != null || Node.Previous != null)
                Node.List?.Remove(Node);
            IsDestroyed = true;
            _pool.Enqueue(this);
        }

        public void Clear()
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    GameObject obj = Tiles[i, j];

                    if (obj == null)
                        continue;

                    var first = GetHeadObject(i, j);

                    while (first != null)
                    {
                        var next = first.TNext;

                        if (first != World.Player)
                            first.Destroy();

                        first.TPrevious = null;
                        first.TNext = null;
                        first = next;
                    }

                    Tiles[i, j] = null;
                }
            }

            if (Node.Next != null || Node.Previous != null)
                Node.List?.Remove(Node);
            IsDestroyed = true;
        }

        public bool HasNoExternalData()
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    for (var obj = GetHeadObject(i, j); obj != null; obj = obj.TNext)
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