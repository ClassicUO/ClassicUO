using ClassicUO.Assets;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using ClassicUO.Renderer.Lights;
using FontStashSharp;
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

        private ushort _gumpNormal;
        private ushort _gumpHover;
        private ushort _gumpPressed;
        private SpriteFontBase _font;

        public DarkRedButton(int x, int y, string text, ushort gumpNormal, ushort gumpHover, ushort gumpPressed, string fontPath, int fontSize)
        {
            X = x;
            Y = y;
            _text = text;

            _gumpNormal = gumpNormal;
            _gumpHover = gumpHover;
            _gumpPressed = gumpPressed;

            // Carregar a fonte TrueType
            _font = TrueTypeLoader.Instance.GetFont(fontPath, fontSize);

            // Pega tamanho do botão
            var texture = GumpsLoader.Instance.GetGumpTexture(_gumpNormal, out Rectangle bounds);
            Width = bounds.Width;
            Height = bounds.Height;
        }

        public string Text
        {
            get => _text;
            set => _text = value;
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
                var textSize = _font.MeasureString(_text); // retorna Vector2
                var textPos = new Vector2(
                    x + (Width - textSize.X) / 2,
                    y + (Height - textSize.Y) / 2
                );

                _font.DrawText(batcher, new StringSegment(_text), textPos, Color.White);
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
    }
}
