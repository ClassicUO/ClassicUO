#region license

// Copyright (c) 2021, andreakarasho
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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Collections;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;

namespace ClassicUO.IO.Resources
{
    internal class FontsLoader : UOFileLoader
    {
        private const int UOFONT_SOLID = 0x0001;
        private const int UOFONT_ITALIC = 0x0002;
        private const int UOFONT_INDENTION = 0x0004;
        private const int UOFONT_BLACK_BORDER = 0x0008;
        private const int UOFONT_UNDERLINE = 0x0010;
        private const int UOFONT_FIXED = 0x0020;
        private const int UOFONT_CROPPED = 0x0040;
        private const int UOFONT_BQ = 0x0080;
        private const int UOFONT_EXTRAHEIGHT = 0x0100;
        private const int UOFONT_CROPTEXTURE = 0x0200;
        private const int UOFONT_FIXEDHEIGHT = 0x0400;
        private const int UNICODE_SPACE_WIDTH = 8;
        private const int MAX_HTML_TEXT_HEIGHT = 18;
        private const byte NOPRINT_CHARS = 32;
        private const float ITALIC_FONT_KOEFFICIENT = 3.3f;

        private static FontsLoader _instance;

        struct HtmlStatus
        {
            public uint BackgroundColor;
            public uint VisitedWebLinkColor;
            public uint WebLinkColor;
            public uint Color;
            public Rectangle Margins;

            public bool IsHtmlBackgroundColored;
        }

        private HtmlStatus _htmlStatus;

        private FontCharacterData[][] _fontData;
        private readonly IntPtr[] _unicodeFontAddress = new IntPtr[20];
        private readonly long[] _unicodeFontSize = new long[20];
        private readonly Dictionary<ushort, WebLink> _webLinks = new Dictionary<ushort, WebLink>();
        private readonly int[] _offsetCharTable =
        {
            2, 0, 2, 2, 0, 0, 2, 2, 0, 0
        };
        private readonly int[] _offsetSymbolTable =
        {
            1, 0, 1, 1, -1, 0, 1, 1, 0, 0
        };

        private FontsLoader()
        {
        }

        public static FontsLoader Instance => _instance ?? (_instance = new FontsLoader());

        public int FontCount { get; private set; }

        public bool UnusePartialHue { get; set; } = false;

        public bool RecalculateWidthByInfo { get; set; } = false;

        public bool IsUsingHTML { get; set; }

        public override unsafe Task Load()
        {
            return Task.Run
            (
                () =>
                {
                    UOFileMul fonts = new UOFileMul(UOFileManager.GetUOFilePath("fonts.mul"));
                    UOFileMul[] uniFonts = new UOFileMul[20];

                    for (int i = 0; i < 20; i++)
                    {
                        string path = UOFileManager.GetUOFilePath("unifont" + (i == 0 ? "" : i.ToString()) + ".mul");

                        if (File.Exists(path))
                        {
                            uniFonts[i] = new UOFileMul(path);

                            _unicodeFontAddress[i] = uniFonts[i].StartAddress;

                            _unicodeFontSize[i] = uniFonts[i].Length;
                        }
                    }

                    int fontHeaderSize = sizeof(FontHeader);
                    FontCount = 0;

                    while (fonts.Position < fonts.Length)
                    {
                        bool exit = false;
                        fonts.Skip(1);

                        for (int i = 0; i < 224; i++)
                        {
                            FontHeader* fh = (FontHeader*) fonts.PositionAddress;

                            if (fonts.Position + fontHeaderSize >= fonts.Length)
                            {
                                continue;
                            }

                            fonts.Skip(fontHeaderSize);
                            int bcount = fh->Width * fh->Height * 2;

                            if (fonts.Position + bcount > fonts.Length)
                            {
                                exit = true;

                                break;
                            }

                            fonts.Skip(bcount);
                        }

                        if (exit)
                        {
                            break;
                        }

                        FontCount++;
                    }

                    if (FontCount < 1)
                    {
                        FontCount = 0;

                        return;
                    }

                    _fontData = new FontCharacterData[FontCount][];
                    fonts.Seek(0);

                    for (int i = 0; i < FontCount; i++)
                    {
                        byte header = fonts.ReadByte();

                        FontCharacterData[] datas = new FontCharacterData[224];

                        for (int j = 0; j < 224; j++)
                        {
                            if (fonts.Position + 3 >= fonts.Length)
                            {
                                continue;
                            }

                            byte w = fonts.ReadByte();
                            byte h = fonts.ReadByte();
                            fonts.Skip(1);
                            ushort[] data = fonts.ReadArray<ushort>(w * h);

                            datas[j] = new FontCharacterData(w, h, data);
                        }

                        _fontData[i] = datas;
                    }

                    if (_unicodeFontAddress[1] == IntPtr.Zero)
                    {
                        _unicodeFontAddress[1] = _unicodeFontAddress[0];
                        _unicodeFontSize[1] = _unicodeFontSize[0];
                    }
                }
            );
        }

        public bool UnicodeFontExists(byte font)
        {
            return font < 20 && _unicodeFontAddress[font] != IntPtr.Zero;
        }

        public (int, int) MeasureText
        (
            string text,
            byte font,
            bool isunicode,
            TEXT_ALIGN_TYPE align,
            ushort flags,
            int maxWidth = 200
        )
        {
            int width, height;

            if (isunicode)
            {
                width = GetWidthUnicode(font, text);

                if (width > maxWidth)
                {
                    width = GetWidthExUnicode
                    (
                        font,
                        text,
                        maxWidth,
                        align,
                        flags
                    );
                }

                height = GetHeightUnicode
                (
                    font,
                    text,
                    width,
                    align,
                    flags
                );
            }
            else
            {
                width = GetWidthASCII(font, text);

                if (width > maxWidth)
                {
                    width = GetWidthExASCII
                    (
                        font,
                        text,
                        maxWidth,
                        align,
                        flags
                    );
                }

                height = GetHeightASCII
                (
                    font,
                    text,
                    width,
                    align,
                    flags
                );
            }

            return (width, height);
        }

        /// <summary> Get the index in ASCII fonts of a character. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetASCIIIndex(char c)
        {
            byte ch = (byte) c; // ASCII fonts cover only 256 characters

            if (ch < NOPRINT_CHARS)
            {
                return 0;
            }

            return ch - NOPRINT_CHARS;
        }

        public int GetWidthASCII(byte font, string str)
        {
            if (font >= FontCount || string.IsNullOrEmpty(str))
            {
                return 0;
            }

            int textLength = 0;

            foreach (char c in str)
            {
                textLength += _fontData[font][GetASCIIIndex(c)].Width;
            }

            return textLength;
        }

        public int GetCharWidthASCII(byte font, char c)
        {
            if (font >= FontCount || c == 0 || c == '\r')
            {
                return 0;
            }

            if (c < NOPRINT_CHARS)
            {
                return _fontData[font][0].Width;
            }

            int index = c - NOPRINT_CHARS;

            if (index < _fontData[font].Length)
            {
                return _fontData[font][index].Width;
            }

            return 0;
        }

        public int GetWidthExASCII(byte font, string text, int maxwidth, TEXT_ALIGN_TYPE align, ushort flags)
        {
            if (font > FontCount || string.IsNullOrEmpty(text))
            {
                return 0;
            }

            MultilinesFontInfo info = GetInfoASCII
            (
                font,
                text,
                text.Length,
                align,
                flags,
                maxwidth
            );

            int textWidth = 0;

            while (info != null)
            {
                if (info.Width > textWidth)
                {
                    textWidth = info.Width;
                }

                MultilinesFontInfo ptr = info;
                info = info.Next;
                ptr.Data.Clear();
                ptr.Data.Count = 0;
                ptr = null;
            }

            return textWidth;
        }


        private int GetHeightASCII(MultilinesFontInfo info)
        {
            int textHeight = 0;

            while (info != null)
            {
                textHeight += info.MaxHeight;
                info = info.Next;
            }

            return textHeight;
        }

        public int GetHeightASCII(byte font, string str, int width, TEXT_ALIGN_TYPE align, ushort flags)
        {
            if (width == 0)
            {
                width = GetWidthASCII(font, str);
            }

            MultilinesFontInfo info = GetInfoASCII
            (
                font,
                str,
                str.Length,
                align,
                flags,
                width
            );

            int textHeight = 0;

            while (info != null)
            {
                if (IsUsingHTML)
                {
                    textHeight += MAX_HTML_TEXT_HEIGHT;
                }
                else
                {
                    textHeight += info.MaxHeight;
                }

                MultilinesFontInfo ptr = info;
                info = info.Next;
                ptr.Data.Clear();
                ptr.Data.Count = 0;
                ptr = null;
            }

            return textHeight;
        }

