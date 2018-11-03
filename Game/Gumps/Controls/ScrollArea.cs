#region license

//  Copyright (C) 2018 ClassicUO Development Community on Github
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

using ClassicUO.Input;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.Controls
{
    internal class ScrollArea : GumpControl
    {
        private readonly IScrollBar _scrollBar;
        private bool _needUpdate = true;

        public ScrollArea(int x, int y, int w, int h, bool normalScrollbar)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;

            if (normalScrollbar)
                _scrollBar = new ScrollBar(this, Width - 14, 0, Height);
            else
            {
                _scrollBar = new ScrollFlag(this)
                {
                    X = Width - 14, Height = h
                };
            }

            _scrollBar.MinValue = 0;
            _scrollBar.MaxValue = Height;
            AcceptMouseInput = false;

            WantUpdateSize = false;
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (_needUpdate)
            {
                CalculateScrollBarMaxValue();
                _scrollBar.IsVisible = _scrollBar.MaxValue > _scrollBar.MinValue;
                _needUpdate = false;
            }

            base.Update(totalMS, frameMS);
        }

        protected override void OnInitialize()
        {
            _needUpdate = true;
            base.OnInitialize();
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Point position, Vector3? hue = null)
        {

            Children[0].Draw(spriteBatch, new Point(position.X + Children[0].X, position.Y + Children[0].Y));


           


            Rectangle scissor = ScissorStack.CalculateScissors(spriteBatch.TransformMatrix, Bounds);

            if (ScissorStack.PushScissors(scissor))
            {
                int height = 0;
                int maxheight = _scrollBar.Value + _scrollBar.Height;


                for (int i = 1; i < Children.Count; i++)
                {
                    GumpControl child = Children[i];

                    if (!child.IsVisible)
                        continue;

                    int y = height - _scrollBar.Value;

                    int tempY = child.Y;
                    child.Y += y;

                    if (height + child.Height <= _scrollBar.Value)
                    {
                        // do nothing
                    }
                    else if (height + child.Height <= maxheight)
                    {
                        if (child.Y < 0)
                        {
                            // TODO: Future implementation
                        }
                        else
                            child.Draw(spriteBatch, new Point(position.X + child.X, position.Y + child.Y));
                    }

                    height += child.Height;


                    child.Y = tempY;
                }


                ScissorStack.PopScissors();
            }

            return true;
        }

        protected override void OnMouseWheel(MouseEvent delta)
        {
            switch (delta)
            {
                case MouseEvent.WheelScrollUp:
                    _scrollBar.Value -= _scrollBar.ScrollStep;

                    break;
                case MouseEvent.WheelScrollDown:
                    _scrollBar.Value += _scrollBar.ScrollStep;

                    break;
            }
        }

        protected override void OnMouseClick(int x, int y, MouseButton button)
        {
            base.OnMouseClick(x, y, button);
        }

        protected override void OnChildAdded()
        {
            _needUpdate = true;
        }

        protected override void OnChildRemoved()
        {
            _needUpdate = true;
        }

        public override void Clear()
        {
            foreach (GumpControl child in Children)
            {
                if (child is IScrollBar)
                    continue;
                child.Dispose();
            }
        }

        private void CalculateScrollBarMaxValue()
        {
            _scrollBar.Height = Height;
            bool maxValue = _scrollBar.Value == _scrollBar.MaxValue;
            int height = 0;

            for (int i = 1; i < Children.Count; i++)
            {
                height += Children[i].Height;
            }

            height -= _scrollBar.Height;

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
}