#region license

//  Copyright (C) 2020 ClassicUO Development Community on Github
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

using ClassicUO.Configuration;
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
using ClassicUO.Utility.Collections;
using ClassicUO.Utility.Logging;
using ClassicUO.Utility.Platforms;

using Microsoft.Xna.Framework;

namespace ClassicUO.Network
{
    internal class PacketHandlers
    {
        private static uint _requestedGridLoot;

        private readonly Action<Packet>[] _handlers = new Action<Packet>[0x100];

        static PacketHandlers()
        {
            Handlers = new PacketHandlers();
            NetClient.PacketReceived += Handlers.OnPacket;
        }


        public static PacketHandlers Handlers { get; }


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

        private List<uint> _clilocRequests = new List<uint>();

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
            Handlers.Add(0x32, p => { }); // unknown
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
            Handlers.Add(0xF0, KrriosClientSpecial);
            Handlers.Add(0xF1, FreeshardListR);
            Handlers.Add(0xF3, UpdateItemSA);
            Handlers.Add(0xF5, DisplayMap);
            Handlers.Add(0xF6, BoatMoving);
            Handlers.Add(0xF7, PacketList);
        }

       
        public static void SendMegaClilocRequests()
        {
            if (World.ClientFeatures.TooltipsEnabled && Handlers._clilocRequests.Count != 0)
            {
                if (Client.Version >= Data.ClientVersion.CV_500A)
                {
                    while (Handlers._clilocRequests.Count != 0)
                        NetClient.Socket.Send(new PMegaClilocRequest(ref Handlers._clilocRequests));
                }
                else
                {
                    foreach (uint serial in Handlers._clilocRequests)
                    {
                        NetClient.Socket.Send(new PMegaClilocRequestOld(serial));
                    }

                    Handlers._clilocRequests.Clear();
                }
            }
        }

        private static void AddMegaClilocRequest(uint serial)
        {
            foreach (uint s in Handlers._clilocRequests)
            {
                if (s == serial)
                    return;
            }

            Handlers._clilocRequests.Add(serial);
        }

        private static void TargetCursor(Packet p)
        {
            TargetManager.SetTargeting((CursorTarget) p.ReadByte(), p.ReadUInt(), (TargetType) p.ReadByte());

            if (World.Party.PartyHealTimer < Time.Ticks && World.Party.PartyHealTarget != 0)
            {
                TargetManager.Target(World.Party.PartyHealTarget);
                World.Party.PartyHealTimer = 0;
                World.Party.PartyHealTarget = 0;
            }
        }

