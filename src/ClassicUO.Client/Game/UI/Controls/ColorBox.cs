// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Scenes;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

using ClassicUO.Game;

namespace ClassicUO.Game.UI.Controls
{
    internal class ColorBox : Control
    {
        public ColorBox(GameContext context, int width, int height, ushort hue) : base(context)
        {
            CanMove = false;

            Width = width;
            Height = height;
            Hue = hue;

            WantUpdateSize = false;
        }

        public ushort Hue { get;  set; }

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            float layerDepth = layerDepthRef;
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(Hue);

            renderLists.AddGumpNoAtlas
            (
                (batcher) =>
                {
                    batcher.Draw
                    (
                        SolidColorTextureCache.GetTexture(Color.White),
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
                }
            );

            return true;
        }
    }
}