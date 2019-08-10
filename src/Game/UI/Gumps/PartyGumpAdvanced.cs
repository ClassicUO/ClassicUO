#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
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

using System.Collections.Generic;

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class PartyGumpAdvanced : Gump
    {
        private const int WIDTH = 320;
        private const int HEIGHT = 400;
        private readonly AlphaBlendControl _alphaBlendControl;
        private readonly Button _createAddButton;
        private readonly Label _createAddLabel;
        private readonly Button _leaveButton;
        private readonly Label _leaveLabel;
        private readonly Button _lootMeButton;
        private readonly Label _lootMeLabel;
        private readonly Button _messagePartyButton;
        private readonly Label _messagePartyLabel;
        private readonly List<PartyListEntry> _partyListEntries;
        private readonly ScrollArea _scrollArea;
        private bool prevStatus;

        public PartyGumpAdvanced() : base(0, 0)
        {
            _partyListEntries = new List<PartyListEntry>();
            X = 100;
            Y = 100;
            CanMove = true;
            AcceptMouseInput = true;
            WantUpdateSize = false;

            Add(_alphaBlendControl = new AlphaBlendControl(0.05f)
            {
                X = 1,
                Y = 1,
                Width = WIDTH - 2,
                Height = HEIGHT - 2
            });

            _scrollArea = new ScrollArea(20, 60, 295, 190, true)
            {
                AcceptMouseInput = true
            };
            Add(_scrollArea);

            Add(new Label("Bar", true, 1153)
            {
                X = 30, Y = 25
            });

            Add(new Label("Kick", true, 1153)
            {
                X = 60, Y = 25
            });

            Add(new Label("Player", true, 1153)
            {
                X = 100, Y = 25
            });

            Add(new Label("Status", true, 1153)
            {
                X = 250, Y = 25
            });

            //======================================================
            Add(_messagePartyButton = new Button((int) Buttons.Message, 0xFAB, 0xFAC, 0xFAD)
            {
                X = 30, Y = 275, ButtonAction = ButtonAction.Activate, IsVisible = false
            });

            Add(_messagePartyLabel = new Label("Message party", true, 1153)
            {
                X = 70, Y = 275, IsVisible = false
            });

            //======================================================
            Add(_lootMeButton = new Button((int) Buttons.Loot, 0xFA2, 0xFA3, 0xFA4)
            {
                X = 30, Y = 300, ButtonAction = ButtonAction.Activate, IsVisible = false
            });

            Add(_lootMeLabel = new Label("Party CANNOT loot me", true, 1153)
            {
                X = 70, Y = 300, IsVisible = false
            });

            //======================================================
            Add(_leaveButton = new Button((int) Buttons.Leave, 0xFAE, 0xFAF, 0xFB0)
            {
                X = 30, Y = 325, ButtonAction = ButtonAction.Activate, IsVisible = false
            });

            Add(_leaveLabel = new Label("Leave party", true, 1153)
            {
                X = 70, Y = 325, IsVisible = false
            });

            //======================================================
            Add(_createAddButton = new Button((int) Buttons.Add, 0xFA8, 0xFA9, 0xFAA)
            {
                X = 30, Y = 350, ButtonAction = ButtonAction.Activate
            });

            Add(_createAddLabel = new Label("Add party member", true, 1153)
            {
                X = 70, Y = 350
            });
            //======================================================

            Add(new Line(30, 50, 260, 1, Color.White.PackedValue));
            Add(new Line(95, 50, 1, 200, Color.White.PackedValue));
            Add(new Line(245, 50, 1, 200, Color.White.PackedValue));
            Add(new Line(30, 250, 260, 1, Color.White.PackedValue));

            Width = WIDTH;
            Height = HEIGHT;

            Height = 320;
            _alphaBlendControl.Height = Height;
            //Set contents if player is NOT in party
            _createAddButton.Y = 270;
            _createAddLabel.Y = _createAddButton.Y;
            _createAddLabel.Text = "Create a party";
            _leaveButton.IsVisible = false;
            _leaveLabel.IsVisible = false;
            _lootMeButton.IsVisible = false;
            _lootMeLabel.IsVisible = false;
            _messagePartyButton.IsVisible = false;
            _messagePartyLabel.IsVisible = false;
        }


        public void Update()
        {
            OnInitialize();
        }


        protected override void OnInitialize()
        {
            _scrollArea.Clear();

            foreach (PartyListEntry entry in _partyListEntries) entry.Dispose();
            _partyListEntries.Clear();

            for (int i = 0; i < World.Party.Members.Length; i++)
            {
                var m = World.Party.Members[i];

                if (m == null)
                    continue;

                PartyListEntry p = new PartyListEntry(i);
                _partyListEntries.Add(p);
                _scrollArea.Add(p);
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);

            ResetHueVector();
            _hueVector.Z = 0.5f;

            batcher.DrawRectangle(Textures.GetTexture(Color.Gray), x, y, Width, Height, ref _hueVector);

            return true;
        }

        private void UpdateGumpStatus()
        {
            bool hasParty = World.Party.Leader != 0;

            if (prevStatus != hasParty)
            {
                prevStatus = hasParty;

                if (prevStatus)
                {
                    //Set gump size if player is in party
                    Height = HEIGHT;
                    _alphaBlendControl.Height = Height;
                    //Set contents if player is in party
                    _createAddButton.Y = 350;
                    _createAddLabel.Y = _createAddButton.Y;
                    _createAddLabel.Text = "Add a member";
                    _leaveButton.IsVisible = true;
                    _leaveLabel.IsVisible = true;
                    _lootMeButton.IsVisible = true;
                    _lootMeLabel.IsVisible = true;
                    _lootMeLabel.Text = !World.Party.CanLoot ? "Party CANNOT loot me" : "Party ALLOWED looting me";
                    _messagePartyButton.IsVisible = true;
                    _messagePartyLabel.IsVisible = true;
                }
                else
                {
                    //Set gump size if player is NOT in party
                    Height = 320;
                    _alphaBlendControl.Height = Height;
                    //Set contents if player is NOT in party
                    _createAddButton.Y = 270;
                    _createAddLabel.Y = _createAddButton.Y;
                    _createAddLabel.Text = "Create a party";
                    _leaveButton.IsVisible = false;
                    _leaveLabel.IsVisible = false;
                    _lootMeButton.IsVisible = false;
                    _lootMeLabel.IsVisible = false;
                    _messagePartyButton.IsVisible = false;
                    _messagePartyLabel.IsVisible = false;
                }
            }
        }


        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            UpdateGumpStatus();
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((Buttons) buttonID)
            {
                case Buttons.Add:
                    //World.Party.TriggerAddPartyMember();

                    if (World.Party.Leader == 0 || World.Party.Leader == World.Player) GameActions.RequestPartyInviteByTarget();

                    break;

                case Buttons.Leave:
                    //World.Party.QuitParty();

                    if (World.Party.Leader == 0)
                        Chat.HandleMessage(null, "You are not in a party.", "System", Hue.INVALID, MessageType.Regular, 3);
                    else
                    {
                        for (int i = 0; i < World.Party.Members.Length; i++)
                        {
                            if (World.Party.Members[i] != null && World.Party.Members[i].Serial != 0)
                                GameActions.RequestPartyRemoveMember(World.Party.Members[i].Serial);
                        }
                    }

                    break;

                case Buttons.Loot:
                    //World.Party.AllowPartyLoot = !World.Party.AllowPartyLoot;

                    if (World.Party.Leader != 0)
                    {
                        World.Party.CanLoot = !World.Party.CanLoot;
                        GameActions.RequestPartyLootState(World.Party.CanLoot);
                    }

                    break;

                case Buttons.Message:

                    //
                    break;
            }
        }


        private enum Buttons
        {
            Add = 1,
            Leave,
            Loot,
            Message
        }
    }

    internal class PartyListEntry : Control
    {
        private readonly GumpPic _status;

        public PartyListEntry(int memeberIndex)
        {
            Height = 20;
            PartyMemeberIndex = memeberIndex;

            PartyMember member = World.Party.Members[memeberIndex];

            if (member == null || member.Serial == 0)
            {
                Dispose();

                return;
            }

            //======================================================
            //Name = new Label(member.Name, true, 1153, font:3);
            //Name.X = 80;
            string name = string.IsNullOrEmpty(member.Name) ? "<Not seen>" : member.Name;

            Label name1 = World.Party.Leader == member.Serial
                              ? new Label(name + "(L)", true, 1161, font: 3)
                              {
                                  X = 80
                              }
                              : new Label(name, true, 1153, font: 3)
                              {
                                  X = 80
                              };
            Add(name1);

            Mobile mobile = World.Mobiles.Get(member.Serial);

            //======================================================
            Add(_status = new GumpPic(240, 0, 0x29F6, (Hue) (mobile != null && mobile.IsDead ? 0 : 0x43)));

            //======================================================
            Button pmButton = new Button((int) Buttons.GetBar, 0xFAE, 0xFAF, 0xFB0)
            {
                X = 10, ButtonAction = ButtonAction.Activate
            };
            Add(pmButton);

            //======================================================
            Button kickButton = new Button((int) Buttons.Kick, 0xFB1, 0xFB2, 0xFB3)
            {
                X = 40, ButtonAction = ButtonAction.Activate
            };
            Add(kickButton);
        }


        public int PartyMemeberIndex { get; }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            PartyMember member = World.Party.Members[PartyMemeberIndex];

            if (member == null || member.Serial == 0)
                Dispose();

            if (IsDisposed)
                return;

            Mobile mobile = World.Mobiles.Get(member.Serial);

            if (mobile != null && mobile.IsDead)
            {
                if (_status.Hue != 0)
                    _status.Hue = 0;
                else if (_status.Hue != 0x43)
                    _status.Hue = 0x43;
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            PartyMember member = World.Party.Members[PartyMemeberIndex];

            if (member == null)
                return;

            switch ((Buttons) buttonID)
            {
                case Buttons.Kick:
                    //

                    GameActions.RequestPartyRemoveMember(member.Serial);


                    break;

                case Buttons.GetBar:

                    Engine.UI.GetGump<HealthBarGump>(member.Serial)?.Dispose();

                    if (member.Serial == World.Player)
                        StatusGumpBase.GetStatusGump()?.Dispose();

                    Rectangle rect = FileManager.Gumps.GetTexture(0x0804).Bounds;
                    Engine.UI.Add(new HealthBarGump(member.Serial) {X = Mouse.Position.X - (rect.Width >> 1), Y = Mouse.Position.Y - (rect.Height >> 1)});

                    break;
            }
        }

        private enum Buttons
        {
            Kick = 1,
            GetBar
        }
    }
}