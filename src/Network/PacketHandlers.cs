#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
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
using System.IO;
using System.Linq;
using System.Text;

using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using ClassicUO.Utility.Platforms;

using Microsoft.Xna.Framework;

namespace ClassicUO.Network
{
    internal class PacketHandlers
    {
        private static Serial _requestedGridLoot;

        private readonly Action<Packet>[] _handlers = new Action<Packet>[0x100];

        static PacketHandlers()
        {
            ToClient = new PacketHandlers();
            NetClient.PacketReceived += ToClient.OnPacket;
        }


        public static PacketHandlers ToClient { get; }


        public void Add(byte id, Action<Packet> handler)
        {
            _handlers[id] = handler;
        }


        private void OnPacket(object sender, Packet p)
        {
            var handler = _handlers[p.ID];

            if (handler != null)
            {
                p.MoveToData();
                handler(p);
            }
        }

        private List<Serial> _clilocRequests = new List<Serial>();

        public static void Load()
        {
            ToClient.Add(0x1B, EnterWorld);
            ToClient.Add(0x55, LoginComplete);
            ToClient.Add(0xBD, ClientVersion);
            ToClient.Add(0x03, ClientTalk);
            ToClient.Add(0x0B, Damage);
            ToClient.Add(0x11, CharacterStatus);
            ToClient.Add(0x15, FollowR);
            ToClient.Add(0x16, NewHealthbarUpdate);
            ToClient.Add(0x17, NewHealthbarUpdate);
            ToClient.Add(0x1A, UpdateItem);
            ToClient.Add(0x1C, Talk);
            ToClient.Add(0x1D, DeleteObject);
            ToClient.Add(0x20, UpdatePlayer);
            ToClient.Add(0x21, DenyWalk);
            ToClient.Add(0x22, ConfirmWalk);
            ToClient.Add(0x23, DragAnimation);
            ToClient.Add(0x24, OpenContainer);
            ToClient.Add(0x25, UpdateContainedItem);
            ToClient.Add(0x27, DenyMoveItem);
            ToClient.Add(0x28, EndDraggingItem);
            ToClient.Add(0x29, DropItemAccepted);
            ToClient.Add(0x2C, DeathScreen);
            ToClient.Add(0x2D, MobileAttributes);
            ToClient.Add(0x2E, EquipItem);
            ToClient.Add(0x32, p => { }); // unknown
            ToClient.Add(0x38, Pathfinding);
            ToClient.Add(0x3A, UpdateSkills);
            ToClient.Add(0x3C, UpdateContainedItems);
            ToClient.Add(0x4E, PersonalLightLevel);
            ToClient.Add(0x4F, LightLevel);
            ToClient.Add(0x54, PlaySoundEffect);
            ToClient.Add(0x56, MapData);
            ToClient.Add(0x5B, SetTime);
            ToClient.Add(0x65, SetWeather);
            ToClient.Add(0x66, BookData);
            ToClient.Add(0x6C, TargetCursor);
            ToClient.Add(0x6D, PlayMusic);
            ToClient.Add(0x6F, SecureTrading);
            ToClient.Add(0x6E, CharacterAnimation);
            ToClient.Add(0x70, GraphicEffect);
            ToClient.Add(0x71, BulletinBoardData);
            ToClient.Add(0x72, Warmode);
            ToClient.Add(0x73, Ping);
            ToClient.Add(0x74, BuyList);
            ToClient.Add(0x77, UpdateCharacter);
            ToClient.Add(0x78, UpdateObject);
            ToClient.Add(0x7C, OpenMenu);
            ToClient.Add(0x88, OpenPaperdoll);
            ToClient.Add(0x89, CorpseEquipment);
            ToClient.Add(0x90, DisplayMap);
            ToClient.Add(0x93, OpenBook);
            ToClient.Add(0x95, DyeData);
            ToClient.Add(0x97, MovePlayer);
            ToClient.Add(0x99, MultiPlacement);
            ToClient.Add(0x9A, ASCIIPrompt);
            ToClient.Add(0x9E, SellList);
            ToClient.Add(0xA1, UpdateHitpoints);
            ToClient.Add(0xA2, UpdateMana);
            ToClient.Add(0xA3, UpdateStamina);
            ToClient.Add(0xA5, OpenUrl);
            ToClient.Add(0xA6, TipWindow);
            ToClient.Add(0xAA, AttackCharacter);
            ToClient.Add(0xAB, TextEntryDialog);
            ToClient.Add(0xAF, DisplayDeath);
            ToClient.Add(0xAE, UnicodeTalk);
            ToClient.Add(0xB0, OpenGump);
            ToClient.Add(0xB2, ChatMessage);
            ToClient.Add(0xB7, Help);
            ToClient.Add(0xB8, CharacterProfile);
            ToClient.Add(0xB9, EnableLockedFeatures);
            ToClient.Add(0xBA, DisplayQuestArrow);
            ToClient.Add(0xBB, UltimaMessengerR);
            ToClient.Add(0xBC, Season);
            ToClient.Add(0xBE, AssistVersion);
            ToClient.Add(0xBF, ExtendedCommand);
            ToClient.Add(0xC0, GraphicEffect);
            ToClient.Add(0xC1, DisplayClilocString);
            ToClient.Add(0xC2, UnicodePrompt);
            ToClient.Add(0xC4, Semivisible);
            ToClient.Add(0xC6, InvalidMapEnable);
            ToClient.Add(0xC7, GraphicEffect);
            ToClient.Add(0xC8, ClientViewRange);
            ToClient.Add(0xCA, GetUserServerPingGodClientR);
            ToClient.Add(0xCB, GlobalQueCount);
            ToClient.Add(0xCC, DisplayClilocString);
            ToClient.Add(0xD0, ConfigurationFileR);
            ToClient.Add(0xD1, Logout);
            ToClient.Add(0xD2, UpdateCharacter);
            ToClient.Add(0xD3, UpdateObject);
            ToClient.Add(0xD4, OpenBook);
            ToClient.Add(0xD6, MegaCliloc);
            ToClient.Add(0xD7, GenericAOSCommandsR);
            ToClient.Add(0xD8, CustomHouse);
            ToClient.Add(0xDB, CharacterTransferLog);
            ToClient.Add(0xDC, OPLInfo);
            ToClient.Add(0xDD, OpenCompressedGump);
            ToClient.Add(0xDE, UpdateMobileStatus);
            ToClient.Add(0xDF, BuffDebuff);
            ToClient.Add(0xE2, NewCharacterAnimation);
            ToClient.Add(0xE3, KREncryptionResponse);
            ToClient.Add(0xF0, KrriosClientSpecial);
            ToClient.Add(0xF1, FreeshardListR);
            ToClient.Add(0xF3, UpdateItemSA);
            ToClient.Add(0xF5, DisplayMap);
            ToClient.Add(0xF6, BoatMoving);
            ToClient.Add(0xF7, PacketList);
        }

        public static void SendMegaClilocRequests()
        {
            if (World.ClientFeatures.TooltipsEnabled && ToClient._clilocRequests.Count != 0)
            {
                if (FileManager.ClientVersion >= ClientVersions.CV_500A)
                {
                    while (ToClient._clilocRequests.Count != 0)
                        NetClient.Socket.Send(new PMegaClilocRequest(ref ToClient._clilocRequests));
                }
                else
                {
                    foreach (Serial serial in ToClient._clilocRequests)
                    {
                        NetClient.Socket.Send(new PMegaClilocRequestOld(serial));
                    }

                    ToClient._clilocRequests.Clear();
                }
            }
        }

        private static void AddMegaClilocRequest(Serial serial)
        {
            foreach (Serial s in ToClient._clilocRequests)
            {
                if (s == serial)
                    return;
            }

            ToClient._clilocRequests.Add(serial);
        }

