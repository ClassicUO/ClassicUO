#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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

using ClassicUO.IO.Resources;
using ClassicUO.Utility.Collections;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer
{
    internal class UOTexture32 : Texture2D
    {
        private uint[] _data;

        public UOTexture32(int width, int height) : base(Client.Game.GraphicsDevice, width, height, false, SurfaceFormat.Color)
        {
            Ticks = Time.Ticks + 3000;
        }

        public long Ticks { get; set; }
        public uint[] Data => _data;

        public void PushData(uint[] data)
        {
            _data = data;
            SetData(data);
        }

        public bool Contains(int x, int y, bool pixelCheck = true)
        {
            if (_data != null && x >= 0 && y >= 0 && x < Width && y < Height)
            {
                if (!pixelCheck)
                    return true;

                int pos = y * Width + x;

                if (pos < _data.Length)
                    return _data[pos] != 0;
            }

            return false;
        }
    }

    internal class FontTexture : UOTexture32
    {
        public FontTexture(int width, int height, int linescount, RawList<WebLinkRect> links) : base(width, height)
        {
            LinesCount = linescount;
            Links = links;
        }

        public int LinesCount { get; set; }

        public RawList<WebLinkRect> Links { get; }
    }

    internal class AnimationFrameTexture : UOTexture32
    {
        public AnimationFrameTexture(int width, int height) : base(width, height)
        {
        }

        public short CenterX { get; set; }

        public short CenterY { get; set; }
    }

    internal class ArtTexture : UOTexture32
    {
        public ArtTexture(int offsetX, int offsetY, int offsetW, int offsetH, int width, int height) : base(width, height)
        {
            ImageRectangle = new Rectangle(offsetX, offsetY, offsetW, offsetH);
        }

        public ArtTexture(Rectangle rect, int width, int height) : base(width, height)
        {
            ImageRectangle = rect;
        }

        public Rectangle ImageRectangle;
    }
}