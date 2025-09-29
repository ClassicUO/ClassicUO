using ClassicUO.Assets;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Renderer;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;

namespace ClassicUO.Game.UI.Controls
{
    public class GothicStyleCombobox : Control
    {
        private string[] _items;
        private int _selectedIndex;
        private string _selectedText;
        private bool _isHovered;
        private bool _isPressed;
        private bool _isOpen;
        private SpriteFontBase _font;
        private Color _baseColor;
        private Color _highlightColor;
        private Color _shadowColor;
        private Color _textColor;
        private Color _textShadowColor;
        private Texture2D _pixelTexture;
        private int _fontSize;

        public GothicStyleCombobox(int x, int y, int width, int height, string[] items, int selectedIndex = -1, int fontSize = 16)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            _items = items ?? new string[0];
            _selectedIndex = selectedIndex;
            _fontSize = fontSize;

            // Cores do tema gótico/medieval - Background vermelho escuro com texto branco
            _baseColor = Color.DarkRed;                    // Background vermelho escuro
            _highlightColor = new Color(180, 50, 50);      // Realce mais claro para bordas
            _shadowColor = new Color(80, 15, 15);          // Sombra mais escura
            _textColor = Color.White;                      // Texto branco para contraste
            _textShadowColor = Color.Black;                // Sombra preta do texto

            // Carregar fonte
            _font = TrueTypeLoader.Instance.GetFont("Arial", fontSize);

            // Definir texto inicial
            if (_selectedIndex >= 0 && _selectedIndex < _items.Length)
            {
                _selectedText = _items[_selectedIndex];
            }
            else
            {
                _selectedText = _items.Length > 0 ? _items[0] : "";
                _selectedIndex = _items.Length > 0 ? 0 : -1;
            }

            AcceptMouseInput = true;
        }

