using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Game.GameObjects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Renderer.Views
{
    public class GameTextView : View
    {
        private string _text;

        public GameTextView(in GameText parent) : base(parent)
        {
            _text = parent.Text;
        }

        public new GameText GameObject => (GameText) base.GameObject;



        public override void Update(in double frameMS)
        {
            

            base.Update(in frameMS);
        }

        public override bool Draw(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            return DrawInternal(in spriteBatch, in position);
        }

        public override bool DrawInternal(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            if (!AllowedToDraw)
                return false;

            if (_text != GameObject.Text || Texture == null || Texture.IsDisposed)
            {
                Texture = TextureManager.GetOrCreateStringTextTexture(GameObject);
                Bounds = new Rectangle(Texture.Width / 2 - 22, Texture.Height, Texture.Width, Texture.Height);

                _text = GameObject.Text;
            }
            
            Texture.Ticks = World.Ticks;
            HueVector = RenderExtentions.GetHueVector(GameObject.Hue, GameObject.IsPartialHue, false, false);

            return base.Draw(spriteBatch, position);
            //return spriteBatch.Draw2D(Texture, new Rectangle(GameObject.X + (int)position.X, GameObject.Y + (int)position.Y, GameObject.Width, GameObject.Height), HueVector);
        }

       
    }
}
