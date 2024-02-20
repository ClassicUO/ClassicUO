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

using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;

namespace ClassicUO.Game.UI.Controls
{
    public enum ScrollbarBehaviour
    {
        ShowWhenDataExceedFromView,
        ShowAlways
    }

    public class ScrollArea : Control
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
                    X = Width - 19,
                    Height = h
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

        public override void SlowUpdate()
        {
            base.SlowUpdate();
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

        public int ScrollBarWidth()
        {
            if (_scrollBar == null)
                return 0;
            return _scrollBar.Width;
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
            ScrollBarBase scrollbar = (ScrollBarBase)Children[0];
            scrollbar.Draw(batcher, x + scrollbar.X, y + scrollbar.Y);

            if (batcher.ClipBegin(x + ScissorRectangle.X, y + ScissorRectangle.Y, Width - 14 + ScissorRectangle.Width, Height + ScissorRectangle.Height))
            {
                for (int i = 1; i < Children.Count; i++)
                {
                    Control child = Children[i];

                    if (!child.IsVisible)
                    {
                        continue;
                    }

                    int finalY = y + child.Y - scrollbar.Value + ScissorRectangle.Y;

                    child.Draw(batcher, x + child.X, finalY);
                }

                batcher.ClipEnd();
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