﻿#region license
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
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class ScrollFlag : ScrollBarBase
    {
        private const int TIME_BETWEEN_CLICKS = 2;
        private readonly bool _showButtons;
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

            UOTexture32 texture_flag = GumpsLoader.Instance.GetTexture(0x0828);

            if (texture_flag == null)
            {
                Dispose();
                return;
            }

            Width = texture_flag.Width;
            Height = texture_flag.Height;

            UOTexture32 texture_button_up = GumpsLoader.Instance.GetTexture(0x0824);
            UOTexture32 texture_button_down = GumpsLoader.Instance.GetTexture(0x0825);

            _rectUpButton = new Rectangle(0, 0, texture_button_up.Width, texture_button_up.Height);
            _rectDownButton = new Rectangle(0, Height, texture_button_down.Width, texture_button_down.Height);

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
                if (_timeUntilNextClick < Time.Ticks)
                {
                    _timeUntilNextClick = Time.Ticks + TIME_BETWEEN_CLICKS;

                    if (_btUpClicked)
                        Value -= 1 + _StepChanger;
                    else if (_btDownClicked)
                        Value += 1 + _StepChanger;

                    _StepsDone++;

                    if (_StepsDone % 8 == 0)
                        _StepChanger++;
                }
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ResetHueVector();

            UOTexture32 texture_flag = GumpsLoader.Instance.GetTexture(0x0828);
            UOTexture32 texture_button_up = GumpsLoader.Instance.GetTexture(0x0824);
            UOTexture32 texture_button_down = GumpsLoader.Instance.GetTexture(0x0825);



            if (MaxValue != MinValue && texture_flag != null)
                batcher.Draw2D(texture_flag, x, (int) (y + _sliderPosition), ref _hueVector);

            if (_showButtons)
            {
                if (texture_button_up != null)
                    batcher.Draw2D(texture_button_up, x, y, ref _hueVector);

                if (texture_button_down != null)
                    batcher.Draw2D(texture_button_down, x, y + Height, ref _hueVector);
            }

            return base.Draw(batcher, x, y);
        }

        protected override int GetScrollableArea()
        {
            UOTexture32 texture = GumpsLoader.Instance.GetTexture(0x0828);

            return Height - texture?.Height ?? 0;
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

                    UOTexture32 texture = GumpsLoader.Instance.GetTexture(0x0828);
                    int height = texture?.Height ?? 0;


                    if (sliderY == 0 && _clickPosition.Y < (height >> 1))
                        _clickPosition.Y = (height >> 1);
                    else if (sliderY == scrollableArea && _clickPosition.Y > Height - (height >> 1))
                        _clickPosition.Y = Height - (height >> 1);

                    _value = (int) Math.Round(sliderY / scrollableArea * (MaxValue - MinValue) + MinValue);
                    _sliderPosition = sliderY;
                }
            }
        }


        public override bool Contains(int x, int y)
        {
            UOTexture32 texture_flag = GumpsLoader.Instance.GetTexture(0x0828);

            if (texture_flag == null)
            {
                return false;
            }

            y -= (int) _sliderPosition;

            return texture_flag.Contains(x, y);
        }
    }
}