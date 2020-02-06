﻿#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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

using ClassicUO.Configuration;
using ClassicUO.Data;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using ClassicUO.Utility;

namespace ClassicUO.Game.UI.Gumps.Login
{
    internal class LoginGump : Gump
    {
        private readonly ushort _buttonNormal;
        private readonly ushort _buttonOver;
        private readonly Checkbox _checkboxAutologin;
        private readonly Checkbox _checkboxSaveAccount;
        private readonly Button _nextArrow0;
        private readonly TextBox _textboxAccount;
        private readonly TextBox _textboxPassword;

        private float _time;

        public LoginGump() : base(0, 0)
        {
            CanCloseWithRightClick = false;

            AcceptKeyboardInput = false;


            int offsetX, offsetY, offtextY;

            if (Client.Version < ClientVersion.CV_706400)
            {
                _buttonNormal = 0x15A4;
                _buttonOver = 0x15A5;
                const ushort HUE = 0x0386;

                if (Client.Version >= ClientVersion.CV_500A)
                    Add(new GumpPic(0, 0, 0x2329, 0));

                // UO Flag
                Add(new GumpPic(0, 4, 0x15A0, 0) { AcceptKeyboardInput = false });
                //// Quit Button
                Add(new Button((int) Buttons.Quit, 0x1589, 0x158B, 0x158A)
                {
                    X = 555,
                    Y = 4,
                    ButtonAction = ButtonAction.Activate
                });

                // Login Panel
                Add(new ResizePic(0x13BE)
                {
                    X = 128,
                    Y = 288,
                    Width = 451,
                    Height = 157
                });

                if (Client.Version < ClientVersion.CV_500A)
                    Add(new GumpPic(286, 45, 0x058A, 0));

                Add(new Label("Log in to Ultima Online", false, HUE, font: 2)
                {
                    X = 253,
                    Y = 305
                });

                Add(new Label("Account Name", false, HUE, font: 2)
                {
                    X = 183,
                    Y = 345
                });

                Add(new Label("Password", false, HUE, font: 2)
                {
                    X = 183,
                    Y = 385
                });

                // Arrow Button
                Add(_nextArrow0 = new Button((int) Buttons.NextArrow, 0x15A4, 0x15A6, 0x15A5)
                {
                    X = 610,
                    Y = 445,
                    ButtonAction = ButtonAction.Activate
                });


                offsetX = 328;
                offsetY = 343;
                offtextY = 40;

                Add(new Label($"UO Version {Settings.GlobalSettings.ClientVersion}.", false, 0x034E, font: 9)
                {
                    X = 286,
                    Y = 453
                });

                Add(new Label($"ClassicUO Version {CUOEnviroment.Version}", false, 0x034E, font: 9)
                {
                    X = 286,
                    Y = 465
                });

                Add(_checkboxAutologin = new Checkbox(0x00D2, 0x00D3, "Autologin", 1, 0x0386, false)
                {
                    X = 200,
                    Y = 417
                });

                Add(_checkboxSaveAccount = new Checkbox(0x00D2, 0x00D3, "Save Account", 1, 0x0386, false)
                {
                    X = _checkboxAutologin.X + _checkboxAutologin.Width + 10,
                    Y = 417
                });
            }
            else
            {
                _buttonNormal = 0x5CD;
                _buttonOver = 0x5CB;

                Add(new GumpPic(0, 0, 0x014E, 0));

                //// Quit Button
                Add(new Button((int) Buttons.Quit, 0x05CA, 0x05C9, 0x05C8)
                {
                    X = 25,
                    Y = 240,
                    ButtonAction = ButtonAction.Activate
                });

                // Arrow Button
                Add(_nextArrow0 = new Button((int) Buttons.NextArrow, 0x5CD, 0x5CC, 0x5CB)
                {
                    X = 280,
                    Y = 365,
                    ButtonAction = ButtonAction.Activate
                });

                offsetX = 218;
                offsetY = 283;
                offtextY = 50;


                Add(new Label($"UO Version {Settings.GlobalSettings.ClientVersion}.", false, 0x0481, font: 9)
                {
                    X = 286,
                    Y = 453
                });

                Add(new Label($"ClassicUO Version {CUOEnviroment.Version}", false, 0x0481, font: 9)
                {
                    X = 286,
                    Y = 465
                });


                Add(_checkboxAutologin = new Checkbox(0x00D2, 0x00D3, "Autologin", 9, 0x0481, false)
                {
                    X = 200,
                    Y = 417
                });

                Add(_checkboxSaveAccount = new Checkbox(0x00D2, 0x00D3, "Save Account", 9, 0x0481, false)
                {
                    X = _checkboxAutologin.X + _checkboxAutologin.Width + 10,
                    Y = 417
                });
            }


            // Account Text Input Background
            Add(new ResizePic(0x0BB8)
            {
                X = offsetX,
                Y = offsetY,
                Width = 210,
                Height = 30
            });

            // Password Text Input Background
            Add(new ResizePic(0x0BB8)
            {
                X = offsetX,
                Y = offsetY + offtextY,
                Width = 210,
                Height = 30
            });

            offsetX += 7;

            // Text Inputs
            Add(_textboxAccount = new TextBox(5, 16, 190, 190, false)
            {
                X = offsetX,
                Y = offsetY,
                Width = 190,
                Height = 25,
                Hue = 0x034F,
                SafeCharactersOnly = true
            });

            Add(_textboxPassword = new TextBox(5, 16, 190, 190, false)
            {
                X = offsetX,
                Y = offsetY + offtextY + 2,
                Width = 190,
                Height = 25,
                Hue = 0x034F,
                IsPassword = true,
                SafeCharactersOnly = true
            });
            _textboxAccount.SetText(Settings.GlobalSettings.Username);
            _textboxPassword.SetText(Crypter.Decrypt(Settings.GlobalSettings.Password));

            
            _checkboxSaveAccount.IsChecked = Settings.GlobalSettings.SaveAccount;
            _checkboxAutologin.IsChecked = Settings.GlobalSettings.AutoLogin;


            int htmlX = 130;
            int htmlY = 442;

            Add(new HtmlControl(htmlX, htmlY, 300, 100,
                                false, false,
                                false,
                                text: "<body link=\"#ad9413\" vlink=\"#00FF00\" ><a href=\"https://www.paypal.me/muskara\">> Support ClassicUO",
                                0x32, true, isunicode: true, style: FontStyle.BlackBorder));
            Add(new HtmlControl(htmlX, htmlY + 20, 300, 100,
                                false, false,
                                false,
                                text: "<body link=\"#ad9413\" vlink=\"#00FF00\" ><a href=\"https://www.patreon.com/user?u=21694183\">> Become a Patreon!",
                                0x32, true, isunicode: true, style: FontStyle.BlackBorder));


            Add(new HtmlControl(505, htmlY + 19, 300, 100,
                                           false, false,
                                           false,
                                           text: "<body link=\"#6a6a62\" vlink=\"#00FF00\" ><a href=\"https://discord.gg/VdyCpjQ\">CUO Discord",
                                           0x32, true, isunicode: true, style: FontStyle.Cropped));


            if (!string.IsNullOrEmpty(_textboxAccount.Text))
                _textboxPassword.SetKeyboardFocus();
        }




