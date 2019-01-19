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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Map;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Audio;
using ClassicUO.IO.Resources;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;

using Multi = ClassicUO.Game.GameObjects.Multi;

namespace ClassicUO.Network
{
    internal class PacketHandler
    {
        public PacketHandler(Action<Packet> callback)
        {
            Callback = callback;
        }

        public Action<Packet> Callback { get; }
    }

    internal class PacketHandlers
    {
        private readonly List<PacketHandler>[] _handlers = new List<PacketHandler>[0x100];

        static PacketHandlers()
        {
            ToClient = new PacketHandlers();
            ToServer = new PacketHandlers();
            NetClient.PacketReceived += ToClient.OnPacket;
            NetClient.PacketSended += ToServer.OnPacket;
        }

        private PacketHandlers()
        {
            for (int i = 0; i < _handlers.Length; i++) _handlers[i] = new List<PacketHandler>();
        }

        public static PacketHandlers ToClient { get; }

        public static PacketHandlers ToServer { get; }

        public void Add(byte id, Action<Packet> handler)
        {
            lock (_handlers) _handlers[id].Add(new PacketHandler(handler));
        }

        public void Remove(byte id, Action<Packet> handler)
        {
            lock (_handlers) _handlers[id].Remove(_handlers[id].FirstOrDefault(s => s.Callback == handler));
        }

        private void OnPacket(object sender, Packet p)
        {
            lock (_handlers)
            {
                for (int i = 0; i < _handlers[p.ID].Count; i++)
                {
                    //if (Engine.Server.IsConnected)
                    //    Engine.Server.SendToPlugin(p.ToArray(), p.Length, p.IsDynamic, false);

                    p.MoveToData();
                    _handlers[p.ID][i].Callback(p);
                }
            }
        }

        public static void Load()
        {
            ToClient.Add(0x1B, EnterWorld);
            ToClient.Add(0x55, LoginComplete);
            ToClient.Add(0x82, LoginError);
            ToClient.Add(0x86, ResendCharacterList);
            ToClient.Add(0x8C, RelayServer);
            ToClient.Add(0xA8, ServerList);
            ToClient.Add(0xA9, CharacterList);
            ToClient.Add(0xBD, ClientVersion);
            ToClient.Add(0x03, ClientTalk);
            /*ToServer.Add(0x00, CreateCharacter);
            ToServer.Add(0x01, Disconnect);
            ToServer.Add(0x02, MoveRequest);
            ToServer.Add(0x03, TalkRequest);
            ToServer.Add(0x04, RequestGodMode);
            ToServer.Add(0x05, RequestAttack);
            ToServer.Add(0x06, DoubleClick);
            ToServer.Add(0x07, PickUpItem);
            ToServer.Add(0x08, DropItem);
            ToServer.Add(0x09, SingleClick);
            ToServer.Add(0x0A, EditGodMode);*/
            ToClient.Add(0x0B, Damage);
            ToClient.Add(0x0C, EditTileDataGodClientR); /*ToServer.Add(0x0C, EditTileDataGodClientS);*/
            ToClient.Add(0x11, CharacterStatus);
            /* ToServer.Add(0x12, RequestSkill);
             ToServer.Add(0x13, WearItem);
             ToServer.Add(0x14, SendElevationGodClient);
             ToServer.Add(0x15 FollowS);*/
            ToClient.Add(0x15, FollowR);
            ToClient.Add(0x16, /*NewHealthBarStatusUpdateSA*/ NewHealthbarUpdate);
            ToClient.Add(0x17, NewHealthbarUpdate);
            ToClient.Add(0x1A, UpdateItem);
            // *** ToClient.Add(0x1B, EnterWorld);
            ToClient.Add(0x1C, Talk);
            ToClient.Add(0x1D, DeleteObject);
            ToClient.Add(0x1F, Explosion);
            ToClient.Add(0x20, UpdatePlayer);
            ToClient.Add(0x21, DenyWalk);
            ToClient.Add(0x22, ConfirmWalk); /*ToServer.Add(0x22, ResyncRequest);*/
            ToClient.Add(0x23, DragAnimation);
            ToClient.Add(0x24, OpenContainer);
            ToClient.Add(0x25, UpdateContainedItem);
            ToClient.Add(0x26, KickPlayer);
            ToClient.Add(0x27, DenyMoveItem);
            ToClient.Add(0x28, EndDraggingItem);
            ToClient.Add(0x29, DropItemAccepted);
            ToClient.Add(0x2A, Blood);
            ToClient.Add(0x2B, GodMode);
            ToClient.Add(0x2C, DeathScreen);
            ToClient.Add(0x2D, MobileAttributes);
            ToClient.Add(0x2E, EquipItem);
            ToClient.Add(0x2F, FightOccuring);
            ToClient.Add(0x30, AttackOK);
            ToClient.Add(0x31, AttackEnded);
            ToClient.Add(0x32, p => { }); // unknown
            ToClient.Add(0x33, PauseControl);
            /*ToServer.Add(0x34, GetPlayerStatus);
            ToServer.Add(0x35, AddResourceGodClient);*/
            ToClient.Add(0x36, ResourceTileDataGodClient);
            ToClient.Add(0x38, Pathfinding);
            /*ToServer.Add(0x37, MoveItemGodClient);
            ToServer.Add(0x38, PathfindingInClient);
            ToServer.Add(0x39, RemoveGroupS);*/
            ToClient.Add(0x39, RemoveGroupR);
            /*ToServer.Add(0x3A, SendSkills);*/
            ToClient.Add(0x3A, UpdateSkills);
            //ToServer.Add(0x3B, BuyItems);
            ToClient.Add(0x3C, UpdateContainedItems);
            ToClient.Add(0x3E, VersionGodClient);
            /*ToServer.Add(0x45, VersionOK);
            ToServer.Add(0x46, NewArtwork);
            ToServer.Add(0x47, NewTerrain);
            ToServer.Add(0x48, NewAnimation);
            ToServer.Add(0x49, NewHues);
            ToServer.Add(0x4A, DeleteArt);
            ToServer.Add(0x4B, CheckClientVersion);
            ToServer.Add(0x4C, ScriptNames);
            ToServer.Add(0x4D, EditScriptFile);*/
            ToClient.Add(0x4E, PersonalLightLevel);
            ToClient.Add(0x4F, LightLevel);
            /*ToServer.Add(0x50, BoardHeader);
            ToServer.Add(0x51, BoardMessage);
            ToServer.Add(0x52, BoardPostMessage);*/
            ToClient.Add(0x53, ErrorCode);
            ToClient.Add(0x54, PlaySoundEffect);
            // *** ToClient.Add(0x55, LoginComplete);
            ToClient.Add(0x56, MapData); //ToServer.Add(0x56, MapPacketTreauseCartographyS);
            /*ToServer.Add(0x57, UpdateRegions);
            ToServer.Add(0x58, AddRegion);
            ToServer.Add(0x59, NewContextFX);
            ToServer.Add(0x5A, UpdateContextFX);*/
            ToClient.Add(0x5B, SetTime);
            /*ToServer.Add(0x5C, RestartVersion);
            ToServer.Add(0x5D, LoginCharacter);
            ToServer.Add(0x5E, ServerListing);
            ToServer.Add(0x5F, ServerListAddEntry);
            ToServer.Add(0x60, ServerListRemoveEntry);
            ToServer.Add(0x61, RemoveStaticObject);
            ToServer.Add(0x62, MoveStaticObject);
            ToServer.Add(0x63, LoadArea);
            ToServer.Add(0x64, LoadAreaRequest);*/
            ToClient.Add(0x65, SetWeather);
            ToClient.Add(0x66, BookData); //ToServer.Add(0x66, BookPagesS);
            //ToServer.Add(0x69, ChangeText);
            ToClient.Add(0x6C, TargetCursor);
            ToClient.Add(0x6D, PlayMusic);
            ToClient.Add(0x6F, SecureTrading);
            ToClient.Add(0x6E, CharacterAnimation);
            ToClient.Add(0x70, GraphicEffect);
            ToClient.Add(0x71, BulletinBoardData); //ToServer.Add(0x71, BulletinBoardMessagesS);
            ToClient.Add(0x72, Warmode); // ToServer.Add(0x72, RequestWarMode);
            ToClient.Add(0x73, Ping); //ToServer.Add(0x73, PingS);
            ToClient.Add(0x74, BuyList);
            //ToServer.Add(0x75, RenameCharacter);
            ToClient.Add(0x76, NewSubServer);
            ToClient.Add(0x77, UpdateCharacter);
            ToClient.Add(0x78, UpdateObject);
            ToClient.Add(0x7C, OpenMenu);
            /*ToServer.Add(0x7D, ResponseToDialogBox);
            ToServer.Add(0x80, LoginRequest);*/
            // *** ToClient.Add(0x82, LoginError);
            //ToServer.Add(0x83, DeleteCharacter);
            // *** ToClient.Add(0x86, ResendCharacterList);
            ToClient.Add(0x88, OpenPaperdoll);
            ToClient.Add(0x89, CorpseEquipment);
            // *** ToClient.Add(0x8C, RelayServer);
            ToClient.Add(0x90, DisplayMap);
            //ToServer.Add(0x91, GameServerLogin);
            ToClient.Add(0x93, OpenBook); //ToServer.Add(0x93, BookHeaderOldS);
            ToClient.Add(0x95, DyeData); //ToServer.Add(0x95, DyeWindowS);
            ToClient.Add(0x97, MovePlayer);
            ToClient.Add(0x98, AllNames3DGameOnlyR); //ToServer.Add(0x98, AllNames3DGameOnlyS);
            ToClient.Add(0x99, MultiPlacement); //ToServer.Add(0x99, RequestBoatAndHousePlacement);
            ToClient.Add(0x9A, ASCIIPrompt); //ToServer.Add(0x9A, ConsoleEntryPromptS);
            //ToServer.Add(0x9B, RequestHelp);
            ToClient.Add(0x9C, RequestAssistance);
            ToClient.Add(0x9E, SellList);
            /*ToServer.Add(0x9F, SellListReply);
            ToServer.Add(0xA0, SelectServer);*/
            ToClient.Add(0xA1, UpdateHitpoints);
            ToClient.Add(0xA2, UpdateMana);
            ToClient.Add(0xA3, UpdateStamina);
            //ToServer.Add(0xA4, ClientSpy);
            ToClient.Add(0xA5, OpenUrl);
            ToClient.Add(0xA6, TipWindow);
            //ToServer.Add(0xA7, RequestNoticeWindow);
            // *** ToClient.Add(0xA8, ServerList);
            // *** ToClient.Add(0xA9, CharacterList);
            ToClient.Add(0xAA, AttackCharacter);
            ToClient.Add(0xAB, TextEntryDialog);
            /*ToServer.Add(0xAC, GumpTextEntryDialogReply);
            ToServer.Add(0xAD, UnicodeAsciiSpeechRequest);*/
            ToClient.Add(0xAE, UnicodeTalk);
            ToClient.Add(0xB0, OpenGump);
            //ToServer.Add(0xB1, GumpMenuSelection);
            ToClient.Add(0xB2, ChatMessage);
            /*ToServer.Add(0xB3, ChatText);
            ToServer.Add(0xB5, OpenChatWindow);
            ToServer.Add(0xB6, SendHelpRequest);*/
            ToClient.Add(0xB7, Help);
            ToClient.Add(0xB8, CharacterProfile); //ToServer.Add(0xB8, RequestCharProfile);
            ToClient.Add(0xB9, EnableLockedFeatures);
            ToClient.Add(0xBA, DisplayQuestArrow);
            ToClient.Add(0xBB, UltimaMessengerR); //ToServer.Add(0xBB, UltimaMessengerS);
            ToClient.Add(0xBC, Season);
            // *** ToClient.Add(0xBD, ClientVersion); //ToServer.Add(0xBD, ClientVersionS);
            ToClient.Add(0xBE, AssistVersion); // ToServer.Add(0xBE, AssistVersionS);
            ToClient.Add(0xBF, ExtendedCommand); //ToServer.Add(0xBF, GeneralInformationPacketS);
            ToClient.Add(0xC0, GraphicEffect);
            ToClient.Add(0xC1, DisplayClilocString);
            ToClient.Add(0xC2, UnicodePrompt); //ToServer.Add(0xC2, UnicodeTextEntryS);
            ToClient.Add(0xC4, Semivisible);
            //ToServer.Add(0xC5, validMapRequest);
            ToClient.Add(0xC6, InvalidMapEnable);
            ToClient.Add(0xC7, GraphicEffect);
            ToClient.Add(0xC8, ClientViewRange);
            ToClient.Add(0xCA, GetUserServerPingGodClientR); //ToServer.Add(0xCA, GetUserServerPingGodClientS);
            ToClient.Add(0xCB, GlobalQueCount);
            ToClient.Add(0xCC, DisplayClilocString);
            ToClient.Add(0xD0, ConfigurationFileR); //ToServer.Add(0xD0, ConfigurationFileS);
            ToClient.Add(0xD1, Logout); //ToServer.Add(0xD1, LogoutStatusS);
            ToClient.Add(0xD2, UpdateCharacter);
            ToClient.Add(0xD3, UpdateObject);
            ToClient.Add(0xD4, OpenBook); //ToServer.Add(0xD4, BookHeaderNewS);
            ToClient.Add(0xD6, MegaCliloc); //ToServer.Add(0xD6, MegaClilocS);
            ToClient.Add(0xD7, GenericAOSCommandsR); //ToServer.Add(0xD7, GenericAOSCommandsS);
            ToClient.Add(0xD8, CustomHouse);
            //ToServer.Add(0xD9, SpyOnClient);
            ToClient.Add(0xDB, CharacterTransferLog);
            ToClient.Add(0xDC, OPLInfo);
            ToClient.Add(0xDD, OpenCompressedGump);
            ToClient.Add(0xDE, UpdateMobileStatus);
            ToClient.Add(0xDF, BuffDebuff);
            /*ToServer.Add(0xE0, BugReportKR);
            ToServer.Add(0xE1, ClientTypeKRSA);*/
            ToClient.Add(0xE2, NewCharacterAnimation);
            ToClient.Add(0xE3, KREncryptionResponse);
            /*ToServer.Add(0xEC, EquipMacroKR);
            ToServer.Add(0xED, UnequipItemMacroKR);
            ToServer.Add(0xEF, KR2DClientLoginSeed);*/
            ToClient.Add(0xF0, KrriosClientSpecial);
            ToClient.Add(0xF1, FreeshardListR); //ToServer.Add(0xF1, FreeshardListS);
            ToClient.Add(0xF3, UpdateItemSA);
            ToClient.Add(0xF5, DisplayMap);
            //ToServer.Add(0xF8, CharacterCreation_7_0_16_0);
            ToClient.Add(0xF7, PacketList);
        }

