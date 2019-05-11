using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer
{
    internal class UltimaBatcher2D : Batcher2D<PositionNormalTextureColor>
    {
        public UltimaBatcher2D(GraphicsDevice device) : base(device, new IsometricEffect(device))
        {
        }


        public void DrawString(SpriteFont spriteFont, string text, int x, int y, Vector3 color)
        {
            if (text == null) throw new ArgumentNullException("text");

            if (text.Length == 0) return;

            EnsureSize();

            Texture2D textureValue = spriteFont.Texture;
            List<Rectangle> glyphData = spriteFont.GlyphData;
            List<Rectangle> croppingData = spriteFont.CroppingData;
            List<Vector3> kerning = spriteFont.Kerning;
            List<char> characterMap = spriteFont.CharacterMap;

            Vector2 curOffset = Vector2.Zero;
            bool firstInLine = true;

            Vector2 baseOffset = Vector2.Zero;
            float axisDirX = 1;
            float axisDirY = 1;

            foreach (char c in text)
            {
                // Special characters
                if (c == '\r') continue;

                if (c == '\n')
                {
                    curOffset.X = 0.0f;
                    curOffset.Y += spriteFont.LineSpacing;
                    firstInLine = true;

                    continue;
                }

                /* Get the List index from the character map, defaulting to the
				 * DefaultCharacter if it's set.
				 */
                int index = characterMap.IndexOf(c);

                if (index == -1)
                {
                    if (!spriteFont.DefaultCharacter.HasValue)
                    {
                        throw new ArgumentException(
                                                    "Text contains characters that cannot be" +
                                                    " resolved by this SpriteFont.",
                                                    "text"
                                                   );
                    }

                    index = characterMap.IndexOf(
                                                 spriteFont.DefaultCharacter.Value
                                                );
                }

                /* For the first character in a line, always push the width
				 * rightward, even if the kerning pushes the character to the
				 * left.
				 */
                Vector3 cKern = kerning[index];

                if (firstInLine)
                {
                    curOffset.X += Math.Abs(cKern.X);
                    firstInLine = false;
                }
                else
                    curOffset.X += spriteFont.Spacing + cKern.X;

                // Calculate the character origin
                Rectangle cCrop = croppingData[index];
                Rectangle cGlyph = glyphData[index];

                float offsetX = baseOffset.X + (
                                                   curOffset.X + cCrop.X
                                               ) * axisDirX;

                float offsetY = baseOffset.Y + (
                                                   curOffset.Y + cCrop.Y
                                               ) * axisDirY;


                Draw2D(textureValue,
                       x + (int) offsetX, y + (int) offsetY,
                       cGlyph.X, cGlyph.Y, cGlyph.Width, cGlyph.Height,
                       color);

                curOffset.X += cKern.Y + cKern.Z;
            }
        }

        public bool DrawSprite(Texture2D texture, int x, int y, int w, int h, int destX, int destY, Vector3 hue)
        {
            EnsureSize();

            int idx = NumSprites << 2;
            ref var vertex0 = ref VertexInfo[idx];
            ref var vertex1 = ref VertexInfo[idx + 1];
            ref var vertex2 = ref VertexInfo[idx + 2];
            ref var vertex3 = ref VertexInfo[idx + 3];


            vertex0.Position.X = x - destX;
            vertex0.Position.Y = y - destY;
            vertex0.Position.Z = 0;
            vertex0.Normal.X = 0;
            vertex0.Normal.Y = 0;
            vertex0.Normal.Z = 1;
            vertex0.TextureCoordinate = Vector3.Zero;

            vertex1.Position = vertex0.Position;
            vertex1.Position.X += w;
            vertex1.Normal.X = 0;
            vertex1.Normal.Y = 0;
            vertex1.Normal.Z = 1;
            vertex1.TextureCoordinate.X = 1;
            vertex1.TextureCoordinate.Y = 0;
            vertex1.TextureCoordinate.Z = 0;

            vertex2.Position = vertex0.Position;
            vertex2.Position.Y += h;
            vertex2.Normal.X = 0;
            vertex2.Normal.Y = 0;
            vertex2.Normal.Z = 1;
            vertex2.TextureCoordinate.X = 0;
            vertex2.TextureCoordinate.Y = 1;
            vertex2.TextureCoordinate.Z = 0;

            vertex3.Position = vertex1.Position;
            vertex3.Position.Y += h;
            vertex3.Normal.X = 0;
            vertex3.Normal.Y = 0;
            vertex3.Normal.Z = 1;
            vertex3.TextureCoordinate.X = 1;
            vertex3.TextureCoordinate.Y = 1;
            vertex3.TextureCoordinate.Z = 0;


            vertex0.Hue =
                vertex1.Hue =
                    vertex2.Hue =
                        vertex3.Hue = hue;

            if (CheckInScreen(idx))
            {
                PushSprite(texture);

                return true;
            }

            return false;
        }

        public bool DrawSpriteFlipped(Texture2D texture, int x, int y, int w, int h, int destX, int destY, Vector3 hue)
        {
            EnsureSize();

            int idx = NumSprites << 2;
            ref var vertex0 = ref VertexInfo[idx];
            ref var vertex1 = ref VertexInfo[idx + 1];
            ref var vertex2 = ref VertexInfo[idx + 2];
            ref var vertex3 = ref VertexInfo[idx + 3];


            vertex0.Position.X = x + destX + 44;
            vertex0.Position.Y = y - destY;
            vertex0.Position.Z = 0;
            vertex0.Normal.X = 0;
            vertex0.Normal.Y = 0;
            vertex0.Normal.Z = 1;
            vertex0.TextureCoordinate = Vector3.Zero;

            vertex1.Position = vertex0.Position;
            vertex1.Position.Y += h;
            vertex1.Normal.X = 0;
            vertex1.Normal.Y = 0;
            vertex1.Normal.Z = 1;
            vertex1.TextureCoordinate.X = 0;
            vertex1.TextureCoordinate.Y = 1;
            vertex1.TextureCoordinate.Z = 0;

            vertex2.Position = vertex0.Position;
            vertex2.Position.X -= w;
            vertex2.Normal.X = 0;
            vertex2.Normal.Y = 0;
            vertex2.Normal.Z = 1;
            vertex2.TextureCoordinate.X = 1;
            vertex2.TextureCoordinate.Y = 0;
            vertex2.TextureCoordinate.Z = 0;

            vertex3.Position = vertex1.Position;
            vertex3.Position.X -= w;
            vertex3.Normal.X = 0;
            vertex3.Normal.Y = 0;
            vertex3.Normal.Z = 1;
            vertex3.TextureCoordinate.X = 1;
            vertex3.TextureCoordinate.Y = 1;
            vertex3.TextureCoordinate.Z = 0;

            vertex0.Hue =
                vertex1.Hue =
                    vertex2.Hue =
                        vertex3.Hue = hue;


            if (CheckInScreen(idx))
            {
                PushSprite(texture);

                return true;
            }

            return false;
        }

        public bool DrawSpriteLand(Texture2D texture, int x, int y, Rectangle rect, Vector3[] normals, Vector3 hue)
        {
            EnsureSize();

            int idx = NumSprites << 2;
            ref var vertex0 = ref VertexInfo[idx];
            ref var vertex1 = ref VertexInfo[idx + 1];
            ref var vertex2 = ref VertexInfo[idx + 2];
            ref var vertex3 = ref VertexInfo[idx + 3];


            vertex0.TextureCoordinate = Vector3.Zero;
            vertex1.TextureCoordinate.X = 1;
            vertex1.TextureCoordinate.Y = vertex1.TextureCoordinate.Z = 0;
            vertex2.TextureCoordinate.X = vertex2.TextureCoordinate.Z = 0;
            vertex2.TextureCoordinate.Y = 1;
            vertex3.TextureCoordinate.X = vertex3.TextureCoordinate.Y = 1;
            vertex3.TextureCoordinate.Z = 0;


            vertex0.Normal = normals[0];
            vertex1.Normal = normals[1];
            vertex3.Normal = normals[2]; // right order!
            vertex2.Normal = normals[3];

            vertex0.Position.X = x + 22;
            vertex0.Position.Y = y - rect.Left;
            vertex0.Position.Z = 0;

            vertex1.Position.X = x + 44;
            vertex1.Position.Y = y + (22 - rect.Bottom);
            vertex1.Position.Z = 0;

            vertex2.Position.X = x;
            vertex2.Position.Y = y + (22 - rect.Top);
            vertex2.Position.Z = 0;

            vertex3.Position.X = x + 22;
            vertex3.Position.Y = y + (44 - rect.Right);
            vertex3.Position.Z = 0;

            vertex0.Hue =
                vertex1.Hue =
                    vertex2.Hue =
                        vertex3.Hue = hue;

            if (CheckInScreen(idx))
            {
                PushSprite(texture);

                return true;
            }

            return false;
        }

        public bool DrawSpriteRotated(Texture2D texture, int x, int y, int w, int h, int destX, int destY, Vector3 hue, float angle)
        {
            EnsureSize();

            int idx = NumSprites << 2;
            ref var vertex0 = ref VertexInfo[idx];
            ref var vertex1 = ref VertexInfo[idx + 1];
            ref var vertex2 = ref VertexInfo[idx + 2];
            ref var vertex3 = ref VertexInfo[idx + 3];

            float ww = w / 2f;
            float hh = h / 2f;

            Vector3 center = new Vector3
            {
                X = x - (destX - 44 + ww),
                Y = y - (destY + hh)
            };

            float sinx = (float) Math.Sin(angle) * ww;
            float cosx = (float) Math.Cos(angle) * ww;
            float siny = (float) Math.Sin(angle) * hh;
            float cosy = (float) Math.Cos(angle) * hh;


            vertex0.Position = center;
            vertex0.Position.X += cosx - -siny;
            vertex0.Position.Y -= sinx + -cosy;
            vertex0.Normal.X = 0;
            vertex0.Normal.Y = 0;
            vertex0.Normal.Z = 1;
            vertex0.TextureCoordinate = Vector3.Zero;

            vertex1.Position = center;
            vertex1.Position.X += cosx - -siny;
            vertex1.Position.Y -= sinx + -cosy;
            vertex1.Normal.X = 0;
            vertex1.Normal.Y = 0;
            vertex1.Normal.Z = 1;
            vertex1.TextureCoordinate.X = 0;
            vertex1.TextureCoordinate.Y = 1;
            vertex1.TextureCoordinate.Z = 0;

            vertex2.Position = center;
            vertex2.Position.X += cosx - -siny;
            vertex2.Position.Y -= sinx + -cosy;
            vertex2.Normal.X = 0;
            vertex2.Normal.Y = 0;
            vertex2.Normal.Z = 1;
            vertex2.TextureCoordinate.X = 1;
            vertex2.TextureCoordinate.Y = 0;
            vertex2.TextureCoordinate.Z = 0;

            vertex3.Position = center;
            vertex3.Position.X += cosx - -siny;
            vertex3.Position.Y -= sinx + -cosy;
            vertex3.Normal.X = 0;
            vertex3.Normal.Y = 0;
            vertex3.Normal.Z = 1;
            vertex3.TextureCoordinate.X = 1;
            vertex3.TextureCoordinate.Y = 1;
            vertex3.TextureCoordinate.Z = 0;


            vertex0.Hue =
                vertex1.Hue =
                    vertex2.Hue =
                        vertex3.Hue = hue;

            if (CheckInScreen(idx))
            {
                PushSprite(texture);

                return true;
            }

            return false;
        }

        public bool DrawSpriteShadow(Texture2D texture, int x, int y, int w, int h, int destX, int destY, int offsetX, int offsetY, bool flip)
        {
            EnsureSize();

            int idx = NumSprites << 2;
            ref var vertex0 = ref VertexInfo[idx];
            ref var vertex1 = ref VertexInfo[idx + 1];
            ref var vertex2 = ref VertexInfo[idx + 2];
            ref var vertex3 = ref VertexInfo[idx + 3];


            if (flip)
            {
                vertex0.Position.X = x + destX + 44;
                vertex0.Position.Y = y - destY;
                vertex0.Position.Z = 0;
                vertex0.Normal.X = 0;
                vertex0.Normal.Y = 0;
                vertex0.Normal.Z = 1;
                vertex0.TextureCoordinate = Vector3.Zero;

                vertex1.Position = vertex0.Position;
                vertex1.Position.Y += h;
                vertex1.Normal.X = 0;
                vertex1.Normal.Y = 0;
                vertex1.Normal.Z = 1;
                vertex1.TextureCoordinate.X = 0;
                vertex1.TextureCoordinate.Y = 1;
                vertex1.TextureCoordinate.Z = 0;

                vertex2.Position = vertex0.Position;
                vertex2.Position.X -= w;
                vertex2.Normal.X = 0;
                vertex2.Normal.Y = 0;
                vertex2.Normal.Z = 1;
                vertex2.TextureCoordinate.X = 1;
                vertex2.TextureCoordinate.Y = 0;
                vertex2.TextureCoordinate.Z = 0;

                vertex3.Position = vertex1.Position;
                vertex3.Position.X -= w;
                vertex3.Normal.X = 0;
                vertex3.Normal.Y = 0;
                vertex3.Normal.Z = 1;
                vertex3.TextureCoordinate.X = 1;
                vertex3.TextureCoordinate.Y = 1;
                vertex3.TextureCoordinate.Z = 0;
            }
            else
            {
                vertex0.Position.X = x - destX;
                vertex0.Position.Y = y - destY;
                vertex0.Position.Z = 0;
                vertex0.Normal.X = 0;
                vertex0.Normal.Y = 0;
                vertex0.Normal.Z = 1;
                vertex0.TextureCoordinate = Vector3.Zero;

                vertex1.Position = vertex0.Position;
                vertex1.Position.X += w;
                vertex1.Normal.X = 0;
                vertex1.Normal.Y = 0;
                vertex1.Normal.Z = 1;
                vertex1.TextureCoordinate.X = 1;
                vertex1.TextureCoordinate.Y = 0;
                vertex1.TextureCoordinate.Z = 0;

                vertex2.Position = vertex0.Position;
                vertex2.Position.Y += h;
                vertex2.Normal.X = 0;
                vertex2.Normal.Y = 0;
                vertex2.Normal.Z = 1;
                vertex2.TextureCoordinate.X = 0;
                vertex2.TextureCoordinate.Y = 1;
                vertex2.TextureCoordinate.Z = 0;

                vertex3.Position = vertex1.Position;
                vertex3.Position.Y += h;
                vertex3.Normal.X = 0;
                vertex3.Normal.Y = 0;
                vertex3.Normal.Z = 1;
                vertex3.TextureCoordinate.X = 1;
                vertex3.TextureCoordinate.Y = 1;
                vertex3.TextureCoordinate.Z = 0;
            }


            float skewHorizTop = (vertex0.Position.Y - offsetY) * .5f;
            float skewHorizBottom = (vertex3.Position.Y - offsetY) * .5f;
            vertex0.Position.X -= skewHorizTop;
            vertex0.Position.Y -= skewHorizTop;

            if (flip)
            {
                vertex2.Position.X -= skewHorizTop;
                vertex2.Position.Y -= skewHorizTop;
                vertex2.Position.X -= skewHorizBottom;
                vertex2.Position.Y -= skewHorizBottom;
            }
            else
            {
                vertex1.Position.X -= skewHorizTop;
                vertex1.Position.Y -= skewHorizTop;
                vertex1.Position.X -= skewHorizBottom;
                vertex1.Position.Y -= skewHorizBottom;
            }

            vertex3.Position.X -= skewHorizBottom;
            vertex3.Position.Y -= skewHorizBottom;

            vertex0.Hue.Z =
                vertex1.Hue.Z =
                    vertex2.Hue.Z =
                        vertex3.Hue.Z =
                            vertex0.Hue.X =
                                vertex1.Hue.X =
                                    vertex2.Hue.X =
                                        vertex3.Hue.X = 0;

            vertex0.Hue.Y =
                vertex1.Hue.Y =
                    vertex2.Hue.Y =
                        vertex3.Hue.Y = ShaderHuesTraslator.SHADER_SHADOW;

            if (CheckInScreen(idx))
            {
                PushSprite(texture);

                return true;
            }

            return false;
        }

        public bool Draw2D(Texture2D texture, int x, int y, Vector3 hue)
        {
            EnsureSize();

            int idx = NumSprites << 2;
            ref var vertex0 = ref VertexInfo[idx];
            ref var vertex1 = ref VertexInfo[idx + 1];
            ref var vertex2 = ref VertexInfo[idx + 2];
            ref var vertex3 = ref VertexInfo[idx + 3];

            vertex0.Position.X = x;
            vertex0.Position.Y = y;
            vertex0.Position.Z = 0;
            vertex0.Normal.X = 0;
            vertex0.Normal.Y = 0;
            vertex0.Normal.Z = 1;
            vertex0.TextureCoordinate = Vector3.Zero;

            vertex1.Position.X = x + texture.Width;
            vertex1.Position.Y = y;
            vertex1.Position.Z = 0;
            vertex1.Normal.X = 0;
            vertex1.Normal.Y = 0;
            vertex1.Normal.Z = 1;
            vertex1.TextureCoordinate.X = 1;
            vertex1.TextureCoordinate.Y = 0;
            vertex1.TextureCoordinate.Z = 0;

            vertex2.Position.X = x;
            vertex2.Position.Y = y + texture.Height;
            vertex2.Position.Z = 0;
            vertex2.Normal.X = 0;
            vertex2.Normal.Y = 0;
            vertex2.Normal.Z = 1;
            vertex2.TextureCoordinate.X = 0;
            vertex2.TextureCoordinate.Y = 1;
            vertex2.TextureCoordinate.Z = 0;

            vertex3.Position.X = x + texture.Width;
            vertex3.Position.Y = y + texture.Height;
            vertex3.Position.Z = 0;
            vertex3.Normal.X = 0;
            vertex3.Normal.Y = 0;
            vertex3.Normal.Z = 1;
            vertex3.TextureCoordinate.X = 1;
            vertex3.TextureCoordinate.Y = 1;
            vertex3.TextureCoordinate.Z = 0;
            vertex0.Hue = vertex1.Hue = vertex2.Hue = vertex3.Hue = hue;

            if (CheckInScreen(idx))
            {
                PushSprite(texture);

                return true;
            }

            return false;
        }

        public bool Draw2D(Texture2D texture, int x, int y, int sx, int sy, int swidth, int sheight, Vector3 hue)
        {
            EnsureSize();

            float minX = sx / (float) texture.Width;
            float maxX = (sx + swidth) / (float) texture.Width;
            float minY = sy / (float) texture.Height;
            float maxY = (sy + sheight) / (float) texture.Height;


            int idx = NumSprites << 2;
            ref var vertex0 = ref VertexInfo[idx];
            ref var vertex1 = ref VertexInfo[idx + 1];
            ref var vertex2 = ref VertexInfo[idx + 2];
            ref var vertex3 = ref VertexInfo[idx + 3];

            vertex0.Position.X = x;
            vertex0.Position.Y = y;
            vertex0.Position.Z = 0;
            vertex0.Normal.X = 0;
            vertex0.Normal.Y = 0;
            vertex0.Normal.Z = 1;
            vertex0.TextureCoordinate.X = minX;
            vertex0.TextureCoordinate.Y = minY;
            vertex0.TextureCoordinate.Z = 0;
            vertex1.Position.X = x + swidth;
            vertex1.Position.Y = y;
            vertex1.Position.Z = 0;
            vertex1.Normal.X = 0;
            vertex1.Normal.Y = 0;
            vertex1.Normal.Z = 1;
            vertex1.TextureCoordinate.X = maxX;
            vertex1.TextureCoordinate.Y = minY;
            vertex2.TextureCoordinate.Z = 0;
            vertex2.Position.X = x;
            vertex2.Position.Y = y + sheight;
            vertex2.Position.Z = 0;
            vertex2.Normal.X = 0;
            vertex2.Normal.Y = 0;
            vertex2.Normal.Z = 1;
            vertex2.TextureCoordinate.X = minX;
            vertex2.TextureCoordinate.Y = maxY;
            vertex2.TextureCoordinate.Z = 0;
            vertex3.Position.X = x + swidth;
            vertex3.Position.Y = y + sheight;
            vertex3.Position.Z = 0;
            vertex3.Normal.X = 0;
            vertex3.Normal.Y = 0;
            vertex3.Normal.Z = 1;
            vertex3.TextureCoordinate.X = maxX;
            vertex3.TextureCoordinate.Y = maxY;
            vertex3.TextureCoordinate.Z = 0;
            vertex0.Hue = vertex1.Hue = vertex2.Hue = vertex3.Hue = hue;

            if (CheckInScreen(idx))
            {
                PushSprite(texture);

                return true;
            }

            return false;
        }

        public bool Draw2D(Texture2D texture, int dx, int dy, int dwidth, int dheight, int sx, int sy, int swidth, int sheight, Vector3 hue)
        {
            EnsureSize();

            float minX = sx / (float) texture.Width, maxX = (sx + swidth) / (float) texture.Width;
            float minY = sy / (float) texture.Height, maxY = (sy + sheight) / (float) texture.Height;

            int idx = NumSprites << 2;
            ref var vertex0 = ref VertexInfo[idx];
            ref var vertex1 = ref VertexInfo[idx + 1];
            ref var vertex2 = ref VertexInfo[idx + 2];
            ref var vertex3 = ref VertexInfo[idx + 3];

            vertex0.Position.X = dx;
            vertex0.Position.Y = dy;
            vertex0.Position.Z = 0;
            vertex0.Normal.X = 0;
            vertex0.Normal.Y = 0;
            vertex0.Normal.Z = 1;
            vertex0.TextureCoordinate.X = minX;
            vertex0.TextureCoordinate.Y = minY;
            vertex0.TextureCoordinate.Z = 0;
            vertex1.Position.X = dx + dwidth;
            vertex1.Position.Y = dy;
            vertex1.Position.Z = 0;
            vertex1.Normal.X = 0;
            vertex1.Normal.Y = 0;
            vertex1.Normal.Z = 1;
            vertex1.TextureCoordinate.X = maxX;
            vertex1.TextureCoordinate.Y = minY;
            vertex1.TextureCoordinate.Z = 0;
            vertex2.Position.X = dx;
            vertex2.Position.Y = dy + dheight;
            vertex2.Position.Z = 0;
            vertex2.Normal.X = 0;
            vertex2.Normal.Y = 0;
            vertex2.Normal.Z = 1;
            vertex2.TextureCoordinate.X = minX;
            vertex2.TextureCoordinate.Y = maxY;
            vertex2.TextureCoordinate.Z = 0;
            vertex3.Position.X = dx + dwidth;
            vertex3.Position.Y = dy + dheight;
            vertex3.Position.Z = 0;
            vertex3.Normal.X = 0;
            vertex3.Normal.Y = 0;
            vertex3.Normal.Z = 1;
            vertex3.TextureCoordinate.X = maxX;
            vertex3.TextureCoordinate.Y = maxY;
            vertex3.TextureCoordinate.Z = 0;
            vertex0.Hue = vertex1.Hue = vertex2.Hue = vertex3.Hue = hue;

            if (CheckInScreen(idx))
            {
                PushSprite(texture);

                return true;
            }

            return false;
        }

        public bool Draw2D(Texture2D texture, int x, int y, int width, int height, Vector3 hue)
        {
            EnsureSize();

            int idx = NumSprites << 2;
            ref var vertex0 = ref VertexInfo[idx];
            ref var vertex1 = ref VertexInfo[idx + 1];
            ref var vertex2 = ref VertexInfo[idx + 2];
            ref var vertex3 = ref VertexInfo[idx + 3];

            vertex0.Position.X = x;
            vertex0.Position.Y = y;
            vertex0.Position.Z = 0;
            vertex0.Normal.X = 0;
            vertex0.Normal.Y = 0;
            vertex0.Normal.Z = 1;
            vertex0.TextureCoordinate = Vector3.Zero;
            vertex1.Position.X = x + width;
            vertex1.Position.Y = y;
            vertex1.Position.Z = 0;
            vertex1.Normal.X = 0;
            vertex1.Normal.Y = 0;
            vertex1.Normal.Z = 1;
            vertex1.TextureCoordinate.X = 1;
            vertex1.TextureCoordinate.Y = 0;
            vertex1.TextureCoordinate.Z = 0;
            vertex2.Position.X = x;
            vertex2.Position.Y = y + height;
            vertex2.Position.Z = 0;
            vertex2.Normal.X = 0;
            vertex2.Normal.Y = 0;
            vertex2.Normal.Z = 1;
            vertex2.TextureCoordinate.X = 0;
            vertex2.TextureCoordinate.Y = 1;
            vertex2.TextureCoordinate.Z = 0;
            vertex3.Position.X = x + width;
            vertex3.Position.Y = y + height;
            vertex3.Position.Z = 0;
            vertex3.Normal.X = 0;
            vertex3.Normal.Y = 0;
            vertex3.Normal.Z = 1;
            vertex3.TextureCoordinate.X = 1;
            vertex3.TextureCoordinate.Y = 1;
            vertex3.TextureCoordinate.Z = 0;
            vertex0.Hue = vertex1.Hue = vertex2.Hue = vertex3.Hue = hue;

            if (CheckInScreen(idx))
            {
                PushSprite(texture);

                return true;
            }

            return false;
        }

        public bool Draw2DTiled(Texture2D texture, int dx, int dy, int dwidth, int dheight, Vector3 hue)
        {
            int y = dy;
            int h = dheight;

            while (h > 0)
            {
                int x = dx;
                int w = dwidth;

                int rw = texture.Width;
                int rh = h < texture.Height ? h : texture.Height;

                while (w > 0)
                {
                    if (w < texture.Width)
                        rw = w;
                    Draw2D(texture, x, y, 0, 0, rw, rh, hue);
                    w -= texture.Width;
                    x += texture.Width;
                }

                h -= texture.Height;
                y += texture.Height;
            }

            return true;
        }

        public bool DrawRectangle(Texture2D texture, int x, int y, int width, int height, Vector3 hue)
        {
            Draw2D(texture, x, y, width, 1, hue);
            Draw2D(texture, x + width, y, 1, height + 1, hue);
            Draw2D(texture, x, y + height, width, 1, hue);
            Draw2D(texture, x, y, 1, height, hue);

            return true;
        }


        protected override bool CheckInScreen(int index)
        {
            for (byte i = 0; i < 4; i++)
                if (DrawingArea.Contains(VertexInfo[index + i].Position) == ContainmentType.Contains)
                    return true;

            return false;
        }

        private class IsometricEffect : MatrixEffect
        {
            public IsometricEffect(GraphicsDevice graphicsDevice) : base(graphicsDevice, Resources.IsometricEffect)
            {
                WorldMatrix = Parameters["WorldMatrix"];
                Viewport = Parameters["Viewport"];
                CurrentTechnique = Techniques["HueTechnique"];
            }

            protected IsometricEffect(Effect cloneSource) : base(cloneSource)
            {
            }


            public EffectParameter WorldMatrix { get; }
            public EffectParameter Viewport { get; }

            public override void ApplyStates()
            {
                WorldMatrix.SetValue(Matrix.Identity);
                Viewport.SetValue(new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height));

                base.ApplyStates();
            }
        }
    }

    internal class FNABatcher2D : Batcher2D<VertexPositionColorTexture4>
    {
        public FNABatcher2D(GraphicsDevice device) : base(device, new MatrixEffect(device, Resources.StandardEffect))
        {
        }

        public bool Draw(Texture2D texture, float x, float y, float w, float h, Color color)
        {
            EnsureSize();

            int idx = NumSprites << 2;

            ref var vertex0 = ref VertexInfo[idx];


            vertex0.Position0.X = x;
            vertex0.Position0.Y = y;

            vertex0.Position1.X = x + w;
            vertex0.Position1.Y = y;

            vertex0.Position2.X = x;
            vertex0.Position2.Y = y + h;

            vertex0.Position3.X = x + w;
            vertex0.Position3.Y = y + h;


            vertex0.TextureCoordinate0.X = 0;
            vertex0.TextureCoordinate0.Y = 0;

            vertex0.TextureCoordinate1.X = 1;
            vertex0.TextureCoordinate1.Y = 0;

            vertex0.TextureCoordinate2.X = 0;
            vertex0.TextureCoordinate2.Y = 1;

            vertex0.TextureCoordinate3.X = 1;
            vertex0.TextureCoordinate3.Y = 1;

            vertex0.Color0 = vertex0.Color1 = vertex0.Color2 = vertex0.Color3 = color;


            return CheckInScreen(idx) && PushSprite(texture);
        }

        public bool Draw(Texture2D texture, float x, float y, Color color)
        {
            return Draw(texture, x, y, texture.Width, texture.Height, color);
        }

        public void DrawString(SpriteFont spriteFont, string text, int x, int y, Vector3 color)
        {
            if (text == null) throw new ArgumentNullException("text");

            if (text.Length == 0) return;

            EnsureSize();

            Texture2D textureValue = spriteFont.Texture;
            List<Rectangle> glyphData = spriteFont.GlyphData;
            List<Rectangle> croppingData = spriteFont.CroppingData;
            List<Vector3> kerning = spriteFont.Kerning;
            List<char> characterMap = spriteFont.CharacterMap;

            Vector2 curOffset = Vector2.Zero;
            bool firstInLine = true;

            Vector2 baseOffset = Vector2.Zero;
            float axisDirX = 1;
            float axisDirY = 1;

            foreach (char c in text)
            {
                // Special characters
                if (c == '\r') continue;

                if (c == '\n')
                {
                    curOffset.X = 0.0f;
                    curOffset.Y += spriteFont.LineSpacing;
                    firstInLine = true;

                    continue;
                }

                /* Get the List index from the character map, defaulting to the
				 * DefaultCharacter if it's set.
				 */
                int index = characterMap.IndexOf(c);

                if (index == -1)
                {
                    if (!spriteFont.DefaultCharacter.HasValue)
                    {
                        throw new ArgumentException(
                                                    "Text contains characters that cannot be" +
                                                    " resolved by this SpriteFont.",
                                                    "text"
                                                   );
                    }

                    index = characterMap.IndexOf(
                                                 spriteFont.DefaultCharacter.Value
                                                );
                }

                /* For the first character in a line, always push the width
				 * rightward, even if the kerning pushes the character to the
				 * left.
				 */
                Vector3 cKern = kerning[index];

                if (firstInLine)
                {
                    curOffset.X += Math.Abs(cKern.X);
                    firstInLine = false;
                }
                else
                    curOffset.X += spriteFont.Spacing + cKern.X;

                // Calculate the character origin
                Rectangle cCrop = croppingData[index];
                Rectangle cGlyph = glyphData[index];

                float offsetX = baseOffset.X + (
                                                   curOffset.X + cCrop.X
                                               ) * axisDirX;

                float offsetY = baseOffset.Y + (
                                                   curOffset.Y + cCrop.Y
                                               ) * axisDirY;

                //Draw(textureValue,)

                //Draw2D(textureValue,
                //       x + (int)offsetX, y + (int)offsetY,
                //       cGlyph.X, cGlyph.Y, cGlyph.Width, cGlyph.Height,
                //       color);

                curOffset.X += cKern.Y + cKern.Z;
            }
        }

    }


    internal abstract class Batcher2D<T> where T : struct, IVertexType
    {
        protected const int MAX_SPRITES = 0x800;
        protected const int MAX_VERTICES = MAX_SPRITES * 4;
        protected const int MAX_INDICES = MAX_SPRITES * 6;


        private readonly MatrixEffect _defaultEffect;
        private readonly IndexBuffer _indexBuffer;
        private readonly Vector3 _minVector3 = new Vector3(0, 0, -150);
        private readonly RasterizerState _rasterizerState;
        private readonly VertexBuffer _vertexBuffer;
        private BlendState _blendState;
        private Effect _customEffect;
        private bool _started;
        private DepthStencilState _stencil;
        private bool _useScissor;


        protected BoundingBox DrawingArea;
        protected int NumSprites;
        protected readonly Texture2D[] TextureInfo;
        protected readonly T[] VertexInfo;


        protected Batcher2D(GraphicsDevice device, MatrixEffect defaultEffect)
        {
            GraphicsDevice = device;
            TextureInfo = new Texture2D[MAX_SPRITES];
            VertexInfo = new T[MAX_VERTICES];
            _vertexBuffer = new DynamicVertexBuffer(GraphicsDevice, typeof(T), MAX_VERTICES, BufferUsage.WriteOnly);
            _indexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, MAX_INDICES, BufferUsage.WriteOnly);
            _indexBuffer.SetData(GenerateIndexArray());
            _blendState = BlendState.AlphaBlend;
            _rasterizerState = RasterizerState.CullNone;

            _rasterizerState = new RasterizerState
            {
                CullMode = _rasterizerState.CullMode,
                DepthBias = _rasterizerState.DepthBias,
                FillMode = _rasterizerState.FillMode,
                MultiSampleAntiAlias = _rasterizerState.MultiSampleAntiAlias,
                SlopeScaleDepthBias = _rasterizerState.SlopeScaleDepthBias,
                ScissorTestEnable = true
            };

            _stencil = Stencil;

            _defaultEffect = defaultEffect;
        }



        public DepthStencilState Stencil { get; } = new DepthStencilState
        {
            StencilEnable = false,
            DepthBufferEnable = false,
            StencilFunction = CompareFunction.NotEqual,
            ReferenceStencil = 1,
            StencilMask = 1,
            StencilFail = StencilOperation.Keep,
            StencilDepthBufferFail = StencilOperation.Keep,
            StencilPass = StencilOperation.Keep
        };

        public GraphicsDevice GraphicsDevice { get; }


        public void Begin()
        {
            Begin(null, Matrix.Identity);
        }

        public void Begin(Effect effect)
        {
            Begin(effect, Matrix.Identity);
        }

        public void Begin(Effect customEffect, Matrix projection)
        {
            EnsureNotStarted();
            _started = true;

            DrawingArea.Min = _minVector3;
            DrawingArea.Max.X = GraphicsDevice.Viewport.Width;
            DrawingArea.Max.Y = GraphicsDevice.Viewport.Height;
            DrawingArea.Max.Z = 150;

            _customEffect = customEffect;
        }

        public void End()
        {
            EnsureStarted();
            Flush();
            _started = false;
            _customEffect = null;
        }

        protected virtual bool CheckInScreen(int index)
        {
            return true;
        }

        protected virtual void EnsureSize()
        {
            EnsureStarted();

            if (NumSprites >= MAX_SPRITES)
                Flush();
        }

        protected bool PushSprite(Texture2D texture)
        {
            EnsureSize();
            TextureInfo[NumSprites++] = texture;

            return true;
        }

        [Conditional("DEBUG")]
        protected void EnsureStarted()
        {
            if (!_started)
                throw new InvalidOperationException();
        }

        [Conditional("DEBUG")]
        protected void EnsureNotStarted()
        {
            if (_started)
                throw new InvalidOperationException();
        }

        private void ApplyStates()
        {
            GraphicsDevice.BlendState = _blendState;
            GraphicsDevice.DepthStencilState = _stencil;
            GraphicsDevice.RasterizerState = _useScissor ? _rasterizerState : RasterizerState.CullNone;
            GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;

            GraphicsDevice.SamplerStates[1] = SamplerState.PointClamp;
            GraphicsDevice.SamplerStates[2] = SamplerState.PointClamp;


            GraphicsDevice.SetVertexBuffer(_vertexBuffer);
            GraphicsDevice.Indices = _indexBuffer;


            _defaultEffect.ApplyStates();
        }

        private void Flush()
        {
            ApplyStates();

            if (NumSprites == 0)
                return;

            _vertexBuffer.SetData(VertexInfo, 0, NumSprites << 2);

            Texture2D current = TextureInfo[0];
            int offset = 0;

            if (_customEffect != null)
            {
                if (_customEffect is MatrixEffect eff)
                    eff.ApplyStates();
                else
                    _customEffect.CurrentTechnique.Passes[0].Apply();
            }



            for (int i = 1; i < NumSprites; i++)
            {
                if (TextureInfo[i] != current)
                {
                    InternalDraw(current, offset, i - offset);
                    current = TextureInfo[i];
                    offset = i;
                }
            }

            InternalDraw(current, offset, NumSprites - offset);

            NumSprites = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InternalDraw(Texture2D texture, int baseSprite, int batchSize)
        {
            GraphicsDevice.Textures[0] = texture;
            GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, baseSprite << 2, 0, batchSize << 2, 0, batchSize << 1);
        }

        public void EnableScissorTest(bool enable)
        {
            if (enable == _useScissor)
                return;

            Flush();

            //_rasterizerState?.Dispose();

            _useScissor = enable;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetBlendState(BlendState blend, bool noflush = false)
        {
            if (!noflush)
                Flush();

            _blendState = blend ?? BlendState.AlphaBlend;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStencil(DepthStencilState stencil, bool noflush = false)
        {
            if (!noflush)
                Flush();

            _stencil = stencil ?? Stencil;
        }

        private static short[] GenerateIndexArray()
        {
            short[] result = new short[MAX_INDICES];

            for (int i = 0, j = 0; i < MAX_INDICES; i += 6, j += 4)
            {
                result[i] = (short) j;
                result[i + 1] = (short) (j + 1);
                result[i + 2] = (short) (j + 2);
                result[i + 3] = (short) (j + 1);
                result[i + 4] = (short) (j + 3);
                result[i + 5] = (short) (j + 2);
            }

            return result;
        }
    }



    internal class Resources
    {
        private static byte[] _isometricEffect, _xBREffect;

        public static byte[] IsometricEffect => _isometricEffect ?? (_isometricEffect = GetResource("ClassicUO.shaders.IsometricWorld.fxc"));

        public static byte[] xBREffect => _xBREffect ?? (_xBREffect = GetResource("ClassicUO.shaders.xBR.fxc"));

        public static byte[] StandardEffect
        {
            get
            {
                Stream stream = typeof(SpriteBatch).Assembly.GetManifestResourceStream(
                                                                                       "Microsoft.Xna.Framework.Graphics.Effect.Resources.SpriteEffect.fxb"
                                                                                      );

                using (MemoryStream ms = new MemoryStream())
                {
                    stream.CopyTo(ms);

                    return ms.ToArray();
                }
            }
        }

        private static byte[] GetResource(string name)
        {
            Stream stream = typeof(UltimaBatcher2D).Assembly.GetManifestResourceStream(
                                                                                       name
                                                                                      );

            using (MemoryStream ms = new MemoryStream())
            {
                stream.CopyTo(ms);

                return ms.ToArray();
            }
        }
    }



    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct VertexPositionColorTexture4 : IVertexType
    {
        public const int RealStride = 96;

        VertexDeclaration IVertexType.VertexDeclaration => VertexPositionColorTexture.VertexDeclaration;

        public Vector3 Position0;
        public Color Color0;
        public Vector2 TextureCoordinate0;
        public Vector3 Position1;
        public Color Color1;
        public Vector2 TextureCoordinate1;
        public Vector3 Position2;
        public Color Color2;
        public Vector2 TextureCoordinate2;
        public Vector3 Position3;
        public Color Color3;
        public Vector2 TextureCoordinate3;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct PositionNormalTextureColor : IVertexType
    {

        public Vector3 Position;
        public Vector3 Normal;
        public Vector3 TextureCoordinate;
        public Vector3 Hue;

        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0), // position
                                                                                           new VertexElement(sizeof(float) * 3, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0), // normal
                                                                                           new VertexElement(sizeof(float) * 6, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0), // tex coord
                                                                                           new VertexElement(sizeof(float) * 9, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 1) // hue
                                                                                         );

        public const int SizeInBytes = sizeof(float) * 12 * 4;

#if DEBUG
        public override string ToString()
        {
            return string.Format("VPNTH: <{0}> <{1}>", Position.ToString(), TextureCoordinate.ToString());
        }
#endif
    }
}