// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game;
using ClassicUO.Game.Events;
using ClassicUO.Network;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Events
{
    // Golden-byte tests for the combat / mobile-state / targeting packet
    // handlers. Each test builds a raw packet, dispatches it through
    // PacketHandlers.AnalyzePacket, and asserts that the matching EventSink
    // event fires with the parsed args.
    //
    // Handlers gated on world.Player != null or world.InGame == true cannot
    // be reached from a fresh World without reflection (Player has a private
    // setter and Map is null in the unit-test environment). Those handlers
    // are noted inline and intentionally not covered here.
    //
    // Skipped (gated on world.Player != null / world.InGame):
    //   0x0B Damage              -> world.Player == null gate
    //   0x16/0x17 NewHealthbar    -> world.Player == null gate
    //   0x20 UpdatePlayer         -> world.Player == null gate
    //   0x21 DenyWalk             -> world.Player == null gate
    //   0x22 ConfirmWalk          -> world.Player == null gate
    //   0x2F Swing                -> !world.InGame gate (also Player check)
    //   0x72 Warmode              -> !world.InGame gate
    //   0x77 UpdateCharacter      -> world.Player == null gate
    //   0x78 UpdateObject         -> world.Player == null gate
    //   0x97 MovePlayer           -> !world.InGame gate
    //   0x98 UpdateName           -> !world.InGame gate
    //   0xAF DisplayDeath         -> !world.InGame gate
    //   0xDF BuffDebuff           -> world.Player == null gate
    public class CombatMobilePacketTests : IDisposable
    {
        private readonly World _world;

        public CombatMobilePacketTests()
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

        // ---- 0x1D DeleteObject ----

        [Fact]
        public void DeleteObject_0x1D_RaisesObjectDeleted_WithSerial()
        {
            // Layout: 1D | serial(4)
            ObjectDeletedArgs? captured = null;
            EventSink.ObjectDeleted += e => captured = e;

            byte[] packet =
            {
                0x1D,
                0x12, 0x34, 0x56, 0x78
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x12345678u);
        }

        // ---- 0x2C DeathScreen ----

        [Fact]
        public void DeathScreen_0x2C_Resurrected_RaisesPlayerDeath_Action1()
        {
            // Layout: 2C | action(1). action==1 is the resurrect/clear path
            // that skips world.Player / Client.Game.Audio access in the
            // handler. The death path (action!=1) would deref world.Player
            // and is therefore not exercised here.
            PlayerDeathArgs? captured = null;
            EventSink.PlayerDeath += e => captured = e;

            Dispatch(new byte[] { 0x2C, 0x01 }, 1);

            captured.Should().NotBeNull();
            captured.Value.Action.Should().Be(0x01);
        }

        // ---- 0x2D MobileAttributes ----

        [Fact]
        public void MobileAttributes_0x2D_MobileSerial_RaisesMobileAttributesUpdated()
        {
            // Layout: 2D | serial(4) | hitsMax(2) | hits(2) | manaMax(2) |
            //              mana(2) | stamMax(2) | stam(2)
            // SerialHelper.IsMobile: 0 < serial < 0x40000000.
            MobileAttributesUpdatedArgs? captured = null;
            EventSink.MobileAttributesUpdated += e => captured = e;

            byte[] packet =
            {
                0x2D,
                0x00, 0x00, 0x00, 0x42,  // mobile serial 0x42
                0x00, 0x64,              // hitsMax = 100
                0x00, 0x32,              // hits = 50
                0x00, 0x50,              // manaMax = 80
                0x00, 0x10,              // mana = 16
                0x00, 0x46,              // stamMax = 70
                0x00, 0x23               // stam = 35
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x42u);
            captured.Value.HitsMax.Should().Be(100);
            captured.Value.Hits.Should().Be(50);
            captured.Value.ManaMax.Should().Be(80);
            captured.Value.Mana.Should().Be(16);
            captured.Value.StaminaMax.Should().Be(70);
            captured.Value.Stamina.Should().Be(35);
        }

        [Fact]
        public void MobileAttributes_0x2D_ItemSerial_RaisesHitpointsUpdated_Only()
        {
            // When SerialHelper.IsMobile is false the handler raises
            // HitpointsUpdated rather than MobileAttributesUpdated.
            // Item serials are >= 0x40000000.
            HitpointsUpdatedArgs? hitsCaptured = null;
            bool attribsFired = false;

            EventSink.HitpointsUpdated += e => hitsCaptured = e;
            EventSink.MobileAttributesUpdated += _ => attribsFired = true;

            byte[] packet =
            {
                0x2D,
                0x40, 0x00, 0x00, 0x01,  // item serial 0x40000001
                0x00, 0xC8,              // hitsMax = 200
                0x00, 0x96               // hits    = 150
            };

            Dispatch(packet, 1);

            attribsFired.Should().BeFalse();
            hitsCaptured.Should().NotBeNull();
            hitsCaptured.Value.Serial.Should().Be(0x40000001u);
            hitsCaptured.Value.HitsMax.Should().Be(200);
            hitsCaptured.Value.Hits.Should().Be(150);
        }

        // ---- 0x6C TargetCursor ----

        [Fact]
        public void TargetCursor_0x6C_NeutralCursor_RaisesTargetCursorReceived()
        {
            // Layout: 6C | cursorType(1) | targetId(4) | targetType(1)
            TargetCursorReceivedArgs? captured = null;
            EventSink.TargetCursorReceived += e => captured = e;

            byte[] packet =
            {
                0x6C,
                0x00,                    // cursorType = 0 (object)
                0xCA, 0xFE, 0xBA, 0xBE,  // targetId
                0x03                     // targetType = 3 (e.g. harmful)
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.CursorType.Should().Be(0x00);
            captured.Value.TargetId.Should().Be(0xCAFEBABEu);
            captured.Value.TargetType.Should().Be(0x03);
        }

        [Fact]
        public void TargetCursor_0x6C_LocationCursor_RaisesTargetCursorReceived()
        {
            // Variant: cursorType = 1 (location targeting) with different
            // targetType — the handler is dumb so we just confirm parsing.
            TargetCursorReceivedArgs? captured = null;
            EventSink.TargetCursorReceived += e => captured = e;

            byte[] packet =
            {
                0x6C,
                0x01,                    // cursorType = 1 (location)
                0x00, 0x00, 0x00, 0x07,  // targetId
                0x01                     // targetType = 1 (beneficial)
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.CursorType.Should().Be(0x01);
            captured.Value.TargetId.Should().Be(0x07u);
            captured.Value.TargetType.Should().Be(0x01);
        }

        // ---- 0xA1 UpdateHitpoints ----

        [Fact]
        public void UpdateHitpoints_0xA1_RaisesHitpointsUpdated()
        {
            // Layout: A1 | serial(4) | hitsMax(2) | hits(2)
            HitpointsUpdatedArgs? captured = null;
            EventSink.HitpointsUpdated += e => captured = e;

            byte[] packet =
            {
                0xA1,
                0x00, 0x00, 0x00, 0x01,
                0x01, 0x00,              // hitsMax = 256
                0x00, 0xFF               // hits    = 255
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x01u);
            captured.Value.HitsMax.Should().Be(256);
            captured.Value.Hits.Should().Be(255);
        }

        // ---- 0xA2 UpdateMana ----

        [Fact]
        public void UpdateMana_0xA2_RaisesManaUpdated()
        {
            // Layout: A2 | serial(4) | manaMax(2) | mana(2)
            ManaUpdatedArgs? captured = null;
            EventSink.ManaUpdated += e => captured = e;

            byte[] packet =
            {
                0xA2,
                0x00, 0x00, 0x00, 0x02,
                0x00, 0x7D,              // manaMax = 125
                0x00, 0x40               // mana    = 64
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x02u);
            captured.Value.ManaMax.Should().Be(125);
            captured.Value.Mana.Should().Be(64);
        }

        // ---- 0xA3 UpdateStamina ----

        [Fact]
        public void UpdateStamina_0xA3_RaisesStaminaUpdated()
        {
            // Layout: A3 | serial(4) | stamMax(2) | stam(2)
            StaminaUpdatedArgs? captured = null;
            EventSink.StaminaUpdated += e => captured = e;

            byte[] packet =
            {
                0xA3,
                0x00, 0x00, 0x00, 0x03,
                0x00, 0x5A,              // stamMax = 90
                0x00, 0x1E               // stam    = 30
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x03u);
            captured.Value.StaminaMax.Should().Be(90);
            captured.Value.Stamina.Should().Be(30);
        }

        // ---- 0xAA AttackCharacter ----

        [Fact]
        public void AttackCharacter_0xAA_TargetSerial_RaisesAttackTargetChanged()
        {
            // Layout: AA | serial(4)
            AttackTargetChangedArgs? captured = null;
            EventSink.AttackTargetChanged += e => captured = e;

            byte[] packet =
            {
                0xAA,
                0xDE, 0xAD, 0xBE, 0xEF
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0xDEADBEEFu);
        }

        [Fact]
        public void AttackCharacter_0xAA_ZeroSerial_RaisesAttackTargetChanged_ClearsTarget()
        {
            // Server sends serial=0 to clear the attack target. The handler
            // still raises the event — consumers decide what zero means.
            AttackTargetChangedArgs? captured = null;
            EventSink.AttackTargetChanged += e => captured = e;

            byte[] packet =
            {
                0xAA,
                0x00, 0x00, 0x00, 0x00
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0u);
        }
    }
}
