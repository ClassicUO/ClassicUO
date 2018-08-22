using ClassicUO.Game.Renderer;
using ClassicUO.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ClassicUO.Game.Gumps
{
    public class Checkbox : GumpControl
    {
        private const int INACTIVE = 0;
        private const int ACTIVE = 1;

        private readonly SpriteTexture[] _textures = new SpriteTexture[2];


        public Checkbox(in GumpControl parent, in ushort inactive, in ushort active) : base(parent)
        {
            _textures[INACTIVE] = TextureManager.GetOrCreateGumpTexture(inactive);
            _textures[ACTIVE] = TextureManager.GetOrCreateGumpTexture(active);

            ref var t = ref _textures[INACTIVE];
            Bounds = t.Bounds;
        }


        public virtual bool IsChecked { get; set; }

        public override bool Draw(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            bool ok = base.Draw(in spriteBatch, in position);

            spriteBatch.Draw2D(IsChecked ? _textures[ACTIVE] : _textures[INACTIVE], position, HueVector);

            return ok;
        }


        public override void OnMouseButton(in MouseEventArgs e)
        {
            if (e.ButtonState == ButtonState.Released && e.Button == Input.MouseButton.Left)
            {
                IsChecked = !IsChecked;
            }
        }
    }
}
