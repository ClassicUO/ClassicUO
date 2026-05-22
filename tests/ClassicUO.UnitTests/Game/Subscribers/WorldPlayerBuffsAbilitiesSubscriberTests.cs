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
    // Behavioural tests for the player-only subscriber paths in
    // World.Subscribers.cs (buffs), World.Subscribers.ExtendedStats.cs (stat
    // locks), World.Subscribers.ExtendedMisc.cs (abilities + speed mode) and
    // World.Subscribers.ExtendedWalk.cs (fast-walk stack).
    //
    // These handlers were previously unreachable from unit tests because
    // PlayerMobile's ctor dereferenced Client.Game globals. The ctor is now
    // null-safe and World exposes SetPlayerForTests / SetMapForTests so we
    // can seed a real PlayerMobile and assert mutations on it.
    //
    // The auto-wired World subscribers must stay live — no EventSink.ClearAll()
    // in the ctor. Dispose() drops them so cross-class leakage doesn't bite.
    [Collection("EventSinkSerial")]
    public class WorldPlayerBuffsAbilitiesSubscriberTests : IDisposable
    {
        private readonly World _world;
        private readonly PlayerMobile _player;

        public WorldPlayerBuffsAbilitiesSubscriberTests()
        {
            ProfileManager.CurrentProfile = new Profile();

            // OnBuffApplied indexes BuffTable.Table; Load() falls back to the
            // built-in default table if no buff.txt is present (test binary
            // working dir). Safe to call repeatedly.
            BuffTable.Load();

            _world = new World();
            _player = new PlayerMobile(_world, 0x0000_0001u);
            _world.SetPlayerForTests(_player);
            _world.SetMapForTests(new ClassicUO.Game.Map.Map(_world, 0));
        }

        public void Dispose()
        {
            EventSink.ClearAll();
        }

        // ------------------------------------------------------------------
        // OnBuffApplied — writes world.Player.BuffIcons[type] = new BuffIcon(...)
        // ------------------------------------------------------------------

        [Fact]
        public void BuffApplied_Adds_Entry_To_BuffIcons()
        {
            ushort iconType = (ushort)BuffIconType.NightSight;
            _player.BuffIcons.ContainsKey(BuffIconType.NightSight).Should().BeFalse();

            EventSink.RaiseBuffApplied(new BuffAppliedArgs(
                Serial: _player.Serial,
                IconType: iconType,
                IconId: 0, // points at BuffTable.Table[0] — within bounds
                Timer: 0,
                Text: "nightsight"));

            _player.BuffIcons.ContainsKey(BuffIconType.NightSight).Should().BeTrue();
            BuffIcon icon = _player.BuffIcons[BuffIconType.NightSight];
            icon.Should().NotBeNull();
            icon.Type.Should().Be(BuffIconType.NightSight);
            icon.Text.Should().Be("nightsight");
        }

        [Fact]
        public void BuffApplied_With_IconId_Out_Of_Range_Is_NoOp()
        {
            // The handler early-returns if IconId >= BuffTable.Table.Length.
            ushort overflow = (ushort)(BuffTable.Table.Length + 1);

            EventSink.RaiseBuffApplied(new BuffAppliedArgs(
                Serial: _player.Serial,
                IconType: (ushort)BuffIconType.DivineFury,
                IconId: overflow,
                Timer: 0,
                Text: "ignored"));

            _player.BuffIcons.ContainsKey(BuffIconType.DivineFury).Should().BeFalse();
        }

        [Fact]
        public void BuffApplied_Overwrites_Existing_Entry_For_Same_Type()
        {
            ushort iconType = (ushort)BuffIconType.DeathStrike;

            EventSink.RaiseBuffApplied(new BuffAppliedArgs(
                _player.Serial, iconType, 0, 0, "first"));
            EventSink.RaiseBuffApplied(new BuffAppliedArgs(
                _player.Serial, iconType, 0, 0, "second"));

            _player.BuffIcons[BuffIconType.DeathStrike].Text.Should().Be("second");
        }

        // ------------------------------------------------------------------
        // OnBuffRemoved — removes entry from BuffIcons.
        // ------------------------------------------------------------------

        [Fact]
        public void BuffRemoved_Drops_Entry_From_BuffIcons()
        {
            ushort iconType = (ushort)BuffIconType.EvilOmen;

            // Pre-seed via BuffApplied — exercises the real wiring rather
            // than poking the dictionary directly.
            EventSink.RaiseBuffApplied(new BuffAppliedArgs(
                _player.Serial, iconType, 0, 0, "doomed"));
            _player.BuffIcons.ContainsKey(BuffIconType.EvilOmen).Should().BeTrue();

            EventSink.RaiseBuffRemoved(new BuffRemovedArgs(_player.Serial, iconType));

            _player.BuffIcons.ContainsKey(BuffIconType.EvilOmen).Should().BeFalse();
        }

        [Fact]
        public void BuffRemoved_For_Absent_Type_Is_NoOp()
        {
            // Dictionary.Remove on a missing key just returns false — handler
            // must not throw.
            Action act = () => EventSink.RaiseBuffRemoved(
                new BuffRemovedArgs(_player.Serial, (ushort)BuffIconType.HonoredDebuff));

            act.Should().NotThrow();
            _player.BuffIcons.ContainsKey(BuffIconType.HonoredDebuff).Should().BeFalse();
        }

        // ------------------------------------------------------------------
        // OnExtendedStatsLocks — writes Player.StrLock / DexLock / IntLock,
        // gated on Serial == Player.Serial.
        // ------------------------------------------------------------------

        [Fact]
        public void ExtendedStatsLocks_Writes_All_Three_Locks()
        {
            EventSink.RaiseExtendedStatsLocks(new ExtendedStatsLocksArgs(
                Serial: _player.Serial,
                StrLock: (byte)Lock.Down,
                DexLock: (byte)Lock.Locked,
                IntLock: (byte)Lock.Up));

            _player.StrLock.Should().Be(Lock.Down);
            _player.DexLock.Should().Be(Lock.Locked);
            _player.IntLock.Should().Be(Lock.Up);
        }

        [Fact]
        public void ExtendedStatsLocks_Wrong_Serial_Is_NoOp()
        {
            _player.StrLock = Lock.Up;
            _player.DexLock = Lock.Up;
            _player.IntLock = Lock.Up;

            EventSink.RaiseExtendedStatsLocks(new ExtendedStatsLocksArgs(
                Serial: _player.Serial + 1, // mismatched
                StrLock: (byte)Lock.Locked,
                DexLock: (byte)Lock.Locked,
                IntLock: (byte)Lock.Locked));

            _player.StrLock.Should().Be(Lock.Up);
            _player.DexLock.Should().Be(Lock.Up);
            _player.IntLock.Should().Be(Lock.Up);
        }

        // ------------------------------------------------------------------
        // OnAbilityIconsReset — for each i in [0,1]: Abilities[i] &= 0x7F.
        // ------------------------------------------------------------------

        [Fact]
        public void AbilityIconsReset_Masks_Off_Top_Bit_Of_Both_Slots()
        {
            // Default value is Ability.Invalid (0xFF) which has the top bit
            // set — perfect candidate for the mask.
            _player.Abilities[0] = (Ability)0xFF;
            _player.Abilities[1] = (Ability)0x83; // 1000_0011 — bit 7 set

            EventSink.RaiseAbilityIconsReset(new AbilityIconsResetArgs());

            ((ushort)_player.Abilities[0]).Should().Be(0x7F);
            ((ushort)_player.Abilities[1]).Should().Be(0x03);
        }

        [Fact]
        public void AbilityIconsReset_Leaves_Values_Without_Top_Bit_Unchanged()
        {
            _player.Abilities[0] = Ability.ArmorIgnore;  // 1
            _player.Abilities[1] = Ability.WhirlwindAttack; // 13

            EventSink.RaiseAbilityIconsReset(new AbilityIconsResetArgs());

            _player.Abilities[0].Should().Be(Ability.ArmorIgnore);
            _player.Abilities[1].Should().Be(Ability.WhirlwindAttack);
        }

        // ------------------------------------------------------------------
        // OnCharacterSpeedMode — Player.SpeedMode = (CharacterSpeedType)val,
        // clamped to 0 if val > FastUnmountAndCantRun (3).
        // ------------------------------------------------------------------

        [Fact]
        public void CharacterSpeedMode_Happy_Path_Writes_Enum_Value()
        {
            EventSink.RaiseCharacterSpeedMode(new CharacterSpeedModeArgs(
                SpeedMode: (byte)CharacterSpeedType.FastUnmount));

            _player.SpeedMode.Should().Be(CharacterSpeedType.FastUnmount);
        }

        [Fact]
        public void CharacterSpeedMode_Boundary_FastUnmountAndCantRun_Is_Accepted()
        {
            EventSink.RaiseCharacterSpeedMode(new CharacterSpeedModeArgs(
                SpeedMode: (byte)CharacterSpeedType.FastUnmountAndCantRun));

            _player.SpeedMode.Should().Be(CharacterSpeedType.FastUnmountAndCantRun);
        }

        [Fact]
        public void CharacterSpeedMode_Out_Of_Range_Clamps_To_Normal()
        {
            _player.SpeedMode = CharacterSpeedType.CantRun;

            // 99 > (int)FastUnmountAndCantRun(3) → handler clamps to 0.
            EventSink.RaiseCharacterSpeedMode(new CharacterSpeedModeArgs(SpeedMode: 99));

            _player.SpeedMode.Should().Be(CharacterSpeedType.Normal);
        }

        // ------------------------------------------------------------------
        // OnFastWalkStackInit / OnFastWalkStackAdd — FastWalkStack only
        // exposes SetValue / AddValue / GetValue. We observe by draining via
        // GetValue() which pops the first non-zero slot.
        // ------------------------------------------------------------------

        [Fact]
        public void FastWalkStackInit_Seeds_First_Five_Slots_From_Values()
        {
            var values = new uint[] { 0xAAAA_AAAA, 0xBBBB_BBBB, 0xCCCC_CCCC, 0xDDDD_DDDD, 0xEEEE_EEEE };

            EventSink.RaiseFastWalkStackInit(new FastWalkStackInitArgs(Values: values));

            // Drain via GetValue — values come back in slot order.
            _player.Walker.FastWalkStack.GetValue().Should().Be(0xAAAA_AAAAu);
            _player.Walker.FastWalkStack.GetValue().Should().Be(0xBBBB_BBBBu);
            _player.Walker.FastWalkStack.GetValue().Should().Be(0xCCCC_CCCCu);
            _player.Walker.FastWalkStack.GetValue().Should().Be(0xDDDD_DDDDu);
            _player.Walker.FastWalkStack.GetValue().Should().Be(0xEEEE_EEEEu);
            _player.Walker.FastWalkStack.GetValue().Should().Be(0u); // exhausted
        }

        [Fact]
        public void FastWalkStackInit_With_Null_Values_Zeroes_All_Slots()
        {
            // First seed with non-zero values so we can prove they get cleared.
            EventSink.RaiseFastWalkStackInit(new FastWalkStackInitArgs(Values: new uint[] { 1, 2, 3 }));

            EventSink.RaiseFastWalkStackInit(new FastWalkStackInitArgs(Values: null));

            _player.Walker.FastWalkStack.GetValue().Should().Be(0u);
        }

        [Fact]
        public void FastWalkStackAdd_Pushes_Value_Into_Next_Empty_Slot()
        {
            EventSink.RaiseFastWalkStackAdd(new FastWalkStackAddArgs(Value: 0x1234_5678u));

            _player.Walker.FastWalkStack.GetValue().Should().Be(0x1234_5678u);
            _player.Walker.FastWalkStack.GetValue().Should().Be(0u); // only one slot occupied
        }

        [Fact]
        public void FastWalkStackAdd_Appends_After_Existing_Init_Values()
        {
            EventSink.RaiseFastWalkStackInit(new FastWalkStackInitArgs(Values: new uint[] { 0x11, 0x22 }));
            EventSink.RaiseFastWalkStackAdd(new FastWalkStackAddArgs(Value: 0x33));

            // Init wrote slots [0..1] = {0x11, 0x22}; slot 2 was zeroed by the
            // init loop, so Add lands in slot 2.
            _player.Walker.FastWalkStack.GetValue().Should().Be(0x11u);
            _player.Walker.FastWalkStack.GetValue().Should().Be(0x22u);
            _player.Walker.FastWalkStack.GetValue().Should().Be(0x33u);
        }

        // ------------------------------------------------------------------
        // Handlers NOT covered here — they touch globals beyond what our
        // seams reach, so they remain unreachable from unit tests:
        //
        //  - OnMapPatchesEnabled: calls Client.Game.UO.FileManager.Maps.ApplyPatches.
        //  - OnExtendedStatsAnimation: dereferences Client.Game.UO.FileManager.Animations
        //    via Mobile.GetReplacedObjectAnimation.
        //  - OnMapIndexChanged: setter triggers FileManager.Maps.LoadMap on the first
        //    non-default transition.
        // ------------------------------------------------------------------
    }
}
