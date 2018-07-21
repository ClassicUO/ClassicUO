using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using ClassicUO;

namespace ClassicUO.Game.Renderer
{
    public class SpriteBatch3D
    {
        private const int MAX_VERTICES_PER_DRAW = 0x2000;
        private const int INITIAL_TEXTURE_COUNT = 0x800;
        private const float MAX_ACCURATE_SINGLE_FLOAT = 65536;

        private float _z;
        private readonly Microsoft.Xna.Framework.Game _game;
        private readonly short[] _indexBuffer = new short[MAX_VERTICES_PER_DRAW * 6];
        private readonly Queue<List<SpriteVertex>> _vertexQueue = new Queue<List<SpriteVertex>>(INITIAL_TEXTURE_COUNT);
        private readonly Dictionary<Texture2D, List<SpriteVertex>> _drawingQueue = new Dictionary<Texture2D, List<SpriteVertex>>(INITIAL_TEXTURE_COUNT);
        private BoundingBox _drawingArea = new BoundingBox();
        private readonly Vector3 _minVector3 = new Vector3(0, 0, int.MinValue);
        private readonly SpriteVertex[] _vertexBuffer = new SpriteVertex[MAX_VERTICES_PER_DRAW];
        private readonly Effect _effect;
        private readonly DepthStencilState _dss = new DepthStencilState
        {
            DepthBufferEnable = true,
            DepthBufferWriteEnable = true
        };


        public SpriteBatch3D(in Microsoft.Xna.Framework.Game game)
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

            _effect = new Effect(GraphicsDevice, File.ReadAllBytes(Path.Combine(Environment.CurrentDirectory, "IsometricWorld.fxc")));
            _effect.Parameters["HuesPerTexture"].SetValue(3000f);

            _drawLightingEffect = _effect.Parameters["DrawLighting"];
            _projectionMatrixEffect = _effect.Parameters["ProjectionMatrix"];
            _worldMatrixEffect = _effect.Parameters["WorldMatrix"];
            _viewportEffect = _effect.Parameters["Viewport"];

            _huesTechnique = _effect.Techniques["HueTechnique"];
        }


        public GraphicsDevice GraphicsDevice => _game?.GraphicsDevice;
        public Matrix ProjectionMatrixWorld => Matrix.Identity;
        public Matrix ProjectionMatrixScreen => Matrix.CreateOrthographicOffCenter(0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 0f, short.MinValue, short.MaxValue);


        public void SetLightDirection(in Vector3 dir) => _effect.Parameters["lightDirection"].SetValue(dir);
        public void SetLightIntensity(in float inte) => _effect.Parameters["lightIntensity"].SetValue(inte);


        public void BeginDraw()
        {
            _z = 0;
            _drawingArea.Min = _minVector3;
            _drawingArea.Max = new Vector3(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, int.MaxValue);
        }

        public bool DrawSprite(in Texture2D texture, in SpriteVertex[] vertices)
        {
            if (texture == null || vertices.Length != 4)
                return false;

            for (byte i = 0; i < 4; i++)
            {
                if (_drawingArea.Contains(vertices[i].Position) == ContainmentType.Contains)
                {
                    vertices[0].Position.Z =
                        vertices[1].Position.Z =
                        vertices[2].Position.Z =
                        vertices[3].Position.Z = GetZ();

                    GetVertexList(texture).AddRange(vertices);

                    return true;
                }
            }
            return false;
        }

        public float GetZ() => _z++;

        private EffectParameter _drawLightingEffect, _projectionMatrixEffect, _worldMatrixEffect, _viewportEffect;
        private EffectTechnique _huesTechnique;

        public void EndDraw(in bool light = false)
        {
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
            //_effect.Parameters["hues"].SetValue(AssetsLoader.Hues.GetColorForShader(38));

            GraphicsDevice.DepthStencilState = _dss;


            _effect.CurrentTechnique = _huesTechnique;
            _effect.CurrentTechnique.Passes[0].Apply();

            var enumerator = _drawingQueue.GetEnumerator();

            while (enumerator.MoveNext())
            {
                Texture2D texture = enumerator.Current.Key;
                var list = enumerator.Current.Value;

                list.CopyTo(0, _vertexBuffer, 0, list.Count <= MAX_VERTICES_PER_DRAW ? list.Count : MAX_VERTICES_PER_DRAW);

                GraphicsDevice.Textures[0] = texture;
                GraphicsDevice.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    _vertexBuffer,
                    0,
                    Math.Min(list.Count, MAX_VERTICES_PER_DRAW),
                    _indexBuffer,
                    0, list.Count / 2);

                list.Clear();
                _vertexQueue.Enqueue(list);
            }

            //foreach (KeyValuePair<Texture2D, List<SpriteVertex>> k in _drawingQueue)
            //{
            //    k.Value.CopyTo(0, _vertexBuffer, 0, Math.Min(k.Value.Count, MAX_VERTICES_PER_DRAW));

            //    GraphicsDevice.Textures[0] = k.Key;
            //    GraphicsDevice.DrawUserIndexedPrimitives
            //    (
            //        PrimitiveType.TriangleList,
            //        _vertexBuffer,
            //        0,
            //        Math.Min(k.Value.Count, MAX_VERTICES_PER_DRAW),
            //        _indexBuffer,
            //        0, k.Value.Count / 2
            //    );

            //    k.Value.Clear();
            //    _vertexQueue.Enqueue(k.Value);
            //}
            _drawingQueue.Clear();
        }

        private List<SpriteVertex> GetVertexList(in Texture2D texture)
        {
            if (!_drawingQueue.TryGetValue(texture, out var list))
            {
                if (_vertexQueue.Count > 0)
                {
                    list = _vertexQueue.Dequeue();
                    list.Clear();
                }
                else
                {
                    list = new List<SpriteVertex>(1024);
                }
                _drawingQueue[texture] = list;
            }

            return list;
        }
    }
}
