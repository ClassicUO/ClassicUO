// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Subscribers
{
    // Behavioral tests for the main World.Subscribers partial — proves the
    // EventSink-wired handlers mutate Mobile / world state for non-player
    // serials. Player-only branches are skipped because PlayerMobile's ctor
    // dereferences unmockable globals.
    //
    // Fixture: build a fresh World (which auto-wires the subscribers via
    // SubscribeEvents) and seed Profile so any handler that touches
    // ProfileManager.CurrentProfile doesn't NRE. We do NOT call
    // EventSink.ClearAll() in the ctor — the World's auto-wired subscribers
    // must remain attached so they react to the events we raise.
    //
    // SetInGameForTests(true) satisfies any InGame guards.
    [Collection("EventSinkSerial")]
    public class WorldMobileStatsSubscriberTests : IDisposable
    {
        private readonly World _world;

        public WorldMobileStatsSubscriberTests()
        {
            ProfileManager.CurrentProfile = new Profile();
            _world = new World();
            _world.SetInGameForTests(true);
        }

        public void Dispose()
        {
            EventSink.ClearAll();
        }

        // ------------------------------------------------------------------
        // OnHitpointsUpdated — writes HitsMax/Hits on the target entity and
        // promotes Pending HitsRequest to Received.
        // ------------------------------------------------------------------

        [Fact]
        public void HitpointsUpdated_Sets_Mobile_Hits_And_HitsMax()
        {
            uint serial = 0x0000_1234u;
            Mobile mob = _world.GetOrCreateMobile(serial);

            EventSink.RaiseHitpointsUpdated(new HitpointsUpdatedArgs(serial, HitsMax: 120, Hits: 95));

            mob.HitsMax.Should().Be(120);
            mob.Hits.Should().Be(95);
        }

        [Fact]
        public void HitpointsUpdated_Promotes_Pending_HitsRequest_To_Received()
        {
            uint serial = 0x0000_1235u;
            Mobile mob = _world.GetOrCreateMobile(serial);
            mob.HitsRequest = HitsRequestStatus.Pending;

            EventSink.RaiseHitpointsUpdated(new HitpointsUpdatedArgs(serial, 50, 25));

            mob.HitsRequest.Should().Be(HitsRequestStatus.Received);
        }

        [Fact]
        public void HitpointsUpdated_Unknown_Serial_Is_NoOp()
        {
            // No mobile or item with this serial — handler should early-return
            // without throwing.
            uint serial = 0x0000_1236u;

            Action act = () => EventSink.RaiseHitpointsUpdated(new HitpointsUpdatedArgs(serial, 1, 1));

            act.Should().NotThrow();
            _world.Mobiles.Get(serial).Should().BeNull();
        }

        // ------------------------------------------------------------------
        // OnManaUpdated — writes ManaMax/Mana on a target Mobile.
        // ------------------------------------------------------------------

        [Fact]
        public void ManaUpdated_Sets_Mobile_Mana_And_ManaMax()
        {
            uint serial = 0x0000_1240u;
            Mobile mob = _world.GetOrCreateMobile(serial);

            EventSink.RaiseManaUpdated(new ManaUpdatedArgs(serial, ManaMax: 200, Mana: 175));

            mob.ManaMax.Should().Be(200);
            mob.Mana.Should().Be(175);
        }

        [Fact]
        public void ManaUpdated_Unknown_Mobile_Is_NoOp()
        {
            uint serial = 0x0000_1241u;

            Action act = () => EventSink.RaiseManaUpdated(new ManaUpdatedArgs(serial, 50, 50));

            act.Should().NotThrow();
            _world.Mobiles.Get(serial).Should().BeNull();
        }

        // ------------------------------------------------------------------
        // OnStaminaUpdated — writes StaminaMax/Stamina on a target Mobile.
        // ------------------------------------------------------------------

        [Fact]
        public void StaminaUpdated_Sets_Mobile_Stamina_And_StaminaMax()
        {
            uint serial = 0x0000_1250u;
            Mobile mob = _world.GetOrCreateMobile(serial);

            EventSink.RaiseStaminaUpdated(new StaminaUpdatedArgs(serial, StaminaMax: 100, Stamina: 42));

            mob.StaminaMax.Should().Be(100);
            mob.Stamina.Should().Be(42);
        }

        [Fact]
        public void StaminaUpdated_Unknown_Mobile_Is_NoOp()
        {
            uint serial = 0x0000_1251u;

            Action act = () => EventSink.RaiseStaminaUpdated(new StaminaUpdatedArgs(serial, 100, 100));

            act.Should().NotThrow();
            _world.Mobiles.Get(serial).Should().BeNull();
        }

        // ------------------------------------------------------------------
        // OnMobileAttributesUpdated — bundles Hits/Mana/Stamina updates onto
        // a Mobile entity in a single dispatch.
        // ------------------------------------------------------------------

        [Fact]
        public void MobileAttributesUpdated_Sets_All_Stats_On_Mobile()
        {
            uint serial = 0x0000_1260u;
            Mobile mob = _world.GetOrCreateMobile(serial);

            EventSink.RaiseMobileAttributesUpdated(new MobileAttributesUpdatedArgs(
                Serial: serial,
                HitsMax: 90,
                Hits: 80,
                ManaMax: 70,
                Mana: 60,
                StaminaMax: 50,
                Stamina: 40));

            mob.HitsMax.Should().Be(90);
            mob.Hits.Should().Be(80);
            mob.ManaMax.Should().Be(70);
            mob.Mana.Should().Be(60);
            mob.StaminaMax.Should().Be(50);
            mob.Stamina.Should().Be(40);
        }

        [Fact]
        public void MobileAttributesUpdated_Promotes_Pending_HitsRequest_To_Received()
        {
            uint serial = 0x0000_1261u;
            Mobile mob = _world.GetOrCreateMobile(serial);
            mob.HitsRequest = HitsRequestStatus.Pending;

            EventSink.RaiseMobileAttributesUpdated(new MobileAttributesUpdatedArgs(
                serial, 10, 5, 10, 5, 10, 5));

            mob.HitsRequest.Should().Be(HitsRequestStatus.Received);
        }

        // ------------------------------------------------------------------
        // OnHealthBarStateChanged — case 2 (YellowBar) is safe to test as it
        // only twiddles the Flags bit. Case 1 (poison) reads Client.Game.UO
        // which isn't initialized in unit tests.
        // ------------------------------------------------------------------

        [Fact]
        public void HealthBarStateChanged_YellowBar_Enabled_Sets_Flag()
        {
            uint serial = 0x0000_1270u;
            Mobile mob = _world.GetOrCreateMobile(serial);
            mob.Flags.Should().NotHaveFlag(Flags.YellowBar);

            EventSink.RaiseHealthBarStateChanged(new HealthBarStateChangedArgs(serial, StateType: 2, Enabled: true));

            mob.IsYellowHits.Should().BeTrue();
            mob.Flags.Should().HaveFlag(Flags.YellowBar);
        }

        [Fact]
        public void HealthBarStateChanged_YellowBar_Disabled_Clears_Flag()
        {
            uint serial = 0x0000_1271u;
            Mobile mob = _world.GetOrCreateMobile(serial);
            mob.Flags |= Flags.YellowBar;

            EventSink.RaiseHealthBarStateChanged(new HealthBarStateChangedArgs(serial, StateType: 2, Enabled: false));

            mob.IsYellowHits.Should().BeFalse();
            mob.Flags.Should().NotHaveFlag(Flags.YellowBar);
        }

        [Fact]
        public void HealthBarStateChanged_Unknown_Mobile_Is_NoOp()
        {
            uint serial = 0x0000_1272u;

            Action act = () => EventSink.RaiseHealthBarStateChanged(new HealthBarStateChangedArgs(serial, 2, true));

            act.Should().NotThrow();
            _world.Mobiles.Get(serial).Should().BeNull();
        }

        // ------------------------------------------------------------------
        // OnMobileNameChanged — writes Name onto the resolved entity. The
        // SetWindowTitle branch is gated on Player != null so it's bypassed
        // (Player can't be constructed in tests). UIManager.GetGump returns
        // null with no NameOverheadGump registered, so the ?. chain is safe.
        // ------------------------------------------------------------------

        [Fact]
        public void MobileNameChanged_Sets_Entity_Name()
        {
            uint serial = 0x0000_1280u;
            Mobile mob = _world.GetOrCreateMobile(serial);
            mob.Name = "old-name";

            EventSink.RaiseMobileNameChanged(new MobileNameChangedArgs(serial, "Sir Testalot"));

            mob.Name.Should().Be("Sir Testalot");
        }

        [Fact]
        public void MobileNameChanged_Unknown_Serial_Is_NoOp()
        {
            uint serial = 0x0000_1281u;

            Action act = () => EventSink.RaiseMobileNameChanged(new MobileNameChangedArgs(serial, "ghost"));

            act.Should().NotThrow();
        }

        // ------------------------------------------------------------------
        // OnClientViewRangeChanged — unconditional write of the byte range.
        // ------------------------------------------------------------------

        [Fact]
        public void ClientViewRangeChanged_Updates_World_ClientViewRange()
        {
            byte original = _world.ClientViewRange;
            byte target = (byte)(original == 12 ? 18 : 12);

            EventSink.RaiseClientViewRangeChanged(new ClientViewRangeChangedArgs(target));

            _world.ClientViewRange.Should().Be(target);
        }

        // ------------------------------------------------------------------
        // Skipped handlers — all touch world.Player whose ctor (PlayerMobile)
        // reads unmockable globals (Client.Game.UO.FileManager.*).
        //
        // OnWarModeChanged: writes only to world.Player which requires PlayerMobile that ctor-reads unmockable globals.
        // OnPlayerMoved:    writes only to world.Player which requires PlayerMobile that ctor-reads unmockable globals.
        // OnWalkDenied:     writes only to world.Player which requires PlayerMobile that ctor-reads unmockable globals.
        // OnWalkConfirmed:  writes only to world.Player which requires PlayerMobile that ctor-reads unmockable globals.
        //
        // OnHealthBarStateChanged case 1 (poison): reads Client.Game.UO.Version which is not initialized in unit tests.
        // ------------------------------------------------------------------
    }
}
