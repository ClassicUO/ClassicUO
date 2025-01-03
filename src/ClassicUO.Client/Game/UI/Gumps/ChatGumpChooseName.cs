// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.UI.Controls;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Resources;

namespace ClassicUO.Game.UI.Gumps
{
    internal class ChatGumpChooseName : Gump
    {
        private readonly StbTextBox _textBox;

        public ChatGumpChooseName(World world) : base(world, 0, 0)
        {
            CanMove = false;
            AcceptKeyboardInput = true;
            AcceptMouseInput = true;
            WantUpdateSize = true;

            X = 250;
            Y = 100;
            Width = 210;
            Height = 330;

            Add
            (
                new AlphaBlendControl
                {
                    Alpha = 1f,
                    Width = Width,
                    Height = Height
                }
            );

            Add
            (
                new BorderControl
                (
                    0,
                    0,
                    Width,
                    Height,
                    4
                )
            );

            Label text = new Label
            (
                ResGumps.ChooseName,
                true,
                23,
                Width - 17,
                3
            )
            {
                X = 6,
                Y = 6
            };

            Add(text);

            int BORDER_SIZE = 4;

            BorderControl border = new BorderControl
            (
                0,
                text.Y + text.Height,
                Width,
                27,
                BORDER_SIZE
            );

            Add(border);

            text = new Label
            (
                ResGumps.Name,
                true,
                0x033,
                0,
                3
            )
            {
                X = 6,
                Y = border.Y + 2
            };

            Add(text);

            int x = text.X + text.Width + 2;

            _textBox = new StbTextBox
            (
                1,
                -1,
                Width - x - 17,
                true,
                FontStyle.Fixed,
                0x0481
            )
            {
                X = x,
                Y = text.Y,
                Width = Width - -x - 17,
                Height = 27 - BORDER_SIZE * 2
            };

            Add(_textBox);

            Add
            (
                new BorderControl
                (
                    0,
                    text.Y + text.Height,
                    Width,
                    27,
                    BORDER_SIZE
                )
            );

            // close
            Add
            (
                new Button(0, 0x0A94, 0x0A95, 0x0A94)
                {
                    X = Width - 19 - BORDER_SIZE,
                    Y = Height - 19 - BORDER_SIZE * 1,
                    ButtonAction = ButtonAction.Activate
                }
            );

            // ok
            Add
            (
                new Button(1, 0x0A9A, 0x0A9B, 0x0A9A)
                {
                    X = Width - 19 * 2 - BORDER_SIZE,
                    Y = Height - 19 - BORDER_SIZE * 1,
                    ButtonAction = ButtonAction.Activate
                }
            );
        }


        public override void OnButtonClick(int buttonID)
        {
            if (buttonID == 0) // close
            {
            }
            else if (buttonID == 1) // ok
            {
                if (!string.IsNullOrWhiteSpace(_textBox.Text))
                {
                    NetClient.Socket.Send_OpenChat(_textBox.Text);
                }
            }

            Dispose();
        }
    }
}