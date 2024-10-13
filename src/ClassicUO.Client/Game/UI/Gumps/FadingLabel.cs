using ClassicUO.Assets;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class FadingLabel : Label
    {
        private readonly int tickSpeed;
        private int c = 0;

        public FadingLabel(int tickSpeed, string text, bool isunicode, ushort hue, int maxwidth = 0, byte font = 255, FontStyle style = FontStyle.None, TEXT_ALIGN_TYPE align = TEXT_ALIGN_TYPE.TS_LEFT, bool ishtml = false) : base(text, isunicode, hue, maxwidth, font, style, align, ishtml)
        {
            this.tickSpeed = tickSpeed;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (c >= tickSpeed)
                Alpha -= 0.01f;
            if (Alpha <= 0f)
                Dispose();
            c++;

            batcher.Draw(SolidColorTextureCache.GetTexture(Color.Green),
                new Rectangle(x, y, Width, Height),
                new Vector3(1, 0, Alpha)
                );

            return base.Draw(batcher, x, y);
        }
    }
}
