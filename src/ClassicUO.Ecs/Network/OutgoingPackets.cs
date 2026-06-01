// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.IO;
using ClassicUO.Assets;
using ClassicUO.Utility;

namespace ClassicUO.Network
{
    internal static class NetClientExt
    {
        public static void Send_ACKTalk(this NetClient socket)
        {
            const byte ID = 0x03;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);
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
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);

            writer.Dispose();
        }

        public static void Send_Ping(this NetClient socket, byte idx)
        {
            const byte ID = 0x73;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8(idx);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);

            writer.Dispose();
        }

        public static void Send_DoubleClick(this NetClient socket, uint serial)
        {
            const byte ID = 0x06;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(serial);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);

            writer.Dispose();
        }

        // 0x6C target response (entity). Mirrors legacy NetClientExt.Send_TargetObject:
        // flag 0x00 = object, echo the server's cursorID + cursorType, then the
        // clicked entity's serial / x / y / z / graphic. z occupies two bytes.
        public static void Send_TargetObject(
            this NetClient socket,
            uint entity,
            ushort graphic,
            ushort x,
            ushort y,
            sbyte z,
            uint cursorID,
            byte cursorType)
        {
            const byte ID = 0x6C;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);
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
            writer.WriteUInt16BE((ushort)z);
            writer.WriteUInt16BE(graphic);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);

            writer.Dispose();
        }

        // 0x6C target response (ground / static). flag 0x01 = position; serial is
        // zero. Land sends graphic 0; a static sends its own graphic.
        public static void Send_TargetXYZ(
            this NetClient socket,
            ushort graphic,
            ushort x,
            ushort y,
            sbyte z,
            uint cursorID,
            byte cursorType)
        {
            const byte ID = 0x6C;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);
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
            writer.WriteUInt16BE((ushort)z);
            writer.WriteUInt16BE(graphic);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);

            writer.Dispose();
        }

        // 0x6C cancel response. flag carries the original cursor mode; the
        // trailing 0xFFFFFFFF / 0x00000000 sentinels mirror legacy Send_TargetCancel.
        public static void Send_TargetCancel(
            this NetClient socket,
            byte cursorTarget,
            uint cursorID,
            byte cursorType)
        {
            const byte ID = 0x6C;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8(cursorTarget);
            writer.WriteUInt32BE(cursorID);
            writer.WriteUInt8(cursorType);
            writer.WriteUInt32BE(0x00);
            writer.WriteUInt32BE(0xFFFF_FFFF);
            writer.WriteUInt32BE(0x0000_0000);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);

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

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);
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
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten, true, true);

            writer.Dispose();
        }

        public static void Send_Seed_Old(this NetClient socket, uint v)
        {
            var writer = new StackDataWriter(4);
            writer.WriteUInt32BE(v);

            socket.Send(writer.BufferWritten, true, true);

            writer.Dispose();
        }

        public static void Send_FirstLogin(this NetClient socket, string user, string psw)
        {
            const byte ID = 0x80;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);
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
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);

            writer.Dispose();
        }

        public static void Send_SelectServer(this NetClient socket, byte index)
        {
            const byte ID = 0xA0;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);
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
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);

            writer.Dispose();
        }

        public static void Send_SecondLogin(this NetClient socket, string user, string psw, uint seed)
        {
            const byte ID = 0x91;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);
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
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);

            writer.Dispose();
        }

        public static void Send_SelectCharacter(this NetClient socket, uint index, string name, uint ipclient, ClientFlags protocol)
        {
            const byte ID = 0x5D;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(0xEDEDEDED);
            writer.WriteASCII(name, 30);
            writer.WriteZero(2);
            writer.WriteUInt32BE((uint)protocol);
            writer.WriteZero(24);
            writer.WriteUInt32BE(index);
            writer.WriteUInt32BE(ipclient);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);

            writer.Dispose();
        }

        public static void Send_PickUpRequest(this NetClient socket, uint serial, ushort count)
        {
            const byte ID = 0x07;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);
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
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);
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

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);
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
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        public static void Send_EquipRequest(this NetClient socket, uint serial, Layer layer, uint container)
        {
            const byte ID = 0x13;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(serial);
            writer.WriteUInt8((byte)layer);
            writer.WriteUInt32BE(container);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        public static void Send_ChangeWarMode(this NetClient socket, bool state)
        {
            const byte ID = 0x72;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);
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
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        public static void Send_HelpRequest(this NetClient socket)
        {
            const byte ID = 0x9B;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteZero(257);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        // 0xBF extended, subcommand 0x1A — cycle a stat's lock (0=up,1=down,2=locked).
        public static void Send_StatLockStateRequest(this NetClient socket, byte stat, byte state)
        {
            const byte ID = 0xBF;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0x1A);
            writer.WriteUInt8(stat);
            writer.WriteUInt8(state);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        public static void Send_StatusRequest(this NetClient socket, uint serial)
        {
            const byte ID = 0x34;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);
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
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        // 0xBF subcommand 0x0C: tell the server we closed a mobile's status/
        // healthbar gump so it stops streaming hp updates for it. Sent when a
        // healthbar goes out of range; the matching Send_StatusRequest re-subs
        // when it comes back. Mirrors legacy Send_CloseStatusBarGump.
        public static void Send_CloseStatusBarGump(this NetClient socket, uint serial)
        {
            const byte ID = 0xBF;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);
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
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        public static void Send_SkillsRequest(this NetClient socket, uint serial)
        {
            const byte ID = 0x34;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);
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
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        // 0x3A — change a skill's lock state (up/down/locked). Mirrors main's
        // Send_SkillStatusChangeRequest.
        public static void Send_SkillStatusChangeRequest(this NetClient socket, ushort skillindex, byte lockstate)
        {
            const byte ID = 0x3A;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);
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
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        // 0x12 type 0x24 — request use of a skill by index. Mirrors main's
        // Send_UseSkill / GameActions.UseSkill.
        public static void Send_UseSkill(this NetClient socket, int idx)
        {
            const byte ID = 0x12;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);
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
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        public static void Send_ClickRequest(this NetClient socket, uint serial)
        {
            const byte ID = 0x09;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(serial);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        public static void Send_ClientVersion(this NetClient socket, string version)
        {
            const byte ID = 0xBD;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteASCII(version);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        public static void Send_ASCIISpeechRequest(this NetClient socket, string text, MessageType type, byte font, ushort hue, List<SpeechEntry> entries)
        {
            const byte ID = 0x03;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            bool encoded = entries != null && entries.Count != 0;

            if (encoded)
            {
                type |= MessageType.Encoded;
            }

            writer.WriteUInt8((byte)type);
            writer.WriteUInt16BE(hue);
            writer.WriteUInt16BE(font);
            writer.WriteASCII(text);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        public static void Send_UnicodeSpeechRequest
        (
            this NetClient socket,
            string text,
            MessageType type,
            byte font,
            ushort hue,
            string lang,
            List<SpeechEntry> entries
        )
        {
            const byte ID = 0xAD;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            bool encoded = entries != null && entries.Count != 0;

            if (encoded)
            {
                type |= MessageType.Encoded;
            }

            writer.WriteUInt8((byte)type);
            writer.WriteUInt16BE(hue);
            writer.WriteUInt16BE(font);
            writer.WriteASCII(lang, 4);

            if (encoded)
            {
                List<byte> codeBytes = new List<byte>();
                byte[] utf8 = Encoding.UTF8.GetBytes(text);
                int len = entries.Count;
                codeBytes.Add((byte)(len >> 4));
                int num3 = len & 15;
                bool flag = false;
                int index = 0;

                while (index < len)
                {
                    int keywordID = entries[index].KeywordID;

                    if (flag)
                    {
                        codeBytes.Add((byte)(keywordID >> 4));
                        num3 = keywordID & 15;
                    }
                    else
                    {
                        codeBytes.Add((byte)((num3 << 4) | ((keywordID >> 8) & 15)));
                        codeBytes.Add((byte)keywordID);
                    }

                    index++;
                    flag = !flag;
                }

                if (!flag)
                {
                    codeBytes.Add((byte)(num3 << 4));
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
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);
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

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(local);
            writer.WriteUInt32BE(server);
            writer.WriteUInt32BE((uint)button);

            writer.WriteUInt32BE((uint)switches.Length);

            for (int i = 0; i < switches.Length; ++i)
            {
                writer.WriteUInt32BE(switches[i]);
            }

            writer.WriteUInt32BE((uint)entries.Length);

            for (int i = 0; i < entries.Length; ++i)
            {
                int len = Math.Min(239, entries[i].Item2.Length);

                writer.WriteUInt16BE(entries[i].Item1);
                writer.WriteUInt16BE((ushort)len);
                writer.WriteUnicodeBE(entries[i].Item2, len);
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

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        public static void Send_LogoutNotification(this NetClient socket)
        {
            const byte ID = 0xD1;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8(0x00);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        public static void Send_ProfileRequest(this NetClient socket, uint serial)
        {
            const byte ID = 0xB8;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);

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
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        public static void Send_GameWindowSize(this NetClient socket, uint w, uint h)
        {
            const byte ID = 0xBF;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);

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
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        public static void Send_Language(this NetClient socket, string lang)
        {
            const byte ID = 0xBF;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);

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
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        public static void Send_ClientType(this NetClient socket, ClientFlags protocol)
        {
            const byte ID = 0xBF;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0x0F);
            writer.WriteUInt8(0x0A);

            uint clientFlag = 0;

            for (int i = 0; i < (uint)protocol; ++i)
            {
                clientFlag |= (uint)(1 << i);
            }


            writer.WriteUInt32BE(clientFlag);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }


            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        public static void Send_OpenChat(this NetClient socket, string name)
        {
            const byte ID = 0xB5;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);

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
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        public static void Send_QuestMenuRequest(this NetClient socket, uint playerSerial)
        {
            const byte ID = 0xD7;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(playerSerial);
            writer.WriteUInt16BE(0x32);
            writer.WriteUInt8(0x00);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        public static void Send_GuildMenuRequest(this NetClient socket, uint playerSerial)
        {
            const byte ID = 0xD7;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(playerSerial);
            writer.WriteUInt16BE(0x28);
            writer.WriteUInt8(0x0A);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        public static void Send_CustomHouseDataRequest(this NetClient socket, uint serial)
        {
            const byte ID = 0xBF;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);

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
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        public static void Send_Resync(this NetClient socket)
        {
            const byte ID = 0x22;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
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


            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        public static void Send_WalkRequest(this NetClient socket, Direction direction, byte seq, bool run, uint fastWalk)
        {
            const byte ID = 0x02;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            if (run)
            {
                direction |= Direction.Running;
            }

            writer.WriteUInt8((byte)direction);
            writer.WriteUInt8(seq);
            writer.WriteUInt32BE(fastWalk);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        public static void Send_ClientViewRange(this NetClient socket, byte range)
        {
            const byte ID = 0xC8;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);

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
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        public static void Send_ShowPublicHouseContent(this NetClient socket, bool show)
        {
            const byte ID = 0xFB;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteBool(show);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        // 0xBF subcommand 0x06 party packets. Byte layout mirrors legacy
        // NetClientExt.Send_Party* verbatim (the party code is a single byte
        // after the 0x06 subcommand).

        // code 1 + count 1 + serial 0: ask the server to target-invite someone.
        public static void Send_PartyInviteRequest(this NetClient socket)
        {
            const byte ID = 0xBF;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0x06);
            writer.WriteUInt8(0x01);
            writer.WriteUInt32BE(0x00);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        // code 2 + serial: remove a member (own serial = leave/disband).
        public static void Send_PartyRemoveRequest(this NetClient socket, uint serial)
        {
            const byte ID = 0xBF;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0x06);
            writer.WriteUInt8(0x02);
            writer.WriteUInt32BE(serial);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        // code 6 + bool: allow / disallow party loot of my corpse.
        public static void Send_PartyChangeLootTypeRequest(this NetClient socket, bool type)
        {
            const byte ID = 0xBF;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);
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
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        // code 8 + inviter serial: accept a party invite.
        public static void Send_PartyAccept(this NetClient socket, uint serial)
        {
            const byte ID = 0xBF;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);
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
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        // code 9 + inviter serial: decline a party invite.
        public static void Send_PartyDecline(this NetClient socket, uint serial)
        {
            const byte ID = 0xBF;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);
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
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        // code 3 (+ target serial) for a private tell, code 4 for a broadcast.
        public static void Send_PartyMessage(this NetClient socket, string text, uint serial)
        {
            const byte ID = 0xBF;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0x06);

            if (Game.SerialHelper.IsValid(serial))
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
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        // Cast a spell by index. Post-CV_60142 uses 0xBF subcommand 0x1C; older
        // clients use 0x12 type 0x56 with the index as ASCII. Mirrors legacy
        // NetClientExt.Send_CastSpell (used by the party heal buttons).
        public static void Send_CastSpell(this NetClient socket, int idx, ClientVersion version)
        {
            const byte ID = 0xBF;
            const byte ID_OLD = 0x12;

            byte id = version < ClientVersion.CV_60142 ? ID_OLD : ID;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(id);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            if (version >= ClientVersion.CV_60142)
            {
                writer.WriteUInt16BE(0x1C);
                writer.WriteUInt16BE(0x02);
                writer.WriteUInt16BE((ushort)idx);
            }
            else
            {
                writer.WriteUInt8(0x56);
                writer.WriteASCII(idx.ToString());
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

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        // 0x3B — buy reply. Each entry is (item serial, amount). Empty list
        // sends the 0x00 "buy nothing" terminator (closes the vendor cleanly).
        public static void Send_BuyRequest(this NetClient socket, uint vendorSerial, ReadOnlySpan<(uint serial, ushort amount)> items)
        {
            const byte ID = 0x3B;
            int length = socket.PacketsTable.GetPacketLength(ID);
            var writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);
            if (length < 0)
                writer.WriteZero(2);

            writer.WriteUInt32BE(vendorSerial);

            if (items.Length > 0)
            {
                writer.WriteUInt8(0x02);
                for (int i = 0; i < items.Length; ++i)
                {
                    writer.WriteUInt8(0x1A);
                    writer.WriteUInt32BE(items[i].serial);
                    writer.WriteUInt16BE(items[i].amount);
                }
            }
            else
            {
                writer.WriteUInt8(0x00);
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

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        // 0x9F — sell reply. Item count then (item serial, amount) per entry.
        public static void Send_SellRequest(this NetClient socket, uint vendorSerial, ReadOnlySpan<(uint serial, ushort amount)> items)
        {
            const byte ID = 0x9F;
            int length = socket.PacketsTable.GetPacketLength(ID);
            var writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);
            if (length < 0)
                writer.WriteZero(2);

            writer.WriteUInt32BE(vendorSerial);
            writer.WriteUInt16BE((ushort)items.Length);
            for (int i = 0; i < items.Length; ++i)
            {
                writer.WriteUInt32BE(items[i].serial);
                writer.WriteUInt16BE(items[i].amount);
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

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

    }
}