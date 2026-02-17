// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class ClickableColorBox : ColorBox
    {
        private readonly World _world;

        public ClickableColorBox
        (
            World world,
            int x,
            int y,
            int w,
            int h,
            ushort hue
        ) : base(w, h, hue)
        {
            _world = world;
            X = x;
            Y = y;
            WantUpdateSize = false;

            GumpPic background = new GumpPic(0, 0, 0x00D4, 0);
            Add(background);

            Width = background.Width;
            Height = background.Height;
        }

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            if (Children.Count != 0)
            {
                layerDepthRef += 0.1f;
                Children[0].AddToRenderLists(renderLists, x, y, ref layerDepthRef);
            }
            float layerDepth = layerDepthRef;

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(Hue);

            renderLists.AddGumpNoAtlas(
                batcher =>
                {
                    batcher.Draw
                    (
                        SolidColorTextureCache.GetTexture(Color.White),
                        new Rectangle
                        (
                            x + 3,
                            y + 3,
                            Width - 6,
                            Height - 6
                        ),
                        hueVector,
                        layerDepth
                    );
                    return true;
                }
            );

            return true;
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                UIManager.GetGump<ColorPickerGump>()?.Dispose();

                ColorPickerGump pickerGump = new ColorPickerGump
                (
                    _world,
                    0,
                    0,
                    100,
                    100,
                    s => Hue = s
                );

                UIManager.Add(pickerGump);
            }
        }
    }
}