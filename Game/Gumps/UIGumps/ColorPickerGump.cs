using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Gumps.UIGumps
{
    class ColorPickerGump : Gump
    {
        private const int SLIDER_MIN = 0;
        private const int SLIDER_MAX = 4;


        private Button _buttonOK;
        private ColorPickerBox _box;
        private StaticPic _dyeTybeImage;

        private Action<ushort> _okClicked;

        public ColorPickerGump(int x, int y, Action<ushort> okClicked) : base(0, 0)
        {
            CanMove = true;
            AcceptMouseInput = false;

            
            X = x;
            Y = y;

            AddChildren(new GumpPic(0, 0, 0x0906, 0));

           

            AddChildren(
            _buttonOK = new Button(0, 0x0907, 0x0908, 0x909)
            {
                X = 208,
                Y = 138,
                ButtonAction = ButtonAction.Activate
            });

            HSliderBar slider;

            AddChildren(
            slider = new HSliderBar(39, 142, 145, SLIDER_MIN, SLIDER_MAX, 1, HSliderBarStyle.BlueWidgetNoBar ));

            slider.ValueChanged += (sender, e) => 
            {
                _box.Graduation = slider.Value;
            };

           

            AddChildren(_box = new ColorPickerBox(34, 34));

            _box.ColorSelectedIndex += (sender, e) =>
            {
                _dyeTybeImage.Hue = (ushort)(_box.SelectedHue + 1);

                var polcolor = Hues.GetPolygoneColor(12, _dyeTybeImage.Hue);

            };


            AddChildren(_dyeTybeImage = new StaticPic(0x0FAB, 0)
            {
                X = 208 - 10,
                Y = _box.Y + _box.Height / 2 - 10
            });


            _okClicked = okClicked;
        }

        public override void OnButtonClick(int buttonID)
        {
            switch (buttonID)
            {
                case 0:
                    _okClicked((ushort)(_box.SelectedHue + 1));
                    Dispose();
                    break;
            }
        }
    }
}
