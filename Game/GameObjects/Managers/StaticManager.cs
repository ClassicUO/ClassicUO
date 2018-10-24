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

using System.Collections.Generic;
using ClassicUO.Interfaces;

namespace ClassicUO.Game.GameObjects.Managers
{
    public class StaticManager : IUpdateable
    {
        private readonly List<Static> _activeStatics = new List<Static>();

        public void Update(double totalMS, double frameMS)
        {
            for (int i = 0; i < _activeStatics.Count; i++)
            {
                _activeStatics[i].Update(totalMS, frameMS);
                if (_activeStatics[i].IsDisposed || _activeStatics[i].OverHeads.Count <= 0)
                    _activeStatics.RemoveAt(i);
            }
        }

        public void Add(Static stat)
        {
            if (!stat.IsDisposed && stat.OverHeads.Count > 0)
                _activeStatics.Add(stat);
        }
    }
}