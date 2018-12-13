#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//      
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using System;
using System.Collections.Generic;
using System.Linq;

using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.IO;
using ClassicUO.IO.Resources;

namespace ClassicUO.Network
{
    public sealed class PACKTalk : PacketWriter
    {
        public PACKTalk() : base(0x03)
        {
            WriteByte(0x20);
            WriteByte(0x00);
            WriteByte(0x34);
            WriteByte(0x00);
            WriteByte(0x03);
            WriteByte(0xdb);
            WriteByte(0x13);
            WriteByte(0x14);
            WriteByte(0x3f);
            WriteByte(0x45);
            WriteByte(0x2c);
            WriteByte(0x58);
            WriteByte(0x0f);
            WriteByte(0x5d);
            WriteByte(0x44);
            WriteByte(0x2e);
            WriteByte(0x50);
            WriteByte(0x11);
            WriteByte(0xdf);
            WriteByte(0x75);
            WriteByte(0x5c);
            WriteByte(0xe0);
            WriteByte(0x3e);
            WriteByte(0x71);
            WriteByte(0x4f);
            WriteByte(0x31);
            WriteByte(0x34);
            WriteByte(0x05);
            WriteByte(0x4e);
            WriteByte(0x18);
            WriteByte(0x1e);
            WriteByte(0x72);
            WriteByte(0x0f);
            WriteByte(0x59);
            WriteByte(0xad);
            WriteByte(0xf5);
            WriteByte(0x00);
        }
    }

    public sealed class PSeed : PacketWriter
    {
        public PSeed(byte[] version) : base(0xEF)
        {
            const uint SEED = 0x1337BEEF;
            WriteUInt(SEED);
            for (int i = 0; i < 4; i++) WriteUInt(version[i]);
        }

        public PSeed(uint v, byte[] version) : base(0xEF)
        {
            WriteUInt(v);
            for (int i = 0; i < 4; i++) WriteUInt(version[i]);
        }
    }

    public sealed class PFirstLogin : PacketWriter
    {
        public PFirstLogin(string account, string password) : base(0x80)
        {
            WriteASCII(account, 30);
            WriteASCII(password, 30);
            WriteByte(0xFF);
        }
    }

    public sealed class PSelectServer : PacketWriter
    {
        public PSelectServer(byte index) : base(0xA0)
        {
            WriteByte(0);
            WriteByte(index);
        }
    }

    public sealed class PSecondLogin : PacketWriter
    {
        public PSecondLogin(string account, string password, uint seed) : base(0x91)
        {
            WriteUInt(seed);
            WriteASCII(account, 30);
            WriteASCII(password, 30);
        }
    }

    public sealed class PCreateCharacter : PacketWriter
    {
        public PCreateCharacter(PlayerMobile character, uint clientIP, int serverIndex, uint slot) : base(0x00)
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
            WriteASCII(character.Name, 30);
            WriteUShort(0x00);
            uint clientflag = 0;
            ushort flags = (ushort) World.ClientFlags.Flags;

            for (ushort i = 0; i < flags; i++)
                clientflag |= (uint) (1 << i);

            WriteUInt(clientflag);
            WriteUInt(0x01);
            WriteUInt(0x0);
            WriteByte(0x0); // Profession
            Skip(15);
            byte val;

            if (FileManager.ClientVersion < ClientVersions.CV_4011D)
                val = Convert.ToByte(character.Flags.HasFlag(Flags.Female));
            else
            {
                val = (byte) character.Race;

                if (FileManager.ClientVersion < ClientVersions.CV_7000)
                    val--;
                val = (byte) (val * 2 + Convert.ToByte(character.Flags.HasFlag(Flags.Female)));
            }

            WriteByte(val);
            WriteByte((byte) character.Strength);
            WriteByte((byte) character.Dexterity);
            WriteByte((byte) character.Intelligence);
            var skills = character.Skills.OrderByDescending(o => o.Value).Take(skillcount).ToList();

            foreach (var skill in skills)
            {
                WriteByte((byte) skill.Index);
                WriteByte((byte) skill.ValueFixed);
            }

            WriteUShort(character.Hue);
            WriteUShort(character.Equipment[(int) Layer.Hair].Graphic);
            WriteUShort(character.Equipment[(int) Layer.Hair].Hue);

            if (character.Equipment[(int) Layer.Beard] != null)
            {
                WriteUShort(character.Equipment[(int) Layer.Beard].Graphic);
                WriteUShort(character.Equipment[(int) Layer.Beard].Hue);
            }
            else
            {
                WriteUShort(0x00);
                WriteUShort(0x00);
            }

            WriteByte((byte) serverIndex);
            var location = 0; // TODO: write the city index