        private static void SecureTrading(Packet p)
        {
            if (!World.InGame)
                return;

            byte type = p.ReadByte();
            uint serial = p.ReadUInt();

            if (type == 0)
            {
                uint id1 = p.ReadUInt();
                uint id2 = p.ReadUInt();

                // standard client doesn't allow the trading system if one of the traders is invisible (=not sent by server)
                if (World.Get(id1) == null || World.Get(id2) == null)
                    return;

                bool hasName = p.ReadBool();
                string name = string.Empty;

                if (hasName && p.Position < p.Length)
                    name = p.ReadASCII();

                UIManager.Add(new TradingGump(serial, name, id1, id2));
            }
            else if (type == 1)
                UIManager.Gumps.OfType<TradingGump>().FirstOrDefault(s => s.ID1 == serial || s.ID2 == serial)?.Dispose();
            else if (type == 2)
            {
                uint id1 = p.ReadUInt();
                uint id2 = p.ReadUInt();

                TradingGump trading = UIManager.Gumps.OfType<TradingGump>().FirstOrDefault(s => s.ID1 == serial || s.ID2 == serial);

                if (trading != null)
                {
                    trading.ImAccepting = id1 != 0;
                    trading.HeIsAccepting = id2 != 0;

                    trading.UpdateContent();
                }
            }
            else if (type == 3 || type == 4)
            {           
                TradingGump trading = UIManager.Gumps.OfType<TradingGump>().FirstOrDefault(s => s.ID1 == serial || s.ID2 == serial);

                if (trading != null)
                {
                    if (type == 4)
                    {
                        trading.Gold = p.ReadUInt();
                        trading.Platinum = p.ReadUInt();
                    }
                    else
                    {
                        trading.HisGold = p.ReadUInt();
                        trading.HisPlatinum = p.ReadUInt();
                    }         
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
                    World.Player.PhysicalResistance = (short) p.ReadUShort();
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
                        if (Client.Version >= Data.ClientVersion.CV_500A)
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
                        World.Player.FireResistance = (short) p.ReadUShort();
                        World.Player.ColdResistance = (short) p.ReadUShort();
                        World.Player.PoisonResistance = (short) p.ReadUShort();
                        World.Player.EnergyResistance = (short) p.ReadUShort();
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
            uint tofollow = p.ReadUInt();
            uint isfollowing = p.ReadUInt();
        }

        private static void NewHealthbarUpdate(Packet p)
        {
            if (World.Player == null)
                return;

            if (p.ID == 0x16 && Client.Version < Data.ClientVersion.CV_500A)
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
                        if (Client.Version >= Data.ClientVersion.CV_7000)
                            mobile.SetSAPoison(true);
                        else
                            flags |= 0x04;
                    }
                    else
                    {
                        if (Client.Version >= Data.ClientVersion.CV_7000)
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
            item.X = x;
            item.Y = y;
            item.Z = z;
            item.UpdateScreenPosition();
            item.FixHue(hue);
            item.Flags = (Flags) flags;
            item.Direction = (Direction) direction;

            if (graphic >= 0x4000)
            {
                item.Graphic -= 0x4000;
                item.WantUpdateMulti = true;
                item.IsMulti = true;
            }

            item.LightID = direction;
            if (SerialHelper.IsValid(item.Container))
            {
                var cont = World.Get(item.Container);
                cont.Items.Remove(item.Serial);
                cont.ProcessDelta();
            }
            item.Container = 0;
            item.CheckGraphicChange();
            item.ProcessDelta();


            if (World.Items.Add(item)) World.Items.ProcessDelta();

            if (item.OnGround)
                item.AddToTile();


            if (graphic == 0x2006 && !item.IsClicked && ProfileManager.Current.ShowNewCorpseNameIncoming) GameActions.SingleClick(item);

            if (graphic == 0x2006 && ProfileManager.Current.AutoOpenCorpses) World.Player.TryOpenCorpses();
        }

        private static void EnterWorld(Packet p)
        {
            if (ProfileManager.Current == null)
                ProfileManager.Load(World.ServerName, LoginScene.Account, Settings.GlobalSettings.LastCharacterName.Trim());

            if (World.Player != null)
            {
                World.Clear();
            }

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

            World.Player.X = x;
            World.Player.Y = y;
            World.Player.Z = z;
            World.Player.UpdateScreenPosition();
            World.Player.Direction = direction;
            World.Player.AddToTile();

            World.RangeSize.X = x;
            World.RangeSize.Y = y;

            if (ProfileManager.Current.UseCustomLightLevel)
                World.Light.Overall = ProfileManager.Current.LightLevel;

            if (Client.Version >= Data.ClientVersion.CV_200)
            {
                NetClient.Socket.Send(new PGameWindowSize((uint) ProfileManager.Current.GameWindowSize.X, (uint) ProfileManager.Current.GameWindowSize.Y));
                NetClient.Socket.Send(new PLanguage("ENU"));
            }

            NetClient.Socket.Send(new PClientVersion(Settings.GlobalSettings.ClientVersion));

            GameActions.SingleClick(World.Player);
            NetClient.Socket.Send(new PSkillsRequest(World.Player));
            World.Player.ProcessDelta();
            World.Mobiles.ProcessDelta();

            if (World.Player.IsDead)
                World.ChangeSeason(Seasons.Desolation, 42);

            if (Client.Version >= Data.ClientVersion.CV_70796)
            {
                NetClient.Socket.Send(new PShowPublicHouseContent(ProfileManager.Current.ShowHouseContent));
            }
        }

        private static void Talk(Packet p)
        {
            uint serial = p.ReadUInt();
            Entity entity = World.Get(serial);
            ushort graphic = p.ReadUShort();
            MessageType type = (MessageType) p.ReadByte();
            ushort hue = p.ReadUShort();
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

            uint serial = p.ReadUInt();

            if (World.Player == serial)
                return;

            Entity entity = World.Get(serial);

            if (entity == null)
                return;

            bool updateAbilities = false;

            if (SerialHelper.IsItem(serial))
            {
                Item it = (Item)entity;
                uint cont = it.Container & 0x7FFFFFFF;

                if (SerialHelper.IsValid(it.Container))
                {
                    Entity top = World.Get(it.RootContainer);

                    if (top != null)
                    {
                        if (top == World.Player) updateAbilities = it.Layer == Layer.OneHanded || it.Layer == Layer.TwoHanded;

                        var tradeBox = top.Items.FirstOrDefault(s => s.Graphic == 0x1E5E && s.Layer == Layer.Invalid);

                        if (tradeBox != null)
                            UIManager.Gumps.OfType<TradingGump>().FirstOrDefault(s => s.ID1 == tradeBox || s.ID2 == tradeBox)?.UpdateContent();
                    }

                    GameScene scene = Client.Game.GetScene<GameScene>();

                    if (cont == World.Player && it.Layer == Layer.Invalid)
                        scene.HeldItem.Enabled = false;


                    if (it.Layer != Layer.Invalid)
                        UIManager.GetGump<PaperDollGump>(cont)?.Update();
                }
            }

            if (World.CorpseManager.Exists(0, serial))
                return;

            if (SerialHelper.IsMobile(serial))
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
            else if (SerialHelper.IsItem(serial))
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
                        UIManager.GetGump<PaperDollGump>(cont)?.Update();
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

            World.Player.CloseBank();

            World.Player.Walker.WalkingFailed = false;
            World.Player.X = x;
            World.Player.Y = y;
            World.Player.Z = z;
            World.Player.UpdateScreenPosition();
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

            World.Player.Walker.ResendPacketResync = false;
            World.Player.AddToTile();
            World.Player.ProcessDelta();

            var scene = Client.Game.GetScene<GameScene>();

            if (scene != null)
            {
                scene.Weather?.Reset();
                scene.UpdateDrawPosition = true;
            }


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

            World.Player.Walker.DenyWalk(seq, x, y, z);
            World.Player.Direction = direction;
            World.Player.ProcessDelta();

            Client.Game.GetScene<GameScene>()?.Weather?.Reset();
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
            World.Player.Walker.ConfirmWalk(seq);
            World.Player.ProcessDelta();

            World.Player.AddToTile();
        }

        private static void DragAnimation(Packet p)
        {
            ushort graphic = p.ReadUShort();
            graphic += p.ReadByte();
            ushort hue = p.ReadUShort();
            ushort count = p.ReadUShort();
            uint source = p.ReadUInt();
            ushort sourceX = p.ReadUShort();
            ushort sourceY = p.ReadUShort();
            sbyte sourceZ = p.ReadSByte();
            uint dest = p.ReadUInt();
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
                sourceX = entity.X;
                sourceY = entity.Y;
                sourceZ = entity.Z;
            }

            Mobile destEntity = World.Mobiles.Get(dest);

            if (destEntity == null)
                dest = 0;
            else
            {
                destX = destEntity.X;
                destY = destEntity.Y;
                destZ = destEntity.Z;
            }

            GameEffect effect;


            if (!SerialHelper.IsValid(source) || !SerialHelper.IsValid(dest))
            {
                effect = new MovingEffect(source, dest, sourceX, sourceY, sourceZ,
                                          destX, destY, destZ, graphic, hue, true, 5)
                {
                    Duration = Time.Ticks + 5000,
                };
            }
            else
            {
                effect = new DragEffect(source, dest, sourceX, sourceY, sourceZ,
                                        destX, destY, destZ, graphic, hue)
                {
                    Duration = Time.Ticks + 5000
                };
            }

            if (effect.AnimDataFrame.FrameCount != 0)
            {
                effect.IntervalInMs = effect.AnimDataFrame.FrameInterval * 45;
            }
            else
            {
                effect.IntervalInMs = 13;
            }

            World.AddEffect(effect);
        }

        private static void OpenContainer(Packet p)
        {
            if (World.Player == null)
                return;

            uint serial = p.ReadUInt();
            ushort graphic = p.ReadUShort();


            if (graphic == 0xFFFF)
            {
                Item spellBookItem = World.Items.Get(serial);
                if (spellBookItem == null)
                    return;

                UIManager.GetGump<SpellbookGump>(serial)?.Dispose();
                SpellbookGump spellbookGump = new SpellbookGump(spellBookItem);
                if (!UIManager.GetGumpCachePosition(spellBookItem, out Point location)) location = new Point(64, 64);

                spellbookGump.Location = location;
                UIManager.Add(spellbookGump);

                Client.Game.Scene.Audio.PlaySound(0x0055);
            }
            else if (graphic == 0x30)
            {
                Mobile vendor = World.Mobiles.Get(serial);

                if (vendor == null)
                    return;

                UIManager.GetGump<ShopGump>(serial)?.Dispose();
                ShopGump gump = new ShopGump(serial, true, 150, 5);
                UIManager.Add(gump);

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
                    if (item.IsCorpse && (ProfileManager.Current.GridLootType == 1 || ProfileManager.Current.GridLootType == 2))
                    {
                        UIManager.GetGump<GridLootGump>(serial)?.Dispose();
                        UIManager.Add(new GridLootGump(serial));
                        _requestedGridLoot = serial;

                        if (ProfileManager.Current.GridLootType == 1)
                            return;
                    }

                    UIManager.GetGump<ContainerGump>(serial)?.Dispose();
                    UIManager.Add(new ContainerGump(item, graphic));
                }
                else 
                    Log.Error( "[OpenContainer]: item not found");
            }

        }

