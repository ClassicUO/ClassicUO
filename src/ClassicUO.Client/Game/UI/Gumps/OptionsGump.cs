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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClassicUO.Configuration;
// ## BEGIN - END ## // UI/GUMPS
using ClassicUO.Dust765.Dust765;
// ## BEGIN - END ## // UI/GUMPS
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps.Login;
using ClassicUO.Dust765.Options;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Threading.Tasks;
using System.Net.Http;

namespace ClassicUO.Game.UI.Gumps
{
    internal partial class OptionsGump : Gump
    {
        private const byte FONT = 0xFF;
        private const ushort HUE_FONT = 0xFFFF;
        private static int WIDTH => LoginLayoutHelper.OptionsWidth;
        private static int HEIGHT => LoginLayoutHelper.OptionsHeight;
        private const int TEXTBOX_HEIGHT = 25;
        private static readonly string[] WINDOW_TITLE_STYLE_LABELS =
        {
            "Like CUO (native)",
            "Like UOS/Orion (custom)"
        };

        private static Texture2D _logoTexture2D;
        private Combobox _auraType;
        private Combobox _autoOpenCorpseOptions;
        private InputField _autoOpenCorpseRange;
        private Combobox _windowTitleStyle;
        private bool _syncingWindowTitleStyleControls;

        //experimental
        private Checkbox _autoOpenDoors, _autoOpenCorpse, _skipEmptyCorpse, _disableTabBtn, _disableCtrlQWBtn, _disableDefaultHotkeys, _disableArrowBtn, _disableAutoMove, _overrideContainerLocation, _smoothDoors, _showTargetRangeIndicator, _autoAvoidObstacules, _customBars, _customBarsBBG, _saveHealthbars;
        private HSliderBar _cellSize;
        private Checkbox _containerScaleItems, _containerDoubleClickToLoot, _relativeDragAnDropItems, _useLargeContianersGumps, _highlightContainersWhenMouseIsOver, _useGridLayoutContainerGumps;


        // containers
        private HSliderBar _containersScale;
        private ModernColorPicker.HueDisplay _altGridContainerBackgroundHue, _gridBorderHue;
        private Combobox _cotType;
        private DataBox _databox;
        private HSliderBar _delay_before_display_tooltip, _tooltip_zoom, _tooltip_background_opacity;
        private Combobox _dragSelectModifierKey, _backpackStyle, _gridContainerSearchAlternative, _gridBorderStyle, _dragSelectPlayersModifier, _dragSelectMonsertModifier, _dragSelectNameplateModifier;
        private Checkbox _hueContainerGumps, _gridContainerItemScale, _gridContainerPreview, _gridContainerAnchorable, _gridOverrideWithContainerHue;
        private HSliderBar _containerOpacity, _gridBorderOpacity, _gridContainerScale;
        private InputField _gridDefaultColumns, _gridDefaultRows;

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
                         _pathFindSingleClick,
                         _alwaysRun,
                         _alwaysRunUnlessHidden,
                         _fastRotation,
                         _showHpMobile,
                         _highlightByPoisoned,
                         _highlightByParalyzed,
                         _highlightByInvul,
                         _drawRoofs,
                         // ## BEGIN - END ## // ART / HUE CHANGES
                         //_treeToStumps,
                         // ## BEGIN - END ## // ART / HUE CHANGES
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
        private Checkbox _holdShiftForContext, _holdShiftToSplitStack, _reduceFPSWhenInactive, _sallosEasyGrab, _partyInviteGump, _objectsFading, _textFading, _holdAltToMoveGumps;
        private Combobox _hpComboBox, _healtbarType, _fieldsType, _hpComboBoxShowWhen;

        // infobar
        private List<InfoBarBuilderControl> _infoBarBuilderControls;
        private Combobox _infoBarHighlightType;

        // combat & spells
        private ModernColorPicker.HueDisplay _innocentColorPickerBox, _friendColorPickerBox, _crimialColorPickerBox, _canAttackColorPickerBox, _enemyColorPickerBox, _murdererColorPickerBox, _neutralColorPickerBox, _beneficColorPickerBox, _harmfulColorPickerBox, _improvedBuffBarHue,
            _damageHueSelf, _damageHuePet, _damageHueAlly, _damageHueLastAttack, _damageHueOther;
        private HSliderBar _lightBar;
        private Checkbox _buffBarTime, _uiButtonsSingleClick, _queryBeforAttackCheckbox, _queryBeforeBeneficialCheckbox, _spellColoringCheckbox, _spellFormatCheckbox, _enableFastSpellsAssign, _enableImprovedBuffGump;

        // macro
        private MacroControl _macroControl;
        private Checkbox _overrideAllFonts;
        private Combobox _overrideAllFontsIsUnicodeCheckbox;
        private Combobox _overrideContainerLocationSetting;
        private ModernColorPicker.HueDisplay _poisonColorPickerBox, _paralyzedColorPickerBox, _invulnerableColorPickerBox;
        private NiceButton _randomizeColorsButton;
        private Checkbox _restorezoomCheckbox, _zoomCheckbox;
        private InputField _rows, _columns, _highlightAmount, _abbreviatedAmount;

        // speech
        private Checkbox _scaleSpeechDelay, _saveJournalCheckBox;
        private Checkbox _showHouseContent;
        private Checkbox _showInfoBar;
        private Checkbox _showHPInTitleBar;
        private Checkbox _actionBarEnabled;
        private Checkbox _ignoreAllianceMessages;
        private Checkbox _ignoreGuildMessages;

        // general
        private HSliderBar _sliderFPS, _circleOfTranspRadius;
        private HSliderBar _sliderSpeechDelay;
        private HSliderBar _sliderZoom;
        private HSliderBar _soundsVolume, _musicVolume, _loginMusicVolume;
        private HSliderBar _hiddenBodyAlpha;
        private ModernColorPicker.HueDisplay _hiddenBodyHue;
        private ModernColorPicker.HueDisplay _speechColorPickerBox, _emoteColorPickerBox, _yellColorPickerBox, _whisperColorPickerBox, _partyMessageColorPickerBox, _guildMessageColorPickerBox, _allyMessageColorPickerBox, _chatMessageColorPickerBox, _partyAuraColorPickerBox;
        private InputField _spellFormatBox;
        private ModernColorPicker.HueDisplay _tooltip_font_hue;
        private FontSelector _tooltip_font_selector;
        private HSliderBar _dragSelectStartX, _dragSelectStartY;
        private Checkbox _dragSelectAsAnchor, _namePlateHealthBar, _disableSystemChat, _journalMessagesOnlyInJournalBox, _namePlateShowAtFullHealth;
        private HSliderBar _journalOpacity, _namePlateOpacity, _namePlateHealthBarOpacity;
        private ModernColorPicker.HueDisplay _journalBackgroundColor;
        private Combobox _journalStyle;
        private NameOverheadAssignControl _nameOverheadControl;

        // video
        private Checkbox _use_old_status_gump, _windowBorderless, _enableWindowBorderFrame, _enableDeathScreen, _enableBlackWhiteEffect, _altLights, _enableLight, _enableShadows, _enableShadowsStatics, _auraMouse, _runMouseInSeparateThread, _useColoredLights, _darkNights, _partyAura, _hideChatGradient, _animatedWaterEffect;
        private Combobox _lightLevelType;
        private Checkbox _use_smooth_boat_movement;
        private HSliderBar _terrainShadowLevel;

        private Checkbox _use_tooltip, _enableTooltipOverride;
        private Checkbox _useStandardSkillsGump, _showMobileNameIncoming, _showCorpseNameIncoming;
        private Checkbox _showStatsMessage, _showSkillsMessage;
        private HSliderBar _showSkillsMessageDelta;
        // ## BEGIN - END ## // BASICSETUP
        // ## BEGIN - END ## // TITLE BAR
        private Checkbox _enableTitleBarStats;
        private Checkbox _titleBarStatsModeText;
        private Checkbox _titleBarStatsModePercent;
        private Checkbox _titleBarStatsModeProgressBar;
        // ## BEGIN - END ## // ART / HUE CHANGES
        private Checkbox _colorStealth, _colorEnergyBolt, _colorGold, _colorTreeTile, _colorBlockerTile;
        private ModernColorPicker.HueDisplay _stealthColorPickerBox, _energyBoltColorPickerBox, _goldColorPickerBox, _treeTileColorPickerBox, _blockerTileColorPickerBox;
        private Combobox _goldType, _treeType, _blockerType, _stealthNeonType, _energyBoltNeonType, _energyBoltArtType;
        // ## BEGIN - END ## // ART / HUE CHANGES
        // ## BEGIN - END ## // VISUAL HELPERS
        private Checkbox _highlightTileRange, _highlightTileRangeSpell, _ownAuraByHP, _previewFields;
        private HSliderBar _highlightTileRangeRange, _highlightTileRangeRangeSpell;
        private ModernColorPicker.HueDisplay _highlightTileRangeColorPickerBox, _highlightTileRangeColorPickerBoxSpell, _highlightLastTargetTypeColorPickerBox, _highlighFriendsGuildTypeHueColorPickerBox, _highlightLastTargetTypeColorPickerBoxPoison, _highlightLastTargetTypeColorPickerBoxPara, _highlightLastTargetTypeColorPickerBoxStunned, _highlightLastTargetTypeColorPickerBoxMortalled, _highlightGlowingWeaponsTypeColorPickerBoxHue, _hueImpassableViewColorPickerBox;
        private Combobox _glowingWeaponsType, _highlightLastTargetType, _highlighFriendsGuildType, _highlightLastTargetTypePoison, _highlightLastTargetTypePara, _highlightLastTargetTypeStunned, _highlightLastTargetTypeMortalled;
        // ## BEGIN - END ## // VISUAL HELPERS
        // ## BEGIN - END ## // HEALTHBAR
        private Checkbox _highlightLastTargetHealthBarOutline, _highlightHealthBarByState;
        // ## BEGIN - END ## // HEALTHBAR
        // ## BEGIN - END ## // CURSOR
        private InputField _spellOnCursorOffsetX, _spellOnCursorOffsetY;
        private Checkbox _spellOnCursor, _colorGameCursor;
        // ## BEGIN - END ## // CURSOR
        // ## BEGIN - END ## // OVERHEAD / UNDERCHAR
        private Checkbox _overheadRange;
        // ## BEGIN - END ## // OVERHEAD / UNDERCHAR
        // ## BEGIN - END ## // OLDHEALTHLINES
        private Checkbox _multipleUnderlinesSelfParty, _multipleUnderlinesSelfPartyBigBars, _useOldHealthBars;
        private HSliderBar _multipleUnderlinesSelfPartyTransparency;
        // ## BEGIN - END ## // OLDHEALTHLINES
        // ## BEGIN - END ## // MISC
        private Checkbox _offscreenTargeting, _setTargetOut, _SpecialSetLastTargetCliloc, _blackOutlineStatics, _ignoreStaminaCheck, _blockWoS, _blockWoSFelOnly, _blockWoSArtForceAoS, _blockEnergyF, _blockEnergyFFelOnly, _blockEnergyFArtForceAoS;
        private InputField _SpecialSetLastTargetClilocText, _blockWoSArt, _blockEnergyFArt;
        // ## BEGIN - END ## // MISC
        // ## BEGIN - END ## // MISC2
        private Checkbox _wireframeView, _hueImpassableView, _transparentHouses, _invisibleHouses, _ignoreCoT, _showDeathOnWorldmap, _showMapCloseFriend, _drawMobilesWithSurfaceOverhead, _autoAvoidMobiles, _scaleMonstersEnabled, _forceGargoyleWalk;
        private HSliderBar _transparentHousesZ, _transparentHousesTransparency, _invisibleHousesZ, _dontRemoveHouseBelowZ;
        // ## BEGIN - END ## // MISC2
        // ## BEGIN - END ## // MACROS
        private HSliderBar _lastTargetRange;
        // ## BEGIN - END ## // MACROS
        // ## BEGIN - END ## // NAMEOVERHEAD
        private Checkbox _showHPLineInNOH;
        // ## BEGIN - END ## // NAMEOVERHEAD
        // ## BEGIN - END ## // UI/GUMPS
        private Checkbox _bandageGump, _bandageUpDownToggle, _uccEnableLTBar;
        private InputField _bandageGumpOffsetX, _bandageGumpOffsetY;
        // ## BEGIN - END ## // UI/GUMPS
        // ## BEGIN - END ## // LINES
        private Checkbox _uccEnableLines;
        // ## BEGIN - END ## // LINES
        // ## BEGIN - END ## // AUTOLOOT
        private Checkbox _uccEnableAL, _uccEnableGridLootColoring, _uccBEnableLootAboveID;
        // ## BEGIN - END ## // AUTOLOOT
        // ## BEGIN - END ## // BUFFBAR/UCCSETTINGS
        private Checkbox _uccEnableBuffbar, _uccEnableSelf, _uccLocked, _uccSwing, _uccDoD, _uccGotD;
        // ## BEGIN - END ## // BUFFBAR/UCCSETTINGS
        // ## BEGIN - END ## // BUFFBAR/UCCSETTINGS
        private InputField _uccDisarmStrikeCooldown, _uccDisarmAttemptCooldown, _uccDisarmedCooldown;
        // ## BEGIN - END ## // BUFFBAR/UCCSETTINGS
        // ## BEGIN - END ## // SELF
        private Checkbox _uccBandiesPoison, _uccNoRefreshPotAfterHamstrung;
        private InputField _uccBandiesHPTreshold, _uccCurepotHPTreshold, _uccHealpotHPTreshold, _uccRefreshpotStamTreshold, _uccAutoRearmAfterDisarmedCooldown, _uccNoRefreshPotAfterHamstrungCooldown, _uccStrengthPotCooldown, _uccDexPotCooldown, _uccRNGMin, _uccRNGMax;
        private Checkbox _uccClilocTrigger, _uccMacroTrigger;
        // ## BEGIN - END ## // SELF
        // ## BEGIN - END ## // ADVMACROS
        private InputField _pullFriendlyBarsX, _pullFriendlyBarsY, _pullFriendlyBarsFinalLocationX, _pullFriendlyBarsFinalLocationY;
        private InputField _pullEnemyBarsX, _pullEnemyBarsY, _pullEnemyBarsFinalLocationX, _pullEnemyBarsFinalLocationY;
        private InputField _pullPartyAllyBarsX, _pullPartyAllyBarsY, _pullPartyAllyBarsFinalLocationX, _pullPartyAllyBarsFinalLocationY;
        // ## BEGIN - END ## // ADVMACROS
        // ## BEGIN - END ## // AUTOMATIONS
        private Checkbox _autoWorldmapMarker;
        private Checkbox _autoRangeDisplayAlways;
        private ModernColorPicker.HueDisplay _autoRangeDisplayHue;
        // ## BEGIN - END ## // AUTOMATIONS
        // ## BEGIN - END ## // OUTLANDS
        /*
        private Checkbox _infernoBridge, _overheadSummonTime, _overheadPeaceTime, _mobileHamstrungTime;
        private InputField _mobileHamstrungTimeCooldown;
        //##UCC##//
        private InputField _uccHamstringStrikeCooldown, _uccHamstringAttemptCooldown, _uccHamstrungCooldown;
        private Checkbox _uccDoH, _uccGotH;
        */
        // ## BEGIN - END ## // OUTLANDS
        // ## BEGIN - END ## // LOBBY
        private InputField _lobbyIP, _lobbyPort;
        // ## BEGIN - END ## // LOBBY
        // ## BEGIN - END ## // STATUSGUMP
        private Checkbox _useRazorEnhStatusGump;
        // ## BEGIN - END ## // STATUSGUMP
        // ## BEGIN - END ## // ONCASTINGGUMP
        private Checkbox _onCastingGump, _onCastingGump_hidden, _onCastingUnderPlayerBar;
        // ## BEGIN - END ## // ONCASTINGGUMP
        // ## BEGIN - END ## // MISC3 SHOWALLLAYERS
        private Checkbox _showAllLayers, _showAllLayersPaperdoll, _colorPaperdollByDurability;
        private InputField _showAllLayersPaperdoll_X;
        // ## BEGIN - END ## // MISC3 SHOWALLLAYERS
        // ## BEGIN - END ## // MISC3 THIEFSUPREME
        private Checkbox _overrideContainerOpenRange;
        // ## BEGIN - END ## // MISC3 THIEFSUPREME
        // ## BEGIN - END ## // VISUALRESPONSEMANAGER
        private Checkbox _visualResponseManager;
        // ## BEGIN - END ## // VISUALRESPONSEMANAGER
        // ## BEGIN - END ## // TABGRID // PKRION
        private Checkbox _enableTabGridGump;
        private InputField _tablistBox, _rowsGrid, _tabsGrid;
        // ## BEGIN - END ## // TABGRID // PKRION
        // ## BEGIN - END ## // BASICSETUP

        private Checkbox _leftAlignToolTips, _namePlateHealthOnlyWarmode, _enableHealthIndicator, _spellIconDisplayHotkey, _enableAlphaScrollWheel, _useModernShop, _forceCenterAlignMobileTooltips, _openHealthBarForLastAttack;
        private Checkbox _hideJournalBorder, _hideJournalTimestamp, _gridHideBorder, _skillProgressBarOnChange, _uselastCooldownPosition, _closeHPBarWhenAnchored, _enableNearbyItemGump;
        private InputField _healthIndicatorPercentage, _healthIndicatorWidth, _tooltipHeaderFormat, _skillProgressBarFormat;
        private ModernColorPicker.HueDisplay _mainWindowHuePicker, _spellIconHotkeyHue, _tooltipBGHue;
        private HSliderBar _spellIconScale, _overheadTextWidth, _gridHightlightLineSize, _maxJournalEntries;
        private HSliderBar _healthLineSizeMultiplier, _regularPlayerAlpha, _nameplateBorderOpacity;

        #region Cooldowns
        private InputField _coolDownX, _coolDownY;
        #endregion


        private Profile _currentProfile = ProfileManager.CurrentProfile;
        private OptionsGumpLanguage _lang = Language.Instance.GetOptionsGumpLanguage;
        private Dust765Language _langDust = Language.Instance.GetDust765;

        public OptionsGump() : base(0, 0)
        {
            Add
            (
                new AlphaBlendControl(0.95f)
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
                new NiceButton
                (
                    10,
                    10 + 30 * i++,
                    140,
                    25,
                    ButtonAction.SwitchPage,
                    ResGumps.General
                )
                { IsSelected = true, ButtonParameter = 1 }
            );

            Add
            (
                new NiceButton
                (
                    10,
                    10 + 30 * i++,
                    140,
                    25,
                    ButtonAction.SwitchPage,
                    _lang.ButtonSound
                )
                { ButtonParameter = 2 }
            );

            Add
            (
                new NiceButton
                (
                    10,
                    10 + 30 * i++,
                    140,
                    25,
                    ButtonAction.SwitchPage,
                    _lang.ButtonVideo
                )
                { ButtonParameter = 3 }
            );

            Add
            (
                new NiceButton
                (
                    10,
                    10 + 30 * i++,
                    140,
                    25,
                    ButtonAction.SwitchPage,
                    _lang.ButtonMacros
                )
                { ButtonParameter = 4 }
            );

            Add
            (
                new NiceButton
                (
                    10,
                    10 + 30 * i++,
                    140,
                    25,
                    ButtonAction.SwitchPage,
                    _lang.ButtonTooltips
                )
                { ButtonParameter = 5 }
            );

            Add
            (
                new NiceButton
                (
                    10,
                    10 + 30 * i++,
                    140,
                    25,
                    ButtonAction.SwitchPage,
                    ResGumps.Fonts
                )
                { ButtonParameter = 6 }
            );

            Add
            (
                new NiceButton
                (
                    10,
                    10 + 30 * i++,
                    140,
                    25,
                    ButtonAction.SwitchPage,
                    _lang.ButtonSpeech
                )
                { ButtonParameter = 7 }
            );

            Add
            (
                new NiceButton
                (
                    10,
                    10 + 30 * i++,
                    140,
                    25,
                    ButtonAction.SwitchPage,
                    _lang.ButtonCombatSpells
                )
                { ButtonParameter = 8 }
            );

            Add
            (
                new NiceButton
                (
                    10,
                    10 + 30 * i++,
                    140,
                    25,
                    ButtonAction.SwitchPage,
                    _lang.ButtonCounters
                )
                { ButtonParameter = 9 }
            );

            Add
            (
                new NiceButton
                (
                    10,
                    10 + 30 * i++,
                    140,
                    25,
                    ButtonAction.SwitchPage,
                    _lang.ButtonInfobar
                )
                { ButtonParameter = 10 }
            );

            Add
            (
                new NiceButton
                (
                    10,
                    10 + 30 * i++,
                    140,
                    25,
                    ButtonAction.SwitchPage,
                    _lang?.GetActionBar?.ShowActionBar ?? "Action Bar"
                )
                { ButtonParameter = 14 }
            );

            Add
            (
                new NiceButton
                (
                    10,
                    10 + 30 * i++,
                    140,
                    25,
                    ButtonAction.SwitchPage,
                    _lang.ButtonContainers
                )
                { ButtonParameter = 11 }
            );

            Add
            (
                new NiceButton
                (
                    10,
                    10 + 30 * i++,
                    140,
                    25,
                    ButtonAction.SwitchPage,
                    _lang.ButtonExperimental
                )
                { ButtonParameter = 12 }
            );

            Add
            (
                new NiceButton
                (
                    10,
                    10 + 30 * i++,
                    140,
                    25,
                    ButtonAction.Activate,
                    _lang.ButtonIgnoreList
                )
                {
                    ButtonParameter = (int)Buttons.OpenIgnoreList
                }
            );

            // ## BEGIN - END ## // BASICSETUP
            Add(new NiceButton(10, 10 + 30 * i++, 140, 25, ButtonAction.SwitchPage, "Dust") { ButtonParameter = 16 });
            Add(new NiceButton(10, 10 + 30 * i++, 140, 25, ButtonAction.SwitchPage, "765") { ButtonParameter = 17 });
            Add(new NiceButton(10, 10 + 30 * i++, 140, 25, ButtonAction.SwitchPage, "Mods") { ButtonParameter = 18 });
            // ## BEGIN - END ## // BASICSETUP

            Add
            (
                new NiceButton
                (
                    10,
                    10 + 30 * i++,
                    140,
                    25,
                    ButtonAction.SwitchPage,
                    "Nameplate Options"
                )
                {
                    ButtonParameter = 13
                }
            );

            Add
            (
                new NiceButton
                (
                    10,
                    10 + 30 * i++,
                    140,
                    25,
                    ButtonAction.SwitchPage,
                    "Cooldowns"
                )
                {
                    ButtonParameter = 8787
                }
            );

            Add
            (
                new NiceButton
                (
                    10,
                    10 + 30 * i++,
                    140,
                    25,
                    ButtonAction.SwitchPage,
                    "Grid Container"
                )
                {
                    ButtonParameter = 8788
                }
            );

            Add
            (
                new NiceButton
                (
                    10,
                    10 + 30 * i++,
                    140,
                    25,
                    ButtonAction.SwitchPage,
                    "Profiles"
                )
                {
                    ButtonParameter = 8789
                }
            );

            int offsetX = 60;
            int offsetY = 60;

            int bottomLineY = HEIGHT - 50;

            Add
            (
                new Line
                (
                    160,
                    bottomLineY,
                    WIDTH - 160,
                    1,
                    Color.Gray.PackedValue
                )
            );

            const int BTN_WIDTH = 90;
            const int BTN_HEIGHT = 20;
            int btnY = HEIGHT - BTN_HEIGHT - 15;

            var cancelBtn = new GothicStyleButton(154 + offsetX, btnY, BTN_WIDTH, BTN_HEIGHT, "Cancel");
            cancelBtn.BaseColor = new Color(130, 55, 55);
            cancelBtn.HighlightColor = new Color(165, 90, 90);
            cancelBtn.ShadowColor = new Color(90, 35, 35);
            cancelBtn.OnClick += () => OnButtonClick((int)Buttons.Cancel);
            Add(cancelBtn);

            var applyBtn = new GothicStyleButton(248 + offsetX, btnY, BTN_WIDTH, BTN_HEIGHT, "Apply");
            applyBtn.BaseColor = new Color(40, 115, 40);
            applyBtn.HighlightColor = new Color(70, 155, 70);
            applyBtn.ShadowColor = new Color(25, 75, 25);
            applyBtn.OnClick += () => OnButtonClick((int)Buttons.Apply);
            Add(applyBtn);

            var defaultBtn = new GothicStyleButton(346 + offsetX, btnY, BTN_WIDTH, BTN_HEIGHT, "Default");
            defaultBtn.BaseColor = new Color(100, 100, 100);
            defaultBtn.HighlightColor = new Color(155, 155, 155);
            defaultBtn.ShadowColor = new Color(60, 60, 60);
            defaultBtn.OnClick += () => OnButtonClick((int)Buttons.Default);
            Add(defaultBtn);

            var okBtn = new GothicStyleButton(443 + offsetX, btnY, BTN_WIDTH, BTN_HEIGHT, "Okay");
            okBtn.BaseColor = new Color(165, 130, 50);
            okBtn.HighlightColor = new Color(210, 175, 90);
            okBtn.ShadowColor = new Color(115, 90, 30);
            okBtn.OnClick += () => OnButtonClick((int)Buttons.Ok);
            Add(okBtn);

            Width = WIDTH;
            Height = HEIGHT;
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
            BuildActionBar();
            BuildContainers();
            BuildExperimental();
            BuildNameOverhead();
            BuildCooldowns();
            BuildGridContainer();
            BuildProfiles();
            // ## BEGIN - END ## // BASICSETUP
            BuildDust();
            Build765();
            BuildMods();
            // ## BEGIN - END ## // BASICSETUP

            ChangePage(1);
        }

        private static Texture2D LogoTexture
        {
            get
            {
                if (_logoTexture2D == null || _logoTexture2D.IsDisposed)
                {
                    string logoPath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client", "logodust.png");
                    _logoTexture2D = PNGLoader.Instance.GetImageTexture(logoPath);

                    if (_logoTexture2D == null || _logoTexture2D.IsDisposed)
                    {
                        using var stream = new MemoryStream(Loader.GetCuoLogo().ToArray());
                        _logoTexture2D = Texture2D.FromStream(Client.Game.GraphicsDevice, stream);
                    }
                }

                return _logoTexture2D;
            }
        }

        private void BuildGeneral()
        {
            const int PAGE = 1;

            ScrollArea rightArea = new ScrollArea
            (
                165,
                20,
                WIDTH - 185,
                HEIGHT - 80,
                true
            );
            rightArea.ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways;

            int startX = 5;
            int startY = 5;


            DataBox box = new DataBox(startX, startY, rightArea.Width - 15, 1);
            box.WantUpdateSize = true;
            rightArea.Add(box);


            SettingsSection section = AddSettingsSection(box, "General");


            section.Add
            (
                _highlightObjects = AddCheckBox
                (
                    null,
                    ResGumps.HighlightObjects,
                    _currentProfile.HighlightGameObjects,
                    startX,
                    startY
                )
            );

            section.Add
            (
                _enablePathfind = AddCheckBox
                (
                    null,
                    ResGumps.EnablePathfinding,
                    _currentProfile.EnablePathfind,
                    startX,
                    startY
                )
            );

            section.AddRight
            (
                _useShiftPathfind = AddCheckBox
                (
                    null,
                    ResGumps.ShiftPathfinding,
                    _currentProfile.UseShiftToPathfind,
                    startX,
                    startY
                )
            );

            section.Add
            (
                _pathFindSingleClick = AddCheckBox
                (
                    null,
                    "Single click",
                    _currentProfile.PathfindSingleClick,
                    startX,
                    startY
                )
            );

            section.Add
            (
                _alwaysRun = AddCheckBox
                (
                    null,
                    ResGumps.AlwaysRun,
                    _currentProfile.AlwaysRun,
                    startX,
                    startY
                )
            );

            section.AddRight
            (
                _alwaysRunUnlessHidden = AddCheckBox
                (
                    null,
                    ResGumps.AlwaysRunHidden,
                    _currentProfile.AlwaysRunUnlessHidden,
                    startX,
                    startY
                )
            );

            section.Add
            (
                _autoOpenDoors = AddCheckBox
                (
                    null,
                    ResGumps.AutoOpenDoors,
                    _currentProfile.AutoOpenDoors,
                    startX,
                    startY
                )
            );

            section.AddRight
            (
                _smoothDoors = AddCheckBox
                (
                    null,
                    ResGumps.SmoothDoors,
                    _currentProfile.SmoothDoors,
                    startX,
                    startY
                )
            );

            section.Add
            (
                _fastRotation = AddCheckBox
                (
                    null,
                    ResGumps.FastRotation,
                    _currentProfile.FastRotation,
                    startX,
                    startY
                )
            );

            section.Add
            (
                _autoOpenCorpse = AddCheckBox
                (
                    null,
                    ResGumps.AutoOpenCorpses,
                    _currentProfile.AutoOpenCorpses,
                    startX,
                    startY
                )
            );

            section.PushIndent();
            section.Add(AddLabel(null, ResGumps.CorpseOpenRange, 0, 0));

            section.AddRight
            (
                _autoOpenCorpseRange = AddInputField
                (
                    null,
                    startX,
                    startY,
                    50,
                    TEXTBOX_HEIGHT,
                    ResGumps.CorpseOpenRange,
                    50,
                    false,
                    true,
                    5
                )
            );

            _autoOpenCorpseRange.SetText(_currentProfile.AutoOpenCorpseRange.ToString());

            section.Add
            (
                _skipEmptyCorpse = AddCheckBox
                (
                    null,
                    ResGumps.SkipEmptyCorpses,
                    _currentProfile.SkipEmptyCorpse,
                    startX,
                    startY
                )
            );

            section.Add(AddLabel(null, ResGumps.CorpseOpenOptions, startX, startY));

            section.AddRight
            (
                _autoOpenCorpseOptions = AddCombobox
                (
                    null,
                    new[]
                    {
                        ResGumps.CorpseOpt_None, ResGumps.CorpseOpt_NotTar, ResGumps.CorpseOpt_NotHid,
                        ResGumps.CorpseOpt_Both
                    },
                    _currentProfile.CorpseOpenOptions,
                    startX,
                    startY,
                    150
                ),
                2
            );

            section.PopIndent();

            section.Add
            (
                _noColorOutOfRangeObjects = AddCheckBox
                (
                    rightArea,
                    ResGumps.OutOfRangeColor,
                    _currentProfile.NoColorObjectsOutOfRange,
                    startX,
                    startY
                )
            );

            section.Add
            (
                _sallosEasyGrab = AddCheckBox
                (
                    null,
                    ResGumps.SallosEasyGrab,
                    _currentProfile.SallosEasyGrab,
                    startX,
                    startY
                )
            );

            section.Add
            (
                _showHouseContent = AddCheckBox
                (
                    null,
                    ResGumps.ShowHousesContent,
                    _currentProfile.ShowHouseContent,
                    startX,
                    startY
                )
            );

            _showHouseContent.IsVisible = Client.Version >= ClientVersion.CV_70796;

            section.Add
            (
                _use_smooth_boat_movement = AddCheckBox
                (
                    null,
                    ResGumps.SmoothBoat,
                    _currentProfile.UseSmoothBoatMovement,
                    startX,
                    startY
                )
            );

            _use_smooth_boat_movement.IsVisible = Client.Version >= ClientVersion.CV_7090;

            SettingsSection section2 = AddSettingsSection(box, "Mobiles");
            section2.Y = section.Bounds.Bottom + 40;

            section2.Add
            (
                _showHpMobile = AddCheckBox
                (
                    null,
                    ResGumps.ShowHP,
                    _currentProfile.ShowMobilesHP,
                    startX,
                    startY
                )
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
                    null,
                    new[] { ResGumps.HP_Percentage, ResGumps.HP_Line, ResGumps.HP_Both },
                    mode,
                    startX,
                    startY,
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
                    null,
                    new[] { ResGumps.HPShow_Always, ResGumps.HPShow_Less, ResGumps.HPShow_Smart },
                    mode,
                    startX,
                    startY,
                    100
                ),
                2
            );