        private static void TargetCursor(Packet p)
        {
            TargetManager.SetTargeting((CursorTarget) p.ReadByte(), p.ReadUInt(), (TargetType) p.ReadByte());

            if (World.Party.PartyHealTimer < Engine.Ticks && World.Party.PartyHealTarget != 0)
            {
                TargetManager.TargetGameObject(World.Get(World.Party.PartyHealTarget));
                World.Party.PartyHealTimer = 0;
                World.Party.PartyHealTarget = 0;
            }
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

                // standard client doesn't allow the trading system if one of the traders is invisible (=not sent by server)
                if (World.Get(id1) == null || World.Get(id2) == null)
                    return;

                bool hasName = p.ReadBool();
                string name = string.Empty;

                if (hasName && p.Position < p.Length)
                    name = p.ReadASCII();

                Engine.UI.Add(new TradingGump(serial, name, id1, id2));
            }
            else if (type == 1)
                Engine.UI.Gumps.OfType<TradingGump>().FirstOrDefault(s => s.ID1 == serial || s.ID2 == serial)?.Dispose();
            else if (type == 2)
            {
                Serial id1 = p.ReadUInt();
                Serial id2 = p.ReadUInt();

                TradingGump trading = Engine.UI.Gumps.OfType<TradingGump>().FirstOrDefault(s => s.ID1 == serial || s.ID2 == serial);

                if (trading != null)
                {
                    trading.ImAccepting = id1 != 0;
                    trading.HeIsAccepting = id2 != 0;

                    trading.UpdateContent();
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

            Entity entity = World.Get(p.ReadUInt());

            if (entity != null)
            {
                ushort damage = p.ReadUShort();

                World.WorldTextManager
                      .AddDamage(entity,
                                 damage
                                );
            }
        }

        private static void CharacterStatus(Packet p)
        {
            if (World.Player == null)
                return;

            Mobile mobile = World.Mobiles.Get(p.ReadUInt());

            if (mobile == null)
                return;

            mobile.Name = p.ReadASCII(30);
            mobile.Hits = p.ReadUShort();
            mobile.HitsMax = p.ReadUShort();
            mobile.IsRenamable = p.ReadBool();
            byte type = p.ReadByte();

            if (type > 0 && p.Position + 1 <= p.Length)
            {
                mobile.IsMale = !p.ReadBool();

                if (mobile == World.Player)
                {
                    ushort str = p.ReadUShort();
                    ushort dex = p.ReadUShort();
                    ushort intell = p.ReadUShort();
                    World.Player.Stamina = p.ReadUShort();
                    World.Player.StaminaMax = p.ReadUShort();
                    World.Player.Mana = p.ReadUShort();
                    World.Player.ManaMax = p.ReadUShort();
                    World.Player.Gold = p.ReadUInt();
                    World.Player.PhysicalResistence = p.ReadUShort();
                    World.Player.Weight = p.ReadUShort();


                    if (World.Player.Strength != 0)
                    {
                        ushort currentStr = World.Player.Strength;
                        ushort currentDex = World.Player.Dexterity;
                        ushort currentInt = World.Player.Intelligence;

                        int deltaStr = str - currentStr;
                        int deltaDex = dex - currentDex;
                        int deltaInt = intell - currentInt;

                        if (deltaStr != 0)
                            GameActions.Print($"Your strength has changed by {deltaStr}.  It is now {str}", 0x0170, MessageType.System, 3, false);

                        if (deltaDex != 0)
                            GameActions.Print($"Your dexterity has changed by {deltaDex}.  It is now {dex}", 0x0170, MessageType.System, 3, false);

                        if (deltaInt != 0)
                            GameActions.Print($"Your intelligence has changed by {deltaInt}.  It is now {intell}", 0x0170, MessageType.System, 3, false);
                    }

                    World.Player.Strength = str;
                    World.Player.Dexterity = dex;
                    World.Player.Intelligence = intell;

                    if (type >= 5) //ML
                    {
                        World.Player.WeightMax = p.ReadUShort();
                        byte race = p.ReadByte();

                        if (race == 0)
                            race = 1;
                        World.Player.Race = (RaceType) race;
                    }
                    else
                    {
                        if (FileManager.ClientVersion >= ClientVersions.CV_500A)
                            World.Player.WeightMax = (ushort) (7 * (World.Player.Strength >> 1) + 40);
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
                        World.Player.FireResistance = p.ReadUShort();
                        World.Player.ColdResistance = p.ReadUShort();
                        World.Player.PoisonResistance = p.ReadUShort();
                        World.Player.EnergyResistance = p.ReadUShort();
                        World.Player.Luck = p.ReadUShort();
                        World.Player.DamageMin = p.ReadUShort();
                        World.Player.DamageMax = p.ReadUShort();
                        World.Player.TithingPoints = p.ReadUInt();
                    }

                    if (type >= 6)
                    {
                        World.Player.MaxPhysicResistence = p.Position + 2 > p.Length ? (ushort) 0 : p.ReadUShort();
                        World.Player.MaxFireResistence = p.Position + 2 > p.Length ? (ushort) 0 : p.ReadUShort();
                        World.Player.MaxColdResistence = p.Position + 2 > p.Length ? (ushort) 0 : p.ReadUShort();
                        World.Player.MaxPoisonResistence = p.Position + 2 > p.Length ? (ushort) 0 : p.ReadUShort();
                        World.Player.MaxEnergyResistence = p.Position + 2 > p.Length ? (ushort) 0 : p.ReadUShort();
                        World.Player.DefenseChanceIncrease = p.Position + 2 > p.Length ? (ushort) 0 : p.ReadUShort();
                        World.Player.MaxDefenseChanceIncrease = p.Position + 2 > p.Length ? (ushort) 0 : p.ReadUShort();
                        World.Player.HitChanceIncrease = p.Position + 2 > p.Length ? (ushort) 0 : p.ReadUShort();
                        World.Player.SwingSpeedIncrease = p.Position + 2 > p.Length ? (ushort) 0 : p.ReadUShort();
                        World.Player.DamageIncrease = p.Position + 2 > p.Length ? (ushort) 0 : p.ReadUShort();
                        World.Player.LowerReagentCost = p.Position + 2 > p.Length ? (ushort) 0 : p.ReadUShort();
                        World.Player.SpellDamageIncrease = p.Position + 2 > p.Length ? (ushort) 0 : p.ReadUShort();
                        World.Player.FasterCastRecovery = p.Position + 2 > p.Length ? (ushort) 0 : p.ReadUShort();
                        World.Player.FasterCasting = p.Position + 2 > p.Length ? (ushort) 0 : p.ReadUShort();
                        World.Player.LowerManaCost = p.Position + 2 > p.Length ? (ushort) 0 : p.ReadUShort();
                    }
                }
            }

            mobile.ProcessDelta();


            if (mobile == World.Player)
            {
                UoAssist.SignalHits();
                UoAssist.SignalStamina();
                UoAssist.SignalMana();
            }
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
                            flags = (byte) (flags & ~0x04);
                    }
                }
                else if (type == 2)
                {
                    if (enabled)
                        flags |= 0x08;
                    else
                        flags &= (byte) (flags & ~0x08);
                }
                else if (type == 3)
                {
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
            item.FixHue(hue);
            item.Flags = (Flags) flags;
            item.Direction = (Direction) direction;

            if (graphic >= 0x4000)
            {
                item.Graphic -= 0x4000;

                //if (item.IsMulti)
                //    item.IsMulti = false;

                //if (item.IsMulti)
                //{
                //    if (World.HouseManager.TryGetHouse(item, out var house))
                //    {
                //        house.Generate(true);
                //    }
                //}
                item.WantUpdateMulti = true;
                item.IsMulti = true;
            }

            item.LightID = direction;
            item.Container = Serial.INVALID;
            item.CheckGraphicChange();
            item.ProcessDelta();


            if (World.Items.Add(item)) World.Items.ProcessDelta();

            if (item.OnGround)
                item.AddToTile();


            if (graphic == 0x2006 && !item.IsClicked && Engine.Profile.Current.ShowNewCorpseNameIncoming) GameActions.SingleClick(item);

            if (graphic == 0x2006 && Engine.Profile.Current.AutoOpenCorpses) World.Player.TryOpenCorpses();
        }

        private static void EnterWorld(Packet p)
        {
            Engine.Profile.Load(World.ServerName, LoginScene.Account, Engine.GlobalSettings.LastCharacterName);

            World.Mobiles.Add(World.Player = new PlayerMobile(p.ReadUInt()));
            p.Skip(4);
            World.Player.Graphic = p.ReadUShort();
            ushort x = p.ReadUShort();
            ushort y = p.ReadUShort();
            sbyte z = (sbyte) p.ReadUShort();

            if (World.Map == null)
                World.MapIndex = 0;

            Direction direction = (Direction) (p.ReadByte() & 0x7);
            //World.Player.ForcePosition(x, y, z, direction);

            World.Player.Position = new Position(x, y, z);
            World.Player.Direction = direction;
            World.Player.AddToTile();

            World.RangeSize.X = x;
            World.RangeSize.Y = y;

            if (Engine.Profile.Current.UseCustomLightLevel)
                World.Light.Overall = Engine.Profile.Current.LightLevel;

            if (FileManager.ClientVersion >= ClientVersions.CV_200)
            {
                NetClient.Socket.Send(new PGameWindowSize((uint) Engine.Profile.Current.GameWindowSize.X, (uint) Engine.Profile.Current.GameWindowSize.Y));
                NetClient.Socket.Send(new PLanguage("ENU"));
            }

            NetClient.Socket.Send(new PClientVersion(Engine.GlobalSettings.ClientVersion));

            GameActions.SingleClick(World.Player);
            NetClient.Socket.Send(new PStatusRequest(World.Player));
            World.Player.ProcessDelta();
            World.Mobiles.ProcessDelta();

            if (World.Player.IsDead)
                World.ChangeSeason(Seasons.Desolation, 42);
        }

        private static void Talk(Packet p)
        {
            Serial serial = p.ReadUInt();
            Entity entity = World.Get(serial);
            ushort graphic = p.ReadUShort();
            MessageType type = (MessageType) p.ReadByte();
            Hue hue = p.ReadUShort();
            ushort font = p.ReadUShort();
            string name = p.ReadASCII(30);
            string text = p.ReadASCII();

            if (serial == 0 && graphic == 0 && type == MessageType.Regular && font == 0xFFFF && hue == 0xFFFF && name.StartsWith("SYSTEM"))
            {
                NetClient.Socket.Send(new PACKTalk());

                return;
            }

            if (entity != null)
            {
                if (string.IsNullOrEmpty(entity.Name))
                    entity.Name = name;
                entity.ProcessDelta();
            }

            Chat.HandleMessage(entity, text, name, hue, type, (byte) font);
        }

        private static void DeleteObject(Packet p)
        {
            if (World.Player == null)
                return;

            Serial serial = p.ReadUInt();

            if (World.Player == serial)
                return;

            Entity entity = World.Get(serial);

            if (entity == null)
                return;

            bool updateAbilities = false;

            if (serial.IsItem)
            {
                Item it = (Item)entity;
                uint cont = it.Container & 0x7FFFFFFF;

                if (it.Container.IsValid)
                {
                    Entity top = it.Items.FirstOrDefault();

                    if (top != null)
                    {
                        if (top == World.Player) updateAbilities = it.Layer == Layer.OneHanded || it.Layer == Layer.TwoHanded;

                        var tradeBox = top.Items.FirstOrDefault(s => s.Graphic == 0x1E5E && s.Layer == Layer.Invalid);

                        if (tradeBox != null)
                            Engine.UI.Gumps.OfType<TradingGump>().FirstOrDefault(s => s.ID1 == tradeBox || s.ID2 == tradeBox)?.UpdateContent();
                    }

                    GameScene scene = Engine.SceneManager.GetScene<GameScene>();

                    if (cont == World.Player && it.Layer == Layer.Invalid)
                        scene.HeldItem.Enabled = false;


                    if (it.Layer != Layer.Invalid)
                        Engine.UI.GetGump<PaperDollGump>(cont)?.Update();
                }
            }

            if (World.CorpseManager.Exists(0, serial))
                return;

            if (serial.IsMobile)
            {
                Mobile m = (Mobile)entity;

                if (World.Party.Contains(serial))
                {
                    // m.RemoveFromTile();
                }
                // else
                {
                    World.RemoveMobile(serial, true);
                    m.Items.ProcessDelta();
                    World.Items.ProcessDelta();
                    World.Mobiles.ProcessDelta();
                }
            }
            else if (serial.IsItem)
            {
                Item it = (Item)entity;

                if (it.IsMulti)
                    World.HouseManager.Remove(it);

                Entity cont = World.Get(it.Container);

                if (cont != null)
                {
                    cont.Items.Remove(it);
                    cont.Items.ProcessDelta();

                    if (it.Layer != Layer.Invalid)
                    {
                        Engine.UI.GetGump<PaperDollGump>(cont)?.Update();
                    }

                    if (Engine.Profile.Current.GridLootType > 0)
                    {
                        GridLootGump grid = Engine.UI.GetGump<GridLootGump>(it.Container);

                        if (grid != null) grid.RedrawItems();
                    }
                }

                World.RemoveItem(it, true);
                World.Items.ProcessDelta();

                if (updateAbilities)
                    World.Player.UpdateAbilities();
            }
        }

        private static void UpdatePlayer(Packet p)
        {
            if (World.Player == null || p.ReadUInt() != World.Player)
                return;

            bool oldDead = World.Player.IsDead;
            ushort oldGraphic = World.Player.Graphic;

            World.Player.Graphic = (ushort) (p.ReadUShort() + p.ReadSByte());
            World.Player.FixHue(p.ReadUShort());
            World.Player.Flags = (Flags) p.ReadByte();
            ushort x = p.ReadUShort();
            ushort y = p.ReadUShort();
            p.Skip(2);
            Direction direction = (Direction) p.ReadByte();
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
            World.RangeSize.X = x;
            World.RangeSize.Y = y;
            World.Player.Direction = dir;
            World.Player.Walker.DenyWalk(0xFF, -1, -1, -1);

            if (oldGraphic != 0 && oldGraphic != World.Player.Graphic)
            {
                if (World.Player.IsDead)
                {
                    TargetManager.Reset();
                }
            }

            if (oldDead != World.Player.IsDead)
            {
                if (World.Player.IsDead)
                    World.ChangeSeason(Seasons.Desolation, 42);
                else 
                    World.ChangeSeason(World.OldSeason, World.OldMusicIndex);
            }

            World.Player.Walker.ResendPacketSended = false;
            World.Player.AddToTile();
#endif
            World.Player.ProcessDelta();

            var scene = Engine.SceneManager.GetScene<GameScene>();

            if (scene != null)
                scene.UpdateDrawPosition = true;


            World.Player.CloseRangedGumps();
        }

