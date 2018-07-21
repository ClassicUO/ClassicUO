using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game.Renderer
{
    public static class RenderExtentions
    {
        public static Vector3 GetHueVector(int hue)
            => GetHueVector(hue, false, false, false);

        public static Vector3 GetHueVector(int hue, bool partial, bool transparent, bool noLighting)
        {
            if ((hue & 0x4000) != 0)
                transparent = true;
            if ((hue & 0x8000) != 0)
                partial = true;

            if (hue == 0)
                return new Vector3(0, 0, transparent ? 0.5f : 0);

            return new Vector3(hue & 0x0FFF, (noLighting ? 4 : 0) + (partial ? 2 : 1), transparent ? 0.5f : 0);
        }
    }
}
