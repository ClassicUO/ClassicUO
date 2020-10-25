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
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal enum ScrollbarBehaviour
    {
        ShowWhenDataExceedFromView,
        ShowAlways
    }

    internal class ScrollArea : Control
    {
        private bool _isNormalScroll;
        private readonly ScrollBarBase _scrollBar;

        public ScrollArea
        (
            int x,
            int y,
            int w,
            int h,
            bool normalScrollbar,
            int scroll_max_height = -1
        )
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
            _isNormalScroll = normalScrollbar;

            if (normalScrollbar)
            {
                _scrollBar = new ScrollBar(Width - 14, 0, Height);
            }
            else
            {
                _scrollBar = new ScrollFlag
                {
                    X = Width - 19, Height = h
                };

                Width += 15;
            }

            ScrollMaxHeight = scroll_max_height;

            _scrollBar.MinValue = 0;
            _scrollBar.MaxValue = scroll_max_height >= 0 ? scroll_max_height : Height;
            _scrollBar.Parent = this;

            AcceptMouseInput = true;
            WantUpdateSize = false;
            CanMove = true;
            ScrollbarBehaviour = ScrollbarBehaviour.ShowWhenDataExceedFromView;
        }


        public int ScrollMaxHeight { get; set; } = -1;
        public ScrollbarBehaviour ScrollbarBehaviour { get; set; }
        public int ScrollValue => _scrollBar.Value;
        public int ScrollMinValue => _scrollBar.MinValue;
        public int ScrollMaxValue => _scrollBar.MaxValue;


        public Rectangle ScissorRectangle;


        public override void Update(double totalTime, double frameTime)
        {
            base.Update(totalTime, frameTime);

            CalculateScrollBarMaxValue();

            if (ScrollbarBehaviour == ScrollbarBehaviour.ShowAlways)
            {
                _scrollBar.IsVisible = true;
            }
            else if (ScrollbarBehaviour == ScrollbarBehaviour.ShowWhenDataExceedFromView)
            {
                _scrollBar.IsVisible = _scrollBar.MaxValue > _scrollBar.MinValue;
            }
        }

        public void Scroll(bool isup)
        {
            if (isup)
            {
                _scrollBar.Value -= _scrollBar.ScrollStep;
            }
            else
            {
                _scrollBar.Value += _scrollBar.ScrollStep;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ScrollBarBase scrollbar = (ScrollBarBase) Children[0];
            scrollbar.Draw(batcher, x + scrollbar.X, y + scrollbar.Y);

            Rectangle scissor = ScissorStack.CalculateScissors
            (
                Matrix.Identity, x + ScissorRectangle.X, y + ScissorRectangle.Y, Width - 14 + ScissorRectangle.Width,
                Height + ScissorRectangle.Height
            );

            if (ScissorStack.PushScissors(batcher.GraphicsDevice, scissor))
            {
                batcher.EnableScissorTest(true);

                for (int i = 1; i < Children.Count; i++)
                {
                    Control child = Children[i];

                    if (!child.IsVisible)
                    {
                        continue;
                    }

                    int finalY = y + child.Y - scrollbar.Value + ScissorRectangle.Y;

                    //if (finalY + child.Bounds.Height >= scissor.Y && finalY - child.Height < scissor.Bottom)
                    {
                        child.Draw(batcher, x + child.X, finalY);
                    }
                }

                batcher.EnableScissorTest(false);
                ScissorStack.PopScissors(batcher.GraphicsDevice);
            }

            return true;
        }


        protected override void OnMouseWheel(MouseEventType delta)
        {
            switch (delta)
            {
                case MouseEventType.WheelScrollUp:
                    _scrollBar.Value -= _scrollBar.ScrollStep;

                    break;

                case MouseEventType.WheelScrollDown:
                    _scrollBar.Value += _scrollBar.ScrollStep;

                    break;
            }
        }

        public override void Clear()
        {
            for (int i = 1; i < Children.Count; i++)
            {
                Children[i].Dispose();
            }
        }

        private void CalculateScrollBarMaxValue()
        {
            _scrollBar.Height = ScrollMaxHeight >= 0 ? ScrollMaxHeight : Height;
            bool maxValue = _scrollBar.Value == _scrollBar.MaxValue && _scrollBar.MaxValue != 0;

            int startX = 0, startY = 0, endX = 0, endY = 0;

            for (int i = 1; i < Children.Count; i++)
            {
                Control c = Children[i];

                if (c.IsVisible && !c.IsDisposed)
                {
                    if (c.X < startX)
                    {
                        startX = c.X;
                    }

                    if (c.Y < startY)
                    {
                        startY = c.Y;
                    }

                    if (c.Bounds.Right > endX)
                    {
                        endX = c.Bounds.Right;
                    }

                    if (c.Bounds.Bottom > endY)
                    {
                        endY = c.Bounds.Bottom;
                    }
                }
            }

            int width = Math.Abs(startX) + Math.Abs(endX);
            int height = Math.Abs(startY) + Math.Abs(endY) - _scrollBar.Height;
            height = Math.Max(0, height - (-ScissorRectangle.Y + ScissorRectangle.Height));

            if (height > 0)
            {
                _scrollBar.MaxValue = height;

                if (maxValue)
                {
                    _scrollBar.Value = _scrollBar.MaxValue;
                }
            }
            else
            {
                _scrollBar.Value = _scrollBar.MaxValue = 0;
            }

            _scrollBar.UpdateOffset(0, Offset.Y);

            for (int i = 1; i < Children.Count; i++)
            {
                Children[i].UpdateOffset(0, -_scrollBar.Value + ScissorRectangle.Y);
            }
        }
    }
}