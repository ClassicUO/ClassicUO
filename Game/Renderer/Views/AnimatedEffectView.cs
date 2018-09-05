using ClassicUO.Game.GameObjects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Renderer.Views
{
    public class AnimatedEffectView : View
    {
        private Graphic _displayedGraphic = Graphic.Invalid;

        public AnimatedEffectView(AnimatedItemEffect effect) : base(effect)
        {
        }

        public new AnimatedItemEffect GameObject => (AnimatedItemEffect)base.GameObject;


        public override bool Draw(SpriteBatch3D spriteBatch,  Vector3 position)
        {
            return !GameObject.IsDisposed && !PreDraw(position) && DrawInternal(spriteBatch, position);
        }

        public override bool DrawInternal(SpriteBatch3D spriteBatch,  Vector3 position)
        {
            if (GameObject.AnimationGraphic != _displayedGraphic)
            {
                _displayedGraphic = GameObject.AnimationGraphic;
                Texture = TextureManager.GetOrCreateStaticTexture(GameObject.AnimationGraphic);
                Bounds = new Rectangle(Texture.Width / 2 - 22, Texture.Height - 44 + GameObject.Position.Z * 4, Texture.Width, Texture.Height);
            }

            HueVector = RenderExtentions.GetHueVector(GameObject.Hue);

            return base.Draw(spriteBatch,  position);
        }

        public override void Update(double frameMS)
        {
            base.Update(frameMS);

            if (!GameObject.IsDisposed)
            {
                GameObject.UpdateAnimation(frameMS);
            }
        }
    }
}