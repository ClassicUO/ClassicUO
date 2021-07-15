#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Runtime.CompilerServices;
using ClassicUO.Configuration;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.GameObjects
{
    internal abstract partial class GameObject
    {
        public static bool DrawTransparent;

        protected static readonly Lazy<DepthStencilState> StaticTransparentStencil = new Lazy<DepthStencilState>
        (
            () =>
            {
                DepthStencilState state = new DepthStencilState
                {
                    StencilEnable = true,
                    StencilFunction = CompareFunction.GreaterEqual,
                    StencilPass = StencilOperation.Keep,
                    ReferenceStencil = 0
                    //DepthBufferEnable = true,
                    //DepthBufferWriteEnable = true,
                };


                return state;
            }
        );
        public bool UseObjectHandles { get; set; }
        public bool ClosedObjectHandles { get; set; }
        public bool ObjectHandlesOpened { get; set; }
        public byte AlphaHue { get; set; }
        public bool AllowedToDraw { get; set; } = true;


        public Rectangle FrameInfo;
        protected bool IsFlipped;


        public abstract bool Draw(UltimaBatcher2D batcher, int posX, int posY, ref Vector3 hue);



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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ProcessAlpha(int max)
        {
            if (ProfileManager.CurrentProfile != null && !ProfileManager.CurrentProfile.UseObjectsFading)
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
                {
                    alpha = max;
                }

                result = true;
            }
            else if (alpha < max)
            {
                alpha += 25;

                if (alpha > max)
                {
                    alpha = max;
                }

                result = true;
            }

            AlphaHue = (byte) alpha;

            return result;
        }


        protected static void DrawLand(UltimaBatcher2D batcher, ushort graphic, int x, int y, ref Vector3 hue)
        {
            UOTexture texture = ArtLoader.Instance.GetLandTexture(graphic);

            if (texture != null)
            {
                texture.Ticks = Time.Ticks;

                batcher.DrawSprite
                (
                    texture,
                    x,
                    y,
                    false,
                    ref hue
                );
            }
        }

        protected static void DrawLand
        (
            UltimaBatcher2D batcher,
            ushort graphic,
            int x,
            int y,
            ref UltimaBatcher2D.YOffsets yOffsets,
            ref Vector3 nTop,
            ref Vector3 nRight,
            ref Vector3 nLeft,
            ref Vector3 nBottom,
            ref Vector3 hue
        )
        {
            UOTexture texture = TexmapsLoader.Instance.GetTexture(TileDataLoader.Instance.LandData[graphic].TexID);

            if (texture != null)
            {
                texture.Ticks = Time.Ticks;

                batcher.DrawSpriteLand
                (
                    texture,
                    x,
                    y,
                    ref yOffsets,
                    ref nTop,
                    ref nRight,
                    ref nLeft,
                    ref nBottom,
                    ref hue
                );
            }
            else
            {
                DrawStatic
                (
                    batcher,
                    graphic,
                    x,
                    y,
                    ref hue
                );
            }
        }

        protected static void DrawStatic(UltimaBatcher2D batcher, ushort graphic, int x, int y, ref Vector3 hue)
        {
            ArtTexture texture = ArtLoader.Instance.GetTexture(graphic);

            if (texture != null)
            {
                texture.Ticks = Time.Ticks;
                ref UOFileIndex index = ref ArtLoader.Instance.GetValidRefEntry(graphic + 0x4000);

                batcher.DrawSprite
                (
                    texture,
                    x - index.Width,
                    y - index.Height,
                    false,
                    ref hue
                );
            }
        }

        protected static void DrawGump(UltimaBatcher2D batcher, ushort graphic, int x, int y, ref Vector3 hue)
        {
            UOTexture texture = GumpsLoader.Instance.GetTexture(graphic);

            if (texture != null)
            {
                texture.Ticks = Time.Ticks;

                batcher.DrawSprite
                (
                    texture,
                    x,
                    y,
                    false,
                    ref hue
                );
            }
        }

        protected static void DrawStaticRotated
        (
            UltimaBatcher2D batcher,
            ushort graphic,
            int x,
            int y,
            float angle,
            ref Vector3 hue
        )
        {
            ArtTexture texture = ArtLoader.Instance.GetTexture(graphic);

            if (texture != null)
            {
                texture.Ticks = Time.Ticks;

                ref UOFileIndex index = ref ArtLoader.Instance.GetValidRefEntry(graphic + 0x4000);

                batcher.DrawSpriteRotated
                (
                    texture,
                    x - index.Width,
                    y - index.Height,
                    texture.Width,
                    texture.Height,
                    ref hue,
                    angle
                );
            }
        }

        protected static void DrawStaticAnimated
        (
            UltimaBatcher2D batcher,
            ushort graphic,
            int x,
            int y,
            ref Vector3 hue,
            ref bool transparent,
            bool shadow
        )
        {
            ref UOFileIndex index = ref ArtLoader.Instance.GetValidRefEntry(graphic + 0x4000);

            graphic = (ushort) (graphic + index.AnimOffset);

            ArtTexture texture = ArtLoader.Instance.GetTexture(graphic);

            if (texture != null)
            {
                texture.Ticks = Time.Ticks;
                index = ref ArtLoader.Instance.GetValidRefEntry(graphic + 0x4000);

                if (transparent)
                {
                    int maxDist = ProfileManager.CurrentProfile.CircleOfTransparencyRadius;

                    int fx = (int) (World.Player.RealScreenPosition.X + World.Player.Offset.X);
                    int fy = (int) (World.Player.RealScreenPosition.Y + (World.Player.Offset.Y - World.Player.Offset.Z));

                    fx -= x;
                    fy -= y;

                    float dist = (float) Math.Floor(Math.Sqrt(fx * fx + fy * fy));

                    if (dist <= maxDist)
                    {
                        float alpha = hue.Z;

                        switch (ProfileManager.CurrentProfile.CircleOfTransparencyType)
                        {
                            default:
                            case 0:
                                hue.Z = 0.75f;

                                break;

                            case 1:

                                float delta = (maxDist - 44) * 0.5f;
                                float fraction = (dist - delta) / (maxDist - delta);

                                hue.Z = MathHelper.Lerp(1f, 0f, fraction);

                                break;
                        }

                        x -= index.Width;
                        y -= index.Height;


                        batcher.DrawSprite
                        (
                            texture,
                            x,
                            y,
                            false,
                            ref hue
                        );

                        batcher.SetStencil(StaticTransparentStencil.Value);
                        hue.Z = alpha;

                        batcher.DrawSprite
                        (
                            texture,
                            x,
                            y,
                            false,
                            ref hue
                        );

                        batcher.SetStencil(null);

                        return;
                    }
                }

                transparent = false;
                x -= index.Width;
                y -= index.Height;


                if (shadow)
                {
                    batcher.DrawSpriteShadow(texture, x, y, false);
                }

                batcher.DrawSprite
                (
                    texture,
                    x,
                    y,
                    false,
                    ref hue
                );
            }
        }
    }
}