// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Network;

namespace ClassicUO.Game.UI.Gumps
{
    internal class ProfileGump : Gump
    {
        private const int _diffY = 22;
        private readonly DataBox _databox;
        private readonly GumpPic _gumpPic;
        private readonly HitBox _hitBox;
        private bool _isMinimized;
        private readonly string _originalText;
        private readonly ScrollArea _scrollArea;
        private readonly StbTextBox _textBox;

        public ProfileGump(World world, uint serial, string header, string footer, string body, bool canEdit) : base(world, serial, 0)
        {
            Height = 300 + _diffY;
            CanMove = true;
            AcceptKeyboardInput = true;
            CanCloseWithRightClick = true;

            Add(_gumpPic = new GumpPic(143, 0, 0x82D, 0));
            _gumpPic.MouseDoubleClick += _picBase_MouseDoubleClick;

            Add(new ExpandableScroll(0, _diffY, Height - _diffY, 0x0820));

            _scrollArea = new ScrollArea
            (
                22,
                32 + _diffY,
                272 - 22,
                Height - (96 + _diffY),
                false
            );

            Label topText = new Label
            (
                header,
                true,
                0,
                font: 1,
                maxwidth: 140
            )
            {
                X = 53,
                Y = 6
            };

            _scrollArea.Add(topText);

            int offsetY = topText.Height - 15;

            _scrollArea.Add(new GumpPic(4, offsetY, 0x005C, 0));

            _scrollArea.Add
            (
                new GumpPicTiled
                (
                    56,
                    offsetY,
                    138,
                    0,
                    0x005D
                )
            );

            _scrollArea.Add(new GumpPic(194, offsetY, 0x005E, 0));

            offsetY += 44;

            _textBox = new StbTextBox(1, -1, 220)
            {
                Width = 220,
                X = 4,
                Y = offsetY,
                IsEditable = canEdit,
                Multiline = true
            };

            _originalText = body;
            _textBox.TextChanged += _textBox_TextChanged;
            _textBox.SetText(body);
            _scrollArea.Add(_textBox);


            _databox = new DataBox(4, _textBox.Height + 3, 1, 1);
            _databox.WantUpdateSize = true;

            _databox.Add(new GumpPic(4, 0, 0x005F, 0));

            _databox.Add
            (
                new GumpPicTiled
                (
                    13,
                    0 + 9,
                    197,
                    0,
                    0x0060
                )
            );

            _databox.Add(new GumpPic(210, 0, 0x0061, 0));

            _databox.Add
            (
                new Label
                (
                    footer,
                    true,
                    0,
                    font: 1,
                    maxwidth: 220
                )
                {
                    X = 2,
                    Y = 26
                }
            );

            Add(_scrollArea);
            _scrollArea.Add(_databox);

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

                    foreach (Control c in Children)
                    {
                        c.IsVisible = !value;
                    }

                    _gumpPic.IsVisible = true;
                    WantUpdateSize = true;
                }
            }
        }


        public override void Update()
        {
            _scrollArea.Height = Height - (96 + _diffY);
            _databox.Y = _textBox.Bounds.Bottom + 3;
            _databox.WantUpdateSize = true;
            WantUpdateSize = true;

            base.Update();
        }

        private void _textBox_TextChanged(object sender, EventArgs e)
        {
            _textBox.Height = Math.Max
            (
                Client.Game.UO.FileManager.Fonts.GetHeightUnicode
                (
                    1,
                    _textBox.Text,
                    220,
                    TEXT_ALIGN_TYPE.TS_LEFT,
                    0x0
                ) + 5,
                20
            );
        }


        private void _hitBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtonType.Left && !IsMinimized)
            {
                IsMinimized = true;
            }
        }

        private void _picBase_MouseDoubleClick(object sender, MouseDoubleClickEventArgs e)
        {
            if (e.Button == MouseButtonType.Left && IsMinimized)
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
            if (_originalText != _textBox.Text && World.Player != null && !World.Player.IsDestroyed && NetClient.Socket.IsConnected)
            {
                NetClient.Socket.Send_ProfileUpdate(LocalSerial, _textBox.Text);
            }

            base.Dispose();
        }
    }
}