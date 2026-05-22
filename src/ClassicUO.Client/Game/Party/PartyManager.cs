// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Party;
using ClassicUO.Resources;

namespace ClassicUO.Game.Party
{
    /// <summary>
    /// Facade over the three Party collaborators: keeps the existing public
    /// surface (<c>_world.Party.X</c>) and owns the EventSink subscriptions.
    /// Roster state, invite tracking and chat routing live in dedicated
    /// cohesive classes under <see cref="Game.Party"/>.
    /// </summary>
    internal sealed class PartyManager : IEventListener
    {
        private readonly IPartyRoster _roster;
        private readonly IPartyInviteCoordinator _invite;
        private readonly IPartyChatRouter _chat;

        /// <summary>Production composition root.</summary>
        public PartyManager(World world)
            : this(world, new PartyInviteCoordinator(world))
        {
        }

        private PartyManager(World world, IPartyInviteCoordinator invite)
            : this(world, new PartyRoster(world, invite), invite)
        {
        }

        private PartyManager(World world, IPartyRoster roster, IPartyInviteCoordinator invite)
            : this(roster, invite, new PartyChatRouter(world, roster))
        {
        }

        /// <summary>Full DI seam — inject all three collaborators.</summary>
        internal PartyManager(IPartyRoster roster, IPartyInviteCoordinator invite, IPartyChatRouter chat)
        {
            _roster = roster;
            _invite = invite;
            _chat = chat;
        }

        public void Subscribe()
        {
            EventSink.PartyListUpdated += OnPartyListUpdated;
            EventSink.PartyChatMessage += OnPartyChatMessage;
            EventSink.PartyInviteReceived += OnPartyInviteReceived;
        }

        public void Unsubscribe()
        {
            EventSink.PartyListUpdated -= OnPartyListUpdated;
            EventSink.PartyChatMessage -= OnPartyChatMessage;
            EventSink.PartyInviteReceived -= OnPartyInviteReceived;
        }

        // ---- Roster facade ----
        public uint Leader
        {
            get => _roster.Leader;
            set => _roster.Leader = value;
        }

        public PartyMember[] Members => _roster.Members;

        public bool Contains(uint serial) => _roster.Contains(serial);

        public void Clear() => _roster.Clear();

        // ---- Invite facade ----
        public uint Inviter
        {
            get => _invite.Inviter;
            set => _invite.Inviter = value;
        }

        // ---- Facade-owned loose state (kept on facade because no collaborator
        //      currently needs to coordinate behaviour around them) ----
        public bool CanLoot { get; set; }
        public long PartyHealTimer { get; set; }
        public uint PartyHealTarget { get; set; }

        // ---- Event handlers ----
        private void OnPartyListUpdated(PartyListUpdatedArgs e) => _roster.HandlePartyListUpdated(e);
        private void OnPartyChatMessage(PartyChatMessageArgs e) => _chat.HandlePartyChatMessage(e);
        private void OnPartyInviteReceived(PartyInviteReceivedArgs e) => _invite.HandlePartyInviteReceived(e);
    }

    internal class PartyMember : IEquatable<PartyMember>
    {
        private readonly World _world;
        private string _name;

        public PartyMember(World world, uint serial)
        {
            _world = world;
            Serial = serial;
            _name = Name;
        }

        public string Name
        {
            get
            {
                Mobile mobile = _world.Mobiles.Get(Serial);

                if (mobile != null)
                {
                    _name = mobile.Name;

                    if (string.IsNullOrEmpty(_name))
                    {
                        _name = ResGeneral.NotSeeing;
                    }
                }

                return _name;
            }
        }

        public bool Equals(PartyMember other)
        {
            if (other == null)
            {
                return false;
            }

            return other.Serial == Serial;
        }

        public uint Serial;
    }
}
