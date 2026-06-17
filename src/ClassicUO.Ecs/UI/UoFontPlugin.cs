// UO bitmap font rendering for Bevy.UI's Text elements. Mirrors the legacy
// client's atlas path (RenderedText.DrawGlyphs): glyphs are baked once into a
// shared FontGlyphAtlas (single 2048x2048 page) and drawn per-glyph from that
// atlas. There is NO per-string Texture2D — the layout (MultilinesFontInfo
// from FontsLoader.GetInfo*) is cached per (text, font, hue, width) tuple and
// walked each frame to place glyph quads. The measurer Clay queries during
// layout uses the same FontsLoader so MeasureText and the eventual draw agree.
//
// Color model: Bevy.UI Text components carry a ClayColor (RGB). UO fonts are
// tinted via a UO hue index (HuesLoader palette → ColorTable[cell]).
//   * unicode non-HTML: glyph baked white, tinted at draw via SHADER_RGB_TINT
//     (the ClayColor multiplies the white glyph).
//   * ASCII gump text: the hue is baked per pixel into the glyph (partial-hue
//     for fonts 1-4/9, full for 5/8) and drawn neutral — pixel-exact vs OOP.
//   * HTML: per-char colour comes from the FontsLoader parse (BASEFONT/FONT
//     tags); untagged segments use the seeded html start colour.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyEcs;
using TinyEcs.Bevy;
using XnaColor = Microsoft.Xna.Framework.Color;
using ClayColor = Clay.Color;

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
            UoFontRuntime.Atlas = new FontGlyphAtlas(fileManager.Value.Fonts, batcher.Value.GraphicsDevice);
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
    // Shared glyph atlas — single 2048x2048 page, glyphs baked on first use
    // and keyed by (font, char, style[, color]). Mirrors legacy
    // Client.Game.UO.FontGlyphAtlas.
    public static FontGlyphAtlas? Atlas;

    // Default font index for ECS text. Matches OOP's gump rendering which
    // uses font 1 throughout (Generic gump font). FontId on Bevy.UI Text
    // components < 20 maps directly; >= 20 falls back here.
    public const byte DefaultFont = 1;

    // Bit set on TextFont.FontId to select the UO ASCII font set
    // (FontsLoader.GenerateASCII) instead of the default unicode set. The
    // login chest / server / character gumps render their labels with the
    // ASCII fonts (font 2/5/9) exactly like the OOP client, so a Bevy.UI Text
    // that wants that look sets `FontId = font | AsciiFlag`.
    public const ushort AsciiFlag = 0x80;

    public static (byte Font, bool Ascii) Resolve(ushort fontId)
    {
        bool ascii = (fontId & AsciiFlag) != 0;
        var idx = (ushort)(fontId & 0x7F);
        return (idx < 20 ? (byte)idx : DefaultFont, ascii);
    }

    // For ASCII gump text the TextColor does NOT carry an RGB tint — it
    // carries the UO hue, packed R=low byte / G=high byte. The renderer bakes
    // the glyph already coloured by that hue (FontsLoader applies it per pixel,
    // partial-hue and all) and draws it neutral, so it matches the OOP client
    // exactly instead of a flat single-colour approximation. hue 0 → white.
    public static ClayColor AsciiHue(ushort hue)
        => new ClayColor(hue & 0xFF, (hue >> 8) & 0xFF, 0, 255);
}

internal sealed class UoFontTextMeasurer : Clay.ITextMeasurer
{
    // maxWidth > 0 wraps via MeasureFont (widest wrapped line + total height) and
    // caches the layout so the matching UoFontRenderer.Draw at the same width hits
    // (Draw re-wraps identically). maxWidth <= 0 measures the run as-is — wrap
    // already handled upstream by ServerGumpPlugin.WrapText inserts '\n' between
    // lines, which GetHeight* counts.
    public Clay.Dimensions MeasureText(ReadOnlySpan<char> text, ushort fontId, ushort fontSize, ushort letterSpacing, float maxWidth = 0)
    {
        var fonts = UoFontRuntime.Fonts;
        if (fonts == null || text.IsEmpty)
            return new Clay.Dimensions(0, fontSize);

        if (maxWidth > 0)
        {
            var (ww, hh) = UoFontRenderer.MeasureFont(text.ToString(), fontId, (int)maxWidth);
            return new Clay.Dimensions(ww, hh == 0 ? fontSize : hh);
        }

        var (font, ascii) = UoFontRuntime.Resolve(fontId);
        var str = text.ToString();
        int w, h;
        if (ascii)
        {
            w = fonts.GetWidthASCII(font, str);
            h = fonts.GetHeightASCII(font, str, w, TEXT_ALIGN_TYPE.TS_LEFT, 0);
        }
        else
        {
            w = fonts.GetWidthUnicode(font, str);
            h = fonts.GetHeightUnicode(font, str, w, TEXT_ALIGN_TYPE.TS_LEFT, 0);
        }
        return new Clay.Dimensions(w, h == 0 ? fontSize : h);
    }
}

