using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace ClassicUO.Game.UI.Controls
{
    public class GothicStyleButtonLogin : Control
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
        private Texture2D _pixelTexture;

        public GothicStyleButtonLogin(int x, int y, int width, int height, string text, string fontPath = null, int fontSize = 16)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            _text = text;

            // Cores do tema gótico/medieval - Background vermelho escuro com texto branco
            _baseColor = Color.DarkRed;                    // Background vermelho escuro
            _highlightColor = new Color(180, 50, 50);      // Realce mais claro para bordas
            _shadowColor = new Color(80, 15, 15);          // Sombra mais escura
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
            // Ajustar cores baseado no estado
            Color currentBaseColor = _shadowColor;
            Color currentHighlightColor = _shadowColor;
            Color currentShadowColor = _shadowColor;

            if (_isPressed)
            {
                // Quando pressionado, inverter as cores para dar efeito de "pressionado"
                currentBaseColor = _baseColor;
                currentHighlightColor = _highlightColor;
                currentShadowColor = _shadowColor;
            }
            else if (_isHovered)
            {
                currentBaseColor = _baseColor;
                currentHighlightColor = _shadowColor;
                currentShadowColor = _shadowColor;
            }

            // Criar textura de pixel se não existir
            if (_pixelTexture == null)
            {
                _pixelTexture = new Texture2D(batcher.GraphicsDevice, 1, 1);
                _pixelTexture.SetData(new[] { Color.Red });
            }

            batcher.Draw(_pixelTexture, new Rectangle(x + 3, y + 3, Width, Height), Color.DarkRed.ToVector3());
            DrawGradientButton(batcher, x, y, Width, Height, currentBaseColor, currentShadowColor);

            // Desenhar borda com efeito 3D
            DrawBorder(batcher, x, y, Width, Height, currentHighlightColor, currentShadowColor);

            // Desenhar textura/rugosidade
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
                batcher.Draw(_pixelTexture, new Rectangle(x, y + i, width, 1),
                    new Vector3(cr / 255f, cg / 255f, cb / 255f));
            }
        }

        private const int BORDER_RADIUS = 6;

        private void DrawBorder(UltimaBatcher2D batcher, int x, int y, int width, int height, Color highlightColor, Color shadowColor)
        {
            int r = BORDER_RADIUS;
            if (width < r * 2 || height < r * 2)
            {
                r = 0;
            }
            Vector3 highlightVec = new Vector3(highlightColor.R / 255f, highlightColor.G / 255f, highlightColor.B / 255f);
            Vector3 shadowVec = new Vector3(shadowColor.R / 255f, shadowColor.G / 255f, shadowColor.B / 255f);
            if (r > 0)
            {
                batcher.Draw(_pixelTexture, new Rectangle(x + r, y, width - r * 2, 2), highlightVec);
                batcher.Draw(_pixelTexture, new Rectangle(x + r, y + height - 2, width - r * 2, 2), shadowVec);
                batcher.Draw(_pixelTexture, new Rectangle(x, y + r, 2, height - r * 2), highlightVec);
                batcher.Draw(_pixelTexture, new Rectangle(x + width - 2, y + r, 2, height - r * 2), shadowVec);
                batcher.Draw(_pixelTexture, new Rectangle(x, y, r, r), highlightVec);
                batcher.Draw(_pixelTexture, new Rectangle(x + width - r, y, r, r), highlightVec);
                batcher.Draw(_pixelTexture, new Rectangle(x, y + height - r, r, r), shadowVec);
                batcher.Draw(_pixelTexture, new Rectangle(x + width - r, y + height - r, r, r), shadowVec);
            }
            else
            {
                batcher.Draw(_pixelTexture, new Rectangle(x, y, width, 2), highlightVec);
                batcher.Draw(_pixelTexture, new Rectangle(x, y + height - 2, width, 2), shadowVec);
                batcher.Draw(_pixelTexture, new Rectangle(x, y, 2, height), highlightVec);
                batcher.Draw(_pixelTexture, new Rectangle(x + width - 2, y, 2, height), shadowVec);
            }
        }

        private void DrawTextureEffect(UltimaBatcher2D batcher, int x, int y, int width, int height, Color baseColor)
        {
            // Efeito de textura vermelha mais sutil
            var textureColor = new Color(
                Math.Max(0, baseColor.R - 20),    // Vermelho mais escuro para textura
                Math.Max(0, baseColor.G - 10),    // Verde reduzido
                Math.Max(0, baseColor.B - 8)      // Azul reduzido
            );

            // Desenhar padrão de textura mais orgânico (não uniforme)
            Random random = new Random(12345); // Seed fixo para consistência
            
            for (int i = 4; i < width - 4; i += 6)
            {
                int lineX = x + i + random.Next(-2, 3); // Variação sutil na posição
                if (lineX >= x + 2 && lineX < x + width - 2)
                {
                    int lineHeight = height - 6 + random.Next(-2, 3); // Variação na altura
                    int lineY = y + 3 + random.Next(-1, 2); // Variação na posição Y
                    
                    // Linha com variação de opacidade
                    var lineColor = new Color(
                        textureColor.R,
                        textureColor.G,
                        textureColor.B,
                        (byte)(180 + random.Next(-30, 31)) // Variação de opacidade
                    );
                    
                    batcher.Draw(_pixelTexture, new Rectangle(lineX, lineY, 1, lineHeight), 
                        new Vector3(lineColor.R / 255f, lineColor.G / 255f, lineColor.B / 255f));
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