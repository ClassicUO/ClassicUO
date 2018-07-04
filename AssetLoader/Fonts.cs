using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ClassicUO.AssetsLoader
{
    public static class Fonts
    {
        private static ASCIIFont[] _asciiFonts = new ASCIIFont[10];
        private static UniFont[] _uniFonts = new UniFont[3];

        public static int FontCount { get; private set; }

        private static FontData[] _font;

        private static readonly byte[] _fontIndex =  
        {
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0,    1,    2,    3,    4,    5,    6,    7,    8,    9,    10,   11,   12,   13,   14,   15,
            16,   17,   18,   19,   20,   21,   22,   23,   24,   25,   26,   27,   28,   29,   30,   31,
            32,   33,   34,   35,   36,   37,   38,   39,   40,   41,   42,   43,   44,   45,   46,   47,
            48,   49,   50,   51,   52,   53,   54,   55,   56,   57,   58,   59,   60,   61,   62,   63,
            64,   65,   66,   67,   68,   69,   70,   71,   72,   73,   74,   75,   76,   77,   78,   79,
            80,   81,   82,   83,   84,   85,   86,   87,   88,   89,   90,   0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 136,  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 152,  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            160,  161,  162,  163,  164,  165,  166,  167,  168,  169,  170,  171,  172,  173,  174,  175,
            176,  177,  178,  179,  180,  181,  182,  183,  184,  185,  186,  187,  188,  189,  190,  191,
            192,  193,  194,  195,  196,  197,  198,  199,  200,  201,  202,  203,  204,  205,  206,  207,
            208,  209,  210,  211,  212,  213,  214,  215,  216,  217,  218,  219,  220,  221,  222,  223
        };

    public static void Load()
        {
            UOFileMul fonts = new UOFileMul(Path.Combine(FileManager.UoFolderPath, "fonts.mul"));

            UOFileMul[] uniFonts = new UOFileMul[20];
            for (int i = 0; i < 20; i++)
            {
                string path = Path.Combine(FileManager.UoFolderPath, "unifont" + (i == 0 ? "" : i.ToString()) + ".mul");
                if (File.Exists(path))
                    uniFonts[i] = new UOFileMul(path);
            }

            int fontHeaderSize = Marshal.SizeOf<FontHeader>();
            FontCount = 0;

            while (fonts.Position < fonts.Length)
            {
                bool exit = false;
                fonts.Skip(1);

                unsafe
                {
                    for (int i = 0; i < 224; i++)
                    {
                        FontHeader* fh = (FontHeader*)fonts.PositionAddress;
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
                _font[i].Header = fonts.ReadByte();
                _font[i].Chars = new FontCharacterData[224];
                for (int j = 0; j < 224; j++)
                {
                    _font[i].Chars[j].Width = fonts.ReadByte();
                    _font[i].Chars[j].Height = fonts.ReadByte();
                    fonts.Skip(1);
                    int dataSize = _font[i].Chars[j].Width * _font[i].Chars[j].Height;
                    _font[i].Chars[j].Data = fonts.ReadArray<ushort>(dataSize).ToList();
                }
            }


            if (uniFonts[1] == null)
            {
                uniFonts[1] = uniFonts[0];
            }

            for (int i = 0; i < 256; i++)
            {
                if (_fontIndex[i] >= 0xE0)
                    _fontIndex[i] = _fontIndex[' '];
            }


            //for (int i = 0; i < _asciiFonts.Length; i++)
            //{
            //    _asciiFonts[i] = new ASCIIFont();
            //    _asciiFonts[i].Load(fonts);
            //}

            //int maxHeight = 0;
            //for (int i = 0; i < _uniFonts.Length; i++)
            //{
            //    UOFileMul unifont = new UOFileMul(Path.Combine(FileManager.UoFolderPath, "unifont" + (i == 0 ? "" : i.ToString()) + ".mul"));

            //    _uniFonts[i] = new UniFont();
            //    _uniFonts[i].Load(unifont);

            //    if (_uniFonts[i].Height > maxHeight)
            //        maxHeight = _uniFonts[i].Height;
            //}

            //for (int i = 0; i < _uniFonts.Length; i++)
            //{
            //    _uniFonts[i].Height = maxHeight;
            //}
        }

        public static ASCIIFont GetASCII(in int index)
        {
            if (index < 0 || index >= _asciiFonts.Length)
                return _asciiFonts[9];
            return _asciiFonts[index];
        }

        public static UniFont GetUnicode(in int index)
        {
            if (index < 0 || index >= _uniFonts.Length)
                return _uniFonts[0];
            return _uniFonts[index];
        }
    }

    public interface IFont
    {
        int Height { get; set; }

        void Load(in UOFile file);
        BaseCharacter GetChar(in char c);
        BaseCharacter[] GetString(in string s);
    }

    public class ASCIIFont : IFont
    {
        private ASCIIChar[] _chars;
        private readonly ASCIIChar _null = new ASCIIChar();

        public ASCIIFont()
        {
            _chars = new ASCIIChar[224];
        }

        public int Height { get; set; }


        public void Load(in UOFile file)
        {
            byte header = file.ReadByte();
            _chars[0] = new ASCIIChar();

            for (int i = 0; i < _chars.Length; i++)
            {
                ASCIIChar ch = new ASCIIChar(file);
                int height = ch.Height;
                if (i > 32 && i < 90 && height > header)
                    Height = height;
                _chars[i] = ch;
            }

            for (int i = 0; i < _chars.Length; i++)
            {
                _chars[i].OffsetY = Height - (_chars[i].Height + _chars[i].OffsetY);
            }

            Height -= 2;

            GetChar(' ').Width = GetChar('M').Width / 3;
        }

        public BaseCharacter GetChar(in char c)
        {
            int index = (c & 0xFFFFF) - 0x20;

            if (index < 0 || index >= _chars.Length)
                return _null;

            return _chars[index];
        }

        public BaseCharacter[] GetString(in string s)
        {
            BaseCharacter[] chars = new BaseCharacter[s.Length];
            for (int i = 0; i < chars.Length; i++)
                chars[i] = GetChar(s[i]);
            return chars;
        }
    }

    public class UniFont : IFont
    {
        private readonly UniChar _null = new UniChar();
        private readonly UniChar[] _chars;
        private UOFile _file;

        public UniFont()
        {
            _chars = new UniChar[100];
        }

        public int Height { get; set; }

        public void Load(in UOFile file)
        {
            _file = file;

            _chars[0] = new UniChar();

            for (int i = 33; i < 128; i++)
                GetChar((char)i);

            GetChar(' ').Width = GetChar('M').Width / 3;
        }

        public BaseCharacter GetChar(in char c)
        {
            int index = (c & 0xFFFFF) - 0x20;
            if (index < 0)
                return _null;

            if (_chars[index] == null)
            {
                UniChar ch = LoadChar(index + 0x20);
                int height = ch.Height + ch.OffsetY;
                if (index < 128 && height > Height)
                    Height = height;
                _chars[index] = ch;
            }

            return _chars[index];
        }

        public BaseCharacter[] GetString(in string s)
        {
            BaseCharacter[] chars = new BaseCharacter[s.Length];
            for (int i = 0; i < chars.Length; i++)
                chars[i] = GetChar(s[i]);
            return chars;
        }

        private UniChar LoadChar(in int index)
        {
            _file.Position = index * 4;
            int lookup = _file.ReadInt();
            if (lookup == 0)
                return _null;
            _file.Position = lookup;
            return new UniChar(_file);
        }
     
    }

    public class ASCIIChar : BaseCharacter
    {
        public ASCIIChar()
        {

        }

        public ASCIIChar(in UOFile file)
        {
            Width = file.ReadByte();
            Height = file.ReadByte();

            file.Skip(1);

            int startY = Height;
            int endY = -1;

            uint[] pixels = null;

            if (Width > 0 && Height > 0)
            {
                pixels = new uint[Width * Height];

                int i = 0;

                for (int y = 0; y < Height; y++)
                {
                    bool rowHasData = false;

                    for (int x = 0; x < Width; x++)
                    {
                        ushort pixel = file.ReadUShort();

                        if (pixel != 0)
                        {
                            pixels[i] = (uint)(0xFF000000 + (
                                ((((pixel >> 10) & 0x1F) * 0xFF / 0x1F)) |
                                ((((pixel >> 5) & 0x1F) * 0xFF / 0x1F) << 8) |
                                (((pixel & 0x1F) * 0xFF / 0x1F) << 16)
                                ));
                            rowHasData = true;
                        }
                        i++;
                    }

                    if (rowHasData)
                    {
                        if (startY > y)
                            startY = y;
                        endY = y;
                    }
                }
            }

            endY++;
            if (endY == 0)
                _pixels = null;
            else if (endY == Height)
                _pixels = pixels;
            else
            {
                _pixels = new uint[Width * endY];
                int i = 0;

                for (int y = 0; y < endY; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        _pixels[i++] = pixels[y * Width + x];
                    }
                }

                OffsetY = Height - endY;
                Height = endY;
            }
        }
    }

    public class UniChar : BaseCharacter
    {
        public UniChar()
        {

        }

        public UniChar(in UOFile file)
        {
            OffsetX = file.ReadSByte();
            OffsetY = file.ReadSByte();
            Width = file.ReadByte();
            Height = file.ReadByte();
            
            if (Width > 0 && Height > 0)
            {
                _pixels = new uint[Width * Height];

                for (int y = 0; y < Height; y++)
                {
                    byte[] line = file.ReadArray<byte>(((Width - 1) / 8) + 1);
                    int bitX = 7; int byteX = 0;

                    for (int x = 0; x < Width; x++)
                    {
                        uint color = (line[byteX] & (byte)Math.Pow(2, bitX)) != 0 ? 0xFFFFFFFF : 0;
                        _pixels[y * Width + x] = color;
                        bitX--;
                        if (bitX < 0)
                        {
                            bitX = 7;
                            byteX++;
                        }

                    }
                }
            }
        }
    }

    public abstract class BaseCharacter
    {
        protected uint[] _pixels;

        public int Width { get; set; }
        public int Height { get; set; }
        public int OffsetX { get; protected set; }
        public int OffsetY { get; internal set; }
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
}