// Glyph-atlas drawer for Bevy.UI Text render commands. Caches the parsed
// layout (MultilinesFontInfo) per unique (text, font, hue, width, html) tuple
// and walks it each frame, drawing each glyph from the shared FontGlyphAtlas —
// exactly like legacy RenderedText.DrawGlyphs.
internal static class UoFontRenderer
{
    // Mirror FontsLoader's private UOFONT_* flag constants (MultilinesFontData.Flags).
    private const ushort UOFONT_SOLID = 0x0001;
    private const ushort UOFONT_ITALIC = 0x0002;
    private const ushort UOFONT_BLACK_BORDER = 0x0008;

    private readonly struct LayoutKey : IEquatable<LayoutKey>
    {
        public readonly string Text;
        public readonly byte Font;
        public readonly bool Ascii;
        public readonly bool IsHtml;
        public readonly uint HtmlStartColor;
        public readonly bool HtmlBg;
        public readonly int Width;
        public readonly TEXT_ALIGN_TYPE Align;

        public LayoutKey(string text, byte font, bool ascii, bool isHtml, uint htmlStartColor, bool htmlBg, int width, TEXT_ALIGN_TYPE align)
        {
            Text = text; Font = font; Ascii = ascii; IsHtml = isHtml;
            HtmlStartColor = htmlStartColor; HtmlBg = htmlBg; Width = width; Align = align;
        }

        public bool Equals(LayoutKey o) =>
            Text == o.Text && Font == o.Font && Ascii == o.Ascii && IsHtml == o.IsHtml
            && HtmlStartColor == o.HtmlStartColor && HtmlBg == o.HtmlBg && Width == o.Width && Align == o.Align;
        public override bool Equals(object? o) => o is LayoutKey k && Equals(k);
        public override int GetHashCode() => HashCode.Combine(Text, Font, Ascii, IsHtml, HtmlStartColor, HtmlBg, Width, Align);
    }

    private sealed class Layout
    {
        public MultilinesFontInfo? Info;
        public int Width;
        public int Height;
        // Wrap width the layout was built with — TS_CENTER/TS_RIGHT align each
        // line within this (legacy FontsLoader's `width`), NOT the content Width.
        public int MaxWidth;
        // Lazy 1-bit ink mask for pixel-perfect hit testing (PixelHit). Built
        // on first query by re-rendering each glyph's pixels; border pixels are
        // included when the mask was built with border=true so the hit area
        // matches the drawn outline.
        public bool[]? HitMask;
        public bool HitMaskBorder;
    }

    private static readonly Dictionary<LayoutKey, Layout> _layouts = new();

    // Shared by DrawGlyphs + BuildHitMask — the hit mask must place glyphs
    // exactly where the draw does, so the per-line walk math lives here once.
    internal static int LineAlignOffset(MultilinesFontInfo ptr, int textWidth, bool isUnicode)
    {
        int w = ptr.Align switch
        {
            TEXT_ALIGN_TYPE.TS_CENTER => isUnicode ? (textWidth - 8) / 2 - ptr.Width / 2 : (textWidth - ptr.Width) >> 1,
            TEXT_ALIGN_TYPE.TS_RIGHT => textWidth - 10 - ptr.Width,
            _ => 0,
        };
        return w < 0 ? 0 : w;
    }

    // Match GeneratePixelsASCII: ASCII font 6 reduces line spacing by 7px.
    internal static int LinePitch(MultilinesFontInfo ptr, byte font, bool isUnicode)
        => ptr.MaxHeight - (!isUnicode && font == 6 ? 7 : 0);

