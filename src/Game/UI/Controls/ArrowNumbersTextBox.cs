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
    internal class ArrowNumbersTextBox : AbstractTextBox
    {
        private static readonly long _nextValueUpdate = TimeSpan.FromMilliseconds(250).Ticks;
        private long _nextUpdateOn;
        private readonly Button _up, _down;
        private readonly int _Min, _Max;

        public ArrowNumbersTextBox(Control c, int x, int y, int width, int page, int raiseamount, int minvalue, int maxvalue, byte font = 0, int maxcharlength = -1, bool isunicode = true, FontStyle style = FontStyle.None, ushort hue = 0)
        {
            TxEntry = new TextEntry(font, maxcharlength, width, width, isunicode, style, hue) { NumericOnly = true };
            int height = TxEntry.Height;
            IsEditable = true;
            base.AcceptKeyboardInput = true;
            base.AcceptMouseInput = true;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            _Min = minvalue;
            _Max = maxvalue;
            
            c.Add(new ResizePic(0x0BB8)
            {
                X = x - 5,
                Y = y - 3,
                Width = width + 15,
                Height = height - 2
            }, page);

            _up = new Button(raiseamount, 0x983, 0x984)
            {
                X = x + width,
                Y = y - 4
            };
            _up.MouseDown += (sender, e) =>
            {
                if (_up.IsClicked)
                {
                    UpdateValue();
                    _nextUpdateOn = DateTime.UtcNow.Ticks + _nextValueUpdate * 2;
                }
            };
            c.Add(_up, page);
            _down = new Button(-raiseamount, 0x985, 0x986)
            {
                X = x + width,
                Y = y + 9
            };
            _down.MouseDown += (sender, e) =>
            {
                if (_down.IsClicked)
                {
                    UpdateValue();
                    _nextUpdateOn = DateTime.UtcNow.Ticks + _nextValueUpdate * 2;
                }
            };
            c.Add(_down, page);
        }

        public event EventHandler TextChanged;

        public TextEntry TxEntry { get; private set; }

        public bool IsChanged => TxEntry.IsChanged;

        public Hue Hue
        {
            get => TxEntry.Hue;
            set => TxEntry.Hue = value;
        }

        public bool ReplaceDefaultTextOnFirstKeyPress { get; set; }

        public string Text { get => TxEntry.Text; set => SetText(value); }

        private void UpdateValue()
        {
            int.TryParse(Text, out int i);
            if (_up.IsClicked)
                i += _up.ButtonID;
            else
                i += _down.ButtonID;
            ValidateValue(i);
        }

        protected override void OnFocusLeft()
        {
            base.OnFocusLeft();
        }

        private void ValidateValue(int val)
        {
            SetText(Math.Max(_Min, Math.Min(_Max, val)).ToString());
        }

        public void SetText(string text)
        {
            int oldidx = TxEntry.CaretIndex;
            TxEntry.SetText(text);
            TxEntry.SetCaretPosition(oldidx + text.Length);
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (IsDisposed)
                return;

            if (_up.IsClicked || _down.IsClicked)
            {
                long nowtick = DateTime.UtcNow.Ticks;
                if (_nextUpdateOn <= nowtick)
                {
                    _nextUpdateOn = nowtick + _nextValueUpdate;

                    UpdateValue();
                }
            }

            if (Height != TxEntry.Height)
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
                if (!string.IsNullOrEmpty(s))
                {
                    TxEntry.InsertString(s.Replace("\r", string.Empty).Replace("\n", string.Empty));
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
                        {
                            TxEntry.RemoveChar(true);
                        }
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
