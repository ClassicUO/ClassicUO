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

using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Renderer;
using ClassicUO.Utility;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class ScrollBar : Control, IScrollBar
    {
        private const int TIME_BETWEEN_CLICKS = 2;
        private bool _btUpClicked, _btDownClicked, _btSliderClicked;
        private Point _clickPosition;
        private int _max;
        private int _min;
        private Rectangle _rectDownButton, _rectUpButton, _rectSlider, _emptySpace;
        private float _sliderPosition, _value;
        private UOTexture _textureSlider;
        private UOTexture[] _textureUpButton, _textureDownButton, _textureBackground;
        private uint _timeUntilNextClick;

        public ScrollBar(int x, int y, int height)
        {
            Height = height;
            Location = new Point(x, y);
            AcceptMouseInput = true;
        }

        public event EventHandler ValueChanged;

        public int Value
        {
            get => (int) _value;
            set
            {
                _value = value;

                if (_value < MinValue)
                    _value = MinValue;

                if (_value > MaxValue)
                    _value = MaxValue;
                ValueChanged.Raise();
            }
        }

        public int MinValue
        {
            get => _min;
            set
            {
                _min = value;

                if (_value < _min)
                    _value = _min;
            }
        }

        public int MaxValue
        {
            get => _max;
            set
            {
                if (value < 0)
                    value = 0;
                _max = value;

                if (_value > _max)
                    _value = _max;
            }
        }

        public int ScrollStep { get; set; } = 15;

        bool IScrollBar.Contains(int x, int y)
        {
            return Contains(x, y);
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _textureUpButton = new UOTexture[2];
            _textureUpButton[0] = FileManager.Gumps.GetTexture(251);
            _textureUpButton[1] = FileManager.Gumps.GetTexture(250);
            _textureDownButton = new UOTexture[2];
            _textureDownButton[0] = FileManager.Gumps.GetTexture(253);
            _textureDownButton[1] = FileManager.Gumps.GetTexture(252);
            _textureBackground = new UOTexture[3];
            _textureBackground[0] = FileManager.Gumps.GetTexture(257);
            _textureBackground[1] = FileManager.Gumps.GetTexture(256);
            _textureBackground[2] = FileManager.Gumps.GetTexture(255);
            _textureSlider = FileManager.Gumps.GetTexture(254);
            Width = _textureBackground[0].Width;


            _rectDownButton = new Rectangle(0, Height - _textureDownButton[0].Height, _textureDownButton[0].Width, _textureDownButton[0].Height);
            _rectUpButton = new Rectangle(0, 0, _textureUpButton[0].Width, _textureUpButton[0].Height);
            _rectSlider = new Rectangle((_textureBackground[0].Width - _textureSlider.Width) >> 1, _textureUpButton[0].Height + (int) _sliderPosition, _textureSlider.Width, _textureSlider.Height);
            _emptySpace.X = 0;
            _emptySpace.Y = _textureUpButton[0].Height;
            _emptySpace.Width = _textureSlider.Width;
            _emptySpace.Height = Height - (_textureDownButton[0].Height + _textureUpButton[0].Height);
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (MaxValue <= MinValue || MinValue >= MaxValue)
                Value = MaxValue = MinValue;
            _sliderPosition = GetSliderYPosition();
            _rectSlider.Y = _textureUpButton[0].Height + (int) _sliderPosition;

            if (_btUpClicked || _btDownClicked)
            {
                if (_timeUntilNextClick < Engine.Ticks)
                {
                    _timeUntilNextClick = Engine.Ticks + TIME_BETWEEN_CLICKS;

                    if (_btUpClicked)
                        Value -= 1 + _StepChanger;
                    else if (_btDownClicked)
                        Value += 1 + _StepChanger;

                    _StepsDone++;

                    if (_StepsDone % 8 == 0)
                        _StepChanger++;
                }
            }

            for (int i = 0; i < 3; i++)
            {
                if (i == 0)
                    _textureSlider.Ticks = (long) totalMS;

                if (i < 2)
                {
                    _textureUpButton[i].Ticks = (long) totalMS;
                    _textureDownButton[i].Ticks = (long) totalMS;
                }

                _textureBackground[i].Ticks = (long) totalMS;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (Height <= 0 || !IsVisible)
                return false;

            ResetHueVector();

            // draw scrollbar background
            int middleHeight = Height - _textureUpButton[0].Height - _textureDownButton[0].Height - _textureBackground[0].Height - _textureBackground[2].Height;

            if (middleHeight > 0)
            {
                batcher.Draw2D(_textureBackground[0], x, y + _textureUpButton[0].Height, ref _hueVector);
                batcher.Draw2DTiled(_textureBackground[1], x, y + _textureUpButton[0].Height + _textureBackground[0].Height, _textureBackground[0].Width, middleHeight, ref _hueVector);
                batcher.Draw2D(_textureBackground[2], x, y + Height - _textureDownButton[0].Height - _textureBackground[2].Height, ref _hueVector);
            }
            else
            {
                middleHeight = Height - _textureUpButton[0].Height - _textureDownButton[0].Height;
                batcher.Draw2DTiled(_textureBackground[1], x, y + _textureUpButton[0].Height, _textureBackground[0].Width, middleHeight, ref _hueVector);
            }

            // draw up button
            batcher.Draw2D(_btUpClicked ? _textureUpButton[1] : _textureUpButton[0], x, y, ref _hueVector);

            // draw down button
            batcher.Draw2D(_btDownClicked ? _textureDownButton[1] : _textureDownButton[0], x, y + Height - _textureDownButton[0].Height, ref _hueVector);

            // draw slider
            if (MaxValue > MinValue && middleHeight > 0)
                batcher.Draw2D(_textureSlider, x + ((_textureBackground[0].Width - _textureSlider.Width) >> 1), (int) (y + _textureUpButton[0].Height + _sliderPosition), ref _hueVector);

            return base.Draw(batcher, x, y);
        }

        private float GetSliderYPosition()
        {
            if (MaxValue - MinValue == 0)
                return 0f;

            return GetScrollableArea() * ((_value - MinValue) / (MaxValue - MinValue));
        }

        private float GetScrollableArea()
        {
            return Height - _textureUpButton[0].Height - _textureDownButton[0].Height - _textureSlider.Height;
        }

        protected override void OnMouseDown(int x, int y, MouseButton button)
        {
            if (button != MouseButton.Left)
                return;

            _timeUntilNextClick = 0;

            if (_rectDownButton.Contains(x, y))
            {
                // clicked on the down button
                _btDownClicked = true;
            }
            else if (_rectUpButton.Contains(x, y))
            {
                // clicked on the up button
                _btUpClicked = true;
            }
            else if (_rectSlider.Contains(x, y))
            {
                // clicked on the slider
                _btSliderClicked = true;
                _clickPosition.X = x;
                _clickPosition.Y = y;
            }
            else if (_emptySpace.Contains(x, y))
            {
                y -= _emptySpace.Y + (_rectSlider.Height >> 1);

                if (y < 0)
                    y = 0;

                _sliderPosition = y;

                float scrollableArea = GetScrollableArea();
                if (_sliderPosition > scrollableArea)
                    _sliderPosition = scrollableArea;

                _value = _sliderPosition / GetScrollableArea() * (MaxValue - MinValue) + MinValue;
                _clickPosition.Y = y;
                _clickPosition.X = x;
            }
        }

        protected override void OnMouseUp(int x, int y, MouseButton button)
        {
            if (button != MouseButton.Left)
                return;

            _btDownClicked = false;
            _btUpClicked = false;
            _btSliderClicked = false;
            _StepChanger = _StepsDone = 1;
        }

        protected override void OnMouseOver(int x, int y)
        {
            if (_btSliderClicked)
                CalculateByPosition(x, y);
        }

        private void CalculateByPosition(int x, int y)
        {
            if (y != _clickPosition.Y)
            {
                float sliderY = _sliderPosition + (y - _clickPosition.Y);
                if (sliderY < 0)
                    sliderY = 0;

                float scrollableArea = GetScrollableArea();
                if (sliderY > scrollableArea)
                    sliderY = scrollableArea;

                _clickPosition.X = x;
                _clickPosition.Y = y;

                if (sliderY == 0 && _clickPosition.Y < _textureUpButton[0].Height + (_textureSlider.Height >> 1))
                    _clickPosition.Y = _textureUpButton[0].Height + (_textureSlider.Height >> 1);

                if (sliderY == scrollableArea && _clickPosition.Y > Height - _textureDownButton[0].Height - (_textureSlider.Height >> 1))
                    _clickPosition.Y = Height - _textureDownButton[0].Height - (_textureSlider.Height >> 1);
                _value = sliderY / scrollableArea * (MaxValue - MinValue) + MinValue;
                _sliderPosition = sliderY;
            }
        }

        protected override void OnMouseWheel(MouseEvent delta)
        {
            switch (delta)
            {
                case MouseEvent.WheelScrollUp:
                    Value -= ScrollStep;

                    break;

                case MouseEvent.WheelScrollDown:
                    Value += ScrollStep;

                    break;
            }
        }

        public override bool Contains(int x, int y)
        {
            return x >= 0 && x <= Width && y >= 0 && y <= Height;
        }
    }
}