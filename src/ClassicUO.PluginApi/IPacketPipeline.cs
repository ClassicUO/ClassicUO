// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.PluginApi;

/// <summary>
/// Subscribes to the server-to-client and client-to-server packet streams
/// and lets a plugin inject synthesized packets in either direction.
/// </summary>
/// <remarks>
/// Packet handlers receive a <see cref="ReadOnlySpan{Byte}"/> over the live
/// network buffer. The span is valid only for the duration of the handler
/// call; do not store the span or any pointer derived from it. If you need
/// to retain bytes, copy them with <c>span.ToArray()</c>.
/// </remarks>
public interface IPacketPipeline
{
    /// <summary>
    /// Fires for every server-to-client packet before the client processes
    /// it. Set <c>block=true</c> in the handler to prevent the client from
    /// seeing the packet at all.
    /// </summary>
    event PacketHandler? Incoming;

    /// <summary>
    /// Fires for every client-to-server packet before it leaves the socket.
    /// Set <c>block=true</c> to prevent the server from receiving it.
    /// </summary>
    event PacketHandler? Outgoing;

    /// <summary>
    /// Inject a packet into the client as if it arrived from the server.
    /// Other plugins do not observe injected packets via <see cref="Incoming"/>.
    /// </summary>
    void SendToClient(ReadOnlySpan<byte> data);

    /// <summary>
    /// Inject a packet into the network as if it originated from the client.
    /// Other plugins do not observe injected packets via <see cref="Outgoing"/>.
    /// </summary>
    void SendToServer(ReadOnlySpan<byte> data);

    /// <summary>
    /// Returns the fixed length of a packet by id, or <c>-1</c> if the
    /// packet uses a length-prefixed (dynamic) layout, or <c>0</c> if the
    /// id is unknown.
    /// </summary>
    short GetPacketLength(byte packetId);
}

/// <summary>
/// Inspects a packet flowing through the pipeline and optionally blocks it
/// from being processed downstream.
/// </summary>
/// <param name="packet">The packet bytes including the leading id byte.</param>
/// <param name="block">
/// Set to <c>true</c> to prevent further processing. If multiple plugins
/// subscribe, the packet is blocked if any handler sets this to <c>true</c>.
/// </param>
public delegate void PacketHandler(ReadOnlySpan<byte> packet, ref bool block);
