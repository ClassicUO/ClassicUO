#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClassicUO.Data;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO.Resources;

namespace ClassicUO.Network
{
    internal sealed class PACKTalk : PacketWriter
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

    internal sealed class PSeed : PacketWriter
    {
        public PSeed(byte[] version) : base(0xEF)
        {
            const uint SEED = 0x1337BEEF;
            WriteUInt(SEED);

            for (int i = 0; i < 4; i++)
            {
                WriteUInt(version[i]);
            }
        }

        public PSeed(uint v, byte major, byte minor, byte build, byte extra) : base(0xEF)
        {
            WriteUInt(v);

            WriteUInt(major);
            WriteUInt(minor);
            WriteUInt(build);
            WriteUInt(extra);
        }
    }

    internal sealed class PFirstLogin : PacketWriter
    {
        public PFirstLogin(string account, string password) : base(0x80)
        {
            WriteASCII(account, 30);
            WriteASCII(password, 30);
            WriteByte(0xFF);
        }
    }

    internal sealed class PSelectServer : PacketWriter
    {
        public PSelectServer(byte index) : base(0xA0)
        {
            WriteByte(0);
            WriteByte(index);
        }
    }

    internal sealed class PSecondLogin : PacketWriter
    {
        public PSecondLogin(string account, string password, uint seed) : base(0x91)
        {
            WriteUInt(seed);
            WriteASCII(account, 30);
            WriteASCII(password, 30);
        }
    }

    internal sealed class PCreateCharacter : PacketWriter
    {
        public PCreateCharacter
        (
            PlayerMobile character,
            int cityIndex,
            uint clientIP,
            int serverIndex,
            uint slot,
            byte profession
        ) : base(0x00)
        {
            int skillcount = 3;

            if (Client.Version >= ClientVersion.CV_70160)
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

            WriteUInt((uint) Client.Protocol);
            WriteUInt(0x01);
            WriteUInt(0x0);
            WriteByte(profession); // Profession
            Skip(15);
            byte val;

            if (Client.Version < ClientVersion.CV_4011D)
            {
                val = Convert.ToByte(character.Flags.HasFlag(Flags.Female));
            }
            else
            {
                val = (byte) character.Race;

                if (Client.Version < ClientVersion.CV_7000)
                {
                    val--;
                }

                val = (byte) (val * 2 + Convert.ToByte(character.Flags.HasFlag(Flags.Female)));
            }

            WriteByte(val);
            WriteByte((byte) character.Strength);
            WriteByte((byte) character.Dexterity);
            WriteByte((byte) character.Intelligence);

            List<Skill> skills = character.Skills.OrderByDescending(o => o.Value).Take(skillcount).ToList();

            foreach (Skill skill in skills)
            {
                WriteByte((byte) skill.Index);
                WriteByte((byte) skill.ValueFixed);
            }

            WriteUShort(character.Hue);
            Item hair = character.FindItemByLayer(Layer.Hair);

            if (hair != null)
            {
                WriteUShort(hair.Graphic);
                WriteUShort(hair.Hue);
            }
            else
            {
                WriteUShort(0x00);
                WriteUShort(0x00);
            }

            Item beard = character.FindItemByLayer(Layer.Beard);

            if (beard != null)
            {
                WriteUShort(beard.Graphic);
                WriteUShort(beard.Hue);
            }
            else
            {
                WriteUShort(0x00);
                WriteUShort(0x00);
            }

            WriteUShort((ushort) cityIndex);
            WriteUShort(0x0000);
            WriteUShort((ushort) slot);

            //if (Client.Version >= ClientVersion.CV_70160)
            //{
            //    WriteUShort((ushort) cityIndex);
            //    WriteUShort(0x0000);
            //    WriteUShort((ushort) slot);
            //}
            //else
            //{
            //    WriteByte((byte) serverIndex);
            //    if (Client.Version < ClientVersion.CV_70130 && cityIndex > 0)
            //        cityIndex--;

            //    WriteByte((byte) cityIndex);
            //    WriteUInt(slot);
            //}

            WriteUInt(clientIP);

            Item shirt = character.FindItemByLayer(Layer.Shirt);

            if (shirt != null)
            {
                WriteUShort(shirt.Hue);
            }
            else
            {
                WriteUShort(0);
            }

            Item pants = character.FindItemByLayer(Layer.Pants);

            if (pants != null)
            {
                WriteUShort(pants.Hue);
            }
            else
            {
                WriteUShort(0x00);
            }
        }
    }

    internal sealed class PDeleteCharacter : PacketWriter
    {
        public PDeleteCharacter(byte index, uint ipclient) : base(0x83)
        {
            Skip(30);
            WriteUInt(index);
            WriteUInt(ipclient);
        }
    }

    internal sealed class PSelectCharacter : PacketWriter
    {
        public PSelectCharacter(uint index, string name, uint ipclient) : base(0x5D)
        {
            WriteUInt(0xEDEDEDED);
            WriteASCII(name, 30);
            Skip(2);
            WriteUInt((uint) Client.Protocol);
            Skip(24);
            WriteUInt(index);
            WriteUInt(ipclient);
        }
    }

