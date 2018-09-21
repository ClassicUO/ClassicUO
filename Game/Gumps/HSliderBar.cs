using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps
{
    enum HSliderBarStyle
    {
        MetalWidgetRecessedBar,
        BlueWidgetNoBar
    }

    class HSliderBar : GumpControl
    {
        private int _value;
        private int _newValue;
        private int _sliderX;
        private SpriteTexture _gumpWidget;
        private SpriteTexture[] _gumpSpliderBackground;
        private HSliderBarStyle _style;
        private List<HSliderBar> _pairedSliders = new List<HSliderBar>();
        private Rectangle _rect;
        private bool _clicked;
        private Point _clickPosition;


        public HSliderBar(int x, int y, int w, int min, int max, int value, HSliderBarStyle style) : base()
        {
            X = x;
            Y = y;
            MinValue = min;
            MaxValue = max;
            BarWidth = w;
            Value = value;
            _style = style;
        }


        public int MinValue { get; set; }
        public int MaxValue { get; set; }
        public int BarWidth { get; set; }

        public int Value
        {
            get => _value;
            set
            {
                _value = _newValue = value;
                if (IsInitialized)
                    RecalculateSliderX();
            }
        }


        public override void Update(double totalMS, double frameMS)
        {
            if (_gumpWidget == null)
            {
                switch (_style)
                {
                    case HSliderBarStyle.MetalWidgetRecessedBar:
                        _gumpSpliderBackground = new SpriteTexture[3]
                        {
                            IO.Resources.Gumps.GetGumpTexture(213),
                            IO.Resources.Gumps.GetGumpTexture(214),
                            IO.Resources.Gumps.GetGumpTexture(215)
                        };
                        _gumpWidget = IO.Resources.Gumps.GetGumpTexture(216);
                        break;
                    case HSliderBarStyle.BlueWidgetNoBar:
                        _gumpWidget = IO.Resources.Gumps.GetGumpTexture(0x845);
                        break;
                }

                Width = BarWidth;
                Height = _gumpWidget.Height;
                RecalculateSliderX();
            }

            ModifyPairedValues(_newValue - Value);
            _gumpWidget.Ticks = (long)totalMS;

            _value = _newValue;

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
        {
            if (_gumpSpliderBackground != null)
            {
                spriteBatch.Draw2D(_gumpSpliderBackground[0], new Vector3(position.X, position.Y, 0), Vector3.Zero);
                spriteBatch.Draw2DTiled(_gumpSpliderBackground[1], new Rectangle((int)position.X + _gumpSpliderBackground[0].Width, (int)position.Y, BarWidth - _gumpSpliderBackground[2].Width - _gumpSpliderBackground[0].Width, _gumpSpliderBackground[1].Height), Vector3.Zero);
                spriteBatch.Draw2D(_gumpSpliderBackground[2], new Vector3(position.X + BarWidth - _gumpSpliderBackground[2].Width, position.Y, 0), Vector3.Zero);
            }
            spriteBatch.Draw2D(_gumpWidget, new Vector3(position.X + _sliderX, position.Y, 0), Vector3.Zero);

            return base.Draw(spriteBatch, position, hue);
        }

        protected override void OnMouseDown(int x, int y, MouseButton button)
        {
            _clicked = true;
            _clickPosition.X = x;
            _clickPosition.Y = y;
        }

        protected override void OnMouseUp(int x, int y, MouseButton button)
        {
            _clicked = false;
        }

        protected override void OnMouseEnter(int x, int y)
        {
            if (_clicked)
            {
                _sliderX = _sliderX + (x - _clickPosition.X);
                if (_sliderX < 0)
                    _sliderX = 0;
                if (_sliderX > BarWidth - _gumpWidget.Width)
                    _sliderX = BarWidth - _gumpWidget.Width;
                _clickPosition.X = x;
                _clickPosition.Y = y;
                if (_clickPosition.X < _gumpWidget.Width / 2)
                    _clickPosition.X = _gumpWidget.Width / 2;
                if (_clickPosition.X > BarWidth - _gumpWidget.Width / 2)
                    _clickPosition.X = BarWidth - _gumpWidget.Width / 2;
                _newValue = (int)(_sliderX / (float)(BarWidth - _gumpWidget.Width) * (MaxValue - MinValue)) + MinValue;
            }
        }


        protected override bool Contains(int x, int y)
        {
            _rect.X = _sliderX;
            _rect.Y = 0;
            _rect.Width = _gumpWidget.Width;
            _rect.Height = _gumpWidget.Height;

            return _rect.Contains(x, y);
        }


        private void RecalculateSliderX() =>
            _sliderX = (int) ((BarWidth - _gumpWidget.Width) * ((Value - MinValue) / (MaxValue - MinValue)));

        public void AddParisSlider(HSliderBar s) => _pairedSliders.Add(s);

        private void ModifyPairedValues(int delta)
        {
            if (_pairedSliders.Count == 0)
                return;

            bool updateSinceLastCycle = true;
            int d = (delta > 0) ? -1 : 1;
            int points = Math.Abs(delta);
            int sliderIndex = Value % _pairedSliders.Count;
            while (points > 0)
            {
                if (d > 0)
                {
                    if (_pairedSliders[sliderIndex].Value < _pairedSliders[sliderIndex].MaxValue)
                    {
                        updateSinceLastCycle = true;
                        _pairedSliders[sliderIndex].Value += d;
                        points--;
                    }
                }
                else
                {
                    if (_pairedSliders[sliderIndex].Value > _pairedSliders[sliderIndex].MinValue)
                    {
                        updateSinceLastCycle = true;
                        _pairedSliders[sliderIndex].Value += d;
                        points--;
                    }
                }

                sliderIndex++;
                if (sliderIndex == _pairedSliders.Count)
                {
                    if (!updateSinceLastCycle)
                        return;
                    updateSinceLastCycle = false;
                    sliderIndex = 0;
                }
            }
        }
        
    }
}
