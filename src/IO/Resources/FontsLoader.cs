﻿#region license

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

        private static readonly HTMLChar[] _emptyHTML = { };
        private uint _backgroundColor;
        private FontCharacterData[][] _font;
        private bool _HTMLBackgroundCanBeColored;
        private uint _HTMLColor = 0xFFFFFFFF;
        private int _leftMargin, _topMargin, _rightMargin, _bottomMargin;
        private readonly IntPtr[] _unicodeFontAddress = new IntPtr[20];
        private readonly long[] _unicodeFontSize = new long[20];
        private uint _visitedWebLinkColor;
        private uint _webLinkColor;
        private readonly Dictionary<ushort, WebLink> _webLinks = new Dictionary<ushort, WebLink>();
        private readonly int[] offsetCharTable =
        {
            2, 0, 2, 2, 0, 0, 2, 2, 0, 0
        };
        private readonly int[] offsetSymbolTable =
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

                    _font = new FontCharacterData[FontCount][];
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

                        _font[i] = datas;
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
                textLength += _font[font][GetASCIIIndex(c)].Width;
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
                return _font[font][0].Width;
            }

            int index = c - NOPRINT_CHARS;

            if (index < _font[font].Length)
            {
                return _font[font][index].Width;
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
            int height
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
                        saveHitmap
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
                saveHitmap
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

            ref FontCharacterData[] fd = ref _font[font];

            StringBuilder sb = new StringBuilder();

            if (IsUsingHTML)
            {
                int strLen = str.Length;

                GetHTMLData
                (
                    font,
                    str,
                    ref strLen,
                    align,
                    flags
                );

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
                textLength += _font[font][GetASCIIIndex(c)].Width;

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

            return sb.ToString();
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
            bool saveHitmap
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

            ref FontCharacterData[] fd = ref _font[font];

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
                    info = null;
                }

                return;
            }

            int blocksize = height * width;
            uint[] pData = new uint[blocksize];
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
                    byte index = (byte) ptr.Data[i].Item;

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

            texture.PushData(pData);
        }

        public int GetFontOffsetY(byte font, byte index)
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
                        return offsetCharTable[font];
                    }

                    return offsetSymbolTable[font];
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

            ref FontCharacterData[] fd = ref _font[font];
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
            _HTMLColor = htmlStartColor;
            _HTMLBackgroundCanBeColored = backgroundCanBeColored;
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
            int height
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
                        saveHitmap
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
                saveHitmap
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

            StringBuilder sb = new StringBuilder();

            if (IsUsingHTML)
            {
                int strLen = str.Length;

                GetHTMLData
                (
                    font,
                    str,
                    ref strLen,
                    align,
                    flags
                );

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
                    width -= *(byte*) ((IntPtr) table + (int) offset + 2) * 3 + 3;
                }
            }


            int textLength = 0;

            foreach (char c in str)
            {
                uint offset = table[c];
                sbyte charWidth = 0;

                if (offset != 0 && offset != 0xFFFFFFFF)
                {
                    byte* ptr = (byte*) ((IntPtr) table + (int) offset);
                    charWidth = (sbyte) ((sbyte) ptr[0] + (sbyte) ptr[2] + 1);
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

            return sb.ToString();
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
            _webLinkColor = 0xFF0000FF;
            _visitedWebLinkColor = 0x0000FFFF;
            _backgroundColor = 0;
            _leftMargin = 0;
            _topMargin = 0;
            _rightMargin = 0;
            _bottomMargin = 0;

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
            bool saveHitmap
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

            if (IsUsingHTML && (_leftMargin != 0 || _rightMargin != 0))
            {
                while (info != null)
                {
                    MultilinesFontInfo ptr1 = info.Next;
                    info.Data.Clear();
                    info = null;
                    info = ptr1;
                }

                int newWidth = width - (_leftMargin + _rightMargin);

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
                    ptr1 = null;
                }

                return;
            }

            height += _topMargin + _bottomMargin + 4;
            int blocksize = height * width;
            uint[] pData = new uint[blocksize];
            //uint* pData = stackalloc uint[blocksize];
            uint* table = (uint*) _unicodeFontAddress[font];
            int lineOffsY = _topMargin;
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
                int w = _leftMargin;

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
                    table = (uint*) _unicodeFontAddress[dataPtr.Font];

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
                            byte* xData = (byte*) ((IntPtr) table + (int) table[si]);
                            ofsX = (sbyte) xData[2];
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

                    byte* data = (byte*) ((IntPtr) table + (int) table[si]);
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
                        offsX = (sbyte) data[0] + 1;
                        offsY = (sbyte) data[1];
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
                                italicOffset = (int) ((dh - y) / ITALIC_FONT_KOEFFICIENT);
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

                                    byte cl = (byte) (scanlines[c] & (1 << (7 - j)));
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
                                    italicOffset = (int) ((dh - cy) / ITALIC_FONT_KOEFFICIENT);
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
                                    italicOffset = (int) ((dh - cy) / ITALIC_FONT_KOEFFICIENT);
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
                                    italicOffset = (int) ((dh - cy) / ITALIC_FONT_KOEFFICIENT);
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
                        byte* aData = (byte*) ((IntPtr) table + (int) table[(byte) 'a']);
                        int testY = lineOffsY + (sbyte) aData[1] + (sbyte) aData[3];

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
                info = null;
            }

            if (IsUsingHTML && _HTMLBackgroundCanBeColored && _backgroundColor != 0)
            {
                _backgroundColor |= 0xFF;

                for (int y = 0; y < height; y++)
                {
                    int yPos = y * width;

                    for (int x = 0; x < width; x++)
                    {
                        ref uint p = ref pData[yPos + x];

                        if (p == 0)
                        {
                            p = HuesHelper.RgbaToArgb(_backgroundColor);
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

            texture.PushData(pData);
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
            HTMLChar[] htmlData = GetHTMLData
            (
                font,
                str,
                ref len,
                align,
                flags
            );

            if (htmlData.Length == 0)
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

        private HTMLChar[] GetHTMLData(byte font, string str, ref int len, TEXT_ALIGN_TYPE align, ushort flags)
        {
            if (len < 1)
            {
                return _emptyHTML;
            }

            HTMLChar[] data = new HTMLChar[len];
            int newlen = 0;

            HTMLDataInfo info = new HTMLDataInfo
            {
                Tag = HTML_TAG_TYPE.HTT_NONE,
                Align = align,
                Flags = flags,
                Font = font,
                Color = _HTMLColor,
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

                    currentInfo = GetCurrentHTMLInfo(ref stack);

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
                    data[newlen].Char = si;

                    data[newlen].Font = currentInfo.Font;

                    data[newlen].Align = currentInfo.Align;

                    data[newlen].Flags = currentInfo.Flags;

                    data[newlen].Color = currentInfo.Color;

                    data[newlen].LinkID = currentInfo.Link;

                    newlen++;
                }
            }

            Array.Resize(ref data, newlen);
            len = newlen;

            return data;
        }

        private HTMLDataInfo GetCurrentHTMLInfo(ref RawList<HTMLDataInfo> list)
        {
            HTMLDataInfo info = new HTMLDataInfo
            {
                Tag = HTML_TAG_TYPE.HTT_NONE,
                Align = TEXT_ALIGN_TYPE.TS_LEFT,
                Flags = 0,
                Font = 0xFF,
                Color = 0,
                Link = 0
            };

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

            return info;
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

                string cmd = str.Substring(j, cmdLen).ToLower();

                j = i;

                while (i < len && str[i] != '>')
                {
                    i++;
                }

                switch (cmd)
                {
                    case "b":
                        tag = HTML_TAG_TYPE.HTT_B;

                        break;

                    case "i":
                        tag = HTML_TAG_TYPE.HTT_I;

                        break;

                    case "a":
                        tag = HTML_TAG_TYPE.HTT_A;

                        break;

                    case "u":
                        tag = HTML_TAG_TYPE.HTT_U;

                        break;

                    case "p":
                        tag = HTML_TAG_TYPE.HTT_P;

                        break;

                    case "big":
                        tag = HTML_TAG_TYPE.HTT_BIG;

                        break;

                    case "small":
                        tag = HTML_TAG_TYPE.HTT_SMALL;

                        break;

                    case "body":
                        tag = HTML_TAG_TYPE.HTT_BODY;

                        break;

                    case "basefont":
                        tag = HTML_TAG_TYPE.HTT_BASEFONT;

                        break;

                    case "h1":
                        tag = HTML_TAG_TYPE.HTT_H1;

                        break;

                    case "h2":
                        tag = HTML_TAG_TYPE.HTT_H2;

                        break;

                    case "h3":
                        tag = HTML_TAG_TYPE.HTT_H3;

                        break;

                    case "h4":
                        tag = HTML_TAG_TYPE.HTT_H4;

                        break;

                    case "h5":
                        tag = HTML_TAG_TYPE.HTT_H5;

                        break;

                    case "h6":
                        tag = HTML_TAG_TYPE.HTT_H6;

                        break;

                    case "br":
                        tag = HTML_TAG_TYPE.HTT_BR;

                        break;

                    case "bq":
                        tag = HTML_TAG_TYPE.HTT_BQ;

                        break;

                    case "left":
                        tag = HTML_TAG_TYPE.HTT_LEFT;

                        break;

                    case "center":
                        tag = HTML_TAG_TYPE.HTT_CENTER;

                        break;

                    case "right":
                        tag = HTML_TAG_TYPE.HTT_RIGHT;

                        break;

                    case "div":
                        tag = HTML_TAG_TYPE.HTT_DIV;

                        break;

                    default:

                        if (str.Contains("bodybgcolor"))
                        {
                            tag = HTML_TAG_TYPE.HTT_BODYBGCOLOR;
                            j = str.IndexOf("bgcolor", StringComparison.Ordinal);
                        }
                        else
                        {
                            Log.Warn($"Unhandled HTML param:\t{str}");
                        }

                        break;
                }

                if (!endTag)
                {
                    info = GetHTMLInfoFromTag(tag);

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
                                string content = str.Substring(j, cmdLen);

                                if (content.Length != 0)
                                {
                                    GetHTMLInfoFromContent(ref info, content);
                                }

                                //if (tag == HTML_TAG_TYPE.HTT_BODYBGCOLOR)
                                //    i = 1;

                                break;
                        }
                    }
                }
            }

            return tag;
        }

        private void GetHTMLInfoFromContent(ref HTMLDataInfo info, string content)
        {
            string[] strings = content.Split
            (
                new[]
                {
                    ' ', '=', '\\'
                },
                StringSplitOptions.RemoveEmptyEntries
            );


            int size = strings.Length;

            for (int i = 0; i < size; i += 2)
            {
                if (i + 1 >= size)
                {
                    break;
                }

                string str = strings[i].ToLower();

                string value = strings[i + 1];
                TrimHTMLString(ref value);

                if (value.Length == 0)
                {
                    continue;
                }

                switch (info.Tag)
                {
                    case HTML_TAG_TYPE.HTT_BODYBGCOLOR:
                    case HTML_TAG_TYPE.HTT_BODY:

                        switch (str)
                        {
                            case "text":
                                info.Color = GetHTMLColorFromText(ref value);

                                break;

                            case "bgcolor":

                                if (_HTMLBackgroundCanBeColored)
                                {
                                    _backgroundColor = GetHTMLColorFromText(ref value);
                                }

                                break;

                            case "link":
                                _webLinkColor = GetHTMLColorFromText(ref value);

                                break;

                            case "vlink":
                                _visitedWebLinkColor = GetHTMLColorFromText(ref value);

                                break;

                            case "leftmargin":
                                _leftMargin = int.Parse(value);

                                break;

                            case "topmargin":
                                _topMargin = int.Parse(value);

                                break;

                            case "rightmargin":
                                _rightMargin = int.Parse(value);

                                break;

                            case "bottommargin":
                                _bottomMargin = int.Parse(value);

                                break;
                        }

                        break;

                    case HTML_TAG_TYPE.HTT_BASEFONT:

                        if (str == "color")
                        {
                            info.Color = GetHTMLColorFromText(ref value);
                        }
                        else if (str == "size")
                        {
                            byte font = byte.Parse(value);

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
                        //else if (str == "face")
                        //{
                        //    byte face = byte.Parse(value);

                        //    if (face == 1)
                        //        info.Font = 0;
                        //}

                        break;

                    case HTML_TAG_TYPE.HTT_A:

                        if (str == "href")
                        {
                            info.Flags = UOFONT_UNDERLINE;
                            info.Color = _webLinkColor;

                            int start = i + 1;

                            while (value[0] == '"' && value[value.Length - 1] != '"' && start + 1 < size)
                            {
                                value += strings[++start];
                            }

                            i = start;

                            info.Link = GetWebLinkID(value, ref info.Color);
                        }

                        break;

                    case HTML_TAG_TYPE.HTT_P:
                    case HTML_TAG_TYPE.HTT_DIV:

                        if (str == "align")
                        {
                            str = value.ToLower();

                            switch (str)
                            {
                                case "left":
                                    info.Align = TEXT_ALIGN_TYPE.TS_LEFT;

                                    break;

                                case "center":
                                    info.Align = TEXT_ALIGN_TYPE.TS_CENTER;

                                    break;

                                case "right":
                                    info.Align = TEXT_ALIGN_TYPE.TS_RIGHT;

                                    break;
                            }
                        }

                        break;
                }
            }
        }

        private ushort GetWebLinkID(string link, ref uint color)
        {
            ushort linkID = 0;
            KeyValuePair<ushort, WebLink>? l = null;

            foreach (KeyValuePair<ushort, WebLink> ll in _webLinks)
            {
                if (ll.Value.Link == link)
                {
                    l = ll;

                    break;
                }
            }

            if (!l.HasValue)
            {
                linkID = (ushort) (_webLinks.Count + 1);

                _webLinks[linkID] = new WebLink
                {
                    IsVisited = false,
                    Link = link
                };
            }
            else
            {
                if (l.Value.Value.IsVisited)
                {
                    color = _visitedWebLinkColor;
                }

                linkID = l.Value.Key;
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

        private unsafe uint GetHTMLColorFromText(ref string str)
        {
            uint color = 0;

            if (str.Length > 1)
            {
                if (str[0] == '#')
                {
                    int start = 1;

                    if (str[1] == '0' && str[2] == 'x')
                    {
                        start = 3;
                    }

                    uint.TryParse(str.Substring(start), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out color);

                    //color = Convert.ToUInt32(str.Substring(start), 16);

                    byte* clrbuf = (byte*) &color;
                    color = (uint) ((clrbuf[0] << 24) | (clrbuf[1] << 16) | (clrbuf[2] << 8) | 0xFF);
                }
                else if (char.IsNumber(str[0]))
                {
                    color = Convert.ToUInt32(str, 16);
                    //color = (HuesHelper.Color16To32((ushort)color) >> 8) | 0xFF;
                    //byte* clrbuf = (byte*)&color;
                    //color = (uint)((clrbuf[0] << 24) | (clrbuf[1] << 16) | (clrbuf[2] << 8) | 0xFF);

                    ////(byte b, byte g, byte r, byte a) = HuesHelper.GetBGRA(color);
                    //Color cc = new Color()
                    //{
                    //    PackedValue = HuesHelper.RgbaToArgb(color)
                    //};
                }
                else
                {
                    str = str.ToLower();

                    switch (str)
                    {
                        case "red":
                            color = 0x0000FFFF;

                            break;

                        case "cyan":
                            color = 0xFFFF00FF;

                            break;

                        case "blue":
                            color = 0xFF0000FF;

                            break;

                        case "darkblue":
                            color = 0xA00000FF;

                            break;

                        case "lightblue":
                            color = 0xE6D8ADFF;

                            break;

                        case "purple":
                            color = 0x800080FF;

                            break;

                        case "yellow":
                            color = 0x00FFFFFF;

                            break;

                        case "lime":
                            color = 0x00FF00FF;

                            break;

                        case "magenta":
                            color = 0xFF00FFFF;

                            break;

                        case "white":
                            color = 0xFFFEFEFF;

                            break;

                        case "silver":
                            color = 0xC0C0C0FF;

                            break;

                        case "gray":
                        case "grey":
                            color = 0x808080FF;

                            break;

                        case "black":
                            color = 0x010101FF;

                            break;

                        case "orange":
                            color = 0x00A5FFFF;

                            break;

                        case "brown":
                            color = 0x2A2AA5FF;

                            break;

                        case "maroon":
                            color = 0x000080FF;

                            break;

                        case "green":
                            color = 0x008000FF;

                            break;

                        case "olive":
                            color = 0x008080FF;

                            break;
                    }
                }
            }

            return color;
        }

        private void TrimHTMLString(ref string str)
        {
            if (str.Length >= 2 && str[0] == '"' && str[str.Length - 1] == '"')
            {
                str = str.Substring(1, str.Length - 2);
            }
        }

        private HTMLDataInfo GetHTMLInfoFromTag(HTML_TAG_TYPE tag)
        {
            HTMLDataInfo info = new HTMLDataInfo
            {
                Tag = tag,
                Align = TEXT_ALIGN_TYPE.TS_LEFT,
                Flags = 0,
                Font = 0xFF,
                Color = 0,
                Link = 0
            };

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

            return info;
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

            ref FontCharacterData[] fd = ref _font[font];

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

            ref FontCharacterData[] fd = ref _font[font];

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