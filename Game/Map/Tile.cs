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

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Views;
using ClassicUO.Interfaces;
using ClassicUO.IO.Resources;

using Microsoft.Xna.Framework;

using MathHelper = ClassicUO.Utility.MathHelper;

namespace ClassicUO.Game.Map
{
    public sealed class Tile : GameObject
    {
        private static readonly List<GameObject> _itemsAtZ = new List<GameObject>();
        private readonly List<GameObject> _objectsOnTile;
        private readonly List<Static> _statics = new List<Static>();
        private bool _needSort;

        public Tile() : base(World.Map)
        {
            _objectsOnTile = new List<GameObject>();
        }

        public int MinZ { get; set; }

        public int AverageZ { get; set; }

        public bool IsIgnored => Graphic < 3 || Graphic == 0x1DB || Graphic >= 0x1AE && Graphic <= 0x1B5;

        public bool IsStretched { get; set; }

        public IReadOnlyList<GameObject> ObjectsOnTiles
        {
            get
            {
                if (_needSort)
                {
                    RemoveDuplicates();
                    TileSorter.Sort(_objectsOnTile);
                    _needSort = false;
                }

                return _objectsOnTile;
            }
        }

        public override Position Position { get; set; }

        public LandTiles TileData => IO.Resources.TileData.LandData[Graphic];

        public void AddGameObject(GameObject obj)
        {
            if (obj is IDynamicItem dyn)
                for (int i = 0; i < _objectsOnTile.Count; i++)
                    if (_objectsOnTile[i] is IDynamicItem dynComp)
                        if (dynComp.Graphic == dyn.Graphic && dynComp.Position.Z == dyn.Position.Z)
                            _objectsOnTile.RemoveAt(i--);
#if ORIONSORT
            short priorityZ = obj.Position.Z;

            switch (obj)
            {
                case Tile tile:
                    var t = tile.View;
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

                    if (IO.Resources.TileData.IsBackground((long) dyn1.ItemData.Flags))
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
            int[] toremove = new int[0x100];
            int index = 0;

            for (int i = 0; i < _objectsOnTile.Count; i++)
                if (_objectsOnTile[i] is Static st)
                    for (int j = i + 1; j < _objectsOnTile.Count; j++)
                        if (_objectsOnTile[i].Position.Z == _objectsOnTile[j].Position.Z)
                            if (_objectsOnTile[j] is Static stj && st.Graphic == stj.Graphic)
                            {
                                toremove[index++] = i;

                                break;
                            }
                            else if (_objectsOnTile[i] is Item item)
                            {
                                for (int jj = i + 1; jj < _objectsOnTile.Count; jj++)
                                    if (_objectsOnTile[i].Position.Z == _objectsOnTile[jj].Position.Z)
                                        if (_objectsOnTile[jj] is Static stj1 && item.ItemData.Name == stj1.ItemData.Name || _objectsOnTile[jj] is Item itemj && item.Serial == itemj.Serial)
                                            toremove[index++] = jj;
                            }

            for (int i = 0; i < index; i++) _objectsOnTile.RemoveAt(toremove[i] - i);
        }

        public List<GameObject> GetItemsBetweenZ(int z0, int z1)
        {
            List<GameObject> items = _itemsAtZ;
            _itemsAtZ.Clear();

            for (int i = 0; i < ObjectsOnTiles.Count; i++)
                if (MathHelper.InRange(ObjectsOnTiles[i].Position.Z, z0, z1))
                    if (ObjectsOnTiles[i] is IDynamicItem)
                        items.Add(ObjectsOnTiles[i]);

            return items;
        }

        public bool IsZUnderObjectOrGround(sbyte z, out GameObject entity, out GameObject ground)
        {
            List<GameObject> list = (List<GameObject>) ObjectsOnTiles;
            entity = null;
            ground = null;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].Position.Z <= z) continue;

                if (list[i] is IDynamicItem dyn)
                {
                    StaticTiles itemdata = dyn.ItemData;

                    if (IO.Resources.TileData.IsRoof((long) itemdata.Flags) || IO.Resources.TileData.IsSurface((long) itemdata.Flags) || IO.Resources.TileData.IsWall((long) itemdata.Flags) && IO.Resources.TileData.IsImpassable((long) itemdata.Flags))
                        if (entity == null || list[i].Position.Z < entity.Position.Z)
                            entity = list[i];
                }
                else if (list[i] is Tile tile && tile.AverageZ >= z + 12)
                {
                    ground = list[i];
                }
            }

            return entity != null || ground != null;
        }

        public List<Static> GetStatics()
        {
            List<Static> items = _statics;
            _statics.Clear();

            for (int i = 0; i < _objectsOnTile.Count; i++)
                if (_objectsOnTile[i] is Static st)
                    items.Add(st);

            return items;
        }

        public void UpdateZ(int zTop, int zRight, int zBottom)
        {
            if (IsStretched)
            {
                int x = Position.Z * 4 + 1;
                int y = zTop * 4;
                int w = zRight * 4 - x;
                int h = zBottom * 4 + 1 - y;

                Rectangle = new Rectangle(x, y, w, h);

                int average = AverageZ;

                if (Math.Abs(Position.Z - zRight) <= Math.Abs(zBottom - zTop))
                    AverageZ = (Position.Z + zRight) >> 1;
                else
                    AverageZ = (zBottom + zTop) >> 1;

                if (AverageZ != average)
                    ForceSort();

                MinZ = Position.Z;

                if (zTop < MinZ)
                    MinZ = zTop;

                if (zRight < MinZ)
                    MinZ = zRight;

                if (zBottom < MinZ)
                    MinZ = zBottom;
            }
        }

        public int CalculateCurrentAverageZ(int direction)
        {
            int result = GetDirectionZ(((byte)(direction >> 1) + 1) & 3);

            if ((direction & 1) > 0)
                return result;

            return (result + GetDirectionZ(direction >> 1)) >> 1;
        }

        private int GetDirectionZ(int direction)
        {
            switch (direction)
            {
                case 1: return Rectangle.Bottom / 4;
                case 2: return Rectangle.Right / 4;
                case 3: return Rectangle.Top / 4;
                default: return Position.Z;
            }
        }

        public Rectangle Rectangle;

        protected override View CreateView() => new TileView(this);
    }
}