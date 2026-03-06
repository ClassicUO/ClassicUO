using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace ClassicUO.Game.UI.Controls
{
    public class GothicStyleSliderBar : Control
    {
        private bool _clicked;
        private int _sliderX;
        private int _value = -1;
        private RenderedText _renderedText;
        private Color _baseColor;
        private Color _highlightColor;
        private Color _shadowColor;
        private Color _textColor;
        private Color _sliderColor;
        private Texture2D _pixelTexture;
        private bool _showText;
        private bool _isUpdatingPairedValues = false;

        public GothicStyleSliderBar(
            int x,
            int y,
            int width,
            int min,
            int max,
            int value,
            bool showText = true
        )
        {
            X = x;
            Y = y;
            Width = width;
            Height = 20; // Altura fixa para o slider
            MinValue = min;
            MaxValue = max;
            _showText = showText;

            // Cores do tema gótico/medieval
            _baseColor = Color.DarkRed;                    // Background vermelho escuro
            _highlightColor = new Color(180, 50, 50);      // Realce mais claro para bordas
            _shadowColor = new Color(80, 15, 15);          // Sombra mais escura
            _textColor = Color.White;
            _sliderColor = new Color(220, 100, 100);

            _renderedText = RenderedText.Create(Value.ToString(), 0x0481, 1, true, FontStyle.BlackBorder);

            AcceptMouseInput = true;
            Value = value;
        }

        public int MinValue { get; set; }
        public int MaxValue { get; set; }
        public int BarWidth => Width;

        public float Percents { get; private set; }

        public int Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    int oldValue = _value;
                    _value = value;

                    if (_value < MinValue)
                    {
                        _value = MinValue;
                    }
                    else if (_value > MaxValue)
                    {
                        _value = MaxValue;
                    }

                    if (_value != oldValue)
                    {
                        if (!_isUpdatingPairedValues)
                        {
                            ModifyPairedValues(_value - oldValue);
                        }
                        CalculateOffset();
                        ValueChanged?.Invoke(this, EventArgs.Empty);
                    }
                }
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

        public Color SliderColor
        {
            get => _sliderColor;
            set => _sliderColor = value;
        }

        public event EventHandler ValueChanged;

        private readonly List<GothicStyleSliderBar> _pairedSliders = new List<GothicStyleSliderBar>();

        public void AddParisSlider(GothicStyleSliderBar slider)
        {
            _pairedSliders.Add(slider);
        }

        public override void Update()
        {
            base.Update();

            if (_clicked)
            {
                int x = Mouse.Position.X - X - ParentX;
                CalculateNew(x);
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            // Criar textura de pixel se não existir
            if (_pixelTexture == null)
            {
                _pixelTexture = new Texture2D(batcher.GraphicsDevice, 1, 1);
                _pixelTexture.SetData(new[] { Color.White });
            }

            // Desenhar sombra do slider
            batcher.Draw(_pixelTexture, new Rectangle(x + 2, y + 2, Width, Height), 
                new Vector3(_shadowColor.R / 255f, _shadowColor.G / 255f, _shadowColor.B / 255f));

            // Desenhar fundo do slider com degradê
            DrawGradientBar(batcher, x, y, Width, Height, _baseColor, _shadowColor);

            // Desenhar bordas
            DrawBorder(batcher, x, y, Width, Height, _highlightColor, _shadowColor);

            // Desenhar textura/rugosidade
            DrawTextureEffect(batcher, x, y, Width, Height, _baseColor);

            // Desenhar o slider (indicador)
            DrawSlider(batcher, x, y, _sliderX, Height, _sliderColor);

            if (_showText)
            {
                _renderedText.Text = Value.ToString();
                var textX = x + Width + 10;
                var textY = y + (Height - _renderedText.Height) / 2;
                _renderedText.Draw(batcher, textX, textY);
            }

            return base.Draw(batcher, x, y);
        }

        private void DrawGradientBar(UltimaBatcher2D batcher, int x, int y, int width, int height, Color baseColor, Color shadowColor)
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
            
            for (int i = 4; i < width - 4; i += 8)
            {
                int lineX = x + i + random.Next(-2, 3);
                if (lineX >= x + 2 && lineX < x + width - 2)
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

        private void DrawSlider(UltimaBatcher2D batcher, int x, int y, int sliderX, int height, Color sliderColor)
        {
            const int SliderWidth = 12;
            int sliderHeight = height - 4;
            int sliderY = y + 2;

            batcher.Draw(_pixelTexture, new Rectangle(x + sliderX + 1, sliderY + 1, SliderWidth, sliderHeight),
                new Vector3(_shadowColor.R / 255f, _shadowColor.G / 255f, _shadowColor.B / 255f));

            batcher.Draw(_pixelTexture, new Rectangle(x + sliderX, sliderY, SliderWidth, sliderHeight),
                new Vector3(sliderColor.R / 255f, sliderColor.G / 255f, sliderColor.B / 255f));

            batcher.Draw(_pixelTexture, new Rectangle(x + sliderX, sliderY, SliderWidth, 2),
                new Vector3(_highlightColor.R / 255f, _highlightColor.G / 255f, _highlightColor.B / 255f));

            batcher.Draw(_pixelTexture, new Rectangle(x + sliderX, sliderY, 2, sliderHeight),
                new Vector3(_highlightColor.R / 255f, _highlightColor.G / 255f, _highlightColor.B / 255f));

            batcher.Draw(_pixelTexture, new Rectangle(x + sliderX, sliderY + sliderHeight - 2, SliderWidth, 2),
                new Vector3(_shadowColor.R / 255f, _shadowColor.G / 255f, _shadowColor.B / 255f));

            batcher.Draw(_pixelTexture, new Rectangle(x + sliderX + SliderWidth - 2, sliderY, 2, sliderHeight),
                new Vector3(_shadowColor.R / 255f, _shadowColor.G / 255f, _shadowColor.B / 255f));
        }

        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                _clicked = true;
            }
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                _clicked = false;
                CalculateNew(x);
            }
        }

        protected override void OnMouseWheel(MouseEventType delta)
        {
            switch (delta)
            {
                case MouseEventType.WheelScrollUp:
                    Value++;
                    break;
                case MouseEventType.WheelScrollDown:
                    Value--;
                    break;
            }
            CalculateOffset();
        }

        private void CalculateNew(int x)
        {
            int len = BarWidth - 12; // Subtrair largura do slider
            int maxValue = MaxValue - MinValue;

            if (len > 0)
            {
                float perc = Math.Max(0, Math.Min(1, x / (float)len));
                Value = (int)(maxValue * perc) + MinValue;
            }
            CalculateOffset();
        }

        private void CalculateOffset()
        {
            if (Value < MinValue)
            {
                Value = MinValue;
            }
            else if (Value > MaxValue)
            {
                Value = MaxValue;
            }

            int value = Value - MinValue;
            int maxValue = MaxValue - MinValue;
            int length = BarWidth - 12; // Subtrair largura do slider

            if (maxValue > 0)
            {
                Percents = value / (float)maxValue * 100.0f;
            }
            else
            {
                Percents = 0;
            }

            _sliderX = (int)(length * Percents / 100.0f);

            if (_sliderX < 0)
            {
                _sliderX = 0;
            }
        }

        private void ModifyPairedValues(int delta)
        {
            if (_pairedSliders.Count == 0 || _isUpdatingPairedValues)
            {
                return;
            }

            _isUpdatingPairedValues = true;

            try
            {
                bool updateSinceLastCycle = true;
                int d = delta > 0 ? -1 : 1;
                int points = Math.Abs(delta);
                int sliderIndex = Value % _pairedSliders.Count;

                while (points > 0)
                {
                    if (d > 0)
                    {
                        if (_pairedSliders[sliderIndex].Value < _pairedSliders[sliderIndex].MaxValue)
                        {
                            updateSinceLastCycle = true;
                            _pairedSliders[sliderIndex]._value += d;
                            _pairedSliders[sliderIndex].CalculateOffset();
                            _pairedSliders[sliderIndex].ValueChanged?.Invoke(_pairedSliders[sliderIndex], EventArgs.Empty);
                            points--;
                        }
                    }
                    else
                    {
                        if (_pairedSliders[sliderIndex].Value > _pairedSliders[sliderIndex].MinValue)
                        {
                            updateSinceLastCycle = true;
                            _pairedSliders[sliderIndex]._value += d;
                            _pairedSliders[sliderIndex].CalculateOffset();
                            _pairedSliders[sliderIndex].ValueChanged?.Invoke(_pairedSliders[sliderIndex], EventArgs.Empty);
                            points--;
                        }
                    }

                    sliderIndex++;

                    if (sliderIndex == _pairedSliders.Count)
                    {
                        if (!updateSinceLastCycle)
                        {
                            return;
                        }

                        updateSinceLastCycle = false;
                        sliderIndex = 0;
                    }
                }
            }
            finally
            {
                _isUpdatingPairedValues = false;
            }
        }

        public override void Dispose()
        {
            _renderedText?.Destroy();
            _pixelTexture?.Dispose();
            base.Dispose();
        }
    }
}
