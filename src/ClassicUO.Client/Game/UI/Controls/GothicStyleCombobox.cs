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
        private int _fontSize;
        private static readonly Vector3 SolidHue = ShaderHueTranslator.GetHueVector(0, false, 1f);

        public GothicStyleCombobox(int x, int y, int width, int height, string[] items, int selectedIndex = -1, int fontSize = 16)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            _items = items ?? new string[0];
            _selectedIndex = selectedIndex;
            _fontSize = fontSize;

            // Cores do tema gótico/medieval - Background preto com texto branco
            _baseColor = Color.Black;                      // Background preto
            _highlightColor = new Color(100, 100, 100);    // Realce cinza claro para bordas
            _shadowColor = new Color(50, 50, 50);          // Sombra cinza escuro
            _textColor = Color.White;                      // Texto branco para contraste
            _textShadowColor = Color.Gray;                 // Sombra cinza do texto

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
            Color currentBaseColor = _baseColor;
            Color currentHighlightColor = _highlightColor;
            Color currentShadowColor = _shadowColor;

            if (_isPressed || _isOpen)
            {
                currentHighlightColor = new Color(
                    Math.Min(255, _highlightColor.R + 20),
                    Math.Min(255, _highlightColor.G + 20),
                    Math.Min(255, _highlightColor.B + 20)
                );
            }
            else if (_isHovered)
            {
                currentHighlightColor = new Color(
                    Math.Min(255, _highlightColor.R + 10),
                    Math.Min(255, _highlightColor.G + 10),
                    Math.Min(255, _highlightColor.B + 10)
                );
            }

            // Desenhar sombra do combobox
            FillRectangle(batcher, new Rectangle(x + 3, y + 3, Width, Height), currentShadowColor);

            DrawSolidBackground(batcher, x, y, Width, Height, currentBaseColor);

            // Desenhar borda com efeito 3D
            DrawBorder(batcher, x, y, Width, Height, currentHighlightColor, currentShadowColor);

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

        private void DrawSolidBackground(UltimaBatcher2D batcher, int x, int y, int width, int height, Color color)
        {
            FillRectangle(batcher, new Rectangle(x, y, width, height), color);
        }

        private void DrawBorder(UltimaBatcher2D batcher, int x, int y, int width, int height, Color highlightColor, Color shadowColor)
        {
            // Desenhar bordas com efeito 3D
            // Borda superior (realce)
            FillRectangle(batcher, new Rectangle(x, y, width, 2), highlightColor);
            
            // Borda esquerda (realce)
            FillRectangle(batcher, new Rectangle(x, y, 2, height), highlightColor);

            // Borda inferior (sombra)
            FillRectangle(batcher, new Rectangle(x, y + height - 2, width, 2), shadowColor);
            
            // Borda direita (sombra)
            FillRectangle(batcher, new Rectangle(x + width - 2, y, 2, height), shadowColor);
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

                FillRectangle(batcher, new Rectangle(lineX, lineY, lineWidth, 1), color);
            }
        }

        private static void FillRectangle(UltimaBatcher2D batcher, Rectangle rectangle, Color color)
        {
            batcher.Draw(SolidColorTextureCache.GetTexture(color), rectangle, null, SolidHue);
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
            private static readonly Vector3 SolidHue = ShaderHueTranslator.GetHueVector(0, false, 1f);

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
                // Desenhar fundo do dropdown
                Color baseColor = new Color(0, 0, 0, (byte)(255)); // Preto sólido
                Color shadowColor = new Color(50, 50, 50); // Cinza escuro para sombra
                Color highlightColor = new Color(100, 100, 100); // Cinza claro para bordas

                // Sombra (fundo escuro atrás)
                FillRect(batcher, new Rectangle(x + 2, y + 2, Width, Height), shadowColor);

                // Fundo principal sólido
                FillRect(batcher, new Rectangle(x, y, Width, Height), baseColor);

                // Bordas
                // Superior
                FillRect(batcher, new Rectangle(x, y, Width, 2), highlightColor);
                // Esquerda
                FillRect(batcher, new Rectangle(x, y, 2, Height), highlightColor);
                // Inferior
                FillRect(batcher, new Rectangle(x, y + Height - 2, Width, 2), shadowColor);
                // Direita
                FillRect(batcher, new Rectangle(x + Width - 2, y, 2, Height), shadowColor);

                // Desenhar itens
                for (int i = 0; i < _items.Length; i++)
                {
                    int itemY = y + 5 + (i * 25);
                    
                    if (itemY + 20 > y + Height) break; // Não desenhar se sair do dropdown

                    // Destacar item selecionado
                    if (i == _combobox.SelectedIndex)
                    {
                        FillRect(batcher, new Rectangle(x + 2, itemY - 2, Width - 4, 20), highlightColor);
                    }

                    // Desenhar texto do item
                    if (_font != null)
                    {
                        _font.DrawText(batcher, new StringSegment(_items[i]), new Vector2(x + 8, itemY), Color.White);
                    }
                }

                return true;
            }

            private static void FillRect(UltimaBatcher2D batcher, Rectangle rectangle, Color color)
            {
                batcher.Draw(SolidColorTextureCache.GetTexture(color), rectangle, null, SolidHue);
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
