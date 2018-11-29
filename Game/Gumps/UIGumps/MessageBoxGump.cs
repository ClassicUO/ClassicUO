using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.Gumps.Controls;

namespace ClassicUO.Game.Gumps.UIGumps
{
    class MessageBoxGump : Gump
    {
        private readonly Action<MessageBoxGump> _action;

        public MessageBoxGump(int x, int y, int w, int h, string message, Action<MessageBoxGump> action) : base(0, 0)
        {
            CanMove = false;
            CanCloseWithRightClick = false;
            CanCloseWithEsc = false;
            AcceptMouseInput = false;

            ControlInfo.IsModal = true;
            ControlInfo.Layer = UILayer.Over;
            WantUpdateSize = false;

            X = x;
            Y = y;
            Width = w;
            Height = h;
            _action = action;

            AddChildren(new ResizePic(0x0A28)
            {
                Width = w, Height = h
            });

            AddChildren(new Label(message, false, 0x0386, Width - 90, 1)
            {
                X = 40,
                Y = 45
            });

            AddChildren(new Button(0, 0x0481, 0x0482, 0x0483)
            {
                X = (w / 2) - 13,
                Y = h - 45,
                ButtonAction = ButtonAction.Activate,               
            });
        }

        public override void OnButtonClick(int buttonID)
        {
            switch (buttonID)
            {
                case 0:
                    _action(this);
                    break;
            }
        }
    }
}