            section2.Add
            (
                _highlightByPoisoned = AddCheckBox
                (
                    null,
                    ResGumps.HighlightPoisoned,
                    _currentProfile.HighlightMobilesByPoisoned,
                    startX,
                    startY
                )
            );

            section2.PushIndent();

            section2.Add
            (
                _poisonColorPickerBox = AddHueDisplay
                (
                    null,
                    startX,
                    startY,
                    _currentProfile.PoisonHue,
                    ResGumps.PoisonedColor
                )
            );

            section2.AddRight(AddLabel(null, ResGumps.PoisonedColor, 0, 0), 2);
            section2.PopIndent();

            section2.Add
            (
                _highlightByParalyzed = AddCheckBox
                (
                    null,
                    ResGumps.HighlightParalyzed,
                    _currentProfile.HighlightMobilesByParalize,
                    startX,
                    startY
                )
            );

            section2.PushIndent();

            section2.Add
            (
                _paralyzedColorPickerBox = AddHueDisplay
                (
                    null,
                    startX,
                    startY,
                    _currentProfile.ParalyzedHue,
                    ResGumps.ParalyzedColor
                )
            );

            section2.AddRight(AddLabel(null, ResGumps.ParalyzedColor, 0, 0), 2);

            section2.PopIndent();

            section2.Add
            (
                _highlightByInvul = AddCheckBox
                (
                    null,
                    ResGumps.HighlightInvulnerable,
                    _currentProfile.HighlightMobilesByInvul,
                    startX,
                    startY
                )
            );

            section2.PushIndent();

            section2.Add
            (
                _invulnerableColorPickerBox = AddHueDisplay
                (
                    null,
                    startX,
                    startY,
                    _currentProfile.InvulnerableHue,
                    ResGumps.InvulColor
                )
            );

            section2.AddRight(AddLabel(null, ResGumps.InvulColor, 0, 0), 2);
            section2.PopIndent();

            section2.Add
            (
                _showMobileNameIncoming = AddCheckBox
                (
                    null,
                    ResGumps.ShowIncMobiles,
                    _currentProfile.ShowNewMobileNameIncoming,
                    startX,
                    startY
                )
            );

            section2.Add
            (
                _showCorpseNameIncoming = AddCheckBox
                (
                    null,
                    ResGumps.ShowIncCorpses,
                    _currentProfile.ShowNewCorpseNameIncoming,
                    startX,
                    startY
                )
            );

            section2.Add(AddLabel(null, ResGumps.AuraUnderFeet, startX, startY));

            section2.AddRight
            (
                _auraType = AddCombobox
                (
                    null,
                    new[]
                    {
                        ResGumps.AuraType_None, ResGumps.AuraType_Warmode, ResGumps.AuraType_CtrlShift,
                        ResGumps.AuraType_Always
                    },
                    _currentProfile.AuraUnderFeetType,
                    startX,
                    startY,
                    100
                ),
                2
            );

            section2.PushIndent();

            section2.Add
            (
                _partyAura = AddCheckBox
                (
                    null,
                    ResGumps.CustomColorAuraForPartyMembers,
                    _currentProfile.PartyAura,
                    startX,
                    startY
                )
            );

            section2.PushIndent();

            section2.Add
            (
                _partyAuraColorPickerBox = AddHueDisplay
                (
                    null,
                    startX,
                    startY,
                    _currentProfile.PartyAuraHue,
                    ResGumps.PartyAuraColor
                )
            );

            section2.AddRight(AddLabel(null, ResGumps.PartyAuraColor, 0, 0));

            section2.PopIndent();
            section2.PopIndent();

            SettingsSection section3 = AddSettingsSection(box, "Gumps & Context");
            section3.Y = section2.Bounds.Bottom + 40;

            section3.Add
            (
                _enableTopbar = AddCheckBox
                (
                    null,
                    ResGumps.DisableMenu,
                    _currentProfile.TopbarGumpIsDisabled,
                    0,
                    0
                )
            );

            section3.Add
            (
                _holdDownKeyAlt = AddCheckBox
                (
                    null,
                    ResGumps.AltCloseGumps,
                    _currentProfile.HoldDownKeyAltToCloseAnchored,
                    0,
                    0
                )
            );

            section3.Add
            (
                _holdAltToMoveGumps = AddCheckBox
                (
                    null,
                    ResGumps.AltMoveGumps,
                    _currentProfile.HoldAltToMoveGumps,
                    0,
                    0
                )
            );

            section3.Add
            (
                _closeAllAnchoredGumpsWithRClick = AddCheckBox
                (
                    null,
                    ResGumps.ClickCloseAllGumps,
                    _currentProfile.CloseAllAnchoredGumpsInGroupWithRightClick,
                    0,
                    0
                )
            );

            section3.Add
            (
                _useStandardSkillsGump = AddCheckBox
                (
                    null,
                    ResGumps.StandardSkillGump,
                    _currentProfile.StandardSkillsGump,
                    0,
                    0
                )
            );

            section3.Add
            (
                _use_old_status_gump = AddCheckBox
                (
                    null,
                    ResGumps.UseOldStatusGump,
                    _currentProfile.UseOldStatusGump,
                    startX,
                    startY
                )
            );

            _use_old_status_gump.IsVisible = !CUOEnviroment.IsOutlands;

            section3.Add
            (
                _partyInviteGump = AddCheckBox
                (
                    null,
                    ResGumps.ShowGumpPartyInv,
                    _currentProfile.PartyInviteGump,
                    0,
                    0
                )
            );

            section3.Add
            (
                _customBars = AddCheckBox
                (
                    null,
                    ResGumps.UseCustomHPBars,
                    _currentProfile.CustomBarsToggled,
                    0,
                    0
                )
            );

            section3.AddRight
            (
                _customBarsBBG = AddCheckBox
                (
                    null,
                    ResGumps.UseBlackBackgr,
                    _currentProfile.CBBlackBGToggled,
                    0,
                    0
                )
            );

            section3.Add
            (
                _saveHealthbars = AddCheckBox
                (
                    null,
                    ResGumps.SaveHPBarsOnLogout,
                    _currentProfile.SaveHealthbars,
                    0,
                    0
                )
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
                null,
                new[] { ResGumps.HPType_None, ResGumps.HPType_MobileOOR, ResGumps.HPType_MobileDead },
                mode,
                0,
                0,
                150
            );

            section3.AddRight(_healtbarType);
            section3.PopIndent();
            section3.Add(AddLabel(null, ResGumps.GridLoot, startX, startY));

            section3.AddRight
            (
                _gridLoot = AddCombobox
                (
                    null,
                    new[] { ResGumps.GridLoot_None, ResGumps.GridLoot_GridOnly, ResGumps.GridLoot_Both },
                    _currentProfile.GridLootType,
                    startX,
                    startY,
                    120
                ),
                2
            );

            section3.Add
            (
                _holdShiftForContext = AddCheckBox
                (
                    null,
                    ResGumps.ShiftContext,
                    _currentProfile.HoldShiftForContext,
                    0,
                    0
                )
            );

            section3.Add
            (
                _holdShiftToSplitStack = AddCheckBox
                (
                    null,
                    ResGumps.ShiftStack,
                    _currentProfile.HoldShiftToSplitStack,
                    0,
                    0
                )
            );


            SettingsSection section4 = AddSettingsSection(box, "Miscellaneous");
            section4.Y = section3.Bounds.Bottom + 40;

            section4.Add
            (
                _useCircleOfTransparency = AddCheckBox
                (
                    null,
                    ResGumps.EnableCircleTrans,
                    _currentProfile.UseCircleOfTransparency,
                    startX,
                    startY
                )
            );

            section4.AddRight
            (
                _circleOfTranspRadius = AddHSlider
                (
                    null,
                    Constants.MIN_CIRCLE_OF_TRANSPARENCY_RADIUS,
                    Constants.MAX_CIRCLE_OF_TRANSPARENCY_RADIUS,
                    _currentProfile.CircleOfTransparencyRadius,
                    startX,
                    startY,
                    200
                )
            );

            section4.PushIndent();
            section4.Add(AddLabel(null, ResGumps.CircleTransType, startX, startY));
            int cottypeindex = _currentProfile.CircleOfTransparencyType;
            string[] cotTypes = { ResGumps.CircleTransType_Full, ResGumps.CircleTransType_Gradient, "Modern" };

            if (cottypeindex < 0 || cottypeindex > cotTypes.Length)
            {
                cottypeindex = 0;
            }

            section4.AddRight
            (
                _cotType = AddCombobox
                (
                    null,
                    cotTypes,
                    cottypeindex,
                    startX,
                    startY,
                    150
                ),
                2
            );

            section4.PopIndent();

            section4.Add
            (
                _hideScreenshotStoredInMessage = AddCheckBox
                (
                    null,
                    ResGumps.HideScreenshotStoredInMessage,
                    _currentProfile.HideScreenshotStoredInMessage,
                    0,
                    0
                )
            );

            section4.Add
            (
                _objectsFading = AddCheckBox
                (
                    null,
                    ResGumps.ObjAlphaFading,
                    _currentProfile.UseObjectsFading,
                    startX,
                    startY
                )
            );

            section4.Add
            (
                _textFading = AddCheckBox
                (
                    null,
                    ResGumps.TextAlphaFading,
                    _currentProfile.TextFading,
                    startX,
                    startY
                )
            );
                

            section4.Add
            (
                _enableDragSelect = AddCheckBox
                (
                    null,
                    ResGumps.EnableDragHPBars,
                    _currentProfile.EnableDragSelect,
                    startX,
                    startY
                )
            );

            section4.PushIndent();
            section4.Add(AddLabel(null, ResGumps.DragKey, startX, startY));

            section4.AddRight
            (
                _dragSelectModifierKey = AddCombobox
                (
                    null,
                    new[] { ResGumps.KeyMod_None, ResGumps.KeyMod_Ctrl, ResGumps.KeyMod_Shift },
                    _currentProfile.DragSelectModifierKey,
                    startX,
                    startY,
                    100
                )
            );

            section4.Add(AddLabel(null, "Select players key", 0, 0));
            section4.AddRight(_dragSelectPlayersModifier = AddCombobox(
                null, new[] { "Disabled", "Ctrl", "Shift" },
                _currentProfile.DragSelect_PlayersModifier,
                0, 0, 100
                ));

            section4.Add(AddLabel(null, "Select monsters key", 0, 0));
            section4.AddRight(_dragSelectMonsertModifier = AddCombobox(
                null, new[] { "Disabled", "Ctrl", "Shift" },
                _currentProfile.DragSelect_MonstersModifier,
                0, 0, 100
                ));

            section4.Add(AddLabel(null, "Select visible nameplates key", 0, 0));
            section4.AddRight(_dragSelectNameplateModifier = AddCombobox(
                null, new[] { "Disabled", "Ctrl", "Shift" },
                _currentProfile.DragSelect_NameplateModifier,
                0, 0, 100
                ));

            //section4.Add
            //(
            //    _dragSelectHumanoidsOnly = AddCheckBox
            //    (
            //        null,
            //        ResGumps.DragHumanoidsOnly,
            //        _currentProfile.DragSelectHumanoidsOnly,
            //        startX,
            //        startY
            //    )
            //);

            section4.Add(new Label(ResGumps.DragSelectStartingPosX, true, HUE_FONT));
            section4.Add(_dragSelectStartX = new HSliderBar(startX, startY, 200, 0, Client.Game.Scene.Camera.Bounds.Width, _currentProfile.DragSelectStartX, HSliderBarStyle.MetalWidgetRecessedBar, true, 0, HUE_FONT));

            section4.Add(new Label(ResGumps.DragSelectStartingPosY, true, HUE_FONT));
            section4.Add(_dragSelectStartY = new HSliderBar(startX, startY, 200, 0, Client.Game.Scene.Camera.Bounds.Height, _currentProfile.DragSelectStartY, HSliderBarStyle.MetalWidgetRecessedBar, true, 0, HUE_FONT));
            section4.Add
            (
                _dragSelectAsAnchor = AddCheckBox
                (
                    null, ResGumps.DragSelectAnchoredHB, _currentProfile.DragSelectAsAnchor, startX,
                    startY
                )
            );

            section4.PopIndent();

            section4.Add
            (
                _showStatsMessage = AddCheckBox
                (
                    null,
                    ResGumps.ShowStatsChangedMessage,
                    _currentProfile.ShowStatsChangedMessage,
                    startX,
                    startY
                )
            );

            section4.Add
            (
                _showSkillsMessage = AddCheckBox
                (
                    null,
                    ResGumps.ShowSkillsChangedMessageBy,
                    _currentProfile.ShowSkillsChangedMessage,
                    startX,
                    startY
                )
            );

            section4.PushIndent();

            section4.AddRight
            (
                _showSkillsMessageDelta = AddHSlider
                (
                    null,
                    0,
                    100,
                    _currentProfile.ShowSkillsChangedDeltaValue,
                    startX,
                    startY,
                    150
                )
            );

            section4.PopIndent();

            SettingsSection section5 = AddSettingsSection(box, "Terrain & Statics");
            section5.Y = section4.Bounds.Bottom + 40;

            section5.Add
            (
                _drawRoofs = AddCheckBox
                (
                    null,
                    ResGumps.HideRoofTiles,
                    !_currentProfile.DrawRoofs,
                    startX,
                    startY
                )
            );

            // ## BEGIN - END ## // ART / HUE CHANGES
            /*
            section5.Add
            (
                _treeToStumps = AddCheckBox
                (
                    null,
                    ResGumps.TreesStumps,
                    _currentProfile.TreeToStumps,
                    startX,
                    startY
                )
            );
            */
            // ## BEGIN - END ## // ART / HUE CHANGES

            section5.Add
            (
                _hideVegetation = AddCheckBox
                (
                    null,
                    ResGumps.HideVegetation,
                    _currentProfile.HideVegetation,
                    startX,
                    startY
                )
            );

            section5.Add
            (
                _enableCaveBorder = AddCheckBox
                (
                    null,
                    ResGumps.MarkCaveTiles,
                    _currentProfile.EnableCaveBorder,
                    startX,
                    startY
                )
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
                    null,
                    new[] { ResGumps.HPFields_Normal, ResGumps.HPFields_Static, ResGumps.HPFields_Tile },
                    mode,
                    startX,
                    startY,
                    150
                )
            );