        public override void OnKeyboardReturn(int textID, string text)
        {
            SaveCheckboxStatus();
            LoginScene ls = Client.Game.GetScene<LoginScene>();

            if (ls.CurrentLoginStep == LoginSteps.Main)
                ls.Connect(_textboxAccount.Text, _textboxPassword.Text);
        }

        private void SaveCheckboxStatus()
        {
            Settings.GlobalSettings.SaveAccount = _checkboxSaveAccount.IsChecked;
            Settings.GlobalSettings.AutoLogin = _checkboxAutologin.IsChecked;
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (IsDisposed)
                return;

            base.Update(totalMS, frameMS);

            if (_time < totalMS)
            {
                _time = (float) totalMS + 1000;
                _nextArrow0.ButtonGraphicNormal = _nextArrow0.ButtonGraphicNormal == _buttonNormal ? _buttonOver : _buttonNormal;
            }

            if (_textboxPassword.HasKeyboardFocus)
            {
                if (_textboxPassword.Hue != 0x0021)
                    _textboxPassword.Hue = 0x0021;
            }
            else if (_textboxPassword.Hue != 0)
                _textboxPassword.Hue = 0;

            if (_textboxAccount.HasKeyboardFocus)
            {
                if (_textboxAccount.Hue != 0x0021)
                    _textboxAccount.Hue = 0x0021;
            }
            else if (_textboxAccount.Hue != 0)
                _textboxAccount.Hue = 0;
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((Buttons) buttonID)
            {
                case Buttons.NextArrow:
                    SaveCheckboxStatus();
                    if (!_textboxAccount.IsDisposed)
                        Client.Game.GetScene<LoginScene>().Connect(_textboxAccount.Text, _textboxPassword.Text);

                    break;

                case Buttons.Quit:
                    Client.Game.Exit();

                    break;
            }
        }
        
        private enum Buttons
        {
            NextArrow,
            Quit
        }
    }
}