        private static void DenyWalk(Packet p)
        {
            if (World.Player == null)
                return;

            byte seq = p.ReadByte();
            ushort x = p.ReadUShort();
            ushort y = p.ReadUShort();
            Direction direction = (Direction) p.ReadByte();
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
            byte noto = (byte) (p.ReadByte() & ~0x40);

            if (noto == 0 || noto >= 8)
                noto = 0x01;

            World.Player.NotorietyFlag = (NotorietyFlag) noto;
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

            Mobile entity = World.Mobiles.Get(source);

            if (entity == null)
                source = 0;
            else
            {
                sourceX = entity.Position.X;
                sourceY = entity.Position.Y;
                sourceZ = entity.Position.Z;
            }

            Mobile destEntity = World.Mobiles.Get(dest);

            if (destEntity == null)
                dest = 0;
            else
            {
                destX = destEntity.Position.X;
                destY = destEntity.Position.Y;
                destZ = destEntity.Position.Z;
            }

            GameEffect effect;


            if (!source.IsValid || !dest.IsValid)
            {
                effect = new MovingEffect(source, dest, sourceX, sourceY, sourceZ,
                                          destX, destY, destZ, graphic, hue, true)
                {
                    Duration = Engine.Ticks + 5000,
                    MovingDelay = 5
                };
            }
            else
            {
                effect = new DragEffect(source, dest, sourceX, sourceY, sourceZ,
                                        destX, destY, destZ, graphic, hue)
                {
                    Duration = Engine.Ticks + 5000
                };
            }

            if (effect.AnimDataFrame.FrameCount != 0)
            {
                effect.Speed = effect.AnimDataFrame.FrameInterval * 45;
            }
            else
            {
                effect.Speed = 13;
            }

            World.AddEffect(effect);
        }

        private static void OpenContainer(Packet p)
        {
            if (World.Player == null)
                return;

            Serial serial = p.ReadUInt();
            Graphic graphic = p.ReadUShort();


            if (graphic == 0xFFFF)
            {
                Item spellBookItem = World.Items.Get(serial);
                if (spellBookItem == null)
                    return;

                Engine.UI.GetGump<SpellbookGump>(serial)?.Dispose();
                SpellbookGump spellbookGump = new SpellbookGump(spellBookItem);
                if (!Engine.UI.GetGumpCachePosition(spellBookItem, out Point location)) location = new Point(64, 64);

                spellbookGump.Location = location;
                Engine.UI.Add(spellbookGump);

                Engine.SceneManager.CurrentScene.Audio.PlaySound(0x0055);
            }
            else if (graphic == 0x30)
            {
                Mobile vendor = World.Mobiles.Get(serial);

                if (vendor == null)
                    return;

                Engine.UI.GetGump<ShopGump>(serial)?.Dispose();
                ShopGump gump = new ShopGump(serial, true, 150, 5);
                Engine.UI.Add(gump);

                for (Layer layer = Layer.ShopBuyRestock; layer < Layer.ShopBuy + 1; layer++)
                {
                    Item item = vendor.Equipment[(int)layer];

                    //Item a = item?.Items.FirstOrDefault();

                    //if (a == null)
                    //    continue;

                    //bool reverse = a.X > 1;

                    //var list = reverse ? 
                    //               item.Items.OrderBy(s => s.Serial.Value).Reverse() 
                    //               :
                    //               item.Items.OrderBy(s => s.Serial.Value);

                    var list = item.Items /*.OrderByDescending(s => s.Serial.Value)*/.ToArray();

                    if (list.Length == 0)
                        return;

                    if (list[0].X > 1)
                        list = list.Reverse().ToArray();

                    foreach (var i in list) gump.AddItem(i, false);
                }
            }
            else
            {
                Item item = World.Items.Get(serial);

                if (item != null)
                {
                    if (item.IsCorpse && (Engine.Profile.Current.GridLootType == 1 || Engine.Profile.Current.GridLootType == 2))
                    {
                        Engine.UI.GetGump<GridLootGump>(serial)?.Dispose();
                        Engine.UI.Add(new GridLootGump(serial));
                        _requestedGridLoot = serial;

                        if (Engine.Profile.Current.GridLootType == 1)
                            return;
                    }

                    Engine.UI.GetGump<ContainerGump>(serial)?.Dispose();
                    Engine.UI.Add(new ContainerGump(item, graphic));
                }
                else 
                    Log.Message(LogTypes.Error, "[OpenContainer]: item not found");
            }

        }

        private static void UpdateContainedItem(Packet p)
        {
            if (!World.InGame)
                return;

            Serial serial = p.ReadUInt();
            Graphic graphic = (Graphic) (p.ReadUShort() + p.ReadByte());
            ushort amount = Math.Max((ushort) 1, p.ReadUShort());
            ushort x = p.ReadUShort();
            ushort y = p.ReadUShort();

            if (FileManager.ClientVersion >= ClientVersions.CV_6017)
                p.Skip(1);

            Serial containerSerial = p.ReadUInt();
            Hue hue = p.ReadUShort();

            AddItemToContainer(serial, graphic, amount, x, y, hue, containerSerial);

            World.Get(containerSerial)?.Items.ProcessDelta();
            World.Items.ProcessDelta();


            if (containerSerial.IsMobile)
            {
                Mobile m = World.Mobiles.Get(containerSerial);
                Item secureBox = m?.GetSecureTradeBox();
                if (secureBox != null)
                {
                    var gump = Engine.UI.Gumps.OfType<TradingGump>().SingleOrDefault(s => s.LocalSerial == secureBox || s.ID1 == secureBox || s.ID2 == secureBox);

                    if (gump != null) gump.UpdateContent();
                }
            }
            else if (containerSerial.IsItem)
            {
                var gump = Engine.UI.Gumps.OfType<TradingGump>().SingleOrDefault(s => s.LocalSerial == containerSerial || s.ID1 == containerSerial || s.ID2 == containerSerial);

                if (gump != null) gump.UpdateContent();
            }
        }

        private static void DenyMoveItem(Packet p)
        {
            if (!World.InGame)
                return;

            GameScene scene = Engine.SceneManager.GetScene<GameScene>();

            ItemHold hold = scene.HeldItem;

            Item item = World.Items.Get(hold.Serial);

            if (hold.Enabled || hold.Dropped && item == null)
            {
                if (hold.Layer == Layer.Invalid && hold.Container.IsValid)
                {
                    Entity container = World.Get(hold.Container);

                    if (container != null)
                    {
                        item = World.GetOrCreateItem(hold.Serial);
                        item.Graphic = hold.Graphic;
                        item.FixHue(hold.Hue);
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
                        item.FixHue(hold.Hue);
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

                                    mob.Items.ProcessDelta();
                                    mob.ProcessDelta();
                                }
                                else
                                    Log.Message(LogTypes.Warning, "SOMETHING WRONG WITH CONTAINER (should be a mobile)");
                            }
                            else
                                Log.Message(LogTypes.Warning, "SOMETHING WRONG WITH CONTAINER (is null)");
                        }
                        else
                            item.AddToTile();

                        World.Items.Add(item);
                        item.ProcessDelta();
                        World.Items.ProcessDelta();
                    }
                }

