// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Scenes;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

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

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(Graphic);

            if (gumpInfo.Texture != null)
            {
                Vector3 hueVector = ShaderHueTranslator.GetHueVector(Hue);

                renderLists.AddGumpSpriteTiled(
                    gumpInfo.Texture,
                    gumpInfo.UV,
                    new Rectangle(x, y, Percent, Height),
                    hueVector,
                    layerDepthRef
                );

                return true;
            }

            return false;
        }
    }
}
