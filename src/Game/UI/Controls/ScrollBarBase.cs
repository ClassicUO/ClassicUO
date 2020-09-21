#region license

// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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


        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);


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

        protected override void OnMouseOver(int x, int y)
        {
            if (_btnSliderClicked)
            {
                CalculateByPosition(x, y);
            }
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