    internal sealed class PPickUpRequest : PacketWriter
    {
        public PPickUpRequest(uint serial, ushort count) : base(0x07)
        {
            WriteUInt(serial);
            WriteUShort(count);
        }
    }

    internal sealed class PDropRequestOld : PacketWriter
    {
        public PDropRequestOld(uint serial, ushort x, ushort y, sbyte z, uint container) : base(0x08)
        {
            WriteUInt(serial);
            WriteUShort(x);
            WriteUShort(y);
            WriteSByte(z);
            WriteUInt(container);
        }
    }

    internal sealed class PDropRequestNew : PacketWriter
    {
        public PDropRequestNew
        (
            uint serial,
            ushort x,
            ushort y,
            sbyte z,
            byte slot,
            uint container
        ) : base(0x08)
        {
            WriteUInt(serial);
            WriteUShort(x);
            WriteUShort(y);
            WriteSByte(z);
            WriteByte(slot);
            WriteUInt(container);
        }
    }

    internal sealed class PEquipRequest : PacketWriter
    {
        public PEquipRequest(uint serial, Layer layer, uint container) : base(0x13)
        {
            WriteUInt(serial);
            WriteByte((byte) layer);
            WriteUInt(container);
        }
    }

    internal sealed class PChangeWarMode : PacketWriter
    {
        public PChangeWarMode(bool state) : base(0x72)
        {
            WriteBool(state);
            WriteByte(0x32);
            WriteByte(0);
        }
    }

    internal sealed class PHelpRequest : PacketWriter
    {
        public PHelpRequest() : base(0x9B)
        {
            for (int i = 0; i < 257; i++)
            {
                WriteByte(0x00);
            }
        }
    }

    internal sealed class PStatusRequest : PacketWriter
    {
        public PStatusRequest(uint serial) : base(0x34)
        {
            WriteUInt(0xEDEDEDED);
            WriteByte(4);
            WriteUInt(serial);
        }
    }

    internal sealed class PSkillsRequest : PacketWriter
    {
        public PSkillsRequest(uint serial) : base(0x34)
        {
            WriteUInt(0xEDEDEDED);
            WriteByte(5);
            WriteUInt(serial);
        }
    }

    internal sealed class PSkillsStatusChangeRequest : PacketWriter
    {
        public PSkillsStatusChangeRequest(ushort skillindex, byte lockstate) : base(0x3A)
        {
            WriteUShort(skillindex);
            WriteByte(lockstate);
        }
    }

    internal sealed class PClickRequest : PacketWriter
    {
        public PClickRequest(uint serial) : base(0x09)
        {
            WriteUInt(serial);
        }
    }

    internal sealed class PDoubleClickRequest : PacketWriter
    {
        public PDoubleClickRequest(uint serial) : base(0x06)
        {
            WriteUInt(serial);
        }
    }

    internal sealed class PAttackRequest : PacketWriter
    {
        public PAttackRequest(uint serial) : base(0x05)
        {
            WriteUInt(serial);
        }
    }

    internal sealed class PClientVersion : PacketWriter
    {
        public PClientVersion(byte[] version) : base(0xBD)
        {
            WriteASCII
            (
                string.Format
                (
                    "{0}.{1}.{2}.{3}",
                    version[0],
                    version[1],
                    version[2],
                    version[3]
                )
            );
        }

        public PClientVersion(string v) : base(0xBD)
        {
            WriteASCII(v);
        }
    }

    internal sealed class PASCIISpeechRequest : PacketWriter
    {
        public PASCIISpeechRequest(string text, MessageType type, byte font, ushort hue) : base(0x03)
        {
            List<SpeechEntry> entries = SpeechesLoader.Instance.GetKeywords(text);

            bool encoded = entries != null && entries.Count != 0;

            if (encoded)
            {
                type |= MessageType.Encoded;
            }

            WriteByte((byte) type);
            WriteUShort(hue);
            WriteUShort(font);
            WriteASCII(text);
        }
    }

    internal sealed class PUnicodeSpeechRequest : PacketWriter
    {
        public PUnicodeSpeechRequest(string text, MessageType type, byte font, ushort hue, string lang) : base(0xAD)
        {
            List<SpeechEntry> entries = SpeechesLoader.Instance.GetKeywords(text);

            bool encoded = entries != null && entries.Count != 0;

            if (encoded)
            {
                type |= MessageType.Encoded;
            }

            WriteByte((byte) type);
            WriteUShort(hue);
            WriteUShort(font);
            WriteASCII(lang, 4);

            if (encoded)
            {
                List<byte> codeBytes = new List<byte>();
                byte[] utf8 = Encoding.UTF8.GetBytes(text);
                int length = entries.Count;
                codeBytes.Add((byte) (length >> 4));
                int num3 = length & 15;
                bool flag = false;
                int index = 0;

                while (index < length)
                {
                    int keywordID = entries[index].KeywordID;

                    if (flag)
                    {
                        codeBytes.Add((byte) (keywordID >> 4));
                        num3 = keywordID & 15;
                    }
                    else
                    {
                        codeBytes.Add((byte) ((num3 << 4) | ((keywordID >> 8) & 15)));
                        codeBytes.Add((byte) keywordID);
                    }

                    index++;
                    flag = !flag;
                }

                if (!flag)
                {
                    codeBytes.Add((byte) (num3 << 4));
                }

                for (int i = 0; i < codeBytes.Count; i++)
                {
                    WriteByte(codeBytes[i]);
                }

                WriteBytes(utf8, 0, utf8.Length);
                WriteByte(0);
            }
            else
            {
                WriteUnicode(text);
            }
        }
    }

