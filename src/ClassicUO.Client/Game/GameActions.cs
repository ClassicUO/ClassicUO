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
using ClassicUO.Utility;
using Microsoft.Xna.Framework;


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
                if (war && player.World.Profile.CurrentProfile != null && player.World.Profile.CurrentProfile.EnableMusic)
                {
                    player.World.Context.Game.Audio.PlayMusic((RandomHelper.GetValue(0, 3) % 3) + 38, true);
                }
                else if (!war)
                {
                    player.World.Context.Game.Audio.StopWarMusic();
                }
            }

            player.World.Network.Send_ChangeWarMode(war);
        }

        public static void OpenMacroGump(World world, string name)
        {
            MacroGump macroGump = world.Context.UI.GetGump<MacroGump>();

            macroGump?.Dispose();
            world.Context.UI.Add(new MacroGump(world, name));
        }

        public static void OpenPaperdoll(World world, uint serial)
        {
            PaperDollGump paperDollGump = world.Context.UI.GetGump<PaperDollGump>(serial);

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
            OptionsGump opt = world.Context.UI.GetGump<OptionsGump>();

            if (opt == null)
            {
                OptionsGump optionsGump = new OptionsGump(world)
                {
                    X = (world.Context.Game.ClientBounds.Width >> 1) - 300,
                    Y = (world.Context.Game.ClientBounds.Height >> 1) - 250
                };

                world.Context.UI.Add(optionsGump);
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
            world.Context.Game.Audio.StopWarMusic();

            if (StatusGumpBase.GetStatusGump(world.Profile.CurrentProfile, world.Context.UI) == null)
            {
                world.Context.UI.Add(StatusGumpBase.AddStatusGump(world, 100, 100));
            }
        }

        public static void OpenJournal(World world)
        {
            if (world.Profile.CurrentProfile.UseAlternateJournal)
            {
                world.Context.UI.Add(new ResizableJournal(world));
                return;
            }

            JournalGump journalGump = world.Context.UI.GetGump<JournalGump>();

            if (journalGump == null)
            {
                world.Context.UI.Add(new JournalGump(world) { X = 64, Y = 64 });
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
            StandardSkillsGump skillsGump = world.Context.UI.GetGump<StandardSkillsGump>();

            if (skillsGump != null && skillsGump.IsMinimized)
            {
                skillsGump.IsMinimized = false;
            }
            else
            {
                world.SkillsRequested = true;
                world.Network.Send_SkillsRequest(world.Player.Serial);
            }
        }

        public static void OpenMiniMap(World world)
        {
            MiniMapGump miniMapGump = world.Context.UI.GetGump<MiniMapGump>();

            if (miniMapGump == null)
            {
                world.Context.UI.Add(new MiniMapGump(world));
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
            WorldMapGump worldMap = world.Context.UI.GetGump<WorldMapGump>();

            if (worldMap == null || worldMap.IsDisposed)
            {
                worldMap = new WorldMapGump(world);
                world.Context.UI.Add(worldMap);
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
                ChatGump chatGump = world.Context.UI.GetGump<ChatGump>();

                if (chatGump == null)
                {
                    world.Context.UI.Add(new ChatGump(world));
                }
                else
                {
                    chatGump.SetInScreen();
                    chatGump.BringOnTop();
                }
            }
            else if (world.ChatManager.ChatIsEnabled == ChatStatus.EnabledUserRequest)
            {
                ChatGumpChooseName chatGump = world.Context.UI.GetGump<ChatGumpChooseName>();

                if (chatGump == null)
                {
                    world.Context.UI.Add(new ChatGumpChooseName(world));
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

            ContainerGump backpackGump = world.Context.UI.GetGump<ContainerGump>(backpack);

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
            if (world.Profile.CurrentProfile.EnabledCriminalActionQuery)
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
                                world.Network.Send_AttackRequest(serial);
                            }
                        }
                    );

                    world.Context.UI.Add(messageBox);

                    return;
                }
            }

            world.TargetManager.NewTargetSystemSerial = serial;
            world.TargetManager.LastAttack = serial;
            world.Network.Send_AttackRequest(serial);
        }

        public static void DoubleClickQueued(World world, uint serial)
        {
            world.Context.Game.GetScene<GameScene>()?.DoubleClickDelayed(serial);
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
                world.Network.Send_DoubleClick(serial);
            }

            if (SerialHelper.IsItem(serial) || (SerialHelper.IsMobile(serial) && (world.Mobiles.Get(serial)?.IsHuman ?? false)))
            {
                if (SerialHelper.IsMobile(serial))
                {
                    world.TargetManager.NewTargetSystemSerial = serial;
                }
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
            world.Network.Send_ClickRequest(serial);

            Entity entity = world.Get(serial);

            if (entity != null)
            {
                entity.IsClicked = true;
            }
        }

        public static void Say(World world, string message, ushort hue = 0xFFFF, MessageType type = MessageType.Regular, byte font = 3)
        {
            if (hue == 0xFFFF)
            {
                hue = world.Profile.CurrentProfile.SpeechHue;
            }

            // TODO: identify what means 'older client' that uses ASCIISpeechRquest [0x03]
            //
            // Fix -> #1267
            if (world.Context.Game.UO.Version >= ClientVersion.CV_200)
            {
                world.Network.Send_UnicodeSpeechRequest(message,
                                                 type,
                                                 font,
                                                 hue,
                                                 world.Settings.Language,
                                                 world.Context.Game.UO.FileManager);
            }
            else
            {
                world.Network.Send_ASCIISpeechRequest(message, type, font, hue, world.Context.Game.UO.FileManager);
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
                world.Settings.Language
            );
        }

        public static void SayParty(World world, string message, uint serial = 0)
        {
            world.Network.Send_PartyMessage(message, serial);
        }

        public static void RequestPartyAccept(World world, uint serial)
        {
            world.Network.Send_PartyAccept(serial);

            world.Context.UI.GetGump<PartyInviteGump>()?.Dispose();
        }

        public static void RequestPartyRemoveMemberByTarget(World world)
        {
            world.Network.Send_PartyRemoveRequest(0x00);
        }

        public static void RequestPartyRemoveMember(World world, uint serial)
        {
            world.Network.Send_PartyRemoveRequest(serial);
        }

        public static void RequestPartyQuit(PlayerMobile player)
        {
            player.World.Network.Send_PartyRemoveRequest(player.Serial);
        }

        public static void RequestPartyInviteByTarget(World world)
        {
            world.Network.Send_PartyInviteRequest();
        }

        public static void RequestPartyLootState(World world, bool isLootable)
        {
            world.Network.Send_PartyChangeLootTypeRequest(isLootable);
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
            if (world.Player.IsDead || world.Context.Game.UO.GameCursor.ItemHold.Enabled)
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
                if (world.Profile.CurrentProfile.HoldShiftToSplitStack == Keyboard.Shift)
                {
                    SplitMenuGump gump = world.Context.UI.GetGump<SplitMenuGump>(item);

                    if (gump != null)
                    {
                        return false;
                    }

                    gump = new SplitMenuGump(world, item, new Point(x, y))
                    {
                        X = Mouse.Position.X - 80,
                        Y = Mouse.Position.Y - 40
                    };

                    world.Context.UI.Add(gump);
                    world.Context.UI.AttemptDragControl(gump, true);

                    return true;
                }
            }

            if (amount <= 0)
            {
                amount = item.Amount;
            }

            world.Context.Game.UO.GameCursor.ItemHold.Clear();
            world.Context.Game.UO.GameCursor.ItemHold.Set(item, (ushort) amount, offset);
            world.Context.Game.UO.GameCursor.ItemHold.IsGumpTexture = is_gump;
            world.Network.Send_PickUpRequest(item, (ushort) amount);

            if (item.OnGround)
            {
                item.RemoveFromTile();
            }

            item.TextContainer?.Clear();

            world.ObjectToRemove = item.Serial;

            return true;
        }

        public static void DropItem(World world, uint serial, int x, int y, int z, uint container)
        {
            if (world.Context.Game.UO.GameCursor.ItemHold.Enabled && !world.Context.Game.UO.GameCursor.ItemHold.IsFixedPosition && (world.Context.Game.UO.GameCursor.ItemHold.Serial != container || world.Context.Game.UO.GameCursor.ItemHold.ItemData.IsStackable))
            {
                if (world.Context.Game.UO.Version >= ClientVersion.CV_6017)
                {
                    world.Network.Send_DropRequest(serial,
                                            (ushort)x,
                                            (ushort)y,
                                            (sbyte)z,
                                            0,
                                            container);
                }
                else
                {
                    world.Network.Send_DropRequest_Old(serial,
                                                (ushort)x,
                                                (ushort)y,
                                                (sbyte)z,
                                                container);
                }

                world.Context.Game.UO.GameCursor.ItemHold.Enabled = false;
                world.Context.Game.UO.GameCursor.ItemHold.Dropped = true;
            }
        }

        public static void Equip(World world, uint container = 0)
        {
            if (world.Context.Game.UO.GameCursor.ItemHold.Enabled && !world.Context.Game.UO.GameCursor.ItemHold.IsFixedPosition && world.Context.Game.UO.GameCursor.ItemHold.ItemData.IsWearable)
            {
                if (!SerialHelper.IsValid(container))
                {
                    container = world.Player.Serial;
                }

                world.Network.Send_EquipRequest(world.Context.Game.UO.GameCursor.ItemHold.Serial, (Layer)world.Context.Game.UO.GameCursor.ItemHold.ItemData.Layer, container);

                world.Context.Game.UO.GameCursor.ItemHold.Enabled = false;
                world.Context.Game.UO.GameCursor.ItemHold.Dropped = true;
            }
        }

        public static void ReplyGump(World world, uint local, uint server, int button, uint[] switches = null, Tuple<ushort, string>[] entries = null)
        {
            world.Network.Send_GumpResponse(local,
                                     server,
                                     button,
                                     switches,
                                     entries);
        }

        public static void RequestHelp(World world)
        {
            world.Network.Send_HelpRequest();
        }

        public static void RequestQuestMenu(World world)
        {
            world.Network.Send_QuestMenuRequest(world);
        }

        public static void RequestProfile(World world, uint serial)
        {
            world.Network.Send_ProfileRequest(serial);
        }

        public static void ChangeSkillLockStatus(World world, ushort skillindex, byte lockstate)
        {
            world.Network.Send_SkillStatusChangeRequest(skillindex, lockstate);
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
                    world.Network.Send_StatusRequest(serial);
                }
            }
        }

        public static void SendCloseStatus(World world, uint serial, bool force = false)
        {
            if (world.Context.Game.UO.Version >= ClientVersion.CV_200 && world.InGame)
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
                    world.Network.Send_CloseStatusBarGump(serial);
                }
            }
        }

        public static void CastSpellFromBook(World world, int index, uint bookSerial)
        {
            if (index >= 0)
            {
                LastSpellIndex = index;
                world.Network.Send_CastSpellFromBook(index, bookSerial);
            }
        }

        public static void CastSpell(World world, int index)
        {
            if (index >= 0)
            {
                LastSpellIndex = index;
                world.Network.Send_CastSpell(index, world.Context.Game.UO.Version);
            }
        }

        public static void OpenGuildGump(World world)
        {
            world.Network.Send_GuildMenuRequest(world);
        }

        public static void ChangeStatLock(World world, byte stat, Lock state)
        {
            world.Network.Send_StatLockStateRequest(stat, state);
        }

        public static void Rename(World world, uint serial, string name)
        {
            world.Network.Send_RenameRequest(serial, name);
        }

        public static void UseSkill(World world, int index)
        {
            if (index >= 0)
            {
                LastSkillIndex = index;
                world.Network.Send_UseSkill(index);
            }
        }

        public static void OpenPopupMenu(World world, uint serial, bool shift = false, Profile currentProfile = null)
        {
            currentProfile ??= world.Profile.CurrentProfile;
            shift = shift || Keyboard.Shift;

            if (currentProfile.HoldShiftForContext && !shift)
            {
                return;
            }

            world.Network.Send_RequestPopupMenu(serial);
        }

        public static void ResponsePopupMenu(World world, uint serial, ushort index)
        {
            world.Network.Send_PopupMenuSelection(serial, index);
        }

        public static void MessageOverhead(World world, string message, uint entity)
        {
            Print(world, world.Get(entity), message);
        }

        public static void MessageOverhead(World world, string message, ushort hue, uint entity)
        {
            Print(world, world.Get(entity), message, hue);
        }

        public static void AcceptTrade(World world, uint serial, bool accepted)
        {
            world.Network.Send_TradeResponse(serial, 2, accepted);
        }

        public static void CancelTrade(World world, uint serial)
        {
            world.Network.Send_TradeResponse(serial, 1, false);
        }

        public static void AllNames(World world)
        {
            foreach (Mobile mobile in world.Mobiles.Values)
            {
                if (mobile != world.Player)
                {
                    world.Network.Send_ClickRequest(mobile.Serial);
                }
            }

            foreach (Item item in world.Items.Values)
            {
                if (item.IsCorpse)
                {
                    world.Network.Send_ClickRequest(item.Serial);
                }
            }
        }

        public static void OpenDoor(World world)
        {
            world.Network.Send_OpenDoor();
        }

        public static void EmoteAction(World world, string action)
        {
            world.Network.Send_EmoteAction(action);
        }

        public static void OpenAbilitiesBook(World world)
        {
            if (world.Context.UI.GetGump<CombatBookGump>() == null)
            {
                world.Context.UI.Add(new CombatBookGump(world, 100, 100));
            }
        }

        private static void SendAbility(World world, byte idx, bool primary)
        {
            if ((world.ClientLockedFeatures.Flags & LockedFeatureFlags.AOS) == 0)
            {
                if (primary)
                    world.Network.Send_StunRequest();
                else
                    world.Network.Send_DisarmRequest();
            }
            else
            {
                world.Network.Send_UseCombatAbility(world, idx);
            }
        }

        public static void UsePrimaryAbility(World world)
        {
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
        public static void UsePrimaryAbility() => UsePrimaryAbility(global::ClassicUO.Client.Game.UO.World);

        [Obsolete("temporary workaround to not break assistants")]
        public static void UseSecondaryAbility() => UseSecondaryAbility(global::ClassicUO.Client.Game.UO.World);
        // ===================================================

        public static void QuestArrow(World world, bool rightClick)
        {
            world.Network.Send_ClickQuestArrow(rightClick);
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
                bag = world.Profile.CurrentProfile.GrabBagSerial == 0 ? backpack.Serial : world.Profile.CurrentProfile.GrabBagSerial;
            }

            if (!world.Items.Contains(bag))
            {
                Print(world, ResGeneral.GrabBagNotFound);
                world.Profile.CurrentProfile.GrabBagSerial = 0;
                bag = backpack.Serial;
            }

            PickUp(world, serial, 0, 0, amount);

            DropItem
            (
                world,
                serial,
                0xFFFF,
                0xFFFF,
                0,
                bag
            );
        }
    }
}
