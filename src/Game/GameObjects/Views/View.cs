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
        protected static Vector3 HueVector;
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


        public abstract bool Draw(UltimaBatcher2D batcher, int posX, int posY);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

                batcher.DrawSprite(texture, x, y, false, ref hue);
            }
        }
        // ## BEGIN - END ## //
        protected static void DrawLandWF(UltimaBatcher2D batcher, ushort graphic, int x, int y, ref Vector3 hue, bool isImpassable)
        {
            UOTexture texture = ArtLoader.Instance.GetLandTextureWF(graphic, isImpassable);

            if (texture != null)
            {
                texture.Ticks = Time.Ticks;

                batcher.DrawSprite(texture, x, y, false, ref hue);
            }
        }
        protected static void DrawLandWF
        (
            UltimaBatcher2D batcher,
            ushort graphic,
            int x,
            int y,
            ref Rectangle rectangle,
            ref Vector3 n0,
            ref Vector3 n1,
            ref Vector3 n2,
            ref Vector3 n3,
            ref Vector3 hue,
            bool isImpassable
        )
        {
            UOTexture texture = TexmapsLoader.Instance.GetTextureWF(TileDataLoader.Instance.LandData[graphic].TexID, isImpassable);

            if (texture != null)
            {
                texture.Ticks = Time.Ticks;

                batcher.DrawSpriteLand(texture, x, y, ref rectangle, ref n0, ref n1, ref n2, ref n3, ref hue);
            }
            else
            {
                DrawStatic(batcher, graphic, x, y, ref hue);
            }
        }
        // ## BEGIN - END ## //

        protected static void DrawLand
        (
            UltimaBatcher2D batcher,
            ushort graphic,
            int x,
            int y,
            ref Rectangle rectangle,
            ref Vector3 n0,
            ref Vector3 n1,
            ref Vector3 n2,
            ref Vector3 n3,
            ref Vector3 hue
        )
        {
            UOTexture texture = TexmapsLoader.Instance.GetTexture(TileDataLoader.Instance.LandData[graphic].TexID);

            if (texture != null)
            {
                texture.Ticks = Time.Ticks;

                batcher.DrawSpriteLand(texture, x, y, ref rectangle, ref n0, ref n1, ref n2, ref n3, ref hue);
            }
            else
            {
                DrawStatic(batcher, graphic, x, y, ref hue);
            }
        }

        protected static void DrawStatic(UltimaBatcher2D batcher, ushort graphic, int x, int y, ref Vector3 hue)
        {
            ArtTexture texture = ArtLoader.Instance.GetTexture(graphic);

            if (texture != null)
            {
                texture.Ticks = Time.Ticks;
                ref UOFileIndex index = ref ArtLoader.Instance.GetValidRefEntry(graphic + 0x4000);

                batcher.DrawSprite(texture, x - index.Width, y - index.Height, false, ref hue);
            }
        }

        protected static void DrawGump(UltimaBatcher2D batcher, ushort graphic, int x, int y, ref Vector3 hue)
        {
            UOTexture texture = GumpsLoader.Instance.GetTexture(graphic);

            if (texture != null)
            {
                texture.Ticks = Time.Ticks;

                batcher.DrawSprite(texture, x, y, false, ref hue);
            }
        }

        protected static void DrawStaticRotated
        (
            UltimaBatcher2D batcher,
            ushort graphic,
            int x,
            int y,
            int destX,
            int destY,
            float angle,
            ref Vector3 hue
        )
        {
            ArtTexture texture = ArtLoader.Instance.GetTexture(graphic);

            if (texture != null)
            {
                texture.Ticks = Time.Ticks;

                ref UOFileIndex index = ref ArtLoader.Instance.GetValidRefEntry(graphic + 0x4000);

                batcher.DrawSpriteRotated(texture, x - index.Width, y - index.Height, destX, destY, ref hue, angle);
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
                    int maxDist = ProfileManager.CurrentProfile.CircleOfTransparencyRadius + 22;
                    int fx = (int) (World.Player.RealScreenPosition.X + World.Player.Offset.X);

                    int fy =
                        (int) (World.Player.RealScreenPosition.Y + (World.Player.Offset.Y - World.Player.Offset.Z)) +
                        44;

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
                                hue.Z = MathHelper.Lerp(1f, 0f, (dist - 44) / maxDist);

                                break;
                        }

                        x -= index.Width;
                        y -= index.Height;


                        batcher.DrawSprite(texture, x, y, false, ref hue);
                        batcher.SetStencil(StaticTransparentStencil.Value);
                        hue.Z = alpha;
                        batcher.DrawSprite(texture, x, y, false, ref hue);
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

                batcher.DrawSprite(texture, x, y, false, ref hue);
            }
        }
    }
}