    internal sealed class PCastSpell : PacketWriter
    {
        public PCastSpell(int idx) : base(0xBF)
        {
            if (Client.Version >= ClientVersion.CV_60142)
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

    internal sealed class PCastSpellFromBook : PacketWriter
    {
        public PCastSpellFromBook(int idx, uint serial) : base(0x12)
        {
            WriteByte(0x27);
            WriteASCII($"{idx} {serial}");
        }
    }

    internal sealed class PUseSkill : PacketWriter
    {
        public PUseSkill(int idx) : base(0x12)
        {
            WriteByte(0x24);
            WriteASCII($"{idx} 0");
        }
    }

    internal sealed class POpenDoor : PacketWriter
    {
        public POpenDoor() : base(0x12)
        {
            WriteByte(0x58);
            WriteByte(0x00);
        }
    }

    internal sealed class POpenSpellBook : PacketWriter
    {
        public POpenSpellBook(byte type) : base(0x12)
        {
            WriteByte(0x43);
            WriteByte(type);
        }
    }

    internal sealed class PEmoteAction : PacketWriter
    {
        public PEmoteAction(string action) : base(0x12)
        {
            WriteByte(0xC7);
            WriteASCII(action);
        }
    }

    internal sealed class PGumpResponse : PacketWriter
    {
        public PGumpResponse(uint local, uint server, int buttonID, uint[] switches, Tuple<ushort, string>[] entries) : base(0xB1)
        {
            WriteUInt(local);
            WriteUInt(server);
            WriteUInt((uint) buttonID);

            WriteUInt((uint) switches.Length);

            //for (int i = switches.Length - 1; i >= 0; i--)
            for (int i = 0; i < switches.Length; i++)
            {
                WriteUInt(switches[i]);
            }

            WriteUInt((uint) entries.Length);

            //for (int i = entries.Length - 1; i >= 0; i--)
            for (int i = 0; i < entries.Length; i++)
            {
                int length = Math.Min(239, entries[i].Item2.Length);

                WriteUShort(entries[i].Item1);

                WriteUShort((ushort) length);

                WriteUnicode(entries[i].Item2, length);
            }
        }
    }

    internal sealed class PVirtueGumpReponse : PacketWriter
    {
        public PVirtueGumpReponse(uint serial, uint code) : base(0xB1)
        {
            WriteUInt(serial);
            WriteUInt(0x000001CD);
            WriteUInt(code);
        }
    }

    internal sealed class PMenuResponse : PacketWriter
    {
        public PMenuResponse(uint serial, ushort graphic, int code, ushort itemGraphic, ushort itemHue) : base(0x7D)
        {
            WriteUInt(serial);
            WriteUShort(graphic);

            if (code != 0)
            {
                WriteUShort((ushort) code);

                WriteUShort(itemGraphic);
                WriteUShort(itemHue);
            }
        }
    }

    internal sealed class PGrayMenuResponse : PacketWriter
    {
        public PGrayMenuResponse(uint serial, ushort graphic, ushort code) : base(0x7D)
        {
            WriteUInt(serial);
            WriteUShort(graphic);
            WriteUShort(code);
        }
    }

    internal sealed class PTradeResponse : PacketWriter
    {
        public PTradeResponse(uint serial, int code, bool state) : base(0x6F)
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
                WriteUInt((uint) (state ? 1 : 0));
            }
        }
    }

    internal sealed class PTradeUpdateGold : PacketWriter
    {
        public PTradeUpdateGold(uint serial, uint gold, uint platinum) : base(0x6F)
        {
            WriteByte(0x03);
            WriteUInt(serial);
            WriteUInt(gold);
            WriteUInt(platinum);
        }
    }

    internal sealed class PLogoutNotification : PacketWriter
    {
        public PLogoutNotification() : base(0x05)
        {
            WriteUInt(0xFFFF_FFFF);
        }
    }

    internal sealed class PTextEntryDialogResponse : PacketWriter
    {
        public PTextEntryDialogResponse(uint serial, byte parentID, byte button, string text, bool code) : base(0xAC)
        {
            WriteUInt(serial);
            WriteByte(parentID);
            WriteByte(button);
            WriteBool(code);

            WriteUShort((ushort) (text.Length + 1));
            WriteASCII(text, text.Length + 1);
        }
    }

