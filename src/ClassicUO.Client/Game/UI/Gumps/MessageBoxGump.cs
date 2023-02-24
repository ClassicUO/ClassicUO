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
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using SDL2;

namespace ClassicUO.Game.UI.Gumps
{
    enum MessageButtonType
    {
        OK,
        OK_CANCEL,
        
    }

    internal class MessageBoxGump : Gump
    {
        private readonly Action<bool> _action;

        public MessageBoxGump(int w, int h, string message, Action<bool> action, bool hasBackground = false, MessageButtonType menuType = MessageButtonType.OK) : base(0, 0)
        {
            CanMove = true;
            CanCloseWithRightClick = false;
            CanCloseWithEsc = false;
            AcceptMouseInput = true;
            AcceptKeyboardInput = true;

            IsModal = true;
            LayerOrder = UILayer.Over;
            WantUpdateSize = false;

            Width = w;
            Height = h;
            _action = action;

            Add
            (
                new ResizePic(0x0A28)
                {
                    Width = w, Height = h
                }
            );

            if (hasBackground)
            {
                ResizePic background = new ResizePic(3000)
                {
                    X = X + 30,
                    Y = Y + 40,
                    Width = Width - 60,
                    Height = Height - 100
                };

                Add(background);
            }

            Add
            (
                new Label
                (
                    message,
                    false,
                    0x0386,
                    Width - 90,
                    1
                )
                {
                    X = 40,
                    Y = 45
                }
            );

            X = (Client.Game.Window.ClientBounds.Width - Width) >> 1;
            Y = (Client.Game.Window.ClientBounds.Height - Height) >> 1;

            // OK
            Button b;

            Add
            (
                b = new Button(0, 0x0481, 0x0483, 0x0482)
                {
                    Y = Height - 45,
                    ButtonAction = ButtonAction.Activate
                }
            );

            b.X = (Width - b.Width) >> 1;


            if (menuType == MessageButtonType.OK_CANCEL)
            {
                Button bCancel;

                Add
                (
                    bCancel = new Button(1, 0x047E, 0x047F, 0x0480)
                    {
                        Y = Height - 45,
                        ButtonAction = ButtonAction.Activate
                    }
                );

                b.X = Width / 2 - bCancel.Width;
                bCancel.X = Width / 2 - bCancel.Width;
                bCancel.X += b.Width + 5;
            }


            WantUpdateSize = false;

            UIManager.KeyboardFocusControl = this;
            UIManager.KeyboardFocusControl.SetKeyboardFocus();
        }

        protected override void OnKeyUp(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            base.OnKeyUp(key, mod);

            if (key == SDL.SDL_Keycode.SDLK_RETURN && mod == 0)
            {
                OnButtonClick(0);
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            switch (buttonID)
            {
                case 0:
                    _action?.Invoke(true);
                    Dispose();

                    break;

                case 1:
                    _action?.Invoke(false);
                    Dispose();

                    break;
            }
        }
    }


    internal class EntryDialog : Gump
    {
        private readonly Action<string> _action;
        private readonly StbTextBox _textBox;

        public EntryDialog(int w, int h, string message, Action<string> action) : base(0, 0)
        {
            CanMove = false;
            CanCloseWithRightClick = false;
            CanCloseWithEsc = false;
            AcceptMouseInput = false;

            IsModal = true;
            LayerOrder = UILayer.Over;
            WantUpdateSize = false;

            Width = w;
            Height = h;
            _action = action;

            Add
            (
                new ResizePic(0x0A28)
                {
                    Width = w,
                    Height = h
                }
            );

            Label l;

            Add
            (
                l = new Label
                (
                    message,
                    false,
                    0x0386,
                    Width - 90,
                    1
                )
                {
                    X = 40,
                    Y = 45
                }
            );

            Add
            (
                new ResizePic(0x0BB8)
                {
                    X = 40,
                    Y = 45 + l.Height + 5,
                    Width = w - 90,
                    Height = 25
                }
            );

            int ww = w - 94;

            _textBox = new StbTextBox
            (
                0xFF,
                -1,
                ww,
                true,
                FontStyle.BlackBorder | FontStyle.Fixed
            )
            {
                X = 42,
                Y = 45 + l.Height + 7,
                Width = ww,
                Height = 25
            };

            Add(_textBox);

            X = (Client.Game.Window.ClientBounds.Width - Width) >> 1;
            Y = (Client.Game.Window.ClientBounds.Height - Height) >> 1;


            // OK
            Button b;

            Add
            (
                b = new Button(0, 0x0481, 0x0482, 0x0483)
                {
                    Y = Height - 45,
                    ButtonAction = ButtonAction.Activate
                }
            );

            b.X = (Width - b.Width) >> 1;
        }

        public override void OnButtonClick(int buttonID)
        {
            switch (buttonID)
            {
                case 0:
                    _action?.Invoke(_textBox.Text);
                    Dispose();

                    break;
            }
        }
    }
}