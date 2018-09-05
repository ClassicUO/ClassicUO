using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps
{
    public class Label : GumpControl
    {
        private readonly GameText _gText;

        public Label() : base()
        {
            _gText = new GameText() { IsPersistent = true };
        }


        public string Text
        {
            get => _gText.Text;
            set => _gText.Text = value;
        }

        public Hue Hue
        {
            get => _gText.Hue;
            set => _gText.Hue = value;
        }

        public override bool Draw(SpriteBatch3D spriteBatch,  Vector3 position)
        {
            _gText.GetView().Draw(spriteBatch, position);
            return base.Draw(spriteBatch,  position);
        }
    }
}
