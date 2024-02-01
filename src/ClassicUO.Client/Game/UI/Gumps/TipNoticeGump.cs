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

namespace ClassicUO.Game.UI.Gumps
{
    internal class TipNoticeGump : Gump
    {
        private readonly ExpandableScroll _background;
        private readonly ScrollArea _scrollArea;
        private readonly StbTextBox _textBox;

        public TipNoticeGump(uint serial, byte type, string text) : base(serial, 0)
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