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

using System;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Network;

namespace ClassicUO.Game.UI.Gumps
{
    internal class ColorPickerGump : Gump
    {
        private const int SLIDER_MIN = 0;
        private const int SLIDER_MAX = 4;
        private readonly ColorPickerBox _box;
        private readonly StaticPic _dyeTybeImage;

        private readonly ushort _graphic;
        private readonly Action<ushort> _okClicked;

        public ColorPickerGump(World world, uint serial, ushort graphic, int x, int y, Action<ushort> okClicked) : base(world, serial, 0)
        {
            CanCloseWithRightClick = serial == 0;
            _graphic = graphic;
            CanMove = true;
            AcceptMouseInput = false;
            X = x;
            Y = y;
            Add(new GumpPic(0, 0, 0x0906, 0));

            Add
            (
                new Button(0, 0x0907, 0x0908, 0x909)
                {
                    X = 208, Y = 138, ButtonAction = ButtonAction.Activate
                }
            );

            HSliderBar slider;

            Add
            (
                slider = new HSliderBar
                (
                    39,
                    142,
                    145,
                    SLIDER_MIN,
                    SLIDER_MAX,
                    1,
                    HSliderBarStyle.BlueWidgetNoBar
                )
            );

            slider.ValueChanged += (sender, e) => { _box.Graduation = slider.Value; };
            Add(_box = new ColorPickerBox(World, 34, 34));
            _box.ColorSelectedIndex += (sender, e) => { _dyeTybeImage.Hue = _box.SelectedHue; };

            Add
            (
                _dyeTybeImage = new StaticPic(0x0FAB, 0)
                {
                    X = 200, Y = 58
                }
            );

            _okClicked = okClicked;
            _dyeTybeImage.Hue = _box.SelectedHue;
        }

        public ushort Graphic => _graphic;

        public override void OnButtonClick(int buttonID)
        {
            switch (buttonID)
            {
                case 0:

                    if (LocalSerial != 0)
                    {
                        NetClient.Socket.Send_DyeDataResponse(LocalSerial, _graphic, _box.SelectedHue);
                    }

                    _okClicked?.Invoke(_box.SelectedHue);
                    Dispose();

                    break;
            }
        }
    }
}