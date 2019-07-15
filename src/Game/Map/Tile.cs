#region license

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

using System.Collections.Generic;
using ClassicUO.Game.GameObjects;
using ClassicUO.IO;
using ClassicUO.IO.Resources;

namespace ClassicUO.Game.Map
{
    internal sealed class Tile
    {
        private bool _isDestroyed;

        public Tile(ushort x, ushort y)
        {
            X = x;
            Y = y;
        }

        private static readonly Queue<Tile> _pool = new Queue<Tile>();

        public static Tile Create(ushort x, ushort y)
        {
            if (_pool.Count != 0)
            {
                var t = _pool.Dequeue();
                t.X = x;
                t.Y = y;
                t._isDestroyed = false;
                
                return t;
            }
            return new Tile(x, y);
        }

        public ushort X { get; private set; }
        public ushort Y { get; private set;  }

        public GameObject FirstNode { get; private set; }

        public void AddGameObject(GameObject obj)
        {
            short priorityZ = obj.Z;

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

                case GameEffect effect when effect.Source == null || !effect.IsItemEffect:
                    priorityZ += 2;

                    break;

                default:

                {
                    ref readonly StaticTiles data = ref FileManager.TileData.StaticData[obj.Graphic];

                    if (data.IsBackground)
                        priorityZ--;

                    if (data.Height != 0)
                        priorityZ++;
                }

                    break;
            }

            obj.PriorityZ = priorityZ;


            if (FirstNode == null)
            {
                FirstNode = obj;

                return;
            }

            GameObject o = FirstNode;

            while (o?.Left != null)
                o = o.Left;

            GameObject found = null;
            GameObject start = o;

            while (o != null)
            {
                int testPriorityZ = o.PriorityZ;

                if (testPriorityZ > priorityZ || testPriorityZ == priorityZ && (obj is Land || obj is Multi) && !(o is Land))
                    break;

                found = o;
                o = o.Right;
            }

            if (found != null)
            {
                obj.Left = found;
                GameObject next = found.Right;
                obj.Right = next;
                found.Right = obj;

                if (next != null)
                    next.Left = obj;
            }
            else if (start != null)
            {
                obj.Right = start;
                start.Left = obj;
                obj.Left = null;
                FirstNode = obj;
            }
        }

        public void RemoveGameObject(GameObject obj)
        {
            if (FirstNode == null || obj == null)
                return;

            if (FirstNode == obj)
                FirstNode = obj.Right;

            if (obj.Right != null)
                obj.Right.Left = obj.Left;

            if (obj.Left != null)
                obj.Left.Right = obj.Right;

            obj.Left = null;
            obj.Right = null;
        }


        public void Destroy()
        {
            if (_isDestroyed)
                return;
            _isDestroyed = true;
            _pool.Enqueue(this);
        }
    }
}