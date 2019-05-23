using ClassicUO.Game.UI.Controls;

using Microsoft.Xna.Framework;

namespace ClassicUO.Renderer.UI
{
    internal class Panel : Control
    {
        private readonly Color _color;

        public Panel(int x, int y, int w, int h, Color color)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
            CanMove = true;
            AcceptMouseInput = true;
            WantUpdateSize = false;
            _color = color;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            Vector3 zero = Vector3.Zero;
            batcher.Draw2D(Textures.GetTexture(_color), x, y, Width, Height, ref zero);

            return base.Draw(batcher, x, y);
        }
    }
}