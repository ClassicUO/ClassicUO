#region license
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.IO.Resources;
using ClassicUO.Network;

namespace ClassicUO.Game.UI.Gumps
{
    class PartyGump : Gump
    {
        enum Buttons
        {
            OK,
            Cancel,
            SendMessage,
            LootType,
            Leave,
            Add,
            TellMember,
            KickMember = TellMember + 10
        }

        public PartyGump(int x, int y, bool canloot) : base(0, 0)
        {
            X = x;
            Y = y;
            CanLoot = canloot;

            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;

            BuildGump();
        }

        public bool CanLoot;

        public void Update()
        {
            Clear();
            BuildGump();
        }

        private void BuildGump()
        {
            Add(new ResizePic(0x0A28)
            {
                Width = 450,
                Height = 480
            });

            Label text = new Label("Tell", false, 0x0386, font: 1)
            {
                X = 40,
                Y = 30
            };
            Add(text);

            text = new Label("Kick", false, 0x0386, font: 1)
            {
                X = 80,
                Y = 30
            };
            Add(text);

            text = new Label("Party Manifest", false, 0x0386, font: 2)
            {
                X = 153,
                Y = 20
            };
            Add(text);

            bool isLeader = World.Party.Leader == 0 || World.Party.Leader == World.Player;
            bool isMemeber = World.Party.Leader != 0 && World.Party.Leader != World.Player;

            int yPtr = 48;

            for (int i = 0; i < 10; i++)
            {
                Add(new Button((int) (Buttons.TellMember + i), 0x0FAB, 0x0FAD, 0x0FAC)
                {
                    X = 40,
                    Y = yPtr + 2,
                    ButtonAction = ButtonAction.Activate,
                });

                if (isLeader)
                {
                    Add(new Button((int) (Buttons.KickMember + i), 0x0FB1, 0x0FB3, 0x0FB2)
                    {
                        X = 80,
                        Y = yPtr + 2,
                        ButtonAction = ButtonAction.Activate,
                    });
                }

                Add(new GumpPic(130, yPtr, 0x0475, 0));

                string name = "";

                if (World.Party.Members[i] != null && World.Party.Members[i].Name != null)
                    name = World.Party.Members[i].Name;

                text = new Label(name, false, 0x0386, font: 2, maxwidth: 250, align: TEXT_ALIGN_TYPE.TS_CENTER)
                {
                    X = 140,
                    Y = yPtr + 1
                };
                Add(text);

                yPtr += 25;
            }

            Add(new Button((int) (Buttons.SendMessage), 0x0FAB, 0x0FAD, 0x0FAC)
            {
                X = 70,
                Y = 307,
                ButtonAction = ButtonAction.Activate,
            });

            text = new Label("Send the party a message", false, 0x0386, font: 2)
            {
                X = 110,
                Y = 307
            };
            Add(text);

            if (CanLoot)
            {
                Add(new Button((int) (Buttons.LootType), 0x0FA2, 0x0FA2, 0x0FA2)
                {
                    X = 70,
                    Y = 334,
                    ButtonAction = ButtonAction.Activate,
                });

                text = new Label("Party can loot me", false, 0x0386, font: 2)
                {
                    X = 110,
                    Y = 334
                };
                Add(text);
            }
            else
            {
                Add(new Button((int) (Buttons.LootType), 0x0FA9, 0x0FA9, 0x0FA9)
                {
                    X = 70,
                    Y = 334,
                    ButtonAction = ButtonAction.Activate,
                });

                text = new Label("Party CANNOT loot me", false, 0x0386, font: 2)
                {
                    X = 110,
                    Y = 334
                };
                Add(text);
            }


            Add(new Button((int) (Buttons.Leave), 0x0FAE, 0x0FB0, 0x0FAF)
            {
                X = 70,
                Y = 360,
                ButtonAction = ButtonAction.Activate,
            });

            if (isMemeber)
            {
                text = new Label("Leave the party", false, 0x0386, font: 2)
                {
                    X = 110,
                    Y = 360
                };
                Add(text);
            }
            else
            {
                text = new Label("Disband the party", false, 0x0386, font: 2)
                {
                    X = 110,
                    Y = 360
                };
                Add(text);
            }

            if (isLeader)
            {
                Add(new Button((int) (Buttons.Add), 0x0FA8, 0x0FAA, 0x0FA9)
                {
                    X = 70,
                    Y = 385,
                    ButtonAction = ButtonAction.Activate,
                });

                text = new Label("Add New Member", false, 0x0386, font: 2)
                {
                    X = 110,
                    Y = 385
                };
                Add(text);
            }

            Add(new Button((int) (Buttons.OK), 0x00F9, 0x00F8, 0x00F7)
            {
                X = 130,
                Y = 430,
                ButtonAction = ButtonAction.Activate,
            });
            Add(new Button((int) (Buttons.Cancel), 0x00F3, 0x00F1, 0x00F2)
            {
                X = 236,
                Y = 430,
                ButtonAction = ButtonAction.Activate,
            });
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((Buttons) buttonID)
            {
                case Buttons.OK:
                    if (World.Party.Leader != 0 && World.Party.CanLoot != CanLoot)
                    {
                        World.Party.CanLoot = CanLoot;
                        NetClient.Socket.Send(new PPartyChangeLootTypeRequest(CanLoot));
                    }
                    Dispose();
                    break;
                case Buttons.Cancel:
                    Dispose();
                    break;
                case Buttons.SendMessage:
                    if (World.Party.Leader == 0)
                    {
                        GameActions.Print("You are not in a party.", 0, MessageType.System, 3, false);
                    }
                    else
                    {
                        UIManager.SystemChat.TextBoxControl.SetText("/");
                    }
                    break;
                case Buttons.LootType:
                    CanLoot = !CanLoot;
                    Update();
                    break;
                case Buttons.Leave:
                    if (World.Party.Leader == 0)
                    {
                        GameActions.Print("You are not in a party.", 0, MessageType.System, 3, false);
                    }
                    else
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            if (World.Party.Members[i] != null && World.Party.Members[i].Serial != 0)
                            {
                                NetClient.Socket.Send(new PPartyRemoveRequest(World.Party.Members[i].Serial));
                            }
                        }
                    }
                    break;
                case Buttons.Add:
                    if (World.Party.Leader == 0 || World.Party.Leader == World.Player)
                    {
                        NetClient.Socket.Send(new PPartyInviteRequest());
                    }
                    break;

                default:
                    if (buttonID >= (int) Buttons.TellMember && buttonID < (int) Buttons.KickMember)
                    {
                        int index = (int) (buttonID - Buttons.TellMember);

                        if (World.Party.Members[index] == null || World.Party.Members[index].Serial == 0)
                        {
                            GameActions.Print("There is no one in that party slot.", 0, MessageType.System, 3, false);
                        }
                        else
                        {
                            //UIManager.SystemChat.textBox.SetText($"/{index + 1}");
                            //UIManager.SystemChat.Mode = ChatMode.Party;
                            UIManager.SystemChat.TextBoxControl.SetText($"/{index + 1} ");
                        }
                    }
                    else if (buttonID >= (int) Buttons.KickMember)
                    {
                        int index = (int) (buttonID - Buttons.KickMember);

                        if (World.Party.Members[index] == null || World.Party.Members[index].Serial == 0)
                        {
                            GameActions.Print("There is no one in that party slot.", 0, MessageType.System, 3, false);
                        }
                        else
                        {
                            NetClient.Socket.Send(new PPartyRemoveRequest(World.Party.Members[index].Serial));
                        }
                    }
                    break;
            }
        }
    }
}
