// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Scenes;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Controls
{
    internal class HitBox : Control
    {
        public HitBox
        (
            int x,
            int y,
            int w,
            int h,
            string tooltip = null,
            float alpha = 0.25f
        )
        {
            CanMove = false;
            AcceptMouseInput = true;
            Alpha = alpha;
            _texture = SolidColorTextureCache.GetTexture(Color.White);

            X = x;
            Y = y;
            Width = w;
            Height = h;
            WantUpdateSize = false;

            SetTooltip(tooltip);
        }


        public override ClickPriority Priority { get; set; } = ClickPriority.High;
        protected readonly Texture2D _texture;


        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            float layerDepth = layerDepthRef;
            if (IsDisposed)
            {
                return false;
            }

            if (MouseIsOver)
            {
                Vector3 hueVector = ShaderHueTranslator.GetHueVector
                                    (
                                        0,
                                        false,
                                        Alpha,
                                        true
                                    );

                renderLists.AddGumpNoAtlas(
                    batcher => 
                    {
                        batcher.Draw
                        (
                            _texture,
                            new Vector2(x, y),
                            new Rectangle(0, 0, Width, Height),
                            hueVector,
                            layerDepth
                        );
                        return true;
                    }
                );
            }

            return base.AddToRenderLists(renderLists, x, y, ref layerDepthRef);
        }
    }
}