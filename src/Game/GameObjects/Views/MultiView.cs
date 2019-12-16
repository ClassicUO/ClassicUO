#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;
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

            if (ItemData.IsAnimated && _lastAnimationFrameTime < Time.Ticks)
            {
                IntPtr ptr = UOFileManager.AnimData.GetAddressToAnim(Graphic);

                if (ptr != IntPtr.Zero)
                {
                    unsafe
                    {
                        AnimDataFrame2* animData = (AnimDataFrame2*)ptr;

                        if (animData->FrameCount != 0)
                        {
                            graphic = (ushort) (Graphic + animData->FrameData[AnimIndex++]);

                            if (AnimIndex >= animData->FrameCount)
                                AnimIndex = 0;

                            _lastAnimationFrameTime = Time.Ticks + (uint)(animData->FrameInterval != 0 ?
                                                          animData->FrameInterval * Constants.ITEM_EFFECT_ANIMATION_DELAY + 25 : Constants.ITEM_EFFECT_ANIMATION_DELAY);
                        }
                    }
                }
            }


            if (Texture == null || Texture.IsDisposed || Graphic != graphic)
            {
                ArtTexture texture = UOFileManager.Art.GetTexture(graphic);
                Texture = texture;
                Bounds = new Rectangle((Texture.Width >> 1) - 22, Texture.Height - 44, Texture.Width, Texture.Height);

                FrameInfo.Width = texture.ImageRectangle.Width;
                FrameInfo.Height = texture.ImageRectangle.Height;

                FrameInfo.X = (Texture.Width >> 1) - 22 - texture.ImageRectangle.X;
                FrameInfo.Y = Texture.Height - 44 - texture.ImageRectangle.Y;
            }

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

            base.Draw(batcher, posX, posY);

            if (ItemData.IsLight)
            {
                CUOEnviroment.Client.GetScene<GameScene>()
                      .AddLight(this, this, posX + 22, posY + 22);
            }

            return true;
        }

        public override void Select(int x, int y)
        {
            if (SelectedObject.Object == this || IsFromTarget)
                return;

            if (State != 0)
            {
                if ((State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_IGNORE_IN_RENDER) != 0)
                    return;
            }

            if (DrawTransparent)
            {
                int d = Distance;
                int maxD = ProfileManager.Current.CircleOfTransparencyRadius + 1;

                if (d <= maxD && d <= 3)
                    return;
            }

            if (SelectedObject.IsPointInStatic(Texture, x - Bounds.X, y - Bounds.Y))
                SelectedObject.Object = this;
        }
    }
}