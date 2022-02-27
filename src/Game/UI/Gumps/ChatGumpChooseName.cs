#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using ClassicUO.Game.UI.Controls;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Resources;

namespace ClassicUO.Game.UI.Gumps
{
    internal class ChatGumpChooseName : Gump
    {
        private readonly StbTextBox _textBox;

        public ChatGumpChooseName() : base(0, 0)
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