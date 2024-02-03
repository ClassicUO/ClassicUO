#region license

// Copyright (c) 2024, andreakarasho
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
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Resources;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using static ClassicUO.Network.NetClient;

namespace ClassicUO.Game
{
    internal static class GameActions
    {
        public static int LastSpellIndex { get; set; } = 1;
        public static int LastSkillIndex { get; set; } = 1;


        public static void ToggleWarMode(PlayerMobile player)
        {
            RequestWarMode(player, !player.InWarMode);
        }

        public static void RequestWarMode(PlayerMobile player, bool war)
        {
            if (!player.IsDead)
            {
                if (war && ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.EnableMusic)
                {
                    Client.Game.Audio.PlayMusic((RandomHelper.GetValue(0, 3) % 3) + 38, true);
                }
                else if (!war)
                {
                    Client.Game.Audio.StopWarMusic();
                }
            }

            Socket.Send_ChangeWarMode(war);
        }

        public static void OpenMacroGump(World world, string name)
        {
            MacroGump macroGump = UIManager.GetGump<MacroGump>();

            macroGump?.Dispose();
            UIManager.Add(new MacroGump(world, name));
        }

        public static void OpenPaperdoll(World world, uint serial)
        {
            PaperDollGump paperDollGump = UIManager.GetGump<PaperDollGump>(serial);

            if (paperDollGump == null)
            {
                DoubleClick(world, serial | 0x80000000);
            }
            else
            {
                if (paperDollGump.IsMinimized)
                {
                    paperDollGump.IsMinimized = false;
                }

                paperDollGump.SetInScreen();
                paperDollGump.BringOnTop();
            }
        }

        public static void OpenSettings(World world, int page = 0)
        {
            OptionsGump opt = UIManager.GetGump<OptionsGump>();

            if (opt == null)
            {
                OptionsGump optionsGump = new OptionsGump(world)
                {
                    X = (Client.Game.Window.ClientBounds.Width >> 1) - 300,
                    Y = (Client.Game.Window.ClientBounds.Height >> 1) - 250
                };

                UIManager.Add(optionsGump);
                optionsGump.ChangePage(page);
                optionsGump.SetInScreen();
            }
            else
            {
                opt.SetInScreen();
                opt.BringOnTop();
            }
        }

        public static void OpenStatusBar(World world)
        {
            Client.Game.Audio.StopWarMusic();

            if (StatusGumpBase.GetStatusGump() == null)
            {
                UIManager.Add(StatusGumpBase.AddStatusGump(world, 100, 100));
            }
        }

        public static void OpenJournal(World world)
        {
            JournalGump journalGump = UIManager.GetGump<JournalGump>();

            if (journalGump == null)
            {
                UIManager.Add(new JournalGump(world) { X = 64, Y = 64 });
            }
            else
            {
                journalGump.SetInScreen();
                journalGump.BringOnTop();

                if (journalGump.IsMinimized)
                {
                    journalGump.IsMinimized = false;
                }
            }
        }

        public static void OpenSkills(World world)
        {
            StandardSkillsGump skillsGump = UIManager.GetGump<StandardSkillsGump>();

            if (skillsGump != null && skillsGump.IsMinimized)
            {
                skillsGump.IsMinimized = false;
            }
            else
            {
                world.SkillsRequested = true;
                Socket.Send_SkillsRequest(world.Player.Serial);
            }
        }

        public static void OpenMiniMap(World world)
        {
            MiniMapGump miniMapGump = UIManager.GetGump<MiniMapGump>();

            if (miniMapGump == null)
            {
                UIManager.Add(new MiniMapGump(world));
            }
            else
            {
                miniMapGump.ToggleSize();
                miniMapGump.SetInScreen();
                miniMapGump.BringOnTop();
            }
        }

        public static void OpenWorldMap(World world)
        {
            WorldMapGump worldMap = UIManager.GetGump<WorldMapGump>();

            if (worldMap == null || worldMap.IsDisposed)
            {
                worldMap = new WorldMapGump(world);
                UIManager.Add(worldMap);
            }
            else
            {
                worldMap.BringOnTop();
                worldMap.SetInScreen();
            }
        }

