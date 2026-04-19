// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using ClassicUO.Utility.Platforms;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Network
{
    internal sealed partial class PacketHandlers
    {
        public delegate void OnPacketBufferReader(World world, ref StackDataReader p);

        private static uint _requestedGridLoot;

        private static readonly TextFileParser _parser = new TextFileParser(
            string.Empty,
            new[] { ' ' },
            new char[] { },
            new[] { '{', '}' }
        );
        private static readonly TextFileParser _cmdparser = new TextFileParser(
            string.Empty,
            new[] { ' ', ',' },
            new char[] { },
            new[] { '@', '@' }
        );

        private List<uint> _clilocRequests = new List<uint>();
        private List<uint> _customHouseRequests = new List<uint>();
        private readonly OnPacketBufferReader[] _handlers = new OnPacketBufferReader[0x100];

        public static PacketHandlers Handler { get; } = new PacketHandlers();

        public void Add(byte id, OnPacketBufferReader handler) => _handlers[id] = handler;

        private byte[] _readingBuffer = new byte[4096];
        private readonly PacketLogger _packetLogger = new PacketLogger();
        private readonly CircularBuffer _buffer = new CircularBuffer();
        private readonly CircularBuffer _pluginsBuffer = new CircularBuffer();

        public int ParsePackets(NetClient socket, World world, Span<byte> data)
        {
            Append(data, false);

            return ParsePackets(socket, world, _buffer, true) + ParsePackets(socket, world, _pluginsBuffer, false);
        }

        private int ParsePackets(NetClient socket, World world, CircularBuffer stream, bool allowPlugins)
        {
            var packetsCount = 0;

            lock (stream)
            {
                ref var packetBuffer = ref _readingBuffer;

                while (stream.Length > 0)
                {
                    if (
                        !GetPacketInfo(
                            socket,
                            stream,
                            stream.Length,
                            out var packetID,
                            out int offset,
                            out int packetlength
                        )
                    )
                    {
                        Log.Warn(
                            $"Invalid ID: {packetID:X2} | off: {offset} | len: {packetlength} | stream.pos: {stream.Length}"
                        );

                        break;
                    }

                    if (stream.Length < packetlength)
                    {
                        Log.Warn(
                            $"need more data ID: {packetID:X2} | off: {offset} | len: {packetlength} | stream.pos: {stream.Length}"
                        );

                        // need more data
                        break;
                    }

                    while (packetlength > packetBuffer.Length)
                    {
                        Array.Resize(ref packetBuffer, packetBuffer.Length * 2);
                    }

                    _ = stream.Dequeue(packetBuffer, 0, packetlength);

                    PacketLogger.Default?.Log(packetBuffer.AsSpan(0, packetlength), false);

                    // TODO: the pluging function should allow Span<byte> or unsafe type only.
                    // The current one is a bad style decision.
                    // It will be fixed once the new plugin system is done.
                    if (!allowPlugins || Plugin.ProcessRecvPacket(packetBuffer, ref packetlength))
                    {
                        AnalyzePacket(world, packetBuffer.AsSpan(0, packetlength), offset);

                        ++packetsCount;
                    }
                }
            }

            return packetsCount;
        }

        public void Append(Span<byte> data, bool fromPlugins)
        {
            if (data.IsEmpty)
                return;

            (fromPlugins ? _pluginsBuffer : _buffer).Enqueue(data);
        }

        private void AnalyzePacket(World world, ReadOnlySpan<byte> data, int offset)
        {
            if (data.IsEmpty)
                return;

            var bufferReader = _handlers[data[0]];

            if (bufferReader != null)
            {
                var buffer = new StackDataReader(data);
                buffer.Seek(offset);

                bufferReader(world, ref buffer);
            }
        }

        private static bool GetPacketInfo(
            NetClient socket,
            CircularBuffer buffer,
            int bufferLen,
            out byte packetID,
            out int packetOffset,
            out int packetLen
        )
        {
            if (buffer == null || bufferLen <= 0)
            {
                packetID = 0xFF;
                packetLen = 0;
                packetOffset = 0;

                return false;
            }

            packetLen = socket.PacketsTable.GetPacketLength(packetID = buffer[0]);
            packetOffset = 1;

            if (packetLen == -1)
            {
                if (bufferLen < 3)
                {
                    return false;
                }

                var b0 = buffer[1];
                var b1 = buffer[2];

                packetLen = (b0 << 8) | b1;
                packetOffset = 3;
            }

            return true;
        }

        static PacketHandlers()
        {
            Handler.Add(0x32, Unknown_0x32);
            Handler.Add(0x3A, UpdateSkills);
            Handler.Add(0x3B, CloseVendorInterface);
            Handler.Add(0x66, BookData);
            Handler.Add(0x6C, TargetCursor);
            Handler.Add(0x6F, SecureTrading);
            Handler.Add(0x71, BulletinBoardData);
            Handler.Add(0x73, Ping);
            Handler.Add(0x74, BuyList);
            Handler.Add(0x7C, OpenMenu);
            Handler.Add(0x93, OpenBook);
            Handler.Add(0x99, MultiPlacement);
            Handler.Add(0x9A, ASCIIPrompt);
            Handler.Add(0x9E, SellList);
            Handler.Add(0xA5, OpenUrl);
            Handler.Add(0xA6, TipWindow);
            Handler.Add(0xAB, TextEntryDialog);
            Handler.Add(0xB0, OpenGump);
            Handler.Add(0xB7, Help);
            Handler.Add(0xB9, EnableLockedFeatures);
            Handler.Add(0xBA, DisplayQuestArrow);
            Handler.Add(0xBF, ExtendedCommand);
            Handler.Add(0xC2, UnicodePrompt);
            Handler.Add(0xC6, InvalidMapEnable);
            Handler.Add(0xC8, ClientViewRange);
            Handler.Add(0xCA, GetUserServerPingGodClientR);
            Handler.Add(0xCB, GlobalQueCount);
            Handler.Add(0xD0, ConfigurationFileR);
            Handler.Add(0xD4, OpenBook);
            Handler.Add(0xD7, GenericAOSCommandsR);
            Handler.Add(0xD8, CustomHouse);
            Handler.Add(0xDB, CharacterTransferLog);
            Handler.Add(0xDC, OPLInfo);
            Handler.Add(0xDD, OpenCompressedGump);
            Handler.Add(0xE5, DisplayWaypoint);
            Handler.Add(0xE6, RemoveWaypoint);
            Handler.Add(0xF0, KrriosClientSpecial);
            Handler.Add(0xF1, FreeshardListR);

            RegisterLoginHandlers(Handler);
            RegisterCombatHandlers(Handler);
            RegisterMovementHandlers(Handler);
            RegisterItemsHandlers(Handler);
            RegisterChatHandlers(Handler);
            RegisterWorldHandlers(Handler);
            RegisterObjectsHandlers(Handler);
        }

        public static void SendMegaClilocRequests(World world)
        {
            if (world.ClientFeatures.TooltipsEnabled && Handler._clilocRequests.Count != 0)
            {
                if (Client.Game.UO.Version >= Utility.ClientVersion.CV_5090)
                {
                    if (Handler._clilocRequests.Count != 0)
                    {
                        NetClient.Socket.Send_MegaClilocRequest(Handler._clilocRequests);
                    }
                }
                else
                {
                    foreach (uint serial in Handler._clilocRequests)
                    {
                        NetClient.Socket.Send_MegaClilocRequest_Old(serial);
                    }

                    Handler._clilocRequests.Clear();
                }
            }

            if (Handler._customHouseRequests.Count > 0)
            {
                for (int i = 0; i < Handler._customHouseRequests.Count; ++i)
                {
                    NetClient.Socket.Send_CustomHouseDataRequest(Handler._customHouseRequests[i]);
                }

                Handler._customHouseRequests.Clear();
            }
        }

        public static void AddMegaClilocRequest(uint serial)
        {
            foreach (uint s in Handler._clilocRequests)
            {
                if (s == serial)
                {
                    return;
                }
            }

            Handler._clilocRequests.Add(serial);
        }

        private static void TargetCursor(World world, ref StackDataReader p)
        {
            world.TargetManager.SetTargeting(
                (CursorTarget)p.ReadUInt8(),
                p.ReadUInt32BE(),
                (TargetType)p.ReadUInt8()
            );

            if (world.Party.PartyHealTimer < Time.Ticks && world.Party.PartyHealTarget != 0)
            {
                world.TargetManager.Target(world.Party.PartyHealTarget);
                world.Party.PartyHealTimer = 0;
                world.Party.PartyHealTarget = 0;
            }
        }

        private static void SecureTrading(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            byte type = p.ReadUInt8();
            uint serial = p.ReadUInt32BE();

            if (type == 0)
            {
                uint id1 = p.ReadUInt32BE();
                uint id2 = p.ReadUInt32BE();

                // standard client doesn't allow the trading system if one of the traders is invisible (=not sent by server)
                if (world.Get(id1) == null || world.Get(id2) == null)
                {
                    return;
                }

                bool hasName = p.ReadBool();
                string name = string.Empty;

                if (hasName && p.Position < p.Length)
                {
                    name = p.ReadASCII();
                }

                UIManager.Add(new TradingGump(world, serial, name, id1, id2));
            }
            else if (type == 1)
            {
                UIManager.GetTradingGump(serial)?.Dispose();
            }
            else if (type == 2)
            {
                uint id1 = p.ReadUInt32BE();
                uint id2 = p.ReadUInt32BE();

                TradingGump trading = UIManager.GetTradingGump(serial);

                if (trading != null)
                {
                    trading.ImAccepting = id1 != 0;
                    trading.HeIsAccepting = id2 != 0;

                    trading.RequestUpdateContents();
                }
            }
            else if (type == 3 || type == 4)
            {
                TradingGump trading = UIManager.GetTradingGump(serial);

                if (trading != null)
                {
                    if (type == 4)
                    {
                        trading.Gold = p.ReadUInt32BE();
                        trading.Platinum = p.ReadUInt32BE();
                    }
                    else
                    {
                        trading.HisGold = p.ReadUInt32BE();
                        trading.HisPlatinum = p.ReadUInt32BE();
                    }
                }
            }
        }




















        private static void Unknown_0x32(World world, ref StackDataReader p) { }

        private static void UpdateSkills(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            byte type = p.ReadUInt8();
            bool haveCap = type != 0u && type <= 0x03 || type == 0xDF;
            bool isSingleUpdate = type == 0xFF || type == 0xDF;

            if (type == 0xFE)
            {
                int count = p.ReadUInt16BE();

                Client.Game.UO.FileManager.Skills.Skills.Clear();
                Client.Game.UO.FileManager.Skills.SortedSkills.Clear();

                for (int i = 0; i < count; i++)
                {
                    bool haveButton = p.ReadBool();
                    int nameLength = p.ReadUInt8();

                    Client.Game.UO.FileManager.Skills.Skills.Add(
                        new SkillEntry(i, p.ReadASCII(nameLength), haveButton)
                    );
                }

                Client.Game.UO.FileManager.Skills.SortedSkills.AddRange(Client.Game.UO.FileManager.Skills.Skills);

                Client.Game.UO.FileManager.Skills.SortedSkills.Sort(
                    (a, b) => string.Compare(a.Name, b.Name, StringComparison.InvariantCulture)
                );
            }
            else
            {
                StandardSkillsGump standard = null;
                SkillGumpAdvanced advanced = null;

                if (ProfileManager.CurrentProfile.StandardSkillsGump)
                {
                    standard = UIManager.GetGump<StandardSkillsGump>();
                }
                else
                {
                    advanced = UIManager.GetGump<SkillGumpAdvanced>();
                }

                if (!isSingleUpdate && (type == 1 || type == 3 || world.SkillsRequested))
                {
                    world.SkillsRequested = false;

                    // TODO: make a base class for this gump
                    if (ProfileManager.CurrentProfile.StandardSkillsGump)
                    {
                        if (standard == null)
                        {
                            UIManager.Add(standard = new StandardSkillsGump(world) { X = 100, Y = 100 });
                        }
                    }
                    else
                    {
                        if (advanced == null)
                        {
                            UIManager.Add(advanced = new SkillGumpAdvanced(world) { X = 100, Y = 100 });
                        }
                    }
                }

                while (p.Position < p.Length)
                {
                    ushort id = p.ReadUInt16BE();

                    if (p.Position >= p.Length)
                    {
                        break;
                    }

                    if (id == 0 && type == 0)
                    {
                        break;
                    }

                    if (type == 0 || type == 0x02)
                    {
                        id--;
                    }

                    ushort realVal = p.ReadUInt16BE();
                    ushort baseVal = p.ReadUInt16BE();
                    Lock locked = (Lock)p.ReadUInt8();
                    ushort cap = 1000;

                    if (haveCap)
                    {
                        cap = p.ReadUInt16BE();
                    }

                    if (id < world.Player.Skills.Length)
                    {
                        Skill skill = world.Player.Skills[id];

                        if (skill != null)
                        {
                            if (isSingleUpdate)
                            {
                                float change = realVal / 10.0f - skill.Value;

                                if (
                                    change != 0.0f
                                    && !float.IsNaN(change)
                                    && ProfileManager.CurrentProfile != null
                                    && ProfileManager.CurrentProfile.ShowSkillsChangedMessage
                                    && Math.Abs(change * 10)
                                        >= ProfileManager.CurrentProfile.ShowSkillsChangedDeltaValue
                                )
                                {
                                    GameActions.Print(
                                        world,
                                        string.Format(
                                            ResGeneral.YourSkillIn0Has1By2ItIsNow3,
                                            skill.Name,
                                            change < 0
                                                ? ResGeneral.Decreased
                                                : ResGeneral.Increased,
                                            Math.Abs(change),
                                            skill.Value + change
                                        ),
                                        0x58,
                                        MessageType.System,
                                        3,
                                        false
                                    );
                                }
                            }

                            skill.BaseFixed = baseVal;
                            skill.ValueFixed = realVal;
                            skill.CapFixed = cap;
                            skill.Lock = locked;

                            standard?.Update(id);
                            advanced?.ForceUpdate();
                        }
                    }

                    if (isSingleUpdate)
                    {
                        break;
                    }
                }
            }
        }



        private static void CloseVendorInterface(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();

            UIManager.GetGump<ShopGump>(serial)?.Dispose();
        }








        private static void BookData(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            ushort pageCnt = p.ReadUInt16BE();

            ModernBookGump gump = UIManager.GetGump<ModernBookGump>(serial);

            if (gump == null || gump.IsDisposed)
            {
                return;
            }

            for (int i = 0; i < pageCnt; i++)
            {
                int pageNum = p.ReadUInt16BE() - 1;
                gump.KnownPages.Add(pageNum);

                if (pageNum < gump.BookPageCount && pageNum >= 0)
                {
                    ushort lineCnt = p.ReadUInt16BE();

                    for (int line = 0; line < lineCnt; line++)
                    {
                        int index = pageNum * ModernBookGump.MAX_BOOK_LINES + line;

                        if (index < gump.BookLines.Length)
                        {
                            gump.BookLines[index] = ModernBookGump.IsNewBook
                                ? p.ReadUTF8(true)
                                : p.ReadASCII();
                        }
                        else
                        {
                            Log.Error(
                                "BOOKGUMP: The server is sending a page number GREATER than the allowed number of pages in BOOK!"
                            );
                        }
                    }

                    if (lineCnt < ModernBookGump.MAX_BOOK_LINES)
                    {
                        for (int line = lineCnt; line < ModernBookGump.MAX_BOOK_LINES; line++)
                        {
                            gump.BookLines[pageNum * ModernBookGump.MAX_BOOK_LINES + line] =
                                string.Empty;
                        }
                    }
                }
                else
                {
                    Log.Error(
                        "BOOKGUMP: The server is sending a page number GREATER than the allowed number of pages in BOOK!"
                    );
                }
            }

            gump.ServerSetBookText();
        }



        private static void ClientViewRange(World world, ref StackDataReader p)
        {
            world.ClientViewRange = p.ReadUInt8();
        }

        private static void BulletinBoardData(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            switch (p.ReadUInt8())
            {
                case 0: // open

                    {
                        uint serial = p.ReadUInt32BE();
                        Item item = world.Items.Get(serial);

                        if (item != null)
                        {
                            BulletinBoardGump bulletinBoard = UIManager.GetGump<BulletinBoardGump>(
                                serial
                            );
                            bulletinBoard?.Dispose();

                            int x = (Client.Game.ClientBounds.Width >> 1) - 245;
                            int y = (Client.Game.ClientBounds.Height >> 1) - 205;

                            bulletinBoard = new BulletinBoardGump(world, item, x, y, p.ReadUTF8(22, true)); //p.ReadASCII(22));
                            UIManager.Add(bulletinBoard);

                            item.Opened = true;
                        }
                    }

                    break;

                case 1: // summary msg

                    {
                        uint boardSerial = p.ReadUInt32BE();
                        BulletinBoardGump bulletinBoard = UIManager.GetGump<BulletinBoardGump>(
                            boardSerial
                        );

                        if (bulletinBoard != null)
                        {
                            uint serial = p.ReadUInt32BE();
                            uint parendID = p.ReadUInt32BE();

                            // poster
                            int len = p.ReadUInt8();
                            string text = (len <= 0 ? string.Empty : p.ReadUTF8(len, true)) + " - ";

                            // subject
                            len = p.ReadUInt8();
                            text += (len <= 0 ? string.Empty : p.ReadUTF8(len, true)) + " - ";

                            // datetime
                            len = p.ReadUInt8();
                            text += (len <= 0 ? string.Empty : p.ReadUTF8(len, true));

                            bulletinBoard.AddBulletinObject(serial, text);
                        }
                    }

                    break;

                case 2: // message

                    {
                        uint boardSerial = p.ReadUInt32BE();
                        BulletinBoardGump bulletinBoard = UIManager.GetGump<BulletinBoardGump>(
                            boardSerial
                        );

                        if (bulletinBoard != null)
                        {
                            uint serial = p.ReadUInt32BE();

                            int len = p.ReadUInt8();
                            string poster = len > 0 ? p.ReadASCII(len) : string.Empty;

                            len = p.ReadUInt8();
                            string subject = len > 0 ? p.ReadUTF8(len, true) : string.Empty;

                            len = p.ReadUInt8();
                            string dataTime = len > 0 ? p.ReadASCII(len) : string.Empty;

                            p.Skip(4);

                            byte unk = p.ReadUInt8();

                            if (unk > 0)
                            {
                                p.Skip(unk * 4);
                            }

                            byte lines = p.ReadUInt8();

                            Span<char> span = stackalloc char[256];
                            ValueStringBuilder sb = new ValueStringBuilder(span);

                            for (int i = 0; i < lines; i++)
                            {
                                byte lineLen = p.ReadUInt8();

                                if (lineLen > 0)
                                {
                                    string putta = p.ReadUTF8(lineLen, true);
                                    sb.Append(putta);
                                    sb.Append('\n');
                                }
                            }

                            string msg = sb.ToString();
                            byte variant = (byte)(1 + (poster == world.Player.Name ? 1 : 0));

                            UIManager.Add(
                                new BulletinBoardItem(
                                    world,
                                    boardSerial,
                                    serial,
                                    poster,
                                    subject,
                                    dataTime,
                                    msg.TrimStart(),
                                    variant
                                )
                                {
                                    X = 40,
                                    Y = 40
                                }
                            );

                            sb.Dispose();
                        }
                    }

                    break;
            }
        }


        private static void Ping(World world, ref StackDataReader p)
        {
            NetClient.Socket.Statistics.PingReceived(p.ReadUInt8());
        }

        private static void BuyList(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            Item container = world.Items.Get(p.ReadUInt32BE());

            if (container == null)
            {
                return;
            }

            Mobile vendor = world.Mobiles.Get(container.Container);

            if (vendor == null)
            {
                return;
            }

            ShopGump gump = UIManager.GetGump<ShopGump>();

            if (gump != null && (gump.LocalSerial != vendor || !gump.IsBuyGump))
            {
                gump.Dispose();
                gump = null;
            }

            if (gump == null)
            {
                gump = new ShopGump(world, vendor, true, 150, 5);
                UIManager.Add(gump);
            }

            if (container.Layer == Layer.ShopBuyRestock || container.Layer == Layer.ShopBuy)
            {
                byte count = p.ReadUInt8();

                LinkedObject first = container.Items;

                if (first == null)
                {
                    return;
                }

                bool reverse = false;

                if (container.Graphic == 0x2AF8) //hardcoded logic in original client that we must match
                {
                    //sort the contents
                    first = container.SortContents<Item>((x, y) => x.X - y.X);
                }
                else
                {
                    //skip to last item and read in reverse later
                    reverse = true;

                    while (first?.Next != null)
                    {
                        first = first.Next;
                    }
                }

                for (int i = 0; i < count; i++)
                {
                    if (first == null)
                    {
                        break;
                    }

                    Item it = (Item)first;

                    it.Price = p.ReadUInt32BE();
                    byte nameLen = p.ReadUInt8();
                    string name = p.ReadASCII(nameLen);

                    if (world.OPL.TryGetNameAndData(it.Serial, out string s, out _))
                    {
                        it.Name = s;
                    }
                    else if (int.TryParse(name, out int cliloc))
                    {
                        it.Name = Client.Game.UO.FileManager.Clilocs.Translate(
                            cliloc,
                            $"\t{it.ItemData.Name}: \t{it.Amount}",
                            true
                        );
                    }
                    else if (string.IsNullOrEmpty(name))
                    {
                        it.Name = it.ItemData.Name;
                    }
                    else
                    {
                        it.Name = name;
                    }

                    if (reverse)
                    {
                        first = first.Previous;
                    }
                    else
                    {
                        first = first.Next;
                    }
                }
            }
        }



        private static void OpenMenu(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            ushort id = p.ReadUInt16BE();
            string name = p.ReadASCII(p.ReadUInt8());
            int count = p.ReadUInt8();

            ushort menuid = p.ReadUInt16BE();
            p.Seek(p.Position - 2);

            if (menuid != 0)
            {
                MenuGump gump = new MenuGump(world, serial, id, name) { X = 100, Y = 100 };

                int posX = 0;

                for (int i = 0; i < count; i++)
                {
                    ushort graphic = p.ReadUInt16BE();
                    ushort hue = p.ReadUInt16BE();
                    name = p.ReadASCII(p.ReadUInt8());

                    ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt(graphic);

                    if (artInfo.UV.Width != 0 && artInfo.UV.Height != 0)
                    {
                        int posY = artInfo.UV.Height;

                        if (posY >= 47)
                        {
                            posY = 0;
                        }
                        else
                        {
                            posY = (47 - posY) >> 1;
                        }

                        gump.AddItem(graphic, hue, name, posX, posY, i + 1);

                        posX += artInfo.UV.Width;
                    }
                }

                UIManager.Add(gump);
            }
            else
            {
                GrayMenuGump gump = new GrayMenuGump(world, serial, id, name)
                {
                    X = (Client.Game.ClientBounds.Width >> 1) - 200,
                    Y = (Client.Game.ClientBounds.Height >> 1) - ((121 + count * 21) >> 1)
                };

                int offsetY = 35 + gump.Height;
                int gumpHeight = 70 + offsetY;

                for (int i = 0; i < count; i++)
                {
                    p.Skip(4);
                    name = p.ReadASCII(p.ReadUInt8());

                    int addHeight = gump.AddItem(name, offsetY);

                    if (addHeight < 21)
                    {
                        addHeight = 21;
                    }

                    offsetY += addHeight - 1;
                    gumpHeight += addHeight;
                }

                offsetY += 5;

                gump.Add(
                    new Button(0, 0x1450, 0x1451, 0x1450)
                    {
                        ButtonAction = ButtonAction.Activate,
                        X = 70,
                        Y = offsetY
                    }
                );

                gump.Add(
                    new Button(1, 0x13B2, 0x13B3, 0x13B2)
                    {
                        ButtonAction = ButtonAction.Activate,
                        X = 200,
                        Y = offsetY
                    }
                );

                gump.SetHeight(gumpHeight);
                gump.WantUpdateSize = false;
                UIManager.Add(gump);
            }
        }




        private static void OpenBook(World world, ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();
            bool oldpacket = p[0] == 0x93;
            bool editable = p.ReadBool();

            if (!oldpacket)
            {
                editable = p.ReadBool();
            }
            else
            {
                p.Skip(1);
            }

            ModernBookGump bgump = UIManager.GetGump<ModernBookGump>(serial);

            if (bgump == null || bgump.IsDisposed)
            {
                ushort page_count = p.ReadUInt16BE();
                string title = oldpacket
                    ? p.ReadUTF8(60, true)
                    : p.ReadUTF8(p.ReadUInt16BE(), true);
                string author = oldpacket
                    ? p.ReadUTF8(30, true)
                    : p.ReadUTF8(p.ReadUInt16BE(), true);

                UIManager.Add(
                    new ModernBookGump(world, serial, page_count, title, author, editable, oldpacket)
                    {
                        X = 100,
                        Y = 100
                    }
                );

                NetClient.Socket.Send_BookPageDataRequest(serial, 1);
            }
            else
            {
                p.Skip(2);
                bgump.IsEditable = editable;
                bgump.SetTile(
                    oldpacket ? p.ReadUTF8(60, true) : p.ReadUTF8(p.ReadUInt16BE(), true),
                    editable
                );
                bgump.SetAuthor(
                    oldpacket ? p.ReadUTF8(30, true) : p.ReadUTF8(p.ReadUInt16BE(), true),
                    editable
                );
                bgump.UseNewHeader = !oldpacket;
                bgump.SetInScreen();
                bgump.BringOnTop();
            }
        }




        private static void MultiPlacement(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            bool allowGround = p.ReadBool();
            uint targID = p.ReadUInt32BE();
            byte flags = p.ReadUInt8();
            p.Seek(18);
            ushort multiID = p.ReadUInt16BE();
            ushort xOff = p.ReadUInt16BE();
            ushort yOff = p.ReadUInt16BE();
            ushort zOff = p.ReadUInt16BE();
            ushort hue = p.ReadUInt16BE();

            world.TargetManager.SetTargetingMulti(targID, multiID, xOff, yOff, zOff, hue);
        }

        private static void ASCIIPrompt(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            world.MessageManager.PromptData = new PromptData(ConsolePrompt.ASCII, p.ReadUInt64BE());
        }

        private static void SellList(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            Mobile vendor = world.Mobiles.Get(p.ReadUInt32BE());

            if (vendor == null)
            {
                return;
            }

            ushort countItems = p.ReadUInt16BE();

            if (countItems <= 0)
            {
                return;
            }

            ShopGump gump = UIManager.GetGump<ShopGump>(vendor);
            gump?.Dispose();
            gump = new ShopGump(world, vendor, false, 100, 0);

            for (int i = 0; i < countItems; i++)
            {
                uint serial = p.ReadUInt32BE();
                ushort graphic = p.ReadUInt16BE();
                ushort hue = p.ReadUInt16BE();
                ushort amount = p.ReadUInt16BE();
                ushort price = p.ReadUInt16BE();
                string name = p.ReadASCII(p.ReadUInt16BE());
                bool fromcliloc = false;

                if (int.TryParse(name, out int clilocnum))
                {
                    name = Client.Game.UO.FileManager.Clilocs.GetString(clilocnum);
                    fromcliloc = true;
                }
                else if (string.IsNullOrEmpty(name))
                {
                    bool success = world.OPL.TryGetNameAndData(serial, out name, out _);

                    if (!success)
                    {
                        name = Client.Game.UO.FileManager.TileData.StaticData[graphic].Name;
                    }
                }

                //if (string.IsNullOrEmpty(item.Name))
                //    item.Name = name;

                gump.AddItem(serial, graphic, hue, amount, price, name, fromcliloc);
            }

            UIManager.Add(gump);
        }




        private static void OpenUrl(World world, ref StackDataReader p)
        {
            string url = p.ReadASCII();

            if (!string.IsNullOrEmpty(url))
            {
                PlatformHelper.LaunchBrowser(url);
            }
        }

        private static void TipWindow(World world, ref StackDataReader p)
        {
            byte flag = p.ReadUInt8();

            if (flag == 1)
            {
                return;
            }

            uint tip = p.ReadUInt32BE();
            string str = p.ReadASCII(p.ReadUInt16BE())?.Replace('\r', '\n');

            int x = 20;
            int y = 20;

            if (flag == 0)
            {
                x = 200;
                y = 100;
            }

            UIManager.Add(new TipNoticeGump(world, tip, flag, str) { X = x, Y = y });
        }


        private static void TextEntryDialog(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            byte parentID = p.ReadUInt8();
            byte buttonID = p.ReadUInt8();

            ushort textLen = p.ReadUInt16BE();
            string text = p.ReadASCII(textLen);

            bool haveCancel = p.ReadBool();
            byte variant = p.ReadUInt8();
            uint maxLength = p.ReadUInt32BE();

            ushort descLen = p.ReadUInt16BE();
            string desc = p.ReadASCII(descLen);

            TextEntryDialogGump gump = new TextEntryDialogGump(
                world,
                serial,
                143,
                172,
                variant,
                (int)maxLength,
                text,
                desc,
                buttonID,
                parentID
            )
            {
                CanCloseWithRightClick = haveCancel
            };

            UIManager.Add(gump);
        }



        private static void OpenGump(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            uint sender = p.ReadUInt32BE();
            uint gumpID = p.ReadUInt32BE();
            int x = (int)p.ReadUInt32BE();
            int y = (int)p.ReadUInt32BE();

            ushort cmdLen = p.ReadUInt16BE();
            string cmd = p.ReadASCII(cmdLen);

            ushort textLinesCount = p.ReadUInt16BE();

            string[] lines = new string[textLinesCount];

            for (int i = 0; i < textLinesCount; ++i)
            {
                int length = p.ReadUInt16BE();

                if (length > 0)
                {
                    lines[i] = p.ReadUnicodeBE(length);
                }
                else
                {
                    lines[i] = string.Empty;
                }
            }

            //for (int i = 0, index = p.Position; i < textLinesCount; i++)
            //{
            //    int length = ((p[index++] << 8) | p[index++]) << 1;
            //    int true_length = 0;

            //    while (true_length < length)
            //    {
            //        if (((p[index + true_length++] << 8) | p[index + true_length++]) << 1 == '\0')
            //        {
            //            break;
            //        }
            //    }

            //    unsafe
            //    {

            //        fixed (byte* ptr = &p.Buffer[index])
            //        {
            //            lines[i] = Encoding.BigEndianUnicode.GetString(ptr, true_length);
            //        }
            //    }
            //    index += length;
            //}

            CreateGump(world, sender, gumpID, x, y, cmd, lines);
        }


        private static void Help(World world, ref StackDataReader p) { }


        private static void EnableLockedFeatures(World world, ref StackDataReader p)
        {
            LockedFeatureFlags flags = 0;

            if (Client.Game.UO.Version >= Utility.ClientVersion.CV_60142)
            {
                flags = (LockedFeatureFlags)p.ReadUInt32BE();
            }
            else
            {
                flags = (LockedFeatureFlags)p.ReadUInt16BE();
            }

            world.ClientLockedFeatures.SetFlags(flags);

            world.ChatManager.ChatIsEnabled = world.ClientLockedFeatures.Flags.HasFlag(
                LockedFeatureFlags.T2A
            )
                ? ChatStatus.Enabled
                : 0;

            BodyConvFlags bcFlags = 0;
            if (flags.HasFlag(LockedFeatureFlags.UOR))
                bcFlags |= BodyConvFlags.Anim1 | BodyConvFlags.Anim2;
            if (flags.HasFlag(LockedFeatureFlags.LBR))
                bcFlags |= BodyConvFlags.Anim1;
            if (flags.HasFlag(LockedFeatureFlags.AOS))
                bcFlags |= BodyConvFlags.Anim2;
            if (flags.HasFlag(LockedFeatureFlags.SE))
                bcFlags |= BodyConvFlags.Anim3;
            if (flags.HasFlag(LockedFeatureFlags.ML))
                bcFlags |= BodyConvFlags.Anim4;

            Client.Game.UO.Animations.UpdateAnimationTable(bcFlags);
        }

        private static void DisplayQuestArrow(World world, ref StackDataReader p)
        {
            bool display = p.ReadBool();
            ushort mx = p.ReadUInt16BE();
            ushort my = p.ReadUInt16BE();

            uint serial = 0;

            if (Client.Game.UO.Version >= Utility.ClientVersion.CV_7090)
            {
                serial = p.ReadUInt32BE();
            }

            QuestArrowGump arrow = UIManager.GetGump<QuestArrowGump>(serial);

            if (display)
            {
                if (arrow == null)
                {
                    UIManager.Add(new QuestArrowGump(world, serial, mx, my));
                }
                else
                {
                    arrow.SetRelativePosition(mx, my);
                }
            }
            else
            {
                if (arrow != null)
                {
                    arrow.Dispose();
                }
            }
        }



        private static void ExtendedCommand(World world, ref StackDataReader p)
        {
            ushort cmd = p.ReadUInt16BE();

            switch (cmd)
            {
                case 0:
                    break;

                //===========================================================================================
                //===========================================================================================
                case 1: // fast walk prevention
                    for (int i = 0; i < 6; i++)
                    {
                        world.Player.Walker.FastWalkStack.SetValue(i, p.ReadUInt32BE());
                    }

                    break;

                //===========================================================================================
                //===========================================================================================
                case 2: // add key to fast walk stack
                    world.Player.Walker.FastWalkStack.AddValue(p.ReadUInt32BE());

                    break;

                //===========================================================================================
                //===========================================================================================
                case 4: // close generic gump
                    uint ser = p.ReadUInt32BE();
                    int button = (int)p.ReadUInt32BE();

                    LinkedListNode<Gump> first = UIManager.Gumps.First;

                    while (first != null)
                    {
                        LinkedListNode<Gump> nextGump = first.Next;

                        if (first.Value.ServerSerial == ser && first.Value.IsFromServer)
                        {
                            if (button != 0)
                            {
                                (first.Value as Gump)?.OnButtonClick(button);
                            }
                            else
                            {
                                if (first.Value.CanMove)
                                {
                                    UIManager.SavePosition(ser, first.Value.Location);
                                }
                                else
                                {
                                    UIManager.RemovePosition(ser);
                                }
                            }

                            first.Value.Dispose();
                        }

                        first = nextGump;
                    }

                    break;

                //===========================================================================================
                //===========================================================================================
                case 6: //party
                    world.Party.ParsePacket(ref p);

                    break;

                //===========================================================================================
                //===========================================================================================
                case 8: // map change
                    world.MapIndex = p.ReadUInt8();

                    break;

                //===========================================================================================
                //===========================================================================================
                case 0x0C: // close statusbar gump
                    UIManager.GetGump<HealthBarGump>(p.ReadUInt32BE())?.Dispose();

                    break;

                //===========================================================================================
                //===========================================================================================
                case 0x10: // display equip info
                    Item item = world.Items.Get(p.ReadUInt32BE());

                    if (item == null)
                    {
                        return;
                    }

                    uint cliloc = p.ReadUInt32BE();
                    string str = string.Empty;

                    if (cliloc > 0)
                    {
                        str = Client.Game.UO.FileManager.Clilocs.GetString((int)cliloc, true);

                        if (!string.IsNullOrEmpty(str))
                        {
                            item.Name = str;
                        }

                        world.MessageManager.HandleMessage(
                            item,
                            str,
                            item.Name,
                            0x3B2,
                            MessageType.Regular,
                            3,
                            TextType.OBJECT,
                            true
                        );
                    }

                    str = string.Empty;
                    ushort crafterNameLen = 0;
                    uint next = p.ReadUInt32BE();

                    Span<char> span = stackalloc char[256];
                    ValueStringBuilder strBuffer = new ValueStringBuilder(span);
                    if (next == 0xFFFFFFFD)
                    {
                        crafterNameLen = p.ReadUInt16BE();

                        if (crafterNameLen > 0)
                        {
                            strBuffer.Append(ResGeneral.CraftedBy);
                            strBuffer.Append(p.ReadASCII(crafterNameLen));
                        }
                    }

                    if (crafterNameLen != 0)
                    {
                        next = p.ReadUInt32BE();
                    }

                    if (next == 0xFFFFFFFC)
                    {
                        strBuffer.Append("[Unidentified");
                    }

                    byte count = 0;

                    while (p.Position < p.Length - 4)
                    {
                        if (count != 0 || next == 0xFFFFFFFD || next == 0xFFFFFFFC)
                        {
                            next = p.ReadUInt32BE();
                        }

                        short charges = (short)p.ReadUInt16BE();
                        string attr = Client.Game.UO.FileManager.Clilocs.GetString((int)next);

                        if (attr != null)
                        {
                            if (charges == -1)
                            {
                                if (count > 0)
                                {
                                    strBuffer.Append("/");
                                    strBuffer.Append(attr);
                                }
                                else
                                {
                                    strBuffer.Append(" [");
                                    strBuffer.Append(attr);
                                }
                            }
                            else
                            {
                                strBuffer.Append("\n[");
                                strBuffer.Append(attr);
                                strBuffer.Append(" : ");
                                strBuffer.Append(charges.ToString());
                                strBuffer.Append("]");
                                count += 20;
                            }
                        }

                        count++;
                    }

                    if (count < 20 && count > 0 || next == 0xFFFFFFFC && count == 0)
                    {
                        strBuffer.Append(']');
                    }

                    if (strBuffer.Length != 0)
                    {
                        world.MessageManager.HandleMessage(
                            item,
                            strBuffer.ToString(),
                            item.Name,
                            0x3B2,
                            MessageType.Regular,
                            3,
                            TextType.OBJECT,
                            true
                        );
                    }

                    strBuffer.Dispose();

                    NetClient.Socket.Send_MegaClilocRequest_Old(item);

                    break;

                //===========================================================================================
                //===========================================================================================
                case 0x11:
                    break;

                //===========================================================================================
                //===========================================================================================
                case 0x14: // display popup/context menu
                    UIManager.ShowGamePopup(
                        new PopupMenuGump(world, PopupMenuData.Parse(ref p))
                        {
                            X = world.DelayedObjectClickManager.LastMouseX,
                            Y = world.DelayedObjectClickManager.LastMouseY
                        }
                    );

                    break;

                //===========================================================================================
                //===========================================================================================
                case 0x16: // close user interface windows
                    uint id = p.ReadUInt32BE();
                    uint serial = p.ReadUInt32BE();

                    switch (id)
                    {
                        case 1: // paperdoll
                            UIManager.GetGump<PaperDollGump>(serial)?.Dispose();

                            break;

                        case 2: //statusbar
                            UIManager.GetGump<HealthBarGump>(serial)?.Dispose();

                            if (serial == world.Player.Serial)
                            {
                                StatusGumpBase.GetStatusGump()?.Dispose();
                            }

                            break;

                        case 8: // char profile
                            UIManager.GetGump<ProfileGump>()?.Dispose();

                            break;

                        case 0x0C: //container
                            UIManager.GetGump<ContainerGump>(serial)?.Dispose();

                            break;
                    }

                    break;

                //===========================================================================================
                //===========================================================================================
                case 0x18: // enable map patches

                    if (Client.Game.UO.FileManager.Maps.ApplyPatches(ref p))
                    {
                        //List<GameObject> list = new List<GameObject>();

                        //foreach (int i in World.Map.GetUsedChunks())
                        //{
                        //    Chunk chunk = World.Map.Chunks[i];

                        //    for (int xx = 0; xx < 8; xx++)
                        //    {
                        //        for (int yy = 0; yy < 8; yy++)
                        //        {
                        //            Tile tile = chunk.Tiles[xx, yy];

                        //            for (GameObject obj = tile.FirstNode; obj != null; obj = obj.Right)
                        //            {
                        //                if (!(obj is Static) && !(obj is Land))
                        //                {
                        //                    list.Add(obj);
                        //                }
                        //            }
                        //        }
                        //    }
                        //}


                        int map = world.MapIndex;
                        world.MapIndex = -1;
                        world.MapIndex = map;

                        Log.Trace("Map Patches applied.");
                    }

                    break;

                //===========================================================================================
                //===========================================================================================
                case 0x19: //extened stats
                    byte version = p.ReadUInt8();
                    serial = p.ReadUInt32BE();

                    switch (version)
                    {
                        case 0:
                            Mobile bonded = world.Mobiles.Get(serial);

                            if (bonded == null)
                            {
                                break;
                            }

                            bool dead = p.ReadBool();
                            bonded.IsDead = dead;

                            break;

                        case 2:

                            if (serial == world.Player)
                            {
                                byte updategump = p.ReadUInt8();
                                byte state = p.ReadUInt8();

                                world.Player.StrLock = (Lock)((state >> 4) & 3);
                                world.Player.DexLock = (Lock)((state >> 2) & 3);
                                world.Player.IntLock = (Lock)(state & 3);

                                StatusGumpBase.GetStatusGump()?.RequestUpdateContents();
                            }

                            break;

                        case 5:

                            int pos = p.Position;
                            byte zero = p.ReadUInt8();
                            byte type2 = p.ReadUInt8();

                            if (type2 == 0xFF)
                            {
                                byte status = p.ReadUInt8();
                                ushort animation = p.ReadUInt16BE();
                                ushort frame = p.ReadUInt16BE();

                                if (status == 0 && animation == 0 && frame == 0)
                                {
                                    p.Seek(pos);
                                    goto case 0;
                                }

                                Mobile mobile = world.Mobiles.Get(serial);

                                if (mobile != null)
                                {
                                    mobile.SetAnimation(
                                        Mobile.GetReplacedObjectAnimation(mobile.Graphic, animation)
                                    );
                                    mobile.ExecuteAnimation = false;
                                    mobile.AnimIndex = (byte)frame;
                                }
                            }
                            else if (world.Player != null && serial == world.Player)
                            {
                                p.Seek(pos);
                                goto case 2;
                            }

                            break;
                    }

                    break;

                //===========================================================================================
                //===========================================================================================
                case 0x1B: // new spellbook content
                    p.Skip(2);
                    Item spellbook = world.GetOrCreateItem(p.ReadUInt32BE());
                    spellbook.Graphic = p.ReadUInt16BE();
                    spellbook.Clear();
                    ushort type = p.ReadUInt16BE();

                    for (int j = 0; j < 2; j++)
                    {
                        uint spells = 0;

                        for (int i = 0; i < 4; i++)
                        {
                            spells |= (uint)(p.ReadUInt8() << (i * 8));
                        }

                        for (int i = 0; i < 32; i++)
                        {
                            if ((spells & (1 << i)) != 0)
                            {
                                ushort cc = (ushort)(j * 32 + i + 1);
                                // FIXME: should i call Item.Create ?
                                Item spellItem = Item.Create(world, cc); // new Item()
                                spellItem.Serial = cc;
                                spellItem.Graphic = 0x1F2E;
                                spellItem.Amount = cc;
                                spellItem.Container = spellbook;
                                spellbook.PushToBack(spellItem);
                            }
                        }
                    }

                    UIManager.GetGump<SpellbookGump>(spellbook)?.RequestUpdateContents();

                    break;

                //===========================================================================================
                //===========================================================================================
                case 0x1D: // house revision state
                    serial = p.ReadUInt32BE();
                    uint revision = p.ReadUInt32BE();

                    Item multi = world.Items.Get(serial);

                    if (multi == null)
                    {
                        world.HouseManager.Remove(serial);
                    }

                    if (
                        !world.HouseManager.TryGetHouse(serial, out House house)
                        || !house.IsCustom
                        || house.Revision != revision
                    )
                    {
                        Handler._customHouseRequests.Add(serial);
                    }
                    else
                    {
                        house.Generate();
                        world.BoatMovingManager.ClearSteps(serial);

                        UIManager.GetGump<MiniMapGump>()?.RequestUpdateContents();

                        if (world.HouseManager.EntityIntoHouse(serial, world.Player))
                        {
                            Client.Game.GetScene<GameScene>()?.UpdateMaxDrawZ(true);
                        }
                    }

                    break;

                //===========================================================================================
                //===========================================================================================
                case 0x20:
                    serial = p.ReadUInt32BE();
                    type = p.ReadUInt8();
                    ushort graphic = p.ReadUInt16BE();
                    ushort x = p.ReadUInt16BE();
                    ushort y = p.ReadUInt16BE();
                    sbyte z = p.ReadInt8();

                    switch (type)
                    {
                        case 1: // update
                            break;

                        case 2: // remove
                            break;

                        case 3: // update multi pos
                            break;

                        case 4: // begin
                            HouseCustomizationGump gump = UIManager.GetGump<HouseCustomizationGump>();

                            if (gump != null)
                            {
                                break;
                            }

                            gump = new HouseCustomizationGump(world, serial, 50, 50);
                            UIManager.Add(gump);

                            break;

                        case 5: // end
                            UIManager.GetGump<HouseCustomizationGump>(serial)?.Dispose();

                            break;
                    }

                    break;

                //===========================================================================================
                //===========================================================================================
                case 0x21:

                    for (int i = 0; i < 2; i++)
                    {
                        world.Player.Abilities[i] &= (Ability)0x7F;
                    }

                    break;

                //===========================================================================================
                //===========================================================================================
                case 0x22:
                    p.Skip(1);

                    Entity en = world.Get(p.ReadUInt32BE());

                    if (en != null)
                    {
                        byte damage = p.ReadUInt8();

                        if (damage > 0)
                        {
                            world.WorldTextManager.AddDamage(en, damage);
                        }
                    }

                    break;

                case 0x25:

                    ushort spell = p.ReadUInt16BE();
                    bool active = p.ReadBool();

                    foreach (Gump g in UIManager.Gumps)
                    {
                        if (!g.IsDisposed && g.IsVisible)
                        {
                            if (g is UseSpellButtonGump spellButton && spellButton.SpellID == spell)
                            {
                                if (active)
                                {
                                    spellButton.Hue = 38;
                                    world.ActiveSpellIcons.Add(spell);
                                }
                                else
                                {
                                    spellButton.Hue = 0;
                                    world.ActiveSpellIcons.Remove(spell);
                                }

                                break;
                            }
                        }
                    }

                    break;

                //===========================================================================================
                //===========================================================================================
                case 0x26:
                    byte val = p.ReadUInt8();

                    if (val > (int)CharacterSpeedType.FastUnmountAndCantRun)
                    {
                        val = 0;
                    }

                    world.Player.SpeedMode = (CharacterSpeedType)val;

                    break;

                case 0x2A:
                    bool isfemale = p.ReadBool();
                    byte race = p.ReadUInt8();

                    UIManager.GetGump<RaceChangeGump>()?.Dispose();
                    UIManager.Add(new RaceChangeGump(world, isfemale, race));
                    break;

                case 0x2B:
                    serial = p.ReadUInt16BE();
                    byte animID = p.ReadUInt8();
                    byte frameCount = p.ReadUInt8();

                    foreach (Mobile m in world.Mobiles.Values)
                    {
                        if ((m.Serial & 0xFFFF) == serial)
                        {
                            m.SetAnimation(animID);
                            m.AnimIndex = frameCount;
                            m.ExecuteAnimation = false;

                            break;
                        }
                    }

                    break;

                case 0xBEEF: // ClassicUO commands

                    type = p.ReadUInt16BE();

                    break;

                default:
                    Log.Warn($"Unhandled 0xBF - sub: {cmd.ToHex()}");

                    break;
            }
        }


        private static void UnicodePrompt(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            world.MessageManager.PromptData = new PromptData(ConsolePrompt.Unicode, p.ReadUInt64BE());
        }


        private static void InvalidMapEnable(World world, ref StackDataReader p) { }

        private static void ParticleEffect3D(World world, ref StackDataReader p) { }

        private static void GetUserServerPingGodClientR(World world, ref StackDataReader p) { }

        private static void GlobalQueCount(World world, ref StackDataReader p) { }

        private static void ConfigurationFileR(World world, ref StackDataReader p) { }


        private static void GenericAOSCommandsR(World world, ref StackDataReader p) { }

        private static unsafe void ReadUnsafeCustomHouseData(
            ReadOnlySpan<byte> source,
            int sourcePosition,
            int dlen,
            int clen,
            int planeZ,
            int planeMode,
            short minX,
            short minY,
            short maxY,
            Item item,
            House house
        )
        {
            //byte* decompressedBytes = stackalloc byte[dlen];
            bool ismovable = item.ItemData.IsMultiMovable;

            byte[] buffer = null;
            Span<byte> span =
                dlen <= 1024
                    ? stackalloc byte[dlen]
                    : (buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(dlen));

            try
            {
                var result = ZLib.Decompress(source.Slice(sourcePosition, clen), span.Slice(0, dlen));
                var reader = new StackDataReader(span.Slice(0, dlen));

                ushort id = 0;
                sbyte x = 0,
                    y = 0,
                    z = 0;

                switch (planeMode)
                {
                    case 0:
                        int c = dlen / 5;

                        for (uint i = 0; i < c; i++)
                        {
                            id = reader.ReadUInt16BE();
                            x = reader.ReadInt8();
                            y = reader.ReadInt8();
                            z = reader.ReadInt8();

                            if (id != 0)
                            {
                                house.Add(
                                    id,
                                    0,
                                    (ushort)(item.X + x),
                                    (ushort)(item.Y + y),
                                    (sbyte)(item.Z + z),
                                    true,
                                    ismovable
                                );
                            }
                        }

                        break;

                    case 1:

                        if (planeZ > 0)
                        {
                            z = (sbyte)((planeZ - 1) % 4 * 20 + 7);
                        }
                        else
                        {
                            z = 0;
                        }

                        c = dlen >> 2;

                        for (uint i = 0; i < c; i++)
                        {
                            id = reader.ReadUInt16BE();
                            x = reader.ReadInt8();
                            y = reader.ReadInt8();

                            if (id != 0)
                            {
                                house.Add(
                                    id,
                                    0,
                                    (ushort)(item.X + x),
                                    (ushort)(item.Y + y),
                                    (sbyte)(item.Z + z),
                                    true,
                                    ismovable
                                );
                            }
                        }

                        break;

                    case 2:
                        short offX = 0,
                            offY = 0;
                        short multiHeight = 0;

                        if (planeZ > 0)
                        {
                            z = (sbyte)((planeZ - 1) % 4 * 20 + 7);
                        }
                        else
                        {
                            z = 0;
                        }

                        if (planeZ <= 0)
                        {
                            offX = minX;
                            offY = minY;
                            multiHeight = (short)(maxY - minY + 2);
                        }
                        else if (planeZ <= 4)
                        {
                            offX = (short)(minX + 1);
                            offY = (short)(minY + 1);
                            multiHeight = (short)(maxY - minY);
                        }
                        else
                        {
                            offX = minX;
                            offY = minY;
                            multiHeight = (short)(maxY - minY + 1);
                        }

                        c = dlen >> 1;

                        for (uint i = 0; i < c; i++)
                        {
                            id = reader.ReadUInt16BE();
                            x = (sbyte)(i / multiHeight + offX);
                            y = (sbyte)(i % multiHeight + offY);

                            if (id != 0)
                            {
                                house.Add(
                                    id,
                                    0,
                                    (ushort)(item.X + x),
                                    (ushort)(item.Y + y),
                                    (sbyte)(item.Z + z),
                                    true,
                                    ismovable
                                );
                            }
                        }

                        break;
                }

                reader.Release();
            }
            finally
            {
                if (buffer != null)
                {
                    System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }

        private static void CustomHouse(World world, ref StackDataReader p)
        {
            bool compressed = p.ReadUInt8() == 0x03;
            bool enableReponse = p.ReadBool();
            uint serial = p.ReadUInt32BE();
            Item foundation = world.Items.Get(serial);
            uint revision = p.ReadUInt32BE();

            if (foundation == null)
            {
                return;
            }

            Rectangle? multi = foundation.MultiInfo;

            if (!foundation.IsMulti || multi == null)
            {
                return;
            }

            p.Skip(4);

            if (!world.HouseManager.TryGetHouse(foundation, out House house))
            {
                house = new House(world, foundation, revision, true);
                world.HouseManager.Add(foundation, house);
            }
            else
            {
                house.ClearComponents(true);
                house.Revision = revision;
                house.IsCustom = true;
            }

            short minX = (short)multi.Value.X;
            short minY = (short)multi.Value.Y;
            short maxY = (short)multi.Value.Height;

            if (minX == 0 && minY == 0 && maxY == 0 && multi.Value.Width == 0)
            {
                Log.Warn(
                    "[CustomHouse (0xD8) - Invalid multi dimentions. Maybe missing some installation required files"
                );

                return;
            }

            byte planes = p.ReadUInt8();

            house.ClearCustomHouseComponents(0);

            for (int plane = 0; plane < planes; plane++)
            {
                uint header = p.ReadUInt32BE();
                int dlen = (int)(((header & 0xFF0000) >> 16) | ((header & 0xF0) << 4));
                int clen = (int)(((header & 0xFF00) >> 8) | ((header & 0x0F) << 8));
                int planeZ = (int)((header & 0x0F000000) >> 24);
                int planeMode = (int)((header & 0xF0000000) >> 28);

                if (clen <= 0)
                {
                    continue;
                }

                ReadUnsafeCustomHouseData(
                    p.Buffer,
                    p.Position,
                    dlen,
                    clen,
                    planeZ,
                    planeMode,
                    minX,
                    minY,
                    maxY,
                    foundation,
                    house
                );

                p.Skip(clen);
            }

            if (world.CustomHouseManager != null)
            {
                world.CustomHouseManager.GenerateFloorPlace();

                UIManager.GetGump<HouseCustomizationGump>(house.Serial)?.Update();
            }

            UIManager.GetGump<MiniMapGump>()?.RequestUpdateContents();

            if (world.HouseManager.EntityIntoHouse(serial, world.Player))
            {
                Client.Game.GetScene<GameScene>()?.UpdateMaxDrawZ(true);
            }

            world.BoatMovingManager.ClearSteps(serial);
        }

        private static void CharacterTransferLog(World world, ref StackDataReader p) { }

        private static void OPLInfo(World world, ref StackDataReader p)
        {
            if (world.ClientFeatures.TooltipsEnabled)
            {
                uint serial = p.ReadUInt32BE();
                uint revision = p.ReadUInt32BE();

                if (!world.OPL.IsRevisionEquals(serial, revision))
                {
                    AddMegaClilocRequest(serial);
                }
            }
        }

        private static void OpenCompressedGump(World world, ref StackDataReader p)
        {
            uint sender = p.ReadUInt32BE();
            uint gumpID = p.ReadUInt32BE();
            uint x = p.ReadUInt32BE();
            uint y = p.ReadUInt32BE();
            uint clen = p.ReadUInt32BE() - 4;
            int dlen = (int)p.ReadUInt32BE();
            byte[] decData = System.Buffers.ArrayPool<byte>.Shared.Rent(dlen);
            string layout;

            try
            {
                ZLib.Decompress(p.Buffer.Slice(p.Position, (int)clen), decData.AsSpan(0, dlen));

                layout = Encoding.UTF8.GetString(decData.AsSpan(0, dlen));
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(decData);
            }

            p.Skip((int)clen);

            uint linesNum = p.ReadUInt32BE();
            string[] lines = new string[linesNum];

            try
            {
                if (linesNum != 0)
                {
                    clen = p.ReadUInt32BE() - 4;
                    dlen = (int)p.ReadUInt32BE();
                    decData = System.Buffers.ArrayPool<byte>.Shared.Rent(dlen);

                    try
                    {
                        ZLib.Decompress(p.Buffer.Slice(p.Position, (int)clen), decData.AsSpan(0, dlen));
                        p.Skip((int)clen);

                        var reader = new StackDataReader(decData.AsSpan(0, dlen));

                        for (int i = 0; i < linesNum; ++i)
                        {
                            int remaining = reader.Remaining;

                            if (remaining >= 2)
                            {
                                int length = reader.ReadUInt16BE();

                                if (length > 0)
                                {
                                    lines[i] = reader.ReadUnicodeBE(length);
                                }
                                else
                                {
                                    lines[i] = string.Empty;
                                }
                            }
                            else
                            {
                                lines[i] = string.Empty;
                            }
                        }

                        reader.Release();

                        //for (int i = 0, index = 0; i < linesNum && index < dlen; i++)
                        //{
                        //    int length = ((decData[index++] << 8) | decData[index++]) << 1;
                        //    int true_length = 0;

                        //    for (int k = 0; k < length && true_length < length && index + true_length < dlen; ++k, true_length += 2)
                        //    {
                        //        ushort c = (ushort)(((decData[index + true_length] << 8) | decData[index + true_length + 1]) << 1);

                        //        if (c == '\0')
                        //        {
                        //            break;
                        //        }
                        //    }

                        //    lines[i] = Encoding.BigEndianUnicode.GetString(decData, index, true_length);

                        //    index += length;
                        //}
                    }
                    finally
                    {
                        System.Buffers.ArrayPool<byte>.Shared.Return(decData);
                    }
                }

                CreateGump(world, sender, gumpID, (int)x, (int)y, layout, lines);
            }
            finally
            {
                //System.Buffers.ArrayPool<string>.Shared.Return(lines);
            }
        }




        private static void DisplayWaypoint(World world, ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();
            ushort x = p.ReadUInt16BE();
            ushort y = p.ReadUInt16BE();
            sbyte z = p.ReadInt8();
            byte map = p.ReadUInt8();
            WaypointsType type = (WaypointsType)p.ReadUInt16BE();
            bool ignoreobject = p.ReadUInt16BE() != 0;
            uint cliloc = p.ReadUInt32BE();
            string name = p.ReadUnicodeLE();
        }

        private static void RemoveWaypoint(World world, ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();
        }

        private static void KrriosClientSpecial(World world, ref StackDataReader p)
        {
            byte type = p.ReadUInt8();

            switch (type)
            {
                case 0x00: // accepted
                    Log.Trace("Krrios special packet accepted");
                    world.WMapManager.SetACKReceived();
                    world.WMapManager.SetEnable(true);

                    break;

                case 0x01: // custom party info
                case 0x02: // guild track info
                    bool locations = type == 0x01 || p.ReadBool();

                    uint serial;

                    while ((serial = p.ReadUInt32BE()) != 0)
                    {
                        if (locations)
                        {
                            ushort x = p.ReadUInt16BE();
                            ushort y = p.ReadUInt16BE();
                            byte map = p.ReadUInt8();
                            int hits = type == 1 ? 0 : p.ReadUInt8();

                            world.WMapManager.AddOrUpdate(
                                serial,
                                x,
                                y,
                                hits,
                                map,
                                type == 0x02,
                                null,
                                true
                            );
                        }
                    }

                    world.WMapManager.RemoveUnupdatedWEntity();

                    break;

                case 0x03: // runebook contents
                    break;

                case 0x04: // guardline data
                    break;

                case 0xF0:
                    break;

                case 0xFE:

                    Client.Game.EnqueueAction(5000, () =>
                    {
                        Log.Info("Razor ACK sent");
                        NetClient.Socket.Send_RazorACK();
                    });

                    break;
            }
        }

        private static void FreeshardListR(World world, ref StackDataReader p) { }




        private static void AddItemToContainer(
            World world,
            uint serial,
            ushort graphic,
            ushort amount,
            ushort x,
            ushort y,
            ushort hue,
            uint containerSerial
        )
        {
            if (Client.Game.UO.GameCursor.ItemHold.Serial == serial)
            {
                if (Client.Game.UO.GameCursor.ItemHold.Dropped)
                {
                    Console.WriteLine("ADD ITEM TO CONTAINER -- CLEAR HOLD");
                    Client.Game.UO.GameCursor.ItemHold.Clear();
                }

                //else if (ItemHold.Graphic == graphic && ItemHold.Amount == amount &&
                //         ItemHold.Container == containerSerial)
                //{
                //    ItemHold.Enabled = false;
                //    ItemHold.Dropped = false;
                //}
            }

            Entity container = world.Get(containerSerial);

            if (container == null)
            {
                Log.Warn($"No container ({containerSerial}) found");

                //container = world.GetOrCreateItem(containerSerial);
                return;
            }

            Item item = world.Items.Get(serial);

            if (SerialHelper.IsMobile(serial))
            {
                world.RemoveMobile(serial, true);
                Log.Warn("AddItemToContainer function adds mobile as Item");
            }

            if (item != null && (container.Graphic != 0x2006 || item.Layer == Layer.Invalid))
            {
                world.RemoveItem(item, true);
            }

            item = world.GetOrCreateItem(serial);
            item.Graphic = graphic;
            item.CheckGraphicChange();
            item.Amount = amount;
            item.FixHue(hue);
            item.X = x;
            item.Y = y;
            item.Z = 0;

            world.RemoveItemFromContainer(item);
            item.Container = containerSerial;
            container.PushToBack(item);

            if (SerialHelper.IsMobile(containerSerial))
            {
                Mobile m = world.Mobiles.Get(containerSerial);
                Item secureBox = m?.GetSecureTradeBox();

                if (secureBox != null)
                {
                    UIManager.GetTradingGump(secureBox)?.RequestUpdateContents();
                }
                else
                {
                    UIManager.GetGump<PaperDollGump>(containerSerial)?.RequestUpdateContents();
                }
            }
            else if (SerialHelper.IsItem(containerSerial))
            {
                Gump gump = UIManager.GetGump<BulletinBoardGump>(containerSerial);

                if (gump != null)
                {
                    NetClient.Socket.Send_BulletinBoardRequestMessageSummary(
                        containerSerial,
                        serial
                    );
                }
                else
                {
                    gump = UIManager.GetGump<SpellbookGump>(containerSerial);

                    if (gump == null)
                    {
                        gump = UIManager.GetGump<ContainerGump>(containerSerial);

                        if (gump != null)
                        {
                            ((ContainerGump)gump).CheckItemControlPosition(item);
                        }

                        if (ProfileManager.CurrentProfile.GridLootType > 0)
                        {
                            GridLootGump grid_gump = UIManager.GetGump<GridLootGump>(
                                containerSerial
                            );

                            if (
                                grid_gump == null
                                && SerialHelper.IsValid(_requestedGridLoot)
                                && _requestedGridLoot == containerSerial
                            )
                            {
                                grid_gump = new GridLootGump(world, _requestedGridLoot);
                                UIManager.Add(grid_gump);
                                _requestedGridLoot = 0;
                            }

                            grid_gump?.RequestUpdateContents();
                        }
                    }

                    if (gump != null)
                    {
                        if (SerialHelper.IsItem(containerSerial))
                        {
                            ((Item)container).Opened = true;
                        }

                        gump.RequestUpdateContents();
                    }
                }
            }

            UIManager.GetTradingGump(containerSerial)?.RequestUpdateContents();
        }

        private static void UpdateGameObject(
            World world,
            uint serial,
            ushort graphic,
            byte graphic_inc,
            ushort count,
            ushort x,
            ushort y,
            sbyte z,
            Direction direction,
            ushort hue,
            Flags flagss,
            int UNK,
            byte type,
            ushort UNK_2
        )
        {
            Mobile mobile = null;
            Item item = null;
            Entity obj = world.Get(serial);

            if (
                Client.Game.UO.GameCursor.ItemHold.Enabled
                && Client.Game.UO.GameCursor.ItemHold.Serial == serial
            )
            {
                if (SerialHelper.IsValid(Client.Game.UO.GameCursor.ItemHold.Container))
                {
                    if (Client.Game.UO.GameCursor.ItemHold.Layer == 0)
                    {
                        UIManager
                            .GetGump<ContainerGump>(Client.Game.UO.GameCursor.ItemHold.Container)
                            ?.RequestUpdateContents();
                    }
                    else
                    {
                        UIManager
                            .GetGump<PaperDollGump>(Client.Game.UO.GameCursor.ItemHold.Container)
                            ?.RequestUpdateContents();
                    }
                }

                Client.Game.UO.GameCursor.ItemHold.UpdatedInWorld = true;
            }

            bool created = false;

            if (obj == null || obj.IsDestroyed)
            {
                created = true;

                if (SerialHelper.IsMobile(serial) && type != 3)
                {
                    mobile = world.GetOrCreateMobile(serial);

                    if (mobile == null)
                    {
                        return;
                    }

                    obj = mobile;
                    mobile.Graphic = (ushort)(graphic + graphic_inc);
                    mobile.CheckGraphicChange();
                    mobile.Direction = direction & Direction.Up;
                    mobile.FixHue(hue);
                    mobile.X = x;
                    mobile.Y = y;
                    mobile.Z = z;
                    mobile.Flags = flagss;
                }
                else
                {
                    item = world.GetOrCreateItem(serial);

                    if (item == null)
                    {
                        return;
                    }

                    obj = item;
                }
            }
            else
            {
                if (obj is Item item1)
                {
                    item = item1;

                    if (SerialHelper.IsValid(item.Container))
                    {
                        world.RemoveItemFromContainer(item);
                    }
                }
                else
                {
                    mobile = (Mobile)obj;
                }
            }

            if (obj == null)
            {
                return;
            }

            if (item != null)
            {
                if (graphic != 0x2006)
                {
                    graphic += graphic_inc;
                }

                if (type == 2)
                {
                    item.IsMulti = true;
                    item.WantUpdateMulti =
                        (graphic & 0x3FFF) != item.Graphic
                        || item.X != x
                        || item.Y != y
                        || item.Z != z
                        || item.Hue != hue;
                    item.Graphic = (ushort)(graphic & 0x3FFF);
                }
                else
                {
                    item.IsDamageable = type == 3;
                    item.IsMulti = false;
                    item.Graphic = graphic;
                }

                item.X = x;
                item.Y = y;
                item.Z = z;
                item.LightID = (byte)direction;

                if (graphic == 0x2006)
                {
                    item.Layer = (Layer)direction;
                }

                item.FixHue(hue);

                if (count == 0)
                {
                    count = 1;
                }

                item.Amount = count;
                item.Flags = flagss;
                item.Direction = direction;
                item.CheckGraphicChange(item.AnimIndex);
            }
            else
            {
                graphic += graphic_inc;

                if (serial != world.Player)
                {
                    Direction cleaned_dir = direction & Direction.Up;
                    bool isrun = (direction & Direction.Running) != 0;

                    if (world.Get(mobile) == null || mobile.X == 0xFFFF && mobile.Y == 0xFFFF)
                    {
                        mobile.X = x;
                        mobile.Y = y;
                        mobile.Z = z;
                        mobile.Direction = cleaned_dir;
                        mobile.IsRunning = isrun;
                        mobile.ClearSteps();
                    }

                    if (!mobile.EnqueueStep(x, y, z, cleaned_dir, isrun))
                    {
                        mobile.X = x;
                        mobile.Y = y;
                        mobile.Z = z;
                        mobile.Direction = cleaned_dir;
                        mobile.IsRunning = isrun;
                        mobile.ClearSteps();
                    }
                }

                mobile.Graphic = (ushort)(graphic & 0x3FFF);
                mobile.FixHue(hue);
                mobile.Flags = flagss;
            }

            if (created && !obj.IsClicked)
            {
                if (mobile != null)
                {
                    if (ProfileManager.CurrentProfile.ShowNewMobileNameIncoming)
                    {
                        GameActions.SingleClick(world, serial);
                    }
                }
                else if (graphic == 0x2006)
                {
                    if (ProfileManager.CurrentProfile.ShowNewCorpseNameIncoming)
                    {
                        GameActions.SingleClick(world, serial);
                    }
                }
            }

            if (mobile != null)
            {
                mobile.SetInWorldTile(mobile.X, mobile.Y, mobile.Z);

                if (created)
                {
                    // This is actually a way to get all Hp from all new mobiles.
                    // Real UO client does it only when LastAttack == serial.
                    // We force to close suddenly.
                    GameActions.RequestMobileStatus(world, serial);

                    //if (TargetManager.LastAttack != serial)
                    //{
                    //    GameActions.SendCloseStatus(serial);
                    //}
                }
            }
            else
            {
                if (
                    Client.Game.UO.GameCursor.ItemHold.Serial == serial
                    && Client.Game.UO.GameCursor.ItemHold.Dropped
                )
                {
                    // we want maintain the item data due to the denymoveitem packet
                    //ItemHold.Clear();
                    Client.Game.UO.GameCursor.ItemHold.Enabled = false;
                    Client.Game.UO.GameCursor.ItemHold.Dropped = false;
                }

                if (item.OnGround)
                {
                    item.SetInWorldTile(item.X, item.Y, item.Z);

                    if (graphic == 0x2006 && ProfileManager.CurrentProfile.AutoOpenCorpses)
                    {
                        world.Player.TryOpenCorpses();
                    }
                }
            }
        }


        private static void ClearContainerAndRemoveItems(
            World world,
            Entity container,
            bool remove_unequipped = false
        )
        {
            if (container == null || container.IsEmpty)
            {
                return;
            }

            LinkedObject first = container.Items;
            LinkedObject new_first = null;

            while (first != null)
            {
                LinkedObject next = first.Next;
                Item it = (Item)first;

                if (remove_unequipped && it.Layer != 0)
                {
                    if (new_first == null)
                    {
                        new_first = first;
                    }
                }
                else
                {
                    world.RemoveItem(it, true);
                }

                first = next;
            }

            container.Items = remove_unequipped ? new_first : null;
        }

        private static Gump CreateGump(
            World world,
            uint sender,
            uint gumpID,
            int x,
            int y,
            string layout,
            string[] lines
        )
        {
            List<string> cmdlist = _parser.GetTokens(layout);
            int cmdlen = cmdlist.Count;

            if (cmdlen <= 0)
            {
                return null;
            }

            Gump gump = null;
            bool mustBeAdded = true;

            if (UIManager.GetGumpCachePosition(gumpID, out Point pos))
            {
                x = pos.X;
                y = pos.Y;

                for (
                    LinkedListNode<Gump> last = UIManager.Gumps.Last;
                    last != null;
                    last = last.Previous
                )
                {
                    Control g = last.Value;

                    if (!g.IsDisposed && g.LocalSerial == sender && g.ServerSerial == gumpID)
                    {
                        g.Clear();
                        gump = g as Gump;
                        mustBeAdded = false;

                        break;
                    }
                }
            }
            else
            {
                UIManager.SavePosition(gumpID, new Point(x, y));
            }

            if (gump == null)
            {
                gump = new Gump(world, sender, gumpID)
                {
                    X = x,
                    Y = y,
                    CanMove = true,
                    CanCloseWithRightClick = true,
                    CanCloseWithEsc = true,
                    InvalidateContents = false,
                    IsFromServer = true
                };
            }

            int group = 0;
            int page = 0;

            bool textBoxFocused = false;

            for (int cnt = 0; cnt < cmdlen; cnt++)
            {
                List<string> gparams = _cmdparser.GetTokens(cmdlist[cnt], false);

                if (gparams.Count == 0)
                {
                    continue;
                }

                string entry = gparams[0];

                if (string.Equals(entry, "button", StringComparison.InvariantCultureIgnoreCase))
                {
                    gump.Add(new Button(gparams), page);
                }
                else if (
                    string.Equals(
                        entry,
                        "buttontileart",
                        StringComparison.InvariantCultureIgnoreCase
                    )
                )
                {
                    gump.Add(new ButtonTileArt(gparams), page);
                }
                else if (
                    string.Equals(
                        entry,
                        "checkertrans",
                        StringComparison.InvariantCultureIgnoreCase
                    )
                )
                {
                    var checkerTrans = new CheckerTrans(gparams);
                    gump.Add(checkerTrans, page);
                    ApplyTrans(
                        gump,
                        page,
                        checkerTrans.X,
                        checkerTrans.Y,
                        checkerTrans.Width,
                        checkerTrans.Height
                    );
                }
                else if (
                    string.Equals(entry, "croppedtext", StringComparison.InvariantCultureIgnoreCase)
                )
                {
                    gump.Add(new CroppedText(gparams, lines), page);
                }
                else if (
                    string.Equals(entry, "tilepicasgumppic", StringComparison.InvariantCultureIgnoreCase) ||
                    string.Equals(entry, "gumppic", StringComparison.InvariantCultureIgnoreCase)
                )
                {
                    GumpPic pic;
                    var isVirtue = gparams.Count >= 6
                        && gparams[5].IndexOf(
                            "virtuegumpitem",
                            StringComparison.InvariantCultureIgnoreCase
                        ) >= 0;

                    if (isVirtue)
                    {
                        pic = new VirtueGumpPic(world, gparams);
                        pic.ContainsByBounds = true;

                        string s,
                            lvl;

                        switch (pic.Hue)
                        {
                            case 2403:
                                lvl = "";

                                break;

                            case 1154:
                            case 1547:
                            case 2213:
                            case 235:
                            case 18:
                            case 2210:
                            case 1348:
                                lvl = "Seeker of ";

                                break;

                            case 2404:
                            case 1552:
                            case 2216:
                            case 2302:
                            case 2118:
                            case 618:
                            case 2212:
                            case 1352:
                                lvl = "Follower of ";

                                break;

                            case 43:
                            case 53:
                            case 1153:
                            case 33:
                            case 318:
                            case 67:
                            case 98:
                                lvl = "Knight of ";

                                break;

                            case 2406:
                                if (pic.Graphic == 0x6F)
                                {
                                    lvl = "Seeker of ";
                                }
                                else
                                {
                                    lvl = "Knight of ";
                                }

                                break;

                            default:
                                lvl = "";

                                break;
                        }

                        switch (pic.Graphic)
                        {
                            case 0x69:
                                s = Client.Game.UO.FileManager.Clilocs.GetString(1051000 + 2);

                                break;

                            case 0x6A:
                                s = Client.Game.UO.FileManager.Clilocs.GetString(1051000 + 7);

                                break;

                            case 0x6B:
                                s = Client.Game.UO.FileManager.Clilocs.GetString(1051000 + 5);

                                break;

                            case 0x6D:
                                s = Client.Game.UO.FileManager.Clilocs.GetString(1051000 + 6);

                                break;

                            case 0x6E:
                                s = Client.Game.UO.FileManager.Clilocs.GetString(1051000 + 1);

                                break;

                            case 0x6F:
                                s = Client.Game.UO.FileManager.Clilocs.GetString(1051000 + 3);

                                break;

                            case 0x70:
                                s = Client.Game.UO.FileManager.Clilocs.GetString(1051000 + 4);

                                break;

                            case 0x6C:
                            default:
                                s = Client.Game.UO.FileManager.Clilocs.GetString(1051000);

                                break;
                        }

                        if (string.IsNullOrEmpty(s))
                        {
                            s = "Unknown virtue";
                        }

                        pic.SetTooltip(lvl + s, 100);
                    }
                    else
                    {
                        pic = new GumpPic(gparams);
                    }

                    gump.Add(pic, page);
                }
                else if (
                    string.Equals(
                        entry,
                        "gumppictiled",
                        StringComparison.InvariantCultureIgnoreCase
                    )
                )
                {
                    gump.Add(new GumpPicTiled(gparams), page);
                }
                else if (
                    string.Equals(entry, "htmlgump", StringComparison.InvariantCultureIgnoreCase)
                )
                {
                    gump.Add(new HtmlControl(gparams, lines), page);
                }
                else if (
                    string.Equals(entry, "xmfhtmlgump", StringComparison.InvariantCultureIgnoreCase)
                )
                {
                    gump.Add(
                        new HtmlControl(
                            int.Parse(gparams[1]),
                            int.Parse(gparams[2]),
                            int.Parse(gparams[3]),
                            int.Parse(gparams[4]),
                            int.Parse(gparams[6]) == 1,
                            int.Parse(gparams[7]) != 0,
                            gparams[6] != "0" && gparams[7] == "2",
                            Client.Game.UO.FileManager.Clilocs.GetString(int.Parse(gparams[5].Replace("#", ""))),
                            0,
                            true
                        )
                        {
                            IsFromServer = true
                        },
                        page
                    );
                }
                else if (
                    string.Equals(
                        entry,
                        "xmfhtmlgumpcolor",
                        StringComparison.InvariantCultureIgnoreCase
                    )
                )
                {
                    int color = int.Parse(gparams[8]);

                    if (color == 0x7FFF)
                    {
                        color = 0x00FFFFFF;
                    }

                    gump.Add(
                        new HtmlControl(
                            int.Parse(gparams[1]),
                            int.Parse(gparams[2]),
                            int.Parse(gparams[3]),
                            int.Parse(gparams[4]),
                            int.Parse(gparams[6]) == 1,
                            int.Parse(gparams[7]) != 0,
                            gparams[6] != "0" && gparams[7] == "2",
                            Client.Game.UO.FileManager.Clilocs.GetString(int.Parse(gparams[5].Replace("#", ""))),
                            color,
                            true
                        )
                        {
                            IsFromServer = true
                        },
                        page
                    );
                }
                else if (
                    string.Equals(entry, "xmfhtmltok", StringComparison.InvariantCultureIgnoreCase)
                )
                {
                    int color = int.Parse(gparams[7]);

                    if (color == 0x7FFF)
                    {
                        color = 0x00FFFFFF;
                    }

                    StringBuilder sb = null;

                    if (gparams.Count >= 9)
                    {
                        sb = new StringBuilder();

                        for (int i = 9; i < gparams.Count; i++)
                        {
                            sb.Append('\t');
                            sb.Append(gparams[i]);
                        }
                    }

                    gump.Add(
                        new HtmlControl(
                            int.Parse(gparams[1]),
                            int.Parse(gparams[2]),
                            int.Parse(gparams[3]),
                            int.Parse(gparams[4]),
                            int.Parse(gparams[5]) == 1,
                            int.Parse(gparams[6]) != 0,
                            gparams[5] != "0" && gparams[6] == "2",
                            sb == null
                                ? Client.Game.UO.FileManager.Clilocs.GetString(
                                    int.Parse(gparams[8].Replace("#", ""))
                                )
                                : Client.Game.UO.FileManager.Clilocs.Translate(
                                    int.Parse(gparams[8].Replace("#", "")),
                                    sb.ToString().Trim('@').Replace('@', '\t')
                                ),
                            color,
                            true
                        )
                        {
                            IsFromServer = true
                        },
                        page
                    );
                }
                else if (string.Equals(entry, "page", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (gparams.Count >= 2)
                    {
                        page = int.Parse(gparams[1]);
                    }
                }
                else if (
                    string.Equals(entry, "resizepic", StringComparison.InvariantCultureIgnoreCase)
                )
                {
                    gump.Add(new ResizePic(gparams), page);
                }
                else if (string.Equals(entry, "text", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (gparams.Count >= 5)
                    {
                        gump.Add(new Label(gparams, lines), page);
                    }
                }
                else if (
                    string.Equals(
                        entry,
                        "textentrylimited",
                        StringComparison.InvariantCultureIgnoreCase
                    )
                    || string.Equals(
                        entry,
                        "textentry",
                        StringComparison.InvariantCultureIgnoreCase
                    )
                )
                {
                    StbTextBox textBox = new StbTextBox(gparams, lines);

                    if (!textBoxFocused)
                    {
                        textBox.SetKeyboardFocus();
                        textBoxFocused = true;
                    }

                    gump.Add(textBox, page);
                }
                else if (
                    string.Equals(entry, "tilepichue", StringComparison.InvariantCultureIgnoreCase) ||
                    string.Equals(entry, "tilepic", StringComparison.InvariantCultureIgnoreCase)
                )
                {
                    gump.Add(new StaticPic(gparams), page);
                }
                else if (
                    string.Equals(entry, "noclose", StringComparison.InvariantCultureIgnoreCase)
                )
                {
                    gump.CanCloseWithRightClick = false;
                }
                else if (
                    string.Equals(entry, "nodispose", StringComparison.InvariantCultureIgnoreCase)
                )
                {
                    gump.CanCloseWithEsc = false;
                }
                else if (
                    string.Equals(entry, "nomove", StringComparison.InvariantCultureIgnoreCase)
                )
                {
                    gump.CanMove = false;
                }
                else if (
                    string.Equals(entry, "group", StringComparison.InvariantCultureIgnoreCase)
                    || string.Equals(entry, "endgroup", StringComparison.InvariantCultureIgnoreCase)
                )
                {
                    group++;
                }
                else if (string.Equals(entry, "radio", StringComparison.InvariantCultureIgnoreCase))
                {
                    gump.Add(new RadioButton(group, gparams, lines), page);
                }
                else if (
                    string.Equals(entry, "checkbox", StringComparison.InvariantCultureIgnoreCase)
                )
                {
                    gump.Add(new Checkbox(gparams, lines), page);
                }
                else if (
                    string.Equals(entry, "tooltip", StringComparison.InvariantCultureIgnoreCase)
                )
                {
                    string text = null;

                    if (gparams.Count > 2 && gparams[2].Length != 0)
                    {
                        string args = gparams[2];

                        for (int i = 3; i < gparams.Count; i++)
                        {
                            args += '\t' + gparams[i];
                        }

                        if (args.Length == 0)
                        {
                            text = Client.Game.UO.FileManager.Clilocs.GetString(int.Parse(gparams[1]));
                            Log.Error(
                                $"String '{args}' too short, something wrong with gump tooltip: {text}"
                            );
                        }
                        else
                        {
                            text = Client.Game.UO.FileManager.Clilocs.Translate(
                                int.Parse(gparams[1]),
                                args,
                                false
                            );
                        }
                    }
                    else
                    {
                        text = Client.Game.UO.FileManager.Clilocs.GetString(int.Parse(gparams[1]));
                    }

                    Control last =
                        gump.Children.Count != 0 ? gump.Children[gump.Children.Count - 1] : null;

                    if (last != null)
                    {
                        if (last.HasTooltip)
                        {
                            if (last.Tooltip is string s)
                            {
                                s += '\n' + text;
                                last.SetTooltip(s);
                            }
                        }
                        else
                        {
                            last.SetTooltip(text);
                        }

                        last.Priority = ClickPriority.High;
                        last.AcceptMouseInput = true;
                    }
                }
                else if (
                    string.Equals(
                        entry,
                        "itemproperty",
                        StringComparison.InvariantCultureIgnoreCase
                    )
                )
                {
                    if (world.ClientFeatures.TooltipsEnabled && gump.Children.Count != 0)
                    {
                        gump.Children[gump.Children.Count - 1].SetTooltip(
                            SerialHelper.Parse(gparams[1])
                        );

                        if (
                            uint.TryParse(gparams[1], out uint s)
                            && (!world.OPL.TryGetRevision(s, out uint rev) || rev == 0)
                        )
                        {
                            AddMegaClilocRequest(s);
                        }
                    }
                }
                else if (
                    string.Equals(entry, "noresize", StringComparison.InvariantCultureIgnoreCase)
                ) { }
                else if (
                    string.Equals(entry, "mastergump", StringComparison.InvariantCultureIgnoreCase)
                )
                {
                    gump.MasterGumpSerial = gparams.Count > 0 ? SerialHelper.Parse(gparams[1]) : 0;
                }
                else if (string.Equals(entry, "picinpichued", StringComparison.InvariantCultureIgnoreCase) ||
                    string.Equals(entry, "picinpicphued", StringComparison.InvariantCultureIgnoreCase) ||
                    string.Equals(entry, "picinpic", StringComparison.InvariantCultureIgnoreCase)
                )
                {
                    if (gparams.Count > 7)
                    {
                        var g = gump.Add(new GumpPicInPic(gparams), page);

                        if (gparams.Count > 8)
                        {
                            g.Hue = UInt16Converter.Parse(gparams[8]);

                            if (string.Equals(entry, "picinpicphued", StringComparison.InvariantCultureIgnoreCase))
                            {
                                g.IsPartialHue = true;
                            }
                        }
                    }
                }
                else if (string.Equals(entry, "\0", StringComparison.InvariantCultureIgnoreCase))
                {
                    //This gump is null terminated: Breaking
                    break;
                }
                else if (string.Equals(entry, "gumppichued", StringComparison.InvariantCultureIgnoreCase) ||
                         string.Equals(entry, "gumppicphued", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (gparams.Count >= 3)
                        gump.Add(new GumpPic(gparams));
                }
                else if (string.Equals(entry, "togglelimitgumpscale", StringComparison.InvariantCultureIgnoreCase))
                {
                    // ??
                }
                else
                {
                    Log.Warn($"Invalid Gump Command: \"{gparams[0]}\"");
                }
            }

            if (mustBeAdded)
            {
                UIManager.Add(gump);
            }

            gump.Update();
            gump.SetInScreen();

            return gump;
        }

        private static void ApplyTrans(
            Gump gump,
            int current_page,
            int x,
            int y,
            int width,
            int height
        )
        {
            int x2 = x + width;
            int y2 = y + height;
            for (int i = 0; i < gump.Children.Count; i++)
            {
                Control child = gump.Children[i];
                bool canDraw = child.Page == 0 || current_page == child.Page;

                bool overlap =
                    (x < child.X + child.Width)
                    && (child.X < x2)
                    && (y < child.Y + child.Height)
                    && (child.Y < y2);

                if (canDraw && child.IsVisible && overlap)
                {
                    child.Alpha = 0.5f;
                }
            }
        }

        [Flags]
        private enum AffixType
        {
            Append = 0x00,
            Prepend = 0x01,
            System = 0x02
        }
    }
}
