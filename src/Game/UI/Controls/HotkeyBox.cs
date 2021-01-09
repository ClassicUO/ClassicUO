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
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using SDL2;

namespace ClassicUO.Game.UI.Controls
{
    internal class HotkeyBox : Control
    {
        private bool _actived;
        private readonly Button _buttonOK, _buttonCancel;
        private readonly HoveredLabel _label;

        public HotkeyBox()
        {
            CanMove = false;
            AcceptMouseInput = true;
            AcceptKeyboardInput = true;


            Width = 210;
            Height = 20;

            ResizePic pic;

            Add
            (
                pic = new ResizePic(0x0BB8)
                {
                    Width = 150,
                    Height = Height,
                    AcceptKeyboardInput = true
                }
            );

            pic.MouseUp += LabelOnMouseUp;

            Add
            (
                _label = new HoveredLabel
                (
                    string.Empty,
                    false,
                    1,
                    0x0021,
                    0x0021,
                    150,
                    1,
                    FontStyle.Italic,
                    TEXT_ALIGN_TYPE.TS_CENTER
                )
                {
                    Y = 5
                }
            );

            _label.MouseUp += LabelOnMouseUp;

            Add
            (
                _buttonOK = new Button((int) ButtonState.Ok, 0x0481, 0x0483, 0x0482)
                {
                    X = 152,
                    ButtonAction = ButtonAction.Activate
                }
            );


            Add
            (
                _buttonCancel = new Button((int) ButtonState.Cancel, 0x047E, 0x0480, 0x047F)
                {
                    X = 182,
                    ButtonAction = ButtonAction.Activate
                }
            );

            WantUpdateSize = false;
            IsActive = false;
        }

        public SDL.SDL_Keycode Key { get; private set; }
        public SDL.SDL_Keymod Mod { get; private set; }

        public bool IsActive
        {
            get => _actived;
            set
            {
                _actived = value;

                if (value)
                {
                    _buttonOK.IsVisible = _buttonCancel.IsVisible = true;
                    _buttonOK.IsEnabled = _buttonCancel.IsEnabled = true;
                }
                else
                {
                    _buttonOK.IsVisible = _buttonCancel.IsVisible = false;
                    _buttonOK.IsEnabled = _buttonCancel.IsEnabled = false;
                }
            }
        }

        public event EventHandler HotkeyChanged, HotkeyCancelled;


        protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            if (IsActive)
            {
                SetKey(key, mod);
            }
        }

        public void SetKey(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            if (key == SDL.SDL_Keycode.SDLK_UNKNOWN && mod == SDL.SDL_Keymod.KMOD_NONE)
            {
                Key = key;
                Mod = mod;
                _label.Text = string.Empty;
            }
            else
            {
                string newvalue = KeysTranslator.TryGetKey(key, mod);

                if (!string.IsNullOrEmpty(newvalue) && key != SDL.SDL_Keycode.SDLK_UNKNOWN)
                {
                    Key = key;
                    Mod = mod;
                    _label.Text = newvalue;
                }
            }
        }

        private void LabelOnMouseUp(object sender, MouseEventArgs e)
        {
            IsActive = true;
            SetKeyboardFocus();
        }


        public override void OnButtonClick(int buttonID)
        {
            switch ((ButtonState) buttonID)
            {
                case ButtonState.Ok:
                    HotkeyChanged.Raise(this);

                    break;

                case ButtonState.Cancel:
                    _label.Text = string.Empty;

                    HotkeyCancelled.Raise(this);

                    Key = SDL.SDL_Keycode.SDLK_UNKNOWN;
                    Mod = SDL.SDL_Keymod.KMOD_NONE;

                    break;
            }

            IsActive = false;
        }

        private enum ButtonState
        {
            Ok,
            Cancel
        }
    }
}