    internal static (byte Font, bool Border, bool Solid, bool Italic) HtmlCharStyle(in MultilinesFontData d)
        => (d.Font, (d.Flags & UOFONT_BLACK_BORDER) != 0, (d.Flags & UOFONT_SOLID) != 0, (d.Flags & UOFONT_ITALIC) != 0);

    // Remove <U></U> tags (case-insensitive). OOP's atlas renderer ignores the
    // per-char underline these set; stripping them matches it without disturbing
    // other tags (<basefont>, <a>, <i>, …) or text width.
    internal static string StripUnderlineTags(string text)
    {
        if (string.IsNullOrEmpty(text) || text.IndexOf('<') < 0)
            return text;
        return text
            .Replace("<U>", string.Empty).Replace("</U>", string.Empty)
            .Replace("<u>", string.Empty).Replace("</u>", string.Empty);
    }

    // Build (or fetch cached) layout for a text run. For HTML the parse is run
    // under SetUseHTML so per-char Font/Color/Flags in the MultilinesFontData
    // already carry the resolved per-segment colour (untagged segments use
    // htmlStartColor). The cached info is read-only afterwards.
    private static Layout GetLayout(string text, byte font, bool ascii, bool isHtml, uint htmlStartColor, bool htmlBg, int maxWidth, TEXT_ALIGN_TYPE align = TEXT_ALIGN_TYPE.TS_LEFT)
    {
        var key = new LayoutKey(text, font, ascii, isHtml, htmlStartColor, htmlBg, maxWidth, align);
        if (_layouts.TryGetValue(key, out var cached))
            return cached;

        var fonts = UoFontRuntime.Fonts!;
        // OOP's atlas glyph renderer draws an underline ONLY from control-level
        // FontStyle.Underline — it ignores the per-char UOFONT_UNDERLINE flag the
        // <U> HTML tag sets. Our atlas path honours that per-char flag, so strip
        // <U>/</U> before parsing to match OOP (the tag carries no width).
        string parseText = isHtml ? StripUnderlineTags(text) : text;

        if (isHtml) fonts.SetUseHTML(true, htmlStartColor, htmlBg);
        try
        {
            MultilinesFontInfo? info = ascii
                ? fonts.GetInfoASCII(font, parseText, parseText.Length, align, 0, maxWidth, countret: true, countspaces: true)
                : fonts.GetInfoUnicode(font, parseText, parseText.Length, align, 0, maxWidth, countret: true, countspaces: true);

            int w = 0, h = 0;
            for (var ptr = info; ptr != null; ptr = ptr.Next)
            {
                if (ptr.Width > w) w = ptr.Width;
                h += LinePitch(ptr, font, isUnicode: !ascii);
            }

            var layout = new Layout { Info = info, Width = w, Height = h, MaxWidth = maxWidth };
            _layouts[key] = layout;
            return layout;
        }
        finally
        {
            if (isHtml) fonts.SetUseHTML(false);
        }
    }

