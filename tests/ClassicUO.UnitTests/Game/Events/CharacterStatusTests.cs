// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game;
using ClassicUO.Game.Events;
using ClassicUO.Network;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Events
{
    // Golden-byte tests for the 0x11 CharacterStatus (MobileStatus) handler.
    //
    // After the EventSink refactor the handler is parse-and-emit-only: it
    // never touches world.Player or world.Get(serial). Every test here
    // therefore only checks the raised CharacterStatusReceivedArgs payload.
    //
    // Subscriber-side coverage (world.SetPlayerForTests + the full mutation
    // path) is skipped: PlayerMobile's ctor walks
    // Client.Game.UO.FileManager.Skills which is not mockable from a unit
    // test without reflection.
    public class CharacterStatusTests : IDisposable
    {
        private readonly World _world;

        public CharacterStatusTests()
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

        // ---- 0x11 CharacterStatus ----

        [Fact]
        public void CharacterStatus_0x11_Type0_BasicMobile_RaisesArgs_WithoutExtendedStats()
        {
            // Layout: 11 | serial(4) | name(30 ASCII) | hits(2) | hitsMax(2)
            //              | isRenamable(1) | type(1=0 -> stop here)
            // SerialHelper.IsMobile: 0 < serial < 0x40000000.
            CharacterStatusReceivedArgs? captured = null;
            EventSink.CharacterStatusReceived += e => captured = e;

            byte[] name = new byte[30];
            byte[] nameSrc = System.Text.Encoding.ASCII.GetBytes("Goblin");
            Array.Copy(nameSrc, name, nameSrc.Length);

            byte[] packet = new byte[1 + 4 + 30 + 2 + 2 + 1 + 1];
            int i = 0;
            packet[i++] = 0x11;
            packet[i++] = 0x00; packet[i++] = 0x00; packet[i++] = 0x01; packet[i++] = 0x23; // serial
            Array.Copy(name, 0, packet, i, 30); i += 30;
            packet[i++] = 0x00; packet[i++] = 0x64; // hits = 100
            packet[i++] = 0x00; packet[i++] = 0xC8; // hitsMax = 200
            packet[i++] = 0x01;                     // isRenamable = true
            packet[i++] = 0x00;                     // type = 0

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x123u);
            captured.Value.Name.Should().StartWith("Goblin");
            captured.Value.Hits.Should().Be(100);
            captured.Value.HitsMax.Should().Be(200);
            captured.Value.IsRenamable.Should().BeTrue();
            captured.Value.Type.Should().Be(0);
            captured.Value.HasExtendedStats.Should().BeFalse();
            captured.Value.HasRenaissanceStats.Should().BeFalse();
            captured.Value.HasAosStats.Should().BeFalse();
            captured.Value.HasKrSaStats.Should().BeFalse();
            // Defaults for unsupplied fields
            captured.Value.Strength.Should().Be(0);
            captured.Value.Gold.Should().Be(0u);
        }

        [Fact]
        public void CharacterStatus_0x11_Type5_Full_RaisesArgs_WithMlBlock()
        {
            // type=5 (ML): exercises basic + extended + Renaissance + AOS +
            // ML blocks (WeightMax + Race). The KR/SA block (type>=6) is
            // not present.
            CharacterStatusReceivedArgs? captured = null;
            EventSink.CharacterStatusReceived += e => captured = e;

            byte[] name = new byte[30];
            byte[] nameSrc = System.Text.Encoding.ASCII.GetBytes("Hero");
            Array.Copy(nameSrc, name, nameSrc.Length);

            byte[] packet = new byte[]
            {
                0x11,
                0x00, 0x00, 0x00, 0x42,           // serial
                // 30 bytes of name (filled below by Array.Copy substitution)
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
                0x00, 0x96,                       // hits = 150
                0x01, 0x2C,                       // hitsMax = 300
                0x00,                             // isRenamable = false
                0x05,                             // type = 5
                0x01,                             // isFemale = true
                0x00, 0x64,                       // str = 100
                0x00, 0x32,                       // dex = 50
                0x00, 0x28,                       // int = 40
                0x00, 0x46,                       // stam = 70
                0x00, 0x50,                       // stamMax = 80
                0x00, 0x14,                       // mana = 20
                0x00, 0x1E,                       // manaMax = 30
                0x00, 0x00, 0x27, 0x10,           // gold = 10000
                0x00, 0x0A,                       // physRes = 10
                0x00, 0xFA,                       // weight = 250
                // ML
                0x01, 0xF4,                       // weightMax = 500
                0x02,                             // race = 2 (Elf)
                // Renaissance
                0x02, 0x58,                       // statsCap = 600
                0x02,                             // followers = 2
                0x05,                             // followersMax = 5
                // AOS
                0x00, 0x14,                       // fireRes = 20
                0x00, 0x1E,                       // coldRes = 30
                0x00, 0x05,                       // poisonRes = 5
                0x00, 0x0F,                       // energyRes = 15
                0x00, 0x32,                       // luck = 50
                0x00, 0x09,                       // dmgMin = 9
                0x00, 0x18,                       // dmgMax = 24
                0x00, 0x00, 0x00, 0x07            // tithing = 7
            };

            // Write the name bytes in
            Array.Copy(name, 0, packet, 5, 30);

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            var c = captured.Value;
            c.Serial.Should().Be(0x42u);
            c.Name.Should().StartWith("Hero");
            c.Hits.Should().Be(150);
            c.HitsMax.Should().Be(300);
            c.IsRenamable.Should().BeFalse();
            c.Type.Should().Be(5);
            c.HasExtendedStats.Should().BeTrue();
            c.IsFemale.Should().BeTrue();
            c.Strength.Should().Be(100);
            c.Dexterity.Should().Be(50);
            c.Intelligence.Should().Be(40);
            c.Stamina.Should().Be(70);
            c.StaminaMax.Should().Be(80);
            c.Mana.Should().Be(20);
            c.ManaMax.Should().Be(30);
            c.Gold.Should().Be(10000u);
            c.PhysicalResistance.Should().Be(10);
            c.Weight.Should().Be(250);
            c.WeightMax.Should().Be(500);
            c.Race.Should().Be(2);
            c.HasRenaissanceStats.Should().BeTrue();
            c.StatsCap.Should().Be(600);
            c.Followers.Should().Be(2);
            c.FollowersMax.Should().Be(5);
            c.HasAosStats.Should().BeTrue();
            c.FireResistance.Should().Be(20);
            c.ColdResistance.Should().Be(30);
            c.PoisonResistance.Should().Be(5);
            c.EnergyResistance.Should().Be(15);
            c.Luck.Should().Be(50);
            c.DamageMin.Should().Be(9);
            c.DamageMax.Should().Be(24);
            c.TithingPoints.Should().Be(7u);
            // KR/SA block absent on type=5
            c.HasKrSaStats.Should().BeFalse();
            c.HitChanceIncrease.Should().Be(0);
        }

        [Fact]
        public void CharacterStatus_0x11_Type6_Truncated_RaisesArgs_WithZerosForMissingKrSaFields()
        {
            // type=6 (KR/SA) but the wire only carries the first KR field
            // (MaxPhysicResistence). The handler must zero-fill every later
            // field because each `p.Position + 2 > p.Length` check trips
            // after the first read.
            CharacterStatusReceivedArgs? captured = null;
            EventSink.CharacterStatusReceived += e => captured = e;

            byte[] name = new byte[30];
            byte[] nameSrc = System.Text.Encoding.ASCII.GetBytes("Trunc");
            Array.Copy(nameSrc, name, nameSrc.Length);

            byte[] packet = new byte[]
            {
                0x11,
                0x00, 0x00, 0x00, 0x77,           // serial
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // name
                0x00, 0x32,                       // hits = 50
                0x00, 0x64,                       // hitsMax = 100
                0x00,                             // isRenamable = false
                0x06,                             // type = 6
                0x00,                             // isFemale = false
                0x00, 0x50,                       // str = 80
                0x00, 0x40,                       // dex
                0x00, 0x30,                       // int
                0x00, 0x20,                       // stam
                0x00, 0x30,                       // stamMax
                0x00, 0x10,                       // mana
                0x00, 0x20,                       // manaMax
                0x00, 0x00, 0x00, 0x64,           // gold = 100
                0x00, 0x05,                       // physRes = 5
                0x00, 0x96,                       // weight = 150
                // ML
                0x01, 0x90,                       // weightMax = 400
                0x01,                             // race = 1
                // Renaissance
                0x02, 0x58,                       // statsCap = 600
                0x01, 0x03,                       // followers/Max
                // AOS
                0x00, 0x0A, 0x00, 0x0B, 0x00, 0x0C, 0x00, 0x0D,
                0x00, 0x14,
                0x00, 0x05, 0x00, 0x0A,
                0x00, 0x00, 0x00, 0x02,
                // KR/SA: only first field provided, then truncated
                0x00, 0x46                        // MaxPhysicResistence = 70
            };

            Array.Copy(name, 0, packet, 5, 30);

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            var c = captured.Value;
            c.Type.Should().Be(6);
            c.HasKrSaStats.Should().BeTrue();
            c.MaxPhysicResistence.Should().Be(70);
            // Every subsequent KR/SA field was truncated -> zero
            c.MaxFireResistence.Should().Be(0);
            c.MaxColdResistence.Should().Be(0);
            c.MaxPoisonResistence.Should().Be(0);
            c.MaxEnergyResistence.Should().Be(0);
            c.DefenseChanceIncrease.Should().Be(0);
            c.MaxDefenseChanceIncrease.Should().Be(0);
            c.HitChanceIncrease.Should().Be(0);
            c.SwingSpeedIncrease.Should().Be(0);
            c.DamageIncrease.Should().Be(0);
            c.LowerReagentCost.Should().Be(0);
            c.SpellDamageIncrease.Should().Be(0);
            c.FasterCastRecovery.Should().Be(0);
            c.FasterCasting.Should().Be(0);
            c.LowerManaCost.Should().Be(0);
        }
    }
}
