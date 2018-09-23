using System;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps
{
    public class ScrollFlag : GumpControl, IScrollBar
    {
        private SpriteTexture _texture;
        private int _sliderExtentTop, _sliderExtentHeight;
        private float _sliderPosition;
        private float _value;
        private int _max, _min;

        private bool _btnSliderClicked;
        private Point _clickPosition;
        private Rectangle _rect;


        public ScrollFlag(GumpControl parent, int x, int y, int height) : this(parent)
        {
            Location = new Point(x, y);
            _sliderExtentTop = y;
            _sliderExtentHeight = height;
        }

        public ScrollFlag(GumpControl parent) : base(parent) => AcceptMouseInput = true;


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

        protected override void OnInitialize()
        {
            base.OnInitialize();

            _texture = IO.Resources.Gumps.GetGumpTexture(0x0828);
            Width = _texture.Width;
            Height = _texture.Height;
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (MaxValue <= MinValue || MinValue >= MaxValue)
                Value = MaxValue = MinValue;

            _sliderPosition = GetSliderYPosition();

            _texture.Ticks = (long) totalMS;
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
        {
            if (MaxValue != MinValue)
            {
                spriteBatch.Draw2D(_texture, new Vector3(position.X - 5, position.Y + _sliderPosition, 0),
                    Vector3.Zero);
            }

            return base.Draw(spriteBatch, position, hue);
        }

        private float GetSliderYPosition()
        {
            if (MaxValue - MinValue == 0)
                return 0f;
            return GetScrollableArea() * ((_value - MinValue) / (MaxValue - MinValue));
        }

        private float GetScrollableArea() => Height - _texture.Height;

        protected override void OnMouseDown(int x, int y, MouseButton button)
        {
            if (Contains(x, y))
            {
                _btnSliderClicked = true;
                _clickPosition = new Point(x, y);
            }
        }

        protected override void OnMouseUp(int x, int y, MouseButton button)
        {
            _btnSliderClicked = false;
        }

        protected override void OnMouseEnter(int x, int y)
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
                    Value--;
                    break;
                case MouseEvent.WheelScrollDown:
                    Value++;
                    break;
            }
        }

        protected override bool Contains(int x, int y)
        {
            x -= 5;
            _rect.Y = (int)_sliderPosition;
            _rect.Width = _texture.Width;
            _rect.Height = _texture.Height;

            return _rect.Contains(x, y);
        }

        bool IScrollBar.Contains(int x, int y)
            => Contains(x, y);
    }
}