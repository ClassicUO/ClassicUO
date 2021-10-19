using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Renderer
{
    struct FontSettings
    {
        public byte FontIndex;
        public bool IsUnicode;
        public bool IsHtml;
        public int CharsCount;
        public bool Bold;
        public bool Italic;
        public bool Underline;
        public bool Border;
    }

    class UOFontRenderer
    {
        const int ASCII_CHARS_COUNT = 224;
        const int DEFAULT_SPACE_SIZE = 8;

        private readonly TextureAtlas _atlas;
        private UOFile _asciiFontFile;
        private UOFile[] _unicodeFontFiles = new UOFile[20];
        private PixelPicker _picker = new PixelPicker();
        private readonly Dictionary<uint, SpriteInfo> _spriteKeyInfo = new Dictionary<uint, SpriteInfo>();
        private CharacterInfo[,] _asciiCharsInfo;
        private int _asciiFontCount;


        public UOFontRenderer(GraphicsDevice device)
        {
            const int ATLAS_SIZE = 1024;
            _atlas = new TextureAtlas(device, ATLAS_SIZE, ATLAS_SIZE, SurfaceFormat.Color);

            const string ASCII_UO_FILE = "fonts.mul";
            const string UNICODE_UO_FILE = "unifont{0}.mul";

            string path = UOFileManager.GetUOFilePath(ASCII_UO_FILE);

            if (File.Exists(path))
            {
                _asciiFontFile = new UOFile(path, true);

                _asciiFontCount = GetFontCount(_asciiFontFile, ASCII_CHARS_COUNT);
            }

            for (int i = 0; i < 20; ++i)
            {
                path = UOFileManager.GetUOFilePath(string.Format(UNICODE_UO_FILE, (i == 0 ? string.Empty : i.ToString())));
            
                if (File.Exists(path))
                {
                    _unicodeFontFiles[i] = new UOFile(path, true);
                }
            }
        }


        public void Draw
        (
            UltimaBatcher2D batcher, 
            ReadOnlySpan<char> text, 
            Vector2 position, 
            float scale, 
            in FontSettings settings,
            Vector3 hue,
            TEXT_ALIGN_TYPE align = TEXT_ALIGN_TYPE.TS_LEFT
        )
        {     
            Vector2 textSizeInPixels = MeasureStringAdvanced(text, settings, scale, position, out bool mouseIsOver, out float maxHeight);

            FixVectorColor(ref hue, settings);

            if (mouseIsOver)
            {
                hue.X = 0x35;

                FixVectorColor(ref hue, settings);
            }

            Vector2 startPosition = position;

            //batcher.DrawRectangle
            //(
            //    SolidColorTextureCache.GetTexture(Color.White),
            //    (int) position.X, 
            //    (int) position.Y,
            //    (int) textSizeInPixels.X,
            //    (int) textSizeInPixels.Y,
            //    ref hue
            //);


            if (align == TEXT_ALIGN_TYPE.TS_CENTER)
            {
                startPosition.X += textSizeInPixels.X / 2f;
            }
            else if (align == TEXT_ALIGN_TYPE.TS_RIGHT)
            {

            }

            Rectangle uv;

            for (int i = 0; i < text.Length; ++i)
            {
                if (text[i] == '\r')
                {
                    continue;
                }

                if (text[i] == '\n')
                {
                    position.X = startPosition.X;
                    position.Y += maxHeight;

                    continue;
                }

                if (text[i] == ' ')
                {
                    position.X += DEFAULT_SPACE_SIZE * scale;

                    continue;
                }

                var texture = ReadChar(text[i], settings, out uv, out _);

                if (texture != null)
                {
                    batcher.Draw
                    (
                        texture, 
                        position,
                        uv,
                        hue, 
                        0f,
                        Vector2.Zero,
                        scale,
                        SpriteEffects.None,
                        0f
                    );

                    position.X += uv.Width * scale;
                }
            }

            if (settings.Underline)
            {
                Vector2 end = startPosition;
                end.X += textSizeInPixels.X;
                startPosition.Y += textSizeInPixels.Y;
                end.Y += textSizeInPixels.Y;

                var texture = SolidColorTextureCache.GetTexture(Color.White);
               
                float stroke = 1f;

                if (settings.Border)
                {
                    Vector2 startPositionBlack = startPosition;
                    startPositionBlack.X -= stroke * scale;
                    startPositionBlack.Y -= stroke * scale;
                    Rectangle destRect = new Rectangle
                    (
                        0,
                        0,
                        (int)(((end.X + stroke * scale) - startPositionBlack.X) / scale),
                        (int)(((end.Y + (stroke * 2f) * scale) - startPositionBlack.Y) / scale)
                    );
                  
                    batcher.Draw
                    (
                        texture, 
                        startPositionBlack, 
                        destRect, 
                        new Vector3(0, 1, 0), 
                        0f,
                        Vector2.Zero,
                        scale,
                        0,
                        0
                    );
                }
               
                batcher.DrawLine
                (
                   texture,
                   startPosition,
                   end,
                   hue,
                   stroke * scale
                );
            }          
        }

        public Vector2 MeasureString(ReadOnlySpan<char> text, in FontSettings settings, float scale)
        {
            Vector2 size = new Vector2();

            Rectangle uv;
            int returns = 0;
            float maxWidth = 0;
            float maxHeight = 0;

            for (int i = 0; i < text.Length; ++i)
            {
                if (text[i] == '\r')
                {
                    continue;
                }

                if (text[i] == '\n')
                {
                    ++returns;

                    maxWidth = size.X;
                    size.X = 0;

                    continue;
                }

                if (text[i] == ' ')
                {
                    size.X += 8 * scale;

                    continue;
                }

                if (ReadChar(text[i], settings, out uv, out _) != null)
                {
                    maxHeight = Math.Max(maxHeight, uv.Height);
                    size.X += uv.Width * scale;
                }
            }

            size.X = Math.Max(size.X, maxWidth);
            size.Y += (returns + 1) * maxHeight;

            return size;
        }
  
        public Vector2 MeasureStringAdvanced(ReadOnlySpan<char> text, in FontSettings settings, float scale, Vector2 position, out bool mouseIsOver, out float maxHeight)
        {
            Vector2 size = new Vector2();

            int returns = 0;
            float maxWidth = 0;

            Rectangle uv;
            _ = ReadChar('W', settings, out uv, out _);
            maxHeight = uv.Height;
            _ = ReadChar('g', settings, out uv, out _);
            maxHeight = Math.Max(maxHeight, uv.Height);
            maxHeight += 1;
            maxHeight *= scale;

            var mouseScreenPosition = Mouse.Position;
            mouseIsOver = false;

            for (int i = 0; i< text.Length; ++i)
            {
                if (text[i] == '\r')
                {
                    continue;
                }

                if (text[i] == '\n')
                {
                    ++returns;

                    maxWidth = size.X;
                    size.X = 0;

                    continue;
                }

                if (text[i] == ' ')
                {
                    size.X += DEFAULT_SPACE_SIZE * scale;

                    continue;
                }

                if (ReadChar(text[i], settings, out uv, out uint key) != null)
                {                   
                    maxHeight = Math.Max(maxHeight, uv.Height);

                    if (!mouseIsOver)
                    {
                        mouseIsOver = _picker.Get
                        (
                            key,
                            (int)((mouseScreenPosition.X - (position.X + size.X)) / scale),
                            (int)((mouseScreenPosition.Y - (position.Y + (maxHeight * returns))) / scale)
                        );
                    }

                    size.X += uv.Width * scale;
                }
            }

            size.X = Math.Max(size.X, maxWidth);
            size.Y += (returns + 1) * maxHeight;

            return size;
        }

        private unsafe Texture2D ReadChar(char c, in FontSettings settings, out Rectangle uv, out uint key)
        {
            return settings.IsUnicode ? ReadCharUnicode(c, settings, out uv, out key) : ReadCharASCII(c, settings, out uv, out key);
        }

        private unsafe Texture2D ReadCharUnicode(char c, in FontSettings settings, out Rectangle uv, out uint key)
        {
            const int UNICODE_SPACE_WIDTH = 8;
            const float ITALIC_FONT_KOEFFICIENT = 3.3f;
            const uint UO_BLACK = 0xFF010101;
            const uint DEFAULT_HUE = 0xFF_FF_FF_FF;

            key = CreateKey(c, settings);

            if (_spriteKeyInfo.TryGetValue(key, out var spriteInfo))
            {
                uv = spriteInfo.UV;
                return spriteInfo.Texture;
            }

            uv = Rectangle.Empty;
            
            uint* table = (uint*)_unicodeFontFiles[settings.FontIndex].StartAddress;
            
            if (c == '\r')
            {
                return null;
            }

            if ((table[c] == 0 || table[c] == 0xFF_FF_FF_FF) && c != ' ')
            {
                c = '?';
            }

            bool isItalic = settings.Italic;
            bool isSolid = settings.Bold;
            bool isUnderline = settings.Underline;
            bool isBlackBorder = settings.Border;

            int lineOffY = 0;
            int w = 0;

            int textureWidth = 0;
            int textureHeight = 0;
            int maxHeight = 0;

            Point offsetFromFlag = Point.Zero;

            if (isBlackBorder)
            {
                ++offsetFromFlag.X;
                ++offsetFromFlag.Y;
            }

            if (isItalic)
            {
                offsetFromFlag.X += 3;
            }

            if (isSolid)
            {
                ++offsetFromFlag.X;
                ++offsetFromFlag.Y;
            }

            if (isUnderline)
            {
                ++offsetFromFlag.Y;
            }

            int tmpW = w;
            byte* data = (byte*)((IntPtr)table + (int)table[c]);

            int offX = 0;
            int offY = 0;
            int dw = 0;
            int dh = 0;

            if (c == ' ')
            {
                dw = UNICODE_SPACE_WIDTH;
                dh = maxHeight;
                w += UNICODE_SPACE_WIDTH;
            }
            else
            {
                offX = (sbyte)data[0] + 1;
                offY = (sbyte)data[1];
                dw = data[2];
                dh = data[3];
            }

            if (dw <= 0 || dh <= 0)
            {
                return null;
            }
            
            textureWidth = dw + offX + offsetFromFlag.X;
            textureHeight = dh + offY + offsetFromFlag.Y;

            Span<uint> buffer = stackalloc uint[textureWidth * textureHeight];

            if (c != ' ')
            {
                data += 4;

                int scanlineCount = ((dw - 1) >> 3) + 1;

                for (int y = 0; y < dh; ++y)
                {
                    int testY = offY + lineOffY + y;

                    if (testY < 0)
                    {
                        testY = 0;
                    }

                    if (testY >= textureHeight)
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

                    int testX = w + offX + italicOffset + (isSolid ? 1 : 0);

                    for (int j = 0; j < scanlineCount; ++j)
                    {
                        int coeff = j << 3;

                        for (int z = 0; z < 8; ++z)
                        {
                            int x = coeff + z;

                            if (x >= dw)
                            {
                                break;
                            }

                            int nowX = testX + x;

                            if (nowX >= textureWidth)
                            {
                                break;
                            }

                            byte cl = (byte)(scanlines[j] & (1 << (7 - z)));
                            int block = testY * textureWidth + nowX;

                            if (cl != 0)
                            {
                                buffer[block] = DEFAULT_HUE;
                            }
                        }
                    }
                }

                if (isSolid)
                {
                    uint solidColor = UO_BLACK;

                    if (solidColor == DEFAULT_HUE)
                    {
                        solidColor++;
                    }

                    int minXOk = w + offX > 0 ? -1 : 0;
                    int maxXOk = w + offX + dw < textureWidth ? 1 : 0;
                    maxXOk += dw;

                    for (int cy = 0; cy < dh; cy++)
                    {
                        int testY = offY + lineOffY + cy;

                        if (testY >= textureHeight)
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
                            int testX = cx + w + offX + italicOffset;

                            if (testX >= textureWidth)
                            {
                                break;
                            }

                            int block = testY * textureWidth + testX;

                            if (buffer[block] == 0 && buffer[block] != solidColor)
                            {
                                int endX = cx < dw ? 2 : 1;

                                if (endX == 2 && testX + 1 >= textureWidth)
                                {
                                    endX--;
                                }

                                for (int x = 0; x < endX; x++)
                                {
                                    int nowX = testX + x;
                                    int testBlock = testY * textureWidth + nowX;

                                    if (buffer[testBlock] != 0 && buffer[testBlock] != solidColor)
                                    {
                                        buffer[block] = solidColor;

                                        break;
                                    }
                                }
                            }
                        }
                    }

                    for (int cy = 0; cy < dh; cy++)
                    {
                        int testY = offY + lineOffY + cy;

                        if (testY >= textureHeight)
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
                            int testX = cx + w + offX + italicOffset;

                            if (testX >= textureWidth)
                            {
                                break;
                            }

                            int block = testY * textureWidth + testX;

                            if (buffer[block] == solidColor)
                            {
                                buffer[block] = DEFAULT_HUE;
                            }
                        }
                    }
                }

                if (isBlackBorder)
                {
                    int minXOk = w + offX > 0 ? -1 : 0;
                    int minYOk = offY > 0 ? -1 : 0;
                    int maxXOk = w + offX + dw < textureWidth ? 1 : 0;
                    int maxYOk = offY + lineOffY + dh < textureHeight ? 1 : 0;
                    maxXOk += dw;
                    maxYOk += dh;

                    for (int cy = minYOk; cy < maxYOk; cy++)
                    {
                        int testY = offY + cy;

                        if (testY < 0)
                        {
                            testY = 0;
                        }

                        if (testY >= textureHeight)
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
                            int testX = cx + w + offX + italicOffset;

                            if (testX >= textureWidth)
                            {
                                break;
                            }

                            int block = testY * textureWidth + testX;

                            if (buffer[block] == 0 && buffer[block] != UO_BLACK)
                            {
                                int startX = cx > 0 ? -1 : 0;
                                int startY = cy > 0 ? -1 : 0;
                                int endX = cx < dw - 1 ? 2 : 1;
                                int endY = cy < dh - 1 ? 2 : 1;

                                if (endX == 2 && testX + 1 >= textureWidth)
                                {
                                    endX--;
                                }

                                bool passed = false;
                                int len = textureWidth * textureHeight;

                                for (int x = startX; x < endX; x++)
                                {
                                    int nowX = testX + x;

                                    for (int y = startY; y < endY; y++)
                                    {
                                        int testBlock = (testY + y) * textureWidth + nowX;

                                        if (testBlock < 0)
                                        {
                                            continue;
                                        }

                                        if (testBlock < len && buffer[testBlock] != 0 && buffer[testBlock] != UO_BLACK)
                                        {
                                            buffer[block] = UO_BLACK;
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

                w += dw + offX + (isSolid ? 1 : 0) + 4;
            }

            if (1 == 2 && isUnderline)
            {
                int minXOk = tmpW + offX > 0 ? -1 : 0;
                int maxXOk = w + offX + dw < textureWidth ? 1 : 0;
                byte* aData = (byte*)((IntPtr)table + (int)table[(byte)'a']);
                int testY = lineOffY + (sbyte)aData[1] + (sbyte)aData[3];

                if (testY < textureHeight)
                {
                    if (testY < 0)
                    {
                        testY = 0;
                    }

                    for (int cx = minXOk; cx < dw + maxXOk; cx++)
                    {
                        int testX = cx + tmpW + offX + (isSolid ? 1 : 0);

                        if (testX >= textureWidth)
                        {
                            break;
                        }

                        int block = testY * textureWidth + testX;
                        buffer[block] = DEFAULT_HUE;
                    }
                }
            }



            spriteInfo = new SpriteInfo();
            spriteInfo.Char = c;
            spriteInfo.Settings = settings;
            spriteInfo.Texture = _atlas.AddSprite(buffer, textureWidth, textureHeight, out spriteInfo.UV);

            _spriteKeyInfo[key] = spriteInfo;

            _picker.Set(key, textureWidth, textureHeight, buffer);

            uv = spriteInfo.UV;

            return spriteInfo.Texture;
        }

        private unsafe Texture2D ReadCharASCII(char c, in FontSettings settings, out Rectangle uv, out uint key)
        {
            if (settings.IsUnicode || settings.FontIndex >= _asciiFontCount)
            {
                key = 0;
                uv = Rectangle.Empty;

                return null;
            }

            if (c > byte.MaxValue)
            {
                c = '?';
            }

            key = CreateKey(c, settings);

            if (_spriteKeyInfo.TryGetValue(key, out var spriteInfo))
            {
                uv = spriteInfo.UV;
                return spriteInfo.Texture;
            }

            int lineOffY = 0;
            int w = 0;

            ref CharacterInfo info1 = ref _asciiCharsInfo[settings.FontIndex, GetASCIIIndex(c)];

            int dw = info1.Width;
            int dh = info1.Height;
            int textureWidth = dw;
            int textureHeight = dh;
            int maxHeight = 0;

            if (textureWidth <= 0 || textureHeight <= 0)
            {
                uv = Rectangle.Empty;
                return null;
            }

            ++textureHeight;

            Span<uint> buffer = stackalloc uint[textureWidth * textureHeight];

            int offsY = GetFontOffsetY((byte)settings.FontIndex, (byte)c);

            for (int y = 0; y < dh; ++y)
            {
                int testY = y + lineOffY + offsY;

                if (testY >= textureHeight)
                {
                    break;
                }

                int pos = y * dw;

                for (int x = 0; x < dw; ++x)
                {
                    if (x + w >= textureWidth)
                    {
                        lineOffY += maxHeight;
                        w = 0;

                        break;
                    }

                    ushort uc = ((ushort*)info1.Data)[pos + x];

                    if (uc != 0)
                    {
                        var color = HuesHelper.Color16To32(uc) | 0xFF_00_00_00;

                        int block = testY * textureWidth + x + w;

                        if (block >= 0)
                        {
                            buffer[block] = color;
                        }
                    }
                }
            }

            w += dw;

            spriteInfo = new SpriteInfo();
            spriteInfo.Char = c;
            spriteInfo.Settings = settings;
            spriteInfo.Texture = _atlas.AddSprite(buffer, textureWidth, textureHeight, out spriteInfo.UV);

            _spriteKeyInfo[key] = spriteInfo;

            _picker.Set(key, textureWidth, textureHeight, buffer);

            uv = spriteInfo.UV;

            return spriteInfo.Texture;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void FixVectorColor(ref Vector3 color, in FontSettings settings)
        {
            if (color.X != 0)
            {
                if (settings.IsUnicode)
                {
                    color.Y = ShaderHueTranslator.SHADER_TEXT_HUE_NO_BLACK;
                }
                else if (settings.FontIndex != 8 && settings.FontIndex != 5)
                {
                    color.Y = ShaderHueTranslator.SHADER_PARTIAL_HUED;
                }
                else
                {
                    color.Y = ShaderHueTranslator.SHADER_HUED;
                }
            }
            else
            {
                color.Y = ShaderHueTranslator.SHADER_NONE;
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint CreateKey(char c, in FontSettings settings)
        {
            unchecked
            {
                uint hash = 17;
                hash = (uint)(hash * 31 + (int)c);
                hash = (uint)(hash * 31 + settings.FontIndex.GetHashCode());
                hash = (uint)(hash * 31 + settings.Bold.GetHashCode());
                hash = (uint)(hash * 31 + settings.Italic.GetHashCode());
                //hash = (uint)(hash * 31 + settings.Underline.GetHashCode());
                hash = (uint)(hash * 31 + settings.Border.GetHashCode());
                hash = (uint)(hash * 31 + settings.IsHtml.GetHashCode());
                hash = (uint)(hash * 31 + settings.IsUnicode.GetHashCode());

                return hash;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetASCIIIndex(char c)
        {
            const byte NOPRINT_CHARS = 32;

            byte ch = (byte)c; // ASCII fonts cover only 256 characters

            if (ch < NOPRINT_CHARS)
            {
                return 0;
            }

            return ch - NOPRINT_CHARS;
        }

        private static readonly int[] _offsetCharTable =
        {
            2, 0, 2, 2, 0, 0, 2, 2, 0, 0
        };
        private static readonly int[] _offsetSymbolTable =
        {
            1, 0, 1, 1, -1, 0, 1, 1, 0, 0
        };

        private static int GetFontOffsetY(byte font, byte index)
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

        private unsafe int GetFontCount(UOFile file, int charactersCount)
        {
            file.Seek(0);

            bool done = false;
            int fontCount = 0;
            int headerSize = sizeof(FontHeader);


            while (file.Position < file.Length)
            {
                file.Skip(1);

                for (int i = 0; i < charactersCount; ++i)
                {
                    FontHeader* fh = (FontHeader*)file.PositionAddress;

                    if (file.Position + headerSize >= file.Length)
                    {
                        continue;
                    }

                    file.Skip(headerSize);
             
                    int bcount = fh->Width * fh->Height * sizeof(ushort);

                    if (file.Position + bcount > file.Length)
                    {
                        done = true;
                        break;
                    }

                    file.Skip(bcount);
                }

                if (done)
                {
                    break;
                }

                ++fontCount;
            }


            _asciiCharsInfo = new CharacterInfo[fontCount, ASCII_CHARS_COUNT];

            file.Seek(0);

            for (int k = 0; k < fontCount; ++k)
            {
                byte header = file.ReadByte();

                for (int i = 0; i < charactersCount; ++i)
                {
                    if (file.Position + 3 >= file.Length)
                    {
                        continue;
                    }

                    ref CharacterInfo info = ref _asciiCharsInfo[k, i];
                    info.Width = file.ReadByte();
                    info.Height = file.ReadByte();


                    file.Skip(1);

                    info.Data = (ushort*)file.PositionAddress;

                    file.Skip(info.Width * info.Height * sizeof(ushort));
                }
            }

            return fontCount;
        }

        private struct SpriteInfo
        {
            public Texture2D Texture;
            public char Char;
            public Rectangle UV;
            public FontSettings Settings;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FontHeader
        {
            public byte Width, Height, Unknown;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private unsafe struct CharacterInfo
        {
            public byte Width, Height;
            public void* Data;
        }
    }
}
