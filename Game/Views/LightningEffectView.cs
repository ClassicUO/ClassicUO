using ClassicUO.Game.GameObjects;
using ClassicUO.Input;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Views
{
    internal class LightningEffectView : View
    {
        private static readonly Point[] _offsets =
        {
            new Point(48, 0), new Point(68, 0), new Point(92, 0), new Point(72, 0), new Point(48, 0), new Point(56, 0), new Point(76, 0), new Point(76, 0), new Point(92, 0), new Point(80, 0)
        };
        private Graphic _displayed = Graphic.Invalid;

        public LightningEffectView(LightningEffect effect) : base(effect)
        {
        }

        public override bool Draw(SpriteBatch3D spriteBatch, Vector3 position, MouseOverList list)
        {
            LightningEffect effect = (LightningEffect)GameObject;

            if (effect.AnimationGraphic != _displayed || Texture == null || Texture.IsDisposed)
            {
                _displayed = effect.AnimationGraphic;

                if (_displayed > 0x4E29)
                    return false;
                Texture = IO.Resources.Gumps.GetGumpTexture(_displayed);
                Point offset = _offsets[_displayed - 20000];
                Bounds = new Rectangle(offset.X, Texture.Height - 33 + offset.Y, Texture.Width, Texture.Height);
            }

            HueVector = ShaderHuesTraslator.GetHueVector(effect.Hue);

            return base.Draw(spriteBatch, position, list);
        }
    }
}