#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
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

using System.Linq;

using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

using SDL2;

namespace ClassicUO.Game.UI.Gumps.Login
{
    internal class ServerSelectionGump : Gump
    {
        private const ushort SELECTED_COLOR = 0x0021;
        private const ushort NORMAL_COLOR = 0x034F;

        public ServerSelectionGump() : base(0,0)
        {
            //AddChildren(new LoginBackground(true));

            Add(new Button((int) Buttons.Prev, 0x15A1, 0x15A3, 0x15A2)
            {
                X = 586, Y = 445, ButtonAction = ButtonAction.Activate
            });

            Add(new Button((int) Buttons.Next, 0x15A4, 0x15A6, 0x15A5)
            {
                X = 610, Y = 445, ButtonAction = ButtonAction.Activate
            });

            if (FileManager.ClientVersion >= ClientVersions.CV_500A)
            {
                ushort textColor = 0xFFFF;

                Add(new Label(FileManager.Cliloc.GetString(1044579), true, textColor, font: 1)
                {
                    X = 155, Y = 70
                }); // "Select which shard to play on:"

                Add(new Label(FileManager.Cliloc.GetString(1044577), true, textColor, font: 1)
                {
                    X = 400, Y = 70
                }); // "Latency:"

                Add(new Label(FileManager.Cliloc.GetString(1044578), true, textColor, font: 1)
                {
                    X = 470, Y = 70
                }); // "Packet Loss:"

                Add(new Label(FileManager.Cliloc.GetString(1044580), true, textColor, font: 1)
                {
                    X = 153, Y = 368
                }); // "Sort by:"
            }
            else
            {
                ushort textColor = 0x0481;

                Add(new Label("Select which shard to play on:", true, textColor, font: 9)
                {
                    X = 155, Y = 70
                });

                Add(new Label("Latency:", true, textColor, font: 9)
                {
                    X = 400, Y = 70
                });

                Add(new Label("Packet Loss:", true, textColor, font: 9)
                {
                    X = 470, Y = 70
                });

                Add(new Label("Sort by:", true, textColor, font: 9)
                {
                    X = 153, Y = 368
                });
            }

            Add(new Button((int) Buttons.SortTimeZone, 0x093B, 0x093C, 0x093D)
            {
                X = 230, Y = 366
            });

            Add(new Button((int) Buttons.SortFull, 0x093E, 0x093F, 0x0940)
            {
                X = 338, Y = 366
            });

            Add(new Button((int) Buttons.SortConnection, 0x0941, 0x0942, 0x0943)
            {
                X = 446, Y = 366
            });

            // World Pic Bg
            Add(new GumpPic(150, 390, 0x0589, 0));

            // Earth
            Add(new Button((int) Buttons.Earth, 0x15E8, 0x15EA, 0x15E9)
            {
                X = 160, Y = 400
            });

            // Sever Scroll Area Bg
            Add(new ResizePic(0x0DAC)
            {
                X = 150, Y = 90, Width = 393 - 14, Height = 271
            });
            // Sever Scroll Area
            ScrollArea scrollArea = new ScrollArea(150, 90, 393, 271, true);
            LoginScene loginScene = Engine.SceneManager.GetScene<LoginScene>();

            foreach (ServerListEntry server in loginScene.Servers)
                scrollArea.Add(new ServerEntryGump(server));

            Add(scrollArea);

            if (loginScene.Servers.Length > 0)
            {
                if (loginScene.Servers.Last().Index < loginScene.Servers.Count())
                {
                    Add(new Label(loginScene.Servers.Last().Name, false, 0x0481, font: 9)
                    {
                        X = 243, Y = 420
                    });
                }
                else
                {
                    Add(new Label(loginScene.Servers.First().Name, false, 0x0481, font: 9)
                    {
                        X = 243, Y = 420
                    });
                }
            }

            AcceptKeyboardInput = true;
        }

        public override void OnButtonClick(int buttonID)
        {
            LoginScene loginScene = Engine.SceneManager.GetScene<LoginScene>();

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
                        if (loginScene.Servers.Any())
                            loginScene.SelectServer((byte) loginScene.Servers[(Engine.GlobalSettings.LastServerNum-1)].Index);
                        break;
                    case Buttons.Prev:
                        loginScene.StepBack();
                        break;
                }
            }
        }

        protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            if (key == SDL.SDL_Keycode.SDLK_RETURN)
            {
                LoginScene loginScene = Engine.SceneManager.GetScene<LoginScene>();
                if (loginScene.Servers.Any())
                    loginScene.SelectServer((byte)loginScene.Servers[(Engine.GlobalSettings.LastServerNum - 1)].Index);
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
            private readonly RenderedText _labelName;
            private readonly RenderedText _labelPacketLoss;
            private readonly RenderedText _labelPing;

            public ServerEntryGump(ServerListEntry entry)
            {
                _buttonId = entry.Index;
                _labelName = CreateRenderedText(entry.Name + _buttonId);
                _labelPing = CreateRenderedText("-");
                _labelPacketLoss = CreateRenderedText("-");
                _labelName.CreateTexture();
                _labelPing.CreateTexture();
                _labelPacketLoss.CreateTexture();
                AcceptMouseInput = true;
                Width = 393;

                Height = new[]
                {
                    _labelName.Height, _labelPing.Height, _labelPacketLoss.Height
                }.Max() + 10;
                X = 0;
                Y = 0;
            }

            private RenderedText CreateRenderedText(string text)
            {
                return new RenderedText
                {
                    Text = text,
                    Font = 5,
                    IsUnicode = false,
                    Hue = _buttonId == Engine.GlobalSettings.LastServerNum ? SELECTED_COLOR : NORMAL_COLOR,
                    Align = TEXT_ALIGN_TYPE.TS_LEFT,
                    MaxWidth = 0
                };
            }

            public override bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
            {
                if (IsDisposed)
                    return false;
                _labelName.Draw(batcher, position + new Point(74, 10));
                _labelPing.Draw(batcher, position + new Point(250, 10));
                _labelPacketLoss.Draw(batcher, position + new Point(310, 10));

                return base.Draw(batcher, position, hue);
            }

            protected override void OnMouseOver(int x, int y)
            {
                _labelName.Hue = SELECTED_COLOR;
                _labelPing.Hue = SELECTED_COLOR;
                _labelPacketLoss.Hue = SELECTED_COLOR;
                _labelName.CreateTexture();
                _labelPing.CreateTexture();
                _labelPacketLoss.CreateTexture();
                base.OnMouseOver(x, y);
            }

            protected override void OnMouseExit(int x, int y)
            {
                _labelName.Hue = NORMAL_COLOR;
                _labelPing.Hue = NORMAL_COLOR;
                _labelPacketLoss.Hue = NORMAL_COLOR;
                _labelName.CreateTexture();
                _labelPing.CreateTexture();
                _labelPacketLoss.CreateTexture();
                base.OnMouseExit(x, y);
            }

            protected override void OnMouseClick(int x, int y, MouseButton button)
            {
                if (button == MouseButton.Left) OnButtonClick((int) Buttons.Server + _buttonId);
            }

            public override void Dispose()
            {
                base.Dispose();
                _labelName.Dispose();
                _labelPing.Dispose();
                _labelPacketLoss.Dispose();
            }
        }
    }
}