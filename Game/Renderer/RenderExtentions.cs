using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Renderer
{
    public static class RenderExtentions
    {
        private const float ALPHA = .5f;

        public static Vector3 GetHueVector(int hue) => GetHueVector(hue, false, false, false);

        public static Vector3 GetHueVector(in int hue, bool partial, bool transparent, in bool noLighting)
        {
            if ((hue & 0x4000) != 0)
                transparent = true;
            if ((hue & 0x8000) != 0)
                partial = true;

            return hue == 0 ? new Vector3(0, 0, transparent ? ALPHA : 0) : new Vector3(hue & 0x0FFF, (noLighting ? 4 : 0) + (partial ? 2 : 1), transparent ? ALPHA : 0);
        }
    }
}