using ClassicUO.Assets;
using ClassicUO.Input;
using ClassicUO.Renderer;
using FontStashSharp;
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
        private SpriteFontBase _font;
        private Color _baseColor;
        private Color _highlightColor;
        private Color _shadowColor;
        private Color _textColor;
        private Color _textShadowColor;
        private Texture2D _pixelTexture;

        public GothicStyleButton(int x, int y, int width, int height, string text, string fontPath = null, int fontSize = 16)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            _text = text;

            // Cores do tema gótico/medieval - Vermelho escuro com degradê mais realista
            _baseColor       = new Color(90, 20, 20);    // Tom de pedra vermelha escura (fundo principal)
            _highlightColor  = new Color(140, 40, 30);   // Realce interno, mais claro e quente
            _shadowColor     = new Color(40, 10, 10);    // Sombra bem profunda, quase preta
            //_edgeColor       = new Color(110, 30, 25);   // Bordas do relevo (intermediário entre base e highlight)
            //_glowColor       = new Color(180, 80, 40);   // Brilho externo alaranjado (aura quente)
            _textColor       = new Color(200, 160, 120); // Texto em dourado queimado (bem legível no fundo)
            _textShadowColor = new Color(30, 5, 5);      // Sombra de texto, contraste forte
            //_crackColor      = new Color(60, 15, 15);    // Fissuras/textura da pedra

            // Carregar fonte gótica se disponível, senão usar fonte padrão
            if (!string.IsNullOrEmpty(fontPath))
            {
                try
                {
                    _font = TrueTypeLoader.Instance.GetFont(fontPath, fontSize);
                }
                catch
                {
                    _font = null;
                }
            }

            if (_font == null)
            {
                // Usar fonte padrão do sistema
                _font = TrueTypeLoader.Instance.GetFont("Arial", fontSize);
            }

            AcceptMouseInput = true;
        }

        public string Text
        {
            get => _text;
            set => _text = value;
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
            Color currentBaseColor = _baseColor;
            Color currentHighlightColor = _highlightColor;
            Color currentShadowColor = _shadowColor;

            if (_isPressed)
            {
                // Quando pressionado, inverter as cores para dar efeito de "pressionado"
                currentBaseColor = _shadowColor;
                currentHighlightColor = _baseColor;
                currentShadowColor = new Color(_shadowColor.R - 20, _shadowColor.G - 10, _shadowColor.B - 10);
            }
            else if (_isHovered)
            {
                // Quando hover, clarear as cores
                currentBaseColor = new Color(
                    Math.Min(255, _baseColor.R + 20),
                    Math.Min(255, _baseColor.G + 10),
                    Math.Min(255, _baseColor.B + 10)
                );
                currentHighlightColor = new Color(
                    Math.Min(255, _highlightColor.R + 20),
                    Math.Min(255, _highlightColor.G + 10),
                    Math.Min(255, _highlightColor.B + 10)
                );
            }

            // Criar textura de pixel se não existir
            if (_pixelTexture == null)
            {
                _pixelTexture = new Texture2D(batcher.GraphicsDevice, 1, 1);
                _pixelTexture.SetData(new[] { Color.White });
            }

            // Desenhar sombra do botão (offset para baixo e direita)
            batcher.Draw(_pixelTexture, new Rectangle(x + 3, y + 3, Width, Height), new Vector3(currentShadowColor.R / 255f, currentShadowColor.G / 255f, currentShadowColor.B / 255f));

            // Desenhar o botão principal com degradê
            DrawGradientButton(batcher, x, y, Width, Height, currentBaseColor, currentShadowColor);

            // Desenhar borda com efeito 3D
            DrawBorder(batcher, x, y, Width, Height, currentHighlightColor, currentShadowColor);

            // Desenhar textura/rugosidade
            DrawTextureEffect(batcher, x, y, Width, Height, currentBaseColor);

            // Desenhar o texto
            if (!string.IsNullOrEmpty(_text) && _font != null)
            {
                var textSize = _font.MeasureString(_text);
                var textX = x + (Width - textSize.X) / 2;
                var textY = y + (Height - textSize.Y) / 2;

                // Offset do texto quando pressionado
                if (_isPressed)
                {
                    textX += 1;
                    textY += 1;
                }

                // Desenhar múltiplas sombras para efeito de inscrição
                _font.DrawText(batcher, new StringSegment(_text), new Vector2(textX + 2, textY + 2), _textShadowColor);
                _font.DrawText(batcher, new StringSegment(_text), new Vector2(textX + 1, textY + 1), new Color(_textShadowColor.R + 20, _textShadowColor.G + 10, _textShadowColor.B + 10));

                // Desenhar o texto principal com efeito de realce
                _font.DrawText(batcher, new StringSegment(_text), new Vector2(textX, textY), _textColor);
            }

            return base.Draw(batcher, x, y);
        }

        private void DrawGradientButton(UltimaBatcher2D batcher, int x, int y, int width, int height, Color baseColor, Color shadowColor)
        {
            // Criar degradê vertical do vermelho escuro para vermelho mais escuro
            int gradientSteps = height;
            for (int i = 0; i < gradientSteps; i++)
            {
                float ratio = (float)i / gradientSteps;
                
                // Interpolar entre a cor base e a cor de sombra
                int r = (int)(baseColor.R + (shadowColor.R - baseColor.R) * ratio);
                int g = (int)(baseColor.G + (shadowColor.G - baseColor.G) * ratio);
                int b = (int)(baseColor.B + (shadowColor.B - baseColor.B) * ratio);
                
                Color gradientColor = new Color(r, g, b);
                
                batcher.Draw(_pixelTexture, new Rectangle(x, y + i, width, 1), 
                    new Vector3(gradientColor.R / 255f, gradientColor.G / 255f, gradientColor.B / 255f));
            }
        }

        private void DrawBorder(UltimaBatcher2D batcher, int x, int y, int width, int height, Color highlightColor, Color shadowColor)
        {
            // Desenhar bordas com efeito 3D usando retângulos
            // Borda superior (realce)
            batcher.Draw(_pixelTexture, new Rectangle(x, y, width, 2), new Vector3(highlightColor.R / 255f, highlightColor.G / 255f, highlightColor.B / 255f));
            
            // Borda esquerda (realce)
            batcher.Draw(_pixelTexture, new Rectangle(x, y, 2, height), new Vector3(highlightColor.R / 255f, highlightColor.G / 255f, highlightColor.B / 255f));

            // Borda inferior (sombra)
            batcher.Draw(_pixelTexture, new Rectangle(x, y + height - 2, width, 2), new Vector3(shadowColor.R / 255f, shadowColor.G / 255f, shadowColor.B / 255f));
            
            // Borda direita (sombra)
            batcher.Draw(_pixelTexture, new Rectangle(x + width - 2, y, 2, height), new Vector3(shadowColor.R / 255f, shadowColor.G / 255f, shadowColor.B / 255f));
        }

        private void DrawTextureEffect(UltimaBatcher2D batcher, int x, int y, int width, int height, Color baseColor)
        {
            // Melhoria: Efeito de textura mais realista e orgânico
            var textureColor = new Color(
                Math.Max(0, baseColor.R - 12),
                Math.Max(0, baseColor.G - 6),
                Math.Max(0, baseColor.B - 6)
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
    }
}