        private static void UpdateContainedItem(Packet p)
        {
            if (!World.InGame)
                return;

            uint serial = p.ReadUInt();
            ushort graphic = (ushort) (p.ReadUShort() + p.ReadByte());
            ushort amount = Math.Max((ushort) 1, p.ReadUShort());
            ushort x = p.ReadUShort();
            ushort y = p.ReadUShort();

            if (Client.Version >= Data.ClientVersion.CV_6017)
                p.Skip(1);

            uint containerSerial = p.ReadUInt();
            ushort hue = p.ReadUShort();

            AddItemToContainer(serial, graphic, amount, x, y, hue, containerSerial);

            World.Get(containerSerial)?.Items.ProcessDelta();
            World.Items.ProcessDelta();


            if (SerialHelper.IsMobile(containerSerial))
            {
                Mobile m = World.Mobiles.Get(containerSerial);
                Item secureBox = m?.GetSecureTradeBox();
                if (secureBox != null)
                {
                    var gump = UIManager.Gumps.OfType<TradingGump>().SingleOrDefault(s => s.LocalSerial == secureBox || s.ID1 == secureBox || s.ID2 == secureBox);

                    if (gump != null) gump.UpdateContent();
                }
            }
            else if (SerialHelper.IsItem(containerSerial))
            {
                var gump = UIManager.Gumps.OfType<TradingGump>().SingleOrDefault(s => s.LocalSerial == containerSerial || s.ID1 == containerSerial || s.ID2 == containerSerial);

                if (gump != null) gump.UpdateContent();
            }
        }

        private static void DenyMoveItem(Packet p)
        {
            if (!World.InGame)
                return;

            GameScene scene = Client.Game.GetScene<GameScene>();

            ItemHold hold = scene.HeldItem;

            Item item = World.Items.Get(hold.Serial);

            if (hold.Enabled || hold.Dropped && item == null)
            {
                if (hold.Layer == Layer.Invalid && SerialHelper.IsValid(hold.Container))
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
                        item.X = hold.X;
                        item.Y = hold.Y;
                        item.Z = hold.Z;
                        item.UpdateScreenPosition();

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
                        item.X = hold.X;
                        item.Y = hold.Y;
                        item.Z = hold.Z;
                        item.UpdateScreenPosition();


                        Entity container = null;
                        if (!hold.OnGround)
                        {
                            container = World.Get(item.Container);

                            if (container != null)
                            {
                                if (SerialHelper.IsMobile(container.Serial))
                                {
                                    Mobile mob = (Mobile) container;

                                    mob.Items.Add(item);

                                    mob.Equipment[(int) hold.Layer] = item;
                                }
                                else
                                    Log.Warn( "SOMETHING WRONG WITH CONTAINER (should be a mobile)");
                            }
                            else
                                Log.Warn( "SOMETHING WRONG WITH CONTAINER (is null)");
                        }
                        else
                            item.AddToTile();

                        World.Items.Add(item);
                        item.ProcessDelta();
                        container?.Items.ProcessDelta();
                        container?.ProcessDelta();
                        World.Items.ProcessDelta();

                        if (item.Layer != 0)
                            UIManager.GetGump<PaperDollGump>(item.Container)?.Update();
                    }
                }

