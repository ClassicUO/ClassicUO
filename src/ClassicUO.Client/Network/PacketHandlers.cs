// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Sdk.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.Services;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Sdk.IO;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Sdk;
using ClassicUO.Platforms;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Sdk.IO;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Sdk;
using ClassicUO.Platforms;
using ClassicUO.Game.Services;

namespace ClassicUO.Network
{
    sealed class PacketHandlers
    {
        private readonly OnPacketBufferReader[] _handlers = new OnPacketBufferReader[0x100];

        public void Add(byte id, OnPacketBufferReader handler) => _handlers[id] = handler;

        private byte[] _readingBuffer = new byte[4096];
        private readonly PacketLogger _packetLogger = new PacketLogger();
        private readonly CircularBuffer _buffer = new CircularBuffer();
        private readonly CircularBuffer _pluginsBuffer = new CircularBuffer();

        public int ParsePackets(NetClient socket, World world, Span<byte> data)
        {
            Append(data, false);

            return ParsePackets(socket, world, _buffer, true) + ParsePackets(socket, world, _pluginsBuffer, false);
        }

        private int ParsePackets(NetClient socket, World world, CircularBuffer stream, bool allowPlugins)
        {
            var packetsCount = 0;

            lock (stream)
            {
                ref var packetBuffer = ref _readingBuffer;

                while (stream.Length > 0)
                {
                    if (
                        !GetPacketInfo(
                            socket,
                            stream,
                            stream.Length,
                            out var packetID,
                            out int offset,
                            out int packetlength
                        )
                    )
                    {
                        Log.Warn(
                            $"Invalid ID: {packetID:X2} | off: {offset} | len: {packetlength} | stream.pos: {stream.Length}"
                        );

                        break;
                    }

                    if (stream.Length < packetlength)
                    {
                        Log.Warn(
                            $"need more data ID: {packetID:X2} | off: {offset} | len: {packetlength} | stream.pos: {stream.Length}"
                        );

                        // need more data
                        break;
                    }

                    while (packetlength > packetBuffer.Length)
                    {
                        Array.Resize(ref packetBuffer, packetBuffer.Length * 2);
                    }

                    _ = stream.Dequeue(packetBuffer, 0, packetlength);

                    PacketLogger.Default?.Log(packetBuffer.AsSpan(0, packetlength), false);

                    // TODO: the pluging function should allow Span<byte> or unsafe type only.
                    // The current one is a bad style decision.
                    // It will be fixed once the new plugin system is done.
                    if (!allowPlugins || Plugin.ProcessRecvPacket(packetBuffer, ref packetlength))
                    {
                        AnalyzePacket(world, packetBuffer.AsSpan(0, packetlength), offset);

                        ++packetsCount;
                    }
                }
            }

            return packetsCount;
        }

        public void Append(Span<byte> data, bool fromPlugins)
        {
            if (data.IsEmpty)
                return;

            (fromPlugins ? _pluginsBuffer : _buffer).Enqueue(data);
        }

        private void AnalyzePacket(World world, ReadOnlySpan<byte> data, int offset)
        {
            if (data.IsEmpty)
                return;

            var bufferReader = _handlers[data[0]];

            if (bufferReader != null)
            {
                var buffer = new StackDataReader(data);
                buffer.Seek(offset);

                bufferReader(world, ref buffer);
            }
        }

        private static bool GetPacketInfo(
            NetClient socket,
            CircularBuffer buffer,
            int bufferLen,
            out byte packetID,
            out int packetOffset,
            out int packetLen
        )
        {
            if (buffer == null || bufferLen <= 0)
            {
                packetID = 0xFF;
                packetLen = 0;
                packetOffset = 0;

                return false;
            }

            packetLen = socket.PacketsTable.GetPacketLength(packetID = buffer[0]);
            packetOffset = 1;

            if (packetLen == -1)
            {
                if (bufferLen < 3)
                {
                    return false;
                }

                var b0 = buffer[1];
                var b1 = buffer[2];

                packetLen = (b0 << 8) | b1;
                packetOffset = 3;
            }

            return true;
        }
    }
}
