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

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.Interfaces;
using ClassicUO.Renderer;

namespace ClassicUO.Game.Managers
{
    internal class OverheadManager : IUpdateable
    {
        private readonly Dictionary<Serial, OverheadDamage> _damages = new Dictionary<Serial, OverheadDamage>();
        private readonly List<GameObject> _staticToUpdate = new List<GameObject>();
        private readonly List<Tuple<Serial, Serial>> _subst = new List<Tuple<Serial, Serial>>();
        private readonly List<Serial> _toRemoveDamages = new List<Serial>();

        private OverheadMessage _firstNode;


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

            if (_toRemoveDamages.Count > 0)
            {
                foreach ( Serial s in _toRemoveDamages)
                {
                    _damages.Remove(s);
                }

                _toRemoveDamages.Clear();
            }
        }



        private void DrawTextOverheads(UltimaBatcher2D batcher, int startX, int startY, float scale)
        {
            if (_firstNode != null)
            {
                int mouseX = Mouse.Position.X;
                int mouseY = Mouse.Position.Y;

                while (_firstNode != null)
                {
                    float alpha = _firstNode.IsOverlap(_firstNode.Right);
                    _firstNode.Draw(batcher, startX, startY, scale);
                    _firstNode.SetAlpha(alpha);

                    if (_firstNode.Contains(mouseX, mouseY))
                    {
                      
                    }

                    _firstNode = _firstNode.Right;
                }
            }
        }

        public void AddOverhead(OverheadMessage overhead)
        {
            if ((overhead.Parent is Static || overhead.Parent is Multi) && !_staticToUpdate.Contains(overhead.Parent))
                _staticToUpdate.Add(overhead.Parent);

            if (_firstNode == null)
                _firstNode = overhead;
            else
            {
                var last = _firstNode;

                while (last.Right != null)
                    last = last.Right;

                last.Right = overhead;
                overhead.Right = null;
            }
        }


        public bool Draw(UltimaBatcher2D batcher, int startX, int startY)
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
                        continue;
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

                if (overheadDamage.Value.IsEmpty) _toRemoveDamages.Add(overheadDamage.Key);
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
                foreach (Serial s in _toRemoveDamages)
                {
                    _damages.Remove(s);
                }

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

            foreach (GameObject s in _staticToUpdate)
            {
                s.Destroy();
            }
            _staticToUpdate.Clear();
        }
    }
}