    internal sealed class PRenameRequest : PacketWriter
    {
        public PRenameRequest(uint serial, string name) : base(0x75)
        {
            WriteUInt(serial);
            WriteASCII(name, 30);
        }
    }

    internal sealed class PNameRequest : PacketWriter
    {
        public PNameRequest(uint serial) : base(0x98)
        {
            WriteUInt(serial);
        }
    }

    internal sealed class PTipRequest : PacketWriter
    {
        public PTipRequest(ushort id, byte flag) : base(0xA7)
        {
            WriteUShort(id);
            WriteByte(flag);
        }
    }

    internal sealed class PTargetObject : PacketWriter
    {
        public PTargetObject
        (
            uint entity,
            ushort graphic,
            ushort x,
            ushort y,
            sbyte z,
            uint cursorID,
            byte cursorType
        ) : base(0x6C)
        {
            WriteByte(0x00);
            WriteUInt(cursorID);
            WriteByte(cursorType);
            WriteUInt(entity);
            WriteUShort(x);
            WriteUShort(y);
            WriteUShort((ushort) z);
            WriteUShort(graphic);
        }
    }

    internal sealed class PTargetXYZ : PacketWriter
    {
        public PTargetXYZ
        (
            ushort x,
            ushort y,
            short z,
            ushort modelNumber,
            uint cursorID,
            byte targetType
        ) : base(0x6C)
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

    internal sealed class PTargetCancel : PacketWriter
    {
        public PTargetCancel(CursorTarget type, uint cursorID, byte cursorType) : base(0x6C)
        {
            WriteByte((byte) type);
            WriteUInt(cursorID);
            WriteByte(cursorType);
            WriteUInt(0);
            WriteUInt(0xFFFF_FFFF);
            WriteByte(0);
            WriteByte(0);
            WriteUShort(0);
        }
    }

    internal sealed class PASCIIPromptResponse : PacketWriter
    {
        public PASCIIPromptResponse(string text, bool cancel) : base(0x9A)
        {
            WriteBytes(MessageManager.PromptData.Data, 0, 8);
            WriteUInt((uint) (cancel ? 0 : 1));

            WriteASCII(text);
        }
    }

    internal sealed class PUnicodePromptResponse : PacketWriter
    {
        public PUnicodePromptResponse(string text, string lang, bool cancel) : base(0xC2)
        {
            WriteBytes(MessageManager.PromptData.Data, 0, 8);
            WriteUInt((uint) (cancel ? 0 : 1));
            WriteASCII(lang, 3);
            WriteUnicode(text, text.Length);
            //This must be terminated with EXACTLY one null byte, unlike most unicode-containing packets which are terminated with two.
            //Some servers are fussy about this and will reject the packet otherwise!
            WriteByte(0x00);
        }
    }

    internal sealed class PDyeDataResponse : PacketWriter
    {
        public PDyeDataResponse(uint serial, ushort graphic, ushort hue) : base(0x95)
        {
            WriteUInt(serial);
            WriteUShort(graphic);
            WriteUShort(hue);
        }
    }

    internal sealed class PProfileRequest : PacketWriter
    {
        public PProfileRequest(uint serial) : base(0xB8)
        {
            WriteByte(0);
            WriteUInt(serial);
        }
    }

    internal sealed class PProfileUpdate : PacketWriter
    {
        public PProfileUpdate(uint serial, string text) : base(0xB8)
        {
            WriteByte(1);
            WriteUInt(serial);
            WriteUShort(0x01);
            WriteUShort((ushort) text.Length);
            WriteUnicode(text, text.Length);
        }
    }

    internal sealed class PClickQuestArrow : PacketWriter
    {
        public PClickQuestArrow(bool rightClick) : base(0xBF)
        {
            WriteUShort(0x07);
            WriteBool(rightClick);
        }
    }

    internal sealed class PCloseStatusBarGump : PacketWriter
    {
        public PCloseStatusBarGump(uint serial) : base(0xBF)
        {
            WriteUShort(0x0C);
            WriteUInt(serial);
        }
    }

    internal sealed class PPartyInviteRequest : PacketWriter
    {
        public PPartyInviteRequest() : base(0xBF)
        {
            WriteUShort(0x06);
            WriteByte(1);
            WriteUInt(0);
        }
    }

    internal sealed class PPartyRemoveRequest : PacketWriter
    {
        public PPartyRemoveRequest(uint serial) : base(0xBF)
        {
            WriteUShort(0x06);
            WriteByte(2);
            WriteUInt(serial);
        }
    }

    internal sealed class PPartyChangeLootTypeRequest : PacketWriter
    {
        public PPartyChangeLootTypeRequest(bool type) : base(0xBF)
        {
            WriteUShort(0x06);
            WriteByte(0x06);
            WriteBool(type);
        }
    }

    internal sealed class PPartyAccept : PacketWriter
    {
        public PPartyAccept(uint serial) : base(0xBF)
        {
            WriteUShort(0x06);
            WriteByte(0x08);
            WriteUInt(serial);
        }
    }