                hold.Clear();
            }
            else
                Log.Warn( "There was a problem with ItemHold object. It was cleared before :|");

            byte code = p.ReadByte();

            if (code < 5) Chat.HandleMessage(null, ServerErrorMessages.GetError(p.ID, code), string.Empty, 1001, MessageType.System, 3);
        }

        private static void EndDraggingItem(Packet p)
        {
            if (!World.InGame)
                return;

            GameScene scene = Client.Game.GetScene<GameScene>();

            scene.HeldItem.Enabled = false;
            scene.HeldItem.Dropped = false;
        }

        private static void DropItemAccepted(Packet p)
        {
            if (!World.InGame)
                return;

            GameScene scene = Client.Game.GetScene<GameScene>();

            scene.HeldItem.Enabled = false;
            scene.HeldItem.Dropped = false;
        }

        private static void DeathScreen(Packet p)
        {
            // todo
            byte action = p.ReadByte();

            if (action != 1)
            {
                Client.Game.GetScene<GameScene>()?.Weather?.Reset();
                Client.Game.Scene.Audio.PlayMusic(42);

                if (ProfileManager.Current.EnableDeathScreen)
                    World.Player.DeathScreenTimer = Time.Ticks + Constants.DEATH_SCREEN_TIMER;

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

            uint serial = p.ReadUInt();

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

                item.Container = 0;
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

            GameScene gs = Client.Game.GetScene<GameScene>();

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
                if (ProfileManager.Current.StandardSkillsGump)
                {
                    var gumpSkills = UIManager.GetGump<StandardSkillsGump>();

                    if (gumpSkills == null)
                    {
                        UIManager.Add(new StandardSkillsGump
                        {
                            X = 100,
                            Y = 100
                        });
                    }
                }
                else
                {
                    var gumpSkills = UIManager.GetGump<SkillGumpAdvanced>();

                    if (gumpSkills == null)
                    {
                        UIManager.Add(new SkillGumpAdvanced
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
                uint serial = p.ReadUInt();
                ushort graphic = (ushort) (p.ReadUShort() + p.ReadByte());
                ushort amount = Math.Max(p.ReadUShort(), (ushort) 1);
                ushort x = p.ReadUShort();
                ushort y = p.ReadUShort();

                if (Client.Version >= Data.ClientVersion.CV_6017)
                    p.Skip(1);
                uint containerSerial = p.ReadUInt();
                ushort hue = p.ReadUShort();

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
                                          s.Container = 0;
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
                                          s.Container = 0;
                                          container.Items.Remove(s);
                                          World.Items.Remove(s);
                                      });
                        }

                        container.ProcessDelta();
                        World.Items.ProcessDelta();
                    }
                }


                AddItemToContainer(serial, graphic, amount, x, y, hue, containerSerial);

                if (grid == null && ProfileManager.Current.GridLootType > 0)
                {
                    grid = UIManager.GetGump<GridLootGump>(containerSerial);

                    if (_requestedGridLoot != 0 && _requestedGridLoot == containerSerial && grid == null)
                    {
                        grid = new GridLootGump(_requestedGridLoot);
                        UIManager.Add(grid);
                        _requestedGridLoot = 0;
                    }
                }
            }

            container?.Items.ProcessDelta();

            if (container != null && SerialHelper.IsItem(container.Serial))
            {
                UIManager.GetGump<SpellbookGump>(container)?.Update();
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

                if (!ProfileManager.Current.UseCustomLightLevel)
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

            if (!ProfileManager.Current.UseCustomLightLevel)
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

            float volume = ProfileManager.Current.SoundVolume / Constants.SOUND_DELTA;
            float distanceFactor = 0.0f;

            if (distance <= World.ClientViewRange && distance >= 1)
            {
                float volumeByDist = volume / World.ClientViewRange;
                distanceFactor = volumeByDist * distance;
            }

            Client.Game.Scene.Audio.PlaySoundWithDistance(index, volume, distanceFactor);
        }

        private static void PlayMusic(Packet p)
        {
            ushort index = p.ReadUShort();

            Client.Game.Scene.Audio.PlayMusic(index);
        }

        private static void LoginComplete(Packet p)
        {
            if (World.Player != null && Client.Game.Scene is LoginScene)
            {
                GameScene scene = new GameScene();
                Client.Game.SetScene(scene);

                GameActions.OpenPaperdoll(World.Player);
                NetClient.Socket.Send(new PStatusRequest(World.Player));
                NetClient.Socket.Send(new POpenChat(""));


                //NetClient.Socket.Send(new PSkillsRequest(World.Player));
                //scene.DoubleClickDelayed(World.Player);

                if (Client.Version >= Data.ClientVersion.CV_306E)
                    NetClient.Socket.Send(new PClientType());

                if (Client.Version >= Data.ClientVersion.CV_305D)
                    NetClient.Socket.Send(new PClientViewRange(World.ClientViewRange));

                ProfileManager.Current.ReadGumps()?.ForEach(UIManager.Add);
            }
        }

        private static void MapData(Packet p)
        {
            if (!World.InGame)
                return;

            uint serial = p.ReadUInt();

            MapGump gump = UIManager.GetGump<MapGump>(serial);

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
            var scene = Client.Game.GetScene<GameScene>();
            if (scene == null)
                return;

            var weather = scene.Weather;
            byte type = p.ReadByte();

            if (weather.CurrentWeather != type) 
            {
            weather.Reset();
            
            weather.Type = type;
            weather.Count = p.ReadByte();

            bool showMessage = (weather.Count > 0);

            if (weather.Count > 70)
                weather.Count = 70;

            weather.Temperature = p.ReadByte();
            weather.Timer = Time.Ticks + Constants.WEATHER_TIMER;
            weather.Generate();

            switch (type)
            {
                case 0:
                    if (showMessage)
                    { 
                        GameActions.Print("It begins to rain.", 1154, MessageType.System, 3, false);
                        weather.CurrentWeather = 0;
                    }
                    break;

                case 1:
                    if (showMessage)
                    {
                        GameActions.Print("A fierce storm approaches.", 1154, MessageType.System, 3, false);
                        weather.CurrentWeather = 1;
                    }
                    break;

                case 2:
                    if (showMessage)
                    {
                        GameActions.Print("It begins to snow.", 1154, MessageType.System, 3, false);
                        weather.CurrentWeather = 2;
                    }
                    break;

                case 3:
                    if (showMessage)
                    {
                        GameActions.Print("A storm is brewing.", 1154, MessageType.System, 3, false);
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

        private static void BookData(Packet p)
        {
            if (!World.InGame)
                return;

            var serial = p.ReadUInt();
            var pageCnt = p.ReadUShort();
            var pages = new string[pageCnt];
            var gump = UIManager.GetGump<BookGump>(serial);

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
                    Log.Error( "BOOKGUMP: The server is sending a page number GREATER than the allowed number of pages in BOOK!");
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

                    Log.Warn( "Effect not implemented");
                }

                return;
            }

            uint source = p.ReadUInt();
            uint target = p.ReadUInt();
            ushort graphic = p.ReadUShort();
            ushort srcX = p.ReadUShort();
            ushort srcY = p.ReadUShort();
            sbyte srcZ = p.ReadSByte();
            ushort targetX = p.ReadUShort();
            ushort targetY = p.ReadUShort();
            sbyte targetZ = p.ReadSByte();
            byte speed = p.ReadByte();
            ushort duration = p.ReadByte();
            p.Skip(2);
            bool fixedDirection = p.ReadBool();
            bool doesExplode = p.ReadBool();
            ushort hue = 0;
            GraphicEffectBlendMode blendmode = 0;

            if (p.ID != 0x70)
            {
                hue = (ushort) p.ReadUInt();
                blendmode = (GraphicEffectBlendMode) (p.ReadUInt() % 7);
            }

            World.AddEffect(type, source, target, graphic, hue, srcX, srcY, srcZ, targetX, targetY, targetZ, speed, duration, fixedDirection, doesExplode, false, blendmode);
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
                    uint serial = p.ReadUInt();
                    Item item = World.Items.Get(serial);

                    if (item != null)
                    {
                        BulletinBoardGump bulletinBoard = UIManager.GetGump<BulletinBoardGump>(serial);
                        bulletinBoard?.Dispose();

                        int x = (Client.Game.Window.ClientBounds.Width >> 1) - 245;
                        int y = (Client.Game.Window.ClientBounds.Height >> 1) - 205;

                        bulletinBoard = new BulletinBoardGump(item, x, y, p.ReadASCII(22));
                        UIManager.Add(bulletinBoard);
                    }
                }

                    break;

                case 1: // summary msg

                {
                    uint boardSerial = p.ReadUInt();
                    BulletinBoardGump bulletinBoard = UIManager.GetGump<BulletinBoardGump>(boardSerial);

                    if (bulletinBoard != null)
                    {
                        uint serial = p.ReadUInt();
                        uint parendID = p.ReadUInt();

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
                    uint boardSerial = p.ReadUInt();
                    BulletinBoardGump bulletinBoard = UIManager.GetGump<BulletinBoardGump>(boardSerial);

                    if (bulletinBoard != null)
                    {
                        uint serial = p.ReadUInt();

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

                        UIManager.Add(new BulletinBoardItem(serial, 0, poster, subject, dataTime, sb.ToString(), variant));
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
                        it.Name = UOFileManager.Cliloc.GetString(cliloc);
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

            uint serial = p.ReadUInt();
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
                    mobile.X = x;
                    mobile.Y = y;
                    mobile.Z = z;
                    mobile.UpdateScreenPosition();
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

            uint serial = p.ReadUInt();
            ushort graphic = p.ReadUShort();
            ushort x = p.ReadUShort();
            ushort y = p.ReadUShort();
            sbyte z = p.ReadSByte();
            Direction direction = (Direction) p.ReadByte();
            ushort hue = p.ReadUShort();
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
                ushort itemGraphic = p.ReadUShort();
                byte layer = p.ReadByte();
                item.Layer = (Layer) layer;

                if (Client.Version >= Data.ClientVersion.CV_70331)
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

                if (layer < mobile.Equipment.Length)
                {
                    mobile.Equipment[layer] = item;
                }
                else
                {
                    Log.Warn($"Invalid layer in UpdateObject(). Layer: {layer}");
                }

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
                    mobile.X = x;
                    mobile.Y = y;
                    mobile.Z = z;
                    mobile.UpdateScreenPosition();
                    mobile.Direction = dir;
                    mobile.IsRunning = isrun;
                    mobile.AddToTile();
                }

                if (!mobile.EnqueueStep(x, y, z, dir, isrun))
                {
                    mobile.X = x;
                    mobile.Y = y;
                    mobile.Z = z;
                    mobile.UpdateScreenPosition();
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

            if (mobile != World.Player && !mobile.IsClicked && ProfileManager.Current.ShowNewMobileNameIncoming)
                GameActions.SingleClick(mobile);

            UIManager.GetGump<PaperDollGump>(mobile)?.Update();


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

            uint serial = p.ReadUInt();
            ushort id = p.ReadUShort();
            string name = p.ReadASCII(p.ReadByte());
            int count = p.ReadByte();

            ushort menuid = p.ReadUShort();
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
                    ushort graphic = p.ReadUShort();
                    ushort hue = p.ReadUShort();
                    name = p.ReadASCII(p.ReadByte());

                    Rectangle rect = UOFileManager.Art.GetTexture(graphic).Bounds;

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
                UIManager.Add(gump);
            }
        }


        private static void OpenPaperdoll(Packet p)
        {
            Mobile mobile = World.Mobiles.Get(p.ReadUInt());

            if (mobile == null) return;

            string text = p.ReadASCII(60);
            byte flags = p.ReadByte();

            mobile.Title = text;

            var paperdoll = UIManager.GetGump<PaperDollGump>(mobile);

            if (paperdoll == null)
            {
                if (!UIManager.GetGumpCachePosition(mobile, out Point location))
                    location = new Point(100, 100);
                UIManager.Add(paperdoll = new PaperDollGump(mobile) {Location = location});
            }
            else
            {
                paperdoll.UpdateTitle(text);
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
            uint serial = p.ReadUInt();
            ushort gumpid = p.ReadUShort();
            ushort startX = p.ReadUShort();
            ushort startY = p.ReadUShort();
            ushort endX = p.ReadUShort();
            ushort endY = p.ReadUShort();
            ushort width = p.ReadUShort();
            ushort height = p.ReadUShort();

            MapGump gump = new MapGump(serial, gumpid, width, height);

            if (p.ID == 0xF5 || Client.Version >= Data.ClientVersion.CV_308Z)
            {
                ushort facet = 0;

                if (p.ID == 0xF5)
                    facet = p.ReadUShort();

                if (UOFileManager.Multimap.HasFacet(facet))
                    gump.SetMapTexture(UOFileManager.Multimap.LoadFacet(facet, width, height, startX, startY, endX, endY));
                else
                    gump.SetMapTexture(UOFileManager.Multimap.LoadMap(width, height, startX, startY, endX, endY));
            }
            else
                gump.SetMapTexture(UOFileManager.Multimap.LoadMap(width, height, startX, startY, endX, endY));

            UIManager.Add(gump);
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
            BookGump bgump = UIManager.GetGump<BookGump>(serial);

            if (bgump == null || bgump.IsDisposed)
            {
                UIManager.Add(new BookGump(serial)
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
            uint serial = p.ReadUInt();
            p.Skip(2);
            ushort graphic = p.ReadUShort();

            Rectangle rect = UOFileManager.Gumps.GetTexture(0x0906).Bounds;

            int x = (Client.Game.Window.ClientBounds.Width >> 1) - (rect.Width >> 1);
            int y = (Client.Game.Window.ClientBounds.Height >> 1) - (rect.Height >> 1);

            ColorPickerGump gump = new ColorPickerGump(serial, graphic, x, y, null);

            UIManager.Add(gump);
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
            ushort hue = p.ReadUShort();
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

            ShopGump gump = UIManager.GetGump<ShopGump>(vendor);
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
                    name = UOFileManager.Cliloc.GetString(clilocnum);
                    fromcliloc = true;
                }

                if (!fromcliloc && string.IsNullOrEmpty(item.Name))
                    item.Name = name;

                gump.AddItem(item, fromcliloc);
            }

            UIManager.Add(gump);
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
                    Log.Warn( "Failed to open url: " + url);
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
                    UIManager.Add(TipNoticeGump._tips);
                }

                TipNoticeGump._tips.AddTip(tip, str);
            }
            else
                UIManager.Add(new TipNoticeGump(flag, str));
        }

        private static void AttackCharacter(Packet p)
        {
            UIManager.RemoveTargetLineGump(TargetManager.LastTarget);
            UIManager.RemoveTargetLineGump(TargetManager.LastAttack);

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

            uint serial = p.ReadUInt();
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

            UIManager.Add(gump);
        }

        private static void UnicodeTalk(Packet p)
        {
            if (!World.InGame)
            {
                LoginScene scene = Client.Game.GetScene<LoginScene>();

                if (scene != null)
                {
                    //Serial serial = p.ReadUInt();
                    //ushort graphic = p.ReadUShort();
                    //MessageType type = (MessageType)p.ReadByte();
                    //Hue hue = p.ReadUShort();
                    //MessageFont font = (MessageFont)p.ReadUShort();
                    //string lang = p.ReadASCII(4);
                    //string name = p.ReadASCII(30);
                    Log.Warn( "UnicodeTalk received during LoginScene");

                    if (p.Length > 48)
                    {
                        p.Seek(48);
                        Log.PushIndent();
                        Log.Warn( "Handled UnicodeTalk in LoginScene");
                        Log.PopIndent();
                    }
                }

                return;
            }


            uint serial = p.ReadUInt();
            Entity entity = World.Get(serial);
            ushort graphic = p.ReadUShort();
            MessageType type = (MessageType) p.ReadByte();
            ushort hue = p.ReadUShort();
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

            Chat.HandleMessage(entity, text, name, hue, type, ProfileManager.Current.ChatFont, true, lang);
        }

        private static void DisplayDeath(Packet p)
        {
            if (!World.InGame)
                return;

            uint serial = p.ReadUInt();
            uint corpseSerial = p.ReadUInt();
            uint running = p.ReadUInt();

            Mobile owner = World.Mobiles.Get(serial);

            if (owner == null)
                return;

            serial |= 0x80000000;

            World.Mobiles.Replace(owner, serial);

            if (SerialHelper.IsValid(corpseSerial))
                World.CorpseManager.Add(corpseSerial, serial, owner.Direction, running != 0);


            byte group = UOFileManager.Animations.GetDieGroupIndex(owner.Graphic, running != 0, true);
            owner.SetAnimation(group, 0, 5, 1);

            if (ProfileManager.Current.AutoOpenCorpses)
                World.Player.TryOpenCorpses();
        }

        private static void OpenGump(Packet p)
        {
            if (World.Player == null)
                return;

            uint sender = p.ReadUInt();
            uint gumpID = p.ReadUInt();
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

            UIManager.Create(sender, gumpID, x, y, cmd, lines);
        }

        private static void ChatMessage(Packet p)
        {
            ushort cmd = p.ReadUShort();

            switch (cmd)
            {
                case 0x03E8: // create conference
                    p.Skip(4);
                    string channelName = p.ReadUnicode();
                    bool hasPassword = p.ReadUShort() == 0x31;
                    UOChatManager.CurrentChannelName = channelName;
                    UOChatManager.AddChannel(channelName, hasPassword);

                    UIManager.GetGump<UOChatGump>()?.Update();
                    break;
                case 0x03E9: // destroy conference
                    p.Skip(4);
                    channelName = p.ReadUnicode();
                    UOChatManager.RemoveChannel(channelName);

                    UIManager.GetGump<UOChatGump>()?.Update();
                    break;
                case 0x03EB: // display enter username window
                    break;
                case 0x03EC: // close chat
                    UOChatManager.Clear();
                    UOChatManager.ChatIsEnabled = false;
                    UIManager.GetGump<UOChatGump>()?.Dispose();
                    break;
                case 0x03ED: // username accepted, display chat
                    p.Skip(4);
                    string username = p.ReadUnicode();
                    UOChatManager.ChatIsEnabled = true;
                    NetClient.Socket.Send(new PChatJoinCommand("General"));
                    break;
                case 0x03EE: // add user
                    p.Skip(4);
                    ushort userType = p.ReadUShort();
                    username = p.ReadUnicode();
                    break;
                case 0x03EF: // remove user
                    p.Skip(4);
                    username = p.ReadUnicode();
                    break;
                case 0x03F0: // clear all players
                    break;
                case 0x03F1: // you have joined a conference
                    p.Skip(4);
                    channelName = p.ReadUnicode();
                    UOChatManager.CurrentChannelName = channelName;
                    UIManager.GetGump<UOChatGump>()?.UpdateConference();

                    GameActions.Print($"You have joined the '{channelName}' channel.", ProfileManager.Current.ChatMessageHue, MessageType.Regular, 1, true);
                    break;
                case 0x03F4:
                    p.Skip(4);
                    channelName = p.ReadUnicode();
                    GameActions.Print($"You have left the '{channelName}' channel.", ProfileManager.Current.ChatMessageHue, MessageType.Regular, 1, true);
                    break;
                case 0x0025:
                case 0x0026:
                case 0x0027:
                    p.Skip(4);
                    ushort msgType = p.ReadUShort();
                    username = p.ReadUnicode();
                    string msgSent = p.ReadUnicode();

                    if (!string.IsNullOrEmpty(msgSent))
                    {
                        int idx = msgSent.IndexOf('{');
                        int idxLast = msgSent.IndexOf('}') + 1;

                        msgSent = msgSent.Remove(idx, idxLast - idx);
                    }

                    //Color c = new Color(49, 82, 156, 0);
                    GameActions.Print($"{username}: {msgSent}", ProfileManager.Current.ChatMessageHue, MessageType.Regular, 1);

                    break;
                default:
                    if ((cmd >= 0x0001 && cmd <= 0x0024) ||
                        (cmd >= 0x0028 && cmd <= 0x002C))
                    {
                        // TODO: read Chat.enu ?
                        // http://docs.polserver.com/packets/index.php?Packet=0xB2

                        string msg = UOChatManager.GetMessage(cmd - 1);

                        if (string.IsNullOrEmpty(msg))
                            return;

                        p.Skip(4);
                        string text = p.ReadUnicode();

                        if (!string.IsNullOrEmpty(msg))
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
                        
                        GameActions.Print(msg, ProfileManager.Current.ChatMessageHue, MessageType.Regular, 1, true);
                    }
                    break;
            }

           
        }

        private static void Help(Packet p)
        {
        }

        private static void CharacterProfile(Packet p)
        {
            if (!World.InGame)
                return;

            uint serial = p.ReadUInt();
            string header = p.ReadASCII();
            string footer = p.ReadUnicode();

            string body = p.ReadUnicode();

            UIManager.GetGump<ProfileGump>(serial)?.Dispose();
            UIManager.Add(new ProfileGump(serial, header, footer, body, serial == World.Player.Serial));
        }

        private static void EnableLockedFeatures(Packet p)
        {
            uint flags = 0;

            if (Client.Version >= Data.ClientVersion.CV_60142)
                flags = p.ReadUInt();
            else
                flags = p.ReadUShort();
            World.ClientLockedFeatures.SetFlags((LockedFeatureFlags) flags);

            UOFileManager.Animations.UpdateAnimationTable(flags);
        }

        private static void DisplayQuestArrow(Packet p)
        {
            var display = p.ReadBool();
            var mx = p.ReadUShort();
            var my = p.ReadUShort();

            uint serial = 0;

            if (Client.Version >= Data.ClientVersion.CV_7090)
                serial = p.ReadUInt();

            var arrow = UIManager.GetGump<QuestArrowGump>(serial);

            if (display)
            {
                if (arrow == null)
                    UIManager.Add(new QuestArrowGump(serial, mx, my));
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
                    for (int i = 0; i < 6; i++) 
                        World.Player.Walker.FastWalkStack.SetValue(i, p.ReadUInt());
                    break;

                //===========================================================================================
                //===========================================================================================
                case 2: // add key to fast walk stack
                    World.Player.Walker.FastWalkStack.AddValue(p.ReadUInt());

                    break;

                //===========================================================================================
                //===========================================================================================
                case 4: // close generic gump 
                    uint ser = p.ReadUInt();
                    int button = (int) p.ReadUInt();

                    var gumpToClose = UIManager.Gumps.OfType<Gump>()
                                     .FirstOrDefault(s => !s.IsDisposed && s.ServerSerial == ser);

                    if (gumpToClose != null)
                    {
                        if (button != 0)
                            gumpToClose.OnButtonClick(button);
                        else
                            UIManager.SavePosition(ser, gumpToClose.Location);
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
                    UIManager.GetGump<HealthBarGump>(p.ReadUInt())?.Dispose();

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
                        str = UOFileManager.Cliloc.Translate(UOFileManager.Cliloc.GetString((int) cliloc), capitalize: true);

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
                        string attr = UOFileManager.Cliloc.GetString((int) next);

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

                    UIManager.GetGump<PopupMenuGump>()?.Dispose();

                    UIManager.Add(new PopupMenuGump(data)
                    {
                        X = DelayedObjectClickManager.X,
                        Y = DelayedObjectClickManager.Y
                    });

                    break;

                //===========================================================================================
                //===========================================================================================
                case 0x16: // close user interface windows
                    uint id = p.ReadUInt();
                    uint serial = p.ReadUInt();

                    switch (id)
                    {
                        case 1: // paperdoll
                            UIManager.GetGump<PaperDollGump>(serial)?.Dispose();

                            break;

                        case 2: //statusbar
                            UIManager.GetGump<HealthBarGump>(serial)?.Dispose();

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

                    if (UOFileManager.Map.ApplyPatches(p))
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
                    spellbook.Items.Clear();
                    ushort type = p.ReadUShort();

                    for (int j = 0; j < 2; j++)
                    {
                        uint spells = 0;

                        for (int i = 0; i < 4; i++)
                        {
                            spells |= (uint) (p.ReadByte() << (i * 8));
                        }

                        for (int i = 0; i < 32; i++)
                        {
                            if ((spells & (1 << i)) != 0)
                            {
                                ushort cc = (ushort) ((j * 32) + i + 1);

                                Item spellItem = new Item(cc)
                                {
                                    Graphic = 0x1F2E, Amount = cc, Container = spellbook
                                };
                                spellbook.Items.Add(spellItem);
                            }
                        }
                    }
                    spellbook.Items.ProcessDelta();
                    UIManager.GetGump<SpellbookGump>(spellbook)?.Update();

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
                        UIManager.GetGump<MiniMapGump>()?.ForceUpdate();
                        if (World.HouseManager.EntityIntoHouse(serial, World.Player))
                            Client.Game.GetScene<GameScene>()?.UpdateMaxDrawZ(true);
                    }

                    break;

                //===========================================================================================
                //===========================================================================================
                case 0x20:
                    serial = p.ReadUInt();
                    type = p.ReadByte();
                    ushort graphic = p.ReadUShort();
                    ushort x = p.ReadUShort();
                    ushort y = p.ReadUShort();
                    sbyte z = p.ReadSByte();

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
                                break;

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

            uint serial = p.ReadUInt();
            Entity entity = World.Get(serial);
            ushort graphic = p.ReadUShort();
            MessageType type = (MessageType) p.ReadByte();
            ushort hue = p.ReadUShort();
            ushort font = p.ReadUShort();
            uint cliloc = p.ReadUInt();
            AffixType flags = p.ID == 0xCC ? (AffixType) p.ReadByte() : 0x00;
            string name = p.ReadASCII(30);
            string affix = p.ID == 0xCC ? p.ReadASCII() : string.Empty;

            string arguments = null;
            
            if (cliloc == 1008092 || cliloc == 1005445) // value for "You notify them you don't want to join the party" || "You have been added to the party"
            {
                foreach (var PartyInviteGump in UIManager.Gumps.OfType<PartyInviteGump>())
                {
                    PartyInviteGump.Dispose();
                }
            }

            if (p.Position < p.Length)
                arguments = p.ReadUnicodeReversed(p.Length - p.Position);

            string text = UOFileManager.Cliloc.Translate((int) cliloc, arguments);

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

            if (!UOFileManager.Fonts.UnicodeFontExists((byte) font))
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

            uint serial = p.ReadUInt();

            p.Skip(2);
            uint revision = p.ReadUInt();

            Entity entity = World.Mobiles.Get(serial);

            if (entity == null)
            {
                if (SerialHelper.IsMobile(serial))
                    Log.Warn( "Searching a mobile into World.Items from MegaCliloc packet");
                entity = World.Items.Get(serial);
            }

            if (entity != null)
            {

                int cliloc;

                List<string> list = new List<string>();

                while ((cliloc = (int) p.ReadUInt()) != 0)
                {
                    string argument = p.ReadUnicodeReversed(p.ReadUShort());

                    string str = UOFileManager.Cliloc.Translate(cliloc, argument, true);


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

                if (entity is Item it && SerialHelper.IsValid(it.Container))
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

                            if (entity != null && !SerialHelper.IsMobile(entity.Serial))
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

                if (inBuyList && SerialHelper.IsValid(container.Serial))
                {
                    UIManager.GetGump<ShopGump>(container.RootContainer)?.SetNameTo((Item)entity, name);
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
            uint serial = p.ReadUInt();
            Item foundation = World.Items.Get(serial);
            uint revision = p.ReadUInt();

            if (foundation == null)
                return;

            var multi = foundation.MultiInfo;

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
                house.ClearComponents(true);
                house.Revision = revision;
                house.IsCustom = true;
            }

            short minX = (short) multi.Value.X;
            short minY = (short) multi.Value.Y;
            short maxY = (short) multi.Value.Height;

            if (minX == 0 && minY == 0 && maxY == 0 && multi.Value.Width == 0)
            {
                Log.Warn( "[CustomHouse (0xD8) - Invalid multi dimentions. Maybe missing some installation required files");
                return;
            }

            byte planes = p.ReadByte();

            DataReader stream = new DataReader();
            ref byte[] buffer = ref p.ToArray();

            RawList<CustomBuildObject> list = new RawList<CustomBuildObject>();

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
                                    list.Add(new CustomBuildObject(id){ X= x, Y = y, Z = z});
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
                                    list.Add(new CustomBuildObject(id) { X = x, Y = y, Z = z });
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
                                    list.Add(new CustomBuildObject(id) { X = x, Y = y, Z = z });
                                }
                            }

                            break;
                    }
                }
                stream.ReleaseData();

            }
            stream.ReleaseData();

            house.Fill(list);

            if (World.CustomHouseManager != null)
            {
                World.CustomHouseManager.GenerateFloorPlace();
                UIManager.GetGump<HouseCustomizationGump>(house.Serial)?.Update();
            }

            UIManager.GetGump<MiniMapGump>()?.ForceUpdate();

            if (World.HouseManager.EntityIntoHouse(serial, World.Player))
                Client.Game.GetScene<GameScene>()?.UpdateMaxDrawZ(true);
        }

        private static void CharacterTransferLog(Packet p)
        {
        }

        private static void OPLInfo(Packet p)
        {
            if (World.ClientFeatures.TooltipsEnabled)
            {
                uint serial = p.ReadUInt();
                uint revision = p.ReadUInt();

                if (!World.OPL.IsRevisionEqual(serial, revision))
                    AddMegaClilocRequest(serial);
            }
        }

        private static void OpenCompressedGump(Packet p)
        {
            uint sender = p.ReadUInt();
            uint gumpID = p.ReadUInt();
            uint x = p.ReadUInt();
            uint y = p.ReadUInt();
            uint clen = p.ReadUInt() - 4;
            int dlen = (int) p.ReadUInt();
            byte[] decData = new byte[dlen];
            string layout;

            ref var buffer = ref p.ToArray();

            unsafe
            {

                fixed (byte* srcPtr = &buffer[p.Position], destPtr = decData)
                {
                    ZLib.Decompress((IntPtr) srcPtr, (int) clen, 0, (IntPtr) destPtr, dlen);
                    layout = Encoding.UTF8.GetString(destPtr, dlen);
                }
            }

            p.Skip((int)clen);

            uint linesNum = p.ReadUInt();
            string[] lines = new string[linesNum];

            if (linesNum > 0)
            {
                clen = p.ReadUInt() - 4;
                dlen = (int) p.ReadUInt();
                decData = new byte[dlen];

                unsafe
                {
                    fixed (byte* srcPtr = &buffer[p.Position], destPtr = decData)
                        ZLib.Decompress((IntPtr)srcPtr, (int)clen, 0, (IntPtr)destPtr, dlen);
                }

                p.Skip((int) clen);

                for (int i = 0, index = 0; i < linesNum; i++)
                {
                    int length = ((decData[index++] << 8) | decData[index++]) << 1;
                    lines[i] = Encoding.BigEndianUnicode.GetString(decData, index, length);
                    index += length;

                }
            }

            UIManager.Create(sender, gumpID, (int) x, (int) y, layout, lines);
        }

        private static void UpdateMobileStatus(Packet p)
        {
        }

        private static void BuffDebuff(Packet p)
        {
            if (World.Player == null)
                return;

            const ushort BUFF_ICON_START = 0x03E9;
            const ushort BUFF_ICON_START_NEW = 0x466;

            uint serial = p.ReadUInt();
            ushort ic = p.ReadUShort();
            ushort iconID = ic >= BUFF_ICON_START_NEW ? (ushort) (ic - (BUFF_ICON_START_NEW - 125)) : (ushort) (ic - BUFF_ICON_START);

            if (iconID < BuffTable.Table.Length)
            {
                BuffGump gump = UIManager.GetGump<BuffGump>();
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
                    string title = UOFileManager.Cliloc.GetString((int) titleCliloc);
                    string description = string.Empty;
                    string wtf = string.Empty;

                    if (descriptionCliloc != 0)
                    {
                        string args = p.ReadUnicodeReversed();
                        description = "\n" + UOFileManager.Cliloc.Translate((int) descriptionCliloc, args, true);

                        if (description.Length < 2)
                            description = string.Empty;
                    }

                    if (wtfCliloc != 0)
                    {
                        wtf = UOFileManager.Cliloc.GetString((int) wtfCliloc);
                        if (!string.IsNullOrEmpty(wtf))
                            wtf = $"\n{wtf}";
                    }

                    string text = $"<left>{title}{description}{wtf}</left>";
                    bool alreadyExists = World.Player.IsBuffIconExists(BuffTable.Table[iconID]);
                    World.Player.AddBuff(BuffTable.Table[iconID], timer, text);
                    if (!alreadyExists)
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

            switch (type)
            {
                case 0x01: // custom party info
                case 0x02: // guild track info
                    bool locations = type == 0x01 || p.ReadBool();

                    uint serial;

                    while((serial = p.ReadUInt()) != 0)
                    {
                        if (locations)
                        {
                            ushort x = p.ReadUShort();
                            ushort y = p.ReadUShort();
                            byte map = p.ReadByte();
                            byte hits = p.ReadByte();

                            World.WMapManager.AddOrUpdate(serial, x, y, hits, map, type == 0x02);

                            //Log.Info($"Received custom {(isparty ? "party" : "guild")} member info: X: {x}, Y: {y}, Map: {map}, Hits: {hits}");
                        }
                    }

                    World.WMapManager.RemoveUnupdatedWEntity();

                    break;
                case 0xF0:
                    break;
                case 0xFE:
                    Log.Info("Razor ACK sent");
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
            uint serial = p.ReadUInt();
            ushort graphic = p.ReadUShort();
            byte graphicInc = p.ReadByte();
            ushort amount = p.ReadUShort();
            p.Skip(2);
            ushort x = p.ReadUShort();
            ushort y = p.ReadUShort();
            sbyte z = p.ReadSByte();
            Direction dir = (Direction) p.ReadByte();
            ushort hue = p.ReadUShort();
            Flags flags = (Flags) p.ReadByte();
            p.Skip(2);

            if (serial != World.Player)
            {
                if (SerialHelper.IsItem(serial))
                {
                    Item item = World.GetOrCreateItem(serial);
                    item.Amount = amount;
                    item.Direction = dir;
                    item.LightID = (byte) dir;
                    item.FixHue(hue);
                    item.Flags = flags;
                    item.Container = 0;

                    if (graphic != 0x2006)
                        graphic += graphicInc;
                    else if (!item.IsClicked && ProfileManager.Current.ShowNewCorpseNameIncoming) GameActions.SingleClick(item);

                    if (graphic == 0x2006 && ProfileManager.Current.AutoOpenCorpses) World.Player.TryOpenCorpses();

                    if (type == 0x02)
                    {
                        item.IsMulti = true;
                        item.WantUpdateMulti = (graphic & 0x3FFF) != item.Graphic || (item.X != x || item.Y != y || item.Z != z);
                        item.Graphic = (ushort) (graphic & 0x3FFF);
                    }
                    else
                    {
                        item.IsDamageable = type == 0x03;
                        item.IsMulti = false;
                        item.Graphic = graphic;
                    }

                    item.X = x;
                    item.Y = y;
                    item.Z = z;
                    item.UpdateScreenPosition();
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

                    if (World.Get(mobile) == null || (mobile.X == 0xFFFF && mobile.Y == 0xFFFF))
                    {
                        mobile.X = x;
                        mobile.Y = y;
                        mobile.Z = z;
                        mobile.UpdateScreenPosition();
                        mobile.Direction = direction;
                        mobile.IsRunning = isrun;
                        mobile.AddToTile();
                    }

                    if (!mobile.EnqueueStep(x, y, z, direction, isrun))
                    {
                        mobile.X = x;
                        mobile.Y = y;
                        mobile.Z = z;
                        mobile.UpdateScreenPosition();
                        mobile.Direction = direction;
                        mobile.IsRunning = isrun;
                        mobile.ClearSteps();
                        mobile.AddToTile();
                    }
                }
            }
            else if (p.ID == 0xF7)
            {

                ushort oldGraphic = World.Player.Graphic;
                bool oldDead = World.Player.IsDead;

                World.Player.X = x;
                World.Player.Y = y;
                World.Player.Z = z;
                World.Player.UpdateScreenPosition();
                World.RangeSize.X = x;
                World.RangeSize.Y = y;
                World.Player.Graphic = graphic;
                World.Player.Direction = dir;
                World.Player.FixHue(hue);
                World.Player.Flags = flags;

                World.Player.CloseBank();
                World.Player.Walker.WalkingFailed = false;
                World.Player.X = x;
                World.Player.Y = y;
                World.Player.Z = z;
                World.Player.UpdateScreenPosition();
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

                World.Player.Walker.ResendPacketResync = false;
                World.Player.AddToTile();
                World.Player.ProcessDelta();

                var scene = Client.Game.GetScene<GameScene>();

                if (scene != null)
                    scene.UpdateDrawPosition = true;

                World.Player.CloseRangedGumps();
            }
        }

        private static void BoatMoving(Packet p)
        {
            if (!World.InGame)
                return;

            uint serial = p.ReadUInt();
            byte boatSpeed = p.ReadByte();
            Direction movingDirection = (Direction) p.ReadByte();
            Direction facingDirection = (Direction) p.ReadByte();
            ushort x = p.ReadUShort();
            ushort y = p.ReadUShort();
            ushort z = p.ReadUShort();

            Item item = World.Items.Get(serial);

            if (item == null)
                return;

            item.X = x;
            item.Y = y;
            item.Z = (sbyte) z;
            item.UpdateScreenPosition();
            item.AddToTile();
            //item.Graphic += (byte) facingDirection;
            //item.WantUpdateMulti = true;
            //item.CheckGraphicChange();
            if (World.HouseManager.TryGetHouse(item, out House house))
                house.Generate(true);


            int count = p.ReadUShort();

            for (int i = 0; i < count; i++)
            {
                uint cSerial = p.ReadUInt();
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

                    entity.X = cx;
                    entity.Y = cy;
                    entity.Z = (sbyte) cz;
                    entity.UpdateScreenPosition();
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
                    Log.Warn( $"Unknown packet ID: [0x{id:X2}] in 0xF7");

                    break;
                }
            }
        }

        private static void AddItemToContainer(uint serial, ushort graphic, ushort amount, ushort x, ushort y, ushort hue, uint containerSerial)
        {
            GameScene gs = Client.Game.GetScene<GameScene>();

            if (gs != null && gs.HeldItem.Serial == serial && gs.HeldItem.Dropped)
                gs.HeldItem.Clear();

            Entity container = World.Get(containerSerial);

            if (container == null)
            {
                Log.Warn( $"No container ({containerSerial}) found");

                return;
            }

            Item item = World.Items.Get(serial);

            if (SerialHelper.IsMobile(serial)) Log.Warn( "AddItemToContainer function adds mobile as Item");

            if (item != null && (container.Graphic != 0x2006 || item.Layer == Layer.Invalid))
            {
                UIManager.GetGump(item.Serial)?.Dispose();

                item.Destroy();

                Entity initcontainer = World.Get(item.Container);

                if (initcontainer != null)
                {
                    item.Container = 0;
                    initcontainer.Items.Remove(item);
                    initcontainer.ProcessDelta();
                }
                else if (SerialHelper.IsValid(item.Container)) 
                    Log.Warn( $"This item ({item.Serial}) has a container ({item.Container}), but cannot be found. :|");

                World.Items.Remove(item);
                World.Items.ProcessDelta();
            }

            item = World.GetOrCreateItem(serial);
            item.Graphic = graphic;
            item.Amount = amount;
            item.FixHue(hue);
            item.X = x;
            item.Y = y;
            item.Z = 0;

            // FIXME: not really needed here
            //item.UpdateScreenPosition(); 

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