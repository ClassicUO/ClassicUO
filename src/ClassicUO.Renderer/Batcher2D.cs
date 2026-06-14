// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Renderer.Effects;
using FontStashSharp.Interfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ClassicUO.Renderer
{
    public sealed unsafe class UltimaBatcher2D : IDisposable, IFontStashRenderer
    {
        private static readonly float[] _cornerOffsetX = new float[] { 0.0f, 1.0f, 0.0f, 1.0f };
        private static readonly float[] _cornerOffsetY = new float[] { 0.0f, 0.0f, 1.0f, 1.0f };

        private const int MAX_SPRITES = 0x800;
        private const int MAX_VERTICES = MAX_SPRITES * 4;
        private const int MAX_INDICES = MAX_SPRITES * 6;
        private BlendState _blendState;
        private int _currentBufferPosition;

        private Effect _customEffect;

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
        private Matrix _transformMatrix;
        private readonly DynamicVertexBuffer _vertexBuffer;
        private DynamicIndexBuffer _dynamicIndexBuffer;
        private readonly BasicUOEffect _basicUOEffect;
        private Texture2D[] _textureInfo;
        private PositionNormalTextureColor4[] _vertexInfo;


        public UltimaBatcher2D(GraphicsDevice device)
        {
            GraphicsDevice = device;

            _textureInfo = new Texture2D[MAX_SPRITES];
            _vertexInfo = new PositionNormalTextureColor4[MAX_SPRITES];
            _vertexBuffer = new DynamicVertexBuffer(GraphicsDevice, typeof(PositionNormalTextureColor4), MAX_VERTICES, BufferUsage.WriteOnly);
            _indexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, MAX_INDICES, BufferUsage.WriteOnly);
            _indexBuffer.SetData(GenerateIndexArray());

            _blendState = BlendState.AlphaBlend;
            _sampler = SamplerState.PointClamp;
            _rasterizerState = new RasterizerState
            {
                CullMode = CullMode.CullCounterClockwiseFace,
                FillMode = FillMode.Solid,
                DepthBias = 0,
                MultiSampleAntiAlias = true,
                ScissorTestEnable = true,
                SlopeScaleDepthBias = 0,
            };

            _stencil = Stencil;

            _basicUOEffect = new BasicUOEffect(device);
        }


        private Matrix TransformMatrix => _transformMatrix;


        private DepthStencilState Stencil { get; } = new DepthStencilState
        {
            StencilEnable = false,
            DepthBufferEnable = false,
            StencilFunction = CompareFunction.NotEqual,
            ReferenceStencil = -1,
            StencilMask = -1,
            StencilFail = StencilOperation.Keep,
            StencilDepthBufferFail = StencilOperation.Keep,
            StencilPass = StencilOperation.Keep
        };

        public GraphicsDevice GraphicsDevice { get; }

        public int TextureSwitches, FlushesDone;
        private bool _worldOffsetActive;



        public void Dispose()
        {
            _vertexInfo = null;
            _basicUOEffect?.Dispose();
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
        }


        public void SetBrightlight(float f)
        {
            _basicUOEffect.Brighlight.SetValue(f);
        }

        public void SetCircleOfTransparencyRadius(float radius)
        {
            _basicUOEffect.CircleOfTransparencyRadius.SetValue(radius);
        }

        public void DrawString(SpriteFont spriteFont, ReadOnlySpan<char> text, int x, int y, Vector3 color, float layerDepth)
            => DrawString(spriteFont, text, new Vector2(x, y), color, layerDepth);

        public void DrawString(SpriteFont spriteFont, ReadOnlySpan<char> text, Vector2 position, Vector3 color, float layerDepth)
        {
            if (text.IsEmpty)
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

                var pos = new Vector2(offsetX, offsetY);
                Draw
                (
                    textureValue,
                    position + pos,
                    cGlyph,
                    color,
                    layerDepth
                );

                curOffset.X += cKern.Y + cKern.Z;
            }
        }

        // ==========================
        // === UO drawing methods ===
        // ==========================
        public struct YOffsets
        {
            public int Top;
            public int Right;
            public int Left;
            public int Bottom;
        }

        public void DrawStretchedLand
        (
            Texture2D texture,
            Vector2 position,
            Rectangle sourceRect,
            ref YOffsets yOffsets,
            ref Vector3 normalTop,
            ref Vector3 normalRight,
            ref Vector3 normalLeft,
            ref Vector3 normalBottom,
            Vector3 hue,
            float depth
        )
        {
            EnsureSize();

            ref PositionNormalTextureColor4 vertex = ref _vertexInfo[_numSprites];

            // we need to apply an offset to the texture
            CalculateHalfPixelUVs(sourceRect, texture.Width, texture.Height,
                out float sourceX, out float sourceY, out float sourceW, out float sourceH);

            vertex.TextureCoordinate0.X = (_cornerOffsetX[0] * sourceW) + sourceX;
            vertex.TextureCoordinate0.Y = (_cornerOffsetY[0] * sourceH) + sourceY;
            vertex.TextureCoordinate1.X = (_cornerOffsetX[1] * sourceW) + sourceX;
            vertex.TextureCoordinate1.Y = (_cornerOffsetY[1] * sourceH) + sourceY;
            vertex.TextureCoordinate2.X = (_cornerOffsetX[2] * sourceW) + sourceX;
            vertex.TextureCoordinate2.Y = (_cornerOffsetY[2] * sourceH) + sourceY;
            vertex.TextureCoordinate3.X = (_cornerOffsetX[3] * sourceW) + sourceX;
            vertex.TextureCoordinate3.Y = (_cornerOffsetY[3] * sourceH) + sourceY;
            vertex.TextureCoordinate0.Z = 0;
            vertex.TextureCoordinate1.Z = 0;
            vertex.TextureCoordinate2.Z = 0;
            vertex.TextureCoordinate3.Z = 0;

            vertex.Normal0 = normalTop;
            vertex.Normal1 = normalRight;
            vertex.Normal2 = normalLeft;
            vertex.Normal3 = normalBottom;

            // Top
            vertex.Position0.X = position.X + 22;
            vertex.Position0.Y = position.Y - yOffsets.Top;

            // Right
            vertex.Position1.X = position.X + 44;
            vertex.Position1.Y = position.Y + (22 - yOffsets.Right);

            // Left
            vertex.Position2.X = position.X;
            vertex.Position2.Y = position.Y + (22 - yOffsets.Left);

            // Bottom
            vertex.Position3.X = position.X + 22;
            vertex.Position3.Y = position.Y + (44 - yOffsets.Bottom);


            vertex.Position0.Z = depth;
            vertex.Position1.Z = depth;
            vertex.Position2.Z = depth;
            vertex.Position3.Z = depth;

            vertex.Hue0 = vertex.Hue1 = vertex.Hue2 = vertex.Hue3 = hue;

            PushSprite(texture);
        }

        public void DrawShadow(Texture2D texture, Vector2 position, Rectangle sourceRect, bool flip, float depth)
        {
            float width = sourceRect.Width;
            float height = sourceRect.Height * 0.5f;
            float translatedY = position.Y + height - 10;
            float ratio = height / width;

            EnsureSize();

            ref PositionNormalTextureColor4 vertex = ref _vertexInfo[_numSprites];

            vertex.Position0.X = position.X + width * ratio;
            vertex.Position0.Y = translatedY;

            vertex.Position1.X = position.X + width * (ratio + 1f);
            vertex.Position1.Y = translatedY;

            vertex.Position2.X = position.X;
            vertex.Position2.Y = translatedY + height;

            vertex.Position3.X = position.X + width;
            vertex.Position3.Y = translatedY + height;

            vertex.Position0.Z = depth;
            vertex.Position1.Z = depth;
            vertex.Position2.Z = depth;
            vertex.Position3.Z = depth;


            CalculateHalfPixelUVs(sourceRect, texture.Width, texture.Height,
                out float sourceX, out float sourceY, out float sourceW, out float sourceH);

            byte effects = (byte)((flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None) & (SpriteEffects)0x03);

            vertex.TextureCoordinate0.X = (_cornerOffsetX[0 ^ effects] * sourceW) + sourceX;
            vertex.TextureCoordinate0.Y = (_cornerOffsetY[0 ^ effects] * sourceH) + sourceY;
            vertex.TextureCoordinate1.X = (_cornerOffsetX[1 ^ effects] * sourceW) + sourceX;
            vertex.TextureCoordinate1.Y = (_cornerOffsetY[1 ^ effects] * sourceH) + sourceY;
            vertex.TextureCoordinate2.X = (_cornerOffsetX[2 ^ effects] * sourceW) + sourceX;
            vertex.TextureCoordinate2.Y = (_cornerOffsetY[2 ^ effects] * sourceH) + sourceY;
            vertex.TextureCoordinate3.X = (_cornerOffsetX[3 ^ effects] * sourceW) + sourceX;
            vertex.TextureCoordinate3.Y = (_cornerOffsetY[3 ^ effects] * sourceH) + sourceY;
            vertex.TextureCoordinate0.Z = 0;
            vertex.TextureCoordinate1.Z = 0;
            vertex.TextureCoordinate2.Z = 0;
            vertex.TextureCoordinate3.Z = 0;

            SetDefaultNormals(ref vertex);

            vertex.Hue0.X = vertex.Hue1.X = vertex.Hue2.X = vertex.Hue3.X = 0;
            vertex.Hue0.Y = vertex.Hue1.Y = vertex.Hue2.Y = vertex.Hue3.Y = ShaderHueTranslator.SHADER_SHADOW;
            vertex.Hue0.Z = vertex.Hue1.Z = vertex.Hue2.Z = vertex.Hue3.Z = 1f;

            PushSprite(texture);
        }

        public void DrawCharacterSitted
        (
            Texture2D texture,
            Vector2 position,
            Rectangle sourceRect,
            Vector3 mod,
            Vector3 hue,
            bool flip,
            float depth
        )
        {
            EnsureSize();

            float h03 = sourceRect.Height * mod.X;
            float h06 = sourceRect.Height * mod.Y;
            float h09 = sourceRect.Height * mod.Z;

            float sittingOffset = flip ? -8.0f : 8.0f;

            float width = sourceRect.Width;
            float widthOffset = sourceRect.Width + sittingOffset;

            if (mod.X != 0.0f)
            {
                DrawSittedSection(
                    texture, ref sourceRect, ref hue, flip, depth, mod.X,
                    position.X + sittingOffset, position.Y,
                    position.X + widthOffset, position.Y,
                    position.X + sittingOffset, position.Y + h03,
                    position.X + widthOffset, position.Y + h03,
                    0f);
            }

            if (mod.Y != 0.0f)
            {
                DrawSittedSection(
                    texture, ref sourceRect, ref hue, flip, depth, mod.Y,
                    position.X + sittingOffset, position.Y + h03,
                    position.X + widthOffset, position.Y + h03,
                    position.X, position.Y + h06,
                    position.X + width, position.Y + h06,
                    h03);
            }

            if (mod.Z != 0.0f)
            {
                DrawSittedSection(
                    texture, ref sourceRect, ref hue, flip, depth, mod.Z,
                    position.X, position.Y + h06,
                    position.X + width, position.Y + h06,
                    position.X, position.Y + h09,
                    position.X + width, position.Y + h09,
                    h06);
            }
        }

        private void DrawSittedSection(
            Texture2D texture,
            ref Rectangle sourceRect,
            ref Vector3 hue,
            bool flip,
            float depth,
            float modValue,
            float x0, float y0,
            float x1, float y1,
            float x2, float y2,
            float x3, float y3,
            float uvYOffset)
        {
            EnsureSize();

            ref PositionNormalTextureColor4 vertex = ref _vertexInfo[_numSprites];

            vertex.Position0.X = x0;
            vertex.Position0.Y = y0;
            vertex.Position1.X = x1;
            vertex.Position1.Y = y1;
            vertex.Position2.X = x2;
            vertex.Position2.Y = y2;
            vertex.Position3.X = x3;
            vertex.Position3.Y = y3;

            vertex.Position0.Z = depth;
            vertex.Position1.Z = depth;
            vertex.Position2.Z = depth;
            vertex.Position3.Z = depth;

            CalculateHalfPixelUVs(sourceRect, texture.Width, texture.Height,
                out float sourceX, out float sourceY, out float sourceW, out float sourceH);
            float invH = 1f / texture.Height;
            sourceY += uvYOffset * invH;
            sourceH -= uvYOffset * invH;

            byte effects = (byte)((flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None) & (SpriteEffects)0x03);

            vertex.TextureCoordinate0.X = (_cornerOffsetX[0 ^ effects] * sourceW) + sourceX;
            vertex.TextureCoordinate0.Y = (_cornerOffsetY[0 ^ effects] * sourceH) + sourceY;
            vertex.TextureCoordinate1.X = (_cornerOffsetX[1 ^ effects] * sourceW) + sourceX;
            vertex.TextureCoordinate1.Y = (_cornerOffsetY[1 ^ effects] * sourceH) + sourceY;
            vertex.TextureCoordinate2.X = (_cornerOffsetX[2 ^ effects] * sourceW) + sourceX;
            vertex.TextureCoordinate2.Y = (_cornerOffsetY[2 ^ effects] * sourceH * modValue) + sourceY;
            vertex.TextureCoordinate3.X = (_cornerOffsetX[3 ^ effects] * sourceW) + sourceX;
            vertex.TextureCoordinate3.Y = (_cornerOffsetY[3 ^ effects] * sourceH * modValue) + sourceY;
            vertex.TextureCoordinate0.Z = 0;
            vertex.TextureCoordinate1.Z = 0;
            vertex.TextureCoordinate2.Z = 0;
            vertex.TextureCoordinate3.Z = 0;

            SetDefaultNormals(ref vertex);

            vertex.Hue0 = vertex.Hue1 = vertex.Hue2 = vertex.Hue3 = hue;

            PushSprite(texture);
        }

        public void DrawTiled
        (
            Texture2D texture,
            Rectangle destinationRectangle,
            Rectangle sourceRectangle,
            Vector3 hue,
            float layerDepth
        )
        {
            int h = destinationRectangle.Height;

            Rectangle rect = sourceRectangle;
            Vector2 pos = new Vector2(destinationRectangle.X, destinationRectangle.Y);

            while (h > 0)
            {
                pos.X = destinationRectangle.X;
                int w = destinationRectangle.Width;

                rect.Height = Math.Min(h, sourceRectangle.Height);

                while (w > 0)
                {
                    rect.Width = Math.Min(w, sourceRectangle.Width);

                    Draw
                    (
                        texture,
                        pos,
                        rect,
                        hue,
                        layerDepth
                    );

                    w -= sourceRectangle.Width;
                    pos.X += sourceRectangle.Width;
                }

                h -= sourceRectangle.Height;
                pos.Y += sourceRectangle.Height;
            }
        }

        public bool DrawRectangle
        (
            Texture2D texture,
            int x,
            int y,
            int width,
            int height,
            Vector3 hue,
            float depth
        )
        {
            Rectangle rect = new Rectangle(x, y, width, 1);
            Draw(texture, rect, null, hue, 0f, Vector2.Zero, SpriteEffects.None, depth);

            rect.X += width;
            rect.Width = 1;
            rect.Height += height;
            Draw(texture, rect, null, hue, 0f, Vector2.Zero, SpriteEffects.None, depth);

            rect.X = x;
            rect.Y = y + height;
            rect.Width = width;
            rect.Height = 1;
            Draw(texture, rect, null, hue, 0f, Vector2.Zero, SpriteEffects.None, depth);

            rect.X = x;
            rect.Y = y;
            rect.Width = 1;
            rect.Height = height;
            Draw(texture, rect, null, hue, 0f, Vector2.Zero, SpriteEffects.None, depth);

            return true;
        }

        public void DrawLine
        (
            Texture2D texture,
            Vector2 start,
            Vector2 end,
            Vector3 color,
            float stroke,
            float depth
        )
        {
            var radians = ClassicUO.Utility.MathHelper.AngleBetweenVectors(start, end);
            Vector2.Distance(ref start, ref end, out var length);

            Draw
            (
                texture,
                start,
                texture.Bounds,
                color,
                radians,
                Vector2.Zero,
                new Vector2(length, stroke),
                SpriteEffects.None,
                depth
            );
        }




        public void Draw
        (
            Texture2D texture,
            Vector2 position,
            Vector3 color,
            float depth
        )
        {
            AddSprite(texture, 0f, 0f, 1f, 1f, position.X, position.Y, texture.Width, texture.Height, color, 0f, 0f, 0f, 1f, depth, 0);
        }

        public void Draw
        (
            Texture2D texture,
            Vector2 position,
            Rectangle? sourceRectangle,
            Vector3 color,
            float depth
        )
        {
            float sourceX, sourceY, sourceW, sourceH;
            float destW, destH;

            if (sourceRectangle.HasValue)
            {
                CalculateUVs(sourceRectangle.Value, texture.Width, texture.Height,
                    out sourceX, out sourceY, out sourceW, out sourceH);
                destW = sourceRectangle.Value.Width;
                destH = sourceRectangle.Value.Height;
            }
            else
            {
                sourceX = 0.0f;
                sourceY = 0.0f;
                sourceW = 1.0f;
                sourceH = 1.0f;
                destW = texture.Width;
                destH = texture.Height;
            }

            AddSprite(texture, sourceX, sourceY, sourceW, sourceH, position.X, position.Y, destW, destH, color, 0.0f, 0.0f, 0.0f, 1.0f, depth, 0);
        }

        public void Draw
        (
            Texture2D texture,
            Vector2 position,
            Rectangle? sourceRectangle,
            Vector3 color,
            float rotation,
            Vector2 origin,
            float scale,
            SpriteEffects effects,
            float layerDepth
        )
        {
            float sourceX, sourceY, sourceW, sourceH;
            float destW = scale;
            float destH = scale;

            if (sourceRectangle.HasValue)
            {
                CalculateUVsSafe(sourceRectangle.Value, texture.Width, texture.Height,
                    out sourceX, out sourceY, out sourceW, out sourceH);
                destW *= sourceRectangle.Value.Width;
                destH *= sourceRectangle.Value.Height;
            }
            else
            {
                sourceX = 0.0f;
                sourceY = 0.0f;
                sourceW = 1.0f;
                sourceH = 1.0f;
                destW *= texture.Width;
                destH *= texture.Height;
            }

            AddSprite
            (
                texture,
                sourceX,
                sourceY,
                sourceW,
                sourceH,
                position.X,
                position.Y,
                destW,
                destH,
                color,
                origin.X / sourceW / (float)texture.Width,
                origin.Y / sourceH / (float)texture.Height,
                (float)Math.Sin(rotation),
                (float)Math.Cos(rotation),
                layerDepth,
                (byte)(effects & (SpriteEffects)0x03)
            );
        }

        public void Draw
        (
            Texture2D texture,
            Vector2 position,
            Rectangle? sourceRectangle,
            Vector3 color,
            float rotation,
            Vector2 origin,
            Vector2 scale,
            SpriteEffects effects,
            float layerDepth
        )
        {
            float sourceX, sourceY, sourceW, sourceH;
            if (sourceRectangle.HasValue)
            {
                CalculateUVsSafe(sourceRectangle.Value, texture.Width, texture.Height,
                    out sourceX, out sourceY, out sourceW, out sourceH);
                scale.X *= sourceRectangle.Value.Width;
                scale.Y *= sourceRectangle.Value.Height;
            }
            else
            {
                sourceX = 0.0f;
                sourceY = 0.0f;
                sourceW = 1.0f;
                sourceH = 1.0f;
                scale.X *= texture.Width;
                scale.Y *= texture.Height;
            }

            AddSprite
            (
                texture,
                sourceX,
                sourceY,
                sourceW,
                sourceH,
                position.X,
                position.Y,
                scale.X,
                scale.Y,
                color,
                origin.X / sourceW / (float)texture.Width,
                origin.Y / sourceH / (float)texture.Height,
                (float)Math.Sin(rotation),
                (float)Math.Cos(rotation),
                layerDepth,
                (byte)(effects & (SpriteEffects)0x03)
            );
        }

        public void Draw
        (
            Texture2D texture,
            Rectangle destinationRectangle,
            Vector3 color,
            float layerDepth
        )
        {
            AddSprite(
                texture,
                0.0f,
                0.0f,
                1.0f,
                1.0f,
                destinationRectangle.X,
                destinationRectangle.Y,
                destinationRectangle.Width,
                destinationRectangle.Height,
                color,
                0.0f,
                0.0f,
                0.0f,
                1.0f,
                layerDepth,
                0
            );
        }

        public void Draw
        (
            Texture2D texture,
            Rectangle destinationRectangle,
            Rectangle? sourceRectangle,
            Vector3 color,
            float layerDepth
        )
        {
            float sourceX, sourceY, sourceW, sourceH;
            if (sourceRectangle.HasValue)
            {
                CalculateUVs(sourceRectangle.Value, texture.Width, texture.Height,
                    out sourceX, out sourceY, out sourceW, out sourceH);
            }
            else
            {
                sourceX = 0.0f;
                sourceY = 0.0f;
                sourceW = 1.0f;
                sourceH = 1.0f;
            }

            AddSprite
            (
                texture,
                sourceX,
                sourceY,
                sourceW,
                sourceH,
                destinationRectangle.X,
                destinationRectangle.Y,
                destinationRectangle.Width,
                destinationRectangle.Height,
                color,
                0.0f,
                0.0f,
                0.0f,
                1.0f,
                layerDepth,
                0
            );
        }

        public void Draw
        (
            Texture2D texture,
            Rectangle destinationRectangle,
            Rectangle? sourceRectangle,
            Vector3 color,
            float rotation,
            Vector2 origin,
            SpriteEffects effects,
            float layerDepth
        )
        {
            float sourceX, sourceY, sourceW, sourceH;
            if (sourceRectangle.HasValue)
            {
                CalculateUVsSafe(sourceRectangle.Value, texture.Width, texture.Height,
                    out sourceX, out sourceY, out sourceW, out sourceH);
            }
            else
            {
                sourceX = 0.0f;
                sourceY = 0.0f;
                sourceW = 1.0f;
                sourceH = 1.0f;
            }

            AddSprite
            (
                texture,
                sourceX,
                sourceY,
                sourceW,
                sourceH,
                destinationRectangle.X,
                destinationRectangle.Y,
                destinationRectangle.Width,
                destinationRectangle.Height,
                color,
                origin.X / sourceW / (float)texture.Width,
                origin.Y / sourceH / (float)texture.Height,
                (float)Math.Sin(rotation),
                (float)Math.Cos(rotation),
                layerDepth,
                (byte)(effects & (SpriteEffects)0x03)
            );
        }

        private void AddSprite
        (
            Texture2D texture,
            float sourceX,
            float sourceY,
            float sourceW,
            float sourceH,
            float destinationX,
            float destinationY,
            float destinationW,
            float destinationH,
            Vector3 color,
            float originX,
            float originY,
            float rotationSin,
            float rotationCos,
            float depth,
            byte effects
        )
        {
            EnsureSize();

            SetVertex
            (
                ref _vertexInfo[_numSprites],
                sourceX, sourceY, sourceW, sourceH,
                destinationX, destinationY, destinationW, destinationH,
                color,
                originX, originY,
                rotationSin, rotationCos,
                depth, effects
            );

            _textureInfo[_numSprites] = texture;
            ++_numSprites;
        }


        public void Begin()
        {
            Begin(null, Matrix.Identity);
        }

        public void Begin(Effect effect)
        {
            Begin(effect, Matrix.Identity);
        }

        public void Begin(Effect customEffect, Matrix transform_matrix)
        {
            EnsureNotStarted();
            _started = true;
            _worldOffsetActive = false;
            TextureSwitches = 0;
            FlushesDone = 0;

            _customEffect = customEffect;
            _transformMatrix = transform_matrix;
        }

        public void End()
        {
            EnsureStarted();
            Flush();
            _started = false;
            _customEffect = null;
        }

        private void SetVertex
        (
            ref PositionNormalTextureColor4 sprite,
            float sourceX,
            float sourceY,
            float sourceW,
            float sourceH,
            float destinationX,
            float destinationY,
            float destinationW,
            float destinationH,
            Vector3 color,
            float originX,
            float originY,
            float rotationSin,
            float rotationCos,
            float depth,
            byte effects
        )
        {
            float cornerX = -originX * destinationW;
            float cornerY = -originY * destinationH;
            sprite.Position0.X = ((-rotationSin * cornerY) + (rotationCos * cornerX) + destinationX);
            sprite.Position0.Y = ((rotationCos * cornerY) + (rotationSin * cornerX) + destinationY);

            cornerX = (1.0f - originX) * destinationW;
            cornerY = -originY * destinationH;
            sprite.Position1.X = ((-rotationSin * cornerY) + (rotationCos * cornerX) + destinationX);
            sprite.Position1.Y = ((rotationCos * cornerY) + (rotationSin * cornerX) + destinationY);

            cornerX = -originX * destinationW;
            cornerY = (1.0f - originY) * destinationH;
            sprite.Position2.X = ((-rotationSin * cornerY) + (rotationCos * cornerX) + destinationX);
            sprite.Position2.Y = ((rotationCos * cornerY) + (rotationSin * cornerX) + destinationY);

            cornerX = (1.0f - originX) * destinationW;
            cornerY = (1.0f - originY) * destinationH;
            sprite.Position3.X = ((-rotationSin * cornerY) + (rotationCos * cornerX) + destinationX);
            sprite.Position3.Y = ((rotationCos * cornerY) + (rotationSin * cornerX) + destinationY);


            sprite.TextureCoordinate0.X = (_cornerOffsetX[0 ^ effects] * sourceW) + sourceX;
            sprite.TextureCoordinate0.Y = (_cornerOffsetY[0 ^ effects] * sourceH) + sourceY;
            sprite.TextureCoordinate1.X = (_cornerOffsetX[1 ^ effects] * sourceW) + sourceX;
            sprite.TextureCoordinate1.Y = (_cornerOffsetY[1 ^ effects] * sourceH) + sourceY;
            sprite.TextureCoordinate2.X = (_cornerOffsetX[2 ^ effects] * sourceW) + sourceX;
            sprite.TextureCoordinate2.Y = (_cornerOffsetY[2 ^ effects] * sourceH) + sourceY;
            sprite.TextureCoordinate3.X = (_cornerOffsetX[3 ^ effects] * sourceW) + sourceX;
            sprite.TextureCoordinate3.Y = (_cornerOffsetY[3 ^ effects] * sourceH) + sourceY;

            sprite.TextureCoordinate0.Z = 0;
            sprite.TextureCoordinate1.Z = 0;
            sprite.TextureCoordinate2.Z = 0;
            sprite.TextureCoordinate3.Z = 0;


            sprite.Position0.Z = depth;
            sprite.Position1.Z = depth;
            sprite.Position2.Z = depth;
            sprite.Position3.Z = depth;


            sprite.Hue0 = color;
            sprite.Hue1 = color;
            sprite.Hue2 = color;
            sprite.Hue3 = color;

            SetDefaultNormals(ref sprite);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetDefaultNormals(ref PositionNormalTextureColor4 vertex)
        {
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
        }

        /// <summary>
        /// Calculates UVs with epsilon-safe width/height to avoid division artifacts when dimensions can be negative or zero.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CalculateUVsSafe(
            Rectangle source,
            int textureWidth, int textureHeight,
            out float sourceX, out float sourceY,
            out float sourceW, out float sourceH)
        {
            float invW = 1f / textureWidth;
            float invH = 1f / textureHeight;
            sourceX = source.X * invW;
            sourceY = source.Y * invH;
            sourceW = Math.Sign(source.Width) * Math.Max(Math.Abs(source.Width), Utility.MathHelper.MachineEpsilonFloat) * invW;
            sourceH = Math.Sign(source.Height) * Math.Max(Math.Abs(source.Height), Utility.MathHelper.MachineEpsilonFloat) * invH;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CalculateUVs(
            Rectangle source,
            int textureWidth, int textureHeight,
            out float sourceX, out float sourceY,
            out float sourceW, out float sourceH)
        {
            float invW = 1f / textureWidth;
            float invH = 1f / textureHeight;
            sourceX = source.X * invW;
            sourceY = source.Y * invH;
            sourceW = source.Width * invW;
            sourceH = source.Height * invH;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CalculateHalfPixelUVs(
            Rectangle sourceRect,
            int textureWidth, int textureHeight,
            out float sourceX, out float sourceY,
            out float sourceW, out float sourceH)
        {
            float invW = 1f / textureWidth;
            float invH = 1f / textureHeight;
            sourceX = (sourceRect.X + 0.5f) * invW;
            sourceY = (sourceRect.Y + 0.5f) * invH;
            sourceW = (sourceRect.Width - 1f) * invW;
            sourceH = (sourceRect.Height - 1f) * invH;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureSize()
        {
            EnsureStarted();

            //if (_numSprites >= MAX_SPRITES)
            //{
            //    Flush();
            //}

            if (_numSprites >= _vertexInfo.Length)
            {
                //Flush();

                int newMax = _vertexInfo.Length + MAX_SPRITES;
                Array.Resize(ref _vertexInfo, newMax);
                Array.Resize(ref _textureInfo, newMax);
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

        public void DrawBatch(PositionNormalTextureColor4[] vertices, Texture2D[] textures, sbyte[] zValues, int count, int offsetX, int offsetY, sbyte maxGroundZ)
        {
            if (count == 0)
                return;

            EnsureBatchCapacity(count);

            for (int i = 0; i < count; i++)
            {
                if (zValues[i] > maxGroundZ)
                    continue;

                CopyBatchSprite(vertices, textures, i, offsetX, offsetY);
            }
        }

        public void DrawBatch(PositionNormalTextureColor4[] vertices, Texture2D[] textures, bool[] visible, int count, int offsetX, int offsetY)
        {
            if (count == 0)
                return;

            EnsureBatchCapacity(count);

            for (int i = 0; i < count; i++)
            {
                if (!visible[i])
                    continue;

                CopyBatchSprite(vertices, textures, i, offsetX, offsetY);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureBatchCapacity(int count)
        {
            while (_numSprites + count >= _vertexInfo.Length)
            {
                int newMax = _vertexInfo.Length + MAX_SPRITES;
                Array.Resize(ref _vertexInfo, newMax);
                Array.Resize(ref _textureInfo, newMax);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CopyBatchSprite(PositionNormalTextureColor4[] vertices, Texture2D[] textures, int i, int offsetX, int offsetY)
        {
            var tex = textures[i];
            if (tex == null || tex.IsDisposed)
                return;

            ref var dst = ref _vertexInfo[_numSprites];
            dst = vertices[i];

            if (!_worldOffsetActive)
            {
                dst.Position0.X -= offsetX;
                dst.Position0.Y -= offsetY;
                dst.Position1.X -= offsetX;
                dst.Position1.Y -= offsetY;
                dst.Position2.X -= offsetX;
                dst.Position2.Y -= offsetY;
                dst.Position3.X -= offsetX;
                dst.Position3.Y -= offsetY;
            }

            _textureInfo[_numSprites] = tex;
            _numSprites++;
        }

        public void DirectDraw(Texture2D texture, int spriteStart, int spriteCount)
        {
            InternalDraw(texture, spriteStart, spriteCount);
        }

        public DynamicIndexBuffer GetDynamicIndexBuffer(int requiredIndices)
        {
            if (_dynamicIndexBuffer == null || _dynamicIndexBuffer.IndexCount < requiredIndices)
            {
                _dynamicIndexBuffer?.Dispose();
                _dynamicIndexBuffer = new DynamicIndexBuffer(
                    GraphicsDevice,
                    IndexElementSize.SixteenBits,
                    Math.Max(requiredIndices, 1024),
                    BufferUsage.WriteOnly
                );
            }
            return _dynamicIndexBuffer;
        }

        public void DrawDirectIndexed(Texture2D texture, int startIndex, int primitiveCount, int numVertices)
        {
            GraphicsDevice.Textures[0] = texture;
            _basicUOEffect.Pass.Apply();

            if (_customEffect != null)
            {
                foreach (EffectPass pass in _customEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    GraphicsDevice.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        0,
                        0,
                        numVertices,
                        startIndex,
                        primitiveCount
                    );
                }
            }
            else
            {
                GraphicsDevice.DrawIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    0,
                    0,
                    numVertices,
                    startIndex,
                    primitiveCount
                );
            }
        }

        public void SetWorldOffset(int offsetX, int offsetY)
        {
            Flush();
            ApplyStates();
            _worldOffsetActive = true;
            _basicUOEffect.WorldMatrix.SetValue(Matrix.CreateTranslation(-offsetX, -offsetY, 0));
            _basicUOEffect.Pass.Apply();
        }

        public void ResetWorldOffset()
        {
            Flush();
            _worldOffsetActive = false;
            _basicUOEffect.WorldMatrix.SetValue(Matrix.Identity);
            _basicUOEffect.Pass.Apply();
            GraphicsDevice.SetVertexBuffer(_vertexBuffer);
            GraphicsDevice.Indices = _indexBuffer;
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



            _projectionMatrix.M11 = (float)(2.0 / GraphicsDevice.Viewport.Width);
            _projectionMatrix.M22 = (float)(-2.0 / GraphicsDevice.Viewport.Height);

            Matrix matrix = _projectionMatrix;
            Matrix.CreateOrthographicOffCenter
            (
                0f,
                GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height,
                0,
                short.MinValue,
                short.MaxValue,
                out matrix
            );
            Matrix.Multiply(ref _transformMatrix, ref matrix, out matrix);


            //Matrix halfPixelOffset = Matrix.CreateTranslation(-0.5f, -0.5f, 0);
            //Matrix.Multiply(ref halfPixelOffset, ref matrix, out matrix);

            if (!_worldOffsetActive)
                _basicUOEffect.WorldMatrix.SetValue(Matrix.Identity);
            _basicUOEffect.Viewport.SetValue(new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height));
            _basicUOEffect.MatrixTransform.SetValue(matrix);
            _basicUOEffect.Pass.Apply();
        }

        private void Flush()
        {
            if (_numSprites == 0)
            {
                return;
            }

            ApplyStates();

            int arrayOffset = 0;
        nextbatch:
            ++FlushesDone;

            int batchSize = Math.Min(_numSprites, MAX_SPRITES);
            int baseOff = UpdateVertexBuffer(arrayOffset, batchSize);
            int offset = 0;

            Texture2D curTexture = _textureInfo[arrayOffset];

            for (int i = 1; i < batchSize; ++i)
            {
                Texture2D tex = _textureInfo[arrayOffset + i];

                if (tex != curTexture)
                {
                    ++TextureSwitches;
                    InternalDraw(curTexture, baseOff + offset, i - offset);
                    curTexture = tex;
                    offset = i;
                }
            }

            InternalDraw(curTexture, baseOff + offset, batchSize - offset);

            if (_numSprites > MAX_SPRITES)
            {
                _numSprites -= MAX_SPRITES;
                arrayOffset += MAX_SPRITES;
                goto nextbatch;
            }

            _numSprites = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InternalDraw(Texture texture, int baseSprite, int batchSize)
        {
            GraphicsDevice.Textures[0] = texture;

            if (_customEffect != null)
            {
                foreach (EffectPass pass in _customEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
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
            }
            else
            {
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

        private unsafe int UpdateVertexBuffer(int start, int count)
        {
            int offset;
            SetDataOptions hint;

            if (_currentBufferPosition + count > MAX_SPRITES)
            {
                offset = 0;
                hint = SetDataOptions.Discard;
            }
            else
            {
                offset = _currentBufferPosition;
                hint = SetDataOptions.NoOverwrite;
            }

            fixed (PositionNormalTextureColor4* p = &_vertexInfo[start])
            {
                _vertexBuffer.SetDataPointerEXT
                (
                    offset * PositionNormalTextureColor4.SIZE_IN_BYTES,
                    (IntPtr)p,
                    count * PositionNormalTextureColor4.SIZE_IN_BYTES,
                    hint
                );
            }

            _currentBufferPosition = offset + count;

            return offset;
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

        // ECS UI render path (GuiRenderingPlugin / UoFontPlugin): draw a texture
        // tinted by an arbitrary RGB Color. Color RGB rides in Normal, the
        // SHADER_TEXT_HUE mode + alpha in Hue. Distinct from the Vector3-hue
        // overloads so both rendering conventions coexist on the shared batcher.
        public void Draw(Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Color color, float rotation, Vector2 scale, float depth)
        {
            Vector3 hueVector = new Vector3(0, ShaderHueTranslator.SHADER_RGB_TINT, MathHelper.Clamp(color.A / 255f, 0f, 1f));

            float sourceX, sourceY, sourceW, sourceH;
            if (sourceRectangle.HasValue)
            {
                CalculateUVsSafe(sourceRectangle.Value, texture.Width, texture.Height,
                    out sourceX, out sourceY, out sourceW, out sourceH);
                scale.X *= sourceRectangle.Value.Width;
                scale.Y *= sourceRectangle.Value.Height;
            }
            else
            {
                sourceX = 0.0f;
                sourceY = 0.0f;
                sourceW = 1.0f;
                sourceH = 1.0f;
                scale.X *= texture.Width;
                scale.Y *= texture.Height;
            }

            EnsureSize();

            ref var sprite = ref _vertexInfo[_numSprites];

            var rotationSin = (float)Math.Sin(rotation);
            var rotationCos = (float)Math.Cos(rotation);

            sprite.Position0.X = position.X;
            sprite.Position0.Y = position.Y;
            sprite.Position0.Z = depth;

            sprite.Position1.X = (rotationCos * scale.X) + position.X;
            sprite.Position1.Y = (rotationSin * scale.X) + position.Y;
            sprite.Position1.Z = depth;

            sprite.Position2.X = (-rotationSin * scale.Y) + position.X;
            sprite.Position2.Y = (rotationCos * scale.Y) + position.Y;
            sprite.Position2.Z = depth;

            sprite.Position3.X = ((-rotationSin * scale.Y) + (rotationCos * scale.X) + position.X);
            sprite.Position3.Y = ((rotationCos * scale.Y) + (rotationSin * scale.X) + position.Y);
            sprite.Position3.Z = depth;

            sprite.TextureCoordinate0.X = (_cornerOffsetX[0] * sourceW) + sourceX;
            sprite.TextureCoordinate0.Y = (_cornerOffsetY[0] * sourceH) + sourceY;
            sprite.TextureCoordinate0.Z = 0;

            sprite.TextureCoordinate1.X = (_cornerOffsetX[1] * sourceW) + sourceX;
            sprite.TextureCoordinate1.Y = (_cornerOffsetY[1] * sourceH) + sourceY;
            sprite.TextureCoordinate1.Z = 0;

            sprite.TextureCoordinate2.X = (_cornerOffsetX[2] * sourceW) + sourceX;
            sprite.TextureCoordinate2.Y = (_cornerOffsetY[2] * sourceH) + sourceY;
            sprite.TextureCoordinate2.Z = 0;

            sprite.TextureCoordinate3.X = (_cornerOffsetX[3] * sourceW) + sourceX;
            sprite.TextureCoordinate3.Y = (_cornerOffsetY[3] * sourceH) + sourceY;
            sprite.TextureCoordinate3.Z = 0;

            sprite.Hue0 = hueVector;
            sprite.Hue1 = hueVector;
            sprite.Hue2 = hueVector;
            sprite.Hue3 = hueVector;

            var r = MathHelper.Clamp(color.R / 255f, 0f, 1f);
            var g = MathHelper.Clamp(color.G / 255f, 0f, 1f);
            var b = MathHelper.Clamp(color.B / 255f, 0f, 1f);

            sprite.Normal0.X = r; sprite.Normal0.Y = g; sprite.Normal0.Z = b;
            sprite.Normal1.X = r; sprite.Normal1.Y = g; sprite.Normal1.Z = b;
            sprite.Normal2.X = r; sprite.Normal2.Y = g; sprite.Normal2.Z = b;
            sprite.Normal3.X = r; sprite.Normal3.Y = g; sprite.Normal3.Z = b;

            _textureInfo[_numSprites] = texture;
            ++_numSprites;
        }

        public void Draw(Texture2D texture, Rectangle destinationRectangle, Vector3 color)
            => Draw(texture, destinationRectangle, color, 0f);

        public void Draw(Texture2D texture, Rectangle destinationRectangle, Rectangle? sourceRectangle, Vector3 color)
            => Draw(texture, destinationRectangle, sourceRectangle, color, 0f);

        public void DrawRoundedRectangleFilled(Texture2D texture, Rectangle rectangle, float cornerRadius, Color color, float depth = 0.0f)
        {
            if (cornerRadius <= 0)
            {
                Draw(texture, new Vector2(rectangle.X, rectangle.Y),
                     new Rectangle(0, 0, rectangle.Width, rectangle.Height),
                     color, 0f, Vector2.One, depth);
                return;
            }

            float maxRadius = Math.Min(rectangle.Width, rectangle.Height) * 0.5f;
            cornerRadius = Math.Min(cornerRadius, maxRadius);

            var centerRect = new Rectangle(
                rectangle.X + (int)cornerRadius,
                rectangle.Y,
                rectangle.Width - (int)(cornerRadius * 2),
                rectangle.Height
            );
            if (centerRect.Width > 0)
            {
                Draw(texture, new Vector2(centerRect.X, centerRect.Y),
                     new Rectangle(0, 0, centerRect.Width, centerRect.Height),
                     color, 0f, Vector2.One, depth);
            }

            var leftRect = new Rectangle(
                rectangle.X,
                rectangle.Y + (int)cornerRadius,
                (int)cornerRadius,
                rectangle.Height - (int)(cornerRadius * 2)
            );
            if (leftRect.Width > 0 && leftRect.Height > 0)
            {
                Draw(texture, new Vector2(leftRect.X, leftRect.Y),
                     new Rectangle(0, 0, leftRect.Width, leftRect.Height),
                     color, 0f, Vector2.One, depth);
            }

            var rightRect = new Rectangle(
                rectangle.X + rectangle.Width - (int)cornerRadius,
                rectangle.Y + (int)cornerRadius,
                (int)cornerRadius,
                rectangle.Height - (int)(cornerRadius * 2)
            );
            if (rightRect.Width > 0 && rightRect.Height > 0)
            {
                Draw(texture, new Vector2(rightRect.X, rightRect.Y),
                     new Rectangle(0, 0, rightRect.Width, rightRect.Height),
                     color, 0f, Vector2.One, depth);
            }

            DrawRoundedCorner(texture, rectangle.X + cornerRadius, rectangle.Y + cornerRadius, cornerRadius, color, depth, 0);
            DrawRoundedCorner(texture, rectangle.X + rectangle.Width - cornerRadius, rectangle.Y + cornerRadius, cornerRadius, color, depth, 1);
            DrawRoundedCorner(texture, rectangle.X + rectangle.Width - cornerRadius, rectangle.Y + rectangle.Height - cornerRadius, cornerRadius, color, depth, 2);
            DrawRoundedCorner(texture, rectangle.X + cornerRadius, rectangle.Y + rectangle.Height - cornerRadius, cornerRadius, color, depth, 3);
        }

        // Number of arc segments for a quarter-circle of the given radius,
        // derived from raylib's SMOOTH_CIRCLE_ERROR_RATE (0.5px) so larger
        // corners get more segments. Clamped so tiny radii still look round
        // and huge ones don't explode the vertex count.
        private static int ArcSegments(float radius)
        {
            const float err = 0.5f;
            if (radius <= err)
                return 4;
            float th = MathF.Acos(2f * MathF.Pow(1f - err / radius, 2f) - 1f);
            int seg = (int)(MathF.Ceiling(2f * MathF.PI / th) / 4f);
            return Math.Clamp(seg, 4, 32);
        }

        // raylib uses 180/270/0/90 as the start angle for TL/TR/BR/BL corners.
        // Our quadrant ids match that order (0=TL,1=TR,2=BR,3=BL).
        private static float QuadrantStartAngle(int quadrant) => quadrant switch
        {
            0 => 180f,
            1 => 270f,
            2 => 0f,
            _ => 90f,
        };

        // Flat-colored triangle pushed as a degenerate quad (v3 == v1, so the
        // batcher's second triangle (1,3,2) collapses to zero area and only
        // (0,1,2) draws). Vertices MUST be supplied so (a,b,c) winds clockwise
        // in screen space (y-down) — that is the front face under
        // CullCounterClockwiseFace. Increasing math-angle around a center is CW
        // here, which is how the corner/ring builders order their points.
        private void DrawColoredTriangle(Texture2D texture, Vector2 a, Vector2 b, Vector2 c, Color color, float depth)
        {
            Vector3 hueVector = new Vector3(0, ShaderHueTranslator.SHADER_RGB_TINT, MathHelper.Clamp(color.A / 255f, 0f, 1f));

            EnsureSize();
            ref var sprite = ref _vertexInfo[_numSprites];

            sprite.Position0 = new Vector3(a.X, a.Y, depth);
            sprite.Position1 = new Vector3(b.X, b.Y, depth);
            sprite.Position2 = new Vector3(c.X, c.Y, depth);
            sprite.Position3 = new Vector3(b.X, b.Y, depth); // degenerate

            // 1x1 white texture — any UV samples white; keep the standard corners.
            sprite.TextureCoordinate0 = new Vector3(0f, 0f, 0f);
            sprite.TextureCoordinate1 = new Vector3(1f, 0f, 0f);
            sprite.TextureCoordinate2 = new Vector3(0f, 1f, 0f);
            sprite.TextureCoordinate3 = new Vector3(1f, 0f, 0f);

            sprite.Hue0 = hueVector;
            sprite.Hue1 = hueVector;
            sprite.Hue2 = hueVector;
            sprite.Hue3 = hueVector;

            var r = MathHelper.Clamp(color.R / 255f, 0f, 1f);
            var g = MathHelper.Clamp(color.G / 255f, 0f, 1f);
            var bl = MathHelper.Clamp(color.B / 255f, 0f, 1f);
            var rgb = new Vector3(r, g, bl);
            sprite.Normal0 = rgb;
            sprite.Normal1 = rgb;
            sprite.Normal2 = rgb;
            sprite.Normal3 = rgb;

            _textureInfo[_numSprites] = texture;
            ++_numSprites;
        }

        // Filled quarter-disc as a triangle fan from the corner center, raylib
        // style (rshapes.c DrawRectangleRounded). ~ArcSegments(radius) triangles
        // instead of one sprite per pixel.
        private void DrawRoundedCorner(Texture2D texture, float centerX, float centerY, float radius, Color color, float depth, int quadrant)
        {
            int seg = ArcSegments(radius);
            float start = QuadrantStartAngle(quadrant);
            float step = 90f / seg;
            const float deg2rad = MathF.PI / 180f;
            var center = new Vector2(centerX, centerY);

            float a = start;
            for (int i = 0; i < seg; i++)
            {
                float a0 = a * deg2rad;
                float a1 = (a + step) * deg2rad;
                var p0 = new Vector2(centerX + MathF.Cos(a0) * radius, centerY + MathF.Sin(a0) * radius);
                var p1 = new Vector2(centerX + MathF.Cos(a1) * radius, centerY + MathF.Sin(a1) * radius);
                DrawColoredTriangle(texture, center, p0, p1, color, depth);
                a += step;
            }
        }

        // Anti-aliased rounded-rect outline (border). Straight edges are quads;
        // the four corners are AA arcs covering only the [radius-thickness, radius]
        // band. Used for window/widget framing where Clay emits a Border command
        // with a corner radius.
        public void DrawRoundedRectangleOutline(Texture2D texture, Rectangle rectangle, float cornerRadius, float thickness, Color color, float depth = 0.0f)
        {
            if (thickness <= 0)
                return;
            if (cornerRadius <= 0)
            {
                int t = (int)MathF.Ceiling(thickness);
                Draw(texture, new Vector2(rectangle.X, rectangle.Y), new Rectangle(0, 0, rectangle.Width, t), color, 0f, Vector2.One, depth);
                Draw(texture, new Vector2(rectangle.X, rectangle.Y + rectangle.Height - t), new Rectangle(0, 0, rectangle.Width, t), color, 0f, Vector2.One, depth);
                Draw(texture, new Vector2(rectangle.X, rectangle.Y), new Rectangle(0, 0, t, rectangle.Height), color, 0f, Vector2.One, depth);
                Draw(texture, new Vector2(rectangle.X + rectangle.Width - t, rectangle.Y), new Rectangle(0, 0, t, rectangle.Height), color, 0f, Vector2.One, depth);
                return;
            }

            float maxRadius = Math.Min(rectangle.Width, rectangle.Height) * 0.5f;
            cornerRadius = Math.Min(cornerRadius, maxRadius);
            int th = (int)MathF.Ceiling(thickness);
            int cr = (int)cornerRadius;

            // Top / bottom / left / right straight runs (between the corner arcs).
            int spanX = rectangle.Width - cr * 2;
            int spanY = rectangle.Height - cr * 2;
            if (spanX > 0)
            {
                Draw(texture, new Vector2(rectangle.X + cr, rectangle.Y), new Rectangle(0, 0, spanX, th), color, 0f, Vector2.One, depth);
                Draw(texture, new Vector2(rectangle.X + cr, rectangle.Y + rectangle.Height - th), new Rectangle(0, 0, spanX, th), color, 0f, Vector2.One, depth);
            }
            if (spanY > 0)
            {
                Draw(texture, new Vector2(rectangle.X, rectangle.Y + cr), new Rectangle(0, 0, th, spanY), color, 0f, Vector2.One, depth);
                Draw(texture, new Vector2(rectangle.X + rectangle.Width - th, rectangle.Y + cr), new Rectangle(0, 0, th, spanY), color, 0f, Vector2.One, depth);
            }

            DrawRoundedCornerRing(texture, rectangle.X + cornerRadius, rectangle.Y + cornerRadius, cornerRadius, thickness, color, depth, 0);
            DrawRoundedCornerRing(texture, rectangle.X + rectangle.Width - cornerRadius, rectangle.Y + cornerRadius, cornerRadius, thickness, color, depth, 1);
            DrawRoundedCornerRing(texture, rectangle.X + rectangle.Width - cornerRadius, rectangle.Y + rectangle.Height - cornerRadius, cornerRadius, thickness, color, depth, 2);
            DrawRoundedCornerRing(texture, rectangle.X + cornerRadius, rectangle.Y + rectangle.Height - cornerRadius, cornerRadius, thickness, color, depth, 3);
        }

        // Quarter annulus (corner of a rounded outline) as a strip of trapezoids
        // between the inner and outer arc — geometry, not per-pixel.
        private void DrawRoundedCornerRing(Texture2D texture, float centerX, float centerY, float radius, float thickness, Color color, float depth, int quadrant)
        {
            int seg = ArcSegments(radius);
            float start = QuadrantStartAngle(quadrant);
            float step = 90f / seg;
            const float deg2rad = MathF.PI / 180f;
            float inner = MathF.Max(0f, radius - thickness);

            float a = start;
            for (int i = 0; i < seg; i++)
            {
                float a0 = a * deg2rad;
                float a1 = (a + step) * deg2rad;
                float c0 = MathF.Cos(a0), s0 = MathF.Sin(a0);
                float c1 = MathF.Cos(a1), s1 = MathF.Sin(a1);

                var O0 = new Vector2(centerX + c0 * radius, centerY + s0 * radius);
                var O1 = new Vector2(centerX + c1 * radius, centerY + s1 * radius);
                var I0 = new Vector2(centerX + c0 * inner, centerY + s0 * inner);
                var I1 = new Vector2(centerX + c1 * inner, centerY + s1 * inner);

                // Trapezoid I0-O0-O1-I1, split into two CW triangles.
                DrawColoredTriangle(texture, I0, O0, O1, color, depth);
                DrawColoredTriangle(texture, I0, O1, I1, color, depth);
                a += step;
            }
        }

        // Soft drop shadow for a rounded rect. `rect` is the shadow silhouette
        // (the casting element's box already shifted by the drop offset).
        // Geometry approximation (raylib has no soft shadow): the silhouette is
        // filled once at full shadow alpha, then a handful of expanding rounded
        // outlines with squared alpha falloff fake the blur tail. The element is
        // drawn opaque on top afterwards, so filling the whole silhouette is safe
        // — no per-pixel occluder test needed.
        public void DrawRoundedRectangleShadow(Texture2D texture, Rectangle rect, float cornerRadius, float blur, float offsetX, float offsetY, Color color, float depth = 0.0f)
        {
            if (blur < 1f) blur = 1f;

            DrawRoundedRectangleFilled(texture, rect, cornerRadius, color, depth);

            int rings = Math.Clamp((int)MathF.Ceiling(blur), 1, 8);
            float ringThickness = blur / rings + 1f;
            for (int i = 1; i <= rings; i++)
            {
                float t = i / (float)rings;            // 0..1 outward
                int grow = (int)(blur * t);
                var rr = new Rectangle(
                    rect.X - grow, rect.Y - grow,
                    rect.Width + grow * 2, rect.Height + grow * 2);

                float falloff = 1f - t;
                falloff *= falloff;                    // squared = softer outer tail
                var c = color;
                c.A = (byte)(color.A * falloff);
                if (c.A == 0)
                    continue;

                DrawRoundedRectangleOutline(texture, rr, cornerRadius + grow, ringThickness, c, depth);
            }
        }


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PositionNormalTextureColor4 : IVertexType
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
        }
    }


    public partial class Resources
    {
        [FileEmbed.FileEmbed("shaders/IsometricWorld.fxc")]
        public static partial ReadOnlySpan<byte> GetUOShader();

        [FileEmbed.FileEmbed("shaders/xBR.fxc")]
        public static partial ReadOnlySpan<byte> GetXBRShader();
    }
}
