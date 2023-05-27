using ClassicUO.Assets;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using System.Linq;

namespace ClassicUO.Game.UI.Gumps
{
    internal class Supporters : Gump
    {
        private const int WIDTH = 512;
        private const int HEIGHT = 512;


        private static readonly string[] SUPPORTERS = {
            "TazmanianTad - Developer",
            "Doskan - Random coffee bringer",
            "Auburok - Don't leave Brit Bank without TazUO",
            "IDiivil - Happily Organized Now",
            "Avernal",
            "d6punk - UO for life!",
            "Eora - Always looking for interesting adventures"
        };
        private AlphaBlendControl _background;

        private Texture2D image = PNGLoader.Instance.GetImageTexture(Path.Combine(CUOEnviroment.ExecutablePath, "ExternalImages", "tazuo.png"));

        private Label[] supporterLabels = new Label[SUPPORTERS.Length];

        private Line line;

        private double offset = 0.0;

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

            line = new Line(0, title.Height, WIDTH, 2, Color.Gray.PackedValue);
            Add(line);

            int y = line.Y + line.Height + 1, count = 0;
            foreach (string SUPPORTER in SUPPORTERS)
            {
                Label l = new Label(SUPPORTER, true, 0xffff, WIDTH, 255, FontStyle.BlackBorder, Assets.TEXT_ALIGN_TYPE.TS_CENTER, true);
                l.Y = y;
                y += l.Height + 1;
                Add(l);
                supporterLabels[count] = l;
                count++;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (image != null)
            {
                batcher.Draw(
                    image,
                    new Rectangle(x, y, image.Bounds.Width, image.Bounds.Height),
                    new Vector3(0, 0, 1)
                    );
            }

            offset += 0.9;

            int newY = line.Y + line.Height + 1;
            for (int i = 0; i < supporterLabels.Length; i++)
            {
                if (supporterLabels[i].Y <= line.Y + line.Height)
                    supporterLabels[i].IsVisible = false;
                else
                {
                    supporterLabels[i].Y -= (int)offset;
                    if (supporterLabels[i].Y < Height - supporterLabels[i].Height - 1)
                        supporterLabels[i].IsVisible = true;
                }
            }

            if (supporterLabels[supporterLabels.Length - 1].Y <= line.Y + line.Height)
                for (int ii = 0; ii < supporterLabels.Length; ii++)
                {
                    supporterLabels[ii].Y = Height + ((supporterLabels[ii].Height - 1) * ii);
                    supporterLabels[ii].IsVisible = false;
                }
            if (offset >= 1)
                offset = 0;

            Vector3 hue = ShaderHueTranslator.GetHueVector(0);
            batcher.DrawRectangle
            (
                SolidColorTextureCache.GetTexture(Color.Gray),
                x,
                y,
                Width - 3,
                Height + 1,
                hue
            );
            return base.Draw(batcher, x, y);
        }
    }
}
