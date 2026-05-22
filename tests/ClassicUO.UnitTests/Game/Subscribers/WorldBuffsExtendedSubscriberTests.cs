// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Subscribers
{
    // Behavioural tests for the World subscribers that live in
    // World.Subscribers.cs (buffs), World.Subscribers.ExtendedStats.cs,
    // World.Subscribers.ExtendedMisc.cs and World.Subscribers.ExtendedWalk.cs.
    //
    // The auto-wired World subscribers stay live (no EventSink.ClearAll() in the
    // ctor) — the goal is to prove the handlers mutate world state when the
    // matching EventSink event is raised.
    //
    // Several handlers touch world.Player or globals that require a fully
    // initialised game host (Client.Game.UO.FileManager.*). Those paths are
    // skipped — see the per-handler comments below.
    [Collection("EventSinkSerial")]
    public class WorldBuffsExtendedSubscriberTests : IDisposable
    {
        private readonly World _world;

        public WorldBuffsExtendedSubscriberTests()
        {
            ProfileManager.CurrentProfile = new Profile();
            _world = new World();
            _world.SetInGameForTests(true);
        }

        public void Dispose()
        {
            EventSink.ClearAll();
        }

        // ---- OnBuffApplied / OnBuffRemoved ------------------------------------
        // Both handlers gate on `Player == null` and then write to
        // world.Player.BuffIcons via Player.AddBuff/Player.RemoveBuff. PlayerMobile
        // cannot be constructed in tests (its ctor reads unmockable globals), so
        // these handlers are unreachable from unit tests. We assert only the
        // null-Player early-return — raising the event with no Player set must
        // not throw.
        [Fact]
        public void BuffApplied_With_No_Player_Is_NoOp()
        {
            _world.Player.Should().BeNull();

            Action act = () => EventSink.RaiseBuffApplied(new BuffAppliedArgs(
                Serial: 0u,
                IconType: 1,
                IconId: 0x03EE,
                Timer: 0,
                Text: "any"));

            act.Should().NotThrow();
        }

        [Fact]
        public void BuffRemoved_With_No_Player_Is_NoOp()
        {
            _world.Player.Should().BeNull();

            Action act = () => EventSink.RaiseBuffRemoved(new BuffRemovedArgs(Serial: 0u, IconType: 1));

            act.Should().NotThrow();
        }

        // ---- OnExtendedStatsBonded -------------------------------------------
        [Fact]
        public void ExtendedStatsBonded_Sets_IsDead_On_Existing_Mobile()
        {
            uint serial = 0x0000_1001u;
            Mobile mob = _world.GetOrCreateMobile(serial);
            mob.IsDead.Should().BeFalse();

            EventSink.RaiseExtendedStatsBonded(new ExtendedStatsBondedArgs(serial, IsDead: true));

            mob.IsDead.Should().BeTrue();
        }

        [Fact]
        public void ExtendedStatsBonded_Can_Clear_IsDead()
        {
            uint serial = 0x0000_1002u;
            Mobile mob = _world.GetOrCreateMobile(serial);
            mob.IsDead = true;

            EventSink.RaiseExtendedStatsBonded(new ExtendedStatsBondedArgs(serial, IsDead: false));

            // _isDead is the writable backing for IsDead; with a default Graphic
            // of 0 (none of the corpse-graphic cases match) the getter mirrors
            // the value we just wrote.
            mob.IsDead.Should().BeFalse();
        }

        [Fact]
        public void ExtendedStatsBonded_For_Unknown_Mobile_Is_NoOp()
        {
            uint serial = 0x0000_1003u;
            _world.Mobiles.Get(serial).Should().BeNull();

            Action act = () => EventSink.RaiseExtendedStatsBonded(
                new ExtendedStatsBondedArgs(serial, IsDead: true));

            act.Should().NotThrow();
            _world.Mobiles.Get(serial).Should().BeNull(); // handler does NOT create
        }

        // Skipped: OnExtendedStatsLocks — writes world.Player.StrLock / DexLock /
        // IntLock and queries StatusGumpBase.GetStatusGump(), both player-only.

        // Skipped: OnExtendedStatsAnimation — calls
        // Mobile.GetReplacedObjectAnimation(...) which dereferences
        // Client.Game.UO.FileManager.Animations (unmockable in unit tests).

        // ---- OnSpellbookContent ----------------------------------------------
        [Fact]
        public void SpellbookContent_Sets_Graphic_And_Populates_Items()
        {
            uint spellbookSerial = 0x4000_2001u;
            // Pre-seed the spellbook so we observe Graphic + child population on
            // an existing instance.
            Item spellbook = _world.GetOrCreateItem(spellbookSerial);
            spellbook.Graphic = 0x0000;

            EventSink.RaiseSpellbookContent(new SpellbookContentArgs(
                Serial: spellbookSerial,
                SpellbookGraphic: 0x0EFA,
                SpellbookType: 0,
                SpellIds: new[] { 1, 2, 3 }));

            spellbook.Graphic.Should().Be((ushort)0x0EFA);

            // Each spell entry is pushed as a child item with Graphic 0x1F2E and
            // Amount == ushort spell id. PushToBack stores them in the Items
            // linked list reachable via spellbook.Items.
            int count = 0;
            ushort firstAmount = 0;
            for (LinkedObject node = spellbook.Items; node != null; node = node.Next)
            {
                if (count == 0 && node is Item it) firstAmount = it.Amount;
                count++;
            }
            count.Should().Be(3);
            firstAmount.Should().Be((ushort)1);
        }

        [Fact]
        public void SpellbookContent_With_Null_SpellIds_Clears_And_Does_Not_Throw()
        {
            uint spellbookSerial = 0x4000_2002u;

            Action act = () => EventSink.RaiseSpellbookContent(new SpellbookContentArgs(
                Serial: spellbookSerial,
                SpellbookGraphic: 0x0EFB,
                SpellbookType: 0,
                SpellIds: null));

            act.Should().NotThrow();

            Item spellbook = _world.Items.Get(spellbookSerial);
            spellbook.Should().NotBeNull();
            spellbook.Graphic.Should().Be((ushort)0x0EFB);
            spellbook.Items.Should().BeNull();
        }

        // ---- OnMapPatchesEnabled ---------------------------------------------
        // Skipped: calls Client.Game.UO.FileManager.Maps.ApplyPatches — globals.

        // ---- OnAbilityIconsReset ---------------------------------------------
        // Skipped: writes world.Player.Abilities[i] (player-only).

        // ---- OnDamageOverhead ------------------------------------------------
        [Fact]
        public void DamageOverhead_With_Zero_Damage_Is_NoOp_Even_For_Existing_Entity()
        {
            uint serial = 0x0000_3001u;
            _world.GetOrCreateMobile(serial);

            Action act = () => EventSink.RaiseDamageOverhead(new DamageOverheadArgs(serial, Damage: 0));

            act.Should().NotThrow();
        }

        [Fact]
        public void DamageOverhead_For_Unknown_Serial_Is_NoOp()
        {
            uint serial = 0x0000_3002u;
            _world.Mobiles.Get(serial).Should().BeNull();

            Action act = () => EventSink.RaiseDamageOverhead(new DamageOverheadArgs(serial, Damage: 5));

            act.Should().NotThrow();
        }

        // ---- OnSpellIconToggle -----------------------------------------------
        // The handler iterates UIManager.Gumps looking for a matching
        // UseSpellButtonGump. Constructing a real UseSpellButtonGump requires
        // GraphicsDevice / FileManager globals, so with no gump in the manager
        // ActiveSpellIcons stays untouched. We verify the no-throw path.
        [Fact]
        public void SpellIconToggle_With_No_Matching_Gump_Does_Not_Mutate_ActiveSpellIcons()
        {
            const ushort spellId = 42;
            _world.ActiveSpellIcons.IsActive(spellId).Should().BeFalse();

            EventSink.RaiseSpellIconToggle(new SpellIconToggleArgs(SpellId: spellId, Active: true));

            // No UseSpellButtonGump exists → Add() never called.
            _world.ActiveSpellIcons.IsActive(spellId).Should().BeFalse();

            EventSink.RaiseSpellIconToggle(new SpellIconToggleArgs(SpellId: spellId, Active: false));
            _world.ActiveSpellIcons.IsActive(spellId).Should().BeFalse();
        }

        // ---- OnCharacterSpeedMode --------------------------------------------
        // Skipped: writes world.Player.SpeedMode (player-only).

        // ---- OnMobileAnimationFrame ------------------------------------------
        [Fact]
        public void MobileAnimationFrame_Updates_Matching_Mobile_By_Partial_Serial()
        {
            // PartialSerial is the low 16 bits — pick a serial whose low word
            // we want to match.
            uint serial = 0x0001_BEEFu;
            ushort partial = 0xBEEF;

            Mobile mob = _world.GetOrCreateMobile(serial);
            mob.ExecuteAnimation = true;
            mob.AnimIndex = 0;

            EventSink.RaiseMobileAnimationFrame(new MobileAnimationFrameArgs(
                PartialSerial: partial,
                AnimationId: 12,
                FrameCount: 7));

            mob.AnimIndex.Should().Be((byte)7);
            mob.ExecuteAnimation.Should().BeFalse();
        }

        [Fact]
        public void MobileAnimationFrame_With_No_Matching_Partial_Is_NoOp()
        {
            uint serial = 0x0001_AAAAu;
            Mobile mob = _world.GetOrCreateMobile(serial);
            mob.ExecuteAnimation = true;
            byte initialAnimIndex = mob.AnimIndex;

            EventSink.RaiseMobileAnimationFrame(new MobileAnimationFrameArgs(
                PartialSerial: 0xBBBB,
                AnimationId: 3,
                FrameCount: 9));

            mob.AnimIndex.Should().Be(initialAnimIndex);
            mob.ExecuteAnimation.Should().BeTrue();
        }

        // ---- OnCuoCommand ----------------------------------------------------
        [Fact]
        public void CuoCommand_Does_Not_Throw()
        {
            Action act = () => EventSink.RaiseCuoCommand(new CuoCommandArgs(Type: 0x1234));
            act.Should().NotThrow();
        }

        // ---- OnFastWalkStackInit / OnFastWalkStackAdd ------------------------
        // Skipped: both gate on `Player == null` and then write to
        // Player.Walker.FastWalkStack — player-only.

        // ---- OnMapIndexChanged -----------------------------------------------
        // Skipped: setter calls Client.Game.UO.FileManager.Maps.LoadMap on the
        // very first transition from -1 → 0, which dereferences unmockable
        // globals.
    }
}
