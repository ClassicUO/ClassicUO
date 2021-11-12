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
        protected static readonly Color SELECTION_COLOR = new Color() { PackedValue = 0x80a06020 };
        private readonly FontStyle _fontStyle;

        private readonly int _maxCharCount = -1;

        private FontSettings _fontSettings;
        private ushort _hue;
        private string _text = string.Empty;
        private Vector2 _textSize;
        private float _maxWidth;


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

            Stb = new TextEdit(this);
            Stb.SingleLine = true;

            if (maxWidth > 0)
            {
                style |= FontStyle.CropTexture;
            }

            _fontStyle = style;

            if ((style & (FontStyle.Fixed | FontStyle.Cropped)) != 0 && maxWidth <= 0)
            {
                Debug.Assert((style & (FontStyle.Fixed | FontStyle.Cropped)) != 0 && maxWidth <= 0);
            }

            // stb_textedit will handle part of these tag
            style &= ~( /*FontStyle.Fixed | */FontStyle.Cropped | FontStyle.CropTexture);


            _hue = hue;
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

        protected TextEdit Stb { get; }


        public override bool AcceptKeyboardInput => base.AcceptKeyboardInput && IsEditable;

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

        public bool AllowTAB { get; set; }
        public bool NoSelection { get; set; }

        public int CaretIndex
        {
            get => Stb.CursorIndex;
            set
            {
                Stb.CursorIndex = value;
                UpdateCaretScreenPosition();
            }
        }

        public bool Multiline
        {
            get => !Stb.SingleLine;
            set => Stb.SingleLine = !value;
        }

        public bool NumbersOnly { get; set; }

        public int SelectionStart
        {
            get => Stb.SelectStart;
            set
            {
                if (AllowSelection)
                {
                    Stb.SelectStart = value;
                }
            }
        }

        public int SelectionEnd
        {
            get => Stb.SelectEnd;
            set
            {
                if (AllowSelection)
                {
                    Stb.SelectEnd = value;
                }
            }
        }

        public bool AllowSelection { get; set; } = true;

        public bool IsUnicode => _fontSettings.IsUnicode;

        public ushort Hue
        {
            get => _hue;
            set => _hue = value;
        }

        internal int TotalHeight
        {
            get
            { 
                return (int) _textSize.Y;
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
                
                //Sanitize(ref value);

                _text = value;
                _textSize = UOFontRenderer.Shared.MeasureString(_text.AsSpan(), _fontSettings, 1f, _maxWidth);

                if (!_is_writing)
                {
                    OnTextChanged();
                }
            }
        }

        public int Length => Text?.Length ?? 0;

        public float GetWidth(int index)
        {
            float width = 0;
            if (!string.IsNullOrEmpty(_text))
            {
                width = UOFontRenderer.Shared.MeasureString(_text.AsSpan(index, 1), _fontSettings, 1f, _maxWidth).X;               
            }

            return width;
        }

        public TextEditRow LayoutRow(int startIndex)
        {
            TextEditRow r = new TextEditRow();

            if (!string.IsNullOrEmpty(_text))
            {
                r.x0 = 0;
                r.x1 = _textSize.X;
                r.num_chars = _text.Length - 1; //TODO: need to be fixed

                r.baseline_y_delta = r.ymax = UOFontRenderer.Shared.GetFontHeight(_fontSettings);
            }

            int sx = ScreenCoordinateX;
            int sy = ScreenCoordinateY;

            r.x0 += sx;
            r.x1 += sx;
            r.ymin += sy;
            r.ymax += sy;

            return r;
        }

        protected Point _caretScreenPosition;
        protected bool _is_writing;
        protected bool _leftWasDown, _fromServer;

        public event EventHandler TextChanged;


        public void SelectAll()
        {
            if (AllowSelection)
            {
                Stb.SelectStart = 0;
                Stb.SelectEnd = Length;
            }
        }

        protected void UpdateCaretScreenPosition()
        {
            var cursorIndex = Math.Min(Stb.CursorIndex, Math.Max(0, _text.Length));

            var size = UOFontRenderer.Shared.MeasureString(_text.AsSpan(0, cursorIndex), _fontSettings, 1f);
            _caretScreenPosition.X = (int)size.X;
            _caretScreenPosition.Y = (int)size.Y;
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
            if (Stb != null)
            {
                Stb.SelectStart = Stb.SelectEnd = 0;
            }

            base.OnFocusLost();
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
                    int selectStart = Math.Min(Stb.SelectStart, Stb.SelectEnd);
                    int selectEnd = Math.Max(Stb.SelectStart, Stb.SelectEnd);

                    if (selectStart < selectEnd && selectStart >= 0 && selectEnd - selectStart <= Text.Length)
                    {
                        SDL.SDL_SetClipboardText(Text.Substring(selectStart, selectEnd - selectStart));
                    }

                    break;

                case SDL.SDL_Keycode.SDLK_x when Keyboard.Ctrl && !NoSelection:
                    selectStart = Math.Min(Stb.SelectStart, Stb.SelectEnd);
                    selectEnd = Math.Max(Stb.SelectStart, Stb.SelectEnd);

                    if (selectStart < selectEnd && selectStart >= 0 && selectEnd - selectStart <= Text.Length)
                    {
                        SDL.SDL_SetClipboardText(Text.Substring(selectStart, selectEnd - selectStart));

                        if (IsEditable)
                        {
                            Stb.Cut();
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
                Stb.Key(stb_key.Value);
            }

            if (update_caret)
            {
                UpdateCaretScreenPosition();
            }

            base.OnKeyDown(key, mod);
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

                Stb.ClearState(!Multiline);
                Text = text;

                Stb.CursorIndex = Length;

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
                Stb.Delete(0, Length);

                if (!_is_writing)
                {
                    OnTextChanged();
                }
            }
        }

        public void AppendText(string text)
        {
            Stb.Paste(text);
        }


        protected override void OnTextInput(string c)
        {
            if (c == null || !IsEditable)
            {
                return;
            }

            _is_writing = true;

            if (SelectionStart != SelectionEnd)
            {
                Stb.DeleteSelection();
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

                count = Math.Min(remains, c.Length);

                if (remains < c.Length && count > 0)
                {
                    c = c.Substring(0, count);
                }
            }
            else
            {
                count = c.Length;
            }

            if (count > 0)
            {
                if (NumbersOnly)
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (!char.IsNumber(c[i]))
                        {
                            _is_writing = false;

                            return;
                        }
                    }

                    if (_maxCharCount > 0 && int.TryParse(Stb.text + c, out int val))
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
                    Stb.Paste(c);
                    OnTextChanged();
                }
                else if (UOFontRenderer.Shared.MeasureString(c.AsSpan(0, 1), _fontSettings, 1f).X > 0.0f || c[0] == '\n' || c[0] == ' ')
                {
                    Stb.InputChar(c[0]);
                    OnTextChanged();
                }
            }

            _is_writing = false;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (batcher.ClipBegin(x - 1, y - 1, Width + 2, Height + 2))
            {
                base.Draw(batcher, x, y);
                DrawSelection(batcher, x, y);

                UOFontRenderer.Shared.Draw
                (
                    batcher,
                    _text.AsSpan(),
                    new Vector2(x, y),
                    1f,
                    _fontSettings,
                    _hue
                );

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

            int selectStart = Math.Min(Stb.SelectStart, Stb.SelectEnd);
            int selectEnd = Math.Max(Stb.SelectStart, Stb.SelectEnd);

            HueVector.Z = 0.5f;

            if (selectStart < selectEnd)
            {
                //MultilinesFontInfo info = _rendererText.GetInfo();

                int drawY = 1;
                int start = 0;

                //for (int i = 0; i < _text.Length; ++i)
                //{
                //    if (selectStart >= i)
                //    {

                //    }
                //}

                //int diffX = _rendererText.Align != TEXT_ALIGN_TYPE.TS_LEFT ? _rendererText.GetCaretPosition(0).X - 1 : 0;

                //while (info != null && selectStart < selectEnd)
                //{
                //    // ok we are inside the selection
                //    if (selectStart >= start && selectStart < start + info.CharCount)
                //    {
                //        int startSelectionIndex = selectStart - start;

                //        // calculate offset x
                //        int drawX = 0;

                //        for (int i = 0; i < startSelectionIndex; i++)
                //        {
                //            drawX += _rendererText.GetCharWidth(info.Data[i].Item);
                //        }

                //        // selection is gone. Bye bye
                //        if (selectEnd >= start && selectEnd < start + info.CharCount)
                //        {
                //            int count = selectEnd - selectStart;

                //            int endX = 0;

                //            // calculate width 
                //            for (int k = 0; k < count; k++)
                //            {
                //                endX += _rendererText.GetCharWidth(info.Data[startSelectionIndex + k].Item);
                //            }

                //            batcher.Draw
                //            (
                //                SolidColorTextureCache.GetTexture(SELECTION_COLOR),
                //                new Rectangle
                //                (
                //                    x + drawX + diffX,
                //                    y + drawY,
                //                    endX,
                //                    info.MaxHeight + 1
                //                ),
                //                HueVector
                //            );

                //            break;
                //        }


                //        // do the whole line
                //        batcher.Draw
                //        (
                //            SolidColorTextureCache.GetTexture(SELECTION_COLOR),
                //            new Rectangle
                //            (
                //                x + drawX + diffX,
                //                y + drawY,
                //                info.Width - drawX,
                //                info.MaxHeight + 1
                //            ),
                //            HueVector
                //        );

                //        // first selection is gone. M
                //        selectStart = start + info.CharCount;
                //    }

                //    start += info.CharCount;
                //    drawY += info.MaxHeight;
                //    info = info.Next;
                //}
            }


            ResetHueVector();
        }

        protected virtual void DrawCaret(UltimaBatcher2D batcher, int x, int y)
        {
            if (HasKeyboardFocus)
            {
                UOFontRenderer.Shared.Draw
                (
                    batcher,
                    "_".AsSpan(),
                    new Vector2(x + _caretScreenPosition.X, y + _caretScreenPosition.Y),
                    1f,
                    _fontSettings,
                    _hue
                );
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

                Stb.Click(Mouse.Position.X, Mouse.Position.Y);
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

            Stb.Drag(Mouse.Position.X, Mouse.Position.Y);
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

                SelectionStart = Stb.MoveToPreviousWord(idx);
                SelectionEnd = Stb.MoveToNextWord(idx);

                if (SelectionEnd < Text.Length)
                {
                    --SelectionEnd;
                }

                return true;
            }

            return base.OnMouseDoubleClick(x, y, button);
        }
    }
}