        private static void TargetCursor(Packet p)
        {
            TargetManager.SetTargeting((TargetType)p.ReadByte(), p.ReadUInt(), p.ReadByte());
        }

        private static void SecureTrading(Packet p)
        {
            if (!World.InGame)
                return;

            byte type = p.ReadByte();
            Serial serial = p.ReadUInt();

            if (type == 0)
            {
                Serial id1 = p.ReadUInt();
                Serial id2 = p.ReadUInt();
                bool hasName = p.ReadBool();
                string name = string.Empty;

                if (hasName && p.Position < p.Length)
                    name = p.ReadASCII();

                Engine.UI.Add(new TradingGump(serial, name, id1, id2));
            }
            else if (type == 1)
            {
                Engine.UI.Gumps.OfType<TradingGump>().FirstOrDefault(s => s.ID1 == serial || s.ID2 == serial)?.Dispose();
            }
            else if (type == 2)
            {
                Serial id1 = p.ReadUInt();
                Serial id2 = p.ReadUInt();

                TradingGump trading = Engine.UI.Gumps.OfType<TradingGump>().FirstOrDefault(s => s.ID1 == serial || s.ID2 == serial);

                if (trading != null)
                {
                    trading.ImAccepting = id1 != 0;
                    trading.HeIsAccepting = id2 != 0;
                }
            }
        }