        public static void OpenChat(World world)
        {
            if (world.ChatManager.ChatIsEnabled == ChatStatus.Enabled)
            {
                ChatGump chatGump = UIManager.GetGump<ChatGump>();

                if (chatGump == null)
                {
                    UIManager.Add(new ChatGump(world));
                }
                else
                {
                    chatGump.SetInScreen();
                    chatGump.BringOnTop();
                }
            }
            else if (world.ChatManager.ChatIsEnabled == ChatStatus.EnabledUserRequest)
            {
                ChatGumpChooseName chatGump = UIManager.GetGump<ChatGumpChooseName>();

                if (chatGump == null)
                {
                    UIManager.Add(new ChatGumpChooseName(world));
                }
                else
                {
                    chatGump.SetInScreen();
                    chatGump.BringOnTop();
                }
            }
        }

        public static bool OpenCorpse(World world, uint serial)
        {
            if (!SerialHelper.IsItem(serial))
            {
                return false;
            }

            Item item = world.Items.Get(serial);

            if (item == null || !item.IsCorpse || item.IsDestroyed)
            {
                return false;
            }

            world.Player.ManualOpenedCorpses.Add(serial);
            DoubleClick(world, serial);

            return true;
        }

        public static bool OpenBackpack(World world)
        {
            Item backpack = world.Player.FindItemByLayer(Layer.Backpack);

            if (backpack == null)
            {
                return false;
            }

            ContainerGump backpackGump = UIManager.GetGump<ContainerGump>(backpack);

            if (backpackGump == null)
            {
                DoubleClick(world,backpack);
            }
            else
            {
                if (backpackGump.IsMinimized)
                {
                    backpackGump.IsMinimized = false;
                }

                backpackGump.SetInScreen();
                backpackGump.BringOnTop();
            }

            return true;
        }

        public static void Attack(World world, uint serial)
        {
            if (ProfileManager.CurrentProfile.EnabledCriminalActionQuery)
            {
                Mobile m = world.Mobiles.Get(serial);

                if (m != null && (world.Player.NotorietyFlag == NotorietyFlag.Innocent || world.Player.NotorietyFlag == NotorietyFlag.Ally) && m.NotorietyFlag == NotorietyFlag.Innocent && m != world.Player)
                {
                    QuestionGump messageBox = new QuestionGump
                    (
                        world,
                        ResGeneral.ThisMayFlagYouCriminal,
                        s =>
                        {
                            if (s)
                            {
                                Socket.Send_AttackRequest(serial);
                            }
                        }
                    );

                    UIManager.Add(messageBox);

                    return;
                }
            }

            world.TargetManager.LastAttack = serial;
            Socket.Send_AttackRequest(serial);
        }

        public static void DoubleClickQueued(uint serial)
        {
            Client.Game.GetScene<GameScene>()?.DoubleClickDelayed(serial);
        }

        public static void DoubleClick(World world, uint serial)
        {
            if (serial != world.Player && SerialHelper.IsMobile(serial) && world.Player.InWarMode)
            {
                RequestMobileStatus(world,serial);
                Attack(world, serial);
            }
            else
            {
                Socket.Send_DoubleClick(serial);
            }

            if (SerialHelper.IsItem(serial) || (SerialHelper.IsMobile(serial) && (world.Mobiles.Get(serial)?.IsHuman ?? false)))
            {
                world.LastObject = serial;
            }
            else
            {
                world.LastObject = 0;
            }
        }

        public static void SingleClick(World world, uint serial)
        {
            // add  request context menu
            Socket.Send_ClickRequest(serial);

            Entity entity = world.Get(serial);

            if (entity != null)
            {
                entity.IsClicked = true;
            }
        }

        public static void Say(string message, ushort hue = 0xFFFF, MessageType type = MessageType.Regular, byte font = 3)
        {
            if (hue == 0xFFFF)
            {
                hue = ProfileManager.CurrentProfile.SpeechHue;
            }

            // TODO: identify what means 'older client' that uses ASCIISpeechRquest [0x03]
            //
            // Fix -> #1267
            if (Client.Game.UO.Version >= ClientVersion.CV_200)
            {
                Socket.Send_UnicodeSpeechRequest(message,
                                                 type,
                                                 font,
                                                 hue,
                                                 Settings.GlobalSettings.Language);
            }
            else
            {
                Socket.Send_ASCIISpeechRequest(message, type, font, hue);
            }
        }


