using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;

namespace ClassicUO.Game.UI.Gumps
{
    internal class SimpleTimedTextGump : Gump
    {
        private readonly DateTime expireAt;

        public SimpleTimedTextGump(string text, Color color, TimeSpan duration) : this(text, 0x0059, duration)
        {
        }

        public SimpleTimedTextGump(string text, uint hue, TimeSpan duration) : base(0, 0)
        {
            expireAt = DateTime.Now.Add(duration);
            Add(new UOLabel(text ?? "", 1, (ushort)hue, TEXT_ALIGN_TYPE.TS_LEFT, 0, FontStyle.BlackBorder));
            WantUpdateSize = true;
        }

        public SimpleTimedTextGump(string text, uint hue, TimeSpan duration, int width) : base(0, 0)
        {
            expireAt = DateTime.Now.Add(duration);
            Add(new UOLabel(text ?? "", 1, (ushort)hue, TEXT_ALIGN_TYPE.TS_LEFT, width, FontStyle.BlackBorder));
            WantUpdateSize = true;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (DateTime.Now >= expireAt)
                Dispose();

            return base.Draw(batcher, x, y);
        }
    }
}
