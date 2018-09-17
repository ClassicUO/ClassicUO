using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.Controls
{
    public class WorldViewport : GumpControl
    {
        private Scenes.GameScene _scene;
        private Rectangle _rect;

        public WorldViewport(Scenes.GameScene scene, int x, int y, int width, int height) : base()
        {
            X = x; Y = y; Width = width; Height = height;
            _scene = scene;
            AcceptMouseInput = true;
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position)
        {
            _rect.X = (int)position.X;
            _rect.Y = (int)position.Y;
            _rect.Width = Width;
            _rect.Height = Height;

            spriteBatch.Draw2D(_scene.ViewportTexture, _rect, Vector3.Zero);

            return base.Draw(spriteBatch, position);
        }


    }
}
