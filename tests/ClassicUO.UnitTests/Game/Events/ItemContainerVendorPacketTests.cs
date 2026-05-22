// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game;
using ClassicUO.Game.Events;
using ClassicUO.Network;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Events
{
    // Golden-byte tests for the items/containers/vendor bucket of packet
    // handlers. Each test builds a raw packet, dispatches it through
    // PacketHandlers.AnalyzePacket, and asserts that the matching EventSink
    // event fires with the parsed args. Tests use a real World instance so
    // the same handler path runs as in production; EventSink.ClearAll()
    // removes the manager subscriptions World wires up, leaving only the
    // test subscriber listening.
    //
    // Many handlers in this bucket are gated on `world.InGame`
    // (Player != null && Map != null) or `world.Player != null`. A fresh
    // World has neither, and we cannot synthesise a Player/Map without
    // reflection (rule forbids that). Those handlers are covered by
    // negative "gate keeps the event silent" tests below, which still
    // exercise the handler dispatch path and lock in the gate contract.
    public class ItemContainerVendorPacketTests : IDisposable
    {
        private readonly World _world;

        public ItemContainerVendorPacketTests()
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

        // ---- 0x23 DragAnimation (no gate, fixed length 26) ----

        [Fact]
        public void DragAnimation_0x23_RaisesItemDragAnimation_WithAllFields()
        {
            ItemDragAnimationArgs? captured = null;
            EventSink.ItemDragAnimation += e => captured = e;

            // Layout (after packet id):
            //   graphic(2) graphicInc(1) hue(2) count(2)
            //   source(4) sourceX(2) sourceY(2) sourceZ(1)
            //   dest(4)   destX(2)   destY(2)   destZ(1)
            // Handler adds graphicInc directly onto graphic.
            byte[] packet =
            {
                0x23,
                0x10, 0x00,                  // graphic = 0x1000
                0x05,                        // graphicInc -> graphic becomes 0x1005
                0x00, 0x21,                  // hue = 0x21
                0x00, 0x03,                  // count = 3
                0x00, 0x00, 0x00, 0xAA,      // source serial
                0x01, 0x00,                  // sourceX = 256
                0x02, 0x00,                  // sourceY = 512
                0x05,                        // sourceZ = 5
                0x00, 0x00, 0x00, 0xBB,      // dest serial
                0x01, 0x10,                  // destX = 272
                0x02, 0x20,                  // destY = 544
                0xFB                         // destZ = -5 (signed)
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Graphic.Should().Be((ushort)0x1005);
            captured.Value.Hue.Should().Be((ushort)0x21);
            captured.Value.Count.Should().Be((ushort)3);
            captured.Value.Source.Should().Be(0xAAu);
            captured.Value.SourceX.Should().Be((ushort)256);
            captured.Value.SourceY.Should().Be((ushort)512);
            captured.Value.SourceZ.Should().Be((sbyte)5);
            captured.Value.Destination.Should().Be(0xBBu);
            captured.Value.DestinationX.Should().Be((ushort)272);
            captured.Value.DestinationY.Should().Be((ushort)544);
            captured.Value.DestinationZ.Should().Be((sbyte)-5);
        }

        [Fact]
        public void DragAnimation_0x23_GraphicIncZero_LeavesGraphicUnchanged()
        {
            ItemDragAnimationArgs? captured = null;
            EventSink.ItemDragAnimation += e => captured = e;

            byte[] packet =
            {
                0x23,
                0x2A, 0xBC,                  // graphic = 0x2ABC
                0x00,                        // graphicInc = 0
                0x00, 0x00,                  // hue
                0x00, 0x01,                  // count = 1
                0x00, 0x00, 0x00, 0x00,      // source = 0
                0x00, 0x00,
                0x00, 0x00,
                0x00,
                0x00, 0x00, 0x00, 0x00,      // dest = 0
                0x00, 0x00,
                0x00, 0x00,
                0x00
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Graphic.Should().Be((ushort)0x2ABC);
        }

        // ---- 0x27 DenyMoveItem (no gate, fixed length 2) ----

        [Fact]
        public void DenyMoveItem_0x27_RaisesItemMoveDenied_WithCode()
        {
            ItemMoveDeniedArgs? captured = null;
            EventSink.ItemMoveDenied += e => captured = e;

            Dispatch(new byte[] { 0x27, 0x05 }, 1);

            captured.Should().NotBeNull();
            captured.Value.Code.Should().Be((byte)0x05);
        }

        [Fact]
        public void DenyMoveItem_0x27_ZeroCode_StillFires()
        {
            ItemMoveDeniedArgs? captured = null;
            EventSink.ItemMoveDenied += e => captured = e;

            Dispatch(new byte[] { 0x27, 0x00 }, 1);

            captured.Should().NotBeNull();
            captured.Value.Code.Should().Be((byte)0x00);
        }

        // ---- 0x95 DyeData (no gate, fixed length 9) ----

        [Fact]
        public void DyeData_0x95_RaisesDyeDataReceived()
        {
            // Layout: serial(4) skip(2) graphic(2)
            DyeDataReceivedArgs? captured = null;
            EventSink.DyeDataReceived += e => captured = e;

            byte[] packet =
            {
                0x95,
                0x00, 0x00, 0x12, 0x34,      // serial = 0x1234
                0xCC, 0xDD,                  // skipped
                0x0F, 0xA0                   // graphic = 0x0FA0
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x1234u);
            captured.Value.Graphic.Should().Be((ushort)0x0FA0);
        }

        [Fact]
        public void DyeData_0x95_HighSerial_PreservedThroughSkip()
        {
            DyeDataReceivedArgs? captured = null;
            EventSink.DyeDataReceived += e => captured = e;

            byte[] packet =
            {
                0x95,
                0xDE, 0xAD, 0xBE, 0xEF,      // serial
                0x00, 0x00,                  // skipped
                0xFF, 0xFF                   // graphic
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0xDEADBEEFu);
            captured.Value.Graphic.Should().Be((ushort)0xFFFF);
        }

        // ---- 0xDC OPLInfo (gated on ClientFeatures.TooltipsEnabled, defaults true) ----

        [Fact]
        public void OPLInfo_0xDC_RaisesOplInfoReceived()
        {
            // ClientFeatures.TooltipsEnabled defaults to true on a fresh
            // World, so the handler emits without any setup.
            OplInfoReceivedArgs? captured = null;
            EventSink.OplInfoReceived += e => captured = e;

            byte[] packet =
            {
                0xDC,
                0x00, 0x00, 0x40, 0x01,      // serial
                0x00, 0x00, 0x00, 0x2A       // revision
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x4001u);
            captured.Value.Revision.Should().Be(0x2Au);
        }

        [Fact]
        public void OPLInfo_0xDC_LargeRevision_PreservedAsUInt32()
        {
            OplInfoReceivedArgs? captured = null;
            EventSink.OplInfoReceived += e => captured = e;

            byte[] packet =
            {
                0xDC,
                0x00, 0x00, 0x00, 0x01,
                0xFF, 0xFF, 0xFF, 0xFE       // near-max revision
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Revision.Should().Be(0xFFFFFFFEu);
        }

        // ---- Gate-contract tests: these handlers refuse to emit without a
        //      Player/Map (or a backing entity we can't construct without
        //      reflection). Asserting "no event" locks in the gate.

        // 0x1A UpdateItem — gate: world.Player != null. SKIP positive test,
        //                   verify the gate keeps the sink silent.
        [Fact]
        public void UpdateItem_0x1A_NoPlayer_DoesNotFire()
        {
            bool fired = false;
            EventSink.ItemUpdated += _ => fired = true;

            // Minimum well-formed UpdateItem payload (variable-length 0x1A,
            // so offset=3 after id + length bytes). Even with valid bytes the
            // gate aborts before the read.
            byte[] packet =
            {
                0x1A,
                0x00, 0x14,                  // length placeholder
                0x00, 0x00, 0x00, 0x42,      // serial
                0x40, 0x00,                  // graphic
                0x00, 0x01,                  // amount (not read because high bit clear)
                0x00, 0x05,                  // x
                0x00, 0x06,                  // y
                0x07,                        // z
                0x00                         // padding
            };

            Dispatch(packet, 3);

            fired.Should().BeFalse();
        }

        // 0xF3 UpdateItemSA — gate: world.Player != null. SKIP positive.
        [Fact]
        public void UpdateItemSA_0xF3_NoPlayer_DoesNotFire()
        {
            bool fired = false;
            EventSink.ItemUpdated += _ => fired = true;

            // 0xF3 is fixed-length (0x1A bytes). offset=1.
            byte[] packet = new byte[26];
            packet[0] = 0xF3;

            Dispatch(packet, 1);

            fired.Should().BeFalse();
        }

        // 0x24 OpenContainer — gate: world.Player != null. SKIP positive.
        [Fact]
        public void OpenContainer_0x24_NoPlayer_DoesNotFire()
        {
            bool fired = false;
            EventSink.ContainerOpened += _ => fired = true;

            // Fixed length 7: id + serial(4) + graphic(2)
            byte[] packet =
            {
                0x24,
                0x00, 0x00, 0x00, 0x10,
                0x00, 0x42
            };

            Dispatch(packet, 1);

            fired.Should().BeFalse();
        }

        // 0x25 UpdateContainedItem — gate: world.InGame. SKIP positive.
        [Fact]
        public void UpdateContainedItem_0x25_NotInGame_DoesNotFire()
        {
            bool fired = false;
            EventSink.ContainerItemAdded += _ => fired = true;

            byte[] packet = new byte[20];
            packet[0] = 0x25;
            Dispatch(packet, 1);

            fired.Should().BeFalse();
        }

        // 0x3C UpdateContainedItems — gate: world.InGame. SKIP positive.
        [Fact]
        public void UpdateContainedItems_0x3C_NotInGame_DoesNotFire()
        {
            bool addedFired = false;
            bool receivedFired = false;
            EventSink.ContainerItemAdded += _ => addedFired = true;
            EventSink.ContainerItemsReceived += _ => receivedFired = true;

            // Variable-length, offset=3
            byte[] packet =
            {
                0x3C,
                0x00, 0x05,                  // length placeholder
                0x00, 0x00                   // count = 0
            };

            Dispatch(packet, 3);

            addedFired.Should().BeFalse();
            receivedFired.Should().BeFalse();
        }

        // 0x2E EquipItem — gate: world.InGame. SKIP positive.
        [Fact]
        public void EquipItem_0x2E_NotInGame_DoesNotFire()
        {
            bool fired = false;
            EventSink.ItemEquipped += _ => fired = true;

            // Fixed length 15
            byte[] packet = new byte[15];
            packet[0] = 0x2E;
            Dispatch(packet, 1);

            fired.Should().BeFalse();
        }

        // 0x89 CorpseEquipment — gate: world.InGame (also needs entity).
        //                       SKIP positive.
        [Fact]
        public void CorpseEquipment_0x89_NotInGame_DoesNotFire()
        {
            bool fired = false;
            EventSink.CorpseEquipmentReceived += _ => fired = true;

            byte[] packet =
            {
                0x89,
                0x00, 0x10,                  // length placeholder
                0x00, 0x00, 0x00, 0x05,      // corpse serial
                0xFF                         // Layer.Invalid sentinel
            };

            Dispatch(packet, 3);

            fired.Should().BeFalse();
        }

        // 0xD6 MegaCliloc — gate: world.InGame. SKIP positive.
        [Fact]
        public void MegaCliloc_0xD6_NotInGame_DoesNotFire()
        {
            bool fired = false;
            EventSink.MegaClilocReceived += _ => fired = true;

            byte[] packet =
            {
                0xD6,
                0x00, 0x0F,                  // length placeholder
                0x00, 0x01,                  // unknown
                0x00, 0x00, 0x00, 0x10,      // serial
                0x00, 0x00,                  // skip
                0x00, 0x00, 0x00, 0x2A,      // revision
                0x00, 0x00, 0x00, 0x00       // cliloc=0 terminator
            };

            Dispatch(packet, 3);

            fired.Should().BeFalse();
        }

        // 0x28 EndDraggingItem — gate: world.InGame. SKIP positive.
        [Fact]
        public void EndDraggingItem_0x28_NotInGame_DoesNotFire()
        {
            bool fired = false;
            EventSink.ItemDragEnded += _ => fired = true;

            // Fixed length 5
            byte[] packet = new byte[5];
            packet[0] = 0x28;
            Dispatch(packet, 1);

            fired.Should().BeFalse();
        }

        // 0x29 DropItemAccepted — gate: world.InGame. SKIP positive.
        [Fact]
        public void DropItemAccepted_0x29_NotInGame_DoesNotFire()
        {
            bool fired = false;
            EventSink.ItemDropAccepted += _ => fired = true;

            Dispatch(new byte[] { 0x29 }, 1);

            fired.Should().BeFalse();
        }

        // 0x74 BuyList — gate: world.InGame (and needs container+vendor).
        //                SKIP positive.
        [Fact]
        public void BuyList_0x74_NotInGame_DoesNotFire()
        {
            bool fired = false;
            EventSink.ShopBuyListReceived += _ => fired = true;

            byte[] packet =
            {
                0x74,
                0x00, 0x09,
                0x00, 0x00, 0x00, 0x10,      // container serial
                0x00                         // count
            };

            Dispatch(packet, 3);

            fired.Should().BeFalse();
        }

        // 0x9E SellList — gate: world.InGame (and needs vendor mobile).
        //                 SKIP positive.
        [Fact]
        public void SellList_0x9E_NotInGame_DoesNotFire()
        {
            bool fired = false;
            EventSink.ShopSellListReceived += _ => fired = true;

            byte[] packet =
            {
                0x9E,
                0x00, 0x09,
                0x00, 0x00, 0x00, 0x10,      // vendor serial
                0x00, 0x00                   // count = 0
            };

            Dispatch(packet, 3);

            fired.Should().BeFalse();
        }

        // 0x6F SecureTrading — gate: world.InGame. All four subtypes
        //                      (open / close / accept-updated / currency)
        //                      share the same gate, so none of them fires.
        [Fact]
        public void SecureTrading_0x6F_NotInGame_AllSubtypesSilent()
        {
            bool openedFired = false;
            bool closedFired = false;
            bool acceptFired = false;
            bool currencyFired = false;
            EventSink.TradeWindowOpened += _ => openedFired = true;
            EventSink.TradeWindowClosed += _ => closedFired = true;
            EventSink.TradeWindowAcceptUpdated += _ => acceptFired = true;
            EventSink.TradeWindowCurrencyUpdated += _ => currencyFired = true;

            // Close (type=1): id + len(2) + type(1) + serial(4) = 8 bytes
            byte[] close =
            {
                0x6F,
                0x00, 0x08,
                0x01,
                0x00, 0x00, 0x00, 0x42
            };
            Dispatch(close, 3);

            // AcceptUpdated (type=2)
            byte[] accept =
            {
                0x6F,
                0x00, 0x10,
                0x02,
                0x00, 0x00, 0x00, 0x42,
                0x00, 0x00, 0x00, 0x01,
                0x00, 0x00, 0x00, 0x00
            };
            Dispatch(accept, 3);

            // Currency (type=4 -> isMine=true)
            byte[] currency =
            {
                0x6F,
                0x00, 0x10,
                0x04,
                0x00, 0x00, 0x00, 0x42,
                0x00, 0x00, 0x00, 0x64,
                0x00, 0x00, 0x00, 0x32
            };
            Dispatch(currency, 3);

            openedFired.Should().BeFalse();
            closedFired.Should().BeFalse();
            acceptFired.Should().BeFalse();
            currencyFired.Should().BeFalse();
        }
    }
}
