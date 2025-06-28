using System;
using System.Collections.Generic;

namespace ClassicUO.Renderer
{
    public sealed class PixelPicker
    {
        private struct TextureInfo
        {
            public int Width;
            public int Height;
            public int StartIndex;
        }

        private readonly Dictionary<ulong, TextureInfo> _ids = new();
        private byte[] _data = new byte[0x40000];
        private int _dataCount = 0;

        public bool Get(ulong textureID, int x, int y, int extraRange = 0)
        {
            if (!_ids.TryGetValue(textureID, out TextureInfo info))
            {
                return false;
            }

            if (x < 0 || x >= info.Width || y < 0 || y >= info.Height)
            {
                return false;
            }

            int index = info.StartIndex;
            int current = 0;
            int target = x + y * info.Width;
            bool inTransparentSpan = true;

            while (current <= target)
            {
                int spanLength = ReadIntegerFromData(ref index);
                current += spanLength;

                if (extraRange == 0)
                {
                    if (target < current)
                    {
                        return !inTransparentSpan;
                    }
                }
                else
                {
                    if (!inTransparentSpan && IsWithinRange(current, spanLength, x, y, info.Width, extraRange))
                    {
                        return true;
                    }
                }

                inTransparentSpan = !inTransparentSpan;
            }
            return false;
        }

        public void GetDimensions(ulong textureID, out int width, out int height)
        {
            if (_ids.TryGetValue(textureID, out TextureInfo info))
            {
                width = info.Width;
                height = info.Height;
            }
            else
            {
                width = height = 0;
            }
        }

        public void Set(ulong textureID, int width, int height, ReadOnlySpan<uint> pixels, bool replace = false)
        {
            if (_ids.ContainsKey(textureID))
            {
                if (!replace)
                {
                    return;
                }

                _ids.Remove(textureID);
            }

            int startIdx = _dataCount;
            bool isTransparent = true;
            int count = 0;

            for (int i = 0, len = width * height; i < len; i++)
            {
                bool pixelIsTransparent = pixels[i] == 0;
                if (isTransparent != pixelIsTransparent)
                {
                    WriteIntegerToData(count);
                    isTransparent = pixelIsTransparent;
                    count = 0;
                }
                count++;
            }
            WriteIntegerToData(count);

            _ids[textureID] = new TextureInfo { Width = width, Height = height, StartIndex = startIdx };
        }

        private bool IsWithinRange(int current, int spanLength, int x, int y, int width, int range)
        {
            int y0 = current / width;
            int x1 = current % width;
            int x0 = x1 - spanLength;

            for (int offsetY = -range; offsetY <= range; offsetY++)
            {
                if (y + offsetY == y0 && (x + range >= x0) && (x - range <= x1))
                {
                    return true;
                }
            }
            return false;
        }

        private void WriteIntegerToData(int value)
        {
            EnsureCapacity();
            while (value > 0x7F)
            {
                _data[_dataCount++] = (byte)((value & 0x7F) | 0x80);
                value >>= 7;
                EnsureCapacity();
            }
            _data[_dataCount++] = (byte)value;
        }

        private int ReadIntegerFromData(ref int index)
        {
            int value = 0;
            int shift = 0;

            while (true)
            {
                byte data = _data[index++];
                value |= (data & 0x7F) << shift;

                if ((data & 0x80) == 0)
                {
                    return value;
                }
                shift += 7;
            }
        }

        private void EnsureCapacity()
        {
            if (_dataCount >= _data.Length)
            {
                Array.Resize(ref _data, _dataCount * 2);
            }
        }
    }
}