    internal sealed class PPartyDecline : PacketWriter
    {
        public PPartyDecline(uint serial) : base(0xBF)
        {
            WriteUShort(0x06);
            WriteByte(0x09);
            WriteUInt(serial);
        }
    }

    internal sealed class PPartyMessage : PacketWriter
    {
        public PPartyMessage(string text, uint serial) : base(0xBF)
        {
            WriteUShort(0x06);

            if (SerialHelper.IsValid(serial))
            {
                WriteByte(0x03);
                WriteUInt(serial);
            }
            else
            {
                WriteByte(0x04);
            }

            WriteUnicode(text);
        }
    }

    internal sealed class PGameWindowSize : PacketWriter
    {
        public PGameWindowSize(uint w, uint h) : base(0xBF)
        {
            WriteUShort(0x05);
            WriteUInt(w);
            WriteUInt(h);
        }
    }

    internal sealed class PBulletinBoardRequestMessage : PacketWriter
    {
        public PBulletinBoardRequestMessage(uint serial, uint msgserial) : base(0x71)
        {
            WriteByte(0x03);
            WriteUInt(serial);
            WriteUInt(msgserial);
        }
    }

    internal sealed class PBulletinBoardRequestMessageSummary : PacketWriter
    {
        public PBulletinBoardRequestMessageSummary(uint serial, uint msgserial) : base(0x71)
        {
            WriteByte(0x04);
            WriteUInt(serial);
            WriteUInt(msgserial);
        }
    }

    internal sealed class PBulletinBoardPostMessage : PacketWriter
    {
        public PBulletinBoardPostMessage(uint serial, uint msgserial, string subject, string _textBox) : base(0x71)
        {
            WriteByte(0x05);
            WriteUInt(serial);
            WriteUInt(msgserial);
            WriteByte((byte) (subject.Length + 1));
            byte[] titolo = Encoding.UTF8.GetBytes(subject);
            WriteBytes(titolo, 0, titolo.Length);
            WriteByte(0);
            string[] splits = _textBox.Split('\n');
            int numlinee = splits.Length;
            WriteByte((byte) numlinee);

            for (int L = 0; L < numlinee; L++)
            {
                byte[] buf = Encoding.UTF8.GetBytes(splits[L].Trim());

                WriteByte((byte) (buf.Length + 1));
                WriteBytes(buf, 0, buf.Length);
                WriteByte(0);
            }
        }
    }

    internal sealed class PBulletinBoardRemoveMessage : PacketWriter
    {
        public PBulletinBoardRemoveMessage(uint serial, uint msgserial) : base(0x71)
        {
            WriteByte(0x06);
            WriteUInt(serial);
            WriteUInt(msgserial);
        }
    }

    internal sealed class PAssistVersion : PacketWriter
    {
        public PAssistVersion(byte[] clientversion, uint version) : base(0xBE)
        {
            WriteUInt(version);

            WriteASCII
            (
                string.Format
                (
                    "{0}.{1}.{2}.{3}",
                    clientversion[0],
                    clientversion[1],
                    clientversion[2],
                    clientversion[3]
                )
            );
        }

        public PAssistVersion(string v, uint version) : base(0xBE)
        {
            WriteUInt(version);
            WriteASCII(v);
        }
    }

    internal sealed class PRazorAnswer : PacketWriter
    {
        public PRazorAnswer() : base(0xF0)
        {
            WriteByte(0xFF);
        }
    }

    internal sealed class PQueryGuildPosition : PacketWriter
    {
        public PQueryGuildPosition() : base(0xF0)
        {
            WriteByte(0x01);
            WriteBool(true);
        }
    }

    internal sealed class PQueryPartyPosition : PacketWriter
    {
        public PQueryPartyPosition() : base(0xF0)
        {
            WriteByte(0x00);
        }
    }

    internal sealed class PLanguage : PacketWriter
    {
        public PLanguage(string lang) : base(0xBF)
        {
            WriteUShort(0x0B);
            WriteASCII(lang);
        }
    }

    internal sealed class PClientType : PacketWriter
    {
        public PClientType() : base(0xBF)
        {
            WriteUShort(0x0F);
            WriteByte(0x0A);

            // other packets sends Client.Protocol directly without doing this. Why not here?
            uint clientFlag = 0;

            for (int i = 0; i < (uint) Client.Protocol; i++)
            {
                clientFlag |= (uint) (1 << i);
            }

            WriteUInt(clientFlag);
        }
    }

    internal sealed class PRequestPopupMenu : PacketWriter
    {
        public PRequestPopupMenu(uint serial) : base(0xBF)
        {
            WriteUShort(0x13);
            WriteUInt(serial);
        }
    }

    internal sealed class PPopupMenuSelection : PacketWriter
    {
        public PPopupMenuSelection(uint serial, ushort menuid) : base(0xBF)
        {
            WriteUShort(0x15);
            WriteUInt(serial);
            WriteUShort(menuid);
        }
    }

