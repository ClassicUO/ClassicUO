using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using SDL2;
using StbTextEditSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.UI.Controls
{
    class StbTextBox : Control, ITextEditHandler
    {
        private readonly TextEdit _stb;
        private RenderedText _renderText, _rendererCaret;

        private int _maxCharCount = -1;
        private Point _caretScreenPosition;


        public StbTextBox(byte font, int max_char_count = -1, int maxWidth = 0, int width = 0, bool isunicode = true, FontStyle style = FontStyle.None, ushort hue = 0, TEXT_ALIGN_TYPE align = 0)
        {
            AcceptKeyboardInput = true;
            AcceptMouseInput = true;
            CanMove = false;

            _maxCharCount = max_char_count;

            _stb = new TextEdit(this);

            _renderText = RenderedText.Create(string.Empty, hue, font, isunicode, style, align, maxWidth, 30, false, false, false);
            if (maxWidth > 0)
                _renderText.FontStyle |= FontStyle.Cropped;

            _rendererCaret = RenderedText.Create("_", hue, font, isunicode, (style & FontStyle.BlackBorder) != 0 ? FontStyle.BlackBorder : FontStyle.None, align: align);
        }



        public string Text
        {
            get => _renderText.Text;
            set => _renderText.Text = value;
        }

        public int Length => Text?.Length ?? 0;

        public bool Multiline
        {
            get => !_stb.SingleLine;
            set => _stb.SingleLine = !value;
        }


        public event EventHandler TextChanged;


        public float GetWidth(int index)
        {
            return 0;
        }

        public TextEditRow LayoutRow(int startIndex)
        {
            TextEditRow r = _renderText.GetLayoutRow(startIndex);

            Rectangle bounds = this.Bounds;

            r.x0 += bounds.X;
            r.x1 += bounds.X;
            r.ymin += bounds.Y;
            r.ymax += bounds.Y;

            return r;
        }

        public void SelectAll()
        {
            _stb.SelectStart = 0;
            _stb.SelectEnd = Length;
        }




        protected virtual void OnTextChanged()
        {
            TextChanged?.Raise();

            UpdateCaretScreenPosition();
        }

        private void UpdateCaretScreenPosition()
        {
            _caretScreenPosition = _renderText.GetCaretPosition(_stb.CursorIndex);
        }


        protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            ControlKeys? stb_key = null;

            switch (key)
            {
                case SDL.SDL_Keycode.SDLK_a when Keyboard.Ctrl:
                    SelectAll();
                    break;
                case SDL.SDL_Keycode.SDLK_ESCAPE:
                    _stb.SelectStart = 0;
                    _stb.SelectEnd = 0;
                    break;

                case SDL.SDL_Keycode.SDLK_INSERT:
                    stb_key = ControlKeys.InsertMode;
                    break;
                case SDL.SDL_Keycode.SDLK_z when Keyboard.Ctrl:
                    stb_key = ControlKeys.Undo;
                    break;
                case SDL.SDL_Keycode.SDLK_y when Keyboard.Ctrl:
                    stb_key = ControlKeys.Redo;
                    break;
                case SDL.SDL_Keycode.SDLK_LEFT:
                    if (Keyboard.Ctrl && Keyboard.Shift)
                    {
                        stb_key = ControlKeys.Shift | ControlKeys.WordLeft;
                    }
                    else if (Keyboard.Shift)
                    {
                        stb_key = ControlKeys.Shift | ControlKeys.Left;
                    }
                    else if (Keyboard.Ctrl)
                    {
                        stb_key = ControlKeys.WordLeft;
                    }
                    else
                    {
                        stb_key = ControlKeys.Left;
                    }
                    UpdateCaretScreenPosition();
                    break;
                case SDL.SDL_Keycode.SDLK_RIGHT:
                    if (Keyboard.Ctrl && Keyboard.Shift)
                    {
                        stb_key = ControlKeys.Shift | ControlKeys.WordRight;
                    }
                    else if (Keyboard.Shift)
                    {
                        stb_key = ControlKeys.Shift | ControlKeys.Right;
                    }
                    else if (Keyboard.Ctrl)
                    {
                        stb_key = ControlKeys.WordRight;
                    }
                    else
                    {
                        stb_key = ControlKeys.Right;
                    }
                    UpdateCaretScreenPosition();
                    break;
                case SDL.SDL_Keycode.SDLK_UP:
                    stb_key = ControlKeys.Up;
                    UpdateCaretScreenPosition();
                    break;
                case SDL.SDL_Keycode.SDLK_DOWN:
                    stb_key = ControlKeys.Down;
                    UpdateCaretScreenPosition();
                    break;
                case SDL.SDL_Keycode.SDLK_BACKSPACE:
                    stb_key = ControlKeys.BackSpace;
                    break;
                case SDL.SDL_Keycode.SDLK_DELETE:
                    stb_key = ControlKeys.Delete;
                    break;
                case SDL.SDL_Keycode.SDLK_HOME:
                    if (Keyboard.Ctrl && Keyboard.Shift)
                    {
                        stb_key = ControlKeys.Shift | ControlKeys.TextStart;
                    }
                    else if (Keyboard.Shift)
                    {
                        stb_key = ControlKeys.Shift | ControlKeys.LineStart;
                    }
                    else if (Keyboard.Ctrl)
                    {
                        stb_key = ControlKeys.TextStart;
                    }
                    else
                    {
                        stb_key = ControlKeys.LineStart;
                    }
                    UpdateCaretScreenPosition();
                    break;
                case SDL.SDL_Keycode.SDLK_END:
                    if (Keyboard.Ctrl && Keyboard.Shift)
                    {
                        stb_key = ControlKeys.Shift | ControlKeys.TextEnd;
                    }
                    else if (Keyboard.Shift)
                    {
                        stb_key = ControlKeys.Shift | ControlKeys.LineEnd;
                    }
                    else if (Keyboard.Ctrl)
                    {
                        stb_key = ControlKeys.TextEnd;
                    }
                    else
                    {
                        stb_key = ControlKeys.LineEnd;
                    }
                    UpdateCaretScreenPosition();
                    break;
                case SDL.SDL_Keycode.SDLK_RETURN:
                    _stb.InputChar('\n');
                    OnTextChanged();
                    break;
            }

            if (stb_key != null)
            {
                _stb.Key(stb_key.Value);
            }

            base.OnKeyDown(key, mod);
        }

        protected override void OnTextInput(string c)
        {
            if (c == null)
                return;

            for (int i = 0; i < c.Length; i++)
            {
                _stb.InputChar(c[i]);
            }

            OnTextChanged();
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);

            _renderText.Draw(batcher, x, y);
            
            if (IsFocused)
            {
                _rendererCaret.Draw(batcher, x + _caretScreenPosition.X, y + _caretScreenPosition.Y);
            }

            return true;
        }

        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            _stb.Click(x, y);
            base.OnMouseDown(x, y, button);
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            base.OnMouseUp(x, y, button);
        }

        protected override void OnMouseOver(int x, int y)
        {
            base.OnMouseOver(x, y);
        }


        public override void Dispose()
        {
            _renderText?.Destroy();
            _rendererCaret?.Destroy();

            base.Dispose();
        }
    }
}
