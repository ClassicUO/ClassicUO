#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using ClassicUO.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using SDL2;
using StbTextEditSharp;

namespace ClassicUO.Game.UI.Controls
{
    internal class StbTextBox : Control, ITextEditHandler
    {
        const int CARET_BLINK_TIME = 450;

        protected static readonly Color SELECTION_COLOR = new Color() { PackedValue = 0x80a06020 };
        private readonly FontStyle _fontStyle;

        private readonly int _maxCharCount = -1;
        private FontSettings _fontSettings;
        private string _text = string.Empty;
        private float _maxWidth;
        private Vector2 _caretScreenPosition;  
        private bool _is_writing;
        private bool _leftWasDown, _fromServer;
        private uint _caretBlinkTime;
        private bool _cursorOn;
        private TextEdit _textEdit;


        public StbTextBox
        (
            byte font,
            int max_char_count = -1,
            int maxWidth = 0,
            bool isunicode = true,
            FontStyle style = FontStyle.None,
            ushort hue = 0,
            TEXT_ALIGN_TYPE align = 0
        )
        {
            AcceptKeyboardInput = true;
            AcceptMouseInput = true;
            CanMove = false;
            IsEditable = true;

            _maxCharCount = max_char_count;

            _textEdit = new TextEdit(this);
            _textEdit.SingleLine = true;

            if (maxWidth > 0)
            {
                style |= FontStyle.CropTexture;
            }

            Hue = hue;

            _fontStyle = style;       
            _maxWidth = maxWidth;
            _fontSettings.FontIndex = (byte)(font == 0xFF ? (Client.Version >= ClientVersion.CV_305D ? 1 : 0) : font);
            _fontSettings.Border = (style & FontStyle.BlackBorder) != 0;
            _fontSettings.IsUnicode = isunicode;

            Height = (int) UOFontRenderer.Shared.GetFontHeight(_fontSettings);
        }

        public StbTextBox(List<string> parts, string[] lines) : this
        (
            1,
            parts[0] == "textentrylimited" ? int.Parse(parts[8]) : byte.MaxValue,
            int.Parse(parts[3]),
            style: FontStyle.BlackBorder | FontStyle.CropTexture,
            hue: (ushort) (UInt16Converter.Parse(parts[5]) + 1)
        )
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            Width = (int) _maxWidth; //int.Parse(parts[3]);
            Height = /*_rendererText.MaxHeight =*/ int.Parse(parts[4]);
            Multiline = false;
            _fromServer = true;
            LocalSerial = SerialHelper.Parse(parts[6]);
            IsFromServer = true;

            int index = int.Parse(parts[7]);

            if (index >= 0 && index < lines.Length)
            {
                SetText(lines[index]);
            }
        }

        public event EventHandler TextChanged;


        public override bool AcceptKeyboardInput => base.AcceptKeyboardInput && IsEditable;
        public bool AllowTAB { get; set; }
        public bool NoSelection { get; set; }
        public bool NumbersOnly { get; set; }
        public bool AllowSelection { get; set; } = true;
        public bool IsPassword { get; set; }
        public ushort Hue { get; set; }
        public int Length => Text?.Length ?? 0;

        public int CaretIndex
        {
            get => _textEdit.CursorIndex;
            set
            {
                _textEdit.CursorIndex = value;

                UpdateCaretScreenPosition();
            }
        }

        public byte Font
        {
            get => _fontSettings.FontIndex;
            set
            {
                if (_fontSettings.FontIndex != value)
                {
                    _fontSettings.FontIndex = (byte)(value == 0xFF ? (Client.Version >= ClientVersion.CV_305D ? 1 : 0) : value);

                    UpdateCaretScreenPosition();
                }
            }
        }

        public bool Multiline
        {
            get => !_textEdit.SingleLine;
            set => _textEdit.SingleLine = !value;
        }

        public int SelectionStart
        {
            get => _textEdit.SelectStart;
            set
            {
                if (AllowSelection)
                {
                    _textEdit.SelectStart = value;
                }
            }
        }

        public int SelectionEnd
        {
            get => _textEdit.SelectEnd;
            set
            {
                if (AllowSelection)
                {
                    _textEdit.SelectEnd = value;
                }
            }
        }

