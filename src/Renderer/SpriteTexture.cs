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

using ClassicUO.IO.Resources;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer
{
    internal class SpriteTexture : Texture2D
    {
        private readonly bool _is32Bit;
        private bool[] _hitMap;

        public SpriteTexture(int width, int height, bool is32bit = true) : base(Engine.Batcher.GraphicsDevice, width, height, false, is32bit ? SurfaceFormat.Color : SurfaceFormat.Bgra5551)
        {
            Ticks = Engine.Ticks + 3000;
            _is32Bit = is32bit;
        }

        public long Ticks { get; set; }


        public void SetDataHitMap16(ushort[] data)
        {
            int size = Width * Height;
            _hitMap = new bool[size];

            for (int i = size - 1; i >= 0; --i)
                _hitMap[i] = data[i] != 0;

            SetData(data);
        }

        public unsafe void SetDataHitMap16(ushort* data)
        {
            int size = Width * Height;
            _hitMap = new bool[size];

            for (int i = size - 1; i >= 0; --i)
                _hitMap[i] = data[i] != 0;

            SetDataPointerEXT(0, new Rectangle(0, 0, Width, Height), (IntPtr) data, size);
        }

        public void SetDataHitMap32(uint[] data)
        {
            int size = Width * Height;
            _hitMap = new bool[size];

            for (int i = size - 1; i >= 0; --i)
                _hitMap[i] = data[i] != 0;

            SetData(data);
        }

        public bool Contains(int x, int y, bool pixelCheck = true)
        {
            if (x >= 0 && y >= 0 && x < Width && y < Height)
            {
                if (!pixelCheck)
                    return true;

                int pos = y * Width + x;

                if (pos < _hitMap.Length)
                    return _hitMap[pos];
            }

            return false;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _hitMap = null;
        }
    }

    internal class FontTexture : SpriteTexture
    {
        public FontTexture(int width, int height, int linescount, List<WebLinkRect> links) : base(width, height)
        {
            LinesCount = linescount;
            Links = links;
        }

        public int LinesCount { get; }

        public List<WebLinkRect> Links { get; }
    }

    internal class AnimationFrameTexture : SpriteTexture
    {
        public AnimationFrameTexture(int width, int height) : base(width, height, false)
        {
        }

        public short CenterX { get; set; }

        public short CenterY { get; set; }
    }

    internal class ArtTexture : SpriteTexture
    {
        public ArtTexture(int offsetX, int offsetY, int offsetW, int offsetH, int width, int height) : base(width, height, false)
        {
            ImageRectangle = new Rectangle(offsetX, offsetY, offsetW, offsetH);
        }

        public ArtTexture(Rectangle rect, int width, int height) : base(width, height, false)
        {
            ImageRectangle = rect;
        }

        public Rectangle ImageRectangle { get; }
    }
}