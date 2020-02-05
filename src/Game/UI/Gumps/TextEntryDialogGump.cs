#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion

using ClassicUO.Game.UI.Controls;
using ClassicUO.Network;

namespace ClassicUO.Game.UI.Gumps
{
    internal class TextEntryDialogGump : Gump
    {
        private readonly TextBox _textBox;

        public TextEntryDialogGump(uint serial, int x, int y, byte variant, int maxlen, string text, string description, byte buttonid, byte parentid) : base(serial, 0)
        {
            CanMove = false;

            ControlInfo.IsModal = true;

            X = x;
            Y = y;

            Add(new GumpPic(0, 0, 0x0474, 0));

            Label label = new Label(text, false, 0x0386, font: 2)
            {
                X = 60, Y = 50
            };
            Add(label);

            label = new Label(description, false, 0x0386, font: 2)
            {
                X = 60,
                Y = 108
            };
            Add(label);

            Add(new GumpPic(60, 130, 0x0477, 0));

            _textBox = new TextBox(new TextEntry(1, unicode: false, hue: 0x0386, maxcharlength: maxlen, width: 200), true)
            {
                X = 71, Y = 137,
                NumericOnly = variant == 2
            };
            Add(_textBox);

            Add(new Button((int) ButtonType.Ok, 0x047B, 0x047C, 0x047D)
            {
                X = 117,
                Y = 190,
                ButtonAction = ButtonAction.Activate
            });

            Add(new Button((int) ButtonType.Cancel, 0x0478, 0x0478, 0x047A)
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
            switch ((ButtonType) buttonID)
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