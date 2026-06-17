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

        // Secure-trade reply. code 1 = cancel the trade, code 2 = set my accept
        // state to `state`. Mirrors legacy Send_TradeResponse.
        public static void Send_TradeResponse(this NetClient socket, uint serial, int code, bool state)
        {
            const byte ID = 0x6F;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);

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
                writer.WriteUInt32BE((uint)(state ? 1 : 0));
            }
            else
            {
                writer.Dispose();

                return;
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

        // Secure-trade gold/platinum update (sub-action 3). Mirrors legacy
        // Send_TradeUpdateGold.
        public static void Send_TradeUpdateGold(this NetClient socket, uint serial, uint gold, uint platinum)
        {
            const byte ID = 0x6F;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);

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
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        // Icon-menu (0x7C menuid != 0) selection reply. code = item index (1-based);
        // 0 = cancel. Mirrors legacy Send_MenuResponse.
        public static void Send_MenuResponse(this NetClient socket, uint serial, ushort graphic, int code, ushort itemGraphic, ushort itemHue)
        {
            const byte ID = 0x7D;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(serial);
            writer.WriteUInt16BE(graphic);

            if (code != 0)
            {
                writer.WriteUInt16BE((ushort)code);
                writer.WriteUInt16BE(itemGraphic);
                writer.WriteUInt16BE(itemHue);
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

        // Gray-menu (0x7C menuid == 0) selection reply. code = radio index
        // (1-based); 0 = cancel. Mirrors legacy Send_GrayMenuResponse.
        public static void Send_GrayMenuResponse(this NetClient socket, uint serial, ushort graphic, ushort code)
        {
            const byte ID = 0x7D;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);

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
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        // Modern (AOS) special-move toggle (0xD7 action 0x19). idx = ability
        // index (1-based) or 0 to clear. Mirrors legacy Send_UseCombatAbility.
        public static void Send_UseCombatAbility(this NetClient socket, uint playerSerial, byte idx)
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
            writer.WriteUInt16BE(0x19);
            writer.WriteUInt32BE(0);
            writer.WriteUInt8(idx);
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

        // Click on the quest arrow (0xBF subcommand 0x07). rightClick=true is
        // the legacy "dismiss/abandon" prompt path.
        public static void Send_ClickQuestArrow(this NetClient socket, bool rightClick)
        {
            const byte ID = 0xBF;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0x07);
            writer.WriteBool(rightClick);

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

        // Pre-AOS primary special move (0xBF subcommand 0x09).
        public static void Send_StunRequest(this NetClient socket)
        {
            const byte ID = 0xBF;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0x09);

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

        // Pre-AOS secondary special move (0xBF subcommand 0x0A).
        public static void Send_DisarmRequest(this NetClient socket)
        {
            const byte ID = 0xBF;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0x0A);

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

        // Tip / notice scroll navigation (0xA7). id = the tip serial; flag 0 =
        // previous, 1 = next. Mirrors legacy Send_TipRequest.
        public static void Send_TipRequest(this NetClient socket, ushort id, byte flag)
        {
            const byte ID = 0xA7;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);

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
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        // Toggle gargoyle flight (0xBF subcommand 0x32). Mirrors legacy
        // Send_ToggleGargoyleFlying.
        public static void Send_ToggleGargoyleFlying(this NetClient socket)
        {
            const byte ID = 0xBF;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);

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

        // 0xBF 0x13 — ask the server for the context/popup menu of an entity.
        public static void Send_RequestPopupMenu(this NetClient socket, uint serial)
        {
            const byte ID = 0xBF;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);
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
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);

            writer.Dispose();
        }

        // 0xBF 0x15 — respond with the chosen popup menu entry index.
        public static void Send_PopupMenuSelection(this NetClient socket, uint serial, ushort index)
        {
            const byte ID = 0xBF;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0x15);
            writer.WriteUInt32BE(serial);
            writer.WriteUInt16BE(index);

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

        // 0xD6 batch — ask the server for the Object Property List (tooltip) of
        // up to 15 entities. CV_5090+. Mirrors legacy Send_MegaClilocRequest.
        public static void Send_MegaClilocRequest(this NetClient socket, ReadOnlySpan<uint> serials)
        {
            const byte ID = 0xD6;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            int count = Math.Min(15, serials.Length);
            for (int i = 0; i < count; ++i)
            {
                writer.WriteUInt32BE(serials[i]);
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

        // 0xBF 0x10 — single-serial OPL request for pre-CV_5090 clients.
        public static void Send_MegaClilocRequest_Old(this NetClient socket, uint serial)
        {
            const byte ID = 0xBF;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);
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

        // Mirrors main's Send_CreateCharacter (0x00 / 0xF8) with the character
        // data flattened to plain args — the ECS branch has no PlayerMobile to
        // read layers/skills from.
        public static void Send_CreateCharacter(
            this NetClient socket,
            ClientVersion version,
            ClientFlags protocol,
            string name,
            bool isFemale,
            RaceType race,
            byte strength,
            byte dexterity,
            byte intelligence,
            ReadOnlySpan<(byte Index, byte Value)> skills,
            ushort skinHue,
            ushort hairGraphic,
            ushort hairHue,
            ushort beardGraphic,
            ushort beardHue,
            ushort shirtHue,
            ushort pantsHue,
            int cityIndex,
            uint slot,
            uint clientIP,
            byte profession)
        {
            const byte ID = 0x00;
            const byte ID_NEW = 0xF8;

            byte id = ID;
            int skillcount = 3;

            if (version >= ClientVersion.CV_70160)
            {
                id = ID_NEW;
                ++skillcount;
            }

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(id);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(0xEDEDEDED);
            writer.WriteUInt32BE(0xFFFF_FFFF);
            writer.WriteUInt8(0x00);
            writer.WriteASCII(name, 30);
            writer.WriteZero(2);

            writer.WriteUInt32BE((uint) protocol);
            writer.WriteUInt32BE(0x01);
            writer.WriteUInt32BE(0x00);
            writer.WriteUInt8(profession);
            writer.WriteZero(15);

            byte val;

            if (version < ClientVersion.CV_4011D)
            {
                val = (byte) (isFemale ? 0x01 : 0x00);
            }
            else
            {
                val = (byte) race;

                if (version < ClientVersion.CV_7000)
                {
                    val--;
                }

                val = (byte) (val * 2 + (byte) (isFemale ? 0x01 : 0x00));
            }

            writer.WriteUInt8(val);
            writer.WriteUInt8(strength);
            writer.WriteUInt8(dexterity);
            writer.WriteUInt8(intelligence);

            for (int i = 0; i < skillcount; i++)
            {
                if (i < skills.Length)
                {
                    writer.WriteUInt8(skills[i].Index);
                    writer.WriteUInt8(skills[i].Value);
                }
                else
                {
                    writer.WriteZero(2);
                }
            }

            writer.WriteUInt16BE(skinHue);

            writer.WriteUInt16BE(hairGraphic);
            writer.WriteUInt16BE(hairHue);
            writer.WriteUInt16BE(beardGraphic);
            writer.WriteUInt16BE(beardHue);

            writer.WriteUInt16BE((ushort) cityIndex);
            writer.WriteZero(2);
            writer.WriteUInt16BE((ushort) slot);
            writer.WriteUInt32BE(clientIP);

            writer.WriteUInt16BE(shirtHue);
            writer.WriteUInt16BE(pantsHue);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort) writer.BytesWritten);
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

        public static void Send_DyeDataResponse(this NetClient socket, uint serial, ushort graphic, ushort hue)
        {
            const byte ID = 0x95;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(serial);
            writer.WriteUInt16BE(0);
            writer.WriteUInt16BE(hue);

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

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(serial);
            writer.WriteUInt8(parentID);
            writer.WriteUInt8(button);
            writer.WriteBool(code);
            writer.WriteUInt16BE((ushort)(text.Length + 1));
            writer.WriteASCII(text, text.Length + 1);

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

        // 0xB8 edit subcommand (0x01): push the player's edited profile body.
        public static void Send_ProfileUpdate(this NetClient socket, uint serial, string text)
        {
            const byte ID = 0xB8;

            int length = socket.PacketsTable.GetPacketLength(ID);

            var writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8(0x01);
            writer.WriteUInt32BE(serial);
            writer.WriteUInt16BE(0x01);
            writer.WriteUInt16BE((ushort)text.Length);
            writer.WriteUnicodeBE(text, text.Length);

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

        // 0xD7 custom-house design commands. Each is a u16BE op after the player
        // serial, an op-specific payload, then a 0x0A terminator. The server
        // validates the action and echoes the resulting house state back via
        // 0xD8, so the client never needs to apply changes locally.
        private static void SendCustomHouseOp(NetClient socket, uint playerSerial, ushort op)
        {
            const byte ID = 0xD7;
            int length = socket.PacketsTable.GetPacketLength(ID);
            var writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);
            if (length < 0)
                writer.WriteZero(2);
            writer.WriteUInt32BE(playerSerial);
            writer.WriteUInt16BE(op);
            writer.WriteUInt8(0x0A);
            FinishCustomHousePacket(socket, ref writer, length);
        }

        private static void SendCustomHouseXY(NetClient socket, uint playerSerial, ushort op, ushort graphic, int x, int y)
        {
            const byte ID = 0xD7;
            int length = socket.PacketsTable.GetPacketLength(ID);
            var writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);
            if (length < 0)
                writer.WriteZero(2);
            writer.WriteUInt32BE(playerSerial);
            writer.WriteUInt16BE(op);
            writer.WriteUInt8(0x00);
            writer.WriteUInt32BE(graphic);
            writer.WriteUInt8(0x00);
            writer.WriteUInt32BE((uint)x);
            writer.WriteUInt8(0x00);
            writer.WriteUInt32BE((uint)y);
            writer.WriteUInt8(0x0A);
            FinishCustomHousePacket(socket, ref writer, length);
        }

        private static void SendCustomHouseXYZ(NetClient socket, uint playerSerial, ushort op, ushort graphic, int x, int y, int z)
        {
            const byte ID = 0xD7;
            int length = socket.PacketsTable.GetPacketLength(ID);
            var writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);
            if (length < 0)
                writer.WriteZero(2);
            writer.WriteUInt32BE(playerSerial);
            writer.WriteUInt16BE(op);
            writer.WriteUInt8(0x00);
            writer.WriteUInt32BE(graphic);
            writer.WriteUInt8(0x00);
            writer.WriteUInt32BE((uint)x);
            writer.WriteUInt8(0x00);
            writer.WriteUInt32BE((uint)y);
            writer.WriteUInt8(0x00);
            writer.WriteUInt32BE((uint)z);
            writer.WriteUInt8(0x0A);
            FinishCustomHousePacket(socket, ref writer, length);
        }

        private static void FinishCustomHousePacket(NetClient socket, ref StackDataWriter writer, int length)
        {
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

        public static void Send_CustomHouseBackup(this NetClient socket, uint playerSerial)
            => SendCustomHouseOp(socket, playerSerial, 0x02);

        public static void Send_CustomHouseRestore(this NetClient socket, uint playerSerial)
            => SendCustomHouseOp(socket, playerSerial, 0x03);

        public static void Send_CustomHouseCommit(this NetClient socket, uint playerSerial)
            => SendCustomHouseOp(socket, playerSerial, 0x04);

        public static void Send_CustomHouseBuildingExit(this NetClient socket, uint playerSerial)
            => SendCustomHouseOp(socket, playerSerial, 0x0C);

        public static void Send_CustomHouseSync(this NetClient socket, uint playerSerial)
            => SendCustomHouseOp(socket, playerSerial, 0x0E);

        public static void Send_CustomHouseClear(this NetClient socket, uint playerSerial)
            => SendCustomHouseOp(socket, playerSerial, 0x10);

        public static void Send_CustomHouseResponse(this NetClient socket, uint playerSerial)
            => SendCustomHouseOp(socket, playerSerial, 0x0A);

        public static void Send_CustomHouseRevert(this NetClient socket, uint playerSerial)
            => SendCustomHouseOp(socket, playerSerial, 0x1A);

        public static void Send_CustomHouseAddItem(this NetClient socket, uint playerSerial, ushort graphic, int x, int y)
            => SendCustomHouseXY(socket, playerSerial, 0x06, graphic, x, y);

        public static void Send_CustomHouseAddStair(this NetClient socket, uint playerSerial, ushort graphic, int x, int y)
            => SendCustomHouseXY(socket, playerSerial, 0x0D, graphic, x, y);

        public static void Send_CustomHouseDeleteItem(this NetClient socket, uint playerSerial, ushort graphic, int x, int y, int z)
            => SendCustomHouseXYZ(socket, playerSerial, 0x05, graphic, x, y, z);

        public static void Send_CustomHouseAddRoof(this NetClient socket, uint playerSerial, ushort graphic, int x, int y, int z)
            => SendCustomHouseXYZ(socket, playerSerial, 0x13, graphic, x, y, z);

        public static void Send_CustomHouseDeleteRoof(this NetClient socket, uint playerSerial, ushort graphic, int x, int y, int z)
            => SendCustomHouseXYZ(socket, playerSerial, 0x14, graphic, x, y, z);

        public static void Send_CustomHouseGoToFloor(this NetClient socket, uint playerSerial, byte floor)
        {
            const byte ID = 0xD7;
            int length = socket.PacketsTable.GetPacketLength(ID);
            var writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);
            if (length < 0)
                writer.WriteZero(2);
            writer.WriteUInt32BE(playerSerial);
            writer.WriteUInt16BE(0x12);
            writer.WriteUInt32BE(0);
            writer.WriteUInt8(floor);
            writer.WriteUInt8(0x0A);
            FinishCustomHousePacket(socket, ref writer, length);
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

        // 0x66 — request a book page (wire pages are 1-based). The server
        // replies with a 0x66 carrying that page's lines. Mirrors legacy
        // Send_BookPageDataRequest.
        public static void Send_BookPageDataRequest(this NetClient socket, uint serial, ushort page)
        {
            const byte ID = 0x66;
            int length = socket.PacketsTable.GetPacketLength(ID);
            var writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);
            if (length < 0)
                writer.WriteZero(2);

            writer.WriteUInt32BE(serial);
            writer.WriteUInt16BE(0x01);
            writer.WriteUInt16BE(page);
            writer.WriteUInt16BE(0xFFFF);

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

        // 0x66 — push one edited page (UTF8 lines, each null-terminated, plus a
        // trailing null). Mirrors legacy Send_BookPageData.
        public static void Send_BookPageData(this NetClient socket, uint serial, string[] text, int page)
        {
            const byte ID = 0x66;
            int length = socket.PacketsTable.GetPacketLength(ID);
            var writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);
            if (length < 0)
                writer.WriteZero(2);

            writer.WriteUInt32BE(serial);
            writer.WriteUInt16BE(0x01);
            writer.WriteUInt16BE((ushort)page);
            writer.WriteUInt16BE((ushort)text.Length);

            for (int i = 0; i < text.Length; ++i)
            {
                if (!string.IsNullOrEmpty(text[i]))
                {
                    string t = text[i].Replace("\n", "");

                    if (t.Length > 0)
                    {
                        byte[] buffer = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetMaxByteCount(t.Length));
                        try
                        {
                            int written = Encoding.UTF8.GetBytes(t, 0, t.Length, buffer, 0);
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
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten);
            writer.Dispose();
        }

        // 0xD4 — book title/author update (length-prefixed UTF8 strings, the
        // post-2.0 header shape). Mirrors legacy Send_BookHeaderChanged.
        public static void Send_BookHeaderChanged(this NetClient socket, uint serial, string title, string author)
        {
            const byte ID = 0xD4;
            int length = socket.PacketsTable.GetPacketLength(ID);
            var writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);
            if (length < 0)
                writer.WriteZero(2);

            writer.WriteUInt32BE(serial);
            writer.WriteUInt8(0x00);
            writer.WriteUInt8(0x00);
            writer.WriteUInt16BE(0);
            int titleLength = Encoding.UTF8.GetByteCount(title);
            writer.WriteUInt16BE((ushort)titleLength);
            writer.WriteUTF8(title, titleLength);
            int authorLength = Encoding.UTF8.GetByteCount(author);
            writer.WriteUInt16BE((ushort)authorLength);
            writer.WriteUTF8(author, authorLength);

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

        // 0x93 — book title/author update, fixed-size header (for books the
        // server opened with the old 0x93). Mirrors legacy Send_BookHeaderChanged_Old.
        public static void Send_BookHeaderChanged_Old(this NetClient socket, uint serial, string title, string author)
        {
            const byte ID = 0x93;
            int length = socket.PacketsTable.GetPacketLength(ID);
            var writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);
            if (length < 0)
                writer.WriteZero(2);

            writer.WriteUInt32BE(serial);
            writer.WriteUInt8(0x00);
            writer.WriteUInt8(0x01);
            writer.WriteUInt16BE(0);
            writer.WriteUTF8(title, 60);
            writer.WriteUTF8(author, 30);

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


        // 0x71 subtype 3 — request a bulletin message body (double-click a row).
        public static void Send_BulletinBoardRequestMessage(this NetClient socket, uint serial, uint msgSerial)
        {
            Send_BulletinBoardCommand(socket, 0x03, serial, msgSerial);
        }

        // 0x71 subtype 5 — post a new message / reply (msgSerial = replied-to or 0).
        public static void Send_BulletinBoardPostMessage(this NetClient socket, uint serial, uint msgSerial, string subject, string text)
        {
            const byte ID = 0x71;
            int length = socket.PacketsTable.GetPacketLength(ID);
            var writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);
            if (length < 0)
                writer.WriteZero(2);

            subject = subject.Replace("\r\n", "\n");
            text = text.Replace("\r\n", "\n");

            writer.WriteUInt8(0x05);
            writer.WriteUInt32BE(serial);
            writer.WriteUInt32BE(msgSerial);

            byte[] title = Encoding.UTF8.GetBytes(subject);
            writer.WriteUInt8((byte)(title.Length + 1));
            writer.Write(title);
            writer.WriteUInt8(0x00);

            var lines = text.Split('\n');
            writer.WriteUInt8((byte)Math.Max(1, lines.Length));
            if (lines.Length == 0)
                lines = new[] { text };
            foreach (var line in lines)
            {
                var bytes = Encoding.UTF8.GetBytes(line);
                writer.WriteUInt8((byte)(bytes.Length + 1));
                writer.Write(bytes.AsSpan());
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

        // 0x71 subtype 6 — remove own message.
        public static void Send_BulletinBoardRemoveMessage(this NetClient socket, uint serial, uint msgSerial)
        {
            Send_BulletinBoardCommand(socket, 0x06, serial, msgSerial);
        }

        private static void Send_BulletinBoardCommand(NetClient socket, byte subtype, uint serial, uint msgSerial)
        {
            const byte ID = 0x71;
            int length = socket.PacketsTable.GetPacketLength(ID);
            var writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);
            if (length < 0)
                writer.WriteZero(2);

            writer.WriteUInt8(subtype);
            writer.WriteUInt32BE(serial);
            writer.WriteUInt32BE(msgSerial);

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

        // 0x56 — treasure-map actions: 1 add pin, 5 clear course, 6 toggle plot.
        public static void Send_MapMessage(this NetClient socket, uint serial, byte action, byte pin, ushort x, ushort y)
        {
            const byte ID = 0x56;
            int length = socket.PacketsTable.GetPacketLength(ID);
            var writer = new StackDataWriter(length < 0 ? 64 : length);
            writer.WriteUInt8(ID);
            if (length < 0)
                writer.WriteZero(2);

            writer.WriteUInt32BE(serial);
            writer.WriteUInt8(action);
            writer.WriteUInt8(pin);
            writer.WriteUInt16BE(x);
            writer.WriteUInt16BE(y);

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
