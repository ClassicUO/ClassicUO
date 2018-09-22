using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Gumps.UIGumps
{
    class ColorPickerGump : Gump
    {
        private const int SLIDER_MIN = 0;
        private const int SLIDER_MAX = 4;

        private int _graduation;

        private HSliderBar _slider;
        private Button _buttonOK;
        private ColorPickerBox _box;


        public ColorPickerGump(int x, int y) : base(0, 0)
        {
            X = x;
            Y = y;

            AddChildren(new GumpPic(0, 0, 0x0906, 0));

            AddChildren(
            _buttonOK = new Button(0x0907, 0x0909, 0x908)
            {
                X = 208,
                Y = 138
            });

            AddChildren(
            _slider = new HSliderBar(39, 142, 145, SLIDER_MIN, SLIDER_MAX, 0, HSliderBarStyle.BlueWidgetNoBar ));

            _slider.ValueChanged += (sender, e) => { _box.Graduation = _slider.Value; };

            CanMove = true;
            AcceptMouseInput = false;

            AddChildren(_box = new ColorPickerBox(34, 34));
        }


    }
}
