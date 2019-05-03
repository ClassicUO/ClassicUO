using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer
{
    internal class Batcher2D
    {
        private const int MAX_SPRITES = 0x800;
        private const int MAX_VERTICES = MAX_SPRITES * 4;
        private const int MAX_INDICES = MAX_SPRITES * 6;

        private readonly IndexBuffer _indexBuffer;
        private readonly IsometricEffect _isometricEffect;
        private readonly Vector3 _minVector3 = new Vector3(0, 0, -150);
        private Vector2 _viewportVector;
        private readonly RasterizerState _rasterizerState;
        private readonly Texture2D[] _textureInfo;
        private readonly VertexBuffer _vertexBuffer;
        private readonly SpriteVertex[] _vertexInfo;
        private BlendState _blendState;
        private Effect _customEffect;
        private BoundingBox _drawingArea;
        private Matrix _matrixTransformMatrix;

        private int _numSprites;

        private Matrix _projectionMatrix;
        private bool _started;
        private DepthStencilState _stencil;
        private Matrix _transformMatrix = Matrix.Identity;

        private bool _useScissor;
        private SpriteVertex[] _vertexBufferUI = new SpriteVertex[4];

        public Batcher2D(GraphicsDevice device)
        {
            GraphicsDevice = device;

            _isometricEffect = new IsometricEffect(device);

            _textureInfo = new Texture2D[MAX_SPRITES];
            _vertexInfo = new SpriteVertex[MAX_VERTICES];
            _vertexBuffer = new DynamicVertexBuffer(GraphicsDevice, SpriteVertex.VertexDeclaration, MAX_VERTICES, BufferUsage.WriteOnly);
            _indexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, MAX_INDICES, BufferUsage.WriteOnly);
            _indexBuffer.SetData(GenerateIndexArray());

            _projectionMatrix = new Matrix(0f, //(float)( 2.0 / (double)viewport.Width ) is the actual value we will use
                                           0.0f, 0.0f, 0.0f, 0.0f, 0f, //(float)( -2.0 / (double)viewport.Height ) is the actual value we will use
                                           0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, -1.0f, 1.0f, 0.0f, 1.0f);
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

        public Matrix TransformMatrix => _transformMatrix;

        public GraphicsDevice GraphicsDevice { get; }

        public void SetLightDirection(Vector3 dir)
        {
            _isometricEffect.Parameters["lightDirection"].SetValue(dir);
        }

        public void SetLightIntensity(float inte)
        {
            _isometricEffect.Parameters["lightIntensity"].SetValue(inte);
        }

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

            _drawingArea.Min = _minVector3;
            _drawingArea.Max.X = GraphicsDevice.Viewport.Width;
            _drawingArea.Max.Y = GraphicsDevice.Viewport.Height;
            _drawingArea.Max.Z = 150;

            _customEffect = customEffect;
        }

        public void End()
        {
            EnsureStarted();
            Flush();
            _started = false;
            _customEffect = null;
        }


        private static bool IsInside(float value, float min, float max) 
            => value >= min && value <= max;

        public bool DrawSprite(Texture2D texture, ref SpriteVertex[] vertices)
        {
            EnsureStarted();

            if (texture == null || texture.IsDisposed)
                return false;

            bool draw = false;

            ref readonly Vector3 posTopLeft = ref vertices[0].Position;
            ref readonly Vector3 posTopRight= ref vertices[1].Position;
            ref readonly Vector3 posBottomLeft = ref vertices[2].Position;
            ref readonly Vector3 posBottomRight = ref vertices[3].Position;

            draw = IsInside(posTopLeft.X, _drawingArea.Min.X, _drawingArea.Max.X) ||
                      IsInside(_drawingArea.Min.X, posTopLeft.X, posTopRight.X) ||
                      IsInside(_drawingArea.Max.X, posTopLeft.X, posTopRight.X) ||
                      IsInside(posTopLeft.Y, _drawingArea.Min.Y, _drawingArea.Max.Y) ||
                      IsInside(_drawingArea.Min.Y, posTopLeft.Y, posTopRight.Y) ||
                      IsInside(_drawingArea.Max.X, posTopLeft.Y, posTopRight.Y) ||
                      IsInside(posTopRight.X, _drawingArea.Min.X, _drawingArea.Max.X) ||
                      IsInside(posTopRight.Y, _drawingArea.Min.Y, _drawingArea.Max.Y) ||               
                      IsInside(posBottomLeft.X, _drawingArea.Min.X, _drawingArea.Max.X) ||
                      IsInside(_drawingArea.Min.X, posBottomLeft.X, posTopRight.X) ||
                      IsInside(_drawingArea.Max.X, posBottomLeft.X, posTopRight.X) ||                     
                      IsInside(posBottomLeft.Y, _drawingArea.Min.Y, _drawingArea.Max.Y) ||
                      IsInside(_drawingArea.Min.Y, posBottomLeft.Y, posBottomRight.Y) ||
                      IsInside(_drawingArea.Max.X, posBottomLeft.Y, posBottomRight.Y) ||                     
                      IsInside(posBottomRight.X, _drawingArea.Min.X, _drawingArea.Max.X) ||
                      IsInside(posBottomRight.Y, _drawingArea.Min.Y, _drawingArea.Max.Y);


            //for (byte i = 0; i < 4; i++)
            //{
            //    ref readonly Vector3 pos = ref vertices[i]
            //        .Position;

                

            //    if ((pos.X >= _drawingArea.Min.X && pos.X <= _drawingArea.Max.X) || 
            //        (_drawingArea.Min.X >= pos.X || _drawingArea.Max.X <= pos.X) ||

            //        (pos.Y >= _drawingArea.Min.Y || pos.Y <= _drawingArea.Max.Y) ||
            //        (_drawingArea.Min.Y >= pos.Y && _drawingArea.Max.Y <= pos.Y))
            //    {
            //        draw = true;
            //        break;
            //    }

            //    //if (_drawingArea.Contains(vertices[i].Position) == ContainmentType.Contains)
            //    //{
            //    //    draw = true;

            //    //    break;
            //    //}
            //}

            if (!draw)
                return false;

            if (_numSprites >= MAX_SPRITES)
                Flush();

            _textureInfo[_numSprites] = texture;

            int idx = _numSprites * 4;

            for (int i = 0; i < 4; i++)
                _vertexInfo[idx + i] = vertices[i];

            _numSprites++;

            return true;
        }

        public void DrawShadow(Texture2D texture, ref SpriteVertex[] vertices, int x, int y, bool flip)
        {
            if (texture == null || texture.IsDisposed)
                return;

            if (_numSprites >= MAX_SPRITES)
                Flush();

            float skewHorizTop = (vertices[0].Position.Y - y) * .5f;
            float skewHorizBottom = (vertices[3].Position.Y - y) * .5f;
            vertices[0].Position.X -= skewHorizTop;
            vertices[0].Position.Y -= skewHorizTop;

            int index = flip ? 2 : 1;
            vertices[index].Position.X -= skewHorizTop;
            vertices[index].Position.Y -= skewHorizTop;
            vertices[index].Position.X -= skewHorizBottom;
            vertices[index].Position.Y -= skewHorizBottom;
            vertices[3].Position.X -= skewHorizBottom;
            vertices[3].Position.Y -= skewHorizBottom;
            _textureInfo[_numSprites] = texture;

            int idx = _numSprites * 4;

            for (int i = 0; i < 4; i++)
                _vertexInfo[idx + i] = vertices[i];

            _numSprites++;

            DrawSprite(texture, ref vertices);
        }

        public bool Draw2D(Texture2D texture, int x, int y, Vector3 hue)
        {
            _vertexBufferUI[0].Position.X = x;
            _vertexBufferUI[0].Position.Y = y;
            _vertexBufferUI[0].Position.Z = 0;
            _vertexBufferUI[0].Normal.X = 0;
            _vertexBufferUI[0].Normal.Y = 0;
            _vertexBufferUI[0].Normal.Z = 1;
            _vertexBufferUI[0].TextureCoordinate = Vector3.Zero;
            _vertexBufferUI[1].Position.X = x + texture.Width;
            _vertexBufferUI[1].Position.Y = y;
            _vertexBufferUI[1].Position.Z = 0;
            _vertexBufferUI[1].Normal.X = 0;
            _vertexBufferUI[1].Normal.Y = 0;
            _vertexBufferUI[1].Normal.Z = 1;
            _vertexBufferUI[1].TextureCoordinate.X = 1;
            _vertexBufferUI[1].TextureCoordinate.Y = 0;
            _vertexBufferUI[1].TextureCoordinate.Z = 0;
            _vertexBufferUI[2].Position.X = x;
            _vertexBufferUI[2].Position.Y = y + texture.Height;
            _vertexBufferUI[2].Position.Z = 0;
            _vertexBufferUI[2].Normal.X = 0;
            _vertexBufferUI[2].Normal.Y = 0;
            _vertexBufferUI[2].Normal.Z = 1;
            _vertexBufferUI[2].TextureCoordinate.X = 0;
            _vertexBufferUI[2].TextureCoordinate.Y = 1;
            _vertexBufferUI[2].TextureCoordinate.Z = 0;
            _vertexBufferUI[3].Position.X = x + texture.Width;
            _vertexBufferUI[3].Position.Y = y + texture.Height;
            _vertexBufferUI[3].Position.Z = 0;
            _vertexBufferUI[3].Normal.X = 0;
            _vertexBufferUI[3].Normal.Y = 0;
            _vertexBufferUI[3].Normal.Z = 1;
            _vertexBufferUI[3].TextureCoordinate.X = 1;
            _vertexBufferUI[3].TextureCoordinate.Y = 1;
            _vertexBufferUI[3].TextureCoordinate.Z = 0;
            _vertexBufferUI[0].Hue = _vertexBufferUI[1].Hue = _vertexBufferUI[2].Hue = _vertexBufferUI[3].Hue = hue;

            return DrawSprite(texture, ref _vertexBufferUI);
        }

        public bool Draw2D(Texture2D texture, int x, int y, int sx, int sy, int swidth, int sheight, Vector3 hue)
        {
            float minX = sx / (float) texture.Width;
            float maxX = (sx + swidth) / (float) texture.Width;
            float minY = sy / (float) texture.Height;
            float maxY = (sy + sheight) / (float) texture.Height;

            _vertexBufferUI[0].Position.X = x;
            _vertexBufferUI[0].Position.Y = y;
            _vertexBufferUI[0].Position.Z = 0;
            _vertexBufferUI[0].Normal.X = 0;
            _vertexBufferUI[0].Normal.Y = 0;
            _vertexBufferUI[0].Normal.Z = 1;
            _vertexBufferUI[0].TextureCoordinate.X = minX;
            _vertexBufferUI[0].TextureCoordinate.Y = minY;
            _vertexBufferUI[0].TextureCoordinate.Z = 0;
            _vertexBufferUI[1].Position.X = x + swidth;
            _vertexBufferUI[1].Position.Y = y;
            _vertexBufferUI[1].Position.Z = 0;
            _vertexBufferUI[1].Normal.X = 0;
            _vertexBufferUI[1].Normal.Y = 0;
            _vertexBufferUI[1].Normal.Z = 1;
            _vertexBufferUI[1].TextureCoordinate.X = maxX;
            _vertexBufferUI[1].TextureCoordinate.Y = minY;
            _vertexBufferUI[1].TextureCoordinate.Z = 0;
            _vertexBufferUI[2].Position.X = x;
            _vertexBufferUI[2].Position.Y = y + sheight;
            _vertexBufferUI[2].Position.Z = 0;
            _vertexBufferUI[2].Normal.X = 0;
            _vertexBufferUI[2].Normal.Y = 0;
            _vertexBufferUI[2].Normal.Z = 1;
            _vertexBufferUI[2].TextureCoordinate.X = minX;
            _vertexBufferUI[2].TextureCoordinate.Y = maxY;
            _vertexBufferUI[2].TextureCoordinate.Z = 0;
            _vertexBufferUI[3].Position.X = x + swidth;
            _vertexBufferUI[3].Position.Y = y + sheight;
            _vertexBufferUI[3].Position.Z = 0;
            _vertexBufferUI[3].Normal.X = 0;
            _vertexBufferUI[3].Normal.Y = 0;
            _vertexBufferUI[3].Normal.Z = 1;
            _vertexBufferUI[3].TextureCoordinate.X = maxX;
            _vertexBufferUI[3].TextureCoordinate.Y = maxY;
            _vertexBufferUI[3].TextureCoordinate.Z = 0;
            _vertexBufferUI[0].Hue = _vertexBufferUI[1].Hue = _vertexBufferUI[2].Hue = _vertexBufferUI[3].Hue = hue;

            return DrawSprite(texture, ref _vertexBufferUI);
        }

        public bool Draw2D(Texture2D texture, int dx, int dy, int dwidth, int dheight, int sx, int sy, int swidth, int sheight, Vector3 hue)
        {
            float minX = sx / (float) texture.Width, maxX = (sx + swidth) / (float) texture.Width;
            float minY = sy / (float) texture.Height, maxY = (sy + sheight) / (float) texture.Height;

            _vertexBufferUI[0].Position.X = dx;
            _vertexBufferUI[0].Position.Y = dy;
            _vertexBufferUI[0].Position.Z = 0;
            _vertexBufferUI[0].Normal.X = 0;
            _vertexBufferUI[0].Normal.Y = 0;
            _vertexBufferUI[0].Normal.Z = 1;
            _vertexBufferUI[0].TextureCoordinate.X = minX;
            _vertexBufferUI[0].TextureCoordinate.Y = minY;
            _vertexBufferUI[0].TextureCoordinate.Z = 0;
            _vertexBufferUI[1].Position.X = dx + dwidth;
            _vertexBufferUI[1].Position.Y = dy;
            _vertexBufferUI[1].Position.Z = 0;
            _vertexBufferUI[1].Normal.X = 0;
            _vertexBufferUI[1].Normal.Y = 0;
            _vertexBufferUI[1].Normal.Z = 1;
            _vertexBufferUI[1].TextureCoordinate.X = maxX;
            _vertexBufferUI[1].TextureCoordinate.Y = minY;
            _vertexBufferUI[1].TextureCoordinate.Z = 0;
            _vertexBufferUI[2].Position.X = dx;
            _vertexBufferUI[2].Position.Y = dy + dheight;
            _vertexBufferUI[2].Position.Z = 0;
            _vertexBufferUI[2].Normal.X = 0;
            _vertexBufferUI[2].Normal.Y = 0;
            _vertexBufferUI[2].Normal.Z = 1;
            _vertexBufferUI[2].TextureCoordinate.X = minX;
            _vertexBufferUI[2].TextureCoordinate.Y = maxY;
            _vertexBufferUI[2].TextureCoordinate.Z = 0;
            _vertexBufferUI[3].Position.X = dx + dwidth;
            _vertexBufferUI[3].Position.Y = dy + dheight;
            _vertexBufferUI[3].Position.Z = 0;
            _vertexBufferUI[3].Normal.X = 0;
            _vertexBufferUI[3].Normal.Y = 0;
            _vertexBufferUI[3].Normal.Z = 1;
            _vertexBufferUI[3].TextureCoordinate.X = maxX;
            _vertexBufferUI[3].TextureCoordinate.Y = maxY;
            _vertexBufferUI[3].TextureCoordinate.Z = 0;
            _vertexBufferUI[0].Hue = _vertexBufferUI[1].Hue = _vertexBufferUI[2].Hue = _vertexBufferUI[3].Hue = hue;

            return DrawSprite(texture, ref _vertexBufferUI);
        }

        public bool Draw2D(Texture2D texture, int x, int y, int width, int height, Vector3 hue)
        {
            _vertexBufferUI[0].Position.X = x;
            _vertexBufferUI[0].Position.Y = y;
            _vertexBufferUI[0].Position.Z = 0;
            _vertexBufferUI[0].Normal.X = 0;
            _vertexBufferUI[0].Normal.Y = 0;
            _vertexBufferUI[0].Normal.Z = 1;
            _vertexBufferUI[0].TextureCoordinate = Vector3.Zero;
            _vertexBufferUI[1].Position.X = x + width;
            _vertexBufferUI[1].Position.Y = y;
            _vertexBufferUI[1].Position.Z = 0;
            _vertexBufferUI[1].Normal.X = 0;
            _vertexBufferUI[1].Normal.Y = 0;
            _vertexBufferUI[1].Normal.Z = 1;
            _vertexBufferUI[1].TextureCoordinate.X = 1;
            _vertexBufferUI[1].TextureCoordinate.Y = 0;
            _vertexBufferUI[1].TextureCoordinate.Z = 0;
            _vertexBufferUI[2].Position.X = x;
            _vertexBufferUI[2].Position.Y = y + height;
            _vertexBufferUI[2].Position.Z = 0;
            _vertexBufferUI[2].Normal.X = 0;
            _vertexBufferUI[2].Normal.Y = 0;
            _vertexBufferUI[2].Normal.Z = 1;
            _vertexBufferUI[2].TextureCoordinate.X = 0;
            _vertexBufferUI[2].TextureCoordinate.Y = 1;
            _vertexBufferUI[2].TextureCoordinate.Z = 0;
            _vertexBufferUI[3].Position.X = x + width;
            _vertexBufferUI[3].Position.Y = y + height;
            _vertexBufferUI[3].Position.Z = 0;
            _vertexBufferUI[3].Normal.X = 0;
            _vertexBufferUI[3].Normal.Y = 0;
            _vertexBufferUI[3].Normal.Z = 1;
            _vertexBufferUI[3].TextureCoordinate.X = 1;
            _vertexBufferUI[3].TextureCoordinate.Y = 1;
            _vertexBufferUI[3].TextureCoordinate.Z = 0;
            _vertexBufferUI[0].Hue = _vertexBufferUI[1].Hue = _vertexBufferUI[2].Hue = _vertexBufferUI[3].Hue = hue;

            return DrawSprite(texture, ref _vertexBufferUI);
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



        public void DrawString(SpriteFont spriteFont, string text, int x, int y, Vector3 color)
        {
            if (text == null) throw new ArgumentNullException("text");

            if (text.Length == 0) return;

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

            Viewport viewport = GraphicsDevice.Viewport;
            _projectionMatrix.M11 = (float) (2.0 / viewport.Width);
            _projectionMatrix.M22 = (float) (-2.0 / viewport.Height);

            Matrix.Multiply(ref _transformMatrix, ref _projectionMatrix, out _matrixTransformMatrix);

            _isometricEffect.ProjectionMatrix.SetValue(_matrixTransformMatrix);
            _isometricEffect.WorldMatrix.SetValue(_transformMatrix);


            _viewportVector.X = GraphicsDevice.Viewport.Width;
            _viewportVector.Y = GraphicsDevice.Viewport.Height;
            _isometricEffect.Viewport.SetValue(_viewportVector);

            GraphicsDevice.SetVertexBuffer(_vertexBuffer);
            GraphicsDevice.Indices = _indexBuffer;

            _isometricEffect.Pass.Apply();
        }

        private unsafe void Flush()
        {
            ApplyStates();

            if (_numSprites == 0)
                return;

            fixed (SpriteVertex* p = &_vertexInfo[0])
                _vertexBuffer.SetDataPointerEXT(0, (IntPtr) p, _numSprites * SpriteVertex.SizeInBytes, SetDataOptions.Discard);

            Texture2D current = _textureInfo[0];
            int offset = 0;


            if (_customEffect != null) _customEffect.CurrentTechnique.Passes[0].Apply();


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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InternalDraw(Texture2D texture, int baseSprite, int batchSize)
        {
            GraphicsDevice.Textures[0] = texture;
            GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, baseSprite * 4, 0, batchSize * 4, 0, batchSize * 2);
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


        private class IsometricEffect : Effect
        {
            public IsometricEffect(GraphicsDevice graphicsDevice) : base(graphicsDevice, Resources.IsometricEffect)
            {
                Parameters["HuesPerTexture"].SetValue(3000.0f);
                CanDrawLight = Parameters["DrawLighting"];
                ProjectionMatrix = Parameters["ProjectionMatrix"];
                WorldMatrix = Parameters["WorldMatrix"];
                Viewport = Parameters["Viewport"];
                CurrentTechnique = Techniques["HueTechnique"];
            }

            protected IsometricEffect(Effect cloneSource) : base(cloneSource)
            {
            }

            public EffectParameter CanDrawLight { get; }
            public EffectParameter ProjectionMatrix { get; }
            public EffectParameter WorldMatrix { get; }
            public EffectParameter Viewport { get; }
            public EffectPass Pass => CurrentTechnique.Passes[0];
        }
    }

    internal class Resources
    {
        private static byte[] _isometricEffect;

        public static byte[] IsometricEffect => _isometricEffect ?? (_isometricEffect = GetResource("ClassicUO.shaders.IsometricWorld.fxc"));

        private static byte[] GetResource(string name)
        {
            Stream stream = typeof(Batcher2D).Assembly.GetManifestResourceStream(
                                                                                 name
                                                                                );

            using (MemoryStream ms = new MemoryStream())
            {
                stream.CopyTo(ms);

                return ms.ToArray();
            }
        }
    }
}