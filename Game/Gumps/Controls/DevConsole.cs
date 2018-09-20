using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.Controls
{
    class DevConsole : Gump
    {
        const ushort BLACK = 0x243A;
        const ushort GRAY = 0x248A;

        const int MAX_LINES = 15;

        private TextBox _textbox;

        public DevConsole() : base(0, 0)
        {
            CanCloseWithRightClick = false;
            CanCloseWithEsc = false;
            CanMove = true;

            X = 150;
            Y = 50;

            AddChildren(new GumpPicTiled(BLACK)
            {
                Width = 400,
                Height = 400,
            });

            AddChildren(_textbox = new TextBox(2, maxcharlength: -1, maxlength: 350, style: FontStyle.BlackBorder)
            {
                Width = 400,
                Height = 400,
                CanMove = true,
            });
        }

        public void Append(string line) => _textbox.SetText(line, true);

        public void AppendLine(string line)
        {
            if (_textbox.LinesCount + 1 > MAX_LINES)
            {
                _textbox.RemoveLineAt(0);
            }
            _textbox.SetText(_textbox.Text + line + "\n");
        }

        public void RemoveLine()
        {
            if (_textbox.LinesCount > 0)
            {

            }
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
        {
            return base.Draw(spriteBatch, position, hue);
        }
    }
}
