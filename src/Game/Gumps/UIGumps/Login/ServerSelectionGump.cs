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

using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.UIGumps.Login
{
    internal class ServerSelectionGump : Control
    {
        public ServerSelectionGump()
        {
            AddChildren(new Button((int) Buttons.Prev, 0x15A1, 0x15A3, 0x15A2)
            {
                X = 586, Y = 445, ButtonAction = ButtonAction.Activate
            });

            AddChildren(new Button((int) Buttons.Next, 0x15A4, 0x15A6, 0x15A5)
            {
                X = 610, Y = 445, ButtonAction = ButtonAction.Activate
            });

            if (FileManager.ClientVersion >= ClientVersions.CV_500A)
            {
                ushort textColor = 0xFFFF;

                AddChildren(new Label(Cliloc.GetString(1044579), true, textColor, font: 1)
                {
                    X = 155, Y = 70
                }); // "Select which shard to play on:"

                AddChildren(new Label(Cliloc.GetString(1044577), true, textColor, font: 1)
                {
                    X = 400, Y = 70
                }); // "Latency:"

                AddChildren(new Label(Cliloc.GetString(1044578), true, textColor, font: 1)
                {
                    X = 470, Y = 70
                }); // "Packet Loss:"

                AddChildren(new Label(Cliloc.GetString(1044580), true, textColor, font: 1)
                {
                    X = 153, Y = 368
                }); // "Sort by:"
            }
            else
            {
                ushort textColor = 0x0481;

                AddChildren(new Label("Select which shard to play on:", true, textColor, font: 9)
                {
                    X = 155, Y = 70
                });

                AddChildren(new Label("Latency:", true, textColor, font: 9)
                {
                    X = 400, Y = 70
                });

                AddChildren(new Label("Packet Loss:", true, textColor, font: 9)
                {
                    X = 470, Y = 70
                });

                AddChildren(new Label("Sort by:", true, textColor, font: 9)
                {
                    X = 153, Y = 368
                });
            }

            AddChildren(new Button((int) Buttons.SortTimeZone, 0x093B, 0x093C, 0x093D)
            {
                X = 230, Y = 366
            });

            AddChildren(new Button((int) Buttons.SortFull, 0x093E, 0x093F, 0x0940)
            {
                X = 338, Y = 366
            });

            AddChildren(new Button((int) Buttons.SortConnection, 0x0941, 0x0942, 0x0943)
            {
                X = 446, Y = 366
            });

            // World Pic Bg
            AddChildren(new GumpPic(150, 390, 0x0589, 0));

            // Earth
            AddChildren(new Button((int) Buttons.Earth, 0x15E8, 0x15EA, 0x15E9)
            {
                X = 160, Y = 400
            });

            // Sever Scroll Area Bg
            AddChildren(new ResizePic(0x0DAC)
            {
                X = 150, Y = 90, Width = 393 - 14, Height = 271
            });
            // Sever Scroll Area
            ScrollArea scrollArea = new ScrollArea(150, 90, 393, 271, true);
            LoginScene loginScene = SceneManager.GetScene<LoginScene>();
            foreach (ServerListEntry server in loginScene.Servers) scrollArea.AddChildren(new ServerEntryGump(server));
            AddChildren(scrollArea);

            if (loginScene.Servers.Count() > 0)
            {
                if (loginScene.Servers.Last().Index < loginScene.Servers.Count())
                {
                    AddChildren(new Label(loginScene.Servers.Last().Name, false, 0x0481, font: 9)
                    {
                        X = 243, Y = 420
                    });
                }
                else
                {
                    AddChildren(new Label(loginScene.Servers.First().Name, false, 0x0481, font: 9)
                    {
                        X = 243, Y = 420
                    });
                }
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            LoginScene loginScene = SceneManager.GetScene<LoginScene>();

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

                        if (loginScene.Servers.Count() > 0)
                            loginScene.SelectServer(0);

                        break;
                    case Buttons.Prev:
                        loginScene.StepBack();

                        break;
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
            private readonly ushort _hoverColor = 0x0021;
            private readonly RenderedText _labelName;
            private readonly RenderedText _labelPacketLoss;
            private readonly RenderedText _labelPing;
            private readonly ushort _normalColor = 0x034F;

            public ServerEntryGump(ServerListEntry entry)
            {
                _buttonId = entry.Index;
                _labelName = CreateRenderedText(entry.Name);
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
                    Hue = _normalColor,
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
                _labelName.Hue = _hoverColor;
                _labelPing.Hue = _hoverColor;
                _labelPacketLoss.Hue = _hoverColor;
                _labelName.CreateTexture();
                _labelPing.CreateTexture();
                _labelPacketLoss.CreateTexture();
                base.OnMouseOver(x, y);
            }

            protected override void OnMouseExit(int x, int y)
            {
                _labelName.Hue = _normalColor;
                _labelPing.Hue = _normalColor;
                _labelPacketLoss.Hue = _normalColor;
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