    internal sealed class PChatJoinCommand : PacketWriter
    {
        public PChatJoinCommand(string name, string password = null) : base(0xB3)
        {
            WriteASCII("ENU", 4);
            WriteUShort(0x0062);

            WriteUShort(0x0022);
            WriteUnicode(name);
            WriteUShort(0x0022);
            WriteUShort(0x0020);

            if (!string.IsNullOrEmpty(password))
            {
                WriteUnicode(password);
            }
        }
    }

    internal sealed class PChatCreateChannelCommand : PacketWriter
    {
        public PChatCreateChannelCommand(string name, string password = null) : base(0xB3)
        {
            WriteASCII("ENU", 4);
            WriteUShort(0x0063);

            WriteUnicode(name);

            if (!string.IsNullOrEmpty(password))
            {
                WriteUShort(0x007B);
                WriteUnicode(password);
                WriteUShort(0x007D);
            }
        }
    }

    internal sealed class PChatLeaveChannelCommand : PacketWriter
    {
        public PChatLeaveChannelCommand() : base(0xB3)
        {
            WriteASCII("ENU", 4);
            WriteUShort(0x0043);
        }
    }

    internal sealed class PChatMessageCommand : PacketWriter
    {
        public PChatMessageCommand(string msg) : base(0xB3)
        {
            WriteASCII("ENU", 4);
            WriteUShort(0x0061);
            WriteUnicode(msg);
        }
    }

    internal sealed class POpenChat : PacketWriter
    {
        public POpenChat(string name) : base(0xB5)
        {
            int len = name.Length;
            WriteByte(0);

            if (len > 0)
            {
                if (len > 30)
                {
                    len = 30;
                }

                WriteUnicode(name, len);
            }
        }
    }

    internal sealed class PMapMessage : PacketWriter
    {
        public PMapMessage(uint serial, byte action, byte pin, ushort x, ushort y) : base(0x56)
        {
            WriteUInt(serial);
            WriteByte(action);
            WriteByte(pin);
            WriteUShort(x);
            WriteUShort(y);
        }
    }

    internal sealed class PGuildMenuRequest : PacketWriter
    {
        public PGuildMenuRequest() : base(0xD7)
        {
            WriteUInt(World.Player);
            WriteUShort(0x28);
            WriteByte(0x0A);
        }
    }

    internal sealed class PQuestMenuRequest : PacketWriter
    {
        public PQuestMenuRequest() : base(0xD7)
        {
            WriteUInt(World.Player);
            WriteUShort(0x32);
            WriteByte(0x00);
        }
    }

    internal sealed class PEquipLastWeapon : PacketWriter
    {
        public PEquipLastWeapon() : base(0xD7)
        {
            WriteUInt(World.Player);
            WriteUShort(0x1E);
            WriteByte(0x0A);
        }
    }

    internal sealed class PInvokeVirtueRequest : PacketWriter
    {
        public PInvokeVirtueRequest(byte id) : base(0x12)
        {
            WriteByte(0xF4);
            WriteASCII(id.ToString());
        }
    }

    internal sealed class PMegaClilocRequestOld : PacketWriter
    {
        public PMegaClilocRequestOld(uint serial) : base(0xBF)
        {
            WriteUShort(0x10);
            WriteUInt(serial);
        }
    }

    internal sealed class PMegaClilocRequest : PacketWriter
    {
        public PMegaClilocRequest(ref List<uint> list) : base(0xD6)
        {
            int count = Math.Min(15, list.Count);

            for (int i = 0; i < count; i++)
            {
                WriteUInt(list[i]);
            }

            list.RemoveRange(0, count);
        }

        public PMegaClilocRequest(uint serial) : base(0xD6)
        {
            WriteUInt(serial);
        }
    }

    internal sealed class PChangeStatLockStateRequest : PacketWriter
    {
        public PChangeStatLockStateRequest(byte stat, Lock state) : base(0xBF)
        {
            WriteUShort(0x1A);
            WriteByte(stat);
            WriteByte((byte) state);
        }
    }

    internal sealed class PBookHeaderChangedOld : PacketWriter
    {
        public PBookHeaderChangedOld(uint serial, string title, string author) : base(0x93)
        {
            WriteUInt(serial);
            WriteByte(0);
            WriteByte(1);
            WriteUShort(0);
            WriteUTF8(title, 60);
            WriteUTF8(author, 30);
        }
    }

    internal sealed class PBookHeaderChanged : PacketWriter
    {
        public PBookHeaderChanged(uint serial, string title, string author) : base(0xD4)
        {
            WriteUInt(serial);
            WriteByte(0);
            WriteByte(0);
            WriteUShort(0);
            int titleLength = Encoding.UTF8.GetByteCount(title);
            WriteUShort((ushort) titleLength);
            WriteUTF8(title, titleLength);
            int authorLength = Encoding.UTF8.GetByteCount(author);
            WriteUShort((ushort) authorLength);
            WriteUTF8(author, authorLength);
        }
    }


