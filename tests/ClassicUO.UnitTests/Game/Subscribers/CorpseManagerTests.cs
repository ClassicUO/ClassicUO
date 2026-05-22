// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Subscribers
{
    // Behavioral tests for CorpseManager.OnCorpseEquipmentReceived.
    //
    // Observable state after event: each entry item's Container is set to the
    // corpse serial and its Layer is (entry.Layer - 1). Backpack-layer entries
    // are skipped. Non-corpse graphics short-circuit the entire payload.
    [Collection("EventSinkSerial")]
    public class CorpseManagerTests : IDisposable
    {
        private readonly World _world;

        public CorpseManagerTests()
        {
            _world = new World();
        }

        public void Dispose()
        {
            EventSink.ClearAll();
        }

        private Item CreateCorpse(uint serial)
        {
            Item corpse = _world.GetOrCreateItem(serial);
            corpse.Graphic = 0x2006;
            return corpse;
        }

        [Fact]
        public void CorpseEquipmentReceived_Reparents_Items_To_Corpse()
        {
            uint corpseSerial = 0x4000_0100u;
            CreateCorpse(corpseSerial);

            uint shirtSerial = 0x4000_0101u;
            uint armorSerial = 0x4000_0102u;

            // Layer in the wire data is 1-based; handler subtracts 1.
            // Wire Layer.Shirt (=0x05 + 1) -> stored as Layer.Shirt after decrement.
            var entries = new List<CorpseEquipmentEntry>
            {
                new CorpseEquipmentEntry((Layer)((int)Layer.Shirt + 1), shirtSerial),
                new CorpseEquipmentEntry((Layer)((int)Layer.Torso + 1), armorSerial)
            };

            EventSink.RaiseCorpseEquipmentReceived(new CorpseEquipmentReceivedArgs(
                CorpseSerial: corpseSerial,
                Entries: entries));

            Item shirt = _world.Items.Get(shirtSerial);
            Item armor = _world.Items.Get(armorSerial);

            shirt.Should().NotBeNull();
            shirt.Container.Should().Be(corpseSerial);
            shirt.Layer.Should().Be(Layer.Shirt);

            armor.Should().NotBeNull();
            armor.Container.Should().Be(corpseSerial);
            armor.Layer.Should().Be(Layer.Torso);
        }

        [Fact]
        public void CorpseEquipmentReceived_Skips_Backpack_Layer_Entry()
        {
            uint corpseSerial = 0x4000_0110u;
            CreateCorpse(corpseSerial);

            uint backpackSerial = 0x4000_0111u;
            uint shirtSerial = 0x4000_0112u;

            var entries = new List<CorpseEquipmentEntry>
            {
                // (Layer.Backpack + 1) decrements back to Layer.Backpack → skipped.
                new CorpseEquipmentEntry((Layer)((int)Layer.Backpack + 1), backpackSerial),
                new CorpseEquipmentEntry((Layer)((int)Layer.Shirt + 1), shirtSerial)
            };

            EventSink.RaiseCorpseEquipmentReceived(new CorpseEquipmentReceivedArgs(
                CorpseSerial: corpseSerial,
                Entries: entries));

            // Backpack entry was skipped — no item created with that serial.
            _world.Items.Get(backpackSerial).Should().BeNull();

            // Non-backpack entry was processed.
            Item shirt = _world.Items.Get(shirtSerial);
            shirt.Should().NotBeNull();
            shirt.Container.Should().Be(corpseSerial);
            shirt.Layer.Should().Be(Layer.Shirt);
        }

        [Fact]
        public void CorpseEquipmentReceived_Without_Existing_Corpse_Is_NoOp()
        {
            uint corpseSerial = 0x4000_0120u;
            // No corpse created — _world.Get(corpseSerial) returns null → handler bails.

            uint shirtSerial = 0x4000_0121u;
            var entries = new List<CorpseEquipmentEntry>
            {
                new CorpseEquipmentEntry((Layer)((int)Layer.Shirt + 1), shirtSerial)
            };

            EventSink.RaiseCorpseEquipmentReceived(new CorpseEquipmentReceivedArgs(
                CorpseSerial: corpseSerial,
                Entries: entries));

            _world.Items.Get(shirtSerial).Should().BeNull();
        }

        [Fact]
        public void CorpseEquipmentReceived_With_Wrong_Graphic_Is_NoOp()
        {
            uint corpseSerial = 0x4000_0130u;
            Item notACorpse = _world.GetOrCreateItem(corpseSerial);
            notACorpse.Graphic = 0x1234; // not 0x2006

            uint shirtSerial = 0x4000_0131u;
            var entries = new List<CorpseEquipmentEntry>
            {
                new CorpseEquipmentEntry((Layer)((int)Layer.Shirt + 1), shirtSerial)
            };

            EventSink.RaiseCorpseEquipmentReceived(new CorpseEquipmentReceivedArgs(
                CorpseSerial: corpseSerial,
                Entries: entries));

            _world.Items.Get(shirtSerial).Should().BeNull();
        }

        [Fact]
        public void CorpseEquipmentReceived_With_Null_Entries_Is_NoOp()
        {
            uint corpseSerial = 0x4000_0140u;
            CreateCorpse(corpseSerial);

            // null Entries → handler bails after graphic check.
            Action act = () => EventSink.RaiseCorpseEquipmentReceived(new CorpseEquipmentReceivedArgs(
                CorpseSerial: corpseSerial,
                Entries: null));

            act.Should().NotThrow();
        }

        [Fact]
        public void Add_Then_Exists_Returns_True_And_Remove_Clears()
        {
            // Direct API state (Deque) — not event-driven, but proves the
            // observable corpse-tracking state matches the manager's contract.
            uint corpseSerial = 0x4000_0150u;
            uint objSerial = 0x0000_0030u;

            _world.CorpseManager.Add(corpseSerial, objSerial, Direction.North, run: false);
            _world.CorpseManager.Exists(corpseSerial, objSerial).Should().BeTrue();

            _world.CorpseManager.Remove(corpseSerial, objSerial);
            _world.CorpseManager.Exists(corpseSerial, objSerial).Should().BeFalse();
        }
    }
}
