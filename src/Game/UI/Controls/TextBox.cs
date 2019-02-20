#region license
//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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

using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;

using Microsoft.Xna.Framework;

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
        }
        public TextBox(byte font, int maxcharlength = -1, int maxWidth = 0, int width = 0, bool isunicode = true, FontStyle style = FontStyle.None, ushort hue = 0)
        {
            TxEntry = new TextEntry(font, maxcharlength, maxWidth, width, isunicode, style, hue);
            base.AcceptKeyboardInput = true;
            base.AcceptMouseInput = true;
            IsEditable = true;
        }

        public TextBox(string[] parts, string[] lines) : this(1, parts[0] == "textentrylimited" ? int.Parse(parts[8]) : -1, 0, int.Parse(parts[3]), style: FontStyle.BlackBorder, hue: Hue.Parse(parts[5]))
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            Width = int.Parse(parts[3]);
            Height = int.Parse(parts[4]);
            LocalSerial = Serial.Parse(parts[6]);
            SetText(lines[int.Parse(parts[7])]);
        }


        public event EventHandler TextChanged;


        public TextEntry TxEntry { get; private set; }

        public bool IsChanged => TxEntry.IsChanged;

        public Hue Hue
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
            get => TxEntry.NumericOnly;
            set => TxEntry.NumericOnly = value;
        }

        public bool SafeCharactersOnly
        {
            get => TxEntry.SafeCharactersOnly;
            set => TxEntry.SafeCharactersOnly = value;
        }

        public uint AllowValidateRules
        {
            get => TxEntry.AllowValidateRules;
            set => TxEntry.AllowValidateRules = value;
        }

        public bool ReplaceDefaultTextOnFirstKeyPress { get; set; }

        public string Text { get => TxEntry.Text; set => SetText(value); }

        //public override bool AcceptMouseInput => base.AcceptMouseInput && IsEditable;

        public void SetText(string text, bool append = false)
        {
            int oldidx = TxEntry.CaretIndex;
            TxEntry.SetText(text);
            TxEntry.SetCaretPosition(oldidx + text.Length);
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (IsDisposed)
                return;

            if(Height != TxEntry.Height)
                Height = TxEntry.Height;

            if (TxEntry.IsChanged)
            {
                TxEntry.UpdateCaretPosition();
                TextChanged.Raise(this);
            }
            base.Update(totalMS, frameMS);
        }

        public override bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
        {
            TxEntry.RenderText.Draw(batcher, new Point(position.X + TxEntry.Offset, position.Y));

            if (IsEditable)
            {
                if (HasKeyboardFocus)
                    TxEntry.RenderCaret.Draw(batcher, new Point(position.X + TxEntry.Offset + TxEntry.CaretPosition.X, position.Y + TxEntry.CaretPosition.Y));
            }

            return base.Draw(batcher, position, hue);
        }

        protected override void OnTextInput(string c)
        {
			if (ReplaceDefaultTextOnFirstKeyPress)
            {
                TxEntry.Clear();
				ReplaceDefaultTextOnFirstKeyPress = false;
            }
            TxEntry.InsertString(c);
        }

        protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            string s;

            if (Input.Keyboard.IsModPressed(mod, SDL.SDL_Keymod.KMOD_CTRL) && key == SDL.SDL_Keycode.SDLK_v)//paste
            {
                if (SDL.SDL_HasClipboardText() == SDL.SDL_bool.SDL_FALSE)
                    return;

                s = SDL.SDL_GetClipboardText();
                if(!string.IsNullOrEmpty(s))
                {
                    TxEntry.InsertString(s.Replace("\r", string.Empty).Replace('\n', ' '));//we remove every carriage-return (windows) and every newline (all systems) and put a blank space instead
                    return;
                }
            }
            else switch (key)
            {
                case SDL.SDL_Keycode.SDLK_KP_ENTER:
                case SDL.SDL_Keycode.SDLK_RETURN:
                        s = TxEntry.Text;
                       Parent?.OnKeyboardReturn(0, s);
                    break;
                case SDL.SDL_Keycode.SDLK_BACKSPACE:
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


            base.OnKeyDown(key, mod);
        }

        protected override void OnMouseClick(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left)
            {
                TxEntry.OnMouseClick(x, y);
            } 
        }

        public override void Dispose()
        {
            TxEntry?.Dispose();
            TxEntry = null;
            base.Dispose();
        }
    }
}