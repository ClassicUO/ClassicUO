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
                            FontsLoader.Instance.SetUseHTML(false);
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
                            _info = FontsLoader.Instance.GetInfoUnicode(
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
                            _info = FontsLoader.Instance.GetInfoASCII(
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
                (p.X, p.Y) = FontsLoader.Instance.GetCaretPosUnicode(
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
                (p.X, p.Y) = FontsLoader.Instance.GetCaretPosASCII(
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
                            return FontsLoader.Instance.GetCharWidthUnicode(Font, c);
                        }

                        return FontsLoader.Instance.GetCharWidthASCII(Font, c);
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
                return FontsLoader.Instance.GetCharWidthUnicode(Font, c);
            }

            return FontsLoader.Instance.GetCharWidthASCII(Font, c);
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
            if (string.IsNullOrEmpty(Text) || Texture == null || IsDestroyed || Texture.IsDisposed)
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
            if (string.IsNullOrEmpty(Text) || Texture == null || IsDestroyed || Texture.IsDisposed)
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
            if (string.IsNullOrEmpty(Text) || Texture == null || IsDestroyed || Texture.IsDisposed)
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
                FontsLoader.Instance.SetUseHTML(true, HTMLColor, HasBackgroundColor);
            }

            FontsLoader.Instance.RecalculateWidthByInfo = RecalculateWidthByInfo;

            FontsLoader.FontInfo fi;
            if (IsUnicode)
            {
                fi = FontsLoader.Instance.GenerateUnicode(
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
                fi = FontsLoader.Instance.GenerateASCII(
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
                FontsLoader.Instance.SetUseHTML(false);
            }

            FontsLoader.Instance.RecalculateWidthByInfo = false;
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
