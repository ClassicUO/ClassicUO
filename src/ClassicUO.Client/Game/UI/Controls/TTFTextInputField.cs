using System;
using ClassicUO.Game.Managers;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class TTFTextInputField : Control
    {
        public readonly StbTextBox TextBox;
        private AlphaBlendControl _background;

        public event EventHandler TextChanged { add { TextBox.TextChanged += value; } remove { TextBox.TextChanged -= value; } }
        public new event EventHandler<KeyboardEventArgs> KeyDown { add { TextBox.KeyDown += value; } remove { TextBox.KeyDown -= value; } }

        public int CaretIndex { get { return TextBox.CaretIndex; } }
        public bool ConvertHtmlColors { get; set; }

        public TTFTextInputField
        (
            int width,
            int height,
            int maxWidthText = 0,
            int maxCharsCount = -1,
            string text = "",
            bool numbersOnly = false,
            bool multiline = false,
            bool convertHtmlColors = true,
            bool codeEditorStyle = false
        )
        {
            WantUpdateSize = false;

            Width = width;
            Height = height;

            if (codeEditorStyle)
            {
                int mw = maxWidthText > 0 ? maxWidthText : width - 16;
                TextBox = new StbTextBox(maxCharsCount, mw, true) { X = 4, Width = width - 8, Hue = 0x0481 };
            }
            else
            {
                TextBox = new StbTextBox(maxCharsCount, maxWidthText, multiline) { X = 4, Width = width - 8, Hue = 0x0481 };
            }

            ConvertHtmlColors = convertHtmlColors;
            TextBox.Height = height;
            TextBox.Text = text;
            TextBox.NumbersOnly = numbersOnly;
            if (codeEditorStyle)
                TextBox.AllowTAB = true;

            _background = new AlphaBlendControl() { Width = Width, Height = Height };
            if (codeEditorStyle)
                _background.BaseColor = new Color(30, 30, 30, 255);
            Add(_background);
            Add(TextBox);
        }

        public void SetFocus()
        {
            TextBox.SetKeyboardFocus();
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (batcher.ClipBegin(x, y, Width, Height))
            {
                base.Draw(batcher, x, y);

                batcher.ClipEnd();
            }

            return true;
        }

        private void UpdateBackground()
        {
            _background.Width = Width;
            _background.Height = Height;
        }

        public void UpdateSize(int width, int height)
        {
            Width = width;
            Height = height;
            TextBox.UpdateSize(width - 8, height);
            TextBox.Width = width - 8;
            TextBox.Height = height;
            UpdateBackground();
        }

        public string Text => TextBox.Text;

        public override bool AcceptKeyboardInput
        {
            get => TextBox.AcceptKeyboardInput;
            set => TextBox.AcceptKeyboardInput = value;
        }

        public bool NumbersOnly
        {
            get => TextBox.NumbersOnly;
            set => TextBox.NumbersOnly = value;
        }

        public void SetText(string text)
        {
            TextBox.SetText(text);
        }
    }
}
