// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
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
using System.IO;
using System.Text;

namespace ClassicUO.Network
{
    sealed class PacketHandlers
    {
        public delegate void OnPacketBufferReader(World world, ref StackDataReader p);

        internal static uint _requestedGridLoot;

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
        internal List<uint> _customHouseRequests = new List<uint>();
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
            Handler.Add(0x1B, EnterWorld);
            Handler.Add(0x55, LoginComplete);
            Handler.Add(0xBD, ClientVersion);
            Handler.Add(0x03, ClientTalk);
            Handler.Add(0x0B, Damage);
            Handler.Add(0x11, CharacterStatus);
            Handler.Add(0x15, FollowR);
            Handler.Add(0x16, NewHealthbarUpdate);
            Handler.Add(0x17, NewHealthbarUpdate);
            Handler.Add(0x1A, UpdateItem);
            Handler.Add(0x1C, Talk);
            Handler.Add(0x1D, DeleteObject);
            Handler.Add(0x20, UpdatePlayer);
            Handler.Add(0x21, DenyWalk);
            Handler.Add(0x22, ConfirmWalk);
            Handler.Add(0x23, DragAnimation);
            Handler.Add(0x24, OpenContainer);
            Handler.Add(0x25, UpdateContainedItem);
            Handler.Add(0x27, DenyMoveItem);
            Handler.Add(0x28, EndDraggingItem);
            Handler.Add(0x29, DropItemAccepted);
            Handler.Add(0x2C, DeathScreen);
            Handler.Add(0x2D, MobileAttributes);
            Handler.Add(0x2E, EquipItem);
            Handler.Add(0x2F, Swing);
            Handler.Add(0x32, Unknown_0x32);
            Handler.Add(0x38, Pathfinding);
            Handler.Add(0x3A, UpdateSkills);
            Handler.Add(0x3B, CloseVendorInterface);
            Handler.Add(0x3C, UpdateContainedItems);
            Handler.Add(0x4E, PersonalLightLevel);
            Handler.Add(0x4F, LightLevel);
            Handler.Add(0x54, PlaySoundEffect);
            Handler.Add(0x56, MapData);
            Handler.Add(0x5B, SetTime);
            Handler.Add(0x65, SetWeather);
            Handler.Add(0x66, BookData);
            Handler.Add(0x6C, TargetCursor);
            Handler.Add(0x6D, PlayMusic);
            Handler.Add(0x6F, SecureTrading);
            Handler.Add(0x6E, CharacterAnimation);
            Handler.Add(0x70, GraphicEffect);
            Handler.Add(0x71, BulletinBoardData);
            Handler.Add(0x72, Warmode);
            Handler.Add(0x73, Ping);
            Handler.Add(0x74, BuyList);
            Handler.Add(0x77, UpdateCharacter);
            Handler.Add(0x78, UpdateObject);
            Handler.Add(0x7C, OpenMenu);
            Handler.Add(0x88, OpenPaperdoll);
            Handler.Add(0x89, CorpseEquipment);
            Handler.Add(0x90, DisplayMap);
            Handler.Add(0x93, OpenBook);
            Handler.Add(0x95, DyeData);
            Handler.Add(0x97, MovePlayer);
            Handler.Add(0x98, UpdateName);
            Handler.Add(0x99, MultiPlacement);
            Handler.Add(0x9A, ASCIIPrompt);
            Handler.Add(0x9E, SellList);
            Handler.Add(0xA1, UpdateHitpoints);
            Handler.Add(0xA2, UpdateMana);
            Handler.Add(0xA3, UpdateStamina);
            Handler.Add(0xA5, OpenUrl);
            Handler.Add(0xA6, TipWindow);
            Handler.Add(0xAA, AttackCharacter);
            Handler.Add(0xAB, TextEntryDialog);
            Handler.Add(0xAF, DisplayDeath);
            Handler.Add(0xAE, UnicodeTalk);
            Handler.Add(0xB0, OpenGump);
            Handler.Add(0xB2, ChatMessage);
            Handler.Add(0xB7, Help);
            Handler.Add(0xB8, CharacterProfile);
            Handler.Add(0xB9, EnableLockedFeatures);
            Handler.Add(0xBA, DisplayQuestArrow);
            Handler.Add(0xBB, UltimaMessengerR);
            Handler.Add(0xBC, Season);
            Handler.Add(0xBE, AssistVersion);
            Handler.Add(0xBF, ExtendedCommand);
            Handler.Add(0xC0, GraphicEffect);
            Handler.Add(0xC1, DisplayClilocString);
            Handler.Add(0xC2, UnicodePrompt);
            Handler.Add(0xC4, Semivisible);
            Handler.Add(0xC6, InvalidMapEnable);
            Handler.Add(0xC7, GraphicEffect);
            Handler.Add(0xC8, ClientViewRange);
            Handler.Add(0xCA, GetUserServerPingGodClientR);
            Handler.Add(0xCB, GlobalQueCount);
            Handler.Add(0xCC, DisplayClilocString);
            Handler.Add(0xD0, ConfigurationFileR);
            Handler.Add(0xD1, Logout);
            Handler.Add(0xD2, UpdateCharacter);
            Handler.Add(0xD3, UpdateObject);
            Handler.Add(0xD4, OpenBook);
            Handler.Add(0xD6, MegaCliloc);
            Handler.Add(0xD7, GenericAOSCommandsR);
            Handler.Add(0xD8, CustomHouse);
            Handler.Add(0xDB, CharacterTransferLog);
            Handler.Add(0xDC, OPLInfo);
            Handler.Add(0xDD, OpenCompressedGump);
            Handler.Add(0xDE, UpdateMobileStatus);
            Handler.Add(0xDF, BuffDebuff);
            Handler.Add(0xE2, NewCharacterAnimation);
            Handler.Add(0xE3, KREncryptionResponse);
            Handler.Add(0xE5, DisplayWaypoint);
            Handler.Add(0xE6, RemoveWaypoint);
            Handler.Add(0xF0, KrriosClientSpecial);
            Handler.Add(0xF1, FreeshardListR);
            Handler.Add(0xF3, UpdateItemSA);
            Handler.Add(0xF5, DisplayMap);
            Handler.Add(0xF6, BoatMoving);
            Handler.Add(0xF7, PacketList);

            // login
            Handler.Add(0xA8, ServerListReceived);
            Handler.Add(0x8C, ReceiveServerRelay);
            Handler.Add(0x86, UpdateCharacterList);
            Handler.Add(0xA9, ReceiveCharacterList);
            Handler.Add(0x82, ReceiveLoginRejection);
            Handler.Add(0x85, ReceiveLoginRejection);
            Handler.Add(0x53, ReceiveLoginRejection);
            Handler.Add(0xFD, LoginDelay);
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
            byte cursorType = p.ReadUInt8();
            uint targetId = p.ReadUInt32BE();
            byte targetType = p.ReadUInt8();

            EventSink.RaiseTargetCursorReceived(new TargetCursorReceivedArgs(cursorType, targetId, targetType));
        }

        private static void SecureTrading(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            byte type = p.ReadUInt8();
            uint serial = p.ReadUInt32BE();

            switch (type)
            {
                case 0:
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

                    EventSink.RaiseTradeWindowOpened(new TradeWindowOpenArgs(serial, id1, id2, name));
                    break;
                }

                case 1:
                {
                    EventSink.RaiseTradeWindowClosed(new TradeWindowClosedArgs(serial));
                    break;
                }

                case 2:
                {
                    uint id1 = p.ReadUInt32BE();
                    uint id2 = p.ReadUInt32BE();

                    EventSink.RaiseTradeWindowAcceptUpdated(new TradeWindowAcceptUpdatedArgs(serial, id1 != 0, id2 != 0));
                    break;
                }

                case 3:
                case 4:
                {
                    uint v1 = p.ReadUInt32BE();
                    uint v2 = p.ReadUInt32BE();
                    bool isMine = type == 4;

                    EventSink.RaiseTradeWindowCurrencyUpdated(new TradeWindowCurrencyUpdatedArgs(serial, isMine, v1, v2));
                    break;
                }
            }
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

        private static void Damage(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            ushort damage = p.ReadUInt16BE();

            EventSink.RaiseDamageReceived(new DamageReceivedArgs(serial, damage));
        }

