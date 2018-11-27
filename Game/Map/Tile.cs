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

using System.Collections.Generic;

using ClassicUO.Game.GameObjects;
using ClassicUO.Interfaces;
using ClassicUO.IO.Resources;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Map
{
    public struct Tile
    {
        public static readonly Tile Invalid = new Tile(0xFFFF, 0xFFFF);
        private static readonly List<GameObject> _itemsAtZ = new List<GameObject>();
        private List<GameObject> _objectsOnTile;
        private bool _needSort;
        private ushort? _x, _y;

        public Tile(ushort x, ushort y)
        {
            _x = x;
            _y = y;
            _needSort = false;
            _objectsOnTile = new List<GameObject>();
            //Land = null;
        }

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

        //public Land Land { get; private set; }

        public static bool operator ==(Tile p1, Tile p2)
        {
            return p1.X == p2.X && p1.Y == p2.Y;
        }

        public static bool operator !=(Tile p1, Tile p2)
        {
            return p1.X != p2.X || p1.Y != p2.Y;
        }

        public override int GetHashCode()
        {
            return X ^ Y;
        }

        public override bool Equals(object obj)
        {
            return obj is Tile tile && this == tile;
        }

        public IReadOnlyList<GameObject> ObjectsOnTiles
        {
            get
            {
                if (_needSort)
                {
                    RemoveDuplicates();
                    TileSorter.Sort(ref _objectsOnTile);
                    _needSort = false;
                }

                return _objectsOnTile;
            }
        }

        public void AddGameObject(GameObject obj)
        {
            //if (obj is Land land)
            //{
            //    if (Land != null && land != Land)
            //    {
            //        Land.Dispose();
            //        RemoveGameObject(Land);
            //    }

            //    Land = land;
            //}

            if (obj is Land)
            {
                for (int i = 0; i < _objectsOnTile.Count; i++)
                {
                    if (_objectsOnTile[i] == obj)
                    {
                        _objectsOnTile[i].Dispose();
                        RemoveGameObject(_objectsOnTile[i]);
                        i--;
                    }
                }
            }

            if (obj is IDynamicItem dyn)
            {
                for (int i = 0; i < _objectsOnTile.Count; i++)
                {
                    if (_objectsOnTile[i] is IDynamicItem dynComp)
                    {
                        if (dynComp.Graphic == dyn.Graphic && dynComp.Position.Z == dyn.Position.Z)
                            _objectsOnTile.RemoveAt(i--);
                    }
                }
            }
#if ORIONSORT
            short priorityZ = obj.Position.Z;

            switch (obj)
            {
                case Land tile:

                    if (tile.IsStretched)
                        priorityZ = (short) (tile.AverageZ - 1);
                    else
                        priorityZ--;

                    break;
                case Mobile _:
                    priorityZ++;

                    break;
                case Item item when item.IsCorpse:
                    priorityZ++;

                    break;
                case GameEffect _:
                    priorityZ += 2;

                    break;
                default:

                {
                    IDynamicItem dyn1 = (IDynamicItem) obj;

                    if (TileData.IsBackground((long) dyn1.ItemData.Flags))
                        priorityZ--;

                    if (dyn1.ItemData.Height > 0)
                        priorityZ++;
                }

                    break;
            }

            obj.PriorityZ = priorityZ;
#endif
            _objectsOnTile.Add(obj);
            _needSort = true;
        }

        public void RemoveGameObject(GameObject obj)
        {
            _objectsOnTile.Remove(obj);
        }

        public void ForceSort()
        {
            _needSort = true;
        }

        private void RemoveDuplicates()
        {
            //int[] toremove = new int[0x100];
            //int index = 0;

            for (int i = 0; i < _objectsOnTile.Count; i++)
            {
                if (_objectsOnTile[i] is Static st)
                {
                    for (int j = i + 1; j < _objectsOnTile.Count; j++)
                    {
                        if (_objectsOnTile[i].Position.Z == _objectsOnTile[j].Position.Z)
                        {
                            if (_objectsOnTile[j] is Static stj && st.Graphic == stj.Graphic)
                            {
                                //toremove[index++] = i;
                                Log.Message(LogTypes.Warning, "Duplicated");
                                _objectsOnTile.RemoveAt(i--);

                                break;
                            }

                            if (_objectsOnTile[i] is Item item)
                            {
                                for (int jj = i + 1; jj < _objectsOnTile.Count; jj++)
                                {
                                    if (_objectsOnTile[i].Position.Z == _objectsOnTile[jj].Position.Z)
                                    {
                                        if (_objectsOnTile[jj] is Static stj1 && item.ItemData.Name == stj1.ItemData.Name || _objectsOnTile[jj] is Item itemj && item.Serial == itemj.Serial)
                                        {
                                            //toremove[index++] = jj;
                                            Log.Message(LogTypes.Warning, "Duplicated");
                                            _objectsOnTile.RemoveAt(jj--);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //for (int i = 0; i < index; i++)
            //    _objectsOnTile.RemoveAt(toremove[i] - i);
        }

        public List<GameObject> GetItemsBetweenZ(int z0, int z1)
        {
            List<GameObject> items = _itemsAtZ;
            _itemsAtZ.Clear();

            for (int i = 0; i < ObjectsOnTiles.Count; i++)
            {
                if (MathHelper.InRange(ObjectsOnTiles[i].Position.Z, z0, z1))
                {
                    if (ObjectsOnTiles[i] is IDynamicItem)
                        items.Add(ObjectsOnTiles[i]);
                }
            }

            return items;
        }

        public bool IsZUnderObjectOrGround(sbyte z, out GameObject entity, out GameObject ground)
        {
            var list = ObjectsOnTiles;
            entity = null;
            ground = null;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].Position.Z <= z) continue;

                if (list[i] is IDynamicItem dyn)
                {
                    StaticTiles itemdata = dyn.ItemData;

                    if (TileData.IsRoof((long) itemdata.Flags) || TileData.IsSurface((long) itemdata.Flags) || TileData.IsWall((long) itemdata.Flags) && TileData.IsImpassable((long) itemdata.Flags))
                    {
                        if (entity == null || list[i].Position.Z < entity.Position.Z)
                            entity = list[i];
                    }
                }
                else if (list[i] is Land tile && tile.AverageZ >= z + 12) ground = list[i];
            }

            return entity != null || ground != null;
        }

        public void Dispose()
        {
            for (int i = 0; i < _objectsOnTile.Count; i++)
            {
                GameObject t = _objectsOnTile[i];

                if (t != World.Player)
                {
                    t.Dispose();
                    _objectsOnTile.RemoveAt(i--);
                }
            }
        }
    }
}