        private static void ClientTalk(Packet p)
        {
            switch (p.ReadByte())
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

        private static void Damage(Packet p)
        {
            if (World.Player == null)
                return;

            Mobile mobile = World.Mobiles.Get(p.ReadUInt());

            if (mobile != null)
            {
                ushort damage = p.ReadUShort();

                Engine.SceneManager.GetScene<GameScene>().Overheads.AddDamage(mobile, new DamageOverhead(mobile, damage.ToString(), hue: (Hue)(mobile == World.Player ? 0x0034 : 0x0021), font: 3, isunicode: false, timeToLive: 1500));
            }

            //World.Mobiles.Get(p.ReadUInt())?.AddDamage(p.ReadUShort());
        }

        private static void EditTileDataGodClientR(Packet p)
        {
        }

        private static void CharacterStatus(Packet p)
        {
            if (World.Player == null)
                return;
            Mobile mobile = World.Mobiles.Get(p.ReadUInt());

            if (mobile == null) return;
            mobile.Name = p.ReadASCII(30);
            mobile.Hits = p.ReadUShort();
            mobile.HitsMax = p.ReadUShort();
            mobile.IsRenamable = p.ReadBool();
            byte type = p.ReadByte();

            if (type > 0)
            {
                World.Player.Female = p.ReadBool();
                World.Player.Strength = p.ReadUShort();
                World.Player.Dexterity = p.ReadUShort();
                World.Player.Intelligence = p.ReadUShort();
                World.Player.Stamina = p.ReadUShort();
                World.Player.StaminaMax = p.ReadUShort();
                World.Player.Mana = p.ReadUShort();
                World.Player.ManaMax = p.ReadUShort();
                World.Player.Gold = p.ReadUInt();
                World.Player.ResistPhysical = p.ReadUShort();
                World.Player.Weight = p.ReadUShort();

                if (type >= 5) //ML
                {
                    World.Player.WeightMax = p.ReadUShort();
                    byte race = p.ReadByte();
                    if (race <= 0) race = 1;
                    World.Player.Race = (RaceType)race;
                }
                else
                {
                    if (FileManager.ClientVersion >= ClientVersions.CV_500A)
                        World.Player.WeightMax = (ushort)(7 * (World.Player.Strength >> 1) + 40);
                    else
                        World.Player.WeightMax = (ushort)(World.Player.Strength * 4 + 25);
                }

                if (type >= 3) //Renaissance
                {
                    World.Player.StatsCap = p.ReadUShort();
                    World.Player.Followers = p.ReadByte();
                    World.Player.FollowersMax = p.ReadByte();
                }

                if (type >= 4) //AOS
                {
                    World.Player.ResistFire = p.ReadUShort();
                    World.Player.ResistCold = p.ReadUShort();
                    World.Player.ResistPoison = p.ReadUShort();
                    World.Player.ResistEnergy = p.ReadUShort();
                    World.Player.Luck = p.ReadUShort();
                    World.Player.DamageMin = p.ReadUShort();
                    World.Player.DamageMax = p.ReadUShort();
                    World.Player.TithingPoints = p.Length == p.Position ? 0 : p.ReadUInt();
                }

                if (type >= 6)
                {
                    World.Player.MaxPhysicRes = p.Position + 2 > p.Length ? (ushort)0 : p.ReadUShort();
                    World.Player.MaxFireRes = p.Position + 2 > p.Length ? (ushort)0 : p.ReadUShort();
                    World.Player.MaxColdRes = p.Position + 2 > p.Length ? (ushort)0 : p.ReadUShort();
                    World.Player.MaxPoisonRes = p.Position + 2 > p.Length ? (ushort)0 : p.ReadUShort();
                    World.Player.MaxEnergyRes = p.Position + 2 > p.Length ? (ushort)0 : p.ReadUShort();
                    World.Player.DefenseChanceInc = p.Position + 2 > p.Length ? (ushort)0 : p.ReadUShort();
                    World.Player.MaxDefChance = p.Position + 2 > p.Length ? (ushort)0 : p.ReadUShort();
                    World.Player.HitChanceInc = p.Position + 2 > p.Length ? (ushort)0 : p.ReadUShort();
                    World.Player.SwingSpeedInc = p.Position + 2 > p.Length ? (ushort)0 : p.ReadUShort();
                    World.Player.DamageIncrease = p.Position + 2 > p.Length ? (ushort)0 : p.ReadUShort();
                    World.Player.LowerReagentCost = p.Position + 2 > p.Length ? (ushort)0 : p.ReadUShort();
                    World.Player.SpellDamageInc = p.Position + 2 > p.Length ? (ushort)0 : p.ReadUShort();
                    World.Player.FasterCastRecovery = p.Position + 2 > p.Length ? (ushort)0 : p.ReadUShort();
                    World.Player.FasterCasting = p.Position + 2 > p.Length ? (ushort)0 : p.ReadUShort();
                    World.Player.LowerManaCost = p.Position + 2 > p.Length ? (ushort)0 : p.ReadUShort();
                }
            }

            mobile.ProcessDelta();
        }

        private static void FollowR(Packet p)
        {
            Serial tofollow = p.ReadUInt();
            Serial isfollowing = p.ReadUInt();
        }

        private static void NewHealthbarUpdate(Packet p)
        {
            if (World.Player == null)
                return;

            if (p.ID == 0x16 && FileManager.ClientVersion < ClientVersions.CV_500A)
                return;
            Mobile mobile = World.Mobiles.Get(p.ReadUInt());

            if (mobile == null) return;
            ushort count = p.ReadUShort();

            for (int i = 0; i < count; i++)
            {
                ushort type = p.ReadUShort();
                bool enabled = p.ReadBool();
                byte flags = (byte)mobile.Flags;

                if (type == 1)
                {
                    if (enabled)
                    {
                        if (FileManager.ClientVersion >= ClientVersions.CV_7000)
                            mobile.SetSAPoison(true);
                        else
                            flags |= 0x04;
                    }
                    else
                    {
                        if (FileManager.ClientVersion >= ClientVersions.CV_7000)
                            mobile.SetSAPoison(false);
                        else
                            flags = (byte)(flags & ~0x04);
                    }
                }
                else if (type == 2)
                {
                    if (enabled)
                        flags |= 0x08;
                    else
                        flags &= (byte)(flags & ~0x08);
                }
                else if (type == 3)
                {
                }

                mobile.Flags = (Flags)flags;
            }

            mobile.ProcessDelta();
        }

        private static void UpdateItem(Packet p)
        {
            if (World.Player == null) return;
            uint serial = p.ReadUInt();
            ushort count = 0;
            byte graphicInc = 0;
            byte direction = 0;
            ushort hue = 0;
            byte flags = 0;

            if ((serial & 0x80000000) != 0)
            {
                serial &= 0x7FFFFFFF;
                count = 1;
            }

            Item item = World.GetOrCreateItem(serial);
            ushort graphic = p.ReadUShort();

            if ((graphic & 0x8000) != 0)
            {
                graphic &= 0x7FFF;
                graphicInc = p.ReadByte();
            }

            if (count > 0)
                count = p.ReadUShort();
            else
                count++;
            ushort x = p.ReadUShort();

            if ((x & 0x8000) != 0)
            {
                x &= 0x7FFF;
                direction = 1;
            }

            ushort y = p.ReadUShort();

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

            if (direction > 0) direction = p.ReadByte();
            sbyte z = p.ReadSByte();
            if (hue > 0) hue = p.ReadUShort();
            if (flags > 0) flags = p.ReadByte();
            if (graphic != 0x2006) graphic += graphicInc;
            item.Graphic = graphic;
            item.Amount = count;
            item.Position = new Position(x, y, z);
            item.Hue = hue;
            item.Flags = (Flags)flags;
            item.Direction = (Direction)direction;

            if (graphic >= 0x4000)
            {
                item.Graphic -= 0x4000;
                item.IsMulti = true;
            }

            item.Container = Serial.INVALID;
            item.ProcessDelta();


            if (World.Items.Add(item)) World.Items.ProcessDelta();

            if (item.ItemData.IsAnimated)
            {
                item.View.AllowedToDraw = false;

                World.AddEffect(new AnimatedItemEffect(item.Serial, item.Graphic, item.Hue, -1));
            }
            //item.Effect = new AnimatedItemEffect(item.Serial, item.Graphic, item.Hue, -1);

            if (item.OnGround)
                item.AddToTile();
        }

        private static void EnterWorld(Packet p)
        {
            Engine.Profile.Load(World.ServerName, Engine.GlobalSettings.Username, Engine.GlobalSettings.LastCharacterName);

            World.Mobiles.Add(World.Player = new PlayerMobile(p.ReadUInt()));
            p.Skip(4);
            World.Player.Graphic = p.ReadUShort();
            ushort x = p.ReadUShort();
            ushort y = p.ReadUShort();
            sbyte z = (sbyte)p.ReadUShort();

            if (World.Map == null)
                World.MapIndex = 0;

            Direction direction = (Direction)(p.ReadByte() & 0x7);
            //World.Player.ForcePosition(x, y, z, direction);

            World.Player.Position = new Position(x, y, z);
            World.Player.Direction = direction;
            World.Player.AddToTile();



            if (FileManager.ClientVersion >= ClientVersions.CV_200)
            {
                NetClient.Socket.Send(new PGameWindowSize((uint)Engine.Profile.Current.GameWindowSize.X, (uint)Engine.Profile.Current.GameWindowSize.Y));
                NetClient.Socket.Send(new PLanguage("ENU"));
            }

            NetClient.Socket.Send(new PClientVersion(Engine.GlobalSettings.ClientVersion));

            GameActions.SingleClick(World.Player);
            NetClient.Socket.Send(new PStatusRequest(World.Player));
            World.Player.ProcessDelta();
            World.Mobiles.ProcessDelta();
        }

        private static void Talk(Packet p)
        {
            Serial serial = p.ReadUInt();
            Entity entity = World.Get(serial);
            ushort graphic = p.ReadUShort();
            MessageType type = (MessageType)p.ReadByte();
            Hue hue = p.ReadUShort();
            MessageFont font = (MessageFont)p.ReadUShort();
            string name = p.ReadASCII(30);
            string text = p.ReadASCII();

            if (serial == 0 && graphic == 0 && type == MessageType.Regular && font == MessageFont.INVALID && hue == 0xFFFF && name.StartsWith("SYSTEM"))
            {
                NetClient.Socket.Send(new PACKTalk());

                return;
            }

            if (entity != null)
            {
                //entity.Graphic = graphic;
                entity.Name = name;
                entity.ProcessDelta();
            }

            Chat.OnMessage(entity, text, hue, type, font);
        }

        private static void DeleteObject(Packet p)
        {
            if (World.Player == null)
                return;
            Serial serial = p.ReadUInt();

            if (World.Player == serial)
                return;

            if (World.Get(serial) == null)
                return;

            GameScene scene = Engine.SceneManager.GetScene<GameScene>();

            if (scene.HeldItem.Serial == serial)
                scene.HeldItem.Enabled = false;

            if (serial.IsItem)
            {
                Item item = World.Items.Get(serial);

                if (!item.OnGround && item.Container.IsValid)
                {
                    if (item.Container == World.Player && item.Layer == Layer.OneHanded || item.Layer == Layer.TwoHanded)
                    {
                        World.Player.UpdateAbilities();
                    }

                    World.Get(item.Container).Items.Remove(item);
                }

                if (World.RemoveItem(serial))
                    World.Items.ProcessDelta();
            }
            else if (serial.IsMobile && World.RemoveMobile(serial))
            {
                World.Items.ProcessDelta();
                World.Mobiles.ProcessDelta();
            }
        }

        private static void Explosion(Packet p)
        {
        }

        private static void UpdatePlayer(Packet p)
        {
            if (World.Player == null) return;

            if (p.ReadUInt() != World.Player) throw new Exception("OnMobileStatus");
            World.Player.Graphic = (ushort)(p.ReadUShort() + p.ReadSByte());
            World.Player.Hue = p.ReadUShort();
            World.Player.Flags = (Flags)p.ReadByte();
            ushort x = p.ReadUShort();
            ushort y = p.ReadUShort();
            p.Skip(2);
            Direction direction = (Direction)p.ReadByte();
            sbyte z = p.ReadSByte();
            Direction dir = direction & Direction.Mask;

#if JAEDAN_MOVEMENT_PATCH
            World.Player.ForcePosition(x, y, z, dir);
#elif MOVEMENT2
            World.Player.ResetSteps();

            World.Player.GetEndPosition(out int endX, out int endY, out sbyte endZ, out Direction endDir);

            if (endX == x && endY == y && endZ == z)
            {
                if (endDir != dir)
                {
                    World.Player.EnqueueStep(x, y, z, dir, false);
                }
            }
            else
            {
                World.Player.ForcePosition(x, y, z , dir);
            }
#else
            World.Player.CloseBank();
            World.Player.Walker.WalkingFailed = false;
            World.Player.Position = new Position(x, y, z);
            World.Player.Direction = dir;
            World.Player.Walker.DenyWalk(0xFF, -1, -1, -1);
            World.Player.Walker.ResendPacketSended = false;
            World.Player.AddToTile();
#endif
            World.Player.ProcessDelta();
        }

        private static void DenyWalk(Packet p)
        {
            if (World.Player == null)
                return;
            byte seq = p.ReadByte();
            ushort x = p.ReadUShort();
            ushort y = p.ReadUShort();
            Direction direction = (Direction)p.ReadByte();
            direction &= Direction.Up;
            sbyte z = p.ReadSByte();


#if JAEDAN_MOVEMENT_PATCH
            World.Player.ForcePosition(x, y , z, direction);
#elif MOVEMENT2
            World.Player.DenyWalk(seq, direction, x, y , z);
#else
            World.Player.Walker.DenyWalk(seq, x, y, z);
            World.Player.Direction = direction;
#endif
            World.Player.ProcessDelta();
        }

        private static void ConfirmWalk(Packet p)
        {
            if (World.Player == null)
                return;
            byte seq = p.ReadByte();
            byte noto = (byte)(p.ReadByte() & ~0x40);

            if (noto == 0 || noto >= 7)
                noto = 0x01;
            World.Player.NotorietyFlag = (NotorietyFlag)noto;
            World.Player.ConfirmWalk(seq);
            World.Player.ProcessDelta();
        }

        private static void DragAnimation(Packet p)
        {
            Graphic graphic = p.ReadUShort();
            graphic += p.ReadByte();
            Hue hue = p.ReadUShort();
            ushort count = p.ReadUShort();
            Serial source = p.ReadUInt();
            ushort sourceX = p.ReadUShort();
            ushort sourceY = p.ReadUShort();
            sbyte sourceZ = p.ReadSByte();
            Serial dest = p.ReadUInt();
            ushort destX = p.ReadUShort();
            ushort destY = p.ReadUShort();
            sbyte destZ = p.ReadSByte();

            if (graphic == 0x0EED)
                graphic = 0x0EEF;
            else if (graphic == 0x0EEA)
                graphic = 0x0EEC;
            else if (graphic == 0x0EF0) graphic = 0x0EF2;
            Entity entity = World.Get(source);

            if (entity == null)
                source = 0;
            else
            {
                sourceX = entity.Position.X;
                sourceY = entity.Position.Y;
                sourceZ = entity.Position.Z;
            }

            Entity destEntity = World.Get(dest);

            if (destEntity == null)
                dest = 0;
            else
            {
                destX = destEntity.Position.X;
                destY = destEntity.Position.Y;
                destZ = destEntity.Position.Z;
            }

            // effect moving. To do
        }

        private static void OpenContainer(Packet p)
        {
            if (World.Player == null)
                return;

            //item.EnableCallBackForItemsUpdate(true);
            var serial = p.ReadUInt();
            Graphic graphic = p.ReadUShort();

            Engine.UI.GetByLocalSerial(serial)?.Dispose();

            if (graphic == 0x30) // vendor
            {
                var mobile = World.Mobiles.Get(serial);
                var itemList = mobile.Items
                    .Where(o => o.Layer == Layer.ShopResale || o.Layer == Layer.ShopBuy)
                    .SelectMany(o => o.Items)
                    .OrderBy(o => o.Serial.Value)
                    .ToArray();

                Engine.UI.Add(new ShopGump(mobile.Serial, itemList, true, 100, 100));
            }
            else
            {
                Item item = World.Items.Get(serial);

                if (graphic == 0xFFFF) // spellbook
                {
                    if (item.IsSpellBook)
                    {
                        SpellbookGump spellbookGump = new SpellbookGump(item);
                        if (!Engine.UI.GetGumpCachePosition(item, out Point location))
                        {
                            location = new Point(64, 64);
                        }

                        spellbookGump.Location = location;
                        Engine.UI.Add(spellbookGump);

                        Engine.SceneManager.CurrentScene.Audio.PlaySound(0x0055);
                    }
                }
                else
                {
                    ContainerGump container = new ContainerGump(item, graphic);

                    //if (!Engine.UI.GetGumpCachePosition(item, out Point location))
                    //{
                    //    location = Engine.UI.GetContainerPosition();
                    //}

                    //container.Location = location;
                    Engine.UI.Add(container);
                }
            }
        }

        private static void UpdateContainedItem(Packet p)
        {
            if (!World.InGame)
                return;

            List<Item> items = new List<Item>();

            if (ReadContainerContent(p, items))
                World.Items.ProcessDelta();
        }

        private static void KickPlayer(Packet p)
        {
        }

        private static void DenyMoveItem(Packet p)
        {
            if (!World.InGame)
                return;

            GameScene scene = Engine.SceneManager.GetScene<GameScene>();

            ItemHold hold = scene.HeldItem;

            Item item = World.Items.Get(hold.Serial);

            if (hold.Enabled || (hold.Dropped && item == null))
            {
                if (hold.Layer == Layer.Invalid && hold.Container.IsValid)
                {
                    Entity container = World.Get(hold.Container);

                    if (container != null)
                    {
                        item = World.GetOrCreateItem(hold.Serial);
                        item.Graphic = hold.Graphic;
                        item.Hue = hold.Hue;
                        item.Amount = hold.Amount;
                        item.Flags = hold.Flags;
                        item.Layer = hold.Layer;
                        item.Container = hold.Container;
                        item.Position = hold.Position;

                        container.Items.Add(item);

                        World.Items.Add(item);
                        World.Items.ProcessDelta();

                        container.ProcessDelta();
                    }
                }
                else
                {
                    item = World.GetOrCreateItem(hold.Serial);

                    //if (item != null)
                    {
                        item.Graphic = hold.Graphic;
                        item.Hue = hold.Hue;
                        item.Amount = hold.Amount;
                        item.Flags = hold.Flags;
                        item.Layer = hold.Layer;
                        item.Container = hold.Container;
                        item.Position = hold.Position;

                        if (!hold.OnGround)
                        {
                            Entity container = World.Get(item.Container);

                            if (container != null)
                            {
                                if (container.Serial.IsMobile)
                                {
                                    Mobile mob = (Mobile) container;

                                    mob.Items.Add(item);

                                    mob.Equipment[(int) hold.Layer] = item;

                                    World.Items.Add(item);

                                    mob.ProcessDelta();
                                }
                                else
                                {
                                    Log.Message(LogTypes.Warning, "SOMETHING WRONG WITH CONTAINER (should be a mobile)");
                                }
                            }
                            else
                            {
                                Log.Message(LogTypes.Warning, "SOMETHING WRONG WITH CONTAINER (is null)");
                            }
                        }
                        else 
                            item.AddToTile();
                    }
                }

                hold.Clear();
            }
            else
            {
                Log.Message(LogTypes.Warning, "There was a problem with ItemHold object. It was cleared before :|");
            }

            byte code = p.ReadByte();

            if (code < 5)
            {
                Chat.OnMessage(null, ServerErrorMessages.PickUpErrors[code], 0, MessageType.System, MessageFont.Normal);
            }
        }

        private static void EndDraggingItem(Packet p)
        {
            if (!World.InGame)
                return;
            GameScene scene = Engine.SceneManager.GetScene<GameScene>();

            scene.HeldItem.Enabled = false;
            scene.HeldItem.Dropped = false;
        }

        private static void DropItemAccepted(Packet p)
        {
            if (!World.InGame)
                return;
            GameScene scene = Engine.SceneManager.GetScene<GameScene>();

            scene.HeldItem.Enabled = false;
            scene.HeldItem.Dropped = false;
        }

        private static void Blood(Packet p)
        {
        }

        private static void GodMode(Packet p)
        {
        }

        private static void DeathScreen(Packet p)
        {
            // todo
            byte action = p.ReadByte();

            if (action != 1)
            {
                Engine.SceneManager.CurrentScene.Audio.PlayMusic(42);

                GameActions.SetWarMode(false);
            }
        }

        private static void MobileAttributes(Packet p)
        {
            Mobile mobile = World.Mobiles.Get(p.ReadUInt());

            if (mobile == null) return;
            mobile.HitsMax = p.ReadUShort();
            mobile.Hits = p.ReadUShort();
            mobile.ManaMax = p.ReadUShort();
            mobile.Mana = p.ReadUShort();
            mobile.StaminaMax = p.ReadUShort();
            mobile.Stamina = p.ReadUShort();
            mobile.ProcessDelta();
        }

        private static void EquipItem(Packet p)
        {
            Item item = World.GetOrCreateItem(p.ReadUInt());
            item.Graphic = (ushort)(p.ReadUShort() + p.ReadSByte());
            item.Layer = (Layer)p.ReadByte();
            item.Container = p.ReadUInt();
            item.Hue = p.ReadUShort();
            item.Amount = 1;
            Mobile mobile = World.Mobiles.Get(item.Container);

            if (mobile != null)
            {
                mobile.Equipment[(int)item.Layer] = item;
                mobile.Items.Add(item);
            }

            item.ProcessDelta();
            if (World.Items.Add(item)) World.Items.ProcessDelta();
            mobile?.ProcessDelta();

            if (mobile == World.Player && (item.Layer == Layer.OneHanded || item.Layer == Layer.TwoHanded))
                World.Player.UpdateAbilities();

            GameScene gs = Engine.SceneManager.GetScene<GameScene>();

            if (gs.HeldItem.Serial == item.Serial)
                gs.HeldItem.Clear();
        }

        private static void FightOccuring(Packet p)
        {
        }

        private static void AttackOK(Packet p)
        {
        }

        private static void AttackEnded(Packet p)
        {
        }

        private static void UpdateSkills(Packet p)
        {
            ushort id;

            switch (p.ReadByte())
            {
                case 0:

                    while (p.Position + 2 <= p.Length && (id = p.ReadUShort()) > 0)
                        World.Player.UpdateSkill(id - 1, p.ReadUShort(), p.ReadUShort(), (Lock)p.ReadByte(), 100);

                    break;
                case 2:

                    while (p.Position + 2 <= p.Length && (id = p.ReadUShort()) > 0)
                        World.Player.UpdateSkill(id - 1, p.ReadUShort(), p.ReadUShort(), (Lock)p.ReadByte(), p.ReadUShort());

                    break;
                case 0xDF:
                    id = p.ReadUShort();
                    World.Player.UpdateSkill(id, p.ReadUShort(), p.ReadUShort(), (Lock)p.ReadByte(), p.ReadUShort(), true);

                    break;
                case 0xFF:
                    id = p.ReadUShort();
                    World.Player.UpdateSkill(id, p.ReadUShort(), p.ReadUShort(), (Lock)p.ReadByte(), 100);

                    break;
            }

            World.Player.ProcessDelta();
        }

        private static void RemoveGroupR(Packet p)
        {
        }

        private static void PauseControl(Packet p)
        {
        }

        private static void Pathfinding(Packet p)
        {
            if (!World.InGame)
                return;

            ushort x = p.ReadUShort();
            ushort y = p.ReadUShort();
            ushort z = p.ReadUShort();

            Pathfinder.WalkTo(x, y, z, 0);
        }

        private static void ResourceTileDataGodClient(Packet p)
        {
        }

        private static void UpdateContainedItems(Packet p)
        {
            if (!World.InGame)
                return;
            ushort count = p.ReadUShort();
            List<Item> items = new List<Item>(count);

            for (int i = 0; i < count; i++)
                ReadContainerContent(p, items);

            if (items.Count > 0)
            {
                Item container = World.Items.Get(items[0].Container);

                if (container != null && container.IsSpellBook && SpellbookData.GetTypeByGraphic(container.Graphic) != SpellBookType.Unknown)
                {
                    SpellbookData.GetData(container, out ulong field, out SpellBookType type);

                    if (container.FillSpellbook(type, field))
                    {
                        SpellbookGump gump = Engine.UI.GetByLocalSerial<SpellbookGump>(container);
                        gump?.Update();
                    }
                }
            }

            World.Items.ProcessDelta();
        }

        private static void VersionGodClient(Packet p)
        {
        }

        private static void UpdateStaticsGodClient(Packet p)
        {
        }

        private static void PersonalLightLevel(Packet p)
        {
            if (!World.InGame)
                return;

            if (World.Player == p.ReadUInt())
            {
                byte level = p.ReadByte();

                if (level > 0x1F)
                    level = 0x1F;

                //World.Light.Personal = level;
            }
        }

        private static void LightLevel(Packet p)
        {
            byte level = p.ReadByte();

            if (level > 0x1F)
                level = 0x1F;

            //World.Light.Overall = level;
        }

        private static void ErrorCode(Packet p)
        {
        }

        private static void PlaySoundEffect(Packet p)
        {
            p.Skip(1);

            ushort index = p.ReadUShort();
            ushort audio = p.ReadUShort();
            ushort x = p.ReadUShort();
            ushort y = p.ReadUShort();

            Position position = new Position(x, y, p.ReadSByte());

            float soundByRange = Engine.Profile.Current.SoundVolume / (float)World.ViewRange;
            soundByRange *= World.Player.Position.DistanceTo(position);
            float volume = (Engine.Profile.Current.SoundVolume - soundByRange) / 100f;

            if (volume > 0 && volume < 0.01f)
                volume = 0.01f;

            Engine.SceneManager.CurrentScene.Audio.PlaySoundWithDistance(index, volume, true);
        }

        private static void PlayMusic(Packet p)
        {
            ushort index = p.ReadUShort();

            Engine.SceneManager.CurrentScene.Audio.PlayMusic(index);
        }

        private static void LoginComplete(Packet p)
        {
            if (World.Player != null)
            {
                GameScene scene = new GameScene();
                Engine.SceneManager.ChangeScene(scene);


                NetClient.Socket.Send(new PSkillsRequest(World.Player));

                if (FileManager.ClientVersion >= ClientVersions.CV_306E)
                    NetClient.Socket.Send(new PClientType());

                //TODO: issue with viewrange
                //if (FileManager.ClientVersion >= ClientVersions.CV_305D)
                //    NetClient.Socket.Send(new PClientViewRange(World.ViewRange));

                Engine.FpsLimit = Engine.Profile.Current.MaxFPS;
                scene.Load();

                Engine.Profile.Current.ReadGumps()?.ForEach(Engine.UI.Add);
            }
        }

        private static void MapData(Packet p)
        {
            if (!World.InGame)
                return;

            Serial serial = p.ReadUInt();

            MapGump gump = Engine.UI.GetByLocalSerial<MapGump>(serial);

            if (gump != null)
            {
                switch ((MapMessageType)p.ReadByte())
                {
                    case MapMessageType.Add:
                        p.Skip(1);

                        ushort x = p.ReadUShort();
                        ushort y = p.ReadUShort();

                        gump.AddToContainer(new GumpPic(x + 24, y + 31, 0x139B, 0));

                        break;
                    case MapMessageType.Insert: break;
                    case MapMessageType.Move: break;
                    case MapMessageType.Remove: break;
                    case MapMessageType.Clear:
                        gump.ClearContainer();
                        break;
                    case MapMessageType.Edit: break;
                    case MapMessageType.EditResponse:
                        gump.SetPlotState(p.ReadByte());
                        break;
                }
            }
        }

        private static void SetTime(Packet p)
        {
        }

        private static void SetWeather(Packet p)
        {
        }

        private static void BookData(Packet p)
        {
            UIManager ui = Engine.UI;
            var serial = p.ReadUInt();
            var pageCnt = p.ReadUShort();
            var pages = new string[pageCnt];
            var gump = ui.GetByLocalSerial<BookGump>(serial);
            if (gump == null)
            {
                return;
            }
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < pageCnt; i++)
            {
                pages[i] = string.Empty;
            }
            //packets sent from server can contain also an uneven amount of page, not counting that we could receive only part of them, not every page!
            for (int i = 0; i < pageCnt; i++, sb.Clear())
            {
                var pageNum = p.ReadUShort() - 1;
                if (pageNum < pageCnt)
                {
                    var lineCnt = p.ReadUShort();
                    for (int x = 0; x < lineCnt; x++)
                    {
                        sb.Append(BookGump.IsNewBookD4 ? p.ReadUTF8StringSafe() : p.ReadASCII());
                        sb.Append('\n');
                    }
                    if (sb.Length > 0)
                        sb.Remove(sb.Length - 1, 1);//this removes the last, unwanted, newline
                    pages[pageNum] = sb.ToString();
                }
                else
                    Log.Message(LogTypes.Error, "BOOKGUMP: The server is sending a page number GREATER than the allowed number of pages in BOOK!");
            }
            gump.BookPages = pages;
        }

