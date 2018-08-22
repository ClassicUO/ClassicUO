using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps
{
    public class CroppedText : GumpControl
    {
        private GameText _gameText;
        private readonly int _index;

        public CroppedText(in GumpControl parent) : base(parent)
        {
            _gameText = new GameText() { IsPersistent = true };
        }

        public CroppedText(in GumpControl parent, in string[] parts, in string[] lines) : this(parent)
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            Width = int.Parse(parts[3]);
            Height = int.Parse(parts[4]);
            Hue = Hue.Parse(parts[5]);
            _index = int.Parse(parts[6]);

            _gameText.MaxWidth = (byte)Width;

            Text = lines[_index];
        }


        public Hue Hue { get; set; }

        public string Text
        {
            get => _gameText.Text;
            set => _gameText.Text = value;
        }


        public override bool Draw(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            _gameText.View.Draw(spriteBatch, position);
            return base.Draw(spriteBatch, position);
        }
    }
}