        private static void CharacterStatus(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            Entity entity = world.Get(serial);

            if (entity == null)
            {
                return;
            }

            string oldName = entity.Name;
            entity.Name = p.ReadASCII(30);
            entity.Hits = p.ReadUInt16BE();
            entity.HitsMax = p.ReadUInt16BE();

            if (entity.HitsRequest == HitsRequestStatus.Pending)
            {
                entity.HitsRequest = HitsRequestStatus.Received;
            }

            if (SerialHelper.IsMobile(serial))
            {
                Mobile mobile = entity as Mobile;

                if (mobile == null)
                {
                    return;
                }

                mobile.IsRenamable = p.ReadBool();
                byte type = p.ReadUInt8();

                if (type > 0 && p.Position + 1 <= p.Length)
                {
                    mobile.IsFemale = p.ReadBool();

                    if (mobile == world.Player)
                    {
                        if (
                            !string.IsNullOrEmpty(world.Player.Name) && oldName != world.Player.Name
                        )
                        {
                            Client.Game.SetWindowTitle(world.Player.Name);
                        }

                        ushort str = p.ReadUInt16BE();
                        ushort dex = p.ReadUInt16BE();
                        ushort intell = p.ReadUInt16BE();
                        world.Player.Stamina = p.ReadUInt16BE();
                        world.Player.StaminaMax = p.ReadUInt16BE();
                        world.Player.Mana = p.ReadUInt16BE();
                        world.Player.ManaMax = p.ReadUInt16BE();
                        world.Player.Gold = p.ReadUInt32BE();
                        world.Player.PhysicalResistance = (short)p.ReadUInt16BE();
                        world.Player.Weight = p.ReadUInt16BE();

                        if (
                            world.Player.Strength != 0
                            && ProfileManager.CurrentProfile != null
                            && ProfileManager.CurrentProfile.ShowStatsChangedMessage
                        )
                        {
                            ushort currentStr = world.Player.Strength;
                            ushort currentDex = world.Player.Dexterity;
                            ushort currentInt = world.Player.Intelligence;

                            int deltaStr = str - currentStr;
                            int deltaDex = dex - currentDex;
                            int deltaInt = intell - currentInt;

                            if (deltaStr != 0)
                            {
                                GameActions.Print(
                                    world,
                                    string.Format(
                                        ResGeneral.Your0HasChangedBy1ItIsNow2,
                                        ResGeneral.Strength,
                                        deltaStr,
                                        str
                                    ),
                                    0x0170,
                                    MessageType.System,
                                    3,
                                    false
                                );
                            }

                            if (deltaDex != 0)
                            {
                                GameActions.Print(
                                    world,
                                    string.Format(
                                        ResGeneral.Your0HasChangedBy1ItIsNow2,
                                        ResGeneral.Dexterity,
                                        deltaDex,
                                        dex
                                    ),
                                    0x0170,
                                    MessageType.System,
                                    3,
                                    false
                                );
                            }

                            if (deltaInt != 0)
                            {
                                GameActions.Print(
                                    world,
                                    string.Format(
                                        ResGeneral.Your0HasChangedBy1ItIsNow2,
                                        ResGeneral.Intelligence,
                                        deltaInt,
                                        intell
                                    ),
                                    0x0170,
                                    MessageType.System,
                                    3,
                                    false
                                );
                            }
                        }

                        world.Player.Strength = str;
                        world.Player.Dexterity = dex;
                        world.Player.Intelligence = intell;

                        if (type >= 5) //ML
                        {
                            world.Player.WeightMax = p.ReadUInt16BE();
                            byte race = p.ReadUInt8();

                            if (race == 0)
                            {
                                race = 1;
                            }

                            world.Player.Race = (RaceType)race;
                        }
                        else
                        {
                            if (Client.Game.UO.Version >= Utility.ClientVersion.CV_500A)
                            {
                                world.Player.WeightMax = (ushort)(
                                    7 * (world.Player.Strength >> 1) + 40
                                );
                            }
                            else
                            {
                                world.Player.WeightMax = (ushort)(world.Player.Strength * 4 + 25);
                            }
                        }

                        if (type >= 3) //Renaissance
                        {
                            world.Player.StatsCap = (short)p.ReadUInt16BE();
                            world.Player.Followers = p.ReadUInt8();
                            world.Player.FollowersMax = p.ReadUInt8();
                        }

                        if (type >= 4) //AOS
                        {
                            world.Player.FireResistance = (short)p.ReadUInt16BE();
                            world.Player.ColdResistance = (short)p.ReadUInt16BE();
                            world.Player.PoisonResistance = (short)p.ReadUInt16BE();
                            world.Player.EnergyResistance = (short)p.ReadUInt16BE();
                            world.Player.Luck = p.ReadUInt16BE();
                            world.Player.DamageMin = (short)p.ReadUInt16BE();
                            world.Player.DamageMax = (short)p.ReadUInt16BE();
                            world.Player.TithingPoints = p.ReadUInt32BE();
                        }

                        if (type >= 6)
                        {
                            world.Player.MaxPhysicResistence =
                                p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                            world.Player.MaxFireResistence =
                                p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                            world.Player.MaxColdResistence =
                                p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                            world.Player.MaxPoisonResistence =
                                p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                            world.Player.MaxEnergyResistence =
                                p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                            world.Player.DefenseChanceIncrease =
                                p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                            world.Player.MaxDefenseChanceIncrease =
                                p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                            world.Player.HitChanceIncrease =
                                p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                            world.Player.SwingSpeedIncrease =
                                p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                            world.Player.DamageIncrease =
                                p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                            world.Player.LowerReagentCost =
                                p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                            world.Player.SpellDamageIncrease =
                                p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                            world.Player.FasterCastRecovery =
                                p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                            world.Player.FasterCasting =
                                p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                            world.Player.LowerManaCost =
                                p.Position + 2 > p.Length ? (short)0 : (short)p.ReadUInt16BE();
                        }
                    }
                }

                if (mobile == world.Player)
                {
                    world.UoAssist.SignalHits();
                    world.UoAssist.SignalStamina();
                    world.UoAssist.SignalMana();
                }
            }
        }

        private static void FollowR(World world, ref StackDataReader p)
        {
            uint tofollow = p.ReadUInt32BE();
            uint isfollowing = p.ReadUInt32BE();
        }

        private static void NewHealthbarUpdate(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            if (p[0] == 0x16 && Client.Game.UO.Version < Utility.ClientVersion.CV_500A)
            {
                return;
            }

            uint mobSerial = p.ReadUInt32BE();
            ushort count = p.ReadUInt16BE();

            for (int i = 0; i < count; i++)
            {
                ushort type = p.ReadUInt16BE();
                bool enabled = p.ReadBool();

                EventSink.RaiseHealthBarStateChanged(new HealthBarStateChangedArgs(mobSerial, type, enabled));
            }
        }

        private static void UpdateItem(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            ushort count = 0;
            byte graphicInc = 0;
            byte direction = 0;
            ushort hue = 0;
            byte flags = 0;
            byte type = 0;

            if ((serial & 0x80000000) != 0)
            {
                serial &= 0x7FFFFFFF;
                count = 1;
            }

            ushort graphic = p.ReadUInt16BE();

            if ((graphic & 0x8000) != 0)
            {
                graphic &= 0x7FFF;
                graphicInc = p.ReadUInt8();
            }

            if (count > 0)
            {
                count = p.ReadUInt16BE();
            }
            else
            {
                count++;
            }

            ushort x = p.ReadUInt16BE();

            if ((x & 0x8000) != 0)
            {
                x &= 0x7FFF;
                direction = 1;
            }

            ushort y = p.ReadUInt16BE();

            if ((y & 0x8000) != 0)
            {
                y &= 0x7FFF;
                hue = 1;
            }

            if ((y & 0x4000) != 0)
            {
                y &= 0x3FFF;
                flags = 1;
            }

            if (direction != 0)
            {
                direction = p.ReadUInt8();
            }

            sbyte z = p.ReadInt8();

            if (hue != 0)
            {
                hue = p.ReadUInt16BE();
            }

            if (flags != 0)
            {
                flags = p.ReadUInt8();
            }

            //if (graphic != 0x2006)
            //    graphic += graphicInc;

            if (graphic >= 0x4000)
            {
                //graphic -= 0x4000;
                type = 2;
            }

            EventSink.RaiseItemUpdated(new ItemUpdatedArgs(serial, graphic, graphicInc, count, x, y, z, (Direction)direction, hue, (Flags)flags, type, count, 1));
        }

        private static void EnterWorld(World world, ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();

            world.CreatePlayer(serial);

            p.Skip(4);
            ushort enterGraphic = p.ReadUInt16BE();
            ushort x = p.ReadUInt16BE();
            ushort y = p.ReadUInt16BE();
            sbyte z = (sbyte)p.ReadUInt16BE();
            Direction enterDirection = (Direction)(p.ReadUInt8() & 0x7);

            EventSink.RaisePlayerEnteredWorld(new PlayerEnteredWorldArgs(serial, enterGraphic, x, y, z, enterDirection));
        }

        private static void Talk(World world, ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();
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

            EventSink.RaiseChatMessage(new ChatMessageArgs(serial, graphic, type, hue, (byte)font, name, text));
        }

        private static void DeleteObject(World world, ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();
            EventSink.RaiseObjectDeleted(new ObjectDeletedArgs(serial));
        }

        private static void UpdatePlayer(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            ushort graphic = p.ReadUInt16BE();
            byte graphic_inc = p.ReadUInt8();
            ushort hue = p.ReadUInt16BE();
            Flags flags = (Flags)p.ReadUInt8();
            ushort x = p.ReadUInt16BE();
            ushort y = p.ReadUInt16BE();
            ushort serverID = p.ReadUInt16BE();
            Direction direction = (Direction)p.ReadUInt8();
            sbyte z = p.ReadInt8();

            EventSink.RaisePlayerUpdated(new PlayerUpdatedArgs(serial, graphic, graphic_inc, hue, flags, x, y, z, serverID, direction));
        }

        private static void DenyWalk(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            byte seq = p.ReadUInt8();
            ushort x = p.ReadUInt16BE();
            ushort y = p.ReadUInt16BE();
            Direction direction = (Direction)p.ReadUInt8();
            direction &= Direction.Up;
            sbyte z = p.ReadInt8();

            EventSink.RaiseWalkDenied(new WalkDeniedArgs(seq, x, y, z, direction));
        }

        private static void ConfirmWalk(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            byte seq = p.ReadUInt8();
            byte noto = (byte)(p.ReadUInt8() & ~0x40);

            EventSink.RaiseWalkConfirmed(new WalkConfirmedArgs(seq, noto));
        }

        private static void DragAnimation(World world, ref StackDataReader p)
        {
            ushort graphic = p.ReadUInt16BE();
            graphic += p.ReadUInt8();
            ushort hue = p.ReadUInt16BE();
            ushort count = p.ReadUInt16BE();
            uint source = p.ReadUInt32BE();
            ushort sourceX = p.ReadUInt16BE();
            ushort sourceY = p.ReadUInt16BE();
            sbyte sourceZ = p.ReadInt8();
            uint dest = p.ReadUInt32BE();
            ushort destX = p.ReadUInt16BE();
            ushort destY = p.ReadUInt16BE();
            sbyte destZ = p.ReadInt8();

            EventSink.RaiseItemDragAnimation(new ItemDragAnimationArgs(graphic, hue, count, source, sourceX, sourceY, sourceZ, dest, destX, destY, destZ));

            //if (effect.AnimDataFrame.FrameCount != 0)
            //{
            //    effect.IntervalInMs = (uint) (effect.AnimDataFrame.FrameInterval * 45);
            //}
            //else
            //{
            //    effect.IntervalInMs = 13;
            //}
        }

