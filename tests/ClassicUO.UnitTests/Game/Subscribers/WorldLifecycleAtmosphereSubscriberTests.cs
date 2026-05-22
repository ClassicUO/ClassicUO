// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Subscribers
{
    // Behavioral tests for the World partial subscribers that handle world
    // lifecycle and atmosphere events: object deletion, season change, light
    // level, character animation, combat swing, graphic effect, pathfinding,
    // boat movement, map data, and mobile death.
    //
    // World ctor auto-wires every subscriber via SubscribeEvents(); these
    // tests keep those subscribers active and raise events on the static
    // EventSink to observe state mutations.
    //
    // Many handlers gate on `Player` or `InGame`. We cannot construct a real
    // PlayerMobile in tests because its ctor reads unmockable file-IO globals
    // (Client.Game.UO.FileManager.Skills, .Maps). For the `InGame` gate we use
    // the existing World.SetInGameForTests(bool) seam. The `Player` gate has
    // no such seam, so handlers that touch `Player.*` are exercised only on
    // their early-return path (null Player / null mobile / not InGame) — that
    // is still load-bearing behavior worth pinning down.
    [Collection("EventSinkSerial")]
    public class WorldLifecycleAtmosphereSubscriberTests : IDisposable
    {
        private readonly World _world;

        public WorldLifecycleAtmosphereSubscriberTests()
        {
            ProfileManager.CurrentProfile = new Profile();
            _world = new World();
            _world.SetInGameForTests(true);
        }

        public void Dispose()
        {
            EventSink.ClearAll();
        }

        // ---------------- OnLightLevelChanged (overall) ----------------

        [Fact]
        public void LightLevelChanged_Overall_Writes_RealOverall_And_Overall_When_NotUsingCustom()
        {
            ProfileManager.CurrentProfile.UseCustomLightLevel = false;

            EventSink.RaiseLightLevelChanged(new LightLevelChangedArgs(
                Serial: 0u,
                Level: 17,
                IsPersonal: false));

            _world.Light.RealOverall.Should().Be(17);
            _world.Light.Overall.Should().Be(17);
        }

        [Fact]
        public void LightLevelChanged_Overall_LightLevelType_Minimum_Clamps_To_Profile_LightLevel()
        {
            // LightLevelType == 1 means "minimum": Overall = Math.Min(e.Level, profile.LightLevel).
            ProfileManager.CurrentProfile.UseCustomLightLevel = true;
            ProfileManager.CurrentProfile.LightLevelType = 1;
            ProfileManager.CurrentProfile.LightLevel = 5;

            EventSink.RaiseLightLevelChanged(new LightLevelChangedArgs(
                Serial: 0u,
                Level: 20,
                IsPersonal: false));

            _world.Light.RealOverall.Should().Be(20);
            _world.Light.Overall.Should().Be(5); // clamped to profile.LightLevel
        }

        [Fact]
        public void LightLevelChanged_Overall_UseCustom_With_Absolute_LightLevelType_Does_Not_Touch_Overall()
        {
            // UseCustomLightLevel + LightLevelType != 1 → Overall is left alone.
            int initialOverall = _world.Light.Overall;
            ProfileManager.CurrentProfile.UseCustomLightLevel = true;
            ProfileManager.CurrentProfile.LightLevelType = 0; // absolute

            EventSink.RaiseLightLevelChanged(new LightLevelChangedArgs(
                Serial: 0u,
                Level: 25,
                IsPersonal: false));

            _world.Light.RealOverall.Should().Be(25); // always updated
            _world.Light.Overall.Should().Be(initialOverall); // unchanged
        }

        // ---------------- OnLightLevelChanged (personal) ----------------

        [Fact]
        public void LightLevelChanged_Personal_With_Null_Player_Is_NoOp()
        {
            // Player is null in tests → personal branch returns immediately.
            int beforePersonal = _world.Light.Personal;
            int beforeReal = _world.Light.RealPersonal;

            EventSink.RaiseLightLevelChanged(new LightLevelChangedArgs(
                Serial: 0xDEADBEEFu,
                Level: 7,
                IsPersonal: true));

            _world.Light.Personal.Should().Be(beforePersonal);
            _world.Light.RealPersonal.Should().Be(beforeReal);
        }

        // ---------------- OnObjectDeleted ----------------

        [Fact]
        public void ObjectDeleted_With_Null_Player_Is_NoOp_Item_Remains()
        {
            // Handler short-circuits on `Player == null`, so the entity stays in
            // the dictionary. This pins down the guard.
            uint serial = 0x4000_0200u;
            _world.GetOrCreateItem(serial);
            _world.Items.Get(serial).Should().NotBeNull();

            EventSink.RaiseObjectDeleted(new ObjectDeletedArgs(serial));

            _world.Items.Get(serial).Should().NotBeNull("Player==null short-circuits OnObjectDeleted");
        }

        // ---------------- OnSeasonChanged ----------------

        [Fact]
        public void SeasonChanged_With_Null_Player_Does_Not_Touch_OldSeason()
        {
            // Player==null guards the whole handler — including the OldSeason write.
            Season before = _world.OldSeason;
            int beforeMusic = _world.OldMusicIndex;

            EventSink.RaiseSeasonChanged(new SeasonChangedArgs(
                Season: 2, // Spring
                MusicCue: 11));

            _world.OldSeason.Should().Be(before);
            _world.OldMusicIndex.Should().Be(beforeMusic);
        }

        // ---------------- OnCharacterAnimation ----------------

        [Fact]
        public void CharacterAnimation_With_Missing_Mobile_Is_NoOp_And_Does_Not_Throw()
        {
            // No mobile in the dict → handler returns before touching SetAnimation.
            uint serial = 0x0000_0500u;
            _world.Mobiles.Get(serial).Should().BeNull();

            Action act = () => EventSink.RaiseCharacterAnimation(new CharacterAnimationArgs(
                Serial: serial,
                Action: 0,
                FrameCount: 5,
                RepeatCount: 1,
                Forward: true,
                Repeat: false,
                Delay: 0));

            act.Should().NotThrow();
            _world.Mobiles.Get(serial).Should().BeNull();
        }

        // ---------------- OnNewCharacterAnimation ----------------

        [Fact]
        public void NewCharacterAnimation_With_Missing_Mobile_Is_NoOp_And_Does_Not_Throw()
        {
            uint serial = 0x0000_0501u;
            _world.Mobiles.Get(serial).Should().BeNull();

            Action act = () => EventSink.RaiseNewCharacterAnimation(new NewCharacterAnimationArgs(
                Serial: serial,
                Type: 0,
                Action: 0,
                Mode: 0));

            act.Should().NotThrow();
            _world.Mobiles.Get(serial).Should().BeNull();
        }

        // ---------------- OnCombatSwing ----------------

        [Fact]
        public void CombatSwing_With_Null_Player_Does_Not_Throw()
        {
            // Handler guards on `Player == null`. Just prove it doesn't blow up.
            Action act = () => EventSink.RaiseCombatSwing(new CombatSwingArgs(
                Attacker: 0x0000_0001u,
                Defender: 0x0000_0002u));

            act.Should().NotThrow();
        }

        // ---------------- OnPathfindingReceived ----------------

        [Fact]
        public void PathfindingReceived_With_Null_Player_Does_Not_Throw()
        {
            // Handler reads Player.Pathfinder, guarded by `Player == null`.
            Action act = () => EventSink.RaisePathfindingReceived(new PathfindingReceivedArgs(
                X: 1234,
                Y: 5678,
                Z: 0));

            act.Should().NotThrow();
        }

        // ---------------- OnBoatMovingReceived ----------------

        [Fact]
        public void BoatMovingReceived_NonSmooth_With_Empty_Passengers_Moves_Multi_Item()
        {
            // Default profile.UseSmoothBoatMovement == false → handler calls
            // multi.SetInWorldTile(x,y,z). Empty passenger list avoids the
            // Player implicit-conversion that would otherwise throw.
            ProfileManager.CurrentProfile.UseSmoothBoatMovement = false;

            uint boatSerial = 0x4000_0700u;
            Item multi = _world.GetOrCreateItem(boatSerial);
            multi.X = 100;
            multi.Y = 100;
            multi.Z = 0;

            EventSink.RaiseBoatMovingReceived(new BoatMovingReceivedArgs(
                Serial: boatSerial,
                Speed: 0x02,
                MovingDirection: 0,
                FacingDirection: 0,
                X: 250,
                Y: 260,
                Z: 5,
                Passengers: Array.Empty<BoatPassenger>()));

            multi.X.Should().Be(250);
            multi.Y.Should().Be(260);
            multi.Z.Should().Be(5);
        }

        [Fact]
        public void BoatMovingReceived_With_Missing_Multi_Item_Is_NoOp()
        {
            uint boatSerial = 0x4000_0710u;
            _world.Items.Get(boatSerial).Should().BeNull();

            Action act = () => EventSink.RaiseBoatMovingReceived(new BoatMovingReceivedArgs(
                Serial: boatSerial,
                Speed: 0x02,
                MovingDirection: 0,
                FacingDirection: 0,
                X: 1,
                Y: 2,
                Z: 3,
                Passengers: Array.Empty<BoatPassenger>()));

            act.Should().NotThrow();
            _world.Items.Get(boatSerial).Should().BeNull();
        }

        [Fact]
        public void BoatMovingReceived_When_Not_InGame_Is_NoOp()
        {
            _world.SetInGameForTests(false);

            uint boatSerial = 0x4000_0720u;
            Item multi = _world.GetOrCreateItem(boatSerial);
            multi.X = 50;
            multi.Y = 60;
            multi.Z = 1;

            EventSink.RaiseBoatMovingReceived(new BoatMovingReceivedArgs(
                Serial: boatSerial,
                Speed: 0x02,
                MovingDirection: 0,
                FacingDirection: 0,
                X: 999,
                Y: 999,
                Z: 9,
                Passengers: Array.Empty<BoatPassenger>()));

            // InGame gate held → position not mutated.
            multi.X.Should().Be(50);
            multi.Y.Should().Be(60);
            multi.Z.Should().Be(1);
        }

        // ---------------- OnMobileDeath ----------------

        [Fact]
        public void MobileDeath_When_Not_InGame_Is_NoOp()
        {
            _world.SetInGameForTests(false);

            uint serial = 0x0000_0800u;
            Mobile m = _world.GetOrCreateMobile(serial);

            EventSink.RaiseMobileDeath(new MobileDeathArgs(
                Serial: serial,
                CorpseSerial: 0x4000_0801u,
                IsRunning: false));

            // Mobile is still keyed under its original serial; no re-key.
            _world.Mobiles.Get(serial).Should().BeSameAs(m);
            _world.Mobiles.Get(serial | 0x80000000).Should().BeNull();
        }

        [Fact]
        public void MobileDeath_With_Missing_Mobile_Is_NoOp_And_Does_Not_Throw()
        {
            uint serial = 0x0000_0810u;
            _world.Mobiles.Get(serial).Should().BeNull();

            Action act = () => EventSink.RaiseMobileDeath(new MobileDeathArgs(
                Serial: serial,
                CorpseSerial: 0x4000_0811u,
                IsRunning: false));

            act.Should().NotThrow();
            _world.Mobiles.Get(serial).Should().BeNull();
        }

        // ---------------- Skipped handlers ----------------
        //
        // OnGraphicEffectSpawned: forwards to World.SpawnEffect → private
        //   _effectManager (EffectManager). EffectManager has no public state
        //   accessor and constructing a GameEffect touches Client.Game.UO
        //   (NullRef in tests). Unobservable from subscriber-side.
        //
        // OnMapDataReceived: only mutates an existing MapGump found via
        //   UIManager.GetGump<MapGump>(e.Serial). UI-only — skipped.
        //
        // OnCharacterAnimation / OnNewCharacterAnimation positive paths:
        //   SetAnimation requires Mobile.GetReplacedObjectAnimation /
        //   GetObjectNewAnimation which both dereference Client.Game.UO
        //   (NullRef in tests). We only exercise the missing-mobile
        //   early-return path.
        //
        // OnSeasonChanged positive path: depends on world.Player and
        //   ChangeSeason() iterating Map.GetUsedChunks(). Both unmockable.
        //
        // OnObjectDeleted positive path: depends on world.Player throughout
        //   (Player.GetSecureTradeBox, RemoveMobile path, etc.). Skipped.
        //
        // OnCombatSwing / OnPathfindingReceived positive paths: read
        //   world.Player.* directly. Skipped (only early-return tested).
        //
        // OnMobileDeath positive path: re-keys the mobile but then reads
        //   Client.Game.UO.Animations and ProfileManager...AutoOpenCorpses
        //   on Player. NullRef without Client.Game. Only InGame-gate and
        //   missing-mobile early-returns tested.
    }
}