    // Per-glyph draw of a parsed layout from the shared atlas. Mirrors legacy
    // RenderedText.DrawGlyphs (no clip/scale/underline — Clay scissor commands
    // handle overflow and ECS Text carries no FontStyle).
    private static void DrawGlyphs(
        UltimaBatcher2D batcher,
        Layout layout,
        byte font,
        bool isUnicode,
        bool isHtml,
        uint asciiHue,
        XnaColor unicodeTint,
        int destX,
        int destY,
        float depth,
        bool border = false,
        uint unicodeBaked = 0,
        float alpha = 1f)
    {
        var atlas = UoFontRuntime.Atlas;
        if (atlas == null || layout.Info == null)
            return;

        // ASCII bakes the UO hue per pixel (the partial-hue gradient can't be
        // reproduced by a flat tint); HTML bakes the per-segment ARGB; overhead
        // unicode bakes the GetPolygoneColor ARGB (unicodeBaked) so the legacy
        // black-border-on-near-black skip applies per glyph. Every glyph is then
        // drawn through SHADER_RGB_TINT — the ECS UI render target doesn't honour
        // the neutral/mode-NONE Vector3 path (it renders white), so even
        // baked-colour glyphs go through RGB_TINT (× white = unchanged).
        uint baseColor = isHtml ? 0u : (isUnicode ? unicodeBaked : asciiHue);

        int lineOffsY = 0;
        var ptr = layout.Info;
        // Center/right alignment is relative to the wrap width (matches legacy
        // FontsLoader), so the block centers in the box, not in its own content.
        int textWidth = layout.MaxWidth > 0 ? layout.MaxWidth : layout.Width;

        while (ptr != null)
        {
            int w = LineAlignOffset(ptr, textWidth, isUnicode);

            int dataLen = ptr.Data.Count;
            var dataSpan = CollectionsMarshal.AsSpan(ptr.Data);

            for (int i = 0; i < dataLen; i++)
            {
                ref MultilinesFontData d = ref dataSpan[i];
                char si = d.Item;
                if (si == '\n' || si == '\r')
                    continue;

                byte charFont = font;
                bool charBorder = border, charSolid = false, charItalic = false;
                uint charColor = baseColor;

                if (isHtml)
                {
                    (charFont, charBorder, charSolid, charItalic) = HtmlCharStyle(in d);
                    if (d.Color != 0xFFFFFFFF)
                        charColor = HuesHelper.RgbaToArgb(d.Color);
                }

                // Match GeneratePixelsUnicode: a black border on near-black text
                // is invisible and produces artifacts — skip it.
                if (charBorder && isUnicode && charColor != 0)
                {
                    bool isBlackPixel =
                        ((charColor >> 0) & 0xFF) <= 8
                        && ((charColor >> 8) & 0xFF) <= 8
                        && ((charColor >> 16) & 0xFF) <= 8;
                    if (isBlackPixel)
                        charBorder = false;
                }

                // Legacy DrawGlyphs bakes the colour into the glyph (charColor 0
                // → white glyph). ASCII bakes via the hue palette; unicode bakes
                // the literal ARGB.
                GlyphAtlasEntry entry = charColor != 0
                    ? atlas.GetColoredEntry(charFont, si, isUnicode, charBorder, charSolid, charItalic, charColor)
                    : atlas.GetEntry(charFont, si, isUnicode, charBorder, charSolid, charItalic);

                if (si == ' ')
                {
                    w += entry.AdvanceWidth;
                    continue;
                }

                if (entry.IsValid)
                {
                    int dx = destX + w + entry.BearingX;
                    int dy = destY + lineOffsY + entry.BearingY;

                    // The ECS UI render target only honours SHADER_RGB_TINT (the
                    // neutral/mode-NONE Vector3 path renders white here). A glyph
                    // with the colour already baked draws under a WHITE tint
                    // (colour × white = colour); a white glyph (charColor 0) draws
                    // under the caller's tint (unicode hue/ClayColor; ASCII white).
                    XnaColor drawTint = charColor != 0
                        ? XnaColor.White
                        : (isUnicode ? unicodeTint : XnaColor.White);

                    // Premultiplied fade (legacy RenderedText.Draw alpha):
                    // scale all four channels so the glyph blends half-out.
                    if (alpha < 1f)
                        drawTint *= alpha;

                    batcher.Draw(
                        entry.Texture,
                        new Vector2(dx, dy),
                        entry.UV,
                        drawTint,
                        0f,
                        Vector2.One,
                        depth);
                }

                w += entry.AdvanceWidth;
            }

            lineOffsY += LinePitch(ptr, font, isUnicode);
            ptr = ptr.Next;
        }
    }

    // Inline label draw path (GuiRenderingPlugin.DrawText). Bevy.UI Text encodes
    // the UO font in TextFont.FontId; the ClayColor is the unicode tint or, for
    // ASCII, the packed hue (see UoFontRuntime.AsciiHue).
    public static void Draw(
        UltimaBatcher2D batcher,
        string text,
        ushort fontId,
        XnaColor tint,
        int x, int y,
        int maxWidth,
        float layerDepth,
        bool allowHtml = true,
        bool border = false,
        uint unicodeBaked = 0,
        float alpha = 1f)
    {
        if (string.IsNullOrEmpty(text) || UoFontRuntime.Fonts == null || UoFontRuntime.Atlas == null)
            return;

        var (font, ascii) = UoFontRuntime.Resolve(fontId);

        // ASCII gump text carries its UO hue in the tint's R/G bytes.
        ushort asciiHue = ascii ? (ushort)(tint.R | (tint.G << 8)) : (ushort)0;

        // Unicode text with '<' goes through the HTML parse (BASEFONT/BR/etc).
        // Callers rendering plain text (e.g. speech overhead, where '<' is a
        // literal char) pass allowHtml: false to keep '<' visible.
        bool isHtml = allowHtml && !ascii && text.IndexOf('<') >= 0;

        var layout = GetLayout(text, font, ascii, isHtml, 0xFFFFFFFF, htmlBg: false, maxWidth);
        DrawGlyphs(batcher, layout, font, isUnicode: !ascii, isHtml, asciiHue, tint, x, y, layerDepth, border, unicodeBaked, alpha);
    }