                hold.Clear();
            }
            else
                Log.Message(LogTypes.Warning, "There was a problem with ItemHold object. It was cleared before :|");

            byte code = p.ReadByte();

            if (code < 5) Chat.HandleMessage(null, ServerErrorMessages.GetError(p.ID, code), string.Empty, 1001, MessageType.System, 3);
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

        private static void DeathScreen(Packet p)
        {
            // todo
            byte action = p.ReadByte();

            if (action != 1)
            {
                Engine.SceneManager.GetScene<GameScene>()?.Weather?.Reset();
                Engine.SceneManager.CurrentScene.Audio.PlayMusic(42);

                if (Engine.Profile.Current.EnableDeathScreen)
                    World.Player.DeathScreenTimer = Engine.Ticks + Constants.DEATH_SCREEN_TIMER;

                GameActions.ChangeWarMode(0);
            }
        }

        private static void MobileAttributes(Packet p)
        {
            Mobile mobile = World.Mobiles.Get(p.ReadUInt());

            if (mobile == null)
                return;

            mobile.HitsMax = p.ReadUShort();
            mobile.Hits = p.ReadUShort();
            mobile.ManaMax = p.ReadUShort();
            mobile.Mana = p.ReadUShort();
            mobile.StaminaMax = p.ReadUShort();
            mobile.Stamina = p.ReadUShort();
            mobile.ProcessDelta();

            if (mobile == World.Player)
            {
                UoAssist.SignalHits();
                UoAssist.SignalStamina();
                UoAssist.SignalMana();
            }
        }

        private static void EquipItem(Packet p)
        {
            if (!World.InGame)
                return;

            Serial serial = p.ReadUInt();

            Item item = World.GetOrCreateItem(serial);


            if (item.Graphic != 0 && item.Layer != Layer.Backpack) item.Items.Clear();

            if (item.Container != 0)
            {
                Entity cont = World.Get(item.Container);

                if (cont != null)
                {
                    cont.Items.Remove(item);
                    cont.Items.ProcessDelta();
                    World.Items.Remove(item);
                    World.Items.ProcessDelta();

                    if (cont.HasEquipment && item.Layer != Layer.Invalid)
                    {
                        cont.Equipment[(int) item.Layer] = null;
                    }
                }

                item.Container = Serial.INVALID;
            }

            item.RemoveFromTile();

            //if (item.Graphic != 0)
            //    World.RemoveItem(item);


            item.Graphic = (ushort) (p.ReadUShort() + p.ReadSByte());
            item.Layer = (Layer) p.ReadByte();
            item.Container = p.ReadUInt();
            item.FixHue(p.ReadUShort());
            item.Amount = 1;
            Mobile mobile = World.Mobiles.Get(item.Container);

            World.Items.Add(item);
            World.Items.ProcessDelta();

            if (mobile != null)
            {
                mobile.Equipment[(int) item.Layer] = item;
                mobile.Items.Add(item);
                mobile.Items.ProcessDelta();
            }

            if (item.Layer >= Layer.ShopBuyRestock && item.Layer <= Layer.ShopSell) item.Items.Clear();


            if (mobile == World.Player && (item.Layer == Layer.OneHanded || item.Layer == Layer.TwoHanded))
                World.Player.UpdateAbilities();

            GameScene gs = Engine.SceneManager.GetScene<GameScene>();

            if (gs.HeldItem.Serial == item.Serial)
                gs.HeldItem.Clear();

          
        }

        private static void UpdateSkills(Packet p)
        {
            if (!World.InGame)
                return;

            if (World.SkillsRequested)
            {
                World.SkillsRequested = false;

                // TODO: make a base class for this gump
                if (Engine.Profile.Current.StandardSkillsGump)
                {
                    var gumpSkills = Engine.UI.GetGump<StandardSkillsGump>();

                    if (gumpSkills == null)
                    {
                        Engine.UI.Add(new StandardSkillsGump
                        {
                            X = 100,
                            Y = 100
                        });
                    }
                }
                else
                {
                    var gumpSkills = Engine.UI.GetGump<SkillGumpAdvanced>();

                    if (gumpSkills == null)
                    {
                        Engine.UI.Add(new SkillGumpAdvanced
                        {
                            X = 100,
                            Y = 100
                        });
                    }
                }
            }

            ushort id;

            switch (p.ReadByte())
            {
                case 0:

                    while (p.Position + 2 <= p.Length && (id = p.ReadUShort()) > 0)
                        World.Player.UpdateSkill(id - 1, p.ReadUShort(), p.ReadUShort(), (Lock) p.ReadByte(), 100);

                    break;

                case 2:

                    while (p.Position + 2 <= p.Length && (id = p.ReadUShort()) > 0)
                        World.Player.UpdateSkill(id - 1, p.ReadUShort(), p.ReadUShort(), (Lock) p.ReadByte(), p.ReadUShort());

                    break;

                case 0xDF:
                    id = p.ReadUShort();
                    World.Player.UpdateSkill(id, p.ReadUShort(), p.ReadUShort(), (Lock) p.ReadByte(), p.ReadUShort(), true);

                    break;

                case 0xFF:
                    id = p.ReadUShort();
                    World.Player.UpdateSkill(id, p.ReadUShort(), p.ReadUShort(), (Lock) p.ReadByte(), 100);

                    break;
            }

            World.Player.ProcessDelta();
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

        private static void UpdateContainedItems(Packet p)
        {
            if (!World.InGame)
                return;

            ushort count = p.ReadUShort();

            Entity container = null;

            GridLootGump grid = null;

            for (int i = 0; i < count; i++)
            {
                Serial serial = p.ReadUInt();
                Graphic graphic = (Graphic) (p.ReadUShort() + p.ReadByte());
                ushort amount = Math.Max(p.ReadUShort(), (ushort) 1);
                ushort x = p.ReadUShort();
                ushort y = p.ReadUShort();

                if (FileManager.ClientVersion >= ClientVersions.CV_6017)
                    p.Skip(1);
                Serial containerSerial = p.ReadUInt();
                Hue hue = p.ReadUShort();

                if (i == 0)
                {
                    container = World.Get(containerSerial);

                    if (container != null)
                    {
                        if (container.Graphic == 0x2006)
                        {
                            container.Items
                                     .Where(s => s.Layer == Layer.Invalid)
                                     .ToList()
                                     .ForEach(s =>
                                      {
                                          s.Container = Serial.INVALID;
                                          container.Items.Remove(s);
                                          World.Items.Remove(s);
                                      });
                        }
                        else
                        {
                            container.Items
                                     .ToList()
                                     .ForEach(s =>
                                      {
                                          s.Container = Serial.INVALID;
                                          container.Items.Remove(s);
                                          World.Items.Remove(s);
                                      });
                        }

                        container.ProcessDelta();
                        World.Items.ProcessDelta();
                    }
                }


                AddItemToContainer(serial, graphic, amount, x, y, hue, containerSerial);

                if (grid == null && Engine.Profile.Current.GridLootType > 0)
                {
                    grid = Engine.UI.GetGump<GridLootGump>(containerSerial);

                    if (_requestedGridLoot != 0 && _requestedGridLoot == containerSerial && grid == null)
                    {
                        grid = new GridLootGump(_requestedGridLoot);
                        Engine.UI.Add(grid);
                        _requestedGridLoot = 0;
                    }
                }

                if (grid != null) grid.RedrawItems();
            }

            container?.Items.ProcessDelta();

            if (container is Item itemContainer && itemContainer.IsSpellBook && SpellbookData.GetTypeByGraphic(itemContainer.Graphic) != SpellBookType.Unknown)
            {
                SpellbookData.GetData(itemContainer, out ulong field, out SpellBookType type);

                if (itemContainer.FillSpellbook(type, field)) Engine.UI.GetGump<SpellbookGump>(itemContainer)?.Update();
            }


            World.Items.ProcessDelta();
        }

        private static void PersonalLightLevel(Packet p)
        {
            if (!World.InGame)
                return;

            if (World.Player == p.ReadUInt())
            {
                byte level = p.ReadByte();

                if (level > 0x1E)
                    level = 0x1E;

                World.Light.RealPersonal = level;

                if (!Engine.Profile.Current.UseCustomLightLevel)
                    World.Light.Personal = level;
            }
        }

        private static void LightLevel(Packet p)
        {
            if (!World.InGame)
                return;

            byte level = p.ReadByte();

            if (level > 0x1E)
                level = 0x1E;

            World.Light.RealOverall = level;

            if (!Engine.Profile.Current.UseCustomLightLevel)
                World.Light.Overall = level;
        }

        private static void PlaySoundEffect(Packet p)
        {
            if (World.Player == null)
                return;

            p.Skip(1);

            ushort index = p.ReadUShort();
            ushort audio = p.ReadUShort();
            ushort x = p.ReadUShort();
            ushort y = p.ReadUShort();
            ushort z = p.ReadUShort();

            int distX = Math.Abs(x - World.Player.X);
            int distY = Math.Abs(y - World.Player.Y);
            int distance = Math.Max(distX, distY);

            float volume = Engine.Profile.Current.SoundVolume / Constants.SOUND_DELTA;

            if (distance <= World.ClientViewRange && distance >= 1)
            {
                float volumeByDist = volume / World.ClientViewRange;
                volume -= volumeByDist * distance;
            }

            Engine.SceneManager.CurrentScene.Audio.PlaySoundWithDistance(index, volume, true);
        }

        private static void PlayMusic(Packet p)
        {
            ushort index = p.ReadUShort();

            Engine.SceneManager.CurrentScene.Audio.PlayMusic(index);
        }

        private static void LoginComplete(Packet p)
        {
            if (World.Player != null && Engine.SceneManager.CurrentScene is LoginScene)
            {
                Engine.SceneManager.ChangeScene(ScenesType.Game);

                NetClient.Socket.Send(new PSkillsRequest(World.Player));

                if (FileManager.ClientVersion >= ClientVersions.CV_306E)
                    NetClient.Socket.Send(new PClientType());

                if (FileManager.ClientVersion >= ClientVersions.CV_305D)
                    NetClient.Socket.Send(new PClientViewRange(World.ClientViewRange));

                Engine.FpsLimit = Engine.Profile.Current.MaxFPS;

                Engine.Profile.Current.ReadGumps()?.ForEach(Engine.UI.Add);
            }
        }

        private static void MapData(Packet p)
        {
            if (!World.InGame)
                return;

            Serial serial = p.ReadUInt();

            MapGump gump = Engine.UI.GetGump<MapGump>(serial);

            if (gump != null)
            {
                switch ((MapMessageType) p.ReadByte())
                {
                    case MapMessageType.Add:
                        p.Skip(1);

                        ushort x = p.ReadUShort();
                        ushort y = p.ReadUShort();

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
            var scene = Engine.SceneManager.GetScene<GameScene>();
            if (scene == null)
                return;

            var weather = scene.Weather;

            weather.Reset();

            byte type = p.ReadByte();
            weather.Type = type;
            weather.Count = p.ReadByte();

            bool showMessage = (weather.Count > 0);

            if (weather.Count > 70)
                weather.Count = 70;

            weather.Temperature = p.ReadByte();
            weather.Timer = Engine.Ticks + Constants.WEATHER_TIMER;
            weather.Generate();

            switch (type)
            {
                case 0:
                    if (showMessage)
                        GameActions.Print("It begins to rain.", 0, MessageType.System, 3, false );
                    break;
                case 1:
                    if (showMessage)
                        GameActions.Print("A fierce storm approaches.", 0, MessageType.System, 3, false);
                    break;
                case 2:
                    if (showMessage)
                        GameActions.Print("It begins to snow.", 0, MessageType.System, 3, false);
                    break;
                case 3:
                    if (showMessage)
                        GameActions.Print("A storm is brewing.", 0, MessageType.System, 3, false);
                    break;
                case 0xFE:
                case 0xFF:
                    weather.Timer = 0;
                    break;
            }
        }

        private static void BookData(Packet p)
        {
            if (!World.InGame)
                return;

            UIManager ui = Engine.UI;
            var serial = p.ReadUInt();
            var pageCnt = p.ReadUShort();
            var pages = new string[pageCnt];
            var gump = ui.GetGump<BookGump>(serial);

            if (gump == null) return;

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < pageCnt; i++) pages[i] = string.Empty;

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
                        sb.Remove(sb.Length - 1, 1); //this removes the last, unwanted, newline
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
            mobile.SetAnimation(Mobile.GetReplacedObjectAnimation(mobile.Graphic, action), delay, (byte) frameCount, (byte) repeatMode, repeat, frameDirection);
            mobile.AnimationFromServer = true;
        }

        private static void GraphicEffect(Packet p)
        {
            if (World.Player == null)
                return;

            GraphicEffectType type = (GraphicEffectType) p.ReadByte();

            if (type > GraphicEffectType.FixedFrom)
            {
                if (type == GraphicEffectType.ScreenFade && p.ID == 0x70)
                {
                    p.Skip(8);
                    ushort val = p.ReadUShort();

                    if (val > 4)
                    {
                        val = 4;
                    }

                    Log.Message(LogTypes.Warning, "Effect not implemented");
                }

                return;
            }

            Serial source = p.ReadUInt();
            Serial target = p.ReadUInt();
            Graphic graphic = p.ReadUShort();
            Position srcPos = new Position(p.ReadUShort(), p.ReadUShort(), p.ReadSByte());
            Position targPos = new Position(p.ReadUShort(), p.ReadUShort(), p.ReadSByte());
            byte speed = p.ReadByte();
            ushort duration = (ushort) (p.ReadByte() * 50);
            p.Skip(2);
            bool fixedDirection = p.ReadBool();
            bool doesExplode = p.ReadBool();
            Hue hue = 0;
            GraphicEffectBlendMode blendmode = 0;

            if (p.ID != 0x70)
            {
                hue = (ushort) p.ReadUInt();
                blendmode = (GraphicEffectBlendMode) (p.ReadUInt() % 7);
            }

            World.AddEffect(type, source, target, graphic, hue, srcPos, targPos, speed, duration, fixedDirection, doesExplode, false, blendmode);
        }

        private static void ClientViewRange(Packet p)
        {
            World.ClientViewRange = p.ReadByte();
        }

        private static void BulletinBoardData(Packet p)
        {
            if (!World.InGame)
                return;

            switch (p.ReadByte())
            {
                case 0: // open

                {
                    Serial serial = p.ReadUInt();
                    Item item = World.Items.Get(serial);

                    if (item != null)
                    {
                        BulletinBoardGump bulletinBoard = Engine.UI.GetGump<BulletinBoardGump>(serial);
                        bulletinBoard?.Dispose();

                        int x = (Engine.WindowWidth >> 1) - 245;
                        int y = (Engine.WindowHeight >> 1) - 205;

                        bulletinBoard = new BulletinBoardGump(item, x, y, p.ReadASCII(22));
                        Engine.UI.Add(bulletinBoard);
                    }
                }

                    break;

                case 1: // summary msg

                {
                    Serial boardSerial = p.ReadUInt();
                    BulletinBoardGump bulletinBoard = Engine.UI.GetGump<BulletinBoardGump>(boardSerial);

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
                    BulletinBoardGump bulletinBoard = Engine.UI.GetGump<BulletinBoardGump>(boardSerial);

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

                        if (unk > 0) p.Skip(unk * 4);

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
            if (!World.InGame)
                return;

            World.Player.InWarMode = p.ReadBool();
            World.Player.ProcessDelta();
        }

        private static void Ping(Packet p)
        {
            if (NetClient.Socket.IsConnected && !NetClient.Socket.IsDisposed)
                NetClient.Socket.Statistics.PingReceived();
        }


        private static void BuyList(Packet p)
        {
            if (!World.InGame)
                return;

            Item container = World.Items.Get(p.ReadUInt());

            if (container == null) return;

            Mobile vendor = World.Mobiles.Get(container.Container);

            if (vendor == null) return;


            ShopGump gump = Engine.UI.GetGump<ShopGump>();

            if (gump != null && (gump.LocalSerial != vendor || !gump.IsBuyGump))
            {
                gump.Dispose();
                gump = null;
            }

            if (gump == null)
            {
                gump = new ShopGump(vendor, true, 150, 5);
                Engine.UI.Add(gump);
            }

            if (container.Layer == Layer.ShopBuyRestock || container.Layer == Layer.ShopBuy)
            {
                byte count = p.ReadByte();

                //Item a = container.Items.FirstOrDefault();

                //if (a == null)
                //    return;

                //bool reverse = a.X > 1;

                //var list = reverse ? 
                //               container.Items.OrderBy(s => s.Serial.Value).Reverse()
                //               :
                //               container.Items.OrderBy(s => s.Serial.Value);


                var list = container.Items /*.OrderBy(s => s.Serial.Value)*/.ToArray();

                if (list.Length == 0)
                    return;

                if (list[0].X > 1)
                    list = list.Reverse().ToArray();


                foreach (Item it in list.Take(count))
                {
                    it.Price = p.ReadUInt();
                    byte nameLen = p.ReadByte();
                    string name = p.ReadASCII(nameLen);
                    bool fromcliloc = false;

                    if (int.TryParse(name, out int cliloc))
                    {
                        it.Name = FileManager.Cliloc.GetString(cliloc);
                        fromcliloc = true;
                    }
                    else if (string.IsNullOrEmpty(it.Name))
                        it.Name = name;

                    gump.SetIfNameIsFromCliloc(it, fromcliloc);
                }
            }
        }

        private static void UpdateCharacter(Packet p)
        {
            if (World.Player == null)
                return;

            Serial serial = p.ReadUInt();
            Mobile mobile = World.Mobiles.Get(serial);
            if (mobile == null)
                return;

            if (!mobile.Exists)
                GameActions.RequestMobileStatus(mobile);

            ushort graphic = p.ReadUShort();
            ushort x = p.ReadUShort();
            ushort y = p.ReadUShort();
            sbyte z = p.ReadSByte();
            Direction direction = (Direction) p.ReadByte();
            ushort hue = p.ReadUShort();


            mobile.Flags = (Flags)p.ReadByte();
            mobile.NotorietyFlag = (NotorietyFlag)p.ReadByte();

            if (mobile != World.Player)
            {
                //if (mobile.IsMoving && (byte) mobile.Direction == mobile.Steps.Back().Direction)
                //{
                //    var step = mobile.Steps.Back();

                //    mobile.Position = new Position((ushort) step.X, (ushort) step.Y, step.Z);
                //    mobile.Direction = (Direction) step.Direction;
                //    mobile.IsRunning = step.Run;
                //    mobile.Steps.Clear();
                //    mobile.AddToTile();
                //}

                mobile.Graphic = graphic;
                mobile.FixHue(hue);

                Direction dir = direction & Direction.Up;
                bool isrun = (direction & Direction.Running) != 0;

                if (!mobile.EnqueueStep(x, y, z, dir, isrun))
                {
                    mobile.Position = new Position(x, y, z);
                    mobile.Direction = dir;
                    mobile.IsRunning = isrun;
                    mobile.ClearSteps();
                    mobile.AddToTile();
                }

                mobile.ProcessDelta();

                if (World.Mobiles.Add(mobile))
                    World.Mobiles.ProcessDelta();

            }
            else
            {
                mobile.ProcessDelta();
                mobile.AddToTile();
            }
        }

        private static void UpdateObject(Packet p)
        {
            if (World.Player == null)
                return;

            Serial serial = p.ReadUInt();
            Graphic graphic = p.ReadUShort();
            ushort x = p.ReadUShort();
            ushort y = p.ReadUShort();
            sbyte z = p.ReadSByte();
            Direction direction = (Direction) p.ReadByte();
            Hue hue = p.ReadUShort();
            Flags flags = (Flags) p.ReadByte();
            NotorietyFlag notoriety = (NotorietyFlag) p.ReadByte();


            Mobile mobile = World.GetOrCreateMobile(serial);

            if (!mobile.Exists)
                GameActions.RequestMobileStatus(serial);

            mobile.Graphic = graphic;
            mobile.FixHue(hue);
            mobile.Flags = flags;
            mobile.NotorietyFlag = notoriety;

            if (p.ID != 0x78)
                p.Skip(6);

            uint itemSerial;

            // reset equipment
            mobile.Equipment = null;

            while ((itemSerial = p.ReadUInt()) != 0)
            {
                Item item = World.GetOrCreateItem(itemSerial);
                Graphic itemGraphic = p.ReadUShort();
                item.Layer = (Layer) p.ReadByte();

                if (FileManager.ClientVersion >= ClientVersions.CV_70331)
                    item.FixHue(p.ReadUShort());
                else if ((itemGraphic & 0x8000) != 0)
                {
                    itemGraphic &= 0x7FFF;
                    item.FixHue(p.ReadUShort());
                }
                //else
                //    itemGraphic &= 0x3FFF;

                item.Graphic = itemGraphic;
                item.Amount = 1;
                item.Container = mobile;
                mobile.Items.Add(item);
                mobile.Equipment[(int) item.Layer] = item;

                //if (World.OPL.Contains(serial))
                //    NetClient.Socket.Send(new PMegaClilocRequest(item));
                item.CheckGraphicChange();
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

            if (mobile != World.Player && !mobile.IsClicked && Engine.Profile.Current.ShowNewMobileNameIncoming)
                GameActions.SingleClick(mobile);

            Engine.UI.GetGump<PaperDollGump>(mobile)?.Update();


            if (mobile == World.Player)
            {
                if (World.Player.IsDead)
                    World.ChangeSeason(Seasons.Desolation, 42);
                else
                {
                    World.ChangeSeason(World.OldSeason, World.OldMusicIndex);
                }
            }
        }

        private static void OpenMenu(Packet p)
        {
            if (!World.InGame)
                return;

            Serial serial = p.ReadUInt();
            ushort id = p.ReadUShort();
            string name = p.ReadASCII(p.ReadByte());
            int count = p.ReadByte();

            Graphic menuid = p.ReadUShort();
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
                    Graphic graphic = p.ReadUShort();
                    Hue hue = p.ReadUShort();
                    name = p.ReadASCII(p.ReadByte());

                    Rectangle rect = FileManager.Art.GetTexture(graphic).Bounds;

                    if (rect.Width != 0 && rect.Height != 0)
                    {
                        int posY = rect.Height;

                        if (posY >= 47)
                            posY = 0;
                        else
                            posY = (47 - posY) >> 1;

                        gump.AddItem(graphic, hue, name, posX, posY, i + 1);

                        posX += rect.Width;
                    }
                }

                Engine.UI.Add(gump);
            }
            else
            {
                GrayMenuGump gump = new GrayMenuGump(serial, id, name)
                {
                    X = (Engine.WindowWidth >> 1) - 200,
                    Y = (Engine.WindowHeight >> 1) - ((121 + count * 21) >> 1)
                };

                int offsetY = 35 + gump.Height;
                int gumpHeight = 70 + offsetY;

                for (int i = 0; i < count; i++)
                {
                    p.Skip(4);
                    name = p.ReadASCII(p.ReadByte());

                    int addHeight = gump.AddItem(name, offsetY);

                    if (addHeight < 21)
                        addHeight = 21;


                    offsetY += addHeight - 1;
                    gumpHeight += addHeight;
                }

                offsetY += 5;

                gump.Add(new Button(0, 0x1450, 0x1451, 0x1450)
                {
                    ButtonAction = ButtonAction.Activate,
                    X = 70,
                    Y = offsetY
                });

                gump.Add(new Button(1, 0x13B2, 0x13B3, 0x13B2)
                {
                    ButtonAction = ButtonAction.Activate,
                    X = 200,
                    Y = offsetY
                });

                gump.SetHeight(gumpHeight);
                gump.WantUpdateSize = false;
                Engine.UI.Add(gump);
            }
        }


        private static void OpenPaperdoll(Packet p)
        {
            Mobile mobile = World.Mobiles.Get(p.ReadUInt());

            if (mobile == null) return;

            string text = p.ReadASCII(60);
            byte flags = p.ReadByte();

            var paperdoll = Engine.UI.GetGump<PaperDollGump>(mobile);

            if (paperdoll == null)
            {
                if (!Engine.UI.GetGumpCachePosition(mobile, out Point location)) location = new Point(100, 100);
                Engine.UI.Add(paperdoll = new PaperDollGump(mobile, text) {Location = location});
            }
            else
            {
                paperdoll.SetInScreen();
                paperdoll.BringOnTop();
            }

            paperdoll.CanLift = (flags & 0x02) != 0;
        }

        private static void CorpseEquipment(Packet p)
        {
            if (!World.InGame)
                return;

            Entity corpse = World.Get(p.ReadUInt());
            if (corpse == null)
                return;

            Layer layer = (Layer) p.ReadByte();

            while (layer != Layer.Invalid && p.Position < p.Length)
            {
                Item item = World.Items.Get(p.ReadUInt());

                if (item != null && item.Container == corpse)
                {
                    item.Layer = layer;
                    corpse.Equipment[(int) layer] = item;
                }

                layer = (Layer) p.ReadByte();
            }
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

                if (FileManager.Multimap.HasFacet(facet))
                    gump.SetMapTexture(FileManager.Multimap.LoadFacet(facet, width, height, startX, startY, endX, endY));
                else
                    gump.SetMapTexture(FileManager.Multimap.LoadMap(width, height, startX, startY, endX, endY));
            }
            else
                gump.SetMapTexture(FileManager.Multimap.LoadMap(width, height, startX, startY, endX, endY));

            Engine.UI.Add(gump);
        }

        private static void OpenBook(Packet p)
        {
            uint serial = p.ReadUInt();
            bool oldpacket = p.ID == 0x93;
            bool editable = p.ReadBool();

            if (!oldpacket)
                editable = p.ReadBool();
            else
                p.Skip(1);
            UIManager ui = Engine.UI;
            BookGump bgump = ui.GetGump<BookGump>(serial);

            if (bgump == null || bgump.IsDisposed)
            {
                ui.Add(new BookGump(serial)
                {
                    X = 100,
                    Y = 100,
                    BookPageCount = p.ReadUShort(),
                    //title allows only 47 dots (. + \0) so 47 is the right number
                    BookTitle =
                        new MultiLineBox(new MultiLineEntry(BookGump.DefaultFont, 47, 150, 150, BookGump.IsNewBookD4, FontStyle.None, 0), editable)
                        {
                            X = 40,
                            Y = 60,
                            Height = 25,
                            Width = 155,
                            IsEditable = editable,
                            Text = oldpacket ? p.ReadASCII(60).Trim('\0') : p.ReadASCII(p.ReadUShort()).Trim('\0')
                        },
                    //as the old booktitle supports only 30 characters in AUTHOR and since the new clients only allow 29 dots (. + \0 character at end), we use 29 as a limitation
                    BookAuthor =
                        new MultiLineBox(new MultiLineEntry(BookGump.DefaultFont, 29, 150, 150, BookGump.IsNewBookD4, FontStyle.None, 0), editable)
                        {
                            X = 40,
                            Y = 160,
                            Height = 25,
                            Width = 155,
                            IsEditable = editable,
                            Text = oldpacket ? p.ReadASCII(30).Trim('\0') : p.ReadASCII(p.ReadUShort()).Trim('\0')
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

            Direction direction = (Direction) p.ReadByte();
            World.Player.Walk(direction & Direction.Mask, (direction & Direction.Running) != 0);
        }

        private static void MultiPlacement(Packet p)
        {
            if (World.Player == null)
                return;

            var allowGround = p.ReadBool();
            var targID = p.ReadUInt();
            var flags = p.ReadByte();
            p.Seek(18);
            var multiID = p.ReadUShort();
            var xOff = p.ReadUShort();
            var yOff = p.ReadUShort();
            var zOff = p.ReadUShort();
            Hue hue = p.ReadUShort();
            TargetManager.SetTargetingMulti(targID, multiID, xOff, yOff, zOff, hue);
        }

        private static void ASCIIPrompt(Packet p)
        {
            if (!World.InGame)
                return;

            byte[] data = p.ReadArray(8);

            Chat.PromptData = new PromptData
            {
                Prompt = ConsolePrompt.ASCII,
                Data = data
            };
        }

        private static void SellList(Packet p)
        {
            if (!World.InGame)
                return;

            Mobile vendor = World.Mobiles.Get(p.ReadUInt());

            if (vendor == null) return;

            ushort countItems = p.ReadUShort();

            if (countItems <= 0) return;

            ShopGump gump = Engine.UI.GetGump<ShopGump>(vendor);
            gump?.Dispose();
            gump = new ShopGump(vendor, false, 100, 0);

            for (int i = 0; i < countItems; i++)
            {
                Item item = World.GetOrCreateItem(p.ReadUInt());
                item.Graphic = p.ReadUShort();
                item.FixHue(p.ReadUShort());
                item.Amount = p.ReadUShort();
                item.Price = p.ReadUShort();

                string name = p.ReadASCII(p.ReadUShort());
                bool fromcliloc = false;

                if (int.TryParse(name, out int clilocnum))
                {
                    name = FileManager.Cliloc.GetString(clilocnum);
                    fromcliloc = true;
                }

                if (!fromcliloc && string.IsNullOrEmpty(item.Name))
                    item.Name = name;

                gump.AddItem(item, fromcliloc);
            }

            Engine.UI.Add(gump);
        }

        private static void UpdateHitpoints(Packet p)
        {
            Entity entity = World.Get(p.ReadUInt());

            if (entity == null)
                return;

            entity.HitsMax = p.ReadUShort();
            entity.Hits = p.ReadUShort();
            entity.ProcessDelta();

            if (entity == World.Player)
                UoAssist.SignalHits();
        }

        private static void UpdateMana(Packet p)
        {
            Mobile mobile = World.Mobiles.Get(p.ReadUInt());

            if (mobile == null) return;

            mobile.ManaMax = p.ReadUShort();
            mobile.Mana = p.ReadUShort();
            mobile.ProcessDelta();

            if (mobile == World.Player) UoAssist.SignalMana();
        }

        private static void UpdateStamina(Packet p)
        {
            Mobile mobile = World.Mobiles.Get(p.ReadUInt());

            if (mobile == null) return;

            mobile.StaminaMax = p.ReadUShort();
            mobile.Stamina = p.ReadUShort();
            mobile.ProcessDelta();

            if (mobile == World.Player) UoAssist.SignalStamina();
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
                catch (Exception)
                {
                    Log.Message(LogTypes.Warning, "Failed to open url: " + url);
                }
            }
        }

        private static void TipWindow(Packet p)
        {
            byte flag = p.ReadByte();

            if (flag == 1)
                return;

            uint tip = p.ReadUInt();
            string str = p.ReadASCII(p.ReadUShort());

            if (flag == 0)
            {
                if (TipNoticeGump._tips == null || TipNoticeGump._tips.IsDisposed)
                {
                    TipNoticeGump._tips = new TipNoticeGump(flag, str);
                    Engine.UI.Add(TipNoticeGump._tips);
                }

                TipNoticeGump._tips.AddTip(tip, str);
            }
            else
                Engine.UI.Add(new TipNoticeGump(flag, str));
        }

        private static void AttackCharacter(Packet p)
        {
            Engine.UI.RemoveTargetLineGump(TargetManager.LastTarget);
            Engine.UI.RemoveTargetLineGump(TargetManager.LastAttack);

            TargetManager.LastAttack = p.ReadUInt();

            if (TargetManager.LastAttack != 0 && World.InGame)
            {
                Mobile mob = World.Mobiles.Get(TargetManager.LastAttack);

                if (mob != null && mob.HitsMax == 0)
                    NetClient.Socket.Send(new PStatusRequest(TargetManager.LastAttack));
            }
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
            if (!World.InGame)
            {
                LoginScene scene = Engine.SceneManager.GetScene<LoginScene>();

                if (scene != null)
                {
                    //Serial serial = p.ReadUInt();
                    //ushort graphic = p.ReadUShort();
                    //MessageType type = (MessageType)p.ReadByte();
                    //Hue hue = p.ReadUShort();
                    //MessageFont font = (MessageFont)p.ReadUShort();
                    //string lang = p.ReadASCII(4);
                    //string name = p.ReadASCII(30);
                    Log.Message(LogTypes.Warning, "UnicodeTalk received during LoginScene");

                    if (p.Length > 48)
                    {
                        p.Seek(48);
                        Log.PushIndent();
                        Log.Message(LogTypes.Warning, "Handled UnicodeTalk in LoginScene");
                        Log.PopIndent();
                    }
                }

                return;
            }


            Serial serial = p.ReadUInt();
            Entity entity = World.Get(serial);
            ushort graphic = p.ReadUShort();
            MessageType type = (MessageType) p.ReadByte();
            Hue hue = p.ReadUShort();
            ushort font = p.ReadUShort();
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

                NetClient.Socket.Send(buffer);

                return;
            }

            string text = string.Empty;

            if (p.Length > 48)
            {
                p.Seek(48);
                text = p.ReadUnicode();
            }

            if (entity != null)
            {
                if (string.IsNullOrEmpty(entity.Name))
                    entity.Name = name;
                entity.ProcessDelta();
            }

            Chat.HandleMessage(entity, text, name, hue, type, Engine.Profile.Current.ChatFont, true, lang);
        }

        private static void DisplayDeath(Packet p)
        {
            if (!World.InGame)
                return;

            Serial serial = p.ReadUInt();
            Serial corpseSerial = p.ReadUInt();
            Serial running = p.ReadUInt();

            Mobile owner = World.Mobiles.Get(serial);

            if (owner == null)
                return;

            serial |= 0x80000000;

            World.Mobiles.Replace(owner, serial);

            if (corpseSerial.IsValid)
                World.CorpseManager.Add(corpseSerial, serial, owner.Direction, running != 0);


            byte group = FileManager.Animations.GetDieGroupIndex(owner.Graphic, running != 0, true);
            owner.SetAnimation(group, 0, 5, 1);

            if (Engine.Profile.Current.AutoOpenCorpses)
                World.Player.TryOpenCorpses();
        }

        private static void OpenGump(Packet p)
        {
            if (World.Player == null)
                return;

            Serial sender = p.ReadUInt();
            Serial gumpID = p.ReadUInt();
            int x = (int) p.ReadUInt();
            int y = (int) p.ReadUInt();

            ushort cmdLen = p.ReadUShort();
            string cmd = p.ReadASCII(cmdLen);

            ushort textLinesCount = p.ReadUShort();

            string[] lines = new string[textLinesCount];

            ref var buffer = ref p.ToArray();

            for (int i = 0, index = p.Position; i < textLinesCount; i++)
            {
                int length = ((buffer[index++] << 8) | buffer[index++]) << 1;
                lines[i] = Encoding.BigEndianUnicode.GetString(buffer, index, length);
                index += length;
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
            if (!World.InGame)
                return;

            Serial serial = p.ReadUInt();
            string header = p.ReadASCII();
            string footer = p.ReadUnicode();

            string body = p.ReadUnicode();

            Engine.UI.GetGump<ProfileGump>(serial)?.Dispose();
            Engine.UI.Add(new ProfileGump(serial, header, footer, body, serial == World.Player.Serial));
        }

        private static void EnableLockedFeatures(Packet p)
        {
            uint flags = 0;

            if (FileManager.ClientVersion >= ClientVersions.CV_60142)
                flags = p.ReadUInt();
            else
                flags = p.ReadUShort();
            World.ClientLockedFeatures.SetFlags((LockedFeatureFlags) flags);

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

            var arrow = ui.GetGump<QuestArrowGump>(serial);

            if (display)
            {
                if (arrow == null)
                    ui.Add(new QuestArrowGump(serial, mx, my));
                else
                    arrow.SetRelativePosition(mx, my);
            }
            else
            {
                if (arrow != null)
                    arrow.Dispose();
            }
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

            if (season > 4)
                season = 0;


            if (World.Player.IsDead && season != 4)
                return;

            World.OldSeason = (Seasons) season;
            World.OldMusicIndex = music;

            if (World.Season == Seasons.Desolation)
            {
                World.OldMusicIndex = 42;
            }

            World.ChangeSeason((Seasons) season, music);
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

                    for (int i = 0; i < 6; i++) World.Player.Walker.FastWalkStack.SetValue(i, p.ReadUInt());
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
                    Serial ser = p.ReadUInt();
                    int button = (int) p.ReadUInt();

                    var gumpToClose = Engine.UI.Gumps.OfType<Gump>()
                                     .FirstOrDefault(s => !s.IsDisposed && s.ServerSerial == ser);

                    if (gumpToClose != null)
                    {
                        if (button != 0)
                            gumpToClose.OnButtonClick(button);
                        gumpToClose.Dispose();
                    }

                    break;

                //===========================================================================================
                //===========================================================================================
                case 6: //party
                    //PartyManager.HandlePartyPacket(p);

                    World.Party.ParsePacket(p);

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
                        str = FileManager.Cliloc.Translate(FileManager.Cliloc.GetString((int) cliloc), capitalize: true);

                        if (!string.IsNullOrEmpty(str))
                            item.Name = str;

                        Chat.HandleMessage(item, str, item.Name, 0x3B2, MessageType.Regular, 3, true);
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
                        short charges = (short) p.ReadUShort();
                        string attr = FileManager.Cliloc.GetString((int) next);

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
                        strBuffer.Append(']');

                    if (strBuffer.Length != 0) Chat.HandleMessage(item, strBuffer.ToString(), item.Name, 0x3B2, MessageType.Regular, 3, true);

                    NetClient.Socket.Send(new PMegaClilocRequestOld(item));

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
                        //int indx = World.MapIndex;
                        //World.MapIndex = -1;
                        //World.MapIndex = indx;
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

                                World.Player.StrLock = (Lock) ((state >> 4) & 3);
                                World.Player.DexLock = (Lock) ((state >> 2) & 3);
                                World.Player.IntLock = (Lock) (state & 3);

                                StatusGumpBase.GetStatusGump()?.UpdateLocksAfterPacket();
                            }

                            break;

                        case 5:
                            Mobile character = World.Mobiles.Get(serial);

                            if (character != null && p.Length == 19)
                                character.IsDead = p.ReadBool();

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

                    if (spellbook.FillSpellbook(sbtype, filed))
                    {
                        SpellbookGump gump = Engine.UI.GetGump<SpellbookGump>(spellbook);
                        gump?.Update();
                    }

                    break;

                //===========================================================================================
                //===========================================================================================
                case 0x1D: // house revision state
                    serial = p.ReadUInt();
                    uint revision = p.ReadUInt();

                    if (!World.HouseManager.TryGetHouse(serial, out House house) || !house.IsCustom || house.Revision != revision)
                        NetClient.Socket.Send(new PCustomHouseDataRequest(serial));
                    else
                    {
                        house.Generate();
                        Engine.UI.GetGump<MiniMapGump>()?.ForceUpdate();
                        if (World.HouseManager.EntityIntoHouse(serial, World.Player))
                            Engine.SceneManager.GetScene<GameScene>()?.UpdateMaxDrawZ(true);
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
                    World.Player.PrimaryAbility = (Ability) ((byte) World.Player.PrimaryAbility & 0x7F);
                    World.Player.SecondaryAbility = (Ability) ((byte) World.Player.SecondaryAbility & 0x7F);

                    break;

                //===========================================================================================
                //===========================================================================================
                case 0x22:
                    p.Skip(1);

                    Entity en = World.Get(p.ReadUInt());

                    if (en != null)
                    {
                        byte damage = p.ReadByte();

                        World.WorldTextManager
                              .AddDamage(en, damage);
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
            ushort font = p.ReadUShort();
            uint cliloc = p.ReadUInt();
            AffixType flags = p.ID == 0xCC ? (AffixType) p.ReadByte() : 0x00;
            string name = p.ReadASCII(30);
            string affix = p.ID == 0xCC ? p.ReadASCII() : string.Empty;

            string arguments = null;
            
            if (cliloc == 1008092) // value for "You notify them you don't want to join the party"
            {
                foreach (var PartyInviteGump in Engine.UI.Gumps.OfType<PartyInviteGump>())
                {
                    PartyInviteGump.Dispose();
                }
            }

            if (p.Position < p.Length)
                arguments = p.ReadUnicodeReversed(p.Length - p.Position);

            string text = FileManager.Cliloc.Translate((int) cliloc, arguments);

            if (text == null)
                return;

            if (!string.IsNullOrWhiteSpace(affix))
            {
                if ((flags & AffixType.Prepend) != 0)
                    text = $"{affix}{text}";
                else
                    text = $"{text}{affix}";
            }

            if ((flags & AffixType.System) != 0)
                type = MessageType.System;

            if (!FileManager.Fonts.UnicodeFontExists((byte) font))
                font = 0;

            if (entity != null)
            {
                //entity.Graphic = graphic;
                entity.Name = name;
                entity.ProcessDelta();
            }

            Chat.HandleMessage(entity, text, name, hue, type, (byte) font, true);
        }

        private static void UnicodePrompt(Packet p)
        {
            if (!World.InGame)
                return;

            byte[] data = p.ReadArray(8);

            Chat.PromptData = new PromptData
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
            if (!World.InGame)
                return;

            ushort unknown = p.ReadUShort();

            if (unknown > 1)
                return;

            Serial serial = p.ReadUInt();

            p.Skip(2);
            uint revision = p.ReadUInt();

            Entity entity = World.Mobiles.Get(serial);

            if (entity == null)
            {
                if (serial.IsMobile)
                    Log.Message(LogTypes.Warning, "Searching a mobile into World.Items from MegaCliloc packet");
                entity = World.Items.Get(serial);
            }

            if (entity != null)
            {

                int cliloc;

                List<string> list = new List<string>();

                while ((cliloc = (int) p.ReadUInt()) != 0)
                {
                    string argument = p.ReadUnicodeReversed(p.ReadUShort());

                    string str = FileManager.Cliloc.Translate(cliloc, argument, true);


                    for (int i = 0; i < list.Count; i++)
                    {
                        var tempstr = list[i];

                        if (tempstr == str)
                        {
                            list.RemoveAt(i);
                            break;
                        }
                    }

                    list.Add(str);
                }

                Item container = null;

                if (entity is Item it && it.Container.IsValid)
                {
                    container = World.Items.Get(it.Container);
                }

                bool inBuyList = false;

                if (container != null)
                {
                    inBuyList = container.Layer == Layer.ShopBuy ||
                                container.Layer == Layer.ShopBuyRestock ||
                                container.Layer == Layer.ShopSell;
                }


                bool first = true;

                string name = string.Empty;
                string data = string.Empty;

                if (list.Count != 0)
                {
                    foreach (string str in list)
                    {
                        if (first)
                        {
                            name = str;

                            if (entity != null && !entity.Serial.IsMobile)
                            {
                                entity.Name = str;
                            }

                            first = false;
                        }
                        else
                        {
                            if (data.Length != 0)
                                data += "\n";

                            data += str;
                        }
                    }
                }

                World.OPL.Add(serial, revision, name, data);

                if (inBuyList && container.Serial.IsValid)
                {
                    Engine.UI.GetGump<ShopGump>(container.RootContainer)?.SetNameTo((Item)entity, name);
                }

            }
        }

        private static void GenericAOSCommandsR(Packet p)
        {
        }

        private static unsafe void CustomHouse(Packet p)
        {
            bool compressed = p.ReadByte() == 0x03;
            bool enableReponse = p.ReadBool();
            Serial serial = p.ReadUInt();
            Item foundation = World.Items.Get(serial);
            uint revision = p.ReadUInt();

            if (foundation == null)
                return;

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

            if (multi.MinX <= 0 && multi.MinY <= 0 && multi.MaxX <= 0 && multi.MaxY <= 0)
            {
                Log.Message(LogTypes.Warning, "[CustomHouse (0xD8) - Invalid multi dimentions. Maybe missing some installation required files");
                return;
            }

            byte planes = p.ReadByte();

            DataReader stream = new DataReader();
            ref byte[] buffer = ref p.ToArray();

            for (int plane = 0; plane < planes; plane++)
            {
                uint header = p.ReadUInt();
                int dlen = (int) (((header & 0xFF0000) >> 16) | ((header & 0xF0) << 4));
                int clen = (int) (((header & 0xFF00) >> 8) | ((header & 0x0F) << 8));
                int planeZ = (int) ((header & 0x0F000000) >> 24);
                int planeMode = (int) ((header & 0xF0000000) >> 28);

                if (clen <= 0) continue;

                byte[] decompressedBytes = new byte[dlen];

                fixed (byte* srcPtr = &buffer[p.Position], destPtr = decompressedBytes)
                    ZLib.Decompress((IntPtr)srcPtr, clen, 0, (IntPtr)destPtr, dlen);

                stream.SetData(decompressedBytes, dlen);
                {
                    p.Skip(clen);
                    ushort id = 0;
                    sbyte x = 0, y = 0, z = 0;

                    switch (planeMode)
                    {
                        case 0:
                            int c = dlen / 5;
                            for (uint i = 0; i < c; i++)
                            {
                                id = stream.ReadUShortReversed();
                                x = stream.ReadSByte();
                                y = stream.ReadSByte();
                                z = stream.ReadSByte();

                                if (id != 0)
                                {
                                    Multi m = Multi.Create(id);
                                    m.Position = new Position((ushort) (foundation.X + x), (ushort) (foundation.Y + y), (sbyte) (foundation.Z + z));

                                    house.Components.Add(m);
                                }
                            }

                            break;

                        case 1:

                            if (planeZ > 0)
                                z = (sbyte) ((planeZ - 1) % 4 * 20 + 7);
                            else
                                z = 0;

                            c = dlen >> 2;
                            for (uint i = 0; i < c; i++)
                            {
                                id = stream.ReadUShortReversed();
                                x = stream.ReadSByte();
                                y = stream.ReadSByte();

                                if (id != 0)
                                {
                                    Multi m = Multi.Create(id);
                                    m.Position = new Position((ushort)(foundation.X + x), (ushort)(foundation.Y + y), (sbyte)(foundation.Z + z));

                                    house.Components.Add(m);
                                }
                            }

                            break;

                        case 2:
                            short offX = 0, offY = 0;
                            short multiHeight = 0;

                            if (planeZ > 0)
                                z = (sbyte) ((planeZ - 1) % 4 * 20 + 7);
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

                            c = dlen >> 1;
                            for (uint i = 0; i < c; i++)
                            {
                                id = stream.ReadUShortReversed();
                                x = (sbyte) (i / multiHeight + offX);
                                y = (sbyte) (i % multiHeight + offY);

                                if (id != 0)
                                {
                                    Multi m = Multi.Create(id);
                                    m.Position = new Position((ushort)(foundation.X + x), (ushort)(foundation.Y + y), (sbyte)(foundation.Z + z));

                                    house.Components.Add(m);
                                }
                            }

                            break;
                    }

                    house.Generate();
                    Engine.UI.GetGump<MiniMapGump>()?.ForceUpdate();

                    if (World.HouseManager.EntityIntoHouse(serial, World.Player))
                        Engine.SceneManager.GetScene<GameScene>()?.UpdateMaxDrawZ(true);
                }
                stream.ReleaseData();

            }
            stream.ReleaseData();
        }

        private static void CharacterTransferLog(Packet p)
        {
        }

        private static void OPLInfo(Packet p)
        {
            if (World.ClientFeatures.TooltipsEnabled)
            {
                Serial serial = p.ReadUInt();
                uint revision = p.ReadUInt();

                if (!World.OPL.IsRevisionEqual(serial, revision))
                    AddMegaClilocRequest(serial);
            }
        }

        private static void OpenCompressedGump(Packet p)
        {
            Serial sender = p.ReadUInt();
            Serial gumpID = p.ReadUInt();
            uint x = p.ReadUInt();
            uint y = p.ReadUInt();
            uint clen = p.ReadUInt() - 4;
            int dlen = (int) p.ReadUInt();
            byte[] decData = new byte[dlen];
            string layout;

            unsafe
            {
                ref var buffer = ref p.ToArray();

                fixed (byte* srcPtr = &buffer[p.Position], destPtr = decData)
                {
                    ZLib.Decompress((IntPtr) srcPtr, (int) clen, 0, (IntPtr) destPtr, dlen);
                    layout = Encoding.UTF8.GetString(destPtr, dlen);
                }
            }

            p.Skip((int)clen);

            uint linesNum = p.ReadUInt();
            string[] lines = new string[0];

            if (linesNum > 0)
            {
                clen = p.ReadUInt() - 4;
                dlen = (int) p.ReadUInt();
                decData = new byte[dlen];

                unsafe
                {
                    ref var buffer = ref p.ToArray();
                    fixed (byte* srcPtr = &buffer[p.Position], destPtr = decData)
                        ZLib.Decompress((IntPtr)srcPtr, (int)clen, 0, (IntPtr)destPtr, dlen);
                }

                p.Skip((int) clen);

                lines = new string[linesNum];

                for (int i = 0, index = 0; i < linesNum; i++)
                {
                    int length = ((decData[index++] << 8) | decData[index++]) << 1;
                    lines[i] = Encoding.BigEndianUnicode.GetString(decData, index, length);
                    index += length;

                }
            }

            Engine.UI.Create(sender, gumpID, (int) x, (int) y, layout, lines);
        }

        private static void UpdateMobileStatus(Packet p)
        {
        }

        private static void BuffDebuff(Packet p)
        {
            const int TABLE_COUNT = 126;
            const ushort BUFF_ICON_START = 0x03E9;
            Serial serial = p.ReadUInt();
            ushort iconID = (ushort) (p.ReadUShort() - BUFF_ICON_START);

            if (iconID < TABLE_COUNT)
            {
                UIManager ui = Engine.UI;
                BuffGump gump = ui.GetGump<BuffGump>();
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
                    string title = FileManager.Cliloc.GetString((int) titleCliloc);
                    string description = string.Empty;
                    string wtf = string.Empty;

                    if (descriptionCliloc != 0)
                    {
                        string args = p.ReadUnicodeReversed();
                        description = "\n" + FileManager.Cliloc.Translate((int) descriptionCliloc, args, true);

                        if (description.Length < 2)
                            description = string.Empty;
                    }

                    if (wtfCliloc != 0)
                        wtf = "\n" + FileManager.Cliloc.GetString((int) wtfCliloc);
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

            bool isparty = false;
            switch (type)
            {
                case 0x01: // custom party info
                    isparty = true;
                    goto case 0x02;
                case 0x02: // guild track info
                    bool locations = p.ReadBool();

                    uint serial;

                    while((serial = p.ReadUInt()) != 0)
                    {
                        if (locations)
                        {
                            ushort x = p.ReadUShort();
                            ushort y = p.ReadUShort();
                            byte map = p.ReadByte();
                            byte hits = p.ReadByte();

                            Log.Message(LogTypes.Info, $"Received custom {(isparty ? "party" : "guild")} member info: X: {x}, Y: {y}, Map: {map}, Hits: {hits}");
                        }
                    }

                    break;
                case 0xF0:
                    break;
                case 0xFE:
                    Log.Message(LogTypes.Info, "Razor ACK sended");
                    NetClient.Socket.Send(new PRazorAnswer());
                    break;
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
            Serial serial = p.ReadUInt();
            Graphic graphic = p.ReadUShort();
            byte graphicInc = p.ReadByte();
            ushort amount = p.ReadUShort();
            p.Skip(2);
            ushort x = p.ReadUShort();
            ushort y = p.ReadUShort();
            sbyte z = p.ReadSByte();
            Direction dir = (Direction) p.ReadByte();
            Hue hue = p.ReadUShort();
            Flags flags = (Flags) p.ReadByte();
            p.Skip(2);

            if (serial != World.Player)
            {
                if (serial.IsItem)
                {
                    Item item = World.GetOrCreateItem(serial);
                    item.Amount = amount;
                    Position position = new Position(x, y, z);
                    item.Direction = dir;
                    item.LightID = (byte) dir;
                    item.FixHue(hue);
                    item.Flags = flags;
                    item.Container = Serial.INVALID;

                    if (graphic != 0x2006)
                        graphic += graphicInc;
                    else if (!item.IsClicked && Engine.Profile.Current.ShowNewCorpseNameIncoming) GameActions.SingleClick(item);

                    if (graphic == 0x2006 && Engine.Profile.Current.AutoOpenCorpses) World.Player.TryOpenCorpses();

                    if (type == 0x02)
                    {
                        item.IsMulti = true;
                        item.WantUpdateMulti = (graphic & 0x3FFF) != item.Graphic || item.Position != position;
                        item.Graphic = (ushort) (graphic & 0x3FFF);
                    }
                    else
                    {
                        item.IsDamageable = type == 0x03;
                        item.IsMulti = false;
                        item.Graphic = graphic;
                    }

                    item.Position = position;
                    item.ProcessDelta();

                    if (World.Items.Add(item))
                        World.Items.ProcessDelta();

                    item.CheckGraphicChange(item.AnimIndex);
                    item.AddToTile();
                }
                else
                {
                    Mobile mobile = World.Mobiles.Get(serial);
                    if (mobile == null)
                        return;

                    mobile.Graphic = (ushort) (graphic + graphicInc);
                    mobile.FixHue(hue);
                    mobile.Flags = flags;
                    mobile.ProcessDelta();

                    if (World.Mobiles.Add(mobile))
                        World.Mobiles.ProcessDelta();

                    if (mobile == World.Player)
                        return;

                    Direction direction = dir & Direction.Up;
                    bool isrun = (dir & Direction.Running) != 0;

                    if (World.Get(mobile) == null || mobile.Position == Position.INVALID)
                    {
                        mobile.Position = new Position(x, y, z);
                        mobile.Direction = direction;
                        mobile.IsRunning = isrun;
                        mobile.AddToTile();
                    }

                    if (!mobile.EnqueueStep(x, y, z, direction, isrun))
                    {
                        mobile.Position = new Position(x, y, z);
                        mobile.Direction = direction;
                        mobile.IsRunning = isrun;
                        mobile.ClearSteps();
                        mobile.AddToTile();
                    }
                }
            }
            else if (p.ID == 0xF7)
            {

                Graphic oldGraphic = World.Player.Graphic;
                bool oldDead = World.Player.IsDead;

                World.Player.Position = new Position(x, y, z);
                World.RangeSize.X = x;
                World.RangeSize.Y = y;
                World.Player.Graphic = graphic;
                World.Player.Direction = dir;
                World.Player.FixHue(hue);
                World.Player.Flags = flags;


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
                World.RangeSize.X = x;
                World.RangeSize.Y = y;
                World.Player.Direction = dir;
                World.Player.Walker.DenyWalk(0xFF, -1, -1, -1);

                if (oldGraphic != 0 && oldGraphic != World.Player.Graphic)
                {
                    if (World.Player.IsDead)
                    {
                        TargetManager.Reset();
                    }
                }

                if (oldDead != World.Player.IsDead)
                {
                    if (World.Player.IsDead)
                        World.ChangeSeason(Seasons.Desolation, 42);
                    else
                        World.ChangeSeason(World.OldSeason, World.OldMusicIndex);
                }

                World.Player.Walker.ResendPacketSended = false;
                World.Player.AddToTile();
#endif
                World.Player.ProcessDelta();

                var scene = Engine.SceneManager.GetScene<GameScene>();

                if (scene != null)
                    scene.UpdateDrawPosition = true;

                World.Player.CloseRangedGumps();
            }
        }

        private static void BoatMoving(Packet p)
        {
            if (!World.InGame)
                return;

            Serial serial = p.ReadUInt();
            byte boatSpeed = p.ReadByte();
            Direction movingDirection = (Direction) p.ReadByte();
            Direction facingDirection = (Direction) p.ReadByte();
            ushort x = p.ReadUShort();
            ushort y = p.ReadUShort();
            ushort z = p.ReadUShort();

            Item item = World.Items.Get(serial);

            if (item == null)
                return;

            item.Position = new Position(x, y, (sbyte) z);
            item.AddToTile();
            //item.Graphic += (byte) facingDirection;
            //item.WantUpdateMulti = true;
            //item.CheckGraphicChange();
            if (World.HouseManager.TryGetHouse(item, out House house))
                house.Generate(true);


            int count = p.ReadUShort();

            for (int i = 0; i < count; i++)
            {
                Serial cSerial = p.ReadUInt();
                ushort cx = p.ReadUShort();
                ushort cy = p.ReadUShort();
                ushort cz = p.ReadUShort();

                Entity entity = World.Get(cSerial);

                if (entity != null)
                {
                    if (entity == World.Player)
                    {
                        World.RangeSize.X = cx;
                        World.RangeSize.Y = cy;
                    }

                    entity.Position = new Position(cx, cy, (sbyte) cz);
                    entity.AddToTile();
                }
            }
        }

        private static void PacketList(Packet p)
        {
            if (World.Player == null)
                return;

            int count = p.ReadUShort();

            for (int i = 0; i < count; i++)
            {
                byte id = p.ReadByte();

                if (id == 0xF3)
                {
                    UpdateItemSA(p);
                }
                else
                {
                    Log.Message(LogTypes.Warning, $"Unknown packet ID: [0x{id:X2}] in 0xF7");

                    break;
                }
            }
        }

        private static void AddItemToContainer(Serial serial, Graphic graphic, ushort amount, ushort x, ushort y, Hue hue, Serial containerSerial)
        {
            GameScene gs = Engine.SceneManager.GetScene<GameScene>();

            if (gs != null && gs.HeldItem.Serial == serial && gs.HeldItem.Dropped)
                gs.HeldItem.Clear();

            Entity container = World.Get(containerSerial);

            if (container == null)
            {
                Log.Message(LogTypes.Warning, $"No container ({containerSerial}) found");

                return;
            }

            Item item = World.Items.Get(serial);

            if (serial.IsMobile) Log.Message(LogTypes.Warning, "AddItemToContainer function adds mobile as Item");

            if (item != null && (container.Graphic != 0x2006 || item.Layer == Layer.Invalid))
            {
                Engine.UI.GetGump(item.Serial)?.Dispose();

                item.Destroy();

                Entity initcontainer = World.Get(item.Container);

                if (initcontainer != null)
                {
                    item.Container = Serial.INVALID;
                    initcontainer.Items.Remove(item);
                    initcontainer.ProcessDelta();
                }
                else if (item.Container.IsValid) 
                    Log.Message(LogTypes.Warning, $"This item ({item.Serial}) has a container ({item.Container}), but cannot be found. :|");

                World.Items.Remove(item);
                World.Items.ProcessDelta();
            }

            item = World.GetOrCreateItem(serial);
            item.Graphic = graphic;
            item.Amount = amount;
            item.FixHue(hue);
            item.Position = new Position(x, y);
            item.Container = containerSerial;

            container.Items.Add(item);
            World.Items.Add(item);
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