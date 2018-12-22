#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
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
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

using SDL2;

namespace ClassicUO.Game.Gumps.Controls
{
    public class TextBox : Control
    {
        public enum PageCommand
        {
            Nothing = 0x00,
            GoForward = 0x01,
            GoBackward = 0x02,
            RemoveText = 0x04,
            PasteText = 0x08
        }
        public bool MultiLineInputAllowed { get; set; } = false;
        public TextEntry _entry { get; private set; }


        public TextBox(TextEntry txentry, bool editable)
        {
            _entry = txentry;
            base.AcceptKeyboardInput = true;
            base.AcceptMouseInput = true;
            IsEditable = editable;
        }
        public TextBox(byte font, int maxcharlength = -1, int maxWidth = 0, int width = 0, bool isunicode = true, FontStyle style = FontStyle.None, ushort hue = 0)
        {
            _entry = new TextEntry(font, maxcharlength, maxWidth, width, isunicode, style, hue);
            base.AcceptKeyboardInput = true;
            base.AcceptMouseInput = true;
            IsEditable = true;
        }

        public TextBox(string[] parts, string[] lines) : this(1, parts[0] == "textentrylimited" ? int.Parse(parts[8]) : -1, parts[0] == "textentrylimited" ? int.Parse(parts[3]) : 0, int.Parse(parts[3]), style: FontStyle.BlackBorder, hue: Hue.Parse(parts[5]))
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            Width = int.Parse(parts[3]);
            Height = int.Parse(parts[4]);
            LocalSerial = Serial.Parse(parts[6]);
            SetText(lines[int.Parse(parts[7])]);
        }

        public bool IsChanged => _entry.IsChanged;

        public Hue Hue
        {
            get => _entry.Hue;
            set => _entry.Hue = value;
        }

        public int MaxCharCount { get; set; }

        public bool IsPassword
        {
            get => _entry.IsPassword;
            set => _entry.IsPassword = value;
        }

        public bool NumericOnly
        {
            get => _entry.NumericOnly;
            set => _entry.NumericOnly = value;
        }

        public bool ReplaceDefaultTextOnFirstKeyPress { get; set; }

        public string Text { get => _entry.Text; set => SetText(value); }

        public int LinesCount => _entry.GetLinesCharsCount().Length;

        //public override bool AcceptMouseInput => base.AcceptMouseInput && IsEditable;

        public override bool AcceptKeyboardInput => base.AcceptKeyboardInput && IsEditable;

        public int MaxLines { get => _entry.MaxLines; set => _entry.MaxLines = value; }

        public void SetText(string text, bool append = false)
        {
            if (append)
            {
                text = _entry.InsertString(text);
                Parent?.OnKeyboardReturn((int)PageCommand.PasteText, text);
            }
            else
                _entry.SetText(text);
        }

        //private bool _isFocused;

        //public override bool IsFocused
        //{
        //    get => base.IsFocused;
        //    set
        //    {
        //        if (value)
        //        {
        //            if (Engine.UI.KeyboardFocusControl != null)
        //                Engine.UI.KeyboardFocusControl.IsFocused = false;

        //            Engine.UI.KeyboardFocusControl = this;
        //        }

        //        base.IsFocused = value;
        //    } 
        //}

        public override void Update(double totalMS, double frameMS)
        {
            if (IsDisposed)
                return;

            //multiline input is fixed height, unmodifiable
            if(!MultiLineInputAllowed)
                Height = _entry.Height;

            //if (Engine.UI.KeyboardFocusControl == this)
            //{
            //    if (!IsFocused)
            //    {
            //        _showCaret = true;
            //    }
            //}
            //else if (IsFocused)
            //{
            //    _showCaret = false;
            //}

            if (_entry.IsChanged)
                _entry.UpdateCaretPosition();
            base.Update(totalMS, frameMS);
        }

        public override bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
        {
            _entry.RenderText.Draw(batcher, new Point(position.X + _entry.Offset, position.Y));

            if (IsEditable)
            {
                if (HasKeyboardFocus)
                    _entry.RenderCaret.Draw(batcher, new Point(position.X + _entry.Offset + _entry.CaretPosition.X, position.Y + _entry.CaretPosition.Y));
            }

            return base.Draw(batcher, position, hue);
        }

