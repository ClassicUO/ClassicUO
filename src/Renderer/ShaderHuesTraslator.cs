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

using System.Runtime.CompilerServices;

using Microsoft.Xna.Framework;

namespace ClassicUO.Renderer
{
    internal static class ShaderHuesTraslator
    {
        public const byte SHADER_NONE = 0;
        public const byte SHADER_HUED = 1;
        public const byte SHADER_PARTIAL_HUED = 2;
        public const byte SHADER_TEXT_HUE_NO_BLACK = 3;
        public const byte SHADER_TEXT_HUE = 4;
        public const byte SHADER_LAND = 6;
        public const byte SHADER_LAND_HUED = 7;
        public const byte SHADER_SPECTRAL = 10;
        public const byte SHADER_SHADOW = 12;
        public const byte SHADER_LIGHTS = 13;
        public const byte COLOR_SWAP = 0x20;

        private const ushort SPECTRAL_COLOR_FLAG = 0x4000;

        public static readonly Vector3 SelectedHue = new Vector3(23, 1, 0);

        public static readonly Vector3 SelectedItemHue = new Vector3(0x0035, 1, 0);

        public static void GetHueVector(ref Vector3 hueVector, int hue)
        {
            GetHueVector(ref hueVector, hue, false, 0);
        }

        [MethodImpl(256)]
        public static void GetHueVector(ref Vector3 hueVector, int hue, bool partial, float alpha, bool gump = false)
        {
            byte type;

            if ((hue & 0x8000) != 0)
            {
                partial = true;
                hue &= 0x7FFF;
            }

            if (hue == 0)
                partial = false;

            if ((hue & SPECTRAL_COLOR_FLAG) != 0)
                type = SHADER_SPECTRAL;
            else if (hue != 0)
            {
                if (partial)
                    type = SHADER_PARTIAL_HUED;
                else
                    type = SHADER_HUED;
            }
            else
                type = SHADER_NONE;

            if (gump)
                type += COLOR_SWAP;

            hueVector.X = hue;
            hueVector.Y = type;
            hueVector.Z = alpha;
        }
    }
}