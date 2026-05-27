// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Assets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ClassicUO.Renderer
{
    public struct GlyphAtlasEntry
    {
        public Texture2D Texture;
        public Rectangle UV;
        public int BearingX;
        public int BearingY;
        public int AdvanceWidth;
        public int GlyphWidth;
        public int GlyphHeight;

        public bool IsValid => Texture != null;

        public static readonly GlyphAtlasEntry Empty = new GlyphAtlasEntry();
    }

    public sealed class FontGlyphAtlas : IDisposable
    {
        private readonly Dictionary<long, GlyphAtlasEntry> _cache = new Dictionary<long, GlyphAtlasEntry>();
        private readonly Dictionary<(long, uint), GlyphAtlasEntry> _coloredCache = new Dictionary<(long, uint), GlyphAtlasEntry>();
        private readonly TextureAtlas _atlas;
        private readonly FontsLoader _fontsLoader;

        public FontGlyphAtlas(FontsLoader fontsLoader, GraphicsDevice device)
        {
            _fontsLoader = fontsLoader;
            _atlas = new TextureAtlas(device, 2048, 2048, SurfaceFormat.Color);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long MakeKey(byte font, char character, bool isUnicode, bool hasBorder, bool isSolid, bool isItalic)
        {
            // Pack into long: font(8) | char(16) | unicode(1) | border(1) | solid(1) | italic(1)
            return ((long)font << 20)
                 | ((long)(ushort)character << 4)
                 | (isUnicode ? 8L : 0L)
                 | (hasBorder ? 4L : 0L)
                 | (isSolid ? 2L : 0L)
                 | (isItalic ? 1L : 0L);
        }

        /// <summary>
        /// Get a glyph entry without baked color (color = 0, rendered as white for Unicode).
        /// Used for non-HTML text where hue is applied via shader at draw time.
        /// </summary>
        public ref readonly GlyphAtlasEntry GetEntry(byte font, char character, bool isUnicode, bool hasBorder, bool isSolid, bool isItalic)
        {
            long key = MakeKey(font, character, isUnicode, hasBorder, isSolid, isItalic);

            ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(_cache, key, out bool exists);

            if (!exists)
            {
                entry = CreateEntry(font, character, isUnicode, hasBorder, isSolid, isItalic, 0);
            }

            return ref entry;
        }

        /// <summary>
        /// Get a glyph entry with a specific baked ARGB color.
        /// Used for HTML text where each character can have a different color.
        /// </summary>
        public GlyphAtlasEntry GetColoredEntry(byte font, char character, bool isUnicode, bool hasBorder, bool isSolid, bool isItalic, uint color)
        {
            if (color == 0)
            {
                return GetEntry(font, character, isUnicode, hasBorder, isSolid, isItalic);
            }

            long baseKey = MakeKey(font, character, isUnicode, hasBorder, isSolid, isItalic);
            var key = (baseKey, color);

            ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(_coloredCache, key, out bool exists);

            if (!exists)
            {
                entry = CreateEntry(font, character, isUnicode, hasBorder, isSolid, isItalic, color);
            }

            return entry;
        }

        private GlyphAtlasEntry CreateEntry(byte font, char character, bool isUnicode, bool hasBorder, bool isSolid, bool isItalic, uint colorOrHue)
        {
            FontsLoader.SingleGlyphInfo glyphInfo;

            if (isUnicode)
            {
                glyphInfo = _fontsLoader.RenderSingleGlyphUnicode(font, character, hasBorder, isSolid, isItalic, colorOrHue);
            }
            else
            {
                // For ASCII, colorOrHue is the hue index (ushort)
                glyphInfo = _fontsLoader.RenderSingleGlyphASCII(font, character, (ushort)colorOrHue);
            }

            if (glyphInfo.Data == null || glyphInfo.Data.Length == 0 || glyphInfo.Width <= 0 || glyphInfo.Height <= 0)
            {
                return new GlyphAtlasEntry
                {
                    Texture = null,
                    BearingX = glyphInfo.BearingX,
                    BearingY = glyphInfo.BearingY,
                    AdvanceWidth = glyphInfo.AdvanceWidth,
                    GlyphWidth = 0,
                    GlyphHeight = 0
                };
            }

            var texture = _atlas.AddSprite(
                glyphInfo.Data,
                glyphInfo.Width,
                glyphInfo.Height,
                out var uv
            );

            return new GlyphAtlasEntry
            {
                Texture = texture,
                UV = uv,
                BearingX = glyphInfo.BearingX,
                BearingY = glyphInfo.BearingY,
                AdvanceWidth = glyphInfo.AdvanceWidth,
                GlyphWidth = glyphInfo.Width,
                GlyphHeight = glyphInfo.Height
            };
        }

        public void Dispose()
        {
            _atlas?.Dispose();
            _cache.Clear();
            _coloredCache.Clear();
        }
    }
}
