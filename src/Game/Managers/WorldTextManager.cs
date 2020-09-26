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

using System;
using System.Collections.Generic;
using ClassicUO.Game.GameObjects;
using ClassicUO.Renderer;

namespace ClassicUO.Game.Managers
{
    internal class WorldTextManager : TextRenderer
    {
        private readonly Dictionary<uint, OverheadDamage> _damages = new Dictionary<uint, OverheadDamage>();
        private readonly List<Tuple<uint, uint>> _subst = new List<Tuple<uint, uint>>();
        private readonly List<uint> _toRemoveDamages = new List<uint>();


        public override void Update(double totalTime, double frameTime)
        {
            base.Update(totalTime, frameTime);


            UpdateDamageOverhead(totalTime, frameTime);

            if (_toRemoveDamages.Count > 0)
            {
                foreach (uint s in _toRemoveDamages)
                {
                    _damages.Remove(s);
                }

                _toRemoveDamages.Clear();
            }
        }


        public override void Draw(UltimaBatcher2D batcher, int startX, int startY, int renderIndex, bool isGump = false)
        {
            base.Draw(batcher, startX, startY, renderIndex, isGump);

            foreach (KeyValuePair<uint, OverheadDamage> overheadDamage in _damages)
            {
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

                overheadDamage.Value.Draw(batcher);
            }
        }

        private void UpdateDamageOverhead(double totalTime, double frameTime)
        {
            if (_subst.Count != 0)
            {
                foreach (Tuple<uint, uint> tuple in _subst)
                {
                    if (_damages.TryGetValue(tuple.Item1, out OverheadDamage dmg))
                    {
                        _damages.Remove(tuple.Item1);
                        _damages[tuple.Item2] = dmg;
                    }
                }

                _subst.Clear();
            }

            foreach (KeyValuePair<uint, OverheadDamage> overheadDamage in _damages)
            {
                overheadDamage.Value.Update();

                if (overheadDamage.Value.IsEmpty)
                {
                    _toRemoveDamages.Add(overheadDamage.Key);
                }
            }
        }


        internal void AddDamage(uint obj, int dmg)
        {
            if (!_damages.TryGetValue(obj, out OverheadDamage dm) || dm == null)
            {
                dm = new OverheadDamage(World.Get(obj));
                _damages[obj] = dm;
            }

            dm.Add(dmg);
        }

        public override void Clear()
        {
            if (_toRemoveDamages.Count > 0)
            {
                foreach (uint s in _toRemoveDamages)
                {
                    _damages.Remove(s);
                }

                _toRemoveDamages.Clear();
            }

            _subst.Clear();

            //_staticToUpdate.Clear();

            base.Clear();
        }
    }
}