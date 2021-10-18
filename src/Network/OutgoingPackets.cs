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
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ClassicUO.Configuration;
using ClassicUO.Data;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.IO;
using ClassicUO.IO.Resources;

namespace ClassicUO.Network
{
    internal static class NetClientExt
    {
        public static void Send_ACKTalk(this NetClient socket)
        {
            const byte ID = 0x03;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8(0x20);
            writer.WriteUInt8(0x00);
            writer.WriteUInt8(0x34);
            writer.WriteUInt8(0x00);
            writer.WriteUInt8(0x03);
            writer.WriteUInt8(0xdb);
            writer.WriteUInt8(0x13);
            writer.WriteUInt8(0x14);
            writer.WriteUInt8(0x3f);
            writer.WriteUInt8(0x45);
            writer.WriteUInt8(0x2c);
            writer.WriteUInt8(0x58);
            writer.WriteUInt8(0x0f);
            writer.WriteUInt8(0x5d);
            writer.WriteUInt8(0x44);
            writer.WriteUInt8(0x2e);
            writer.WriteUInt8(0x50);
            writer.WriteUInt8(0x11);
            writer.WriteUInt8(0xdf);
            writer.WriteUInt8(0x75);
            writer.WriteUInt8(0x5c);
            writer.WriteUInt8(0xe0);
            writer.WriteUInt8(0x3e);
            writer.WriteUInt8(0x71);
            writer.WriteUInt8(0x4f);
            writer.WriteUInt8(0x31);
            writer.WriteUInt8(0x34);
            writer.WriteUInt8(0x05);
            writer.WriteUInt8(0x4e);
            writer.WriteUInt8(0x18);
            writer.WriteUInt8(0x1e);
            writer.WriteUInt8(0x72);
            writer.WriteUInt8(0x0f);
            writer.WriteUInt8(0x59);
            writer.WriteUInt8(0xad);
            writer.WriteUInt8(0xf5);
            writer.WriteUInt8(0x00);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);

            writer.Dispose();
        }

        public static void Send_Ping(this NetClient socket)
        {
            const byte ID = 0x73;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8(0x00);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);

