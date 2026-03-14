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
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Dust765;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL3;
using System.Collections.Generic;
using System.IO;

namespace ClassicUO.Game.UI.Gumps.Login
{
    internal class LoginGump : Gump
    {
        private readonly ushort _buttonNormal;
        private readonly ushort _buttonOver;
        private readonly Checkbox _checkboxAutologin;
        private readonly Checkbox _checkboxSaveAccount;
        private readonly GothicStyleButtonLogin _nextArrow0;
        private readonly PasswordStbTextBox _passwordFake;
        private readonly StbTextBox _textboxAccount;
        private readonly GothicStyleCombobox _languageCombo;

        private float _time;
        private Texture2D LogoBackgroundImg = PNGLoader.Instance.GetImageTexture(Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client", "logodust.png"));


        public LoginGump(LoginScene scene) : base(0, 0)
        {
            X = LoginLayoutHelper.ContentOffsetX;
            Y = LoginLayoutHelper.ContentOffsetY;
            CanCloseWithRightClick = false;

            AcceptKeyboardInput = false;

            int offsetX, offsetY, offtextY;
            byte font;
            ushort hue;


            _buttonNormal = 0x5CD;
            _buttonOver = 0x5CB;
            UIManager.Add(new LoginBackground());
            const int LogoMaxWidth = 750;
            const int LogoMaxHeight = 188;
            if (LogoBackgroundImg != null)
            {
                float scale = Math.Min((float)LogoMaxWidth / LogoBackgroundImg.Width, (float)LogoMaxHeight / LogoBackgroundImg.Height);
                int logoW = (int)(LogoBackgroundImg.Width * scale);
                int logoH = (int)(LogoBackgroundImg.Height * scale);
                Add(new CustomGumpPic(
                    LoginLayoutHelper.CenterOffsetX(logoW),
                    LoginLayoutHelper.Y(240),
                    LogoBackgroundImg,
                    logoW,
                    logoH,
                    0
                ));
            }

            var loginLang = Language.Instance.Login;
            int langIndex = GetLanguageComboIndex(Settings.GlobalSettings.UILanguage);
            _languageCombo = new GothicStyleCombobox(
                LoginLayoutHelper.ContentWidth - 140,
                50,
                130,
                30,
                Language.SupportedUILanguages,
                langIndex >= 0 ? langIndex : 0
            );
            _languageCombo.OnSelectionChanged += (s, idx) =>
            {
                string code = Language.SupportedUILanguages[idx];
                Settings.GlobalSettings.UILanguage = code;
                Language.Load(code);
                Settings.GlobalSettings.Save();
                UIManager.GetGump<LoginBackground>()?.Dispose();
                UIManager.GetGump<LoginGump>()?.Dispose();
                UIManager.Add(new LoginGump(scene));
            };

            Add(new Label(string.Format(loginLang.UOVersionFormat, Settings.GlobalSettings.ClientVersion) + ".", false, 0x034E, font: 9)
            {
                X = LoginLayoutHelper.X(395),
                Y = LoginLayoutHelper.Y(700)
            });

            Add(new Label(string.Format(loginLang.VersionFormat, CUOEnviroment.Version), false, 0x034E, font: 9)
            {
                X = LoginLayoutHelper.X(395),
                Y = LoginLayoutHelper.Y(720)
            });


            // Arrow Button
            /*
            Add
            (
                _nextArrow0 = new Button((int)Buttons.NextArrow, 0x5CD, 0x5CC, 0x5CB)
                {
                    X = 455,
                    Y = 570,
                    ButtonAction = ButtonAction.Activate
                }
            );
            */

            Add(_nextArrow0 = new GothicStyleButtonLogin(
                LoginLayoutHelper.CenterOffsetX(120),
                LoginLayoutHelper.Y(570),
                120,
                40,
                loginLang.LoginButton,
                null,
                16
            ));

            _nextArrow0.OnClick += () =>
            {
                OnButtonClick(0);
            };


            //Add(_nextArrow0 = new DarkRedButton(
            //    x: 455,             // posição X
            //    y: 570,             // posição Y
            //    text: "Login", // texto do botão
            //    gumpNormal: 0x15E1, // ID do gump normal
            //    gumpHover: 0x15E3,  // ID do gump quando hover
            //    gumpPressed: 0x15E5,// ID do gump pressionado
            //    fontPath: "fonts/Arial.ttf", // caminho da fonte TTF
            //    fontSize: 16        // tamanho da fonte
            //));

            offsetX = LoginLayoutHelper.X(370);
            offsetY = LoginLayoutHelper.Y(430);
            offtextY = 40;

            Add(_checkboxAutologin = new Checkbox
                (
                    0x00D2,
                    0x00D3,
                    loginLang.Autologin,
                    9,
                    0x0481,
                    false
                )
                {
                    X = LoginLayoutHelper.X(510),
                    Y = LoginLayoutHelper.Y(510)
                }
            );

            Add(_checkboxSaveAccount = new Checkbox(
                    0x00D2,
                    0x00D3,
                    loginLang.SaveAccount,
                    9,
                    0x0481,
                    false
                )
                {
                    X = LoginLayoutHelper.X(375),
                    Y = LoginLayoutHelper.Y(510)
                }
            );

            font = 9;
            hue = 0x0481;



            // Account Text Input Background
            Add
            (
                new ResizePic(0x0BB8)
                {
                    X = offsetX,
                    Y = offsetY,
                    Width = 270,
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
                    Width = 270,
                    Height = 30
                }
            );

            offsetX += 7;

            Checkbox loginmusic_checkbox = new Checkbox(0x00D2, 0x00D3, loginLang.Music, font, hue, false)
            {
                X = LoginLayoutHelper.X(375),
                Y = LoginLayoutHelper.Y(535),
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
                _textboxAccount.SetTooltip(loginLang.RightClickAccountTooltip);
                _textboxAccount.MouseUp += (s, e) => { if (e.Button == MouseButtonType.Right) _textboxAccount.ContextMenu.Show(); };
            }

            _passwordFake.RealText = Crypter.Decrypt(Settings.GlobalSettings.Password);

            _checkboxSaveAccount.IsChecked = Settings.GlobalSettings.SaveAccount;
            _checkboxAutologin.IsChecked = Settings.GlobalSettings.AutoLogin;


            UOLabel _;
            HitBox _hit;

            Add(_ = new UOLabel(loginLang.Support, 1, 32, Assets.TEXT_ALIGN_TYPE.TS_LEFT, 200) { X = LoginLayoutHelper.X(30), Y = LoginLayoutHelper.Y(660) });
            Add(_hit = new HitBox(_.X, _.Y, _.Width, _.Height));
            _hit.MouseUp += (s, e) =>
            {
                Utility.Platforms.PlatformHelper.LaunchBrowser("https://github.com/dust765/ClassicUO/wiki");
            };

            var updateBtn = new GothicStyleButtonLogin(
                LoginLayoutHelper.X(30),
                LoginLayoutHelper.Y(628),
                320,
                24,
                "Update available - click to download and install",
                null,
                12
            )
            {
                IsVisible = UpdateManager.HasUpdate,
                TextColor = new Color(0, 150, 255)
            };
            updateBtn.OnClick += () => UpdateManager.StartUpdateAndExit();
            Add(updateBtn);

            if (!string.IsNullOrEmpty(_textboxAccount.Text))
            {
                _passwordFake.SetKeyboardFocus();
            }
            else
            {
                _textboxAccount.SetKeyboardFocus();
            }

            if (!UpdateManager.SkipUpdateCheck)
            {
                UpdateManager.UpdateStatusChanged += (s, e) =>
                {
                    if (UpdateManager.HasUpdate)
                        updateBtn.IsVisible = true;
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

        internal void TrySubmitFromController()
        {
            LoginScene ls = Client.Game.GetScene<LoginScene>();
            if (ls == null || ls.CurrentLoginStep != LoginSteps.Main || IsDisposed || _textboxAccount == null || _passwordFake == null)
                return;
            SaveCheckboxStatus();
            ls.Connect(_textboxAccount.Text, _passwordFake.RealText);
        }

        private static int GetLanguageComboIndex(string uiLang)
        {
            if (string.IsNullOrEmpty(uiLang)) return 0;
            string upper = uiLang.ToUpperInvariant();
            for (int i = 0; i < Language.SupportedUILanguages.Length; i++)
            {
                if (Language.SupportedUILanguages[i].Equals(upper, System.StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return 0;
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

            X = LoginLayoutHelper.ContentOffsetX;
            Y = LoginLayoutHelper.ContentOffsetY;
            base.Update();

            if (_time < Time.Ticks)
            {
                _time = (float)Time.Ticks + 1000;

                // _nextArrow0.ButtonGraphicNormal = _nextArrow0.ButtonGraphicNormal == _buttonNormal ? _buttonOver : _buttonNormal;
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
                    UIManager.GetGump<LoginBackground>()?.Dispose();
                    break;

                case Buttons.Quit:
                    Client.Game.Exit();

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