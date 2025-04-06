// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Sdk.Assets;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using ClassicUO.Services;

namespace ClassicUO.Game.UI.Controls
{
    internal class GumpPicWithWidth : GumpPic
    {
        public GumpPicWithWidth(int x, int y, ushort graphic, ushort hue, int perc)
            : base(x, y, graphic, hue)
        {
            Percent = perc;
            CanMove = true;
            //AcceptMouseInput = false;
        }

        public int Percent { get; set; }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(Hue);

            ref readonly var gumpInfo = ref ServiceProvider.Get<UOService>().Gumps.GetGump(Graphic);

            if (gumpInfo.Texture != null)
            {
                batcher.DrawTiled(
                    gumpInfo.Texture,
                    new Rectangle(x, y, Percent, Height),
                    gumpInfo.UV,
                    hueVector
                );

                return true;
            }

            return false;
        }
    }
}
