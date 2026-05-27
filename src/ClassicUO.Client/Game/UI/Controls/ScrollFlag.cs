// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Scenes;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;

namespace ClassicUO.Game.UI.Controls
{
    internal class ScrollFlag : ScrollBarBase
    {
        private readonly bool _showButtons;

        const ushort BUTTON_UP = 0x0824;
        const ushort BUTTON_DOWN = 0x0825;
        const ushort BUTTON_FLAG = 0x0828;

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

            ref readonly var gumpInfoFlag = ref Client.Game.UO.Gumps.GetGump(BUTTON_FLAG);

            if (gumpInfoFlag.Texture == null)
            {
                Dispose();

                return;
            }

            ref readonly var gumpInfoUp = ref Client.Game.UO.Gumps.GetGump(BUTTON_UP);
            ref readonly var gumpInfoDown = ref Client.Game.UO.Gumps.GetGump(BUTTON_DOWN);

            Width = gumpInfoFlag.UV.Width;
            Height = gumpInfoFlag.UV.Height;

            _rectUpButton = new Rectangle(0, 0, gumpInfoUp.UV.Width, gumpInfoUp.UV.Height);
            _rectDownButton = new Rectangle(
                0,
                Height,
                gumpInfoDown.UV.Width,
                gumpInfoDown.UV.Height
            );

            WantUpdateSize = false;
        }

        public override ClickPriority Priority { get; set; } = ClickPriority.High;

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            float layerDepth = layerDepthRef;
            renderLists.AddGumpWithAtlas
            (
                (batcher) =>
                {
                    var hueVector = ShaderHueTranslator.GetHueVector(0);

                    ref readonly var gumpInfoFlag = ref Client.Game.UO.Gumps.GetGump(BUTTON_FLAG);
                    ref readonly var gumpInfoUp = ref Client.Game.UO.Gumps.GetGump(BUTTON_UP);
                    ref readonly var gumpInfoDown = ref Client.Game.UO.Gumps.GetGump(BUTTON_DOWN);

                    if (MaxValue != MinValue && gumpInfoFlag.Texture != null)
                    {
                        batcher.Draw(
                            gumpInfoFlag.Texture,
                            new Vector2(x, y + _sliderPosition),
                            gumpInfoFlag.UV,
                            hueVector,
                            layerDepth
                        );
                    }

                    if (_showButtons)
                    {
                        if (gumpInfoUp.Texture != null)
                        {
                            batcher.Draw(gumpInfoUp.Texture, new Vector2(x, y), gumpInfoUp.UV, hueVector, layerDepth);
                        }

                        if (gumpInfoDown.Texture != null)
                        {
                            batcher.Draw(
                                gumpInfoDown.Texture,
                                new Vector2(x, y + Height),
                                gumpInfoDown.UV,
                                hueVector,
                                layerDepth
                            );
                        }
                    }
                    return true;
                }
            );
            return base.AddToRenderLists(renderLists, x, y, ref layerDepthRef);
        }

        protected override int GetScrollableArea()
        {
            ref readonly var gumpInfoFlag = ref Client.Game.UO.Gumps.GetGump(BUTTON_FLAG);

            return Height - gumpInfoFlag.UV.Height;
        }

        protected override void CalculateByPosition(int x, int y)
        {
            if (y != _clickPosition.Y)
            {
                ref readonly var gumpInfoFlag = ref Client.Game.UO.Gumps.GetGump(BUTTON_FLAG);
                int height = gumpInfoFlag.UV.Height;

                y -= (height >> 1);

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

                if (y == 0 && _clickPosition.Y < height >> 1)
                {
                    _clickPosition.Y = height >> 1;
                }
                else if (y == scrollableArea && _clickPosition.Y > Height - (height >> 1))
                {
                    _clickPosition.Y = Height - (height >> 1);
                }

                _value = (int)
                    Math.Round(y / (float)scrollableArea * (MaxValue - MinValue) + MinValue);
            }
        }

        public override bool Contains(int x, int y)
        {
            ref readonly var gumpInfoFlag = ref Client.Game.UO.Gumps.GetGump(BUTTON_FLAG);

            if (gumpInfoFlag.Texture == null)
            {
                return false;
            }

            y -= _sliderPosition;

            return Client.Game.UO.Gumps.PixelCheck(BUTTON_FLAG, x, y);
        }
    }
}
