using ClassicUO.Game.GameObjects;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Views
{
    internal class MovingEffectView : View
    {
        private Graphic _displayedGraphic = Graphic.Invalid;

        public MovingEffectView(MovingEffect effect) : base(effect)
        {
        }

        public override bool Draw(SpriteBatch3D spriteBatch, Vector3 position, MouseOverList list)
        {
#if !ORIONSORT
            PreDraw(position);
#endif

            return DrawInternal(spriteBatch, position, list);
        }

        public override bool DrawInternal(SpriteBatch3D spriteBatch, Vector3 position, MouseOverList objectList)
        {
            if (GameObject.IsDisposed)
                return false;

            MovingEffect effect = (MovingEffect) GameObject;

            if (effect.AnimationGraphic != _displayedGraphic || Texture == null || Texture.IsDisposed)
            {
                _displayedGraphic = effect.AnimationGraphic;
                Texture = Art.GetStaticTexture(effect.AnimationGraphic);
                Bounds = new Rectangle(0, 0, Texture.Width, Texture.Height);
            }

            Bounds.X = (int)-effect.Offset.X;
            Bounds.Y = (int)(effect.Offset.Z - effect.Offset.Y);        
            Rotation = effect.AngleToTarget;
            HueVector = ShaderHuesTraslator.GetHueVector(GameObject.Hue);

            return base.Draw(spriteBatch, position, objectList);
        }
    }
}