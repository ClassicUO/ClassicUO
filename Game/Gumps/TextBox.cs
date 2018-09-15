using ClassicUO.Input;
using ClassicUO.Input.TextEntry;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using SDL2;

namespace ClassicUO.Game.Gumps
{
    public class TextBox : GumpControl
    {
        const float CARAT_BLINK_TIME = 500f;

        private bool _caratBlink;
        private readonly TextEntry _entry;


        public TextBox(byte font, int maxcharlength = -1, int maxlength = 0, bool isunicode = true, FontStyle style = FontStyle.None) : base()
        {
            _entry = new TextEntry(font, maxcharlength, maxlength, isunicode, style);

            base.AcceptKeyboardInput = true;
            base.AcceptMouseInput = true;
            IsEditable = true;
        }

        public TextBox(string[] parts, string[] lines) : this(1, parts[0] == "textentrylimited" ? int.Parse(parts[8]) : -1, int.Parse(parts[3]))
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            Width = int.Parse(parts[3]);
            Height = int.Parse(parts[4]);
            Hue = Hue.Parse(parts[5]);
            Graphic = Graphic.Parse(parts[6]);
            SetText(lines[int.Parse(parts[7])]);
        }


        public Hue Hue { get; set; }
        public Graphic Graphic { get; set; }
        public int MaxCharCount { get; set; }
        public bool IsPassword { get; set; }
        public bool NumericOnly { get; set; }
        public bool ReplaceDefaultTextOnFirstKeyPress { get; set; }
        public string Text => _entry.Text;

        public int LinesCount => _entry.GetLinesCount();

        public override bool AcceptMouseInput => base.AcceptMouseInput && IsEditable;
        public override bool AcceptKeyboardInput => base.AcceptKeyboardInput && IsEditable;

        public void SetText(string text, bool append = false)
        {
            if (append)
                _entry.InsertString(text);
            else
                _entry.SetText(text);
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (Service.Get<UIManager>().KeyboardFocusControl == this)
            {
                if (!IsFocused)
                {
                    SetFocused();
                    _caratBlink = true;
                }
                _caratBlink = true;
            }
            else
            {
                RemoveFocus();
                _caratBlink = false;
            }

            if (_entry.IsChanged)
            {
                _entry.UpdateCaretPosition();
            }

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position)
        {

            //if (_prevOffset < _offset)
            //{
            //    _text.Draw(spriteBatch, new Rectangle((int)position.X, (int)position.Y, _text.Width + _offset, _text.Height), -_offset, 0);

            //}
            //else
            //{
            //    _text.Draw(spriteBatch, new Rectangle((int)position.X + _offset, (int)position.Y, _text.Width + _offset, _text.Height), _offset, 0);

            //}

            //_prevOffset = _offset;

            _entry.RenderText.Draw(spriteBatch, new Vector3(position.X + _entry.Offset, position.Y, 0));


            if (IsEditable)
            {
                if (_caratBlink)
                    _entry.RenderCaret.Draw(spriteBatch, new Vector3(position.X + _entry.Offset + _entry.CaretPosition.X, position.Y + _entry.CaretPosition.Y, 0));
            }

            return base.Draw(spriteBatch, position);
        }

        public void RemoveLineAt(int index) => _entry.RemoveLineAt(index);

        protected override void OnTextInput(string c)
        {
            _entry.InsertString(c);
        }

        protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            switch (key)
            {
                /*case SDL.SDL_Keycode.SDLK_TAB:
                    if (AllowTAB)
                        _entry.InsertString("    ");
                    break;*/
                case SDL.SDL_Keycode.SDLK_RETURN:
                    _entry.InsertString("\n");
                    break;
                case SDL.SDL_Keycode.SDLK_BACKSPACE:
                    if (ReplaceDefaultTextOnFirstKeyPress)
                    {
                        //Text = string.Empty;
                        ReplaceDefaultTextOnFirstKeyPress = false;
                    }
                    else
                    {
                        _entry.RemoveChar(true);
                    }
                    break;
                case SDL.SDL_Keycode.SDLK_LEFT:
                    _entry.SeekCaretPosition(-1);
                    break;
                case SDL.SDL_Keycode.SDLK_RIGHT:
                    _entry.SeekCaretPosition(1);
                    break;
                case SDL.SDL_Keycode.SDLK_DELETE:
                    _entry.RemoveChar(false);
                    break;
                case SDL.SDL_Keycode.SDLK_HOME:
                    _entry.SetCaretPosition(0);
                    break;
                case SDL.SDL_Keycode.SDLK_END:
                    _entry.SetCaretPosition(Text.Length - 1);
                    break;
            }
        }

        protected override void OnMouseClick(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left)
                _entry.OnMouseClick(x, y);
        }

    }
}
