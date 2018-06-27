using ClassicUO.Assets;
using ClassicUO.Game;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Network
{
    public sealed class PCreateCharacter : PacketWriter
    {
        public PCreateCharacter(string name) : base(0x00)
        {
            int skillcount = 3;

            if (FileManager.ClientVersion >= ClientVersions.CV_70160)
            {
                skillcount++;
                this[0] = 0xF8;
            }

            WriteUInt(0xEDEDEDED);
            WriteUShort(0xFFFF);
            WriteUShort(0xFFFF);
            WriteByte(0x00);
            WriteASCII(name, 30);
            WriteUShort(0x00);

            uint clientflag = 0;

            /*IFOR(i, 0, g_CharacterList.ClientFlag)
                clientFlag |= (1 << i);*/

            WriteUInt(clientflag);
            WriteUInt(0x01);
            WriteUInt(0x0);

            // to terminate...
        }
    }

    public sealed class PPickUpRequest : PacketWriter
    {
        public PPickUpRequest(Serial serial, ushort count) : base(0x07)
        {
            WriteUInt(serial);
            WriteUShort(count);
        }
    }

    public sealed class PDropRequestOld : PacketWriter
    {
        public PDropRequestOld(Serial serial, Position position, Serial container) : base(0x08)
        {
            WriteUInt(serial);
            WriteUShort(position.X);
            WriteUShort(position.Y);
            WriteSByte(position.Z);
            WriteUInt(container);
        }
    }

    public sealed class PDropRequestNew : PacketWriter
    {
        public PDropRequestNew(Serial serial, Position position, byte slot, Serial container) : base(0x08)
        {
            WriteUInt(serial);
            WriteUShort(position.X);
            WriteUShort(position.Y);
            WriteSByte(position.Z);
            WriteByte(slot);
            WriteUInt(container);
        }
    }

    public sealed class PEquipRequest : PacketWriter
    {
        public PEquipRequest(Serial serial, Layer layer, Serial container) : base(0x13)
        {
            WriteUInt(serial);
            WriteByte((byte)layer);
            WriteUInt(container);
        }
    }

    public sealed class PChangeWarMode : PacketWriter
    {
        public PChangeWarMode(bool state) : base(0x72)
        {
            WriteBool(state);
            WriteUShort(0x0032);
        }
    }

    public sealed class PHelpRequest : PacketWriter
    {
        public PHelpRequest() : base(0x9B)
        {

        }
    }
    
    public sealed class PStatusRequest : PacketWriter
    {
        public PStatusRequest(Serial serial) : base(0x34)
        {
            WriteUInt(0xEDEDEDED);
            WriteByte(4);
            WriteUInt(serial);
        }
    }




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