    // Wrapped HTML block draw path (UOCustomKind.WrappedText). The node was
    // sized at spawn via Measure with the same args, so the layout here is the
    // cached one. htmlStartColor seeds the body colour for untagged segments
    // (Help menu = black on parchment).
    public static void DrawWrapped(
        UltimaBatcher2D batcher,
        string text,
        byte font,
        ushort hue,
        int maxWidth,
        bool isHtml,
        uint htmlStartColor,
        bool htmlBg,
        int x, int y,
        float layerDepth,
        TEXT_ALIGN_TYPE align = TEXT_ALIGN_TYPE.TS_LEFT,
        bool ascii = false)
    {
        if (string.IsNullOrEmpty(text) || UoFontRuntime.Fonts == null || UoFontRuntime.Atlas == null)
            return;

        // ASCII gump text bakes the UO hue per pixel (partial-hue and all) into
        // the glyph and draws it neutral — pixel-exact vs OOP RenderedText. The
        // hue rides in asciiHue; the draw tint is white (baked colour unchanged).
        if (ascii)
        {
            var asciiLayout = GetLayout(text, font, ascii: true, isHtml: false, 0xFFFFFFFF, htmlBg: false, maxWidth, align);
            DrawGlyphs(batcher, asciiLayout, font, isUnicode: false, isHtml: false, asciiHue: hue, XnaColor.White, x, y, layerDepth);
            return;
        }

        // HTML carries its own per-segment colours (drawn neutral). Plain
        // wrapped text (croppedtext/text) is a white glyph tinted by the UO
        // hue — same flat-tint model as inline unicode labels. hue 0 → white.
        var tint = (!isHtml && hue != 0) ? HueToTint(hue) : XnaColor.White;
        var layout = GetLayout(text, font, ascii: false, isHtml, htmlStartColor, htmlBg, maxWidth, align);
        DrawGlyphs(batcher, layout, font, isUnicode: true, isHtml, asciiHue: 0, tint, x, y, layerDepth);
    }

    // Legacy RenderedText unicode baked colour (MessageManager overhead path):
    // GetPolygoneColor(cell 30, hue) (its own hue-1) → 0x00BBGGRR, then
    // (<<8 | 0xFF) + RgbaToArgb → 0xFFBBGGRR, a packed XNA colour the atlas bakes
    // per glyph. hue 0 → the sentinel near-black (0xFF010101), matching legacy —
    // NOT white. The black border is added separately at the draw call.
    private static uint UnicodeBakedColor(ushort hue)
    {
        var hues = UoFontRuntime.Hues;
        if (hues == null) return 0xFFFFFFFF;
        uint poly = hues.GetPolygoneColor(30, hue);
        return HuesHelper.RgbaToArgb((poly << 8) | 0xFF);
    }

    // UO hue index → flat tint for plain (non-HTML) text. cell 30 is the bright
    // end of the hue gradient (OOP RenderedText default); the atlas glyph keeps
    // its own per-pixel alpha so a uniform tint reads correctly on any bg.
    private static XnaColor HueToTint(ushort hue)
    {
        var hues = UoFontRuntime.Hues;
        if (hues == null) return XnaColor.White;
        uint packed = hues.GetPolygoneColor(30, (ushort)(hue + 1));
        if (packed == 0 || packed == 0xFF010101) return XnaColor.Black;
        return new XnaColor((byte)(packed & 0xFF), (byte)((packed >> 8) & 0xFF), (byte)((packed >> 16) & 0xFF), (byte)255);
    }

    // Measure a wrapped block (no draw). Builds + caches the layout so the
    // subsequent DrawWrapped is a cache hit. Returns the content (width, height)
    // used to size the scrollable inner node.
    public static (int Width, int Height) Measure(string text, byte font, int maxWidth, bool isHtml, uint htmlStartColor, bool htmlBg, TEXT_ALIGN_TYPE align = TEXT_ALIGN_TYPE.TS_LEFT, bool ascii = false)
    {
        if (string.IsNullOrEmpty(text) || UoFontRuntime.Fonts == null)
            return (0, 0);

        var layout = GetLayout(text, font, ascii, isHtml, htmlStartColor, htmlBg, maxWidth, align);
        return (layout.Width, layout.Height);
    }

