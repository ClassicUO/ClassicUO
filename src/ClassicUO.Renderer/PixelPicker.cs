using System;
using System.Collections.Generic;

namespace ClassicUO.Renderer
{
    public sealed class PixelPicker
    {
        const int InitialDataCount = 0x40000; // 256kb

        Dictionary<ulong, int> m_IDs = new Dictionary<ulong, int>();
        readonly List<byte> m_Data = new List<byte>(InitialDataCount); // list<t> access is 10% slower than t[].

        public bool Get(ulong textureID, int x, int y, int extraRange = 0, double scale = 1f)
        {
            int index;
            if (!m_IDs.TryGetValue(textureID, out index))
            {
                return false;
            }

            if (scale != 1f)
            {
                x = (int)(x / scale);
                y = (int)(y / scale);
            }

            int width = ReadIntegerFromData(ref index);


            if (x < 0 || x >= width)
            {
                return false;
            }

            if (y < 0 || y >= ReadIntegerFromData(ref index))
            {
                return false;
            }

            int current = 0;
            int target = x + y * width;
            bool inTransparentSpan = true;
            while (current < target)
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
                    if (!inTransparentSpan)
                    {
                        int y0 = current / width;
                        int x1 = current % width;
                        int x0 = x1 - spanLength;
                        for (int range = -extraRange; range <= extraRange; range++)
                        {
                            if (y + range == y0 && (x + extraRange >= x0) && (x - extraRange <= x1))
                            {
                                return true;
                            }
                        }
                    }
                }
                inTransparentSpan = !inTransparentSpan;
            }
            return false;
        }

        public void GetDimensions(ulong textureID, out int width, out int height)
        {
            int index;
            if (!m_IDs.TryGetValue(textureID, out index))
            {
                width = height = 0;
                return;
            }
            width = ReadIntegerFromData(ref index);
            height = ReadIntegerFromData(ref index);
        }

        public void Set(ulong textureID, int width, int height, ReadOnlySpan<uint> pixels)
        {
            if (Has(textureID))
            {
                return;
            }

            int begin = m_Data.Count;
            WriteIntegerToData(width);
            WriteIntegerToData(height);
            bool countingTransparent = true;
            int count = 0;
            for (int i = 0, len = width * height; i < len; i++)
            {
                bool isTransparent = pixels[i] == 0;
                if (countingTransparent != isTransparent)
                {
                    WriteIntegerToData(count);
                    countingTransparent = !countingTransparent;
                    count = 0;
                }
                count += 1;
            }
            WriteIntegerToData(count);
            m_IDs[textureID] = begin;
        }

        bool Has(ulong textureID)
        {
            return m_IDs.ContainsKey(textureID);
        }

        void WriteIntegerToData(int value)
        {
            while (value > 0x7f)
            {
                m_Data.Add((byte)((value & 0x7f) | 0x80));
                value >>= 7;
            }
            m_Data.Add((byte)value);
        }

        int ReadIntegerFromData(ref int index)
        {
            int value = 0;
            int shift = 0;
            while (true)
            {
                byte data = m_Data[index++];
                value += (data & 0x7f) << shift;
                if ((data & 0x80) == 0x00)
                {
                    return value;
                }
                shift += 7;
            }
        }
    }
}
