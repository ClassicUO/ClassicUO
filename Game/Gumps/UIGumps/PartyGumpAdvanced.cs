using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Game.Gumps.Controls.InGame;
using ClassicUO.Game.System;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Gumps.UIGumps
{
    class PartyGumpAdvanced : Gump
    {
        private GameBorder _gameBorder;
        private GumpPicTiled _gumpPicTiled;
        private ScrollArea _scrollArea;
        private Texture2D _line;
        private List<PartyListEntry> _partyListEntries;
        private Label _createAddLabel;
        private Label _leaveLabel;
        private Button _leaveButton;
        private Label _lootMeLabel;
        private Button _lootMeButton;
        private Label _messagePartyLabel;
        private Button _messagePartyButton;
        private Button _createAddButton;

        public PartyGumpAdvanced() : base(0, 0)
        {
            _partyListEntries = new List<PartyListEntry>();
            _line = new Texture2D(Service.Get<SpriteBatch3D>().GraphicsDevice, 1, 1);
            _line.SetData(new[] { Color.White });

            X = 100;
            Y = 100;
            CanMove = true;
            AcceptMouseInput = false;

            AddChildren(_gameBorder = new GameBorder(0, 0, 320, 400));

            AddChildren(_gumpPicTiled = new GumpPicTiled(4, 6, 320 - 8, 400 - 12, 0x0A40) { IsTransparent = true });
            AddChildren(_gumpPicTiled);

            _scrollArea = new ScrollArea(20, 60, 295, 190, true) { AcceptMouseInput = true };
            AddChildren(_scrollArea);
            AddChildren(new Label("PM", true, 1153) { X = 30, Y = 25 });
            AddChildren(new Label("Kick", true, 1153) { X = 60, Y = 25 });
            AddChildren(new Label("Player", true, 1153) { X = 100, Y = 25 });
            AddChildren(new Label("Status", true, 1153) { X = 250, Y = 25 });
            //======================================================
            AddChildren(_messagePartyButton = new Button((int)Buttons.Message, 0xFAB, 0xFAC, 0xFAD) { X = 30, Y = 275, ButtonAction = ButtonAction.Activate, IsVisible = false });
            AddChildren(_messagePartyLabel = new Label("Message party", true, 1153) { X = 70, Y = 275, IsVisible = false });
            //======================================================
            AddChildren(_lootMeButton = new Button((int)Buttons.Loot, 0xFA2, 0xFA3, 0xFA4) { X = 30, Y = 300, ButtonAction = ButtonAction.Activate, IsVisible = false });
            AddChildren(_lootMeLabel = new Label("Party CANNOT loot me", true, 1153) { X = 70, Y = 300, IsVisible = false });
            //======================================================
            AddChildren(_leaveButton = new Button((int)Buttons.Leave, 0xFAE, 0xFAF, 0xFB0) { X = 30, Y = 325, ButtonAction = ButtonAction.Activate, IsVisible = false});
            AddChildren(_leaveLabel = new Label("Leave party", true, 1153) { X = 70, Y = 325, IsVisible = false});
            //======================================================
            AddChildren(_createAddButton = new Button((int)Buttons.Add, 0xFA8, 0xFA9, 0xFAA) { X = 30, Y = 350, ButtonAction = ButtonAction.Activate });
            AddChildren(_createAddLabel = new Label("Add party member", true, 1153) { X = 70, Y = 350 });
            //======================================================
           

            PartySystem.PartyMemberChanged += OnPartyMemberChanged;

        }

        private void OnPartyMemberChanged(object sender, EventArgs e)
        {
            OnInitialize();
        }

        protected override void OnInitialize()
        {
            _scrollArea.Clear();

            foreach (var entry in _partyListEntries)
            {
                entry.Clear();
                entry.Dispose();
            }
            _partyListEntries.Clear();

            foreach (PartyMember member in PartySystem.Members)
            {

                _partyListEntries.Add(new PartyListEntry(member));
            }

            for (int i = 0; i < _partyListEntries.Count; i++)
            {
                _scrollArea.AddChildren(_partyListEntries[i]);
            }

        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
        {
            base.Draw(spriteBatch, position, hue);
            spriteBatch.Draw2D(_line, new Rectangle((int)position.X + 30, (int)position.Y + 50, 260, 1), RenderExtentions.GetHueVector(0, false, .5f, false));
            spriteBatch.Draw2D(_line, new Rectangle((int)position.X + 95, (int)position.Y + 50, 1, 200), RenderExtentions.GetHueVector(0, false, .5f, false));
            spriteBatch.Draw2D(_line, new Rectangle((int)position.X + 245, (int)position.Y + 50, 1, 200), RenderExtentions.GetHueVector(0, false, .5f, false));
            spriteBatch.Draw2D(_line, new Rectangle((int)position.X + 30, (int)position.Y + 250, 260, 1), RenderExtentions.GetHueVector(0, false, .5f, false));
            return true;

        }

        public override void Update(double totalMS, double frameMS)
        {
            if (!PartySystem.IsInParty)
            {
                //Set gump size if player is NOT in party
                _gameBorder.Height = 320;
                _gumpPicTiled.Height = _gameBorder.Height - 12;
                //Set contents if player is NOT in party
                _createAddButton.Y = 270;
                _createAddLabel.Y = _createAddButton.Y;
                _createAddLabel.Text = "Create a party";
                _leaveButton.IsVisible = false;
                _leaveLabel.IsVisible = false;
                _lootMeButton.IsVisible = false;
                _lootMeLabel.IsVisible = false;
                _messagePartyButton.IsVisible = false;
                _messagePartyLabel.IsVisible = false;
               
            }
            else
            {
                //Set gump size if player is in party
                _gameBorder.Height = 400;
                _gumpPicTiled.Height = _gameBorder.Height - 12;
                //Set contents if player is in party
                _createAddButton.Y = 350;
                _createAddLabel.Y = _createAddButton.Y;
                _createAddLabel.Text = "Add a member";
                _leaveButton.IsVisible = true;
                _leaveLabel.IsVisible = true;
                _lootMeButton.IsVisible = true;
                _lootMeLabel.IsVisible = true;
                _lootMeLabel.Text = (!PartySystem.AllowPartyLoot) ? "Party CANNOT loot me" : "Party ALLOWED looting me";
                _messagePartyButton.IsVisible = true;
                _messagePartyLabel.IsVisible = true;
                
            }

            
            base.Update(totalMS, frameMS);
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((Buttons)buttonID)
            {
                case Buttons.Add:
                    PartySystem.TriggerAddPartyMember();
                    break;
                case Buttons.Leave:
                    PartySystem.LeaveParty();
                    break;
                case Buttons.Loot:
                    PartySystem.AllowPartyLoot = (!PartySystem.AllowPartyLoot) ? true : false;
                    break;
                case Buttons.Message:
                    //
                    break;

            }
        }
        private enum Buttons
        {
            Add = 1,
            Leave,
            Loot,
            Message
        }
    }



    public class PartyListEntry : GumpControl
    {
        public readonly Button PMButton;
        public readonly Button KickButton;
        public readonly Label Name;
        public readonly GumpPic Status;
        public readonly PartyMember Member;
       


        public PartyListEntry(PartyMember member) 
        {
           
            Height = 20;
            Member = member;
            //======================================================
            //Name = new Label(member.Name, true, 1153, font:3);
            //Name.X = 80;
            Name = (PartySystem.Leader == member.Serial)
                ? new Label(member.Name+"(L)", true, 1161, font: 3) {X = 80}
                : new Label(member.Name, true, 1153, font: 3) {X = 80};
           
            AddChildren(Name);
            //======================================================
            if (Member.Mobile.IsDead)
            {
                Status = new GumpPic(240, 0, 0x29F6,0);
            }
            else
            {
                Status = new GumpPic(240, 0, 0x29F6, 0x43);
            }
            AddChildren(Status);
            //======================================================
            PMButton = new Button((int) Buttons.PM, 0xFBD, 0xFBE, 0xFBF)
                {X = 10, ButtonAction = ButtonAction.Activate};
            AddChildren(PMButton);
            //======================================================
            KickButton = new Button((int)Buttons.Kick, 0xFB1, 0xFB2, 0xFB3)
                { X = 40, ButtonAction = ButtonAction.Activate };
            AddChildren(KickButton);


        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((Buttons)buttonID)
            {
                case Buttons.Kick:
                    //
                    PartySystem.RemovePartyMember(Member.Serial);
                    break;
                case Buttons.PM:
                    if (UIManager.GetByLocalSerial<PartyMemberGump>() == null)
                    {
                        UIManager.Add(new PartyMemberGump(Member));
                    }
                    else if (UIManager.GetByLocalSerial<PartyMemberGump>() != null && !UIManager.GetByLocalSerial<PartyMemberGump>().IsMemberGumpActive(Member))
                    {
                        UIManager.Add(new PartyMemberGump(Member));
                    }
                        
                    
                    break;

            }
        }

        private enum Buttons
        {
            Kick = 1,
            PM
        }






    }
}
