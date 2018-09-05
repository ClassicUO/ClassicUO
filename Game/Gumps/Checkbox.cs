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


        public Checkbox(ushort inactive,  ushort active) : base()
        {
            _textures[INACTIVE] = TextureManager.GetOrCreateGumpTexture(inactive);
            _textures[ACTIVE] = TextureManager.GetOrCreateGumpTexture(active);

            ref var t = ref _textures[INACTIVE];
            Width = t.Width;
            Height = t.Height;

            CanMove = false;
        }


        public virtual bool IsChecked { get; set; }

        public override bool Draw(SpriteBatch3D spriteBatch,  Vector3 position)
        {
            bool ok = base.Draw(spriteBatch,  position);

            for (int i = 0; i < _textures.Length; i++)
                _textures[i].Ticks = World.Ticks;

            spriteBatch.Draw2D(IsChecked ? _textures[ACTIVE] : _textures[INACTIVE], position, HueVector);

            return ok;
        }


        public override void OnMouseButton(MouseEventArgs e)
        {
            if (e.ButtonState == ButtonState.Released && e.Button == Input.MouseButton.Left)
            {
                IsChecked = !IsChecked;
            }
        }
    }
}
