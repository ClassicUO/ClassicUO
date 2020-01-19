#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;

using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;

using SDL2;

namespace ClassicUO.Game.UI.Controls
{
    internal class TextBox : AbstractTextBox
    {
        public TextBox(TextEntry txentry, bool editable)
        {
            TxEntry = txentry;
            base.AcceptKeyboardInput = true;
            base.AcceptMouseInput = true;
            IsEditable = editable;

            Texture = TxEntry.RenderText.Texture;
        }

        public TextBox(byte font, int maxcharlength = -1, int maxWidth = 0, int width = 0, bool isunicode = true, FontStyle style = FontStyle.None, ushort hue = 0, TEXT_ALIGN_TYPE alig = 0)
        {
            TxEntry = new TextEntry(font, maxcharlength, maxWidth, width, isunicode, style, hue, alig);
            base.AcceptKeyboardInput = true;
            base.AcceptMouseInput = true;
            IsEditable = true;
            Unicode = isunicode;
            Font = font;

            Texture = TxEntry.RenderText.Texture;
        }

        public TextBox(List<string> parts, string[] lines) : this(1, parts[0] == "textentrylimited" ? int.Parse(parts[8]) : byte.MaxValue, 0, int.Parse(parts[3]), style: FontStyle.BlackBorder | FontStyle.CropTexture, hue: (ushort) (UInt16Converter.Parse(parts[5]) + 1))
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            Width = int.Parse(parts[3]);
            Height = int.Parse(parts[4]);
            LocalSerial = SerialHelper.Parse(parts[6]);
            TxEntry.SetHeight(Height);

            int index = int.Parse(parts[7]);

            if (index >= 0 && index < lines.Length)
                SetText(lines[index]);
        }

        public TextEntry TxEntry { get; private set; }

        public bool IsChanged => TxEntry.IsChanged;

        public ushort Hue
        {
            get => TxEntry.Hue;
            set => TxEntry.Hue = value;
        }

        public bool IsPassword
        {
            get => TxEntry.IsPassword;
            set => TxEntry.IsPassword = value;
        }

        public bool NumericOnly
        {
            set => TxEntry.NumericOnly = value;
        }

        public bool UNumericOnly
        {
            set => TxEntry.UNumericOnly = value;
        }

        public bool LettersOnly
        {
            set => TxEntry.LettersOnly = value;
        }

        public bool SafeCharactersOnly
        {
            set => TxEntry.SafeCharactersOnly = value;
        }

        public uint ValidationRules
        {
            get => TxEntry.ValidationRules;
            set => TxEntry.ValidationRules = value;
        }

        public bool ReplaceDefaultTextOnFirstKeyPress { get; set; }

        public override string Text
        {
            get => base.Text;
            set => SetText(value);
        }

        public bool AllowDeleteKey { get; set; } = true;

        public override AbstractEntry EntryValue => TxEntry;

        public event EventHandler TextChanged;

        //public override bool AcceptMouseInput => base.AcceptMouseInput && IsEditable;

        public void SetText(string text, bool append = false)
        {
            int oldidx = TxEntry.CaretIndex;

            if (text == null)
                text = string.Empty;

            TxEntry.SetText(text);
            TxEntry.SetCaretPosition(oldidx + text.Length);
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (IsDisposed || TxEntry == null)
                return;

            int h = Math.Max(Height, TxEntry.Height);

            if (Height != h)
                Height = h;

            if (TxEntry.IsChanged)
            {
                TxEntry.UpdateCaretPosition();
                TextChanged.Raise(this);
            }

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            TxEntry.RenderText.Draw(batcher, x + TxEntry.Offset, y);

            if (IsEditable && HasKeyboardFocus)
                TxEntry.RenderCaret.Draw(batcher, x + TxEntry.Offset + TxEntry.CaretPosition.X, y + TxEntry.CaretPosition.Y);

            return base.Draw(batcher, x, y);
        }

        protected override void OnTextInput(string c)
        {
            if (!IsEditable)
                return;

            if (ReplaceDefaultTextOnFirstKeyPress)
            {
                TxEntry.Clear();
                ReplaceDefaultTextOnFirstKeyPress = false;
            }

            TxEntry.InsertString(c);
        }

        protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            if (TxEntry != null)
            {
                string s;

                if (Keyboard.IsModPressed(mod, SDL.SDL_Keymod.KMOD_CTRL) && key == SDL.SDL_Keycode.SDLK_v) //paste
                {
                    if (!IsEditable)
                        return;

                    if (SDL.SDL_HasClipboardText() == SDL.SDL_bool.SDL_FALSE)
                        return;

                    s = SDL.SDL_GetClipboardText();

                    if (!string.IsNullOrEmpty(s))
                    {
                        TxEntry.InsertString(s.Replace("\r", string.Empty).Replace('\n', ' ')); //we remove every carriage-return (windows) and every newline (all systems) and put a blank space instead

                        return;
                    }
                }
                else if (!TxEntry.IsPassword && Keyboard.IsModPressed(mod, SDL.SDL_Keymod.KMOD_CTRL) && (key == SDL.SDL_Keycode.SDLK_x || key == SDL.SDL_Keycode.SDLK_c))
                {
                    if (!IsEditable)
                        key = SDL.SDL_Keycode.SDLK_c;
                    string txt = TxEntry.GetSelectionText(key == SDL.SDL_Keycode.SDLK_x);
                    SDL.SDL_SetClipboardText(txt);
                }
                else
                {
                    switch (key)
                    {
                        case SDL.SDL_Keycode.SDLK_KP_ENTER:
                        case SDL.SDL_Keycode.SDLK_RETURN:

                            if (IsEditable)
                            {
                                s = TxEntry.Text;
                                Parent?.OnKeyboardReturn(0, s);
                            }

                            break;

                        case SDL.SDL_Keycode.SDLK_BACKSPACE:

                            if (!IsEditable)
                                return;

                            if (!ReplaceDefaultTextOnFirstKeyPress)
                                TxEntry.RemoveChar(true);
                            else
                                ReplaceDefaultTextOnFirstKeyPress = false;

                            break;

                        case SDL.SDL_Keycode.SDLK_LEFT:
                            TxEntry.SeekCaretPosition(-1);

                            break;

                        case SDL.SDL_Keycode.SDLK_RIGHT:
                            TxEntry.SeekCaretPosition(1);

                            break;

                        case SDL.SDL_Keycode.SDLK_DELETE:

                            if (!AllowDeleteKey)
                                break;
                            if (!IsEditable)
                                return;

                            TxEntry.RemoveChar(false);

                            break;

                        case SDL.SDL_Keycode.SDLK_HOME:
                            TxEntry.SetCaretPosition(0);

                            break;

                        case SDL.SDL_Keycode.SDLK_END:
                            TxEntry.SetCaretPosition(Text.Length - 1);

                            break;

                        case SDL.SDL_Keycode.SDLK_TAB:
                            Parent.KeyboardTabToNextFocus(this);

                            break;
                    }
                }
            }

            base.OnKeyDown(key, mod);
        }
    }
}