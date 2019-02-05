﻿using System;
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

        public override bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
        {
            Vector3 vec = new Vector3(0, 0, Alpha);

            if (ScaleTexture)
                return batcher.Draw2D(Texture, new Rectangle(position.X, position.Y, Width, Height), new Rectangle(0, 0, Texture.Width, Texture.Height), vec);
            else
                return batcher.Draw2D(Texture, position, vec);
        }
    }
}
