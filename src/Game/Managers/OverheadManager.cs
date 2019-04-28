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

using System;
using System.Collections.Generic;
using System.Linq;

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using IUpdateable = ClassicUO.Interfaces.IUpdateable;

namespace ClassicUO.Game.Managers
{
    internal class OverheadManager : IUpdateable
    {
        private readonly List<Serial> _toRemoveDamages = new List<Serial>();
        private readonly List< Tuple<Serial, Serial>> _subst = new List<Tuple<Serial, Serial>>();
        private readonly List<GameObject> _staticToUpdate = new List<GameObject>();

        private OverheadMessage _firstNode;

        private readonly Dictionary<Serial, OverheadDamage> _damages = new Dictionary<Serial, OverheadDamage>();



        private void DrawTextOverheads(Batcher2D batcher, int startX, int startY, float scale)
        {
            if (_firstNode != null)
            {

                var first = _firstNode;

                int mouseX = Mouse.Position.X;
                int mouseY = Mouse.Position.Y;

                while (first != null)
                {
                    float alpha = first.IsOverlap(first.Right);
                    first.Draw(batcher, startX, startY, scale);
                    first.SetAlpha(alpha);
                    first.Contains(mouseX, mouseY);

                    var temp = first.Right;
                    first.Right = null;
                    first = temp;
                }

                _firstNode = null;
            }
        }

        public void AddOverhead(OverheadMessage overhead)
        {
            if ((overhead.Parent is Static || overhead.Parent is Multi) && !_staticToUpdate.Contains(overhead.Parent))
                _staticToUpdate.Add(overhead.Parent);

            if (_firstNode == null)
            {
                _firstNode = overhead;
            }
            else
            {
                var last = _firstNode;

                while (last.Right != null)
                    last = last.Right;

                last.Right = overhead;
                overhead.Right = null;
            }
        }
        

        public void Update(double totalMS, double frameMS)
        {
            for (int i = 0; i < _staticToUpdate.Count; i++)
            {
                var st = _staticToUpdate[i];
                st.Update(totalMS, frameMS);

                if (st.IsDestroyed)
                    _staticToUpdate.RemoveAt(i--);
            }

            UpdateDamageOverhead(totalMS, frameMS);

            if(_toRemoveDamages.Count > 0)
            {
                _toRemoveDamages.ForEach(s =>
                {
                    _damages.Remove(s);
                });
                _toRemoveDamages.Clear();
            }
        }


        public bool Draw(Batcher2D batcher, int startX, int startY)
        {
            float scale = Engine.SceneManager.GetScene<GameScene>().Scale;

            DrawTextOverheads(batcher, startX, startY, scale);

            foreach (KeyValuePair<Serial, OverheadDamage> overheadDamage in _damages)
            {
                int x = startX;
                int y = startY;

                Entity mob = World.Get(overheadDamage.Key);

                if (mob == null || mob.IsDestroyed)
                {
                    uint ser = overheadDamage.Key | 0x8000_0000;

                    if (World.CorpseManager.Exists(0, ser))
                    {
                        Item item = World.CorpseManager.GetCorpseObject(ser);

                        if (item != null && item != overheadDamage.Value.Parent)
                        {
                            _subst.Add(Tuple.Create(overheadDamage.Key, item.Serial));
                            overheadDamage.Value.SetParent(item);
                        }
                    }
                    else
                    {
                        continue;
                    }
                }

                overheadDamage.Value.Draw(batcher, x, y, scale);
            }


            return true;
        }

        private void UpdateDamageOverhead(double totalMS, double frameMS)
        {

            if (_subst.Count != 0)
            {
                foreach (Tuple<Serial, Serial> tuple in _subst)
                {
                    if (_damages.TryGetValue(tuple.Item1, out var dmg))
                    {
                        _damages.Remove(tuple.Item1);
                        _damages.Add(tuple.Item2, dmg);
                    }
                }

                _subst.Clear();
            }

            foreach (KeyValuePair<Serial, OverheadDamage> overheadDamage in _damages)
            {
                overheadDamage.Value.Update();

                if (overheadDamage.Value.IsEmpty)
                {
                    _toRemoveDamages.Add(overheadDamage.Key);
                }
            }
        }


        internal void AddDamage(Serial obj, int dmg)
        {
            if (!_damages.TryGetValue(obj, out var dm) || dm == null)
            {
                dm = new OverheadDamage(World.Get(obj));
                _damages[obj] = dm;
            }

            dm.Add(dmg);

        }

        public void Clear()
        {
            if (_toRemoveDamages.Count > 0)
            {
                _toRemoveDamages.ForEach(s =>
                {
                    _damages.Remove(s);
                });
                _toRemoveDamages.Clear();
            }

            _subst.Clear();

            var last = _firstNode;

            while (last != null)
            {
                var temp = last.Right;

                last.Destroy();

                last.Left = null;
                last.Right = null;

                last = temp;
            }

            _firstNode = null;

            _staticToUpdate.ForEach( s=> s.Destroy());
            _staticToUpdate.Clear();
        }
    }
}