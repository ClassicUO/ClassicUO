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

using ClassicUO.Game.GameObjects;
using ClassicUO.Interfaces;
using ClassicUO.Utility.Collections;

namespace ClassicUO.Game.Managers
{
    internal class UseItemQueue : IUpdateable
    {
        private readonly Deque<uint> _actions = new Deque<uint>();
        private long _timer;


        public UseItemQueue()
        {
            // ## BEGIN - END ## //  ORIG
            //_timer = Time.Ticks + 1000;
            // ## BEGIN - END ## // 
            _timer = Time.Ticks + 600;// 1000;
            // ## BEGIN - END ## //
        }

        public void Update(double totalTime, double frameTime)
        {
            if (_timer < Time.Ticks)
            {
                // ## BEGIN - END ## //  ORIG
                //_timer = Time.Ticks + 1000;
                // ## BEGIN - END ## // 
                _timer = Time.Ticks + 600;// 1000;
                // ## BEGIN - END ## //

                if (_actions.Count == 0)
                {
                    return;
                }

                uint serial = _actions.RemoveFromFront();

                if (World.Get(serial) != null)
                {
                    if (SerialHelper.IsMobile(serial))
                    {
                        GameActions.OpenPaperdoll(serial);
                    }
                    else
                    {
                        GameActions.DoubleClick(serial);
                    }
                }
            }
        }

        public void Add(uint serial)
        {
            foreach (uint s in _actions)
            {
                if (serial == s)
                {
                    return;
                }
            }

            _actions.AddToBack(serial);
        }

        public void Clear()
        {
            _actions.Clear();
        }

        public void ClearCorpses()
        {
            for (int i = 0; i < _actions.Count; i++)
            {
                Entity entity = World.Get(_actions[i]);

                if (entity == null)
                {
                    continue;
                }

                if (entity is Item it && it.IsCorpse)
                {
                    _actions.RemoveAt(i--);
                }
            }
        }
    }
}