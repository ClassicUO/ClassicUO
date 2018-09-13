using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Input.TextEntry
{
    public class TextEntry
    {
        private string _plainText;

        public TextEntry(byte font, int maxcharlength = -1, int maxlength = 0, bool unicode = true, FontStyle style = FontStyle.None)
        {
            RenderText = new RenderedText()
            {
                IsUnicode = unicode,
                Font = font,
                MaxWidth = maxlength,
                FontStyle = style
            };

            RenderCaret = new RenderedText("_")
            {
                IsUnicode = unicode,
                Font = font,
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

            SetText(text);
        }

        public void SetText(string text) => Text = text;

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


        public void UpdateCaretPosition()
        {
            int x, y;

            if (RenderText.IsUnicode)
                (x, y) = IO.Resources.Fonts.GetCaretPosUnicode(RenderText.Font, Text, CaretIndex, MaxLength, RenderText.Align, (ushort)RenderText.FontStyle);
            else
                (x, y) = IO.Resources.Fonts.GetCaretPosASCII(RenderText.Font, Text, CaretIndex, MaxLength, RenderText.Align, (ushort)RenderText.FontStyle);

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
                UpdateCaretPosition();
        }

        public int GetLinesCount() => RenderText.IsUnicode ? IO.Resources.Fonts.GetLinesCountUnicode(RenderText.Font, Text, RenderText.Align, (ushort)RenderText.FontStyle, RenderText.MaxWidth) :
                IO.Resources.Fonts.GetLinesCountASCII(RenderText.Font, Text, RenderText.Align, (ushort)RenderText.FontStyle, RenderText.MaxWidth);

        //public int GetLinesCount()
        //{
        //    return Text.Split(new char[2] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries).Length;
        //}

        public void RemoveLineAt(int index)
        {
            //var lines = Text.Split(new char[2] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

            //if (lines == null || lines.Length <= 0)
            //    return;

            //StringBuilder sb = new StringBuilder();
            //for (int i = 0; i < lines.Length; i++)
            //{
            //    if (i != index)
            //        sb.Append(lines[i] + "\n");
            //}

            //Text = sb.ToString();

            int count = GetLinesCount();
            if (count <= index)
                return;

            int current = 0;
            int foundedIndex = 0;
            while (index != count)
            {
                int first = Text.IndexOf('\n', foundedIndex);
                if (first == -1)
                    break;

                if (index == current)
                {
                    Text = Text.Remove(foundedIndex, first - foundedIndex + 1);
                    break;
                }
                foundedIndex = first + 1;
                current++;
            }

        }

        public void Clear()
        {
            Text = string.Empty;
            Offset = 0;
            CaretPosition = Point.Zero;
            CaretIndex = 0;
        }
    }
}
