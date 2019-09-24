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
using System.Collections.Generic;

using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Renderer;
using ClassicUO.Utility;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal enum HSliderBarStyle
    {
        MetalWidgetRecessedBar,
        BlueWidgetNoBar
    }

    internal class HSliderBar : Control
    {
        private readonly List<HSliderBar> _pairedSliders = new List<HSliderBar>();
        private readonly HSliderBarStyle _style;
        private readonly RenderedText _text;
        private bool _clicked;
        private Point _clickPosition;

        private readonly bool _drawUp;
        private UOTexture[] _gumpSpliderBackground;
        private UOTexture _gumpWidget;
        private Rectangle _rect;

        //private int _newValue;
        private int _sliderX;
        private int _value = -1;

        public HSliderBar(int x, int y, int w, int min, int max, int value, HSliderBarStyle style, bool hasText = false, byte font = 0, ushort color = 0, bool unicode = true, bool drawUp = false)
        {
            X = x;
            Y = y;

            if (hasText)
            {
                _text = RenderedText.Create(string.Empty, color, font, unicode);
                _drawUp = drawUp;
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
                    var oldValue = _value;
                    _value = /*_newValue =*/ value;
                    //if (IsInitialized)
                    //    RecalculateSliderX();

                    if (_value < MinValue)
                        _value = MinValue;
                    else if (_value > MaxValue)
                        _value = MaxValue;

                    if (_text != null)
                        _text.Text = Value.ToString();

                    if (_value != oldValue)
                    {
                        ModifyPairedValues(_value - oldValue);

                        if (IsInitialized)
                            CalculateOffset();
                    }

                    ValueChanged.Raise();
                }
            }
        }

        public event EventHandler ValueChanged;

        public override void Update(double totalMS, double frameMS)
        {
            if (_gumpWidget == null)
            {
                switch (_style)
                {
                    case HSliderBarStyle.MetalWidgetRecessedBar:

                        _gumpSpliderBackground = new UOTexture[3]
                        {
                            FileManager.Gumps.GetTexture(213), FileManager.Gumps.GetTexture(214), FileManager.Gumps.GetTexture(215)
                        };
                        _gumpWidget = FileManager.Gumps.GetTexture(216);

                        break;

                    case HSliderBarStyle.BlueWidgetNoBar:
                        _gumpWidget = FileManager.Gumps.GetTexture(0x845);

                        break;
                }

                Width = BarWidth;
                if (_gumpWidget != null) Height = _gumpWidget.Height;
                //RecalculateSliderX();
                CalculateOffset();
            }

            if (_gumpSpliderBackground != null)
            {
                foreach (UOTexture t in _gumpSpliderBackground)
                    t.Ticks = (long) totalMS;
            }

            //ModifyPairedValues(_newValue - Value);
            _gumpWidget.Ticks = (long) totalMS;

            // if (_value != _newValue)
            //_value = _newValue;
            base.Update(totalMS, frameMS);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ResetHueVector();

            if (_gumpSpliderBackground != null)
            {
                batcher.Draw2D(_gumpSpliderBackground[0], x, y, ref _hueVector);
                batcher.Draw2DTiled(_gumpSpliderBackground[1], x + _gumpSpliderBackground[0].Width, y, BarWidth - _gumpSpliderBackground[2].Width - _gumpSpliderBackground[0].Width, _gumpSpliderBackground[1].Height, ref _hueVector);
                batcher.Draw2D(_gumpSpliderBackground[2], x + BarWidth - _gumpSpliderBackground[2].Width, y, ref _hueVector);
            }

            batcher.Draw2D(_gumpWidget, x + _sliderX, y, ref _hueVector);

            if (_text != null)
            {
                if (_drawUp)
                    _text.Draw(batcher, x, y - _text.Height);
                else
                    _text.Draw(batcher, x + BarWidth + 2, y + (Height >> 1) - (_text.Height >> 1));
            }

            return base.Draw(batcher, x, y);
        }

        private void InternalSetValue(int value)
        {
            _value = value;
            CalculateOffset();

            if (_text != null)
                _text.Text = Value.ToString();
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

            if (button == MouseButton.Left) CalculateNew(x);
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

        protected override void OnMouseOver(int x, int y)
        {
            if (_clicked) CalculateNew(x);
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
                Percents = 0;
            _sliderX = (int) (length * Percents / 100.0f);

            if (_sliderX < 0)
                _sliderX = 0;
        }

        public override bool Contains(int x, int y)
        {
            _rect.X = 0;
            _rect.Y = 0;
            _rect.Width = BarWidth;
            _rect.Height = _gumpWidget.Height;

            return _rect.Contains(x, y);
        }

        private void RecalculateSliderX()
        {
            _sliderX = (BarWidth - _gumpWidget.Width) * ((Value - MinValue) / (MaxValue - MinValue));
        }

        public void AddParisSlider(HSliderBar s)
        {
            _pairedSliders.Add(s);
        }

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
                        _pairedSliders[sliderIndex].InternalSetValue(_pairedSliders[sliderIndex].Value + d);
                        points--;
                    }
                }
                else
                {
                    if (_pairedSliders[sliderIndex].Value > _pairedSliders[sliderIndex].MinValue)
                    {
                        updateSinceLastCycle = true;
                        _pairedSliders[sliderIndex].InternalSetValue(_pairedSliders[sliderIndex]._value + d);
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

        public override void Dispose()
        {
            _text?.Destroy();
            base.Dispose();
        }
    }
}