#region license

//  Copyright (C) 2018 ClassicUO Development Community on Github
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

using System.Collections.Generic;

namespace ClassicUO.Renderer
{
    public class PixelPicking
    {
        private const int InitialDataCount = 0x40000; // 256kb
        private readonly List<byte> _data = new List<byte>(InitialDataCount); // list<t> access is 10% slower than t[].
        private readonly Dictionary<int, int> _ends = new Dictionary<int, int>();
        private readonly Dictionary<int, int> _ids = new Dictionary<int, int>();

        public bool Get(int textureID, int x, int y, int extraRange = 0)
        {
            if (!_ids.TryGetValue(textureID, out int index)) return false;
            int width = ReadIntegerFromData(ref index);

            if (x < 0 || x >= width) return false;
            int height = ReadIntegerFromData(ref index);

            if (y < 0 || y >= height) return false;
            int current = 0;
            int target = x + y * width;
            bool inTransparentSpan = true;

            while (current < target)
            {
                int spanLength = ReadIntegerFromData(ref index);
                current += spanLength;

                if (extraRange == 0)
                {
                    if (target < current) return !inTransparentSpan;
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
                            if (y + range == y0 && x + extraRange >= x0 && x - extraRange <= x1)
                                return true;
                        }
                    }
                }

                inTransparentSpan = !inTransparentSpan;
            }

            return false;
        }

        public void GetDimensions(int textureID, out int width, out int height)
        {
            if (!_ids.TryGetValue(textureID, out int index))
            {
                width = height = 0;

                return;
            }

            width = ReadIntegerFromData(ref index);
            height = ReadIntegerFromData(ref index);
        }

        public void Set(int textureID, int width, int height, ushort[] pixels)
        {
            if (Has(textureID))
                return;
            int begin = _data.Count;
            WriteIntegerToData(width);
            WriteIntegerToData(height);
            bool countingTransparent = true;
            int count = 0;

            for (int i = 0; i < pixels.Length; i++)
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
            _ids[textureID] = begin;
            _ends[textureID] = _data.Count - begin;
        }

        public void Set(int textureID, int width, int height, uint[] pixels)
        {
            if (Has(textureID))
                return;
            int begin = _data.Count;
            WriteIntegerToData(width);
            WriteIntegerToData(height);
            bool countingTransparent = true;
            int count = 0;

            for (int i = 0; i < pixels.Length; i++)
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
            _ids[textureID] = begin;
            _ends[textureID] = _data.Count - begin;
        }

        public void Remove(int textureID)
        {
            //if (_ids.TryGetValue(textureID, out var index))
            //{
            //    //ReadIntegerFromData(ref index);
            //    //ReadIntegerFromData(ref index);

            //    int count = _ends[textureID];

            //    _data.RemoveRange(index, count);

            //    _ends.Remove(textureID);
            //    _ids.Remove(textureID);
            //}
        }

        private bool Has(int textureID)
        {
            return _ids.ContainsKey(textureID);
        }

        private void WriteIntegerToData(int value)
        {
            while (value > 0x7f)
            {
                _data.Add((byte) ((value & 0x7f) | 0x80));
                value >>= 7;
            }

            _data.Add((byte) value);
        }

        private int ReadIntegerFromData(ref int index)
        {
            int value = 0;
            int shift = 0;

            while (true)
            {
                byte data = _data[index++];
                value += (data & 0x7f) << shift;

                if ((data & 0x80) == 0x00) return value;
                shift += 7;
            }
        }
    }
}