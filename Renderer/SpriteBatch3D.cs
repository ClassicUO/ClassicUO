#region license

//  Copyright (C) 2018 ClassicUO Development Community on Github
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
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer
{
    // https://git.gmantaos.com/gamedev/Engine/blob/fcc0d5bcca1e9fcf5eb8481185ff7ecffbbe4fe2/Engine/Nez/Utils/MonoGameCompat.cs

    public static class Ext
    {
        public static void DrawIndexedPrimitives(this GraphicsDevice self, PrimitiveType primitiveType, int baseVertex,
            int startIndex, int primitiveCount)
        {
            self.DrawIndexedPrimitives(primitiveType, baseVertex, 0, primitiveCount * 2, startIndex, primitiveCount);
        }
    }

    public class SpriteBatch3D
    {
        private const int VERTEX_COUNT = 4;
        private const int INDEX_COUNT = 6;
        private const int PRIMITIVES_COUNT = 2;

        private const int MAX_VERTICES_PER_DRAW = 0x8000 * 4;
        private const int INITIAL_TEXTURE_COUNT = 0x800;
        private const float MAX_ACCURATE_SINGLE_FLOAT = 65536;
        private readonly DrawCallInfo[] _drawCalls;
        private readonly EffectParameter _drawLightingEffect;

        private readonly DepthStencilState _dss = new DepthStencilState
        {
            DepthBufferEnable = true,
            DepthBufferWriteEnable = true
        };

        private readonly Effect _effect;

        private readonly Microsoft.Xna.Framework.Game _game;
        private readonly short[] _geometryIndices = new short[6];

        private readonly EffectTechnique _huesTechnique;
        private readonly IndexBuffer _indexBuffer;
        private readonly short[] _indices = new short[MAX_VERTICES_PER_DRAW * 6];

        private readonly Vector3 _minVector3 = new Vector3(0, 0, int.MinValue);
        private readonly EffectParameter _projectionMatrixEffect;
        private readonly short[] _sortedIndices = new short[MAX_VERTICES_PER_DRAW * 6];
        private readonly VertexBuffer _vertexBuffer;
        private readonly SpriteVertex[] _vertices = new SpriteVertex[MAX_VERTICES_PER_DRAW];
        private readonly EffectParameter _viewportEffect;
        private readonly EffectParameter _worldMatrixEffect;

        private BoundingBox _drawingArea;
        private int _indicesCount;
        private bool _isStarted;


        private int _vertexCount;
        private float _z;


        public SpriteBatch3D(Microsoft.Xna.Framework.Game game)
        {
            _game = game;

            _effect = new Effect(GraphicsDevice,
                File.ReadAllBytes(Path.Combine(Bootstrap.ExeDirectory, "shaders/IsometricWorld.fxc")));

            _effect.Parameters["HuesPerTexture"].SetValue((float) IO.Resources.Hues.HuesCount);

            _drawLightingEffect = _effect.Parameters["DrawLighting"];
            _projectionMatrixEffect = _effect.Parameters["ProjectionMatrix"];
            _worldMatrixEffect = _effect.Parameters["WorldMatrix"];
            _viewportEffect = _effect.Parameters["Viewport"];

            _huesTechnique = _effect.Techniques["HueTechnique"];


            _drawCalls = new DrawCallInfo[MAX_VERTICES_PER_DRAW];

            _vertexBuffer = new DynamicVertexBuffer(GraphicsDevice, SpriteVertex.VertexDeclaration, _vertices.Length,
                BufferUsage.WriteOnly);
            _indexBuffer = new DynamicIndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, _indices.Length,
                BufferUsage.WriteOnly);
        }


        public GraphicsDevice GraphicsDevice => _game?.GraphicsDevice;
        public Matrix ProjectionMatrixWorld => Matrix.Identity;

        public Matrix ProjectionMatrixScreen => Matrix.CreateOrthographicOffCenter(0, GraphicsDevice.Viewport.Width,
            GraphicsDevice.Viewport.Height, 0f, short.MinValue, short.MaxValue);

        public int Merged { get; private set; }
        public int Calls { get; private set; }

        public int TotalCalls => Merged + Calls;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddQuadrilateralIndices(int indexOffset)
        {
            _geometryIndices[0] = (short) (0 + indexOffset);
            _geometryIndices[1] = (short) (1 + indexOffset);
            _geometryIndices[2] = (short) (2 + indexOffset);
            _geometryIndices[3] = (short) (1 + indexOffset);
            _geometryIndices[4] = (short) (3 + indexOffset);
            _geometryIndices[5] = (short) (2 + indexOffset);
        }

        public void SetLightDirection(Vector3 dir)
        {
            _effect.Parameters["lightDirection"].SetValue(dir);
        }

        public void SetLightIntensity(float inte)
        {
            _effect.Parameters["lightIntensity"].SetValue(inte);
        }

        public float GetZ() => _z++;

        public void Begin()
        {
            if (_isStarted)
                throw new Exception("");

            _isStarted = true;

            Calls = 0;
            Merged = 0;

            _z = 0;
            _drawingArea.Min = _minVector3;
            _drawingArea.Max = new Vector3(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, int.MaxValue);
        }

        public bool DrawSprite(Texture2D texture, SpriteVertex[] vertices)
        {
            if (!_isStarted)
                throw new Exception();

            if (texture == null || texture.IsDisposed) return false;

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

#if !ORIONSORT
            vertices[0].Position.Z = vertices[1].Position.Z = vertices[2].Position.Z = vertices[3].Position.Z = GetZ();
#endif
            Build(texture, vertices);

            return true;
        }


        private void Build(Texture2D texture, SpriteVertex[] vertices)
        {
            AddQuadrilateralIndices(_vertexCount);

            DrawCallInfo call = new DrawCallInfo(texture, _indicesCount, PRIMITIVES_COUNT);

            Array.Copy(vertices, 0, _vertices, _vertexCount, VERTEX_COUNT);
            _vertexCount += VERTEX_COUNT;
            Array.Copy(_geometryIndices, 0, _indices, _indicesCount, INDEX_COUNT);
            _indicesCount += INDEX_COUNT;

            Enqueue(ref call);
        }

        private void Enqueue(ref DrawCallInfo call)
        {
            if (Calls > 0 && call.TryMerge(ref _drawCalls[Calls - 1]))
            {
                Merged++;
                return;
            }

            _drawCalls[Calls++] = call;
        }

        public void End(bool light = false)
        {
            if (!_isStarted)
                throw new Exception();

            Flush(light);
            _isStarted = false;
        }

        private void Flush(bool light)
        {
            if (Calls == 0)
                return;

            SetupBuffers();
            ApplyStates(light);
            InternalDraw();
        }

        private void SetupBuffers()
        {
            _vertexBuffer.SetData(_vertices, 0, _vertexCount);
            GraphicsDevice.SetVertexBuffer(_vertexBuffer);

            //SortIndicesAndMerge();

            _indexBuffer.SetData(_indices, 0, _indicesCount);
            GraphicsDevice.Indices = _indexBuffer;

            _indicesCount = 0;
            _vertexCount = 0;
        }

        private void SortIndicesAndMerge()
        {
            Array.Sort(_drawCalls, 0, Calls);


            int newDrawCallCount = 0;
            int start = _drawCalls[0].StartIndex;

            _drawCalls[0].StartIndex = 0;
            DrawCallInfo currentDrawCall = _drawCalls[0];
            _drawCalls[newDrawCallCount++] = _drawCalls[0];

            int drawCallIndexCount = currentDrawCall.PrimitiveCount * 3;
            Array.Copy(_indices, start, _sortedIndices, 0, drawCallIndexCount);
            int sortedIndexCount = drawCallIndexCount;

            for (int i = 1; i < Calls; i++)
            {
                currentDrawCall = _drawCalls[i];
                drawCallIndexCount = currentDrawCall.PrimitiveCount * 3;
                Array.Copy(_indices, currentDrawCall.StartIndex, _sortedIndices, sortedIndexCount, drawCallIndexCount);

                sortedIndexCount += drawCallIndexCount;
                if (currentDrawCall.TryMerge(ref _drawCalls[newDrawCallCount - 1]))
                {
                    Merged++;
                    continue;
                }

                currentDrawCall.StartIndex = sortedIndexCount - drawCallIndexCount;
                _drawCalls[newDrawCallCount++] = currentDrawCall;
            }

            Calls = newDrawCallCount;
        }

        private void ApplyStates(bool light)
        {
            GraphicsDevice.BlendState = BlendState.AlphaBlend;
            //GraphicsDevice.BlendState.ColorBlendFunction = BlendFunction.Add;
            //GraphicsDevice.BlendState.AlphaSourceBlend = Blend.SourceColor;

            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
            GraphicsDevice.SamplerStates[1] = SamplerState.PointClamp;
            GraphicsDevice.SamplerStates[2] = SamplerState.PointClamp;

            //GraphicsDevice.SamplerStates[3] = SamplerState.PointClamp;
            //GraphicsDevice.SamplerStates[4] = SamplerState.PointWrap;

            _drawLightingEffect.SetValue(light);
            // set up viewport.
            _projectionMatrixEffect.SetValue(ProjectionMatrixScreen);
            _worldMatrixEffect.SetValue(ProjectionMatrixWorld);
            _viewportEffect.SetValue(new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height));

            GraphicsDevice.DepthStencilState = _dss;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InternalDraw()
        {
            _effect.CurrentTechnique = _huesTechnique;
            _effect.CurrentTechnique.Passes[0].Apply();

            for (int i = 0; i < Calls; i++)
            {
                ref DrawCallInfo call = ref _drawCalls[i];

                GraphicsDevice.Textures[0] = call.Texture;
                GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, call.StartIndex,
                    call.PrimitiveCount);
            }

            Array.Clear(_drawCalls, 0, Calls);
        }


        private struct DrawCallInfo : IComparable<DrawCallInfo>
        {
            public DrawCallInfo(Texture2D texture, int start, int count)
            {
                Texture = texture;
                TextureKey = (uint) texture.GetHashCode();
                StartIndex = start;
                PrimitiveCount = count;
            }

            public readonly Texture2D Texture;
            public readonly uint TextureKey;
            public int StartIndex;
            public int PrimitiveCount;

            public bool TryMerge(ref DrawCallInfo callInfo)
            {
                if (TextureKey != callInfo.TextureKey)
                    return false;
                callInfo.PrimitiveCount += PrimitiveCount;
                return true;
            }

            public int CompareTo(DrawCallInfo other)
            {
                int result = TextureKey.CompareTo(other.TextureKey);
                return result;
            }
        }
    }
}