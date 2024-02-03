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
using ClassicUO.Input;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal abstract class ScrollBarBase : Control
    {
        private const int TIME_BETWEEN_CLICKS = 2;

        private float _timeUntilNextClick;

        protected bool _btUpClicked, _btDownClicked, _btnSliderClicked, _btSliderClicked;
        protected Point _clickPosition;
        protected Rectangle _rectUpButton, _rectDownButton;
        protected int _sliderPosition;


        public int Value
        {
            get => _value;
            set
            {
                if (_value == value)
                {
                    return;
                }

                _value = value;

                if (_value < MinValue)
                {
                    _value = MinValue;
                }
                else if (_value > MaxValue)
                {
                    _value = MaxValue;
                }

                ValueChanged.Raise();
            }
        }

        public int MinValue
        {
            get => _minValue;
            set
            {
                if (_minValue == value)
                {
                    return;
                }

                _minValue = value;

                if (_value < _minValue)
                {
                    _value = _minValue;
                }
            }
        }

        public int MaxValue
        {
            get => _maxValue;
            set
            {
                if (_maxValue == value)
                {
                    return;
                }

                if (value < 0)
                {
                    _maxValue = 0;
                }
                else
                {
                    _maxValue = value;
                }

                if (_value > _maxValue)
                {
                    _value = _maxValue;
                }
            }
        }

        public int ScrollStep { get; set; } = 50;
        protected int _value, _minValue, _maxValue;


        public event EventHandler ValueChanged;


        public override void Update()
        {
            base.Update();

            if (_btnSliderClicked)
            {
                int x = Mouse.Position.X - X - ParentX;
                int y = Mouse.Position.Y - Y - ParentY;

                CalculateByPosition(x, y);
            }

            if (MaxValue <= MinValue)
            {
                Value = MaxValue = MinValue;
            }

            _sliderPosition = GetSliderYPosition();

            //_rectSlider.Y = _textureUpButton[0].Height + _sliderPosition;

            if (_btUpClicked || _btDownClicked)
            {
                if (_timeUntilNextClick < Time.Ticks)
                {
                    _timeUntilNextClick = Time.Ticks + TIME_BETWEEN_CLICKS;

                    if (_btUpClicked)
                    {
                        Value -= 1 + _StepChanger;
                    }
                    else if (_btDownClicked)
                    {
                        Value += 1 + _StepChanger;
                    }

                    _StepsDone++;

                    if (_StepsDone % 8 == 0)
                    {
                        _StepChanger++;
                    }
                }
            }
        }

        protected override void OnMouseWheel(MouseEventType delta)
        {
            switch (delta)
            {
                case MouseEventType.WheelScrollUp:
                    Value -= ScrollStep;

                    break;

                case MouseEventType.WheelScrollDown:
                    Value += ScrollStep;

                    break;
            }
        }

        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            if (button != MouseButtonType.Left)
            {
                return;
            }

            _timeUntilNextClick = 0f;
            _btnSliderClicked = false;

            if (_rectDownButton.Contains(x, y))
            {
                _btDownClicked = true;
            }
            else if (_rectUpButton.Contains(x, y))
            {
                _btUpClicked = true;
            }
            else if (Contains(x, y))
            {
                _btnSliderClicked = true;

                CalculateByPosition(x, y);
            }
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button != MouseButtonType.Left)
            {
                return;
            }

            _btDownClicked = false;
            _btUpClicked = false;
            _btnSliderClicked = false;
            _StepChanger = _StepsDone = 1;
        }

        protected int GetSliderYPosition()
        {
            if (MaxValue == MinValue)
            {
                return 0;
            }

            return (int) Math.Round(GetScrollableArea() * ((Value - MinValue) / (float) (MaxValue - MinValue)));
        }


        protected abstract int GetScrollableArea();

        protected abstract void CalculateByPosition(int x, int y);
    }
}