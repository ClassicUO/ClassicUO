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

using System;
using System.Collections.Generic;
using System.Linq;

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Utility;

namespace ClassicUO.Game.Managers
{
    internal class PartyManager
    {
        private bool _allowPartyLoot;

        public bool IsInParty => Members.Count > 1;

        public bool IsPlayerLeader => IsInParty && Leader == World.Player;

        public Serial Leader { get; private set; }

        public string LeaderName => Leader != null && Leader.IsValid ? World.Mobiles.Get(Leader).Name : "";

        public List<PartyMember> Members { get; } = new List<PartyMember>();

        public bool AllowPartyLoot
        {
            get => _allowPartyLoot;
            set
            {
                _allowPartyLoot = value;
                GameActions.RequestPartyLootState(_allowPartyLoot);
            }
        }

        public event EventHandler PartyMemberChanged;

        public static void HandlePartyPacket(Packet p)
        {
            const byte CommandPartyList = 0x01;
            const byte CommandRemoveMember = 0x02;
            const byte CommandPrivateMessage = 0x03;
            const byte CommandPublicMessage = 0x04;
            const byte CommandInvitation = 0x07;
            byte SubCommand = p.ReadByte();

            switch (SubCommand)
            {
                case CommandPartyList:
                    int count = p.ReadByte();
                    Serial[] serials = new Serial[count];
                    for (int i = 0; i < serials.Length; i++) serials[i] = p.ReadUInt();
                    World.Party.ReceivePartyMemberList(serials);

                    break;
                case CommandRemoveMember:
                    count = p.ReadByte();

                    if (count <= 1)
                        p.Skip(4);

                    serials = new Serial[count];
                    for (int i = 0; i < serials.Length; i++)
                        serials[i] = p.ReadUInt();
                    World.Party.ReceiveRemovePartyMember(serials);

                    break;
                case CommandPrivateMessage:
                case CommandPublicMessage:
                    Serial partyMemberSerial = p.ReadUInt();
                    PartyMember partyMember = World.Party.GetPartyMember(partyMemberSerial);

                    if (partyMember != null)
                    {
                        //Entity partyMemberEntity = World.Get(partyMemberSerial);//party messages from players off screen == null
                        //string partyMemberName = partyMemberEntity.Name; //party messages from players off screen == null

                        string partyMemberName = partyMember.Name;
                        string partyMessage = "[" + partyMemberName + "]: " + p.ReadUnicode();

                        Hue partyMessagehue = Engine.Profile.Current.PartyMessageHue;
                        MessageType messageType = MessageType.Party;
                        MessageFont partyMessageFont = MessageFont.Normal;

                        Chat.OnMessage(null, partyMessage, partyMemberName, partyMessagehue, messageType, partyMessageFont);
                    }

                    break;
                case CommandInvitation:
                    //The packet that arrives in PacketHandlers.DisplayClilocString(Packet p) for party invite does not have the party leader's serial
                    //and therefor it is not handled by Chat.OnMessage because we have no entity and also the packet message type is incorrectly set to .Label
                    //we handle the party invite here because we have the partyLeader's serial and we can appropriately set the MesageType.System
                    Serial partyLeaderSerial = p.ReadUInt();
                    Entity partyLeaderEntity = World.Get(partyLeaderSerial);

                    if (partyLeaderEntity != null)
                    {
                        Hue hue = 0x03B2; //white system
                        MessageType messageType = MessageType.System;
                        MessageFont font = MessageFont.Normal;
                        int cliloc = 1008089; // " : You are invited to join the party. Type /accept to join or /decline to decline the offer."
                        string clilocString = FileManager.Cliloc.Translate(FileManager.Cliloc.GetString(cliloc));
                        string clilocMessage = partyLeaderEntity.Name + clilocString;

                        Chat.OnMessage(partyLeaderEntity, clilocMessage, partyLeaderEntity.Name, hue, messageType, font, true);
                    }

                    break;
            }
        }

        public void ReceivePartyMemberList(Serial[] mobileSerials)
        {
            Members.Clear();
            foreach (Serial serial in mobileSerials) AddPartyMember(serial);
            PartyMemberChanged.Raise();
        }

        public void ReceiveRemovePartyMember(Serial[] mobileSerials)
        {
            var list = new List<PartyMember>(Members);
            Members.Clear();
            list.ForEach(s => Engine.UI.GetByLocalSerial<HealthBarGump>(s.Serial)?.Update());
            foreach (Serial serial in mobileSerials)
                AddPartyMember(serial);
            PartyMemberChanged.Raise();
        }

        public void TriggerAddPartyMember()
        {
            if (!IsInParty)
            {
                Leader = World.Player;
                GameActions.RequestPartyInviteByTarget();
            }
            else if (IsInParty && IsPlayerLeader)
                GameActions.RequestPartyInviteByTarget();
            else if (IsInParty && !IsPlayerLeader)
            {
                //"You may only add members to the party if you are the leader."
            }
        }

        private void AddPartyMember(Serial mobileSerial)
        {
            if (!Members.Any(p => p.Serial == mobileSerial))
            {
                Members.Add(new PartyMember(mobileSerial));
                GameActions.RequestMobileStatus(mobileSerial);

                Engine.UI.GetByLocalSerial<HealthBarGump>(mobileSerial)?.Update();
            }
        }

        public PartyMember GetPartyMember(Serial mobileSerial)
        {
            return Members.FirstOrDefault(s => s.Serial == mobileSerial);
        }

        public void RemovePartyMember(Serial mobileSerial)
        {
            if (Members.Any(p => p.Serial == mobileSerial))
            {
                Members.RemoveAt(Members.FindIndex(p => p.Serial == mobileSerial));
                GameActions.RequestPartyRemoveMember(mobileSerial);
            }
        }

        public void AcceptPartyInvite()
        {
            GameActions.RequestPartyAccept(World.Player.Serial);
        }

        public void DeclinePartyInvite()
        {
            //Do nothing, let party invite expire
        }

        public void QuitParty()
        {
            GameActions.RequestPartyQuit();
            var list = new List<PartyMember>(Members);
            Members.Clear();
            list.ForEach(s => Engine.UI.GetByLocalSerial<HealthBarGump>(s.Serial)?.Update());
        }

        public void PartyMessage(string message)
        {
            GameActions.SayParty(message);
        }
    }

    internal class PartyMember
    {
        public readonly Serial Serial;
        private string _name;

        public PartyMember(Serial serial)
        {
            Serial = serial;
            _name = Name;
        }

        public Mobile Mobile => World.Mobiles.Get(Serial);

        public string Name
        {
            get
            {
                if (Mobile != null) _name = Mobile.Name;

                return _name;
            }
        }
    }
}