        private static void CharacterAnimation(Packet p)
        {
            Mobile mobile = World.Mobiles.Get(p.ReadUInt());

            if (mobile == null) return;
            ushort action = p.ReadUShort();
            ushort frameCount = p.ReadUShort();
            frameCount = 0;
            ushort repeatMode = p.ReadUShort();
            bool frameDirection = !p.ReadBool();
            bool repeat = p.ReadBool();
            byte delay = p.ReadByte();
            mobile.SetAnimation(Mobile.GetReplacedObjectAnimation(mobile.Graphic, action), delay, (byte)frameCount, (byte)repeatMode, repeat, frameDirection);
            mobile.AnimationFromServer = true;
        }

        private static void GraphicEffect(Packet p)
        {
            GraphicEffectType type = (GraphicEffectType)p.ReadByte();
            Serial source = p.ReadUInt();
            Serial target = p.ReadUInt();
            Graphic graphic = p.ReadUShort();
            Position srcPos = new Position(p.ReadUShort(), p.ReadUShort(), p.ReadSByte());
            Position targPos = new Position(p.ReadUShort(), p.ReadUShort(), p.ReadSByte());
            byte speed = p.ReadByte();
            ushort duration = (ushort)(p.ReadByte() * 50);
            p.Skip(2);
            bool fixedDirection = p.ReadBool();
            bool doesExplode = p.ReadBool();
            Hue hue = 0;
            GraphicEffectBlendMode blendmode = 0;

            if (p.ID != 0x70)
            {
                hue = (ushort)p.ReadUInt();
                blendmode = (GraphicEffectBlendMode)p.ReadUInt();
            }

            World.AddEffect(type, source, target, graphic, hue, srcPos, targPos, speed, duration, fixedDirection, doesExplode, false, blendmode);
        }

