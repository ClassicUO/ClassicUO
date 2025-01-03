// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.Diagnostics;
using ClassicUO.Game.Managers;
using ClassicUO.Input;
using ClassicUO.Assets;
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

            _rendererText = RenderedText.Create
            (
                string.Empty,
                hue,
                font,
                isunicode,
                style,
                align,
                maxWidth
            );

            _rendererCaret = RenderedText.Create
            (
                "_",
                hue,
                font,
                isunicode,
                (style & FontStyle.BlackBorder) != 0 ? FontStyle.BlackBorder : FontStyle.None,
                align
            );

            Height = _rendererCaret.Height;
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
            Width = _rendererText.MaxWidth; //int.Parse(parts[3]);
            Height = _rendererText.MaxHeight = int.Parse(parts[4]);
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
            get => _rendererText.Font;
            set
            {
                if (_rendererText.Font != value)
                {
                    _rendererText.Font = value;
                    _rendererText.CreateTexture();
                    _rendererCaret.Font = value;
                    _rendererCaret.CreateTexture();

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

        public bool IsUnicode => _rendererText.IsUnicode;

        public ushort Hue
        {
            get => _rendererText.Hue;
            set
            {
                if (_rendererText.Hue != value)
                {
                    _rendererText.Hue = value;
                    _rendererCaret.Hue = value;

                    _rendererText.CreateTexture();
                    _rendererCaret.CreateTexture();
                }
            }
        }

        internal int TotalHeight
        {
            get
            {
                int h = 20;
                MultilinesFontInfo info = GetInfo();

                while (info != null)
                {
                    h += info.MaxHeight;
                    info = info.Next;
                }

                return h;
            }
        }

        public string Text
        {
            get => _rendererText.Text;

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

                _rendererText.Text = value;

                if (!_is_writing)
                {
                    OnTextChanged();
                }
            }
        }

        public int Length => Text?.Length ?? 0;

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

        protected Point _caretScreenPosition;
        protected bool _is_writing;
        protected bool _leftWasDown, _fromServer;
        protected RenderedText _rendererText, _rendererCaret;

        public event EventHandler TextChanged;

        public MultilinesFontInfo CalculateFontInfo(string text, bool countret = true)
        {
            if (IsUnicode)
            {
                return Client.Game.UO.FileManager.Fonts.GetInfoUnicode
                (
                    _rendererText.Font,
                    text,
                    text.Length,
                    _rendererText.Align,
                    (ushort) _rendererText.FontStyle,
                    _rendererText.MaxWidth,
                    countret
                );
            }

            return Client.Game.UO.FileManager.Fonts.GetInfoASCII
            (
                _rendererText.Font,
                text,
                text.Length,
                _rendererText.Align,
                (ushort) _rendererText.FontStyle,
                _rendererText.MaxWidth,
                countret
            );
        }

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
            _caretScreenPosition = _rendererText.GetCaretPosition(Stb.CursorIndex);
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

        private void Sanitize(ref string text)
        {
            if ((_fontStyle & FontStyle.Fixed) != 0 || (_fontStyle & FontStyle.Cropped) != 0 || (_fontStyle & FontStyle.CropTexture) != 0)
            {
                if (_rendererText.MaxWidth == 0)
                {
                    Log.Warn("maxwidth must be setted.");

                    return;
                }

                if (string.IsNullOrEmpty(text))
                {
                    return;
                }


                int realWidth = _rendererText.IsUnicode ? Client.Game.UO.FileManager.Fonts.GetWidthUnicode(_rendererText.Font, text) : Client.Game.UO.FileManager.Fonts.GetWidthASCII(_rendererText.Font, text);

                if (realWidth > _rendererText.MaxWidth)
                {
                    if ((_fontStyle & FontStyle.Fixed) != 0)
                    {
                        text = Text;
                        Stb.CursorIndex = Math.Max(0, text.Length - 1);

                        return;
                    }

                    //MultilinesFontInfo info = _rendererText.IsUnicode
                    //                              ? Client.Game.UO.FileManager.Fonts.GetInfoUnicode(
                    //                                                                    _rendererText.Font,
                    //                                                                    text,
                    //                                                                    text.Length,
                    //                                                                    _rendererText.Align,
                    //                                                                    (ushort) _rendererText.FontStyle,
                    //                                                                    realWidth
                    //                                                                   )
                    //                              : Client.Game.UO.FileManager.Fonts.GetInfoASCII(
                    //                                                                  _rendererText.Font,
                    //                                                                  text,
                    //                                                                  text.Length,
                    //                                                                  _rendererText.Align,
                    //                                                                  (ushort) _rendererText.FontStyle,
                    //                                                                  realWidth
                    //                                                                 );


                    if ((_fontStyle & FontStyle.CropTexture) != 0)
                    {
                        //string sb = text;
                        //int total_height = 0;
                        //int start = 0;

                        //while (info != null)
                        //{
                        //    total_height += info.MaxHeight;

                        //    if (total_height >= Height)
                        //    {
                        //        if (Text != null && Text.Length <= text.Length)
                        //            text = Text;

                        //        _stb.CursorIndex = Math.Max(0, text.Length - 1);
                        //        return;
                        //    }

                        //    uint count = info.Data.Count;

                        //    //if (_stb.CursorIndex >= start && _stb.CursorIndex <= start + info.CharCount)
                        //    {
                        //        int pixel_width = 0;

                        //        for (int i = 0; i < count; i++)
                        //        {
                        //            pixel_width += _rendererText.GetCharWidth(info.Data[i].Item);

                        //            if (pixel_width >= _rendererText.MaxWidth)
                        //            {
                        //                sb = sb.Insert(start + i, "\n");
                        //                _stb.CursorIndex = start + i + 1;
                        //                pixel_width = 0;
                        //            }
                        //        }
                        //    }

                        //    start += (int) count;
                        //    info = info.Next;
                        //}

                        //text = sb.ToString();
                    }

                    if ((_fontStyle & FontStyle.Cropped) != 0)
                    {
                    }
                }
            }
        }


        protected virtual void OnTextChanged()
        {
            TextChanged?.Raise(this);

            UpdateCaretScreenPosition();
        }

        protected MultilinesFontInfo GetInfo()
        {
            return _rendererText.GetInfo();
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

                    // if (int.TryParse(Stb.text + c, out int val))
                    // {
                    //     if (val > _maxCharCount)
                    //     {
                    //         _is_writing = false;
                    //         SetText(_maxCharCount.ToString());
                    //
                    //         return;
                    //     }
                    // }
                }


                if (count > 1)
                {
                    Stb.Paste(c);
                    OnTextChanged();
                }
                else if (_rendererText.GetCharWidth(c[0]) > 0 || c[0] == '\n')
                {
                    Stb.InputChar(c[0]);
                    OnTextChanged();
                }
            }

            _is_writing = false;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (batcher.ClipBegin(x, y, Width, Height))
            {
                base.Draw(batcher, x, y);
                DrawSelection(batcher, x, y);
                _rendererText.Draw(batcher, x, y);
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

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0, false, 0.5f);

            int selectStart = Math.Min(Stb.SelectStart, Stb.SelectEnd);
            int selectEnd = Math.Max(Stb.SelectStart, Stb.SelectEnd);

            if (selectStart < selectEnd)
            {
                MultilinesFontInfo info = _rendererText.GetInfo();

                int drawY = 1;
                int start = 0;
                int diffX = _rendererText.Align != TEXT_ALIGN_TYPE.TS_LEFT ? _rendererText.GetCaretPosition(0).X - 1 : 0;

                while (info != null && selectStart < selectEnd)
                {
                    // ok we are inside the selection
                    if (selectStart >= start && selectStart < start + info.CharCount)
                    {
                        int startSelectionIndex = selectStart - start;

                        // calculate offset x
                        int drawX = 0;

                        for (int i = 0; i < startSelectionIndex; i++)
                        {
                            drawX += _rendererText.GetCharWidth(info.Data[i].Item);
                        }

                        // selection is gone. Bye bye
                        if (selectEnd >= start && selectEnd < start + info.CharCount)
                        {
                            int count = selectEnd - selectStart;

                            int endX = 0;

                            // calculate width
                            for (int k = 0; k < count; k++)
                            {
                                endX += _rendererText.GetCharWidth(info.Data[startSelectionIndex + k].Item);
                            }

                            batcher.Draw
                            (
                                SolidColorTextureCache.GetTexture(SELECTION_COLOR),
                                new Rectangle
                                (
                                    x + drawX + diffX,
                                    y + drawY,
                                    endX,
                                    info.MaxHeight + 1
                                ),
                                hueVector
                            );

                            break;
                        }


                        // do the whole line
                        batcher.Draw
                        (
                            SolidColorTextureCache.GetTexture(SELECTION_COLOR),
                            new Rectangle
                            (
                                x + drawX + diffX,
                                y + drawY,
                                info.Width - drawX,
                                info.MaxHeight + 1
                            ),
                            hueVector
                        );

                        // first selection is gone. M
                        selectStart = start + info.CharCount;
                    }

                    start += info.CharCount;
                    drawY += info.MaxHeight;
                    info = info.Next;
                }
            }
        }

        protected virtual void DrawCaret(UltimaBatcher2D batcher, int x, int y)
        {
            if (HasKeyboardFocus)
            {
                _rendererCaret.Draw(batcher, x + _caretScreenPosition.X, y + _caretScreenPosition.Y);
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

        public override void Dispose()
        {
            _rendererText?.Destroy();
            _rendererCaret?.Destroy();

            base.Dispose();
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
