// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Scenes;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal sealed class AlphaBlendControl : Control
    {
        public AlphaBlendControl(float alpha = 0.5f)
        {
            Alpha = alpha;
            AcceptMouseInput = false;
        }

        public ushort Hue { get; set; }

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            float layerDepth = layerDepthRef;
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(Hue, false, Alpha);

            renderLists.AddGumpNoAtlas
            (
                batcher =>
                {
                    batcher.Draw
                    (
                        SolidColorTextureCache.GetTexture(Color.Black),
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