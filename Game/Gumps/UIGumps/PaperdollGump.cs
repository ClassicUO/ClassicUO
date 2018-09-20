using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Utility;
using Microsoft.Xna.Framework.Input;

namespace ClassicUO.Game.Gumps.UIGumps
{
    class PaperDollGump : Gump
    {
        private bool _isWarMode;
        private Button _warModeBtn;
        private readonly ushort[] PeaceModeBtnGumps = { 0x07e5, 0x07e6, 0x07e7 };
        private readonly ushort[] WarModeBtnGumps = { 0x07e8, 0x07e9, 0x07ea };
        private GumpPic _virtueMenuPic;
        private GumpPic _specialMovesBookPic;
        private GumpPic _partyManifestPic;
        private static PaperDollGump _self;

        public PaperDollGump()
            : base(0, 0)
        {
        }

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

        public Mobile Mobile
        {
            get;
            private set;
        }

        public string Title
        {
            get;
            private set;
        }

        public static void Toggle(Serial serial, string mobileTitle)
        {
            var ui = Service.Get<UIManager>();
            var gump = ui.Get<PaperDollGump>();
            if (gump == null || gump.IsDisposed)
            {
                ui.Add(_self = new PaperDollGump(serial, mobileTitle));
            }
            else
            {
                _self.Dispose();
            }
        }

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
            //GumpLocalID = Mobile.Serial;


            if (Mobile == World.Player)
            {

                AddChildren(new GumpPic(0, 0, 0x07d0, 0) { CanMove = true });
                //HELP BUTTON
                AddChildren(new Button((int)Buttons.Help, 0x07ef, 0x07f0, 0x07f1) { X = 185, Y = 44 + 27 * 0, ButtonAction = ButtonAction.Activate });
                //OPTIONS BUTTON
                AddChildren(new Button((int)Buttons.Options, 0x07d6, 0x07d7, 0x07d8) { X = 185, Y = 44 + 27 * 1, ButtonAction = ButtonAction.Activate });
                // LOG OUT BUTTON
                AddChildren(new Button((int)Buttons.LogOut, 0x07d9, 0x07da, 0x07db) { X = 185, Y = 44 + 27 * 2, ButtonAction = ButtonAction.Activate });
                // QUESTS BUTTON
                AddChildren(new Button((int)Buttons.Quests, 0x57b5, 0x57b7, 0x57b6) { X = 185, Y = 44 + 27 * 3, ButtonAction = ButtonAction.Activate });
                // SKILLS BUTTON
                AddChildren(new Button((int)Buttons.Skills, 0x07df, 0x07e0, 0x07e1) { X = 185, Y = 44 + 27 * 4, ButtonAction = ButtonAction.Activate });
                // GUILD BUTTON
                AddChildren(new Button((int)Buttons.Guild, 0x57b2, 0x57b4, 0x57b3) { X = 185, Y = 44 + 27 * 5, ButtonAction = ButtonAction.Activate });
                // TOGGLE PEACE/WAR BUTTON
                _isWarMode = Mobile.InWarMode;
                ushort[] btngumps = _isWarMode ? WarModeBtnGumps : PeaceModeBtnGumps;
                AddChildren(_warModeBtn = new Button((int)Buttons.PeaceWarToggle, btngumps[0], btngumps[1], btngumps[2]) { X = 185, Y = 44 + 27 * 6, ButtonAction = ButtonAction.Activate });
                // STATUS BUTTON
                AddChildren(new Button((int)Buttons.Status, 0x07eb, 0x07ec, 0x07ed) { X = 185, Y = 44 + 27 * 7, ButtonAction = ButtonAction.Activate });
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
                AddChildren(new EquipmentSlot(2, 76)); //AddControl(new EquipmentSlot(this, 2, 76, Mobile, EquipLayer.Helm));
                AddChildren(new EquipmentSlot(2, 76 + 22 * 1)); //AddControl(new EquipmentSlot(this, 2, 76 + 22 * 1, Mobile, EquipLayer.Earrings));
                AddChildren(new EquipmentSlot(2, 76 + 22 * 2));//AddControl(new EquipmentSlot(this, 2, 76 + 22 * 2, Mobile, EquipLayer.Neck));
                AddChildren(new EquipmentSlot(2, 76 + 22 * 3)); //AddControl(new EquipmentSlot(this, 2, 76 + 22 * 3, Mobile, EquipLayer.Ring));
                AddChildren(new EquipmentSlot(2, 76 + 22 * 4));//AddControl(new EquipmentSlot(this, 2, 76 + 22 * 4, Mobile, EquipLayer.Bracelet));
                // Paperdoll control!
                AddChildren(new PaperDollInteractable( this,8, 21, Mobile));
                }
            else
            {
                AddChildren(new GumpPic(0, 0, 0x07d1, 0));
                // Paperdoll
                AddChildren(new PaperDollInteractable(this, 8, 21, Mobile));
            }
            
            // Name and title
            AddChildren(new HtmlGump(35, 260, 180, 42, string.Format("<span color=#222 style='font-family:uni0;'>{0}", Title), 0, 0, 0, true));

        }


        private void VirtueMenu_MouseDoubleClickEvent(object sender, MouseEventArgs args)
        {

            if (args.Button == MouseButton.Left)
            {
                Service.Get<Log>().Message(LogTypes.Warning, "Virtue DoubleClick event!!");
                //NetClient.Send(new GumpMenuSelectPacket(Mobile.Serial, 0x000001CD, 0x00000001, new int[1] { Mobile.Serial }, null));
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
