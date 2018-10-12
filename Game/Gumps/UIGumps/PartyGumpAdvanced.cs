﻿using System;
using System.Collections.Generic;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Game.Gumps.Controls.InGame;
using ClassicUO.Game.System;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Gumps.UIGumps
{
    internal class PartyGumpAdvanced : Gump
    {
        private readonly Label _createAddLabel;
        private readonly Button _leaveButton;
        private readonly Label _leaveLabel;
        private readonly Texture2D _line;
        private readonly Button _lootMeButton;
        private readonly Label _lootMeLabel;
        private readonly Button _messagePartyButton;
        private readonly Label _messagePartyLabel;
        private readonly Label _partyLeader;
        private readonly List<PartyListEntry> _partyListEntries;
        private readonly ScrollArea _scrollArea;

        public PartyGumpAdvanced() : base(0, 0)
        {
            _partyListEntries = new List<PartyListEntry>();
            _line = new Texture2D(Service.Get<SpriteBatch3D>().GraphicsDevice, 1, 1);
            _line.SetData(new[] {Color.White});

            X = 100;
            Y = 100;
            CanMove = true;
            AcceptMouseInput = false;

            AddChildren(new GameBorder(0, 0, 320, 400, 4));

            AddChildren(new GumpPicTiled(4, 4, 320 - 8, 400 - 8, 0x0A40) {IsTransparent = true});
            AddChildren(new GumpPicTiled(4, 4, 320 - 8, 400 - 8, 0x0A40) {IsTransparent = true});

            _scrollArea = new ScrollArea(20, 60, 295, 190, true) {AcceptMouseInput = true};
            AddChildren(_scrollArea);
            AddChildren(new Label("PM", true, 1153) {X = 30, Y = 25});
            AddChildren(new Label("Kick", true, 1153) {X = 60, Y = 25});
            AddChildren(new Label("Player", true, 1153) {X = 100, Y = 25});
            AddChildren(new Label("Status", true, 1153) {X = 250, Y = 25});
            //======================================================
            AddChildren(_messagePartyButton = new Button((int) Buttons.Message, 0xFAB, 0xFAC, 0xFAD)
                {X = 30, Y = 275, ButtonAction = ButtonAction.Activate, IsVisible = false});
            AddChildren(
                _messagePartyLabel = new Label("Message party", true, 1153) {X = 70, Y = 275, IsVisible = false});
            //======================================================
            AddChildren(_lootMeButton = new Button((int) Buttons.Loot, 0xFA2, 0xFA3, 0xFA4)
                {X = 30, Y = 300, ButtonAction = ButtonAction.Activate, IsVisible = false});
            AddChildren(_lootMeLabel = new Label("Party CANNOT loot me", true, 1153)
                {X = 70, Y = 300, IsVisible = false});
            //======================================================
            AddChildren(_leaveButton = new Button((int) Buttons.Leave, 0xFAE, 0xFAF, 0xFB0)
                {X = 30, Y = 325, ButtonAction = ButtonAction.Activate, IsVisible = false});
            AddChildren(_leaveLabel = new Label("Leave party", true, 1153) {X = 70, Y = 325, IsVisible = false});
            //======================================================
            AddChildren(new Button((int) Buttons.Add, 0xFA8, 0xFA9, 0xFAA)
                {X = 30, Y = 350, ButtonAction = ButtonAction.Activate});
            AddChildren(_createAddLabel = new Label("Add party member", true, 1153) {X = 70, Y = 350});
            //======================================================
            AddChildren(_partyLeader = new Label("Leader: ", true, 1161) {X = 180, Y = 275, IsVisible = false});

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
            spriteBatch.Draw2D(_line, new Rectangle((int) position.X + 30, (int) position.Y + 50, 260, 1),
                RenderExtentions.GetHueVector(0, false, .5f, false));
            spriteBatch.Draw2D(_line, new Rectangle((int) position.X + 95, (int) position.Y + 50, 1, 200),
                RenderExtentions.GetHueVector(0, false, .5f, false));
            spriteBatch.Draw2D(_line, new Rectangle((int) position.X + 245, (int) position.Y + 50, 1, 200),
                RenderExtentions.GetHueVector(0, false, .5f, false));
            spriteBatch.Draw2D(_line, new Rectangle((int) position.X + 30, (int) position.Y + 250, 260, 1),
                RenderExtentions.GetHueVector(0, false, .5f, false));
            return true;
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (!PartySystem.IsInParty)
            {
                _createAddLabel.Text = "Create a party";
                _leaveButton.IsVisible = false;
                _leaveLabel.IsVisible = false;
                _lootMeButton.IsVisible = false;
                _lootMeLabel.IsVisible = false;
                _messagePartyButton.IsVisible = false;
                _messagePartyLabel.IsVisible = false;
                _partyLeader.IsVisible = false;
            }
            else
            {
                _createAddLabel.Text = "Add a member";
                _leaveButton.IsVisible = true;
                _leaveLabel.IsVisible = true;
                _lootMeButton.IsVisible = true;
                _lootMeLabel.IsVisible = true;
                _lootMeLabel.Text = !PartySystem.AllowPartyLoot ? "Party CANNOT loot me" : "Party ALLOWED looting me";
                _messagePartyButton.IsVisible = true;
                _messagePartyLabel.IsVisible = true;
                _partyLeader.Text = PartySystem.LeaderName != string.Empty ? $"Leader: {PartySystem.LeaderName}" : "";
                _partyLeader.IsVisible = true;
            }


            base.Update(totalMS, frameMS);
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((Buttons) buttonID)
            {
                case Buttons.Add:
                    PartySystem.TriggerAddPartyMember();
                    break;
                case Buttons.Leave:
                    PartySystem.LeaveParty();
                    break;
                case Buttons.Loot:
                    PartySystem.AllowPartyLoot = !PartySystem.AllowPartyLoot ? true : false;
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
        public readonly Button KickButton;
        public readonly PartyMember Member;
        public readonly Label Name;
        public readonly Button PMButton;
        public readonly GumpPic Status;


        public PartyListEntry(PartyMember member)
        {
            Height = 20;
            Member = member;
            //======================================================
            Name = new Label(member.Name, true, 1153, font: 3);
            Name.X = 80;
            AddChildren(Name);
            //======================================================
            if (Member.Mobile.IsDead)
            {
                Status = new GumpPic(240, 0, 0x29F6, 0);
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
            KickButton = new Button((int) Buttons.Kick, 0xFB1, 0xFB2, 0xFB3)
                {X = 40, ButtonAction = ButtonAction.Activate};
            AddChildren(KickButton);
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((Buttons) buttonID)
            {
                case Buttons.Kick:
                    //
                    PartySystem.RemovePartyMember(Member.Serial);
                    break;
                case Buttons.PM:
                    //
                    var test2 = "";
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