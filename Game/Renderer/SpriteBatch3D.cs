using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;


namespace ClassicUO.Game.Renderer
{
    public class SpriteBatch3D
    {
        private const int MAX_VERTICES_PER_DRAW = 0x4000;
        private const int INITIAL_TEXTURE_COUNT = 0x800;
        private const float MAX_ACCURATE_SINGLE_FLOAT = 65536;

        /// <summary>
        /// Game instance
        /// </summary>
        private readonly Microsoft.Xna.Framework.Game _game;
        /// <summary>
        /// List of key and values of Texture2D and a list of SpriteVertex objects
        /// </summary>
        private readonly List<Dictionary<Texture2D, List<SpriteVertex>>> _drawingQueue;
        /// <summary>
        /// Array of index buffers
        /// </summary>
        private readonly short[] _indexBuffer;
        /// <summary>
        /// Array of sprite vertexes
        /// </summary>
        private readonly SpriteVertex[] _spriteVertexArray;
        /// <summary>
        /// Queue of sprite vertexes
        /// </summary>
        private readonly Queue<List<SpriteVertex>> _spriteVertexListQueue;
        /// <summary>
        /// Shader effect
        /// </summary>
        private readonly Effect _effect;
        /// <summary>
        /// the bounding box
        /// </summary>
        private static BoundingBox _viewportArea;
        /// <summary>
        /// Current Z index
        /// </summary>
        private float _z;
        /// <summary>
        /// The sprite vertex array for UI (2D)
        /// </summary>
        private readonly SpriteVertex[] _vertexBufferUI = new SpriteVertex[4];

        public SpriteBatch3D(in Microsoft.Xna.Framework.Game game)
        {
            _game = game;

            //Initializing the drawing queue
            _drawingQueue = new List<Dictionary<Texture2D, List<SpriteVertex>>>((int)Techniques.All);
            for (int i = 0; i <= (int)Techniques.All; i++)
            {
                _drawingQueue.Add(new Dictionary<Texture2D, List<SpriteVertex>>(INITIAL_TEXTURE_COUNT));
            }
            //Initializing of the index buffer
            _indexBuffer = CreateIndexBuffer(MAX_VERTICES_PER_DRAW);
            //Initializing of sprite vertexes
            _spriteVertexArray = new SpriteVertex[MAX_VERTICES_PER_DRAW];
            //Initializing of queue of sprite vertexes
            _spriteVertexListQueue = new Queue<List<SpriteVertex>>(INITIAL_TEXTURE_COUNT);
            //Initializing of shader effect
            _effect = new Effect(GraphicsDevice, File.ReadAllBytes(Path.Combine(Environment.CurrentDirectory, "IsometricWorld.fxc")));
            //Sets value for parameter HuesPerTexture
            _effect.Parameters["HuesPerTexture"].SetValue(3000f);

        }

        /// <summary>
        /// Returns the current graphics device
        /// </summary>
        public GraphicsDevice GraphicsDevice => _game?.GraphicsDevice;
        /// <summary>
        /// Returns ProjectionMatrixWorld
        /// </summary>
        public Matrix ProjectionMatrixWorld => Matrix.Identity;
        /// <summary>
        /// Returns ProjectionMatrixScreen
        /// </summary>
        public Matrix ProjectionMatrixScreen => Matrix.CreateOrthographicOffCenter(0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 0f, short.MinValue, short.MaxValue);


        /// <summary>
        /// Functions sets the effect parameter for light direction
        /// </summary>
        /// <param name="dir"></param>
        public void SetLightDirection(in Vector3 dir)
        {
            _effect.Parameters["lightDirection"].SetValue(dir);
        }
        /// <summary>
        /// Functions sets the effect parameter for light intensity
        /// </summary>
        /// <param name="inte"></param>
        public void SetLightIntensity(in float inte)
        {
            _effect.Parameters["lightIntensity"].SetValue(inte);
        }


        /// <summary>
        /// Methode resets z and viewport
        /// </summary>
        /// <param name="setZHigh"></param>
        public void BeginDraw(bool setZHigh = false)
        {
            _z = setZHigh ? MAX_ACCURATE_SINGLE_FLOAT : 0;
            _viewportArea = new BoundingBox(new Vector3(0, 0, Int32.MinValue), new Vector3(_game.GraphicsDevice.Viewport.Width, _game.GraphicsDevice.Viewport.Height, Int32.MaxValue));
        }

        /// <summary>
        /// Function will try to return z index +1
        /// </summary>
        /// <returns></returns>
        public virtual float GetNextUniqueZ()
        {
            return _z++;
        }


