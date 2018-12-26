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

using System.IO;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.UI.Gumps
{
    internal class PaperDollGump : Gump
    {
        private static readonly ushort[] PeaceModeBtnGumps =
        {
            0x07e5, 0x07e6, 0x07e7
        };
        private static readonly ushort[] WarModeBtnGumps =
        {
            0x07e8, 0x07e9, 0x07ea
        };
        private bool _isWarMode;
        private GumpPic _partyManifestPic;
        private GumpPic _specialMovesBookPic;
        private GumpPic _virtueMenuPic;
        private Button _warModeBtn;
        private PaperDollInteractable _paperDollInteractable;

        public PaperDollGump() : base(0, 0)
        {
            
        }

        public PaperDollGump(Serial serial, string mobileTitle) : this()
        {
            Mobile mobile = World.Mobiles.Get(serial);

            if (mobile != null)
            {
                Mobile = mobile;
                Title = mobileTitle;
                BuildGump();
            }
        }

        public string Title { get; }

        public Mobile Mobile { get; set; }

        public override void Dispose()
        {
            Engine.UI.SavePosition(LocalSerial, Location);

            if (Mobile == World.Player)
            {
                if (_virtueMenuPic != null)
                    _virtueMenuPic.MouseDoubleClick -= VirtueMenu_MouseDoubleClickEvent;
                if (_partyManifestPic != null)
                    _partyManifestPic.MouseDoubleClick -= PartyManifest_MouseDoubleClickEvent;
            }

            Clear();
            base.Dispose();
        }

        protected override void OnMouseExit(int x, int y)
        {
            _paperDollInteractable.AddFakeDress(null);
        }

        protected override void OnMouseEnter(int x, int y)
        {
            GameScene gs = Engine.SceneManager.GetScene<GameScene>();

            if (gs.IsHoldingItem)
            {
                _paperDollInteractable.AddFakeDress(new Item(gs.HeldItem.Serial)
                {
                    Graphic = gs.HeldItem.Graphic,
                    Hue = gs.HeldItem.Hue
                });
            }
        }

        private void BuildGump()
        {
            AcceptMouseInput = false;
            CanBeSaved = true;
            CanMove = true;
            LocalSerial = Mobile.Serial;

            if (Mobile == World.Player)
            {
                AddChildren(new GumpPic(0, 0, 0x07d0, 0));

                //HELP BUTTON
                AddChildren(new Button((int) Buttons.Help, 0x07ef, 0x07f0, 0x07f1)
                {
                    X = 185, Y = 44 + 27 * 0, ButtonAction = ButtonAction.Activate
                });

                //OPTIONS BUTTON
                AddChildren(new Button((int) Buttons.Options, 0x07d6, 0x07d7, 0x07d8)
                {
                    X = 185, Y = 44 + 27 * 1, ButtonAction = ButtonAction.Activate
                });

                // LOG OUT BUTTON
                AddChildren(new Button((int) Buttons.LogOut, 0x07d9, 0x07da, 0x07db)
                {
                    X = 185, Y = 44 + 27 * 2, ButtonAction = ButtonAction.Activate
                });

                // QUESTS BUTTON
                AddChildren(new Button((int) Buttons.Quests, 0x57b5, 0x57b7, 0x57b6)
                {
                    X = 185, Y = 44 + 27 * 3, ButtonAction = ButtonAction.Activate
                });

                // SKILLS BUTTON
                AddChildren(new Button((int) Buttons.Skills, 0x07df, 0x07e0, 0x07e1)
                {
                    X = 185, Y = 44 + 27 * 4, ButtonAction = ButtonAction.Activate
                });

                // GUILD BUTTON
                AddChildren(new Button((int) Buttons.Guild, 0x57b2, 0x57b4, 0x57b3)
                {
                    X = 185, Y = 44 + 27 * 5, ButtonAction = ButtonAction.Activate
                });
                // TOGGLE PEACE/WAR BUTTON
                _isWarMode = Mobile.InWarMode;
                ushort[] btngumps = _isWarMode ? WarModeBtnGumps : PeaceModeBtnGumps;

                AddChildren(_warModeBtn = new Button((int) Buttons.PeaceWarToggle, btngumps[0], btngumps[1], btngumps[2])
                {
                    X = 185, Y = 44 + 27 * 6, ButtonAction = ButtonAction.Activate
                });

                // STATUS BUTTON
                AddChildren(new Button((int) Buttons.Status, 0x07eb, 0x07ec, 0x07ed)
                {
                    X = 185, Y = 44 + 27 * 7, ButtonAction = ButtonAction.Activate
                });
                // Virtue menu
                AddChildren(_virtueMenuPic = new GumpPic(80, 8, 0x0071, 0));
                _virtueMenuPic.MouseDoubleClick += VirtueMenu_MouseDoubleClickEvent;
                // Special moves book
                //AddChildren(_specialMovesBookPic = new GumpPic(178, 220, 0x2B34, 0));
                //_specialMovesBookPic.MouseDoubleClick += SpecialMoves_MouseDoubleClickEvent;
                // Party manifest caller
                AddChildren(_partyManifestPic = new GumpPic(44, 195, 2002, 0));
                _partyManifestPic.MouseDoubleClick += PartyManifest_MouseDoubleClickEvent;
            }
            else
                AddChildren(new GumpPic(0, 0, 0x07d1, 0));

            // Equipment slots for hat/earrings/neck/ring/bracelet
            AddChildren(new EquipmentSlot(2, 76, Mobile, Layer.Helmet));
            AddChildren(new EquipmentSlot(2, 76 + 22, Mobile, Layer.Earrings));
            AddChildren(new EquipmentSlot(2, 76 + 22 * 2, Mobile, Layer.Necklace));
            AddChildren(new EquipmentSlot(2, 76 + 22 * 3, Mobile, Layer.Ring));
            AddChildren(new EquipmentSlot(2, 76 + 22 * 4, Mobile, Layer.Bracelet));
            AddChildren(new EquipmentSlot(2, 76 + 22 * 5, Mobile, Layer.Tunic));

            // Paperdoll control!
            _paperDollInteractable = new PaperDollInteractable(8, 21, Mobile);
            //_paperDollInteractable.MouseOver += (sender, e) =>
            //{
            //    OnMouseOver(e.X, e.Y);
            //};
            AddChildren(_paperDollInteractable);

            // Name and title
            Label titleLabel = new Label(Title, false, 0x0386, 185)
            {
                X = 39, Y = 262
            };
            AddChildren(titleLabel);
        }


        protected override void OnMouseUp(int x, int y, MouseButton button)
        {
            GameScene gs = Engine.SceneManager.GetScene<GameScene>();
            if (!gs.IsHoldingItem || !gs.IsMouseOverUI)
                return;

            if (gs.HeldItem.ItemData.IsWearable)
            {
                gs.WearHeldItem(Mobile);
            }
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButton button)
        {
            return true;
        }

        private void VirtueMenu_MouseDoubleClickEvent(object sender, MouseDoubleClickEventArgs args)
        {
            if (args.Button == MouseButton.Left)
            {
                GameActions.ReplyGump(World.Player, 0x000001CD, 0x00000001, new[]
                {
                    Mobile.Serial
                });
                Log.Message(LogTypes.Info, "Virtue DoubleClick event!!");
            }
        }

        private void PartyManifest_MouseDoubleClickEvent(object sender, MouseDoubleClickEventArgs args)
        {
            //CALLS PARTYGUMP
            if (args.Button == MouseButton.Left)
            {
                Log.Message(LogTypes.Warning, "Party manifest pic event!!");

                if (Engine.UI.GetByLocalSerial<PartyGumpAdvanced>() == null)
                    Engine.UI.Add(new PartyGumpAdvanced());
                else
                    Engine.UI.Remove<PartyGumpAdvanced>();
            }
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (Mobile != null && Mobile.IsDisposed)
                Mobile = null;

            if (Mobile == null)
            {
                Dispose();

                return;
            }

            // This is to update the state of the war mode button.
            if (_isWarMode != Mobile.InWarMode && Mobile == World.Player)
            {
                _isWarMode = Mobile.InWarMode;
                ushort[] btngumps = _isWarMode ? WarModeBtnGumps : PeaceModeBtnGumps;
                _warModeBtn.ButtonGraphicNormal = btngumps[0];
                _warModeBtn.ButtonGraphicPressed = btngumps[1];
                _warModeBtn.ButtonGraphicOver = btngumps[2];
            }

            base.Update(totalMS, frameMS);
        }


        public override void Save(BinaryWriter writer)
        {
            base.Save(writer);
            writer.Write(Mobile.Serial);
        }

        public override void Restore(BinaryReader reader)
        {
            base.Restore(reader);
            LocalSerial = reader.ReadUInt32();
            Engine.SceneManager.GetScene<GameScene>().DoubleClickDelayed(LocalSerial);
            Dispose();
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((Buttons) buttonID)
            {
                case Buttons.Help:
                    GameActions.RequestHelp();

                    break;
                case Buttons.Options:

                    if (Engine.UI.GetByLocalSerial<OptionsGump1>() == null)
                    {
                        Engine.UI.Add(new OptionsGump1
                        {
                            X = 80, Y = 80
                        });
                    }
                    else
                        Engine.UI.Remove<OptionsGump1>();

                    break;
                case Buttons.LogOut:
                    Engine.UI.Add(new QuestionGump("Quit\nUltima Online?", s =>
                    {
                        if (s)
                            Engine.SceneManager.ChangeScene(ScenesType.Login);
                    }));

                    break;
                case Buttons.Quests:
                    GameActions.RequestQuestMenu();

                    break;
                case Buttons.Skills:

                    if (Engine.UI.GetByLocalSerial<SkillGumpAdvanced>() == null)
                        Engine.UI.Add(new SkillGumpAdvanced());
                    else
                        Engine.UI.Remove<SkillGumpAdvanced>();

                    break;
                case Buttons.Guild:
                    GameActions.OpenGuildGump();

                    break;
                case Buttons.PeaceWarToggle:
                    GameActions.ToggleWarMode();

                    break;
                case Buttons.Status:

                    Engine.UI.GetByLocalSerial<HealthBarGump>(World.Player)?.Dispose();

                    if (Engine.UI.GetByLocalSerial<StatusGump>() == null)
                    {
                        Engine.UI.Add(new StatusGump()
                        {
                            X = Mouse.Position.X - 100,
                            Y = Mouse.Position.Y - 25
                        });
                    }

                    break;
            }
        }

        private enum Buttons
        {
            Help,
            Options,
            LogOut,
            Quests,
            Skills,
            Guild,
            PeaceWarToggle,
            Status
        }
    }
}