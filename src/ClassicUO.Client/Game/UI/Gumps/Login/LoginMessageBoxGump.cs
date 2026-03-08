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
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using Microsoft.Xna.Framework;
using SDL3;

namespace ClassicUO.Game.UI.Gumps.Login
{
    internal class LoginMessageBoxGump : Gump
    {
        private const int ModalWidth = 460;
        private const int ModalHeight = 260;
        private const int MessageMaxWidth = 400;
        private const int ButtonWidth = 120;
        private const int ButtonHeight = 40;
        private static readonly Color ModalBgColor = Color.FromNonPremultiplied(25, 8, 8, 255);
        private static readonly Color AccentColor = Color.FromNonPremultiplied(180, 50, 50, 255);

        private readonly Action<bool> _action;

        public LoginMessageBoxGump(string message, Action<bool> action = null, bool showCancel = false) : base(0, 0)
        {
            X = LoginLayoutHelper.ContentOffsetX;
            Y = LoginLayoutHelper.ContentOffsetY;
            _action = action;
            CanCloseWithRightClick = false;
            CanCloseWithEsc = false;
            AcceptMouseInput = true;
            AcceptKeyboardInput = true;
            IsModal = true;
            LayerOrder = UILayer.Over;

            Add(new SquareBlendControl(0.7f)
            {
                X = 0,
                Y = 0,
                Width = LoginLayoutHelper.ContentWidth,
                Height = LoginLayoutHelper.ContentHeight,
                BaseColor = Color.Black
            });

            int panelX = LoginLayoutHelper.CenterOffsetX(ModalWidth);
            int panelY = LoginLayoutHelper.CenterOffsetY(ModalHeight);

            Add(new RoundedColorBox(ModalWidth, ModalHeight, ModalBgColor, 12)
            {
                X = panelX,
                Y = panelY
            });

            Add(new RoundedColorBox(ModalWidth, 2, AccentColor, 0)
            {
                X = panelX,
                Y = panelY + 52,
                Alpha = 0.85f
            });

            var label = new UOLabel(message, 1, 0x0481, TEXT_ALIGN_TYPE.TS_CENTER, MessageMaxWidth)
            {
                X = panelX + (ModalWidth >> 1) - (MessageMaxWidth >> 1),
                Y = panelY + 68
            };
            Add(label);

            int buttonY = panelY + ModalHeight - ButtonHeight - 24;

            if (showCancel)
            {
                int centerX = panelX + (ModalWidth >> 1);
                int gap = 16;
                int totalButtons = ButtonWidth * 2 + gap;
                AddMessageButton(centerX - totalButtons / 2, buttonY, "OK", true);
                AddMessageButton(centerX - totalButtons / 2 + ButtonWidth + gap, buttonY, "CANCEL", false);
            }
            else
            {
                AddMessageButton(panelX + (ModalWidth >> 1) - (ButtonWidth >> 1), buttonY, "OK", true);
            }

            UIManager.KeyboardFocusControl = this;
            UIManager.KeyboardFocusControl.SetKeyboardFocus();
        }

        private void AddMessageButton(int x, int y, string text, bool result)
        {
            var btn = new GothicStyleButtonLogin(x, y, ButtonWidth, ButtonHeight, text, null, 16);
            btn.OnClick += () => OnButtonResult(result);
            Add(btn);
        }

        private void OnButtonResult(bool result)
        {
            _action?.Invoke(result);
            Dispose();
        }

        protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            if (key == SDL.SDL_Keycode.SDLK_KP_ENTER || key == SDL.SDL_Keycode.SDLK_RETURN)
            {
                OnButtonResult(true);
            }
        }

        public override void Update()
        {
            if (!IsDisposed)
            {
                X = LoginLayoutHelper.ContentOffsetX;
                Y = LoginLayoutHelper.ContentOffsetY;
            }
            base.Update();
        }
    }
}
