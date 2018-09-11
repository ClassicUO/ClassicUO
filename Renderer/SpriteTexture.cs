using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Renderer
{
    public class SpriteTexture : Texture2D
    {
        public SpriteTexture(int width, int height, bool is32bit = true) : base(Service.Get<SpriteBatch3D>().GraphicsDevice, width, height, false, is32bit ? SurfaceFormat.Color : SurfaceFormat.Bgra5551)
        {
        }

        public long Ticks { get; set; }
    }

}