            writer.Dispose();
        }

        public static void Send_DoubleClick(this NetClient socket, uint serial)
        {
            const byte ID = 0x06;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(serial);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);

            writer.Dispose();
        }

        public static void Send_Seed
        (
            this NetClient socket,
            uint v,
            byte major,
            byte minor,
            byte build,
            byte extra
        )
        {
            const byte ID = 0xEF;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(v);
            writer.WriteUInt32BE(major);
            writer.WriteUInt32BE(minor);
            writer.WriteUInt32BE(build);
            writer.WriteUInt32BE(extra);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten, true, true);

            writer.Dispose();
        }

        public static void Send_Seed_Old(this NetClient socket, uint v)
        {
            StackDataWriter writer = new StackDataWriter(4);
            writer.WriteUInt32BE(v);

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten, true, true);

            writer.Dispose();
        }

        public static void Send_FirstLogin(this NetClient socket, string user, string psw)
        {
            const byte ID = 0x80;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteASCII(user, 30);
            writer.WriteASCII(psw, 30);
            writer.WriteUInt8(0xFF);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);

            writer.Dispose();
        }

        public static void Send_SelectServer(this NetClient socket, byte index)
        {
            const byte ID = 0xA0;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8(0x00);
            writer.WriteUInt8(index);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);

            writer.Dispose();
        }

        public static void Send_SecondLogin(this NetClient socket, string user, string psw, uint seed)
        {
            const byte ID = 0x91;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(seed);
            writer.WriteASCII(user, 30);
            writer.WriteASCII(psw, 30);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);

            writer.Dispose();
        }

        public static void Send_CreateCharacter
        (
            this NetClient socket,
            PlayerMobile character,
            int cityIndex,
            uint clientIP,
            int serverIndex,
            uint slot,
            byte profession
        )
        {
            const byte ID = 0x00;
            const byte ID_NEW = 0xF8;

            byte id = ID;
            int skillcount = 3;

            if (Client.Version >= ClientVersion.CV_70160)
            {
                id = ID_NEW;
                ++skillcount;
            }

            int length = PacketsTable.GetPacketLength(id);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(id);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(0xEDEDEDED);
            writer.WriteUInt32BE(0xFFFF_FFFF);
            writer.WriteUInt8(0x00);
            writer.WriteASCII(character.Name, 30);
            writer.WriteZero(2);

            writer.WriteUInt32BE((uint) Client.Protocol);
            writer.WriteUInt32BE(0x01);
            writer.WriteUInt32BE(0x00);
            writer.WriteUInt8(profession);
            writer.WriteZero(15);

            byte val;

            if (Client.Version < ClientVersion.CV_4011D)
            {
                val = (byte) (character.Flags.HasFlag(Flags.Female) ? 0x01 : 0x00);
            }
            else
            {
                val = (byte) character.Race;

                if (Client.Version < ClientVersion.CV_7000)
                {
                    val--;
                }

                val = (byte) (val * 2 + (byte) (character.Flags.HasFlag(Flags.Female) ? 0x01 : 0x00));
            }

            writer.WriteUInt8(val);
            writer.WriteUInt8((byte) character.Strength);
            writer.WriteUInt8((byte) character.Dexterity);
            writer.WriteUInt8((byte) character.Intelligence);

            List<Skill> skills = character.Skills.OrderByDescending(o => o.Value).Take(skillcount).ToList();

            foreach (Skill skill in skills)
            {
                writer.WriteUInt8((byte) skill.Index);
                writer.WriteUInt8((byte) skill.ValueFixed);
            }

            writer.WriteUInt16BE(character.Hue);

            Item hair = character.FindItemByLayer(Layer.Hair);

            if (hair != null)
            {
                writer.WriteUInt16BE(hair.Graphic);
                writer.WriteUInt16BE(hair.Hue);
            }
            else
            {
                writer.WriteZero(2 * 2);
            }

            Item beard = character.FindItemByLayer(Layer.Beard);

            if (beard != null)
            {
                writer.WriteUInt16BE(beard.Graphic);
                writer.WriteUInt16BE(beard.Hue);
            }
            else
            {
                writer.WriteZero(2 * 2);
            }

            writer.WriteUInt16BE((ushort) cityIndex);
            writer.WriteZero(2);
            writer.WriteUInt16BE((ushort) slot);
            writer.WriteUInt32BE(clientIP);

            Item shirt = character.FindItemByLayer(Layer.Shirt);

            if (shirt != null)
            {
                writer.WriteUInt16BE(shirt.Hue);
            }
            else
            {
                writer.WriteZero(2);
            }

            Item pants = character.FindItemByLayer(Layer.Pants);

            if (pants != null)
            {
                writer.WriteUInt16BE(pants.Hue);
            }
            else
            {
                writer.WriteZero(2);
            }

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);

            writer.Dispose();
        }

        public static void Send_DeleteCharacter(this NetClient socket, byte index, uint ipclient)
        {
            const byte ID = 0x83;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteZero(30);
            writer.WriteUInt32BE(index);
            writer.WriteUInt32BE(ipclient);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);

            writer.Dispose();
        }

        public static void Send_SelectCharacter(this NetClient socket, uint index, string name, uint ipclient)
        {
            const byte ID = 0x5D;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(0xEDEDEDED);
            writer.WriteASCII(name, 30);
            writer.WriteZero(2);
            writer.WriteUInt32BE((uint) Client.Protocol);
            writer.WriteZero(24);
            writer.WriteUInt32BE(index);
            writer.WriteUInt32BE(ipclient);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);

            writer.Dispose();
        }

        public static void Send_PickUpRequest(this NetClient socket, uint serial, ushort count)
        {
            const byte ID = 0x07;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(serial);
            writer.WriteUInt16BE(count);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_DropRequest_Old
        (
            this NetClient socket,
            uint serial,
            ushort x,
            ushort y,
            sbyte z,
            uint container
        )
        {
            const byte ID = 0x08;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(serial);
            writer.WriteUInt16BE(x);
            writer.WriteUInt16BE(y);
            writer.WriteInt8(z);
            writer.WriteUInt32BE(container);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_DropRequest
        (
            this NetClient socket,
            uint serial,
            ushort x,
            ushort y,
            sbyte z,
            byte slot,
            uint container
        )
        {
            const byte ID = 0x08;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(serial);
            writer.WriteUInt16BE(x);
            writer.WriteUInt16BE(y);
            writer.WriteInt8(z);
            writer.WriteUInt8(slot);
            writer.WriteUInt32BE(container);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_EquipRequest(this NetClient socket, uint serial, Layer layer, uint container)
        {
            const byte ID = 0x13;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(serial);
            writer.WriteUInt8((byte) layer);
            writer.WriteUInt32BE(container);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_ChangeWarMode(this NetClient socket, bool state)
        {
            const byte ID = 0x72;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteBool(state);
            writer.WriteUInt8(0x32);
            writer.WriteUInt8(0x00);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_HelpRequest(this NetClient socket)
        {
            const byte ID = 0x9B;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteZero(257);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_StatusRequest(this NetClient socket, uint serial)
        {
            const byte ID = 0x34;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(0xEDEDEDED);
            writer.WriteUInt8(0x04);
            writer.WriteUInt32BE(serial);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_SkillsRequest(this NetClient socket, uint serial)
        {
            const byte ID = 0x34;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(0xEDEDEDED);
            writer.WriteUInt8(0x05);
            writer.WriteUInt32BE(serial);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_SkillsStatusRequest(this NetClient socket, ushort skillIndex, byte lockState)
        {
            const byte ID = 0x3A;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(skillIndex);
            writer.WriteUInt8(lockState);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_ClickRequest(this NetClient socket, uint serial)
        {
            const byte ID = 0x09;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(serial);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_AttackRequest(this NetClient socket, uint serial)
        {
            const byte ID = 0x05;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(serial);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_ClientVersion(this NetClient socket, string version)
        {
            const byte ID = 0xBD;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteASCII(version);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_ASCIISpeechRequest(this NetClient socket, string text, MessageType type, byte font, ushort hue)
        {
            const byte ID = 0x03;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            List<SpeechEntry> entries = SpeechesLoader.Instance.GetKeywords(text);
            bool encoded = entries != null && entries.Count != 0;

            if (encoded)
            {
                type |= MessageType.Encoded;
            }

            writer.WriteUInt8((byte) type);
            writer.WriteUInt16BE(hue);
            writer.WriteUInt16BE(font);
            writer.WriteASCII(text);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_UnicodeSpeechRequest
        (
            this NetClient socket,
            string text,
            MessageType type,
            byte font,
            ushort hue,
            string lang
        )
        {
            const byte ID = 0xAD;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            List<SpeechEntry> entries = SpeechesLoader.Instance.GetKeywords(text);
            bool encoded = entries != null && entries.Count != 0;

            if (encoded)
            {
                type |= MessageType.Encoded;
            }

            writer.WriteUInt8((byte) type);
            writer.WriteUInt16BE(hue);
            writer.WriteUInt16BE(font);
            writer.WriteASCII(lang, 4);

            if (encoded)
            {
                List<byte> codeBytes = new List<byte>();
                byte[] utf8 = Encoding.UTF8.GetBytes(text);
                int len = entries.Count;
                codeBytes.Add((byte) (len >> 4));
                int num3 = len & 15;
                bool flag = false;
                int index = 0;

                while (index < len)
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

                for (int i = 0; i < codeBytes.Count; ++i)
                {
                    writer.WriteUInt8(codeBytes[i]);
                }

                writer.Write(utf8);
                writer.WriteUInt8(0x00);
            }
            else
            {
                writer.WriteUnicodeBE(text);
            }

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_CastSpell(this NetClient socket, int idx)
        {
            const byte ID = 0xBF;
            const byte ID_OLD = 0x12;

            byte id = ID;

            if (Client.Version < ClientVersion.CV_60142)
            {
                id = ID_OLD;
            }

            int length = PacketsTable.GetPacketLength(id);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(id);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            if (Client.Version >= ClientVersion.CV_60142)
            {
                writer.WriteUInt16BE(0x1C);
                writer.WriteUInt16BE(0x02);
                writer.WriteUInt16BE((ushort) idx);
            }
            else
            {
                writer.WriteUInt8(0x56);
                writer.WriteASCII(idx.ToString());
            }

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_CastSpellFromBook(this NetClient socket, int idx, uint serial)
        {
            const byte ID = 0x12;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8(0x27);
            writer.WriteASCII($"{idx} {serial}");

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_UseSkill(this NetClient socket, int idx)
        {
            const byte ID = 0x12;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8(0x24);
            writer.WriteASCII($"{idx} 0");

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_OpenDoor(this NetClient socket)
        {
            const byte ID = 0x12;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8(0x58);
            writer.WriteUInt8(0x00);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_OpenSpellBook(this NetClient socket, byte type)
        {
            const byte ID = 0x12;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8(0x43);
            writer.WriteUInt8(type);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_EmoteAction(this NetClient socket, string action)
        {
            const byte ID = 0x12;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8(0xC7);
            writer.WriteASCII(action);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_GumpResponse
        (
            this NetClient socket,
            uint local,
            uint server,
            int button,
            uint[] switches,
            Tuple<ushort, string>[] entries
        )
        {
            const byte ID = 0xB1;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(local);
            writer.WriteUInt32BE(server);
            writer.WriteUInt32BE((uint) button);

            writer.WriteUInt32BE((uint) switches.Length);

            for (int i = 0; i < switches.Length; ++i)
            {
                writer.WriteUInt32BE(switches[i]);
            }

            writer.WriteUInt32BE((uint) entries.Length);

            for (int i = 0; i < entries.Length; ++i)
            {
                int len = Math.Min(239, entries[i].Item2.Length);

                writer.WriteUInt16BE(entries[i].Item1);
                writer.WriteUInt16BE((ushort) len);
                writer.WriteUnicodeBE(entries[i].Item2, len);
            }

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_VirtueGumpResponse(this NetClient socket, uint serial, uint code)
        {
            const byte ID = 0xB1;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(serial);
            writer.WriteUInt32BE(0x000001CD);
            writer.WriteUInt32BE(code);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_MenuResponse
        (
            this NetClient socket,
            uint serial,
            ushort graphic,
            int code,
            ushort itemGraphic,
            ushort itemHue
        )
        {
            const byte ID = 0x7D;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(serial);
            writer.WriteUInt16BE(graphic);

            if (code != 0)
            {
                writer.WriteUInt16BE((ushort) code);
                writer.WriteUInt16BE(itemGraphic);
                writer.WriteUInt16BE(itemHue);
            }

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }


            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_GrayMenuResponse(this NetClient socket, uint serial, ushort graphic, ushort code)
        {
            const byte ID = 0x7D;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(serial);
            writer.WriteUInt16BE(graphic);
            writer.WriteUInt16BE(code);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_TradeResponse(this NetClient socket, uint serial, int code, bool state)
        {
            const byte ID = 0x6F;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            if (code == 1)
            {
                writer.WriteUInt8(0x01);
                writer.WriteUInt32BE(serial);
            }
            else if (code == 2)
            {
                writer.WriteUInt8(0x02);
                writer.WriteUInt32BE(serial);
                writer.WriteUInt32BE((uint) (state ? 1 : 0));
            }
            else
            {
                writer.Dispose();

                return;
            }

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_TradeUpdateGold(this NetClient socket, uint serial, uint gold, uint platinum)
        {
            const byte ID = 0x6F;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8(0x03);
            writer.WriteUInt32BE(serial);
            writer.WriteUInt32BE(gold);
            writer.WriteUInt32BE(platinum);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_LogoutNotification(this NetClient socket)
        {
            const byte ID = 0xD1;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8(0x00);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_TextEntryDialogResponse
        (
            this NetClient socket,
            uint serial,
            byte parentID,
            byte button,
            string text,
            bool code
        )
        {
            const byte ID = 0xAC;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(serial);
            writer.WriteUInt8(parentID);
            writer.WriteUInt8(button);
            writer.WriteBool(code);
            writer.WriteUInt16BE((ushort) (text.Length + 1));
            writer.WriteASCII(text, text.Length + 1);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_RenameRequest(this NetClient socket, uint serial, string name)
        {
            const byte ID = 0x75;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(serial);
            writer.WriteASCII(name, 30);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_NameRequest(this NetClient socket, uint serial)
        {
            const byte ID = 0x98;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(serial);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_TipRequest(this NetClient socket, ushort id, byte flag)
        {
            const byte ID = 0xA7;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(id);
            writer.WriteUInt8(flag);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_TargetObject
        (
            this NetClient socket,
            uint entity,
            ushort graphic,
            ushort x,
            ushort y,
            sbyte z,
            uint cursorID,
            byte cursorType
        )
        {
            const byte ID = 0x6C;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8(0x00);
            writer.WriteUInt32BE(cursorID);
            writer.WriteUInt8(cursorType);
            writer.WriteUInt32BE(entity);
            writer.WriteUInt16BE(x);
            writer.WriteUInt16BE(y);
            writer.WriteUInt16BE((ushort) z);
            writer.WriteUInt16BE(graphic);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_TargetXYZ
        (
            this NetClient socket,
            ushort graphic,
            ushort x,
            ushort y,
            sbyte z,
            uint cursorID,
            byte cursorType
        )
        {
            const byte ID = 0x6C;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8(0x01);
            writer.WriteUInt32BE(cursorID);
            writer.WriteUInt8(cursorType);
            writer.WriteUInt32BE(0x00);
            writer.WriteUInt16BE(x);
            writer.WriteUInt16BE(y);
            writer.WriteUInt16BE((ushort) z);
            writer.WriteUInt16BE(graphic);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_TargetCancel(this NetClient socket, CursorTarget type, uint cursorID, byte cursorType)
        {
            const byte ID = 0x6C;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8((byte) type);
            writer.WriteUInt32BE(cursorID);
            writer.WriteUInt8(cursorType);
            writer.WriteUInt32BE(0x00);
            writer.WriteUInt32BE(0xFFFF_FFFF);
            writer.WriteUInt32BE(0x0000_0000);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_ASCIIPromptResponse(this NetClient socket, string text, bool cancel)
        {
            const byte ID = 0x9A;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt64BE(MessageManager.PromptData.Data);
            writer.WriteUInt32BE((uint) (cancel ? 0 : 1));
            writer.WriteASCII(text);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_UnicodePromptResponse(this NetClient socket, string text, string lang, bool cancel)
        {
            const byte ID = 0xC2;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt64BE(MessageManager.PromptData.Data);
            writer.WriteUInt32BE((uint) (cancel ? 0 : 1));
            writer.WriteASCII(lang);
            writer.WriteUnicodeLE(text, text.Length);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_DyeDataResponse(this NetClient socket, uint serial, ushort graphic, ushort hue)
        {
            const byte ID = 0x95;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(serial);
            writer.WriteUInt16BE(graphic);
            writer.WriteUInt16BE(hue);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_ProfileRequest(this NetClient socket, uint serial)
        {
            const byte ID = 0xB8;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8(0x00);
            writer.WriteUInt32BE(serial);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_ProfileUpdate(this NetClient socket, uint serial, string text)
        {
            const byte ID = 0xB8;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8(0x01);
            writer.WriteUInt32BE(serial);
            writer.WriteUInt16BE(0x01);
            writer.WriteUInt16BE((ushort) text.Length);
            writer.WriteUnicodeBE(text, text.Length);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_ClickQuestArrow(this NetClient socket, bool righClick)
        {
            const byte ID = 0xBF;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0x07);
            writer.WriteBool(righClick);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_CloseStatusBarGump(this NetClient socket, uint serial)
        {
            const byte ID = 0xBF;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0x0C);
            writer.WriteUInt32BE(serial);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_PartyInviteRequest(this NetClient socket)
        {
            const byte ID = 0xBF;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0x06);
            writer.WriteUInt8(1);
            writer.WriteUInt32BE(0);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_PartyRemoveRequest(this NetClient socket, uint serial)
        {
            const byte ID = 0xBF;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0x06);
            writer.WriteUInt8(2);
            writer.WriteUInt32BE(serial);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_PartyChangeLootTypeRequest(this NetClient socket, bool type)
        {
            const byte ID = 0xBF;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0x06);
            writer.WriteUInt8(0x06);
            writer.WriteBool(type);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_PartyAccept(this NetClient socket, uint serial)
        {
            const byte ID = 0xBF;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0x06);
            writer.WriteUInt8(0x08);
            writer.WriteUInt32BE(serial);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_PartyDecline(this NetClient socket, uint serial)
        {
            const byte ID = 0xBF;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0x06);
            writer.WriteUInt8(0x09);
            writer.WriteUInt32BE(serial);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }


            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_PartyMessage(this NetClient socket, string text, uint serial)
        {
            const byte ID = 0xBF;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }


            writer.WriteUInt16BE(0x06);

            if (SerialHelper.IsValid(serial))
            {
                writer.WriteUInt8(0x03);
                writer.WriteUInt32BE(serial);
            }
            else
            {
                writer.WriteUInt8(0x04);
            }

            writer.WriteUnicodeBE(text);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_GameWindowSize(this NetClient socket, uint w, uint h)
        {
            const byte ID = 0xBF;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0x05);
            writer.WriteUInt32BE(w);
            writer.WriteUInt32BE(h);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_BulletinBoardRequestMessage(this NetClient socket, uint serial, uint msgSerial)
        {
            const byte ID = 0x71;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8(0x03);
            writer.WriteUInt32BE(serial);
            writer.WriteUInt32BE(msgSerial);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_BulletinBoardRequestMessageSummary(this NetClient socket, uint serial, uint msgSerial)
        {
            const byte ID = 0x71;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8(0x04);
            writer.WriteUInt32BE(serial);
            writer.WriteUInt32BE(msgSerial);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_BulletinBoardPostMessage(this NetClient socket, uint serial, uint msgSerial, string subject, string text)
        {
            const byte ID = 0x71;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8(0x05);
            writer.WriteUInt32BE(serial);
            writer.WriteUInt32BE(msgSerial);
            writer.WriteUInt8((byte) (subject.Length + 1));

            byte[] title = Encoding.UTF8.GetBytes(subject);
            writer.Write(title);
            writer.WriteUInt8(0x00);

            int numOfLinesPosition = writer.Position;
            int numOfLines = 0;

            for (int i = 0, startIndex = 0; i < text.Length; ++i)
            {
                if (text[i] == '\n')
                {
                    ++numOfLines;

                    int len = i - startIndex;

                    if (len > 0)
                    {
                        byte[] buffer = ArrayPool<byte>.Shared.Rent(len * 2);

                        try
                        {
                            int written = Encoding.UTF8.GetBytes
                            (
                                text,
                                startIndex,
                                len,
                                buffer,
                                0
                            );

                            writer.WriteUInt8((byte) (written + 1));
                            writer.Write(buffer.AsSpan(0, written));
                            writer.WriteUInt8(0x00);
                        }
                        finally
                        {
                            ArrayPool<byte>.Shared.Return(buffer);
                        }
                    }
                    else
                    {
                        writer.WriteUInt8(0x01);
                        writer.WriteUInt8(0x00);
                    }

                    startIndex = i;
                }
            }

            if (numOfLines == 0)
            {
                writer.WriteUInt8((byte) (text.Length + 1));
                writer.WriteASCII(text);
                writer.WriteUInt8(0x00);
            }

            writer.Seek(numOfLinesPosition, SeekOrigin.Begin);
            writer.WriteUInt8((byte) numOfLines);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }


            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_BulletinBoardRemoveMessage(this NetClient socket, uint serial, uint msgSerial)
        {
            const byte ID = 0x71;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8(0x06);
            writer.WriteUInt32BE(serial);
            writer.WriteUInt32BE(msgSerial);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }


            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_RazorACK(this NetClient socket)
        {
            const byte ID = 0xF0;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8(0xFF);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_QueryGuildPosition(this NetClient socket)
        {
            const byte ID = 0xF0;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8(0x01);
            writer.WriteBool(true);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_QueryPartyPosition(this NetClient socket)
        {
            const byte ID = 0xF0;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8(0x00);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_Language(this NetClient socket, string lang)
        {
            const byte ID = 0xBF;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0x0B);
            writer.WriteASCII(lang, 3);
            writer.WriteUInt8(0x00);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_ClientType(this NetClient socket)
        {
            const byte ID = 0xBF;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0x0F);
            writer.WriteUInt8(0x0A);

            uint clientFlag = 0;

            for (int i = 0; i < (uint) Client.Protocol; ++i)
            {
                clientFlag |= (uint) (1 << i);
            }


            writer.WriteUInt32BE(clientFlag);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }


            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_RequestPopupMenu(this NetClient socket, uint serial)
        {
            const byte ID = 0xBF;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0x13);
            writer.WriteUInt32BE(serial);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_PopupMenuSelection(this NetClient socket, uint serial, ushort menuid)
        {
            const byte ID = 0xBF;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0x15);
            writer.WriteUInt32BE(serial);
            writer.WriteUInt16BE(menuid);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_ChatJoinCommand(this NetClient socket, string name, string password = null)
        {
            const byte ID = 0xB3;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteASCII(Settings.GlobalSettings.Language, 4);
            writer.WriteUInt16BE(0x62);
            writer.WriteUInt16BE(0x22);
            writer.WriteUnicodeBE(name);
            writer.WriteUInt16BE(0x22);
            writer.WriteUInt16BE(0x020);

            if (!string.IsNullOrEmpty(password))
            {
                writer.WriteUnicodeBE(password);
            }

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }


            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_ChatCreateChannelCommand(this NetClient socket, string name, string password = null)
        {
            const byte ID = 0xB3;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteASCII(Settings.GlobalSettings.Language, 4);
            writer.WriteUInt16BE(0x63);
            writer.WriteUnicodeBE(name);

            if (!string.IsNullOrEmpty(password))
            {
                writer.WriteUInt16BE(0x7B);
                writer.WriteUnicodeBE(password);
                writer.WriteUInt16BE(0x07D);
            }

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_ChatLeaveChannelCommand(this NetClient socket)
        {
            const byte ID = 0xB3;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteASCII(Settings.GlobalSettings.Language, 4);
            writer.WriteUInt16BE(0x43);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_ChatMessageCommand(this NetClient socket, string msg)
        {
            const byte ID = 0xB3;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteASCII(Settings.GlobalSettings.Language, 4);
            writer.WriteUInt16BE(0x61);
            writer.WriteUnicodeBE(msg);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }


            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_OpenChat(this NetClient socket, string name)
        {
            const byte ID = 0xB5;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }


            writer.WriteUInt8(0x00);
            int len = Math.Min(name.Length, 30);

            if (len > 0)
            {
                writer.WriteUnicodeBE(name, len);
            }

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_MapMessage
        (
            this NetClient socket,
            uint serial,
            byte action,
            byte pin,
            ushort x,
            ushort y
        )
        {
            const byte ID = 0x56;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(serial);
            writer.WriteUInt8(action);
            writer.WriteUInt8(pin);
            writer.WriteUInt16BE(x);
            writer.WriteUInt16BE(y);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_GuildMenuRequest(this NetClient socket)
        {
            const byte ID = 0xD7;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(World.Player.Serial);
            writer.WriteUInt16BE(0x28);
            writer.WriteUInt8(0x0A);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_QuestMenuRequest(this NetClient socket)
        {
            const byte ID = 0xD7;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(World.Player.Serial);
            writer.WriteUInt16BE(0x32);
            writer.WriteUInt8(0x00);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_EquipLastWeapon(this NetClient socket)
        {
            const byte ID = 0xD7;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(World.Player.Serial);
            writer.WriteUInt16BE(0x1E);
            writer.WriteUInt8(0x0A);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_InvokeVirtueRequest(this NetClient socket, byte id)
        {
            const byte ID = 0x12;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8(0xF4);
            writer.WriteASCII(id.ToString());


            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_MegaClilocRequest_Old(this NetClient socket, uint serial)
        {
            const byte ID = 0xBF;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0x10);
            writer.WriteUInt32BE(serial);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_MegaClilocRequest(this NetClient socket, ref List<uint> serials)
        {
            const byte ID = 0xD6;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            int count = Math.Min(15, serials.Count);

            for (int i = 0; i < count; ++i)
            {
                writer.WriteUInt32BE(serials[i]);
            }

            serials.RemoveRange(0, count);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_StatLockStateRequest(this NetClient socket, byte stat, Lock state)
        {
            const byte ID = 0xBF;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0x1A);
            writer.WriteUInt8(stat);
            writer.WriteUInt8((byte) state);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }


            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_SkillStatusChangeRequest(this NetClient socket, ushort skillindex, byte lockstate)
        {
            const byte ID = 0x3A;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(skillindex);
            writer.WriteUInt8(lockstate);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_BookHeaderChanged_Old(this NetClient socket, uint serial, string title, string author)
        {
            const byte ID = 0x93;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(serial);
            writer.WriteUInt8(0x00);
            writer.WriteUInt8(0x01);
            writer.WriteUInt16BE(0);
            writer.WriteUTF8(title, 60);
            writer.WriteUTF8(author, 30);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }


            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_BookHeaderChanged(this NetClient socket, uint serial, string title, string author)
        {
            const byte ID = 0xD4;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(serial);
            writer.WriteUInt8(0x00);
            writer.WriteUInt8(0x00);
            writer.WriteUInt16BE(0);
            int titleLength = Encoding.UTF8.GetByteCount(title);
            writer.WriteUInt16BE((ushort) titleLength);
            writer.WriteUTF8(title, titleLength);
            int authorLength = Encoding.UTF8.GetByteCount(author);
            writer.WriteUInt16BE((ushort) authorLength);
            writer.WriteUTF8(author, authorLength);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }


            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_BookPageData(this NetClient socket, uint serial, string[] text, int page)
        {
            const byte ID = 0x66;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(serial);
            writer.WriteUInt16BE(0x01);
            writer.WriteUInt16BE((ushort) page);
            writer.WriteUInt16BE((ushort) text.Length);

            for (int i = 0; i < text.Length; ++i)
            {
                if (!string.IsNullOrEmpty(text[i]))
                {
                    string t = text[i].Replace("\n", "");

                    if (t.Length > 0)
                    {
                        byte[] buffer = ArrayPool<byte>.Shared.Rent(t.Length * 2);//we have to assume we are using all two byte chars

                        try
                        {
                            int written = Encoding.UTF8.GetBytes
                            (
                                t,
                                0,
                                t.Length,
                                buffer,
                                0
                            );

                            writer.Write(buffer.AsSpan(0, written));
                        }
                        finally
                        {
                            ArrayPool<byte>.Shared.Return(buffer);
                        }
                    }
                }

                writer.WriteUInt8(0x00);
            }

            writer.WriteUInt8(0x00);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }


            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_BookPageDataRequest(this NetClient socket, uint serial, ushort page)
        {
            const byte ID = 0x66;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(serial);
            writer.WriteUInt16BE(0x01);
            writer.WriteUInt16BE(page);
            writer.WriteUInt16BE(0xFFFF);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }


            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_BuyRequest(this NetClient socket, uint serial, Tuple<uint, ushort>[] items)
        {
            const byte ID = 0x3B;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(serial);

            if (items.Length > 0)
            {
                writer.WriteUInt8(0x02);

                for (int i = 0; i < items.Length; ++i)
                {
                    writer.WriteUInt8(0x1A);
                    writer.WriteUInt32BE(items[i].Item1);
                    writer.WriteUInt16BE(items[i].Item2);
                }
            }
            else
            {
                writer.WriteUInt8(0x00);
            }


            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }


            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_SellRequest(this NetClient socket, uint serial, Tuple<uint, ushort>[] items)
        {
            const byte ID = 0x9F;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(serial);
            writer.WriteUInt16BE((ushort) items.Length);

            for (int i = 0; i < items.Length; ++i)
            {
                writer.WriteUInt32BE(items[i].Item1);
                writer.WriteUInt16BE(items[i].Item2);
            }


            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_UseCombatAbility(this NetClient socket, byte idx)
        {
            const byte ID = 0xD7;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(World.Player.Serial);
            writer.WriteUInt16BE(0x19);
            writer.WriteUInt32BE(0);
            writer.WriteUInt8(idx);
            writer.WriteUInt8(0x0A);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }


            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_TargetSelectedObject(this NetClient socket, uint serial, uint targetSerial)
        {
            const byte ID = 0xBF;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0x2C);
            writer.WriteUInt32BE(serial);
            writer.WriteUInt32BE(targetSerial);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }


            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_ToggleGargoyleFlying(this NetClient socket)
        {
            const byte ID = 0xBF;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0x32);
            writer.WriteUInt16BE(0x01);
            writer.WriteUInt32BE(0);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_CustomHouseDataRequest(this NetClient socket, uint serial)
        {
            const byte ID = 0xBF;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0x1E);
            writer.WriteUInt32BE(serial);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_StunRequest(this NetClient socket)
        {
            const byte ID = 0xBF;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0x09);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_DisarmRequest(this NetClient socket)
        {
            const byte ID = 0xBF;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0x0A);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_ChangeRaceRequest
        (
            this NetClient socket,
            ushort skinHue,
            ushort hairStyle,
            ushort hairHue,
            ushort beardStyle,
            ushort beardHue
        )
        {
            const byte ID = 0xBF;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0x2A);
            writer.WriteUInt16BE(skinHue);
            writer.WriteUInt16BE(hairStyle);
            writer.WriteUInt16BE(hairHue);
            writer.WriteUInt16BE(beardStyle);
            writer.WriteUInt16BE(beardHue);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_MultiBoatMoveRequest(this NetClient socket, uint serial, Direction dir, byte speed)
        {
            const byte ID = 0xBF;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0x33);
            writer.WriteUInt32BE(serial);
            writer.WriteUInt8((byte) dir);
            writer.WriteUInt8((byte) dir);
            writer.WriteUInt8(speed);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_Resync(this NetClient socket)
        {
            const byte ID = 0x22;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }


            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }


            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_WalkRequest(this NetClient socket, Direction direction, byte seq, bool run, uint fastWalk)
        {
            const byte ID = 0x02;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            if (run)
            {
                direction |= Direction.Running;
            }

            writer.WriteUInt8((byte) direction);
            writer.WriteUInt8(seq);
            writer.WriteUInt32BE(fastWalk);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_CustomHouseBackup(this NetClient socket)
        {
            const byte ID = 0xD7;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(World.Player.Serial);
            writer.WriteUInt16BE(0x02);
            writer.WriteUInt8(0x0A);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }


            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_CustomHouseRestore(this NetClient socket)
        {
            const byte ID = 0xD7;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(World.Player.Serial);
            writer.WriteUInt16BE(0x03);
            writer.WriteUInt8(0x0A);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }


            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_CustomHouseCommit(this NetClient socket)
        {
            const byte ID = 0xD7;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(World.Player.Serial);
            writer.WriteUInt16BE(0x04);
            writer.WriteUInt8(0x0A);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }


            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_CustomHouseBuildingExit(this NetClient socket)
        {
            const byte ID = 0xD7;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(World.Player.Serial);
            writer.WriteUInt16BE(0x0C);
            writer.WriteUInt8(0x0A);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_CustomHouseGoToFloor(this NetClient socket, byte floor)
        {
            const byte ID = 0xD7;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(World.Player.Serial);
            writer.WriteUInt16BE(0x12);
            writer.WriteUInt32BE(0);
            writer.WriteUInt8(floor);
            writer.WriteUInt8(0x0A);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }


            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_CustomHouseSync(this NetClient socket)
        {
            const byte ID = 0xD7;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(World.Player.Serial);
            writer.WriteUInt16BE(0x0E);
            writer.WriteUInt8(0x0A);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }


            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_CustomHouseClear(this NetClient socket)
        {
            const byte ID = 0xD7;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(World.Player.Serial);
            writer.WriteUInt16BE(0x10);
            writer.WriteUInt8(0x0A);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }


            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_CustomHouseRevert(this NetClient socket)
        {
            const byte ID = 0xD7;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(World.Player.Serial);
            writer.WriteUInt16BE(0x1A);
            writer.WriteUInt8(0x0A);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_CustomHouseResponse(this NetClient socket)
        {
            const byte ID = 0xD7;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(World.Player.Serial);
            writer.WriteUInt16BE(0x0A);
            writer.WriteUInt8(0x0A);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_CustomHouseAddItem(this NetClient socket, ushort graphic, int x, int y)
        {
            const byte ID = 0xD7;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(World.Player.Serial);
            writer.WriteUInt16BE(0x06);
            writer.WriteUInt8(0x00);
            writer.WriteUInt32BE(graphic);
            writer.WriteUInt8(0x00);
            writer.WriteUInt32BE((uint) x);
            writer.WriteUInt8(0x00);
            writer.WriteUInt32BE((uint) y);
            writer.WriteUInt8(0x0A);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_CustomHouseDeleteItem(this NetClient socket, ushort graphic, int x, int y, int z)
        {
            const byte ID = 0xD7;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(World.Player.Serial);
            writer.WriteUInt16BE(0x05);
            writer.WriteUInt8(0x00);
            writer.WriteUInt32BE(graphic);
            writer.WriteUInt8(0x00);
            writer.WriteUInt32BE((uint) x);
            writer.WriteUInt8(0x00);
            writer.WriteUInt32BE((uint) y);
            writer.WriteUInt8(0x00);
            writer.WriteUInt32BE((uint) z);
            writer.WriteUInt8(0x0A);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_CustomHouseAddRoof(this NetClient socket, ushort graphic, int x, int y, int z)
        {
            const byte ID = 0xD7;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(World.Player.Serial);
            writer.WriteUInt16BE(0x13);
            writer.WriteUInt8(0x00);
            writer.WriteUInt32BE(graphic);
            writer.WriteUInt8(0x00);
            writer.WriteUInt32BE((uint) x);
            writer.WriteUInt8(0x00);
            writer.WriteUInt32BE((uint) y);
            writer.WriteUInt8(0x00);
            writer.WriteUInt32BE((uint) z);
            writer.WriteUInt8(0x0A);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_CustomHouseDeleteRoof(this NetClient socket, ushort graphic, int x, int y, int z)
        {
            const byte ID = 0xD7;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(World.Player.Serial);
            writer.WriteUInt16BE(0x14);
            writer.WriteUInt8(0x00);
            writer.WriteUInt32BE(graphic);
            writer.WriteUInt8(0x00);
            writer.WriteUInt32BE((uint) x);
            writer.WriteUInt8(0x00);
            writer.WriteUInt32BE((uint) y);
            writer.WriteUInt8(0x00);
            writer.WriteUInt32BE((uint) z);
            writer.WriteUInt8(0x0A);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_CustomHouseAddStair(this NetClient socket, ushort graphic, int x, int y)
        {
            const byte ID = 0xD7;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(World.Player.Serial);
            writer.WriteUInt16BE(0x0D);
            writer.WriteUInt8(0x00);
            writer.WriteUInt32BE(graphic);
            writer.WriteUInt8(0x00);
            writer.WriteUInt32BE((uint) x);
            writer.WriteUInt8(0x00);
            writer.WriteUInt32BE((uint) y);
            writer.WriteUInt8(0x0A);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_ClientViewRange(this NetClient socket, byte range)
        {
            const byte ID = 0xC8;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            if (range < Constants.MIN_VIEW_RANGE)
            {
                range = Constants.MIN_VIEW_RANGE;
            }
            else if (range > Constants.MAX_VIEW_RANGE)
            {
                range = Constants.MAX_VIEW_RANGE;
            }

            writer.WriteUInt8(range);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_OpenUOStore(this NetClient socket)
        {
            const byte ID = 0xFA;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_ShowPublicHouseContent(this NetClient socket, bool show)
        {
            const byte ID = 0xFB;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteBool(show);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }

        public static void Send_DeathScreen(this NetClient socket)
        {
            const byte ID = 0x2C;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8(0x02); // Ghost

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }


        public static void Send_UOLive_HashResponse(this NetClient socket, uint block, byte mapIndex, Span<ushort> checksums)
        {
            const byte ID = 0x3F;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(block);
            writer.WriteZero(6);
            writer.WriteUInt8(0xFF);
            writer.WriteUInt8(mapIndex);

            for (int i = 0; i < checksums.Length; ++i)
            {
                writer.WriteUInt16BE(checksums[i]);
            }

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.AllocatedBuffer, writer.BytesWritten);
            writer.Dispose();
        }


        public static void Send_ToPlugins_AllSpells(this NetClient socket)
        {
            const byte ID = 0xBF;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0xBEEF);
            writer.WriteUInt8(0x00);

            void writeDef(IReadOnlyDictionary<int, SpellDefinition> dict, ref StackDataWriter w)
            {
                w.WriteUInt16BE((ushort)dict.Count);

                foreach (KeyValuePair<int, SpellDefinition> m in dict)
                {
                    // spell id
                    w.WriteUInt16BE((ushort)m.Key);

                    // mana cost
                    w.WriteUInt16BE((ushort)m.Value.ManaCost);

                    // min skill
                    w.WriteUInt16BE((ushort)m.Value.MinSkill);

                    // target type
                    w.WriteUInt8((byte)m.Value.TargetType);

                    // spell name
                    w.WriteUInt16BE((ushort)m.Value.Name.Length);
                    w.WriteUnicodeBE(m.Value.Name, m.Value.Name.Length);

                    // power of word
                    w.WriteUInt16BE((ushort)m.Value.PowerWords.Length);
                    w.WriteUnicodeBE(m.Value.PowerWords, m.Value.PowerWords.Length);

                    // reagents
                    w.WriteUInt16BE((ushort)m.Value.Regs.Length);

                    foreach (Reagents r in m.Value.Regs)
                    {
                        w.WriteUInt8((byte)r);
                    }
                }
            }

            writeDef(SpellsMagery.GetAllSpells, ref writer);
            writeDef(SpellsNecromancy.GetAllSpells, ref writer);
            writeDef(SpellsBushido.GetAllSpells, ref writer);
            writeDef(SpellsNinjitsu.GetAllSpells, ref writer);
            writeDef(SpellsChivalry.GetAllSpells, ref writer);
            writeDef(SpellsSpellweaving.GetAllSpells, ref writer);
            writeDef(SpellsMastery.GetAllSpells, ref writer);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            int len = writer.BytesWritten;
            Plugin.ProcessRecvPacket(writer.AllocatedBuffer, ref len);
            writer.Dispose();
        }

        public static void Send_ToPlugins_AllSkills(this NetClient socket)
        {
            const byte ID = 0xBF;

            int length = PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0xBEEF);
            writer.WriteUInt8(0x01);

            writer.WriteUInt16BE((ushort)SkillsLoader.Instance.SortedSkills.Count);

            foreach (SkillEntry s in SkillsLoader.Instance.SortedSkills)
            {
                writer.WriteUInt16BE((ushort) s.Index);
                writer.WriteBool(s.HasAction);

                writer.WriteUInt16BE((ushort) s.Name.Length);
                writer.WriteUnicodeBE(s.Name, s.Name.Length);
            }

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            int len = writer.BytesWritten;
            Plugin.ProcessRecvPacket(writer.AllocatedBuffer, ref len);
            writer.Dispose();
        }
    }
}