        private static void ClientViewRange(Packet p)
        {
            World.ViewRange = p.ReadByte();
        }

        private static void BulletinBoardData(Packet p)
        {
            if (World.Player == null)
                return;

            switch (p.ReadByte())
            {
                case 0: // open

                    {
                        Serial serial = p.ReadUInt();
                        Item item = World.Items.Get(serial);

                        if (item != null)
                        {
                            BulletinBoardGump bulletinBoard = Engine.UI.GetByLocalSerial<BulletinBoardGump>(serial);
                            bulletinBoard?.Dispose();

                            int x = (Engine.WindowWidth / 2) - 245;
                            int y = (Engine.WindowHeight / 2) - 205;

                            bulletinBoard = new BulletinBoardGump(item, x, y, p.ReadASCII(22));
                            Engine.UI.Add(bulletinBoard);
                        }
                    }

                    break;
                case 1: // summary msg

                    {
                        Serial boardSerial = p.ReadUInt();
                        BulletinBoardGump bulletinBoard = Engine.UI.GetByLocalSerial<BulletinBoardGump>(boardSerial);

                        if (bulletinBoard != null)
                        {
                            Serial serial = p.ReadUInt();
                            Serial parendID = p.ReadUInt();

                            int len = p.ReadByte();
                            string text = len > 0 ? p.ReadASCII(len) : string.Empty;
                            text += " - ";

                            len = p.ReadByte();
                            text += len > 0 ? p.ReadASCII(len) : string.Empty;
                            text += " - ";

                            bulletinBoard.Add(new BulletinBoardObject(boardSerial, World.Items.Get(serial), text));
                        }
                    }

                    break;
                case 2: // message

                    {
                        Serial boardSerial = p.ReadUInt();
                        BulletinBoardGump bulletinBoard = Engine.UI.GetByLocalSerial<BulletinBoardGump>(boardSerial);

                        if (bulletinBoard != null)
                        {
                            Serial serial = p.ReadUInt();

                            int len = p.ReadByte();
                            string poster = len > 0 ? p.ReadASCII(len) : string.Empty;

                            len = p.ReadByte();
                            string subject = len > 0 ? p.ReadASCII(len) : string.Empty;

                            len = p.ReadByte();
                            string dataTime = len > 0 ? p.ReadASCII(len) : string.Empty;

                            p.Skip(4);

                            byte unk = p.ReadByte();

                            if (unk > 0)
                            {
                                p.Skip(unk * 4);
                            }

                            byte lines = p.ReadByte();

                            StringBuilder sb = new StringBuilder();

                            for (int i = 0; i < lines; i++)
                            {
                                byte lineLen = p.ReadByte();

                                if (sb.Length != 0)
                                    sb.Append('\n');

                                if (lineLen > 0)
                                    sb.Append(p.ReadASCII(lineLen));
                            }

                            byte variant = (byte) (1 + (poster == World.Player.Name ? 1 : 0));

                            Engine.UI.Add(new BulletinBoardItem(serial, 0, poster, subject, dataTime, sb.ToString(), variant));
                        }
                    }
                    break;
            }
        }

        private static void Warmode(Packet p)
        {
            World.Player.InWarMode = p.ReadBool();
            p.ReadByte(); // always 0x00
            p.ReadByte(); // always 0x32
            p.ReadByte(); // always 0x00
            World.Player.ProcessDelta();
        }

        private static void Ping(Packet p)
        {
        }

        private static void BuyList(Packet p)
        {
            Item container = World.Items.Get(p.ReadUInt());

            if (container == null) return;
            Mobile vendor = World.Mobiles.Get(container.Container);

            if (vendor == null) return;
            var count = p.ReadByte();

            //Server sends items ordered by serial
            foreach (var item in container.Items.OrderBy(o => o.Serial.Value))
            {
                item.Price = p.ReadUInt();
                var nameLen = p.ReadByte();
                var name = p.ReadASCII(nameLen);
                int cliloc = 0;

                if (int.TryParse(name, out cliloc))
                    item.Name = FileManager.Cliloc.GetString(cliloc);
                else
                    item.Name = name;
            }
        }

        private static void NewSubServer(Packet p)
        {
        }

        private static void UpdateCharacter(Packet p)
        {
            if (World.Player == null)
                return;
            Mobile mobile = World.GetOrCreateMobile(p.ReadUInt());
            mobile.Graphic = p.ReadUShort();
            int x = p.ReadUShort();
            int y = p.ReadUShort();
            sbyte z = p.ReadSByte();
            Direction direction = (Direction)p.ReadByte();
            mobile.Hue = p.ReadUShort();
            mobile.Flags = (Flags)p.ReadByte();
            mobile.NotorietyFlag = (NotorietyFlag)p.ReadByte();
            mobile.ProcessDelta();

            if (World.Mobiles.Add(mobile))
                World.Mobiles.ProcessDelta();

            if (mobile == World.Player)
                return;
            Direction dir = direction & Direction.Up;
            bool isrun = (direction & Direction.Running) != 0;

            if (World.Get(mobile) == null)
            {
                mobile.Position = new Position((ushort)x, (ushort)y, z);
                mobile.Direction = dir;
                mobile.IsRunning = isrun;

                mobile.AddToTile();
            }

            if (!mobile.EnqueueStep(x, y, z, dir, isrun))
            {
                mobile.Position = new Position((ushort)x, (ushort)y, z);
                mobile.Direction = dir;
                mobile.IsRunning = isrun;
                mobile.ClearSteps();
                mobile.AddToTile();
            }
        }

        private static void UpdateObject(Packet p)
        {
            if (World.Player == null) return;
            Mobile mobile = World.GetOrCreateMobile(p.ReadUInt());
            Graphic graphic = p.ReadUShort();
            ushort x = p.ReadUShort();
            ushort y = p.ReadUShort();
            sbyte z = p.ReadSByte();
            Direction direction = (Direction)p.ReadByte();
            Hue hue = p.ReadUShort();
            Flags flags = (Flags)p.ReadByte();
            NotorietyFlag notoriety = (NotorietyFlag)p.ReadByte();
            mobile.Graphic = graphic;
            mobile.Hue = hue;
            mobile.Flags = flags;
            mobile.NotorietyFlag = notoriety;

            if (p.ID != 0x78)
                p.Skip(6);
            uint itemSerial;

            while ((itemSerial = p.ReadUInt()) != 0)
            {
                Item item = World.GetOrCreateItem(itemSerial);
                Graphic itemGraphic = p.ReadUShort();
                item.Layer = (Layer)p.ReadByte();

                if (FileManager.ClientVersion >= ClientVersions.CV_70331)
                    item.Hue = p.ReadUShort();
                else if ((itemGraphic & 0x8000) != 0)
                {
                    itemGraphic &= 0x7FFF;
                    item.Hue = p.ReadUShort();
                }
                else
                    itemGraphic &= 0x3FFF;

                item.Graphic = itemGraphic;
                item.Amount = 1;
                item.Container = mobile;
                mobile.Items.Add(item);
                mobile.Equipment[(int)item.Layer] = item;

                if (item.PropertiesHash == 0)
                    NetClient.Socket.Send(new PMegaClilocRequest(item));
                item.ProcessDelta();
                World.Items.Add(item);
            }

            if (mobile == World.Player) // resync ?
            {
                World.Player.UpdateAbilities();
                //World.Player.ResetSteps();
                //World.Player.Position = new Position(x, y, z);
                //World.Player.Direction = direction;
            }
            else
            {
                Direction dir = direction & Direction.Up;
                bool isrun = (direction & Direction.Running) != 0;

                if (World.Get(mobile) == null)
                {
                    mobile.Position = new Position(x, y, z);
                    mobile.Direction = dir;
                    mobile.IsRunning = isrun;
                    mobile.AddToTile();
                }

                if (!mobile.EnqueueStep(x, y, z, dir, isrun))
                {
                    mobile.Position = new Position(x, y, z);
                    mobile.Direction = dir;
                    mobile.IsRunning = isrun;
                    mobile.ClearSteps();
                    mobile.AddToTile();
                }
            }

            mobile.ProcessDelta();

            if (World.Mobiles.Add(mobile))
                World.Mobiles.ProcessDelta();
            World.Items.ProcessDelta();

            //if (string.IsNullOrEmpty(mobile.Name))
            //    NetClient.Socket.Send(new PNameRequest(mobile));

            if (mobile != World.Player)
                NetClient.Socket.Send(new PClickRequest(mobile));
        }

        private static void OpenMenu(Packet p)
        {
            Serial serial = p.ReadUInt();
            uint id = p.ReadUInt();
            //string name = p.ReadASCII(p.ReadByte());
            //int count = p.ReadByte();
            // to finish
        }

        private static void LoginError(Packet p)
        {
        }

        private static void ResendCharacterList(Packet p)
        {
            int slots = p.ReadByte();

            if (slots > 0)
            {
                for (int i = 0; i < slots; i++)
                {
                    string name = p.ReadASCII(30);
                    p.Skip(30);

                    if (name.Length > 0)
                    {
                    }
                }
            }
        }

        private static void OpenPaperdoll(Packet p)
        {
            Mobile mobile = World.Mobiles.Get(p.ReadUInt());

            if (mobile == null) return;
            string text = p.ReadASCII(60);
            byte flags = p.ReadByte();
            UIManager ui = Engine.UI;

            if (ui.GetByLocalSerial<PaperDollGump>(mobile) == null)
            {
                if (!ui.GetGumpCachePosition(mobile, out Point location))
                {
                    location = new Point(100, 100);
                }
                ui.Add(new PaperDollGump(mobile, text) { Location = location });
            }
        }

        private static void CorpseEquipment(Packet p)
        {
            Entity corpse = World.Get(p.ReadUInt());
            Layer layer = (Layer)p.ReadByte();

            while (layer != Layer.Invalid && p.Position < p.Length)
            {
                Item item = World.Items.Get(p.ReadUInt());

                if (item != null && item.Container == corpse)
                {
                    // put equip
                }

                layer = (Layer)p.ReadByte();
            }
        }

        private static void RelayServer(Packet p)
        {
        }

        private static void DisplayMap(Packet p)
        {
            Serial serial = p.ReadUInt();
            Graphic gumpid = p.ReadUShort();
            ushort startX = p.ReadUShort();
            ushort startY = p.ReadUShort();
            ushort endX = p.ReadUShort();
            ushort endY = p.ReadUShort();
            ushort width = p.ReadUShort();
            ushort height = p.ReadUShort();

            MapGump gump = new MapGump(serial, gumpid, width, height);

            if (p.ID == 0xF5 || FileManager.ClientVersion >= ClientVersions.CV_308Z)
            {
                ushort facet = 0;

                if (p.ID == 0xF5)
                    facet = p.ReadUShort();
                gump.SetMapTexture(FileManager.Multimap.LoadFacet(facet, width, height, startX, startY, endX, endY));
            }
            else
            {
                gump.SetMapTexture(FileManager.Multimap.LoadMap(width, height, startX, startY, endX, endY));
            }

            Engine.UI.Add(gump);
        }

