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
    sealed class PacketHandlers
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

        /// <summary>
        /// Re-registers all built-in packet handlers. Used by tests after overwriting handlers.
        /// </summary>
        internal static void RegisterHandlers()
        {
            RegisterAllHandlers();
        }

        /// <summary>
        /// Dispatches a raw packet through the registered handler.
        /// The first byte of <paramref name="data"/> is the packet ID.
        /// </summary>
        internal void DispatchPacket(World world, byte[] data)
        {
            AnalyzePacket(world, data, 1);
        }

        private byte[] _readingBuffer = new byte[4096];
        private readonly PacketLogger _packetLogger = new PacketLogger();
        private readonly CircularBuffer _buffer = new CircularBuffer();
        private readonly CircularBuffer _pluginsBuffer = new CircularBuffer();

        public int ParsePackets(INetworkClient socket, World world, Span<byte> data)
        {
            Append(data, false);

            return ParsePackets(socket, world, _buffer, true) + ParsePackets(socket, world, _pluginsBuffer, false);
        }

        private int ParsePackets(INetworkClient socket, World world, CircularBuffer stream, bool allowPlugins)
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
            INetworkClient socket,
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
            RegisterAllHandlers();
        }

        private static void RegisterAllHandlers()
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
                if (world.Context.Game.UO.Version >= Utility.ClientVersion.CV_5090)
                {
                    if (Handler._clilocRequests.Count != 0)
                    {
                        world.Network.Send_MegaClilocRequest(Handler._clilocRequests);
                    }
                }
                else
                {
                    foreach (uint serial in Handler._clilocRequests)
                    {
                        world.Network.Send_MegaClilocRequest_Old(serial);
                    }

                    Handler._clilocRequests.Clear();
                }
            }

            if (Handler._customHouseRequests.Count > 0)
            {
                for (int i = 0; i < Handler._customHouseRequests.Count; ++i)
                {
                    world.Network.Send_CustomHouseDataRequest(Handler._customHouseRequests[i]);
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

                world.Context.UI.Add(new TradingGump(world, serial, name, id1, id2));
            }
            else if (type == 1)
            {
                world.Context.UI.GetTradingGump(serial)?.Dispose();
            }
            else if (type == 2)
            {
                uint id1 = p.ReadUInt32BE();
                uint id2 = p.ReadUInt32BE();

                TradingGump trading = world.Context.UI.GetTradingGump(serial);

                if (trading != null)
                {
                    trading.ImAccepting = id1 != 0;
                    trading.HeIsAccepting = id2 != 0;

                    trading.RequestUpdateContents();
                }
            }
            else if (type == 3 || type == 4)
            {
                TradingGump trading = world.Context.UI.GetTradingGump(serial);

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

            Entity entity = world.Get(p.ReadUInt32BE());

            if (entity != null)
            {
                ushort damage = p.ReadUInt16BE();

                if (damage > 0)
                {
                    world.WorldTextManager.AddDamage(entity, damage);
                }
            }
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
                            world.Context.Game.SetWindowTitle(world.Player.Name);
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
                            && world.Profile.CurrentProfile != null
                            && world.Profile.CurrentProfile.ShowStatsChangedMessage
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
                            if (world.Context.Game.UO.Version >= Utility.ClientVersion.CV_500A)
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

            if (p[0] == 0x16 && world.Context.Game.UO.Version < Utility.ClientVersion.CV_500A)
            {
                return;
            }

            Mobile mobile = world.Mobiles.Get(p.ReadUInt32BE());

            if (mobile == null)
            {
                return;
            }

            ushort count = p.ReadUInt16BE();

            for (int i = 0; i < count; i++)
            {
                ushort type = p.ReadUInt16BE();
                bool enabled = p.ReadBool();

                if (type == 1)
                {
                    if (enabled)
                    {
                        if (world.Context.Game.UO.Version >= Utility.ClientVersion.CV_7000)
                        {
                            mobile.SetSAPoison(true);
                        }
                        else
                        {
                            mobile.Flags |= Flags.Poisoned;
                        }
                    }
                    else
                    {
                        if (world.Context.Game.UO.Version >= Utility.ClientVersion.CV_7000)
                        {
                            mobile.SetSAPoison(false);
                        }
                        else
                        {
                            mobile.Flags &= ~Flags.Poisoned;
                        }
                    }
                }
                else if (type == 2)
                {
                    if (enabled)
                    {
                        mobile.Flags |= Flags.YellowBar;
                    }
                    else
                    {
                        mobile.Flags &= ~Flags.YellowBar;
                    }
                }
                else if (type == 3)
                {
                    // ???
                }
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

            UpdateGameObject(
                world,
                serial,
                graphic,
                graphicInc,
                count,
                x,
                y,
                z,
                (Direction)direction,
                hue,
                (Flags)flags,
                count,
                type,
                1
            );
        }

        private static void EnterWorld(World world, ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();

            world.CreatePlayer(serial);

            p.Skip(4);
            world.Player.Graphic = p.ReadUInt16BE();
            world.Player.CheckGraphicChange();
            ushort x = p.ReadUInt16BE();
            ushort y = p.ReadUInt16BE();
            sbyte z = (sbyte)p.ReadUInt16BE();

            if (world.Map == null)
            {
                world.MapIndex = 0;
            }

            world.Player.SetInWorldTile(x, y, z);
            world.Player.Direction = (Direction)(p.ReadUInt8() & 0x7);
            world.RangeSize.X = x;
            world.RangeSize.Y = y;

            if (
                world.Profile.CurrentProfile != null
                && world.Profile.CurrentProfile.UseCustomLightLevel
            )
            {
                world.Light.Overall =
                    world.Profile.CurrentProfile.LightLevelType == 1
                        ? Math.Min(world.Light.Overall, world.Profile.CurrentProfile.LightLevel)
                        : world.Profile.CurrentProfile.LightLevel;
            }

            world.Context.Game.Audio.UpdateCurrentMusicVolume();

            if (world.Context.Game.UO.Version >= Utility.ClientVersion.CV_200)
            {
                if (world.Profile.CurrentProfile != null)
                {
                    world.Network.Send_GameWindowSize(
                        (uint)world.Context.Game.Scene.Camera.Bounds.Width,
                        (uint)world.Context.Game.Scene.Camera.Bounds.Height
                    );
                }

                world.Network.Send_Language(world.Settings.Language);
            }

            world.Network.Send_ClientVersion(world.Settings.ClientVersion);

            GameActions.SingleClick(world, world.Player);
            world.Network.Send_SkillsRequest(world.Player.Serial);

            if (world.Player.IsDead)
            {
                world.ChangeSeason(Game.Managers.Season.Desolation, 42);
            }

            if (
                world.Context.Game.UO.Version >= Utility.ClientVersion.CV_70796
                && world.Profile.CurrentProfile != null
            )
            {
                world.Network.Send_ShowPublicHouseContent(
                    world.Profile.CurrentProfile.ShowHouseContent
                );
            }

            world.Network.Send_ToPlugins_AllSkills(world.Context.Game.UO.FileManager);
            world.Network.Send_ToPlugins_AllSpells();
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
                world.Network.Send_ACKTalk();

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

        private static void DeleteObject(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();

            if (world.Player == serial)
            {
                return;
            }

            Entity entity = world.Get(serial);

            if (entity == null)
            {
                return;
            }

            bool updateAbilities = false;

            if (entity is Item it)
            {
                uint cont = it.Container & 0x7FFFFFFF;

                if (SerialHelper.IsValid(it.Container))
                {
                    Entity top = world.Get(it.RootContainer);

                    if (top != null)
                    {
                        if (top == world.Player)
                        {
                            updateAbilities =
                                it.Layer == Layer.OneHanded || it.Layer == Layer.TwoHanded;
                            Item tradeBoxItem = world.Player.GetSecureTradeBox();

                            if (tradeBoxItem != null)
                            {
                                world.Context.UI.GetTradingGump(tradeBoxItem)?.RequestUpdateContents();
                            }
                        }
                    }

                    if (cont == world.Player && it.Layer == Layer.Invalid)
                    {
                        world.Context.Game.UO.GameCursor.ItemHold.Enabled = false;
                    }

                    if (it.Layer != Layer.Invalid)
                    {
                        world.Context.UI.GetGump<PaperDollGump>(cont)?.RequestUpdateContents();
                    }

                    world.Context.UI.GetGump<ContainerGump>(cont)?.RequestUpdateContents();

                    if (
                        top != null
                        && top.Graphic == 0x2006
                        && (
                            world.Profile.CurrentProfile.GridLootType == 1
                            || world.Profile.CurrentProfile.GridLootType == 2
                        )
                    )
                    {
                        world.Context.UI.GetGump<GridLootGump>(cont)?.RequestUpdateContents();
                    }

                    if (it.Graphic == 0x0EB0)
                    {
                        world.Context.UI.GetGump<BulletinBoardItem>(serial)?.Dispose();

                        BulletinBoardGump bbgump = world.Context.UI.GetGump<BulletinBoardGump>();

                        if (bbgump != null)
                        {
                            bbgump.RemoveBulletinObject(serial);
                        }
                    }
                }
            }

            if (world.CorpseManager.Exists(0, serial))
            {
                return;
            }

            if (entity is Mobile m)
            {
                if (world.Party.Contains(serial))
                {
                    // m.RemoveFromTile();
                }

                // else
                {
                    //BaseHealthBarGump bar = world.Context.UI.GetGump<BaseHealthBarGump>(serial);

                    //if (bar == null)
                    //{
                    //    world.Network.Send(new PCloseStatusBarGump(serial));
                    //}

                    world.RemoveMobile(serial, true);
                }
            }
            else
            {
                Item item = (Item)entity;

                if (item.IsMulti)
                {
                    world.HouseManager.Remove(serial);
                }

                Entity cont = world.Get(item.Container);

                if (cont != null)
                {
                    cont.Remove(item);

                    if (item.Layer != Layer.Invalid)
                    {
                        world.Context.UI.GetGump<PaperDollGump>(cont)?.RequestUpdateContents();
                    }
                }
                else if (item.IsMulti)
                {
                    world.Context.UI.GetGump<MiniMapGump>()?.RequestUpdateContents();
                }

                world.RemoveItem(serial, true);

                if (updateAbilities)
                {
                    world.Player.UpdateAbilities();
                }
            }
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

            UpdatePlayer(world, serial, graphic, graphic_inc, hue, flags, x, y, z, serverID, direction);
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

            world.Player.Walker.DenyWalk(seq, x, y, z);
            world.Player.Direction = direction;

            world.Weather.Reset();
        }

        private static void ConfirmWalk(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            byte seq = p.ReadUInt8();
            byte noto = (byte)(p.ReadUInt8() & ~0x40);

            if (noto == 0 || noto >= 8)
            {
                noto = 0x01;
            }

            world.Player.NotorietyFlag = (NotorietyFlag)noto;
            world.Player.Walker.ConfirmWalk(seq);

            world.Player.AddToTile();
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

            if (graphic == 0x0EED)
            {
                graphic = 0x0EEF;
            }
            else if (graphic == 0x0EEA)
            {
                graphic = 0x0EEC;
            }
            else if (graphic == 0x0EF0)
            {
                graphic = 0x0EF2;
            }

            Mobile entity = world.Mobiles.Get(source);

            if (entity == null)
            {
                source = 0;
            }
            else
            {
                sourceX = entity.X;
                sourceY = entity.Y;
                sourceZ = entity.Z;
            }

            Mobile destEntity = world.Mobiles.Get(dest);

            if (destEntity == null)
            {
                dest = 0;
            }
            else
            {
                destX = destEntity.X;
                destY = destEntity.Y;
                destZ = destEntity.Z;
            }

            world.SpawnEffect(
                !SerialHelper.IsValid(source) || !SerialHelper.IsValid(dest)
                    ? GraphicEffectType.Moving
                    : GraphicEffectType.DragEffect,
                source,
                dest,
                graphic,
                hue,
                sourceX,
                sourceY,
                sourceZ,
                destX,
                destY,
                destZ,
                5,
                5000,
                true,
                false,
                false,
                GraphicEffectBlendMode.Normal
            );

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

            if (graphic == 0xFFFF)
            {
                Item spellBookItem = world.Items.Get(serial);

                if (spellBookItem == null)
                {
                    return;
                }

                world.Context.UI.GetGump<SpellbookGump>(serial)?.Dispose();

                SpellbookGump spellbookGump = new SpellbookGump(world, spellBookItem);

                if (!world.Context.UI.GetGumpCachePosition(spellBookItem, out Point location))
                {
                    location = new Point(64, 64);
                }

                spellbookGump.Location = location;
                world.Context.UI.Add(spellbookGump);

                world.Context.Game.Audio.PlaySound(0x0055);
            }
            else if (graphic == 0x0030)
            {
                Mobile vendor = world.Mobiles.Get(serial);

                if (vendor == null)
                {
                    return;
                }

                world.Context.UI.GetGump<ShopGump>(serial)?.Dispose();

                ShopGump gump = new ShopGump(world, serial, true, 150, 5);
                world.Context.UI.Add(gump);

                for (Layer layer = Layer.ShopBuyRestock; layer < Layer.ShopBuy + 1; layer++)
                {
                    Item item = vendor.FindItemByLayer(layer);

                    LinkedObject first = item.Items;

                    if (first == null)
                    {
                        //Log.Warn("buy item not found");
                        continue;
                    }

                    bool reverse = item.Graphic != 0x2AF8; //hardcoded logic in original client that we must match

                    if (reverse)
                    {
                        while (first?.Next != null)
                        {
                            first = first.Next;
                        }
                    }

                    while (first != null)
                    {
                        Item it = (Item)first;

                        gump.AddItem(
                            it.Serial,
                            it.Graphic,
                            it.Hue,
                            it.Amount,
                            it.Price,
                            it.Name,
                            false
                        );

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
            else
            {
                Item item = world.Items.Get(serial);

                if (item != null)
                {
                    if (
                        item.IsCorpse
                        && (
                            world.Profile.CurrentProfile.GridLootType == 1
                            || world.Profile.CurrentProfile.GridLootType == 2
                        )
                    )
                    {
                        //world.Context.UI.GetGump<GridLootGump>(serial)?.Dispose();
                        //world.Context.UI.Add(new GridLootGump(serial));
                        _requestedGridLoot = serial;

                        if (world.Profile.CurrentProfile.GridLootType == 1)
                        {
                            return;
                        }
                    }

                    ContainerGump container = world.Context.UI.GetGump<ContainerGump>(serial);
                    bool playsound = false;
                    int x,
                        y;

                    // TODO: check client version ?
                    if (
                        world.Context.Game.UO.Version >= Utility.ClientVersion.CV_706000
                        && world.Profile.CurrentProfile != null
                        && world.Profile.CurrentProfile.UseLargeContainerGumps
                    )
                    {
                        var gumps = world.Context.Game.UO.Gumps;

                        switch (graphic)
                        {
                            case 0x0048:
                                if (gumps.GetGump(0x06E8).Texture != null)
                                {
                                    graphic = 0x06E8;
                                }

                                break;

                            case 0x0049:
                                if (gumps.GetGump(0x9CDF).Texture != null)
                                {
                                    graphic = 0x9CDF;
                                }

                                break;

                            case 0x0051:
                                if (gumps.GetGump(0x06E7).Texture != null)
                                {
                                    graphic = 0x06E7;
                                }

                                break;

                            case 0x003E:
                                if (gumps.GetGump(0x06E9).Texture != null)
                                {
                                    graphic = 0x06E9;
                                }

                                break;

                            case 0x004D:
                                if (gumps.GetGump(0x06EA).Texture != null)
                                {
                                    graphic = 0x06EA;
                                }

                                break;

                            case 0x004E:
                                if (gumps.GetGump(0x06E6).Texture != null)
                                {
                                    graphic = 0x06E6;
                                }

                                break;

                            case 0x004F:
                                if (gumps.GetGump(0x06E5).Texture != null)
                                {
                                    graphic = 0x06E5;
                                }

                                break;

                            case 0x004A:
                                if (gumps.GetGump(0x9CDD).Texture != null)
                                {
                                    graphic = 0x9CDD;
                                }

                                break;

                            case 0x0044:
                                if (gumps.GetGump(0x9CE3).Texture != null)
                                {
                                    graphic = 0x9CE3;
                                }

                                break;
                        }
                    }

                    if (container != null)
                    {
                        x = container.ScreenCoordinateX;
                        y = container.ScreenCoordinateY;
                        container.Dispose();
                    }
                    else
                    {
                        world.ContainerManager.CalculateContainerPosition(serial, graphic);
                        x = world.ContainerManager.X;
                        y = world.ContainerManager.Y;
                        playsound = true;
                    }

                    world.Context.UI.Add(
                        new ContainerGump(world, item, graphic, playsound)
                        {
                            X = x,
                            Y = y,
                            InvalidateContents = true
                        }
                    );

                    world.Context.UI.RemovePosition(serial);
                }
                else
                {
                    Log.Error("[OpenContainer]: item not found");
                }
            }

            if (graphic != 0x0030)
            {
                Item it = world.Items.Get(serial);

                if (it != null)
                {
                    it.Opened = true;

                    if (!it.IsCorpse && graphic != 0xFFFF)
                    {
                        ClearContainerAndRemoveItems(world, it);
                    }
                }
            }
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

            if (world.Context.Game.UO.Version >= Utility.ClientVersion.CV_6017)
            {
                p.Skip(1);
            }

            uint containerSerial = p.ReadUInt32BE();
            ushort hue = p.ReadUInt16BE();

            AddItemToContainer(world, serial, graphic, amount, x, y, hue, containerSerial);
        }

        private static void DenyMoveItem(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            Item firstItem = world.Items.Get(world.Context.Game.UO.GameCursor.ItemHold.Serial);

            if (
                world.Context.Game.UO.GameCursor.ItemHold.Enabled
                || world.Context.Game.UO.GameCursor.ItemHold.Dropped
                    && (firstItem == null || !firstItem.AllowedToDraw)
            )
            {
                if (world.ObjectToRemove == world.Context.Game.UO.GameCursor.ItemHold.Serial)
                {
                    world.ObjectToRemove = 0;
                }

                if (
                    SerialHelper.IsValid(world.Context.Game.UO.GameCursor.ItemHold.Serial)
                    && world.Context.Game.UO.GameCursor.ItemHold.Graphic != 0xFFFF
                )
                {
                    if (!world.Context.Game.UO.GameCursor.ItemHold.UpdatedInWorld)
                    {
                        if (
                            world.Context.Game.UO.GameCursor.ItemHold.Layer == Layer.Invalid
                            && SerialHelper.IsValid(world.Context.Game.UO.GameCursor.ItemHold.Container)
                        )
                        {
                            // Server should send an UpdateContainedItem after this packet.
                            Console.WriteLine("=== DENY === ADD TO CONTAINER");

                            AddItemToContainer(
                                world,
                                world.Context.Game.UO.GameCursor.ItemHold.Serial,
                                world.Context.Game.UO.GameCursor.ItemHold.Graphic,
                                world.Context.Game.UO.GameCursor.ItemHold.TotalAmount,
                                world.Context.Game.UO.GameCursor.ItemHold.X,
                                world.Context.Game.UO.GameCursor.ItemHold.Y,
                                world.Context.Game.UO.GameCursor.ItemHold.Hue,
                                world.Context.Game.UO.GameCursor.ItemHold.Container
                            );

                            world.Context.UI
                                .GetGump<ContainerGump>(world.Context.Game.UO.GameCursor.ItemHold.Container)
                                ?.RequestUpdateContents();
                        }
                        else
                        {
                            Item item = world.GetOrCreateItem(
                                world.Context.Game.UO.GameCursor.ItemHold.Serial
                            );

                            item.Graphic = world.Context.Game.UO.GameCursor.ItemHold.Graphic;
                            item.Hue = world.Context.Game.UO.GameCursor.ItemHold.Hue;
                            item.Amount = world.Context.Game.UO.GameCursor.ItemHold.TotalAmount;
                            item.Flags = world.Context.Game.UO.GameCursor.ItemHold.Flags;
                            item.Layer = world.Context.Game.UO.GameCursor.ItemHold.Layer;
                            item.X = world.Context.Game.UO.GameCursor.ItemHold.X;
                            item.Y = world.Context.Game.UO.GameCursor.ItemHold.Y;
                            item.Z = world.Context.Game.UO.GameCursor.ItemHold.Z;
                            item.CheckGraphicChange();

                            Entity container = world.Get(world.Context.Game.UO.GameCursor.ItemHold.Container);

                            if (container != null)
                            {
                                if (SerialHelper.IsMobile(container.Serial))
                                {
                                    Console.WriteLine("=== DENY === ADD TO PAPERDOLL");

                                    world.RemoveItemFromContainer(item);
                                    container.PushToBack(item);
                                    item.Container = container.Serial;

                                    world.Context.UI
                                        .GetGump<PaperDollGump>(item.Container)
                                        ?.RequestUpdateContents();
                                }
                                else
                                {
                                    Console.WriteLine("=== DENY === SOMETHING WRONG");

                                    world.RemoveItem(item, true);
                                }
                            }
                            else
                            {
                                Console.WriteLine("=== DENY === ADD TO TERRAIN");

                                world.RemoveItemFromContainer(item);

                                item.SetInWorldTile(item.X, item.Y, item.Z);
                            }
                        }
                    }
                }
                else
                {
                    Log.Error(
                        $"Wrong data: serial = {world.Context.Game.UO.GameCursor.ItemHold.Serial:X8}  -  graphic = {world.Context.Game.UO.GameCursor.ItemHold.Graphic:X4}"
                    );
                }

                world.Context.UI.GetGump<SplitMenuGump>(world.Context.Game.UO.GameCursor.ItemHold.Serial)?.Dispose();

                world.Context.Game.UO.GameCursor.ItemHold.Clear();
            }
            else
            {
                Log.Warn("There was a problem with ItemHold object. It was cleared before :|");
            }

            //var result = World.Items.Get(ItemHold.Serial);

            //if (result != null && !result.IsDestroyed)
            //    result.AllowedToDraw = true;

            byte code = p.ReadUInt8();

            if (code < 5)
            {
                world.MessageManager.HandleMessage(
                    null,
                    ServerErrorMessages.GetError(world.Context.Game.UO.FileManager.Clilocs, p[0], code),
                    string.Empty,
                    0x03b2,
                    MessageType.System,
                    3,
                    TextType.SYSTEM
                );
            }
        }

        private static void EndDraggingItem(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            world.Context.Game.UO.GameCursor.ItemHold.Enabled = false;
            world.Context.Game.UO.GameCursor.ItemHold.Dropped = false;
        }

        private static void DropItemAccepted(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            world.Context.Game.UO.GameCursor.ItemHold.Enabled = false;
            world.Context.Game.UO.GameCursor.ItemHold.Dropped = false;

            Console.WriteLine("PACKET - ITEM DROP OK!");
        }

        private static void DeathScreen(World world, ref StackDataReader p)
        {
            // todo
            byte action = p.ReadUInt8();

            if (action != 1)
            {
                world.Weather.Reset();

                world.Context.Game.Audio.PlayMusic(world.Context.Game.Audio.DeathMusicIndex, true);

                if (world.Profile.CurrentProfile.EnableDeathScreen)
                {
                    world.Player.DeathScreenTimer = Time.Ticks + Constants.DEATH_SCREEN_TIMER;
                }

                GameActions.RequestWarMode(world.Player, false);
            }
        }

        private static void MobileAttributes(World world, ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();

            Entity entity = world.Get(serial);

            if (entity == null)
            {
                return;
            }

            entity.HitsMax = p.ReadUInt16BE();
            entity.Hits = p.ReadUInt16BE();

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

                mobile.ManaMax = p.ReadUInt16BE();
                mobile.Mana = p.ReadUInt16BE();
                mobile.StaminaMax = p.ReadUInt16BE();
                mobile.Stamina = p.ReadUInt16BE();

                if (mobile == world.Player)
                {
                    world.UoAssist.SignalHits();
                    world.UoAssist.SignalStamina();
                    world.UoAssist.SignalMana();
                }
            }
        }

        private static void EquipItem(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();

            Item item = world.GetOrCreateItem(serial);

            if (item.Graphic != 0 && item.Layer != Layer.Backpack)
            {
                //ClearContainerAndRemoveItems(item);
                world.RemoveItemFromContainer(item);
            }

            if (SerialHelper.IsValid(item.Container))
            {
                world.Context.UI.GetGump<ContainerGump>(item.Container)?.RequestUpdateContents();

                world.Context.UI.GetGump<PaperDollGump>(item.Container)?.RequestUpdateContents();
            }

            item.Graphic = (ushort)(p.ReadUInt16BE() + p.ReadInt8());
            item.Layer = (Layer)p.ReadUInt8();
            item.Container = p.ReadUInt32BE();
            item.FixHue(p.ReadUInt16BE());
            item.Amount = 1;

            Entity entity = world.Get(item.Container);

            entity?.PushToBack(item);

            if (item.Layer >= Layer.ShopBuyRestock && item.Layer <= Layer.ShopSell)
            {
                //item.Clear();
            }
            else if (SerialHelper.IsValid(item.Container) && item.Layer < Layer.Mount)
            {
                world.Context.UI.GetGump<PaperDollGump>(item.Container)?.RequestUpdateContents();
            }

            if (
                entity == world.Player
                && (item.Layer == Layer.OneHanded || item.Layer == Layer.TwoHanded)
            )
            {
                world.Player?.UpdateAbilities();
            }

            //if (ItemHold.Serial == item.Serial)
            //{
            //    Console.WriteLine("PACKET - ITEM EQUIP");
            //    ItemHold.Enabled = false;
            //    ItemHold.Dropped = true;
            //}
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

            const int TIME_TURN_TO_LASTTARGET = 2000;

            if (
                world.TargetManager.LastAttack == defenders
                && world.Player.InWarMode
                && world.Player.Walker.LastStepRequestTime + TIME_TURN_TO_LASTTARGET < Time.Ticks
                && world.Player.Steps.Count == 0
            )
            {
                Mobile enemy = world.Mobiles.Get(defenders);

                if (enemy != null)
                {
                    Direction pdir = DirectionHelper.GetDirectionAB(
                        world.Player.X,
                        world.Player.Y,
                        enemy.X,
                        enemy.Y
                    );

                    int x = world.Player.X;
                    int y = world.Player.Y;
                    sbyte z = world.Player.Z;

                    if (
                        world.Player.Pathfinder.CanWalk(ref pdir, ref x, ref y, ref z)
                        && world.Player.Direction != pdir
                    )
                    {
                        world.Player.Walk(pdir, false);
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

                world.Context.Game.UO.FileManager.Skills.Skills.Clear();
                world.Context.Game.UO.FileManager.Skills.SortedSkills.Clear();

                for (int i = 0; i < count; i++)
                {
                    bool haveButton = p.ReadBool();
                    int nameLength = p.ReadUInt8();

                    world.Context.Game.UO.FileManager.Skills.Skills.Add(
                        new SkillEntry(i, p.ReadASCII(nameLength), haveButton)
                    );
                }

                world.Context.Game.UO.FileManager.Skills.SortedSkills.AddRange(world.Context.Game.UO.FileManager.Skills.Skills);

                world.Context.Game.UO.FileManager.Skills.SortedSkills.Sort(
                    (a, b) => string.Compare(a.Name, b.Name, StringComparison.InvariantCulture)
                );
            }
            else
            {
                StandardSkillsGump standard = null;
                SkillGumpAdvanced advanced = null;

                if (world.Profile.CurrentProfile.StandardSkillsGump)
                {
                    standard = world.Context.UI.GetGump<StandardSkillsGump>();
                }
                else
                {
                    advanced = world.Context.UI.GetGump<SkillGumpAdvanced>();
                }

                if (!isSingleUpdate && (type == 1 || type == 3 || world.SkillsRequested))
                {
                    world.SkillsRequested = false;

                    // TODO: make a base class for this gump
                    if (world.Profile.CurrentProfile.StandardSkillsGump)
                    {
                        if (standard == null)
                        {
                            world.Context.UI.Add(standard = new StandardSkillsGump(world) { X = 100, Y = 100 });
                        }
                    }
                    else
                    {
                        if (advanced == null)
                        {
                            world.Context.UI.Add(advanced = new SkillGumpAdvanced(world) { X = 100, Y = 100 });
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
                                    && world.Profile.CurrentProfile != null
                                    && world.Profile.CurrentProfile.ShowSkillsChangedMessage
                                    && Math.Abs(change * 10)
                                        >= world.Profile.CurrentProfile.ShowSkillsChangedDeltaValue
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

            world.Player.Pathfinder.WalkTo(x, y, z, 0);
        }

        private static void UpdateContainedItems(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            ushort count = p.ReadUInt16BE();

            for (int i = 0; i < count; i++)
            {
                uint serial = p.ReadUInt32BE();
                ushort graphic = (ushort)(p.ReadUInt16BE() + p.ReadUInt8());
                ushort amount = Math.Max(p.ReadUInt16BE(), (ushort)1);
                ushort x = p.ReadUInt16BE();
                ushort y = p.ReadUInt16BE();

                if (world.Context.Game.UO.Version >= Utility.ClientVersion.CV_6017)
                {
                    p.Skip(1);
                }

                uint containerSerial = p.ReadUInt32BE();
                ushort hue = p.ReadUInt16BE();

                if (i == 0)
                {
                    Entity container = world.Get(containerSerial);

                    if (container != null)
                    {
                        ClearContainerAndRemoveItems(world, container, container.Graphic == 0x2006);
                    }
                }

                AddItemToContainer(world, serial, graphic, amount, x, y, hue, containerSerial);
            }
        }

        private static void CloseVendorInterface(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();

            world.Context.UI.GetGump<ShopGump>(serial)?.Dispose();
        }

        private static void PersonalLightLevel(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            if (world.Player == p.ReadUInt32BE())
            {
                byte level = p.ReadUInt8();

                if (level > 0x1E)
                {
                    level = 0x1E;
                }

                world.Light.RealPersonal = level;

                if (!world.Profile.CurrentProfile.UseCustomLightLevel)
                {
                    world.Light.Personal = level;
                }
            }
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

            world.Light.RealOverall = level;

            if (
                !world.Profile.CurrentProfile.UseCustomLightLevel
                || world.Profile.CurrentProfile.LightLevelType == 1
            )
            {
                world.Light.Overall =
                    world.Profile.CurrentProfile.LightLevelType == 1
                        ? Math.Min(level, world.Profile.CurrentProfile.LightLevel)
                        : level;
            }
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

            world.Context.Game.Audio.PlaySoundWithDistance(world, index, x, y);
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
                    world.Context.Game.Audio.StopMusic();
                }
                else
                {
                    world.Context.Game.Audio.PlayMusic(index);
                }
            }
            else
            {
                ushort index = p.ReadUInt16BE();
                world.Context.Game.Audio.PlayMusic(index);
            }
        }

        private static void LoginComplete(World world, ref StackDataReader p)
        {
            if (world.Player != null && world.Context.Game.Scene is LoginScene)
            {
                var scene = new GameScene(world);
                world.Context.Game.SetScene(scene);

                //GameActions.OpenPaperdoll(world.Player);
                GameActions.RequestMobileStatus(world, world.Player);
                world.Network.Send_OpenChat("");

                world.Network.Send_SkillsRequest(world.Player);
                scene.DoubleClickDelayed(world.Player);

                if (world.Context.Game.UO.Version >= Utility.ClientVersion.CV_306E)
                {
                    world.Network.Send_ClientType(world.Context.Game.UO.Protocol);
                }

                if (world.Context.Game.UO.Version >= Utility.ClientVersion.CV_305D)
                {
                    world.Network.Send_ClientViewRange(world.ClientViewRange);
                }

                List<Gump> gumps = world.Profile.CurrentProfile.ReadGumps(
                    world,
                    world.Profile.ProfilePath
                );

                if (gumps != null)
                {
                    foreach (Gump gump in gumps)
                    {
                        world.Context.UI.Add(gump);
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

            MapGump gump = world.Context.UI.GetGump<MapGump>(serial);

            if (gump != null)
            {
                switch ((MapMessageType)p.ReadUInt8())
                {
                    case MapMessageType.Add:
                        p.Skip(1);

                        ushort x = p.ReadUInt16BE();
                        ushort y = p.ReadUInt16BE();

                        gump.AddPin(x, y);

                        break;

                    case MapMessageType.Insert:
                        break;
                    case MapMessageType.Move:
                        break;
                    case MapMessageType.Remove:
                        break;

                    case MapMessageType.Clear:
                        gump.ClearContainer();

                        break;

                    case MapMessageType.Edit:
                        break;

                    case MapMessageType.EditResponse:
                        gump.SetPlotState(p.ReadUInt8());

                        break;
                }
            }
        }

        private static void SetTime(World world, ref StackDataReader p) { }

        private static void SetWeather(World world, ref StackDataReader p)
        {
            GameScene scene = world.Context.Game.GetScene<GameScene>();

            if (scene == null)
            {
                return;
            }

            WeatherType type = (WeatherType)p.ReadUInt8();

            if (world.Weather.CurrentWeather != type)
            {
                byte count = p.ReadUInt8();
                byte temp = p.ReadUInt8();

                world.Weather.Generate(type, count, temp);
            }
        }

        private static void BookData(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            ushort pageCnt = p.ReadUInt16BE();

            ModernBookGump gump = world.Context.UI.GetGump<ModernBookGump>(serial);

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
                            gump.BookLines[index] = gump.IsNewBook
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

        private static void CharacterAnimation(World world, ref StackDataReader p)
        {
            Mobile mobile = world.Mobiles.Get(p.ReadUInt32BE());

            if (mobile == null)
            {
                return;
            }

            ushort action = p.ReadUInt16BE();
            ushort frame_count = p.ReadUInt16BE();
            ushort repeat_count = p.ReadUInt16BE();
            bool forward = !p.ReadBool();
            bool repeat = p.ReadBool();
            byte delay = p.ReadUInt8();

            mobile.SetAnimation(
                Mobile.GetReplacedObjectAnimation(world.Context.Game.UO.FileManager.Animations, world.Context.Game.UO.Animations, mobile.Graphic, action),
                delay,
                (byte)frame_count,
                (byte)repeat_count,
                repeat,
                forward,
                true
            );
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

            world.SpawnEffect(
                type,
                source,
                target,
                graphic,
                (ushort)hue,
                srcX,
                srcY,
                srcZ,
                targetX,
                targetY,
                targetZ,
                speed,
                duration,
                fixedDirection,
                doesExplode,
                false,
                blendmode
            );
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
                            BulletinBoardGump bulletinBoard = world.Context.UI.GetGump<BulletinBoardGump>(
                                serial
                            );
                            bulletinBoard?.Dispose();

                            int x = (world.Context.Game.ClientBounds.Width >> 1) - 245;
                            int y = (world.Context.Game.ClientBounds.Height >> 1) - 205;

                            bulletinBoard = new BulletinBoardGump(world, item, x, y, p.ReadUTF8(22, true)); //p.ReadASCII(22));
                            world.Context.UI.Add(bulletinBoard);

                            item.Opened = true;
                        }
                    }

                    break;

                case 1: // summary msg

                    {
                        uint boardSerial = p.ReadUInt32BE();
                        BulletinBoardGump bulletinBoard = world.Context.UI.GetGump<BulletinBoardGump>(
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
                        BulletinBoardGump bulletinBoard = world.Context.UI.GetGump<BulletinBoardGump>(
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

                            world.Context.UI.Add(
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

        private static void Warmode(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            world.Player.InWarMode = p.ReadBool();
        }

        private static void Ping(World world, ref StackDataReader p)
        {
            world.Network.Statistics.PingReceived(p.ReadUInt8());
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

            ShopGump gump = world.Context.UI.GetGump<ShopGump>();

            if (gump != null && (gump.LocalSerial != vendor || !gump.IsBuyGump))
            {
                gump.Dispose();
                gump = null;
            }

            if (gump == null)
            {
                gump = new ShopGump(world, vendor, true, 150, 5);
                world.Context.UI.Add(gump);
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
                        it.Name = world.Context.Game.UO.FileManager.Clilocs.Translate(
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

        private static void UpdateCharacter(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            Mobile mobile = world.Mobiles.Get(serial);

            if (mobile == null)
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

            mobile.NotorietyFlag = notoriety;

            if (serial == world.Player)
            {
                mobile.Flags = flags;
                mobile.Graphic = graphic;
                mobile.CheckGraphicChange();
                mobile.FixHue(hue);
                // TODO: x,y,z, direction cause elastic effect, ignore 'em for the moment
            }
            else
            {
                UpdateGameObject(world, serial, graphic, 0, 0, x, y, z, direction, hue, flags, 0, 1, 1);
            }
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
            bool oldDead = false;
            //bool alreadyExists =world.Get(serial) != null;

            if (serial == world.Player)
            {
                oldDead = world.Player.IsDead;
                world.Player.Graphic = graphic;
                world.Player.CheckGraphicChange();
                world.Player.FixHue(hue);
                world.Player.Flags = flags;
            }
            else
            {
                UpdateGameObject(world, serial, graphic, 0, 0, x, y, z, direction, hue, flags, 0, 0, 1);
            }

            Entity obj = world.Get(serial);

            if (obj == null)
            {
                return;
            }

            if (!obj.IsEmpty)
            {
                LinkedObject o = obj.Items;

                while (o != null)
                {
                    LinkedObject next = o.Next;
                    Item it = (Item)o;

                    if (!it.Opened && it.Layer != Layer.Backpack)
                    {
                        world.RemoveItem(it.Serial, true);
                    }

                    o = next;
                }
            }

            if (SerialHelper.IsMobile(serial) && obj is Mobile mob)
            {
                mob.NotorietyFlag = notoriety;

                world.Context.UI.GetGump<PaperDollGump>(serial)?.RequestUpdateContents();
            }

            if (p[0] != 0x78)
            {
                p.Skip(6);
            }

            uint itemSerial = p.ReadUInt32BE();

            while (itemSerial != 0 && p.Position < p.Length)
            {
                //if (!SerialHelper.IsItem(itemSerial))
                //    break;

                ushort itemGraphic = p.ReadUInt16BE();
                byte layer = p.ReadUInt8();
                ushort item_hue = 0;

                if (world.Context.Game.UO.Version >= Utility.ClientVersion.CV_70331)
                {
                    item_hue = p.ReadUInt16BE();
                }
                else if ((itemGraphic & 0x8000) != 0)
                {
                    itemGraphic &= 0x7FFF;
                    item_hue = p.ReadUInt16BE();
                }

                Item item = world.GetOrCreateItem(itemSerial);
                item.Graphic = itemGraphic;
                item.FixHue(item_hue);
                item.Amount = 1;
                world.RemoveItemFromContainer(item);
                item.Container = serial;
                item.Layer = (Layer)layer;

                item.CheckGraphicChange();

                obj.PushToBack(item);

                itemSerial = p.ReadUInt32BE();
            }

            if (serial == world.Player)
            {
                if (oldDead != world.Player.IsDead)
                {
                    if (world.Player.IsDead)
                    {
                        // NOTE: This packet causes some weird issue on sphere servers.
                        //       When the character dies, this packet trigger a "reset" and
                        //       somehow it messes up the packet reading server side
                        //world.Network.Send_DeathScreen();
                        world.ChangeSeason(Game.Managers.Season.Desolation, 42);
                    }
                    else
                    {
                        world.ChangeSeason(world.OldSeason, world.OldMusicIndex);
                    }
                }

                world.Context.UI.GetGump<PaperDollGump>(serial)?.RequestUpdateContents();

                world.Player.UpdateAbilities();
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

                    ref readonly var artInfo = ref world.Context.Game.UO.Arts.GetArt(graphic);

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

                world.Context.UI.Add(gump);
            }
            else
            {
                GrayMenuGump gump = new GrayMenuGump(world, serial, id, name)
                {
                    X = (world.Context.Game.ClientBounds.Width >> 1) - 200,
                    Y = (world.Context.Game.ClientBounds.Height >> 1) - ((121 + count * 21) >> 1)
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
                    new Button(world.Context, 0, 0x1450, 0x1451, 0x1450)
                    {
                        ButtonAction = ButtonAction.Activate,
                        X = 70,
                        Y = offsetY
                    }
                );

                gump.Add(
                    new Button(world.Context, 1, 0x13B2, 0x13B3, 0x13B2)
                    {
                        ButtonAction = ButtonAction.Activate,
                        X = 200,
                        Y = offsetY
                    }
                );

                gump.SetHeight(gumpHeight);
                gump.WantUpdateSize = false;
                world.Context.UI.Add(gump);
            }
        }

        private static void OpenPaperdoll(World world, ref StackDataReader p)
        {
            Mobile mobile = world.Mobiles.Get(p.ReadUInt32BE());

            if (mobile == null)
            {
                return;
            }

            string text = p.ReadASCII(60);
            byte flags = p.ReadUInt8();

            mobile.Title = text;

            PaperDollGump paperdoll = world.Context.UI.GetGump<PaperDollGump>(mobile);

            if (paperdoll == null)
            {
                if (!world.Context.UI.GetGumpCachePosition(mobile, out Point location))
                {
                    location = new Point(100, 100);
                }

                world.Context.UI.Add(
                    new PaperDollGump(world, mobile, (flags & 0x02) != 0) { Location = location }
                );
            }
            else
            {
                bool old = paperdoll.CanLift;
                bool newLift = (flags & 0x02) != 0;

                paperdoll.CanLift = newLift;
                paperdoll.UpdateTitle(text);

                if (old != newLift)
                {
                    paperdoll.RequestUpdateContents();
                }

                paperdoll.SetInScreen();
                paperdoll.BringOnTop();
            }
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

            Layer layer = (Layer)p.ReadUInt8();

            while (layer != Layer.Invalid && p.Position < p.Length)
            {
                uint item_serial = p.ReadUInt32BE();

                if (layer - 1 != Layer.Backpack)
                {
                    Item item = world.GetOrCreateItem(item_serial);

                    world.RemoveItemFromContainer(item);
                    item.Container = serial;
                    item.Layer = layer - 1;
                    corpse.PushToBack(item);
                }

                layer = (Layer)p.ReadUInt8();
            }
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

            MapGump gump = new MapGump(world, serial, gumpid, width, height);
            SpriteInfo multiMapInfo;

            if (p[0] == 0xF5 || world.Context.Game.UO.Version >= Utility.ClientVersion.CV_308Z)
            {
                ushort facet = 0;

                if (p[0] == 0xF5)
                {
                    facet = p.ReadUInt16BE();
                }

                multiMapInfo = world.Context.Game.UO.MultiMaps.GetMap(facet, width, height, startX, startY, endX, endY);
            }
            else
            {
                multiMapInfo = world.Context.Game.UO.MultiMaps.GetMap(null, width, height, startX, startY, endX, endY);
            }

            if (multiMapInfo.Texture != null)
                gump.SetMapTexture(multiMapInfo.Texture);

            world.Context.UI.Add(gump);

            Item it = world.Items.Get(serial);

            if (it != null)
            {
                it.Opened = true;
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

            ModernBookGump bgump = world.Context.UI.GetGump<ModernBookGump>(serial);

            if (bgump == null || bgump.IsDisposed)
            {
                ushort page_count = p.ReadUInt16BE();
                string title = oldpacket
                    ? p.ReadUTF8(60, true)
                    : p.ReadUTF8(p.ReadUInt16BE(), true);
                string author = oldpacket
                    ? p.ReadUTF8(30, true)
                    : p.ReadUTF8(p.ReadUInt16BE(), true);

                world.Context.UI.Add(
                    new ModernBookGump(world, serial, page_count, title, author, editable, oldpacket)
                    {
                        X = 100,
                        Y = 100
                    }
                );

                world.Network.Send_BookPageDataRequest(serial, 1);
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

        private static void DyeData(World world, ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();
            p.Skip(2);
            ushort graphic = p.ReadUInt16BE();

            ref readonly var gumpInfo = ref world.Context.Game.UO.Gumps.GetGump(0x0906);

            int x = (world.Context.Game.ClientBounds.Width >> 1) - (gumpInfo.UV.Width >> 1);
            int y = (world.Context.Game.ClientBounds.Height >> 1) - (gumpInfo.UV.Height >> 1);

            ColorPickerGump gump = world.Context.UI.GetGump<ColorPickerGump>(serial);

            if (gump == null || gump.IsDisposed || gump.Graphic != graphic)
            {
                gump?.Dispose();

                gump = new ColorPickerGump(world, serial, graphic, x, y, null);

                world.Context.UI.Add(gump);
            }
        }

        private static void MovePlayer(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            Direction direction = (Direction)p.ReadUInt8();
            world.Player.Walk(direction & Direction.Mask, (direction & Direction.Running) != 0);
        }

        private static void UpdateName(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            string name = p.ReadASCII();

            WMapEntity wme = world.WMapManager.GetEntity(serial);

            if (wme != null && !string.IsNullOrEmpty(name))
            {
                wme.Name = name;
            }

            Entity entity = world.Get(serial);

            if (entity != null)
            {
                entity.Name = name;

                if (
                    serial == world.Player.Serial
                    && !string.IsNullOrEmpty(name)
                    && name != world.Player.Name
                )
                {
                    world.Context.Game.SetWindowTitle(name);
                }

                world.Context.UI.GetGump<NameOverheadGump>(serial)?.SetName();
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

            ShopGump gump = world.Context.UI.GetGump<ShopGump>(vendor);
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
                    name = world.Context.Game.UO.FileManager.Clilocs.GetString(clilocnum);
                    fromcliloc = true;
                }
                else if (string.IsNullOrEmpty(name))
                {
                    bool success = world.OPL.TryGetNameAndData(serial, out name, out _);

                    if (!success)
                    {
                        name = world.Context.Game.UO.FileManager.TileData.StaticData[graphic].Name;
                    }
                }

                //if (string.IsNullOrEmpty(item.Name))
                //    item.Name = name;

                gump.AddItem(serial, graphic, hue, amount, price, name, fromcliloc);
            }

            world.Context.UI.Add(gump);
        }

        private static void UpdateHitpoints(World world, ref StackDataReader p)
        {
            Entity entity = world.Get(p.ReadUInt32BE());

            if (entity == null)
            {
                return;
            }

            entity.HitsMax = p.ReadUInt16BE();
            entity.Hits = p.ReadUInt16BE();

            if (entity.HitsRequest == HitsRequestStatus.Pending)
            {
                entity.HitsRequest = HitsRequestStatus.Received;
            }

            if (entity == world.Player)
            {
                world.UoAssist.SignalHits();
            }
        }

        private static void UpdateMana(World world, ref StackDataReader p)
        {
            Mobile mobile = world.Mobiles.Get(p.ReadUInt32BE());

            if (mobile == null)
            {
                return;
            }

            mobile.ManaMax = p.ReadUInt16BE();
            mobile.Mana = p.ReadUInt16BE();

            if (mobile == world.Player)
            {
                world.UoAssist.SignalMana();
            }
        }

        private static void UpdateStamina(World world, ref StackDataReader p)
        {
            Mobile mobile = world.Mobiles.Get(p.ReadUInt32BE());

            if (mobile == null)
            {
                return;
            }

            mobile.StaminaMax = p.ReadUInt16BE();
            mobile.Stamina = p.ReadUInt16BE();

            if (mobile == world.Player)
            {
                world.UoAssist.SignalStamina();
            }
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

            world.Context.UI.Add(new TipNoticeGump(world, tip, flag, str) { X = x, Y = y });
        }

        private static void AttackCharacter(World world, ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();

            //if (TargetManager.LastAttack != serial && World.InGame)
            //{



            //}

            GameActions.SendCloseStatus(world, world.TargetManager.LastAttack);
            world.TargetManager.LastAttack = serial;
            GameActions.RequestMobileStatus(world, serial);
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

            world.Context.UI.Add(gump);
        }

        private static void UnicodeTalk(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                LoginScene scene = world.Context.Game.GetScene<LoginScene>();

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

                world.Network.Send(buffer);

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
                world.Profile.CurrentProfile.ChatFont,
                text_type,
                true,
                lang
            );
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

            Mobile owner = world.Mobiles.Get(serial);

            if (owner == null || serial == world.Player)
            {
                return;
            }

            serial |= 0x80000000;

            if (world.Mobiles.Remove(owner.Serial))
            {
                for (LinkedObject i = owner.Items; i != null; i = i.Next)
                {
                    Item it = (Item)i;
                    it.Container = serial;
                }

                world.Mobiles[serial] = owner;
                owner.Serial = serial;
            }

            if (SerialHelper.IsValid(corpseSerial))
            {
                world.CorpseManager.Add(corpseSerial, serial, owner.Direction, running != 0);
            }

            var animations = world.Context.Game.UO.Animations;
            var gfx = owner.Graphic;
            animations.ConvertBodyIfNeeded(ref gfx);
            var animGroup = animations.GetAnimType(gfx);
            var animFlags = animations.GetAnimFlags(gfx);
            byte group = world.Context.Game.UO.FileManager.Animations.GetDeathAction(
                gfx,
                animFlags,
                animGroup,
                running != 0,
                true
            );
            owner.SetAnimation(group, 0, 5, 1);
            owner.AnimIndex = 0;

            if (world.Profile.CurrentProfile.AutoOpenCorpses)
            {
                world.Player.TryOpenCorpses();
            }
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

                    world.Context.UI.GetGump<ChatGump>()?.RequestUpdateContents();

                    break;

                case 0x03E9: // destroy conference
                    p.Skip(4);
                    channelName = p.ReadUnicodeBE();
                    world.ChatManager.RemoveChannel(channelName);

                    world.Context.UI.GetGump<ChatGump>()?.RequestUpdateContents();

                    break;

                case 0x03EB: // display enter username window
                    world.ChatManager.ChatIsEnabled = ChatStatus.EnabledUserRequest;

                    break;

                case 0x03EC: // close chat
                    world.ChatManager.Clear();
                    world.ChatManager.ChatIsEnabled = ChatStatus.Disabled;

                    world.Context.UI.GetGump<ChatGump>()?.Dispose();

                    break;

                case 0x03ED: // username accepted, display chat
                    p.Skip(4);
                    string username = p.ReadUnicodeBE();
                    world.ChatManager.ChatIsEnabled = ChatStatus.Enabled;
                    world.Network.Send_ChatJoinCommand(world.Settings.Language, "General");

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

                    world.Context.UI.GetGump<ChatGump>()?.UpdateConference();

                    GameActions.Print(
                        world,
                        string.Format(ResGeneral.YouHaveJoinedThe0Channel, channelName),
                        world.Profile.CurrentProfile.ChatMessageHue,
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
                        world.Profile.CurrentProfile.ChatMessageHue,
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
                        world.Profile.CurrentProfile.ChatMessageHue,
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
                            world.Profile.CurrentProfile.ChatMessageHue,
                            MessageType.Regular,
                            1
                        );
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

            world.Context.UI.GetGump<ProfileGump>(serial)?.Dispose();

            world.Context.UI.Add(
                new ProfileGump(world, serial, header, footer, body, serial == world.Player.Serial)
            );
        }

        private static void EnableLockedFeatures(World world, ref StackDataReader p)
        {
            LockedFeatureFlags flags = 0;

            if (world.Context.Game.UO.Version >= Utility.ClientVersion.CV_60142)
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

            world.Context.Game.UO.Animations.UpdateAnimationTable(bcFlags);
        }

        private static void DisplayQuestArrow(World world, ref StackDataReader p)
        {
            bool display = p.ReadBool();
            ushort mx = p.ReadUInt16BE();
            ushort my = p.ReadUInt16BE();

            uint serial = 0;

            if (world.Context.Game.UO.Version >= Utility.ClientVersion.CV_7090)
            {
                serial = p.ReadUInt32BE();
            }

            QuestArrowGump arrow = world.Context.UI.GetGump<QuestArrowGump>(serial);

            if (display)
            {
                if (arrow == null)
                {
                    world.Context.UI.Add(new QuestArrowGump(world, serial, mx, my));
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

            if (season > 4)
            {
                season = 0;
            }

            if (world.Player.IsDead && season == 4)
            {
                return;
            }

            world.OldSeason = (Season)season;
            world.OldMusicIndex = music;

            if (world.Season == Game.Managers.Season.Desolation)
            {
                world.OldMusicIndex = 42;
            }

            world.ChangeSeason((Season)season, music);
        }

        private static void ClientVersion(World world, ref StackDataReader p)
        {
            world.Network.Send_ClientVersion(world.Settings.ClientVersion);
        }

        private static void AssistVersion(World world, ref StackDataReader p)
        {
            //uint version = p.ReadUInt32BE();

            //string[] parts = Service.GetByLocalSerial<Settings>().ClientVersion.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            //byte[] clientVersionBuffer =
            //    {byte.Parse(parts[0]), byte.Parse(parts[1]), byte.Parse(parts[2]), byte.Parse(parts[3])};

            //world.Network.Send(new PAssistVersion(clientVersionBuffer, version));
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

                    LinkedListNode<Gump> first = world.Context.UI.Gumps.First;

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
                                    world.Context.UI.SavePosition(ser, first.Value.Location);
                                }
                                else
                                {
                                    world.Context.UI.RemovePosition(ser);
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
                    world.Context.UI.GetGump<HealthBarGump>(p.ReadUInt32BE())?.Dispose();

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
                        str = world.Context.Game.UO.FileManager.Clilocs.GetString((int)cliloc, true);

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
                        string attr = world.Context.Game.UO.FileManager.Clilocs.GetString((int)next);

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

                    world.Network.Send_MegaClilocRequest_Old(item);

                    break;

                //===========================================================================================
                //===========================================================================================
                case 0x11:
                    break;

                //===========================================================================================
                //===========================================================================================
                case 0x14: // display popup/context menu
                    world.Context.UI.ShowGamePopup(
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
                            world.Context.UI.GetGump<PaperDollGump>(serial)?.Dispose();

                            break;

                        case 2: //statusbar
                            world.Context.UI.GetGump<HealthBarGump>(serial)?.Dispose();

                            if (serial == world.Player.Serial)
                            {
                                StatusGumpBase.GetStatusGump(world.Profile.CurrentProfile, world.Context.UI)?.Dispose();
                            }

                            break;

                        case 8: // char profile
                            world.Context.UI.GetGump<ProfileGump>()?.Dispose();

                            break;

                        case 0x0C: //container
                            world.Context.UI.GetGump<ContainerGump>(serial)?.Dispose();

                            break;
                    }

                    break;

                //===========================================================================================
                //===========================================================================================
                case 0x18: // enable map patches

                    if (world.Context.Game.UO.FileManager.Maps.ApplyPatches(ref p))
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

                                StatusGumpBase.GetStatusGump(world.Profile.CurrentProfile, world.Context.UI)?.RequestUpdateContents();
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
                                        Mobile.GetReplacedObjectAnimation(world.Context.Game.UO.FileManager.Animations, world.Context.Game.UO.Animations, mobile.Graphic, animation)
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

                    world.Context.UI.GetGump<SpellbookGump>(spellbook)?.RequestUpdateContents();

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

                        world.Context.UI.GetGump<MiniMapGump>()?.RequestUpdateContents();

                        if (world.HouseManager.EntityIntoHouse(serial, world.Player))
                        {
                            world.Context.Game.GetScene<GameScene>()?.UpdateMaxDrawZ(true);
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
                            HouseCustomizationGump gump = world.Context.UI.GetGump<HouseCustomizationGump>();

                            if (gump != null)
                            {
                                break;
                            }

                            gump = new HouseCustomizationGump(world, serial, 50, 50);
                            world.Context.UI.Add(gump);

                            break;

                        case 5: // end
                            world.Context.UI.GetGump<HouseCustomizationGump>(serial)?.Dispose();

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

                    foreach (Gump g in world.Context.UI.Gumps)
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

                    world.Context.UI.GetGump<RaceChangeGump>()?.Dispose();
                    world.Context.UI.Add(new RaceChangeGump(world, isfemale, race));
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
                for (LinkedListNode<Gump> g = world.Context.UI.Gumps.Last; g != null; g = g.Previous)
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

            string text = world.Context.Game.UO.FileManager.Clilocs.Translate((int)cliloc, arguments);

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

            if (!world.Context.Game.UO.FileManager.Fonts.UnicodeFontExists((byte)font))
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

        private static void UnicodePrompt(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            world.MessageManager.PromptData = new PromptData(ConsolePrompt.Unicode, p.ReadUInt64BE());
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
                world.Context.Game.GetScene<GameScene>().DisconnectionRequested
                && (
                    world.ClientFeatures.Flags
                    & CharacterListFlags.CLF_OWERWRITE_CONFIGURATION_BUTTON
                ) != 0
            )
            {
                if (p.ReadBool())
                {
                    // client can disconnect
                    world.Network.Disconnect();
                    world.Context.Game.SetScene(new LoginScene(world));
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

                string str = world.Context.Game.UO.FileManager.Clilocs.Translate(cliloc, argument, true);

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
                        if (world.Context.Game.UO.Version >= Utility.ClientVersion.CV_60143)
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
                world.Context.UI.GetGump<ShopGump>(container.RootContainer)?.SetNameTo((Item)entity, name);
            }
        }

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

                world.Context.UI.GetGump<HouseCustomizationGump>(house.Serial)?.Update();
            }

            world.Context.UI.GetGump<MiniMapGump>()?.RequestUpdateContents();

            if (world.HouseManager.EntityIntoHouse(serial, world.Player))
            {
                world.Context.Game.GetScene<GameScene>()?.UpdateMaxDrawZ(true);
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

        private static void UpdateMobileStatus(World world, ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();
            byte status = p.ReadUInt8();

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
                BuffGump gump = world.Context.UI.GetGump<BuffGump>();
                ushort count = p.ReadUInt16BE();

                if (count == 0)
                {
                    world.Player.RemoveBuff(ic);
                    gump?.RequestUpdateContents();
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
                        string title = world.Context.Game.UO.FileManager.Clilocs.Translate(
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
                                + world.Context.Game.UO.FileManager.Clilocs.Translate(
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
                            wtf = world.Context.Game.UO.FileManager.Clilocs.Translate(
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
                        bool alreadyExists = world.Player.IsBuffIconExists(ic);
                        world.Player.AddBuff(ic, BuffTable.Table[iconID], timer, text);

                        if (!alreadyExists)
                        {
                            gump?.RequestUpdateContents();
                        }
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

            Mobile mobile = world.Mobiles.Get(p.ReadUInt32BE());

            if (mobile == null)
            {
                return;
            }

            ushort type = p.ReadUInt16BE();
            ushort action = p.ReadUInt16BE();
            byte mode = p.ReadUInt8();
            byte group = Mobile.GetObjectNewAnimation(mobile, type, action, mode);

            mobile.SetAnimation(
                group,
                repeatCount: 1,
                repeat: (type == 1 || type == 2) && mobile.Graphic == 0x0015,
                forward: true,
                fromServer: true
            );
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

                    world.Context.Game.EnqueueAction(5000, () =>
                    {
                        Log.Info("Razor ACK sent");
                        world.Network.Send_RazorACK();
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

            if (serial != world.Player)
            {
                UpdateGameObject(
                    world,
                    serial,
                    graphic,
                    graphicInc,
                    amount,
                    x,
                    y,
                    z,
                    dir,
                    hue,
                    flags,
                    unk,
                    type,
                    unk2
                );

                if (graphic == 0x2006 && world.Profile.CurrentProfile.AutoOpenCorpses)
                {
                    world.Player.TryOpenCorpses();
                }
            }
            else if (p[0] == 0xF7)
            {
                UpdatePlayer(world, serial, graphic, graphicInc, hue, flags, x, y, z, 0, dir);
            }
        }

        private static void BoatMoving(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            byte boatSpeed = p.ReadUInt8();
            Direction movingDirection = (Direction)p.ReadUInt8() & Direction.Mask;
            Direction facingDirection = (Direction)p.ReadUInt8() & Direction.Mask;
            ushort x = p.ReadUInt16BE();
            ushort y = p.ReadUInt16BE();
            ushort z = p.ReadUInt16BE();

            Item multi = world.Items.Get(serial);

            if (multi == null)
            {
                return;
            }

            //multi.LastX = x;
            //multi.LastY = y;

            //if (World.HouseManager.TryGetHouse(serial, out var house))
            //{
            //    foreach (Multi component in house.Components)
            //    {
            //        component.LastX = (ushort) (x + component.MultiOffsetX);
            //        component.LastY = (ushort) (y + component.MultiOffsetY);
            //    }
            //}

            bool smooth =
                world.Profile.CurrentProfile != null
                && world.Profile.CurrentProfile.UseSmoothBoatMovement;

            if (smooth)
            {
                world.BoatMovingManager.AddStep(
                    serial,
                    boatSpeed,
                    movingDirection,
                    facingDirection,
                    x,
                    y,
                    (sbyte)z
                );
            }
            else
            {
                //UpdateGameObject(serial,
                //                 multi.Graphic,
                //                 0,
                //                 multi.Amount,
                //                 x,
                //                 y,
                //                 (sbyte) z,
                //                 facingDirection,
                //                 multi.Hue,
                //                 multi.Flags,
                //                 0,
                //                 2,
                //                 1);

                multi.SetInWorldTile(x, y, (sbyte)z);

                if (world.HouseManager.TryGetHouse(serial, out House house))
                {
                    house.Generate(true, true, true);
                }
            }

            int count = p.ReadUInt16BE();

            for (int i = 0; i < count; i++)
            {
                uint cSerial = p.ReadUInt32BE();
                ushort cx = p.ReadUInt16BE();
                ushort cy = p.ReadUInt16BE();
                ushort cz = p.ReadUInt16BE();

                if (cSerial == world.Player)
                {
                    world.RangeSize.X = cx;
                    world.RangeSize.Y = cy;
                }

                Entity ent = world.Get(cSerial);

                if (ent == null)
                {
                    continue;
                }

                //if (SerialHelper.IsMobile(cSerial))
                //{
                //    Mobile m = (Mobile) ent;

                //    if (m.Steps.Count != 0)
                //    {
                //        ref var step = ref m.Steps.Back();

                //        step.X = cx;
                //        step.Y = cy;
                //    }
                //}

                //ent.LastX = cx;
                //ent.LastY = cy;

                if (smooth)
                {
                    world.BoatMovingManager.PushItemToList(
                        serial,
                        cSerial,
                        x - cx,
                        y - cy,
                        (sbyte)(z - cz)
                    );
                }
                else
                {
                    if (cSerial == world.Player)
                    {
                        UpdatePlayer(
                            world,
                            cSerial,
                            ent.Graphic,
                            0,
                            ent.Hue,
                            ent.Flags,
                            cx,
                            cy,
                            (sbyte)cz,
                            0,
                            world.Player.Direction
                        );
                    }
                    else
                    {
                        UpdateGameObject(
                            world,
                            cSerial,
                            ent.Graphic,
                            0,
                            (ushort)(ent.Graphic == 0x2006 ? ((Item)ent).Amount : 0),
                            cx,
                            cy,
                            (sbyte)cz,
                            SerialHelper.IsMobile(ent) ? ent.Direction : 0,
                            ent.Hue,
                            ent.Flags,
                            0,
                            0,
                            1
                        );
                    }
                }
            }
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

            LoginScene scene = world.Context.Game.GetScene<LoginScene>();

            if (scene != null)
            {
                scene.ServerListReceived(ref p);
            }
        }

        private static void ReceiveServerRelay(World world, ref StackDataReader p)
        {
            if (world.InGame)
            {
                return;
            }

            LoginScene scene = world.Context.Game.GetScene<LoginScene>();

            if (scene != null)
            {
                scene.HandleRelayServerPacket(ref p);
            }
        }

        private static void UpdateCharacterList(World world, ref StackDataReader p)
        {
            if (world.InGame)
            {
                return;
            }

            LoginScene scene = world.Context.Game.GetScene<LoginScene>();

            if (scene != null)
            {
                scene.UpdateCharacterList(ref p);
            }
        }

        private static void ReceiveCharacterList(World world, ref StackDataReader p)
        {
            if (world.InGame)
            {
                return;
            }

            LoginScene scene = world.Context.Game.GetScene<LoginScene>();

            if (scene != null)
            {
                scene.ReceiveCharacterList(ref p);
            }
        }

        private static void LoginDelay(World world, ref StackDataReader p)
        {
            if (world.InGame)
            {
                return;
            }

            LoginScene scene = world.Context.Game.GetScene<LoginScene>();

            if (scene != null)
            {
                scene.HandleLoginDelayPacket(ref p);
            }
        }

        private static void ReceiveLoginRejection(World world, ref StackDataReader p)
        {
            if (world.InGame)
            {
                return;
            }

            LoginScene scene = world.Context.Game.GetScene<LoginScene>();

            if (scene != null)
            {
                scene.HandleErrorCode(ref p);
            }
        }

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
            if (world.Context.Game.UO.GameCursor.ItemHold.Serial == serial)
            {
                if (world.Context.Game.UO.GameCursor.ItemHold.Dropped)
                {
                    Console.WriteLine("ADD ITEM TO CONTAINER -- CLEAR HOLD");
                    world.Context.Game.UO.GameCursor.ItemHold.Clear();
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
                    world.Context.UI.GetTradingGump(secureBox)?.RequestUpdateContents();
                }
                else
                {
                    world.Context.UI.GetGump<PaperDollGump>(containerSerial)?.RequestUpdateContents();
                }
            }
            else if (SerialHelper.IsItem(containerSerial))
            {
                Gump gump = world.Context.UI.GetGump<BulletinBoardGump>(containerSerial);

                if (gump != null)
                {
                    world.Network.Send_BulletinBoardRequestMessageSummary(
                        containerSerial,
                        serial
                    );
                }
                else
                {
                    gump = world.Context.UI.GetGump<SpellbookGump>(containerSerial);

                    if (gump == null)
                    {
                        gump = world.Context.UI.GetGump<ContainerGump>(containerSerial);

                        if (gump != null)
                        {
                            ((ContainerGump)gump).CheckItemControlPosition(item);
                        }

                        if (world.Profile.CurrentProfile.GridLootType > 0)
                        {
                            GridLootGump grid_gump = world.Context.UI.GetGump<GridLootGump>(
                                containerSerial
                            );

                            if (
                                grid_gump == null
                                && SerialHelper.IsValid(_requestedGridLoot)
                                && _requestedGridLoot == containerSerial
                            )
                            {
                                grid_gump = new GridLootGump(world, _requestedGridLoot);
                                world.Context.UI.Add(grid_gump);
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

            world.Context.UI.GetTradingGump(containerSerial)?.RequestUpdateContents();
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
                world.Context.Game.UO.GameCursor.ItemHold.Enabled
                && world.Context.Game.UO.GameCursor.ItemHold.Serial == serial
            )
            {
                if (SerialHelper.IsValid(world.Context.Game.UO.GameCursor.ItemHold.Container))
                {
                    if (world.Context.Game.UO.GameCursor.ItemHold.Layer == 0)
                    {
                        world.Context.UI
                            .GetGump<ContainerGump>(world.Context.Game.UO.GameCursor.ItemHold.Container)
                            ?.RequestUpdateContents();
                    }
                    else
                    {
                        world.Context.UI
                            .GetGump<PaperDollGump>(world.Context.Game.UO.GameCursor.ItemHold.Container)
                            ?.RequestUpdateContents();
                    }
                }

                world.Context.Game.UO.GameCursor.ItemHold.UpdatedInWorld = true;
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
                    if (world.Profile.CurrentProfile.ShowNewMobileNameIncoming)
                    {
                        GameActions.SingleClick(world, serial);
                    }
                }
                else if (graphic == 0x2006)
                {
                    if (world.Profile.CurrentProfile.ShowNewCorpseNameIncoming)
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
                    world.Context.Game.UO.GameCursor.ItemHold.Serial == serial
                    && world.Context.Game.UO.GameCursor.ItemHold.Dropped
                )
                {
                    // we want maintain the item data due to the denymoveitem packet
                    //ItemHold.Clear();
                    world.Context.Game.UO.GameCursor.ItemHold.Enabled = false;
                    world.Context.Game.UO.GameCursor.ItemHold.Dropped = false;
                }

                if (item.OnGround)
                {
                    item.SetInWorldTile(item.X, item.Y, item.Z);

                    if (graphic == 0x2006 && world.Profile.CurrentProfile.AutoOpenCorpses)
                    {
                        world.Player.TryOpenCorpses();
                    }
                }
            }
        }

        private static void UpdatePlayer(
            World world,
            uint serial,
            ushort graphic,
            byte graph_inc,
            ushort hue,
            Flags flags,
            ushort x,
            ushort y,
            sbyte z,
            ushort serverID,
            Direction direction
        )
        {
            if (serial == world.Player)
            {
                world.RangeSize.X = x;
                world.RangeSize.Y = y;

                bool olddead = world.Player.IsDead;
                ushort old_graphic = world.Player.Graphic;

                world.Player.CloseBank();
                world.Player.Walker.WalkingFailed = false;
                world.Player.Graphic = graphic;
                world.Player.Direction = direction & Direction.Mask;
                world.Player.FixHue(hue);
                world.Player.Flags = flags;
                world.Player.Walker.DenyWalk(0xFF, -1, -1, -1);

                GameScene gs = world.Context.Game.GetScene<GameScene>();

                if (gs != null)
                {
                    world.Weather.Reset();
                    gs.UpdateDrawPosition = true;
                }

                // std client keeps the target open!
                /*if (old_graphic != 0 && old_graphic != world.Player.Graphic)
                {
                    if (world.Player.IsDead)
                    {
                        TargetManager.Reset();
                    }
                }*/

                if (olddead != world.Player.IsDead)
                {
                    if (world.Player.IsDead)
                    {
                        world.ChangeSeason(Game.Managers.Season.Desolation, 42);
                    }
                    else
                    {
                        world.ChangeSeason(world.OldSeason, world.OldMusicIndex);
                    }
                }

                world.Player.Walker.ResendPacketResync = false;
                world.Player.CloseRangedGumps();
                world.Player.SetInWorldTile(x, y, z);
                world.Player.UpdateAbilities();
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

            if (world.Context.UI.GetGumpCachePosition(gumpID, out Point pos))
            {
                x = pos.X;
                y = pos.Y;

                for (
                    LinkedListNode<Gump> last = world.Context.UI.Gumps.Last;
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
                world.Context.UI.SavePosition(gumpID, new Point(x, y));
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
                    gump.Add(new Button(gparams, world.Context), page);
                }
                else if (
                    string.Equals(
                        entry,
                        "buttontileart",
                        StringComparison.InvariantCultureIgnoreCase
                    )
                )
                {
                    gump.Add(new ButtonTileArt(gparams, world.Context), page);
                }
                else if (
                    string.Equals(
                        entry,
                        "checkertrans",
                        StringComparison.InvariantCultureIgnoreCase
                    )
                )
                {
                    var checkerTrans = new CheckerTrans(gparams, world.Context);
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
                    gump.Add(new CroppedText(gparams, lines, world.Context), page);
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
                                s = world.Context.Game.UO.FileManager.Clilocs.GetString(1051000 + 2);

                                break;

                            case 0x6A:
                                s = world.Context.Game.UO.FileManager.Clilocs.GetString(1051000 + 7);

                                break;

                            case 0x6B:
                                s = world.Context.Game.UO.FileManager.Clilocs.GetString(1051000 + 5);

                                break;

                            case 0x6D:
                                s = world.Context.Game.UO.FileManager.Clilocs.GetString(1051000 + 6);

                                break;

                            case 0x6E:
                                s = world.Context.Game.UO.FileManager.Clilocs.GetString(1051000 + 1);

                                break;

                            case 0x6F:
                                s = world.Context.Game.UO.FileManager.Clilocs.GetString(1051000 + 3);

                                break;

                            case 0x70:
                                s = world.Context.Game.UO.FileManager.Clilocs.GetString(1051000 + 4);

                                break;

                            case 0x6C:
                            default:
                                s = world.Context.Game.UO.FileManager.Clilocs.GetString(1051000);

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
                        pic = new GumpPic(gparams, world.Context);
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
                    gump.Add(new GumpPicTiled(gparams, world.Context), page);
                }
                else if (
                    string.Equals(entry, "htmlgump", StringComparison.InvariantCultureIgnoreCase)
                )
                {
                    gump.Add(new HtmlControl(gparams, lines, world.Context), page);
                }
                else if (
                    string.Equals(entry, "xmfhtmlgump", StringComparison.InvariantCultureIgnoreCase)
                )
                {
                    gump.Add(
                        new HtmlControl(
                            world.Context,
                            int.Parse(gparams[1]),
                            int.Parse(gparams[2]),
                            int.Parse(gparams[3]),
                            int.Parse(gparams[4]),
                            int.Parse(gparams[6]) == 1,
                            int.Parse(gparams[7]) != 0,
                            gparams[6] != "0" && gparams[7] == "2",
                            world.Context.Game.UO.FileManager.Clilocs.GetString(int.Parse(gparams[5].Replace("#", ""))),
                            0,
                            true)
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
                            world.Context,
                            int.Parse(gparams[1]),
                            int.Parse(gparams[2]),
                            int.Parse(gparams[3]),
                            int.Parse(gparams[4]),
                            int.Parse(gparams[6]) == 1,
                            int.Parse(gparams[7]) != 0,
                            gparams[6] != "0" && gparams[7] == "2",
                            world.Context.Game.UO.FileManager.Clilocs.GetString(int.Parse(gparams[5].Replace("#", ""))),
                            color,
                            true)
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
                            world.Context,
                            int.Parse(gparams[1]),
                            int.Parse(gparams[2]),
                            int.Parse(gparams[3]),
                            int.Parse(gparams[4]),
                            int.Parse(gparams[5]) == 1,
                            int.Parse(gparams[6]) != 0,
                            gparams[5] != "0" && gparams[6] == "2",
                            sb == null
                                ? world.Context.Game.UO.FileManager.Clilocs.GetString(
                                    int.Parse(gparams[8].Replace("#", ""))
                                )
                                : world.Context.Game.UO.FileManager.Clilocs.Translate(
                                    int.Parse(gparams[8].Replace("#", "")),
                                    sb.ToString().Trim('@').Replace('@', '\t')
                                ),
                            color,
                            true)
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
                    gump.Add(new ResizePic(gparams, world.Context), page);
                }
                else if (string.Equals(entry, "text", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (gparams.Count >= 5)
                    {
                        gump.Add(new Label(gparams, lines, world.Context), page);
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
                    StbTextBox textBox = new StbTextBox(gparams, lines, world.Context);

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
                    gump.Add(new StaticPic(gparams, world.Context), page);
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
                    gump.Add(new RadioButton(group, gparams, lines, world.Context), page);
                }
                else if (
                    string.Equals(entry, "checkbox", StringComparison.InvariantCultureIgnoreCase)
                )
                {
                    gump.Add(new Checkbox(gparams, lines, world.Context), page);
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
                            text = world.Context.Game.UO.FileManager.Clilocs.GetString(int.Parse(gparams[1]));
                            Log.Error(
                                $"String '{args}' too short, something wrong with gump tooltip: {text}"
                            );
                        }
                        else
                        {
                            text = world.Context.Game.UO.FileManager.Clilocs.Translate(
                                int.Parse(gparams[1]),
                                args,
                                false
                            );
                        }
                    }
                    else
                    {
                        text = world.Context.Game.UO.FileManager.Clilocs.GetString(int.Parse(gparams[1]));
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
                        var g = gump.Add(new GumpPicInPic(gparams, world.Context), page);

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
                        gump.Add(new GumpPic(gparams, world.Context));
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
                world.Context.UI.Add(gump);
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
