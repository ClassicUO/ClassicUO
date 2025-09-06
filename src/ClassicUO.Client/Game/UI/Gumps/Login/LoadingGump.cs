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
using ClassicUO.Configuration;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Assets;
using SDL2;
using ClassicUO.Game.Managers;
using Microsoft.Xna.Framework;

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
        private readonly Action<int> _buttonClick;
        private readonly TextBox _label;

        public LoadingGump(string labelText, LoginButtons showButtons, Action<int> buttonClick = null) : base(0, 0)
        {
            _buttonClick = buttonClick;
            CanCloseWithRightClick = false;
            CanCloseWithEsc = false;

            bool isAsianLang = string.Compare(Settings.GlobalSettings.Language, "CHT", StringComparison.InvariantCultureIgnoreCase) == 0 || 
                string.Compare(Settings.GlobalSettings.Language, "KOR", StringComparison.InvariantCultureIgnoreCase) == 0 ||
                string.Compare(Settings.GlobalSettings.Language, "JPN", StringComparison.InvariantCultureIgnoreCase) == 0;

            bool unicode = isAsianLang;
            byte font = (byte)(isAsianLang ? 1 : 2);
            ushort hue = (ushort)(isAsianLang ? 0xFFFF : 0x0386);


            Add
              (
                 new SquareBlendControl
                 {
                     X = 0,
                     Y = 0,
                     Width = 1024,
                     Height = 768,
                     Hue = 0x1AF // Cor preta (0x0000)
                 }
              );



            _label = new TextBox(labelText, TrueTypeLoader.EMBEDDED_FONT, 24, 300, Color.Orange, strokeEffect: false) { X = 446, Y = 250, AcceptMouseInput = false };
            Add
              (
                 new SquareBlendControl
                 {
                     X = 356,
                     Y = 230,
                     Width = 366,
                     Height = 212,
                     Hue = 0x1AF // Cor preta (0x0000)
                 }
              );


            Add(_label);

            if (showButtons == LoginButtons.OK)
            {
                Add
                (
                    new Button((int) LoginButtons.OK, 0x0481, 0x0483, 0x0482)
                    {
                        X = 456, Y = 324, ButtonAction = ButtonAction.Activate
                    }
                );
            }
            else if (showButtons == LoginButtons.Cancel)
            {
                Add
                (
                    new Button((int) LoginButtons.Cancel, 0x047E, 0x0480, 0x047F)
                    {
                        X = 510,
                        Y = 324,
                        ButtonAction = ButtonAction.Activate
                    }
                );
            }
            else if (showButtons == (LoginButtons.OK | LoginButtons.Cancel))
            {
                Add
                (
                    new Button((int) LoginButtons.OK, 0x0481, 0x0483, 0x0482)
                    {
                        X = 426, Y = 304, ButtonAction = ButtonAction.Activate
                    }
                );

                Add
                (
                    new Button((int) LoginButtons.Cancel, 0x047E, 0x0480, 0x047F)
                    {
                        X = 496, Y = 304, ButtonAction = ButtonAction.Activate
                    }
                );
            }
        }

        public void SetText(string text)
        {
            _label.Text = text;
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