using ClassicUO.Game.GameObjects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Renderer.Views
{
    public class StaticView : View
    {
        public StaticView(Static st) : base(st)
        {
            AllowedToDraw = !IsNoDrawable(st.Graphic);
        }

        //public new Static GameObject => (Static)base.GameObject;

        public override bool Draw(SpriteBatch3D spriteBatch,  Vector3 position)
        {
            if (!AllowedToDraw || GameObject.IsDisposed)
            {
                return false;
            }

            if (Texture == null || Texture.IsDisposed)
            {
                Texture = TextureManager.GetOrCreateStaticTexture(GameObject.Graphic);
                Bounds = new Rectangle(Texture.Width / 2 - 22, Texture.Height - 44 + GameObject.Position.Z * 4, Texture.Width, Texture.Height);
            }


            return base.Draw(spriteBatch, position);
        }
    }
}