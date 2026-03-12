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
using System;
using static ClassicUO.Network.NetClient;

namespace ClassicUO.Game
{
    public static class GameActions
    {

        public static int LastSpellIndex { get; set; } = 1;
        public static int LastSkillIndex { get; set; } = 1;
        // ## BEGIN - END ## // VISUAL HELPERS
        public static int LastSpellIndexCursor { get; set; } = 0;
        // ## BEGIN - END ## // VISUAL HELPERS
        // ## BEGIN - END ## // ONCASTINGGUMP
        public static bool iscasting { get; set; } = false;
        // ## BEGIN - END ## // ONCASTINGGUMP


        public static void ToggleWarMode()
        {
            RequestWarMode(!World.Player.InWarMode);
        }

        public static void RequestWarMode(bool war)
        {
            if (!World.Player.IsDead)
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns>False if no durability gump was open</returns>
        public static bool CloseDurabilityGump()
        {
            Gump g = UIManager.GetGump<DurabilitysGump>();
            if (g != null)
            {
                g.Dispose();
                return true;
            }
            g = UIManager.GetGump<DurabilitysGumpOld>();
            if (g != null)
            {
                g.Dispose();
                return true;
            }
            return false;
        }

        public static void OpenDurabilityGump()
        {
            UIManager.Add(new DurabilitysGump());
        }

        public static void OpenMacroGump(string name)
        {
            MacroGump macroGump = UIManager.GetGump<MacroGump>();

            macroGump?.Dispose();
            UIManager.Add(new MacroGump(name));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serial"></param>
        /// <returns>False if no paperdoll is open</returns>
        public static bool ClosePaperdoll(uint? serial = null)
        {
            serial ??= World.Player.Serial;
            Gump g = UIManager.GetGump<PaperDollGump>(serial);

            if (g != null)
            {
                g.Dispose();
                return true;
            }
            return false;
        }

        public static void OpenPaperdoll(uint serial)
        {
            PaperDollGump paperDollGump = UIManager.GetGump<PaperDollGump>(serial);

            if (paperDollGump == null)
            {
                DoubleClick(serial | 0x80000000);
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns>False if no settings are open</returns>
        public static bool CloseSettings()
        {
            Gump g = UIManager.GetGump<OptionsGump>();

            if (g != null)
            {
                g.Dispose();
                return true;
            }

            return false;
        }

        public static void OpenSettings(int page = 0)
        {
            OptionsGump opt = UIManager.GetGump<OptionsGump>();

            if (opt == null)
            {
                OptionsGump optionsGump = new OptionsGump();

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

        public static void OpenStatusBar()
        {
            Client.Game.Audio.StopWarMusic();

            if (StatusGumpBase.GetStatusGump() == null)
            {
                UIManager.Add(StatusGumpBase.AddStatusGump(ProfileManager.CurrentProfile.StatusGumpPosition.X, ProfileManager.CurrentProfile.StatusGumpPosition.Y));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>False if no status gump open</returns>
        public static bool CloseStatusBar()
        {
            Gump g = StatusGumpBase.GetStatusGump();
            if (g != null)
            {
                g.Dispose();
                return true;
            }

            return false;
        }

        public static void OpenJournal()
        {
            UIManager.Add(new ResizableJournal());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>False if no journals were open</returns>
        public static bool CloseAllJournals()
        {
            Gump g = UIManager.GetGump<ResizableJournal>();

            bool status = g != null;

            while (g != null)
            {
                g.Dispose();
                g = UIManager.GetGump<ResizableJournal>();
            }

            return status;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns>False if no spell books of that type were open</returns>
        public static bool CloseSpellBook(SpellBookType type)
        {
            SpellbookGump g = UIManager.GetGump<SpellbookGump>();

            while (g != null)
            {
                if (g.SpellBookType == type)
                {
                    g.Dispose();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>False if no skill gumps were open</returns>
        public static bool CloseSkills()
        {
            Gump g;
            if (ProfileManager.CurrentProfile.StandardSkillsGump)

                g = UIManager.GetGump<StandardSkillsGump>();
            else
                g = UIManager.GetGump<SkillGumpAdvanced>();

            if (g != null)
            {
                g.Dispose();
                return true;
            }

            return false;
        }

        public static void OpenSkills()
        {
            if (ProfileManager.CurrentProfile.StandardSkillsGump)
            {
                StandardSkillsGump skillsGump = UIManager.GetGump<StandardSkillsGump>();

                if (skillsGump != null && skillsGump.IsMinimized)
                {
                    skillsGump.IsMinimized = false;
                }
                else
                {
                    World.SkillsRequested = true;
                    Socket.Send_SkillsRequest(World.Player.Serial);
                }
            }
            else
            {
                SkillGumpAdvanced skillsGump = UIManager.GetGump<SkillGumpAdvanced>();

                if (skillsGump == null)
                {
                    World.SkillsRequested = true;
                    Socket.Send_SkillsRequest(World.Player.Serial);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>False if no mini map open</returns>
        public static bool CloseMiniMap()
        {
            Gump g = UIManager.GetGump<MiniMapGump>();

            if (g != null)
            {
                g.Dispose();
                return true;
            }

            return false;
        }

        public static void OpenMiniMap()
        {
            MiniMapGump miniMapGump = UIManager.GetGump<MiniMapGump>();

            if (miniMapGump == null)
            {
                UIManager.Add(new MiniMapGump());
            }
            else
            {
                miniMapGump.ToggleSize();
                miniMapGump.SetInScreen();
                miniMapGump.BringOnTop();
            }
        }

        public static void BandageSelf()
        {
            Item bandage = World.Player.FindBandage();
            if (bandage != null)
            {
                NetClient.Socket.Send_TargetSelectedObject(bandage.Serial, World.Player.Serial);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>False if no world map is open</returns>
        public static bool CloseWorldMap()
        {
            Gump g = UIManager.GetGump<WorldMapGump>();

            if (g != null)
            {
                g.Dispose();
                return true;
            }

            return false;
        }

        public static void OpenWorldMap()
        {
            WorldMapGump worldMap = UIManager.GetGump<WorldMapGump>();

            if (worldMap == null || worldMap.IsDisposed)
            {
                worldMap = new WorldMapGump();
                UIManager.Add(worldMap);
            }
            else
            {
                worldMap.BringOnTop();
                worldMap.SetInScreen();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>False if no chat was open</returns>
        public static bool CloseChat()
        {
            Gump g = UIManager.GetGump<ChatGump>();
            if (g != null)
            {
                g.Dispose();
                return true;
            }
            return false;
        }

        public static void OpenChat()
        {
            if (ChatManager.ChatIsEnabled == ChatStatus.Enabled)
            {
                ChatGump chatGump = UIManager.GetGump<ChatGump>();

                if (chatGump == null)
                {
                    UIManager.Add(new ChatGump());
                }
                else
                {
                    chatGump.SetInScreen();
                    chatGump.BringOnTop();
                }
            }
            else if (ChatManager.ChatIsEnabled == ChatStatus.EnabledUserRequest)
            {
                ChatGumpChooseName chatGump = UIManager.GetGump<ChatGumpChooseName>();

                if (chatGump == null)
                {
                    UIManager.Add(new ChatGumpChooseName());
                }
                else
                {
                    chatGump.SetInScreen();
                    chatGump.BringOnTop();
                }
            }
        }

        public static bool OpenCorpse(uint serial)
        {
            if (!SerialHelper.IsItem(serial))
            {
                return false;
            }

            Item item = World.Items.Get(serial);

            if (item == null || !item.IsCorpse || item.IsDestroyed)
            {
                return false;
            }

            World.Player.ManualOpenedCorpses.Add(serial);
            DoubleClick(serial);

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>False if no backpack was opened</returns>
        public static bool CloseBackpack()
        {
            Gump g;

            Item backpack = World.Player.FindItemByLayer(Layer.Backpack);

            if (backpack == null)
            {
                return false;
            }

            g = UIManager.GetGump<ContainerGump>(backpack);
            g ??= UIManager.GetGump<GridContainer>(backpack);

            if (g != null)
            {
                g.Dispose();
                return true;
            }

            return false;
        }

        public static bool ForceNormalContainerForNextOpen { get; set; }

        public static bool OpenBackpack()
        {
            Item backpack = World.Player.FindItemByLayer(Layer.Backpack);

            if (backpack == null)
            {
                return false;
            }

            Gump backpackGump = UIManager.GetGump<ContainerGump>(backpack);
            if (backpackGump == null)
            {
                backpackGump = UIManager.GetGump<GridContainer>(backpack);
                if (backpackGump == null)
                {
                    GameActions.DoubleClick(backpack);
                    return true;
                }
                else
                {
                    backpackGump.RequestUpdateContents();
                    backpackGump.SetInScreen();
                    backpackGump.BringOnTop();
                }
            }
            else
            {
                ((ContainerGump)backpackGump).IsMinimized = false;
                backpackGump.SetInScreen();
                backpackGump.BringOnTop();
            }
            return true;
        }

        public static bool OpenBackpackNormal()
        {
            Item backpack = World.Player.FindItemByLayer(Layer.Backpack);
            if (backpack == null) return false;

            UIManager.GetGump<GridContainer>(backpack)?.Dispose();
            var existing = UIManager.GetGump<ContainerGump>(backpack);
            if (existing != null)
            {
                ((ContainerGump)existing).IsMinimized = false;
                existing.SetInScreen();
                existing.BringOnTop();
                return true;
            }

            ForceNormalContainerForNextOpen = true;
            DoubleClick(backpack);
            return true;
        }

        public static bool OpenBackpackSecond()
        {
            if (!World.InGame || World.Player == null) return false;
            Item backpack = World.Player.FindItemByLayer(Layer.Backpack);
            if (backpack == null) return false;

            var existingNormal = UIManager.GetGump<ContainerGump>(backpack.Serial);
            if (existingNormal != null)
            {
                existingNormal.IsMinimized = false;
                existingNormal.SetInScreen();
                existingNormal.BringOnTop();
                return true;
            }

            ushort graphic = backpack.DisplayedGraphic;
            if (graphic == 0) graphic = 0x003C;
            ContainerManager.CalculateContainerPosition(backpack.Serial, graphic);
            int offset = 30;
            int x = ContainerManager.X + offset;
            int y = ContainerManager.Y + offset;
            var gump = new ContainerGump(backpack.Serial, graphic, false)
            {
                X = x,
                Y = y,
                InvalidateContents = true
            };
            UIManager.Add(gump);
            gump.RequestUpdateContents();
            gump.Update();
            gump.SetInScreen();
            gump.BringOnTop();
            return true;
        }

        public static void SetLastTarget(uint serial)
        {
            if (World.InGame && serial != 0)
                TargetManager.LastTargetInfo.SetEntity(serial);
        }

        public static void Attack(uint serial)
        {
            if (ProfileManager.CurrentProfile.EnabledCriminalActionQuery)
            {
                Mobile m = World.Mobiles.Get(serial);
                Item item = World.Items.Get(serial);

                if ((m == null) && (item == null))
                    return;

                if (m != null && (World.Player.NotorietyFlag == NotorietyFlag.Innocent || World.Player.NotorietyFlag == NotorietyFlag.Ally) && m.NotorietyFlag == NotorietyFlag.Innocent && m != World.Player)
                {
                    QuestionGump messageBox = new QuestionGump
                    (
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


                if (m != null && m != World.Player)
                {
                    TargetManager.NewTargetSystemSerial = serial;
                    TargetManager.LastAttack = serial;

                    Socket.Send_AttackRequest(serial);

                    return;
                }


                if (item !=  null)
                {
                   
                    Socket.Send_DoubleClick(serial);
                    return;

                }
            } else {
                Mobile m = World.Mobiles.Get(serial);
                Item item = World.Items.Get(serial);

                if ((m == null) && (item == null))
                    return;

                if (m != null && (World.Player.NotorietyFlag == NotorietyFlag.Innocent || World.Player.NotorietyFlag == NotorietyFlag.Ally) && m.NotorietyFlag == NotorietyFlag.Innocent && m != World.Player)
                {
                   
                    Socket.Send_AttackRequest(serial);
                           
                    return;
                }


                if (m != null && m != World.Player)
                {
                    TargetManager.NewTargetSystemSerial = serial;
                    TargetManager.LastAttack = serial;

                    Socket.Send_AttackRequest(serial);

                    return;
                }

                if (item !=  null)
                {      
                    Socket.Send_DoubleClick(serial);
                    return;

                }
            }
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
                if (SerialHelper.IsItem(serial))
                {
                    Gump g = UIManager.GetGump<GridContainer>(serial);
                    if (g != null)
                    {
                        g.SetInScreen();
                        g.BringOnTop();
                    }
                   
                    Socket.Send_DoubleClick(serial);
                }
                else
                    Socket.Send_DoubleClick(serial);
            }

            if (SerialHelper.IsItem(serial) || (SerialHelper.IsMobile(serial) && (World.Mobiles.Get(serial)?.IsHuman ?? false)))
            {
                if (SerialHelper.IsMobile(serial))
                {
                    TargetManager.NewTargetSystemSerial = serial;
                }

                World.LastObject = serial;
            }
            else
            {
                World.LastObject = 0;
            }
        }

        public static void SingleClick(uint serial)
        {
            // add  request context menu
            Socket.Send_ClickRequest(serial);

            Entity entity = World.Get(serial);

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
            if (Client.Version >= ClientVersion.CV_200)
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


        public static void Print(string message, ushort hue = 946, MessageType type = MessageType.Regular, byte font = 3, bool unicode = true)
        {
            if (type == MessageType.ChatSystem)
            {
                MessageManager.HandleMessage
                (
                    null,
                    message,
                    "Chat",
                    hue,
                    type,
                    font,
                    TextType.OBJECT,
                    unicode,
                    Settings.GlobalSettings.Language
                );
                return;
            }

            Print
            (
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
            Entity entity,
            string message,
            ushort hue = 946,
            MessageType type = MessageType.Regular,
            byte font = 3,
            bool unicode = true
        )
        {
            MessageManager.HandleMessage
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

        public static void RequestPartyQuit()
        {
            Socket.Send_PartyRemoveRequest(World.Player.Serial);
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
            uint serial,
            int x,
            int y,
            int amount = -1,
            Point? offset = null,
            bool is_gump = false
        )
        {
            if (World.Player.IsDead || Client.Game.GameCursor.ItemHold.Enabled)
            {
                return false;
            }

            Item item = World.Items.Get(serial);

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

                    gump = new SplitMenuGump(item, new Point(x, y))
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

            Client.Game.GameCursor.ItemHold.Clear();
            Client.Game.GameCursor.ItemHold.Set(item, (ushort)amount, offset);
            Client.Game.GameCursor.ItemHold.IsGumpTexture = is_gump;
            Socket.Send_PickUpRequest(item, (ushort)amount);

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
            if (Client.Game.GameCursor.ItemHold.Enabled && !Client.Game.GameCursor.ItemHold.IsFixedPosition && (Client.Game.GameCursor.ItemHold.Serial != container || Client.Game.GameCursor.ItemHold.ItemData.IsStackable))
            {
                if (Client.Version >= ClientVersion.CV_6017)
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

                Client.Game.GameCursor.ItemHold.Enabled = false;
                Client.Game.GameCursor.ItemHold.Dropped = true;
            }
        }

        public static void Equip(uint container = 0)
        {
            if (Client.Game.GameCursor.ItemHold.Enabled && !Client.Game.GameCursor.ItemHold.IsFixedPosition && Client.Game.GameCursor.ItemHold.ItemData.IsWearable)
            {
                if (!SerialHelper.IsValid(container))
                {
                    container = World.Player.Serial;
                }

                Socket.Send_EquipRequest(Client.Game.GameCursor.ItemHold.Serial, (Layer)Client.Game.GameCursor.ItemHold.ItemData.Layer, container);

                Client.Game.GameCursor.ItemHold.Enabled = false;
                Client.Game.GameCursor.ItemHold.Dropped = true;
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

        public static void RequestQuestMenu()
        {
            Socket.Send_QuestMenuRequest();
        }

        public static void RequestProfile(uint serial)
        {
            Socket.Send_ProfileRequest(serial);
        }

        public static void ChangeSkillLockStatus(ushort skillindex, byte lockstate)
        {
            Socket.Send_SkillStatusChangeRequest(skillindex, lockstate);
        }

        public static void RequestMobileStatus(uint serial, bool force = false)
        {
            if (World.InGame)
            {
                Entity ent = World.Get(serial);

                if (ent != null)
                {
                    if (force)
                    {
                        if (ent.HitsRequest >= HitsRequestStatus.Pending)
                        {
                            SendCloseStatus(serial);
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

        public static void SendCloseStatus(uint serial, bool force = false)
        {
            if (Client.Version >= ClientVersion.CV_200 && World.InGame)
            {
                Entity ent = World.Get(serial);

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
                // ## BEGIN - END ## // VISUAL HELPERS
                LastSpellIndexCursor = index;
                GameCursor._spellTime = 0;
                // ## BEGIN - END ## // VISUAL HELPERS
                // ## BEGIN - END ## // ONCASTINGGUMP
                if (ProfileManager.CurrentProfile.OnCastingGump)
                {
                    if (!iscasting)
                        World.Player.OnCasting.Start((uint) index);
                }
                // ## BEGIN - END ## // ONCASTINGGUMP
                Socket.Send_CastSpellFromBook(index, bookSerial);
            }
        }

        public static void CastSpell(int index)
        {
            if (index >= 0)
            {
                LastSpellIndex = index;
                Client.Game.GetScene<GameScene>()?.ActionBar?.NotifySpellCast(index);
                // ## BEGIN - END ## // VISUAL HELPERS
                LastSpellIndexCursor = index;
                GameCursor._spellTime = 0;
                // ## BEGIN - END ## // VISUAL HELPERS
                 // ## BEGIN - END ## // ONCASTINGGUMP
                if (ProfileManager.CurrentProfile.OnCastingGump)
                {
                    if (!iscasting)
                        World.Player.OnCasting.Start((uint) index);
                }
                // ## BEGIN - END ## // ONCASTINGGUMP
                Socket.Send_CastSpell(index);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">Can be a partial match</param>
        public static bool CastSpellByName(string name)
        {
            name = name.Trim();

            if (!string.IsNullOrEmpty(name) && SpellDefinition.TryGetSpellFromName(name, out var spellDef))
            {
                CastSpell(spellDef.ID);
                return true;
            }

            return false;
        }

        public static void OpenGuildGump()
        {
            Socket.Send_GuildMenuRequest();
        }

        public static void ChangeStatLock(byte stat, Lock state)
        {
            Socket.Send_StatLockStateRequest(stat, state);
        }

        public static void Rename(uint serial, string name)
        {
            Socket.Send_RenameRequest(serial, name);
        }

        public static void Logout()
        {
            if ((World.ClientFeatures.Flags & CharacterListFlags.CLF_OWERWRITE_CONFIGURATION_BUTTON) != 0)
            {
                Client.Game.GetScene<GameScene>().DisconnectionRequested = true;
                NetClient.Socket.Send_LogoutNotification();
            }
            else
            {
                NetClient.Socket.Disconnect();
                Client.Game.SetScene(new LoginScene());
            }
        }

        public static void UseSkill(int index)
        {
            Client.Game.GetScene<GameScene>()?.ActionBar?.NotifySkillUsed(index);
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
            Socket.Send_TradeResponse(serial, 2, accepted);
        }

        public static void CancelTrade(uint serial)
        {
            Socket.Send_TradeResponse(serial, 1, false);
        }

        public static void AllNames()
        {
            foreach (Mobile mobile in World.Mobiles.Values)
            {
                if (mobile != World.Player)
                {
                    Socket.Send_ClickRequest(mobile.Serial);
                }
            }

            foreach (Item item in World.Items.Values)
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

        public static void OpenAbilitiesBook()
        {
            if (UIManager.GetGump<CombatBookGump>() == null)
            {
                UIManager.Add(new CombatBookGump(100, 100));
            }
        }
        private static void SendAbility(byte idx, bool primary)
        {
            if ((World.ClientLockedFeatures.Flags & LockedFeatureFlags.AOS) == 0)
            {
                if (primary)
                    Socket.Send_StunRequest();
                else
                    Socket.Send_DisarmRequest();
            }
            else
            {
                Socket.Send_UseCombatAbility(idx);
                if (idx > 0)
                    Client.Game.GetScene<GameScene>()?.ActionBar?.NotifyAbilityUsed(idx);
            }
        }


        public static void UseCombatAbility(byte index)
        {
            if (index > 0 && index <= AbilityData.Abilities.Length && (World.ClientLockedFeatures.Flags & LockedFeatureFlags.AOS) != 0)
            {
                Socket.Send_UseCombatAbility(index);
                Client.Game.GetScene<GameScene>()?.ActionBar?.NotifyAbilityUsed(index);
            }
        }

        public static void UsePrimaryAbility()
        {
            ref var ability = ref World.Player.Abilities[0];

            if (((byte)ability & 0x80) == 0)
            {
                for (int i = 0; i < 2; i++)
                {
                    World.Player.Abilities[i] &= (Ability)0x7F;
                }

                SendAbility((byte)ability, true);
            }
            else
            {
                SendAbility(0, true);
            }


            ability ^= (Ability)0x80;
        }

        public static void UseSecondaryAbility()
        {
            ref Ability ability = ref World.Player.Abilities[1];

            if (((byte)ability & 0x80) == 0)
            {
                for (int i = 0; i < 2; i++)
                {
                    World.Player.Abilities[i] &= (Ability)0x7F;
                }

                SendAbility((byte)ability, false);
            }
            else
            {
                SendAbility(0, true);
            }

            ability ^= (Ability)0x80;
        }

        public static void QuestArrow(bool rightClick)
        {
            Socket.Send_ClickQuestArrow(rightClick);
        }

        public static void GrabItem(uint serial, ushort amount, uint bag = 0, bool stack = true)
        {
            //Socket.Send(new PPickUpRequest(serial, amount));

            Item backpack = World.Player.FindItemByLayer(Layer.Backpack);

            if (backpack == null)
            {
                return;
            }

            if (bag == 0)
            {
                bag = ProfileManager.CurrentProfile.GrabBagSerial == 0 ? backpack.Serial : ProfileManager.CurrentProfile.GrabBagSerial;
            }

            if (!World.Items.Contains(bag))
            {
                Print(ResGeneral.GrabBagNotFound);
                ProfileManager.CurrentProfile.GrabBagSerial = 0;
                bag = backpack.Serial;
            }

            PickUp(serial, 0, 0, amount);

            if (stack)
                DropItem
                (
                    serial,
                    0xFFFF,
                    0xFFFF,
                    0,
                    bag
                );
            else
                DropItem
                (
                    serial,
                    0,
                    0,
                    0,
                    bag
                );
        }
    }
}