            if (FileManager.ClientVersion < ClientVersions.CV_70130)
                location--;
            WriteByte((byte) location); //location
            WriteUInt(slot);
            WriteUInt(clientIP);
            WriteUShort(character.Equipment[(int) Layer.Shirt].Hue);

            if (character.Equipment[(int) Layer.Pants] != null)
                WriteUShort(character.Equipment[(int) Layer.Pants].Hue);
            else
                WriteUShort(0x00);
        }
    }

    public sealed class PDeleteCharacter : PacketWriter
    {
        public PDeleteCharacter(byte index, uint ipclient) : base(0x83)
        {
            Skip(30);
            WriteUInt(index);
            WriteUInt(ipclient);
        }
    }

    public sealed class PSelectCharacter : PacketWriter
    {
        public PSelectCharacter(uint index, string name, uint ipclient) : base(0x5D)
        {
            WriteUInt(0xEDEDEDED);
            WriteASCII(name, 30);
            Skip(2);
            uint clientflag = 0x1f;
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
        public PDropRequestNew(Serial serial, ushort x, ushort y, sbyte z, byte slot, Serial container) : base(0x08)
        {
            WriteUInt(serial);
            WriteUShort(x);
            WriteUShort(y);
            WriteSByte(z);
            WriteByte(slot);
            WriteUInt(container);
        }

        public PDropRequestNew(Serial serial, Position position, byte slot, Serial container) : this(serial, position.X, position.Y, position.Z, slot, container)
        {
        }
    }

    public sealed class PEquipRequest : PacketWriter
    {
        public PEquipRequest(Serial serial, Layer layer, Serial container) : base(0x13)
        {
            WriteUInt(serial);
            WriteByte((byte) layer);
            WriteUInt(container);
        }
    }

    public sealed class PChangeWarMode : PacketWriter
    {
        public PChangeWarMode(bool state) : base(0x72)
        {
            WriteBool(state);
            WriteByte(0x00); //always
            WriteByte(0x32); //always
            WriteByte(0x00); //always
        }
    }

    public sealed class PHelpRequest : PacketWriter
    {
        public PHelpRequest() : base(0x9B)
        {
            byte[] empty = new byte[257];
            foreach (byte emptyByte in empty) WriteByte(emptyByte);
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
        public PSkillsStatusChangeRequest(ushort skillindex, byte lockstate) : base(0x3A)
        {
            WriteUShort(skillindex);
            WriteByte(lockstate);
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
        //public PClientVersion(byte[] version) : base(0xBD)
        //{
        //    WriteASCII(string.Format("{0}.{1}.{2}.{3}", version[0], version[1], version[2], version[3]));
        //}

        public PClientVersion(string v) : base(0xBD)
        {
            string[] version = v.Split(new[]
            {
                '.'
            }, StringSplitOptions.RemoveEmptyEntries);
            WriteASCII($"{version[0]}.{version[1]}.{version[2]}.{version[3]}");
        }
    }

    public sealed class PASCIISpeechRequest : PacketWriter
    {
        public PASCIISpeechRequest(string text, MessageType type, MessageFont font, Hue hue) : base(0x03)
        {
            WriteByte((byte) type);
            WriteUShort(hue);
            WriteUShort((ushort) font);
            WriteASCII(text);
        }
    }

    public sealed class PUnicodeSpeechRequest : PacketWriter
    {
        public PUnicodeSpeechRequest(string text, MessageType type, MessageFont font, Hue hue, string lang) : base(0xAD)
        {
            SpeechEntry[] entries = Speeches.GetKeywords(text);

            if (entries.Length > 0)
                type |= MessageType.Encoded;
            WriteByte((byte) type);
            WriteUShort(hue);
            WriteUShort((ushort) font);
            WriteASCII(lang, 4);

            if (entries.Length > 0)
            {
                byte[] t = new byte[(int) Math.Ceiling((entries.Length + 1) * 1.5f)];
                // write 12 bits at a time. first write count: byte then half byte.
                t[0] = (byte) ((entries.Length & 0x0FF0) >> 4);
                t[1] = (byte) ((entries.Length & 0x000F) << 4);

                for (int i = 0; i < entries.Length; i++)
                {
                    int index = (int) ((i + 1) * 1.5f);

                    if (i % 2 == 0) // write half byte and then byte
                    {
                        t[index + 0] |= (byte) ((entries[i].KeywordID & 0x0F00) >> 8);
                        t[index + 1] = (byte) (entries[i].KeywordID & 0x00FF);
                    }
                    else // write byte and then half byte
                    {
                        t[index] = (byte) ((entries[i].KeywordID & 0x0FF0) >> 4);
                        t[index + 1] = (byte) ((entries[i].KeywordID & 0x000F) << 4);
                    }
                }

                for (int i = 0; i < t.Length; i++)
                    WriteByte(t[i]);
                WriteASCII(text);
            }
            else
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
        public POpenSpellBook(byte type) : base(0x12)
        {
            WriteByte(0x43);
            WriteByte(type);
        }
    }

    public sealed class PEmoteAction : PacketWriter
    {
        public PEmoteAction(string action) : base(0x12)
        {
            WriteByte(0xC7);
            WriteASCII(action);
        }
    }

    public sealed class PGumpResponse : PacketWriter
    {
        public PGumpResponse(Serial local, Serial server, int buttonID, Serial[] switches, Tuple<ushort, string>[] entries) : base(0xB1)
        {
            WriteUInt(local);
            WriteUInt(server);
            WriteUInt((uint) buttonID);

            if (switches == null)
                WriteUInt(0);
            else
            {
                WriteUInt((uint) switches.Length);

                for (int i = 0; i < switches.Length; i++)
                    WriteUInt(switches[i]);
            }

            if (entries == null)
                WriteUInt(0);
            else
            {
                WriteUInt((uint) entries.Length);

                for (int i = 0; i < entries.Length; i++)
                {
                    int length = entries[i].Item2.Length;
                    WriteUShort(entries[i].Item1);
                    WriteUShort((ushort) length);
                    WriteUnicode(entries[i].Item2, entries[i].Item2.Length);
                }
            }
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
        public PTradeResponse(Serial serial, int code, bool state) : base(0x6F)
        {
            if (code == 1) // cancel
            {
                WriteByte(0x01);
                WriteUInt(serial);
            }
            else if (code == 2) // update
            {
                WriteByte(0x02);
                WriteUInt(serial);
                WriteUInt( (uint) (state ? 1 : 0));
            }
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
        public PRenameRequest(Serial serial, string name) : base(0x75)
        {
            WriteUInt(serial);
            WriteASCII(name, 30);
        }
    }

    public sealed class PNameRequest : PacketWriter
    {
        public PNameRequest(Serial serial) : base(0x98)
        {
            WriteUInt(serial);
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

    public sealed class PTargetObject : PacketWriter
    {
        public PTargetObject(Entity entity, Serial cursorID, byte cursorType) : base(0x6C)
        {
            WriteByte(0x00);
            WriteUInt(cursorID);
            WriteByte(cursorType);
            WriteUInt(entity.Serial);
            WriteUShort(entity.Position.X);
            WriteUShort(entity.Position.Y);
            WriteByte(0xFF);
            WriteSByte(entity.Position.Z);
            WriteUShort(entity.Graphic);
        }
    }

    public sealed class PTargetXYZ : PacketWriter
    {
        public PTargetXYZ(ushort x, ushort y, short z, ushort modelNumber, Serial cursorID, byte targetType) : base(0x6C)
        {
            WriteByte(0x01);
            WriteUInt(cursorID);
            WriteByte(targetType);
            WriteUInt(0x00);
            WriteUShort(x);
            WriteUShort(y);
            WriteUShort((ushort) z);
            WriteUShort(modelNumber);
        }
    }

    public sealed class PTargetCancel : PacketWriter
    {
        public PTargetCancel(Serial cursorID, byte cursorType) : base(0x6C)
        {
            WriteByte(0x00);
            WriteUInt(cursorID);
            WriteByte(cursorType);
            WriteUInt(0x00);
            WriteUShort(0x00);
            WriteUShort(0x00);
            WriteUShort(0x00);
            WriteUShort(0x00);
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
        public PDyeDataResponse(Serial serial, Graphic graphic, Hue hue) : base(0x95)
        {
            WriteUInt(serial);
            WriteUShort(graphic);
            WriteUShort(hue);
        }
    }

    public sealed class PProfileRequest : PacketWriter
    {
        public PProfileRequest(Serial serial) : base(0xB8)
        {
            WriteByte(0);
            WriteUInt(serial);
        }
    }

    public sealed class PProfileUpdate : PacketWriter
    {
        public PProfileUpdate(Serial serial, string text, int len) : base(0xB8)
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
        public PCloseStatusBarGump(Serial serial) : base(0xBF)
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
        public PPartyRemoveRequest(Serial serial) : base(0xBF)
        {
            WriteUShort(0x06);
            WriteByte(2);
            WriteUInt(serial);
        }
    }

    public sealed class PPartyChangeLootTypeRequest : PacketWriter
    {
        public PPartyChangeLootTypeRequest(bool type) : base(0xBF)
        {
            WriteUShort(0x06);
            WriteByte(0x06);
            WriteBool(type);
        }
    }

    public sealed class PPartyAccept : PacketWriter
    {
        public PPartyAccept(Serial serial) : base(0xBF)
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
        public PBulletinBoardRequestMessage(Serial serial, Serial msgserial) : base(0x71)
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
            WriteByte((byte) (subject.Length + 1));
            WriteASCII(subject);

            string[] lines = message.Split(new[]
            {
                '\n'
            }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < lines.Length; i++)
            {
                WriteByte((byte) lines[i].Length);
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

        public PAssistVersion(string v, uint version) : base(0xBE)
        {
            WriteUInt(version);

            string[] clientversion = v.Split(new[]
            {
                '.'
            }, StringSplitOptions.RemoveEmptyEntries);
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
        public PLanguage(string lang) : base(0xBF)
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

    public sealed class POpenChat : PacketWriter
    {
        public POpenChat(string name) : base(0xB5)
        {
            WriteUnicode(name, 30);
        }
    }

    public sealed class PMapMessage : PacketWriter
    {
        public PMapMessage(Serial serial, byte action, byte pin, ushort x, ushort y) : base(0x56)
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
            //WriteByte(0x0A);
            WriteByte(0x00);
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
        public PVirtueRequest(uint buttonID) : base(0xB1)
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
        public PInvokeVirtueRequest(byte id) : base(0x12)
        {
            WriteByte(0xF4);
            WriteByte(id);
            WriteByte(0);
        }
    }

    public sealed class PMegaClilocRequestOld : PacketWriter
    {
        public PMegaClilocRequestOld(Serial serial) : base(0xBF)
        {
            WriteUShort(0x10);
            WriteUInt(serial);
        }
    }

    public sealed class PMegaClilocRequest : PacketWriter
    {
        public PMegaClilocRequest(List<Serial> list) : base(0xD6)
        {
            for (int i = 0; i < list.Count && i < 50; i++) WriteUInt(list[i]);
        }

        public PMegaClilocRequest(Serial serial) : base(0xD6)
        {
            WriteUInt(serial);
        }
    }

    public sealed class PChangeStatLockStateRequest : PacketWriter
    {
        public PChangeStatLockStateRequest(byte stat, Lock state) : base(0xBF)
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
        public PBookPageDataRequest(Serial serial, ushort page) : base(0x66)
        {
            WriteUInt(serial);
            WriteUShort(1);
            WriteUShort(page);
            WriteUShort(0xFFFF);
        }
    }

    public sealed class PBuyRequest : PacketWriter
    {
        public PBuyRequest(Serial vendorSerial, Tuple<uint, ushort>[] items) : base(0x3B)
        {
            WriteUInt(vendorSerial);

            if (items.Length > 0)
            {
                WriteByte(0x02); // flag

                for (int i = 0; i < items.Length; i++)
                {
                    WriteByte(0x1A); // layer?
                    WriteUInt(items[i].Item1);
                    WriteUShort(items[i].Item2);
                }
            }
            else
                WriteByte(0x00);
        }
    }

    public sealed class PSellRequest : PacketWriter
    {
        public PSellRequest(Serial vendorSerial, Tuple<uint, ushort>[] items) : base(0x9F)
        {
            WriteUInt(vendorSerial);
            WriteUShort((ushort)items.Length);

            for (int i = 0; i < items.Length; i++)
            {
                WriteUInt(items[i].Item1);
                WriteUShort(items[i].Item2);
            }
        }
    }

    public sealed class PUseCombatAbility : PacketWriter
    {
        public PUseCombatAbility(byte idx) : base(0xD7)
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
        public PTargetSelectedObject(Serial useObjSerial, Serial targObjSerial) : base(0xBF)
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
        public PCustomHouseDataRequest(Serial serial) : base(0xBF)
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
        public static Lazy<PResend> Instance { get; } = new Lazy<PResend>(() => new PResend());
        private PResend() : base(0x22)
        {
        }
    }

    public sealed class PWalkRequest : PacketWriter
    {
        public PWalkRequest(Direction direction, byte seq, bool run) : base(0x02)
        {
            if (run)
                direction |= Direction.Running;
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
        public PCustomHouseGoToFloor(byte floor) : base(0xD7)
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
        public PCustomHouseAddItem(Graphic graphic, uint x, uint y) : base(0xD7)
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
        public PCustomHouseDeleteItem(Graphic graphic, uint x, uint y, uint z) : base(0xD7)
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
        public PCustomHouseAddRoof(Graphic graphic, uint x, uint y, uint z) : base(0xD7)
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
        public PCustomHouseDeleteRoof(Graphic graphic, uint x, uint y, uint z) : base(0xD7)
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
        public PCustomHouseAddStair(Graphic graphic, uint x, uint y) : base(0xD7)
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
        private PPing() : base(0x73)
        {
            WriteByte(0);
        }

        public static Lazy<PPing> Instance { get; } = new Lazy<PPing>(() => new PPing());
    }

    public sealed class PClientViewRange : PacketWriter
    {
        public PClientViewRange(byte range) : base(0xC8)
        {
            if (range < 5)
                range = 5;
            else if (range > 24) range = 24;
            WriteByte(range);
        }
    }
}