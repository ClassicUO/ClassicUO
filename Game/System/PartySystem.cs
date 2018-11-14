using System;
using System.Collections.Generic;
using System.Linq;

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.UIGumps;
using ClassicUO.Utility;

namespace ClassicUO.Game.System
{
    internal class PartySystem
    {
        private static bool _allowPartyLoot;
        
        public static bool IsInParty => Members.Count > 1;

        public static bool IsPlayerLeader => IsInParty && Leader == World.Player;

        public static Serial Leader { get; private set; }

        public static string LeaderName => Leader != null && Leader.IsValid ? World.Mobiles.Get(Leader).Name : "";

        public static List<PartyMember> Members { get; } = new List<PartyMember>();

        public static bool AllowPartyLoot
        {
            get => _allowPartyLoot;
            set
            {
                _allowPartyLoot = value;
                GameActions.RequestPartyLootState(_allowPartyLoot);
            }
        }

        public static event EventHandler PartyMemberChanged;

        public static void RegisterCommands()
        {
            CommandSystem.Register("add", (sender, args) => TriggerAddPartyMember());
            CommandSystem.Register("leave", (sender, args) => LeaveParty());
            CommandSystem.Register("loot", (sender, args) => AllowPartyLoot = !AllowPartyLoot ? true : false);
        }

        public static void ReceivePartyMemberList(Serial[] mobileSerials)
        {
            Members.Clear();
            foreach (Serial serial in mobileSerials) AddPartyMember(serial);
            PartyMemberChanged.Raise();
        }

        public static void ReceiveRemovePartyMember(Serial[] mobileSerials)
        {
            Members.Clear();
            foreach (Serial serial in mobileSerials) AddPartyMember(serial);
            PartyMemberChanged.Raise();
        }

        public static void TriggerAddPartyMember()
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

        private static void AddPartyMember(Serial mobileSerial)
        {
            if (!Members.Any(p => p.Serial == mobileSerial))
            {
                Members.Add(new PartyMember(mobileSerial));
                GameActions.RequestMobileStatus(mobileSerial);
            }
        }

        public static void RemovePartyMember(Serial mobileSerial)
        {
            if (Members.Any(p => p.Serial == mobileSerial))
            {
                Members.RemoveAt(Members.FindIndex(p => p.Serial == mobileSerial));
                GameActions.RequestPartyRemoveMember(mobileSerial);
            }
        }

        public static void LeaveParty()
        {
            GameActions.RequestPartyLeave();
            Members.Clear();
        }

        public static void PartyMessage(string message)
        {
            GameActions.SayParty(message);
        }
    }

    public class PartyMember
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