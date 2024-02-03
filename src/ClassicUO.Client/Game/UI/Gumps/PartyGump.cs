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

using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Assets;
using ClassicUO.Network;
using ClassicUO.Resources;

namespace ClassicUO.Game.UI.Gumps
{
    internal class PartyGump : Gump
    {
        public PartyGump(World world, int x, int y, bool canloot) : base(world, 0, 0)
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


        protected override void UpdateContents()
        {
            Clear();
            BuildGump();
        }

        private void BuildGump()
        {
            Add
            (
                new ResizePic(0x0A28)
                {
                    Width = 450,
                    Height = 480
                }
            );

            Add
            (
                new Label(ResGumps.Tell, false, 0x0386, font: 1)
                {
                    X = 40,
                    Y = 30
                }
            );

            Add
            (
                new Label(ResGumps.Kick, false, 0x0386, font: 1)
                {
                    X = 80,
                    Y = 30
                }
            );

            Add
            (
                new Label(ResGumps.PartyManifest, false, 0x0386, font: 2)
                {
                    X = 153,
                    Y = 20
                }
            );

            bool isLeader = World.Party.Leader == 0 || World.Party.Leader == World.Player;
            bool isMemeber = World.Party.Leader != 0 && World.Party.Leader != World.Player;

            int yPtr = 48;

            for (int i = 0; i < 10; i++)
            {
                Add
                (
                    new Button((int) (Buttons.TellMember + i), 0x0FAB, 0x0FAD, 0x0FAC)
                    {
                        X = 40,
                        Y = yPtr + 2,
                        ButtonAction = ButtonAction.Activate
                    }
                );

                if (isLeader)
                {
                    Add
                    (
                        new Button((int) (Buttons.KickMember + i), 0x0FB1, 0x0FB3, 0x0FB2)
                        {
                            X = 80,
                            Y = yPtr + 2,
                            ButtonAction = ButtonAction.Activate
                        }
                    );
                }

                Add(new GumpPic(130, yPtr, 0x0475, 0));

                string name = "";

                if (World.Party.Members[i] != null && World.Party.Members[i].Name != null)
                {
                    name = World.Party.Members[i].Name;
                }

                Add
                (
                    new Label
                    (
                        name,
                        false,
                        0x0386,
                        font: 2,
                        maxwidth: 250,
                        align: TEXT_ALIGN_TYPE.TS_CENTER
                    )
                    {
                        X = 140,
                        Y = yPtr + 1
                    }
                );

                yPtr += 25;
            }

            Add
            (
                new Button((int) Buttons.SendMessage, 0x0FAB, 0x0FAD, 0x0FAC)
                {
                    X = 70,
                    Y = 307,
                    ButtonAction = ButtonAction.Activate
                }
            );

            Add
            (
                new Label(ResGumps.SendThePartyAMessage, false, 0x0386, font: 2)
                {
                    X = 110,
                    Y = 307
                }
            );

            if (CanLoot)
            {
                Add
                (
                    new Button((int) Buttons.LootType, 0x0FA2, 0x0FA2, 0x0FA2)
                    {
                        X = 70,
                        Y = 334,
                        ButtonAction = ButtonAction.Activate
                    }
                );

                Add
                (
                    new Label(ResGumps.PartyCanLootMe, false, 0x0386, font: 2)
                    {
                        X = 110,
                        Y = 334
                    }
                );
            }
            else
            {
                Add
                (
                    new Button((int) Buttons.LootType, 0x0FA9, 0x0FA9, 0x0FA9)
                    {
                        X = 70,
                        Y = 334,
                        ButtonAction = ButtonAction.Activate
                    }
                );

                Add
                (
                    new Label(ResGumps.PartyCannotLootMe, false, 0x0386, font: 2)
                    {
                        X = 110,
                        Y = 334
                    }
                );
            }


            Add
            (
                new Button((int) Buttons.Leave, 0x0FAE, 0x0FB0, 0x0FAF)
                {
                    X = 70,
                    Y = 360,
                    ButtonAction = ButtonAction.Activate
                }
            );

            if (isMemeber)
            {
                Add
                (
                    new Label(ResGumps.LeaveTheParty, false, 0x0386, font: 2)
                    {
                        X = 110,
                        Y = 360
                    }
                );
            }
            else
            {
                Add
                (
                    new Label(ResGumps.DisbandTheParty, false, 0x0386, font: 2)
                    {
                        X = 110,
                        Y = 360
                    }
                );
            }

            if (isLeader)
            {
                Add
                (
                    new Button((int) Buttons.Add, 0x0FA8, 0x0FAA, 0x0FA9)
                    {
                        X = 70,
                        Y = 385,
                        ButtonAction = ButtonAction.Activate
                    }
                );

                Add
                (
                    new Label(ResGumps.AddNewMember, false, 0x0386, font: 2)
                    {
                        X = 110,
                        Y = 385
                    }
                );
            }

            Add
            (
                new Button((int) Buttons.OK, 0x00F9, 0x00F8, 0x00F7)
                {
                    X = 130,
                    Y = 430,
                    ButtonAction = ButtonAction.Activate
                }
            );

            Add
            (
                new Button((int) Buttons.Cancel, 0x00F3, 0x00F1, 0x00F2)
                {
                    X = 236,
                    Y = 430,
                    ButtonAction = ButtonAction.Activate
                }
            );
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((Buttons) buttonID)
            {
                case Buttons.OK:
                    if (World.Party.Leader != 0 && World.Party.CanLoot != CanLoot)
                    {
                        World.Party.CanLoot = CanLoot;
                        NetClient.Socket.Send_PartyChangeLootTypeRequest(CanLoot);
                    }

                    Dispose();

                    break;

                case Buttons.Cancel:
                    Dispose();

                    break;

                case Buttons.SendMessage:
                    if (World.Party.Leader == 0)
                    {
                        GameActions.Print
                        (
                            World,
                            ResGumps.YouAreNotInAParty,
                            0,
                            MessageType.System,
                            3,
                            false
                        );
                    }
                    else
                    {
                        UIManager.SystemChat.TextBoxControl.SetText("/");
                    }

                    break;

                case Buttons.LootType:
                    CanLoot = !CanLoot;
                    RequestUpdateContents();

                    break;

                case Buttons.Leave:
                    if (World.Party.Leader == 0)
                    {
                        GameActions.Print
                        (
                            World,
                            ResGumps.YouAreNotInAParty,
                            0,
                            MessageType.System,
                            3,
                            false
                        );
                    }
                    else
                    {
                        // NetClient.Socket.Send(new PPartyRemoveRequest(World.Player));
                        GameActions.RequestPartyQuit(World.Player);
                        //for (int i = 0; i < 10; i++)
                        //{
                        //    if (World.Party.Members[i] != null && World.Party.Members[i].Serial != 0)
                        //    {
                        //        NetClient.Socket.Send(new PPartyRemoveRequest(World.Party.Members[i].Serial));
                        //    }
                        //}
                    }

                    break;

                case Buttons.Add:
                    if (World.Party.Leader == 0 || World.Party.Leader == World.Player)
                    {
                        NetClient.Socket.Send_PartyInviteRequest();
                    }

                    break;

                default:
                    if (buttonID >= (int) Buttons.TellMember && buttonID < (int) Buttons.KickMember)
                    {
                        int index = (int) (buttonID - Buttons.TellMember);

                        if (World.Party.Members[index] == null || World.Party.Members[index].Serial == 0)
                        {
                            GameActions.Print
                            (
                                World,
                                ResGumps.ThereIsNoOneInThatPartySlot,
                                0,
                                MessageType.System,
                                3,
                                false
                            );
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
                            GameActions.Print
                            (
                                World,
                                ResGumps.ThereIsNoOneInThatPartySlot,
                                0,
                                MessageType.System,
                                3,
                                false
                            );
                        }
                        else
                        {
                            NetClient.Socket.Send_PartyRemoveRequest(World.Party.Members[index].Serial);
                        }
                    }

                    break;
            }
        }

        private enum Buttons
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
    }
}