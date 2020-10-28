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
        public const byte SHADER_LAND = 6;
        public const byte SHADER_LAND_HUED = 7;
        public const byte SHADER_SPECTRAL = 10;
        public const byte SHADER_SHADOW = 12;
        public const byte SHADER_LIGHTS = 13;

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

                type = partial ? SHADER_PARTIAL_HUED : SHADER_HUED;

                if (gump)
                {
                    type |= 20;
                }
            }
            else
            {
                type = SHADER_NONE;
            }

            hueVector.X = hue;
            hueVector.Y = type;
            hueVector.Z = alpha;
        }
    }
}