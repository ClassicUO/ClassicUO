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
using System.Collections.Generic;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.Controls
{
    internal enum HSliderBarStyle
    {
        MetalWidgetRecessedBar,
        BlueWidgetNoBar
    }

    internal class HSliderBar : GumpControl
    {
        private bool _clicked;
        private Point _clickPosition;
        private SpriteTexture[] _gumpSpliderBackground;
        private SpriteTexture _gumpWidget;
        private readonly List<HSliderBar> _pairedSliders = new List<HSliderBar>();

        private Rectangle _rect;

        //private int _newValue;
        private int _sliderX;
        private readonly HSliderBarStyle _style;
        private readonly RenderedText _text;
        private int _value = -1;


        public EventHandler ValueChanged;


        public HSliderBar(int x, int y, int w, int min, int max, int value, HSliderBarStyle style, bool hasText = false,
            byte font = 0, ushort color = 0, bool unicode = true)
        {
            X = x;
            Y = y;

            if (hasText)
            {
                _text = new RenderedText
                {
                    Font = font,
                    Hue = color,
                    IsUnicode = unicode
                };
            }

            MinValue = min;
            MaxValue = max;
            BarWidth = w;
            Value = value;
            _style = style;


            AcceptMouseInput = true;
        }


        public int MinValue { get; set; }
        public int MaxValue { get; set; }
        public int BarWidth { get; set; }
        public float Percents { get; private set; }

        public int Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = /*_newValue =*/ value;
                    //if (IsInitialized)
                    //    RecalculateSliderX();

                    if (_value < MinValue)
                        _value = MinValue;
                    else if (_value > MaxValue)
                        _value = MaxValue;

                    if (_text != null)
                        _text.Text = Value.ToString();
                    ValueChanged.Raise();
                }
            }
        }


        public override void Update(double totalMS, double frameMS)
        {
            if (_gumpWidget == null)
            {
                switch (_style)
                {
                    case HSliderBarStyle.MetalWidgetRecessedBar:
                        _gumpSpliderBackground = new SpriteTexture[3]
                        {
                            IO.Resources.Gumps.GetGumpTexture(213),
                            IO.Resources.Gumps.GetGumpTexture(214),
                            IO.Resources.Gumps.GetGumpTexture(215)
                        };
                        _gumpWidget = IO.Resources.Gumps.GetGumpTexture(216);
                        break;
                    case HSliderBarStyle.BlueWidgetNoBar:
                        _gumpWidget = IO.Resources.Gumps.GetGumpTexture(0x845);
                        break;
                }

                Width = BarWidth;
                Height = _gumpWidget.Height;
                //RecalculateSliderX();

                CalculateOffset();
            }

            if (_gumpSpliderBackground != null)
            {
                for (int i = 0; i < _gumpSpliderBackground.Length; i++)
                {
                    _gumpSpliderBackground[i].Ticks = (long) totalMS;
                }
            }

            //ModifyPairedValues(_newValue - Value);
            _gumpWidget.Ticks = (long) totalMS;

            // if (_value != _newValue)
            //_value = _newValue;

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
        {
            if (_gumpSpliderBackground != null)
            {
                spriteBatch.Draw2D(_gumpSpliderBackground[0], new Vector3(position.X, position.Y, 0), Vector3.Zero);
                spriteBatch.Draw2DTiled(_gumpSpliderBackground[1],
                    new Rectangle((int) position.X + _gumpSpliderBackground[0].Width, (int) position.Y,
                        BarWidth - _gumpSpliderBackground[2].Width - _gumpSpliderBackground[0].Width,
                        _gumpSpliderBackground[1].Height), Vector3.Zero);
                spriteBatch.Draw2D(_gumpSpliderBackground[2],
                    new Vector3(position.X + BarWidth - _gumpSpliderBackground[2].Width, position.Y, 0), Vector3.Zero);
            }

            spriteBatch.Draw2D(_gumpWidget, new Vector3(position.X + _sliderX, position.Y, 0), Vector3.Zero);

            _text?.Draw(spriteBatch,
                new Vector3(position.X + BarWidth + 2, position.Y + Height / 2 - _text.Height / 2, 0));

            return base.Draw(spriteBatch, position, hue);
        }

        protected override void OnMouseDown(int x, int y, MouseButton button)
        {
            _clicked = true;
            _clickPosition.X = x;
            _clickPosition.Y = y;
        }

        protected override void OnMouseUp(int x, int y, MouseButton button)
        {
            _clicked = false;
        }

        protected override void OnMouseClick(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left)
            {
                CalculateNew(x);
            }
        }

        protected override void OnMouseWheel(MouseEvent delta)
        {
            switch (delta)
            {
                case MouseEvent.WheelScrollUp:
                    Value--;
                    break;
                case MouseEvent.WheelScrollDown:
                    Value++;
                    break;
            }

            CalculateOffset();
        }

        protected override void OnMouseEnter(int x, int y)
        {
            if (_clicked)
            {
                CalculateNew(x);

                //_sliderX = _sliderX + (x - _clickPosition.X);
                //if (_sliderX < 0)
                //    _sliderX = 0;
                //if (_sliderX > BarWidth - _gumpWidget.Width)
                //    _sliderX = BarWidth - _gumpWidget.Width;


                //_clickPosition.X = x;
                //_clickPosition.Y = y;
                //if (_clickPosition.X < _gumpWidget.Width / 2)
                //    _clickPosition.X = _gumpWidget.Width / 2;
                //if (_clickPosition.X > BarWidth - _gumpWidget.Width / 2)
                //    _clickPosition.X = BarWidth - _gumpWidget.Width / 2;

                //_newValue = (int)(_sliderX / (float)(BarWidth - _gumpWidget.Width) * (MaxValue - MinValue)) + MinValue;
            }
        }

        private void CalculateNew(int x)
        {
            int len = BarWidth;
            int maxValue = MaxValue - MinValue;

            len -= _gumpWidget.Width;

            float perc = x / (float) len * 100.0f;

            Value = (int) (maxValue * perc / 100.0f) + MinValue;


            CalculateOffset();
        }

        private void CalculateOffset()
        {
            if (Value < MinValue)
                Value = MinValue;
            else if (Value > MaxValue)
                Value = MaxValue;

            int value = Value - MinValue;
            int maxValue = MaxValue - MinValue;
            int length = BarWidth;

            length -= _gumpWidget.Width;

            if (maxValue > 0)
                Percents = value / (float) maxValue * 100.0f;
            else
            {
                Percents = 0;
            }

            _sliderX = (int) (length * Percents / 100.0f);
            if (_sliderX < 0)
                _sliderX = 0;
        }


        protected override bool Contains(int x, int y)
        {
            _rect.X = 0;
            _rect.Y = 0;
            _rect.Width = BarWidth;
            _rect.Height = _gumpWidget.Height;

            return _rect.Contains(x, y);
        }


        private void RecalculateSliderX() =>
            _sliderX = (BarWidth - _gumpWidget.Width) * ((Value - MinValue) / (MaxValue - MinValue));

        public void AddParisSlider(HSliderBar s) => _pairedSliders.Add(s);

        private void ModifyPairedValues(int delta)
        {
            if (_pairedSliders.Count == 0)
                return;

            bool updateSinceLastCycle = true;
            int d = delta > 0 ? -1 : 1;
            int points = Math.Abs(delta);
            int sliderIndex = Value % _pairedSliders.Count;
            while (points > 0)
            {
                if (d > 0)
                {
                    if (_pairedSliders[sliderIndex].Value < _pairedSliders[sliderIndex].MaxValue)
                    {
                        updateSinceLastCycle = true;
                        _pairedSliders[sliderIndex].Value += d;
                        points--;
                    }
                }
                else
                {
                    if (_pairedSliders[sliderIndex].Value > _pairedSliders[sliderIndex].MinValue)
                    {
                        updateSinceLastCycle = true;
                        _pairedSliders[sliderIndex].Value += d;
                        points--;
                    }
                }

                sliderIndex++;
                if (sliderIndex == _pairedSliders.Count)
                {
                    if (!updateSinceLastCycle)
                        return;
                    updateSinceLastCycle = false;
                    sliderIndex = 0;
                }
            }
        }
    }
}