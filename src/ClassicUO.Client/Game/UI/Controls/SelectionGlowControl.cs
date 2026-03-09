using System;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class SelectionGlowControl : Control
    {
        private const int GLOW_LAYERS = 8;
        private const float PULSE_SPEED = 2.5f;

        public Color GlowColor { get; set; } = new Color(0, 180, 255);

        public SelectionGlowControl()
        {
            AcceptMouseInput = false;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed || !IsVisible)
                return false;

            float timeSeconds = Time.Ticks / 1000.0f;
            float pulse = 0.60f + 0.40f * (float)Math.Sin(timeSeconds * PULSE_SPEED);
            float baseAlpha = Alpha * pulse;

            var tex = SolidColorTextureCache.GetTexture(GlowColor);

            for (int i = GLOW_LAYERS; i >= 1; i--)
            {
                float t = GLOW_LAYERS > 1 ? (float)(GLOW_LAYERS - i) / (GLOW_LAYERS - 1) : 1f;
                float layerAlpha = (0.02f + 0.10f * t) * baseAlpha;
                Vector3 hue = ShaderHueTranslator.GetHueVector(0, false, layerAlpha);
                int pad = i * 2;

                batcher.Draw(tex, new Rectangle(x - pad, y - pad, Width + pad * 2, pad), hue);
                batcher.Draw(tex, new Rectangle(x - pad, y + Height, Width + pad * 2, pad), hue);
                batcher.Draw(tex, new Rectangle(x - pad, y, pad, Height), hue);
                batcher.Draw(tex, new Rectangle(x + Width, y, pad, Height), hue);
            }

            float borderAlpha = 0.80f * baseAlpha;
            Vector3 borderHue = ShaderHueTranslator.GetHueVector(0, false, borderAlpha);
            batcher.DrawRectangle(tex, x, y, Width, Height, borderHue);

            float innerAlpha = 0.45f * baseAlpha;
            Vector3 innerHue = ShaderHueTranslator.GetHueVector(0, false, innerAlpha);
            batcher.DrawRectangle(tex, x + 1, y + 1, Width - 2, Height - 2, innerHue);

            return true;
        }
    }
}
