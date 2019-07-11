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
using System.Linq;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Managers
{
    internal class CorpseManager
    {
        private readonly Dictionary<Serial, CorpseInfo?> _corpses = new Dictionary<Serial, CorpseInfo?>();

        public void Add(Serial corpse, Serial obj, Direction dir, bool run)
        {
            if (!_corpses.ContainsKey(corpse)) _corpses[corpse] = new CorpseInfo(corpse, obj, dir, run);
        }

        public void Remove(Serial corpse, Serial obj)
        {
            CorpseInfo? c = _corpses.Values.FirstOrDefault(s => s.HasValue && (s.Value.CorpseSerial == corpse || s.Value.ObjectSerial == obj));

            if (c != null)
            {
                Item item = World.Items.Get(corpse);

                if (item != null) item.Layer = (Layer) ((c.Value.Direction & Direction.Mask) | (c.Value.IsRunning ? Direction.Running : 0));
                _corpses.Remove(c.Value.CorpseSerial);
            }
        }

        public bool Exists(Serial corpse, Serial obj)
        {
            return _corpses.Values.Any(s => s.HasValue && (s.Value.CorpseSerial == corpse || s.Value.ObjectSerial == obj));
        }

        public Item GetCorpseObject(Serial serial)
        {
            CorpseInfo? c = _corpses.Values.FirstOrDefault(s => s.HasValue && s.Value.ObjectSerial == serial);

            return c.HasValue ? World.Items.Get(c.Value.CorpseSerial) : null;
        }

        public void Clear()
        {
            _corpses.Clear();
        }
    }

    internal readonly struct CorpseInfo
    {
        public CorpseInfo(Serial corpseSerial, Serial objectSerial, Direction direction, bool isRunning)
        {
            CorpseSerial = corpseSerial;
            ObjectSerial = objectSerial;
            Direction = direction;
            IsRunning = isRunning;
        }

        public readonly Serial CorpseSerial, ObjectSerial;
        public readonly Direction Direction;
        public readonly bool IsRunning;
    }
}