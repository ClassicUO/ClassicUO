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

using ClassicUO.Configuration;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Network;

namespace ClassicUO.Game.Managers
{
    internal class PartyManager
    {
        private const int PARTY_SIZE = 10;

        public uint Leader { get; set; }
        public uint Inviter { get; set; }
        public bool CanLoot { get; set; }

        public PartyMember[] Members { get; } = new PartyMember[PARTY_SIZE];


        public long PartyHealTimer { get; set; }
        public uint PartyHealTarget { get; set; }

        public void ParsePacket(Packet p)
        {
            byte code = p.ReadByte();

            bool add = false;

            switch (code)
            {
                case 1:
                    add = true;
                    goto case 2;
                case 2:
                    byte count = p.ReadByte();

                    if (count <= 1)
                    {
                        Leader = 0;
                        Inviter = 0;

                        for (int i = 0; i < PARTY_SIZE; i++)
                        {
                            if (Members[i] == null || Members[i].Serial == 0)
                                break;

                            BaseHealthBarGump gump = UIManager.GetGump<BaseHealthBarGump>(Members[i].Serial);


                            if (gump != null)
                            {
                                if (code == 2)
                                    Members[i].Serial = 0;

                                gump.Update();
                            }
                        }

                        Clear();
                        UIManager.GetGump<PartyGump>()?.Update();

                        break;
                    }

                    Clear();

                    if (!add)
                    {
                        UIManager.GetGump<BaseHealthBarGump>(p.ReadUInt())?.Update();
                    }

                    for (int i = 0; i < count; i++)
                    {
                        uint serial = p.ReadUInt();
                        Members[i] = new PartyMember(serial);

                        if (i == 0)
                            Leader = serial;

                        BaseHealthBarGump gump = UIManager.GetGump<BaseHealthBarGump>(serial);

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

                    UIManager.GetGump<PartyGump>()?.Update();

                    break;
                
                case 3:
                case 4:
                    uint ser = p.ReadUInt();
                    string name = p.ReadUnicode();

                    for (int i = 0; i < PARTY_SIZE; i++)
                    {
                        if (Members[i] != null && Members[i].Serial == ser)
                        {
                            Chat.HandleMessage(null, name, Members[i].Name, ProfileManager.Current.PartyMessageHue, MessageType.Party, 3);

                            break;
                        }
                    }

                    break;

                case 7:
                    Inviter = p.ReadUInt();

                    if (ProfileManager.Current.PartyInviteGump)
                    {
                        UIManager.Add(new PartyInviteGump(Inviter));
                    }
                    break;
            }
        }
        public bool Contains(uint serial)
        {
            for (int i = 0; i < PARTY_SIZE; i++)
            {
                var mem = Members[i];
                if (mem != null && mem.Serial == serial)
                    return true;
            }

            return false;
        }

        public void Clear()
        {
            Leader = 0;
            Inviter = 0;
            for (int i = 0; i < PARTY_SIZE; i++)
                Members[i] = null;
        }
    }

    internal class PartyMember : IEquatable<PartyMember>
    {
        private string _name;
        public uint Serial;

        public PartyMember(uint serial)
        {
            Serial = serial;
            _name = Name;
        }

        public string Name
        {
            get
            {
                var mobile = World.Mobiles.Get(Serial);

                if (mobile != null)
                {
                    _name = mobile.Name;

                    if (string.IsNullOrEmpty(_name))
                        _name = "<not seeing>";
                }

                return _name;
            }
        }

        public bool Equals(PartyMember other)
        {
            if (other == null)
                return false;

            return other.Serial == Serial;
        }
    }
}
