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

            var dropdownGump = new GothicDropdownGump(
                ScreenCoordinateX,
                ScreenCoordinateY + Height,
                Width,
                200,
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
            private int _scrollOffset;
            private static readonly Vector3 SolidHue = ShaderHueTranslator.GetHueVector(0, false, 1f);
            private const int ItemHeight = 25;
            private const int ScrollBarWidth = 12;
            private const int Padding = 5;

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

            private int MaxScroll => Math.Max(0, _items.Length * ItemHeight + Padding * 2 - Height);
            private bool HasScroll => _items.Length * ItemHeight + Padding * 2 > Height;

            protected override void OnMouseWheel(MouseEventType delta)
            {
                if (!HasScroll) return;
                if (delta == MouseEventType.WheelScrollUp)
                    _scrollOffset = Math.Max(0, _scrollOffset - ItemHeight);
                else
                    _scrollOffset = Math.Min(MaxScroll, _scrollOffset + ItemHeight);
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                Color baseColor = Color.Black;
                Color shadowColor = new Color(50, 50, 50);
                Color highlightColor = new Color(100, 100, 100);

                FillRect(batcher, new Rectangle(x + 2, y + 2, Width, Height), shadowColor);
                FillRect(batcher, new Rectangle(x, y, Width, Height), baseColor);
                FillRect(batcher, new Rectangle(x, y, Width, 2), highlightColor);
                FillRect(batcher, new Rectangle(x, y, 2, Height), highlightColor);
                FillRect(batcher, new Rectangle(x, y + Height - 2, Width, 2), shadowColor);
                FillRect(batcher, new Rectangle(x + Width - 2, y, 2, Height), shadowColor);

                int contentWidth = Width - (HasScroll ? ScrollBarWidth + 4 : 4);
                int contentHeight = Height - Padding * 2;

                if (batcher.ClipBegin(x + 2, y + Padding, contentWidth + (HasScroll ? ScrollBarWidth + 4 : 0), contentHeight))
                {
                for (int i = 0; i < _items.Length; i++)
                {
                    int itemY = y + Padding + i * ItemHeight - _scrollOffset;
                    if (itemY + ItemHeight <= y + Padding || itemY >= y + Height - Padding) continue;

                    bool isSelected = i == _combobox.SelectedIndex;
                    if (isSelected)
                        FillRect(batcher, new Rectangle(x + 2, itemY, contentWidth, ItemHeight - 2), highlightColor);
                    if (_font != null && !string.IsNullOrEmpty(_items[i]))
                        _font.DrawText(batcher, new StringSegment(_items[i]), new Vector2(x + 8, itemY + 4), Color.White);
                }
                batcher.ClipEnd();
                }

                if (HasScroll)
                {
                    int barX = x + Width - ScrollBarWidth - 2;
                    FillRect(batcher, new Rectangle(barX, y + Padding, ScrollBarWidth, Height - Padding * 2), new Color(60, 60, 60));
                    int trackHeight = Height - Padding * 2;
                    int thumbHeight = Math.Max(20, trackHeight * trackHeight / (_items.Length * ItemHeight + Padding * 2));
                    int maxThumbY = trackHeight - thumbHeight;
                    int thumbY = maxThumbY > 0 ? y + Padding + (maxThumbY * _scrollOffset / MaxScroll) : y + Padding;
                    FillRect(batcher, new Rectangle(barX + 2, thumbY, ScrollBarWidth - 4, thumbHeight), new Color(120, 120, 120));
                }

                return true;
            }

            protected override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                if (button != MouseButtonType.Left) return;

                int contentWidth = Width - (HasScroll ? ScrollBarWidth + 4 : 4);
                if (x >= contentWidth + 4)
                {
                    if (HasScroll && x >= Width - ScrollBarWidth - 2)
                    {
                        int trackHeight = Height - Padding * 2;
                        int relY = y - Padding;
                        int thumbHeight = Math.Max(20, trackHeight * trackHeight / (_items.Length * ItemHeight + Padding * 2));
                        int maxThumbY = trackHeight - thumbHeight;
                        if (maxThumbY > 0)
                        {
                            int thumbCenterY = Math.Max(thumbHeight / 2, Math.Min(relY, trackHeight - thumbHeight / 2));
                            _scrollOffset = MaxScroll * (thumbCenterY - thumbHeight / 2) / maxThumbY;
                        }
                    }
                    return;
                }

                int itemIndex = (y - Padding + _scrollOffset) / ItemHeight;
                if (itemIndex >= 0 && itemIndex < _items.Length)
                {
                    _combobox.SelectedIndex = itemIndex;
                    _combobox.CloseDropdown();
                    Dispose();
                }
            }

            private static void FillRect(UltimaBatcher2D batcher, Rectangle rectangle, Color color)
            {
                batcher.Draw(SolidColorTextureCache.GetTexture(color), rectangle, null, SolidHue);
            }
        }
    }
}
