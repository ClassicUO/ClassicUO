// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;

namespace ClassicUO.Game.UI.Controls
{
    internal class ScrollBar : ScrollBarBase
    {
        private Rectangle _rectSlider,
            _emptySpace;

        const ushort BUTTON_UP_0 = 251;
        const ushort BUTTON_UP_1 = 250;
        const ushort BUTTON_DOWN_0 = 253;
        const ushort BUTTON_DOWN_1 = 252;
        const ushort BACKGROUND_0 = 257;
        const ushort BACKGROUND_1 = 256;
        const ushort BACKGROUND_2 = 255;
        const ushort SLIDER = 254;

        public ScrollBar(int x, int y, int height)
        {
            Height = height;
            Location = new Point(x, y);
            AcceptMouseInput = true;

            ref readonly var gumpInfoUp = ref Client.Game.UO.Gumps.GetGump(BUTTON_UP_0);
            ref readonly var gumpInfoDown = ref Client.Game.UO.Gumps.GetGump(BUTTON_DOWN_0);
            ref readonly var gumpInfoBackground = ref Client.Game.UO.Gumps.GetGump(BACKGROUND_0);
            ref readonly var gumpInfoSlider = ref Client.Game.UO.Gumps.GetGump(SLIDER);

            Width = gumpInfoBackground.UV.Width;

            _rectDownButton = new Rectangle(
                0,
                Height - gumpInfoDown.UV.Height,
                gumpInfoDown.UV.Width,
                gumpInfoDown.UV.Height
            );
            _rectUpButton = new Rectangle(0, 0, gumpInfoUp.UV.Width, gumpInfoUp.UV.Height);
            _rectSlider = new Rectangle(
                (gumpInfoBackground.UV.Width - gumpInfoSlider.UV.Width) >> 1,
                gumpInfoUp.UV.Height + _sliderPosition,
                gumpInfoSlider.UV.Width,
                gumpInfoSlider.UV.Height
            );
            _emptySpace.X = 0;
            _emptySpace.Y = gumpInfoUp.UV.Height;
            _emptySpace.Width = gumpInfoSlider.UV.Width;
            _emptySpace.Height = Height - (gumpInfoDown.UV.Height + gumpInfoUp.UV.Height);
        }

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            if (Height <= 0 || !IsVisible)
            {
                return false;
            }

            var hueVector = ShaderHueTranslator.GetHueVector(0);

            ref readonly var gumpInfoUp0 = ref Client.Game.UO.Gumps.GetGump(BUTTON_UP_0);
            ref readonly var gumpInfoUp1 = ref Client.Game.UO.Gumps.GetGump(BUTTON_UP_1);
            ref readonly var gumpInfoDown0 = ref Client.Game.UO.Gumps.GetGump(BUTTON_DOWN_0);
            ref readonly var gumpInfoDown1 = ref Client.Game.UO.Gumps.GetGump(BUTTON_DOWN_1);
            ref readonly var gumpInfoBackground0 = ref Client.Game.UO.Gumps.GetGump(BACKGROUND_0);
            ref readonly var gumpInfoBackground1 = ref Client.Game.UO.Gumps.GetGump(BACKGROUND_1);
            ref readonly var gumpInfoBackground2 = ref Client.Game.UO.Gumps.GetGump(BACKGROUND_2);
            ref readonly var gumpInfoSlider = ref Client.Game.UO.Gumps.GetGump(SLIDER);

            // Track: top cap + tiled middle + bottom cap (or just tiled middle when short).
            int middleHeight =
                Height
                - gumpInfoUp0.UV.Height
                - gumpInfoDown0.UV.Height
                - gumpInfoBackground0.UV.Height
                - gumpInfoBackground2.UV.Height;

            if (middleHeight > 0)
            {
                // Top cap
                renderLists.AddGumpSprite(
                    gumpInfoBackground0.Texture, gumpInfoBackground0.UV,
                    new Rectangle(x, y + gumpInfoUp0.UV.Height, gumpInfoBackground0.UV.Width, gumpInfoBackground0.UV.Height),
                    hueVector, layerDepthRef);

                // Tiled middle
                renderLists.AddGumpSpriteTiled(
                    gumpInfoBackground1.Texture, gumpInfoBackground1.UV,
                    new Rectangle(
                        x,
                        y + gumpInfoUp1.UV.Height + gumpInfoBackground0.UV.Height,
                        gumpInfoBackground0.UV.Width,
                        middleHeight),
                    hueVector, layerDepthRef);

                // Bottom cap
                renderLists.AddGumpSprite(
                    gumpInfoBackground2.Texture, gumpInfoBackground2.UV,
                    new Rectangle(
                        x,
                        y + Height - gumpInfoDown0.UV.Height - gumpInfoBackground2.UV.Height,
                        gumpInfoBackground2.UV.Width,
                        gumpInfoBackground2.UV.Height),
                    hueVector, layerDepthRef);
            }
            else
            {
                middleHeight = Height - gumpInfoUp0.UV.Height - gumpInfoDown0.UV.Height;

                renderLists.AddGumpSpriteTiled(
                    gumpInfoBackground1.Texture, gumpInfoBackground1.UV,
                    new Rectangle(
                        x,
                        y + gumpInfoUp0.UV.Height,
                        gumpInfoBackground0.UV.Width,
                        middleHeight),
                    hueVector, layerDepthRef);
            }