        private static void OpenBook(Packet p)
        {
            uint serial = p.ReadUInt();
            bool oldpacket = p.ID == 0x93;
            bool editable = p.ReadBool();
            p.Skip(1);
            UIManager ui = Engine.UI;
            BookGump bgump = ui.GetByLocalSerial<BookGump>(serial);

            if (bgump == null || bgump.IsDisposed)
            {
                ui.Add(new BookGump(serial)
                {
                    X = 100,
                    Y = 100,
                    BookPageCount = p.ReadUShort(),
                    //title allows only 47 dots (. + \0) so 47 is the right number
                    BookTitle =
                    new MultiLineBox(new MultiLineEntry(BookGump.DefaultFont, 47, 150, 150, BookGump.IsNewBookD4, Renderer.FontStyle.None, 0), editable)
                    {
                        X = 40,
                        Y = 60,
                        Height = 25,
                        Width = 155,
                        IsEditable = editable,
                        Text = oldpacket ? p.ReadASCII(60).Trim('\0') : p.ReadASCII(p.ReadUShort()).Trim('\0'),
                    },
                    //as the old booktitle supports only 30 characters in AUTHOR and since the new clients only allow 29 dots (. + \0 character at end), we use 29 as a limitation
                    BookAuthor =
                    new MultiLineBox(new MultiLineEntry(BookGump.DefaultFont, 29, 150, 150, BookGump.IsNewBookD4, Renderer.FontStyle.None, 0), editable)
                    {
                        X = 40,
                        Y = 160,
                        Height = 25,
                        Width = 155,
                        IsEditable = editable,
                        Text = oldpacket ? p.ReadASCII(30).Trim('\0') : p.ReadASCII(p.ReadUShort()).Trim('\0'),
                    },
                    IsEditable = editable
                });
            }
            else
            {
                p.Skip(2);
                bgump.IsEditable = editable;
                bgump.BookTitle.Text = oldpacket ? p.ReadASCII(60).Trim('\0') : p.ReadASCII(p.ReadUShort()).Trim('\0');
                bgump.BookTitle.IsEditable = editable;
                bgump.BookAuthor.Text = oldpacket ? p.ReadASCII(30).Trim('\0') : p.ReadASCII(p.ReadUShort()).Trim('\0');
                bgump.BookAuthor.IsEditable = editable;
            }
        }

        private static void DyeData(Packet p)
        {
            Serial serial = p.ReadUInt();
            p.Skip(2);
            Graphic graphic = p.ReadUShort();

            Rectangle rect = FileManager.Gumps.GetTexture(0x0906).Bounds;

            int x = (Engine.WindowWidth >> 1) - (rect.Width >> 1);
            int y = (Engine.WindowHeight >> 1) - (rect.Height >> 1);

            ColorPickerGump gump = new ColorPickerGump(serial, graphic, x, y, null);

            Engine.UI.Add(gump);
        }

        private static void MovePlayer(Packet p)
        {
            if (!World.InGame)
                return;

            Direction direction = (Direction)p.ReadByte();
            World.Player.Walk(direction & Direction.Mask, (direction & Direction.Running) != 0);
        }

        private static void AllNames3DGameOnlyR(Packet p)
        {
        }

        private static void MultiPlacement(Packet p)
        {
        }

        private static void ASCIIPrompt(Packet p)
        {
            if (!World.InGame)
                return;

            byte[] data = p.ReadArray(8);

            Chat.PromptData = new PromptData()
            {
                Prompt = ConsolePrompt.ASCII,
                Data = data
            };       
        }

        private static void RequestAssistance(Packet p)
        {
        }

        private static void SellList(Packet p)
        {
            Mobile vendor = World.Mobiles.Get(p.ReadUInt());

            if (vendor == null) return;
            ushort countItems = p.ReadUShort();

            if (countItems <= 0) return;

            List<Item> itemList = new List<Item>(countItems);

            for (int i = 0; i < countItems; i++)
            {
                Item item = World.GetOrCreateItem(p.ReadUInt());
                item.Graphic = p.ReadUShort();
                item.Hue = p.ReadUShort();
                item.Amount = p.ReadUShort();
                item.Price = p.ReadUShort();

                string name = p.ReadASCII(p.ReadUShort());
                if (int.TryParse(name, out int clilocnum))
                    name = FileManager.Cliloc.GetString(clilocnum);

                item.Name = name;

                itemList.Add(item);
            }

            UIManager ui = Engine.UI;
            ui.Add(new ShopGump(vendor.Serial, itemList.ToArray(), false, 100, 100));
        }

        private static void UpdateHitpoints(Packet p)
        {
            Mobile mobile = World.Mobiles.Get(p.ReadUInt());

            if (mobile == null) return;
            mobile.HitsMax = p.ReadUShort();
            mobile.Hits = p.ReadUShort();
            mobile.ProcessDelta();
        }

        private static void UpdateMana(Packet p)
        {
            Mobile mobile = World.Mobiles.Get(p.ReadUInt());

            if (mobile == null) return;
            mobile.ManaMax = p.ReadUShort();
            mobile.Mana = p.ReadUShort();
            mobile.ProcessDelta();
        }

        private static void UpdateStamina(Packet p)
        {
            Mobile mobile = World.Mobiles.Get(p.ReadUInt());

            if (mobile == null) return;
            mobile.StaminaMax = p.ReadUShort();
            mobile.Stamina = p.ReadUShort();
            mobile.ProcessDelta();
        }

        private static void OpenUrl(Packet p)
        {
            string url = p.ReadASCII();

            if (!string.IsNullOrEmpty(url))
            {
                try
                {
                    Process.Start(url);
                }
                catch
                {

                }
            }
        }

        private static void TipWindow(Packet p)
        {
            byte flag = p.ReadByte();

            if (flag != 1)
            {
                Serial serial = p.ReadUInt();
                string str = p.ReadASCII(p.ReadUShort());
            }
        }

        private static void ServerList(Packet p)
        {
        }

        private static void CharacterList(Packet p)
        {
        }

        private static void AttackCharacter(Packet p)
        {
            World.LastAttack = p.ReadUInt();

            if (World.LastAttack != 0 && World.InGame)
            {
                Mobile mob = World.Mobiles.Get(World.LastAttack);

                if (mob != null && mob.HitsMax <= 0)
                    NetClient.Socket.Send(new PStatusRequest(World.LastAttack));
            }
            //Mobile lastattackedmobile = World.Mobiles.Get(p.ReadUInt());

            //if (lastattackedmobile != null)
            //{
            //}
        }

        private static void TextEntryDialog(Packet p)
        {
            if (!World.InGame)
                return;

            Serial serial = p.ReadUInt();
            byte parentID = p.ReadByte();
            byte buttonID = p.ReadByte();

            ushort textLen = p.ReadUShort();
            string text = p.ReadASCII(textLen);

            bool haveCancel = p.ReadBool();
            byte variant = p.ReadByte();
            uint maxLength = p.ReadUInt();

            ushort descLen = p.ReadUShort();
            string desc = p.ReadASCII(descLen);

            TextEntryDialogGump gump = new TextEntryDialogGump(serial, 143, 172, variant, (int) maxLength, text, desc, buttonID, parentID)
            {
                CanCloseWithRightClick = haveCancel
            };

            Engine.UI.Add(gump);
        }

        private static void UnicodeTalk(Packet p)
        {
            Serial serial = p.ReadUInt();
            Entity entity = World.Get(serial);
            ushort graphic = p.ReadUShort();
            MessageType type = (MessageType)p.ReadByte();
            Hue hue = p.ReadUShort();
            MessageFont font = (MessageFont)p.ReadUShort();
            string lang = p.ReadASCII(4);
            string name = p.ReadASCII(30);
            string text = p.ReadUnicode();

            if (entity != null)
            {
                entity.Graphic = graphic;
                entity.Name = name;
                entity.ProcessDelta();
            }

            Chat.OnMessage(entity, text, hue, type, (MessageFont)Engine.Profile.Current.ChatFont, true, lang);
        }

        private static void OpenGump(Packet p)
        {
            if (World.Player == null)
                return;

            Serial sender = p.ReadUInt();
            Serial gumpID = p.ReadUInt();
            int x = (int)p.ReadUInt();
            int y = (int)p.ReadUInt();

            ushort cmdLen = p.ReadUShort();
            string cmd = p.ReadASCII(cmdLen);

            ushort textLinesCount = p.ReadUShort();

            string[] lines = new string[textLinesCount];

            byte[] decData = p.ReadArray(p.Length - p.Position);

            for (int i = 0, index = 0; i < textLinesCount; i++)
            {
                int length = (decData[index++] << 8) | decData[index++];
                byte[] text = new byte[length * 2];
                Buffer.BlockCopy(decData, index, text, 0, text.Length);
                index += text.Length;
                lines[i] = Encoding.BigEndianUnicode.GetString(text);
                //ushort lineLen = p.ReadUShort();
                ////byte[] text = new byte[lineLen * 2];
                ////Buffer.BlockCopy();
                //string text = p.ReadUnicode(lineLen);

                //lines[i] = text;
            }

            Engine.UI.Create(sender, gumpID, x, y, cmd, lines);
        }

        private static void ChatMessage(Packet p)
        {
        }

        private static void Help(Packet p)
        {
        }

        private static void CharacterProfile(Packet p)
        {
            var serial = p.ReadUInt();
            var header = p.ReadASCII();
            var footer = p.ReadUnicode();
            var body = p.ReadUnicode();

            Engine.UI.GetByLocalSerial<ProfileGump>(serial)?.Dispose();
            Engine.UI.Add(new ProfileGump(serial, header, footer, body, (serial == World.Player.Serial)));
        }

        private static void EnableLockedFeatures(Packet p)
        {
            uint flags = 0;

            if (FileManager.ClientVersion >= ClientVersions.CV_60142)
                flags = p.ReadUInt();
            else
                flags = p.ReadUShort();
            World.ClientLockedFeatures.SetFlags((LockedFeatureFlags)flags);

            FileManager.Animations.UpdateAnimationTable(flags);
        }

        private static void DisplayQuestArrow(Packet p)
        {
            var ui = Engine.UI;

            var display = p.ReadBool();
            var mx = p.ReadUShort();
            var my = p.ReadUShort();

            var serial = default(Serial);

            if (FileManager.ClientVersion >= ClientVersions.CV_7090)
                serial = p.ReadUInt();

            ui.GetByLocalSerial<QuestArrowGump>(serial)?.Dispose();

            if (display)
                ui.Add(new QuestArrowGump(serial, mx, my));
        }

        private static void UltimaMessengerR(Packet p)
        {
        }

        private static void Season(Packet p)
        {
            if (World.Player == null)
                return;

            byte season = p.ReadByte();
            byte music = p.ReadByte();

            if (Engine.Profile.Current.EnableCombatMusic)
                Engine.SceneManager.CurrentScene.Audio.PlayMusic(music);
        }

        private static void ClientVersion(Packet p)
        {
            //new PClientVersion().SendToServer();
        }

        private static void AssistVersion(Packet p)
        {
            //uint version = p.ReadUInt();

            //string[] parts = Service.GetByLocalSerial<Settings>().ClientVersion.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            //byte[] clientVersionBuffer =
            //    {byte.Parse(parts[0]), byte.Parse(parts[1]), byte.Parse(parts[2]), byte.Parse(parts[3])};

            //NetClient.Socket.Send(new PAssistVersion(clientVersionBuffer, version));
        }

