using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Network
{
    public sealed class PSeed : PacketWriter
    {
        public PSeed(byte[] version) : base(0xEF)
        {
            const uint SEED = 0x1337BEEF;

            WriteUInt(SEED);

            for (int i = 0; i < 4; i++)
                WriteUInt(version[i]);
        }
    }

    public sealed class PLoginAccount : PacketWriter
    {
        public PLoginAccount(string username, string password) : base (0x80)
        {
            WriteASCII(username, 30);
            WriteASCII(password, 30);
            WriteByte(0x5D);
        }
    }

    public sealed class PChooseServer : PacketWriter
    {
        public PChooseServer(ushort idx) : base(0xA0)
        {
            WriteUShort(idx);
        }
    }

    public sealed class PGameLoginAccount : PacketWriter
    {
        public PGameLoginAccount(uint auth, string username, string password) : base(0x91)
        {
            WriteUInt(auth);
            WriteASCII(username, 30);
            WriteASCII(password, 30);
        }
    }

    public sealed class PLoginCharacter : PacketWriter
    {
        public PLoginCharacter(string name, uint index, uint ip) : base (0x5D)
        {
            WriteUInt(0xedededed);
            WriteASCII(name, 30);
            WriteUShort(0);
            WriteUInt(0x1F);
            WriteUInt(1);
            WriteUInt(0x18);
            WriteASCII("", 0x10);
            WriteUInt(index);
            WriteUInt(ip);
        }
    }

    public sealed class PPingPacket : PacketWriter
    {
        public PPingPacket() : base(0x73)
        {
            WriteByte(0);
        }
    }

    public sealed class PClientVersion : PacketWriter
    {
        public PClientVersion(byte[] version) : base(0xBD)
        {
            WriteASCII(string.Format("{0}.{1}.{2}.{3}", version[0], version[1], version[2], version[3]));
        }
    }

    public sealed class PNegotiateFeatures : PacketWriter
    {
        public PNegotiateFeatures() : base (0xF0)
        {
            WriteByte(0xFF);
        }
    }
}