        /// <summary>
        /// Draws a quad on screen with the specified texture and vertices.
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="vertices"></param>
        /// <returns>True if the object was drawn, false otherwise.</returns>
        public bool DrawSprite(Texture2D texture, SpriteVertex[] vertices, Techniques effect = Techniques.Default)
        {

            bool draw = false;

            // Sanity: do not draw if there is no texture to draw with.
            if (texture == null)
                return false;

            // Check: only draw if the texture is within the visible area.
            for (int i = 0; i < 4; i++) // only draws a 2 triangle tristrip.
            {
                if (_viewportArea.Contains(vertices[i].Position) == ContainmentType.Contains)
                {
                    draw = true;
                    break;
                }
            }
            if (!draw)
                return false;

            // Set the draw position's z value, and increment the z value for the next drawn object.
            vertices[0].Position.Z = vertices[1].Position.Z = vertices[2].Position.Z = vertices[3].Position.Z = GetNextUniqueZ();

            // Get the vertex list for this texture. if none exists, dequeue existing or create a new vertex list.
            List<SpriteVertex> vertexList = GetVertexList(texture, effect);

            // Add the drawn object to the vertex list.
            for (int i = 0; i < vertices.Length; i++)
                vertexList.Add(vertices[i]);

            return true;
        }


        /// <summary>
        /// Function resets the sprite states and so on
        /// </summary>
        /// <param name="doLighting"></param>
        public void FlushSprites(bool doLighting)
        {
            // set up graphics device and texture sampling.
            GraphicsDevice.BlendState = BlendState.AlphaBlend;
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp; // the sprite texture sampler.
            GraphicsDevice.SamplerStates[1] = SamplerState.PointClamp; // hue sampler (1/2)
            //GraphicsDevice.SamplerStates[2] = SamplerState.PointClamp; // hue sampler (2/2)
            //GraphicsDevice.SamplerStates[3] = SamplerState.PointWrap; // the minimap sampler.
            // We use lighting parameters to shade vertexes when we're drawing the world.
            _effect.Parameters["DrawLighting"].SetValue(doLighting);
            // set up viewport.
            _effect.Parameters["ProjectionMatrix"].SetValue(ProjectionMatrixScreen);
            _effect.Parameters["WorldMatrix"].SetValue(ProjectionMatrixWorld);
            _effect.Parameters["Viewport"].SetValue(new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height));
            // enable depth sorting, disable the stencil
            SetDepthStencilState(true, false);
            DrawAllVertices(Techniques.FirstDrawn, Techniques.LastDrawn);
        }

