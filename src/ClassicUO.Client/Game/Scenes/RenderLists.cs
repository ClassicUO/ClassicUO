// SPDX-License-Identifier: BSD-2-Clause
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Map;
using ClassicUO.Renderer;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ClassicUO.Game.Scenes
{
    /// <summary>
    /// A queued draw into the non-atlas gump layer. Prefer the typed text path:
    /// store a reference to the <see cref="RenderedText"/> plus its draw parameters.
    /// This avoids allocating a closure per frame and makes it safe to skip entries
    /// whose text was destroyed/recycled (pooled via <see cref="RenderedText"/>'s
    /// internal pool) between queue and flush.
    ///
    /// Callers that need an arbitrary non-text draw (clipping, compound operations,
    /// solid color rectangles, etc.) use the <see cref="Callback"/> path.
    /// </summary>
    internal readonly struct NoAtlasGumpCommand
    {
        public readonly RenderedText Text;
        public readonly int X;
        public readonly int Y;
        public readonly float LayerDepth;
        public readonly float Alpha;
        public readonly ushort Hue;
        public readonly Func<UltimaBatcher2D, bool> Callback;

        public NoAtlasGumpCommand(RenderedText text, int x, int y, float layerDepth, float alpha, ushort hue)
        {
            Text = text;
            X = x;
            Y = y;
            LayerDepth = layerDepth;
            Alpha = alpha;
            Hue = hue;
            Callback = null;
        }

        public NoAtlasGumpCommand(Func<UltimaBatcher2D, bool> callback)
        {
            Text = null;
            X = 0;
            Y = 0;
            LayerDepth = 0f;
            Alpha = 0f;
            Hue = 0;
            Callback = callback;
        }
    }

    /// <summary>
    /// Represents an ordered queue of GameObjects to be rendered.
    /// The order is determined by the draw order, not by the insertion order.
    /// Implementation for sorting and processing is passed as delegates.
    /// </summary>
    internal class RenderLists
    {
        private readonly List<GameObject> _tiles = [];
        private readonly List<GameObject> _stretchedTiles = [];
        private readonly List<GameObject> _statics = [];
        private readonly List<GameObject> _animations = [];
        private readonly List<GameObject> _effects = [];
        private readonly List<GameObject> _transparentObjects = [];
        private readonly List<Func<UltimaBatcher2D, bool>> _gumpSprites = [];
        private readonly List<NoAtlasGumpCommand> _gumpTexts = [];

        public void Clear()
        {
            _tiles.Clear();
            _stretchedTiles.Clear();
            _statics.Clear();
            _animations.Clear();
            _effects.Clear();
            _transparentObjects.Clear();
            _gumpSprites.Clear();
            _gumpTexts.Clear();
        }

        public void Add(GameObject toRender, bool isTransparent = false)
        {
            if (isTransparent)
            {
                _transparentObjects.Add(toRender);
                return;
            }

            switch (toRender)
            {
                case Land land:
                    if (land.IsStretched)
                    {
                        _stretchedTiles.Add(toRender);
                    }
                    else
                    {
                        _tiles.Add(toRender);
                    }
                    break;

                case Static:
                case Multi:
                    _statics.Add(toRender);
                    break;

                case Mobile:
                    _animations.Add(toRender);
                    break;

                case Item item:
                    if (item.IsCorpse)
                    {
                        _animations.Add(toRender);
                    }
                    else
                    {
                        _statics.Add(toRender);
                    }
                    break;

                case GameEffect:
                    _effects.Add(toRender);
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// This is an intermediate, crappy solution. Rewriting gump rendering would be way too much at this point.
        /// Adding gump elements that use atlas textures for efficient rendering.
        /// </summary>
        /// <param name="toRender"></param>
        public void AddGumpWithAtlas(Func<UltimaBatcher2D, bool> toRender)
        {
            _gumpSprites.Add(toRender);
        }

        /// <summary>
        /// Queue a <see cref="RenderedText"/> draw into the non-atlas gump layer.
        /// This is the preferred path: allocation-free (struct value), insertion-order
        /// preserved alongside <see cref="AddGumpNoAtlas(Func{UltimaBatcher2D, bool})"/>
        /// fallback entries, and flushed with a validity guard against destroyed/recycled
        /// text references.
        /// </summary>
        public void AddGumpNoAtlas(RenderedText text, int x, int y, float layerDepth, float alpha = 1f, ushort hue = 0)
        {
            if (text == null)
            {
                return;
            }

            _gumpTexts.Add(new NoAtlasGumpCommand(text, x, y, layerDepth, alpha, hue));
        }

        /// <summary>
        /// Fallback: queue an arbitrary draw closure into the non-atlas gump layer.
        /// Use this for compound operations (clipping, nested render lists, solid-color
        /// rectangles) that don't fit the <see cref="RenderedText"/> fast path. New code
        /// should prefer the typed overload when drawing text.
        /// </summary>
        public void AddGumpNoAtlas(Func<UltimaBatcher2D, bool> toRender)
        {
            if (toRender == null)
            {
                return;
            }

            _gumpTexts.Add(new NoAtlasGumpCommand(toRender));
        }

        // Test accessors. Kept internal; allow unit tests to inspect what was queued
        // without requiring a live graphics device to invoke the flush path.
        internal int GumpTextsCount => _gumpTexts.Count;
        internal NoAtlasGumpCommand PeekGumpText(int index) => _gumpTexts[index];

        public int DrawRenderLists(UltimaBatcher2D batcher, sbyte maxGroundZ)
        {
            int result = DrawRenderList(batcher, _tiles, maxGroundZ) +
                   DrawRenderList(batcher, _stretchedTiles, maxGroundZ) +
                   DrawRenderList(batcher, _statics, maxGroundZ) +
                   DrawRenderList(batcher, _animations, maxGroundZ) +
                   DrawRenderList(batcher, _effects, maxGroundZ);

            if (_transparentObjects.Count > 0 || _gumpSprites.Count > 0 || _gumpTexts.Count > 0)
            {
                result += DrawRenderList(batcher, _transparentObjects, maxGroundZ);
                result += DrawRenderListWithAtlas(batcher, _gumpSprites);
                result += DrawRenderListNoAtlas(batcher, _gumpTexts);
            }

            return result;
        }

        public int DrawRenderLists(UltimaBatcher2D batcher, sbyte maxGroundZ, List<Chunk> visibleChunks, int offsetX, int offsetY)
        {
            int result = 0;

            // Build visible indices for all chunks (skip rebuild if visibility unchanged)
            foreach (var chunk in visibleChunks)
            {
                var mesh = chunk.Mesh;
                if (mesh.Land.Count > 0)
                    mesh.Land.BuildVisibleIndices();
                if (mesh.Statics.Count > 0)
                    mesh.Statics.BuildVisibleIndices();
            }

            // Draw chunk mesh land tiles from GPU buffers with per-frame visibility
            batcher.SetWorldOffset(offsetX, offsetY);
            foreach (var chunk in visibleChunks)
                result += DrawMeshLayer(batcher, chunk.Mesh.Land);
            batcher.ResetWorldOffset();

            // Draw excluded land tiles (animated water, etc.)
            result += DrawRenderList(batcher, _tiles, maxGroundZ);
            result += DrawRenderList(batcher, _stretchedTiles, maxGroundZ);

            // Draw chunk mesh statics from GPU buffers with per-frame visibility
            batcher.SetWorldOffset(offsetX, offsetY);
            foreach (var chunk in visibleChunks)
                result += DrawMeshLayer(batcher, chunk.Mesh.Statics);
            batcher.ResetWorldOffset();

            // Draw excluded statics + animations + effects
            result += DrawRenderList(batcher, _statics, maxGroundZ) +
                   DrawRenderList(batcher, _animations, maxGroundZ) +
                   DrawRenderList(batcher, _effects, maxGroundZ);

            if (_transparentObjects.Count > 0 || _gumpSprites.Count > 0 || _gumpTexts.Count > 0)
            {
                //batcher.SetStencil(DepthStencilState.DepthRead);
                result += DrawRenderList(batcher, _transparentObjects, maxGroundZ);
                result += DrawRenderListWithAtlas(batcher, _gumpSprites);
                result += DrawRenderListNoAtlas(batcher, _gumpTexts);
                //batcher.SetStencil(null);
            }

            return result;
        }

        private static int DrawMeshLayer(UltimaBatcher2D batcher, MeshLayer layer)
        {
            if (layer.VisibleSpriteCount == 0 || layer.VertexBuffer == null || layer.VertexBuffer.IsDisposed)
                return 0;

            layer.FlushAlphaChanges();

            var indexBuffer = batcher.GetDynamicIndexBuffer(layer.VisibleSpriteCount * 6);
            layer.UploadVisibleIndices(indexBuffer);

            batcher.GraphicsDevice.SetVertexBuffer(layer.VertexBuffer);
            batcher.GraphicsDevice.Indices = indexBuffer;

            for (int i = 0; i < layer.VisibleRunCount; i++)
            {
                ref var run = ref layer.VisibleRuns[i];
                batcher.DrawDirectIndexed(run.Texture, run.Start * 6, run.Count * 2, layer.Count * 4);
            }

            return layer.VisibleSpriteCount;
        }

        private static int DrawRenderList(UltimaBatcher2D batcher, List<GameObject> renderList, sbyte maxGroundZ)
        {
            int done = 0;

            foreach (var obj in renderList)
            {
                if (obj.Z <= maxGroundZ)
                {
                    float depth = obj.CalculateDepthZ();

                    if (obj.Draw(batcher, obj.RealScreenPosition.X, obj.RealScreenPosition.Y, depth))
                    {
                        done++;
                    }
                }
            }

            return done;
        }

        private static int DrawRenderListWithAtlas(UltimaBatcher2D batcher, List<Func<UltimaBatcher2D, bool>> renderList)
        {
            int done = 0;

            foreach (var obj in renderList)
            {
                if (obj.Invoke(batcher))
                {
                    done++;
                }
            }

            return done;
        }

        private static int DrawRenderListNoAtlas(UltimaBatcher2D batcher, List<NoAtlasGumpCommand> renderList)
        {
            int done = 0;

            // AsSpan avoids the List<T> enumerator allocation on the hot path.
            var span = CollectionsMarshal.AsSpan(renderList);
            for (int i = 0; i < span.Length; i++)
            {
                ref readonly var cmd = ref span[i];

                if (cmd.Text != null)
                {
                    // Typed fast path. HasContent rejects destroyed/empty text, which is
                    // possible when the underlying instance was returned to the pool
                    // between queue and flush.
                    if (!cmd.Text.HasContent)
                    {
                        continue;
                    }

                    if (cmd.Text.Draw(batcher, cmd.X, cmd.Y, cmd.LayerDepth, cmd.Alpha, cmd.Hue))
                    {
                        done++;
                    }
                }
                else if (cmd.Callback != null && cmd.Callback.Invoke(batcher))
                {
                    done++;
                }
            }

            return done;
        }
    }
}
