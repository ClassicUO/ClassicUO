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
using System.Linq;
using System.Text;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps;
using ClassicUO.Game.Gumps.UIGumps;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.System;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Utility;
using Multi = ClassicUO.Game.GameObjects.Multi;

namespace ClassicUO.Network
{
    public class PacketHandler
    {
        public PacketHandler(Action<Packet> callback) => Callback = callback;

        public Action<Packet> Callback { get; }
    }

    public class PacketHandlers
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
            /*ToServer.Add(0x37, MoveItemGodClient);
            ToServer.Add(0x38, PathfindingInClient);
            ToServer.Add(0x39, RemoveGroupS);*/
            ToClient.Add(0x39, RemoveGroupR);
            /*ToServer.Add(0x3A, SendSkills);*/
            ToClient.Add(0x3A, UpdateSkills);
            //ToServer.Add(0x3B, BuyItems);
            ToClient.Add(0x3C, UpdateContainedItems);
            ToClient.Add(0x3E, VersionGodClient);
            ToClient.Add(0x3F, UpdateStaticsGodClient);
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
            ToClient.Add(0xD4, OpenBookNew); //ToServer.Add(0xD4, BookHeaderNewS);
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
            byte CommandType = p.ReadByte();
            uint CursorID = p.ReadUInt();
            byte CursorType = p.ReadByte();
            TargetSystem.SetTargeting((TargetSystem.TargetType) CommandType, (int) CursorID, CursorType);
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
            Mobile mobile = World.Mobiles.Get(p.ReadUInt());
            if (mobile == null) return;

            ushort damage = p.ReadUShort();
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

