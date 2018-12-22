using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.UIGumps
{
    class DebugGump : Gump
    {
        private readonly StringBuilder _sb = new StringBuilder();
        private readonly Label _label;

        public DebugGump() : base(0, 0)
        {
            CanMove = true;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = false;
            AcceptMouseInput = false;
            AcceptKeyboardInput = false;

            Width = 500;
            Height = 200;

            AddChildren(new CheckerTrans()
            {
                Width = Width , Height = Height
            });
            AddChildren(_label = new Label("", true, 0x35, font: 1, style: FontStyle.BlackBorder)
            {
                X = 10, Y = 10
            });

            ControlInfo.Layer = UILayer.Over;

            WantUpdateSize = false;
        }


        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);
        }

        public override bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
        {
            //_label.Text = _sb.ToString();
            return base.Draw(batcher, position, hue);
        }
    }
}
