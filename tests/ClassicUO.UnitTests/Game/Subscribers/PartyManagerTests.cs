// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Reflection;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Events;
using ClassicUO.Game.Managers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Subscribers
{
    // Behavioral tests for PartyManager — proves the manager mutates Leader /
    // Inviter / Members in response to EventSink events.
    //
    // Fixture: build a fresh World (which auto-instantiates _world.Party and
    // subscribes it to EventSink). We do NOT call EventSink.ClearAll() because
    // the chat handler dereferences _world.MessageManager and other paths read
    // ProfileManager.CurrentProfile; keeping the World-wired manager avoids
    // re-instantiation pitfalls. ProfileManager.CurrentProfile is seeded via
    // reflection so the invite/chat paths don't NRE.
    //
    // Collection serializes execution against other EventSink test classes since
    // EventSink is a static, process-wide bus.
    [Collection("EventSinkSerial")]
    public class PartyManagerTests : IDisposable
    {
        private readonly World _world;
        private readonly PartyManager _party;

        public PartyManagerTests()
        {
            SeedCurrentProfile();
            _world = new World();
            _party = _world.Party;
        }

        public void Dispose()
        {
            EventSink.ClearAll();
        }

        private static void SeedCurrentProfile()
        {
            // CurrentProfile has a private setter — set via reflection so handlers
            // referencing ProfileManager.CurrentProfile.X don't NRE.
            PropertyInfo prop = typeof(ProfileManager).GetProperty(
                nameof(ProfileManager.CurrentProfile),
                BindingFlags.Public | BindingFlags.Static);
            prop?.SetValue(null, new Profile());
        }

        [Fact]
        public void PartyListUpdated_Add_With_Two_Serials_Sets_Leader_And_Members()
        {
            uint a = 0x0000_0010u;
            uint b = 0x0000_0011u;
            var args = new PartyListUpdatedArgs(
                IsAdd: true,
                RemovedSerial: 0u,
                Serials: new uint[] { a, b });

            EventSink.RaisePartyListUpdated(args);

            _party.Leader.Should().Be(a);
            _party.Contains(a).Should().BeTrue();
            _party.Contains(b).Should().BeTrue();
            _party.Members.Should().HaveCount(10); // PARTY_SIZE constant
            _party.Members[0].Should().NotBeNull();
            _party.Members[0].Serial.Should().Be(a);
            _party.Members[1].Should().NotBeNull();
            _party.Members[1].Serial.Should().Be(b);
        }

        [Fact]
        public void PartyListUpdated_Empty_Roster_Clears_State()
        {
            // Seed roster first.
            EventSink.RaisePartyListUpdated(new PartyListUpdatedArgs(
                IsAdd: true,
                RemovedSerial: 0u,
                Serials: new uint[] { 0x20u, 0x21u }));

            _party.Leader.Should().NotBe(0u);

            // Empty serials triggers the count<=1 clear branch.
            EventSink.RaisePartyListUpdated(new PartyListUpdatedArgs(
                IsAdd: true,
                RemovedSerial: 0u,
                Serials: Array.Empty<uint>()));

            _party.Leader.Should().Be(0u);
            _party.Inviter.Should().Be(0u);
            _party.Contains(0x20u).Should().BeFalse();
            _party.Contains(0x21u).Should().BeFalse();
        }

        [Fact]
        public void PartyListUpdated_Single_Serial_Clears_State()
        {
            // Seed roster first.
            EventSink.RaisePartyListUpdated(new PartyListUpdatedArgs(
                IsAdd: true,
                RemovedSerial: 0u,
                Serials: new uint[] { 0x30u, 0x31u, 0x32u }));

            _party.Contains(0x30u).Should().BeTrue();

            // Single serial also hits the count<=1 clear branch.
            EventSink.RaisePartyListUpdated(new PartyListUpdatedArgs(
                IsAdd: true,
                RemovedSerial: 0u,
                Serials: new uint[] { 0x30u }));

            _party.Leader.Should().Be(0u);
            _party.Contains(0x30u).Should().BeFalse();
        }

        [Fact]
        public void PartyInviteReceived_Sets_Inviter()
        {
            uint inviter = 0x0000_00ABu;

            EventSink.RaisePartyInviteReceived(new PartyInviteReceivedArgs(inviter));

            _party.Inviter.Should().Be(inviter);
        }

        [Fact]
        public void PartyListUpdated_Add_Then_Add_Replaces_Roster()
        {
            // First roster.
            EventSink.RaisePartyListUpdated(new PartyListUpdatedArgs(
                IsAdd: true,
                RemovedSerial: 0u,
                Serials: new uint[] { 0x40u, 0x41u }));

            _party.Leader.Should().Be(0x40u);

            // Subsequent add path calls Clear() before re-populating, so leader
            // becomes the new first serial.
            EventSink.RaisePartyListUpdated(new PartyListUpdatedArgs(
                IsAdd: true,
                RemovedSerial: 0u,
                Serials: new uint[] { 0x50u, 0x51u, 0x52u }));

            _party.Leader.Should().Be(0x50u);
            _party.Contains(0x40u).Should().BeFalse();
            _party.Contains(0x50u).Should().BeTrue();
            _party.Contains(0x51u).Should().BeTrue();
            _party.Contains(0x52u).Should().BeTrue();
        }

        // Skipped: OnPartyChatMessage exercises ProfileManager.CurrentProfile.PartyMessageHue
        // and routes through MessageManager.HandleMessage which fans out to JournalManager
        // and gump-construction code that touches unmockable globals
        // (Client.Game.UO.FileManager.Clilocs / font assets).
    }
}