            // Up button (pressed variant swaps the graphic)
            var upInfo = _btUpClicked ? gumpInfoUp1 : gumpInfoUp0;
            renderLists.AddGumpSprite(
                upInfo.Texture, upInfo.UV,
                new Rectangle(x, y, upInfo.UV.Width, upInfo.UV.Height),
                hueVector, layerDepthRef);

            // Down button
            var downInfo = _btDownClicked ? gumpInfoDown1 : gumpInfoDown0;
            renderLists.AddGumpSprite(
                downInfo.Texture, downInfo.UV,
                new Rectangle(x, y + Height - gumpInfoDown0.UV.Height, downInfo.UV.Width, downInfo.UV.Height),
                hueVector, layerDepthRef);

            // Slider thumb
            if (MaxValue > MinValue && middleHeight > 0)
            {
                renderLists.AddGumpSprite(
                    gumpInfoSlider.Texture, gumpInfoSlider.UV,
                    new Rectangle(
                        x + ((gumpInfoBackground0.UV.Width - gumpInfoSlider.UV.Width) >> 1),
                        y + gumpInfoUp0.UV.Height + _sliderPosition,
                        gumpInfoSlider.UV.Width,
                        gumpInfoSlider.UV.Height),
                    hueVector, layerDepthRef);
            }

            return base.AddToRenderLists(renderLists, x, y, ref layerDepthRef);
        }

        protected override int GetScrollableArea()
        {
            ref readonly var gumpInfoUp = ref Client.Game.UO.Gumps.GetGump(BUTTON_UP_0);
            ref readonly var gumpInfoDown = ref Client.Game.UO.Gumps.GetGump(BUTTON_DOWN_0);
            ref readonly var gumpInfoSlider = ref Client.Game.UO.Gumps.GetGump(SLIDER);

            return Height
                - gumpInfoUp.UV.Height
                - gumpInfoDown.UV.Height
                - gumpInfoSlider.UV.Height;
        }

        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            base.OnMouseDown(x, y, button);

            if (_btnSliderClicked && _emptySpace.Contains(x, y))
            {
                CalculateByPosition(x, y);
            }
        }

        protected override void CalculateByPosition(int x, int y)
        {
            if (y != _clickPosition.Y)
            {
                y -= _emptySpace.Y + (_rectSlider.Height >> 1);

                if (y < 0)
                {
                    y = 0;
                }

                int scrollableArea = GetScrollableArea();

                if (y > scrollableArea)
                {
                    y = scrollableArea;
                }

                _sliderPosition = y;
                _clickPosition.X = x;
                _clickPosition.Y = y;

                ref readonly var gumpInfoUp = ref Client.Game.UO.Gumps.GetGump(BUTTON_UP_0);
                ref readonly var gumpInfoDown = ref Client.Game.UO.Gumps.GetGump(BUTTON_DOWN_0);
                ref readonly var gumpInfoSlider = ref Client.Game.UO.Gumps.GetGump(SLIDER);

                if (
                    y == 0
                    && _clickPosition.Y < gumpInfoUp.UV.Height + (gumpInfoSlider.UV.Height >> 1)
                )
                {
                    _clickPosition.Y = gumpInfoUp.UV.Height + (gumpInfoSlider.UV.Height >> 1);
                }
                else if (
                    y == scrollableArea
                    && _clickPosition.Y
                        > Height - gumpInfoDown.UV.Height - (gumpInfoSlider.UV.Height >> 1)
                )
                {
                    _clickPosition.Y =
                        Height - gumpInfoDown.UV.Height - (gumpInfoSlider.UV.Height >> 1);
                }

                _value = (int)
                    Math.Round(y / (float)scrollableArea * (MaxValue - MinValue) + MinValue);
            }
        }

        public override bool Contains(int x, int y)
        {
            return x >= 0 && x <= Width && y >= 0 && y <= Height;
        }
    }
}
