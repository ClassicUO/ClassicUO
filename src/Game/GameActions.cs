﻿#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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
using ClassicUO.Configuration;
using ClassicUO.Data;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Utility;

using Microsoft.Xna.Framework;

using static ClassicUO.Network.NetClient;

namespace ClassicUO.Game
{
    internal static class GameActions
    {
        public static int LastSpellIndex { get; set; } = 1;
        public static int LastSkillIndex { get; set; } = 1;


        public static void ChangeWarMode(byte status = 0xFF)
        {
            bool newStatus = !World.Player.InWarMode;

            if (status != 0xFF)
            {
                bool ok = status != 0;

                if (World.Player.InWarMode == ok)
                    return;

                newStatus = ok;
            }

            //if (ProfileManager.Current != null && ProfileManager.Current.EnableCombatMusic)
            {
                if (newStatus && ProfileManager.Current != null && ProfileManager.Current.EnableMusic)
                {
                    Client.Game.Scene.Audio.PlayMusic((RandomHelper.GetValue(0, 3) % 3) + 38, true);
                }
                else if (!newStatus)
                {
                    Client.Game.Scene.Audio.StopWarMusic();
                }
            }
            
            Socket.Send(new PChangeWarMode(newStatus));
        }

        public static void OpenPaperdoll(uint serial)
        {
            DoubleClick(serial | 0x80000000);
        }

        public static bool OpenCorpse(uint serial)
        {
            if (!SerialHelper.IsItem(serial)) return false;

            Item item = World.Items.Get(serial);
            if (item == null || !item.IsCorpse || item.IsDestroyed) return false;

            World.Player.ManualOpenedCorpses.Add(serial);
            DoubleClick(serial);

            return true;
        }

        public static void Attack(uint serial)
        {
            if (ProfileManager.Current.EnabledCriminalActionQuery)
            {
                Mobile m = World.Mobiles.Get(serial);

                if (m != null && (World.Player.NotorietyFlag == NotorietyFlag.Innocent || World.Player.NotorietyFlag == NotorietyFlag.Ally) && m.NotorietyFlag == NotorietyFlag.Innocent && m != World.Player)
                {
                    QuestionGump messageBox = new QuestionGump("This may flag\nyou criminal!",
                                                               s =>
                                                               {
                                                                   if (s)
                                                                       Socket.Send(new PAttackRequest(serial));
                                                               });

                    UIManager.Add(messageBox);

                    return;
                }
            }

            TargetManager.LastAttack = serial;
            Socket.Send(new PAttackRequest(serial));
        }

        public static void DoubleClickQueued(uint serial)
        {
            Client.Game.GetScene<GameScene>()?.DoubleClickDelayed(serial);
        }

        public static void DoubleClick(uint serial)
        {
            if (serial != World.Player && SerialHelper.IsMobile(serial) && World.Player.InWarMode)
            {
                RequestMobileStatus(serial);
                Attack(serial);
            }
            else
            {
                Socket.Send(new PDoubleClickRequest(serial));
            }

            if (SerialHelper.IsItem(serial))
                World.LastObject = serial;
            else
                World.LastObject = 0;
        }

        public static void SingleClick(uint serial)
        {
            // add  request context menu
            Socket.Send(new PClickRequest(serial));

            Entity entity = World.Get(serial);

            if (entity != null)
                entity.IsClicked = true;
        }

        public static void Say(string message, ushort hue = 0xFFFF, MessageType type = MessageType.Regular, byte font = 3)
        {
            if (hue == 0xFFFF)
                hue = ProfileManager.Current.SpeechHue;

            if (Client.Version >= ClientVersion.CV_500A)
                Socket.Send(new PUnicodeSpeechRequest(message, type, font, hue, "ENU"));
            else
                Socket.Send(new PASCIISpeechRequest(message, type, font, hue));
        }


        public static void Print(string message, ushort hue = 946, MessageType type = MessageType.Regular, byte font = 3, bool unicode = true)
        {
            Print(null, message, hue, type, font, unicode);
        }

        public static void Print(Entity entity, string message, ushort hue = 946, MessageType type = MessageType.Regular, byte font = 3, bool unicode = true)
        {
            MessageManager.HandleMessage(entity, message, entity != null ? entity.Name : "System", hue, type, font, entity == null ? TEXT_TYPE.SYSTEM : TEXT_TYPE.OBJECT, unicode, "ENU");
        }

