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
        private RenderedText _rendererText, _rendererCaret, _passwordRenderedText;

        private int _maxCharCount = -1;
        private Point _caretScreenPosition;
        private bool _leftWasDown, _isPassword;
        private ushort _hue;
        private RenderedText _currentRenderedText => IsPassword ? _passwordRenderedText : _rendererText;


        public StbTextBox(byte font, int max_char_count = -1, int maxWidth = 0, int width = 0, bool isunicode = true, FontStyle style = FontStyle.None, ushort hue = 0, TEXT_ALIGN_TYPE align = 0)
        {
            AcceptKeyboardInput = true;
            AcceptMouseInput = true;
            CanMove = false;
            IsEditable = true;

            _hue = hue;
            _maxCharCount = max_char_count;

            _stb = new TextEdit(this);
            _stb.SingleLine = true;

            _rendererText = RenderedText.Create(string.Empty, hue, font, isunicode, style, align, maxWidth, 30, false, false, false);
            if (maxWidth > 0)
                _rendererText.FontStyle |= FontStyle.Cropped;

            _rendererCaret = RenderedText.Create("_", hue, font, isunicode, (style & FontStyle.BlackBorder) != 0 ? FontStyle.BlackBorder : FontStyle.None, align: align);
        }



        public string Text
        {
            get => _currentRenderedText.Text;
            set
            {
                if (_maxCharCount >= 0 && value != null && value.Length > _maxCharCount)
                    value = value.Substring(0, _maxCharCount);

                _rendererText.Text = value;

                if (IsPassword)
                {
                    char[] v = value.ToCharArray();

                    for (int i = 0; i < v.Length; i++)
                    {
                        if (v[i] != '\n')
                            v[i] = '*';
                    }

                    _currentRenderedText.Text = new string(v);
                }
            }
        }

        public int Length => Text?.Length ?? 0;

        public bool AllowTAB { get; set; }

        public bool Multiline
        {
            get => !_stb.SingleLine;
            set => _stb.SingleLine = !value;
        }

        public int SelectionStart
        {
            get => _stb.SelectStart;
            set => _stb.SelectStart = value;
        }

        public int SelectionEnd
        {
            get => _stb.SelectEnd;
            set => _stb.SelectEnd = value;
        }

        public ushort Hue
        {
            get => _hue;
            set 
            {
                if (_currentRenderedText.Hue != value)
                {
                    if (_passwordRenderedText != null)
                        _passwordRenderedText.Hue = value;
                    _rendererText.Hue = value;
                    _rendererCaret.Hue = value;

                    _currentRenderedText.CreateTexture();
                    _rendererCaret.CreateTexture();
                }
            }
        }

        public bool IsPassword
        {
            get => _isPassword;
            set
            {
                _isPassword = value;

                if (value && (_passwordRenderedText == null || _passwordRenderedText.IsDestroyed))
                {
                    _passwordRenderedText = RenderedText.Create(
                                                                "*",
                                                                _rendererText.Hue,
                                                                _rendererText.Font,
                                                                _rendererText.IsUnicode,
                                                                _rendererText.FontStyle,
                                                                _rendererText.Align,
                                                                _rendererText.MaxWidth,
                                                                _rendererText.Cell,
                                                                _rendererText.IsHTML,
                                                                _rendererText.RecalculateWidthByInfo,
                                                                _rendererText.SaveHitMap);
                }
            }
        }



        public event EventHandler TextChanged;




        public float GetWidth(int index)
        {
            return _currentRenderedText.GetCharWidthAtIndex(index);
        }

        public TextEditRow LayoutRow(int startIndex)
        {
            TextEditRow r = _currentRenderedText.GetLayoutRow(startIndex);

            int sx = ScreenCoordinateX;
            int sy = ScreenCoordinateY;

            r.x0 += sx;
            r.x1 += sx;
            r.ymin += sy;
            r.ymax += sy;

            return r;
        }

        public void SelectAll()
        {
            _stb.SelectStart = 0;
            _stb.SelectEnd = Length;
        }




        
        private void UpdateCaretScreenPosition()
        {
            _caretScreenPosition = _currentRenderedText.GetCaretPosition(_stb.CursorIndex);
        }

        private ControlKeys ApplyShiftIfNecessary(ControlKeys k)
        {
            if (Keyboard.Shift)
            {
                k |= ControlKeys.Shift;
            }

            return k;
        }

        private bool IsMaxCharReached(int count)
            => _maxCharCount >= 0 && Length + count >= _maxCharCount;



        protected virtual void OnTextChanged()
        {
            TextChanged?.Raise();

            UpdateCaretScreenPosition();
        }





        protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            ControlKeys? stb_key = null;
            bool update_caret = false;

            switch (key)
            {
                case SDL.SDL_Keycode.SDLK_TAB:
                    if (AllowTAB)
                    {
                        // UO does not support '\t' char in its fonts
                        OnTextInput("   ");
                    }
                    else
                    {
                        Parent?.KeyboardTabToNextFocus(this);
                    }
                    break;
                case SDL.SDL_Keycode.SDLK_a when Keyboard.Ctrl:
                    SelectAll();
                    break;
                case SDL.SDL_Keycode.SDLK_ESCAPE:
                    SelectionStart = 0;
                    SelectionEnd = 0;
                    break;

                case SDL.SDL_Keycode.SDLK_INSERT when IsEditable:
                    stb_key = ControlKeys.InsertMode;
                    break;
                case SDL.SDL_Keycode.SDLK_c when Keyboard.Ctrl:
                    int selectStart = Math.Min(_stb.SelectStart, _stb.SelectEnd);
                    int selectEnd = Math.Max(_stb.SelectStart, _stb.SelectEnd);

                    if (selectStart < selectEnd)
                    {
                        SDL.SDL_SetClipboardText(Text.Substring(selectStart, selectEnd - selectStart));
                    }

                    break;
                case SDL.SDL_Keycode.SDLK_x when Keyboard.Ctrl:
                    selectStart = Math.Min(_stb.SelectStart, _stb.SelectEnd);
                    selectEnd = Math.Max(_stb.SelectStart, _stb.SelectEnd);

                    if (selectStart < selectEnd)
                    {
                        SDL.SDL_SetClipboardText(Text.Substring(selectStart, selectEnd - selectStart));
                        if (IsEditable)
                            _stb.Cut();
                    }

                    break;
                case SDL.SDL_Keycode.SDLK_v when Keyboard.Ctrl && IsEditable:
                    OnTextInput(SDL.SDL_GetClipboardText());
                    break;
                case SDL.SDL_Keycode.SDLK_z when Keyboard.Ctrl && IsEditable:
                    stb_key = ControlKeys.Undo;
                    break;
                case SDL.SDL_Keycode.SDLK_y when Keyboard.Ctrl && IsEditable:
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
                case SDL.SDL_Keycode.SDLK_BACKSPACE when IsEditable:
                    stb_key = ApplyShiftIfNecessary(ControlKeys.BackSpace);
                    update_caret = true;
                    break;
                case SDL.SDL_Keycode.SDLK_DELETE when IsEditable:
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
                case SDL.SDL_Keycode.SDLK_RETURN when IsEditable && !IsMaxCharReached(0):
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
            if (c == null || !IsEditable)
                return;

            int count;

            if (_maxCharCount >= 0)
            {
                int remains = _maxCharCount - Length;
                if (remains <= 0)
                    return;

                count = Math.Min(remains, c.Length);
            }
            else
            {
                count = c.Length;
            } 

            for (int i = 0; i < count; i++)
            {                
                _stb.InputChar(c[i]);
            }

            OnTextChanged();
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);

            ResetHueVector();

            RenderedText renderText = _currentRenderedText;

            int selectStart = Math.Min(_stb.SelectStart, _stb.SelectEnd);
            int selectEnd = Math.Max(_stb.SelectStart, _stb.SelectEnd);

            if (selectStart < selectEnd)
            {
                MultilinesFontInfo info = renderText.GetInfo();

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
                            drawX += renderText.GetCharWidth(info.Data[i].Item);
                        }

                        // selection is gone. Bye bye
                        if (selectEnd >= info.CharStart && selectEnd < info.CharStart + info.CharCount)
                        {
                            int count = selectEnd - selectStart;

                            int endX = 0;

                            // calculate width 
                            for (int k = 0; k < count; k++)
                            {
                                endX += renderText.GetCharWidth(info.Data[startSelectionIndex + k].Item);
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

            renderText.Draw(batcher, x, y);
            
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
            _passwordRenderedText?.Destroy();
            _rendererText?.Destroy();
            _rendererCaret?.Destroy();

            base.Dispose();
        }
    }
}
