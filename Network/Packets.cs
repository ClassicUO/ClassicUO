using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClassicUO.AssetsLoader;
using ClassicUO.Game;
using ClassicUO.Game.WorldObjects;

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
        public PClientVersion(byte[] version) : base(0xBD)
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

    public sealed class PPartyRemoveRequest : PacketWriter
    {
        public PPartyRemoveRequest(Serial serial) : base (0xBF)
        {
            WriteUShort(0x06);
            WriteByte(2);
            WriteUInt(serial);
        }
    }

    public sealed class PPartyChangeLootTypeRequest : PacketWriter
    {
        public PPartyChangeLootTypeRequest(bool type) : base (0xBF)
        {
            WriteUShort(0x06);
            WriteByte(0x06);
            WriteBool(type);
        }
    }

    public sealed class PPartyAccept : PacketWriter
    {
        public PPartyAccept(Serial serial) : base (0xBF)
        {
            WriteUShort(0x06);
            WriteByte(0x08);
            WriteUInt(serial);
        }
    }

    public sealed class PPartyDecline : PacketWriter
    {
        public PPartyDecline(Serial serial) : base(0xBF)
        {
            WriteUShort(0x06);
            WriteByte(0x09);
            WriteUInt(serial);
        }
    }

    public sealed class PPartyMessage : PacketWriter
    {
        public PPartyMessage(string text, Serial serial) : base(0xBF)
        {
            WriteUShort(0x06);
            if (serial.IsValid)
            {
                WriteByte(0x03);
                WriteUInt(serial);
            }
            else
                WriteByte(0x04);

            WriteUnicode(text);
        }
    }

    public sealed class PGameWindowSize : PacketWriter
    {
        public PGameWindowSize(uint w, uint h) : base(0xBF)
        {
            WriteUShort(0x05);
            WriteUInt(w);
            WriteUInt(h);
        }
    }

    public sealed class PBulletinBoardRequestMessage : PacketWriter
    {
        public PBulletinBoardRequestMessage(Serial serial, Serial msgserial) : base (0x71)
        {
            WriteByte(0x03);
            WriteUInt(serial);
            WriteUInt(msgserial);
        }
    }

    public sealed class PBulletinBoardRequestMessageSummary : PacketWriter
    {
        public PBulletinBoardRequestMessageSummary(Serial serial, Serial msgserial) : base(0x71)
        {
            WriteByte(0x04);
            WriteUInt(serial);
            WriteUInt(msgserial);
        }
    }

    public sealed class PBulletinBoardPostMessage : PacketWriter
    {
        public PBulletinBoardPostMessage(Serial serial, Serial msgserial, string subject, string message) : base(0x71)
        {
            WriteByte(0x05);
            WriteUInt(serial);
            WriteUInt(msgserial);
            WriteByte((byte)(subject.Length + 1));
            WriteASCII(subject);

            string[] lines = message.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < lines.Length; i++)
            {
                WriteByte((byte)lines[i].Length);
                WriteASCII(lines[i]);
            }
        }
    }

    public sealed class PBulletinBoardRemoveMessage : PacketWriter
    {
        public PBulletinBoardRemoveMessage(Serial serial, Serial msgserial) : base(0x71)
        {
            WriteByte(0x06);
            WriteUInt(serial);
            WriteUInt(msgserial);
        }
    }

    public sealed class PAssistVersion : PacketWriter
    {
        public PAssistVersion(byte[] clientversion, uint version) : base(0xBE)
        {
            WriteUInt(version);
            WriteASCII(string.Format("{0}.{1}.{2}.{3}", clientversion[0], clientversion[1], clientversion[2], clientversion[3]));
        }
    }

    public sealed class PRazorAnswer : PacketWriter
    {
        public PRazorAnswer() : base( 0xF0)
        {
            WriteByte(0x04);
            WriteByte(0xFF);
        }
    }
    
    public sealed class PLanguage : PacketWriter
    {
        public PLanguage(string lang) : base(0xBF)
        {
            WriteUShort(0x0B);
            WriteASCII(lang);
        }
    }

    public sealed class PClientType : PacketWriter
    {
        public PClientType() : base (0xBF)
        {
            WriteUShort(0x0F);
            WriteByte(0x0A);

            uint clientFlag = 0;

            /*IFOR(i, 0, g_CharacterList.ClientFlag)
                clientFlag |= (1 << i);*/
            WriteUInt(clientFlag);
        }
    }

    public sealed class PRequestPopupMenu : PacketWriter
    {
        public PRequestPopupMenu(Serial serial) : base(0xBF)
        {
            WriteUShort(0x13);
            WriteUInt(serial);
        }
    }

    public sealed class PPopupMenuSelection : PacketWriter
    {
        public PPopupMenuSelection(Serial serial, Graphic menuid) : base(0xBF)
        {
            WriteUShort(0x15);
            WriteUInt(serial);
            WriteUShort(menuid);
        }
    }

}
