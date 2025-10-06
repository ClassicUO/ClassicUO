// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Scenes;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Controls
{
    internal class Line : Control
    {
        private readonly Texture2D _texture;

        public Line(int x, int y, int w, int h, uint color)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;

            _texture = SolidColorTextureCache.GetTexture(new Color { PackedValue = color });
        }

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            float layerDepth = layerDepthRef;
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0, false, Alpha);

            renderLists.AddGumpNoAtlas(batcher =>
            {
                batcher.Draw
                (
                    _texture,
                    new Rectangle
                    (
                        x,
                        y,
                        Width,
                        Height
                    ),
                    hueVector,
                    layerDepth
                );

                return true;
            });

            return true;
        }
    }
}