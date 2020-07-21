#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion

using System;

using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    internal partial class Multi
    {
        private int _canBeTransparent;

        public bool IsFromTarget;

        public override bool TransparentTest(int z)
        {
            bool r = true;

            if (Z <= z - ItemData.Height)
                r = false;
            else if (z < Z && (_canBeTransparent & 0xFF) == 0)
                r = false;

            return r;
        }

        public override bool Draw(UltimaBatcher2D batcher, int posX, int posY)
        {
            if (!AllowedToDraw || IsDestroyed)
                return false;

            ResetHueVector();

            ushort hue = Hue;
            float alpha = 0;
            if (State != 0)
            {
                if ((State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_IGNORE_IN_RENDER) != 0)
                    return false;

                if ((State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) != 0)
                {
                    hue = 0x002B;
                }

                if ((State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_TRANSPARENT) != 0)
                {
                    if (AlphaHue >= 192)
                    {
                        alpha = 0.25f;
                        AlphaHue = 0xFF;
                    }
                    else
                        ProcessAlpha(192);
                }
            }

            ushort graphic = Graphic;

            if (ProfileManager.Current.HighlightGameObjects && SelectedObject.LastObject == this)
            {
                HueVector.X = 0x0023;
                HueVector.Y = 1;
            }
            else if (ProfileManager.Current.NoColorObjectsOutOfRange && Distance > World.ClientViewRange)
            {
                HueVector.X = Constants.OUT_RANGE_COLOR;
                HueVector.Y = 1;
            }
            else if (World.Player.IsDead && ProfileManager.Current.EnableBlackWhiteEffect)
            {
                HueVector.X = Constants.DEAD_RANGE_COLOR;
                HueVector.Y = 1;
            }
            else
                ShaderHuesTraslator.GetHueVector(ref HueVector, hue, ItemData.IsPartialHue, alpha);

            //Engine.DebugInfo.MultiRendered++;

            if (IsFromTarget)
                HueVector.Z = 0.5f;

            posX += (int) Offset.X;
            posY += (int) (Offset.Y + Offset.Z);

            if (DrawTransparent)
            {
                int maxDist = ProfileManager.Current.CircleOfTransparencyRadius + 44;
                int fx = (int) (World.Player.RealScreenPosition.X + World.Player.Offset.X);
                int fy = (int) (World.Player.RealScreenPosition.Y + (World.Player.Offset.Y - World.Player.Offset.Z));

                fx -= posX;
                fy -= posY;

                int dist = (int) Math.Sqrt(fx * fx + fy * fy);

                if (dist <= maxDist)
                {
                    switch (ProfileManager.Current.CircleOfTransparencyType)
                    {
                        default:
                        case 0:
                            HueVector.Z = 0.75f;
                            break;
                        case 1:
                            HueVector.Z = MathHelper.Lerp(1f, 0f, (dist / (float) maxDist));
                            break;
                    }

                    DrawStaticAnimated(batcher, graphic, posX, posY, ref HueVector);

                    if (AlphaHue != 255)
                        HueVector.Z = 1f - AlphaHue / 255f;
                    else
                        HueVector.Z = 0;

                    batcher.SetStencil(StaticTransparentStencil.Value);
                    DrawStaticAnimated(batcher, graphic, posX, posY, ref HueVector);
                    batcher.SetStencil(null);
                }
                else
                {
                    if (AlphaHue != 255)
                        HueVector.Z = 1f - AlphaHue / 255f;

                    DrawStaticAnimated(batcher, graphic, posX, posY, ref HueVector);
                }
            }
            else
            {
                if (AlphaHue != 255)
                    HueVector.Z = 1f - AlphaHue / 255f;

                DrawStaticAnimated(batcher, graphic, posX, posY, ref HueVector);
            }

            if (ItemData.IsLight)
            {
                Client.Game.GetScene<GameScene>()
                      .AddLight(this, this, posX + 22, posY + 22);
            }

            if (!(SelectedObject.Object == this || IsFromTarget ||
                  (FoliageIndex != -1 &&
                   Client.Game.GetScene<GameScene>().FoliageIndex == FoliageIndex)))
            {
                if (State != 0)
                {
                    if ((State & (CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_IGNORE_IN_RENDER |
                                  CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_PREVIEW)) != 0)
                        return true;
                }
                 
                if (DrawTransparent)
                {
                    return true;
                }

                ref var index = ref ArtLoader.Instance.GetValidRefEntry(graphic + 0x4000);

                posX -= index.Width;
                posY -= index.Height;

                if (SelectedObject.IsPointInStatic(ArtLoader.Instance.GetTexture(graphic), posX, posY))
                    SelectedObject.Object = this;
            }

            return true;
        }
    }
}