        public static void Print(World world, string message, ushort hue = 946, MessageType type = MessageType.Regular, byte font = 3, bool unicode = true)
        {
            Print
            (
                world,
                null,
                message,
                hue,
                type,
                font,
                unicode
            );
        }

        public static void Print
        (
            World world,
            Entity entity,
            string message,
            ushort hue = 946,
            MessageType type = MessageType.Regular,
            byte font = 3,
            bool unicode = true
        )
        {
            world.MessageManager.HandleMessage
            (
                entity,
                message,
                entity != null ? entity.Name : "System",
                hue,
                type,
                font,
                entity == null ? TextType.SYSTEM : TextType.OBJECT,
                unicode,
                Settings.GlobalSettings.Language
            );
        }

        public static void SayParty(string message, uint serial = 0)
        {
            Socket.Send_PartyMessage(message, serial);
        }

        public static void RequestPartyAccept(uint serial)
        {
            Socket.Send_PartyAccept(serial);

            UIManager.GetGump<PartyInviteGump>()?.Dispose();
        }

        public static void RequestPartyRemoveMemberByTarget()
        {
            Socket.Send_PartyRemoveRequest(0x00);
        }

        public static void RequestPartyRemoveMember(uint serial)
        {
            Socket.Send_PartyRemoveRequest(serial);
        }

        public static void RequestPartyQuit(PlayerMobile player)
        {
            Socket.Send_PartyRemoveRequest(player.Serial);
        }

        public static void RequestPartyInviteByTarget()
        {
            Socket.Send_PartyInviteRequest();
        }

        public static void RequestPartyLootState(bool isLootable)
        {
            Socket.Send_PartyChangeLootTypeRequest(isLootable);
        }

        public static bool PickUp
        (
            World world,
            uint serial,
            int x,
            int y,
            int amount = -1,
            Point? offset = null,
            bool is_gump = false
        )
        {
            if (world.Player.IsDead || Client.Game.UO.GameCursor.ItemHold.Enabled)
            {
                return false;
            }

            Item item = world.Items.Get(serial);

            if (item == null || item.IsDestroyed || item.IsMulti || item.OnGround && (item.IsLocked || item.Distance > Constants.DRAG_ITEMS_DISTANCE))
            {
                return false;
            }

            if (amount <= -1 && item.Amount > 1 && item.ItemData.IsStackable)
            {
                if (ProfileManager.CurrentProfile.HoldShiftToSplitStack == Keyboard.Shift)
                {
                    SplitMenuGump gump = UIManager.GetGump<SplitMenuGump>(item);

                    if (gump != null)
                    {
                        return false;
                    }

                    gump = new SplitMenuGump(world, item, new Point(x, y))
                    {
                        X = Mouse.Position.X - 80,
                        Y = Mouse.Position.Y - 40
                    };

                    UIManager.Add(gump);
                    UIManager.AttemptDragControl(gump, true);

                    return true;
                }
            }

            if (amount <= 0)
            {
                amount = item.Amount;
            }

            Client.Game.UO.GameCursor.ItemHold.Clear();
            Client.Game.UO.GameCursor.ItemHold.Set(item, (ushort) amount, offset);
            Client.Game.UO.GameCursor.ItemHold.IsGumpTexture = is_gump;
            Socket.Send_PickUpRequest(item, (ushort) amount);

            if (item.OnGround)
            {
                item.RemoveFromTile();
            }

            item.TextContainer?.Clear();

            world.ObjectToRemove = item.Serial;

            return true;
        }

        public static void DropItem(uint serial, int x, int y, int z, uint container)
        {
            if (Client.Game.UO.GameCursor.ItemHold.Enabled && !Client.Game.UO.GameCursor.ItemHold.IsFixedPosition && (Client.Game.UO.GameCursor.ItemHold.Serial != container || Client.Game.UO.GameCursor.ItemHold.ItemData.IsStackable))
            {
                if (Client.Game.UO.Version >= ClientVersion.CV_6017)
                {
                    Socket.Send_DropRequest(serial,
                                            (ushort)x,
                                            (ushort)y,
                                            (sbyte)z,
                                            0,
                                            container);
                }
                else
                {
                    Socket.Send_DropRequest_Old(serial,
                                                (ushort)x,
                                                (ushort)y,
                                                (sbyte)z,
                                                container);
                }

                Client.Game.UO.GameCursor.ItemHold.Enabled = false;
                Client.Game.UO.GameCursor.ItemHold.Dropped = true;
            }
        }

