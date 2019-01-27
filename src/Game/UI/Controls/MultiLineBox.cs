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

using ClassicUO.Input;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

using SDL2;

namespace ClassicUO.Game.UI.Controls
{
    internal class MultiLineBox : AbstractTextBox
    {
        public MultiLineEntry TxEntry { get; private set; }

        public MultiLineBox(MultiLineEntry txentry, bool editable)
        {
            TxEntry = txentry;
            base.AcceptKeyboardInput = true;
            base.AcceptMouseInput = true;
            IsEditable = editable;
        }

        public bool IsChanged => TxEntry.IsChanged;

        public Hue Hue
        {
            get => TxEntry.Hue;
            set => TxEntry.Hue = value;
        }

        public string Text { get => TxEntry.Text; set => SetText(value); }

        public int LinesCount => TxEntry.GetLinesCharsCount().Length;
        public int GetCharsOnLine(int line)
        {
            int[] lnch = TxEntry.GetLinesCharsCount();
            if (line < lnch.Length)
                return lnch[line];
            return 0;
        }

        public bool ScissorsEnabled { get; set; }

        //public override bool AcceptMouseInput => base.AcceptMouseInput && IsEditable;

        public int MaxLines { get => TxEntry.MaxLines; set => TxEntry.MaxLines = value; }
        public const int PasteCommandID = 0x10000000;
        public const int RetrnCommandID = 0x20000000;
        public const int PasteRetnCmdID = 0x30000000;

        public void SetText(string text)
        {
            TxEntry.SetText(text, TxEntry.CaretIndex + text.Length);
        }


        public override void Update(double totalMS, double frameMS)
        {
            if (IsDisposed)
                return;

            if (TxEntry.IsChanged)
                TxEntry.UpdateCaretPosition();
            base.Update(totalMS, frameMS);
        }

        private Rectangle _rect;

        public override bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
        {
            if (ScissorsEnabled)
            {
                _rect.X = position.X;
                _rect.Y = position.Y;
                _rect.Width = Width;
                _rect.Height = Height;

                Rectangle scissor = ScissorStack.CalculateScissors(batcher.TransformMatrix, _rect);

                if (ScissorStack.PushScissors(scissor))
                {
                    batcher.EnableScissorTest(true);

                    TxEntry.RenderText.Draw(batcher, new Point(position.X + TxEntry.Offset, position.Y));

                    if (IsEditable)
                    {
                        if (HasKeyboardFocus)
                            TxEntry.RenderCaret.Draw(batcher, new Point(position.X + TxEntry.Offset + TxEntry.CaretPosition.X, position.Y + TxEntry.CaretPosition.Y));
                    }

                    batcher.EnableScissorTest(false);
                    ScissorStack.PopScissors();
                }
            }
            else
            {
                TxEntry.RenderText.Draw(batcher, new Point(position.X + TxEntry.Offset, position.Y));

                if (IsEditable)
                {
                    if (HasKeyboardFocus)
                        TxEntry.RenderCaret.Draw(batcher, new Point(position.X + TxEntry.Offset + TxEntry.CaretPosition.X, position.Y + TxEntry.CaretPosition.Y));
                }
            }

            return base.Draw(batcher, position, hue);
        }

        protected override void OnTextInput(string c)
        {
            string s = TxEntry.InsertString(c);
            if (!string.IsNullOrEmpty(s))
                Parent?.OnKeyboardReturn(PasteCommandID, s);
        }

        protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            string s = null;
            int oldidx = TxEntry.CaretIndex;
            if (Input.Keyboard.IsModPressed(mod, SDL.SDL_Keymod.KMOD_CTRL) && key == SDL.SDL_Keycode.SDLK_v)//paste
            {
                if (SDL.SDL_HasClipboardText() == SDL.SDL_bool.SDL_FALSE)
                    return;

                s = SDL.SDL_GetClipboardText();
                if (!string.IsNullOrEmpty(s))
                {
                    Parent?.OnKeyboardReturn(PasteCommandID, s);
                    return;
                }
            }
            else switch (key)
                {
                    case SDL.SDL_Keycode.SDLK_KP_ENTER:
                    case SDL.SDL_Keycode.SDLK_RETURN:
                        Parent?.OnKeyboardReturn(RetrnCommandID, "\n");
                        break;
                    case SDL.SDL_Keycode.SDLK_BACKSPACE:
                        if (Parent is Gumps.BookGump bbook)
                            bbook.ScaleOnBackspace(TxEntry);
                        TxEntry.RemoveChar(true);
                        break;
                    case SDL.SDL_Keycode.SDLK_UP:
                        TxEntry.OnMouseClick(TxEntry.CaretPosition.X, TxEntry.CaretPosition.Y - (TxEntry.RenderCaret.Height >> 1));
                        break;
                    case SDL.SDL_Keycode.SDLK_DOWN:
                        TxEntry.OnMouseClick(TxEntry.CaretPosition.X, TxEntry.CaretPosition.Y + TxEntry.RenderCaret.Height);
                        break;
                    case SDL.SDL_Keycode.SDLK_LEFT:
                        TxEntry.SeekCaretPosition(-1);
                        break;
                    case SDL.SDL_Keycode.SDLK_RIGHT:
                        TxEntry.SeekCaretPosition(1);
                        break;
                    case SDL.SDL_Keycode.SDLK_DELETE:
                        if (Parent is Gumps.BookGump dbook)
                            dbook.ScaleOnDelete(TxEntry);
                        TxEntry.RemoveChar(false);
                        break;
                    case SDL.SDL_Keycode.SDLK_HOME:
                        if (Parent is Gumps.BookGump hbook)
                            hbook.OnHomeOrEnd(TxEntry, true);
                        else
                            TxEntry.SetCaretPosition(0);
                        break;
                    case SDL.SDL_Keycode.SDLK_END:
                        if (Parent is Gumps.BookGump ebook)
                            ebook.OnHomeOrEnd(TxEntry, false);
                        else
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