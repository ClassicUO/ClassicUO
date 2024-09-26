#region license

// Copyright (c) 2024, andreakarasho
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

using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using SDL2;
using System.Collections.Generic;

namespace ClassicUO.Game.UI.Gumps.Login
{
    internal class LoginGump : Gump
    {
        private readonly ushort _buttonNormal;
        private readonly ushort _buttonOver;
        private readonly Checkbox _checkboxAutologin;
        private readonly Checkbox _checkboxSaveAccount;
        private readonly Button _nextArrow0;
        private readonly PasswordStbTextBox _passwordFake;
        private readonly StbTextBox _textboxAccount;

        private float _time;

        public LoginGump(World world, LoginScene scene) : base(world, 0, 0)
        {
            CanCloseWithRightClick = false;

            AcceptKeyboardInput = false;

            int offsetX, offsetY, offtextY;
            byte font;
            ushort hue;

            if (Client.Game.UO.Version < ClientVersion.CV_706400)
            {
                _buttonNormal = 0x15A4;
                _buttonOver = 0x15A5;
                const ushort HUE = 0x0386;

                if (Client.Game.UO.Version >= ClientVersion.CV_500A)
                {
                    Add(new GumpPic(0, 0, 0x2329, 0));
                }

                //UO Flag
                Add(new GumpPic(0, 4, 0x15A0, 0) { AcceptKeyboardInput = false });

                // Quit Button
                Add
                (
                    new Button((int)Buttons.Quit, 0x1589, 0x158B, 0x158A)
                    {
                        X = 555,
                        Y = 4,
                        ButtonAction = ButtonAction.Activate
                    }
                );

                //Login Panel
                Add
                (
                    new ResizePic(0x13BE)
                    {
                        X = 128,
                        Y = 288,
                        Width = 451,
                        Height = 157
                    }
                );

                if (Client.Game.UO.Version < ClientVersion.CV_500A)
                {
                    Add(new GumpPic(286, 45, 0x058A, 0));
                }

                // Credits
                Add
                (
                    new Button((int)Buttons.Credits, 0x1583, 0x1585, 0x1584)
                    {
                        X = 60,
                        Y = 385,
                        ButtonAction = ButtonAction.Activate
                    }
                );

                Add
                (
                    new Label(ResGumps.LoginToUO, false, HUE, font: 2)
                    {
                        X = 253,
                        Y = 305
                    }
                );

                Add
                (
                    new Label(ResGumps.Account, false, HUE, font: 2)
                    {
                        X = 183,
                        Y = 345
                    }
                );

                Add
                (
                    new Label(ResGumps.Password, false, HUE, font: 2)
                    {
                        X = 183,
                        Y = 385
                    }
                );

                // Arrow Button
                Add
                (
                    _nextArrow0 = new Button((int)Buttons.NextArrow, 0x15A4, 0x15A6, 0x15A5)
                    {
                        X = 610,
                        Y = 445,
                        ButtonAction = ButtonAction.Activate
                    }
                );


                offsetX = 328;
                offsetY = 343;
                offtextY = 40;

                Add
                (
                    new Label($"UO Version {Settings.GlobalSettings.ClientVersion}.", false, 0x034E, font: 9)
                    {
                        X = 286,
                        Y = 453
                    }
                );

                Add
                (
                    new Label(string.Format("TazUO Version {0}", CUOEnviroment.Version), false, 0x034E, font: 9)
                    {
                        X = 286,
                        Y = 465
                    }
                );


                Add
                (
                    _checkboxAutologin = new Checkbox
                    (
                        0x00D2,
                        0x00D3,
                        ResGumps.Autologin,
                        1,
                        0x0386,
                        false
                    )
                    {
                        X = 150,
                        Y = 417
                    }
                );

                Add
                (
                    _checkboxSaveAccount = new Checkbox
                    (
                        0x00D2,
                        0x00D3,
                        ResGumps.SaveAccount,
                        1,
                        0x0386,
                        false
                    )
                    {
                        X = _checkboxAutologin.X + _checkboxAutologin.Width + 10,
                        Y = 417
                    }
                );

                font = 1;
                hue = 0x0386;
            }
            else
            {
                _buttonNormal = 0x5CD;
                _buttonOver = 0x5CB;

                Add(new GumpPic(0, 0, 0x014E, 0));

                //// Quit Button
                Add
                (
                    new Button((int)Buttons.Quit, 0x05CA, 0x05C9, 0x05C8)
                    {
                        X = 25,
                        Y = 240,
                        ButtonAction = ButtonAction.Activate
                    }
                );

                //// Credit Button
                Add
                (
                    new Button((int)Buttons.Credits, 0x05D0, 0x05CF, 0x5CE)
                    {
                        X = 530,
                        Y = 125,
                        ButtonAction = ButtonAction.Activate
                    }
                );

                // Arrow Button
                Add
                (
                    _nextArrow0 = new Button((int)Buttons.NextArrow, 0x5CD, 0x5CC, 0x5CB)
                    {
                        X = 280,
                        Y = 365,
                        ButtonAction = ButtonAction.Activate
                    }
                );

                offsetX = 218;
                offsetY = 283;
                offtextY = 50;


                Add
                (
                    new Label($"UO Version {Settings.GlobalSettings.ClientVersion}.", false, 0x0481, font: 9)
                    {
                        X = 286,
                        Y = 453
                    }
                );

                Add
                (
                    new Label(string.Format("TazUO Version {0}", CUOEnviroment.Version), false, 0x0481, font: 9)
                    {
                        X = 286,
                        Y = 465
                    }
                );


                Add
                (
                    _checkboxAutologin = new Checkbox
                    (
                        0x00D2,
                        0x00D3,
                        ResGumps.Autologin,
                        9,
                        0x0481,
                        false
                    )
                    {
                        X = 150,
                        Y = 417
                    }
                );

                Add
                (
                    _checkboxSaveAccount = new Checkbox
                    (
                        0x00D2,
                        0x00D3,
                        ResGumps.SaveAccount,
                        9,
                        0x0481,
                        false
                    )
                    {
                        X = _checkboxAutologin.X + _checkboxAutologin.Width + 10,
                        Y = 417
                    }
                );

                font = 9;
                hue = 0x0481;
            }


            // Account Text Input Background
            Add
            (
                new ResizePic(0x0BB8)
                {
                    X = offsetX,
                    Y = offsetY,
                    Width = 210,
                    Height = 30
                }
            );

            // Password Text Input Background
            Add
            (
                new ResizePic(0x0BB8)
                {
                    X = offsetX,
                    Y = offsetY + offtextY,
                    Width = 210,
                    Height = 30
                }
            );

            offsetX += 7;

            // Text Inputs
            Add
            (
                _textboxAccount = new StbTextBox
                (
                    5,
                    16,
                    190,
                    false,
                    hue: 0x034F
                )
                {
                    X = offsetX,
                    Y = offsetY,
                    Width = 190,
                    Height = 25
                }
            );

            _textboxAccount.SetText(Settings.GlobalSettings.Username);

            Add
            (
                _passwordFake = new PasswordStbTextBox
                (
                    5,
                    16,
                    190,
                    false,
                    hue: 0x034F
                )
                {
                    X = offsetX,
                    Y = offsetY + offtextY + 2,
                    Width = 190,
                    Height = 25
                }
            );

            string[] accts = SimpleAccountManager.GetAccounts();
            if (accts.Length > 0)
            {
                _textboxAccount.ContextMenu = new ContextMenuControl();
                foreach (string acct in accts)
                {
                    _textboxAccount.ContextMenu.Add(new ContextMenuItemEntry(acct, () => { _textboxAccount.SetText(acct); }));
                }
                _textboxAccount.SetTooltip("Right click to select another account.");
                _textboxAccount.MouseUp += (s, e) => { if (e.Button == MouseButtonType.Right) _textboxAccount.ContextMenu.Show(); };
            }

            _passwordFake.RealText = Crypter.Decrypt(Settings.GlobalSettings.Password);

            _checkboxSaveAccount.IsChecked = Settings.GlobalSettings.SaveAccount;
            _checkboxAutologin.IsChecked = Settings.GlobalSettings.AutoLogin;


            Add
            (
                new HtmlControl
                (
                    505,
                    420,
                    150,
                    15,
                    false,
                    false,
                    false,
                    "<body link=\"#FF00FF00\" vlink=\"#FF00FF00\" ><a href=\"https://www.classicuo.eu/support.php\">Support ClassicUO!",
                    0x32,
                    true,
                    isunicode: true,
                    style: FontStyle.BlackBorder
                )
            );


            Add
            (
                new HtmlControl
                (
                    505,
                    440,
                    100,
                    15,
                    false,
                    false,
                    false,
                    "<body link=\"#FF00FF00\" vlink=\"#FF00FF00\" ><a href=\"https://www.classicuo.eu\">Website",
                    0x32,
                    true,
                    isunicode: true,
                    style: FontStyle.BlackBorder
                )
            );

            Add
            (
                new HtmlControl
                (
                    505,
                    460,
                    100,
                    15,
                    false,
                    false,
                    false,
                    "<body link=\"#FF00FF00\" vlink=\"#FF00FF00\" ><a href=\"https://discord.gg/VdyCpjQ\">Join Discord",
                    0x32,
                    true,
                    isunicode: true,
                    style: FontStyle.BlackBorder
                )
            );

            TextBox _;
            HitBox _hit;
            Add(_ = new TextBox("TazUO Wiki", TrueTypeLoader.EMBEDDED_FONT, 15, 200, Color.Orange, strokeEffect: false) { X = 30, Y = 420, AcceptMouseInput = true });
            Add(_hit = new HitBox(_.X, _.Y, _.MeasuredSize.X, _.MeasuredSize.Y));
            _hit.MouseUp += (s, e) =>
            {
                Utility.Platforms.PlatformHelper.LaunchBrowser("https://github.com/bittiez/ClassicUO/wiki");
            };

            Add(_ = new TextBox("TazUO Discord", TrueTypeLoader.EMBEDDED_FONT, 15, 200, Color.Orange, strokeEffect: false) { X = 30, Y = 440, AcceptMouseInput = true });
            Add(_hit = new HitBox(_.X, _.Y, _.MeasuredSize.X, _.MeasuredSize.Y));
            _hit.MouseUp += (s, e) =>
            {
                Utility.Platforms.PlatformHelper.LaunchBrowser("https://discord.gg/SqwtB5g95H");
            };

            Checkbox loginmusic_checkbox = new Checkbox
            (
                0x00D2,
                0x00D3,
                "Music",
                font,
                hue,
                false
            )
            {
                X = _checkboxSaveAccount.X + _checkboxSaveAccount.Width + 10,
                Y = 417,
                IsChecked = Settings.GlobalSettings.LoginMusic
            };

            Add(loginmusic_checkbox);

            HSliderBar login_music = new HSliderBar
            (
                loginmusic_checkbox.X + loginmusic_checkbox.Width + 10,
                loginmusic_checkbox.Y + 4,
                80,
                0,
                100,
                Settings.GlobalSettings.LoginMusicVolume,
                HSliderBarStyle.MetalWidgetRecessedBar,
                true,
                font,
                hue,
                false
            );

            Add(login_music);
            login_music.IsVisible = Settings.GlobalSettings.LoginMusic;

            loginmusic_checkbox.ValueChanged += (sender, e) =>
            {
                Settings.GlobalSettings.LoginMusic = loginmusic_checkbox.IsChecked;
                Client.Game.Audio.UpdateCurrentMusicVolume(true);

                login_music.IsVisible = Settings.GlobalSettings.LoginMusic;
            };

            login_music.ValueChanged += (sender, e) =>
            {
                Settings.GlobalSettings.LoginMusicVolume = login_music.Value;
                Client.Game.Audio.UpdateCurrentMusicVolume(true);
            };


            if (!string.IsNullOrEmpty(_textboxAccount.Text))
            {
                _passwordFake.SetKeyboardFocus();
            }
            else
            {
                _textboxAccount.SetKeyboardFocus();
            }

            _ = new TextBox("A new version of TazUO is available!\n Click to open the download page.", TrueTypeLoader.EMBEDDED_FONT, 20, 300, Color.Yellow, strokeEffect: false) { X = 10, Y = 10, AcceptMouseInput = false };
            Add(_hit = new HitBox(_.X, _.Y, _.MeasuredSize.X, _.MeasuredSize.Y));
            _hit.MouseUp += (s, e) =>
            {
                Utility.Platforms.PlatformHelper.LaunchBrowser("https://github.com/bittiez/TazUO/releases/latest");
            };
            _hit.Add(new AlphaBlendControl() { Width = _hit.Width, Height = _hit.Height });
            Add(_);
            if (!UpdateManager.HasUpdate)
            {
                _.IsVisible = false;
                _hit.IsVisible = false;
            }

            if (!UpdateManager.SkipUpdateCheck)
            {
                UpdateManager.UpdateStatusChanged += (s, e) =>
                {
                    if (UpdateManager.HasUpdate)
                    {
                        _.IsVisible = true;
                        _hit.IsVisible = true;
                    }
                };
            }

        }