        public void GenerateASCII
        (
            ref FontTexture texture,
            byte font,
            string str,
            ushort color,
            int width,
            TEXT_ALIGN_TYPE align,
            ushort flags,
            bool saveHitmap,
            int height,
            PixelPicker picker
        )
        {
            if (string.IsNullOrEmpty(str))
            {
                return;
            }

            if ((flags & UOFONT_FIXED) != 0 || (flags & UOFONT_CROPPED) != 0 || (flags & UOFONT_CROPTEXTURE) != 0)
            {
                if (width == 0 || string.IsNullOrEmpty(str))
                {
                    return;
                }

                int realWidth = GetWidthASCII(font, str);

                if (realWidth > width)
                {
                    string newstr = GetTextByWidthASCII
                    (
                        font,
                        str,
                        width,
                        (flags & UOFONT_CROPPED) != 0,
                        align,
                        flags
                    );

                    if ((flags & UOFONT_CROPTEXTURE) != 0 && !string.IsNullOrEmpty(newstr))
                    {
                        int totalheight = 0;

                        while (totalheight < height)
                        {
                            totalheight += GetHeightASCII
                            (
                                font,
                                newstr,
                                width,
                                align,
                                flags
                            );

                            if (str.Length > newstr.Length)
                            {
                                newstr += GetTextByWidthASCII
                                (
                                    font,
                                    str.Substring(newstr.Length),
                                    width,
                                    (flags & UOFONT_CROPPED) != 0,
                                    align,
                                    flags
                                );
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    GeneratePixelsASCII
                    (
                        ref texture,
                        font,
                        newstr,
                        color,
                        width,
                        align,
                        flags,
                        saveHitmap,
                        picker
                    );

                    return;
                }
            }

            GeneratePixelsASCII
            (
                ref texture,
                font,
                str,
                color,
                width,
                align,
                flags,
                saveHitmap,
                picker
            );
        }

        public string GetTextByWidthASCII
        (
            byte font,
            string str,
            int width,
            bool isCropped,
            TEXT_ALIGN_TYPE align,
            ushort flags
        )
        {
            if (font >= FontCount || string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            ref FontCharacterData[] fd = ref _fontData[font];

            int strLen = str.Length;

           
            Span<char> span = stackalloc char[strLen];
            ValueStringBuilder sb = new ValueStringBuilder(span);

            if (IsUsingHTML)
            {
                unsafe
                {
                    HTMLChar* chars = stackalloc HTMLChar[strLen];

                    GetHTMLData
                    (
                        chars,
                        font,
                        str,
                        ref strLen,
                        align,
                        flags
                    );
                }

                int size = str.Length - strLen;

                if (size > 0)
                {
                    sb.Append(str.Substring(0, size));
                    str = str.Substring(str.Length - strLen, strLen);

                    if (GetWidthASCII(font, str) < width)
                    {
                        isCropped = false;
                    }
                }
            }

            if (isCropped)
            {
                width -= fd['.' - NOPRINT_CHARS].Width * 3;
            }

            int textLength = 0;

            foreach (char c in str)
            {
                textLength += _fontData[font][GetASCIIIndex(c)].Width;

                if (textLength > width)
                {
                    break;
                }

                sb.Append(c);
            }

            if (isCropped)
            {
                sb.Append("...");
            }

            string ss = sb.ToString();

            sb.Dispose();

            return ss;
        }

        private void GeneratePixelsASCII
        (
            ref FontTexture texture,
            byte font,
            string str,
            ushort color,
            int width,
            TEXT_ALIGN_TYPE align,
            ushort flags,
            bool saveHitmap,
            PixelPicker picker
        )
        {
            if (font >= FontCount)
            {
                return;
            }

            int len = str.Length;

            if (len == 0)
            {
                return;
            }

            ref FontCharacterData[] fd = ref _fontData[font];

            if (width <= 0)
            {
                width = GetWidthASCII(font, str);
            }

            if (width <= 0)
            {
                return;
            }

            MultilinesFontInfo info = GetInfoASCII
            (
                font,
                str,
                len,
                align,
                flags,
                width
            );

            if (info == null)
            {
                return;
            }

            width += 4;
            int height = GetHeightASCII(info);

            if (height <= 0)
            {
                MultilinesFontInfo ptr1 = info;

                while (ptr1 != null)
                {
                    info = ptr1;
                    ptr1 = ptr1.Next;
                    info.Data.Clear();
                    info.Data.Count = 0;
                    info = null;
                }

                return;
            }

            int blocksize = height * width;
            uint[] pData = System.Buffers.ArrayPool<uint>.Shared.Rent(blocksize);

            try
            {
                int lineOffsY = 0;
                MultilinesFontInfo ptr = info;
                bool isPartial = font != 5 && font != 8 && !UnusePartialHue;
                int font6OffsetY = font == 6 ? 7 : 0;
                int linesCount = 0; // this value should be added to TextTexture.LineCount += linesCount

                while (ptr != null)
                {
                    info = ptr;
                    linesCount++;
                    int w = 0;

                    switch (ptr.Align)
                    {
                        case TEXT_ALIGN_TYPE.TS_CENTER:

                            {
                                w = (width - ptr.Width) >> 1;

                                if (w < 0)
                                {
                                    w = 0;
                                }

                                break;
                            }

                        case TEXT_ALIGN_TYPE.TS_RIGHT:

                            {
                                w = width - 10 - ptr.Width;

                                if (w < 0)
                                {
                                    w = width;
                                }

                                break;
                            }

                        case TEXT_ALIGN_TYPE.TS_LEFT when (flags & UOFONT_INDENTION) != 0:
                            w = ptr.IndentionOffset;

                            break;
                    }

                    uint count = ptr.Data.Count;

                    for (int i = 0; i < count; i++)
                    {
                        byte index = (byte)ptr.Data[i].Item;

                        int offsY = GetFontOffsetY(font, index);

                        ref FontCharacterData fcd = ref fd[GetASCIIIndex(ptr.Data[i].Item)];

                        int dw = fcd.Width;
                        int dh = fcd.Height;
                        ushort charColor = color;

                        for (int y = 0; y < dh; y++)
                        {
                            int testY = y + lineOffsY + offsY;

                            if (testY >= height)
                            {
                                break;
                            }

                            for (int x = 0; x < dw; x++)
                            {
                                if (x + w >= width)
                                {
                                    break;
                                }

                                ushort pic = fcd.Data[y * dw + x];

                                if (pic != 0)
                                {
                                    uint pcl;

                                    if (isPartial)
                                    {
                                        pcl = HuesLoader.Instance.GetPartialHueColor(pic, charColor);
                                    }
                                    else
                                    {
                                        pcl = HuesLoader.Instance.GetColor(pic, charColor);
                                    }

                                    int block = testY * width + x + w;

                                    if (block >= 0)
                                    {
                                        pData[block] = pcl | 0xFF_00_00_00;
                                    }
                                }
                            }
                        }

                        w += dw;
                    }

                    lineOffsY += ptr.MaxHeight - font6OffsetY;
                    ptr = ptr.Next;
                    info.Data.Clear();
                    info.Data.Count = 0;
                    info = null;
                }

                if (texture == null || texture.IsDisposed)
                {
                    texture = new FontTexture(width, height, linesCount, new RawList<WebLinkRect>());
                }
                else
                {
                    texture.Links.Clear();
                    texture.LineCount = linesCount;
                }

                texture.SetData(pData, 0, width * height);

                if (saveHitmap)
                {
                    ulong b = (ulong)(str.GetHashCode() ^ color ^ ((int)align) ^ ((int)flags) ^ font ^ 0x00);
                    picker.Set(b, width, height, pData);
                }
            }
            finally
            {
                System.Buffers.ArrayPool<uint>.Shared.Return(pData, true);
            }
        }

        private int GetFontOffsetY(byte font, byte index)
        {
            if (index == 0xB8)
            {
                return 1;
            }

            if (!(index >= 0x41 && index <= 0x5A) && !(index >= 0xC0 && index <= 0xDF) && index != 0xA8)
            {
                if (font < 10)
                {
                    if (index >= 0x61 && index <= 0x7A)
                    {
                        return _offsetCharTable[font];
                    }

                    return _offsetSymbolTable[font];
                }

                return 2;
            }

            return 0;
        }

        public MultilinesFontInfo GetInfoASCII
        (
            byte font,
            string str,
            int len,
            TEXT_ALIGN_TYPE align,
            ushort flags,
            int width,
            bool countret = false,
            bool countspaces = false
        )
        {
            if (font >= FontCount)
            {
                return null;
            }

            ref FontCharacterData[] fd = ref _fontData[font];
            MultilinesFontInfo info = new MultilinesFontInfo();
            info.Reset();
            info.Align = align;
            MultilinesFontInfo ptr = info;
            int indentionOffset = 0;
            ptr.IndentionOffset = 0;
            bool isFixed = (flags & UOFONT_FIXED) != 0;
            bool isCropped = (flags & UOFONT_CROPPED) != 0;
            int charCount = 0;
            int lastSpace = 0;
            int readWidth = 0;
            int newlineval = countret ? 1 : 0;

            for (int i = 0; i < len; i++)
            {
                char si = str[i];

                if ( /*si == '\r' ||*/ si == '\n')
                {
                    if (si == '\r' || isFixed || isCropped)
                    {
                        continue;
                    }
                }

                if (si == ' ')
                {
                    lastSpace = i;
                    ptr.Width += readWidth;
                    readWidth = 0;
                    ptr.CharCount += charCount;
                    charCount = 0;
                }

                ref FontCharacterData fcd = ref fd[GetASCIIIndex(si)];
                int eval = ptr.CharStart;

                if (si == '\n' || ptr.Width + readWidth + fcd.Width > width)
                {
                    if (lastSpace == ptr.CharStart && lastSpace == 0 && si != '\n')
                    {
                        ++eval;
                    }

                    if (si == '\n')
                    {
                        ptr.Width += readWidth;
                        ptr.CharCount += charCount + newlineval;
                        lastSpace = i;

                        if (ptr.Width == 0)
                        {
                            ptr.Width = 1;
                        }

                        if (ptr.MaxHeight == 0)
                        {
                            ptr.MaxHeight = 14;
                        }

                        ptr.Data.Resize((uint) (ptr.CharCount - newlineval)); // = new List<MultilinesFontData>(ptr.CharCount);

                        MultilinesFontInfo newptr = new MultilinesFontInfo();
                        newptr.Reset();
                        ptr.Next = newptr;
                        ptr = newptr;
                        ptr.Align = align;
                        ptr.CharStart = i + 1;
                        readWidth = 0;
                        charCount = 0;
                        indentionOffset = 0;
                        ptr.IndentionOffset = 0;

                        continue;
                    }

                    if (lastSpace + 1 == eval && !isFixed && !isCropped)
                    {
                        ptr.Width += readWidth;
                        ptr.CharCount += charCount;

                        if (ptr.Width == 0)
                        {
                            ptr.Width = 1;
                        }

                        if (ptr.MaxHeight == 0)
                        {
                            ptr.MaxHeight = 14;
                        }

                        MultilinesFontInfo newptr = new MultilinesFontInfo();
                        newptr.Reset();
                        ptr.Next = newptr;
                        ptr = newptr;
                        ptr.Align = align;
                        ptr.CharStart = i;
                        lastSpace = i - 1;
                        charCount = 0;

                        if (ptr.Align == TEXT_ALIGN_TYPE.TS_LEFT && (flags & UOFONT_INDENTION) != 0)
                        {
                            indentionOffset = 14;
                        }

                        ptr.IndentionOffset = indentionOffset;
                        readWidth = indentionOffset;
                    }
                    else
                    {
                        if (isFixed)
                        {
                            MultilinesFontData mfd1 = new MultilinesFontData
                            (
                                0xFFFFFFFF,
                                flags,
                                font,
                                si,
                                0
                            );

                            ptr.Data.Add(mfd1);
                            readWidth += fcd.Width;

                            if (fcd.Height > ptr.MaxHeight)
                            {
                                ptr.MaxHeight = fcd.Height;
                            }

                            charCount++;
                            ptr.Width += readWidth;
                            ptr.CharCount += charCount;
                        }

                        i = lastSpace + 1;
                        si = i < len ? str[i] : '\0';

                        if (ptr.Width == 0)
                        {
                            ptr.Width = 1;
                        }
                        else if (countspaces && si != '\0' && lastSpace - eval == ptr.CharCount)
                        {
                            ptr.CharCount++;
                        }

                        if (ptr.MaxHeight == 0)
                        {
                            ptr.MaxHeight = 14;
                        }

                        //ptr.CharCount = charCount;
                        charCount = 0;
                        ptr.Data.Resize((uint) ptr.CharCount);

                        if (isFixed || isCropped)
                        {
                            break;
                        }

                        MultilinesFontInfo newptr = new MultilinesFontInfo();
                        newptr.Reset();
                        ptr.Next = newptr;
                        ptr = newptr;
                        ptr.Align = align;
                        ptr.CharStart = i;

                        if (ptr.Align == TEXT_ALIGN_TYPE.TS_LEFT && (flags & UOFONT_INDENTION) != 0)
                        {
                            indentionOffset = 14;
                        }

                        ptr.IndentionOffset = indentionOffset;
                        readWidth = indentionOffset;
                    }
                }

                MultilinesFontData mfd = new MultilinesFontData
                (
                    0xFFFFFFFF,
                    flags,
                    font,
                    si,
                    0
                );

                ptr.Data.Add(mfd);
                readWidth += si == '\r' ? 0 : fcd.Width;

                if (fcd.Height > ptr.MaxHeight)
                {
                    ptr.MaxHeight = fcd.Height;
                }

                charCount++;
            }

            ptr.Width += readWidth;
            ptr.CharCount += charCount;

            if (readWidth == 0 && len > 0 && (str[len - 1] == '\n' || str[len - 1] == '\r'))
            {
                ptr.Width = 1;
                ptr.MaxHeight = 14;
            }

            if (font == 4)
            {
                ptr = info;

                while (ptr != null)
                {
                    if (ptr.Width > 1)
                    {
                        ptr.MaxHeight = ptr.MaxHeight + 2;
                    }
                    else
                    {
                        ptr.MaxHeight = ptr.MaxHeight + 6;
                    }

                    ptr = ptr.Next;
                }
            }

            return info;
        }

        public void SetUseHTML(bool value, uint htmlStartColor = 0xFFFFFFFF, bool backgroundCanBeColored = false)
        {
            IsUsingHTML = value;
            _htmlStatus.Color = htmlStartColor;
            _htmlStatus.IsHtmlBackgroundColored = backgroundCanBeColored;
        }

        public void GenerateUnicode
        (
            ref FontTexture texture,
            byte font,
            string str,
            ushort color,
            byte cell,
            int width,
            TEXT_ALIGN_TYPE align,
            ushort flags,
            bool saveHitmap,
            int height,
            PixelPicker picker
        )
        {
            if (string.IsNullOrEmpty(str))
            {
                return;
            }

            if ((flags & UOFONT_FIXED) != 0 || (flags & UOFONT_CROPPED) != 0 || (flags & UOFONT_CROPTEXTURE) != 0)
            {
                if (width == 0 || string.IsNullOrEmpty(str))
                {
                    return;
                }

                int realWidth = GetWidthUnicode(font, str);

                if (realWidth > width)
                {
                    string newstr = GetTextByWidthUnicode
                    (
                        font,
                        str,
                        width,
                        (flags & UOFONT_CROPPED) != 0,
                        align,
                        flags
                    );

                    if ((flags & UOFONT_CROPTEXTURE) != 0 && !string.IsNullOrEmpty(newstr))
                    {
                        int totalheight = 0;

                        while (totalheight < height)
                        {
                            totalheight += GetHeightUnicode
                            (
                                font,
                                newstr,
                                width,
                                align,
                                flags
                            );

                            if (str.Length > newstr.Length)
                            {
                                newstr += GetTextByWidthUnicode
                                (
                                    font,
                                    str.Substring(newstr.Length),
                                    width,
                                    (flags & UOFONT_CROPPED) != 0,
                                    align,
                                    flags
                                );
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    GeneratePixelsUnicode
                    (
                        ref texture,
                        font,
                        newstr,
                        color,
                        cell,
                        width,
                        align,
                        flags,
                        saveHitmap,
                        picker
                    );

                    return;
                }
            }

            GeneratePixelsUnicode
            (
                ref texture,
                font,
                str,
                color,
                cell,
                width,
                align,
                flags,
                saveHitmap,
                picker
            );
        }

        public unsafe string GetTextByWidthUnicode
        (
            byte font,
            string str,
            int width,
            bool isCropped,
            TEXT_ALIGN_TYPE align,
            ushort flags
        )
        {
            if (font >= 20 || _unicodeFontAddress[font] == IntPtr.Zero || string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            uint* table = (uint*) _unicodeFontAddress[font];
            int strLen = str.Length;

            Span<char> span = stackalloc char[strLen];
            ValueStringBuilder sb = new ValueStringBuilder(span);

            if (IsUsingHTML)
            {
                unsafe
                {
                    HTMLChar* data = stackalloc HTMLChar[strLen];

                    GetHTMLData
                    (
                        data,
                        font,
                        str,
                        ref strLen,
                        align,
                        flags
                    );
                }

                int size = str.Length - strLen;

                if (size > 0)
                {
                    sb.Append(str.Substring(0, size));
                    str = str.Substring(str.Length - strLen, strLen);

                    if (GetWidthUnicode(font, str) < width)
                    {
                        isCropped = false;
                    }
                }
            }

            if (isCropped)
            {
                uint offset = table['.'];

                if (offset != 0 && offset != 0xFFFFFFFF)
                {
                    width -= *(byte*)((IntPtr)table + (int)offset + 2) * 3 + 3;
                }
            }


            int textLength = 0;

            foreach (char c in str)
            {
                uint offset = table[c];
                sbyte charWidth = 0;

                if (offset != 0 && offset != 0xFFFFFFFF)
                {
                    byte* ptr = (byte*)((IntPtr)table + (int)offset);
                    charWidth = (sbyte)((sbyte)ptr[0] + (sbyte)ptr[2] + 1);
                }
                else if (c == ' ')
                {
                    charWidth = UNICODE_SPACE_WIDTH;
                }

                if (charWidth != 0)
                {
                    textLength += charWidth;

                    if (textLength > width)
                    {
                        break;
                    }

                    sb.Append(c);
                }
            }

            if (isCropped)
            {
                sb.Append("...");
            }

            string ss = sb.ToString();

            sb.Dispose();

            return ss;
        }

        public unsafe int GetWidthUnicode(byte font, string str)
        {
            if (font >= 20 || _unicodeFontAddress[font] == IntPtr.Zero || string.IsNullOrEmpty(str))
            {
                return 0;
            }

            uint* table = (uint*) _unicodeFontAddress[font];
            int textLength = 0;
            int maxTextLenght = 0;

            foreach (char c in str)
            {
                uint offset = table[c];

                if (c != '\r' && offset != 0 && offset != 0xFFFFFFFF)
                {
                    byte* ptr = (byte*) ((IntPtr) table + (int) offset);
                    textLength += (sbyte) ptr[0] + (sbyte) ptr[2] + 1;
                }
                else if (c == ' ')
                {
                    textLength += UNICODE_SPACE_WIDTH;
                }
                else if (c == '\n')
                {
                    maxTextLenght = Math.Max(maxTextLenght, textLength);
                    textLength = 0;
                }
            }

            return Math.Max(maxTextLenght, textLength);
        }

        public unsafe int GetCharWidthUnicode(byte font, char c)
        {
            if (font >= 20 || _unicodeFontAddress[font] == IntPtr.Zero || c == 0 || c == '\r')
            {
                return 0;
            }

            uint* table = (uint*) _unicodeFontAddress[font];
            uint offset = table[c];

            if (offset != 0 && offset != 0xFFFFFFFF)
            {
                byte* ptr = (byte*) ((IntPtr) table + (int) offset);

                return (sbyte) ptr[0] + (sbyte) ptr[2] + 1;
            }

            if (c == ' ')
            {
                return UNICODE_SPACE_WIDTH;
            }

            return 0;
        }

        public int GetWidthExUnicode(byte font, string text, int maxwidth, TEXT_ALIGN_TYPE align, ushort flags)
        {
            if (font >= 20 || _unicodeFontAddress[font] == IntPtr.Zero || string.IsNullOrEmpty(text))
            {
                return 0;
            }

            MultilinesFontInfo info = GetInfoUnicode
            (
                font,
                text,
                text.Length,
                align,
                flags,
                maxwidth
            );

            int textWidth = 0;

            while (info != null)
            {
                if (info.Width > textWidth)
                {
                    textWidth = info.Width;
                }

                MultilinesFontInfo ptr = info;
                info = info.Next;
                ptr.Data.Clear();
                ptr.Data.Count = 0;
                ptr = null;
            }

            return textWidth;
        }

        public unsafe MultilinesFontInfo GetInfoUnicode
        (
            byte font,
            string str,
            int len,
            TEXT_ALIGN_TYPE align,
            ushort flags,
            int width,
            bool countret = false,
            bool countspaces = false
        )
        {
            _htmlStatus.WebLinkColor = 0xFF0000FF;
            _htmlStatus.VisitedWebLinkColor = 0x0000FFFF;
            _htmlStatus.BackgroundColor = 0;
            _htmlStatus.Margins = Rectangle.Empty;

            if (font >= 20 || _unicodeFontAddress[font] == IntPtr.Zero)
            {
                return null;
            }

            if (IsUsingHTML)
            {
                return GetInfoHTML
                (
                    font,
                    str,
                    len,
                    align,
                    flags,
                    width
                );
            }

            uint* table = (uint*) _unicodeFontAddress[font];
            MultilinesFontInfo info = new MultilinesFontInfo();
            info.Reset();
            info.Align = align;
            MultilinesFontInfo ptr = info;
            int indetionOffset = 0;
            ptr.IndentionOffset = 0;
            int charCount = 0;
            int lastSpace = 0;
            int readWidth = 0;
            int newlineval = countret ? 1 : 0;
            int extraheight = (flags & UOFONT_EXTRAHEIGHT) != 0 ? 4 : 0;
            bool isFixed = (flags & UOFONT_FIXED) != 0;
            bool isCropped = (flags & UOFONT_CROPPED) != 0;
            TEXT_ALIGN_TYPE current_align = align;
            ushort current_flags = flags;
            byte current_font = font;
            uint charcolor = 0xFFFFFFFF;
            uint current_charcolor = 0xFFFFFFFF;
            uint lastspace_charcolor = 0xFFFFFFFF;
            uint lastaspace_current_charcolor = 0xFFFFFFFF;

            for (int i = 0; i < len; i++)
            {
                char si = str[i];

                if (si == '\n')
                {
                    if (isFixed || isCropped)
                    {
                        si = (char) 0;
                    }
                }


                if ((table[si] == 0 || table[si] == 0xFFFFFFFF) && si != ' ' && si != '\n' && si != '\r')
                {
                    continue;
                }

                byte* data = (byte*) ((IntPtr) table + (int) table[si]);

                if (si == ' ')
                {
                    lastSpace = i;
                    ptr.Width += readWidth;
                    readWidth = 0;
                    ptr.CharCount += charCount;
                    charCount = 0;
                    lastspace_charcolor = charcolor;
                    lastaspace_current_charcolor = current_charcolor;
                }

                int eval = ptr.CharStart;

                if (ptr.Width + readWidth + (sbyte) data[0] + (sbyte) data[2] > width || si == '\n')
                {
                    if (lastSpace == ptr.CharStart && lastSpace == 0 && si != '\n')
                    {
                        ++eval;
                    }

                    if (si == '\n')
                    {
                        ptr.Width += readWidth;
                        ptr.CharCount += charCount + newlineval;
                        lastSpace = i;

                        if (ptr.Width == 0)
                        {
                            ptr.Width = 1;
                        }

                        if (ptr.MaxHeight == 0)
                        {
                            ptr.MaxHeight = 14 + extraheight;
                        }

                        ptr.Data.Resize((uint) (ptr.CharCount - newlineval));
                        MultilinesFontInfo newptr = new MultilinesFontInfo();
                        newptr.Reset();
                        ptr.Next = newptr;
                        ptr = newptr;
                        ptr.Align = current_align;
                        ptr.CharStart = i + 1;
                        readWidth = 0;
                        charCount = 0;
                        indetionOffset = 0;
                        ptr.IndentionOffset = 0;

                        continue;
                    }

                    if (lastSpace + 1 == eval && !isFixed && !isCropped)
                    {
                        ptr.Width += readWidth;
                        ptr.CharCount += charCount;

                        if (ptr.Width == 0)
                        {
                            ptr.Width = 1;
                        }

                        if (ptr.MaxHeight == 0)
                        {
                            ptr.MaxHeight = 14 + extraheight;
                        }

                        MultilinesFontInfo newptr = new MultilinesFontInfo();
                        newptr.Reset();
                        ptr.Next = newptr;
                        ptr = newptr;
                        ptr.Align = current_align;
                        ptr.CharStart = i;
                        lastSpace = i - 1;
                        charCount = 0;

                        if (ptr.Align == TEXT_ALIGN_TYPE.TS_LEFT && (current_flags & UOFONT_INDENTION) != 0)
                        {
                            indetionOffset = 14;
                        }

                        ptr.IndentionOffset = indetionOffset;
                        readWidth = indetionOffset;
                    }
                    else
                    {
                        if (isFixed)
                        {
                            MultilinesFontData mfd1 = new MultilinesFontData
                            (
                                current_charcolor,
                                current_flags,
                                current_font,
                                si,
                                0
                            );

                            ptr.Data.Add(mfd1);
                            readWidth += si == '\r' ? 0 : (sbyte) data[0] + (sbyte) data[2] + 1;

                            if ((sbyte) data[1] + (sbyte) data[3] > ptr.MaxHeight)
                            {
                                ptr.MaxHeight = (sbyte) data[1] + (sbyte) data[3] + extraheight;
                            }

                            charCount++;
                            ptr.Width += readWidth;
                            ptr.CharCount += charCount;
                        }

                        i = lastSpace + 1;
                        charcolor = lastspace_charcolor;
                        current_charcolor = lastspace_charcolor;
                        si = i < str.Length ? str[i] : '\0';

                        if (ptr.Width == 0)
                        {
                            ptr.Width = 1;
                        }
                        else if (countspaces && si != '\0' && lastSpace - eval == ptr.CharCount)
                        {
                            ptr.CharCount++;
                        }

                        if (ptr.MaxHeight == 0)
                        {
                            ptr.MaxHeight = 14 + extraheight;
                        }

                        //ptr.CharCount = charCount;

                        charCount = 0;
                        ptr.Data.Resize((uint) ptr.CharCount);

                        if (isFixed || isCropped)
                        {
                            break;
                        }

                        MultilinesFontInfo newptr = new MultilinesFontInfo();
                        newptr.Reset();
                        ptr.Next = newptr;
                        ptr = newptr;
                        ptr.Align = current_align;
                        ptr.CharStart = i;
                        charCount = 0;

                        if (ptr.Align == TEXT_ALIGN_TYPE.TS_LEFT && (current_flags & UOFONT_INDENTION) != 0)
                        {
                            indetionOffset = 14;
                        }

                        ptr.IndentionOffset = indetionOffset;
                        readWidth = indetionOffset;
                    }
                }

                MultilinesFontData mfd = new MultilinesFontData
                (
                    current_charcolor,
                    current_flags,
                    current_font,
                    si,
                    0
                );

                ptr.Data.Add(mfd);

                if (si == ' ')
                {
                    readWidth += UNICODE_SPACE_WIDTH;

                    if (ptr.MaxHeight <= 0)
                    {
                        ptr.MaxHeight = 5 + extraheight;
                    }
                }
                else
                {
                    readWidth += si == '\r' ? 0 : (sbyte) data[0] + (sbyte) data[2] + 1;

                    if ((sbyte) data[1] + (sbyte) data[3] > ptr.MaxHeight)
                    {
                        ptr.MaxHeight = (sbyte) data[1] + (sbyte) data[3] + extraheight;
                    }
                }

                charCount++;
            }

            ptr.Width += readWidth;
            ptr.CharCount += charCount;

            if (readWidth == 0 && len != 0)
            {
                switch (str[len - 1])
                {
                    case '\n':
                        ptr.CharCount += newlineval;
                        goto case '\r';

                    case '\r':
                        ptr.Width = 1;
                        ptr.MaxHeight = 14;

                        break;
                }
            }

            return info;
        }

        private unsafe void GeneratePixelsUnicode
        (
            ref FontTexture texture,
            byte font,
            string str,
            ushort color,
            byte cell,
            int width,
            TEXT_ALIGN_TYPE align,
            ushort flags,
            bool saveHitmap,
            PixelPicker picker
        )
        {
            if (font >= 20 || _unicodeFontAddress[font] == IntPtr.Zero)
            {
                return;
            }

            int len = str.Length;

            if (len == 0)
            {
                return;
            }

            int oldWidth = width;

            if (width == 0)
            {
                width = GetWidthUnicode(font, str);

                if (width == 0)
                {
                    return;
                }
            }

            MultilinesFontInfo info = GetInfoUnicode
            (
                font,
                str,
                len,
                align,
                flags,
                width
            );

            if (info == null)
            {
                return;
            }

            if (IsUsingHTML && (_htmlStatus.Margins.X != 0 || _htmlStatus.Margins.Width != 0))
            {
                while (info != null)
                {
                    MultilinesFontInfo ptr1 = info.Next;
                    info.Data.Clear();
                    info.Data.Count = 0;
                    info = null;
                    info = ptr1;
                }

                int newWidth = width - (_htmlStatus.Margins.Right);

                if (newWidth < 10)
                {
                    newWidth = 10;
                }

                info = GetInfoUnicode
                (
                    font,
                    str,
                    len,
                    align,
                    flags,
                    newWidth
                );

                if (info == null)
                {
                    return;
                }
            }

            if (oldWidth == 0 && RecalculateWidthByInfo)
            {
                MultilinesFontInfo ptr1 = info;
                width = 0;

                while (ptr1 != null)
                {
                    if (ptr1.Width > width)
                    {
                        width = ptr1.Width;
                    }

                    ptr1 = ptr1.Next;
                }
            }

            width += 4;
            int height = GetHeightUnicode(info);

            if (height == 0)
            {
                while (info != null)
                {
                    MultilinesFontInfo ptr1 = info;
                    info = info.Next;
                    ptr1.Data.Clear();
                    ptr1.Data.Count = 0;
                    ptr1 = null;
                }

                return;
            }

            height += _htmlStatus.Margins.Y + _htmlStatus.Margins.Height + 4;
            int blocksize = height * width;
            uint[] pData = System.Buffers.ArrayPool<uint>.Shared.Rent(blocksize);

            try
            {
                uint* table = (uint*)_unicodeFontAddress[font];
                int lineOffsY = _htmlStatus.Margins.Y;
                MultilinesFontInfo ptr = info;
                uint datacolor = 0;

                if (color == 0xFFFF)
                {
                    datacolor = 0xFEFFFFFF;
                }
                else
                {
                    datacolor = /*FileManager.Hues.GetPolygoneColor(cell, color) << 8 | 0xFF;*/
                        HuesHelper.RgbaToArgb((HuesLoader.Instance.GetPolygoneColor(cell, color) << 8) | 0xFF);
                }

                bool isItalic = (flags & UOFONT_ITALIC) != 0;
                bool isSolid = (flags & UOFONT_SOLID) != 0;
                bool isBlackBorder = (flags & UOFONT_BLACK_BORDER) != 0;
                bool isUnderline = (flags & UOFONT_UNDERLINE) != 0;
                uint blackColor = 0xFF010101;
                bool isLink = false;
                int linkStartX = 0;
                int linkStartY = 0;
                int linesCount = 0;
                RawList<WebLinkRect> links = new RawList<WebLinkRect>();

                while (ptr != null)
                {
                    info = ptr;
                    linesCount++;
                    int w = _htmlStatus.Margins.Y;

                    switch (ptr.Align)
                    {
                        case TEXT_ALIGN_TYPE.TS_CENTER:

                            {
                                w += (width - 8) / 2 - ptr.Width / 2;

                                if (w < 0)
                                {
                                    w = 0;
                                }

                                break;
                            }

                        case TEXT_ALIGN_TYPE.TS_RIGHT:

                            {
                                w += width - 10 - ptr.Width;

                                if (w < 0)
                                {
                                    w = 0;
                                }

                                break;
                            }

                        case TEXT_ALIGN_TYPE.TS_LEFT when (flags & UOFONT_INDENTION) != 0:
                            w += ptr.IndentionOffset;

                            break;
                    }

                    ushort oldLink = 0;
                    uint dataSize = ptr.Data.Count;

                    for (int i = 0; i < dataSize; i++)
                    {
                        ref MultilinesFontData dataPtr = ref ptr.Data[i];
                        char si = dataPtr.Item;
                        table = (uint*)_unicodeFontAddress[dataPtr.Font];

                        if (!isLink)
                        {
                            oldLink = dataPtr.LinkID;

                            if (oldLink != 0)
                            {
                                isLink = true;
                                linkStartX = w;
                                linkStartY = lineOffsY + 3;
                            }
                        }
                        else if (dataPtr.LinkID == 0 || i + 1 == dataSize)
                        {
                            isLink = false;
                            int linkHeight = lineOffsY - linkStartY;

                            if (linkHeight < 14)
                            {
                                linkHeight = 14;
                            }

                            int ofsX = 0;

                            if (si == ' ')
                            {
                                ofsX = UNICODE_SPACE_WIDTH;
                            }
                            else if ((table[si] == 0 || table[si] == 0xFFFFFFFF) && si != ' ')
                            {
                            }
                            else
                            {
                                byte* xData = (byte*)((IntPtr)table + (int)table[si]);
                                ofsX = (sbyte)xData[2];
                            }

                            WebLinkRect wlr = new WebLinkRect
                            {
                                LinkID = oldLink,
                                Bounds = new Rectangle(linkStartX, linkStartY, w - ofsX, linkHeight)
                            };

                            links.Add(wlr);
                            oldLink = 0;
                        }

                        if ((table[si] == 0 || table[si] == 0xFFFFFFFF) && si != ' ')
                        {
                            continue;
                        }

                        byte* data = (byte*)((IntPtr)table + (int)table[si]);
                        int offsX = 0;
                        int offsY = 0;
                        int dw = 0;
                        int dh = 0;

                        if (si == ' ')
                        {
                            offsX = 0;
                            dw = UNICODE_SPACE_WIDTH;
                        }
                        else
                        {
                            offsX = (sbyte)data[0] + 1;
                            offsY = (sbyte)data[1];
                            dw = data[2];
                            dh = data[3];
                            data += 4;
                        }

                        int tmpW = w;
                        uint charcolor = datacolor;

                        bool isBlackPixel = ((charcolor >> 0) & 0xFF) <= 8 && ((charcolor >> 8) & 0xFF) <= 8 && ((charcolor >> 16) & 0xFF) <= 8;

                        if (si != ' ')
                        {
                            if (IsUsingHTML && i < ptr.Data.Count)
                            {
                                isItalic = (dataPtr.Flags & UOFONT_ITALIC) != 0;
                                isSolid = (dataPtr.Flags & UOFONT_SOLID) != 0;
                                isBlackBorder = (dataPtr.Flags & UOFONT_BLACK_BORDER) != 0;
                                isUnderline = (dataPtr.Flags & UOFONT_UNDERLINE) != 0;

                                if (dataPtr.Color != 0xFFFFFFFF)
                                {
                                    charcolor = HuesHelper.RgbaToArgb(dataPtr.Color);

                                    //isBlackPixel = ((charcolor >> 24) & 0xFF) <= 8 && ((charcolor >> 16) & 0xFF) <= 8 && ((charcolor >> 8) & 0xFF) <= 8;
                                    isBlackPixel = ((charcolor >> 0) & 0xFF) <= 8 && ((charcolor >> 8) & 0xFF) <= 8 && ((charcolor >> 16) & 0xFF) <= 8;
                                }
                            }

                            int scanlineCount = ((dw - 1) >> 3) + 1;

                            for (int y = 0; y < dh; y++)
                            {
                                int testY = offsY + lineOffsY + y;

                                if (testY < 0)
                                {
                                    testY = 0;
                                }

                                if (testY >= height)
                                {
                                    break;
                                }

                                byte* scanlines = data;
                                data += scanlineCount;

                                int italicOffset = 0;

                                if (isItalic)
                                {
                                    italicOffset = (int)((dh - y) / ITALIC_FONT_KOEFFICIENT);
                                }

                                int testX = w + offsX + italicOffset + (isSolid ? 1 : 0);

                                for (int c = 0; c < scanlineCount; c++)
                                {
                                    int coff = c << 3;

                                    for (int j = 0; j < 8; j++)
                                    {
                                        int x = coff + j;

                                        if (x >= dw)
                                        {
                                            break;
                                        }

                                        int nowX = testX + x;

                                        if (nowX >= width)
                                        {
                                            break;
                                        }

                                        byte cl = (byte)(scanlines[c] & (1 << (7 - j)));
                                        int block = testY * width + nowX;

                                        if (cl != 0)
                                        {
                                            pData[block] = charcolor;
                                        }
                                    }
                                }
                            }

                            if (isSolid)
                            {
                                uint solidColor = blackColor;

                                if (solidColor == charcolor)
                                {
                                    solidColor++;
                                }

                                int minXOk = w + offsX > 0 ? -1 : 0;
                                int maxXOk = w + offsX + dw < width ? 1 : 0;
                                maxXOk += dw;

                                for (int cy = 0; cy < dh; cy++)
                                {
                                    int testY = offsY + lineOffsY + cy;

                                    if (testY >= height)
                                    {
                                        break;
                                    }

                                    if (testY < 0)
                                    {
                                        testY = 0;
                                    }

                                    int italicOffset = 0;

                                    if (isItalic && cy < dh)
                                    {
                                        italicOffset = (int)((dh - cy) / ITALIC_FONT_KOEFFICIENT);
                                    }

                                    for (int cx = minXOk; cx < maxXOk; cx++)
                                    {
                                        int testX = cx + w + offsX + italicOffset;

                                        if (testX >= width)
                                        {
                                            break;
                                        }

                                        int block = testY * width + testX;

                                        if (pData[block] == 0 && pData[block] != solidColor)
                                        {
                                            int endX = cx < dw ? 2 : 1;

                                            if (endX == 2 && testX + 1 >= width)
                                            {
                                                endX--;
                                            }

                                            for (int x = 0; x < endX; x++)
                                            {
                                                int nowX = testX + x;
                                                int testBlock = testY * width + nowX;

                                                if (pData[testBlock] != 0 && pData[testBlock] != solidColor)
                                                {
                                                    pData[block] = solidColor;

                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }

                                for (int cy = 0; cy < dh; cy++)
                                {
                                    int testY = offsY + lineOffsY + cy;

                                    if (testY >= height)
                                    {
                                        break;
                                    }

                                    if (testY < 0)
                                    {
                                        testY = 0;
                                    }

                                    int italicOffset = 0;

                                    if (isItalic)
                                    {
                                        italicOffset = (int)((dh - cy) / ITALIC_FONT_KOEFFICIENT);
                                    }

                                    for (int cx = 0; cx < dw; cx++)
                                    {
                                        int testX = cx + w + offsX + italicOffset;

                                        if (testX >= width)
                                        {
                                            break;
                                        }

                                        int block = testY * width + testX;

                                        if (pData[block] == solidColor)
                                        {
                                            pData[block] = charcolor;
                                        }
                                    }
                                }
                            }

                            if (isBlackBorder && !isBlackPixel)
                            {
                                int minXOk = w + offsX > 0 ? -1 : 0;
                                int minYOk = offsY + lineOffsY > 0 ? -1 : 0;
                                int maxXOk = w + offsX + dw < width ? 1 : 0;
                                int maxYOk = offsY + lineOffsY + dh < height ? 1 : 0;
                                maxXOk += dw;
                                maxYOk += dh;

                                for (int cy = minYOk; cy < maxYOk; cy++)
                                {
                                    int testY = offsY + lineOffsY + cy;

                                    if (testY < 0)
                                    {
                                        testY = 0;
                                    }

                                    if (testY >= height)
                                    {
                                        break;
                                    }

                                    int italicOffset = 0;

                                    if (isItalic && cy >= 0 && cy < dh)
                                    {
                                        italicOffset = (int)((dh - cy) / ITALIC_FONT_KOEFFICIENT);
                                    }

                                    for (int cx = minXOk; cx < maxXOk; cx++)
                                    {
                                        int testX = cx + w + offsX + italicOffset;

                                        if (testX >= width)
                                        {
                                            break;
                                        }

                                        int block = testY * width + testX;

                                        if (pData[block] == 0 && pData[block] != blackColor)
                                        {
                                            int startX = cx > 0 ? -1 : 0;
                                            int startY = cy > 0 ? -1 : 0;
                                            int endX = cx < dw - 1 ? 2 : 1;
                                            int endY = cy < dh - 1 ? 2 : 1;

                                            if (endX == 2 && testX + 1 >= width)
                                            {
                                                endX--;
                                            }

                                            bool passed = false;

                                            for (int x = startX; x < endX; x++)
                                            {
                                                int nowX = testX + x;

                                                for (int y = startY; y < endY; y++)
                                                {
                                                    int testBlock = (testY + y) * width + nowX;

                                                    if (testBlock < 0)
                                                    {
                                                        continue;
                                                    }

                                                    if (testBlock < pData.Length && pData[testBlock] != 0 && pData[testBlock] != blackColor)
                                                    {
                                                        pData[block] = blackColor;
                                                        passed = true;

                                                        break;
                                                    }
                                                }

                                                if (passed)
                                                {
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            w += dw + offsX + (isSolid ? 1 : 0);
                        }
                        else if (si == ' ')
                        {
                            w += UNICODE_SPACE_WIDTH;

                            if (IsUsingHTML)
                            {
                                isUnderline = (dataPtr.Flags & UOFONT_UNDERLINE) != 0;

                                if (dataPtr.Color != 0xFFFFFFFF)
                                {
                                    charcolor = HuesHelper.RgbaToArgb(dataPtr.Color);

                                    isBlackPixel = ((charcolor >> 0) & 0xFF) <= 8 && ((charcolor >> 8) & 0xFF) <= 8 && ((charcolor >> 16) & 0xFF) <= 8;
                                }
                            }
                        }

                        if (isUnderline)
                        {
                            int minXOk = tmpW + offsX > 0 ? -1 : 0;
                            int maxXOk = w + offsX + dw < width ? 1 : 0;
                            byte* aData = (byte*)((IntPtr)table + (int)table[(byte)'a']);
                            int testY = lineOffsY + (sbyte)aData[1] + (sbyte)aData[3];

                            if (testY >= height)
                            {
                                break;
                            }

                            if (testY < 0)
                            {
                                testY = 0;
                            }

                            for (int cx = minXOk; cx < dw + maxXOk; cx++)
                            {
                                int testX = cx + tmpW + offsX + (isSolid ? 1 : 0);

                                if (testX >= width)
                                {
                                    break;
                                }

                                int block = testY * width + testX;
                                pData[block] = charcolor;
                            }
                        }
                    }

                    lineOffsY += ptr.MaxHeight;
                    ptr = ptr.Next;
                    info.Data.Clear();
                    info.Data.Count = 0;
                    info = null;
                }

                if (IsUsingHTML && _htmlStatus.IsHtmlBackgroundColored && _htmlStatus.BackgroundColor != 0)
                {
                    _htmlStatus.BackgroundColor |= 0xFF;

                    uint hue = HuesHelper.RgbaToArgb(_htmlStatus.BackgroundColor);

                    for (int y = 0; y < height; y++)
                    {
                        int yPos = y * width;

                        for (int x = 0; x < width; x++)
                        {
                            ref uint p = ref pData[yPos + x];

                            if (p == 0)
                            {
                                p = hue;
                            }
                        }
                    }
                }

                if (texture == null || texture.IsDisposed)
                {
                    texture = new FontTexture(width, height, linesCount, links);
                }
                else
                {
                    texture.Links.Clear();
                    texture.Links.AddRange(links);
                    texture.LineCount = linesCount;
                }

                texture.SetData(pData, 0, width * height);

                if (saveHitmap)
                {
                    ulong b = (ulong)(str.GetHashCode() ^ color ^ ((int)align) ^ ((int)flags) ^ font ^ 0x01);
                    picker.Set(b, width, height, pData);
                }
            }
            finally
            {
                System.Buffers.ArrayPool<uint>.Shared.Return(pData, true);
            }
        }

        private unsafe MultilinesFontInfo GetInfoHTML
        (
            byte font,
            string str,
            int len,
            TEXT_ALIGN_TYPE align,
            ushort flags,
            int width
        )
        {
            if (len <= 0)
            {
                return null;
            }

            HTMLChar* htmlData = stackalloc HTMLChar[len];
                
            GetHTMLData
            (
                htmlData,
                font,
                str,
                ref len,
                align,
                flags
            );

            if (len <= 0)
            {
                return null;
            }

            MultilinesFontInfo info = new MultilinesFontInfo();
            info.Reset();
            info.Align = align;
            MultilinesFontInfo ptr = info;
            int indentionOffset = 0;
            ptr.IndentionOffset = indentionOffset;
            int charCount = 0;
            int lastSpace = 0;
            int readWidth = 0;
            bool isFixed = (flags & UOFONT_FIXED) != 0;
            bool isCropped = (flags & UOFONT_CROPPED) != 0;

            if (len != 0)
            {
                ptr.Align = htmlData[0].Align;
            }

            for (int i = 0; i < len; i++)
            {
                char si = htmlData[i].Char;

                uint* table = (uint*) _unicodeFontAddress[htmlData[i].Font];

                if (si == 0x000D || si == '\n')
                {
                    if (si == 0x000D || isFixed || isCropped)
                    {
                        si = (char) 0;
                    }
                    else
                    {
                        si = '\n';
                    }
                }

                if ((table[si] == 0 || table[si] == 0xFFFFFFFF) && si != ' ' && si != '\n')
                {
                    continue;
                }

                byte* data = (byte*) ((IntPtr) table + (int) table[si]);

                if (si == ' ')
                {
                    lastSpace = i;
                    ptr.Width += readWidth;
                    readWidth = 0;
                    ptr.CharCount += charCount;
                    charCount = 0;
                }

                int solidWidth = htmlData[i].Flags & UOFONT_SOLID;

                if (ptr.Width + readWidth + (sbyte) data[0] + (sbyte) data[2] + solidWidth > width || si == '\n')
                {
                    if (lastSpace == ptr.CharStart && lastSpace == 0 && si != '\n')
                    {
                        ptr.CharStart = 1;
                    }

                    if (si == '\n')
                    {
                        ptr.Width += readWidth;
                        ptr.CharCount += charCount;
                        lastSpace = i;

                        if (ptr.Width <= 0)
                        {
                            ptr.Width = 1;
                        }

                        ptr.MaxHeight = MAX_HTML_TEXT_HEIGHT;
                        ptr.Data.Resize((uint) ptr.CharCount);
                        MultilinesFontInfo newptr = new MultilinesFontInfo();
                        newptr.Reset();
                        ptr.Next = newptr;
                        ptr = newptr;

                        ptr.Align = htmlData[i].Align;

                        ptr.CharStart = i + 1;
                        readWidth = 0;
                        charCount = 0;
                        indentionOffset = 0;
                        ptr.IndentionOffset = indentionOffset;

                        continue;
                    }

                    if (lastSpace + 1 == ptr.CharStart && !isFixed && !isCropped)
                    {
                        ptr.Width += readWidth;
                        ptr.CharCount += charCount;

                        if (ptr.Width <= 0)
                        {
                            ptr.Width = 1;
                        }

                        ptr.MaxHeight = MAX_HTML_TEXT_HEIGHT;
                        MultilinesFontInfo newptr = new MultilinesFontInfo();
                        newptr.Reset();
                        ptr.Next = newptr;
                        ptr = newptr;

                        ptr.Align = htmlData[i].Align;

                        ptr.CharStart = i;
                        lastSpace = i - 1;
                        charCount = 0;

                        if (ptr.Align == TEXT_ALIGN_TYPE.TS_LEFT && (htmlData[i].Flags & UOFONT_INDENTION) != 0)
                        {
                            indentionOffset = 14;
                        }

                        ptr.IndentionOffset = indentionOffset;
                        readWidth = indentionOffset;
                    }
                    else
                    {
                        if (isFixed)
                        {
                            MultilinesFontData mfd1 = new MultilinesFontData
                            (
                                htmlData[i].Color,
                                htmlData[i].Flags,
                                htmlData[i].Font,
                                si,
                                htmlData[i].LinkID
                            );

                            ptr.Data.Add(mfd1);
                            readWidth += (sbyte) data[0] + (sbyte) data[2] + 1;
                            ptr.MaxHeight = MAX_HTML_TEXT_HEIGHT;
                            charCount++;
                            ptr.Width += readWidth;
                            ptr.CharCount += charCount;
                        }

                        i = lastSpace + 1;

                        if (i >= len)
                        {
                            break;
                        }

                        si = htmlData[i].Char;

                        solidWidth = htmlData[i].Flags & UOFONT_SOLID;

                        if (ptr.Width <= 0)
                        {
                            ptr.Width = 1;
                        }

                        ptr.MaxHeight = MAX_HTML_TEXT_HEIGHT;
                        ptr.Data.Resize((uint) ptr.CharCount);
                        charCount = 0;

                        if (isFixed || isCropped)
                        {
                            break;
                        }

                        MultilinesFontInfo newptr = new MultilinesFontInfo();
                        newptr.Reset();
                        ptr.Next = newptr;
                        ptr = newptr;

                        ptr.Align = htmlData[i].Align;

                        ptr.CharStart = i;

                        if (ptr.Align == TEXT_ALIGN_TYPE.TS_LEFT && (htmlData[i].Flags & UOFONT_INDENTION) != 0)
                        {
                            indentionOffset = 14;
                        }

                        ptr.IndentionOffset = indentionOffset;
                        readWidth = indentionOffset;
                    }
                }

                MultilinesFontData mfd = new MultilinesFontData
                (
                    htmlData[i].Color,
                    htmlData[i].Flags,
                    htmlData[i].Font,
                    si,
                    htmlData[i].LinkID
                );

                ptr.Data.Add(mfd);

                if (si == ' ')
                {
                    readWidth += UNICODE_SPACE_WIDTH;
                }
                else
                {
                    readWidth += (sbyte) data[0] + (sbyte) data[2] + 1 + solidWidth;
                }

                charCount++;
            }

            ptr.Width += readWidth;
            ptr.CharCount += charCount;
            ptr.MaxHeight = MAX_HTML_TEXT_HEIGHT;

            return info;
        }

        private unsafe void GetHTMLData(HTMLChar* data, byte font, string str, ref int len, TEXT_ALIGN_TYPE align, ushort flags)
        {
            int newlen = 0;

            HTMLDataInfo info = new HTMLDataInfo
            {
                Tag = HTML_TAG_TYPE.HTT_NONE,
                Align = align,
                Flags = flags,
                Font = font,
                Color = _htmlStatus.Color,
                Link = 0
            };

            RawList<HTMLDataInfo> stack = new RawList<HTMLDataInfo>();
            stack.Add(info);
            HTMLDataInfo currentInfo = info;

            for (int i = 0; i < len; i++)
            {
                char si = str[i];

                if (si == '<')
                {
                    bool endTag = false;

                    HTMLDataInfo newInfo = new HTMLDataInfo
                    {
                        Tag = HTML_TAG_TYPE.HTT_NONE,
                        Align = TEXT_ALIGN_TYPE.TS_LEFT,
                        Flags = 0,
                        Font = 0xFF,
                        Color = 0,
                        Link = 0
                    };

                    HTML_TAG_TYPE tag = ParseHTMLTag
                    (
                        str,
                        len,
                        ref i,
                        ref endTag,
                        ref newInfo
                    );

                    if (tag == HTML_TAG_TYPE.HTT_NONE)
                    {
                        continue;
                    }

                    if (!endTag)
                    {
                        if (newInfo.Font == 0xFF)
                        {
                            newInfo.Font = stack[stack.Count - 1].Font;
                        }

                        if (tag != HTML_TAG_TYPE.HTT_BODY)
                        {
                            stack.Add(newInfo);
                        }
                        else
                        {
                            stack.Clear();
                            newlen = 0;

                            if (newInfo.Color != 0)
                            {
                                info.Color = newInfo.Color;
                            }

                            stack.Add(info);
                        }
                    }
                    else if (stack.Count > 1)
                    {
                        //int index = -1;

                        for (uint j = stack.Count - 1; j >= 1; j--)
                        {
                            if (stack[j].Tag == tag)
                            {
                                stack.RemoveAt(j); // MAYBE ERROR?

                                break;
                            }
                        }
                    }

                    GetCurrentHTMLInfo(ref stack, ref currentInfo);

                    switch (tag)
                    {
                        case HTML_TAG_TYPE.HTT_LEFT:
                        case HTML_TAG_TYPE.HTT_CENTER:
                        case HTML_TAG_TYPE.HTT_RIGHT:

                            if (newlen != 0)
                            {
                                endTag = true;
                            }

                            goto case HTML_TAG_TYPE.HTT_P;

                        case HTML_TAG_TYPE.HTT_P:

                            if (endTag)
                            {
                                si = '\n';
                            }
                            else
                            {
                                si = (char) 0;
                            }

                            break;

                        case HTML_TAG_TYPE.HTT_BODYBGCOLOR:
                        case HTML_TAG_TYPE.HTT_BR:
                        case HTML_TAG_TYPE.HTT_BQ:
                            si = '\n';

                            break;

                        default:
                            si = (char) 0;

                            break;
                    }
                }

                if (si != 0)
                {
                    ref HTMLChar c = ref data[newlen];

                    c.Char = si;
                    c.Font = currentInfo.Font;
                    c.Align = currentInfo.Align;
                    c.Flags = currentInfo.Flags;
                    c.Color = currentInfo.Color;
                    c.LinkID = currentInfo.Link;

                    ++newlen;
                }
            }

            len = newlen;
        }

        private void GetCurrentHTMLInfo(ref RawList<HTMLDataInfo> list, ref HTMLDataInfo info)
        {
            info.Tag = HTML_TAG_TYPE.HTT_NONE;
            info.Align = TEXT_ALIGN_TYPE.TS_LEFT;
            info.Flags = 0;
            info.Font = 0xFF;
            info.Color = 0;
            info.Link = 0;

            for (int i = 0; i < list.Count; i++)
            {
                ref HTMLDataInfo current = ref list[i];

                switch (current.Tag)
                {
                    case HTML_TAG_TYPE.HTT_NONE:
                        info = current;

                        break;

                    case HTML_TAG_TYPE.HTT_B:
                    case HTML_TAG_TYPE.HTT_I:
                    case HTML_TAG_TYPE.HTT_U:
                    case HTML_TAG_TYPE.HTT_P:
                        info.Flags |= current.Flags;
                        info.Align = current.Align;

                        break;

                    case HTML_TAG_TYPE.HTT_A:
                        info.Flags |= current.Flags;
                        info.Color = current.Color;
                        info.Link = current.Link;

                        break;

                    case HTML_TAG_TYPE.HTT_BIG:
                    case HTML_TAG_TYPE.HTT_SMALL:

                        if (current.Font != 0xFF && _unicodeFontAddress[current.Font] != IntPtr.Zero)
                        {
                            info.Font = current.Font;
                        }

                        break;

                    case HTML_TAG_TYPE.HTT_BASEFONT:

                        if (current.Font != 0xFF && _unicodeFontAddress[current.Font] != IntPtr.Zero)
                        {
                            info.Font = current.Font;
                        }

                        if (current.Color != 0)
                        {
                            info.Color = current.Color;
                        }

                        break;

                    case HTML_TAG_TYPE.HTT_H1:
                    case HTML_TAG_TYPE.HTT_H2:
                    case HTML_TAG_TYPE.HTT_H4:
                    case HTML_TAG_TYPE.HTT_H5:
                        info.Flags |= current.Flags;
                        goto case HTML_TAG_TYPE.HTT_H3;

                    case HTML_TAG_TYPE.HTT_H3:
                    case HTML_TAG_TYPE.HTT_H6:

                        if (current.Font != 0xFF && _unicodeFontAddress[current.Font] != IntPtr.Zero)
                        {
                            info.Font = current.Font;
                        }

                        break;

                    case HTML_TAG_TYPE.HTT_BQ:
                        info.Color = current.Color;
                        info.Flags |= current.Flags;

                        break;

                    case HTML_TAG_TYPE.HTT_LEFT:
                    case HTML_TAG_TYPE.HTT_CENTER:
                    case HTML_TAG_TYPE.HTT_RIGHT:
                        info.Align = current.Align;

                        break;

                    case HTML_TAG_TYPE.HTT_DIV:
                        info.Align = current.Align;

                        break;
                }
            }
        }

        private HTML_TAG_TYPE ParseHTMLTag(string str, int len, ref int i, ref bool endTag, ref HTMLDataInfo info)
        {
            HTML_TAG_TYPE tag = HTML_TAG_TYPE.HTT_NONE;
            i++;

            if (i < len && str[i] == '/')
            {
                endTag = true;
                i++;
            }

            while (i < len && str[i] == ' ')
            {
                i++;
            }

            int j = i;

            for (; i < len; i++)
            {
                // special case for single <{TAG}/>
                if (str[i] == '/')
                {
                    endTag = true;

                    break;
                }

                if (str[i] == ' ' || str[i] == '>')
                {
                    break;
                }
            }

            if (j != i && i < len)
            {
                int cmdLen = i - j;

                int startIndex = j;

                j = i;

                while (i < len && str[i] != '>')
                {
                    i++;
                }


                if (string.Compare(str, startIndex, "b", 0, cmdLen, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    tag = HTML_TAG_TYPE.HTT_B;
                }
                else if (string.Compare(str, startIndex, "i", 0, cmdLen, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    tag = HTML_TAG_TYPE.HTT_I;
                }
                else if (string.Compare(str, startIndex, "a", 0, cmdLen, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    tag = HTML_TAG_TYPE.HTT_A;
                }
                else if (string.Compare(str, startIndex, "u", 0, cmdLen, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    tag = HTML_TAG_TYPE.HTT_U;
                }
                else if (string.Compare(str, startIndex, "p", 0, cmdLen, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    tag = HTML_TAG_TYPE.HTT_P;
                }
                else if (string.Compare(str, startIndex, "big", 0, cmdLen, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    tag = HTML_TAG_TYPE.HTT_BIG;
                }
                else if (string.Compare(str, startIndex, "small", 0, cmdLen, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    tag = HTML_TAG_TYPE.HTT_SMALL;
                }
                else if (string.Compare(str, startIndex, "body", 0, cmdLen, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    tag = HTML_TAG_TYPE.HTT_BODY;
                }
                else if (string.Compare(str, startIndex, "basefont", 0, cmdLen, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    tag = HTML_TAG_TYPE.HTT_BASEFONT;
                }
                else if (string.Compare(str, startIndex, "h1", 0, cmdLen, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    tag = HTML_TAG_TYPE.HTT_H1;
                }
                else if (string.Compare(str, startIndex, "h2", 0, cmdLen, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    tag = HTML_TAG_TYPE.HTT_H2;
                }
                else if (string.Compare(str, startIndex, "h3", 0, cmdLen, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    tag = HTML_TAG_TYPE.HTT_H3;
                }
                else if (string.Compare(str, startIndex, "h4", 0, cmdLen, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    tag = HTML_TAG_TYPE.HTT_H4;
                }
                else if (string.Compare(str, startIndex, "h5", 0, cmdLen, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    tag = HTML_TAG_TYPE.HTT_H5;
                }
                else if (string.Compare(str, startIndex, "h6", 0, cmdLen, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    tag = HTML_TAG_TYPE.HTT_H6;
                }
                else if (string.Compare(str, startIndex, "br", 0, cmdLen, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    tag = HTML_TAG_TYPE.HTT_BR;
                }
                else if (string.Compare(str, startIndex, "bq", 0, cmdLen, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    tag = HTML_TAG_TYPE.HTT_BQ;
                }
                else if (string.Compare(str, startIndex, "left", 0, cmdLen, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    tag = HTML_TAG_TYPE.HTT_LEFT;
                }
                else if (string.Compare(str, startIndex, "center", 0, cmdLen, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    tag = HTML_TAG_TYPE.HTT_CENTER;
                }
                else if (string.Compare(str, startIndex, "right", 0, cmdLen, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    tag = HTML_TAG_TYPE.HTT_RIGHT;
                }
                else if (string.Compare(str, startIndex, "div", 0, cmdLen, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    tag = HTML_TAG_TYPE.HTT_DIV;
                }
                else
                {
                    if (str.IndexOf("bodybgcolor", StringComparison.InvariantCultureIgnoreCase) >= 0)
                    {
                        tag = HTML_TAG_TYPE.HTT_BODYBGCOLOR;
                        j = str.IndexOf("bgcolor", StringComparison.InvariantCultureIgnoreCase);
                        endTag = false;
                    }
                    else if (str.IndexOf("basefont", StringComparison.InvariantCultureIgnoreCase) >= 0)
                    {
                        tag = HTML_TAG_TYPE.HTT_BASEFONT;
                        j = str.IndexOf("color", StringComparison.InvariantCultureIgnoreCase);
                        endTag = false;
                    }
                    else if (str.IndexOf("bodytext", StringComparison.InvariantCultureIgnoreCase) >= 0)
                    {
                        tag = HTML_TAG_TYPE.HTT_BODY;
                        j = str.IndexOf("text", StringComparison.InvariantCultureIgnoreCase);
                        endTag = false;
                    }
                    else
                    {
                        Log.Warn($"Unhandled HTML param:\t{str}");
                    }
                }

                if (!endTag)
                {
                    GetHTMLInfoFromTag(tag, ref info);

                    if (i < len && j != i)
                    {
                        switch (tag)
                        {
                            case HTML_TAG_TYPE.HTT_BODYBGCOLOR:
                            case HTML_TAG_TYPE.HTT_BODY:
                            case HTML_TAG_TYPE.HTT_BASEFONT:
                            case HTML_TAG_TYPE.HTT_A:
                            case HTML_TAG_TYPE.HTT_DIV:
                            case HTML_TAG_TYPE.HTT_P:
                                cmdLen = i - j;

                                if (str.Length != 0 && str.Length > j && str.Length >= cmdLen)
                                {
                                    GetHTMLInfoFromContent(ref info, str, j, cmdLen);
                                }

                                break;
                        }
                    }
                }
            }

            return tag;
        }

        
        private unsafe void GetHTMLInfoFromContent(ref HTMLDataInfo info, string content, int start, int length)
        {
            int i = 0;

            if (!string.IsNullOrEmpty(content))
            {
                while (i < length && char.IsWhiteSpace(content[i + start]))
                {
                    ++i;
                }
            }
            else
            {
                return;
            }

            char* bufferCmd = stackalloc char[128];
            char* bufferValue = stackalloc char[128];
            
            for (int cmdLenght = 0; i < length; ++i)
            {
                char c = content[i + start];

                bufferCmd[cmdLenght++] = char.IsLetter(c) ? char.ToLowerInvariant(c) : c;

                if (c == ' ' || c == '=' || c == '\\')
                {
                    ++i;
                    bool inside = false;
                    int valueLength = 0;
                    for (; i < length; ++i)
                    {
                        c = content[i + start];

                        if (c == ' ' || c == '\\' || c == '<' || c == '>' || (c == '=' && !inside))
                        {
                            break;
                        }

                        if (c != '"')
                        {
                            bufferValue[valueLength++] = char.IsLetter(c) ? char.ToLowerInvariant(c) : c;
                        }
                        else
                        {
                            inside = !inside;
                        }
                    }

                    if (valueLength != 0)
                    {
                        switch (info.Tag)
                        {
                            case HTML_TAG_TYPE.HTT_BODY:
                            case HTML_TAG_TYPE.HTT_BODYBGCOLOR:
                                
                                if (StringHelper.UnsafeCompare(bufferCmd, "text", cmdLenght))
                                {
                                    ReadColorFromTextBuffer(bufferValue, valueLength, ref info.Color);
                                }
                                else if (StringHelper.UnsafeCompare(bufferCmd, "bgcolor", cmdLenght))
                                {
                                    if (_htmlStatus.IsHtmlBackgroundColored)
                                    {
                                        ReadColorFromTextBuffer(bufferValue, valueLength, ref _htmlStatus.BackgroundColor);
                                    }
                                }
                                else if (StringHelper.UnsafeCompare(bufferCmd, "link", cmdLenght))
                                {
                                    ReadColorFromTextBuffer(bufferValue, valueLength, ref _htmlStatus.WebLinkColor);
                                }
                                else if (StringHelper.UnsafeCompare(bufferCmd, "vlink", cmdLenght))
                                {
                                    ReadColorFromTextBuffer(bufferValue, valueLength, ref _htmlStatus.VisitedWebLinkColor);
                                }
                                else if (StringHelper.UnsafeCompare(bufferCmd, "leftmargin", cmdLenght))
                                {
                                    _htmlStatus.Margins.X = int.Parse(new string(bufferValue, 0, valueLength));
                                }
                                else if (StringHelper.UnsafeCompare(bufferCmd, "topmargin", cmdLenght))
                                {
                                    _htmlStatus.Margins.Y = int.Parse(new string(bufferValue, 0, valueLength));
                                }
                                else if (StringHelper.UnsafeCompare(bufferCmd, "rightmargin", cmdLenght))
                                {
                                    _htmlStatus.Margins.Width = int.Parse(new string(bufferValue, 0, valueLength));
                                }
                                else if (StringHelper.UnsafeCompare(bufferCmd, "bottommargin", cmdLenght))
                                {
                                    _htmlStatus.Margins.Height = int.Parse(new string(bufferValue, 0, valueLength));
                                }

                                break;

                            case HTML_TAG_TYPE.HTT_BASEFONT:

                                if (StringHelper.UnsafeCompare(bufferCmd, "color", cmdLenght))
                                {
                                    ReadColorFromTextBuffer(bufferValue, valueLength, ref info.Color);
                                }
                                else if (StringHelper.UnsafeCompare(bufferCmd, "size", cmdLenght))
                                {
                                    byte font = byte.Parse(new string(bufferValue, 0, valueLength));

                                    if (font == 0 || font == 4)
                                    {
                                        info.Font = 1;
                                    }
                                    else if (font < 4)
                                    {
                                        info.Font = 2;
                                    }
                                    else
                                    {
                                        info.Font = 0;
                                    }
                                }

                                break;

                            case HTML_TAG_TYPE.HTT_A:

                                if (StringHelper.UnsafeCompare(bufferCmd, "href", cmdLenght))
                                {
                                    info.Flags = UOFONT_UNDERLINE;
                                    info.Color = _htmlStatus.WebLinkColor;
                                    info.Link = GetWebLinkID(bufferValue, valueLength, ref info.Color);
                                }

                                break;

                            case HTML_TAG_TYPE.HTT_P:
                            case HTML_TAG_TYPE.HTT_DIV:

                                if (StringHelper.UnsafeCompare(bufferCmd, "align", cmdLenght))
                                {
                                    if (StringHelper.UnsafeCompare(bufferValue, "left", valueLength))
                                    {
                                        info.Align = TEXT_ALIGN_TYPE.TS_LEFT;
                                    }
                                    else if (StringHelper.UnsafeCompare(bufferValue, "center", valueLength))
                                    {
                                        info.Align = TEXT_ALIGN_TYPE.TS_CENTER;
                                    }
                                    else if (StringHelper.UnsafeCompare(bufferValue, "right", valueLength))
                                    {
                                        info.Align = TEXT_ALIGN_TYPE.TS_RIGHT;
                                    }
                                }

                                break;
                        }
                    }

                    cmdLenght = 0;
                }
            }
        }

        private unsafe ushort GetWebLinkID(char* link, int linkLength, ref uint color)
        {
            foreach (KeyValuePair<ushort, WebLink> ll in _webLinks)
            {
                if (ll.Value.Link.Length == linkLength && StringHelper.UnsafeCompare(link, ll.Value.Link, linkLength))
                {
                    if (ll.Value.IsVisited)
                    {
                        color = _htmlStatus.VisitedWebLinkColor;
                    }

                    return ll.Key;
                }
            }

            ushort linkID = (ushort)(_webLinks.Count + 1);

            if (!_webLinks.TryGetValue(linkID, out WebLink webLink))
            {
                webLink = new WebLink();
                webLink.IsVisited = false;
                webLink.Link = new string(link, 0, linkLength);

                _webLinks[linkID] = webLink;
            }

            return linkID;
        }

        public bool GetWebLink(ushort link, out WebLink result)
        {
            if (!_webLinks.TryGetValue(link, out result))
            {
                return false;
            }

            result.IsVisited = true;

            return true;
        }


        private unsafe void ReadColorFromTextBuffer(char* buffer, int length, ref uint color)
        {
            color = 0x00_00_00_00;

            if (length > 0)
            {
                if (buffer[0] == '#')
                {
                    if (length > 1)
                    {
                        int startIndex = buffer[1] == '0' && buffer[2] == 'x' ? 3 : 1;

                        string temp = new string(buffer, startIndex, length - startIndex);
                        uint.TryParse(temp, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var cc);

                        byte* clrbuf = (byte*)&cc;
                        color = (uint)((clrbuf[0] << 24) | (clrbuf[1] << 16) | (clrbuf[2] << 8) | 0xFF);
                    }
                }
                else if (char.IsNumber(buffer[0]))
                {
                    color = Convert.ToUInt32(new string(buffer, 0, length), 16);
                }
                else
                {
                    if (StringHelper.UnsafeCompare(buffer, "red", length))
                    {
                        color = 0x0000FFFF;
                    }
                    else if (StringHelper.UnsafeCompare(buffer, "cyan", length))
                    {
                        color = 0xFFFF00FF;
                    }
                    else if (StringHelper.UnsafeCompare(buffer, "blue", length))
                    {
                        color = 0xFF0000FF;
                    }
                    else if (StringHelper.UnsafeCompare(buffer, "darkblue", length))
                    {
                        color = 0xA00000FF;
                    }
                    else if (StringHelper.UnsafeCompare(buffer, "lightblue", length))
                    {
                        color = 0xE6D8ADFF;
                    }
                    else if (StringHelper.UnsafeCompare(buffer, "purple", length))
                    {
                        color = 0x800080FF;
                    }
                    else if (StringHelper.UnsafeCompare(buffer, "yellow", length))
                    {
                        color = 0x00FFFFFF;
                    }
                    else if (StringHelper.UnsafeCompare(buffer, "lime", length))
                    {
                        color = 0x00FF00FF;
                    }
                    else if (StringHelper.UnsafeCompare(buffer, "magenta", length))
                    {
                        color = 0xFF00FFFF;
                    }
                    else if (StringHelper.UnsafeCompare(buffer, "white", length))
                    {
                        color = 0xFFFEFEFF;
                    }
                    else if (StringHelper.UnsafeCompare(buffer, "silver", length))
                    {
                        color = 0xC0C0C0FF;
                    }
                    else if (StringHelper.UnsafeCompare(buffer, "gray", length) || StringHelper.UnsafeCompare(buffer, "grey", length))
                    {
                        color = 0x808080FF;
                    }
                    else if (StringHelper.UnsafeCompare(buffer, "black", length))
                    {
                        color = 0x010101FF;
                    }
                    else if (StringHelper.UnsafeCompare(buffer, "orange", length))
                    {
                        color = 0x00A5FFFF;
                    }
                    else if (StringHelper.UnsafeCompare(buffer, "brown", length))
                    {
                        color = 0x2A2AA5FF;
                    }
                    else if (StringHelper.UnsafeCompare(buffer, "maroon", length))
                    {
                        color = 0x000080FF;
                    }
                    else if (StringHelper.UnsafeCompare(buffer, "green", length))
                    {
                        color = 0x008000FF;
                    }
                    else if (StringHelper.UnsafeCompare(buffer, "olive", length))
                    {
                        color = 0x008080FF;
                    }
                }
            }
        }

        private void GetHTMLInfoFromTag(HTML_TAG_TYPE tag, ref HTMLDataInfo info)
        {
            info.Tag = tag;
            info.Align = TEXT_ALIGN_TYPE.TS_LEFT;
            info.Flags = 0;
            info.Font = 0xFF;
            info.Color = 0;
            info.Link = 0;

            switch (tag)
            {
                case HTML_TAG_TYPE.HTT_B:
                    info.Flags = UOFONT_SOLID;

                    break;

                case HTML_TAG_TYPE.HTT_I:
                    info.Flags = UOFONT_ITALIC;

                    break;

                case HTML_TAG_TYPE.HTT_U:
                    info.Flags = UOFONT_UNDERLINE;

                    break;

                case HTML_TAG_TYPE.HTT_P:
                    info.Flags = UOFONT_INDENTION;

                    break;

                case HTML_TAG_TYPE.HTT_BIG:
                    info.Font = 0;

                    break;

                case HTML_TAG_TYPE.HTT_SMALL:
                    info.Font = 2;

                    break;

                case HTML_TAG_TYPE.HTT_H1:
                    info.Flags = UOFONT_SOLID | UOFONT_UNDERLINE;
                    info.Font = 0;

                    break;

                case HTML_TAG_TYPE.HTT_H2:
                    info.Flags = UOFONT_SOLID;
                    info.Font = 0;

                    break;

                case HTML_TAG_TYPE.HTT_H3:
                    info.Font = 0;

                    break;

                case HTML_TAG_TYPE.HTT_H4:
                    info.Flags = UOFONT_SOLID;
                    info.Font = 2;

                    break;

                case HTML_TAG_TYPE.HTT_H5:
                    info.Flags = UOFONT_ITALIC;
                    info.Font = 2;

                    break;

                case HTML_TAG_TYPE.HTT_H6:
                    info.Font = 2;

                    break;

                case HTML_TAG_TYPE.HTT_BQ:
                    info.Flags = UOFONT_BQ;
                    info.Color = 0x008000FF;

                    break;

                case HTML_TAG_TYPE.HTT_LEFT:
                    info.Align = TEXT_ALIGN_TYPE.TS_LEFT;

                    break;

                case HTML_TAG_TYPE.HTT_CENTER:
                    info.Align = TEXT_ALIGN_TYPE.TS_CENTER;

                    break;

                case HTML_TAG_TYPE.HTT_RIGHT:
                    info.Align = TEXT_ALIGN_TYPE.TS_RIGHT;

                    break;
            }
        }

        private int GetHeightUnicode(MultilinesFontInfo info)
        {
            int textHeight = 0;

            for (; info != null; info = info.Next)
            {
                if (IsUsingHTML)
                {
                    textHeight += MAX_HTML_TEXT_HEIGHT;
                }
                else
                {
                    textHeight += info.MaxHeight;
                }
            }

            return textHeight;
        }

        public int GetHeightUnicode(byte font, string str, int width, TEXT_ALIGN_TYPE align, ushort flags)
        {
            if (font >= 20 || _unicodeFontAddress[font] == IntPtr.Zero || string.IsNullOrEmpty(str))
            {
                return 0;
            }

            if (width <= 0)
            {
                width = GetWidthUnicode(font, str);
            }

            MultilinesFontInfo info = GetInfoUnicode
            (
                font,
                str,
                str.Length,
                align,
                flags,
                width
            );

            int textHeight = 0;

            while (info != null)
            {
                if (IsUsingHTML)
                {
                    textHeight += MAX_HTML_TEXT_HEIGHT;
                }
                else
                {
                    textHeight += info.MaxHeight;
                }

                MultilinesFontInfo ptr = info;
                info = info.Next;
                ptr.Data.Clear();
                ptr.Data.Count = 0;
                ptr = null;
            }

            return textHeight;
        }

        public unsafe int CalculateCaretPosUnicode
        (
            byte font,
            string str,
            int x,
            int y,
            int width,
            TEXT_ALIGN_TYPE align,
            ushort flags
        )
        {
            if (x < 0 || y < 0 || font >= 20 || _unicodeFontAddress[font] == IntPtr.Zero || string.IsNullOrEmpty(str))
            {
                switch (align)
                {
                    case TEXT_ALIGN_TYPE.TS_CENTER: return width >> 1;

                    case TEXT_ALIGN_TYPE.TS_RIGHT: return width;

                    default: return 0;
                }
            }

            if (width == 0)
            {
                width = GetWidthUnicode(font, str);
            }

            if (x >= width)
            {
                return str.Length;
            }

            MultilinesFontInfo info = GetInfoUnicode
            (
                font,
                str,
                str.Length,
                align,
                flags,
                width
            );

            if (info == null)
            {
                switch (align)
                {
                    case TEXT_ALIGN_TYPE.TS_CENTER: return width >> 1;

                    case TEXT_ALIGN_TYPE.TS_RIGHT: return width;

                    default: return 0;
                }
            }

            int height = 0;
            uint* table = (uint*) _unicodeFontAddress[font];
            int pos = 0;
            bool found = false;

            int fwidth = width;

            while (info != null)
            {
                height += info.MaxHeight;

                switch (info.Align)
                {
                    case TEXT_ALIGN_TYPE.TS_CENTER:
                        width = (fwidth - info.Width) >> 1;

                        if (width < 0)
                        {
                            width = 0;
                        }

                        break;

                    case TEXT_ALIGN_TYPE.TS_RIGHT:
                        width = fwidth;

                        break;

                    default:
                        width = 0;

                        break;
                }

                if (!found)
                {
                    if (y < height)
                    {
                        int len = info.CharCount;

                        for (int i = 0; i < len && i < info.Data.Count; i++)
                        {
                            char ch = info.Data[i].Item;

                            uint offset = table[ch];

                            if (ch != '\r' && offset != 0 && offset != 0xFFFFFFFF)
                            {
                                byte* cptr = (byte*) ((IntPtr) table + (int) offset);
                                width += (sbyte) cptr[0] + (sbyte) cptr[2] + 1;
                            }
                            else if (ch == ' ')
                            {
                                width += UNICODE_SPACE_WIDTH;
                            }

                            if (width > x)
                            {
                                break;
                            }

                            pos++;
                        }

                        found = true;
                    }
                    else
                    {
                        pos += info.CharCount;
                        pos++;
                    }
                }

                MultilinesFontInfo ptr = info;
                info = info.Next;
                ptr.Data.Clear();
                ptr.Data.Count = 0;
                ptr = null;
            }

            if (pos > str.Length)
            {
                pos = str.Length;
            }

            return pos;
        }

        public unsafe (int, int) GetCaretPosUnicode
        (
            byte font,
            string str,
            int pos,
            int width,
            TEXT_ALIGN_TYPE align,
            ushort flags
        )
        {
            int x = 0;
            int y = 0;

            switch (align)
            {
                case TEXT_ALIGN_TYPE.TS_CENTER:
                    x = width >> 1;

                    break;

                case TEXT_ALIGN_TYPE.TS_RIGHT:
                    x = width;

                    break;
            }

            if (font >= 20 || _unicodeFontAddress[font] == IntPtr.Zero || string.IsNullOrEmpty(str))
            {
                return (x, y);
            }

            if (width == 0)
            {
                width = GetWidthUnicode(font, str);
            }

            MultilinesFontInfo info = GetInfoUnicode
            (
                font,
                str,
                str.Length,
                align,
                flags,
                width
            );

            if (info == null)
            {
                return (x, y);
            }

            uint* table = (uint*) _unicodeFontAddress[font];

            while (info != null)
            {
                switch (info.Align)
                {
                    case TEXT_ALIGN_TYPE.TS_CENTER:
                        x = (width - info.Width) >> 1;

                        if (x < 0)
                        {
                            x = 0;
                        }

                        break;

                    case TEXT_ALIGN_TYPE.TS_RIGHT:
                        x = width;

                        break;

                    default:
                        x = 0;

                        break;
                }

                int len = info.CharCount;

                if (info.CharStart == pos)
                {
                    return (x, y);
                }

                if (pos <= info.CharStart + len && info.Data.Count >= len)
                {
                    for (int i = 0; i < len; i++)
                    {
                        char ch = info.Data[i].Item;

                        uint offset = table[ch];

                        if (ch != '\r' && offset != 0 && offset != 0xFFFFFFFF)
                        {
                            byte* cptr = (byte*) ((IntPtr) table + (int) offset);
                            x += (sbyte) cptr[0] + (sbyte) cptr[2] + 1;
                        }
                        else if (ch == ' ')
                        {
                            x += UNICODE_SPACE_WIDTH;
                        }

                        if (info.CharStart + i + 1 == pos)
                        {
                            return (x, y);
                        }
                    }
                }
                else
                {
                    x = width;
                }

                if (info.Next != null)
                {
                    y += info.MaxHeight;
                }

                MultilinesFontInfo ptr = info;
                info = info.Next;
                ptr.Data.Clear();
                ptr.Data.Count = 0;
                ptr = null;
            }

            return (x, y);
        }

        public int CalculateCaretPosASCII
        (
            byte font,
            string str,
            int x,
            int y,
            int width,
            TEXT_ALIGN_TYPE align,
            ushort flags
        )
        {
            if (font >= FontCount || x < 0 || y < 0 || string.IsNullOrEmpty(str))
            {
                switch (align)
                {
                    case TEXT_ALIGN_TYPE.TS_CENTER: return width >> 1;

                    case TEXT_ALIGN_TYPE.TS_RIGHT: return width;

                    default: return 0;
                }
            }

            ref FontCharacterData[] fd = ref _fontData[font];

            if (width <= 0)
            {
                width = GetWidthASCII(font, str);
            }

            if (x >= width)
            {
                return str.Length;
            }

            MultilinesFontInfo info = GetInfoASCII
            (
                font,
                str,
                str.Length,
                align,
                flags,
                width
            );

            if (info == null)
            {
                switch (align)
                {
                    case TEXT_ALIGN_TYPE.TS_CENTER: return width >> 1;

                    case TEXT_ALIGN_TYPE.TS_RIGHT: return width;

                    default: return 0;
                }
            }

            int height = 0;
            int pos = 0;
            bool found = false;

            int fwidth = width;

            while (info != null)
            {
                height += info.MaxHeight;

                switch (info.Align)
                {
                    case TEXT_ALIGN_TYPE.TS_CENTER:
                        width = (fwidth - info.Width) >> 1;

                        if (width < 0)
                        {
                            width = 0;
                        }

                        break;

                    case TEXT_ALIGN_TYPE.TS_RIGHT:
                        width = fwidth;

                        break;

                    default:
                        width = 0;

                        break;
                }

                if (!found)
                {
                    if (y < height)
                    {
                        int len = info.CharCount;

                        for (int i = 0; i < len && i < info.Data.Count; i++)
                        {
                            width += fd[GetASCIIIndex(info.Data[i].Item)].Width;

                            if (width > x)
                            {
                                break;
                            }

                            pos++;
                        }

                        found = true;
                    }
                    else
                    {
                        pos += info.CharCount;
                        pos++;
                    }
                }

                MultilinesFontInfo ptr = info;
                info = info.Next;
                ptr.Data.Clear();
                ptr.Data.Count = 0;
                ptr = null;
            }

            if (pos > str.Length)
            {
                pos = str.Length;
            }

            return pos;
        }

        public (int, int) GetCaretPosASCII
        (
            byte font,
            string str,
            int pos,
            int width,
            TEXT_ALIGN_TYPE align,
            ushort flags
        )
        {
            int x = 0;
            int y = 0;

            switch (align)
            {
                case TEXT_ALIGN_TYPE.TS_CENTER:
                    x = width >> 1;

                    break;

                case TEXT_ALIGN_TYPE.TS_RIGHT:
                    x = width;

                    break;
            }

            if (font >= FontCount || string.IsNullOrEmpty(str))
            {
                return (x, y);
            }

            ref FontCharacterData[] fd = ref _fontData[font];

            if (width == 0)
            {
                width = GetWidthASCII(font, str);
            }

            MultilinesFontInfo info = GetInfoASCII
            (
                font,
                str,
                str.Length,
                align,
                flags,
                width
            );

            if (info == null)
            {
                return (x, y);
            }

            while (info != null)
            {
                switch (info.Align)
                {
                    case TEXT_ALIGN_TYPE.TS_CENTER:
                        x = (width - info.Width) >> 1;

                        if (x < 0)
                        {
                            x = 0;
                        }

                        break;

                    case TEXT_ALIGN_TYPE.TS_RIGHT:
                        x = width;

                        break;

                    default:
                        x = 0;

                        break;
                }

                int len = info.CharCount;

                if (info.CharStart == pos)
                {
                    return (x, y);
                }

                if (pos <= info.CharStart + len && info.Data.Count >= len)
                {
                    for (int i = 0; i < len; i++)
                    {
                        x += fd[GetASCIIIndex(info.Data[i].Item)].Width;

                        if (info.CharStart + i + 1 == pos)
                        {
                            return (x, y);
                        }
                    }
                }
                else
                {
                    x = width;
                }

                if (info.Next != null)
                {
                    y += info.MaxHeight;
                }

                MultilinesFontInfo ptr1 = info;
                info = info.Next;
                ptr1.Data.Clear();
                ptr1.Data.Count = 0;
                ptr1 = null;
            }

            return (x, y);
        }

        public int[] GetLinesCharsCountASCII
        (
            byte font,
            string str,
            TEXT_ALIGN_TYPE align,
            ushort flags,
            int width,
            bool countret = false,
            bool countspaces = false
        )
        {
            if (width == 0)
            {
                width = GetWidthASCII(font, str);
            }

            MultilinesFontInfo info = GetInfoASCII
            (
                font,
                str,
                str.Length,
                align,
                flags,
                width,
                countret,
                countspaces
            );

            if (info == null)
            {
                return new int[0];
            }

            MultilinesFontInfo orig = info;
            int count = 0;

            while (info != null)
            {
                info = info.Next;
                count++;
            }

            int[] chars = new int[count];
            count = 0;

            while (orig != null)
            {
                chars[count++] = orig.CharCount;
                orig = orig.Next;
            }

            return chars;
        }

        public int[] GetLinesCharsCountUnicode
        (
            byte font,
            string str,
            TEXT_ALIGN_TYPE align,
            ushort flags,
            int width,
            bool countret = false,
            bool countspaces = false
        )
        {
            if (width == 0)
            {
                width = GetWidthUnicode(font, str);
            }

            MultilinesFontInfo info = GetInfoUnicode
            (
                font,
                str,
                str.Length,
                align,
                flags,
                width,
                countret,
                countspaces
            );

            if (info == null)
            {
                return new int[0];
            }

            MultilinesFontInfo orig = info;
            int count = 0;

            while (info != null)
            {
                count++;
                info = info.Next;
            }

            int[] chars = new int[count];
            count = 0;

            while (orig != null)
            {
                chars[count++] = orig.CharCount;
                orig = orig.Next;
            }

            return chars;
        }
    }

    internal enum TEXT_ALIGN_TYPE
    {
        TS_LEFT = 0,
        TS_CENTER,
        TS_RIGHT
    }

    internal enum HTML_TAG_TYPE
    {
        HTT_NONE = 0,
        HTT_B,
        HTT_I,
        HTT_A,
        HTT_U,
        HTT_P,
        HTT_BIG,
        HTT_SMALL,
        HTT_BODY,
        HTT_BASEFONT,
        HTT_H1,
        HTT_H2,
        HTT_H3,
        HTT_H4,
        HTT_H5,
        HTT_H6,
        HTT_BR,
        HTT_BQ,
        HTT_LEFT,
        HTT_CENTER,
        HTT_RIGHT,
        HTT_DIV,

        HTT_BODYBGCOLOR
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct FontHeader
    {
        public byte Width, Height, Unknown;
    }


    internal struct FontCharacterData
    {
        public FontCharacterData(byte w, byte h, ushort[] data)
        {
            Width = w;
            Height = h;
            Data = data;
        }

        public byte Width, Height;
        public ushort[] Data;
    }

    internal sealed class MultilinesFontInfo
    {
        public TEXT_ALIGN_TYPE Align;
        public int CharCount;
        public int CharStart;
        public RawList<MultilinesFontData> Data = new RawList<MultilinesFontData>();
        public int IndentionOffset;
        public int MaxHeight;
        public MultilinesFontInfo Next;
        public int Width;

        public void Reset()
        {
            Width = 0;
            IndentionOffset = 0;
            MaxHeight = 0;
            CharStart = 0;
            CharCount = 0;
            Align = TEXT_ALIGN_TYPE.TS_LEFT;
            Next = null;
        }
    }

    internal struct MultilinesFontData
    {
        public MultilinesFontData(uint color, ushort flags, byte font, char item, ushort linkid)
        {
            Color = color;
            Flags = flags;
            Font = font;
            Item = item;
            LinkID = linkid;
        }

        public uint Color;
        public ushort Flags;
        public byte Font;
        public char Item;
        public ushort LinkID;
        //public MultilinesFontData Next;
    }

    internal struct WebLinkRect
    {
        public ushort LinkID;
        public Rectangle Bounds;
    }

    internal class WebLink
    {
        public bool IsVisited;
        public string Link;
    }

    internal struct HTMLChar
    {
        public char Char;
        public byte Font;
        public TEXT_ALIGN_TYPE Align;
        public ushort Flags;
        public uint Color;
        public ushort LinkID;
    }

    internal struct HTMLDataInfo
    {
        public HTML_TAG_TYPE Tag;
        public TEXT_ALIGN_TYPE Align;
        public ushort Flags;
        public byte Font;
        public uint Color;
        public ushort Link;
    }
}