using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Renderer
{
    public static class RenderExtentions
    {
        private const float ALPHA = .8f;

        public static Vector3 GetHueVector(int hue)
        {
            return GetHueVector(hue, false, false, false);
        }

        public static Vector3 GetHueVector(in int hue, bool partial, bool transparent, in bool noLighting)
        {
            if ((hue & 0x4000) != 0)
                transparent = true;
            if ((hue & 0x8000) != 0)
                partial = true;

            if (hue == 0)
                return new Vector3(0, 0, transparent ? ALPHA : 0);

            return new Vector3(hue & 0x0FFF, (noLighting ? 4 : 0) + (partial ? 2 : 1), transparent ? ALPHA : 0);
        }
    }
}