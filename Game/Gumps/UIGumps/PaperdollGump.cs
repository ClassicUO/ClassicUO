#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
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
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.UIGumps
{
    internal class PaperDollGump : Gump
    {
        private bool _isWarMode;
        private Button _warModeBtn;
        private readonly ushort[] PeaceModeBtnGumps = {0x07e5, 0x07e6, 0x07e7};
        private readonly ushort[] WarModeBtnGumps = {0x07e8, 0x07e9, 0x07ea};
        private GumpPic _virtueMenuPic;
        private GumpPic _specialMovesBookPic;
        private GumpPic _partyManifestPic;

        public PaperDollGump()
            : base(0, 0) => AcceptMouseInput = false;

        public PaperDollGump(Serial serial, string mobileTitle)
            : this()
        {
            Mobile mobile = World.Mobiles.Get(serial);
            if (mobile != null)
            {
                Mobile = mobile;
                Title = mobileTitle;

                BuildGump();
            }
        }

        public Mobile Mobile { get; private set; }

        public string Title { get; }


        public override void Dispose()
        {
            if (Mobile == World.Player)
            {
                _virtueMenuPic.MouseDoubleClick -= VirtueMenu_MouseDoubleClickEvent;
                _partyManifestPic.MouseDoubleClick -= PartyManifest_MouseDoubleClickEvent;
            }

            Clear();
            base.Dispose();
        }

        private void BuildGump()
        {
            //m_World = Service.Get<WorldModel>();
            //m_Client = Service.Get<INetworkClient>();

            CanMove = true;
            X = 100;
            Y = 100;
            //SaveOnWorldStop = true;
            LocalSerial = Mobile.Serial;


            if (Mobile == World.Player)
            {
                AddChildren(new GumpPic(0, 0, 0x07d0, 0) {CanMove = true});
                //HELP BUTTON
                AddChildren(new Button((int) Buttons.Help, 0x07ef, 0x07f0, 0x07f1)
                    {X = 185, Y = 44 + 27 * 0, ButtonAction = ButtonAction.Activate});
                //OPTIONS BUTTON
                AddChildren(new Button((int) Buttons.Options, 0x07d6, 0x07d7, 0x07d8)
                    {X = 185, Y = 44 + 27 * 1, ButtonAction = ButtonAction.Activate});
                // LOG OUT BUTTON
                AddChildren(new Button((int) Buttons.LogOut, 0x07d9, 0x07da, 0x07db)
                    {X = 185, Y = 44 + 27 * 2, ButtonAction = ButtonAction.Activate});
                // QUESTS BUTTON
                AddChildren(new Button((int) Buttons.Quests, 0x57b5, 0x57b7, 0x57b6)
                    {X = 185, Y = 44 + 27 * 3, ButtonAction = ButtonAction.Activate});
                // SKILLS BUTTON
                AddChildren(new Button((int) Buttons.Skills, 0x07df, 0x07e0, 0x07e1)
                    {X = 185, Y = 44 + 27 * 4, ButtonAction = ButtonAction.Activate});
                // GUILD BUTTON
                AddChildren(new Button((int) Buttons.Guild, 0x57b2, 0x57b4, 0x57b3)
                    {X = 185, Y = 44 + 27 * 5, ButtonAction = ButtonAction.Activate});
                // TOGGLE PEACE/WAR BUTTON
                _isWarMode = Mobile.InWarMode;
                ushort[] btngumps = _isWarMode ? WarModeBtnGumps : PeaceModeBtnGumps;
                AddChildren(_warModeBtn =
                    new Button((int) Buttons.PeaceWarToggle, btngumps[0], btngumps[1], btngumps[2])
                        {X = 185, Y = 44 + 27 * 6, ButtonAction = ButtonAction.Activate});
                // STATUS BUTTON
                AddChildren(new Button((int) Buttons.Status, 0x07eb, 0x07ec, 0x07ed)
                    {X = 185, Y = 44 + 27 * 7, ButtonAction = ButtonAction.Activate});
                // Virtue menu
                AddChildren(_virtueMenuPic = new GumpPic(80, 8, 0x0071, 0));
                _virtueMenuPic.MouseDoubleClick += VirtueMenu_MouseDoubleClickEvent;
                // Special moves book
                //AddChildren(_specialMovesBookPic = new GumpPic(178, 220, 0x2B34, 0));
                //_specialMovesBookPic.MouseDoubleClick += SpecialMoves_MouseDoubleClickEvent;
                // Party manifest caller
                AddChildren(_partyManifestPic = new GumpPic(44, 195, 2002, 0));
                _partyManifestPic.MouseDoubleClick += PartyManifest_MouseDoubleClickEvent;
                // Equipment slots for hat/earrings/neck/ring/bracelet
                AddChildren(new EquipmentSlot(2,
                    76)); //AddControl(new EquipmentSlot(this, 2, 76, Mobile, EquipLayer.Helm));
                AddChildren(new EquipmentSlot(2,
                    76 + 22 * 1)); //AddControl(new EquipmentSlot(this, 2, 76 + 22 * 1, Mobile, EquipLayer.Earrings));
                AddChildren(new EquipmentSlot(2,
                    76 + 22 * 2)); //AddControl(new EquipmentSlot(this, 2, 76 + 22 * 2, Mobile, EquipLayer.Neck));
                AddChildren(new EquipmentSlot(2,
                    76 + 22 * 3)); //AddControl(new EquipmentSlot(this, 2, 76 + 22 * 3, Mobile, EquipLayer.Ring));
                AddChildren(new EquipmentSlot(2,
                    76 + 22 * 4)); //AddControl(new EquipmentSlot(this, 2, 76 + 22 * 4, Mobile, EquipLayer.Bracelet));
            }
            else
                AddChildren(new GumpPic(0, 0, 0x07d1, 0));

            // Paperdoll control!
            AddChildren(new PaperDollInteractable(8, 21, Mobile));

            // Name and title
            //AddChildren(new HtmlGump(35, 260, 180, 42, string.Format("<span color=#222 style='font-family:uni0;'>{0}", Title), 0, 0, 0, true));

            AddChildren(new HtmlGump(39, 262, 185, 42, Title, 0, 0, 0x0386, false, 1, false));
        }


        private void VirtueMenu_MouseDoubleClickEvent(object sender, MouseEventArgs args)
        {
            if (args.Button == MouseButton.Left)
            {
                GameActions.ReplyGump(World.Player, 0x000001CD, 0x00000001, new Serial[1] { Mobile.Serial });
                Service.Get<Log>().Message(LogTypes.Info, "Virtue DoubleClick event!!");
            }
        }

        private void PartyManifest_MouseDoubleClickEvent(object sender, MouseEventArgs args)
        {
            //CALLS PARTYGUMP
            if (args.Button == MouseButton.Left)
            {
                Service.Get<Log>().Message(LogTypes.Warning, "Party manifest pic event!!");
                //if (UserInterface.GetControl<PartyGump>() == null)
                //    UserInterface.AddControl(new PartyGump(), 200, 40);
                //else
                //    UserInterface.RemoveControl<PartyGump>();
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
            if (_isWarMode != Mobile.InWarMode)
            {
                _isWarMode = Mobile.InWarMode;
                ushort[] btngumps = _isWarMode ? WarModeBtnGumps : PeaceModeBtnGumps;
                _warModeBtn.ButtonGraphicNormal = btngumps[0];
                _warModeBtn.ButtonGraphicPressed = btngumps[1];
                _warModeBtn.ButtonGraphicOver = btngumps[2];
            }


            base.Update(totalMS, frameMS);
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null) =>
            base.Draw(spriteBatch, position, hue);


        public override void OnButtonClick(int buttonID)
        {
            switch ((Buttons) buttonID)
            {
                case Buttons.Help:
                    GameActions.RequestHelp();
                    Service.Get<Log>().Message(LogTypes.Info,"Help request sent!");
                    break;

                case Buttons.Options:
                    if (UIManager.Get<OptionsGump>() == null)
                        UIManager.Add(new OptionsGump() { X = 80, Y = 80 });
                    else
                        UIManager.Remove<OptionsGump>();
                    break;

                case Buttons.LogOut:
                    //
                    break;

                case Buttons.Quests:
                    GameActions.RequestQuestMenu();
                    Service.Get<Log>().Message(LogTypes.Info, "Quest menu request sent!");
                    break;

                case Buttons.Skills:
                    //
                    break;

                case Buttons.Guild:
                    //
                    break;

                case Buttons.PeaceWarToggle:
                    GameActions.ToggleWarMode();
                    Service.Get<Log>().Message(LogTypes.Info,
                        $"War mode set {(!World.Player.InWarMode ? "ON" : "OFF")} !");
                    break;

                case Buttons.Status:
                    //
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