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

using System.Collections.Generic;
using System.Linq;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Utility.Collections;

namespace ClassicUO.Game.Managers
{
    internal class CorpseManager
    {
        private readonly Deque<CorpseInfo> _corpses = new Deque<CorpseInfo>();

        public void Add(uint corpse, uint obj, Direction dir, bool run)
        {
            for (int i = 0; i < _corpses.Count; i++)
            {
                ref var c = ref _corpses.GetAt(i);

                if (c.CorpseSerial == corpse)
                {
                    return;
                }
            }

            _corpses.AddToBack(new CorpseInfo(corpse, obj, dir, run));
        }

        public void Remove(uint corpse, uint obj)
        {
            for (int i = 0; i < _corpses.Count;)
            {
                ref var c = ref _corpses.GetAt(i);

                if (c.CorpseSerial == corpse || c.ObjectSerial == obj)
                {
                    if (corpse != 0)
                    {
                        Item item = World.Items.Get(corpse);
                        if (item != null)
                            item.Layer = (Layer) ((c.Direction & Direction.Mask) | (c.IsRunning ? Direction.Running : 0));
                    }

                    _corpses.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
        }

        public bool Exists(uint corpse, uint obj)
        {
            for (int i = 0; i < _corpses.Count; i++)
            {
                ref var c = ref _corpses.GetAt(i);

                if (c.CorpseSerial == corpse || c.ObjectSerial == obj)
                {
                    return true;
                }
            }

            return false;
        }

        public Item GetCorpseObject(uint serial)
        {
            for (int i = 0; i < _corpses.Count; i++)
            {
                ref var c = ref _corpses.GetAt(i);

                if (c.ObjectSerial == serial)
                {
                    return World.Items.Get(c.CorpseSerial);
                }
            }

            return null;
        }

        public void Clear()
        {
            _corpses.Clear();
        }
    }

    struct CorpseInfo
    {
        public CorpseInfo(uint corpseSerial, uint objectSerial, Direction direction, bool isRunning)
        {
            CorpseSerial = corpseSerial;
            ObjectSerial = objectSerial;
            Direction = direction;
            IsRunning = isRunning;
        }

        public uint CorpseSerial, ObjectSerial;
        public Direction Direction;
        public bool IsRunning;
    }
}