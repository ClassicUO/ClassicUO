using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace ClassicUO.Game.UI.Controls
{
    internal class DarkRedButton : Control
    {
        private string _text;
        private bool _isHovered;
        private bool _isPressed;
        private RenderedText _renderedText;

        private ushort _gumpNormal;
        private ushort _gumpHover;
        private ushort _gumpPressed;

        public DarkRedButton(int x, int y, string text, ushort gumpNormal, ushort gumpHover, ushort gumpPressed)
        {
            X = x;
            Y = y;
            _text = text;

            _gumpNormal = gumpNormal;
            _gumpHover = gumpHover;
            _gumpPressed = gumpPressed;

            _renderedText = RenderedText.Create(text ?? string.Empty, 0, 1, true, FontStyle.BlackBorder);

            var texture = GumpsLoader.Instance.GetGumpTexture(_gumpNormal, out Rectangle bounds);
            Width = bounds.Width;
            Height = bounds.Height;
        }

        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                _renderedText.Text = value ?? string.Empty;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ushort gumpID = _gumpNormal;
            if (_isPressed)
                gumpID = _gumpPressed;
            else if (_isHovered)
                gumpID = _gumpHover;

            var texture = GumpsLoader.Instance.GetGumpTexture(gumpID, out Rectangle bounds);

            if (texture != null)
            {
                var destination = new Rectangle(x, y, bounds.Width, bounds.Height);
                batcher.Draw(texture, destination, Vector3.One); // Vector3.One = cor branca
            }

            if (!string.IsNullOrEmpty(_text))
            {
                _renderedText.Text = _text;
                var textX = x + (Width - _renderedText.Width) / 2;
                var textY = y + (Height - _renderedText.Height) / 2;
                _renderedText.Draw(batcher, textX, textY);
            }

            return base.Draw(batcher, x, y);
        }

        public void UpdateMouseState(bool hover, bool pressed)
        {
            _isHovered = hover;
            _isPressed = pressed;
        }

        public event Action OnClick = delegate { };

        public void Click()
        {
            OnClick.Invoke();
        }

        public override void Dispose()
        {
            _renderedText?.Destroy();
            base.Dispose();
        }
    }
}