            Add(rightArea, PAGE);
        }

        private void BuildSounds()
        {
            const int PAGE = 2;

            ScrollArea rightArea = new ScrollArea
            (
                165,
                20,
                WIDTH - 185,
                HEIGHT - 80,
                true
            );
            rightArea.ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways;

            int startX = 5;
            int startY = 5;

            const int VOLUME_WIDTH = 200;

            _enableSounds = AddCheckBox
            (
                rightArea,
                ResGumps.Sounds,
                _currentProfile.EnableSound,
                startX,
                startY
            );

            _enableMusic = AddCheckBox
            (
                rightArea,
                ResGumps.Music,
                _currentProfile.EnableMusic,
                startX,
                startY + _enableSounds.Height + 2
            );

            _loginMusic = AddCheckBox
            (
                rightArea,
                ResGumps.LoginMusic,
                Settings.GlobalSettings.LoginMusic,
                startX,
                startY + _enableSounds.Height + 2 + _enableMusic.Height + 2
            );

            startX = 120;
            startY += 2;

            _soundsVolume = AddHSlider
            (
                rightArea,
                0,
                100,
                _currentProfile.SoundVolume,
                startX,
                startY,
                VOLUME_WIDTH
            );

            _musicVolume = AddHSlider
            (
                rightArea,
                0,
                100,
                _currentProfile.MusicVolume,
                startX,
                startY + _enableSounds.Height + 2,
                VOLUME_WIDTH
            );

            _loginMusicVolume = AddHSlider
            (
                rightArea,
                0,
                100,
                Settings.GlobalSettings.LoginMusicVolume,
                startX,
                startY + _enableSounds.Height + 2 + _enableMusic.Height + 2,
                VOLUME_WIDTH
            );

            startX = 5;
            startY += _loginMusic.Bounds.Bottom + 2;

            _footStepsSound = AddCheckBox
            (
                rightArea,
                ResGumps.PlayFootsteps,
                _currentProfile.EnableFootstepsSound,
                startX,
                startY
            );

            startY += _footStepsSound.Height + 2;

            _combatMusic = AddCheckBox
            (
                rightArea,
                ResGumps.CombatMusic,
                _currentProfile.EnableCombatMusic,
                startX,
                startY
            );

            startY += _combatMusic.Height + 2;

            _musicInBackground = AddCheckBox
            (
                rightArea,
                ResGumps.ReproduceSoundsAndMusic,
                _currentProfile.ReproduceSoundsInBackground,
                startX,
                startY
            );

            startY += _musicInBackground.Height + 2;

            Add(rightArea, PAGE);
        }

        private void BuildVideo()
        {
            const int PAGE = 3;

            ScrollArea rightArea = new ScrollArea
            (
                165,
                20,
                WIDTH - 185,
                HEIGHT - 80,
                true
            );

            int startX = 5;
            int startY = 5;


            Label text = AddLabel(rightArea, ResGumps.FPS, startX, startY);
            startX += text.Bounds.Right + 5;

            _sliderFPS = AddHSlider
            (
                rightArea,
                Constants.MIN_FPS,
                Constants.MAX_FPS,
                Settings.GlobalSettings.FPS,
                startX,
                startY,
                250
            );

            startY += text.Bounds.Bottom + 5;

            _reduceFPSWhenInactive = AddCheckBox
            (
                rightArea,
                ResGumps.FPSInactive,
                _currentProfile.ReduceFPSWhenInactive,
                startX,
                startY
            );

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
                    null,
                    ResGumps.AlwaysUseFullsizeGameWindow,
                    _currentProfile.GameWindowFullSize,
                    startX,
                    startY
                )
            );

            section.Add
            (
                _windowBorderless = AddCheckBox
                (
                    null,
                    ResGumps.BorderlessWindow,
                    _currentProfile.WindowBorderless,
                    startX,
                    startY
                )
            );

            section.Add
            (
                _enableWindowBorderFrame = AddCheckBox
                (
                    null,
                    "Custom border frame (borderless mode)",
                    _currentProfile.EnableWindowBorderFrame,
                    startX,
                    startY
                )
            );

            section.Add
            (
                _gameWindowLock = AddCheckBox
                (
                    null,
                    ResGumps.LockGameWindowMovingResizing,
                    _currentProfile.GameWindowLock,
                    startX,
                    startY
                )
            );
            section.Add(AddLabel(null, ResGumps.GamePlayWindowPosition, startX, startY));

            section.AddRight
            (
                _gameWindowPositionX = AddInputField
                (
                    null,
                    startX,
                    startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    50,
                    false,
                    true
                ),
                4
            );

            var camera = Client.Game.Scene.Camera;

            _gameWindowPositionX.SetText(camera.Bounds.X.ToString());

            section.AddRight
            (
                _gameWindowPositionY = AddInputField
                (
                    null,
                    startX,
                    startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    50,
                    false,
                    true
                )
            );

            _gameWindowPositionY.SetText(camera.Bounds.Y.ToString());


            section.Add(AddLabel(null, ResGumps.GamePlayWindowSize, startX, startY));

            section.AddRight
            (
                _gameWindowWidth = AddInputField
                (
                    null,
                    startX,
                    startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    50,
                    false,
                    true
                )
            );

            _gameWindowWidth.SetText(camera.Bounds.Width.ToString());

            section.AddRight
            (
                _gameWindowHeight = AddInputField
                (
                    null,
                    startX,
                    startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    50,
                    false,
                    true
                )
            );

            _gameWindowHeight.SetText(camera.Bounds.Height.ToString());


            SettingsSection section2 = AddSettingsSection(box, "Zoom");
            section2.Y = section.Bounds.Bottom + 40;
            section2.Add(AddLabel(null, ResGumps.DefaultZoom, startX, startY));

            var cameraZoomCount = (int)((camera.ZoomMax - camera.ZoomMin) / camera.ZoomStep);
            var cameraZoomIndex = cameraZoomCount - (int)((camera.ZoomMax - camera.Zoom) / camera.ZoomStep);

            section2.AddRight
            (
                _sliderZoom = AddHSlider
                (
                    null,
                    0,
                    cameraZoomCount,
                    cameraZoomIndex,
                    startX,
                    startY,
                    100
                )
            );

            section2.Add
            (
                _zoomCheckbox = AddCheckBox
                (
                    null,
                    ResGumps.EnableMouseWheelForZoom,
                    _currentProfile.EnableMousewheelScaleZoom,
                    startX,
                    startY
                )
            );

            section2.Add
            (
                _restorezoomCheckbox = AddCheckBox
                (
                    null,
                    ResGumps.ReleasingCtrlRestoresScale,
                    _currentProfile.RestoreScaleAfterUnpressCtrl,
                    startX,
                    startY
                )
            );


            SettingsSection section3 = AddSettingsSection(box, "Lights");
            section3.Y = section2.Bounds.Bottom + 40;

            section3.Add
            (
                _altLights = AddCheckBox
                (
                    null,
                    ResGumps.AlternativeLights,
                    _currentProfile.UseAlternativeLights,
                    startX,
                    startY
                )
            );

            section3.Add
            (
                _enableLight = AddCheckBox
                (
                    null,
                    ResGumps.LightLevel,
                    _currentProfile.UseCustomLightLevel,
                    startX,
                    startY
                )
            );

            section3.AddRight
            (
                _lightBar = AddHSlider
                (
                    null,
                    0,
                    0x1E,
                    0x1E - _currentProfile.LightLevel,
                    startX,
                    startY,
                    250
                )
            );

            section3.Add(AddLabel(null, ResGumps.LightLevelType, startX, startY));

            section3.AddRight
            (
                _lightLevelType = AddCombobox
                (
                    null,
                    new[] { ResGumps.LightLevelTypeAbsolute, ResGumps.LightLevelTypeMinimum },
                    _currentProfile.LightLevelType,
                    startX,
                    startY,
                    150
                )
            );

            section3.Add
            (
                _darkNights = AddCheckBox
                (
                    null,
                    ResGumps.DarkNights,
                    _currentProfile.UseDarkNights,
                    startX,
                    startY
                )
            );

            section3.Add
            (
                _useColoredLights = AddCheckBox
                (
                    null,
                    ResGumps.UseColoredLights,
                    _currentProfile.UseColoredLights,
                    startX,
                    startY
                )
            );


            SettingsSection section4 = AddSettingsSection(box, "Misc");
            section4.Y = section3.Bounds.Bottom + 40;

            section4.Add
            (
                _enableDeathScreen = AddCheckBox
                (
                    null,
                    ResGumps.EnableDeathScreen,
                    _currentProfile.EnableDeathScreen,
                    startX,
                    startY
                )
            );

            section4.AddRight
            (
                _enableBlackWhiteEffect = AddCheckBox
                (
                    null,
                    ResGumps.BlackWhiteModeForDeadPlayer,
                    _currentProfile.EnableBlackWhiteEffect,
                    startX,
                    startY
                )
            );

            section4.Add
            (
                _runMouseInSeparateThread = AddCheckBox
                (
                    null,
                    ResGumps.RunMouseInASeparateThread,
                    Settings.GlobalSettings.RunMouseInASeparateThread,
                    startX,
                    startY
                )
            );

            section4.Add
            (
                _auraMouse = AddCheckBox
                (
                    null,
                    ResGumps.AuraOnMouseTarget,
                    _currentProfile.AuraOnMouse,
                    startX,
                    startY
                )
            );

            section4.Add
            (
                _animatedWaterEffect = AddCheckBox
                (
                    null,
                    ResGumps.AnimatedWaterEffect,
                    _currentProfile.AnimatedWaterEffect,
                    startX,
                    startY
                )
            );


            SettingsSection section5 = AddSettingsSection(box, "Shadows");
            section5.Y = section4.Bounds.Bottom + 40;

            section5.Add
            (
                _enableShadows = AddCheckBox
                (
                    null,
                    ResGumps.Shadows,
                    _currentProfile.ShadowsEnabled,
                    startX,
                    startY
                )
            );

            section5.PushIndent();

            section5.Add
            (
                _enableShadowsStatics = AddCheckBox
                (
                    null,
                    ResGumps.ShadowStatics,
                    _currentProfile.ShadowsStatics,
                    startX,
                    startY
                )
            );

            section5.PopIndent();

            section5.Add(AddLabel(null, ResGumps.TerrainShadowsLevel, startX, startY));
            section5.AddRight(_terrainShadowLevel = AddHSlider(null, Constants.MIN_TERRAIN_SHADOWS_LEVEL, Constants.MAX_TERRAIN_SHADOWS_LEVEL, _currentProfile.TerrainShadowsLevel, startX, startY, 200));

            Add(rightArea, PAGE);
        }

        private void BuildCommands()
        {
            const int PAGE = 4;

            ScrollArea rightArea = new ScrollArea(165, 20, WIDTH - 185, HEIGHT - 80, true);
            rightArea.ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways;

            int startX = 5;
            int startY = 5;

            NiceButton addButton = new NiceButton(startX, startY, 130, 25, ButtonAction.Activate, ResGumps.NewMacro)
            { IsSelectable = false, ButtonParameter = (int)Buttons.NewMacro };

            NiceButton delButton = new NiceButton(startX, startY + 30, 130, 25, ButtonAction.Activate, ResGumps.DeleteMacro)
            { IsSelectable = false, ButtonParameter = (int)Buttons.DeleteMacro };

            DataBox databox = new DataBox(startX, startY + 65, WIDTH - 230, 1);
            databox.WantUpdateSize = true;

            rightArea.Add(addButton);
            rightArea.Add(delButton);
            rightArea.Add(databox);


            addButton.MouseUp += (sender, e) =>
            {
                EntryDialog dialog = new EntryDialog
                (
                    250,
                    150,
                    ResGumps.MacroName,
                    name =>
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
                            nb = new NiceButton
                            (
                                0,
                                0,
                                130,
                                25,
                                ButtonAction.Activate,
                                name
                            )
                            {
                                ButtonParameter = (int)Buttons.Last + 1 + rightArea.Children.Count,
                                CanMove = true
                            }
                        );

                        databox.ReArrangeChildren();

                        nb.IsSelected = true;

                        _macroControl?.Dispose();

                        _macroControl = new MacroControl(name)
                        {
                            X = 350,
                            Y = 20
                        };

                        manager.PushToBack(_macroControl.Macro);

                        Add(_macroControl, PAGE);

                        nb.DragBegin += (sss, eee) =>
                        {
                            if (UIManager.DraggingControl != this || UIManager.MouseOverControl != sss)
                            {
                                return;
                            }

                            UIManager.Gumps.OfType<MacroButtonGump>().FirstOrDefault(s => s.TheMacro == _macroControl.Macro)?.Dispose();

                            MacroButtonGump macroButtonGump = new MacroButtonGump(_macroControl.Macro, Mouse.Position.X, Mouse.Position.Y);

                            macroButtonGump.X = Mouse.Position.X - (macroButtonGump.Width >> 1);
                            macroButtonGump.Y = Mouse.Position.Y - (macroButtonGump.Height >> 1);

                            UIManager.Add(macroButtonGump);

                            UIManager.AttemptDragControl(macroButtonGump, true);
                        };

                        nb.MouseUp += (sss, eee) =>
                        {
                            _macroControl?.Dispose();

                            _macroControl = new MacroControl(name)
                            {
                                X = 350,
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
                        ResGumps.MacroDeleteConfirmation,
                        b =>
                        {
                            if (!b)
                            {
                                return;
                            }

                            if (_macroControl != null)
                            {
                                UIManager.Gumps.OfType<MacroButtonGump>().FirstOrDefault(s => s.TheMacro == _macroControl.Macro)?.Dispose();

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

            for (Macro macro = (Macro)macroManager.Items; macro != null; macro = (Macro)macro.Next)
            {
                NiceButton nb;

                databox.Add
                (
                    nb = new NiceButton
                    (
                        0,
                        0,
                        130,
                        25,
                        ButtonAction.Activate,
                        macro.Name
                    )
                    {
                        ButtonParameter = (int)Buttons.Last + 1 + rightArea.Children.Count,
                        Tag = macro,
                        CanMove = true
                    }
                );

                nb.IsSelected = true;

                nb.DragBegin += (sss, eee) =>
                {
                    NiceButton mupNiceButton = (NiceButton)sss;

                    Macro m = mupNiceButton.Tag as Macro;

                    if (m == null)
                    {
                        return;
                    }

                    if (UIManager.DraggingControl != this || UIManager.MouseOverControl != sss)
                    {
                        return;
                    }

                    UIManager.Gumps.OfType<MacroButtonGump>().FirstOrDefault(s => s.TheMacro == m)?.Dispose();

                    MacroButtonGump macroButtonGump = new MacroButtonGump(m, Mouse.Position.X, Mouse.Position.Y);

                    macroButtonGump.X = Mouse.Position.X - (macroButtonGump.Width >> 1);
                    macroButtonGump.Y = Mouse.Position.Y - (macroButtonGump.Height >> 1);

                    UIManager.Add(macroButtonGump);

                    UIManager.AttemptDragControl(macroButtonGump, true);
                };

                nb.MouseUp += (sss, eee) =>
                {
                    NiceButton mupNiceButton = (NiceButton)sss;

                    Macro m = mupNiceButton.Tag as Macro;

                    if (m == null)
                    {
                        return;
                    }

                    _macroControl?.Dispose();

                    _macroControl = new MacroControl(m.Name)
                    {
                        X = 350,
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
            const int ROW = 28;
            const int INDENT = 45;

            ScrollArea rightArea = new ScrollArea
            (
                165,
                20,
                WIDTH - 185,
                HEIGHT - 80,
                true
            );
            rightArea.ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways;

            int startX = 5;
            int startY = 5;

            _use_tooltip = AddCheckBox(rightArea, ResGumps.UseTooltip, _currentProfile.UseTooltip, startX, startY);
            startY += ROW;

            startX = INDENT;
            Label text = AddLabel(rightArea, ResGumps.DelayBeforeDisplay, startX, startY);
            startX += text.Width + 5;
            _delay_before_display_tooltip = AddHSlider(rightArea, 0, 1000, _currentProfile.TooltipDelayBeforeDisplay, startX, startY, 200);
            startX = INDENT;
            startY += ROW;

            text = AddLabel(rightArea, ResGumps.TooltipZoom, startX, startY);
            startX += text.Width + 5;
            _tooltip_zoom = AddHSlider(rightArea, 100, 200, _currentProfile.TooltipDisplayZoom, startX, startY, 200);
            startX = INDENT;
            startY += ROW;

            text = AddLabel(rightArea, ResGumps.TooltipBackgroundOpacity, startX, startY);
            startX += text.Width + 5;
            _tooltip_background_opacity = AddHSlider(rightArea, 0, 100, _currentProfile.TooltipBackgroundOpacity, startX, startY, 200);
            startX = INDENT;
            startY += ROW;

            _tooltip_font_hue = AddHueDisplay(rightArea, startX, startY, _currentProfile.TooltipTextHue, ResGumps.TooltipFontHue);
            startY += ROW;
            startX = INDENT;

            text = AddLabel(rightArea, ResGumps.TooltipFont, startX, startY);
            startY += text.Height + 4;
            startX += 40;
            _tooltip_font_selector = new FontSelector(7, _currentProfile.TooltipFont, ResGumps.TooltipFontSelect) { X = startX, Y = startY };
            rightArea.Add(_tooltip_font_selector);
            startY += 180;
            startX = INDENT;

            _leftAlignToolTips = AddCheckBox(rightArea, "Align tooltips to the left side", _currentProfile.LeftAlignToolTips, startX, startY);
            startY += ROW;

            _forceCenterAlignMobileTooltips = AddCheckBox(rightArea, "Center align mobile name tooltips", _currentProfile.ForceCenterAlignTooltipMobiles, startX, startY);
            startY += ROW;

            text = AddLabel(rightArea, "Tooltip background hue", startX, startY);
            startY += text.Height + 4;
            rightArea.Add(_tooltipBGHue = new ModernColorPicker.HueDisplay(_currentProfile.ToolTipBGHue, null, true) { X = startX, Y = startY });
            startY += ROW;

            text = AddLabel(rightArea, "Tooltip header format (Item name)", startX, startY);
            startY += text.Height + 4;
            _tooltipHeaderFormat = AddInputField(null, startX, startY, 250, TEXTBOX_HEIGHT, null, 0, false, false, 50000);
            _tooltipHeaderFormat.SetText(_currentProfile.TooltipHeaderFormat);
            rightArea.Add(_tooltipHeaderFormat);
            startY += ROW;

            _enableTooltipOverride = AddCheckBox(rightArea, "Enable tooltip override (custom text per item)", _currentProfile.EnableTooltipOverride, startX, startY);
            startY += ROW;

            var ttipOverrideBtn = new NiceButton(startX, startY, 250, TEXTBOX_HEIGHT, ButtonAction.Activate, "Open tooltip override settings") { IsSelectable = false };
            ttipOverrideBtn.SetTooltip("Warning: This is an advanced feature.");
            ttipOverrideBtn.MouseUp += (s, e) => { UIManager.GetGump<ClassicUO.Dust765.UI.Gumps.ToolTipOverideMenu>()?.Dispose(); UIManager.Add(new ClassicUO.Dust765.UI.Gumps.ToolTipOverideMenu()); };
            rightArea.Add(ttipOverrideBtn);

            void SetTooltipOptionsEnabled(bool enabled)
            {
                for (int i = 2; i < rightArea.Children.Count; i++)
                {
                    var c = rightArea.Children[i];
                    c.AcceptMouseInput = enabled;
                    c.Alpha = enabled ? 1f : 0.5f;
                }
            }
            SetTooltipOptionsEnabled(_use_tooltip.IsChecked);
            _use_tooltip.ValueChanged += (s, e) => SetTooltipOptionsEnabled(_use_tooltip.IsChecked);

            Add(rightArea, PAGE);
        }

        private void BuildFonts()
        {
            const int PAGE = 6;

            ScrollArea rightArea = new ScrollArea
            (
                165,
                20,
                WIDTH - 185,
                HEIGHT - 80,
                true
            );
            rightArea.ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways;

            int startX = 5;
            int startY = 5;

            _overrideAllFonts = AddCheckBox
            (
                rightArea,
                ResGumps.OverrideGameFont,
                _currentProfile.OverrideAllFonts,
                startX,
                startY
            );

            startX += _overrideAllFonts.Width + 5;

            _overrideAllFontsIsUnicodeCheckbox = AddCombobox
            (
                rightArea,
                new[]
                {
                    ResGumps.ASCII, ResGumps.Unicode
                },
                _currentProfile.OverrideAllFontsIsUnicode ? 1 : 0,
                startX,
                startY,
                100
            );

            startX = 5;
            startY += _overrideAllFonts.Height + 2;

            _forceUnicodeJournal = AddCheckBox
            (
                rightArea,
                ResGumps.ForceUnicodeInJournal,
                _currentProfile.ForceUnicodeJournal,
                startX,
                startY
            );

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

            ScrollArea rightArea = new ScrollArea
            (
                165,
                20,
                WIDTH - 185,
                HEIGHT - 80,
                true
            );
            rightArea.ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways;

            int startX = 5;
            int startY = 5;

            _scaleSpeechDelay = AddCheckBox
            (
                rightArea,
                ResGumps.ScaleSpeechDelay,
                _currentProfile.ScaleSpeechDelay,
                startX,
                startY
            );

            startX += _scaleSpeechDelay.Width + 5;

            _sliderSpeechDelay = AddHSlider
            (
                rightArea,
                0,
                1000,
                _currentProfile.SpeechDelay,
                startX,
                startY,
                180
            );

            startX = 5;
            startY += _scaleSpeechDelay.Height + 2;

            _saveJournalCheckBox = AddCheckBox
            (
                rightArea,
                ResGumps.SaveJournalToFileInGameFolder,
                _currentProfile.SaveJournalToFile,
                startX,
                startY
            );

            startY += _saveJournalCheckBox.Height + 2;

            if (!_currentProfile.SaveJournalToFile)
            {
                World.Journal.CloseWriter();
            }

            _chatAfterEnter = AddCheckBox
            (
                rightArea,
                ResGumps.ActiveChatWhenPressingEnter,
                _currentProfile.ActivateChatAfterEnter,
                startX,
                startY
            );

            startX += 40;
            startY += _chatAfterEnter.Height + 2;

            _chatAdditionalButtonsCheckbox = AddCheckBox
            (
                rightArea,
                ResGumps.UseAdditionalButtonsToActivateChat,
                _currentProfile.ActivateChatAdditionalButtons,
                startX,
                startY
            );

            startY += _chatAdditionalButtonsCheckbox.Height + 2;

            _chatShiftEnterCheckbox = AddCheckBox
            (
                rightArea,
                ResGumps.UseShiftEnterToSendMessage,
                _currentProfile.ActivateChatShiftEnterSupport,
                startX,
                startY
            );

            startY += _chatShiftEnterCheckbox.Height + 2;
            startX = 5;

            _hideChatGradient = AddCheckBox
            (
                rightArea,
                ResGumps.HideChatGradient,
                _currentProfile.HideChatGradient,
                startX,
                startY
            );

            startY += _hideChatGradient.Height + 2;

            _ignoreGuildMessages = AddCheckBox
            (
                rightArea,
                ResGumps.IgnoreGuildMessages,
                _currentProfile.IgnoreGuildMessages,
                startX,
                startY
            );

            startY += _ignoreGuildMessages.Height + 2;

            _ignoreAllianceMessages = AddCheckBox
            (
                rightArea,
                ResGumps.IgnoreAllianceMessages,
                _currentProfile.IgnoreAllianceMessages,
                startX,
                startY
            );

            startY += 35;

            _randomizeColorsButton = new NiceButton
            (
                startX,
                startY,
                140,
                25,
                ButtonAction.Activate,
                ResGumps.RandomizeSpeechHues
            )
            { ButtonParameter = (int)Buttons.Disabled };

            _randomizeColorsButton.MouseUp += (sender, e) =>
            {
                if (e.Button != MouseButtonType.Left)
                {
                    return;
                }

                ushort speechHue = (ushort)RandomHelper.GetValue(2, 0x03b2);
                ushort emoteHue = (ushort)RandomHelper.GetValue(2, 0x03b2);
                ushort yellHue = (ushort)RandomHelper.GetValue(2, 0x03b2);
                ushort whisperHue = (ushort)RandomHelper.GetValue(2, 0x03b2);
                _currentProfile.SpeechHue = speechHue;
                _speechColorPickerBox.Hue = speechHue;
                _currentProfile.EmoteHue = emoteHue;
                _emoteColorPickerBox.Hue = emoteHue;
                _currentProfile.YellHue = yellHue;
                _yellColorPickerBox.Hue = yellHue;
                _currentProfile.WhisperHue = whisperHue;
                _whisperColorPickerBox.Hue = whisperHue;
            };

            rightArea.Add(_randomizeColorsButton);
            startY += _randomizeColorsButton.Height + 2 + 20;

            _speechColorPickerBox = AddHueDisplay
            (
                rightArea,
                startX,
                startY,
                _currentProfile.SpeechHue,
                ResGumps.SpeechColor
            );

            startX += 200;

            _emoteColorPickerBox = AddHueDisplay
            (
                rightArea,
                startX,
                startY,
                _currentProfile.EmoteHue,
                ResGumps.EmoteColor
            );

            startY += _emoteColorPickerBox.Height + 2;
            startX = 5;

            _yellColorPickerBox = AddHueDisplay
            (
                rightArea,
                startX,
                startY,
                _currentProfile.YellHue,
                ResGumps.YellColor
            );

            startX += 200;

            _whisperColorPickerBox = AddHueDisplay
            (
                rightArea,
                startX,
                startY,
                _currentProfile.WhisperHue,
                ResGumps.WhisperColor
            );

            startY += _whisperColorPickerBox.Height + 2;
            startX = 5;

            _partyMessageColorPickerBox = AddHueDisplay
            (
                rightArea,
                startX,
                startY,
                _currentProfile.PartyMessageHue,
                ResGumps.PartyMessageColor
            );

            startX += 200;

            _guildMessageColorPickerBox = AddHueDisplay
            (
                rightArea,
                startX,
                startY,
                _currentProfile.GuildMessageHue,
                ResGumps.GuildMessageColor
            );

            startY += _guildMessageColorPickerBox.Height + 2;
            startX = 5;

            _allyMessageColorPickerBox = AddHueDisplay
            (
                rightArea,
                startX,
                startY,
                _currentProfile.AllyMessageHue,
                ResGumps.AllianceMessageColor
            );

            startX += 200;

            _chatMessageColorPickerBox = AddHueDisplay
            (
                rightArea,
                startX,
                startY,
                _currentProfile.ChatMessageHue,
                ResGumps.ChatMessageColor
            );

            startY += _chatMessageColorPickerBox.Height + 2;
            startX = 5;

            Add(rightArea, PAGE);
        }

        private void BuildCombat()
        {
            const int PAGE = 8;

            ScrollArea rightArea = new ScrollArea
            (
                165,
                20,
                WIDTH - 185,
                HEIGHT - 80,
                true
            );
            rightArea.ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways;

            int startX = 5;
            int startY = 5;

            _holdDownKeyTab = AddCheckBox
            (
                rightArea,
                ResGumps.TabCombat,
                _currentProfile.HoldDownKeyTab,
                startX,
                startY
            );

            startY += _holdDownKeyTab.Height + 2;

            _queryBeforAttackCheckbox = AddCheckBox
            (
                rightArea,
                ResGumps.QueryAttack,
                _currentProfile.EnabledCriminalActionQuery,
                startX,
                startY
            );

            startY += _queryBeforAttackCheckbox.Height + 2;

            _queryBeforeBeneficialCheckbox = AddCheckBox
            (
                rightArea,
                ResGumps.QueryBeneficialActs,
                _currentProfile.EnabledBeneficialCriminalActionQuery,
                startX,
                startY
            );

            startY += _queryBeforeBeneficialCheckbox.Height + 2;

            _spellFormatCheckbox = AddCheckBox
            (
                rightArea,
                ResGumps.EnableOverheadSpellFormat,
                _currentProfile.EnabledSpellFormat,
                startX,
                startY
            );

            startY += _spellFormatCheckbox.Height + 2;

            _spellColoringCheckbox = AddCheckBox
            (
                rightArea,
                ResGumps.EnableOverheadSpellHue,
                _currentProfile.EnabledSpellHue,
                startX,
                startY
            );

            startY += _spellColoringCheckbox.Height + 2;

            _uiButtonsSingleClick = AddCheckBox
            (
                rightArea,
                ResGumps.UIButtonsSingleClick,
                _currentProfile.CastSpellsByOneClick,
                startX,
                startY
            );

            startY += _uiButtonsSingleClick.Height + 2;

            _buffBarTime = AddCheckBox
            (
                rightArea,
                ResGumps.ShowBuffDuration,
                _currentProfile.BuffBarTime,
                startX,
                startY
            );

            startY += _buffBarTime.Height + 2;

            _enableFastSpellsAssign = AddCheckBox
            (
                rightArea,
                ResGumps.EnableFastSpellsAssign,
                _currentProfile.FastSpellsAssign,
                startX,
                startY
            );

            startY += 30;

            int initialY = startY;

            _innocentColorPickerBox = AddHueDisplay
            (
                rightArea,
                startX,
                startY,
                _currentProfile.InnocentHue,
                ResGumps.InnocentColor
            );

            startY += _innocentColorPickerBox.Height + 2;

            _friendColorPickerBox = AddHueDisplay
            (
                rightArea,
                startX,
                startY,
                _currentProfile.FriendHue,
                ResGumps.FriendColor
            );

            startY += _innocentColorPickerBox.Height + 2;

            _crimialColorPickerBox = AddHueDisplay
            (
                rightArea,
                startX,
                startY,
                _currentProfile.CriminalHue,
                ResGumps.CriminalColor
            );

            startY += _innocentColorPickerBox.Height + 2;

            _canAttackColorPickerBox = AddHueDisplay
            (
                rightArea,
                startX,
                startY,
                _currentProfile.CanAttackHue,
                ResGumps.CanAttackColor
            );

            startY += _innocentColorPickerBox.Height + 2;

            _murdererColorPickerBox = AddHueDisplay
            (
                rightArea,
                startX,
                startY,
                _currentProfile.MurdererHue,
                ResGumps.MurdererColor
            );

            startY += _innocentColorPickerBox.Height + 2;

            _enemyColorPickerBox = AddHueDisplay
            (
                rightArea,
                startX,
                startY,
                _currentProfile.EnemyHue,
                ResGumps.EnemyColor
            );

            startY += _innocentColorPickerBox.Height + 2;

            startY = initialY;
            startX += 200;

            _beneficColorPickerBox = AddHueDisplay
            (
                rightArea,
                startX,
                startY,
                _currentProfile.BeneficHue,
                ResGumps.BeneficSpellHue
            );

            startY += _beneficColorPickerBox.Height + 2;

            _harmfulColorPickerBox = AddHueDisplay
            (
                rightArea,
                startX,
                startY,
                _currentProfile.HarmfulHue,
                ResGumps.HarmfulSpellHue
            );

            startY += _harmfulColorPickerBox.Height + 2;

            _neutralColorPickerBox = AddHueDisplay
            (
                rightArea,
                startX,
                startY,
                _currentProfile.NeutralHue,
                ResGumps.NeutralSpellHue
            );

            startY += _neutralColorPickerBox.Height + 2;

            startX = 5;
            startY += (_neutralColorPickerBox.Height + 2) * 4;

            _spellFormatBox = AddInputField
            (
                rightArea,
                startX,
                startY,
                200,
                TEXTBOX_HEIGHT,
                ResGumps.SpellOverheadFormat,
                0,
                true,
                false,
                30
            );

            _spellFormatBox.SetText(_currentProfile.SpellDisplayFormat);

            startX = 5;
            startY += (_spellFormatBox.Height * 2) + 2;
            Add(rightArea, PAGE);
        }

        private void BuildCounters()
        {
            const int PAGE = 9;

            ScrollArea rightArea = new ScrollArea
            (
                165,
                20,
                WIDTH - 185,
                HEIGHT - 80,
                true
            );

            int startX = 5;
            int startY = 5;


            _enableCounters = AddCheckBox
            (
                rightArea,
                ResGumps.EnableCounters,
                _currentProfile.CounterBarEnabled,
                startX,
                startY
            );

            startX += 40;
            startY += _enableCounters.Height + 2;

            _highlightOnUse = AddCheckBox
            (
                rightArea,
                ResGumps.HighlightOnUse,
                _currentProfile.CounterBarHighlightOnUse,
                startX,
                startY
            );

            startY += _highlightOnUse.Height + 2;

            _enableAbbreviatedAmount = AddCheckBox
            (
                rightArea,
                ResGumps.EnableAbbreviatedAmountCountrs,
                _currentProfile.CounterBarDisplayAbbreviatedAmount,
                startX,
                startY
            );

            startX += _enableAbbreviatedAmount.Width + 5;

            _abbreviatedAmount = AddInputField
            (
                rightArea,
                startX,
                startY,
                50,
                TEXTBOX_HEIGHT,
                null,
                50,
                false,
                true
            );

            _abbreviatedAmount.SetText(_currentProfile.CounterBarAbbreviatedAmount.ToString());

            startX = 5;
            startX += 40;
            startY += _enableAbbreviatedAmount.Height + 2;

            _highlightOnAmount = AddCheckBox
            (
                rightArea,
                ResGumps.HighlightRedWhenBelow,
                _currentProfile.CounterBarHighlightOnAmount,
                startX,
                startY
            );

            startX += _highlightOnAmount.Width + 5;

            _highlightAmount = AddInputField
            (
                rightArea,
                startX,
                startY,
                50,
                TEXTBOX_HEIGHT,
                null,
                50,
                false,
                true,
                999
            );

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

            _cellSize = AddHSlider
            (
                rightArea,
                30,
                80,
                _currentProfile.CounterBarCellSize,
                startX,
                startY,
                80
            );


            startX = initialX;
            startY += text.Height + 2 + 15;

            _rows = AddInputField
            (
                rightArea,
                startX,
                startY,
                50,
                30,
                ResGumps.Counter_Rows,
                50,
                false,
                true,
                30
            );

            _rows.SetText(_currentProfile.CounterBarRows.ToString());


            startX += _rows.Width + 5 + 100;

            _columns = AddInputField
            (
                rightArea,
                startX,
                startY,
                50,
                30,
                ResGumps.Counter_Columns,
                50,
                false,
                true,
                30
            );

            _columns.SetText(_currentProfile.CounterBarColumns.ToString());


            Add(rightArea, PAGE);
        }

        private void BuildExperimental()
        {
            const int PAGE = 12;

            ScrollArea rightArea = new ScrollArea
            (
                165,
                20,
                WIDTH - 185,
                HEIGHT - 80,
                true
            );
            rightArea.ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways;

            int startX = 5;
            int startY = 5;

            _disableDefaultHotkeys = AddCheckBox
            (
                rightArea,
                ResGumps.DisableDefaultUOHotkeys,
                _currentProfile.DisableDefaultHotkeys,
                startX,
                startY
            );

            startX += 40;
            startY += _disableDefaultHotkeys.Height + 2;

            _disableArrowBtn = AddCheckBox
            (
                rightArea,
                ResGumps.DisableArrowsPlayerMovement,
                _currentProfile.DisableArrowBtn,
                startX,
                startY
            );

            startY += _disableArrowBtn.Height + 2;

            _disableTabBtn = AddCheckBox
            (
                rightArea,
                ResGumps.DisableTab,
                _currentProfile.DisableTabBtn,
                startX,
                startY
            );

            startY += _disableTabBtn.Height + 2;

            _disableCtrlQWBtn = AddCheckBox
            (
                rightArea,
                ResGumps.DisableMessageHistory,
                _currentProfile.DisableCtrlQWBtn,
                startX,
                startY
            );

            startY += _disableCtrlQWBtn.Height + 2;

            _disableAutoMove = AddCheckBox
            (
                rightArea,
                ResGumps.DisableClickAutomove,
                _currentProfile.DisableAutoMove,
                startX,
                startY
            );

            startY += _disableAutoMove.Height + 2;

            Add(rightArea, PAGE);
        }

        private void BuildInfoBar()
        {
            const int PAGE = 10;

            ScrollArea rightArea = new ScrollArea
            (
                165,
                20,
                WIDTH - 185,
                HEIGHT - 80,
                true
            );
            rightArea.ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways;

            int startX = 5;
            int startY = 5;

            _showInfoBar = AddCheckBox
            (
                rightArea,
                ResGumps.ShowInfoBar,
                _currentProfile.ShowInfoBar,
                startX,
                startY
            );

            startX += 40;
            startY += _showInfoBar.Height + 2;

            _showHPInTitleBar = AddCheckBox
            (
                rightArea,
                ResGumps.ShowHPInTitleBar,
                _currentProfile.ShowHPInTitleBar,
                startX,
                startY
            );

            startX += 40;
            startY += _showHPInTitleBar.Height + 2;

            Label text = AddLabel(rightArea, ResGumps.DataHighlightType, startX, startY);

            startX += text.Width + 5;

            _infoBarHighlightType = AddCombobox
            (
                rightArea,
                new[] { ResGumps.TextColor, ResGumps.ColoredBars },
                _currentProfile.InfoBarHighlightType,
                startX,
                startY,
                150
            );

            startX = 5;
            startY += _infoBarHighlightType.Height + 5;

            NiceButton nb = new NiceButton
            (
                startX,
                startY,
                90,
                20,
                ButtonAction.Activate,
                ResGumps.AddItem,
                0,
                TEXT_ALIGN_TYPE.TS_LEFT
            )
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

            rightArea.Add
            (
                new Line
                (
                    startX,
                    startY,
                    rightArea.Width,
                    1,
                    Color.Gray.PackedValue
                )
            );

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

            ScrollArea rightArea = new ScrollArea
            (
                165,
                20,
                WIDTH - 185,
                HEIGHT - 80,
                true
            );

            int startX = 5;
            int startY = 5;
            Label text;

            bool hasBackpacks = Client.Version >= ClientVersion.CV_705301;

            if (hasBackpacks)
            {
                text = AddLabel(rightArea, ResGumps.BackpackStyle, startX, startY);
                startX += text.Width + 5;
            }

            _backpackStyle = AddCombobox
            (
                rightArea,
                new[]
                {
                    ResGumps.BackpackStyle_Default, ResGumps.BackpackStyle_Suede,
                    ResGumps.BackpackStyle_PolarBear, ResGumps.BackpackStyle_GhoulSkin
                },
                _currentProfile.BackpackStyle,
                startX,
                startY,
                200
            );

            _backpackStyle.IsVisible = hasBackpacks;

            if (hasBackpacks)
            {
                startX = 5;
                startY += _backpackStyle.Height + 2 + 10;
            }

            text = AddLabel(rightArea, ResGumps.ContainerScale, startX, startY);
            startX += text.Width + 5;

            _containersScale = AddHSlider
            (
                rightArea,
                Constants.MIN_CONTAINER_SIZE_PERC,
                Constants.MAX_CONTAINER_SIZE_PERC,
                _currentProfile.ContainersScale,
                startX,
                startY,
                200
            );

            startX = 5;
            startY += _containersScale.Height + 2;

            _containerScaleItems = AddCheckBox
            (
                rightArea,
                ResGumps.ScaleItemsInsideContainers,
                _currentProfile.ScaleItemsInsideContainers,
                startX,
                startY
            );
            startY += _containerScaleItems.Height + 2;

            _useLargeContianersGumps = AddCheckBox
            (
                rightArea,
                ResGumps.UseLargeContainersGump,
                _currentProfile.UseLargeContainerGumps,
                startX,
                startY
            );
            _useLargeContianersGumps.IsVisible = Client.Version >= ClientVersion.CV_706000;

            if (_useLargeContianersGumps.IsVisible)
            {
                startY += _useLargeContianersGumps.Height + 2;
            }

            _containerDoubleClickToLoot = AddCheckBox
            (
                rightArea,
                ResGumps.DoubleClickLootContainers,
                _currentProfile.DoubleClickToLootInsideContainers,
                startX,
                startY
            );

            startY += _containerDoubleClickToLoot.Height + 2;

            _relativeDragAnDropItems = AddCheckBox
            (
                rightArea,
                ResGumps.RelativeDragAndDropContainers,
                _currentProfile.RelativeDragAndDropItems,
                startX,
                startY
            );

            startY += _relativeDragAnDropItems.Height + 2;

            _highlightContainersWhenMouseIsOver = AddCheckBox
            (
                rightArea,
                ResGumps.HighlightContainerWhenSelected,
                _currentProfile.HighlightContainerWhenSelected,
                startX,
                startY
            );

            startY += _highlightContainersWhenMouseIsOver.Height + 2;

            _hueContainerGumps = AddCheckBox
            (
                rightArea,
                ResGumps.HueContainerGumps,
                _currentProfile.HueContainerGumps,
                startX,
                startY
            );

            startY += _hueContainerGumps.Height + 2;

            _overrideContainerLocation = AddCheckBox
            (
                rightArea,
                ResGumps.OverrideContainerGumpLocation,
                _currentProfile.OverrideContainerLocation,
                startX,
                startY
            );

            startX += _overrideContainerLocation.Width + 5;

            _overrideContainerLocationSetting = AddCombobox
            (
                rightArea,
                new[]
                {
                    ResGumps.ContLoc_NearContainerPosition, ResGumps.ContLoc_TopRight,
                    ResGumps.ContLoc_LastDraggedPosition, ResGumps.ContLoc_RememberEveryContainer
                },
                _currentProfile.OverrideContainerLocationSetting,
                startX,
                startY,
                200
            );

            startX = 5;
            startY += _overrideContainerLocation.Height + 2 + 10;

            NiceButton button = new NiceButton
            (
                startX,
                startY,
                130,
                30,
                ButtonAction.Activate,
                ResGumps.RebuildContainers
            )
            {
                ButtonParameter = -1,
                IsSelectable = true,
                IsSelected = true
            };

            button.MouseUp += (sender, e) => { ContainerManager.BuildContainerFile(true); };
            rightArea.Add(button);

            startX = 5;
            startY += button.Height + 2;

            Add(rightArea, PAGE);
        }

        private void BuildNameOverhead()
        {
            const int PAGE = 13;
            int topButtonY = 52 + 25 + 4;
            int bottomSeparatorY = HEIGHT - 51;

            ScrollArea rightArea = new ScrollArea
            (
                165,
                topButtonY,
                150,
                bottomSeparatorY - topButtonY,
                true
            );

            Add
            (
                new Line
                (
                    165,
                    52 + 25 + 2,
                    WIDTH - 165,
                    1,
                    Color.Gray.PackedValue
                ),
                PAGE
            );

            Add
            (
                new Line
                (
                    165 + 150,
                    21,
                    1,
                    bottomSeparatorY - 21,
                    Color.Gray.PackedValue
                ),
                PAGE
            );

            NiceButton addButton = new NiceButton
            (
                165,
                20,
                130,
                20,
                ButtonAction.Activate,
                "New entry"
            )
            { IsSelectable = false, ButtonParameter = (int)Buttons.NewNameOverheadEntry };

            Add(addButton, PAGE);

            NiceButton delButton = new NiceButton
            (
                165,
                52,
                130,
                20,
                ButtonAction.Activate,
                "Delete entry"
            )
            { IsSelectable = false, ButtonParameter = (int)Buttons.DeleteOverheadEntry };

            Add(delButton, PAGE);


            int startX = 5;
            int startY = 5;

            DataBox databox = new DataBox(startX, startY, 1, 1);
            databox.WantUpdateSize = true;
            rightArea.Add(databox);


            addButton.MouseUp += (sender, e) =>
            {
                EntryDialog dialog = new
                (
                    250,
                    150,
                    "Name overhead entry name",
                    name =>
                    {
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            return;
                        }
                        if (NameOverHeadManager.FindOption(name) != null)
                        {
                            return;
                        }
                        NiceButton nb;
                        databox.Add
                        (
                            nb = new NiceButton
                            (
                                0,
                                0,
                                130,
                                25,
                                ButtonAction.Activate,
                                name
                            )
                            {
                                ButtonParameter = (int)Buttons.Last + 1 + rightArea.Children.Count
                            }
                        );
                        databox.ReArrangeChildren();
                        nb.IsSelected = true;
                        _nameOverheadControl?.Dispose();
                        var option = new NameOverheadOption(name);
                        NameOverHeadManager.AddOption(option);
                        _nameOverheadControl = new NameOverheadAssignControl(option)
                        {
                            X = 325,
                            Y = 20
                        };
                        Add(_nameOverheadControl, PAGE);
                        nb.MouseUp += (sss, eee) =>
                        {
                            _nameOverheadControl?.Dispose();
                            _nameOverheadControl = new NameOverheadAssignControl(option)
                            {
                                X = 325,
                                Y = 20
                            };
                            Add(_nameOverheadControl, PAGE);
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
                        ResGumps.MacroDeleteConfirmation,
                        b =>
                        {
                            if (!b)
                            {
                                return;
                            }
                            if (_nameOverheadControl != null)
                            {
                                NameOverHeadManager.RemoveOption(_nameOverheadControl.Option);
                                _nameOverheadControl.Dispose();
                            }
                            nb.Dispose();
                            databox.ReArrangeChildren();
                        }
                    );
                    UIManager.Add(dialog);
                }
            };


            foreach (var option in NameOverHeadManager.GetAllOptions())
            {
                NiceButton nb;

                databox.Add
                (
                    nb = new NiceButton
                    (
                        0,
                        0,
                        130,
                        25,
                        ButtonAction.Activate,
                        option.Name
                    )
                    {
                        ButtonParameter = (int)Buttons.Last + 1 + rightArea.Children.Count,
                        Tag = option
                    }
                );

                nb.IsSelected = true;

                nb.MouseUp += (sss, eee) =>
                {
                    NiceButton mupNiceButton = (NiceButton)sss;
                    var option = mupNiceButton.Tag as NameOverheadOption;
                    if (option == null)
                    {
                        return;
                    }
                    _nameOverheadControl?.Dispose();
                    _nameOverheadControl = new NameOverheadAssignControl(option)
                    {
                        X = 325,
                        Y = 20
                    };
                    Add(_nameOverheadControl, PAGE);
                };
            }

            databox.ReArrangeChildren();

            Add(rightArea, PAGE);
        }

        private void BuildMods()
        {
            const int PAGE = 18;
            // ## BEGIN - END ## // TAZUO
            //ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);
            // ## BEGIN - END ## // TAZUO
            ScrollArea rightArea = new ScrollArea(165, 20, WIDTH - 185, HEIGHT - 80, true);
            rightArea.ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways;
            // ## BEGIN - END ## // TAZUO

            int startX = 5;
            int startY = 5;

            DataBox box = new DataBox(startX, startY, rightArea.Width - 15, 1);
            box.WantUpdateSize = true;
            rightArea.Add(box);

            // ## BEGIN - END ## // UI/GUMPS
            SettingsSection section = AddSettingsSection(box, "-----UI / Gumps-----");

            section.Add(_uccEnableLTBar = AddCheckBox(null, "Enable UCC - LastTarget Bar", _currentProfile.UOClassicCombatLTBar, startX, startY));
            startY += _uccEnableLTBar.Height + 2;
            section.Add(AddLabel(null, "(Doubleklick to lock in place)", startX, startY));

            section.Add(_bandageGump = AddCheckBox(null, "Show gump when using bandages", _currentProfile.BandageGump, startX, startY));
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
                    5000
                )
            );
            _bandageGumpOffsetX.SetText(_currentProfile.BandageGumpOffset.X.ToString());
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
                    5000
                )
            );
            _bandageGumpOffsetY.SetText(_currentProfile.BandageGumpOffset.Y.ToString());

            startY += _bandageGumpOffsetY.Height + 2;
            section.AddRight(AddLabel(null, "Y", 0, 0), 2);

            section.Add(_bandageUpDownToggle = AddCheckBox(null, "count up or down", _currentProfile.BandageGumpUpDownToggle, startX, startY));
            startY += _bandageGumpOffsetY.Height + 2;
            // ## BEGIN - END ## // UI/GUMPS
            // ## BEGIN - END ## // ONCASTINGGUMP
            section.Add(_onCastingGump = AddCheckBox(null, "OnCasting gump (anti-rubberbanding) on mouse", _currentProfile.OnCastingGump, startX, startY));
            startY += _highlightContainersWhenMouseIsOver.Height + 2;
            section.Add(_onCastingGump_hidden = AddCheckBox(null, "hide the gump", _currentProfile.OnCastingGump_hidden, startX, startY));
            startY += _highlightContainersWhenMouseIsOver.Height + 2;
            section.Add(_onCastingUnderPlayerBar = AddCheckBox(null, "show cast bar under player (4px)", _currentProfile.OnCastingUnderPlayerBar, startX, startY));
            startY += _highlightContainersWhenMouseIsOver.Height + 2;
            // ## BEGIN - END ## // ONCASTINGGUMP
            // ## BEGIN - END ## // VISUALRESPONSEMANAGER
            section.Add(_visualResponseManager = AddCheckBox(null, "Visual response manager ON / OFF", _currentProfile.VisualResponseManager, startX, startY));
            startY += _visualResponseManager.Height + 2;
            // ## BEGIN - END ## // VISUALRESPONSEMANAGER
            // ## BEGIN - END ## // LINES
            SettingsSection section3 = AddSettingsSection(box, "-----LINES (LINES UI)-----");
            section3.Y = section.Bounds.Bottom + 40;

            startY = section.Bounds.Bottom + 40;

            section3.Add(_uccEnableLines = AddCheckBox(null, "Enable UCC - Lines", _currentProfile.UOClassicCombatLines, startX, startY));
            startY += _uccEnableLines.Height + 2;
            // ## BEGIN - END ## // LINES
            // ## BEGIN - END ## // AUTOLOOT
            SettingsSection section4 = AddSettingsSection(box, "-----AUTOLOOT (AL UI)-----");
            section4.Y = section3.Bounds.Bottom + 40;

            startY = section3.Bounds.Bottom + 40;

            section4.Add(_uccEnableAL = AddCheckBox(null, "Enable UCC - AL", _currentProfile.UOClassicCombatAL, startX, startY));
            startY += _uccEnableAL.Height + 2;
            section4.Add(_uccEnableGridLootColoring = AddCheckBox(null, "Enable GridLootColoring", _currentProfile.UOClassicCombatAL_EnableGridLootColoring, startX, startY));
            startY += _uccEnableGridLootColoring.Height + 2;
            section4.Add(_uccBEnableLootAboveID = AddCheckBox(null, "Enable LootAboveID", _currentProfile.UOClassicCombatAL_EnableLootAboveID, startX, startY));
            startY += _uccBEnableLootAboveID.Height + 2;

            SettingsSection section6 = AddSettingsSection(box, "-----BUFFBAR (COOLDOWN UI)-----");
            section6.Y = section4.Bounds.Bottom + 40;

            startY = section4.Bounds.Bottom + 40;

            section6.Add(_uccEnableBuffbar = AddCheckBox(null, "Enable UCC - Buffbar", _currentProfile.UOClassicCombatBuffbar, startX, startY));
            startY += _uccEnableBuffbar.Height + 2;

            section6.Add(_uccEnableSelf = AddCheckBox(null, "Enable UCC - Self", _currentProfile.UOClassicCombatSelf, startX, startY));
            startY += _uccEnableSelf.Height + 2;

            section6.Add(AddLabel(null, "-----DISABLE / ENABLE BUFFBAR ON CHANGES BELOW-----", startX, startY));

            section6.Add(_uccSwing = AddCheckBox(null, "Show Swing Line", _currentProfile.UOClassicCombatBuffbar_SwingEnabled, startX, startY));
            startY += _uccSwing.Height + 2;
            section6.Add(_uccDoD = AddCheckBox(null, "Show Do Disarm Line", _currentProfile.UOClassicCombatBuffbar_DoDEnabled, startX, startY));
            startY += _uccDoD.Height + 2;
            section6.Add(_uccGotD = AddCheckBox(null, "Show Got Disarmed Line", _currentProfile.UOClassicCombatBuffbar_GotDEnabled, startX, startY));
            startY += _uccGotD.Height + 2;
            section6.Add(_uccLocked = AddCheckBox(null, "Lock in place", _currentProfile.UOClassicCombatBuffbar_Locked, startX, startY));
            startY += _uccLocked.Height + 2;
            // ## BEGIN - END ## // BUFFBAR/UCCSETTINGS
            // ## BEGIN - END ## // BUFFBAR/UCCSETTINGS
            SettingsSection section7 = AddSettingsSection(box, "-----SETTINGS (BUFFBAR AND SELF)-----");
            section7.Y = section6.Bounds.Bottom + 40;

            startY = section6.Bounds.Bottom + 40;

            section7.Add(AddLabel(null, "General cooldown when you get disarmed (ms)", startX, startY));

            section7.Add
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
                    50000
                )
            );
            _uccDisarmedCooldown.SetText(_currentProfile.UOClassicCombatSelf_DisarmedCooldown.ToString());

            section7.Add(AddLabel(null, "Cooldown after successfull disarm (ms)", startX, startY));

            section7.Add
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
                    90000
                )
            );
            _uccDisarmStrikeCooldown.SetText(_currentProfile.UOClassicCombatSelf_DisarmStrikeCooldown.ToString());

            section7.Add(AddLabel(null, "Cooldown after failed disarm (ms)", startX, startY));

            section7.Add
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
                    50000
                )
            );
            _uccDisarmAttemptCooldown.SetText(_currentProfile.UOClassicCombatSelf_DisarmAttemptCooldown.ToString());
            // ## BEGIN - END ## // BUFFBAR/UCCSETTINGS
            //TRESHOLD SETTINGS
            SettingsSection section10 = AddSettingsSection(box, "-----SETTINGS (TRESHOLDS)-----");
            section10.Y = section7.Bounds.Bottom + 40;

            startY = section7.Bounds.Bottom + 40;

            section10.Add(AddLabel(null, "Bandies treshold (diffhits >= ):", startX, startY));

            section10.Add
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
                    90000
                )
            );
            _uccBandiesHPTreshold.SetText(_currentProfile.UOClassicCombatSelf_BandiesHPTreshold.ToString());

            section10.Add(_uccBandiesPoison = AddCheckBox(null, "Bandies when poisoned", _currentProfile.UOClassicCombatSelf_BandiesPoison, startX, startY));
            startY += _uccBandiesPoison.Height + 2;

            section10.Add(AddLabel(null, "Curepot HP treshold (diffhits >= ): ", startX, startY));

            section10.Add
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
                    90000
                )
            );
            _uccCurepotHPTreshold.SetText(_currentProfile.UOClassicCombatSelf_CurepotHPTreshold.ToString());

            section10.Add(AddLabel(null, " HP treshold (diffhits >= ): ", startX, startY));

            section10.Add
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
                    90000
                )
            );
            _uccHealpotHPTreshold.SetText(_currentProfile.UOClassicCombatSelf_HealpotHPTreshold.ToString());

            section10.Add(AddLabel(null, "Refreshpot Stam treshold (diffstam >= ): ", startX, startY));

            section10.Add
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
                    90000
                )
            );
            _uccRefreshpotStamTreshold.SetText(_currentProfile.UOClassicCombatSelf_RefreshpotStamTreshold.ToString());

            //MISC SETTINGS AREA
            SettingsSection section11 = AddSettingsSection(box, "-----SETTINGS (MISC)-----");
            section11.Y = section10.Bounds.Bottom + 40;

            startY = section10.Bounds.Bottom + 40;

            section11.Add(AddLabel(null, "Auto rearm weps held before got disarmeded (ms)", startX, startY));

            section11.Add
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
                    90000
                )
            );
            _uccAutoRearmAfterDisarmedCooldown.SetText(_currentProfile.UOClassicCombatSelf_AutoRearmAfterDisarmedCooldown.ToString());

            section11.Add(_uccClilocTrigger = AddCheckBox(null, "Use Cliloc Triggers (up time on cliloc and uoc hotkey)", _currentProfile.UOClassicCombatSelf_ClilocTriggers, startX, startY));
            startY += _uccClilocTrigger.Height + 2;

            section11.Add(_uccMacroTrigger = AddCheckBox(null, "Use Macro Triggers (change uoc hotkey to disarm / pot / rearm by ucc)", _currentProfile.UOClassicCombatSelf_MacroTriggers, startX, startY));
            startY += _uccMacroTrigger.Height + 2;

            section11.Add(AddLabel(null, "Strength Pot Cooldown (ms)", startX, startY));
            section11.Add
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
                    190000
                )
            );
            _uccStrengthPotCooldown.SetText(_currentProfile.UOClassicCombatSelf_StrengthPotCooldown.ToString());

            section11.Add(AddLabel(null, "Agility Pot Cooldown (ms)", startX, startY));
            section11.Add
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
                    190000
                )
            );
            _uccDexPotCooldown.SetText(_currentProfile.UOClassicCombatSelf_DexPotCooldown.ToString());

            section11.Add(AddLabel(null, "Min RNG (ms)", startX, startY));

            section11.Add
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
                    90000
                )
            );
            _uccRNGMin.SetText(_currentProfile.UOClassicCombatSelf_MinRNG.ToString());

            section11.Add(AddLabel(null, "Max RNG (ms)", startX, startY));

            section11.Add
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
                    90000
                )
            );
            _uccRNGMax.SetText(_currentProfile.UOClassicCombatSelf_MaxRNG.ToString());
            // ## BEGIN - END ## // SELF
            // ## BEGIN - END ## // TABGRID // PKRION
            //TABGRID SETTINGS AREA
            SettingsSection section12 = AddSettingsSection(box, "-----TABGRID-----");
            section12.Y = section11.Bounds.Bottom + 40;

            startY = section11.Bounds.Bottom + 40;

            section12.Add(_enableTabGridGump = AddCheckBox(null, ResGumps.enableTabGridGump, _currentProfile.TabGridGumpEnabled, startX, startY));
            startY += _enableTabGridGump.Height + 2;

            section12.Add(AddLabel(null, "Grid Rows", startX, startY));
            section12.AddRight
            (
                _rowsGrid = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    30,//TEXTBOX_HEIGHT,
                    null,
                    50,
                    false,
                    true,
                    30
                )
            );
            _rowsGrid.SetText(_currentProfile.GridRows.ToString());

            section12.Add(AddLabel(null, "Number of Tabs", startX, startY));
            section12.AddRight
            (
                _tabsGrid = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    30,//TEXTBOX_HEIGHT,
                    null,
                    50,
                    false,
                    true,
                    30
                )
            );
            _tabsGrid.SetText(_currentProfile.GridTabs.ToString());

            section12.Add(AddLabel(null, "List of Tab Names using this format", startX, startY));
            section12.Add
            (
                _tablistBox = AddInputField
                (
                    null,
                    startX, startY,
                    400,
                    TEXTBOX_HEIGHT,
                    null,
                    0,
                    true,
                    false,
                    60
                )
            );
            _tablistBox.SetText(_currentProfile.TabList.ToString());
            // ## BEGIN - END ## // TABGRID // PKRION

            Add(rightArea, PAGE);
        }
        // ## BEGIN - END ## // BASICSETUP

        private void BuildCooldowns()
        {
            const int PAGE = 8787;

            ScrollArea rightArea = new ScrollArea
            (
                165,
                20,
                WIDTH - 185,
                HEIGHT - 80,
                true
            );


            SettingsSection _coolDowns = new SettingsSection("Cooldown bars", rightArea.Width);

            {
                _coolDowns.Add(AddLabel(null, "Position", 0, 0));
                _coolDowns.AddRight(_coolDownX = AddInputField(
                        null, 0, 0,
                        50, TEXTBOX_HEIGHT,
                        numbersOnly: true
                    ));
                _coolDownX.SetText(_currentProfile.CoolDownX.ToString());

                _coolDowns.AddRight(_coolDownY = AddInputField(
                        null, 0, 0,
                        50, TEXTBOX_HEIGHT,
                        numbersOnly: true
                    ));
                _coolDownY.SetText(_currentProfile.CoolDownY.ToString());

                _coolDowns.Add(_uselastCooldownPosition = AddCheckBox(null, "Use last moved bar position", _currentProfile.UseLastMovedCooldownPosition, 0, 0));

            }//Cooldown position
            rightArea.Add(_coolDowns);

            #region Cooldown conditions
            SettingsSection conditions = new SettingsSection("Condition", rightArea.Width + 10);
            conditions.Y = _coolDowns.Y + _coolDowns.Height + 5;

            {
                NiceButton _button;
                conditions.Add(_button = new NiceButton(
                        0, 0,
                        100, TEXTBOX_HEIGHT,
                        ButtonAction.Activate,
                        "+ Condition"
                    ));
                _button.IsSelectable = false;
                _button.MouseUp += (sender, e) =>
                {
                    conditions.Add(GenConditionControl(_currentProfile.CoolDownConditionCount, WIDTH - 240, true));
                };

                int count = _currentProfile.CoolDownConditionCount;
                for (int i = 0; i < count; i++)
                {
                    conditions.Add(GenConditionControl(i, WIDTH - 240, false));
                }
            } //Add condition

            rightArea.Add(conditions);
            #endregion

            Add(rightArea, PAGE);
        }

        private void BuildGridContainer()
        {
            const int PAGE = 8788;
            ScrollArea rightArea = new ScrollArea(165, 20, WIDTH - 185, HEIGHT - 80, true);
            rightArea.ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways;

            SettingsSection gridSection = new SettingsSection("Grid Containers", rightArea.Width);

                gridSection.Add(_useGridLayoutContainerGumps = AddCheckBox(
                        null,
                        "Use grid containers",
                        _currentProfile.UseGridLayoutContainerGumps,
                        0,
                        0
                    ));

                gridSection.Add(AddLabel(null, "Grid container scale", 0, 0));

                    gridSection.AddRight(_gridContainerScale = AddHSlider(
                            null,
                            50, 200,
                            _currentProfile.GridContainersScale,
                            0, 0,
                            200
                        ));

                gridSection.PushIndent();

                    gridSection.Add(_gridContainerItemScale = AddCheckBox(
                        null,
                        "",
                        _currentProfile.GridContainerScaleItems,
                        0, 0
                        ));

                    gridSection.AddRight(AddLabel(null, "Also scale items", 0, 0));

                    gridSection.PopIndent();

                gridSection.Add(AddLabel(null, "Border opacity", 0, 0));
                    gridSection.AddRight
                        (
                            _gridBorderOpacity = AddHSlider
                            (
                                null,
                                0,
                                100,
                                _currentProfile.GridBorderAlpha,
                                0,
                                0,
                                200
                            )
                        );

                gridSection.PushIndent();
                gridSection.Add
                    (
                     _gridBorderHue = new ModernColorPicker.HueDisplay(_currentProfile.GridBorderHue, null, true)
                    );
                gridSection.AddRight(AddLabel(null, "Border hue", 0, 0));
                gridSection.PopIndent();

                gridSection.Add(AddLabel(null, "Background opacity", 0, 0));
                    gridSection.AddRight(_containerOpacity = AddHSlider
                    (
                        null,
                        0,
                        100,
                        _currentProfile.ContainerOpacity,
                        0,
                        0,
                        200
                    ));

                gridSection.PushIndent();
                gridSection.Add(_altGridContainerBackgroundHue = new ModernColorPicker.HueDisplay(_currentProfile.AltGridContainerBackgroundHue, null, true));
                gridSection.AddRight(AddLabel(null, "Background hue", 0, 0));
                gridSection.PopIndent();

                gridSection.PushIndent();
                gridSection.Add(_gridOverrideWithContainerHue = AddCheckBox(null, "Override hue with the container's hue", _currentProfile.Grid_UseContainerHue, 0, 0));
                gridSection.PopIndent();

                gridSection.Add(
                            AddLabel(null, "Search Style", 0, 0)
                        );

                    gridSection.AddRight(
                        _gridContainerSearchAlternative = AddCombobox(
                                null,
                                new string[] {
                                "Only show",
                                "Highlight"
                                },
                                _currentProfile.GridContainerSearchMode,
                                0,
                                0,
                                200
                            )
                        );

                gridSection.Add(_gridContainerPreview = AddCheckBox(
                            null,
                            "Enable container preview",
                            _currentProfile.GridEnableContPreview,
                            0,
                            0
                        ));
                    _gridContainerPreview.SetTooltip("This only works on containers that you have opened, otherwise the client does not have that information yet.");

                gridSection.Add(_gridContainerAnchorable = AddCheckBox(
                            null, "Make anchorable",
                            _currentProfile.EnableGridContainerAnchor,
                            0, 0
                        ));
                    _gridContainerAnchorable.SetTooltip("This will allow grid containers to be anchored to other containers/world map/journal");

                gridSection.Add(AddLabel(null, "Container Style", 0, 0));

                    gridSection.AddRight(_gridBorderStyle = AddCombobox(
                            null,
                            Enum.GetNames(typeof(GridContainer.BorderStyle)),
                            _currentProfile.Grid_BorderStyle,
                            0, 0,
                            200
                        ));

                gridSection.Add(AddLabel(null, "Hide border around gump", 0, 0));
                gridSection.Add(_gridHideBorder = AddCheckBox(null, "", _currentProfile.Grid_HideBorder, 0, 0));

                gridSection.Add(AddLabel(null, "Default grid rows x columns", 0, 0));
                    gridSection.AddRight(_gridDefaultRows = AddInputField(null, 0, 0, 25, TEXTBOX_HEIGHT, null, 0, false, true));
                    _gridDefaultRows.SetText(_currentProfile.Grid_DefaultRows.ToString());
                    gridSection.AddRight(_gridDefaultColumns = AddInputField(null, 0, 0, 25, TEXTBOX_HEIGHT, null, 0, false, true));
                    _gridDefaultColumns.SetText(_currentProfile.Grid_DefaultColumns.ToString());

                NiceButton _;
                gridSection.Add(_ = new NiceButton(X, 0, 150, TEXTBOX_HEIGHT, ButtonAction.Activate, "Grid highlight settings"));
                    _.DisplayBorder = true;
                    _.IsSelectable = false;
                    _.MouseUp += (s, e) =>
                    {
                        UIManager.GetGump<ClassicUO.Dust765.UI.Gumps.GridHightlightMenu>()?.Dispose();
                        UIManager.Add(new ClassicUO.Dust765.UI.Gumps.GridHightlightMenu());
                    };

                gridSection.Add(AddLabel(null, "Grid highlight line size", 0, 0));
                gridSection.AddRight(_gridHightlightLineSize = AddHSlider(null, 1, 10, _currentProfile.GridHightlightSize, 0, 0, 150));

            rightArea.Add(gridSection);
            Add(rightArea, PAGE);
        }


        private void MinimizeSectionMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtonType.Left)
            {
                if (sender is NiceButton)
                {
                    SettingsSection section = ((NiceButton)sender).Parent as SettingsSection;

                    ScrollArea rightArea = section.Parent as ScrollArea;

                    int diff = section.Height - 25;
                    if (section.Children[2].IsVisible)
                        diff = -section.Height + 25;

                    for (int i = rightArea.Children.IndexOf(section) + 1; i < rightArea.Children.Count; i++)
                        if (rightArea.Children[i] != section)
                            rightArea.Children[i].Y += diff;
                    section.Children[2].IsVisible = !section.Children[2].IsVisible;
                }
            }
        }

        public Combobox GenerateFontSelector(byte selectedFont)
        {
            var fontLabels = Enumerable.Range(0, 20).Select(i => $"Font {i}").ToArray();
            int idx = Math.Max(0, Math.Min(19, (int)selectedFont));
            return AddCombobox(null, fontLabels, idx, 0, 0, 200);
        }

        public Control GenConditionControl(int key, int width, bool createIfNotExists)
        {
            CoolDownBar.CoolDownConditionData data = CoolDownBar.CoolDownConditionData.GetConditionData(key, createIfNotExists);
            Area main = new Area();
            main.Width = width;
            main.Height = 60;

            AlphaBlendControl _background = new AlphaBlendControl();
            _background.Width = width;
            _background.Height = main.Height;
            main.Add(_background);


            NiceButton _delete = new NiceButton(1, 1, 22, TEXTBOX_HEIGHT, ButtonAction.Activate, "X");
            _delete.SetTooltip("Delete this cooldown bar");
            _delete.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    CoolDownBar.CoolDownConditionData.RemoveCondition(key);
                    main.Dispose();
                }
            };
            main.Add(_delete);


            Label _hueLabel = new Label("Hue:", true, HUE_FONT, 25, FONT);
            _hueLabel.X = _delete.X + _delete.Width + 5;
            _hueLabel.Y = 1;
            main.Add(_hueLabel);

            var _hueSelector = new ModernColorPicker.HueDisplay(data.hue, null, true) { X = _hueLabel.X + _hueLabel.Width + 2, Y = 1 };
            main.Add(_hueSelector);

            InputField _name = AddInputField(null, _hueSelector.X + _hueSelector.Width + 10, 1, 100, TEXTBOX_HEIGHT);
            _name.SetText(data.label);
            main.Add(_name);



            Label _cooldownLabel = new Label("Cooldown:", true, HUE_FONT, 52, FONT);
            _cooldownLabel.X = _name.X + _name.Width + 5;
            _cooldownLabel.Y = 1;
            main.Add(_cooldownLabel);

            InputField _cooldown = AddInputField(
                null, 0, 1,
                25, TEXTBOX_HEIGHT,
                numbersOnly: true
                );
            _cooldown.X = _cooldownLabel.X + _cooldownLabel.Width + 5;
            _cooldown.SetText(data.cooldown.ToString());
            main.Add(_cooldown);

            Combobox _message_type = AddCombobox(null, new string[] { "All", "Self", "Other" }, data.message_type, _cooldown.X + _cooldown.Width + 5, 1, 80);
            main.Add(_message_type);

            InputField _conditionText = AddInputField(
                null, 1, _delete.Height + 5,
                main.Width - 25, TEXTBOX_HEIGHT
                );
            _conditionText.SetText(data.trigger);
            main.Add(_conditionText);

            Checkbox _replaceIfExists = AddCheckBox(null, "", data.replace_if_exists, _conditionText.X + _conditionText.Width + 2, _conditionText.Y);
            _replaceIfExists.SetTooltip("Replace any active cooldown of this type with a new one if triggered again.");
            main.Add(_replaceIfExists);

            NiceButton _save = new NiceButton(main.Width - 37, 1, 37, TEXTBOX_HEIGHT, ButtonAction.Activate, "Save");
            _save.IsSelectable = false;
            _save.MouseUp += (s, e) =>
            {
                CoolDownBar.CoolDownConditionData.SaveCondition(key, _hueSelector.Hue, _name.Text, _conditionText.Text, int.Parse(_cooldown.Text), false, _message_type.SelectedIndex, _replaceIfExists.IsChecked);
            };
            main.Add(_save);

            NiceButton _preview = new NiceButton(_save.X - 54, 1, 54, TEXTBOX_HEIGHT, ButtonAction.Activate, "Preview");
            _preview.IsSelectable = false;
            _preview.MouseUp += (s, e) =>
            {
                CoolDownBarManager.AddCoolDownBar(TimeSpan.FromSeconds(int.Parse(_cooldown.Text)), _name.Text, _hueSelector.Hue, _replaceIfExists.IsChecked);
            };
            main.Add(_preview);
            return main;
        }


        private void BuildProfiles()
        {
            const int PAGE = 8789;

            ScrollArea rightArea = new ScrollArea(165, 20, WIDTH - 185, HEIGHT - 80, true);
            rightArea.ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways;

            int startX = 5;
            int startY = 5;

            DataBox box = new DataBox(startX, startY, rightArea.Width - 15, 1);
            box.WantUpdateSize = true;
            rightArea.Add(box);

            SettingsSection section = AddSettingsSection(box, "-----PROFILE MANAGEMENT-----");

            section.Add(AddLabel(null, "Use these options to share your current settings", startX, startY));
            startY += 20;
            section.Add(AddLabel(null, "across characters. This is useful when you have", startX, startY));
            startY += 20;
            section.Add(AddLabel(null, "configured hues, fields, tiles, visual helpers,", startX, startY));
            startY += 20;
            section.Add(AddLabel(null, "and other options and want to reuse them.", startX, startY));
            startY += 30;

            section.Add(AddLabel(null, "WARNING: Override actions cannot be undone.", startX, startY));
            startY += 40;

            const int PROFILE_BUTTON_MARGIN = 40;
            section.PushIndent();
            int buttonWidth = box.Width - PROFILE_BUTTON_MARGIN * 2;
            var btnSaveDefault = new NiceButton(0, 0, buttonWidth, 25, ButtonAction.Activate, "Save current settings as DEFAULT for new characters")
            {
                ButtonParameter = (int)Buttons.SaveAsDefault,
                IsSelectable = false,
                DisplayBorder = true
            };
            section.Add(btnSaveDefault);
            startY += 45;

            section.Add(AddLabel(null, "Saves a 'default.json' profile. Any new character", startX, startY));
            startY += 18;
            section.Add(AddLabel(null, "that does not yet have settings will inherit these.", startX, startY));
            startY += 40;

            var allPaths = ProfileManager.GetAllProfilePaths();
            var btnOverrideAll = new NiceButton(0, 0, buttonWidth, 25, ButtonAction.Activate, $"Override ALL other profiles ({allPaths.Count})")
            {
                ButtonParameter = (int)Buttons.OverrideAllProfiles,
                IsSelectable = false,
                DisplayBorder = true
            };
            section.Add(btnOverrideAll);
            startY += 45;

            section.Add(AddLabel(null, "Copies your current settings to every other", startX, startY));
            startY += 18;
            section.Add(AddLabel(null, "character profile found on this installation.", startX, startY));
            startY += 40;

            var serverPaths = ProfileManager.GetSameServerProfilePaths();
            string serverName = _currentProfile?.ServerName ?? "server";
            var btnOverrideServer = new NiceButton(0, 0, buttonWidth, 25, ButtonAction.Activate, $"Override profiles on same server ({serverPaths.Count})")
            {
                ButtonParameter = (int)Buttons.OverrideSameServer,
                IsSelectable = false,
                DisplayBorder = true
            };
            section.Add(btnOverrideServer);
            startY += 45;

            section.Add(AddLabel(null, $"Copies your current settings only to profiles", startX, startY));
            startY += 18;
            section.Add(AddLabel(null, $"on the same server: {serverName}.", startX, startY));
            section.PopIndent();

            Add(rightArea, PAGE);
        }

        private void BuildDust()
        {
            const int PAGE = 16;
            // ## BEGIN - END ## // TAZUO
            //ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);
            // ## BEGIN - END ## // TAZUO
            ScrollArea rightArea = new ScrollArea(165, 20, WIDTH - 185, HEIGHT - 80, true);
            rightArea.ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways;
            // ## BEGIN - END ## // TAZUO

            int startX = 5;
            int startY = 5;

            DataBox box = new DataBox(startX, startY, rightArea.Width - 15, 1);
            box.WantUpdateSize = true;
            rightArea.Add(box);

            // ## BEGIN - END ## // ART / HUE CHANGES
            SettingsSection section = AddSettingsSection(box, _langDust.ArtHueChanges);

            section.Add(_colorStealth = AddCheckBox(null, _langDust.ColorStealth, _currentProfile.ColorStealth, startX, startY));
            startY += _colorStealth.Height + 2;

            section.Add(_stealthColorPickerBox = AddHueDisplay(null, startX, startY, _currentProfile.StealthHue, ""));
            startY += _stealthColorPickerBox.Height + 2;

            section.AddRight(AddLabel(null, _langDust.StealthColor, 0, 0), 2);

            section.Add(AddLabel(null, _langDust.OrNeon, startX, startY));

            int mode = _currentProfile.StealthNeonType;
            section.AddRight(_stealthNeonType = AddCombobox(null, new[] { _langDust.Off, _langDust.White, _langDust.Pink, _langDust.Ice, _langDust.Fire }, mode, startX, startY, 100));
            startY += _stealthNeonType.Height + 2;

            section.Add(_colorEnergyBolt = AddCheckBox(null, _langDust.ColorEnergyBolt, _currentProfile.ColorEnergyBolt, startX, startY));
            startY += _colorEnergyBolt.Height + 2;

            section.Add(_energyBoltColorPickerBox = AddHueDisplay(null, startX, startY, _currentProfile.EnergyBoltHue, ""));
            startY += _energyBoltColorPickerBox.Height + 2;

            section.AddRight(AddLabel(null, _langDust.ColorEnergyBoltLabel, 0, 0), 2);

            section.Add(AddLabel(null, _langDust.OrNeonLabel, startX, startY));

            mode = _currentProfile.EnergyBoltNeonType;
            section.AddRight(_energyBoltNeonType = AddCombobox(null, new[] { _langDust.Off, _langDust.White, _langDust.Pink, _langDust.Ice, _langDust.Fire }, mode, startX, startY, 100));
            startY += _energyBoltNeonType.Height + 2;

            section.Add(AddLabel(null, _langDust.ChangeEnergyBoltArtTo, startX, startY));

            mode = _currentProfile.EnergyBoltArtType;
            section.AddRight(_energyBoltArtType = AddCombobox(null, new[] { _langDust.Normal, _langDust.Explo, _langDust.Bagball }, mode, startX, startY, 100));
            startY += _energyBoltArtType.Height + 2;

            section.Add(AddLabel(null, _langDust.ChangeGoldArtTo, startX, startY));

            mode = _currentProfile.GoldType;
            section.AddRight(_goldType = AddCombobox(null, new[] { _langDust.Normal, _langDust.Cannonball, _langDust.PrevCoin }, mode, startX, startY, 100));
            startY += _goldType.Height + 2;

            section.Add(_colorGold = AddCheckBox(null, _langDust.ColorCannonballOrPrevCoin, _currentProfile.ColorGold, startX, startY));
            startY += _colorGold.Height + 2;

            section.Add(_goldColorPickerBox = AddHueDisplay(null, startX, startY, _currentProfile.GoldHue, ""));
            startY += _goldColorPickerBox.Height + 2;

            section.AddRight(AddLabel(null, _langDust.CannonballOrPrevCoinColor, 0, 0), 2);

            section.Add(AddLabel(null, _langDust.ChangeTreeArtTo, startX, startY));

            mode = _currentProfile.TreeType;
            section.AddRight(_treeType = AddCombobox(null, new[] { _langDust.Normal, _langDust.Stump, _langDust.Tile }, mode, startX, startY, 100));
            startY += _treeType.Height + 2;

            section.Add(_colorTreeTile = AddCheckBox(null, _langDust.ColorStumpOrTile, _currentProfile.ColorTreeTile, startX, startY));
            startY += _colorTreeTile.Height + 2;

            section.Add(_treeTileColorPickerBox = AddHueDisplay(null, startX, startY, _currentProfile.TreeTileHue, ""));
            startY += _treeTileColorPickerBox.Height + 2;

            section.AddRight(AddLabel(null, _langDust.StumpOrTileColor, 0, 0), 2);

            section.Add(AddLabel(null, _langDust.BlockerType, startX, startY));

            mode = _currentProfile.BlockerType;
            section.AddRight(_blockerType = AddCombobox(null, new[] { _langDust.Normal, _langDust.Stump, _langDust.Tile }, mode, startX, startY, 100));
            startY += _blockerType.Height + 2;

            section.Add(_colorBlockerTile = AddCheckBox(null, _langDust.ColorStumpOrTileBlocker, _currentProfile.ColorBlockerTile, startX, startY));
            startY += _colorBlockerTile.Height + 2;

            section.Add(_blockerTileColorPickerBox = AddHueDisplay(null, startX, startY, _currentProfile.BlockerTileHue, ""));
            startY += _blockerTileColorPickerBox.Height + 2;

            section.AddRight(AddLabel(null, _langDust.StumpOrTileColor, 0, 0), 2);
            // ## BEGIN - END ## // ART / HUE CHANGES
            // ## BEGIN - END ## // TITLE BAR
            SettingsSection sectionTitleBar = AddSettingsSection(box, "-----TITLE BAR-----");
            sectionTitleBar.Y = section.Bounds.Bottom + 80;
            startY = section.Bounds.Bottom + 80;

            sectionTitleBar.Add(AddLabel(null, "Window title style", startX, startY));
            sectionTitleBar.AddRight(_windowTitleStyle = AddCombobox(null, WINDOW_TITLE_STYLE_LABELS, GetWindowTitleStyleIndex(_currentProfile.GetEffectiveWindowTitleStyle()), startX, startY, 180));
            startY += _windowTitleStyle.Height + 2;

            _windowTitleStyle.OnOptionSelected += (_, _) => ApplyWindowTitleStyleSelection(true);
            if (_windowBorderless != null) _windowBorderless.ValueChanged += (_, _) => SyncWindowTitleStyleFromWindowOptions();
            if (_enableWindowBorderFrame != null) _enableWindowBorderFrame.ValueChanged += (_, _) => SyncWindowTitleStyleFromWindowOptions();

            sectionTitleBar.Add(_enableTitleBarStats = AddCheckBox(null, "Enable title bar stats (HP, Mana, Stamina)", _currentProfile.EnableTitleBarStats, startX, startY));
            startY += _enableTitleBarStats.Height + 2;

            sectionTitleBar.Add(AddLabel(null, "Display mode (choose one):", startX, startY));
            startY += 20;

            sectionTitleBar.Add(_titleBarStatsModeText = AddCheckBox(null, "Text (HP 85/100, MP 42/50, SP 95/100)", _currentProfile.TitleBarStatsMode == TitleBarStatsMode.Text, startX, startY));
            startY += _titleBarStatsModeText.Height + 2;

            sectionTitleBar.Add(_titleBarStatsModePercent = AddCheckBox(null, "Percent (HP 85%, MP 84%, SP 95%)", _currentProfile.TitleBarStatsMode == TitleBarStatsMode.Percent, startX, startY));
            startY += _titleBarStatsModePercent.Height + 2;

            sectionTitleBar.Add(_titleBarStatsModeProgressBar = AddCheckBox(null, "Progress Bar (colored bars below each other)", _currentProfile.TitleBarStatsMode == TitleBarStatsMode.ProgressBar, startX, startY));
            startY += _titleBarStatsModeProgressBar.Height + 2;

            void UncheckOtherTitleBarModes(Checkbox selected)
            {
                if (!selected.IsChecked) return;
                if (_titleBarStatsModeText != null && _titleBarStatsModeText != selected) _titleBarStatsModeText.IsChecked = false;
                if (_titleBarStatsModePercent != null && _titleBarStatsModePercent != selected) _titleBarStatsModePercent.IsChecked = false;
                if (_titleBarStatsModeProgressBar != null && _titleBarStatsModeProgressBar != selected) _titleBarStatsModeProgressBar.IsChecked = false;
            }
            _titleBarStatsModeText.ValueChanged += (s, e) => UncheckOtherTitleBarModes(_titleBarStatsModeText);
            _titleBarStatsModePercent.ValueChanged += (s, e) => UncheckOtherTitleBarModes(_titleBarStatsModePercent);
            _titleBarStatsModeProgressBar.ValueChanged += (s, e) => UncheckOtherTitleBarModes(_titleBarStatsModeProgressBar);
            UpdateTitleBarStatsControlsAvailability();

    
            // ## BEGIN - END ## // TITLE BAR
            // ## BEGIN - END ## // VISUAL HELPERS
            SettingsSection section2 = AddSettingsSection(box, "-----VISUAL HELPERS-----");
            section2.Y = sectionTitleBar.Bounds.Bottom + 40;

            startY = sectionTitleBar.Bounds.Bottom + 40;

            section2.Add(_highlightTileRange = AddCheckBox(null, _langDust.HighlightTilesOnRange, _currentProfile.HighlightTileAtRange, startX, startY));
            startY += _highlightTileRange.Height + 2;

            section2.Add(AddLabel(null, _langDust.AtRange, startX, startY));

            section2.AddRight(_highlightTileRangeRange = AddHSlider(null, 1, 20, _currentProfile.HighlightTileAtRangeRange, startX, startY, 200));
            startY += _highlightTileRangeRange.Height + 2;

            section2.Add(_highlightTileRangeColorPickerBox = AddHueDisplay(null, startX, startY, _currentProfile.HighlightTileRangeHue, ""));
            startY += _highlightTileRangeColorPickerBox.Height + 2;
            section2.AddRight(AddLabel(null, _langDust.TileColor, 0, 0), 2);

            section2.Add(_highlightTileRangeSpell = AddCheckBox(null, _langDust.HighlightTilesOnRangeSpell, _currentProfile.HighlightTileAtRangeSpell, startX, startY));
            startY += _highlightTileRangeSpell.Height + 2;

            section2.Add(AddLabel(null, _langDust.AtRange, startX, startY));

            section2.AddRight(_highlightTileRangeRangeSpell = AddHSlider(null, 1, 20, _currentProfile.HighlightTileAtRangeRangeSpell, startX, startY, 200));
            startY += _highlightTileRangeRangeSpell.Height + 2;

            section2.Add(_highlightTileRangeColorPickerBoxSpell = AddHueDisplay(null, startX, startY, _currentProfile.HighlightTileRangeHueSpell, ""));
            startY += _highlightTileRangeColorPickerBoxSpell.Height + 2;
            section2.AddRight(AddLabel(null, _langDust.TileColor, 0, 0), 2);

            section2.Add(_previewFields = AddCheckBox(null, _langDust.PreviewFields, _currentProfile.PreviewFields, startX, startY));
            startY += _previewFields.Height + 2;

            section2.Add(_ownAuraByHP = AddCheckBox(null, _langDust.ColorOwnAuraByHP, _currentProfile.OwnAuraByHP, startX, startY));
            startY += _ownAuraByHP.Height + 2;

            section2.Add(AddLabel(null, _langDust.GlowingWeapons, startX, startY));

            mode = _currentProfile.GlowingWeaponsType;
            section2.Add(_glowingWeaponsType = AddCombobox(null, new[] { _langDust.Off, _langDust.White, _langDust.Pink, _langDust.Ice, _langDust.Fire, _langDust.Custom }, mode, startX, startY, 100));
            startY += _glowingWeaponsType.Height + 2;

            section2.Add(_highlightGlowingWeaponsTypeColorPickerBoxHue = AddHueDisplay(null, startX, startY, _currentProfile.HighlightGlowingWeaponsTypeHue, ""));
            startY += _highlightGlowingWeaponsTypeColorPickerBoxHue.Height + 2;
            section2.AddRight(AddLabel(null, _langDust.CustomColorGlowingWeapons, 0, 0), 2);

            section2.Add(AddLabel(null, _langDust.HighlightLasttarget, startX, startY));

            mode = _currentProfile.HighlightLastTargetType;
            section2.Add(_highlightLastTargetType = AddCombobox(null, new[] { _langDust.Off, _langDust.White, _langDust.Pink, _langDust.Ice, _langDust.Fire, _langDust.Custom }, mode, startX, startY, 100));
            startY += _highlightLastTargetType.Height + 2;

            section2.Add(_highlightLastTargetTypeColorPickerBox = AddHueDisplay(null, startX, startY, _currentProfile.HighlightLastTargetTypeHue, ""));
            startY += _highlightLastTargetTypeColorPickerBox.Height + 2;
            section2.AddRight(AddLabel(null, _langDust.CustomColorLastTarget, 0, 0), 2);

            section2.Add(AddLabel(null, "Highlight Friends Guild Mobiles:", startX, startY));

            mode = _currentProfile.HighlighFriendsGuildType;
            section2.Add(_highlighFriendsGuildType = AddCombobox(null, new[] { _langDust.Off, _langDust.White, _langDust.Pink, _langDust.Ice, _langDust.Fire, _langDust.Custom }, mode, startX, startY, 100));
            startY += _highlighFriendsGuildType.Height + 2;

            section2.Add(_highlighFriendsGuildTypeHueColorPickerBox = AddHueDisplay(null, startX, startY, _currentProfile.HighlighFriendsGuildTypeHue, ""));
            startY += _highlighFriendsGuildTypeHueColorPickerBox.Height + 2;
            section2.AddRight(AddLabel(null, "Custom color friends guild", 0, 0), 2);

            section2.Add(AddLabel(null, _langDust.HighlightLasttargetPoisoned, startX, startY));

            mode = _currentProfile.HighlightLastTargetTypePoison;
            section2.Add(_highlightLastTargetTypePoison = AddCombobox(null, new[] { _langDust.Off, _langDust.White, _langDust.Pink, _langDust.Ice, _langDust.Fire, "Special", _langDust.Custom }, mode, startX, startY, 100));
            startY += _highlightLastTargetTypePoison.Height + 2;

            section2.Add(_highlightLastTargetTypeColorPickerBoxPoison = AddHueDisplay(null, startX, startY, _currentProfile.HighlightLastTargetTypePoisonHue, ""));
            startY += _highlightLastTargetTypeColorPickerBoxPoison.Height + 2;
            section2.AddRight(AddLabel(null, _langDust.CustomColorPoisoned, 0, 0), 2);

            section2.Add(AddLabel(null, _langDust.HighlightLasttargetParalyzed, startX, startY));

            mode = _currentProfile.HighlightLastTargetTypePara;
            section2.Add(_highlightLastTargetTypePara = AddCombobox(null, new[] { _langDust.Off, _langDust.White, _langDust.Pink, _langDust.Ice, _langDust.Fire, "Special", _langDust.Custom }, mode, startX, startY, 100));
            startY += _highlightLastTargetTypePara.Height + 2;

            section2.Add(_highlightLastTargetTypeColorPickerBoxPara = AddHueDisplay(null, startX, startY, _currentProfile.HighlightLastTargetTypeParaHue, ""));
            startY += _highlightLastTargetTypeColorPickerBoxPara.Height + 2;
            section2.AddRight(AddLabel(null, _langDust.CustomColorParalyzed, 0, 0), 2);

            section2.Add(AddLabel(null, "Highlight lasttarget stunned:", startX, startY));

            mode = _currentProfile.HighlightLastTargetTypeStunned;
            section2.Add(_highlightLastTargetTypeStunned = AddCombobox(null, new[] { _langDust.Off, _langDust.White, _langDust.Pink, _langDust.Ice, _langDust.Fire, "Special", _langDust.Custom }, mode, startX, startY, 100));
            startY += _highlightLastTargetTypeStunned.Height + 2;

            section2.Add(_highlightLastTargetTypeColorPickerBoxStunned = AddHueDisplay(null, startX, startY, _currentProfile.HighlightLastTargetTypeStunnedHue, ""));
            startY += _highlightLastTargetTypeColorPickerBoxStunned.Height + 2;
            section2.AddRight(AddLabel(null, "Custom color stunned", 0, 0), 2);

            section2.Add(AddLabel(null, "Highlight lasttarget mortalled (yellow hits):", startX, startY));

            mode = _currentProfile.HighlightLastTargetTypeMortalled;
            section2.Add(_highlightLastTargetTypeMortalled = AddCombobox(null, new[] { _langDust.Off, _langDust.White, _langDust.Pink, _langDust.Ice, _langDust.Fire, "Special", _langDust.Custom }, mode, startX, startY, 100));
            startY += _highlightLastTargetTypeMortalled.Height + 2;

            section2.Add(_highlightLastTargetTypeColorPickerBoxMortalled = AddHueDisplay(null, startX, startY, _currentProfile.HighlightLastTargetTypeMortalledHue, ""));
            startY += _highlightLastTargetTypeColorPickerBoxMortalled.Height + 2;
            section2.AddRight(AddLabel(null, "Custom color mortalled", 0, 0), 2);
            // ## BEGIN - END ## // VISUAL HELPERS
            // ## BEGIN - END ## // HEALTHBAR
            SettingsSection section3 = AddSettingsSection(box, "-----HEALTHBAR-----");
            section3.Y = section2.Bounds.Bottom + 40;

            startY = section2.Bounds.Bottom + 40;

            section3.Add(_highlightLastTargetHealthBarOutline = AddCheckBox(null, _langDust.HighlightLTHealthbar, _currentProfile.HighlightLastTargetHealthBarOutline, startX, startY));
            startY += _highlightLastTargetHealthBarOutline.Height + 2;
            section3.Add(_highlightHealthBarByState = AddCheckBox(null, _langDust.HighlightHealthBarByState, _currentProfile.HighlightHealthBarByState, startX, startY));
            startY += _highlightHealthBarByState.Height + 2;
            // ## BEGIN - END ## // HEALTHBAR
            // ## BEGIN - END ## // CURSOR
            SettingsSection section4 = AddSettingsSection(box, "-----CURSOR-----");
            section4.Y = section3.Bounds.Bottom + 40;

            startY = section3.Bounds.Bottom + 40;

            section4.Add(_spellOnCursor = AddCheckBox(null, _langDust.ShowSpellsOnCursor, _currentProfile.SpellOnCursor, startX, startY));
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
                    5000
                )
            );
            _spellOnCursorOffsetX.SetText(_currentProfile.SpellOnCursorOffset.X.ToString());
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
                    5000
                )
            );
            _spellOnCursorOffsetY.SetText(_currentProfile.SpellOnCursorOffset.Y.ToString());
            section4.AddRight(AddLabel(null, "Y", 0, 0), 2);
            startY += _spellOnCursorOffsetY.Height + 2;

            section4.Add(_colorGameCursor = AddCheckBox(null, _langDust.ColorGameCursorWhenTargeting, _currentProfile.ColorGameCursor, startX, startY));
            startY += _colorGameCursor.Height + 2;
            section4.Add(_showTargetRangeIndicator = AddCheckBox(null, "", _currentProfile.ShowTargetRangeIndicator, 0, 0));
            section4.AddRight(AddLabel(null, Language.Instance?.GetOptionsGumpLanguage?.GetGeneral?.CursorRange ?? "Show target range indicator (shows range circle when targeting)", 0, 0));
            startY += _showTargetRangeIndicator.Height + 2;
            // ## BEGIN - END ## // CURSOR
            // ## BEGIN - END ## // OVERHEAD / UNDERCHAR
            SettingsSection section5 = AddSettingsSection(box, "-----OVERHEAD / UNDERFEET-----");
            section5.Y = section4.Bounds.Bottom + 40;
            startY = section4.Bounds.Bottom + 40;

            section5.Add(_overheadRange = AddCheckBox(null, _langDust.DisplayRangeInOverhead, _currentProfile.OverheadRange, startX, startY));
            startY += _overheadRange.Height + 2;
            // ## BEGIN - END ## // OVERHEAD / UNDERCHAR
            // ## BEGIN - END ## // OLDHEALTHLINES
            SettingsSection section6 = AddSettingsSection(box, "-----OLDHEALTHLINES-----");
            section6.Y = section5.Bounds.Bottom + 40;

            startY = section5.Bounds.Bottom + 40;

            section6.Add(_useOldHealthBars = AddCheckBox(null, _langDust.UseOldHealthlines, _currentProfile.UseOldHealthBars, startX, startY));
            startY += _useOldHealthBars.Height + 2;
            section6.Add(_multipleUnderlinesSelfParty = AddCheckBox(null, _langDust.DisplayManaStamInUnderline, _currentProfile.MultipleUnderlinesSelfParty, startX, startY));
            startY += _multipleUnderlinesSelfParty.Height + 2;
            section6.Add(_multipleUnderlinesSelfPartyBigBars = AddCheckBox(null, _langDust.UseBiggerUnderlines, _currentProfile.MultipleUnderlinesSelfPartyBigBars, startX, startY));
            startY += _multipleUnderlinesSelfPartyBigBars.Height + 2;

            section6.Add(AddLabel(null, _langDust.TransparencyForSelfAndParty, startX, startY));

            section6.Add(_multipleUnderlinesSelfPartyTransparency = AddHSlider(null, 1, 10, _currentProfile.MultipleUnderlinesSelfPartyTransparency, startX, startY, 200));
            startY += _multipleUnderlinesSelfPartyTransparency.Height + 2;
            // ## BEGIN - END ## // OLDHEALTHLINES
            // ## BEGIN - END ## // MISC
            SettingsSection section7 = AddSettingsSection(box, "-----MISC-----");
            section7.Y = section6.Bounds.Bottom + 40;

            startY = section6.Bounds.Bottom + 40;

            section7.Add(_offscreenTargeting = AddCheckBox(null, _langDust.OffscreenTargeting, true, startX, startY)); //has no effect but feature list
            startY += _offscreenTargeting.Height + 2;

            section7.Add(_setTargetOut = AddCheckBox(null, _langDust.SetTargetOutRange, true, startX, startY)); //has no effect but feature list
            startY += _setTargetOut.Height + 2;
            
            section7.Add(_SpecialSetLastTargetCliloc = AddCheckBox(null, _langDust.RazorTargetToLasttargetString, _currentProfile.SpecialSetLastTargetCliloc, startX, startY));
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
                    50000
                )
            );
            _SpecialSetLastTargetClilocText.SetText(_currentProfile.SpecialSetLastTargetClilocText.ToString());

            section7.Add(_blackOutlineStatics = AddCheckBox(null, _langDust.OutlineStaticsBlack, _currentProfile.BlackOutlineStatics, startX, startY));
            startY += _blackOutlineStatics.Height + 2;

            section7.Add(_ignoreStaminaCheck = AddCheckBox(null, _langDust.IgnoreStaminaCheck, _currentProfile.IgnoreStaminaCheck, startX, startY));
            startY += _ignoreStaminaCheck.Height + 2;

            section7.Add(_blockWoS = AddCheckBox(null, _langDust.BlockWallOfStone, _currentProfile.BlockWoS, startX, startY));
            startY += _blockWoS.Height + 2;

            section7.Add(_blockWoSFelOnly = AddCheckBox(null, _langDust.BlockWallOfStoneFelOnly, _currentProfile.BlockWoSFelOnly, startX, startY));
            startY += _blockWoSFelOnly.Height + 2;

            section7.Add(AddLabel(null, _langDust.WallOfStoneArt, startX, startY));
            section7.Add
            (
                _blockWoSArt = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    50000
                )
            );
            _blockWoSArt.SetText(_currentProfile.BlockWoSArt.ToString());
            startY += _blockWoSArt.Height + 2;

            section7.Add(_blockWoSArtForceAoS = AddCheckBox(null, _langDust.ForceWoSToArtAbove, _currentProfile.BlockWoSArtForceAoS, startX, startY));
            startY += _blockWoSArtForceAoS.Height + 2;

            section7.Add(_blockEnergyF = AddCheckBox(null, _langDust.BlockEnergyField, _currentProfile.BlockEnergyF, startX, startY));
            startY += _blockEnergyF.Height + 2;

            section7.Add(_blockEnergyFFelOnly = AddCheckBox(null, _langDust.BlockEnergyFieldFelOnly, _currentProfile.BlockEnergyFFelOnly, startX, startY));
            startY += _blockEnergyFFelOnly.Height + 2;

            section7.Add(AddLabel(null, _langDust.EnergyFieldArt, startX, startY));
            section7.Add
            (
                _blockEnergyFArt = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    50000
                )
            );
            _blockEnergyFArt.SetText(_currentProfile.BlockEnergyFArt.ToString());
            startY += _blockEnergyFArt.Height + 2;

            section7.Add(_blockEnergyFArtForceAoS = AddCheckBox(null, _langDust.ForceEnergyFToArtAbove, _currentProfile.BlockEnergyFArtForceAoS, startX, startY));
            startY += _blockEnergyFArtForceAoS.Height + 2;
            // ## BEGIN - END ## // MISC
            // ## BEGIN - END ## // MISC2
            SettingsSection section8 = AddSettingsSection(box, "-----MISC2-----");
            section8.Y = section7.Bounds.Bottom + 40;

            startY = section7.Bounds.Bottom + 40;

            section8.Add(_wireframeView = AddCheckBox(null, _langDust.EnableWireFrameView, _currentProfile.WireFrameView, startX, startY));
            startY += _wireframeView.Height + 2;

            section8.Add(_hueImpassableView = AddCheckBox(null, _langDust.HueImpassableTiles, _currentProfile.HueImpassableView, startX, startY));
            startY += _hueImpassableView.Height + 2;

            section8.Add(_hueImpassableViewColorPickerBox = AddHueDisplay(null, startX, startY, _currentProfile.HueImpassableViewHue, ""));
            startY += _hueImpassableViewColorPickerBox.Height + 2;
            section8.AddRight(AddLabel(null, _langDust.HueLabel, 0, 0), 2);

            section8.Add(_transparentHouses = AddCheckBox(null, _langDust.TransparentHousesAndItems, _currentProfile.TransparentHousesEnabled, startX, startY));
            startY += _transparentHouses.Height + 2;

            section8.Add(_transparentHousesZ = AddHSlider(null, 1, 100, _currentProfile.TransparentHousesZ, startX, startY, 200));
            startY += _transparentHousesZ.Height + 2;

            section8.Add(AddLabel(null, _langDust.Transparency, startX, startY));
            section8.Add(_transparentHousesTransparency = AddHSlider(null, 1, 9, _currentProfile.TransparentHousesTransparency, startX, startY, 200));
            startY += _transparentHousesTransparency.Height + 2;

            section8.Add(_invisibleHouses = AddCheckBox(null, _langDust.InvisibleHousesAndItems, _currentProfile.InvisibleHousesEnabled, startX, startY));
            startY += _invisibleHouses.Height + 2;

            section8.Add(_invisibleHousesZ = AddHSlider(null, 1, 100, _currentProfile.InvisibleHousesZ, startX, startY, 200));
            startY += _invisibleHousesZ.Height + 2;

            section8.Add(AddLabel(null, "Dont make Invisible or Transparent below (Z level)", startX, startY));
            section8.Add(_dontRemoveHouseBelowZ = AddHSlider(null, 1, 100, _currentProfile.DontRemoveHouseBelowZ, startX, startY, 200));
            startY += _dontRemoveHouseBelowZ.Height + 2;

            section8.Add(_drawMobilesWithSurfaceOverhead = AddCheckBox(null, "Draw mobiles with surface overhead", _currentProfile.DrawMobilesWithSurfaceOverhead, startX, startY));
            startY += _drawMobilesWithSurfaceOverhead.Height + 2;

            section8.Add(_ignoreCoT = AddCheckBox(null, "Enable ignorelist for circle of transparency:", _currentProfile.IgnoreCoTEnabled, startX, startY));
            startY += _ignoreCoT.Height + 2;

            section8.Add(_showDeathOnWorldmap = AddCheckBox(null, "Show death location on world map for 5min:", _currentProfile.ShowDeathOnWorldmap, startX, startY));
            startY += _showDeathOnWorldmap.Height + 2;

            section8.Add(_showMapCloseFriend = AddCheckBox(null, "Show closed friend in World Map:", _currentProfile.ShowMapCloseFriend, startX, startY));
            startY += _showMapCloseFriend.Height + 2;
            section8.Add(_autoAvoidMobiles = AddCheckBox(null, _langDust.AutoAvoidObstaculesAndMobiles, _currentProfile.AutoAvoidObstacules, startX, startY));
            startY += _autoAvoidMobiles.Height + 2;
            section8.Add(_scaleMonstersEnabled = AddCheckBox(null, "Scale monsters (Ctrl+Shift over monster: +/- to scale)", _currentProfile.ScaleMonstersEnabled, startX, startY));
            startY += _scaleMonstersEnabled.Height + 2;

            section8.Add(_forceGargoyleWalk = AddCheckBox(null, "Force gargoyle to walk instead of fly", _currentProfile.ForceGargoyleWalk, startX, startY));
            startY += _forceGargoyleWalk.Height + 2;

            // ## BEGIN - END ## // MISC2
            // ## BEGIN - END ## // NAMEOVERHEAD
            SettingsSection section9 = AddSettingsSection(box, "-----NAMEOVERHEAD-----");
            section9.Y = section8.Bounds.Bottom + 40;

            startY = section8.Bounds.Bottom + 40;

            section9.Add(_showHPLineInNOH = AddCheckBox(null, "Show HPLine in NameOverheadGump:", _currentProfile.ShowHPLineInNOH, startX, startY));
            startY += _showHPLineInNOH.Height + 2;

            // ## BEGIN - END ## // NAMEOVERHEAD
            // ## BEGIN - END ## // STATUSGUMP
            SettingsSection section10 = AddSettingsSection(box, "-----STATUSGUMP-----");
            section10.Y = section9.Bounds.Bottom + 40;

            startY = section9.Bounds.Bottom + 40;

            section10.Add(_useRazorEnhStatusGump = AddCheckBox(null, "Use Razor Enhanced status gump:", _currentProfile.UseRazorEnhStatusGump, startX, startY));
            startY += _useRazorEnhStatusGump.Height + 2;
            // ## BEGIN - END ## // STATUSGUMP
            // ## BEGIN - END ## // MISC3 SHOWALLLAYERS
            SettingsSection section11 = AddSettingsSection(box, "-----MISC3-----");
            section11.Y = section10.Bounds.Bottom + 40;

            startY = section10.Bounds.Bottom + 40;

            section11.Add(_showAllLayers = AddCheckBox(null, "Show all equipment layers on mobiles ON / OFF", _currentProfile.ShowAllLayers, startX, startY));
            startY += _showAllLayers.Height + 2;
            section11.Add(_showAllLayersPaperdoll = AddCheckBox(null, "Show all equipment layers on paperdoll ON / OFF", _currentProfile.ShowAllLayersPaperdoll, startX, startY));
            startY += _showAllLayersPaperdoll.Height + 2;
            section11.Add(_colorPaperdollByDurability = AddCheckBox(null, "Color paperdoll items by low durability (yellow/red)", _currentProfile.ColorPaperdollByDurability, startX, startY));
            startY += _colorPaperdollByDurability.Height + 2;

            section11.Add
            (
                _showAllLayersPaperdoll_X = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    5000
                )
            );
            _showAllLayersPaperdoll_X.SetText(_currentProfile.ShowAllLayersPaperdoll_X.ToString());
            section11.AddRight(AddLabel(null, "X ( reopen paperdoll after changes )", 0, 0), 2);
            startY += _showAllLayersPaperdoll_X.Height + 2;
            // ## BEGIN - END ## // MISC3 SHOWALLLAYERS
            // ## BEGIN - END ## // MISC3 THIEFSUPREME
            section11.Add(_overrideContainerOpenRange = AddCheckBox(null, "Override container open range", _currentProfile.OverrideContainerOpenRange, startX, startY));
            startY += _overrideContainerOpenRange.Height + 2;

            SettingsSection sectionJournal = AddSettingsSection(box, _langDust.Journal);
            sectionJournal.Y = section11.Bounds.Bottom + 40;
            startY = section11.Bounds.Bottom + 40;
            sectionJournal.Add(AddLabel(null, _langDust.MaxJournalEntries, 0, 0));
            sectionJournal.AddRight(_maxJournalEntries = AddHSlider(null, 200, 2000, _currentProfile.MaxJournalEntries, 0, 0, 200));
            sectionJournal.Add(AddLabel(null, _langDust.JournalOpacity, 0, 0));
            sectionJournal.AddRight(_journalOpacity = AddHSlider(null, 0, 100, _currentProfile.JournalOpacity, 0, 0, 200), 2);
            sectionJournal.PushIndent();
            sectionJournal.Add(_journalBackgroundColor = AddHueDisplay(null, 0, 0, _currentProfile.AltJournalBackgroundHue, ""));
            sectionJournal.AddRight(AddLabel(null, _langDust.JournalBackgroundColor, 0, 0));
            sectionJournal.PopIndent();
            sectionJournal.Add(AddLabel(null, _langDust.JournalStyle, 0, 0));
            sectionJournal.AddRight(_journalStyle = AddCombobox(null, Enum.GetNames(typeof(ResizableJournal.BorderStyle)), _currentProfile.JournalStyle, 0, 0, 150));
            sectionJournal.Add(AddLabel(null, _langDust.JournalHideBorders, 0, 0));
            sectionJournal.Add(_hideJournalBorder = AddCheckBox(null, "", _currentProfile.HideJournalBorder, 0, 0));
            sectionJournal.Add(AddLabel(null, _langDust.HideTimestamp, 0, 0));
            sectionJournal.Add(_hideJournalTimestamp = AddCheckBox(null, "", _currentProfile.HideJournalTimestamp, 0, 0));

            SettingsSection sectionNameplates = AddSettingsSection(box, _langDust.Nameplates);
            sectionNameplates.Y = sectionJournal.Bounds.Bottom + 40;
            sectionNameplates.Add(_namePlateHealthBar = AddCheckBox(null, "", _currentProfile.NamePlateHealthBar, 0, 0));
            sectionNameplates.AddRight(AddLabel(null, _langDust.NameplatesAlsoActAsHealthBars, 0, 0));
            sectionNameplates.PushIndent();
            sectionNameplates.Add(AddLabel(null, _langDust.HpOpacity, 0, 0));
            sectionNameplates.AddRight(_namePlateHealthBarOpacity = AddHSlider(null, 0, 100, _currentProfile.NamePlateHealthBarOpacity, 0, 0, 200));
            sectionNameplates.Add(_namePlateShowAtFullHealth = AddCheckBox(null, "", _currentProfile.NamePlateHideAtFullHealth, 0, 0));
            sectionNameplates.AddRight(new Label(_langDust.HideNameplatesIfFullHealth, true, HUE_FONT, font: FONT));
            sectionNameplates.PushIndent();
            sectionNameplates.Add(_namePlateHealthOnlyWarmode = AddCheckBox(null, "", _currentProfile.NamePlateHideAtFullHealthInWarmode, 0, 0));
            sectionNameplates.AddRight(new Label(_langDust.OnlyInWarmode, true, HUE_FONT, font: FONT));
            sectionNameplates.PopIndent();
            sectionNameplates.PopIndent();
            sectionNameplates.Add(AddLabel(null, _langDust.BorderOpacity, 0, 0));
            sectionNameplates.AddRight(_nameplateBorderOpacity = AddHSlider(null, 0, 100, _currentProfile.NamePlateBorderOpacity, 0, 0, 200));
            sectionNameplates.Add(AddLabel(null, _langDust.BackgroundOpacity, 0, 0));
            sectionNameplates.AddRight(_namePlateOpacity = AddHSlider(null, 0, 100, _currentProfile.NamePlateOpacity, 0, 0, 200));

            SettingsSection sectionMobiles = AddSettingsSection(box, _langDust.Mobiles);
            sectionMobiles.Y = sectionNameplates.Bounds.Bottom + 40;
            sectionMobiles.Add(_damageHueSelf = AddHueDisplay(null, 0, 0, _currentProfile.DamageHueSelf, ""));
            sectionMobiles.AddRight(new Label(_langDust.DamageToSelf, true, HUE_FONT, font: FONT));
            sectionMobiles.AddRight(_damageHueOther = AddHueDisplay(null, 0, 0, _currentProfile.DamageHueOther, ""));
            sectionMobiles.AddRight(new Label(_langDust.DamageToOthers, true, HUE_FONT, font: FONT));
            sectionMobiles.Add(_damageHuePet = AddHueDisplay(null, 0, 0, _currentProfile.DamageHuePet, ""));
            sectionMobiles.AddRight(new Label(_langDust.DamageToPets, true, HUE_FONT, font: FONT));
            sectionMobiles.Add(_damageHueAlly = AddHueDisplay(null, 0, 0, _currentProfile.DamageHueAlly, ""));
            sectionMobiles.AddRight(new Label(_langDust.DamageToAllies, true, HUE_FONT, font: FONT));
            sectionMobiles.Add(_damageHueLastAttack = AddHueDisplay(null, 0, 0, _currentProfile.DamageHueLastAttck, ""));
            sectionMobiles.AddRight(new Label(_langDust.DamageToLastAttack, true, HUE_FONT, font: FONT));
            sectionMobiles.Add(AddLabel(null, _langDust.OverheadTextWidth, 0, 0));
            sectionMobiles.AddRight(_overheadTextWidth = AddHSlider(null, 100, 600, _currentProfile.OverheadChatWidth, 0, 0, 200));
            sectionMobiles.Add(AddLabel(null, _langDust.BelowMobileHealthBarScale, 0, 0));
            sectionMobiles.AddRight(_healthLineSizeMultiplier = AddHSlider(null, 1, 5, _currentProfile.HealthLineSizeMultiplier, 0, 0, 150));
            sectionMobiles.Add(_openHealthBarForLastAttack = AddCheckBox(null, "", _currentProfile.OpenHealthBarForLastAttack, 0, 0));
            sectionMobiles.AddRight(AddLabel(null, _langDust.AutomaticallyOpenHealthBarsForLastAttack, 0, 0));

            SettingsSection sectionMiscTaz = AddSettingsSection(box, _langDust.Misc);
            sectionMiscTaz.Y = sectionMobiles.Bounds.Bottom + 40;
            sectionMiscTaz.Add(_disableSystemChat = AddCheckBox(null, "", _currentProfile.DisableSystemChat, 0, 0));
            sectionMiscTaz.AddRight(AddLabel(null, _langDust.DisableSystemChat, 0, 0));
            sectionMiscTaz.Add(_journalMessagesOnlyInJournalBox = AddCheckBox(null, "", _currentProfile.JournalMessagesOnlyInJournalBox, 0, 0));
            sectionMiscTaz.AddRight(AddLabel(null, _langDust.JournalMessagesOnlyInJournalBox, 0, 0));
            sectionMiscTaz.Add(AddLabel(null, _langDust.HiddenPlayerOpacity, 0, 0));
            sectionMiscTaz.AddRight(_hiddenBodyAlpha = AddHSlider(null, 0, 100, _currentProfile.HiddenBodyAlpha, 0, 0, 200), 2);
            sectionMiscTaz.PushIndent();
            sectionMiscTaz.Add(_hiddenBodyHue = AddHueDisplay(null, 0, 0, _currentProfile.HiddenBodyHue, ""));
            sectionMiscTaz.AddRight(AddLabel(null, _langDust.HiddenPlayerHue, 0, 0));
            sectionMiscTaz.PopIndent();
            sectionMiscTaz.Add(AddLabel(null, _langDust.RegularPlayerOpacity, 0, 0));
            sectionMiscTaz.AddRight(_regularPlayerAlpha = AddHSlider(null, 0, 100, _currentProfile.PlayerConstantAlpha, 0, 0, 200));
            sectionMiscTaz.Add(_enableImprovedBuffGump = AddCheckBox(null, _langDust.EnableImprovedBuffGump, _currentProfile.UseImprovedBuffBar, 0, 0));
            sectionMiscTaz.AddRight(_improvedBuffBarHue = AddHueDisplay(null, 0, 0, _currentProfile.ImprovedBuffBarHue, ""));
            sectionMiscTaz.Add(_mainWindowHuePicker = new ModernColorPicker.HueDisplay(_currentProfile.MainWindowBackgroundHue, null, true));
            sectionMiscTaz.AddRight(AddLabel(null, _langDust.MainGameWindowBackground, 0, 0));
            sectionMiscTaz.Add(_enableHealthIndicator = AddCheckBox(null, _langDust.EnableHealthIndicatorBorder, _currentProfile.EnableHealthIndicator, 0, 0));
            sectionMiscTaz.PushIndent();
            sectionMiscTaz.Add(AddLabel(null, _langDust.OnlyShowBelowHp, 0, 0));
            sectionMiscTaz.AddRight(_healthIndicatorPercentage = AddInputField(null, 0, 0, 150, TEXTBOX_HEIGHT, numbersOnly: true));
            _healthIndicatorPercentage.SetText((_currentProfile.ShowHealthIndicatorBelow * 100).ToString());
            sectionMiscTaz.Add(AddLabel(null, _langDust.Size, 0, 0));
            sectionMiscTaz.AddRight(_healthIndicatorWidth = AddInputField(null, 0, 0, 150, TEXTBOX_HEIGHT, numbersOnly: true));
            _healthIndicatorWidth.SetText(_currentProfile.HealthIndicatorWidth.ToString());
            sectionMiscTaz.PopIndent();
            sectionMiscTaz.Add(AddLabel(null, _langDust.SpellIconScale, 0, 0));
            sectionMiscTaz.AddRight(_spellIconScale = AddHSlider(null, 50, 300, _currentProfile.SpellIconScale, 0, 0, 200));
            sectionMiscTaz.Add(_spellIconDisplayHotkey = AddCheckBox(null, "", _currentProfile.SpellIcon_DisplayHotkey, 0, 0));
            sectionMiscTaz.AddRight(AddLabel(null, _langDust.DisplayMatchingHotkeysOnSpellIcons, 0, 0));
            sectionMiscTaz.PushIndent();
            sectionMiscTaz.Add(AddLabel(null, _langDust.HotkeyTextHue, 0, 0));
            sectionMiscTaz.AddRight(_spellIconHotkeyHue = new ModernColorPicker.HueDisplay(_currentProfile.SpellIcon_HotkeyHue, null, true));
            sectionMiscTaz.PopIndent();
            sectionMiscTaz.Add(_enableAlphaScrollWheel = AddCheckBox(null, "", _currentProfile.EnableAlphaScrollingOnGumps, 0, 0));
            sectionMiscTaz.AddRight(AddLabel(null, _langDust.EnableGumpOpacityAdjustViaAltScroll, 0, 0));
            sectionMiscTaz.Add(_useModernShop = AddCheckBox(null, "", _currentProfile.UseModernShopGump, 0, 0));
            sectionMiscTaz.AddRight(AddLabel(null, _langDust.EnableAdvancedShopGump, 0, 0));
            sectionMiscTaz.Add(_skillProgressBarOnChange = AddCheckBox(null, "", _currentProfile.DisplaySkillBarOnChange, 0, 0));
            sectionMiscTaz.AddRight(AddLabel(null, _langDust.DisplaySkillProgressBarOnSkillChanges, 0, 0));
            sectionMiscTaz.Add(AddLabel(null, _langDust.TextFormat, 0, 0));
            sectionMiscTaz.AddRight(_skillProgressBarFormat = AddInputField(null, 0, 0, 250, TEXTBOX_HEIGHT));
            _skillProgressBarFormat.SetText(_currentProfile.SkillBarFormat);
            sectionMiscTaz.Add(_closeHPBarWhenAnchored = AddCheckBox(null, "", _currentProfile.CloseHealthBarIfAnchored, 0, 0));
            sectionMiscTaz.AddRight(AddLabel(null, _langDust.AlsoCloseAnchoredHealthbarsWhenAutoClosingHealthbars, 0, 0));
            var autoLootBtn = new NiceButton(0, 0, 150, TEXTBOX_HEIGHT, ButtonAction.Activate, _langDust.AutoLoot) { IsSelectable = false, DisplayBorder = true };
            autoLootBtn.MouseUp += (s, e) => { if (e.Button == MouseButtonType.Left) AutoLootOptions.AddToUI(); };
            sectionMiscTaz.Add(autoLootBtn);
            sectionMiscTaz.Add(_enableNearbyItemGump = AddCheckBox(null, "", _currentProfile.EnableNearbyItemGump, 0, 0));
            sectionMiscTaz.AddRight(AddLabel(null, _langDust.ShowUseLootModalOnCtrl, 0, 0));

            Add(rightArea, PAGE);
        }
        private void Build765()
        {
            const int PAGE = 17;
            // ## BEGIN - END ## // TAZUO
            //ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);
            // ## BEGIN - END ## // TAZUO
            ScrollArea rightArea = new ScrollArea(165, 20, WIDTH - 185, HEIGHT - 80, true);
            rightArea.ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways;
            // ## BEGIN - END ## // TAZUO

            int startX = 5;
            int startY = 5;

            DataBox box = new DataBox(startX, startY, rightArea.Width - 15, 1);
            box.WantUpdateSize = true;
            rightArea.Add(box);

            // ## BEGIN - END ## // VISUAL HELPERS
            SettingsSection section = AddSettingsSection(box, "-----FEATURES MACROS-----");

            section.Add(AddLabel(null, "HighlightTileAtRange (toggle HighlightTileAtRange on / off)", startX, startY));
            // ## BEGIN - END ## // VISUAL HELPERS
            // ## BEGIN - END ## // MISC2
            section.Add(AddLabel(null, "ToggleTransparentHouses (toggle TransparentHouses on / off)", startX, startY));
            section.Add(AddLabel(null, "ToggleInvisibleHouses (toggle InvisibleHouses on / off)", startX, startY));
            // ## BEGIN - END ## // MISC2
            // ## BEGIN - END ## // LINES
            section.Add(AddLabel(null, "UCCLinesToggleLT (toggle on / off UCC Lines LastTarget)", startX, startY));
            section.Add(AddLabel(null, "UCCLinesToggleHM (toggle on / off UCC Lines Hunting Mode)", startX, startY));
            // ## BEGIN - END ## // LINES
            // ## BEGIN - END ## // AUTOMATIONS
            section.Add(AddLabel(null, "AutoMeditate (toggle on / off automed)", startX, startY));
            section.Add(AddLabel(null, "Using AIBOT  (toggle on / off usingBot)", startX, startY));
            // ## BEGIN - END ## // AUTOMATIONS
            // ## BEGIN - END ## // MODERNCOOLDOWNBAR
            section.Add(AddLabel(null, "ToggleECBuffGump (toggle on / off EC alike buff gump)", startX, startY));
            section.Add(AddLabel(null, "ToggleECDebuffGump (toggle on / off EC alike debuff gump)", startX, startY));
            section.Add(AddLabel(null, "ToggleECStateGump (toggle on / off EC alike state gump)", startX, startY));
            section.Add(AddLabel(null, "ToggleModernCooldownBar (toggle on / off modern cooldown bar)", startX, startY));
            // ## BEGIN - END ## // MODERNCOOLDOWNBAR
            // ## BEGIN - END ## // MACROS
            SettingsSection section2 = AddSettingsSection(box, "-----SIMPLE MACROS-----");
            section2.Y = section.Bounds.Bottom + 40;

            section2.Add(AddLabel(null, "LastTargetRC (last target with custom range check)", startX, startY));
            section2.Add(AddLabel(null, "LastTargetRC - Range:", startX, startY));

            section2.AddRight(_lastTargetRange = AddHSlider(null, 1, 30, _currentProfile.LastTargetRange, startX, startY, 200));
            startY += _lastTargetRange.Height + 2;

            section2.Add(AddLabel(null, "ObjectInfo (macro for -info command)", startX, startY));
            section2.Add(AddLabel(null, "HideX (remove landtile, entity, mobile or item)", startX, startY));
            section2.Add(AddLabel(null, "HealOnHPChange (keep pressed, casts heal on own hp change)", startX, startY));
            section2.Add(AddLabel(null, "HarmOnSwing (keep pressed, casts harm on next own swing animation)", startX, startY));
            section2.Add(AddLabel(null, "CureGH (if poisoned cure, else greater heal)", startX, startY));
            section2.Add(AddLabel(null, "SetTargetClientSide (set target client side only)", startX, startY));
            section2.Add(AddLabel(null, "OpenJournal2 (opens second journal)", startX, startY));
            section2.Add(AddLabel(null, "OpenBackpack2 (opens second backpack, normal view)", startX, startY));
            section2.Add(AddLabel(null, "OpenCorpses (opens 0x2006 corpses within 2 tiles)", startX, startY));
            // ## BEGIN - END ## // MACROS
            // ## BEGIN - END ## // ADVMACROS
            SettingsSection section3 = AddSettingsSection(box, "-----ADVANCED MACROS-----");
            section3.Y = section2.Bounds.Bottom + 40;
            startY = section2.Bounds.Bottom + 40;
            section3.Add(AddLabel(null, "use macro to run advanced scripts ONCE:", startX, startY));
            section3.Add(AddLabel(null, "OpenCorpsesSafeLoot (opens non blue 0x2006 corpses within 2 tiles)", startX, startY));
            section3.Add(AddLabel(null, "EquipManager (equip an item)", startX, startY));
            section3.Add(AddLabel(null, "SetMimic_PlayerSerial (set master or custom serial for EquipManager)", startX, startY));
            section3.Add(AddLabel(null, "AutoPot (disarm 2h layer -> \n healpot below 85%, \n pouch if paralyzed, \n cure if poisoned and not full hp, \n refresh if below 23, \n str pot if str below 100, \n agi pot if dex above 89)", startX, startY));
            section3.Add(AddLabel(null, "DefendPartyKey (if ally / party member in 12 tile range and hits < 64: \n if targeting -> target them, else cast gheal \n if own hits < 64: \n if targeting -> target self and use gheal pot, \n else start casting gheal)", startX, startY));
            section3.Add(AddLabel(null, "DefendSelfKey (if own hits < 64, \n if targeting -> target self and use gheal pot, \n else start casting gheal)", startX, startY));
            section3.Add(AddLabel(null, "CustomInterrupt (fast macro to interrupt active spellcasting)", startX, startY));

            section3.Add(AddLabel(null, "GrabFriendlyBars (grab all innocent bars)", startX, startY));
            section3.Add
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
                    5000
                )
            );
            _pullFriendlyBarsX.SetText(_currentProfile.PullFriendlyBars.X.ToString());
            section3.AddRight(AddLabel(null, "X", 0, 0), 2);
            startY += _pullFriendlyBarsX.Height + 2;

            section3.Add
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
                    5000
                )
            );
            _pullFriendlyBarsY.SetText(_currentProfile.PullFriendlyBars.Y.ToString());
            section3.AddRight(AddLabel(null, "Y", 0, 0), 2);
            startY += _pullFriendlyBarsY.Height + 2;
            //
            section3.Add
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
                    5000
                )
            );
            _pullFriendlyBarsFinalLocationX.SetText(_currentProfile.PullFriendlyBarsFinalLocation.X.ToString());
            section3.AddRight(AddLabel(null, "X", 0, 0), 2);
            startY += _pullFriendlyBarsFinalLocationX.Height + 2;

            section3.Add
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
                    5000
                )
            );
            _pullFriendlyBarsFinalLocationY.SetText(_currentProfile.PullFriendlyBarsFinalLocation.Y.ToString());
            section3.AddRight(AddLabel(null, "Y", 0, 0), 2);
            startY += _pullFriendlyBarsFinalLocationY.Height + 2;

            section3.Add(AddLabel(null, "GrabEnemyBars (grab all criminal, enemy, gray, murderer bars)", startX, startY));
            section3.Add
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
                    5000
                )
            );
            _pullEnemyBarsX.SetText(_currentProfile.PullEnemyBars.X.ToString());
            section3.AddRight(AddLabel(null, "X", 0, 0), 2);
            startY += _pullEnemyBarsX.Height + 2;

            section3.Add
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
                    5000
                )
            );
            _pullEnemyBarsY.SetText(_currentProfile.PullEnemyBars.Y.ToString());
            section3.AddRight(AddLabel(null, "Y", 0, 0), 2);
            startY += _pullEnemyBarsY.Height + 2;
            //
            section3.Add
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
                    5000
                )
            );
            _pullEnemyBarsFinalLocationX.SetText(_currentProfile.PullEnemyBarsFinalLocation.X.ToString());
            section3.AddRight(AddLabel(null, "FX", 0, 0), 2);
            startY += _pullEnemyBarsFinalLocationX.Height + 2;

            section3.Add
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
                    5000
                )
            );
            _pullEnemyBarsFinalLocationY.SetText(_currentProfile.PullEnemyBarsFinalLocation.Y.ToString());
            section3.AddRight(AddLabel(null, "FY", 0, 0), 2);
            startY += _pullEnemyBarsFinalLocationY.Height + 2;

            section3.Add(AddLabel(null, "GrabPartyAllyBars (grab all ally and party bars)", startX, startY));
            section3.Add
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
                    5000
                )
            );
            _pullPartyAllyBarsX.SetText(_currentProfile.PullPartyAllyBars.X.ToString());
            section3.AddRight(AddLabel(null, "X", 0, 0), 2);
            startY += _pullPartyAllyBarsX.Height + 2;

            section3.Add
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
                    5000
                )
            );
            _pullPartyAllyBarsY.SetText(_currentProfile.PullPartyAllyBars.Y.ToString());
            section3.AddRight(AddLabel(null, "Y", 0, 0), 2);
            startY += _pullPartyAllyBarsY.Height + 2;
            //
            section3.Add
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
                    5000
                )
            );
            _pullPartyAllyBarsFinalLocationX.SetText(_currentProfile.PullPartyAllyBarsFinalLocation.X.ToString());
            section3.AddRight(AddLabel(null, "FX", 0, 0), 2);
            startY += _pullPartyAllyBarsFinalLocationX.Height + 2;

            section3.Add
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
                    5000
                )
            );
            _pullPartyAllyBarsFinalLocationY.SetText(_currentProfile.PullPartyAllyBarsFinalLocation.Y.ToString());
            section3.AddRight(AddLabel(null, "FY", 0, 0), 2);
            startY += _pullPartyAllyBarsFinalLocationY.Height + 2;
            // ## BEGIN - END ## // ADVMACROS
            // ## BEGIN - END ## // AUTOMATIONS
            SettingsSection section4 = AddSettingsSection(box, "-----AUTOMATIONS-----");
            section4.Y = section3.Bounds.Bottom + 40;
            startY = section3.Bounds.Bottom + 40;

            section4.Add(AddLabel(null, "write in chat or use macro to enable / disable:", startX, startY));
            section4.Add(AddLabel(null, "(runs in background until disabled)", startX, startY));
            section4.Add(AddLabel(null, "-automed or macro AutoMeditate (auto meditates \n with 2.5s delay and not targeting)", startX, startY));
            section4.Add(AddLabel(null, "-aibot or macro aibot (auto bot \n)", startX, startY));
            section4.Add(AddLabel(null, "-engange (auto engage and pathfind to last target)", startX, startY));

            SettingsSection section5 = AddSettingsSection(box, "-----MISC-----");
            section5.Y = section4.Bounds.Bottom + 40;
            startY = section4.Bounds.Bottom + 40;

            section5.Add(AddLabel(null, "write in chat to enable / disable:", startX, startY));
            section5.Add(AddLabel(null, "-mimic (mimic harmful spells 1:1, on beneficial macro defendSelf/defendParty)", startX, startY));
            section5.Add(AddLabel(null, "Macro: SetMimic_PlayerSerial (define the player to mimic)", startX, startY));
            section5.Add(AddLabel(null, "-marker X Y (place a dot and line to X Y on world map \n use -marker to remove it)", startX, startY));
            section5.Add(_autoWorldmapMarker = AddCheckBox(null, "Auto add marker for MapGumps (ie. T-Maps)", _currentProfile.AutoWorldmapMarker, startX, startY));
            startY += _autoWorldmapMarker.Height + 2;
            section5.Add(AddLabel(null, "-df (if GreaterHeal cursor is up and you or a party member \n " +
                                                "gets hit by EB, Explor or FS \n " +
                                                "and your or the party members condition is met \n " +
                                                "greater heal will be cast on you or party member \n " +
                                                "Condition: Poisoned and HP smaller than random between 65 - 80 \n " +
                                                "Condition: HP smaller than random between 40-70)", startX, startY));

            //
            section5.Add(AddLabel(null, "-autorange (show range depending on archery equipment)", startX, startY));
            section5.Add(AddLabel(null, "(configure range for every ranged weapon in the autorange.txt file!)", startX, startY));
            section5.Add(_autoRangeDisplayAlways = AddCheckBox(null, "always have -autorange ON", _currentProfile.AutoRangeDisplayAlways, startX, startY));
            startY += _autoRangeDisplayAlways.Height + 2;
            section5.Add(_autoRangeDisplayHue = AddHueDisplay(null, startX, startY, _currentProfile.AutoRangeDisplayHue, ""));
            startY += _autoRangeDisplayHue.Height + 2;
            section5.AddRight(AddLabel(null, "Hue", 0, 0), 2);
            //
            // ## BEGIN - END ## // AUTOMATIONS
            // ## BEGIN - END ## // OUTLANDS
            /*
            section7.Add(_infernoBridge = AddCheckBox(null, "Solve Inferno bridge (needs relog)", ProfileManager.CurrentProfile.InfernoBridge, startX, startY));
            startY += _infernoBridge.Height + 2;

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

            section3.Add(_uccDoH = AddCheckBox(null, "Show Do Hamstring Line (Outlands)", ProfileManager.CurrentProfile.UOClassicCombatBuffbar_DoHEnabled, startX, startY));
            startY += _uccDoH.Height + 2;
            section3.Add(_uccGotH = AddCheckBox(null, "Show Got Hamstung Line (Outlands)", ProfileManager.CurrentProfile.UOClassicCombatBuffbar_GotHEnabled, startX, startY));
            startY += _uccGotH.Height + 2;

            */
            // ## BEGIN - END ## // OUTLANDS
            // ## BEGIN - END ## // LOBBY
            SettingsSection section6 = AddSettingsSection(box, "-----LOBBY-----");
            section6.Y = section5.Bounds.Bottom + 40;
            startY = section5.Bounds.Bottom + 40;

            section6.Add(AddLabel(null, "write in chat to enable / disable:", startX, startY));
            section6.Add(AddLabel(null, "-lobby help (show help menu)", startX, startY));
            section6.Add(AddLabel(null, "-lobby status (show status)", startX, startY));
            section6.Add(AddLabel(null, "-lobby connect <IP> (connect to IP)", startX, startY));
            section6.Add(AddLabel(null, "-lobby diconnect (disconnect)", startX, startY));
            section6.Add(AddLabel(null, "-lobby target (send your lasttarget to be everyones)", startX, startY));
            section6.Add(AddLabel(null, "-lobby cast <SPELLNAME> (makes everyone cast a spell)", startX, startY));
            section6.Add(AddLabel(null, "-lobby drop (drop everyones spell on last target)", startX, startY));
            section6.Add(AddLabel(null, "-lobby attack (send your lasttarget to be everyones and everyone attacks it)", startX, startY));

            section6.Add(AddLabel(null, "-autohid ((needs connected lobby) broadcast your position when hidden)", startX, startY));

            section6.Add(AddLabel(null, "Macro: LobbyConnect (connect to IP and Port)", startX, startY));
            section6.Add
            (
                _lobbyIP = AddInputField
                (
                    null,
                    startX, startY,
                    150,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    false,
                    99999999
                )
            );
            _lobbyIP.SetText(_currentProfile.LobbyIP.ToString());

            startY += _lobbyIP.Height + 2;
            section6.AddRight(AddLabel(null, "Lobby IP", 0, 0), 2);

            section6.Add
            (
                _lobbyPort = AddInputField
                (
                    null,
                    startX, startY,
                    50,
                    TEXTBOX_HEIGHT,
                    null,
                    80,
                    false,
                    true,
                    99999999
                )
            );
            _lobbyPort.SetText(_currentProfile.LobbyPort.ToString());
            startY += _lobbyPort.Height + 2;
            section6.AddRight(AddLabel(null, "Lobby Port", 0, 0), 2);

            section6.Add(AddLabel(null, "Macro: LobbyDisconnect (disconnect)", startX, startY));
            section6.Add(AddLabel(null, "Macro: LobbyTarget (send your lasttarget to be everyones)", startX, startY));
            section6.Add(AddLabel(null, "Macro: LobbyCastLightning (everyone casts Lightning)", startX, startY));
            section6.Add(AddLabel(null, "Macro: LobbyCastEB (everyone casts Energy Bolt)", startX, startY));
            section6.Add(AddLabel(null, "Macro: LobbyDrop (everyone drops spell on target)", startX, startY));
            // ## BEGIN - END ## // LOBBY

            Add(rightArea, PAGE);
        }
        // ## BEGIN - END ## // TAZUO
        // ## BEGIN - END ## // BASICSETUP

        public override void OnButtonClick(int buttonID)
        {
            if (buttonID == (int)Buttons.Last + 1)
            {
                // it's the macro buttonssss
                return;
            }

            switch ((Buttons)buttonID)
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
                case Buttons.OpenIgnoreList:
                    UIManager.GetGump<IgnoreManagerGump>()?.Dispose();
                    UIManager.Add(new IgnoreManagerGump());
                    break;

                case Buttons.SaveAsDefault:
                    Apply();
                    ProfileManager.SetProfileAsDefault(ProfileManager.CurrentProfile);
                    GameActions.Print("Current settings saved as default for new characters.");
                    break;

                case Buttons.OverrideAllProfiles:
                {
                    Apply();
                    var allPaths = ProfileManager.GetAllProfilePaths();
                    ProfileManager.OverrideProfiles(ProfileManager.CurrentProfile, allPaths);
                    GameActions.Print($"Settings copied to {allPaths.Count} other profile(s).");
                    break;
                }

                case Buttons.OverrideSameServer:
                {
                    Apply();
                    var serverPaths = ProfileManager.GetSameServerProfilePaths();
                    ProfileManager.OverrideProfiles(ProfileManager.CurrentProfile, serverPaths);
                    GameActions.Print($"Settings copied to {serverPaths.Count} profile(s) on same server.");
                    break;
                }
            }
        }

        private void SetDefault()
        {
            switch (ActivePage)
            {
                case 1: // general
                    _sliderFPS.Value = 60;
                    _reduceFPSWhenInactive.IsChecked = true;
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
                    if (_fastRotation != null) _fastRotation.IsChecked = false;
                    _showHpMobile.IsChecked = false;
                    _hpComboBox.SelectedIndex = 0;
                    _hpComboBoxShowWhen.SelectedIndex = 0;
                    _highlightByPoisoned.IsChecked = true;
                    _highlightByParalyzed.IsChecked = true;
                    _highlightByInvul.IsChecked = true;
                    _poisonColorPickerBox.Hue = 0x0044;
                    _paralyzedColorPickerBox.Hue = 0x014C;
                    _invulnerableColorPickerBox.Hue = 0x0030;
                    _drawRoofs.IsChecked = false;
                    _enableCaveBorder.IsChecked = false;
                    // ## BEGIN - END ## // ART / HUE CHANGES
                    //_treeToStumps.IsChecked = false;
                    // ## BEGIN - END ## // ART / HUE CHANGES
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
                    _showTargetRangeIndicator.IsChecked = false;
                    _autoAvoidObstacules.IsChecked = true;
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
                    _journalOpacity.Value = 50;

                    _showSkillsMessage.IsChecked = true;
                    _showSkillsMessageDelta.Value = 1;
                    _showStatsMessage.IsChecked = true;

                    _dragSelectStartX.Value = 100;
                    _dragSelectStartY.Value = 100;
                    _dragSelectAsAnchor.IsChecked = false;
                    _hiddenBodyAlpha.Value = 40;
                    _hiddenBodyHue.Hue = 0x038E;

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
                    if (_windowTitleStyle != null)
                    {
                        _windowTitleStyle.SelectedIndex = 0;
                    }
                    _zoomCheckbox.IsChecked = false;
                    _restorezoomCheckbox.IsChecked = false;
                    _gameWindowWidth.SetText(Constants.MIN_GAME_WINDOW_WIDTH.ToString());
                    _gameWindowHeight.SetText(Constants.MIN_GAME_WINDOW_HEIGHT.ToString());
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
                    _lightLevelType.SelectedIndex = 0;
                    _useColoredLights.IsChecked = false;
                    _darkNights.IsChecked = false;
                    _enableShadows.IsChecked = true;
                    _enableShadowsStatics.IsChecked = true;
                    _terrainShadowLevel.Value = 15;
                    _runMouseInSeparateThread.IsChecked = true;
                    _auraMouse.IsChecked = true;
                    _partyAura.IsChecked = true;
                    _animatedWaterEffect.IsChecked = false;
                    _partyAuraColorPickerBox.Hue = 0x0044;

                    break;

                case 4: // macros
                    break;

                case 5: // tooltip
                    _use_tooltip.IsChecked = true;
                    _tooltip_font_hue.Hue = 0xFFFF;
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
                    _speechColorPickerBox.Hue = 0x02B2;
                    _emoteColorPickerBox.Hue = 0x0021;
                    _yellColorPickerBox.Hue = 0x0021;
                    _whisperColorPickerBox.Hue = 0x0033;
                    _partyMessageColorPickerBox.Hue = 0x0044;
                    _guildMessageColorPickerBox.Hue = 0x0044;
                    _allyMessageColorPickerBox.Hue = 0x0057;
                    _chatMessageColorPickerBox.Hue = 0x0256;
                    _chatAfterEnter.IsChecked = false;
                    UIManager.SystemChat.IsActive = !_chatAfterEnter.IsChecked;
                    _chatAdditionalButtonsCheckbox.IsChecked = true;
                    _chatShiftEnterCheckbox.IsChecked = true;
                    _saveJournalCheckBox.IsChecked = false;
                    _hideChatGradient.IsChecked = false;
                    _ignoreGuildMessages.IsChecked = false;
                    _ignoreAllianceMessages.IsChecked = false;

                    break;

                case 8: // combat
                    _innocentColorPickerBox.Hue = 0x005A;
                    _friendColorPickerBox.Hue = 0x0044;
                    _crimialColorPickerBox.Hue = 0x03b2;
                    _canAttackColorPickerBox.Hue = 0x03b2;
                    _murdererColorPickerBox.Hue = 0x0023;
                    _enemyColorPickerBox.Hue = 0x0031;
                    _queryBeforAttackCheckbox.IsChecked = true;
                    _queryBeforeBeneficialCheckbox.IsChecked = false;
                    _uiButtonsSingleClick.IsChecked = false;
                    _buffBarTime.IsChecked = false;
                    _enableFastSpellsAssign.IsChecked = false;
                    _beneficColorPickerBox.Hue = 0x0059;
                    _harmfulColorPickerBox.Hue = 0x0020;
                    _neutralColorPickerBox.Hue = 0x03b2;
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
                    _containerOpacity.Value = 50;
                    _containersScale.Value = 100;
                    _containerScaleItems.IsChecked = false;
                    _useGridLayoutContainerGumps.IsChecked = true;
                    _useLargeContianersGumps.IsChecked = false;
                    _containerDoubleClickToLoot.IsChecked = false;
                    _relativeDragAnDropItems.IsChecked = false;
                    _highlightContainersWhenMouseIsOver.IsChecked = false;
                    _overrideContainerLocation.IsChecked = false;
                    _overrideContainerLocationSetting.SelectedIndex = 0;
                    _backpackStyle.SelectedIndex = 0;
                    _hueContainerGumps.IsChecked = true;

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

            if (_currentProfile.JournalStyle != _journalStyle.SelectedIndex)
            {
                _currentProfile.JournalStyle = _journalStyle.SelectedIndex;
                UIManager.GetGump<ResizableJournal>()?.BuildBorder();
            }

            if (_currentProfile.MainWindowBackgroundHue != _mainWindowHuePicker.Hue)
            {
                _currentProfile.MainWindowBackgroundHue = _mainWindowHuePicker.Hue;
                GameController.UpdateBackgroundHueShader();
            }

            _currentProfile.CloseHealthBarIfAnchored = _closeHPBarWhenAnchored.IsChecked;
            _currentProfile.EnableNearbyItemGump = _enableNearbyItemGump.IsChecked;
            _currentProfile.UseLastMovedCooldownPosition = _uselastCooldownPosition.IsChecked;

            _currentProfile.PlayerConstantAlpha = _regularPlayerAlpha.Value;


            _currentProfile.DisplaySkillBarOnChange = _skillProgressBarOnChange.IsChecked;
            _currentProfile.SkillBarFormat = _skillProgressBarFormat.Text;

            if (_tooltipHeaderFormat.Text.Length > 0)
                _currentProfile.TooltipHeaderFormat = _tooltipHeaderFormat.Text;

            _currentProfile.ToolTipBGHue = _tooltipBGHue.Hue;

            _currentProfile.OpenHealthBarForLastAttack = _openHealthBarForLastAttack.IsChecked;
            _currentProfile.HealthLineSizeMultiplier = _healthLineSizeMultiplier.Value;

            _currentProfile.MaxJournalEntries = _maxJournalEntries.Value;

            if (_currentProfile.HideJournalBorder != _hideJournalBorder.IsChecked)
            {
                _currentProfile.HideJournalBorder = _hideJournalBorder.IsChecked;
                UIManager.GetGump<ResizableJournal>()?.BuildBorder();
            }
            _currentProfile.HideJournalTimestamp = _hideJournalTimestamp.IsChecked;

            _currentProfile.ForceCenterAlignTooltipMobiles = _forceCenterAlignMobileTooltips.IsChecked;

            _currentProfile.UseModernShopGump = _useModernShop.IsChecked;

            _currentProfile.OverheadChatWidth = _overheadTextWidth.Value;

            _currentProfile.EnableAlphaScrollingOnGumps = _enableAlphaScrollWheel.IsChecked;
            _currentProfile.SpellIcon_DisplayHotkey = _spellIconDisplayHotkey.IsChecked;
            _currentProfile.SpellIcon_HotkeyHue = _spellIconHotkeyHue.Hue;
            _currentProfile.SpellIconScale = _spellIconScale.Value;

            _currentProfile.DisplayPartyChatOverhead = false;

            _currentProfile.EnableHealthIndicator = _enableHealthIndicator.IsChecked;
            if (int.TryParse(_healthIndicatorPercentage.Text, out int hpPercent))
                _currentProfile.ShowHealthIndicatorBelow = (float)hpPercent / 100f;
            if (int.TryParse(_healthIndicatorWidth.Text, out int healthWidth))
                _currentProfile.HealthIndicatorWidth = healthWidth;

            _currentProfile.PathfindSingleClick = _pathFindSingleClick.IsChecked;

            _currentProfile.DragSelect_PlayersModifier = _dragSelectPlayersModifier.SelectedIndex;
            _currentProfile.DragSelect_MonstersModifier = _dragSelectMonsertModifier.SelectedIndex;
            _currentProfile.DragSelect_NameplateModifier = _dragSelectNameplateModifier.SelectedIndex;

            _currentProfile.NamePlateHideAtFullHealthInWarmode = _namePlateHealthOnlyWarmode.IsChecked;

            _currentProfile.LeftAlignToolTips = _leftAlignToolTips.IsChecked;

            _currentProfile.NamePlateHideAtFullHealth = _namePlateShowAtFullHealth.IsChecked;

            _currentProfile.DamageHueSelf = _damageHueSelf.Hue;
            _currentProfile.DamageHuePet = _damageHuePet.Hue;
            _currentProfile.DamageHueAlly = _damageHueAlly.Hue;
            _currentProfile.DamageHueLastAttck = _damageHueLastAttack.Hue;

            if (_currentProfile.EnableGridContainerAnchor != _gridContainerAnchorable.IsChecked)
            {
                _currentProfile.EnableGridContainerAnchor = _gridContainerAnchorable.IsChecked;
                foreach (GridContainer _ in UIManager.Gumps.OfType<GridContainer>())
                {
                    _.AnchorType = _currentProfile.EnableGridContainerAnchor ? ANCHOR_TYPE.NONE : ANCHOR_TYPE.DISABLED;
                }
            }
            if (_currentProfile.UseImprovedBuffBar != _enableImprovedBuffGump.IsChecked)
            {
                _currentProfile.UseImprovedBuffBar = _enableImprovedBuffGump.IsChecked;
                if (_currentProfile.UseImprovedBuffBar)
                {
                    foreach (Gump g in UIManager.Gumps.OfType<BuffGump>())
                        g.Dispose();
                    UIManager.Add(new ImprovedBuffGump());
                }
                else
                {
                    foreach (Gump g in UIManager.Gumps.OfType<ImprovedBuffGump>())
                        g.Dispose();
                    UIManager.Add(new BuffGump(100, 100));
                }
            }

            if (_currentProfile.Grid_BorderStyle != _gridBorderStyle.SelectedIndex || _currentProfile.Grid_HideBorder != _gridHideBorder.IsChecked)
            {
                _currentProfile.Grid_BorderStyle = _gridBorderStyle.SelectedIndex;
                _currentProfile.Grid_HideBorder = _gridHideBorder.IsChecked;
                foreach (GridContainer gridContainer in UIManager.Gumps.OfType<GridContainer>())
                {
                    gridContainer.BuildBorder();
                }
            }


            _currentProfile.ImprovedBuffBarHue = _improvedBuffBarHue.Hue;

            _currentProfile.DisableSystemChat = _disableSystemChat.IsChecked;
            _currentProfile.JournalMessagesOnlyInJournalBox = _journalMessagesOnlyInJournalBox.IsChecked;
            _currentProfile.GridBorderAlpha = (byte)_gridBorderOpacity.Value;
            _currentProfile.GridBorderHue = _gridBorderHue.Hue;
            _currentProfile.GridContainersScale = (byte)_gridContainerScale.Value;
            _currentProfile.NamePlateHealthBar = _namePlateHealthBar.IsChecked;
            _currentProfile.NamePlateOpacity = (byte)_namePlateOpacity.Value;
            _currentProfile.NamePlateBorderOpacity = (byte)_nameplateBorderOpacity.Value;
            _currentProfile.NamePlateHealthBarOpacity = (byte)_namePlateHealthBarOpacity.Value;
            _currentProfile.GridContainerSearchMode = _gridContainerSearchAlternative.SelectedIndex;
            _currentProfile.GridContainerScaleItems = _gridContainerItemScale.IsChecked;
            _currentProfile.GridEnableContPreview = _gridContainerPreview.IsChecked;
            _currentProfile.Grid_DefaultColumns = int.Parse(_gridDefaultColumns.Text);
            _currentProfile.Grid_DefaultRows = int.Parse(_gridDefaultRows.Text);
            _currentProfile.Grid_UseContainerHue = _gridOverrideWithContainerHue.IsChecked;
            _currentProfile.GridHightlightSize = _gridHightlightLineSize.Value;

            {
                _currentProfile.CoolDownX = int.Parse(_coolDownX.Text);
                _currentProfile.CoolDownY = int.Parse(_coolDownY.Text);
            } //Cooldown bars

            _currentProfile.HighlightGameObjects = _highlightObjects.IsChecked;
            _currentProfile.ReduceFPSWhenInactive = _reduceFPSWhenInactive.IsChecked;
            _currentProfile.EnablePathfind = _enablePathfind.IsChecked;
            _currentProfile.UseShiftToPathfind = _useShiftPathfind.IsChecked;
            _currentProfile.AlwaysRun = _alwaysRun.IsChecked;
            _currentProfile.AlwaysRunUnlessHidden = _alwaysRunUnlessHidden.IsChecked;
            _currentProfile.FastRotation = _fastRotation?.IsChecked ?? _currentProfile.FastRotation;
            Pathfinder.FastRotation = _currentProfile.FastRotation;
            _currentProfile.ShowMobilesHP = _showHpMobile.IsChecked;
            _currentProfile.HighlightMobilesByPoisoned = _highlightByPoisoned.IsChecked;
            _currentProfile.HighlightMobilesByParalize = _highlightByParalyzed.IsChecked;
            _currentProfile.HighlightMobilesByInvul = _highlightByInvul.IsChecked;
            _currentProfile.PoisonHue = _poisonColorPickerBox.Hue;
            _currentProfile.ParalyzedHue = _paralyzedColorPickerBox.Hue;
            _currentProfile.InvulnerableHue = _invulnerableColorPickerBox.Hue;
            _currentProfile.MobileHPType = _hpComboBox.SelectedIndex;
            _currentProfile.MobileHPShowWhen = _hpComboBoxShowWhen.SelectedIndex;
            _currentProfile.HoldDownKeyTab = _holdDownKeyTab.IsChecked;
            _currentProfile.HoldDownKeyAltToCloseAnchored = _holdDownKeyAlt.IsChecked;

            _currentProfile.CloseAllAnchoredGumpsInGroupWithRightClick = _closeAllAnchoredGumpsWithRClick.IsChecked;

            _currentProfile.HoldShiftForContext = _holdShiftForContext.IsChecked;
            _currentProfile.HoldAltToMoveGumps = _holdAltToMoveGumps.IsChecked;
            _currentProfile.HoldShiftToSplitStack = _holdShiftToSplitStack.IsChecked;
            _currentProfile.CloseHealthBarType = _healtbarType.SelectedIndex;
            _currentProfile.HideScreenshotStoredInMessage = _hideScreenshotStoredInMessage.IsChecked;

            _currentProfile.HiddenBodyHue = _hiddenBodyHue.Hue;
            _currentProfile.HiddenBodyAlpha = (byte)_hiddenBodyAlpha.Value;

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

            // ## BEGIN - END ## // ART / HUE CHANGES
            /*
            if (_currentProfile.TreeToStumps != _treeToStumps.IsChecked)
            {
                StaticFilters.CleanTreeTextures();
                _currentProfile.TreeToStumps = _treeToStumps.IsChecked;
            }
            */
            // ## BEGIN - END ## // ART / HUE CHANGES

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

            _currentProfile.AltJournalBackgroundHue = _journalBackgroundColor.Hue;

            if (_currentProfile.AltGridContainerBackgroundHue != _altGridContainerBackgroundHue.Hue)
            {
                _currentProfile.AltGridContainerBackgroundHue = _altGridContainerBackgroundHue.Hue;
                foreach (GridContainer _ in UIManager.Gumps.OfType<GridContainer>())
                {
                    _.OptionsUpdated();
                }
            }

            if (_currentProfile.ContainerOpacity != (byte)_containerOpacity.Value)
            {
                _currentProfile.ContainerOpacity = (byte)_containerOpacity.Value;
                foreach (GridContainer _ in UIManager.Gumps.OfType<GridContainer>())
                {
                    _.OptionsUpdated();
                }
            }

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

            if (_currentProfile.JournalOpacity != _journalOpacity.Value)
            {
                _currentProfile.JournalOpacity = (byte)_journalOpacity.Value;
                UIManager.GetGump<ResizableJournal>()?.RequestUpdateContents();
            }

            if (_currentProfile.ShowHouseContent != _showHouseContent.IsChecked)
            {
                _currentProfile.ShowHouseContent = _showHouseContent.IsChecked;
                NetClient.Socket.Send_ShowPublicHouseContent(_currentProfile.ShowHouseContent);
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

            Client.Game.Audio.UpdateCurrentMusicVolume();
            Client.Game.Audio.UpdateCurrentSoundsVolume();

            if (!_currentProfile.EnableMusic)
            {
                Client.Game.Audio.StopMusic();
            }

            if (!_currentProfile.EnableSound)
            {
                Client.Game.Audio.StopSounds();
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

            var camera = Client.Game.Scene.Camera;
            _currentProfile.DefaultScale = camera.Zoom = (_sliderZoom.Value * camera.ZoomStep) + camera.ZoomMin;

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

            if (gameWindowSizeWidth != Client.Game.Scene.Camera.Bounds.Width || gameWindowSizeHeight != Client.Game.Scene.Camera.Bounds.Height)
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

            if (gameWindowPositionX != camera.Bounds.X || gameWindowPositionY != camera.Bounds.Y)
            {
                if (vp != null)
                {
                    vp.SetGameWindowPosition(new Point(gameWindowPositionX, gameWindowPositionY));
                    _currentProfile.GameWindowPosition = vp.Location;
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

            if (_currentProfile.GameWindowFullSize != _gameWindowFullsize.IsChecked)
            {
                Point n = Point.Zero, loc = Point.Zero;

                if (_gameWindowFullsize.IsChecked)
                {
                    if (vp != null)
                    {
                        n = vp.ResizeGameWindow(new Point(Client.Game.Window.ClientBounds.Width, Client.Game.Window.ClientBounds.Height));
                        vp.SetGameWindowPosition(new Point(-5, -5));
                        _currentProfile.GameWindowPosition = vp.Location;
                    }
                }
                else
                {
                    if (vp != null)
                    {
                        n = vp.ResizeGameWindow(new Point(Constants.MIN_GAME_WINDOW_WIDTH, Constants.MIN_GAME_WINDOW_HEIGHT));
                        vp.SetGameWindowPosition(new Point(20, 20));
                        _currentProfile.GameWindowPosition = vp.Location;
                    }
                }

                _gameWindowPositionX.SetText(loc.X.ToString());
                _gameWindowPositionY.SetText(loc.Y.ToString());
                _gameWindowWidth.SetText(n.X.ToString());
                _gameWindowHeight.SetText(n.Y.ToString());

                _currentProfile.GameWindowFullSize = _gameWindowFullsize.IsChecked;
            }

            bool wasUsingCustomWindowTitleBar = _currentProfile.UsesCustomWindowTitleBar();

            _currentProfile.WindowBorderless = _windowBorderless.IsChecked;
            _currentProfile.EnableWindowBorderFrame = _enableWindowBorderFrame.IsChecked;
            _currentProfile.WindowTitleStyle = _windowBorderless.IsChecked && _enableWindowBorderFrame.IsChecked
                ? GetWindowTitleStyleFromIndex(_windowTitleStyle?.SelectedIndex ?? 0)
                : WindowTitleBarStyle.CUO;

            bool useCustomWindowTitleBar = _currentProfile.UsesCustomWindowTitleBar();

            // Dispose existing custom title bar before switching
            UIManager.GetGump<TopStatusBarGump>()?.Dispose();

            // SetWindowBorderless handles border toggle + window resize/reposition
            Client.Game.SetWindowBorderless(useCustomWindowTitleBar);

            if (useCustomWindowTitleBar)
            {
                TopStatusBarGump.Create();
            }

            if (wasUsingCustomWindowTitleBar != useCustomWindowTitleBar)
            {
                InWindowTitleBarBarsGump.UpdateVisibility();
            }

            _currentProfile.UseAlternativeLights = _altLights.IsChecked;
            _currentProfile.UseCustomLightLevel = _enableLight.IsChecked;
            InWindowTitleBarBarsGump.UpdateVisibility();
            WindowBorderFrameGump.UpdateVisibility();
            _currentProfile.LightLevel = (byte)(_lightBar.MaxValue - _lightBar.Value);
            _currentProfile.LightLevelType = _lightLevelType.SelectedIndex;

            if (_enableLight.IsChecked)
            {
                World.Light.Overall = _currentProfile.LightLevelType == 1 ? Math.Min(World.Light.RealOverall, _currentProfile.LightLevel) : _currentProfile.LightLevel;
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
            _currentProfile.TerrainShadowsLevel = _terrainShadowLevel.Value;
            _currentProfile.AuraUnderFeetType = _auraType.SelectedIndex;

            Client.Game.IsMouseVisible = Settings.GlobalSettings.RunMouseInASeparateThread = _runMouseInSeparateThread.IsChecked;

            _currentProfile.AuraOnMouse = _auraMouse.IsChecked;
            _currentProfile.AnimatedWaterEffect = _animatedWaterEffect.IsChecked;
            _currentProfile.PartyAura = _partyAura.IsChecked;
            _currentProfile.PartyAuraHue = _partyAuraColorPickerBox.Hue;
            _currentProfile.HideChatGradient = _hideChatGradient.IsChecked;
            _currentProfile.IgnoreGuildMessages = _ignoreGuildMessages.IsChecked;
            _currentProfile.IgnoreAllianceMessages = _ignoreAllianceMessages.IsChecked;

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
            _currentProfile.CastSpellsByOneClick = _uiButtonsSingleClick.IsChecked;
            _currentProfile.BuffBarTime = _buffBarTime.IsChecked;
            _currentProfile.FastSpellsAssign = _enableFastSpellsAssign.IsChecked;

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

            if (!int.TryParse(_rows.Text, out int v))
            {
                v = 1;
                _rows.SetText("1");
            }

            _currentProfile.CounterBarRows = v;

            if (!int.TryParse(_columns.Text, out v))
            {
                v = 1;
                _columns.SetText("1");
            }
            _currentProfile.CounterBarColumns = v;
            _currentProfile.CounterBarHighlightOnUse = _highlightOnUse.IsChecked;

            if (!int.TryParse(_highlightAmount.Text, out v))
            {
                v = 5;
                _highlightAmount.SetText("5");
            }
            _currentProfile.CounterBarHighlightAmount = v;

            if (!int.TryParse(_abbreviatedAmount.Text, out v))
            {
                v = 1000;
                _abbreviatedAmount.SetText("1000");
            }
            _currentProfile.CounterBarAbbreviatedAmount = v;
            _currentProfile.CounterBarHighlightOnAmount = _highlightOnAmount.IsChecked;
            _currentProfile.CounterBarDisplayAbbreviatedAmount = _enableAbbreviatedAmount.IsChecked;

            CounterBarGump counterGump = UIManager.GetGump<CounterBarGump>();

            counterGump?.SetLayout(_currentProfile.CounterBarCellSize, _currentProfile.CounterBarRows, _currentProfile.CounterBarColumns);


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
                                200,
                                200,
                                _currentProfile.CounterBarCellSize,
                                _currentProfile.CounterBarRows,
                                _currentProfile.CounterBarColumns
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
            //_currentProfile.DragSelectHumanoidsOnly = _dragSelectHumanoidsOnly.IsChecked;
            _currentProfile.DragSelectStartX = _dragSelectStartX.Value;
            _currentProfile.DragSelectStartY = _dragSelectStartY.Value;
            _currentProfile.DragSelectAsAnchor = _dragSelectAsAnchor.IsChecked;

            _currentProfile.ShowSkillsChangedMessage = _showSkillsMessage.IsChecked;
            _currentProfile.ShowSkillsChangedDeltaValue = _showSkillsMessageDelta.Value;
            _currentProfile.ShowStatsChangedMessage = _showStatsMessage.IsChecked;

            _currentProfile.OverrideContainerLocation = _overrideContainerLocation.IsChecked;
            _currentProfile.OverrideContainerLocationSetting = _overrideContainerLocationSetting.SelectedIndex;

            _currentProfile.ShowTargetRangeIndicator = _showTargetRangeIndicator.IsChecked;
            _currentProfile.AutoAvoidObstacules = _autoAvoidMobiles.IsChecked;

            bool updateHealthBars = _currentProfile.CustomBarsToggled != _customBars.IsChecked;
            _currentProfile.CustomBarsToggled = _customBars.IsChecked;

            if (updateHealthBars)
            {
                if (_currentProfile.CustomBarsToggled)
                {
                    List<HealthBarGump> hbgstandard = UIManager.Gumps.OfType<HealthBarGump>().ToList();

                    foreach (HealthBarGump healthbar in hbgstandard)
                    {
                        UIManager.Add(new HealthBarGumpCustom(healthbar.LocalSerial) { X = healthbar.X, Y = healthbar.Y });

                        healthbar.Dispose();
                    }
                }
                else
                {
                    List<HealthBarGumpCustom> hbgcustom = UIManager.Gumps.OfType<HealthBarGumpCustom>().ToList();

                    foreach (HealthBarGumpCustom customhealthbar in hbgcustom)
                    {
                        UIManager.Add(new HealthBarGump(customhealthbar.LocalSerial) { X = customhealthbar.X, Y = customhealthbar.Y });

                        customhealthbar.Dispose();
                    }
                }
            }

            _currentProfile.CBBlackBGToggled = _customBarsBBG.IsChecked;
            _currentProfile.SaveHealthbars = _saveHealthbars.IsChecked;


            // infobar
            _currentProfile.ShowInfoBar = _showInfoBar.IsChecked;
            _currentProfile.ShowHPInTitleBar = _showHPInTitleBar?.IsChecked ?? _currentProfile.ShowHPInTitleBar;
            _currentProfile.InfoBarHighlightType = _infoBarHighlightType.SelectedIndex;
            if (World.InGame && World.Player != null)
                Client.Game.SetWindowTitle(World.Player.Name);

            if (_actionBarEnabled != null)
                _currentProfile.ActionBarEnabled = _actionBarEnabled.IsChecked;


            InfoBarManager ibmanager = Client.Game.GetScene<GameScene>().InfoBars;

            ibmanager.Clear();

            for (int i = 0; i < _infoBarBuilderControls.Count; i++)
            {
                if (!_infoBarBuilderControls[i].IsDisposed)
                {
                    ibmanager.AddItem(new InfoBarItem(_infoBarBuilderControls[i].LabelText, _infoBarBuilderControls[i].Var, _infoBarBuilderControls[i].Hue));
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

            if (_actionBarEnabled != null)
            {
                var actionBarGump = UIManager.GetGump<ActionBarGump>();
                if (_currentProfile.ActionBarEnabled)
                {
                    if (actionBarGump == null)
                        UIManager.Add(new ActionBarGump());
                    else
                        actionBarGump.RefreshSlots();
                }
                else
                    actionBarGump?.Dispose();
            }

            // containers


            int containerScale = _currentProfile.ContainersScale;

            if ((byte)_containersScale.Value != containerScale || _currentProfile.ScaleItemsInsideContainers != _containerScaleItems.IsChecked)
            {
                containerScale = _currentProfile.ContainersScale = (byte)_containersScale.Value;
                UIManager.ContainerScale = containerScale / 100f;
                _currentProfile.ScaleItemsInsideContainers = _containerScaleItems.IsChecked;

                foreach (ContainerGump resizableGump in UIManager.Gumps.OfType<ContainerGump>())
                {
                    resizableGump.RequestUpdateContents();
                }
            }

            _currentProfile.UseGridLayoutContainerGumps = _useGridLayoutContainerGumps.IsChecked;
            _currentProfile.UseLargeContainerGumps = _useLargeContianersGumps.IsChecked;
            _currentProfile.DoubleClickToLootInsideContainers = _containerDoubleClickToLoot.IsChecked;
            _currentProfile.RelativeDragAndDropItems = _relativeDragAnDropItems.IsChecked;
            _currentProfile.HighlightContainerWhenSelected = _highlightContainersWhenMouseIsOver.IsChecked;
            _currentProfile.HueContainerGumps = _hueContainerGumps.IsChecked;


            if (_currentProfile.BackpackStyle != _backpackStyle.SelectedIndex)
            {
                _currentProfile.BackpackStyle = _backpackStyle.SelectedIndex;
                UIManager.GetGump<PaperDollGump>(World.Player.Serial)?.RequestUpdateContents();
                Item backpack = World.Player.FindItemByLayer(Layer.Backpack);
                GameActions.DoubleClick(backpack);
            }


            // tooltip
            _currentProfile.UseTooltip = _use_tooltip.IsChecked;
            _currentProfile.EnableTooltipOverride = _enableTooltipOverride.IsChecked;
            _currentProfile.TooltipTextHue = _tooltip_font_hue.Hue;
            _currentProfile.TooltipDelayBeforeDisplay = _delay_before_display_tooltip.Value;
            _currentProfile.TooltipBackgroundOpacity = _tooltip_background_opacity.Value;
            _currentProfile.TooltipDisplayZoom = _tooltip_zoom.Value;
            _currentProfile.TooltipFont = _tooltip_font_selector.GetSelectedFont();

            // ## BEGIN - END ## // BASICSETUP
            // ## BEGIN - END ## // TITLE BAR
            _currentProfile.EnableTitleBarStats = _enableTitleBarStats?.IsChecked ?? _currentProfile.EnableTitleBarStats;
            if (_titleBarStatsModeProgressBar != null && _titleBarStatsModeProgressBar.IsChecked)
                _currentProfile.TitleBarStatsMode = TitleBarStatsMode.ProgressBar;
            else if (_titleBarStatsModePercent != null && _titleBarStatsModePercent.IsChecked)
                _currentProfile.TitleBarStatsMode = TitleBarStatsMode.Percent;
            else if (_titleBarStatsModeText != null && _titleBarStatsModeText.IsChecked)
                _currentProfile.TitleBarStatsMode = TitleBarStatsMode.Text;
            if (World.Player != null)
            {
                if (_currentProfile.EnableTitleBarStats)
                    TitleBarStatsManager.ForceUpdate();
                else
                    Client.Game.SetWindowTitle(World.Player.Name);
            }
            TitleBarStatsBarsGump.UpdateVisibility();
            InWindowTitleBarBarsGump.UpdateVisibility();
            // ## BEGIN - END ## // ART / HUE CHANGES
            _currentProfile.ColorStealth = _colorStealth.IsChecked;
            _currentProfile.StealthHue = _stealthColorPickerBox.Hue;
            _currentProfile.GoldType = _goldType.SelectedIndex;
            _currentProfile.GoldHue = _goldColorPickerBox.Hue;
            _currentProfile.ColorGold = _colorGold.IsChecked;
            _currentProfile.ColorEnergyBolt = _colorEnergyBolt.IsChecked;
            _currentProfile.EnergyBoltHue = _energyBoltColorPickerBox.Hue;
            _currentProfile.ColorTreeTile = _colorTreeTile.IsChecked;
            _currentProfile.TreeTileHue = _treeTileColorPickerBox.Hue;
            _currentProfile.ColorBlockerTile = _colorBlockerTile.IsChecked;
            _currentProfile.BlockerTileHue = _blockerTileColorPickerBox.Hue;
            _currentProfile.BlockerType = _blockerType.SelectedIndex;
            _currentProfile.StealthNeonType = _stealthNeonType.SelectedIndex;
            _currentProfile.EnergyBoltNeonType = _energyBoltNeonType.SelectedIndex;
            _currentProfile.EnergyBoltArtType = _energyBoltArtType.SelectedIndex;
            if (_currentProfile.TreeType != _treeType.SelectedIndex)
            {
                if (_treeType.SelectedIndex == 0)
                {
                    StaticFilters.CleanTreeTextures();
                }
                _currentProfile.TreeType = _treeType.SelectedIndex;
            }
            // ## BEGIN - END ## // ART / HUE CHANGES
            // ## BEGIN - END ## // VISUAL HELPERS
            _currentProfile.HighlightTileAtRange = _highlightTileRange.IsChecked;
            _currentProfile.HighlightTileAtRangeRange = _highlightTileRangeRange.Value;
            _currentProfile.HighlightTileRangeHue = _highlightTileRangeColorPickerBox.Hue;
            _currentProfile.HighlightTileAtRangeSpell = _highlightTileRangeSpell.IsChecked;
            _currentProfile.HighlightTileAtRangeRangeSpell = _highlightTileRangeRangeSpell.Value;
            _currentProfile.HighlightTileRangeHueSpell = _highlightTileRangeColorPickerBoxSpell.Hue;
            _currentProfile.GlowingWeaponsType = _glowingWeaponsType.SelectedIndex;
            _currentProfile.PreviewFields = _previewFields.IsChecked;
            _currentProfile.OwnAuraByHP = _ownAuraByHP.IsChecked;
            _currentProfile.HighlightGlowingWeaponsTypeHue = _highlightGlowingWeaponsTypeColorPickerBoxHue.Hue;
            _currentProfile.HighlightLastTargetType = _highlightLastTargetType.SelectedIndex;
            _currentProfile.HighlighFriendsGuildType = _highlighFriendsGuildType.SelectedIndex;
            _currentProfile.HighlightLastTargetTypePoison = _highlightLastTargetTypePoison.SelectedIndex;
            _currentProfile.HighlightLastTargetTypePara = _highlightLastTargetTypePara.SelectedIndex;
            _currentProfile.HighlightLastTargetTypeStunned = _highlightLastTargetTypeStunned.SelectedIndex;
            _currentProfile.HighlightLastTargetTypeMortalled = _highlightLastTargetTypeMortalled.SelectedIndex;
            _currentProfile.HighlightLastTargetTypeHue = _highlightLastTargetTypeColorPickerBox.Hue;
            _currentProfile.HighlighFriendsGuildTypeHue = _highlighFriendsGuildTypeHueColorPickerBox.Hue;
            _currentProfile.HighlightLastTargetTypePoisonHue = _highlightLastTargetTypeColorPickerBoxPoison.Hue;
            _currentProfile.HighlightLastTargetTypeParaHue = _highlightLastTargetTypeColorPickerBoxPara.Hue;
            _currentProfile.HighlightLastTargetTypeStunnedHue = _highlightLastTargetTypeColorPickerBoxStunned.Hue;
            _currentProfile.HighlightLastTargetTypeMortalledHue = _highlightLastTargetTypeColorPickerBoxMortalled.Hue;
            // ## BEGIN - END ## // VISUAL HELPERS
            // ## BEGIN - END ## // HEALTHBAR
            _currentProfile.HighlightHealthBarByState = _highlightHealthBarByState.IsChecked;
            _currentProfile.HighlightLastTargetHealthBarOutline = _highlightLastTargetHealthBarOutline.IsChecked;
            // ## BEGIN - END ## // HEALTHBAR
            // ## BEGIN - END ## // CURSOR
            _currentProfile.SpellOnCursor = _spellOnCursor.IsChecked;
            int.TryParse(_spellOnCursorOffsetX.Text, out int spellOnCursorOffsetX);
            int.TryParse(_spellOnCursorOffsetY.Text, out int spellOnCursorOffsetY);
            _currentProfile.SpellOnCursorOffset = new Point(spellOnCursorOffsetX, spellOnCursorOffsetY);
            _currentProfile.ColorGameCursor = _colorGameCursor.IsChecked;
            // ## BEGIN - END ## // CURSOR
            // ## BEGIN - END ## // OVERHEAD / UNDERCHAR
            _currentProfile.OverheadRange = _overheadRange.IsChecked;
            // ## BEGIN - END ## // OVERHEAD / UNDERCHAR
            // ## BEGIN - END ## // OLDHEALTHLINES
            _currentProfile.MultipleUnderlinesSelfParty = _multipleUnderlinesSelfParty.IsChecked;
            _currentProfile.MultipleUnderlinesSelfPartyBigBars = _multipleUnderlinesSelfPartyBigBars.IsChecked;
            _currentProfile.MultipleUnderlinesSelfPartyTransparency = _multipleUnderlinesSelfPartyTransparency.Value;
            _currentProfile.UseOldHealthBars = _useOldHealthBars.IsChecked;
            // ## BEGIN - END ## // OLDHEALTHLINES
            // ## BEGIN - END ## // MISC
            _currentProfile.BlackOutlineStatics = _blackOutlineStatics.IsChecked;
            _currentProfile.IgnoreStaminaCheck = _ignoreStaminaCheck.IsChecked;
            _currentProfile.SpecialSetLastTargetCliloc = _SpecialSetLastTargetCliloc.IsChecked;
            _currentProfile.SpecialSetLastTargetClilocText = _SpecialSetLastTargetClilocText.Text;
            _currentProfile.BlockWoSArtForceAoS = _blockWoSArtForceAoS.IsChecked;
            _currentProfile.BlockWoSArt = uint.Parse(_blockWoSArt.Text);
            if (_currentProfile.BlockWoS != _blockWoS.IsChecked)
            {
                if (_blockWoS.IsChecked)
                {
                    TileDataLoader.Instance.StaticData[0x038A].Flags |= TileFlag.Impassable;
                    TileDataLoader.Instance.StaticData[_currentProfile.BlockWoSArt].Flags |= TileFlag.Impassable;
                }
                else
                {
                    TileDataLoader.Instance.StaticData[0x038A].Flags &= ~TileFlag.Impassable;
                    TileDataLoader.Instance.StaticData[_currentProfile.BlockWoSArt].Flags &= ~TileFlag.Impassable;
                }
                _currentProfile.BlockWoS = _blockWoS.IsChecked;
            }
            if (_currentProfile.BlockWoSFelOnly != _blockWoSFelOnly.IsChecked)
            {
                if (_blockWoSFelOnly.IsChecked && World.MapIndex == 0)
                {
                    TileDataLoader.Instance.StaticData[0x038A].Flags |= TileFlag.Impassable;
                    TileDataLoader.Instance.StaticData[_currentProfile.BlockWoSArt].Flags |= TileFlag.Impassable;
                }
                else
                {
                    TileDataLoader.Instance.StaticData[0x038A].Flags &= ~TileFlag.Impassable;
                    TileDataLoader.Instance.StaticData[_currentProfile.BlockWoSArt].Flags &= ~TileFlag.Impassable;
                }
                _currentProfile.BlockWoSFelOnly = _blockWoSFelOnly.IsChecked;
            }
            _currentProfile.BlockEnergyFArtForceAoS = _blockEnergyFArtForceAoS.IsChecked;
            _currentProfile.BlockEnergyFArt = uint.Parse(_blockEnergyFArt.Text);
            if (_currentProfile.BlockEnergyF != _blockEnergyF.IsChecked)
            {
                if (_blockEnergyF.IsChecked)
                {
                    for (int i = 0; i < 31; i++)
                    {
                        //0x3946 to 0x3964 / 14662 to 14692
                        TileDataLoader.Instance.StaticData[0x3946 + i].Flags |= TileFlag.Impassable;
                    }
                    TileDataLoader.Instance.StaticData[_currentProfile.BlockEnergyFArt].Flags |= TileFlag.Impassable;
                }
                else
                {
                    for (int i = 0; i < 31; i++)
                    {
                        TileDataLoader.Instance.StaticData[0x3946 + i].Flags &= ~TileFlag.Impassable;
                    }
                    TileDataLoader.Instance.StaticData[_currentProfile.BlockEnergyFArt].Flags &= ~TileFlag.Impassable;
                }
                _currentProfile.BlockEnergyF = _blockEnergyF.IsChecked;
            }
            if (_currentProfile.BlockEnergyFFelOnly != _blockEnergyFFelOnly.IsChecked)
            {
                if (_blockEnergyFFelOnly.IsChecked && World.MapIndex == 0)
                {
                    for (int i = 0; i < 31; i++)
                    {
                        TileDataLoader.Instance.StaticData[0x3946 + i].Flags |= TileFlag.Impassable;
                    }
                    TileDataLoader.Instance.StaticData[_currentProfile.BlockEnergyFArt].Flags |= TileFlag.Impassable;
                }
                else
                {
                    for (int i = 0; i < 31; i++)
                    {
                        TileDataLoader.Instance.StaticData[0x3946 + i].Flags &= ~TileFlag.Impassable;
                    }
                    TileDataLoader.Instance.StaticData[_currentProfile.BlockEnergyFArt].Flags &= ~TileFlag.Impassable;
                }
                _currentProfile.BlockEnergyFFelOnly = _blockEnergyFFelOnly.IsChecked;
            }
            // ## BEGIN - END ## // MISC
            // ## BEGIN - END ## // MISC2
            _currentProfile.WireFrameView = _wireframeView.IsChecked;
            _currentProfile.HueImpassableView = _hueImpassableView.IsChecked;
            _currentProfile.HueImpassableViewHue = _hueImpassableViewColorPickerBox.Hue;
            _currentProfile.TransparentHousesEnabled = _transparentHouses.IsChecked;
            _currentProfile.TransparentHousesZ = _transparentHousesZ.Value;
            _currentProfile.TransparentHousesTransparency = _transparentHousesTransparency.Value;
            _currentProfile.InvisibleHousesEnabled = _invisibleHouses.IsChecked;
            _currentProfile.InvisibleHousesZ = _invisibleHousesZ.Value;
            _currentProfile.DontRemoveHouseBelowZ = _dontRemoveHouseBelowZ.Value;
            _currentProfile.DrawMobilesWithSurfaceOverhead = _drawMobilesWithSurfaceOverhead.IsChecked;
            _currentProfile.IgnoreCoTEnabled = _ignoreCoT.IsChecked;
            _currentProfile.ShowDeathOnWorldmap = _showDeathOnWorldmap.IsChecked;
            _currentProfile.ForceGargoyleWalk = _forceGargoyleWalk.IsChecked;
            // ## BEGIN - END ## // MISC2
            // ## BEGIN - END ## // MACROS
            _currentProfile.LastTargetRange = _lastTargetRange.Value;
            // ## BEGIN - END ## // MACROS
            // ## BEGIN - END ## // NAMEOVERHEAD
            _currentProfile.ShowHPLineInNOH = _showHPLineInNOH.IsChecked;
            // ## BEGIN - END ## // NAMEOVERHEAD
            // ## BEGIN - END ## // UI/GUMPS
            _currentProfile.BandageGump = _bandageGump.IsChecked;

            int.TryParse(_bandageGumpOffsetX.Text, out int bandageGumpOffsetX);
            int.TryParse(_bandageGumpOffsetY.Text, out int bandageGumpOffsetY);

            _currentProfile.BandageGumpOffset = new Point(bandageGumpOffsetX, bandageGumpOffsetY);

            if (_currentProfile.UOClassicCombatLTBar != _uccEnableLTBar.IsChecked)
            {
                UOClassicCombatLTBar UOClassicCombatLTBar = UIManager.GetGump<UOClassicCombatLTBar>();
                UIManager.Gumps.OfType<UOClassicCombatLTBar>().ToList().ForEach(g => g.Dispose());

                if (_uccEnableLTBar.IsChecked)
                {
                    UOClassicCombatLTBar = new UOClassicCombatLTBar
                    {
                        X = _currentProfile.UOClassicCombatLTBarLocation.X,
                        Y = _currentProfile.UOClassicCombatLTBarLocation.Y
                    };
                    UIManager.Add(UOClassicCombatLTBar);
                }

                _currentProfile.UOClassicCombatLTBar = _uccEnableLTBar.IsChecked;
            }

            _currentProfile.BandageGumpUpDownToggle = _bandageUpDownToggle.IsChecked;
            // ## BEGIN - END ## // UI/GUMPS
            // ## BEGIN - END ## // TEXTUREMANAGER
            // ## BEGIN - END ## // TEXTUREMANAGER
            // ## BEGIN - END ## // LINES
            if (_currentProfile.UOClassicCombatLines != _uccEnableLines.IsChecked)
            {
                UOClassicCombatLines UOClassicCombatLines = UIManager.GetGump<UOClassicCombatLines>();

                if (_uccEnableLines.IsChecked)
                {
                    if (UOClassicCombatLines != null)
                        UOClassicCombatLines.Dispose();

                    UOClassicCombatLines = new UOClassicCombatLines
                    {
                        X = _currentProfile.UOClassicCombatLinesLocation.X,
                        Y = _currentProfile.UOClassicCombatLinesLocation.Y
                    };
                    UIManager.Add(UOClassicCombatLines);
                }
                else
                {
                    if (UOClassicCombatLines != null)
                        UOClassicCombatLines.Dispose();
                }

                _currentProfile.UOClassicCombatLines = _uccEnableLines.IsChecked;
            }
            // ## BEGIN - END ## // LINES
            // ## BEGIN - END ## // AUTOLOOT
            _currentProfile.UOClassicCombatAL_EnableGridLootColoring = _uccEnableGridLootColoring.IsChecked;
            _currentProfile.UOClassicCombatAL_EnableLootAboveID = _uccBEnableLootAboveID.IsChecked;

            if (_currentProfile.UOClassicCombatAL != _uccEnableAL.IsChecked)
            {
                UOClassicCombatAL UOClassicCombatAL = UIManager.GetGump<UOClassicCombatAL>();

                if (_uccEnableAL.IsChecked)
                {
                    if (UOClassicCombatAL != null)
                        UOClassicCombatAL.Dispose();

                    UOClassicCombatAL = new UOClassicCombatAL
                    {
                        X = _currentProfile.UOClassicCombatALLocation.X,
                        Y = _currentProfile.UOClassicCombatALLocation.Y
                    };
                    UIManager.Add(UOClassicCombatAL);
                }
                else
                {
                    if (UOClassicCombatAL != null)
                        UOClassicCombatAL.Dispose();
                }

                _currentProfile.UOClassicCombatAL = _uccEnableAL.IsChecked;
            }
            // ## BEGIN - END ## // AUTOLOOT
            // ## BEGIN - END ## // BUFFBAR/UCCSETTINGS
            bool buffbarLineSettingsChanged = _currentProfile.UOClassicCombatBuffbar_SwingEnabled != _uccSwing.IsChecked
                                          || _currentProfile.UOClassicCombatBuffbar_DoDEnabled != _uccDoD.IsChecked
                                          || _currentProfile.UOClassicCombatBuffbar_GotDEnabled != _uccGotD.IsChecked;

            _currentProfile.UOClassicCombatBuffbar_SwingEnabled = _uccSwing.IsChecked;
            _currentProfile.UOClassicCombatBuffbar_DoDEnabled = _uccDoD.IsChecked;
            _currentProfile.UOClassicCombatBuffbar_GotDEnabled = _uccGotD.IsChecked;
            _currentProfile.UOClassicCombatBuffbar_Locked = _uccLocked.IsChecked;

            if (_currentProfile.UOClassicCombatBuffbar != _uccEnableBuffbar.IsChecked || (_uccEnableBuffbar.IsChecked && buffbarLineSettingsChanged))
            {
                UOClassicCombatBuffbar UOClassicCombatBuffbar = UIManager.GetGump<UOClassicCombatBuffbar>();

                if (_uccEnableBuffbar.IsChecked)
                {
                    if (UOClassicCombatBuffbar != null)
                        UOClassicCombatBuffbar.Dispose();

                    UOClassicCombatBuffbar = new UOClassicCombatBuffbar
                    {
                        X = _currentProfile.UOClassicCombatBuffbarLocation.X,
                        Y = _currentProfile.UOClassicCombatBuffbarLocation.Y
                    };
                    UIManager.Add(UOClassicCombatBuffbar);
                }
                else
                {
                    if (UOClassicCombatBuffbar != null)
                        UOClassicCombatBuffbar.Dispose();
                }

                _currentProfile.UOClassicCombatBuffbar = _uccEnableBuffbar.IsChecked;
            }
            // ## BEGIN - END ## // BUFFBAR/UCCSETTINGS
            // ## BEGIN - END ## // SELF
            if (_currentProfile.UOClassicCombatSelf != _uccEnableSelf.IsChecked)
            {
                UOClassicCombatSelf uccSelfGump = UIManager.GetGump<UOClassicCombatSelf>();

                if (_uccEnableSelf.IsChecked)
                {
                    if (uccSelfGump != null)
                        uccSelfGump.Dispose();

                    uccSelfGump = new UOClassicCombatSelf
                    {
                        X = _currentProfile.UOClassicCombatSelfLocation.X,
                        Y = _currentProfile.UOClassicCombatSelfLocation.Y
                    };
                    UIManager.Add(uccSelfGump);
                }
                else
                {
                    if (uccSelfGump != null)
                        uccSelfGump.Dispose();
                }

                _currentProfile.UOClassicCombatSelf = _uccEnableSelf.IsChecked;
            }
            // ## BEGIN - END ## // SELF
            // ## BEGIN - END ## // BUFFBAR/UCCSETTINGS
            _currentProfile.UOClassicCombatSelf_DisarmStrikeCooldown = uint.Parse(_uccDisarmStrikeCooldown.Text);
            _currentProfile.UOClassicCombatSelf_DisarmAttemptCooldown = uint.Parse(_uccDisarmAttemptCooldown.Text);
            _currentProfile.UOClassicCombatSelf_DisarmedCooldown = uint.Parse(_uccDisarmedCooldown.Text);
            // ## BEGIN - END ## // BUFFBAR/UCCSETTINGS
            // ## BEGIN - END ## // SELF
            _currentProfile.UOClassicCombatSelf_BandiesHPTreshold = uint.Parse(_uccBandiesHPTreshold.Text);
            _currentProfile.UOClassicCombatSelf_BandiesPoison = _uccBandiesPoison.IsChecked;
            _currentProfile.UOClassicCombatSelf_CurepotHPTreshold = uint.Parse(_uccCurepotHPTreshold.Text);
            _currentProfile.UOClassicCombatSelf_HealpotHPTreshold = uint.Parse(_uccHealpotHPTreshold.Text);
            _currentProfile.UOClassicCombatSelf_RefreshpotStamTreshold = uint.Parse(_uccRefreshpotStamTreshold.Text);

            _currentProfile.UOClassicCombatSelf_AutoRearmAfterDisarmedCooldown = uint.Parse(_uccAutoRearmAfterDisarmedCooldown.Text);

            _currentProfile.UOClassicCombatSelf_StrengthPotCooldown = uint.Parse(_uccStrengthPotCooldown.Text);
            _currentProfile.UOClassicCombatSelf_DexPotCooldown = uint.Parse(_uccDexPotCooldown.Text);

            _currentProfile.UOClassicCombatSelf_MinRNG = int.Parse(_uccRNGMin.Text);
            _currentProfile.UOClassicCombatSelf_MaxRNG = int.Parse(_uccRNGMax.Text);

            _currentProfile.UOClassicCombatSelf_ClilocTriggers = _uccClilocTrigger.IsChecked;
            _currentProfile.UOClassicCombatSelf_MacroTriggers = _uccMacroTrigger.IsChecked;

            UOClassicCombatSelf UOClassicCombatSelfCurrent = UIManager.GetGump<UOClassicCombatSelf>();
            if (UOClassicCombatSelfCurrent != null)
                UOClassicCombatSelfCurrent.UpdateVars();
            // ## BEGIN - END ## // SELF
            // ## BEGIN - END ## // ADVMACROS
            int.TryParse(_pullFriendlyBarsX.Text, out int pullFriendlyBarsX);
            int.TryParse(_pullFriendlyBarsY.Text, out int pullFriendlyBarsY);
            _currentProfile.PullFriendlyBars = new Point(pullFriendlyBarsX, pullFriendlyBarsY);
            int.TryParse(_pullFriendlyBarsFinalLocationX.Text, out int pullFriendlyBarsFinalLocationX);
            int.TryParse(_pullFriendlyBarsFinalLocationY.Text, out int pullFriendlyBarsFinalLocationY);
            _currentProfile.PullFriendlyBarsFinalLocation = new Point(pullFriendlyBarsFinalLocationX, pullFriendlyBarsFinalLocationY);

            int.TryParse(_pullEnemyBarsX.Text, out int pullEnemyBarsX);
            int.TryParse(_pullEnemyBarsY.Text, out int pullEnemyBarsY);
            _currentProfile.PullEnemyBars = new Point(pullEnemyBarsX, pullEnemyBarsY);
            int.TryParse(_pullEnemyBarsFinalLocationX.Text, out int pullEnemyBarsFinalLocationX);
            int.TryParse(_pullEnemyBarsFinalLocationY.Text, out int pullEnemyBarsFinalLocationY);
            _currentProfile.PullEnemyBarsFinalLocation = new Point(pullEnemyBarsFinalLocationX, pullEnemyBarsFinalLocationY);

            int.TryParse(_pullPartyAllyBarsX.Text, out int pullPartyAllyBarsX);
            int.TryParse(_pullPartyAllyBarsY.Text, out int pullPartyAllyBarsY);
            _currentProfile.PullPartyAllyBars = new Point(pullPartyAllyBarsX, pullPartyAllyBarsY);
            int.TryParse(_pullPartyAllyBarsFinalLocationX.Text, out int pullPartyAllyBarsFinalLocationX);
            int.TryParse(_pullPartyAllyBarsFinalLocationY.Text, out int pullPartyAllyBarsFinalLocationY);
            _currentProfile.PullPartyAllyBarsFinalLocation = new Point(pullPartyAllyBarsFinalLocationX, pullPartyAllyBarsFinalLocationY);
            // ## BEGIN - END ## // ADVMACROS
            // ## BEGIN - END ## // AUTOMATIONS
            _currentProfile.AutoWorldmapMarker = _autoWorldmapMarker.IsChecked;
            _currentProfile.AutoRangeDisplayAlways = _autoRangeDisplayAlways.IsChecked;
            _currentProfile.AutoRangeDisplayHue = _autoRangeDisplayHue.Hue;
            // ## BEGIN - END ## // AUTOMATIONS
            // ## BEGIN - END ## // OUTLANDS
            /*
            ProfileManager.CurrentProfile.InfernoBridge = _infernoBridge.IsChecked;
            ProfileManager.CurrentProfile.OverheadSummonTime = _overheadSummonTime.IsChecked;
            ProfileManager.CurrentProfile.OverheadPeaceTime = _overheadPeaceTime.IsChecked;
            ProfileManager.CurrentProfile.MobileHamstrungTime = _mobileHamstrungTime.IsChecked;
            ProfileManager.CurrentProfile.MobileHamstrungTimeCooldown = uint.Parse(_mobileHamstrungTimeCooldown.Text);
            ProfileManager.CurrentProfile.UOClassicCombatSelf_HamstringStrikeCooldown = uint.Parse(_uccHamstringStrikeCooldown.Text);
            ProfileManager.CurrentProfile.UOClassicCombatSelf_HamstringAttemptCooldown = uint.Parse(_uccHamstringAttemptCooldown.Text);
            ProfileManager.CurrentProfile.UOClassicCombatSelf_HamstrungCooldown = uint.Parse(_uccHamstrungCooldown.Text);
            ProfileManager.CurrentProfile.UOClassicCombatBuffbar_DoHEnabled = _uccDoH.IsChecked;
            ProfileManager.CurrentProfile.UOClassicCombatBuffbar_GotHEnabled = _uccGotH.IsChecked;
            */
            // ## BEGIN - END ## // OUTLANDS
            // ## BEGIN - END ## // LOBBY
            _currentProfile.LobbyIP = _lobbyIP.Text;
            _currentProfile.LobbyPort = _lobbyPort.Text;
            // ## BEGIN - END ## // LOBBY
            // ## BEGIN - END ## // STATUSGUMP
            _currentProfile.UseRazorEnhStatusGump = _useRazorEnhStatusGump.IsChecked;
            // ## BEGIN - END ## // STATUSGUMP
            // ## BEGIN - END ## // ONCASTINGGUMP
            _currentProfile.OnCastingGump = _onCastingGump.IsChecked;
            _currentProfile.OnCastingGump_hidden = _onCastingGump_hidden.IsChecked;
            _currentProfile.OnCastingUnderPlayerBar = _onCastingUnderPlayerBar.IsChecked;
            _currentProfile.ShowMapCloseFriend = _showMapCloseFriend.IsChecked;
            _currentProfile.AutoAvoidObstacules = _autoAvoidMobiles.IsChecked;
            _currentProfile.ScaleMonstersEnabled = _scaleMonstersEnabled.IsChecked;
            // ## BEGIN - END ## // ONCASTINGGUMP

            RefreshBandageAndOnCastingGumps();

            // ## BEGIN - END ## // MISC3 SHOWALLLAYERS
            _currentProfile.ShowAllLayers = _showAllLayers.IsChecked;
            _currentProfile.ShowAllLayersPaperdoll = _showAllLayersPaperdoll.IsChecked;
            _currentProfile.ShowAllLayersPaperdoll_X = int.Parse(_showAllLayersPaperdoll_X.Text);
            _currentProfile.ColorPaperdollByDurability = _colorPaperdollByDurability.IsChecked;
            // ## BEGIN - END ## // MISC3 SHOWALLLAYERS
            // ## BEGIN - END ## // MISC3 THIEFSUPREME
            _currentProfile.OverrideContainerOpenRange = _overrideContainerOpenRange.IsChecked;
            // ## BEGIN - END ## // MISC3 THIEFSUPREME
            // ## BEGIN - END ## // VISUALRESPONSEMANAGER
            _currentProfile.VisualResponseManager = _visualResponseManager.IsChecked;
            // ## BEGIN - END ## // VISUALRESPONSEMANAGER
            // ## BEGIN - END ## // TABGRID // PKRION
            bool tabGridSettingsChanged = _currentProfile.TabGridGumpEnabled != _enableTabGridGump.IsChecked;

            int gridRows = _currentProfile.GridRows;
            if (int.TryParse(_rowsGrid.Text, out int parsedRows))
            {
                gridRows = Math.Max(1, parsedRows);
            }

            int gridTabs = _currentProfile.GridTabs;
            if (int.TryParse(_tabsGrid.Text, out int parsedTabs))
            {
                gridTabs = Math.Max(1, parsedTabs);
            }

            string tabList = string.IsNullOrWhiteSpace(_tablistBox.Text) ? _currentProfile.TabList : _tablistBox.Text.Trim();

            if (_currentProfile.GridRows != gridRows || _currentProfile.GridTabs != gridTabs || _currentProfile.TabList != tabList)
            {
                tabGridSettingsChanged = true;
            }

            _currentProfile.TabGridGumpEnabled = _enableTabGridGump.IsChecked;
            _currentProfile.GridRows = gridRows;
            _currentProfile.GridTabs = gridTabs;
            _currentProfile.TabList = tabList;

            if (_currentProfile.TabGridGumpEnabled || tabGridSettingsChanged)
            {
                TabGridGump tabGridGump = UIManager.GetGump<TabGridGump>();

                if (tabGridGump != null)
                {
                    tabGridGump.Dispose();
                }

                if (_currentProfile.TabGridGumpEnabled)
                {
                    UIManager.Add(new TabGridGump());
                }
            }
            // ## BEGIN - END ## // TABGRID // PKRION

            // ## BEGIN - END ## // BASICSETUP

            _currentProfile?.Save(ProfileManager.ProfilePath);
        }

        internal void UpdateVideo()
        {
            var camera = Client.Game.Scene.Camera;

            _gameWindowPositionX.SetText(camera.Bounds.X.ToString());
            _gameWindowPositionY.SetText(camera.Bounds.Y.ToString());
            _gameWindowWidth.SetText(camera.Bounds.Width.ToString());
            _gameWindowHeight.SetText(camera.Bounds.Height.ToString());
        }

        public string GetPageString()
        {
            return ActivePage.ToString();
        }

        private void RefreshBandageAndOnCastingGumps()
        {
            if (World.Player == null)
            {
                return;
            }

            if (_currentProfile.BandageGump)
            {
                if (World.Player.BandageTimer == null || World.Player.BandageTimer.IsDisposed)
                {
                    UIManager.Add(World.Player.BandageTimer = new ClassicUO.Dust765.External.BandageGump());
                }
            }
            else
            {
                World.Player.BandageTimer?.Stop();
            }

            if (_currentProfile.OnCastingGump)
            {
                if (World.Player.OnCasting == null || World.Player.OnCasting.IsDisposed)
                {
                    UIManager.Add(World.Player.OnCasting = new ClassicUO.Dust765.External.OnCastingGump());
                }

                if (World.Player.OnCasting != null)
                {
                    World.Player.OnCasting.IsVisible = GameActions.iscasting && !_currentProfile.OnCastingGump_hidden;
                }
            }
            else
            {
                World.Player.OnCasting?.Stop();
            }
        }

        private static int GetWindowTitleStyleIndex(WindowTitleBarStyle style)
        {
            switch (style)
            {
                case WindowTitleBarStyle.UOS:
                    return 1;

                case WindowTitleBarStyle.Orion:
                    return 2;

                default:
                    return 0;
            }
        }

        private static WindowTitleBarStyle GetWindowTitleStyleFromIndex(int index)
        {
            switch (index)
            {
                case 1:
                    return WindowTitleBarStyle.UOS;

                case 2:
                    return WindowTitleBarStyle.Orion;

                default:
                    return WindowTitleBarStyle.CUO;
            }
        }

        private void UpdateTitleBarStatsControlsAvailability()
        {
            if (_windowTitleStyle == null)
            {
                return;
            }

            bool enableStatsToggle = GetWindowTitleStyleFromIndex(_windowTitleStyle.SelectedIndex) != WindowTitleBarStyle.CUO;

            if (_enableTitleBarStats != null)
            {
                _enableTitleBarStats.IsEnabled = enableStatsToggle;

                if (!enableStatsToggle)
                {
                    _enableTitleBarStats.IsChecked = true;
                }
            }

            if (_titleBarStatsModeText != null)
            {
                _titleBarStatsModeText.IsEnabled = true;
            }

            if (_titleBarStatsModePercent != null)
            {
                _titleBarStatsModePercent.IsEnabled = true;
            }

            if (_titleBarStatsModeProgressBar != null)
            {
                _titleBarStatsModeProgressBar.IsEnabled = true;
            }
        }

        private void ApplyWindowTitleStyleSelection(bool resizeForSelectionChange)
        {
            if (_syncingWindowTitleStyleControls || _windowTitleStyle == null)
            {
                return;
            }

            _syncingWindowTitleStyleControls = true;

            WindowTitleBarStyle style = GetWindowTitleStyleFromIndex(_windowTitleStyle.SelectedIndex);
            bool useCustomWindowTitle = style != WindowTitleBarStyle.CUO;

            if (_windowBorderless != null)
            {
                _windowBorderless.IsChecked = useCustomWindowTitle;
            }

            if (_enableWindowBorderFrame != null)
            {
                _enableWindowBorderFrame.IsChecked = useCustomWindowTitle;
            }

            _syncingWindowTitleStyleControls = false;
            UpdateTitleBarStatsControlsAvailability();

            // Apply window borderless state immediately so the custom bar appears without waiting for Apply
            UIManager.GetGump<TopStatusBarGump>()?.Dispose();
            Client.Game.SetWindowBorderless(useCustomWindowTitle);

            if (useCustomWindowTitle)
            {
                TopStatusBarGump.Create();
            }

            if (resizeForSelectionChange)
            {
                // Slight resize to force the window to repaint and render the new bar
                int cw = Client.Game.Window.ClientBounds.Width;
                int ch = Client.Game.Window.ClientBounds.Height;
                Client.Game.SetWindowSize(cw + 1, ch);
                Client.Game.SetWindowSize(cw, ch);
            }
        }

        private void SyncWindowTitleStyleFromWindowOptions()
        {
            if (_syncingWindowTitleStyleControls || _windowTitleStyle == null || _windowBorderless == null || _enableWindowBorderFrame == null)
            {
                return;
            }

            if (_windowBorderless.IsChecked && _enableWindowBorderFrame.IsChecked)
            {
                return;
            }

            _syncingWindowTitleStyleControls = true;
            _windowTitleStyle.SelectedIndex = 0;
            _syncingWindowTitleStyleControls = false;
            UpdateTitleBarStatsControlsAvailability();
        }

        public void GoToPage(string pageString)
        {
            if (string.IsNullOrEmpty(pageString)) return;
            string[] parts = pageString.Split(':');
            if (parts.Length >= 1 && int.TryParse(parts[0].Trim(), out int p))
                ChangePage(p);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

            batcher.Draw
            (
                LogoTexture,
                new Rectangle
                (
                    x + 165,
                    y + 20,
                    WIDTH - 250,
                    400
                ),
                hueVector
            );

            batcher.DrawRectangle
            (
                SolidColorTextureCache.GetTexture(Color.Gray),
                x,
                y,
                Width,
                Height,
                hueVector
            );

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
            InputField elem = new InputField
            (
                0x0BB8,
                FONT,
                HUE_FONT,
                true,
                width,
                height,
                maxWidth,
                maxCharCount
            )
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
            Checkbox box = new Checkbox
            (
                0x00D2,
                0x00D3,
                text,
                FONT,
                HUE_FONT
            )
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
            (
                x,
                y,
                width,
                min,
                max,
                value,
                HSliderBarStyle.MetalWidgetRecessedBar,
                true,
                FONT,
                HUE_FONT
            );

            area?.Add(slider);

            return slider;
        }

        private ModernColorPicker.HueDisplay AddHueDisplay(ScrollArea area, int x, int y, ushort hue, string text)
        {
            ModernColorPicker.HueDisplay hueDisplay = new ModernColorPicker.HueDisplay(hue, null, true) { X = x, Y = y };

            area?.Add(hueDisplay);

            area?.Add
            (
                new Label(text, true, HUE_FONT)
                {
                    X = x + hueDisplay.Width + 10,
                    Y = y
                }
            );

            return hueDisplay;
        }

        private SettingsSection AddSettingsSection(DataBox area, string label)
        {
            SettingsSection section = new SettingsSection(label, area.Width);
            area.Add(section);
            area.WantUpdateSize = true;
            //area.ReArrangeChildren();

            return section;
        }

        protected override void OnDragBegin(int x, int y)
        {
            if (UIManager.MouseOverControl?.RootParent == this)
            {
                UIManager.MouseOverControl.InvokeDragBegin(new Point(x, y));
            }

            base.OnDragBegin(x, y);
        }

        protected override void OnDragEnd(int x, int y)
        {
            if (UIManager.MouseOverControl?.RootParent == this)
            {
                UIManager.MouseOverControl.InvokeDragEnd(new Point(x, y));
            }

            base.OnDragEnd(x, y);
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

            OpenIgnoreList,
            NewMacro,
            DeleteMacro,

            NewNameOverheadEntry,
            DeleteOverheadEntry,

            SaveAsDefault,
            OverrideAllProfiles,
            OverrideSameServer,

            Last = OverrideSameServer
        }


        public class SettingsSection : Control
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

                base.Add
                (
                    new Line
                    (
                        0,
                        label.Height,
                        width - 30,
                        1,
                        0xFFbabdc2
                    )
                );

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

            public void BaseAdd(Control c, int page = 0)
            {
                base.Add(c, page);
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
                            _buttons[i] = new RadioButton
                            (
                                0,
                                0x00D0,
                                0x00D1,
                                markup,
                                i,
                                HUE_FONT
                            )
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

        public class InputField : Control
        {
            private readonly StbTextBox _textbox;

            public event EventHandler TextChanged { add { _textbox.TextChanged += value; } remove { _textbox.TextChanged -= value; } }

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

                _textbox = new StbTextBox
                (
                    font,
                    maxCharsCount,
                    maxWidthText,
                    unicode,
                    FontStyle.BlackBorder,
                    hue
                )
                {
                    X = 4,
                    Y = 4,
                    Width = width - 8,
                    Height = height - 8
                };


                Add(background);
                Add(_textbox);
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                if (batcher.ClipBegin(x, y, Width, Height))
                {
                    base.Draw(batcher, x, y);

                    batcher.ClipEnd();
                }

                return true;
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
