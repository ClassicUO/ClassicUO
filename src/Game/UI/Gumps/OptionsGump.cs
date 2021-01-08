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
// ## BEGIN - END ## //
using ClassicUO.Game.InteropServices.Runtime.UOClassicCombat;
// ## BEGIN - END ## //
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
        private const ushort HUE_FONT = 0xFFFF;
        private const int WIDTH = 700;
        private const int HEIGHT = 500;
        private const int TEXTBOX_HEIGHT = 25;

        private static Texture2D _logoTexture2D;
        private Combobox _auraType, _filterType;
        private Combobox _autoOpenCorpseOptions;
        private InputField _autoOpenCorpseRange;

        //experimental
        private Checkbox _autoOpenDoors,
                         _autoOpenCorpse,
                         _skipEmptyCorpse,
                         _disableTabBtn,
                         _disableCtrlQWBtn,
                         _disableDefaultHotkeys,
                         _disableArrowBtn,
                         _disableAutoMove,
                         _overrideContainerLocation,
                         _smoothDoors,
                         _showTargetRangeIndicator,
                         _customBars,
                         _customBarsBBG,
                         _saveHealthbars;
        private Checkbox _buffBarTime,
                         _castSpellsByOneClick,
                         _queryBeforAttackCheckbox,
                         _queryBeforeBeneficialCheckbox,
                         _spellColoringCheckbox,
                         _spellFormatCheckbox;
        private HSliderBar _cellSize;
        private Checkbox _containerScaleItems,
                         _containerDoubleClickToLoot,
                         _relativeDragAnDropItems,
                         _useLargeContianersGumps,
                         _highlightContainersWhenMouseIsOver;


        // containers
        private HSliderBar _containersScale;
        private Combobox _cotType;
        private DataBox _databox;
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
        private InputField _gameWindowHeight;

        private Checkbox _gameWindowLock, _gameWindowFullsize;
        // GameWindowPosition
        private InputField _gameWindowPositionX;
        private InputField _gameWindowPositionY;

        // GameWindowSize
        private InputField _gameWindowWidth;
        private Combobox _gridLoot;
        private Checkbox _hideScreenshotStoredInMessage;
        private Checkbox _highlightObjects, /*_smoothMovements,*/
                         _enablePathfind,
                         _useShiftPathfind,
                         _alwaysRun,
                         _alwaysRunUnlessHidden,
                         _showHpMobile,
                         _highlightByState,
                         _drawRoofs,
                         // ## BEGIN - END ## // ORIG
                         //_treeToStumps,
                         // ## BEGIN - END ## // ORIG
                         _hideVegetation,
                         _noColorOutOfRangeObjects,
                         _useCircleOfTransparency,
                         _enableTopbar,
                         _holdDownKeyTab,
                         _holdDownKeyAlt,
                         _closeAllAnchoredGumpsWithRClick,
                         _chatAfterEnter,
                         _chatAdditionalButtonsCheckbox,
                         _chatShiftEnterCheckbox,
                         _enableCaveBorder;
        private Checkbox _holdShiftForContext,
                         _holdShiftToSplitStack,
                         _reduceFPSWhenInactive,
                         _sallosEasyGrab,
                         _partyInviteGump,
                         _objectsFading,
                         _textFading,
                         _holdAltToMoveGumps;
        private Combobox _hpComboBox, _healtbarType, _fieldsType, _hpComboBoxShowWhen;

        // infobar
        private List<InfoBarBuilderControl> _infoBarBuilderControls;
        private Combobox _infoBarHighlightType;

        // combat & spells
        private ClickableColorBox _innocentColorPickerBox,
                                  _friendColorPickerBox,
                                  _crimialColorPickerBox,
                                  _canAttackColorPickerBox,
                                  _enemyColorPickerBox,
                                  _murdererColorPickerBox,
                                  _neutralColorPickerBox,
                                  _beneficColorPickerBox,
                                  _harmfulColorPickerBox;
        private HSliderBar _lightBar;

        // macro
        private MacroControl _macroControl;
        private Checkbox _overrideAllFonts;
        private Combobox _overrideAllFontsIsUnicodeCheckbox;
        private Combobox _overrideContainerLocationSetting;
        private ClickableColorBox _poisonColorPickerBox, _paralyzedColorPickerBox, _invulnerableColorPickerBox;
        private NiceButton _randomizeColorsButton;
        private Checkbox _restorezoomCheckbox, _zoomCheckbox;
        private InputField _rows, _columns, _highlightAmount, _abbreviatedAmount;

        // speech
        private Checkbox _scaleSpeechDelay, _saveJournalCheckBox;
        private Checkbox _showHouseContent;
        private Checkbox _showInfoBar;

        // general
        private HSliderBar _sliderFPS, _circleOfTranspRadius;
        private HSliderBar _sliderSpeechDelay;
        private HSliderBar _sliderZoom;
        private HSliderBar _soundsVolume, _musicVolume, _loginMusicVolume;
        private ClickableColorBox _speechColorPickerBox,
                                  _emoteColorPickerBox,
                                  _yellColorPickerBox,
                                  _whisperColorPickerBox,
                                  _partyMessageColorPickerBox,
                                  _guildMessageColorPickerBox,
                                  _allyMessageColorPickerBox,
                                  _chatMessageColorPickerBox,
                                  _partyAuraColorPickerBox;
        private InputField _spellFormatBox;
        private ClickableColorBox _tooltip_font_hue;
        private FontSelector _tooltip_font_selector;

        // video
        private Checkbox _use_old_status_gump,
                         _windowBorderless,
                         _enableDeathScreen,
                         _enableBlackWhiteEffect,
                         _altLights,
                         _enableLight,
                         _enableShadows, _enableShadowsStatics,
                         _auraMouse,
                         _runMouseInSeparateThread,
                         _useColoredLights,
                         _darkNights,
                         _partyAura,
                         _hideChatGradient;
        private Checkbox _use_smooth_boat_movement;

        private Checkbox _use_tooltip;
        private Checkbox _useStandardSkillsGump, _showMobileNameIncoming, _showCorpseNameIncoming;
        private Checkbox _showStatsMessage, _showSkillsMessage;
        private HSliderBar _showSkillsMessageDelta;

        // ## BEGIN - END ## //
        private Checkbox _colorStealth, _colorEnergyBolt, _colorGold, _colorTreeTile, _colorBlockerTile, _highlightTileRange, _highlightTileRangeSpell, _overheadRange, _ownAuraByHP, _infernoBridge, _offscreenTargeting, _spellOnCursor, _previewFields, _overheadSummonTime, _overheadPeaceTime, _mobileHamstrungTime, _SpecialSetLastTargetCliloc, _highlightLastTargetHealthBarOutline, _highlightHealthBarByState;
        private ClickableColorBox _stealthColorPickerBox, _energyBoltColorPickerBox, _goldColorPickerBox, _treeTileColorPickerBox, _blockerTileColorPickerBox, _highlightTileRangeColorPickerBox, _highlightTileRangeColorPickerBoxSpell, _highlightLastTargetTypeColorPickerBox, _highlightLastTargetTypeColorPickerBoxPoison, _highlightLastTargetTypeColorPickerBoxPara, _highlightGlowingWeaponsTypeColorPickerBoxHue, _hueImpassableViewColorPickerBox;
        private Combobox _goldType, _treeType, _blockerType, _stealthNeonType, _energyBoltNeonType, _glowingWeaponsType, _energyBoltArtType, _highlightLastTargetType, _highlightLastTargetTypePoison, _highlightLastTargetTypePara;
        private HSliderBar _highlightTileRangeRange, _highlightTileRangeRangeSpell, _lastTargetRange;
        private InputField _spellOnCursorOffsetX, _spellOnCursorOffsetY, _mobileHamstrungTimeCooldown, _SpecialSetLastTargetClilocText;
        private Checkbox _multipleUnderlinesSelfParty, _multipleUnderlinesSelfPartyBigBars, _useOldHealthBars;
        private HSliderBar _multipleUnderlinesSelfPartyTransparency, _flashingHealthbarTreshold;
        private Checkbox _ignoreStaminaCheck, _blackOutlineStatics, _flashingHealthbarOutlineSelf, _flashingHealthbarOutlineParty, _flashingHealthbarOutlineGreen, _flashingHealthbarOutlineOrange, _flashingHealthbarOutlineAll, _flashingHealthbarNegativeOnly;

        //##UCC##//
        private Checkbox _uccEnableSelf, _uccBandiesPoison, _uccNoRefreshPotAfterHamstrung;
        private InputField _uccActionCooldown, _uccPoucheCooldown, _uccCurepotCooldown, _uccHealpotCooldown, _uccRefreshpotCooldown, _uccWaitForTarget, _uccBandiesHPTreshold, _uccCurepotHPTreshold, _uccHealpotHPTreshold, _uccRefreshpotStamTreshold, _uccAutoRearmAfterDisarmedCooldown, _uccNoRefreshPotAfterHamstrungCooldown, _uccDisarmStrikeCooldown, _uccDisarmAttemptCooldown, _uccHamstringStrikeCooldown, _uccHamstringAttemptCooldown, _uccDisarmedCooldown, _uccHamstrungCooldown, _uccStrengthPotCooldown, _uccDexPotCooldown, _uccRNGMin, _uccRNGMax;
        private Checkbox _uccEnableBuffbar, _uccSwing, _uccDoD, _uccGotD, _uccDoH, _uccGotH, _uccClilocTrigger, _uccMacroTrigger, _uccLocked;
        private Checkbox _uccEnableLines;
        private Checkbox _textureManagerEnabled, _textureManagerHalosEnabled, _textureManagerArrowsEnabled; //##TEXTUREMANAGER##//
        private Checkbox _uccEnableAL, _uccEnableGridLootColoring, _uccBEnableLootAboveID;
        private InputField _uccLootDelay, _uccPurgeDelay, _uccQueueSpeed;
        private InputField _uccLootAboveID, _uccSL_Gray, _uccSL_Blue, _uccSL_Green, _uccSL_Red;

        //##-MISC-##//
        private Checkbox _bandageGump, _wireframeView, _hueImpassableView, _autoWorldmapMarker;
        private InputField _bandageGumpOffsetX, _bandageGumpOffsetY;

        //##GrabBars##//
        private InputField _pullFriendlyBarsX, _pullFriendlyBarsY, _pullFriendlyBarsFinalLocationX, _pullFriendlyBarsFinalLocationY;
        private InputField _pullEnemyBarsX, _pullEnemyBarsY, _pullEnemyBarsFinalLocationX, _pullEnemyBarsFinalLocationY;
        private InputField _pullPartyAllyBarsX, _pullPartyAllyBarsY, _pullPartyAllyBarsFinalLocationX, _pullPartyAllyBarsFinalLocationY;
        // ## BEGIN - END ## //

        private Profile _currentProfile = ProfileManager.CurrentProfile;

        public OptionsGump() : base(0, 0)
        {
            Add
            (
                new AlphaBlendControl(0.05f)
                {
                    X = 1,
                    Y = 1,
                    Width = WIDTH - 2,
                    Height = HEIGHT - 2,
                    Hue = 999
                }
            );


            int i = 0;

            Add
            (
                new NiceButton(10, 10 + 30 * i++, 140, 25, ButtonAction.SwitchPage, ResGumps.General)
                    { IsSelected = true, ButtonParameter = 1 }
            );

            Add
            (
                new NiceButton(10, 10 + 30 * i++, 140, 25, ButtonAction.SwitchPage, ResGumps.Sound)
                    { ButtonParameter = 2 }
            );

            Add
            (
                new NiceButton(10, 10 + 30 * i++, 140, 25, ButtonAction.SwitchPage, ResGumps.Video)
                    { ButtonParameter = 3 }
            );

            Add
            (
                new NiceButton(10, 10 + 30 * i++, 140, 25, ButtonAction.SwitchPage, ResGumps.Macros)
                    { ButtonParameter = 4 }
            );

            Add
            (
                new NiceButton(10, 10 + 30 * i++, 140, 25, ButtonAction.SwitchPage, ResGumps.Tooltip)
                    { ButtonParameter = 5 }
            );

            Add
            (
                new NiceButton(10, 10 + 30 * i++, 140, 25, ButtonAction.SwitchPage, ResGumps.Fonts)
                    { ButtonParameter = 6 }
            );

            Add
            (
                new NiceButton(10, 10 + 30 * i++, 140, 25, ButtonAction.SwitchPage, ResGumps.Speech)
                    { ButtonParameter = 7 }
            );

            Add
            (
                new NiceButton(10, 10 + 30 * i++, 140, 25, ButtonAction.SwitchPage, ResGumps.CombatSpells)
                    { ButtonParameter = 8 }
            );

            Add
            (
                new NiceButton(10, 10 + 30 * i++, 140, 25, ButtonAction.SwitchPage, ResGumps.Counters)
                    { ButtonParameter = 9 }
            );

            Add
            (
                new NiceButton(10, 10 + 30 * i++, 140, 25, ButtonAction.SwitchPage, ResGumps.InfoBar)
                    { ButtonParameter = 10 }
            );

            Add
            (
                new NiceButton(10, 10 + 30 * i++, 140, 25, ButtonAction.SwitchPage, ResGumps.Containers)
                    { ButtonParameter = 11 }
            );

            Add
            (
                new NiceButton(10, 10 + 30 * i++, 140, 25, ButtonAction.SwitchPage, ResGumps.Experimental)
                    { ButtonParameter = 12 }
            );

            // ## BEGIN - END ## //
            Add(new NiceButton(10, 10 + 30 * i++, 140, 25, ButtonAction.SwitchPage, "") { ButtonParameter = 13 });
            Add(new NiceButton(10, 10 + 30 * i++, 140, 25, ButtonAction.SwitchPage, "") { ButtonParameter = 14 });
            Add(new NiceButton(10, 10 + 30 * i++, 140, 25, ButtonAction.SwitchPage, "") { ButtonParameter = 15 });
            // ## BEGIN - END ## //

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
            // ## BEGIN - END ## //
            BuildMods();
            BuildUCC();
            BuildAgents();
            // ## BEGIN - END ## //

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


            DataBox box = new DataBox(startX, startY, rightArea.Width - 15, 1);
            box.WantUpdateSize = true;
            rightArea.Add(box);


            SettingsSection section = AddSettingsSection(box, "General");


            section.Add
            (
                _highlightObjects = AddCheckBox
                    (null, ResGumps.HighlightObjects, _currentProfile.HighlightGameObjects, startX, startY)
            );

            section.Add
            (
                _enablePathfind = AddCheckBox
                    (null, ResGumps.EnablePathfinding, _currentProfile.EnablePathfind, startX, startY)
            );

            section.AddRight
            (
                _useShiftPathfind = AddCheckBox
                    (null, ResGumps.ShiftPathfinding, _currentProfile.UseShiftToPathfind, startX, startY)
            );

            section.Add
                (_alwaysRun = AddCheckBox(null, ResGumps.AlwaysRun, _currentProfile.AlwaysRun, startX, startY));

            section.AddRight
            (
                _alwaysRunUnlessHidden = AddCheckBox
                    (null, ResGumps.AlwaysRunHidden, _currentProfile.AlwaysRunUnlessHidden, startX, startY)
            );

            section.Add
            (
                _autoOpenDoors = AddCheckBox
                    (null, ResGumps.AutoOpenDoors, _currentProfile.AutoOpenDoors, startX, startY)
            );

            section.AddRight
            (
                _smoothDoors = AddCheckBox
                    (null, ResGumps.SmoothDoors, _currentProfile.SmoothDoors, startX, startY)
            );

            section.Add
            (
                _autoOpenCorpse = AddCheckBox
                    (null, ResGumps.AutoOpenCorpses, _currentProfile.AutoOpenCorpses, startX, startY)
            );

            section.PushIndent();
            section.Add(AddLabel(null, ResGumps.CorpseOpenRange, 0, 0));

            section.AddRight
            (
                _autoOpenCorpseRange = AddInputField
                    (null, startX, startY, 50, TEXTBOX_HEIGHT, ResGumps.CorpseOpenRange, 80, false, true, 2)
            );

            _autoOpenCorpseRange.SetText(_currentProfile.AutoOpenCorpseRange.ToString());

            section.Add
            (
                _skipEmptyCorpse = AddCheckBox
                    (null, ResGumps.SkipEmptyCorpses, _currentProfile.SkipEmptyCorpse, startX, startY)
            );

            section.Add(AddLabel(null, ResGumps.CorpseOpenOptions, startX, startY));

            section.AddRight
            (
                _autoOpenCorpseOptions = AddCombobox
                (
                    null, new[]
                    {
                        ResGumps.CorpseOpt_None, ResGumps.CorpseOpt_NotTar, ResGumps.CorpseOpt_NotHid,
                        ResGumps.CorpseOpt_Both
                    }, _currentProfile.CorpseOpenOptions, startX, startY, 150
                ), 2
            );

            section.PopIndent();

            section.Add
            (
                _noColorOutOfRangeObjects = AddCheckBox
                (
                    rightArea, ResGumps.OutOfRangeColor, _currentProfile.NoColorObjectsOutOfRange, startX, startY
                )
            );

            section.Add
            (
                _sallosEasyGrab = AddCheckBox
                    (null, ResGumps.SallosEasyGrab, _currentProfile.SallosEasyGrab, startX, startY)
            );

            section.Add
            (
                _showHouseContent = AddCheckBox
                    (null, ResGumps.ShowHousesContent, _currentProfile.ShowHouseContent, startX, startY)
            );

            _showHouseContent.IsVisible = Client.Version >= ClientVersion.CV_70796;

            section.Add
            (
                _use_smooth_boat_movement = AddCheckBox
                    (null, ResGumps.SmoothBoat, _currentProfile.UseSmoothBoatMovement, startX, startY)
            );

            _use_smooth_boat_movement.IsVisible = Client.Version >= ClientVersion.CV_7090;


            SettingsSection section2 = AddSettingsSection(box, "Mobiles");
            section2.Y = section.Bounds.Bottom + 40;

            section2.Add
            (
                _showHpMobile = AddCheckBox(null, ResGumps.ShowHP, _currentProfile.ShowMobilesHP, startX, startY)
            );

            int mode = _currentProfile.MobileHPType;

            if (mode < 0 || mode > 2)
            {
                mode = 0;
            }

            section2.AddRight
            (
                _hpComboBox = AddCombobox
                (
                    null, new[] { ResGumps.HP_Percentage, ResGumps.HP_Line, ResGumps.HP_Both }, mode, startX, startY,
                    100
                )
            );

            section2.AddRight(AddLabel(null, ResGumps.HP_Mode, startX, startY));

            mode = _currentProfile.MobileHPShowWhen;

            if (mode != 0 && mode > 2)
            {
                mode = 0;
            }

            section2.AddRight
            (
                _hpComboBoxShowWhen = AddCombobox
                (
                    null, new[] { ResGumps.HPShow_Always, ResGumps.HPShow_Less, ResGumps.HPShow_Smart }, mode, startX,
                    startY, 100
                ), 2
            );

            section2.Add
            (
                _highlightByState = AddCheckBox
                    (null, ResGumps.HighlighState, _currentProfile.HighlightMobilesByFlags, startX, startY)
            );

            section2.PushIndent();

            section2.Add
            (
                _poisonColorPickerBox = AddColorBox
                    (null, startX, startY, _currentProfile.PoisonHue, ResGumps.PoisonedColor)
            );

            section2.AddRight(AddLabel(null, ResGumps.PoisonedColor, 0, 0), 2);

            section2.Add
            (
                _paralyzedColorPickerBox = AddColorBox
                    (null, startX, startY, _currentProfile.ParalyzedHue, ResGumps.ParalyzedColor)
            );

            section2.AddRight(AddLabel(null, ResGumps.ParalyzedColor, 0, 0), 2);

            section2.Add
            (
                _invulnerableColorPickerBox = AddColorBox
                    (null, startX, startY, _currentProfile.InvulnerableHue, ResGumps.InvulColor)
            );

            section2.AddRight(AddLabel(null, ResGumps.InvulColor, 0, 0), 2);
            section2.PopIndent();

            section2.Add
            (
                _showMobileNameIncoming = AddCheckBox
                    (null, ResGumps.ShowIncMobiles, _currentProfile.ShowNewMobileNameIncoming, startX, startY)
            );

            section2.Add
            (
                _showCorpseNameIncoming = AddCheckBox
                    (null, ResGumps.ShowIncCorpses, _currentProfile.ShowNewCorpseNameIncoming, startX, startY)
            );

            section2.Add(AddLabel(null, ResGumps.AuraUnderFeet, startX, startY));

            section2.AddRight
            (
                _auraType = AddCombobox
                (
                    null, new[]
                    {
                        ResGumps.AuraType_None, ResGumps.AuraType_Warmode, ResGumps.AuraType_CtrlShift,
                        ResGumps.AuraType_Always
                    }, _currentProfile.AuraUnderFeetType, startX, startY, 100
                ), 2
            );

            section2.PushIndent();

            section2.Add
            (
                _partyAura = AddCheckBox
                    (null, ResGumps.CustomColorAuraForPartyMembers, _currentProfile.PartyAura, startX, startY)
            );

            section2.PushIndent();

            section2.Add
            (
                _partyAuraColorPickerBox = AddColorBox
                    (null, startX, startY, _currentProfile.PartyAuraHue, ResGumps.PartyAuraColor)
            );

            section2.AddRight(AddLabel(null, ResGumps.PartyAuraColor, 0, 0));
            section2.PopIndent();
            section2.PopIndent();

            SettingsSection section3 = AddSettingsSection(box, "Gumps & Context");
            section3.Y = section2.Bounds.Bottom + 40;

            section3.Add
            (
                _enableTopbar = AddCheckBox
                    (null, ResGumps.DisableMenu, _currentProfile.TopbarGumpIsDisabled, 0, 0)
            );

            section3.Add
            (
                _holdDownKeyAlt = AddCheckBox
                    (null, ResGumps.AltCloseGumps, _currentProfile.HoldDownKeyAltToCloseAnchored, 0, 0)
            );

            section3.Add
            (
                _holdAltToMoveGumps = AddCheckBox
                    (null, ResGumps.AltMoveGumps, _currentProfile.HoldAltToMoveGumps, 0, 0)
            );

            section3.Add
            (
                _closeAllAnchoredGumpsWithRClick = AddCheckBox
                (
                    null, ResGumps.ClickCloseAllGumps,
                    _currentProfile.CloseAllAnchoredGumpsInGroupWithRightClick, 0, 0
                )
            );

            section3.Add
            (
                _useStandardSkillsGump = AddCheckBox
                    (null, ResGumps.StandardSkillGump, _currentProfile.StandardSkillsGump, 0, 0)
            );

            section3.Add
            (
                _use_old_status_gump = AddCheckBox
                    (null, ResGumps.UseOldStatusGump, _currentProfile.UseOldStatusGump, startX, startY)
            );

            _use_old_status_gump.IsVisible = !CUOEnviroment.IsOutlands;

            section3.Add
            (
                _partyInviteGump = AddCheckBox
                    (null, ResGumps.ShowGumpPartyInv, _currentProfile.PartyInviteGump, 0, 0)
            );

            section3.Add
            (
                _customBars = AddCheckBox
                    (null, ResGumps.UseCustomHPBars, _currentProfile.CustomBarsToggled, 0, 0)
            );

            section3.AddRight
            (
                _customBarsBBG = AddCheckBox
                    (null, ResGumps.UseBlackBackgr, _currentProfile.CBBlackBGToggled, 0, 0)
            );

            section3.Add
            (
                _saveHealthbars = AddCheckBox
                    (null, ResGumps.SaveHPBarsOnLogout, _currentProfile.SaveHealthbars, 0, 0)
            );

            section3.PushIndent();
            section3.Add(AddLabel(null, ResGumps.CloseHPGumpWhen, 0, 0));

            mode = _currentProfile.CloseHealthBarType;

            if (mode < 0 || mode > 2)
            {
                mode = 0;
            }

            _healtbarType = AddCombobox
            (
                null, new[] { ResGumps.HPType_None, ResGumps.HPType_MobileOOR, ResGumps.HPType_MobileDead }, mode, 0, 0,
                150
            );

            section3.AddRight(_healtbarType);
            section3.PopIndent();
            section3.Add(AddLabel(null, ResGumps.GridLoot, startX, startY));

            section3.AddRight
            (
                _gridLoot = AddCombobox
                (
                    null, new[] { ResGumps.GridLoot_None, ResGumps.GridLoot_GridOnly, ResGumps.GridLoot_Both },
                    _currentProfile.GridLootType, startX, startY, 120
                ), 2
            );

            section3.Add
            (
                _holdShiftForContext = AddCheckBox
                    (null, ResGumps.ShiftContext, _currentProfile.HoldShiftForContext, 0, 0)
            );

            section3.Add
            (
                _holdShiftToSplitStack = AddCheckBox
                    (null, ResGumps.ShiftStack, _currentProfile.HoldShiftToSplitStack, 0, 0)
            );


            SettingsSection section4 = AddSettingsSection(box, "Miscellaneous");
            section4.Y = section3.Bounds.Bottom + 40;

            section4.Add
            (
                _useCircleOfTransparency = AddCheckBox
                    (null, ResGumps.EnableCircleTrans, _currentProfile.UseCircleOfTransparency, startX, startY)
            );

            section4.AddRight
            (
                _circleOfTranspRadius = AddHSlider
                (
                    null, Constants.MIN_CIRCLE_OF_TRANSPARENCY_RADIUS, Constants.MAX_CIRCLE_OF_TRANSPARENCY_RADIUS,
                    _currentProfile.CircleOfTransparencyRadius, startX, startY, 200
                )
            );

            section4.PushIndent();
            section4.Add(AddLabel(null, ResGumps.CircleTransType, startX, startY));
            int cottypeindex = _currentProfile.CircleOfTransparencyType;
            string[] cotTypes = { ResGumps.CircleTransType_Full, ResGumps.CircleTransType_Gradient };

            if (cottypeindex < 0 || cottypeindex > cotTypes.Length)
            {
                cottypeindex = 0;
            }

            section4.AddRight(_cotType = AddCombobox(null, cotTypes, cottypeindex, startX, startY, 150), 2);
            section4.PopIndent();

            section4.Add
            (
                _hideScreenshotStoredInMessage = AddCheckBox
                (
                    null, ResGumps.HideScreenshotStoredInMessage, _currentProfile.HideScreenshotStoredInMessage,
                    0, 0
                )
            );

            section4.Add
            (
                _objectsFading = AddCheckBox
                    (null, ResGumps.ObjAlphaFading, _currentProfile.UseObjectsFading, startX, startY)
            );

            section4.Add
            (
                _textFading = AddCheckBox
                    (null, ResGumps.TextAlphaFading, _currentProfile.TextFading, startX, startY)
            );

            section4.Add
            (
                _showTargetRangeIndicator = AddCheckBox
                    (null, ResGumps.ShowTarRangeIndic, _currentProfile.ShowTargetRangeIndicator, startX, startY)
            );

            section4.Add
            (
                _enableDragSelect = AddCheckBox
                    (null, ResGumps.EnableDragHPBars, _currentProfile.EnableDragSelect, startX, startY)
            );

            section4.PushIndent();
            section4.Add(AddLabel(null, ResGumps.DragKey, startX, startY));

            section4.AddRight
            (
                _dragSelectModifierKey = AddCombobox
                (
                    null, new[] { ResGumps.KeyMod_None, ResGumps.KeyMod_Ctrl, ResGumps.KeyMod_Shift },
                    _currentProfile.DragSelectModifierKey, startX, startY, 100
                )
            );

            section4.Add
            (
                _dragSelectHumanoidsOnly = AddCheckBox
                    (null, ResGumps.DragHumanoidsOnly, _currentProfile.DragSelectHumanoidsOnly, startX, startY)
            );

            section4.PopIndent();

            section4.Add(_showStatsMessage = AddCheckBox(null, ResGumps.ShowStatsChangedMessage, _currentProfile.ShowStatsChangedMessage, startX, startY));
            section4.Add(_showSkillsMessage  = AddCheckBox(null, ResGumps.ShowSkillsChangedMessageBy, _currentProfile.ShowStatsChangedMessage, startX, startY));
            section4.PushIndent();
            section4.AddRight(_showSkillsMessageDelta = AddHSlider(null, 0, 100, _currentProfile.ShowSkillsChangedDeltaValue, startX, startY, 200));
            section4.PopIndent();


            SettingsSection section5 = AddSettingsSection(box, "Terrain & Statics");
            section5.Y = section4.Bounds.Bottom + 40;

            section5.Add
            (
                _drawRoofs = AddCheckBox
                    (null, ResGumps.HideRoofTiles, !_currentProfile.DrawRoofs, startX, startY)
            );

            // ## BEGIN - END ## //  ORIG
            /*
            section5.Add
            (
                _treeToStumps = AddCheckBox
                    (null, ResGumps.TreesStumps, _currentProfile.TreeToStumps, startX, startY)
            );
            */
            // ## BEGIN - END ## //  ORIG

            section5.Add
            (
                _hideVegetation = AddCheckBox
                    (null, ResGumps.HideVegetation, _currentProfile.HideVegetation, startX, startY)
            );

            section5.Add
            (
                _enableCaveBorder = AddCheckBox
                    (null, ResGumps.MarkCaveTiles, _currentProfile.EnableCaveBorder, startX, startY)
            );

            section5.Add(AddLabel(null, ResGumps.HPFields, startX, startY));
            mode = _currentProfile.FieldsType;

            if (mode < 0 || mode > 2)
            {
                mode = 0;
            }

            section5.AddRight
            (
                _fieldsType = AddCombobox
                (
                    null, new[] { ResGumps.HPFields_Normal, ResGumps.HPFields_Static, ResGumps.HPFields_Tile }, mode,
                    startX, startY, 150
                )
            );


            Add(rightArea, PAGE);
        }

        private void BuildSounds()
        {
            const int PAGE = 2;

            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            int startX = 5;
            int startY = 5;

            const int VOLUME_WIDTH = 200;

            _enableSounds = AddCheckBox(rightArea, ResGumps.Sounds, _currentProfile.EnableSound, startX, startY);

            _enableMusic = AddCheckBox
            (
                rightArea, ResGumps.Music, _currentProfile.EnableMusic, startX, startY + _enableSounds.Height + 2
            );

            _loginMusic = AddCheckBox
            (
                rightArea, ResGumps.LoginMusic, Settings.GlobalSettings.LoginMusic, startX,
                startY + _enableSounds.Height + 2 + _enableMusic.Height + 2
            );

            startX = 120;
            startY += 2;

            _soundsVolume = AddHSlider
                (rightArea, 0, 100, _currentProfile.SoundVolume, startX, startY, VOLUME_WIDTH);

            _musicVolume = AddHSlider
            (
                rightArea, 0, 100, _currentProfile.MusicVolume, startX, startY + _enableSounds.Height + 2,
                VOLUME_WIDTH
            );

            _loginMusicVolume = AddHSlider
            (
                rightArea, 0, 100, Settings.GlobalSettings.LoginMusicVolume, startX,
                startY + _enableSounds.Height + 2 + _enableMusic.Height + 2, VOLUME_WIDTH
            );

            startX = 5;
            startY += _loginMusic.Bounds.Bottom + 2;

            _footStepsSound = AddCheckBox
                (rightArea, ResGumps.PlayFootsteps, _currentProfile.EnableFootstepsSound, startX, startY);

            startY += _footStepsSound.Height + 2;

            _combatMusic = AddCheckBox
                (rightArea, ResGumps.CombatMusic, _currentProfile.EnableCombatMusic, startX, startY);

            startY += _combatMusic.Height + 2;

            _musicInBackground = AddCheckBox
            (
                rightArea, ResGumps.ReproduceSoundsAndMusic, _currentProfile.ReproduceSoundsInBackground, startX,
                startY
            );

            startY += _musicInBackground.Height + 2;

            Add(rightArea, PAGE);
        }

        private void BuildVideo()
        {
            const int PAGE = 3;

            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            int startX = 5;
            int startY = 5;


            Label text = AddLabel(rightArea, ResGumps.FPS, startX, startY);
            startX += text.Bounds.Right + 5;

            _sliderFPS = AddHSlider
                (rightArea, Constants.MIN_FPS, Constants.MAX_FPS, Settings.GlobalSettings.FPS, startX, startY, 250);

            startY += text.Bounds.Bottom + 5;

            _reduceFPSWhenInactive = AddCheckBox
                (rightArea, ResGumps.FPSInactive, _currentProfile.ReduceFPSWhenInactive, startX, startY);

            startY += _reduceFPSWhenInactive.Height + 2;

            startX = 5;
            startY += 20;


            DataBox box = new DataBox(startX, startY, rightArea.Width - 15, 1);
            box.WantUpdateSize = true;
            rightArea.Add(box);

            SettingsSection section = AddSettingsSection(box, "Game window");

            section.Add
            (
                _gameWindowFullsize = AddCheckBox
                (
                    null, ResGumps.AlwaysUseFullsizeGameWindow, _currentProfile.GameWindowFullSize, startX,
                    startY
                )
            );

            section.Add
            (
                _windowBorderless = AddCheckBox
                    (null, ResGumps.BorderlessWindow, _currentProfile.WindowBorderless, startX, startY)
            );

            section.Add
            (
                _gameWindowLock = AddCheckBox
                    (null, ResGumps.LockGameWindowMovingResizing, _currentProfile.GameWindowLock, startX, startY)
            );

            section.Add(AddLabel(null, ResGumps.GamePlayWindowPosition, startX, startY));

            section.AddRight
            (
                _gameWindowPositionX = AddInputField
                    (null, startX, startY, 50, TEXTBOX_HEIGHT, null, 80, false, true, 5), 4
            );

            _gameWindowPositionX.SetText(_currentProfile.GameWindowPosition.X.ToString());

            section.AddRight
            (
                _gameWindowPositionY = AddInputField(null, startX, startY, 50, TEXTBOX_HEIGHT, null, 80, false, true, 5)
            );

            _gameWindowPositionY.SetText(_currentProfile.GameWindowPosition.Y.ToString());


            section.Add(AddLabel(null, ResGumps.GamePlayWindowSize, startX, startY));

            section.AddRight
                (_gameWindowWidth = AddInputField(null, startX, startY, 50, TEXTBOX_HEIGHT, null, 80, false, true, 5));

            _gameWindowWidth.SetText(_currentProfile.GameWindowSize.X.ToString());

            section.AddRight
                (_gameWindowHeight = AddInputField(null, startX, startY, 50, TEXTBOX_HEIGHT, null, 80, false, true, 5));

            _gameWindowHeight.SetText(_currentProfile.GameWindowSize.Y.ToString());


            SettingsSection section2 = AddSettingsSection(box, "Zoom");
            section2.Y = section.Bounds.Bottom + 40;
            section2.Add(AddLabel(null, ResGumps.DefaultZoom, startX, startY));

            section2.AddRight
            (
                _sliderZoom = AddHSlider
                (
                    null, 0, Client.Game.Scene.Camera.ZoomValuesCount, Client.Game.Scene.Camera.ZoomIndex, startX,
                    startY, 100
                )
            );

            section2.Add
            (
                _zoomCheckbox = AddCheckBox
                (
                    null, ResGumps.EnableMouseWheelForZoom, _currentProfile.EnableMousewheelScaleZoom, startX,
                    startY
                )
            );

            section2.Add
            (
                _restorezoomCheckbox = AddCheckBox
                (
                    null, ResGumps.ReleasingCtrlRestoresScale, _currentProfile.RestoreScaleAfterUnpressCtrl,
                    startX, startY
                )
            );


            SettingsSection section3 = AddSettingsSection(box, "Lights");
            section3.Y = section2.Bounds.Bottom + 40;

            section3.Add
            (
                _altLights = AddCheckBox
                    (null, ResGumps.AlternativeLights, _currentProfile.UseAlternativeLights, startX, startY)
            );

            section3.Add
            (
                _enableLight = AddCheckBox
                    (null, ResGumps.LightLevel, _currentProfile.UseCustomLightLevel, startX, startY)
            );

            section3.AddRight
                (_lightBar = AddHSlider(null, 0, 0x1E, 0x1E - _currentProfile.LightLevel, startX, startY, 250));

            section3.Add
            (
                _darkNights = AddCheckBox
                    (null, ResGumps.DarkNights, _currentProfile.UseDarkNights, startX, startY)
            );

            section3.Add
            (
                _useColoredLights = AddCheckBox
                    (null, ResGumps.UseColoredLights, _currentProfile.UseColoredLights, startX, startY)
            );


            SettingsSection section4 = AddSettingsSection(box, "Misc");
            section4.Y = section3.Bounds.Bottom + 40;

            section4.Add
            (
                _enableDeathScreen = AddCheckBox
                    (null, ResGumps.EnableDeathScreen, _currentProfile.EnableDeathScreen, startX, startY)
            );

            section4.AddRight
            (
                _enableBlackWhiteEffect = AddCheckBox
                (
                    null, ResGumps.BlackWhiteModeForDeadPlayer, _currentProfile.EnableBlackWhiteEffect, startX,
                    startY
                )
            );

            section4.Add
            (
                _runMouseInSeparateThread = AddCheckBox
                (
                    null, ResGumps.RunMouseInASeparateThread, Settings.GlobalSettings.RunMouseInASeparateThread, startX,
                    startY
                )
            );

            section4.Add
            (
                _auraMouse = AddCheckBox
                    (null, ResGumps.AuraOnMouseTarget, _currentProfile.AuraOnMouse, startX, startY)
            );


            SettingsSection section5 = AddSettingsSection(box, "Shadows");
            section5.Y = section4.Bounds.Bottom + 40;

            section5.Add
            (
                _enableShadows = AddCheckBox
                    (null, ResGumps.Shadows, _currentProfile.ShadowsEnabled, startX, startY)
            );

            section5.PushIndent();
            section5.Add(_enableShadowsStatics = AddCheckBox(null, ResGumps.ShadowStatics, _currentProfile.ShadowsStatics, startX, startY));
            section5.PopIndent();


            SettingsSection section6 = AddSettingsSection(box, "Filters");
            section6.Y = section5.Bounds.Bottom + 40;
            section6.Add(AddLabel(null, ResGumps.FilterType, startX, startY));

            section6.AddRight
            (
                _filterType = AddCombobox
                (
                    null, new[]
                    {
                        ResGumps.OFF,
                        string.Format(ResGumps.FilterTypeFormatON, ResGumps.ON, ResGumps.AnisotropicClamp),
                        string.Format(ResGumps.FilterTypeFormatON, ResGumps.ON, ResGumps.LinearClamp)
                    }, _currentProfile.FilterType, startX, startY, 200
                )
            );


            Add(rightArea, PAGE);
        }


        private void BuildCommands()
        {
            const int PAGE = 4;

            ScrollArea rightArea = new ScrollArea(190, 52 + 25 + 4, 150, 360, true);

            Add(new Line(190, 52 + 25 + 2, 150, 1, Color.Gray.PackedValue), PAGE);
            Add(new Line(191 + 150, 21, 1, 418, Color.Gray.PackedValue), PAGE);

            NiceButton addButton = new NiceButton(190, 20, 130, 20, ButtonAction.Activate, ResGumps.NewMacro)
                { IsSelectable = false, ButtonParameter = (int) Buttons.NewMacro };

            Add(addButton, PAGE);

            NiceButton delButton = new NiceButton(190, 52, 130, 20, ButtonAction.Activate, ResGumps.DeleteMacro)
                { IsSelectable = false, ButtonParameter = (int) Buttons.DeleteMacro };

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

                        MacroManager manager = Client.Game.GetScene<GameScene>().Macros;

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
                            if (UIManager.IsDragging ||
                                Math.Max(Math.Abs(Mouse.LDragOffset.X), Math.Abs(Mouse.LDragOffset.Y)) < 5 ||
                                nb.ScreenCoordinateX > Mouse.LClickPosition.X ||
                                nb.ScreenCoordinateX < Mouse.LClickPosition.X - nb.Width ||
                                nb.ScreenCoordinateY > Mouse.LClickPosition.Y ||
                                nb.ScreenCoordinateY + nb.Height < Mouse.LClickPosition.Y)
                            {
                                return;
                            }

                            MacroControl control = _macroControl.FindControls<MacroControl>().SingleOrDefault();

                            if (control == null)
                            {
                                return;
                            }

                            UIManager.Gumps.OfType<MacroButtonGump>()
                                     .FirstOrDefault(s => s._macro == control.Macro)
                                     ?.Dispose();

                            MacroButtonGump macroButtonGump = new MacroButtonGump
                                (control.Macro, Mouse.Position.X, Mouse.Position.Y);

                            macroButtonGump.X = Mouse.Position.X + (macroButtonGump.Width >> 1);
                            macroButtonGump.Y = Mouse.Position.Y + (macroButtonGump.Height >> 1);

                            UIManager.Add(macroButtonGump);

                            UIManager.AttemptDragControl(macroButtonGump, true);
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
                NiceButton nb = databox.FindControls<NiceButton>().SingleOrDefault(a => a.IsSelected);

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

                                Client.Game.GetScene<GameScene>().Macros.Remove(_macroControl.Macro);

                                _macroControl.Dispose();
                            }

                            nb.Dispose();
                            databox.ReArrangeChildren();
                        }
                    );

                    UIManager.Add(dialog);
                }
            };


            MacroManager macroManager = Client.Game.GetScene<GameScene>().Macros;

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

                    if (UIManager.IsDragging ||
                        Math.Max(Math.Abs(Mouse.LDragOffset.X), Math.Abs(Mouse.LDragOffset.Y)) < 5 ||
                        nb.ScreenCoordinateX > Mouse.LClickPosition.X ||
                        nb.ScreenCoordinateX < Mouse.LClickPosition.X - nb.Width ||
                        nb.ScreenCoordinateY > Mouse.LClickPosition.Y ||
                        nb.ScreenCoordinateY + nb.Height < Mouse.LClickPosition.Y)
                    {
                        return;
                    }

                    UIManager.Gumps.OfType<MacroButtonGump>().FirstOrDefault(s => s._macro == m)?.Dispose();

                    MacroButtonGump macroButtonGump = new MacroButtonGump
                        (m, Mouse.Position.X, Mouse.Position.Y);

                    macroButtonGump.X = Mouse.Position.X + (macroButtonGump.Width >> 1);
                    macroButtonGump.Y = Mouse.Position.Y + (macroButtonGump.Height >> 1);

                    UIManager.Add(macroButtonGump);

                    UIManager.AttemptDragControl(macroButtonGump, true);
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

            _use_tooltip = AddCheckBox
                (rightArea, ResGumps.UseTooltip, _currentProfile.UseTooltip, startX, startY);

            startY += _use_tooltip.Height + 2;

            startX += 40;

            Label text = AddLabel(rightArea, ResGumps.DelayBeforeDisplay, startX, startY);
            startX += text.Width + 5;

            _delay_before_display_tooltip = AddHSlider
                (rightArea, 0, 1000, _currentProfile.TooltipDelayBeforeDisplay, startX, startY, 200);

            startX = 5 + 40;
            startY += text.Height + 2;

            text = AddLabel(rightArea, ResGumps.TooltipZoom, startX, startY);
            startX += text.Width + 5;

            _tooltip_zoom = AddHSlider
                (rightArea, 100, 200, _currentProfile.TooltipDisplayZoom, startX, startY, 200);

            startX = 5 + 40;
            startY += text.Height + 2;

            text = AddLabel(rightArea, ResGumps.TooltipBackgroundOpacity, startX, startY);
            startX += text.Width + 5;

            _tooltip_background_opacity = AddHSlider
                (rightArea, 0, 100, _currentProfile.TooltipBackgroundOpacity, startX, startY, 200);

            startX = 5 + 40;
            startY += text.Height + 2;

            _tooltip_font_hue = AddColorBox
                (rightArea, startX, startY, _currentProfile.TooltipTextHue, ResGumps.TooltipFontHue);

            startY += _tooltip_font_hue.Height + 2;

            startY += 15;

            text = AddLabel(rightArea, ResGumps.TooltipFont, startX, startY);
            startY += text.Height + 2;
            startX += 40;

            _tooltip_font_selector = new FontSelector(7, _currentProfile.TooltipFont, ResGumps.TooltipFontSelect)
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

            _overrideAllFonts = AddCheckBox
                (rightArea, ResGumps.OverrideGameFont, _currentProfile.OverrideAllFonts, startX, startY);

            startX += _overrideAllFonts.Width + 5;

            _overrideAllFontsIsUnicodeCheckbox = AddCombobox
            (
                rightArea, new[]
                {
                    ResGumps.ASCII, ResGumps.Unicode
                }, _currentProfile.OverrideAllFontsIsUnicode ? 1 : 0, startX, startY, 100
            );

            startX = 5;
            startY += _overrideAllFonts.Height + 2;

            _forceUnicodeJournal = AddCheckBox
                (rightArea, ResGumps.ForceUnicodeInJournal, _currentProfile.ForceUnicodeJournal, startX, startY);

            startY += _forceUnicodeJournal.Height + 2;

            Label text = AddLabel(rightArea, ResGumps.SpeechFont, startX, startY);
            startX += 40;
            startY += text.Height + 2;

            _fontSelectorChat = new FontSelector(20, _currentProfile.ChatFont, ResGumps.ThatSClassicUO)
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

            _scaleSpeechDelay = AddCheckBox
                (rightArea, ResGumps.ScaleSpeechDelay, _currentProfile.ScaleSpeechDelay, startX, startY);

            startX += _scaleSpeechDelay.Width + 5;

            _sliderSpeechDelay = AddHSlider
                (rightArea, 0, 1000, _currentProfile.SpeechDelay, startX, startY, 180);

            startX = 5;
            startY += _scaleSpeechDelay.Height + 2;

            _saveJournalCheckBox = AddCheckBox
            (
                rightArea, ResGumps.SaveJournalToFileInGameFolder, _currentProfile.SaveJournalToFile, startX,
                startY
            );

            startY += _saveJournalCheckBox.Height + 2;

            if (!_currentProfile.SaveJournalToFile)
            {
                World.Journal.CloseWriter();
            }

            _chatAfterEnter = AddCheckBox
            (
                rightArea, ResGumps.ActiveChatWhenPressingEnter, _currentProfile.ActivateChatAfterEnter, startX,
                startY
            );

            startX += 40;
            startY += _chatAfterEnter.Height + 2;

            _chatAdditionalButtonsCheckbox = AddCheckBox
            (
                rightArea, ResGumps.UseAdditionalButtonsToActivateChat,
                _currentProfile.ActivateChatAdditionalButtons, startX, startY
            );

            startY += _chatAdditionalButtonsCheckbox.Height + 2;

            _chatShiftEnterCheckbox = AddCheckBox
            (
                rightArea, ResGumps.UseShiftEnterToSendMessage, _currentProfile.ActivateChatShiftEnterSupport,
                startX, startY
            );

            startY += _chatShiftEnterCheckbox.Height + 2;
            startX = 5;

            _hideChatGradient = AddCheckBox
                (rightArea, ResGumps.HideChatGradient, _currentProfile.HideChatGradient, startX, startY);

            startY += _hideChatGradient.Height + 2;

            startY += 20;

            _randomizeColorsButton = new NiceButton
                (startX, startY, 140, 25, ButtonAction.Activate, ResGumps.RandomizeSpeechHues)
                { ButtonParameter = (int) Buttons.Disabled };

            _randomizeColorsButton.MouseUp += (sender, e) =>
            {
                if (e.Button != MouseButtonType.Left)
                {
                    return;
                }

                ushort speechHue = (ushort) RandomHelper.GetValue
                    (2, 0x03b2); //this seems to be the acceptable hue range for chat messages,

                ushort emoteHue = (ushort) RandomHelper.GetValue(2, 0x03b2); //taken from POL source code.
                ushort yellHue = (ushort) RandomHelper.GetValue(2, 0x03b2);
                ushort whisperHue = (ushort) RandomHelper.GetValue(2, 0x03b2);
                _currentProfile.SpeechHue = speechHue;
                _speechColorPickerBox.SetColor(speechHue, HuesLoader.Instance.GetPolygoneColor(12, speechHue));
                _currentProfile.EmoteHue = emoteHue;
                _emoteColorPickerBox.SetColor(emoteHue, HuesLoader.Instance.GetPolygoneColor(12, emoteHue));
                _currentProfile.YellHue = yellHue;
                _yellColorPickerBox.SetColor(yellHue, HuesLoader.Instance.GetPolygoneColor(12, yellHue));
                _currentProfile.WhisperHue = whisperHue;
                _whisperColorPickerBox.SetColor(whisperHue, HuesLoader.Instance.GetPolygoneColor(12, whisperHue));
            };

            rightArea.Add(_randomizeColorsButton);
            startY += _randomizeColorsButton.Height + 2 + 20;


            _speechColorPickerBox = AddColorBox
                (rightArea, startX, startY, _currentProfile.SpeechHue, ResGumps.SpeechColor);

            startX += 200;

            _emoteColorPickerBox = AddColorBox
                (rightArea, startX, startY, _currentProfile.EmoteHue, ResGumps.EmoteColor);

            startY += _emoteColorPickerBox.Height + 2;
            startX = 5;

            _yellColorPickerBox = AddColorBox
                (rightArea, startX, startY, _currentProfile.YellHue, ResGumps.YellColor);

            startX += 200;

            _whisperColorPickerBox = AddColorBox
                (rightArea, startX, startY, _currentProfile.WhisperHue, ResGumps.WhisperColor);

            startY += _whisperColorPickerBox.Height + 2;
            startX = 5;

            _partyMessageColorPickerBox = AddColorBox
                (rightArea, startX, startY, _currentProfile.PartyMessageHue, ResGumps.PartyMessageColor);

            startX += 200;

            _guildMessageColorPickerBox = AddColorBox
                (rightArea, startX, startY, _currentProfile.GuildMessageHue, ResGumps.GuildMessageColor);

            startY += _guildMessageColorPickerBox.Height + 2;
            startX = 5;

            _allyMessageColorPickerBox = AddColorBox
                (rightArea, startX, startY, _currentProfile.AllyMessageHue, ResGumps.AllianceMessageColor);

            startX += 200;

            _chatMessageColorPickerBox = AddColorBox
                (rightArea, startX, startY, _currentProfile.ChatMessageHue, ResGumps.ChatMessageColor);

            startY += _chatMessageColorPickerBox.Height + 2;
            startX = 5;

            Add(rightArea, PAGE);
        }

        private void BuildCombat()
        {
            const int PAGE = 8;

            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            int startX = 5;
            int startY = 5;

            _holdDownKeyTab = AddCheckBox
                (rightArea, ResGumps.TabCombat, _currentProfile.HoldDownKeyTab, startX, startY);

            startY += _holdDownKeyTab.Height + 2;

            _queryBeforAttackCheckbox = AddCheckBox
                (rightArea, ResGumps.QueryAttack, _currentProfile.EnabledCriminalActionQuery, startX, startY);

            startY += _queryBeforAttackCheckbox.Height + 2;

            _queryBeforeBeneficialCheckbox = AddCheckBox
            (
                rightArea, ResGumps.QueryBeneficialActs, _currentProfile.EnabledBeneficialCriminalActionQuery,
                startX, startY
            );

            startY += _queryBeforeBeneficialCheckbox.Height + 2;

            _spellFormatCheckbox = AddCheckBox
            (
                rightArea, ResGumps.EnableOverheadSpellFormat, _currentProfile.EnabledSpellFormat, startX, startY
            );

            startY += _spellFormatCheckbox.Height + 2;

            _spellColoringCheckbox = AddCheckBox
                (rightArea, ResGumps.EnableOverheadSpellHue, _currentProfile.EnabledSpellHue, startX, startY);

            startY += _spellColoringCheckbox.Height + 2;

            _castSpellsByOneClick = AddCheckBox
                (rightArea, ResGumps.CastSpellsByOneClick, _currentProfile.CastSpellsByOneClick, startX, startY);

            startY += _castSpellsByOneClick.Height + 2;

            _buffBarTime = AddCheckBox
                (rightArea, ResGumps.ShowBuffDuration, _currentProfile.BuffBarTime, startX, startY);

            startY += _buffBarTime.Height + 2;

            startY += 40;

            int initialY = startY;

            _innocentColorPickerBox = AddColorBox
                (rightArea, startX, startY, _currentProfile.InnocentHue, ResGumps.InnocentColor);

            startY += _innocentColorPickerBox.Height + 2;

            _friendColorPickerBox = AddColorBox
                (rightArea, startX, startY, _currentProfile.FriendHue, ResGumps.FriendColor);

            startY += _innocentColorPickerBox.Height + 2;

            _crimialColorPickerBox = AddColorBox
                (rightArea, startX, startY, _currentProfile.CriminalHue, ResGumps.CriminalColor);

            startY += _innocentColorPickerBox.Height + 2;

            _canAttackColorPickerBox = AddColorBox
                (rightArea, startX, startY, _currentProfile.CanAttackHue, ResGumps.CanAttackColor);

            startY += _innocentColorPickerBox.Height + 2;

            _murdererColorPickerBox = AddColorBox
                (rightArea, startX, startY, _currentProfile.MurdererHue, ResGumps.MurdererColor);

            startY += _innocentColorPickerBox.Height + 2;

            _enemyColorPickerBox = AddColorBox
                (rightArea, startX, startY, _currentProfile.EnemyHue, ResGumps.EnemyColor);

            startY += _innocentColorPickerBox.Height + 2;

            startY = initialY;
            startX += 200;

            _beneficColorPickerBox = AddColorBox
                (rightArea, startX, startY, _currentProfile.BeneficHue, ResGumps.BeneficSpellHue);

            startY += _beneficColorPickerBox.Height + 2;

            _harmfulColorPickerBox = AddColorBox
                (rightArea, startX, startY, _currentProfile.HarmfulHue, ResGumps.HarmfulSpellHue);

            startY += _harmfulColorPickerBox.Height + 2;

            _neutralColorPickerBox = AddColorBox
                (rightArea, startX, startY, _currentProfile.NeutralHue, ResGumps.NeutralSpellHue);

            startY += _neutralColorPickerBox.Height + 2;

            startX = 5;
            startY += (_neutralColorPickerBox.Height + 2) * 4;

            _spellFormatBox = AddInputField
                (rightArea, startX, startY, 200, TEXTBOX_HEIGHT, ResGumps.SpellOverheadFormat, 0, true, false, 30);

            _spellFormatBox.SetText(_currentProfile.SpellDisplayFormat);

            Add(rightArea, PAGE);
        }

        private void BuildCounters()
        {
            const int PAGE = 9;
            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            int startX = 5;
            int startY = 5;


            _enableCounters = AddCheckBox
                (rightArea, ResGumps.EnableCounters, _currentProfile.CounterBarEnabled, startX, startY);

            startX += 40;
            startY += _enableCounters.Height + 2;

            _highlightOnUse = AddCheckBox
                (rightArea, ResGumps.HighlightOnUse, _currentProfile.CounterBarHighlightOnUse, startX, startY);

            startY += _highlightOnUse.Height + 2;

            _enableAbbreviatedAmount = AddCheckBox
            (
                rightArea, ResGumps.EnableAbbreviatedAmountCountrs,
                _currentProfile.CounterBarDisplayAbbreviatedAmount, startX, startY
            );

            startX += _enableAbbreviatedAmount.Width + 5;

            _abbreviatedAmount = AddInputField(rightArea, startX, startY, 50, TEXTBOX_HEIGHT, null, 80, false, true);

            _abbreviatedAmount.SetText(_currentProfile.CounterBarAbbreviatedAmount.ToString());

            startX = 5;
            startX += 40;
            startY += _enableAbbreviatedAmount.Height + 2;

            _highlightOnAmount = AddCheckBox
            (
                rightArea, ResGumps.HighlightRedWhenBelow, _currentProfile.CounterBarHighlightOnAmount, startX,
                startY
            );

            startX += _highlightOnAmount.Width + 5;

            _highlightAmount = AddInputField(rightArea, startX, startY, 50, TEXTBOX_HEIGHT, null, 80, false, true, 2);

            _highlightAmount.SetText(_currentProfile.CounterBarHighlightAmount.ToString());

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
            _cellSize = AddHSlider(rightArea, 30, 80, _currentProfile.CounterBarCellSize, startX, startY, 80);


            startX = initialX;
            startY += text.Height + 2 + 15;

            _rows = AddInputField(rightArea, startX, startY, 50, 30, ResGumps.Counter_Rows, 80, false, true, 5);

            _rows.SetText(_currentProfile.CounterBarRows.ToString());


            startX += _rows.Width + 5 + 100;

            _columns = AddInputField(rightArea, startX, startY, 50, 30, ResGumps.Counter_Columns, 80, false, true, 5);

            _columns.SetText(_currentProfile.CounterBarColumns.ToString());


            Add(rightArea, PAGE);
        }

        private void BuildExperimental()
        {
            const int PAGE = 12;
            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            int startX = 5;
            int startY = 5;

            _disableDefaultHotkeys = AddCheckBox
            (
                rightArea, ResGumps.DisableDefaultUOHotkeys, _currentProfile.DisableDefaultHotkeys, startX,
                startY
            );

            startX += 40;
            startY += _disableDefaultHotkeys.Height + 2;

            _disableArrowBtn = AddCheckBox
            (
                rightArea, ResGumps.DisableArrowsPlayerMovement, _currentProfile.DisableArrowBtn, startX, startY
            );

            startY += _disableArrowBtn.Height + 2;

            _disableTabBtn = AddCheckBox
                (rightArea, ResGumps.DisableTab, _currentProfile.DisableTabBtn, startX, startY);

            startY += _disableTabBtn.Height + 2;

            _disableCtrlQWBtn = AddCheckBox
                (rightArea, ResGumps.DisableMessageHistory, _currentProfile.DisableCtrlQWBtn, startX, startY);

            startY += _disableCtrlQWBtn.Height + 2;

            _disableAutoMove = AddCheckBox
                (rightArea, ResGumps.DisableClickAutomove, _currentProfile.DisableAutoMove, startX, startY);

            startY += _disableAutoMove.Height + 2;

            Add(rightArea, PAGE);
        }


        private void BuildInfoBar()
        {
            const int PAGE = 10;

            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            int startX = 5;
            int startY = 5;

            _showInfoBar = AddCheckBox
                (rightArea, ResGumps.ShowInfoBar, _currentProfile.ShowInfoBar, startX, startY);

            startX += 40;
            startY += _showInfoBar.Height + 2;

            Label text = AddLabel(rightArea, ResGumps.DataHighlightType, startX, startY);

            startX += text.Width + 5;

            _infoBarHighlightType = AddCombobox
            (
                rightArea, new[] { ResGumps.TextColor, ResGumps.ColoredBars },
                _currentProfile.InfoBarHighlightType, startX, startY, 150
            );

            startX = 5;
            startY += _infoBarHighlightType.Height + 5;

            NiceButton nb = new NiceButton
                (startX, startY, 90, 20, ButtonAction.Activate, ResGumps.AddItem, 0, TEXT_ALIGN_TYPE.TS_LEFT)
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


            InfoBarManager ibmanager = Client.Game.GetScene<GameScene>().InfoBars;

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

            _containersScale = AddHSlider
            (
                rightArea, Constants.MIN_CONTAINER_SIZE_PERC, Constants.MAX_CONTAINER_SIZE_PERC,
                _currentProfile.ContainersScale, startX, startY, 200
            );

            startX = 5;
            startY += text.Height + 2;

            _containerScaleItems = AddCheckBox
            (
                rightArea, ResGumps.ScaleItemsInsideContainers, _currentProfile.ScaleItemsInsideContainers,
                startX, startY
            );

            startY += _containerScaleItems.Height + 2;

            _useLargeContianersGumps = AddCheckBox
            (
                rightArea, ResGumps.UseLargeContainersGump, _currentProfile.UseLargeContainerGumps, startX,
                startY
            );

            _useLargeContianersGumps.IsVisible = Client.Version >= ClientVersion.CV_706000;

            if (_useLargeContianersGumps.IsVisible)
            {
                startY += _useLargeContianersGumps.Height + 2;
            }

            _containerDoubleClickToLoot = AddCheckBox
            (
                rightArea, ResGumps.DoubleClickLootContainers, _currentProfile.DoubleClickToLootInsideContainers,
                startX, startY
            );

            startY += _containerDoubleClickToLoot.Height + 2;

            _relativeDragAnDropItems = AddCheckBox
            (
                rightArea, ResGumps.RelativeDragAndDropContainers, _currentProfile.RelativeDragAndDropItems,
                startX, startY
            );

            startY += _relativeDragAnDropItems.Height + 2;

            _highlightContainersWhenMouseIsOver = AddCheckBox
            (
                rightArea, ResGumps.HighlightContainerWhenSelected,
                _currentProfile.HighlightContainerWhenSelected, startX, startY
            );

            startY += _highlightContainersWhenMouseIsOver.Height + 2;

            _overrideContainerLocation = AddCheckBox
            (
                rightArea, ResGumps.OverrideContainerGumpLocation, _currentProfile.OverrideContainerLocation,
                startX, startY
            );

            startX += _overrideContainerLocation.Width + 5;

            _overrideContainerLocationSetting = AddCombobox
            (
                rightArea, new[]
                {
                    ResGumps.ContLoc_NearContainerPosition, ResGumps.ContLoc_TopRight,
                    ResGumps.ContLoc_LastDraggedPosition, ResGumps.ContLoc_RememberEveryContainer
                }, _currentProfile.OverrideContainerLocationSetting, startX, startY, 200
            );

            startX = 5;
            startY += _overrideContainerLocation.Height + 2 + 10;

            NiceButton button = new NiceButton
                (startX, startY, 130, 30, ButtonAction.Activate, ResGumps.RebuildContainers)
                {
                    ButtonParameter = -1,
                    IsSelectable = true,
                    IsSelected = true
                };

            button.MouseUp += (sender, e) => { ContainerManager.BuildContainerFile(true); };
            rightArea.Add(button);

            Add(rightArea, PAGE);
        }
        // ## BEGIN - END ## //
        private void BuildMods()
        {
            const int PAGE = 13;
            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            int startX = 5;
            int startY = 5;

            DataBox box = new DataBox(startX, startY, rightArea.Width - 15, 1);
            box.WantUpdateSize = true;
            rightArea.Add(box);

            //ART / HUE CHANGES START
            SettingsSection section = AddSettingsSection(box, "-----ART / HUE CHANGES-----");

            section.Add(_colorStealth = AddCheckBox(null, "Color stealth ON / OFF", ProfileManager.CurrentProfile.ColorStealth, startX, startY));
            startY += _colorStealth.Height + 2;

            section.Add(_stealthColorPickerBox = AddColorBox(null, startX, startY, ProfileManager.CurrentProfile.StealthHue, ""));
            startY += _stealthColorPickerBox.Height + 2;

            section.AddRight(AddLabel(null, "Stealth color", 0, 0), 2);

            section.Add(AddLabel(null, "or neon:", startX, startY));

            int mode = ProfileManager.CurrentProfile.StealthNeonType;
            section.AddRight(_stealthNeonType = AddCombobox(null, new[] { "Off", "White", "Pink", "Ice", "Fire" }, mode, startX, startY, 100));
            startY += _stealthNeonType.Height + 2;

            section.Add(_colorEnergyBolt = AddCheckBox(null, "Color energy bolt ON / OFF", ProfileManager.CurrentProfile.ColorEnergyBolt, startX, startY));
            startY += _colorEnergyBolt.Height + 2;

            section.Add(_energyBoltColorPickerBox = AddColorBox(null, startX, startY, ProfileManager.CurrentProfile.EnergyBoltHue, ""));
            startY += _energyBoltColorPickerBox.Height + 2;

            section.AddRight(AddLabel(null, "Energy bolt color", 0, 0), 2);

            section.Add(AddLabel(null, "or neon: ", startX, startY));

            mode = ProfileManager.CurrentProfile.EnergyBoltNeonType;
            section.AddRight(_energyBoltNeonType = AddCombobox(null, new[] { "Off", "White", "Pink", "Ice", "Fire" }, mode, startX, startY, 100));
            startY += _energyBoltNeonType.Height + 2;

            section.Add(AddLabel(null, "Change energy bolt art to:", startX, startY));

            mode = ProfileManager.CurrentProfile.EnergyBoltArtType;
            section.AddRight(_energyBoltArtType = AddCombobox(null, new[] { "Normal", "Explo", "Bagball" }, mode, startX, startY, 100));
            startY += _energyBoltArtType.Height + 2;

            section.Add(AddLabel(null, "Change gold art to:", startX, startY));

            mode = ProfileManager.CurrentProfile.GoldType;
            section.AddRight(_goldType = AddCombobox(null, new[] { "Normal", "Cannonball", "Prev Coin" }, mode, startX, startY, 100));
            startY += _goldType.Height + 2;

            section.Add(_colorGold = AddCheckBox(null, "Color cannonball or prev coin ON / OFF", ProfileManager.CurrentProfile.ColorGold, startX, startY));
            startY += _colorGold.Height + 2;

            section.Add(_goldColorPickerBox = AddColorBox(null, startX, startY, ProfileManager.CurrentProfile.GoldHue, ""));
            startY += _goldColorPickerBox.Height + 2;

            section.AddRight(AddLabel(null, "Cannonball or prev coin color", 0, 0), 2);

            section.Add(AddLabel(null, "Change tree art to:", startX, startY));

            mode = ProfileManager.CurrentProfile.TreeType;
            section.AddRight(_treeType = AddCombobox(null, new[] { "Normal", "Stump", "Tile" }, mode, startX, startY, 100));
            startY += _treeType.Height + 2;

            section.Add(_colorTreeTile = AddCheckBox(null, "Color stump or tile ON / OFF", ProfileManager.CurrentProfile.ColorTreeTile, startX, startY));
            startY += _colorTreeTile.Height + 2;

            section.Add(_treeTileColorPickerBox = AddColorBox(null, startX, startY, ProfileManager.CurrentProfile.TreeTileHue, ""));
            startY += _treeTileColorPickerBox.Height + 2;

            section.AddRight(AddLabel(null, "Stump or tile color", 0, 0), 2);

            section.Add(AddLabel(null, "Blocker Type:", startX, startY));

            mode = ProfileManager.CurrentProfile.BlockerType;
            section.AddRight(_blockerType = AddCombobox(null, new[] { "Normal", "Stump", "Tile" }, mode, startX, startY, 100));
            startY += _blockerType.Height + 2;

            section.Add(_colorBlockerTile = AddCheckBox(null, "Color stump or tile", ProfileManager.CurrentProfile.ColorBlockerTile, startX, startY));
            startY += _colorBlockerTile.Height + 2;

            section.Add(_blockerTileColorPickerBox = AddColorBox(null, startX, startY, ProfileManager.CurrentProfile.BlockerTileHue, ""));
            startY += _blockerTileColorPickerBox.Height + 2;

            section.AddRight(AddLabel(null, "Stump or tile color", 0, 0), 2);
            //ART / HUE CHANGES END

            //VISUAL HELPERS START
            SettingsSection section2 = AddSettingsSection(box, "-----VISUAL HELPERS-----");
            section2.Y = section.Bounds.Bottom + 40;

            startY = section.Bounds.Bottom + 40;

            section2.Add(_highlightTileRange = AddCheckBox(null, "Highlight tiles on range", ProfileManager.CurrentProfile.HighlightTileAtRange, startX, startY));
            startY += _highlightTileRange.Height + 2;

            section2.Add(AddLabel(null, "@ range: ", startX, startY));

            section2.AddRight(_highlightTileRangeRange = AddHSlider(null, 1, 20, ProfileManager.CurrentProfile.HighlightTileAtRangeRange, startX, startY, 200));
            startY += _highlightTileRangeRange.Height + 2;

            section2.Add(_highlightTileRangeColorPickerBox = AddColorBox(null, startX, startY, ProfileManager.CurrentProfile.HighlightTileRangeHue, ""));
            startY += _highlightTileRangeColorPickerBox.Height + 2;
            section2.AddRight(AddLabel(null, "Tile color", 0, 0), 2);

            section2.Add(_highlightTileRangeSpell = AddCheckBox(null, "Highlight tiles on range for spells", ProfileManager.CurrentProfile.HighlightTileAtRangeSpell, startX, startY));
            startY += _highlightTileRangeSpell.Height + 2;

            section2.Add(AddLabel(null, "@ range: ", startX, startY));

            section2.AddRight(_highlightTileRangeRangeSpell = AddHSlider(null, 1, 20, ProfileManager.CurrentProfile.HighlightTileAtRangeRangeSpell, startX, startY, 200));
            startY += _highlightTileRangeRangeSpell.Height + 2;

            section2.Add(_highlightTileRangeColorPickerBoxSpell = AddColorBox(null, startX, startY, ProfileManager.CurrentProfile.HighlightTileRangeHueSpell, ""));
            startY += _highlightTileRangeColorPickerBoxSpell.Height + 2;
            section2.AddRight(AddLabel(null, "Tile color", 0, 0), 2);

            section2.Add(_previewFields = AddCheckBox(null, "Preview fields", ProfileManager.CurrentProfile.PreviewFields, startX, startY));
            startY += _previewFields.Height + 2;

            section2.Add(_ownAuraByHP = AddCheckBox(null, "Color own aura by HP (needs aura enabled)", ProfileManager.CurrentProfile.OwnAuraByHP, startX, startY));
            startY += _ownAuraByHP.Height + 2;

            section2.Add(AddLabel(null, "Glowing Weapons:", startX, startY));

            mode = ProfileManager.CurrentProfile.GlowingWeaponsType;
            section2.Add(_glowingWeaponsType = AddCombobox(null, new[] { "Off", "White", "Pink", "Ice", "Fire", "Custom" }, mode, startX, startY, 100));
            startY += _glowingWeaponsType.Height + 2;

            section2.Add(_highlightGlowingWeaponsTypeColorPickerBoxHue = AddColorBox(null, startX, startY, ProfileManager.CurrentProfile.HighlightGlowingWeaponsTypeHue, ""));
            startY += _highlightGlowingWeaponsTypeColorPickerBoxHue.Height + 2;
            section2.AddRight(AddLabel(null, "Custom color", 0, 0), 2);

            section2.Add(AddLabel(null, "Highlight lasttarget:", startX, startY));

            mode = ProfileManager.CurrentProfile.HighlightLastTargetType;
            section2.Add(_highlightLastTargetType = AddCombobox(null, new[] { "Off", "White", "Pink", "Ice", "Fire", "Custom" }, mode, startX, startY, 100));
            startY += _highlightLastTargetType.Height + 2;

            section2.Add(_highlightLastTargetTypeColorPickerBox = AddColorBox(null, startX, startY, ProfileManager.CurrentProfile.HighlightLastTargetTypeHue, ""));
            startY += _highlightLastTargetTypeColorPickerBox.Height + 2;
            section2.AddRight(AddLabel(null, "Custom color lasttarget", 0, 0), 2);

            section2.Add(AddLabel(null, "Highlight lasttarget poisoned:", startX, startY));

            mode = ProfileManager.CurrentProfile.HighlightLastTargetTypePoison;
            section2.Add(_highlightLastTargetTypePoison = AddCombobox(null, new[] { "Off", "White", "Pink", "Ice", "Fire", "Special", "Custom" }, mode, startX, startY, 100));
            startY += _highlightLastTargetTypePoison.Height + 2;

            section2.Add(_highlightLastTargetTypeColorPickerBoxPoison = AddColorBox(null, startX, startY, ProfileManager.CurrentProfile.HighlightLastTargetTypePoisonHue, ""));
            startY += _highlightLastTargetTypeColorPickerBoxPoison.Height + 2;
            section2.AddRight(AddLabel(null, "Custom color poison", 0, 0), 2);

            section2.Add(AddLabel(null, "Highlight lasttarget paralyzed:", startX, startY));

            mode = ProfileManager.CurrentProfile.HighlightLastTargetTypePara;
            section2.Add(_highlightLastTargetTypePara = AddCombobox(null, new[] { "Off", "White", "Pink", "Ice", "Fire", "Special", "Custom" }, mode, startX, startY, 100));
            startY += _highlightLastTargetTypePara.Height + 2;

            section2.Add(_highlightLastTargetTypeColorPickerBoxPara = AddColorBox(null, startX, startY, ProfileManager.CurrentProfile.HighlightLastTargetTypeParaHue, ""));
            startY += _highlightLastTargetTypeColorPickerBoxPara.Height + 2;
            section2.AddRight(AddLabel(null, "Custom color paralyzed", 0, 0), 2);

            //VISUAL HELPERS END

            //HEALTHBAR START

            SettingsSection section3 = AddSettingsSection(box, "-----HEALTHBAR-----");
            section3.Y = section2.Bounds.Bottom + 40;

            startY = section2.Bounds.Bottom + 40;

            section3.Add(_highlightLastTargetHealthBarOutline = AddCheckBox(null, "Highlight LT healthbar", ProfileManager.CurrentProfile.HighlightLastTargetHealthBarOutline, startX, startY));
            startY += _highlightLastTargetHealthBarOutline.Height + 2;
            section3.Add(_highlightHealthBarByState = AddCheckBox(null, "Highlight healthbar border by state", ProfileManager.CurrentProfile.HighlightHealthBarByState, startX, startY));
            startY += _highlightHealthBarByState.Height + 2;
            section3.Add(_flashingHealthbarOutlineSelf = AddCheckBox(null, "Flashing healthbar outline - self", ProfileManager.CurrentProfile.FlashingHealthbarOutlineSelf, startX, startY));
            startY += _flashingHealthbarOutlineSelf.Height + 2;
            section3.Add(_flashingHealthbarOutlineParty = AddCheckBox(null, "Flashing healthbar outline - party", ProfileManager.CurrentProfile.FlashingHealthbarOutlineParty, startX, startY));
            startY += _flashingHealthbarOutlineParty.Height + 2;
            section3.Add(_flashingHealthbarOutlineGreen = AddCheckBox(null, "Flashing healthbar outline - ally", ProfileManager.CurrentProfile.FlashingHealthbarOutlineGreen, startX, startY));
            startY += _flashingHealthbarOutlineGreen.Height + 2;
            section3.Add(_flashingHealthbarOutlineOrange = AddCheckBox(null, "Flashing healthbar outline - enemy", ProfileManager.CurrentProfile.FlashingHealthbarOutlineOrange, startX, startY));
            startY += _flashingHealthbarOutlineOrange.Height + 2;
            section3.Add(_flashingHealthbarOutlineAll = AddCheckBox(null, "Flashing healthbar outline - all", ProfileManager.CurrentProfile.FlashingHealthbarOutlineAll, startX, startY));
            startY += _flashingHealthbarOutlineAll.Height + 2;
            section3.Add(_flashingHealthbarNegativeOnly = AddCheckBox(null, "Flashing healthbar outline on negative changes only", ProfileManager.CurrentProfile.FlashingHealthbarNegativeOnly, startX, startY));
            startY += _flashingHealthbarNegativeOnly.Height + 2;

            section3.Add(AddLabel(null, "only flash on HP change >= : ", startX, startY));

            section3.AddRight(_flashingHealthbarTreshold = AddHSlider(null, 1, 50, ProfileManager.CurrentProfile.FlashingHealthbarTreshold, startX, startY, 200));
            startY += _flashingHealthbarTreshold.Height + 2;

            //HEALTHBAR END

            //CURSOR START

            SettingsSection section4 = AddSettingsSection(box, "-----CURSOR-----");
            section4.Y = section3.Bounds.Bottom + 40;

            startY = section3.Bounds.Bottom + 40;

            section4.Add(_spellOnCursor = AddCheckBox(null, "Show spells on cursor", ProfileManager.CurrentProfile.SpellOnCursor, startX, startY));
            startY += _spellOnCursor.Height + 2;

            section4.Add(AddLabel(null, "Spellicon offset: ", startX, startY));
            
            section4.Add
            (
                _spellOnCursorOffsetX = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _spellOnCursorOffsetX.SetText(ProfileManager.CurrentProfile.SpellOnCursorOffset.X.ToString());
            section4.AddRight(AddLabel(null, "X", 0, 0), 2);
            startY += _spellOnCursorOffsetX.Height + 2;
            
            section4.Add
            (
                _spellOnCursorOffsetY = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _spellOnCursorOffsetY.SetText(ProfileManager.CurrentProfile.SpellOnCursorOffset.Y.ToString());
            section4.AddRight(AddLabel(null, "Y", 0, 0), 2);
            startY += _spellOnCursorOffsetY.Height + 2;
            
            //VISUAL HELPERS END

            //OVERHEAD / UNDERCHAR START
            SettingsSection section5 = AddSettingsSection(box, "-----OVERHEAD / UNDERFEET-----");
            section5.Y = section4.Bounds.Bottom + 40;
            startY = section4.Bounds.Bottom + 40;

            section5.Add(_overheadRange = AddCheckBox(null, "Display range in overhead (needs HP overhead enabled)", ProfileManager.CurrentProfile.OverheadRange, startX, startY));
            startY += _overheadRange.Height + 2;
            section5.Add(_overheadSummonTime = AddCheckBox(null, "Overhead summon time (needs HP overhead enabled) or in healthbar", ProfileManager.CurrentProfile.OverheadSummonTime, startX, startY));
            startY += _overheadSummonTime.Height + 2;
            section5.Add(_overheadPeaceTime = AddCheckBox(null, "Overhead peacemaking time (needs HP overhead enabled) or in healthbar", ProfileManager.CurrentProfile.OverheadPeaceTime, startX, startY));
            startY += _overheadPeaceTime.Height + 2;
            section5.Add(_mobileHamstrungTime = AddCheckBox(null, "Show hamstrung time on mobile (needs HP lines or HP overhead ernabled) or in healthbar", true, startX, startY)); //has no effect but feature list
            startY += _mobileHamstrungTime.Height + 2;

            section5.Add(AddLabel(null, "Cooldown (ms): ", startX, startY));

            section5.Add
            (
                _mobileHamstrungTimeCooldown = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _mobileHamstrungTimeCooldown.SetText(ProfileManager.CurrentProfile.MobileHamstrungTimeCooldown.ToString());

            //OVERHEAD / UNDERCHAR START NED

            //HEALTHBARS START

            SettingsSection section6 = AddSettingsSection(box, "-----HEALTHBARS-----");
            section6.Y = section5.Bounds.Bottom + 40;

            startY = section5.Bounds.Bottom + 40;

            section6.Add(_useOldHealthBars = AddCheckBox(null, "Use old healthbars", ProfileManager.CurrentProfile.UseOldHealthBars, startX, startY));
            startY += _useOldHealthBars.Height + 2;
            section6.Add(_multipleUnderlinesSelfParty = AddCheckBox(null, "Display Mana / Stam in underline for self and party (requires old healthbars)", ProfileManager.CurrentProfile.MultipleUnderlinesSelfParty, startX, startY));
            startY += _multipleUnderlinesSelfParty.Height + 2;
            section6.Add(_multipleUnderlinesSelfPartyBigBars = AddCheckBox(null, "Use bigger underlines for self and party (requires old healthbars)", ProfileManager.CurrentProfile.MultipleUnderlinesSelfPartyBigBars, startX, startY));
            startY += _multipleUnderlinesSelfPartyBigBars.Height + 2;

            section6.Add(AddLabel(null, "Transparency for self and party (close client completly), ", startX, startY));
            section6.Add(AddLabel(null, "(requires old healthbars): ", startX, startY));

            section6.Add(_multipleUnderlinesSelfPartyTransparency = AddHSlider(null, 1, 10, ProfileManager.CurrentProfile.MultipleUnderlinesSelfPartyTransparency, startX, startY, 200));
            startY += _multipleUnderlinesSelfPartyTransparency.Height + 2;

            //HEALTHBARS START END

            // MISC START
            SettingsSection section7 = AddSettingsSection(box, "-----MISC-----");
            section7.Y = section6.Bounds.Bottom + 40;

            startY = section6.Bounds.Bottom + 40;

            section7.Add(_infernoBridge = AddCheckBox(null, "Solve Inferno bridge (needs relog)", ProfileManager.CurrentProfile.InfernoBridge, startX, startY));
            startY += _infernoBridge.Height + 2;
            section7.Add(_offscreenTargeting = AddCheckBox(null, "Offscreen targeting (always on)", true, startX, startY)); //has no effect but feature list
            startY += _offscreenTargeting.Height + 2;
            section7.Add(_SpecialSetLastTargetCliloc = AddCheckBox(null, "Razor * Target * to lasttarget string", ProfileManager.CurrentProfile.SpecialSetLastTargetCliloc, startX, startY));
            startY += _SpecialSetLastTargetCliloc.Height + 2;

            section7.Add
            (
                _SpecialSetLastTargetClilocText = AddInputField
                (
                    null,
                    startX, startY,
                    150,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    false,
                    15
                )
            );
            _SpecialSetLastTargetClilocText.SetText(ProfileManager.CurrentProfile.SpecialSetLastTargetClilocText.ToString());

            section7.Add(_blackOutlineStatics = AddCheckBox(null, "Outline statics black", ProfileManager.CurrentProfile.BlackOutlineStatics, startX, startY));
            startY += _blackOutlineStatics.Height + 2;

            section7.Add(_ignoreStaminaCheck = AddCheckBox(null, "Ignore stamina check", ProfileManager.CurrentProfile.IgnoreStaminaCheck, startX, startY));
            startY += _ignoreStaminaCheck.Height + 2;

            // MISC END
            //MACRO START
            SettingsSection section8 = AddSettingsSection(box, "-----MACRO-----");
            section8.Y = section7.Bounds.Bottom + 40;

            startY = section7.Bounds.Bottom + 40;

            section8.Add(AddLabel(null, "Macro - LastTargetRC - Range:", startX, startY));

            section8.AddRight(_lastTargetRange = AddHSlider(null, 1, 30, ProfileManager.CurrentProfile.LastTargetRange, startX, startY, 200));
            startY += _lastTargetRange.Height + 2;
            //MACRO END

            Add(rightArea, PAGE);
        }
        //##UCC##//
        private void BuildUCC()
        {
            const int PAGE = 14;
            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            int startX = 5;
            int startY = 5;

            DataBox box = new DataBox(startX, startY, rightArea.Width - 15, 1);
            box.WantUpdateSize = true;
            rightArea.Add(box);

            //UI ON OFF TOGGLES
            SettingsSection section = AddSettingsSection(box, "-----AL (AL UI)-----");

            section.Add(_uccEnableAL = AddCheckBox(null, "Enable UCC - AL", ProfileManager.CurrentProfile.UOClassicCombatAL, startX, startY));
            startY += _uccEnableAL.Height + 2;
            section.Add(_uccEnableGridLootColoring = AddCheckBox(null, "Enable GridLootColoring", ProfileManager.CurrentProfile.UOClassicCombatAL_EnableGridLootColoring, startX, startY));
            startY += _uccEnableGridLootColoring.Height + 2;
            section.Add(_uccBEnableLootAboveID = AddCheckBox(null, "Enable LootAboveID", ProfileManager.CurrentProfile.UOClassicCombatAL_EnableLootAboveID, startX, startY));
            startY += _uccBEnableLootAboveID.Height + 2;

            SettingsSection section2 = AddSettingsSection(box, "-----LINES (LINES UI)-----");
            section2.Y = section.Bounds.Bottom + 40;

            startY = section.Bounds.Bottom + 40;

            section2.Add(_uccEnableLines = AddCheckBox(null, "Enable UCC - Lines", ProfileManager.CurrentProfile.UOClassicCombatLines, startX, startY));
            startY += _uccEnableLines.Height + 2;

            SettingsSection section3 = AddSettingsSection(box, "-----BUFFBAR (COOLDOWN UI)-----");
            section3.Y = section2.Bounds.Bottom + 40;

            startY = section2.Bounds.Bottom + 40;

            section3.Add(_uccEnableBuffbar = AddCheckBox(null, "Enable UCC - Buffbar", ProfileManager.CurrentProfile.UOClassicCombatBuffbar, startX, startY));
            startY += _uccEnableBuffbar.Height + 2;

            section3.Add(AddLabel(null, "-----DISABLE / ENABLE BUFFBAR ON CHANGES BELOW-----", startX, startY));

            section3.Add(_uccSwing = AddCheckBox(null, "Show Swing Line", ProfileManager.CurrentProfile.UOClassicCombatBuffbar_SwingEnabled, startX, startY));
            startY += _uccSwing.Height + 2;
            section3.Add(_uccDoD = AddCheckBox(null, "Show Do Disarm Line", ProfileManager.CurrentProfile.UOClassicCombatBuffbar_DoDEnabled, startX, startY));
            startY += _uccDoD.Height + 2;
            section3.Add(_uccGotD = AddCheckBox(null, "Show Got Disarmed Line", ProfileManager.CurrentProfile.UOClassicCombatBuffbar_GotDEnabled, startX, startY));
            startY += _uccGotD.Height + 2;
            section3.Add(_uccDoH = AddCheckBox(null, "Show Do Hamstring Line (Outlands)", ProfileManager.CurrentProfile.UOClassicCombatBuffbar_DoHEnabled, startX, startY));
            startY += _uccDoH.Height + 2;
            section3.Add(_uccGotH = AddCheckBox(null, "Show Got Hamstung Line (Outlands)", ProfileManager.CurrentProfile.UOClassicCombatBuffbar_GotHEnabled, startX, startY));
            startY += _uccGotH.Height + 2;
            section3.Add(_uccLocked = AddCheckBox(null, "Lock in place", ProfileManager.CurrentProfile.UOClassicCombatBuffbar_Locked, startX, startY));
            startY += _uccLocked.Height + 2;

            SettingsSection section4 = AddSettingsSection(box, "-----SELF (AUTOMATIONS UI)-----");
            section4.Y = section3.Bounds.Bottom + 40;

            startY = section3.Bounds.Bottom + 40;

            section4.Add(_uccEnableSelf = AddCheckBox(null, "Enable UCC - Self", ProfileManager.CurrentProfile.UOClassicCombatSelf, startX, startY));
            startY += _uccEnableSelf.Height + 2;

            //UI ON OFF TOGGLES END
            //SETTINGS

            //SETTING - AL
            SettingsSection section5 = AddSettingsSection(box, "-----SETTINGS (AL)-----");
            section5.Y = section4.Bounds.Bottom + 40;

            startY = section4.Bounds.Bottom + 40;

            section5.Add(AddLabel(null, "-----DISABLE / ENABLE AL ON CHANGES BELOW-----", startX, startY));

            section5.Add(AddLabel(null, "Time between looting two items(ms)", startX, startY));

            section5.Add
            (
                _uccLootDelay = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _uccLootDelay.SetText(ProfileManager.CurrentProfile.UOClassicCombatAL_LootDelay.ToString());

            section5.Add(AddLabel(null, "Time to purge the queue of old items (ms)", startX, startY));

            section5.Add
            (
                _uccPurgeDelay = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _uccPurgeDelay.SetText(ProfileManager.CurrentProfile.UOClassicCombatAL_PurgeDelay.ToString());

            section5.Add(AddLabel(null, "Time between processing the queue (ms)", startX, startY));

            section5.Add
            (
                _uccQueueSpeed = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _uccQueueSpeed.SetText(ProfileManager.CurrentProfile.UOClassicCombatAL_QueueSpeed.ToString());

            section5.Add(AddLabel(null, "Loot above ID", startX, startY));

            section5.Add
            (
                _uccLootAboveID = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _uccLootAboveID.SetText(ProfileManager.CurrentProfile.UOClassicCombatAL_LootAboveID.ToString());

            section5.Add(AddLabel(null, "Gray corpse color", startX, startY));

            section5.Add
            (
                _uccSL_Gray = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _uccSL_Gray.SetText(ProfileManager.CurrentProfile.UOClassicCombatAL_SL_Gray.ToString());

            section5.Add(AddLabel(null, "Blue corpse color", startX, startY));

            section5.Add
            (
                _uccSL_Blue = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _uccSL_Blue.SetText(ProfileManager.CurrentProfile.UOClassicCombatAL_SL_Blue.ToString());

            section5.Add(AddLabel(null, "Green corpse color", startX, startY));

            section5.Add
            (
                _uccSL_Green = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _uccSL_Green.SetText(ProfileManager.CurrentProfile.UOClassicCombatAL_SL_Green.ToString());

            section5.Add(AddLabel(null, "Red corpse color", startX, startY));

            section5.Add
            (
                _uccSL_Red = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _uccSL_Red.SetText(ProfileManager.CurrentProfile.UOClassicCombatAL_SL_Red.ToString());

            //SETTING - BUFFBAR AND SELF
            SettingsSection section6 = AddSettingsSection(box, "-----SETTINGS (BUFFBAR AND SELF)-----");
            section6.Y = section5.Bounds.Bottom + 40;

            startY = section5.Bounds.Bottom + 40;

            section6.Add(AddLabel(null, "General cooldown when you get disarmed (ms)", startX, startY));

            section6.Add
            (
                _uccDisarmedCooldown = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _uccDisarmedCooldown.SetText(ProfileManager.CurrentProfile.UOClassicCombatSelf_DisarmedCooldown.ToString());

            section6.Add(AddLabel(null, "General cooldown when you get hamstrung (ms)", startX, startY));

            section6.Add
            (
                _uccHamstrungCooldown = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _uccHamstrungCooldown.SetText(ProfileManager.CurrentProfile.UOClassicCombatSelf_HamstrungCooldown.ToString());

            section6.Add(AddLabel(null, "Cooldown after successfull disarm (ms)", startX, startY));

            section6.Add
            (
                _uccDisarmStrikeCooldown = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _uccDisarmStrikeCooldown.SetText(ProfileManager.CurrentProfile.UOClassicCombatSelf_DisarmStrikeCooldown.ToString());

            section6.Add(AddLabel(null, "Cooldown after failed disarm (ms)", startX, startY));

            section6.Add
            (
                _uccDisarmAttemptCooldown = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _uccDisarmAttemptCooldown.SetText(ProfileManager.CurrentProfile.UOClassicCombatSelf_DisarmAttemptCooldown.ToString());

            section6.Add(AddLabel(null, "Cooldown after successfull hamstring (ms)", startX, startY));

            section6.Add
            (
                _uccHamstringStrikeCooldown = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _uccHamstringStrikeCooldown.SetText(ProfileManager.CurrentProfile.UOClassicCombatSelf_HamstringStrikeCooldown.ToString());

            section6.Add(AddLabel(null, "Cooldown after failed hamstring (ms)", startX, startY));

            section6.Add
            (
                _uccHamstringAttemptCooldown = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _uccHamstringAttemptCooldown.SetText(ProfileManager.CurrentProfile.UOClassicCombatSelf_HamstringAttemptCooldown.ToString());

            //SETTING FOR SELF ONLY
            SettingsSection section7 = AddSettingsSection(box, "-----SETTINGS (SELF)-----");
            section7.Y = section6.Bounds.Bottom + 40;

            startY = section6.Bounds.Bottom + 40;

            //COOLDOWN SETTINGS
            section7.Add(AddLabel(null, "-----SETTINGS (COOLDOWNS)-----", startX, startY));

            section7.Add(AddLabel(null, "ActionCooldown (ms): ", startX, startY));

            section7.Add
            (
                _uccActionCooldown = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _uccActionCooldown.SetText(ProfileManager.CurrentProfile.UOClassicCombatSelf_ActionCooldown.ToString());

            section7.Add(AddLabel(null, "Repeated Pouche Cooldown (ms): ", startX, startY));

            section7.Add
            (
                _uccPoucheCooldown = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _uccPoucheCooldown.SetText(ProfileManager.CurrentProfile.UOClassicCombatSelf_PoucheCooldown.ToString());

            section7.Add(AddLabel(null, "Repeated Curepot Cooldown (ms): ", startX, startY));

            section7.Add
            (
                _uccCurepotCooldown = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _uccCurepotCooldown.SetText(ProfileManager.CurrentProfile.UOClassicCombatSelf_CurepotCooldown.ToString());

            section7.Add(AddLabel(null, "Repeated Healpot Cooldown (ms): ", startX, startY));

            section7.Add
            (
                _uccHealpotCooldown = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _uccHealpotCooldown.SetText(ProfileManager.CurrentProfile.UOClassicCombatSelf_HealpotCooldown.ToString());

            section7.Add(AddLabel(null, "Repeated Refreshpot Cooldown (ms): ", startX, startY));

            section7.Add
            (
                _uccRefreshpotCooldown = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _uccRefreshpotCooldown.SetText(ProfileManager.CurrentProfile.UOClassicCombatSelf_RefreshpotCooldown.ToString());

            section7.Add(AddLabel(null, "WaitForTarget (oldBandies) (ms): ", startX, startY));

            section7.Add
            (
                _uccWaitForTarget = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _uccWaitForTarget.SetText(ProfileManager.CurrentProfile.UOClassicCombatSelf_WaitForTarget.ToString());

            //TRESHOLD SETTINGS
            SettingsSection section8 = AddSettingsSection(box, "-----SETTINGS (TRESHOLDS)-----");
            section8.Y = section7.Bounds.Bottom + 40;

            startY = section7.Bounds.Bottom + 40;

            section8.Add(AddLabel(null, "Bandies treshold (diffhits >= ):", startX, startY));

            section8.Add
            (
                _uccBandiesHPTreshold = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _uccBandiesHPTreshold.SetText(ProfileManager.CurrentProfile.UOClassicCombatSelf_BandiesHPTreshold.ToString());

            section8.Add(_uccBandiesPoison = AddCheckBox(null, "Bandies when poisoned", ProfileManager.CurrentProfile.UOClassicCombatSelf_BandiesPoison, startX, startY));
            startY += _uccBandiesPoison.Height + 2;

            section8.Add(AddLabel(null, "Curepot HP treshold (diffhits >= ): ", startX, startY));

            section8.Add
            (
                _uccCurepotHPTreshold = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _uccCurepotHPTreshold.SetText(ProfileManager.CurrentProfile.UOClassicCombatSelf_CurepotHPTreshold.ToString());

            section8.Add(AddLabel(null, " HP treshold (diffhits >= ): ", startX, startY));

            section8.Add
            (
                _uccHealpotHPTreshold = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _uccHealpotHPTreshold.SetText(ProfileManager.CurrentProfile.UOClassicCombatSelf_HealpotHPTreshold.ToString());

            section8.Add(AddLabel(null, "Refreshpot Stam treshold (diffstam >= ): ", startX, startY));

            section8.Add
            (
                _uccRefreshpotStamTreshold = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _uccRefreshpotStamTreshold.SetText(ProfileManager.CurrentProfile.UOClassicCombatSelf_RefreshpotStamTreshold.ToString());

            //MISC SETTINGS AREA
            SettingsSection section9 = AddSettingsSection(box, "-----SETTINGS (MISC)-----");
            section9.Y = section8.Bounds.Bottom + 40;

            startY = section8.Bounds.Bottom + 40;

            section9.Add(AddLabel(null, "Auto rearm weps held before got disarmeded (ms)", startX, startY));

            section9.Add
            (
                _uccAutoRearmAfterDisarmedCooldown = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _uccAutoRearmAfterDisarmedCooldown.SetText(ProfileManager.CurrentProfile.UOClassicCombatSelf_AutoRearmAfterDisarmedCooldown.ToString());

            section9.Add(_uccNoRefreshPotAfterHamstrung = AddCheckBox(null, "Drink no refresh pot after being hamstrung", ProfileManager.CurrentProfile.UOClassicCombatSelf_NoRefreshPotAfterHamstrung, startX, startY));
            startY += _uccNoRefreshPotAfterHamstrung.Height + 2;

            section9.Add(AddLabel(null, "Cooldown (ms)", startX, startY));

            section9.Add
            (
                _uccNoRefreshPotAfterHamstrungCooldown = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _uccNoRefreshPotAfterHamstrungCooldown.SetText(ProfileManager.CurrentProfile.UOClassicCombatSelf_NoRefreshPotAfterHamstrungCooldown.ToString());

            section9.Add(_uccClilocTrigger = AddCheckBox(null, "Use Cliloc Triggers (up time on cliloc and uoc hotkey)", ProfileManager.CurrentProfile.UOClassicCombatSelf_ClilocTriggers, startX, startY));
            startY += _uccClilocTrigger.Height + 2;

            section9.Add(_uccMacroTrigger = AddCheckBox(null, "Use Macro Triggers (change uoc hotkey to disarm / pot / rearm through ucc)", ProfileManager.CurrentProfile.UOClassicCombatSelf_MacroTriggers, startX, startY));
            startY += _uccMacroTrigger.Height + 2;

            section9.Add(AddLabel(null, "Strength Pot Cooldown (ms)", startX, startY));
            section9.Add
            (
                _uccStrengthPotCooldown = AddInputField
                (
                    null,
                    startX, startY,
                    70,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    7
                )
            );
            _uccStrengthPotCooldown.SetText(ProfileManager.CurrentProfile.UOClassicCombatSelf_StrengthPotCooldown.ToString());

            section9.Add(AddLabel(null, "Agility Pot Cooldown (ms)", startX, startY));
            section9.Add
            (
                _uccDexPotCooldown = AddInputField
                (
                    null,
                    startX, startY,
                    70,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    7
                )
            );
            _uccDexPotCooldown.SetText(ProfileManager.CurrentProfile.UOClassicCombatSelf_DexPotCooldown.ToString());

            section9.Add(AddLabel(null, "Min RNG (ms)", startX, startY));

            section9.Add
            (
                _uccRNGMin = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _uccRNGMin.SetText(ProfileManager.CurrentProfile.UOClassicCombatSelf_MinRNG.ToString());

            section9.Add(AddLabel(null, "Max RNG (ms)", startX, startY));

            section9.Add
            (
                _uccRNGMax = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _uccRNGMax.SetText(ProfileManager.CurrentProfile.UOClassicCombatSelf_MaxRNG.ToString());

            Add(rightArea, PAGE);
        }
        //##UCC##//

        private void BuildAgents()
        {
            const int PAGE = 15;

            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            int startX = 5;
            int startY = 5;

            DataBox box = new DataBox(startX, startY, rightArea.Width - 15, 1);
            box.WantUpdateSize = true;
            rightArea.Add(box);

            SettingsSection section = AddSettingsSection(box, "-----UI / Gumps-----");

            section.Add(_bandageGump = AddCheckBox(null, "Show gump when using bandages", ProfileManager.CurrentProfile.BandageGump, startX, startY));
            startY += _highlightContainersWhenMouseIsOver.Height + 2;

            section.Add(AddLabel(null, "Bandage Timer Offset: ", startX, startY));
            
            section.Add
            (
                _bandageGumpOffsetX = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _bandageGumpOffsetX.SetText(ProfileManager.CurrentProfile.BandageGumpOffset.X.ToString());
            startY += _bandageGumpOffsetX.Height + 2;
            section.AddRight(AddLabel(null, "X", 0, 0), 2);
            
            section.Add
            (
                _bandageGumpOffsetY = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _bandageGumpOffsetY.SetText(ProfileManager.CurrentProfile.BandageGumpOffset.Y.ToString());
            
            startY += _bandageGumpOffsetY.Height + 2;
            section.AddRight(AddLabel(null, "Y", 0, 0), 2);
            section.Add(AddLabel(null, "Macro: OpenJournal2 (opens a second journal)", startX, startY));


            SettingsSection section2 = AddSettingsSection(box, "-----Texture Manager-----");
            section2.Y = section.Bounds.Bottom + 40;

            startY = section.Bounds.Bottom + 40;

            section2.Add(_textureManagerEnabled = AddCheckBox(null, "Enable TextureManager", ProfileManager.CurrentProfile.TextureManagerEnabled, startX, startY));
            startY += _textureManagerEnabled.Height + 2;
            section2.Add(_textureManagerHalosEnabled = AddCheckBox(null, "Enable TextureManager Halos", ProfileManager.CurrentProfile.TextureManagerHalos, startX, startY));
            startY += _textureManagerHalosEnabled.Height + 2;
            section2.Add(_textureManagerArrowsEnabled = AddCheckBox(null, "Enable TextureManager Arrows", ProfileManager.CurrentProfile.TextureManagerArrows, startX, startY));
            startY += _textureManagerArrowsEnabled.Height + 2;

            section2.Add(_wireframeView = AddCheckBox(null, "Enable WireFrame view", ProfileManager.CurrentProfile.WireFrameView, startX, startY));
            startY += _wireframeView.Height + 2;

            section2.Add(_hueImpassableView = AddCheckBox(null, "Hue impassable Tiles", ProfileManager.CurrentProfile.HueImpassableView, startX, startY));
            startY += _hueImpassableView.Height + 2;

            section2.Add(_hueImpassableViewColorPickerBox = AddColorBox(null, startX, startY, ProfileManager.CurrentProfile.HueImpassableViewHue, ""));
            startY += _hueImpassableViewColorPickerBox.Height + 2;
            section2.AddRight(AddLabel(null, "Hue", 0, 0), 2);

            SettingsSection section3 = AddSettingsSection(box, "-----MISC-----");
            section3.Y = section2.Bounds.Bottom + 40;

            startY = section2.Bounds.Bottom + 40;

            section3.Add(AddLabel(null, "write in chat to enable / disable:", startX, startY));
            section3.Add(AddLabel(null, "-mimic (mimic harmful spells 1:1, on beneficial macro defendSelf/defendParty)", startX, startY));
            section3.Add(AddLabel(null, "Macro: SetMimic_PlayerSerial (define the player to mimic)", startX, startY));
            section3.Add(AddLabel(null, "-marker X Y (place a dot and line to X Y on world map \n use -marker to remove it)", startX, startY));
            section3.Add(_autoWorldmapMarker = AddCheckBox(null, "Auto add marker for MapGumps (ie. T-Maps)", ProfileManager.CurrentProfile.AutoWorldmapMarker, startX, startY));
            startY += _autoWorldmapMarker.Height + 2;
            section3.Add(AddLabel(null, "-df (if GreaterHeal cursor is up and you or a party member \n " +
                                                "gets hit by EB, Explor or FS \n " +
                                                "and your or the party members condition is met \n " +
                                                "greater heal will be cast on you or party member \n " +
                                                "Condition: Poisoned and HP smaller than random between 65 - 80 \n" +
                                                "Condition: HP smaller than random between 40-70)", startX, startY));

            SettingsSection section4 = AddSettingsSection(box, "-----RESERVED-----");
            section4.Y = section3.Bounds.Bottom + 40;
            startY = section3.Bounds.Bottom + 40;

            SettingsSection section5 = AddSettingsSection(box, "-----Automations-----");
            section5.Y = section4.Bounds.Bottom + 40;
            startY = section4.Bounds.Bottom + 40;

            section5.Add(AddLabel(null, "write in chat or use macro to enable / disable:", startX, startY));
            section5.Add(AddLabel(null, "(runs in background until disabled)", startX, startY));
            section5.Add(AddLabel(null, "-automed or macro AutoMeditate (auto meditates \n with 2.5s delay and not \n targeting)", startX, startY));
            section5.Add(AddLabel(null, "-engange (auto engage and pathfind to last target)", startX, startY));

            SettingsSection section6 = AddSettingsSection(box, "-----Advanced macros-----");
            section6.Y = section5.Bounds.Bottom + 40;
            startY = section5.Bounds.Bottom + 40;
            section6.Add(AddLabel(null, "use macro to run advanced scripts ONCE:", startX, startY));
            section6.Add(AddLabel(null, "AutoPot (disarm 2h layer -> \n healpot below 85%, \n pouch if paralyzed, \n cure if poisoned and not full hp, \n refresh if below 23, \n str pot if str below 100, \n agi pot if dex above 89)", startX, startY));
            section6.Add(AddLabel(null, "DefendPartyKey (if ally / party member in 12 tile range and hits < 64: \n if targeting -> target them, else cast gheal \n if own hits < 64: \n if targeting -> target self and use gheal pot, \n else start casting gheal)", startX, startY));
            section6.Add(AddLabel(null, "DefendSelfKey (if own hits < 64, \n if targeting -> target self and use gheal pot, \n else start casting gheal)", startX, startY));
            section6.Add(AddLabel(null, "Interrupt (fast macro to interrupt active spellcasting)", startX, startY));
            section6.Add(AddLabel(null, "ObjectInfo (macro for -info command)", startX, startY));

            section6.Add(AddLabel(null, "GrabFriendlyBars (grab all innocent bars)", startX, startY));
            section6.Add
            (
                _pullFriendlyBarsX = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _pullFriendlyBarsX.SetText(ProfileManager.CurrentProfile.PullFriendlyBars.X.ToString());
            section6.AddRight(AddLabel(null, "X", 0, 0), 2);
            startY += _pullFriendlyBarsX.Height + 2;

            section6.Add
            (
                _pullFriendlyBarsY = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _pullFriendlyBarsY.SetText(ProfileManager.CurrentProfile.PullFriendlyBars.Y.ToString());
            section6.AddRight(AddLabel(null, "Y", 0, 0), 2);
            startY += _pullFriendlyBarsY.Height + 2;
            //
            section6.Add
            (
                _pullFriendlyBarsFinalLocationX = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _pullFriendlyBarsFinalLocationX.SetText(ProfileManager.CurrentProfile.PullFriendlyBarsFinalLocation.X.ToString());
            section6.AddRight(AddLabel(null, "X", 0, 0), 2);
            startY += _pullFriendlyBarsFinalLocationX.Height + 2;

            section6.Add
            (
                _pullFriendlyBarsFinalLocationY = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _pullFriendlyBarsFinalLocationY.SetText(ProfileManager.CurrentProfile.PullFriendlyBarsFinalLocation.Y.ToString());
            section6.AddRight(AddLabel(null, "Y", 0, 0), 2);
            startY += _pullFriendlyBarsFinalLocationY.Height + 2;

            section6.Add(AddLabel(null, "GrabEnemyBars (grab all criminal, enemy, gray, murderer bars)", startX, startY));
            section6.Add
            (
                _pullEnemyBarsX = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _pullEnemyBarsX.SetText(ProfileManager.CurrentProfile.PullEnemyBars.X.ToString());
            section6.AddRight(AddLabel(null, "X", 0, 0), 2);
            startY += _pullEnemyBarsX.Height + 2;

            section6.Add
            (
                _pullEnemyBarsY = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _pullEnemyBarsY.SetText(ProfileManager.CurrentProfile.PullEnemyBars.Y.ToString());
            section6.AddRight(AddLabel(null, "Y", 0, 0), 2);
            startY += _pullEnemyBarsY.Height + 2;
            //
            section6.Add
            (
                _pullEnemyBarsFinalLocationX = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _pullEnemyBarsFinalLocationX.SetText(ProfileManager.CurrentProfile.PullEnemyBarsFinalLocation.X.ToString());
            section6.AddRight(AddLabel(null, "FX", 0, 0), 2);
            startY += _pullEnemyBarsFinalLocationX.Height + 2;

            section6.Add
            (
                _pullEnemyBarsFinalLocationY = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _pullEnemyBarsFinalLocationY.SetText(ProfileManager.CurrentProfile.PullEnemyBarsFinalLocation.Y.ToString());
            section6.AddRight(AddLabel(null, "FY", 0, 0), 2);
            startY += _pullEnemyBarsFinalLocationY.Height + 2;

            section6.Add(AddLabel(null, "GrabPartyAllyBars (grab all ally and party bars)", startX, startY));
            section6.Add
            (
                _pullPartyAllyBarsX = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _pullPartyAllyBarsX.SetText(ProfileManager.CurrentProfile.PullPartyAllyBars.X.ToString());
            section6.AddRight(AddLabel(null, "X", 0, 0), 2);
            startY += _pullPartyAllyBarsX.Height + 2;

            section6.Add
            (
                _pullPartyAllyBarsY = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _pullPartyAllyBarsY.SetText(ProfileManager.CurrentProfile.PullPartyAllyBars.Y.ToString());
            section6.AddRight(AddLabel(null, "Y", 0, 0), 2);
            startY += _pullPartyAllyBarsY.Height + 2;
            //
            section6.Add
            (
                _pullPartyAllyBarsFinalLocationX = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _pullPartyAllyBarsFinalLocationX.SetText(ProfileManager.CurrentProfile.PullPartyAllyBarsFinalLocation.X.ToString());
            section6.AddRight(AddLabel(null, "FX", 0, 0), 2);
            startY += _pullPartyAllyBarsFinalLocationX.Height + 2;

            section6.Add
            (
                _pullPartyAllyBarsFinalLocationY = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5
                )
            );
            _pullPartyAllyBarsFinalLocationY.SetText(ProfileManager.CurrentProfile.PullPartyAllyBarsFinalLocation.Y.ToString());
            section6.AddRight(AddLabel(null, "FY", 0, 0), 2);
            startY += _pullPartyAllyBarsFinalLocationY.Height + 2;

            section6.Add(AddLabel(null, "OpenCorpses (opens 0x2006 corpses within 2 tiles)", startX, startY));
            section6.Add(AddLabel(null, "OpenCorpsesSafeLoot (opens non blue 0x2006 corpses within 2 tiles)", startX, startY));
            section6.Add(AddLabel(null, "EquipManager (equip an item)", startX, startY));
            section6.Add(AddLabel(null, "SetTargetClientSide (set target client side only)", startX, startY));

            SettingsSection section7 = AddSettingsSection(box, "-----Advanced macros-----");
            section7.Y = section6.Bounds.Bottom + 40;
            startY = section5.Bounds.Bottom + 40;
            section7.Add(AddLabel(null, "use macro to run advanced scripts ONCE:", startX, startY));
            section7.Add(AddLabel(null, "LastTargetRC (last target with custom range check (set in options))", startX, startY));
            section7.Add(AddLabel(null, "HideX (remove landtile, entity, mobile or item)", startX, startY));
            section7.Add(AddLabel(null, "HighlightTileAtRange (toggle HighlightTileAtRange on / off)", startX, startY));
            section7.Add(AddLabel(null, "HealOnHPChange (keep pressed, casts heal on own hp change)", startX, startY));
            section7.Add(AddLabel(null, "HarmOnSwing (keep pressed, casts harm on next own swing animation)", startX, startY));
            section7.Add(AddLabel(null, "UCCLinesToggleLT (toggle on / off UCC Lines LastTarget)", startX, startY));
            section7.Add(AddLabel(null, "UCCLinesToggleHM (toggle on / off UCC Lines Hunting Mode)", startX, startY));
            section7.Add(AddLabel(null, "CureGH (if poisoned cure, else greater heal)", startX, startY));

            Add(rightArea, PAGE);
        }
        // ## BEGIN - END ## //

        public override void OnButtonClick(int buttonID)
        {
            if (buttonID == (int) Buttons.Last + 1)
            {
                // it's the macro buttonssss
                return;
            }

            switch ((Buttons) buttonID)
            {
                case Buttons.Disabled: break;

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

                case Buttons.NewMacro: break;

                case Buttons.DeleteMacro: break;
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
                    // ## BEGIN - END ## //  ORIG
                    //_treeToStumps.IsChecked = false;
                    // ## BEGIN - END ## //
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
                    _use_old_status_gump.IsChecked = false;
                    _auraType.SelectedIndex = 0;
                    _fieldsType.SelectedIndex = 0;

                    _showSkillsMessage.IsChecked = true;
                    _showSkillsMessageDelta.Value = 1;
                    _showStatsMessage.IsChecked = true;

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
                    _gameWindowWidth.SetText("600");
                    _gameWindowHeight.SetText("480");
                    _gameWindowPositionX.SetText("20");
                    _gameWindowPositionY.SetText("20");
                    _gameWindowLock.IsChecked = false;
                    _gameWindowFullsize.IsChecked = false;
                    _enableDeathScreen.IsChecked = true;
                    _enableBlackWhiteEffect.IsChecked = true;
                    Client.Game.Scene.Camera.Zoom = 1f;
                    _currentProfile.DefaultScale = 1f;
                    _lightBar.Value = 0;
                    _enableLight.IsChecked = false;
                    _useColoredLights.IsChecked = false;
                    _darkNights.IsChecked = false;
                    _enableShadows.IsChecked = true;
                    _enableShadowsStatics.IsChecked = true;
                    _runMouseInSeparateThread.IsChecked = true;
                    _auraMouse.IsChecked = true;
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
                    _hideChatGradient.IsChecked = false;

                    break;

                case 8: // combat
                    _innocentColorPickerBox.SetColor(0x005A, HuesLoader.Instance.GetPolygoneColor(12, 0x005A));
                    _friendColorPickerBox.SetColor(0x0044, HuesLoader.Instance.GetPolygoneColor(12, 0x0044));
                    _crimialColorPickerBox.SetColor(0x03b2, HuesLoader.Instance.GetPolygoneColor(12, 0x03b2));
                    _canAttackColorPickerBox.SetColor(0x03b2, HuesLoader.Instance.GetPolygoneColor(12, 0x03b2));
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

            _currentProfile.HighlightGameObjects = _highlightObjects.IsChecked;
            _currentProfile.ReduceFPSWhenInactive = _reduceFPSWhenInactive.IsChecked;
            _currentProfile.EnablePathfind = _enablePathfind.IsChecked;
            _currentProfile.UseShiftToPathfind = _useShiftPathfind.IsChecked;
            _currentProfile.AlwaysRun = _alwaysRun.IsChecked;
            _currentProfile.AlwaysRunUnlessHidden = _alwaysRunUnlessHidden.IsChecked;
            _currentProfile.ShowMobilesHP = _showHpMobile.IsChecked;
            _currentProfile.HighlightMobilesByFlags = _highlightByState.IsChecked;
            _currentProfile.PoisonHue = _poisonColorPickerBox.Hue;
            _currentProfile.ParalyzedHue = _paralyzedColorPickerBox.Hue;
            _currentProfile.InvulnerableHue = _invulnerableColorPickerBox.Hue;
            _currentProfile.MobileHPType = _hpComboBox.SelectedIndex;
            _currentProfile.MobileHPShowWhen = _hpComboBoxShowWhen.SelectedIndex;
            _currentProfile.HoldDownKeyTab = _holdDownKeyTab.IsChecked;
            _currentProfile.HoldDownKeyAltToCloseAnchored = _holdDownKeyAlt.IsChecked;

            _currentProfile.CloseAllAnchoredGumpsInGroupWithRightClick =
                _closeAllAnchoredGumpsWithRClick.IsChecked;

            _currentProfile.HoldShiftForContext = _holdShiftForContext.IsChecked;
            _currentProfile.HoldAltToMoveGumps = _holdAltToMoveGumps.IsChecked;
            _currentProfile.HoldShiftToSplitStack = _holdShiftToSplitStack.IsChecked;
            _currentProfile.CloseHealthBarType = _healtbarType.SelectedIndex;
            _currentProfile.HideScreenshotStoredInMessage = _hideScreenshotStoredInMessage.IsChecked;

            if (_currentProfile.DrawRoofs == _drawRoofs.IsChecked)
            {
                _currentProfile.DrawRoofs = !_drawRoofs.IsChecked;

                Client.Game.GetScene<GameScene>()?.UpdateMaxDrawZ(true);
            }

            if (_currentProfile.TopbarGumpIsDisabled != _enableTopbar.IsChecked)
            {
                if (_enableTopbar.IsChecked)
                {
                    UIManager.GetGump<TopBarGump>()?.Dispose();
                }
                else
                {
                    TopBarGump.Create();
                }

                _currentProfile.TopbarGumpIsDisabled = _enableTopbar.IsChecked;
            }

            if (_currentProfile.EnableCaveBorder != _enableCaveBorder.IsChecked)
            {
                StaticFilters.CleanCaveTextures();
                _currentProfile.EnableCaveBorder = _enableCaveBorder.IsChecked;
            }

            // ## BEGIN - END ## //     ORIG
            /*
            if (_currentProfile.TreeToStumps != _treeToStumps.IsChecked)
            {
                StaticFilters.CleanTreeTextures();
                _currentProfile.TreeToStumps = _treeToStumps.IsChecked;
            }
            */
            // ## BEGIN - END ## //     ORIG

            _currentProfile.FieldsType = _fieldsType.SelectedIndex;
            _currentProfile.HideVegetation = _hideVegetation.IsChecked;
            _currentProfile.NoColorObjectsOutOfRange = _noColorOutOfRangeObjects.IsChecked;
            _currentProfile.UseCircleOfTransparency = _useCircleOfTransparency.IsChecked;

            if (_currentProfile.CircleOfTransparencyRadius != _circleOfTranspRadius.Value)
            {
                _currentProfile.CircleOfTransparencyRadius = _circleOfTranspRadius.Value;
                CircleOfTransparency.Create(_currentProfile.CircleOfTransparencyRadius);
            }

            _currentProfile.CircleOfTransparencyType = _cotType.SelectedIndex;
            _currentProfile.StandardSkillsGump = _useStandardSkillsGump.IsChecked;

            if (_useStandardSkillsGump.IsChecked)
            {
                SkillGumpAdvanced newGump = UIManager.GetGump<SkillGumpAdvanced>();

                if (newGump != null)
                {
                    UIManager.Add(new StandardSkillsGump { X = newGump.X, Y = newGump.Y });

                    newGump.Dispose();
                }
            }
            else
            {
                StandardSkillsGump standardGump = UIManager.GetGump<StandardSkillsGump>();

                if (standardGump != null)
                {
                    UIManager.Add(new SkillGumpAdvanced { X = standardGump.X, Y = standardGump.Y });

                    standardGump.Dispose();
                }
            }

            _currentProfile.ShowNewMobileNameIncoming = _showMobileNameIncoming.IsChecked;
            _currentProfile.ShowNewCorpseNameIncoming = _showCorpseNameIncoming.IsChecked;
            _currentProfile.GridLootType = _gridLoot.SelectedIndex;
            _currentProfile.SallosEasyGrab = _sallosEasyGrab.IsChecked;
            _currentProfile.PartyInviteGump = _partyInviteGump.IsChecked;
            _currentProfile.UseObjectsFading = _objectsFading.IsChecked;
            _currentProfile.TextFading = _textFading.IsChecked;
            _currentProfile.UseSmoothBoatMovement = _use_smooth_boat_movement.IsChecked;

            if (_currentProfile.ShowHouseContent != _showHouseContent.IsChecked)
            {
                _currentProfile.ShowHouseContent = _showHouseContent.IsChecked;
                NetClient.Socket.Send(new PShowPublicHouseContent(_currentProfile.ShowHouseContent));
            }


            // sounds
            _currentProfile.EnableSound = _enableSounds.IsChecked;
            _currentProfile.EnableMusic = _enableMusic.IsChecked;
            _currentProfile.EnableFootstepsSound = _footStepsSound.IsChecked;
            _currentProfile.EnableCombatMusic = _combatMusic.IsChecked;
            _currentProfile.ReproduceSoundsInBackground = _musicInBackground.IsChecked;
            _currentProfile.SoundVolume = _soundsVolume.Value;
            _currentProfile.MusicVolume = _musicVolume.Value;
            Settings.GlobalSettings.LoginMusicVolume = _loginMusicVolume.Value;
            Settings.GlobalSettings.LoginMusic = _loginMusic.IsChecked;

            Client.Game.Scene.Audio.UpdateCurrentMusicVolume();
            Client.Game.Scene.Audio.UpdateCurrentSoundsVolume();

            if (!_currentProfile.EnableMusic)
            {
                Client.Game.Scene.Audio.StopMusic();
            }

            if (!_currentProfile.EnableSound)
            {
                Client.Game.Scene.Audio.StopSounds();
            }

            // speech
            _currentProfile.ScaleSpeechDelay = _scaleSpeechDelay.IsChecked;
            _currentProfile.SpeechDelay = _sliderSpeechDelay.Value;
            _currentProfile.SpeechHue = _speechColorPickerBox.Hue;
            _currentProfile.EmoteHue = _emoteColorPickerBox.Hue;
            _currentProfile.YellHue = _yellColorPickerBox.Hue;
            _currentProfile.WhisperHue = _whisperColorPickerBox.Hue;
            _currentProfile.PartyMessageHue = _partyMessageColorPickerBox.Hue;
            _currentProfile.GuildMessageHue = _guildMessageColorPickerBox.Hue;
            _currentProfile.AllyMessageHue = _allyMessageColorPickerBox.Hue;
            _currentProfile.ChatMessageHue = _chatMessageColorPickerBox.Hue;

            if (_currentProfile.ActivateChatAfterEnter != _chatAfterEnter.IsChecked)
            {
                UIManager.SystemChat.IsActive = !_chatAfterEnter.IsChecked;
                _currentProfile.ActivateChatAfterEnter = _chatAfterEnter.IsChecked;
            }

            _currentProfile.ActivateChatAdditionalButtons = _chatAdditionalButtonsCheckbox.IsChecked;
            _currentProfile.ActivateChatShiftEnterSupport = _chatShiftEnterCheckbox.IsChecked;
            _currentProfile.SaveJournalToFile = _saveJournalCheckBox.IsChecked;

            // video
            _currentProfile.EnableDeathScreen = _enableDeathScreen.IsChecked;
            _currentProfile.EnableBlackWhiteEffect = _enableBlackWhiteEffect.IsChecked;

            Client.Game.Scene.Camera.ZoomIndex = _sliderZoom.Value;
            _currentProfile.DefaultScale = Client.Game.Scene.Camera.Zoom;
            _currentProfile.EnableMousewheelScaleZoom = _zoomCheckbox.IsChecked;
            _currentProfile.RestoreScaleAfterUnpressCtrl = _restorezoomCheckbox.IsChecked;

            if (!CUOEnviroment.IsOutlands && _use_old_status_gump.IsChecked != _currentProfile.UseOldStatusGump)
            {
                StatusGumpBase status = StatusGumpBase.GetStatusGump();

                _currentProfile.UseOldStatusGump = _use_old_status_gump.IsChecked;

                if (status != null)
                {
                    status.Dispose();
                    UIManager.Add(StatusGumpBase.AddStatusGump(status.ScreenCoordinateX, status.ScreenCoordinateY));
                }
            }


            int.TryParse(_gameWindowWidth.Text, out int gameWindowSizeWidth);
            int.TryParse(_gameWindowHeight.Text, out int gameWindowSizeHeight);

            if (gameWindowSizeWidth != _currentProfile.GameWindowSize.X ||
                gameWindowSizeHeight != _currentProfile.GameWindowSize.Y)
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

            if (gameWindowPositionX != _currentProfile.GameWindowPosition.X ||
                gameWindowPositionY != _currentProfile.GameWindowPosition.Y)
            {
                if (vp != null)
                {
                    vp.Location = _currentProfile.GameWindowPosition =
                        new Point(gameWindowPositionX, gameWindowPositionY);
                }
            }

            if (_currentProfile.GameWindowLock != _gameWindowLock.IsChecked)
            {
                if (vp != null)
                {
                    vp.CanMove = !_gameWindowLock.IsChecked;
                }

                _currentProfile.GameWindowLock = _gameWindowLock.IsChecked;
            }

            if (_gameWindowFullsize.IsChecked && (gameWindowPositionX != -5 || gameWindowPositionY != -5))
            {
                if (_currentProfile.GameWindowFullSize == _gameWindowFullsize.IsChecked)
                {
                    _gameWindowFullsize.IsChecked = false;
                }
            }

            if (_currentProfile.GameWindowFullSize != _gameWindowFullsize.IsChecked)
            {
                Point n = Point.Zero, loc = Point.Zero;

                if (_gameWindowFullsize.IsChecked)
                {
                    if (vp != null)
                    {
                        n = vp.ResizeGameWindow
                            (new Point(Client.Game.Window.ClientBounds.Width, Client.Game.Window.ClientBounds.Height));

                        loc = _currentProfile.GameWindowPosition = vp.Location = new Point(-5, -5);
                    }
                }
                else
                {
                    if (vp != null)
                    {
                        n = vp.ResizeGameWindow(new Point(600, 480));
                        loc = vp.Location = _currentProfile.GameWindowPosition = new Point(20, 20);
                    }
                }

                _gameWindowPositionX.SetText(loc.X.ToString());
                _gameWindowPositionY.SetText(loc.Y.ToString());
                _gameWindowWidth.SetText(n.X.ToString());
                _gameWindowHeight.SetText(n.Y.ToString());

                _currentProfile.GameWindowFullSize = _gameWindowFullsize.IsChecked;
            }

            if (_currentProfile.WindowBorderless != _windowBorderless.IsChecked)
            {
                _currentProfile.WindowBorderless = _windowBorderless.IsChecked;
                Client.Game.SetWindowBorderless(_windowBorderless.IsChecked);
            }

            _currentProfile.UseAlternativeLights = _altLights.IsChecked;
            _currentProfile.UseCustomLightLevel = _enableLight.IsChecked;
            _currentProfile.LightLevel = (byte) (_lightBar.MaxValue - _lightBar.Value);

            if (_enableLight.IsChecked)
            {
                World.Light.Overall = _currentProfile.LightLevel;
                World.Light.Personal = 0;
            }
            else
            {
                World.Light.Overall = World.Light.RealOverall;
                World.Light.Personal = World.Light.RealPersonal;
            }

            _currentProfile.UseColoredLights = _useColoredLights.IsChecked;
            _currentProfile.UseDarkNights = _darkNights.IsChecked;
            _currentProfile.ShadowsEnabled = _enableShadows.IsChecked;
            _currentProfile.ShadowsStatics = _enableShadowsStatics.IsChecked;
            _currentProfile.AuraUnderFeetType = _auraType.SelectedIndex;
            _currentProfile.FilterType = _filterType.SelectedIndex;

            Client.Game.IsMouseVisible =
                Settings.GlobalSettings.RunMouseInASeparateThread = _runMouseInSeparateThread.IsChecked;

            _currentProfile.AuraOnMouse = _auraMouse.IsChecked;
            _currentProfile.PartyAura = _partyAura.IsChecked;
            _currentProfile.PartyAuraHue = _partyAuraColorPickerBox.Hue;
            _currentProfile.HideChatGradient = _hideChatGradient.IsChecked;

            // fonts
            _currentProfile.ForceUnicodeJournal = _forceUnicodeJournal.IsChecked;
            byte _fontValue = _fontSelectorChat.GetSelectedFont();
            _currentProfile.OverrideAllFonts = _overrideAllFonts.IsChecked;
            _currentProfile.OverrideAllFontsIsUnicode = _overrideAllFontsIsUnicodeCheckbox.SelectedIndex == 1;

            if (_currentProfile.ChatFont != _fontValue)
            {
                _currentProfile.ChatFont = _fontValue;
                UIManager.SystemChat.TextBoxControl.Font = _fontValue;
            }

            // combat
            _currentProfile.InnocentHue = _innocentColorPickerBox.Hue;
            _currentProfile.FriendHue = _friendColorPickerBox.Hue;
            _currentProfile.CriminalHue = _crimialColorPickerBox.Hue;
            _currentProfile.CanAttackHue = _canAttackColorPickerBox.Hue;
            _currentProfile.EnemyHue = _enemyColorPickerBox.Hue;
            _currentProfile.MurdererHue = _murdererColorPickerBox.Hue;
            _currentProfile.EnabledCriminalActionQuery = _queryBeforAttackCheckbox.IsChecked;
            _currentProfile.EnabledBeneficialCriminalActionQuery = _queryBeforeBeneficialCheckbox.IsChecked;
            _currentProfile.CastSpellsByOneClick = _castSpellsByOneClick.IsChecked;
            _currentProfile.BuffBarTime = _buffBarTime.IsChecked;

            _currentProfile.BeneficHue = _beneficColorPickerBox.Hue;
            _currentProfile.HarmfulHue = _harmfulColorPickerBox.Hue;
            _currentProfile.NeutralHue = _neutralColorPickerBox.Hue;
            _currentProfile.EnabledSpellHue = _spellColoringCheckbox.IsChecked;
            _currentProfile.EnabledSpellFormat = _spellFormatCheckbox.IsChecked;
            _currentProfile.SpellDisplayFormat = _spellFormatBox.Text;

            // macros
            Client.Game.GetScene<GameScene>().Macros.Save();

            // counters

            bool before = _currentProfile.CounterBarEnabled;
            _currentProfile.CounterBarEnabled = _enableCounters.IsChecked;
            _currentProfile.CounterBarCellSize = _cellSize.Value;
            _currentProfile.CounterBarRows = int.Parse(_rows.Text);
            _currentProfile.CounterBarColumns = int.Parse(_columns.Text);
            _currentProfile.CounterBarHighlightOnUse = _highlightOnUse.IsChecked;

            _currentProfile.CounterBarHighlightAmount = int.Parse(_highlightAmount.Text);
            _currentProfile.CounterBarAbbreviatedAmount = int.Parse(_abbreviatedAmount.Text);
            _currentProfile.CounterBarHighlightOnAmount = _highlightOnAmount.IsChecked;
            _currentProfile.CounterBarDisplayAbbreviatedAmount = _enableAbbreviatedAmount.IsChecked;

            CounterBarGump counterGump = UIManager.GetGump<CounterBarGump>();

            counterGump?.SetLayout
            (
                _currentProfile.CounterBarCellSize, _currentProfile.CounterBarRows,
                _currentProfile.CounterBarColumns
            );


            if (before != _currentProfile.CounterBarEnabled)
            {
                if (counterGump == null)
                {
                    if (_currentProfile.CounterBarEnabled)
                    {
                        UIManager.Add
                        (
                            new CounterBarGump
                            (
                                200, 200, _currentProfile.CounterBarCellSize,
                                _currentProfile.CounterBarRows, _currentProfile.CounterBarColumns
                            )
                        );
                    }
                }
                else
                {
                    counterGump.IsEnabled = counterGump.IsVisible = _currentProfile.CounterBarEnabled;
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
            _currentProfile.DisableDefaultHotkeys = _disableDefaultHotkeys.IsChecked;
            _currentProfile.DisableArrowBtn = _disableArrowBtn.IsChecked;
            _currentProfile.DisableTabBtn = _disableTabBtn.IsChecked;
            _currentProfile.DisableCtrlQWBtn = _disableCtrlQWBtn.IsChecked;
            _currentProfile.DisableAutoMove = _disableAutoMove.IsChecked;
            _currentProfile.AutoOpenDoors = _autoOpenDoors.IsChecked;
            _currentProfile.SmoothDoors = _smoothDoors.IsChecked;
            _currentProfile.AutoOpenCorpses = _autoOpenCorpse.IsChecked;
            _currentProfile.AutoOpenCorpseRange = int.Parse(_autoOpenCorpseRange.Text);
            _currentProfile.CorpseOpenOptions = _autoOpenCorpseOptions.SelectedIndex;
            _currentProfile.SkipEmptyCorpse = _skipEmptyCorpse.IsChecked;

            _currentProfile.EnableDragSelect = _enableDragSelect.IsChecked;
            _currentProfile.DragSelectModifierKey = _dragSelectModifierKey.SelectedIndex;
            _currentProfile.DragSelectHumanoidsOnly = _dragSelectHumanoidsOnly.IsChecked;

            _currentProfile.ShowSkillsChangedMessage =_showSkillsMessage.IsChecked;
            _currentProfile.ShowSkillsChangedDeltaValue = _showSkillsMessageDelta.Value;
            _currentProfile.ShowStatsChangedMessage = _showStatsMessage.IsChecked;

            _currentProfile.OverrideContainerLocation = _overrideContainerLocation.IsChecked;
            _currentProfile.OverrideContainerLocationSetting = _overrideContainerLocationSetting.SelectedIndex;

            _currentProfile.ShowTargetRangeIndicator = _showTargetRangeIndicator.IsChecked;


            bool updateHealthBars = _currentProfile.CustomBarsToggled != _customBars.IsChecked;
            _currentProfile.CustomBarsToggled = _customBars.IsChecked;

            if (updateHealthBars)
            {
                if (_currentProfile.CustomBarsToggled)
                {
                    List<HealthBarGump> hbgstandard = UIManager.Gumps.OfType<HealthBarGump>().ToList();

                    foreach (HealthBarGump healthbar in hbgstandard)
                    {
                        UIManager.Add
                            (new HealthBarGumpCustom(healthbar.LocalSerial) { X = healthbar.X, Y = healthbar.Y });

                        healthbar.Dispose();
                    }
                }
                else
                {
                    List<HealthBarGumpCustom> hbgcustom = UIManager.Gumps.OfType<HealthBarGumpCustom>().ToList();

                    foreach (HealthBarGumpCustom customhealthbar in hbgcustom)
                    {
                        UIManager.Add
                        (
                            new HealthBarGump(customhealthbar.LocalSerial)
                                { X = customhealthbar.X, Y = customhealthbar.Y }
                        );

                        customhealthbar.Dispose();
                    }
                }
            }

            _currentProfile.CBBlackBGToggled = _customBarsBBG.IsChecked;
            _currentProfile.SaveHealthbars = _saveHealthbars.IsChecked;


            // infobar
            _currentProfile.ShowInfoBar = _showInfoBar.IsChecked;
            _currentProfile.InfoBarHighlightType = _infoBarHighlightType.SelectedIndex;


            InfoBarManager ibmanager = Client.Game.GetScene<GameScene>().InfoBars;

            ibmanager.Clear();

            for (int i = 0; i < _infoBarBuilderControls.Count; i++)
            {
                if (!_infoBarBuilderControls[i].IsDisposed)
                {
                    ibmanager.AddItem
                    (
                        new InfoBarItem
                        (
                            _infoBarBuilderControls[i].LabelText, _infoBarBuilderControls[i].Var,
                            _infoBarBuilderControls[i].Hue
                        )
                    );
                }
            }

            ibmanager.Save();

            InfoBarGump infoBarGump = UIManager.GetGump<InfoBarGump>();

            if (_currentProfile.ShowInfoBar)
            {
                if (infoBarGump == null)
                {
                    UIManager.Add(new InfoBarGump { X = 300, Y = 300 });
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
            int containerScale = _currentProfile.ContainersScale;

            if ((byte) _containersScale.Value != containerScale || _currentProfile.ScaleItemsInsideContainers !=
                _containerScaleItems.IsChecked)
            {
                containerScale = _currentProfile.ContainersScale = (byte) _containersScale.Value;
                UIManager.ContainerScale = containerScale / 100f;
                _currentProfile.ScaleItemsInsideContainers = _containerScaleItems.IsChecked;

                foreach (ContainerGump resizableGump in UIManager.Gumps.OfType<ContainerGump>())
                {
                    resizableGump.RequestUpdateContents();
                }
            }

            _currentProfile.UseLargeContainerGumps = _useLargeContianersGumps.IsChecked;
            _currentProfile.DoubleClickToLootInsideContainers = _containerDoubleClickToLoot.IsChecked;
            _currentProfile.RelativeDragAndDropItems = _relativeDragAnDropItems.IsChecked;
            _currentProfile.HighlightContainerWhenSelected = _highlightContainersWhenMouseIsOver.IsChecked;


            // tooltip
            _currentProfile.UseTooltip = _use_tooltip.IsChecked;
            _currentProfile.TooltipTextHue = _tooltip_font_hue.Hue;
            _currentProfile.TooltipDelayBeforeDisplay = _delay_before_display_tooltip.Value;
            _currentProfile.TooltipBackgroundOpacity = _tooltip_background_opacity.Value;
            _currentProfile.TooltipDisplayZoom = _tooltip_zoom.Value;
            _currentProfile.TooltipFont = _tooltip_font_selector.GetSelectedFont();

            // ## BEGIN - END ## //
            ProfileManager.CurrentProfile.ColorStealth = _colorStealth.IsChecked;
            ProfileManager.CurrentProfile.StealthHue = _stealthColorPickerBox.Hue;
            ProfileManager.CurrentProfile.GoldType = _goldType.SelectedIndex;
            ProfileManager.CurrentProfile.GoldHue = _goldColorPickerBox.Hue;
            ProfileManager.CurrentProfile.ColorGold = _colorGold.IsChecked;
            ProfileManager.CurrentProfile.ColorEnergyBolt = _colorEnergyBolt.IsChecked;
            ProfileManager.CurrentProfile.EnergyBoltHue = _energyBoltColorPickerBox.Hue;
            ProfileManager.CurrentProfile.ColorTreeTile = _colorTreeTile.IsChecked;
            ProfileManager.CurrentProfile.TreeTileHue = _treeTileColorPickerBox.Hue;
            ProfileManager.CurrentProfile.ColorBlockerTile = _colorBlockerTile.IsChecked;
            ProfileManager.CurrentProfile.BlockerTileHue = _blockerTileColorPickerBox.Hue;
            ProfileManager.CurrentProfile.BlockerType = _blockerType.SelectedIndex;
            ProfileManager.CurrentProfile.HighlightTileAtRange = _highlightTileRange.IsChecked;
            ProfileManager.CurrentProfile.HighlightTileAtRangeRange = _highlightTileRangeRange.Value;
            ProfileManager.CurrentProfile.HighlightTileRangeHue = _highlightTileRangeColorPickerBox.Hue;
            ProfileManager.CurrentProfile.HighlightTileAtRangeSpell = _highlightTileRangeSpell.IsChecked;
            ProfileManager.CurrentProfile.HighlightTileAtRangeRangeSpell = _highlightTileRangeRangeSpell.Value;
            ProfileManager.CurrentProfile.HighlightTileRangeHueSpell = _highlightTileRangeColorPickerBoxSpell.Hue;
            ProfileManager.CurrentProfile.StealthNeonType = _stealthNeonType.SelectedIndex;
            ProfileManager.CurrentProfile.EnergyBoltNeonType = _energyBoltNeonType.SelectedIndex;
            ProfileManager.CurrentProfile.EnergyBoltArtType = _energyBoltArtType.SelectedIndex;
            ProfileManager.CurrentProfile.GlowingWeaponsType = _glowingWeaponsType.SelectedIndex;
            ProfileManager.CurrentProfile.OverheadRange = _overheadRange.IsChecked;
            ProfileManager.CurrentProfile.OverheadSummonTime = _overheadSummonTime.IsChecked;
            ProfileManager.CurrentProfile.OverheadPeaceTime = _overheadPeaceTime.IsChecked;
            ProfileManager.CurrentProfile.OwnAuraByHP = _ownAuraByHP.IsChecked;
            ProfileManager.CurrentProfile.InfernoBridge = _infernoBridge.IsChecked;
            ProfileManager.CurrentProfile.SpellOnCursor = _spellOnCursor.IsChecked;
            int.TryParse(_spellOnCursorOffsetX.Text, out int spellOnCursorOffsetX);
            int.TryParse(_spellOnCursorOffsetY.Text, out int spellOnCursorOffsetY);
            ProfileManager.CurrentProfile.SpellOnCursorOffset = new Point(spellOnCursorOffsetX, spellOnCursorOffsetY);
            ProfileManager.CurrentProfile.PreviewFields = _previewFields.IsChecked;
            ProfileManager.CurrentProfile.SpecialSetLastTargetCliloc = _SpecialSetLastTargetCliloc.IsChecked;
            ProfileManager.CurrentProfile.SpecialSetLastTargetClilocText = _SpecialSetLastTargetClilocText.Text;
            ProfileManager.CurrentProfile.LastTargetRange = _lastTargetRange.Value;
            ProfileManager.CurrentProfile.HighlightHealthBarByState = _highlightHealthBarByState.IsChecked;
            ProfileManager.CurrentProfile.FlashingHealthbarOutlineSelf = _flashingHealthbarOutlineSelf.IsChecked;
            ProfileManager.CurrentProfile.FlashingHealthbarOutlineParty = _flashingHealthbarOutlineParty.IsChecked;
            ProfileManager.CurrentProfile.FlashingHealthbarOutlineGreen = _flashingHealthbarOutlineGreen.IsChecked;
            ProfileManager.CurrentProfile.FlashingHealthbarOutlineOrange = _flashingHealthbarOutlineOrange.IsChecked;
            ProfileManager.CurrentProfile.FlashingHealthbarOutlineAll = _flashingHealthbarOutlineAll.IsChecked;
            ProfileManager.CurrentProfile.FlashingHealthbarNegativeOnly = _flashingHealthbarNegativeOnly.IsChecked;
            ProfileManager.CurrentProfile.FlashingHealthbarTreshold = _flashingHealthbarTreshold.Value;
            ProfileManager.CurrentProfile.HighlightLastTargetHealthBarOutline = _highlightLastTargetHealthBarOutline.IsChecked;
            ProfileManager.CurrentProfile.HighlightLastTargetType = _highlightLastTargetType.SelectedIndex;
            ProfileManager.CurrentProfile.HighlightLastTargetTypePoison = _highlightLastTargetTypePoison.SelectedIndex;
            ProfileManager.CurrentProfile.HighlightLastTargetTypePara = _highlightLastTargetTypePara.SelectedIndex;
            ProfileManager.CurrentProfile.HighlightGlowingWeaponsTypeHue = _highlightGlowingWeaponsTypeColorPickerBoxHue.Hue;
            ProfileManager.CurrentProfile.HighlightLastTargetTypeHue = _highlightLastTargetTypeColorPickerBox.Hue;
            ProfileManager.CurrentProfile.HighlightLastTargetTypePoisonHue = _highlightLastTargetTypeColorPickerBoxPoison.Hue;
            ProfileManager.CurrentProfile.HighlightLastTargetTypeParaHue = _highlightLastTargetTypeColorPickerBoxPara.Hue;
            ProfileManager.CurrentProfile.MobileHamstrungTime = _mobileHamstrungTime.IsChecked;
            ProfileManager.CurrentProfile.MobileHamstrungTimeCooldown = uint.Parse(_mobileHamstrungTimeCooldown.Text);

            ProfileManager.CurrentProfile.MultipleUnderlinesSelfParty = _multipleUnderlinesSelfParty.IsChecked;
            ProfileManager.CurrentProfile.MultipleUnderlinesSelfPartyBigBars = _multipleUnderlinesSelfPartyBigBars.IsChecked;
            ProfileManager.CurrentProfile.MultipleUnderlinesSelfPartyTransparency = _multipleUnderlinesSelfPartyTransparency.Value;
            ProfileManager.CurrentProfile.UseOldHealthBars = _useOldHealthBars.IsChecked;
            ProfileManager.CurrentProfile.BlackOutlineStatics = _blackOutlineStatics.IsChecked;
            ProfileManager.CurrentProfile.IgnoreStaminaCheck = _ignoreStaminaCheck.IsChecked;

            if (ProfileManager.CurrentProfile.TreeType != _treeType.SelectedIndex)
            {
                if (_treeType.SelectedIndex == 0)
                {
                    StaticFilters.CleanTreeTextures();
                }
                ProfileManager.CurrentProfile.TreeType = _treeType.SelectedIndex;
            }
            //##UCC##//
            ProfileManager.CurrentProfile.UOClassicCombatBuffbar_SwingEnabled = _uccSwing.IsChecked;
            ProfileManager.CurrentProfile.UOClassicCombatBuffbar_DoDEnabled = _uccDoD.IsChecked;
            ProfileManager.CurrentProfile.UOClassicCombatBuffbar_GotDEnabled = _uccGotD.IsChecked;
            ProfileManager.CurrentProfile.UOClassicCombatBuffbar_DoHEnabled = _uccDoH.IsChecked;
            ProfileManager.CurrentProfile.UOClassicCombatBuffbar_GotHEnabled = _uccGotH.IsChecked;
            ProfileManager.CurrentProfile.UOClassicCombatBuffbar_Locked = _uccLocked.IsChecked;

            if (ProfileManager.CurrentProfile.UOClassicCombatBuffbar != _uccEnableBuffbar.IsChecked)
            {
                UOClassicCombatBuffbar UOClassicCombatBuffbar = UIManager.GetGump<UOClassicCombatBuffbar>();

                if (_uccEnableBuffbar.IsChecked)
                {
                    if (UOClassicCombatBuffbar != null)
                        UOClassicCombatBuffbar.Dispose();

                    UOClassicCombatBuffbar = new UOClassicCombatBuffbar
                    {
                        X = ProfileManager.CurrentProfile.UOClassicCombatBuffbarLocation.X,
                        Y = ProfileManager.CurrentProfile.UOClassicCombatBuffbarLocation.Y
                    };
                    UIManager.Add(UOClassicCombatBuffbar);
                }
                else
                {
                    if (UOClassicCombatBuffbar != null)
                        UOClassicCombatBuffbar.Dispose();
                }

                ProfileManager.CurrentProfile.UOClassicCombatBuffbar = _uccEnableBuffbar.IsChecked;
            }
            //
            if (ProfileManager.CurrentProfile.UOClassicCombatSelf != _uccEnableSelf.IsChecked)
            {
                UOClassicCombatSelf UOClassicCombatSelf = UIManager.GetGump<UOClassicCombatSelf>();

                if (_uccEnableSelf.IsChecked)
                {
                    if (UOClassicCombatSelf != null)
                        UOClassicCombatSelf.Dispose();

                    UOClassicCombatSelf = new UOClassicCombatSelf
                    {
                        X = ProfileManager.CurrentProfile.UOClassicCombatSelfLocation.X,
                        Y = ProfileManager.CurrentProfile.UOClassicCombatSelfLocation.Y
                    };
                    UIManager.Add(UOClassicCombatSelf);
                }
                else
                {
                    if (UOClassicCombatSelf != null)
                        UOClassicCombatSelf.Dispose();
                }

                ProfileManager.CurrentProfile.UOClassicCombatSelf = _uccEnableSelf.IsChecked;
            }

            ProfileManager.CurrentProfile.UOClassicCombatSelf_ActionCooldown = uint.Parse(_uccActionCooldown.Text);
            ProfileManager.CurrentProfile.UOClassicCombatSelf_PoucheCooldown = uint.Parse(_uccPoucheCooldown.Text);
            ProfileManager.CurrentProfile.UOClassicCombatSelf_CurepotCooldown = uint.Parse(_uccCurepotCooldown.Text);
            ProfileManager.CurrentProfile.UOClassicCombatSelf_HealpotCooldown = uint.Parse(_uccHealpotCooldown.Text);
            ProfileManager.CurrentProfile.UOClassicCombatSelf_RefreshpotCooldown = uint.Parse(_uccRefreshpotCooldown.Text);
            ProfileManager.CurrentProfile.UOClassicCombatSelf_WaitForTarget = uint.Parse(_uccWaitForTarget.Text);

            ProfileManager.CurrentProfile.UOClassicCombatSelf_BandiesHPTreshold = uint.Parse(_uccBandiesHPTreshold.Text);
            ProfileManager.CurrentProfile.UOClassicCombatSelf_BandiesPoison = _uccBandiesPoison.IsChecked;
            ProfileManager.CurrentProfile.UOClassicCombatSelf_CurepotHPTreshold = uint.Parse(_uccCurepotHPTreshold.Text);
            ProfileManager.CurrentProfile.UOClassicCombatSelf_HealpotHPTreshold = uint.Parse(_uccHealpotHPTreshold.Text);
            ProfileManager.CurrentProfile.UOClassicCombatSelf_RefreshpotStamTreshold = uint.Parse(_uccRefreshpotStamTreshold.Text);

            ProfileManager.CurrentProfile.UOClassicCombatSelf_AutoRearmAfterDisarmedCooldown = uint.Parse(_uccAutoRearmAfterDisarmedCooldown.Text);
            ProfileManager.CurrentProfile.UOClassicCombatSelf_NoRefreshPotAfterHamstrung = _uccNoRefreshPotAfterHamstrung.IsChecked;
            ProfileManager.CurrentProfile.UOClassicCombatSelf_NoRefreshPotAfterHamstrungCooldown = uint.Parse(_uccNoRefreshPotAfterHamstrungCooldown.Text);
            ProfileManager.CurrentProfile.UOClassicCombatSelf_DisarmStrikeCooldown = uint.Parse(_uccDisarmStrikeCooldown.Text);
            ProfileManager.CurrentProfile.UOClassicCombatSelf_DisarmAttemptCooldown = uint.Parse(_uccDisarmAttemptCooldown.Text);
            ProfileManager.CurrentProfile.UOClassicCombatSelf_HamstringStrikeCooldown = uint.Parse(_uccHamstringStrikeCooldown.Text);
            ProfileManager.CurrentProfile.UOClassicCombatSelf_HamstringAttemptCooldown = uint.Parse(_uccHamstringAttemptCooldown.Text);

            ProfileManager.CurrentProfile.UOClassicCombatSelf_DisarmedCooldown = uint.Parse(_uccDisarmedCooldown.Text);
            ProfileManager.CurrentProfile.UOClassicCombatSelf_HamstrungCooldown = uint.Parse(_uccHamstrungCooldown.Text);

            ProfileManager.CurrentProfile.UOClassicCombatSelf_StrengthPotCooldown = uint.Parse(_uccStrengthPotCooldown.Text);
            ProfileManager.CurrentProfile.UOClassicCombatSelf_DexPotCooldown = uint.Parse(_uccDexPotCooldown.Text);

            ProfileManager.CurrentProfile.UOClassicCombatSelf_MinRNG = int.Parse(_uccRNGMin.Text);
            ProfileManager.CurrentProfile.UOClassicCombatSelf_MaxRNG = int.Parse(_uccRNGMax.Text);

            ProfileManager.CurrentProfile.UOClassicCombatSelf_ClilocTriggers = _uccClilocTrigger.IsChecked;
            ProfileManager.CurrentProfile.UOClassicCombatSelf_MacroTriggers = _uccMacroTrigger.IsChecked;


            ProfileManager.CurrentProfile.UOClassicCombatAL_LootDelay = uint.Parse(_uccLootDelay.Text);
            ProfileManager.CurrentProfile.UOClassicCombatAL_PurgeDelay = uint.Parse(_uccPurgeDelay.Text);
            ProfileManager.CurrentProfile.UOClassicCombatAL_QueueSpeed = uint.Parse(_uccQueueSpeed.Text);

            ProfileManager.CurrentProfile.UOClassicCombatAL_EnableGridLootColoring = _uccEnableGridLootColoring.IsChecked;
            ProfileManager.CurrentProfile.UOClassicCombatAL_EnableLootAboveID = _uccBEnableLootAboveID.IsChecked;

            ProfileManager.CurrentProfile.UOClassicCombatAL_LootAboveID = uint.Parse(_uccLootAboveID.Text);
            ProfileManager.CurrentProfile.UOClassicCombatAL_SL_Gray = uint.Parse(_uccSL_Gray.Text);
            ProfileManager.CurrentProfile.UOClassicCombatAL_SL_Blue = uint.Parse(_uccSL_Blue.Text);
            ProfileManager.CurrentProfile.UOClassicCombatAL_SL_Green = uint.Parse(_uccSL_Green.Text);
            ProfileManager.CurrentProfile.UOClassicCombatAL_SL_Red = uint.Parse(_uccSL_Red.Text);


            UOClassicCombatSelf UOClassicCombatSelfCurrent = UIManager.GetGump<UOClassicCombatSelf>();
            if (UOClassicCombatSelfCurrent != null)
                UOClassicCombatSelfCurrent.UpdateVars();

            //
            if (ProfileManager.CurrentProfile.UOClassicCombatLines != _uccEnableLines.IsChecked)
            {
                UOClassicCombatLines UOClassicCombatLines = UIManager.GetGump<UOClassicCombatLines>();

                if (_uccEnableLines.IsChecked)
                {
                    if (UOClassicCombatLines != null)
                        UOClassicCombatLines.Dispose();

                    UOClassicCombatLines = new UOClassicCombatLines
                    {
                        X = ProfileManager.CurrentProfile.UOClassicCombatLinesLocation.X,
                        Y = ProfileManager.CurrentProfile.UOClassicCombatLinesLocation.Y
                    };
                    UIManager.Add(UOClassicCombatLines);
                }
                else
                {
                    if (UOClassicCombatLines != null)
                        UOClassicCombatLines.Dispose();
                }

                ProfileManager.CurrentProfile.UOClassicCombatLines = _uccEnableLines.IsChecked;
            }
            //
            if (ProfileManager.CurrentProfile.UOClassicCombatAL != _uccEnableAL.IsChecked)
            {
                UOClassicCombatAL UOClassicCombatAL = UIManager.GetGump<UOClassicCombatAL>();

                if (_uccEnableAL.IsChecked)
                {
                    if (UOClassicCombatAL != null)
                        UOClassicCombatAL.Dispose();

                    UOClassicCombatAL = new UOClassicCombatAL
                    {
                        X = ProfileManager.CurrentProfile.UOClassicCombatALLocation.X,
                        Y = ProfileManager.CurrentProfile.UOClassicCombatALLocation.Y
                    };
                    UIManager.Add(UOClassicCombatAL);
                }
                else
                {
                    if (UOClassicCombatAL != null)
                        UOClassicCombatAL.Dispose();
                }

                ProfileManager.CurrentProfile.UOClassicCombatAL = _uccEnableAL.IsChecked;
            }

            ProfileManager.CurrentProfile.BandageGump = _bandageGump.IsChecked;

            int.TryParse(_bandageGumpOffsetX.Text, out int bandageGumpOffsetX);
            int.TryParse(_bandageGumpOffsetY.Text, out int bandageGumpOffsetY);

            ProfileManager.CurrentProfile.BandageGumpOffset = new Point(bandageGumpOffsetX, bandageGumpOffsetY);

            ProfileManager.CurrentProfile.WireFrameView = _wireframeView.IsChecked; //##WIREFRAME##//

            ProfileManager.CurrentProfile.HueImpassableView = _hueImpassableView.IsChecked;
            ProfileManager.CurrentProfile.HueImpassableViewHue = _hueImpassableViewColorPickerBox.Hue;

            ProfileManager.CurrentProfile.TextureManagerEnabled = _textureManagerEnabled.IsChecked; //##TEXTUREMANAGER##//
            ProfileManager.CurrentProfile.TextureManagerArrows = _textureManagerArrowsEnabled.IsChecked;
            ProfileManager.CurrentProfile.TextureManagerHalos = _textureManagerHalosEnabled.IsChecked;

            int.TryParse(_pullFriendlyBarsX.Text, out int pullFriendlyBarsX);
            int.TryParse(_pullFriendlyBarsY.Text, out int pullFriendlyBarsY);
            ProfileManager.CurrentProfile.PullFriendlyBars = new Point(pullFriendlyBarsX, pullFriendlyBarsY);
            int.TryParse(_pullFriendlyBarsFinalLocationX.Text, out int pullFriendlyBarsFinalLocationX);
            int.TryParse(_pullFriendlyBarsFinalLocationY.Text, out int pullFriendlyBarsFinalLocationY);
            ProfileManager.CurrentProfile.PullFriendlyBarsFinalLocation = new Point(pullFriendlyBarsFinalLocationX, pullFriendlyBarsFinalLocationY);

            int.TryParse(_pullEnemyBarsX.Text, out int pullEnemyBarsX);
            int.TryParse(_pullEnemyBarsY.Text, out int pullEnemyBarsY);
            ProfileManager.CurrentProfile.PullEnemyBars = new Point(pullEnemyBarsX, pullEnemyBarsY);
            int.TryParse(_pullEnemyBarsFinalLocationX.Text, out int pullEnemyBarsFinalLocationX);
            int.TryParse(_pullEnemyBarsFinalLocationY.Text, out int pullEnemyBarsFinalLocationY);
            ProfileManager.CurrentProfile.PullEnemyBarsFinalLocation = new Point(pullEnemyBarsFinalLocationX, pullEnemyBarsFinalLocationY);

            int.TryParse(_pullPartyAllyBarsX.Text, out int pullPartyAllyBarsX);
            int.TryParse(_pullPartyAllyBarsY.Text, out int pullPartyAllyBarsY);
            ProfileManager.CurrentProfile.PullPartyAllyBars = new Point(pullPartyAllyBarsX, pullPartyAllyBarsY);
            int.TryParse(_pullPartyAllyBarsFinalLocationX.Text, out int pullPartyAllyBarsFinalLocationX);
            int.TryParse(_pullPartyAllyBarsFinalLocationY.Text, out int pullPartyAllyBarsFinalLocationY);
            ProfileManager.CurrentProfile.PullPartyAllyBarsFinalLocation = new Point(pullPartyAllyBarsFinalLocationX, pullPartyAllyBarsFinalLocationY);

            ProfileManager.CurrentProfile.AutoWorldmapMarker = _autoWorldmapMarker.IsChecked;
            // ## BEGIN - END ## //

            _currentProfile?.Save(ProfileManager.ProfilePath);
        }

        internal void UpdateVideo()
        {
            _gameWindowWidth.SetText(_currentProfile.GameWindowSize.X.ToString());
            _gameWindowHeight.SetText(_currentProfile.GameWindowSize.Y.ToString());
            _gameWindowPositionX.SetText(_currentProfile.GameWindowPosition.X.ToString());
            _gameWindowPositionY.SetText(_currentProfile.GameWindowPosition.Y.ToString());
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ResetHueVector();

            batcher.Draw2D(LogoTexture, x + 190, y + 20, WIDTH - 250, 400, ref HueVector);
            batcher.DrawRectangle(SolidColorTextureCache.GetTexture(Color.Gray), x, y, Width, Height, ref HueVector);

            return base.Draw(batcher, x, y);
        }

        private InputField AddInputField
        (
            ScrollArea area,
            int x,
            int y,
            int width,
            int height,
            string label = null,
            int maxWidth = 0,
            bool set_down = false,
            bool numbersOnly = false,
            int maxCharCount = -1
        )
        {
            InputField elem = new InputField(0x0BB8, FONT, HUE_FONT, true, width, height, maxWidth, maxCharCount)
            {
                NumbersOnly = numbersOnly,
                X = x,
                Y = y
            };


            if (area != null)
            {
                Label text = AddLabel(area, label, x, y);

                if (set_down)
                {
                    elem.Y = text.Bounds.Bottom + 2;
                }
                else
                {
                    elem.X = text.Bounds.Right + 2;
                }

                area.Add(elem);
            }

            return elem;
        }

        private Label AddLabel(ScrollArea area, string text, int x, int y)
        {
            Label label = new Label(text, true, HUE_FONT)
            {
                X = x,
                Y = y
            };

            area?.Add(label);

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

            area?.Add(box);

            return box;
        }

        private Combobox AddCombobox
        (
            ScrollArea area,
            string[] values,
            int currentIndex,
            int x,
            int y,
            int width
        )
        {
            Combobox combobox = new Combobox(x, y, width, values)
            {
                SelectedIndex = currentIndex
            };

            area?.Add(combobox);

            return combobox;
        }

        private HSliderBar AddHSlider
        (
            ScrollArea area,
            int min,
            int max,
            int value,
            int x,
            int y,
            int width
        )
        {
            HSliderBar slider = new HSliderBar
                (x, y, width, min, max, value, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT);

            area?.Add(slider);

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
            area?.Add(box);

            area?.Add
            (
                new Label(text, true, HUE_FONT)
                {
                    X = x + box.Width + 10,
                    Y = y
                }
            );

            return box;
        }

        private SettingsSection AddSettingsSection(DataBox area, string label)
        {
            SettingsSection section = new SettingsSection(label, area.Width);
            area.Add(section);
            area.WantUpdateSize = true;
            //area.ReArrangeChildren();

            return section;
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
            private int _indent;

            public SettingsSection(string title, int width)
            {
                CanMove = true;
                AcceptMouseInput = true;
                WantUpdateSize = true;


                Label label = new Label(title, true, HUE_FONT, font: FONT);
                label.X = 5;
                base.Add(label);

                base.Add(new Line(0, label.Height, width - 30, 1, 0xFFbabdc2));

                Width = width;
                Height = label.Height + 1;

                _databox = new DataBox(label.X + 10, label.Height + 4, 0, 0);

                base.Add(_databox);
            }

            public void PushIndent()
            {
                _indent += 40;
            }

            public void PopIndent()
            {
                _indent -= 40;
            }


            public void AddRight(Control c, int offset = 15)
            {
                int i = _databox.Children.Count - 1;

                for (; i >= 0; --i)
                {
                    if (_databox.Children[i].IsVisible)
                    {
                        break;
                    }
                }

                c.X = i >= 0 ? _databox.Children[i].Bounds.Right + offset : _indent;

                c.Y = i >= 0 ? _databox.Children[i].Bounds.Top : 0;

                _databox.Add(c);
                _databox.WantUpdateSize = true;
            }

            public override void Add(Control c, int page = 0)
            {
                int i = _databox.Children.Count - 1;
                int bottom = 0;

                for (; i >= 0; --i)
                {
                    if (_databox.Children[i].IsVisible)
                    {
                        if (bottom == 0 || bottom < _databox.Children[i].Bounds.Bottom + 2)
                        {
                            bottom = _databox.Children[i].Bounds.Bottom + 2;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                c.X = _indent;
                c.Y = bottom;

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
                    _buttons[index].IsChecked = true;
                }
            }
        }

        private class InputField : Control
        {
            private readonly StbTextBox _textbox;

            public InputField
            (
                ushort backgroundGraphic,
                byte font,
                ushort hue,
                bool unicode,
                int width,
                int height,
                int maxWidthText = 0,
                int maxCharsCount = -1
            )
            {
                WantUpdateSize = false;

                Width = width;
                Height = height;

                ResizePic background = new ResizePic(backgroundGraphic)
                {
                    Width = width,
                    Height = height
                };

                _textbox = new StbTextBox(font, maxCharsCount, maxWidthText, unicode, FontStyle.BlackBorder, hue)
                {
                    X = 4,
                    Y = 4,
                    Width = width - 8,
                    Height = height - 8
                };


                Add(background);
                Add(_textbox);
            }


            public string Text => _textbox.Text;

            public override bool AcceptKeyboardInput
            {
                get => _textbox.AcceptKeyboardInput;
                set => _textbox.AcceptKeyboardInput = value;
            }

            public bool NumbersOnly
            {
                get => _textbox.NumbersOnly;
                set => _textbox.NumbersOnly = value;
            }


            public void SetText(string text)
            {
                _textbox.SetText(text);
            }
        }
    }
}