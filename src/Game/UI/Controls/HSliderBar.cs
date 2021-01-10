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

using System;
using System.Collections.Generic;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;

namespace ClassicUO.Game.UI.Controls
{
    internal enum HSliderBarStyle
    {
        MetalWidgetRecessedBar,
        BlueWidgetNoBar
    }

    internal class HSliderBar : Control
    {
        private bool _clicked;
        private readonly bool _drawUp;
        private readonly UOTexture[] _gumpSpliderBackground;
        private readonly UOTexture _gumpWidget;
        private readonly List<HSliderBar> _pairedSliders = new List<HSliderBar>();
        private int _sliderX;
        private readonly HSliderBarStyle _style;
        private readonly RenderedText _text;
        private int _value = -1;

        public HSliderBar
        (
            int x,
            int y,
            int w,
            int min,
            int max,
            int value,
            HSliderBarStyle style,
            bool hasText = false,
            byte font = 0,
            ushort color = 0,
            bool unicode = true,
            bool drawUp = false
        )
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
            _style = style;
            AcceptMouseInput = true;


            if (_gumpWidget == null)
            {
                switch (_style)
                {
                    case HSliderBarStyle.MetalWidgetRecessedBar:

                        _gumpSpliderBackground = new UOTexture[3]
                        {
                            GumpsLoader.Instance.GetTexture(213), GumpsLoader.Instance.GetTexture(214),
                            GumpsLoader.Instance.GetTexture(215)
                        };

                        _gumpWidget = GumpsLoader.Instance.GetTexture(216);

                        break;

                    case HSliderBarStyle.BlueWidgetNoBar:
                        _gumpWidget = GumpsLoader.Instance.GetTexture(0x845);

                        break;
                }

                Width = BarWidth;

                if (_gumpWidget != null)
                {
                    Height = _gumpWidget.Height;
                }

                CalculateOffset();
            }

            Value = value;
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
                    int oldValue = _value;
                    _value = /*_newValue =*/ value;
                    //if (IsInitialized)
                    //    RecalculateSliderX();

                    if (_value < MinValue)
                    {
                        _value = MinValue;
                    }
                    else if (_value > MaxValue)
                    {
                        _value = MaxValue;
                    }

                    if (_text != null)
                    {
                        _text.Text = Value.ToString();
                    }

                    if (_value != oldValue)
                    {
                        ModifyPairedValues(_value - oldValue);

                        CalculateOffset();
                    }

                    ValueChanged.Raise();
                }
            }
        }

        public event EventHandler ValueChanged;

        public override void Update(double totalTime, double frameTime)
        {
            if (_gumpSpliderBackground != null)
            {
                foreach (UOTexture t in _gumpSpliderBackground)
                {
                    t.Ticks = (long) totalTime;
                }
            }

            _gumpWidget.Ticks = (long) totalTime;

            base.Update(totalTime, frameTime);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ResetHueVector();

            if (_gumpSpliderBackground != null)
            {
                batcher.Draw2D(_gumpSpliderBackground[0], x, y, ref HueVector);

                batcher.Draw2DTiled
                (
                    _gumpSpliderBackground[1],
                    x + _gumpSpliderBackground[0].Width,
                    y,
                    BarWidth - _gumpSpliderBackground[2].Width - _gumpSpliderBackground[0].Width,
                    _gumpSpliderBackground[1].Height,
                    ref HueVector
                );

                batcher.Draw2D(_gumpSpliderBackground[2], x + BarWidth - _gumpSpliderBackground[2].Width, y, ref HueVector);
            }

            batcher.Draw2D(_gumpWidget, x + _sliderX, y, ref HueVector);

            if (_text != null)
            {
                if (_drawUp)
                {
                    _text.Draw(batcher, x, y - _text.Height);
                }
                else
                {
                    _text.Draw(batcher, x + BarWidth + 2, y + (Height >> 1) - (_text.Height >> 1));
                }
            }

            return base.Draw(batcher, x, y);
        }

        private void InternalSetValue(int value)
        {
            _value = value;
            CalculateOffset();

            if (_text != null)
            {
                _text.Text = Value.ToString();
            }
        }

        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            if (button != MouseButtonType.Left)
            {
                return;
            }

            _clicked = true;
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button != MouseButtonType.Left)
            {
                return;
            }

            _clicked = false;
            CalculateNew(x);
        }


        protected override void OnMouseWheel(MouseEventType delta)
        {
            switch (delta)
            {
                case MouseEventType.WheelScrollUp:
                    Value++;

                    break;

                case MouseEventType.WheelScrollDown:
                    Value--;

                    break;
            }

            CalculateOffset();
        }

        protected override void OnMouseOver(int x, int y)
        {
            if (_clicked)
            {
                CalculateNew(x);
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
            {
                Value = MinValue;
            }
            else if (Value > MaxValue)
            {
                Value = MaxValue;
            }

            int value = Value - MinValue;
            int maxValue = MaxValue - MinValue;
            int length = BarWidth;
            length -= _gumpWidget.Width;

            if (maxValue > 0)
            {
                Percents = value / (float) maxValue * 100.0f;
            }
            else
            {
                Percents = 0;
            }

            _sliderX = (int) (length * Percents / 100.0f);

            if (_sliderX < 0)
            {
                _sliderX = 0;
            }
        }

        public void AddParisSlider(HSliderBar s)
        {
            _pairedSliders.Add(s);
        }

        private void ModifyPairedValues(int delta)
        {
            if (_pairedSliders.Count == 0)
            {
                return;
            }

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
                    {
                        return;
                    }

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