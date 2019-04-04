using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    class TextureControl : Control
    {
        public TextureControl()
        {
            CanMove = true;
            AcceptMouseInput = true;
            ScaleTexture = true;
        }

        public bool ScaleTexture { get; set; }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (Texture != null)
                Texture.Ticks = Engine.Ticks;
        }

        public Hue Hue { get; set; }
        public bool IsPartial { get; set; }

        public override bool Draw(Batcher2D batcher, int x, int y)
        {
            Vector3 vec = ShaderHuesTraslator.GetHueVector(Hue, IsPartial, Alpha, false);

            if (ScaleTexture)
            {
                if (Texture is ArtTexture artTexture)
                {
                    int w = Width;
                    int h = Height;

                    if (artTexture.ImageRectangle.Width < Width)
                    {
                        w = artTexture.ImageRectangle.Width;
                        x += Width / 2 - w / 2;
                    }

                    if (artTexture.ImageRectangle.Height < Height)
                    {
                        h = artTexture.ImageRectangle.Height;
                        y += Height / 2 - h / 2;
                    }


                    var r = artTexture.ImageRectangle;

                    return batcher.Draw2D(Texture, x, y, w, h, r.X, r.Y, r.Width, r.Height, vec);
                }
                return batcher.Draw2D(Texture, x, y, Width, Height, 0, 0, Texture.Width, Texture.Height, vec);
            }
            return batcher.Draw2D(Texture, x, y, vec);
        }
    }
}
