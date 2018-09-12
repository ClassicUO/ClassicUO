using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using SDL2;

namespace ClassicUO.Game.Gumps
{
    public class TextBox : GumpControl
    {
        const float CARAT_BLINK_TIME = 500f;

        private bool _caratBlink;
        private float _lastCaratBlinkTime;
        private readonly RenderedText _text, _carat;

        private string _plainText;

        public TextBox() : base()
        {
            _text = new RenderedText()
            {
                IsUnicode = true,
                Font = 1,
            };
            _carat = new RenderedText("_")
            {
                IsUnicode = true,
                Font = 1,
            };

            base.AcceptKeyboardInput = true;
            base.AcceptMouseInput = true;
            IsEditable = true;
        }

        public TextBox(string[] parts, string[] lines) : this()
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            Width = int.Parse(parts[3]);
            Height = int.Parse(parts[4]);
            Hue = Hue.Parse(parts[5]);
            Graphic = Graphic.Parse(parts[6]);
            Text = lines[int.Parse(parts[7])];
            MaxCharCount = 0;
            if (parts[0] == "textentrylimited")
                MaxCharCount = int.Parse(parts[8]);
        }

        public Hue Hue { get; set; }
        public Graphic Graphic { get; set; }
        public int MaxCharCount { get; set; }
        public bool IsPassword { get; set; }
        public bool NumericOnly { get; set; }
        public bool ReplaceDefaultTextOnFirstKeyPress { get; set; }
        public string Text
        {
            get => IsPassword ? _plainText : _text.Text;
            set
            {
                _plainText = value;
                if (MultiLine)
                    _text.MaxWidth = Parent.Width;

                _text.Text = IsPassword ? new string('*', value.Length) : value;

                (int x, int y) = IO.Resources.Fonts.GetCaretPosUnicode(_carat.Font, _text.Text, _caretIndex, _text.Width, _carat.Align, (ushort)_carat.FontStyle);
                _caretPosition = new Point(x, y);

                _changed = false;
            }
        }

        public bool MultiLine { get; set; }
        public bool AllowTAB { get; set; }

        public override bool AcceptMouseInput => base.AcceptMouseInput && IsEditable;
        public override bool AcceptKeyboardInput => base.AcceptKeyboardInput && IsEditable;



        public override void Update(double totalMS, double frameMS)
        {
            if (GumpManager.KeyboardFocusControl == this)
            {
                if (!IsFocused)
                {
                    SetFocused();
                    _caratBlink = true;
                    _lastCaratBlinkTime = 0f;
                }

                _caratBlink = true;


            }
            else
            {
                RemoveFocus();
                _caratBlink = false;
            }

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position)
        {
            //Vector3 caratPosition = new Vector3(position.X, position.Y, 0);

            if (IsEditable)
            {
                if (_text.Width + _carat.Width <= Width)
                {
                    _text.Draw(spriteBatch, position);
                    //caratPosition.X += _text.Width;
                }
                else
                {
                    int offset = _text.Width - ( Width - _carat.Width );
                    _text.Draw(spriteBatch, new Rectangle((int)position.X, (int)position.Y, _text.Width - offset, _text.Height), offset, 0);
                    //caratPosition.X += ( Width - _carat.Width );
                }
            }
            else
            {
                //caratPosition.X = 0;
                _text.Draw(spriteBatch, new Rectangle((int)position.X, (int)position.Y, Width, Height), 0, 0);
            }

            if (_caratBlink)
                _carat.Draw(spriteBatch, new Vector3(position.X + _caretPosition.X, position.Y + _caretPosition.Y, 0));

            return base.Draw(spriteBatch, position);
        }

        protected override void OnTextInput(char c)
        {
            if (MaxCharCount != 0 && Text.Length >= MaxCharCount)
                return;

            if (NumericOnly && !char.IsNumber(c))
                return;

            if (ReplaceDefaultTextOnFirstKeyPress)
            {
                Text = string.Empty;
                ReplaceDefaultTextOnFirstKeyPress = false;
            }

            //Text += c;
            Insert(c.ToString());
        }

        protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            switch (key)
            {
                case SDL.SDL_Keycode.SDLK_PASTE:

                    break;
                case SDL.SDL_Keycode.SDLK_TAB:
                    // throw an error if text is empty :|
                    //if (AllowTAB)
                    //    Text += "\t";
                    break;
                case SDL.SDL_Keycode.SDLK_RETURN:
                    if (MultiLine)
                        Text += "\r\n";
                    break;
                case SDL.SDL_Keycode.SDLK_BACKSPACE:
                    if (ReplaceDefaultTextOnFirstKeyPress)
                    {
                        Text = string.Empty;
                        ReplaceDefaultTextOnFirstKeyPress = false;
                    }
                    else if (!string.IsNullOrEmpty(Text))
                    {
                        //Text = Text.Substring(0, Text.Length - 1);
                        RemoveChar();
                    }
                    break;
            }
        }


        private Point _caretPosition;
        private int _caretIndex;

        protected override void OnMouseClick(int x, int y, MouseButton button)
        {
            if (button != MouseButton.Left)
                return;

            int oldPos = _caretIndex;

            if (_text.IsUnicode)
                _caretIndex = IO.Resources.Fonts.CalculateCaretPosUnicode(_text.Font, _text.Text, x, y, _text.Width, _text.Align, (ushort)_text.FontStyle);
            else
                _caretIndex = IO.Resources.Fonts.CalculateCaretPosASCII(_text.Font, _text.Text, x, y, _text.Width, _text.Align, (ushort)_text.FontStyle);


            if (oldPos != _caretIndex)
            {
                _changed = true;
                (int xx, int yy) = IO.Resources.Fonts.GetCaretPosUnicode(_carat.Font, _text.Text, _caretIndex, _text.Width, _carat.Align, (ushort)_carat.FontStyle);
                _caretPosition = new Point(xx, yy);
                _changed = true;
            }

        }


        private bool _changed;


        private void AddCaretPosition(int value)
        {
            _caretIndex += value;

            if (_caretIndex < 0)
                _caretIndex = 0;

            if (_caretIndex > Text.Length)
                _caretIndex = Text.Length;

            _changed = true;
        }

        private void SetCaretPosition(int value)
        {
            _caretIndex = value;

            if (_caretIndex < 0)
                _caretIndex = 0;

            if (_caretIndex > Text.Length)
                _caretIndex = Text.Length;

            _changed = true;
        }

        private void Insert(string c)
        {
            if (_caretIndex < 0)
                _caretIndex = 0;

            if (_caretIndex > Text.Length)
                _caretIndex = Text.Length;

            if (MaxCharCount > 0)
            {
                if (NumericOnly)
                {
                    string s = Text;
                    s = s.Insert(_caretIndex, c);

                    if (int.Parse(s) > MaxCharCount)
                        return;
                }
                else if (Text.Length >= MaxCharCount)
                    return;
            }

            string text = Text.Insert(_caretIndex, c);
            _caretIndex += c.Length;
            _changed = true;

            Text = text;
        }

        private void AddChar(bool fromleft = false)
        {

        }

        private void RemoveChar(bool fromleft = false)
        {
            if (fromleft)
            {
                if (_caretIndex < 1)
                    return;
                _caretIndex--;
            }
            else
            {
                if (_caretIndex >= Text.Length)
                    return;
            }

            if (_caretIndex < Text.Length)
                Text = Text.Remove(_caretIndex);
            else
                Text = Text.Remove(Text.Length - 1);

            _changed = true;
        }

        private void Clear()
        {
            Text = string.Empty;
            _caretIndex = 0;
            _changed = true;

        }
    }
}
