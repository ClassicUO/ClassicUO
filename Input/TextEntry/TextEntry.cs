using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Input.TextEntry
{
    public class TextEntry
    {
        private string _plainText;

        public TextEntry(byte font, int maxcharlength = -1, int maxlength = 0, bool unicode = true)
        {
            RenderText = new Renderer.RenderedText()
            {
                IsUnicode = unicode,
                Font = font,
                MaxWidth = maxlength,
            };

            RenderCaret = new RenderedText("_")
            {
                IsUnicode = unicode,
                Font = font
            };

            MaxCharCount = maxcharlength;
            MaxLength = maxlength;
        }

        public int MaxCharCount { get; }
        public int MaxLength { get; }
        public bool IsPassword { get; set; }
        public bool NumericOnly { get; set; }
        public ushort Hue { get => RenderText.Hue; set => RenderText.Hue = value; }

        public bool IsChanged { get; set; }
        public int Offset { get; set; }
        public Point CaretPosition { get; set; }
        protected int CaretIndex { get; set; }
        public RenderedText RenderText { get; }
        public RenderedText RenderCaret { get; }

        public string Text
        {
            get => IsPassword ? _plainText : RenderText.Text;
            set
            {
                _plainText = value;
                //if (MultiLine)
                //    _text.MaxWidth = Parent.Width - _carat.Width / 2;
                RenderText.Text = IsPassword ? new string('*', value.Length) : value;

                IsChanged = true;

            }
        }


        protected virtual void OnTextChanged()
        {

        }

        protected virtual void OnCaretPositionChanged()
        {

        }

        public void InsertString(string c)
        {
            if (CaretIndex < 0)
                CaretIndex = 0;

            if (CaretIndex > Text.Length)
                CaretIndex = Text.Length;

            if (MaxCharCount > 0)
            {
                if (NumericOnly)
                {
                    string s = Text;
                    s = s.Insert(CaretIndex, c);

                    if (int.Parse(s) > MaxCharCount)
                        return;
                }
                else if (Text.Length >= MaxCharCount)
                    return;
            }

            string text = Text.Insert(CaretIndex, c);
            CaretIndex += c.Length;

            Text = text;
        }

        public void RemoveChar(bool fromleft)
        {
            if (fromleft)
            {
                if (CaretIndex < 1)
                    return;
                CaretIndex--;
            }
            else
            {
                if (CaretIndex >= Text.Length)
                    return;
            }

            if (CaretIndex < Text.Length)
                Text = Text.Remove(CaretIndex, 1);
            else
                Text = Text.Remove(Text.Length);
        }

        public void SeekCaretPosition(int value)
        {
            CaretIndex += value;

            if (CaretIndex < 0)
                CaretIndex = 0;

            if (CaretIndex > Text.Length)
                CaretIndex = Text.Length;

            IsChanged = true;
        }

        public void SetCaretPosition(int value)
        {
            CaretIndex = value;

            if (CaretIndex < 0)
                CaretIndex = 0;

            if (CaretIndex > Text.Length)
                CaretIndex = Text.Length;

            IsChanged = true;
        }


        public void UpdateCaretPosition(string text, int width)
        {
            int x, y;

            if (RenderText.IsUnicode)
                (x, y) = IO.Resources.Fonts.GetCaretPosUnicode(RenderText.Font, text, CaretIndex, MaxLength, RenderText.Align, (ushort)RenderText.FontStyle);
            else
                (x, y) = IO.Resources.Fonts.GetCaretPosASCII(RenderText.Font, text, CaretIndex, MaxLength, RenderText.Align, (ushort)RenderText.FontStyle);

            CaretPosition = new Point(x, y);

            //if (_offset > 0)
            //{
            //    if (_caretPosition.X + _offset < 0)
            //        _offset = -_caretPosition.X;
            //    else if (Width + -_offset < _caretPosition.X + _carat.Width)
            //        _offset = Width - _caretPosition.X - _carat.Width;
            //}
            //else if (Width + _offset < _caretPosition.X + _carat.Width)
            //    _offset = Width - _caretPosition.X - _carat.Width;
            //else
            //    _offset = 0;

            if (Offset > 0)
            {
                if (CaretPosition.X + Offset < 0)
                    Offset = -CaretPosition.X;
                else if (MaxLength + -Offset < CaretPosition.X)
                    Offset = MaxLength - CaretPosition.X;
            }
            else if (MaxLength + Offset < CaretPosition.X)
                Offset = MaxLength - CaretPosition.X;
            else
                Offset = 0;

            if (IsChanged)
                IsChanged = false;
        }


        public void OnMouseClick(int x, int y)
        {
            int oldPos = CaretIndex;

            if (RenderText.IsUnicode)
                CaretIndex = IO.Resources.Fonts.CalculateCaretPosUnicode(RenderText.Font, Text, x, y, MaxLength, RenderText.Align, (ushort)RenderText.FontStyle);
            else
                CaretIndex = IO.Resources.Fonts.CalculateCaretPosASCII(RenderText.Font, Text, x, y, MaxLength, RenderText.Align, (ushort)RenderText.FontStyle);


            if (oldPos != CaretIndex)
                UpdateCaretPosition(Text, MaxLength);
        }
    }
}
