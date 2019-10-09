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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using ClassicUO.Utility;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer
{
    internal sealed class UltimaBatcher2D : IDisposable
    {
        private const int MAX_SPRITES = 0x800;
        private const int MAX_VERTICES = MAX_SPRITES * 4;
        private const int MAX_INDICES = MAX_SPRITES * 6;


        private readonly IndexBuffer _indexBuffer;
        private readonly RasterizerState _rasterizerState;
        private readonly VertexBuffer _vertexBuffer;
        private readonly Texture2D[] _textureInfo;
        private readonly PositionNormalTextureColor[] _vertexInfo;
        private BlendState _blendState;
        private Effect _customEffect;
        private bool _started;
        private DepthStencilState _stencil;
        private bool _useScissor;
        private BoundingBox _drawingArea;
        private int _numSprites;
        //private readonly IntPtr _ptrVertexBufferArray;
        private GCHandle _handle;

        public UltimaBatcher2D(GraphicsDevice device)
        {
            GraphicsDevice = device;
            _textureInfo = new Texture2D[MAX_SPRITES];
            _vertexInfo = new PositionNormalTextureColor[MAX_VERTICES];
            _vertexBuffer = new DynamicVertexBuffer(GraphicsDevice, typeof(PositionNormalTextureColor), MAX_VERTICES, BufferUsage.WriteOnly);
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

            DefaultEffect = new IsometricEffect(device);

            GraphicsDevice.Indices = _indexBuffer;
            GraphicsDevice.SetVertexBuffer(_vertexBuffer);

            _handle = GCHandle.Alloc(_vertexInfo, GCHandleType.Pinned);
        }


        public MatrixEffect DefaultEffect { get; }

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



        public void SetBrightlight(float f)
        {
            ((IsometricEffect)DefaultEffect).Brighlight.SetValue(f);
        }

        public void DrawString(SpriteFont spriteFont, string text, int x, int y, ref Vector3 color)
        {
            if (string.IsNullOrEmpty(text))
                return;

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
                       ref color);

                curOffset.X += cKern.Y + cKern.Z;
            }
        }

        [MethodImpl(256)]
        public bool DrawSprite(Texture2D texture, int x, int y, int w, int h, int destX, int destY, ref Vector3 hue)
        {
            EnsureSize();

            int idx = _numSprites << 2;
            ref var vertex0 = ref _vertexInfo[idx];
            ref var vertex1 = ref _vertexInfo[idx + 1];
            ref var vertex2 = ref _vertexInfo[idx + 2];
            ref var vertex3 = ref _vertexInfo[idx + 3];


            vertex0.Position.X = x - destX;
            vertex0.Position.Y = y - destY;
            vertex0.Position.Z = 0;
            vertex0.Normal.X = 0;
            vertex0.Normal.Y = 0;
            vertex0.Normal.Z = 1;
            vertex0.TextureCoordinate.X = 0;
            vertex0.TextureCoordinate.Y = 0;
            vertex0.TextureCoordinate.Z = 0;

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

        [MethodImpl(256)]
        public bool DrawSpriteFlipped(Texture2D texture, int x, int y, int w, int h, int destX, int destY, ref Vector3 hue)
        {
            EnsureSize();

            int idx = _numSprites << 2;
            ref var vertex0 = ref _vertexInfo[idx];
            ref var vertex1 = ref _vertexInfo[idx + 1];
            ref var vertex2 = ref _vertexInfo[idx + 2];
            ref var vertex3 = ref _vertexInfo[idx + 3];


            vertex0.Position.X = x + destX + 44;
            vertex0.Position.Y = y - destY;
            vertex0.Position.Z = 0;
            vertex0.Normal.X = 0;
            vertex0.Normal.Y = 0;
            vertex0.Normal.Z = 1;
            vertex0.TextureCoordinate.X = 0;
            vertex0.TextureCoordinate.Y = 0;
            vertex0.TextureCoordinate.Z = 0;

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

        [MethodImpl(256)]
        public bool DrawSpriteLand(Texture2D texture, int x, int y, ref Rectangle rect, ref Vector3[] normals, ref Vector3 hue)
        {
            EnsureSize();

            int idx = _numSprites << 2;
            ref var vertex0 = ref _vertexInfo[idx];
            ref var vertex1 = ref _vertexInfo[idx + 1];
            ref var vertex2 = ref _vertexInfo[idx + 2];
            ref var vertex3 = ref _vertexInfo[idx + 3];


            vertex0.TextureCoordinate.X = 0;
            vertex0.TextureCoordinate.Y = 0;
            vertex0.TextureCoordinate.Z = 0;
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

        [MethodImpl(256)]
        public bool DrawSpriteRotated(Texture2D texture, int x, int y, int w, int h, int destX, int destY, ref Vector3 hue, float angle)
        {
            EnsureSize();

            int idx = _numSprites << 2;
            ref var vertex0 = ref _vertexInfo[idx];
            ref var vertex1 = ref _vertexInfo[idx + 1];
            ref var vertex2 = ref _vertexInfo[idx + 2];
            ref var vertex3 = ref _vertexInfo[idx + 3];

            float ww = w / 2f;
            float hh = h / 2f;


            float startX = x - (destX - 44 + ww);
            float startY = y - (destY + hh);

            float sin = (float) Math.Sin(angle);
            float cos = (float) Math.Cos(angle);

            float sinx = sin * ww;
            float cosx = cos * ww;
            float siny = sin * hh;
            float cosy = cos * hh;


            vertex0.Position.X = startX;
            vertex0.Position.Y = startY;
            vertex0.Position.X += cosx - -siny;
            vertex0.Position.Y -= sinx + -cosy;
            vertex0.Normal.X = 0;
            vertex0.Normal.Y = 0;
            vertex0.Normal.Z = 1;
            vertex0.TextureCoordinate.X = 0;
            vertex0.TextureCoordinate.Y = 0;
            vertex0.TextureCoordinate.Z = 0;

            vertex1.Position.X = startX;
            vertex1.Position.Y = startY;
            vertex1.Position.X += cosx - siny;
            vertex1.Position.Y += -sinx + -cosy;
            vertex1.Normal.X = 0;
            vertex1.Normal.Y = 0;
            vertex1.Normal.Z = 1;
            vertex1.TextureCoordinate.X = 0;
            vertex1.TextureCoordinate.Y = 1;
            vertex1.TextureCoordinate.Z = 0;

            vertex2.Position.X = startX;
            vertex2.Position.Y = startY;
            vertex2.Position.X += -cosx - -siny;
            vertex2.Position.Y += sinx + cosy;
            vertex2.Normal.X = 0;
            vertex2.Normal.Y = 0;
            vertex2.Normal.Z = 1;
            vertex2.TextureCoordinate.X = 1;
            vertex2.TextureCoordinate.Y = 0;
            vertex2.TextureCoordinate.Z = 0;

            vertex3.Position.X = startX;
            vertex3.Position.Y = startY;
            vertex3.Position.X += -cosx - siny;
            vertex3.Position.Y += sinx + -cosy;
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

        [MethodImpl(256)]
        public bool DrawSpriteShadow(Texture2D texture, int x, int y, bool flip)
        {
            EnsureSize();

            int idx = _numSprites << 2;
            ref var vertex0 = ref _vertexInfo[idx];
            ref var vertex1 = ref _vertexInfo[idx + 1];
            ref var vertex2 = ref _vertexInfo[idx + 2];
            ref var vertex3 = ref _vertexInfo[idx + 3];

            float width = texture.Width;
            float height = texture.Height / 2f;

            float translatedY = y + height * 0.75f;

            float ratio = height / width;

            if (flip)
            {
                vertex0.Position.X = x + width;
                vertex0.Position.Y = translatedY + height;
                vertex0.Position.Z = 0;
                vertex0.Normal.X = 0;
                vertex0.Normal.Y = 0;
                vertex0.Normal.Z = 1;
                vertex0.TextureCoordinate.X = 0;
                vertex0.TextureCoordinate.Y = 1;
                vertex0.TextureCoordinate.Z = 0;

                vertex1.Position.X = x;
                vertex1.Position.Y = translatedY + height;
                vertex1.Normal.X = 0;
                vertex1.Normal.Y = 0;
                vertex1.Normal.Z = 1;
                vertex1.TextureCoordinate.X = 1;
                vertex1.TextureCoordinate.Y = 1;
                vertex1.TextureCoordinate.Z = 0;

                vertex2.Position.X = x + width * (ratio + 1f);
                vertex2.Position.Y = translatedY;
                vertex2.Normal.X = 0;
                vertex2.Normal.Y = 0;
                vertex2.Normal.Z = 1;
                vertex2.TextureCoordinate.X = 0;
                vertex2.TextureCoordinate.Y = 0;
                vertex2.TextureCoordinate.Z = 0;

                vertex3.Position.X = x + width * ratio;
                vertex3.Position.Y = translatedY;
                vertex3.Normal.X = 0;
                vertex3.Normal.Y = 0;
                vertex3.Normal.Z = 1;
                vertex3.TextureCoordinate.X = 1;
                vertex3.TextureCoordinate.Y = 0;
                vertex3.TextureCoordinate.Z = 0;
            }
            else
            {
                vertex0.Position.X = x;
                vertex0.Position.Y = translatedY + height;
                vertex0.Position.Z = 0;
                vertex0.Normal.X = 0;
                vertex0.Normal.Y = 0;
                vertex0.Normal.Z = 1;
                vertex0.TextureCoordinate.X = 0;
                vertex0.TextureCoordinate.Y = 1;
                vertex0.TextureCoordinate.Z = 0;

                vertex1.Position.X = x + width;
                vertex1.Position.Y = translatedY + height;
                vertex1.Normal.X = 0;
                vertex1.Normal.Y = 0;
                vertex1.Normal.Z = 1;
                vertex1.TextureCoordinate.X = 1;
                vertex1.TextureCoordinate.Y = 1;
                vertex1.TextureCoordinate.Z = 0;

                vertex2.Position.X = x + width * ratio;
                vertex2.Position.Y = translatedY;
                vertex2.Normal.X = 0;
                vertex2.Normal.Y = 0;
                vertex2.Normal.Z = 1;
                vertex2.TextureCoordinate.X = 0;
                vertex2.TextureCoordinate.Y = 0;
                vertex2.TextureCoordinate.Z = 0;

                vertex3.Position.X = x + width * (ratio + 1f);
                vertex3.Position.Y = translatedY;
                vertex3.Normal.X = 0;
                vertex3.Normal.Y = 0;
                vertex3.Normal.Z = 1;
                vertex3.TextureCoordinate.X = 1;
                vertex3.TextureCoordinate.Y = 0;
                vertex3.TextureCoordinate.Z = 0;
            }

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

        [MethodImpl(256)]
        public bool DrawCharacterSitted(Texture2D texture, int x, int y, bool mirror, float h3mod, float h6mod, float h9mod, ref Vector3 hue)
        {
            EnsureSize();

            int idx = _numSprites << 2;

            float width = texture.Width;
            float height = texture.Height;


            float h03 = height * h3mod;
            float h06 = height * h6mod;
            float h09 = height * h9mod;

            const float SITTING_OFFSET = 8.0f;

            float widthOffset = width + SITTING_OFFSET;


            int count = 0;

            if (mirror)
            {
                if (h3mod != 0.0f)
                {
                    ref var vertex0 = ref _vertexInfo[idx + count++];
                    ref var vertex1 = ref _vertexInfo[idx + count++];
                    ref var vertex2 = ref _vertexInfo[idx + count++];
                    ref var vertex3 = ref _vertexInfo[idx + count++];

                    vertex0.Position.X = x + width;
                    vertex0.Position.Y = y;
                    vertex0.Position.Z = 0;
                    vertex0.TextureCoordinate.X = 0;
                    vertex0.TextureCoordinate.Y = 0;
                    vertex0.TextureCoordinate.Z = 0;

                    vertex1.Position.X = x;
                    vertex1.Position.Y = y;
                    vertex1.Position.Z = 0;
                    vertex1.TextureCoordinate.X = 1;
                    vertex1.TextureCoordinate.Y = 0;
                    vertex1.TextureCoordinate.Z = 0;

                    vertex2.Position.X = x + width;
                    vertex2.Position.Y = y + h03;
                    vertex2.Position.Z = 0;
                    vertex2.TextureCoordinate.X = 0;
                    vertex2.TextureCoordinate.Y = h3mod;
                    vertex2.TextureCoordinate.Z = 0;

                    vertex3.Position.X = x;
                    vertex3.Position.Y = y + h03;
                    vertex3.Position.Z = 0;
                    vertex3.TextureCoordinate.X = 1;
                    vertex3.TextureCoordinate.Y = h3mod;
                    vertex3.TextureCoordinate.Z = 0;


                    vertex0.Normal.X = 0;
                    vertex0.Normal.Y = 0;
                    vertex0.Normal.Z = 1;
                    vertex1.Normal.X = 0;
                    vertex1.Normal.Y = 0;
                    vertex1.Normal.Z = 1;
                    vertex2.Normal.X = 0;
                    vertex2.Normal.Y = 0;
                    vertex2.Normal.Z = 1;
                    vertex3.Normal.X = 0;
                    vertex3.Normal.Y = 0;
                    vertex3.Normal.Z = 1;
                    vertex0.Hue = vertex1.Hue = vertex2.Hue = vertex3.Hue = hue;
                }

                if (h6mod != 0.0f)
                {
                    //if (h3mod == 0.0f)
                    //{
                    //    if (idx + count + 2 >= MAX_SPRITES)
                    //        Flush();

                    //    ref var vertex4 = ref VertexInfo[idx + count++];
                    //    ref var vertex5 = ref VertexInfo[idx + count++];

                    //    vertex4.Position.X = x + width;
                    //    vertex4.Position.Y = y;
                    //    vertex4.Position.Z = 0;
                    //    vertex4.TextureCoordinate.X = 0;
                    //    vertex4.TextureCoordinate.Y = 0;
                    //    vertex4.TextureCoordinate.Z = 0;

                    //    vertex5.Position.X = x;
                    //    vertex5.Position.Y = y;
                    //    vertex5.Position.Z = 0;
                    //    vertex5.TextureCoordinate.X = 1;
                    //    vertex5.TextureCoordinate.Y = 0;
                    //    vertex5.TextureCoordinate.Z = 0;

                    //    vertex4.Normal.X = vertex4.Normal.Y = vertex5.Normal.X = vertex5.Normal.Y = 0;
                    //    vertex4.Normal.Z = vertex5.Normal.Z = 1;
                    //    vertex4.Hue = vertex5.Hue = hue;
                    //}

                    if (_numSprites + 4 >= MAX_SPRITES)
                    {
                        idx = 0;
                        Flush();
                    }

                    ref var vertex4_s = ref _vertexInfo[idx + count++];
                    ref var vertex5_s = ref _vertexInfo[idx + count++];
                    ref var vertex6_s = ref _vertexInfo[idx + count++];
                    ref var vertex7_s = ref _vertexInfo[idx + count++];


                    vertex4_s.Position.X = x + width;
                    vertex4_s.Position.Y = y + h03;
                    vertex4_s.Position.Z = 0;
                    vertex4_s.TextureCoordinate.X = 0;
                    vertex4_s.TextureCoordinate.Y = h3mod;
                    vertex4_s.TextureCoordinate.Z = 0;

                    vertex5_s.Position.X = x;
                    vertex5_s.Position.Y = y + h03;
                    vertex5_s.Position.Z = 0;
                    vertex5_s.TextureCoordinate.X = 1;
                    vertex5_s.TextureCoordinate.Y = h3mod;
                    vertex5_s.TextureCoordinate.Z = 0;

                    //vertex4_s.Position.X = x + width;
                    //vertex4_s.Position.Y = y;
                    //vertex4_s.Position.Z = 0;
                    //vertex4_s.TextureCoordinate.X = 0;
                    //vertex4_s.TextureCoordinate.Y = 0;
                    //vertex4_s.TextureCoordinate.Z = 0;

                    //vertex5_s.Position.X = x;
                    //vertex5_s.Position.Y = y;
                    //vertex5_s.Position.Z = 0;
                    //vertex5_s.TextureCoordinate.X = 1;
                    //vertex5_s.TextureCoordinate.Y = 0;
                    //vertex5_s.TextureCoordinate.Z = 0;

                    vertex6_s.Position.X = x + widthOffset;
                    vertex6_s.Position.Y = y + h06;
                    vertex6_s.Position.Z = 0;
                    vertex6_s.TextureCoordinate.X = 0;
                    vertex6_s.TextureCoordinate.Y = h6mod;
                    vertex6_s.TextureCoordinate.Z = 0;

                    vertex7_s.Position.X = x + SITTING_OFFSET;
                    vertex7_s.Position.Y = y + h06;
                    vertex7_s.Position.Z = 0;
                    vertex7_s.TextureCoordinate.X = 1;
                    vertex7_s.TextureCoordinate.Y = h6mod;
                    vertex7_s.TextureCoordinate.Z = 0;


                    vertex4_s.Normal.X = 0;
                    vertex4_s.Normal.Y = 0;
                    vertex4_s.Normal.Z = 1;
                    vertex5_s.Normal.X = 0;
                    vertex5_s.Normal.Y = 0;
                    vertex5_s.Normal.Z = 1;
                    vertex6_s.Normal.X = 0;
                    vertex6_s.Normal.Y = 0;
                    vertex6_s.Normal.Z = 1;
                    vertex7_s.Normal.X = 0;
                    vertex7_s.Normal.Y = 0;
                    vertex7_s.Normal.Z = 1;
                    vertex4_s.Hue = vertex5_s.Hue = vertex6_s.Hue = vertex7_s.Hue = hue;


                    //vertex4_s.Position.X = x + widthOffset;
                    //vertex4_s.Position.Y = y + h06;
                    //vertex4_s.Position.Z = 0;
                    //vertex4_s.TextureCoordinate.X = 0;
                    //vertex4_s.TextureCoordinate.Y = h6mod;
                    //vertex4_s.TextureCoordinate.Z = 0;

                    //vertex5_s.Position.X = x + SITTING_OFFSET;
                    //vertex5_s.Position.Y = y + h06;
                    //vertex5_s.Position.Z = 0;
                    //vertex5_s.TextureCoordinate.X = 1;
                    //vertex5_s.TextureCoordinate.Y = h6mod;
                    //vertex5_s.TextureCoordinate.Z = 0;

                    //vertex4_s.Normal.X = vertex4_s.Normal.Y = vertex5_s.Normal.X = vertex5_s.Normal.Y = 0;
                    //vertex4_s.Normal.Z = vertex5_s.Normal.Z = 1;
                    //vertex4_s.Hue = vertex5_s.Hue = new Vector3(51, 1, 0);
                }

                if (h9mod != 0.0f)
                {
                    //if (h6mod == 0.0f)
                    //{
                    //    if (idx + count + 2 >= MAX_SPRITES)
                    //        Flush();

                    //    ref var vertex6 = ref VertexInfo[idx + count++];
                    //    ref var vertex7 = ref VertexInfo[idx + count++];

                    //    vertex6.Position.X = x + widthOffset;
                    //    vertex6.Position.Y = y;
                    //    vertex6.Position.Z = 0;
                    //    vertex6.TextureCoordinate.X = 0;
                    //    vertex6.TextureCoordinate.Y = 0;
                    //    vertex6.TextureCoordinate.Z = 0;

                    //    vertex7.Position.X = x + SITTING_OFFSET;
                    //    vertex7.Position.Y = y;
                    //    vertex7.Position.Z = 0;
                    //    vertex7.TextureCoordinate.X = 1;
                    //    vertex7.TextureCoordinate.Y = 0;
                    //    vertex7.TextureCoordinate.Z = 0;

                    //    vertex6.Normal.X = vertex6.Normal.Y = vertex7.Normal.X = vertex7.Normal.Y = 0;
                    //    vertex6.Normal.Z = vertex7.Normal.Z = 1;
                    //    vertex6.Hue = vertex7.Hue = hue;
                    //}

                    if (_numSprites + 4 >= MAX_SPRITES)
                    {
                        idx = 0;
                        Flush();
                    }

                    ref var vertex6_s = ref _vertexInfo[idx + count++];
                    ref var vertex7_s = ref _vertexInfo[idx + count++];
                    ref var vertex8_s = ref _vertexInfo[idx + count++];
                    ref var vertex9_s = ref _vertexInfo[idx + count++];


                    vertex6_s.Position.X = x + widthOffset;
                    vertex6_s.Position.Y = y + h06;
                    vertex6_s.Position.Z = 0;
                    vertex6_s.TextureCoordinate.X = 0;
                    vertex6_s.TextureCoordinate.Y = h6mod;
                    vertex6_s.TextureCoordinate.Z = 0;

                    vertex7_s.Position.X = x + SITTING_OFFSET;
                    vertex7_s.Position.Y = y + h06;
                    vertex7_s.Position.Z = 0;
                    vertex7_s.TextureCoordinate.X = 1;
                    vertex7_s.TextureCoordinate.Y = h6mod;
                    vertex7_s.TextureCoordinate.Z = 0;

                    vertex8_s.Position.X = x + widthOffset;
                    vertex8_s.Position.Y = y + h09;
                    vertex8_s.Position.Z = 0;
                    vertex8_s.TextureCoordinate.X = 0;
                    vertex8_s.TextureCoordinate.Y = 1;
                    vertex8_s.TextureCoordinate.Z = 0;

                    vertex9_s.Position.X = x + SITTING_OFFSET;
                    vertex9_s.Position.Y = y + h09;
                    vertex9_s.Position.Z = 0;
                    vertex9_s.TextureCoordinate.X = 1;
                    vertex9_s.TextureCoordinate.Y = 1;
                    vertex9_s.TextureCoordinate.Z = 0;


                    vertex6_s.Normal.X = 0;
                    vertex6_s.Normal.Y = 0;
                    vertex6_s.Normal.Z = 1;
                    vertex7_s.Normal.X = 0;
                    vertex7_s.Normal.Y = 0;
                    vertex7_s.Normal.Z = 1;
                    vertex8_s.Normal.X = 0;
                    vertex8_s.Normal.Y = 0;
                    vertex8_s.Normal.Z = 1;
                    vertex9_s.Normal.X = 0;
                    vertex9_s.Normal.Y = 0;
                    vertex9_s.Normal.Z = 1;
                    vertex6_s.Hue = vertex7_s.Hue = vertex8_s.Hue = vertex9_s.Hue = hue;



                    //vertex6_s.Position.X = x + widthOffset;
                    //vertex6_s.Position.Y = y + h09;
                    //vertex6_s.Position.Z = 0;
                    //vertex6_s.TextureCoordinate.X = 0;
                    //vertex6_s.TextureCoordinate.Y = 1;
                    //vertex6_s.TextureCoordinate.Z = 0;

                    //vertex7_s.Position.X = x + SITTING_OFFSET;
                    //vertex7_s.Position.Y = y + h09;
                    //vertex7_s.Position.Z = 0;
                    //vertex7_s.TextureCoordinate.X = 1;
                    //vertex7_s.TextureCoordinate.Y = 1;
                    //vertex7_s.TextureCoordinate.Z = 0;

                    //vertex6_s.Normal.X = vertex6_s.Normal.Y = vertex7_s.Normal.X = vertex7_s.Normal.Y = 0;
                    //vertex6_s.Normal.Z = vertex7_s.Normal.Z = 1;
                    //vertex6_s.Hue = vertex7_s.Hue = hue;
                }
            }
            else
            {
                if (h3mod != 0.0f)
                {
                    if (_numSprites + 4 >= MAX_SPRITES)
                    {
                        idx = 0;
                        Flush();
                    }

                    ref var vertex0 = ref _vertexInfo[idx + count++];
                    ref var vertex1 = ref _vertexInfo[idx + count++];
                    ref var vertex2 = ref _vertexInfo[idx + count++];
                    ref var vertex3 = ref _vertexInfo[idx + count++];

                    vertex0.Position.X = x + SITTING_OFFSET;
                    vertex0.Position.Y = y;
                    vertex0.Position.Z = 0;
                    vertex0.TextureCoordinate.X = 0;
                    vertex0.TextureCoordinate.Y = 0;
                    vertex0.TextureCoordinate.Z = 0;

                    vertex1.Position.X = x + widthOffset;
                    vertex1.Position.Y = y;
                    vertex1.Position.Z = 0;
                    vertex1.TextureCoordinate.X = 1;
                    vertex1.TextureCoordinate.Y = 0;
                    vertex1.TextureCoordinate.Z = 0;

                    vertex2.Position.X = x + SITTING_OFFSET;
                    vertex2.Position.Y = y + h03;
                    vertex2.Position.Z = 0;
                    vertex2.TextureCoordinate.X = 0;
                    vertex2.TextureCoordinate.Y = h3mod;
                    vertex2.TextureCoordinate.Z = 0;

                    vertex3.Position.X = x + widthOffset;
                    vertex3.Position.Y = y + h03;
                    vertex3.Position.Z = 0;
                    vertex3.TextureCoordinate.X = 1;
                    vertex3.TextureCoordinate.Y = h3mod;
                    vertex3.TextureCoordinate.Z = 0;



                    vertex0.Normal.X = 0;
                    vertex0.Normal.Y = 0;
                    vertex0.Normal.Z = 1;
                    vertex1.Normal.X = 0;
                    vertex1.Normal.Y = 0;
                    vertex1.Normal.Z = 1;
                    vertex2.Normal.X = 0;
                    vertex2.Normal.Y = 0;
                    vertex2.Normal.Z = 1;
                    vertex3.Normal.X = 0;
                    vertex3.Normal.Y = 0;
                    vertex3.Normal.Z = 1;

                    vertex0.Hue = vertex1.Hue = vertex2.Hue = vertex3.Hue = hue;

                }

                if (h6mod != 0.0f)
                {
                    if (h3mod == 0.0f)
                    {

                    }

                    if (_numSprites + 4 >= MAX_SPRITES)
                    {
                        idx = 0;
                        Flush();
                    }

                    ref var vertex4_s = ref _vertexInfo[idx + count++];
                    ref var vertex5_s = ref _vertexInfo[idx + count++];
                    ref var vertex6_s = ref _vertexInfo[idx + count++];
                    ref var vertex7_s = ref _vertexInfo[idx + count++];


                    vertex4_s.Position.X = x + SITTING_OFFSET;
                    vertex4_s.Position.Y = y + h03;
                    vertex4_s.Position.Z = 0;
                    vertex4_s.TextureCoordinate.X = 0;
                    vertex4_s.TextureCoordinate.Y = h3mod;
                    vertex4_s.TextureCoordinate.Z = 0;

                    vertex5_s.Position.X = x + widthOffset;
                    vertex5_s.Position.Y = y + h03;
                    vertex5_s.Position.Z = 0;
                    vertex5_s.TextureCoordinate.X = 1;
                    vertex5_s.TextureCoordinate.Y = h3mod;
                    vertex5_s.TextureCoordinate.Z = 0;

                    vertex6_s.Position.X = x;
                    vertex6_s.Position.Y = y + h06;
                    vertex6_s.Position.Z = 0;
                    vertex6_s.TextureCoordinate.X = 0;
                    vertex6_s.TextureCoordinate.Y = h6mod;
                    vertex6_s.TextureCoordinate.Z = 0;

                    vertex7_s.Position.X = x + width;
                    vertex7_s.Position.Y = y + h06;
                    vertex7_s.Position.Z = 0;
                    vertex7_s.TextureCoordinate.X = 1;
                    vertex7_s.TextureCoordinate.Y = h6mod;
                    vertex7_s.TextureCoordinate.Z = 0;


                    vertex4_s.Normal.X = 0;
                    vertex4_s.Normal.Y = 0;
                    vertex4_s.Normal.Z = 1;
                    vertex5_s.Normal.X = 0;
                    vertex5_s.Normal.Y = 0;
                    vertex5_s.Normal.Z = 1;
                    vertex6_s.Normal.X = 0;
                    vertex6_s.Normal.Y = 0;
                    vertex6_s.Normal.Z = 1;
                    vertex7_s.Normal.X = 0;
                    vertex7_s.Normal.Y = 0;
                    vertex7_s.Normal.Z = 1;
                    vertex4_s.Hue = vertex5_s.Hue = vertex6_s.Hue = vertex7_s.Hue = hue;
                }

                if (h9mod != 0.0f)
                {
                    if (h6mod == 0.0f)
                    {

                    }


                    if (_numSprites + 4 >= MAX_SPRITES)
                    {
                        idx = 0;
                        Flush();
                    }


                    ref var vertex6_s = ref _vertexInfo[idx + count++];
                    ref var vertex7_s = ref _vertexInfo[idx + count++];
                    ref var vertex8_s = ref _vertexInfo[idx + count++];
                    ref var vertex9_s = ref _vertexInfo[idx + count++];


                    vertex6_s.Position.X = x;
                    vertex6_s.Position.Y = y + h06;
                    vertex6_s.Position.Z = 0;
                    vertex6_s.TextureCoordinate.X = 0;
                    vertex6_s.TextureCoordinate.Y = h6mod;
                    vertex6_s.TextureCoordinate.Z = 0;

                    vertex7_s.Position.X = x + width;
                    vertex7_s.Position.Y = y + h06;
                    vertex7_s.Position.Z = 0;
                    vertex7_s.TextureCoordinate.X = 1;
                    vertex7_s.TextureCoordinate.Y = h6mod;
                    vertex7_s.TextureCoordinate.Z = 0;

                    vertex8_s.Position.X = x;
                    vertex8_s.Position.Y = y + h09;
                    vertex8_s.Position.Z = 0;
                    vertex8_s.TextureCoordinate.X = 0;
                    vertex8_s.TextureCoordinate.Y = 1;
                    vertex8_s.TextureCoordinate.Z = 0;

                    vertex9_s.Position.X = x + width;
                    vertex9_s.Position.Y = y + h09;
                    vertex9_s.Position.Z = 0;
                    vertex9_s.TextureCoordinate.X = 1;
                    vertex9_s.TextureCoordinate.Y = 1;
                    vertex9_s.TextureCoordinate.Z = 0;


                    vertex6_s.Normal.X = 0;
                    vertex6_s.Normal.Y = 0;
                    vertex6_s.Normal.Z = 1;
                    vertex7_s.Normal.X = 0;
                    vertex7_s.Normal.Y = 0;
                    vertex7_s.Normal.Z = 1;
                    vertex8_s.Normal.X = 0;
                    vertex8_s.Normal.Y = 0;
                    vertex8_s.Normal.Z = 1;
                    vertex9_s.Normal.X = 0;
                    vertex9_s.Normal.Y = 0;
                    vertex9_s.Normal.Z = 1;
                    vertex6_s.Hue = vertex7_s.Hue = vertex8_s.Hue = vertex9_s.Hue = hue;
                }
            }


            int v = count >> 2;

            for (int i = 0; i < v; i++)
            {
                if (!CheckInScreen(idx + (i << 2)))
                    return false;

                PushSprite(texture);
            }

            return true;
        }

        [MethodImpl(256)]
        public bool Draw2D(Texture2D texture, int x, int y, ref Vector3 hue)
        {
            EnsureSize();

            int idx = _numSprites << 2;
            ref var vertex0 = ref _vertexInfo[idx];
            ref var vertex1 = ref _vertexInfo[idx + 1];
            ref var vertex2 = ref _vertexInfo[idx + 2];
            ref var vertex3 = ref _vertexInfo[idx + 3];

            vertex0.Position.X = x;
            vertex0.Position.Y = y;
            vertex0.Position.Z = 0;
            vertex0.Normal.X = 0;
            vertex0.Normal.Y = 0;
            vertex0.Normal.Z = 1;
            vertex0.TextureCoordinate.X = 0;
            vertex0.TextureCoordinate.Y = 0;
            vertex0.TextureCoordinate.Z = 0;

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

        [MethodImpl(256)]
        public bool Draw2D(Texture2D texture, int x, int y, int sx, int sy, float swidth, float sheight, ref Vector3 hue)
        {
            EnsureSize();

            float minX = sx / (float) texture.Width;
            float maxX = (sx + swidth) / (float) texture.Width;
            float minY = sy / (float) texture.Height;
            float maxY = (sy + sheight) / (float) texture.Height;


            int idx = _numSprites << 2;
            ref var vertex0 = ref _vertexInfo[idx];
            ref var vertex1 = ref _vertexInfo[idx + 1];
            ref var vertex2 = ref _vertexInfo[idx + 2];
            ref var vertex3 = ref _vertexInfo[idx + 3];

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
        
        [MethodImpl(256)]
        public bool Draw2D(Texture2D texture, float dx, float dy, float dwidth, float dheight, int sx, int sy, float swidth, float sheight, ref Vector3 hue, float angle = 0.0f)
        {
            EnsureSize();

            float minX = sx / (float) texture.Width, maxX = (sx + swidth) / (float) texture.Width;
            float minY = sy / (float) texture.Height, maxY = (sy + sheight) / (float) texture.Height;

            int idx = _numSprites << 2;
            ref var vertex0 = ref _vertexInfo[idx];
            ref var vertex1 = ref _vertexInfo[idx + 1];
            ref var vertex2 = ref _vertexInfo[idx + 2];
            ref var vertex3 = ref _vertexInfo[idx + 3];


            float x = dx;
            float y = dy;
            float w = dx + dwidth;
            float h = dy + dheight;

            if (angle != 0.0f)
            {
                //angle = (float)(angle * 57.295780);
                angle = (float)(angle * Math.PI) / 180.0f;

                float ww = dwidth / 2f;
                float hh = dheight / 2f;

                float sin = (float)Math.Sin(angle);
                float cos = (float)Math.Cos(angle);

                //float sinx = sin * ww;
                //float cosx = cos * ww;
                //float siny = sin * hh;
                //float cosy = cos * hh;



                float tempX = -ww;
                float tempY = -hh;
                float rotX = tempX * cos - tempY * sin;
                float rotY = tempX * sin + tempY * cos;
                rotX += dx + ww;
                rotY += dy + hh;

                vertex0.Position.X = rotX;
                vertex0.Position.Y = rotY;




                tempX = dwidth - ww;
                tempY = -hh;
                rotX = tempX * cos - tempY * sin;
                rotY = tempX * sin + tempY * cos;
                rotX += dx + ww;
                rotY += dy + hh;

                vertex1.Position.X = rotX;
                vertex1.Position.Y = rotY;




                tempX = -ww;
                tempY = dheight - hh;
                rotX = tempX * cos - tempY * sin;
                rotY = tempX * sin + tempY * cos;
                rotX += dx + ww;
                rotY += dy + hh;

                vertex2.Position.X = rotX;
                vertex2.Position.Y = rotY;


                tempX = dwidth - ww;
                tempY = dheight - hh;
                rotX = tempX * cos - tempY * sin;
                rotY = tempX * sin + tempY * cos;
                rotX += dx + ww;
                rotY += dy + hh;

                vertex3.Position.X = rotX;
                vertex3.Position.Y = rotY;

            }
            else
            {
                vertex0.Position.X = x;
                vertex0.Position.Y = y;

                vertex1.Position.X = w;
                vertex1.Position.Y = y;

                vertex2.Position.X = x;
                vertex2.Position.Y = h;

                vertex3.Position.X = w;
                vertex3.Position.Y = h;
            }

            vertex0.Position.Z = 0;
            vertex0.Normal.X = 0;
            vertex0.Normal.Y = 0;
            vertex0.Normal.Z = 1;
            vertex0.TextureCoordinate.X = minX;
            vertex0.TextureCoordinate.Y = minY;
            vertex0.TextureCoordinate.Z = 0;
            vertex1.Position.Z = 0;
            vertex1.Normal.X = 0;
            vertex1.Normal.Y = 0;
            vertex1.Normal.Z = 1;
            vertex1.TextureCoordinate.X = maxX;
            vertex1.TextureCoordinate.Y = minY;
            vertex1.TextureCoordinate.Z = 0;
            vertex2.Position.Z = 0;
            vertex2.Normal.X = 0;
            vertex2.Normal.Y = 0;
            vertex2.Normal.Z = 1;
            vertex2.TextureCoordinate.X = minX;
            vertex2.TextureCoordinate.Y = maxY;
            vertex2.TextureCoordinate.Z = 0;
            vertex3.Position.Z = 0;
            vertex3.Normal.X = 0;
            vertex3.Normal.Y = 0;
            vertex3.Normal.Z = 1;
            vertex3.TextureCoordinate.X = maxX;
            vertex3.TextureCoordinate.Y = maxY;
            vertex3.TextureCoordinate.Z = 0;
            vertex0.Hue = vertex1.Hue = vertex2.Hue = vertex3.Hue = hue;



            //if (CheckInScreen(idx))
            {
                PushSprite(texture);

                return true;
            }

            return false;
        }

        [MethodImpl(256)]
        public bool Draw2D(Texture2D texture, int x, int y, float width, float height, ref Vector3 hue)
        {
            EnsureSize();

            int idx = _numSprites << 2;
            ref var vertex0 = ref _vertexInfo[idx];
            ref var vertex1 = ref _vertexInfo[idx + 1];
            ref var vertex2 = ref _vertexInfo[idx + 2];
            ref var vertex3 = ref _vertexInfo[idx + 3];

            vertex0.Position.X = x;
            vertex0.Position.Y = y;
            vertex0.Position.Z = 0;
            vertex0.Normal.X = 0;
            vertex0.Normal.Y = 0;
            vertex0.Normal.Z = 1;
            vertex0.TextureCoordinate.X = 0;
            vertex0.TextureCoordinate.Y = 0;
            vertex0.TextureCoordinate.Z = 0;
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

        [MethodImpl(256)]
        public bool Draw2DTiled(Texture2D texture, int dx, int dy, float dwidth, float dheight, ref Vector3 hue)
        {
            int y = dy;
            int h = (int) dheight;

            while (h > 0)
            {
                int x = dx;
                int w = (int) dwidth;

                int rw = texture.Width;
                int rh = h < texture.Height ? h : texture.Height;

                while (w > 0)
                {
                    if (w < texture.Width)
                        rw = w;
                    Draw2D(texture, x, y, 0, 0, rw, rh, ref hue);
                    w -= texture.Width;
                    x += texture.Width;
                }

                h -= texture.Height;
                y += texture.Height;
            }

            return true;
        }

        [MethodImpl(256)]
        public bool DrawRectangle(Texture2D texture, int x, int y, int width, int height, ref Vector3 hue)
        {
            Draw2D(texture, x, y, width, 1, ref hue);
            Draw2D(texture, x + width, y, 1, height + 1, ref hue);
            Draw2D(texture, x, y + height, width, 1, ref hue);
            Draw2D(texture, x, y, 1, height, ref hue);

            return true;
        }

        [MethodImpl(256)]
        public bool DrawLine(Texture2D texture, int startX, int startY, int endX, int endY, int originX, int originY)
        {
            EnsureSize();

            int idx = _numSprites << 2;
            ref var vertex0 = ref _vertexInfo[idx];
            ref var vertex1 = ref _vertexInfo[idx + 1];
            ref var vertex2 = ref _vertexInfo[idx + 2];
            ref var vertex3 = ref _vertexInfo[idx + 3];


            const int WIDTH = 1;
            Vector2 begin = new Vector2(startX, startY);
            Vector2 end = new Vector2(endX, endY);

            Rectangle r = new Rectangle((int)begin.X, (int)begin.Y, (int)(end - begin).Length() + WIDTH, WIDTH);

            float angle = (float)(Math.Atan2(end.Y - begin.Y, end.X - begin.X) * 57.295780);
            angle = -(float)(angle * Math.PI) / 180.0f;


            float ww = r.Width / 2f;
            float hh = r.Height / 2f;


            float rotSin = (float) Math.Sin(angle);
            float rotCos = (float) Math.Cos(angle);


            float sinx = rotSin * ww;
            float cosx = rotCos * ww;
            float siny = rotSin * hh;
            float cosy = rotCos * hh;


            vertex0.Position.X = originX;
            vertex0.Position.Y = originY;
            vertex0.Position.X += cosx - -siny;
            vertex0.Position.Y -= sinx + -cosy;
            vertex0.Normal.X = 0;
            vertex0.Normal.Y = 0;
            vertex0.Normal.Z = 1;
            vertex0.TextureCoordinate.X = 0;
            vertex0.TextureCoordinate.Y = 0;
            vertex0.TextureCoordinate.Z = 0;

            vertex1.Position.X = originX;
            vertex1.Position.Y = originY;
            vertex1.Position.X += cosx - siny;
            vertex1.Position.Y += -sinx + -cosy;
            vertex1.Normal.X = 0;
            vertex1.Normal.Y = 0;
            vertex1.Normal.Z = 1;
            vertex1.TextureCoordinate.X = 0;
            vertex1.TextureCoordinate.Y = 1;
            vertex1.TextureCoordinate.Z = 0;

            vertex2.Position.X = originX;
            vertex2.Position.Y = originY;
            vertex2.Position.X += -cosx - -siny;
            vertex2.Position.Y += sinx + cosy;
            vertex2.Normal.X = 0;
            vertex2.Normal.Y = 0;
            vertex2.Normal.Z = 1;
            vertex2.TextureCoordinate.X = 1;
            vertex2.TextureCoordinate.Y = 0;
            vertex2.TextureCoordinate.Z = 0;

            vertex3.Position.X = originX;
            vertex3.Position.Y = originY;
            vertex3.Position.X += -cosx - siny;
            vertex3.Position.Y += sinx + -cosy;
            vertex3.Normal.X = 0;
            vertex3.Normal.Y = 0;
            vertex3.Normal.Z = 1;
            vertex3.TextureCoordinate.X = 1;
            vertex3.TextureCoordinate.Y = 1;
            vertex3.TextureCoordinate.Z = 0;


            vertex0.Hue =
                vertex1.Hue =
                    vertex2.Hue =
                        vertex3.Hue = Vector3.Zero;

            if (CheckInScreen(idx))
            {
                PushSprite(texture);

                return true;
            }

            return false;
        }



        [MethodImpl(256)]
        private bool CheckInScreen(int index)
        {
            for (byte i = 0; i < 4; i++)
            {
                _drawingArea.Contains(ref _vertexInfo[index + i].Position, out ContainmentType res);
                if (res == ContainmentType.Contains)
                    return true;
            }

            return false;
        }

        [MethodImpl(256)]
        public void Begin()
        {
            Begin(null, Matrix.Identity);
        }

        [MethodImpl(256)]
        public void Begin(Effect effect)
        {
            Begin(effect, Matrix.Identity);
        }

        [MethodImpl(256)]
        public void Begin(Effect customEffect, Matrix projection)
        {
            EnsureNotStarted();
            _started = true;

            _drawingArea.Min.X = 0;
            _drawingArea.Min.Y = 0;
            _drawingArea.Min.Z = -150;
            _drawingArea.Max.X = GraphicsDevice.Viewport.Width;
            _drawingArea.Max.Y = GraphicsDevice.Viewport.Height;
            _drawingArea.Max.Z = 150;

            _customEffect = customEffect;
        }

        [MethodImpl(256)]
        public void End()
        {
            EnsureStarted();
            Flush();
            _started = false;
            _customEffect = null;
        }


        [MethodImpl(256)]
        private void EnsureSize()
        {
            EnsureStarted();

            if (_numSprites >= MAX_SPRITES)
                Flush();
        }

        [MethodImpl(256)]
        private bool PushSprite(Texture2D texture)
        {
            EnsureSize();
            _textureInfo[_numSprites++] = texture;

            return true;
        }

        [Conditional("DEBUG")]
        private void EnsureStarted()
        {
            if (!_started)
                throw new InvalidOperationException();
        }

        [Conditional("DEBUG")]
        private void EnsureNotStarted()
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



            DefaultEffect.ApplyStates();
        }

        private void Flush()
        {
            ApplyStates();

            if (_numSprites == 0)
                return;

            _vertexBuffer.SetDataPointerEXT(0, _handle.AddrOfPinnedObject(), PositionNormalTextureColor.SIZE_IN_BYTES * (_numSprites << 2), SetDataOptions.None);

            Texture2D current = _textureInfo[0];
            int offset = 0;

            if (_customEffect != null)
            {
                if (_customEffect is MatrixEffect eff)
                    eff.ApplyStates();
                else
                    _customEffect.CurrentTechnique.Passes[0].Apply();
            }


            for (int i = 1; i < _numSprites; i++)
            {
                if (_textureInfo[i] != current)
                {
                    InternalDraw(current, offset, i - offset);
                    current = _textureInfo[i];
                    offset = i;
                }
            }

            InternalDraw(current, offset, _numSprites - offset);

            _numSprites = 0;
        }

        [MethodImpl(256)]
        private void InternalDraw(Texture2D texture, int baseSprite, int batchSize)
        {
            GraphicsDevice.Textures[0] = texture;
            GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, baseSprite << 2, 0, batchSize << 2, 0, batchSize << 1);
        }

        [MethodImpl(256)]
        public void EnableScissorTest(bool enable)
        {
            if (enable == _useScissor)
                return;

            if (!enable && _useScissor && ScissorStack.HasScissors)
                return;

            Flush();

            _useScissor = enable;
        }

        [MethodImpl(256)]
        public void SetBlendState(BlendState blend, bool noflush = false)
        {
            if (!noflush)
                Flush();

            _blendState = blend ?? BlendState.AlphaBlend;
        }

        [MethodImpl(256)]
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
                result[i] = (short)j;
                result[i + 1] = (short)(j + 1);
                result[i + 2] = (short)(j + 2);
                result[i + 3] = (short)(j + 1);
                result[i + 4] = (short)(j + 3);
                result[i + 5] = (short)(j + 2);
            }

            return result;
        }

        private class IsometricEffect : MatrixEffect
        {
            private Vector2 _viewPort;
            private Matrix _matrix = Matrix.Identity;

            public IsometricEffect(GraphicsDevice graphicsDevice) : base(graphicsDevice, Resources.IsometricEffect)
            {
                WorldMatrix = Parameters["WorldMatrix"];
                Viewport = Parameters["Viewport"];
                Brighlight = Parameters["Brightlight"];

                CurrentTechnique = Techniques["HueTechnique"];
            }

            protected IsometricEffect(Effect cloneSource) : base(cloneSource)
            {
            }


            public EffectParameter WorldMatrix { get; }
            public EffectParameter Viewport { get; }
            public EffectParameter Brighlight { get; }


            public override void ApplyStates()
            {
                WorldMatrix.SetValueRef(ref _matrix);

                _viewPort.X = GraphicsDevice.Viewport.Width;
                _viewPort.Y = GraphicsDevice.Viewport.Height;
                Viewport.SetValue(_viewPort);

                base.ApplyStates();
            }
        }

        public void Dispose()
        {
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
            _handle.Free();
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

        public const int SIZE_IN_BYTES = sizeof(float) * 12;

#if DEBUG
        public override string ToString()
        {
            return string.Format("VPNTH: <{0}> <{1}>", Position.ToString(), TextureCoordinate.ToString());
        }
#endif
    }
}