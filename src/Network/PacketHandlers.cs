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
using System.Diagnostics;
using System.Linq;
using System.Text;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Resources;
using ClassicUO.Utility;
using ClassicUO.Utility.Collections;
using ClassicUO.Utility.Logging;
using ClassicUO.Utility.Platforms;
using Microsoft.Xna.Framework;

namespace ClassicUO.Network
{
    internal class PacketHandlers
    {
        public delegate void OnPacketBufferReader(ref StackDataReader p);

        private static uint _requestedGridLoot;

        private static readonly TextFileParser _parser = new TextFileParser(string.Empty, new[] { ' ' }, new char[] { }, new[] { '{', '}' });
        private static readonly TextFileParser _cmdparser = new TextFileParser(string.Empty, new[] { ' ', ',' }, new char[] { }, new[] { '@', '@' });


        private List<uint> _clilocRequests = new List<uint>();
        private readonly OnPacketBufferReader[] _handlers = new OnPacketBufferReader[0x100];


        public static PacketHandlers Handlers { get; } = new PacketHandlers();


        public void Add(byte id, OnPacketBufferReader handler)
        {
            _handlers[id] = handler;
        }


        public void AnalyzePacket(byte[] data, int offset, int length)
        {
            OnPacketBufferReader bufferReader = _handlers[data[0]];

            if (bufferReader != null)
            {
                StackDataReader buffer = new StackDataReader(data.AsSpan(0, length));
                buffer.Seek(offset);

                bufferReader(ref buffer);
            }
        }

        public static void Load()
        {
            Handlers.Add(0x1B, EnterWorld);
            Handlers.Add(0x55, LoginComplete);
            Handlers.Add(0xBD, ClientVersion);
            Handlers.Add(0x03, ClientTalk);
            Handlers.Add(0x0B, Damage);
            Handlers.Add(0x11, CharacterStatus);
            Handlers.Add(0x15, FollowR);
            Handlers.Add(0x16, NewHealthbarUpdate);
            Handlers.Add(0x17, NewHealthbarUpdate);
            Handlers.Add(0x1A, UpdateItem);
            Handlers.Add(0x1C, Talk);
            Handlers.Add(0x1D, DeleteObject);
            Handlers.Add(0x20, UpdatePlayer);
            Handlers.Add(0x21, DenyWalk);
            Handlers.Add(0x22, ConfirmWalk);
            Handlers.Add(0x23, DragAnimation);
            Handlers.Add(0x24, OpenContainer);
            Handlers.Add(0x25, UpdateContainedItem);
            Handlers.Add(0x27, DenyMoveItem);
            Handlers.Add(0x28, EndDraggingItem);
            Handlers.Add(0x29, DropItemAccepted);
            Handlers.Add(0x2C, DeathScreen);
            Handlers.Add(0x2D, MobileAttributes);
            Handlers.Add(0x2E, EquipItem);
            Handlers.Add(0x2F, Swing);
            Handlers.Add(0x32, Unknown_0x32);
            Handlers.Add(0x38, Pathfinding);
            Handlers.Add(0x3A, UpdateSkills);
            Handlers.Add(0x3C, UpdateContainedItems);
            Handlers.Add(0x4E, PersonalLightLevel);
            Handlers.Add(0x4F, LightLevel);
            Handlers.Add(0x54, PlaySoundEffect);
            Handlers.Add(0x56, MapData);
            Handlers.Add(0x5B, SetTime);
            Handlers.Add(0x65, SetWeather);
            Handlers.Add(0x66, BookData);
            Handlers.Add(0x6C, TargetCursor);
            Handlers.Add(0x6D, PlayMusic);
            Handlers.Add(0x6F, SecureTrading);
            Handlers.Add(0x6E, CharacterAnimation);
            Handlers.Add(0x70, GraphicEffect);
            Handlers.Add(0x71, BulletinBoardData);
            Handlers.Add(0x72, Warmode);
            Handlers.Add(0x73, Ping);
            Handlers.Add(0x74, BuyList);
            Handlers.Add(0x77, UpdateCharacter);
            Handlers.Add(0x78, UpdateObject);
            Handlers.Add(0x7C, OpenMenu);
            Handlers.Add(0x88, OpenPaperdoll);
            Handlers.Add(0x89, CorpseEquipment);
            Handlers.Add(0x90, DisplayMap);
            Handlers.Add(0x93, OpenBook);
            Handlers.Add(0x95, DyeData);
            Handlers.Add(0x97, MovePlayer);
            Handlers.Add(0x98, UpdateName);
            Handlers.Add(0x99, MultiPlacement);
            Handlers.Add(0x9A, ASCIIPrompt);
            Handlers.Add(0x9E, SellList);
            Handlers.Add(0xA1, UpdateHitpoints);
            Handlers.Add(0xA2, UpdateMana);
            Handlers.Add(0xA3, UpdateStamina);
            Handlers.Add(0xA5, OpenUrl);
            Handlers.Add(0xA6, TipWindow);
            Handlers.Add(0xAA, AttackCharacter);
            Handlers.Add(0xAB, TextEntryDialog);
            Handlers.Add(0xAF, DisplayDeath);
            Handlers.Add(0xAE, UnicodeTalk);
            Handlers.Add(0xB0, OpenGump);
            Handlers.Add(0xB2, ChatMessage);
            Handlers.Add(0xB7, Help);
            Handlers.Add(0xB8, CharacterProfile);
            Handlers.Add(0xB9, EnableLockedFeatures);
            Handlers.Add(0xBA, DisplayQuestArrow);
            Handlers.Add(0xBB, UltimaMessengerR);
            Handlers.Add(0xBC, Season);
            Handlers.Add(0xBE, AssistVersion);
            Handlers.Add(0xBF, ExtendedCommand);
            Handlers.Add(0xC0, GraphicEffect);
            Handlers.Add(0xC1, DisplayClilocString);
            Handlers.Add(0xC2, UnicodePrompt);
            Handlers.Add(0xC4, Semivisible);
            Handlers.Add(0xC6, InvalidMapEnable);
            Handlers.Add(0xC7, GraphicEffect);
            Handlers.Add(0xC8, ClientViewRange);
            Handlers.Add(0xCA, GetUserServerPingGodClientR);
            Handlers.Add(0xCB, GlobalQueCount);
            Handlers.Add(0xCC, DisplayClilocString);
            Handlers.Add(0xD0, ConfigurationFileR);
            Handlers.Add(0xD1, Logout);
            Handlers.Add(0xD2, UpdateCharacter);
            Handlers.Add(0xD3, UpdateObject);
            Handlers.Add(0xD4, OpenBook);
            Handlers.Add(0xD6, MegaCliloc);
            Handlers.Add(0xD7, GenericAOSCommandsR);
            Handlers.Add(0xD8, CustomHouse);
            Handlers.Add(0xDB, CharacterTransferLog);
            Handlers.Add(0xDC, OPLInfo);
            Handlers.Add(0xDD, OpenCompressedGump);
            Handlers.Add(0xDE, UpdateMobileStatus);
            Handlers.Add(0xDF, BuffDebuff);
            Handlers.Add(0xE2, NewCharacterAnimation);
            Handlers.Add(0xE3, KREncryptionResponse);
            Handlers.Add(0xE5, DisplayWaypoint);
            Handlers.Add(0xE6, RemoveWaypoint);
            Handlers.Add(0xF0, KrriosClientSpecial);
            Handlers.Add(0xF1, FreeshardListR);
            Handlers.Add(0xF3, UpdateItemSA);
            Handlers.Add(0xF5, DisplayMap);
            Handlers.Add(0xF6, BoatMoving);
            Handlers.Add(0xF7, PacketList);

            // login
            Handlers.Add(0xA8, ServerListReceived);
            Handlers.Add(0x8C, ReceiveServerRelay);
            Handlers.Add(0x86, UpdateCharacterList);
            Handlers.Add(0xA9, ReceiveCharacterList);
            Handlers.Add(0x82, ReceiveLoginRejection);
            Handlers.Add(0x85, ReceiveLoginRejection);
            Handlers.Add(0x53, ReceiveLoginRejection);
        }


        public static void SendMegaClilocRequests()
        {
            if (World.ClientFeatures.TooltipsEnabled && Handlers._clilocRequests.Count != 0)
            {
                if (Client.Version >= Data.ClientVersion.CV_5090)
                {
                    if (Handlers._clilocRequests.Count != 0)
                    {
                        NetClient.Socket.Send_MegaClilocRequest(ref Handlers._clilocRequests);
                    }
                }
                else
                {
                    foreach (uint serial in Handlers._clilocRequests)
                    {
                        NetClient.Socket.Send_MegaClilocRequest_Old(serial);
                    }

                    Handlers._clilocRequests.Clear();
                }
            }
        }

        public static void AddMegaClilocRequest(uint serial)
        {
            foreach (uint s in Handlers._clilocRequests)
            {
                if (s == serial)
                {
                    return;
                }
            }

            Handlers._clilocRequests.Add(serial);
        }

        private static void TargetCursor(ref StackDataReader p)
        {
            TargetManager.SetTargeting((CursorTarget) p.ReadUInt8(), p.ReadUInt32BE(), (TargetType) p.ReadUInt8());

            if (World.Party.PartyHealTimer < Time.Ticks && World.Party.PartyHealTarget != 0)
            {
                TargetManager.Target(World.Party.PartyHealTarget);
                World.Party.PartyHealTimer = 0;
                World.Party.PartyHealTarget = 0;
            }
        }

        private static void SecureTrading(ref StackDataReader p)
        {
            if (!World.InGame)
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
                if (World.Get(id1) == null || World.Get(id2) == null)
                {
                    return;
                }

                bool hasName = p.ReadBool();
                string name = string.Empty;

                if (hasName && p.Position < p.Length)
                {
                    name = p.ReadASCII();
                }

                UIManager.Add(new TradingGump(serial, name, id1, id2));
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

        private static void ClientTalk(ref StackDataReader p)
        {
            switch (p.ReadUInt8())
            {
                case 0x78: break;

                case 0x3C: break;

                case 0x25: break;

                case 0x2E: break;
            }
        }

        private static void Damage(ref StackDataReader p)
        {
            if (World.Player == null)
            {
                return;
            }

            Entity entity = World.Get(p.ReadUInt32BE());

            if (entity != null)
            {
                ushort damage = p.ReadUInt16BE();

                if (damage > 0)
                {
                    World.WorldTextManager.AddDamage(entity, damage);
                }
            }
        }

        private static void CharacterStatus(ref StackDataReader p)
        {
            if (World.Player == null)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            Entity entity = World.Get(serial);

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

                    if (mobile == World.Player)
                    {
                        if (!string.IsNullOrEmpty(World.Player.Name) && oldName != World.Player.Name)
                        {
                            Client.Game.SetWindowTitle(World.Player.Name);
                        }

                        ushort str = p.ReadUInt16BE();
                        ushort dex = p.ReadUInt16BE();
                        ushort intell = p.ReadUInt16BE();
                        World.Player.Stamina = p.ReadUInt16BE();
                        World.Player.StaminaMax = p.ReadUInt16BE();
                        World.Player.Mana = p.ReadUInt16BE();
                        World.Player.ManaMax = p.ReadUInt16BE();
                        World.Player.Gold = p.ReadUInt32BE();
                        World.Player.PhysicalResistance = (short) p.ReadUInt16BE();
                        World.Player.Weight = p.ReadUInt16BE();


                        if (World.Player.Strength != 0 && ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.ShowStatsChangedMessage)
                        {
                            ushort currentStr = World.Player.Strength;
                            ushort currentDex = World.Player.Dexterity;
                            ushort currentInt = World.Player.Intelligence;

                            int deltaStr = str - currentStr;
                            int deltaDex = dex - currentDex;
                            int deltaInt = intell - currentInt;

                            if (deltaStr != 0)
                            {
                                GameActions.Print
                                (
                                    string.Format(ResGeneral.Your0HasChangedBy1ItIsNow2, ResGeneral.Strength, deltaStr, str),
                                    0x0170,
                                    MessageType.System,
                                    3,
                                    false
                                );
                            }

                            if (deltaDex != 0)
                            {
                                GameActions.Print
                                (
                                    string.Format(ResGeneral.Your0HasChangedBy1ItIsNow2, ResGeneral.Dexterity, deltaDex, dex),
                                    0x0170,
                                    MessageType.System,
                                    3,
                                    false
                                );
                            }

                            if (deltaInt != 0)
                            {
                                GameActions.Print
                                (
                                    string.Format(ResGeneral.Your0HasChangedBy1ItIsNow2, ResGeneral.Intelligence, deltaInt, intell),
                                    0x0170,
                                    MessageType.System,
                                    3,
                                    false
                                );
                            }
                        }

                        World.Player.Strength = str;
                        World.Player.Dexterity = dex;
                        World.Player.Intelligence = intell;

                        if (type >= 5) //ML
                        {
                            World.Player.WeightMax = p.ReadUInt16BE();
                            byte race = p.ReadUInt8();

                            if (race == 0)
                            {
                                race = 1;
                            }

                            World.Player.Race = (RaceType) race;
                        }
                        else
                        {
                            if (Client.Version >= Data.ClientVersion.CV_500A)
                            {
                                World.Player.WeightMax = (ushort) (7 * (World.Player.Strength >> 1) + 40);
                            }
                            else
                            {
                                World.Player.WeightMax = (ushort) (World.Player.Strength * 4 + 25);
                            }
                        }

                        if (type >= 3) //Renaissance
                        {
                            World.Player.StatsCap = (short) p.ReadUInt16BE();
                            World.Player.Followers = p.ReadUInt8();
                            World.Player.FollowersMax = p.ReadUInt8();
                        }

                        if (type >= 4) //AOS
                        {
                            World.Player.FireResistance = (short) p.ReadUInt16BE();
                            World.Player.ColdResistance = (short) p.ReadUInt16BE();
                            World.Player.PoisonResistance = (short) p.ReadUInt16BE();
                            World.Player.EnergyResistance = (short) p.ReadUInt16BE();
                            World.Player.Luck = p.ReadUInt16BE();
                            World.Player.DamageMin = (short) p.ReadUInt16BE();
                            World.Player.DamageMax = (short) p.ReadUInt16BE();
                            World.Player.TithingPoints = p.ReadUInt32BE();
                        }

                        if (type >= 6)
                        {
                            World.Player.MaxPhysicResistence = p.Position + 2 > p.Length ? (short) 0 : (short) p.ReadUInt16BE();

                            World.Player.MaxFireResistence = p.Position + 2 > p.Length ? (short) 0 : (short) p.ReadUInt16BE();

                            World.Player.MaxColdResistence = p.Position + 2 > p.Length ? (short) 0 : (short) p.ReadUInt16BE();

                            World.Player.MaxPoisonResistence = p.Position + 2 > p.Length ? (short) 0 : (short) p.ReadUInt16BE();

                            World.Player.MaxEnergyResistence = p.Position + 2 > p.Length ? (short) 0 : (short) p.ReadUInt16BE();

                            World.Player.DefenseChanceIncrease = p.Position + 2 > p.Length ? (short) 0 : (short) p.ReadUInt16BE();

                            World.Player.MaxDefenseChanceIncrease = p.Position + 2 > p.Length ? (short) 0 : (short) p.ReadUInt16BE();

                            World.Player.HitChanceIncrease = p.Position + 2 > p.Length ? (short) 0 : (short) p.ReadUInt16BE();

                            World.Player.SwingSpeedIncrease = p.Position + 2 > p.Length ? (short) 0 : (short) p.ReadUInt16BE();

                            World.Player.DamageIncrease = p.Position + 2 > p.Length ? (short) 0 : (short) p.ReadUInt16BE();

                            World.Player.LowerReagentCost = p.Position + 2 > p.Length ? (short) 0 : (short) p.ReadUInt16BE();

                            World.Player.SpellDamageIncrease = p.Position + 2 > p.Length ? (short) 0 : (short) p.ReadUInt16BE();

                            World.Player.FasterCastRecovery = p.Position + 2 > p.Length ? (short) 0 : (short) p.ReadUInt16BE();

                            World.Player.FasterCasting = p.Position + 2 > p.Length ? (short) 0 : (short) p.ReadUInt16BE();
                            World.Player.LowerManaCost = p.Position + 2 > p.Length ? (short) 0 : (short) p.ReadUInt16BE();
                        }
                    }
                }

                if (mobile == World.Player)
                {
                    UoAssist.SignalHits();
                    UoAssist.SignalStamina();
                    UoAssist.SignalMana();
                }
            }
        }

