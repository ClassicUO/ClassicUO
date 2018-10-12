using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.UIGumps;
using ClassicUO.Utility;

namespace ClassicUO.Game.System
{
    class PartySystem
    {
        
        private static readonly List<PartyMember> _partyMemberList = new List<PartyMember>();
        private static Serial _leader;
        private static bool _allowPartyLoot = false;
        public static event EventHandler PartyMemberChanged;
        public static Dictionary<PartyMember, PartyMemberGump> PartyMemberGumpStack = new Dictionary<PartyMember, PartyMemberGump>();

        public static bool IsInParty => _partyMemberList.Count > 1;
        public static bool IsPlayerLeader => IsInParty && Leader == World.Player;
        public static Serial Leader => _leader;
        public static string LeaderName => (_leader != null && _leader.IsValid) ? World.Mobiles.Get(_leader).Name : "";
        public static List<PartyMember> Members => _partyMemberList;

        public static void RegisterCommands()
        {
            CommandSystem.Register("add", (sender, args) => TriggerAddPartyMember());
            CommandSystem.Register("leave", (sender, args) => LeaveParty());
            CommandSystem.Register("loot", (sender, args) => AllowPartyLoot = (!AllowPartyLoot) ? true : false);
        }

        public static bool AllowPartyLoot
        {
            get { return _allowPartyLoot; }
            set
            {
                _allowPartyLoot = value;
                GameActions.RequestPartyLootState(_allowPartyLoot);
            }
        }

        public static void ReceivePartyMemberList(Serial[] mobileSerials)
        {
            _partyMemberList.Clear();
            foreach (var serial in mobileSerials)
            {
                AddPartyMember(serial);
            }
            PartyMemberChanged.Raise();
        }

        public static void ReceiveRemovePartyMember(Serial[] mobileSerials)
        {
            _partyMemberList.Clear();
            foreach (var serial in mobileSerials)
            {
                AddPartyMember(serial);
            }
            PartyMemberChanged.Raise();

        }

        public static void TriggerAddPartyMember()
        {
            if (!IsInParty)
            {
                _leader = World.Player;
                GameActions.RequestPartyInviteByTarget();
            }
            else if (IsInParty && IsPlayerLeader)
            {
                GameActions.RequestPartyInviteByTarget();
            }
            else if (IsInParty && !IsPlayerLeader)
            {
                //"You may only add members to the party if you are the leader."
            }
        }


        private static void AddPartyMember(Serial mobileSerial)
        {
            if (!_partyMemberList.Any(p => p.Serial == mobileSerial))
            {
                _partyMemberList.Add(new PartyMember(mobileSerial));
                GameActions.RequestMobileStatus(mobileSerial);
            }
            
        }

        public static void RemovePartyMember(Serial mobileSerial)
        {
            if (_partyMemberList.Any(p => p.Serial == mobileSerial))
            {
                _partyMemberList.RemoveAt(_partyMemberList.FindIndex(p => p.Serial == mobileSerial));
                GameActions.RequestPartyRemoveMember(mobileSerial);
            }
        }

        public static void LeaveParty()
        {
            GameActions.RequestPartyLeave();
            _partyMemberList.Clear();
            
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
        public Mobile Mobile => World.Mobiles.Get(Serial);

        public string Name
        {
            get
            {
                if (Mobile != null)
                {
                    _name = Mobile.Name;
                }
                return _name;
            }
        }

        public PartyMember(Serial serial)
        {
            Serial = serial;
            _name = Name;
        }
    }
}