    // Hue-driven draw for code paths that only have a UO hue (overhead text).
    // Resolves ascii/unicode from the fontId and builds the right tint: ascii
    // carries the hue in the tint's R/G bytes (Draw's asciiHue convention),
    // unicode tints the white glyph by the hue's bright-end colour.
    public static void Draw(
        UltimaBatcher2D batcher,
        string text,
        ushort fontId,
        ushort hue,
        int x, int y,
        int maxWidth,
        float layerDepth,
        bool allowHtml = true,
        float alpha = 1f)
    {
        var (_, ascii) = UoFontRuntime.Resolve(fontId);
        // Overhead/world text mirrors legacy MessageManager.CreateMessage:
        // FontStyle.BlackBorder outline + colour baked into the glyph (ASCII per-
        // pixel hue carried in the tint R/G bytes; unicode the GetPolygoneColor
        // ARGB). Baked-colour glyphs draw under a white tint (SHADER_RGB_TINT
        // leaves them unchanged); the border is baked black per glyph.
        XnaColor tint = ascii ? new XnaColor(hue & 0xFF, (hue >> 8) & 0xFF, 0, 255) : XnaColor.White;
        uint unicodeBaked = ascii ? 0u : UnicodeBakedColor(hue);
        Draw(batcher, text, fontId, tint, x, y, maxWidth, layerDepth, allowHtml, border: true, unicodeBaked: unicodeBaked, alpha: alpha);
    }

    // Measure a run honouring the fontId's ascii/unicode flag (the other Measure
    // is unicode-only). Builds + caches the layout the matching Draw will reuse.
    public static (int Width, int Height) MeasureFont(string text, ushort fontId, int maxWidth, bool allowHtml = true)
    {
        if (string.IsNullOrEmpty(text) || UoFontRuntime.Fonts == null)
            return (0, 0);

        var (font, ascii) = UoFontRuntime.Resolve(fontId);
        bool isHtml = allowHtml && !ascii && text.IndexOf('<') >= 0;
        var layout = GetLayout(text, font, ascii, isHtml, 0xFFFFFFFF, htmlBg: false, maxWidth);
        return (layout.Width, layout.Height);
    }

    // Pixel-perfect hit test against a rendered run — the ECS analogue of
    // legacy RenderedText.PixelCheck. (x, y) are text-local px from the run's
    // top-left draw position. Walks the SAME cached layout the matching Draw
    // uses, so glyph placement is identical; the per-layout ink mask is built
    // lazily by re-rendering each glyph's pixels once. `border` must match the
    // draw call (overhead text draws with the black border, which widens the
    // ink by 1px all around — legacy PixelCheck includes it).
    public static bool PixelHit(string text, ushort fontId, int maxWidth, int x, int y, bool allowHtml = true, bool border = false)
    {
        if (string.IsNullOrEmpty(text) || UoFontRuntime.Fonts == null)
            return false;

        var (font, ascii) = UoFontRuntime.Resolve(fontId);
        bool isHtml = allowHtml && !ascii && text.IndexOf('<') >= 0;
        var layout = GetLayout(text, font, ascii, isHtml, 0xFFFFFFFF, htmlBg: false, maxWidth);
        if (x < 0 || y < 0 || x >= layout.Width || y >= layout.Height)
            return false;

        if (layout.HitMask == null || layout.HitMaskBorder != border)
        {
            layout.HitMask = BuildHitMask(layout, font, isUnicode: !ascii, isHtml, border);
            layout.HitMaskBorder = border;
        }

        return layout.HitMask[y * layout.Width + x];
    }