        public string[] Items
        {
            get => _items;
            set
            {
                _items = value ?? new string[0];
                if (_selectedIndex >= _items.Length)
                {
                    _selectedIndex = _items.Length > 0 ? 0 : -1;
                    _selectedText = _items.Length > 0 ? _items[0] : "";
                }
            }
        }

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (value >= 0 && value < _items.Length)
                {
                    _selectedIndex = value;
                    _selectedText = _items[value];
                    OnSelectionChanged?.Invoke(this, value);
                }
            }
        }

        public string SelectedText => _selectedText;

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

        public event Action<GothicStyleCombobox, int> OnSelectionChanged;

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            // Ajustar cores baseado no estado
            Color currentBaseColor = _baseColor;
            Color currentHighlightColor = _highlightColor;
            Color currentShadowColor = _shadowColor;

            if (_isPressed || _isOpen)
            {
                // Quando pressionado ou aberto, inverter as cores
                currentBaseColor = _shadowColor;
                currentHighlightColor = _baseColor;
                currentShadowColor = new Color(_shadowColor.R - 20, _shadowColor.G - 10, _shadowColor.B - 10);
            }
            else if (_isHovered)
            {
                // Quando hover, clarear o vermelho
                currentBaseColor = new Color(
                    Math.Min(255, _baseColor.R + 30),
                    Math.Min(255, _baseColor.G + 15),
                    Math.Min(255, _baseColor.B + 10)
                );
                currentHighlightColor = new Color(
                    Math.Min(255, _highlightColor.R + 25),
                    Math.Min(255, _highlightColor.G + 15),
                    Math.Min(255, _highlightColor.B + 10)
                );
            }

            // Criar textura de pixel se não existir
            if (_pixelTexture == null)
            {
                _pixelTexture = new Texture2D(batcher.GraphicsDevice, 1, 1);
                _pixelTexture.SetData(new[] { Color.White });
            }

            // Desenhar sombra do combobox
            batcher.Draw(_pixelTexture, new Rectangle(x + 3, y + 3, Width, Height), 
                new Vector3(currentShadowColor.R / 255f, currentShadowColor.G / 255f, currentShadowColor.B / 255f));

            // Desenhar o combobox principal com degradê
            DrawGradientButton(batcher, x, y, Width, Height, currentBaseColor, currentShadowColor);

            // Desenhar borda com efeito 3D
            DrawBorder(batcher, x, y, Width, Height, currentHighlightColor, currentShadowColor);

            // Desenhar textura/rugosidade
            DrawTextureEffect(batcher, x, y, Width, Height, currentBaseColor);

            // Desenhar o texto selecionado
            if (!string.IsNullOrEmpty(_selectedText) && _font != null)
            {
                var textSize = _font.MeasureString(_selectedText);
                var textX = x + 8; // Margem esquerda
                var textY = y + (Height - textSize.Y) / 2;

                // Offset do texto quando pressionado
                if (_isPressed || _isOpen)
                {
                    textX += 1;
                    textY += 1;
                }

                // Desenhar sombra do texto
                _font.DrawText(batcher, new StringSegment(_selectedText), new Vector2(textX + 1, textY + 1), _textShadowColor);

                // Desenhar o texto principal
                _font.DrawText(batcher, new StringSegment(_selectedText), new Vector2(textX, textY), _textColor);
            }

            // Desenhar seta do dropdown
            DrawDropdownArrow(batcher, x, y, Width, Height, _textColor);

            return base.Draw(batcher, x, y);
        }

        private void DrawGradientButton(UltimaBatcher2D batcher, int x, int y, int width, int height, Color baseColor, Color shadowColor)
        {
            // Criar degradê vertical
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
            // Desenhar bordas com efeito 3D
            // Borda superior (realce)
            batcher.Draw(_pixelTexture, new Rectangle(x, y, width, 2), 
                new Vector3(highlightColor.R / 255f, highlightColor.G / 255f, highlightColor.B / 255f));
            
            // Borda esquerda (realce)
            batcher.Draw(_pixelTexture, new Rectangle(x, y, 2, height), 
                new Vector3(highlightColor.R / 255f, highlightColor.G / 255f, highlightColor.B / 255f));

            // Borda inferior (sombra)
            batcher.Draw(_pixelTexture, new Rectangle(x, y + height - 2, width, 2), 
                new Vector3(shadowColor.R / 255f, shadowColor.G / 255f, shadowColor.B / 255f));
            
            // Borda direita (sombra)
            batcher.Draw(_pixelTexture, new Rectangle(x + width - 2, y, 2, height), 
                new Vector3(shadowColor.R / 255f, shadowColor.G / 255f, shadowColor.B / 255f));
        }

        private void DrawTextureEffect(UltimaBatcher2D batcher, int x, int y, int width, int height, Color baseColor)
        {
            // Efeito de textura vermelha mais sutil
            var textureColor = new Color(
                Math.Max(0, baseColor.R - 20),
                Math.Max(0, baseColor.G - 10),
                Math.Max(0, baseColor.B - 8)
            );

            // Desenhar padrão de textura orgânico
            Random random = new Random(12345);
            
            for (int i = 4; i < width - 20; i += 6) // Evitar área da seta
            {
                int lineX = x + i + random.Next(-2, 3);
                if (lineX >= x + 2 && lineX < x + width - 20)
                {
                    int lineHeight = height - 6 + random.Next(-2, 3);
                    int lineY = y + 3 + random.Next(-1, 2);
                    
                    var lineColor = new Color(
                        textureColor.R,
                        textureColor.G,
                        textureColor.B,
                        (byte)(180 + random.Next(-30, 31))
                    );
                    
                    batcher.Draw(_pixelTexture, new Rectangle(lineX, lineY, 1, lineHeight), 
                        new Vector3(lineColor.R / 255f, lineColor.G / 255f, lineColor.B / 255f));
                }
            }
        }

        private void DrawDropdownArrow(UltimaBatcher2D batcher, int x, int y, int width, int height, Color color)
        {
            // Desenhar seta do dropdown (triângulo)
            int arrowX = x + width - 15;
            int arrowY = y + height / 2;
            int arrowSize = 6;

            // Triângulo apontando para baixo
            for (int i = 0; i < arrowSize; i++)
            {
                int lineWidth = (i * 2) + 1;
                int lineX = arrowX - (lineWidth / 2);
                int lineY = arrowY - (arrowSize / 2) + i;

                batcher.Draw(_pixelTexture, new Rectangle(lineX, lineY, lineWidth, 1), 
                    new Vector3(color.R / 255f, color.G / 255f, color.B / 255f));
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
                    // Abrir dropdown
                    OpenDropdown();
                }
            }
        }

        private void OpenDropdown()
        {
            if (_items.Length == 0) return;

            _isOpen = true;

            // Criar gump do dropdown
            var dropdownGump = new GothicDropdownGump(
                ScreenCoordinateX,
                ScreenCoordinateY + Height,
                Width,
                Math.Min(200, _items.Length * 25 + 10),
                _items,
                _fontSize,
                this
            );

            UIManager.Add(dropdownGump);
        }

        public void CloseDropdown()
        {
            _isOpen = false;
        }

        private class GothicDropdownGump : Gump
        {
            private readonly GothicStyleCombobox _combobox;
            private readonly string[] _items;
            private readonly SpriteFontBase _font;
            private readonly int _fontSize;
            private Texture2D _pixelTexture;

            public GothicDropdownGump(int x, int y, int width, int height, string[] items, int fontSize, GothicStyleCombobox combobox) 
                : base(0, 0)
            {
                CanMove = false;
                AcceptMouseInput = true;
                X = x;
                Y = y;
                Width = width;
                Height = height;

                IsModal = true;
                LayerOrder = UILayer.Over;
                ModalClickOutsideAreaClosesThisControl = true;

                _combobox = combobox;
                _items = items;
                _fontSize = fontSize;
                _font = TrueTypeLoader.Instance.GetFont("Arial", fontSize);
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                if (_pixelTexture == null)
                {
                    _pixelTexture = new Texture2D(batcher.GraphicsDevice, 1, 1);
                    _pixelTexture.SetData(new[] { Color.White });
                }

                // Desenhar fundo do dropdown
                Color baseColor = new Color(0, 0, 0, (byte)(255)); // Preto com opacity 0.87
                Color shadowColor = new Color(0, 0, 0, (byte)(255));
                Color highlightColor = new Color(180, 50, 50);

                // Sombra (fundo escuro atrás)
                batcher.Draw(SolidColorTextureCache.GetTexture(shadowColor), new Rectangle(x + 2, y + 2, Width, Height), Vector3.Zero);

                // Fundo principal sólido
                batcher.Draw(SolidColorTextureCache.GetTexture(baseColor), new Rectangle(x, y, Width, Height), Vector3.Zero);

                // Bordas
                // Superior
                batcher.Draw(_pixelTexture, new Rectangle(x, y, Width, 2), 
                    new Vector3(highlightColor.R / 255f, highlightColor.G / 255f, highlightColor.B / 255f));
                // Esquerda
                batcher.Draw(_pixelTexture, new Rectangle(x, y, 2, Height), 
                    new Vector3(highlightColor.R / 255f, highlightColor.G / 255f, highlightColor.B / 255f));
                // Inferior
                batcher.Draw(_pixelTexture, new Rectangle(x, y + Height - 2, Width, 2), 
                    new Vector3(shadowColor.R / 255f, shadowColor.G / 255f, shadowColor.B / 255f));
                // Direita
                batcher.Draw(_pixelTexture, new Rectangle(x + Width - 2, y, 2, Height), 
                    new Vector3(shadowColor.R / 255f, shadowColor.G / 255f, shadowColor.B / 255f));

                // Desenhar itens
                for (int i = 0; i < _items.Length; i++)
                {
                    int itemY = y + 5 + (i * 25);
                    
                    if (itemY + 20 > y + Height) break; // Não desenhar se sair do dropdown

                    // Destacar item selecionado
                    if (i == _combobox.SelectedIndex)
                    {
                        batcher.Draw(_pixelTexture, new Rectangle(x + 2, itemY - 2, Width - 4, 20), 
                            new Vector3(highlightColor.R / 255f, highlightColor.G / 255f, highlightColor.B / 255f));
                    }

                    // Desenhar texto do item
                    if (_font != null)
                    {
                        _font.DrawText(batcher, new StringSegment(_items[i]), new Vector2(x + 8, itemY), Color.White);
                    }
                }

                return true;
            }

            protected override void OnMouseUp(int x, int y, ClassicUO.Input.MouseButtonType button)
            {
                if (button == ClassicUO.Input.MouseButtonType.Left)
                {
                    // Calcular qual item foi clicado
                    int itemIndex = (y - 5) / 25;
                    
                    if (itemIndex >= 0 && itemIndex < _items.Length)
                    {
                        _combobox.SelectedIndex = itemIndex;
                    }

                    _combobox.CloseDropdown();
                    Dispose();
                }
            }
        }
    }
}
