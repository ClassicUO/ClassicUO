// UO bitmap font rendering for Bevy.UI's Text elements. Replaces the
// FontStashSharp TTF path (FontStashTextMeasurer + DrawText via
// FontSystem.GetFont) with FontsLoader.GenerateUnicode → Texture2D atlas
// per (text, font, color, width) tuple. The measurer Clay queries during
// layout uses the same FontsLoader so MeasureText and the eventual draw
// agree on glyph dimensions.
//
// Color model: Bevy.UI Text components carry a ClayColor (RGB). UO fonts
// are tinted via a UO hue index (HuesLoader palette → ColorTable[cell]).
// To bridge: text entities that want UO hue coloring carry the
// UoTextHue component; renderer reads it. Without UoTextHue, the bitmap
// is baked as white pixels and tinted at draw time by the ClayColor's
// shader-hue vector (no_hue=identity, otherwise BB/GG/RR multiply).

using System;
using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyEcs;
using TinyEcs.Bevy;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace ClassicUO.Ecs;

internal readonly struct UoFontPlugin : IPlugin
{
    public void Build(App app)
    {
        // FontsLoader is loaded by AssetsPlugin in Stage.Startup. We can't
        // wire it into UoFontMeasurer at IPlugin.Build time because the
        // measurer is constructed *before* the loader runs. Defer the
        // wiring to a Startup system that fires after AssetsPlugin's load.
        app.AddSystem(Stage.Startup, (Res<UOFileManager> fileManager, Res<UltimaBatcher2D> batcher) =>
        {
            UoFontRuntime.Fonts = fileManager.Value.Fonts;
            UoFontRuntime.Hues = fileManager.Value.Hues;
            UoFontRuntime.Device = batcher.Value.GraphicsDevice;
        });
    }
}

// Global access for the measurer + renderer. Set once at startup, read on
// every text element. ITextMeasurer + the DrawText path don't get system
// param injection so we can't pull Res<UOFileManager> at the call site.
internal static class UoFontRuntime
{
    public static FontsLoader? Fonts;
    public static HuesLoader? Hues;
    public static GraphicsDevice? Device;

    // Default font index for ECS text. Matches OOP's gump rendering which
    // uses font 1 throughout (Generic gump font). FontId on Bevy.UI Text
    // components < 20 maps directly; >= 20 falls back here.
    public const byte DefaultFont = 1;

    public static byte ResolveFont(ushort fontId)
        => fontId < 20 ? (byte)fontId : DefaultFont;
}

internal sealed class UoFontTextMeasurer : Clay.ITextMeasurer
{
    public Clay.Dimensions MeasureText(ReadOnlySpan<char> text, ushort fontId, ushort fontSize, ushort letterSpacing)
    {
        var fonts = UoFontRuntime.Fonts;
        if (fonts == null || text.IsEmpty)
            return new Clay.Dimensions(0, fontSize);

        var font = UoFontRuntime.ResolveFont(fontId);
        var str = text.ToString();
        var w = fonts.GetWidthUnicode(font, str);
        var h = fonts.GetHeightUnicode(font, str, w, TEXT_ALIGN_TYPE.TS_LEFT, 0);
        // Clay uses the measured dimensions to lay out a single text
        // element rect. Wrap (handled upstream by ServerGumpPlugin.WrapText)
        // already inserts '\n' between lines; GetHeightUnicode counts them.
        return new Clay.Dimensions(w, h == 0 ? fontSize : h);
    }
}

// Texture cache + drawer for Bevy.UI Text render commands. Bakes each
// unique (text, font, color, width) into a Texture2D once, draws straight
// from cache afterwards.
internal static class UoFontRenderer
{
    private readonly struct CacheKey : IEquatable<CacheKey>
    {
        public readonly string Text;
        public readonly byte Font;
        public readonly int Width;
        public readonly bool IsHtml;

        public CacheKey(string text, byte font, int width, bool isHtml)
        {
            Text = text; Font = font; Width = width; IsHtml = isHtml;
        }

        public bool Equals(CacheKey o) => Text == o.Text && Font == o.Font && Width == o.Width && IsHtml == o.IsHtml;
        public override bool Equals(object? o) => o is CacheKey k && Equals(k);
        public override int GetHashCode() => HashCode.Combine(Text, Font, Width, IsHtml);
    }

