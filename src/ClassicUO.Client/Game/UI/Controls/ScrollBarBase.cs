// SPDX-License-Identifier: BSD-2-Clause

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