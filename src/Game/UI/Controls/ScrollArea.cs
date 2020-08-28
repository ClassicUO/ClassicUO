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

using System.Linq;

using ClassicUO.Input;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    enum ScrollbarBehaviour
    {
        ShowWhenDataExceedFromView,
        ShowAlways,
    }

    internal class ScrollArea : Control
    {
        private readonly ScrollBarBase _scrollBar;
        private bool _isNormalScroll;
        private int _scroll_max_height = -1;

        public ScrollArea(int x, int y, int w, int h, bool normalScrollbar, int scroll_max_height = -1)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
            _isNormalScroll = normalScrollbar;

            if (normalScrollbar)
                _scrollBar = new ScrollBar(Width - 14, 0, Height);
            else
            {
                _scrollBar = new ScrollFlag
                {
                    X = Width - 19, Height = h
                };
                Width += 15;
            }

            _scroll_max_height = scroll_max_height;

            _scrollBar.MinValue = 0;
            _scrollBar.MaxValue = scroll_max_height >= 0 ? scroll_max_height : Height;
            //Add((Control)_scrollBar);

            _scrollBar.Parent = this;

            AcceptMouseInput = true;
            WantUpdateSize = false;
            CanMove = true;
            ScrollbarBehaviour = ScrollbarBehaviour.ShowWhenDataExceedFromView;
        }


        public ScrollbarBehaviour ScrollbarBehaviour;
        public Rectangle ScissorRectangle;

        public int ScrollMaxHeight
        {
            get => _scroll_max_height;
            set => _scroll_max_height = value;
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            CalculateScrollBarMaxValue();

            if (ScrollbarBehaviour == ScrollbarBehaviour.ShowAlways)
            {
                _scrollBar.IsVisible = true;
            }
            else if (ScrollbarBehaviour == ScrollbarBehaviour.ShowWhenDataExceedFromView)
            {
                _scrollBar.IsVisible =_scrollBar.MaxValue > _scrollBar.MinValue;
            }

        }

        public void Scroll(bool isup)
        {
            if (isup)
                _scrollBar.Value -= _scrollBar.ScrollStep;
            else
                _scrollBar.Value += _scrollBar.ScrollStep;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            Control scrollbar = Children[0];
            scrollbar.Draw(batcher, x + scrollbar.X, y + scrollbar.Y);

            Rectangle scissor = ScissorStack.CalculateScissors(Matrix.Identity, x + ScissorRectangle.X, y + ScissorRectangle.Y, (Width - 14) + ScissorRectangle.Width, Height + ScissorRectangle.Height);

            if (ScissorStack.PushScissors(batcher.GraphicsDevice, scissor))
            {
                batcher.EnableScissorTest(true);
                int height = ScissorRectangle.Y;

                for (int i = 1; i < Children.Count; i++)
                {
                    Control child = Children[i];

                    if (!child.IsVisible)
                        continue;

                    child.Y = height - _scrollBar.Value;

                    if (height + child.Height <= _scrollBar.Value)
                    {
                        // do nothing
                    }
                    else
                    {
                        child.Draw(batcher, x + child.X, y + child.Y);
                    }

                    height += child.Height;
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

        public override void Remove(Control c)
        {
            if (c is ScrollAreaItem)
                base.Remove(c);
            else
            {
                // Try to find the wrapped control
                ScrollAreaItem wrapper = Children.OfType<ScrollAreaItem>().FirstOrDefault(o => o.Children.Contains(c));
                base.Remove(wrapper);
            }
        }

        public override void Add(Control c, int page = 0)
        {
            ScrollAreaItem item = new ScrollAreaItem
            {
                CanMove = true
            };
            item.Add(c);
            base.Add(item, page);
        }

        public void Add(ScrollAreaItem c, int page = 0)
        {
            c.CanMove = true;
            base.Add(c, page);
        }

        public override void Clear()
        {
            for (int i = 1; i < Children.Count; i++)
                Children[i].Dispose();
        }

        private void CalculateScrollBarMaxValue()
        {
            _scrollBar.Height = _scroll_max_height >= 0 ? _scroll_max_height : Height;
            bool maxValue = _scrollBar.Value == _scrollBar.MaxValue && _scrollBar.MaxValue != 0;
            int height = ScissorRectangle.Y;

            for (int i = 1; i < Children.Count; i++)
            {
                if (Children[i].IsVisible)
                    height += Children[i].Height;
            }

            height -= _scrollBar.Height;

            height -= ScissorRectangle.Y + ScissorRectangle.Height;

            //if (_isNormalScroll)
            //    height += 40;

            if (height > 0)
            {
                _scrollBar.MaxValue = height;

                if (maxValue)
                    _scrollBar.Value = _scrollBar.MaxValue;
            }
            else
            {
                _scrollBar.MaxValue = 0;
                _scrollBar.Value = 0;
            }
        }
    }

    internal class ScrollAreaItem : Control
    {
        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (Children.Count == 0)
                Dispose();

            WantUpdateSize = true;
        }

        public override void OnPageChanged()
        {
            int maxheight = Children.Count > 0 ? Children.Sum(o => o.IsVisible ? o.Y < 0 ? o.Height + o.Y : o.Height : 0) : 0;
            IsVisible = maxheight > 0;
            Height = maxheight;
            Parent?.OnPageChanged();
        }
    }
}