        public static void SayParty(string message, uint serial = 0)
        {
            Socket.Send(new PPartyMessage(message, serial));
        }

        public static void RequestPartyAccept(uint serial)
        {
            Socket.Send(new PPartyAccept(serial));

            UIManager.GetGump<PartyInviteGump>()?.Dispose();
        }

        public static void RequestPartyRemoveMember(uint serial)
        {
            Socket.Send(new PPartyRemoveRequest(serial));
        }

        public static void RequestPartyQuit()
        {
            Socket.Send(new PPartyRemoveRequest(World.Player));
        }

        public static void RequestPartyInviteByTarget()
        {
            Socket.Send(new PPartyInviteRequest());
        }

        public static void RequestPartyLootState(bool isLootable)
        {
            Socket.Send(new PPartyChangeLootTypeRequest(isLootable));
        }

        public static bool PickUp(uint serial, int x, int y, int amount = -1, Point? offset = null)
        {
            if (World.Player.IsDead || ItemHold.Enabled)
                return false;

            Item item = World.Items.Get(serial);

            if (item == null || item.IsDestroyed || item.IsMulti || item.OnGround && (item.IsLocked || item.Distance > Constants.DRAG_ITEMS_DISTANCE))
                return false;

            if (amount <= -1 && item.Amount > 1 && item.ItemData.IsStackable)
            {
                if (ProfileManager.Current.HoldShiftToSplitStack == Keyboard.Shift)
                {
                    SplitMenuGump gump = UIManager.GetGump<SplitMenuGump>(item);

                    if (gump != null)
                    {
                        return false;
                    }

                    gump = new SplitMenuGump(item, new Point(x, y))
                    {
                        X = Mouse.LDropPosition.X - 80,
                        Y = Mouse.LDropPosition.Y - 40
                    };
                    UIManager.Add(gump);
                    UIManager.AttemptDragControl(gump, Mouse.Position, true);

                    return true;
                }
            }
            
            if (amount <= 0)
                amount = item.Amount;

            ItemHold.Clear();
            ItemHold.Set(item, (ushort) amount, offset);
            Socket.Send(new PPickUpRequest(item, (ushort) amount));
            UIManager.GameCursor.SetDraggedItem(offset);

            if (item.OnGround)
            {
                item.RemoveFromTile();
            }

            item.TextContainer?.Clear();

            World.ObjectToRemove = item.Serial;

            return true;
        }

        public static void DropItem(uint serial, int x, int y, int z, uint container)
        {
            if (ItemHold.Enabled && !ItemHold.IsFixedPosition && (ItemHold.Serial != container || ItemHold.ItemData.IsStackable))
            {
                if (Client.Version >= ClientVersion.CV_6017)
                    Socket.Send(new PDropRequestNew(serial, (ushort) x, (ushort) y, (sbyte) z, 0, container));
                else
                    Socket.Send(new PDropRequestOld(serial, (ushort) x, (ushort) y, (sbyte) z, container));

                ItemHold.Enabled = false;
                ItemHold.Dropped = true;
            }
        }

        public static void Equip(uint container = 0)
        {
            if (ItemHold.Enabled && !ItemHold.IsFixedPosition && ItemHold.ItemData.IsWearable)
            {
                if (!SerialHelper.IsValid(container))
                {
                    container = World.Player.Serial;
                }

                Socket.Send(new PEquipRequest(ItemHold.Serial, (Layer) ItemHold.ItemData.Layer, container));
               
                ItemHold.Enabled = false;
                ItemHold.Dropped = true;
            }
        }

        public static void ReplyGump(uint local, uint server, int button, uint[] switches = null, Tuple<ushort, string>[] entries = null)
        {
            Socket.Send(new PGumpResponse(local, server, button, switches, entries));
        }

        public static void RequestHelp()
        {
            Socket.Send(new PHelpRequest());
        }

        public static void RequestQuestMenu()
        {
            Socket.Send(new PQuestMenuRequest());
        }

        public static void RequestProfile(uint serial)
        {
            Socket.Send(new PProfileRequest(serial));
        }

        public static void ChangeSkillLockStatus(ushort skillindex, byte lockstate)
        {
            Socket.Send(new PSkillsStatusChangeRequest(skillindex, lockstate));
        }

        public static void RequestMobileStatus(uint serial)
        {
            //Mobile mob = World.Mobiles.Get(serial);
            //if (mob != null)
            //{
            //    mob.AddMessage(MessageType.Regular, "[PACKET REQUESTED]");
            //}
            Socket.Send(new PStatusRequest(serial));
        }