    private sealed class Cached
    {
        public Texture2D? Texture;
        public int Width;
        public int Height;
    }

    private static readonly Dictionary<CacheKey, Cached> _cache = new();

    // Pre-bake a wrapped text bitmap for use as a UiImage. Spawn-time path
    // (server gumps) — caller knows the box size and wants the texture
    // ready before layout runs. Returns (texture, width, height); texture
    // may be null when fonts/device aren't loaded yet or text is empty.
    // htmlStartColor: default colour for untagged HTML segments. Mirrors
    // OOP RenderedText.HTMLColor → SetUseHTML(true, HTMLColor, ...). 0xFFFFFFFF
    // = white (FontsLoader default); the Help-menu path passes 0x010101FF
    // (black) so body text reads on the parchment bg instead of vanishing.
    public static (Texture2D? Texture, int Width, int Height) Bake(string text, byte font, ushort hue, int maxWidth, bool isHtml, uint htmlStartColor = 0xFFFFFFFF, bool htmlBgColored = false)
    {
        if (string.IsNullOrEmpty(text) || UoFontRuntime.Fonts == null || UoFontRuntime.Device == null)
            return (null, 0, 0);

        var key = new BakeKey(text, font, hue, maxWidth, isHtml, htmlStartColor);
        if (_bakeCache.TryGetValue(key, out var entry) && entry.Texture != null)
            return (entry.Texture, entry.Width, entry.Height);

        entry = GenerateColored(UoFontRuntime.Device, text, font, hue, maxWidth, isHtml, htmlStartColor, htmlBgColored);
        _bakeCache[key] = entry;
        return (entry.Texture, entry.Width, entry.Height);
    }

    private readonly struct BakeKey : IEquatable<BakeKey>
    {
        public readonly string Text;
        public readonly byte Font;
        public readonly ushort Hue;
        public readonly int Width;
        public readonly bool IsHtml;
        public readonly uint HtmlStartColor;

        public BakeKey(string text, byte font, ushort hue, int width, bool isHtml, uint htmlStartColor)
        {
            Text = text; Font = font; Hue = hue; Width = width; IsHtml = isHtml; HtmlStartColor = htmlStartColor;
        }

        public bool Equals(BakeKey o) => Text == o.Text && Font == o.Font && Hue == o.Hue && Width == o.Width && IsHtml == o.IsHtml && HtmlStartColor == o.HtmlStartColor;
        public override bool Equals(object? o) => o is BakeKey k && Equals(k);
        public override int GetHashCode() => HashCode.Combine(Text, Font, Hue, Width, IsHtml, HtmlStartColor);
    }

    private static readonly Dictionary<BakeKey, Cached> _bakeCache = new();

    // Remove <U></U> tags (case-insensitive). OOP's atlas renderer ignores the
    // per-char underline these set; stripping them matches it without disturbing
    // other tags (<basefont>, <a>, <i>, …) or text width.
    private static string StripUnderlineTags(string text)
    {
        if (string.IsNullOrEmpty(text) || text.IndexOf('<') < 0)
            return text;
        return text
            .Replace("<U>", string.Empty).Replace("</U>", string.Empty)
            .Replace("<u>", string.Empty).Replace("</u>", string.Empty);
    }

