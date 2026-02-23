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
        private Texture2D _pixelTexture;
        private static readonly Random _textureRandom = new Random(12345);

        public GothicStyleCombobox(int x, int y, int width, int height, string[] items, int selectedIndex = -1, int fontSize = 16)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            _items = items ?? new string[0];
            _selectedIndex = selectedIndex;
            _fontSize = fontSize;

            _baseColor = Color.DarkRed;
            _highlightColor = new Color(180, 50, 50);
            _shadowColor = new Color(80, 15, 15);
            _textColor = Color.White;
            _textShadowColor = Color.Black;

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
            Color currentBaseColor = _shadowColor;
            Color currentHighlightColor = _shadowColor;
            Color currentShadowColor = _shadowColor;

            if (_isPressed || _isOpen)
            {
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

            if (_pixelTexture == null)
            {
                _pixelTexture = new Texture2D(batcher.GraphicsDevice, 1, 1);
                _pixelTexture.SetData(new[] { Color.Red });
            }

            batcher.Draw(_pixelTexture, new Rectangle(x + 3, y + 3, Width, Height), Color.DarkRed.ToVector3());
            DrawGradientBackground(batcher, x, y, Width, Height, currentBaseColor, currentShadowColor);
            DrawBorder(batcher, x, y, Width, Height, currentHighlightColor, currentShadowColor);
            DrawTextureEffect(batcher, x, y, Width, Height, currentBaseColor);

            if (!string.IsNullOrEmpty(_selectedText) && _font != null)
            {
                var textSize = _font.MeasureString(_selectedText);
                var textX = x + 8;
                var textY = y + (Height - textSize.Y) / 2;

                if (_isPressed || _isOpen)
                {
                    textX += 1;
                    textY += 1;
                }

                _font.DrawText(batcher, new StringSegment(_selectedText), new Vector2(textX + 2, textY + 2), _textShadowColor);
                _font.DrawText(batcher, new StringSegment(_selectedText), new Vector2(textX + 1, textY + 1), new Color(_textShadowColor.R + 20, _textShadowColor.G + 10, _textShadowColor.B + 10));
                _font.DrawText(batcher, new StringSegment(_selectedText), new Vector2(textX, textY), _textColor);
            }

            DrawDropdownArrow(batcher, x, y, Width, Height);

            return base.Draw(batcher, x, y);
        }

        private static readonly Vector3 SolidHue = ShaderHueTranslator.GetHueVector(0, false, 1f);

        private void DrawGradientBackground(UltimaBatcher2D batcher, int x, int y, int width, int height, Color baseColor, Color shadowColor)
        {
            for (int i = 0; i < height; i++)
            {
                float ratio = (float)i / height;
                int cr = (int)(baseColor.R + (shadowColor.R - baseColor.R) * ratio);
                int cg = (int)(baseColor.G + (shadowColor.G - baseColor.G) * ratio);
                int cb = (int)(baseColor.B + (shadowColor.B - baseColor.B) * ratio);
                batcher.Draw(_pixelTexture, new Rectangle(x, y + i, width, 1), new Vector3(cr / 255f, cg / 255f, cb / 255f));
            }
        }

        private const int BORDER_RADIUS = 6;

        private void DrawBorder(UltimaBatcher2D batcher, int x, int y, int width, int height, Color highlightColor, Color shadowColor)
        {
            int r = BORDER_RADIUS;
            if (width < r * 2 || height < r * 2)
                r = 0;
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
            var textureColor = new Color(
                Math.Max(0, baseColor.R - 20),
                Math.Max(0, baseColor.G - 10),
                Math.Max(0, baseColor.B - 8)
            );
            for (int i = 4; i < width - 4; i += 6)
            {
                int lineX = x + i + _textureRandom.Next(-2, 3);
                if (lineX >= x + 2 && lineX < x + width - 2)
                {
                    int lineHeight = height - 6 + _textureRandom.Next(-2, 3);
                    int lineY = y + 3 + _textureRandom.Next(-1, 2);
                    batcher.Draw(_pixelTexture, new Rectangle(lineX, lineY, 1, lineHeight),
                        new Vector3(textureColor.R / 255f, textureColor.G / 255f, textureColor.B / 255f));
                }
            }
        }

        private void DrawDropdownArrow(UltimaBatcher2D batcher, int x, int y, int width, int height)
        {
            int arrowX = x + width - 15;
            int arrowY = y + height / 2;
            int arrowSize = 6;
            for (int i = 0; i < arrowSize; i++)
            {
                int lineWidth = (i * 2) + 1;
                int lineX = arrowX - (lineWidth / 2);
                int lineY = arrowY - (arrowSize / 2) + i;
                batcher.Draw(SolidColorTextureCache.GetTexture(_textColor), new Rectangle(lineX, lineY, lineWidth, 1), null, SolidHue);
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
            private static readonly Random _dropdownTextureRandom = new Random(12345);
            private static readonly Vector3 OpaqueHue = ShaderHueTranslator.GetHueVector(0, false, 1f);
            private const int ItemHeight = 25;
            private const int ScrollBarWidth = 12;
            private const int Padding = 5;
            private const int BORDER_RADIUS = 6;

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
                Alpha = 1f;

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
                Color baseColor = _combobox.BaseColor;
                Color shadowColor = _combobox.ShadowColor;
                Color highlightColor = _combobox.HighlightColor;

                batcher.Draw(SolidColorTextureCache.GetTexture(baseColor), new Rectangle(x, y, Width, Height), OpaqueHue);
                DrawBorder(batcher, x, y, Width, Height, highlightColor, shadowColor);
                DrawTextureEffect(batcher, x, y, Width, Height, baseColor);

                int contentWidth = Width - (HasScroll ? ScrollBarWidth + 4 : 4);

                if (batcher.ClipBegin(x + 2, y + Padding, contentWidth + (HasScroll ? ScrollBarWidth + 4 : 0), Height - Padding * 2))
                {
                    for (int i = 0; i < _items.Length; i++)
                    {
                        int itemY = y + Padding + i * ItemHeight - _scrollOffset;
                        if (itemY + ItemHeight <= y + Padding || itemY >= y + Height - Padding) continue;

                        bool isSelected = i == _combobox.SelectedIndex;
                        if (isSelected)
                            batcher.Draw(SolidColorTextureCache.GetTexture(highlightColor), new Rectangle(x + 2, itemY, contentWidth, ItemHeight - 2), OpaqueHue);
                        if (_font != null && !string.IsNullOrEmpty(_items[i]))
                            _font.DrawText(batcher, new StringSegment(_items[i]), new Vector2(x + 8, itemY + 4), Color.White);
                    }
                    batcher.ClipEnd();
                }

                if (HasScroll)
                {
                    int barX = x + Width - ScrollBarWidth - 2;
                    batcher.Draw(SolidColorTextureCache.GetTexture(shadowColor), new Rectangle(barX, y + Padding, ScrollBarWidth, Height - Padding * 2), OpaqueHue);
                    int trackHeight = Height - Padding * 2;
                    int thumbHeight = Math.Max(20, trackHeight * trackHeight / (_items.Length * ItemHeight + Padding * 2));
                    int maxThumbY = trackHeight - thumbHeight;
                    int thumbY = maxThumbY > 0 ? y + Padding + (maxThumbY * _scrollOffset / MaxScroll) : y + Padding;
                    batcher.Draw(SolidColorTextureCache.GetTexture(highlightColor), new Rectangle(barX + 2, thumbY, ScrollBarWidth - 4, thumbHeight), OpaqueHue);
                }

                return true;
            }

            private void DrawBorder(UltimaBatcher2D batcher, int x, int y, int width, int height, Color highlightColor, Color shadowColor)
            {
                int r = BORDER_RADIUS;
                if (width < r * 2 || height < r * 2) r = 0;
                Texture2D highlightTex = SolidColorTextureCache.GetTexture(highlightColor);
                Texture2D shadowTex = SolidColorTextureCache.GetTexture(shadowColor);
                if (r > 0)
                {
                    batcher.Draw(highlightTex, new Rectangle(x + r, y, width - r * 2, 2), OpaqueHue);
                    batcher.Draw(shadowTex, new Rectangle(x + r, y + height - 2, width - r * 2, 2), OpaqueHue);
                    batcher.Draw(highlightTex, new Rectangle(x, y + r, 2, height - r * 2), OpaqueHue);
                    batcher.Draw(shadowTex, new Rectangle(x + width - 2, y + r, 2, height - r * 2), OpaqueHue);
                    batcher.Draw(highlightTex, new Rectangle(x, y, r, r), OpaqueHue);
                    batcher.Draw(highlightTex, new Rectangle(x + width - r, y, r, r), OpaqueHue);
                    batcher.Draw(shadowTex, new Rectangle(x, y + height - r, r, r), OpaqueHue);
                    batcher.Draw(shadowTex, new Rectangle(x + width - r, y + height - r, r, r), OpaqueHue);
                }
                else
                {
                    batcher.Draw(highlightTex, new Rectangle(x, y, width, 2), OpaqueHue);
                    batcher.Draw(shadowTex, new Rectangle(x, y + height - 2, width, 2), OpaqueHue);
                    batcher.Draw(highlightTex, new Rectangle(x, y, 2, height), OpaqueHue);
                    batcher.Draw(shadowTex, new Rectangle(x + width - 2, y, 2, height), OpaqueHue);
                }
            }

            private void DrawTextureEffect(UltimaBatcher2D batcher, int x, int y, int width, int height, Color baseColor)
            {
                Color textureColor = new Color(Math.Max(0, baseColor.R - 20), Math.Max(0, baseColor.G - 10), Math.Max(0, baseColor.B - 8));
                Texture2D tex = SolidColorTextureCache.GetTexture(textureColor);
                for (int i = 4; i < width - 4; i += 6)
                {
                    int lineX = x + i + _dropdownTextureRandom.Next(-2, 3);
                    if (lineX >= x + 2 && lineX < x + width - 2)
                    {
                        int lineHeight = height - 6 + _dropdownTextureRandom.Next(-2, 3);
                        int lineY = y + 3 + _dropdownTextureRandom.Next(-1, 2);
                        batcher.Draw(tex, new Rectangle(lineX, lineY, 1, lineHeight), OpaqueHue);
                    }
                }
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

        }
    }
}
