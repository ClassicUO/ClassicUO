using ClassicUO.Game.WorldObjects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Renderer.Views
{
    public class StaticView : View
    {
        public StaticView(in Static st) : base(st)
        {
            AllowedToDraw = !IsNoDrawable(st.Graphic);
        }

        public override bool Draw(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            if (!AllowedToDraw)
                return false;

            if (Texture == null || Texture.IsDisposed)
            {
                Texture = TextureManager.GetOrCreateStaticTexture(WorldObject.Graphic);
                Bounds = new Rectangle(Texture.Width / 2 - 22, Texture.Height - 44 + WorldObject.Position.Z * 4,
                    Texture.Width, Texture.Height);
            }

            return base.Draw(spriteBatch, position);
        }
    }
}