        public string Text
        {
            get => _text;
            set
            {
                if (_maxCharCount > 0)
                {
                    if (NumbersOnly)
                    {

                    }
                    if (value != null && value.Length > _maxCharCount)
                    {
                        value = value.Substring(0, _maxCharCount);
                    }
                }
                
                _text = value;

                if (!_is_writing)
                {
                    OnTextChanged();
                }
            }
        }




        public float GetWidth(int index)
        {
            float width = 0;
            if (!string.IsNullOrEmpty(_text) && index >= 0 && index < _text.Length && _text[index] != '\n')
            {
                width = UOFontRenderer.Shared.MeasureString(IsPassword ? "*".AsSpan() : _text.AsSpan(index, 1), _fontSettings, 1f).X;               
            }

            return width;
        }

        // https://github.com/rds1983/Myra/blob/5dde5e3229cb6752a264abb4b92edfa671b507ca/src/Myra/Graphics2D/UI/TextField.cs
        public TextEditRow LayoutRow(int startIndex)
        {
            TextEditRow r = new TextEditRow();

            if (!string.IsNullOrEmpty(_text))
            {
                int? lastBreakPosition = null;
                Vector2? lastBreakMeasure = null;

                var fontHeight = UOFontRenderer.Shared.GetFontHeight(_fontSettings);

                for (int i = startIndex; i < _text.Length; ++i)
                {
                    var c = _text[i];
                    var span = IsPassword ? "*".AsSpan() : _text.AsSpan(startIndex, (i - startIndex) + 1);

                    var size = UOFontRenderer.Shared.MeasureString(span, _fontSettings, 1f);

                    if (IsPassword)
                    {
                        size.X *= (i - startIndex) + 1;
                    }

                    if (char.IsWhiteSpace(c))
                    {
                        lastBreakPosition = i + 1;
                        lastBreakMeasure = size;
                    }

                    if ((_maxWidth > 0.0f && size.X > _maxWidth) || c == '\n')
                    {
                        if (lastBreakPosition != null)
                        {
                            r.num_chars = lastBreakPosition.Value - startIndex;
                        }

                        if (lastBreakMeasure != null)
                        {
                            r.x1 = lastBreakMeasure.Value.X;
                            r.ymax = fontHeight;
                            r.baseline_y_delta = fontHeight;
                        }

                        break;
                    }

                    ++r.num_chars;
                    r.x1 = size.X;
                    r.ymax = fontHeight;
                    r.baseline_y_delta = fontHeight;
                }
            }

            int sx = ScreenCoordinateX;
            int sy = ScreenCoordinateY;

            r.x0 += sx;
            r.x1 += sx;
            r.ymin += sy;
            r.ymax += sy;

            return r;
        }

       


        

        private void UpdateCaretScreenPosition()
        {
            _caretScreenPosition = Vector2.Zero;
            var fontHeight = UOFontRenderer.Shared.GetFontHeight(_fontSettings);

            for (int i = 0, count = Math.Min(_textEdit.CursorIndex, _text.Length); i < count; ++i)
            {
                var size = _text[i] == '\n' ? Vector2.Zero : UOFontRenderer.Shared.MeasureString(IsPassword ? "*".AsSpan() : _text.AsSpan(i, 1), _fontSettings, 1f);

                _caretScreenPosition.X += size.X;

                if ((_maxWidth > 0.0f && _caretScreenPosition.X > _maxWidth) || _text[i] == '\n')
                {
                    _caretScreenPosition.X = size.X;
                    _caretScreenPosition.Y += fontHeight;
                }
            }
        }

        private ControlKeys ApplyShiftIfNecessary(ControlKeys k)
        {
            if (Keyboard.Shift && !NoSelection)
            {
                k |= ControlKeys.Shift;
            }

            return k;
        }

        private bool IsMaxCharReached(int count)
        {
            return _maxCharCount >= 0 && Length + count >= _maxCharCount;
        }



