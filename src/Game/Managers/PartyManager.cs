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

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Network;

namespace ClassicUO.Game.Managers
{
    internal class PartyManager
    {
        public Serial Leader { get; set; }
        public Serial Inviter { get; set; }
        public bool CanLoot { get; set; }

        public PartyMember[] Members { get; } = new PartyMember[10];


        public long PartyHealTimer { get; set; }
        public Serial PartyHealTarget { get; set; }


        public void ParsePacket(Packet p)
        {
            byte code = p.ReadByte();

            switch (code)
            {
                case 1:
                case 2:
                    byte count = p.ReadByte();

                    if (count <= 1)
                    {
                        Leader = 0;
                        Inviter = 0;

                        for (int i = 0; i < Members.Length; i++)
                        {
                            if (Members[i] == null || Members[i].Serial == 0)
                                break;

                            HealthBarGump gump = Engine.UI.GetGump<HealthBarGump>(Members[i].Serial);


                            if (gump != null)
                            {
                                if (code == 2)
                                    Members[i].Serial = 0;

                                gump.Update();
                            }
                        }

                        Clear();
                        Engine.UI.GetGump<PartyGumpAdvanced>()?.Update();

                        break;
                    }

                    Clear();

                    for (int i = 0; i < count; i++)
                    {
                        Serial serial = p.ReadUInt();
                        Members[i] = new PartyMember(serial);

                        if (i == 0)
                            Leader = serial;


                        HealthBarGump gump = Engine.UI.GetGump<HealthBarGump>(serial);

                        if (gump != null)
                        {
                            GameActions.RequestMobileStatus(serial);
                            gump.Update();
                        }
                        else
                        {
                            if (serial == World.Player)
                            {
                            }
                        }
                    }

                    Engine.UI.GetGump<PartyGumpAdvanced>()?.Update();

                    break;

                case 3:
                case 4:
                    Serial ser = p.ReadUInt();
                    string name = p.ReadUnicode();

                    for (int i = 0; i < Members.Length; i++)
                    {
                        if (Members[i] == null)
                            break;

                        if (Members[i].Serial == ser)
                        {
                            Mobile m = Members[i].Mobile;

                            if (m != null)
                                Chat.HandleMessage(null, name, m.Name, Engine.Profile.Current.PartyMessageHue, MessageType.Party, 3);

                            break;
                        }
                    }

                    break;

                case 7:
                    Inviter = p.ReadUInt();

                    break;
            }
        }

        public bool Contains(Serial serial)
        {
            for (int i = 0; i < Members.Length; i++)
            {
                if (Members[i] != null && Members[i].Serial == serial)
                    return true;
            }

            return false;
        }

        public void Clear()
        {
            for (int i = 0; i < Members.Length; i++) Members[i] = null;
        }
    }

    //internal class PartyManager
    //{
    //    private bool _allowPartyLoot;

    //    public bool IsInParty => Members.Count > 1;

    //    public bool IsPlayerLeader => IsInParty && Leader == World.Player;

    //    public Serial Leader { get; private set; }

    //    public string LeaderName => Leader.IsValid ? World.Mobiles.Get(Leader).Name : "";

    //    public List<PartyMember> Members { get; } = new List<PartyMember>();

    //    public bool AllowPartyLoot
    //    {
    //        get => _allowPartyLoot;
    //        set
    //        {
    //            _allowPartyLoot = value;
    //            GameActions.RequestPartyLootState(_allowPartyLoot);
    //        }
    //    }

    //    public long PartyHealTimer { get; set; }

    //    public Serial PartyHealTarget { get; set; }

    //    public event EventHandler PartyMemberChanged;

    //    public static void HandlePartyPacket(Packet p)
    //    {
    //        const byte CommandPartyList = 0x01;
    //        const byte CommandRemoveMember = 0x02;
    //        const byte CommandPrivateMessage = 0x03;
    //        const byte CommandPublicMessage = 0x04;
    //        const byte CommandInvitation = 0x07;
    //        byte SubCommand = p.ReadByte();

    //        switch (SubCommand)
    //        {
    //            case CommandPartyList:
    //                int count = p.ReadByte();
    //                Serial[] serials = new Serial[count];
    //                for (int i = 0; i < serials.Length; i++) serials[i] = p.ReadUInt();
    //                World.Party.ReceivePartyMemberList(serials);

    //                break;
    //            case CommandRemoveMember:
    //                count = p.ReadByte();

    //                if (count <= 1)
    //                    p.Skip(4);