    internal sealed class PBookPageData : PacketWriter
    {
        public PBookPageData(uint serial, string text, int page, List<int> chars) : base(0x66)
        {
            if (text == null)
            {
                text = string.Empty;
            }

            WriteUInt(serial);
            WriteUShort(0x0001);
            WriteUShort((ushort) page);
            WriteUShort((ushort) chars.Count);

            for (int i = 0, x = 0; i < chars.Count; i++)
            {
                if (chars[i] > 0)
                {
                    WriteBytes(Encoding.UTF8.GetBytes(text.Substring(x, chars[i])));
                    x += chars[i];
                }

                WriteByte(0);
            }

            WriteByte(0);
        }

        public PBookPageData(uint serial, string[] text, int page) : base(0x66)
        {
            if (text == null)
            {
                text = new string[ModernBookGump.MAX_BOOK_LINES];

                for (int i = 0; i < text.Length; i++)
                {
                    text[i] = string.Empty;
                }
            }

            WriteUInt(serial);
            WriteUShort(0x0001);
            WriteUShort((ushort) page);
            WriteUShort((ushort) text.Length);

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] != null && text[i].Length > 0)
                {
                    WriteBytes(Encoding.UTF8.GetBytes(text[i].Replace("\n", "")));
                }

                WriteByte(0);
            }

