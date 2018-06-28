using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ClassicUO.AssetsLoader
{
    public static class Fonts
    {
        private static ASCIIFont[] _asciiFonts = new ASCIIFont[10];
        private static UniFont[] _uniFonts = new UniFont[3];

        public static void Load()
        {
            UOFileMul fonts = new UOFileMul(Path.Combine(FileManager.UoFolderPath, "fonts.mul"));

            for (int i = 0; i < _asciiFonts.Length; i++)
            {
                _asciiFonts[i] = new ASCIIFont();
                _asciiFonts[i].Load(fonts);
            }

            int maxHeight = 0;
            for (int i = 0; i < _uniFonts.Length; i++)
            {
                UOFileMul unifont = new UOFileMul(Path.Combine(FileManager.UoFolderPath, "unifont" + (i == 0 ? "" : i.ToString()) + ".mul"));

                _uniFonts[i] = new UniFont();
                _uniFonts[i].Load(unifont);

                if (_uniFonts[i].Height > maxHeight)
                    maxHeight = _uniFonts[i].Height;
            }

            for (int i = 0; i < _uniFonts.Length; i++)
            {
                _uniFonts[i].Height = maxHeight;
            }
        }

        public static ASCIIFont GetASCII(int index)
        {
            if (index < 0 || index >= _asciiFonts.Length)
                return _asciiFonts[9];
            return _asciiFonts[index];
        }

        public static UniFont GetUnicode(int index)
        {
            if (index < 0 || index >= _uniFonts.Length)
                return _uniFonts[0];
            return _uniFonts[index];
        }
    }

    public interface IFont
    {
        int Height { get; set; }

        void Load(UOFile file);
        BaseCharacter GetChar(char c);
        string GetString();
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


        public void Load(UOFile file)
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

        public BaseCharacter GetChar(char c)
        {
            int index = (c & 0xFFFFF) - 0x20;

            if (index < 0 || index >= _chars.Length)
                return _null;

            return _chars[index];
        }

        public string GetString()
        {
            throw new NotImplementedException();
        }
    }

    public class UniFont : IFont
    {
        private readonly UniChar _null = new UniChar();
        private readonly UniChar[] _chars;
        private UOFile _file;

        public UniFont()
        {
            _chars = new UniChar[0x10000];
        }

        public int Height { get; set; }

        public void Load(UOFile file)
        {
            _file = file;

            _chars[0] = new UniChar();

            for (int i = 33; i < 128; i++)
                GetChar((char)i);

            GetChar(' ').Width = GetChar('M').Width / 3;
        }

        public BaseCharacter GetChar(char c)
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

        public string GetString()
        {
            throw new NotImplementedException();
        }

        private UniChar LoadChar(int index)
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

        public ASCIIChar(UOFile file)
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

        public UniChar(UOFile file)
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
}