        /// <summary>
        /// Draws all vertices in relation to their type
        /// </summary>
        /// <param name="first"></param>
        /// <param name="last"></param>
        private void DrawAllVertices(Techniques first, Techniques last)
        {
            // draw normal objects
            for (Techniques effect = first; effect <= last; effect++)
            {
                switch (effect)
                {
                    case Techniques.Hued:
                        _effect.CurrentTechnique = _effect.Techniques["HueTechnique"];
                        break;
                    case Techniques.MiniMap:
                        _effect.CurrentTechnique = _effect.Techniques["MiniMapTechnique"];
                        break;
                    case Techniques.Grayscale:
                        _effect.CurrentTechnique = _effect.Techniques["GrayscaleTechnique"];
                        break;
                    case Techniques.ShadowSet:
                        _effect.CurrentTechnique = _effect.Techniques["ShadowSetTechnique"];
                        SetDepthStencilState(true, true);
                        break;
                    case Techniques.StencilSet:
                        // do nothing;
                        break;
                    default:
                        Log.Message(LogTypes.Trace, "Unknown effect in SpriteBatch3D.Flush(). Effect index is " + effect);
                        break;
                }
                _effect.CurrentTechnique.Passes[0].Apply();

                IEnumerator<KeyValuePair<Texture2D, List<SpriteVertex>>> vertexEnumerator = _drawingQueue[(int)effect].GetEnumerator();
                while (vertexEnumerator.MoveNext())
                {
                    Texture2D texture = vertexEnumerator.Current.Key;
                    List<SpriteVertex> vertexList = vertexEnumerator.Current.Value;
                    GraphicsDevice.Textures[0] = texture;
                    GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, CopyVerticesToArray(vertexList), 0, Math.Min(vertexList.Count, MAX_VERTICES_PER_DRAW), _indexBuffer, 0, vertexList.Count / 2);
                    vertexList.Clear();
                    _spriteVertexListQueue.Enqueue(vertexList);

                }
                _drawingQueue[(int)effect].Clear();
            }
        }

        /// <summary>
        /// Helper function for vertices
        /// </summary>
        /// <param name="vertices"></param>
        /// <returns></returns>
        private SpriteVertex[] CopyVerticesToArray(List<SpriteVertex> vertices)
        {
            int max = vertices.Count <= MAX_VERTICES_PER_DRAW ? vertices.Count : MAX_VERTICES_PER_DRAW;
            vertices.CopyTo(0, _spriteVertexArray, 0, max);
            return _spriteVertexArray;
        }


        /// <summary>
        /// Function draws UI
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="position"></param>
        /// <param name="hue"></param>
        /// <returns></returns>
        public bool Draw2D(Texture2D texture, Vector3 position, Vector3 hue)
        {
            _vertexBufferUI[0] = new SpriteVertex(new Vector3(position.X, position.Y, 0), new Vector3(0, 0, 1), new Vector3(0, 0, 0));
            _vertexBufferUI[1] = new SpriteVertex(new Vector3(position.X + texture.Width, position.Y, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 0));
            _vertexBufferUI[2] = new SpriteVertex(new Vector3(position.X, position.Y + texture.Height, 0), new Vector3(0, 0, 1), new Vector3(0, 1, 0));
            _vertexBufferUI[3] = new SpriteVertex(new Vector3(position.X + texture.Width, position.Y + texture.Height, 0), new Vector3(0, 0, 1), new Vector3(1, 1, 0));
            _vertexBufferUI[0].Hue = _vertexBufferUI[1].Hue = _vertexBufferUI[2].Hue = _vertexBufferUI[3].Hue = hue;
            return DrawSprite(texture, _vertexBufferUI);
        }
        /// <summary>
        /// Function draws UI
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="position"></param>
        /// <param name="sourceRect"></param>
        /// <param name="hue"></param>
        /// <returns></returns>
        public bool Draw2D(Texture2D texture, Vector3 position, Rectangle sourceRect, Vector3 hue)
        {
            float minX = sourceRect.X / (float)texture.Width;
            float maxX = (sourceRect.X + sourceRect.Width) / (float)texture.Width;
            float minY = sourceRect.Y / (float)texture.Height;
            float maxY = (sourceRect.Y + sourceRect.Height) / (float)texture.Height;

            _vertexBufferUI[0] = new SpriteVertex(new Vector3(position.X, position.Y, 0), new Vector3(0, 0, 1), new Vector3(minX, minY, 0));
            _vertexBufferUI[1] = new SpriteVertex(new Vector3(position.X + sourceRect.Width, position.Y, 0), new Vector3(0, 0, 1), new Vector3(maxX, minY, 0));
            _vertexBufferUI[2] = new SpriteVertex(new Vector3(position.X, position.Y + sourceRect.Height, 0), new Vector3(0, 0, 1), new Vector3(minX, maxY, 0));
            _vertexBufferUI[3] = new SpriteVertex(new Vector3(position.X + sourceRect.Width, position.Y + sourceRect.Height, 0), new Vector3(0, 0, 1), new Vector3(maxX, maxY, 0));

            _vertexBufferUI[0].Hue = _vertexBufferUI[1].Hue = _vertexBufferUI[2].Hue = _vertexBufferUI[3].Hue = hue;
            return DrawSprite(texture, _vertexBufferUI);
        }
        /// <summary>
        /// Function draws UI
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="destRect"></param>
        /// <param name="sourceRect"></param>
        /// <param name="hue"></param>
        /// <returns></returns>
        public bool Draw2D(Texture2D texture, Rectangle destRect, Rectangle sourceRect, Vector3 hue)
        {
            float minX = sourceRect.X / (float)texture.Width, maxX = (sourceRect.X + sourceRect.Width) / (float)texture.Width;
            float minY = sourceRect.Y / (float)texture.Height, maxY = (sourceRect.Y + sourceRect.Height) / (float)texture.Height;

            _vertexBufferUI[0] = new SpriteVertex(new Vector3(destRect.X, destRect.Y, 0), new Vector3(0, 0, 1), new Vector3(minX, minY, 0));
            _vertexBufferUI[1] = new SpriteVertex(new Vector3(destRect.X + destRect.Width, destRect.Y, 0), new Vector3(0, 0, 1), new Vector3(maxX, minY, 0));
            _vertexBufferUI[2] = new SpriteVertex(new Vector3(destRect.X, destRect.Y + destRect.Height, 0), new Vector3(0, 0, 1), new Vector3(minX, maxY, 0));
            _vertexBufferUI[3] = new SpriteVertex(new Vector3(destRect.X + destRect.Width, destRect.Y + destRect.Height, 0), new Vector3(0, 0, 1), new Vector3(maxX, maxY, 0));

            _vertexBufferUI[0].Hue = _vertexBufferUI[1].Hue = _vertexBufferUI[2].Hue = _vertexBufferUI[3].Hue = hue;
            return DrawSprite(texture, _vertexBufferUI);
        }
        /// <summary>
        /// Function draws UI
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="destRect"></param>
        /// <param name="hue"></param>
        /// <returns></returns>
        public bool Draw2D(Texture2D texture, Rectangle destRect, Vector3 hue)
        {
            _vertexBufferUI[0] = new SpriteVertex(new Vector3(destRect.X, destRect.Y, 0), new Vector3(0, 0, 1), new Vector3(0, 0, 0));
            _vertexBufferUI[1] = new SpriteVertex(new Vector3(destRect.X + destRect.Width, destRect.Y, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 0));
            _vertexBufferUI[2] = new SpriteVertex(new Vector3(destRect.X, destRect.Y + destRect.Height, 0), new Vector3(0, 0, 1), new Vector3(0, 1, 0));
            _vertexBufferUI[3] = new SpriteVertex(new Vector3(destRect.X + destRect.Width, destRect.Y + destRect.Height, 0), new Vector3(0, 0, 1), new Vector3(1, 1, 0));

            _vertexBufferUI[0].Hue = _vertexBufferUI[1].Hue = _vertexBufferUI[2].Hue = _vertexBufferUI[3].Hue = hue;
            return DrawSprite(texture, _vertexBufferUI);
        }
        /// <summary>
        /// Function draws UI
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="destRect"></param>
        /// <param name="hue"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Helper function for drawing rectangles 
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="rectangle"></param>
        /// <param name="hue"></param>
        /// <returns></returns>
        public bool DrawRectangle(in Texture2D texture, in Rectangle rectangle, in Vector3 hue)
        {
            DrawLine(texture, new Vector2(rectangle.X, rectangle.Y), new Vector2(rectangle.Right, rectangle.Y), hue);
            DrawLine(texture, new Vector2(rectangle.Right, rectangle.Y), new Vector2(rectangle.Right, rectangle.Bottom), hue);
            DrawLine(texture, new Vector2(rectangle.Right, rectangle.Bottom), new Vector2(rectangle.X, rectangle.Bottom), hue);
            DrawLine(texture, new Vector2(rectangle.X, rectangle.Bottom), new Vector2(rectangle.X, rectangle.Y), hue);

            return true;
        }
        /// <summary>
        /// Helper function for drawing lines
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="hue"></param>
        /// <returns></returns>
        public bool DrawLine(in Texture2D texture, in Vector2 start, in Vector2 end, in Vector3 hue)
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

        /// <summary>
        /// Gets all vertexes
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="effect"></param>
        /// <returns></returns>
        private List<SpriteVertex> GetVertexList(Texture2D texture, Techniques effect)
        {
            List<SpriteVertex> vertexList;
            if (_drawingQueue[(int)effect].ContainsKey(texture))
            {
                vertexList = _drawingQueue[(int)effect][texture];
            }
            else
            {
                if (_spriteVertexListQueue.Count > 0)
                {
                    vertexList = _spriteVertexListQueue.Dequeue();
                    vertexList.Clear();
                }
                else
                {
                    vertexList = new List<SpriteVertex>(1024);
                }
                _drawingQueue[(int)effect].Add(texture, vertexList);
            }
            return vertexList;
        }
        /// <summary>
        /// Sets the stencil states 
        /// </summary>
        /// <param name="depth"></param>
        /// <param name="stencil"></param>
        private void SetDepthStencilState(bool depth, bool stencil)
        {
            // depth is currently ignored.
            DepthStencilState dss = new DepthStencilState();
            dss.DepthBufferEnable = true;
            dss.DepthBufferWriteEnable = true;

            if (stencil)
            {
                dss.StencilEnable = true;
                dss.StencilFunction = CompareFunction.Equal;
                dss.ReferenceStencil = 0;
                dss.StencilPass = StencilOperation.Increment;
                dss.StencilFail = StencilOperation.Keep;
            }

            GraphicsDevice.DepthStencilState = dss;
        }
        /// <summary>
        /// Helper function fir creating the index buffers
        /// </summary>
        /// <param name="primitiveCount"></param>
        /// <returns></returns>
        private short[] CreateIndexBuffer(int primitiveCount)
        {
            short[] indices = new short[primitiveCount * 6];

            for (int i = 0; i < primitiveCount; i++)
            {
                indices[i * 6] = (short)(i * 4);
                indices[i * 6 + 1] = (short)(i * 4 + 1);
                indices[i * 6 + 2] = (short)(i * 4 + 2);
                indices[i * 6 + 3] = (short)(i * 4 + 2);
                indices[i * 6 + 4] = (short)(i * 4 + 1);
                indices[i * 6 + 5] = (short)(i * 4 + 3);
            }

            return indices;
        }
    }
}