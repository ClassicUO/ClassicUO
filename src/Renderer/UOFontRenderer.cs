using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
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
        private FontDrawCmd[] _commands = new FontDrawCmd[256];
        private int _cmdCount;


        public static UOFontRenderer Shared { get; private set; }


        public static void Create(GraphicsDevice device)
        {
            if (Shared == null)
            {
                Shared = new UOFontRenderer(device);
            }
        }


        private UOFontRenderer(GraphicsDevice device)
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



        public bool Draw
        (
            UltimaBatcher2D batcher,
            ReadOnlySpan<char> text,
            Vector2 position,
            float scale,
            in FontSettings settings,
            ushort hue,
            bool allowSelection = false
        )
        {
            Vector3 hueVec = new Vector3();
            ShaderHueTranslator.GetHueVector(ref hueVec, hue);
            return Draw(batcher, text, position, scale, settings, hueVec, allowSelection);
        }

        public bool Draw
        (
           UltimaBatcher2D batcher,
           ReadOnlySpan<char> text,
           Vector2 position,
           float scale,
           in FontSettings settings,
           Color color,
           bool allowSelection = false
        )
        {
            // TODO: shaders should support RGBA without using the UO colors

            Vector3 hueVec = new Vector3(0, -1, 0);
            return Draw(batcher, text, position, scale, settings, hueVec, allowSelection);
        }

        public bool Draw
        (
            UltimaBatcher2D batcher, 
            ReadOnlySpan<char> text, 
            Vector2 position, 
            float scale, 
            in FontSettings settings,
            Vector3 hue,
            bool allowSelection = false,
            float maxTextWidth = 0
        )
        {
            FixVectorColor(ref hue, settings);

            if (maxTextWidth <= 0.0f)
            {
                maxTextWidth = MeasureStringInternal(text, settings, scale, position, 0f).X;
            }
            else
            {
                maxTextWidth *= scale;
            }         

            Vector2 startPosition = position;
            float lineHeight = GetFontHeight(settings) * scale;
            Vector2 fullSize = new Vector2(0, lineHeight);
            Point mousePosition = Mouse.Position;

            bool mouseIsOver = false;

            InternalDraw
            (
                text,
                settings,
                ref position,
                ref fullSize,
                ref hue,
                mousePosition,
                allowSelection,
                ref mouseIsOver,
                startPosition,
                maxTextWidth,
                lineHeight,
                scale
            );

            //if (CUOEnviroment.Debug)
            {
                batcher.DrawRectangle(SolidColorTextureCache.GetTexture(Color.Red), (int)startPosition.X, (int)startPosition.Y, (int)fullSize.X, (int)fullSize.Y, ref hue);
            }

            RenderDrawCommands(batcher);

            if (settings.Underline)
            {                      
                var texture = SolidColorTextureCache.GetTexture(Color.White);

                int count = Math.Max(1, (int) (fullSize.Y / lineHeight));
                float stroke = 1f;
                Vector2 end = new Vector2(startPosition.X + fullSize.X, startPosition.Y);

                for (int i = 0; i < count; ++i)
                {
                    startPosition.Y += lineHeight;
                    end.Y += lineHeight;

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
                            Vector3.UnitY,
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

            ResetFontDrawCmd();

            return allowSelection && mouseIsOver;
        }
     
        private void InternalDraw
        (
            ReadOnlySpan<char> text,
            in FontSettings settings,
            ref Vector2 position,
            ref Vector2 fullSize,
            ref Vector3 hue,
            Point mousePosition,
            bool allowSelection,
            ref bool mouseIsOver,
            Vector2 startPosition,
            float maxTextWidth, 
            float lineHeight,
            float scale
        )
        {
            ResetFontDrawCmd();

            Color color = Color.Red;

            maxTextWidth += 4 * scale;

            int last = 0;
            Vector2 wordSize = new Vector2(0, lineHeight);
            float totalSpaceWidth = 0.0f;
            float anotherYOffset = 0f;

            for (int i = 0; i < text.Length; ++i)
            {
                char c = text[i];

                if (c == '\r')
                {
                    continue;
                }

                if (c == ' ' || c == '\n')
                {
                    for (int j = last; j < i; ++j)
                    {
                        if (text[j] == '\r' || text[j] == '\n' || text[j] == ' ')
                        {
                            continue;
                        }

                        var texture = ReadChar(text[j], settings, out var uv, out var key);

                        if (texture != null)
                        {
                            PushFontDrawCmd(CommandType.Char, texture, position, uv, hue, color, scale, key);

                            wordSize.X += uv.Width * scale;
                            position.X += uv.Width * scale;
                        }
                    }

                    if (c == '\n' || (last > 0 && wordSize.X > maxTextWidth))
                    {
                        if (c == '\n')
                        {
                            PushFontDrawCmd(CommandType.NewLine, null, position, Rectangle.Empty, hue, color, scale, 0);
                        }

                        wordSize.X = 0;
                        wordSize.Y += lineHeight;

                        position.X = startPosition.X;
                        position.Y += lineHeight;
                        float offsetY = lineHeight;

                        if (c != '\n')
                        {
                            for (int j = last; j < i; ++j)
                            {
                                ref var cmd = ref _commands[j];

                                if (/*last > 0 &&*/ wordSize.X - 0 > maxTextWidth)
                                {
                                    fullSize.X = Math.Max(fullSize.X, wordSize.X);
                                    wordSize.X = 0;
                                    wordSize.Y += lineHeight;
                                    offsetY += lineHeight;
                                    anotherYOffset += lineHeight;
                                }

                                cmd.Position.X = startPosition.X + wordSize.X;
                                cmd.Position.Y += offsetY;

                                wordSize.X += cmd.UV.Width * scale;
                            }
                           
                            position.X += wordSize.X;                            
                        }   
                    }

                    if (c == ' ' && last != i + 1 && i + 1 != text.Length)
                    {
                        PushFontDrawCmd(CommandType.Space, null, position, new Rectangle(0, 0, DEFAULT_SPACE_SIZE, 0), hue, color, scale, 0);

                        wordSize.X += DEFAULT_SPACE_SIZE * scale;
                        position.X += DEFAULT_SPACE_SIZE * scale;
                        totalSpaceWidth += DEFAULT_SPACE_SIZE * scale;
                    }

                    fullSize.X = Math.Max(fullSize.X, wordSize.X);
                    fullSize.Y = Math.Max(fullSize.Y, wordSize.Y);

                    last = i + 1;
                }
            }

            if (last < text.Length)
            {
                position.Y += anotherYOffset;

                for (int i = last; i < text.Length; ++i)
                {
                    if (text[i] == '\r')
                    {
                        continue;
                    }

                    if (text[i] == '\n')
                    {
                        PushFontDrawCmd(CommandType.NewLine, null, position, Rectangle.Empty, hue, color, scale, 0);

                        position.X = startPosition.X;
                        position.Y += lineHeight;

                        wordSize.X = 0;
                        wordSize.Y += lineHeight;

                        continue;
                    }

                    if (text[i] == ' ')
                    {
                        PushFontDrawCmd(CommandType.Space, null, position, new Rectangle(0, 0, DEFAULT_SPACE_SIZE, 0), hue, color, scale, 0);

                        wordSize.X += DEFAULT_SPACE_SIZE * scale;
                        position.X += DEFAULT_SPACE_SIZE * scale;
                        totalSpaceWidth += DEFAULT_SPACE_SIZE * scale;

                        continue;
                    }

                    var texture = ReadChar(text[i], settings, out var uv, out var key);

                    if (texture != null)
                    {
                        PushFontDrawCmd(CommandType.Char, texture, position, uv, hue, color, scale, key);

                        wordSize.X += uv.Width * scale;
                        position.X += uv.Width * scale;
                    }
                }

                if (wordSize.X - totalSpaceWidth > maxTextWidth)
                {
                    wordSize.X = 0;
                    float offsetY = last > 0.0f ? lineHeight : 0f;
                    wordSize.Y += offsetY;

                    for (int i = last; i < _cmdCount; ++i)
                    {
                        ref var cmd = ref _commands[i];

                        if (/*last == 0 &&*/ wordSize.X > maxTextWidth)
                        {
                            fullSize.X = Math.Max(fullSize.X, wordSize.X);

                            wordSize.X = 0;
                            wordSize.Y += lineHeight;
                            offsetY += lineHeight;
                        }

                        cmd.Position.X = startPosition.X + wordSize.X;
                        cmd.Position.Y += offsetY;

                        wordSize.X += cmd.UV.Width * scale;
                    }
                }

                fullSize.X = Math.Max(fullSize.X, wordSize.X);
                fullSize.Y = Math.Max(fullSize.Y, wordSize.Y);
            }


            for (int i = 0; i < _cmdCount; ++i)
            {
                ref var cmd = ref _commands[i];

                switch (cmd.Type)
                {
                    case CommandType.Char:
                        
                        if (allowSelection && !mouseIsOver)
                        {
                            mouseIsOver = _picker.Get
                            (
                                cmd.Key,
                                (int)((mousePosition.X - cmd.Position.X) / scale),
                                (int)((mousePosition.Y - cmd.Position.Y) / scale)
                            );

                            if (mouseIsOver)
                            {
                                hue.X = 0x35;

                                FixVectorColor(ref hue, settings);
                                FixFontCmdHue(0, i, ref hue);
                            }
                        }

                        break;

                    case CommandType.Space:

                        //if (wordSize.X > maxTextWidth)
                        //{
                        //    wordSize.X = 0;
                        //    position.X = startPosition.X;
                        //    position.Y += lineHeight;
                        //    offsetY += lineHeight;
                        //}
                        //else
                        //{
                        //    value = DEFAULT_SPACE_SIZE * scale;
                        //}
                        
                        break;

                    case CommandType.NewLine:

                        //wordSize.X = 0;
                        //position.X = startPosition.X;
                        //position.Y += lineHeight;
                        //offsetY += lineHeight;

                        break;

                    default: continue;
                }

                cmd.UOHue = hue;
            }
        }
      

        private void PushFontDrawCmd
        (
            CommandType type,
            Texture2D texture, 
            Vector2 pos, 
            Rectangle uv, 
            Vector3 hue, 
            Color color,
            float scale,
            uint key
        )
        {
            if (_cmdCount >= _commands.Length)
            {
                Array.Resize(ref _commands, _cmdCount * 2);

                Log.Trace($"Font renderer ---> new font cmd length: {_commands.Length}");
            }

            ref var cmd = ref _commands[_cmdCount++];
            cmd.Type = type;
            cmd.Texture = texture;
            cmd.Position = pos;
            cmd.UV = uv;
            cmd.UOHue = hue;
            cmd.Color = color;
            cmd.Scale = scale;
            cmd.Key = key;
        }

        private void ResetFontDrawCmd()
        {
            _cmdCount = 0;
        }

        private void FixFontCmdHue(int startIndex, int endIndex, ref Vector3 hue)
        {
            if (startIndex < 0 || startIndex >= endIndex || endIndex > _cmdCount)
            {
                return;
            }

            for (; startIndex <= endIndex; ++startIndex)
            {
                _commands[startIndex].UOHue = hue;
            }
        }

        private void RenderDrawCommands(UltimaBatcher2D batcher)
        {
            for (int i = 0; i < _cmdCount; ++i)
            {
                ref var cmd = ref _commands[i];

                if (cmd.Texture != null && cmd.Type == CommandType.Char)
                {
                    batcher.Draw
                    (
                        cmd.Texture,
                        cmd.Position,
                        cmd.UV,
                        cmd.UOHue,
                        0f,
                        Vector2.Zero,
                        cmd.Scale,
                        SpriteEffects.None,
                        0f
                    );
                }
            }
        }

        public Vector2 MeasureString
        (
            ReadOnlySpan<char> text,
            in FontSettings settings,
            float scale
        )
        {
            return MeasureString(text, settings, scale, 0, Vector2.Zero);
        }

        public Vector2 MeasureString
        (
            ReadOnlySpan<char> text,
            in FontSettings settings, 
            float scale,
            float maxTextWidth
        )
        {
            return MeasureString(text, settings, scale, maxTextWidth, Vector2.Zero);
        }

        public Vector2 MeasureString
        (
            ReadOnlySpan<char> text,
            in FontSettings settings, 
            float scale,
            float maxTextWidth,
            Vector2 position
        )
        {
            if (maxTextWidth <= 0.0f)
            {
                maxTextWidth = MeasureStringInternal(text, settings, scale, position, 0f).X;
            }
            else
            {
                maxTextWidth *= scale;
            }

            Vector2 startPosition = position;
            float lineHeight = GetFontHeight(settings) * scale;
            Vector2 fullSize = new Vector2(0, lineHeight);
            Vector3 hue = Vector3.Zero;
            bool mouseIsOver = false;

            InternalDraw
            (
                text,
                settings,
                ref position,
                ref fullSize,
                ref hue,
                Point.Zero,
                false,
                ref mouseIsOver,
                startPosition,
                maxTextWidth,
                lineHeight,
                scale
            );

            //fullSize.X = Math.Max(maxTextWidth, fullSize.X);

            return fullSize;
        }

       
        private Vector2 MeasureStringInternal
        (
            ReadOnlySpan<char> text, 
            in FontSettings settings, 
            float scale, 
            Vector2 position,
            float maxTextWidth
        )
        {
            Vector2 size = new Vector2();
            Rectangle uv;
            float maxWidth = 0;
            float lineHeight = GetFontHeight(settings) * scale;
            maxTextWidth *= scale;
            Vector2 startPoint = position;

            for (int i = 0; i < text.Length; ++i)
            {
                if (text[i] == '\r')
                {
                    continue;
                }

                if (text[i] == ' ')
                {
                    size.X += DEFAULT_SPACE_SIZE * scale;

                    continue;
                }

                if (text[i] == '\n' || (maxTextWidth > 0.0f && size.X > maxTextWidth))
                {
                    maxWidth = size.X;
                    size.X = 0;
                    size.Y += lineHeight;

                    position.X = startPoint.X;
                    position.Y += lineHeight;

                    if (text[i] == '\n')
                    {
                        continue;
                    }
                }

                if (ReadChar(text[i], settings, out uv, out uint key) != null)
                {
                    lineHeight = Math.Max(lineHeight, uv.Height * scale);
                    size.X += uv.Width * scale;
                }
            }

            size.X = Math.Max(size.X, maxWidth);
            size.Y = Math.Max(size.Y, lineHeight);

            return size;
        }

        public float GetFontHeight(in FontSettings settings)
        {
            float height = 14;

            if (ReadChar('W', settings, out var uv, out _) != null)
            {
                height = Math.Max(height, uv.Height);
            }

            if (ReadChar('g', settings, out uv, out _) != null)
            {
                height = Math.Max(height, uv.Height);
            }
          
            return height;
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

            key = CreateKey(c, in settings);

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

            key = CreateKey(c, in settings);

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
            else if (color.Y > 0)
            {
                //color.Y = ShaderHueTranslator.SHADER_NONE;
            }
        }
        
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint CreateKey(char c, in FontSettings settings)
        {
            unchecked
            {
                uint hash = 17;
                hash = (uint)(hash * 31 + (int)c);
                hash = (uint)(hash * 31 + settings.FontIndex);
                hash = (uint)(hash * 31 + (settings.Bold ? 1 : 0));
                hash = (uint)(hash * 31 + (settings.Italic ? 1 : 0));
                //hash = (uint)(hash * 31 + settings.Underline.GetHashCode());
                hash = (uint)(hash * 31 + (settings.Border ? 1 : 0));
                //hash = (uint)(hash * 31 + (settings.IsHtml ? 1 : 0));
                hash = (uint)(hash * 31 + (settings.IsUnicode ? 1 : 0));

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

        private enum CommandType : byte
        {
            Char,
            NewLine,
            Space
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct FontDrawCmd
        {
            public CommandType Type;
            public Texture2D Texture;
            public Vector2 Position;
            public Rectangle UV;
            public Vector3 UOHue;
            public Color Color;
            public float Scale;
            public uint Key;
        }
    }
}