        private static void ExtendedCommand(Packet p)
        {
            switch (p.ReadUShort())
            {
                case 0:

                    break;
                //===========================================================================================
                //===========================================================================================
                case 1: // fast walk prevention

#if !JAEDAN_MOVEMENT_PATCH && !MOVEMENT2

                    for (int i = 0; i < 6; i++)
                    {
                        World.Player.Walker.FastWalkStack.SetValue(i, p.ReadUInt());
                    }
#endif
                    break;
                //===========================================================================================
                //===========================================================================================
                case 2: // add key to fast walk stack
#if !JAEDAN_MOVEMENT_PATCH && !MOVEMENT2
                    World.Player.Walker.FastWalkStack.AddValue(p.ReadUInt());
#endif
                    break;
                //===========================================================================================
                //===========================================================================================
                case 4: // close generic gump
                    Engine.UI.GetByServerSerial(p.ReadUInt())?.OnButtonClick((int)p.ReadUInt());

                    break;
                //===========================================================================================
                //===========================================================================================
                case 6: //party
                    PartyManager.HandlePartyPacket(p);
                    break;
                //===========================================================================================
                //===========================================================================================
                case 8: // map change
                    World.MapIndex = p.ReadByte();

                    break;
                //===========================================================================================
                //===========================================================================================
                case 0x0C: // close statusbar gump
                    Engine.UI.Remove<HealthBarGump>(p.ReadUInt());
                    break;
                //===========================================================================================
                //===========================================================================================
                case 0x10: // display equip info
                    Item item = World.Items.Get(p.ReadUInt());

                    if (item == null) return;
                    uint cliloc = p.ReadUInt();
                    string str = string.Empty;

                    if (cliloc > 0)
                    {
                        str = FileManager.Cliloc.Translate(FileManager.Cliloc.GetString((int)cliloc), capitalize: true);

                        if (!string.IsNullOrEmpty(str))
                            item.Name = str;
                        item.AddOverhead(MessageType.Label, str, 3, 0x3B2, true, 4000.0f);
                    }

                    str = string.Empty;
                    ushort crafterNameLen = 0;
                    uint next = p.ReadUInt();
                    StringBuilder strBuffer = new StringBuilder();

                    if (next == 0xFFFFFFFD)
                    {
                        crafterNameLen = p.ReadUShort();

                        if (crafterNameLen > 0)
                        {
                            strBuffer.Append("Crafted by ");
                            strBuffer.Append(p.ReadASCII(crafterNameLen));
                        }
                    }

                    if (crafterNameLen != 0) next = p.ReadUInt();
                    if (next == 0xFFFFFFFC) strBuffer.Append("[Unidentified");
                    byte count = 0;

                    while (p.Position < p.Length - 4)
                    {
                        if (count != 0 || next == 0xFFFFFFFD || next == 0xFFFFFFFC) next = p.ReadUInt();
                        short charges = (short)p.ReadUShort();
                        string attr = FileManager.Cliloc.GetString((int)next);

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

                    break;
                //===========================================================================================
                //===========================================================================================
                case 0x14: // display popup/context menu
                    PopupMenuData data = PopupMenuData.Parse(p);

                    Engine.UI.Add(new PopupMenuGump(data)
                    {
                        X = Mouse.Position.X,
                        Y = Mouse.Position.Y
                    });

                    break;
                //===========================================================================================
                //===========================================================================================
                case 0x16: // close user interface windows
                    uint id = p.ReadUInt();
                    Serial serial = p.ReadUInt();

                    switch (id)
                    {
                        case 1: // paperdoll
                            Engine.UI.Remove<PaperDollGump>(serial);
                            break;
                        case 2: //statusbar
                            Engine.UI.Remove<HealthBarGump>(serial);
                            break;
                        case 8: // char profile
                            Engine.UI.Remove<ProfileGump>();
                            break;
                        case 0x0C: //container
                            Engine.UI.Remove<ContainerGump>(serial);
                            break;
                    }

                    break;
                //===========================================================================================
                //===========================================================================================
                case 0x18: // enable map patches

                    if (FileManager.Map.ApplyPatches(p))
                    {
                        int indx = World.MapIndex;
                        World.MapIndex = -1;
                        World.MapIndex = indx;
                    }

                    break;
                //===========================================================================================
                //===========================================================================================
                case 0x19: //extened stats
                    byte version = p.ReadByte();
                    serial = p.ReadUInt();

                    switch (version)
                    {
                        case 0:
                            Mobile bonded = World.Mobiles.Get(serial);

                            if (bonded == null) break;
                            bool dead = p.ReadBool();
                            bonded.IsDead = dead;
                            break;
                        case 2:

                            if (serial == World.Player)
                            {
                                byte updategump = p.ReadByte();
                                byte state = p.ReadByte();

                                //TODO: drawstatlockers = true
                                World.Player.StrLock = (Lock)((state >> 4) & 3);
                                World.Player.DexLock = (Lock)((state >> 2) & 3);
                                World.Player.IntLock = (Lock)(state & 3);
                            }

                            break;
                        case 5:
                            Mobile character = World.Mobiles.Get(serial);

                            if (character == null) return;
                            if (p.Length == 19) dead = p.ReadBool();

                            break;
                    }

                    break;
                //===========================================================================================
                //===========================================================================================
                case 0x1B: // new spellbook content
                    p.Skip(2);
                    Item spellbook = World.GetOrCreateItem(p.ReadUInt());
                    spellbook.Graphic = p.ReadUShort();

                    if (!spellbook.IsSpellBook)
                        return;
                    ushort type = p.ReadUShort();
                    ulong filed = p.ReadUInt() + ((ulong)p.ReadUInt() << 32);
                    SpellBookType sbtype = SpellBookType.Unknown;

                    switch (type)
                    {
                        case 1:
                            sbtype = SpellBookType.Magery;

                            break;
                        case 101:
                            sbtype = SpellBookType.Necromancy;

                            break;
                        case 201:
                            sbtype = SpellBookType.Chivalry;

                            break;
                        case 401:
                            sbtype = SpellBookType.Bushido;

                            break;
                        case 501:
                            sbtype = SpellBookType.Ninjitsu;

                            break;
                        case 601:
                            sbtype = SpellBookType.Spellweaving;

                            break;
                    }

                    if (spellbook.FillSpellbook(sbtype, filed))
                    {
                        SpellbookGump gump = Engine.UI.GetByLocalSerial<SpellbookGump>(spellbook);
                        gump?.Update();
                    }

                    break;
                //===========================================================================================
                //===========================================================================================
                case 0x1D: // house revision state
                    serial = p.ReadUInt();
                    uint revision = p.ReadUInt();

                    if (!World.HouseManager.TryGetHouse(serial, out House house) || !house.IsCustom || house.Revision != revision)
                    {
                        NetClient.Socket.Send(new PCustomHouseDataRequest(serial));
                    }
                    else
                    {
                        house.Generate();
                        Engine.UI.GetByLocalSerial<MiniMapGump>()?.ForceUpdate();
                    }

                    break;
                //===========================================================================================
                //===========================================================================================
                case 0x20:
                    serial = p.ReadUInt();
                    type = p.ReadByte();
                    Graphic graphic = p.ReadUShort();
                    Position position = new Position(p.ReadUShort(), p.ReadUShort(), p.ReadSByte());

                    switch (type)
                    {
                    }

                    break;
                //===========================================================================================
                //===========================================================================================
                case 0x21:
                    World.Player.PrimaryAbility = (Ability)((byte)World.Player.PrimaryAbility & 0x7F);
                    World.Player.SecondaryAbility = (Ability)((byte)World.Player.SecondaryAbility & 0x7F);

                    break;
                //===========================================================================================
                //===========================================================================================
                case 0x22:
                    p.Skip(1);

                    Mobile mobile = World.Mobiles.Get(p.ReadUInt());

                    if (mobile != null)
                    {
                        byte damage = p.ReadByte();
                        Engine.SceneManager.GetScene<GameScene>().Overheads.AddDamage(mobile, new DamageOverhead(mobile, damage.ToString(), hue: (Hue)(mobile == World.Player ? 0x0034 : 0x0021), font: 3, isunicode: false, timeToLive: 1500));
                    }

                    break;
                //===========================================================================================
                //===========================================================================================
                case 0x26:
                    byte val = p.ReadByte();

                    if (val > (int)CharacterSpeedType.FastUnmountAndCantRun)
                        val = 0;
                    World.Player.SpeedMode = (CharacterSpeedType)val;

                    break;
            }
        }

        [Flags]
        private enum AffixType
        {
            Append = 0x00,
            Prepend = 0x01,
            System = 0x02
        }

        private static void DisplayClilocString(Packet p)
        {
            if (World.Player == null)
                return;
            Serial serial = p.ReadUInt();
            Entity entity = World.Get(serial);
            ushort graphic = p.ReadUShort();
            MessageType type = (MessageType)p.ReadByte();
            Hue hue = p.ReadUShort();
            MessageFont font = (MessageFont)p.ReadUShort();
            uint cliloc = p.ReadUInt();
            AffixType flags = (p.ID == 0xCC) ? (AffixType)p.ReadByte() : 0x00;
            string name = p.ReadASCII(30);
            string affix = (p.ID == 0xCC) ? p.ReadASCII() : String.Empty;

            string arguments = null;

            if (p.Position < p.Length)
                arguments = p.ReadUnicodeReversed(p.Length - p.Position);

            string text = FileManager.Cliloc.Translate((int)cliloc, arguments);

            if (!String.IsNullOrWhiteSpace(affix))
            {
                if ((flags & AffixType.Prepend) != 0)
                    text = $"{affix}{text}";
                else
                    text = $"{text}{affix}";
            }

            if ((flags & AffixType.System) != 0)
                type = MessageType.System;

            if (!FileManager.Fonts.UnicodeFontExists((byte)font))
                font = MessageFont.Bold;

            if (entity != null)
            {
                entity.Graphic = graphic;
                entity.Name = name;
                entity.ProcessDelta();
            }

            Chat.OnMessage(entity, text, hue, type, font, true);
        }

        private static void UnicodePrompt(Packet p)
        {
            if (!World.InGame)
                return;

            byte[] data = p.ReadArray(8);

            Chat.PromptData = new PromptData()
            {
                Prompt = ConsolePrompt.Unicode,
                Data = data
            };
        }

        private static void Semivisible(Packet p)
        {
        }

        private static void InvalidMapEnable(Packet p)
        {
        }

        private static void ParticleEffect3D(Packet p)
        {
        }

        private static void GetUserServerPingGodClientR(Packet p)
        {
        }

        private static void GlobalQueCount(Packet p)
        {
        }

        private static void ConfigurationFileR(Packet p)
        {
        }

        private static void Logout(Packet p)
        {
        }

        private static void MegaCliloc(Packet p)
        {
            p.Skip(2);
            Entity entity = World.Get(p.ReadUInt());

            if (entity == null) return;
            p.Skip(2);
            entity.PropertiesHash = p.ReadUInt();
            entity.UpdateProperties(ReadProperties(p));
            entity.ProcessDelta();
        }

        private static void GenericAOSCommandsR(Packet p)
        {
        }

        private static void CustomHouse(Packet p)
        {
            bool compressed = p.ReadByte() == 0x03;
            bool enableReponse = p.ReadBool();
            Serial serial = p.ReadUInt();
            Item foundation = World.Items.Get(serial);
            uint revision = p.ReadUInt();

            MultiInfo multi = foundation.MultiInfo;
            if (!foundation.IsMulti || multi == null)
                return;

            p.Skip(4);

            if (!World.HouseManager.TryGetHouse(foundation, out House house))
            {
                house = new House(foundation, revision, true);
                World.HouseManager.Add(foundation, house);
            }
            else
            {
                house.ClearComponents();
                house.Revision = revision;
                house.IsCustom = true;
            }

            short minX = multi.MinX;
            short minY = multi.MinY;
            short maxY = multi.MaxY;
            byte planes = p.ReadByte();

            for (int plane = 0; plane < planes; plane++)
            {
                uint header = p.ReadUInt();
                int dlen = (int)(((header & 0xFF0000) >> 16) | ((header & 0xF0) << 4));
                int clen = (int)(((header & 0xFF00) >> 8) | ((header & 0x0F) << 8));
                int planeZ = (int)((header & 0x0F000000) >> 24);
                int planeMode = (int)((header & 0xF0000000) >> 28);

                if (clen <= 0) continue;
                byte[] compressedBytes = new byte[clen];
                Buffer.BlockCopy(p.ToArray(), p.Position, compressedBytes, 0, clen);
                byte[] decompressedBytes = new byte[dlen];
                ZLib.Decompress(compressedBytes, 0, decompressedBytes, dlen);

                //ZLib.Unpack(decompressedBytes, ref dlen, compressedBytes, clen);
                Packet stream = new Packet(decompressedBytes, dlen);

                // using (BinaryReader stream = new BinaryReader(new MemoryStream(decompressedBytes)))
                {
                    p.Skip(clen);
                    ushort id = 0;
                    sbyte x = 0, y = 0, z = 0;

                    switch (planeMode)
                    {
                        case 0:

                            for (uint i = 0; i < decompressedBytes.Length / 5; i++)
                            {
                                id = stream.ReadUShort();
                                x = stream.ReadSByte();
                                y = stream.ReadSByte();
                                z = stream.ReadSByte();

                                if (id != 0)
                                {
                                    house.Components.Add(new Multi(id)
                                    {
                                        Position = new Position((ushort)(foundation.X + x), (ushort)(foundation.Y + y), (sbyte)(foundation.Z + z))
                                    });
                                }
                            }

                            break;
                        case 1:

                            if (planeZ > 0)
                                z = (sbyte)(((planeZ - 1) % 4) * 20 + 7);
                            else
                                z = 0;

                            for (uint i = 0; i < (decompressedBytes.Length >> 2); i++)
                            {
                                id = stream.ReadUShort();
                                x = stream.ReadSByte();
                                y = stream.ReadSByte();

                                if (id != 0)
                                {
                                    house.Components.Add(new Multi(id)
                                    {
                                        Position = new Position((ushort)(foundation.X + x), (ushort)(foundation.Y + y), (sbyte)(foundation.Z + z))
                                    });
                                }
                            }

                            break;
                        case 2:
                            short offX = 0, offY = 0;
                            short multiHeight = 0;

                            if (planeZ > 0)
                                z = (sbyte)(((planeZ - 1) % 4) * 20 + 7);
                            else
                                z = 0;

                            if (planeZ <= 0)
                            {
                                offX = minX;
                                offY = minY;
                                multiHeight = (short)((maxY - minY) + 2);
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
                                multiHeight = (short)((maxY - minY) + 1);
                            }

                            for (uint i = 0; i < (decompressedBytes.Length >> 1); i++)
                            {
                                id = stream.ReadUShort();
                                x = (sbyte)((i / multiHeight) + offX);
                                y = (sbyte)((i % multiHeight) + offY);

                                if (id != 0)
                                {
                                    house.Components.Add(new Multi(id)
                                    {
                                        Position = new Position((ushort)(foundation.X + x), (ushort)(foundation.Y + y), (sbyte)(foundation.Z + z))
                                    });
                                }
                            }

                            break;
                    }

                    house.Generate();
                    Engine.UI.GetByLocalSerial<MiniMapGump>()?.ForceUpdate();
                }
            }
        }

        private static void CharacterTransferLog(Packet p)
        {
        }

        private static void OPLInfo(Packet p)
        {
            if (World.ClientFlags.TooltipsEnabled)
            {
                Serial serial = p.ReadUInt();
                uint revision = p.ReadUInt();
                Entity entity = World.Get(serial);
                if (entity != null && entity.PropertiesHash != revision) NetClient.Socket.Send(new PMegaClilocRequest(entity));
            }
        }

        private static void OpenCompressedGump(Packet p)
        {
            Serial sender = p.ReadUInt();
            Serial gumpID = p.ReadUInt();
            uint x = p.ReadUInt();
            uint y = p.ReadUInt();
            uint clen = p.ReadUInt() - 4;
            int dlen = (int)p.ReadUInt();
            byte[] data = p.ReadArray((int)clen);
            byte[] decData = new byte[dlen];
            ZLib.Decompress(data, 0, decData, dlen);
            string layout = Encoding.UTF8.GetString(decData);
            uint linesNum = p.ReadUInt();
            string[] lines = new string[0];

            if (linesNum > 0)
            {
                clen = p.ReadUInt() - 4;
                dlen = (int)p.ReadUInt();
                data = new byte[clen];

                for (int i = 0; i < clen; i++)
                    data[i] = p.ReadByte();
                decData = new byte[dlen];
                ZLib.Decompress(data, 0, decData, dlen);
                lines = new string[linesNum];

                for (int i = 0, index = 0; i < linesNum; i++)
                {
                    int length = (decData[index++] << 8) | decData[index++];
                    byte[] text = new byte[length * 2];
                    Buffer.BlockCopy(decData, index, text, 0, text.Length);
                    index += text.Length;
                    lines[i] = Encoding.BigEndianUnicode.GetString(text);
                }
            }

            Engine.UI.Create(sender, gumpID, (int)x, (int)y, layout, lines);
        }

        private static void UpdateMobileStatus(Packet p)
        {
        }

        private static void BuffDebuff(Packet p)
        {
            const int TABLE_COUNT = 126;
            const ushort BUFF_ICON_START = 0x03E9;
            Serial serial = p.ReadUInt();
            ushort iconID = (ushort)(p.ReadUShort() - BUFF_ICON_START);

            if (iconID < TABLE_COUNT)
            {
                UIManager ui = Engine.UI;
                BuffGump gump = ui.GetByLocalSerial<BuffGump>();
                ushort mode = p.ReadUShort();

                if (mode != 0)
                {
                    p.Skip(12);
                    ushort timer = p.ReadUShort();
                    p.Skip(3);
                    uint titleCliloc = p.ReadUInt();
                    uint descriptionCliloc = p.ReadUInt();
                    uint wtfCliloc = p.ReadUInt();
                    p.Skip(4);
                    string title = FileManager.Cliloc.GetString((int)titleCliloc);
                    string description = string.Empty;
                    string wtf = string.Empty;

                    if (descriptionCliloc != 0)
                    {
                        string args = p.ReadUnicodeReversed();
                        description = "\n" + FileManager.Cliloc.Translate((int)descriptionCliloc, args, true);

                        if (description.Length < 2)
                            description = string.Empty;
                    }

                    if (wtfCliloc != 0)
                        wtf = "\n" + FileManager.Cliloc.GetString((int)wtfCliloc);
                    string text = $"<left>{title}{description}{wtf}</left>";
                    World.Player.AddBuff(BuffTable.Table[iconID], timer, text);
                    gump?.AddBuff(BuffTable.Table[iconID]);
                }
                else
                {
                    World.Player.RemoveBuff(BuffTable.Table[iconID]);
                    gump?.RemoveBuff(BuffTable.Table[iconID]);
                }
            }
        }

        private static void NewCharacterAnimation(Packet p)
        {
            if (World.Player == null)
                return;
            Mobile mobile = World.Mobiles.Get(p.ReadUInt());

            if (mobile == null)
                return;
            ushort type = p.ReadUShort();
            ushort action = p.ReadUShort();
            byte mode = p.ReadByte();
            byte group = Mobile.GetObjectNewAnimation(mobile, type, action, mode);
            mobile.SetAnimation(group);
            mobile.AnimationRepeatMode = 1;
            mobile.AnimationDirection = true;
            if ((type == 1 || type == 2) && mobile.Graphic == 0x0015) mobile.AnimationRepeat = true;
            mobile.AnimationFromServer = true;
        }

        private static void KREncryptionResponse(Packet p)
        {
        }

        private static void KrriosClientSpecial(Packet p)
        {
            byte type = p.ReadByte();

            if (type == 0xFE)
            {
                Log.Message(LogTypes.Info, "Razor ACK sended");
                NetClient.Socket.Send(new PRazorAnswer());
            }
        }

        private static void FreeshardListR(Packet p)
        {
        }

        private static void UpdateItemSA(Packet p)
        {
            if (World.Player == null)
                return;
            p.Skip(2);
            byte type = p.ReadByte();
            Item item = World.GetOrCreateItem(p.ReadUInt());
            item.Graphic = p.ReadUShort();
            ushort graphicInc = p.ReadByte();
            //item.Direction = (Direction)p.ReadByte();
            item.Amount = p.ReadUShort();
            p.Skip(2); //amount again? wtf???
            item.Position = new Position(p.ReadUShort(), p.ReadUShort(), p.ReadSByte());
            item.Direction = (Direction)p.ReadByte();
            item.Hue = p.ReadUShort();
            item.Flags = (Flags)p.ReadByte();
            if (FileManager.ClientVersion >= ClientVersions.CV_7090)
                p.ReadUShort(); //unknown
            item.Container = Serial.INVALID;

            if (item.Graphic != 0x2006)
                item.Graphic += graphicInc;

            if (type == 2)
            {
                item.IsMulti = true;
                item.Graphic = (ushort)(item.Graphic & 0x3FFF);
            }

            item.ProcessDelta();
            if (World.Items.Add(item))
                World.Items.ProcessDelta();

            if (item.ItemData.IsAnimated)
            {
                item.View.AllowedToDraw = false;
                World.AddEffect(new AnimatedItemEffect(item.Serial, item.Graphic, item.Hue, -1));
            }

            if (item.OnGround)
                item.AddToTile();
        }

        private static void PacketList(Packet p)
        {
            if (World.Player == null)
                return;
        }

        private static bool ReadContainerContent(Packet p, List<Item> items)
        {
            Item item = World.GetOrCreateItem(p.ReadUInt());
            item.Graphic = (ushort)(p.ReadUShort() + p.ReadSByte());
            item.Amount = Math.Max(p.ReadUShort(), (ushort)1);
            ushort x = p.ReadUShort();
            ushort y = p.ReadUShort();
            if (FileManager.ClientVersion >= ClientVersions.CV_6017) p.ReadByte(); //gridnumber - useless?
            item.Container = p.ReadUInt();
            item.Position = new Position(x, y);
            item.Hue = p.ReadUShort();
            items.Add(item);
            Entity entity = World.Get(item.Container);

            GameScene gs = Engine.SceneManager.GetScene<GameScene>();

            if (gs != null && gs.HeldItem.Serial == item.Serial && gs.HeldItem.Dropped)
            {
                gs.HeldItem.Clear();
            }

            if (entity != null)
            {
                entity.Items.Add(item);

                foreach (Item i in World.ToAdd.Where(i => i.Container == item))
                {
                    item.Items.Add(i);
                    World.Items.Add(i);
                }

                World.ToAdd.ExceptWith(item.Items);
                item.ProcessDelta();
                entity.ProcessDelta();

                return World.Items.Add(item);
            }

            World.ToAdd.Add(item);
            item.ProcessDelta();

            return false;
        }

        private static IEnumerable<Property> ReadProperties(Packet p)
        {
            uint cliloc;

            while ((cliloc = p.ReadUInt()) != 0)
            {
                ushort len = p.ReadUShort();
                string str = p.ReadUnicodeReversed(len);

                yield return new Property(cliloc, str);
            }
        }
    }
}