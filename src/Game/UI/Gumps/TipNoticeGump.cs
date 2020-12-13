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
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Utility.Collections;

namespace ClassicUO.Game.UI.Gumps
{
    internal class TipNoticeGump : Gump
    {
        private readonly ExpandableScroll _background;
        private readonly Button _prev, _next;
        private readonly ScrollArea _scrollArea;
        private readonly StbTextBox _textBox;

        public TipNoticeGump(uint serial, byte type, string text) : base(serial, 0)
        {
            Height = 300;
            CanMove = true;
            CanCloseWithRightClick = true;
            _scrollArea = new ScrollArea(0, 32, 272, Height - 96, false);

            _textBox = new StbTextBox(6, -1, 220, isunicode: false)
            {
                Height = 20,
                X = 35,
                Y = 0,
                Width = 220,
                IsEditable = false
            };

            _textBox.SetText(text);
            Add(_background = new ExpandableScroll(0, 0, Height, 0x0820));
            _scrollArea.Add(_textBox);
            Add(_scrollArea);

            if (type == 0)
            {
                _background.TitleGumpID = 0x9CA;
                Add(new Button(1, 0x9cc, 0x9cc) { X = 35, ContainsByBounds = true, ButtonAction = ButtonAction.Activate });
                Add(new Button(2, 0x9cd, 0x9cd) { X = 240, ContainsByBounds = true, ButtonAction = ButtonAction.Activate });
            }
            else
            {
                _background.TitleGumpID = 0x9D2;
            }
        }


        public override void OnButtonClick(int buttonID)
        {
            switch (buttonID)
            {
                case 1: // prev
                    NetClient.Socket.Send(new PTipRequest((ushort) LocalSerial, 0));
                    Dispose();
                    break;
                case 2: // next
                    NetClient.Socket.Send(new PTipRequest((ushort) LocalSerial, 1));
                    Dispose();
                    break;
            }
        }


        //public override void OnPageChanged()
        //{
        //    Height = _background.SpecialHeight;
        //    _scrollArea.Height = _background.SpecialHeight - 96;

        //    foreach (Control c in _scrollArea.Children)
        //    {
        //        // if (c is ScrollAreaItem)
        //        {
        //            c.OnPageChanged();
        //        }
        //    }

        //    if (_prev != null && _next != null)
        //    {
        //        _prev.Y = _next.Y = _background.SpecialHeight - 53;
        //    }
        //}
    }
}