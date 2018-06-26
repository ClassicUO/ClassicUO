using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.UI
{
    public class Panel : Control
    {
        
        public Panel(int x, int y, int width, int heigth) : base(null, x, y, width, heigth)
        {

        }

        public Panel(Control parent, int x, int y, int width, int heigth) : base(parent, x, y, width, heigth)
        {

        }

        public Texture2D Texture { get; set; }
        public bool IsScrollable { get; set; }


        internal override void Draw(GameTime time, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Texture, Rectangle, Color.Red);

            base.Draw(time, spriteBatch);
        }

    }
}
