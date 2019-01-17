using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.UI.Controls;
using ClassicUO.Network;

namespace ClassicUO.Game.UI.Gumps
{
    class TextEntryDialogGump : Gump
    {
        private readonly TextBox _textBox;

        public TextEntryDialogGump(Serial serial, int x, int y, byte variant, int maxlen, string text, string description, byte buttonid, byte parentid) : base(serial, 0)
        {
            CanMove = false;

            ControlInfo.IsModal = true;

            X = x;
            Y = y;

            AddChildren(new GumpPic(0, 0, 0x0474, 0));

            Label label = new Label(text, false, 0x0386, font: 2)
            {
                X = 60, Y = 50
            };
            AddChildren(label);

            label = new Label(description, false, 0x0386, font: 2)
            {
                X = 60,
                Y = 108
            };
            AddChildren(label);

            AddChildren(new GumpPic(60, 130, 0x0477, 0));

            _textBox = new TextBox(new TextEntry(1, unicode: false, hue: 0x0386, maxcharlength: maxlen, width: 200), true)
            {
                X = 71, Y = 137,
                NumericOnly = variant == 2
            };
            AddChildren(_textBox);

            AddChildren(new Button((int)ButtonType.Ok, 0x047B, 0x047C, 0x047D)
            {
                X = 117,
                Y = 190,
                ButtonAction = ButtonAction.Activate
            });

            AddChildren(new Button((int)ButtonType.Cancel, 0x0478, 0x0478, 0x047A)
            {
                X = 204,
                Y = 190,
                ButtonAction = ButtonAction.Activate
            });

            ButtonID = buttonid;
            ParentID = parentid;
        }

        public byte ParentID { get; }
        public byte ButtonID { get; }

        public override void OnButtonClick(int buttonID)
        {
            switch ((ButtonType)buttonID)
            {
                case ButtonType.Ok:
                    NetClient.Socket.Send(new PTextEntryDialogResponse(LocalSerial, ButtonID, _textBox.Text, true));
                    Dispose();
                    break;
                case ButtonType.Cancel:
                    NetClient.Socket.Send(new PTextEntryDialogResponse(LocalSerial, ButtonID, _textBox.Text, false));
                    Dispose();
                    break;
            }
        }

        private enum ButtonType
        {
            Ok,
            Cancel
        }
    }
}