        public override void OnKeyboardReturn(int textID, string text)
        {
            SaveCheckboxStatus();
            LoginScene ls = Client.Game.GetScene<LoginScene>();

            if (ls.CurrentLoginStep == LoginSteps.Main)
            {
                ls.Connect(_textboxAccount.Text, _passwordFake.RealText);
            }
        }

        private void SaveCheckboxStatus()
        {
            Settings.GlobalSettings.SaveAccount = _checkboxSaveAccount.IsChecked;
            Settings.GlobalSettings.AutoLogin = _checkboxAutologin.IsChecked;
        }

        public override void Update()
        {
            if (IsDisposed)
            {
                return;
            }

            base.Update();

            if (_time < Time.Ticks)
            {
                _time = (float)Time.Ticks + 1000;

                _nextArrow0.ButtonGraphicNormal = _nextArrow0.ButtonGraphicNormal == _buttonNormal ? _buttonOver : _buttonNormal;
            }

            if (_passwordFake.HasKeyboardFocus)
            {
                if (_passwordFake.Hue != 0x0021)
                {
                    _passwordFake.Hue = 0x0021;
                }
            }
            else if (_passwordFake.Hue != 0)
            {
                _passwordFake.Hue = 0;
            }

            if (_textboxAccount.HasKeyboardFocus)
            {
                if (_textboxAccount.Hue != 0x0021)
                {
                    _textboxAccount.Hue = 0x0021;
                }
            }
            else if (_textboxAccount.Hue != 0)
            {
                _textboxAccount.Hue = 0;
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((Buttons)buttonID)
            {
                case Buttons.NextArrow:
                    SaveCheckboxStatus();

                    if (!_textboxAccount.IsDisposed)
                    {
                        Client.Game.GetScene<LoginScene>().Connect(_textboxAccount.Text, _passwordFake.RealText);
                    }

                    break;

                case Buttons.Quit:
                    Client.Game.Exit();

                    break;

                case Buttons.Credits:
                    UIManager.Add(new CreditsGump(World));

                    break;
            }
        }

        private class PasswordStbTextBox : StbTextBox
        {
            private new Point _caretScreenPosition;
            private new readonly RenderedText _rendererCaret;

            private new readonly RenderedText _rendererText;

            public PasswordStbTextBox
            (
                byte font,
                int max_char_count = -1,
                int maxWidth = 0,
                bool isunicode = true,
                FontStyle style = FontStyle.None,
                ushort hue = 0,
                TEXT_ALIGN_TYPE align = TEXT_ALIGN_TYPE.TS_LEFT
            ) : base
            (
                font,
                max_char_count,
                maxWidth,
                isunicode,
                style,
                hue,
                align
            )
            {
                _rendererText = RenderedText.Create
                (
                    string.Empty,
                    hue,
                    font,
                    isunicode,
                    style,
                    align,
                    maxWidth
                );

                _rendererCaret = RenderedText.Create
                (
                    "_",
                    hue,
                    font,
                    isunicode,
                    (style & FontStyle.BlackBorder) != 0 ? FontStyle.BlackBorder : FontStyle.None,
                    align
                );

                NoSelection = true;
            }

            internal string RealText
            {
                get => Text;
                set => SetText(value);
            }

            public new ushort Hue
            {
                get => _rendererText.Hue;
                set
                {
                    if (_rendererText.Hue != value)
                    {
                        _rendererText.Hue = value;
                        _rendererCaret.Hue = value;

                        _rendererText.CreateTexture();
                        _rendererCaret.CreateTexture();
                    }
                }
            }

            protected override void DrawCaret(UltimaBatcher2D batcher, int x, int y)
            {
                if (HasKeyboardFocus)
                {
                    _rendererCaret.Draw(batcher, x + _caretScreenPosition.X, y + _caretScreenPosition.Y);
                }
            }

            protected override void OnMouseDown(int x, int y, MouseButtonType button)
            {
                base.OnMouseDown(x, y, button);

                if (button == MouseButtonType.Left)
                {
                    UpdateCaretScreenPosition();
                }
            }

            protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
            {
                base.OnKeyDown(key, mod);
                UpdateCaretScreenPosition();
            }

            public override void Dispose()
            {
                _rendererText?.Destroy();
                _rendererCaret?.Destroy();

                base.Dispose();
            }

            protected override void OnTextInput(string c)
            {
                base.OnTextInput(c);
            }

            protected override void OnTextChanged()
            {
                if (Text.Length > 0)
                {
                    _rendererText.Text = new string('*', Text.Length);
                }
                else
                {
                    _rendererText.Text = string.Empty;
                }

                base.OnTextChanged();
                UpdateCaretScreenPosition();
            }

            internal override void OnFocusEnter()
            {
                base.OnFocusEnter();
                CaretIndex = Text?.Length ?? 0;
                UpdateCaretScreenPosition();
            }

            private new void UpdateCaretScreenPosition()
            {
                _caretScreenPosition = _rendererText.GetCaretPosition(Stb.CursorIndex);
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                if (batcher.ClipBegin(x, y, Width, Height))
                {
                    DrawSelection(batcher, x, y);

                    _rendererText.Draw(batcher, x, y);

                    DrawCaret(batcher, x, y);
                    batcher.ClipEnd();
                }

                return true;
            }
        }


        private enum Buttons
        {
            NextArrow,
            Quit,
            Credits
        }
    }
}