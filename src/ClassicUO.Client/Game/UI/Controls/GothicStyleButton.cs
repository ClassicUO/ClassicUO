using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace ClassicUO.Game.UI.Controls
{
    public class GothicStyleButton : Control
    {
        private string _text;
        private bool _isHovered;
        private bool _isPressed;
        private RenderedText _renderedText;
        private Color _baseColor;
        private Color _highlightColor;
        private Color _shadowColor;
        private Color _textColor;
        private Color _textShadowColor;
        private static readonly Vector3 _hueVector = ShaderHueTranslator.GetHueVector(0, false, 1f);

        public GothicStyleButton(int x, int y, int width, int height, string text, string fontPath = null, int fontSize = 16)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            _text = text;

            // Cores do tema gótico/medieval - Background vermelho escuro com texto branco
            _baseColor = Color.DarkRed;
            _highlightColor = new Color(180, 50, 50);
            _shadowColor = new Color(80, 15, 15);
            _textColor = Color.White;                      // Texto branco para contraste
            _textShadowColor = Color.Black;                // Sombra preta do texto

            _renderedText = RenderedText.Create(_text, 0x0481, 1, true, FontStyle.BlackBorder);
            AcceptMouseInput = true;
        }

        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                _renderedText.Text = value;
            }
        }

        public Color BaseColor
        {
            get => _baseColor;
            set => _baseColor = value;
        }

        public Color HighlightColor
        {
            get => _highlightColor;
            set => _highlightColor = value;
        }

        public Color ShadowColor
        {
            get => _shadowColor;
            set => _shadowColor = value;
        }

        public Color TextColor
        {
            get => _textColor;
            set => _textColor = value;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            Color currentBaseColor = _baseColor;
            Color currentHighlightColor = _highlightColor;
            Color currentShadowColor = _shadowColor;

            if (_isPressed)
            {
                currentBaseColor = new Color(
                    Math.Max(0, _baseColor.R - 25),
                    Math.Max(0, _baseColor.G - 25),
                    Math.Max(0, _baseColor.B - 25));
                currentHighlightColor = new Color(
                    Math.Max(0, _highlightColor.R - 35),
                    Math.Max(0, _highlightColor.G - 35),
                    Math.Max(0, _highlightColor.B - 35));
            }
            else if (_isHovered)
            {
                int boost = 22;
                currentBaseColor = new Color(
                    Math.Min(255, _baseColor.R + boost),
                    Math.Min(255, _baseColor.G + boost),
                    Math.Min(255, _baseColor.B + boost));
                currentHighlightColor = new Color(
                    Math.Min(255, _highlightColor.R + boost),
                    Math.Min(255, _highlightColor.G + boost),
                    Math.Min(255, _highlightColor.B + boost));
            }
            else
            {
                currentHighlightColor = new Color(
                    (_baseColor.R + _highlightColor.R) / 2,
                    (_baseColor.G + _highlightColor.G) / 2,
                    (_baseColor.B + _highlightColor.B) / 2);
            }

            batcher.Draw(SolidColorTextureCache.GetTexture(_shadowColor), new Rectangle(x + 3, y + 3, Width, Height), _hueVector);
            DrawGradientButton(batcher, x, y, Width, Height, currentBaseColor, currentShadowColor);
            DrawBorder(batcher, x, y, Width, Height, currentHighlightColor, currentShadowColor);
            DrawTextureEffect(batcher, x, y, Width, Height, currentBaseColor);

            if (!string.IsNullOrEmpty(_text))
            {
                var textX = x + (Width - _renderedText.Width) / 2;
                var textY = y + (Height - _renderedText.Height) / 2;
                if (_isPressed)
                {
                    textX += 1;
                    textY += 1;
                }
                _renderedText.Draw(batcher, textX, textY);
            }

            return base.Draw(batcher, x, y);
        }

        private void DrawGradientButton(UltimaBatcher2D batcher, int x, int y, int width, int height, Color baseColor, Color shadowColor)
        {
            for (int i = 0; i < height; i++)
            {
                float ratio = (float)i / height;
                int cr = (int)(baseColor.R + (shadowColor.R - baseColor.R) * ratio);
                int cg = (int)(baseColor.G + (shadowColor.G - baseColor.G) * ratio);
                int cb = (int)(baseColor.B + (shadowColor.B - baseColor.B) * ratio);
                var gradColor = new Color(cr, cg, cb, 255);
                batcher.Draw(SolidColorTextureCache.GetTexture(gradColor), new Rectangle(x, y + i, width, 1), _hueVector);
            }
        }

        private const int BORDER_RADIUS = 6;

        private void DrawBorder(UltimaBatcher2D batcher, int x, int y, int width, int height, Color highlightColor, Color shadowColor)
        {
            int r = BORDER_RADIUS;
            if (width < r * 2 || height < r * 2)
                r = 0;
            var highlightTex = SolidColorTextureCache.GetTexture(highlightColor);
            var shadowTex = SolidColorTextureCache.GetTexture(shadowColor);
            if (r > 0)
            {
                batcher.Draw(highlightTex, new Rectangle(x + r, y, width - r * 2, 2), _hueVector);
                batcher.Draw(shadowTex, new Rectangle(x + r, y + height - 2, width - r * 2, 2), _hueVector);
                batcher.Draw(highlightTex, new Rectangle(x, y + r, 2, height - r * 2), _hueVector);
                batcher.Draw(shadowTex, new Rectangle(x + width - 2, y + r, 2, height - r * 2), _hueVector);
                batcher.Draw(highlightTex, new Rectangle(x, y, r, r), _hueVector);
                batcher.Draw(highlightTex, new Rectangle(x + width - r, y, r, r), _hueVector);
                batcher.Draw(shadowTex, new Rectangle(x, y + height - r, r, r), _hueVector);
                batcher.Draw(shadowTex, new Rectangle(x + width - r, y + height - r, r, r), _hueVector);
            }
            else
            {
                batcher.Draw(highlightTex, new Rectangle(x, y, width, 2), _hueVector);
                batcher.Draw(shadowTex, new Rectangle(x, y + height - 2, width, 2), _hueVector);
                batcher.Draw(highlightTex, new Rectangle(x, y, 2, height), _hueVector);
                batcher.Draw(shadowTex, new Rectangle(x + width - 2, y, 2, height), _hueVector);
            }
        }

        private void DrawTextureEffect(UltimaBatcher2D batcher, int x, int y, int width, int height, Color baseColor)
        {
            var textureColor = new Color(
                Math.Max(0, baseColor.R - 20),
                Math.Max(0, baseColor.G - 10),
                Math.Max(0, baseColor.B - 8),
                255
            );
            var textureTex = SolidColorTextureCache.GetTexture(textureColor);
            var random = new Random(12345);
            for (int i = 4; i < width - 4; i += 6)
            {
                int lineX = x + i + random.Next(-2, 3);
                if (lineX >= x + 2 && lineX < x + width - 2)
                {
                    int lineHeight = height - 6 + random.Next(-2, 3);
                    int lineY = y + 3 + random.Next(-1, 2);
                    batcher.Draw(textureTex, new Rectangle(lineX, lineY, 1, lineHeight), _hueVector);
                }
            }
        }

        protected override void OnMouseEnter(int x, int y)
        {
            _isHovered = true;
        }

        protected override void OnMouseExit(int x, int y)
        {
            _isHovered = false;
        }

        protected override void OnMouseDown(int x, int y, ClassicUO.Input.MouseButtonType button)
        {
            if (button == ClassicUO.Input.MouseButtonType.Left)
            {
                _isPressed = true;
            }
        }

        protected override void OnMouseUp(int x, int y, ClassicUO.Input.MouseButtonType button)
        {
            if (button == ClassicUO.Input.MouseButtonType.Left)
            {
                _isPressed = false;

                if (MouseIsOver)
                {
                    OnClick?.Invoke();
                }
            }
        }

        public event Action OnClick = delegate { };

        public override void Dispose()
        {
            _renderedText?.Destroy();
            base.Dispose();
        }
    }
}
