// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace ClassicUO.Game.UI.Controls
{
    internal enum HSliderBarStyle
    {
        MetalWidgetRecessedBar,
        BlueWidgetNoBar
    }

    internal class HSliderBar : Control
    {
        private bool _clicked;
        private readonly bool _drawUp;
        private readonly List<HSliderBar> _pairedSliders = new List<HSliderBar>();
        private int _sliderX;
        private readonly HSliderBarStyle _style;
        private readonly RenderedText _text;
        private int _value = -1;

        public HSliderBar(
            int x,
            int y,
            int w,
            int min,
            int max,
            int value,
            HSliderBarStyle style,
            bool hasText = false,
            byte font = 0,
            ushort color = 0,
            bool unicode = true,
            bool drawUp = false
        )
        {
            X = x;
            Y = y;

            if (hasText)
            {
                _text = RenderedText.Create(string.Empty, color, font, unicode);
                _drawUp = drawUp;
            }

            MinValue = min;
            MaxValue = max;
            BarWidth = w;
            _style = style;
            AcceptMouseInput = true;

            ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(
                (uint)(_style == HSliderBarStyle.MetalWidgetRecessedBar ? 216 : 0x845)
            );

            Width = BarWidth;

            if (gumpInfo.Texture != null)
            {
                Height = gumpInfo.UV.Height;
            }

            CalculateOffset();

            Value = value;
        }

        public int MinValue { get; set; }

        public int MaxValue { get; set; }

        public int BarWidth { get; set; }

        public float Percents { get; private set; }

        public int Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    int oldValue = _value;
                    _value = /*_newValue =*/
                    value;
                    //if (IsInitialized)
                    //    RecalculateSliderX();

                    if (_value < MinValue)
                    {
                        _value = MinValue;
                    }
                    else if (_value > MaxValue)
                    {
                        _value = MaxValue;
                    }

                    if (_text != null)
                    {
                        _text.Text = Value.ToString();
                    }

                    if (_value != oldValue)
                    {
                        ModifyPairedValues(_value - oldValue);

                        CalculateOffset();
                    }

                    ValueChanged.Raise();
                }
            }
        }

        public event EventHandler ValueChanged;

        public override void Update()
        {
            base.Update();

            if (_clicked)
            {
                int x = Mouse.Position.X - X - ParentX;
                int y = Mouse.Position.Y - Y - ParentY;

                CalculateNew(x);
            }
        }

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

            if (_style == HSliderBarStyle.MetalWidgetRecessedBar)
            {
                ref readonly var gumpInfo0 = ref Client.Game.UO.Gumps.GetGump(213);
                ref readonly var gumpInfo1 = ref Client.Game.UO.Gumps.GetGump(214);
                ref readonly var gumpInfo2 = ref Client.Game.UO.Gumps.GetGump(215);
                ref readonly var gumpInfo3 = ref Client.Game.UO.Gumps.GetGump(216);

                // Left cap
                renderLists.AddGumpSprite(
                    gumpInfo0.Texture, gumpInfo0.UV,
                    new Rectangle(x, y, gumpInfo0.UV.Width, gumpInfo0.UV.Height),
                    hueVector, layerDepthRef);

                // Middle track (tiled)
                renderLists.AddGumpSpriteTiled(
                    gumpInfo1.Texture, gumpInfo1.UV,
                    new Rectangle(
                        x + gumpInfo0.UV.Width,
                        y,
                        BarWidth - gumpInfo2.UV.Width - gumpInfo0.UV.Width,
                        gumpInfo1.UV.Height),
                    hueVector, layerDepthRef);

                // Right cap
                renderLists.AddGumpSprite(
                    gumpInfo2.Texture, gumpInfo2.UV,
                    new Rectangle(
                        x + BarWidth - gumpInfo2.UV.Width, y,
                        gumpInfo2.UV.Width, gumpInfo2.UV.Height),
                    hueVector, layerDepthRef);

                // Thumb
                renderLists.AddGumpSprite(
                    gumpInfo3.Texture, gumpInfo3.UV,
                    new Rectangle(x + _sliderX, y, gumpInfo3.UV.Width, gumpInfo3.UV.Height),
                    hueVector, layerDepthRef);
            }
            else
            {
                ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(idx: 0x845);

                renderLists.AddGumpSprite(
                    gumpInfo.Texture, gumpInfo.UV,
                    new Rectangle(x + _sliderX, y, gumpInfo.UV.Width, gumpInfo.UV.Height),
                    hueVector, layerDepthRef);
            }

            if (_text != null)
            {
                int tx = _drawUp ? x : x + BarWidth + 2;
                int ty = _drawUp ? y - _text.Height : y + (Height >> 1) - (_text.Height >> 1);
                renderLists.AddGumpNoAtlas(_text, tx, ty, layerDepthRef);
            }

            return base.AddToRenderLists(renderLists, x, y, ref layerDepthRef);
        }

        private void InternalSetValue(int value)
        {
            _value = value;
            CalculateOffset();

            if (_text != null)
            {
                _text.Text = Value.ToString();
            }
        }

        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            if (button != MouseButtonType.Left)
            {
                return;
            }

            _clicked = true;
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button != MouseButtonType.Left)
            {
                return;
            }

            _clicked = false;
            CalculateNew(x);
        }

        protected override void OnMouseWheel(MouseEventType delta)
        {
            switch (delta)
            {
                case MouseEventType.WheelScrollUp:
                    Value++;

                    break;

                case MouseEventType.WheelScrollDown:
                    Value--;

                    break;
            }

            CalculateOffset();
        }

        private void CalculateNew(int x)
        {
            int len = BarWidth;
            int maxValue = MaxValue - MinValue;

            ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(
                (uint)(_style == HSliderBarStyle.MetalWidgetRecessedBar ? 216 : 0x845)
            );

            len -= gumpInfo.UV.Width;
            float perc = x / (float)len * 100.0f;
            Value = (int)(maxValue * perc / 100.0f) + MinValue;
            CalculateOffset();
        }

        private void CalculateOffset()
        {
            if (Value < MinValue)
            {
                Value = MinValue;
            }
            else if (Value > MaxValue)
            {
                Value = MaxValue;
            }

            int value = Value - MinValue;
            int maxValue = MaxValue - MinValue;
            int length = BarWidth;

            ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(
                (uint)(_style == HSliderBarStyle.MetalWidgetRecessedBar ? 216 : 0x845)
            );
            length -= gumpInfo.UV.Width;

            if (maxValue > 0)
            {
                Percents = value / (float)maxValue * 100.0f;
            }
            else
            {
                Percents = 0;
            }

            _sliderX = (int)(length * Percents / 100.0f);

            if (_sliderX < 0)
            {
                _sliderX = 0;
            }
        }

        public void AddParisSlider(HSliderBar s)
        {
            _pairedSliders.Add(s);
        }

        private void ModifyPairedValues(int delta)
        {
            if (_pairedSliders.Count == 0)
            {
                return;
            }

            bool updateSinceLastCycle = true;
            int d = delta > 0 ? -1 : 1;
            int points = Math.Abs(delta);
            int sliderIndex = Value % _pairedSliders.Count;

            while (points > 0)
            {
                if (d > 0)
                {
                    if (_pairedSliders[sliderIndex].Value < _pairedSliders[sliderIndex].MaxValue)
                    {
                        updateSinceLastCycle = true;

                        _pairedSliders[sliderIndex].InternalSetValue(
                            _pairedSliders[sliderIndex].Value + d
                        );

                        points--;
                    }
                }
                else
                {
                    if (_pairedSliders[sliderIndex].Value > _pairedSliders[sliderIndex].MinValue)
                    {
                        updateSinceLastCycle = true;

                        _pairedSliders[sliderIndex].InternalSetValue(
                            _pairedSliders[sliderIndex]._value + d
                        );

                        points--;
                    }
                }

                sliderIndex++;

                if (sliderIndex == _pairedSliders.Count)
                {
                    if (!updateSinceLastCycle)
                    {
                        return;
                    }

                    updateSinceLastCycle = false;
                    sliderIndex = 0;
                }
            }
        }

        public override void Dispose()
        {
            _text?.Destroy();
            base.Dispose();
        }
    }
}
