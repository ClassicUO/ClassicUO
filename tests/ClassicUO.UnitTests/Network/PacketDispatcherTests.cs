// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Network.Encryption;
using ClassicUO.Utility;
using Xunit;

namespace ClassicUO.UnitTests.Network
{
    /// <summary>
    /// Characterizes <see cref="PacketHandlers"/> dispatch behavior so that the Phase 1 refactor
    /// (splitting PacketHandlers.cs into per-family partials) can be verified to preserve it.
    /// These tests bypass the plugin path by appending to the "fromPlugins" buffer directly,
    /// so no Client.Game setup is required.
    /// </summary>
    public class PacketDispatcherTests
    {
        private static NetClient NewNetClient()
        {
            var client = new NetClient();
            client.Load(ClientVersion.CV_7000, EncryptionType.NONE);
            return client;
        }

        private sealed class HandlerSpy
        {
            public int CallCount;
            public byte LastPacketId;
            public int LastPayloadLength;

            public void Handle(World world, ref StackDataReader p)
            {
                CallCount++;
                LastPacketId = p.Buffer[0];
                LastPayloadLength = p.Remaining;
            }
        }

        [Fact]
        public void Dispatcher_RoutesKnownPacket_ToRegisteredHandler()
        {
            var socket = NewNetClient();
            var world = new World();
            var handlers = new PacketHandlers();
            var spy = new HandlerSpy();
            handlers.Add(0x0B, spy.Handle);

            // 0x0B Damage: 7 bytes total (1 ID + 4 serial + 2 damage)
            var packet = PacketBuilder.Fixed(0x0B).U32BE(0x12345678).U16BE(25).ToBytes();
            handlers.Append(packet, fromPlugins: true);

            var parsed = handlers.ParsePackets(socket, world, Span<byte>.Empty);

            Assert.Equal(1, parsed);
            Assert.Equal(1, spy.CallCount);
            Assert.Equal((byte)0x0B, spy.LastPacketId);
        }

        [Fact]
        public void Dispatcher_UnknownPacketId_DoesNotCrash()
        {
            var socket = NewNetClient();
            var world = new World();
            var handlers = new PacketHandlers();   // no handlers registered

            // 0x2B has length 2 in the table — send without registering a handler
            var packet = PacketBuilder.Fixed(0x2B).U8(0).ToBytes();
            handlers.Append(packet, fromPlugins: true);

            var parsed = handlers.ParsePackets(socket, world, Span<byte>.Empty);

            // Dispatcher consumes the packet from the stream even when no handler is registered.
            // This is the current behavior Phase 1 must preserve.
            Assert.Equal(1, parsed);
        }

        [Fact]
        public void Dispatcher_VariableLengthPacket_ReadsHeaderCorrectly()
        {
            var socket = NewNetClient();
            var world = new World();
            var handlers = new PacketHandlers();
            var spy = new HandlerSpy();
            handlers.Add(0x11, spy.Handle);   // 0x11 CharacterStatus is variable-length

            // Variable-length: ID + 2-byte BE length + payload
            var packet = PacketBuilder.Variable(0x11)
                .U32BE(0xDEADBEEF)
                .AsciiFixed("test", 30)
                .U16BE(100)
                .U16BE(100)
                .ToBytes();

            handlers.Append(packet, fromPlugins: true);

            var parsed = handlers.ParsePackets(socket, world, Span<byte>.Empty);

            Assert.Equal(1, parsed);
            Assert.Equal(1, spy.CallCount);
            Assert.Equal((byte)0x11, spy.LastPacketId);
        }

        [Fact]
        public void Dispatcher_MultiplePacketsInBuffer_AllDispatched()
        {
            var socket = NewNetClient();
            var world = new World();
            var handlers = new PacketHandlers();
            var spy = new HandlerSpy();
            handlers.Add(0x0B, spy.Handle);
            handlers.Add(0x2B, spy.Handle);

            var damage = PacketBuilder.Fixed(0x0B).U32BE(0x1).U16BE(10).ToBytes();
            var other = PacketBuilder.Fixed(0x2B).U8(0).ToBytes();

            var combined = new byte[damage.Length + other.Length];
            damage.CopyTo(combined, 0);
            other.CopyTo(combined, damage.Length);

            handlers.Append(combined, fromPlugins: true);
            var parsed = handlers.ParsePackets(socket, world, Span<byte>.Empty);

            Assert.Equal(2, parsed);
            Assert.Equal(2, spy.CallCount);
        }

        [Fact]
        public void Dispatcher_PartialPacket_StaysBufferedUntilComplete()
        {
            var socket = NewNetClient();
            var world = new World();
            var handlers = new PacketHandlers();
            var spy = new HandlerSpy();
            handlers.Add(0x0B, spy.Handle);

            var full = PacketBuilder.Fixed(0x0B).U32BE(0x1).U16BE(10).ToBytes();
            Assert.Equal(7, full.Length);

            // Feed first 4 bytes — not enough for a 7-byte packet
            handlers.Append(full.AsSpan(0, 4).ToArray(), fromPlugins: true);
            var firstPass = handlers.ParsePackets(socket, world, Span<byte>.Empty);

            Assert.Equal(0, firstPass);
            Assert.Equal(0, spy.CallCount);

            // Feed remaining 3 bytes — now complete
            handlers.Append(full.AsSpan(4, 3).ToArray(), fromPlugins: true);
            var secondPass = handlers.ParsePackets(socket, world, Span<byte>.Empty);

            Assert.Equal(1, secondPass);
            Assert.Equal(1, spy.CallCount);
        }
    }
}
