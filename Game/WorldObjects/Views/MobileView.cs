using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.WorldObjects.Views
{
    public class MobileView : WorldRenderObject
    {
        public MobileView(in Mobile mobile) : base(mobile)
        {
            Texture = TextureManager.GetOrCreateStaticTexture(567);
            Bounds = new Rectangle(Texture.Width / 2 - 22, Texture.Height - 44 + (WorldObject.Position.Z * 4), Texture.Width, Texture.Height);
        }

        public override bool Draw(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            return base.Draw(spriteBatch, position);
        }

        public override void Update()
        {
            if (WorldObject == World.Player)
                World.Player.CheckIfNeedToMove();

            base.Update();
        }
    }
}
