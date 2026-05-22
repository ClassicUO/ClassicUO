// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Network;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Events
{
    // Positive parse-and-emit tests for handlers previously skipped in
    // AtmosphereLoginExtendedPacketTests because their world.InGame /
    // world.Player gate could not be satisfied without seeding World.
    //
    // World now exposes test-only seams:
    //   SetInGameForTests(bool)      — overrides InGame.
    //   SetPlayerForTests(player)    — seeds Player (PlayerMobile ctor is null-safe).
    //   SetMapForTests(map)          — seeds Map (Map ctor is null-safe).
    //
    // Handlers that still dereference Client.Game.* on unavoidable paths
    // (SetWeather, ReceiveCharacterList, EnableLockedFeatures, Logout) cannot
    // be exercised in unit tests without further production refactoring, and
    // remain skipped with a one-line comment.
    [Collection("EventSinkSerial")]
    public class AtmosphereLoginExtendedPacketInGameTests : IDisposable
    {
        private readonly World _world;

        public AtmosphereLoginExtendedPacketInGameTests()
        {
            _world = new World();
            _world.SetPlayerForTests(new PlayerMobile(_world, 0x00000001u));
            _world.SetMapForTests(new ClassicUO.Game.Map.Map(_world, 0));
            EventSink.ClearAll();
        }

        public void Dispose()
        {
            EventSink.ClearAll();
        }

        private void Dispatch(byte[] data, int offset)
            => PacketHandlers.Handler.AnalyzePacket(_world, data, offset);

        // ----------------------------------------------------------------- Atmosphere

        [Fact]
        public void Season_0xBC_RaisesSeasonChanged()
        {
            SeasonChangedArgs? captured = null;
            EventSink.SeasonChanged += e => captured = e;

            // BC | season(1) | music(1)
            byte[] packet = { 0xBC, 0x02, 0x07 };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Season.Should().Be(0x02);
            captured.Value.MusicCue.Should().Be(0x07);
        }

        [Fact]
        public void PersonalLightLevel_0x4E_RaisesLightLevelChanged_IsPersonalTrue()
        {
            LightLevelChangedArgs? captured = null;
            EventSink.LightLevelChanged += e => captured = e;

            // 4E | lightSerial(4) | level(1)
            byte[] packet =
            {
                0x4E,
                0x00, 0x00, 0x40, 0x01,              // lightSerial
                0x0A                                 // level = 10
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0x00004001u);
            captured.Value.Level.Should().Be(0x0A);
            captured.Value.IsPersonal.Should().BeTrue();
        }

        [Fact]
        public void PersonalLightLevel_0x4E_ClampsLevelAbove0x1E()
        {
            LightLevelChangedArgs? captured = null;
            EventSink.LightLevelChanged += e => captured = e;

            byte[] packet =
            {
                0x4E,
                0x00, 0x00, 0x40, 0x02,
                0xFF                                 // clamped to 0x1E
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Level.Should().Be(0x1E);
            captured.Value.IsPersonal.Should().BeTrue();
        }

        [Fact]
        public void LightLevel_0x4F_RaisesLightLevelChanged_IsPersonalFalse()
        {
            LightLevelChangedArgs? captured = null;
            EventSink.LightLevelChanged += e => captured = e;

            // 4F | level(1)
            byte[] packet = { 0x4F, 0x12 };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Serial.Should().Be(0u);
            captured.Value.Level.Should().Be(0x12);
            captured.Value.IsPersonal.Should().BeFalse();
        }

        // ----------------------------------------------------------------- Graphic effects

        [Fact]
        public void GraphicEffect_0xC0_RaisesGraphicEffectSpawned_WithHueAndBlendMode()
        {
            // 0xC0 layout: type(1) | source(4) | target(4) | graphic(2) |
            //   srcX(2) | srcY(2) | srcZ(1) | tgtX(2) | tgtY(2) | tgtZ(1) |
            //   speed(1) | duration(1) | unk(2) | fixedDir(1) | doesExplode(1) |
            //   hue(4) | blendMode(4)
            GraphicEffectSpawnedArgs? captured = null;
            EventSink.GraphicEffectSpawned += e => captured = e;

            byte[] packet =
            {
                0xC0,
                0x00,                                // type = Moving (<= FixedFrom)
                0x00, 0x00, 0x40, 0x01,              // source
                0x00, 0x00, 0x40, 0x02,              // target
                0x10, 0x20,                          // graphic
                0x00, 0x0A,                          // srcX
                0x00, 0x0B,                          // srcY
                0x05,                                // srcZ
                0x00, 0x0C,                          // tgtX
                0x00, 0x0D,                          // tgtY
                0xF6,                                // tgtZ = -10
                0x07,                                // speed
                0x09,                                // duration
                0x00, 0x00,                          // unk
                0x01,                                // fixedDirection = true
                0x00,                                // doesExplode = false
                0x00, 0x00, 0x00, 0x42,              // hue
                0x00, 0x00, 0x00, 0x03               // blendMode = 3 % 7 = 3
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.EffectType.Should().Be(0x00);
            captured.Value.Source.Should().Be(0x00004001u);
            captured.Value.Target.Should().Be(0x00004002u);
            captured.Value.Graphic.Should().Be(0x1020);
            captured.Value.SourceX.Should().Be(0x000A);
            captured.Value.SourceY.Should().Be(0x000B);
            captured.Value.SourceZ.Should().Be((sbyte)5);
            captured.Value.TargetX.Should().Be(0x000C);
            captured.Value.TargetY.Should().Be(0x000D);
            captured.Value.TargetZ.Should().Be((sbyte)-10);
            captured.Value.Speed.Should().Be(0x07);
            captured.Value.Duration.Should().Be(0x09);
            captured.Value.FixedDirection.Should().BeTrue();
            captured.Value.DoesExplode.Should().BeFalse();
            captured.Value.Hue.Should().Be(0x42u);
            captured.Value.BlendMode.Should().Be(0x03);
        }

        [Fact]
        public void GraphicEffect_0x70_RaisesGraphicEffectSpawned_WithoutHueOrBlend()
        {
            // 0x70 has the same prefix but no trailing hue/blendmode bytes.
            GraphicEffectSpawnedArgs? captured = null;
            EventSink.GraphicEffectSpawned += e => captured = e;

            byte[] packet =
            {
                0x70,
                0x01,                                // type = Lightning
                0x00, 0x00, 0x50, 0x01,              // source
                0x00, 0x00, 0x50, 0x02,              // target
                0x00, 0x55,                          // graphic
                0x00, 0x10,                          // srcX
                0x00, 0x20,                          // srcY
                0x03,                                // srcZ
                0x00, 0x11,                          // tgtX
                0x00, 0x21,                          // tgtY
                0x04,                                // tgtZ
                0x02,                                // speed
                0x05,                                // duration
                0x00, 0x00,                          // unk
                0x00,                                // fixedDirection
                0x01                                 // doesExplode
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.EffectType.Should().Be(0x01);
            captured.Value.Source.Should().Be(0x00005001u);
            captured.Value.Target.Should().Be(0x00005002u);
            captured.Value.Graphic.Should().Be(0x0055);
            captured.Value.Speed.Should().Be(0x02);
            captured.Value.Duration.Should().Be(0x05);
            captured.Value.FixedDirection.Should().BeFalse();
            captured.Value.DoesExplode.Should().BeTrue();
            captured.Value.Hue.Should().Be(0u);
            captured.Value.BlendMode.Should().Be(0);
        }

        [Fact]
        public void GraphicEffect_0xC7_RaisesGraphicEffectSpawned_WithExtraTrailingBytes()
        {
            // 0xC7 = same as 0xC0 plus 13 trailing bytes that get read & ignored.
            GraphicEffectSpawnedArgs? captured = null;
            EventSink.GraphicEffectSpawned += e => captured = e;

            byte[] packet =
            {
                0xC7,
                0x00,                                // type
                0x00, 0x00, 0x60, 0x01,              // source
                0x00, 0x00, 0x60, 0x02,              // target
                0x00, 0x77,                          // graphic
                0x00, 0x30,                          // srcX
                0x00, 0x40,                          // srcY
                0x02,                                // srcZ
                0x00, 0x31,                          // tgtX
                0x00, 0x41,                          // tgtY
                0x03,                                // tgtZ
                0x01,                                // speed
                0x02,                                // duration
                0x00, 0x00,                          // unk
                0x00,                                // fixedDirection
                0x00,                                // doesExplode
                0x00, 0x00, 0x00, 0x99,              // hue
                0x00, 0x00, 0x00, 0x05,              // blendMode = 5 % 7 = 5
                // 0xC7 trailing 13 bytes: tileID(2) explodeEffect(2) explodeSound(2) serial(4) layer(1) skip(2)
                0x00, 0x00,
                0x00, 0x00,
                0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
                0x00,
                0x00, 0x00
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Hue.Should().Be(0x99u);
            captured.Value.BlendMode.Should().Be(0x05);
        }

        // ----------------------------------------------------------------- Multi placement / Pathfinding

        [Fact]
        public void MultiPlacement_0x99_RaisesMultiPlacementReceived()
        {
            MultiPlacementReceivedArgs? captured = null;
            EventSink.MultiPlacementReceived += e => captured = e;

            // 99 layout (length 0x1A = 26):
            //   allowGround(1) | targID(4) | flags(1) | skip-up-to-pos18 |
            //   multiID(2) | xOff(2) | yOff(2) | zOff(2) | hue(2)
            // p.Seek(18) jumps absolute position (data index 18 from start of buffer).
            byte[] packet = new byte[26];
            packet[0] = 0x99;
            packet[1] = 0x01;                       // allowGround = true
            packet[2] = 0x00; packet[3] = 0x00; packet[4] = 0x70; packet[5] = 0x01; // targID
            packet[6] = 0x42;                       // flags
            // positions 7..17 are skipped via Seek(18). Then read from index 18:
            packet[18] = 0x10; packet[19] = 0x20;   // multiID = 0x1020
            packet[20] = 0x00; packet[21] = 0x05;   // xOff = 5
            packet[22] = 0x00; packet[23] = 0x06;   // yOff = 6
            packet[24] = 0x00; packet[25] = 0x07;   // zOff = 7 — last 2 are out-of-range for hue

            // Need 2 more bytes for hue read; build a longer buffer instead.
            byte[] big = new byte[28];
            Array.Copy(packet, big, 26);
            big[26] = 0x00;
            big[27] = 0x33;                         // hue = 0x33

            Dispatch(big, 1);

            captured.Should().NotBeNull();
            captured.Value.AllowGround.Should().Be((byte)1);
            captured.Value.TargetId.Should().Be(0x00007001u);
            captured.Value.Flags.Should().Be(0x42);
            captured.Value.MultiId.Should().Be(0x1020);
            captured.Value.OffsetX.Should().Be((ushort)5);
            captured.Value.OffsetY.Should().Be((ushort)6);
            captured.Value.OffsetZ.Should().Be((ushort)7);
            captured.Value.Hue.Should().Be(0x33);
        }

        [Fact]
        public void Pathfinding_0x38_RaisesPathfindingReceived()
        {
            PathfindingReceivedArgs? captured = null;
            EventSink.PathfindingReceived += e => captured = e;

            // 38 | x(2) | y(2) | z(2)
            byte[] packet =
            {
                0x38,
                0x04, 0xD2,                          // x = 1234
                0x16, 0x2E,                          // y = 5678
                0x00, 0x0A                           // z = 10
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.X.Should().Be(1234);
            captured.Value.Y.Should().Be(5678);
            captured.Value.Z.Should().Be(10);
        }

        // ----------------------------------------------------------------- Sound

        [Fact]
        public void PlaySoundEffect_0x54_RaisesSoundPlay()
        {
            SoundPlayArgs? captured = null;
            EventSink.SoundPlay += e => captured = e;

            // 54 | skip(1) | index(2) | audio(2) | x(2) | y(2) | z(2 -> short)
            byte[] packet =
            {
                0x54,
                0x00,                                // skipped
                0x00, 0x42,                          // index
                0x01, 0x23,                          // audio
                0x00, 0x10,                          // x
                0x00, 0x20,                          // y
                0xFF, 0xF6                           // z = -10 (short)
            };

            Dispatch(packet, 1);

            captured.Should().NotBeNull();
            captured.Value.Index.Should().Be(0x0042);
            captured.Value.Audio.Should().Be(0x0123);
            captured.Value.X.Should().Be(0x0010);
            captured.Value.Y.Should().Be(0x0020);
            captured.Value.Z.Should().Be((short)-10);
        }

        // ----------------------------------------------------------------- Skipped (still NRE without further refactor)

        // 0x65 SetWeather — top of handler dereferences Client.Game.GetScene<GameScene>();
        //   Client.Game is null in unit tests so this NREs before the null-check.
        // 0xA9 ReceiveCharacterList — ParseCities reads Client.Game.UO.Version. NREs.
        // 0xB9 EnableLockedFeatures — handler reads Client.Game.UO.Version and calls
        //   Client.Game.UO.Animations.UpdateAnimationTable. NREs.
        // 0xD1 Logout — handler dereferences Client.Game.GetScene<GameScene>() at the
        //   very top of the if. NREs.
    }
}
