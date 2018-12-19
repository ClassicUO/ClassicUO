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
        public bool MultiLineInputAllowed { get; set; } = false;
        private TextEntry _entry;

        private bool _showCaret;

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

        public int LinesCount => _entry.GetLinesCount();

        //public override bool AcceptMouseInput => base.AcceptMouseInput && IsEditable;

        public override bool AcceptKeyboardInput => base.AcceptKeyboardInput && IsEditable;

        public int MaxLines { get => _entry.MaxLines; set => _entry.MaxLines = value; }

        public void SetText(string text, bool append = false)
        {
            if (append)
                _entry.InsertString(text);
            else
                _entry.SetText(text);
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (IsDisposed)
                return;

            //multiline input is fixed height, unmodifiable
            if(!MultiLineInputAllowed)
                Height = _entry.Height;
            if (Engine.UI.KeyboardFocusControl == this)
            {
                if (!IsFocused)
                {
                    SetFocused();
                    _showCaret = true;
                }

                _showCaret = true;
            }
            else if (IsFocused)
            {
                RemoveFocus();
                _showCaret = false;
            }

            if (_entry.IsChanged)
                _entry.UpdateCaretPosition();
            base.Update(totalMS, frameMS);
        }

        public override bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
        {
            _entry.RenderText.Draw(batcher, new Point(position.X + _entry.Offset, position.Y));

            if (IsEditable)
            {
                if (_showCaret)
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
            switch (key)
            {
                /*case SDL.SDL_Keycode.SDLK_TAB:
                    if (AllowTAB)
                        _entry.InsertString("    ");
                    break;*/
                case SDL.SDL_Keycode.SDLK_RETURN:
                    if ( MultiLineInputAllowed )
                        _entry.InsertString( "\n" );
                    //if ((_entry.RenderText.FontStyle & FontStyle.Fixed) == 0)
                    //    _entry.InsertString("\n");
                    //else
                    Parent.OnKeybaordReturn((int) LocalSerial.Value, Text);

                    break;
                case SDL.SDL_Keycode.SDLK_BACKSPACE:
                    //TODO remove from current ccaret index
                    if (ReplaceDefaultTextOnFirstKeyPress)
                        ReplaceDefaultTextOnFirstKeyPress = false;
                    else
                        _entry.RemoveChar(true);

                    break;
                case SDL.SDL_Keycode.SDLK_LEFT:
                    _entry.SeekCaretPosition(-1);

                    break;
                case SDL.SDL_Keycode.SDLK_RIGHT:
                    _entry.SeekCaretPosition(1);

                    break;
                case SDL.SDL_Keycode.SDLK_DELETE:
                    _entry.RemoveChar(false);

                    break;
                case SDL.SDL_Keycode.SDLK_HOME:
                    _entry.SetCaretPosition(0);

                    break;
                case SDL.SDL_Keycode.SDLK_END:
                    _entry.SetCaretPosition(Text.Length - 1);

                    break;
            }

            base.OnKeyDown(key, mod);
        }

        protected override void OnMouseClick(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left)
                _entry.OnMouseClick(x, y);
        }

        public override void Dispose()
        {
            _entry?.Dispose();
            _entry = null;
            base.Dispose();
        }
    }
}