using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer
{
    class MatrixEffect : Effect
    {
        public MatrixEffect(GraphicsDevice graphicsDevice, byte[] effectCode) : base(graphicsDevice, effectCode)
        {
        }

        protected MatrixEffect(Effect cloneSource) : base(cloneSource)
        {
        }

        public EffectParameter ProjectionMatrix { get; protected set; }
        public EffectParameter WorldMatrix { get; protected set; }
        public EffectParameter Viewport { get; protected set; }
        public EffectPass Pass => CurrentTechnique.Passes[0];
    }


    internal class UltimaBatcher2D : Batcher2D<SpriteVertex>
    {
        public UltimaBatcher2D(GraphicsDevice device) : base(device, new IsometricEffect(device))
        {
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
                       x + (int)offsetX, y + (int)offsetY,
                       cGlyph.X, cGlyph.Y, cGlyph.Width, cGlyph.Height,
                       color);

                curOffset.X += cKern.Y + cKern.Z;
            }
        }

        public bool DrawSprite(Texture2D texture, int x, int y, int w, int h, int destX, int destY, Vector3 hue)
        {
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

            if (CheckInScreen())
            {
                PushSprite(texture);
                return true;
            }

            return false;
            //EnsureStarted();

            //if (texture == null || texture.IsDisposed)
            //    return false;

            //bool draw = false;


            //int idx = (_numSprites << 2);

            //for (byte i = 0; i < 4; i++)
            //{
            //    if (_drawingArea.Contains(_vertexInfo[idx + i].Position) == ContainmentType.Contains)
            //    {
            //        draw = true;

            //        break;
            //    }
            //}

            //if (!draw)
            //    return false;

            //if (_numSprites >= MAX_SPRITES)
            //    Flush();

            //_textureInfo[_numSprites] = texture;

            ////int idx = _numSprites << 2;

            ////for (int i = 0; i < 4; i++)
            ////    _vertexInfo[idx + i] = vertices[i];

            //_numSprites++;

            //return true;
        }

        public bool DrawSpriteFlipped(Texture2D texture, int x, int y, int w, int h, int destX, int destY, Vector3 hue)
        {
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


            if (CheckInScreen())
            {
                PushSprite(texture);
                return true;
            }

            return false;
        }

        public bool DrawSpriteLand(Texture2D texture, int x, int y, Rectangle rect, ref Vector3[] normals)
        {
            int idx = _numSprites << 2;
            ref var vertex0 = ref _vertexInfo[idx];
            ref var vertex1 = ref _vertexInfo[idx + 1];
            ref var vertex2 = ref _vertexInfo[idx + 2];
            ref var vertex3 = ref _vertexInfo[idx + 3];

            vertex0.Normal = normals[0];
            vertex1.Normal = normals[1];
            vertex2.Normal = normals[2];
            vertex3.Normal = normals[3];

            vertex0.Position.X = x + 22;
            vertex0.Position.Y = y - rect.Left;


            if (CheckInScreen())
            {
                PushSprite(texture);
                return true;
            }

            return false;
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

            //DrawSprite(texture, ref vertices);
        }

        public bool Draw2D(Texture2D texture, int x, int y, Vector3 hue)
        {
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

            if (CheckInScreen())
            {
                PushSprite(texture);
                return true;
            }

            return false;
        }

        public bool Draw2D(Texture2D texture, int x, int y, int sx, int sy, int swidth, int sheight, Vector3 hue)
        {
            float minX = sx / (float)texture.Width;
            float maxX = (sx + swidth) / (float)texture.Width;
            float minY = sy / (float)texture.Height;
            float maxY = (sy + sheight) / (float)texture.Height;


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

            if (CheckInScreen())
            {
                PushSprite(texture);
                return true;
            }

            return false;
        }

        public bool Draw2D(Texture2D texture, int dx, int dy, int dwidth, int dheight, int sx, int sy, int swidth, int sheight, Vector3 hue)
        {
            float minX = sx / (float)texture.Width, maxX = (sx + swidth) / (float)texture.Width;
            float minY = sy / (float)texture.Height, maxY = (sy + sheight) / (float)texture.Height;

            int idx = _numSprites << 2;
            ref var vertex0 = ref _vertexInfo[idx];
            ref var vertex1 = ref _vertexInfo[idx + 1];
            ref var vertex2 = ref _vertexInfo[idx + 2];
            ref var vertex3 = ref _vertexInfo[idx + 3];

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

            if (CheckInScreen())
            {
                PushSprite(texture);
                return true;
            }

            return false;
        }

        public bool Draw2D(Texture2D texture, int x, int y, int width, int height, Vector3 hue)
        {
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

            if (CheckInScreen())
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


        private class IsometricEffect : MatrixEffect
        {
            public IsometricEffect(GraphicsDevice graphicsDevice) : base(graphicsDevice, Resources.IsometricEffect)
            {
                ProjectionMatrix = Parameters["ProjectionMatrix"];
                WorldMatrix = Parameters["WorldMatrix"];
                Viewport = Parameters["Viewport"];
                CurrentTechnique = Techniques["HueTechnique"];
            }

            protected IsometricEffect(Effect cloneSource) : base(cloneSource)
            {
            }

        }
    }

    //internal class FNABatcher2D : Batcher2D<VertexPositionColorTexture>
    //{
    //    public FNABatcher2D(GraphicsDevice device) : base(device, new BasicEffect(device))
    //    {

    //    }

    //    public bool Draw(Texture2D texture, float x, float y, Color color)
    //    {
    //        //unsafe
    //        //{
    //        //    fixed (VertexPositionColorTexture* t = &_vertexInfo[_numSprites])
    //        //    {
    //        //        return DrawSprite(texture, ref t);

    //        //    }
    //        //}

            


    //        return false;
    //    }

    //    public override bool DrawSprite(Texture2D texture, ref VertexPositionColorTexture[] vertices)
    //    {
    //        EnsureStarted();

    //        if (texture == null || texture.IsDisposed)
    //            return false;

    //        if (_numSprites >= MAX_SPRITES)
    //            Flush();

    //        _textureInfo[_numSprites] = texture;

    //        int idx = _numSprites << 4;

    //        for (int i = 0; i < 4; i++)
    //            _vertexInfo[idx + i] = vertices[i];

    //        _numSprites++;

    //        return true;
    //    }
    //}


    internal abstract class Batcher2D<T> where T : struct, IVertexType
    {
        protected const int MAX_SPRITES = 0x800;
        protected const int MAX_VERTICES = MAX_SPRITES * 4;
        protected const int MAX_INDICES = MAX_SPRITES * 6;

        private readonly VertexBuffer _vertexBuffer;
        private readonly IndexBuffer _indexBuffer;

        private readonly Vector3 _minVector3 = new Vector3(0, 0, -150);
        private Vector2 _viewportVector;
        private readonly RasterizerState _rasterizerState;
        protected readonly Texture2D[] _textureInfo;
        protected readonly T[] _vertexInfo;
        private BlendState _blendState;
        private Effect _customEffect;
        protected BoundingBox _drawingArea;
        protected Matrix _matrixTransformMatrix;
        private readonly Effect _defaultEffect;

        protected int _numSprites;

        protected Matrix _projectionMatrix;
        private bool _started;
        private DepthStencilState _stencil;
        protected Matrix _transformMatrix = Matrix.Identity;

        private bool _useScissor;

        protected Batcher2D(GraphicsDevice device, Effect defaultEffect)
        {
            GraphicsDevice = device;
            _textureInfo = new Texture2D[MAX_SPRITES];
            _vertexInfo = new T[MAX_VERTICES];
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

        public Matrix TransformMatrix => _transformMatrix;

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

        protected virtual bool CheckInScreen() => true;

        protected bool PushSprite(Texture2D texture)
        {
            if (_numSprites >= MAX_SPRITES)
                Flush();


            if (CheckInScreen())
            {
                _textureInfo[_numSprites++] = texture;
                return true;
            }

            return false;
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

            Viewport viewport = GraphicsDevice.Viewport;
            _projectionMatrix.M11 = (float) (2.0 / viewport.Width);
            _projectionMatrix.M22 = (float) (-2.0 / viewport.Height);

            Matrix.Multiply(ref _transformMatrix, ref _projectionMatrix, out _matrixTransformMatrix);

            GraphicsDevice.SetVertexBuffer(_vertexBuffer);
            GraphicsDevice.Indices = _indexBuffer;


            if (_defaultEffect is MatrixEffect matrixEffect)
            {
                matrixEffect.ProjectionMatrix.SetValue(_matrixTransformMatrix);
                matrixEffect.WorldMatrix.SetValue(_transformMatrix);
                _viewportVector.X = GraphicsDevice.Viewport.Width;
                _viewportVector.Y = GraphicsDevice.Viewport.Height;
                matrixEffect.Viewport.SetValue(_viewportVector);
                matrixEffect.Pass.Apply();
            }
        }

        protected void Flush()
        {
            ApplyStates();

            if (_numSprites == 0)
                return;

            _vertexBuffer.SetData(_vertexInfo, 0, _numSprites << 2);

            Texture2D current = _textureInfo[0];
            int offset = 0;

            if (_customEffect != null)
                _customEffect.CurrentTechnique.Passes[0].Apply();

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


    }

    internal class Resources
    {
        private static byte[] _isometricEffect;

        public static byte[] IsometricEffect => _isometricEffect ?? (_isometricEffect = GetResource("ClassicUO.shaders.IsometricWorld.fxc"));

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
}