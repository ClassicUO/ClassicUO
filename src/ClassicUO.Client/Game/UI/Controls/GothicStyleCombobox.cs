using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Renderer;
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
        private RenderedText _renderedText;
        private Color _baseColor;
        private Color _highlightColor;
        private Color _shadowColor;
        private Color _textColor;
        private Color _textShadowColor;
        private Texture2D _pixelTexture;

        public GothicStyleCombobox(int x, int y, int width, int height, string[] items, int selectedIndex = -1)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            _items = items ?? new string[0];
            _selectedIndex = selectedIndex;

            _baseColor = Color.DarkRed;
            _highlightColor = new Color(180, 50, 50);
            _shadowColor = new Color(80, 15, 15);
            _textColor = Color.White;
            _textShadowColor = Color.Black;

            if (_selectedIndex >= 0 && _selectedIndex < _items.Length)
            {
                _selectedText = _items[_selectedIndex];
            }
            else
            {
                _selectedText = _items.Length > 0 ? _items[0] : "";
                _selectedIndex = _items.Length > 0 ? 0 : -1;
            }
            _renderedText = RenderedText.Create(_selectedText ?? string.Empty, 0x0481, 1, true, FontStyle.BlackBorder);

            if (Client.Game?.GraphicsDevice != null)
            {
                _pixelTexture = new Texture2D(Client.Game.GraphicsDevice, 1, 1);
                _pixelTexture.SetData(new[] { Color.Red });
            }

            AcceptMouseInput = true;
            Alpha = 1f;
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
                    _renderedText.Text = _selectedText ?? string.Empty;
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
                    _renderedText.Text = _selectedText;
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

            if (_pixelTexture == null || _pixelTexture.IsDisposed)
            {
                _pixelTexture?.Dispose();
                _pixelTexture = new Texture2D(batcher.GraphicsDevice, 1, 1);
                _pixelTexture.SetData(new[] { Color.White });
            }

            Vector3 opaqueHue = ShaderHueTranslator.GetHueVector(0, false, 1f);
            Color solidBg = new Color(currentBaseColor.R, currentBaseColor.G, currentBaseColor.B, 255);
            batcher.Draw(SolidColorTextureCache.GetTexture(solidBg), new Rectangle(x, y, Width, Height), null, opaqueHue);
            DrawGradientBackground(batcher, x, y, Width, Height, currentBaseColor, currentShadowColor, opaqueHue);
            DrawBorder(batcher, x, y, Width, Height, currentHighlightColor, currentShadowColor, opaqueHue);
            DrawTextureEffect(batcher, x, y, Width, Height, currentBaseColor, opaqueHue);

            if (!string.IsNullOrEmpty(_selectedText))
            {
                _renderedText.Text = _selectedText;
                var textX = x + 8;
                var textY = y + (Height - _renderedText.Height) / 2;
                if (_isPressed || _isOpen)
                {
                    textX += 1;
                    textY += 1;
                }
                _renderedText.Draw(batcher, textX, textY);
            }

            DrawDropdownArrow(batcher, x, y, Width, Height);

            return base.Draw(batcher, x, y);
        }

        private static readonly Vector3 SolidHue = ShaderHueTranslator.GetHueVector(0, false, 1f);

        private const int GRADIENT_STRIP_HEIGHT = 4;

        private void DrawGradientBackground(UltimaBatcher2D batcher, int x, int y, int width, int height, Color baseColor, Color shadowColor, Vector3 hue)
        {
            for (int i = 0; i < height; i += GRADIENT_STRIP_HEIGHT)
            {
                int stripHeight = Math.Min(GRADIENT_STRIP_HEIGHT, height - i);
                float ratio = (float)(i + stripHeight / 2f) / height;
                int cr = (int)(baseColor.R + (shadowColor.R - baseColor.R) * ratio);
                int cg = (int)(baseColor.G + (shadowColor.G - baseColor.G) * ratio);
                int cb = (int)(baseColor.B + (shadowColor.B - baseColor.B) * ratio);
                var stripColor = new Color((byte)cr, (byte)cg, (byte)cb, 255);
                batcher.Draw(SolidColorTextureCache.GetTexture(stripColor), new Rectangle(x, y + i, width, stripHeight), null, hue);
            }
        }

        private const int BORDER_RADIUS = 6;

        private void DrawBorder(UltimaBatcher2D batcher, int x, int y, int width, int height, Color highlightColor, Color shadowColor, Vector3 hue)
        {
            int r = BORDER_RADIUS;
            if (width < r * 2 || height < r * 2)
                r = 0;
            var highlightTex = SolidColorTextureCache.GetTexture(highlightColor);
            var shadowTex = SolidColorTextureCache.GetTexture(shadowColor);
            if (r > 0)
            {
                batcher.Draw(highlightTex, new Rectangle(x + r, y, width - r * 2, 2), null, hue);
                batcher.Draw(shadowTex, new Rectangle(x + r, y + height - 2, width - r * 2, 2), null, hue);
                batcher.Draw(highlightTex, new Rectangle(x, y + r, 2, height - r * 2), null, hue);
                batcher.Draw(shadowTex, new Rectangle(x + width - 2, y + r, 2, height - r * 2), null, hue);
                batcher.Draw(highlightTex, new Rectangle(x, y, r, r), null, hue);
                batcher.Draw(highlightTex, new Rectangle(x + width - r, y, r, r), null, hue);
                batcher.Draw(shadowTex, new Rectangle(x, y + height - r, r, r), null, hue);
                batcher.Draw(shadowTex, new Rectangle(x + width - r, y + height - r, r, r), null, hue);
            }
            else
            {
                batcher.Draw(highlightTex, new Rectangle(x, y, width, 2), null, hue);
                batcher.Draw(shadowTex, new Rectangle(x, y + height - 2, width, 2), null, hue);
                batcher.Draw(highlightTex, new Rectangle(x, y, 2, height), null, hue);
                batcher.Draw(shadowTex, new Rectangle(x + width - 2, y, 2, height), null, hue);
            }
        }

        private void DrawTextureEffect(UltimaBatcher2D batcher, int x, int y, int width, int height, Color baseColor, Vector3 hue)
        {
            var textureColor = new Color(
                (byte)Math.Max(0, baseColor.R - 20),
                (byte)Math.Max(0, baseColor.G - 10),
                (byte)Math.Max(0, baseColor.B - 8),
                255
            );
            var textureTex = SolidColorTextureCache.GetTexture(textureColor);
            for (int i = 4; i < width - 4; i += 6)
            {
                int offsetX = ((i * 7 + 11) % 5) - 2;
                int lineX = x + i + offsetX;
                if (lineX >= x + 2 && lineX < x + width - 2)
                {
                    int offsetH = ((i * 3) % 5) - 2;
                    int lineHeight = Math.Max(1, height - 6 + offsetH);
                    int offsetY = ((i * 2) % 3) - 1;
                    int lineY = y + 3 + offsetY;
                    batcher.Draw(textureTex, new Rectangle(lineX, lineY, 1, lineHeight), null, hue);
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

            const int maxHeight = 200;
            int comboY = ScreenCoordinateY + Height;
            if (comboY < 0)
                comboY = 0;
            else if (comboY + maxHeight > Client.Game.Window.ClientBounds.Height)
                comboY = Client.Game.Window.ClientBounds.Height - maxHeight;

            var dropdownGump = new GothicDropdownGump(
                ScreenCoordinateX,
                comboY,
                Width,
                maxHeight,
                _items,
                this
            );

            UIManager.Add(dropdownGump);
        }

        public void CloseDropdown()
        {
            _isOpen = false;
        }

        public override void Dispose()
        {
            _renderedText?.Destroy();
            _pixelTexture?.Dispose();
            _pixelTexture = null;
            base.Dispose();
        }

        private class GothicDropdownGump : Gump
        {
            private const int ELEMENT_HEIGHT = 25;
            private const int BORDER_RADIUS = 6;
            private const int GRADIENT_STRIP_HEIGHT = 4;
            private readonly GothicStyleCombobox _combobox;
            private Texture2D _pixelTexture;

            public GothicDropdownGump(int x, int y, int width, int height, string[] items, GothicStyleCombobox combobox)
                : base(0, 0)
            {
                CanMove = false;
                AcceptMouseInput = true;
                X = x;
                Y = y;
                IsModal = true;
                LayerOrder = UILayer.Over;
                ModalClickOutsideAreaClosesThisControl = true;
                _combobox = combobox;

                if (Client.Game?.GraphicsDevice != null)
                {
                    _pixelTexture = new Texture2D(Client.Game.GraphicsDevice, 1, 1);
                    _pixelTexture.SetData(new[] { Color.Red });
                }

                var labels = new HoveredLabel[items.Length];
                for (int i = 0; i < items.Length; i++)
                {
                    string item = items[i] ?? string.Empty;
                    var label = new HoveredLabel(item, true, 0x0481, 0x0481, 0x0481, font: 1)
                    {
                        X = 2,
                        Y = i * ELEMENT_HEIGHT,
                        DrawBackgroundCurrentIndex = true,
                        IsVisible = item.Length != 0,
                        Tag = i
                    };
                    label.MouseUp += (s, e) =>
                    {
                        if (e.Button == MouseButtonType.Left)
                        {
                            _combobox.SelectedIndex = (int)((Label)s).Tag;
                            _combobox.CloseDropdown();
                            Dispose();
                        }
                    };
                    labels[i] = label;
                }

                int totalHeight = Math.Min(height, items.Length * ELEMENT_HEIGHT);
                int maxWidth = Math.Max(width, labels.Max(o => o.X + o.Width));

                var area = new ScrollArea(0, 0, maxWidth + 15, totalHeight, true);
                foreach (var label in labels)
                {
                    label.Width = maxWidth;
                    area.Add(label);
                }
                Add(area);

                Width = maxWidth;
                Height = totalHeight;

                for (int i = 0; i < labels.Length; i++)
                {
                    labels[i].IsSelected = i == _combobox.SelectedIndex;
                }
            }

            private void EnsurePixelTexture(GraphicsDevice device)
            {
                if (_pixelTexture == null || _pixelTexture.IsDisposed)
                {
                    _pixelTexture?.Dispose();
                    _pixelTexture = new Texture2D(device, 1, 1);
                    _pixelTexture.SetData(new[] { Color.Red });
                }
            }

            private void DrawGradientBackground(UltimaBatcher2D batcher, int x, int y, int w, int h)
            {
                Vector3 opaqueHue = ShaderHueTranslator.GetHueVector(0, false, 1f);
                Color baseColor = new Color(_combobox.BaseColor.R, _combobox.BaseColor.G, _combobox.BaseColor.B, 255);
                batcher.Draw(SolidColorTextureCache.GetTexture(baseColor), new Rectangle(x, y, w, h), null, opaqueHue);
                Color shadowColor = _combobox.ShadowColor;
                for (int i = 0; i < h; i += GRADIENT_STRIP_HEIGHT)
                {
                    int stripHeight = Math.Min(GRADIENT_STRIP_HEIGHT, h - i);
                    float ratio = (float)(i + stripHeight / 2f) / h;
                    int cr = (int)(baseColor.R + (shadowColor.R - baseColor.R) * ratio);
                    int cg = (int)(baseColor.G + (shadowColor.G - baseColor.G) * ratio);
                    int cb = (int)(baseColor.B + (shadowColor.B - baseColor.B) * ratio);
                    var stripColor = new Color((byte)cr, (byte)cg, (byte)cb, 255);
                    batcher.Draw(SolidColorTextureCache.GetTexture(stripColor), new Rectangle(x, y + i, w, stripHeight), null, opaqueHue);
                }
            }

            private static readonly Vector3 SolidHue = ShaderHueTranslator.GetHueVector(0, false, 1f);

            private void DrawBorder(UltimaBatcher2D batcher, int x, int y, int w, int h)
            {
                Color highlightColor = _combobox.HighlightColor;
                Color shadowColor = _combobox.ShadowColor;
                var highlightTex = SolidColorTextureCache.GetTexture(highlightColor);
                var shadowTex = SolidColorTextureCache.GetTexture(shadowColor);
                int r = BORDER_RADIUS;
                if (w < r * 2 || h < r * 2) r = 0;
                if (r > 0)
                {
                    batcher.Draw(shadowTex, new Rectangle(x + r, y, w - r * 2, 2), null, SolidHue);
                    batcher.Draw(shadowTex, new Rectangle(x + r, y + h - 2, w - r * 2, 2), null, SolidHue);
                    batcher.Draw(shadowTex, new Rectangle(x, y + r, 2, h - r * 2), null, SolidHue);
                    batcher.Draw(shadowTex, new Rectangle(x + w - 2, y + r, 2, h - r * 2), null, SolidHue);
                    batcher.Draw(shadowTex, new Rectangle(x, y, r, r), null, SolidHue);
                    batcher.Draw(shadowTex, new Rectangle(x + w - r, y, r, r), null, SolidHue);
                    batcher.Draw(shadowTex, new Rectangle(x, y + h - r, r, r), null, SolidHue);
                    batcher.Draw(shadowTex, new Rectangle(x + w - r, y + h - r, r, r), null, SolidHue);
                }
                else
                {
                    batcher.Draw(shadowTex, new Rectangle(x, y, w, 2), null, SolidHue);
                    batcher.Draw(shadowTex, new Rectangle(x, y + h - 2, w, 2), null, SolidHue);
                    batcher.Draw(shadowTex, new Rectangle(x, y, 2, h), null, SolidHue);
                    batcher.Draw(shadowTex, new Rectangle(x + w - 2, y, 2, h), null, SolidHue);
                }
            }

            private void DrawTextureEffect(UltimaBatcher2D batcher, int x, int y, int w, int h)
            {
                Vector3 opaqueHue = ShaderHueTranslator.GetHueVector(0, false, 1f);
                Color baseColor = _combobox.BaseColor;
                Color textureColor = new Color((byte)Math.Max(0, baseColor.R - 20), (byte)Math.Max(0, baseColor.G - 10), (byte)Math.Max(0, baseColor.B - 8), 255);
                var textureTex = SolidColorTextureCache.GetTexture(textureColor);
                for (int i = 4; i < w - 4; i += 6)
                {
                    int offsetX = ((i * 7 + 11) % 5) - 2;
                    int lineX = x + i + offsetX;
                    if (lineX >= x + 2 && lineX < x + w - 2)
                    {
                        int offsetH = ((i * 3) % 5) - 2;
                        int lineHeight = Math.Max(1, h - 6 + offsetH);
                        int offsetY = ((i * 2) % 3) - 1;
                        int lineY = y + 3 + offsetY;
                        batcher.Draw(textureTex, new Rectangle(lineX, lineY, 1, lineHeight), null, opaqueHue);
                    }
                }
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                EnsurePixelTexture(batcher.GraphicsDevice);
                if (_pixelTexture == null || _pixelTexture.IsDisposed)
                    return true;

                DrawGradientBackground(batcher, x, y, Width, Height);
                DrawTextureEffect(batcher, x, y, Width, Height);

                if (batcher.ClipBegin(x, y, Width, Height))
                {
                    base.Draw(batcher, x, y);
                    batcher.ClipEnd();
                }

                DrawBorder(batcher, x, y, Width, Height);
                return true;
            }

            public override void Dispose()
            {
                _pixelTexture?.Dispose();
                _pixelTexture = null;
                base.Dispose();
            }
        }
    }
}
