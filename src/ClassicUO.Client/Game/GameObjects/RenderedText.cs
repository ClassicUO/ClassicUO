#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using ClassicUO.IO;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StbTextEditSharp;
using System;
using System.Collections.Generic;
using FontStashSharp.RichText;
using System.Text.RegularExpressions;

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

    internal abstract class BaseRenderedText
    {
        public abstract bool IsValid { get; }
    }

    internal sealed class TTFRenderedText
    {

    }

    internal sealed class RenderedText : BaseRenderedText
    {
        private static PixelPicker _picker = new PixelPicker();
        private byte _font;
        private readonly RichTextLayout _textLayout = new RichTextLayout()
        {
            CalculateGlyphs = true
        };

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
                        CreateTexture();
                        if (IsHTML)
                        {
                            //FontsLoader.Instance.SetUseHTML(false);
                        }

                        Links.Clear();
                        Texture?.Dispose();
                        Texture = null;
                        _info = null;
                    }
                    else
                    {
                        CreateTexture();

                        if (IsUnicode)
                        {
                            //_info = FontsLoader.Instance.GetInfoUnicode(
                            //    Font,
                            //    Text,
                            //    Text.Length,
                            //    Align,
                            //    (ushort)FontStyle,
                            //    MaxWidth > 0 ? MaxWidth : Width,
                            //    true,
                            //    true
                            //);
                        }
                        else
                        {
                            //_info = FontsLoader.Instance.GetInfoASCII(
                            //    Font,
                            //    Text,
                            //    Text.Length,
                            //    Align,
                            //    (ushort)FontStyle,
                            //    MaxWidth > 0 ? MaxWidth : Width,
                            //    true,
                            //    true
                            //);
                        }
                    }
                }
            }
        }

        public int LinesCount => _textLayout?.Lines.Count ?? 0;

        public bool SaveHitMap { get; private set; }

        public bool IsDestroyed { get; private set; }

        public int Width { get; private set; }

        public int Height { get; private set; }

        public Texture2D Texture { get; set; }

        public override bool IsValid => !IsDestroyed;

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
            var r = new RenderedText();
            r.Hue = hue;
            r.Font = font;
            r.IsUnicode = isunicode;
            r.FontStyle = style;
            r.Cell = cell;
            r.Align = align;
            r.MaxWidth = maxWidth;
            r.IsHTML = isHTML;
            r.RecalculateWidthByInfo = recalculateWidthByInfo;
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
            Point p = Point.Zero;

            var line = _textLayout?.GetLineByCursorPosition(caret_index);
            if (line != null)
            {
                var maxIndex = _textLayout.GetGlyphInfoByIndex(caret_index);
                p.X = maxIndex?.Bounds.X /*+ maxIndex?.XAdvance*/ ?? line.Size.X;
                p.Y = line.LineIndex * _textLayout?.Font?.LineHeight ?? 0;
            }

            //if (IsUnicode)
            //{
            //    (p.X, p.Y) = FontsLoader.Instance.GetCaretPosUnicode(
            //        Font,
            //        Text,
            //        caret_index,
            //        MaxWidth,
            //        Align,
            //        (ushort)FontStyle
            //    );
            //}
            //else
            //{
            //    (p.X, p.Y) = FontsLoader.Instance.GetCaretPosASCII(
            //        Font,
            //        Text,
            //        caret_index,
            //        MaxWidth,
            //        Align,
            //        (ushort)FontStyle
            //    );
            //}

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

            var vec = _textLayout.Font.TextBounds(Text, Vector2.Zero);
            var rect = new Rectangle((int)vec.X, (int)vec.Y, (int)vec.X2, (int)vec.Y2);

            return rect.Contains(x, y);

            //ushort hue = Hue;

            //if (!IsUnicode && SaveHitMap)
            //{
            //    hue = 0x7FFF;
            //}

            //ulong b = (ulong)(
            //    Text.GetHashCode()
            //    ^ hue
            //    ^ ((int)Align)
            //    ^ ((int)FontStyle)
            //    ^ Font
            //    ^ (IsUnicode ? 0x01 : 0x00)
            //);

            //return _picker.Get(b, x, y);
        }

        public TextEditRow GetLayoutRow(int startIndex)
        {
            TextEditRow r = new TextEditRow();

            if (string.IsNullOrEmpty(Text))
            {
                return r;
            }

            //MultilinesFontInfo info = _info;

            //if (info == null)
            //{
            //    return r;
            //}

            //switch (Align)
            //{
            //    case TEXT_ALIGN_TYPE.TS_LEFT:
            //        r.x0 = 0;
            //        r.x1 = Width;

            //        break;

            //    case TEXT_ALIGN_TYPE.TS_CENTER:
            //        r.x0 = (Width - info.Width) >> 1;

            //        if (r.x0 < 0)
            //        {
            //            r.x0 = 0;
            //        }

            //        r.x1 = r.x0;

            //        break;

            //    case TEXT_ALIGN_TYPE.TS_RIGHT:
            //        r.x0 = Width;

            //        // TODO: r.x1 ???  i don't know atm :D
            //        break;
            //}

            int start = 0;

            //while (info != null)
            //{
            //    if (startIndex >= start && startIndex < start + info.CharCount)
            //    {
            //        r.num_chars = info.CharCount;
            //        r.ymax = info.MaxHeight;
            //        r.baseline_y_delta = info.MaxHeight;

            //        break;
            //    }

            //    start += info.CharCount;
            //    info = info.Next;
            //}

            if (_textLayout == null)
                return r;

            r.x0 = 0;
            r.x1 = Width;

            foreach (var line in _textLayout.Lines)
            {
                if (startIndex >= start && startIndex < start + line.Count)
                {
                    r.num_chars = line.Count;
                    r.ymax = line.Size.Y;
                    r.baseline_y_delta = line.Size.Y;

                    break;
                }

                start += line.Count;
            }

            return r;
        }

        public int GetCharWidthAtIndex(int index)
        {
            if (string.IsNullOrEmpty(Text))
            {
                return 0;
            }

            //MultilinesFontInfo info = _info;

            //int start = 0;

            //while (info != null)
            //{
            //    if (index >= start && index < start + info.CharCount)
            //    {
            //        int x = index - start;

            //        if (x >= 0)
            //        {
            //            char c = x >= info.Data.Length ? '\n' : info.Data[x].Item;

            //            if (IsUnicode)
            //            {
            //                return FontsLoader.Instance.GetCharWidthUnicode(Font, c);
            //            }

            //            return FontsLoader.Instance.GetCharWidthASCII(Font, c);
            //        }
            //    }

            //    start += info.CharCount;
            //    info = info.Next;
            //}

            return _textLayout?.GetGlyphInfoByIndex(index)?.Bounds.Width ?? 0;
        }

        public int GetCharWidth(char c)
        {
            var f = _textLayout?.Font?.MeasureString(c.ToString()).X ?? 0;
            return (int)f;

            //if (IsUnicode)
            //{
            //    return FontsLoader.Instance.GetCharWidthUnicode(Font, c);
            //}

            //return FontsLoader.Instance.GetCharWidthASCII(Font, c);
        }

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
            ushort hue = 0
        )
        {
            //if (string.IsNullOrEmpty(Text) || Texture == null || IsDestroyed || Texture.IsDisposed)
            //{
            //    return false;
            //}

            if (offsetX > swidth || offsetX < -swidth || offsetY > sheight || offsetY < -sheight)
            {
                return false;
            }

            int srcX = offsetX;
            int srcY = offsetY;
            int maxX = srcX + dwidth;

            int srcWidth;
            int srcHeight;

            if (maxX <= swidth)
            {
                srcWidth = dwidth;
            }
            else
            {
                srcWidth = swidth - srcX;
                dwidth = srcWidth;
            }

            int maxY = srcY + dheight;

            if (maxY <= sheight)
            {
                srcHeight = dheight;
            }
            else
            {
                srcHeight = sheight - srcY;
                dheight = srcHeight;
            }

            if (!IsUnicode && SaveHitMap && hue == 0)
            {
                hue = Hue;
            }

            if (hue > 0)
            {
                --hue;
            }

            Vector3 hueVector = new Vector3(hue, 0, 1f);

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
            else
            {
                hueVector.Y = 0;
            }

            //_textLayout.Draw(batcher,)

            //batcher.Draw(
            //    Texture,
            //    new Rectangle(dx, dy, dwidth, dheight),
            //    new Rectangle(srcX, srcY, srcWidth, srcHeight),
            //    hueVector
            //);

            if (batcher.ClipBegin(dx, dy, dwidth, dheight))
            {
                _textLayout.Draw(batcher, new Vector2(dx - offsetX, dy - offsetY), GetColor(1f, hue));
                batcher.ClipEnd();
            }

            return true;
        }

        public bool Draw(
            UltimaBatcher2D batcher,
            int dx,
            int dy,
            int sx,
            int sy,
            int swidth,
            int sheight,
            int hue = -1
        )
        {
            //if (string.IsNullOrEmpty(Text) || Texture == null || IsDestroyed || Texture.IsDisposed)
            //{
            //    return false;
            //}

            //if (sx > Texture.Width || sy > Texture.Height)
            //{
            //    return false;
            //}

            if (!IsUnicode && SaveHitMap && hue == -1)
            {
                hue = Hue;
            }

            if (hue > 0)
            {
                --hue;
            }

            Vector3 hueVector = new Vector3(hue, 0, 1f);

            if (hue != -1)
            {
                hueVector.X = hue;

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
                else
                {
                    hueVector.Y = 0;
                }
            }

            //batcher.Draw(
            //    Texture,
            //    new Vector2(dx, dy),
            //    new Rectangle(sx, sy, swidth, sheight),
            //    hueVector
            //);

            if (batcher.ClipBegin(dx, dy, Width, Height))
            {
                _textLayout.Draw(batcher, new Vector2(dx - sx, dy - sy), GetColor(1f, (ushort)hue));
                batcher.ClipEnd();
            }

            //_textLayout.Draw(batcher, new Vector2(dx, dy), Color.Red);

            return true;
        }

        public bool Draw(UltimaBatcher2D batcher, int x, int y, float alpha = 1, ushort hue = 0)
        {
            //if (string.IsNullOrEmpty(Text) || Texture == null || IsDestroyed || Texture.IsDisposed)
            //{
            //    return false;
            //}

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
            else
            {
                hueVector.Y = 0;
            }

            var pos = new Vector2(x, y);
            var alignment = TextHorizontalAlignment.Left;

            if (Align == TEXT_ALIGN_TYPE.TS_CENTER)
            {
                pos.X += MaxWidth / 2;
                // pos.Y += MaxHeight > 0 ? MaxHeight / 2 : 0;

                alignment = TextHorizontalAlignment.Center;
            }

            _textLayout.Draw(batcher, pos, GetColor(alpha, hue), horizontalAlignment: alignment);

            return true;
        }

        private string ConvertHtmlToFontStashSharpCommand(string text)
        {
            string finalString;

            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var size = GetFontSize();

            finalString = Regex.Replace(text, "<basefont color=\"?'?(?<color>.*?)\"?'?>", " /c[${color}]", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            finalString = Regex.Replace(finalString, "<Bodytextcolor\"?'?(?<color>.*?)\"?'?>", " /c[${color}]", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            finalString = finalString.Replace("</basefont>", "/cd").Replace("</BASEFONT>", "/cd").Replace("<br>", "\n").Replace("<BR>", "\n").Replace("\n", "\n/cd");
            finalString = finalString.Replace("<left>", "").Replace("</left>", "");
            finalString = finalString.Replace("<b>", $"/f[bold, {size}]").Replace("</b>", "/fd").Replace("<B>", $"/f[bold, {size}]").Replace("</B>", "/fd");
            finalString = finalString.Replace("<u>", "/tu").Replace("</u>", "/td").Replace("<U>", "/tu").Replace("</U>", "/td");
            finalString = finalString.Replace("<i>", $"/f[italic, {size}]").Replace("</i>", "/fd").Replace("<I>", $"/f[italic, {size}]").Replace("</I>", "/fd");
            finalString = finalString.Replace("</font>", "").Replace("<h2>", "");

            if (FontStyle.HasFlag(FontStyle.Underline))
                finalString = "/tu" + finalString;

            return "/es[1]" + finalString.Trim();
        }

        private int GetFontSize()
        {
            switch (Font)
            {
                default:
                    return 16;
            }
        }

        private Color GetColor(float alpha = 1.0f, ushort hue = 0)
        {
            // this might be simplified somehow
            var h = HuesHelper.Color16To32(
                Client.Game.UO.FileManager.Hues.GetColor16(
                     HuesHelper.ColorToHue(Color.White), hue > 0 ? hue : Hue)) | 0xFF_00_00_00;

            var c = new Color() { PackedValue = h };
            c.A = (byte)Microsoft.Xna.Framework.MathHelper.Clamp(alpha * 255f, byte.MinValue, byte.MaxValue);

            return c;
        }

        private string GetFontName()
        {
            var font = "medium";

            if (FontStyle.HasFlag(FontStyle.Italic))
            {
                font += "italic";
            }

            if (FontStyle.HasFlag(FontStyle.Solid))
            {
                font = "bold";

                if (FontStyle.HasFlag(FontStyle.Italic))
                {
                    font = "bold-italic";
                }
            }

            return font;
        }

        public unsafe void CreateTexture()
        {
            _textLayout.Text = ConvertHtmlToFontStashSharpCommand(Text);
            _textLayout.Width = MaxWidth <= 0 ? null : MaxWidth;
            _textLayout.Height = MaxHeight <= 0 ? null : MaxHeight;
            _textLayout.Font = RichTextDefaults.FontResolver.Invoke($"{GetFontName()}, {GetFontSize()}");

            Width = _textLayout.Size.X;
            Height = _textLayout.Size.Y;

            //if (Texture != null && !Texture.IsDisposed)
            //{
            //    Texture.Dispose();
            //    Texture = null;
            //}

            //if (IsHTML)
            //{
            //    FontsLoader.Instance.SetUseHTML(true, HTMLColor, HasBackgroundColor);
            //}

            //FontsLoader.Instance.RecalculateWidthByInfo = RecalculateWidthByInfo;

            //FontsLoader.FontInfo fi;
            //if (IsUnicode)
            //{
            //    fi = FontsLoader.Instance.GenerateUnicode(
            //        Font,
            //        Text,
            //        Hue,
            //        Cell,
            //        MaxWidth,
            //        Align,
            //        (ushort)FontStyle,
            //        SaveHitMap,
            //        MaxHeight
            //    );
            //}
            //else
            //{
            //    fi = FontsLoader.Instance.GenerateASCII(
            //        Font,
            //        Text,
            //        Hue,
            //        MaxWidth,
            //        Align,
            //        (ushort)FontStyle,
            //        SaveHitMap,
            //        MaxHeight
            //    );
            //}

            //if (SaveHitMap)
            //{
            //    var b = (ulong)(
            //        Text.GetHashCode()
            //        ^ Hue
            //        ^ ((int)Align)
            //        ^ ((int)FontStyle)
            //        ^ Font
            //        ^ (IsUnicode ? 0x01 : 0x00)
            //    );
            //    _picker.Set(b, fi.Width, fi.Height, fi.Data);
            //}

            //var isValid = fi.Data != null && fi.Data.Length > 0;

            //if (isValid && (Texture == null || Texture.IsDisposed))
            //{
            //    Texture = new Texture2D(
            //        Client.Game.GraphicsDevice,
            //        fi.Width,
            //        fi.Height,
            //        false,
            //        SurfaceFormat.Color
            //    );
            //}

            //Links.Clear();
            //if (fi.Links != null)
            //{
            //    for (int i = 0; i < fi.Links.Length; ++i)
            //    {
            //        Links.Add(fi.Links[i]);
            //    }
            //}

            //LinesCount = fi.LineCount;

            //if (Texture != null && isValid)
            //{
            //    fixed (uint* dataPtr = fi.Data)
            //    {
            //        Texture.SetDataPointerEXT(
            //            0,
            //            null,
            //            (IntPtr)dataPtr,
            //            fi.Width * fi.Height * sizeof(uint)
            //        );
            //    }

            //    Width = Texture.Width;
            //    Height = Texture.Height;
            //}

            //if (IsHTML)
            //{
            //    FontsLoader.Instance.SetUseHTML(false);
            //}

            //FontsLoader.Instance.RecalculateWidthByInfo = false;
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
        }
    }

    internal class RenderedText2 : BaseRenderedText
    {
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
                    }
                    else
                    {
                        CreateTexture();

                        if (IsUnicode)
                        {
                            _info = Client.Game.UO.FileManager.Fonts.GetInfoUnicode(
                                Font,
                                Text,
                                Text.Length,
                                Align,
                                (ushort)FontStyle,
                                MaxWidth > 0 ? MaxWidth : Width,
                                true,
                                true
                            );
                        }
                        else
                        {
                            _info = Client.Game.UO.FileManager.Fonts.GetInfoASCII(
                                Font,
                                Text,
                                Text.Length,
                                Align,
                                (ushort)FontStyle,
                                MaxWidth > 0 ? MaxWidth : Width,
                                true,
                                true
                            );
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

        public override bool IsValid => !IsDestroyed && Texture != null && !Texture.IsDisposed;

        public static RenderedText2 Create(
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
            var r = new RenderedText2();
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
                        char c = x >= info.Data.Length ? '\n' : info.Data[x].Item;

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
            ushort hue = 0
        )
        {
            if (string.IsNullOrEmpty(Text) || !IsValid)
            {
                return false;
            }

            if (offsetX > swidth || offsetX < -swidth || offsetY > sheight || offsetY < -sheight)
            {
                return false;
            }

            int srcX = offsetX;
            int srcY = offsetY;
            int maxX = srcX + dwidth;

            int srcWidth;
            int srcHeight;

            if (maxX <= swidth)
            {
                srcWidth = dwidth;
            }
            else
            {
                srcWidth = swidth - srcX;
                dwidth = srcWidth;
            }

            int maxY = srcY + dheight;

            if (maxY <= sheight)
            {
                srcHeight = dheight;
            }
            else
            {
                srcHeight = sheight - srcY;
                dheight = srcHeight;
            }

            if (!IsUnicode && SaveHitMap && hue == 0)
            {
                hue = Hue;
            }

            if (hue > 0)
            {
                --hue;
            }

            Vector3 hueVector = new Vector3(hue, 0, 1f);

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
            else
            {
                hueVector.Y = 0;
            }

            batcher.Draw(
                Texture,
                new Rectangle(dx, dy, dwidth, dheight),
                new Rectangle(srcX, srcY, srcWidth, srcHeight),
                hueVector
            );

            return true;
        }

        public bool Draw(
            UltimaBatcher2D batcher,
            int dx,
            int dy,
            int sx,
            int sy,
            int swidth,
            int sheight,
            int hue = -1
        )
        {
            if (string.IsNullOrEmpty(Text) || !IsValid)
            {
                return false;
            }

            if (sx > Texture.Width || sy > Texture.Height)
            {
                return false;
            }

            if (!IsUnicode && SaveHitMap && hue == -1)
            {
                hue = Hue;
            }

            if (hue > 0)
            {
                --hue;
            }

            Vector3 hueVector = new Vector3(hue, 0, 1f);

            if (hue != -1)
            {
                hueVector.X = hue;

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
                else
                {
                    hueVector.Y = 0;
                }
            }

            batcher.Draw(
                Texture,
                new Vector2(dx, dy),
                new Rectangle(sx, sy, swidth, sheight),
                hueVector
            );

            return true;
        }

        public bool Draw(UltimaBatcher2D batcher, int x, int y, float alpha = 1, ushort hue = 0)
        {
            if (string.IsNullOrEmpty(Text) || !IsValid)
            {
                return false;
            }

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
            else
            {
                hueVector.Y = 0;
            }

            batcher.Draw(Texture, new Rectangle(x, y, Width, Height), hueVector);

            return true;
        }

        public unsafe void CreateTexture()
        {
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
                    Font,
                    Text,
                    Hue,
                    Cell,
                    MaxWidth,
                    Align,
                    (ushort)FontStyle,
                    SaveHitMap,
                    MaxHeight
                );
            }
            else
            {
                fi = Client.Game.UO.FileManager.Fonts.GenerateASCII(
                    Font,
                    Text,
                    Hue,
                    MaxWidth,
                    Align,
                    (ushort)FontStyle,
                    SaveHitMap,
                    MaxHeight
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

            var isValid = fi.Data != null && fi.Data.Length > 0;

            if (isValid && (Texture == null || Texture.IsDisposed))
            {
                Texture = new Texture2D(
                    Client.Game.GraphicsDevice,
                    fi.Width,
                    fi.Height,
                    false,
                    SurfaceFormat.Color
                );
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

            if (Texture != null && isValid)
            {
                fixed (uint* dataPtr = fi.Data)
                {
                    Texture.SetDataPointerEXT(
                        0,
                        null,
                        (IntPtr)dataPtr,
                        fi.Width * fi.Height * sizeof(uint)
                    );
                }

                Width = Texture.Width;
                Height = Texture.Height;
            }

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
        }
    }
}
