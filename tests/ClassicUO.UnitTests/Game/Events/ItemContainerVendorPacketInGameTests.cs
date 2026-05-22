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
    // Positive parse-and-emit tests for the items / containers / vendor /
    // OPL handler bucket that previously had to be skipped because they
    // gate on `world.InGame` (Player != null && Map != null) or
    // `world.Player != null`. The new World test seams
    //   - SetPlayerForTests(new PlayerMobile(world, serial))
    //   - SetMapForTests(new Map(world, 0))
    // satisfy the gate without reflection. The fixture seeds both, then
    // calls EventSink.ClearAll() so World's auto-wired subscribers don't
    // also run — the test subscriber is the only listener for the event.
    //
    // Handlers covered:
    //   0x1A UpdateItem            -> ItemUpdatedArgs
    //   0xF3 UpdateItemSA          -> ItemUpdatedArgs (SA path)
    //   0x24 OpenContainer         -> ContainerOpenedArgs
    //   0x2E EquipItem             -> ItemEquippedArgs
    //   0x89 CorpseEquipment       -> CorpseEquipmentReceivedArgs
    //   0xD6 MegaCliloc            -> MegaClilocReceivedArgs
    //   0x28 EndDraggingItem       -> ItemDragEndedArgs
    //   0x29 DropItemAccepted      -> ItemDropAcceptedArgs
    //   0x74 BuyList               -> ShopBuyListReceivedArgs
    //   0x9E SellList              -> ShopSellListReceivedArgs
    //   0x6F SecureTrading         -> TradeWindowOpened / Closed /
    //                                 AcceptUpdated / CurrencyUpdated
    //
    // Handlers left out of the positive set:
    //   0x25 UpdateContainedItem  and
    //   0x3C UpdateContainedItems
    // both call `Client.Game.UO.Version >= CV_6017` unconditionally, and
    // `Client.Game` is null in tests, so they NRE on a path we can't
    // avoid without either production changes or reflection. They remain
    // covered by the gate-contract tests in ItemContainerVendorPacketTests.
    [Collection("EventSinkSerial")]
    public class ItemContainerVendorPacketInGameTests : IDisposable
    {
        private readonly World _world;

        public ItemContainerVendorPacketInGameTests()
        {
            _world = new World();
            _world.SetPlayerForTests(new PlayerMobile(_world, 0x00000001u));
            _world.SetMapForTests(new ClassicUO.Game.Map.Map(_world, 0));

            // Clear after seeding so World's auto-wired subscribers don't
            // run on dispatch — the per-test subscriber is the only one.
            EventSink.ClearAll();
        }

        public void Dispose()
        {
            EventSink.ClearAll();
        }

        private void Dispatch(byte[] data, int offset)
            => PacketHandlers.Handler.AnalyzePacket(_world, data, offset);

        // ---- 0x1A UpdateItem (variable length, offset = 3) ----

        [Fact]
        public void UpdateItem_0x1A_RaisesItemUpdated_MinimalLayout()
        {
            ItemUpdatedArgs? captured = null;
            EventSink.ItemUpdated += e => captured = e;

            // Minimal layout — no count flag, no graphic-inc flag, no
            // direction flag, no hue flag, no flags flag.
            //   serial(4) graphic(2) x(2) y(2) z(1)
            byte[] packet =
            {
                0x1A,
                0x00, 0x14,                  // length placeholder (unused)
                0x00, 0x00, 0x00, 0x42,      // serial = 0x42
                0x10, 0x00,                  // graphic = 0x1000
                0x00, 0x05,                  // x = 5
                0x00, 0x06,                  // y = 6
                0x07                         // z = 7
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x42u);
            captured.Value.Graphic.Should().Be((ushort)0x1000);
            captured.Value.GraphicInc.Should().Be((byte)0);
            // count defaults to 1 when high bit of serial is clear
            captured.Value.Amount.Should().Be((ushort)1);
            captured.Value.X.Should().Be((ushort)5);
            captured.Value.Y.Should().Be((ushort)6);
            captured.Value.Z.Should().Be((sbyte)7);
            captured.Value.Hue.Should().Be((ushort)0);
            captured.Value.Type.Should().Be((byte)0);
        }

        [Fact]
        public void UpdateItem_0x1A_HighBitSerial_ReadsExplicitCount()
        {
            ItemUpdatedArgs? captured = null;
            EventSink.ItemUpdated += e => captured = e;

            // Serial high bit set -> handler reads explicit ushort count.
            // Graphic high bit set -> handler reads graphicInc byte.
            // Graphic >= 0x4000 -> handler sets type = 2 (after the high
            // bit is stripped, the produced graphic is 0x4001).
            //   serial(4|0x80000000) graphic(2|0x8000) graphicInc(1)
            //   count(2) x(2) y(2) z(1)
            byte[] packet =
            {
                0x1A,
                0x00, 0x18,
                0x80, 0x00, 0x00, 0x55,      // serial high bit + 0x55
                0xC0, 0x01,                  // graphic high bit + 0x4001
                0x03,                        // graphicInc = 3
                0x00, 0x0A,                  // count = 10
                0x01, 0x00,                  // x = 256
                0x01, 0x10,                  // y = 272
                0xFE                         // z = -2
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x55u);
            captured.Value.Graphic.Should().Be((ushort)0x4001);
            captured.Value.GraphicInc.Should().Be((byte)3);
            captured.Value.Amount.Should().Be((ushort)10);
            captured.Value.X.Should().Be((ushort)256);
            captured.Value.Y.Should().Be((ushort)272);
            captured.Value.Z.Should().Be((sbyte)-2);
            captured.Value.Type.Should().Be((byte)2);
        }

        // ---- 0xF3 UpdateItemSA (fixed length 26, offset = 1) ----

        [Fact]
        public void UpdateItemSA_0xF3_RaisesItemUpdated_SAFlagSet()
        {
            ItemUpdatedArgs? captured = null;
            EventSink.ItemUpdated += e => captured = e;

            // Layout (after packet id):
            //   skip(2) type(1) serial(4) graphic(2) graphicInc(1)
            //   amount(2) unk(2) x(2) y(2) z(1) dir(1) hue(2) flags(1)
            //   unk2(2)
            byte[] packet =
            {
                0xF3,
                0x00, 0x00,                  // skipped
                0x02,                        // type = 2
                0x00, 0x00, 0x40, 0x10,      // serial = 0x4010
                0x12, 0x34,                  // graphic = 0x1234
                0x05,                        // graphicInc = 5
                0x00, 0x07,                  // amount = 7
                0x00, 0x00,                  // unk
                0x01, 0x20,                  // x = 288
                0x02, 0x30,                  // y = 560
                0xFA,                        // z = -6
                0x02,                        // direction = 2 (East)
                0x00, 0x21,                  // hue = 0x21
                0x04,                        // flags
                0x00, 0x01                   // unk2 = 1
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Type.Should().Be((byte)2);
            captured.Value.Serial.Should().Be(0x4010u);
            captured.Value.Graphic.Should().Be((ushort)0x1234);
            captured.Value.GraphicInc.Should().Be((byte)5);
            captured.Value.Amount.Should().Be((ushort)7);
            captured.Value.X.Should().Be((ushort)288);
            captured.Value.Y.Should().Be((ushort)560);
            captured.Value.Z.Should().Be((sbyte)-6);
            captured.Value.Direction.Should().Be(Direction.East);
            captured.Value.Hue.Should().Be((ushort)0x21);
            captured.Value.IsItemSA.Should().BeTrue();
            captured.Value.IsFromPacketList.Should().BeFalse();
        }

        // ---- 0x24 OpenContainer (fixed length 7, offset = 1) ----

        [Fact]
        public void OpenContainer_0x24_RaisesContainerOpened()
        {
            ContainerOpenedArgs? captured = null;
            EventSink.ContainerOpened += e => captured = e;

            byte[] packet =
            {
                0x24,
                0x00, 0x00, 0x00, 0x10,      // serial = 0x10
                0x00, 0x42                   // graphic = 0x42
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x10u);
            captured.Value.Graphic.Should().Be((ushort)0x42);
        }

        // ---- 0x2E EquipItem (fixed length 15, offset = 1) ----

        [Fact]
        public void EquipItem_0x2E_RaisesItemEquipped()
        {
            ItemEquippedArgs? captured = null;
            EventSink.ItemEquipped += e => captured = e;

            // Layout (after id):
            //   serial(4) graphic(2) graphicIncSigned(1) layer(1)
            //   container(4) hue(2)
            // eqGraphic = graphic + graphicIncSigned.
            byte[] packet =
            {
                0x2E,
                0x00, 0x00, 0x00, 0x80,      // serial = 0x80
                0x40, 0x10,                  // graphic = 0x4010
                0x02,                        // graphicInc (signed) = +2 -> 0x4012
                0x0D,                        // layer = Torso
                0x00, 0x00, 0x00, 0x90,      // container = 0x90
                0x00, 0x21                   // hue = 0x21
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x80u);
            captured.Value.Graphic.Should().Be((ushort)0x4012);
            captured.Value.Layer.Should().Be(Layer.Torso);
            captured.Value.ContainerSerial.Should().Be(0x90u);
            captured.Value.Hue.Should().Be((ushort)0x21);
        }

        // ---- 0x89 CorpseEquipment (variable length, offset = 3) ----

        [Fact]
        public void CorpseEquipment_0x89_RaisesCorpseEquipmentReceived()
        {
            // Handler needs an existing Item with Graphic == 0x2006
            // (corpse). Seed one through the test seam, then dispatch.
            const uint corpseSerial = 0x40000010u; // item serial range
            Item corpse = _world.GetOrCreateItem(corpseSerial);
            corpse.Graphic = 0x2006;

            CorpseEquipmentReceivedArgs? captured = null;
            EventSink.CorpseEquipmentReceived += e => captured = e;

            // Two equipment entries, then Layer.Invalid terminator.
            byte[] packet =
            {
                0x89,
                0x00, 0x14,                          // length placeholder
                0x40, 0x00, 0x00, 0x10,              // corpse serial
                0x05,                                // layer = Shirt
                0x40, 0x00, 0x00, 0xA1,              //   item serial
                0x0D,                                // layer = Torso
                0x40, 0x00, 0x00, 0xA2,              //   item serial
                0x00                                 // Layer.Invalid (end)
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.CorpseSerial.Should().Be(corpseSerial);
            captured.Value.Entries.Should().HaveCount(2);
            captured.Value.Entries[0].Layer.Should().Be(Layer.Shirt);
            captured.Value.Entries[0].ItemSerial.Should().Be(0x400000A1u);
            captured.Value.Entries[1].Layer.Should().Be(Layer.Torso);
            captured.Value.Entries[1].ItemSerial.Should().Be(0x400000A2u);
        }

        // ---- 0xD6 MegaCliloc (variable length, offset = 3) ----

        [Fact]
        public void MegaCliloc_0xD6_RaisesMegaClilocReceived_EmptyList()
        {
            // The translation loop calls Client.Game.UO.FileManager.Clilocs
            // for each non-zero cliloc id, which would NRE in tests. Sending
            // cliloc=0 immediately exits the loop with an empty list, so the
            // handler still emits MegaClilocReceived with default strings.
            MegaClilocReceivedArgs? captured = null;
            EventSink.MegaClilocReceived += e => captured = e;

            byte[] packet =
            {
                0xD6,
                0x00, 0x13,                          // length placeholder
                0x00, 0x01,                          // unknown = 1 (allowed)
                0x00, 0x00, 0x40, 0x20,              // serial
                0x00, 0x00,                          // skipped (2)
                0x00, 0x00, 0x00, 0x2A,              // revision = 0x2A
                0x00, 0x00, 0x00, 0x00               // cliloc = 0 terminator
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x4020u);
            captured.Value.Revision.Should().Be(0x2Au);
            captured.Value.Name.Should().BeEmpty();
            captured.Value.Properties.Should().BeEmpty();
            captured.Value.NameCliloc.Should().Be(0);
        }

        // ---- 0x28 EndDraggingItem (fixed length 5, offset = 1) ----

        [Fact]
        public void EndDraggingItem_0x28_RaisesItemDragEnded()
        {
            bool fired = false;
            EventSink.ItemDragEnded += _ => fired = true;

            byte[] packet = new byte[5];
            packet[0] = 0x28;
            Dispatch(packet, 1);

            fired.Should().BeTrue();
        }

        // ---- 0x29 DropItemAccepted (fixed length 1, offset = 1) ----

        [Fact]
        public void DropItemAccepted_0x29_RaisesItemDropAccepted()
        {
            bool fired = false;
            EventSink.ItemDropAccepted += _ => fired = true;

            Dispatch(new byte[] { 0x29 }, 1);

            fired.Should().BeTrue();
        }

        // ---- 0x74 BuyList (variable length, offset = 3) ----

        [Fact]
        public void BuyList_0x74_RaisesShopBuyListReceived_EmptyEntries()
        {
            // BuyList needs:
            //   - an Item with serial == container serial
            //   - that item's Container field points at an existing Mobile
            // The shop-entry parsing branch only runs when the container's
            // Layer is ShopBuyRestock or ShopBuy. Leaving Layer at its
            // default (Invalid) skips the parse branch, so the handler
            // emits the event with an empty entry list.
            const uint vendorSerial = 0x00000200u;  // mobile serial range
            const uint containerSerial = 0x40000201u; // item serial range
            Mobile vendor = _world.GetOrCreateMobile(vendorSerial);
            Item container = _world.GetOrCreateItem(containerSerial);
            container.Container = vendorSerial;

            ShopBuyListReceivedArgs? captured = null;
            EventSink.ShopBuyListReceived += e => captured = e;

            // After id and length, the handler reads the container serial,
            // then (only when the layer matches) a count byte + entries.
            byte[] packet =
            {
                0x74,
                0x00, 0x07,                          // length placeholder
                0x40, 0x00, 0x02, 0x01               // container serial
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.VendorSerial.Should().Be(vendorSerial);
            captured.Value.Entries.Should().NotBeNull();
            captured.Value.Entries.Count.Should().Be(0);
        }

        // ---- 0x9E SellList (variable length, offset = 3) ----

        [Fact]
        public void SellList_0x9E_RaisesShopSellListReceived_WithEntry()
        {
            // SellList needs an existing Mobile with the vendor serial,
            // then reads countItems(2) and exactly that many entries:
            //   serial(4) graphic(2) hue(2) amount(2) price(2)
            //   nameLen(2) name(nameLen)
            const uint vendorSerial = 0x00000300u;
            _world.GetOrCreateMobile(vendorSerial);

            ShopSellListReceivedArgs? captured = null;
            EventSink.ShopSellListReceived += e => captured = e;

            // Build a packet with one entry whose name is "Sword" (5 ASCII).
            byte[] packet =
            {
                0x9E,
                0x00, 0x1A,                          // length placeholder
                0x00, 0x00, 0x03, 0x00,              // vendor serial
                0x00, 0x01,                          // countItems = 1
                0x40, 0x00, 0x03, 0x10,              //   item serial
                0x10, 0x00,                          //   graphic = 0x1000
                0x00, 0x21,                          //   hue = 0x21
                0x00, 0x05,                          //   amount = 5
                0x00, 0x64,                          //   price = 100
                0x00, 0x05,                          //   nameLen = 5
                (byte)'S', (byte)'w', (byte)'o', (byte)'r', (byte)'d'
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.VendorSerial.Should().Be(vendorSerial);
            captured.Value.Entries.Should().HaveCount(1);
            captured.Value.Entries[0].Serial.Should().Be(0x40000310u);
            captured.Value.Entries[0].Graphic.Should().Be((ushort)0x1000);
            captured.Value.Entries[0].Hue.Should().Be((ushort)0x21);
            captured.Value.Entries[0].Amount.Should().Be((ushort)5);
            captured.Value.Entries[0].Price.Should().Be((ushort)100);
            captured.Value.Entries[0].Name.Should().Be("Sword");
        }

        // ---- 0x6F SecureTrading (variable length, offset = 3) ----

        [Fact]
        public void SecureTrading_0x6F_Type0_RaisesTradeWindowOpened()
        {
            // type=0 (open) reads serial, id1, id2 — both ids must
            // resolve through world.Get(). Seed two mobiles, then send a
            // packet with hasName=false (handler accepts an empty name).
            const uint id1 = 0x00000401u;
            const uint id2 = 0x00000402u;
            _world.GetOrCreateMobile(id1);
            _world.GetOrCreateMobile(id2);

            TradeWindowOpenArgs? captured = null;
            EventSink.TradeWindowOpened += e => captured = e;

            byte[] packet =
            {
                0x6F,
                0x00, 0x11,                          // length placeholder
                0x00,                                // type = 0 (open)
                0x00, 0x00, 0x00, 0x42,              // window serial
                0x00, 0x00, 0x04, 0x01,              // id1
                0x00, 0x00, 0x04, 0x02,              // id2
                0x00                                 // hasName = false
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x42u);
            captured.Value.Id1.Should().Be(id1);
            captured.Value.Id2.Should().Be(id2);
            captured.Value.Name.Should().BeEmpty();
        }

        [Fact]
        public void SecureTrading_0x6F_Type1_RaisesTradeWindowClosed()
        {
            TradeWindowClosedArgs? captured = null;
            EventSink.TradeWindowClosed += e => captured = e;

            byte[] packet =
            {
                0x6F,
                0x00, 0x08,                          // length placeholder
                0x01,                                // type = 1 (close)
                0x00, 0x00, 0x00, 0x42               // window serial
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x42u);
        }

        [Fact]
        public void SecureTrading_0x6F_Type2_RaisesTradeWindowAcceptUpdated()
        {
            TradeWindowAcceptUpdatedArgs? captured = null;
            EventSink.TradeWindowAcceptUpdated += e => captured = e;

            // type=2: id1 -> ImAccepting (non-zero), id2 -> HeIsAccepting.
            byte[] packet =
            {
                0x6F,
                0x00, 0x10,
                0x02,                                // type = 2
                0x00, 0x00, 0x00, 0x42,              // window serial
                0x00, 0x00, 0x00, 0x01,              // id1 = 1 -> true
                0x00, 0x00, 0x00, 0x00               // id2 = 0 -> false
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x42u);
            captured.Value.ImAccepting.Should().BeTrue();
            captured.Value.HeIsAccepting.Should().BeFalse();
        }

        [Fact]
        public void SecureTrading_0x6F_Type4_RaisesTradeWindowCurrencyUpdated_IsMine()
        {
            TradeWindowCurrencyUpdatedArgs? captured = null;
            EventSink.TradeWindowCurrencyUpdated += e => captured = e;

            // type=4 -> isMine = true. type=3 also routes here but with
            // isMine = false; we cover the IsMine = true branch.
            byte[] packet =
            {
                0x6F,
                0x00, 0x10,
                0x04,                                // type = 4
                0x00, 0x00, 0x00, 0x42,              // window serial
                0x00, 0x00, 0x00, 0x64,              // v1 (gold) = 100
                0x00, 0x00, 0x00, 0x32               // v2 (platinum) = 50
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x42u);
            captured.Value.IsMine.Should().BeTrue();
            captured.Value.Gold.Should().Be(100u);
            captured.Value.Platinum.Should().Be(50u);
        }
    }
}
