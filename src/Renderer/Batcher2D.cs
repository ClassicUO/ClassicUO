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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer
{
    internal sealed unsafe class UltimaBatcher2D : IDisposable
    {
        private static readonly float[] _cornerOffsetX = new float[] { 0.0f, 1.0f, 0.0f, 1.0f };
        private static readonly float[] _cornerOffsetY = new float[] { 0.0f, 0.0f, 1.0f, 1.0f };

        private const int MAX_SPRITES = 0x800;
        private const int MAX_VERTICES = MAX_SPRITES * 4;
        private const int MAX_INDICES = MAX_SPRITES * 6;
        private BlendState _blendState;
        private int _currentBufferPosition;

        private Effect _customEffect;
        private BoundingBox _drawingArea;


        private readonly IndexBuffer _indexBuffer;
        private int _numSprites;
        private Matrix _projectionMatrix = new Matrix
        (
            0f, //(float)( 2.0 / (double)viewport.Width ) is the actual value we will use
            0.0f,
            0.0f,
            0.0f,
            0.0f,
            0f, //(float)( -2.0 / (double)viewport.Height ) is the actual value we will use
            0.0f,
            0.0f,
            0.0f,
            0.0f,
            1.0f,
            0.0f,
            -1.0f,
            1.0f,
            0.0f,
            1.0f
        );
        private readonly RasterizerState _rasterizerState;
        private SamplerState _sampler;
        private bool _started;
        private DepthStencilState _stencil;
        private readonly Texture2D[] _textureInfo;
        private Matrix _transformMatrix;
        private readonly DynamicVertexBuffer _vertexBuffer;
        private PositionNormalTextureColor4* _vertexInfo;


        public UltimaBatcher2D(GraphicsDevice device)
        {
            GraphicsDevice = device;
            _textureInfo = new Texture2D[MAX_SPRITES];

            _vertexInfo = (PositionNormalTextureColor4*) Marshal.AllocHGlobal(sizeof(PositionNormalTextureColor4) * MAX_SPRITES);

            _vertexBuffer = new DynamicVertexBuffer(GraphicsDevice, typeof(PositionNormalTextureColor4), MAX_VERTICES, BufferUsage.WriteOnly);

            _indexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, MAX_INDICES, BufferUsage.WriteOnly);

            _indexBuffer.SetData(GenerateIndexArray());
            _blendState = BlendState.AlphaBlend;
            _rasterizerState = RasterizerState.CullNone;
            _sampler = SamplerState.PointClamp;

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
        }


        public Matrix TransformMatrix => _transformMatrix;

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

        public void Dispose()
        {
            if (_vertexInfo != null)
            {
                Marshal.FreeHGlobal((IntPtr) _vertexInfo);
                _vertexInfo = null;
            }

            DefaultEffect?.Dispose();
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
        }


        public void SetBrightlight(float f)
        {
            ((IsometricEffect) DefaultEffect).Brighlight.SetValue(f);
        }

        public void DrawString(SpriteFont spriteFont, string text, int x, int y, ref Vector3 color)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

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
                if (c == '\r')
                {
                    continue;
                }

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
                        index = characterMap.IndexOf('?');
                        //throw new ArgumentException(
                        //                            "Text contains characters that cannot be" +
                        //                            " resolved by this SpriteFont.",
                        //                            "text"
                        //                           );
                    }
                    else
                    {
                        index = characterMap.IndexOf(spriteFont.DefaultCharacter.Value);
                    }
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
                {
                    curOffset.X += spriteFont.Spacing + cKern.X;
                }

                // Calculate the character origin
                Rectangle cCrop = croppingData[index];
                Rectangle cGlyph = glyphData[index];

                float offsetX = baseOffset.X + (curOffset.X + cCrop.X) * axisDirX;

                float offsetY = baseOffset.Y + (curOffset.Y + cCrop.Y) * axisDirY;

                Draw2D
                (
                    textureValue,
                    x + (int) Math.Round(offsetX),
                    y + (int) Math.Round(offsetY),
                    cGlyph.X,
                    cGlyph.Y,
                    cGlyph.Width,
                    cGlyph.Height,
                    ref color
                );

                curOffset.X += cKern.Y + cKern.Z;
            }
        }


        /*public void DrawString(BaseUOFont font, string text, int x, int y, ref Vector3 hue)
        {
            int startX = x;

            foreach (char c in text)
            {
                if (c == '\n')
                {
                    x = startX;
                    y += font.MaxHeight;

                    continue;
                }

                Rectangle rect = font.GetCharBounds(c);
                Texture2D texture = font.GetTextureByChar(c);

                Draw2D
                (
                    texture,
                    x,
                    y,
                    rect.X,
                    rect.Y,
                    rect.Width,
                    rect.Height,
                    ref hue
                );

                x += rect.Width;
            }
        }
        */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool DrawSprite(Texture2D texture, int x, int y, bool mirror, ref Vector3 hue)
        {
            EnsureSize();

            ref PositionNormalTextureColor4 vertex = ref _vertexInfo[_numSprites];

            int w = texture.Width;
            int h = texture.Height;

            if (mirror)
            {
                vertex.Position0.X = x + w;
                vertex.Position0.Y = y + h;
                vertex.Position0.Z = 0;
                vertex.Normal0.X = 0;
                vertex.Normal0.Y = 0;
                vertex.Normal0.Z = 1;
                vertex.TextureCoordinate0.X = 0;
                vertex.TextureCoordinate0.Y = 1;
                vertex.TextureCoordinate0.Z = 0;

                vertex.Position1.X = x;
                vertex.Position1.Y = y + h;
                vertex.Position0.Z = 0;
                vertex.Normal1.X = 0;
                vertex.Normal1.Y = 0;
                vertex.Normal1.Z = 1;
                vertex.TextureCoordinate1.X = 1;
                vertex.TextureCoordinate1.Y = 1;
                vertex.TextureCoordinate1.Z = 0;

                vertex.Position2.X = x + w;
                vertex.Position2.Y = y;
                vertex.Position2.Z = 0;
                vertex.Normal2.X = 0;
                vertex.Normal2.Y = 0;
                vertex.Normal2.Z = 1;
                vertex.TextureCoordinate2.X = 0;
                vertex.TextureCoordinate2.Y = 0;
                vertex.TextureCoordinate2.Z = 0;

                vertex.Position3.X = x;
                vertex.Position3.Y = y;
                vertex.Position3.Z = 0;
                vertex.Normal3.X = 0;
                vertex.Normal3.Y = 0;
                vertex.Normal3.Z = 1;
                vertex.TextureCoordinate3.X = 1;
                vertex.TextureCoordinate3.Y = 0;
                vertex.TextureCoordinate3.Z = 0;
            }
            else
            {
                vertex.Position0.X = x;
                vertex.Position0.Y = y + h;
                vertex.Position0.Z = 0;
                vertex.Normal0.X = 0;
                vertex.Normal0.Y = 0;
                vertex.Normal0.Z = 1;
                vertex.TextureCoordinate0.X = 0;
                vertex.TextureCoordinate0.Y = 1;
                vertex.TextureCoordinate0.Z = 0;

                vertex.Position1.X = x + w;
                vertex.Position1.Y = y + h;
                vertex.Position1.Z = 0;
                vertex.Normal1.X = 0;
                vertex.Normal1.Y = 0;
                vertex.Normal1.Z = 1;
                vertex.TextureCoordinate1.X = 1;
                vertex.TextureCoordinate1.Y = 1;
                vertex.TextureCoordinate1.Z = 0;

                vertex.Position2.X = x;
                vertex.Position2.Y = y;
                vertex.Position2.Z = 0;
                vertex.Normal2.X = 0;
                vertex.Normal2.Y = 0;
                vertex.Normal2.Z = 1;
                vertex.TextureCoordinate2.X = 0;
                vertex.TextureCoordinate2.Y = 0;
                vertex.TextureCoordinate2.Z = 0;

                vertex.Position3.X = x + w;
                vertex.Position3.Y = y;
                vertex.Position3.Z = 0;
                vertex.Normal3.X = 0;
                vertex.Normal3.Y = 0;
                vertex.Normal3.Z = 1;
                vertex.TextureCoordinate3.X = 1;
                vertex.TextureCoordinate3.Y = 0;
                vertex.TextureCoordinate3.Z = 0;
            }

            vertex.Hue0 = vertex.Hue1 = vertex.Hue2 = vertex.Hue3 = hue;

            PushSprite(texture);

            return true;
        }

        public struct YOffsets
        {
            public int Top;
            public int Right;
            public int Left;
            public int Bottom;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool DrawSpriteLand
        (
            Texture2D texture,
            int x,
            int y,
            ref YOffsets yOffsets,
            ref Vector3 normalTop,
            ref Vector3 normalRight,
            ref Vector3 normalLeft,
            ref Vector3 normalBottom,
            ref Vector3 hue
        )
        {
            EnsureSize();

            ref PositionNormalTextureColor4 vertex = ref _vertexInfo[_numSprites];

            vertex.TextureCoordinate0.X = 0;
            vertex.TextureCoordinate0.Y = 0;
            vertex.TextureCoordinate0.Z = 0;

            vertex.TextureCoordinate1.X = 1;
            vertex.TextureCoordinate1.Y = vertex.TextureCoordinate1.Z = 0;

            vertex.TextureCoordinate2.X = vertex.TextureCoordinate2.Z = 0;
            vertex.TextureCoordinate2.Y = 1;

            vertex.TextureCoordinate3.X = vertex.TextureCoordinate3.Y = 1;
            vertex.TextureCoordinate3.Z = 0;

            vertex.Normal0 = normalTop;
            vertex.Normal1 = normalRight;
            vertex.Normal2 = normalLeft;
            vertex.Normal3 = normalBottom;

            // Top
            vertex.Position0.X = x + 22;
            vertex.Position0.Y = y - yOffsets.Top;
            vertex.Position0.Z = 0;

            // Right
            vertex.Position1.X = x + 44;
            vertex.Position1.Y = y + (22 - yOffsets.Right);
            vertex.Position1.Z = 0;

            // Left
            vertex.Position2.X = x;
            vertex.Position2.Y = y + (22 - yOffsets.Left);
            vertex.Position2.Z = 0;

            // Bottom
            vertex.Position3.X = x + 22;
            vertex.Position3.Y = y + (44 - yOffsets.Bottom);
            vertex.Position3.Z = 0;

            vertex.Hue0 = vertex.Hue1 = vertex.Hue2 = vertex.Hue3 = hue;

            PushSprite(texture);

            return true;
        }

        

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool DrawSpriteRotated
        (
            Texture2D texture,
            int x,
            int y,
            float width,
            float height,
            ref Vector3 hue,
            float angle
        )
        {
            EnsureSize();

            ref PositionNormalTextureColor4 vertex = ref _vertexInfo[_numSprites];


            float sin = (float)Math.Sin(angle);
            float cos = (float)Math.Cos(angle);

            // Rotation Calculations
            float rotationMatrix1X = cos;
            float rotationMatrix1Y = sin;
            float rotationMatrix2X = -sin;
            float rotationMatrix2Y = cos;

            
            var cornerX = (_cornerOffsetX[0] - 0) * width;
            var cornerY = (_cornerOffsetY[0] - 0) * height;
            vertex.Position0.X = x + rotationMatrix2X * cornerY + rotationMatrix1X * cornerX;
            vertex.Position0.Y = y + rotationMatrix2Y * cornerY + rotationMatrix1Y * cornerX;
            vertex.Normal0.X = 0;
            vertex.Normal0.Y = 0;
            vertex.Normal0.Z = 1;
            vertex.TextureCoordinate0.X = _cornerOffsetX[0];
            vertex.TextureCoordinate0.Y = _cornerOffsetY[0];
            vertex.TextureCoordinate0.Z = 0;


            cornerX = (_cornerOffsetX[1] - 0) * width;
            cornerY = (_cornerOffsetY[1] - 0) * height;
            vertex.Position1.X = x + rotationMatrix2X * cornerY + rotationMatrix1X * cornerX;
            vertex.Position1.Y = y + rotationMatrix2Y * cornerY + rotationMatrix1Y * cornerX;
            vertex.Normal1.X = 0;
            vertex.Normal1.Y = 0;
            vertex.Normal1.Z = 1;
            vertex.TextureCoordinate1.X = _cornerOffsetX[1];
            vertex.TextureCoordinate1.Y = _cornerOffsetY[1];
            vertex.TextureCoordinate1.Z = 0;


            cornerX = (_cornerOffsetX[2] - 0) * width;
            cornerY = (_cornerOffsetY[2] - 0) * height;
            vertex.Position2.X = x + rotationMatrix2X * cornerY + rotationMatrix1X * cornerX;
            vertex.Position2.Y = y + rotationMatrix2Y * cornerY + rotationMatrix1Y * cornerX;
            vertex.Normal2.X = 0;
            vertex.Normal2.Y = 0;
            vertex.Normal2.Z = 1;
            vertex.TextureCoordinate2.X = _cornerOffsetX[2];
            vertex.TextureCoordinate2.Y = _cornerOffsetY[2];
            vertex.TextureCoordinate2.Z = 0;


            cornerX = (_cornerOffsetX[3] - 0) * width;
            cornerY = (_cornerOffsetY[3] - 0) * height;
            vertex.Position3.X = x + rotationMatrix2X * cornerY + rotationMatrix1X * cornerX;
            vertex.Position3.Y = y + rotationMatrix2Y * cornerY + rotationMatrix1Y * cornerX;
            vertex.Normal3.X = 0;
            vertex.Normal3.Y = 0;
            vertex.Normal3.Z = 1;
            vertex.TextureCoordinate3.X = _cornerOffsetX[3];
            vertex.TextureCoordinate3.Y = _cornerOffsetY[3];
            vertex.TextureCoordinate3.Z = 0;


            vertex.Hue0 = vertex.Hue1 = vertex.Hue2 = vertex.Hue3 = hue;

            PushSprite(texture);

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool DrawSpriteShadow(Texture2D texture, int x, int y, bool flip)
        {
            EnsureSize();

            ref PositionNormalTextureColor4 vertex = ref _vertexInfo[_numSprites];

            float width = texture.Width;
            float height = texture.Height * 0.5f;

            float translatedY = y + height * 0.75f;

            float ratio = height / width;

            if (flip)
            {
                vertex.Position0.X = x + width;
                vertex.Position0.Y = translatedY + height;
                vertex.Position0.Z = 0;
                vertex.Normal0.X = 0;
                vertex.Normal0.Y = 0;
                vertex.Normal0.Z = 1;
                vertex.TextureCoordinate0.X = 0;
                vertex.TextureCoordinate0.Y = 1;
                vertex.TextureCoordinate0.Z = 0;

                vertex.Position1.X = x;
                vertex.Position1.Y = translatedY + height;
                vertex.Normal1.X = 0;
                vertex.Normal1.Y = 0;
                vertex.Normal1.Z = 1;
                vertex.TextureCoordinate1.X = 1;
                vertex.TextureCoordinate1.Y = 1;
                vertex.TextureCoordinate1.Z = 0;

                vertex.Position2.X = x + width * (ratio + 1f);
                vertex.Position2.Y = translatedY;
                vertex.Normal2.X = 0;
                vertex.Normal2.Y = 0;
                vertex.Normal2.Z = 1;
                vertex.TextureCoordinate2.X = 0;
                vertex.TextureCoordinate2.Y = 0;
                vertex.TextureCoordinate2.Z = 0;

                vertex.Position3.X = x + width * ratio;
                vertex.Position3.Y = translatedY;
                vertex.Normal3.X = 0;
                vertex.Normal3.Y = 0;
                vertex.Normal3.Z = 1;
                vertex.TextureCoordinate3.X = 1;
                vertex.TextureCoordinate3.Y = 0;
                vertex.TextureCoordinate3.Z = 0;
            }
            else
            {
                vertex.Position0.X = x;
                vertex.Position0.Y = translatedY + height;
                vertex.Position0.Z = 0;
                vertex.Normal0.X = 0;
                vertex.Normal0.Y = 0;
                vertex.Normal0.Z = 1;
                vertex.TextureCoordinate0.X = 0;
                vertex.TextureCoordinate0.Y = 1;
                vertex.TextureCoordinate0.Z = 0;

                vertex.Position1.X = x + width;
                vertex.Position1.Y = translatedY + height;
                vertex.Normal1.X = 0;
                vertex.Normal1.Y = 0;
                vertex.Normal1.Z = 1;
                vertex.TextureCoordinate1.X = 1;
                vertex.TextureCoordinate1.Y = 1;
                vertex.TextureCoordinate1.Z = 0;

                vertex.Position2.X = x + width * ratio;
                vertex.Position2.Y = translatedY;
                vertex.Normal2.X = 0;
                vertex.Normal2.Y = 0;
                vertex.Normal2.Z = 1;
                vertex.TextureCoordinate2.X = 0;
                vertex.TextureCoordinate2.Y = 0;
                vertex.TextureCoordinate2.Z = 0;

                vertex.Position3.X = x + width * (ratio + 1f);
                vertex.Position3.Y = translatedY;
                vertex.Normal3.X = 0;
                vertex.Normal3.Y = 0;
                vertex.Normal3.Z = 1;
                vertex.TextureCoordinate3.X = 1;
                vertex.TextureCoordinate3.Y = 0;
                vertex.TextureCoordinate3.Z = 0;
            }

            vertex.Hue0.Z = vertex.Hue1.Z = vertex.Hue2.Z = vertex.Hue3.Z = vertex.Hue0.X = vertex.Hue1.X = vertex.Hue2.X = vertex.Hue3.X = 0;

            vertex.Hue0.Y = vertex.Hue1.Y = vertex.Hue2.Y = vertex.Hue3.Y = ShaderHueTranslator.SHADER_SHADOW;

            PushSprite(texture);

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool DrawCharacterSitted
        (
            Texture2D texture,
            int x,
            int y,
            bool mirror,
            float h3mod,
            float h6mod,
            float h9mod,
            ref Vector3 hue
        )
        {
            float width = texture.Width;
            float height = texture.Height;


            float h03 = height * h3mod;
            float h06 = height * h6mod;
            float h09 = height * h9mod;

            const float SITTING_OFFSET = 8.0f;

            float widthOffset = width + SITTING_OFFSET;


            if (mirror)
            {
                if (h3mod != 0.0f)
                {
                    EnsureSize();
                    ref PositionNormalTextureColor4 vertex = ref _vertexInfo[_numSprites];

                    vertex.Position0.X = x + width;
                    vertex.Position0.Y = y;
                    vertex.Position0.Z = 0;
                    vertex.TextureCoordinate0.X = 0;
                    vertex.TextureCoordinate0.Y = 0;
                    vertex.TextureCoordinate0.Z = 0;

                    vertex.Position1.X = x;
                    vertex.Position1.Y = y;
                    vertex.Position1.Z = 0;
                    vertex.TextureCoordinate1.X = 1;
                    vertex.TextureCoordinate1.Y = 0;
                    vertex.TextureCoordinate1.Z = 0;

                    vertex.Position2.X = x + width;
                    vertex.Position2.Y = y + h03;
                    vertex.Position2.Z = 0;
                    vertex.TextureCoordinate2.X = 0;
                    vertex.TextureCoordinate2.Y = h3mod;
                    vertex.TextureCoordinate2.Z = 0;

                    vertex.Position3.X = x;
                    vertex.Position3.Y = y + h03;
                    vertex.Position3.Z = 0;
                    vertex.TextureCoordinate3.X = 1;
                    vertex.TextureCoordinate3.Y = h3mod;
                    vertex.TextureCoordinate3.Z = 0;

                    vertex.Normal0.X = 0;
                    vertex.Normal0.Y = 0;
                    vertex.Normal0.Z = 1;
                    vertex.Normal1.X = 0;
                    vertex.Normal1.Y = 0;
                    vertex.Normal1.Z = 1;
                    vertex.Normal2.X = 0;
                    vertex.Normal2.Y = 0;
                    vertex.Normal2.Z = 1;
                    vertex.Normal3.X = 0;
                    vertex.Normal3.Y = 0;
                    vertex.Normal3.Z = 1;

                    vertex.Hue0 = vertex.Hue1 = vertex.Hue2 = vertex.Hue3 = hue;

                    PushSprite(texture);
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

                    //if (_numSprites + count >= MAX_SPRITES)
                    //{
                    //    Flush();
                    //}

                    EnsureSize();
                    ref PositionNormalTextureColor4 vertex = ref _vertexInfo[_numSprites];


                    vertex.Position0.X = x + width;
                    vertex.Position0.Y = y + h03;
                    vertex.Position0.Z = 0;
                    vertex.TextureCoordinate0.X = 0;
                    vertex.TextureCoordinate0.Y = h3mod;
                    vertex.TextureCoordinate0.Z = 0;

                    vertex.Position1.X = x;
                    vertex.Position1.Y = y + h03;
                    vertex.Position1.Z = 0;
                    vertex.TextureCoordinate1.X = 1;
                    vertex.TextureCoordinate1.Y = h3mod;
                    vertex.TextureCoordinate1.Z = 0;

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

                    vertex.Position2.X = x + widthOffset;
                    vertex.Position2.Y = y + h06;
                    vertex.Position2.Z = 0;
                    vertex.TextureCoordinate2.X = 0;
                    vertex.TextureCoordinate2.Y = h6mod;
                    vertex.TextureCoordinate2.Z = 0;

                    vertex.Position3.X = x + SITTING_OFFSET;
                    vertex.Position3.Y = y + h06;
                    vertex.Position3.Z = 0;
                    vertex.TextureCoordinate3.X = 1;
                    vertex.TextureCoordinate3.Y = h6mod;
                    vertex.TextureCoordinate3.Z = 0;


                    vertex.Normal0.X = 0;
                    vertex.Normal0.Y = 0;
                    vertex.Normal0.Z = 1;
                    vertex.Normal1.X = 0;
                    vertex.Normal1.Y = 0;
                    vertex.Normal1.Z = 1;
                    vertex.Normal2.X = 0;
                    vertex.Normal2.Y = 0;
                    vertex.Normal2.Z = 1;
                    vertex.Normal3.X = 0;
                    vertex.Normal3.Y = 0;
                    vertex.Normal3.Z = 1;

                    vertex.Hue0 = vertex.Hue1 = vertex.Hue2 = vertex.Hue3 = hue;

                    PushSprite(texture);
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

                    //if (_numSprites + count >= MAX_SPRITES)
                    //{
                    //    Flush();
                    //}

                    EnsureSize();
                    ref PositionNormalTextureColor4 vertex = ref _vertexInfo[_numSprites];

                    vertex.Position0.X = x + widthOffset;
                    vertex.Position0.Y = y + h06;
                    vertex.Position0.Z = 0;
                    vertex.TextureCoordinate0.X = 0;
                    vertex.TextureCoordinate0.Y = h6mod;
                    vertex.TextureCoordinate0.Z = 0;

                    vertex.Position1.X = x + SITTING_OFFSET;
                    vertex.Position1.Y = y + h06;
                    vertex.Position1.Z = 0;
                    vertex.TextureCoordinate1.X = 1;
                    vertex.TextureCoordinate1.Y = h6mod;
                    vertex.TextureCoordinate1.Z = 0;

                    vertex.Position2.X = x + widthOffset;
                    vertex.Position2.Y = y + h09;
                    vertex.Position2.Z = 0;
                    vertex.TextureCoordinate2.X = 0;
                    vertex.TextureCoordinate2.Y = 1;
                    vertex.TextureCoordinate2.Z = 0;

                    vertex.Position3.X = x + SITTING_OFFSET;
                    vertex.Position3.Y = y + h09;
                    vertex.Position3.Z = 0;
                    vertex.TextureCoordinate3.X = 1;
                    vertex.TextureCoordinate3.Y = 1;
                    vertex.TextureCoordinate3.Z = 0;


                    vertex.Normal0.X = 0;
                    vertex.Normal0.Y = 0;
                    vertex.Normal0.Z = 1;
                    vertex.Normal1.X = 0;
                    vertex.Normal1.Y = 0;
                    vertex.Normal1.Z = 1;
                    vertex.Normal2.X = 0;
                    vertex.Normal2.Y = 0;
                    vertex.Normal2.Z = 1;
                    vertex.Normal3.X = 0;
                    vertex.Normal3.Y = 0;
                    vertex.Normal3.Z = 1;

                    vertex.Hue0 = vertex.Hue1 = vertex.Hue2 = vertex.Hue3 = hue;


                    PushSprite(texture);
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
                    //if (_numSprites + count >= MAX_SPRITES)
                    //{
                    //    Flush();
                    //}

                    EnsureSize();
                    ref PositionNormalTextureColor4 vertex = ref _vertexInfo[_numSprites];

                    vertex.Position0.X = x + SITTING_OFFSET;
                    vertex.Position0.Y = y;
                    vertex.Position0.Z = 0;
                    vertex.TextureCoordinate0.X = 0;
                    vertex.TextureCoordinate0.Y = 0;
                    vertex.TextureCoordinate0.Z = 0;

                    vertex.Position1.X = x + widthOffset;
                    vertex.Position1.Y = y;
                    vertex.Position1.Z = 0;
                    vertex.TextureCoordinate1.X = 1;
                    vertex.TextureCoordinate1.Y = 0;
                    vertex.TextureCoordinate1.Z = 0;

                    vertex.Position2.X = x + SITTING_OFFSET;
                    vertex.Position2.Y = y + h03;
                    vertex.Position2.Z = 0;
                    vertex.TextureCoordinate2.X = 0;
                    vertex.TextureCoordinate2.Y = h3mod;
                    vertex.TextureCoordinate2.Z = 0;

                    vertex.Position3.X = x + widthOffset;
                    vertex.Position3.Y = y + h03;
                    vertex.Position3.Z = 0;
                    vertex.TextureCoordinate3.X = 1;
                    vertex.TextureCoordinate3.Y = h3mod;
                    vertex.TextureCoordinate3.Z = 0;

                    vertex.Normal0.X = 0;
                    vertex.Normal0.Y = 0;
                    vertex.Normal0.Z = 1;
                    vertex.Normal1.X = 0;
                    vertex.Normal1.Y = 0;
                    vertex.Normal1.Z = 1;
                    vertex.Normal2.X = 0;
                    vertex.Normal2.Y = 0;
                    vertex.Normal2.Z = 1;
                    vertex.Normal3.X = 0;
                    vertex.Normal3.Y = 0;
                    vertex.Normal3.Z = 1;

                    vertex.Hue0 = vertex.Hue1 = vertex.Hue2 = vertex.Hue3 = hue;

                    PushSprite(texture);
                }

                if (h6mod != 0.0f)
                {
                    if (h3mod == 0.0f)
                    {
                    }

                    //if (_numSprites + count >= MAX_SPRITES)
                    //{
                    //    Flush();
                    //}

                    EnsureSize();
                    ref PositionNormalTextureColor4 vertex = ref _vertexInfo[_numSprites];

                    vertex.Position0.X = x + SITTING_OFFSET;
                    vertex.Position0.Y = y + h03;
                    vertex.Position0.Z = 0;
                    vertex.TextureCoordinate0.X = 0;
                    vertex.TextureCoordinate0.Y = h3mod;
                    vertex.TextureCoordinate0.Z = 0;

                    vertex.Position1.X = x + widthOffset;
                    vertex.Position1.Y = y + h03;
                    vertex.Position1.Z = 0;
                    vertex.TextureCoordinate1.X = 1;
                    vertex.TextureCoordinate1.Y = h3mod;
                    vertex.TextureCoordinate1.Z = 0;

                    vertex.Position2.X = x;
                    vertex.Position2.Y = y + h06;
                    vertex.Position2.Z = 0;
                    vertex.TextureCoordinate2.X = 0;
                    vertex.TextureCoordinate2.Y = h6mod;
                    vertex.TextureCoordinate2.Z = 0;

                    vertex.Position3.X = x + width;
                    vertex.Position3.Y = y + h06;
                    vertex.Position3.Z = 0;
                    vertex.TextureCoordinate3.X = 1;
                    vertex.TextureCoordinate3.Y = h6mod;
                    vertex.TextureCoordinate3.Z = 0;

                    vertex.Normal0.X = 0;
                    vertex.Normal0.Y = 0;
                    vertex.Normal0.Z = 1;
                    vertex.Normal1.X = 0;
                    vertex.Normal1.Y = 0;
                    vertex.Normal1.Z = 1;
                    vertex.Normal2.X = 0;
                    vertex.Normal2.Y = 0;
                    vertex.Normal2.Z = 1;
                    vertex.Normal3.X = 0;
                    vertex.Normal3.Y = 0;
                    vertex.Normal3.Z = 1;

                    vertex.Hue0 = vertex.Hue1 = vertex.Hue2 = vertex.Hue3 = hue;

                    PushSprite(texture);
                }

                if (h9mod != 0.0f)
                {
                    if (h6mod == 0.0f)
                    {
                    }

                    //if (_numSprites + count >= MAX_SPRITES)
                    //{
                    //    Flush();
                    //}

                    EnsureSize();
                    ref PositionNormalTextureColor4 vertex = ref _vertexInfo[_numSprites];

                    vertex.Position0.X = x;
                    vertex.Position0.Y = y + h06;
                    vertex.Position0.Z = 0;
                    vertex.TextureCoordinate0.X = 0;
                    vertex.TextureCoordinate0.Y = h6mod;
                    vertex.TextureCoordinate0.Z = 0;

                    vertex.Position1.X = x + width;
                    vertex.Position1.Y = y + h06;
                    vertex.Position1.Z = 0;
                    vertex.TextureCoordinate1.X = 1;
                    vertex.TextureCoordinate1.Y = h6mod;
                    vertex.TextureCoordinate1.Z = 0;

                    vertex.Position2.X = x;
                    vertex.Position2.Y = y + h09;
                    vertex.Position2.Z = 0;
                    vertex.TextureCoordinate2.X = 0;
                    vertex.TextureCoordinate2.Y = 1;
                    vertex.TextureCoordinate2.Z = 0;

                    vertex.Position3.X = x + width;
                    vertex.Position3.Y = y + h09;
                    vertex.Position3.Z = 0;
                    vertex.TextureCoordinate3.X = 1;
                    vertex.TextureCoordinate3.Y = 1;
                    vertex.TextureCoordinate3.Z = 0;

                    vertex.Normal0.X = 0;
                    vertex.Normal0.Y = 0;
                    vertex.Normal0.Z = 1;
                    vertex.Normal1.X = 0;
                    vertex.Normal1.Y = 0;
                    vertex.Normal1.Z = 1;
                    vertex.Normal2.X = 0;
                    vertex.Normal2.Y = 0;
                    vertex.Normal2.Z = 1;
                    vertex.Normal3.X = 0;
                    vertex.Normal3.Y = 0;
                    vertex.Normal3.Z = 1;

                    vertex.Hue0 = vertex.Hue1 = vertex.Hue2 = vertex.Hue3 = hue;

                    PushSprite(texture);
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Draw2D(Texture2D texture, int x, int y, ref Vector3 hue)
        {
            EnsureSize();

            ref PositionNormalTextureColor4 vertex = ref _vertexInfo[_numSprites];

            vertex.Position0.X = x;
            vertex.Position0.Y = y;
            vertex.Position0.Z = 0;
            vertex.Normal0.X = 0;
            vertex.Normal0.Y = 0;
            vertex.Normal0.Z = 1;
            vertex.TextureCoordinate0.X = 0;
            vertex.TextureCoordinate0.Y = 0;
            vertex.TextureCoordinate0.Z = 0;

            vertex.Position1.X = x + texture.Width;
            vertex.Position1.Y = y;
            vertex.Position1.Z = 0;
            vertex.Normal1.X = 0;
            vertex.Normal1.Y = 0;
            vertex.Normal1.Z = 1;
            vertex.TextureCoordinate1.X = 1;
            vertex.TextureCoordinate1.Y = 0;
            vertex.TextureCoordinate1.Z = 0;

            vertex.Position2.X = x;
            vertex.Position2.Y = y + texture.Height;
            vertex.Position2.Z = 0;
            vertex.Normal2.X = 0;
            vertex.Normal2.Y = 0;
            vertex.Normal2.Z = 1;
            vertex.TextureCoordinate2.X = 0;
            vertex.TextureCoordinate2.Y = 1;
            vertex.TextureCoordinate2.Z = 0;

            vertex.Position3.X = x + texture.Width;
            vertex.Position3.Y = y + texture.Height;
            vertex.Position3.Z = 0;
            vertex.Normal3.X = 0;
            vertex.Normal3.Y = 0;
            vertex.Normal3.Z = 1;
            vertex.TextureCoordinate3.X = 1;
            vertex.TextureCoordinate3.Y = 1;
            vertex.TextureCoordinate3.Z = 0;

            vertex.Hue0 = vertex.Hue1 = vertex.Hue2 = vertex.Hue3 = hue;

            PushSprite(texture);

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Draw2D
        (
            Texture2D texture,
            int x,
            int y,
            int sx,
            int sy,
            float swidth,
            float sheight,
            ref Vector3 hue
        )
        {
            EnsureSize();

            float minX = sx / (float) texture.Width;
            float maxX = (sx + swidth) / texture.Width;
            float minY = sy / (float) texture.Height;
            float maxY = (sy + sheight) / texture.Height;

            ref PositionNormalTextureColor4 vertex = ref _vertexInfo[_numSprites];

            vertex.Position0.X = x;
            vertex.Position0.Y = y;
            vertex.Position0.Z = 0;
            vertex.Normal0.X = 0;
            vertex.Normal0.Y = 0;
            vertex.Normal0.Z = 1;
            vertex.TextureCoordinate0.X = minX;
            vertex.TextureCoordinate0.Y = minY;
            vertex.TextureCoordinate0.Z = 0;
            vertex.Position1.X = x + swidth;
            vertex.Position1.Y = y;
            vertex.Position1.Z = 0;
            vertex.Normal1.X = 0;
            vertex.Normal1.Y = 0;
            vertex.Normal1.Z = 1;
            vertex.TextureCoordinate1.X = maxX;
            vertex.TextureCoordinate1.Y = minY;
            vertex.TextureCoordinate1.Z = 0;
            vertex.Position2.X = x;
            vertex.Position2.Y = y + sheight;
            vertex.Position2.Z = 0;
            vertex.Normal2.X = 0;
            vertex.Normal2.Y = 0;
            vertex.Normal2.Z = 1;
            vertex.TextureCoordinate2.X = minX;
            vertex.TextureCoordinate2.Y = maxY;
            vertex.TextureCoordinate2.Z = 0;
            vertex.Position3.X = x + swidth;
            vertex.Position3.Y = y + sheight;
            vertex.Position3.Z = 0;
            vertex.Normal3.X = 0;
            vertex.Normal3.Y = 0;
            vertex.Normal3.Z = 1;
            vertex.TextureCoordinate3.X = maxX;
            vertex.TextureCoordinate3.Y = maxY;
            vertex.TextureCoordinate3.Z = 0;
            vertex.Hue0 = vertex.Hue1 = vertex.Hue2 = vertex.Hue3 = hue;

            PushSprite(texture);

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Draw2D
        (
            Texture2D texture,
            float dx,
            float dy,
            float dwidth,
            float dheight,
            float sx,
            float sy,
            float swidth,
            float sheight,
            ref Vector3 hue,
            float angle = 0.0f
        )
        {
            EnsureSize();

            float minX = sx / texture.Width, maxX = (sx + swidth) / texture.Width;
            float minY = sy / texture.Height, maxY = (sy + sheight) / texture.Height;

            ref PositionNormalTextureColor4 vertex = ref _vertexInfo[_numSprites];

            float x = dx;
            float y = dy;
            float w = dx + dwidth;
            float h = dy + dheight;

            if (angle != 0.0f)
            {
                //angle = (float)(angle * 57.295780);
                angle = (float) (angle * Math.PI) / 180.0f;

                float ww = dwidth * 0.5f;
                float hh = dheight * 0.5f;

                float sin = (float) Math.Sin(angle);
                float cos = (float) Math.Cos(angle);

                float tempX = -ww;
                float tempY = -hh;
                float rotX = tempX * cos - tempY * sin;
                float rotY = tempX * sin + tempY * cos;
                rotX += dx + ww;
                rotY += dy + hh;

                vertex.Position0.X = rotX;
                vertex.Position0.Y = rotY;


                tempX = dwidth - ww;
                tempY = -hh;
                rotX = tempX * cos - tempY * sin;
                rotY = tempX * sin + tempY * cos;
                rotX += dx + ww;
                rotY += dy + hh;

                vertex.Position1.X = rotX;
                vertex.Position1.Y = rotY;


                tempX = -ww;
                tempY = dheight - hh;
                rotX = tempX * cos - tempY * sin;
                rotY = tempX * sin + tempY * cos;
                rotX += dx + ww;
                rotY += dy + hh;

                vertex.Position2.X = rotX;
                vertex.Position2.Y = rotY;


                tempX = dwidth - ww;
                tempY = dheight - hh;
                rotX = tempX * cos - tempY * sin;
                rotY = tempX * sin + tempY * cos;
                rotX += dx + ww;
                rotY += dy + hh;

                vertex.Position3.X = rotX;
                vertex.Position3.Y = rotY;
            }
            else
            {
                vertex.Position0.X = x;
                vertex.Position0.Y = y;

                vertex.Position1.X = w;
                vertex.Position1.Y = y;

                vertex.Position2.X = x;
                vertex.Position2.Y = h;

                vertex.Position3.X = w;
                vertex.Position3.Y = h;
            }

            vertex.Position0.Z = 0;
            vertex.Normal0.X = 0;
            vertex.Normal0.Y = 0;
            vertex.Normal0.Z = 1;
            vertex.TextureCoordinate0.X = minX;
            vertex.TextureCoordinate0.Y = minY;
            vertex.TextureCoordinate0.Z = 0;

            vertex.Position1.Z = 0;
            vertex.Normal1.X = 0;
            vertex.Normal1.Y = 0;
            vertex.Normal1.Z = 1;
            vertex.TextureCoordinate1.X = maxX;
            vertex.TextureCoordinate1.Y = minY;
            vertex.TextureCoordinate1.Z = 0;

            vertex.Position2.Z = 0;
            vertex.Normal2.X = 0;
            vertex.Normal2.Y = 0;
            vertex.Normal2.Z = 1;
            vertex.TextureCoordinate2.X = minX;
            vertex.TextureCoordinate2.Y = maxY;
            vertex.TextureCoordinate2.Z = 0;

            vertex.Position3.Z = 0;
            vertex.Normal3.X = 0;
            vertex.Normal3.Y = 0;
            vertex.Normal3.Z = 1;
            vertex.TextureCoordinate3.X = maxX;
            vertex.TextureCoordinate3.Y = maxY;
            vertex.TextureCoordinate3.Z = 0;
            vertex.Hue0 = vertex.Hue1 = vertex.Hue2 = vertex.Hue3 = hue;


            //if (CheckInScreen(idx))
            {
                PushSprite(texture);

                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Draw2D
        (
            Texture2D texture,
            float x,
            float y,
            float width,
            float height,
            ref Vector3 hue
        )
        {
            EnsureSize();

            ref PositionNormalTextureColor4 vertex = ref _vertexInfo[_numSprites];

            vertex.Position0.X = x;
            vertex.Position0.Y = y;
            vertex.Position0.Z = 0;
            vertex.Normal0.X = 0;
            vertex.Normal0.Y = 0;
            vertex.Normal0.Z = 1;
            vertex.TextureCoordinate0.X = 0;
            vertex.TextureCoordinate0.Y = 0;
            vertex.TextureCoordinate0.Z = 0;

            vertex.Position1.X = x + width;
            vertex.Position1.Y = y;
            vertex.Position1.Z = 0;
            vertex.Normal1.X = 0;
            vertex.Normal1.Y = 0;
            vertex.Normal1.Z = 1;
            vertex.TextureCoordinate1.X = 1;
            vertex.TextureCoordinate1.Y = 0;
            vertex.TextureCoordinate1.Z = 0;

            vertex.Position2.X = x;
            vertex.Position2.Y = y + height;
            vertex.Position2.Z = 0;
            vertex.Normal2.X = 0;
            vertex.Normal2.Y = 0;
            vertex.Normal2.Z = 1;
            vertex.TextureCoordinate2.X = 0;
            vertex.TextureCoordinate2.Y = 1;
            vertex.TextureCoordinate2.Z = 0;

            vertex.Position3.X = x + width;
            vertex.Position3.Y = y + height;
            vertex.Position3.Z = 0;
            vertex.Normal3.X = 0;
            vertex.Normal3.Y = 0;
            vertex.Normal3.Z = 1;
            vertex.TextureCoordinate3.X = 1;
            vertex.TextureCoordinate3.Y = 1;
            vertex.TextureCoordinate3.Z = 0;

            vertex.Hue0 = vertex.Hue1 = vertex.Hue2 = vertex.Hue3 = hue;

            PushSprite(texture);

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Draw2DTiled
        (
            Texture2D texture,
            int dx,
            int dy,
            float dwidth,
            float dheight,
            ref Vector3 hue
        )
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
                    {
                        rw = w;
                    }

                    Draw2D
                    (
                        texture,
                        x,
                        y,
                        0,
                        0,
                        rw,
                        rh,
                        ref hue
                    );

                    w -= texture.Width;
                    x += texture.Width;
                }

                h -= texture.Height;
                y += texture.Height;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool DrawRectangle
        (
            Texture2D texture,
            int x,
            int y,
            int width,
            int height,
            ref Vector3 hue
        )
        {
            Draw2D
            (
                texture,
                x,
                y,
                width,
                1,
                ref hue
            );

            Draw2D
            (
                texture,
                x + width,
                y,
                1,
                height + 1,
                ref hue
            );

            Draw2D
            (
                texture,
                x,
                y + height,
                width,
                1,
                ref hue
            );

            Draw2D
            (
                texture,
                x,
                y,
                1,
                height,
                ref hue
            );

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool DrawLine
        (
            Texture2D texture,
            int startX,
            int startY,
            int endX,
            int endY,
            int originX,
            int originY
        )
        {
            EnsureSize();

            ref PositionNormalTextureColor4 vertex = ref _vertexInfo[_numSprites];

            const int WIDTH = 1;
            Vector2 begin = new Vector2(startX, startY);
            Vector2 end = new Vector2(endX, endY);

            Rectangle r = new Rectangle((int) begin.X, (int) begin.Y, (int) (end - begin).Length() + WIDTH, WIDTH);

            float angle = (float) (Math.Atan2(end.Y - begin.Y, end.X - begin.X) * 57.295780);
            angle = -(float) (angle * Math.PI) / 180.0f;


            float ww = r.Width * 0.5f;
            float hh = r.Height * 0.5f;


            float rotSin = (float) Math.Sin(angle);
            float rotCos = (float) Math.Cos(angle);


            float sinx = rotSin * ww;
            float cosx = rotCos * ww;
            float siny = rotSin * hh;
            float cosy = rotCos * hh;


            vertex.Position0.X = originX;
            vertex.Position0.Y = originY;
            vertex.Position0.X += cosx - -siny;
            vertex.Position0.Y -= sinx + -cosy;
            vertex.Normal0.X = 0;
            vertex.Normal0.Y = 0;
            vertex.Normal0.Z = 1;
            vertex.TextureCoordinate0.X = 0;
            vertex.TextureCoordinate0.Y = 0;
            vertex.TextureCoordinate0.Z = 0;

            vertex.Position1.X = originX;
            vertex.Position1.Y = originY;
            vertex.Position1.X += cosx - siny;
            vertex.Position1.Y += -sinx + -cosy;
            vertex.Normal1.X = 0;
            vertex.Normal1.Y = 0;
            vertex.Normal1.Z = 1;
            vertex.TextureCoordinate1.X = 0;
            vertex.TextureCoordinate1.Y = 1;
            vertex.TextureCoordinate1.Z = 0;

            vertex.Position2.X = originX;
            vertex.Position2.Y = originY;
            vertex.Position2.X += -cosx - -siny;
            vertex.Position2.Y += sinx + cosy;
            vertex.Normal2.X = 0;
            vertex.Normal2.Y = 0;
            vertex.Normal2.Z = 1;
            vertex.TextureCoordinate2.X = 1;
            vertex.TextureCoordinate2.Y = 0;
            vertex.TextureCoordinate2.Z = 0;

            vertex.Position3.X = originX;
            vertex.Position3.Y = originY;
            vertex.Position3.X += -cosx - siny;
            vertex.Position3.Y += sinx + -cosy;
            vertex.Normal3.X = 0;
            vertex.Normal3.Y = 0;
            vertex.Normal3.Z = 1;
            vertex.TextureCoordinate3.X = 1;
            vertex.TextureCoordinate3.Y = 1;
            vertex.TextureCoordinate3.Z = 0;


            vertex.Hue0 = vertex.Hue1 = vertex.Hue2 = vertex.Hue3 = Vector3.Zero;

            PushSprite(texture);

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Begin()
        {
            Begin(null, Matrix.Identity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Begin(Effect effect)
        {
            Begin(effect, Matrix.Identity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Begin(Effect customEffect, Matrix transform_matrix)
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
            _transformMatrix = transform_matrix;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void End()
        {
            EnsureStarted();
            Flush();
            _started = false;
            _customEffect = null;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureSize()
        {
            EnsureStarted();

            if (_numSprites >= MAX_SPRITES)
            {
                Flush();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool PushSprite(Texture2D texture)
        {
            if (texture == null || texture.IsDisposed)
            {
                return false;
            }

            EnsureSize();
            _textureInfo[_numSprites++] = texture;

            return true;
        }

        [Conditional("DEBUG")]
        private void EnsureStarted()
        {
            if (!_started)
            {
                throw new InvalidOperationException();
            }
        }

        [Conditional("DEBUG")]
        private void EnsureNotStarted()
        {
            if (_started)
            {
                throw new InvalidOperationException();
            }
        }

        private void ApplyStates()
        {
            GraphicsDevice.BlendState = _blendState;
            GraphicsDevice.DepthStencilState = _stencil;
            GraphicsDevice.RasterizerState = _rasterizerState;
            GraphicsDevice.SamplerStates[0] = _sampler;
            GraphicsDevice.SamplerStates[1] = SamplerState.PointClamp;
            GraphicsDevice.SamplerStates[2] = SamplerState.PointClamp;
            GraphicsDevice.SamplerStates[3] = SamplerState.PointClamp;

            GraphicsDevice.Indices = _indexBuffer;
            GraphicsDevice.SetVertexBuffer(_vertexBuffer);

            SetMatrixForEffect(DefaultEffect);
        }

        private void Flush()
        {
            if (_numSprites == 0)
            {
                return;
            }

            ApplyStates();

            int start = UpdateVertexBuffer(_numSprites);

            Texture2D current = _textureInfo[0];
            int offset = 0;

            if (_customEffect != null)
            {
                if (_customEffect is MatrixEffect eff)
                {
                    SetMatrixForEffect(eff);
                }
                else
                {
                    _customEffect.CurrentTechnique.Passes[0].Apply();
                }
            }

            for (int i = 1; i < _numSprites; i++)
            {
                if (_textureInfo[i] != current)
                {
                    InternalDraw(current, start + offset, i - offset);
                    current = _textureInfo[i];
                    offset = i;
                }
            }

            InternalDraw(current, start + offset, _numSprites - offset);

            _numSprites = 0;
        }

        private void SetMatrixForEffect(MatrixEffect effect)
        {
            _projectionMatrix.M11 = (float) (2.0 / GraphicsDevice.Viewport.Width);
            _projectionMatrix.M22 = (float) (-2.0 / GraphicsDevice.Viewport.Height);

            Matrix.Multiply(ref _transformMatrix, ref _projectionMatrix, out Matrix matrix);

            effect.ApplyStates(matrix);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InternalDraw(Texture texture, int baseSprite, int batchSize)
        {
            GraphicsDevice.Textures[0] = texture;

            GraphicsDevice.DrawIndexedPrimitives
            (
                PrimitiveType.TriangleList,
                baseSprite << 2,
                0,
                batchSize << 2,
                0,
                batchSize << 1
            );
        }

        public bool ClipBegin(int x, int y, int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                return false;
            }

            Rectangle scissor = ScissorStack.CalculateScissors
            (
                TransformMatrix,
                x,
                y,
                width,
                height
            );

            Flush();

            if (ScissorStack.PushScissors(GraphicsDevice, scissor))
            {
                EnableScissorTest(true);

                return true;
            }
            
            return false;
        }

        public void ClipEnd()
        {
            EnableScissorTest(false);
            ScissorStack.PopScissors(GraphicsDevice);

            Flush();
        }

        public void EnableScissorTest(bool enable)
        {
            bool rasterize = GraphicsDevice.RasterizerState.ScissorTestEnable;

            if (ScissorStack.HasScissors)
            {
                enable = true;
            }

            if (enable == rasterize)
            {
                return;
            }

            Flush();

            GraphicsDevice.RasterizerState.ScissorTestEnable = enable;
        }

        public void SetBlendState(BlendState blend)
        {
            Flush();

            _blendState = blend ?? BlendState.AlphaBlend;
        }

        public void SetStencil(DepthStencilState stencil)
        {
            Flush();

            _stencil = stencil ?? Stencil;
        }

        public void SetSampler(SamplerState sampler)
        {
            Flush();

            _sampler = sampler ?? SamplerState.PointClamp;
        }

        private int UpdateVertexBuffer(int len)
        {
            int pos;
            SetDataOptions hint;

            if (_currentBufferPosition + len > MAX_SPRITES)
            {
                pos = 0;
                hint = SetDataOptions.Discard;
            }
            else
            {
                pos = _currentBufferPosition;
                hint = SetDataOptions.NoOverwrite;
            }

            _vertexBuffer.SetDataPointerEXT(pos * PositionNormalTextureColor4.SIZE_IN_BYTES, (IntPtr) _vertexInfo, len * PositionNormalTextureColor4.SIZE_IN_BYTES, hint);

            _currentBufferPosition = pos + len;

            return pos;
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


        private class IsometricEffect : MatrixEffect
        {
            private readonly Matrix _matrix = Matrix.Identity;
            private Vector2 _viewPort;

            public IsometricEffect(GraphicsDevice graphicsDevice) : base(graphicsDevice, Resources.IsometricEffect)
            {
                WorldMatrix = Parameters["WorldMatrix"];
                Viewport = Parameters["Viewport"];
                Brighlight = Parameters["Brightlight"];

                CurrentTechnique = Techniques["HueTechnique"];
            }


            public EffectParameter WorldMatrix { get; }
            public EffectParameter Viewport { get; }
            public EffectParameter Brighlight { get; }


            public override void ApplyStates(Matrix matrix)
            {
                WorldMatrix.SetValue(_matrix);

                _viewPort.X = GraphicsDevice.Viewport.Width;
                _viewPort.Y = GraphicsDevice.Viewport.Height;
                Viewport.SetValue(_viewPort);

                base.ApplyStates(matrix);
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct PositionNormalTextureColor4 : IVertexType
        {
            public Vector3 Position0;
            public Vector3 Normal0;
            public Vector3 TextureCoordinate0;
            public Vector3 Hue0;

            public Vector3 Position1;
            public Vector3 Normal1;
            public Vector3 TextureCoordinate1;
            public Vector3 Hue1;

            public Vector3 Position2;
            public Vector3 Normal2;
            public Vector3 TextureCoordinate2;
            public Vector3 Hue2;

            public Vector3 Position3;
            public Vector3 Normal3;
            public Vector3 TextureCoordinate3;
            public Vector3 Hue3;

            VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

            private static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration
            (
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),                          // position
                new VertexElement(sizeof(float) * 3, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),            // normal
                new VertexElement(sizeof(float) * 6, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0), // tex coord
                new VertexElement(sizeof(float) * 9, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 1)  // hue
            );

            public const int SIZE_IN_BYTES = sizeof(float) * 12 * 4;

            //#if DEBUG
            //        public override string ToString()
            //        {
            //            return string.Format("VPNTH: <{0}> <{1}>", Position.ToString(), TextureCoordinate.ToString());
            //        }
            //#endif
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
                Stream stream = typeof(SpriteBatch).Assembly.GetManifestResourceStream("Microsoft.Xna.Framework.Graphics.Effect.Resources.SpriteEffect.fxb");

                using (MemoryStream ms = new MemoryStream())
                {
                    stream.CopyTo(ms);

                    return ms.ToArray();
                }
            }
        }

        private static byte[] GetResource(string name)
        {
            Stream stream = typeof(UltimaBatcher2D).Assembly.GetManifestResourceStream(name);

            using (MemoryStream ms = new MemoryStream())
            {
                stream.CopyTo(ms);

                return ms.ToArray();
            }
        }
    }
}