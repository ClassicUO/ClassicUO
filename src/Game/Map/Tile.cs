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
        public Tile(ushort x, ushort y)
        {
            X = x;
            Y = y;
        }

        public ushort X { get; }
        public ushort Y { get; }

        public GameObject FirstNode { get; private set; }

        private void Add(GameObject obj)
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

        private void Remove(GameObject obj)
        {

            //if (obj != null)
            //{
            //    GameObject left = obj.Left;
            //    GameObject right = obj.Right;

            //    if (left != null)
            //        left.Right = right;

            //    if (right != null)
            //        right.Left = left;

            //    obj.Left = null;
            //    obj.Right = null;
            //}


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

                    StaticTiles data = TileData.StaticData[obj.Graphic];

                    if (TileData.IsBackground(data.Flags))
                        priorityZ--;

                    if (data.Height > 0)
                        priorityZ++;
                }

                    break;
            }

            obj.PriorityZ = priorityZ;

            Add(obj);

            TileSorter.Sort(FirstNode);
        }

        public void RemoveGameObject(GameObject obj)
        {
            Remove(obj);
        }

    }
}