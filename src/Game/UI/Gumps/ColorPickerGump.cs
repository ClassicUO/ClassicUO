#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
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

namespace ClassicUO.Game.UI.Gumps
{
    internal class ColorPickerGump : Gump
    {
        private const int SLIDER_MIN = 0;
        private const int SLIDER_MAX = 4;
        private readonly ColorPickerBox _box;
        private readonly StaticPic _dyeTybeImage;
        private readonly Action<ushort> _okClicked;

        public ColorPickerGump(int x, int y, Action<ushort> okClicked) : base(0, 0)
        {
            CanMove = true;
            AcceptMouseInput = false;
            X = x;
            Y = y;
            AddChildren(new GumpPic(0, 0, 0x0906, 0));

            AddChildren(new Button(0, 0x0907, 0x0908, 0x909)
            {
                X = 208, Y = 138, ButtonAction = ButtonAction.Activate
            });
            HSliderBar slider;
            AddChildren(slider = new HSliderBar(39, 142, 145, SLIDER_MIN, SLIDER_MAX, 1, HSliderBarStyle.BlueWidgetNoBar));
            slider.ValueChanged += (sender, e) => { _box.Graduation = slider.Value; };
            AddChildren(_box = new ColorPickerBox(34, 34));
            _box.ColorSelectedIndex += (sender, e) => { _dyeTybeImage.Hue = (ushort) (_box.SelectedHue + 1); };

            AddChildren(_dyeTybeImage = new StaticPic(0x0FAB, 0)
            {
                X = 208 - 10, Y = _box.Y + (_box.Height >> 1) - 10
            });
            _okClicked = okClicked;
        }

        public override void OnButtonClick(int buttonID)
        {
            switch (buttonID)
            {
                case 0:
                    _okClicked((ushort) (_box.SelectedHue + 1));
                    Dispose();

                    break;
            }
        }
    }
}