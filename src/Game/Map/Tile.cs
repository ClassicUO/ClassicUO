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
        //private List<GameObject> _objectsOnTile;
        private bool _needSort;

        public Tile(ushort x, ushort y)
        {
            X = x;
            Y = y;
            _needSort = false;
        }

        public ushort X { get; }
        public ushort Y { get; }

        //public List<GameObject> ObjectsOnTiles
        //{
        //    get
        //    {
        //        if (_objectsOnTile == null)
        //            _objectsOnTile = new List<GameObject>();

        //        if (_needSort)
        //        {
        //            RemoveDuplicates();
        //            TileSorter.Sort(ref _objectsOnTile);
        //            _needSort = false;
        //        }

        //        return _objectsOnTile;
        //    }
        //}

        public GameObject FirstNode { get; private set; }

        public void Add(GameObject obj)
        {
            if (FirstNode == null)
            {
                FirstNode = obj;             
                FirstNode.Left = null;
                FirstNode.Right = null;
            }
            else
            {
                GameObject last = FirstNode;

                while (last.Right != null)
                    last = last.Right;

                last.Right = obj;
                obj.Left = last;
                obj.Right = null;
            }
        }

        public void Remove(GameObject obj)
        {
            GameObject founded = FirstNode;
            if (founded == null)
                throw new NullReferenceException();

            while (founded != obj && founded != null)
            {
                founded = founded.Right;
            }

            if (founded != null)
            {
                GameObject left = founded.Left;
                GameObject right = founded.Right;

                if (left != null)
                    left.Right = right;

                if (right != null)
                    right.Left = left;

                founded.Left = null;
                founded.Right = null;
            }
        }


        public void AddGameObject(GameObject obj)
        {
            //if (_objectsOnTile == null)
            //    _objectsOnTile = new List<GameObject>();

            //if (obj is Land)
            //{
            //    for (int i = 0; i < _objectsOnTile.Count; i++)
            //    {
            //        if (_objectsOnTile[i] == obj)
            //        {
            //            _objectsOnTile[i].Dispose();
            //            _objectsOnTile.RemoveAt(i);
            //            i--;
            //        }
            //    }
            //}

            //if (obj is IDynamicItem dyn)
            //{
            //    for (int i = 0; i < _objectsOnTile.Count; i++)
            //    {
            //        if (_objectsOnTile[i] is IDynamicItem dynComp)
            //        {
            //            if (dynComp.Graphic == dyn.Graphic && dynComp.Position.Z == dyn.Position.Z)
            //                _objectsOnTile.RemoveAt(i--);
            //        }
            //    }
            //}
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

            //_objectsOnTile.Add(obj);
            //_needSort = _objectsOnTile.Count > 1;

            Add(obj);

            TileSorter.Sort(FirstNode);
        }

        public void RemoveGameObject(GameObject obj)
        {
            Remove(obj);
            //_objectsOnTile.Remove(obj);
        }

        private void RemoveDuplicates()
        {
            //int[] toremove = new int[0x100];
            //int index = 0;

            //for (int i = 0; i < _objectsOnTile.Count; i++)
            //{
            //    if (_objectsOnTile[i] is Static st)
            //    {
            //        for (int j = i + 1; j < _objectsOnTile.Count; j++)
            //        {
            //            if (_objectsOnTile[i].Position.Z == _objectsOnTile[j].Position.Z)
            //            {
            //                if (_objectsOnTile[j] is Static stj && st.Graphic == stj.Graphic)
            //                {
            //                    //toremove[index++] = i;
            //                    Log.Message(LogTypes.Warning, "Duplicated");
            //                    _objectsOnTile.RemoveAt(i--);

            //                    break;
            //                }

            //                if (_objectsOnTile[i] is Item item)
            //                {
            //                    for (int jj = i + 1; jj < _objectsOnTile.Count; jj++)
            //                    {
            //                        if (_objectsOnTile[i].Position.Z == _objectsOnTile[jj].Position.Z)
            //                        {
            //                            if (_objectsOnTile[jj] is Static stj1 && item.ItemData.Name == stj1.ItemData.Name || _objectsOnTile[jj] is Item itemj && item.Serial == itemj.Serial)
            //                            {
            //                                //toremove[index++] = jj;
            //                                Log.Message(LogTypes.Warning, "Duplicated");
            //                                _objectsOnTile.RemoveAt(jj--);
            //                            }
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //    }
            //}

            //for (int i = 0; i < index; i++)
            //    _objectsOnTile.RemoveAt(toremove[i] - i);
        }


        //public void Dispose()
        //{
        //    if (_objectsOnTile == null)
        //        return;

        //    for (int i = 0; i < _objectsOnTile.Count; i++)
        //    {
        //        GameObject t = _objectsOnTile[i];

        //        if (t != World.Player)
        //        {
        //            t.Dispose();
        //        }
        //    }

        //}
    }
}