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

using ClassicUO.Interfaces;
using ClassicUO.Utility;
using ClassicUO.Utility.Collections;

namespace ClassicUO.Game.Managers
{
    internal class UseItemQueue : IUpdateable
    {
        private readonly Deque<Serial> _actions = new Deque<Serial>();
        private long _timer;

        public UseItemQueue()
        {
            _timer = Engine.Ticks + 2000;
        }

        public void Update(double totalMS, double frameMS)
        {
            if (_timer <= totalMS)
            {
                _timer = (long) (totalMS + 1000);

                if (_actions.Count == 0)
                    return;

                Serial serial = _actions.RemoveFromFront();

                if (World.Get(serial) != null)
                {
                    if (serial.IsMobile)
                        GameActions.OpenPaperdoll(serial);
                    else
                        GameActions.DoubleClick(serial);
                }
            }
        }

        public void Add(Serial action)
        {
            _actions.AddToBack(action);
        }

        public void Clear()
        {
            _actions.Clear();
        }
    }
}