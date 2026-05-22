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
    // Behavioral tests for the World subscriber paths that mutate `Player.*`.
    // Previously these paths were skipped because PlayerMobile could not be
    // constructed in tests. The PlayerMobile ctor is now null-safe over
    // Client.Game, and World.SetPlayerForTests / SetMapForTests are exposed.
    //
    // Covered handlers:
    //   OnPlayerUpdated     — World.Subscribers.MobileUpdates.cs (UpdatePlayer)
    //   OnPlayerMoved       — World.Subscribers.cs (Player.Walk early-return)
    //   OnWalkDenied        — World.Subscribers.cs (Walker.DenyWalk)
    //   OnWalkConfirmed     — World.Subscribers.cs (Walker.ConfirmWalk good step)
    //   OnPlayerEnteredWorld— World.Subscribers.cs (CreatePlayer + Graphic)
    //   OnWarModeChanged    — World.Subscribers.cs (Player.InWarMode)
    //
    // Fixture: build a fresh World (auto-wires subscribers via
    // SubscribeEvents), seed Profile, Player, and Map. We do NOT call
    // EventSink.ClearAll() in the ctor — the World's auto-wired subscribers
    // must remain attached so they react to the events we raise.
    [Collection("EventSinkSerial")]
    public class WorldPlayerMovementSubscriberTests : IDisposable
    {
        private readonly World _world;
        private readonly PlayerMobile _player;

        public WorldPlayerMovementSubscriberTests()
        {
            ProfileManager.CurrentProfile = new Profile();
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
        // OnPlayerUpdated — delegates to World.UpdatePlayer which mutates
        // Graphic, Direction, Hue, Flags, RangeSize, and resets the Walker
        // before reaching Client.Game.GetScene<GameScene>() (which NREs in
        // tests; the exception is swallowed by EventSink.Invoke). Mutations
        // performed before the NRE point ARE observable; X/Y/Z (set via
        // SetInWorldTile after the NRE) are NOT.
        // ------------------------------------------------------------------

        [Fact]
        public void PlayerUpdated_Sets_Graphic_Direction_Hue_And_Flags_On_Player()
        {
            _player.Graphic = 0x0000;
            _player.Hue = 0;
            _player.Flags = Flags.None;
            _player.Direction = Direction.North;

            EventSink.RaisePlayerUpdated(new PlayerUpdatedArgs(
                Serial: _player.Serial,
                Graphic: 0x0190,        // human male
                GraphicIncrement: 0,
                Hue: 0x0042,
                Flags: Flags.WarMode,
                X: 1000,
                Y: 500,
                Z: 10,
                ServerId: 0,
                Direction: Direction.South));

            _player.Graphic.Should().Be(0x0190);
            _player.Direction.Should().Be(Direction.South);
            _player.Hue.Should().Be(0x0042); // FixHue passes 0x0042 through unchanged
            _player.Flags.Should().Be(Flags.WarMode);
        }

        [Fact]
        public void PlayerUpdated_Updates_World_RangeSize_To_Args_X_Y()
        {
            _world.RangeSize.X = 0;
            _world.RangeSize.Y = 0;

            EventSink.RaisePlayerUpdated(new PlayerUpdatedArgs(
                Serial: _player.Serial,
                Graphic: 0x0190,
                GraphicIncrement: 0,
                Hue: 0,
                Flags: Flags.None,
                X: 1234,
                Y: 5678,
                Z: 0,
                ServerId: 0,
                Direction: Direction.North));

            _world.RangeSize.X.Should().Be(1234);
            _world.RangeSize.Y.Should().Be(5678);
        }

        [Fact]
        public void PlayerUpdated_Resets_Walker_And_Clears_Steps()
        {
            // Pre-load some walker state so we can observe DenyWalk(0xFF,-1,-1,-1) clearing it.
            _player.Walker.WalkingFailed = true;
            _player.Walker.StepsCount = 3;
            _player.Walker.CurrentWalkSequence = 2;
            _player.Walker.UnacceptedPacketsCount = 4;
            _player.Walker.WalkSequence = 7;
            _player.Steps.AddToBack(new Mobile.Step { X = 1, Y = 1, Z = 0, Direction = 0 });

            EventSink.RaisePlayerUpdated(new PlayerUpdatedArgs(
                Serial: _player.Serial,
                Graphic: 0x0190,
                GraphicIncrement: 0,
                Hue: 0,
                Flags: Flags.None,
                X: 100,
                Y: 100,
                Z: 0,
                ServerId: 0,
                Direction: Direction.North));

            // UpdatePlayer first sets WalkingFailed=false, then DenyWalk(0xFF,-1,-1,-1)
            // calls Walker.Reset() which zeros the queue counters and Player.ClearSteps()
            // which empties the Steps deque.
            _player.Walker.WalkingFailed.Should().BeFalse();
            _player.Walker.StepsCount.Should().Be(0);
            _player.Walker.CurrentWalkSequence.Should().Be(0);
            _player.Walker.UnacceptedPacketsCount.Should().Be(0);
            _player.Walker.WalkSequence.Should().Be(0);
            _player.Steps.Count.Should().Be(0);
        }

        [Fact]
        public void PlayerUpdated_When_Player_Is_Null_Is_NoOp()
        {
            _world.SetPlayerForTests(null);

            Action act = () => EventSink.RaisePlayerUpdated(new PlayerUpdatedArgs(
                Serial: 0x0000_0001u,
                Graphic: 0x0190,
                GraphicIncrement: 0,
                Hue: 0,
                Flags: Flags.None,
                X: 0,
                Y: 0,
                Z: 0,
                ServerId: 0,
                Direction: Direction.North));

            act.Should().NotThrow();
        }

        // ------------------------------------------------------------------
        // OnPlayerMoved — delegates to Player.Walk(direction, isRunning).
        // Player.Walk has a guard `Walker.WalkingFailed → return false` early
        // (before any unmockable globals). We exercise that guard so the
        // handler observably no-ops without throwing.
        //
        // The "happy path" of Player.Walk reads Client.Game.UO.Version and
        // calls NetClient.Socket.Send_WalkRequest — both NRE in tests and
        // the exception is swallowed by EventSink.Invoke. We don't assert on
        // that path because no clean observable state mutation precedes it.
        // ------------------------------------------------------------------

        [Fact]
        public void PlayerMoved_Early_Returns_When_WalkingFailed_Is_True_And_Does_Not_Throw()
        {
            _player.Walker.WalkingFailed = true;
            _player.Direction = Direction.North;
            int stepsBefore = _player.Steps.Count;
            int walkerStepsBefore = _player.Walker.StepsCount;

            Action act = () => EventSink.RaisePlayerMoved(new PlayerMovedArgs(
                Direction: Direction.East,
                IsRunning: false));

            act.Should().NotThrow();
            // Early-return path: no step queued.
            _player.Steps.Count.Should().Be(stepsBefore);
            _player.Walker.StepsCount.Should().Be(walkerStepsBefore);
        }

        [Fact]
        public void PlayerMoved_When_Player_Is_Null_Is_NoOp()
        {
            _world.SetPlayerForTests(null);

            Action act = () => EventSink.RaisePlayerMoved(new PlayerMovedArgs(
                Direction: Direction.East,
                IsRunning: false));

            act.Should().NotThrow();
        }

        // ------------------------------------------------------------------
        // OnWalkDenied — calls Player.Walker.DenyWalk(seq, x, y, z) then
        // sets Player.Direction. DenyWalk clears Steps, resets the walker,
        // and (when x != -1) repositions the player and updates RangeSize.
        // ------------------------------------------------------------------

        [Fact]
        public void WalkDenied_Clears_Steps_And_Resets_Walker_State()
        {
            _player.Steps.AddToBack(new Mobile.Step { X = 5, Y = 5, Z = 0, Direction = 0 });
            _player.Steps.AddToBack(new Mobile.Step { X = 6, Y = 5, Z = 0, Direction = 0 });
            _player.Walker.StepsCount = 2;
            _player.Walker.WalkSequence = 9;
            _player.Walker.CurrentWalkSequence = 1;
            _player.Walker.UnacceptedPacketsCount = 2;
            _player.Walker.WalkingFailed = true;
            _player.Walker.ResendPacketResync = true;

            EventSink.RaiseWalkDenied(new WalkDeniedArgs(
                Sequence: 3,
                X: 50,
                Y: 60,
                Z: 5,
                Direction: Direction.West));

            _player.Steps.Count.Should().Be(0);
            _player.Walker.StepsCount.Should().Be(0);
            _player.Walker.WalkSequence.Should().Be(0);
            _player.Walker.CurrentWalkSequence.Should().Be(0);
            _player.Walker.UnacceptedPacketsCount.Should().Be(0);
            _player.Walker.WalkingFailed.Should().BeFalse();
            _player.Walker.ResendPacketResync.Should().BeFalse();
        }

        [Fact]
        public void WalkDenied_Repositions_Player_And_Updates_RangeSize_To_Args_XY()
        {
            _player.X = 0;
            _player.Y = 0;
            _player.Z = 0;
            _world.RangeSize.X = 0;
            _world.RangeSize.Y = 0;

            EventSink.RaiseWalkDenied(new WalkDeniedArgs(
                Sequence: 7,
                X: 1200,
                Y: 800,
                Z: -5,
                Direction: Direction.South));

            _player.X.Should().Be(1200);
            _player.Y.Should().Be(800);
            _player.Z.Should().Be(-5);
            _world.RangeSize.X.Should().Be(1200);
            _world.RangeSize.Y.Should().Be(800);
        }

        // NOTE: `Player.Direction = e.Direction` runs AFTER Walker.DenyWalk
        // in the handler. DenyWalk calls Player.SetInWorldTile which fires
        // PlayerMobile.OnPositionChanged → Plugin.UpdatePlayerPosition, and
        // that NREs on `Client.Game.PluginHost?...` because Client.Game is
        // null in tests. The exception bubbles out of DenyWalk and is
        // swallowed by EventSink.Invoke, so the Direction assignment never
        // runs. Skipped — not observable through this handler in unit tests.

        [Fact]
        public void WalkDenied_When_Player_Is_Null_Is_NoOp()
        {
            _world.SetPlayerForTests(null);

            Action act = () => EventSink.RaiseWalkDenied(new WalkDeniedArgs(
                Sequence: 0,
                X: 0,
                Y: 0,
                Z: 0,
                Direction: Direction.North));

            act.Should().NotThrow();
        }

        // ------------------------------------------------------------------
        // OnWalkConfirmed — writes NotorietyFlag and invokes Walker.ConfirmWalk
        // which (on the good-step path) accepts the matching StepInfo and
        // updates world.RangeSize. The bad-step path calls NetClient.Socket
        // .Send_Resync which NREs in tests, so we pre-populate a matching
        // StepInfo so the good-step branch is taken.
        // ------------------------------------------------------------------

        [Fact]
        public void WalkConfirmed_Sets_Player_NotorietyFlag_From_Args()
        {
            // Pre-load a matching step so ConfirmWalk takes the accepted branch
            // and does not call NetClient.Socket.Send_Resync().
            _player.Walker.StepsCount = 1;
            _player.Walker.StepInfos[0] = new StepInfo
            {
                Sequence = 5,
                X = 100,
                Y = 100,
                Z = 0,
            };
            _player.NotorietyFlag = NotorietyFlag.Unknown;

            EventSink.RaiseWalkConfirmed(new WalkConfirmedArgs(
                Sequence: 5,
                Notoriety: (byte)NotorietyFlag.Murderer));

            _player.NotorietyFlag.Should().Be(NotorietyFlag.Murderer);
        }

        [Fact]
        public void WalkConfirmed_Coerces_Zero_Notoriety_To_Innocent()
        {
            _player.Walker.StepsCount = 1;
            _player.Walker.StepInfos[0] = new StepInfo
            {
                Sequence = 10,
                X = 50,
                Y = 50,
                Z = 0,
            };

            EventSink.RaiseWalkConfirmed(new WalkConfirmedArgs(
                Sequence: 10,
                Notoriety: 0));

            _player.NotorietyFlag.Should().Be(NotorietyFlag.Innocent); // 0 → 0x01
        }

        [Fact]
        public void WalkConfirmed_Coerces_OutOfRange_Notoriety_To_Innocent()
        {
            _player.Walker.StepsCount = 1;
            _player.Walker.StepInfos[0] = new StepInfo
            {
                Sequence = 11,
                X = 50,
                Y = 50,
                Z = 0,
            };

            EventSink.RaiseWalkConfirmed(new WalkConfirmedArgs(
                Sequence: 11,
                Notoriety: 9)); // >= 8 → coerced to 0x01

            _player.NotorietyFlag.Should().Be(NotorietyFlag.Innocent);
        }

        [Fact]
        public void WalkConfirmed_Accepts_Matching_Step_And_Updates_RangeSize()
        {
            // stepIndex >= CurrentWalkSequence path: StepInfos[stepIndex].Accepted = true
            // and RangeSize.X/Y = StepInfos[stepIndex].X/Y.
            _player.Walker.StepsCount = 1;
            _player.Walker.CurrentWalkSequence = 0;
            _player.Walker.StepInfos[0] = new StepInfo
            {
                Sequence = 4,
                Accepted = false,
                X = 777,
                Y = 555,
                Z = 0,
            };
            _world.RangeSize.X = 0;
            _world.RangeSize.Y = 0;

            EventSink.RaiseWalkConfirmed(new WalkConfirmedArgs(
                Sequence: 4,
                Notoriety: 1));

            _player.Walker.StepInfos[0].Accepted.Should().BeTrue();
            _world.RangeSize.X.Should().Be(777);
            _world.RangeSize.Y.Should().Be(555);
        }

        [Fact]
        public void WalkConfirmed_When_Player_Is_Null_Is_NoOp()
        {
            _world.SetPlayerForTests(null);

            Action act = () => EventSink.RaiseWalkConfirmed(new WalkConfirmedArgs(
                Sequence: 0,
                Notoriety: 1));

            act.Should().NotThrow();
        }

        // ------------------------------------------------------------------
        // OnWarModeChanged — sets Player.InWarMode iff the event Serial
        // matches Player.Serial.
        // ------------------------------------------------------------------

        [Fact]
        public void WarModeChanged_Matching_Serial_Sets_InWarMode_True()
        {
            _player.InWarMode = false;

            EventSink.RaiseWarModeChanged(new WarModeChangedArgs(
                Serial: _player.Serial,
                InWarMode: true));

            _player.InWarMode.Should().BeTrue();
        }

        [Fact]
        public void WarModeChanged_Matching_Serial_Sets_InWarMode_False()
        {
            _player.InWarMode = true;

            EventSink.RaiseWarModeChanged(new WarModeChangedArgs(
                Serial: _player.Serial,
                InWarMode: false));

            _player.InWarMode.Should().BeFalse();
        }

        [Fact]
        public void WarModeChanged_Mismatched_Serial_Does_Not_Touch_Player()
        {
            _player.InWarMode = false;

            EventSink.RaiseWarModeChanged(new WarModeChangedArgs(
                Serial: 0xDEAD_BEEFu,
                InWarMode: true));

            _player.InWarMode.Should().BeFalse();
        }

        // ------------------------------------------------------------------
        // OnPlayerEnteredWorld — calls World.CreatePlayer(serial) which (if
        // Player != null) first calls World.Clear(); inside Clear() the old
        // Player.Destroy() runs Entity.Destroy → GameActions.SendCloseStatus
        // which dereferences Client.Game.UO.Version and NREs. The exception
        // bubbles out of CreatePlayer before Player is replaced.
        //
        // To observe positive state mutations we must run with Player==null
        // up front so CreatePlayer skips Clear() entirely. Map remains seeded
        // (set in fixture, never touched by CreatePlayer) so the handler's
        // `if (Map == null) MapIndex = 0` branch is skipped, avoiding the
        // Client.Game.UO.FileManager NRE in the MapIndex setter.
        //
        // After CreatePlayer succeeds the handler proceeds to:
        //   Player.Graphic = e.Graphic
        //   Player.CheckGraphicChange()
        //   Player.SetInWorldTile(e.X, e.Y, e.Z)  // X/Y/Z set, then NREs
        //                                          // inside UpdateScreenPosition
        //                                          // → Plugin.UpdatePlayerPosition
        //                                          // → Client.Game.PluginHost?
        //   Player.Direction = e.Direction        // unreachable
        //   RangeSize.X/Y = e.X/Y                 // unreachable
        // ------------------------------------------------------------------

        [Fact]
        public void PlayerEnteredWorld_Creates_New_Player_And_Sets_Graphic_And_XYZ()
        {
            // Clear Player so CreatePlayer's `Clear()` early-out fires and we
            // avoid the Entity.Destroy → GameActions.SendCloseStatus NRE path.
            _world.SetPlayerForTests(null);
            uint serial = 0x0000_0042u;

            EventSink.RaisePlayerEnteredWorld(new PlayerEnteredWorldArgs(
                Serial: serial,
                Graphic: 0x0191, // human female
                X: 1500,
                Y: 1600,
                Z: 10,
                Direction: Direction.East));

            _world.Player.Should().NotBeNull();
            _world.Player.Serial.Should().Be(serial);
            _world.Player.Graphic.Should().Be(0x0191);
            // CheckGraphicChange ran (human female sets IsFemale + Race.HUMAN).
            _world.Player.IsFemale.Should().BeTrue();
            _world.Player.Race.Should().Be(RaceType.HUMAN);
            // SetInWorldTile assigned X/Y/Z before its inner UpdateScreenPosition
            // path NREs on Client.Game.PluginHost.
            _world.Player.X.Should().Be(1500);
            _world.Player.Y.Should().Be(1600);
            _world.Player.Z.Should().Be(10);
        }

        [Fact]
        public void PlayerEnteredWorld_With_Male_Graphic_Sets_Race_Human_Not_Female()
        {
            _world.SetPlayerForTests(null);

            EventSink.RaisePlayerEnteredWorld(new PlayerEnteredWorldArgs(
                Serial: 0x0000_0010u,
                Graphic: 0x0190, // human male
                X: 200,
                Y: 300,
                Z: 0,
                Direction: Direction.North));

            _world.Player.Should().NotBeNull();
            _world.Player.Graphic.Should().Be(0x0190);
            _world.Player.IsFemale.Should().BeFalse();
            _world.Player.Race.Should().Be(RaceType.HUMAN);
        }

        // ------------------------------------------------------------------
        // Skipped paths — documented for transparency.
        //
        // OnPlayerEnteredWorldExtras (World.Subscribers.EnterWorld.cs):
        //   Sends NetClient.Socket.Send_* packets and reads Client.Game.Audio
        //   and Client.Game.UO.Version. All side effects are UI/network with
        //   no isolated state mutation worth observing in unit tests.
        //
        // OnPlayerUpdated X/Y/Z mutation:
        //   UpdatePlayer calls Client.Game.GetScene<GameScene>() before
        //   Player.SetInWorldTile + Player.UpdateAbilities. Client.Game is
        //   null in tests → NRE swallowed by EventSink.Invoke → those
        //   mutations are not reached. Covered above only up to the Walker
        //   reset point.
        //
        // OnPlayerMoved happy path:
        //   Player.Walk reads Client.Game.UO.Version and calls
        //   NetClient.Socket.Send_WalkRequest — both NRE. Only the
        //   WalkingFailed early-return guard is exercised here.
        //
        // OnWalkDenied Player.Direction assignment:
        //   The handler runs `Player.Direction = e.Direction` AFTER
        //   Walker.DenyWalk; DenyWalk's Player.SetInWorldTile path NREs
        //   inside PlayerMobile.OnPositionChanged → Plugin.UpdatePlayerPosition
        //   (Client.Game is null). Exception swallowed; Direction never set.
        //
        // OnPlayerEnteredWorld post-SetInWorldTile writes (Direction + RangeSize):
        //   SetInWorldTile NREs inside its UpdateScreenPosition →
        //   Plugin.UpdatePlayerPosition path; subsequent assignments unreachable.
        // ------------------------------------------------------------------
    }
}
