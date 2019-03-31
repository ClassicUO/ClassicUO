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

        public override bool Draw(Batcher2D batcher, Point position)
        {
            Vector3 vec = ShaderHuesTraslator.GetHueVector(Hue, IsPartial, Alpha, false);

            if (ScaleTexture)
            {
                if (Texture is ArtTexture artTexture)
                {
                    var rect = new Rectangle(position.X, position.Y, Width, Height);

                    if (artTexture.ImageRectangle.Width < Width)
                        rect.Width = artTexture.ImageRectangle.Width;
                    if (artTexture.ImageRectangle.Height < Height)
                        rect.Height = artTexture.ImageRectangle.Height;

                    return batcher.Draw2D(Texture, rect, artTexture.ImageRectangle, vec);
                }
                return batcher.Draw2D(Texture, new Rectangle(position.X, position.Y, Width, Height), new Rectangle(0, 0, Texture.Width, Texture.Height), vec);
            }
            return batcher.Draw2D(Texture, position, vec);
        }
    }
}
