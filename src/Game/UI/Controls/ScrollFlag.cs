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
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class ScrollFlag : ScrollBarBase
    {
        private const int TIME_BETWEEN_CLICKS = 150;
        private readonly UOTexture _downButton;

        private readonly bool _showButtons;
        private readonly UOTexture _upButton;
        private bool _btUpClicked, _btDownClicked, _btnSliderClicked;

        private Point _clickPosition;

        private Rectangle _rectUpButton, _rectDownButton;
        private float _sliderPosition;
        private float _timeUntilNextClick;

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

            Texture = GumpsLoader.Instance.GetTexture(0x0828);
            Width = Texture.Width;
            Height = Texture.Height;

            _upButton = GumpsLoader.Instance.GetTexture(0x0824);
            _downButton = GumpsLoader.Instance.GetTexture(0x0825);

            _rectUpButton = new Rectangle(0, 0, _upButton.Width, _upButton.Height);
            _rectDownButton = new Rectangle(0, Height, _downButton.Width, _downButton.Height);

            WantUpdateSize = false;
        }

        public override ClickPriority Priority { get; set; } = ClickPriority.High;


        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (MaxValue <= MinValue)
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

        protected override float GetScrollableArea()
        {
            return Height - Texture.Height;
        }

        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            if (button != MouseButtonType.Left)
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

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button != MouseButtonType.Left)
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
                    _value = (int) (sliderY / scrollableArea * (MaxValue - MinValue) + MinValue);
                    _sliderPosition = sliderY;
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

        public override bool Contains(int x, int y)
        {
            y -= (int) _sliderPosition;

            return Texture.Contains(x, y);
        }
    }
}