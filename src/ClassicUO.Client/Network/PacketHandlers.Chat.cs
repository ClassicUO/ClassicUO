// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;
using ClassicUO.Resources;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Network
{
    internal sealed partial class PacketHandlers
    {
        internal static void RegisterChatHandlers(PacketHandlers h)
        {
            h.Add(0x03, ClientTalk);
            h.Add(0x1C, Talk);
            h.Add(0xAE, UnicodeTalk);
            h.Add(0xB2, ChatMessage);
            h.Add(0xBB, UltimaMessengerR);
            h.Add(0xC1, DisplayClilocString);
            h.Add(0xCC, DisplayClilocString);
            h.Add(0xD6, MegaCliloc);
        }

        private static void ClientTalk(World world, ref StackDataReader p)
        {
            switch (p.ReadUInt8())
            {
                case 0x78:
                    break;

                case 0x3C:
                    break;

                case 0x25:
                    break;

                case 0x2E:
                    break;
            }
        }

        private static void Talk(World world, ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();
            Entity entity = world.Get(serial);
            ushort graphic = p.ReadUInt16BE();
            MessageType type = (MessageType)p.ReadUInt8();
            ushort hue = p.ReadUInt16BE();
            ushort font = p.ReadUInt16BE();
            string name = p.ReadASCII(30);
            string text;

            if (p.Length > 44)
            {
                p.Seek(44);
                text = p.ReadASCII();
            }
            else
            {
                text = string.Empty;
            }

            if (
                serial == 0
                && graphic == 0
                && type == MessageType.Regular
                && font == 0xFFFF
                && hue == 0xFFFF
                && name.StartsWith("SYSTEM")
            )
            {
                NetClient.Socket.Send_ACKTalk();

                return;
            }

            TextType text_type = TextType.SYSTEM;

            if (
                type == MessageType.System
                || serial == 0xFFFF_FFFF
                || serial == 0
                || name.ToLower() == "system" && entity == null
            )
            {
                // do nothing
            }
            else if (entity != null)
            {
                text_type = TextType.OBJECT;

                if (string.IsNullOrEmpty(entity.Name))
                {
                    entity.Name = string.IsNullOrEmpty(name) ? text : name;
                }
            }

            world.MessageManager.HandleMessage(entity, text, name, hue, type, (byte)font, text_type);
        }

        private static void UnicodeTalk(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                LoginScene scene = Client.Game.GetScene<LoginScene>();

                if (scene != null)
                {
                    //Serial serial = p.ReadUInt32BE();
                    //ushort graphic = p.ReadUInt16BE();
                    //MessageType type = (MessageType)p.ReadUInt8();
                    //Hue hue = p.ReadUInt16BE();
                    //MessageFont font = (MessageFont)p.ReadUInt16BE();
                    //string lang = p.ReadASCII(4);
                    //string name = p.ReadASCII(30);
                    Log.Warn("UnicodeTalk received during LoginScene");

                    if (p.Length > 48)
                    {
                        p.Seek(48);
                        Log.PushIndent();
                        Log.Warn("Handled UnicodeTalk in LoginScene");
                        Log.PopIndent();
                    }
                }

                return;
            }

            uint serial = p.ReadUInt32BE();
            Entity entity = world.Get(serial);
            ushort graphic = p.ReadUInt16BE();
            MessageType type = (MessageType)p.ReadUInt8();
            ushort hue = p.ReadUInt16BE();
            ushort font = p.ReadUInt16BE();
            string lang = p.ReadASCII(4);
            string name = p.ReadASCII();

            if (
                serial == 0
                && graphic == 0
                && type == MessageType.Regular
                && font == 0xFFFF
                && hue == 0xFFFF
                && name.ToLower() == "system"
            )
            {
                Span<byte> buffer =
                    stackalloc byte[] {
                        0x03,
                        0x00,
                        0x28,
                        0x20,
                        0x00,
                        0x34,
                        0x00,
                        0x03,
                        0xdb,
                        0x13,
                        0x14,
                        0x3f,
                        0x45,
                        0x2c,
                        0x58,
                        0x0f,
                        0x5d,
                        0x44,
                        0x2e,
                        0x50,
                        0x11,
                        0xdf,
                        0x75,
                        0x5c,
                        0xe0,
                        0x3e,
                        0x71,
                        0x4f,
                        0x31,
                        0x34,
                        0x05,
                        0x4e,
                        0x18,
                        0x1e,
                        0x72,
                        0x0f,
                        0x59,
                        0xad,
                        0xf5,
                        0x00
                    };

                NetClient.Socket.Send(buffer);

                return;
            }

            string text = string.Empty;

            if (p.Length > 48)
            {
                p.Seek(48);
                text = p.ReadUnicodeBE();
            }

            TextType text_type = TextType.SYSTEM;

            if (type == MessageType.Alliance || type == MessageType.Guild)
            {
                text_type = TextType.GUILD_ALLY;
            }
            else if (
                type == MessageType.System
                || serial == 0xFFFF_FFFF
                || serial == 0
                || name.ToLower() == "system" && entity == null
            )
            {
                // do nothing
            }
            else if (entity != null)
            {
                text_type = TextType.OBJECT;

                if (string.IsNullOrEmpty(entity.Name))
                {
                    entity.Name = string.IsNullOrEmpty(name) ? text : name;
                }
            }

            world.MessageManager.HandleMessage(
                entity,
                text,
                name,
                hue,
                type,
                ProfileManager.CurrentProfile.ChatFont,
                text_type,
                true,
                lang
            );
        }

        private static void ChatMessage(World world, ref StackDataReader p)
        {
            ushort cmd = p.ReadUInt16BE();

            switch (cmd)
            {
                case 0x03E8: // create conference
                    p.Skip(4);
                    string channelName = p.ReadUnicodeBE();
                    bool hasPassword = p.ReadUInt16BE() == 0x31;
                    world.ChatManager.CurrentChannelName = channelName;
                    world.ChatManager.AddChannel(channelName, hasPassword);

                    UIManager.GetGump<ChatGump>()?.RequestUpdateContents();

                    break;

                case 0x03E9: // destroy conference
                    p.Skip(4);
                    channelName = p.ReadUnicodeBE();
                    world.ChatManager.RemoveChannel(channelName);

                    UIManager.GetGump<ChatGump>()?.RequestUpdateContents();

                    break;

                case 0x03EB: // display enter username window
                    world.ChatManager.ChatIsEnabled = ChatStatus.EnabledUserRequest;

                    break;

                case 0x03EC: // close chat
                    world.ChatManager.Clear();
                    world.ChatManager.ChatIsEnabled = ChatStatus.Disabled;

                    UIManager.GetGump<ChatGump>()?.Dispose();

                    break;

                case 0x03ED: // username accepted, display chat
                    p.Skip(4);
                    string username = p.ReadUnicodeBE();
                    world.ChatManager.ChatIsEnabled = ChatStatus.Enabled;
                    NetClient.Socket.Send_ChatJoinCommand("General");

                    break;

                case 0x03EE: // add user
                    p.Skip(4);
                    ushort userType = p.ReadUInt16BE();
                    username = p.ReadUnicodeBE();

                    break;

                case 0x03EF: // remove user
                    p.Skip(4);
                    username = p.ReadUnicodeBE();

                    break;

                case 0x03F0: // clear all players
                    break;

                case 0x03F1: // you have joined a conference
                    p.Skip(4);
                    channelName = p.ReadUnicodeBE();
                    world.ChatManager.CurrentChannelName = channelName;

                    UIManager.GetGump<ChatGump>()?.UpdateConference();

                    GameActions.Print(
                        world,
                        string.Format(ResGeneral.YouHaveJoinedThe0Channel, channelName),
                        ProfileManager.CurrentProfile.ChatMessageHue,
                        MessageType.Regular,
                        1
                    );

                    break;

                case 0x03F4:
                    p.Skip(4);
                    channelName = p.ReadUnicodeBE();

                    GameActions.Print(
                        world,
                        string.Format(ResGeneral.YouHaveLeftThe0Channel, channelName),
                        ProfileManager.CurrentProfile.ChatMessageHue,
                        MessageType.Regular,
                        1
                    );

                    break;

                case 0x0025:
                case 0x0026:
                case 0x0027:
                    p.Skip(4);
                    ushort msgType = p.ReadUInt16BE();
                    username = p.ReadUnicodeBE();
                    string msgSent = p.ReadUnicodeBE();

                    if (!string.IsNullOrEmpty(msgSent))
                    {
                        int idx = msgSent.IndexOf('{');
                        int idxLast = msgSent.IndexOf('}') + 1;

                        if (idxLast > idx && idx > -1)
                        {
                            msgSent = msgSent.Remove(idx, idxLast - idx);
                        }
                    }

                    //Color c = new Color(49, 82, 156, 0);
                    GameActions.Print(
                        world,
                        $"{username}: {msgSent}",
                        ProfileManager.CurrentProfile.ChatMessageHue,
                        MessageType.Regular,
                        1
                    );

                    break;

                default:
                    if (cmd >= 0x0001 && cmd <= 0x0024 || cmd >= 0x0028 && cmd <= 0x002C)
                    {
                        // TODO: read Chat.enu ?
                        // http://docs.polserver.com/packets/index.php?Packet=0xB2

                        string msg = ChatManager.GetMessage(cmd - 1);

                        if (string.IsNullOrEmpty(msg))
                        {
                            return;
                        }

                        p.Skip(4);
                        string text = p.ReadUnicodeBE();

                        if (!string.IsNullOrEmpty(text))
                        {
                            int idx = msg.IndexOf("%1");

                            if (idx >= 0)
                            {
                                msg = msg.Replace("%1", text);
                            }

                            if (cmd - 1 == 0x000A || cmd - 1 == 0x0017)
                            {
                                idx = msg.IndexOf("%2");

                                if (idx >= 0)
                                {
                                    msg = msg.Replace("%2", text);
                                }
                            }
                        }

                        GameActions.Print(
                            world,
                            msg,
                            ProfileManager.CurrentProfile.ChatMessageHue,
                            MessageType.Regular,
                            1
                        );
                    }

                    break;
            }
        }

        private static void UltimaMessengerR(World world, ref StackDataReader p) { }

        private static void DisplayClilocString(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            Entity entity = world.Get(serial);
            ushort graphic = p.ReadUInt16BE();
            MessageType type = (MessageType)p.ReadUInt8();
            ushort hue = p.ReadUInt16BE();
            ushort font = p.ReadUInt16BE();
            uint cliloc = p.ReadUInt32BE();
            AffixType flags = p[0] == 0xCC ? (AffixType)p.ReadUInt8() : 0x00;
            string name = p.ReadASCII(30);
            string affix = p[0] == 0xCC ? p.ReadASCII() : string.Empty;

            string arguments = null;

            if (cliloc == 1008092 || cliloc == 1005445) // value for "You notify them you don't want to join the party" || "You have been added to the party"
            {
                for (LinkedListNode<Gump> g = UIManager.Gumps.Last; g != null; g = g.Previous)
                {
                    if (g.Value is PartyInviteGump pg)
                    {
                        pg.Dispose();
                    }
                }
            }

            int remains = p.Remaining;

            if (remains > 0)
            {
                if (p[0] == 0xCC)
                {
                    arguments = p.ReadUnicodeBE(remains);
                }
                else
                {
                    arguments = p.ReadUnicodeLE(remains / 2);
                }
            }

            string text = Client.Game.UO.FileManager.Clilocs.Translate((int)cliloc, arguments);

            if (text == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(affix))
            {
                if ((flags & AffixType.Prepend) != 0)
                {
                    text = $"{affix}{text}";
                }
                else
                {
                    text = $"{text}{affix}";
                }
            }

            if ((flags & AffixType.System) != 0)
            {
                type = MessageType.System;
            }

            if (!Client.Game.UO.FileManager.Fonts.UnicodeFontExists((byte)font))
            {
                font = 0;
            }

            TextType text_type = TextType.SYSTEM;

            if (
                serial == 0xFFFF_FFFF
                || serial == 0
                || !string.IsNullOrEmpty(name)
                    && string.Equals(name, "system", StringComparison.InvariantCultureIgnoreCase)
            )
            {
                // do nothing
            }
            else if (entity != null)
            {
                //entity.Graphic = graphic;
                text_type = TextType.OBJECT;

                if (string.IsNullOrEmpty(entity.Name))
                {
                    entity.Name = name;
                }
            }
            else
            {
                if (type == MessageType.Label)
                    return;
            }

            world.MessageManager.HandleMessage(
                entity,
                text,
                name,
                hue,
                type,
                (byte)font,
                text_type,
                true
            );
        }

        private static void MegaCliloc(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            ushort unknown = p.ReadUInt16BE();

            if (unknown > 1)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();

            p.Skip(2);

            uint revision = p.ReadUInt32BE();

            Entity entity = world.Mobiles.Get(serial);

            if (entity == null)
            {
                if (SerialHelper.IsMobile(serial))
                {
                    Log.Warn("Searching a mobile into World.Items from MegaCliloc packet");
                }

                entity = world.Items.Get(serial);
            }

            List<(int, string, int)> list = new List<(int, string, int)>();
            int totalLength = 0;

            while (p.Position < p.Length)
            {
                int cliloc = (int)p.ReadUInt32BE();

                if (cliloc == 0)
                {
                    break;
                }

                ushort length = p.ReadUInt16BE();

                string argument = string.Empty;

                if (length != 0)
                {
                    argument = p.ReadUnicodeLE(length / 2);
                }

                string str = Client.Game.UO.FileManager.Clilocs.Translate(cliloc, argument, true);

                if (str == null)
                {
                    continue;
                }

                int argcliloc = 0;

                string[] argcheck = argument.Split(
                    new[] { '#' },
                    StringSplitOptions.RemoveEmptyEntries
                );

                if (argcheck.Length == 2)
                {
                    int.TryParse(argcheck[1], out argcliloc);
                }

                // hardcoded colors lol
                switch (cliloc)
                {
                    case 1080418:
                        if (Client.Game.UO.Version >= Utility.ClientVersion.CV_60143)
                            str = "<basefont color=#40a4fe>" + str + "</basefont>";
                        break;
                    case 1061170:
                        if (int.TryParse(argument, out var strength) && world.Player.Strength < strength)
                            str = "<basefont color=#FF0000>" + str + "</basefont>";
                        break;
                    case 1062613:
                        str = "<basefont color=#FFCC33>" + str + "</basefont>";
                        break;
                    case 1159561:
                        str = "<basefont color=#b66dff>" + str + "</basefont>";
                        break;
                }


                for (int i = 0; i < list.Count; i++)
                {
                    if (
                        list[i].Item1 == cliloc
                        && string.Equals(list[i].Item2, str, StringComparison.Ordinal)
                    )
                    {
                        list.RemoveAt(i);

                        break;
                    }
                }

                list.Add((cliloc, str, argcliloc));

                totalLength += str.Length;
            }

            Item container = null;

            if (entity is Item it && SerialHelper.IsValid(it.Container))
            {
                container = world.Items.Get(it.Container);
            }

            bool inBuyList = false;

            if (container != null)
            {
                inBuyList =
                    container.Layer == Layer.ShopBuy
                    || container.Layer == Layer.ShopBuyRestock
                    || container.Layer == Layer.ShopSell;
            }

            bool first = true;

            string name = string.Empty;
            string data = string.Empty;
            int namecliloc = 0;

            if (list.Count != 0)
            {
                Span<char> span = stackalloc char[totalLength];
                ValueStringBuilder sb = new ValueStringBuilder(span);

                foreach (var s in list)
                {
                    string str = s.Item2;

                    if (first)
                    {
                        name = str;

                        if (entity != null && !SerialHelper.IsMobile(serial))
                        {
                            entity.Name = str;
                            namecliloc = s.Item3 > 0 ? s.Item3 : s.Item1;
                        }

                        first = false;
                    }
                    else
                    {
                        if (sb.Length != 0)
                        {
                            sb.Append('\n');
                        }

                        sb.Append(str);
                    }
                }

                data = sb.ToString();

                sb.Dispose();
            }

            world.OPL.Add(serial, revision, name, data, namecliloc);

            if (inBuyList && container != null && SerialHelper.IsValid(container.Serial))
            {
                UIManager.GetGump<ShopGump>(container.RootContainer)?.SetNameTo((Item)entity, name);
            }
        }
    }
}
