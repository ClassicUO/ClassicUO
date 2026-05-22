// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Network;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Events
{
    // Golden-byte parser tests for atmosphere, login, houses, and the 0xBF
    // sub-command space. Each test builds raw bytes, dispatches through
    // PacketHandlers.AnalyzePacket, and asserts the emitted EventSink args.
    //
    // Fixed-length packets use offset=1 (just the packet id). Variable-length
    // packets use offset=3 (packet id + 2-byte length). For 0xBF, after the
    // offset the body begins with a 2-byte sub-command id.
    //
    // Handlers that require world.InGame == true (Player AND Map non-null) or
    // that dereference Client.Game.UO are not testable from a unit-test
    // harness without reflection/static seam-changes and are skipped with a
    // one-line comment below.
    public class AtmosphereLoginExtendedPacketTests : IDisposable
    {
        private readonly World _world;

        public AtmosphereLoginExtendedPacketTests()
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

        // ----------------------------------------------------------------- Atmosphere

        // 0xBC Season — handler gates on world.Player != null. Skipped.
        // 0x4E PersonalLightLevel — gates on world.InGame. Skipped.
        // 0x4F LightLevel — gates on world.InGame. Skipped.
        // 0x70/0xC0/0xC7 GraphicEffect — gates on world.Player != null. Skipped.
        // 0xF6 BoatMoving — handler has no gate, but production callsite is
        //   always after login. Tested below.
        // 0x99 MultiPlacement — gates on world.Player != null. Skipped.
        // 0x38 Pathfinding — gates on world.InGame. Skipped.
        // 0x65 SetWeather — dereferences Client.Game.GetScene<GameScene>(). Skipped.
        // 0x54 PlaySoundEffect — gates on world.Player != null. Skipped.

        [Fact]
        public void ClientViewRange_0xC8_RaisesClientViewRangeChanged()
        {
            ClientViewRangeChangedArgs? captured = null;
            EventSink.ClientViewRangeChanged += e => captured = e;

            Dispatch(new byte[] { 0xC8, 0x12 }, 1);

            captured.Should().NotBeNull();
            captured.Value.Range.Should().Be(0x12);
        }

        [Fact]
        public void BoatMoving_0xF6_NoPassengers_RaisesBoatMovingReceived()
        {
            BoatMovingReceivedArgs? captured = null;
            EventSink.BoatMovingReceived += e => captured = e;

            // F6 | len(2) | serial(4) | speed(1) | mDir(1) | fDir(1) | x(2) | y(2) | z(2) | count(2)
            byte[] packet =
            {
                0xF6,
                0x00, 0x12,                          // length placeholder
                0x00, 0x00, 0x40, 0x01,              // serial
                0x02,                                // speed
                0x01,                                // movingDirection (East raw)
                0x05,                                // facingDirection (Left raw)
                0x04, 0xD2,                          // x = 1234
                0x16, 0x2E,                          // y = 5678
                0x00, 0x0A,                          // z = 10
                0x00, 0x00                           // count = 0
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x00004001u);
            captured.Value.Speed.Should().Be(0x02);
            captured.Value.MovingDirection.Should().Be((byte)(0x01 & 0x07));
            captured.Value.FacingDirection.Should().Be((byte)(0x05 & 0x07));
            captured.Value.X.Should().Be(1234);
            captured.Value.Y.Should().Be(5678);
            captured.Value.Z.Should().Be(10);
            captured.Value.Passengers.Count.Should().Be(0);
        }

        [Fact]
        public void BoatMoving_0xF6_WithPassengers_ParsesEachPassenger()
        {
            BoatMovingReceivedArgs? captured = null;
            EventSink.BoatMovingReceived += e => captured = e;

            byte[] packet =
            {
                0xF6,
                0x00, 0x24,
                0x00, 0x00, 0x40, 0x01,              // serial
                0x00,                                // speed
                0x00,                                // movingDirection
                0x00,                                // facingDirection
                0x00, 0x10,                          // x
                0x00, 0x20,                          // y
                0x00, 0x05,                          // z
                0x00, 0x02,                          // count = 2
                0x00, 0x00, 0xA0, 0x01,              // passenger 0 serial
                0x00, 0x11, 0x00, 0x21, 0x00, 0x06,
                0x00, 0x00, 0xA0, 0x02,              // passenger 1 serial
                0x00, 0x12, 0x00, 0x22, 0x00, 0x07
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.Passengers.Count.Should().Be(2);
            captured.Value.Passengers[0].Serial.Should().Be(0x0000A001u);
            captured.Value.Passengers[0].X.Should().Be(0x11);
            captured.Value.Passengers[0].Y.Should().Be(0x21);
            captured.Value.Passengers[0].Z.Should().Be(0x06);
            captured.Value.Passengers[1].Serial.Should().Be(0x0000A002u);
            captured.Value.Passengers[1].X.Should().Be(0x12);
            captured.Value.Passengers[1].Y.Should().Be(0x22);
            captured.Value.Passengers[1].Z.Should().Be(0x07);
        }

        // ----------------------------------------------------------------- Houses

        [Fact]
        public void CustomHouse_0xD8_NoFoundation_RaisesEmptyComponents()
        {
            // World has no items registered, so Items.Get(serial) returns null
            // and the handler takes the "no foundation" branch — empty list.
            CustomHouseReceivedArgs? captured = null;
            EventSink.CustomHouseReceived += e => captured = e;

            byte[] packet =
            {
                0xD8,
                0x00, 0x12,                          // length placeholder
                0x03,                                // compressed marker
                0x01,                                // enableResponse
                0x00, 0x00, 0x50, 0x07,              // serial
                0xDE, 0xAD, 0xBE, 0xEF,              // revision
                0x00, 0x00, 0x00, 0x00,              // 4 skipped bytes
                0x00                                 // planes = 0
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x00005007u);
            captured.Value.Revision.Should().Be(0xDEADBEEFu);
            captured.Value.Components.Count.Should().Be(0);
        }

        // ----------------------------------------------------------------- Login

        [Fact]
        public void LoginComplete_0x55_RaisesLoginCompleted()
        {
            bool fired = false;
            EventSink.LoginCompleted += _ => fired = true;

            Dispatch(new byte[] { 0x55 }, 1);

            fired.Should().BeTrue();
        }

        [Fact]
        public void ServerRelay_0x8C_RaisesServerRelayReceived()
        {
            // 8C is fixed-length; offset=1.
            // Layout: 8C | ip(4 LE) | port(2 BE) | seed(4 BE)
            ServerRelayReceivedArgs? captured = null;
            EventSink.ServerRelayReceived += e => captured = e;

            byte[] packet =
            {
                0x8C,
                0x01, 0x02, 0x03, 0x04,              // ip LE -> 0x04030201
                0x09, 0x3E,                          // port = 2366
                0xDE, 0xAD, 0xBE, 0xEF               // seed
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Ip.Should().Be(0x04030201u);
            captured.Value.Port.Should().Be(0x093E);
            captured.Value.Seed.Should().Be(0xDEADBEEFu);
        }

        [Fact]
        public void UpdateCharacterList_0x86_RaisesCharacterListUpdated()
        {
            // Variable-length. Layout after offset=3: count(1), then count * 60 bytes
            // (30 name + 30 password). Names are ASCII trimmed at null.
            CharacterListUpdatedArgs? captured = null;
            EventSink.CharacterListUpdated += e => captured = e;

            byte[] packet = new byte[3 + 1 + 60];
            packet[0] = 0x86;
            packet[1] = 0x00; packet[2] = 0x40;  // length placeholder
            packet[3] = 0x01;                     // count = 1
            // Name "Hero" at offset 4-33, then 30 password bytes (zeros).
            packet[4] = (byte)'H';
            packet[5] = (byte)'e';
            packet[6] = (byte)'r';
            packet[7] = (byte)'o';
            // remaining name bytes already 0

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.Characters.Count.Should().Be(1);
            captured.Value.Characters[0].Should().Be("Hero");
        }

        // 0xA9 ReceiveCharacterList — ParseCities reads Client.Game.UO.Version.
        // Without a GameController instance this NREs. Skipped.

        // 0xB9 EnableLockedFeatures — reads Client.Game.UO.Version and calls
        // Client.Game.UO.Animations.UpdateAnimationTable. Skipped.

        // 0xD1 Logout — gated on Client.Game.GetScene<GameScene>().DisconnectionRequested,
        // which NREs because Client.Game is null in unit tests. Skipped.

        [Fact]
        public void EnterWorld_0x1B_RaisesPlayerEnteredWorld_WithParsedFields()
        {
            // Fixed-length. Layout after offset=1:
            //   serial(4) | skip(4) | graphic(2) | x(2) | y(2) | z(2 BE, cast to sbyte)
            //   | direction(1) & 0x7
            PlayerEnteredWorldArgs? captured = null;
            EventSink.PlayerEnteredWorld += e => captured = e;

            byte[] packet =
            {
                0x1B,
                0x00, 0x00, 0x00, 0x05,              // serial
                0x00, 0x00, 0x00, 0x00,              // skipped 4 bytes
                0x01, 0x90,                          // graphic = 0x0190
                0x04, 0xD2,                          // x = 1234
                0x16, 0x2E,                          // y = 5678
                0xFF, 0xF6,                          // z = (sbyte)(ushort)0xFFF6 = -10
                0x83                                 // direction = 0x83 & 0x7 = 3
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(5u);
            captured.Value.Graphic.Should().Be(0x0190);
            captured.Value.X.Should().Be(1234);
            captured.Value.Y.Should().Be(5678);
            captured.Value.Z.Should().Be((sbyte)-10);
            captured.Value.Direction.Should().Be((Direction)3);
        }

        // ----------------------------------------------------------------- 0xBF sub-commands

        [Fact]
        public void FastWalkStackInit_0xBF01_RaisesFastWalkStackInit_With6Values()
        {
            FastWalkStackInitArgs? captured = null;
            EventSink.FastWalkStackInit += e => captured = e;

            // BF | len(2) | cmd(2)=0001 | 6 * uint
            byte[] packet =
            {
                0xBF, 0x00, 0x1F,
                0x00, 0x01,
                0x00, 0x00, 0x00, 0x01,
                0x00, 0x00, 0x00, 0x02,
                0x00, 0x00, 0x00, 0x03,
                0x00, 0x00, 0x00, 0x04,
                0x00, 0x00, 0x00, 0x05,
                0x00, 0x00, 0x00, 0x06
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.Values.Count.Should().Be(6);
            captured.Value.Values.Should().Equal(new uint[] { 1, 2, 3, 4, 5, 6 });
        }

        [Fact]
        public void FastWalkStackAdd_0xBF02_RaisesFastWalkStackAdd_WithValue()
        {
            FastWalkStackAddArgs? captured = null;
            EventSink.FastWalkStackAdd += e => captured = e;

            byte[] packet =
            {
                0xBF, 0x00, 0x09,
                0x00, 0x02,
                0xCA, 0xFE, 0xBA, 0xBE
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.Value.Should().Be(0xCAFEBABEu);
        }

        [Fact]
        public void GenericGumpClose_0xBF04_RaisesGenericGumpClose()
        {
            GenericGumpCloseArgs? captured = null;
            EventSink.GenericGumpClose += e => captured = e;

            byte[] packet =
            {
                0xBF, 0x00, 0x0D,
                0x00, 0x04,
                0x00, 0x00, 0x00, 0x42,              // serial
                0x00, 0x00, 0x00, 0x07               // button = 7
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x42u);
            captured.Value.Button.Should().Be(7);
        }

        [Fact]
        public void MapIndexChanged_0xBF08_RaisesMapIndexChanged()
        {
            MapIndexChangedArgs? captured = null;
            EventSink.MapIndexChanged += e => captured = e;

            byte[] packet =
            {
                0xBF, 0x00, 0x06,
                0x00, 0x08,
                0x03                                 // map index
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.MapIndex.Should().Be(0x03);
        }

        [Fact]
        public void CloseStatusbarGump_0xBF0C_RaisesCloseStatusbarGump()
        {
            CloseStatusbarGumpArgs? captured = null;
            EventSink.CloseStatusbarGump += e => captured = e;

            byte[] packet =
            {
                0xBF, 0x00, 0x09,
                0x00, 0x0C,
                0x00, 0x00, 0x00, 0x99
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x99u);
        }

        [Fact]
        public void EquipInfo_0xBF10_TagLine_RaisesEquipInfoReceived()
        {
            // Use a single tag-style line (charges = -1, i.e. 0xFFFF). The handler
            // loop runs while position < length - 4. After itemSerial, nameCliloc,
            // and the first cliloc, we need exactly 4 trailing bytes to stop.
            // Layout: itemSerial(4) | nameCliloc(4) | lineCliloc(4) | charges(2) | tail(4)
            EquipInfoArgs? captured = null;
            EventSink.EquipInfoReceived += e => captured = e;

            byte[] packet =
            {
                0xBF, 0x00, 0x16,
                0x00, 0x10,
                0x00, 0x00, 0x10, 0x00,              // itemSerial = 0x00001000
                0x00, 0x10, 0x00, 0x01,              // nameCliloc = 0x00100001
                0x00, 0x10, 0x00, 0x02,              // lineCliloc = 0x00100002 (becomes 'next')
                0xFF, 0xFF,                          // charges = -1 (tag)
                0xFF, 0xFF, 0xFF, 0xFF               // terminator (stops loop)
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.ItemSerial.Should().Be(0x00001000u);
            captured.Value.Cliloc.Should().Be(0x00100001u);
            captured.Value.CrafterName.Should().Be(string.Empty);
            captured.Value.Unidentified.Should().BeFalse();
            captured.Value.Lines.Count.Should().Be(1);
            captured.Value.Lines[0].Cliloc.Should().Be(0x00100002);
            captured.Value.Lines[0].Charges.Should().Be((short)-1);
        }

        [Fact]
        public void PopupMenu_0xBF14_NewCliloc_RaisesPopupMenuReceived()
        {
            PopupMenuArgs? captured = null;
            EventSink.PopupMenuReceived += e => captured = e;

            // PopupMenuData.Parse layout (mode>=2 == isNewCliloc):
            //   mode(2) | serial(4) | count(1) | entries[count] {cliloc(4) index(2) flags(2)}
            byte[] packet =
            {
                0xBF, 0x00, 0x12,
                0x00, 0x14,
                0x00, 0x02,                          // mode = 2 (isNewCliloc)
                0x00, 0x00, 0x40, 0x00,              // serial
                0x01,                                // count = 1
                0x00, 0x0F, 0x42, 0x40,              // cliloc
                0x00, 0x0A,                          // index
                0x00, 0x00                           // flags
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.Data.Serial.Should().Be(0x00004000u);
            captured.Value.Data.Items.Length.Should().Be(1);
            captured.Value.Data.Items[0].Cliloc.Should().Be(0x000F4240);
            captured.Value.Data.Items[0].Index.Should().Be(0x000A);
        }

        [Fact]
        public void CloseUserInterface_0xBF16_RaisesCloseUserInterface()
        {
            CloseUserInterfaceArgs? captured = null;
            EventSink.CloseUserInterface += e => captured = e;

            byte[] packet =
            {
                0xBF, 0x00, 0x0D,
                0x00, 0x16,
                0x00, 0x00, 0x00, 0x07,              // gumpKindId
                0x00, 0x00, 0x40, 0x01               // serial
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.GumpKindId.Should().Be(7u);
            captured.Value.Serial.Should().Be(0x00004001u);
        }

        [Fact]
        public void ExtendedStats_0xBF19_Version0_RaisesExtendedStatsBonded()
        {
            ExtendedStatsBondedArgs? captured = null;
            EventSink.ExtendedStatsBonded += e => captured = e;

            byte[] packet =
            {
                0xBF, 0x00, 0x0B,
                0x00, 0x19,
                0x00,                                // version 0 = bonded
                0x00, 0x00, 0x10, 0x05,              // serial
                0x01                                 // dead = true
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x00001005u);
            captured.Value.IsDead.Should().BeTrue();
        }

        [Fact]
        public void ExtendedStats_0xBF19_Version2_RaisesExtendedStatsLocks()
        {
            ExtendedStatsLocksArgs? captured = null;
            EventSink.ExtendedStatsLocks += e => captured = e;

            // state byte 0b_01_10_11 -> strLock=01, dexLock=10, intLock=11
            byte state = 0b01_10_11; // = 0x1B
            byte[] packet =
            {
                0xBF, 0x00, 0x0C,
                0x00, 0x19,
                0x02,                                // version 2 = locks
                0x00, 0x00, 0x10, 0x05,              // serial
                0x00,                                // updategump byte
                state
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x00001005u);
            captured.Value.StrLock.Should().Be((byte)((state >> 4) & 3));
            captured.Value.DexLock.Should().Be((byte)((state >> 2) & 3));
            captured.Value.IntLock.Should().Be((byte)(state & 3));
        }

        [Fact]
        public void ExtendedStats_0xBF19_Version5_Type0xFF_RaisesExtendedStatsAnimation()
        {
            ExtendedStatsAnimationArgs? captured = null;
            EventSink.ExtendedStatsAnimation += e => captured = e;

            byte[] packet =
            {
                0xBF, 0x00, 0x10,
                0x00, 0x19,
                0x05,                                // version 5
                0x00, 0x00, 0x10, 0x05,              // serial
                0x00,                                // zero
                0xFF,                                // type2 = 0xFF
                0x01,                                // status = 1 (non-zero so not sentinel)
                0x00, 0x42,                          // animation
                0x00, 0x07                           // frame
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x00001005u);
            captured.Value.Animation.Should().Be(0x0042);
            captured.Value.Frame.Should().Be(0x0007);
        }

        [Fact]
        public void SpellbookContent_0xBF1B_RaisesSpellbookContent()
        {
            SpellbookContentArgs? captured = null;
            EventSink.SpellbookContent += e => captured = e;

            // Layout after sub-cmd: skip(2) | serial(4) | graphic(2) | bookType(2)
            // | 2 sets of 4 little-endian bytes = 64 bit masks of spell ids.
            // Set bit 0 in the first set -> spellId = 1.
            byte[] packet =
            {
                0xBF, 0x00, 0x17,
                0x00, 0x1B,
                0x00, 0x00,                          // skip
                0x00, 0x00, 0x70, 0x01,              // serial
                0x00, 0x2D,                          // graphic
                0x00, 0x01,                          // book type
                0x01, 0x00, 0x00, 0x00,              // first 32-bit mask LE -> bit0 set
                0x00, 0x00, 0x00, 0x00               // second 32-bit mask LE
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x00007001u);
            captured.Value.SpellbookGraphic.Should().Be(0x002D);
            captured.Value.SpellbookType.Should().Be(0x0001);
            captured.Value.SpellIds.Count.Should().Be(1);
            captured.Value.SpellIds[0].Should().Be(1);
        }

        [Fact]
        public void HouseRevisionState_0xBF1D_RaisesHouseRevisionState()
        {
            HouseRevisionStateArgs? captured = null;
            EventSink.HouseRevisionState += e => captured = e;

            byte[] packet =
            {
                0xBF, 0x00, 0x0D,
                0x00, 0x1D,
                0x00, 0x00, 0x70, 0x07,              // serial
                0xDE, 0xAD, 0xBE, 0xEF               // revision
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x00007007u);
            captured.Value.Revision.Should().Be(0xDEADBEEFu);
        }

        [Fact]
        public void HouseDesignState_0xBF20_RaisesHouseDesignState()
        {
            HouseDesignStateArgs? captured = null;
            EventSink.HouseDesignState += e => captured = e;

            byte[] packet =
            {
                0xBF, 0x00, 0x11,
                0x00, 0x20,
                0x00, 0x00, 0x70, 0x07,              // serial
                0x04,                                // action
                0x00, 0xA5,                          // graphic
                0x04, 0xD2,                          // x = 1234
                0x16, 0x2E,                          // y = 5678
                0xF6                                 // z = -10 (sbyte)
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x00007007u);
            captured.Value.Action.Should().Be(0x04);
            captured.Value.Graphic.Should().Be(0x00A5);
            captured.Value.X.Should().Be(1234);
            captured.Value.Y.Should().Be(5678);
            captured.Value.Z.Should().Be((sbyte)-10);
        }

        [Fact]
        public void AbilityIconsReset_0xBF21_RaisesAbilityIconsReset()
        {
            bool fired = false;
            EventSink.AbilityIconsReset += _ => fired = true;

            byte[] packet =
            {
                0xBF, 0x00, 0x05,
                0x00, 0x21
            };

            Dispatch(packet, 3);

            fired.Should().BeTrue();
        }

        [Fact]
        public void DamageOverhead_0xBF22_RaisesDamageOverhead()
        {
            DamageOverheadArgs? captured = null;
            EventSink.DamageOverhead += e => captured = e;

            byte[] packet =
            {
                0xBF, 0x00, 0x0B,
                0x00, 0x22,
                0x00,                                // skipped byte
                0x00, 0x00, 0x40, 0x01,              // serial
                0x2A                                 // damage = 42
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x00004001u);
            captured.Value.Damage.Should().Be(42);
        }

        [Fact]
        public void SpellIconToggle_0xBF25_RaisesSpellIconToggle()
        {
            SpellIconToggleArgs? captured = null;
            EventSink.SpellIconToggle += e => captured = e;

            byte[] packet =
            {
                0xBF, 0x00, 0x08,
                0x00, 0x25,
                0x00, 0x1F,                          // spell id = 31
                0x01                                 // active = true
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.SpellId.Should().Be(31);
            captured.Value.Active.Should().BeTrue();
        }

        [Fact]
        public void CharacterSpeedMode_0xBF26_RaisesCharacterSpeedMode()
        {
            CharacterSpeedModeArgs? captured = null;
            EventSink.CharacterSpeedMode += e => captured = e;

            byte[] packet =
            {
                0xBF, 0x00, 0x06,
                0x00, 0x26,
                0x02                                 // speed mode
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.SpeedMode.Should().Be(0x02);
        }

        [Fact]
        public void RaceChangeRequested_0xBF2A_RaisesRaceChangeRequested()
        {
            RaceChangeRequestedArgs? captured = null;
            EventSink.RaceChangeRequested += e => captured = e;

            byte[] packet =
            {
                0xBF, 0x00, 0x07,
                0x00, 0x2A,
                0x01,                                // isFemale
                0x03                                 // race
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.IsFemale.Should().BeTrue();
            captured.Value.Race.Should().Be(0x03);
        }

        [Fact]
        public void MobileAnimationFrame_0xBF2B_RaisesMobileAnimationFrame()
        {
            MobileAnimationFrameArgs? captured = null;
            EventSink.MobileAnimationFrame += e => captured = e;

            byte[] packet =
            {
                0xBF, 0x00, 0x09,
                0x00, 0x2B,
                0x12, 0x34,                          // partial serial
                0x05,                                // animId
                0x10                                 // frameCount
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.PartialSerial.Should().Be(0x1234);
            captured.Value.AnimationId.Should().Be(0x05);
            captured.Value.FrameCount.Should().Be(0x10);
        }

        [Fact]
        public void CuoCommand_0xBFBEEF_RaisesCuoCommand()
        {
            CuoCommandArgs? captured = null;
            EventSink.CuoCommand += e => captured = e;

            byte[] packet =
            {
                0xBF, 0x00, 0x07,
                0xBE, 0xEF,                          // sub-cmd = 0xBEEF
                0x00, 0x42                           // type
            };

            Dispatch(packet, 3);

            captured.Should().NotBeNull();
            captured.Value.Type.Should().Be(0x0042);
        }
    }
}
