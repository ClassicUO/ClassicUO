using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.Controls
{
    class ScrollArea : GumpControl
    {
        private readonly IScrollBar _scrollBar;

        public ScrollArea(int x, int y, int w, int h, bool normalScrollbar) : base()
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;

            if (normalScrollbar)
            {
                _scrollBar = new ScrollBar(this, Width - 14, 0, Height);
            }
            else
            {
                _scrollBar = new ScrollFlag(this)
                {
                    X = Width - 14,
                    Height = h,
                };
            }

            _scrollBar.MinValue = 0;
            _scrollBar.MaxValue = Height;

            IgnoreParentFill = true;
        }


        public override void Update(double totalMS, double frameMS)
        {
            _scrollBar.Height = Height;
            CalculateScrollBarMaxValue();
            _scrollBar.IsVisible = _scrollBar.MaxValue > _scrollBar.MinValue;

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
        {
            int height = 0;
            int maxheight = _scrollBar.Value + _scrollBar.Height;

            for (int i = 0; i < Children.Count; i++)
            {
                var child = Children[i];

                if (child is IScrollBar)
                {
                    child.Draw(spriteBatch, new Vector3(position.X + child.X, position.Y + child.Y, 0));                    
                }
                else
                {
                    child.Y = height - _scrollBar.Value;

                    if (height + child.Height <= _scrollBar.Value)
                    {
                        
                    }
                    else if(height + child.Height <= maxheight)
                    {
                        child.Draw(spriteBatch, new Vector3(position.X + child.X, position.Y + child.Y, 0));

                    }

                    height += child.Height;
                }

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


        private void CalculateScrollBarMaxValue()
        {
            bool maxValue = _scrollBar.Value == _scrollBar.MaxValue;

            int height = 0;
            for (int i = 0; i < Children.Count; i++)
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
