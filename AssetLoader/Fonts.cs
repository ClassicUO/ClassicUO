using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ClassicUO.Utility;

namespace ClassicUO.AssetsLoader
{
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
        HTT_DIV
    }

    public static class Fonts
    {
        private const int UOFONT_SOLID = 0x01;
        private const int UOFONT_ITALIC = 0x02;
        private const int UOFONT_INDENTION = 0x04;
        private const int UOFONT_BLACK_BORDER = 0x08;
        private const int UOFONT_UNDERLINE = 0x10;
        private const int UOFONT_FIXED = 0x20;
        private const int UOFONT_CROPPED = 0x40;
        private const int UOFONT_BQ = 0x80;

        private const int UNICODE_SPACE_WIDTH = 8;
        private const int MAX_HTML_TEXT_HEIGHT = 18;
        private const float ITALIC_FONT_KOEFFICIENT = 3.3f;

        private static FontData[] _font;
        private static readonly IntPtr[] _unicodeFontAddress = new IntPtr[20];
        private static readonly long[] _unicodeFontSize = new long[20];
        private static readonly Dictionary<ushort, WebLink> _webLinks = new Dictionary<ushort, WebLink>();

        private static readonly int[] offsetCharTable = {2, 0, 2, 2, 0, 0, 2, 2, 0, 0};
        private static readonly int[] offsetSymbolTable = {1, 0, 1, 1, -1, 0, 1, 1, 0, 0};

        private static readonly byte[] _fontIndex =
        {
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
            16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31,
            32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47,
            48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63,
            64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79,
            80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 136, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 152, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            160, 161, 162, 163, 164, 165, 166, 167, 168, 169, 170, 171, 172, 173, 174, 175,
            176, 177, 178, 179, 180, 181, 182, 183, 184, 185, 186, 187, 188, 189, 190, 191,
            192, 193, 194, 195, 196, 197, 198, 199, 200, 201, 202, 203, 204, 205, 206, 207,
            208, 209, 210, 211, 212, 213, 214, 215, 216, 217, 218, 219, 220, 221, 222, 223
        };

        private static uint _webLinkColor;
        private static uint _visitedWebLinkColor;
        private static uint _backgroundColor;
        private static int _leftMargin, _topMargin, _rightMargin, _bottomMargin;
        private static bool _HTMLBackgroundCanBeColored;
        private static uint _HTMLColor = 0xFFFFFFFF;

        public static int FontCount { get; private set; }

        public static bool UnusePartialHue { get; set; } = false;
        public static bool RecalculateWidthByInfo { get; set; } = false;
        public static bool IsUsingHTML { get; private set; }

        public static void Load()
        {
            var fonts = new UOFileMul(Path.Combine(FileManager.UoFolderPath, "fonts.mul"));

            var uniFonts = new UOFileMul[20];
            for (var i = 0; i < 20; i++)
            {
                var path = Path.Combine(FileManager.UoFolderPath, "unifont" + (i == 0 ? "" : i.ToString()) + ".mul");
                if (File.Exists(path))
                {
                    uniFonts[i] = new UOFileMul(path);
                    _unicodeFontAddress[i] = uniFonts[i].StartAddress;
                    _unicodeFontSize[i] = uniFonts[i].Length;
                }
            }

            var fontHeaderSize = Marshal.SizeOf<FontHeader>();
            FontCount = 0;

            while (fonts.Position < fonts.Length)
            {
                var exit = false;
                fonts.Skip(1);

                unsafe
                {
                    for (var i = 0; i < 224; i++)
                    {
                        var fh = (FontHeader*) fonts.PositionAddress;
                        fonts.Skip(fontHeaderSize);

                        var bcount = fh->Width * fh->Height * 2;
                        if (fonts.Position + bcount > fonts.Length)
                        {
                            exit = true;
                            break;
                        }

                        fonts.Skip(bcount);
                    }
                }

                if (exit)
                    break;

                FontCount++;
            }

            if (FontCount < 1)
            {
                FontCount = 0;
                return;
            }

            _font = new FontData[FontCount];
            fonts.Seek(0);

            for (var i = 0; i < FontCount; i++)
            {
                _font[i].Header = fonts.ReadByte();
                _font[i].Chars = new FontCharacterData[224];
                for (var j = 0; j < 224; j++)
                {
                    _font[i].Chars[j].Width = fonts.ReadByte();
                    _font[i].Chars[j].Height = fonts.ReadByte();
                    fonts.Skip(1);
                    var dataSize = _font[i].Chars[j].Width * _font[i].Chars[j].Height;
                    _font[i].Chars[j].Data = fonts.ReadArray<ushort>(dataSize).ToList();
                }
            }


            if (_unicodeFontAddress[1] == IntPtr.Zero)
            {
                _unicodeFontAddress[1] = _unicodeFontAddress[0];
                _unicodeFontSize[1] = _unicodeFontSize[0];
            }

            for (var i = 0; i < 256; i++)
                if (_fontIndex[i] >= 0xE0)
                    _fontIndex[i] = _fontIndex[' '];
        }


        private static int GetWidthASCII(in byte font, in string str)
        {
            if (font >= FontCount || string.IsNullOrEmpty(str))
                return 0;
            var fd = _font[font];
            var textLength = 0;
            foreach (var c in str)
                textLength += fd.Chars[_fontIndex[(byte) c]].Width;
            return textLength;
        }

        private static int GetHeightASCII(MultilinesFontInfo info)
        {
            var textHeight = 0;

            while (info != null)
            {
                textHeight += info.MaxHeight;
                info = info.Next;
            }

            return textHeight;
        }

        public static (uint[], int, int, int, bool) GenerateASCII(in byte font, in string str, in ushort color,
            int width, in TEXT_ALIGN_TYPE align, in ushort flags)
        {
            var linesCount = 0;
            if ((flags & UOFONT_FIXED) != 0 || (flags & UOFONT_CROPPED) != 0)
            {
                linesCount--;
                if (width <= 0 || string.IsNullOrEmpty(str))
                    return (null, 0, 0, linesCount, false);

                var realWidth = GetWidthASCII(font, str);

                if (realWidth > width)
                {
                    var newstr = GetTextByWidthASCII(font, str, width, (flags & UOFONT_CROPPED) != 0);
                    return GeneratePixelsASCII(font, newstr, color, width, align, flags);
                }
            }

            return GeneratePixelsASCII(font, str, color, width, align, flags);
        }

        private static string GetTextByWidthASCII(in byte font, in string str, int width, in bool isCropped)
        {
            if (font >= FontCount || string.IsNullOrEmpty(str))
                return string.Empty;

            var fd = _font[font];

            if (isCropped)
                width -= fd.Chars[_fontIndex[(byte) '.']].Width * 3;

            var textLength = 0;
            var result = "";

            foreach (var c in str)
            {
                textLength += fd.Chars[_fontIndex[(byte) c]].Width;
                if (textLength > width)
                    break;
                result += c;
            }

            if (isCropped)
                result += "...";

            return result;
        }

        private static (uint[], int, int, int, bool) GeneratePixelsASCII(in byte font, in string str, in ushort color,
            int width, in TEXT_ALIGN_TYPE align, in ushort flags)
        {
            uint[] pData;

            if (font >= FontCount)
                return (null, 0, 0, 0, false);

            var len = str.Length;
            if (len <= 0)
                return (null, 0, 0, 0, false);

            var fd = _font[font];
            if (width <= 0)
                width = GetWidthASCII(font, str);
            if (width <= 0)
                return (null, 0, 0, 0, false);

            var info = GetInfoASCII(font, str, len, align, flags, width);
            if (info == null)
                return (null, 0, 0, 0, false);

            width += 4;
            var height = GetHeightASCII(info);

            if (height <= 0)
            {
                var ptr1 = info;
                while (ptr1 != null)
                {
                    info = ptr1;
                    ptr1 = ptr1.Next;
                    info.Data.Clear();
                    info = null;
                }

                return (null, 0, 0, 0, false);
            }

            var blocksize = height * width;
            pData = new uint[blocksize];

            var lineOffsY = 0;
            var ptr = info;

            var partialHue = font != 5 && font != 8 && !UnusePartialHue;
            var font6OffsetY = font == 6 ? 7 : 0;

            var linesCount = 0; // this value should be added to TextTexture.LinesCount += linesCount

            while (ptr != null)
            {
                info = ptr;
                linesCount++;
                var w = 0;
                if (ptr.Align == TEXT_ALIGN_TYPE.TS_CENTER)
                {
                    w = (w - 10 - ptr.Width) / 2;
                    if (w < 0)
                        w = 0;
                }
                else if (ptr.Align == TEXT_ALIGN_TYPE.TS_RIGHT)
                {
                    w = width - 10 - ptr.Width;
                    if (w == 0)
                        w = 0;
                }
                else if (ptr.Align == TEXT_ALIGN_TYPE.TS_LEFT && (flags & UOFONT_INDENTION) != 0)
                {
                    w = ptr.IndentionOffset;
                }

                var count = ptr.Data.Count;

                for (var i = 0; i < count; i++)
                {
                    var index = (byte) ptr.Data[i].Item;
                    var offsY = GetFontOffsetY(font, index);

                    var fcd = fd.Chars[_fontIndex[index]];
                    int dw = fcd.Width;
                    int dh = fcd.Height;

                    var charColor = color;

                    for (var y = 0; y < dh; y++)
                    {
                        var testrY = y + lineOffsY + offsY;
                        if (testrY >= height)
                            break;

                        for (var x = 0; x < dw; x++)
                        {
                            if (x + w >= width)
                                break;
                            var pic = fcd.Data[y * dw + x];

                            if (pic > 0)
                            {
                                uint pcl = 0;

                                if (partialHue)
                                    pcl = Hues.GetPartialHueColor(pic, charColor);
                                else
                                    pcl = Hues.GetColor(pic, charColor);

                                var block = testrY * width + x + w;

                                pData[block] = Hues.RgbaToArgb((pcl << 8) | 0xFF);
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

            return (pData, width, height, linesCount, partialHue);
        }

        public static int GetFontOffsetY(in byte font, in byte index)
        {
            if (index == 0xB8) return 1;

            if (!(index >= 0x41 && index <= 0x5A) && !(index >= 0xC0 && index <= 0xDF) && index != 0xA8)
            {
                if (font < 10)
                {
                    if (index >= 0x61 && index <= 0x7A)
                        return offsetCharTable[font];
                    return offsetSymbolTable[font];
                }

                return 2;
            }

            return 0;
        }

        public static MultilinesFontInfo GetInfoASCII(in byte font, in string str, in int len, in TEXT_ALIGN_TYPE align,
            in ushort flags, in int width)
        {
            if (font >= FontCount)
                return null;
            var fd = _font[font];

            var info = new MultilinesFontInfo();
            info.Reset();
            info.Align = align;

            var ptr = info;

            var indentionOffset = 0;
            ptr.IndentionOffset = 0;

            var isFixed = (flags & UOFONT_FIXED) != 0;
            var isCropped = (flags & UOFONT_CROPPED) != 0;

            var charCount = 0;
            var lastSpace = 0;
            var readWidth = 0;

            for (var i = 0; i < len; i++)
            {
                var si = str[i];
                if (si == '\r' || si == '\n')
                {
                    if (si == '\r' || isFixed || isCropped)
                        continue;
                    si = '\n';
                }

                if (si == ' ')
                {
                    lastSpace = i;
                    ptr.Width += readWidth;
                    readWidth = 0;
                    ptr.CharCount += charCount;
                    charCount = 0;
                }

                var fcd = fd.Chars[_fontIndex[(byte) si]];

                if (si == '\n' || ptr.Width + readWidth + fcd.Width > width)
                {
                    if (lastSpace == ptr.CharStart && lastSpace <= 0 && si != '\n')
                        ptr.CharStart = 1;

                    if (si == '\n')
                    {
                        ptr.Width += readWidth;
                        ptr.CharCount += charCount;

                        lastSpace = i;

                        if (ptr.Width <= 0)
                            ptr.Width = 1;
                        if (ptr.MaxHeight <= 0)
                            ptr.MaxHeight = 14;

                        ptr.Data.Resize(ptr.CharCount); // = new List<MultilinesFontData>(ptr.CharCount);
                        var newptr = new MultilinesFontInfo();
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

                    if (lastSpace + 1 == ptr.CharStart && !isFixed && !isCropped)
                    {
                        ptr.Width += readWidth;
                        ptr.CharCount += charCount;

                        if (ptr.Width <= 0)
                            ptr.Width = 1;
                        if (ptr.MaxHeight <= 0)
                            ptr.MaxHeight = 14;

                        var newptr = new MultilinesFontInfo();
                        newptr.Reset();
                        ptr.Next = newptr;
                        ptr = newptr;
                        ptr.Align = align;
                        ptr.CharStart = i;
                        lastSpace = i - 1;
                        charCount = 0;

                        if (ptr.Align == TEXT_ALIGN_TYPE.TS_LEFT && (flags & UOFONT_INDENTION) != 0)
                            indentionOffset = 14;

                        ptr.IndentionOffset = indentionOffset;
                        readWidth = indentionOffset;
                    }
                    else
                    {
                        if (isFixed)
                        {
                            var mfd1 = new MultilinesFontData
                            {
                                Item = si,
                                Flags = flags,
                                Font = font,
                                LinkID = 0,
                                Color = 0xFFFFFFFF
                            };

                            ptr.Data.Add(mfd1);

                            readWidth += fcd.Width;

                            if (fcd.Height > ptr.MaxHeight)
                                ptr.MaxHeight = fcd.Height;
                            charCount++;
                            ptr.Width += readWidth;
                            ptr.CharCount += charCount;
                        }

                        i = lastSpace + 1;
                        si = str[i];
                        if (ptr.Width <= 0)
                            ptr.Width = 1;
                        if (ptr.MaxHeight <= 0)
                            ptr.MaxHeight = 14;

                        // DATA.resize() ??????
                        ptr.Data.Resize(charCount); //= new List<MultilinesFontData>(charCount);
                        charCount = 0;

                        if (isFixed || isCropped)
                            break;

                        var newptr = new MultilinesFontInfo();
                        newptr.Reset();

                        ptr.Next = newptr;
                        ptr = newptr;
                        ptr.Align = align;
                        ptr.CharStart = i;
                        if (ptr.Align == TEXT_ALIGN_TYPE.TS_LEFT && (flags & UOFONT_INDENTION) != 0)
                            indentionOffset = 14;
                        ptr.IndentionOffset = indentionOffset;
                        readWidth = indentionOffset;
                    }
                }

                var mfd = new MultilinesFontData
                {
                    Item = si,
                    Flags = flags,
                    Font = font,
                    LinkID = 0,
                    Color = 0xFFFFFFFF
                };
                ptr.Data.Add(mfd);
                readWidth += fcd.Width;
                if (fcd.Height > ptr.MaxHeight)
                    ptr.MaxHeight = fcd.Height;
                charCount++;
            }

            ptr.Width += readWidth;
            ptr.CharCount += charCount;

            if ((readWidth <= 0) & (len > 0) && (str[len - 1] == '\n' || str[len - 1] == '\r'))
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
                        ptr.MaxHeight = ptr.MaxHeight + 2;
                    else
                        ptr.MaxHeight = ptr.MaxHeight + 6;
                    ptr = ptr.Next;
                }
            }

            return info;
        }


        public static void SetUseHTML(in bool value, uint htmlStartColor = 0xFFFFFFFF,
            in bool backgroundCanBeColored = false)
        {
            IsUsingHTML = value;
            _HTMLColor = htmlStartColor;
            _HTMLBackgroundCanBeColored = backgroundCanBeColored;
        }


        public static (uint[], int, int, int, List<WebLinkRect>) GenerateUnicode(in byte font, in string str,
            in ushort color, in byte cell, int width, in TEXT_ALIGN_TYPE align, in ushort flags)
        {
            if ((flags & UOFONT_FIXED) != 0 || (flags & UOFONT_CROPPED) != 0)
            {
                if (width <= 0 || string.IsNullOrEmpty(str))
                    return (null, 0, 0, 0, null);

                var realWidth = GetWidthUnicode(font, str);

                if (realWidth > width)
                {
                    var newstring = GetTextByWidthUnicode(font, str, width, (flags & UOFONT_CROPPED) != 0);
                    return GeneratePixelsUnicode(font, newstring, color, cell, width, align, flags);
                }
            }

            return GeneratePixelsUnicode(font, str, color, cell, width, align, flags);
        }

        private static unsafe string GetTextByWidthUnicode(in byte font, in string str, int width, in bool isCropped)
        {
            if (font >= 20 || _unicodeFontAddress[font] == IntPtr.Zero || string.IsNullOrEmpty(str))
                return string.Empty;

            var table = (uint*) _unicodeFontAddress[font];

            if (isCropped)
            {
                var offset = table['.'];

                if (offset > 0 && offset != 0xFFFFFFFF)
                    width -= *(byte*) ((IntPtr) table + (int) offset + 2) * 3;
            }

            var textLength = 0;
            var result = "";

            foreach (var c in str)
            {
                var offset = table[c];
                sbyte charWidth = 0;

                if (offset > 0 && offset != 0xFFFFFFFF)
                {
                    var ptr = (byte*) ((IntPtr) table + (int) offset);
                    charWidth = (sbyte) (ptr[0] + ptr[2] + 1);
                }
                else if (c == ' ')
                {
                    charWidth = UNICODE_SPACE_WIDTH;
                }

                if (charWidth > 0)
                {
                    textLength += charWidth;
                    if (textLength > width)
                        break;
                    result += c;
                }
            }

            if (isCropped)
                result += "...";
            return result;
        }

        private static unsafe int GetWidthUnicode(in byte font, in string str)
        {
            if (font >= 20 || _unicodeFontAddress[font] == IntPtr.Zero || string.IsNullOrEmpty(str))
                return 0;

            var table = (uint*) _unicodeFontAddress[font];
            var textLength = 0;
            var maxTextLenght = 0;

            foreach (var c in str)
            {
                var offset = table[c];
                if (offset > 0 && offset != 0xFFFFFFFF)
                {
                    var ptr = (byte*) ((IntPtr) table + (int) offset);
                    textLength += ptr[0] + ptr[2] + 1;
                }
                else if (c == ' ')
                {
                    textLength += UNICODE_SPACE_WIDTH;
                }
                else if (c == '\n' || c == '\r')
                {
                    maxTextLenght = Math.Max(maxTextLenght, textLength);
                    textLength = 0;
                }
            }

            return Math.Max(maxTextLenght, textLength);
        }

        private static unsafe MultilinesFontInfo GetInfoUnicode(in byte font, in string str, in int len,
            in TEXT_ALIGN_TYPE align, in ushort flags, in int width)
        {
            _webLinkColor = 0xFF0000FF;
            _visitedWebLinkColor = 0x0000FFFF;
            _backgroundColor = 0;
            _leftMargin = 0;
            _topMargin = 0;
            _rightMargin = 0;
            _bottomMargin = 0;

            if (font >= 20 || _unicodeFontAddress[font] == IntPtr.Zero)
                return null;

            if (IsUsingHTML) return GetInfoHTML(font, str, len, align, flags, width);

            var table = (uint*) _unicodeFontAddress[font];
            var info = new MultilinesFontInfo();
            info.Reset();
            info.Align = align;

            var ptr = info;

            var indetionOffset = 0;
            ptr.IndentionOffset = 0;

            var charCount = 0;
            var lastSpace = 0;
            var readWidth = 0;

            var isFixed = (flags & UOFONT_FIXED) != 0;
            var isCropped = (flags & UOFONT_CROPPED) != 0;

            var current_align = align;
            var current_flags = flags;
            var current_font = font;
            var charcolor = 0xFFFFFFFF;
            var current_charcolor = 0xFFFFFFFF;
            var lastspace_charcolor = 0xFFFFFFFF;
            var lastaspace_current_charcolor = 0xFFFFFFFF;

            for (var i = 0; i < len; i++)
            {
                var si = str[i];
                if (si == '\r' || si == '\n')
                {
                    if (isFixed || isCropped)
                        si = (char) 0;
                    else
                        si = '\n';
                }

                if ((table[si] <= 0 || table[si] == 0xFFFFFFFF) && si != ' ' && si != '\n')
                    continue;

                var data = (byte*) ((IntPtr) table + (int) table[si]);

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

                if (ptr.Width + readWidth + data[0] + data[2] > width || si == '\n')
                {
                    if (lastSpace == ptr.CharStart && lastSpace <= 0 && si != '\n')
                        ptr.CharStart = 1;

                    if (si == '\n')
                    {
                        ptr.Width += readWidth;
                        ptr.CharCount += charCount;

                        lastSpace = i;

                        if (ptr.Width <= 0)
                            ptr.Width = 1;
                        if (ptr.MaxHeight <= 0)
                            ptr.MaxHeight = 14;

                        ptr.Data.Resize(ptr.CharCount);

                        var newptr = new MultilinesFontInfo();
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

                    if (lastSpace + 1 == ptr.CharStart && !isFixed && !isCropped)
                    {
                        ptr.Width += readWidth;
                        ptr.CharCount += charCount;

                        if (ptr.Width <= 0)
                            ptr.Width = 1;
                        if (ptr.MaxHeight <= 0)
                            ptr.MaxHeight = 14;

                        var newptr = new MultilinesFontInfo();
                        newptr.Reset();
                        ptr.Next = newptr;
                        ptr = newptr;
                        ptr.Align = current_align;
                        ptr.CharStart = i;
                        lastSpace = i - 1;
                        charCount = 0;

                        if (ptr.Align == TEXT_ALIGN_TYPE.TS_LEFT && (current_flags & UOFONT_INDENTION) != 0)
                            indetionOffset = 14;

                        ptr.IndentionOffset = indetionOffset;
                        readWidth = indetionOffset;
                    }
                    else
                    {
                        if (isFixed)
                        {
                            var mfd1 = new MultilinesFontData
                            {
                                Item = si,
                                Flags = current_flags,
                                Font = current_font,
                                LinkID = 0,
                                Color = current_charcolor
                            };

                            ptr.Data.Add(mfd1);
                            readWidth += data[0] + data[2] + 1;

                            if (data[1] + data[3] > ptr.MaxHeight)
                                ptr.MaxHeight = data[1] + data[3];

                            charCount++;

                            ptr.Width += readWidth;
                            ptr.CharCount += charCount;
                        }

                        i = lastSpace + 1;

                        charcolor = lastspace_charcolor;
                        current_charcolor = lastspace_charcolor;
                        si = str[i];

                        if (ptr.Width <= 0)
                            ptr.Width = 1;
                        if (ptr.MaxHeight <= 0)
                            ptr.MaxHeight = 14;
                        ptr.Data.Resize(ptr.CharCount);

                        if (isFixed || isCropped)
                            break;

                        var newptr = new MultilinesFontInfo();
                        newptr.Reset();
                        ptr.Next = newptr;

                        ptr.Align = current_align;
                        ptr.CharStart = i;
                        charCount = 0;

                        if (ptr.Align == TEXT_ALIGN_TYPE.TS_LEFT && (current_flags & UOFONT_INDENTION) != 0)
                            indetionOffset = 14;
                        ptr.IndentionOffset = indetionOffset;
                        readWidth = indetionOffset;
                    }
                }

                var mfd = new MultilinesFontData
                {
                    Item = si,
                    Flags = current_flags,
                    Font = current_font,
                    LinkID = 0,
                    Color = current_charcolor
                };
                ptr.Data.Add(mfd);

                if (si == ' ')
                {
                    readWidth += UNICODE_SPACE_WIDTH;
                    if (ptr.MaxHeight <= 0)
                        ptr.MaxHeight = 5;
                }
                else
                {
                    readWidth += data[0] + data[2] + 1;
                    if (data[1] + data[3] > ptr.MaxHeight)
                        ptr.MaxHeight = data[1] + data[3];
                }

                charCount++;
            }

            ptr.Width += readWidth;
            ptr.CharCount += charCount;

            if (readWidth <= 0 && len > 0 && (str[len - 1] == '\n' || str[len - 1] == '\r'))
            {
                ptr.Width = 1;
                ptr.MaxHeight = 14;
            }

            return info;
        }

        private static unsafe (uint[], int, int, int, List<WebLinkRect>) GeneratePixelsUnicode(in byte font,
            in string str, in ushort color, in byte cell, int width, in TEXT_ALIGN_TYPE align, in ushort flags)
        {
            uint[] pData;
            if (font >= 20 || _unicodeFontAddress[font] == IntPtr.Zero)
                return (null, 0, 0, 0, null);

            var len = str.Length;
            if (len <= 0)
                return (null, 0, 0, 0, null);

            var oldWidth = width;
            if (width <= 0)
            {
                width = GetWidthUnicode(font, str);
                if (width <= 0)
                    return (null, 0, 0, 0, null);
            }

            var info = GetInfoUnicode(font, str, len, align, flags, width);
            if (info == null)
                return (null, 0, 0, 0, null);

            if (IsUsingHTML && (_leftMargin > 0 || _rightMargin > 0))
            {
                while (info != null)
                {
                    var ptr1 = info.Next;
                    info.Data.Clear();
                    info = null;
                    info = ptr1;
                }

                var newWidth = width - (_leftMargin + _rightMargin);

                if (newWidth < 10)
                    newWidth = 10;
                info = GetInfoUnicode(font, str, len, align, flags, newWidth);
                if (info == null)
                    return (null, 0, 0, 0, null);
            }

            if (oldWidth <= 0 && RecalculateWidthByInfo)
            {
                var ptr1 = info;
                width = 0;
                while (ptr1 != null)
                {
                    if (ptr1.Width > width)
                        width = ptr1.Width;
                    ptr1 = ptr1.Next;
                }
            }

            width += 4;

            var height = GetHeightUnicode(info);
            if (height <= 0)
            {
                while (info != null)
                {
                    var ptr1 = info;
                    info = info.Next;
                    ptr1.Data.Clear();
                    ptr1 = null;
                }

                return (null, 0, 0, 0, null);
            }

            height += _topMargin + _bottomMargin + 4;
            var blocksize = height * width;
            pData = new uint[blocksize];

            var table = (uint*) _unicodeFontAddress[font];
            var lineOffsY = 1 + _topMargin;

            var ptr = info;

            uint datacolor = 0;

            if (color == 0xFFFF)
                datacolor = /*0xFFFFFFFE;*/ Hues.RgbaToArgb(0xFFFFFFFE);
            else
                datacolor = /*Hues.GetPolygoneColor(cell, color) << 8 | 0xFF;*/
                    Hues.RgbaToArgb((Hues.GetPolygoneColor(cell, color) << 8) | 0xFF);

            var isItalic = (flags & UOFONT_ITALIC) != 0;
            var isSolid = (flags & UOFONT_SOLID) != 0;
            var isBlackBorder = (flags & UOFONT_BLACK_BORDER) != 0;
            var isUnderline = (flags & UOFONT_UNDERLINE) != 0;
            var blackColor = Hues.RgbaToArgb(0x010101FF);

            var isLink = false;
            var linkStartX = 0;
            var linkStartY = 0;

            var linesCount = 0;
            var links = new List<WebLinkRect>();

            while (ptr != null)
            {
                info = ptr;
                linesCount++;

                var w = _leftMargin;

                if (ptr.Align == TEXT_ALIGN_TYPE.TS_CENTER)
                {
                    w += (width - 10 - ptr.Width) / 2;
                    if (w < 0)
                        w = 0;
                }
                else if (ptr.Align == TEXT_ALIGN_TYPE.TS_RIGHT)
                {
                    w += width - 10 - ptr.Width;
                    if (w < 0)
                        w = 0;
                }
                else if (ptr.Align == TEXT_ALIGN_TYPE.TS_LEFT && (flags & UOFONT_INDENTION) != 0)
                {
                    w += ptr.IndentionOffset;
                }

                ushort oldLink = 0;

                var dataSize = ptr.Data.Count;

                for (var i = 0; i < dataSize; i++)
                {
                    var data = ptr.Data[i];
                    var si = data.Item;

                    table = (uint*) _unicodeFontAddress[data.Font];

                    if (!isLink)
                    {
                        oldLink = data.LinkID;
                        if (oldLink > 0)
                        {
                            isLink = true;
                            linkStartX = w;
                            linkStartY = lineOffsY + 3;
                        }
                    }
                    else if (data.LinkID <= 0 || i + 1 == dataSize)
                    {
                        isLink = false;
                        var linkHeight = lineOffsY - linkStartY;
                        if (linkHeight < 14)
                            linkHeight = 14;

                        var ofsX = 0;

                        if (si == ' ')
                        {
                            ofsX = UNICODE_SPACE_WIDTH;
                        }
                        else if ((table[si] <= 0 || table[si] == 0xFFFFFFFF) && si != ' ')
                        {
                        }
                        else
                        {
                            var xData = (byte*) ((IntPtr) table + (int) table[si]);
                            ofsX = (sbyte) xData[2];
                        }

                        var wlr = new WebLinkRect
                        {
                            LinkID = oldLink,
                            StartX = linkStartX,
                            StartY = linkStartY,
                            EndX = w - ofsX,
                            EndY = linkHeight
                        };

                        links.Add(wlr);
                        oldLink = 0;
                    }

                    if ((table[si] <= 0 || table[si] == 0xFFFFFFFF) && si != ' ')
                        continue;

                    var ddata = (byte*) ((IntPtr) table + (int) table[si]);
                    var offsX = 0;
                    var offsY = 0;
                    var dw = 0;
                    var dh = 0;

                    if (si == ' ')
                    {
                        offsX = 0;
                        dw = UNICODE_SPACE_WIDTH;
                    }
                    else
                    {
                        offsX = ddata[0] + 1;
                        offsY = ddata[1];
                        dw = ddata[2];
                        dh = ddata[3];

                        ddata = (byte*) ((IntPtr) ddata + 4);
                    }

                    var tmpW = w;
                    var charcolor = datacolor;
                    var isBlackPixel = ((charcolor >> 24) & 0xFF) <= 8 && ((charcolor >> 16) & 0xFF) <= 8 &&
                                       ((charcolor >> 8) & 0xFF) <= 8;
                    if (si != ' ')
                    {
                        if (IsUsingHTML && i < ptr.Data.Count)
                        {
                            isItalic = (data.Flags & UOFONT_ITALIC) != 0;
                            isSolid = (data.Flags & UOFONT_SOLID) != 0;
                            isBlackBorder = (data.Flags & UOFONT_BLACK_BORDER) != 0;
                            isUnderline = (data.Flags & UOFONT_UNDERLINE) != 0;

                            if (data.Color != 0xFFFFFFFF)
                            {
                                charcolor = data.Color;
                                isBlackPixel =
                                    ((charcolor >> 24) & 0xFF) <= 8 && ((charcolor >> 16) & 0xFF) <= 8 &&
                                    ((charcolor >> 8) & 0xFF) <= 8;
                            }
                        }

                        var scanlineCount = (dw - 1) / 8 + 1;
                        for (var y = 0; y < dh; y++)
                        {
                            var testY = offsY + lineOffsY + y;
                            if (testY >= height)
                                break;

                            var scanlines = ddata;
                            //ddata += scanlineCount;

                            ddata = (byte*) ((IntPtr) ddata + scanlineCount);

                            var italicOffset = 0;
                            if (isItalic)
                                italicOffset = (int) ((dh - y) / ITALIC_FONT_KOEFFICIENT);

                            var testX = w + offsX + italicOffset + (isSolid ? 1 : 0);

                            for (var c = 0; c < scanlineCount; c++)
                            for (var j = 0; j < 8; j++)
                            {
                                var x = c * 8 + j;
                                if (x >= dw)
                                    break;

                                var nowX = testX + x;
                                if (nowX >= width)
                                    break;

                                var cl = (byte) (scanlines[c] & (1 << (7 - j)));
                                var block = testY * width + nowX;

                                if (cl > 0)
                                    pData[block] = charcolor;
                            }
                        }

                        if (isSolid)
                        {
                            var solidColor = Hues.RgbaToArgb(blackColor);

                            if (solidColor == charcolor)
                                solidColor++;

                            var minXOk = w + offsX > 0 ? -1 : 0;
                            var maxXOk = w + offsX + dw < width ? 1 : 0;

                            maxXOk += dw;

                            for (var cy = 0; cy < dh; cy++)
                            {
                                var testY = offsY + lineOffsY + cy;

                                if (testY >= height)
                                    break;

                                var italicOffset = 0;
                                if (isItalic && cy < dh)
                                    italicOffset = (int) ((dh - cy) / ITALIC_FONT_KOEFFICIENT);

                                for (var cx = minXOk; cx < maxXOk; cx++)
                                {
                                    var testX = cx + w + offsX + italicOffset;

                                    if (testX >= width)
                                        break;

                                    var block = testY * width + testX;

                                    if (pData[block] <= 0 && pData[block] != solidColor)
                                    {
                                        var endX = cx < dw ? 2 : 1;

                                        if (endX == 2 && testX + 1 >= width)
                                            endX--;

                                        for (var x = 0; x < endX; x++)
                                        {
                                            var nowX = testX + x;

                                            var testBlock = testY * width + nowX;

                                            if (pData[testBlock] > 0 && pData[testBlock] != solidColor)
                                            {
                                                pData[block] = solidColor;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }

                            for (var cy = 0; cy < dh; cy++)
                            {
                                var testY = offsY + lineOffsY + cy;

                                if (testY >= height)
                                    break;

                                var italicOffset = 0;
                                if (isItalic)
                                    italicOffset = (int) ((dh - cy) / ITALIC_FONT_KOEFFICIENT);

                                for (var cx = 0; cx < dw; cx++)
                                {
                                    var testX = cx + w + offsX + italicOffset;

                                    if (testX >= width)
                                        break;

                                    var block = testY * width + testX;

                                    if (pData[block] == solidColor)
                                        pData[block] = charcolor;
                                }
                            }
                        }


                        if (isBlackBorder && !isBlackPixel)
                        {
                            var minXOk = w + offsX > 0 ? -1 : 0;
                            var minYOk = offsY + lineOffsY > 0 ? -1 : 0;
                            var maxXOk = w + offsX + dw < width ? 1 : 0;
                            var maxYOk = offsY + lineOffsY + dh < height ? 1 : 0;

                            maxXOk += dw;
                            maxYOk += dh;

                            for (var cy = minYOk; cy < maxYOk; cy++)
                            {
                                var testY = offsY + lineOffsY + cy;

                                if (testY >= height)
                                    break;

                                var italicOffset = 0;
                                if (isItalic && cy >= 0 && cy < dh)
                                    italicOffset = (int) ((dh - cy) / ITALIC_FONT_KOEFFICIENT);

                                for (var cx = minXOk; cx < maxXOk; cx++)
                                {
                                    var testX = cx + w + offsX + italicOffset;

                                    if (testX >= width)
                                        break;

                                    var block = testY * width + testX;

                                    if (pData[block] <= 0 && pData[block] != blackColor)
                                    {
                                        var startX = cx > 0 ? -1 : 0;
                                        var startY = cy > 0 ? -1 : 0;
                                        var endX = cx < dw - 1 ? 2 : 1;
                                        var endY = cy < dh - 1 ? 2 : 1;

                                        if (endX == 2 && testX + 1 >= width)
                                            endX--;

                                        var passed = false;

                                        for (var x = startX; x < endX; x++)
                                        {
                                            var nowX = testX + x;
                                            for (var y = startY; y < endY; y++)
                                            {
                                                var testBlock = (testY + y) * width + nowX;
                                                if (pData[testBlock] > 0 && pData[testBlock] != blackColor)
                                                {
                                                    pData[block] = blackColor;

                                                    passed = true;

                                                    break;
                                                }
                                            }

                                            if (passed)
                                                break;
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
                            isUnderline = (data.Flags & UOFONT_UNDERLINE) != 0;
                            if (data.Color != 0xFFFFFFFF)
                            {
                                charcolor = data.Color;
                                isBlackPixel =
                                    ((charcolor >> 24) & 0xFF) <= 8 && ((charcolor >> 16) & 0xFF) <= 8 &&
                                    ((charcolor >> 8) & 0xFF) <= 8;
                            }
                        }
                    }

                    if (isUnderline)
                    {
                        var minXOk = tmpW + offsX > 0 ? -1 : 0;
                        var maxXOk = w + offsX + dw < width ? 1 : 0;

                        var aData = (byte*) ((IntPtr) table + (int) table[(byte) 'a']);

                        var testY = lineOffsY + aData[1] + aData[3];

                        if (testY >= height)
                            break;

                        for (var cx = minXOk; cx < dw + maxXOk; cx++)
                        {
                            var testX = cx + tmpW + offsX + (isSolid ? 1 : 0);

                            if (testX >= width)
                                break;

                            var block = testY * width + testX;

                            pData[block] = charcolor;
                        }
                    }
                }

                lineOffsY += ptr.MaxHeight;
                ptr = ptr.Next;
                info.Data.Clear();
                info = null;
            }

            if (IsUsingHTML && _HTMLBackgroundCanBeColored && _backgroundColor > 0)
            {
                _backgroundColor |= 0xFF;

                for (var y = 0; y < height; y++)
                {
                    var yPos = y * width;
                    for (var x = 0; x < width; x++)
                        if (pData[yPos + x] <= 0)
                            pData[yPos + x] = Hues.RgbaToArgb(_backgroundColor);
                }
            }

            return (pData, width, height, linesCount, links);
        }

        private static unsafe MultilinesFontInfo GetInfoHTML(in byte font, in string str, int len,
            in TEXT_ALIGN_TYPE align, in ushort flags, in int width)
        {
            var htmlData = GetHTMLData(font, str, ref len, align, flags);

            if (htmlData.Length <= 0)
                return null;

            var info = new MultilinesFontInfo();
            info.Reset();
            info.Align = align;

            var ptr = info;
            var indentionOffset = 0;

            ptr.IndentionOffset = indentionOffset;

            var charCount = 0;
            var lastSpace = 0;
            var readWidth = 0;

            var isFixed = (flags & UOFONT_FIXED) != 0;
            var isCropped = (flags & UOFONT_CROPPED) != 0;

            if (len > 0)
                ptr.Align = htmlData[0].Align;

            for (var i = 0; i < len; i++)
            {
                var si = htmlData[i].Char;
                var table = (uint*) _unicodeFontAddress[htmlData[i].Font];

                if ((byte) si == 0x000D || si == '\n')
                {
                    if ((byte) si == 0x000D || isFixed || isCropped)
                        si = (char) 0;
                    else si = '\n';
                }

                if ((table[si] <= 0 || table[si] == 0xFFFFFFFF) && si != ' ' && si != '\n')
                    continue;

                var data = (byte*) ((IntPtr) table + (int) table[(byte) si]);

                if (si == ' ')
                {
                    lastSpace = i;
                    ptr.Width += readWidth;
                    readWidth = 0;
                    ptr.CharCount += charCount;
                    charCount = 0;
                }

                var solidWidth = htmlData[i].Flags & UOFONT_SOLID;
                if (ptr.Width + readWidth + data[0] + data[2] + solidWidth > width || si == '\n')
                {
                    if (lastSpace == ptr.CharStart && lastSpace <= 0 && si != '\n')
                        ptr.CharStart = 1;

                    if (si == '\n')
                    {
                        ptr.Width += readWidth;
                        ptr.CharCount += charCount;

                        lastSpace = i;

                        if (ptr.Width <= 0)
                            ptr.Width = 1;

                        ptr.MaxHeight = MAX_HTML_TEXT_HEIGHT;

                        ptr.Data.Resize(ptr.CharCount);

                        var newptr = new MultilinesFontInfo();
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
                            ptr.Width = 1;

                        ptr.MaxHeight = MAX_HTML_TEXT_HEIGHT;

                        var newptr = new MultilinesFontInfo();
                        newptr.Reset();
                        ptr.Next = newptr;
                        ptr = newptr;

                        ptr.Align = htmlData[i].Align;
                        ptr.CharStart = i;
                        lastSpace = i - 1;
                        charCount = 0;

                        if (ptr.Align == TEXT_ALIGN_TYPE.TS_LEFT && (htmlData[i].Flags & UOFONT_INDENTION) != 0)
                            indentionOffset = 14;

                        ptr.IndentionOffset = indentionOffset;
                        readWidth = indentionOffset;
                    }
                    else
                    {
                        if (isFixed)
                        {
                            var mfd1 = new MultilinesFontData
                            {
                                Item = si,
                                Flags = htmlData[i].Flags,
                                Font = htmlData[i].Font,
                                LinkID = htmlData[i].LinkID,
                                Color = htmlData[i].Color
                            };

                            ptr.Data.Add(mfd1);
                            ;
                            readWidth += (sbyte) data[0] + (sbyte) data[2] + 1;
                            ptr.MaxHeight = MAX_HTML_TEXT_HEIGHT;

                            charCount++;

                            ptr.Width += readWidth;
                            ptr.CharCount += charCount;
                        }


                        i = lastSpace + 1;

                        si = htmlData[i].Char;
                        solidWidth = htmlData[i].Flags & UOFONT_SOLID;

                        if (ptr.Width <= 0)
                            ptr.Width = 1;

                        ptr.MaxHeight = MAX_HTML_TEXT_HEIGHT;

                        ptr.Data.Resize(ptr.CharCount);
                        charCount = 0;

                        if (isFixed || isCropped)
                            break;

                        var newptr = new MultilinesFontInfo();
                        newptr.Reset();
                        ptr.Next = newptr;
                        ptr = newptr;
                        ptr.Align = htmlData[i].Align;
                        ptr.CharCount = i;

                        if (ptr.Align == TEXT_ALIGN_TYPE.TS_LEFT && (htmlData[i].Flags & UOFONT_INDENTION) != 0)
                            indentionOffset = 14;
                        ptr.IndentionOffset = indentionOffset;
                        readWidth = indentionOffset;
                    }
                }

                var mfd = new MultilinesFontData
                {
                    Item = si,
                    Flags = htmlData[i].Flags,
                    Font = htmlData[i].Font,
                    LinkID = htmlData[i].LinkID,
                    Color = htmlData[i].Color
                };
                ptr.Data.Add(mfd);

                if (si == ' ')
                    readWidth += UNICODE_SPACE_WIDTH;
                else
                    readWidth += data[0] + data[2] + 1 + solidWidth;

                charCount++;
            }

            ptr.Width += readWidth;
            ptr.CharCount += charCount;
            ptr.MaxHeight = MAX_HTML_TEXT_HEIGHT;

            return info;
        }

        private static HTMLChar[] GetHTMLData(in byte font, in string str, ref int len, in TEXT_ALIGN_TYPE align,
            in ushort flags)
        {
            var data = new HTMLChar[0];

            if (len < 1)
                return data;

            data = new HTMLChar[len];

            var newlen = 0;

            var info = new HTMLDataInfo
            {
                Tag = HTML_TAG_TYPE.HTT_NONE,
                Align = align,
                Flags = flags,
                Font = font,
                Color = _HTMLColor,
                Link = 0
            };
            var stack = new List<HTMLDataInfo>();
            stack.Add(info);

            var currentInfo = info;

            for (var i = 0; i < len; i++)
            {
                var si = str[i];

                if (si == '<')
                {
                    var endTag = false;
                    var newInfo = new HTMLDataInfo
                    {
                        Tag = HTML_TAG_TYPE.HTT_NONE,
                        Align = TEXT_ALIGN_TYPE.TS_LEFT,
                        Flags = 0,
                        Font = 0xFF,
                        Color = 0,
                        Link = 0
                    };

                    var tag = ParseHTMLTag(str, len, ref i, ref endTag, newInfo);

                    if (tag == HTML_TAG_TYPE.HTT_NONE)
                        continue;

                    if (!endTag)
                    {
                        if (newInfo.Font == 0xFF)
                            newInfo.Font = stack.LastOrDefault().Font;

                        if (tag != HTML_TAG_TYPE.HTT_BODY)
                        {
                            stack.Add(newInfo);
                        }
                        else
                        {
                            stack.Clear();
                            newlen = 0;

                            if (newInfo.Color > 0)
                                info.Color = newInfo.Color;
                            stack.Add(info);
                        }
                    }
                    else if (stack.Count > 1)
                    {
                        var index = -1;
                        for (var j = stack.Count - 1; j > 1; j--)
                            if (stack[j].Tag == tag)
                            {
                                stack.RemoveAt(j); // MAYBE ERROR?
                                break;
                            }
                    }

                    currentInfo = GetCurrentHTMLInfo(stack);

                    switch (tag)
                    {
                        case HTML_TAG_TYPE.HTT_LEFT:
                        case HTML_TAG_TYPE.HTT_CENTER:
                        case HTML_TAG_TYPE.HTT_RIGHT:
                            if (newlen > 0) endTag = true;
                            goto case HTML_TAG_TYPE.HTT_P;
                        case HTML_TAG_TYPE.HTT_P:
                            if (endTag)
                                si = '\n';
                            else
                                si = (char) 0;
                            break;
                        case HTML_TAG_TYPE.HTT_BR:
                        case HTML_TAG_TYPE.HTT_BQ:
                            si = '\n';
                            break;
                        default:
                            si = (char) 0;
                            break;
                    }
                }

                if ((byte) si > 0)
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

        private static HTMLDataInfo GetCurrentHTMLInfo(in List<HTMLDataInfo> list)
        {
            var info = new HTMLDataInfo
            {
                Tag = HTML_TAG_TYPE.HTT_NONE,
                Align = TEXT_ALIGN_TYPE.TS_LEFT,
                Flags = 0,
                Font = 0xFF,
                Color = 0,
                Link = 0
            };

            for (var i = 0; i < list.Count; i++)
            {
                var current = list[i];

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
                        break;
                    case HTML_TAG_TYPE.HTT_A:
                        info.Flags |= current.Flags;
                        info.Color = current.Color;
                        info.Link = current.Link;
                        break;
                    case HTML_TAG_TYPE.HTT_BIG:
                    case HTML_TAG_TYPE.HTT_SMALL:
                        if (current.Font == 0xFF && _unicodeFontAddress[current.Font] != IntPtr.Zero)
                            info.Font = current.Font;
                        break;
                    case HTML_TAG_TYPE.HTT_BASEFONT:
                        if (current.Font != 0xFF && _unicodeFontAddress[current.Font] != IntPtr.Zero)
                            info.Font = current.Font;

                        if (current.Color != 0)
                            info.Color = current.Color;
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
                            info.Font = current.Font;
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

        private static HTML_TAG_TYPE ParseHTMLTag(in string str, in int len, ref int i, ref bool endTag,
            HTMLDataInfo info)
        {
            var tag = HTML_TAG_TYPE.HTT_NONE;
            i++;

            if (i < len && str[i] == '/')
            {
                endTag = true;
                i++;
            }

            while (str[i] == ' ' && i < len)
                i++;

            var j = i;
            for (; i < len; i++)
                if (str[i] == ' ' || str[i] == '>')
                    break;

            if (j != i && i < len)
            {
                var cmdLen = i - j;
                var cmd = str.Substring(j, cmdLen);

                cmd = cmd.ToLower();
                j = i;

                while (str[i] != '>' && i < len)
                    i++;

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
                }


                if (!endTag)
                {
                    info = GetHTMLInfoFromTag(tag);

                    if (i < len && j != i)
                        switch (tag)
                        {
                            case HTML_TAG_TYPE.HTT_BODY:
                            case HTML_TAG_TYPE.HTT_BASEFONT:
                            case HTML_TAG_TYPE.HTT_A:
                            case HTML_TAG_TYPE.HTT_DIV:

                                var content = "";
                                cmdLen = i - j;
                                content = content.Substring(j, cmdLen);

                                if (content.Length > 0)
                                    GetHTMLInfoFromContent(ref info, content);

                                break;
                        }
                }
            }

            return tag;
        }

        private static void GetHTMLInfoFromContent(ref HTMLDataInfo info, in string content)
        {
            var strings = content.Split(new[] {' ', '=', '\\'}, StringSplitOptions.RemoveEmptyEntries);
            var size = strings.Length;

            for (var i = 0; i < size; i += 2)
            {
                if (i + 1 >= size)
                    break;

                var str = strings[i].ToLower();
                var value = strings[i + 1];
                TrimHTMLString(ref value);

                if (value.Length <= 0)
                    continue;

                switch (info.Tag)
                {
                    case HTML_TAG_TYPE.HTT_BODY:

                        switch (str)
                        {
                            case "text":
                                info.Color = GetHTMLColorFromText(ref value);
                                break;
                            case "bgcolor":
                                if (_HTMLBackgroundCanBeColored)
                                    _backgroundColor = GetHTMLColorFromText(ref value);
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
                            var font = byte.Parse(value);
                            if (font == 0 || font == 4)
                                info.Font = 1;
                            else if (font < 4)
                                info.Font = 2;
                            else
                                info.Font = 0;
                        }

                        break;
                    case HTML_TAG_TYPE.HTT_A:
                        if (str == "href")
                        {
                            info.Flags = UOFONT_UNDERLINE;
                            info.Color = _webLinkColor;
                            info.Link = GetWebLinkID(value, ref info.Color);
                        }

                        break;
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

        private static ushort GetWebLinkID(in string link, ref uint color)
        {
            ushort linkID = 0;
            KeyValuePair<ushort, WebLink>? l = null;

            foreach (var ll in _webLinks)
                if (ll.Value.Link == link)
                {
                    l = ll;
                    break;
                }

            if (l == null || !l.HasValue)
            {
                linkID = (ushort) (_webLinks.Count + 1);
                _webLinks[linkID] = new WebLink {IsVisited = false, Link = link};
            }
            else
            {
                if (l.Value.Value.IsVisited)
                    color = _visitedWebLinkColor;
                linkID = l.Value.Key;
            }

            return linkID;
        }

        private static unsafe uint GetHTMLColorFromText(ref string str)
        {
            uint color = 0;

            if (str.Length > 1)
            {
                if (str[0] == '#')
                {
                    color = str.Substring(1).StartsWith("0x")
                        ? Convert.ToUInt32(str.Substring(3), 16)
                        : Convert.ToUInt32(str.Substring(1), 10);

                    var clrBuf = (byte*) color;
                    color = (uint) ((clrBuf[0] << 24) | (clrBuf[1] << 16) | (clrBuf[2] << 8) | 0xFF);
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
                        case "blackv":
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

        private static void TrimHTMLString(ref string str)
        {
            if (str.Length >= 2 && str[0] == '"' && str[str.Length - 1] == '"')
                str = str.Remove(str.Length - 1).Remove(0);
        }

        private static HTMLDataInfo GetHTMLInfoFromTag(in HTML_TAG_TYPE tag)
        {
            var info = new HTMLDataInfo
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

        private static int GetHeightUnicode(MultilinesFontInfo info)
        {
            var textHeight = 0;
            for (; info != null; info = info.Next)
                if (IsUsingHTML)
                    textHeight += MAX_HTML_TEXT_HEIGHT;
                else
                    textHeight += info.MaxHeight;

            return textHeight;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FontHeader
    {
        public byte Width, Height, Unknown;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FontCharacter
    {
        public byte Width, Height, Unknown;
    }

    public struct FontCharacterData
    {
        public byte Width, Height;
        public List<ushort> Data;
    }

    public struct FontData
    {
        public byte Header;

        // 224
        public FontCharacterData[] Chars;
    }

    public class MultilinesFontInfo
    {
        public TEXT_ALIGN_TYPE Align;
        public int CharCount;
        public int CharStart;
        public List<MultilinesFontData> Data = new List<MultilinesFontData>();
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

    public class MultilinesFontData
    {
        public uint Color;
        public ushort Flags;
        public byte Font;
        public char Item;
        public ushort LinkID;

        public MultilinesFontData Next;
    }

    public struct WebLinkRect
    {
        public ushort LinkID;
        public int StartX, StartY, EndX, EndY;
    }

    public struct WebLink
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