        public void SetText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                ClearText();
            }
            else
            {
                if (_maxCharCount > 0)
                {
                    if (NumbersOnly)
                    {
                        // TODO ?
                    }
                    else if (text.Length > _maxCharCount)
                    {
                        text = text.Substring(0, _maxCharCount);
                    }
                }

                _textEdit.ClearState(!Multiline);
             
                Text = text;

                _textEdit.CursorIndex = Length;

                if (!_is_writing)
                {
                    OnTextChanged();
                }
            }
        }

        public void ClearText()
        {
            if (Length != 0)
            {
                SelectionStart = 0;
                SelectionEnd = 0;
                _textEdit.Delete(0, Length);

                if (!_is_writing)
                {
                    OnTextChanged();
                }
            }
        }

        public void AppendText(string text) => _textEdit.Paste(text);
      
        public void SelectAll()
        {
            if (AllowSelection)
            {
                _textEdit.SelectStart = 0;
                _textEdit.SelectEnd = Length;
            }
        }


        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (batcher.ClipBegin(x - 1, y - 1, Width + 2, Height + 2))
            {
                base.Draw(batcher, x, y);
                DrawSelection(batcher, x, y);

                Vector3 hueVec = new Vector3();
                ShaderHueTranslator.GetHueVector(ref hueVec, Hue);

                var fontHeight = UOFontRenderer.Shared.GetFontHeight(_fontSettings);
                var offY = 0.0f;
                var width = 0.0f;
                Vector2 position = new Vector2(x, y);

                for (int i = 0; i < _text.Length; ++i)
                {
                    var span = IsPassword ? "*".AsSpan() : _text.AsSpan(i, 1);

                    var size = _text[i] == '\n' ? Vector2.Zero : UOFontRenderer.Shared.MeasureString(span, _fontSettings, 1f);
                    width += size.X;

                    UOFontRenderer.Shared.Draw
                    (
                       batcher,
                       span,
                       position,
                       1f,
                       _fontSettings,
                       hueVec,
                       false
                    );

                    position.X += size.X;

                    if ((_maxWidth > 0.0f && width + size.X > _maxWidth) || _text[i] == '\n')
                    {
                        position.X = x;
                        position.Y += fontHeight;
                        offY += fontHeight;
                        width = 0;
                    }
                }

                DrawCaret(batcher, x, y);

                batcher.ClipEnd();
            }
            
            return true;
        }

        private protected void DrawSelection(UltimaBatcher2D batcher, int x, int y)
        {
            if (!AllowSelection)
            {
                return;
            }

            ResetHueVector();
            HueVector.Z = 0.5f;

            int selectStart = Math.Min(_textEdit.SelectStart, _textEdit.SelectEnd);
            int selectEnd = Math.Max(_textEdit.SelectStart, _textEdit.SelectEnd);
          
            if (selectStart < selectEnd)
            {
                var texture = SolidColorTextureCache.GetTexture(SELECTION_COLOR);
                var fontHeight = UOFontRenderer.Shared.GetFontHeight(_fontSettings);


                Vector2 startPosition = Vector2.Zero;
                int i = 0;

                for (; i < selectStart; ++i)
                {
                    var span = IsPassword ? "*".AsSpan() : _text.AsSpan(i, 1);
                    var size = _text[i] == '\n' ? Vector2.Zero : UOFontRenderer.Shared.MeasureString(span, _fontSettings, 1f);
                
                    startPosition.X += size.X;

                    if ((_maxWidth > 0.0f && startPosition.X > _maxWidth) || _text[i] == '\n')
                    {
                        startPosition.X = 0;
                        startPosition.Y += fontHeight;
                    }
                }

                while (i < selectEnd)
                {
                    var span = IsPassword ? "*".AsSpan() : _text.AsSpan(selectStart, (i - selectStart) + 1);
                    var size = UOFontRenderer.Shared.MeasureString(span, _fontSettings, 1f);

                    if (IsPassword)
                    {
                        size.X *= (i - selectStart) + 1;
                    }

                    if ((_maxWidth > 0.0f && size.X > _maxWidth) || _text[i] == '\n')
                    {
                        batcher.Draw
                        (
                            texture,
                            new Rectangle(x + (int)startPosition.X, y + (int)startPosition.Y, (int)(size.X), (int)fontHeight),
                            HueVector
                        );

                        startPosition.X = 0;
                        startPosition.Y += fontHeight;
                        selectStart = i;
                    }

                    ++i;
                }

                if (selectStart < selectEnd)
                {
                    var sizeEnd = UOFontRenderer.Shared.MeasureString(IsPassword ? "*".AsSpan() : _text.AsSpan(selectStart, Math.Min(_text.Length, selectEnd - selectStart)), _fontSettings, 1f);

                    if (IsPassword)
                    {
                        sizeEnd.X *= Math.Min(_text.Length, selectEnd - selectStart);
                    }

                    batcher.Draw
                    (
                        texture,
                        new Rectangle(x + (int)startPosition.X, y + (int)startPosition.Y, (int)sizeEnd.X, (int)fontHeight),
                        HueVector
                    );
                }
            }

            ResetHueVector();
        }

        protected virtual void DrawCaret(UltimaBatcher2D batcher, int x, int y)
        {
            if (HasKeyboardFocus)
            {
                if (Time.Ticks - _caretBlinkTime >= CARET_BLINK_TIME)
                {
                    _cursorOn = !_cursorOn;
                    _caretBlinkTime = Time.Ticks;
                }

                if (_cursorOn)
                {
                    if (_textEdit.InsertMode && _textEdit.CursorIndex < _text.Length)
                    {
                        var size = UOFontRenderer.Shared.MeasureString(IsPassword ? "*".AsSpan() : _text.AsSpan(_textEdit.CursorIndex, 1), _fontSettings, 1f);

                        batcher.Draw
                        (
                            SolidColorTextureCache.GetTexture(Color.Gray * 0.5f),
                            new Rectangle(x + (int) _caretScreenPosition.X, y + (int)_caretScreenPosition.Y, (int)size.X, (int)size.Y),
                            Vector3.Zero
                        );
                    }
                    else
                    {
                        var fontHeight = UOFontRenderer.Shared.GetFontHeight(_fontSettings);

                        batcher.Draw
                        (
                            SolidColorTextureCache.GetTexture(Color.White),
                            new Rectangle(x + (int)_caretScreenPosition.X, y + (int)_caretScreenPosition.Y, 1, (int)fontHeight),
                            ShaderHueTranslator.GetHueVector(Hue)
                        );
                    } 
                }               
            }
        }

        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left && IsEditable)
            {
                if (!NoSelection)
                {
                    _leftWasDown = true;
                }

                _textEdit.Click(Mouse.Position.X, Mouse.Position.Y);
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
            {
                return;
            }

            _textEdit.Drag(Mouse.Position.X, Mouse.Position.Y);
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (!NoSelection && CaretIndex < Text.Length && CaretIndex >= 0 && !char.IsWhiteSpace(Text[CaretIndex]))
            {
                int idx = CaretIndex;

                if (idx - 1 >= 0 && char.IsWhiteSpace(Text[idx - 1]))
                {
                    ++idx;
                }

                SelectionStart = _textEdit.MoveToPreviousWord(idx);
                SelectionEnd = _textEdit.MoveToNextWord(idx);

                if (SelectionEnd < Text.Length)
                {
                    --SelectionEnd;
                }

                return true;
            }

            return base.OnMouseDoubleClick(x, y, button);
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

                case SDL.SDL_Keycode.SDLK_a when Keyboard.Ctrl && !NoSelection:
                    SelectAll();

                    break;

                case SDL.SDL_Keycode.SDLK_ESCAPE:
                    SelectionStart = 0;
                    SelectionEnd = 0;

                    break;

                case SDL.SDL_Keycode.SDLK_INSERT when IsEditable:
                    stb_key = ControlKeys.InsertMode;

                    break;

                case SDL.SDL_Keycode.SDLK_c when Keyboard.Ctrl && !NoSelection:
                    int selectStart = Math.Min(_textEdit.SelectStart, _textEdit.SelectEnd);
                    int selectEnd = Math.Max(_textEdit.SelectStart, _textEdit.SelectEnd);

                    if (selectStart < selectEnd && selectStart >= 0 && selectEnd - selectStart <= Text.Length)
                    {
                        SDL.SDL_SetClipboardText(Text.Substring(selectStart, selectEnd - selectStart));
                    }

                    break;

                case SDL.SDL_Keycode.SDLK_x when Keyboard.Ctrl && !NoSelection:
                    selectStart = Math.Min(_textEdit.SelectStart, _textEdit.SelectEnd);
                    selectEnd = Math.Max(_textEdit.SelectStart, _textEdit.SelectEnd);

                    if (selectStart < selectEnd && selectStart >= 0 && selectEnd - selectStart <= Text.Length)
                    {
                        SDL.SDL_SetClipboardText(Text.Substring(selectStart, selectEnd - selectStart));

                        if (IsEditable)
                        {
                            _textEdit.Cut();
                        }
                    }

                    break;

                case SDL.SDL_Keycode.SDLK_v when Keyboard.Ctrl && IsEditable:
                    OnTextInput(StringHelper.GetClipboardText(Multiline));

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
                        if (!NoSelection)
                        {
                            stb_key = ControlKeys.Shift | ControlKeys.WordLeft;
                        }
                    }
                    else if (Keyboard.Shift)
                    {
                        if (!NoSelection)
                        {
                            stb_key = ControlKeys.Shift | ControlKeys.Left;
                        }
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
                        if (!NoSelection)
                        {
                            stb_key = ControlKeys.Shift | ControlKeys.WordRight;
                        }
                    }
                    else if (Keyboard.Shift)
                    {
                        if (!NoSelection)
                        {
                            stb_key = ControlKeys.Shift | ControlKeys.Right;
                        }
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
                        if (!NoSelection)
                        {
                            stb_key = ControlKeys.Shift | ControlKeys.TextStart;
                        }
                    }
                    else if (Keyboard.Shift)
                    {
                        if (!NoSelection)
                        {
                            stb_key = ControlKeys.Shift | ControlKeys.LineStart;
                        }
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
                        if (!NoSelection)
                        {
                            stb_key = ControlKeys.Shift | ControlKeys.TextEnd;
                        }
                    }
                    else if (Keyboard.Shift)
                    {
                        if (!NoSelection)
                        {
                            stb_key = ControlKeys.Shift | ControlKeys.LineEnd;
                        }
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

                case SDL.SDL_Keycode.SDLK_KP_ENTER:
                case SDL.SDL_Keycode.SDLK_RETURN:
                    if (IsEditable)
                    {
                        if (Multiline)
                        {
                            if (!_fromServer && !IsMaxCharReached(0))
                            {
                                OnTextInput("\n");
                            }
                        }
                        else
                        {
                            Parent?.OnKeyboardReturn(0, Text);

                            if (UIManager.SystemChat != null && UIManager.SystemChat.TextBoxControl != null && IsFocused)
                            {
                                if (!IsFromServer || !UIManager.SystemChat.TextBoxControl.IsVisible)
                                {
                                    OnFocusLost();
                                    OnFocusEnter();
                                }
                                else if (UIManager.KeyboardFocusControl == null || UIManager.KeyboardFocusControl != UIManager.SystemChat.TextBoxControl)
                                {
                                    UIManager.SystemChat.TextBoxControl.SetKeyboardFocus();
                                }
                            }
                        }
                    }

                    break;
            }

            if (stb_key != null)
            {
                _textEdit.Key(stb_key.Value);
            }

            if (update_caret)
            {
                UpdateCaretScreenPosition();
            }

            base.OnKeyDown(key, mod);
        }

        protected override void OnTextInput(string text)
        {
            if (text == null || !IsEditable)
            {
                return;
            }

            _is_writing = true;

            if (SelectionStart != SelectionEnd)
            {
                _textEdit.DeleteSelection();
            }

            int count;

            if (_maxCharCount > 0)
            {
                int remains = _maxCharCount - Length;

                if (remains <= 0)
                {
                    _is_writing = false;

                    return;
                }

                count = Math.Min(remains, text.Length);

                if (remains < text.Length && count > 0)
                {
                    text = text.Substring(0, count);
                }
            }
            else
            {
                count = text.Length;
            }

            if (count > 0)
            {
                if (NumbersOnly)
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (!char.IsNumber(text[i]))
                        {
                            _is_writing = false;

                            return;
                        }
                    }

                    if (_maxCharCount > 0 && int.TryParse(_textEdit.text + text, out int val))
                    {
                        if (val > _maxCharCount)
                        {
                            _is_writing = false;
                            SetText(_maxCharCount.ToString());

                            return;
                        }
                    }
                }
                
                if (count > 1)
                {
                    _textEdit.Paste(text);
                }
                else
                {
                    _textEdit.InputChar(text[0]);
                }

                OnTextChanged();
            }

            _is_writing = false;
        }

        protected virtual void OnTextChanged()
        {
            TextChanged?.Raise(this);

            UpdateCaretScreenPosition();
        }

        internal override void OnFocusEnter()
        {
            base.OnFocusEnter();
            CaretIndex = Text?.Length ?? 0;
        }

        internal override void OnFocusLost()
        {
            if (_textEdit != null)
            {
                _textEdit.SelectStart = _textEdit.SelectEnd = 0;
            }

            base.OnFocusLost();
        }
    }
}