    //                serials = new Serial[count];

    //                for (int i = 0; i < serials.Length; i++)
    //                    serials[i] = p.ReadUInt();
    //                World.Party.ReceiveRemovePartyMember(serials);

    //                break;
    //            case CommandPrivateMessage:
    //            case CommandPublicMessage:
    //                PartyMember partyMember = World.Party.GetPartyMember(p.ReadUInt());

    //                if (partyMember != null)
    //                    Chat.HandleMessage(null, p.ReadUnicode(), partyMember.Name, Engine.Profile.Current.PartyMessageHue, MessageType.Party, 3);

    //                break;
    //            case CommandInvitation:
    //                //The packet that arrives in PacketHandlers.DisplayClilocString(Packet p) for party invite does not have the party leader's serial
    //                //and therefor it is not handled by Chat.OnMessage because we have no entity and also the packet message type is incorrectly set to .Label
    //                //we handle the party invite here because we have the partyLeader's serial and we can appropriately set the MesageType.System
    //                Serial serial = p.ReadUInt();
    //                Mobile partyLeaderEntity = World.Mobiles.Get(serial);

    //                if (partyLeaderEntity != null) Chat.HandleMessage(partyLeaderEntity, partyLeaderEntity.Name + FileManager.Cliloc.Translate(FileManager.Cliloc.GetString(1008089)), partyLeaderEntity.Name, 0x03B2, MessageType.System, 3, true);

    //                World.Party.SetPartyLeader(serial);

    //                break;
    //        }
    //    }

    //    private void SetPartyLeader(Serial s)
    //    {
    //        Leader = s;
    //    }

    //    public void ReceivePartyMemberList(Serial[] mobileSerials)
    //    {
    //        Members.Clear();
    //        foreach (Serial serial in mobileSerials) AddPartyMember(serial);
    //        PartyMemberChanged.Raise();
    //    }

    //    public void ReceiveRemovePartyMember(Serial[] mobileSerials)
    //    {
    //        //var list = new List<PartyMember>(Members);
    //        Members.ForEach(s => Engine.UI.GetControl<HealthBarGump>(s.Serial)?.Update());
    //        Members.Clear();

    //        foreach (Serial serial in mobileSerials)
    //            AddPartyMember(serial);
    //        PartyMemberChanged.Raise();
    //    }

    //    public void TriggerAddPartyMember()
    //    {
    //        if (!IsInParty)
    //        {
    //            Leader = World.Player;
    //            GameActions.RequestPartyInviteByTarget();
    //        }
    //        else if (IsInParty && IsPlayerLeader)
    //            GameActions.RequestPartyInviteByTarget();
    //        else if (IsInParty && !IsPlayerLeader)
    //        {
    //            //"You may only add members to the party if you are the leader."
    //        }
    //    }

    //    private void AddPartyMember(Serial mobileSerial)
    //    {
    //        if (Members.All(p => p.Serial != mobileSerial))
    //        {
    //            Members.Add(new PartyMember(mobileSerial));
    //            GameActions.RequestMobileStatus(mobileSerial);

    //            Engine.UI.GetControl<HealthBarGump>(mobileSerial)?.Update();
    //        }
    //    }

    //    public PartyMember GetPartyMember(Serial mobileSerial)
    //    {
    //        return Members.FirstOrDefault(s => s.Serial == mobileSerial);
    //    }

    //    public void RemovePartyMember(Serial mobileSerial)
    //    {
    //        for (int i = 0; i < Members.Count; i++)
    //        {
    //            var m = Members[i];

    //            if (m != null && m.Serial == mobileSerial)
    //            {
    //                Members.RemoveAt(i--);
    //                GameActions.RequestPartyRemoveMember(mobileSerial);
    //            }
    //        }
    //    }

    //    public void AcceptPartyInvite()
    //    {
    //        GameActions.RequestPartyAccept(Leader);
    //    }

    //    public void DeclinePartyInvite()
    //    {
    //        //Do nothing, let party invite expire
    //    }

    //    public void QuitParty()
    //    {
    //        GameActions.RequestPartyQuit();
    //        var list = new List<PartyMember>(Members);
    //        Members.Clear();
    //        list.ForEach(s => Engine.UI.GetControl<HealthBarGump>(s.Serial)?.Update());
    //    }

    //    public void PartyMessage(string message)
    //    {
    //        GameActions.SayParty(message);
    //    }
    //}

    internal class PartyMember
    {
        private string _name;
        public Serial Serial;

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