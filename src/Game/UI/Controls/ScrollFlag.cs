﻿#region license

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
    internal class ScrollFlag : Control, IScrollBar
    {
        private const int TIME_BETWEEN_CLICKS = 150;
        private readonly SpriteTexture _downButton;

        private readonly bool _showButtons;
        private readonly SpriteTexture _upButton;
        private bool _btUpClicked, _btDownClicked, _btnSliderClicked;

        private Point _clickPosition;
        private int _max, _min;

        private Rectangle _rectUpButton, _rectDownButton;
        private float _sliderPosition;
        private float _timeUntilNextClick;
        private float _value;

        public ScrollFlag(int x, int y, int height, bool showbuttons) : this()
        {
            X = x;
            Y = y;
            Height = height;

            //TODO:
            _showButtons = false; // showbuttons;
        }

        public ScrollFlag()
        {
            AcceptMouseInput = true;

            Texture = FileManager.Gumps.GetTexture(0x0828);
            Width = Texture.Width;
            Height = Texture.Height;

            _upButton = FileManager.Gumps.GetTexture(0x0824);
            _downButton = FileManager.Gumps.GetTexture(0x0825);

            _rectUpButton = new Rectangle(0, 0, _upButton.Width, _upButton.Height);
            _rectDownButton = new Rectangle(0, Height, _downButton.Width, _downButton.Height);

            WantUpdateSize = false;
        }

        protected override ClickPriority Priority { get; } = ClickPriority.High;

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
                _max = value;

                if (_value > _max)
                    _value = _max;
            }
        }

        public int ScrollStep { get; set; } = 5;

        bool IScrollBar.Contains(int x, int y)
        {
            return Contains(x, y);
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (MaxValue <= MinValue || MinValue >= MaxValue)
                Value = MaxValue = MinValue;
            _sliderPosition = GetSliderYPosition();


            if (_btUpClicked || _btDownClicked)
            {
                if (_timeUntilNextClick <= 0f)
                {
                    _timeUntilNextClick += TIME_BETWEEN_CLICKS;

                    if (_btUpClicked)
                        Value -= ScrollStep + _StepChanger;

                    if (_btDownClicked)
                        Value += ScrollStep + _StepChanger;
                    _StepsDone++;

                    if (_StepsDone % 4 == 0)
                        _StepChanger++;
                }

                _timeUntilNextClick -= (float) frameMS;
            }


            Texture.Ticks = _upButton.Ticks = _downButton.Ticks = (long) totalMS;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ResetHueVector();

            if (MaxValue != MinValue)
                batcher.Draw2D(Texture, x, (int) (y + _sliderPosition), ref _hueVector);

            if (_showButtons)
            {
                batcher.Draw2D(_upButton, x, y, ref _hueVector);
                batcher.Draw2D(_downButton, x, y + Height, ref _hueVector);
            }

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
            return Height - Texture.Height;
        }

        protected override void OnMouseDown(int x, int y, MouseButton button)
        {
            if (button != MouseButton.Left)
                return;

            _timeUntilNextClick = 0f;

            if (_showButtons && _rectDownButton.Contains(x, y))
                _btDownClicked = true;
            else if (_showButtons && _rectUpButton.Contains(x, y))
                _btUpClicked = true;
            else if (Contains(x, y))
            {
                _btnSliderClicked = true;
                _clickPosition = new Point(x, y);
            }
        }

        protected override void OnMouseUp(int x, int y, MouseButton button)
        {
            if (button != MouseButton.Left)
                return;

            _btDownClicked = false;
            _btUpClicked = false;
            _btnSliderClicked = false;
            _StepChanger = _StepsDone = 1;
        }

        protected override void OnMouseOver(int x, int y)
        {
            if (_btnSliderClicked)
            {
                if (y != _clickPosition.Y)
                {
                    float sliderY = _sliderPosition + (y - _clickPosition.Y);

                    if (sliderY < 0)
                        sliderY = 0;
                    float scrollableArea = GetScrollableArea();

                    if (sliderY > scrollableArea)
                        sliderY = scrollableArea;
                    _clickPosition = new Point(x, y);
                    _value = sliderY / scrollableArea * (MaxValue - MinValue) + MinValue;
                    _sliderPosition = sliderY;
                }
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
            y -= (int) _sliderPosition;

            return Texture.Contains(x, y);
        }
    }
}