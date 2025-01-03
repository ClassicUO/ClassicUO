// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.IO;
using ClassicUO.Utility;
using ClassicUO.Utility.Collections;
using ClassicUO.Utility.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ClassicUO.Assets
{
    public sealed class FontsLoader : UOFileLoader
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

        public struct Margin
        {
            public int X, Y, Width, Height;

            public Margin()
            {
                this = default;
            }

            public Margin(int x, int y, int width, int height)
            {
                X = x; Y = y; Width = width; Height = height;
            }


            public readonly int Right => X + Width;
            public readonly int Bottom => Y + Height;

            public readonly bool Contains(int x, int y)
            {
                return (x >= X && x < Right && y >= Y && y < Bottom);
            }

            public static readonly Margin Empty = new Margin();
        }

        struct HtmlStatus
        {
            public uint BackgroundColor;
            public uint VisitedWebLinkColor;
            public uint WebLinkColor;
            public uint Color;
            public Margin Margins;

            public bool IsHtmlBackgroundColored;
        }

        private HtmlStatus _htmlStatus;

        private FontCharacterData[,] _fontDataASCII;
        private FontCharacterDataUnicode[,] _fontDataUNICODE;
        private readonly UOFile[] _unicodeFontAddress = new UOFile[20];
        private readonly long[] _unicodeFontSize = new long[20];
        private readonly Dictionary<ushort, WebLink> _webLinks = new Dictionary<ushort, WebLink>();
        private readonly int[] _offsetCharTable = { 2, 0, 2, 2, 0, 0, 2, 2, 0, 0 };
        private readonly int[] _offsetSymbolTable = { 1, 0, 1, 1, -1, 0, 1, 1, 0, 0 };

        public FontsLoader(UOFileManager fileManager) : base(fileManager) { }


        public int FontCount { get; private set; }

        public bool UnusePartialHue { get; set; } = false;

        public bool RecalculateWidthByInfo { get; set; } = false;

        public bool IsUsingHTML { get; set; }

        public override unsafe void Load()
        {
            UOFileMul fonts = new UOFileMul(FileManager.GetUOFilePath("fonts.mul"));

            for (int i = 0; i < 20; i++)
            {
                string path = FileManager.GetUOFilePath(
                    "unifont" + (i == 0 ? "" : i.ToString()) + ".mul"
                );

                if (File.Exists(path))
                {
                    _unicodeFontAddress[i] = new UOFileMul(path);
                }
            }

            int fontHeaderSize = sizeof(FontHeader);
            FontCount = 0;

            while (fonts.Position < fonts.Length)
            {
                bool exit = false;
                fonts.ReadUInt8();

                for (int i = 0; i < 224; i++)
                {
                    if (fonts.Position + fontHeaderSize >= fonts.Length)
                    {
                        break;
                    }

                    var fh = new FontHeader()
                    {
                        Width = fonts.ReadUInt8(),
                        Height = fonts.ReadUInt8(),
                        Unknown = fonts.ReadUInt8(),
                    };

                    int bcount = fh.Width * fh.Height * 2;

                    if (fonts.Position + bcount > fonts.Length)
                    {
                        exit = true;

                        break;
                    }

                    fonts.Seek(bcount, SeekOrigin.Current);
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

            _fontDataASCII = new FontCharacterData[FontCount, 224];
            fonts.Seek(0, SeekOrigin.Begin);

            for (int i = 0; i < FontCount; i++)
            {
                byte header = fonts.ReadUInt8();

                for (int j = 0; j < 224; j++)
                {
                    if (fonts.Position + 3 >= fonts.Length)
                    {
                        continue;
                    }

                    byte w = fonts.ReadUInt8();
                    byte h = fonts.ReadUInt8();
                    fonts.ReadUInt8();
                    var data = new ushort[w * h];
                    int read = fonts.Read(MemoryMarshal.AsBytes(data.AsSpan()));
                    _fontDataASCII[i, j] = new FontCharacterData(w, h, data);
                }
            }

            if (_unicodeFontAddress[1] == null)
            {
                _unicodeFontAddress[1] = _unicodeFontAddress[0];
                _unicodeFontSize[1] = _unicodeFontSize[0];
            }

            _fontDataUNICODE = new FontCharacterDataUnicode[_unicodeFontAddress.Length, 0x10000];
        }

        private static FontCharacterDataUnicode _nullChar = new FontCharacterDataUnicode();

        private ref FontCharacterDataUnicode GetCharUni(byte font, char c)
        {
            int index = (int)c; /*((int)c & 0xFFFFF) - 0x20*/;

            if (index < 0)
            {
                return ref _nullChar;
            }

            ref var cc = ref _fontDataUNICODE[font, index];

            if (cc.Data == null)
            {
                LoadChar(font, index);

                if (cc.Data == null)
                {
                    return ref _nullChar;
                }
            }

            return ref cc;
        }

        private void LoadChar(byte font, int index)
        {
            var file = _unicodeFontAddress[font];

            if (file == null)
            {
                return;
            }
            file.Seek(index * 4, SeekOrigin.Begin);
            var lookup = file.ReadInt32();

            if (lookup == 0)
            {
                return;
            }

            file.Seek(lookup, SeekOrigin.Begin);

            ref var cc = ref _fontDataUNICODE[font, index];

            cc.OffsetX = file.ReadInt8();
            cc.OffsetY = file.ReadInt8();
            cc.Width = file.ReadInt8();
            cc.Height = file.ReadInt8();

            if (cc.Width > 0 && cc.Height > 0)
            {
                cc.Data = new byte[(((cc.Width - 1) / 8) + 1) * cc.Height];
                file.Read(cc.Data);
                //Span<byte> scanline = stackalloc byte[((cc.Width - 1) / 8) + 1];
                //file.Read(scanline);

                //for (int y = 0 ; y < cc.Height; ++y)
                //{
                //    int bitX = 7;
                //    int byteX = 0;

                //    for (int x = 0; x < cc.Width; ++x)
                //    {
                //        ref var col = ref cc.Data[y * cc.Width + x];

                //        col = 0;
                //        if ((scanline[byteX] & (byte)Math.Pow(2, bitX)) != 0)
                //        {
                //            col = 0xFFFFFFFF;
                //        }

                //        --bitX;

                //        if (bitX < 0)
                //        {
                //            bitX = 7;
                //            ++byteX;
                //        }
                //    }
                //}
            }
        }

        public bool UnicodeFontExists(byte font)
        {
            return font < 20 && _unicodeFontAddress[font] != null;
        }

        /// <summary> Get the index in ASCII fonts of a character. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetASCIIIndex(char c)
        {
            byte ch = (byte)c; // ASCII fonts cover only 256 characters

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
                textLength += _fontDataASCII[font, GetASCIIIndex(c)].Width;
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
                return _fontDataASCII[font, 0].Width;
            }

            int index = c - NOPRINT_CHARS;

            if (index < _fontDataASCII.GetLength(1))
            {
                return _fontDataASCII[font, index].Width;
            }

            return 0;
        }

        public int GetWidthExASCII(
            byte font,
            string text,
            int maxwidth,
            TEXT_ALIGN_TYPE align,
            ushort flags
        )
        {
            if (font > FontCount || string.IsNullOrEmpty(text))
            {
                return 0;
            }

            MultilinesFontInfo info = GetInfoASCII(font, text, text.Length, align, flags, maxwidth);

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

        public int GetHeightASCII(
            byte font,
            string str,
            int width,
            TEXT_ALIGN_TYPE align,
            ushort flags
        )
        {
            if (width == 0)
            {
                width = GetWidthASCII(font, str);
            }

            MultilinesFontInfo info = GetInfoASCII(font, str, str.Length, align, flags, width);

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

        public struct FontInfo
        {
            public uint[] Data;
            public int Width;
            public int Height;

            public int LineCount;
            public FastList<WebLinkRect> Links;

            public static FontInfo Empty = new FontInfo() { Data = null };
        }

        public FontInfo GenerateASCII(
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
                return FontInfo.Empty;
            }

            if (
                (flags & UOFONT_FIXED) != 0
                || (flags & UOFONT_CROPPED) != 0
                || (flags & UOFONT_CROPTEXTURE) != 0
            )
            {
                if (width == 0 || string.IsNullOrEmpty(str))
                {
                    return FontInfo.Empty;
                }

                int realWidth = GetWidthASCII(font, str);

                if (realWidth > width)
                {
                    string newstr = GetTextByWidthASCII(
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
                            totalheight += GetHeightASCII(font, newstr, width, align, flags);

                            if (str.Length > newstr.Length)
                            {
                                newstr += GetTextByWidthASCII(
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

                    return GeneratePixelsASCII(
                        font,
                        newstr,
                        color,
                        width,
                        align,
                        flags,
                        saveHitmap
                    );
                }
            }

            return GeneratePixelsASCII(font, str, color, width, align, flags, saveHitmap);
        }

        public string GetTextByWidthASCII(
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

            int strLen = str.Length;

            Span<char> span = stackalloc char[strLen];
            ValueStringBuilder sb = new ValueStringBuilder(span);

            if (IsUsingHTML)
            {
                unsafe
                {
                    HTMLChar* chars = stackalloc HTMLChar[strLen];

                    GetHTMLData(chars, font, str.AsSpan(), ref strLen, align, flags);
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
                width -= _fontDataASCII[font, '.' - NOPRINT_CHARS].Width * 3;
            }

            int textLength = 0;

            foreach (char c in str)
            {
                textLength += _fontDataASCII[font, GetASCIIIndex(c)].Width;

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

        private unsafe FontInfo GeneratePixelsASCII(
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
                return FontInfo.Empty;
            }

            int len = str.Length;

            if (len == 0)
            {
                return FontInfo.Empty;
            }

            if (width <= 0)
            {
                width = GetWidthASCII(font, str);
            }

            if (width <= 0)
            {
                return FontInfo.Empty;
            }

            MultilinesFontInfo info = GetInfoASCII(font, str, len, align, flags, width);

            if (info == null)
            {
                return FontInfo.Empty;
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

                return FontInfo.Empty;
            }

            int blocksize = height * width;
            uint[] pData = new uint[blocksize]; // System.Buffers.ArrayPool<uint>.Shared.Rent(blocksize);

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

                    var count = ptr.Data.Length;

                    for (int i = 0; i < count; i++)
                    {
                        byte index = (byte)ptr.Data[i].Item;

                        int offsY = GetFontOffsetY(font, index);

                        ref FontCharacterData fcd = ref _fontDataASCII[
                            font,
                            GetASCIIIndex(ptr.Data[i].Item)
                        ];

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
                                        pcl = FileManager.Hues.GetPartialHueColor(
                                            pic,
                                            charColor
                                        );
                                    }
                                    else
                                    {
                                        pcl = FileManager.Hues.GetColor(pic, charColor);
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

                FontInfo fi = new FontInfo();
                fi.LineCount = linesCount;
                fi.Data = pData;
                fi.Width = width;
                fi.Height = height;
                fi.Links = null;

                return fi;
            }
            finally
            {
                //System.Buffers.ArrayPool<uint>.Shared.Return(pData, true);
            }
        }

        private int GetFontOffsetY(byte font, byte index)
        {
            if (index == 0xB8)
            {
                return 1;
            }

            if (
                !(index >= 0x41 && index <= 0x5A)
                && !(index >= 0xC0 && index <= 0xDF)
                && index != 0xA8
            )
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

        public MultilinesFontInfo GetInfoASCII(
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

                if ( /*si == '\r' ||*/
                    si == '\n')
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

                ref FontCharacterData fcd = ref _fontDataASCII[font, GetASCIIIndex(si)];
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

                        ptr.Data.Length = ptr.CharCount - newlineval;

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
                            MultilinesFontData mfd1 = new MultilinesFontData(
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
                        ptr.Data.Length = ptr.CharCount;

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

                MultilinesFontData mfd = new MultilinesFontData(0xFFFFFFFF, flags, font, si, 0);

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

        public void SetUseHTML(
            bool value,
            uint htmlStartColor = 0xFFFFFFFF,
            bool backgroundCanBeColored = false
        )
        {
            IsUsingHTML = value;
            _htmlStatus.Color = htmlStartColor;
            _htmlStatus.IsHtmlBackgroundColored = backgroundCanBeColored;
        }

        public FontInfo GenerateUnicode(
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
                return FontInfo.Empty;
            }

            if (
                (flags & UOFONT_FIXED) != 0
                || (flags & UOFONT_CROPPED) != 0
                || (flags & UOFONT_CROPTEXTURE) != 0
            )
            {
                if (width == 0)
                {
                    return FontInfo.Empty;
                }

                int realWidth = GetWidthUnicode(font, str.AsSpan());

                if (realWidth > width)
                {
                    string newstr = GetTextByWidthUnicode(
                        font,
                        str.AsSpan(),
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
                            totalheight += GetHeightUnicode(font, newstr, width, align, flags);

                            if (str.Length > newstr.Length)
                            {
                                newstr += GetTextByWidthUnicode(
                                    font,
                                    str.AsSpan(0, newstr.Length),
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

                    return GeneratePixelsUnicode(
                        font,
                        newstr,
                        color,
                        cell,
                        width,
                        align,
                        flags,
                        saveHitmap
                    );
                }
            }

            return GeneratePixelsUnicode(font, str, color, cell, width, align, flags, saveHitmap);
        }

        public unsafe string GetTextByWidthUnicode(
            byte font,
            ReadOnlySpan<char> str,
            int width,
            bool isCropped,
            TEXT_ALIGN_TYPE align,
            ushort flags
        )
        {
            if (font >= 20 || _unicodeFontAddress[font] == null || str.IsEmpty)
            {
                return string.Empty;
            }

            int strLen = str.Length;

            Span<char> span = stackalloc char[strLen];
            ValueStringBuilder sb = new ValueStringBuilder(span);

            if (IsUsingHTML)
            {
                unsafe
                {
                    HTMLChar* data = stackalloc HTMLChar[strLen];

                    GetHTMLData(data, font, str, ref strLen, align, flags);
                }

                int size = str.Length - strLen;

                if (size > 0)
                {
                    sb.Append(str.Slice(0, size));
                    str = str.Slice(str.Length - strLen, strLen);

                    if (GetWidthUnicode(font, str) < width)
                    {
                        isCropped = false;
                    }
                }
            }

            if (isCropped)
            {
                ref var @char = ref GetCharUni(font, '.');

                if (@char.Data != null)
                {
                    width -= @char.Width * 3 + 3;
                }
            }

            int textLength = 0;

            foreach (char c in str)
            {
                ref var @char = ref GetCharUni(font, c);
                sbyte charWidth = 0;

                if (@char.Data != null)
                {
                    charWidth = (sbyte)(@char.OffsetX + @char.Width + 1);
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

        public int GetWidthUnicode(byte font, string str)
        {
            if (font >= 20 || _unicodeFontAddress[font] == null || string.IsNullOrEmpty(str))
            {
                return 0;
            }

            return GetWidthUnicode(font, str.AsSpan());
        }

        private int GetWidthUnicode(byte font, ReadOnlySpan<char> str)
        {
            if (font >= 20 || _unicodeFontAddress[font] == null || str.IsEmpty)
            {
                return 0;
            }

            int textLength = 0;
            int maxTextLenght = 0;

            foreach (char c in str)
            {
                ref var @char = ref GetCharUni(font, c);

                if (c != '\r' && @char.Data != null)
                {
                    textLength += (sbyte)(@char.OffsetX + @char.Width + 1);
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

        public int GetCharWidthUnicode(byte font, char c)
        {
            if (font >= 20 || _unicodeFontAddress[font] == null || c == 0 || c == '\r')
            {
                return 0;
            }

            ref var @char = ref GetCharUni(font, c);

            if (@char.Data != null)
            {
                return (sbyte)(@char.OffsetX + @char.Width + 1);
            }

            if (c == ' ')
            {
                return UNICODE_SPACE_WIDTH;
            }

            return 0;
        }

        public int GetWidthExUnicode(
            byte font,
            string text,
            int maxwidth,
            TEXT_ALIGN_TYPE align,
            ushort flags
        )
        {
            if (
                font >= 20 || _unicodeFontAddress[font] == null || string.IsNullOrEmpty(text)
            )
            {
                return 0;
            }

            MultilinesFontInfo info = GetInfoUnicode(
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

            return textWidth + 4;
        }

        public unsafe MultilinesFontInfo GetInfoUnicode(
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
            _htmlStatus.Margins = Margin.Empty;

            if (font >= 20 || _unicodeFontAddress[font] == null)
            {
                return null;
            }

            if (IsUsingHTML)
            {
                return GetInfoHTML(font, str, len, align, flags, width);
            }

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
                        si = (char)0;
                    }
                }

                ref var @char = ref GetCharUni(font, si);

                if (@char.Data == null && si != ' ' && si != '\n' && si != '\r')
                {
                    continue;
                }

                var charWidth = @char.OffsetX + @char.Width + 1;
                var charHeight = @char.OffsetY + @char.Height;

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

                if (ptr.Width + readWidth + charWidth > width || si == '\n')
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

                        ptr.Data.Length = ptr.CharCount - newlineval;
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

                        if (
                            ptr.Align == TEXT_ALIGN_TYPE.TS_LEFT
                            && (current_flags & UOFONT_INDENTION) != 0
                        )
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
                            MultilinesFontData mfd1 = new MultilinesFontData(
                                current_charcolor,
                                current_flags,
                                current_font,
                                si,
                                0
                            );

                            ptr.Data.Add(mfd1);
                            readWidth += si == '\r' ? 0 : charWidth;

                            if (charHeight > ptr.MaxHeight)
                            {
                                ptr.MaxHeight = charHeight + extraheight;
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
                        ptr.Data.Length = ptr.CharCount;

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

                        if (
                            ptr.Align == TEXT_ALIGN_TYPE.TS_LEFT
                            && (current_flags & UOFONT_INDENTION) != 0
                        )
                        {
                            indetionOffset = 14;
                        }

                        ptr.IndentionOffset = indetionOffset;
                        readWidth = indetionOffset;
                    }
                }

                MultilinesFontData mfd = new MultilinesFontData(
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
                    readWidth += si == '\r' ? 0 : charWidth;

                    if (charHeight > ptr.MaxHeight)
                    {
                        ptr.MaxHeight = charHeight + extraheight;
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

        private unsafe FontInfo GeneratePixelsUnicode(
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
            if (font >= 20 || _unicodeFontAddress[font] == null)
            {
                return FontInfo.Empty;
            }

            int len = str.Length;

            if (len == 0)
            {
                return FontInfo.Empty;
            }

            int oldWidth = width;

            if (width == 0)
            {
                width = GetWidthUnicode(font, str.AsSpan());

                if (width == 0)
                {
                    return FontInfo.Empty;
                }
            }

            MultilinesFontInfo info = GetInfoUnicode(font, str, len, align, flags, width);

            if (info == null)
            {
                return FontInfo.Empty;
            }

            if (IsUsingHTML && (_htmlStatus.Margins.X != 0 || _htmlStatus.Margins.Width != 0))
            {
                while (info != null)
                {
                    MultilinesFontInfo ptr1 = info.Next;
                    info.Data.Clear();
                    info = null;
                    info = ptr1;
                }

                int newWidth = width - (_htmlStatus.Margins.Right);

                if (newWidth < 10)
                {
                    newWidth = 10;
                }

                info = GetInfoUnicode(font, str, len, align, flags, newWidth);

                if (info == null)
                {
                    return FontInfo.Empty;
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

                return FontInfo.Empty;
            }

            height += _htmlStatus.Margins.Y + _htmlStatus.Margins.Height + 4;
            int blocksize = height * width;
            uint[] pData = new uint[blocksize]; // System.Buffers.ArrayPool<uint>.Shared.Rent(blocksize);

            try
            {
                int lineOffsY = _htmlStatus.Margins.Y;
                MultilinesFontInfo ptr = info;
                uint datacolor = 0;

                if (color == 0xFFFF)
                {
                    datacolor = 0xFEFFFFFF;
                }
                else
                {
                    datacolor = HuesHelper.RgbaToArgb(
                        (FileManager.Hues.GetPolygoneColor(cell, color) << 8) | 0xFF
                    );
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
                var links = new FastList<WebLinkRect>();

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
                    var dataSize = ptr.Data.Length;

                    for (int i = 0; i < dataSize; i++)
                    {
                        ref MultilinesFontData dataPtr = ref ptr.Data.Buffer[i];
                        char si = dataPtr.Item;
                        ref var @char = ref GetCharUni(dataPtr.Font, si);

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
                            else if (@char.Data == null && si != ' ')
                            {
                            }
                            else
                            {
                                ofsX = @char.OffsetX;
                            }

                            WebLinkRect wlr = new WebLinkRect
                            {
                                LinkID = oldLink,
                                Bounds = new Margin(linkStartX, linkStartY, w - ofsX, linkHeight)
                            };

                            links.Add(wlr);
                            oldLink = 0;
                        }

                        if (@char.Data == null && si != ' ')
                        {
                            continue;
                        }

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
                            offsX = @char.OffsetX + 1;
                            offsY = @char.OffsetY;
                            dw = @char.Width;
                            dh = @char.Height;
                        }

                        int tmpW = w;
                        uint charcolor = datacolor;

                        bool isBlackPixel =
                            ((charcolor >> 0) & 0xFF) <= 8
                            && ((charcolor >> 8) & 0xFF) <= 8
                            && ((charcolor >> 16) & 0xFF) <= 8;

                        if (si != ' ')
                        {
                            if (IsUsingHTML && i < ptr.Data.Length)
                            {
                                isItalic = (dataPtr.Flags & UOFONT_ITALIC) != 0;
                                isSolid = (dataPtr.Flags & UOFONT_SOLID) != 0;
                                isBlackBorder = (dataPtr.Flags & UOFONT_BLACK_BORDER) != 0;
                                isUnderline = (dataPtr.Flags & UOFONT_UNDERLINE) != 0;

                                if (dataPtr.Color != 0xFFFFFFFF)
                                {
                                    charcolor = HuesHelper.RgbaToArgb(dataPtr.Color);

                                    //isBlackPixel = ((charcolor >> 24) & 0xFF) <= 8 && ((charcolor >> 16) & 0xFF) <= 8 && ((charcolor >> 8) & 0xFF) <= 8;
                                    isBlackPixel =
                                        ((charcolor >> 0) & 0xFF) <= 8
                                        && ((charcolor >> 8) & 0xFF) <= 8
                                        && ((charcolor >> 16) & 0xFF) <= 8;
                                }
                            }

                            int scanlineCount = ((dw - 1) >> 3) + 1;
                            int scanLineOff = 0;

                            for (int y = 0; y < dh; y++, scanLineOff += scanlineCount)
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

                                        byte cl = (byte)(@char.Data[scanLineOff + c] & (1 << (7 - j)));
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

                                                if (
                                                    pData[testBlock] != 0
                                                    && pData[testBlock] != solidColor
                                                )
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

                                                    if (
                                                        testBlock < pData.Length
                                                        && pData[testBlock] != 0
                                                        && pData[testBlock] != blackColor
                                                    )
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

                                    isBlackPixel =
                                        ((charcolor >> 0) & 0xFF) <= 8
                                        && ((charcolor >> 8) & 0xFF) <= 8
                                        && ((charcolor >> 16) & 0xFF) <= 8;
                                }
                            }
                        }

                        if (isUnderline)
                        {
                            int minXOk = tmpW + offsX > 0 ? -1 : 0;
                            int maxXOk = w + offsX + dw < width ? 1 : 0;
                            ref var @achar = ref GetCharUni(font, 'a');
                            int testY = lineOffsY + @achar.OffsetY + @achar.Height;

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

                if (
                    IsUsingHTML
                    && _htmlStatus.IsHtmlBackgroundColored
                    && _htmlStatus.BackgroundColor != 0
                )
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

                FontInfo fi = new FontInfo();

                fi.LineCount = linesCount;
                fi.Width = width;
                fi.Height = height;
                fi.Data = pData;
                fi.Links = links;
                return fi;
            }
            finally
            {
                //System.Buffers.ArrayPool<uint>.Shared.Return(pData, true);
            }
        }

        private unsafe MultilinesFontInfo GetInfoHTML(
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

            GetHTMLData(htmlData, font, str.AsSpan(), ref len, align, flags);

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
                ref var @char = ref GetCharUni(htmlData[i].Font, si);

                if (si == 0x000D || si == '\n')
                {
                    if (si == 0x000D || isFixed || isCropped)
                    {
                        si = (char)0;
                    }
                    else
                    {
                        si = '\n';
                    }
                }

                if (@char.Data == null && si != ' ' && si != '\n')
                {
                    continue;
                }

                if (si == ' ')
                {
                    lastSpace = i;
                    ptr.Width += readWidth;
                    readWidth = 0;
                    ptr.CharCount += charCount;
                    charCount = 0;
                }

                int solidWidth = htmlData[i].Flags & UOFONT_SOLID;
                var charWidth = @char.OffsetX + @char.Width + 1;
                var charHeight = @char.OffsetY + @char.Height;

                if (ptr.Width + readWidth + charWidth + solidWidth > width || si == '\n')
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
                        ptr.Data.Length = ptr.CharCount;
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

                        if (
                            ptr.Align == TEXT_ALIGN_TYPE.TS_LEFT
                            && (htmlData[i].Flags & UOFONT_INDENTION) != 0
                        )
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
                            MultilinesFontData mfd1 = new MultilinesFontData(
                                htmlData[i].Color,
                                htmlData[i].Flags,
                                htmlData[i].Font,
                                si,
                                htmlData[i].LinkID
                            );

                            ptr.Data.Add(mfd1);
                            readWidth += charWidth;
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
                        ptr.Data.Length = ptr.CharCount;
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

                        if (
                            ptr.Align == TEXT_ALIGN_TYPE.TS_LEFT
                            && (htmlData[i].Flags & UOFONT_INDENTION) != 0
                        )
                        {
                            indentionOffset = 14;
                        }

                        ptr.IndentionOffset = indentionOffset;
                        readWidth = indentionOffset;
                    }
                }

                MultilinesFontData mfd = new MultilinesFontData(
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
                    readWidth += charWidth + solidWidth;
                }

                charCount++;
            }

            ptr.Width += readWidth;
            ptr.CharCount += charCount;
            ptr.MaxHeight = MAX_HTML_TEXT_HEIGHT;

            return info;
        }

        private unsafe void GetHTMLData(
            HTMLChar* data,
            byte font,
            ReadOnlySpan<char> str,
            ref int len,
            TEXT_ALIGN_TYPE align,
            ushort flags
        )
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

            var stack = new FastList<HTMLDataInfo>();
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

                    HTML_TAG_TYPE tag = ParseHTMLTag(str, len, ref i, ref endTag, ref newInfo);

                    if (tag == HTML_TAG_TYPE.HTT_NONE)
                    {
                        continue;
                    }

                    if (!endTag)
                    {
                        if (newInfo.Font == 0xFF)
                        {
                            newInfo.Font = stack[stack.Length - 1].Font;
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
                    else if (stack.Length > 1)
                    {
                        //int index = -1;

                        for (var j = stack.Length - 1; j >= 1; j--)
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
                                si = (char)0;
                            }

                            break;

                        case HTML_TAG_TYPE.HTT_BODYBGCOLOR:
                        case HTML_TAG_TYPE.HTT_BR:
                        case HTML_TAG_TYPE.HTT_BQ:
                            si = '\n';

                            break;

                        default:
                            si = (char)0;

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

        private void GetCurrentHTMLInfo(ref FastList<HTMLDataInfo> list, ref HTMLDataInfo info)
        {
            info.Tag = HTML_TAG_TYPE.HTT_NONE;
            info.Align = TEXT_ALIGN_TYPE.TS_LEFT;
            info.Flags = 0;
            info.Font = 0xFF;
            info.Color = 0;
            info.Link = 0;

            for (int i = 0; i < list.Length; i++)
            {
                ref var current = ref list.Buffer[i];

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

                        if (
                            current.Font != 0xFF && _unicodeFontAddress[current.Font] != null
                        )
                        {
                            info.Font = current.Font;
                        }

                        break;

                    case HTML_TAG_TYPE.HTT_BASEFONT:

                        if (
                            current.Font != 0xFF && _unicodeFontAddress[current.Font] != null
                        )
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

                        if (
                            current.Font != 0xFF && _unicodeFontAddress[current.Font] != null
                        )
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

        private HTML_TAG_TYPE ParseHTMLTag(
            ReadOnlySpan<char> str,
            int len,
            ref int i,
            ref bool endTag,
            ref HTMLDataInfo info
        )
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

                ReadOnlySpan<char> span = str.Slice(startIndex, cmdLen);

                if (span.Equals("b".AsSpan(), StringComparison.InvariantCultureIgnoreCase))
                {
                    tag = HTML_TAG_TYPE.HTT_B;
                }
                else if (span.Equals("i".AsSpan(), StringComparison.InvariantCultureIgnoreCase))
                {
                    tag = HTML_TAG_TYPE.HTT_I;
                }
                else if (span.Equals("a".AsSpan(), StringComparison.InvariantCultureIgnoreCase))
                {
                    tag = HTML_TAG_TYPE.HTT_A;
                }
                else if (span.Equals("u".AsSpan(), StringComparison.InvariantCultureIgnoreCase))
                {
                    tag = HTML_TAG_TYPE.HTT_U;
                }
                else if (span.Equals("p".AsSpan(), StringComparison.InvariantCultureIgnoreCase))
                {
                    tag = HTML_TAG_TYPE.HTT_P;
                }
                else if (span.Equals("big".AsSpan(), StringComparison.InvariantCultureIgnoreCase))
                {
                    tag = HTML_TAG_TYPE.HTT_BIG;
                }
                else if (span.Equals("small".AsSpan(), StringComparison.InvariantCultureIgnoreCase))
                {
                    tag = HTML_TAG_TYPE.HTT_SMALL;
                }
                else if (span.Equals("body".AsSpan(), StringComparison.InvariantCultureIgnoreCase))
                {
                    tag = HTML_TAG_TYPE.HTT_BODY;
                }
                else if (
                    span.Equals("basefont".AsSpan(), StringComparison.InvariantCultureIgnoreCase)
                )
                {
                    tag = HTML_TAG_TYPE.HTT_BASEFONT;
                }
                else if (span.Equals("h1".AsSpan(), StringComparison.InvariantCultureIgnoreCase))
                {
                    tag = HTML_TAG_TYPE.HTT_H1;
                }
                else if (span.Equals("h2".AsSpan(), StringComparison.InvariantCultureIgnoreCase))
                {
                    tag = HTML_TAG_TYPE.HTT_H2;
                }
                else if (span.Equals("h3".AsSpan(), StringComparison.InvariantCultureIgnoreCase))
                {
                    tag = HTML_TAG_TYPE.HTT_H3;
                }
                else if (span.Equals("h4".AsSpan(), StringComparison.InvariantCultureIgnoreCase))
                {
                    tag = HTML_TAG_TYPE.HTT_H4;
                }
                else if (span.Equals("h5".AsSpan(), StringComparison.InvariantCultureIgnoreCase))
                {
                    tag = HTML_TAG_TYPE.HTT_H5;
                }
                else if (span.Equals("h6".AsSpan(), StringComparison.InvariantCultureIgnoreCase))
                {
                    tag = HTML_TAG_TYPE.HTT_H6;
                }
                else if (span.Equals("br".AsSpan(), StringComparison.InvariantCultureIgnoreCase))
                {
                    tag = HTML_TAG_TYPE.HTT_BR;
                }
                else if (span.Equals("bq".AsSpan(), StringComparison.InvariantCultureIgnoreCase))
                {
                    tag = HTML_TAG_TYPE.HTT_BQ;
                }
                else if (span.Equals("left".AsSpan(), StringComparison.InvariantCultureIgnoreCase))
                {
                    tag = HTML_TAG_TYPE.HTT_LEFT;
                }
                else if (
                    span.Equals("center".AsSpan(), StringComparison.InvariantCultureIgnoreCase)
                )
                {
                    tag = HTML_TAG_TYPE.HTT_CENTER;
                }
                else if (span.Equals("right".AsSpan(), StringComparison.InvariantCultureIgnoreCase))
                {
                    tag = HTML_TAG_TYPE.HTT_RIGHT;
                }
                else if (span.Equals("div".AsSpan(), StringComparison.InvariantCultureIgnoreCase))
                {
                    tag = HTML_TAG_TYPE.HTT_DIV;
                }
                else
                {
                    if (
                        str.IndexOf(
                            "bodybgcolor".AsSpan(),
                            StringComparison.InvariantCultureIgnoreCase
                        ) >= 0
                    )
                    {
                        tag = HTML_TAG_TYPE.HTT_BODYBGCOLOR;
                        j = str.IndexOf(
                            "bgcolor".AsSpan(),
                            StringComparison.InvariantCultureIgnoreCase
                        );
                        endTag = false;
                    }
                    else if (
                        str.IndexOf(
                            "basefont".AsSpan(),
                            StringComparison.InvariantCultureIgnoreCase
                        ) >= 0
                    )
                    {
                        tag = HTML_TAG_TYPE.HTT_BASEFONT;
                        j = str.IndexOf(
                            "color".AsSpan(),
                            StringComparison.InvariantCultureIgnoreCase
                        );
                        endTag = false;
                    }
                    else if (
                        str.IndexOf(
                            "bodytext".AsSpan(),
                            StringComparison.InvariantCultureIgnoreCase
                        ) >= 0
                    )
                    {
                        tag = HTML_TAG_TYPE.HTT_BODY;
                        j = str.IndexOf(
                            "text".AsSpan(),
                            StringComparison.InvariantCultureIgnoreCase
                        );
                        endTag = false;
                    }
                    else
                    {
                        Log.Warn($"Unhandled HTML param:\t{str.ToString()}");
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

                                if (str.Length != 0 && cmdLen >= 0 && str.Length > j && str.Length >= cmdLen)
                                {
                                    GetHTMLInfoFromContent(ref info, str.Slice(j, cmdLen));
                                }

                                break;
                        }
                    }
                }
            }

            return tag;
        }

        private void GetHTMLInfoFromContent(ref HTMLDataInfo info, ReadOnlySpan<char> content)
        {
            if (content.IsEmpty)
                return;

            static void TrimWhitespaces(ref ReadOnlySpan<char> content)
            {
                while (!content.IsEmpty && char.IsWhiteSpace(content[0]))
                    content = content.Slice(1);
            }

            while (!content.IsEmpty)
            {
                TrimWhitespaces(ref content);

                // If content is empty after trimming, exit the loop
                if (content.IsEmpty)
                    break;

                // Parse the attribute name (command)
                ReadOnlySpan<char> command = ReadOnlySpan<char>.Empty;
                var i = 0;

                for (; i < content.Length; i++)
                {
                    var c = content[i];

                    if (char.IsWhiteSpace(c) || c == '=' || c == '\\')
                    {
                        command = content.Slice(0, i);
                        content = content.Slice(i);
                        break;
                    }
                }

                if (command.IsEmpty)
                {
                    break; // Exit if no command is found
                }

                TrimWhitespaces(ref content);

                // Parse the attribute value
                ReadOnlySpan<char> value = ReadOnlySpan<char>.Empty;

                if (!content.IsEmpty && content[0] == '=')
                {
                    // Move past '='
                    content = content.Slice(1);
                    TrimWhitespaces(ref content);

                    // Quoted value handling
                    if (!content.IsEmpty && content[0] == '"')
                    {
                        // Find end quote
                        var endQuoteIndex = content.Slice(1).IndexOf('"') + 1;
                        if (endQuoteIndex > 0)
                        {
                            value = content.Slice(1, endQuoteIndex - 1);
                            content = content.Slice(endQuoteIndex + 1); // Move past the closing quote
                        }
                    }
                    else
                    {
                        // Non-quoted value
                        i = 0;
                        for (; i < content.Length; i++)
                        {
                            var c = content[i];
                            if (char.IsWhiteSpace(c) || c == '\\' || c == '<' || c == '>' || c == '=')
                            {
                                break;
                            }
                        }

                        value = content.Slice(0, i);
                        content = content.Slice(i); // Move past the value
                    }
                }


                switch (info.Tag)
                {
                    case HTML_TAG_TYPE.HTT_BODY:
                    case HTML_TAG_TYPE.HTT_BODYBGCOLOR:

                        if (MemoryExtensions.Equals(command, "text", StringComparison.InvariantCultureIgnoreCase))
                        {
                            ReadColorFromTextBuffer(value, ref info.Color);
                        }
                        else if (MemoryExtensions.Equals(command, "bgcolor", StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (_htmlStatus.IsHtmlBackgroundColored)
                            {
                                ReadColorFromTextBuffer(value, ref _htmlStatus.BackgroundColor);
                            }
                        }
                        else if (MemoryExtensions.Equals(command, "link", StringComparison.InvariantCultureIgnoreCase))
                        {
                            ReadColorFromTextBuffer(value, ref _htmlStatus.WebLinkColor);
                        }
                        else if (MemoryExtensions.Equals(command, "vlink", StringComparison.InvariantCultureIgnoreCase))
                        {
                            ReadColorFromTextBuffer(value, ref _htmlStatus.VisitedWebLinkColor);
                        }
                        else if (MemoryExtensions.Equals(command, "leftmargin", StringComparison.InvariantCultureIgnoreCase))
                        {
                            _htmlStatus.Margins.X = int.Parse(value);
                        }
                        else if (MemoryExtensions.Equals(command, "topmargin", StringComparison.InvariantCultureIgnoreCase))
                        {
                            _htmlStatus.Margins.Y = int.Parse(value);
                        }
                        else if (MemoryExtensions.Equals(command, "rightmargin", StringComparison.InvariantCultureIgnoreCase))
                        {
                            _htmlStatus.Margins.Width = int.Parse(value);
                        }
                        else if (MemoryExtensions.Equals(command, "bottommargin", StringComparison.InvariantCultureIgnoreCase))
                        {
                            _htmlStatus.Margins.Height = int.Parse(value);
                        }

                        break;

                    case HTML_TAG_TYPE.HTT_BASEFONT:

                        if (MemoryExtensions.Equals(command, "color", StringComparison.InvariantCultureIgnoreCase))
                        {
                            ReadColorFromTextBuffer(value, ref info.Color);
                        }
                        else if (MemoryExtensions.Equals(command, "size", StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (!byte.TryParse(value, out var font))
                            {
                                if (MemoryExtensions.Equals(value, "big", StringComparison.InvariantCultureIgnoreCase))
                                    info.Font = 4;
                                else if (MemoryExtensions.Equals(value, "small", StringComparison.InvariantCultureIgnoreCase))
                                    info.Font = 0;
                                else
                                    info.Font = 1;
                            }
                            else switch (font)
                            {
                                case 0:
                                case 4:
                                    info.Font = 1;

                                    break;

                                case < 4:
                                    info.Font = 2;

                                    break;

                                default:
                                    info.Font = 0;

                                    break;
                            }
                        }

                        break;

                    case HTML_TAG_TYPE.HTT_A:

                        if (MemoryExtensions.Equals(command, "href", StringComparison.InvariantCultureIgnoreCase))
                        {
                            info.Flags = UOFONT_UNDERLINE;
                            info.Color = _htmlStatus.WebLinkColor;
                            info.Link = GetWebLinkID(value, ref info.Color);
                        }

                        break;

                    case HTML_TAG_TYPE.HTT_P:
                    case HTML_TAG_TYPE.HTT_DIV:

                        if (MemoryExtensions.Equals(command, "align", StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (MemoryExtensions.Equals(value, "left", StringComparison.InvariantCultureIgnoreCase))
                            {
                                info.Align = TEXT_ALIGN_TYPE.TS_LEFT;
                            }
                            else if (MemoryExtensions.Equals(value, "center", StringComparison.InvariantCultureIgnoreCase))
                            {
                                info.Align = TEXT_ALIGN_TYPE.TS_CENTER;
                            }
                            else if (MemoryExtensions.Equals(value, "right", StringComparison.InvariantCultureIgnoreCase))
                            {
                                info.Align = TEXT_ALIGN_TYPE.TS_RIGHT;
                            }
                        }

                        break;
                }

                // Continue parsing the remaining content until it is empty
                TrimWhitespaces(ref content);
            }
        }

        private ushort GetWebLinkID(ReadOnlySpan<char> link, ref uint color)
        {
            foreach (KeyValuePair<ushort, WebLink> ll in _webLinks)
            {
                if (link.SequenceEqual(ll.Value.Link))
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
                webLink.Link = link.ToString();

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

        private unsafe void ReadColorFromTextBuffer(ReadOnlySpan<char> buffer, ref uint color)
        {
            color = 0x00_00_00_00;

            if (!buffer.IsEmpty)
            {
                if (buffer[0] == '#')
                {
                    if (buffer.Length > 1)
                    {
                        int startIndex = buffer[1] == '0' && buffer[2] == 'x' ? 3 : 1;

                        uint.TryParse(
                            buffer.Slice(startIndex),
                            NumberStyles.HexNumber,
                            CultureInfo.InvariantCulture,
                            out var cc
                        );

                        byte* clrbuf = (byte*)&cc;
                        color = (uint)(
                            (clrbuf[0] << 24) | (clrbuf[1] << 16) | (clrbuf[2] << 8) | 0xFF
                        );
                    }
                }
                else if (char.IsNumber(buffer[0]))
                {
                    color = Convert.ToUInt32(buffer.ToString(), 16);
                }
                else
                {
                    if (MemoryExtensions.Equals(buffer, "red", StringComparison.InvariantCultureIgnoreCase))
                    {
                        color = 0x0000FFFF;
                    }
                    else if (MemoryExtensions.Equals(buffer, "cyan", StringComparison.InvariantCultureIgnoreCase))
                    {
                        color = 0xFFFF00FF;
                    }
                    else if (MemoryExtensions.Equals(buffer, "blue", StringComparison.InvariantCultureIgnoreCase))
                    {
                        color = 0xFF0000FF;
                    }
                    else if (MemoryExtensions.Equals(buffer, "darkblue", StringComparison.InvariantCultureIgnoreCase))
                    {
                        color = 0xA00000FF;
                    }
                    else if (MemoryExtensions.Equals(buffer, "lightblue", StringComparison.InvariantCultureIgnoreCase))
                    {
                        color = 0xE6D8ADFF;
                    }
                    else if (MemoryExtensions.Equals(buffer, "purple", StringComparison.InvariantCultureIgnoreCase))
                    {
                        color = 0x800080FF;
                    }
                    else if (MemoryExtensions.Equals(buffer, "yellow", StringComparison.InvariantCultureIgnoreCase))
                    {
                        color = 0x00FFFFFF;
                    }
                    else if (MemoryExtensions.Equals(buffer, "lime", StringComparison.InvariantCultureIgnoreCase))
                    {
                        color = 0x00FF00FF;
                    }
                    else if (MemoryExtensions.Equals(buffer, "magenta", StringComparison.InvariantCultureIgnoreCase))
                    {
                        color = 0xFF00FFFF;
                    }
                    else if (MemoryExtensions.Equals(buffer, "white", StringComparison.InvariantCultureIgnoreCase))
                    {
                        color = 0xFFFEFEFF;
                    }
                    else if (MemoryExtensions.Equals(buffer, "silver", StringComparison.InvariantCultureIgnoreCase))
                    {
                        color = 0xC0C0C0FF;
                    }
                    else if (MemoryExtensions.Equals(buffer, "grey", StringComparison.InvariantCultureIgnoreCase) ||
                             MemoryExtensions.Equals(buffer, "gray", StringComparison.InvariantCultureIgnoreCase))
                    {
                        color = 0x808080FF;
                    }
                    else if (MemoryExtensions.Equals(buffer, "black", StringComparison.InvariantCultureIgnoreCase))
                    {
                        color = 0x010101FF;
                    }
                    else if (MemoryExtensions.Equals(buffer, "orange", StringComparison.InvariantCultureIgnoreCase))
                    {
                        color = 0x00A5FFFF;
                    }
                    else if (MemoryExtensions.Equals(buffer, "brown", StringComparison.InvariantCultureIgnoreCase))
                    {
                        color = 0x2A2AA5FF;
                    }
                    else if (MemoryExtensions.Equals(buffer, "maroon", StringComparison.InvariantCultureIgnoreCase))
                    {
                        color = 0x000080FF;
                    }
                    else if (MemoryExtensions.Equals(buffer, "green", StringComparison.InvariantCultureIgnoreCase))
                    {
                        color = 0x008000FF;
                    }
                    else if (MemoryExtensions.Equals(buffer, "olive", StringComparison.InvariantCultureIgnoreCase))
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

        public int GetHeightUnicode(
            byte font,
            string str,
            int width,
            TEXT_ALIGN_TYPE align,
            ushort flags
        )
        {
            if (font >= 20 || _unicodeFontAddress[font] == null || string.IsNullOrEmpty(str))
            {
                return 0;
            }

            if (width <= 0)
            {
                width = GetWidthUnicode(font, str.AsSpan());
            }

            MultilinesFontInfo info = GetInfoUnicode(font, str, str.Length, align, flags, width);

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

        public unsafe (int, int) GetCaretPosUnicode(
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

            if (font >= 20 || _unicodeFontAddress[font] == null || string.IsNullOrEmpty(str))
            {
                return (x, y);
            }

            if (width == 0)
            {
                width = GetWidthUnicode(font, str.AsSpan());
            }

            MultilinesFontInfo info = GetInfoUnicode(font, str, str.Length, align, flags, width);

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

                if (pos <= info.CharStart + len && info.Data.Length >= len)
                {
                    for (int i = 0; i < len; i++)
                    {
                        char ch = info.Data[i].Item;
                        ref var @char = ref GetCharUni(font, ch);
                        if (ch != '\r' && @char.Data != null)
                        {
                            x += (sbyte)(@char.OffsetX + @char.Width + 1);
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

        public (int, int) GetCaretPosASCII(
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

            if (width == 0)
            {
                width = GetWidthASCII(font, str);
            }

            MultilinesFontInfo info = GetInfoASCII(font, str, str.Length, align, flags, width);

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

                if (pos <= info.CharStart + len && info.Data.Length >= len)
                {
                    for (int i = 0; i < len; i++)
                    {
                        x += _fontDataASCII[font, GetASCIIIndex(info.Data[i].Item)].Width;

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
    }

    public enum TEXT_ALIGN_TYPE
    {
        TS_LEFT = 0,
        TS_CENTER,
        TS_RIGHT
    }

    public enum HTML_TAG_TYPE
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
    public struct FontHeader
    {
        public byte Width, Height, Unknown;
    }

    public struct FontCharacterData
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

    public struct FontCharacterDataUnicode
    {
        public FontCharacterDataUnicode(sbyte w, sbyte h, sbyte offX, sbyte offY, byte[] data)
        {
            OffsetX = offX;
            OffsetY = offY;
            Width = w;
            Height = h;
            Data = data;
        }

        public sbyte OffsetX, OffsetY;
        public sbyte Width, Height;
        public byte[] Data;
    }

    public sealed class MultilinesFontInfo
    {
        public TEXT_ALIGN_TYPE Align;
        public int CharCount;
        public int CharStart;
        public FastList<MultilinesFontData> Data = new FastList<MultilinesFontData>();
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

    public struct MultilinesFontData
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

    public struct WebLinkRect
    {
        public ushort LinkID;
        public FontsLoader.Margin Bounds;
    }

    public class WebLink
    {
        public bool IsVisited;
        public string Link;
    }

    public struct HTMLChar
    {
        public char Char;
        public byte Font;
        public TEXT_ALIGN_TYPE Align;
        public ushort Flags;
        public uint Color;
        public ushort LinkID;
    }

    public struct HTMLDataInfo
    {
        public HTML_TAG_TYPE Tag;
        public TEXT_ALIGN_TYPE Align;
        public ushort Flags;
        public byte Font;
        public uint Color;
        public ushort Link;
    }
}