        private static void OpenContainer(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            ushort graphic = p.ReadUInt16BE();

            EventSink.RaiseContainerOpened(new ContainerOpenedArgs(serial, graphic));
        }

        private static void UpdateContainedItem(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            ushort graphic = (ushort)(p.ReadUInt16BE() + p.ReadUInt8());
            ushort amount = Math.Max((ushort)1, p.ReadUInt16BE());
            ushort x = p.ReadUInt16BE();
            ushort y = p.ReadUInt16BE();

            if (Client.Game.UO.Version >= Utility.ClientVersion.CV_6017)
            {
                p.Skip(1);
            }

            uint containerSerial = p.ReadUInt32BE();
            ushort hue = p.ReadUInt16BE();

            EventSink.RaiseContainerItemAdded(new ContainerItemAddedArgs(serial, graphic, amount, x, y, containerSerial, hue));
        }

        private static void DenyMoveItem(World world, ref StackDataReader p)
        {
            byte code = p.ReadUInt8();

            EventSink.RaiseItemMoveDenied(new ItemMoveDeniedArgs(code));
        }

        private static void EndDraggingItem(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            EventSink.RaiseItemDragEnded(new ItemDragEndedArgs());
        }

        private static void DropItemAccepted(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            EventSink.RaiseItemDropAccepted(new ItemDropAcceptedArgs());
        }

        private static void DeathScreen(World world, ref StackDataReader p)
        {
            // todo
            byte action = p.ReadUInt8();

            EventSink.RaisePlayerDeath(new PlayerDeathArgs(action));

            if (action != 1)
            {
                world.Weather.Reset();

                Client.Game.Audio.PlayMusic(Client.Game.Audio.DeathMusicIndex, true);

                if (ProfileManager.CurrentProfile.EnableDeathScreen)
                {
                    world.Player.DeathScreenTimer = Time.Ticks + Constants.DEATH_SCREEN_TIMER;
                }

                GameActions.RequestWarMode(world.Player, false);
            }
        }

        private static void MobileAttributes(World world, ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();
            ushort hitsMax = p.ReadUInt16BE();
            ushort hits = p.ReadUInt16BE();

            if (SerialHelper.IsMobile(serial))
            {
                ushort manaMax = p.ReadUInt16BE();
                ushort mana = p.ReadUInt16BE();
                ushort stamMax = p.ReadUInt16BE();
                ushort stam = p.ReadUInt16BE();

                EventSink.RaiseMobileAttributesUpdated(new MobileAttributesUpdatedArgs(serial, hitsMax, hits, manaMax, mana, stamMax, stam));
            }
            else
            {
                EventSink.RaiseHitpointsUpdated(new HitpointsUpdatedArgs(serial, hitsMax, hits));
            }
        }

        private static void EquipItem(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            ushort eqGraphic = (ushort)(p.ReadUInt16BE() + p.ReadInt8());
            Layer eqLayer = (Layer)p.ReadUInt8();
            uint eqContainer = p.ReadUInt32BE();
            ushort eqHue = p.ReadUInt16BE();

            EventSink.RaiseItemEquipped(new ItemEquippedArgs(serial, eqGraphic, eqLayer, eqContainer, eqHue));
        }

        private static void Swing(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            p.Skip(1);

            uint attackers = p.ReadUInt32BE();

            if (attackers != world.Player)
            {
                return;
            }

            uint defenders = p.ReadUInt32BE();

            EventSink.RaiseCombatSwing(new CombatSwingArgs(attackers, defenders));
        }

        private static void Unknown_0x32(World world, ref StackDataReader p) { }

