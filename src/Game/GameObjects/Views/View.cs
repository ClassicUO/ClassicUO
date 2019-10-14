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
using System.Runtime.CompilerServices;

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

        protected float Rotation { get; set; }

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
                DepthBufferEnable = false,
            };


            return state;
        });

        public virtual bool Draw(UltimaBatcher2D batcher, int posX, int posY)
        {
            if (DrawTransparent)
            {
                int dist = Distance;
                int maxDist = Engine.Profile.Current.CircleOfTransparencyRadius + 1;

                if (dist <= maxDist)
                {
                    HueVector.Z = MathHelper.Lerp(1f, 1f - dist / (float)maxDist, 0.5f);
                    //HueVector.Z = 1f - (dist / (float)maxDist);
                }
                else
                    HueVector.Z = 1f - AlphaHue / 255f;
            }
            else if (AlphaHue != 255)
                HueVector.Z = 1f - AlphaHue / 255f;
            

            if (Rotation != 0.0f)
            {
                if (!batcher.DrawSpriteRotated(Texture, posX, posY, Bounds.Width, Bounds.Height, Bounds.X, Bounds.Y, ref HueVector, Rotation))
                    return false;
            }
            else if (IsFlipped)
            {
                if (!batcher.DrawSpriteFlipped(Texture, posX, posY, Bounds.Width, Bounds.Height, Bounds.X, Bounds.Y, ref HueVector))
                    return false;
            }
            else
            {
                //if (DrawTransparent)
                //{
                //    int dist = Distance;
                //    int maxDist = Engine.Profile.Current.CircleOfTransparencyRadius + 1;

                //    if (dist <= maxDist)
                //    {
                //        HueVector.Z = 0.75f; // MathHelper.Lerp(1f, 1f - dist / (float)maxDist, 0.5f);
                //        //HueVector.Z = 1f - (dist / (float)maxDist);
                //    }
                //    else
                //        HueVector.Z = 1f - AlphaHue / 255f;

                //    batcher.DrawSprite(Texture, posX, posY, Bounds.Width, Bounds.Height, Bounds.X, Bounds.Y, ref HueVector);

                //    HueVector.Z = 0;

                //    batcher.SetStencil(_stencil.Value);
                //    batcher.DrawSprite(Texture, posX, posY, Bounds.Width, Bounds.Height, Bounds.X, Bounds.Y, ref HueVector);
                //    batcher.SetStencil(null);
                //}
                //else
                {
                    if (!batcher.DrawSprite(Texture, posX, posY, Bounds.Width, Bounds.Height, Bounds.X, Bounds.Y, ref HueVector))
                        return false;
                }
            }


            Select(posX, posY);

            Texture.Ticks = Engine.Ticks;

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