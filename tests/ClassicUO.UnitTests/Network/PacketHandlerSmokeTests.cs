// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game;
using ClassicUO.Network;
using ClassicUO.Network.Encryption;
using ClassicUO.Utility;
using Xunit;

namespace ClassicUO.UnitTests.Network
{
    /// <summary>
    /// Smoke tests for the real <see cref="PacketHandlers"/> singleton and its registered handlers.
    /// Characterizes: (1) dispatcher initialization succeeds (all ~150 registrations complete),
    /// and (2) the Player==null early-return path in simple handlers runs safely end-to-end.
    ///
    /// Uses the singleton intentionally — we want to exercise the real static-init registration
    /// chain that Phase 1 must preserve. Tests run serially within the class (xUnit default).
    /// </summary>
    [Collection("PacketHandlersSingleton")]
    public class PacketHandlerSmokeTests
    {
        private static NetClient NewNetClient()
        {
            var client = new NetClient();
            client.Load(ClientVersion.CV_7000, EncryptionType.NONE);
            return client;
        }

        [Fact]
        public void Singleton_InitializesWithoutError()
        {
            // Touching PacketHandlers.Handler runs the static constructor, which registers
            // ~150 handlers. If any registration threw, this access would surface it.
            var handler = PacketHandlers.Handler;
            Assert.NotNull(handler);
        }

        [Fact]
        public void Damage_WithNullPlayer_EarlyReturnsWithoutCrash()
        {
            var socket = NewNetClient();
            var world = new World();   // Player == null
            var handler = PacketHandlers.Handler;

            // 0x0B Damage: serial + damage
            var packet = PacketBuilder.Fixed(0x0B).U32BE(0x40000001).U16BE(25).ToBytes();
            handler.Append(packet, fromPlugins: true);

            var parsed = handler.ParsePackets(socket, world, Span<byte>.Empty);

            Assert.True(parsed >= 1, "Damage packet should be consumed from the stream.");
            // No exception → handler's null-Player early-return behaves as characterized.
        }
    }
}
