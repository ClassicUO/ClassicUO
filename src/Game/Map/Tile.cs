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

using System;
using System.Collections.Generic;
using System.Diagnostics;

using ClassicUO.Game.GameObjects;
using ClassicUO.Interfaces;
using ClassicUO.IO.Resources;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Map
{
    public sealed class Tile
    {
        //public static readonly Tile Invalid = new Tile(0xFFFF, 0xFFFF);
        private static readonly List<GameObject> _itemsAtZ = new List<GameObject>();
        private List<GameObject> _objectsOnTile;
        private bool _needSort;
        //private ushort? _x, _y;

        public Tile(ushort x, ushort y)
        {
            X = x;
            Y = y;
            _needSort = false;
            //_objectsOnTile = new List<GameObject>();
            //Land = null;
        }

        public ushort X { get; }
        public ushort Y { get; }
        //public ushort X
        //{
        //    get => _x ?? 0xFFFF;
        //    set => _x = value;
        //}

        //public ushort Y
        //{
        //    get => _y ?? 0xFFFF;
        //    set => _y = value;
        //}

        //public Land Land { get; private set; }

        //public static bool operator ==(Tile p1, Tile p2)
        //{
        //    return p1.X == p2.X && p1.Y == p2.Y;
        //}

        //public static bool operator !=(Tile p1, Tile p2)
        //{
        //    return p1.X != p2.X || p1.Y != p2.Y;
        //}

        //public override int GetHashCode()
        //{
        //    return X ^ Y;
        //}

        //public override bool Equals(object obj)
        //{
        //    return obj is Tile tile && this == tile;
        //}

        public List<GameObject> ObjectsOnTiles
        {
            get
            {
                if (_objectsOnTile == null)
                    _objectsOnTile = new List<GameObject>();

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
            if (_objectsOnTile == null)
                _objectsOnTile = new List<GameObject>();

            if (obj is Land)
            {
                for (int i = 0; i < _objectsOnTile.Count; i++)
                {
                    if (_objectsOnTile[i] == obj)
                    {
                        _objectsOnTile[i].Dispose();
                        _objectsOnTile.RemoveAt(i);
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

                    if (TileData.IsBackground(dyn1.ItemData.Flags))
                        priorityZ--;

                    if (dyn1.ItemData.Height > 0)
                        priorityZ++;
                }

                    break;
            }

            obj.PriorityZ = priorityZ;

            _objectsOnTile.Add(obj);
            _needSort = _objectsOnTile.Count > 1;
        }

        public void RemoveGameObject(GameObject obj)
        {
            _objectsOnTile.Remove(obj);
        }

        //public void ForceSort()
        //{
        //    _needSort = true;
        //}

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


        public void Dispose()
        {
            if (_objectsOnTile == null)
                return;

            for (int i = 0; i < _objectsOnTile.Count; i++)
            {
                GameObject t = _objectsOnTile[i];

                if (t != World.Player)
                {
                    t.Dispose();
                }
            }

        }
    }
}