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

namespace ClassicUO.Renderer
{
    public enum ShadersEffectType
    {
        None = 0,
        Hued = 1,
        PartialHued = 2,
        Land = 6,
        LandHued = 7,
        Spectral = 10,
        Shadow = 12
    }

    public static class RenderExtentions
    {
        private const ushort SPECTRAL_COLOR_FLAG = 0x4000;

        public static Vector3 SelectedHue { get; } = new Vector3(27, 1, 0);

        public static Vector3 GetHueVector(int hue)
        {
            return GetHueVector(hue, false, 0, false);
        }

        public static Vector3 GetHueVector(int hue, bool partial, float alpha, bool noLighting)
        {
            ShadersEffectType type;

            if ((hue & 0x8000) != 0)
            {
                partial = true;
                hue &= 0x7FFF;
            }

            if (hue == 0)
                partial = false;

            if ((hue & SPECTRAL_COLOR_FLAG) != 0)
                type = ShadersEffectType.Spectral;
            else if (hue != 0)
            {
                if (partial)
                    type = ShadersEffectType.PartialHued;
                else
                    type = ShadersEffectType.Hued;
            }
            else
                type = ShadersEffectType.None;

            return new Vector3(hue, (int) type, alpha);
        }

        public static Vector3 GetHueVector(int hue, ShadersEffectType type)
        {
            return new Vector3(hue, (int) type, 0);
        }
    }
}