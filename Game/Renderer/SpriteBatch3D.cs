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
        private const int MAX_VERTICES_PER_DRAW = 0x2000;
        private const int INITIAL_TEXTURE_COUNT = 0x800;
        private const float MAX_ACCURATE_SINGLE_FLOAT = 65536;

        private readonly Dictionary<Texture2D, List<SpriteVertex>> _drawingQueue = new Dictionary<Texture2D, List<SpriteVertex>>(INITIAL_TEXTURE_COUNT);
        private readonly DepthStencilState _dss = new DepthStencilState { DepthBufferEnable = true, DepthBufferWriteEnable = true };
        private readonly Microsoft.Xna.Framework.Game _game;
        private readonly short[] _indexBuffer = new short[MAX_VERTICES_PER_DRAW * 6];
        private readonly Vector3 _minVector3 = new Vector3(0, 0, int.MinValue);
        private readonly SpriteVertex[] _vertexBuffer = new SpriteVertex[MAX_VERTICES_PER_DRAW];
        private readonly Queue<List<SpriteVertex>> _vertexQueue = new Queue<List<SpriteVertex>>(INITIAL_TEXTURE_COUNT);

        private readonly EffectTechnique _huesTechnique;
        private readonly Effect _effect;
        private readonly EffectParameter _viewportEffect;
        private readonly EffectParameter _worldMatrixEffect;
        private readonly EffectParameter _projectionMatrixEffect;
        private readonly EffectParameter _drawLightingEffect;

        private BoundingBox _drawingArea;
        private float _z;
        private readonly SpriteVertex[] _vertexBufferUI = new SpriteVertex[4];


        public SpriteBatch3D(Microsoft.Xna.Framework.Game game)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            {
                return false;
            }

            vertices[0].Position.Z = vertices[1].Position.Z = vertices[2].Position.Z = vertices[3].Position.Z = GetZ();

            //var z = GetZ();

            //vertices[0].Position.Z += z;
            //vertices[1].Position.Z += z;
            //vertices[2].Position.Z += z;
            //vertices[3].Position.Z += z;

            GetVertexList(texture).AddRange(vertices);
            return true;
        }

        public void EndDraw(bool light = false)
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


            GraphicsDevice.DepthStencilState = _dss;

            _effect.CurrentTechnique = _huesTechnique;
            _effect.CurrentTechnique.Passes[0].Apply();

            using (IEnumerator<KeyValuePair<Texture2D, List<SpriteVertex>>> enumerator = _drawingQueue.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Texture2D texture = enumerator.Current.Key;
                    List<SpriteVertex> list = enumerator.Current.Value;

                    GraphicsDevice.Textures[0] = texture;
                    GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, CopyVerticesToArray(list), 0, Math.Min(list.Count, MAX_VERTICES_PER_DRAW), _indexBuffer, 0, list.Count / 2);

                    list.Clear();
                    _vertexQueue.Enqueue(list);
                }
            }

            _drawingQueue.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private SpriteVertex[] CopyVerticesToArray(List<SpriteVertex> vertices)
        {
            int max = vertices.Count <= MAX_VERTICES_PER_DRAW ? vertices.Count : MAX_VERTICES_PER_DRAW;
            vertices.CopyTo(0, _vertexBuffer , 0, max); 
            return _vertexBuffer;
        }

        public bool Draw2D(Texture2D texture, Vector3 position, Vector3 hue)
        {
            _vertexBufferUI[0].Position.X = position.X;
            _vertexBufferUI[0].Position.Y = position.Y;
            _vertexBufferUI[0].Position.Z = 0;
            _vertexBufferUI[0].Normal.X = 0;
            _vertexBufferUI[0].Normal.Y = 0;
            _vertexBufferUI[0].Normal.Z = 1;
            _vertexBufferUI[0].TextureCoordinate = Vector3.Zero;

            _vertexBufferUI[1].Position.X = position.X + texture.Width;
            _vertexBufferUI[1].Position.Y = position.Y;
            _vertexBufferUI[1].Position.Z = 0;
            _vertexBufferUI[1].Normal.X = 0;
            _vertexBufferUI[1].Normal.Y = 0;
            _vertexBufferUI[1].Normal.Z = 1;
            _vertexBufferUI[1].TextureCoordinate.X = 1;
            _vertexBufferUI[1].TextureCoordinate.Y = 0;
            _vertexBufferUI[1].TextureCoordinate.Z = 0;

            _vertexBufferUI[2].Position.X = position.X;
            _vertexBufferUI[2].Position.Y = position.Y + texture.Height;
            _vertexBufferUI[2].Position.Z = 0;
            _vertexBufferUI[2].Normal.X = 0;
            _vertexBufferUI[2].Normal.Y = 0;
            _vertexBufferUI[2].Normal.Z = 1;
            _vertexBufferUI[2].TextureCoordinate.X = 0;
            _vertexBufferUI[2].TextureCoordinate.Y = 1;
            _vertexBufferUI[2].TextureCoordinate.Z = 0;

            _vertexBufferUI[3].Position.X = position.X + texture.Width;
            _vertexBufferUI[3].Position.Y = position.Y + texture.Height;
            _vertexBufferUI[3].Position.Z = 0;
            _vertexBufferUI[3].Normal.X = 0;
            _vertexBufferUI[3].Normal.Y = 0;
            _vertexBufferUI[3].Normal.Z = 1;
            _vertexBufferUI[3].TextureCoordinate.X = 1;
            _vertexBufferUI[3].TextureCoordinate.Y = 1;
            _vertexBufferUI[3].TextureCoordinate.Z = 0;

            _vertexBufferUI[0].Hue = _vertexBufferUI[1].Hue = _vertexBufferUI[2].Hue = _vertexBufferUI[3].Hue = hue;
            return DrawSprite(texture, _vertexBufferUI);
        }

        public bool Draw2D(Texture2D texture, Vector3 position, Rectangle sourceRect,  Vector3 hue)
        {
            float minX = sourceRect.X / (float)texture.Width;
            float maxX = (sourceRect.X + sourceRect.Width) / (float)texture.Width;
            float minY = sourceRect.Y / (float)texture.Height;
            float maxY = (sourceRect.Y + sourceRect.Height) / (float)texture.Height;

            _vertexBufferUI[0].Position.X = position.X;
            _vertexBufferUI[0].Position.Y = position.Y;
            _vertexBufferUI[0].Position.Z = 0;
            _vertexBufferUI[0].Normal.X = 0;
            _vertexBufferUI[0].Normal.Y = 0;
            _vertexBufferUI[0].Normal.Z = 1;
            _vertexBufferUI[0].TextureCoordinate.X = minX;
            _vertexBufferUI[0].TextureCoordinate.Y = minY;
            _vertexBufferUI[0].TextureCoordinate.Z = 0;

            _vertexBufferUI[1].Position.X = position.X + sourceRect.Width;
            _vertexBufferUI[1].Position.Y = position.Y;
            _vertexBufferUI[1].Position.Z = 0;
            _vertexBufferUI[1].Normal.X = 0;
            _vertexBufferUI[1].Normal.Y = 0;
            _vertexBufferUI[1].Normal.Z = 1;
            _vertexBufferUI[1].TextureCoordinate.X = maxX;
            _vertexBufferUI[1].TextureCoordinate.Y = minY;
            _vertexBufferUI[1].TextureCoordinate.Z = 0;

            _vertexBufferUI[2].Position.X = position.X;
            _vertexBufferUI[2].Position.Y = position.Y + sourceRect.Height;
            _vertexBufferUI[2].Position.Z = 0;
            _vertexBufferUI[2].Normal.X = 0;
            _vertexBufferUI[2].Normal.Y = 0;
            _vertexBufferUI[2].Normal.Z = 1;
            _vertexBufferUI[2].TextureCoordinate.X = minX;
            _vertexBufferUI[2].TextureCoordinate.Y = maxY;
            _vertexBufferUI[2].TextureCoordinate.Z = 0;

            _vertexBufferUI[3].Position.X = position.X + sourceRect.Width;
            _vertexBufferUI[3].Position.Y = position.Y + sourceRect.Height;
            _vertexBufferUI[3].Position.Z = 0;
            _vertexBufferUI[3].Normal.X = 0;
            _vertexBufferUI[3].Normal.Y = 0;
            _vertexBufferUI[3].Normal.Z = 1;
            _vertexBufferUI[3].TextureCoordinate.X = maxX;
            _vertexBufferUI[3].TextureCoordinate.Y = maxY;
            _vertexBufferUI[3].TextureCoordinate.Z = 0;

            _vertexBufferUI[0].Hue = _vertexBufferUI[1].Hue = _vertexBufferUI[2].Hue = _vertexBufferUI[3].Hue = hue;

            return DrawSprite(texture, _vertexBufferUI);
        }

        public bool Draw2D(Texture2D texture, Rectangle destRect, Rectangle sourceRect,  Vector3 hue)
        {
            float minX = sourceRect.X / (float)texture.Width, maxX = (sourceRect.X + sourceRect.Width) / (float)texture.Width;
            float minY = sourceRect.Y / (float)texture.Height, maxY = (sourceRect.Y + sourceRect.Height) / (float)texture.Height;

            _vertexBufferUI[0].Position.X = destRect.X;
            _vertexBufferUI[0].Position.Y = destRect.Y;
            _vertexBufferUI[0].Position.Z = 0;
            _vertexBufferUI[0].Normal.X = 0;
            _vertexBufferUI[0].Normal.Y = 0;
            _vertexBufferUI[0].Normal.Z = 1;
            _vertexBufferUI[0].TextureCoordinate.X = minX;
            _vertexBufferUI[0].TextureCoordinate.Y = minY;
            _vertexBufferUI[0].TextureCoordinate.Z = 0;

            _vertexBufferUI[1].Position.X = destRect.X + destRect.Width;
            _vertexBufferUI[1].Position.Y = destRect.Y;
            _vertexBufferUI[1].Position.Z = 0;
            _vertexBufferUI[1].Normal.X = 0;
            _vertexBufferUI[1].Normal.Y = 0;
            _vertexBufferUI[1].Normal.Z = 1;
            _vertexBufferUI[1].TextureCoordinate.X = maxX;
            _vertexBufferUI[1].TextureCoordinate.Y = minY;
            _vertexBufferUI[1].TextureCoordinate.Z = 0;

            _vertexBufferUI[2].Position.X = destRect.X;
            _vertexBufferUI[2].Position.Y = destRect.Y + destRect.Height;
            _vertexBufferUI[2].Position.Z = 0;
            _vertexBufferUI[2].Normal.X = 0;
            _vertexBufferUI[2].Normal.Y = 0;
            _vertexBufferUI[2].Normal.Z = 1;
            _vertexBufferUI[2].TextureCoordinate.X = minX;
            _vertexBufferUI[2].TextureCoordinate.Y = maxY;
            _vertexBufferUI[2].TextureCoordinate.Z = 0;

            _vertexBufferUI[3].Position.X = destRect.X + destRect.Width;
            _vertexBufferUI[3].Position.Y = destRect.Y + destRect.Height;
            _vertexBufferUI[3].Position.Z = 0;
            _vertexBufferUI[3].Normal.X = 0;
            _vertexBufferUI[3].Normal.Y = 0;
            _vertexBufferUI[3].Normal.Z = 1;
            _vertexBufferUI[3].TextureCoordinate.X = maxX;
            _vertexBufferUI[3].TextureCoordinate.Y = maxY;
            _vertexBufferUI[3].TextureCoordinate.Z = 0;

            _vertexBufferUI[0].Hue = _vertexBufferUI[1].Hue = _vertexBufferUI[2].Hue = _vertexBufferUI[3].Hue = hue;

            return DrawSprite(texture, _vertexBufferUI);
        }

        public bool Draw2D(Texture2D texture, Rectangle destRect, Vector3 hue)
        {
            _vertexBufferUI[0].Position.X = destRect.X;
            _vertexBufferUI[0].Position.Y = destRect.Y;
            _vertexBufferUI[0].Position.Z = 0;
            _vertexBufferUI[0].Normal.X = 0;
            _vertexBufferUI[0].Normal.Y = 0;
            _vertexBufferUI[0].Normal.Z = 1;
            _vertexBufferUI[0].TextureCoordinate = Vector3.Zero;

            _vertexBufferUI[1].Position.X = destRect.X + destRect.Width;
            _vertexBufferUI[1].Position.Y = destRect.Y;
            _vertexBufferUI[1].Position.Z = 0;
            _vertexBufferUI[1].Normal.X = 0;
            _vertexBufferUI[1].Normal.Y = 0;
            _vertexBufferUI[1].Normal.Z = 1;
            _vertexBufferUI[1].TextureCoordinate.X = 1;
            _vertexBufferUI[1].TextureCoordinate.Y = 0;
            _vertexBufferUI[1].TextureCoordinate.Z = 0;

            _vertexBufferUI[2].Position.X = destRect.X;
            _vertexBufferUI[2].Position.Y = destRect.Y + destRect.Height;
            _vertexBufferUI[2].Position.Z = 0;
            _vertexBufferUI[2].Normal.X = 0;
            _vertexBufferUI[2].Normal.Y = 0;
            _vertexBufferUI[2].Normal.Z = 1;
            _vertexBufferUI[2].TextureCoordinate.X = 0;
            _vertexBufferUI[2].TextureCoordinate.Y = 1;
            _vertexBufferUI[2].TextureCoordinate.Z = 0;

            _vertexBufferUI[3].Position.X = destRect.X + destRect.Width;
            _vertexBufferUI[3].Position.Y = destRect.Y + destRect.Height;
            _vertexBufferUI[3].Position.Z = 0;
            _vertexBufferUI[3].Normal.X = 0;
            _vertexBufferUI[3].Normal.Y = 0;
            _vertexBufferUI[3].Normal.Z = 1;
            _vertexBufferUI[3].TextureCoordinate.X = 1;
            _vertexBufferUI[3].TextureCoordinate.Y = 1;
            _vertexBufferUI[3].TextureCoordinate.Z = 0;

            _vertexBufferUI[0].Hue = _vertexBufferUI[1].Hue = _vertexBufferUI[2].Hue = _vertexBufferUI[3].Hue = hue;
            return DrawSprite(texture, _vertexBufferUI);
        }

        public bool Draw2DTiled(Texture2D texture, Rectangle destRect, Vector3 hue)
        {
            int y = destRect.Y;
            int h = destRect.Height;
            Rectangle sRect;

            while (h > 0)
            {
                int x = destRect.X;
                int w = destRect.Width;
                if (h < texture.Height)
                {
                    sRect = new Rectangle(0, 0, texture.Width, h);
                }
                else
                {
                    sRect = new Rectangle(0, 0, texture.Width, texture.Height);
                }

                while (w > 0)
                {
                    if (w < texture.Width)
                    {
                        sRect.Width = w;
                    }

                    Draw2D(texture, new Vector3(x, y, 0), sRect, hue);
                    w -= texture.Width;
                    x += texture.Width;
                }

                h -= texture.Height;
                y += texture.Height;
            }

            return true;
        }

        public bool DrawRectangle(Texture2D texture, Rectangle rectangle, Vector3 hue)
        {
            DrawLine(texture, new Vector2(rectangle.X, rectangle.Y), new Vector2(rectangle.Right, rectangle.Y), hue);
            DrawLine(texture, new Vector2(rectangle.Right, rectangle.Y), new Vector2(rectangle.Right, rectangle.Bottom), hue);
            DrawLine(texture, new Vector2(rectangle.Right, rectangle.Bottom), new Vector2(rectangle.X, rectangle.Bottom), hue);
            DrawLine(texture, new Vector2(rectangle.X, rectangle.Bottom), new Vector2(rectangle.X, rectangle.Y), hue);

            return true;
        }

        public bool DrawLine(Texture2D texture, Vector2 start, Vector2 end, Vector3 hue)
        {
            int offX = start.X == end.X ? 1 : 0;
            int offY = start.Y == end.Y ? 1 : 0;

            _vertexBufferUI[0].Position.X = start.X;
            _vertexBufferUI[0].Position.Y = start.Y;
            _vertexBufferUI[0].Normal = new Vector3(0, 0, 1);
            _vertexBufferUI[0].TextureCoordinate = new Vector3(0, 0, 0);

            _vertexBufferUI[1].Position.X = end.X + offX;
            _vertexBufferUI[1].Position.Y = start.Y + offY;
            _vertexBufferUI[1].Normal = new Vector3(0, 0, 1);
            _vertexBufferUI[1].TextureCoordinate = new Vector3(1, 0, 0);

            _vertexBufferUI[2].Position.X = start.X + offX;
            _vertexBufferUI[2].Position.Y = end.Y + offY;
            _vertexBufferUI[2].Normal = new Vector3(0, 0, 1);
            _vertexBufferUI[2].TextureCoordinate = new Vector3(0, 1, 0);

            _vertexBufferUI[3].Position.X = end.X;
            _vertexBufferUI[3].Position.Y = end.Y;
            _vertexBufferUI[3].Normal = new Vector3(0, 0, 1);
            _vertexBufferUI[3].TextureCoordinate = new Vector3(1, 1, 0);

            _vertexBufferUI[0].Hue = _vertexBufferUI[1].Hue = _vertexBufferUI[2].Hue = _vertexBufferUI[3].Hue = hue;

            return DrawSprite(texture, _vertexBufferUI);
        }

        private List<SpriteVertex> GetVertexList(Texture2D texture)
        {
            if (!_drawingQueue.TryGetValue(texture, out List<SpriteVertex> list))
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

                _drawingQueue.Add(texture, list);
            }
            return list;
        }
    }
}