        private static void FollowR(ref StackDataReader p)
        {
            uint tofollow = p.ReadUInt32BE();
            uint isfollowing = p.ReadUInt32BE();
        }

        private static void NewHealthbarUpdate(ref StackDataReader p)
        {
            if (World.Player == null)
            {
                return;
            }

            if (p[0] == 0x16 && Client.Version < Data.ClientVersion.CV_500A)
            {
                return;
            }

            Mobile mobile = World.Mobiles.Get(p.ReadUInt32BE());

            if (mobile == null)
            {
                return;
            }

            ushort count = p.ReadUInt16BE();

            for (int i = 0; i < count; i++)
            {
                ushort type = p.ReadUInt16BE();
                bool enabled = p.ReadBool();
                byte flags = (byte) mobile.Flags;

                if (type == 1)
                {
                    if (enabled)
                    {
                        if (Client.Version >= Data.ClientVersion.CV_7000)
                        {
                            mobile.SetSAPoison(true);
                        }
                        else
                        {
                            flags |= 0x04;
                        }
                    }
                    else
                    {
                        if (Client.Version >= Data.ClientVersion.CV_7000)
                        {
                            mobile.SetSAPoison(false);
                        }
                        else
                        {
                            flags = (byte) (flags & ~0x04);
                        }
                    }
                }
                else if (type == 2)
                {
                    if (enabled)
                    {
                        flags |= 0x08;
                    }
                    else
                    {
                        flags &= (byte) (flags & ~0x08);
                    }
                }
                else if (type == 3)
                {
                }

                mobile.Flags = (Flags) flags;
            }
        }

        private static void UpdateItem(ref StackDataReader p)
        {
            if (World.Player == null)
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

            UpdateGameObject
            (
                serial,
                graphic,
                graphicInc,
                count,
                x,
                y,
                z,
                (Direction) direction,
                hue,
                (Flags) flags,
                count,
                type,
                1
            );
        }

        private static void EnterWorld(ref StackDataReader p)
        {
            if (ProfileManager.CurrentProfile == null)
            {
                ProfileManager.Load(World.ServerName, LoginScene.Account, Settings.GlobalSettings.LastCharacterName.Trim());
            }

            if (World.Player != null)
            {
                World.Clear();
            }

            World.Mobiles.Add(World.Player = new PlayerMobile(p.ReadUInt32BE()));
            p.Skip(4);
            World.Player.Graphic = p.ReadUInt16BE();
            World.Player.CheckGraphicChange();
            ushort x = p.ReadUInt16BE();
            ushort y = p.ReadUInt16BE();
            sbyte z = (sbyte) p.ReadUInt16BE();

            if (World.Map == null)
            {
                World.MapIndex = 0;
            }

            Direction direction = (Direction) (p.ReadUInt8() & 0x7);

            World.Player.X = x;
            World.Player.Y = y;
            World.Player.Z = z;
            World.Player.UpdateScreenPosition();
            World.Player.Direction = direction;
            World.Player.AddToTile();

            World.RangeSize.X = x;
            World.RangeSize.Y = y;

            if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.UseCustomLightLevel)
            {
                World.Light.Overall = ProfileManager.CurrentProfile.LightLevel;
            }

            Client.Game.Scene.Audio.UpdateCurrentMusicVolume();

            if (Client.Version >= Data.ClientVersion.CV_200)
            {
                if (ProfileManager.CurrentProfile != null)
                {
                    NetClient.Socket.Send_GameWindowSize((uint)ProfileManager.CurrentProfile.GameWindowSize.X, (uint)ProfileManager.CurrentProfile.GameWindowSize.Y);
                }

                NetClient.Socket.Send_Language(Settings.GlobalSettings.Language);
            }

            NetClient.Socket.Send_ClientVersion(Settings.GlobalSettings.ClientVersion);

            GameActions.SingleClick(World.Player);
            NetClient.Socket.Send_SkillsRequest(World.Player.Serial);

            if (World.Player.IsDead)
            {
                World.ChangeSeason(Game.Managers.Season.Desolation, 42);
            }

            if (Client.Version >= Data.ClientVersion.CV_70796 && ProfileManager.CurrentProfile != null)
            {
                NetClient.Socket.Send_ShowPublicHouseContent(ProfileManager.CurrentProfile.ShowHouseContent);
            }


