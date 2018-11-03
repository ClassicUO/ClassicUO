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

using ClassicUO.IO.Resources;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer
{
    public class SpriteTexture : Texture2D
    {
        public SpriteTexture(int width, int height, bool is32bit = true) : base(Service.Get<SpriteBatch3D>().GraphicsDevice, width, height, false, is32bit ? SurfaceFormat.Color : SurfaceFormat.Bgra5551)
        {
            Ticks = CoreGame.Ticks + 3000;
        }

        public long Ticks { get; set; }
    }

    public class FontTexture : SpriteTexture
    {
        public FontTexture(int width, int height, int linescount, List<WebLinkRect> links) : base(width, height)
        {
            LinesCount = linescount;
            Links = links;
        }

        public int LinesCount { get; }

        public List<WebLinkRect> Links { get; }
    }

    public class TextureAnimationFrame : SpriteTexture
    {
        public TextureAnimationFrame(int id, int width, int height) : base(width, height, false)
        {
            ID = id;
        }

        public short CenterX { get; set; }

        public short CenterY { get; set; }

        public int ID { get; }
    }
}