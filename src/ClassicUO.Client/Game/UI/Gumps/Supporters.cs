using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class Supporters : Gump
    {
        private const int WIDTH = 450;
        private const int HEIGHT = 400;


        private readonly string[] SUPPORTERS = { "TazmanianTad - Developer" };
        private AlphaBlendControl _background;

        public Supporters() : base(0, 0)
        {
            Width = WIDTH;
            Height = HEIGHT;
            X = (Client.Game.Window.ClientBounds.Width - Width) >> 1;
            Y = (Client.Game.Window.ClientBounds.Height - Height) >> 1;

            CanCloseWithEsc = true;
            CanCloseWithRightClick = true;
            CanMove = true;
            AcceptMouseInput = true;

            _background = new AlphaBlendControl();
            _background.Width = WIDTH;
            _background.Height = HEIGHT;
            _background.X = 1;
            _background.Y = 1;
            Add(_background);

            Label title = new Label("TazUO supporters and honorable mentions<br>And a special thanks to all the ClassicUO devs that made this possible!", true, 0xffff, WIDTH, 255, FontStyle.BlackBorder, Assets.TEXT_ALIGN_TYPE.TS_CENTER, true);
            title.Y = 1;
            Add(title);

            Line line = new Line(0, title.Height, WIDTH, 2, Color.Gray.PackedValue);
            Add(line);

            int y = line.Y + line.Height + 1;
            foreach (string SUPPORTER in SUPPORTERS)
            {
                Label l = new Label(SUPPORTER, true, 0xffff, WIDTH, 255, FontStyle.BlackBorder, Assets.TEXT_ALIGN_TYPE.TS_CENTER, true);
                l.Y = y;
                y += 5;
                Add(l);
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            Vector3 hue = ShaderHueTranslator.GetHueVector(0);
            batcher.DrawRectangle
            (
                SolidColorTextureCache.GetTexture(Color.Gray),
                x,
                y,
                Width-3,
                Height+1,
                hue
            );
            return base.Draw(batcher, x, y);
        }
    }
}