    // Mirrors DrawGlyphs' placement walk exactly (align offset, bearings, line
    // pitch, html per-char font/flags), ORing each glyph's non-transparent
    // pixels into a Width×Height mask. The near-black border skip DrawGlyphs
    // applies per baked colour is ignored here — the layout cache isn't keyed
    // on hue, and the 1px divergence only exists for near-black text.
    private static bool[] BuildHitMask(Layout layout, byte font, bool isUnicode, bool isHtml, bool border)
    {
        var mask = new bool[layout.Width * layout.Height];
        var fonts = UoFontRuntime.Fonts!;
        int textWidth = layout.Width;
        int lineOffsY = 0;

        for (var ptr = layout.Info; ptr != null; ptr = ptr.Next)
        {
            int w = LineAlignOffset(ptr, textWidth, isUnicode);

            var dataSpan = CollectionsMarshal.AsSpan(ptr.Data);
            for (int i = 0; i < dataSpan.Length; i++)
            {
                ref MultilinesFontData d = ref dataSpan[i];
                char si = d.Item;
                if (si == '\n' || si == '\r')
                    continue;

                byte charFont = font;
                bool charBorder = border, charSolid = false, charItalic = false;
                if (isHtml)
                    (charFont, charBorder, charSolid, charItalic) = HtmlCharStyle(in d);

                var gi = isUnicode
                    ? fonts.RenderSingleGlyphUnicode(charFont, si, charBorder, charSolid, charItalic)
                    : fonts.RenderSingleGlyphASCII(charFont, si);

                if (si != ' ' && gi.Data != null && gi.Width > 0 && gi.Height > 0)
                {
                    int dx = w + gi.BearingX;
                    int dy = lineOffsY + gi.BearingY;
                    for (int gy = 0; gy < gi.Height; gy++)
                    {
                        int my = dy + gy;
                        if (my < 0 || my >= layout.Height) continue;
                        for (int gx = 0; gx < gi.Width; gx++)
                        {
                            int mx = dx + gx;
                            if (mx < 0 || mx >= textWidth) continue;
                            if (gi.Data[gy * gi.Width + gx] != 0)
                                mask[my * textWidth + mx] = true;
                        }
                    }
                }

                w += gi.AdvanceWidth;
            }

            lineOffsY += LinePitch(ptr, font, isUnicode);
        }

        return mask;
    }

    // Wrap a unicode run to maxWidth and return one entry per visual line:
    // (Start = buffer index the line begins at, Count = chars on the line incl a
    // trailing space/newline, Width px, Height px). Goes through the SAME GetLayout
    // the renderer uses (incl. HTML mode), so a caret/selection mapped through
    // these lines — and the per-line height the caret stacks by — match the drawn
    // glyphs exactly. The multiline editor field renders HTML (for its near-black
    // text colour), and HTML mode changes per-line MaxHeight; measuring plain here
    // made the caret drift up a few px per line. Used by TextEditPlugin.
    public static List<(int Start, int Count, int Width, int Height)> WrapLines(
        string text, ushort fontId, int maxWidth, bool isHtml = false, uint htmlStartColor = 0xFFFFFFFF, bool htmlBg = false)
    {
        var lines = new List<(int, int, int, int)>();
        if (UoFontRuntime.Fonts == null)
            return lines;

        var (font, _) = UoFontRuntime.Resolve(fontId);
        text ??= string.Empty;
        var layout = GetLayout(text, font, ascii: false, isHtml, htmlStartColor, htmlBg, maxWidth);
        int start = 0;
        for (var ptr = layout.Info; ptr != null; ptr = ptr.Next)
        {
            lines.Add((start, ptr.CharCount, ptr.Width, ptr.MaxHeight));
            start += ptr.CharCount;
        }
        return lines;
    }

