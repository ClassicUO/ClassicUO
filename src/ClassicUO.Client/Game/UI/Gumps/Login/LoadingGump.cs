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
using System.IO;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL3;

namespace ClassicUO.Game.UI.Gumps.Login
{
    [Flags]
    internal enum LoginButtons
    {
        None = 1,
        OK = 2,
        Cancel = 4
    }

    internal class LoadingGump : Gump
    {
        private const int ModalWidth = 460;
        private const int ModalHeight = 260;
        private const int LabelMaxWidth = 400;
        private const int ButtonWidth = 120;
        private const int ButtonHeight = 40;
        private const uint LoadingDotIntervalMs = 400;
        private static readonly Color ModalBgColor = Color.FromNonPremultiplied(25, 8, 8, 255);
        private static readonly Color AccentColor = Color.FromNonPremultiplied(180, 50, 50, 255);

        private readonly Action<int> _buttonClick;
        private readonly UOLabel _label;
        private readonly Texture2D _logoTexture;
        private string _baseLabelText;
        private uint _lastDotTicks;
        private int _loadingDotPhase;

        public LoadingGump(string labelText, LoginButtons showButtons, Action<int> buttonClick = null) : base(0, 0)
        {
            X = LoginLayoutHelper.ContentOffsetX;
            Y = LoginLayoutHelper.ContentOffsetY;
            _buttonClick = buttonClick;
            _baseLabelText = string.IsNullOrEmpty(labelText) ? "Loading" : labelText.TrimEnd('.');
            CanCloseWithRightClick = false;
            CanCloseWithEsc = false;

            Add(new SquareBlendControl(1f)
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

            const int LogoMaxWidth = 180;
            const int LogoMaxHeight = 40;
            string logoPath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client", "logodust.png");
            _logoTexture = PNGLoader.Instance.GetImageTexture(logoPath);
            if (_logoTexture != null)
            {
                float scale = Math.Min((float)LogoMaxWidth / _logoTexture.Width, (float)LogoMaxHeight / _logoTexture.Height);
                int logoW = (int)(_logoTexture.Width * scale);
                int logoH = (int)(_logoTexture.Height * scale);
                int logoX = panelX + ((ModalWidth - logoW) >> 1);
                int logoY = panelY + 12;
                Add(new CustomGumpPic(logoX, logoY, _logoTexture, logoW, logoH, 0));
            }

            Add(new RoundedColorBox(ModalWidth, 2, AccentColor, 0)
            {
                X = panelX,
                Y = panelY + 52,
                Alpha = 0.85f
            });

            string initialLabel = _baseLabelText + ".";
            _label = new UOLabel(initialLabel, 1, 0x0481, TEXT_ALIGN_TYPE.TS_CENTER, LabelMaxWidth)
            {
                X = panelX + (ModalWidth >> 1) - (LabelMaxWidth >> 1),
                Y = panelY + 68
            };
            Add(_label);
            _lastDotTicks = Time.Ticks;
            _loadingDotPhase = 1;

            int buttonY = panelY + ModalHeight - ButtonHeight - 24;

            if (showButtons == LoginButtons.OK)
            {
                AddLoginButton(panelX + (ModalWidth >> 1) - (ButtonWidth >> 1), buttonY, "OK", (int)LoginButtons.OK);
            }
            else if (showButtons == LoginButtons.Cancel)
            {
                AddLoginButton(panelX + (ModalWidth >> 1) - (ButtonWidth >> 1), buttonY, "CANCEL", (int)LoginButtons.Cancel);
            }
            else if (showButtons == (LoginButtons.OK | LoginButtons.Cancel))
            {
                int centerX = panelX + (ModalWidth >> 1);
                int gap = 16;
                int totalButtons = ButtonWidth * 2 + gap;
                AddLoginButton(centerX - totalButtons / 2, buttonY, "OK", (int)LoginButtons.OK);
                AddLoginButton(centerX - totalButtons / 2 + ButtonWidth + gap, buttonY, "CANCEL", (int)LoginButtons.Cancel);
            }
        }

        private void AddLoginButton(int x, int y, string text, int buttonId)
        {
            GothicStyleButtonLogin btn = new GothicStyleButtonLogin(x, y, ButtonWidth, ButtonHeight, text, null, 16);
            btn.OnClick += () => OnButtonClick(buttonId);
            Add(btn);
        }

        public override void Update()
        {
            if (!IsDisposed)
            {
                X = LoginLayoutHelper.ContentOffsetX;
                Y = LoginLayoutHelper.ContentOffsetY;
                uint now = Time.Ticks;
                if (now - _lastDotTicks >= LoadingDotIntervalMs)
                {
                    _lastDotTicks = now;
                    _loadingDotPhase = (_loadingDotPhase % 3) + 1;
                    _label.Text = _baseLabelText + new string('.', _loadingDotPhase);
                }
            }
            base.Update();
        }

        public void SetText(string text)
        {
            _baseLabelText = string.IsNullOrEmpty(text) ? "Loading" : text.TrimEnd('.');
            _label.Text = _baseLabelText + new string('.', _loadingDotPhase);
        }

        protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            if (key == SDL.SDL_Keycode.SDLK_KP_ENTER || key == SDL.SDL_Keycode.SDLK_RETURN)
            {
                OnButtonClick((int) LoginButtons.OK);
            }
        }


        public override void OnButtonClick(int buttonID)
        {
            _buttonClick?.Invoke(buttonID);
            base.OnButtonClick(buttonID);
        }
    }
}