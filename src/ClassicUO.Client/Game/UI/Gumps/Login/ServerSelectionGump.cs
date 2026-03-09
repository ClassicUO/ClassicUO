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

using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Resources;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using SDL3;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using ClassicUO.Game.Managers;
using Microsoft.Xna.Framework;
using ClassicUO.Game.GameObjects;
using System;


namespace ClassicUO.Game.UI.Gumps.Login
{
    internal class ServerSelectionGump : Gump
    {
        private const ushort SELECTED_COLOR = 0x0481;
        private const ushort NORMAL_COLOR = 0x0481;
        private GothicStyleButtonLogin button;
        private Texture2D LogoBackgroundImg = PNGLoader.Instance.GetImageTexture(Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client", "logodust.png"));

        public ServerSelectionGump() : base(0, 0)
        {
            X = LoginLayoutHelper.ContentOffsetX;
            Y = LoginLayoutHelper.ContentOffsetY;
            // Background
            UIManager.Add(new LoginBackground());

            const int LogoMaxWidth = 800;
            const int LogoMaxHeight = 140;
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
            Add(button = new GothicStyleButtonLogin(
                LoginLayoutHelper.X(30),
                LoginLayoutHelper.Y(680),
                120,
                40,
                loginLang.Back,
                null,
                16
            ));
            button.OnClick += () => OnButtonClick(0);

            Add(button = new GothicStyleButtonLogin(
                LoginLayoutHelper.ContentWidth - 30 - 120,
                LoginLayoutHelper.Y(680),
                120,
                40,
                loginLang.Next,
                null,
                16
            ));

            button.OnClick += () =>
            {
                OnButtonClick(1);
            };



            if (Client.Version >= ClientVersion.CV_500A)
            {
                Add(new UOLabel(ClilocLoader.Instance.GetString(1044579), 1, 32, Assets.TEXT_ALIGN_TYPE.TS_LEFT, 300, FontStyle.BlackBorder) { X = LoginLayoutHelper.X(210), Y = LoginLayoutHelper.Y(70) });

                if (CUOEnviroment.NoServerPing == false)
                {
                    Add(new UOLabel(ClilocLoader.Instance.GetString(1044577), 1, 32, Assets.TEXT_ALIGN_TYPE.TS_LEFT, 300, FontStyle.BlackBorder) { X = LoginLayoutHelper.X(650), Y = LoginLayoutHelper.Y(70) });
                   

                }


            }
            else
            {
                Add(new UOLabel(loginLang.SelectWhichShardToPlayOn, 1, 32, Assets.TEXT_ALIGN_TYPE.TS_LEFT, 300, FontStyle.BlackBorder) { X = LoginLayoutHelper.X(210), Y = LoginLayoutHelper.Y(70) });
                Add(new UOLabel(loginLang.Latency, 1, 32, Assets.TEXT_ALIGN_TYPE.TS_LEFT, 300, FontStyle.BlackBorder) { X = LoginLayoutHelper.X(650), Y = LoginLayoutHelper.Y(70) });
            }

            ScrollArea scrollArea = new ScrollArea(
                LoginLayoutHelper.X(150),
                LoginLayoutHelper.Y(90),
                LoginLayoutHelper.W(600),
                LoginLayoutHelper.H(500),
                true
            );

            DataBox databox = new DataBox(0, 0, 1, 1);
            databox.WantUpdateSize = true;
            LoginScene loginScene = Client.Game.GetScene<LoginScene>();

            scrollArea.ScissorRectangle.Y = 16;
            scrollArea.ScissorRectangle.Height = -32;

            foreach (ServerListEntry server in loginScene.Servers)
            {
                databox.Add(new ServerEntryGump(server, 5, NORMAL_COLOR, SELECTED_COLOR));
            }

            databox.ReArrangeChildren();

            Add(scrollArea);
            scrollArea.Add(databox);

            Add(new AlphaBlendControl
            {
                X = LoginLayoutHelper.X(210),
                Y = LoginLayoutHelper.Y(620),
                Width = LoginLayoutHelper.W(540),
                Height = LoginLayoutHelper.H(85),
                Hue = 0x0000
            });

            Add(new Button((int)Buttons.Earth, 0x15E8, 0x15EA, 0x15E9)
            {
                X = LoginLayoutHelper.X(243),
                Y = LoginLayoutHelper.Y(630),
                ButtonAction = ButtonAction.Activate
            });

            Add(new UOLabel("Last server is played:", 1, 32, Assets.TEXT_ALIGN_TYPE.TS_LEFT, 300, FontStyle.BlackBorder) { X = LoginLayoutHelper.X(310), Y = LoginLayoutHelper.Y(640) });

            if (loginScene.Servers.Length != 0)
            {
                int index = loginScene.GetServerIndexFromSettings();
                Add(new UOLabel(loginScene.Servers[index].Name, 1, 32, Assets.TEXT_ALIGN_TYPE.TS_LEFT, 300, FontStyle.BlackBorder) { X = LoginLayoutHelper.X(310), Y = LoginLayoutHelper.Y(660) });
            }

            AcceptKeyboardInput = true;
            CanCloseWithRightClick = false;
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

        public override void OnButtonClick(int buttonID)
        {
            LoginScene loginScene = Client.Game.GetScene<LoginScene>();

            if (buttonID >= (int) Buttons.Server)
            {
                int index = buttonID - (int) Buttons.Server;
                loginScene.SelectServer((byte) index);
            }
            else
            {
                switch ((Buttons) buttonID)
                {
                    case Buttons.Next:
                    case Buttons.Earth:

                        if (loginScene.Servers.Length != 0)
                        {
                            int index = loginScene.GetServerIndexFromSettings();

                            loginScene.SelectServer((byte) loginScene.Servers[index].Index);
                        }
                        UIManager.GetGump<SelectServerBackground>()?.Dispose();
                        UIManager.Add(new CharacterSelectionBackground());

                        break;

                    case Buttons.Prev:
                        UIManager.GetGump<SelectServerBackground>()?.Dispose();
                        UIManager.GetGump<CharacterSelectionBackground>()?.Dispose();
                        loginScene.StepBack();

                        break;
                }
            }
        }

        protected override void OnControllerButtonUp(SDL.SDL_GamepadButton button)
        {
            base.OnControllerButtonUp(button);
            if (button == SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_SOUTH)
            {
                LoginScene loginScene = Client.Game.GetScene<LoginScene>();

                if (loginScene.Servers?.Any(s => s != null) ?? false)
                {
                    int index = loginScene.GetServerIndexFromSettings();

                    loginScene.SelectServer((byte)loginScene.Servers[index].Index);
                }
            }
        }

        protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            if (key == SDL.SDL_Keycode.SDLK_RETURN || key == SDL.SDL_Keycode.SDLK_KP_ENTER)
            {
                LoginScene loginScene = Client.Game.GetScene<LoginScene>();

                if (loginScene.Servers?.Any(s => s != null) ?? false)
                {
                    int index = loginScene.GetServerIndexFromSettings();

                    loginScene.SelectServer((byte) loginScene.Servers[index].Index);
                }
            }
        }

        private enum Buttons
        {
            Prev,
            Next,
            SortTimeZone,
            SortFull,
            SortConnection,
            Earth,
            Server = 99
        }


        private class ServerEntryGump : Control
        {
            private readonly int _buttonId;
            private readonly ServerListEntry _entry;
            private readonly UOLabel _server_ping;
            private UOLabel _serverName;
            private AlphaBlendControl _alphaBlendControl;
            private uint _pingCheckTime = 0;

            public ServerEntryGump(ServerListEntry entry, byte font, ushort normal_hue, ushort selected_hue)
            {
                _entry = entry;

                _buttonId = entry.Index;

                Add
               (
                  _alphaBlendControl = new AlphaBlendControl
                  {
                      X = 30,
                      Width = 640,
                      Height = 30,
                      Hue = 0 // Cor preta (0x0000)
                  }
               );


                Add
                (
                    _serverName = new UOLabel(entry.Name, 1, 32, Assets.TEXT_ALIGN_TYPE.TS_LEFT, 300) { X = 74, Y = 6 }
                    
                );

                Add
                (
                    _server_ping = new UOLabel(CUOEnviroment.NoServerPing ? string.Empty : "-", 1, 32, Assets.TEXT_ALIGN_TYPE.TS_LEFT, 70) { X = 500, Y = 6 }
        
                );



                AcceptMouseInput = true;
                Width = 620;
                Height = 31;

                WantUpdateSize = false;
            }

            protected override void OnMouseEnter(int x, int y)
            {
                base.OnMouseEnter(x, y);
                _alphaBlendControl.Hue = 0x7EA; // Cor preta original
            }

            protected override void OnMouseExit(int x, int y)
            {
                base.OnMouseExit(x, y);

                _alphaBlendControl.Hue = 0x0000; // Cor preta original
            }

            protected override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                if (button == MouseButtonType.Left)
                {
                    OnButtonClick((int) Buttons.Server + _buttonId);
                }
            }

            public override void Update()
            {
                base.Update();

                if (CUOEnviroment.NoServerPing == false && _pingCheckTime < Time.Ticks)
                {
                    _pingCheckTime = Time.Ticks + 2000;
                    _entry.DoPing();

                    switch (_entry.PingStatus)
                    {
                        case IPStatus.Success:
                            _server_ping.Text = _entry.Ping == -1 ? "-" : _entry.Ping.ToString();

                            break;

                        case IPStatus.DestinationNetworkUnreachable:
                        case IPStatus.DestinationHostUnreachable:
                        case IPStatus.DestinationProtocolUnreachable:
                        case IPStatus.DestinationPortUnreachable:
                        case IPStatus.DestinationUnreachable:
                            _server_ping.Text = "unreach.";

                            break;

                        case IPStatus.TimedOut:
                            _server_ping.Text = "time out";

                            break;

                        default:
                            _server_ping.Text = $"unk. [{(int) _entry.PingStatus}]";

                            break;
                    }

                }
            }
        }
    }
}