#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//      
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Collections;
using ClassicUO.Utility.Logging;

namespace ClassicUO.IO.Resources
{
    internal class FontsLoader : ResourceLoader
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
        private const int UNICODE_SPACE_WIDTH = 8;
        private const int MAX_HTML_TEXT_HEIGHT = 18;
        private const float ITALIC_FONT_KOEFFICIENT = 3.3f;
        private readonly byte[] _fontIndex =
        {
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 136, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 152, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 160, 161, 162, 163, 164, 165, 166, 167, 168, 169, 170, 171, 172, 173, 174, 175, 176, 177, 178, 179, 180, 181, 182, 183, 184, 185, 186, 187, 188, 189, 190, 191, 192, 193, 194, 195, 196, 197, 198, 199, 200, 201, 202, 203, 204, 205, 206, 207, 208, 209, 210, 211, 212, 213, 214, 215, 216, 217, 218, 219, 220, 221, 222, 223
        };
        private readonly IntPtr[] _unicodeFontAddress = new IntPtr[20];
        private readonly long[] _unicodeFontSize = new long[20];
        private readonly Dictionary<ushort, WebLink> _webLinks = new Dictionary<ushort, WebLink>();
        private readonly int[] offsetCharTable =
        {
            2, 0, 2, 2, 0, 0, 2, 2, 0, 0
        };
        private readonly int[] offsetSymbolTable =
        {
            1, 0, 1, 1, -1, 0, 1, 1, 0, 0
        };
        private uint _backgroundColor;
        private FontData[] _font;
        private bool _HTMLBackgroundCanBeColored;
        private uint _HTMLColor = 0xFFFFFFFF;
        private int _leftMargin, _topMargin, _rightMargin, _bottomMargin;
        private uint _visitedWebLinkColor;
        private uint _webLinkColor;

        public int FontCount { get; private set; }

        public bool UnusePartialHue { get; set; } = false;

        public bool RecalculateWidthByInfo { get; set; } = false;

        public bool IsUsingHTML { get; set; }