        private static void UpdateSkills(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            byte type = p.ReadUInt8();
            EventSink.RaiseSkillsUpdated(new SkillsUpdatedArgs(type));
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

        private static void Pathfinding(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            ushort x = p.ReadUInt16BE();
            ushort y = p.ReadUInt16BE();
            ushort z = p.ReadUInt16BE();

            EventSink.RaisePathfindingReceived(new PathfindingReceivedArgs(x, y, z));
        }

        private static void UpdateContainedItems(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            ushort count = p.ReadUInt16BE();
            bool firstEmitted = false;

            for (int i = 0; i < count; i++)
            {
                uint serial = p.ReadUInt32BE();
                ushort graphic = (ushort)(p.ReadUInt16BE() + p.ReadUInt8());
                ushort amount = Math.Max(p.ReadUInt16BE(), (ushort)1);
                ushort x = p.ReadUInt16BE();
                ushort y = p.ReadUInt16BE();

                if (Client.Game.UO.Version >= Utility.ClientVersion.CV_6017)
                {
                    p.Skip(1);
                }

                uint containerSerial = p.ReadUInt32BE();
                ushort hue = p.ReadUInt16BE();

                if (!firstEmitted)
                {
                    EventSink.RaiseContainerItemsReceived(new ContainerItemsReceivedArgs(containerSerial, count));
                    firstEmitted = true;
                }

                EventSink.RaiseContainerItemAdded(new ContainerItemAddedArgs(serial, graphic, amount, x, y, containerSerial, hue));
            }
        }

        private static void CloseVendorInterface(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();

            EventSink.RaiseVendorWindowClosed(new VendorWindowClosedArgs(serial));
        }

        private static void PersonalLightLevel(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            uint lightSerial = p.ReadUInt32BE();
            byte level = p.ReadUInt8();

            if (level > 0x1E)
            {
                level = 0x1E;
            }

            EventSink.RaiseLightLevelChanged(new LightLevelChangedArgs(lightSerial, level, true));
        }

        private static void LightLevel(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            byte level = p.ReadUInt8();

            if (level > 0x1E)
            {
                level = 0x1E;
            }

            EventSink.RaiseLightLevelChanged(new LightLevelChangedArgs(0, level, false));
        }

        private static void PlaySoundEffect(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            p.Skip(1);

            ushort index = p.ReadUInt16BE();
            ushort audio = p.ReadUInt16BE();
            ushort x = p.ReadUInt16BE();
            ushort y = p.ReadUInt16BE();
            short z = (short)p.ReadUInt16BE();

            EventSink.RaiseSoundPlay(new SoundPlayArgs(index, audio, x, y, z));
        }

        private static void PlayMusic(World world, ref StackDataReader p)
        {
            if (p.Length == 3) // Play Midi Music packet (0x6D, 0x10, index)
            {
                byte cmd = p.ReadUInt8();
                byte index = p.ReadUInt8();

                // Check for stop music packet (6D 1F FF)
                if (cmd == 0x1F && index == 0xFF)
                {
                    EventSink.RaiseMusicPlay(new MusicPlayArgs(0xFFFF));
                }
                else
                {
                    EventSink.RaiseMusicPlay(new MusicPlayArgs(index));
                }
            }
            else
            {
                ushort index = p.ReadUInt16BE();
                EventSink.RaiseMusicPlay(new MusicPlayArgs(index));
            }
        }

        private static void LoginComplete(World world, ref StackDataReader p)
        {
            EventSink.RaiseLoginCompleted(new LoginCompletedArgs());

            if (world.Player != null && Client.Game.Scene is LoginScene)
            {
                var scene = new GameScene(world);
                Client.Game.SetScene(scene);

                //GameActions.OpenPaperdoll(world.Player);
                GameActions.RequestMobileStatus(world, world.Player);
                NetClient.Socket.Send_OpenChat("");

                NetClient.Socket.Send_SkillsRequest(world.Player);
                scene.DoubleClickDelayed(world.Player);

                if (Client.Game.UO.Version >= Utility.ClientVersion.CV_306E)
                {
                    NetClient.Socket.Send_ClientType();
                }

                if (Client.Game.UO.Version >= Utility.ClientVersion.CV_305D)
                {
                    NetClient.Socket.Send_ClientViewRange(world.ClientViewRange);
                }

                List<Gump> gumps = ProfileManager.CurrentProfile.ReadGumps(
                    world,
                    ProfileManager.ProfilePath
                );

                if (gumps != null)
                {
                    foreach (Gump gump in gumps)
                    {
                        UIManager.Add(gump);
                    }
                }
            }
        }

        private static void MapData(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            byte action = p.ReadUInt8();

            ushort pinX = 0;
            ushort pinY = 0;
            byte plotState = 0;

            switch ((MapMessageType)action)
            {
                case MapMessageType.Add:
                    p.Skip(1);
                    pinX = p.ReadUInt16BE();
                    pinY = p.ReadUInt16BE();
                    break;

                case MapMessageType.EditResponse:
                    plotState = p.ReadUInt8();
                    break;
            }

            EventSink.RaiseMapDataReceived(new MapDataReceivedArgs(serial, action, pinX, pinY, plotState));
        }

        private static void SetTime(World world, ref StackDataReader p) { }

        private static void SetWeather(World world, ref StackDataReader p)
        {
            GameScene scene = Client.Game.GetScene<GameScene>();

            if (scene == null)
            {
                return;
            }

            WeatherType type = (WeatherType)p.ReadUInt8();
            byte count = p.ReadUInt8();
            byte temp = p.ReadUInt8();

            EventSink.RaiseWeatherChanged(new WeatherChangedArgs((byte)type, count, temp));
        }

        private static void BookData(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            ushort pageCnt = p.ReadUInt16BE();

            bool isNewBook = Client.Game.UO.Version > Utility.ClientVersion.CV_200;

            var pages = new List<BookPage>(pageCnt);

            for (int i = 0; i < pageCnt; i++)
            {
                int pageNum = p.ReadUInt16BE() - 1;
                ushort lineCnt = p.ReadUInt16BE();

                var lines = new string[lineCnt];

                for (int line = 0; line < lineCnt; line++)
                {
                    lines[line] = isNewBook ? p.ReadUTF8(true) : p.ReadASCII();
                }

                pages.Add(new BookPage(pageNum, lines));
            }

            EventSink.RaiseBookDataReceived(new BookDataReceivedArgs(serial, pageCnt, pages));
        }

        private static void CharacterAnimation(World world, ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();
            ushort action = p.ReadUInt16BE();
            ushort frame_count = p.ReadUInt16BE();
            ushort repeat_count = p.ReadUInt16BE();
            bool forward = !p.ReadBool();
            bool repeat = p.ReadBool();
            byte delay = p.ReadUInt8();

            EventSink.RaiseCharacterAnimation(new CharacterAnimationArgs(serial, action, frame_count, repeat_count, forward, repeat, delay));
        }

        private static void GraphicEffect(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            GraphicEffectType type = (GraphicEffectType)p.ReadUInt8();

            if (type > GraphicEffectType.FixedFrom)
            {
                if (type == GraphicEffectType.ScreenFade && p[0] == 0x70)
                {
                    p.Skip(8);
                    ushort val = p.ReadUInt16BE();

                    if (val > 4)
                    {
                        val = 4;
                    }

                    Log.Warn("Effect not implemented");
                }

                return;
            }

            uint source = p.ReadUInt32BE();
            uint target = p.ReadUInt32BE();
            ushort graphic = p.ReadUInt16BE();
            ushort srcX = p.ReadUInt16BE();
            ushort srcY = p.ReadUInt16BE();
            sbyte srcZ = p.ReadInt8();
            ushort targetX = p.ReadUInt16BE();
            ushort targetY = p.ReadUInt16BE();
            sbyte targetZ = p.ReadInt8();
            byte speed = p.ReadUInt8();
            byte duration = p.ReadUInt8();
            ushort unk = p.ReadUInt16BE();
            bool fixedDirection = p.ReadBool();
            bool doesExplode = p.ReadBool();
            uint hue = 0;
            GraphicEffectBlendMode blendmode = 0;

            if (p[0] == 0x70) { }
            else
            {
                hue = p.ReadUInt32BE();
                blendmode = (GraphicEffectBlendMode)(p.ReadUInt32BE() % 7);

                if (p[0] == 0xC7)
                {
                    var tileID = p.ReadUInt16BE();
                    var explodeEffect = p.ReadUInt16BE();
                    var explodeSound = p.ReadUInt16BE();
                    var serial = p.ReadUInt32BE();
                    var layer = p.ReadUInt8();
                    p.Skip(2);
                }
            }

            EventSink.RaiseGraphicEffectSpawned(new GraphicEffectSpawnedArgs(
                (byte)type,
                source,
                target,
                graphic,
                srcX,
                srcY,
                srcZ,
                targetX,
                targetY,
                targetZ,
                hue,
                speed,
                duration,
                fixedDirection,
                doesExplode,
                (byte)blendmode));
        }

        private static void ClientViewRange(World world, ref StackDataReader p)
        {
            byte range = p.ReadUInt8();
            EventSink.RaiseClientViewRangeChanged(new ClientViewRangeChangedArgs(range));
        }

        private static void BulletinBoardData(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            byte action = p.ReadUInt8();
            uint serial = p.ReadUInt32BE();

            switch (action)
            {
                case 0: // open
                    {
                        string name = p.ReadUTF8(22, true);
                        EventSink.RaiseBulletinBoardOpened(new BulletinBoardOpenedArgs(serial, name));
                    }
                    break;

                case 1: // summary
                    {
                        uint messageSerial = p.ReadUInt32BE();
                        uint parentSerial = p.ReadUInt32BE();

                        int len = p.ReadUInt8();
                        string poster = len > 0 ? p.ReadUTF8(len, true) : string.Empty;

                        len = p.ReadUInt8();
                        string subject = len > 0 ? p.ReadUTF8(len, true) : string.Empty;

                        len = p.ReadUInt8();
                        string dateTime = len > 0 ? p.ReadUTF8(len, true) : string.Empty;

                        EventSink.RaiseBulletinBoardSummary(new BulletinBoardSummaryArgs(
                            serial,
                            messageSerial,
                            parentSerial,
                            poster,
                            subject,
                            dateTime));
                    }
                    break;

                case 2: // message
                    {
                        uint messageSerial = p.ReadUInt32BE();

                        int len = p.ReadUInt8();
                        string poster = len > 0 ? p.ReadASCII(len) : string.Empty;

                        len = p.ReadUInt8();
                        string subject = len > 0 ? p.ReadUTF8(len, true) : string.Empty;

                        len = p.ReadUInt8();
                        string dateTime = len > 0 ? p.ReadASCII(len) : string.Empty;

                        p.Skip(4);

                        byte unk = p.ReadUInt8();
                        if (unk > 0)
                        {
                            p.Skip(unk * 4);
                        }

                        byte lineCount = p.ReadUInt8();

                        Span<char> span = stackalloc char[256];
                        ValueStringBuilder sb = new ValueStringBuilder(span);

                        for (int i = 0; i < lineCount; i++)
                        {
                            byte lineLen = p.ReadUInt8();

                            if (lineLen > 0)
                            {
                                string putta = p.ReadUTF8(lineLen, true);
                                sb.Append(putta);
                                sb.Append('\n');
                            }
                        }

                        string message = sb.ToString();
                        sb.Dispose();

                        EventSink.RaiseBulletinBoardMessage(new BulletinBoardMessageArgs(
                            serial,
                            messageSerial,
                            poster,
                            subject,
                            dateTime,
                            message));
                    }
                    break;
            }
        }

        private static void Warmode(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            bool inWar = p.ReadBool();
            EventSink.RaiseWarModeChanged(new WarModeChangedArgs(world.Player.Serial, inWar));
        }

        private static void Ping(World world, ref StackDataReader p)
        {
            byte seq = p.ReadUInt8();
            EventSink.RaisePingReceived(new PingReceivedArgs(seq));
            NetClient.Socket.Statistics.PingReceived(seq);
        }

        private static void BuyList(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            uint containerSerial = p.ReadUInt32BE();
            Item container = world.Items.Get(containerSerial);

            if (container == null)
            {
                return;
            }

            Mobile vendor = world.Mobiles.Get(container.Container);

            if (vendor == null)
            {
                return;
            }

            List<ShopBuyListEntry> entries = null;

            if (container.Layer == Layer.ShopBuyRestock || container.Layer == Layer.ShopBuy)
            {
                byte count = p.ReadUInt8();

                LinkedObject first = container.Items;

                if (first != null)
                {
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

                    entries = new List<ShopBuyListEntry>(count);

                    for (int i = 0; i < count; i++)
                    {
                        if (first == null)
                        {
                            break;
                        }

                        Item it = (Item)first;

                        uint price = p.ReadUInt32BE();
                        byte nameLen = p.ReadUInt8();
                        string name = p.ReadASCII(nameLen);

                        entries.Add(new ShopBuyListEntry(it.Serial, price, name));

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

            EventSink.RaiseShopBuyListReceived(new ShopBuyListReceivedArgs(
                vendor.Serial,
                (IReadOnlyList<ShopBuyListEntry>)entries ?? Array.Empty<ShopBuyListEntry>()));
        }

        private static void UpdateCharacter(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();

            if (world.Mobiles.Get(serial) == null)
            {
                return;
            }

            ushort graphic = p.ReadUInt16BE();
            ushort x = p.ReadUInt16BE();
            ushort y = p.ReadUInt16BE();
            sbyte z = p.ReadInt8();
            Direction direction = (Direction)p.ReadUInt8();
            ushort hue = p.ReadUInt16BE();
            Flags flags = (Flags)p.ReadUInt8();
            NotorietyFlag notoriety = (NotorietyFlag)p.ReadUInt8();

            EventSink.RaiseMobileUpdated(new MobileUpdatedArgs(serial, graphic, x, y, z, direction, hue, flags, notoriety));
        }

        private static void UpdateObject(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            ushort graphic = p.ReadUInt16BE();
            ushort x = p.ReadUInt16BE();
            ushort y = p.ReadUInt16BE();
            sbyte z = p.ReadInt8();
            Direction direction = (Direction)p.ReadUInt8();
            ushort hue = p.ReadUInt16BE();
            Flags flags = (Flags)p.ReadUInt8();
            NotorietyFlag notoriety = (NotorietyFlag)p.ReadUInt8();

            if (p[0] != 0x78)
            {
                p.Skip(6);
            }

            var equipment = new List<MobileUpdatedEquipmentEntry>();
            uint itemSerial = p.ReadUInt32BE();

            while (itemSerial != 0 && p.Position < p.Length)
            {
                ushort itemGraphic = p.ReadUInt16BE();
                byte layer = p.ReadUInt8();
                ushort item_hue = 0;

                if (Client.Game.UO.Version >= Utility.ClientVersion.CV_70331)
                {
                    item_hue = p.ReadUInt16BE();
                }
                else if ((itemGraphic & 0x8000) != 0)
                {
                    itemGraphic &= 0x7FFF;
                    item_hue = p.ReadUInt16BE();
                }

                equipment.Add(new MobileUpdatedEquipmentEntry(itemSerial, itemGraphic, (Layer)layer, item_hue));

                itemSerial = p.ReadUInt32BE();
            }

            EventSink.RaiseMobileUpdated(new MobileUpdatedArgs(serial, graphic, x, y, z, direction, hue, flags, notoriety, true, equipment));
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

            EventSink.RaiseContextMenuOpened(new ContextMenuOpenedArgs(serial, id, name));

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

        private static void OpenPaperdoll(World world, ref StackDataReader p)
        {
            uint mobSerial = p.ReadUInt32BE();
            string text = p.ReadASCII(60);
            byte flags = p.ReadUInt8();

            EventSink.RaisePaperdollOpened(new PaperdollOpenedArgs(mobSerial, text, flags));
        }

        private static void CorpseEquipment(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            Entity corpse = world.Get(serial);

            if (corpse == null)
            {
                return;
            }

            // if it's not a corpse we should skip this [?]
            if (corpse.Graphic != 0x2006)
            {
                return;
            }

            var entries = new List<CorpseEquipmentEntry>();
            Layer layer = (Layer)p.ReadUInt8();

            while (layer != Layer.Invalid && p.Position < p.Length)
            {
                uint item_serial = p.ReadUInt32BE();
                entries.Add(new CorpseEquipmentEntry(layer, item_serial));
                layer = (Layer)p.ReadUInt8();
            }

            EventSink.RaiseCorpseEquipmentReceived(new CorpseEquipmentReceivedArgs(serial, entries));
        }

        private static void DisplayMap(World world, ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();
            ushort gumpid = p.ReadUInt16BE();
            ushort startX = p.ReadUInt16BE();
            ushort startY = p.ReadUInt16BE();
            ushort endX = p.ReadUInt16BE();
            ushort endY = p.ReadUInt16BE();
            ushort width = p.ReadUInt16BE();
            ushort height = p.ReadUInt16BE();

            ushort? facet = null;

            if (p[0] == 0xF5)
            {
                facet = p.ReadUInt16BE();
            }
            else if (Client.Game.UO.Version >= Utility.ClientVersion.CV_308Z)
            {
                facet = 0;
            }

            EventSink.RaiseMapDisplayed(new MapDisplayedArgs(serial, gumpid, startX, startY, endX, endY, width, height, facet));
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

            ushort pageCount = p.ReadUInt16BE();

            string title = oldpacket
                ? p.ReadUTF8(60, true)
                : p.ReadUTF8(p.ReadUInt16BE(), true);
            string author = oldpacket
                ? p.ReadUTF8(30, true)
                : p.ReadUTF8(p.ReadUInt16BE(), true);

            EventSink.RaiseBookOpened(new BookOpenedArgs(serial, editable, pageCount, oldpacket, title, author));
        }

        private static void DyeData(World world, ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();
            p.Skip(2);
            ushort graphic = p.ReadUInt16BE();

            EventSink.RaiseDyeDataReceived(new DyeDataReceivedArgs(serial, graphic));
        }

        private static void MovePlayer(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            Direction direction = (Direction)p.ReadUInt8();
            bool running = (direction & Direction.Running) != 0;
            EventSink.RaisePlayerMoved(new PlayerMovedArgs(direction & Direction.Mask, running));
        }

        private static void UpdateName(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            string name = p.ReadASCII();

            EventSink.RaiseMobileNameChanged(new MobileNameChangedArgs(serial, name));
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

            EventSink.RaiseMultiPlacementReceived(new MultiPlacementReceivedArgs((byte)(allowGround ? 1 : 0), targID, flags, multiID, xOff, yOff, zOff, hue));
        }

        private static void ASCIIPrompt(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            EventSink.RaiseAsciiPrompt(new AsciiPromptArgs(p.ReadUInt64BE()));
        }

        private static void SellList(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            uint vendorSerial = p.ReadUInt32BE();
            Mobile vendor = world.Mobiles.Get(vendorSerial);

            if (vendor == null)
            {
                return;
            }

            ushort countItems = p.ReadUInt16BE();

            if (countItems <= 0)
            {
                return;
            }

            var entries = new List<ShopSellListEntry>(countItems);

            for (int i = 0; i < countItems; i++)
            {
                uint serial = p.ReadUInt32BE();
                ushort graphic = p.ReadUInt16BE();
                ushort hue = p.ReadUInt16BE();
                ushort amount = p.ReadUInt16BE();
                ushort price = p.ReadUInt16BE();
                string name = p.ReadASCII(p.ReadUInt16BE());

                entries.Add(new ShopSellListEntry(serial, graphic, hue, amount, price, name));
            }

            EventSink.RaiseShopSellListReceived(new ShopSellListReceivedArgs(vendorSerial, entries));
        }

        private static void UpdateHitpoints(World world, ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();
            ushort hitsMax = p.ReadUInt16BE();
            ushort hits = p.ReadUInt16BE();

            EventSink.RaiseHitpointsUpdated(new HitpointsUpdatedArgs(serial, hitsMax, hits));
        }

        private static void UpdateMana(World world, ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();
            ushort manaMax = p.ReadUInt16BE();
            ushort mana = p.ReadUInt16BE();

            EventSink.RaiseManaUpdated(new ManaUpdatedArgs(serial, manaMax, mana));
        }

        private static void UpdateStamina(World world, ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();
            ushort stamMax = p.ReadUInt16BE();
            ushort stam = p.ReadUInt16BE();

            EventSink.RaiseStaminaUpdated(new StaminaUpdatedArgs(serial, stamMax, stam));
        }

        private static void OpenUrl(World world, ref StackDataReader p)
        {
            string url = p.ReadASCII();

            EventSink.RaiseOpenUrlRequested(new OpenUrlRequestedArgs(url));
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

            EventSink.RaiseTipWindowDisplayed(new TipWindowDisplayedArgs(tip, flag, str));
        }

        private static void AttackCharacter(World world, ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();

            EventSink.RaiseAttackTargetChanged(new AttackTargetChangedArgs(serial));
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

            EventSink.RaiseTextEntryDialogOpened(new TextEntryDialogArgs(serial, parentID, buttonID, maxLength, text, desc));
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

            EventSink.RaiseUnicodeChatMessage(new UnicodeChatMessageArgs(serial, graphic, type, hue, (byte)font, lang, name, text));
        }

        private static void DisplayDeath(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            uint corpseSerial = p.ReadUInt32BE();
            uint running = p.ReadUInt32BE();

            EventSink.RaiseMobileDeath(new MobileDeathArgs(serial, corpseSerial, running != 0));
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

            EventSink.RaiseGumpOpened(new GumpOpenedArgs(sender, gumpID, x, y));

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

        private static void ChatMessage(World world, ref StackDataReader p)
        {
            ushort cmd = p.ReadUInt16BE();

            switch (cmd)
            {
                case 0x03E8: // create conference
                {
                    p.Skip(4);
                    string channelName = p.ReadUnicodeBE();
                    bool hasPassword = p.ReadUInt16BE() == 0x31;
                    EventSink.RaiseChatConferenceCreated(new ChatConferenceCreatedArgs(channelName, hasPassword));
                    break;
                }

                case 0x03E9: // destroy conference
                {
                    p.Skip(4);
                    string channelName = p.ReadUnicodeBE();
                    EventSink.RaiseChatConferenceDestroyed(new ChatConferenceDestroyedArgs(channelName));
                    break;
                }

                case 0x03EB: // display enter username window
                    EventSink.RaiseChatUsernameRequest(new ChatUsernameRequestArgs());
                    break;

                case 0x03EC: // close chat
                    EventSink.RaiseChatClosed(new ChatClosedArgs());
                    break;

                case 0x03ED: // username accepted, display chat
                {
                    p.Skip(4);
                    string username = p.ReadUnicodeBE();
                    EventSink.RaiseChatUsernameAccepted(new ChatUsernameAcceptedArgs(username));
                    break;
                }

                case 0x03EE: // add user
                {
                    p.Skip(4);
                    ushort userType = p.ReadUInt16BE();
                    string username = p.ReadUnicodeBE();
                    EventSink.RaiseChatUserAdded(new ChatUserAddedArgs(userType, username));
                    break;
                }

                case 0x03EF: // remove user
                {
                    p.Skip(4);
                    string username = p.ReadUnicodeBE();
                    EventSink.RaiseChatUserRemoved(new ChatUserRemovedArgs(username));
                    break;
                }

                case 0x03F0: // clear all players
                    EventSink.RaiseChatClearAllPlayers(new ChatClearAllPlayersArgs());
                    break;

                case 0x03F1: // you have joined a conference
                {
                    p.Skip(4);
                    string channelName = p.ReadUnicodeBE();
                    EventSink.RaiseChatConferenceJoined(new ChatConferenceJoinedArgs(channelName));
                    break;
                }

                case 0x03F4: // you have left a conference
                {
                    p.Skip(4);
                    string channelName = p.ReadUnicodeBE();
                    EventSink.RaiseChatConferenceLeft(new ChatConferenceLeftArgs(channelName));
                    break;
                }

                case 0x0025:
                case 0x0026:
                case 0x0027:
                {
                    p.Skip(4);
                    p.ReadUInt16BE(); // msgType (unused)
                    string username = p.ReadUnicodeBE();
                    string message = p.ReadUnicodeBE();
                    EventSink.RaiseChatTextReceived(new ChatTextReceivedArgs(cmd, username, message));
                    break;
                }

                default:
                    if (cmd >= 0x0001 && cmd <= 0x0024 || cmd >= 0x0028 && cmd <= 0x002C)
                    {
                        p.Skip(4);
                        string text = p.ReadUnicodeBE();
                        EventSink.RaiseChatSystemMessage(new ChatSystemMessageArgs(cmd, text));
                    }
                    break;
            }
        }

        private static void Help(World world, ref StackDataReader p) { }

        private static void CharacterProfile(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            string header = p.ReadASCII();
            string footer = p.ReadUnicodeBE();

            string body = p.ReadUnicodeBE();

            EventSink.RaiseCharacterProfileOpened(new CharacterProfileOpenedArgs(serial, header, footer, body));
        }

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

            EventSink.RaiseLockedFeaturesEnabled(new LockedFeaturesEnabledArgs((uint)flags));

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

            EventSink.RaiseQuestArrowDisplayed(new QuestArrowDisplayedArgs(display, mx, my, serial));

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

        private static void UltimaMessengerR(World world, ref StackDataReader p) { }

        private static void Season(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            byte season = p.ReadUInt8();
            byte music = p.ReadUInt8();

            EventSink.RaiseSeasonChanged(new SeasonChangedArgs(season, music));
        }

        private static void ClientVersion(World world, ref StackDataReader p)
        {
            EventSink.RaiseClientVersionRequested(new ClientVersionRequestedArgs());
            ClientVersionImpl(world, ref p);
        }

        private static void ClientVersionImpl(World world, ref StackDataReader p)
        {
            NetClient.Socket.Send_ClientVersion(Settings.GlobalSettings.ClientVersion);
        }

        private static void AssistVersion(World world, ref StackDataReader p)
        {
            //uint version = p.ReadUInt32BE();

            //string[] parts = Service.GetByLocalSerial<Settings>().ClientVersion.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            //byte[] clientVersionBuffer =
            //    {byte.Parse(parts[0]), byte.Parse(parts[1]), byte.Parse(parts[2]), byte.Parse(parts[3])};

            //NetClient.Socket.Send(new PAssistVersion(clientVersionBuffer, version));
        }

        private static void ExtendedCommand(World world, ref StackDataReader p)
        {
            ushort cmd = p.ReadUInt16BE();

            switch (cmd)
            {
                case 0:
                    break;

                case 1: // fast walk prevention
                {
                    uint[] values = new uint[6];
                    for (int i = 0; i < 6; i++)
                    {
                        values[i] = p.ReadUInt32BE();
                    }

                    EventSink.RaiseFastWalkStackInit(new FastWalkStackInitArgs(values));
                    break;
                }

                case 2: // add key to fast walk stack
                {
                    EventSink.RaiseFastWalkStackAdd(new FastWalkStackAddArgs(p.ReadUInt32BE()));
                    break;
                }

                case 4: // close generic gump
                {
                    uint ser = p.ReadUInt32BE();
                    int button = (int)p.ReadUInt32BE();
                    EventSink.RaiseGenericGumpClose(new GenericGumpCloseArgs(ser, button));
                    break;
                }

                case 6: // party
                {
                    // Party packet has its own multi-format inner parser owned by PartyManager.
                    // Snapshot remaining bytes so the subscriber can re-wrap them.
                    int remaining = p.Remaining;
                    byte[] bytes = new byte[remaining];
                    if (remaining > 0)
                    {
                        p.Buffer.Slice(p.Position, remaining).CopyTo(bytes);
                    }
                    p.Skip(remaining);

                    EventSink.RaisePartyPacket(new PartyPacketArgs(bytes));
                    break;
                }

                case 8: // map change
                {
                    EventSink.RaiseMapIndexChanged(new MapIndexChangedArgs(p.ReadUInt8()));
                    break;
                }

                case 0x0C: // close statusbar gump
                {
                    EventSink.RaiseCloseStatusbarGump(new CloseStatusbarGumpArgs(p.ReadUInt32BE()));
                    break;
                }

                case 0x10: // display equip info
                {
                    uint itemSerial = p.ReadUInt32BE();
                    uint nameCliloc = p.ReadUInt32BE();

                    string crafterName = string.Empty;
                    bool unidentified = false;
                    ushort crafterNameLen = 0;
                    uint next = p.ReadUInt32BE();

                    if (next == 0xFFFFFFFD)
                    {
                        crafterNameLen = p.ReadUInt16BE();
                        if (crafterNameLen > 0)
                        {
                            crafterName = p.ReadASCII(crafterNameLen);
                        }
                    }

                    if (crafterNameLen != 0)
                    {
                        next = p.ReadUInt32BE();
                    }

                    if (next == 0xFFFFFFFC)
                    {
                        unidentified = true;
                    }

                    var lines = new List<EquipInfoLine>();
                    byte count = 0;

                    while (p.Position < p.Length - 4)
                    {
                        if (count != 0 || next == 0xFFFFFFFD || next == 0xFFFFFFFC)
                        {
                            next = p.ReadUInt32BE();
                        }

                        short charges = (short)p.ReadUInt16BE();
                        lines.Add(new EquipInfoLine((int)next, charges));

                        // Original counter logic — preserved verbatim so the subscriber can
                        // reproduce the original closing-bracket heuristic.
                        if (charges != -1)
                        {
                            count += 20;
                        }
                        count++;
                    }

                    EventSink.RaiseEquipInfoReceived(
                        new EquipInfoArgs(itemSerial, nameCliloc, crafterName, unidentified, lines)
                    );
                    break;
                }

                case 0x11:
                    break;

                case 0x14: // display popup/context menu
                {
                    PopupMenuData data = PopupMenuData.Parse(ref p);
                    EventSink.RaisePopupMenuReceived(new PopupMenuArgs(data));
                    break;
                }

                case 0x16: // close user interface windows
                {
                    uint id = p.ReadUInt32BE();
                    uint ifSerial = p.ReadUInt32BE();
                    EventSink.RaiseCloseUserInterface(new CloseUserInterfaceArgs(id, ifSerial));
                    break;
                }

                case 0x18: // enable map patches
                {
                    // The map loader consumes the rest of the packet with its own
                    // StackDataReader. Snapshot remaining bytes for the subscriber to re-wrap.
                    int remaining = p.Remaining;
                    byte[] bytes = new byte[remaining];
                    if (remaining > 0)
                    {
                        p.Buffer.Slice(p.Position, remaining).CopyTo(bytes);
                    }
                    p.Skip(remaining);

                    EventSink.RaiseMapPatchesEnabled(new MapPatchesEnabledArgs(bytes));
                    break;
                }

                case 0x19: // extended stats
                {
                    byte version = p.ReadUInt8();
                    uint statsSerial = p.ReadUInt32BE();

                    switch (version)
                    {
                        case 0:
                        {
                            bool dead = p.ReadBool();
                            EventSink.RaiseExtendedStatsBonded(new ExtendedStatsBondedArgs(statsSerial, dead));
                            break;
                        }

                        case 2:
                        {
                            // Original packet has an "updategump" byte followed by a packed state byte.
                            p.Skip(1); // updategump (ignored in original implementation)
                            byte state = p.ReadUInt8();
                            byte strLock = (byte)((state >> 4) & 3);
                            byte dexLock = (byte)((state >> 2) & 3);
                            byte intLock = (byte)(state & 3);
                            EventSink.RaiseExtendedStatsLocks(
                                new ExtendedStatsLocksArgs(statsSerial, strLock, dexLock, intLock)
                            );
                            break;
                        }

                        case 5:
                        {
                            int pos = p.Position;
                            p.Skip(1); // zero
                            byte type2 = p.ReadUInt8();

                            if (type2 == 0xFF)
                            {
                                byte status = p.ReadUInt8();
                                ushort animation = p.ReadUInt16BE();
                                ushort frame = p.ReadUInt16BE();

                                if (status == 0 && animation == 0 && frame == 0)
                                {
                                    // No-op sentinel, matches original `goto case 0` behavior.
                                    break;
                                }

                                EventSink.RaiseExtendedStatsAnimation(
                                    new ExtendedStatsAnimationArgs(statsSerial, animation, frame)
                                );
                            }
                            else
                            {
                                // Original code re-read this as a version-2 stat-locks packet
                                // but only if the serial matched the player. Replay the same
                                // parse so the subscriber receives the same args.
                                p.Seek(pos);
                                p.Skip(1); // updategump
                                byte state = p.ReadUInt8();
                                byte strLock = (byte)((state >> 4) & 3);
                                byte dexLock = (byte)((state >> 2) & 3);
                                byte intLock = (byte)(state & 3);
                                EventSink.RaiseExtendedStatsLocks(
                                    new ExtendedStatsLocksArgs(statsSerial, strLock, dexLock, intLock)
                                );
                            }
                            break;
                        }
                    }

                    break;
                }

                case 0x1B: // new spellbook content
                {
                    p.Skip(2);
                    uint spellbookSerial = p.ReadUInt32BE();
                    ushort spellbookGraphic = p.ReadUInt16BE();
                    ushort spellbookType = p.ReadUInt16BE();

                    var spellIds = new List<int>();
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
                                spellIds.Add(j * 32 + i + 1);
                            }
                        }
                    }

                    EventSink.RaiseSpellbookContent(
                        new SpellbookContentArgs(spellbookSerial, spellbookGraphic, spellbookType, spellIds)
                    );
                    break;
                }

                case 0x1D: // house revision state
                {
                    uint houseSerial = p.ReadUInt32BE();
                    uint revision = p.ReadUInt32BE();
                    EventSink.RaiseHouseRevisionState(new HouseRevisionStateArgs(houseSerial, revision));
                    break;
                }

                case 0x20: // house customization tool state
                {
                    uint hcSerial = p.ReadUInt32BE();
                    byte hcType = p.ReadUInt8();
                    ushort hcGraphic = p.ReadUInt16BE();
                    ushort hcX = p.ReadUInt16BE();
                    ushort hcY = p.ReadUInt16BE();
                    sbyte hcZ = p.ReadInt8();
                    EventSink.RaiseHouseDesignState(
                        new HouseDesignStateArgs(hcSerial, hcType, hcGraphic, hcX, hcY, hcZ)
                    );
                    break;
                }

                case 0x21:
                    EventSink.RaiseAbilityIconsReset(new AbilityIconsResetArgs());
                    break;

                case 0x22:
                {
                    p.Skip(1);
                    uint dmgSerial = p.ReadUInt32BE();
                    byte damage = p.ReadUInt8();
                    EventSink.RaiseDamageOverhead(new DamageOverheadArgs(dmgSerial, damage));
                    break;
                }

                case 0x25:
                {
                    ushort spell = p.ReadUInt16BE();
                    bool active = p.ReadBool();
                    EventSink.RaiseSpellIconToggle(new SpellIconToggleArgs(spell, active));
                    break;
                }

                case 0x26:
                {
                    byte val = p.ReadUInt8();
                    EventSink.RaiseCharacterSpeedMode(new CharacterSpeedModeArgs(val));
                    break;
                }

                case 0x2A:
                {
                    bool isfemale = p.ReadBool();
                    byte race = p.ReadUInt8();
                    EventSink.RaiseRaceChangeRequested(new RaceChangeRequestedArgs(isfemale, race));
                    break;
                }

                case 0x2B:
                {
                    ushort partial = p.ReadUInt16BE();
                    byte animID = p.ReadUInt8();
                    byte frameCount = p.ReadUInt8();
                    EventSink.RaiseMobileAnimationFrame(
                        new MobileAnimationFrameArgs(partial, animID, frameCount)
                    );
                    break;
                }

                case 0xBEEF: // ClassicUO commands
                {
                    ushort cuoType = p.ReadUInt16BE();
                    EventSink.RaiseCuoCommand(new CuoCommandArgs(cuoType));
                    break;
                }

                default:
                    Log.Warn($"Unhandled 0xBF - sub: {cmd.ToHex()}");
                    break;
            }
        }

        private static void DisplayClilocString(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
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

            EventSink.RaiseClilocMessage(new ClilocMessageArgs(serial, graphic, type, hue, (byte)font, cliloc, name, arguments, affix, (byte)flags));
        }

        private static void UnicodePrompt(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            EventSink.RaiseUnicodePrompt(new UnicodePromptArgs(p.ReadUInt64BE()));
        }

        private static void Semivisible(World world, ref StackDataReader p) { }

        private static void InvalidMapEnable(World world, ref StackDataReader p) { }

        private static void ParticleEffect3D(World world, ref StackDataReader p) { }

        private static void GetUserServerPingGodClientR(World world, ref StackDataReader p) { }

        private static void GlobalQueCount(World world, ref StackDataReader p) { }

        private static void ConfigurationFileR(World world, ref StackDataReader p) { }

        private static void Logout(World world, ref StackDataReader p)
        {
            // http://docs.polserver.com/packets/index.php?Packet=0xD1

            if (
                Client.Game.GetScene<GameScene>().DisconnectionRequested
                && (
                    world.ClientFeatures.Flags
                    & CharacterListFlags.CLF_OWERWRITE_CONFIGURATION_BUTTON
                ) != 0
            )
            {
                bool canDisconnect = p.ReadBool();

                EventSink.RaiseLogoutReceived(new LogoutReceivedArgs(canDisconnect));

                if (canDisconnect)
                {
                    // client can disconnect
                    NetClient.Socket.Disconnect();
                    Client.Game.SetScene(new LoginScene(world));
                }
                else
                {
                    Log.Warn("0x1D - client asked to disconnect but server answered 'NO!'");
                }
            }
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

            string name = string.Empty;
            string data = string.Empty;
            int namecliloc = 0;

            if (list.Count != 0)
            {
                Span<char> span = stackalloc char[totalLength];
                ValueStringBuilder sb = new ValueStringBuilder(span);

                bool first = true;

                foreach (var s in list)
                {
                    string str = s.Item2;

                    if (first)
                    {
                        name = str;

                        if (!SerialHelper.IsMobile(serial))
                        {
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

            EventSink.RaiseMegaClilocReceived(new MegaClilocReceivedArgs(serial, revision, name, data, namecliloc));
        }

        private static void GenericAOSCommandsR(World world, ref StackDataReader p) { }

        private static void CustomHouse(World world, ref StackDataReader p)
        {
            bool compressed = p.ReadUInt8() == 0x03;
            bool enableReponse = p.ReadBool();
            uint serial = p.ReadUInt32BE();
            uint revision = p.ReadUInt32BE();

            // The original 0xD8 stream contains 4 bytes here (size/flags) that
            // the existing logic skipped before the plane count.
            p.Skip(4);

            // Look up the foundation so plane mode 2 can compute its tile
            // coordinates which depend on the multi bounds.
            Item foundation = world.Items.Get(serial);
            Rectangle? multi = foundation?.MultiInfo;

            List<CustomHouseComponent> components;
            byte planes = p.ReadUInt8();

            if (foundation == null || !foundation.IsMulti || multi == null)
            {
                // Skip the remaining plane payload so the stream is fully
                // consumed; we still raise the event with an empty list so
                // the subscriber can no-op cleanly.
                for (int plane = 0; plane < planes; plane++)
                {
                    uint header = p.ReadUInt32BE();
                    int clen = (int)(((header & 0xFF00) >> 8) | ((header & 0x0F) << 8));

                    if (clen > 0)
                    {
                        p.Skip(clen);
                    }
                }

                components = new List<CustomHouseComponent>(0);
            }
            else
            {
                short minX = (short)multi.Value.X;
                short minY = (short)multi.Value.Y;
                short maxY = (short)multi.Value.Height;

                if (minX == 0 && minY == 0 && maxY == 0 && multi.Value.Width == 0)
                {
                    // Drain remaining payload to keep the reader consistent.
                    for (int plane = 0; plane < planes; plane++)
                    {
                        uint header = p.ReadUInt32BE();
                        int clen = (int)(((header & 0xFF00) >> 8) | ((header & 0x0F) << 8));

                        if (clen > 0)
                        {
                            p.Skip(clen);
                        }
                    }

                    components = new List<CustomHouseComponent>(0);
                }
                else
                {
                    components = new List<CustomHouseComponent>();

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
                            components
                        );

                        p.Skip(clen);
                    }
                }
            }

            EventSink.RaiseCustomHouseReceived(new CustomHouseReceivedArgs(serial, revision, components));
        }

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
            List<CustomHouseComponent> components
        )
        {
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
                                components.Add(new CustomHouseComponent(id, x, y, z));
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
                                components.Add(new CustomHouseComponent(id, x, y, z));
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
                                components.Add(new CustomHouseComponent(id, x, y, z));
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

        private static void CharacterTransferLog(World world, ref StackDataReader p) { }

        private static void OPLInfo(World world, ref StackDataReader p)
        {
            if (world.ClientFeatures.TooltipsEnabled)
            {
                uint serial = p.ReadUInt32BE();
                uint revision = p.ReadUInt32BE();

                EventSink.RaiseOplInfoReceived(new OplInfoReceivedArgs(serial, revision));
            }
        }

        private static void OpenCompressedGump(World world, ref StackDataReader p)
        {
            uint sender = p.ReadUInt32BE();
            uint gumpID = p.ReadUInt32BE();
            uint x = p.ReadUInt32BE();
            uint y = p.ReadUInt32BE();

            EventSink.RaiseCompressedGumpOpened(new CompressedGumpOpenedArgs(sender, gumpID, (int)x, (int)y));
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

        private static void UpdateMobileStatus(World world, ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();
            byte status = p.ReadUInt8();

            EventSink.RaiseMobileStatusUpdated(new MobileStatusUpdatedArgs(serial, status));

            if (status == 1)
            {
                uint attackerSerial = p.ReadUInt32BE();
            }
        }

        private static void BuffDebuff(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            const ushort BUFF_ICON_START = 0x03E9;
            const ushort BUFF_ICON_START_NEW = 0x466;

            uint serial = p.ReadUInt32BE();
            BuffIconType ic = (BuffIconType)p.ReadUInt16BE();

            ushort iconID =
                (ushort)ic >= BUFF_ICON_START_NEW
                    ? (ushort)(ic - (BUFF_ICON_START_NEW - 125))
                    : (ushort)((ushort)ic - BUFF_ICON_START);

            if (iconID < BuffTable.Table.Length)
            {
                ushort count = p.ReadUInt16BE();

                if (count == 0)
                {
                    EventSink.RaiseBuffRemoved(new BuffRemovedArgs(serial, (ushort)ic));
                }
                else
                {
                    for (int i = 0; i < count; i++)
                    {
                        ushort source_type = p.ReadUInt16BE();
                        p.Skip(2);
                        ushort icon = p.ReadUInt16BE();
                        ushort queue_index = p.ReadUInt16BE();
                        p.Skip(4);
                        ushort timer = p.ReadUInt16BE();
                        p.Skip(3);

                        uint titleCliloc = p.ReadUInt32BE();
                        uint descriptionCliloc = p.ReadUInt32BE();
                        uint wtfCliloc = p.ReadUInt32BE();

                        ushort arg_length = p.ReadUInt16BE();
                        var str = p.ReadUnicodeLE(2);
                        var args = str + p.ReadUnicodeLE();
                        string title = Client.Game.UO.FileManager.Clilocs.Translate(
                            (int)titleCliloc,
                            args,
                            true
                        );

                        arg_length = p.ReadUInt16BE();
                        string args_2 = p.ReadUnicodeLE();
                        string description = string.Empty;

                        if (descriptionCliloc != 0)
                        {
                            description =
                                "\n"
                                + Client.Game.UO.FileManager.Clilocs.Translate(
                                    (int)descriptionCliloc,
                                    String.IsNullOrEmpty(args_2) ? args : args_2,
                                    true
                                );

                            if (description.Length < 2)
                            {
                                description = string.Empty;
                            }
                        }

                        arg_length = p.ReadUInt16BE();
                        string args_3 = p.ReadUnicodeLE();
                        string wtf = string.Empty;

                        if (wtfCliloc != 0)
                        {
                            wtf = Client.Game.UO.FileManager.Clilocs.Translate(
                                (int)wtfCliloc,
                                String.IsNullOrEmpty(args_3) ? args : args_3,
                                true
                            );

                            if (!string.IsNullOrWhiteSpace(wtf))
                            {
                                wtf = $"\n{wtf}";
                            }
                        }

                        string text = $"<left>{title}{description}{wtf}</left>";
                        EventSink.RaiseBuffApplied(new BuffAppliedArgs(serial, (ushort)ic, iconID, timer, text));
                    }
                }
            }
        }

        private static void NewCharacterAnimation(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            ushort type = p.ReadUInt16BE();
            ushort action = p.ReadUInt16BE();
            byte mode = p.ReadUInt8();

            EventSink.RaiseNewCharacterAnimation(new NewCharacterAnimationArgs(serial, type, action, mode));
        }

        private static void KREncryptionResponse(World world, ref StackDataReader p) { }

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

            EventSink.RaiseWaypointDisplayed(new WaypointDisplayedArgs(serial, x, y, z, map, (ushort)type, ignoreobject, cliloc));
        }

        private static void RemoveWaypoint(World world, ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();
            EventSink.RaiseWaypointRemoved(new WaypointRemovedArgs(serial));
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

        private static void UpdateItemSA(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            p.Skip(2);
            byte type = p.ReadUInt8();
            uint serial = p.ReadUInt32BE();
            ushort graphic = p.ReadUInt16BE();
            byte graphicInc = p.ReadUInt8();
            ushort amount = p.ReadUInt16BE();
            ushort unk = p.ReadUInt16BE();
            ushort x = p.ReadUInt16BE();
            ushort y = p.ReadUInt16BE();
            sbyte z = p.ReadInt8();
            Direction dir = (Direction)p.ReadUInt8();
            ushort hue = p.ReadUInt16BE();
            Flags flags = (Flags)p.ReadUInt8();
            ushort unk2 = p.ReadUInt16BE();

            bool isFromPacketList = p[0] == 0xF7;

            EventSink.RaiseItemUpdated(new ItemUpdatedArgs(serial, graphic, graphicInc, amount, x, y, z, dir, hue, flags, type, unk, unk2, true, isFromPacketList));
        }

        private static void BoatMoving(World world, ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();
            byte boatSpeed = p.ReadUInt8();
            Direction movingDirection = (Direction)p.ReadUInt8() & Direction.Mask;
            Direction facingDirection = (Direction)p.ReadUInt8() & Direction.Mask;
            ushort x = p.ReadUInt16BE();
            ushort y = p.ReadUInt16BE();
            ushort z = p.ReadUInt16BE();

            int count = p.ReadUInt16BE();
            var passengers = new List<BoatPassenger>(count);
            for (int i = 0; i < count; i++)
            {
                uint cSerial = p.ReadUInt32BE();
                ushort cx = p.ReadUInt16BE();
                ushort cy = p.ReadUInt16BE();
                ushort cz = p.ReadUInt16BE();
                passengers.Add(new BoatPassenger(cSerial, cx, cy, cz));
            }

            EventSink.RaiseBoatMovingReceived(new BoatMovingReceivedArgs(serial, boatSpeed, (byte)movingDirection, (byte)facingDirection, x, y, z, passengers));
        }

        private static void PacketList(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            int count = p.ReadUInt16BE();

            for (int i = 0; i < count; i++)
            {
                byte id = p.ReadUInt8();

                if (id == 0xF3)
                {
                    UpdateItemSA(world, ref p);
                }
                else
                {
                    Log.Warn($"Unknown packet ID: [0x{id:X2}] in 0xF7");

                    break;
                }
            }
        }

        private static void ServerListReceived(World world, ref StackDataReader p)
        {
            if (world.InGame)
            {
                return;
            }

            byte flags = p.ReadUInt8();
            ushort count = p.ReadUInt16BE();
            ServerListEntry[] servers = new ServerListEntry[count];

            for (ushort i = 0; i < count; i++)
            {
                servers[i] = ServerListEntry.Create(ref p);
            }

            EventSink.RaiseServerListReceived(new ServerListReceivedArgs(flags, servers));
        }

        private static void ReceiveServerRelay(World world, ref StackDataReader p)
        {
            if (world.InGame)
            {
                return;
            }

            uint ip = p.ReadUInt32LE(); // use LittleEndian here
            ushort port = p.ReadUInt16BE();
            uint seed = p.ReadUInt32BE();

            EventSink.RaiseServerRelayReceived(new ServerRelayReceivedArgs(ip, port, seed));
        }

        private static void UpdateCharacterList(World world, ref StackDataReader p)
        {
            if (world.InGame)
            {
                return;
            }

            string[] characters = ParseCharacterList(ref p);

            EventSink.RaiseCharacterListUpdated(new CharacterListUpdatedArgs(characters));
        }

        private static void ReceiveCharacterList(World world, ref StackDataReader p)
        {
            if (world.InGame)
            {
                return;
            }

            string[] characters = ParseCharacterList(ref p);
            CityInfo[] cities = ParseCities(ref p);
            uint clientFlags = p.ReadUInt32BE();

            EventSink.RaiseCharacterListReceived(new CharacterListReceivedArgs(characters, cities, clientFlags));
        }

        private static void LoginDelay(World world, ref StackDataReader p)
        {
            if (world.InGame)
            {
                return;
            }

            byte delay = p.ReadUInt8();

            EventSink.RaiseLoginDelayReceived(new LoginDelayReceivedArgs(delay));
        }

        private static void ReceiveLoginRejection(World world, ref StackDataReader p)
        {
            if (world.InGame)
            {
                return;
            }

            byte packetId = p[0];
            byte rejectReason = packetId == 0x82 || packetId == 0x85 || packetId == 0x53
                ? p.Buffer[1]
                : (byte)0;

            EventSink.RaiseLoginRejected(new LoginRejectedArgs(packetId, rejectReason));
        }

        private static string[] ParseCharacterList(ref StackDataReader p)
        {
            int count = p.ReadUInt8();
            string[] characters = new string[count];

            for (ushort i = 0; i < count; i++)
            {
                characters[i] = p.ReadASCII(30).TrimEnd('\0');

                p.Skip(30);
            }

            return characters;
        }

        private static CityInfo[] ParseCities(ref StackDataReader p)
        {
            byte count = p.ReadUInt8();
            CityInfo[] cities = new CityInfo[count];

            bool isNew = Client.Game.UO.Version >= Utility.ClientVersion.CV_70130;
            string[] descriptions = null;

            if (!isNew)
            {
                descriptions = ReadCityTextFile(count);
            }

            Point[] oldtowns =
            {
                new Point(105, 130), new Point(245, 90),
                new Point(165, 200), new Point(395, 160),
                new Point(200, 305), new Point(335, 250),
                new Point(160, 395), new Point(100, 250),
                new Point(270, 130), new Point(0xFFFF, 0xFFFF)
            };

            for (int i = 0; i < count; i++)
            {
                CityInfo cityInfo;

                if (isNew)
                {
                    byte cityIndex = p.ReadUInt8();
                    string cityName = p.ReadASCII(32);
                    string cityBuilding = p.ReadASCII(32);
                    ushort cityX = (ushort) p.ReadUInt32BE();
                    ushort cityY = (ushort) p.ReadUInt32BE();
                    sbyte cityZ = (sbyte) p.ReadUInt32BE();
                    uint cityMapIndex = p.ReadUInt32BE();
                    uint cityDescription = p.ReadUInt32BE();
                    p.Skip(4);

                    cityInfo = new CityInfo
                    (
                        cityIndex,
                        cityName,
                        cityBuilding,
                        Client.Game.UO.FileManager.Clilocs.GetString((int) cityDescription),
                        cityX,
                        cityY,
                        cityZ,
                        cityMapIndex,
                        isNew
                    );
                }
                else
                {
                    byte cityIndex = p.ReadUInt8();
                    string cityName = p.ReadASCII(31);
                    string cityBuilding = p.ReadASCII(31);

                    cityInfo = new CityInfo
                    (
                        cityIndex,
                        cityName,
                        cityBuilding,
                        descriptions != null ? descriptions[i] : string.Empty,
                        (ushort) oldtowns[i % oldtowns.Length].X,
                        (ushort) oldtowns[i % oldtowns.Length].Y,
                        0,
                        0,
                        isNew
                    );
                }

                cities[i] = cityInfo;
            }

            return cities;
        }

        private static string[] ReadCityTextFile(int count)
        {
            string path = Client.Game.UO.FileManager.GetUOFilePath("citytext.enu");

            if (!File.Exists(path))
            {
                return null;
            }

            string[] descr = new string[count];

            // TODO: stackalloc ?
            byte[] data = new byte[4];

            StringBuilder name = new StringBuilder();
            StringBuilder text = new StringBuilder();

            using (FileStream stream = File.OpenRead(path))
            {
                int cityIndex = 0;

                while (stream.Position < stream.Length)
                {
                    int r = stream.Read(data, 0, 4);

                    if (r == -1)
                    {
                        break;
                    }

                    string dataText = Encoding.UTF8.GetString(data, 0, 4);

                    if (dataText == "END\0")
                    {
                        name.Clear();

                        while (stream.Position < stream.Length)
                        {
                            char b = (char) stream.ReadByte();

                            if (b == '<')
                            {
                                stream.Position -= 1;

                                break;
                            }

                            name.Append(b);
                        }

                        text.Clear();

                        while (stream.Position < stream.Length)
                        {
                            char b;

                            while ((b = (char) stream.ReadByte()) != '\0')
                            {
                                text.Append(b);
                            }

                            if (text.Length != 0)
                            {
                                string t = text + "\n\n";
                                text.Clear();

                                text.Append(t);
                            }

                            long pos = stream.Position;
                            byte end = (byte) stream.ReadByte();
                            stream.Position = pos;

                            if (end == 0x2E)
                            {
                                break;
                            }

                            int r1 = stream.Read(data, 0, 4);
                            stream.Position = pos;

                            if (r1 == -1)
                            {
                                break;
                            }

                            string dataText1 = Encoding.UTF8.GetString(data, 0, 4);

                            if (dataText1 == "END\0")
                            {
                                break;
                            }
                        }

                        if (descr.Length <= cityIndex)
                        {
                            break;
                        }

                        descr[cityIndex++] = text.ToString();
                    }
                    else
                    {
                        stream.Position -= 3;
                    }
                }
            }

            return descr;
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
