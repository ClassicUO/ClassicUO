// SPDX-License-Identifier: BSD-2-Clause

using Microsoft.Xna.Framework;
using System.Runtime.CompilerServices;

namespace ClassicUO.Renderer
{
    public static class ShaderHueTranslator
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

                // TODO: check if effect + partial is a thing. Because on shader the effect uses the G component instead of R to get the hue index
                type = effect && !partial ? SHADER_EFFECT_HUED : partial ? SHADER_PARTIAL_HUED : SHADER_HUED;

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
