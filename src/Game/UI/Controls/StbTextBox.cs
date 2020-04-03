using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using SDL2;
using StbTextEditSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.UI.Controls
{
    class StbPasswordBox : StbTextBox
    {
        private RenderedText _passwordRenderedText;

        public StbPasswordBox(byte font, int max_char_count = -1, int maxWidth = 0, int width = 0, bool isunicode = true, FontStyle style = FontStyle.None, ushort hue = 0, TEXT_ALIGN_TYPE align = TEXT_ALIGN_TYPE.TS_LEFT) : base(font, max_char_count, maxWidth, isunicode, style, hue, align)
        {
            _passwordRenderedText = RenderedText.Create(string.Empty, hue, font, isunicode, style, align, maxWidth, 30, false, true, true);
        }

        public string PlainText { get; private set; }



        protected override void OnTextInput(string c)
        {

            base.OnTextInput(c);
        }

        protected override void OnTextChanged()
        {
            PlainText = Text;

            if (!string.IsNullOrEmpty(Text))
            {
                char[] v = Text.ToCharArray();

                for (int i = 0; i < v.Length; i++)
                {
                    if (v[i] != '\n')
                        v[i] = '*';
                }
                _passwordRenderedText.Text = new string(v);
            }
            else
            {
                _passwordRenderedText.Text = string.Empty;
            }

            base.OnTextChanged();
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            DrawSelection(batcher, x, y);

            _passwordRenderedText.Draw(batcher, x, y);

            DrawCaret(batcher, x, y);

            return true;
        }

        public override void Dispose()
        {
            _passwordRenderedText?.Destroy();
            base.Dispose();
        }
    }

    class StbTextBox : Control, ITextEditHandler
    {
        private readonly TextEdit _stb;
        private RenderedText _rendererText, _rendererCaret;

        private int _maxCharCount = -1;
        private Point _caretScreenPosition;
        private bool _leftWasDown, _fromServer;
        private ushort _hue;

        public StbTextBox(byte font, int max_char_count = -1, int maxWidth = 0, bool isunicode = true, FontStyle style = FontStyle.None, ushort hue = 0, TEXT_ALIGN_TYPE align = 0)
        {
            AcceptKeyboardInput = true;
            AcceptMouseInput = true;
            CanMove = false;
            IsEditable = true;

            _hue = hue;
            _maxCharCount = max_char_count;

            _stb = new TextEdit(this);
            _stb.SingleLine = true;

            if ((style & (FontStyle.Fixed | FontStyle.Cropped)) != 0 && maxWidth <= 0)
            {
                throw new Exception(nameof(maxWidth));
            }

            _rendererText = RenderedText.Create(string.Empty, hue, font, isunicode, style, align, maxWidth, 30, false, false, false);
            if (maxWidth > 0)
                _rendererText.FontStyle |= FontStyle.Cropped;

            _rendererCaret = RenderedText.Create("_", hue, font, isunicode, (style & FontStyle.BlackBorder) != 0 ? FontStyle.BlackBorder : FontStyle.None, align: align);

            Height = _rendererText.Height;
        }

        public StbTextBox(List<string> parts, string[] lines) : this(1, parts[0] == "textentrylimited" ? int.Parse(parts[8]) : byte.MaxValue, int.Parse(parts[3]), style: FontStyle.BlackBorder | FontStyle.CropTexture, hue: (ushort) (UInt16Converter.Parse(parts[5]) + 1))
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            Width = int.Parse(parts[3]);
            Height = _rendererText.MaxHeight = int.Parse(parts[4]);
            Multiline = true;
            _fromServer = true;
            LocalSerial = SerialHelper.Parse(parts[6]);
            _rendererText.FontStyle &= ~FontStyle.Cropped;
            int index = int.Parse(parts[7]);

            if (index >= 0 && index < lines.Length)
                Text = lines[index];
        }


        public override bool AcceptKeyboardInput => base.AcceptKeyboardInput && IsEditable;

        public string Text
        {
            get => _rendererText.Text;
            set
            {
                if (_maxCharCount >= 0 && value != null && value.Length > _maxCharCount)
                    value = value.Substring(0, _maxCharCount);

                Sanitize(ref value);

                _rendererText.Text = value;

                OnTextChanged();
            }
        }

        public int Length => Text?.Length ?? 0;

        public bool AllowTAB { get; set; }

        public bool Multiline
        {
            get => !_stb.SingleLine;
            set => _stb.SingleLine = !value;
        }

        public bool NumbersOnly { get; set; }

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
                if (_rendererText.Hue != value)
                {
                    if (_rendererText != null)
                        _rendererText.Hue = value;
                    _rendererText.Hue = value;
                    _rendererCaret.Hue = value;

                    _rendererText.CreateTexture();
                    _rendererCaret.CreateTexture();
                }
            }
        }


        public event EventHandler TextChanged;




        public float GetWidth(int index)
        {
            return _rendererText.GetCharWidthAtIndex(index);
        }

        public TextEditRow LayoutRow(int startIndex)
        {
            TextEditRow r = _rendererText.GetLayoutRow(startIndex);

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
            _caretScreenPosition = _rendererText.GetCaretPosition(_stb.CursorIndex);
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

        private void Sanitize(ref string text)
        {
            FontStyle style = _rendererText.FontStyle;

            if ((style & FontStyle.Fixed) != 0 || (style & FontStyle.Cropped) != 0 || (style & FontStyle.CropTexture) != 0)
            {
                if (_rendererText.Width == 0 || string.IsNullOrEmpty(text))
                    return;

                int realWidth = _rendererText.IsUnicode
                                    ? FontsLoader.Instance.GetWidthUnicode(_rendererText.Font, text)
                                    : FontsLoader.Instance.GetWidthASCII(_rendererText.Font, text);

                if (realWidth > _rendererText.Width)
                {
                    string str = _rendererText.IsUnicode ? 
                                     FontsLoader.Instance.GetTextByWidthUnicode(_rendererText.Font, text, _rendererText.Width, (style & FontStyle.Cropped) != 0, _rendererText.Align, (ushort) _rendererText.FontStyle)
                                     : 
                                     FontsLoader.Instance.GetTextByWidthASCII(_rendererText.Font, text, _rendererText.Width, (style & FontStyle.Cropped) != 0, _rendererText.Align, (ushort) _rendererText.FontStyle);

                    if ((style & FontStyle.CropTexture) != 0)
                    {
                        int totalheight = 0;
                        // TODO: add a '\n' to split the string
                        while (totalheight < _rendererText.MaxHeight)
                        {
                            totalheight += _rendererText.IsUnicode ?
                                               FontsLoader.Instance.GetHeightUnicode(_rendererText.Font, str, _rendererText.Width, _rendererText.Align, (ushort) _rendererText.FontStyle)
                                               :
                                               FontsLoader.Instance.GetHeightASCII(_rendererText.Font, str, _rendererText.Width, _rendererText.Align, (ushort) _rendererText.FontStyle);

                            if (text.Length > str.Length)
                            {
                                str += _rendererText.IsUnicode ?
                                           FontsLoader.Instance.GetTextByWidthUnicode(_rendererText.Font, text.Substring(str.Length), _rendererText.Width, (style & FontStyle.Cropped) != 0, _rendererText.Align, (ushort) _rendererText.FontStyle)
                                           :
                                           FontsLoader.Instance.GetTextByWidthASCII(_rendererText.Font, text.Substring(str.Length), _rendererText.Width, (style & FontStyle.Cropped) != 0, _rendererText.Align, (ushort) _rendererText.FontStyle);
                            }
                            else
                                break;
                        }
                    }
                    
                    text = str;
                }
            }
        }


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
                case SDL.SDL_Keycode.SDLK_RETURN when IsEditable:
                    if (Multiline)
                    {
                        if (!_fromServer && !IsMaxCharReached(0))
                        {
                            _stb.InputChar('\n');
                            OnTextChanged();
                        }
                    }
                    else
                    {
                        Parent?.OnKeyboardReturn(0, Text);
                    }
                    
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
                if (NumbersOnly && !char.IsNumber(c[i]))
                    continue;

                _stb.InputChar(c[i]);
            }

            OnTextChanged();
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);

            DrawSelection(batcher, x, y);

            _rendererText.Draw(batcher, x, y);
            
            DrawCaret(batcher, x, y);

            return true;
        }

        private protected void DrawSelection(UltimaBatcher2D batcher, int x, int y)
        {
            ResetHueVector();

            int selectStart = Math.Min(_stb.SelectStart, _stb.SelectEnd);
            int selectEnd = Math.Max(_stb.SelectStart, _stb.SelectEnd);

            if (selectStart < selectEnd)
            {
                MultilinesFontInfo info = _rendererText.GetInfo();

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
                            drawX += _rendererText.GetCharWidth(info.Data[i].Item);
                        }

                        // selection is gone. Bye bye
                        if (selectEnd >= info.CharStart && selectEnd < info.CharStart + info.CharCount)
                        {
                            int count = selectEnd - selectStart;

                            int endX = 0;

                            // calculate width 
                            for (int k = 0; k < count; k++)
                            {
                                endX += _rendererText.GetCharWidth(info.Data[startSelectionIndex + k].Item);
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
        }

        private protected void DrawCaret(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsFocused)
            {
                _rendererCaret.Draw(batcher, x + _caretScreenPosition.X, y + _caretScreenPosition.Y);
            }
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
            _rendererText?.Destroy();
            _rendererCaret?.Destroy();

            base.Dispose();
        }
    }
}
