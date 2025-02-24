// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Utility.Collections;

namespace ClassicUO.Game.UI.Gumps
{
    internal class TipNoticeGump : Gump
    {
        private readonly ExpandableScroll _background;
        private readonly ScrollArea _scrollArea;
        private readonly StbTextBox _textBox;

        public TipNoticeGump(World world, uint serial, byte type, string text) : base(world, serial, 0)
        {
            Height = 300;
            CanMove = true;
            CanCloseWithRightClick = true;

            _scrollArea = new ScrollArea
            (
                0,
                32,
                272,
                Height - 96,
                false
            );

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

        public override void Update()
        {
            base.Update();

            Height = _background.SpecialHeight;

            _scrollArea.Height = _background.Height - 96;
        }

        public override void OnButtonClick(int buttonID)
        {
            switch (buttonID)
            {
                case 1: // prev
                    NetClient.Socket.Send_TipRequest((ushort)LocalSerial, 0);
                    Dispose();

                    break;

                case 2: // next
                    NetClient.Socket.Send_TipRequest((ushort)LocalSerial, 1);

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