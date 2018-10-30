using System;
using System.IO;
using System.Runtime.CompilerServices;

using ClassicUO.IO.Resources;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer
{
#if SB1
    public class SpriteBatch3D
    {
        private const int MAX_SPRITES = 0x800 ;
        private const int MAX_VERTICES = MAX_SPRITES * 4;
        private const int MAX_INDICES = MAX_SPRITES * 6;

        private Matrix _projectionMatrix;
        private Matrix _matrixTransformMatrix;
        private Matrix _transformMatrix = Matrix.Identity;

        private readonly EffectParameter _viewportEffect;
        private readonly EffectParameter _worldMatrixEffect;
        private readonly EffectParameter _drawLightingEffect;
        private readonly EffectParameter _projectionMatrixEffect;
        private readonly EffectTechnique _huesTechnique, _shadowTechnique, _landTechnique;
        private readonly Effect _effect;
        private readonly DepthStencilState _dss = new DepthStencilState
        {
            DepthBufferEnable = true,
            DepthBufferWriteEnable = true
        };
        private readonly DepthStencilState _dssStencil = new DepthStencilState
        {
            StencilEnable = true,
            StencilFunction = CompareFunction.Always,
            StencilPass = StencilOperation.Replace,
            DepthBufferEnable = false,
        };
        private readonly VertexBuffer _vertexBuffer;
        private readonly IndexBuffer _indexBuffer;
        private readonly DrawInfo[] _textureInfo;
        private readonly SpriteVertex[] _vertexInfo;
        private bool _started;
#if !ORIONSORT
        private float _z;
#endif
        private int _numSprites;

        public SpriteBatch3D(GraphicsDevice device)
        {
            GraphicsDevice = device;
            _effect = new Effect(GraphicsDevice, File.ReadAllBytes(Path.Combine(Bootstrap.ExeDirectory, "shaders/IsometricWorld.fxc")));
            _effect.Parameters["HuesPerTexture"].SetValue((float)Hues.HuesCount);
            _drawLightingEffect = _effect.Parameters["DrawLighting"];
            _projectionMatrixEffect = _effect.Parameters["ProjectionMatrix"];
            _worldMatrixEffect = _effect.Parameters["WorldMatrix"];
            _viewportEffect = _effect.Parameters["Viewport"];
            _huesTechnique = _effect.Techniques["HueTechnique"];
            _shadowTechnique = _effect.Techniques["ShadowSetTechnique"];
            _landTechnique = _effect.Techniques["LandTechnique"];
            _textureInfo = new DrawInfo[MAX_SPRITES];
            _vertexInfo = new SpriteVertex[MAX_VERTICES];
            _vertexBuffer = new DynamicVertexBuffer(GraphicsDevice, SpriteVertex.VertexDeclaration, MAX_VERTICES, BufferUsage.WriteOnly);
            _indexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, MAX_INDICES, BufferUsage.WriteOnly);
            _indexBuffer.SetData(GenerateIndexArray());


            _projectionMatrix = new Matrix(
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
        }

        public GraphicsDevice GraphicsDevice { get; }

        //public Matrix ProjectionMatrixWorld => Matrix.Identity;

        //public Matrix ProjectionMatrixScreen => Matrix.CreateOrthographicOffCenter(0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 0f, short.MinValue, short.MaxValue);

        public int Calls { get; set; }

        public int Merged { get; set; }

        public void SetLightDirection(Vector3 dir)
        {
            _effect.Parameters["lightDirection"].SetValue(dir);
        }

        public void SetLightIntensity(float inte)
        {
            _effect.Parameters["lightIntensity"].SetValue(inte);
        }

        public void EnableLight(bool value)
        {
            _drawLightingEffect.SetValue(value);
        }

#if !ORIONSORT
        public float GetZ() => _z++;
#endif

        public void Begin()
        {
            EnsureNotStarted();
            _started = true;
            Calls = 0;
            Merged = 0;
#if !ORIONSORT
            _z = 0;
#endif
        }

        public void End()
        {
            EnsureStarted();
            Flush();
            _started = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool DrawSprite(Texture2D texture, SpriteVertex[] vertices, Techniques technique = Techniques.Default)
        {
            if (texture == null || texture.IsDisposed)
                return false;

            if (_numSprites >= MAX_SPRITES)
                Flush();
#if !ORIONSORT
            vertices[0].Position.Z = vertices[1].Position.Z = vertices[2].Position.Z = vertices[3].Position.Z = GetZ();
#endif
            _textureInfo[_numSprites] = new DrawInfo(texture, technique);

            for (int i = 0; i < 4; i++)
                _vertexInfo[_numSprites * 4 + i] = vertices[i];
            _numSprites++;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawShadow(Texture2D texture, SpriteVertex[] vertices, Vector2 position, bool flip, float z)
        {
            if (texture == null || texture.IsDisposed)
                return;

            if (_numSprites >= MAX_SPRITES)
                Flush();
#if !ORIONSORT
            vertices[0].Position.Z = vertices[1].Position.Z = vertices[2].Position.Z = vertices[3].Position.Z = z;
#endif
            float skewHorizTop = (vertices[0].Position.Y - position.Y) * .5f;
            float skewHorizBottom = (vertices[3].Position.Y - position.Y) * .5f;
            vertices[0].Position.X -= skewHorizTop;
            vertices[0].Position.Y -= skewHorizTop;
            vertices[flip ? 2 : 1].Position.X -= skewHorizTop;
            vertices[flip ? 2 : 1].Position.Y -= skewHorizTop;
            vertices[flip ? 1 : 2].Position.X -= skewHorizBottom;
            vertices[flip ? 1 : 2].Position.Y -= skewHorizBottom;
            vertices[3].Position.X -= skewHorizBottom;
            vertices[3].Position.Y -= skewHorizBottom;
            _textureInfo[_numSprites] = new DrawInfo(texture, Techniques.ShadowSet);

            for (int i = 0; i < 4; i++)
                _vertexInfo[_numSprites * 4 + i] = vertices[i];
            _numSprites++;
        }


        [System.Diagnostics.Conditional("DEBUG")]
        private void EnsureStarted()
        {
            if (!_started)
                throw new InvalidOperationException();
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private void EnsureNotStarted()
        {
            if (_started)
                throw new InvalidOperationException();
        }

        private void ApplyStates()
        {
            GraphicsDevice.BlendState = BlendState.AlphaBlend;
            GraphicsDevice.DepthStencilState = DepthStencilState.None;
            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
            GraphicsDevice.SamplerStates[1] = SamplerState.PointClamp;
            GraphicsDevice.SamplerStates[2] = SamplerState.PointClamp;


            Viewport viewport = GraphicsDevice.Viewport;
            _projectionMatrix.M11 = (float)(2.0 / (double)(viewport.Width / 2 * 2 - 1));
            _projectionMatrix.M22 = (float)(-2.0 / (double)(viewport.Height / 2 * 2 - 1));
            _projectionMatrix.M41 = -1 - 0.5f * _projectionMatrix.M11;
            _projectionMatrix.M42 = 1 - 0.5f * _projectionMatrix.M22;

            Matrix.Multiply(ref _transformMatrix, ref _projectionMatrix, out _matrixTransformMatrix);
            _projectionMatrixEffect.SetValue(_matrixTransformMatrix);

            //_projectionMatrixEffect.SetValue(ProjectionMatrixScreen);
            _worldMatrixEffect.SetValue(_transformMatrix);

            _viewportEffect.SetValue(new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height));

            GraphicsDevice.SetVertexBuffer(_vertexBuffer);
            GraphicsDevice.Indices = _indexBuffer;
        }

        private void Flush()
        {
            if (_numSprites == 0)
                return;
            int offset = 0;
            Techniques last = Techniques.None;
            ApplyStates();
            _vertexBuffer.SetData(0, _vertexInfo, 0, _numSprites * 4, SpriteVertex.SizeInBytes);
            DrawInfo current = _textureInfo[0];

            for (int i = 1; i < _numSprites; i++)
                if (_textureInfo[i].Texture != current.Texture || _textureInfo[i].Technique != current.Technique)
                {
                    InternalDraw(current, offset, i - offset, ref last);
                    current = _textureInfo[i];
                    offset = i;
                }
                else
                {
                    Merged++;
                }

            InternalDraw(current, offset, _numSprites - offset, ref last);
            Calls += _numSprites;
            _numSprites = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InternalDraw(DrawInfo info, int baseSprite, int batchSize, ref Techniques last)
        {
            switch (info.Technique)
            {
                case Techniques.Hued:

                    if (last != info.Technique)
                    {
                        _effect.CurrentTechnique = _huesTechnique;
                        last = info.Technique;
                        _effect.CurrentTechnique.Passes[0].Apply();
                    }

                    break;
                case Techniques.ShadowSet:

                    if (last != info.Technique)
                    {
                        _effect.CurrentTechnique = _shadowTechnique;
                        last = info.Technique;
                        _effect.CurrentTechnique.Passes[0].Apply();
                    }

                    break;
                case Techniques.Land:

                    if (last != info.Technique)
                    {
                        _effect.CurrentTechnique = _landTechnique;
                        last = info.Technique;
                        _effect.CurrentTechnique.Passes[0].Apply();
                    }

                    break;
            }

            GraphicsDevice.Textures[0] = info.Texture;
            GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, baseSprite * 4, 0, batchSize * 2);
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

        internal struct DrawInfo
        {
            public DrawInfo(Texture2D texture, Techniques technique)
            {
                Texture = texture;
                Technique = technique;
            }

            public readonly Texture2D Texture;
            public readonly Techniques Technique;
        }
    }
#endif
}