        public static void Equip(World world, uint container = 0)
        {
            if (Client.Game.UO.GameCursor.ItemHold.Enabled && !Client.Game.UO.GameCursor.ItemHold.IsFixedPosition && Client.Game.UO.GameCursor.ItemHold.ItemData.IsWearable)
            {
                if (!SerialHelper.IsValid(container))
                {
                    container = world.Player.Serial;
                }

                Socket.Send_EquipRequest(Client.Game.UO.GameCursor.ItemHold.Serial, (Layer)Client.Game.UO.GameCursor.ItemHold.ItemData.Layer, container);

                Client.Game.UO.GameCursor.ItemHold.Enabled = false;
                Client.Game.UO.GameCursor.ItemHold.Dropped = true;
            }
        }

        public static void ReplyGump(uint local, uint server, int button, uint[] switches = null, Tuple<ushort, string>[] entries = null)
        {
            Socket.Send_GumpResponse(local,
                                     server,
                                     button,
                                     switches,
                                     entries);
        }

        public static void RequestHelp()
        {
            Socket.Send_HelpRequest();
        }

        public static void RequestQuestMenu(World world)
        {
            Socket.Send_QuestMenuRequest(world);
        }

        public static void RequestProfile(uint serial)
        {
            Socket.Send_ProfileRequest(serial);
        }

        public static void ChangeSkillLockStatus(ushort skillindex, byte lockstate)
        {
            Socket.Send_SkillStatusChangeRequest(skillindex, lockstate);
        }

        public static void RequestMobileStatus(World world, uint serial, bool force = false)
        {
            if (world.InGame)
            {
                Entity ent = world.Get(serial);

                if (ent != null)
                {
                    if (force)
                    {
                        if (ent.HitsRequest >= HitsRequestStatus.Pending)
                        {
                            SendCloseStatus(world, serial);
                        }
                    }

                    if (ent.HitsRequest < HitsRequestStatus.Received)
                    {
                        ent.HitsRequest = HitsRequestStatus.Pending;
                        force = true;
                    }
                }

                if (force && SerialHelper.IsValid(serial))
                {
                    //ent = ent ?? World.Player;
                    //ent.AddMessage(MessageType.Regular, $"PACKET SENT: 0x{serial:X8}", 3, 0x34, true, TextType.OBJECT);
                    Socket.Send_StatusRequest(serial);
                }
            }
        }

        public static void SendCloseStatus(World world, uint serial, bool force = false)
        {
            if (Client.Game.UO.Version >= ClientVersion.CV_200 && world.InGame)
            {
                Entity ent = world.Get(serial);

                if (ent != null && ent.HitsRequest >= HitsRequestStatus.Pending)
                {
                    ent.HitsRequest = HitsRequestStatus.None;
                    force = true;
                }

                if (force && SerialHelper.IsValid(serial))
                {
                    //ent = ent ?? World.Player;
                    //ent.AddMessage(MessageType.Regular, $"PACKET REMOVED SENT: 0x{serial:X8}", 3, 0x34 + 10, true, TextType.OBJECT);
                    Socket.Send_CloseStatusBarGump(serial);
                }
            }
        }

        public static void CastSpellFromBook(int index, uint bookSerial)
        {
            if (index >= 0)
            {
                LastSpellIndex = index;
                Socket.Send_CastSpellFromBook(index, bookSerial);
            }
        }

        public static void CastSpell(int index)
        {
            if (index >= 0)
            {
                LastSpellIndex = index;
                Socket.Send_CastSpell(index);
            }
        }

        public static void OpenGuildGump(World world)
        {
            Socket.Send_GuildMenuRequest(world);
        }

        public static void ChangeStatLock(byte stat, Lock state)
        {
            Socket.Send_StatLockStateRequest(stat, state);
        }

        public static void Rename(uint serial, string name)
        {
            Socket.Send_RenameRequest(serial, name);
        }

        public static void UseSkill(int index)
        {
            if (index >= 0)
            {
                LastSkillIndex = index;
                Socket.Send_UseSkill(index);
            }
        }

