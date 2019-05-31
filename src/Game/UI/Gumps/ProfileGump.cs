#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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

using System;

using ClassicUO.Game.UI.Controls;
using ClassicUO.IO;
using ClassicUO.Network;

namespace ClassicUO.Game.UI.Gumps
{
    internal class ProfileGump : Gump
    {
        private readonly ScrollArea _scrollArea;
        private readonly MultiLineBox _textBox;
        private readonly ExpandableScroll _scrollExp;
        private readonly string _originalText;

        public ProfileGump(Serial serial, string header, string footer, string body, bool canEdit) : base(serial, serial)
        {
            Height = 300;
            CanMove = true;
            AcceptKeyboardInput = true;
            Add(_scrollExp = new ExpandableScroll(0, 0, Height, 0x0820));
            _scrollArea = new ScrollArea(0, 32, 272, Height - 96, false);
            Control c = new Label(header, true, 0, font: 1, maxwidth: 140)
            {
                X = 85,
                Y = 0
            };
            _scrollArea.Add(c);
            AddHorizontalBar(_scrollArea, 92, 35, 220);
            _textBox = new MultiLineBox(new MultiLineEntry(1, -1, 0, 220, true, hue: 0), canEdit)
            {
                Height = FileManager.Fonts.GetHeightUnicode(1, body, 220, IO.Resources.TEXT_ALIGN_TYPE.TS_LEFT, 0x0),
                X = 35,
                Y = 0,
                Text = _originalText = body
            };
            _scrollArea.Add(_textBox);
            AddHorizontalBar(_scrollArea, 95, 35, 220);
            _scrollArea.Add(new Label(footer, true, 0, font: 1, maxwidth: 220)
            {
                X = 35,
                Y = 0,
            });
            Add(_scrollArea);
        }

        /*protected override void OnMouseWheel(MouseEvent delta)
        {
            switch (delta)
            {
                case MouseEvent.WheelScrollUp:
                    _scrollBar.Value -= 5;

                    break;
                case MouseEvent.WheelScrollDown:
                    _scrollBar.Value += 5;

                    break;
            }
        }*/


        public override void OnButtonClick(int buttonID)
        {
            // necessary to avoid closing
        }

        public override void Dispose()
        {
            if(_originalText != _textBox.Text && World.Player != null && !World.Player.IsDestroyed && !NetClient.Socket.IsDisposed && NetClient.Socket.IsConnected)
            {
                NetClient.Socket.Send(new PProfileUpdate(World.Player.Serial, _textBox.Text));
            }
            base.Dispose();
        }

        public override void Update(double totalMS, double frameMS)
        {
            /*WantUpdateSize = true;

            if(_textBox.Height > 0)
                _textBox.Height = Height - 150;*/
            if(!_textBox.IsDisposed && _textBox.IsChanged)
            {
                _textBox.Height = Math.Max(FileManager.Fonts.GetHeightUnicode(1, _textBox.TxEntry.Text, 220, IO.Resources.TEXT_ALIGN_TYPE.TS_LEFT, 0x0) + 20, 40);
                foreach(Control c in _scrollArea.Children)
                {
                    if (c is ScrollAreaItem)
                        c.OnPageChanged();
                }
            }

            base.Update(totalMS, frameMS);
        }

        private void AddHorizontalBar(ScrollArea area, Graphic start, int x, int width)
        {
            var startBounds = FileManager.Gumps.GetTexture(start);
            var middleBounds = FileManager.Gumps.GetTexture((Graphic)(start + 1));
            var endBounds = FileManager.Gumps.GetTexture((Graphic)(start + 2));
            int y = -startBounds.Height;
            Control c;
            c = new GumpPic(x, (y >> 1) - 6, (Graphic)start, 0);
            c.Add(new GumpPicWithWidth(startBounds.Width, ((startBounds.Height - middleBounds.Height) >> 1), (Graphic)(start + 1), 0, width - startBounds.Width - endBounds.Width));
            c.Add(new GumpPic(width - endBounds.Width, 0, (Graphic)(start + 2), 0));
            area.Add(c);
        }

        public override void OnPageChanged()
        {
            Height = _scrollExp.SpecialHeight;
            _scrollArea.Height = _scrollExp.SpecialHeight - 96;
            foreach (Control c in _scrollArea.Children)
            {
                if (c is ScrollAreaItem)
                    c.OnPageChanged();
            }
        }

        public override void OnKeyboardReturn(int textID, string text)
        {
            if ((MultiLineBox.PasteRetnCmdID & textID) != 0 && !string.IsNullOrEmpty(text)) _textBox.TxEntry.InsertString(text.Replace("\r", string.Empty));
        }
    }
}