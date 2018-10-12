﻿#region license

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
using ClassicUO.Game.Views;
using ClassicUO.Interfaces;
using ClassicUO.IO.Resources;
using ClassicUO.Utility;

namespace ClassicUO.Game.Map
{
    public sealed class Tile : GameObject
    {
        private static readonly List<GameObject> _itemsAtZ = new List<GameObject>();
        private readonly List<GameObject> _objectsOnTile;

        private readonly List<Static> _statics = new List<Static>();
        private bool _needSort;
        private LandTiles? _tileData;

        public Tile() : base(World.Map) => _objectsOnTile = new List<GameObject>();

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

        public LandTiles TileData
        {
            get
            {
                if (!_tileData.HasValue)
                    _tileData = IO.Resources.TileData.LandData[Graphic];
                return _tileData.Value;
            }
        }


        public void AddGameObject(GameObject obj)
        {
            if (obj is IDynamicItem)
            {
                for (int i = 0; i < _objectsOnTile.Count; i++)
                {
                    if (_objectsOnTile[i] is Item || _objectsOnTile[i] is Static)
                    {
                        if (obj.Graphic == _objectsOnTile[i].Graphic && obj.Position.Z == _objectsOnTile[i].Position.Z)
                        {
                            _objectsOnTile.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }

#if ORIONSORT
            short priorityZ = obj.Position.Z;


            switch (obj)
            {
                case Tile tile:
                    {
                        if (tile.IsStretched)
                            priorityZ = (short)(((TileView)tile.View).SortZ - 1);
                        else
                            priorityZ--;
                    }
                    break;
                case Mobile mobile:
                    priorityZ++;
                    break;
                case Item item:
                    if (item.IsCorpse)
                        priorityZ++;
                    else
                        goto default;
                    break;
                case GameEffect effect:
                    priorityZ += 2;
                    break;
                //case DeferredEntity deferred:
                //    if (deferred.Entity is Mobile)
                //        priorityZ++;
                //    else if (deferred.Entity is GameEffect)
                //        priorityZ += 2;
                //    break;
                default:
                    {
                        IDynamicItem dyn = (IDynamicItem)obj;

                        if (IO.Resources.TileData.IsBackground((long)dyn.ItemData.Flags))
                            priorityZ--;

                        if (dyn.ItemData.Height > 0)
                            priorityZ++;
                    }
                    break;
            }


            obj.PriorityZ = priorityZ;

#endif

            //GameObject found = null;
            //var objFirst = _objectsOnTile.Count > 0 ? _objectsOnTile[0] : null;
            //if (objFirst != null)
            //{
            //    for (int i = 0; i < _objectsOnTile.Count - 1; i++)
            //    {
            //        int test = _objectsOnTile[i].PriorityZ;
            //        if (test > priorityZ || (test == priorityZ && obj is Tile && !(objFirst is Tile)))
            //            break;

            //        found = objFirst;
            //        objFirst = _objectsOnTile[i + 1];
            //    }
            //}

            //if (found != null)
            //{

            //}


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

        //public void Clear()
        //{

        //    for (int k = 0; k < _objectsOnTile.Count; k++)
        //    {
        //        var obj = _objectsOnTile[k];
        //        if (obj is Tile || obj is Static)
        //        {
        //            int count = _objectsOnTile.Count;
        //            obj.Dispose();
        //            if (count == _objectsOnTile.Count)
        //                _objectsOnTile.RemoveAt(k);
        //            k--;
        //        }
        //    }

        //    _statics.Clear();

        //    //for (int i = 0; i < _objectsOnTile.Count; i++)
        //    //{
        //    //    var obj = _objectsOnTile[i];

        //    //    if (obj is Entity || obj is Tile)
        //    //        continue;

        //    //    int count = _objectsOnTile.Count;

        //    //    obj.Dispose();

        //    //    if (count == _objectsOnTile.Count)
        //    //    {
        //    //        _objectsOnTile.RemoveAt(i);
        //    //    }

        //    //    i--;           
        //    //}

        //    //_objectsOnTile.Clear();

        //    //DisposeView();
        //    //Graphic = 0;
        //    //Position = Position.Invalid;
        //    //_tileData = null;
        //    //_needSort = false;
        //    //_statics.Clear();
        //}


        private void RemoveDuplicates()
        {
            int[] toremove = new int[0x100];
            int index = 0;

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
                                toremove[index++] = i;
                                break;
                            }
                        }
                    }
                }
                else if (_objectsOnTile[i] is Item item)
                {
                    for (int j = i + 1; j < _objectsOnTile.Count; j++)
                    {
                        if (_objectsOnTile[i].Position.Z == _objectsOnTile[j].Position.Z)
                        {
                            if (_objectsOnTile[j] is Static stj && item.ItemData.Name == stj.ItemData.Name ||
                                _objectsOnTile[j] is Item itemj && item.Serial == itemj.Serial) toremove[index++] = j;
                        }
                    }
                }
            }

            for (int i = 0; i < index; i++) _objectsOnTile.RemoveAt(toremove[i] - i);
        }


        public List<GameObject> GetItemsBetweenZ(int z0, int z1)
        {
            List<GameObject> items = _itemsAtZ;
            _itemsAtZ.Clear();

            for (int i = 0; i < ObjectsOnTiles.Count; i++)
            {
                if (MathHelper.InRange(ObjectsOnTiles[i].Position.Z, z0, z1))
                    if (ObjectsOnTiles[i] is IDynamicItem)
                        items.Add(ObjectsOnTiles[i]);
            }

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
                    if (IO.Resources.TileData.IsRoof((long) itemdata.Flags) ||
                        IO.Resources.TileData.IsSurface((long) itemdata.Flags) ||
                        IO.Resources.TileData.IsWall((long) itemdata.Flags) &&
                        IO.Resources.TileData.IsImpassable((long) itemdata.Flags))
                        if (entity == null || list[i].Position.Z < entity.Position.Z)
                            entity = list[i];
                }

                else if (list[i] is Tile tile && tile.View.SortZ >= z + 12) ground = list[i];
            }

            return entity != null || ground != null;
        }

        public List<Static> GetStatics()
        {
            List<Static> items = _statics;
            _statics.Clear();

            for (int i = 0; i < _objectsOnTile.Count; i++)
            {
                if (_objectsOnTile[i] is Static st)
                    items.Add(st);
            }

            return items;
        }

        //public T[] GetGameObjects<T>() where T : GameObject
        //{
        //    return (T[])_objectsOnTile.OfType<T>();
        //}

        // create view only when TileID is initialized
        protected override View CreateView() =>
            Graphic <= 0 || Position == Position.Invalid ? null : new TileView(this);
    }
}