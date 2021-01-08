#region license

// Copyright (C) 2020 project dust765
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

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.Managers;

using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Data;
using ClassicUO.IO.Resources;

namespace ClassicUO.Game.InteropServices.Runtime.UOClassicCombat
{
    internal class UOClassicCombatSelf : Gump
    {
        //MAIN UI CONSTRUCT
        private readonly AlphaBlendControl _background;
        private readonly Label _title;

        //UI
        private Label _uiTimerAutoBandage, _uiTimerAutoPouche, _uiTimerAutoCurepot, _uiTimerAutoHealpot, _uiTimerAutoRefreshpot, _uiTimerAutoRearmAfterDisarmed, _uiTimerStrengthpot, _uiTimerDexpot;
        private Label _uiTextAutoBandage, _uiTextAutoPouche, _uiTextAutoCurepot, _uiTextAutoHealpot, _uiTextAutoRefreshpot;
        private Checkbox _uiCboxAutoBandage, _uiCboxAutoPouche, _uiCboxAutoCurepot, _uiCboxAutoHealpot, _uiCboxAutoRefreshpot, _uiCboxRearmAfterPot, _uiCboxAutoRearmAfterDisarmed, _uiCboxConsiderHidden, _uiCboxConsiderSpells, _uiCboxIsDuelingOrTankMage; //NEW _uiCboxIsDuelingOrTankMage
        private TextureControl _uiIconAutoBandage, _uiIconAutoPouche, _uiIconAutoCurepot, _uiIconAutoHealpot, _uiIconAutoRefreshpot, _uiIconStrengthpot, _uiIconDexpot, _uiIconAutoRearmAfterDisarmed, _uiIconConsiderHidden, _uiIconConsiderSpells;
        private Label _uiTextGotDisarmed, _uiTextGotHamstrung;
        private Label _uiTimerGotDisarmed, _uiTimerGotHamstrung;
        private Label _uiTextTimerDoDisarm, _uiTextTimerDoHamstring;
        private Label _uiTextTracking;

        //CONSTANTS
        private const byte FONT = 0xFF;
        private const ushort HUE_FONT = 999, HUE_FONTS_YELLOW = 0x35, HUE_FONTS_RED = 0x26, HUE_FONTS_GREEN = 0x3F, HUE_FONTS_BLUE = 0x5D;
        //MAIN UI CONSTRUCT

        //GENERAL GAME DELAYS BETWEEN TWO ACTIONS
        private uint _tickLastActionTime;

        //AUTOBANDAGE
        private uint _tickStartAutoBandage; //startpoint of timer after bandies were applied
        private bool _useBandiesTime; //for non outlands, use count up timer
        private uint _timerAutoBandage { get; set; } //the timer after bandies were applied
        private long _tickWaitForTarget { get; set; } //for non outlands, oldbandies, tick for _useWaitForTarget
        private bool _useWaitForTarget { get; set; } //for non outlands, oldbandies, use wait for target cursor (after doublclicks bandies this is set true)
        //AUTOBANDAGE

        //AUTOPOUCHE
        private uint _tickStartAutoPouche; //startpoint of timer after pouche used
        private uint _timerAutoPouche { get; set; } //the timer between pouches
        //AUTOPOUCHE

        //AUTOCURE
        private uint _tickStartAutoCurepot;
        private uint _timerAutoCurepot { get; set; }
        //AUTOCURE

        //AUTOHEAL
        private uint _tickStartAutoHealpot;
        private uint _timerAutoHealpot { get; set; }
        //AUTOHEAL

        //AUTOREFRESH
        private uint _tickStartAutoRefreshpot;
        private uint _timerAutoRefreshpot { get; set; }
        //AUTOREFRESH

        //REARM WEP AFTER POT
        private Item _tempItemInLeftHand; //temp var, wep that was in hand for re equip
        private Item _tempItemInRightHand;
        private Item _tempItemEquipAfterPot;
        private bool _doRearmAfterPot;
        private bool _disarmFromTrigger; //Makes no rearm is issued

        //DISARMED
        private uint _timerGotDisarmed; //countdown timer
        public uint _tickGotDisarmed { get; set; }
        //AUTO REARM AFTER GOT DISARMED
        public uint _tickGotDisarmedAutoRearmAfterDisarmed { get; set; }
        private uint _timerAutoRearmAfterDisarmed; //countdown timer
        private bool _doAutoRearmAfterDisarmed;
        private Item _itemAutoRearmAfterDisarmedItemLastInLeftHand;
        private Item _itemAutoRearmAfterDisarmedItemLastInRightHand;
        private uint _tickAutoRearmAfterDisarmedItemLastInLeftHand;
        private uint _tickAutoRearmAfterDisarmedItemLastInRightHand;
        private uint _tickLastAutoRearmAfterDisarmedItemSave = Time.Ticks;
        private uint _tickLastAutoRearmAfterDisarmedMessage = Time.Ticks;
        //ARM / REARM WEP

        //HAMSTRUNG
        private uint _timerGotHamstrung; //countdown timer
        public uint _tickGotHamstrung { get; set; }
        //HAMSTRUNG

        //DO DISARM
        private uint _timerDoDisarm; //countdown timer
        private uint _tickDoDisarmStriked { get; set; }
        private uint _tickDoDisarmFailed { get; set; }
        //DO DISARM

        //DO HAMSTRING
        private uint _timerDoHamstring; //countdown timer
        private uint _tickDoHamstringStriked { get; set; }
        private uint _tickDoHamstringFailed { get; set; }
        //DO HAMSTRING

        //MACROPOT
        private ushort _lastMacroPot { get; set; }
        private uint _tickStartStrengthpot;
        private uint _timerStrengthpot { get; set; }
        private uint _tickStartDexpot;
        private uint _timerDexpot { get; set; }
        //MACROPOT

        //OPTIONS TO VARS
        private bool UCCS_AutoBandage = ProfileManager.CurrentProfile.UOClassicCombatSelf_AutoBandage;

        private bool UCCS_AutoPouche = ProfileManager.CurrentProfile.UOClassicCombatSelf_AutoPouche;
        private uint UCCS_PoucheCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_PoucheCooldown;

        private bool UCCS_AutoCurepot = ProfileManager.CurrentProfile.UOClassicCombatSelf_AutoCurepot;
        private uint UCCS_CurepotCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_CurepotCooldown;

        private bool UCCS_AutoHealpot = ProfileManager.CurrentProfile.UOClassicCombatSelf_AutoHealpot;
        private uint UCCS_HealpotCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_HealpotCooldown;

        private bool UCCS_AutoRefreshpot = ProfileManager.CurrentProfile.UOClassicCombatSelf_AutoRefreshpot;
        private uint UCCS_RefreshpotCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_RefreshpotCooldown;

        private bool UCCS_RearmAfterPot = ProfileManager.CurrentProfile.UOClassicCombatSelf_RearmAfterPot;

        private bool UCCS_IsDuelingOrTankMage = ProfileManager.CurrentProfile.UOClassicCombatSelf_IsDuelingOrTankMage;

        private uint UCCS_ActionCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_ActionCooldown;
        private uint UCCS_UOClassicCombatSelf_WaitForTarget = ProfileManager.CurrentProfile.UOClassicCombatSelf_WaitForTarget;

        private uint UCCS_BandiesHPTreshold = ProfileManager.CurrentProfile.UOClassicCombatSelf_BandiesHPTreshold;
        private bool UCCS_BandiesPoison = ProfileManager.CurrentProfile.UOClassicCombatSelf_BandiesPoison;
        private uint UCCS_CurepotHPTreshold = ProfileManager.CurrentProfile.UOClassicCombatSelf_CurepotHPTreshold;
        private uint UCCS_HealpotHPTreshold = ProfileManager.CurrentProfile.UOClassicCombatSelf_HealpotHPTreshold;
        private uint UCCS_RefreshpotStamTreshold = ProfileManager.CurrentProfile.UOClassicCombatSelf_RefreshpotStamTreshold;

        private bool UCCS_AutoRearmAfterDisarmed = ProfileManager.CurrentProfile.UOClassicCombatSelf_AutoRearmAfterDisarmed;
        private uint UCCS_AutoRearmAfterDisarmedCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_AutoRearmAfterDisarmedCooldown;

        private bool UCCS_NoRefreshPotAfterHamstrung = ProfileManager.CurrentProfile.UOClassicCombatSelf_NoRefreshPotAfterHamstrung;
        private uint UCCS_NoRefreshPotAfterHamstrungCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_NoRefreshPotAfterHamstrungCooldown;

        private uint UCCS_DisarmStrikeCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_DisarmStrikeCooldown;
        private uint UCCS_DisarmAttemptCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_DisarmAttemptCooldown;

        private uint UCCS_HamstringStrikeCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_HamstringStrikeCooldown;
        private uint UCCS_HamstringAttemptCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_HamstringAttemptCooldown;

        private uint UCCS_DisarmedCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_DisarmedCooldown;
        private uint UCCS_HamstrungCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_HamstrungCooldown;

        private bool UCCS_ConsiderHidden = ProfileManager.CurrentProfile.UOClassicCombatSelf_ConsiderHidden;
        private bool UCCS_ConsiderSpells = ProfileManager.CurrentProfile.UOClassicCombatSelf_ConsiderSpells;

        private int UCCS_MinRNG = ProfileManager.CurrentProfile.UOClassicCombatSelf_MinRNG;
        private int UCCS_MaxRNG = ProfileManager.CurrentProfile.UOClassicCombatSelf_MaxRNG;

        private bool UCCS_ClilocTriggers = ProfileManager.CurrentProfile.UOClassicCombatSelf_ClilocTriggers;
        private bool UCCS_MacroTriggers = ProfileManager.CurrentProfile.UOClassicCombatSelf_MacroTriggers;

        private uint UCCS_StrengthpotCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_StrengthPotCooldown;
        private uint UCCS_DexpotCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_DexPotCooldown;

        //OPTIONS TO VARS END

        //VARS
        private int diffhits = 0;
        private int diffstam = 0;
        private uint logondelay = Time.Ticks;

        //RNG
        private uint _tickLastRNGCalced = Time.Ticks;
        private int _varRNGtoWait = 0;
        private bool _waitRNG;
        private uint _tickWaitRNG = Time.Ticks;
        private bool _doneWaitRNG;

