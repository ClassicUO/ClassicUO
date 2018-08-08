using System;
using System.Collections.Generic;
using ClassicUO.AssetsLoader;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Network
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

    public sealed class PFirstLogin : PacketWriter
    {
        public PFirstLogin(in string account, in string password) : base(0x80)
        {
            WriteASCII(account, 30);
            WriteASCII(password, 30);
            WriteByte(0xFF);
        }
    }

    public sealed class PSelectServer : PacketWriter
    {
        public PSelectServer(in byte index) : base(0xA0)
        {
            WriteByte(0);
            WriteByte(index);
        }
    }

    public sealed class PSecondLogin : PacketWriter
    {
        public PSecondLogin(in string account, in string password, in uint seed) : base(0x91)
        {
            WriteUInt(seed);
            WriteASCII(account, 30);
            WriteASCII(password, 30);
        }
    }

    public sealed class PCreateCharacter : PacketWriter
    {
        public PCreateCharacter(in string name) : base(0x00)
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

    public sealed class PDeleteCharacter : PacketWriter
    {
        public PDeleteCharacter(in byte index, in uint ipclient) : base(0x83)
        {
            Skip(30);
            WriteUInt(index);
            WriteUInt(ipclient);
        }
    }

    public sealed class PSelectCharacter : PacketWriter
    {
        public PSelectCharacter(in byte index, in string name, in uint ipclient) : base(0x5D)
        {
            WriteUInt(0xEDEDEDED);
            WriteASCII(name, 30);
            Skip(2);

            uint clientflag = 0;
            /* IFOR (i, 0, g_CharacterList.ClientFlag)
            clientFlag |= (1 << i);*/
            WriteUInt(clientflag);
            Skip(24);
            WriteUInt(index);
            WriteUInt(ipclient);
        }
    }

    public sealed class PPickUpRequest : PacketWriter
    {
        public PPickUpRequest(in Serial serial, in ushort count) : base(0x07)
        {
            WriteUInt(serial);
            WriteUShort(count);
        }
    }

    public sealed class PDropRequestOld : PacketWriter
    {
        public PDropRequestOld(in Serial serial, in Position position, in Serial container) : base(0x08)
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
        public PDropRequestNew(in Serial serial, in Position position, in byte slot, in Serial container) : base(0x08)
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
        public PEquipRequest(in Serial serial, in Layer layer, in Serial container) : base(0x13)
        {
            WriteUInt(serial);
            WriteByte((byte) layer);
            WriteUInt(container);
        }
    }

    public sealed class PChangeWarMode : PacketWriter
    {
        public PChangeWarMode(in bool state) : base(0x72)
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
        public PStatusRequest(in Serial serial) : base(0x34)
        {
            WriteUInt(0xEDEDEDED);
            WriteByte(4);
            WriteUInt(serial);
        }
    }

    public sealed class PSkillsRequest : PacketWriter
    {
        public PSkillsRequest(in Serial serial) : base(0x34)
        {
            WriteUInt(0xEDEDEDED);
            WriteByte(5);
            WriteUInt(serial);
        }
    }

    public sealed class PSkillsStatusChangeRequest : PacketWriter
    {
        public PSkillsStatusChangeRequest(in byte skill, in bool state) : base(0x3A)
        {
            WriteUShort(skill);
            WriteBool(state);
        }
    }

    public sealed class PClickRequest : PacketWriter
    {
        public PClickRequest(in Serial serial) : base(0x09)
        {
            WriteUInt(serial);
        }
    }

    public sealed class PDoubleClickRequest : PacketWriter
    {
        public PDoubleClickRequest(in Serial serial) : base(0x06)
        {
            WriteUInt(serial);
        }
    }

    public sealed class PAttackRequest : PacketWriter
    {
        public PAttackRequest(in Serial serial) : base(0x05)
        {
            WriteUInt(serial);
        }
    }

    public sealed class PClientVersion : PacketWriter
    {
        public PClientVersion(in byte[] version) : base(0xBD)
        {
            WriteASCII(string.Format("{0}.{1}.{2}.{3}", version[0], version[1], version[2], version[3]));
        }
    }

    public sealed class PASCIISpeechRequest : PacketWriter
    {
        public PASCIISpeechRequest(in string text, in MessageType type, in MessageFont font, in Hue hue) : base(0x03)
        {
            WriteByte((byte) type);
            WriteUShort(hue);
            WriteUShort((ushort) font);
            WriteASCII(text);
        }
    }

    public sealed class PUnicodeSpeechRequest : PacketWriter
    {
        public PUnicodeSpeechRequest(in string text, in MessageType type, in MessageFont font, in Hue hue, in string lang) : base(0xAD)
        {
            WriteByte((byte) type);
            WriteUShort(hue);
            WriteUShort((ushort) font);
            WriteASCII(lang, 4);
            WriteUnicode(text);
        }
    }

    public sealed class PCastSpell : PacketWriter
    {
        public PCastSpell(in int idx) : base(0xBF)
        {
            if (FileManager.ClientVersion >= ClientVersions.CV_60142)
            {
                WriteUShort(0x1C);
                WriteUShort(0x02);
                WriteUShort((ushort) idx);
            }
            else
            {
                this[0] = 0x12;
                IsDynamic = PacketsTable.GetPacketLength(this[0]) < 0;
                WriteByte(0x56);
                WriteASCII(idx.ToString());
                // need a \0 ?
            }
        }
    }

    public sealed class PCastSpellFromBook : PacketWriter
    {
        public PCastSpellFromBook(in int idx, in Serial serial) : base(0x12)
        {
            WriteByte(0x27);
            WriteASCII(string.Format("{0} {1}", idx, serial));
        }
    }

    public sealed class PUseSkill : PacketWriter
    {
        public PUseSkill(in int idx) : base(0x12)
        {
            WriteByte(0x24);
            WriteASCII(idx + " 0");
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
        public POpenSpellBook(in byte type) : base(0x12)
        {
            WriteByte(0x43);
            WriteByte(type);
        }
    }

    public sealed class PEmoteAction : PacketWriter
    {
        public PEmoteAction(in string action) : base(0x12)
        {
            WriteByte(0xC7);
            WriteASCII(action);
        }
    }

    public sealed class PGumpResponse : PacketWriter
    {
        public PGumpResponse() : base(0xB1)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class PVirtueGumpReponse : PacketWriter
    {
        public PVirtueGumpReponse() : base(0xB1)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class PMenuResponse : PacketWriter
    {
        public PMenuResponse() : base(0x7D)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class PGrayMenuResponse : PacketWriter
    {
        public PGrayMenuResponse() : base(0x7D)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class PTradeResponse : PacketWriter
    {
        public PTradeResponse() : base(0x6F)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class PTextEntryDialogResponse : PacketWriter
    {
        public PTextEntryDialogResponse() : base(0xAC)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class PRenameRequest : PacketWriter
    {
        public PRenameRequest(in Serial serial, in string name) : base(0x75)
        {
            WriteUInt(serial);
            WriteASCII(name);
        }
    }

    public sealed class PTipRequest : PacketWriter
    {
        public PTipRequest(in ushort id, in byte flag) : base(0xA7)
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
        public PDyeDataResponse(in Serial serial, in Graphic graphic, in Hue hue) : base(0x95)
        {
            WriteUInt(serial);
            WriteUShort(graphic);
            WriteUShort(hue);
        }
    }

    public sealed class PProfileRequest : PacketWriter
    {
        public PProfileRequest(in Serial serial) : base(0xB8)
        {
            WriteByte(0);
            WriteUInt(serial);
        }
    }

    public sealed class PProfileUpdate : PacketWriter
    {
        public PProfileUpdate(in Serial serial, in string text, in int len) : base(0xB8)
        {
            WriteByte(1);
            WriteUInt(serial);
            WriteUShort(0x01);
            WriteUShort((ushort) len);
            WriteUnicode(text, len);
        }
    }

    public sealed class PCloseStatusBarGump : PacketWriter
    {
        public PCloseStatusBarGump(in Serial serial) : base(0xBF)
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
        public PPartyRemoveRequest(in Serial serial) : base(0xBF)
        {
            WriteUShort(0x06);
            WriteByte(2);
            WriteUInt(serial);
        }
    }

    public sealed class PPartyChangeLootTypeRequest : PacketWriter
    {
        public PPartyChangeLootTypeRequest(in bool type) : base(0xBF)
        {
            WriteUShort(0x06);
            WriteByte(0x06);
            WriteBool(type);
        }
    }

    public sealed class PPartyAccept : PacketWriter
    {
        public PPartyAccept(in Serial serial) : base(0xBF)
        {
            WriteUShort(0x06);
            WriteByte(0x08);
            WriteUInt(serial);
        }
    }

    public sealed class PPartyDecline : PacketWriter
    {
        public PPartyDecline(in Serial serial) : base(0xBF)
        {
            WriteUShort(0x06);
            WriteByte(0x09);
            WriteUInt(serial);
        }
    }

    public sealed class PPartyMessage : PacketWriter
    {
        public PPartyMessage(in string text, in Serial serial) : base(0xBF)
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
        public PGameWindowSize(in uint w, in uint h) : base(0xBF)
        {
            WriteUShort(0x05);
            WriteUInt(w);
            WriteUInt(h);
        }
    }

    public sealed class PBulletinBoardRequestMessage : PacketWriter
    {
        public PBulletinBoardRequestMessage(in Serial serial, in Serial msgserial) : base(0x71)
        {
            WriteByte(0x03);
            WriteUInt(serial);
            WriteUInt(msgserial);
        }
    }

    public sealed class PBulletinBoardRequestMessageSummary : PacketWriter
    {
        public PBulletinBoardRequestMessageSummary(in Serial serial, in Serial msgserial) : base(0x71)
        {
            WriteByte(0x04);
            WriteUInt(serial);
            WriteUInt(msgserial);
        }
    }

    public sealed class PBulletinBoardPostMessage : PacketWriter
    {
        public PBulletinBoardPostMessage(in Serial serial, in Serial msgserial, in string subject, in string message) : base(0x71)
        {
            WriteByte(0x05);
            WriteUInt(serial);
            WriteUInt(msgserial);
            WriteByte((byte) (subject.Length + 1));
            WriteASCII(subject);

            string[] lines = message.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < lines.Length; i++)
            {
                WriteByte((byte) lines[i].Length);
                WriteASCII(lines[i]);
            }
        }
    }

    public sealed class PBulletinBoardRemoveMessage : PacketWriter
    {
        public PBulletinBoardRemoveMessage(in Serial serial, in Serial msgserial) : base(0x71)
        {
            WriteByte(0x06);
            WriteUInt(serial);
            WriteUInt(msgserial);
        }
    }

    public sealed class PAssistVersion : PacketWriter
    {
        public PAssistVersion(in byte[] clientversion, in uint version) : base(0xBE)
        {
            WriteUInt(version);
            WriteASCII(string.Format("{0}.{1}.{2}.{3}", clientversion[0], clientversion[1], clientversion[2], clientversion[3]));
        }
    }

    public sealed class PRazorAnswer : PacketWriter
    {
        public PRazorAnswer() : base(0xF0)
        {
            WriteByte(0xFF);
        }
    }

    public sealed class PLanguage : PacketWriter
    {
        public PLanguage(in string lang) : base(0xBF)
        {
            WriteUShort(0x0B);
            WriteASCII(lang);
        }
    }

    public sealed class PClientType : PacketWriter
    {
        public PClientType() : base(0xBF)
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
        public PRequestPopupMenu(in Serial serial) : base(0xBF)
        {
            WriteUShort(0x13);
            WriteUInt(serial);
        }
    }

    public sealed class PPopupMenuSelection : PacketWriter
    {
        public PPopupMenuSelection(in Serial serial, in Graphic menuid) : base(0xBF)
        {
            WriteUShort(0x15);
            WriteUInt(serial);
            WriteUShort(menuid);
        }
    }

    public sealed class POpenChat : PacketWriter
    {
        public POpenChat(in string name) : base(0xB5)
        {
            WriteUnicode(name, 30);
        }
    }

    public sealed class PMapMessage : PacketWriter
    {
        public PMapMessage(in Serial serial, in byte action, in byte pin, in ushort x, in ushort y) : base(0x56)
        {
            WriteUInt(serial);
            WriteByte(action);
            WriteByte(pin);
            WriteUShort(x);
            WriteUShort(y);
        }
    }

    public sealed class PGuildMenuRequest : PacketWriter
    {
        public PGuildMenuRequest() : base(0xD7)
        {
            WriteUInt(World.Player);
            WriteUShort(0x28);
            WriteByte(0x0A);
        }
    }

    public sealed class PQuestMenuRequest : PacketWriter
    {
        public PQuestMenuRequest() : base(0xD7)
        {
            WriteUInt(World.Player);
            WriteUShort(0x32);
            WriteByte(0x0A);
        }
    }

    public sealed class PEquipLastWeapon : PacketWriter
    {
        public PEquipLastWeapon() : base(0xD7)
        {
            WriteUInt(World.Player);
            WriteUShort(0x1E);
            WriteByte(0x0A);
        }
    }

    public sealed class PVirtueRequest : PacketWriter
    {
        public PVirtueRequest(in uint buttonID) : base(0xB1)
        {
            WriteUInt(World.Player);
            WriteUInt(0x000001CD);
            WriteUInt(buttonID);
            WriteUInt(0x00000001);
            WriteUInt(World.Player);
        }
    }

    public sealed class PInvokeVirtueRequest : PacketWriter
    {
        public PInvokeVirtueRequest(in byte id) : base(0x12)
        {
            WriteByte(0xF4);
            WriteByte(id);
            WriteByte(0);
        }
    }

    public sealed class PMegaClilocRequestOld : PacketWriter
    {
        public PMegaClilocRequestOld(in Serial serial) : base(0xBF)
        {
            WriteUShort(0x10);
            WriteUInt(serial);
        }
    }

    public sealed class PMegaClilocRequest : PacketWriter
    {
        public PMegaClilocRequest(in List<Serial> list) : base(0xD6)
        {
            for (int i = 0; i < list.Count && i < 50; i++)
                WriteUInt(list[i]);
        }
    }

    public sealed class PChangeStatLockStateRequest : PacketWriter
    {
        public PChangeStatLockStateRequest(in byte stat, in SkillLock state) : base(0xBF)
        {
            WriteUShort(0x1A);
            WriteByte(stat);
            WriteByte((byte) state);
        }
    }

    /* public sealed class PBookPageData : PacketWriter
     {
         public PBookPageData()
         {
 
         }
     }*/

    public sealed class PBookPageDataRequest : PacketWriter
    {
        public PBookPageDataRequest(in Serial serial, in ushort page) : base(0x66)
        {
            WriteUInt(serial);
            WriteUShort(1);
            WriteUShort(page);
            WriteUShort(0xFFFF);
        }
    }

    /*public sealed class PBuyRequest : PacketWriter
    {
        public PBuyRequest() : base()
        {

        }
    }

    public sealed class PSellRequest : PacketWriter
    {

    }*/

    public sealed class PUseCombatAbility : PacketWriter
    {
        public PUseCombatAbility(in byte idx) : base(0xD7)
        {
            WriteUInt(World.Player);
            WriteUShort(0x19);
            WriteUInt(0);
            WriteByte(idx);
            WriteByte(0x0A);
        }
    }

    public sealed class PTargetSelectedObject : PacketWriter
    {
        public PTargetSelectedObject(in Serial useObjSerial, in Serial targObjSerial) : base(0xBF)
        {
            WriteUShort(0x2C);
            WriteUInt(useObjSerial);
            WriteUInt(targObjSerial);
        }
    }

    public sealed class PToggleGargoyleFlying : PacketWriter
    {
        public PToggleGargoyleFlying() : base(0xBF)
        {
            WriteUShort(0x32);
            WriteUShort(0x01);
            WriteUInt(0);
        }
    }

    public sealed class PCustomHouseDataRequest : PacketWriter
    {
        public PCustomHouseDataRequest(in Serial serial) : base(0xBF)
        {
            WriteUShort(0x1E);
            WriteUInt(serial);
        }
    }

    public sealed class PStunRequest : PacketWriter
    {
        public PStunRequest() : base(0xBF)
        {
            WriteUShort(0x09);
        }
    }

    public sealed class PDisarmRequest : PacketWriter
    {
        public PDisarmRequest() : base(0xBF)
        {
            WriteUShort(0x0A);
        }
    }

    public sealed class PResend : PacketWriter
    {
        public PResend() : base(0x22)
        {
        }
    }

    public sealed class PWalkRequest : PacketWriter
    {
        public PWalkRequest(in Direction direction, in byte seq) : base(0x02)
        {
            WriteByte((byte) direction);
            WriteByte(seq);
            WriteUInt(0);
        }
    }

    public sealed class PCustomHouseBackup : PacketWriter
    {
        public PCustomHouseBackup() : base(0xD7)
        {
            WriteUInt(World.Player);
            WriteUShort(0x02);
            WriteByte(0x0A);
        }
    }

    public sealed class PCustomHouseRestore : PacketWriter
    {
        public PCustomHouseRestore() : base(0xD7)
        {
            WriteUInt(World.Player);
            WriteUShort(0x03);
            WriteByte(0x0A);
        }
    }

    public sealed class PCustomHouseCommit : PacketWriter
    {
        public PCustomHouseCommit() : base(0xD7)
        {
            WriteUInt(World.Player);
            WriteUShort(0x04);
            WriteByte(0x0A);
        }
    }

    public sealed class PCustomHouseBuildingExit : PacketWriter
    {
        public PCustomHouseBuildingExit() : base(0xD7)
        {
            WriteUInt(World.Player);
            WriteUShort(0x0C);
            WriteByte(0x0A);
        }
    }

    public sealed class PCustomHouseGoToFloor : PacketWriter
    {
        public PCustomHouseGoToFloor(in byte floor) : base(0xD7)
        {
            WriteUInt(World.Player);
            WriteUShort(0x12);
            WriteUInt(0);
            WriteByte(floor);
            WriteByte(0x0A);
        }
    }

    public sealed class PCustomHouseSync : PacketWriter
    {
        public PCustomHouseSync() : base(0xD7)
        {
            WriteUInt(World.Player);
            WriteUShort(0x0E);
            WriteByte(0x0A);
        }
    }

    public sealed class PCustomHouseClear : PacketWriter
    {
        public PCustomHouseClear() : base(0xD7)
        {
            WriteUInt(World.Player);
            WriteUShort(0x10);
            WriteByte(0x0A);
        }
    }

    public sealed class PCustomHouseRevert : PacketWriter
    {
        public PCustomHouseRevert() : base(0xD7)
        {
            WriteUInt(World.Player);
            WriteUShort(0x1A);
            WriteByte(0x0A);
        }
    }

    public sealed class PCustomHouseResponse : PacketWriter
    {
        public PCustomHouseResponse() : base(0xD7)
        {
            WriteUInt(World.Player);
            WriteUShort(0x0A);
            WriteByte(0x0A);
        }
    }

    public sealed class PCustomHouseAddItem : PacketWriter
    {
        public PCustomHouseAddItem(in Graphic graphic, in uint x, in uint y) : base(0xD7)
        {
            WriteUInt(World.Player);
            WriteUShort(0x06);
            WriteByte(0);
            WriteUInt(graphic);
            WriteByte(0);
            WriteUInt(x);
            WriteByte(0);
            WriteUInt(y);
            WriteByte(0x0A);
        }
    }

    public sealed class PCustomHouseDeleteItem : PacketWriter
    {
        public PCustomHouseDeleteItem(in Graphic graphic, in uint x, in uint y, in uint z) : base(0xD7)
        {
            WriteUInt(World.Player);
            WriteUShort(0x05);
            WriteByte(0);
            WriteUInt(graphic);
            WriteByte(0);
            WriteUInt(x);
            WriteByte(0);
            WriteUInt(y);
            WriteByte(0);
            WriteUInt(z);
            WriteByte(0x0A);
        }
    }

    public sealed class PCustomHouseAddRoof : PacketWriter
    {
        public PCustomHouseAddRoof(in Graphic graphic, in uint x, in uint y, in uint z) : base(0xD7)
        {
            WriteUInt(World.Player);
            WriteUShort(0x13);
            WriteByte(0);
            WriteUInt(graphic);
            WriteByte(0);
            WriteUInt(x);
            WriteByte(0);
            WriteUInt(y);
            WriteByte(0);
            WriteUInt(z);
            WriteByte(0x0A);
        }
    }

    public sealed class PCustomHouseDeleteRoof : PacketWriter
    {
        public PCustomHouseDeleteRoof(in Graphic graphic, in uint x, in uint y, in uint z) : base(0xD7)
        {
            WriteUInt(World.Player);
            WriteUShort(0x14);
            WriteByte(0);
            WriteUInt(graphic);
            WriteByte(0);
            WriteUInt(x);
            WriteByte(0);
            WriteUInt(y);
            WriteByte(0);
            WriteUInt(z);
            WriteByte(0x0A);
        }
    }

    public sealed class PCustomHouseAddStair : PacketWriter
    {
        public PCustomHouseAddStair(in Graphic graphic, in uint x, in uint y) : base(0xD7)
        {
            WriteUInt(World.Player);
            WriteUShort(0x0D);
            WriteByte(0);
            WriteUInt(graphic);
            WriteByte(0);
            WriteUInt(x);
            WriteByte(0);
            WriteUInt(y);
            WriteByte(0x0A);
        }
    }

    public sealed class PPing : PacketWriter
    {
        public PPing() : base(0x73)
        {
            WriteByte(0);
        }
    }

    public sealed class PClientViewRange : PacketWriter
    {
        public PClientViewRange(byte range) : base(0xC8)
        {
            if (range < 5)
                range = 5;
            else if (range > 24)
                range = 24;
            WriteByte(range);
        }
    }
}