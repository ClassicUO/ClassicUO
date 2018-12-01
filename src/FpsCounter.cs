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
using Microsoft.Xna.Framework;

namespace ClassicUO
{
    public class FpsCounter
    {
        private int _totalFrames;

        public int FPS { get; private set; }

        public double CurrentFpsTime { get; private set; }

        public void Update(GameTime gameTime)
        {
            CurrentFpsTime += gameTime.ElapsedGameTime.TotalSeconds;

            if (CurrentFpsTime >= 1.0)
            {
                FPS = _totalFrames;
                _totalFrames = 0;
                CurrentFpsTime = 0;
            }
        }

        public void IncreaseFrame()
        {
            _totalFrames++;
        }
    }
}