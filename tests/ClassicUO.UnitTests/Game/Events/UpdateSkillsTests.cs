// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using ClassicUO.Game;
using ClassicUO.Game.Events;
using ClassicUO.Network;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Events
{
    // Parser-side golden-byte tests for the 0x3A UpdateSkills packet. The
    // packet's typed events (SkillListReceived for type 0xFE; SkillsUpdated
    // for the per-skill update wire formats) are raised by the handler with
    // no InGame gate — the World subscriber owns the side effects and the
    // gate. Subscriber-side tests are skipped because PlayerMobile's ctor
    // reads Client.Game.UO.FileManager.Skills, which is not mockable here.
    public class UpdateSkillsTests : IDisposable
    {
        private readonly World _world;

        public UpdateSkillsTests()
        {
            _world = new World();
            EventSink.ClearAll();
        }

        public void Dispose()
        {
            EventSink.ClearAll();
        }

        private void Dispatch(byte[] data, int offset)
            => PacketHandlers.Handler.AnalyzePacket(_world, data, offset);

        // ---- type 0xFE: full skill name list ----

        [Fact]
        public void UpdateSkills_0x3A_FullList_RaisesSkillListReceived()
        {
            SkillListReceivedArgs? captured = null;
            EventSink.SkillListReceived += e => captured = e;

            // Layout: 0x3A len(2) type(1=0xFE) count(2) [ haveButton(1) nameLen(1) name(ascii) ]*count
            string[] names = { "Alchemy", "Magery" };
            var payload = new List<byte>
            {
                0x3A,
                0x00, 0x00,    // length placeholder
                0xFE,          // type = full list
                0x00, 0x02,    // count = 2
            };

            // entry 0: haveButton=true, "Alchemy"
            payload.Add(0x01);
            payload.Add((byte)names[0].Length);
            foreach (char c in names[0]) payload.Add((byte)c);

            // entry 1: haveButton=false, "Magery"
            payload.Add(0x00);
            payload.Add((byte)names[1].Length);
            foreach (char c in names[1]) payload.Add((byte)c);

            Dispatch(payload.ToArray(), 3);

            captured.Should().NotBeNull();
            captured.Value.Entries.Should().NotBeNull();
            captured.Value.Entries.Count.Should().Be(2);

            captured.Value.Entries[0].Index.Should().Be(0);
            captured.Value.Entries[0].Name.Should().Be("Alchemy");
            captured.Value.Entries[0].HasButton.Should().BeTrue();

            captured.Value.Entries[1].Index.Should().Be(1);
            captured.Value.Entries[1].Name.Should().Be("Magery");
            captured.Value.Entries[1].HasButton.Should().BeFalse();
        }

        [Fact]
        public void UpdateSkills_0x3A_FullList_DoesNotRaiseSkillsUpdated()
        {
            bool updatedFired = false;
            EventSink.SkillsUpdated += _ => updatedFired = true;

            var payload = new List<byte>
            {
                0x3A,
                0x00, 0x00,
                0xFE,
                0x00, 0x00,    // count = 0
            };

            Dispatch(payload.ToArray(), 3);

            updatedFired.Should().BeFalse();
        }

        // ---- type 0x02: full per-skill update with cap (id is wire+1) ----

        [Fact]
        public void UpdateSkills_0x3A_Type02_RaisesSkillsUpdated_WithCap_AndIdDecremented()
        {
            SkillsUpdatedArgs? captured = null;
            EventSink.SkillsUpdated += e => captured = e;

            // Layout: 0x3A len(2) type(1=0x02) [ id(2) real(2) base(2) lock(1) cap(2) ]*
            // type==0x02 means haveCap=true, isSingleUpdate=false, id-- in parser.
            var payload = new List<byte>
            {
                0x3A,
                0x00, 0x00,
                0x02,
                // entry 1: wire id=5 → parsed id=4
                0x00, 0x05,
                0x01, 0x2C,    // realVal = 300
                0x01, 0x40,    // baseVal = 320
                0x01,          // lock = 1
                0x03, 0xE8,    // cap = 1000
                // entry 2: wire id=10 → parsed id=9
                0x00, 0x0A,
                0x02, 0xBC,    // realVal = 700
                0x02, 0x58,    // baseVal = 600
                0x02,
                0x03, 0xE8,
            };

            Dispatch(payload.ToArray(), 3);

            captured.Should().NotBeNull();
            captured.Value.UpdateType.Should().Be((byte)0x02);
            captured.Value.HasCap.Should().BeTrue();
            captured.Value.IsSingleUpdate.Should().BeFalse();
            captured.Value.Updates.Should().NotBeNull();
            captured.Value.Updates.Count.Should().Be(2);

            captured.Value.Updates[0].Id.Should().Be((ushort)4);
            captured.Value.Updates[0].RealValue.Should().Be((ushort)300);
            captured.Value.Updates[0].BaseValue.Should().Be((ushort)320);
            captured.Value.Updates[0].LockState.Should().Be((byte)1);
            captured.Value.Updates[0].Cap.Should().Be((ushort)1000);

            captured.Value.Updates[1].Id.Should().Be((ushort)9);
            captured.Value.Updates[1].RealValue.Should().Be((ushort)700);
            captured.Value.Updates[1].BaseValue.Should().Be((ushort)600);
            captured.Value.Updates[1].LockState.Should().Be((byte)2);
            captured.Value.Updates[1].Cap.Should().Be((ushort)1000);
        }

        // ---- type 0xFF: single-skill update without cap, id NOT decremented ----

        [Fact]
        public void UpdateSkills_0x3A_TypeFF_SingleUpdate_NoCap_NoIdDecrement()
        {
            SkillsUpdatedArgs? captured = null;
            EventSink.SkillsUpdated += e => captured = e;

            // type=0xFF: haveCap = (type != 0 && type <= 0x03) || type == 0xDF → false
            //            isSingleUpdate = (type == 0xFF || type == 0xDF) → true
            //            id-- only when type == 0 || type == 0x02 → no
            var payload = new List<byte>
            {
                0x3A,
                0x00, 0x00,
                0xFF,
                // single entry: id=15
                0x00, 0x0F,
                0x01, 0xF4,    // realVal = 500
                0x01, 0xC2,    // baseVal = 450
                0x00,          // lock = 0
                // no cap bytes for 0xFF
                // even if more bytes followed, the parser must stop after one entry
                0xDE, 0xAD,
            };

            Dispatch(payload.ToArray(), 3);

            captured.Should().NotBeNull();
            captured.Value.UpdateType.Should().Be((byte)0xFF);
            captured.Value.HasCap.Should().BeFalse();
            captured.Value.IsSingleUpdate.Should().BeTrue();
            captured.Value.Updates.Count.Should().Be(1);

            captured.Value.Updates[0].Id.Should().Be((ushort)15);
            captured.Value.Updates[0].RealValue.Should().Be((ushort)500);
            captured.Value.Updates[0].BaseValue.Should().Be((ushort)450);
            captured.Value.Updates[0].LockState.Should().Be((byte)0);
            captured.Value.Updates[0].Cap.Should().Be((ushort)1000); // default cap
        }

        // ---- type 0x00: full update without cap, terminator id=0 stops list ----

        [Fact]
        public void UpdateSkills_0x3A_Type00_StopsAtTerminatorId()
        {
            SkillsUpdatedArgs? captured = null;
            EventSink.SkillsUpdated += e => captured = e;

            // type=0x00: haveCap=false, isSingleUpdate=false, id-- yes
            // Loop terminates when wire id == 0.
            var payload = new List<byte>
            {
                0x3A,
                0x00, 0x00,
                0x00,
                // entry 1: wire id=3 → parsed id=2
                0x00, 0x03,
                0x00, 0x64,    // realVal = 100
                0x00, 0x32,    // baseVal = 50
                0x00,          // lock = 0
                // terminator
                0x00, 0x00,
            };

            Dispatch(payload.ToArray(), 3);

            captured.Should().NotBeNull();
            captured.Value.UpdateType.Should().Be((byte)0x00);
            captured.Value.HasCap.Should().BeFalse();
            captured.Value.IsSingleUpdate.Should().BeFalse();
            captured.Value.Updates.Count.Should().Be(1);
            captured.Value.Updates[0].Id.Should().Be((ushort)2);
            captured.Value.Updates[0].RealValue.Should().Be((ushort)100);
            captured.Value.Updates[0].BaseValue.Should().Be((ushort)50);
        }
    }
}
