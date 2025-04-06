// SPDX-License-Identifier: BSD-2-Clause

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
using ClassicUO.Sdk;
using Microsoft.Xna.Framework;
using static ClassicUO.Network.NetClient;
using ClassicUO.Game.Services;

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
                    ServiceProvider.Get<AudioService>().PlayMusic((RandomHelper.GetValue(0, 3) % 3) + 38, true);
                }
                else if (!war)
                {
                    ServiceProvider.Get<AudioService>().StopWarMusic();
                }
            }

            Socket.Send_ChangeWarMode(war);
        }

        public static void OpenMacroGump(World world, string name)
        {
            UIManager.GetGump<MacroGump>()?.Dispose();
            UIManager.Add(new MacroGump(world, name));
        }

        public static void OpenPaperdoll(World world, uint serial)
        {
            var paperDollGump = UIManager.GetGump<PaperDollGump>(serial);

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
            var opt = UIManager.GetGump<OptionsGump>();

            if (opt == null)
            {
                OptionsGump optionsGump = new OptionsGump(world)
                {
                    X = (ServiceProvider.Get<WindowService>().ClientBounds.Width >> 1) - 300,
                    Y = (ServiceProvider.Get<WindowService>().ClientBounds.Height >> 1) - 250
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
            ServiceProvider.Get<AudioService>().StopWarMusic();

            if (StatusGumpBase.GetStatusGump() == null)
            {
                UIManager.Add(StatusGumpBase.AddStatusGump(world, 100, 100));
            }
        }

        public static void OpenJournal(World world)
        {
            if (ProfileManager.CurrentProfile.UseAlternateJournal)
            {
                UIManager.Add(new ResizableJournal(world));
                return;
            }

            var journalGump = UIManager.GetGump<JournalGump>();

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
            if (world.Player == null)
                return;

            var skillsGump = UIManager.GetGump<StandardSkillsGump>();

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
            var miniMapGump = UIManager.GetGump<MiniMapGump>();

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
            var worldMap = UIManager.GetGump<WorldMapGump>();

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
                var chatGump = UIManager.GetGump<ChatGump>();

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
                var chatGump = UIManager.GetGump<ChatGumpChooseName>();

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
            if (world.Player == null)
                return false;

            if (!SerialHelper.IsItem(serial))
            {
                return false;
            }

            var item = world.Items.Get(serial);

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
            if (world.Player == null)
                return false;

            var backpack = world.Player.FindItemByLayer(Layer.Backpack);

            if (backpack == null)
            {
                return false;
            }

            var backpackGump = UIManager.GetGump<ContainerGump>(backpack);

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
            if (world.Player == null)
                return;

            if (ProfileManager.CurrentProfile.EnabledCriminalActionQuery)
            {
                var m = world.Mobiles.Get(serial);

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

            world.TargetManager.NewTargetSystemSerial = serial;
            world.TargetManager.LastAttack = serial;
            Socket.Send_AttackRequest(serial);
        }

        public static void DoubleClickQueued(uint serial)
        {
            ServiceProvider.Get<SceneService>().GetScene<GameScene>()?.DoubleClickDelayed(serial);
        }

        public static void DoubleClick(World world, uint serial)
        {
            if (world.Player == null)
                return;

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

            var entity = world.Get(serial);

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
            if (ServiceProvider.Get<UOService>().Version >= ClientVersion.CV_200)
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
            Entity? entity,
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
            if (world.Player == null)
                return false;

            if (world.Player.IsDead || ServiceProvider.Get<UOService>().GameCursor.ItemHold.Enabled)
            {
                return false;
            }

            var item = world.Items.Get(serial);

            if (item == null || item.IsDestroyed || item.IsMulti || item.OnGround && (item.IsLocked || item.Distance > Constants.DRAG_ITEMS_DISTANCE))
            {
                return false;
            }

            if (amount <= -1 && item.Amount > 1 && item.ItemData.IsStackable)
            {
                if (ProfileManager.CurrentProfile.HoldShiftToSplitStack == Keyboard.Shift)
                {
                    var gump = UIManager.GetGump<SplitMenuGump>(item);

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

            ServiceProvider.Get<UOService>().GameCursor.ItemHold.Clear();
            ServiceProvider.Get<UOService>().GameCursor.ItemHold.Set(item, (ushort) amount, offset);
            ServiceProvider.Get<UOService>().GameCursor.ItemHold.IsGumpTexture = is_gump;
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
            var uoService = ServiceProvider.Get<UOService>();
            if (uoService.GameCursor.ItemHold.Enabled && !uoService.GameCursor.ItemHold.IsFixedPosition && (uoService.GameCursor.ItemHold.Serial != container || uoService.GameCursor.ItemHold.ItemData.IsStackable))
            {
                if (uoService.Version >= ClientVersion.CV_6017)
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

                uoService.GameCursor.ItemHold.Enabled = false;
                uoService.GameCursor.ItemHold.Dropped = true;
            }
        }

        public static void Equip(World world, uint container = 0)
        {
            if (world.Player == null)
                return;

            var uoService = ServiceProvider.Get<UOService>();
            if (uoService.GameCursor.ItemHold.Enabled && !uoService.GameCursor.ItemHold.IsFixedPosition && uoService.GameCursor.ItemHold.ItemData.IsWearable)
            {
                if (!SerialHelper.IsValid(container))
                {
                    container = world.Player.Serial;
                }

                Socket.Send_EquipRequest(uoService.GameCursor.ItemHold.Serial, (Layer)uoService.GameCursor.ItemHold.ItemData.Layer, container);

                uoService.GameCursor.ItemHold.Enabled = false;
                uoService.GameCursor.ItemHold.Dropped = true;
            }
        }

        public static void ReplyGump(uint local, uint server, int button, uint[] switches, Tuple<ushort, string>[] entries)
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
                var ent = world.Get(serial);

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
            if (ServiceProvider.Get<UOService>().Version >= ClientVersion.CV_200 && world.InGame)
            {
                var ent = world.Get(serial);

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

        private static void SendAbility(World world, byte idx, bool primary)
        {
            if ((world.ClientLockedFeatures.Flags & LockedFeatureFlags.AOS) == 0)
            {
                if (primary)
                    Socket.Send_StunRequest();
                else
                    Socket.Send_DisarmRequest();
            }
            else
            {
                Socket.Send_UseCombatAbility(world, idx);
            }
        }

        public static void UsePrimaryAbility(World world)
        {
            if (world.Player == null)
                return;

            ref var ability = ref world.Player.Abilities[0];

            if (((byte) ability & 0x80) == 0)
            {
                for (int i = 0; i < 2; i++)
                {
                    world.Player.Abilities[i] &= (Ability) 0x7F;
                }

                SendAbility(world, (byte)ability, true);
            }
            else
            {
                SendAbility(world, 0, true);
            }

            ability ^= (Ability) 0x80;
        }

        public static void UseSecondaryAbility(World world)
        {
            if (world.Player == null)
                return;

            ref Ability ability = ref world.Player.Abilities[1];

            if (((byte) ability & 0x80) == 0)
            {
                for (int i = 0; i < 2; i++)
                {
                    world.Player.Abilities[i] &= (Ability) 0x7F;
                }

                SendAbility(world, (byte)ability, false);
            }
            else
            {
                SendAbility(world, 0, true);
            }

            ability ^= (Ability) 0x80;
        }

        // ===================================================
        [Obsolete("temporary workaround to not break assistants")]
        public static void UsePrimaryAbility() => UsePrimaryAbility(ServiceProvider.Get<UOService>().World);

        [Obsolete("temporary workaround to not break assistants")]
        public static void UseSecondaryAbility() => UseSecondaryAbility(ServiceProvider.Get<UOService>().World);
        // ===================================================

        public static void QuestArrow(bool rightClick)
        {
            Socket.Send_ClickQuestArrow(rightClick);
        }

        public static void GrabItem(World world, uint serial, ushort amount, uint bag = 0)
        {
            if (world.Player == null)
                return;

            //Socket.Send(new PPickUpRequest(serial, amount));

            var backpack = world.Player.FindItemByLayer(Layer.Backpack);

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
