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
using System.Runtime.InteropServices;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer
{
    // https://git.gmantaos.com/gamedev/Engine/blob/fcc0d5bcca1e9fcf5eb8481185ff7ecffbbe4fe2/Engine/Nez/Utils/MonoGameCompat.cs

    public static class Ext
    {
        public static void DrawIndexedPrimitives(this GraphicsDevice self, PrimitiveType primitiveType, int baseVertex, int startIndex, int primitiveCount)
        {
            self.DrawIndexedPrimitives(primitiveType, baseVertex, 0, primitiveCount * 2, startIndex, primitiveCount);
        }
    }

#if !SB1
    public class SpriteBatch3D
    {
        private const int VERTEX_COUNT = 4;
        private const int INDEX_COUNT = 6;
        private const int PRIMITIVES_COUNT = 2;

        private const int MAX_SPRITES = 0x800;
        private const int MAX_VERTICES_PER_DRAW = MAX_SPRITES * 2;
        private const int INITIAL_TEXTURE_COUNT = 0x800;
        private const float MAX_ACCURATE_SINGLE_FLOAT = 65536;
        private readonly DrawCallInfo[] _drawCalls;

        private readonly DepthStencilState _dss = new DepthStencilState
        {
            DepthBufferEnable = true,
            DepthBufferWriteEnable = true
        };

        private readonly Effect _effect;

        private readonly short[] _geometryIndices = new short[6];

        private readonly EffectTechnique _huesTechnique, _shadowTechnique;
        private readonly IndexBuffer _indexBuffer;
        private readonly short[] _indices = new short[MAX_VERTICES_PER_DRAW * 6];

        private readonly Vector3 _minVector3 = new Vector3(0, 0, int.MinValue);
        private readonly short[] _sortedIndices = new short[MAX_VERTICES_PER_DRAW * 6];
        private readonly VertexBuffer _vertexBuffer;
        private readonly SpriteVertex[] _vertices = new SpriteVertex[MAX_VERTICES_PER_DRAW * 4];

        private readonly EffectParameter _viewportEffect;
        private readonly EffectParameter _worldMatrixEffect;
        private readonly EffectParameter _drawLightingEffect;
        private readonly EffectParameter _projectionMatrixEffect;

        private BoundingBox _drawingArea;
        private int _indicesCount;
        private bool _isStarted;

        private int _vertexCount;

#if !ORIONSORT
        private float _z;
#endif

        public SpriteBatch3D(GraphicsDevice device)
        {
            GraphicsDevice = device;

            _effect = new Effect(GraphicsDevice,
                File.ReadAllBytes(Path.Combine(Bootstrap.ExeDirectory, "shaders/IsometricWorld.fxc")));

            _effect.Parameters["HuesPerTexture"].SetValue((float)IO.Resources.Hues.HuesCount);

            _drawLightingEffect = _effect.Parameters["DrawLighting"];
            _projectionMatrixEffect = _effect.Parameters["ProjectionMatrix"];
            _worldMatrixEffect = _effect.Parameters["WorldMatrix"];
            _viewportEffect = _effect.Parameters["Viewport"];

            _huesTechnique = _effect.Techniques["HueTechnique"];
            _shadowTechnique = _effect.Techniques["ShadowSetTechnique"];

            _drawCalls = new DrawCallInfo[MAX_SPRITES];

            _vertexBuffer = new DynamicVertexBuffer(GraphicsDevice, SpriteVertex.VertexDeclaration, _vertices.Length,
                BufferUsage.WriteOnly);
            _indexBuffer = new DynamicIndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, _indices.Length,
                BufferUsage.WriteOnly);
        }

        public GraphicsDevice GraphicsDevice { get;}
        public Matrix ProjectionMatrixWorld => Matrix.Identity;

        public Matrix ProjectionMatrixScreen => Matrix.CreateOrthographicOffCenter(0, GraphicsDevice.Viewport.Width,
            GraphicsDevice.Viewport.Height, 0f, short.MinValue, short.MaxValue);

        public int Merged { get; private set; }
        public int Calls { get; private set; }

        public int TotalCalls => Merged + Calls;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddQuadrilateralIndices(int indexOffset)
        {
            _geometryIndices[0] = (short)(0 + indexOffset);
            _geometryIndices[1] = (short)(1 + indexOffset);
            _geometryIndices[2] = (short)(2 + indexOffset);
            _geometryIndices[3] = (short)(1 + indexOffset);
            _geometryIndices[4] = (short)(3 + indexOffset);
            _geometryIndices[5] = (short)(2 + indexOffset);
        }

        public void SetLightDirection(Vector3 dir)
        {
            _effect.Parameters["lightDirection"].SetValue(dir);
        }

        public void SetLightIntensity(float inte)
        {
            _effect.Parameters["lightIntensity"].SetValue(inte);
        }

#if !ORIONSORT
        public float GetZ() => _z++;
#endif

        public void Begin()
        {
            if (_isStarted)
                throw new Exception();

            _isStarted = true;

            Merged = 0;
#if !ORIONSORT
            _z = 0;
#endif
            _drawingArea.Min = _minVector3;
            _drawingArea.Max = new Vector3(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, int.MaxValue);
        }

        public bool DrawSprite(Texture2D texture, SpriteVertex[] vertices, Techniques technique = Techniques.Default)
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
            Build(texture, vertices, technique);

            return true;
        }

        public void DrawShadow(Texture2D texture, SpriteVertex[] vertices, Vector2 position, bool flip, float z)
        {
            if (texture == null || texture.IsDisposed)
                return;

            vertices[0].Position.Z =
                vertices[1].Position.Z =
                    vertices[2].Position.Z =
                        vertices[3].Position.Z = z;
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

            Build(texture, vertices, Techniques.ShadowSet);
        }

        private unsafe void Build(Texture2D texture, SpriteVertex[] vertices, Techniques technique)
        {
            AddQuadrilateralIndices(_vertexCount);

            if (_vertexCount + VERTEX_COUNT > _vertices.Length || _indicesCount + INDEX_COUNT > _indices.Length)
                Flush();

            DrawCallInfo call = new DrawCallInfo(texture, technique, _indicesCount, PRIMITIVES_COUNT, 0);

            //Array.Copy(vertices, 0, _vertices, _vertexCount, VERTEX_COUNT);

            fixed (SpriteVertex* p = &_vertices[_vertexCount])
            {
                fixed (SpriteVertex* t = &vertices[0])
                {
                    SpriteVertex* ptr = p;
                    SpriteVertex* tptr = t;

                    *ptr++ = *tptr++;
                    *ptr++ = *tptr++;
                    *ptr++ = *tptr++;
                    *ptr = *tptr;
                }
            }

            _vertexCount += VERTEX_COUNT;

            //Array.Copy(_geometryIndices, 0, _indices, _indicesCount, INDEX_COUNT);

            fixed (short* p = &_indices[_indicesCount])
            {
                fixed (short* t = &_geometryIndices[0])
                {
                    short* ptr = p;
                    short* tptr = t;

                    *ptr++ = *tptr++;
                    *ptr++ = *tptr++;
                    *ptr++ = *tptr++;
                    *ptr++ = *tptr++;
                    *ptr++ = *tptr++;
                    *ptr = *tptr;
                }
            }

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

            if (Calls >= _drawCalls.Length)
                Flush();

            _drawCalls[Calls++] = call;
        }

        public void End()
        {
            if (!_isStarted)
                throw new Exception();

            Flush();
            _isStarted = false;
        }

        public void EnableLight(bool value)
        {
            _drawLightingEffect.SetValue(value);
        }

        private void Flush()
        {
            if (Calls == 0)
                return;

            SetupBuffers();
            ApplyStates();
            InternalDraw();
        }

        private unsafe void SetupBuffers()
        {
            //_vertexBuffer.SetData(_vertices, 0, _vertexCount);

            fixed (SpriteVertex* p = &_vertices[0])
            {
                _vertexBuffer.SetDataPointerEXT(0, (IntPtr)p, _vertexCount * SpriteVertex.SizeInBytes, SetDataOptions.None);
            }

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

        private void ApplyStates()
        {
            GraphicsDevice.BlendState = BlendState.AlphaBlend;
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
            GraphicsDevice.SamplerStates[1] = SamplerState.PointClamp;
            GraphicsDevice.SamplerStates[2] = SamplerState.PointClamp;

            // set up viewport.
            _projectionMatrixEffect.SetValue(ProjectionMatrixScreen);
            _worldMatrixEffect.SetValue(ProjectionMatrixWorld);
            _viewportEffect.SetValue(new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height));

            GraphicsDevice.DepthStencilState = _dss;
        }

        private void InternalDraw()
        {
            if (Calls == 0)
                return;

            Techniques last = Techniques.None;

            for (int i = 0; i < Calls; i++)
            {
                ref DrawCallInfo call = ref _drawCalls[i];

                switch (call.Technique)
                {
                    case Techniques.Hued:
                        if (last != call.Technique)
                        {
                            _effect.CurrentTechnique = _huesTechnique;
                            _effect.CurrentTechnique.Passes[0].Apply();
                        }
                        break;
                    case Techniques.ShadowSet:
                        if (last != call.Technique)
                        {
                            _effect.CurrentTechnique = _shadowTechnique;
                            _effect.CurrentTechnique.Passes[0].Apply();
                        }
                        break;
                }

                last = call.Technique;

                DoDraw(ref call);
            }

            Array.Clear(_drawCalls, 0, Calls);
            Calls = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoDraw(ref DrawCallInfo call)
        {
            GraphicsDevice.Textures[0] = call.Texture;
            GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, call.StartIndex, call.PrimitiveCount);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct DrawCallInfo : IComparable<DrawCallInfo>
        {
            public unsafe DrawCallInfo(Texture2D texture, Techniques technique, int start, int count, float depth)
            {
                Texture = texture;
                TextureKey = (uint)RuntimeHelpers.GetHashCode(texture);
                Technique = technique;
                StartIndex = start;
                PrimitiveCount = count;
                DepthKey = *(uint*)&depth;
            }

            public readonly Texture2D Texture;
            public readonly uint TextureKey;
            public int StartIndex;
            public int PrimitiveCount;
            public readonly uint DepthKey;
            public readonly Techniques Technique;

            public bool TryMerge(ref DrawCallInfo callInfo)
            {
                if (Technique != callInfo.Technique || TextureKey != callInfo.TextureKey || DepthKey != callInfo.DepthKey)
                    return false;
                callInfo.PrimitiveCount += PrimitiveCount;
                return true;
            }

            public int CompareTo(DrawCallInfo other)
            {
                int result = TextureKey.CompareTo(other.TextureKey);
                if (result != 0)
                    return result;
                result = DepthKey.CompareTo(other.DepthKey);

                return result != 0 ? result : ((byte)Technique).CompareTo((byte)other.Technique);
            }
        }
    }
#endif
}