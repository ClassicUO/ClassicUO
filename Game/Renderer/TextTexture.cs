using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game.Renderer
{
    public class TextTexture : Texture2D
    {
        public TextTexture(in GraphicsDevice device, in int width, in int height) : base(device, width, height)
        {

        }

        public TextTexture(in GraphicsDevice device, in int width, in int height, in bool mipMap, in SurfaceFormat format) : base(device, width, height, mipMap, format)
        {

        }

        public int LinesCount { get; set; }
    }
}
