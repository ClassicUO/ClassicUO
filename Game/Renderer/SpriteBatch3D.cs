using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace ClassicUO.Game.Renderer
{
    public class SpriteBatch3D
    {
        private const int MAX_VERTICES_PER_DRAW = 0x8000;
        private const int INITIAL_TEXTURE_COUNT = 0x800;
        private const float MAX_ACCURATE_SINGLE_FLOAT = 65536;

        private readonly DepthStencilState _dss = new DepthStencilState { DepthBufferEnable = true, DepthBufferWriteEnable = true };
        private readonly Microsoft.Xna.Framework.Game _game;
        private readonly short[] _indexBuffer = new short[MAX_VERTICES_PER_DRAW * 6];
        private readonly Vector3 _minVector3 = new Vector3(0, 0, int.MinValue);
        private readonly SpriteVertex[] _vertexBuffer = new SpriteVertex[MAX_VERTICES_PER_DRAW];

        private readonly EffectTechnique _huesTechnique;
        private readonly Effect _effect;
        private readonly EffectParameter _viewportEffect;
        private readonly EffectParameter _worldMatrixEffect;
        private readonly EffectParameter _projectionMatrixEffect;
        private readonly EffectParameter _drawLightingEffect;

        private BoundingBox _drawingArea;
        private float _z;
        private readonly DrawCallInfo[] _drawCalls;

        public SpriteBatch3D(Microsoft.
            Xna.Framework.Game game)
        {
            _game = game;

            for (int i = 0; i < MAX_VERTICES_PER_DRAW; i++)
            {
                _indexBuffer[i * 6] = (short)(i * 4);
                _indexBuffer[i * 6 + 1] = (short)(i * 4 + 1);
                _indexBuffer[i * 6 + 2] = (short)(i * 4 + 2);
                _indexBuffer[i * 6 + 3] = (short)(i * 4 + 2);
                _indexBuffer[i * 6 + 4] = (short)(i * 4 + 1);
                _indexBuffer[i * 6 + 5] = (short)(i * 4 + 3);
            }

            _effect = new Effect(GraphicsDevice, File.ReadAllBytes(Path.Combine(Environment.CurrentDirectory, "Graphic/Shaders/IsometricWorld.fxc")));
            _effect.Parameters["HuesPerTexture"].SetValue(/*IO.Resources.Hues.HuesCount*/3000f);

            _drawLightingEffect = _effect.Parameters["DrawLighting"];
            _projectionMatrixEffect = _effect.Parameters["ProjectionMatrix"];
            _worldMatrixEffect = _effect.Parameters["WorldMatrix"];
            _viewportEffect = _effect.Parameters["Viewport"];

            _huesTechnique = _effect.Techniques["HueTechnique"];


            _drawCalls = new DrawCallInfo[MAX_VERTICES_PER_DRAW];
        }


        public GraphicsDevice GraphicsDevice => _game?.GraphicsDevice;
        public Matrix ProjectionMatrixWorld => Matrix.Identity;
        public Matrix ProjectionMatrixScreen => Matrix.CreateOrthographicOffCenter(0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 0f, short.MinValue, short.MaxValue);


        public void SetLightDirection(Vector3 dir)
        {
            _effect.Parameters["lightDirection"].SetValue(dir);
        }

        public void SetLightIntensity(float inte)
        {
            _effect.Parameters["lightIntensity"].SetValue(inte);
        }

        public float GetZ() => _z++;

        public void BeginDraw()
        {
            _z = 0;
            _drawingArea.Min = _minVector3;
            _drawingArea.Max = new Vector3(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, int.MaxValue);
        }

        public bool DrawSprite(Texture2D texture, SpriteVertex[] vertices)
        {
            if (texture == null || texture.IsDisposed)
            {
                return false;
            }

            bool draw = false;

            for (byte i = 0; i < 4; i++)
            {
                if (_drawingArea.Contains(vertices[i].Position) == ContainmentType.Contains)
                {
                    draw = true;
                    break;
                }
            }

            if (!draw)
                return false;

            vertices[0].Position.Z = vertices[1].Position.Z = vertices[2].Position.Z = vertices[3].Position.Z = GetZ();


            var call = new DrawCallInfo(texture, _callsCount, 4);

            Array.Copy(vertices, 0,  _vertexBuffer, _callsCount, 4);
            _callsCount += call.Count;

            Enqueue(ref call);

            //GetVertexList(texture).AddRange(vertices);

            return true;
        }

        private int _callsCount = 0;
        private int _enqueuedDrawCalls = 0;
        private DynamicVertexBuffer _buffer;


        public void EndDraw(bool light = false)
        {
            if (_enqueuedDrawCalls == 0)
                return;

            GraphicsDevice.BlendState = BlendState.AlphaBlend;
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
            GraphicsDevice.SamplerStates[1] = SamplerState.PointClamp;

            //GraphicsDevice.SamplerStates[2] = SamplerState.PointClamp;
            //GraphicsDevice.SamplerStates[3] = SamplerState.PointClamp;

            //GraphicsDevice.SamplerStates[4] = SamplerState.PointWrap;

            _drawLightingEffect.SetValue(light);
            // set up viewport.
            _projectionMatrixEffect.SetValue(ProjectionMatrixScreen);
            _worldMatrixEffect.SetValue(ProjectionMatrixWorld);
            _viewportEffect.SetValue(new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height));

            GraphicsDevice.DepthStencilState = _dss;


            _effect.CurrentTechnique = _huesTechnique;
            _effect.CurrentTechnique.Passes[0].Apply();

            for (int i = 0; i < _enqueuedDrawCalls; i++)
            {
                ref var call = ref _drawCalls[i];
                GraphicsDevice.Textures[0] = call.Texture;

                GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _vertexBuffer, call.StartIndex, call.Count, _indexBuffer, 0, call.Count / 2); 
            }

            Array.Clear(_drawCalls, 0, _enqueuedDrawCalls);
            _enqueuedDrawCalls = 0;
            _callsCount = 0;
        }

        private void Enqueue(ref DrawCallInfo call)
        {
            if (_enqueuedDrawCalls > 0 && call.TryMerge(ref _drawCalls[_enqueuedDrawCalls - 1]))
                return;

            _drawCalls[_enqueuedDrawCalls++] = call;
        }


        struct DrawCallInfo : IComparable<DrawCallInfo>
        {
            public DrawCallInfo(Texture2D texture, int start, int count)
            {
                Texture = texture;
                TextureKey = (uint)RuntimeHelpers.GetHashCode(texture);
                StartIndex = start;
                Count = count;
            }

            public readonly Texture2D Texture;
            public readonly uint TextureKey;
            public readonly int StartIndex;
            public int Count;

            public bool TryMerge(ref DrawCallInfo callInfo)
            {
                if (TextureKey != callInfo.TextureKey)
                    return false;
                callInfo.Count += Count;
                return true;
            }

            public int CompareTo(DrawCallInfo other)
            {
                return TextureKey.CompareTo(other.TextureKey);
            }
        }

    }
}