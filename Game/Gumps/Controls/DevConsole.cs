using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.Controls
{
    class DevConsole : Gump
    {
        const ushort BLACK = 0x243A;
        const ushort GRAY = 0x248A;

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

            AddChildren(new TextBox()
            {
                Width = 400,
                Height = 400,
                CanMove = true,
                MultiLine = false,
                AllowTAB = true
            });
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position)
        {
            return base.Draw(spriteBatch, position);
        }
    }
}
