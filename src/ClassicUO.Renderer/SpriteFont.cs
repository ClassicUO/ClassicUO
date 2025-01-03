// SPDX-License-Identifier: BSD-2-Clause

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace ClassicUO.Renderer
{
    public sealed class SpriteFont
    {
        private SpriteFont
        (
            Texture2D texture,
            List<Rectangle> glyph,
            List<Rectangle> cropping,
            List<char> characters,
            int lineSpacing,
            float spacing,
            List<Vector3> kerning,
            char? defaultCharacter
        )
        {
            Characters = new ReadOnlyCollection<char>(characters.ToArray());
            DefaultCharacter = defaultCharacter;
            LineSpacing = lineSpacing;
            Spacing = spacing;

            Texture = texture;
            GlyphData = glyph;
            CroppingData = cropping;
            Kerning = kerning;
            CharacterMap = characters;
        }


        internal ReadOnlyCollection<char> Characters { get; }
        internal char? DefaultCharacter { get; }
        internal int LineSpacing { get; }
        internal float Spacing { get; }
        internal Texture2D Texture { get; }
        internal List<Rectangle> GlyphData { get; }
        internal List<Rectangle> CroppingData { get; }
        internal List<Vector3> Kerning { get; }
        internal List<char> CharacterMap { get; }


        public Vector2 MeasureString(ReadOnlySpan<char> text)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            if (text.Length == 0)
            {
                return Vector2.Zero;
            }

            Vector2 result = Vector2.Zero;
            float curLineWidth = 0.0f;
            float finalLineHeight = LineSpacing;
            bool firstInLine = true;

            foreach (char c in text)
            {
                // Special characters
                if (c == '\r')
                {
                    continue;
                }

                if (c == '\n')
                {
                    result.X = Math.Max(result.X, curLineWidth);
                    result.Y += LineSpacing;
                    curLineWidth = 0.0f;
                    finalLineHeight = LineSpacing;
                    firstInLine = true;

                    continue;
                }

                /* Get the List index from the character map, defaulting to the
				 * DefaultCharacter if it's set.
				 */
                int index = CharacterMap.IndexOf(c);

                if (index == -1)
                {
                    if (!DefaultCharacter.HasValue)
                    {
                        index = CharacterMap.IndexOf('?');
                        //throw new ArgumentException(
                        //                            "Text contains characters that cannot be" +
                        //                            " resolved by this SpriteFont.",
                        //                            "text"
                        //                           );
                    }
                    else
                    {
                        index = CharacterMap.IndexOf(DefaultCharacter.Value);
                    }
                }

                /* For the first character in a line, always push the width
				 * rightward, even if the kerning pushes the character to the
				 * left.
				 */
                Vector3 cKern = Kerning[index];

                if (firstInLine)
                {
                    curLineWidth += Math.Abs(cKern.X);
                    firstInLine = false;
                }
                else
                {
                    curLineWidth += Spacing + cKern.X;
                }

                /* Add the character width and right-side bearing to the line
				 * width.
				 */
                curLineWidth += cKern.Y + cKern.Z;

                /* If a character is taller than the default line height,
				 * increase the height to that of the line's tallest character.
				 */
                int cCropHeight = CroppingData[index].Height;

                if (cCropHeight > finalLineHeight)
                {
                    finalLineHeight = cCropHeight;
                }
            }

            // Calculate the final width/height of the text box
            result.X = Math.Max(result.X, curLineWidth);
            result.Y += finalLineHeight;

            return result;
        }


        internal static SpriteFont Create(GraphicsDevice device, ReadOnlySpan<byte> resource)
        {
            using (var ms = new MemoryStream(resource.ToArray()))
            using (var reader = new BinReader(ms))
            {
                reader.ReadByte();
                reader.ReadByte();
                reader.ReadByte();
                char c = reader.ReadChar();


                byte version = reader.ReadByte();
                byte flags = reader.ReadByte();
                bool compressed = (flags & 0x80) != 0;

                if (version != 5 && version != 4)
                {
                    throw new ContentLoadException("Invalid XNB version");
                }

                int xnbLength = reader.ReadInt32();

                int numberOfReaders = reader.Read7BitEncodedInt();

                for (int i = 0; i < numberOfReaders; i++)
                {
                    string originalReaderTypeString = reader.ReadString();
                    reader.ReadInt32();
                }

                int shared = reader.Read7BitEncodedInt();
                int typeReaderIndex = reader.Read7BitEncodedInt();
                reader.Read7BitEncodedInt();

                SurfaceFormat format = (SurfaceFormat) reader.ReadInt32();
                int width = reader.ReadInt32();
                int height = reader.ReadInt32();
                int levelCount = reader.ReadInt32();

                int levelDataSizeInBytes = reader.ReadInt32();
                byte[] levelData = null; // Don't assign this quite yet...
                int levelWidth = width >> 0;
                int levelHeight = height >> 0;
                levelData = reader.ReadBytes(levelDataSizeInBytes);


                if (format != SurfaceFormat.Color)
                {
                    levelData = DecompressDxt3(levelData, levelWidth, levelHeight);
                    levelDataSizeInBytes = levelData.Length;
                }

                Texture2D texture = new Texture2D
                (
                    device,
                    width,
                    height,
                    false,
                    SurfaceFormat.Color
                );

                unsafe
                {
                    fixed (byte* ptr = levelData)
                        texture.SetDataPointerEXT(0, null, (IntPtr)ptr, width * height * sizeof(byte));
                }

                reader.Read7BitEncodedInt();
                int glyphCount = reader.ReadInt32();
                List<Rectangle> glyphs = new List<Rectangle>(glyphCount);

                for (int i = 0; i < glyphCount; i++)
                {
                    int x = reader.ReadInt32();
                    int y = reader.ReadInt32();
                    int w = reader.ReadInt32();
                    int h = reader.ReadInt32();

                    glyphs.Add(new Rectangle(x, y, w, h));
                }

                reader.Read7BitEncodedInt();
                int croppingCount = reader.ReadInt32();
                List<Rectangle> croppings = new List<Rectangle>(croppingCount);

                for (int i = 0; i < croppingCount; i++)
                {
                    int x = reader.ReadInt32();
                    int y = reader.ReadInt32();
                    int w = reader.ReadInt32();
                    int h = reader.ReadInt32();

                    croppings.Add(new Rectangle(x, y, w, h));
                }

                reader.Read7BitEncodedInt();
                int charCount = reader.ReadInt32();
                List<char> charMap = new List<char>(charCount);

                for (int i = 0; i < charCount; i++)
                {
                    charMap.Add(reader.ReadChar());
                }

                int lineSpacing = reader.ReadInt32();
                float spacing = reader.ReadSingle();

                reader.Read7BitEncodedInt();
                int kerningCount = reader.ReadInt32();
                List<Vector3> kernings = new List<Vector3>(croppingCount);

                for (int i = 0; i < kerningCount; i++)
                {
                    float x = reader.ReadSingle();
                    float y = reader.ReadSingle();
                    float z = reader.ReadSingle();

                    kernings.Add(new Vector3(x, y, z));
                }

                char? defaultChar = null;

                if (reader.ReadBoolean())
                {
                    defaultChar = reader.ReadChar();
                }


                return new SpriteFont
                (
                    texture,
                    glyphs,
                    croppings,
                    charMap,
                    lineSpacing,
                    spacing,
                    kernings,
                    defaultChar
                );
            }
        }

        private static byte[] DecompressDxt3(byte[] imageData, int width, int height)
        {
            using (MemoryStream imageStream = new MemoryStream(imageData))
            {
                return DecompressDxt3(imageStream, width, height);
            }
        }

        private static byte[] DecompressDxt3(Stream imageStream, int width, int height)
        {
            byte[] imageData = new byte[width * height * 4];

            using (BinaryReader imageReader = new BinaryReader(imageStream))
            {
                int blockCountX = (width + 3) >> 2;
                int blockCountY = (height + 3) >> 2;

                for (int y = 0; y < blockCountY; y++)
                {
                    for (int x = 0; x < blockCountX; x++)
                    {
                        DecompressDxt3Block
                        (
                            imageReader,
                            x,
                            y,
                            blockCountX,
                            width,
                            height,
                            imageData
                        );
                    }
                }
            }

            return imageData;
        }

        private static void ConvertRgb565ToRgb888(ushort color, out byte r, out byte g, out byte b)
        {
            int temp = (color >> 11) * 255 + 16;
            r = (byte) ((temp / 32 + temp) / 32);
            temp = ((color & 0x07E0) >> 5) * 255 + 32;
            g = (byte) ((temp / 64 + temp) / 64);
            temp = (color & 0x001F) * 255 + 16;
            b = (byte) ((temp / 32 + temp) / 32);
        }

        private static void DecompressDxt3Block
        (
            BinaryReader imageReader,
            int x,
            int y,
            int blockCountX,
            int width,
            int height,
            byte[] imageData
        )
        {
            byte a0 = imageReader.ReadByte();
            byte a1 = imageReader.ReadByte();
            byte a2 = imageReader.ReadByte();
            byte a3 = imageReader.ReadByte();
            byte a4 = imageReader.ReadByte();
            byte a5 = imageReader.ReadByte();
            byte a6 = imageReader.ReadByte();
            byte a7 = imageReader.ReadByte();

            ushort c0 = imageReader.ReadUInt16();
            ushort c1 = imageReader.ReadUInt16();

            ConvertRgb565ToRgb888(c0, out byte r0, out byte g0, out byte b0);
            ConvertRgb565ToRgb888(c1, out byte r1, out byte g1, out byte b1);

            uint lookupTable = imageReader.ReadUInt32();

            int alphaIndex = 0;

            for (int blockY = 0; blockY < 4; blockY++)
            {
                for (int blockX = 0; blockX < 4; blockX++)
                {
                    byte r = 0, g = 0, b = 0, a = 0;

                    uint index = (lookupTable >> (2 * (4 * blockY + blockX))) & 0x03;

                    switch (alphaIndex)
                    {
                        case 0:
                            a = (byte) ((a0 & 0x0F) | ((a0 & 0x0F) << 4));

                            break;

                        case 1:
                            a = (byte) ((a0 & 0xF0) | ((a0 & 0xF0) >> 4));

                            break;

                        case 2:
                            a = (byte) ((a1 & 0x0F) | ((a1 & 0x0F) << 4));

                            break;

                        case 3:
                            a = (byte) ((a1 & 0xF0) | ((a1 & 0xF0) >> 4));

                            break;

                        case 4:
                            a = (byte) ((a2 & 0x0F) | ((a2 & 0x0F) << 4));

                            break;

                        case 5:
                            a = (byte) ((a2 & 0xF0) | ((a2 & 0xF0) >> 4));

                            break;

                        case 6:
                            a = (byte) ((a3 & 0x0F) | ((a3 & 0x0F) << 4));

                            break;

                        case 7:
                            a = (byte) ((a3 & 0xF0) | ((a3 & 0xF0) >> 4));

                            break;

                        case 8:
                            a = (byte) ((a4 & 0x0F) | ((a4 & 0x0F) << 4));

                            break;

                        case 9:
                            a = (byte) ((a4 & 0xF0) | ((a4 & 0xF0) >> 4));

                            break;

                        case 10:
                            a = (byte) ((a5 & 0x0F) | ((a5 & 0x0F) << 4));

                            break;

                        case 11:
                            a = (byte) ((a5 & 0xF0) | ((a5 & 0xF0) >> 4));

                            break;

                        case 12:
                            a = (byte) ((a6 & 0x0F) | ((a6 & 0x0F) << 4));

                            break;

                        case 13:
                            a = (byte) ((a6 & 0xF0) | ((a6 & 0xF0) >> 4));

                            break;

                        case 14:
                            a = (byte) ((a7 & 0x0F) | ((a7 & 0x0F) << 4));

                            break;

                        case 15:
                            a = (byte) ((a7 & 0xF0) | ((a7 & 0xF0) >> 4));

                            break;
                    }

                    ++alphaIndex;

                    switch (index)
                    {
                        case 0:
                            r = r0;
                            g = g0;
                            b = b0;

                            break;

                        case 1:
                            r = r1;
                            g = g1;
                            b = b1;

                            break;

                        case 2:
                            r = (byte) ((2 * r0 + r1) / 3);
                            g = (byte) ((2 * g0 + g1) / 3);
                            b = (byte) ((2 * b0 + b1) / 3);

                            break;

                        case 3:
                            r = (byte) ((r0 + 2 * r1) / 3);
                            g = (byte) ((g0 + 2 * g1) / 3);
                            b = (byte) ((b0 + 2 * b1) / 3);

                            break;
                    }

                    int px = (x << 2) + blockX;
                    int py = (y << 2) + blockY;

                    if (px < width && py < height)
                    {
                        int offset = (py * width + px) << 2;
                        imageData[offset] = r;
                        imageData[offset + 1] = g;
                        imageData[offset + 2] = b;
                        imageData[offset + 3] = a;
                    }
                }
            }
        }


        private class BinReader : BinaryReader
        {
            public BinReader(Stream input) : base(input)
            {
            }

            public BinReader(Stream input, Encoding encoding) : base(input, encoding)
            {
            }

            public BinReader(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen)
            {
            }

            public new int Read7BitEncodedInt()
            {
                return base.Read7BitEncodedInt();
            }
        }
    }
}
