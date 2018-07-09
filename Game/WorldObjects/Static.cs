using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.WorldObjects
{
    public class Static : WorldObject
    {
        public Static(Graphic tileID, Hue hue, int index)
        {
            TileID = tileID; Hue = hue; Index = index;
        }

        public Graphic TileID { get; }
        public Hue Hue { get; }
        public int Index { get; }
        public override Position Position { get; set; }

        protected override WorldRenderObject CreateView()
            => new StaticView(this);

        public new StaticView ViewObject => (StaticView)base.ViewObject;   
    }

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
