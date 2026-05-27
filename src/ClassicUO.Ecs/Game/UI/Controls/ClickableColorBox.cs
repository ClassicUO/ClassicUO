// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Assets;
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

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (Children.Count != 0)
            {
                Children[0].Draw(batcher, x, y);
            }

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(Hue);

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
                hueVector
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