// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game;
using ClassicUO.Game.Events;
using ClassicUO.Game.Managers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Subscribers
{
    // Behavioral tests for TargetManager's EventSink subscriptions.
    //
    // Pattern: build a fresh TargetManager AFTER EventSink.ClearAll() and
    // call Subscribe() explicitly so the manager wires its handlers
    // (TargetCursorReceived, AttackTargetChanged, MultiPlacementReceived).
    // The test then raises each event and asserts the observable state on
    // the new manager.
    //
    // World ctor already creates a TargetManager that subscribes, so we
    // explicitly clear and rebuild to keep the test's manager the only
    // listener. The fresh World has Player == null, which keeps the
    // GameActions network calls behind their world.InGame guards (no
    // NetClient.Socket.PacketsTable null-deref).
    //
    // SKIPPED: paths that send a TargetType.Cancel — those route through
    // CancelTarget() which calls NetClient.Socket.Send_TargetCancel and
    // dereferences PacketsTable (null in unit tests).
    //
    // SKIPPED: OnAttackTargetChanged observable mutation (LastAttack = e.Serial)
    // — the handler first calls GameActions.SendCloseStatus, which dereferences
    // Client.Game.UO.Version *before* writing LastAttack. Client.Game is null
    // outside of the running game, so the handler throws an NRE that
    // EventSink.Invoke swallows, and LastAttack stays at 0. We can't observe
    // the assignment without standing up Client.Game / the asset stack.
    public class TargetManagerTests : IDisposable
    {
        private readonly World _world;
        private TargetManager _target;

        public TargetManagerTests()
        {
            _world = new World();
            EventSink.ClearAll();
            _target = new TargetManager(_world);
            _target.Subscribe();
        }

        public void Dispose()
        {
            _target.Unsubscribe();
            EventSink.ClearAll();
        }

        // ---- TargetCursorReceived ----

        [Fact]
        public void OnTargetCursorReceived_Object_Neutral_SetsTargetingState()
        {
            EventSink.RaiseTargetCursorReceived(new TargetCursorReceivedArgs(
                CursorType: (byte)CursorTarget.Object,
                TargetId: 0xDEADBEEF,
                TargetType: (byte)TargetType.Neutral));

            _target.TargetingState.Should().Be(CursorTarget.Object);
            _target.TargetingType.Should().Be(TargetType.Neutral);
            _target.IsTargeting.Should().BeTrue();
        }

        [Fact]
        public void OnTargetCursorReceived_Position_Harmful_SetsTargetingType()
        {
            EventSink.RaiseTargetCursorReceived(new TargetCursorReceivedArgs(
                CursorType: (byte)CursorTarget.Position,
                TargetId: 0x42,
                TargetType: (byte)TargetType.Harmful));

            _target.TargetingState.Should().Be(CursorTarget.Position);
            _target.TargetingType.Should().Be(TargetType.Harmful);
            _target.IsTargeting.Should().BeTrue();
        }

        // NOTE: no Invalid-cursor test. CursorTarget.Invalid is -1, which can't
        // be represented in the byte-typed CursorType field that flows in via
        // the network packet, so the OnTargetCursorReceived path that handles
        // "Invalid" is effectively unreachable from a real packet/event.

        [Fact]
        public void OnTargetCursorReceived_SecondEvent_OverridesFirst()
        {
            EventSink.RaiseTargetCursorReceived(new TargetCursorReceivedArgs(
                CursorType: (byte)CursorTarget.Object,
                TargetId: 0x1,
                TargetType: (byte)TargetType.Neutral));
            EventSink.RaiseTargetCursorReceived(new TargetCursorReceivedArgs(
                CursorType: (byte)CursorTarget.Position,
                TargetId: 0x2,
                TargetType: (byte)TargetType.Beneficial));

            _target.TargetingState.Should().Be(CursorTarget.Position);
            _target.TargetingType.Should().Be(TargetType.Beneficial);
            _target.IsTargeting.Should().BeTrue();
        }

        // ---- AttackTargetChanged ----
        // No tests: see class-level XML comment. OnAttackTargetChanged's first
        // line dereferences Client.Game.UO.Version (via GameActions.SendCloseStatus),
        // which is null in unit tests, so the handler throws before reaching
        // the LastAttack assignment. The exception is swallowed by EventSink's
        // Invoke wrapper, leaving no observable state change.

        // ---- MultiPlacementReceived ----

        [Fact]
        public void OnMultiPlacementReceived_SetsMultiPlacementStateAndInfo()
        {
            EventSink.RaiseMultiPlacementReceived(new MultiPlacementReceivedArgs(
                AllowGround: 1,
                TargetId: 0xABCDEFu,
                Flags: 0,
                MultiId: 0x4001,
                OffsetX: 10,
                OffsetY: 20,
                OffsetZ: 5,
                Hue: 0x1234));

            _target.TargetingState.Should().Be(CursorTarget.MultiPlacement);
            _target.TargetingType.Should().Be(TargetType.Neutral);
            _target.IsTargeting.Should().BeTrue();

            _target.MultiTargetInfo.Should().NotBeNull();
            _target.MultiTargetInfo!.Model.Should().Be((ushort)0x4001);
            _target.MultiTargetInfo.XOff.Should().Be((ushort)10);
            _target.MultiTargetInfo.YOff.Should().Be((ushort)20);
            _target.MultiTargetInfo.ZOff.Should().Be((ushort)5);
            _target.MultiTargetInfo.Hue.Should().Be((ushort)0x1234);
        }

        [Fact]
        public void OnMultiPlacementReceived_LaterEvent_ReplacesMultiTargetInfo()
        {
            EventSink.RaiseMultiPlacementReceived(new MultiPlacementReceivedArgs(
                AllowGround: 1, TargetId: 1, Flags: 0,
                MultiId: 0x1000, OffsetX: 1, OffsetY: 2, OffsetZ: 3, Hue: 0));

            EventSink.RaiseMultiPlacementReceived(new MultiPlacementReceivedArgs(
                AllowGround: 1, TargetId: 2, Flags: 0,
                MultiId: 0x2000, OffsetX: 7, OffsetY: 8, OffsetZ: 9, Hue: 0xFF));

            _target.TargetingState.Should().Be(CursorTarget.MultiPlacement);
            _target.MultiTargetInfo!.Model.Should().Be((ushort)0x2000);
            _target.MultiTargetInfo.XOff.Should().Be((ushort)7);
            _target.MultiTargetInfo.Hue.Should().Be((ushort)0xFF);
        }
    }
}
