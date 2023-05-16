using ClassicUO.Assets;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;

namespace ClassicUO.Game.UI.Gumps
{
    internal class SimpleFadingTextGump : Gump
    {
        private readonly TimeSpan duration;
        private readonly DateTime creation = DateTime.Now;

        public SimpleTimedTextGump(string text, int width, Color color, TimeSpan duration) : base(0, 0)
        {
            this.duration = duration;

            Add(new TextBox(text, TrueTypeLoader.EMBEDDED_FONT, 20, width, color));
            WantUpdateSize = true;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if ((creation + duration) > DateTime.Now)
                Dispose();

            return base.Draw(batcher, x, y);
        }
    }
}
