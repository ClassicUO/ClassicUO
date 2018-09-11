#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
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

namespace ClassicUO.Renderer
{
    public static class RenderExtentions
    {
        private const float ALPHA = .5f;

        public static Vector3 GetHueVector(int hue) => GetHueVector(hue, false, false, false);

        public static Vector3 GetHueVector(int hue, bool partial, bool transparent,  bool noLighting)
        {
            if ((hue & 0x4000) != 0)
            {
                transparent = true;


                //return new Vector3( 16843263 & 0x0FFF , (noLighting ? 4 : 0) + (partial ? 2 : 1), transparent ? ALPHA : 0); 
            }

            if ((hue & 0x8000) != 0)
            {
                partial = true;
            }

            return hue == 0 ? new Vector3(0, 0, transparent ? ALPHA : 0) : new Vector3(hue & 0x0FFF, (noLighting ? 4 : 0) + (partial ? 2 : 1), transparent ? ALPHA : 0);
        }
    }
}