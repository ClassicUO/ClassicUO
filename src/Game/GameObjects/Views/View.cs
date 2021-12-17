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
    enum ObjectHandlesStatus
    {
        NONE,
        OPEN,
        CLOSED,
        DISPLAYING
    }

    internal abstract partial class GameObject
    {
        public byte AlphaHue;
        public bool AllowedToDraw = true;
        public ObjectHandlesStatus ObjectHandlesStatus;
        public Rectangle FrameInfo;
        protected bool IsFlipped;

        public abstract bool Draw(UltimaBatcher2D batcher, int posX, int posY, float depth);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float CalculateDepthZ()
        {
            int x = X;
            int y = Y;
            int z = PriorityZ;

            // Offsets are in SCREEN coordinates
            if (Offset.X > 0 && Offset.Y < 0)
            {
                // North
            }
            else if (Offset.X > 0 && Offset.Y == 0)
            {
                // Northeast
                x++;
            }
            else if (Offset.X > 0 && Offset.Y > 0)
            {
                // East
                z += Math.Max(0, (int)Offset.Z);
                x++;
            }
            else if (Offset.X == 0 && Offset.Y > 0)
            {
                // Southeast
                x++;
                y++;
            }
            else if (Offset.X < 0 && Offset.Y > 0)
            {
                // South
                z += Math.Max(0, (int)Offset.Z);
                y++;
            }
            else if (Offset.X < 0 && Offset.Y == 0)
            {
                // Southwest
                y++;
            }
            else if (Offset.X < 0 && Offset.Y > 0)
            {
                // West
            }
            else if (Offset.X == 0 && Offset.Y < 0)
            {
                // Northwest
            }

           return (x + y) + (127 + z) * 0.01f;
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

        protected static void DrawLand(UltimaBatcher2D batcher, ushort graphic, int x, int y, Vector3 hue, float depth)
        {
            var texture = ArtLoader.Instance.GetLandTexture(graphic, out var bounds);

            if (texture != null)
            {
                batcher.Draw
                (
                    texture,
                    new Vector2(x, y), 
                    bounds,
                    hue,
                    0f,
                    Vector2.Zero,
                    1f, 
                    SpriteEffects.None,
                    depth + 0.5f
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
            Vector3 hue,
            float depth 
        )
        {
            var texture = TexmapsLoader.Instance.GetLandTexture(TileDataLoader.Instance.LandData[graphic].TexID, out var bounds);

            if (texture != null)
            {
                batcher.DrawStretchedLand
                (
                    texture,
                    new Vector2(x, y),
                    bounds,
                    ref yOffsets,
                    ref nTop,
                    ref nRight,
                    ref nLeft,
                    ref nBottom,
                    hue,
                    depth + 0.5f
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
                    hue,
                    depth
                );
            }
        }

        protected static void DrawStatic(UltimaBatcher2D batcher, ushort graphic, int x, int y, Vector3 hue, float depth)
        {
            var texture = ArtLoader.Instance.GetStaticTexture(graphic, out var bounds);

            if (texture != null)
            {
                ref UOFileIndex index = ref ArtLoader.Instance.GetValidRefEntry(graphic + 0x4000);

                x -= index.Width;
                y -= index.Height;

                batcher.Draw
                (
                    texture,
                    new Vector2(x, y),
                    bounds,
                    hue,
                    0f,
                    Vector2.Zero,
                    1f,
                    SpriteEffects.None, 
                    depth + 0.5f
                );
            }
        }

        protected static void DrawGump(UltimaBatcher2D batcher, ushort graphic, int x, int y, Vector3 hue, float depth)
        {
            var texture = GumpsLoader.Instance.GetGumpTexture(graphic, out var bounds);

            if (texture != null)
            {
                batcher.Draw
                (
                    texture,
                    new Vector2(x, y),
                    bounds,
                    hue,
                    0f,
                    Vector2.Zero,
                    1f,
                    SpriteEffects.None,
                    depth + 0.5f
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
            Vector3 hue,
            float depth
        )
        {
            var texture = ArtLoader.Instance.GetStaticTexture(graphic, out var bounds);

            if (texture != null)
            {
                ref UOFileIndex index = ref ArtLoader.Instance.GetValidRefEntry(graphic + 0x4000);

                batcher.Draw
                (
                    texture,
                    new Rectangle
                    (
                        x - index.Width,
                        y - index.Height, 
                        bounds.Width,
                        bounds.Height
                    ),
                    bounds,
                    hue,
                    MathHelper.ToRadians(angle),
                    Vector2.Zero,
                    SpriteEffects.None,
                    depth + 0.5f
                );
            }
        }

        protected static void DrawStaticAnimated
        (
            UltimaBatcher2D batcher,
            ushort graphic,
            int x,
            int y,
            Vector3 hue,
            bool shadow,
            float depth
        )
        {
            ref UOFileIndex index = ref ArtLoader.Instance.GetValidRefEntry(graphic + 0x4000);

            graphic = (ushort) (graphic + index.AnimOffset);

            var texture = ArtLoader.Instance.GetStaticTexture(graphic, out var bounds);

            if (texture != null)
            {
                index = ref ArtLoader.Instance.GetValidRefEntry(graphic + 0x4000);
              
                x -= index.Width;
                y -= index.Height;

                Vector2 pos = new Vector2(x, y);

                if (shadow)
                {
                    batcher.DrawShadow(texture, pos, bounds, false, depth + 0.25f);
                }

                batcher.Draw
                (
                    texture,
                    pos,
                    bounds,
                    hue,
                    0f,
                    Vector2.Zero,
                    1f,
                    SpriteEffects.None,
                    depth + 0.5f
                );
            }
        }
    }
}