        public override Task Load()
        {
            return Task.Run(() =>
            {
                UOFileMul fonts = new UOFileMul(Path.Combine(FileManager.UoFolderPath, "fonts.mul"));
                UOFileMul[] uniFonts = new UOFileMul[20];

                for (int i = 0; i < 20; i++)
                {
                    string path = Path.Combine(FileManager.UoFolderPath, "unifont" + (i == 0 ? "" : i.ToString()) + ".mul");

                    if (File.Exists(path))
                    {
                        uniFonts[i] = new UOFileMul(path);
                        _unicodeFontAddress[i] = uniFonts[i].StartAddress;
                        _unicodeFontSize[i] = uniFonts[i].Length;
                    }
                }

                int fontHeaderSize = UnsafeMemoryManager.SizeOf<FontHeader>();
                FontCount = 0;

                while (fonts.Position < fonts.Length)
                {
                    bool exit = false;
                    fonts.Skip(1);

                    unsafe
                    {
                        for (int i = 0; i < 224; i++)
                        {
                            FontHeader* fh = (FontHeader*) fonts.PositionAddress;

                            if (fonts.Position + fontHeaderSize >= fonts.Length)
                                continue;

                            fonts.Skip(fontHeaderSize);
                            int bcount = fh->Width * fh->Height * 2;

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

                for (int i = 0; i < FontCount; i++)
                {
                    byte header = fonts.ReadByte();

                    FontCharacterData[] datas = new FontCharacterData[224];

                    for (int j = 0; j < 224; j++)
                    {
                        if (fonts.Position + 3 >= fonts.Length)
                            continue;

                        byte w = fonts.ReadByte();
                        byte h = fonts.ReadByte();
                        fonts.Skip(1);
                        ushort[] data = fonts.ReadArray<ushort>(w * h);

                        datas[j] = new FontCharacterData(w, h, data);
                    }

                    _font[i] = new FontData(header, datas);
                }

                if (_unicodeFontAddress[1] == IntPtr.Zero)
                {
                    _unicodeFontAddress[1] = _unicodeFontAddress[0];
                    _unicodeFontSize[1] = _unicodeFontSize[0];
                }

                for (int i = 0; i < 256; i++)
                {
                    if (_fontIndex[i] >= 0xE0)
                        _fontIndex[i] = _fontIndex[' '];
                }
            });
        }

        public override void CleanResources()
        {
            // do nothing
        }


        public bool UnicodeFontExists(byte font)
        {
            return font < 20 && _unicodeFontAddress[font] != IntPtr.Zero;
        }


        public (int, int) MeasureText(string text, byte font, bool isunicode, TEXT_ALIGN_TYPE align, ushort flags, int maxWidth = 200)
        {
            int width, height;

            if (isunicode)
            {
                width = GetWidthUnicode(font, text);

                if (width > maxWidth)
                    width = GetWidthExUnicode(font, text, maxWidth, align, flags);

                height = GetHeightUnicode(font, text, width, align, flags);
            }
            else
            {
                width = GetWidthASCII(font, text);

                if (width > maxWidth)
                    width = GetWidthExASCII(font, text, maxWidth, align, flags);

                height = GetHeightASCII(font, text, width, align, flags);
            }

            return (width, height);
        }


        public int GetWidthASCII(byte font, string str)
        {
            if (font >= FontCount || string.IsNullOrEmpty(str))
                return 0;

            FontData fd = _font[font];
            int textLength = 0;

            foreach (char c in str)
                textLength += fd.Chars[_fontIndex[(byte) c]].Width;

            return textLength;
        }

        public int GetWidthExASCII(byte font, string text, int maxwidth, TEXT_ALIGN_TYPE align, ushort flags)
        {
            if (font > FontCount || string.IsNullOrEmpty(text))
                return 0;

            MultilinesFontInfo info = GetInfoASCII(font, text, text.Length, align, flags, maxwidth);
            int textWidth = 0;

            while (info != null)
            {
                if (info.Width > textWidth)
                    textWidth = info.Width;
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
                width = GetWidthASCII(font, str);

            MultilinesFontInfo info = GetInfoASCII(font, str, str.Length, align, flags, width);

            int textHeight = 0;

            while (info != null)
            {
                if (IsUsingHTML)
                    textHeight += MAX_HTML_TEXT_HEIGHT;
                else
                    textHeight += info.MaxHeight;
                MultilinesFontInfo ptr = info;
                info = info.Next;
                ptr.Data.Clear();
                ptr = null;
            }

            return textHeight;
        }

        public void GenerateASCII(ref FontTexture texture, byte font, string str, ushort color, int width, TEXT_ALIGN_TYPE align, ushort flags, out bool isPartial, bool saveHitmap, int height)
        {
            isPartial = false;

            if (string.IsNullOrEmpty(str))
                return;

            if ((flags & UOFONT_FIXED) != 0 || (flags & UOFONT_CROPPED) != 0 || (flags & UOFONT_CROPTEXTURE) != 0)
            {
                if (width == 0 || string.IsNullOrEmpty(str))
                    return;

                int realWidth = GetWidthASCII(font, str);

                if (realWidth > width)
                {
                    string newstr = GetTextByWidthASCII(font, str, width, (flags & UOFONT_CROPPED) != 0, align, flags);
                    if((flags & UOFONT_CROPTEXTURE) != 0)
                    {
                        int totalheight = 0;
                        while(totalheight < height)
                        {
                            totalheight += GetHeightASCII(font, newstr, width, align, flags);
                            if (str.Length > newstr.Length)
                                newstr += GetTextByWidthASCII(font, str.Substring(newstr.Length), width, (flags & UOFONT_CROPPED) != 0, align, flags);
                            else
                                break;
                        }
                    }
                    GeneratePixelsASCII(ref texture, font, newstr, color, width, align, flags, out isPartial, saveHitmap);

                    return;
                }
            }

            GeneratePixelsASCII(ref texture, font, str, color, width, align, flags, out isPartial, saveHitmap);
        }

        private string GetTextByWidthASCII(byte font, string str, int width, bool isCropped, TEXT_ALIGN_TYPE align, ushort flags)
        {
            if (font >= FontCount || string.IsNullOrEmpty(str))
                return string.Empty;

            ref readonly FontData fd = ref _font[font];

            StringBuilder sb = new StringBuilder();

            if (IsUsingHTML)
            {
                int strLen = str.Length;

                GetHTMLData(font, str, ref strLen, align, flags);
                int size = str.Length - strLen;
                if (size > 0)
                {
                    sb.Append(str.Substring(0, size));
                    str = str.Substring(str.Length - strLen, strLen);

                    if (GetWidthASCII(font, str) < width)
                        isCropped = false;
                }
            }

            if (isCropped)
                width -= fd.Chars[_fontIndex[(byte) '.']].Width * 3;
            int textLength = 0;

            foreach (char c in str)
            {
                textLength += fd.Chars[_fontIndex[(byte) c]].Width;

                if (textLength > width)
                    break;

                sb.Append(c);
            }

            if (isCropped)
                sb.Append("...");

            return sb.ToString();
        }

        private void GeneratePixelsASCII(ref FontTexture texture, byte font, string str, ushort color, int width, TEXT_ALIGN_TYPE align, ushort flags, out bool isPartial, bool saveHitmap)
        {
            isPartial = false;

            if (font >= FontCount)
                return;

            int len = str.Length;

            if (len == 0)
                return;

            ref readonly FontData fd = ref _font[font];

            if (width <= 0)
                width = GetWidthASCII(font, str);

            if (width <= 0)
                return ;

            MultilinesFontInfo info = GetInfoASCII(font, str, len, align, flags, width);

            if (info == null)
                return;

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
            isPartial = font != 5 && font != 8 && !UnusePartialHue;
            int font6OffsetY = font == 6 ? 7 : 0;
            int linesCount = 0; // this value should be added to TextTexture.LinesCount += linesCount

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
                            w = 0;

                        break;
                    }

                    case TEXT_ALIGN_TYPE.TS_RIGHT:

                    {
                        w = width - 10 - ptr.Width;

                        if (w == 0)
                            w = 0;

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
                    ref readonly FontCharacterData fcd = ref fd.Chars[_fontIndex[index]];
                    int dw = fcd.Width;
                    int dh = fcd.Height;
                    ushort charColor = color;

                    for (int y = 0; y < dh; y++)
                    {
                        int testrY = y + lineOffsY + offsY;

                        if (testrY >= height)
                            break;

                        for (int x = 0; x < dw; x++)
                        {
                            if (x + w >= width)
                                break;

                            ushort pic = fcd.Data[y * dw + x];

                            if (pic != 0)
                            {
                                uint pcl = 0;

                                if (isPartial)
                                    pcl = FileManager.Hues.GetPartialHueColor(pic, charColor) | 0xFF000000;
                                else
                                    pcl = FileManager.Hues.GetColor(pic, charColor) | 0xFF000000;
                                int block = testrY * width + x + w;
                                pData[block] = pcl; //HuesHelper.RgbaToArgb((pcl << 8) | 0xFF);
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
                texture = new FontTexture(width, height, linesCount, new List<WebLinkRect>());
            else
            {
                texture.Links.Clear();
                texture.LinesCount = linesCount;
            }

            texture.PushData(pData);
        }

        public int GetFontOffsetY(byte font, byte index)
        {
            if (index == 0xB8)
                return 1;

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

        public MultilinesFontInfo GetInfoASCII(byte font, string str, int len, TEXT_ALIGN_TYPE align, ushort flags, int width, bool countret = false, bool countspaces = false)
        {
            if (font >= FontCount)
                return null;

            ref readonly FontData fd = ref _font[font];
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

                ref readonly FontCharacterData fcd = ref fd.Chars[_fontIndex[(byte) si]];

                if (si == '\n' || ptr.Width + readWidth + fcd.Width > width)
                {
                    if (lastSpace == ptr.CharStart && lastSpace == 0 && si != '\n')
                        ptr.CharStart = 1;

                    if (si == '\n')
                    {
                        ptr.Width += readWidth;
                        ptr.CharCount += charCount + newlineval;
                        lastSpace = i;

                        if (ptr.Width == 0)
                            ptr.Width = 1;

                        if (ptr.MaxHeight == 0)
                            ptr.MaxHeight = 14;
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

                    if (lastSpace + 1 == ptr.CharStart && !isFixed && !isCropped)
                    {
                        ptr.Width += readWidth;
                        ptr.CharCount += charCount;

                        if (ptr.Width == 0)
                            ptr.Width = 1;

                        if (ptr.MaxHeight == 0)
                            ptr.MaxHeight = 14;
                        MultilinesFontInfo newptr = new MultilinesFontInfo();
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
                            MultilinesFontData mfd1 = new MultilinesFontData(0xFFFFFFFF, flags, font, si, 0);

                            ptr.Data.Add(mfd1);
                            readWidth += fcd.Width;

                            if (fcd.Height > ptr.MaxHeight)
                                ptr.MaxHeight = fcd.Height;
                            charCount++;
                            ptr.Width += readWidth;
                            ptr.CharCount += charCount;
                        }

                        i = lastSpace + 1;
                        si = i < len ? str[i] : '\0';

                        if (ptr.Width == 0)
                            ptr.Width = 1;
                        else if (countspaces && si != '\0' && lastSpace - ptr.CharStart == ptr.CharCount)
                            ptr.CharCount++;

                        if (ptr.MaxHeight == 0)
                            ptr.MaxHeight = 14;
                        ptr.Data.Resize( (uint) ptr.CharCount);
                        charCount = 0;

                        if (isFixed || isCropped)
                            break;

                        MultilinesFontInfo newptr = new MultilinesFontInfo();
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

                MultilinesFontData mfd = new MultilinesFontData(0xFFFFFFFF, flags, font, si, 0);
                ptr.Data.Add(mfd);
                readWidth += fcd.Width;

                if (fcd.Height > ptr.MaxHeight)
                    ptr.MaxHeight = fcd.Height;
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
                        ptr.MaxHeight = ptr.MaxHeight + 2;
                    else
                        ptr.MaxHeight = ptr.MaxHeight + 6;
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

        public void GenerateUnicode(ref FontTexture texture, byte font, string str, ushort color, byte cell, int width, TEXT_ALIGN_TYPE align, ushort flags, bool saveHitmap, int height)
        {
            if (string.IsNullOrEmpty(str))
                return;

            if ((flags & UOFONT_FIXED) != 0 || (flags & UOFONT_CROPPED) != 0 || (flags & UOFONT_CROPTEXTURE) != 0)
            {
                if (width == 0 || string.IsNullOrEmpty(str))
                    return;

                int realWidth = GetWidthUnicode(font, str);

                if (realWidth > width)
                {
                    string newstr = GetTextByWidthUnicode(font, str, width, (flags & UOFONT_CROPPED) != 0, align, flags);
                    if ((flags & UOFONT_CROPTEXTURE) != 0)
                    {
                        int totalheight = 0;
                        while (totalheight < height)
                        {
                            totalheight += GetHeightUnicode(font, newstr, width, align, flags);
                            if (str.Length > newstr.Length)
                                newstr += GetTextByWidthUnicode(font, str.Substring(newstr.Length), width, (flags & UOFONT_CROPPED) != 0, align, flags);
                            else
                                break;
                        }
                    }
                    GeneratePixelsUnicode(ref texture, font, newstr, color, cell, width, align, flags, saveHitmap);

                    return;
                }
            }

            GeneratePixelsUnicode(ref texture, font, str, color, cell, width, align, flags, saveHitmap);
        }

        public unsafe string GetTextByWidthUnicode(byte font, string str, int width, bool isCropped, TEXT_ALIGN_TYPE align, ushort flags)
        {
            if (font >= 20 || _unicodeFontAddress[font] == IntPtr.Zero || string.IsNullOrEmpty(str))
                return string.Empty;

            uint* table = (uint*) _unicodeFontAddress[font];

            StringBuilder sb = new StringBuilder();

            if (IsUsingHTML)
            {
                int strLen = str.Length;

                GetHTMLData(font, str, ref strLen, align, flags);
                int size = str.Length - strLen;
                if (size > 0)
                {
                    sb.Append(str.Substring(0, size));
                    str = str.Substring(str.Length - strLen, strLen);

                    if (GetWidthUnicode(font, str) < width)
                        isCropped = false;
                }
            }

            if (isCropped)
            {
                uint offset = table['.'];

                if (offset != 0 && offset != 0xFFFFFFFF)
                    width -= *(byte*)((IntPtr)table + (int)offset + 2) * 3 + 3;
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
                else if (c == ' ') charWidth = UNICODE_SPACE_WIDTH;

                if (charWidth != 0)
                {
                    textLength += charWidth;

                    if (textLength > width)
                        break;

                    sb.Append(c);
                }
            }

            if (isCropped)
                sb.Append("...");

            return sb.ToString();
        }

        public unsafe int GetWidthUnicode(byte font, string str)
        {
            if (font >= 20 || _unicodeFontAddress[font] == IntPtr.Zero || string.IsNullOrEmpty(str))
                return 0;

            uint* table = (uint*) _unicodeFontAddress[font];
            int textLength = 0;
            int maxTextLenght = 0;

            foreach (char c in str)
            {
                uint offset = table[c];

                if (offset != 0 && offset != 0xFFFFFFFF)
                {
                    byte* ptr = (byte*) ((IntPtr) table + (int) offset);
                    textLength += (sbyte) ptr[0] + (sbyte) ptr[2] + 1;
                }
                else if (c == ' ')
                    textLength += UNICODE_SPACE_WIDTH;
                else if (c == '\n' || c == '\r')
                {
                    maxTextLenght = Math.Max(maxTextLenght, textLength);
                    textLength = 0;
                }
            }

            return Math.Max(maxTextLenght, textLength);
        }

        public int GetWidthExUnicode(byte font, string text, int maxwidth, TEXT_ALIGN_TYPE align, ushort flags)
        {
            if (font >= 20 || _unicodeFontAddress[font] == IntPtr.Zero || string.IsNullOrEmpty(text))
                return 0;

            MultilinesFontInfo info = GetInfoUnicode(font, text, text.Length, align, flags, maxwidth);
            int textWidth = 0;

            while (info != null)
            {
                if (info.Width > textWidth)
                    textWidth = info.Width;
                MultilinesFontInfo ptr = info;
                info = info.Next;
                ptr.Data.Clear();
                ptr = null;
            }

            return textWidth;
        }

        private unsafe MultilinesFontInfo GetInfoUnicode(byte font, string str, int len, TEXT_ALIGN_TYPE align, ushort flags, int width, bool countret = false, bool countspaces = false)
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

            if (IsUsingHTML)
                return GetInfoHTML(font, str, len, align, flags, width);

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

                if (si == '\r' || si == '\n')
                {
                    if (isFixed || isCropped)
                        si = (char) 0;
                    else
                        si = '\n';
                }

                if ((table[si] == 0 || table[si] == 0xFFFFFFFF) && si != ' ' && si != '\n')
                    continue;

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

                if (ptr.Width + readWidth + (sbyte) data[0] + (sbyte) data[2] > width || si == '\n')
                {
                    if (lastSpace == ptr.CharStart && lastSpace == 0 && si != '\n')
                        ptr.CharStart = 1;

                    if (si == '\n')
                    {
                        ptr.Width += readWidth;
                        ptr.CharCount += charCount + newlineval;
                        lastSpace = i;

                        if (ptr.Width == 0)
                            ptr.Width = 1;

                        if (ptr.MaxHeight == 0)
                            ptr.MaxHeight = 14 + extraheight;

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

                    if (lastSpace + 1 == ptr.CharStart && !isFixed && !isCropped)
                    {
                        ptr.Width += readWidth;
                        ptr.CharCount += charCount;

                        if (ptr.Width == 0)
                            ptr.Width = 1;

                        if (ptr.MaxHeight == 0)
                            ptr.MaxHeight = 14 + extraheight;
                        MultilinesFontInfo newptr = new MultilinesFontInfo();
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
                            MultilinesFontData mfd1 = new MultilinesFontData(current_charcolor, current_flags, current_font, si, 0);
                            ptr.Data.Add(mfd1);
                            readWidth += (sbyte) data[0] + (sbyte) data[2] + 1;

                            if ((sbyte) data[1] + (sbyte) data[3] > ptr.MaxHeight)
                                ptr.MaxHeight = (sbyte) data[1] + (sbyte) data[3] + extraheight;
                            charCount++;
                            ptr.Width += readWidth;
                            ptr.CharCount += charCount;
                        }

                        i = lastSpace + 1;
                        charcolor = lastspace_charcolor;
                        current_charcolor = lastspace_charcolor;
                        si = i < str.Length ? str[i] : '\0';

                        if (ptr.Width == 0)
                            ptr.Width = 1;
                        else if (countspaces && si != '\0' && lastSpace - ptr.CharStart == ptr.CharCount)
                            ptr.CharCount++;

                        if (ptr.MaxHeight == 0)
                            ptr.MaxHeight = 14 + extraheight;

                        ptr.Data.Resize( (uint) ptr.CharCount);

                        if (isFixed || isCropped)
                            break;

                        MultilinesFontInfo newptr = new MultilinesFontInfo();
                        newptr.Reset();
                        ptr.Next = newptr;
                        ptr = newptr;
                        ptr.Align = current_align;
                        ptr.CharStart = i;
                        charCount = 0;

                        if (ptr.Align == TEXT_ALIGN_TYPE.TS_LEFT && (current_flags & UOFONT_INDENTION) != 0)
                            indetionOffset = 14;
                        ptr.IndentionOffset = indetionOffset;
                        readWidth = indetionOffset;
                    }
                }

                MultilinesFontData mfd = new MultilinesFontData(current_charcolor, current_flags, current_font, si, 0);
                ptr.Data.Add(mfd);

                if (si == ' ')
                {
                    readWidth += UNICODE_SPACE_WIDTH;

                    if (ptr.MaxHeight <= 0)
                        ptr.MaxHeight = 5 + extraheight;
                }
                else
                {
                    readWidth += (sbyte) data[0] + (sbyte) data[2] + 1;

                    if ((sbyte) data[1] + (sbyte) data[3] > ptr.MaxHeight)
                        ptr.MaxHeight = (sbyte) data[1] + (sbyte) data[3] + extraheight;
                }

                charCount++;
            }

            ptr.Width += readWidth;
            ptr.CharCount += charCount;

            if (readWidth == 0 && len != 0 && (str[len - 1] == '\n' || str[len - 1] == '\r'))
            {
                ptr.Width = 1;
                ptr.MaxHeight = 14;
            }

            return info;
        }

        private unsafe void GeneratePixelsUnicode(ref FontTexture texture, byte font, string str, ushort color, byte cell, int width, TEXT_ALIGN_TYPE align, ushort flags, bool saveHitmap)
        {
#if !DEBUG
            try
            {
#endif
                if (font >= 20 || _unicodeFontAddress[font] == IntPtr.Zero)
                    return;

                int len = str.Length;

                if (len == 0)
                    return;

                int oldWidth = width;

                if (width == 0)
                {
                    width = GetWidthUnicode(font, str);

                    if (width == 0)
                        return;
                }

                MultilinesFontInfo info = GetInfoUnicode(font, str, len, align, flags, width);

                if (info == null)
                    return;

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
                        newWidth = 10;
                    info = GetInfoUnicode(font, str, len, align, flags, newWidth);

                    if (info == null)
                        return;
                }

                if (oldWidth == 0 && RecalculateWidthByInfo)
                {
                    MultilinesFontInfo ptr1 = info;
                    width = 0;

                    while (ptr1 != null)
                    {
                        if (ptr1.Width > width)
                            width = ptr1.Width;
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
                int lineOffsY = 1 + _topMargin;
                MultilinesFontInfo ptr = info;
                uint datacolor = 0;

                if (color == 0xFFFF)
                    datacolor = 0xFEFFFFFF;
                else
                {
                    datacolor = /*FileManager.Hues.GetPolygoneColor(cell, color) << 8 | 0xFF;*/
                        HuesHelper.RgbaToArgb((FileManager.Hues.GetPolygoneColor(cell, color) << 8) | 0xFF);
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
                List<WebLinkRect> links = new List<WebLinkRect>();

                while (ptr != null)
                {
                    info = ptr;
                    linesCount++;
                    int w = _leftMargin;

                    switch (ptr.Align)
                    {
                        case TEXT_ALIGN_TYPE.TS_CENTER:

                        {
                            w += (width - ptr.Width) >> 1;

                            if (w < 0)
                                w = 0;

                            break;
                        }

                        case TEXT_ALIGN_TYPE.TS_RIGHT:

                        {
                            w += width - 10 - ptr.Width;

                            if (w < 0)
                                w = 0;

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
                        ref readonly MultilinesFontData dataPtr = ref ptr.Data[i];
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
                                linkHeight = 14;
                            int ofsX = 0;

                            if (si == ' ')
                                ofsX = UNICODE_SPACE_WIDTH;
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
                                StartX = linkStartX,
                                StartY = linkStartY,
                                EndX = w - ofsX,
                                EndY = linkHeight
                            };
                            links.Add(wlr);
                            oldLink = 0;
                        }

                        if ((table[si] == 0 || table[si] == 0xFFFFFFFF) && si != ' ')
                            continue;

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
                            dw = (sbyte) data[2];
                            dh = (sbyte) data[3];
                            data += 4;
                        }

                        int tmpW = w;
                        uint charcolor = datacolor;
                        //bool isBlackPixel = ((charcolor >> 24) & 0xFF) <= 8 && ((charcolor >> 16) & 0xFF) <= 8 && ((charcolor >> 8) & 0xFF) <= 8;
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

                                if (testY >= height)
                                    break;

                                byte* scanlines = data;
                                data += scanlineCount;

                                //data = (byte*) ((IntPtr) data + scanlineCount);
                                int italicOffset = 0;

                                if (isItalic)
                                    italicOffset = (int) ((dh - y) / ITALIC_FONT_KOEFFICIENT);
                                int testX = w + offsX + italicOffset + (isSolid ? 1 : 0);

                                for (int c = 0; c < scanlineCount; c++)
                                {
                                    for (int j = 0; j < 8; j++)
                                    {
                                        int x = c * 8 + j;

                                        if (x >= dw)
                                            break;

                                        int nowX = testX + x;

                                        if (nowX >= width)
                                            break;

                                        byte cl = (byte) (scanlines[c] & (1 << (7 - j)));
                                        int block = testY * width + nowX;

                                        if (cl != 0)
                                            pData[block] = charcolor;
                                    }
                                }
                            }

                            if (isSolid)
                            {
                                uint solidColor = blackColor;

                                if (solidColor == charcolor)
                                    solidColor++;
                                int minXOk = w + offsX > 0 ? -1 : 0;
                                int maxXOk = w + offsX + dw < width ? 1 : 0;
                                maxXOk += dw;

                                for (int cy = 0; cy < dh; cy++)
                                {
                                    int testY = offsY + lineOffsY + cy;

                                    if (testY >= height)
                                        break;

                                    int italicOffset = 0;

                                    if (isItalic && cy < dh)
                                        italicOffset = (int) ((dh - cy) / ITALIC_FONT_KOEFFICIENT);

                                    for (int cx = minXOk; cx < maxXOk; cx++)
                                    {
                                        int testX = cx + w + offsX + italicOffset;

                                        if (testX >= width)
                                            break;

                                        int block = testY * width + testX;

                                        if (pData[block] == 0 && pData[block] != solidColor)
                                        {
                                            int endX = cx < dw ? 2 : 1;

                                            if (endX == 2 && testX + 1 >= width)
                                                endX--;

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
                                        break;

                                    int italicOffset = 0;

                                    if (isItalic)
                                        italicOffset = (int) ((dh - cy) / ITALIC_FONT_KOEFFICIENT);

                                    for (int cx = 0; cx < dw; cx++)
                                    {
                                        int testX = cx + w + offsX + italicOffset;

                                        if (testX >= width)
                                            break;

                                        int block = testY * width + testX;

                                        if (pData[block] == solidColor)
                                            pData[block] = charcolor;
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

                                    if (testY >= height)
                                        break;

                                    int italicOffset = 0;

                                    if (isItalic && cy >= 0 && cy < dh)
                                        italicOffset = (int) ((dh - cy) / ITALIC_FONT_KOEFFICIENT);

                                    for (int cx = minXOk; cx < maxXOk; cx++)
                                    {
                                        int testX = cx + w + offsX + italicOffset;

                                        if (testX >= width)
                                            break;

                                        int block = testY * width + testX;

                                        if (pData[block] == 0 && pData[block] != blackColor)
                                        {
                                            int startX = cx > 0 ? -1 : 0;
                                            int startY = cy > 0 ? -1 : 0;
                                            int endX = cx < dw - 1 ? 2 : 1;
                                            int endY = cy < dh - 1 ? 2 : 1;

                                            if (endX == 2 && testX + 1 >= width)
                                                endX--;
                                            bool passed = false;

                                            for (int x = startX; x < endX; x++)
                                            {
                                                int nowX = testX + x;

                                                for (int y = startY; y < endY; y++)
                                                {
                                                    int testBlock = (testY + y) * width + nowX;

                                                    if (testBlock < pData.Length && pData[testBlock] != 0 && pData[testBlock] != blackColor)
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
                                break;

                            for (int cx = minXOk; cx < dw + maxXOk; cx++)
                            {
                                int testX = cx + tmpW + offsX + (isSolid ? 1 : 0);

                                if (testX >= width)
                                    break;

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
                                p = HuesHelper.RgbaToArgb(_backgroundColor);
                        }
                    }
                }

                if (texture == null || texture.IsDisposed)
                    texture = new FontTexture(width, height, linesCount, links);
                else
                {
                    texture.Links.Clear();
                    texture.Links.AddRange(links);
                    texture.LinesCount = linesCount;
                }

                texture.PushData(pData);

#if !DEBUG
            }
            catch (Exception ex)
            {
                string path = Path.Combine(Engine.ExePath, "Logs");
                FileSystemHelper.CreateFolderIfNotExists(path);

                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("Fonts crash log\r\nParams:\r\ntext: {0}\r\nfont: {1}\r\ncolor: {2}\r\ncell: {3}\r\nwidth: {4}\r\nalign: {5}\r\nflags: {6}\r\nsavehitmap: {7}\r\nStackTrace: {8}", str, font, color, cell, width, align, flags, saveHitmap, ex);

                using (LogFile file = new LogFile(path, "font_crash.txt"))
                    file.Write(sb.ToString());

                Chat.HandleMessage(World.Player, "An issue has been reported in /Logs.\nPlease report to CUO devs", "CUO ERROR", 32, MessageType.Regular, 1, true);

                return;
            }
#endif
        }

        private unsafe MultilinesFontInfo GetInfoHTML(byte font, string str, int len, TEXT_ALIGN_TYPE align, ushort flags, int width)
        {
            HTMLChar[] htmlData = GetHTMLData(font, str, ref len, align, flags);

            if (htmlData.Length == 0)
                return null;

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
                ptr.Align = htmlData[0].Align;

            for (int i = 0; i < len; i++)
            {
                char si = htmlData[i].Char;
                uint* table = (uint*) _unicodeFontAddress[htmlData[i].Font];

                if ((byte) si == 0x000D || si == '\n')
                {
                    if ((byte) si == 0x000D || isFixed || isCropped)
                        si = (char) 0;
                    else
                        si = '\n';
                }

                if ((table[si] == 0 || table[si] == 0xFFFFFFFF) && si != ' ' && si != '\n')
                    continue;

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
                        ptr.CharStart = 1;

                    if (si == '\n')
                    {
                        ptr.Width += readWidth;
                        ptr.CharCount += charCount;
                        lastSpace = i;

                        if (ptr.Width <= 0)
                            ptr.Width = 1;
                        ptr.MaxHeight = MAX_HTML_TEXT_HEIGHT;
                        ptr.Data.Resize( (uint) ptr.CharCount);
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
                            ptr.Width = 1;
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
                            indentionOffset = 14;
                        ptr.IndentionOffset = indentionOffset;
                        readWidth = indentionOffset;
                    }
                    else
                    {
                        if (isFixed)
                        {
                            MultilinesFontData mfd1 = new MultilinesFontData(htmlData[i].Color, htmlData[i].Flags, htmlData[i].Font, si, htmlData[i].LinkID);
                            ptr.Data.Add(mfd1);
                            readWidth += (sbyte) data[0] + (sbyte) data[2] + 1;
                            ptr.MaxHeight = MAX_HTML_TEXT_HEIGHT;
                            charCount++;
                            ptr.Width += readWidth;
                            ptr.CharCount += charCount;
                        }

                        i = lastSpace + 1;

                        if (i >= len)
                            break;

                        si = htmlData[i].Char;
                        solidWidth = htmlData[i].Flags & UOFONT_SOLID;

                        if (ptr.Width <= 0)
                            ptr.Width = 1;
                        ptr.MaxHeight = MAX_HTML_TEXT_HEIGHT;
                        ptr.Data.Resize( (uint) ptr.CharCount);
                        charCount = 0;

                        if (isFixed || isCropped)
                            break;

                        MultilinesFontInfo newptr = new MultilinesFontInfo();
                        newptr.Reset();
                        ptr.Next = newptr;
                        ptr = newptr;
                        ptr.Align = htmlData[i].Align;
                        ptr.CharStart = i;

                        if (ptr.Align == TEXT_ALIGN_TYPE.TS_LEFT && (htmlData[i].Flags & UOFONT_INDENTION) != 0)
                            indentionOffset = 14;
                        ptr.IndentionOffset = indentionOffset;
                        readWidth = indentionOffset;
                    }
                }

                MultilinesFontData mfd = new MultilinesFontData(htmlData[i].Color, htmlData[i].Flags, htmlData[i].Font, si, htmlData[i].LinkID);
                ptr.Data.Add(mfd);

                if (si == ' ')
                    readWidth += UNICODE_SPACE_WIDTH;
                else
                    readWidth += (sbyte) data[0] + (sbyte) data[2] + 1 + solidWidth;
                charCount++;
            }

            ptr.Width += readWidth;
            ptr.CharCount += charCount;
            ptr.MaxHeight = MAX_HTML_TEXT_HEIGHT;

            return info;
        }

        private static readonly HTMLChar[] _emptyHTML = { };

        private HTMLChar[] GetHTMLData(byte font, string str, ref int len, TEXT_ALIGN_TYPE align, ushort flags)
        {
            if (len < 1)
                return _emptyHTML;

            var data = new HTMLChar[len];
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
                    HTML_TAG_TYPE tag = ParseHTMLTag(str, len, ref i, ref endTag, ref newInfo);

                    if (tag == HTML_TAG_TYPE.HTT_NONE)
                        continue;

                    if (!endTag)
                    {
                        if (newInfo.Font == 0xFF)
                            newInfo.Font = stack[stack.Count - 1].Font;

                        if (tag != HTML_TAG_TYPE.HTT_BODY)
                            stack.Add(newInfo);
                        else
                        {
                            stack.Clear();
                            newlen = 0;

                            if (newInfo.Color != 0)
                                info.Color = newInfo.Color;
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
                                endTag = true;
                            goto case HTML_TAG_TYPE.HTT_P;

                        case HTML_TAG_TYPE.HTT_P:

                            if (endTag)
                                si = '\n';
                            else
                                si = (char) 0;

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
                ref readonly HTMLDataInfo current = ref list[i];

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

                        if (current.Font != 0xFF && _unicodeFontAddress[current.Font] != IntPtr.Zero)
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
                i++;
            int j = i;

            for (; i < len; i++)
            {
                if (str[i] == ' ' || str[i] == '>')
                    break;
            }

            if (j != i && i < len)
            {
                int cmdLen = i - j;
                string cmd = str.Substring(j, cmdLen).ToLower();
                j = i;

                while (i < len && str[i] != '>')
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

                    default:

                        if (str.Contains("bodybgcolor"))
                        {
                            tag = HTML_TAG_TYPE.HTT_BODYBGCOLOR;
                            j = str.IndexOf("bgcolor", StringComparison.Ordinal);
                        }
                        else
                            Log.Message(LogTypes.Warning, $"Unhandled HTML param:\t{str}");

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
                                cmdLen = i - j;
                                string content = str.Substring(j, cmdLen);

                                if (content.Length != 0)
                                    GetHTMLInfoFromContent(ref info, content);

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
            string[] strings = content.Split(new[]
            {
                ' ', '=', '\\'
            }, StringSplitOptions.RemoveEmptyEntries);


            int size = strings.Length;

            for (int i = 0; i < size; i += 2)
            {
                if (i + 1 >= size)
                    break;

                string str = strings[i].ToLower();
                string value = strings[i + 1];
                TrimHTMLString(ref value);

                if (value.Length == 0)
                    continue;

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
                            info.Color = GetHTMLColorFromText(ref value);
                        else if (str == "size")
                        {
                            byte font = byte.Parse(value);

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

                            int start = i + 1;

                            while (value[0] == '"' && value[value.Length - 1] != '"' && start + 1 < size) value += strings[++start];

                            i = start;

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
                    color = _visitedWebLinkColor;
                linkID = l.Value.Key;
            }

            return linkID;
        }

        public bool GetWebLink(ushort link, out WebLink result)
        {
            if (!_webLinks.TryGetValue(link, out result)) return false;

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
                    color = str.Substring(1).StartsWith("0x") ? Convert.ToUInt32(str.Substring(3), 16) : Convert.ToUInt32(str.Substring(1), 16);
                    byte* clrbuf = (byte*) &color;
                    color = (uint) ((clrbuf[0] << 24) | (clrbuf[1] << 16) | (clrbuf[2] << 8) | 0xFF);
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
                str = str.Substring(1, str.Length - 2);
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
                    textHeight += MAX_HTML_TEXT_HEIGHT;
                else
                    textHeight += info.MaxHeight;
            }

            return textHeight;
        }

        public int GetHeightUnicode(byte font, string str, int width, TEXT_ALIGN_TYPE align, ushort flags)
        {
            if (font >= 20 || _unicodeFontAddress[font] == IntPtr.Zero || string.IsNullOrEmpty(str))
                return 0;

            if (width <= 0)
                width = GetWidthUnicode(font, str);
            MultilinesFontInfo info = GetInfoUnicode(font, str, str.Length, align, flags, width);
            int textHeight = 0;

            while (info != null)
            {
                if (IsUsingHTML)
                    textHeight += MAX_HTML_TEXT_HEIGHT;
                else
                    textHeight += info.MaxHeight;
                MultilinesFontInfo ptr = info;
                info = info.Next;
                ptr.Data.Clear();
                ptr = null;
            }

            return textHeight;
        }

        public unsafe int CalculateCaretPosUnicode(byte font, string str, int x, int y, int width, TEXT_ALIGN_TYPE align, ushort flags)
        {
            if (x < 0 || y < 0 || font >= 20 || _unicodeFontAddress[font] == IntPtr.Zero || string.IsNullOrEmpty(str))
                return 0;

            if (width == 0)
                width = GetWidthUnicode(font, str);

            if (x >= width)
                return str.Length;

            MultilinesFontInfo info = GetInfoUnicode(font, str, str.Length, align, flags, width);

            if (info == null)
                return 0;

            int height = 0;
            uint* table = (uint*) _unicodeFontAddress[font];
            int pos = 0;
            bool found = false;

            while (info != null)
            {
                height += info.MaxHeight;
                width = 0;

                if (!found)
                {
                    if (y < height)
                    {
                        int len = info.CharCount;

                        for (int i = 0; i < len; i++)
                        {
                            char ch = info.Data[i].Item;
                            uint offset = table[ch];

                            if (offset != 0 && offset != 0xFFFFFFFF)
                            {
                                byte* cptr = (byte*) ((IntPtr) table + (int) offset);
                                width += (sbyte) cptr[0] + (sbyte) cptr[2] + 1;
                            }
                            else if (ch == ' ') width += UNICODE_SPACE_WIDTH;

                            if (width > x)
                                break;

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
                pos = str.Length;

            return pos;
        }

        public unsafe (int, int) GetCaretPosUnicode(byte font, string str, int pos, int width, TEXT_ALIGN_TYPE align, ushort flags)
        {
            if (pos < 1 || font >= 20 || _unicodeFontAddress[font] == IntPtr.Zero || string.IsNullOrEmpty(str))
                return (0, 0);

            if (width == 0)
                width = GetWidthUnicode(font, str);
            MultilinesFontInfo info = GetInfoUnicode(font, str, str.Length, align, flags, width);

            if (info == null)
                return (0, 0);

            uint* table = (uint*) _unicodeFontAddress[font];
            int x = 0;
            int y = 0;

            while (info != null)
            {
                x = 0;
                int len = info.CharCount;

                if (info.CharStart == pos)
                    return (x, y);

                if (pos <= info.CharStart + len && info.Data.Count >= len)
                {
                    for (int i = 0; i < len; i++)
                    {
                        char ch = info.Data[i].Item;
                        uint offset = table[ch];

                        if (offset != 0 && offset != 0xFFFFFFFF)
                        {
                            byte* cptr = (byte*) ((IntPtr) table + (int) offset);
                            x += (sbyte) cptr[0] + (sbyte) cptr[2] + 1;
                        }
                        else if (ch == ' ') x += UNICODE_SPACE_WIDTH;

                        if (info.CharStart + i + 1 == pos)
                            return (x, y);
                    }
                }

                if (info.Next != null)
                    y += info.MaxHeight;
                MultilinesFontInfo ptr = info;
                info = info.Next;
                ptr.Data.Clear();
                ptr = null;
            }

            return (x, y);
        }

        public int CalculateCaretPosASCII(byte font, string str, int x, int y, int width, TEXT_ALIGN_TYPE align, ushort flags)
        {
            if (font >= FontCount || x < 0 || y < 0 || string.IsNullOrEmpty(str))
                return 0;

            FontData fd = _font[font];

            if (width <= 0)
                width = GetWidthASCII(font, str);

            if (x >= width)
                return str.Length;

            MultilinesFontInfo info = GetInfoASCII(font, str, str.Length, align, flags, width);

            if (info == null)
                return 0;

            int height = GetHeightASCII(info);
            height = 0;
            int pos = 0;
            bool found = false;

            while (info != null)
            {
                height += info.MaxHeight;
                width = 0;

                if (!found)
                {
                    if (y < height)
                    {
                        int len = info.CharCount;

                        for (int i = 0; i < len; i++)
                        {
                            byte index = _fontIndex[info.Data[i].Item];
                            width += fd.Chars[index].Width;

                            if (width > x)
                                break;

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

                var ptr = info;
                info = info.Next;
                ptr.Data.Clear();
                ptr = null;
            }

            if (pos > str.Length)
                pos = str.Length;

            return pos;
        }

        public (int, int) GetCaretPosASCII(byte font, string str, int pos, int width, TEXT_ALIGN_TYPE align, ushort flags)
        {
            if (font >= FontCount || pos < 1 || string.IsNullOrEmpty(str))
                return (0, 0);

            FontData fd = _font[font];

            if (width == 0)
                width = GetWidthASCII(font, str);
            MultilinesFontInfo info = GetInfoASCII(font, str, str.Length, align, flags, width);

            if (info == null)
                return (0, 0);

            //int height = 0;
            //MultilinesFontInfo ptr = info;
            int x = 0;
            int y = 0;

            while (info != null)
            {
                x = 0;
                int len = info.CharCount;

                if (info.CharStart == pos)
                    return (x, y);

                if (pos <= info.CharStart + len && info.Data.Count >= len)
                {
                    for (int i = 0; i < len; i++)
                    {
                        byte index = _fontIndex[info.Data[i].Item];
                        x += fd.Chars[index].Width;

                        if (info.CharStart + i + 1 == pos)
                            return (x, y);
                    }
                }

                if (info.Next != null)
                    y += info.MaxHeight;
                MultilinesFontInfo ptr1 = info;
                info = info.Next;
                ptr1.Data.Clear();
                ptr1 = null;
            }

            return (x, y);
        }

        public int[] GetLinesCharsCountASCII(byte font, string str, TEXT_ALIGN_TYPE align, ushort flags, int width, bool countret = false, bool countspaces = false)
        {
            if (width == 0)
                width = GetWidthASCII(font, str);
            MultilinesFontInfo info = GetInfoASCII(font, str, str.Length, align, flags, width, countret, countspaces);

            if (info == null)
                return new int[0];

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

        public int[] GetLinesCharsCountUnicode(byte font, string str, TEXT_ALIGN_TYPE align, ushort flags, int width, bool countret = false, bool countspaces = false)
        {
            if (width == 0)
                width = GetWidthUnicode(font, str);
            MultilinesFontInfo info = GetInfoUnicode(font, str, str.Length, align, flags, width, countret, countspaces);

            if (info == null)
                return new int[0];

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
    internal readonly struct FontHeader
    {
        public readonly byte Width, Height, Unknown;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct FontCharacter
    {
        public readonly byte Width, Height, Unknown;
    }

    internal readonly struct FontCharacterData
    {
        public FontCharacterData(byte w, byte h, ushort[] data)
        {
            Width = w;
            Height = h;
            Data = data;
        }

        public readonly byte Width, Height;
        public readonly ushort[] Data;
    }

    internal readonly struct FontData
    {
        public FontData(byte header, FontCharacterData[] chars)
        {
            Header = header;
            Chars = chars;
        }

        public readonly byte Header;

        // 224
        public readonly FontCharacterData[] Chars;
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

    internal readonly struct MultilinesFontData
    {
        public MultilinesFontData(uint color, ushort flags, byte font, char item, ushort linkid)
        {
            Color = color;
            Flags = flags;
            Font = font;
            Item = item;
            LinkID = linkid;
        }

        public readonly uint Color;
        public readonly ushort Flags;
        public readonly byte Font;
        public readonly char Item;
        public readonly ushort LinkID;
        //public MultilinesFontData Next;
    }

    internal struct WebLinkRect
    {
        public ushort LinkID;
        public int StartX, StartY, EndX, EndY;
    }

    internal struct WebLink
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