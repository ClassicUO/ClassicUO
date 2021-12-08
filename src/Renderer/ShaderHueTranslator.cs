#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace ClassicUO.Renderer
{
    internal static class ShaderHueTranslator
    {
        public const byte SHADER_NONE = 0;
        public const byte SHADER_HUED = 1;
        public const byte SHADER_PARTIAL_HUED = 2;
        public const byte SHADER_TEXT_HUE_NO_BLACK = 3;
        public const byte SHADER_TEXT_HUE = 4;
        public const byte SHADER_LAND = 5;
        public const byte SHADER_LAND_HUED = 6;
        public const byte SHADER_SPECTRAL = 7;
        public const byte SHADER_SHADOW = 8;
        public const byte SHADER_LIGHTS = 9;
        public const byte SHADER_EFFECT_HUED = 10;

        private const byte GUMP_OFFSET = 20;

        private const ushort SPECTRAL_COLOR_FLAG = 0x4000;

        public static readonly Vector3 SelectedHue = new Vector3(23, 1, 0);

        public static readonly Vector3 SelectedItemHue = new Vector3(0x0035, 1, 0);

        public static Vector3 GetHueVector(int hue)
        {
            return GetHueVector(hue, false, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetHueVector(int hue, bool partial, float alpha, bool gump = false, bool effect = false)
        {
            Vector3 hueVector;
            byte type;

            if ((hue & 0x8000) != 0)
            {
                partial = true;
                hue &= 0x7FFF;
            }

            if (hue == 0)
            {
                partial = false;
            }

            if ((hue & SPECTRAL_COLOR_FLAG) != 0)
            {
                type = SHADER_SPECTRAL;
            }
            else if (hue != 0)
            {
                hue -= 1;

                type = effect ? SHADER_EFFECT_HUED : partial ? SHADER_PARTIAL_HUED : SHADER_HUED;

                if (gump && !effect)
                {
                    type += GUMP_OFFSET;
                }
            }
            else
            {
                type = SHADER_NONE;
            }

            hueVector.X = hue;
            hueVector.Y = type;
            hueVector.Z = alpha;

            return hueVector;
        }
    }
}