        public static void CastSpellFromBook(int index, uint bookSerial)
        {
            if (index >= 0)
            {
                LastSpellIndex = index;
                Socket.Send(new PCastSpellFromBook(index, bookSerial));
            }
        }

        public static void CastSpell(int index)
        {
            if (index >= 0)
            {
                LastSpellIndex = index;
                Socket.Send(new PCastSpell(index));
            }
        }

        public static void OpenGuildGump()
        {
            Socket.Send(new PGuildMenuRequest());
        }

        public static void ChangeStatLock(byte stat, Lock state)
        {
            Socket.Send(new PChangeStatLockStateRequest(stat, state));
        }

        public static void Rename(uint serial, string name)
        {
            Socket.Send(new PRenameRequest(serial, name));
        }

        public static void UseSkill(int index)
        {
            if (index >= 0)
            {
                LastSkillIndex = index;
                Socket.Send(new PUseSkill(index));
            }
        }

        public static void OpenPopupMenu(uint serial, bool shift = false)
        {
            shift = shift || Input.Keyboard.Shift;

            if (ProfileManager.Current.HoldShiftForContext && !shift)
                return;

            Socket.Send(new PRequestPopupMenu(serial));
        }

        public static void ResponsePopupMenu(uint serial, ushort index)
        {
            Socket.Send(new PPopupMenuSelection(serial, index));
        }

        public static void MessageOverhead(string message, uint entity)
        {
            Print(World.Get(entity), message);
        }

        public static void MessageOverhead(string message, ushort hue, uint entity)
        {
            Print(World.Get(entity), message, hue);
        }

        public static void AcceptTrade(uint serial, bool accepted)
        {
            Socket.Send(new PTradeResponse(serial, 2, accepted));
        }

        public static void CancelTrade(uint serial)
        {
            Socket.Send(new PTradeResponse(serial, 1, false));
        }

        public static void AllNames()
        {
            foreach (Mobile mobile in World.Mobiles)
                if (mobile != World.Player)
                    Socket.Send(new PClickRequest(mobile));

            foreach (Item item in World.Items)
            {
                if (item.IsCorpse)
                    Socket.Send(new PClickRequest(item));
            }
        }

        public static void OpenDoor()
        {
            Socket.Send(new POpenDoor());
        }

        public static void EmoteAction(string action)
        {
            Socket.Send(new PEmoteAction(action));
        }

        public static void OpenAbilitiesBook()
        {
            if (UIManager.GetGump<CombatBookGump>() == null) UIManager.Add(new CombatBookGump(100, 100));
        }

        public static void UsePrimaryAbility()
        {
            ref Ability ability = ref World.Player.Abilities[0];

            if (((byte) ability & 0x80) == 0)
            {
                for (int i = 0; i < 2; i++)
                    World.Player.Abilities[i] &= (Ability) 0x7F;
                Socket.Send(new PUseCombatAbility((byte) ability));
            }
            else
                Socket.Send(new PUseCombatAbility(0));

            ability ^= (Ability) 0x80;
        }

        public static void UseSecondaryAbility()
        {
            ref Ability ability = ref World.Player.Abilities[1];

            if (((byte) ability & 0x80) == 0)
            {
                for (int i = 0; i < 2; i++)
                    World.Player.Abilities[i] &= (Ability) 0x7F;
                Socket.Send(new PUseCombatAbility((byte) ability));
            }
            else
                Socket.Send(new PUseCombatAbility(0));

            ability ^= (Ability) 0x80;
        }

        public static void QuestArrow(bool rightClick)
        {
            Socket.Send(new PClickQuestArrow(rightClick));
        }

        public static void GrabItem(uint serial, ushort amount, uint bag = 0)
        {
            //Socket.Send(new PPickUpRequest(serial, amount));

            Item backpack = World.Player.FindItemByLayer(Layer.Backpack);

            if (backpack == null)
                return;

            if(bag == 0)
                bag = ProfileManager.Current.GrabBagSerial == 0
                    ? backpack.Serial
                    : ProfileManager.Current.GrabBagSerial;

            if (!World.Items.Contains(bag))
            {
                Print("Grab Bag not found, setting to Backpack.");
                ProfileManager.Current.GrabBagSerial = 0;
                bag = backpack.Serial;
            }

            PickUp(serial, 0, 0, amount);
            DropItem(serial, 0xFFFF, 0xFFFF, 0, bag);
        }
    }
}