    private static Cached GenerateColored(GraphicsDevice device, string text, byte font, ushort hue, int maxWidth, bool isHtml, uint htmlStartColor, bool htmlBgColored)
    {
        var fonts = UoFontRuntime.Fonts!;
        // OOP HtmlControl.InternalBuild path:
        //   _gameText.HTMLColor = htmlStartColor (default body colour before tags;
        //                         0x010101FF black for bg/no-scroll, else white)
        //   _gameText.Hue       = 0xFFFF      (pass-through; FontsLoader picks
        //                                       datacolor = 0xFEFFFFFF white)
        // SetUseHTML(true, HTMLColor, HasBackgroundColor) seeds the start colour;
        // BASEFONT/FONT tags inside the text override per-segment colour during
        // GetInfoHTML. Untagged segments use htmlStartColor.
        //
        // Plain (non-HTML): OOP passes parts[3] + 1 as `Hue` to RenderedText →
        // FontsLoader.GenerateUnicode tints all pixels via Hues.GetPolygoneColor.
        ushort bakeColor = isHtml ? (ushort)0xFFFF : (ushort)(hue == 0 ? 0 : hue + 1);
        byte cell = 30;

        // OOP's atlas glyph renderer (RenderedText.DrawGlyphs) draws an underline
        // ONLY from the control-level FontStyle.Underline — it ignores the
        // per-char UOFONT_UNDERLINE flag that the <U> HTML tag sets. Our baked
        // path (FontsLoader.GenerateUnicode → DrawUnicode) DOES honour the per-char
        // flag, so <U> server text comes out underlined where OOP shows none.
        // Strip <U>/</U> before baking to match OOP (the tag carries no width, so
        // wrapping is unaffected; italic/bold/colour/link tags are kept).
        if (isHtml) text = StripUnderlineTags(text);

        if (isHtml) fonts.SetUseHTML(true, htmlStartColor, htmlBgColored);
        try
        {
            var info = fonts.GenerateUnicode(font, text, bakeColor, cell, maxWidth,
                TEXT_ALIGN_TYPE.TS_LEFT, 0, false, 0);
            if (info.Data == null || info.Width == 0 || info.Height == 0)
                return new Cached();

            var tex = new Texture2D(device, info.Width, info.Height);
            tex.SetData(info.Data);
            return new Cached { Texture = tex, Width = info.Width, Height = info.Height };
        }
        finally
        {
            if (isHtml) fonts.SetUseHTML(false);
        }
    }

    public static void Draw(
        UltimaBatcher2D batcher,
        string text,
        byte font,
        XnaColor tint,
        int x, int y,
        int maxWidth,
        float layerDepth)
    {
        if (string.IsNullOrEmpty(text) || UoFontRuntime.Fonts == null) return;
        var device = UoFontRuntime.Device ??= batcher.GraphicsDevice;

        // HTML detection: any text with '<' goes through FontsLoader's HTML
        // path (parses <BASEFONT COLOR=#XXX>, <BR>, etc and bakes coloured
        // pixels). Tint stays neutral so baked HTML colours survive.
        // Non-HTML text bakes as opaque white and gets tinted at draw via
        // SHADER_TEXT_HUE — one texture per (text, font, width).
        bool isHtml = text.IndexOf('<') >= 0;
        var key = new CacheKey(text, font, maxWidth, isHtml);
        if (!_cache.TryGetValue(key, out var entry) || entry.Texture == null)
        {
            entry = Generate(device, text, font, maxWidth, isHtml);
            _cache[key] = entry;
            if (entry.Texture == null) return;
        }

        var drawTint = isHtml ? XnaColor.White : tint;
        batcher.Draw(
            entry.Texture,
            new Vector2(x, y),
            new Rectangle(0, 0, entry.Width, entry.Height),
            drawTint,
            0f,
            Vector2.One,
            layerDepth);
    }

    private static Cached Generate(GraphicsDevice device, string text, byte font, int maxWidth, bool isHtml)
    {
        var fonts = UoFontRuntime.Fonts!;
        ushort bakeColor = isHtml ? (ushort)0 : (ushort)0xFFFF;

        if (isHtml) fonts.SetUseHTML(true);
        try
        {
            var info = fonts.GenerateUnicode(font, text, bakeColor, 0, maxWidth,
                TEXT_ALIGN_TYPE.TS_LEFT, 0, false, 0);
            if (info.Data == null || info.Width == 0 || info.Height == 0)
                return new Cached();

            var tex = new Texture2D(device, info.Width, info.Height);
            tex.SetData(info.Data);
            return new Cached { Texture = tex, Width = info.Width, Height = info.Height };
        }
        finally
        {
            if (isHtml) fonts.SetUseHTML(false);
        }
    }

    // Released on full UI rebuild (e.g. logout). Keeps memory bounded
    // across long sessions where many gumps come and go.
    public static void Clear()
    {
        foreach (var entry in _cache.Values)
            entry.Texture?.Dispose();
        _cache.Clear();
    }
}
