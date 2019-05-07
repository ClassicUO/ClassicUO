using ClassicUO.Game.UI.Controls;

using Microsoft.Xna.Framework;

namespace ClassicUO.Renderer.UI
{
    internal class Label : Control
    {
        private string _text;

        public Label(string text, int x, int y)
        {
            CanMove = true;
            AcceptMouseInput = false;


            X = x;
            Y = y;
            Text = text;
        }

        public string Text
        {
            get => _text;
            set
            {
                _text = value;

                Vector2 size = Fonts.Regular.MeasureString(_text);
                Width = (int) size.X;
                Height = (int) size.Y;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            batcher.DrawString(Fonts.Regular, Text, x, y, Vector3.Zero);

            return base.Draw(batcher, x, y);
        }
    }
}