    // Per-visual-line highlight rects for a selection [selStart,selEnd) over a
    // wrapped run, in text-local px (X,Y from the text's top-left). Mirrors the
    // caret math: PLAIN wrap drives the char-index→line membership + x widths
    // (raw-buffer indices), the HTML wrap (what's drawn) drives the Y pitch + row
    // height, so the rects sit exactly under the glyphs. Used to paint multi-row
    // text selection, which a single overlay rect can't cover.
    public static List<(float X, float Y, float W, float H)> SelectionRects(
        string text, ushort fontId, int maxWidth, bool isHtml, uint htmlStartColor, bool htmlBg, int selStart, int selEnd)
    {
        var rects = new List<(float, float, float, float)>();
        if (selEnd <= selStart || string.IsNullOrEmpty(text) || UoFontRuntime.Fonts == null)
            return rects;

        // selStart/selEnd come from the live editor while `text` is the field's
        // rendered string, which lags one frame (WriteBack runs after the system
        // that stamps the range) — clamp to text length so a just-deleted tail
        // can't drive Substring past the end.
        int len = text.Length;
        int ss = Math.Clamp(selStart, 0, len);
        int se = Math.Clamp(selEnd, 0, len);
        if (se <= ss) return rects;

        var lines = WrapLines(text, fontId, maxWidth);
        var rlines = isHtml ? WrapLines(text, fontId, maxWidth, isHtml, htmlStartColor, htmlBg) : lines;
        int fontH = MeasureFont("Wg", fontId, int.MaxValue, allowHtml: false).Height;
        float lh = fontH > 0 ? fontH : 20;
        float y = 0;
        for (int i = 0; i < lines.Count; i++)
        {
            var ln = lines[i];
            int ls = Math.Clamp(ln.Start, 0, len);
            int le = Math.Clamp(ln.Start + ln.Count, ls, len);
            int lineH = i < rlines.Count ? rlines[i].Height : ln.Height;
            float rh = lineH > 0 ? lineH : lh;
            int a = Math.Clamp(Math.Max(ss, ls), ls, le);
            int b = Math.Clamp(Math.Min(se, le), ls, le);
            if (b > a)
            {
                float x0 = a > ls ? MeasureFont(text.Substring(ls, a - ls), fontId, int.MaxValue, allowHtml: false).Width : 0;
                float x1 = MeasureFont(text.Substring(ls, b - ls), fontId, int.MaxValue, allowHtml: false).Width;
                rects.Add((x0, y, MathF.Max(1, x1 - x0), rh));
            }
            y += rh;
        }
        return rects;
    }

    private static readonly Dictionary<ushort, (float Top, float Height, float Cell)> _caretMetrics = new();

    // Glyph-ink geometry for a font: the visible INK extent within a line, plus
    // the full line cell. The cell (what MeasureFont returns) carries top leading
    // and — for the ASCII fonts — a tall transparent-padded bitmap, so the ink
    // sits asymmetrically low in the cell; a caret sized to the cell floats above
    // the text, and text drawn at the cell top floats low. Scan the actual glyph
    // pixels of a cap + a descender for the inked rows (BearingY + first/last lit
    // row): Top = where the cap ink begins, Height = cap-ink-top to descender-ink-
    // bottom, Cell = the full line cell (MeasureFont). Callers centre the ink
    // within the cell ((Cell-Height)/2) to cancel the asymmetry. Cached per font
    // (RenderSingleGlyph* allocates, so this runs once).
    public static (float Top, float Height, float Cell) CaretMetrics(ushort fontId)
    {
        if (_caretMetrics.TryGetValue(fontId, out var m))
            return m;

        var fonts = UoFontRuntime.Fonts;
        var (font, ascii) = UoFontRuntime.Resolve(fontId);
        int top = int.MaxValue, bottom = int.MinValue;
        if (fonts != null)
        {
            foreach (char c in "WQgy")
            {
                var gi = ascii
                    ? fonts.RenderSingleGlyphASCII(font, c)
                    : fonts.RenderSingleGlyphUnicode(font, c, hasBorder: false, isSolid: false);
                if (gi.Data == null || gi.Width <= 0 || gi.Height <= 0) continue;
                int inkTop = -1, inkBot = -1;
                for (int y = 0; y < gi.Height; y++)
                {
                    bool lit = false;
                    for (int x = 0; x < gi.Width; x++)
                        if (gi.Data[y * gi.Width + x] != 0) { lit = true; break; }
                    if (!lit) continue;
                    if (inkTop < 0) inkTop = y;
                    inkBot = y;
                }
                if (inkTop < 0) continue;
                int gt = gi.BearingY + inkTop;
                int gb = gi.BearingY + inkBot + 1;
                if (gt < top) top = gt;
                if (gb > bottom) bottom = gb;
            }
        }
        float cell = MeasureFont("Wg", fontId, int.MaxValue, allowHtml: false).Height;
        if (top == int.MaxValue)
        {
            top = 0;
            bottom = (int)cell;
        }
        if (cell <= 0) cell = bottom;
        m = (top, Math.Max(1, bottom - top), cell);
        _caretMetrics[fontId] = m;
        return m;
    }

    // Released on full UI rebuild (e.g. logout). The atlas itself persists
    // (glyphs are reusable); only the per-string layout cache is cleared.
    public static void Clear() { _layouts.Clear(); _caretMetrics.Clear(); }
}