                    World.Player.Race = (RaceType) race;
                }
                else
                {
                    if (FileManager.ClientVersion >= ClientVersions.CV_500A)
                        World.Player.WeightMax = (ushort) (7 * (World.Player.Strength / 2) + 40);
                    else
                        World.Player.WeightMax = (ushort) (World.Player.Strength * 4 + 25);
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
                    World.Player.MaxPhysicRes = p.ReadUShort();
                    World.Player.MaxFireRes = p.ReadUShort();
                    World.Player.MaxColdRes = p.ReadUShort();
                    World.Player.MaxPoisonRes = p.ReadUShort();
                    World.Player.MaxEnergyRes = p.ReadUShort();
                    World.Player.DefenseChanceInc = p.ReadUShort();
                    World.Player.MaxDefChance = p.ReadUShort();
                    World.Player.HitChanceInc = p.ReadUShort();
                    World.Player.SwingSpeedInc = p.ReadUShort();
                    World.Player.DamageChanceInc = p.ReadUShort();
                    World.Player.LowerReagentCost = p.ReadUShort();
                    World.Player.SpellDamageInc = p.ReadUShort();
                    World.Player.FasterCastRecovery = p.ReadUShort();
                    World.Player.FasterCasting = p.ReadUShort();
                    World.Player.LowerManaCost = p.ReadUShort();

                    //World.Player.HitChanceInc = p.ReadUShort();
                    //World.Player.SwingSpeedInc = p.ReadUShort();
                    //World.Player.DamageChanceInc = p.ReadUShort();
                    //World.Player.LowerReagentCost = p.ReadUShort();
                    //World.Player.HitPointsRegen = p.ReadUShort();
                    //World.Player.StaminaRegen = p.ReadUShort(); 
                    //World.Player.ManaRegen = p.ReadUShort(); 
                    //World.Player.ReflectPhysicalDamage = p.ReadUShort();
                    //World.Player.EnhancePotions = p.ReadUShort();
                    //World.Player.DefenseChanceInc = p.ReadUShort();
                    //World.Player.SpellDamageInc = p.ReadUShort();
                    //World.Player.FasterCastRecovery = p.ReadUShort();
                    //World.Player.FasterCasting = p.ReadUShort();
                    //World.Player.LowerManaCost = p.ReadUShort();
                    //World.Player.StrengthInc = p.ReadUShort();
                    //World.Player.DexterityInc = p.ReadUShort();
                    //World.Player.IntelligenceInc = p.ReadUShort();
                    //World.Player.HitPointsInc = p.ReadUShort();
                    //World.Player.StaminaInc = p.ReadUShort();
                    //World.Player.ManaInc = p.ReadUShort();
                    //World.Player.MaximumHitPointsInc = p.ReadUShort();
                    //World.Player.MaximumStaminaInc = p.ReadUShort();
                    //World.Player.MaximumManaInc = p.ReadUShort();
                }
            }

            mobile.ProcessDelta();
        }

        private static void FollowR(Packet p)
        {
            Serial tofollow = p.ReadUInt();
            Serial isfollowing = p.ReadUInt();
        }

        /* private static void NewHealthBarStatusUpdateSA(Packet p)
        {

        }*/

        private static void NewHealthbarUpdate(Packet p)
        {
            if (p.ID == 0x16 && FileManager.ClientVersion < ClientVersions.CV_500A) return;

            Mobile mobile = World.Mobiles.Get(p.ReadUInt());
            if (mobile == null) return;

            ushort count = p.ReadUShort();
            for (int i = 0; i < count; i++)
            {
                ushort type = p.ReadUShort();
                bool enabled = p.ReadBool();
                byte flags = (byte) mobile.Flags;

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
                            flags &= 0x04;
                    }
                }
                else if (type == 2)
                {
                    if (enabled)
                        flags |= 0x08;
                    else
                        flags &= 0x08;

                    //if (FileManager.ClientVersion >= ClientVersions.CV_7000)
                    //    mobile.SetSAPoison(false);
                    //else
                    //    flags &= 0x04;
                }
                else if (type == 3)
                {
                    //if (enabled)
                    //    flags |= 0x08;
                    //else
                    //    flags &= 0x08;
                }

                mobile.Flags = (Flags) flags;
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
            item.Flags = (Flags) flags;
            item.Direction = (Direction) direction;

            if (graphic >= 0x4000)
            {
                item.IsMulti = true;
                item.Graphic -= 0x4000;
            }


            //uint serial = p.ReadUInt();
            //Item item = World.GetOrCreateItem(serial & 0x7FFFFFFF);

            //ushort graphic = (ushort) (p.ReadUShort() & 0x3FFF);
            //item.Amount = (serial & 0x80000000) != 0 ? p.ReadUShort() : (ushort) 1;

            //if ((graphic & 0x8000) != 0)
            //    item.Graphic = (ushort) (graphic & (0x7FFF + p.ReadSByte()));
            //else
            //    item.Graphic = (ushort) (graphic & 0x7FFF);

            //ushort x = p.ReadUShort();
            //ushort y = p.ReadUShort();

            //if ((x & 0x8000) != 0)
            //    item.Direction = (Direction) p.ReadByte(); //wtf???

            ////item.Position.Set((ushort)(x & 0x7FFF), (ushort)(y & 0x3FFF), p.ReadSByte());
            //item.Position = new Position((ushort) (x & 0x7FFF), (ushort) (y & 0x3FFF), p.ReadSByte());

            //if ((y & 0x8000) != 0)
            //    item.Hue = p.ReadUShort();

            //if ((y & 0x4000) != 0)
            //    item.Flags = (Flags) p.ReadByte();

            //item.IsMulti = item.Graphic >= 0x4000;

            //if (item.IsMulti)
            //    item.Graphic -= 0x4000;

            item.Container = Serial.Invalid;
            item.ProcessDelta();
            if (World.Items.Add(item)) World.Items.ProcessDelta();

            if (TileData.IsAnimated((long) item.ItemData.Flags))
                item.Effect = new AnimatedItemEffect(item.Serial, item.Graphic, item.Hue, -1);
        }

        private static void EnterWorld(Packet p)
        {
            World.Mobiles.Add(World.Player = new PlayerMobile(p.ReadUInt()));
            p.Skip(4);
            World.Player.Graphic = p.ReadUShort();
            World.Player.Position = new Position(p.ReadUShort(), p.ReadUShort(), (sbyte) p.ReadUShort());
            //World.Player.Position.Set(p.ReadUShort(), p.ReadUShort(), (sbyte)p.ReadUShort());
            World.Player.Direction = (Direction) p.ReadByte();
            World.Player.ProcessDelta();
            World.Mobiles.ProcessDelta();


            var settings = Service.Get<Settings>();

            NetClient.Socket.Send(new PClientVersion(settings.ClientVersion));

            if (FileManager.ClientVersion >= ClientVersions.CV_200)
            {
                NetClient.Socket.Send(new PGameWindowSize((uint) settings.GameWindowWidth,
                    (uint) settings.GameWindowHeight));
                NetClient.Socket.Send(new PLanguage("ENU"));
            }


            GameActions.SingleClick(World.Player);
            NetClient.Socket.Send(new PStatusRequest(World.Player));

            Service.Get<SceneManager>().ChangeScene(ScenesType.Game);
        }

        private static void Talk(Packet p)
        {
            Serial serial = p.ReadUInt();
            Entity entity = World.Get(serial);
            ushort graphic = p.ReadUShort();
            MessageType type = (MessageType) p.ReadByte();
            Hue hue = p.ReadUShort();
            MessageFont font = (MessageFont) p.ReadUShort();
            string name = p.ReadASCII(30);
            string text = p.ReadASCII();

            if (serial <= 0 && graphic <= 0 && type == MessageType.Regular && font == MessageFont.INVALID &&
                hue == 0xFFFF && name.StartsWith("SYSTEM"))
            {
                NetClient.Socket.Send(new PACKTalk());
                return;
            }

            if (entity != null)
            {
                entity.Graphic = graphic;
                entity.Name = name;
                entity.ProcessDelta();
            }

            Chat.OnMessage(entity, new UOMessageEventArgs(text, hue, type, font, false));
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

            if (serial.IsItem)
            {
                Item item = World.Items.Get(serial);
                if (!item.OnGround && item.Container.IsValid)
                    World.Get(item.Container).Items.Remove(item);

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

            World.Player.Graphic = (ushort) (p.ReadUShort() + p.ReadSByte());
            World.Player.Hue = p.ReadUShort();
            World.Player.Flags = (Flags) p.ReadByte();
            ushort x = p.ReadUShort();
            ushort y = p.ReadUShort();
            p.Skip(2);
            Direction direction = (Direction) p.ReadByte();
            sbyte z = p.ReadSByte();


            // Direction dir = direction & Direction.Up;

            int endX = 0, endY = 0;
            sbyte endZ = 0;
            Direction endDir = Direction.NONE;

            World.Player.GetEndPosition(ref endX, ref endY, ref endZ, ref endDir);

            World.Player.SequenceNumber = 0;

            if (endX != x || endY != y)
            {
                //World.Player.ForcePosition(x, y, z, direction);

                World.Player.ResetSteps();
                World.Player.Position = new Position(x, y, z);
                World.Player.Direction = direction;
            }
            else if ((endDir & Direction.Up) != (direction & Direction.Up))
                World.Player.EnqueueStep(x, y, z, direction, (direction & Direction.Running) != 0);
            else if (World.Player.Tile == null)
                World.Player.Tile = World.Map.GetTile(x, y);


            World.Player.ProcessDelta();
        }

        private static void DenyWalk(Packet p)
        {
            if (World.Player == null)
                return;

            byte seq = p.ReadByte();
            ushort x = p.ReadUShort();
            ushort y = p.ReadUShort();
            Direction direction = (Direction) p.ReadByte();
            sbyte z = p.ReadSByte();
            Position position = new Position(x, y, z);

            World.Player.DenyWalk(seq, direction, position);
            World.Player.ProcessDelta();
        }

        private static void ConfirmWalk(Packet p)
        {
            if (World.Player == null)
                return;

            byte seq = p.ReadByte();
            byte noto = (byte) (p.ReadByte() & ~0x40);
            if (noto <= 0 || noto >= 7)
                noto = 0x01;

            World.Player.Notoriety = (Notoriety) noto;
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

            Item item = World.Items.Get(p.ReadUInt());
            Graphic graphic = p.ReadUShort();

            UIManager ui = Service.Get<UIManager>();
            if (ui.GetByLocalSerial(item.Serial) != null)
                return;

            if (graphic == 0x30) // vendor
            {
            }
            else if (graphic == 0xFFFF) // spellbook
            {
                if (item.IsSpellBook)
                {
                    SpellbookGump spellbookGump = new SpellbookGump(item)
                    {
                        X = 100, Y = 100
                    };

                    ui.Add(spellbookGump);
                }
            }
            else
            {
                if (item.IsCorpse)
                {
                }
                else
                {
                    ContainerGump container = new ContainerGump(item, graphic)
                    {
                        X = 64, Y = 64
                    };
                    ui.Add(container);
                }
            }
        }

        private static void UpdateContainedItem(Packet p)
        {
            List<Item> items = new List<Item>();
            if (ReadContainerContent(p, items))
                World.Items.ProcessDelta();
        }

        private static void KickPlayer(Packet p)
        {
        }

        private static void DenyMoveItem(Packet p)
        {
        }

        private static void EndDraggingItem(Packet p)
        {
        }

        private static void DropItemAccepted(Packet p)
        {
        }

        private static void Blood(Packet p)
        {
        }

        private static void GodMode(Packet p)
        {
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
            item.Graphic = (ushort) (p.ReadUShort() + p.ReadSByte());
            item.Layer = (Layer) p.ReadByte();
            item.Container = p.ReadUInt();
            item.Hue = p.ReadUShort();
            item.Amount = 1;

            Mobile mobile = World.Mobiles.Get(item.Container);

            if (mobile != null) // could it render bad mobiles?
            {
                mobile.Equipment[(int) item.Layer] = item;
                mobile.Items.Add(item);
            }

            item.ProcessDelta();
            if (World.Items.Add(item)) World.Items.ProcessDelta();

            mobile?.ProcessDelta();

            if (mobile == World.Player) World.Player.UpdateAbilities();
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
                    while ((id = p.ReadUShort()) > 0)
                        World.Player.UpdateSkill(id - 1, p.ReadUShort(), p.ReadUShort(), (SkillLock) p.ReadByte(), 100);

                    break;

                case 2:
                    while ((id = p.ReadUShort()) > 0)
                    {
                        World.Player.UpdateSkill(id - 1, p.ReadUShort(), p.ReadUShort(), (SkillLock) p.ReadByte(),
                            p.ReadUShort());
                    }

                    break;

                case 0xDF:
                    id = p.ReadUShort();
                    World.Player.UpdateSkill(id, p.ReadUShort(), p.ReadUShort(), (SkillLock) p.ReadByte(),
                        p.ReadUShort());
                    break;

                case 0xFF:
                    id = p.ReadUShort();
                    World.Player.UpdateSkill(id, p.ReadUShort(), p.ReadUShort(), (SkillLock) p.ReadByte(), 100);
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

        private static void ResourceTileDataGodClient(Packet p)
        {
        }

        private static void UpdateContainedItems(Packet p)
        {
            ushort count = p.ReadUShort();
            List<Item> items = new List<Item>(count);

            for (int i = 0; i < count; i++)
                ReadContainerContent(p, items);

            if (items.Count > 0)
            {
                Item container = World.Items.Get(items[0].Container);
                if (container != null && container.IsSpellBook &&
                    SpellbookData.GetTypeByGraphic(container.Graphic) != SpellBookType.Unknown)
                {
                    SpellbookData.GetData(container, out ulong field, out SpellBookType type);
                    container.FillSpellbook(type, field);
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
            World.Light.Personal = 0;
            // if (World.Player == p.ReadUInt()) World.Light.Personal = 0; // p.ReadByte();
        }

        private static void LightLevel(Packet p)
        {
            World.Light.Overall = 0; // p.ReadByte();
        }

        private static void ErrorCode(Packet p)
        {
        }

        private static void PlaySoundEffect(Packet p)
        {
        }

        private static void LoginComplete(Packet p)
        {
            //Load();
        }

        private static void MapData(Packet p)
        {
        }

        private static void SetTime(Packet p)
        {
        }

        private static void SetWeather(Packet p)
        {
        }

        private static void BookData(Packet p)
        {
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

            mobile.SetAnimation(Mobile.GetReplacedObjectAnimation(mobile, action), delay, (byte) frameCount,
                (byte) repeatMode, repeat, frameDirection);
            mobile.AnimationFromServer = true;
        }

        private static void GraphicEffect(Packet p)
        {
        }

        private static void ClientViewRange(Packet p)
        {
            World.ViewRange = p.ReadByte();
        }

        private static void BulletinBoardData(Packet p)
        {
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

            // to finish
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
            Direction direction = (Direction) p.ReadByte();
            mobile.Hue = p.ReadUShort();
            mobile.Flags = (Flags) p.ReadByte();
            mobile.Notoriety = (Notoriety) p.ReadByte();
            mobile.ProcessDelta();
            if (World.Mobiles.Add(mobile)) World.Mobiles.ProcessDelta();

            if (mobile == World.Player) return;

            if (World.Get(mobile) == null)
            {
                mobile.Position = new Position((ushort) x, (ushort) y, z);
                mobile.Direction = direction;
            }

            if (!mobile.EnqueueStep(x, y, z, direction, (direction & Direction.Running) != 0))
            {
                mobile.Position = new Position((ushort) x, (ushort) y, z);
                mobile.Direction = direction;
                mobile.ClearSteps();
            }
        }

        private static void UpdateObject(Packet p)
        {
            if (World.Player == null) return;

            Mobile mobile = World.GetOrCreateMobile(p.ReadUInt());
            mobile.Graphic = p.ReadUShort();
            ushort x = p.ReadUShort();
            ushort y = p.ReadUShort();
            sbyte z = p.ReadSByte();
            Direction direction = (Direction) p.ReadByte();
            mobile.Hue = p.ReadUShort();
            mobile.Flags = (Flags) p.ReadByte();
            mobile.Notoriety = (Notoriety) p.ReadByte();

            if (p.ID != 0x78) p.Skip(6);

            uint itemSerial;
            while ((itemSerial = p.ReadUInt()) != 0)
            {
                Item item = World.GetOrCreateItem(itemSerial);
                Graphic graphic = p.ReadUShort();
                item.Layer = (Layer) p.ReadByte();

                if (FileManager.ClientVersion >= ClientVersions.CV_70331)
                {
                    item.Hue = p.ReadUShort();
                    item.Graphic = graphic;
                }
                else if (FileManager.ClientVersion >= ClientVersions.CV_7000)
                {
                    item.Graphic = (ushort) (graphic & 0x7FFF);
                    item.Hue = p.ReadUShort();
                }
                else
                    item.Graphic = (ushort) (graphic & 0x3FFF);

                item.Amount = 1;
                item.Container = mobile;
                mobile.Items.Add(item);
                mobile.Equipment[(int) item.Layer] = item;
                item.ProcessDelta();
                World.Items.Add(item);
            }


            if (mobile == World.Player) // resync ?
            {
                //World.Player.ResetSteps();
                //World.Player.Position = new Position(x, y, z);
                //World.Player.Direction = direction;
            }
            else
            {
                direction &= Direction.Up;

                if (World.Get(mobile) == null)
                {
                    mobile.Position = new Position(x, y, z);
                    mobile.Direction = direction;
                }

                if (!mobile.EnqueueStep(x, y, z, direction, (direction & Direction.Running) != 0))
                {
                    mobile.Position = new Position(x, y, z);
                    mobile.Direction = direction;
                    mobile.ClearSteps();
                }
            }

            mobile.ProcessDelta();
            if (World.Mobiles.Add(mobile))
                World.Mobiles.ProcessDelta();
            World.Items.ProcessDelta();

            NetClient.Socket.Send(new PClickRequest(mobile));
        }

        private static void OpenMenu(Packet p)
        {
            Serial serial = p.ReadUInt();
            uint id = p.ReadUInt();
            string name = p.ReadASCII(p.ReadByte());
            int count = p.ReadByte();
            // to finish
        }

        private static void LoginError(Packet p)
        {
            byte errorCode = p.ReadByte();
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

            UIManager ui = Service.Get<UIManager>();
            if (ui.GetByLocalSerial<PaperDollGump>(mobile) == null)
                ui.Add(new PaperDollGump(mobile, text) {X = 100, Y = 100});
        }

        private static void CorpseEquipment(Packet p)
        {
            Entity corpse = World.Get(p.ReadUInt());
            Layer layer = (Layer) p.ReadByte();

            while (layer != Layer.Invalid && p.Position < p.Length)
            {
                Item item = World.Items.Get(p.ReadUInt());
                if (item != null && item.Container == corpse)
                {
                    // put equip
                }

                layer = (Layer) p.ReadByte();
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

            // to finish
        }

        private static void OpenBook(Packet p)
        {
            Item book = World.Items.Get(p.ReadUInt());
            byte flags = p.ReadByte();
            p.Skip(1);
            ushort pages = p.ReadUShort();
            string title = p.ReadASCII(60);
            string author = p.ReadASCII(30);
        }

        private static void DyeData(Packet p)
        {
            Item item = World.Items.Get(p.ReadUInt());
            p.Skip(2);
            Graphic graphic = p.ReadUShort();
        }

        private static void MovePlayer(Packet p)
        {
            Direction direction = (Direction) p.ReadByte();
            World.Player.ProcessDelta();
        }

        private static void AllNames3DGameOnlyR(Packet p)
        {
        }

        private static void MultiPlacement(Packet p)
        {
        }

        private static void ASCIIPrompt(Packet p)
        {
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

            for (int i = 0; i < countItems; i++)
            {
                Item item = World.GetOrCreateItem(p.ReadUInt());
                Graphic graphic = p.ReadUShort();
                Hue hue = p.ReadUShort();
                ushort count = p.ReadUShort();
                ushort price = p.ReadUShort();
                string name = p.ReadASCII(p.ReadByte());

                if (int.TryParse(name, out int clilocnum)) name = Cliloc.GetString(clilocnum);
            }
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
            //int locCount = p.ReadByte();
            //if (FileManager.ClientVersion >= ClientVersions.CV_70130)
            //{
            //    for (int i = 0; i < locCount; i++)
            //    {
            //        byte cityIdx = p.ReadByte();
            //        string cityName = p.ReadASCII(32);
            //        string cityArea = p.ReadASCII(32);
            //        Position cityPosition.Set((ushort)p.ReadUInt(), (ushort)p.ReadUInt(), (sbyte)p.ReadUInt());
            //        uint mapIdx = p.ReadUInt();
            //        uint cliloc = p.ReadUInt();
            //        p.Skip(4);

            //    }
            //}
            //else
            //{
            //    for (int i = 0; i < locCount; i++)
            //    {
            //        byte cityIdx = p.ReadByte();
            //        string cityName = p.ReadASCII(31);
            //        string cityArea = p.ReadASCII(31);
            //    }
            //}
        }

        private static void AttackCharacter(Packet p)
        {
            Mobile lastattackedmobile = World.Mobiles.Get(p.ReadUInt());
            if (lastattackedmobile != null)
            {
            }
        }

        private static void TextEntryDialog(Packet p)
        {
        }

        private static void UnicodeTalk(Packet p)
        {
            Serial serial = p.ReadUInt();
            Entity entity = World.Get(serial);
            ushort graphic = p.ReadUShort();
            MessageType type = (MessageType) p.ReadByte();
            Hue hue = p.ReadUShort();
            MessageFont font = (MessageFont) p.ReadUShort();
            string lang = p.ReadASCII(4);
            string name = p.ReadASCII(30);
            string text = p.ReadUnicode();

            if (entity != null)
            {
                entity.Graphic = graphic;
                entity.Name = name;
                entity.ProcessDelta();
            }

            Chat.OnMessage(entity, new UOMessageEventArgs(text, hue, type, font, true, lang));
        }

        private static void OpenGump(Packet p)
        {
            if (World.Player == null) return;
        }

        private static void ChatMessage(Packet p)
        {
        }

        private static void Help(Packet p)
        {
        }

        private static void CharacterProfile(Packet p)
        {
        }

        private static void EnableLockedFeatures(Packet p)
        {
            uint flags = 0;
            if (FileManager.ClientVersion >= ClientVersions.CV_60142)
                flags = p.ReadUInt();
            else
                flags = p.ReadUShort();

            Animations.UpdateAnimationTable(flags);
        }

        private static void DisplayQuestArrow(Packet p)
        {
        }

        private static void UltimaMessengerR(Packet p)
        {
        }

        private static void Season(Packet p)
        {
            byte season = p.ReadByte();

            byte music = p.ReadByte();
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
                    for (int i = 0; i < 6; i++) p.ReadUInt();

                    break;
                //===========================================================================================
                //===========================================================================================
                case 2: // add key to fast walk stack
                    uint key = p.ReadUInt();
                    break;
                //===========================================================================================
                //===========================================================================================
                case 4: // close generic gump
                    Service.Get<UIManager>().GetByServerSerial(p.ReadUInt())?.OnButtonClick((int) p.ReadUInt());
                    break;
                //===========================================================================================
                //===========================================================================================
                case 6: //party
                    const byte CommandPartyList = 0x01;
                    const byte CommandRemoveMember = 0x02;
                    const byte CommandPrivateMessage = 0x03;
                    const byte CommandPublicMessage = 0x04;
                    const byte CommandInvitation = 0x07;
                    byte SubCommand = p.ReadByte();
                    switch (SubCommand)
                    {
                        case CommandPartyList:
                            int Count = p.ReadByte();
                            Serial[] Serials = new Serial[Count];
                            for (int i = 0; i < Serials.Length; i++)
                            {
                                Serials[i] = p.ReadUInt();
                            }

                            PartySystem.ReceivePartyMemberList(Serials);
                            break;
                        case CommandRemoveMember:
                            Count = p.ReadByte();
                            p.ReadUInt();
                            Serials = new Serial[Count];
                            for (int i = 0; i < Serials.Length; i++)
                            {
                                Serials[i] = p.ReadUInt();
                            }

                            PartySystem.ReceiveRemovePartyMember(Serials);
                            break;
                        case CommandPrivateMessage:
                            //Info = new PartyMessageInfo(reader, true);
                            break;
                        case CommandPublicMessage:
                            //Info = new PartyMessageInfo(reader, false);
                            break;
                        case CommandInvitation: //PARTY INVITE PROGRESS
                            //Info = new PartyInvitationInfo(reader);
                            break;
                    }

                    break;
                //===========================================================================================
                //===========================================================================================
                case 8: // map change
                    World.MapIndex = p.ReadByte();
                    break;
                //===========================================================================================
                //===========================================================================================
                case 0x0C: // close statusbar gump
                    Serial serial = p.ReadUInt();
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
                        str = Cliloc.Translate(Cliloc.GetString((int) cliloc), capitalize: true);

                        if (!string.IsNullOrEmpty(str))
                            item.Name = str;

                        item.AddGameText(MessageType.Label, str, 3, 0x3B2, true, 4000.0f);
                    }

                    str = string.Empty;

                    ushort crafterNameLen = 0;
                    uint next = p.ReadUInt();
                    if (next == 0xFFFFFFFD)
                    {
                        crafterNameLen = p.ReadUShort();
                        if (crafterNameLen > 0) str = "Crafted by " + p.ReadASCII(crafterNameLen);
                    }

                    if (crafterNameLen != 0) next = p.ReadUInt();

                    if (next == 0xFFFFFFFC) str += "[Unidentified";

                    byte count = 0;

                    while (p.Position < p.Length - 4)
                    {
                        if (count != 0 || next == 0xFFFFFFFD || next == 0xFFFFFFFC) next = p.ReadUInt();

                        short charges = (short) p.ReadUShort();
                        string attr = Cliloc.GetString((int) next);
                        if (charges == -1)
                        {
                            if (count > 0)
                            {
                                str += "/";
                                str += attr;
                            }
                            else
                            {
                                str += " [";
                                str += attr;
                            }
                        }
                        else
                        {
                            str += "\n[";
                            str += attr;
                            str += " : ";
                            str += charges.ToString();
                            str += "]";
                            count += 20;
                        }

                        count++;
                    }

                    break;
                //===========================================================================================
                //===========================================================================================
                case 0x14: // display popup/context menu
                    break;
                //===========================================================================================
                //===========================================================================================
                case 0x16: // close user interface windows
                    uint id = p.ReadUInt();
                    serial = p.ReadUInt();
                    switch (id)
                    {
                        case 1: // paperdoll
                            break;
                        case 2: //statusbar
                            break;
                        case 8: // char profile
                            break;
                        case 0x0C: //container
                            break;
                    }

                    break;
                //===========================================================================================
                //===========================================================================================
                case 0x18: // enable map patches

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
                            //bonded.IsDead
                            break;
                        case 2:
                            if (serial == World.Player)
                            {
                                byte updategump = p.ReadByte();
                                byte state = p.ReadByte();
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
                    ulong filed = p.ReadUInt() + ((ulong) p.ReadUInt() << 32);

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

                    spellbook.FillSpellbook(sbtype, filed);

                    break;
                //===========================================================================================
                //===========================================================================================
                case 0x1D: // house revision state
                    serial = p.ReadUInt();
                    uint revision = p.ReadUInt();

                    House house = World.GetHouse(serial);

                    if (house != null && house.Revision == revision)
                    {
                        if (house.Items.Count > 0)
                            house.GenerateCustom();
                        else
                            house.GenerateOriginal(World.Items.Get(house.Serial).Multi);
                    }
                    else
                        NetClient.Socket.Send(new PCustomHouseDataRequest(serial));

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

                    World.Player.PrimaryAbility = (Ability) ((byte) World.Player.PrimaryAbility & 0x7F);
                    World.Player.SecondaryAbility = (Ability) ((byte) World.Player.SecondaryAbility & 0x7F);

                    break;
                //===========================================================================================
                //===========================================================================================
                case 0x22:
                    p.Skip(1);
                    Mobile mobile = World.Mobiles.Get(p.ReadUInt());
                    if (mobile != null)
                    {
                        int damage = p.ReadByte();
                    }

                    break;
                //===========================================================================================
                //===========================================================================================
                case 0x26:
                    byte val = p.ReadByte();
                    if (val > (int) CharacterSpeedType.FastUnmountAndCantRun)
                        val = 0;

                    World.Player.SpeedMode = (CharacterSpeedType) val;

                    break;
            }
        }

        private static void DisplayClilocString(Packet p)
        {
            if (World.Player == null)
                return;

            Serial serial = p.ReadUInt();
            Entity entity = World.Get(serial);
            ushort graphic = p.ReadUShort();
            MessageType type = (MessageType) p.ReadByte();
            Hue hue = p.ReadUShort();
            MessageFont font = (MessageFont) p.ReadUShort();
            uint cliloc = p.ReadUInt();
            byte flags = p.ID == 0xCC ? p.ReadByte() : (byte) 0;
            string name = p.ReadASCII(30);

            string arguments = null;
            if (p.Position < p.Length)
                arguments = p.ReadUnicodeReversed(p.Length - p.Position);

            string text = Cliloc.Translate(Cliloc.GetString((int) cliloc), arguments);

            if (!Fonts.UnicodeFontExists((byte) font))
                font = MessageFont.Bold;

            if (entity != null)
            {
                entity.Graphic = graphic;
                entity.Name = name;
                entity.ProcessDelta();
            }

            Chat.OnMessage(entity, new UOMessageEventArgs(text, hue, type, font, true));
        }

        private static void UnicodePrompt(Packet p)
        {
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

        private static void OpenBookNew(Packet p)
        {
        }

        private static void MegaCliloc(Packet p)
        {
            p.Skip(2);
            Entity entity = World.Get(p.ReadUInt());
            if (entity == null) return;

            p.Skip(6);
            entity.UpdateProperties(ReadProperties(p));
            entity.ProcessDelta();
        }

        private static void GenericAOSCommandsR(Packet p)
        {
        }

        private static void CustomHouse(Packet p)
        {
            //Log.Message(LogTypes.Info, "CUSTOM HOUSE RECV");

            bool compressed = p.ReadByte() == 0x03;
            bool enableReponse = p.ReadBool();
            Item foundation = World.Items.Get(p.ReadUInt());
            uint revision = p.ReadUInt();

            Multi multi = foundation?.Multi;
            if (multi == null) return;

            p.Skip(4);

            House house = World.GetOrCreateHouse(foundation);
            house.Position = foundation.Position;
            house.Revision = revision;
            house.Clear();


            short minX = multi.MinX;
            short minY = multi.MinY;
            short maxY = multi.MaxY;

            byte planes = p.ReadByte();

            for (int plane = 0; plane < planes; plane++)
            {
                uint header = p.ReadUInt();
                ulong dlen = ((header & 0xFF0000) >> 16) | ((header & 0xF0) << 4);
                int clen = (int) (((header & 0xFF00) >> 8) | ((header & 0x0F) << 8));
                int planeZ = (int) ((header & 0x0F000000) >> 24);
                int planeMode = (int) ((header & 0xF0000000) >> 28);
                if (clen <= 0) continue;

                byte[] compressedBytes = new byte[clen];
                Buffer.BlockCopy(p.ToArray(), p.Position, compressedBytes, 0, clen);

                byte[] decompressedBytes = new byte[dlen];
                Zlib.Decompress(compressedBytes, 0, decompressedBytes, (int) dlen);

                Packet stream = new Packet(decompressedBytes, (int) dlen);
                // using (BinaryReader stream = new BinaryReader(new MemoryStream(decompressedBytes)))
                {
                    p.Skip(clen);

                    ushort id = 0;
                    byte x = 0, y = 0, z = 0;

                    switch (planeMode)
                    {
                        case 0:
                            for (uint i = 0; i < decompressedBytes.Length / 5; i++)
                            {
                                id = stream.ReadUShort();
                                x = stream.ReadByte();
                                y = stream.ReadByte();
                                z = stream.ReadByte();

                                x += (byte) -minX;
                                y += (byte) -minY;

                                if (id != 0)
                                {
                                    house.Items.Add(new Static(id, 0, 0)
                                    {
                                        Position = new Position((ushort) (minX + foundation.Position.X + x),
                                            (ushort) (minY + foundation.Position.Y + y),
                                            (sbyte) (foundation.Position.Z + z))
                                    });
                                }
                            }

                            break;
                        case 1:
                            if (planeZ > 0)
                                z = (byte) ((planeZ - 1) % 4 * 20 + 7);
                            else
                                z = 0;

                            for (uint i = 0; i < decompressedBytes.Length / 4; i++)
                            {
                                id = stream.ReadUShort();
                                x = stream.ReadByte();
                                y = stream.ReadByte();


                                //x += (byte)-minX;
                                //y += (byte)-minY;

                                if (id != 0)
                                {
                                    house.Items.Add(new Static(id, 0, 0)
                                    {
                                        Position = new Position((ushort) (minX + foundation.Position.X + x),
                                            (ushort) (minY + foundation.Position.Y + y),
                                            (sbyte) (foundation.Position.Z + z))
                                    });
                                }
                            }

                            break;
                        case 2:
                            short offX = 0, offY = 0;
                            short multiHeight = 0;

                            if (planeZ > 0)
                                z = (byte) ((planeZ - 1) % 4 * 20 + 7);
                            else
                                z = 0;

                            if (planeZ <= 0)
                            {
                                offX = minX;
                                offY = minY;
                                multiHeight = (short) (maxY - minY + 2);
                            }
                            else if (planeZ <= 4)
                            {
                                offX = (short) (minX + 1);
                                offY = (short) (minY + 1);
                                multiHeight = (short) (maxY - minY);
                            }
                            else
                            {
                                offX = minX;
                                offY = minY;
                                multiHeight = (short) (maxY - minY + 1);
                            }

                            if (multiHeight == 0)
                            {
                                //TODO: CHECK WHY
                                return;
                            }

                            for (uint i = 0; i < decompressedBytes.Length / 2; i++)
                            {
                                id = stream.ReadUShort();
                                x = (byte) (i / multiHeight + offX);
                                y = (byte) (i % multiHeight + offY);

                                x += (byte) -minX;
                                y += (byte) -minY;

                                if (id != 0)
                                {
                                    house.Items.Add(new Static(id, 0, 0)
                                    {
                                        Position = new Position((ushort) (minX + foundation.Position.X + x),
                                            (ushort) (minY + foundation.Position.Y + y),
                                            (sbyte) (foundation.Position.Z + z))
                                    });
                                }
                            }

                            break;
                    }
                }
            }


            house.GenerateCustom();

            World.AddOrUpdateHouse(house);
        }

        private static void CharacterTransferLog(Packet p)
        {
        }

        private static void OPLInfo(Packet p)
        {
        }

        private static void OpenCompressedGump(Packet p)
        {
            Serial sender = p.ReadUInt();
            Serial gumpID = p.ReadUInt();
            uint x = p.ReadUInt();
            uint y = p.ReadUInt();
            uint clen = p.ReadUInt() - 4;
            uint dlen = p.ReadUInt();

            byte[] data = p.ReadArray((int) clen);

            byte[] decData = new byte[dlen];

            Zlib.Decompress(data, 0, decData, (int) dlen);

            string layout = Encoding.UTF8.GetString(decData);

            uint linesNum = p.ReadUInt();
            string[] lines = new string[0];

            if (linesNum > 0)
            {
                clen = p.ReadUInt() - 4;
                dlen = p.ReadUInt();

                data = new byte[clen];
                for (int i = 0; i < data.Length; i++) data[i] = p.ReadByte();

                decData = new byte[dlen];
                Zlib.Decompress(data, 0, decData, (int) dlen);

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

            Service.Get<UIManager>().Create(sender, gumpID, (int) x, (int) y, layout, lines);
        }

        private static void UpdateMobileStatus(Packet p)
        {
        }

        private static void BuffDebuff(Packet p)
        {
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

            if ((type == 1 || type == 2) && mobile.Graphic == 0x015) mobile.AnimationRepeat = true;

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
            item.Direction = (Direction) p.ReadByte();
            item.Hue = p.ReadUShort();
            item.Flags = (Flags) p.ReadByte();

            if (FileManager.ClientVersion >= ClientVersions.CV_7090) p.ReadUShort(); //unknown

            item.Container = Serial.Invalid;

            if (type == 2) item.IsMulti = true;

            if (item.Graphic != 0x2006) item.Graphic += graphicInc;

            item.ProcessDelta();
            if (World.Items.Add(item)) World.Items.ProcessDelta();

            if (TileData.IsAnimated((long) item.ItemData.Flags))
                item.Effect = new AnimatedItemEffect(item.Serial, item.Graphic, item.Hue, -1);
        }

        private static void PacketList(Packet p)
        {
            if (World.Player == null)
                return;
        }

        private static bool ReadContainerContent(Packet p, List<Item> items)
        {
            Item item = World.GetOrCreateItem(p.ReadUInt());
            item.Graphic = (ushort) (p.ReadUShort() + p.ReadSByte());
            item.Amount = Math.Max(p.ReadUShort(), (ushort) 1);
            item.Position = new Position(p.ReadUShort(), p.ReadUShort());
            if (FileManager.ClientVersion >= ClientVersions.CV_6017) p.ReadByte(); //gridnumber - useless?

            item.Container = p.ReadUInt();
            item.Hue = p.ReadUShort();

            items.Add(item);

            Entity entity = World.Get(item.Container);
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