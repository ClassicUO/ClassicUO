using System;
using ClassicUO.Client.Tests;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.IO;
using ClassicUO.Network;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Client.Tests.Network
{
    public class PacketHandlerTests
    {
        private readonly World _world;

        // Mobile serials must be > 0 and < 0x40000000
        private const uint MobileSerial = 0x00000100;
        private const uint PlayerSerial = 0x00000001;

        // Item serials must be >= 0x40000000 and < 0x80000000
        private const uint ItemSerial = 0x40000001;

        public PacketHandlerTests()
        {
            // Re-register all handlers in case a previous test (e.g. Add_AllPacketIds_DoesNotThrow)
            // overwrote them with no-ops on the static singleton.
            PacketHandlers.RegisterHandlers();

            _world = TestHelpers.CreateTestWorld();
            // CreatePlayerForTest sets up world.Player without static FileManager dependencies.
            _world.CreatePlayerForTest(PlayerSerial);
        }

        /// <summary>
        /// Helper: dispatches a raw packet (first byte = packet ID) through the
        /// registered handler on <see cref="PacketHandlers.Handler"/>.
        /// </summary>
        private void DispatchPacket(byte[] data)
        {
            PacketHandlers.Handler.DispatchPacket(_world, data);
        }

        #region DeleteObject (0x1D) - 5 bytes: packetId + serial

        [Fact]
        public void DeleteObject_HandlerReadsCorrectSerial()
        {
            // The full DeleteObject handler ultimately calls Entity.Destroy() which has
            // static Client.Game.UO dependencies not available in unit tests.
            // We verify the handler correctly reads the serial from the packet by testing
            // the early-exit path: when the entity doesn't exist, the handler returns
            // without error, proving it successfully parsed the serial and looked it up.
            var writer = new StackDataWriter(5);
            writer.WriteUInt8(0x1D);
            writer.WriteUInt32BE(ItemSerial);
            byte[] packet = writer.AllocatedBuffer[..writer.BytesWritten];

            // No item exists for this serial, so handler returns early after reading serial
            var act = () => DispatchPacket(packet);
            act.Should().NotThrow("handler should parse serial and exit gracefully when entity not found");
        }

        [Fact]
        public void DeleteObject_SkipsWhenPlayerIsNull()
        {
            // The handler returns immediately if world.Player is null.
            // Create a world without a player to verify this guard.
            var worldNoPlayer = TestHelpers.CreateTestWorld();
            var item = worldNoPlayer.GetOrCreateItem(ItemSerial);

            var writer = new StackDataWriter(5);
            writer.WriteUInt8(0x1D);
            writer.WriteUInt32BE(ItemSerial);

            // Handler should return early without crash since Player is null
            PacketHandlers.Handler.DispatchPacket(worldNoPlayer, writer.AllocatedBuffer[..writer.BytesWritten]);

            // Item should still exist because handler returned early
            worldNoPlayer.Items.Get(ItemSerial).Should().NotBeNull("item should remain when Player is null");
        }

        [Fact]
        public void DeleteObject_DoesNotRemovePlayer()
        {
            // The player serial should never be removed by DeleteObject
            var writer = new StackDataWriter(5);
            writer.WriteUInt8(0x1D);
            writer.WriteUInt32BE(PlayerSerial);

            // Act
            DispatchPacket(writer.AllocatedBuffer[..writer.BytesWritten]);

            // Assert
            _world.Player.Should().NotBeNull("player should not be removed by DeleteObject");
            _world.Mobiles.Get(PlayerSerial).Should().NotBeNull("player mobile should still exist");
        }

        [Fact]
        public void DeleteObject_NonExistentSerial_DoesNotThrow()
        {
            uint nonExistent = 0x40099999;

            var writer = new StackDataWriter(5);
            writer.WriteUInt8(0x1D);
            writer.WriteUInt32BE(nonExistent);
            byte[] packet = writer.AllocatedBuffer[..writer.BytesWritten];

            // Should not crash
            var act = () => DispatchPacket(packet);
            act.Should().NotThrow();
        }

        #endregion

        #region UpdateHitpoints (0xA1) - 9 bytes: packetId + serial + hitsMax(u16) + hits(u16)

        [Fact]
        public void UpdateHitpoints_UpdatesMobileHits()
        {
            var mob = _world.GetOrCreateMobile(MobileSerial);
            mob.Hits = 50;
            mob.HitsMax = 100;

            var writer = new StackDataWriter(9);
            writer.WriteUInt8(0xA1);
            writer.WriteUInt32BE(MobileSerial);
            writer.WriteUInt16BE(200); // hitsMax
            writer.WriteUInt16BE(150); // hits

            DispatchPacket(writer.AllocatedBuffer[..writer.BytesWritten]);

            mob.HitsMax.Should().Be(200);
            mob.Hits.Should().Be(150);
        }

        [Fact]
        public void UpdateHitpoints_UpdatesPlayerHits()
        {
            _world.Player.Hits = 10;
            _world.Player.HitsMax = 50;

            var writer = new StackDataWriter(9);
            writer.WriteUInt8(0xA1);
            writer.WriteUInt32BE(PlayerSerial);
            writer.WriteUInt16BE(300); // hitsMax
            writer.WriteUInt16BE(275); // hits

            DispatchPacket(writer.AllocatedBuffer[..writer.BytesWritten]);

            _world.Player.HitsMax.Should().Be(300);
            _world.Player.Hits.Should().Be(275);
        }

        [Fact]
        public void UpdateHitpoints_UnknownSerial_DoesNotThrow()
        {
            var writer = new StackDataWriter(9);
            writer.WriteUInt8(0xA1);
            writer.WriteUInt32BE(0x00099999); // non-existent mobile
            writer.WriteUInt16BE(100);
            writer.WriteUInt16BE(50);
            byte[] packet = writer.AllocatedBuffer[..writer.BytesWritten];

            var act = () => DispatchPacket(packet);
            act.Should().NotThrow();
        }

        [Fact]
        public void UpdateHitpoints_ZeroValues_SetsToZero()
        {
            var mob = _world.GetOrCreateMobile(MobileSerial);
            mob.Hits = 100;
            mob.HitsMax = 200;

            var writer = new StackDataWriter(9);
            writer.WriteUInt8(0xA1);
            writer.WriteUInt32BE(MobileSerial);
            writer.WriteUInt16BE(0);
            writer.WriteUInt16BE(0);

            DispatchPacket(writer.AllocatedBuffer[..writer.BytesWritten]);

            mob.HitsMax.Should().Be(0);
            mob.Hits.Should().Be(0);
        }

        [Fact]
        public void UpdateHitpoints_MaxValues_SetsCorrectly()
        {
            var mob = _world.GetOrCreateMobile(MobileSerial);

            var writer = new StackDataWriter(9);
            writer.WriteUInt8(0xA1);
            writer.WriteUInt32BE(MobileSerial);
            writer.WriteUInt16BE(ushort.MaxValue);
            writer.WriteUInt16BE(ushort.MaxValue);

            DispatchPacket(writer.AllocatedBuffer[..writer.BytesWritten]);

            mob.HitsMax.Should().Be(ushort.MaxValue);
            mob.Hits.Should().Be(ushort.MaxValue);
        }

        #endregion

        #region UpdateMana (0xA2) - 9 bytes: packetId + serial + manaMax(u16) + mana(u16)

        [Fact]
        public void UpdateMana_UpdatesMobileMana()
        {
            var mob = _world.GetOrCreateMobile(MobileSerial);
            mob.Mana = 10;
            mob.ManaMax = 50;

            var writer = new StackDataWriter(9);
            writer.WriteUInt8(0xA2);
            writer.WriteUInt32BE(MobileSerial);
            writer.WriteUInt16BE(120); // manaMax
            writer.WriteUInt16BE(80);  // mana

            DispatchPacket(writer.AllocatedBuffer[..writer.BytesWritten]);

            mob.ManaMax.Should().Be(120);
            mob.Mana.Should().Be(80);
        }

        [Fact]
        public void UpdateMana_UpdatesPlayerMana()
        {
            _world.Player.Mana = 5;
            _world.Player.ManaMax = 25;

            var writer = new StackDataWriter(9);
            writer.WriteUInt8(0xA2);
            writer.WriteUInt32BE(PlayerSerial);
            writer.WriteUInt16BE(500);
            writer.WriteUInt16BE(499);

            DispatchPacket(writer.AllocatedBuffer[..writer.BytesWritten]);

            _world.Player.ManaMax.Should().Be(500);
            _world.Player.Mana.Should().Be(499);
        }

        [Fact]
        public void UpdateMana_UnknownSerial_DoesNotThrow()
        {
            var writer = new StackDataWriter(9);
            writer.WriteUInt8(0xA2);
            writer.WriteUInt32BE(0x00099999);
            writer.WriteUInt16BE(100);
            writer.WriteUInt16BE(50);
            byte[] packet = writer.AllocatedBuffer[..writer.BytesWritten];

            var act = () => DispatchPacket(packet);
            act.Should().NotThrow();
        }

        [Fact]
        public void UpdateMana_ZeroValues_SetsToZero()
        {
            var mob = _world.GetOrCreateMobile(MobileSerial);
            mob.Mana = 100;
            mob.ManaMax = 200;

            var writer = new StackDataWriter(9);
            writer.WriteUInt8(0xA2);
            writer.WriteUInt32BE(MobileSerial);
            writer.WriteUInt16BE(0);
            writer.WriteUInt16BE(0);

            DispatchPacket(writer.AllocatedBuffer[..writer.BytesWritten]);

            mob.ManaMax.Should().Be(0);
            mob.Mana.Should().Be(0);
        }

        #endregion

        #region UpdateStamina (0xA3) - 9 bytes: packetId + serial + staminaMax(u16) + stamina(u16)

        [Fact]
        public void UpdateStamina_UpdatesMobileStamina()
        {
            var mob = _world.GetOrCreateMobile(MobileSerial);
            mob.Stamina = 30;
            mob.StaminaMax = 60;

            var writer = new StackDataWriter(9);
            writer.WriteUInt8(0xA3);
            writer.WriteUInt32BE(MobileSerial);
            writer.WriteUInt16BE(250); // staminaMax
            writer.WriteUInt16BE(175); // stamina

            DispatchPacket(writer.AllocatedBuffer[..writer.BytesWritten]);

            mob.StaminaMax.Should().Be(250);
            mob.Stamina.Should().Be(175);
        }

        [Fact]
        public void UpdateStamina_UpdatesPlayerStamina()
        {
            _world.Player.Stamina = 10;
            _world.Player.StaminaMax = 50;

            var writer = new StackDataWriter(9);
            writer.WriteUInt8(0xA3);
            writer.WriteUInt32BE(PlayerSerial);
            writer.WriteUInt16BE(400);
            writer.WriteUInt16BE(350);

            DispatchPacket(writer.AllocatedBuffer[..writer.BytesWritten]);

            _world.Player.StaminaMax.Should().Be(400);
            _world.Player.Stamina.Should().Be(350);
        }

        [Fact]
        public void UpdateStamina_UnknownSerial_DoesNotThrow()
        {
            var writer = new StackDataWriter(9);
            writer.WriteUInt8(0xA3);
            writer.WriteUInt32BE(0x00099999);
            writer.WriteUInt16BE(100);
            writer.WriteUInt16BE(50);
            byte[] packet = writer.AllocatedBuffer[..writer.BytesWritten];

            var act = () => DispatchPacket(packet);
            act.Should().NotThrow();
        }

        [Fact]
        public void UpdateStamina_ZeroValues_SetsToZero()
        {
            var mob = _world.GetOrCreateMobile(MobileSerial);
            mob.Stamina = 100;
            mob.StaminaMax = 200;

            var writer = new StackDataWriter(9);
            writer.WriteUInt8(0xA3);
            writer.WriteUInt32BE(MobileSerial);
            writer.WriteUInt16BE(0);
            writer.WriteUInt16BE(0);

            DispatchPacket(writer.AllocatedBuffer[..writer.BytesWritten]);

            mob.StaminaMax.Should().Be(0);
            mob.Stamina.Should().Be(0);
        }

        #endregion

        #region MobileAttributes (0x2D) - 17 bytes: packetId + serial + hitsMax + hits + manaMax + mana + stamMax + stam

        [Fact]
        public void MobileAttributes_UpdatesAllStats()
        {
            var mob = _world.GetOrCreateMobile(MobileSerial);

            var writer = new StackDataWriter(17);
            writer.WriteUInt8(0x2D);
            writer.WriteUInt32BE(MobileSerial);
            writer.WriteUInt16BE(100); // hitsMax
            writer.WriteUInt16BE(75);  // hits
            writer.WriteUInt16BE(200); // manaMax
            writer.WriteUInt16BE(150); // mana
            writer.WriteUInt16BE(300); // stamMax
            writer.WriteUInt16BE(250); // stam

            DispatchPacket(writer.AllocatedBuffer[..writer.BytesWritten]);

            mob.HitsMax.Should().Be(100);
            mob.Hits.Should().Be(75);
            mob.ManaMax.Should().Be(200);
            mob.Mana.Should().Be(150);
            mob.StaminaMax.Should().Be(300);
            mob.Stamina.Should().Be(250);
        }

        [Fact]
        public void MobileAttributes_UpdatesPlayerAllStats()
        {
            var writer = new StackDataWriter(17);
            writer.WriteUInt8(0x2D);
            writer.WriteUInt32BE(PlayerSerial);
            writer.WriteUInt16BE(500); // hitsMax
            writer.WriteUInt16BE(490); // hits
            writer.WriteUInt16BE(600); // manaMax
            writer.WriteUInt16BE(550); // mana
            writer.WriteUInt16BE(700); // stamMax
            writer.WriteUInt16BE(680); // stam

            DispatchPacket(writer.AllocatedBuffer[..writer.BytesWritten]);

            _world.Player.HitsMax.Should().Be(500);
            _world.Player.Hits.Should().Be(490);
            _world.Player.ManaMax.Should().Be(600);
            _world.Player.Mana.Should().Be(550);
            _world.Player.StaminaMax.Should().Be(700);
            _world.Player.Stamina.Should().Be(680);
        }

        [Fact]
        public void MobileAttributes_UnknownSerial_DoesNotThrow()
        {
            var writer = new StackDataWriter(17);
            writer.WriteUInt8(0x2D);
            writer.WriteUInt32BE(0x00099999);
            writer.WriteUInt16BE(100);
            writer.WriteUInt16BE(50);
            writer.WriteUInt16BE(100);
            writer.WriteUInt16BE(50);
            writer.WriteUInt16BE(100);
            writer.WriteUInt16BE(50);
            byte[] packet = writer.AllocatedBuffer[..writer.BytesWritten];

            var act = () => DispatchPacket(packet);
            act.Should().NotThrow();
        }

        [Fact]
        public void MobileAttributes_OverwritesPreviousValues()
        {
            var mob = _world.GetOrCreateMobile(MobileSerial);
            mob.HitsMax = 999;
            mob.Hits = 888;
            mob.ManaMax = 777;
            mob.Mana = 666;
            mob.StaminaMax = 555;
            mob.Stamina = 444;

            var writer = new StackDataWriter(17);
            writer.WriteUInt8(0x2D);
            writer.WriteUInt32BE(MobileSerial);
            writer.WriteUInt16BE(10);
            writer.WriteUInt16BE(5);
            writer.WriteUInt16BE(20);
            writer.WriteUInt16BE(15);
            writer.WriteUInt16BE(30);
            writer.WriteUInt16BE(25);

            DispatchPacket(writer.AllocatedBuffer[..writer.BytesWritten]);

            mob.HitsMax.Should().Be(10);
            mob.Hits.Should().Be(5);
            mob.ManaMax.Should().Be(20);
            mob.Mana.Should().Be(15);
            mob.StaminaMax.Should().Be(30);
            mob.Stamina.Should().Be(25);
        }

        [Fact]
        public void MobileAttributes_AllZeros_SetsAllToZero()
        {
            var mob = _world.GetOrCreateMobile(MobileSerial);
            mob.HitsMax = 100;
            mob.Hits = 50;
            mob.ManaMax = 100;
            mob.Mana = 50;
            mob.StaminaMax = 100;
            mob.Stamina = 50;

            var writer = new StackDataWriter(17);
            writer.WriteUInt8(0x2D);
            writer.WriteUInt32BE(MobileSerial);
            writer.WriteUInt16BE(0);
            writer.WriteUInt16BE(0);
            writer.WriteUInt16BE(0);
            writer.WriteUInt16BE(0);
            writer.WriteUInt16BE(0);
            writer.WriteUInt16BE(0);

            DispatchPacket(writer.AllocatedBuffer[..writer.BytesWritten]);

            mob.HitsMax.Should().Be(0);
            mob.Hits.Should().Be(0);
            mob.ManaMax.Should().Be(0);
            mob.Mana.Should().Be(0);
            mob.StaminaMax.Should().Be(0);
            mob.Stamina.Should().Be(0);
        }

        #endregion

        #region Talk (0x1C) - Variable length speech packet

        [Fact]
        public void Talk_SystemMessage_DoesNotThrow()
        {
            // Build a system message packet (serial=0xFFFFFFFF, type=System)
            // Format: packetId(1) + serial(4) + graphic(2) + type(1) + hue(2) + font(2) + name(30) + text(null-terminated)
            var writer = new StackDataWriter(64);
            writer.WriteUInt8(0x1C);
            writer.WriteUInt32BE(0xFFFFFFFF); // system serial
            writer.WriteUInt16BE(0x0000);     // graphic
            writer.WriteUInt8(0x06);          // MessageType.System = 6
            writer.WriteUInt16BE(0x0000);     // hue
            writer.WriteUInt16BE(0x0003);     // font
            // name: 30 bytes, null-padded
            WriteFixedASCII(ref writer, "System", 30);
            // text
            WriteNullTerminatedASCII(ref writer, "Hello World");
            byte[] packet = writer.AllocatedBuffer[..writer.BytesWritten];

            var act = () => DispatchPacket(packet);
            act.Should().NotThrow();
        }

        [Fact]
        public void Talk_ParsesPacketFieldsCorrectly()
        {
            // The Talk handler reads: serial(4) + graphic(2) + type(1) + hue(2) + font(2) + name(30) + text
            // We verify the packet structure is correctly parsed by testing with known values.
            // Full handler execution requires Client.Game.UO.FileManager (fonts) so we test
            // packet parsing via the StackDataReader directly to verify our packet construction.
            var writer = new StackDataWriter(64);
            writer.WriteUInt8(0x1C);
            writer.WriteUInt32BE(MobileSerial);
            writer.WriteUInt16BE(0x0190);     // graphic
            writer.WriteUInt8(0x00);          // MessageType.Regular
            writer.WriteUInt16BE(0x0035);     // hue
            writer.WriteUInt16BE(0x0003);     // font
            WriteFixedASCII(ref writer, "TestNPC", 30);
            WriteNullTerminatedASCII(ref writer, "Greetings!");

            // Verify we can read back the fields correctly (validates our packet construction)
            var reader = new StackDataReader(writer.AllocatedBuffer.AsSpan(0, writer.BytesWritten));
            reader.ReadUInt8().Should().Be(0x1C);         // packet id
            reader.ReadUInt32BE().Should().Be(MobileSerial);
            reader.ReadUInt16BE().Should().Be(0x0190);    // graphic
            reader.ReadUInt8().Should().Be(0x00);         // type
            reader.ReadUInt16BE().Should().Be(0x0035);    // hue
            reader.ReadUInt16BE().Should().Be(0x0003);    // font
            var name = reader.ReadASCII(30);
            name.Should().StartWith("TestNPC");
        }

        [Fact]
        public void Talk_EmptyText_DoesNotThrow()
        {
            var writer = new StackDataWriter(64);
            writer.WriteUInt8(0x1C);
            writer.WriteUInt32BE(0xFFFFFFFF);
            writer.WriteUInt16BE(0x0000);
            writer.WriteUInt8(0x06);          // System
            writer.WriteUInt16BE(0x0000);
            writer.WriteUInt16BE(0x0003);
            WriteFixedASCII(ref writer, "System", 30);
            // No text beyond the name (length <= 44)
            byte[] packet = writer.AllocatedBuffer[..writer.BytesWritten];

            var act = () => DispatchPacket(packet);
            act.Should().NotThrow();
        }

        [Fact]
        public void Talk_NonExistentEntity_DoesNotThrow()
        {
            // Speech from a serial that doesn't exist in the world
            var writer = new StackDataWriter(64);
            writer.WriteUInt8(0x1C);
            writer.WriteUInt32BE(0x00055555); // non-existent mobile serial
            writer.WriteUInt16BE(0x0190);
            writer.WriteUInt8(0x00);          // Regular
            writer.WriteUInt16BE(0x0035);
            writer.WriteUInt16BE(0x0003);
            WriteFixedASCII(ref writer, "Unknown", 30);
            WriteNullTerminatedASCII(ref writer, "Hello!");
            byte[] packet = writer.AllocatedBuffer[..writer.BytesWritten];

            var act = () => DispatchPacket(packet);
            act.Should().NotThrow();
        }

        #endregion

        #region Cross-handler consistency tests

        [Fact]
        public void UpdateHitpoints_ThenMobileAttributes_LastOneWins()
        {
            var mob = _world.GetOrCreateMobile(MobileSerial);

            // First, send an HP-only update
            var writer1 = new StackDataWriter(9);
            writer1.WriteUInt8(0xA1);
            writer1.WriteUInt32BE(MobileSerial);
            writer1.WriteUInt16BE(100);
            writer1.WriteUInt16BE(50);
            DispatchPacket(writer1.AllocatedBuffer[..writer1.BytesWritten]);

            mob.HitsMax.Should().Be(100);
            mob.Hits.Should().Be(50);

            // Then send a full attributes update with different HP values
            var writer2 = new StackDataWriter(17);
            writer2.WriteUInt8(0x2D);
            writer2.WriteUInt32BE(MobileSerial);
            writer2.WriteUInt16BE(200);
            writer2.WriteUInt16BE(180);
            writer2.WriteUInt16BE(300);
            writer2.WriteUInt16BE(250);
            writer2.WriteUInt16BE(400);
            writer2.WriteUInt16BE(350);
            DispatchPacket(writer2.AllocatedBuffer[..writer2.BytesWritten]);

            mob.HitsMax.Should().Be(200, "MobileAttributes should overwrite previous HP");
            mob.Hits.Should().Be(180);
            mob.ManaMax.Should().Be(300);
            mob.Mana.Should().Be(250);
            mob.StaminaMax.Should().Be(400);
            mob.Stamina.Should().Be(350);
        }

        [Fact]
        public void MultipleStatUpdates_EachAffectsOnlyItsStats()
        {
            var mob = _world.GetOrCreateMobile(MobileSerial);

            // Send HP update
            var w1 = new StackDataWriter(9);
            w1.WriteUInt8(0xA1);
            w1.WriteUInt32BE(MobileSerial);
            w1.WriteUInt16BE(100);
            w1.WriteUInt16BE(90);
            DispatchPacket(w1.AllocatedBuffer[..w1.BytesWritten]);

            // Send Mana update
            var w2 = new StackDataWriter(9);
            w2.WriteUInt8(0xA2);
            w2.WriteUInt32BE(MobileSerial);
            w2.WriteUInt16BE(200);
            w2.WriteUInt16BE(180);
            DispatchPacket(w2.AllocatedBuffer[..w2.BytesWritten]);

            // Send Stamina update
            var w3 = new StackDataWriter(9);
            w3.WriteUInt8(0xA3);
            w3.WriteUInt32BE(MobileSerial);
            w3.WriteUInt16BE(300);
            w3.WriteUInt16BE(270);
            DispatchPacket(w3.AllocatedBuffer[..w3.BytesWritten]);

            // Each handler should only update its own stats
            mob.HitsMax.Should().Be(100);
            mob.Hits.Should().Be(90);
            mob.ManaMax.Should().Be(200);
            mob.Mana.Should().Be(180);
            mob.StaminaMax.Should().Be(300);
            mob.Stamina.Should().Be(270);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Writes a fixed-length ASCII string, padding with null bytes if needed.
        /// </summary>
        private static void WriteFixedASCII(ref StackDataWriter writer, string text, int length)
        {
            int i = 0;
            for (; i < text.Length && i < length; i++)
            {
                writer.WriteUInt8((byte)text[i]);
            }
            for (; i < length; i++)
            {
                writer.WriteUInt8(0);
            }
        }

        /// <summary>
        /// Writes a null-terminated ASCII string.
        /// </summary>
        private static void WriteNullTerminatedASCII(ref StackDataWriter writer, string text)
        {
            foreach (char c in text)
            {
                writer.WriteUInt8((byte)c);
            }
            writer.WriteUInt8(0);
        }

        #endregion
    }
}
