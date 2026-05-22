// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game;
using ClassicUO.Game.Events;
using ClassicUO.Network;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Events
{
    // Parser-side coverage for the 0x55 LoginComplete handler.
    //
    // Pre-migration the handler raised LoginCompleted AND performed ~30 lines
    // of tail work inline (scene swap to GameScene, RequestMobileStatus,
    // OpenChat/SkillsRequest/ClientType/ClientViewRange network sends, profile
    // gump load). That tail now lives in LoginScene.OnLoginCompleted, so the
    // handler is parse-and-emit-only and this test asserts the event fires.
    //
    // Subscriber-side coverage (LoginScene.OnLoginCompleted) is intentionally
    // skipped: the subscriber requires a live Client.Game with a current
    // Scene, a NetClient.Socket, ProfileManager.CurrentProfile, and UIManager
    // state — none of which can be constructed in a unit-test harness without
    // reflection or a wider seam (forbidden by phase rules).
    public class LoginCompleteTests : IDisposable
    {
        private readonly World _world;

        public LoginCompleteTests()
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

        [Fact]
        public void LoginComplete_0x55_RaisesLoginCompletedExactlyOnce()
        {
            int count = 0;
            EventSink.LoginCompleted += _ => count++;

            // 0x55 is fixed-length (id only); dispatch at offset=1.
            Dispatch(new byte[] { 0x55 }, 1);

            count.Should().Be(1);
        }

        [Fact]
        public void LoginComplete_0x55_HandlerDoesNotMutateWorldState()
        {
            // Handler is parse-and-emit-only — the tail work moved to
            // LoginScene.OnLoginCompleted, so dispatching 0x55 in isolation
            // must not touch world.Player / world.Map / scene state.
            EventSink.LoginCompleted += _ => { };

            Dispatch(new byte[] { 0x55 }, 1);

            _world.Player.Should().BeNull();
            _world.Map.Should().BeNull();
        }
    }
}