            NetClient.Socket.Send_ToPlugins_AllSkills();
            NetClient.Socket.Send_ToPlugins_AllSpells();
        }

        private static void Talk(ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();
            Entity entity = World.Get(serial);
            ushort graphic = p.ReadUInt16BE();
            MessageType type = (MessageType) p.ReadUInt8();
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

            if (serial == 0 && graphic == 0 && type == MessageType.Regular && font == 0xFFFF && hue == 0xFFFF && name.StartsWith("SYSTEM"))
            {
                NetClient.Socket.Send_ACKTalk();

                return;
            }

            TextType text_type = TextType.SYSTEM;

            if (type == MessageType.System || serial == 0xFFFF_FFFF || serial == 0 || name.ToLower() == "system" && entity == null)
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


            MessageManager.HandleMessage
            (
                entity,
                text,
                name,
                hue,
                type,
                (byte) font,
                text_type
            );
        }

        private static void DeleteObject(ref StackDataReader p)
        {
            if (World.Player == null)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();

            if (World.Player == serial)
            {
                return;
            }

            Entity entity = World.Get(serial);

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
                    Entity top = World.Get(it.RootContainer);

                    if (top != null)
                    {
                        if (top == World.Player)
                        {
                            updateAbilities = it.Layer == Layer.OneHanded || it.Layer == Layer.TwoHanded;
                            Item tradeBoxItem = World.Player.GetSecureTradeBox();

                            if (tradeBoxItem != null)
                            {
                                UIManager.GetTradingGump(tradeBoxItem)?.RequestUpdateContents();
                            }
                        }
                    }

                    if (cont == World.Player && it.Layer == Layer.Invalid)
                    {
                        ItemHold.Enabled = false;
                    }

                    if (it.Layer != Layer.Invalid)
                    {
                        UIManager.GetGump<PaperDollGump>(cont)?.RequestUpdateContents();
                    }

                    UIManager.GetGump<ContainerGump>(cont)?.RequestUpdateContents();

                    if (top != null && top.Graphic == 0x2006 && (ProfileManager.CurrentProfile.GridLootType == 1 || ProfileManager.CurrentProfile.GridLootType == 2))
                    {
                        UIManager.GetGump<GridLootGump>(cont)?.RequestUpdateContents();
                    }

                    if (it.Graphic == 0x0EB0)
                    {
                        UIManager.GetGump<BulletinBoardItem>(serial)?.Dispose();

                        BulletinBoardGump bbgump = UIManager.GetGump<BulletinBoardGump>();

                        if (bbgump != null)
                        {
                            bbgump.RemoveBulletinObject(serial);
                        }
                    }
                }
            }

            if (World.CorpseManager.Exists(0, serial))
            {
                return;
            }

            if (entity is Mobile m)
            {
                if (World.Party.Contains(serial))
                {
                    // m.RemoveFromTile();
                }

                // else
                {
                    //BaseHealthBarGump bar = UIManager.GetGump<BaseHealthBarGump>(serial);

                    //if (bar == null)
                    //{
                    //    NetClient.Socket.Send(new PCloseStatusBarGump(serial));
                    //}

                    World.RemoveMobile(serial, true);
                }
            }
            else
            {
                Item item = (Item) entity;

                if (item.IsMulti)
                {
                    World.HouseManager.Remove(serial);
                }

                Entity cont = World.Get(item.Container);

                if (cont != null)
                {
                    cont.Remove(item);

                    if (item.Layer != Layer.Invalid)
                    {
                        UIManager.GetGump<PaperDollGump>(cont)?.RequestUpdateContents();
                    }
                }
                else if (item.IsMulti)
                {
                    UIManager.GetGump<MiniMapGump>()?.RequestUpdateContents();
                }

                World.RemoveItem(serial, true);

                if (updateAbilities)
                {
                    World.Player.UpdateAbilities();
                }
            }
        }

        private static void UpdatePlayer(ref StackDataReader p)
        {
            if (World.Player == null)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            ushort graphic = p.ReadUInt16BE();
            byte graphic_inc = p.ReadUInt8();
            ushort hue = p.ReadUInt16BE();
            Flags flags = (Flags) p.ReadUInt8();
            ushort x = p.ReadUInt16BE();
            ushort y = p.ReadUInt16BE();
            ushort serverID = p.ReadUInt16BE();
            Direction direction = (Direction) p.ReadUInt8();
            sbyte z = p.ReadInt8();

            UpdatePlayer
            (
                serial,
                graphic,
                graphic_inc,
                hue,
                flags,
                x,
                y,
                z,
                serverID,
                direction
            );
        }

        private static void DenyWalk(ref StackDataReader p)
        {
            if (World.Player == null)
            {
                return;
            }

            byte seq = p.ReadUInt8();
            ushort x = p.ReadUInt16BE();
            ushort y = p.ReadUInt16BE();
            Direction direction = (Direction) p.ReadUInt8();
            direction &= Direction.Up;
            sbyte z = p.ReadInt8();

            World.Player.Walker.DenyWalk(seq, x, y, z);
            World.Player.Direction = direction;

            Client.Game.GetScene<GameScene>()?.Weather?.Reset();
        }

        private static void ConfirmWalk(ref StackDataReader p)
        {
            if (World.Player == null)
            {
                return;
            }

            byte seq = p.ReadUInt8();
            byte noto = (byte) (p.ReadUInt8() & ~0x40);

            if (noto == 0 || noto >= 8)
            {
                noto = 0x01;
            }

            World.Player.NotorietyFlag = (NotorietyFlag) noto;
            World.Player.Walker.ConfirmWalk(seq);

            World.Player.AddToTile();
        }

        private static void DragAnimation(ref StackDataReader p)
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

            Mobile entity = World.Mobiles.Get(source);

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

            Mobile destEntity = World.Mobiles.Get(dest);

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

            World.SpawnEffect
            (
                !SerialHelper.IsValid(source) || !SerialHelper.IsValid(dest) ? GraphicEffectType.Moving : GraphicEffectType.DragEffect,
                source,
                dest,
                graphic,
                hue,
                sourceX, sourceY, sourceZ,
                destX, destY, destZ,
                5, 5000,
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

        private static void OpenContainer(ref StackDataReader p)
        {
            if (World.Player == null)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            ushort graphic = p.ReadUInt16BE();


            if (graphic == 0xFFFF)
            {
                Item spellBookItem = World.Items.Get(serial);

                if (spellBookItem == null)
                {
                    return;
                }

                UIManager.GetGump<SpellbookGump>(serial)?.Dispose();

                SpellbookGump spellbookGump = new SpellbookGump(spellBookItem);

                if (!UIManager.GetGumpCachePosition(spellBookItem, out Point location))
                {
                    location = new Point(64, 64);
                }

                spellbookGump.Location = location;
                UIManager.Add(spellbookGump);

                Client.Game.Scene.Audio.PlaySound(0x0055);
            }
            else if (graphic == 0x0030)
            {
                Mobile vendor = World.Mobiles.Get(serial);

                if (vendor == null)
                {
                    return;
                }

                UIManager.GetGump<ShopGump>(serial)?.Dispose();

                ShopGump gump = new ShopGump(serial, true, 150, 5);
                UIManager.Add(gump);

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
                        Item it = (Item) first;

                        gump.AddItem
                        (
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
                Item item = World.Items.Get(serial);

                if (item != null)
                {
                    if (item.IsCorpse && (ProfileManager.CurrentProfile.GridLootType == 1 || ProfileManager.CurrentProfile.GridLootType == 2))
                    {
                        //UIManager.GetGump<GridLootGump>(serial)?.Dispose();
                        //UIManager.Add(new GridLootGump(serial));
                        _requestedGridLoot = serial;

                        if (ProfileManager.CurrentProfile.GridLootType == 1)
                        {
                            return;
                        }
                    }

                    ContainerGump container = UIManager.GetGump<ContainerGump>(serial);
                    bool playsound = false;
                    int x, y;

                    // TODO: check client version ?
                    if (Client.Version >= Data.ClientVersion.CV_706000 && ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.UseLargeContainerGumps)
                    {
                        GumpsLoader loader = GumpsLoader.Instance;

                        switch (graphic)
                        {
                            case 0x0048:
                                if (loader.GetTexture(0x06E8) != null)
                                {
                                    graphic = 0x06E8;
                                }

                                break;

                            case 0x0049:
                                if (loader.GetTexture(0x9CDF) != null)
                                {
                                    graphic = 0x9CDF;
                                }

                                break;

                            case 0x0051:
                                if (loader.GetTexture(0x06E7) != null)
                                {
                                    graphic = 0x06E7;
                                }

                                break;

                            case 0x003E:
                                if (loader.GetTexture(0x06E9) != null)
                                {
                                    graphic = 0x06E9;
                                }

                                break;

                            case 0x004D:
                                if (loader.GetTexture(0x06EA) != null)
                                {
                                    graphic = 0x06EA;
                                }

                                break;

                            case 0x004E:
                                if (loader.GetTexture(0x06E6) != null)
                                {
                                    graphic = 0x06E6;
                                }

                                break;

                            case 0x004F:
                                if (loader.GetTexture(0x06E5) != null)
                                {
                                    graphic = 0x06E5;
                                }

                                break;

                            case 0x004A:
                                if (loader.GetTexture(0x9CDD) != null)
                                {
                                    graphic = 0x9CDD;
                                }

                                break;

                            case 0x0044:
                                if (loader.GetTexture(0x9CE3) != null)
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
                        ContainerManager.CalculateContainerPosition(serial, graphic);
                        x = ContainerManager.X;
                        y = ContainerManager.Y;
                        playsound = true;
                    }


                    UIManager.Add
                    (
                        new ContainerGump(item, graphic, playsound)
                        {
                            X = x,
                            Y = y,
                            InvalidateContents = true
                        }
                    );

                    UIManager.RemovePosition(serial);
                }
                else
                {
                    Log.Error("[OpenContainer]: item not found");
                }
            }


            if (graphic != 0x0030)
            {
                Item it = World.Items.Get(serial);

                if (it != null)
                {
                    it.Opened = true;

                    if (!it.IsCorpse && graphic != 0xFFFF)
                    {
                        ClearContainerAndRemoveItems(it);
                    }
                }
            }
        }

        private static void UpdateContainedItem(ref StackDataReader p)
        {
            if (!World.InGame)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            ushort graphic = (ushort) (p.ReadUInt16BE() + p.ReadUInt8());
            ushort amount = Math.Max((ushort) 1, p.ReadUInt16BE());
            ushort x = p.ReadUInt16BE();
            ushort y = p.ReadUInt16BE();

            if (Client.Version >= Data.ClientVersion.CV_6017)
            {
                p.Skip(1);
            }

            uint containerSerial = p.ReadUInt32BE();
            ushort hue = p.ReadUInt16BE();

            AddItemToContainer
            (
                serial,
                graphic,
                amount,
                x,
                y,
                hue,
                containerSerial
            );
        }

        private static void DenyMoveItem(ref StackDataReader p)
        {
            if (!World.InGame)
            {
                return;
            }

            Item firstItem = World.Items.Get(ItemHold.Serial);

            if (ItemHold.Enabled || ItemHold.Dropped && (firstItem == null || !firstItem.AllowedToDraw))
            {
                if (World.ObjectToRemove == ItemHold.Serial)
                {
                    World.ObjectToRemove = 0;
                }

                if (SerialHelper.IsValid(ItemHold.Serial) && ItemHold.Graphic != 0xFFFF)
                {
                    if (!ItemHold.UpdatedInWorld)
                    {
                        if (ItemHold.Layer == Layer.Invalid && SerialHelper.IsValid(ItemHold.Container))
                        {
                            // Server should send an UpdateContainedItem after this packet.
                            Console.WriteLine("=== DENY === ADD TO CONTAINER");

                            AddItemToContainer
                            (
                                ItemHold.Serial,
                                ItemHold.Graphic,
                                ItemHold.TotalAmount,
                                ItemHold.X,
                                ItemHold.Y,
                                ItemHold.Hue,
                                ItemHold.Container
                            );

                            UIManager.GetGump<ContainerGump>(ItemHold.Container)?.RequestUpdateContents();
                        }
                        else
                        {
                            Item item = World.GetOrCreateItem(ItemHold.Serial);

                            item.Graphic = ItemHold.Graphic;
                            item.Hue = ItemHold.Hue;
                            item.Amount = ItemHold.TotalAmount;
                            item.Flags = ItemHold.Flags;
                            item.Layer = ItemHold.Layer;
                            item.X = ItemHold.X;
                            item.Y = ItemHold.Y;
                            item.Z = ItemHold.Z;
                            item.CheckGraphicChange();

                            Entity container = World.Get(ItemHold.Container);

                            if (container != null)
                            {
                                if (SerialHelper.IsMobile(container.Serial))
                                {
                                    Console.WriteLine("=== DENY === ADD TO PAPERDOLL");

                                    World.RemoveItemFromContainer(item);
                                    container.PushToBack(item);
                                    item.Container = container.Serial;

                                    UIManager.GetGump<PaperDollGump>(item.Container)?.RequestUpdateContents();
                                }
                                else
                                {
                                    Console.WriteLine("=== DENY === SOMETHING WRONG");

                                    World.RemoveItem(item, true);
                                }
                            }
                            else
                            {
                                Console.WriteLine("=== DENY === ADD TO TERRAIN");

                                World.RemoveItemFromContainer(item);
                                item.AddToTile();
                                item.UpdateScreenPosition();
                            }
                        }
                    }
                }
                else
                {
                    Log.Error($"Wrong data: serial = {ItemHold.Serial:X8}  -  graphic = {ItemHold.Graphic:X4}");
                }

                UIManager.GetGump<SplitMenuGump>(ItemHold.Serial)?.Dispose();

                ItemHold.Clear();
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
                MessageManager.HandleMessage
                (
                    null,
                    ServerErrorMessages.GetError(p[0], code),
                    string.Empty,
                    0x03b2,
                    MessageType.System,
                    3,
                    TextType.SYSTEM
                );
            }
        }

        private static void EndDraggingItem(ref StackDataReader p)
        {
            if (!World.InGame)
            {
                return;
            }

            ItemHold.Enabled = false;
            ItemHold.Dropped = false;
        }

        private static void DropItemAccepted(ref StackDataReader p)
        {
            if (!World.InGame)
            {
                return;
            }

            ItemHold.Enabled = false;
            ItemHold.Dropped = false;

            Console.WriteLine("PACKET - ITEM DROP OK!");
        }

        private static void DeathScreen(ref StackDataReader p)
        {
            // todo
            byte action = p.ReadUInt8();

            if (action != 1)
            {
                Client.Game.GetScene<GameScene>()?.Weather?.Reset();

                Client.Game.Scene.Audio.PlayMusic(Client.Game.Scene.Audio.DeathMusicIndex, true);

                if (ProfileManager.CurrentProfile.EnableDeathScreen)
                {
                    World.Player.DeathScreenTimer = Time.Ticks + Constants.DEATH_SCREEN_TIMER;
                }

                GameActions.RequestWarMode(false);
            }
        }

        private static void MobileAttributes(ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();

            Entity entity = World.Get(serial);

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

                if (mobile == World.Player)
                {
                    UoAssist.SignalHits();
                    UoAssist.SignalStamina();
                    UoAssist.SignalMana();
                }
            }
        }

        private static void EquipItem(ref StackDataReader p)
        {
            if (!World.InGame)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();

            Item item = World.GetOrCreateItem(serial);

            if (item.Graphic != 0 && item.Layer != Layer.Backpack)
            {
                //ClearContainerAndRemoveItems(item);
                World.RemoveItemFromContainer(item);
            }

            if (SerialHelper.IsValid(item.Container))
            {
                UIManager.GetGump<ContainerGump>(item.Container)?.RequestUpdateContents();

                UIManager.GetGump<PaperDollGump>(item.Container)?.RequestUpdateContents();
            }

            item.Graphic = (ushort) (p.ReadUInt16BE() + p.ReadInt8());
            item.Layer = (Layer) p.ReadUInt8();
            item.Container = p.ReadUInt32BE();
            item.FixHue(p.ReadUInt16BE());
            item.Amount = 1;

            Entity entity = World.Get(item.Container);

            entity?.PushToBack(item);

            if (item.Layer >= Layer.ShopBuyRestock && item.Layer <= Layer.ShopSell)
            {
                //item.Clear();
            }
            else if (SerialHelper.IsValid(item.Container) && item.Layer < Layer.Mount)
            {
                UIManager.GetGump<PaperDollGump>(item.Container)?.RequestUpdateContents();
            }

            if (entity == World.Player && (item.Layer == Layer.OneHanded || item.Layer == Layer.TwoHanded))
            {
                World.Player?.UpdateAbilities();
            }

            //if (ItemHold.Serial == item.Serial)
            //{
            //    Console.WriteLine("PACKET - ITEM EQUIP");
            //    ItemHold.Enabled = false;
            //    ItemHold.Dropped = true;
            //}
        }

        private static void Swing(ref StackDataReader p)
        {
            if (!World.InGame)
            {
                return;
            }

            p.Skip(1);

            uint attackers = p.ReadUInt32BE();

            if (attackers != World.Player)
            {
                return;
            }

            uint defenders = p.ReadUInt32BE();

            const int TIME_TURN_TO_LASTTARGET = 2000;

            if (TargetManager.LastAttack == defenders && World.Player.InWarMode && World.Player.Walker.LastStepRequestTime + TIME_TURN_TO_LASTTARGET < Time.Ticks && World.Player.Steps.Count == 0)
            {
                Mobile enemy = World.Mobiles.Get(defenders);

                if (enemy != null)
                {
                    Direction pdir = DirectionHelper.GetDirectionAB(World.Player.X, World.Player.Y, enemy.X, enemy.Y);

                    int x = World.Player.X;
                    int y = World.Player.Y;
                    sbyte z = World.Player.Z;

                    if (Pathfinder.CanWalk(ref pdir, ref x, ref y, ref z) && World.Player.Direction != pdir)
                    {
                        World.Player.Walk(pdir, false);
                    }
                }
            }
        }

        private static void Unknown_0x32(ref StackDataReader p)
        {
        }

        private static void UpdateSkills(ref StackDataReader p)
        {
            if (!World.InGame)
            {
                return;
            }

            byte type = p.ReadUInt8();
            bool haveCap = type != 0u && type <= 0x03 || type == 0xDF;
            bool isSingleUpdate = type == 0xFF || type == 0xDF;

            if (type == 0xFE)
            {
                int count = p.ReadUInt16BE();

                SkillsLoader.Instance.Skills.Clear();
                SkillsLoader.Instance.SortedSkills.Clear();

                for (int i = 0; i < count; i++)
                {
                    bool haveButton = p.ReadBool();
                    int nameLength = p.ReadUInt8();

                    SkillsLoader.Instance.Skills.Add(new SkillEntry(i, p.ReadASCII(nameLength), haveButton));
                }

                SkillsLoader.Instance.SortedSkills.AddRange(SkillsLoader.Instance.Skills);

                SkillsLoader.Instance.SortedSkills.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.InvariantCulture));
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

                if (!isSingleUpdate && (type == 1 || type == 3 || World.SkillsRequested))
                {
                    World.SkillsRequested = false;

                    // TODO: make a base class for this gump
                    if (ProfileManager.CurrentProfile.StandardSkillsGump)
                    {
                        if (standard == null)
                        {
                            UIManager.Add
                            (
                                standard = new StandardSkillsGump
                                {
                                    X = 100,
                                    Y = 100
                                }
                            );
                        }
                    }
                    else
                    {
                        if (advanced == null)
                        {
                            UIManager.Add
                            (
                                advanced = new SkillGumpAdvanced
                                {
                                    X = 100,
                                    Y = 100
                                }
                            );
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
                    Lock locked = (Lock) p.ReadUInt8();
                    ushort cap = 1000;

                    if (haveCap)
                    {
                        cap = p.ReadUInt16BE();
                    }

                    if (id < World.Player.Skills.Length)
                    {
                        Skill skill = World.Player.Skills[id];

                        if (skill != null)
                        {
                            if (isSingleUpdate)
                            {
                                float change = realVal / 10.0f - skill.Value;

                                if (change != 0.0f && !float.IsNaN(change) && ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.ShowSkillsChangedMessage && Math.Abs(change) >= ProfileManager.CurrentProfile.ShowSkillsChangedDeltaValue)
                                {
                                    GameActions.Print
                                    (
                                        string.Format
                                        (
                                            ResGeneral.YourSkillIn0Has1By2ItIsNow3,
                                            skill.Name,
                                            change < 0 ? ResGeneral.Decreased : ResGeneral.Increased,
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

        private static void Pathfinding(ref StackDataReader p)
        {
            if (!World.InGame)
            {
                return;
            }

            ushort x = p.ReadUInt16BE();
            ushort y = p.ReadUInt16BE();
            ushort z = p.ReadUInt16BE();

            Pathfinder.WalkTo(x, y, z, 0);
        }

        private static void UpdateContainedItems(ref StackDataReader p)
        {
            if (!World.InGame)
            {
                return;
            }

            ushort count = p.ReadUInt16BE();

            for (int i = 0; i < count; i++)
            {
                uint serial = p.ReadUInt32BE();
                ushort graphic = (ushort) (p.ReadUInt16BE() + p.ReadUInt8());
                ushort amount = Math.Max(p.ReadUInt16BE(), (ushort) 1);
                ushort x = p.ReadUInt16BE();
                ushort y = p.ReadUInt16BE();

                if (Client.Version >= Data.ClientVersion.CV_6017)
                {
                    p.Skip(1);
                }

                uint containerSerial = p.ReadUInt32BE();
                ushort hue = p.ReadUInt16BE();

                if (i == 0)
                {
                    Entity container = World.Get(containerSerial);

                    if (container != null)
                    {
                        ClearContainerAndRemoveItems(container, container.Graphic == 0x2006);
                    }
                }

                AddItemToContainer
                (
                    serial,
                    graphic,
                    amount,
                    x,
                    y,
                    hue,
                    containerSerial
                );
            }
        }

        private static void PersonalLightLevel(ref StackDataReader p)
        {
            if (!World.InGame)
            {
                return;
            }

            if (World.Player == p.ReadUInt32BE())
            {
                byte level = p.ReadUInt8();

                if (level > 0x1E)
                {
                    level = 0x1E;
                }

                World.Light.RealPersonal = level;

                if (!ProfileManager.CurrentProfile.UseCustomLightLevel)
                {
                    World.Light.Personal = level;
                }
            }
        }

        private static void LightLevel(ref StackDataReader p)
        {
            if (!World.InGame)
            {
                return;
            }

            byte level = p.ReadUInt8();

            if (level > 0x1E)
            {
                level = 0x1E;
            }

            World.Light.RealOverall = level;

            if (!ProfileManager.CurrentProfile.UseCustomLightLevel)
            {
                World.Light.Overall = level;
            }
        }

        private static void PlaySoundEffect(ref StackDataReader p)
        {
            if (World.Player == null)
            {
                return;
            }

            p.Skip(1);

            ushort index = p.ReadUInt16BE();
            ushort audio = p.ReadUInt16BE();
            ushort x = p.ReadUInt16BE();
            ushort y = p.ReadUInt16BE();
            short z = (short) p.ReadUInt16BE();

            Client.Game.Scene.Audio.PlaySoundWithDistance(index, x, y);
        }

        private static void PlayMusic(ref StackDataReader p)
        {
            ushort index = p.ReadUInt16BE();

            Client.Game.Scene.Audio.PlayMusic(index);
        }

        private static void LoginComplete(ref StackDataReader p)
        {
            if (World.Player != null && Client.Game.Scene is LoginScene)
            {
                GameScene scene = new GameScene();
                scene.Audio = Client.Game.Scene.Audio;
                Client.Game.Scene.Audio = null;
                Client.Game.SetScene(scene);

                //GameActions.OpenPaperdoll(World.Player);
                GameActions.RequestMobileStatus(World.Player);
                NetClient.Socket.Send_OpenChat("");


                //NetClient.Socket.Send(new PSkillsRequest(World.Player));
                scene.DoubleClickDelayed(World.Player);

                if (Client.Version >= Data.ClientVersion.CV_306E)
                {
                    NetClient.Socket.Send_ClientType();
                }

                if (Client.Version >= Data.ClientVersion.CV_305D)
                {
                    NetClient.Socket.Send_ClientViewRange(World.ClientViewRange);
                }

                List<Gump> gumps = ProfileManager.CurrentProfile.ReadGumps(ProfileManager.ProfilePath);

                if (gumps != null)
                {
                    foreach (Gump gump in gumps)
                    {
                        UIManager.Add(gump);
                    }
                }
            }
        }

        private static void MapData(ref StackDataReader p)
        {
            if (!World.InGame)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();

            MapGump gump = UIManager.GetGump<MapGump>(serial);

            if (gump != null)
            {
                switch ((MapMessageType) p.ReadUInt8())
                {
                    case MapMessageType.Add:
                        p.Skip(1);

                        ushort x = p.ReadUInt16BE();
                        ushort y = p.ReadUInt16BE();

                        gump.AddPin(x, y);

                        break;

                    case MapMessageType.Insert: break;
                    case MapMessageType.Move: break;
                    case MapMessageType.Remove: break;

                    case MapMessageType.Clear:
                        gump.ClearContainer();

                        break;

                    case MapMessageType.Edit: break;

                    case MapMessageType.EditResponse:
                        gump.SetPlotState(p.ReadUInt8());

                        break;
                }
            }
        }

        private static void SetTime(ref StackDataReader p)
        {
        }

        private static void SetWeather(ref StackDataReader p)
        {
            GameScene scene = Client.Game.GetScene<GameScene>();

            if (scene == null)
            {
                return;
            }

            Weather weather = scene.Weather;
            byte type = p.ReadUInt8();

            if (weather.CurrentWeather != type)
            {
                weather.Reset();

                weather.Type = type;
                weather.Count = p.ReadUInt8();

                bool showMessage = weather.Count > 0;

                if (weather.Count > 70)
                {
                    weather.Count = 70;
                }

                weather.Temperature = p.ReadUInt8();
                weather.Timer = Time.Ticks + Constants.WEATHER_TIMER;
                weather.Generate();

                switch (type)
                {
                    case 0:
                        if (showMessage)
                        {
                            GameActions.Print
                            (
                                ResGeneral.ItBeginsToRain,
                                1154,
                                MessageType.System,
                                3,
                                false
                            );

                            weather.CurrentWeather = 0;
                        }

                        break;

                    case 1:
                        if (showMessage)
                        {
                            GameActions.Print
                            (
                                ResGeneral.AFierceStormApproaches,
                                1154,
                                MessageType.System,
                                3,
                                false
                            );

                            weather.CurrentWeather = 1;
                        }

                        break;

                    case 2:
                        if (showMessage)
                        {
                            GameActions.Print
                            (
                                ResGeneral.ItBeginsToSnow,
                                1154,
                                MessageType.System,
                                3,
                                false
                            );

                            weather.CurrentWeather = 2;
                        }

                        break;

                    case 3:
                        if (showMessage)
                        {
                            GameActions.Print
                            (
                                ResGeneral.AStormIsBrewing,
                                1154,
                                MessageType.System,
                                3,
                                false
                            );

                            weather.CurrentWeather = 3;
                        }

                        break;

                    case 0xFE:
                    case 0xFF:
                        weather.Timer = 0;
                        weather.CurrentWeather = null;

                        break;
                }
            }
        }

        private static void BookData(ref StackDataReader p)
        {
            if (!World.InGame)
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
                            gump.BookLines[index] = ModernBookGump.IsNewBook ? p.ReadUTF8(true) : p.ReadASCII();
                        }
                        else
                        {
                            Log.Error("BOOKGUMP: The server is sending a page number GREATER than the allowed number of pages in BOOK!");
                        }
                    }

                    if (lineCnt < ModernBookGump.MAX_BOOK_LINES)
                    {
                        for (int line = lineCnt; line < ModernBookGump.MAX_BOOK_LINES; line++)
                        {
                            gump.BookLines[pageNum * ModernBookGump.MAX_BOOK_LINES + line] = string.Empty;
                        }
                    }
                }
                else
                {
                    Log.Error("BOOKGUMP: The server is sending a page number GREATER than the allowed number of pages in BOOK!");
                }
            }

            gump.ServerSetBookText();
        }

        private static void CharacterAnimation(ref StackDataReader p)
        {
            Mobile mobile = World.Mobiles.Get(p.ReadUInt32BE());

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

            mobile.SetAnimation
            (
                Mobile.GetReplacedObjectAnimation(mobile.Graphic, action),
                delay,
                (byte) frame_count,
                (byte) repeat_count,
                repeat,
                forward
            );

            mobile.AnimationFromServer = true;
        }

        private static void GraphicEffect(ref StackDataReader p)
        {
            if (World.Player == null)
            {
                return;
            }

            GraphicEffectType type = (GraphicEffectType) p.ReadUInt8();

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
            ushort duration = p.ReadUInt8();
            p.Skip(2);
            bool fixedDirection = p.ReadBool();
            bool doesExplode = p.ReadBool();
            ushort hue = 0;
            GraphicEffectBlendMode blendmode = 0;

            if (p[0] == 0x70)
            {
                if (speed > 20)
                {
                    speed = (byte)(speed - 20);
                }

                speed = (byte)(20 - speed);
            }
            else
            {
                hue = (ushort)p.ReadUInt32BE();
                blendmode = (GraphicEffectBlendMode)(p.ReadUInt32BE() % 7);

                if (speed > 7)
                {
                    speed = 7;
                }
            }

            World.SpawnEffect
            (
                type,
                source,
                target,
                graphic,
                hue,
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

        private static void ClientViewRange(ref StackDataReader p)
        {
            World.ClientViewRange = p.ReadUInt8();
        }

        private static void BulletinBoardData(ref StackDataReader p)
        {
            if (!World.InGame)
            {
                return;
            }

            switch (p.ReadUInt8())
            {
                case 0: // open

                {
                    uint serial = p.ReadUInt32BE();
                    Item item = World.Items.Get(serial);

                    if (item != null)
                    {
                        BulletinBoardGump bulletinBoard = UIManager.GetGump<BulletinBoardGump>(serial);
                        bulletinBoard?.Dispose();

                        int x = (Client.Game.Window.ClientBounds.Width >> 1) - 245;
                        int y = (Client.Game.Window.ClientBounds.Height >> 1) - 205;

                        bulletinBoard = new BulletinBoardGump(item, x, y, p.ReadUTF8(22, true)); //p.ReadASCII(22));
                        UIManager.Add(bulletinBoard);

                        item.Opened = true;
                    }
                }

                    break;

                case 1: // summary msg

                {
                    uint boardSerial = p.ReadUInt32BE();
                    BulletinBoardGump bulletinBoard = UIManager.GetGump<BulletinBoardGump>(boardSerial);

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
                    BulletinBoardGump bulletinBoard = UIManager.GetGump<BulletinBoardGump>(boardSerial);

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
                        byte variant = (byte)(1 + (poster == World.Player.Name ? 1 : 0));

                        UIManager.Add
                        (
                            new BulletinBoardItem
                                (
                                    boardSerial,
                                    serial,
                                    poster,
                                    subject,
                                    dataTime,
                                    msg.TrimStart(),
                                    variant
                                )
                                { X = 40, Y = 40 }
                        );

                        sb.Dispose();
                    }
                }

                    break;
            }
        }

        private static void Warmode(ref StackDataReader p)
        {
            if (!World.InGame)
            {
                return;
            }

            World.Player.InWarMode = p.ReadBool();
        }

        private static void Ping(ref StackDataReader p)
        {
            if (NetClient.Socket.IsConnected && !NetClient.Socket.IsDisposed)
            {
                NetClient.Socket.Statistics.PingReceived();
            }
            else if (NetClient.LoginSocket.IsConnected && !NetClient.LoginSocket.IsDisposed)
            {
                NetClient.LoginSocket.Statistics.PingReceived();
            }
        }


        private static void BuyList(ref StackDataReader p)
        {
            if (!World.InGame)
            {
                return;
            }

            Item container = World.Items.Get(p.ReadUInt32BE());

            if (container == null)
            {
                return;
            }

            Mobile vendor = World.Mobiles.Get(container.Container);

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
                gump = new ShopGump(vendor, true, 150, 5);
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

                    Item it = (Item) first;

                    it.Price = p.ReadUInt32BE();
                    byte nameLen = p.ReadUInt8();
                    string name = p.ReadASCII(nameLen);

                    if (World.OPL.TryGetNameAndData(it.Serial, out string s, out _))
                    {
                        it.Name = s;
                    }
                    else if (int.TryParse(name, out int cliloc))
                    {
                        it.Name = ClilocLoader.Instance.Translate(cliloc, $"\t{it.ItemData.Name}: \t{it.Amount}", true);
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

        private static void UpdateCharacter(ref StackDataReader p)
        {
            if (World.Player == null)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            Mobile mobile = World.Mobiles.Get(serial);

            if (mobile == null)
            {
                return;
            }

            ushort graphic = p.ReadUInt16BE();
            ushort x = p.ReadUInt16BE();
            ushort y = p.ReadUInt16BE();
            sbyte z = p.ReadInt8();
            Direction direction = (Direction) p.ReadUInt8();
            ushort hue = p.ReadUInt16BE();
            Flags flags = (Flags) p.ReadUInt8();
            NotorietyFlag notoriety = (NotorietyFlag) p.ReadUInt8();

            mobile.NotorietyFlag = notoriety;

            if (serial == World.Player)
            {
                mobile.Flags = flags;
                mobile.Graphic = graphic;
                mobile.CheckGraphicChange();
                mobile.FixHue(hue);
                // TODO: x,y,z, direction cause elastic effect, ignore 'em for the moment
            }
            else
            {
                UpdateGameObject
                (
                    serial,
                    graphic,
                    0,
                    0,
                    x,
                    y,
                    z,
                    direction,
                    hue,
                    flags,
                    0,
                    1,
                    1
                );
            }
        }

        private static void UpdateObject(ref StackDataReader p)
        {
            if (World.Player == null)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            ushort graphic = p.ReadUInt16BE();
            ushort x = p.ReadUInt16BE();
            ushort y = p.ReadUInt16BE();
            sbyte z = p.ReadInt8();
            Direction direction = (Direction) p.ReadUInt8();
            ushort hue = p.ReadUInt16BE();
            Flags flags = (Flags) p.ReadUInt8();
            NotorietyFlag notoriety = (NotorietyFlag) p.ReadUInt8();
            bool oldDead = false;
            //bool alreadyExists = World.Get(serial) != null;

            if (serial == World.Player)
            {
                oldDead = World.Player.IsDead;
                World.Player.Graphic = graphic;
                World.Player.CheckGraphicChange();
                World.Player.FixHue(hue);
                World.Player.Flags = flags;
            }
            else
            {
                UpdateGameObject
                (
                    serial,
                    graphic,
                    0,
                    0,
                    x,
                    y,
                    z,
                    direction,
                    hue,
                    flags,
                    0,
                    0,
                    1
                );
            }

            Entity obj = World.Get(serial);

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
                    Item it = (Item) o;

                    if (!it.Opened && it.Layer != Layer.Backpack)
                    {
                        World.RemoveItem(it.Serial, true);
                    }

                    o = next;
                }
            }

            if (SerialHelper.IsMobile(serial) && obj is Mobile mob)
            {
                mob.NotorietyFlag = notoriety;

                UIManager.GetGump<PaperDollGump>(serial)?.RequestUpdateContents();
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

                if (Client.Version >= Data.ClientVersion.CV_70331)
                {
                    item_hue = p.ReadUInt16BE();
                }
                else if ((itemGraphic & 0x8000) != 0)
                {
                    itemGraphic &= 0x7FFF;
                    item_hue = p.ReadUInt16BE();
                }


                Item item = World.GetOrCreateItem(itemSerial);
                item.Graphic = itemGraphic;
                item.FixHue(item_hue);
                item.Amount = 1;
                World.RemoveItemFromContainer(item);
                item.Container = serial;
                item.Layer = (Layer) layer;

                item.CheckGraphicChange();

                obj.PushToBack(item);

                itemSerial = p.ReadUInt32BE();
            }

            if (serial == World.Player)
            {
                if (oldDead != World.Player.IsDead)
                {
                    if (World.Player.IsDead)
                    {
                        World.ChangeSeason(Game.Managers.Season.Desolation, 42);
                    }
                    else
                    {
                        World.ChangeSeason(World.OldSeason, World.OldMusicIndex);
                    }
                }

                UIManager.GetGump<PaperDollGump>(serial)?.RequestUpdateContents();

                World.Player.UpdateAbilities();
            }
        }

        private static void OpenMenu(ref StackDataReader p)
        {
            if (!World.InGame)
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
                MenuGump gump = new MenuGump(serial, id, name)
                {
                    X = 100,
                    Y = 100
                };

                int posX = 0;

                for (int i = 0; i < count; i++)
                {
                    ushort graphic = p.ReadUInt16BE();
                    ushort hue = p.ReadUInt16BE();
                    name = p.ReadASCII(p.ReadUInt8());

                    Rectangle rect = ArtLoader.Instance.GetTexture(graphic)?.Bounds ?? Rectangle.Empty;

                    if (rect.Width != 0 && rect.Height != 0)
                    {
                        int posY = rect.Height;

                        if (posY >= 47)
                        {
                            posY = 0;
                        }
                        else
                        {
                            posY = (47 - posY) >> 1;
                        }

                        gump.AddItem
                        (
                            graphic,
                            hue,
                            name,
                            posX,
                            posY,
                            i + 1
                        );

                        posX += rect.Width;
                    }
                }

                UIManager.Add(gump);
            }
            else
            {
                GrayMenuGump gump = new GrayMenuGump(serial, id, name)
                {
                    X = (Client.Game.Window.ClientBounds.Width >> 1) - 200,
                    Y = (Client.Game.Window.ClientBounds.Height >> 1) - ((121 + count * 21) >> 1)
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

                gump.Add
                (
                    new Button(0, 0x1450, 0x1451, 0x1450)
                    {
                        ButtonAction = ButtonAction.Activate,
                        X = 70,
                        Y = offsetY
                    }
                );

                gump.Add
                (
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


        private static void OpenPaperdoll(ref StackDataReader p)
        {
            Mobile mobile = World.Mobiles.Get(p.ReadUInt32BE());

            if (mobile == null)
            {
                return;
            }

            string text = p.ReadASCII(60);
            byte flags = p.ReadUInt8();

            mobile.Title = text;

            PaperDollGump paperdoll = UIManager.GetGump<PaperDollGump>(mobile);

            if (paperdoll == null)
            {
                if (!UIManager.GetGumpCachePosition(mobile, out Point location))
                {
                    location = new Point(100, 100);
                }

                UIManager.Add(new PaperDollGump(mobile, (flags & 0x02) != 0) { Location = location });
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

        private static void CorpseEquipment(ref StackDataReader p)
        {
            if (!World.InGame)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            Entity corpse = World.Get(serial);

            if (corpse == null)
            {
                return;
            }

            Layer layer = (Layer) p.ReadUInt8();

            while (layer != Layer.Invalid && p.Position < p.Length)
            {
                uint item_serial = p.ReadUInt32BE();

                if (layer - 1 != Layer.Backpack)
                {
                    Item item = World.GetOrCreateItem(item_serial);

                    World.RemoveItemFromContainer(item);
                    item.Container = serial;
                    item.Layer = layer - 1;
                    corpse.PushToBack(item);
                }

                layer = (Layer) p.ReadUInt8();
            }
        }


        private static void DisplayMap(ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();
            ushort gumpid = p.ReadUInt16BE();
            ushort startX = p.ReadUInt16BE();
            ushort startY = p.ReadUInt16BE();
            ushort endX = p.ReadUInt16BE();
            ushort endY = p.ReadUInt16BE();
            ushort width = p.ReadUInt16BE();
            ushort height = p.ReadUInt16BE();

            MapGump gump = new MapGump(serial, gumpid, width, height);

            if (p[0] == 0xF5 || Client.Version >= Data.ClientVersion.CV_308Z)
            {
                ushort facet = 0;

                if (p[0] == 0xF5)
                {
                    facet = p.ReadUInt16BE();
                }

                if (MultiMapLoader.Instance.HasFacet(facet))
                {
                    gump.SetMapTexture
                    (
                        MultiMapLoader.Instance.LoadFacet
                        (
                            facet,
                            width,
                            height,
                            startX,
                            startY,
                            endX,
                            endY
                        )
                    );
                }
                else
                {
                    gump.SetMapTexture
                    (
                        MultiMapLoader.Instance.LoadMap
                        (
                            width,
                            height,
                            startX,
                            startY,
                            endX,
                            endY
                        )
                    );
                }
            }
            else
            {
                gump.SetMapTexture
                (
                    MultiMapLoader.Instance.LoadMap
                    (
                        width,
                        height,
                        startX,
                        startY,
                        endX,
                        endY
                    )
                );
            }

            UIManager.Add(gump);

            Item it = World.Items.Get(serial);

            if (it != null)
            {
                it.Opened = true;
            }
        }

        private static void OpenBook(ref StackDataReader p)
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
                string title = oldpacket ? p.ReadUTF8(60, true) : p.ReadUTF8(p.ReadUInt16BE(), true);
                string author = oldpacket ? p.ReadUTF8(30, true) : p.ReadUTF8(p.ReadUInt16BE(), true);

                UIManager.Add
                (
                    new ModernBookGump
                    (
                        serial,
                        page_count,
                        title,
                        author,
                        editable,
                        oldpacket
                    )
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
                bgump.SetTile(oldpacket ? p.ReadUTF8(60, true) : p.ReadUTF8(p.ReadUInt16BE(), true), editable);
                bgump.SetAuthor(oldpacket ? p.ReadUTF8(30, true) : p.ReadUTF8(p.ReadUInt16BE(), true), editable);
                bgump.UseNewHeader = !oldpacket;
                bgump.SetInScreen();
                bgump.BringOnTop();
            }
        }

        private static void DyeData(ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();
            p.Skip(2);
            ushort graphic = p.ReadUInt16BE();

            Rectangle rect = GumpsLoader.Instance.GetTexture(0x0906).Bounds;

            int x = (Client.Game.Window.ClientBounds.Width >> 1) - (rect.Width >> 1);
            int y = (Client.Game.Window.ClientBounds.Height >> 1) - (rect.Height >> 1);

            ColorPickerGump gump = UIManager.GetGump<ColorPickerGump>(serial);

            if (gump == null || gump.IsDisposed || gump.Graphic != graphic)
            {
                gump?.Dispose();

                gump = new ColorPickerGump
                (
                    serial,
                    graphic,
                    x,
                    y,
                    null
                );

                UIManager.Add(gump);
            }
        }

        private static void MovePlayer(ref StackDataReader p)
        {
            if (!World.InGame)
            {
                return;
            }

            Direction direction = (Direction) p.ReadUInt8();
            World.Player.Walk(direction & Direction.Mask, (direction & Direction.Running) != 0);
        }

        private static void UpdateName(ref StackDataReader p)
        {
            if (!World.InGame)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            string name = p.ReadASCII();

            WMapEntity wme = World.WMapManager.GetEntity(serial);

            if (wme != null && !string.IsNullOrEmpty(name))
            {
                wme.Name = name;
            }


            Entity entity = World.Get(serial);

            if (entity != null)
            {
                entity.Name = name;

                if (serial == World.Player.Serial && !string.IsNullOrEmpty(name) && name != World.Player.Name)
                {
                    Client.Game.SetWindowTitle(name);
                }

                UIManager.GetGump<NameOverheadGump>(serial)?.SetName();
            }
        }

        private static void MultiPlacement(ref StackDataReader p)
        {
            if (World.Player == null)
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

            TargetManager.SetTargetingMulti
            (
                targID,
                multiID,
                xOff,
                yOff,
                zOff,
                hue
            );
        }

        private static void ASCIIPrompt(ref StackDataReader p)
        {
            if (!World.InGame)
            {
                return;
            }

            MessageManager.PromptData = new PromptData
            {
                Prompt = ConsolePrompt.ASCII,
                Data = p.ReadUInt64BE()
            };
        }

        private static void SellList(ref StackDataReader p)
        {
            if (!World.InGame)
            {
                return;
            }

            Mobile vendor = World.Mobiles.Get(p.ReadUInt32BE());

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
            gump = new ShopGump(vendor, false, 100, 0);

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
                    name = ClilocLoader.Instance.GetString(clilocnum);
                    fromcliloc = true;
                }
                else if (string.IsNullOrEmpty(name))
                {
                    bool success = World.OPL.TryGetNameAndData(serial, out name, out _);

                    if (!success)
                    {
                        name = TileDataLoader.Instance.StaticData[graphic].Name;
                    }
                }

                //if (string.IsNullOrEmpty(item.Name))
                //    item.Name = name;

                gump.AddItem
                (
                    serial,
                    graphic,
                    hue,
                    amount,
                    price,
                    name,
                    fromcliloc
                );
            }

            UIManager.Add(gump);
        }

        private static void UpdateHitpoints(ref StackDataReader p)
        {
            Entity entity = World.Get(p.ReadUInt32BE());

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

            if (entity == World.Player)
            {
                UoAssist.SignalHits();
            }
        }

        private static void UpdateMana(ref StackDataReader p)
        {
            Mobile mobile = World.Mobiles.Get(p.ReadUInt32BE());

            if (mobile == null)
            {
                return;
            }

            mobile.ManaMax = p.ReadUInt16BE();
            mobile.Mana = p.ReadUInt16BE();

            if (mobile == World.Player)
            {
                UoAssist.SignalMana();
            }
        }

        private static void UpdateStamina(ref StackDataReader p)
        {
            Mobile mobile = World.Mobiles.Get(p.ReadUInt32BE());

            if (mobile == null)
            {
                return;
            }

            mobile.StaminaMax = p.ReadUInt16BE();
            mobile.Stamina = p.ReadUInt16BE();

            if (mobile == World.Player)
            {
                UoAssist.SignalStamina();
            }
        }

        private static void OpenUrl(ref StackDataReader p)
        {
            string url = p.ReadASCII();

            if (!string.IsNullOrEmpty(url))
            {
                PlatformHelper.LaunchBrowser(url);
            }
        }

        private static void TipWindow(ref StackDataReader p)
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

            UIManager.Add
            (
                new TipNoticeGump(tip, flag, str)
                {
                    X = x,
                    Y = y
                }
            );
        }

        private static void AttackCharacter(ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();

            //if (TargetManager.LastAttack != serial && World.InGame)
            //{



            //}

            GameActions.SendCloseStatus(TargetManager.LastAttack);
            TargetManager.LastAttack = serial;
            GameActions.RequestMobileStatus(serial);
        }

        private static void TextEntryDialog(ref StackDataReader p)
        {
            if (!World.InGame)
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

            TextEntryDialogGump gump = new TextEntryDialogGump
            (
                serial,
                143,
                172,
                variant,
                (int) maxLength,
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

        private static void UnicodeTalk(ref StackDataReader p)
        {
            if (!World.InGame)
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
            Entity entity = World.Get(serial);
            ushort graphic = p.ReadUInt16BE();
            MessageType type = (MessageType) p.ReadUInt8();
            ushort hue = p.ReadUInt16BE();
            ushort font = p.ReadUInt16BE();
            string lang = p.ReadASCII(4);
            string name = p.ReadASCII();

            if (serial == 0 && graphic == 0 && type == MessageType.Regular && font == 0xFFFF && hue == 0xFFFF && name.ToLower() == "system")
            {
                byte[] buffer =
                {
                    0x03, 0x00, 0x28, 0x20, 0x00, 0x34, 0x00, 0x03, 0xdb, 0x13,
                    0x14, 0x3f, 0x45, 0x2c, 0x58, 0x0f, 0x5d, 0x44, 0x2e, 0x50,
                    0x11, 0xdf, 0x75, 0x5c, 0xe0, 0x3e, 0x71, 0x4f, 0x31, 0x34,
                    0x05, 0x4e, 0x18, 0x1e, 0x72, 0x0f, 0x59, 0xad, 0xf5, 0x00
                };

                NetClient.Socket.Send(buffer, buffer.Length);

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
            else if (type == MessageType.System || serial == 0xFFFF_FFFF || serial == 0 || name.ToLower() == "system" && entity == null)
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

            MessageManager.HandleMessage
            (
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

        private static void DisplayDeath(ref StackDataReader p)
        {
            if (!World.InGame)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            uint corpseSerial = p.ReadUInt32BE();
            uint running = p.ReadUInt32BE();

            Mobile owner = World.Mobiles.Get(serial);

            if (owner == null || serial == World.Player)
            {
                return;
            }

            serial |= 0x80000000;

            if (World.Mobiles.Remove(owner.Serial))
            {
                for (LinkedObject i = owner.Items; i != null; i = i.Next)
                {
                    Item it = (Item)i;
                    it.Container = serial;
                }

                World.Mobiles[serial] = owner;
                owner.Serial = serial;
            }

            if (SerialHelper.IsValid(corpseSerial))
            {
                World.CorpseManager.Add(corpseSerial, serial, owner.Direction, running != 0);
            }


            byte group = AnimationsLoader.Instance.GetDieGroupIndex(owner.Graphic, running != 0, true);
            owner.SetAnimation(group, 0, 5, 1);
            owner.AnimIndex = 0;

            if (ProfileManager.CurrentProfile.AutoOpenCorpses)
            {
                World.Player.TryOpenCorpses();
            }
        }

        private static void OpenGump(ref StackDataReader p)
        {
            if (World.Player == null)
            {
                return;
            }

            uint sender = p.ReadUInt32BE();
            uint gumpID = p.ReadUInt32BE();
            int x = (int) p.ReadUInt32BE();
            int y = (int) p.ReadUInt32BE();

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

            CreateGump
            (
                sender,
                gumpID,
                x,
                y,
                cmd,
                lines
            );
        }

        private static void ChatMessage(ref StackDataReader p)
        {
            ushort cmd = p.ReadUInt16BE();

            switch (cmd)
            {
                case 0x03E8: // create conference
                    p.Skip(4);
                    string channelName = p.ReadUnicodeBE();
                    bool hasPassword = p.ReadUInt16BE() == 0x31;
                    ChatManager.CurrentChannelName = channelName;
                    ChatManager.AddChannel(channelName, hasPassword);

                    UIManager.GetGump<ChatGump>()?.RequestUpdateContents();

                    break;

                case 0x03E9: // destroy conference
                    p.Skip(4);
                    channelName = p.ReadUnicodeBE();
                    ChatManager.RemoveChannel(channelName);

                    UIManager.GetGump<ChatGump>()?.RequestUpdateContents();

                    break;

                case 0x03EB: // display enter username window
                    ChatManager.ChatIsEnabled = ChatStatus.EnabledUserRequest;

                    break;

                case 0x03EC: // close chat
                    ChatManager.Clear();
                    ChatManager.ChatIsEnabled = ChatStatus.Disabled;

                    UIManager.GetGump<ChatGump>()?.Dispose();

                    break;

                case 0x03ED: // username accepted, display chat
                    p.Skip(4);
                    string username = p.ReadUnicodeBE();
                    ChatManager.ChatIsEnabled = ChatStatus.Enabled;
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
                    ChatManager.CurrentChannelName = channelName;

                    UIManager.GetGump<ChatGump>()?.UpdateConference();

                    GameActions.Print(string.Format(ResGeneral.YouHaveJoinedThe0Channel, channelName), ProfileManager.CurrentProfile.ChatMessageHue, MessageType.Regular, 1);

                    break;

                case 0x03F4:
                    p.Skip(4);
                    channelName = p.ReadUnicodeBE();

                    GameActions.Print(string.Format(ResGeneral.YouHaveLeftThe0Channel, channelName), ProfileManager.CurrentProfile.ChatMessageHue, MessageType.Regular, 1);

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
                    GameActions.Print($"{username}: {msgSent}", ProfileManager.CurrentProfile.ChatMessageHue, MessageType.Regular, 1);

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

                        GameActions.Print(msg, ProfileManager.CurrentProfile.ChatMessageHue, MessageType.Regular, 1);
                    }

                    break;
            }
        }

        private static void Help(ref StackDataReader p)
        {
        }

        private static void CharacterProfile(ref StackDataReader p)
        {
            if (!World.InGame)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            string header = p.ReadASCII();
            string footer = p.ReadUnicodeBE();

            string body = p.ReadUnicodeBE();

            UIManager.GetGump<ProfileGump>(serial)?.Dispose();

            UIManager.Add
            (
                new ProfileGump
                (
                    serial,
                    header,
                    footer,
                    body,
                    serial == World.Player.Serial
                )
            );
        }

        private static void EnableLockedFeatures(ref StackDataReader p)
        {
            uint flags = 0;

            if (Client.Version >= Data.ClientVersion.CV_60142)
            {
                flags = p.ReadUInt32BE();
            }
            else
            {
                flags = p.ReadUInt16BE();
            }

            World.ClientLockedFeatures.SetFlags((LockedFeatureFlags) flags);

            ChatManager.ChatIsEnabled = World.ClientLockedFeatures.T2A ? ChatStatus.Enabled : 0;

            AnimationsLoader.Instance.UpdateAnimationTable(flags);
        }

        private static void DisplayQuestArrow(ref StackDataReader p)
        {
            bool display = p.ReadBool();
            ushort mx = p.ReadUInt16BE();
            ushort my = p.ReadUInt16BE();

            uint serial = 0;

            if (Client.Version >= Data.ClientVersion.CV_7090)
            {
                serial = p.ReadUInt32BE();
            }

            QuestArrowGump arrow = UIManager.GetGump<QuestArrowGump>(serial);

            if (display)
            {
                if (arrow == null)
                {
                    UIManager.Add(new QuestArrowGump(serial, mx, my));
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

        private static void UltimaMessengerR(ref StackDataReader p)
        {
        }

        private static void Season(ref StackDataReader p)
        {
            if (World.Player == null)
            {
                return;
            }

            byte season = p.ReadUInt8();
            byte music = p.ReadUInt8();

            if (season > 4)
            {
                season = 0;
            }


            if (World.Player.IsDead && season == 4)
            {
                return;
            }

            World.OldSeason = (Season) season;
            World.OldMusicIndex = music;

            if (World.Season == Game.Managers.Season.Desolation)
            {
                World.OldMusicIndex = 42;
            }

            World.ChangeSeason((Season) season, music);
        }

        private static void ClientVersion(ref StackDataReader p)
        {
            NetClient.Socket.Send_ClientVersion(Settings.GlobalSettings.ClientVersion);
        }

        private static void AssistVersion(ref StackDataReader p)
        {
            //uint version = p.ReadUInt32BE();

            //string[] parts = Service.GetByLocalSerial<Settings>().ClientVersion.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            //byte[] clientVersionBuffer =
            //    {byte.Parse(parts[0]), byte.Parse(parts[1]), byte.Parse(parts[2]), byte.Parse(parts[3])};

            //NetClient.Socket.Send(new PAssistVersion(clientVersionBuffer, version));
        }

        private static void ExtendedCommand(ref StackDataReader p)
        {
            ushort cmd = p.ReadUInt16BE();

            switch (cmd)
            {
                case 0: break;

                //===========================================================================================
                //===========================================================================================
                case 1: // fast walk prevention
                    for (int i = 0; i < 6; i++)
                    {
                        World.Player.Walker.FastWalkStack.SetValue(i, p.ReadUInt32BE());
                    }

                    break;

                //===========================================================================================
                //===========================================================================================
                case 2: // add key to fast walk stack
                    World.Player.Walker.FastWalkStack.AddValue(p.ReadUInt32BE());

                    break;

                //===========================================================================================
                //===========================================================================================
                case 4: // close generic gump 
                    uint ser = p.ReadUInt32BE();
                    int button = (int) p.ReadUInt32BE();


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
                    World.Party.ParsePacket(ref p);

                    break;

                //===========================================================================================
                //===========================================================================================
                case 8: // map change
                    World.MapIndex = p.ReadUInt8();

                    break;

                //===========================================================================================
                //===========================================================================================
                case 0x0C: // close statusbar gump
                    UIManager.GetGump<HealthBarGump>(p.ReadUInt32BE())?.Dispose();

                    break;

                //===========================================================================================
                //===========================================================================================
                case 0x10: // display equip info
                    Item item = World.Items.Get(p.ReadUInt32BE());

                    if (item == null)
                    {
                        return;
                    }

                    uint cliloc = p.ReadUInt32BE();
                    string str = string.Empty;

                    if (cliloc > 0)
                    {
                        str = ClilocLoader.Instance.GetString((int) cliloc, true);

                        if (!string.IsNullOrEmpty(str))
                        {
                            item.Name = str;
                        }

                        MessageManager.HandleMessage
                        (
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
                        string attr = ClilocLoader.Instance.GetString((int)next);

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

                        count++;
                    }

                    if (count < 20 && count > 0 || next == 0xFFFFFFFC && count == 0)
                    {
                        strBuffer.Append(']');
                    }

                    if (strBuffer.Length != 0)
                    {
                        MessageManager.HandleMessage
                        (
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
                case 0x11: break;

                //===========================================================================================
                //===========================================================================================
                case 0x14: // display popup/context menu
                    UIManager.ShowGamePopup
                    (
                        new PopupMenuGump(PopupMenuData.Parse(ref p))
                        {
                            X = DelayedObjectClickManager.LastMouseX,
                            Y = DelayedObjectClickManager.LastMouseY
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

                            if (serial == World.Player.Serial)
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

                    if (MapLoader.Instance.ApplyPatches(ref p))
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


                        int map = World.MapIndex;
                        World.MapIndex = -1;
                        World.MapIndex = map;


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
                            Mobile bonded = World.Mobiles.Get(serial);

                            if (bonded == null)
                            {
                                break;
                            }

                            bool dead = p.ReadBool();
                            bonded.IsDead = dead;

                            break;

                        case 2:

                            if (serial == World.Player)
                            {
                                byte updategump = p.ReadUInt8();
                                byte state = p.ReadUInt8();

                                World.Player.StrLock = (Lock) ((state >> 4) & 3);
                                World.Player.DexLock = (Lock) ((state >> 2) & 3);
                                World.Player.IntLock = (Lock) (state & 3);

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

                                Mobile mobile = World.Mobiles.Get(serial);

                                if (mobile != null)
                                {
                                    // TODO: animation for statues
                                    //mobile.SetAnimation(Mobile.GetReplacedObjectAnimation(mobile.Graphic, animation), 0, (byte) frame, 0, false, false);
                                    //mobile.AnimationFromServer = true;
                                }
                            }
                            else if (World.Player != null && serial == World.Player)
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
                    Item spellbook = World.GetOrCreateItem(p.ReadUInt32BE());
                    spellbook.Graphic = p.ReadUInt16BE();
                    spellbook.Clear();
                    ushort type = p.ReadUInt16BE();

                    for (int j = 0; j < 2; j++)
                    {
                        uint spells = 0;

                        for (int i = 0; i < 4; i++)
                        {
                            spells |= (uint) (p.ReadUInt8() << (i * 8));
                        }

                        for (int i = 0; i < 32; i++)
                        {
                            if ((spells & (1 << i)) != 0)
                            {
                                ushort cc = (ushort) (j * 32 + i + 1);
                                // FIXME: should i call Item.Create ?
                                Item spellItem = Item.Create(cc); // new Item()
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

                    Item multi = World.Items.Get(serial);

                    if (multi == null)
                    {
                        World.HouseManager.Remove(serial);
                    }

                    if (!World.HouseManager.TryGetHouse(serial, out House house) || !house.IsCustom || house.Revision != revision)
                    {
                        NetClient.Socket.Send_CustomHouseDataRequest(serial);
                    }
                    else
                    {
                        house.Generate();
                        BoatMovingManager.ClearSteps(serial);

                        UIManager.GetGump<MiniMapGump>()?.RequestUpdateContents();

                        if (World.HouseManager.EntityIntoHouse(serial, World.Player))
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

                            gump = new HouseCustomizationGump(serial, 50, 50);
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
                        World.Player.Abilities[i] &= (Ability) 0x7F;
                    }

                    break;

                //===========================================================================================
                //===========================================================================================
                case 0x22:
                    p.Skip(1);

                    Entity en = World.Get(p.ReadUInt32BE());

                    if (en != null)
                    {
                        byte damage = p.ReadUInt8();

                        if (damage > 0)
                        {
                            World.WorldTextManager.AddDamage(en, damage);
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
                                    World.ActiveSpellIcons.Add(spell);
                                }
                                else
                                {
                                    spellButton.Hue = 0;
                                    World.ActiveSpellIcons.Remove(spell);
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

                    if (val > (int) CharacterSpeedType.FastUnmountAndCantRun)
                    {
                        val = 0;
                    }

                    World.Player.SpeedMode = (CharacterSpeedType) val;

                    break;

                case 0x2A:
                    bool isfemale = p.ReadBool();
                    byte race = p.ReadUInt8();

                    // TODO: gump race request

                    GameActions.Print("[DEBUG]: change-race gump is not implemented yet.", 34);

                    break;

                case 0x2B:
                    serial = p.ReadUInt16BE();
                    byte animID = p.ReadUInt8();
                    byte frameCount = p.ReadUInt8();

                    //foreach (Mobile m in World.Mobiles)
                    //{
                    //    if ((m.Serial & 0xFFFF) == serial)
                    //    {
                    //       // byte group = Mobile.GetObjectNewAnimation(m, animID, action, mode);
                    //        m.SetAnimation(animID);
                    //        //m.AnimationRepeatMode = 1;
                    //        //m.AnimationForwardDirection = true;
                    //        //if ((type == 1 || type == 2) && mobile.Graphic == 0x0015)
                    //        //    mobile.AnimationRepeat = true;
                    //        //mobile.AnimationFromServer = true;

                    //        //m.SetAnimation(Mobile.GetReplacedObjectAnimation(m.Graphic, animID), 0, frameCount);
                    //       // m.AnimationFromServer = true;
                    //        break;
                    //    }
                    //}

                    break;

                case 0xBEEF: // ClassicUO commands

                    type = p.ReadUInt16BE();


                    break;

                default:
                    Log.Warn($"Unhandled 0xBF - sub: {cmd.ToHex()}");

                    break;
            }
        }

        private static void DisplayClilocString(ref StackDataReader p)
        {
            if (World.Player == null)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            Entity entity = World.Get(serial);
            ushort graphic = p.ReadUInt16BE();
            MessageType type = (MessageType) p.ReadUInt8();
            ushort hue = p.ReadUInt16BE();
            ushort font = p.ReadUInt16BE();
            uint cliloc = p.ReadUInt32BE();
            AffixType flags = p[0] == 0xCC ? (AffixType) p.ReadUInt8() : 0x00;
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

            string text = ClilocLoader.Instance.Translate((int) cliloc, arguments);

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

            if (!FontsLoader.Instance.UnicodeFontExists((byte) font))
            {
                font = 0;
            }

            TextType text_type = TextType.SYSTEM;

            if (serial == 0xFFFF_FFFF || serial == 0 || !string.IsNullOrEmpty(name) && string.Equals(name, "system", StringComparison.InvariantCultureIgnoreCase))
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

            MessageManager.HandleMessage
            (
                entity,
                text,
                name,
                hue,
                type,
                (byte) font,
                text_type,
                true
            );
        }

        private static void UnicodePrompt(ref StackDataReader p)
        {
            if (!World.InGame)
            {
                return;
            }

            MessageManager.PromptData = new PromptData
            {
                Prompt = ConsolePrompt.Unicode,
                Data = p.ReadUInt64BE()
            };
        }

        private static void Semivisible(ref StackDataReader p)
        {
        }

        private static void InvalidMapEnable(ref StackDataReader p)
        {
        }

        private static void ParticleEffect3D(ref StackDataReader p)
        {
        }

        private static void GetUserServerPingGodClientR(ref StackDataReader p)
        {
        }

        private static void GlobalQueCount(ref StackDataReader p)
        {
        }

        private static void ConfigurationFileR(ref StackDataReader p)
        {
        }

        private static void Logout(ref StackDataReader p)
        {
        }

        private static void MegaCliloc(ref StackDataReader p)
        {
            if (!World.InGame)
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

            Entity entity = World.Mobiles.Get(serial);

            if (entity == null)
            {
                if (SerialHelper.IsMobile(serial))
                {
                    Log.Warn("Searching a mobile into World.Items from MegaCliloc packet");
                }

                entity = World.Items.Get(serial);
            }

            List<(int, string)> list = new List<(int, string)>();
            int totalLength = 0;

            while (p.Position < p.Length)
            {
                int cliloc = (int) p.ReadUInt32BE();

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

                string str = ClilocLoader.Instance.Translate(cliloc, argument, true);

                if (str == null)
                {
                    continue;
                }

                // horrible fix for (Imbued) hue
                if (Client.Version >= Data.ClientVersion.CV_60143 && cliloc == 1080418)
                {
                    str = str.Insert(0, "<basefont color=#42a5ff>");
                    str += "</basefont>";
                }


                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Item1 == cliloc && string.Equals(list[i].Item2, str, StringComparison.Ordinal))
                    {
                        list.RemoveAt(i);

                        break;
                    }
                }

                list.Add((cliloc, str));

                totalLength += str.Length;
            }

            Item container = null;

            if (entity is Item it && SerialHelper.IsValid(it.Container))
            {
                container = World.Items.Get(it.Container);
            }

            bool inBuyList = false;

            if (container != null)
            {
                inBuyList = container.Layer == Layer.ShopBuy || container.Layer == Layer.ShopBuyRestock || container.Layer == Layer.ShopSell;
            }


            bool first = true;

            string name = string.Empty;
            string data = string.Empty;

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

            World.OPL.Add(serial, revision, name, data);

            if (inBuyList && container != null && SerialHelper.IsValid(container.Serial))
            {
                UIManager.GetGump<ShopGump>(container.RootContainer)?.SetNameTo((Item) entity, name);
            }
        }

        private static void GenericAOSCommandsR(ref StackDataReader p)
        {
        }

        private static unsafe void ReadUnsafeCustomHouseData
        (
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
            Span<byte> span = dlen <= 1024 ? stackalloc byte[dlen] : (buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(dlen));

            try
            {
                fixed (byte* dbytesPtr = span)
                {
                    fixed (byte* srcPtr = &source[sourcePosition])
                    {
                        ZLib.Decompress
                        (
                            (IntPtr)srcPtr,
                            clen,
                            0,
                            (IntPtr)dbytesPtr,
                            dlen
                        );
                    }

                    StackDataReader reader = new StackDataReader(span.Slice(0, dlen));

                    ushort id = 0;
                    sbyte x = 0, y = 0, z = 0;

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
                                    house.Add
                                    (
                                        id,
                                        0,
                                        (ushort) (item.X + x),
                                        (ushort) (item.Y + y),
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
                                    house.Add
                                    (
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
                            short offX = 0, offY = 0;
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
                                    house.Add
                                    (
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
            }
            finally
            {
                if (buffer != null)
                {
                    System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }

        private static void CustomHouse(ref StackDataReader p)
        {
            bool compressed = p.ReadUInt8() == 0x03;
            bool enableReponse = p.ReadBool();
            uint serial = p.ReadUInt32BE();
            Item foundation = World.Items.Get(serial);
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

            if (!World.HouseManager.TryGetHouse(foundation, out House house))
            {
                house = new House(foundation, revision, true);
                World.HouseManager.Add(foundation, house);
            }
            else
            {
                house.ClearComponents(true);
                house.Revision = revision;
                house.IsCustom = true;
            }

            short minX = (short) multi.Value.X;
            short minY = (short) multi.Value.Y;
            short maxY = (short) multi.Value.Height;

            if (minX == 0 && minY == 0 && maxY == 0 && multi.Value.Width == 0)
            {
                Log.Warn("[CustomHouse (0xD8) - Invalid multi dimentions. Maybe missing some installation required files");

                return;
            }

            byte planes = p.ReadUInt8();

            house.ClearCustomHouseComponents(0);

            for (int plane = 0; plane < planes; plane++)
            {
                uint header = p.ReadUInt32BE();
                int dlen = (int) (((header & 0xFF0000) >> 16) | ((header & 0xF0) << 4));
                int clen = (int) (((header & 0xFF00) >> 8) | ((header & 0x0F) << 8));
                int planeZ = (int) ((header & 0x0F000000) >> 24);
                int planeMode = (int) ((header & 0xF0000000) >> 28);

                if (clen <= 0)
                {
                    continue;
                }

                ReadUnsafeCustomHouseData
                (
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


            if (World.CustomHouseManager != null)
            {
                World.CustomHouseManager.GenerateFloorPlace();

                UIManager.GetGump<HouseCustomizationGump>(house.Serial)?.Update();
            }

            UIManager.GetGump<MiniMapGump>()?.RequestUpdateContents();

            if (World.HouseManager.EntityIntoHouse(serial, World.Player))
            {
                Client.Game.GetScene<GameScene>()?.UpdateMaxDrawZ(true);
            }

            BoatMovingManager.ClearSteps(serial);
        }

        private static void CharacterTransferLog(ref StackDataReader p)
        {
        }

        private static void OPLInfo(ref StackDataReader p)
        {
            if (World.ClientFeatures.TooltipsEnabled)
            {
                uint serial = p.ReadUInt32BE();
                uint revision = p.ReadUInt32BE();

                if (!World.OPL.IsRevisionEquals(serial, revision))
                {
                    AddMegaClilocRequest(serial);
                }
            }
        }

        private static void OpenCompressedGump(ref StackDataReader p)
        {
            uint sender = p.ReadUInt32BE();
            uint gumpID = p.ReadUInt32BE();
            uint x = p.ReadUInt32BE();
            uint y = p.ReadUInt32BE();
            uint clen = p.ReadUInt32BE() - 4;
            int dlen = (int) p.ReadUInt32BE();
            byte[] decData = System.Buffers.ArrayPool<byte>.Shared.Rent(dlen);
            string layout;

            try
            {
                unsafe
                {
                    fixed (byte* destPtr = decData)
                    {
                        ZLib.Decompress
                        (
                            p.PositionAddress,
                            (int)clen,
                            0,
                            (IntPtr)destPtr,
                            dlen
                        );

                        layout = Encoding.UTF8.GetString(destPtr, dlen);
                    }
                }
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(decData);
            }

 
            p.Skip((int) clen);

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
                        unsafe
                        {
                            fixed (byte* destPtr = decData)
                            {
                                ZLib.Decompress
                                (
                                    p.PositionAddress,
                                    (int)clen,
                                    0,
                                    (IntPtr)destPtr,
                                    dlen
                                );
                            }
                        }

                        p.Skip((int)clen);


                        StackDataReader reader = new StackDataReader(decData.AsSpan(0, dlen));

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

                CreateGump
                (
                    sender,
                    gumpID,
                    (int)x,
                    (int)y,
                    layout,
                    lines
                );
            }
            finally
            {
                //System.Buffers.ArrayPool<string>.Shared.Return(lines);
            }
        }

        private static void UpdateMobileStatus(ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();
            byte status = p.ReadUInt8();

            if (status == 1)
            {
                uint attackerSerial = p.ReadUInt32BE();
            }
        }

        private static void BuffDebuff(ref StackDataReader p)
        {
            if (World.Player == null)
            {
                return;
            }

            const ushort BUFF_ICON_START = 0x03E9;
            const ushort BUFF_ICON_START_NEW = 0x466;

            uint serial = p.ReadUInt32BE();
            BuffIconType ic = (BuffIconType) p.ReadUInt16BE();

            ushort iconID = (ushort) ic >= BUFF_ICON_START_NEW ? (ushort) (ic - (BUFF_ICON_START_NEW - 125)) : (ushort) ((ushort) ic - BUFF_ICON_START);

            if (iconID < BuffTable.Table.Length)
            {
                BuffGump gump = UIManager.GetGump<BuffGump>();
                ushort count = p.ReadUInt16BE();

                if (count == 0)
                {
                    World.Player.RemoveBuff(ic);
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
                        p.Skip(4);
                        string args = p.ReadUnicodeLE();     
                        string title = ClilocLoader.Instance.Translate((int) titleCliloc, args, true);

                        arg_length = p.ReadUInt16BE();
                        string args_2 = p.ReadUnicodeLE();
                        string description = string.Empty;

                        if (descriptionCliloc != 0)
                        {
                            description = "\n" + ClilocLoader.Instance.Translate((int) descriptionCliloc, String.IsNullOrEmpty(args_2) ? args : args_2, true);

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
                            wtf = ClilocLoader.Instance.Translate((int) wtfCliloc, String.IsNullOrEmpty(args_3) ? args : args_3, true);

                            if (!string.IsNullOrWhiteSpace(wtf))
                            {
                                wtf = $"\n{wtf}";
                            }
                        }


                        string text = $"<left>{title}{description}{wtf}</left>";
                        bool alreadyExists = World.Player.IsBuffIconExists(ic);
                        World.Player.AddBuff(ic, BuffTable.Table[iconID], timer, text);

                        if (!alreadyExists)
                        {
                            gump?.RequestUpdateContents();
                        }
                    }
                }
            }
        }

        private static void NewCharacterAnimation(ref StackDataReader p)
        {
            if (World.Player == null)
            {
                return;
            }

            Mobile mobile = World.Mobiles.Get(p.ReadUInt32BE());

            if (mobile == null)
            {
                return;
            }

            ushort type = p.ReadUInt16BE();
            ushort action = p.ReadUInt16BE();
            byte mode = p.ReadUInt8();
            byte group = Mobile.GetObjectNewAnimation(mobile, type, action, mode);
            mobile.SetAnimation(group);
            mobile.AnimationRepeatMode = 1;
            mobile.AnimationForwardDirection = true;

            if ((type == 1 || type == 2) && mobile.Graphic == 0x0015)
            {
                mobile.AnimationRepeat = true;
            }

            mobile.AnimationFromServer = true;
        }

        private static void KREncryptionResponse(ref StackDataReader p)
        {
        }

        private static void DisplayWaypoint(ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();
            ushort x = p.ReadUInt16BE();
            ushort y = p.ReadUInt16BE();
            sbyte z = p.ReadInt8();
            byte map = p.ReadUInt8();
            WaypointsType type = (WaypointsType) p.ReadUInt16BE();
            bool ignoreobject = p.ReadUInt16BE() != 0;
            uint cliloc = p.ReadUInt32BE();
            string name = p.ReadUnicodeLE();
        }

        private static void RemoveWaypoint(ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();
        }

        private static void KrriosClientSpecial(ref StackDataReader p)
        {
            byte type = p.ReadUInt8();

            switch (type)
            {
                case 0x00: // accepted
                    Log.Trace("Krrios special packet accepted");
                    World.WMapManager.SetACKReceived();
                    World.WMapManager.SetEnable(true);

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

                            World.WMapManager.AddOrUpdate
                            (
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

                    World.WMapManager.RemoveUnupdatedWEntity();

                    break;

                case 0x03: // runebook contents
                    break;

                case 0x04: // guardline data
                    break;

                case 0xF0: break;

                case 0xFE:
                    Log.Info("Razor ACK sent");
                    NetClient.Socket.Send_RazorACK();

                    break;
            }
        }

        private static void FreeshardListR(ref StackDataReader p)
        {
        }

        private static void UpdateItemSA(ref StackDataReader p)
        {
            if (World.Player == null)
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
            Direction dir = (Direction) p.ReadUInt8();
            ushort hue = p.ReadUInt16BE();
            Flags flags = (Flags) p.ReadUInt8();
            ushort unk2 = p.ReadUInt16BE();


            if (serial != World.Player)
            {
                UpdateGameObject
                (
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


                if (graphic == 0x2006 && ProfileManager.CurrentProfile.AutoOpenCorpses)
                {
                    World.Player.TryOpenCorpses();
                }
            }
            else if (p[0] == 0xF7)
            {
                UpdatePlayer
                (
                    serial,
                    graphic,
                    graphicInc,
                    hue,
                    flags,
                    x,
                    y,
                    z,
                    0,
                    dir
                );
            }
        }

        private static void BoatMoving(ref StackDataReader p)
        {
            if (!World.InGame)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            byte boatSpeed = p.ReadUInt8();
            Direction movingDirection = (Direction) p.ReadUInt8() & Direction.Mask;
            Direction facingDirection = (Direction) p.ReadUInt8() & Direction.Mask;
            ushort x = p.ReadUInt16BE();
            ushort y = p.ReadUInt16BE();
            ushort z = p.ReadUInt16BE();

            Item multi = World.Items.Get(serial);

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

            bool smooth = ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.UseSmoothBoatMovement;

            if (smooth)
            {
                BoatMovingManager.AddStep
                (
                    serial,
                    boatSpeed,
                    movingDirection,
                    facingDirection,
                    x,
                    y,
                    (sbyte) z
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
                multi.X = x;
                multi.Y = y;
                multi.Z = (sbyte) z;
                multi.AddToTile();
                multi.UpdateScreenPosition();

                if (World.HouseManager.TryGetHouse(serial, out House house))
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

                if (cSerial == World.Player)
                {
                    World.RangeSize.X = cx;
                    World.RangeSize.Y = cy;
                }

                Entity ent = World.Get(cSerial);

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
                    BoatMovingManager.PushItemToList
                    (
                        serial,
                        cSerial,
                        x - cx,
                        y - cy,
                        (sbyte) (z - cz)
                    );
                }
                else
                {
                    if (cSerial == World.Player)
                    {
                        UpdatePlayer
                        (
                            cSerial,
                            ent.Graphic,
                            0,
                            ent.Hue,
                            ent.Flags,
                            cx,
                            cy,
                            (sbyte) cz,
                            0,
                            World.Player.Direction
                        );
                    }
                    else
                    {
                        UpdateGameObject
                        (
                            cSerial,
                            ent.Graphic,
                            0,
                            (ushort) (ent.Graphic == 0x2006 ? ((Item) ent).Amount : 0),
                            cx,
                            cy,
                            (sbyte) cz,
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

        private static void PacketList(ref StackDataReader p)
        {
            if (World.Player == null)
            {
                return;
            }

            int count = p.ReadUInt16BE();

            for (int i = 0; i < count; i++)
            {
                byte id = p.ReadUInt8();

                if (id == 0xF3)
                {
                    UpdateItemSA(ref p);
                }
                else
                {
                    Log.Warn($"Unknown packet ID: [0x{id:X2}] in 0xF7");

                    break;
                }
            }
        }

        private static void ServerListReceived(ref StackDataReader p)
        {
            if (World.InGame)
            {
                return;
            }

            LoginScene scene = Client.Game.GetScene<LoginScene>();

            if (scene != null)
            {
                scene.ServerListReceived(ref p);
            }
        }

        private static void ReceiveServerRelay(ref StackDataReader p)
        {
            if (World.InGame)
            {
                return;
            }

            LoginScene scene = Client.Game.GetScene<LoginScene>();

            if (scene != null)
            {
                scene.HandleRelayServerPacket(ref p);
            }
        }

        private static void UpdateCharacterList(ref StackDataReader p)
        {
            if (World.InGame)
            {
                return;
            }

            LoginScene scene = Client.Game.GetScene<LoginScene>();

            if (scene != null)
            {
                scene.UpdateCharacterList(ref p);
            }
        }

        private static void ReceiveCharacterList(ref StackDataReader p)
        {
            if (World.InGame)
            {
                return;
            }

            LoginScene scene = Client.Game.GetScene<LoginScene>();

            if (scene != null)
            {
                scene.ReceiveCharacterList(ref p);
            }
        }

        private static void ReceiveLoginRejection(ref StackDataReader p)
        {
            if (World.InGame)
            {
                return;
            }

            LoginScene scene = Client.Game.GetScene<LoginScene>();

            if (scene != null)
            {
                scene.HandleErrorCode(ref p);
            }
        }


        private static void AddItemToContainer
        (
            uint serial,
            ushort graphic,
            ushort amount,
            ushort x,
            ushort y,
            ushort hue,
            uint containerSerial
        )
        {
            if (ItemHold.Serial == serial)
            {
                if (ItemHold.Dropped)
                {
                    Console.WriteLine("ADD ITEM TO CONTAINER -- CLEAR HOLD");
                    ItemHold.Clear();
                }

                //else if (ItemHold.Graphic == graphic && ItemHold.Amount == amount &&
                //         ItemHold.Container == containerSerial)
                //{
                //    ItemHold.Enabled = false;
                //    ItemHold.Dropped = false;
                //}
            }

            Entity container = World.Get(containerSerial);

            if (container == null)
            {
                Log.Warn($"No container ({containerSerial}) found");

                //container = World.GetOrCreateItem(containerSerial);
                return;
            }

            Item item = World.Items.Get(serial);

            if (SerialHelper.IsMobile(serial))
            {
                World.RemoveMobile(serial, true);
                Log.Warn("AddItemToContainer function adds mobile as Item");
            }

            if (item != null && (container.Graphic != 0x2006 || item.Layer == Layer.Invalid))
            {
                World.RemoveItem(item, true);
            }

            item = World.GetOrCreateItem(serial);
            item.Graphic = graphic;
            item.CheckGraphicChange();
            item.Amount = amount;
            item.FixHue(hue);
            item.X = x;
            item.Y = y;
            item.Z = 0;

            World.RemoveItemFromContainer(item);
            item.Container = containerSerial;
            container.PushToBack(item);

            if (SerialHelper.IsMobile(containerSerial))
            {
                Mobile m = World.Mobiles.Get(containerSerial);
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
                    NetClient.Socket.Send_BulletinBoardRequestMessageSummary(containerSerial, serial);
                }
                else
                {
                    gump = UIManager.GetGump<SpellbookGump>(containerSerial);

                    if (gump == null)
                    {
                        gump = UIManager.GetGump<ContainerGump>(containerSerial);

                        if (gump != null)
                        {
                            ((ContainerGump) gump).CheckItemControlPosition(item);
                        }

                        if (ProfileManager.CurrentProfile.GridLootType > 0)
                        {
                            GridLootGump grid_gump = UIManager.GetGump<GridLootGump>(containerSerial);

                            if (grid_gump == null && SerialHelper.IsValid(_requestedGridLoot) && _requestedGridLoot == containerSerial)
                            {
                                grid_gump = new GridLootGump(_requestedGridLoot);
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
                            ((Item) container).Opened = true;
                        }

                        gump.RequestUpdateContents();
                    }
                }
            }

            UIManager.GetTradingGump(containerSerial)?.RequestUpdateContents();
        }

        private static void UpdateGameObject
        (
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
            Entity obj = World.Get(serial);

            if (ItemHold.Enabled && ItemHold.Serial == serial)
            {
                if (SerialHelper.IsValid(ItemHold.Container))
                {
                    if (ItemHold.Layer == 0)
                    {
                        UIManager.GetGump<ContainerGump>(ItemHold.Container)?.RequestUpdateContents();
                    }
                    else
                    {
                        UIManager.GetGump<PaperDollGump>(ItemHold.Container)?.RequestUpdateContents();
                    }
                }

                ItemHold.UpdatedInWorld = true;
            }

            bool created = false;

            if (obj == null || obj.IsDestroyed)
            {
                created = true;

                if (SerialHelper.IsMobile(serial) && type != 3)
                {
                    mobile = World.GetOrCreateMobile(serial);

                    if (mobile == null)
                    {
                        return;
                    }

                    obj = mobile;
                    mobile.Graphic = (ushort) (graphic + graphic_inc);
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
                    item = World.GetOrCreateItem(serial);

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
                        World.RemoveItemFromContainer(item);
                    }
                }
                else
                {
                    mobile = (Mobile) obj;
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

                    item.WantUpdateMulti = (graphic & 0x3FFF) != item.Graphic || item.X != x || item.Y != y || item.Z != z;

                    item.Graphic = (ushort) (graphic & 0x3FFF);
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
                item.LightID = (byte) direction;

                if (graphic == 0x2006)
                {
                    item.Layer = (Layer) direction;
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

                if (serial != World.Player)
                {
                    Direction cleaned_dir = direction & Direction.Up;
                    bool isrun = (direction & Direction.Running) != 0;

                    if (World.Get(mobile) == null || mobile.X == 0xFFFF && mobile.Y == 0xFFFF)
                    {
                        mobile.X = x;
                        mobile.Y = y;
                        mobile.Z = z;
                        mobile.Direction = cleaned_dir;
                        mobile.IsRunning = isrun;
                        mobile.ClearSteps();
                    }

                    if (!mobile.EnqueueStep
                    (
                        x,
                        y,
                        z,
                        cleaned_dir,
                        isrun
                    ))
                    {
                        mobile.X = x;
                        mobile.Y = y;
                        mobile.Z = z;
                        mobile.Direction = cleaned_dir;
                        mobile.IsRunning = isrun;
                        mobile.ClearSteps();
                    }
                }

                mobile.Graphic = (ushort) (graphic & 0x3FFF);
                mobile.FixHue(hue);
                mobile.Flags = flagss;
            }

            if (created && !obj.IsClicked)
            {
                if (mobile != null)
                {
                    if (ProfileManager.CurrentProfile.ShowNewMobileNameIncoming)
                    {
                        GameActions.SingleClick(serial);
                    }
                }
                else if (graphic == 0x2006)
                {
                    if (ProfileManager.CurrentProfile.ShowNewCorpseNameIncoming)
                    {
                        GameActions.SingleClick(serial);
                    }
                }
            }

            if (mobile != null)
            {
                mobile.AddToTile();
                mobile.UpdateScreenPosition();

                if (created)
                {
                    // This is actually a way to get all Hp from all new mobiles.
                    // Real UO client does it only when LastAttack == serial.
                    // We force to close suddenly.
                    GameActions.RequestMobileStatus(serial);

                    //if (TargetManager.LastAttack != serial)
                    //{
                    //    GameActions.SendCloseStatus(serial);
                    //}
                }
            }
            else
            {
                if (ItemHold.Serial == serial && ItemHold.Dropped)
                {
                    // we want maintain the item data due to the denymoveitem packet
                    //ItemHold.Clear();
                    ItemHold.Enabled = false;
                    ItemHold.Dropped = false;
                }

                if (item.OnGround)
                {
                    item.AddToTile();
                    item.UpdateScreenPosition();

                    if (graphic == 0x2006 && ProfileManager.CurrentProfile.AutoOpenCorpses)
                    {
                        World.Player.TryOpenCorpses();
                    }
                }
            }
        }

        private static void UpdatePlayer
        (
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
            if (serial == World.Player)
            {
                World.Player.CloseBank();

                World.Player.Walker.WalkingFailed = false;

                World.Player.X = x;
                World.Player.Y = y;
                World.Player.Z = z;

                World.RangeSize.X = x;
                World.RangeSize.Y = y;

                bool olddead = World.Player.IsDead;
                ushort old_graphic = World.Player.Graphic;

                World.Player.Graphic = graphic;
                World.Player.Direction = direction & Direction.Mask;
                World.Player.FixHue(hue);

                World.Player.Flags = flags;

                World.Player.Walker.DenyWalk(0xFF, -1, -1, -1);
                GameScene gs = Client.Game.GetScene<GameScene>();

                if (gs != null)
                {
                    gs.Weather.Reset();
                    gs.UpdateDrawPosition = true;
                }

                if (old_graphic != 0 && old_graphic != World.Player.Graphic)
                {
                    if (World.Player.IsDead)
                    {
                        TargetManager.Reset();
                    }
                }

                if (olddead != World.Player.IsDead)
                {
                    if (World.Player.IsDead)
                    {
                        World.ChangeSeason(Game.Managers.Season.Desolation, 42);
                    }
                    else
                    {
                        World.ChangeSeason(World.OldSeason, World.OldMusicIndex);
                    }
                }

                World.Player.Walker.ResendPacketResync = false;
                World.Player.CloseRangedGumps();

                World.Player.UpdateScreenPosition();
                World.Player.AddToTile();

                World.Player.UpdateAbilities();
            }
        }


        private static void ClearContainerAndRemoveItems(Entity container, bool remove_unequipped = false)
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
                Item it = (Item) first;

                if (remove_unequipped && it.Layer != 0)
                {
                    if (new_first == null)
                    {
                        new_first = first;
                    }
                }
                else
                {
                    World.RemoveItem(it, true);
                }

                first = next;
            }

            container.Items = remove_unequipped ? new_first : null;
        }

        private static Gump CreateGump
        (
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

                for (LinkedListNode<Gump> last = UIManager.Gumps.Last; last != null; last = last.Previous)
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
                gump = new Gump(sender, gumpID)
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
                else if (string.Equals(entry, "buttontileart", StringComparison.InvariantCultureIgnoreCase))
                {
                    gump.Add(new ButtonTileArt(gparams), page);
                }
                else if (string.Equals(entry, "checkertrans", StringComparison.InvariantCultureIgnoreCase))
                {
                    var checkerTrans = new CheckerTrans(gparams);
                    gump.Add(checkerTrans, page);
                    ApplyTrans(gump, page, checkerTrans.X, checkerTrans.Y, checkerTrans.Width, checkerTrans.Height);
                }
                else if (string.Equals(entry, "croppedtext", StringComparison.InvariantCultureIgnoreCase))
                {
                    gump.Add(new CroppedText(gparams, lines), page);
                }
                else if (string.Equals(entry, "gumppic", StringComparison.InvariantCultureIgnoreCase))
                {
                    GumpPic pic = new GumpPic(gparams);

                    if (gparams.Count >= 6 && gparams[5].IndexOf("virtuegumpitem", StringComparison.InvariantCultureIgnoreCase) >= 0)
                    {
                        pic.ContainsByBounds = true;
                        pic.IsVirtue = true;

                        string s, lvl;

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
                                s = ClilocLoader.Instance.GetString(1051000 + 2);

                                break;

                            case 0x6A:
                                s = ClilocLoader.Instance.GetString(1051000 + 7);

                                break;

                            case 0x6B:
                                s = ClilocLoader.Instance.GetString(1051000 + 5);

                                break;

                            case 0x6D:
                                s = ClilocLoader.Instance.GetString(1051000 + 6);

                                break;

                            case 0x6E:
                                s = ClilocLoader.Instance.GetString(1051000 + 1);

                                break;

                            case 0x6F:
                                s = ClilocLoader.Instance.GetString(1051000 + 3);

                                break;

                            case 0x70:
                                s = ClilocLoader.Instance.GetString(1051000 + 4);

                                break;

                            case 0x6C:
                            default:
                                s = ClilocLoader.Instance.GetString(1051000);

                                break;
                        }

                        if (string.IsNullOrEmpty(s))
                        {
                            s = "Unknown virtue";
                        }

                        pic.SetTooltip(lvl + s, 100);
                    }

                    gump.Add(pic, page);
                }
                else if (string.Equals(entry, "gumppictiled", StringComparison.InvariantCultureIgnoreCase))
                {
                    gump.Add(new GumpPicTiled(gparams), page);
                }
                else if (string.Equals(entry, "htmlgump", StringComparison.InvariantCultureIgnoreCase))
                {
                    gump.Add(new HtmlControl(gparams, lines), page);
                }
                else if (string.Equals(entry, "xmfhtmlgump", StringComparison.InvariantCultureIgnoreCase))
                {
                    gump.Add
                    (
                        new HtmlControl
                            (
                                int.Parse(gparams[1]),
                                int.Parse(gparams[2]),
                                int.Parse(gparams[3]),
                                int.Parse(gparams[4]),
                                int.Parse(gparams[6]) == 1,
                                int.Parse(gparams[7]) != 0,
                                gparams[6] != "0" && gparams[7] == "2",
                                ClilocLoader.Instance.GetString(int.Parse(gparams[5].Replace("#", ""))),
                                0,
                                true
                            )
                            { IsFromServer = true },
                        page
                    );
                }
                else if (string.Equals(entry, "xmfhtmlgumpcolor", StringComparison.InvariantCultureIgnoreCase))
                {
                    int color = int.Parse(gparams[8]);

                    if (color == 0x7FFF)
                    {
                        color = 0x00FFFFFF;
                    }

                    gump.Add
                    (
                        new HtmlControl
                            (
                                int.Parse(gparams[1]),
                                int.Parse(gparams[2]),
                                int.Parse(gparams[3]),
                                int.Parse(gparams[4]),
                                int.Parse(gparams[6]) == 1,
                                int.Parse(gparams[7]) != 0,
                                gparams[6] != "0" && gparams[7] == "2",
                                ClilocLoader.Instance.GetString(int.Parse(gparams[5].Replace("#", ""))),
                                color,
                                true
                            )
                            { IsFromServer = true },
                        page
                    );
                }
                else if (string.Equals(entry, "xmfhtmltok", StringComparison.InvariantCultureIgnoreCase))
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

                    gump.Add
                    (
                        new HtmlControl
                            (
                                int.Parse(gparams[1]),
                                int.Parse(gparams[2]),
                                int.Parse(gparams[3]),
                                int.Parse(gparams[4]),
                                int.Parse(gparams[5]) == 1,
                                int.Parse(gparams[6]) != 0,
                                gparams[5] != "0" && gparams[6] == "2",
                                sb == null ? ClilocLoader.Instance.GetString(int.Parse(gparams[8].Replace("#", ""))) : ClilocLoader.Instance.Translate(int.Parse(gparams[8].Replace("#", "")), sb.ToString().Trim('@').Replace('@', '\t')),
                                color,
                                true
                            )
                            { IsFromServer = true },
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
                else if (string.Equals(entry, "resizepic", StringComparison.InvariantCultureIgnoreCase))
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
                else if (string.Equals(entry, "textentrylimited", StringComparison.InvariantCultureIgnoreCase) ||
                         string.Equals(entry, "textentry", StringComparison.InvariantCultureIgnoreCase))
                {
                    StbTextBox textBox = new StbTextBox(gparams, lines);

                    if (!textBoxFocused)
                    {
                        textBox.SetKeyboardFocus();
                        textBoxFocused = true;
                    }

                    gump.Add(textBox, page);
                }
                else if (string.Equals(entry, "tilepichue", StringComparison.InvariantCultureIgnoreCase) ||
                         string.Equals(entry, "tilepic", StringComparison.InvariantCultureIgnoreCase))
                {
                    gump.Add(new StaticPic(gparams), page);
                }
                else if (string.Equals(entry, "noclose", StringComparison.InvariantCultureIgnoreCase))
                {
                    gump.CanCloseWithRightClick = false;
                }
                else if (string.Equals(entry, "nodispose", StringComparison.InvariantCultureIgnoreCase))
                {
                    gump.CanCloseWithEsc = false;
                }
                else if (string.Equals(entry, "nomove", StringComparison.InvariantCultureIgnoreCase))
                {
                    gump.BlockMovement = true;
                }
                else if (string.Equals(entry, "group", StringComparison.InvariantCultureIgnoreCase) ||
                         string.Equals(entry, "endgroup", StringComparison.InvariantCultureIgnoreCase))
                {
                    group++;
                }
                else if (string.Equals(entry, "radio", StringComparison.InvariantCultureIgnoreCase))
                {
                    gump.Add(new RadioButton(group, gparams, lines), page);
                }
                else if (string.Equals(entry, "checkbox", StringComparison.InvariantCultureIgnoreCase))
                {
                    gump.Add(new Checkbox(gparams, lines), page);
                }
                else if (string.Equals(entry, "tooltip", StringComparison.InvariantCultureIgnoreCase))
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
                            text = ClilocLoader.Instance.GetString(int.Parse(gparams[1]));
                            Log.Error($"String '{args}' too short, something wrong with gump tooltip: {text}");
                        }
                        else
                        {
                            text = ClilocLoader.Instance.Translate(int.Parse(gparams[1]), args, false);
                        }
                    }
                    else
                    {
                        text = ClilocLoader.Instance.GetString(int.Parse(gparams[1]));
                    }

                    Control last = gump.Children.Count != 0 ? gump.Children[gump.Children.Count - 1] : null;

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
                else if (string.Equals(entry, "itemproperty", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (World.ClientFeatures.TooltipsEnabled && gump.Children.Count != 0)
                    {
                        gump.Children[gump.Children.Count - 1].SetTooltip(SerialHelper.Parse(gparams[1]));

                        if (uint.TryParse(gparams[1], out uint s) && (!World.OPL.TryGetRevision(s, out uint rev) || rev == 0))
                        {
                            AddMegaClilocRequest(s);
                        }
                    }
                }
                else if (string.Equals(entry, "noresize", StringComparison.InvariantCultureIgnoreCase))
                {

                }
                else if (string.Equals(entry, "mastergump", StringComparison.InvariantCultureIgnoreCase))
                {
                    gump.MasterGumpSerial = gparams.Count > 0 ? SerialHelper.Parse(gparams[1]) : 0;
                }
                else
                {
                    Log.Warn(gparams[0]);
                }
            }

            if (mustBeAdded)
            {
                UIManager.Add(gump);
            }

            gump.Update(Time.Ticks, 0);
            gump.SetInScreen();

            return gump;
        }

        private static void ApplyTrans(Gump gump, int current_page, int x, int y, int width, int height)
        {
            int x2 = x + width;
            int y2 = y + height;
            for (int i = 0; i < gump.Children.Count; i++)
            {
                Control child = gump.Children[i];
                bool canDraw = child.Page == 0 || current_page == child.Page;

                bool overlap = (x < child.X + child.Width) && (child.X < x2) && (y < child.Y + child.Height) && (child.Y < y2);

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