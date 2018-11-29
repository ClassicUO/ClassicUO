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
        private readonly HashSet<Static> _activeStatics = new HashSet<Static>();
        private readonly List<Static> _toRemove = new List<Static>();

        public void Update(double totalMS, double frameMS)
        {
            foreach (Static k in _activeStatics)
            {
                k.Update(totalMS, frameMS);

                if (k.IsDisposed || k.OverHeads.Count <= 0 && (k.Effect == null || k.Effect.IsDisposed))
                    _toRemove.Add(k);
            }

            if (_toRemove.Count > 0)
            {
                _toRemove.ForEach( s => s.Dispose());
                _toRemove.Clear();
            }
        }

        public void Add(Static stat)
        {
            if (!stat.IsDisposed && (stat.OverHeads.Count > 0 || stat.Effect != null) && !_activeStatics.Contains(stat))
                _activeStatics.Add(stat);
        }
    }
}