// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static ClassicUO.Renderer.UltimaBatcher2D;

namespace ClassicUO.Renderer
{
    public struct TextureRun
    {
        public Texture2D Texture;
        public int Start;
        public int Count;
    }

    /// <summary>
    /// A GPU-buffered layer of sprites (vertices + textures + visibility) with
    /// per-frame index buffer filtering. Sprites must be added in texture-sorted
    /// order (via bucket-insert in the caller) so that draw call batching is optimal.
    /// </summary>
    public sealed class MeshLayer
    {
        private const int INITIAL_CAPACITY = 64;

        // Sprite data (parallel arrays, must be in texture-sorted order)
        public PositionNormalTextureColor4[] Vertices = new PositionNormalTextureColor4[INITIAL_CAPACITY];
        public Texture2D[] Textures = new Texture2D[INITIAL_CAPACITY];
        public int Count;

        // GPU vertex buffer
        public DynamicVertexBuffer VertexBuffer;

        // Per-frame visibility filtering
        public bool[] Visible = new bool[INITIAL_CAPACITY];
        private short[] _visibleIndexData = new short[INITIAL_CAPACITY * 6];
        public int VisibleSpriteCount;
        public TextureRun[] VisibleRuns = new TextureRun[16];
        public int VisibleRunCount;
        private bool _visibilityDirty = true;
        private bool _alphaDirty;
        private bool[] _prevVisible = new bool[INITIAL_CAPACITY];

        // Track which sprite indices had alpha modified (avoids full scan in ResetAlpha)
        private int[] _alphaDirtyIndices = new int[16];
        private int _alphaDirtyCount;


        /// <summary>
        /// Ensures internal arrays can hold at least <paramref name="minCapacity"/> sprites.
        /// </summary>
        public void EnsureCapacity(int minCapacity)
        {
            if (minCapacity <= Vertices.Length)
                return;

            int newSize = Vertices.Length;
            while (newSize < minCapacity)
                newSize *= 2;

            Array.Resize(ref Vertices, newSize);
            Array.Resize(ref Textures, newSize);
            Array.Resize(ref Visible, newSize);
        }

        /// <summary>
        /// Resets all visibility flags to false. Called each frame before
        /// AddTileToRenderList marks visible sprites.
        /// </summary>
        public void ResetVisibility()
        {
            if (Count > 0)
                Array.Clear(Visible, 0, Count);
        }

        /// <summary>
        /// Marks a sprite as visible this frame and updates its vertex alpha.
        /// AlphaHue=0 means "not yet processed" → treat as fully opaque.
        /// AlphaHue=255 means fully opaque. Any other value is a fade in progress.
        /// </summary>
        public void SetVisible(int index, byte alphaHue, bool circletrans = false)
        {
            Visible[index] = true;

            float alpha;
            if (circletrans)
            {
                // Alpha > 1.0 signals the shader to apply circle of transparency
                alpha = (alphaHue == 0 ? 1f : alphaHue / 255f) + 1f;
            }
            else if (alphaHue != 0 && alphaHue != 0xFF)
            {
                alpha = alphaHue / 255f;
            }
            else
            {
                return;
            }

            ref var v = ref Vertices[index];
            v.Hue0.Z = alpha;
            v.Hue1.Z = alpha;
            v.Hue2.Z = alpha;
            v.Hue3.Z = alpha;

            if (_alphaDirtyCount >= _alphaDirtyIndices.Length)
                Array.Resize(ref _alphaDirtyIndices, _alphaDirtyIndices.Length * 2);
            _alphaDirtyIndices[_alphaDirtyCount++] = index;

            MarkVertexDirty();
        }

        /// <summary>
        /// Resets vertex alphas back to fully opaque for sprites that were modified.
        /// Only touches indices tracked by SetVisible, avoiding a full scan.
        /// </summary>
        public void ResetAlpha()
        {
            if (_alphaDirtyCount == 0)
                return;

            for (int i = 0; i < _alphaDirtyCount; i++)
            {
                ref var v = ref Vertices[_alphaDirtyIndices[i]];
                v.Hue0.Z = 1f;
                v.Hue1.Z = 1f;
                v.Hue2.Z = 1f;
                v.Hue3.Z = 1f;
            }

            _alphaDirty = true;
            _alphaDirtyCount = 0;
        }

