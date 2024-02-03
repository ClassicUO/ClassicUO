#region license

// Copyright (c) 2024, andreakarasho
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