        protected override void OnTextInput(string c)
        {
            _entry.InsertString(c);
        }

        protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            string s=null;
            int oldidx = _entry.CaretIndex;
            TextEntry oldentry = _entry;
            if (KeyboardInput.IsKeymodPressed(mod, SDL.SDL_Keymod.KMOD_CTRL) && key == SDL.SDL_Keycode.SDLK_v)//paste
            {
                s = SDL.SDL_GetClipboardText();
                if(!string.IsNullOrEmpty(s))
                {
                    s = _entry.InsertString(s.Replace("\r", string.Empty));
                    Parent?.OnKeyboardReturn((int)PageCommand.PasteText, s);
                    return;
                }
            }
            else switch (key)
            {
                case SDL.SDL_Keycode.SDLK_RETURN:

                    if (MultiLineInputAllowed)
                        s = _entry.InsertString("\n");
                    else
                        s = _entry.Text;
                        break;
                case SDL.SDL_Keycode.SDLK_BACKSPACE:
                    //TODO remove from current ccaret index
                    if (ReplaceDefaultTextOnFirstKeyPress)
                        ReplaceDefaultTextOnFirstKeyPress = false;
                    else
                        _entry.RemoveChar(true);
                    break;
                case SDL.SDL_Keycode.SDLK_UP when MultiLineInputAllowed:
                    _entry.OnMouseClick(_entry.CaretPosition.X, _entry.CaretPosition.Y - (_entry.RenderCaret.Height >> 1));
                        if (_entry.CaretIndex == 0 && oldidx == 0)
                        {
                            Parent?.OnKeyboardReturn((int)PageCommand.GoBackward, null);
                        }
                    break;
                case SDL.SDL_Keycode.SDLK_DOWN when MultiLineInputAllowed:
                    _entry.OnMouseClick(_entry.CaretPosition.X, _entry.CaretPosition.Y + _entry.RenderCaret.Height);
                    if (_entry.CaretIndex == Text.Length && oldidx == Text.Length)
                    {
                        Parent?.OnKeyboardReturn((int)PageCommand.GoForward, null);
                    }
                    break;
                case SDL.SDL_Keycode.SDLK_LEFT:
                    _entry.SeekCaretPosition(-1);
                    if (_entry.CaretIndex == 0 && oldidx == 0)
                    {
                        Parent?.OnKeyboardReturn((int)PageCommand.GoBackward, null);
                    }
                    break;
                case SDL.SDL_Keycode.SDLK_RIGHT:
                    _entry.SeekCaretPosition(1);
                    if (_entry.CaretIndex == Text.Length && oldidx == Text.Length)
                    {
                        Parent?.OnKeyboardReturn((int)PageCommand.GoForward, null);
                    }
                    break;
                case SDL.SDL_Keycode.SDLK_DELETE:
                    _entry.RemoveChar(false);
                    Parent?.OnKeyboardReturn((int)PageCommand.RemoveText, null);
                    break;
                case SDL.SDL_Keycode.SDLK_HOME:
                    _entry.SetCaretPosition(0);
                    if (oldidx == 0)
                    {
                        Parent?.OnKeyboardReturn((int)PageCommand.GoBackward, null);
                    }
                    break;
                case SDL.SDL_Keycode.SDLK_END:
                    _entry.SetCaretPosition(Text.Length - 1);
                    if (oldidx == Text.Length)
                    {
                        Parent?.OnKeyboardReturn((int)PageCommand.GoForward, null);
                    }
                    break;
            }

            Parent?.OnKeyboardReturn((int)PageCommand.Nothing, s);

            base.OnKeyDown(key, mod);
        }

        protected override void OnMouseClick(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left)
            {
                _entry.OnMouseClick(x, y);
            } 
        }

        protected override void OnMouseDown(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left)
                SetKeyboardFocus();
        }

        public override void Dispose()
        {
            _entry?.Dispose();
            _entry = null;
            base.Dispose();
        }
    }
}