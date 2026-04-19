// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;

namespace ClassicUO.UnitTests.Network
{
    internal sealed class PacketBuilder
    {
        private readonly List<byte> _bytes = new();
        private readonly bool _variable;

        private PacketBuilder(byte id, bool variable)
        {
            _variable = variable;
            _bytes.Add(id);
            if (variable)
            {
                _bytes.Add(0);
                _bytes.Add(0);
            }
        }

        public static PacketBuilder Fixed(byte id) => new(id, variable: false);
        public static PacketBuilder Variable(byte id) => new(id, variable: true);

        public PacketBuilder U8(byte v)
        {
            _bytes.Add(v);
            return this;
        }

        public PacketBuilder U16BE(ushort v)
        {
            _bytes.Add((byte)(v >> 8));
            _bytes.Add((byte)(v & 0xFF));
            return this;
        }

        public PacketBuilder U32BE(uint v)
        {
            _bytes.Add((byte)(v >> 24));
            _bytes.Add((byte)(v >> 16));
            _bytes.Add((byte)(v >> 8));
            _bytes.Add((byte)(v & 0xFF));
            return this;
        }

        public PacketBuilder Bytes(ReadOnlySpan<byte> data)
        {
            foreach (var b in data) _bytes.Add(b);
            return this;
        }

        public PacketBuilder AsciiFixed(string value, int totalLength)
        {
            for (int i = 0; i < totalLength; i++)
                _bytes.Add(i < value.Length ? (byte)value[i] : (byte)0);
            return this;
        }

        public byte[] ToBytes()
        {
            if (_variable)
            {
                var len = _bytes.Count;
                _bytes[1] = (byte)((len >> 8) & 0xFF);
                _bytes[2] = (byte)(len & 0xFF);
            }

            return _bytes.ToArray();
        }
    }
}
