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
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using ClassicUO.Configuration;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using IDrawable = ClassicUO.Interfaces.IDrawable;

namespace ClassicUO.Game.GameObjects
{
    internal abstract partial class GameObject : IDrawable
    {
        protected static Vector3 HueVector;
        public Rectangle Bounds;
        public Rectangle FrameInfo;

        protected bool IsFlipped { get; set; }

        public bool UseObjectHandles { get; set; }

        public bool ClosedObjectHandles { get; set; }

        public bool ObjectHandlesOpened { get; set; }

        public byte AlphaHue { get; set; }

        public bool DrawTransparent { get; set; }

        public bool AllowedToDraw { get; set; } = true;

        public UOTexture Texture { get; set; }

        private static readonly Lazy<DepthStencilState> _stencil = new Lazy<DepthStencilState>(() =>
        {
            DepthStencilState state = new DepthStencilState
            {
                StencilEnable = true,
                StencilFunction = CompareFunction.GreaterEqual,
                StencilPass = StencilOperation.Keep,
                ReferenceStencil = 0,
                //DepthBufferEnable = true,
                //DepthBufferWriteEnable = true,
            };


            return state;
        });


        public virtual bool Draw(UltimaBatcher2D batcher, int posX, int posY)
        {
            //if (DrawTransparent)
            //{
            //    int x = RealScreenPosition.X;
            //    int y = RealScreenPosition.Y;
            //    int fx = (int) (World.Player.RealScreenPosition.X + World.Player.Offset.X);
            //    int fy = (int) (World.Player.RealScreenPosition.Y + (World.Player.Offset.Y - World.Player.Offset.Z));

            //    int dist = Math.Max(Math.Abs(x - fx), Math.Abs(y - fy));
            //    int maxDist = ProfileManager.Current.CircleOfTransparencyRadius;

            //    if (dist <= maxDist)
            //    {
            //        HueVector.Z = MathHelper.Lerp(1f, 1f - dist / (float)maxDist, 0.5f);
            //        //HueVector.Z = 1f - (dist / (float)maxDist);
            //    }
            //    else
            //        HueVector.Z = 1f - AlphaHue / 255f;
            //}
            //else if (AlphaHue != 255)
            //    HueVector.Z = 1f - AlphaHue / 255f;

            //if (!batcher.DrawSprite(Texture, posX - Bounds.X, posY - Bounds.Y, IsFlipped, ref HueVector))
            //    return false;



            if (DrawTransparent)
            {
                int maxDist = ProfileManager.Current.CircleOfTransparencyRadius + 44;

                int fx = (int) (World.Player.RealScreenPosition.X + World.Player.Offset.X);
                int fy = (int) (World.Player.RealScreenPosition.Y + (World.Player.Offset.Y - World.Player.Offset.Z));

                fx -= posX;
                fy -= posY;
                int dist = (int) Math.Sqrt(fx * fx + fy * fy);

                //dist = Math.Max(Math.Abs(fx - x), Math.Abs(fy - y));

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

                    batcher.DrawSprite(Texture, posX - Bounds.X, posY - Bounds.Y, IsFlipped, ref HueVector);

                    if (AlphaHue != 255)
                        HueVector.Z = 1f - AlphaHue / 255f;
                    else
                        HueVector.Z = 0;

                    batcher.SetStencil(_stencil.Value);
                    batcher.DrawSprite(Texture, posX - Bounds.X, posY - Bounds.Y, IsFlipped, ref HueVector);
                    batcher.SetStencil(null);
                    goto COT;
                }
            }

            if (AlphaHue != 255)
                HueVector.Z = 1f - AlphaHue / 255f;

            if (!batcher.DrawSprite(Texture, posX - Bounds.X, posY - Bounds.Y, IsFlipped, ref HueVector))
                return false;

            COT:

            Select(posX, posY);

            Texture.Ticks = Time.Ticks;

            return true;
        }

        [MethodImpl(256)]
        protected static void ResetHueVector()
        {
            HueVector.X = 0;
            HueVector.Y = 0;
            HueVector.Z = 0;
        }

        public Rectangle GetOnScreenRectangle()
        {
            Rectangle prect = Rectangle.Empty;

            prect.X = (int) (RealScreenPosition.X - FrameInfo.X + 22 + Offset.X);
            prect.Y = (int) (RealScreenPosition.Y - FrameInfo.Y + 22 + (Offset.Y - Offset.Z));
            prect.Width = FrameInfo.Width;
            prect.Height = FrameInfo.Height;

            return prect;
        }

        public virtual bool TransparentTest(int z)
        {
            return false;
        }

        [MethodImpl(256)]
        public bool ProcessAlpha(int max)
        {
            if (ProfileManager.Current != null && !ProfileManager.Current.UseObjectsFading)
            {
                AlphaHue = (byte) max;

                return max != 0;
            }

            bool result = false;

            int alpha = AlphaHue;

            if (alpha > max)
            {
                alpha -= 25;

                if (alpha < max)
                    alpha = max;

                result = true;
            }
            else if (alpha < max)
            {
                alpha += 25;

                if (alpha > max)
                    alpha = max;

                result = true;
            }

            AlphaHue = (byte) alpha;

            return result;
        }


        public virtual void Select(int x, int y)
        {
        }
    }
}