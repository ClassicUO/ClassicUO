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
        private bool _leftWasDown;


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
            return _renderText.GetCharWidthAtIndex(index);
        }

        public TextEditRow LayoutRow(int startIndex)
        {
            TextEditRow r = _renderText.GetLayoutRow(startIndex);

            r.x0 += Bounds.X;
            r.x1 += Bounds.Width;
            r.ymin += Bounds.Y;
            r.ymax += Bounds.Height;

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

        private ControlKeys ApplyShiftIfNecessary(ControlKeys k)
        {
            if (Keyboard.Shift)
            {
                k |= ControlKeys.Shift;
            }

            return k;
        }

        protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            ControlKeys? stb_key = null;
            bool update_caret = false;

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
                case SDL.SDL_Keycode.SDLK_v when Keyboard.Ctrl:
                    string clipboard = SDL.SDL_GetClipboardText();

                    if (!string.IsNullOrEmpty(clipboard))
                    {
                        for (int i = 0; i < clipboard.Length && i < 2000; i++)
                        {
                            _stb.InputChar(clipboard[i]);
                        }

                        OnTextChanged();
                    }
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

                    update_caret = true;
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
                    update_caret = true;
                    break;
                case SDL.SDL_Keycode.SDLK_UP:
                    stb_key = ApplyShiftIfNecessary(ControlKeys.Up);
                    update_caret = true;
                    break;
                case SDL.SDL_Keycode.SDLK_DOWN:
                    stb_key = ApplyShiftIfNecessary(ControlKeys.Down);
                    update_caret = true;
                    break;
                case SDL.SDL_Keycode.SDLK_BACKSPACE:
                    stb_key = ApplyShiftIfNecessary(ControlKeys.BackSpace);
                    update_caret = true;
                    break;
                case SDL.SDL_Keycode.SDLK_DELETE:
                    stb_key = ApplyShiftIfNecessary(ControlKeys.Delete);
                    update_caret = true;
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
                    update_caret = true;
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
                    update_caret = true;
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

            if (update_caret)
            {
                UpdateCaretScreenPosition();
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

            ResetHueVector();

            int selectStart = Math.Min(_stb.SelectStart, _stb.SelectEnd);
            int selectEnd = Math.Max(_stb.SelectStart, _stb.SelectEnd);

            if (selectStart < selectEnd)
            {
                MultilinesFontInfo info = _renderText.GetInfo();

                int drawY = 0;

                while (info != null && selectStart < selectEnd)
                {
                    // ok we are inside the selection
                    if (selectStart >= info.CharStart && selectStart < info.CharStart + info.CharCount)
                    {
                        int startSelectionIndex = selectStart - info.CharStart;

                        // calculate offset x
                        int drawX = 0;
                        for (int i = 0; i < startSelectionIndex; i++)
                        {
                            drawX += _renderText.GetCharWidth(info.Data[i].Item);
                        }

                        // selection is gone. Bye bye
                        if (selectEnd >= info.CharStart && selectEnd < info.CharStart + info.CharCount)
                        {
                            int count = selectEnd - selectStart;

                            int endX = 0;

                            // calculate width 
                            for (int k = 0; k < count; k++)
                            {
                                endX += _renderText.GetCharWidth(info.Data[startSelectionIndex + k].Item);
                            }

                            batcher.Draw2D(
                                           Texture2DCache.GetTexture(Color.Magenta),
                                           x + drawX,
                                           y + drawY,
                                           endX,
                                           info.MaxHeight,
                                           ref _hueVector);
                            
                            break;
                        }


                        // do the whole line
                        batcher.Draw2D(
                                       Texture2DCache.GetTexture(Color.Magenta),
                                       x + drawX,
                                       y + drawY,
                                       info.Width - drawX,
                                       info.MaxHeight,
                                       ref _hueVector);

                        // first selection is gone. M
                        selectStart = info.CharStart + info.CharCount;
                    }

                    drawY += info.MaxHeight;
                    info = info.Next;
                }
            }


            _renderText.Draw(batcher, x, y);
            
            if (IsFocused)
            {
                _rendererCaret.Draw(batcher, x + _caretScreenPosition.X, y + _caretScreenPosition.Y);
            }

            return true;
        }

        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                _leftWasDown = true;
                _stb.Click(Mouse.Position.X, Mouse.Position.Y);
                UpdateCaretScreenPosition();
            }
          
            base.OnMouseDown(x, y, button);
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                _leftWasDown = false;
            }

            base.OnMouseUp(x, y, button);
        }

        protected override void OnMouseOver(int x, int y)
        {
            base.OnMouseOver(x, y);

            if (!_leftWasDown)
                return;

            _stb.Drag(Mouse.Position.X, Mouse.Position.Y);
        }


        public override void Dispose()
        {
            _renderText?.Destroy();
            _rendererCaret?.Destroy();

            base.Dispose();
        }
    }
}
