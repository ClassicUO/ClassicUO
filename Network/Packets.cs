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
                IsDynamic = PacketsTable.GetPacketLength(this[0]) < 0;
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

    public sealed class PSkillsRequest : PacketWriter
    {
        public PSkillsRequest(Serial serial) : base(0x34)
        {
            WriteUInt(0xEDEDEDED);
            WriteByte(5);
            WriteUInt(serial);
        }
    }

    public sealed class PSkillsStatusChangeRequest : PacketWriter
    {
        public PSkillsStatusChangeRequest(byte skill, bool state) : base(0x3A)
        {
            WriteUShort(skill);
            WriteBool(state);
        }
    }

    public sealed class PClickRequest : PacketWriter
    {
        public PClickRequest(Serial serial) : base(0x09)
        {
            WriteUInt(serial);
        }
    }

    public sealed class PDoubleClickRequest : PacketWriter
    {
        public PDoubleClickRequest(Serial serial) : base(0x06)
        {
            WriteUInt(serial);
        }
    }

    public sealed class PAttackRequest : PacketWriter
    {
        public PAttackRequest(Serial serial) : base(0x05)
        {
            WriteUInt(serial);
        }
    }

    public sealed class PClientVersion : PacketWriter
    {
        public PClientVersion(string version) : base(0xBD)
        {
            WriteASCII(string.Format("{0}.{1}.{2}.{3}", version[0], version[1], version[2], version[3]));
        }
    }

    public sealed class PASCIISpeechRequest : PacketWriter
    {
        public PASCIISpeechRequest(string text, MessageType type, MessageFont font, Hue hue) : base(0x03)
        {
            WriteByte((byte)type);
            WriteUShort(hue);
            WriteUShort((ushort)font);
            WriteASCII(text);
        }
    }

    public sealed class PUnicodeSpeechRequest : PacketWriter
    {
        public PUnicodeSpeechRequest(string text, MessageType type, MessageFont font, Hue hue, string lang) : base(0xAD)
        {
            WriteByte((byte)type);
            WriteUShort(hue);
            WriteUShort((ushort)font);
            WriteASCII(lang, 4);
            WriteUnicode(text);
        }
    }

    public sealed class PCastSpell : PacketWriter
    {
        public PCastSpell(int idx) : base(0xBF)
        {
            if (FileManager.ClientVersion >= ClientVersions.CV_60142)
            {
                WriteUShort(0x1C);
                WriteUShort(0x02);
                WriteUShort((ushort)idx);
            }
            else
            {
                this[0] = 0x12;
                this.IsDynamic = PacketsTable.GetPacketLength(this[0]) < 0; 
                WriteByte(0x56);
                WriteASCII(idx.ToString());
                // need a \0 ?
            }
        }
    }

    public sealed class PCastSpellFromBook : PacketWriter
    {
        public PCastSpellFromBook(int idx, Serial serial) : base(0x12)
        {
            WriteByte(0x27);
            WriteASCII(string.Format("{0} {1}", idx, serial));
        }
    }

    public sealed class PUseSkill : PacketWriter
    {
        public PUseSkill(int idx) : base(0x12)
        {
            WriteByte(0x24);
            WriteASCII(idx.ToString() + " 0");
        }
    }

    public sealed class POpenDoor : PacketWriter
    {
        public POpenDoor() : base(0x12)
        {
            WriteByte(0x58);
        }
    }

    public sealed class POpenSpellBook : PacketWriter
    {
        public POpenSpellBook(byte type) : base (0x12)
        {
            WriteByte(0x43);
            WriteByte(type);
        }
    }

    public sealed class PEmoteAction : PacketWriter
    {
        public PEmoteAction(string action) : base (0x12)
        {
            WriteByte(0xC7);
            WriteASCII(action.ToString());
        }
    }

    public sealed class PGumpResponse : PacketWriter
    {
        public PGumpResponse() : base (0xB1)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class PVirtueGumpReponse : PacketWriter
    {
        public PVirtueGumpReponse() : base (0xB1)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class PMenuResponse : PacketWriter
    {
        public PMenuResponse() : base (0x7D)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class PGrayMenuResponse : PacketWriter
    {
        public PGrayMenuResponse() : base (0x7D)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class PTradeResponse : PacketWriter
    {
        public PTradeResponse() : base (0x6F)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class PTextEntryDialogResponse : PacketWriter
    {
        public PTextEntryDialogResponse() : base (0xAC)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class PRenameRequest : PacketWriter
    {
        public PRenameRequest(Serial serial, string name) : base(0x75)
        {
            WriteUInt(serial);
            WriteASCII(name);
        }
    }

    public sealed class PTipRequest : PacketWriter
    {
        public PTipRequest(ushort id, byte flag) : base(0xA7)
        {
            WriteUShort(id);
            WriteByte(flag);
        }
    }

    /*public sealed class PASCIIPromptResponse : PacketWriter
    {
        public PASCIIPromptResponse(string text, int len, bool cancel) : base(0x)
    }

    public sealed class PUnicodePromptResponse : PacketWriter
    {
        public PUnicodePromptResponse(string text, int len, string lang, bool cancel) : base()
    }*/

    public sealed class PDyeDataResponse : PacketWriter
    {
        public PDyeDataResponse(Serial serial, Graphic graphic, Hue hue) : base (0x95)
        {
            WriteUInt(serial);
            WriteUShort(graphic);
            WriteUShort(hue);
        }
    }

    public sealed class PProfileRequest : PacketWriter
    {
        public PProfileRequest(Serial serial) : base (0xB8)
        {
            WriteByte(0);
            WriteUInt(serial);
        }
    }

    public sealed class PProfileUpdate : PacketWriter
    {
        public PProfileUpdate(Serial serial, string text, int len) : base (0xB8)
        {
            WriteByte(1);
            WriteUInt(serial);
            WriteUShort(0x01);
            WriteUShort((ushort)len);
            WriteUnicode(text, len);
        }
    }

    public sealed class PCloseStatusBarGump : PacketWriter
    {
        public PCloseStatusBarGump(Serial serial) : base (0xBF)
        {
            WriteUShort(0x0C);
            WriteUInt(serial);
        }
    }

    public sealed class PPartyInviteRequest : PacketWriter
    {
        public PPartyInviteRequest() : base(0xBF)
        {
            WriteUShort(0x06);
            WriteByte(1);
            WriteUInt(0);
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
