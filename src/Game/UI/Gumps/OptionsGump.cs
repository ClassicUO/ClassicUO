#region license

// Copyright (C) 2020 ClassicUO Development Community on Github
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Data;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Gumps
{
    internal class OptionsGump : Gump
    {
        private const byte FONT = 0xFF;
        private const ushort HUE_FONT = 999;
        private const int WIDTH = 700;
        private const int HEIGHT = 500;
        private const int TEXTBOX_HEIGHT = 25;

        private static Texture2D _logoTexture2D;
        private Combobox _auraType, _filterType;
        private Combobox _autoOpenCorpseOptions;
        private StbTextBox _autoOpenCorpseRange;

        //experimental
        private Checkbox _autoOpenDoors, _autoOpenCorpse, _skipEmptyCorpse, _disableTabBtn, _disableCtrlQWBtn, _disableDefaultHotkeys, _disableArrowBtn, _disableAutoMove, _overrideContainerLocation, _smoothDoors, _showTargetRangeIndicator, _customBars, _customBarsBBG, _saveHealthbars;
        private HSliderBar _sliderZoom;
        private Checkbox _buffBarTime, _castSpellsByOneClick, _queryBeforAttackCheckbox, _queryBeforeBeneficialCheckbox, _spellColoringCheckbox, _spellFormatCheckbox;
        private HSliderBar _cellSize;
        private Checkbox _containerScaleItems, _containerDoubleClickToLoot, _relativeDragAnDropItems, _useLargeContianersGumps, _highlightContainersWhenMouseIsOver;


        // containers
        private HSliderBar _containersScale;
        private Combobox _cotType;
        private HSliderBar _delay_before_display_tooltip, _tooltip_zoom, _tooltip_background_opacity;
        private Combobox _dragSelectModifierKey;


        //counters
        private Checkbox _enableCounters, _highlightOnUse, _highlightOnAmount, _enableAbbreviatedAmount;
        private Checkbox _enableDragSelect, _dragSelectHumanoidsOnly;

        // sounds
        private Checkbox _enableSounds, _enableMusic, _footStepsSound, _combatMusic, _musicInBackground, _loginMusic;

        // fonts
        private FontSelector _fontSelectorChat;
        private Checkbox _forceUnicodeJournal;
        private StbTextBox _gameWindowHeight;

        private Checkbox _gameWindowLock, _gameWindowFullsize;
        // GameWindowPosition
        private StbTextBox _gameWindowPositionX;
        private StbTextBox _gameWindowPositionY;

        // GameWindowSize
        private StbTextBox _gameWindowWidth;
        private Combobox _gridLoot;
        private Checkbox _hideScreenshotStoredInMessage;
        private Checkbox _highlightObjects, /*_smoothMovements,*/ _enablePathfind, _useShiftPathfind, _alwaysRun, _alwaysRunUnlessHidden, _showHpMobile, _highlightByState, _drawRoofs, _treeToStumps, _hideVegetation, _noColorOutOfRangeObjects, _useCircleOfTransparency, _enableTopbar, _holdDownKeyTab, _holdDownKeyAlt, _closeAllAnchoredGumpsWithRClick, _chatAfterEnter, _chatAdditionalButtonsCheckbox, _chatShiftEnterCheckbox, _enableCaveBorder;
        private Checkbox _holdShiftForContext, _holdShiftToSplitStack, _reduceFPSWhenInactive, _sallosEasyGrab, _partyInviteGump, _objectsFading, _textFading, _holdAltToMoveGumps;
        private Combobox _hpComboBox, _healtbarType, _fieldsType, _hpComboBoxShowWhen;

        // infobar
        private List<InfoBarBuilderControl> _infoBarBuilderControls;
        private Combobox _infoBarHighlightType;
        private DataBox _databox;

        // combat & spells
        private ColorBox _innocentColorPickerBox, _friendColorPickerBox, _crimialColorPickerBox, _genericColorPickerBox, _enemyColorPickerBox, _murdererColorPickerBox, _neutralColorPickerBox, _beneficColorPickerBox, _harmfulColorPickerBox;
        private HSliderBar _lightBar;

        // macro
        private MacroControl _macroControl;
        private Checkbox _overrideAllFonts;
        private Combobox _overrideAllFontsIsUnicodeCheckbox;
        private Combobox _overrideContainerLocationSetting;
        private ColorBox _poisonColorPickerBox, _paralyzedColorPickerBox, _invulnerableColorPickerBox;
        private NiceButton _randomizeColorsButton;
        private Checkbox _restorezoomCheckbox, _zoomCheckbox;
        private StbTextBox _rows, _columns, _highlightAmount, _abbreviatedAmount;

        // speech
        private Checkbox _scaleSpeechDelay, _saveJournalCheckBox;
        private Checkbox _showHouseContent;
        private Checkbox _showInfoBar;

        // general
        private HSliderBar _sliderFPS, _circleOfTranspRadius;
        private HSliderBar _sliderSpeechDelay;
        private HSliderBar _soundsVolume, _musicVolume, _loginMusicVolume;
        private ColorBox _speechColorPickerBox, _emoteColorPickerBox, _yellColorPickerBox, _whisperColorPickerBox, _partyMessageColorPickerBox, _guildMessageColorPickerBox, _allyMessageColorPickerBox, _chatMessageColorPickerBox, _partyAuraColorPickerBox;
        private StbTextBox _spellFormatBox;
        private ColorBox _tooltip_font_hue;
        private FontSelector _tooltip_font_selector;

        // video
        private Checkbox _use_old_status_gump, _windowBorderless, _enableDeathScreen, _enableBlackWhiteEffect, _altLights, _enableLight, _enableShadows, _auraMouse, _runMouseInSeparateThread, _useColoredLights, _darkNights, _partyAura, _hideChatGradient;
        private Checkbox _use_smooth_boat_movement;

        private Checkbox _use_tooltip;
        private Checkbox _useStandardSkillsGump, _showMobileNameIncoming, _showCorpseNameIncoming;



        public OptionsGump() : base(0, 0)
        {
            Add
            (
                new AlphaBlendControl(0.05f)
                {
                    X = 1,
                    Y = 1,
                    Width = WIDTH - 2,
                    Height = HEIGHT - 2
                }
            );

            
            int i = 0;
            Add(new NiceButton(10, 10 + 30 * i++, 140, 25, ButtonAction.SwitchPage, ResGumps.General) {IsSelected = true, ButtonParameter = 1});
            Add(new NiceButton(10, 10 + 30 * i++, 140, 25, ButtonAction.SwitchPage, ResGumps.Sound) {ButtonParameter = 2});
            Add(new NiceButton(10, 10 + 30 * i++, 140, 25, ButtonAction.SwitchPage, ResGumps.Video) {ButtonParameter = 3});
            Add(new NiceButton(10, 10 + 30 * i++, 140, 25, ButtonAction.SwitchPage, ResGumps.Macros) {ButtonParameter = 4});
            Add(new NiceButton(10, 10 + 30 * i++, 140, 25, ButtonAction.SwitchPage, ResGumps.Tooltip) {ButtonParameter = 5});
            Add(new NiceButton(10, 10 + 30 * i++, 140, 25, ButtonAction.SwitchPage, ResGumps.Fonts) {ButtonParameter = 6});
            Add(new NiceButton(10, 10 + 30 * i++, 140, 25, ButtonAction.SwitchPage, ResGumps.Speech) {ButtonParameter = 7});
            Add(new NiceButton(10, 10 + 30 * i++, 140, 25, ButtonAction.SwitchPage, ResGumps.CombatSpells) {ButtonParameter = 8});
            Add(new NiceButton(10, 10 + 30 * i++, 140, 25, ButtonAction.SwitchPage, ResGumps.Counters) {ButtonParameter = 9});
            Add(new NiceButton(10, 10 + 30 * i++, 140, 25, ButtonAction.SwitchPage, ResGumps.InfoBar) {ButtonParameter = 10});
            Add(new NiceButton(10, 10 + 30 * i++, 140, 25, ButtonAction.SwitchPage, ResGumps.Containers) {ButtonParameter = 11});
            Add(new NiceButton(10, 10 + 30 * i++, 140, 25, ButtonAction.SwitchPage, ResGumps.Experimental) {ButtonParameter = 12});


            Add(new Line(160, 5, 1, HEIGHT - 10, Color.Gray.PackedValue));

            int offsetX = 60;
            int offsetY = 60;

            Add(new Line(160, 405 + 35 + 1, WIDTH - 160, 1, Color.Gray.PackedValue));

            Add
            (
                new Button((int) Buttons.Cancel, 0x00F3, 0x00F1, 0x00F2)
                {
                    X = 154 + offsetX, Y = 405 + offsetY, ButtonAction = ButtonAction.Activate
                }
            );

            Add
            (
                new Button((int) Buttons.Apply, 0x00EF, 0x00F0, 0x00EE)
                {
                    X = 248 + offsetX, Y = 405 + offsetY, ButtonAction = ButtonAction.Activate
                }
            );

            Add
            (
                new Button((int) Buttons.Default, 0x00F6, 0x00F4, 0x00F5)
                {
                    X = 346 + offsetX, Y = 405 + offsetY, ButtonAction = ButtonAction.Activate
                }
            );

            Add
            (
                new Button((int) Buttons.Ok, 0x00F9, 0x00F8, 0x00F7)
                {
                    X = 443 + offsetX, Y = 405 + offsetY, ButtonAction = ButtonAction.Activate
                }
            );

            AcceptMouseInput = true;
            CanMove = true;
            CanCloseWithRightClick = true;

            BuildGeneral();
            BuildSounds();
            BuildVideo();
            BuildCommands();
            BuildFonts();
            BuildSpeech();
            BuildCombat();
            BuildTooltip();
            BuildCounters();
            BuildInfoBar();
            BuildContainers();
            BuildExperimental();

            ChangePage(1);
        }

        private static Texture2D LogoTexture
        {
            get
            {
                if (_logoTexture2D == null || _logoTexture2D.IsDisposed)
                {
                    Stream stream = typeof(CUOEnviroment).Assembly.GetManifestResourceStream("ClassicUO.cuologo.png");
                    _logoTexture2D = Texture2D.FromStream(Client.Game.GraphicsDevice, stream);
                }

                return _logoTexture2D;
            }
        }

        private void BuildGeneral()
        {
            const int PAGE = 1;

            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            int startX = 5;
            int startY = 5;


            // FPS
            Label text = AddLabel(rightArea, ResGumps.FPS, startX, startY);
            startX += text.Bounds.Right + 5;
            _sliderFPS = AddHSlider(rightArea, Constants.MIN_FPS, Constants.MAX_FPS, Settings.GlobalSettings.FPS, startX, startY, 250);
            startY += text.Bounds.Bottom + 5;
            _reduceFPSWhenInactive = AddCheckBox(rightArea, ResGumps.FPSInactive, ProfileManager.Current.ReduceFPSWhenInactive, startX, startY);

            startX = 5;
            startY += 30;

            _highlightObjects = AddCheckBox(rightArea, ResGumps.HighlightObjects, ProfileManager.Current.HighlightGameObjects, startX, startY);
            startY += _highlightObjects.Height + 2;
            _enablePathfind = AddCheckBox(rightArea, ResGumps.EnablePathfinding, ProfileManager.Current.EnablePathfind, startX, startY);
            startX += _enablePathfind.Width + 5 + 15;
            _useShiftPathfind = AddCheckBox(rightArea, ResGumps.ShiftPathfinding, ProfileManager.Current.UseShiftToPathfind, startX, startY);
            startX = 5;
            startY += _useShiftPathfind.Height + 2;
            _alwaysRun = AddCheckBox(rightArea, ResGumps.AlwaysRun, ProfileManager.Current.AlwaysRun, startX, startY);
            startX += _alwaysRun.Width + 5 + 15;
            _alwaysRunUnlessHidden = AddCheckBox(rightArea, ResGumps.AlwaysRunHidden, ProfileManager.Current.AlwaysRunUnlessHidden, startX, startY);
            startX = 5;
            startY += _alwaysRun.Height + 2;
            _enableTopbar = AddCheckBox(rightArea, ResGumps.DisableMenu, ProfileManager.Current.TopbarGumpIsDisabled, startX, startY);
            startY += _enableTopbar.Height + 2;
            _holdDownKeyTab = AddCheckBox(rightArea, ResGumps.TabCombat, ProfileManager.Current.HoldDownKeyTab, startX, startY);
            startY += _holdDownKeyTab.Height + 2;
            _holdDownKeyAlt = AddCheckBox(rightArea, ResGumps.AltCloseGumps, ProfileManager.Current.HoldDownKeyAltToCloseAnchored, startX, startY);
            startY += _holdDownKeyAlt.Height + 2;
            _closeAllAnchoredGumpsWithRClick = AddCheckBox(rightArea, ResGumps.ClickCloseAllGumps, ProfileManager.Current.CloseAllAnchoredGumpsInGroupWithRightClick, startX, startY);
            startY += _closeAllAnchoredGumpsWithRClick.Height + 2;
            _holdAltToMoveGumps = AddCheckBox(rightArea, ResGumps.AltMoveGumps, ProfileManager.Current.HoldAltToMoveGumps, startX, startY);
            startY += _holdAltToMoveGumps.Height + 2;
            _hideScreenshotStoredInMessage = AddCheckBox(rightArea, ResGumps.HideScreenshotStoredInMessage, ProfileManager.Current.HideScreenshotStoredInMessage, startX, startY);
            startY += _hideScreenshotStoredInMessage.Height + 2;
            _holdShiftForContext = AddCheckBox(rightArea, ResGumps.ShiftContext, ProfileManager.Current.HoldShiftForContext, startX, startY);
            startY += _holdShiftForContext.Height + 2;
            _holdShiftToSplitStack = AddCheckBox(rightArea, ResGumps.ShiftStack, ProfileManager.Current.HoldShiftToSplitStack, startX, startY);
            startY += _holdShiftToSplitStack.Height + 2;
          
            
            _highlightByState = AddCheckBox(rightArea, ResGumps.HighlighState, ProfileManager.Current.HighlightMobilesByFlags, startX, startY);
            startY += _highlightByState.Height + 2;
            startX += 40;
            _poisonColorPickerBox = AddColorBox(rightArea, startX, startY, ProfileManager.Current.PoisonHue, ResGumps.PoisonedColor);
            startY += _poisonColorPickerBox.Height + 2 + 3;
            _paralyzedColorPickerBox = AddColorBox(rightArea, startX, startY, ProfileManager.Current.ParalyzedHue, ResGumps.ParalyzedColor);
            startY += _paralyzedColorPickerBox.Height + 2 + 3;
            _invulnerableColorPickerBox = AddColorBox(rightArea, startX, startY, ProfileManager.Current.InvulnerableHue, ResGumps.InvulColor);
            startY += _invulnerableColorPickerBox.Height + 2 + 3;


            startX = 5;

            _noColorOutOfRangeObjects = AddCheckBox(rightArea, ResGumps.OutOfRangeColor, ProfileManager.Current.NoColorObjectsOutOfRange, startX, startY);
            startY += _noColorOutOfRangeObjects.Height + 2;
            _objectsFading = AddCheckBox(rightArea, ResGumps.ObjAlphaFading, ProfileManager.Current.UseObjectsFading, startX, startY);
            startY += _objectsFading.Height + 2;
            _textFading = AddCheckBox(rightArea, ResGumps.TextAlphaFading, ProfileManager.Current.TextFading, startX, startY);
            startY += _textFading.Height + 2;
            _useStandardSkillsGump = AddCheckBox(rightArea, ResGumps.StandardSkillGump, ProfileManager.Current.StandardSkillsGump, startX, startY);
            startY += _useStandardSkillsGump.Height + 2;
            _showMobileNameIncoming = AddCheckBox(rightArea, ResGumps.ShowIncMobiles, ProfileManager.Current.ShowNewMobileNameIncoming, startX, startY);
            startY += _showMobileNameIncoming.Height + 2;
            _showCorpseNameIncoming = AddCheckBox(rightArea, ResGumps.ShowIncCorpses, ProfileManager.Current.ShowNewCorpseNameIncoming, startX, startY);
            startY += _showCorpseNameIncoming.Height + 2;
            _sallosEasyGrab = AddCheckBox(rightArea, ResGumps.SallosEasyGrab, ProfileManager.Current.SallosEasyGrab, startX, startY);
            startY += _sallosEasyGrab.Height + 2;
            _partyInviteGump = AddCheckBox(rightArea, ResGumps.ShowGumpPartyInv, ProfileManager.Current.PartyInviteGump, startX, startY);
            startY += _partyInviteGump.Height + 2;
            _showHouseContent = AddCheckBox(rightArea, ResGumps.ShowHousesContent, ProfileManager.Current.ShowHouseContent, startX, startY);
            _showHouseContent.IsVisible = Client.Version >= ClientVersion.CV_70796;

            if (_showHouseContent.IsVisible)
            {
                startY += _showHouseContent.Height + 2;
            }

            _customBars = AddCheckBox(rightArea, ResGumps.UseCustomHPBars, ProfileManager.Current.CustomBarsToggled, startX, startY);
            startY += _customBars.Height + 2;
            _customBarsBBG = AddCheckBox(rightArea, ResGumps.UseBlackBackgr, ProfileManager.Current.CBBlackBGToggled, startX, startY);
            startY += _customBarsBBG.Height + 2;
            _saveHealthbars = AddCheckBox(rightArea, ResGumps.SaveHPBarsOnLogout, ProfileManager.Current.SaveHealthbars, startX, startY);
            startY += _saveHealthbars.Height + 2;
            _showTargetRangeIndicator = AddCheckBox(rightArea, ResGumps.ShowTarRangeIndic, ProfileManager.Current.ShowTargetRangeIndicator, startX, startY);
            startY += _showTargetRangeIndicator.Height + 2;
            _enableDragSelect = AddCheckBox(rightArea, ResGumps.EnableDragHPBars, ProfileManager.Current.EnableDragSelect, startX, startY);
            startY += _enableDragSelect.Height + 2;

            startX = 40;
            text = AddLabel(rightArea, ResGumps.DragKey, startX, startY);
            startX += text.Width + 5;
            _dragSelectModifierKey = AddCombobox(rightArea, new[] { ResGumps.KeyMod_None, ResGumps.KeyMod_Ctrl, ResGumps.KeyMod_Shift }, ProfileManager.Current.DragSelectModifierKey, startX, startY, 100);
            startX += _dragSelectModifierKey.Width + 5;
            _dragSelectHumanoidsOnly = AddCheckBox(rightArea, ResGumps.DragHumanoidsOnly, ProfileManager.Current.DragSelectHumanoidsOnly, startX, startY);
            startY += _dragSelectHumanoidsOnly.Height + 2;
            startX = 5;

            
            _use_smooth_boat_movement = AddCheckBox(rightArea, ResGumps.SmoothBoat, ProfileManager.Current.UseSmoothBoatMovement, startX, startY);
            _use_smooth_boat_movement.IsVisible = Client.Version >= ClientVersion.CV_7090;

            if (_use_smooth_boat_movement.IsVisible)
            {
                startY += _use_smooth_boat_movement.Height + 2;
            }
            

            _autoOpenDoors = AddCheckBox(rightArea, ResGumps.AutoOpenDoors, ProfileManager.Current.AutoOpenDoors, startX, startY);
            startX += _autoOpenDoors.Width + 20;
            _smoothDoors = AddCheckBox(rightArea, ResGumps.SmoothDoors, ProfileManager.Current.SmoothDoors, startX, startY);

            startX = 5;
            startY += _smoothDoors.Height + 2;
            _autoOpenCorpse = AddCheckBox(rightArea, ResGumps.AutoOpenCorpses, ProfileManager.Current.AutoOpenCorpses, startX, startY);
            startX += 40;
            startY += _autoOpenCorpse.Height + 2;
            _autoOpenCorpseRange = AddInputField
            (
                rightArea,
                startX,
                startY,
                50, TEXTBOX_HEIGHT,
                ResGumps.CorpseOpenRange,
                80,
                false,
                true,
                2
            );
            _autoOpenCorpseRange.SetText(ProfileManager.Current.AutoOpenCorpseRange.ToString());
            startY += _autoOpenCorpseRange.Height + 2;
            _skipEmptyCorpse = AddCheckBox(rightArea, ResGumps.SkipEmptyCorpses, ProfileManager.Current.SkipEmptyCorpse, startX, startY);
            startY += _skipEmptyCorpse.Height + 2;
            text = AddLabel(rightArea, ResGumps.CorpseOpenOptions, startX, startY);
            startX += text.Bounds.Width + 5;
            _autoOpenCorpseOptions = AddCombobox(rightArea, new[] {ResGumps.CorpseOpt_None, ResGumps.CorpseOpt_NotTar, ResGumps.CorpseOpt_NotHid, ResGumps.CorpseOpt_Both}, ProfileManager.Current.CorpseOpenOptions, startX, startY, 150);

            startX = 5;
            startY += _autoOpenCorpseOptions.Height + 2;

            _drawRoofs = AddCheckBox(rightArea, ResGumps.HideRoofTiles, !ProfileManager.Current.DrawRoofs, startX, startY);
            startY += _drawRoofs.Height + 2;
            _treeToStumps = AddCheckBox(rightArea, ResGumps.TreesStumps, ProfileManager.Current.TreeToStumps, startX, startY);
            startY += _treeToStumps.Height + 2;
            _hideVegetation = AddCheckBox(rightArea, ResGumps.HideVegetation, ProfileManager.Current.HideVegetation, startX, startY);
            startY += _hideVegetation.Height + 2;
            _enableCaveBorder = AddCheckBox(rightArea, ResGumps.MarkCaveTiles, ProfileManager.Current.EnableCaveBorder, startX, startY);
            startY += _enableCaveBorder.Height + 2;
            _useCircleOfTransparency = AddCheckBox(rightArea, ResGumps.EnableCircleTrans, ProfileManager.Current.UseCircleOfTransparency, startX, startY);
            startX += _useCircleOfTransparency.Width + 5;
            _circleOfTranspRadius = AddHSlider(rightArea, Constants.MIN_CIRCLE_OF_TRANSPARENCY_RADIUS, Constants.MAX_CIRCLE_OF_TRANSPARENCY_RADIUS, ProfileManager.Current.CircleOfTransparencyRadius, startX, startY, 200);
            startX = 5 + 40;
            startY += _useCircleOfTransparency.Height + 2;
            text = AddLabel(rightArea, ResGumps.CircleTransType, startX, startY);

            int cottypeindex = ProfileManager.Current.CircleOfTransparencyType;
            string[] cotTypes = { ResGumps.CircleTransType_Full, ResGumps.CircleTransType_Gradient };

            if (cottypeindex < 0 || cottypeindex > cotTypes.Length)
            {
                cottypeindex = 0;
            }

            startX += text.Width + 5;
            _cotType = AddCombobox(rightArea, cotTypes, cottypeindex, startX, startY, 150);

            startX = 5;
            startY += _cotType.Height + 2;


            text = AddLabel(rightArea, ResGumps.GridLoot, startX, startY);
            startX += text.Width + 5;
            _gridLoot = AddCombobox(rightArea, new[] {ResGumps.GridLoot_None, ResGumps.GridLoot_GridOnly, ResGumps.GridLoot_Both}, ProfileManager.Current.GridLootType, startX, startY, 120);

            startX = 5;
            startY += _gridLoot.Height + 5;

            _showHpMobile = AddCheckBox(rightArea, ResGumps.ShowHP, ProfileManager.Current.ShowMobilesHP, startX, startY);
            int mode = ProfileManager.Current.MobileHPType;

            if (mode < 0 || mode > 2)
            {
                mode = 0;
            }

            startX += _showHpMobile.Width + 5;
            _hpComboBox = AddCombobox(rightArea, new[] { ResGumps.HP_Percentage, ResGumps.HP_Line, ResGumps.HP_Both }, mode, startX, startY, 100);

            startX += _hpComboBox.Width + 15;
            text = AddLabel(rightArea, ResGumps.HP_Mode, startX, startY);
            startX += text.Width + 5;

            mode = ProfileManager.Current.MobileHPShowWhen;

            if (mode != 0 && mode > 2)
            {
                mode = 0;
            }

            _hpComboBoxShowWhen = AddCombobox(rightArea, new[] {ResGumps.HPShow_Always, ResGumps.HPShow_Less, ResGumps.HPShow_Smart}, mode, startX, startY, 100);

            startX = 40;
            startY += _hpComboBoxShowWhen.Height + 2;

            text = AddLabel(rightArea, ResGumps.CloseHPGumpWhen, startX, startY);
            startX += text.Width + 5;

            mode = ProfileManager.Current.CloseHealthBarType;

            if (mode < 0 || mode > 2)
            {
                mode = 0;
            }

            _healtbarType = AddCombobox(rightArea, new[] { ResGumps.HPType_None, ResGumps.HPType_MobileOOR, ResGumps.HPType_MobileDead }, mode, startX, startY, 150);

            startX = 5;
            startY += _healtbarType.Height + 5;



            text = AddLabel(rightArea, ResGumps.HPFields, startX, startY);
            startX += text.Width;

            mode = ProfileManager.Current.FieldsType;

            if (mode < 0 || mode > 2)
            {
                mode = 0;
            }

            _fieldsType = AddCombobox(rightArea, new[] { ResGumps.HPFields_Normal, ResGumps.HPFields_Static, ResGumps.HPFields_Tile }, mode, startX, startY, 150);
            startY += _fieldsType.Height + 5;

            startX = 5;

            SettingsSection section = new SettingsSection("An option set title", rightArea.Width - 15);
            section.X = startX;
            section.Y = startY;

            section.Add(new Checkbox(0x00D2, 0x00D3, "text", FONT, HUE_FONT));
            section.Add(new Checkbox(0x00D2, 0x00D3, "text", FONT, HUE_FONT));
            section.Add(new Checkbox(0x00D2, 0x00D3, "text", FONT, HUE_FONT));
            section.Add(new Checkbox(0x00D2, 0x00D3, "text", FONT, HUE_FONT));
            section.Add(new Checkbox(0x00D2, 0x00D3, "text", FONT, HUE_FONT));
            section.Add(new Checkbox(0x00D2, 0x00D3, "text", FONT, HUE_FONT));


            startY += section.Height + 5;

            SettingsSection section2 = new SettingsSection("An option set title 2", rightArea.Width - 15);
            section2.X = startX;
            section2.Y = startY;

            section2.Add(new Checkbox(0x00D2, 0x00D3, "text", FONT, HUE_FONT));
            section2.Add(new Checkbox(0x00D2, 0x00D3, "text", FONT, HUE_FONT));
            section2.Add(new Checkbox(0x00D2, 0x00D3, "text", FONT, HUE_FONT));
            section2.Add(new Checkbox(0x00D2, 0x00D3, "text", FONT, HUE_FONT));
            section2.Add(new Checkbox(0x00D2, 0x00D3, "text", FONT, HUE_FONT));
            section2.Add(new Checkbox(0x00D2, 0x00D3, "text", FONT, HUE_FONT));


            rightArea.Add(section);
            rightArea.Add(section2);

            Add(rightArea, PAGE);
        }

        private void BuildSounds()
        {
            const int PAGE = 2;

            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            int startX = 5;
            int startY = 5;

            const int VOLUME_WIDTH = 200;

            _enableSounds = AddCheckBox(rightArea, ResGumps.Sounds, ProfileManager.Current.EnableSound, startX, startY);
            _enableMusic = AddCheckBox(rightArea, ResGumps.Music, ProfileManager.Current.EnableMusic, startX, startY + _enableSounds.Height + 2);
            _loginMusic = AddCheckBox(rightArea, ResGumps.LoginMusic, Settings.GlobalSettings.LoginMusic, startX, startY + _enableSounds.Height + 2 + _enableMusic.Height + 2);

            startX = 120;
            startY += 2;
            _soundsVolume = AddHSlider(rightArea, 0, 100, ProfileManager.Current.SoundVolume, startX, startY, VOLUME_WIDTH);
            _musicVolume = AddHSlider(rightArea, 0, 100, ProfileManager.Current.MusicVolume, startX, startY + _enableSounds.Height + 2, VOLUME_WIDTH);
            _loginMusicVolume = AddHSlider(rightArea, 0, 100, Settings.GlobalSettings.LoginMusicVolume, startX, startY + _enableSounds.Height + 2 + _enableMusic.Height + 2, VOLUME_WIDTH);

            startX = 5;
            startY += _loginMusic.Bounds.Bottom + 2;
            _footStepsSound = AddCheckBox(rightArea, ResGumps.PlayFootsteps, ProfileManager.Current.EnableFootstepsSound, startX, startY);
            startY += _footStepsSound.Height + 2;
            _combatMusic = AddCheckBox(rightArea, ResGumps.CombatMusic, ProfileManager.Current.EnableCombatMusic, startX, startY);
            startY += _combatMusic.Height + 2;
            _musicInBackground = AddCheckBox(rightArea, ResGumps.ReproduceSoundsAndMusic, ProfileManager.Current.ReproduceSoundsInBackground, startX, startY);
            startY += _musicInBackground.Height + 2;

            Add(rightArea, PAGE);
        }

        private void BuildVideo()
        {
            const int PAGE = 3;

            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            int startX = 5;
            int startY = 5;

            _gameWindowFullsize = AddCheckBox(rightArea, ResGumps.AlwaysUseFullsizeGameWindow, ProfileManager.Current.GameWindowFullSize, startX, startY);
            startY += _gameWindowFullsize.Height + 2;
            _windowBorderless = AddCheckBox(rightArea, ResGumps.BorderlessWindow, ProfileManager.Current.WindowBorderless, startX, startY);
            startY += _windowBorderless.Height + 2;
            _gameWindowLock = AddCheckBox(rightArea, ResGumps.LockGameWindowMovingResizing, ProfileManager.Current.GameWindowLock, startX, startY);
            startY += _gameWindowLock.Height + 2;


            Label text = AddLabel(rightArea, ResGumps.GamePlayWindowPosition, startX, startY);
            startX += text.Width + 5;

            _gameWindowPositionX = AddInputField
            (
                rightArea,
                startX, startY,
                50,
                TEXTBOX_HEIGHT,
                null,
                80,
                false,
                true,
                5
            );
            _gameWindowPositionX.SetText(ProfileManager.Current.GameWindowPosition.X.ToString());

            startX += _gameWindowPositionX.Width + 5;
            startX += 5;

            _gameWindowPositionY = AddInputField
            (
                rightArea,
                startX, startY,
                50,
                TEXTBOX_HEIGHT,
                null,
                80,
                false,
                true,
                5
            );
            _gameWindowPositionY.SetText(ProfileManager.Current.GameWindowPosition.Y.ToString());

            startX = text.X;
            startY += _gameWindowPositionY.Height + 2;

            startY += 5;

            text = AddLabel(rightArea, ResGumps.GamePlayWindowSize, startX, startY);
            startX += text.Width + 5;

            _gameWindowWidth = AddInputField
            (
                rightArea,
                startX, startY,
                50,
                TEXTBOX_HEIGHT,
                null,
                80,
                false,
                true,
                5
            );
            _gameWindowWidth.SetText(ProfileManager.Current.GameWindowSize.X.ToString());

            startX += _gameWindowWidth.Width + 5;
            startX += 5;
            _gameWindowHeight = AddInputField
            (
                rightArea,
                startX, startY,
                50,
                TEXTBOX_HEIGHT,
                null,
                80,
                false,
                true,
                5
            );
            _gameWindowHeight.SetText(ProfileManager.Current.GameWindowSize.Y.ToString());

            startX = 5;
            startY += _gameWindowHeight.Height + 2;

            startY += 20;

            text = AddLabel(rightArea, ResGumps.DefaultZoom, startX, startY);
            startX += text.Width + 5;
            _sliderZoom = AddHSlider(rightArea, 0, Client.Game.Scene.Camera.ZoomValuesCount, Client.Game.Scene.Camera.ZoomIndex, startX, startY, 100);
            startX = 40;
            startY += text.Height + 2;
            _zoomCheckbox = AddCheckBox(rightArea, ResGumps.EnableMouseWheelForZoom, ProfileManager.Current.EnableMousewheelScaleZoom, startX, startY);
            startY += _zoomCheckbox.Height + 2;
            _restorezoomCheckbox = AddCheckBox(rightArea, ResGumps.ReleasingCtrlRestoresScale, ProfileManager.Current.RestoreScaleAfterUnpressCtrl, startX, startY);
           
            startX = 5;
            startY += _restorezoomCheckbox.Height + 2;
            
            _enableDeathScreen = AddCheckBox(rightArea, ResGumps.EnableDeathScreen, ProfileManager.Current.EnableDeathScreen, startX, startY);
            startX += _enableDeathScreen.Width + 20;
            _enableBlackWhiteEffect = AddCheckBox(rightArea, ResGumps.BlackWhiteModeForDeadPlayer, ProfileManager.Current.EnableBlackWhiteEffect, startX, startY);

            startX = 5;
            startY += _enableBlackWhiteEffect.Height + 2;

            _use_old_status_gump = AddCheckBox(rightArea, ResGumps.UseOldStatusGump, ProfileManager.Current.UseOldStatusGump, startX, startY);
            _use_old_status_gump.IsVisible = !CUOEnviroment.IsOutlands;

            if (_use_old_status_gump.IsVisible)
            {
                startY += _use_old_status_gump.Height + 2;
            }

            _altLights = AddCheckBox(rightArea, ResGumps.AlternativeLights, ProfileManager.Current.UseAlternativeLights, startX, startY);
            startY += _altLights.Height + 2;
            _enableLight = AddCheckBox(rightArea, ResGumps.LightLevel, ProfileManager.Current.UseCustomLightLevel, startX, startY);
            startX += _enableLight.Width + 5;
            _lightBar = AddHSlider(rightArea, 0, 0x1E, 0x1E - ProfileManager.Current.LightLevel, startX, startY, 250);
            startX = 40;
            startY += _enableLight.Height + 2;
            _darkNights = AddCheckBox(rightArea, ResGumps.DarkNights, ProfileManager.Current.UseDarkNights, startX, startY);
            startY += _darkNights.Height + 2;
            startX = 5;
        
            _useColoredLights = AddCheckBox(rightArea, ResGumps.UseColoredLights, ProfileManager.Current.UseColoredLights, startX, startY);
            startY += _useColoredLights.Height + 2;


            _enableShadows = AddCheckBox(rightArea, ResGumps.Shadows, ProfileManager.Current.ShadowsEnabled, startX, startY);
            startY += _enableShadows.Height + 2;

            text = AddLabel(rightArea, ResGumps.AuraUnderFeet, startX, startY);
            startX += text.Width + 5;
            _auraType = AddCombobox(rightArea, new[] {ResGumps.AuraType_None, ResGumps.AuraType_Warmode, ResGumps.AuraType_CtrlShift, ResGumps.AuraType_Always}, ProfileManager.Current.AuraUnderFeetType, startX, startY, 100);
            startY += _auraType.Height + 2;
            startX = 40;
            _partyAura = AddCheckBox(rightArea, ResGumps.CustomColorAuraForPartyMembers, ProfileManager.Current.PartyAura, startX, startY);
            startX += _partyAura.Width + 10;
            _partyAuraColorPickerBox = AddColorBox(rightArea, startX, startY, ProfileManager.Current.PartyAuraHue, ResGumps.PartyAuraColor);

            startX = 5;
            startY += _partyAura.Height + 2;

            _runMouseInSeparateThread = AddCheckBox(rightArea, ResGumps.RunMouseInASeparateThread, Settings.GlobalSettings.RunMouseInASeparateThread, startX, startY);
            startY += _runMouseInSeparateThread.Height + 2;
            _auraMouse = AddCheckBox(rightArea, ResGumps.AuraOnMouseTarget, ProfileManager.Current.AuraOnMouse, startX, startY);
            startY += _auraMouse.Height + 2;
            _hideChatGradient = AddCheckBox(rightArea, ResGumps.HideChatGradient, ProfileManager.Current.HideChatGradient, startX, startY);
            startY += _hideChatGradient.Height + 2;

            //_xBR = AddCheckBox(rightArea, ResGumps.UseXBREffectBETA, ProfileManager.Current.UseXBR, startX, startY);
            // TODO: due to the new rendering engine, xBR cannot be applied directly to the World render target
            //       we need a PostProcessing system
            //_xBR.IsVisible = false;



            text = AddLabel(rightArea, ResGumps.FilterType, startX, startY);
            startX += text.Width + 5;
            _filterType = AddCombobox
            (
                rightArea, new[] {ResGumps.OFF, string.Format(ResGumps.FilterTypeFormatON, ResGumps.ON, ResGumps.AnisotropicClamp), string.Format(ResGumps.FilterTypeFormatON, ResGumps.ON, ResGumps.LinearClamp)},
                ProfileManager.Current.FilterType, startX, startY, 200
            );

            startX = 5;
            startY += text.Height + 2;

            Add(rightArea, PAGE);
        }


        private void BuildCommands()
        {
            const int PAGE = 4;

            ScrollArea rightArea = new ScrollArea(190, 52 + 25 + 4, 150, 360, true);

            Add(new Line(190, 52 + 25 + 2, 150, 1, Color.Gray.PackedValue), PAGE);
            Add(new Line(191 + 150, 21, 1, 418, Color.Gray.PackedValue), PAGE);
            NiceButton addButton = new NiceButton(190, 20, 130, 20, ButtonAction.Activate, ResGumps.NewMacro) {IsSelectable = false, ButtonParameter = (int) Buttons.NewMacro};
            Add(addButton, PAGE);
            NiceButton delButton = new NiceButton(190, 52, 130, 20, ButtonAction.Activate, ResGumps.DeleteMacro) { IsSelectable = false, ButtonParameter = (int) Buttons.DeleteMacro };
            Add(delButton, PAGE);


            int startX = 5;
            int startY = 5;

            DataBox databox = new DataBox(startX, startY, 1, 1);
            databox.WantUpdateSize = true;
            rightArea.Add(databox);


            addButton.MouseUp += (sender, e) =>
            {
                EntryDialog dialog = new EntryDialog
                (
                    250, 150, ResGumps.MacroName, name =>
                    {
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            return;
                        }

                        MacroManager manager = Client.Game.GetScene<GameScene>()
                                                     .Macros;

                        if (manager.FindMacro(name) != null)
                        {
                            return;
                        }

                        NiceButton nb;

                        databox.Add
                        (
                            nb = new NiceButton(0, 0, 130, 25, ButtonAction.Activate, name)
                            {
                                ButtonParameter = (int) Buttons.Last + 1 + rightArea.Children.Count
                            }
                        );

                        databox.ReArrangeChildren();

                        nb.IsSelected = true;

                        _macroControl?.Dispose();

                        _macroControl = new MacroControl(name)
                        {
                            X = 400,
                            Y = 20
                        };

                        manager.PushToBack(_macroControl.Macro);

                        Add(_macroControl, PAGE);

                        nb.DragBegin += (sss, eee) =>
                        {
                            if (UIManager.IsDragging || Math.Max(Math.Abs(Mouse.LDroppedOffset.X), Math.Abs(Mouse.LDroppedOffset.Y)) < 5
                                                     || nb.ScreenCoordinateX > Mouse.LDropPosition.X || nb.ScreenCoordinateX < Mouse.LDropPosition.X - nb.Width
                                                     || nb.ScreenCoordinateY > Mouse.LDropPosition.Y || nb.ScreenCoordinateY + nb.Height < Mouse.LDropPosition.Y)
                            {
                                return;
                            }

                            MacroControl control = _macroControl.FindControls<MacroControl>()
                                                                          .SingleOrDefault();

                            if (control == null)
                            {
                                return;
                            }

                            UIManager.Gumps.OfType<MacroButtonGump>()
                                     .FirstOrDefault(s => s._macro == control.Macro)
                                     ?.Dispose();

                            MacroButtonGump macroButtonGump = new MacroButtonGump(control.Macro, Mouse.LDropPosition.X, Mouse.LDropPosition.Y);
                            UIManager.Add(macroButtonGump);
                            UIManager.AttemptDragControl(macroButtonGump, new Point(Mouse.Position.X + (macroButtonGump.Width >> 1), Mouse.Position.Y + (macroButtonGump.Height >> 1)), true);
                        };

                        nb.MouseUp += (sss, eee) =>
                        {
                            _macroControl?.Dispose();

                            _macroControl = new MacroControl(name)
                            {
                                X = 400,
                                Y = 20
                            };

                            Add(_macroControl, PAGE);
                        };
                    }
                )
                {
                    CanCloseWithRightClick = true
                };

                UIManager.Add(dialog);
            };
            delButton.MouseUp += (ss, ee) =>
            {
                NiceButton nb = databox.FindControls<NiceButton>()
                                         .SingleOrDefault(a => a.IsSelected);

                if (nb != null)
                {
                    QuestionGump dialog = new QuestionGump
                    (
                        ResGumps.MacroDeleteConfirmation, b =>
                        {
                            if (!b)
                            {
                                return;
                            }

                            if (_macroControl != null)
                            {
                                UIManager.Gumps.OfType<MacroButtonGump>()
                                         .FirstOrDefault(s => s._macro == _macroControl.Macro)
                                         ?.Dispose();

                                Client.Game.GetScene<GameScene>()
                                      .Macros.Remove(_macroControl.Macro);

                                _macroControl.Dispose();
                            }
                            
                            nb.Dispose();
                            databox.ReArrangeChildren();
                        }
                    );

                    UIManager.Add(dialog);
                }
            };



            MacroManager macroManager = Client.Game.GetScene<GameScene>()
                                              .Macros;

            for (Macro macro = (Macro) macroManager.Items; macro != null; macro = (Macro) macro.Next)
            {
                NiceButton nb;

                databox.Add
                (
                    nb = new NiceButton(0, 0, 130, 25, ButtonAction.Activate, macro.Name)
                    {
                        ButtonParameter = (int) Buttons.Last + 1 + rightArea.Children.Count,
                        Tag = macro
                    }
                );

                nb.IsSelected = true;

                nb.DragBegin += (sss, eee) =>
                {
                    NiceButton mupNiceButton = (NiceButton) sss;

                    Macro m = mupNiceButton.Tag as Macro;

                    if (m == null)
                    {
                        return;
                    }

                    if (UIManager.IsDragging || Math.Max(Math.Abs(Mouse.LDroppedOffset.X), Math.Abs(Mouse.LDroppedOffset.Y)) < 5
                                             || nb.ScreenCoordinateX > Mouse.LDropPosition.X || nb.ScreenCoordinateX < Mouse.LDropPosition.X - nb.Width
                                             || nb.ScreenCoordinateY > Mouse.LDropPosition.Y || nb.ScreenCoordinateY + nb.Height < Mouse.LDropPosition.Y)
                    {
                        return;
                    }

                    UIManager.Gumps.OfType<MacroButtonGump>()
                             .FirstOrDefault(s => s._macro == m)
                             ?.Dispose();

                    MacroButtonGump macroButtonGump = new MacroButtonGump(m, Mouse.LDropPosition.X, Mouse.LDropPosition.Y);
                    UIManager.Add(macroButtonGump);
                    UIManager.AttemptDragControl(macroButtonGump, new Point(Mouse.Position.X + (macroButtonGump.Width >> 1), Mouse.Position.Y + (macroButtonGump.Height >> 1)), true);
                };

                nb.MouseUp += (sss, eee) =>
                {
                    NiceButton mupNiceButton = (NiceButton) sss;

                    Macro m = mupNiceButton.Tag as Macro;
                    if (m == null)
                    {
                        return;
                    }

                    _macroControl?.Dispose();

                    _macroControl = new MacroControl(m.Name)
                    {
                        X = 400,
                        Y = 20
                    };

                    Add(_macroControl, PAGE);
                };
            }

            databox.ReArrangeChildren();

            Add(rightArea, PAGE);
        }

        private void BuildTooltip()
        {
            const int PAGE = 5;
            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            int startX = 5;
            int startY = 5;

            _use_tooltip = AddCheckBox(rightArea, ResGumps.UseTooltip, ProfileManager.Current.UseTooltip, startX, startY);

            startY += _use_tooltip.Height + 2;

            startX += 40;

            Label text = AddLabel(rightArea, ResGumps.DelayBeforeDisplay, startX, startY);
            startX += text.Width + 5;
            _delay_before_display_tooltip = AddHSlider(rightArea, 0, 1000, ProfileManager.Current.TooltipDelayBeforeDisplay, startX, startY, 200);

            startX = 5 + 40;
            startY += text.Height + 2;

            text = AddLabel(rightArea, ResGumps.TooltipZoom, startX, startY);
            startX += text.Width + 5;
            _tooltip_zoom = AddHSlider(rightArea, 100, 200, ProfileManager.Current.TooltipDisplayZoom, startX, startY, 200);

            startX = 5 + 40;
            startY += text.Height + 2;

            text = AddLabel(rightArea, ResGumps.TooltipBackgroundOpacity, startX, startY);
            startX += text.Width + 5;
            _tooltip_background_opacity = AddHSlider(rightArea, 0, 100, ProfileManager.Current.TooltipBackgroundOpacity, startX, startY, 200);

            startX = 5 + 40;
            startY += text.Height + 2;

            _tooltip_font_hue = AddColorBox(rightArea, startX, startY, ProfileManager.Current.TooltipTextHue, ResGumps.TooltipFontHue);
            startY += _tooltip_font_hue.Height + 2;

            startY += 15;

            text = AddLabel(rightArea, ResGumps.TooltipFont, startX, startY);
            startY += text.Height + 2;
            startX += 40;
            _tooltip_font_selector = new FontSelector(7, ProfileManager.Current.TooltipFont, ResGumps.TooltipFontSelect)
            {
                X = startX,
                Y = startY
            };
            rightArea.Add(_tooltip_font_selector);

            Add(rightArea, PAGE);
        }

        private void BuildFonts()
        {
            const int PAGE = 6;

            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            int startX = 5;
            int startY = 5;

            _overrideAllFonts = AddCheckBox(rightArea, ResGumps.OverrideGameFont, ProfileManager.Current.OverrideAllFonts, startX, startY);
            startX += _overrideAllFonts.Width + 5;
            _overrideAllFontsIsUnicodeCheckbox = AddCombobox
            (
                rightArea, new[]
                {
                    ResGumps.ASCII, ResGumps.Unicode
                }, ProfileManager.Current.OverrideAllFontsIsUnicode ? 1 : 0,
                startX, startY, 100
            );

            startX = 5;
            startY += _overrideAllFonts.Height + 2;
            _forceUnicodeJournal = AddCheckBox(rightArea, ResGumps.ForceUnicodeInJournal, ProfileManager.Current.ForceUnicodeJournal, startX, startY);
            startY += _forceUnicodeJournal.Height + 2;

            Label text = AddLabel(rightArea, ResGumps.SpeechFont, startX, startY);
            startX += 40;
            startY += text.Height + 2;

            _fontSelectorChat = new FontSelector(20, ProfileManager.Current.ChatFont, ResGumps.ThatSClassicUO)
            {
                X = startX,
                Y = startY
            };
            rightArea.Add(_fontSelectorChat);

            Add(rightArea, PAGE);
        }

        private void BuildSpeech()
        {
            const int PAGE = 7;
            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            int startX = 5;
            int startY = 5;

            _scaleSpeechDelay = AddCheckBox(rightArea, ResGumps.ScaleSpeechDelay, ProfileManager.Current.ScaleSpeechDelay, startX, startY);
            startX += _scaleSpeechDelay.Width + 5;
            _sliderSpeechDelay = AddHSlider(rightArea, 0, 1000, ProfileManager.Current.SpeechDelay, startX, startY, 180);
            startX = 5;
            startY += _scaleSpeechDelay.Height + 2;

            _saveJournalCheckBox = AddCheckBox(rightArea, ResGumps.SaveJournalToFileInGameFolder, ProfileManager.Current.SaveJournalToFile, startX, startY);
            startY += _saveJournalCheckBox.Height + 2;

            if (!ProfileManager.Current.SaveJournalToFile)
            {
                World.Journal.CloseWriter();
            }

            _chatAfterEnter = AddCheckBox(rightArea, ResGumps.ActiveChatWhenPressingEnter, ProfileManager.Current.ActivateChatAfterEnter, startX, startY);
            startX += 40;
            startY += _chatAfterEnter.Height + 2;
            _chatAdditionalButtonsCheckbox = AddCheckBox(rightArea, ResGumps.UseAdditionalButtonsToActivateChat, ProfileManager.Current.ActivateChatAdditionalButtons, startX, startY);
            startY += _chatAdditionalButtonsCheckbox.Height + 2;
            _chatShiftEnterCheckbox = AddCheckBox(rightArea, ResGumps.UseShiftEnterToSendMessage, ProfileManager.Current.ActivateChatShiftEnterSupport, startX, startY);
            startY += _chatShiftEnterCheckbox.Height + 2 + 20;

            startX = 5;

            _randomizeColorsButton = new NiceButton(startX, startY, 140, 25, ButtonAction.Activate, ResGumps.RandomizeSpeechHues) {ButtonParameter = (int) Buttons.Disabled};
            _randomizeColorsButton.MouseUp += (sender, e) =>
            {
                if (e.Button != MouseButtonType.Left)
                {
                    return;
                }

                ushort speechHue = (ushort) RandomHelper.GetValue(2, 0x03b2); //this seems to be the acceptable hue range for chat messages,
                ushort emoteHue = (ushort) RandomHelper.GetValue(2, 0x03b2);  //taken from POL source code.
                ushort yellHue = (ushort) RandomHelper.GetValue(2, 0x03b2);
                ushort whisperHue = (ushort) RandomHelper.GetValue(2, 0x03b2);
                ProfileManager.Current.SpeechHue = speechHue;
                _speechColorPickerBox.SetColor(speechHue, HuesLoader.Instance.GetPolygoneColor(12, speechHue));
                ProfileManager.Current.EmoteHue = emoteHue;
                _emoteColorPickerBox.SetColor(emoteHue, HuesLoader.Instance.GetPolygoneColor(12, emoteHue));
                ProfileManager.Current.YellHue = yellHue;
                _yellColorPickerBox.SetColor(yellHue, HuesLoader.Instance.GetPolygoneColor(12, yellHue));
                ProfileManager.Current.WhisperHue = whisperHue;
                _whisperColorPickerBox.SetColor(whisperHue, HuesLoader.Instance.GetPolygoneColor(12, whisperHue));
            };
            rightArea.Add(_randomizeColorsButton);
            startY += _randomizeColorsButton.Height + 2 + 20;


            _speechColorPickerBox = AddColorBox(rightArea, startX, startY, ProfileManager.Current.SpeechHue, ResGumps.SpeechColor);
            startX += 200;
            _emoteColorPickerBox = AddColorBox(rightArea, startX, startY, ProfileManager.Current.EmoteHue, ResGumps.EmoteColor);
            startY += _emoteColorPickerBox.Height + 2 + 3;
            startX = 5;
            _yellColorPickerBox = AddColorBox(rightArea, startX, startY, ProfileManager.Current.YellHue, ResGumps.YellColor);
            startX += 200;
            _whisperColorPickerBox = AddColorBox(rightArea, startX, startY, ProfileManager.Current.WhisperHue, ResGumps.WhisperColor);
            
            startY += _whisperColorPickerBox.Height + 2 + 3;
            startX = 5;

            _partyMessageColorPickerBox = AddColorBox(rightArea, startX, startY, ProfileManager.Current.PartyMessageHue, ResGumps.PartyMessageColor);
            startX += 200;
            _guildMessageColorPickerBox = AddColorBox(rightArea, startX, startY, ProfileManager.Current.GuildMessageHue, ResGumps.GuildMessageColor);
            startY += _guildMessageColorPickerBox.Height + 2 + 3;
            startX = 5;
            _allyMessageColorPickerBox = AddColorBox(rightArea, startX, startY, ProfileManager.Current.AllyMessageHue, ResGumps.AllianceMessageColor);
            startX += 200;
            _chatMessageColorPickerBox = AddColorBox(rightArea, startX, startY, ProfileManager.Current.ChatMessageHue, ResGumps.ChatMessageColor);
            startY += _chatMessageColorPickerBox.Height + 2 + 3;
            startX = 5;

            Add(rightArea, PAGE);
        }

        private void BuildCombat()
        {
            const int PAGE = 8;

            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            int startX = 5;
            int startY = 5;

            _queryBeforAttackCheckbox = AddCheckBox(rightArea, ResGumps.QueryAttack, ProfileManager.Current.EnabledCriminalActionQuery, startX, startY);
            startY += _queryBeforAttackCheckbox.Height + 2;
            _queryBeforeBeneficialCheckbox = AddCheckBox(rightArea, ResGumps.QueryBeneficialActs, ProfileManager.Current.EnabledBeneficialCriminalActionQuery, startX, startY);
            startY += _queryBeforeBeneficialCheckbox.Height + 2;
            _spellFormatCheckbox = AddCheckBox(rightArea, ResGumps.EnableOverheadSpellFormat, ProfileManager.Current.EnabledSpellFormat, startX, startY);
            startY += _spellFormatCheckbox.Height + 2;
            _spellColoringCheckbox = AddCheckBox(rightArea, ResGumps.EnableOverheadSpellHue, ProfileManager.Current.EnabledSpellHue, startX, startY);
            startY += _spellColoringCheckbox.Height + 2;
            _castSpellsByOneClick = AddCheckBox(rightArea, ResGumps.CastSpellsByOneClick, ProfileManager.Current.CastSpellsByOneClick, startX, startY);
            startY += _castSpellsByOneClick.Height + 2;
            _buffBarTime = AddCheckBox(rightArea, ResGumps.ShowBuffDuration, ProfileManager.Current.BuffBarTime, startX, startY);
            startY += _buffBarTime.Height + 2;

            startY += 40;

            int initialY = startY;

            _innocentColorPickerBox = AddColorBox(rightArea, startX, startY, ProfileManager.Current.InnocentHue, ResGumps.InnocentColor);
            startY += _innocentColorPickerBox.Height + 2 + 3;
            _friendColorPickerBox = AddColorBox(rightArea, startX, startY, ProfileManager.Current.FriendHue, ResGumps.FriendColor);
            startY += _innocentColorPickerBox.Height + 2 + 3;
            _crimialColorPickerBox = AddColorBox(rightArea, startX, startY, ProfileManager.Current.CriminalHue, ResGumps.CriminalColor);
            startY += _innocentColorPickerBox.Height + 2 + 3;
            _genericColorPickerBox = AddColorBox(rightArea, startX, startY, ProfileManager.Current.AnimalHue, ResGumps.AnimalColor);
            startY += _innocentColorPickerBox.Height + 2 + 3;
            _murdererColorPickerBox = AddColorBox(rightArea, startX, startY, ProfileManager.Current.MurdererHue, ResGumps.MurdererColor);
            startY += _innocentColorPickerBox.Height + 2 + 3;
            _enemyColorPickerBox = AddColorBox(rightArea, startX, startY, ProfileManager.Current.EnemyHue, ResGumps.EnemyColor);
            startY += _innocentColorPickerBox.Height + 2 + 3;

            startY = initialY;
            startX += 200;
            _beneficColorPickerBox = AddColorBox(rightArea, startX, startY, ProfileManager.Current.BeneficHue, ResGumps.BeneficSpellHue);
            startY += _beneficColorPickerBox.Height + 2 + 3;
            _harmfulColorPickerBox = AddColorBox(rightArea, startX, startY, ProfileManager.Current.HarmfulHue, ResGumps.HarmfulSpellHue);
            startY += _harmfulColorPickerBox.Height + 2 + 3;
            _neutralColorPickerBox = AddColorBox(rightArea, startX, startY, ProfileManager.Current.NeutralHue, ResGumps.NeutralSpellHue);
            startY += _neutralColorPickerBox.Height + 2 + 3;

            startX = 5;
            startY += (_neutralColorPickerBox.Height + 2 + 3) * 4;

            _spellFormatBox = AddInputField
            (
                rightArea,  
                startX, startY,
                200,
                TEXTBOX_HEIGHT,
                ResGumps.SpellOverheadFormat, 
                0, 
                true,
                false,
                30
            );

            _spellFormatBox.SetText(ProfileManager.Current.SpellDisplayFormat);

            Add(rightArea, PAGE);
        }

        private void BuildCounters()
        {
            const int PAGE = 9;
            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            int startX = 5;
            int startY = 5;


            _enableCounters = AddCheckBox(rightArea, ResGumps.EnableCounters, ProfileManager.Current.CounterBarEnabled, startX, startY);
            startX += 40;
            startY += _enableCounters.Height + 2;
            _highlightOnUse = AddCheckBox(rightArea, ResGumps.HighlightOnUse, ProfileManager.Current.CounterBarHighlightOnUse, startX, startY);
            startY += _highlightOnUse.Height + 2;
            _enableAbbreviatedAmount = AddCheckBox(rightArea, ResGumps.EnableAbbreviatedAmountCountrs, ProfileManager.Current.CounterBarDisplayAbbreviatedAmount, startX, startY);

            startX += _enableAbbreviatedAmount.Width + 5;

            _abbreviatedAmount = AddInputField
            (
                rightArea,
                startX, startY,
                50,
                TEXTBOX_HEIGHT,
                null,
                80,
                false,
                true
            );
            _abbreviatedAmount.SetText(ProfileManager.Current.CounterBarAbbreviatedAmount.ToString());
        
            startX = 5;
            startX += 40;
            startY += _enableAbbreviatedAmount.Height + 2;

            _highlightOnAmount = AddCheckBox(rightArea, ResGumps.HighlightRedWhenBelow, ProfileManager.Current.CounterBarHighlightOnAmount, startX, startY);

            startX += _highlightOnAmount.Width + 5;

            _highlightAmount = AddInputField
            (
                rightArea,
                startX, startY,
                50,
                TEXTBOX_HEIGHT,
                null,
                80,
                false,
                true,
                2
            );
            _highlightAmount.SetText(ProfileManager.Current.CounterBarHighlightAmount.ToString());

            startX = 5;
            startX += 40;
            startY += _highlightAmount.Height + 2 + 5;

            startY += 40;

            Label text = AddLabel(rightArea, ResGumps.CounterLayout, startX, startY);

            startX += 40;
            startY += text.Height + 2;
            text = AddLabel(rightArea, ResGumps.CellSize, startX, startY);

            int initialX = startX;
            startX += text.Width + 5;
            _cellSize = AddHSlider(rightArea, 30, 80, ProfileManager.Current.CounterBarCellSize, startX, startY, 80);


            startX = initialX;
            startY += text.Height + 2 + 15;
            
            _rows = AddInputField
            (
                rightArea,
                startX, startY, 
                50,
                30,
                ResGumps.Counter_Rows,
                80,
                false,
                true,
                5
            );
            _rows.SetText(ProfileManager.Current.CounterBarRows.ToString());


            startX += _rows.Width + 5 + 100;

            _columns = AddInputField
            (
                rightArea,
                startX, startY, 
                50,
                30,
                ResGumps.Counter_Columns,
                80,
                false,
                true,
                5
            );
            _columns.SetText(ProfileManager.Current.CounterBarColumns.ToString());


            Add(rightArea, PAGE);
        }

        private void BuildExperimental()
        {
            const int PAGE = 12;
            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            int startX = 5;
            int startY = 5;

            _disableDefaultHotkeys = AddCheckBox(rightArea, ResGumps.DisableDefaultUOHotkeys, ProfileManager.Current.DisableDefaultHotkeys, startX, startY);
            startX += 40;
            startY += _disableDefaultHotkeys.Height + 2;
            _disableArrowBtn = AddCheckBox(rightArea, ResGumps.DisableArrowsPlayerMovement, ProfileManager.Current.DisableArrowBtn, startX, startY);
            startY += _disableArrowBtn.Height + 2;
            _disableTabBtn = AddCheckBox(rightArea, ResGumps.DisableTab, ProfileManager.Current.DisableTabBtn, startX, startY);
            startY += _disableTabBtn.Height + 2;
            _disableCtrlQWBtn = AddCheckBox(rightArea, ResGumps.DisableMessageHistory, ProfileManager.Current.DisableCtrlQWBtn, startX, startY);
            startY += _disableCtrlQWBtn.Height + 2;
            _disableAutoMove = AddCheckBox(rightArea, ResGumps.DisableClickAutomove, ProfileManager.Current.DisableAutoMove, startX, startY);
            startY += _disableAutoMove.Height + 2;

            Add(rightArea, PAGE);
        }


        private void BuildInfoBar()
        {
            const int PAGE = 10;

            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            int startX = 5;
            int startY = 5;

            _showInfoBar = AddCheckBox(rightArea, ResGumps.ShowInfoBar, ProfileManager.Current.ShowInfoBar, startX, startY);

            startX += 40;
            startY += _showInfoBar.Height + 2;

            Label text = AddLabel(rightArea, ResGumps.DataHighlightType, startX, startY);

            startX += text.Width + 5;
            _infoBarHighlightType = AddCombobox(rightArea, new[] {ResGumps.TextColor, ResGumps.ColoredBars}, ProfileManager.Current.InfoBarHighlightType, startX, startY, 150);

            startX = 5;
            startY += _infoBarHighlightType.Height + 5;
           
            NiceButton nb = new NiceButton(startX, startY, 90, 20, ButtonAction.Activate, ResGumps.AddItem, 0, TEXT_ALIGN_TYPE.TS_LEFT)
            {
                ButtonParameter = -1,
                IsSelectable = true,
                IsSelected = true
            };

            nb.MouseUp += (sender, e) =>
            {
                InfoBarBuilderControl ibbc = new InfoBarBuilderControl(new InfoBarItem("", InfoBarVars.HP, 0x3B9));
                ibbc.X = 5;
                ibbc.Y = _databox.Children.Count * ibbc.Height;
                _infoBarBuilderControls.Add(ibbc);
                _databox.Add(ibbc);
                _databox.WantUpdateSize = true;
            };
            rightArea.Add(nb);


            startY += 40;

            text = AddLabel(rightArea, ResGumps.Label, startX, startY);

            startX += 150;

            text = AddLabel(rightArea, ResGumps.Color, startX, startY);

            startX += 55;
            text = AddLabel(rightArea, ResGumps.Data, startX, startY);

            startX = 5;
            startY += text.Height + 2;

            rightArea.Add(new Line(startX, startY, rightArea.Width, 1, Color.Gray.PackedValue));

            startY += 20;



            InfoBarManager ibmanager = Client.Game.GetScene<GameScene>()
                                             .InfoBars;

            List<InfoBarItem> _infoBarItems = ibmanager.GetInfoBars();

            _infoBarBuilderControls = new List<InfoBarBuilderControl>();

            _databox = new DataBox(startX, startY, 10, 10)
            {
                WantUpdateSize = true
            };


            for (int i = 0; i < _infoBarItems.Count; i++)
            {
                InfoBarBuilderControl ibbc = new InfoBarBuilderControl(_infoBarItems[i]);
                ibbc.X = 5;
                ibbc.Y = i * ibbc.Height;
                _infoBarBuilderControls.Add(ibbc);
                _databox.Add(ibbc);
            }

            rightArea.Add(_databox);

            Add(rightArea, PAGE);
        }

        private void BuildContainers()
        {
            const int PAGE = 11;

            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            int startX = 5;
            int startY = 5;

            Label text = AddLabel(rightArea, ResGumps.ContainerScale, startX, startY);
            startX += text.Width + 5;
            _containersScale = AddHSlider(rightArea, Constants.MIN_CONTAINER_SIZE_PERC, Constants.MAX_CONTAINER_SIZE_PERC, ProfileManager.Current.ContainersScale, startX, startY, 200);
            startX = 5;
            startY += text.Height + 2;
            _containerScaleItems = AddCheckBox(rightArea, ResGumps.ScaleItemsInsideContainers, ProfileManager.Current.ScaleItemsInsideContainers, startX, startY);
            startY += _containerScaleItems.Height + 2;
            _useLargeContianersGumps = AddCheckBox(rightArea, ResGumps.UseLargeContainersGump, ProfileManager.Current.UseLargeContainerGumps, startX, startY);
            _useLargeContianersGumps.IsVisible = Client.Version >= ClientVersion.CV_706000;

            if (_useLargeContianersGumps.IsVisible)
            {
                startY += _useLargeContianersGumps.Height + 2;
            }
            
            _containerDoubleClickToLoot = AddCheckBox(rightArea, ResGumps.DoubleClickLootContainers, ProfileManager.Current.DoubleClickToLootInsideContainers, startX, startY);
            startY += _containerDoubleClickToLoot.Height + 2;
            _relativeDragAnDropItems = AddCheckBox(rightArea, ResGumps.RelativeDragAndDropContainers, ProfileManager.Current.RelativeDragAndDropItems, startX, startY);
            startY += _relativeDragAnDropItems.Height + 2;
            _highlightContainersWhenMouseIsOver = AddCheckBox(rightArea, ResGumps.HighlightContainerWhenSelected, ProfileManager.Current.HighlightContainerWhenSelected, startX, startY);
            startY += _highlightContainersWhenMouseIsOver.Height + 2;
            _overrideContainerLocation = AddCheckBox(rightArea, ResGumps.OverrideContainerGumpLocation, ProfileManager.Current.OverrideContainerLocation, startX, startY);
            startX += _overrideContainerLocation.Width + 5;
            _overrideContainerLocationSetting = AddCombobox(rightArea, new[] {ResGumps.ContLoc_NearContainerPosition, ResGumps.ContLoc_TopRight, ResGumps.ContLoc_LastDraggedPosition, ResGumps.ContLoc_RememberEveryContainer}, ProfileManager.Current.OverrideContainerLocationSetting, startX, startY, 200);

            startX = 5;
            startY += _overrideContainerLocation.Height + 2 + 10;

            NiceButton button = new NiceButton(startX, startY, 130, 30, ButtonAction.Activate, ResGumps.RebuildContainers)
            {
                ButtonParameter = -1,
                IsSelectable = true,
                IsSelected = true
            };

            button.MouseUp += (sender, e) => { ContainerManager.BuildContainerFile(true); };
            rightArea.Add(button);

            Add(rightArea, PAGE);
        }


        public override void OnButtonClick(int buttonID)
        {
            if (buttonID == (int) Buttons.Last + 1)
            {
                // it's the macro buttonssss
                return;
            }

            switch ((Buttons) buttonID)
            {
                case Buttons.Disabled:
                    break;

                case Buttons.Cancel:
                    Dispose();

                    break;

                case Buttons.Apply:
                    Apply();

                    break;

                case Buttons.Default:
                    SetDefault();

                    break;

                case Buttons.Ok:
                    Apply();
                    Dispose();

                    break;

                case Buttons.NewMacro:
                    break;

                case Buttons.DeleteMacro:
                    break;
            }
        }

        private void SetDefault()
        {
            switch (ActivePage)
            {
                case 1: // general
                    _sliderFPS.Value = 60;
                    _reduceFPSWhenInactive.IsChecked = false;
                    _highlightObjects.IsChecked = true;
                    _enableTopbar.IsChecked = false;
                    _holdDownKeyTab.IsChecked = true;
                    _holdDownKeyAlt.IsChecked = true;
                    _closeAllAnchoredGumpsWithRClick.IsChecked = false;
                    _holdShiftForContext.IsChecked = false;
                    _holdAltToMoveGumps.IsChecked = false;
                    _holdShiftToSplitStack.IsChecked = false;
                    _enablePathfind.IsChecked = false;
                    _useShiftPathfind.IsChecked = false;
                    _alwaysRun.IsChecked = false;
                    _alwaysRunUnlessHidden.IsChecked = false;
                    _showHpMobile.IsChecked = false;
                    _hpComboBox.SelectedIndex = 0;
                    _hpComboBoxShowWhen.SelectedIndex = 0;
                    _highlightByState.IsChecked = true;
                    _poisonColorPickerBox.SetColor(0x0044, HuesLoader.Instance.GetPolygoneColor(12, 0x0044));
                    _paralyzedColorPickerBox.SetColor(0x014C, HuesLoader.Instance.GetPolygoneColor(12, 0x014C));
                    _invulnerableColorPickerBox.SetColor(0x0030, HuesLoader.Instance.GetPolygoneColor(12, 0x0030));
                    _drawRoofs.IsChecked = false;
                    _enableCaveBorder.IsChecked = false;
                    _treeToStumps.IsChecked = false;
                    _hideVegetation.IsChecked = false;
                    _noColorOutOfRangeObjects.IsChecked = false;
                    _circleOfTranspRadius.Value = Constants.MIN_CIRCLE_OF_TRANSPARENCY_RADIUS;
                    _cotType.SelectedIndex = 0;
                    _useCircleOfTransparency.IsChecked = false;
                    _healtbarType.SelectedIndex = 0;
                    _fieldsType.SelectedIndex = 0;
                    _useStandardSkillsGump.IsChecked = true;
                    _showCorpseNameIncoming.IsChecked = true;
                    _showMobileNameIncoming.IsChecked = true;
                    _gridLoot.SelectedIndex = 0;
                    _sallosEasyGrab.IsChecked = false;
                    _partyInviteGump.IsChecked = false;
                    _showHouseContent.IsChecked = false;
                    _objectsFading.IsChecked = true;
                    _textFading.IsChecked = true;
                    _enableDragSelect.IsChecked = false;
                    _dragSelectHumanoidsOnly.IsChecked = false;
                    _showTargetRangeIndicator.IsChecked = false;
                    _customBars.IsChecked = false;
                    _customBarsBBG.IsChecked = false;
                    _autoOpenCorpse.IsChecked = false;
                    _autoOpenDoors.IsChecked = false;
                    _smoothDoors.IsChecked = false;
                    _skipEmptyCorpse.IsChecked = false;
                    _saveHealthbars.IsChecked = false;
                    _use_smooth_boat_movement.IsChecked = false;
                    _hideScreenshotStoredInMessage.IsChecked = false;

                    break;

                case 2: // sounds
                    _enableSounds.IsChecked = true;
                    _enableMusic.IsChecked = true;
                    _combatMusic.IsChecked = true;
                    _soundsVolume.Value = 100;
                    _musicVolume.Value = 100;
                    _musicInBackground.IsChecked = false;
                    _footStepsSound.IsChecked = true;
                    _loginMusicVolume.Value = 100;
                    _loginMusic.IsChecked = true;
                    _soundsVolume.IsVisible = _enableSounds.IsChecked;
                    _musicVolume.IsVisible = _enableMusic.IsChecked;

                    break;

                case 3: // video
                    _windowBorderless.IsChecked = false;
                    _zoomCheckbox.IsChecked = false;
                    _restorezoomCheckbox.IsChecked = false;
                    _use_old_status_gump.IsChecked = false;
                    _gameWindowWidth.SetText("600");
                    _gameWindowHeight.SetText("480");
                    _gameWindowPositionX.SetText("20");
                    _gameWindowPositionY.SetText("20");
                    _gameWindowLock.IsChecked = false;
                    _gameWindowFullsize.IsChecked = false;
                    _enableDeathScreen.IsChecked = true;
                    _enableBlackWhiteEffect.IsChecked = true;
                    Client.Game.Scene.Camera.Zoom = 1f;
                    ProfileManager.Current.DefaultScale = 1f;
                    _lightBar.Value = 0;
                    _enableLight.IsChecked = false;
                    _useColoredLights.IsChecked = false;
                    _darkNights.IsChecked = false;
                    _enableShadows.IsChecked = true;
                    _auraType.SelectedIndex = 0;
                    _fieldsType.SelectedIndex = 0;
                    _runMouseInSeparateThread.IsChecked = true;
                    _auraMouse.IsChecked = true;
                    _hideChatGradient.IsChecked = false;
                    _partyAura.IsChecked = true;
                    _partyAuraColorPickerBox.SetColor(0x0044, HuesLoader.Instance.GetPolygoneColor(12, 0x0044));

                    break;

                case 4: // macros
                    break;

                case 5: // tooltip
                    _use_tooltip.IsChecked = true;
                    _tooltip_font_hue.SetColor(0xFFFF, 0xFF7F7F7F);
                    _delay_before_display_tooltip.Value = 200;
                    _tooltip_background_opacity.Value = 70;
                    _tooltip_zoom.Value = 100;
                    _tooltip_font_selector.SetSelectedFont(1);

                    break;

                case 6: // fonts
                    _fontSelectorChat.SetSelectedFont(0);
                    _overrideAllFonts.IsChecked = false;
                    _overrideAllFontsIsUnicodeCheckbox.SelectedIndex = 1;

                    break;

                case 7: // speech
                    _scaleSpeechDelay.IsChecked = true;
                    _sliderSpeechDelay.Value = 100;
                    _speechColorPickerBox.SetColor(0x02B2, HuesLoader.Instance.GetPolygoneColor(12, 0x02B2));
                    _emoteColorPickerBox.SetColor(0x0021, HuesLoader.Instance.GetPolygoneColor(12, 0x0021));
                    _yellColorPickerBox.SetColor(0x0021, HuesLoader.Instance.GetPolygoneColor(12, 0x0021));
                    _whisperColorPickerBox.SetColor(0x0033, HuesLoader.Instance.GetPolygoneColor(12, 0x0033));
                    _partyMessageColorPickerBox.SetColor(0x0044, HuesLoader.Instance.GetPolygoneColor(12, 0x0044));
                    _guildMessageColorPickerBox.SetColor(0x0044, HuesLoader.Instance.GetPolygoneColor(12, 0x0044));
                    _allyMessageColorPickerBox.SetColor(0x0057, HuesLoader.Instance.GetPolygoneColor(12, 0x0057));
                    _chatMessageColorPickerBox.SetColor(0x0256, HuesLoader.Instance.GetPolygoneColor(12, 0x0256));
                    _chatAfterEnter.IsChecked = false;
                    UIManager.SystemChat.IsActive = !_chatAfterEnter.IsChecked;
                    _chatAdditionalButtonsCheckbox.IsChecked = true;
                    _chatShiftEnterCheckbox.IsChecked = true;
                    _saveJournalCheckBox.IsChecked = false;

                    break;

                case 8: // combat
                    _innocentColorPickerBox.SetColor(0x005A, HuesLoader.Instance.GetPolygoneColor(12, 0x005A));
                    _friendColorPickerBox.SetColor(0x0044, HuesLoader.Instance.GetPolygoneColor(12, 0x0044));
                    _crimialColorPickerBox.SetColor(0x03b2, HuesLoader.Instance.GetPolygoneColor(12, 0x03b2));
                    _genericColorPickerBox.SetColor(0x03b2, HuesLoader.Instance.GetPolygoneColor(12, 0x03b2));
                    _murdererColorPickerBox.SetColor(0x0023, HuesLoader.Instance.GetPolygoneColor(12, 0x0023));
                    _enemyColorPickerBox.SetColor(0x0031, HuesLoader.Instance.GetPolygoneColor(12, 0x0031));
                    _queryBeforAttackCheckbox.IsChecked = true;
                    _queryBeforeBeneficialCheckbox.IsChecked = false;
                    _castSpellsByOneClick.IsChecked = false;
                    _buffBarTime.IsChecked = false;
                    _beneficColorPickerBox.SetColor(0x0059, HuesLoader.Instance.GetPolygoneColor(12, 0x0059));
                    _harmfulColorPickerBox.SetColor(0x0020, HuesLoader.Instance.GetPolygoneColor(12, 0x0020));
                    _neutralColorPickerBox.SetColor(0x03b2, HuesLoader.Instance.GetPolygoneColor(12, 0x03b2));
                    _spellFormatBox.SetText(ResGumps.SpellFormat_Default);
                    _spellColoringCheckbox.IsChecked = false;
                    _spellFormatCheckbox.IsChecked = false;

                    break;

                case 9: // counters
                    _enableCounters.IsChecked = false;
                    _highlightOnUse.IsChecked = false;
                    _enableAbbreviatedAmount.IsChecked = false;
                    _columns.SetText("1");
                    _rows.SetText("1");
                    _cellSize.Value = 40;
                    _highlightOnAmount.IsChecked = false;
                    _highlightAmount.SetText("5");
                    _abbreviatedAmount.SetText("1000");

                    break;

                case 10: // info bar


                    break;

                case 11: // containers
                    _containersScale.Value = 100;
                    _containerScaleItems.IsChecked = false;
                    _useLargeContianersGumps.IsChecked = false;
                    _containerDoubleClickToLoot.IsChecked = false;
                    _relativeDragAnDropItems.IsChecked = false;
                    _highlightContainersWhenMouseIsOver.IsChecked = false;
                    _overrideContainerLocation.IsChecked = false;
                    _overrideContainerLocationSetting.SelectedIndex = 0;

                    break;

                case 12: // experimental

                    _disableDefaultHotkeys.IsChecked = false;
                    _disableArrowBtn.IsChecked = false;
                    _disableTabBtn.IsChecked = false;
                    _disableCtrlQWBtn.IsChecked = false;
                    _disableAutoMove.IsChecked = false;

                    break;
            }
        }

        private void Apply()
        {
            WorldViewportGump vp = UIManager.GetGump<WorldViewportGump>();

            // general
            if (Settings.GlobalSettings.FPS != _sliderFPS.Value)
            {
                Client.Game.SetRefreshRate(_sliderFPS.Value);
            }

            ProfileManager.Current.HighlightGameObjects = _highlightObjects.IsChecked;
            ProfileManager.Current.ReduceFPSWhenInactive = _reduceFPSWhenInactive.IsChecked;
            ProfileManager.Current.EnablePathfind = _enablePathfind.IsChecked;
            ProfileManager.Current.UseShiftToPathfind = _useShiftPathfind.IsChecked;
            ProfileManager.Current.AlwaysRun = _alwaysRun.IsChecked;
            ProfileManager.Current.AlwaysRunUnlessHidden = _alwaysRunUnlessHidden.IsChecked;
            ProfileManager.Current.ShowMobilesHP = _showHpMobile.IsChecked;
            ProfileManager.Current.HighlightMobilesByFlags = _highlightByState.IsChecked;
            ProfileManager.Current.PoisonHue = _poisonColorPickerBox.Hue;
            ProfileManager.Current.ParalyzedHue = _paralyzedColorPickerBox.Hue;
            ProfileManager.Current.InvulnerableHue = _invulnerableColorPickerBox.Hue;
            ProfileManager.Current.MobileHPType = _hpComboBox.SelectedIndex;
            ProfileManager.Current.MobileHPShowWhen = _hpComboBoxShowWhen.SelectedIndex;
            ProfileManager.Current.HoldDownKeyTab = _holdDownKeyTab.IsChecked;
            ProfileManager.Current.HoldDownKeyAltToCloseAnchored = _holdDownKeyAlt.IsChecked;
            ProfileManager.Current.CloseAllAnchoredGumpsInGroupWithRightClick = _closeAllAnchoredGumpsWithRClick.IsChecked;
            ProfileManager.Current.HoldShiftForContext = _holdShiftForContext.IsChecked;
            ProfileManager.Current.HoldAltToMoveGumps = _holdAltToMoveGumps.IsChecked;
            ProfileManager.Current.HoldShiftToSplitStack = _holdShiftToSplitStack.IsChecked;
            ProfileManager.Current.CloseHealthBarType = _healtbarType.SelectedIndex;
            ProfileManager.Current.HideScreenshotStoredInMessage = _hideScreenshotStoredInMessage.IsChecked;

            if (ProfileManager.Current.DrawRoofs == _drawRoofs.IsChecked)
            {
                ProfileManager.Current.DrawRoofs = !_drawRoofs.IsChecked;

                Client.Game.GetScene<GameScene>()
                      ?.UpdateMaxDrawZ(true);
            }

            if (ProfileManager.Current.TopbarGumpIsDisabled != _enableTopbar.IsChecked)
            {
                if (_enableTopbar.IsChecked)
                {
                    UIManager.GetGump<TopBarGump>()
                             ?.Dispose();
                }
                else
                {
                    TopBarGump.Create();
                }

                ProfileManager.Current.TopbarGumpIsDisabled = _enableTopbar.IsChecked;
            }

            if (ProfileManager.Current.EnableCaveBorder != _enableCaveBorder.IsChecked)
            {
                StaticFilters.CleanCaveTextures();
                ProfileManager.Current.EnableCaveBorder = _enableCaveBorder.IsChecked;
            }

            if (ProfileManager.Current.TreeToStumps != _treeToStumps.IsChecked)
            {
                StaticFilters.CleanTreeTextures();
                ProfileManager.Current.TreeToStumps = _treeToStumps.IsChecked;
            }

            ProfileManager.Current.FieldsType = _fieldsType.SelectedIndex;
            ProfileManager.Current.HideVegetation = _hideVegetation.IsChecked;
            ProfileManager.Current.NoColorObjectsOutOfRange = _noColorOutOfRangeObjects.IsChecked;
            ProfileManager.Current.UseCircleOfTransparency = _useCircleOfTransparency.IsChecked;

            if (ProfileManager.Current.CircleOfTransparencyRadius != _circleOfTranspRadius.Value)
            {
                ProfileManager.Current.CircleOfTransparencyRadius = _circleOfTranspRadius.Value;
                CircleOfTransparency.Create(ProfileManager.Current.CircleOfTransparencyRadius);
            }

            ProfileManager.Current.CircleOfTransparencyType = _cotType.SelectedIndex;
            ProfileManager.Current.StandardSkillsGump = _useStandardSkillsGump.IsChecked;

            if (_useStandardSkillsGump.IsChecked)
            {
                SkillGumpAdvanced newGump = UIManager.GetGump<SkillGumpAdvanced>();

                if (newGump != null)
                {
                    UIManager.Add
                    (
                        new StandardSkillsGump
                            {X = newGump.X, Y = newGump.Y}
                    );

                    newGump.Dispose();
                }
            }
            else
            {
                StandardSkillsGump standardGump = UIManager.GetGump<StandardSkillsGump>();

                if (standardGump != null)
                {
                    UIManager.Add
                    (
                        new SkillGumpAdvanced
                            {X = standardGump.X, Y = standardGump.Y}
                    );

                    standardGump.Dispose();
                }
            }

            ProfileManager.Current.ShowNewMobileNameIncoming = _showMobileNameIncoming.IsChecked;
            ProfileManager.Current.ShowNewCorpseNameIncoming = _showCorpseNameIncoming.IsChecked;
            ProfileManager.Current.GridLootType = _gridLoot.SelectedIndex;
            ProfileManager.Current.SallosEasyGrab = _sallosEasyGrab.IsChecked;
            ProfileManager.Current.PartyInviteGump = _partyInviteGump.IsChecked;
            ProfileManager.Current.UseObjectsFading = _objectsFading.IsChecked;
            ProfileManager.Current.TextFading = _textFading.IsChecked;
            ProfileManager.Current.UseSmoothBoatMovement = _use_smooth_boat_movement.IsChecked;

            if (ProfileManager.Current.ShowHouseContent != _showHouseContent.IsChecked)
            {
                ProfileManager.Current.ShowHouseContent = _showHouseContent.IsChecked;
                NetClient.Socket.Send(new PShowPublicHouseContent(ProfileManager.Current.ShowHouseContent));
            }


            // sounds
            ProfileManager.Current.EnableSound = _enableSounds.IsChecked;
            ProfileManager.Current.EnableMusic = _enableMusic.IsChecked;
            ProfileManager.Current.EnableFootstepsSound = _footStepsSound.IsChecked;
            ProfileManager.Current.EnableCombatMusic = _combatMusic.IsChecked;
            ProfileManager.Current.ReproduceSoundsInBackground = _musicInBackground.IsChecked;
            ProfileManager.Current.SoundVolume = _soundsVolume.Value;
            ProfileManager.Current.MusicVolume = _musicVolume.Value;
            Settings.GlobalSettings.LoginMusicVolume = _loginMusicVolume.Value;
            Settings.GlobalSettings.LoginMusic = _loginMusic.IsChecked;

            Client.Game.Scene.Audio.UpdateCurrentMusicVolume();
            Client.Game.Scene.Audio.UpdateCurrentSoundsVolume();

            if (!ProfileManager.Current.EnableMusic)
            {
                Client.Game.Scene.Audio.StopMusic();
            }

            if (!ProfileManager.Current.EnableSound)
            {
                Client.Game.Scene.Audio.StopSounds();
            }

            // speech
            ProfileManager.Current.ScaleSpeechDelay = _scaleSpeechDelay.IsChecked;
            ProfileManager.Current.SpeechDelay = _sliderSpeechDelay.Value;
            ProfileManager.Current.SpeechHue = _speechColorPickerBox.Hue;
            ProfileManager.Current.EmoteHue = _emoteColorPickerBox.Hue;
            ProfileManager.Current.YellHue = _yellColorPickerBox.Hue;
            ProfileManager.Current.WhisperHue = _whisperColorPickerBox.Hue;
            ProfileManager.Current.PartyMessageHue = _partyMessageColorPickerBox.Hue;
            ProfileManager.Current.GuildMessageHue = _guildMessageColorPickerBox.Hue;
            ProfileManager.Current.AllyMessageHue = _allyMessageColorPickerBox.Hue;
            ProfileManager.Current.ChatMessageHue = _chatMessageColorPickerBox.Hue;

            if (ProfileManager.Current.ActivateChatAfterEnter != _chatAfterEnter.IsChecked)
            {
                UIManager.SystemChat.IsActive = !_chatAfterEnter.IsChecked;
                ProfileManager.Current.ActivateChatAfterEnter = _chatAfterEnter.IsChecked;
            }

            ProfileManager.Current.ActivateChatAdditionalButtons = _chatAdditionalButtonsCheckbox.IsChecked;
            ProfileManager.Current.ActivateChatShiftEnterSupport = _chatShiftEnterCheckbox.IsChecked;
            ProfileManager.Current.SaveJournalToFile = _saveJournalCheckBox.IsChecked;

            // video
            ProfileManager.Current.EnableDeathScreen = _enableDeathScreen.IsChecked;
            ProfileManager.Current.EnableBlackWhiteEffect = _enableBlackWhiteEffect.IsChecked;

            Client.Game.Scene.Camera.ZoomIndex = _sliderZoom.Value;
            ProfileManager.Current.DefaultScale = Client.Game.Scene.Camera.Zoom;
            ProfileManager.Current.EnableMousewheelScaleZoom = _zoomCheckbox.IsChecked;
            ProfileManager.Current.RestoreScaleAfterUnpressCtrl = _restorezoomCheckbox.IsChecked;

            if (!CUOEnviroment.IsOutlands && _use_old_status_gump.IsChecked != ProfileManager.Current.UseOldStatusGump)
            {
                StatusGumpBase status = StatusGumpBase.GetStatusGump();

                ProfileManager.Current.UseOldStatusGump = _use_old_status_gump.IsChecked;

                if (status != null)
                {
                    status.Dispose();
                    UIManager.Add(StatusGumpBase.AddStatusGump(status.ScreenCoordinateX, status.ScreenCoordinateY));
                }
            }


            int.TryParse(_gameWindowWidth.Text, out int gameWindowSizeWidth);
            int.TryParse(_gameWindowHeight.Text, out int gameWindowSizeHeight);

            if (gameWindowSizeWidth != ProfileManager.Current.GameWindowSize.X || gameWindowSizeHeight != ProfileManager.Current.GameWindowSize.Y)
            {
                if (vp != null)
                {
                    Point n = vp.ResizeGameWindow(new Point(gameWindowSizeWidth, gameWindowSizeHeight));

                    _gameWindowWidth.SetText(n.X.ToString());
                    _gameWindowHeight.SetText(n.Y.ToString());
                }
            }

            int.TryParse(_gameWindowPositionX.Text, out int gameWindowPositionX);
            int.TryParse(_gameWindowPositionY.Text, out int gameWindowPositionY);

            if (gameWindowPositionX != ProfileManager.Current.GameWindowPosition.X || gameWindowPositionY != ProfileManager.Current.GameWindowPosition.Y)
            {
                if (vp != null)
                {
                    vp.Location = ProfileManager.Current.GameWindowPosition = new Point(gameWindowPositionX, gameWindowPositionY);
                }
            }

            if (ProfileManager.Current.GameWindowLock != _gameWindowLock.IsChecked)
            {
                if (vp != null)
                {
                    vp.CanMove = !_gameWindowLock.IsChecked;
                }

                ProfileManager.Current.GameWindowLock = _gameWindowLock.IsChecked;
            }

            if (_gameWindowFullsize.IsChecked && (gameWindowPositionX != -5 || gameWindowPositionY != -5))
            {
                if (ProfileManager.Current.GameWindowFullSize == _gameWindowFullsize.IsChecked)
                {
                    _gameWindowFullsize.IsChecked = false;
                }
            }

            if (ProfileManager.Current.GameWindowFullSize != _gameWindowFullsize.IsChecked)
            {
                Point n = Point.Zero, loc = Point.Zero;

                if (_gameWindowFullsize.IsChecked)
                {
                    if (vp != null)
                    {
                        n = vp.ResizeGameWindow(new Point(Client.Game.Window.ClientBounds.Width, Client.Game.Window.ClientBounds.Height));
                        loc = ProfileManager.Current.GameWindowPosition = vp.Location = new Point(-5, -5);
                    }
                }
                else
                {
                    if (vp != null)
                    {
                        n = vp.ResizeGameWindow(new Point(600, 480));
                        loc = vp.Location = ProfileManager.Current.GameWindowPosition = new Point(20, 20);
                    }
                }

                _gameWindowPositionX.SetText(loc.X.ToString());
                _gameWindowPositionY.SetText(loc.Y.ToString());
                _gameWindowWidth.SetText(n.X.ToString());
                _gameWindowHeight.SetText(n.Y.ToString());

                ProfileManager.Current.GameWindowFullSize = _gameWindowFullsize.IsChecked;
            }

            if (ProfileManager.Current.WindowBorderless != _windowBorderless.IsChecked)
            {
                ProfileManager.Current.WindowBorderless = _windowBorderless.IsChecked;
                Client.Game.SetWindowBorderless(_windowBorderless.IsChecked);
            }

            ProfileManager.Current.UseAlternativeLights = _altLights.IsChecked;
            ProfileManager.Current.UseCustomLightLevel = _enableLight.IsChecked;
            ProfileManager.Current.LightLevel = (byte) (_lightBar.MaxValue - _lightBar.Value);

            if (_enableLight.IsChecked)
            {
                World.Light.Overall = ProfileManager.Current.LightLevel;
                World.Light.Personal = 0;
            }
            else
            {
                World.Light.Overall = World.Light.RealOverall;
                World.Light.Personal = World.Light.RealPersonal;
            }

            ProfileManager.Current.UseColoredLights = _useColoredLights.IsChecked;
            ProfileManager.Current.UseDarkNights = _darkNights.IsChecked;
            ProfileManager.Current.ShadowsEnabled = _enableShadows.IsChecked;
            ProfileManager.Current.AuraUnderFeetType = _auraType.SelectedIndex;
            ProfileManager.Current.FilterType = _filterType.SelectedIndex;
            Client.Game.IsMouseVisible = Settings.GlobalSettings.RunMouseInASeparateThread = _runMouseInSeparateThread.IsChecked;
            ProfileManager.Current.AuraOnMouse = _auraMouse.IsChecked;
            ProfileManager.Current.PartyAura = _partyAura.IsChecked;
            ProfileManager.Current.PartyAuraHue = _partyAuraColorPickerBox.Hue;
            ProfileManager.Current.HideChatGradient = _hideChatGradient.IsChecked;

            // fonts
            ProfileManager.Current.ForceUnicodeJournal = _forceUnicodeJournal.IsChecked;
            byte _fontValue = _fontSelectorChat.GetSelectedFont();
            ProfileManager.Current.OverrideAllFonts = _overrideAllFonts.IsChecked;
            ProfileManager.Current.OverrideAllFontsIsUnicode = _overrideAllFontsIsUnicodeCheckbox.SelectedIndex == 1;

            if (ProfileManager.Current.ChatFont != _fontValue)
            {
                ProfileManager.Current.ChatFont = _fontValue;
                UIManager.SystemChat.TextBoxControl.Font = _fontValue;
            }

            // combat
            ProfileManager.Current.InnocentHue = _innocentColorPickerBox.Hue;
            ProfileManager.Current.FriendHue = _friendColorPickerBox.Hue;
            ProfileManager.Current.CriminalHue = _crimialColorPickerBox.Hue;
            ProfileManager.Current.AnimalHue = _genericColorPickerBox.Hue;
            ProfileManager.Current.EnemyHue = _enemyColorPickerBox.Hue;
            ProfileManager.Current.MurdererHue = _murdererColorPickerBox.Hue;
            ProfileManager.Current.EnabledCriminalActionQuery = _queryBeforAttackCheckbox.IsChecked;
            ProfileManager.Current.EnabledBeneficialCriminalActionQuery = _queryBeforeBeneficialCheckbox.IsChecked;
            ProfileManager.Current.CastSpellsByOneClick = _castSpellsByOneClick.IsChecked;
            ProfileManager.Current.BuffBarTime = _buffBarTime.IsChecked;

            ProfileManager.Current.BeneficHue = _beneficColorPickerBox.Hue;
            ProfileManager.Current.HarmfulHue = _harmfulColorPickerBox.Hue;
            ProfileManager.Current.NeutralHue = _neutralColorPickerBox.Hue;
            ProfileManager.Current.EnabledSpellHue = _spellColoringCheckbox.IsChecked;
            ProfileManager.Current.EnabledSpellFormat = _spellFormatCheckbox.IsChecked;
            ProfileManager.Current.SpellDisplayFormat = _spellFormatBox.Text;

            // macros
            Client.Game.GetScene<GameScene>()
                  .Macros.Save();

            // counters

            bool before = ProfileManager.Current.CounterBarEnabled;
            ProfileManager.Current.CounterBarEnabled = _enableCounters.IsChecked;
            ProfileManager.Current.CounterBarCellSize = _cellSize.Value;
            ProfileManager.Current.CounterBarRows = int.Parse(_rows.Text);
            ProfileManager.Current.CounterBarColumns = int.Parse(_columns.Text);
            ProfileManager.Current.CounterBarHighlightOnUse = _highlightOnUse.IsChecked;

            ProfileManager.Current.CounterBarHighlightAmount = int.Parse(_highlightAmount.Text);
            ProfileManager.Current.CounterBarAbbreviatedAmount = int.Parse(_abbreviatedAmount.Text);
            ProfileManager.Current.CounterBarHighlightOnAmount = _highlightOnAmount.IsChecked;
            ProfileManager.Current.CounterBarDisplayAbbreviatedAmount = _enableAbbreviatedAmount.IsChecked;

            CounterBarGump counterGump = UIManager.GetGump<CounterBarGump>();

            counterGump?.SetLayout
            (
                ProfileManager.Current.CounterBarCellSize,
                ProfileManager.Current.CounterBarRows,
                ProfileManager.Current.CounterBarColumns
            );


            if (before != ProfileManager.Current.CounterBarEnabled)
            {
                if (counterGump == null)
                {
                    if (ProfileManager.Current.CounterBarEnabled)
                    {
                        UIManager.Add(new CounterBarGump(200, 200, ProfileManager.Current.CounterBarCellSize, ProfileManager.Current.CounterBarRows, ProfileManager.Current.CounterBarColumns));
                    }
                }
                else
                {
                    counterGump.IsEnabled = counterGump.IsVisible = ProfileManager.Current.CounterBarEnabled;
                }
            }

            // experimental
            // Reset nested checkboxes if parent checkbox is unchecked
            if (!_disableDefaultHotkeys.IsChecked)
            {
                _disableArrowBtn.IsChecked = false;
                _disableTabBtn.IsChecked = false;
                _disableCtrlQWBtn.IsChecked = false;
                _disableAutoMove.IsChecked = false;
            }

            // NOTE: Keep these assignments AFTER the code above that resets nested checkboxes if parent checkbox is unchecked
            ProfileManager.Current.DisableDefaultHotkeys = _disableDefaultHotkeys.IsChecked;
            ProfileManager.Current.DisableArrowBtn = _disableArrowBtn.IsChecked;
            ProfileManager.Current.DisableTabBtn = _disableTabBtn.IsChecked;
            ProfileManager.Current.DisableCtrlQWBtn = _disableCtrlQWBtn.IsChecked;
            ProfileManager.Current.DisableAutoMove = _disableAutoMove.IsChecked;
            ProfileManager.Current.AutoOpenDoors = _autoOpenDoors.IsChecked;
            ProfileManager.Current.SmoothDoors = _smoothDoors.IsChecked;
            ProfileManager.Current.AutoOpenCorpses = _autoOpenCorpse.IsChecked;
            ProfileManager.Current.AutoOpenCorpseRange = int.Parse(_autoOpenCorpseRange.Text);
            ProfileManager.Current.CorpseOpenOptions = _autoOpenCorpseOptions.SelectedIndex;
            ProfileManager.Current.SkipEmptyCorpse = _skipEmptyCorpse.IsChecked;

            ProfileManager.Current.EnableDragSelect = _enableDragSelect.IsChecked;
            ProfileManager.Current.DragSelectModifierKey = _dragSelectModifierKey.SelectedIndex;
            ProfileManager.Current.DragSelectHumanoidsOnly = _dragSelectHumanoidsOnly.IsChecked;

            ProfileManager.Current.OverrideContainerLocation = _overrideContainerLocation.IsChecked;
            ProfileManager.Current.OverrideContainerLocationSetting = _overrideContainerLocationSetting.SelectedIndex;

            ProfileManager.Current.ShowTargetRangeIndicator = _showTargetRangeIndicator.IsChecked;


            bool updateHealthBars = ProfileManager.Current.CustomBarsToggled != _customBars.IsChecked;
            ProfileManager.Current.CustomBarsToggled = _customBars.IsChecked;

            if (updateHealthBars)
            {
                if (ProfileManager.Current.CustomBarsToggled)
                {
                    List<HealthBarGump> hbgstandard = UIManager.Gumps.OfType<HealthBarGump>()
                                                               .ToList();

                    foreach (HealthBarGump healthbar in hbgstandard)
                    {
                        UIManager.Add(new HealthBarGumpCustom(healthbar.LocalSerial) {X = healthbar.X, Y = healthbar.Y});
                        healthbar.Dispose();
                    }
                }
                else
                {
                    List<HealthBarGumpCustom> hbgcustom = UIManager.Gumps.OfType<HealthBarGumpCustom>()
                                                                   .ToList();

                    foreach (HealthBarGumpCustom customhealthbar in hbgcustom)
                    {
                        UIManager.Add(new HealthBarGump(customhealthbar.LocalSerial) {X = customhealthbar.X, Y = customhealthbar.Y});
                        customhealthbar.Dispose();
                    }
                }
            }

            ProfileManager.Current.CBBlackBGToggled = _customBarsBBG.IsChecked;
            ProfileManager.Current.SaveHealthbars = _saveHealthbars.IsChecked;


            // infobar
            ProfileManager.Current.ShowInfoBar = _showInfoBar.IsChecked;
            ProfileManager.Current.InfoBarHighlightType = _infoBarHighlightType.SelectedIndex;


            InfoBarManager ibmanager = Client.Game.GetScene<GameScene>()
                                             .InfoBars;

            ibmanager.Clear();

            for (int i = 0; i < _infoBarBuilderControls.Count; i++)
            {
                if (!_infoBarBuilderControls[i]
                    .IsDisposed)
                {
                    ibmanager.AddItem
                    (
                        new InfoBarItem
                        (
                            _infoBarBuilderControls[i]
                                .LabelText, _infoBarBuilderControls[i]
                                .Var, _infoBarBuilderControls[i]
                                .Hue
                        )
                    );
                }
            }

            ibmanager.Save();

            InfoBarGump infoBarGump = UIManager.GetGump<InfoBarGump>();

            if (ProfileManager.Current.ShowInfoBar)
            {
                if (infoBarGump == null)
                {
                    UIManager.Add
                    (
                        new InfoBarGump
                            {X = 300, Y = 300}
                    );
                }
                else
                {
                    infoBarGump.ResetItems();
                    infoBarGump.SetInScreen();
                }
            }
            else
            {
                if (infoBarGump != null)
                {
                    infoBarGump.Dispose();
                }
            }


            // containers
            int containerScale = ProfileManager.Current.ContainersScale;

            if ((byte) _containersScale.Value != containerScale || ProfileManager.Current.ScaleItemsInsideContainers != _containerScaleItems.IsChecked)
            {
                containerScale = ProfileManager.Current.ContainersScale = (byte) _containersScale.Value;
                UIManager.ContainerScale = containerScale / 100f;
                ProfileManager.Current.ScaleItemsInsideContainers = _containerScaleItems.IsChecked;

                foreach (ContainerGump resizableGump in UIManager.Gumps.OfType<ContainerGump>())
                {
                    resizableGump.RequestUpdateContents();
                }
            }

            ProfileManager.Current.UseLargeContainerGumps = _useLargeContianersGumps.IsChecked;
            ProfileManager.Current.DoubleClickToLootInsideContainers = _containerDoubleClickToLoot.IsChecked;
            ProfileManager.Current.RelativeDragAndDropItems = _relativeDragAnDropItems.IsChecked;
            ProfileManager.Current.HighlightContainerWhenSelected = _highlightContainersWhenMouseIsOver.IsChecked;


            // tooltip
            ProfileManager.Current.UseTooltip = _use_tooltip.IsChecked;
            ProfileManager.Current.TooltipTextHue = _tooltip_font_hue.Hue;
            ProfileManager.Current.TooltipDelayBeforeDisplay = _delay_before_display_tooltip.Value;
            ProfileManager.Current.TooltipBackgroundOpacity = _tooltip_background_opacity.Value;
            ProfileManager.Current.TooltipDisplayZoom = _tooltip_zoom.Value;
            ProfileManager.Current.TooltipFont = _tooltip_font_selector.GetSelectedFont();

            ProfileManager.Current?.Save
            (
                UIManager.Gumps.OfType<Gump>()
                         .Where(s => s.CanBeSaved)
                         .Reverse()
                         .ToList()
            );
        }

        internal void UpdateVideo()
        {
            _gameWindowWidth.SetText(ProfileManager.Current.GameWindowSize.X.ToString());
            _gameWindowHeight.SetText(ProfileManager.Current.GameWindowSize.Y.ToString());
            _gameWindowPositionX.SetText(ProfileManager.Current.GameWindowPosition.X.ToString());
            _gameWindowPositionY.SetText(ProfileManager.Current.GameWindowPosition.Y.ToString());
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ResetHueVector();

            batcher.Draw2D(LogoTexture, x + 190, y + 20, WIDTH - 250, 400, ref _hueVector);
            batcher.DrawRectangle(Texture2DCache.GetTexture(Color.Gray), x, y, Width, Height, ref _hueVector);

            return base.Draw(batcher, x, y);
        }

        private StbTextBox AddInputField(ScrollArea area, int x, int y, int width, int height, string label = null, int maxWidth = 0, bool set_down = false, bool numbersOnly = false, int maxCharCount = -1)
        {
            StbTextBox elem = new StbTextBox(FONT, maxCharCount, maxWidth)
            {
                Width = width,
                Height = height,
                NumbersOnly = numbersOnly,
            };

            if (label != null)
            {
                Label text = new Label(label, true, HUE_FONT)
                {
                    X = x,
                    Y = y
                };

                if (set_down)
                {
                    elem.X = x;
                    elem.Y = y + text.Height + 2;
                }
                else
                {
                    elem.X = text.Bounds.Right + 10;
                    elem.Y = y;
                }
                
                area.Add(text);
            }
            else
            {
                elem.X = x;
                elem.Y = y;
            }

            area.Add
            (
                new ResizePic(0x0BB8)
                {
                    X = elem.X,
                    Y = elem.Y,
                    Width = elem.Width,
                    Height = elem.Height
                }
            );


            elem.X += 4;
            elem.Y += 4;
            elem.Width -= 8;
            elem.Height -= 8;

            area.Add(elem);

            return elem;
        }

        private Label AddLabel(ScrollArea area, string text, int x, int y)
        {
            Label label = new Label(text, true, HUE_FONT)
            {
                X = x,
                Y = y,
            };

            area.Add(label);

            return label;
        }

        private Checkbox AddCheckBox(ScrollArea area, string text, bool ischecked, int x, int y)
        {
            Checkbox box = new Checkbox(0x00D2, 0x00D3, text, FONT, HUE_FONT)
            {
                IsChecked = ischecked,
                X = x,
                Y = y
            };


            area.Add(box);

            return box;
        }

        private Combobox AddCombobox(ScrollArea area, string[] values, int currentIndex, int x, int y, int width)
        {
            Combobox combobox = new Combobox(x, y, width, values)
            {
                SelectedIndex = currentIndex
            };

            area.Add(combobox);

            return combobox;
        }

        private HSliderBar AddHSlider(ScrollArea area, int min, int max, int value, int x, int y, int width)
        {
            HSliderBar slider = new HSliderBar(x, y, width, min, max, value, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT);

            area.Add(slider);

            return slider;
        }

        private ClickableColorBox AddColorBox(ScrollArea area, int x, int y, ushort hue, string text)
        {
            uint color = 0xFF7F7F7F;

            if (hue != 0xFFFF)
            {
                color = HuesLoader.Instance.GetPolygoneColor(12, hue);
            }

            ClickableColorBox box = new ClickableColorBox(x, y, 13, 14, hue, color);
            area.Add(box);

            area.Add
            (
                new Label(text, true, HUE_FONT)
                {
                    X = x + box.Width + 10,
                    Y = y
                }
            );

            return box;
        }

        private enum Buttons
        {
            Disabled, //no action will be done on these buttons, at least not by OnButtonClick()
            Cancel,
            Apply,
            Default,
            Ok,
            SpeechColor,
            EmoteColor,
            PartyMessageColor,
            GuildMessageColor,
            AllyMessageColor,
            InnocentColor,
            FriendColor,
            CriminalColor,
            EnemyColor,
            MurdererColor,

            NewMacro,
            DeleteMacro,

            Last = DeleteMacro
        }


        private class SettingsSection : Control
        {
            private readonly DataBox _databox;

            public SettingsSection(string title, int width)
            {
                CanMove = true;
                AcceptMouseInput = true;
                WantUpdateSize = false;

                

                Label label = new Label(title, true, HUE_FONT, font: FONT);
                label.X = 5;
                base.Add(label);

                base.Add(new Line(0, label.Height, width - 30, 1, 0xFFbabdc2));

                Width = width;
                Height = label.Height + 1;

                _databox = new DataBox(label.X + 10, label.Height + 4, 0, 0);

                base.Add(_databox);
            }



            public void AddRight(Control c)
            {

            }

            public override void Add(Control c, int page = 0)
            {
                c.Y = _databox.Children.Count != 0
                    ? _databox.Children[_databox.Children.Count - 1]
                              .Bounds.Bottom + 2
                    : 0;

                _databox.Add(c, page);
                _databox.WantUpdateSize = true;

                Height += c.Height + 2;
            }
        }

        private class FontSelector : Control
        {
            private readonly RadioButton[] _buttons;

            public FontSelector(int max_font, int current_font_index, string markup)
            {
                CanMove = false;
                CanCloseWithRightClick = false;

                int y = 0;

                _buttons = new RadioButton[max_font];

                for (byte i = 0; i < max_font; i++)
                {
                    if (FontsLoader.Instance.UnicodeFontExists(i))
                    {
                        Add
                        (
                            _buttons[i] = new RadioButton(0, 0x00D0, 0x00D1, markup, i, HUE_FONT)
                            {
                                Y = y,
                                Tag = i,
                                IsChecked = current_font_index == i
                            }
                        );

                        y += 25;
                    }
                }
            }

            public byte GetSelectedFont()
            {
                for (byte i = 0; i < _buttons.Length; i++)
                {
                    RadioButton b = _buttons[i];

                    if (b != null && b.IsChecked)
                    {
                        return i;
                    }
                }

                return 0xFF;
            }

            public void SetSelectedFont(int index)
            {
                if (index >= 0 && index < _buttons.Length && _buttons[index] != null)
                {
                    _buttons[index]
                        .IsChecked = true;
                }
            }
        }
    }
}