            WriteByte(0);
        }
    }

    internal sealed class PBookPageDataRequest : PacketWriter
    {
        public PBookPageDataRequest(uint serial, ushort page) : base(0x66)
        {
            WriteUInt(serial);
            WriteUShort(1);
            WriteUShort(page);
            WriteUShort(0xFFFF);
        }
    }

    internal sealed class PBuyRequest : PacketWriter
    {
        public PBuyRequest(uint vendorSerial, Tuple<uint, ushort>[] items) : base(0x3B)
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
            {
                WriteByte(0x00);
            }
        }
    }

    internal sealed class PSellRequest : PacketWriter
    {
        public PSellRequest(uint vendorSerial, Tuple<uint, ushort>[] items) : base(0x9F)
        {
            WriteUInt(vendorSerial);
            WriteUShort((ushort) items.Length);

            for (int i = 0; i < items.Length; i++)
            {
                WriteUInt(items[i].Item1);

                WriteUShort(items[i].Item2);
            }
        }
    }

    internal sealed class PUseCombatAbility : PacketWriter
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

    internal sealed class PTargetSelectedObject : PacketWriter
    {
        public PTargetSelectedObject(uint useObjSerial, uint targObjSerial) : base(0xBF)
        {
            WriteUShort(0x2C);
            WriteUInt(useObjSerial);
            WriteUInt(targObjSerial);
        }
    }

    internal sealed class PToggleGargoyleFlying : PacketWriter
    {
        public PToggleGargoyleFlying() : base(0xBF)
        {
            WriteUShort(0x32);
            WriteUShort(0x01);
            WriteUInt(0);
        }
    }

    internal sealed class PCustomHouseDataRequest : PacketWriter
    {
        public PCustomHouseDataRequest(uint serial) : base(0xBF)
        {
            WriteUShort(0x1E);
            WriteUInt(serial);
        }
    }

    internal sealed class PStunRequest : PacketWriter
    {
        public PStunRequest() : base(0xBF)
        {
            WriteUShort(0x09);
        }
    }

    internal sealed class PDisarmRequest : PacketWriter
    {
        public PDisarmRequest() : base(0xBF)
        {
            WriteUShort(0x0A);
        }
    }

    internal sealed class PChangeRaceRequest : PacketWriter
    {
        public PChangeRaceRequest(ushort skin_hue, ushort hair_style, ushort hair_color, ushort beard_style, ushort beard_color) : base(0xBF)
        {
            WriteUShort(skin_hue);
            WriteUShort(hair_style);
            WriteUShort(hair_color);
            WriteUShort(beard_style);
            WriteUShort(beard_color);
        }
    }

    internal sealed class PMultiBoatMoveRequest : PacketWriter
    {
        public PMultiBoatMoveRequest(uint playerSerial, Direction dir, byte speed) : base(0xBF)
        {
            WriteUShort(0x33);
            WriteUInt(playerSerial);
            WriteByte((byte) dir);
            WriteByte((byte) dir);
            WriteByte(speed);
        }
    }

    internal sealed class PResend : PacketWriter
    {
        public PResend() : base(0x22)
        {
        }
    }

    internal sealed class PWalkRequest : PacketWriter
    {
        public PWalkRequest(Direction direction, byte seq, bool run, uint fastwalk) : base(0x02)
        {
            if (run)
            {
                direction |= Direction.Running;
            }

            WriteByte((byte) direction);
            WriteByte(seq);
            WriteUInt(fastwalk);
        }
    }

    internal sealed class PCustomHouseBackup : PacketWriter
    {
        public PCustomHouseBackup() : base(0xD7)
        {
            WriteUInt(World.Player);
            WriteUShort(0x02);
            WriteByte(0x0A);
        }
    }

    internal sealed class PCustomHouseRestore : PacketWriter
    {
        public PCustomHouseRestore() : base(0xD7)
        {
            WriteUInt(World.Player);
            WriteUShort(0x03);
            WriteByte(0x0A);
        }
    }

    internal sealed class PCustomHouseCommit : PacketWriter
    {
        public PCustomHouseCommit() : base(0xD7)
        {
            WriteUInt(World.Player);
            WriteUShort(0x04);
            WriteByte(0x0A);
        }
    }

    internal sealed class PCustomHouseBuildingExit : PacketWriter
    {
        public PCustomHouseBuildingExit() : base(0xD7)
        {
            WriteUInt(World.Player);
            WriteUShort(0x0C);
            WriteByte(0x0A);
        }
    }

    internal sealed class PCustomHouseGoToFloor : PacketWriter
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

    internal sealed class PCustomHouseSync : PacketWriter
    {
        public PCustomHouseSync() : base(0xD7)
        {
            WriteUInt(World.Player);
            WriteUShort(0x0E);
            WriteByte(0x0A);
        }
    }

    internal sealed class PCustomHouseClear : PacketWriter
    {
        public PCustomHouseClear() : base(0xD7)
        {
            WriteUInt(World.Player);
            WriteUShort(0x10);
            WriteByte(0x0A);
        }
    }

    internal sealed class PCustomHouseRevert : PacketWriter
    {
        public PCustomHouseRevert() : base(0xD7)
        {
            WriteUInt(World.Player);
            WriteUShort(0x1A);
            WriteByte(0x0A);
        }
    }

    internal sealed class PCustomHouseResponse : PacketWriter
    {
        public PCustomHouseResponse() : base(0xD7)
        {
            WriteUInt(World.Player);
            WriteUShort(0x0A);
            WriteByte(0x0A);
        }
    }

    internal sealed class PCustomHouseAddItem : PacketWriter
    {
        public PCustomHouseAddItem(ushort graphic, int x, int y) : base(0xD7)
        {
            WriteUInt(World.Player);
            WriteUShort(0x06);
            WriteByte(0);
            WriteUInt(graphic);
            WriteByte(0);
            WriteUInt((uint) x);
            WriteByte(0);
            WriteUInt((uint) y);
            WriteByte(0x0A);
        }
    }

    internal sealed class PCustomHouseDeleteItem : PacketWriter
    {
        public PCustomHouseDeleteItem(ushort graphic, int x, int y, int z) : base(0xD7)
        {
            WriteUInt(World.Player);
            WriteUShort(0x05);
            WriteByte(0);
            WriteUInt(graphic);
            WriteByte(0);
            WriteUInt((uint) x);
            WriteByte(0);
            WriteUInt((uint) y);
            WriteByte(0);
            WriteUInt((uint) z);
            WriteByte(0x0A);
        }
    }

    internal sealed class PCustomHouseAddRoof : PacketWriter
    {
        public PCustomHouseAddRoof(ushort graphic, int x, int y, int z) : base(0xD7)
        {
            WriteUInt(World.Player);
            WriteUShort(0x13);
            WriteByte(0);
            WriteUInt(graphic);
            WriteByte(0);
            WriteUInt((uint) x);
            WriteByte(0);
            WriteUInt((uint) y);
            WriteByte(0);
            WriteUInt((uint) z);
            WriteByte(0x0A);
        }
    }

    internal sealed class PCustomHouseDeleteRoof : PacketWriter
    {
        public PCustomHouseDeleteRoof(ushort graphic, int x, int y, int z) : base(0xD7)
        {
            WriteUInt(World.Player);
            WriteUShort(0x14);
            WriteByte(0);
            WriteUInt(graphic);
            WriteByte(0);
            WriteUInt((uint) x);
            WriteByte(0);
            WriteUInt((uint) y);
            WriteByte(0);
            WriteUInt((uint) z);
            WriteByte(0x0A);
        }
    }

    internal sealed class PCustomHouseAddStair : PacketWriter
    {
        public PCustomHouseAddStair(ushort graphic, int x, int y) : base(0xD7)
        {
            WriteUInt(World.Player);
            WriteUShort(0x0D);
            WriteByte(0);
            WriteUInt(graphic);
            WriteByte(0);
            WriteUInt((uint) x);
            WriteByte(0);
            WriteUInt((uint) y);
            WriteByte(0x0A);
        }
    }

    internal sealed class PPing : PacketWriter
    {
        public PPing() : base(0x73)
        {
            WriteByte(0);
        }
    }

    internal sealed class PClientViewRange : PacketWriter
    {
        public PClientViewRange(byte range) : base(0xC8)
        {
            if (range < Constants.MIN_VIEW_RANGE)
            {
                range = Constants.MIN_VIEW_RANGE;
            }
            else if (range > Constants.MAX_VIEW_RANGE)
            {
                range = Constants.MAX_VIEW_RANGE;
            }

            WriteByte(range);
        }
    }

    internal sealed class POpenUOStore : PacketWriter
    {
        public POpenUOStore() : base(0xFA)
        {
        }
    }

    internal sealed class PShowPublicHouseContent : PacketWriter
    {
        public PShowPublicHouseContent(bool show) : base(0xFB)
        {
            WriteBool(show);
        }
    }
}