        /// <summary>
        /// Sets the hue (X=hue index, Y=shader type) on a meshed sprite, preserving alpha (Z).
        /// Used per-frame to apply out-of-range color or sync with runtime hue changes.
        /// </summary>
        public void SetHue(int index, float hueX, float hueY)
        {
            ref var v = ref Vertices[index];
            if (v.Hue0.X == hueX && v.Hue0.Y == hueY)
                return;

            float alpha = v.Hue0.Z;
            var h = new Vector3(hueX, hueY, alpha);
            v.Hue0 = v.Hue1 = v.Hue2 = v.Hue3 = h;

            MarkVertexDirty();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MarkVertexDirty()
        {
            _alphaDirty = true;
        }

        /// <summary>
        /// Writes a quad at a specific index from a texture atlas source rectangle.
        /// Does not modify Count — caller is responsible for setting Count after all writes.
        /// </summary>
        public void WriteQuadAt(int index, Texture2D texture, Rectangle sourceRect, int posX, int posY,
            Vector3 hue, float depth, float uvInset = 0f)
        {
            ref var vertex = ref Vertices[index];

            float sourceX = (sourceRect.X + uvInset) / (float)texture.Width;
            float sourceY = (sourceRect.Y + uvInset) / (float)texture.Height;
            float sourceW = (sourceRect.Width - uvInset * 2f) / (float)texture.Width;
            float sourceH = (sourceRect.Height - uvInset * 2f) / (float)texture.Height;

            vertex.Position0.X = posX;
            vertex.Position0.Y = posY;
            vertex.Position1.X = posX + sourceRect.Width;
            vertex.Position1.Y = posY;
            vertex.Position2.X = posX;
            vertex.Position2.Y = posY + sourceRect.Height;
            vertex.Position3.X = posX + sourceRect.Width;
            vertex.Position3.Y = posY + sourceRect.Height;

            vertex.Position0.Z = vertex.Position1.Z = vertex.Position2.Z = vertex.Position3.Z = depth;

            vertex.TextureCoordinate0 = new Vector3(sourceX, sourceY, 0);
            vertex.TextureCoordinate1 = new Vector3(sourceW + sourceX, sourceY, 0);
            vertex.TextureCoordinate2 = new Vector3(sourceX, sourceH + sourceY, 0);
            vertex.TextureCoordinate3 = new Vector3(sourceW + sourceX, sourceH + sourceY, 0);

            vertex.Normal0 = vertex.Normal1 = vertex.Normal2 = vertex.Normal3 = new Vector3(0, 0, 1);
            vertex.Hue0 = vertex.Hue1 = vertex.Hue2 = vertex.Hue3 = hue;

            Textures[index] = texture;
        }

        /// <summary>
        /// Uploads all vertex data to the GPU buffer. Creates or resizes as needed.
        /// </summary>
        public unsafe void UploadVertexBuffer(GraphicsDevice graphicsDevice)
        {
            if (Count == 0)
                return;

            int requiredVertices = Count * 4;
            if (VertexBuffer == null || VertexBuffer.IsDisposed || VertexBuffer.VertexCount < requiredVertices)
            {
                VertexBuffer?.Dispose();
                VertexBuffer = new DynamicVertexBuffer(
                    graphicsDevice,
                    typeof(PositionNormalTextureColor4),
                    requiredVertices,
                    BufferUsage.WriteOnly
                );
            }

            fixed (PositionNormalTextureColor4* p = &Vertices[0])
            {
                VertexBuffer.SetDataPointerEXT(
                    0,
                    (IntPtr)p,
                    Count * PositionNormalTextureColor4.SIZE_IN_BYTES,
                    SetDataOptions.Discard
                );
            }
        }

        /// <summary>
        /// Builds per-frame index data from the Visible[] flags. Sprites are already
        /// in texture-sorted order, so the visible subset preserves texture grouping for
        /// optimal draw call batching. Skips rebuild if visibility is unchanged.
        /// </summary>
        public bool BuildVisibleIndices()
        {
            if (Count == 0)
            {
                VisibleSpriteCount = 0;
                VisibleRunCount = 0;
                return false;
            }

            // Check if visibility changed since last frame
            if (!_visibilityDirty)
            {
                if (Visible.AsSpan(0, Count).SequenceEqual(_prevVisible.AsSpan(0, Count)))
                    return VisibleSpriteCount > 0;
            }

            // Save current visibility for next frame comparison
            if (_prevVisible.Length < Count)
                _prevVisible = new bool[Count];
            Visible.AsSpan(0, Count).CopyTo(_prevVisible.AsSpan(0, Count));
            _visibilityDirty = false;

            // Rebuild index data
            VisibleSpriteCount = 0;
            VisibleRunCount = 0;

            int maxIndices = Count * 6;
            if (_visibleIndexData.Length < maxIndices)
                _visibleIndexData = new short[maxIndices];

            Texture2D curTexture = null;
            int runStart = 0;
            int indexPos = 0;

            // Max 8191 sprites per layer (short index: 8191 * 4 = 32764 < 32767)
            for (int i = 0; i < Count; i++)
            {
                if (!Visible[i])
                    continue;

                var tex = Textures[i];
                if (tex != curTexture)
                {
                    if (curTexture != null && VisibleSpriteCount > runStart)
                    {
                        if (VisibleRunCount >= VisibleRuns.Length)
                            Array.Resize(ref VisibleRuns, VisibleRuns.Length * 2);

                        VisibleRuns[VisibleRunCount++] = new TextureRun
                        {
                            Texture = curTexture,
                            Start = runStart,
                            Count = VisibleSpriteCount - runStart
                        };
                    }
                    curTexture = tex;
                    runStart = VisibleSpriteCount;
                }

                short baseVertex = (short)(i * 4);
                _visibleIndexData[indexPos++] = baseVertex;
                _visibleIndexData[indexPos++] = (short)(baseVertex + 1);
                _visibleIndexData[indexPos++] = (short)(baseVertex + 2);
                _visibleIndexData[indexPos++] = (short)(baseVertex + 1);
                _visibleIndexData[indexPos++] = (short)(baseVertex + 3);
                _visibleIndexData[indexPos++] = (short)(baseVertex + 2);

                VisibleSpriteCount++;
            }

            if (curTexture != null && VisibleSpriteCount > runStart)
            {
                if (VisibleRunCount >= VisibleRuns.Length)
                    Array.Resize(ref VisibleRuns, VisibleRuns.Length * 2);

                VisibleRuns[VisibleRunCount++] = new TextureRun
                {
                    Texture = curTexture,
                    Start = runStart,
                    Count = VisibleSpriteCount - runStart
                };
            }

            return VisibleSpriteCount > 0;
        }

        /// <summary>
        /// Uploads the per-frame visible index data to a shared DynamicIndexBuffer.
        /// </summary>
        public unsafe void UploadVisibleIndices(DynamicIndexBuffer indexBuffer)
        {
            if (VisibleSpriteCount == 0)
                return;

            fixed (short* p = &_visibleIndexData[0])
            {
                indexBuffer.SetDataPointerEXT(
                    0,
                    (IntPtr)p,
                    VisibleSpriteCount * 6 * sizeof(short),
                    SetDataOptions.Discard
                );
            }
        }

        /// <summary>
        /// Re-uploads vertex data when alpha/hue has been modified.
        /// Uses full buffer Discard to avoid partial-update driver bugs on Intel GPUs.
        /// </summary>
        public unsafe void FlushAlphaChanges()
        {
            if (!_alphaDirty || Count == 0 || VertexBuffer == null || VertexBuffer.IsDisposed)
                return;

            _alphaDirty = false;

            fixed (PositionNormalTextureColor4* p = &Vertices[0])
            {
                VertexBuffer.SetDataPointerEXT(
                    0,
                    (IntPtr)p,
                    Count * PositionNormalTextureColor4.SIZE_IN_BYTES,
                    SetDataOptions.Discard
                );
            }
        }

        public void Reset()
        {
            Count = 0;
            VisibleSpriteCount = 0;
            VisibleRunCount = 0;
            _visibilityDirty = true;
            _alphaDirty = false;
            _alphaDirtyCount = 0;
        }

        /// <summary>
        /// Resets all state for reuse from a pool. Keeps GPU buffer and arrays allocated.
        /// </summary>
        public void SoftReset()
        {
            Count = 0;
            VisibleSpriteCount = 0;
            VisibleRunCount = 0;
            _visibilityDirty = true;
            _alphaDirty = false;
            _alphaDirtyCount = 0;

            // Clear texture references so they can be GC'd if atlas rebuilds
            if (Textures.Length > 0)
                Array.Clear(Textures, 0, Textures.Length);
        }

        public void Dispose()
        {
            VertexBuffer?.Dispose();
            VertexBuffer = null;
        }
    }
}