        public UOClassicCombatSelf() : base(0, 0)
        {
            CanMove = true;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = false;
            AcceptMouseInput = true;
            AcceptKeyboardInput = false;

            //MAIN CONSTRUCT
            Width = 95;
            Height = 280;

            Add(_background = new AlphaBlendControl()
            {
                Alpha = 0.6f,
                Width = Width,
                Height = Height
            });
            Add(_background);

            _title = new Label("UCC -SELF-", true, HUE_FONTS_BLUE, 0, 1, FontStyle.BlackBorder)
            {
                X = 2,
                Y = 2
            };
            Add(_title);

            //MAIN CONSTRUCT

            //AUTOBANDAGE
            _uiIconAutoBandage = new TextureControl()
            {
                AcceptMouseInput = false
            };

            _uiIconAutoBandage.Texture = ArtLoader.Instance.GetTexture(0x0E21); //bandies
            _uiIconAutoBandage.Hue = 0;
            _uiIconAutoBandage.X = -13;
            _uiIconAutoBandage.Y = _title.Bounds.Bottom - 5;
            _uiIconAutoBandage.Width = _uiIconAutoBandage.Texture.Width;
            _uiIconAutoBandage.Height = _uiIconAutoBandage.Texture.Height;
            Add(_uiIconAutoBandage);

            _uiTimerAutoBandage = new Label($"{_timerAutoBandage}", true, HUE_FONTS_YELLOW, 0, 1, FontStyle.BlackBorder)
            {
                X = _uiIconAutoBandage.Bounds.Right,
                Y = _uiIconAutoBandage.Y + 7
            };
            Add(_uiTimerAutoBandage);

            _uiTextAutoBandage = new Label("OFF", true, HUE_FONTS_RED, 0, 1, FontStyle.BlackBorder)
            {
                X = _uiTimerAutoBandage.Bounds.Right,
                Y = _uiIconAutoBandage.Y + 7
            };
            Add(_uiTextAutoBandage);

            _uiCboxAutoBandage = new Checkbox(0x00D2, 0x00D3, "", FONT, HUE_FONT)
            {
                X = _uiTextAutoBandage.Bounds.Right,
                Y = _uiIconAutoBandage.Y + 8,
                IsChecked = ProfileManager.CurrentProfile.UOClassicCombatSelf_AutoBandage
            };
            _uiCboxAutoBandage.ValueChanged += (sender, e) =>
            {
                ProfileManager.CurrentProfile.UOClassicCombatSelf_AutoBandage = _uiCboxAutoBandage.IsChecked;
                UpdateVars();
            };
            Add(_uiCboxAutoBandage);
            //AUTOBANDAGE

            //AUTOPOUCHE
            _uiIconAutoPouche = new TextureControl()
            {
                AcceptMouseInput = false
            };

            _uiIconAutoPouche.Texture = ArtLoader.Instance.GetTexture(0x0E79);
            _uiIconAutoPouche.Hue = 0x0026;
            _uiIconAutoPouche.X = -13;
            _uiIconAutoPouche.Y = _uiIconAutoBandage.Bounds.Bottom - 11;
            _uiIconAutoPouche.Width = _uiIconAutoPouche.Texture.Width;
            _uiIconAutoPouche.Height = _uiIconAutoPouche.Texture.Height;
            Add(_uiIconAutoPouche);

            _uiTimerAutoPouche = new Label($"{_timerAutoPouche}", true, HUE_FONTS_YELLOW, 0, 1, FontStyle.BlackBorder)
            {
                X = _uiIconAutoPouche.Bounds.Right,
                Y = _uiIconAutoPouche.Y + 3
            };
            Add(_uiTimerAutoPouche);

            _uiTextAutoPouche = new Label("OFF", true, HUE_FONTS_RED, 0, 1, FontStyle.BlackBorder)
            {
                X = _uiTimerAutoPouche.Bounds.Right,
                Y = _uiIconAutoPouche.Y + 3
            };
            Add(_uiTextAutoPouche);

            _uiCboxAutoPouche = new Checkbox(0x00D2, 0x00D3, "", FONT, HUE_FONT)
            {
                X = _uiTextAutoPouche.Bounds.Right,
                Y = _uiIconAutoPouche.Y + 4,
                IsChecked = ProfileManager.CurrentProfile.UOClassicCombatSelf_AutoPouche
            };
            _uiCboxAutoPouche.ValueChanged += (sender, e) =>
            {
                ProfileManager.CurrentProfile.UOClassicCombatSelf_AutoPouche = _uiCboxAutoPouche.IsChecked;
                UpdateVars();
            };
            Add(_uiCboxAutoPouche);
            //AUTOPOUCHE

            //AUTOCUREPOT
            _uiIconAutoCurepot = new TextureControl()
            {
                AcceptMouseInput = false
            };

            _uiIconAutoCurepot.Texture = ArtLoader.Instance.GetTexture(0x0F07); //curepot
            _uiIconAutoCurepot.Hue = 0;
            _uiIconAutoCurepot.X = -13;
            _uiIconAutoCurepot.Y = _uiIconAutoPouche.Bounds.Bottom - 7;
            _uiIconAutoCurepot.Width = _uiIconAutoCurepot.Texture.Width;
            _uiIconAutoCurepot.Height = _uiIconAutoCurepot.Texture.Height;
            Add(_uiIconAutoCurepot);

            _uiTimerAutoCurepot = new Label($"{_timerAutoCurepot}", true, HUE_FONTS_YELLOW, 0, 1, FontStyle.BlackBorder)
            {
                X = _uiIconAutoCurepot.Bounds.Right,
                Y = _uiIconAutoCurepot.Y + 4
            };
            Add(_uiTimerAutoCurepot);

            _uiTextAutoCurepot = new Label("OFF", true, HUE_FONTS_RED, 0, 1, FontStyle.BlackBorder)
            {
                X = _uiTimerAutoCurepot.Bounds.Right,
                Y = _uiIconAutoCurepot.Y + 4
            };
            Add(_uiTextAutoCurepot);

            _uiCboxAutoCurepot = new Checkbox(0x00D2, 0x00D3, "", FONT, HUE_FONT)
            {
                X = _uiTextAutoCurepot.Bounds.Right,
                Y = _uiIconAutoCurepot.Y + 5,
                IsChecked = ProfileManager.CurrentProfile.UOClassicCombatSelf_AutoCurepot
            };
            _uiCboxAutoCurepot.ValueChanged += (sender, e) =>
            {
                ProfileManager.CurrentProfile.UOClassicCombatSelf_AutoCurepot = _uiCboxAutoCurepot.IsChecked;
                UpdateVars();
            };
            Add(_uiCboxAutoCurepot);
            //AUTOCUREPOT

            //AUTOHEALPOT
            _uiIconAutoHealpot = new TextureControl()
            {
                AcceptMouseInput = false
            };

            _uiIconAutoHealpot.Texture = ArtLoader.Instance.GetTexture(0x0F0C); //healpot
            _uiIconAutoHealpot.Hue = 0;
            _uiIconAutoHealpot.X = -13;
            _uiIconAutoHealpot.Y = _uiIconAutoCurepot.Bounds.Bottom - 14;
            _uiIconAutoHealpot.Width = _uiIconAutoHealpot.Texture.Width;
            _uiIconAutoHealpot.Height = _uiIconAutoHealpot.Texture.Height;
            Add(_uiIconAutoHealpot);

            _uiTimerAutoHealpot = new Label($"{_timerAutoHealpot}", true, HUE_FONTS_YELLOW, 0, 1, FontStyle.BlackBorder)
            {
                X = _uiIconAutoHealpot.Bounds.Right,
                Y = _uiIconAutoHealpot.Y + 10
            };
            Add(_uiTimerAutoHealpot);

            _uiTextAutoHealpot = new Label("OFF", true, HUE_FONTS_RED, 0, 1, FontStyle.BlackBorder)
            {
                X = _uiTimerAutoHealpot.Bounds.Right,
                Y = _uiIconAutoHealpot.Y + 10
            };
            Add(_uiTextAutoHealpot);

            _uiCboxAutoHealpot = new Checkbox(0x00D2, 0x00D3, "", FONT, HUE_FONT)
            {
                X = _uiTextAutoHealpot.Bounds.Right,
                Y = _uiIconAutoHealpot.Y + 11,
                IsChecked = ProfileManager.CurrentProfile.UOClassicCombatSelf_AutoHealpot
            };
            _uiCboxAutoHealpot.ValueChanged += (sender, e) =>
            {
                ProfileManager.CurrentProfile.UOClassicCombatSelf_AutoHealpot = _uiCboxAutoHealpot.IsChecked;
                UpdateVars();
            };
            Add(_uiCboxAutoHealpot);
            //AUTOHEALPOT

            //AUTOREFRESHPOT
            _uiIconAutoRefreshpot = new TextureControl()
            {
                AcceptMouseInput = false
            };

            _uiIconAutoRefreshpot.Texture = ArtLoader.Instance.GetTexture(0xF0B); //refreshpot
            _uiIconAutoRefreshpot.Hue = 0;
            _uiIconAutoRefreshpot.X = -13;
            _uiIconAutoRefreshpot.Y = _uiIconAutoHealpot.Bounds.Bottom - 19;
            _uiIconAutoRefreshpot.Width = _uiIconAutoRefreshpot.Texture.Width;
            _uiIconAutoRefreshpot.Height = _uiIconAutoRefreshpot.Texture.Height;
            Add(_uiIconAutoRefreshpot);

            _uiTimerAutoRefreshpot = new Label($"{_timerAutoRefreshpot}", true, HUE_FONTS_YELLOW, 0, 1, FontStyle.BlackBorder)
            {
                X = _uiIconAutoRefreshpot.Bounds.Right,
                Y = _uiIconAutoRefreshpot.Y + 7
            };
            Add(_uiTimerAutoRefreshpot);

            _uiTextAutoRefreshpot = new Label("OFF", true, HUE_FONTS_RED, 0, 1, FontStyle.BlackBorder)
            {
                X = _uiTimerAutoRefreshpot.Bounds.Right,
                Y = _uiIconAutoRefreshpot.Y + 7
            };
            Add(_uiTextAutoRefreshpot);

            _uiCboxAutoRefreshpot = new Checkbox(0x00D2, 0x00D3, "", FONT, HUE_FONT)
            {
                X = _uiTextAutoRefreshpot.Bounds.Right,
                Y = _uiIconAutoRefreshpot.Y + 8,
                IsChecked = ProfileManager.CurrentProfile.UOClassicCombatSelf_AutoRefreshpot
            };
            _uiCboxAutoRefreshpot.ValueChanged += (sender, e) =>
            {
                ProfileManager.CurrentProfile.UOClassicCombatSelf_AutoRefreshpot = _uiCboxAutoRefreshpot.IsChecked;
                UpdateVars();
            };
            Add(_uiCboxAutoRefreshpot);
            //AUTOREFRESHPOT

            //STRENGTH
            _uiIconStrengthpot = new TextureControl()
            {
                AcceptMouseInput = false
            };

            _uiIconStrengthpot.Texture = ArtLoader.Instance.GetTexture(0xF09); //strength
            _uiIconStrengthpot.Hue = 0;
            _uiIconStrengthpot.X = -13;
            _uiIconStrengthpot.Y = _uiTextAutoRefreshpot.Bounds.Bottom;
            _uiIconStrengthpot.Width = _uiIconStrengthpot.Texture.Width;
            _uiIconStrengthpot.Height = _uiIconStrengthpot.Texture.Height;
            Add(_uiIconStrengthpot);

            _uiTimerStrengthpot = new Label($"{_timerStrengthpot}", true, HUE_FONTS_YELLOW, 0, 1, FontStyle.BlackBorder)
            {
                X = _uiIconStrengthpot.Bounds.Right,
                Y = _uiIconStrengthpot.Y + 7
            };
            Add(_uiTimerStrengthpot);
            //STRENGTH
            //DEX
            _uiIconDexpot = new TextureControl()
            {
                AcceptMouseInput = false
            };

            _uiIconDexpot.Texture = ArtLoader.Instance.GetTexture(0xF08); //agility
            _uiIconDexpot.Hue = 0;
            _uiIconDexpot.X = -13;
            _uiIconDexpot.Y = _uiIconStrengthpot.Bounds.Bottom - 19;
            _uiIconDexpot.Width = _uiIconDexpot.Texture.Width;
            _uiIconDexpot.Height = _uiIconDexpot.Texture.Height;
            Add(_uiIconDexpot);

            _uiTimerDexpot = new Label($"{_timerDexpot}", true, HUE_FONTS_YELLOW, 0, 1, FontStyle.BlackBorder)
            {
                X = _uiIconDexpot.Bounds.Right,
                Y = _uiIconDexpot.Y + 7
            };
            Add(_uiTimerDexpot);
            //DEX

            //Duelist Option
            _uiCboxIsDuelingOrTankMage = new Checkbox(0x00D2, 0x00D3, "Duelist Rules", FONT, HUE_FONT)
            {
                X = 0,
                Y = _uiIconDexpot.Bounds.Bottom,
                IsChecked = ProfileManager.CurrentProfile.UOClassicCombatSelf_IsDuelingOrTankMage
            };
            _uiCboxIsDuelingOrTankMage.ValueChanged += (sender, e) =>
            {
                ProfileManager.CurrentProfile.UOClassicCombatSelf_IsDuelingOrTankMage = _uiCboxIsDuelingOrTankMage.IsChecked;
                UpdateVars();
            };
            Add(_uiCboxIsDuelingOrTankMage);
            //Duelist Option
            //DISARM / ARM
            _uiCboxRearmAfterPot = new Checkbox(0x00D2, 0x00D3, "Rearm Pot", FONT, HUE_FONT)
            {
                X = 0,
                Y = _uiIconDexpot.Bounds.Bottom + 20,
                IsChecked = ProfileManager.CurrentProfile.UOClassicCombatSelf_RearmAfterPot
            };
            _uiCboxRearmAfterPot.ValueChanged += (sender, e) =>
            {
                ProfileManager.CurrentProfile.UOClassicCombatSelf_RearmAfterPot = _uiCboxRearmAfterPot.IsChecked;
                UpdateVars();
            };
            Add(_uiCboxRearmAfterPot);
            //DISARM / ARM

            //AUTO REAM AFTER DISARMED
            _uiIconAutoRearmAfterDisarmed = new TextureControl()
            {
                AcceptMouseInput = false
            };

            _uiIconAutoRearmAfterDisarmed.Texture = ArtLoader.Instance.GetTexture(0x0BC0); //armourer
            _uiIconAutoRearmAfterDisarmed.Hue = 0;
            _uiIconAutoRearmAfterDisarmed.X = 0;
            _uiIconAutoRearmAfterDisarmed.Y = _uiCboxRearmAfterPot.Bounds.Bottom + 2;
            _uiIconAutoRearmAfterDisarmed.Width = 20;
            _uiIconAutoRearmAfterDisarmed.Height = 20;
            Add(_uiIconAutoRearmAfterDisarmed);

            _uiTimerAutoRearmAfterDisarmed = new Label($"{_timerGotDisarmed}", true, HUE_FONTS_YELLOW, 0, 1, FontStyle.BlackBorder)
            {
                X = _uiIconAutoRearmAfterDisarmed.Bounds.Right + 10,
                Y = _uiIconAutoRearmAfterDisarmed.Y
            };
            Add(_uiTimerAutoRearmAfterDisarmed);

            _uiCboxAutoRearmAfterDisarmed = new Checkbox(0x00D2, 0x00D3, "", FONT, HUE_FONT)
            {
                X = _uiTimerAutoRearmAfterDisarmed.Bounds.Right + 10,
                Y = _uiIconAutoRearmAfterDisarmed.Y + 1,
                IsChecked = ProfileManager.CurrentProfile.UOClassicCombatSelf_AutoRearmAfterDisarmed
            };
            _uiCboxAutoRearmAfterDisarmed.ValueChanged += (sender, e) =>
            {
                ProfileManager.CurrentProfile.UOClassicCombatSelf_AutoRearmAfterDisarmed = _uiCboxAutoRearmAfterDisarmed.IsChecked;
                UpdateVars();
            };
            Add(_uiCboxAutoRearmAfterDisarmed);
            //AUTO REAM AFTER DISARMED

            //CONSIDER HIDDEN
            _uiIconConsiderHidden = new TextureControl()
            {
                AcceptMouseInput = false
            };

            _uiIconConsiderHidden.Texture = ArtLoader.Instance.GetTexture(0x0C00); //assasins guild
            _uiIconConsiderHidden.Hue = 0;
            _uiIconConsiderHidden.X = 0;
            _uiIconConsiderHidden.Y = _uiIconAutoRearmAfterDisarmed.Bounds.Bottom + 4;
            _uiIconConsiderHidden.Width = 20;
            _uiIconConsiderHidden.Height = 20;
            Add(_uiIconConsiderHidden);

            _uiCboxConsiderHidden = new Checkbox(0x00D2, 0x00D3, "", FONT, HUE_FONT)
            {
                X = _uiIconConsiderHidden.Bounds.Right + 10,
                Y = _uiIconConsiderHidden.Y,
                IsChecked = ProfileManager.CurrentProfile.UOClassicCombatSelf_ConsiderHidden
            };
            _uiCboxConsiderHidden.ValueChanged += (sender, e) =>
            {
                ProfileManager.CurrentProfile.UOClassicCombatSelf_ConsiderHidden = _uiCboxConsiderHidden.IsChecked;
                UpdateVars();
            };
            Add(_uiCboxConsiderHidden);
            //CONSIDER HIDDEN

            //CONSIDER SPELLS
            _uiIconConsiderSpells = new TextureControl()
            {
                AcceptMouseInput = false
            };

            _uiIconConsiderSpells.Texture = ArtLoader.Instance.GetTexture(0x0BAD); //mage shop
            _uiIconConsiderSpells.Hue = 0;
            _uiIconConsiderSpells.X = 0;
            _uiIconConsiderSpells.Y = _uiIconConsiderHidden.Bounds.Bottom + 4;
            _uiIconConsiderSpells.Width = 20;
            _uiIconConsiderSpells.Height = 20;
            Add(_uiIconConsiderSpells);

            _uiCboxConsiderSpells = new Checkbox(0x00D2, 0x00D3, "", FONT, HUE_FONT)
            {
                X = _uiIconConsiderSpells.Bounds.Right + 10,
                Y = _uiIconConsiderSpells.Y,
                IsChecked = ProfileManager.CurrentProfile.UOClassicCombatSelf_ConsiderSpells
            };
            _uiCboxConsiderSpells.ValueChanged += (sender, e) =>
            {
                ProfileManager.CurrentProfile.UOClassicCombatSelf_ConsiderSpells = _uiCboxConsiderSpells.IsChecked;
                UpdateVars();
            };
            Add(_uiCboxConsiderSpells);
            //CONSIDER SPELLS

            //DISARMED / HAMSTRUNG
            _uiTextGotDisarmed = new Label("DARM", true, HUE_FONTS_GREEN, 0, 1, FontStyle.BlackBorder)
            {
                X = 0,
                Y = _uiIconConsiderSpells.Bounds.Bottom
            };
            Add(_uiTextGotDisarmed);

            _uiTimerGotDisarmed = new Label($"{_timerGotDisarmed}", true, HUE_FONTS_GREEN, 0, 1, FontStyle.BlackBorder)
            {
                X = _uiTextGotDisarmed.Bounds.Right + 10,
                Y = _uiTextGotDisarmed.Y
            };
            Add(_uiTimerGotDisarmed);

            _uiTextTimerDoDisarm = new Label("OFF", true, HUE_FONTS_RED, 0, 1, FontStyle.BlackBorder)
            {
                X = _uiTimerGotDisarmed.Bounds.Right + 2,
                Y = _uiTextGotDisarmed.Y
            };
            Add(_uiTextTimerDoDisarm);
            //
            _uiTextGotHamstrung = new Label("HSTR", true, HUE_FONTS_GREEN, 0, 1, FontStyle.BlackBorder)
            {
                X = 0,
                Y = _uiTextGotDisarmed.Bounds.Bottom
            };
            Add(_uiTextGotHamstrung);

            _uiTimerGotHamstrung = new Label($"{_timerGotHamstrung}", true, HUE_FONTS_GREEN, 0, 1, FontStyle.BlackBorder)
            {
                X = _uiTextGotHamstrung.Bounds.Right + 13,
                Y = _uiTextGotHamstrung.Y
            };
            Add(_uiTimerGotHamstrung);

            _uiTextTimerDoHamstring = new Label("OFF", true, HUE_FONTS_RED, 0, 1, FontStyle.BlackBorder)
            {
                X = _uiTimerGotHamstrung.Bounds.Right + 2,
                Y = _uiTextGotHamstrung.Y
            };
            Add(_uiTextTimerDoHamstring);
            //DISARMED / HAMSTRUNG

            //TRACKING
            _uiTextTracking = new Label("TRACKING OFF", true, HUE_FONTS_RED, 0, 1, FontStyle.BlackBorder)
            {
                X = 0,
                Y = _uiTextGotHamstrung.Bounds.Bottom
            };
            Add(_uiTextTracking);
            //TRACKING

            //UPDATE VARS
            UpdateVars();

            //COPY PASTED
            LayerOrder = UILayer.Over;
            WantUpdateSize = false;

        }
        //MAIN
        public override void Update(double totalMS, double frameMS)
        {
            if (World.Player == null || World.Player.IsDestroyed)
                return;

            //FAILSAFES
            if (_tickStartAutoBandage + 20000 >= Time.Ticks)
                _tickStartAutoBandage = 0;

            //UI PART
            //UPDATE UI
            UpdateUI();
            //UPDATE COUNTERS
            UpdateCounters();
            //UI PART END

            //VARS PART
            //CALC DIFFS
            diffhits = World.Player.HitsMax - World.Player.Hits;
            diffstam = World.Player.StaminaMax - World.Player.Stamina;
            //SAVE LAST WEP
            if (_tickLastAutoRearmAfterDisarmedItemSave + UCCS_ActionCooldown <= Time.Ticks)
            {
                if (World.Player.FindItemByLayer(Layer.OneHanded) != null)
                {
                    _itemAutoRearmAfterDisarmedItemLastInLeftHand = World.Player.FindItemByLayer(Layer.OneHanded);
                    _tickAutoRearmAfterDisarmedItemLastInLeftHand = Time.Ticks;
                }
                if (World.Player.FindItemByLayer(Layer.TwoHanded) != null)
                {
                    _itemAutoRearmAfterDisarmedItemLastInRightHand = World.Player.FindItemByLayer(Layer.TwoHanded);
                    _tickAutoRearmAfterDisarmedItemLastInRightHand = Time.Ticks;
                }
                _tickLastAutoRearmAfterDisarmedItemSave = Time.Ticks;
            }
            //VARS PART END

            //MAIN PART AUTOMATION
            MainAutomation();

            //TRIGGER BASED AUTOMATION
            TriggerBasedAutomation();

            base.Update(totalMS, frameMS);
        }
        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            return base.Draw(batcher, x, y);
        }
        protected override void OnDragEnd(int x, int y)
        {
            base.OnDragEnd(x, y);
            ProfileManager.CurrentProfile.UOClassicCombatSelfLocation = Location;
        }
        public void UpdateVars()
        {
            //UPDATE VARS
            UCCS_AutoBandage = ProfileManager.CurrentProfile.UOClassicCombatSelf_AutoBandage;

            UCCS_AutoPouche = ProfileManager.CurrentProfile.UOClassicCombatSelf_AutoPouche;
            UCCS_PoucheCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_PoucheCooldown;

            UCCS_AutoCurepot = ProfileManager.CurrentProfile.UOClassicCombatSelf_AutoCurepot;
            UCCS_CurepotCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_CurepotCooldown;

            UCCS_AutoHealpot = ProfileManager.CurrentProfile.UOClassicCombatSelf_AutoHealpot;
            UCCS_HealpotCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_HealpotCooldown;

            UCCS_AutoRefreshpot = ProfileManager.CurrentProfile.UOClassicCombatSelf_AutoRefreshpot;
            UCCS_RefreshpotCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_RefreshpotCooldown;

            UCCS_RearmAfterPot = ProfileManager.CurrentProfile.UOClassicCombatSelf_RearmAfterPot;

            UCCS_IsDuelingOrTankMage = ProfileManager.CurrentProfile.UOClassicCombatSelf_IsDuelingOrTankMage;

            UCCS_ActionCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_ActionCooldown;
            UCCS_UOClassicCombatSelf_WaitForTarget = ProfileManager.CurrentProfile.UOClassicCombatSelf_WaitForTarget;

            UCCS_BandiesHPTreshold = ProfileManager.CurrentProfile.UOClassicCombatSelf_BandiesHPTreshold;
            UCCS_BandiesPoison = ProfileManager.CurrentProfile.UOClassicCombatSelf_BandiesPoison;
            UCCS_CurepotHPTreshold = ProfileManager.CurrentProfile.UOClassicCombatSelf_CurepotHPTreshold;
            UCCS_HealpotHPTreshold = ProfileManager.CurrentProfile.UOClassicCombatSelf_HealpotHPTreshold;
            UCCS_RefreshpotStamTreshold = ProfileManager.CurrentProfile.UOClassicCombatSelf_RefreshpotStamTreshold;

            UCCS_AutoRearmAfterDisarmed = ProfileManager.CurrentProfile.UOClassicCombatSelf_AutoRearmAfterDisarmed;
            UCCS_AutoRearmAfterDisarmedCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_AutoRearmAfterDisarmedCooldown;

            UCCS_NoRefreshPotAfterHamstrung = ProfileManager.CurrentProfile.UOClassicCombatSelf_NoRefreshPotAfterHamstrung;
            UCCS_NoRefreshPotAfterHamstrungCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_NoRefreshPotAfterHamstrungCooldown;

            UCCS_DisarmStrikeCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_DisarmStrikeCooldown;
            UCCS_DisarmAttemptCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_DisarmAttemptCooldown;

            UCCS_HamstringStrikeCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_HamstringStrikeCooldown;
            UCCS_HamstringAttemptCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_HamstringAttemptCooldown;

            UCCS_DisarmedCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_DisarmedCooldown;
            UCCS_HamstrungCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_HamstrungCooldown;

            UCCS_ConsiderHidden = ProfileManager.CurrentProfile.UOClassicCombatSelf_ConsiderHidden;
            UCCS_ConsiderSpells = ProfileManager.CurrentProfile.UOClassicCombatSelf_ConsiderSpells;

            UCCS_MinRNG = ProfileManager.CurrentProfile.UOClassicCombatSelf_MinRNG;
            UCCS_MaxRNG = ProfileManager.CurrentProfile.UOClassicCombatSelf_MaxRNG;
            UCCS_ClilocTriggers = ProfileManager.CurrentProfile.UOClassicCombatSelf_ClilocTriggers;
            UCCS_MacroTriggers = ProfileManager.CurrentProfile.UOClassicCombatSelf_MacroTriggers;

            //UPDATE UI TOGGLES
            _uiCboxAutoBandage.IsChecked = UCCS_AutoBandage;
            _uiCboxAutoPouche.IsChecked = UCCS_AutoPouche;
            _uiCboxAutoCurepot.IsChecked = UCCS_AutoCurepot;
            _uiCboxAutoHealpot.IsChecked = UCCS_AutoHealpot;
            _uiCboxAutoRefreshpot.IsChecked = UCCS_AutoRefreshpot;

            _uiCboxIsDuelingOrTankMage.IsChecked = UCCS_IsDuelingOrTankMage;
            _uiCboxRearmAfterPot.IsChecked = UCCS_RearmAfterPot;
            _uiCboxAutoRearmAfterDisarmed.IsChecked = UCCS_AutoRearmAfterDisarmed;
            _uiCboxConsiderHidden.IsChecked = UCCS_ConsiderHidden;
            _uiCboxConsiderSpells.IsChecked = UCCS_ConsiderSpells;

            //ON OFF STATUS
            if (UCCS_AutoBandage)
            {
                _uiTextAutoBandage.Text = "ON";
                _uiTextAutoBandage.Hue = HUE_FONTS_GREEN;
            }
            else
            {
                _uiTextAutoBandage.Text = "OFF";
                _uiTextAutoBandage.Hue = HUE_FONTS_RED;
            }
            if (UCCS_AutoPouche)
            {
                _uiTextAutoPouche.Text = "ON";
                _uiTextAutoPouche.Hue = HUE_FONTS_GREEN;
            }
            else
            {
                _uiTextAutoPouche.Text = "OFF";
                _uiTextAutoPouche.Hue = HUE_FONTS_RED;
            }
            if (UCCS_AutoCurepot)
            {
                _uiTextAutoCurepot.Text = "ON";
                _uiTextAutoCurepot.Hue = HUE_FONTS_GREEN;
            }
            else
            {
                _uiTextAutoCurepot.Text = "OFF";
                _uiTextAutoCurepot.Hue = HUE_FONTS_RED;
            }
            if (UCCS_AutoHealpot)
            {
                _uiTextAutoHealpot.Text = "ON";
                _uiTextAutoHealpot.Hue = HUE_FONTS_GREEN;
            }
            else
            {
                _uiTextAutoHealpot.Text = "OFF";
                _uiTextAutoHealpot.Hue = HUE_FONTS_RED;
            }
            if (UCCS_AutoRefreshpot)
            {
                _uiTextAutoRefreshpot.Text = "ON";
                _uiTextAutoRefreshpot.Hue = HUE_FONTS_GREEN;
            }
            else
            {
                _uiTextAutoRefreshpot.Text = "OFF";
                _uiTextAutoRefreshpot.Hue = HUE_FONTS_RED;
            }
            //ON OFF STATUS
        }
        public override void Dispose()
        {
            base.Dispose();
        }
        //MAIN LOOP METHODS
        private void UpdateUI()
        {
            //HUE TEXT
            //AUTOMATION
            if (_timerAutoBandage == 0)
                _uiTimerAutoBandage.Hue = HUE_FONTS_GREEN;
            else
                _uiTimerAutoBandage.Hue = HUE_FONTS_YELLOW;
            if (_timerAutoPouche == 0)
                _uiTimerAutoPouche.Hue = HUE_FONTS_GREEN;
            else
                _uiTimerAutoPouche.Hue = HUE_FONTS_YELLOW;
            if (_timerAutoCurepot == 0)
                _uiTimerAutoCurepot.Hue = HUE_FONTS_GREEN;
            else
                _uiTimerAutoCurepot.Hue = HUE_FONTS_YELLOW;
            if (_timerAutoHealpot == 0)
                _uiTimerAutoHealpot.Hue = HUE_FONTS_GREEN;
            else
                _uiTimerAutoHealpot.Hue = HUE_FONTS_YELLOW;
            if (_timerAutoRefreshpot == 0)
                _uiTimerAutoRefreshpot.Hue = HUE_FONTS_GREEN;
            else
                _uiTimerAutoRefreshpot.Hue = HUE_FONTS_YELLOW;
            if (_timerStrengthpot == 0)
                _uiTimerStrengthpot.Hue = HUE_FONTS_GREEN;
            else
                _uiTimerStrengthpot.Hue = HUE_FONTS_YELLOW;
            if (_timerDexpot == 0)
                _uiTimerDexpot.Hue = HUE_FONTS_GREEN;
            else
                _uiTimerDexpot.Hue = HUE_FONTS_YELLOW;

            //DISARM / HAMSTRING (DO)
            if (_timerDoDisarm == 0)
                _uiTextTimerDoDisarm.Hue = HUE_FONTS_GREEN;
            else
                _uiTextTimerDoDisarm.Hue = HUE_FONTS_RED;
            if (_timerDoHamstring == 0)
                _uiTextTimerDoHamstring.Hue = HUE_FONTS_GREEN;
            else
                _uiTextTimerDoHamstring.Hue = HUE_FONTS_RED;

            //DISARM / HAMSTRING (GOT)
            if (_timerGotDisarmed == 0)
            {
                _uiTimerGotDisarmed.Hue = HUE_FONTS_GREEN;
                _uiTextGotDisarmed.Hue = HUE_FONTS_GREEN;
            }
            else
            {
                _uiTimerGotDisarmed.Hue = HUE_FONTS_RED;
                _uiTextGotDisarmed.Hue = HUE_FONTS_RED;
            }

            if (_timerGotHamstrung == 0)
            {
                _uiTimerGotHamstrung.Hue = HUE_FONTS_GREEN;
                _uiTextGotHamstrung.Hue = HUE_FONTS_GREEN;
            }
            else
            {
                _uiTimerGotHamstrung.Hue = HUE_FONTS_RED;
                _uiTextGotHamstrung.Hue = HUE_FONTS_RED;
            }

            //AUTOREAMAFTERIDSARM
            if (_timerAutoRearmAfterDisarmed == 0)
                _uiTimerAutoRearmAfterDisarmed.Hue = HUE_FONTS_GREEN;
            else
                _uiTimerAutoRearmAfterDisarmed.Hue = HUE_FONTS_RED;
            //HUE TEXT
        }
        private void UpdateCounters()
        {
            //AUTOBANDIES
            switch (Settings.GlobalSettings.ShardType)
            {
                case 2: // outlands
                    _timerAutoBandage = (ushort) World.Player.EnergyResistance;

                    break;

                default:

                    if (_useBandiesTime)
                    {
                        _timerAutoBandage = (Time.Ticks - _tickStartAutoBandage) / 1000;

                        if (_timerAutoBandage > 20) //FAILSAFE
                            ClilocTriggerStopBandies();

                        //NOTE: WE DONT KNOW HOW LONG BANDIES TAKE SO WE RELY ON CLILOC TO SHUT OFF
                    }

                    break;
            }
            _uiTimerAutoBandage.Text = $"{_timerAutoBandage}";

            //AUTOPOUCHE
            if (_tickStartAutoPouche != 0)
            {
                _timerAutoPouche = (UCCS_PoucheCooldown / 1000) - (Time.Ticks - _tickStartAutoPouche) / 1000;
            }
            if ((_tickStartAutoPouche + UCCS_PoucheCooldown) <= Time.Ticks)
            {
                _tickStartAutoPouche = 0;
                _timerAutoPouche = 0;
            }
            _uiTimerAutoPouche.Text = $"{_timerAutoPouche}";

            //AUTOCUREPOT
            if (_tickStartAutoCurepot != 0)
            {
                _timerAutoCurepot = (UCCS_CurepotCooldown / 1000) - (Time.Ticks - _tickStartAutoCurepot) / 1000;
            }
            if (_tickStartAutoCurepot != 0 && (_tickStartAutoCurepot + UCCS_CurepotCooldown) <= Time.Ticks)
            {
                _tickStartAutoCurepot = 0;
                _timerAutoCurepot = 0;
            }
            _uiTimerAutoCurepot.Text = $"{_timerAutoCurepot}";

            //AUTOHEALPOT
            if (_tickStartAutoHealpot != 0)
            {
                _timerAutoHealpot = (UCCS_HealpotCooldown / 1000) - (Time.Ticks - _tickStartAutoHealpot) / 1000;
            }
            if (_tickStartAutoHealpot != 0 && (_tickStartAutoHealpot + UCCS_HealpotCooldown) <= Time.Ticks)
            {
                _tickStartAutoHealpot = 0;
                _timerAutoHealpot = 0;
            }
            _uiTimerAutoHealpot.Text = $"{_timerAutoHealpot}";

            //AUTOREFRESHPOT
            if (_tickStartAutoRefreshpot != 0)
            {
                if (_tickStartAutoRefreshpot < Time.Ticks) //Can be in "future" because of UCCS_NoRefreshPotAfterHamstrung
                    _timerAutoRefreshpot = (UCCS_RefreshpotCooldown / 1000) - (Time.Ticks - _tickStartAutoRefreshpot) / 1000;
            }
            if (_tickStartAutoRefreshpot != 0 && (_tickStartAutoRefreshpot + UCCS_RefreshpotCooldown) <= Time.Ticks)
            {
                _tickStartAutoRefreshpot = 0;
                _timerAutoRefreshpot = 0;
            }
            _uiTimerAutoRefreshpot.Text = $"{_timerAutoRefreshpot}";

            //MACROSTRENGTH
            if (_tickStartStrengthpot != 0)
            {
                _timerStrengthpot = (UCCS_StrengthpotCooldown / 1000) - (Time.Ticks - _tickStartStrengthpot) / 1000;
            }
            if (_tickStartStrengthpot != 0 && (_tickStartStrengthpot + UCCS_StrengthpotCooldown) <= Time.Ticks)
            {
                _tickStartStrengthpot = 0;
                _timerStrengthpot = 0;
            }
            _uiTimerStrengthpot.Text = $"{_timerStrengthpot}";

            //MACRODEX
            if (_tickStartDexpot != 0)
            {
                _timerDexpot = (UCCS_DexpotCooldown / 1000) - (Time.Ticks - _tickStartDexpot) / 1000;
            }
            if (_tickStartDexpot != 0 && (_tickStartDexpot + UCCS_DexpotCooldown) <= Time.Ticks)
            {
                _tickStartDexpot = 0;
                _timerDexpot = 0;
            }
            _uiTimerDexpot.Text = $"{_timerDexpot}";

            //DO DISARM
            if (_tickDoDisarmFailed != 0)
            {
                _timerDoDisarm = (UCCS_DisarmAttemptCooldown / 1000) - (Time.Ticks - _tickDoDisarmFailed) / 1000;
            }
            if (_tickDoDisarmStriked != 0)
            {
                _timerDoDisarm = (UCCS_DisarmStrikeCooldown / 1000) - (Time.Ticks - _tickDoDisarmStriked) / 1000;
            }
            //------------------------------------
            if (_tickDoDisarmFailed != 0 && (_tickDoDisarmFailed + UCCS_DisarmAttemptCooldown) <= Time.Ticks || _tickDoDisarmStriked != 0 && (_tickDoDisarmStriked + UCCS_DisarmStrikeCooldown) <= Time.Ticks)
            {
                _timerDoDisarm = 0;
                _tickDoDisarmFailed = 0;
                _tickDoDisarmStriked = 0;
            }
            //------------------------------------
            if (_timerDoDisarm == 0)
            {
                _uiTextTimerDoDisarm.Text = "RDY";
            }
            else
            {
                _uiTextTimerDoDisarm.Text = $"{_timerDoDisarm}";
            }

            //DO HAMSTRING
            if (_tickDoHamstringFailed != 0)
            {
                _timerDoHamstring = (UCCS_HamstringAttemptCooldown / 1000) - (Time.Ticks - _tickDoHamstringFailed) / 1000;
            }
            if (_tickDoHamstringStriked != 0)
            {
                _timerDoHamstring = (UCCS_HamstringStrikeCooldown / 1000) - (Time.Ticks - _tickDoHamstringStriked) / 1000;
            }
            //------------------------------------
            if (_tickDoHamstringFailed != 0 && (_tickDoHamstringFailed + UCCS_HamstringAttemptCooldown) <= Time.Ticks || _tickDoHamstringStriked != 0 && (_tickDoHamstringStriked + UCCS_HamstringStrikeCooldown) <= Time.Ticks)
            {
                _timerDoHamstring = 0;
                _tickDoHamstringFailed = 0;
                _tickDoHamstringStriked = 0;
            }
            //------------------------------------
            if (_timerDoHamstring == 0)
            {
                _uiTextTimerDoHamstring.Text = "RDY";
            }
            else
            {
                _uiTextTimerDoHamstring.Text = $"{_timerDoHamstring}";
            }

            //GOT DISARMED
            if (_tickGotDisarmed != 0)
            {
                _timerGotDisarmed = (UCCS_DisarmedCooldown / 1000) - (Time.Ticks - _tickGotDisarmed) / 1000;
            }
            if (_tickGotDisarmed != 0 && (_tickGotDisarmed + UCCS_DisarmedCooldown) <= Time.Ticks)
            {
                _timerGotDisarmed = 0;
                _tickGotDisarmed = 0;
            }
            _uiTimerGotDisarmed.Text = $"{_timerGotDisarmed}";

            //GOT HAMSTRUNG
            if (_tickGotHamstrung != 0)
            {
                _timerGotHamstrung = (UCCS_HamstrungCooldown / 1000) - (Time.Ticks - _tickGotHamstrung) / 1000;
            }

            if (_tickGotHamstrung != 0 && (_tickGotHamstrung + UCCS_HamstrungCooldown) <= Time.Ticks)
            {
                _timerGotHamstrung = 0;
                _tickGotHamstrung = 0;
            }
            _uiTimerGotHamstrung.Text = $"{_timerGotHamstrung}";

            //SPECIAL CASES
            //GotDisarmedAutoRearmAfterDisarmed
            if (_tickGotDisarmedAutoRearmAfterDisarmed != 0)
            {
                _timerAutoRearmAfterDisarmed = (UCCS_AutoRearmAfterDisarmedCooldown / 1000) - (Time.Ticks - _tickGotDisarmedAutoRearmAfterDisarmed) / 1000;
            }

            if ((_tickGotDisarmedAutoRearmAfterDisarmed + UCCS_AutoRearmAfterDisarmedCooldown) <= Time.Ticks)
            {
                _timerAutoRearmAfterDisarmed = 0;
                //_tickGotDisarmedAutoRearmAfterDisarmed = 0; DONT SET ZERO HERE ELSE NO TRIGGER TO REARM
            }
            _uiTimerAutoRearmAfterDisarmed.Text = $"{_timerAutoRearmAfterDisarmed}";
        }
        private void TriggerBasedAutomation()
        {
            //NO REFRESH POT AFTER HAMSTRUNG
            if (UCCS_NoRefreshPotAfterHamstrung)
            {
                if ((_tickGotHamstrung + UCCS_NoRefreshPotAfterHamstrungCooldown) >= Time.Ticks)
                {
                    _tickStartAutoRefreshpot = UCCS_NoRefreshPotAfterHamstrungCooldown + _tickGotHamstrung;
                }
            }

            //AUTO REARM AFTER DISARMED
            if (UCCS_AutoRearmAfterDisarmed)
            {
                //NOTIFY
                if (_tickLastAutoRearmAfterDisarmedMessage != 0 && _tickLastAutoRearmAfterDisarmedMessage + 1000 <= Time.Ticks)
                {
                    if (_timerAutoRearmAfterDisarmed == 4)
                        GameActions.Print("UCC Self: Auto rearming in 4");
                    if (_timerAutoRearmAfterDisarmed == 3)
                        GameActions.Print("UCC Self: Auto rearming in 3");
                    if (_timerAutoRearmAfterDisarmed == 2)
                        GameActions.Print("UCC Self: Auto rearming in 2");
                    if (_timerAutoRearmAfterDisarmed == 1)
                        GameActions.Print("UCC Self: Auto rearming in 1");

                    _tickLastAutoRearmAfterDisarmedMessage = Time.Ticks;

                    if (_timerAutoRearmAfterDisarmed == 0)
                        _tickLastAutoRearmAfterDisarmedMessage = 0;
                }

                //ISSUE THE REARM
                if (_doAutoRearmAfterDisarmed && _tickGotDisarmedAutoRearmAfterDisarmed != 0 && (_tickLastActionTime + UCCS_ActionCooldown) <= Time.Ticks)
                {
                    if ((_tickGotDisarmedAutoRearmAfterDisarmed + UCCS_AutoRearmAfterDisarmedCooldown) <= Time.Ticks)
                    {
                        if (_tickAutoRearmAfterDisarmedItemLastInRightHand > _tickAutoRearmAfterDisarmedItemLastInLeftHand)
                        {
                            RearmNEW(_itemAutoRearmAfterDisarmedItemLastInRightHand);
                        }
                        else
                        {
                            RearmNEW(_itemAutoRearmAfterDisarmedItemLastInLeftHand);
                        }
                    }

                }
            }

            //REARM AFTER DRINKING POT
            if (_doRearmAfterPot && (_tickLastActionTime + (UCCS_ActionCooldown * 2)) <= Time.Ticks)
            {
                if (_tempItemEquipAfterPot != null)
                {
                    RearmNEW(_tempItemEquipAfterPot);
                }
            }
        }
        private void MainAutomation()
        {
            //LOGON DELAY 5SEC TO LET CLIENT OPEN BACKPACK
            if (logondelay + 5000 >= Time.Ticks)
                return;

            //DO NOTHING EXCEPTIONS
            if (World.Player.IsDead)
                return;

            if (UCCS_ConsiderHidden && World.Player.IsHidden)
                return;

            if (UCCS_ConsiderSpells && GameCursor._spellTime >= 1)
                return;

            //RNG (NEW RNG EVERY 3 SEC)
            if (_tickLastRNGCalced + 3000 >= Time.Ticks)
            {
                _varRNGtoWait = RandomNumber(UCCS_MinRNG, UCCS_MaxRNG);
                _tickLastRNGCalced = Time.Ticks;
            }
            //RNG WAIT FOR RNG
            if (_waitRNG)
            {
                if (_tickWaitRNG + _varRNGtoWait >= Time.Ticks)
                {
                    return;
                }
                else
                {
                    _doneWaitRNG = true;
                    _waitRNG = false;
                }
            }
            #region AUTOMATIONS
            //AUTOBANDIES
            if (UCCS_AutoBandage)
            {
                switch (Settings.GlobalSettings.ShardType)
                {
                    case 2: // outlands
                        if (diffhits >= UCCS_BandiesHPTreshold || UCCS_BandiesPoison && World.Player.IsPoisoned)
                        {
                            if (World.Player.EnergyResistance == 0 && _tickStartAutoBandage == 0) //_tickStartAutoBandage == 0
                            {
                                var bandage = World.Player.FindBandage();
                                if (bandage != null)
                                {
                                    if ((_tickLastActionTime + (UCCS_ActionCooldown * 2)) <= Time.Ticks)
                                    {
                                        NetClient.Socket.Send(new PTargetSelectedObject(bandage.Serial, World.Player.Serial));
                                        _tickLastActionTime = Time.Ticks;
                                    }
                                }
                                else //if you log in without full hp, it turns itself off because backpack is not opened yet
                                {
                                    UCCS_AutoBandage = false;
                                    ProfileManager.CurrentProfile.UOClassicCombatSelf_AutoBandage = false;

                                    UpdateVars();
                                }
                            }
                        }

                        break;

                    default:
                        if (diffhits >= UCCS_BandiesHPTreshold || UCCS_BandiesPoison && World.Player.IsPoisoned)
                        {
                            if (_timerAutoBandage == 0 && _tickStartAutoBandage == 0)
                            {
                                if (Client.Version < ClientVersion.CV_5020 || ProfileManager.CurrentProfile.BandageSelfOld)
                                {
                                    if (_useWaitForTarget)
                                    {
                                        if (_tickWaitForTarget == 0)
                                            _tickWaitForTarget = Time.Ticks + UCCS_UOClassicCombatSelf_WaitForTarget;

                                        if (TargetManager.IsTargeting && _tickWaitForTarget < Time.Ticks)
                                        {
                                            TargetManager.Target(World.Player);
                                            _tickLastActionTime = Time.Ticks;
                                            _useWaitForTarget = false;
                                            _tickWaitForTarget = 0;
                                            GameActions.Print("UCC Self: Bandies applyed to self.");
                                        }
                                    }
                                    else
                                    {
                                        var bandage = World.Player.FindBandage();

                                        if (bandage != null)
                                        {
                                            if ((_tickLastActionTime + UCCS_ActionCooldown) <= Time.Ticks)
                                            {
                                                _useWaitForTarget = true;
                                                GameActions.DoubleClick(bandage);
                                                _tickLastActionTime = Time.Ticks;
                                                GameActions.Print("UCC Self: Bandies applyed to self.");
                                            }
                                        }
                                        else
                                        {
                                            UCCS_AutoBandage = false;
                                            ProfileManager.CurrentProfile.UOClassicCombatSelf_AutoBandage = false;

                                            UpdateVars();
                                        }
                                    }
                                }
                                else
                                {
                                    var bandage = World.Player.FindBandage();
                                    if (bandage != null)
                                    {
                                        if ((_tickLastActionTime + (UCCS_ActionCooldown * 2)) <= Time.Ticks)
                                        {
                                            NetClient.Socket.Send(new PTargetSelectedObject(bandage.Serial, World.Player.Serial));
                                            _tickLastActionTime = Time.Ticks;
                                            GameActions.Print("UCC Self: Bandies applyed to self.");
                                        }
                                    }
                                    else
                                    {
                                        UCCS_AutoBandage = false;
                                        ProfileManager.CurrentProfile.UOClassicCombatSelf_AutoBandage = false;

                                        UpdateVars();
                                    }
                                }
                            }
                        }

                        break;
                }
            }
            //AUTOPOUCHE
            if (UCCS_AutoPouche)
            {
                if (World.Player.IsParalyzed)
                {
                    Item backpack = World.Player.FindItemByLayer(Layer.Backpack);
                    var redpouche = backpack.FindItem(0x0E79, 0x0026);

                    if (redpouche != null)
                    {
                        switch (Settings.GlobalSettings.ShardType)
                        {
                            case 2: // outlands

                                if ((_tickLastActionTime + UCCS_ActionCooldown) <= Time.Ticks && (_tickStartAutoPouche + UCCS_PoucheCooldown) <= Time.Ticks)
                                {
                                    //RNG
                                    if (_doneWaitRNG == false)
                                    {
                                        _waitRNG = true;
                                        _tickWaitRNG = Time.Ticks;
                                        return;
                                    }

                                    GameActions.Say("[pouch", ProfileManager.CurrentProfile.SpeechHue, MessageType.Regular);
                                    GameActions.Print("UCC Self: Pouche used.");
                                    _tickLastActionTime = Time.Ticks;
                                    _tickStartAutoPouche = Time.Ticks;

                                    //RNG
                                    _doneWaitRNG = false;
                                }

                                break;

                            default:

                                if ((_tickLastActionTime + UCCS_ActionCooldown) <= Time.Ticks && (_tickStartAutoPouche + UCCS_PoucheCooldown) <= Time.Ticks)
                                {
                                    //RNG
                                    if (_doneWaitRNG == false)
                                    {
                                        _waitRNG = true;
                                        _tickWaitRNG = Time.Ticks;
                                        return;
                                    }

                                    GameActions.DoubleClick(redpouche);
                                    GameActions.Print("UCC Self: Pouche used.");
                                    _tickLastActionTime = Time.Ticks;
                                    _tickStartAutoPouche = Time.Ticks;

                                    //RNG
                                    _doneWaitRNG = false;
                                }

                                break;
                        }
                    }
                    else
                    {
                        UCCS_AutoPouche = false;
                        ProfileManager.CurrentProfile.UOClassicCombatSelf_AutoPouche = false;

                        UpdateVars();
                    }
                }
            }
            //AUTOCUREPOT
            if (_lastMacroPot == 0x0F07 || UCCS_AutoCurepot)
            {
                if (_lastMacroPot == 0x0F07 || diffhits >= UCCS_CurepotHPTreshold && World.Player.IsPoisoned)
                {
                    //DISARM CHECK
                    if (DisarmNeeded())
                    {
                        if ((_tickLastActionTime + UCCS_ActionCooldown) <= Time.Ticks && (_tickStartAutoCurepot + UCCS_CurepotCooldown) <= Time.Ticks)
                        {
                            //RNG
                            if (_doneWaitRNG == false)
                            {
                                _waitRNG = true;
                                _tickWaitRNG = Time.Ticks;
                                return;
                            }

                            DisarmNEW();

                            //RNG
                            _doneWaitRNG = false;
                        }
                    }

                    var curepotion = World.Player.FindItemByGraphic(0x0F07);

                    if (curepotion != null)
                    {
                        if ((_tickLastActionTime + UCCS_ActionCooldown) <= Time.Ticks && (_tickStartAutoCurepot + UCCS_CurepotCooldown) <= Time.Ticks)
                        {
                            //RNG
                            if (_doneWaitRNG == false)
                            {
                                _waitRNG = true;
                                _tickWaitRNG = Time.Ticks;
                                return;
                            }

                            GameActions.DoubleClick(curepotion);
                            GameActions.Print("UCC Self: Curing Poison.");
                            _tickLastActionTime = Time.Ticks;
                            _tickStartAutoCurepot = Time.Ticks;
                            _lastMacroPot = 0;

                            //RNG
                            _doneWaitRNG = false;
                        }
                    }
                    else
                    {
                        UCCS_AutoCurepot = false;
                        ProfileManager.CurrentProfile.UOClassicCombatSelf_AutoCurepot = false;

                        UpdateVars();

                        _lastMacroPot = 0;
                    }
                }
            }
            //AUTOHEALPOT
            if (_lastMacroPot == 0x0F0C || UCCS_AutoHealpot)
            {
                if (_lastMacroPot == 0x0F0C || diffhits >= UCCS_HealpotHPTreshold)
                {
                    //DISARM CHECK
                    if (DisarmNeeded())
                    {
                        if ((_tickLastActionTime + UCCS_ActionCooldown) <= Time.Ticks && (_tickStartAutoHealpot + UCCS_HealpotCooldown) <= Time.Ticks)
                        {
                            //RNG
                            if (_doneWaitRNG == false)
                            {
                                _waitRNG = true;
                                _tickWaitRNG = Time.Ticks;
                                return;
                            }

                            DisarmNEW();

                            //RNG
                            _doneWaitRNG = false;
                        }
                    }

                    var healpotion = World.Player.FindItemByGraphic(0x0F0C);
                    if (healpotion != null)
                    {
                        if ((_tickLastActionTime + UCCS_ActionCooldown) <= Time.Ticks && (_tickStartAutoHealpot + UCCS_HealpotCooldown) <= Time.Ticks)
                        {
                            //RNG
                            if (_doneWaitRNG == false)
                            {
                                _waitRNG = true;
                                _tickWaitRNG = Time.Ticks;
                                return;
                            }

                            GameActions.DoubleClick(healpotion);
                            GameActions.Print("UCC Self: Healing Damage.");
                            _tickLastActionTime = Time.Ticks;
                            _tickStartAutoHealpot = Time.Ticks;
                            _lastMacroPot = 0;

                            //RNG
                            _doneWaitRNG = false;
                        }
                    }
                    else
                    {
                        UCCS_AutoHealpot = false;
                        ProfileManager.CurrentProfile.UOClassicCombatSelf_AutoHealpot = false;

                        UpdateVars();

                        _lastMacroPot = 0;
                    }
                }
            }
            //AUTOREFRESHPOT
            if (_lastMacroPot == 0xF0B || UCCS_AutoRefreshpot)
            {
                if (_lastMacroPot == 0xF0B || diffstam >= UCCS_RefreshpotStamTreshold)
                {
                    //DISARM CHECK
                    if (DisarmNeeded())
                    {
                        if ((_tickLastActionTime + UCCS_ActionCooldown) <= Time.Ticks && (_tickStartAutoRefreshpot + UCCS_RefreshpotCooldown) <= Time.Ticks)
                        {
                            //RNG
                            if (_doneWaitRNG == false)
                            {
                                _waitRNG = true;
                                _tickWaitRNG = Time.Ticks;
                                return;
                            }

                            DisarmNEW();

                            //RNG
                            _doneWaitRNG = false;
                        }
                    }

                    var refreshpotion = World.Player.FindItemByGraphic(0xF0B);
                    if (refreshpotion != null)
                    {
                        if ((_tickLastActionTime + UCCS_ActionCooldown) <= Time.Ticks && (_tickStartAutoRefreshpot + UCCS_RefreshpotCooldown) <= Time.Ticks)
                        {
                            //RNG
                            if (_doneWaitRNG == false)
                            {
                                _waitRNG = true;
                                _tickWaitRNG = Time.Ticks;
                                return;
                            }

                            GameActions.DoubleClick(refreshpotion);
                            GameActions.Print("UCC Self: Refresh Potion.");
                            _tickLastActionTime = Time.Ticks;
                            _tickStartAutoRefreshpot = Time.Ticks;
                            _lastMacroPot = 0;

                            //RNG
                            _doneWaitRNG = false;
                        }
                    }
                    else
                    {
                        UCCS_AutoRefreshpot = false;
                        ProfileManager.CurrentProfile.UOClassicCombatSelf_AutoRefreshpot = false;

                        UpdateVars();

                        _lastMacroPot = 0;
                    }
                }
            }
            //STRENGTHPOT
            if (_lastMacroPot == 0x0F09/* || UCCS_AutoCurepot*/)
            {
                if (_lastMacroPot == 0x0F09/* || diffhits >= UCCS_CurepotHPTreshold && World.Player.IsPoisoned*/)
                {
                    //DISARM CHECK
                    if (DisarmNeeded())
                    {
                        if ((_tickLastActionTime + UCCS_ActionCooldown) <= Time.Ticks && (_tickStartStrengthpot + UCCS_StrengthpotCooldown) <= Time.Ticks)
                        {
                            //RNG
                            if (_doneWaitRNG == false)
                            {
                                _waitRNG = true;
                                _tickWaitRNG = Time.Ticks;
                                return;
                            }

                            DisarmNEW();

                            //RNG
                            _doneWaitRNG = false;
                        }
                    }

                    var strengthpotion = World.Player.FindItemByGraphic(0x0F09);

                    if (strengthpotion != null)
                    {
                        if ((_tickLastActionTime + UCCS_ActionCooldown) <= Time.Ticks && (_tickStartStrengthpot + UCCS_StrengthpotCooldown) <= Time.Ticks)
                        {
                            //RNG
                            if (_doneWaitRNG == false)
                            {
                                _waitRNG = true;
                                _tickWaitRNG = Time.Ticks;
                                return;
                            }

                            GameActions.DoubleClick(strengthpotion);
                            GameActions.Print("UCC Self: Strength Potion.");
                            _tickLastActionTime = Time.Ticks;
                            _tickStartStrengthpot = Time.Ticks;
                            _lastMacroPot = 0;

                            //RNG
                            _doneWaitRNG = false;
                        }
                    }
                    else
                    {
                        _lastMacroPot = 0;

                        UpdateVars();
                    }
                }
            }
            //DEXPOT
            if (_lastMacroPot == 0x0F08/* || UCCS_AutoCurepot*/)
            {
                if (_lastMacroPot == 0x0F08/* || diffhits >= UCCS_CurepotHPTreshold && World.Player.IsPoisoned*/)
                {
                    //DISARM CHECK
                    if (DisarmNeeded())
                    {
                        if ((_tickLastActionTime + UCCS_ActionCooldown) <= Time.Ticks && (_tickStartDexpot + UCCS_DexpotCooldown) <= Time.Ticks)
                        {
                            //RNG
                            if (_doneWaitRNG == false)
                            {
                                _waitRNG = true;
                                _tickWaitRNG = Time.Ticks;
                                return;
                            }

                            DisarmNEW();

                            //RNG
                            _doneWaitRNG = false;
                        }
                    }

                    var dexpotion = World.Player.FindItemByGraphic(0x0F08);

                    if (dexpotion != null)
                    {
                        if ((_tickLastActionTime + UCCS_ActionCooldown) <= Time.Ticks && (_tickStartDexpot + UCCS_DexpotCooldown) <= Time.Ticks)
                        {
                            //RNG
                            if (_doneWaitRNG == false)
                            {
                                _waitRNG = true;
                                _tickWaitRNG = Time.Ticks;
                                return;
                            }

                            GameActions.DoubleClick(dexpotion);
                            GameActions.Print("UCC Self: Agility Potion.");
                            _tickLastActionTime = Time.Ticks;
                            _tickStartDexpot = Time.Ticks;
                            _lastMacroPot = 0;

                            //RNG
                            _doneWaitRNG = false;
                        }
                    }
                    else
                    {
                        _lastMacroPot = 0;

                        UpdateVars();
                    }
                }
            }
            #endregion
        }
        //MAIN ACTION METHODS
        public bool DisarmNeeded()
        {
            _tempItemInLeftHand = null;
            _tempItemInRightHand = null;

            _tempItemInLeftHand = World.Player.FindItemByLayer(Layer.OneHanded);
            _tempItemInRightHand = World.Player.FindItemByLayer(Layer.TwoHanded);

            //LEFT HAND = 1H
            //RIGHT HAND = 2H

            //YOU CAN DRINK IF LEFT AND RIGHT HAND IS EMPTY
            //YOU CAN DRINK IF LEFT HAND EQUIPED BUT RIGHT HAND EMPTY
            //YOU CAN DRINK IF LEFT HAND EMPTY AND RIGHT HAND IS SHIELD

            //RETURN TRUE IF DISARM NEEDED

            if (_tempItemInLeftHand == null && _tempItemInRightHand == null)
            {
                return false;
            }
            if (_tempItemInLeftHand != null && _tempItemInRightHand == null)
            {
                return false;
            }
            if (_tempItemInLeftHand == null && _tempItemInRightHand != null)
            {
                if (_tempItemInRightHand.Graphic >= 0x1B72 && _tempItemInRightHand.Graphic <= 0x1B7B || _tempItemInRightHand.Graphic >= 0x1BC3 && _tempItemInRightHand.Graphic <= 0x1BC7)
                    return false;
            }

            //DEFAULT
            return true;
        }
        public void DisarmNEW()
        {
            GameScene gs = Client.Game.GetScene<GameScene>();
            if (ItemHold.Enabled) //dont do while dragging already
                return;

            //CHECK IF ITS TIME ALRDY
            if ((_tickLastActionTime + UCCS_ActionCooldown) <= Time.Ticks)
            {
                //FEW CHECKS
                if (!DisarmNeeded())
                    return;

                Item backpack = World.Player.FindItemByLayer(Layer.Backpack);
                if (backpack == null)
                    return;

                //SET VARS
                _tempItemInLeftHand = null;
                _tempItemInRightHand = null;
                _tempItemInLeftHand = World.Player.FindItemByLayer(Layer.OneHanded);
                _tempItemInRightHand = World.Player.FindItemByLayer(Layer.TwoHanded);


                //LEFT HAND = 1H
                //RIGHT HAND = 2H

                //YOU CAN DRINK IF LEFT AND RIGHT HAND IS EMPTY
                //YOU CAN DRINK IF LEFT HAND EQUIPED BUT RIGHT HAND EMPTY
                //YOU CAN DRINK IF LEFT HAND EMPTY AND RIGHT HAND IS SHIELD

                //DUELIST RULES
                if (UCCS_IsDuelingOrTankMage)
                {
                    if (_tempItemInLeftHand != null && _tempItemInRightHand == null)
                    {
                        _tickLastActionTime = Time.Ticks;

                        if (UCCS_RearmAfterPot)
                        {
                            _doRearmAfterPot = false;
                        }
                    }
                }
                //NO DUELIST RULES
                //DISARM RIGHT HAND (2H) IF LEFT (1H) AND RIGHT (2H) IS NOT EMPTY (=WEP AND SHIELD), ELSE IF, 
                //DISARM RIGHT HAND (2H) IF LEFT (1H) IS EMPTY AND RIGHT (2H) IS NOT EMPTY (=2H WEP) 
                //UNLESS ITS A SHIELD (AS YOU CAN DRINK WITH EM)
                else if (_tempItemInLeftHand != null && _tempItemInRightHand != null)
                {
                    GameActions.PickUp(_tempItemInRightHand.Serial, 0, 0, 1);
                    GameActions.DropItem(ItemHold.Serial, 0xFFFF, 0xFFFF, 0, backpack.Serial);
                    _tickLastActionTime = Time.Ticks;
                    GameActions.Print($"UCC Self: Disarming: {_tempItemInRightHand.Graphic}");

                    //ISSUE REARM AFTER POT IF ENABLED
                    if (UCCS_RearmAfterPot)
                    {
                        _doRearmAfterPot = true;

                        if (_disarmFromTrigger)
                        {
                            _doRearmAfterPot = false;
                            _disarmFromTrigger = false;
                        }
                        _tempItemEquipAfterPot = _tempItemInRightHand;
                    }
                }
                else if (_tempItemInLeftHand == null && _tempItemInRightHand != null)
                {
                    //SHIELD ARE 2H LAYER BUT YOU CAN DRINK WITH EM, SO WE DO A CHECK HERE
                    if (_tempItemInRightHand.Graphic >= 0x1B72 && _tempItemInRightHand.Graphic <= 0x1B7B || _tempItemInRightHand.Graphic >= 0x1BC3 && _tempItemInRightHand.Graphic <= 0x1BC7)
                        return;
                    
                    GameActions.PickUp(_tempItemInRightHand.Serial, 0, 0, 1);
                    GameActions.DropItem(ItemHold.Serial, 0xFFFF, 0xFFFF, 0, backpack.Serial);
                    _tickLastActionTime = Time.Ticks;
                    GameActions.Print($"UCC Self: Disarming: {_tempItemInRightHand.Graphic}");
                    
                    //ISSUE REARM AFTER POT IF ENABLED
                    if (UCCS_RearmAfterPot)
                    {
                        _doRearmAfterPot = true;

                        if (_disarmFromTrigger)
                        {
                            _doRearmAfterPot = false;
                            _disarmFromTrigger = false;
                        }

                        _tempItemEquipAfterPot = _tempItemInRightHand;
                    }
                }
            }
        }
        public void RearmNEW(Item weapon)
        {
            if ((_tickLastActionTime + UCCS_ActionCooldown) <= Time.Ticks)
            {
                GameScene gs = Client.Game.GetScene<GameScene>();
                if (ItemHold.Enabled) //dont do while dragging
                    return;

                //GET GFX
                Item equipweapon = World.Items.Get(weapon);

                //FAILSAFE
                if (equipweapon == null)
                    return;

                //REARM
                switch (Settings.GlobalSettings.ShardType)
                {
                    case 2: // outlands

                        //PICKUP AND EQUIP INSTEAD OF DOUBLECLICK
                        if (TargetManager.IsTargeting || equipweapon.Graphic == 3713) //3713 fix for crook
                        {
                            GameActions.PickUp(weapon.Serial, 0, 0);
                            GameActions.Equip();
                        }
                        else
                        {
                            GameActions.DoubleClick(weapon.Serial);
                        }
                        GameActions.Print($"UCC Self: Arming: {equipweapon.Graphic}");
                        _tickLastActionTime = Time.Ticks;
                        _doRearmAfterPot = false;
                        _tempItemInLeftHand = null;
                        _tempItemInRightHand = null;
                        _tempItemEquipAfterPot = null;
                        _tickGotDisarmedAutoRearmAfterDisarmed = 0;
                        _doAutoRearmAfterDisarmed = false;

                        break;

                    default:

                        GameActions.PickUp(weapon.Serial, 0, 0, 1);
                        GameActions.Equip();
                        GameActions.Print($"UCC Self: Arming: {equipweapon.Graphic}");
                        _tickLastActionTime = Time.Ticks;
                        _doRearmAfterPot = false;
                        _tempItemInLeftHand = null;
                        _tempItemInRightHand = null;
                        _tempItemEquipAfterPot = null;
                        _tickGotDisarmedAutoRearmAfterDisarmed = 0;
                        _doAutoRearmAfterDisarmed = false;

                        break;
                }
            }
        }
        public void Disarm()
        {
            GameScene gs = Client.Game.GetScene<GameScene>();
            if (ItemHold.Enabled) //dont do while dragging already
                return;

            //DISARM LEFT HAND (1H) IF LEFT (1H) AND RIGHT (2H) IS NOT EMPTY (=1H WEP AND SHIELD), ELSE IF, 
            //DISARM RIGHT HAND (2H) IF LEFT (1H) IS EMPTY AND RIGHT (2H) IS NOT EMPTY (=2H WEP) UNLESS ITS A SHIELD             
            if ((_tickLastActionTime + UCCS_ActionCooldown) <= Time.Ticks)
            {

                Item backpack = World.Player.FindItemByLayer(Layer.Backpack);
                if (backpack == null)
                    return;

                _tempItemInLeftHand = World.Player.FindItemByLayer(Layer.OneHanded);
                _tempItemInRightHand = World.Player.FindItemByLayer(Layer.TwoHanded);
                if (UCCS_IsDuelingOrTankMage)
                {
                    if (_tempItemInLeftHand != null && _tempItemInRightHand == null)
                    {
                        _tickLastActionTime = Time.Ticks;

                        if (UCCS_RearmAfterPot)
                        {
                            _doRearmAfterPot = false;
                            GameActions.Print($"UCC Self: Duelist: {World.Player.Name} is using Duelist Rulesets.");
                        }
                    }
                }
                else if (_tempItemInRightHand != null)
                {
                    GameActions.PickUp(_tempItemInRightHand.Serial, 0, 0, 1);
                    GameActions.DropItem(ItemHold.Serial, 0xFFFF, 0xFFFF, 0, backpack.Serial);
                    _tickLastActionTime = Time.Ticks;
                    GameActions.Print($"UCC Self: Disarming: {_tempItemInRightHand.Graphic}");

                    //ISSUE REARM AFTER POT IF ENABLED
                    if (UCCS_RearmAfterPot)
                    {
                        _doRearmAfterPot = true;

                        if (_disarmFromTrigger)
                        {
                            _doRearmAfterPot = false;
                            _disarmFromTrigger = false;
                        }

                        _tempItemEquipAfterPot = _tempItemInRightHand;
                    }
                }
                else if (_tempItemInLeftHand == null && _tempItemInRightHand != null)
                {
                    //SHIELD ARE 2H LAYER BUT YOU CAN DRINK WITH EM, SO WE DO A CHECK HERE
                    if (_tempItemInRightHand.Graphic >= 0x1B72 && _tempItemInRightHand.Graphic <= 0x1B7B || _tempItemInRightHand.Graphic >= 0x1BC3 && _tempItemInRightHand.Graphic <= 0x1BC7)
                        return;
                    GameActions.PickUp(_tempItemInRightHand.Serial, 0, 0, 1);
                    GameActions.DropItem(ItemHold.Serial, 0xFFFF, 0xFFFF, 0, backpack.Serial);
                    _tickLastActionTime = Time.Ticks;
                    GameActions.Print($"UCC Self: Disarming: {_tempItemInRightHand.Graphic}");
                    //ISSUE REARM AFTER POT IF ENABLED
                    if (UCCS_RearmAfterPot)
                    {
                        _doRearmAfterPot = true;

                        if (_disarmFromTrigger)
                        {
                            _doRearmAfterPot = false;
                            _disarmFromTrigger = false;
                        }

                        _tempItemEquipAfterPot = _tempItemInRightHand;
                    }
                }
            }
        }
        public void Rearm(Item weapon)
        {
            if ((_tickLastActionTime + UCCS_ActionCooldown) <= Time.Ticks)
            {
                GameScene gs = Client.Game.GetScene<GameScene>();
                if (ItemHold.Enabled) //dont do while dragging
                    return;

                //GET GFX
                Item equipweapon = World.Items.Get(weapon);

                //FAILSAFE
                if (equipweapon == null)
                    return;

                //REARM
                switch (Settings.GlobalSettings.ShardType)
                {
                    case 2: // outlands
                        
                        if (TargetManager.IsTargeting || equipweapon.Graphic == 3713) //3713 fix for crook
                        {
                            GameActions.PickUp(weapon.Serial, 0, 0);
                            GameActions.Equip();
                        }
                        else
                            GameActions.DoubleClick(weapon.Serial);
                        GameActions.Print($"UCC Self: Arming: {equipweapon.Graphic}");
                        _tickLastActionTime = Time.Ticks;
                        _doRearmAfterPot = false;
                        _tempItemInLeftHand = null;
                        _tempItemInRightHand = null;
                        _tempItemEquipAfterPot = null;
                        _tickGotDisarmedAutoRearmAfterDisarmed = 0;
                        _doAutoRearmAfterDisarmed = false;

                        break;

                    default:

                        GameActions.PickUp(weapon.Serial, 0, 0, 1);
                        GameActions.Equip();
                        GameActions.Print($"UCC Self: Arming: {equipweapon.Graphic}");
                        _tickLastActionTime = Time.Ticks;
                        _doRearmAfterPot = false;
                        _tempItemInLeftHand = null;
                        _tempItemInRightHand = null;
                        _tempItemEquipAfterPot = null;
                        _tickGotDisarmedAutoRearmAfterDisarmed = 0;
                        _doAutoRearmAfterDisarmed = false;

                        break;
                }
            }
        }
        //CLILOC TRIGGERS (ON / OFF / UI)
        #region CLILOC TRIGGERS (ON / OFF / UI)
        public void ClilocTriggerTrackingON()
        {
            _uiTextTracking.Text = "TRACKING ON";
            _uiTextTracking.Hue = HUE_FONTS_GREEN;
        }
        public void ClilocTriggerTrackingOFF()
        {
            _uiTextTracking.Text = "TRACKING OFF";
            _uiTextTracking.Hue = HUE_FONTS_RED;
        }
        public void ClilocTriggerTrackingActive()
        {
            _uiTextTracking.Text = "TRACKING TARGET";
            _uiTextTracking.Hue = HUE_FONTS_YELLOW;
        }
        public void ClilocTriggerTrackingInActive()
        {
            _uiTextTracking.Text = "TRACKING OOR";
            _uiTextTracking.Hue = HUE_FONTS_BLUE;
        }
        public void ClilocTriggerDisarmON()
        {
            _uiTextTimerDoDisarm.Text = "RDY";
            _uiTextTimerDoDisarm.Hue = HUE_FONTS_GREEN;
        }
        public void ClilocTriggerDisarmOFF()
        {
            _uiTextTimerDoDisarm.Text = "OFF";
            _uiTextTimerDoDisarm.Hue = HUE_FONTS_RED;
        }
        public void ClilocTriggerHamstringON()
        {
            _uiTextTimerDoHamstring.Text = "RDY";
            _uiTextTimerDoHamstring.Hue = HUE_FONTS_GREEN;
        }
        public void ClilocTriggerHamstringOFF()
        {
            _uiTextTimerDoHamstring.Text = "OFF";
            _uiTextTimerDoHamstring.Hue = HUE_FONTS_RED;
        }
        #endregion
        //ACION CLILOC TRIGGERS (DO)
        #region //ACION CLILOC TRIGGERS (DO)
        public void ClilocTriggerStartBandies()
        {
            _useBandiesTime = true;
            _tickStartAutoBandage = Time.Ticks;
            _tickLastActionTime = Time.Ticks;
        }
        public void ClilocTriggerStopBandies()
        {
            _useBandiesTime = false;
            _tickStartAutoBandage = 0;
            _timerAutoBandage = 0;
        }
        public void ClilocTriggerDisarmStriked()
        {
            _tickDoDisarmStriked = Time.Ticks;
        }
        public void ClilocTriggerDisarmFailed()
        {
            _tickDoDisarmFailed = Time.Ticks;
        }
        public void ClilocTriggerHamstringStriked()
        {
            _tickDoHamstringStriked = Time.Ticks;
        }
        public void ClilocTriggerHamstringFailed()
        {
            _tickDoHamstringFailed = Time.Ticks;
        }
        #endregion
        //ACION CLILOC TRIGGERS (GOT)
        #region ACION CLILOC TRIGGERS (GOT)
        public void ClilocTriggerGotDisarmed()
        {
            _tickGotDisarmed = Time.Ticks;
            _tickGotDisarmedAutoRearmAfterDisarmed = Time.Ticks;

            if (UCCS_AutoRearmAfterDisarmed)
            {
                _doAutoRearmAfterDisarmed = true;
                _tickLastAutoRearmAfterDisarmedMessage = Time.Ticks;
            }
        }
        public void ClilocTriggerGotHamstrung()
        {
            _tickGotHamstrung = Time.Ticks;
        }
        #endregion
        //FAILSAFE CLILOC TRIGGERS
        #region FAILSAFE CLILOC TRIGGERS
        public void ClilocTriggerFSFreeHands()
        {
            if (UCCS_ClilocTriggers == false)
                return;

            //TRIGGER FROM "YOU MUST HAVE FREE HANDS" MESSAGE

            _tickLastActionTime = Time.Ticks - UCCS_ActionCooldown;

            _disarmFromTrigger = true; //Makes no rearm is issued
            DisarmNEW();
        }
        public void ClilocTriggerFSWaitX(int seconds, ushort potion)
        {
            if (UCCS_ClilocTriggers == false)
                return;

            //TRIGGER FROM "YOU MUST WAIT X SECONDS BEFORE USING ANOTHER X POTION" MESSAGE

            switch (potion)
            {
                case 0x0F07: //CURE
                    _tickStartAutoCurepot = Time.Ticks + 2000;
                    _timerAutoCurepot = 2;

                    break;
                case 0x0F0C: //HEAL
                    _tickStartAutoHealpot = Time.Ticks + 2000;
                    _timerAutoHealpot = 2;

                    break;
                case 0xF0B: //REFRESH
                    _tickStartAutoRefreshpot = Time.Ticks + 2000;
                    _timerAutoRefreshpot = 2;

                    break;
                case 0xF09: //STRENGTH
                    //

                    break;
                case 0xF08: //AGILITY
                    //

                    break;

                default:
                    break;
            }

            _tickLastActionTime = Time.Ticks;
        }
        public void ClilocTriggerFSHamstrungRefreshpot()
        {
            if (UCCS_ClilocTriggers == false)
                return;

            //TRIGGER FROM "YOU HAVE BEEN HAMSTRUNG AND CANNOT REGAIN STAMINA AT THE MOMENT" MESSAGE

            _tickStartAutoRefreshpot = Time.Ticks + 2000;
            _timerAutoRefreshpot = 2;

            _tickLastActionTime = Time.Ticks;
        }
        public void ClilocTriggerFSFullHP()
        {
            if (UCCS_ClilocTriggers == false)
                return;

            //TRIGGER FROM "You are already at full health." MESSAGE

            _tickStartAutoHealpot = 0;
            _timerAutoHealpot = 0;

            _tickLastActionTime = Time.Ticks;
        }
        public void ClilocTriggerFSNoPoison()
        {
            if (UCCS_ClilocTriggers == false)
                return;

            //TRIGGER FROM "You are not poisoned." MESSAGE

            _tickStartAutoCurepot = 0;
            _timerAutoCurepot = 0;

            _tickLastActionTime = Time.Ticks;
        }
        public void ClilocTriggerFSFullStamina()
        {
            if (UCCS_ClilocTriggers == false)
                return;

            //TRIGGER FROM "You decide against drinking this potion, as you are already at full stamina." MESSAGE

            _tickStartAutoRefreshpot = 0;
            _timerAutoRefreshpot = 0;

            _tickLastActionTime = Time.Ticks;
        }
        #endregion
        //MACRO CLILOC TRIGGERS
        public void ClilocTriggerPotMacro(ushort potion)
        {
            if (UCCS_ClilocTriggers == false)
                return;

            //TRIGGERED WHEN USING A POT FROM MACRO

            switch (potion)
            {
                case 0x0F07: //CURE
                    _tickLastActionTime = Time.Ticks;

                    if (!DisarmNeeded()) //WE ASSUME THE POT DRINK WORKED
                    {
                        _tickStartAutoCurepot = Time.Ticks; //UI TIMER
                    }

                    break;
                case 0x0F0C: //HEAL
                    _tickLastActionTime = Time.Ticks;

                    if (!DisarmNeeded()) //WE ASSUME THE POT DRINK WORKED
                    {
                        _tickStartAutoHealpot = Time.Ticks; //UI TIMER
                    }

                    break;
                case 0xF0B: //REFRESH
                    _tickLastActionTime = Time.Ticks;

                    if (!DisarmNeeded()) //WE ASSUME THE POT DRINK WORKED
                    {
                        _tickStartAutoRefreshpot = Time.Ticks; //UI TIMER
                    }

                    break;
                case 0xF09: //STRENGTH
                    _tickLastActionTime = Time.Ticks;

                    if (!DisarmNeeded()) //WE ASSUME THE POT DRINK WORKED
                    {
                        _tickStartStrengthpot = Time.Ticks; //UI TIMER
                    }

                    break;
                case 0xF08: //AGILITY
                    _tickLastActionTime = Time.Ticks;

                    if (!DisarmNeeded()) //WE ASSUME THE POT DRINK WORKED
                    {
                        _tickStartDexpot = Time.Ticks; //UI TIMER
                    }

                    break;

                default:
                    break;
            }
        }
        public void MacroTriggerPotMacro(ushort potion)
        {
            if (UCCS_MacroTriggers == false)
                return;

            //TRIGGERED WHEN USING A POT FROM MACRO

            switch (potion)
            {
                case 0x0F07: //CURE

                    if (World.Player.IsPoisoned)
                    {
                        _lastMacroPot = potion; //WE SET TO TRIGGER A POTION CONUSMATION
                    }

                    break;
                case 0x0F0C: //HEAL

                    if (World.Player.Hits < World.Player.HitsMax)
                    {
                        _lastMacroPot = potion; //WE SET TO TRIGGER A POTION CONUSMATION
                    }

                    break;
                case 0xF0B: //REFRESH

                    if (World.Player.Stamina < World.Player.StaminaMax)
                    {
                        _lastMacroPot = potion; //WE SET TO TRIGGER A POTION CONUSMATION
                    }

                    break;
                case 0xF09: //STRENGTH

                    _lastMacroPot = potion; //WE SET TO TRIGGER A POTION CONUSMATION

                    break;
                case 0xF08: //AGILITY
                    
                    _lastMacroPot = potion; //WE SET TO TRIGGER A POTION CONUSMATION

                    break;

                default:
                    break;
            }
        }
        //MISC
        // Generate a random number between two numbers  
        public int RandomNumber(int min, int max)
        {
            Random random = new Random();
            return random.Next(min, max);
        }
    }
}