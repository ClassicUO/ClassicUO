using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game.WorldObjects.Views
{
    public class StaticView : WorldRenderObject
    {
        public StaticView(in Static st) : base(st)
        {
            Texture = TextureManager.GetOrCreateStaticTexture(st.TileID);
            Bounds = new Rectangle(Texture.Width / 2 - 22, Texture.Height - 44 + (st.Position.Z * 4), Texture.Width, Texture.Height);
        }

        public override bool Draw(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            return base.Draw(spriteBatch, position);
        }
    }
}
