// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StbTextEditSharp;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ClassicUO.Game
{
    [Flags]
    internal enum FontStyle : ushort
    {
        None = 0x0000,
        Solid = 0x0001,
        Italic = 0x0002,
        Indention = 0x0004,
        BlackBorder = 0x0008,
        Underline = 0x0010,
        Fixed = 0x0020,
        Cropped = 0x0040,
        BQ = 0x0080,
        ExtraHeight = 0x0100,
        CropTexture = 0x0200
    }
    internal sealed class RenderedText
    {
        private static readonly QueuedPool<RenderedText> _pool = new QueuedPool<RenderedText>(
            3000,
            r =>
            {
                r.IsDestroyed = false;
                r.Links.Clear();
            }
        );

        private static PixelPicker _picker = new PixelPicker();
        private byte _font;

        private MultilinesFontInfo _info;
        private string _text;

        public bool IsUnicode { get; set; }

        public byte Font
        {
            get => _font;
            set
            {
                if (value == 0xFF)
                {
                    value = (byte)(Client.Game.UO.Version >= ClientVersion.CV_305D ? 1 : 0);
                }

                _font = value;
            }
        }

        public TEXT_ALIGN_TYPE Align { get; set; }

        public int MaxWidth { get; set; }

        public int MaxHeight { get; set; } = 0;

        public FontStyle FontStyle { get; set; }

        public byte Cell { get; set; }

        public bool IsHTML { get; set; }

        public bool RecalculateWidthByInfo { get; set; }

        public List<WebLinkRect> Links { get; set; } = new List<WebLinkRect>();

        public ushort Hue { get; set; }

        public uint HTMLColor { get; set; } = 0xFFFFFFFF;

        public bool HasBackgroundColor { get; set; }

        /// <summary>
        /// Whether this RenderedText has valid content to display.
        /// Use this instead of checking Texture == null for atlas-based text.
        /// </summary>
        public bool HasContent => !string.IsNullOrEmpty(Text) && !IsDestroyed && Width > 0 && Height > 0;

        /// <summary>
        /// Stored HTML background color (ARGB), 0 if none.
        /// </summary>
        private uint _htmlBgColor;

        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;

                    if (string.IsNullOrEmpty(value))
                    {
                        Width = 0;
                        Height = 0;

                        if (IsHTML)
                        {
                            Client.Game.UO.FileManager.Fonts.SetUseHTML(false);
                        }

                        Links.Clear();
                        Texture?.Dispose();
                        Texture = null;
                        _info = null;
                        _htmlBgColor = 0;
                    }
                    else
                    {
                        CreateTexture();

                        // Compute layout info for atlas-based drawing.
                        // For HTML text, enable HTML parsing so per-char colors/fonts/flags are populated.
                        if (IsHTML)
                        {
                            Client.Game.UO.FileManager.Fonts.SetUseHTML(true, HTMLColor, HasBackgroundColor);
                        }

                        // The atlas-based renderer (see DrawGlyphs) iterates _info.Data to
                        // place per-glyph quads — so _info must carry exactly the chars
                        // that should appear on screen, including the "..." ellipsis for
                        // cropped strings. Pre-atlas (<= commit 119108d3), cropping with
                        // ellipsis happened inside GenerateUnicode/GeneratePixelsUnicode
                        // when producing the per-string texture; _info was not on the
                        // draw path. Now that _info IS on the draw path, we need to crop
                        // the text here via GetTextByWidth{Unicode,ASCII} and hand the
                        // already-truncated result (e.g. "LongTermMu...") to GetInfo*.
                        // Otherwise long single-word property names would either go blank
                        // or overflow without an ellipsis marker.
                        string layoutText = Text;
                        int layoutWidth = MaxWidth > 0 ? MaxWidth : Width;

                        if (MaxWidth > 0 && (FontStyle & FontStyle.Cropped) != 0)
                        {
                            var fonts = Client.Game.UO.FileManager.Fonts;
                            int realWidth = IsUnicode
                                ? fonts.GetWidthUnicode(Font, Text)
                                : fonts.GetWidthASCII(Font, Text);

                            if (realWidth > MaxWidth)
                            {
                                layoutText = IsUnicode
                                    ? fonts.GetTextByWidthUnicode(Font, Text.AsSpan(), MaxWidth, isCropped: true, Align, (ushort)FontStyle)
                                    : fonts.GetTextByWidthASCII(Font, Text, MaxWidth, isCropped: true, Align, (ushort)FontStyle);
                            }
                        }

                        // countret/countspaces MUST be true so that CharCount in each
                        // MultilinesFontInfo node includes '\n' and ' '. Stb's hit-testing
                        // (LocateCoord) walks rows by `i += r.num_chars` and bails out by
                        // returning text length when it sees num_chars == 0. With
                        // countret=false an empty line ("\n") produces CharCount=0, which
                        // forces every click on an empty editable area (book body, etc.)
                        // to put the caret at the end of the buffer — observed in
                        // ModernBookGump as "click goes to the last page". The draw path
                        // (DrawGlyphs) explicitly skips '\n'/'\r' glyphs so counting them
                        // here costs nothing visually.
                        if (IsUnicode)
                        {
                            _info = Client.Game.UO.FileManager.Fonts.GetInfoUnicode(
                                Font,
                                layoutText,
                                layoutText.Length,
                                Align,
                                (ushort)FontStyle,
                                layoutWidth,
                                countret: true,
                                countspaces: true
                            );
                        }
                        else
                        {
                            _info = Client.Game.UO.FileManager.Fonts.GetInfoASCII(
                                Font,
                                layoutText,
                                layoutText.Length,
                                Align,
                                (ushort)FontStyle,
                                layoutWidth,
                                countret: true,
                                countspaces: true
                            );
                        }

                        if (IsHTML)
                        {
                            Client.Game.UO.FileManager.Fonts.SetUseHTML(false);
                        }
                    }
                }
            }
        }

        public int LinesCount { get; set; }

        public bool SaveHitMap { get; private set; }

        public bool IsDestroyed { get; private set; }

        public int Width { get; private set; }

        public int Height { get; private set; }

        public Texture2D Texture { get; set; }

        public static RenderedText Create(
            string text,
            ushort hue = 0xFFFF,
            byte font = 0xFF,
            bool isunicode = true,
            FontStyle style = 0,
            TEXT_ALIGN_TYPE align = 0,
            int maxWidth = 0,
            byte cell = 30,
            bool isHTML = false,
            bool recalculateWidthByInfo = false,
            bool saveHitmap = false
        )
        {
            RenderedText r = _pool.GetOne();
            r.Hue = hue;
            r.Font = font;
            r.IsUnicode = isunicode;
            r.FontStyle = style;
            r.Cell = cell;
            r.Align = align;
            r.MaxWidth = maxWidth;
            r.IsHTML = isHTML;
            r.RecalculateWidthByInfo = recalculateWidthByInfo;
            r.Width = 0;
            r.Height = 0;
            r.SaveHitMap = saveHitmap;
            r.HTMLColor = 0xFFFF_FFFF;
            r.HasBackgroundColor = false;

            if (r.Text != text)
            {
                r.Text = text; // here makes the texture
            }
            else
            {
                r.CreateTexture();
            }

            return r;
        }

        public Point GetCaretPosition(int caret_index)
        {
            Point p;

            if (IsUnicode)
            {
                (p.X, p.Y) = Client.Game.UO.FileManager.Fonts.GetCaretPosUnicode(
                    Font,
                    Text,
                    caret_index,
                    MaxWidth,
                    Align,
                    (ushort)FontStyle
                );
            }
            else
            {
                (p.X, p.Y) = Client.Game.UO.FileManager.Fonts.GetCaretPosASCII(
                    Font,
                    Text,
                    caret_index,
                    MaxWidth,
                    Align,
                    (ushort)FontStyle
                );
            }

            return p;
        }

        public MultilinesFontInfo GetInfo()
        {
            return _info;
        }

        public bool PixelCheck(int x, int y)
        {
            if (string.IsNullOrWhiteSpace(Text))
            {
                return false;
            }

            // For atlas-based text without SaveHitMap, use bounding-box hit test
            if (!SaveHitMap)
            {
                return x >= 0 && x < Width && y >= 0 && y < Height;
            }

            ushort hue = Hue;

            if (!IsUnicode && SaveHitMap)
            {
                hue = 0x7FFF;
            }

            ulong b = (ulong)(
                Text.GetHashCode()
                ^ hue
                ^ ((int)Align)
                ^ ((int)FontStyle)
                ^ Font
                ^ (IsUnicode ? 0x01 : 0x00)
            );

            return _picker.Get(b, x, y);
        }

        public TextEditRow GetLayoutRow(int startIndex)
        {
            TextEditRow r = new TextEditRow();

            if (string.IsNullOrEmpty(Text))
            {
                return r;
            }

            MultilinesFontInfo info = _info;

            if (info == null)
            {
                return r;
            }

            switch (Align)
            {
                case TEXT_ALIGN_TYPE.TS_LEFT:
                    r.x0 = 0;
                    r.x1 = Width;

                    break;

                case TEXT_ALIGN_TYPE.TS_CENTER:
                    r.x0 = (Width - info.Width) >> 1;

                    if (r.x0 < 0)
                    {
                        r.x0 = 0;
                    }

                    r.x1 = r.x0;

                    break;

                case TEXT_ALIGN_TYPE.TS_RIGHT:
                    r.x0 = Width;

                    // TODO: r.x1 ???  i don't know atm :D
                    break;
            }

            int start = 0;

            while (info != null)
            {
                if (startIndex >= start && startIndex < start + info.CharCount)
                {
                    r.num_chars = info.CharCount;
                    r.ymax = info.MaxHeight;
                    r.baseline_y_delta = info.MaxHeight;

                    break;
                }

                start += info.CharCount;
                info = info.Next;
            }

            return r;
        }

        public int GetCharWidthAtIndex(int index)
        {
            if (string.IsNullOrEmpty(Text))
            {
                return 0;
            }

            MultilinesFontInfo info = _info;

            int start = 0;

            while (info != null)
            {
                if (index >= start && index < start + info.CharCount)
                {
                    int x = index - start;

                    if (x >= 0)
                    {
                        char c = x >= info.Data.Count ? '\n' : info.Data[x].Item;

                        if (IsUnicode)
                        {
                            return Client.Game.UO.FileManager.Fonts.GetCharWidthUnicode(Font, c);
                        }

                        return Client.Game.UO.FileManager.Fonts.GetCharWidthASCII(Font, c);
                    }
                }

                start += info.CharCount;
                info = info.Next;
            }

            return 0;
        }

        public int GetCharWidth(char c)
        {
            if (IsUnicode)
            {
                return Client.Game.UO.FileManager.Fonts.GetCharWidthUnicode(Font, c);
            }

            return Client.Game.UO.FileManager.Fonts.GetCharWidthASCII(Font, c);
        }

        private Vector3 GetHueVector(ushort hue, float alpha)
        {
            if (!IsUnicode && SaveHitMap && hue == 0)
            {
                hue = Hue;
            }

            if (hue > 0)
            {
                --hue;
            }

            Vector3 hueVector = new Vector3(hue, 0, alpha);

            if (hue != 0)
            {
                if (IsUnicode)
                {
                    hueVector.Y = ShaderHueTranslator.SHADER_TEXT_HUE_NO_BLACK;
                }
                else if (Font != 5 && Font != 8)
                {
                    hueVector.Y = ShaderHueTranslator.SHADER_PARTIAL_HUED;
                }
                else
                {
                    hueVector.Y = ShaderHueTranslator.SHADER_HUED;
                }
            }

            return hueVector;
        }

        // ── Atlas-based per-glyph drawing ──

        private bool DrawGlyphs(
            UltimaBatcher2D batcher,
            int destX,
            int destY,
            float depth,
            float alpha,
            ushort drawHue,
            float scale,
            int clipOffsetX,
            int clipOffsetY,
            int clipWidth,
            int clipHeight
        )
        {
            if (!HasContent || _info == null)
            {
                return false;
            }

            var atlas = Client.Game.UO.FontGlyphAtlas;
            if (atlas == null)
                return false;

            // Draw HTML background color if present
            if (_htmlBgColor != 0)
            {
                Color bgColor = default;
                bgColor.PackedValue = _htmlBgColor;
                int bgX = destX - (int)(clipOffsetX * scale);
                int bgY = destY - (int)(clipOffsetY * scale);
                int bgW = (int)(Width * scale);
                int bgH = (int)(Height * scale);

                batcher.Draw(
                    SolidColorTextureCache.GetTexture(bgColor),
                    new Rectangle(bgX, bgY, bgW, bgH),
                    new Vector3(0, 0, alpha),
                    depth
                );
            }

            // Compute base color/hue baked into atlas glyphs.
            // For Unicode: Hue=0xFFFF → white (0), otherwise GetPolygoneColor result (ARGB).
            // For ASCII: the Hue index itself is passed to RenderSingleGlyphASCII for per-pixel hue.
            uint baseColor = 0;
            if (IsUnicode)
            {
                if (Hue != 0xFFFF)
                {
                    baseColor = HuesHelper.RgbaToArgb(
                        (Client.Game.UO.FileManager.Hues.GetPolygoneColor(Cell, Hue) << 8) | 0xFF
                    );
                }
            }
            else if (!IsHTML)
            {
                // For ASCII, pass hue index as the color key (cast to uint)
                baseColor = Hue;
            }

            // Hue vector: no shader hue by default (color is baked).
            // Only use shader hue for draw-time overrides (e.g., highlighting with drawHue > 0).
            Vector3 hueVector;
            if (drawHue > 0)
            {
                hueVector = GetHueVector(drawHue, alpha);
            }
            else
            {
                hueVector = new Vector3(0, 0, alpha);
            }

            // Base style flags from FontStyle (non-HTML uses these for all chars)
            bool hasBorder = (FontStyle & FontStyle.BlackBorder) != 0;
            bool isSolid = (FontStyle & FontStyle.Solid) != 0;
            bool isItalic = (FontStyle & FontStyle.Italic) != 0;
            bool isUnderline = (FontStyle & FontStyle.Underline) != 0;

            int lineOffsY = 0;
            MultilinesFontInfo ptr = _info;
            int textWidth = Width;

            while (ptr != null)
            {
                int w = 0;

                switch (ptr.Align)
                {
                    case TEXT_ALIGN_TYPE.TS_CENTER:
                        if (IsUnicode)
                        {
                            w = (textWidth - 8) / 2 - ptr.Width / 2;
                        }
                        else
                        {
                            w = (textWidth - ptr.Width) >> 1;
                        }
                        if (w < 0) w = 0;
                        break;

                    case TEXT_ALIGN_TYPE.TS_RIGHT:
                        w = textWidth - 10 - ptr.Width;
                        if (w < 0) w = 0;
                        break;

                    case TEXT_ALIGN_TYPE.TS_LEFT:
                        if ((FontStyle & FontStyle.Indention) != 0)
                            w = ptr.IndentionOffset;
                        break;
                }

                int dataLen = ptr.Data.Count;
                var dataSpan = CollectionsMarshal.AsSpan(ptr.Data);

                for (int i = 0; i < dataLen; i++)
                {
                    ref MultilinesFontData dataPtr = ref dataSpan[i];
                    char si = dataPtr.Item;

                    if (si == '\n' || si == '\r')
                        continue;

                    // Per-character styling
                    byte charFont = Font;
                    bool charBorder = hasBorder;
                    bool charSolid = isSolid;
                    bool charItalic = isItalic;
                    bool charUnderline = isUnderline;
                    uint charColor = baseColor; // baked base color for Unicode
                    Vector3 charHueVector = hueVector;

                    if (IsHTML)
                    {
                        charFont = dataPtr.Font;
                        charBorder = (dataPtr.Flags & 0x0008) != 0; // UOFONT_BLACK_BORDER
                        charSolid = (dataPtr.Flags & 0x0001) != 0;  // UOFONT_SOLID
                        charItalic = (dataPtr.Flags & 0x0002) != 0;  // UOFONT_ITALIC
                        charUnderline = (dataPtr.Flags & 0x0010) != 0; // UOFONT_UNDERLINE

                        if (dataPtr.Color != 0xFFFFFFFF)
                        {
                            charColor = HuesHelper.RgbaToArgb(dataPtr.Color);
                        }
                    }

                    // Match GeneratePixelsUnicode: skip black border when text color is near-black.
                    // A black border on near-black text is invisible and produces artifacts.
                    if (charBorder && IsUnicode && charColor != 0)
                    {
                        bool isBlackPixel =
                            ((charColor >> 0) & 0xFF) <= 8
                            && ((charColor >> 8) & 0xFF) <= 8
                            && ((charColor >> 16) & 0xFF) <= 8;

                        if (isBlackPixel)
                            charBorder = false;
                    }

                    // Get glyph from atlas
                    GlyphAtlasEntry entry;
                    if (charColor != 0)
                    {
                        entry = atlas.GetColoredEntry(
                            charFont, si, IsUnicode, charBorder, charSolid, charItalic, charColor
                        );
                    }
                    else
                    {
                        entry = atlas.GetEntry(
                            charFont, si, IsUnicode, charBorder, charSolid, charItalic
                        );
                    }

                    if (si == ' ')
                    {
                        w += entry.AdvanceWidth;
                        continue;
                    }

                    if (entry.IsValid)
                    {
                        int localX = w + entry.BearingX;
                        int localY = lineOffsY + entry.BearingY;

                        // Clip check
                        if (localX + entry.GlyphWidth > clipOffsetX
                            && localX < clipOffsetX + clipWidth
                            && localY + entry.GlyphHeight > clipOffsetY
                            && localY < clipOffsetY + clipHeight)
                        {
                            int drawX = destX + (int)(localX * scale) - (int)(clipOffsetX * scale);
                            int drawY = destY + (int)(localY * scale) - (int)(clipOffsetY * scale);
                            int drawW = (int)(entry.GlyphWidth * scale);
                            int drawH = (int)(entry.GlyphHeight * scale);

                            batcher.Draw(
                                entry.Texture,
                                new Rectangle(drawX, drawY, drawW, drawH),
                                entry.UV,
                                charHueVector,
                                depth
                            );
                        }
                    }

                    w += entry.AdvanceWidth;
                }

                // Draw underline for this line
                if (isUnderline && dataLen > 0)
                {
                    var aEntry = atlas.GetEntry(Font, 'a', IsUnicode, false, false, false);
                    int underlineY = lineOffsY + aEntry.BearingY + aEntry.GlyphHeight;

                    if (underlineY >= clipOffsetY && underlineY < clipOffsetY + clipHeight)
                    {
                        int drawUX = destX - (int)(clipOffsetX * scale);
                        int drawUY = destY + (int)(underlineY * scale) - (int)(clipOffsetY * scale);
                        int drawUW = (int)(ptr.Width * scale);

                        batcher.Draw(
                            SolidColorTextureCache.GetTexture(Color.White),
                            new Rectangle(drawUX, drawUY, drawUW, 1),
                            hueVector,
                            depth
                        );
                    }
                }

                // Match GeneratePixelsASCII: font 6 reduces line spacing by 7px
                int font6OffsetY = !IsUnicode && Font == 6 ? 7 : 0;
                lineOffsY += ptr.MaxHeight - font6OffsetY;
                ptr = ptr.Next;
            }

            return true;
        }

        // ── Draw overloads (all use atlas-based glyph rendering) ──

        public bool Draw(
            UltimaBatcher2D batcher,
            int swidth,
            int sheight,
            int dx,
            int dy,
            int dwidth,
            int dheight,
            int offsetX,
            int offsetY,
            float layerDepth,
            ushort hue = 0
        )
        {
            if (!HasContent)
                return false;

            if (offsetX > swidth || offsetX < -swidth || offsetY > sheight || offsetY < -sheight)
                return false;

            int srcWidth = Math.Min(dwidth, swidth - offsetX);
            int srcHeight = Math.Min(dheight, sheight - offsetY);

            return DrawGlyphs(
                batcher, dx, dy, layerDepth, 1f, hue, 1f,
                offsetX, offsetY, srcWidth, srcHeight
            );
        }

        public bool Draw(
            UltimaBatcher2D batcher,
            int dx,
            int dy,
            int sx,
            int sy,
            int swidth,
            int sheight,
            float layerDepth,
            int hue = -1
        )
        {
            if (!HasContent)
                return false;

            ushort effectiveHue = hue > 0 ? (ushort)hue : (ushort)0;
            return DrawGlyphs(
                batcher, dx, dy, layerDepth, 1f, effectiveHue, 1f,
                sx, sy, swidth, sheight
            );
        }

        public bool Draw(UltimaBatcher2D batcher, int x, int y, float depth, float alpha = 1, ushort hue = 0, float scale = 1f)
        {
            if (!HasContent)
                return false;

            return DrawGlyphs(
                batcher, x, y, depth, alpha, hue, scale,
                0, 0, Width, Height
            );
        }

        public unsafe void CreateTexture()
        {
            // Atlas path: generate FontInfo for Width/Height/LinesCount/Links,
            // but don't create a per-string Texture2D — glyphs are drawn from the shared atlas.
            if (Texture != null && !Texture.IsDisposed)
            {
                Texture.Dispose();
                Texture = null;
            }

            if (IsHTML)
            {
                Client.Game.UO.FileManager.Fonts.SetUseHTML(true, HTMLColor, HasBackgroundColor);
            }

            Client.Game.UO.FileManager.Fonts.RecalculateWidthByInfo = RecalculateWidthByInfo;

            FontsLoader.FontInfo fi;
            if (IsUnicode)
            {
                fi = Client.Game.UO.FileManager.Fonts.GenerateUnicode(
                    Font, Text, Hue, Cell, MaxWidth, Align,
                    (ushort)FontStyle, SaveHitMap, MaxHeight
                );
            }
            else
            {
                fi = Client.Game.UO.FileManager.Fonts.GenerateASCII(
                    Font, Text, Hue, MaxWidth, Align,
                    (ushort)FontStyle, SaveHitMap, MaxHeight
                );
            }

            if (SaveHitMap)
            {
                var b = (ulong)(
                    Text.GetHashCode()
                    ^ Hue
                    ^ ((int)Align)
                    ^ ((int)FontStyle)
                    ^ Font
                    ^ (IsUnicode ? 0x01 : 0x00)
                );
                _picker.Set(b, fi.Width, fi.Height, fi.Data);
            }

            Links.Clear();
            if (fi.Links != null)
            {
                for (int i = 0; i < fi.Links.Length; ++i)
                {
                    Links.Add(fi.Links[i]);
                }
            }

            LinesCount = fi.LineCount;
            Width = fi.Width;
            Height = fi.Height;
            _htmlBgColor = fi.HtmlBackgroundColor;

            if (IsHTML)
            {
                Client.Game.UO.FileManager.Fonts.SetUseHTML(false);
            }

            Client.Game.UO.FileManager.Fonts.RecalculateWidthByInfo = false;
        }

        public void Destroy()
        {
            if (IsDestroyed)
            {
                return;
            }

            IsDestroyed = true;

            if (Texture != null && !Texture.IsDisposed)
            {
                Texture.Dispose();
            }

            _pool.ReturnOne(this);
        }
    }
}