        public static void OpenPopupMenu(uint serial, bool shift = false)
        {
            shift = shift || Keyboard.Shift;

            if (ProfileManager.CurrentProfile.HoldShiftForContext && !shift)
            {
                return;
            }

            Socket.Send_RequestPopupMenu(serial);
        }

        public static void ResponsePopupMenu(uint serial, ushort index)
        {
            Socket.Send_PopupMenuSelection(serial, index);
        }

        public static void MessageOverhead(World world, string message, uint entity)
        {
            Print(world, world.Get(entity), message);
        }

        public static void MessageOverhead(World world, string message, ushort hue, uint entity)
        {
            Print(world, world.Get(entity), message, hue);
        }

        public static void AcceptTrade(uint serial, bool accepted)
        {
            Socket.Send_TradeResponse(serial, 2, accepted);
        }

        public static void CancelTrade(uint serial)
        {
            Socket.Send_TradeResponse(serial, 1, false);
        }

        public static void AllNames(World world)
        {
            foreach (Mobile mobile in world.Mobiles.Values)
            {
                if (mobile != world.Player)
                {
                    Socket.Send_ClickRequest(mobile.Serial);
                }
            }

            foreach (Item item in world.Items.Values)
            {
                if (item.IsCorpse)
                {
                    Socket.Send_ClickRequest(item.Serial);
                }
            }
        }

        public static void OpenDoor()
        {
            Socket.Send_OpenDoor();
        }

        public static void EmoteAction(string action)
        {
            Socket.Send_EmoteAction(action);
        }

        public static void OpenAbilitiesBook(World world)
        {
            if (UIManager.GetGump<CombatBookGump>() == null)
            {
                UIManager.Add(new CombatBookGump(world, 100, 100));
            }
        }

        public static void UsePrimaryAbility(World world)
        {
            ref Ability ability = ref world.Player.Abilities[0];

            if (((byte) ability & 0x80) == 0)
            {
                for (int i = 0; i < 2; i++)
                {
                    world.Player.Abilities[i] &= (Ability) 0x7F;
                }

                Socket.Send_UseCombatAbility(world, (byte)ability);
            }
            else
            {
                Socket.Send_UseCombatAbility(world, 0);
            }

            ability ^= (Ability) 0x80;
        }

        public static void UseSecondaryAbility(World world)
        {
            ref Ability ability = ref world.Player.Abilities[1];

            if (((byte) ability & 0x80) == 0)
            {
                for (int i = 0; i < 2; i++)
                {
                    world.Player.Abilities[i] &= (Ability) 0x7F;
                }

                Socket.Send_UseCombatAbility(world, (byte)ability);
            }
            else
            {
                Socket.Send_UseCombatAbility(world, 0);
            }

            ability ^= (Ability) 0x80;
        }

        // ===================================================
        [Obsolete("temporary workaround to not break assistants")]
        public static void UsePrimaryAbility() => UsePrimaryAbility(ClassicUO.Client.Game.UO.World);

        [Obsolete("temporary workaround to not break assistants")]
        public static void UseSecondaryAbility() => UseSecondaryAbility(ClassicUO.Client.Game.UO.World);
        // ===================================================

        public static void QuestArrow(bool rightClick)
        {
            Socket.Send_ClickQuestArrow(rightClick);
        }

        public static void GrabItem(World world, uint serial, ushort amount, uint bag = 0)
        {
            //Socket.Send(new PPickUpRequest(serial, amount));

            Item backpack = world.Player.FindItemByLayer(Layer.Backpack);

            if (backpack == null)
            {
                return;
            }

            if (bag == 0)
            {
                bag = ProfileManager.CurrentProfile.GrabBagSerial == 0 ? backpack.Serial : ProfileManager.CurrentProfile.GrabBagSerial;
            }

            if (!world.Items.Contains(bag))
            {
                Print(world, ResGeneral.GrabBagNotFound);
                ProfileManager.CurrentProfile.GrabBagSerial = 0;
                bag = backpack.Serial;
            }

            PickUp(world, serial, 0, 0, amount);

            DropItem
            (
                serial,
                0xFFFF,
                0xFFFF,
                0,
                bag
            );
        }
    }
}
