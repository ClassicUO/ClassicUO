// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Network;

namespace ClassicUO.Game.UI.Gumps
{
    internal class TextEntryDialogGump : Gump
    {
        private readonly StbTextBox _textBox;

        public TextEntryDialogGump
        (
            World world,
            uint serial,
            int x,
            int y,
            byte variant,
            int maxlen,
            string text,
            string description,
            byte buttonid,
            byte parentid
        ) : base(world, serial, 0)
        {
            CanMove = false;
            IsFromServer = true;

            IsModal = true;

            X = x;
            Y = y;

            GumpPic background = new GumpPic(0, 0, 0x0474, 0);
            Add(background);

            Label label = new Label(text, false, 0x0386, font: 2, maxwidth: background.Width - 110)
            {
                X = 60, Y = 50
            };

            Add(label);

            label = new Label(description, false, 0x0386, font: 2, maxwidth: background.Width - 110)
            {
                X = 60,
                Y = 108
            };

            Add(label);

            Add(new GumpPic(60, 130, 0x0477, 0));

            _textBox = new StbTextBox(1, isunicode: false, hue: 0x0386, max_char_count: maxlen)
            {
                X = 71, Y = 137,
                Width = 250,
                NumbersOnly = variant == 2
            };

            Add(_textBox);

            Add
            (
                new Button((int) ButtonType.Ok, 0x047B, 0x047C, 0x047D)
                {
                    X = 117,
                    Y = 190,
                    ButtonAction = ButtonAction.Activate
                }
            );

            Add
            (
                new Button((int) ButtonType.Cancel, 0x0478, 0x0478, 0x047A)
                {
                    X = 204,
                    Y = 190,
                    ButtonAction = ButtonAction.Activate
                }
            );

            ButtonID = buttonid;
            ParentID = parentid;

            UIManager.KeyboardFocusControl = _textBox;
            _textBox.SetKeyboardFocus();
        }

        public byte ParentID { get; }
        public byte ButtonID { get; }

        public override void OnButtonClick(int buttonID)
        {
            switch ((ButtonType) buttonID)
            {
                case ButtonType.Ok:
                    NetClient.Socket.Send_TextEntryDialogResponse(LocalSerial,
                                                                  ParentID,
                                                                  ButtonID,
                                                                  _textBox.Text,
                                                                  true);

                    Dispose();

                    break;

                case ButtonType.Cancel:
                    NetClient.Socket.Send_TextEntryDialogResponse(LocalSerial,
                                                                  ParentID,
                                                                  ButtonID,
                                                                  _textBox.Text,
                                                                  false);

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