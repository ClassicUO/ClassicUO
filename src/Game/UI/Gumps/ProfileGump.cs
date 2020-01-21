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

using System;

using ClassicUO.Game.UI.Controls;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Network;

namespace ClassicUO.Game.UI.Gumps
{
    internal class ProfileGump : Gump
    {
        private readonly string _originalText;
        private readonly ScrollArea _scrollArea;
        private readonly ExpandableScroll _scrollExp;
        private readonly MultiLineBox _textBox;
        private readonly GumpPic _gumpPic;
        private readonly HitBox _hitBox;
        private bool _isMinimized;
        private const int _diffY = 22;

        public ProfileGump(uint serial, string header, string footer, string body, bool canEdit) : base(serial == World.Player.Serial ? serial = Constants.PROFILE_LOCALSERIAL : serial, serial)
        {
            Height = 300 + _diffY;
            CanMove = true;
            AcceptKeyboardInput = true;
            CanCloseWithRightClick = true;

            Add(_gumpPic = new GumpPic(143, 0, 0x82D, 0));
            _gumpPic.MouseDoubleClick += _picBase_MouseDoubleClick;
            Add(_scrollExp = new ExpandableScroll(0, _diffY, Height - _diffY, 0x0820));
            _scrollArea = new ScrollArea(0, 32 + _diffY, 272, Height - (96 + _diffY), false);

            Control c = new Label(header, true, 0, font: 1, maxwidth: 140)
            {
                X = 85,
                Y = 0
            };
            _scrollArea.Add(c);
            AddHorizontalBar(_scrollArea, 92, 35, 220);

            _textBox = new MultiLineBox(new MultiLineEntry(1, -1, 0, 220, true, hue: 0), canEdit)
            {
                Height = FontsLoader.Instance.GetHeightUnicode(1, body, 220, TEXT_ALIGN_TYPE.TS_LEFT, 0x0),
                Width = 220,
                X = 35,
                Y = 0,
                Text = _originalText = body
            };
            _scrollArea.Add(_textBox);
            AddHorizontalBar(_scrollArea, 95, 35, 220);

            _scrollArea.Add(new Label(footer, true, 0, font: 1, maxwidth: 220)
            {
                X = 35,
                Y = 0
            });
            Add(_scrollArea);

            Add(_hitBox = new HitBox(143, 0, 23, 24));
            _hitBox.MouseUp += _hitBox_MouseUp;
        }


        public bool IsMinimized
        {
            get => _isMinimized;
            set
            {
                if (_isMinimized != value)
                {
                    _isMinimized = value;

                    _gumpPic.Graphic = value ? (ushort) 0x9D4 : (ushort) 0x82D;

                    if (value)
                    {
                        _gumpPic.X = 0;
                    }
                    else
                    {
                        _gumpPic.X = 143;
                    }

                    foreach (var c in Children)
                    {
                        c.IsVisible = !value;
                    }

                    _gumpPic.IsVisible = true;
                    WantUpdateSize = true;
                }
            }
        }


        private void _hitBox_MouseUp(object sender, Input.MouseEventArgs e)
        {
            if (e.Button == Input.MouseButtonType.Left && !IsMinimized)
            {
                IsMinimized = true;
            }
        }

        private void _picBase_MouseDoubleClick(object sender, Input.MouseDoubleClickEventArgs e)
        {
            if (e.Button == Input.MouseButtonType.Left && IsMinimized)
            {
                IsMinimized = false;
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            // necessary to avoid closing
        }

        public override void Dispose()
        {
            if (_originalText != _textBox.Text && World.Player != null && !World.Player.IsDestroyed && !NetClient.Socket.IsDisposed && NetClient.Socket.IsConnected) NetClient.Socket.Send(new PProfileUpdate(World.Player.Serial, _textBox.Text));
            base.Dispose();
        }

        public override void Update(double totalMS, double frameMS)
        {
            /*WantUpdateSize = true;

            if(_textBox.Height > 0)
                _textBox.Height = Height - 150;*/
            if (!_textBox.IsDisposed && _textBox.IsChanged)
            {
                _textBox.Height = Math.Max(FontsLoader.Instance.GetHeightUnicode(1, _textBox.TxEntry.Text, 220, TEXT_ALIGN_TYPE.TS_LEFT, 0x0) + 20, 40);

                foreach (Control c in _scrollArea.Children)
                {
                    if (c is ScrollAreaItem)
                        c.OnPageChanged();
                }
            }

            base.Update(totalMS, frameMS);
        }

        private void AddHorizontalBar(ScrollArea area, ushort start, int x, int width)
        {
            var startBounds = GumpsLoader.Instance.GetTexture(start);
            var middleBounds = GumpsLoader.Instance.GetTexture((ushort) (start + 1));
            var endBounds = GumpsLoader.Instance.GetTexture((ushort) (start + 2));

            PrivateContainer container = new PrivateContainer();

            Control c = new GumpPic(x, 0, start, 0);
            
            container.Add(c);
            container.Add(new GumpPicWithWidth(x + startBounds.Width, (startBounds.Height - middleBounds.Height) >> 1, (ushort) (start + 1), 0, width - startBounds.Width - endBounds.Width));
            container.Add(new GumpPic(x + width - endBounds.Width, 0, (ushort) (start + 2), 0));

            area.Add(container);
        }

        class PrivateContainer : Control
        {

        }

        public override void OnPageChanged()
        {
            Height = _scrollExp.SpecialHeight + _diffY;
            _scrollArea.Height = _scrollExp.SpecialHeight - (96 + _diffY);

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