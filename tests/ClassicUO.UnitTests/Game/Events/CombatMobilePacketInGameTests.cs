// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Network;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Events
{
    // Golden-byte parser tests for combat / mobile-state packet handlers that
    // gate on world.InGame / world.Player. Sister fixture to
    // CombatMobilePacketTests — those handlers couldn't be reached without
    // reflection before the World.SetPlayerForTests / SetMapForTests seams
    // and null-safe PlayerMobile / Map ctors landed.
    //
    // EventSink.ClearAll() in the ctor strips out the World's auto-wired
    // subscribers; the test subscriber attached in each test then sees the
    // event in isolation. The subscribers themselves are covered by the
    // World.Subscribers* fixtures.
    [Collection("EventSinkSerial")]
    public class CombatMobilePacketInGameTests : IDisposable
    {
        private const uint PlayerSerial = 0x0000_0001u;

        private readonly World _world;

        public CombatMobilePacketInGameTests()
        {
            _world = new World();
            _world.SetPlayerForTests(new PlayerMobile(_world, PlayerSerial));
            _world.SetMapForTests(new ClassicUO.Game.Map.Map(_world, 0));
            EventSink.ClearAll();
        }

        public void Dispose()
        {
            EventSink.ClearAll();
        }

        private void Dispatch(byte[] data, int offset)
            => PacketHandlers.Handler.AnalyzePacket(_world, data, offset);

        // ---- 0x0B Damage ----

        [Fact]
        public void Damage_0x0B_RaisesDamageReceived()
        {
            // Layout: 0B | serial(4) | damage(2)
            DamageReceivedArgs? captured = null;
            EventSink.DamageReceived += e => captured = e;

            byte[] packet =
            {
                0x0B,
                0x00, 0x00, 0x00, 0x55,  // serial = 0x55
                0x00, 0x2A               // damage = 42
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x55u);
            captured.Value.Damage.Should().Be(42);
        }

        // ---- 0x17 NewHealthbarUpdate ----
        //
        // 0x16 is NOT covered: its handler unconditionally evaluates
        // Client.Game.UO.Version, which NREs in tests because Client.Game is
        // null. The short-circuit in p[0] == 0x16 && Client.Game... means
        // 0x17 is the only safely-dispatchable variant.

        [Fact]
        public void NewHealthbarUpdate_0x17_SingleEntry_RaisesHealthBarStateChanged()
        {
            // Layout: 17 | _len(2) | mobSerial(4) | count(2) | { type(2) enabled(1) } *
            // NOTE: This handler is variable-length; AnalyzePacket reads from
            // offset onward, so the 2-byte _len is included only for layout
            // realism, not consumed by the handler.
            HealthBarStateChangedArgs? captured = null;
            int callCount = 0;
            EventSink.HealthBarStateChanged += e =>
            {
                captured = e;
                callCount++;
            };

            byte[] packet =
            {
                0x17,
                0x00, 0x0C,              // _len placeholder (2)
                0x00, 0x00, 0x00, 0x77,  // mobSerial = 0x77
                0x00, 0x01,              // count = 1
                0x00, 0x02,              // type = 2
                0x01                     // enabled = true
            };

            Dispatch(packet, 3);

            callCount.Should().Be(1);
            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x77u);
            captured.Value.StateType.Should().Be((ushort)2);
            captured.Value.Enabled.Should().BeTrue();
        }

        [Fact]
        public void NewHealthbarUpdate_0x17_TwoEntries_RaisesTwice()
        {
            int count = 0;
            EventSink.HealthBarStateChanged += _ => count++;

            byte[] packet =
            {
                0x17,
                0x00, 0x10,              // _len placeholder
                0x00, 0x00, 0x00, 0x77,  // mobSerial
                0x00, 0x02,              // count = 2
                0x00, 0x01, 0x01,        // type=1 enabled=true
                0x00, 0x05, 0x00         // type=5 enabled=false
            };

            Dispatch(packet, 3);

            count.Should().Be(2);
        }

        // ---- 0x20 UpdatePlayer ----

        [Fact]
        public void UpdatePlayer_0x20_RaisesPlayerUpdated()
        {
            // Layout: 20 | serial(4) | graphic(2) | graphic_inc(1) | hue(2)
            //           | flags(1) | x(2) | y(2) | serverId(2) | direction(1) | z(1)
            PlayerUpdatedArgs? captured = null;
            EventSink.PlayerUpdated += e => captured = e;

            byte[] packet =
            {
                0x20,
                0x00, 0x00, 0x00, 0x01,  // serial = 1
                0x01, 0x90,              // graphic = 0x0190
                0x00,                    // graphic_inc
                0x00, 0x42,              // hue = 0x0042
                0x00,                    // flags
                0x05, 0x39,              // x = 1337
                0x03, 0xE7,              // y = 999
                0x00, 0x00,              // serverId = 0
                0x01,                    // direction = 1
                0x0A                     // z = 10
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x01u);
            captured.Value.Graphic.Should().Be((ushort)0x0190);
            captured.Value.GraphicIncrement.Should().Be((byte)0);
            captured.Value.Hue.Should().Be((ushort)0x0042);
            captured.Value.X.Should().Be((ushort)1337);
            captured.Value.Y.Should().Be((ushort)999);
            captured.Value.Z.Should().Be((sbyte)10);
            captured.Value.ServerId.Should().Be((ushort)0);
            captured.Value.Direction.Should().Be((Direction)1);
        }

        // ---- 0x21 DenyWalk ----

        [Fact]
        public void DenyWalk_0x21_RaisesWalkDenied_MasksDirectionToUp()
        {
            // Layout: 21 | seq(1) | x(2) | y(2) | direction(1) | z(1)
            // Handler ANDs direction with Direction.Up (0x07) — high bits dropped.
            WalkDeniedArgs? captured = null;
            EventSink.WalkDenied += e => captured = e;

            byte[] packet =
            {
                0x21,
                0x07,                    // seq = 7
                0x05, 0x39,              // x = 1337
                0x03, 0xE7,              // y = 999
                0x85,                    // direction = 0x85 -> masked to 0x05
                0xF6                     // z = -10
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Sequence.Should().Be((byte)7);
            captured.Value.X.Should().Be((ushort)1337);
            captured.Value.Y.Should().Be((ushort)999);
            captured.Value.Z.Should().Be((sbyte)-10);
            captured.Value.Direction.Should().Be((Direction)0x05);
        }

        // ---- 0x22 ConfirmWalk ----

        [Fact]
        public void ConfirmWalk_0x22_RaisesWalkConfirmed_StripsHighBitFromNoto()
        {
            // Layout: 22 | seq(1) | noto(1). Handler masks ~0x40 off noto.
            WalkConfirmedArgs? captured = null;
            EventSink.WalkConfirmed += e => captured = e;

            byte[] packet =
            {
                0x22,
                0x09,    // seq = 9
                0x41     // noto raw = 0x41 -> 0x01 after & ~0x40
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Sequence.Should().Be((byte)9);
            captured.Value.Notoriety.Should().Be((byte)0x01);
        }

        // ---- 0x2F Swing ----

        [Fact]
        public void Swing_0x2F_AttackerIsPlayer_RaisesCombatSwing()
        {
            // Layout: 2F | pad(1) | attacker(4) | defender(4)
            // Handler bails when attacker != world.Player.
            CombatSwingArgs? captured = null;
            EventSink.CombatSwing += e => captured = e;

            byte[] packet =
            {
                0x2F,
                0x00,                                                // pad
                (byte)(PlayerSerial >> 24), (byte)(PlayerSerial >> 16),
                (byte)(PlayerSerial >> 8),  (byte)PlayerSerial,      // attacker = player
                0xDE, 0xAD, 0xBE, 0xEF                               // defender
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Attacker.Should().Be(PlayerSerial);
            captured.Value.Defender.Should().Be(0xDEADBEEFu);
        }

        [Fact]
        public void Swing_0x2F_AttackerNotPlayer_DoesNotRaise()
        {
            bool fired = false;
            EventSink.CombatSwing += _ => fired = true;

            byte[] packet =
            {
                0x2F,
                0x00,
                0x00, 0x00, 0x00, 0x42,  // attacker = 0x42 (not player)
                0xDE, 0xAD, 0xBE, 0xEF
            };

            Dispatch(packet, 1);

            fired.Should().BeFalse();
        }

        // ---- 0x72 Warmode ----

        [Fact]
        public void Warmode_0x72_True_RaisesWarModeChanged_WithPlayerSerial()
        {
            // Layout: 72 | inWar(1) [+ padding bytes ignored]
            // Handler sources the serial from world.Player.Serial.
            WarModeChangedArgs? captured = null;
            EventSink.WarModeChanged += e => captured = e;

            byte[] packet =
            {
                0x72,
                0x01,    // inWar = true
                0x00, 0x32, 0x00
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(PlayerSerial);
            captured.Value.InWarMode.Should().BeTrue();
        }

        [Fact]
        public void Warmode_0x72_False_RaisesWarModeChanged()
        {
            WarModeChangedArgs? captured = null;
            EventSink.WarModeChanged += e => captured = e;

            byte[] packet = { 0x72, 0x00, 0x00, 0x32, 0x00 };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.InWarMode.Should().BeFalse();
        }

        // ---- 0x77 UpdateCharacter ----

        [Fact]
        public void UpdateCharacter_0x77_KnownMobile_RaisesMobileUpdated()
        {
            // Layout: 77 | serial(4) | graphic(2) | x(2) | y(2) | z(1)
            //           | direction(1) | hue(2) | flags(1) | notoriety(1)
            // Handler early-returns when the mobile isn't already known.
            const uint mobSerial = 0x0000_00ABu;
            _world.GetOrCreateMobile(mobSerial);

            MobileUpdatedArgs? captured = null;
            EventSink.MobileUpdated += e => captured = e;

            byte[] packet =
            {
                0x77,
                0x00, 0x00, 0x00, 0xAB,  // serial
                0x01, 0x90,              // graphic = 0x0190
                0x04, 0xD2,              // x = 1234
                0x02, 0xBC,              // y = 700
                0x05,                    // z = 5
                0x02,                    // direction = 2
                0x00, 0x21,              // hue = 0x0021
                0x00,                    // flags
                0x01                     // notoriety = 1
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(mobSerial);
            captured.Value.Graphic.Should().Be((ushort)0x0190);
            captured.Value.X.Should().Be((ushort)1234);
            captured.Value.Y.Should().Be((ushort)700);
            captured.Value.Z.Should().Be((sbyte)5);
            captured.Value.Direction.Should().Be((Direction)2);
            captured.Value.Hue.Should().Be((ushort)0x0021);
            captured.Value.Notoriety.Should().Be((NotorietyFlag)1);
            captured.Value.IsFullObject.Should().BeFalse();
        }

        [Fact]
        public void UpdateCharacter_0x77_UnknownMobile_DoesNotRaise()
        {
            bool fired = false;
            EventSink.MobileUpdated += _ => fired = true;

            byte[] packet =
            {
                0x77,
                0x00, 0x00, 0x00, 0xCC,  // serial not in world.Mobiles
                0x01, 0x90,
                0x04, 0xD2,
                0x02, 0xBC,
                0x05, 0x02,
                0x00, 0x21,
                0x00, 0x01
            };

            Dispatch(packet, 1);

            fired.Should().BeFalse();
        }

        // ---- 0x78 UpdateObject ----
        //
        // The equipment loop in UpdateObject reads Client.Game.UO.Version,
        // which NREs without a real GameController. To keep the test on the
        // safe path we send itemSerial = 0 immediately so the loop body
        // never executes.

        [Fact]
        public void UpdateObject_0x78_NoEquipment_RaisesMobileUpdated_AsFullObject()
        {
            // Layout: 78 | _len(2) | serial(4) | graphic(2) | x(2) | y(2)
            //           | z(1) | direction(1) | hue(2) | flags(1) | notoriety(1)
            //           | itemSerial(4) = 0  (terminator -> no equipment)
            MobileUpdatedArgs? captured = null;
            EventSink.MobileUpdated += e => captured = e;

            byte[] packet =
            {
                0x78,
                0x00, 0x17,              // _len placeholder
                0x00, 0x00, 0x00, 0x9A,  // serial
                0x01, 0x90,              // graphic
                0x04, 0xD2,              // x = 1234
                0x02, 0xBC,              // y = 700
                0x07,                    // z = 7
                0x03,                    // direction = 3
                0x00, 0x42,              // hue
                0x00,                    // flags
                0x01,                    // notoriety
                0x00, 0x00, 0x00, 0x00   // terminator itemSerial = 0
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x9Au);
            captured.Value.Graphic.Should().Be((ushort)0x0190);
            captured.Value.X.Should().Be((ushort)1234);
            captured.Value.Y.Should().Be((ushort)700);
            captured.Value.Z.Should().Be((sbyte)7);
            captured.Value.Direction.Should().Be((Direction)3);
            captured.Value.Hue.Should().Be((ushort)0x0042);
            captured.Value.Notoriety.Should().Be((NotorietyFlag)1);
            captured.Value.IsFullObject.Should().BeTrue();
            captured.Value.Equipment.Should().NotBeNull();
            captured.Value.Equipment.Count.Should().Be(0);
        }

        // ---- 0x97 MovePlayer ----

        [Fact]
        public void MovePlayer_0x97_Running_RaisesPlayerMoved_RunningTrue_DirectionMasked()
        {
            // Layout: 97 | direction(1). Running bit = 0x80, Mask = 0x07.
            PlayerMovedArgs? captured = null;
            EventSink.PlayerMoved += e => captured = e;

            byte[] packet = { 0x97, 0x84 };   // running + direction=4

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.IsRunning.Should().BeTrue();
            captured.Value.Direction.Should().Be((Direction)4);
        }

        [Fact]
        public void MovePlayer_0x97_Walking_RaisesPlayerMoved_RunningFalse()
        {
            PlayerMovedArgs? captured = null;
            EventSink.PlayerMoved += e => captured = e;

            byte[] packet = { 0x97, 0x02 };   // direction=2, no running bit

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.IsRunning.Should().BeFalse();
            captured.Value.Direction.Should().Be((Direction)2);
        }

        // ---- 0x98 UpdateName ----

        [Fact]
        public void UpdateName_0x98_RaisesMobileNameChanged()
        {
            // Layout: 98 | _len(2) | serial(4) | name(ASCII, NUL-terminated within buffer)
            MobileNameChangedArgs? captured = null;
            EventSink.MobileNameChanged += e => captured = e;

            // Build "Bob\0..." padded with zeros to a reasonable length.
            byte[] packet =
            {
                0x98,
                0x00, 0x25,              // _len placeholder
                0x00, 0x00, 0x00, 0xAA,  // serial
                (byte)'B', (byte)'o', (byte)'b', 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0xAAu);
            captured.Value.Name.Should().Be("Bob");
        }

        // ---- 0xAF DisplayDeath ----

        [Fact]
        public void DisplayDeath_0xAF_RaisesMobileDeath()
        {
            // Layout: AF | serial(4) | corpseSerial(4) | running(4)
            MobileDeathArgs? captured = null;
            EventSink.MobileDeath += e => captured = e;

            byte[] packet =
            {
                0xAF,
                0x00, 0x00, 0x00, 0x42,  // serial
                0x40, 0x00, 0x00, 0x99,  // corpseSerial
                0x00, 0x00, 0x00, 0x01   // running != 0 -> true
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x42u);
            captured.Value.CorpseSerial.Should().Be(0x40000099u);
            captured.Value.IsRunning.Should().BeTrue();
        }

        [Fact]
        public void DisplayDeath_0xAF_RunningZero_RaisesMobileDeath_NotRunning()
        {
            MobileDeathArgs? captured = null;
            EventSink.MobileDeath += e => captured = e;

            byte[] packet =
            {
                0xAF,
                0x00, 0x00, 0x00, 0x42,
                0x40, 0x00, 0x00, 0x99,
                0x00, 0x00, 0x00, 0x00
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.IsRunning.Should().BeFalse();
        }

        // ---- 0xDF BuffDebuff ----
        //
        // NOT covered: the handler dereferences BuffTable.Table.Length, but
        // BuffTable._table is only populated by BuffTable.Load() during
        // normal client startup. In a fresh unit-test World it's null, so
        // any 0xDF byte sequence NREs before either branch (count==0 ->
        // BuffRemoved, count>0 -> BuffApplied) can fire. Covering this
        // would require either Load() side-effects we don't want, or a
        // production-side null guard. Subscriber-level fixtures cover the
        // applied/removed handlers via direct EventSink.Raise* calls.
    }
}
