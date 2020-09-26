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
                         _treeToStumps,
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
                                  _genericColorPickerBox,
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
                         _enableShadows,
                         _auraMouse,
                         _runMouseInSeparateThread,
                         _useColoredLights,
                         _darkNights,
                         _partyAura,
                         _hideChatGradient;
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


            DataBox box = new DataBox(startX, startY, rightArea.Width - 15, 1);
            box.WantUpdateSize = true;
            rightArea.Add(box);


            SettingsSection section = AddSettingsSection(box, "General");

            section.Add
            (
                _highlightObjects = AddCheckBox
                    (null, ResGumps.HighlightObjects, ProfileManager.CurrentProfile.HighlightGameObjects, startX, startY)
            );

            section.Add
            (
                _enablePathfind = AddCheckBox
                    (null, ResGumps.EnablePathfinding, ProfileManager.CurrentProfile.EnablePathfind, startX, startY)
            );

            section.AddRight
            (
                _useShiftPathfind = AddCheckBox
                    (null, ResGumps.ShiftPathfinding, ProfileManager.CurrentProfile.UseShiftToPathfind, startX, startY)
            );

            section.Add
                (_alwaysRun = AddCheckBox(null, ResGumps.AlwaysRun, ProfileManager.CurrentProfile.AlwaysRun, startX, startY));

            section.AddRight
            (
                _alwaysRunUnlessHidden = AddCheckBox
                    (null, ResGumps.AlwaysRunHidden, ProfileManager.CurrentProfile.AlwaysRunUnlessHidden, startX, startY)
            );

            section.Add
            (
                _autoOpenDoors = AddCheckBox
                    (null, ResGumps.AutoOpenDoors, ProfileManager.CurrentProfile.AutoOpenDoors, startX, startY)
            );

            section.AddRight
            (
                _smoothDoors = AddCheckBox
                    (null, ResGumps.SmoothDoors, ProfileManager.CurrentProfile.SmoothDoors, startX, startY)
            );

            section.Add
            (
                _autoOpenCorpse = AddCheckBox
                    (null, ResGumps.AutoOpenCorpses, ProfileManager.CurrentProfile.AutoOpenCorpses, startX, startY)
            );

            section.PushIndent();
            section.Add(AddLabel(null, ResGumps.CorpseOpenRange, 0, 0));

            section.AddRight
            (
                _autoOpenCorpseRange = AddInputField
                    (null, startX, startY, 50, TEXTBOX_HEIGHT, ResGumps.CorpseOpenRange, 80, false, true, 2)
            );

            _autoOpenCorpseRange.SetText(ProfileManager.CurrentProfile.AutoOpenCorpseRange.ToString());

            section.Add
            (
                _skipEmptyCorpse = AddCheckBox
                    (null, ResGumps.SkipEmptyCorpses, ProfileManager.CurrentProfile.SkipEmptyCorpse, startX, startY)
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
                    }, ProfileManager.CurrentProfile.CorpseOpenOptions, startX, startY, 150
                ), 2
            );

            section.PopIndent();

            section.Add
            (
                _noColorOutOfRangeObjects = AddCheckBox
                (
                    rightArea, ResGumps.OutOfRangeColor, ProfileManager.CurrentProfile.NoColorObjectsOutOfRange, startX, startY
                )
            );

            section.Add
            (
                _sallosEasyGrab = AddCheckBox
                    (null, ResGumps.SallosEasyGrab, ProfileManager.CurrentProfile.SallosEasyGrab, startX, startY)
            );

            section.Add
            (
                _showHouseContent = AddCheckBox
                    (null, ResGumps.ShowHousesContent, ProfileManager.CurrentProfile.ShowHouseContent, startX, startY)
            );

            _showHouseContent.IsVisible = Client.Version >= ClientVersion.CV_70796;

            section.Add
            (
                _use_smooth_boat_movement = AddCheckBox
                    (null, ResGumps.SmoothBoat, ProfileManager.CurrentProfile.UseSmoothBoatMovement, startX, startY)
            );

            _use_smooth_boat_movement.IsVisible = Client.Version >= ClientVersion.CV_7090;


            SettingsSection section2 = AddSettingsSection(box, "Mobiles");
            section2.Y = section.Bounds.Bottom + 40;

            section2.Add
            (
                _showHpMobile = AddCheckBox(null, ResGumps.ShowHP, ProfileManager.CurrentProfile.ShowMobilesHP, startX, startY)
            );

            int mode = ProfileManager.CurrentProfile.MobileHPType;

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

            mode = ProfileManager.CurrentProfile.MobileHPShowWhen;

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
                    (null, ResGumps.HighlighState, ProfileManager.CurrentProfile.HighlightMobilesByFlags, startX, startY)
            );

            section2.PushIndent();

            section2.Add
            (
                _poisonColorPickerBox = AddColorBox
                    (null, startX, startY, ProfileManager.CurrentProfile.PoisonHue, ResGumps.PoisonedColor)
            );

            section2.AddRight(AddLabel(null, ResGumps.PoisonedColor, 0, 0), 2);

            section2.Add
            (
                _paralyzedColorPickerBox = AddColorBox
                    (null, startX, startY, ProfileManager.CurrentProfile.ParalyzedHue, ResGumps.ParalyzedColor)
            );

            section2.AddRight(AddLabel(null, ResGumps.ParalyzedColor, 0, 0), 2);

            section2.Add
            (
                _invulnerableColorPickerBox = AddColorBox
                    (null, startX, startY, ProfileManager.CurrentProfile.InvulnerableHue, ResGumps.InvulColor)
            );

            section2.AddRight(AddLabel(null, ResGumps.InvulColor, 0, 0), 2);
            section2.PopIndent();

            section2.Add
            (
                _showMobileNameIncoming = AddCheckBox
                    (null, ResGumps.ShowIncMobiles, ProfileManager.CurrentProfile.ShowNewMobileNameIncoming, startX, startY)
            );

            section2.Add
            (
                _showCorpseNameIncoming = AddCheckBox
                    (null, ResGumps.ShowIncCorpses, ProfileManager.CurrentProfile.ShowNewCorpseNameIncoming, startX, startY)
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
                    }, ProfileManager.CurrentProfile.AuraUnderFeetType, startX, startY, 100
                ), 2
            );

            section2.PushIndent();

            section2.Add
            (
                _partyAura = AddCheckBox
                    (null, ResGumps.CustomColorAuraForPartyMembers, ProfileManager.CurrentProfile.PartyAura, startX, startY)
            );

            section2.PushIndent();

            section2.Add
            (
                _partyAuraColorPickerBox = AddColorBox
                    (null, startX, startY, ProfileManager.CurrentProfile.PartyAuraHue, ResGumps.PartyAuraColor)
            );

            section2.AddRight(AddLabel(null, ResGumps.PartyAuraColor, 0, 0));
            section2.PopIndent();
            section2.PopIndent();

            SettingsSection section3 = AddSettingsSection(box, "Gumps & Context");
            section3.Y = section2.Bounds.Bottom + 40;

            section3.Add
            (
                _enableTopbar = AddCheckBox
                    (null, ResGumps.DisableMenu, ProfileManager.CurrentProfile.TopbarGumpIsDisabled, 0, 0)
            );

            section3.Add
            (
                _holdDownKeyAlt = AddCheckBox
                    (null, ResGumps.AltCloseGumps, ProfileManager.CurrentProfile.HoldDownKeyAltToCloseAnchored, 0, 0)
            );

            section3.Add
            (
                _holdAltToMoveGumps = AddCheckBox
                    (null, ResGumps.AltMoveGumps, ProfileManager.CurrentProfile.HoldAltToMoveGumps, 0, 0)
            );

            section3.Add
            (
                _closeAllAnchoredGumpsWithRClick = AddCheckBox
                (
                    null, ResGumps.ClickCloseAllGumps,
                    ProfileManager.CurrentProfile.CloseAllAnchoredGumpsInGroupWithRightClick, 0, 0
                )
            );

            section3.Add
            (
                _useStandardSkillsGump = AddCheckBox
                    (null, ResGumps.StandardSkillGump, ProfileManager.CurrentProfile.StandardSkillsGump, 0, 0)
            );

            section3.Add
            (
                _use_old_status_gump = AddCheckBox
                    (null, ResGumps.UseOldStatusGump, ProfileManager.CurrentProfile.UseOldStatusGump, startX, startY)
            );

            _use_old_status_gump.IsVisible = !CUOEnviroment.IsOutlands;

            section3.Add
            (
                _partyInviteGump = AddCheckBox
                    (null, ResGumps.ShowGumpPartyInv, ProfileManager.CurrentProfile.PartyInviteGump, 0, 0)
            );

            section3.Add
            (
                _customBars = AddCheckBox
                    (null, ResGumps.UseCustomHPBars, ProfileManager.CurrentProfile.CustomBarsToggled, 0, 0)
            );

            section3.AddRight
            (
                _customBarsBBG = AddCheckBox
                    (null, ResGumps.UseBlackBackgr, ProfileManager.CurrentProfile.CBBlackBGToggled, 0, 0)
            );

            section3.Add
            (
                _saveHealthbars = AddCheckBox
                    (null, ResGumps.SaveHPBarsOnLogout, ProfileManager.CurrentProfile.SaveHealthbars, 0, 0)
            );

            section3.PushIndent();
            section3.Add(AddLabel(null, ResGumps.CloseHPGumpWhen, 0, 0));

            mode = ProfileManager.CurrentProfile.CloseHealthBarType;

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
                    ProfileManager.CurrentProfile.GridLootType, startX, startY, 120
                ), 2
            );

            section3.Add
            (
                _holdShiftForContext = AddCheckBox
                    (null, ResGumps.ShiftContext, ProfileManager.CurrentProfile.HoldShiftForContext, 0, 0)
            );

            section3.Add
            (
                _holdShiftToSplitStack = AddCheckBox
                    (null, ResGumps.ShiftStack, ProfileManager.CurrentProfile.HoldShiftToSplitStack, 0, 0)
            );


            SettingsSection section4 = AddSettingsSection(box, "Miscellaneous");
            section4.Y = section3.Bounds.Bottom + 40;

            section4.Add
            (
                _useCircleOfTransparency = AddCheckBox
                    (null, ResGumps.EnableCircleTrans, ProfileManager.CurrentProfile.UseCircleOfTransparency, startX, startY)
            );

            section4.AddRight
            (
                _circleOfTranspRadius = AddHSlider
                (
                    null, Constants.MIN_CIRCLE_OF_TRANSPARENCY_RADIUS, Constants.MAX_CIRCLE_OF_TRANSPARENCY_RADIUS,
                    ProfileManager.CurrentProfile.CircleOfTransparencyRadius, startX, startY, 200
                )
            );

            section4.PushIndent();
            section4.Add(AddLabel(null, ResGumps.CircleTransType, startX, startY));
            int cottypeindex = ProfileManager.CurrentProfile.CircleOfTransparencyType;
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
                    null, ResGumps.HideScreenshotStoredInMessage, ProfileManager.CurrentProfile.HideScreenshotStoredInMessage,
                    0, 0
                )
            );

            section4.Add
            (
                _objectsFading = AddCheckBox
                    (null, ResGumps.ObjAlphaFading, ProfileManager.CurrentProfile.UseObjectsFading, startX, startY)
            );

            section4.Add
            (
                _textFading = AddCheckBox
                    (null, ResGumps.TextAlphaFading, ProfileManager.CurrentProfile.TextFading, startX, startY)
            );

            section4.Add
            (
                _showTargetRangeIndicator = AddCheckBox
                    (null, ResGumps.ShowTarRangeIndic, ProfileManager.CurrentProfile.ShowTargetRangeIndicator, startX, startY)
            );

            section4.Add
            (
                _enableDragSelect = AddCheckBox
                    (null, ResGumps.EnableDragHPBars, ProfileManager.CurrentProfile.EnableDragSelect, startX, startY)
            );

            section4.PushIndent();
            section4.Add(AddLabel(null, ResGumps.DragKey, startX, startY));

            section4.AddRight
            (
                _dragSelectModifierKey = AddCombobox
                (
                    null, new[] { ResGumps.KeyMod_None, ResGumps.KeyMod_Ctrl, ResGumps.KeyMod_Shift },
                    ProfileManager.CurrentProfile.DragSelectModifierKey, startX, startY, 100
                )
            );

            section4.Add
            (
                _dragSelectHumanoidsOnly = AddCheckBox
                    (null, ResGumps.DragHumanoidsOnly, ProfileManager.CurrentProfile.DragSelectHumanoidsOnly, startX, startY)
            );

            section4.PopIndent();


            SettingsSection section5 = AddSettingsSection(box, "Terrain & Statics");
            section5.Y = section4.Bounds.Bottom + 40;

            section5.Add
            (
                _drawRoofs = AddCheckBox
                    (null, ResGumps.HideRoofTiles, !ProfileManager.CurrentProfile.DrawRoofs, startX, startY)
            );

            section5.Add
            (
                _treeToStumps = AddCheckBox
                    (null, ResGumps.TreesStumps, ProfileManager.CurrentProfile.TreeToStumps, startX, startY)
            );

            section5.Add
            (
                _hideVegetation = AddCheckBox
                    (null, ResGumps.HideVegetation, ProfileManager.CurrentProfile.HideVegetation, startX, startY)
            );

            section5.Add
            (
                _enableCaveBorder = AddCheckBox
                    (null, ResGumps.MarkCaveTiles, ProfileManager.CurrentProfile.EnableCaveBorder, startX, startY)
            );

            section5.Add(AddLabel(null, ResGumps.HPFields, startX, startY));
            mode = ProfileManager.CurrentProfile.FieldsType;

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

            _enableSounds = AddCheckBox(rightArea, ResGumps.Sounds, ProfileManager.CurrentProfile.EnableSound, startX, startY);

            _enableMusic = AddCheckBox
            (
                rightArea, ResGumps.Music, ProfileManager.CurrentProfile.EnableMusic, startX, startY + _enableSounds.Height + 2
            );

            _loginMusic = AddCheckBox
            (
                rightArea, ResGumps.LoginMusic, Settings.GlobalSettings.LoginMusic, startX,
                startY + _enableSounds.Height + 2 + _enableMusic.Height + 2
            );

            startX = 120;
            startY += 2;

            _soundsVolume = AddHSlider
                (rightArea, 0, 100, ProfileManager.CurrentProfile.SoundVolume, startX, startY, VOLUME_WIDTH);

            _musicVolume = AddHSlider
            (
                rightArea, 0, 100, ProfileManager.CurrentProfile.MusicVolume, startX, startY + _enableSounds.Height + 2,
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
                (rightArea, ResGumps.PlayFootsteps, ProfileManager.CurrentProfile.EnableFootstepsSound, startX, startY);

            startY += _footStepsSound.Height + 2;

            _combatMusic = AddCheckBox
                (rightArea, ResGumps.CombatMusic, ProfileManager.CurrentProfile.EnableCombatMusic, startX, startY);

            startY += _combatMusic.Height + 2;

            _musicInBackground = AddCheckBox
            (
                rightArea, ResGumps.ReproduceSoundsAndMusic, ProfileManager.CurrentProfile.ReproduceSoundsInBackground, startX,
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
                (rightArea, ResGumps.FPSInactive, ProfileManager.CurrentProfile.ReduceFPSWhenInactive, startX, startY);

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
                    null, ResGumps.AlwaysUseFullsizeGameWindow, ProfileManager.CurrentProfile.GameWindowFullSize, startX,
                    startY
                )
            );

            section.Add
            (
                _windowBorderless = AddCheckBox
                    (null, ResGumps.BorderlessWindow, ProfileManager.CurrentProfile.WindowBorderless, startX, startY)
            );

            section.Add
            (
                _gameWindowLock = AddCheckBox
                    (null, ResGumps.LockGameWindowMovingResizing, ProfileManager.CurrentProfile.GameWindowLock, startX, startY)
            );

            section.Add(AddLabel(null, ResGumps.GamePlayWindowPosition, startX, startY));

            section.AddRight
            (
                _gameWindowPositionX = AddInputField
                    (null, startX, startY, 50, TEXTBOX_HEIGHT, null, 80, false, true, 5), 4
            );

            _gameWindowPositionX.SetText(ProfileManager.CurrentProfile.GameWindowPosition.X.ToString());

            section.AddRight
            (
                _gameWindowPositionY = AddInputField(null, startX, startY, 50, TEXTBOX_HEIGHT, null, 80, false, true, 5)
            );

            _gameWindowPositionY.SetText(ProfileManager.CurrentProfile.GameWindowPosition.Y.ToString());


            section.Add(AddLabel(null, ResGumps.GamePlayWindowSize, startX, startY));

            section.AddRight
                (_gameWindowWidth = AddInputField(null, startX, startY, 50, TEXTBOX_HEIGHT, null, 80, false, true, 5));

            _gameWindowWidth.SetText(ProfileManager.CurrentProfile.GameWindowSize.X.ToString());

            section.AddRight
                (_gameWindowHeight = AddInputField(null, startX, startY, 50, TEXTBOX_HEIGHT, null, 80, false, true, 5));

            _gameWindowHeight.SetText(ProfileManager.CurrentProfile.GameWindowSize.Y.ToString());


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
                    null, ResGumps.EnableMouseWheelForZoom, ProfileManager.CurrentProfile.EnableMousewheelScaleZoom, startX,
                    startY
                )
            );

            section2.Add
            (
                _restorezoomCheckbox = AddCheckBox
                (
                    null, ResGumps.ReleasingCtrlRestoresScale, ProfileManager.CurrentProfile.RestoreScaleAfterUnpressCtrl,
                    startX, startY
                )
            );


            SettingsSection section3 = AddSettingsSection(box, "Lights");
            section3.Y = section2.Bounds.Bottom + 40;

            section3.Add
            (
                _altLights = AddCheckBox
                    (null, ResGumps.AlternativeLights, ProfileManager.CurrentProfile.UseAlternativeLights, startX, startY)
            );

            section3.Add
            (
                _enableLight = AddCheckBox
                    (null, ResGumps.LightLevel, ProfileManager.CurrentProfile.UseCustomLightLevel, startX, startY)
            );

            section3.AddRight
                (_lightBar = AddHSlider(null, 0, 0x1E, 0x1E - ProfileManager.CurrentProfile.LightLevel, startX, startY, 250));

            section3.Add
            (
                _darkNights = AddCheckBox
                    (null, ResGumps.DarkNights, ProfileManager.CurrentProfile.UseDarkNights, startX, startY)
            );

            section3.Add
            (
                _useColoredLights = AddCheckBox
                    (null, ResGumps.UseColoredLights, ProfileManager.CurrentProfile.UseColoredLights, startX, startY)
            );


            SettingsSection section4 = AddSettingsSection(box, "Misc");
            section4.Y = section3.Bounds.Bottom + 40;

            section4.Add
            (
                _enableDeathScreen = AddCheckBox
                    (null, ResGumps.EnableDeathScreen, ProfileManager.CurrentProfile.EnableDeathScreen, startX, startY)
            );

            section4.AddRight
            (
                _enableBlackWhiteEffect = AddCheckBox
                (
                    null, ResGumps.BlackWhiteModeForDeadPlayer, ProfileManager.CurrentProfile.EnableBlackWhiteEffect, startX,
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
                    (null, ResGumps.AuraOnMouseTarget, ProfileManager.CurrentProfile.AuraOnMouse, startX, startY)
            );


            SettingsSection section5 = AddSettingsSection(box, "Shadows");
            section5.Y = section4.Bounds.Bottom + 40;

            section5.Add
            (
                _enableShadows = AddCheckBox
                    (null, ResGumps.Shadows, ProfileManager.CurrentProfile.ShadowsEnabled, startX, startY)
            );


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
                    }, ProfileManager.CurrentProfile.FilterType, startX, startY, 200
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
                (rightArea, ResGumps.UseTooltip, ProfileManager.CurrentProfile.UseTooltip, startX, startY);

            startY += _use_tooltip.Height + 2;

            startX += 40;

            Label text = AddLabel(rightArea, ResGumps.DelayBeforeDisplay, startX, startY);
            startX += text.Width + 5;

            _delay_before_display_tooltip = AddHSlider
                (rightArea, 0, 1000, ProfileManager.CurrentProfile.TooltipDelayBeforeDisplay, startX, startY, 200);

            startX = 5 + 40;
            startY += text.Height + 2;

            text = AddLabel(rightArea, ResGumps.TooltipZoom, startX, startY);
            startX += text.Width + 5;

            _tooltip_zoom = AddHSlider
                (rightArea, 100, 200, ProfileManager.CurrentProfile.TooltipDisplayZoom, startX, startY, 200);

            startX = 5 + 40;
            startY += text.Height + 2;

            text = AddLabel(rightArea, ResGumps.TooltipBackgroundOpacity, startX, startY);
            startX += text.Width + 5;

            _tooltip_background_opacity = AddHSlider
                (rightArea, 0, 100, ProfileManager.CurrentProfile.TooltipBackgroundOpacity, startX, startY, 200);

            startX = 5 + 40;
            startY += text.Height + 2;

            _tooltip_font_hue = AddColorBox
                (rightArea, startX, startY, ProfileManager.CurrentProfile.TooltipTextHue, ResGumps.TooltipFontHue);

            startY += _tooltip_font_hue.Height + 2;

            startY += 15;

            text = AddLabel(rightArea, ResGumps.TooltipFont, startX, startY);
            startY += text.Height + 2;
            startX += 40;

            _tooltip_font_selector = new FontSelector(7, ProfileManager.CurrentProfile.TooltipFont, ResGumps.TooltipFontSelect)
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
                (rightArea, ResGumps.OverrideGameFont, ProfileManager.CurrentProfile.OverrideAllFonts, startX, startY);

            startX += _overrideAllFonts.Width + 5;

            _overrideAllFontsIsUnicodeCheckbox = AddCombobox
            (
                rightArea, new[]
                {
                    ResGumps.ASCII, ResGumps.Unicode
                }, ProfileManager.CurrentProfile.OverrideAllFontsIsUnicode ? 1 : 0, startX, startY, 100
            );

            startX = 5;
            startY += _overrideAllFonts.Height + 2;

            _forceUnicodeJournal = AddCheckBox
                (rightArea, ResGumps.ForceUnicodeInJournal, ProfileManager.CurrentProfile.ForceUnicodeJournal, startX, startY);

            startY += _forceUnicodeJournal.Height + 2;

            Label text = AddLabel(rightArea, ResGumps.SpeechFont, startX, startY);
            startX += 40;
            startY += text.Height + 2;

            _fontSelectorChat = new FontSelector(20, ProfileManager.CurrentProfile.ChatFont, ResGumps.ThatSClassicUO)
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
                (rightArea, ResGumps.ScaleSpeechDelay, ProfileManager.CurrentProfile.ScaleSpeechDelay, startX, startY);

            startX += _scaleSpeechDelay.Width + 5;

            _sliderSpeechDelay = AddHSlider
                (rightArea, 0, 1000, ProfileManager.CurrentProfile.SpeechDelay, startX, startY, 180);

            startX = 5;
            startY += _scaleSpeechDelay.Height + 2;

            _saveJournalCheckBox = AddCheckBox
            (
                rightArea, ResGumps.SaveJournalToFileInGameFolder, ProfileManager.CurrentProfile.SaveJournalToFile, startX,
                startY
            );

            startY += _saveJournalCheckBox.Height + 2;

            if (!ProfileManager.CurrentProfile.SaveJournalToFile)
            {
                World.Journal.CloseWriter();
            }

            _chatAfterEnter = AddCheckBox
            (
                rightArea, ResGumps.ActiveChatWhenPressingEnter, ProfileManager.CurrentProfile.ActivateChatAfterEnter, startX,
                startY
            );

            startX += 40;
            startY += _chatAfterEnter.Height + 2;

            _chatAdditionalButtonsCheckbox = AddCheckBox
            (
                rightArea, ResGumps.UseAdditionalButtonsToActivateChat,
                ProfileManager.CurrentProfile.ActivateChatAdditionalButtons, startX, startY
            );

            startY += _chatAdditionalButtonsCheckbox.Height + 2;

            _chatShiftEnterCheckbox = AddCheckBox
            (
                rightArea, ResGumps.UseShiftEnterToSendMessage, ProfileManager.CurrentProfile.ActivateChatShiftEnterSupport,
                startX, startY
            );

            startY += _chatShiftEnterCheckbox.Height + 2;
            startX = 5;

            _hideChatGradient = AddCheckBox
                (rightArea, ResGumps.HideChatGradient, ProfileManager.CurrentProfile.HideChatGradient, startX, startY);

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
                ProfileManager.CurrentProfile.SpeechHue = speechHue;
                _speechColorPickerBox.SetColor(speechHue, HuesLoader.Instance.GetPolygoneColor(12, speechHue));
                ProfileManager.CurrentProfile.EmoteHue = emoteHue;
                _emoteColorPickerBox.SetColor(emoteHue, HuesLoader.Instance.GetPolygoneColor(12, emoteHue));
                ProfileManager.CurrentProfile.YellHue = yellHue;
                _yellColorPickerBox.SetColor(yellHue, HuesLoader.Instance.GetPolygoneColor(12, yellHue));
                ProfileManager.CurrentProfile.WhisperHue = whisperHue;
                _whisperColorPickerBox.SetColor(whisperHue, HuesLoader.Instance.GetPolygoneColor(12, whisperHue));
            };

            rightArea.Add(_randomizeColorsButton);
            startY += _randomizeColorsButton.Height + 2 + 20;


            _speechColorPickerBox = AddColorBox
                (rightArea, startX, startY, ProfileManager.CurrentProfile.SpeechHue, ResGumps.SpeechColor);

            startX += 200;

            _emoteColorPickerBox = AddColorBox
                (rightArea, startX, startY, ProfileManager.CurrentProfile.EmoteHue, ResGumps.EmoteColor);

            startY += _emoteColorPickerBox.Height + 2;
            startX = 5;

            _yellColorPickerBox = AddColorBox
                (rightArea, startX, startY, ProfileManager.CurrentProfile.YellHue, ResGumps.YellColor);

            startX += 200;

            _whisperColorPickerBox = AddColorBox
                (rightArea, startX, startY, ProfileManager.CurrentProfile.WhisperHue, ResGumps.WhisperColor);

            startY += _whisperColorPickerBox.Height + 2;
            startX = 5;

            _partyMessageColorPickerBox = AddColorBox
                (rightArea, startX, startY, ProfileManager.CurrentProfile.PartyMessageHue, ResGumps.PartyMessageColor);

            startX += 200;

            _guildMessageColorPickerBox = AddColorBox
                (rightArea, startX, startY, ProfileManager.CurrentProfile.GuildMessageHue, ResGumps.GuildMessageColor);

            startY += _guildMessageColorPickerBox.Height + 2;
            startX = 5;

            _allyMessageColorPickerBox = AddColorBox
                (rightArea, startX, startY, ProfileManager.CurrentProfile.AllyMessageHue, ResGumps.AllianceMessageColor);

            startX += 200;

            _chatMessageColorPickerBox = AddColorBox
                (rightArea, startX, startY, ProfileManager.CurrentProfile.ChatMessageHue, ResGumps.ChatMessageColor);

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
                (rightArea, ResGumps.TabCombat, ProfileManager.CurrentProfile.HoldDownKeyTab, startX, startY);

            startY += _holdDownKeyTab.Height + 2;

            _queryBeforAttackCheckbox = AddCheckBox
                (rightArea, ResGumps.QueryAttack, ProfileManager.CurrentProfile.EnabledCriminalActionQuery, startX, startY);

            startY += _queryBeforAttackCheckbox.Height + 2;

            _queryBeforeBeneficialCheckbox = AddCheckBox
            (
                rightArea, ResGumps.QueryBeneficialActs, ProfileManager.CurrentProfile.EnabledBeneficialCriminalActionQuery,
                startX, startY
            );

            startY += _queryBeforeBeneficialCheckbox.Height + 2;

            _spellFormatCheckbox = AddCheckBox
            (
                rightArea, ResGumps.EnableOverheadSpellFormat, ProfileManager.CurrentProfile.EnabledSpellFormat, startX, startY
            );

            startY += _spellFormatCheckbox.Height + 2;

            _spellColoringCheckbox = AddCheckBox
                (rightArea, ResGumps.EnableOverheadSpellHue, ProfileManager.CurrentProfile.EnabledSpellHue, startX, startY);

            startY += _spellColoringCheckbox.Height + 2;

            _castSpellsByOneClick = AddCheckBox
                (rightArea, ResGumps.CastSpellsByOneClick, ProfileManager.CurrentProfile.CastSpellsByOneClick, startX, startY);

            startY += _castSpellsByOneClick.Height + 2;

            _buffBarTime = AddCheckBox
                (rightArea, ResGumps.ShowBuffDuration, ProfileManager.CurrentProfile.BuffBarTime, startX, startY);

            startY += _buffBarTime.Height + 2;

            startY += 40;

            int initialY = startY;

            _innocentColorPickerBox = AddColorBox
                (rightArea, startX, startY, ProfileManager.CurrentProfile.InnocentHue, ResGumps.InnocentColor);

            startY += _innocentColorPickerBox.Height + 2;

            _friendColorPickerBox = AddColorBox
                (rightArea, startX, startY, ProfileManager.CurrentProfile.FriendHue, ResGumps.FriendColor);

            startY += _innocentColorPickerBox.Height + 2;

            _crimialColorPickerBox = AddColorBox
                (rightArea, startX, startY, ProfileManager.CurrentProfile.CriminalHue, ResGumps.CriminalColor);

            startY += _innocentColorPickerBox.Height + 2;

            _genericColorPickerBox = AddColorBox
                (rightArea, startX, startY, ProfileManager.CurrentProfile.AnimalHue, ResGumps.AnimalColor);

            startY += _innocentColorPickerBox.Height + 2;

            _murdererColorPickerBox = AddColorBox
                (rightArea, startX, startY, ProfileManager.CurrentProfile.MurdererHue, ResGumps.MurdererColor);

            startY += _innocentColorPickerBox.Height + 2;

            _enemyColorPickerBox = AddColorBox
                (rightArea, startX, startY, ProfileManager.CurrentProfile.EnemyHue, ResGumps.EnemyColor);

            startY += _innocentColorPickerBox.Height + 2;

            startY = initialY;
            startX += 200;

            _beneficColorPickerBox = AddColorBox
                (rightArea, startX, startY, ProfileManager.CurrentProfile.BeneficHue, ResGumps.BeneficSpellHue);

            startY += _beneficColorPickerBox.Height + 2;

            _harmfulColorPickerBox = AddColorBox
                (rightArea, startX, startY, ProfileManager.CurrentProfile.HarmfulHue, ResGumps.HarmfulSpellHue);

            startY += _harmfulColorPickerBox.Height + 2;

            _neutralColorPickerBox = AddColorBox
                (rightArea, startX, startY, ProfileManager.CurrentProfile.NeutralHue, ResGumps.NeutralSpellHue);

            startY += _neutralColorPickerBox.Height + 2;

            startX = 5;
            startY += (_neutralColorPickerBox.Height + 2) * 4;

            _spellFormatBox = AddInputField
                (rightArea, startX, startY, 200, TEXTBOX_HEIGHT, ResGumps.SpellOverheadFormat, 0, true, false, 30);

            _spellFormatBox.SetText(ProfileManager.CurrentProfile.SpellDisplayFormat);

            Add(rightArea, PAGE);
        }

        private void BuildCounters()
        {
            const int PAGE = 9;
            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            int startX = 5;
            int startY = 5;


            _enableCounters = AddCheckBox
                (rightArea, ResGumps.EnableCounters, ProfileManager.CurrentProfile.CounterBarEnabled, startX, startY);

            startX += 40;
            startY += _enableCounters.Height + 2;

            _highlightOnUse = AddCheckBox
                (rightArea, ResGumps.HighlightOnUse, ProfileManager.CurrentProfile.CounterBarHighlightOnUse, startX, startY);

            startY += _highlightOnUse.Height + 2;

            _enableAbbreviatedAmount = AddCheckBox
            (
                rightArea, ResGumps.EnableAbbreviatedAmountCountrs,
                ProfileManager.CurrentProfile.CounterBarDisplayAbbreviatedAmount, startX, startY
            );

            startX += _enableAbbreviatedAmount.Width + 5;

            _abbreviatedAmount = AddInputField(rightArea, startX, startY, 50, TEXTBOX_HEIGHT, null, 80, false, true);

            _abbreviatedAmount.SetText(ProfileManager.CurrentProfile.CounterBarAbbreviatedAmount.ToString());

            startX = 5;
            startX += 40;
            startY += _enableAbbreviatedAmount.Height + 2;

            _highlightOnAmount = AddCheckBox
            (
                rightArea, ResGumps.HighlightRedWhenBelow, ProfileManager.CurrentProfile.CounterBarHighlightOnAmount, startX,
                startY
            );

            startX += _highlightOnAmount.Width + 5;

            _highlightAmount = AddInputField(rightArea, startX, startY, 50, TEXTBOX_HEIGHT, null, 80, false, true, 2);

            _highlightAmount.SetText(ProfileManager.CurrentProfile.CounterBarHighlightAmount.ToString());

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
            _cellSize = AddHSlider(rightArea, 30, 80, ProfileManager.CurrentProfile.CounterBarCellSize, startX, startY, 80);


            startX = initialX;
            startY += text.Height + 2 + 15;

            _rows = AddInputField(rightArea, startX, startY, 50, 30, ResGumps.Counter_Rows, 80, false, true, 5);

            _rows.SetText(ProfileManager.CurrentProfile.CounterBarRows.ToString());


            startX += _rows.Width + 5 + 100;

            _columns = AddInputField(rightArea, startX, startY, 50, 30, ResGumps.Counter_Columns, 80, false, true, 5);

            _columns.SetText(ProfileManager.CurrentProfile.CounterBarColumns.ToString());


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
                rightArea, ResGumps.DisableDefaultUOHotkeys, ProfileManager.CurrentProfile.DisableDefaultHotkeys, startX,
                startY
            );

            startX += 40;
            startY += _disableDefaultHotkeys.Height + 2;

            _disableArrowBtn = AddCheckBox
            (
                rightArea, ResGumps.DisableArrowsPlayerMovement, ProfileManager.CurrentProfile.DisableArrowBtn, startX, startY
            );

            startY += _disableArrowBtn.Height + 2;

            _disableTabBtn = AddCheckBox
                (rightArea, ResGumps.DisableTab, ProfileManager.CurrentProfile.DisableTabBtn, startX, startY);

            startY += _disableTabBtn.Height + 2;

            _disableCtrlQWBtn = AddCheckBox
                (rightArea, ResGumps.DisableMessageHistory, ProfileManager.CurrentProfile.DisableCtrlQWBtn, startX, startY);

            startY += _disableCtrlQWBtn.Height + 2;

            _disableAutoMove = AddCheckBox
                (rightArea, ResGumps.DisableClickAutomove, ProfileManager.CurrentProfile.DisableAutoMove, startX, startY);

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
                (rightArea, ResGumps.ShowInfoBar, ProfileManager.CurrentProfile.ShowInfoBar, startX, startY);

            startX += 40;
            startY += _showInfoBar.Height + 2;

            Label text = AddLabel(rightArea, ResGumps.DataHighlightType, startX, startY);

            startX += text.Width + 5;

            _infoBarHighlightType = AddCombobox
            (
                rightArea, new[] { ResGumps.TextColor, ResGumps.ColoredBars },
                ProfileManager.CurrentProfile.InfoBarHighlightType, startX, startY, 150
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
                ProfileManager.CurrentProfile.ContainersScale, startX, startY, 200
            );

            startX = 5;
            startY += text.Height + 2;

            _containerScaleItems = AddCheckBox
            (
                rightArea, ResGumps.ScaleItemsInsideContainers, ProfileManager.CurrentProfile.ScaleItemsInsideContainers,
                startX, startY
            );

            startY += _containerScaleItems.Height + 2;

            _useLargeContianersGumps = AddCheckBox
            (
                rightArea, ResGumps.UseLargeContainersGump, ProfileManager.CurrentProfile.UseLargeContainerGumps, startX,
                startY
            );

            _useLargeContianersGumps.IsVisible = Client.Version >= ClientVersion.CV_706000;

            if (_useLargeContianersGumps.IsVisible)
            {
                startY += _useLargeContianersGumps.Height + 2;
            }

            _containerDoubleClickToLoot = AddCheckBox
            (
                rightArea, ResGumps.DoubleClickLootContainers, ProfileManager.CurrentProfile.DoubleClickToLootInsideContainers,
                startX, startY
            );

            startY += _containerDoubleClickToLoot.Height + 2;

            _relativeDragAnDropItems = AddCheckBox
            (
                rightArea, ResGumps.RelativeDragAndDropContainers, ProfileManager.CurrentProfile.RelativeDragAndDropItems,
                startX, startY
            );

            startY += _relativeDragAnDropItems.Height + 2;

            _highlightContainersWhenMouseIsOver = AddCheckBox
            (
                rightArea, ResGumps.HighlightContainerWhenSelected,
                ProfileManager.CurrentProfile.HighlightContainerWhenSelected, startX, startY
            );

            startY += _highlightContainersWhenMouseIsOver.Height + 2;

            _overrideContainerLocation = AddCheckBox
            (
                rightArea, ResGumps.OverrideContainerGumpLocation, ProfileManager.CurrentProfile.OverrideContainerLocation,
                startX, startY
            );

            startX += _overrideContainerLocation.Width + 5;

            _overrideContainerLocationSetting = AddCombobox
            (
                rightArea, new[]
                {
                    ResGumps.ContLoc_NearContainerPosition, ResGumps.ContLoc_TopRight,
                    ResGumps.ContLoc_LastDraggedPosition, ResGumps.ContLoc_RememberEveryContainer
                }, ProfileManager.CurrentProfile.OverrideContainerLocationSetting, startX, startY, 200
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
                    _use_old_status_gump.IsChecked = false;
                    _auraType.SelectedIndex = 0;
                    _fieldsType.SelectedIndex = 0;

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
                    ProfileManager.CurrentProfile.DefaultScale = 1f;
                    _lightBar.Value = 0;
                    _enableLight.IsChecked = false;
                    _useColoredLights.IsChecked = false;
                    _darkNights.IsChecked = false;
                    _enableShadows.IsChecked = true;
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

            ProfileManager.CurrentProfile.HighlightGameObjects = _highlightObjects.IsChecked;
            ProfileManager.CurrentProfile.ReduceFPSWhenInactive = _reduceFPSWhenInactive.IsChecked;
            ProfileManager.CurrentProfile.EnablePathfind = _enablePathfind.IsChecked;
            ProfileManager.CurrentProfile.UseShiftToPathfind = _useShiftPathfind.IsChecked;
            ProfileManager.CurrentProfile.AlwaysRun = _alwaysRun.IsChecked;
            ProfileManager.CurrentProfile.AlwaysRunUnlessHidden = _alwaysRunUnlessHidden.IsChecked;
            ProfileManager.CurrentProfile.ShowMobilesHP = _showHpMobile.IsChecked;
            ProfileManager.CurrentProfile.HighlightMobilesByFlags = _highlightByState.IsChecked;
            ProfileManager.CurrentProfile.PoisonHue = _poisonColorPickerBox.Hue;
            ProfileManager.CurrentProfile.ParalyzedHue = _paralyzedColorPickerBox.Hue;
            ProfileManager.CurrentProfile.InvulnerableHue = _invulnerableColorPickerBox.Hue;
            ProfileManager.CurrentProfile.MobileHPType = _hpComboBox.SelectedIndex;
            ProfileManager.CurrentProfile.MobileHPShowWhen = _hpComboBoxShowWhen.SelectedIndex;
            ProfileManager.CurrentProfile.HoldDownKeyTab = _holdDownKeyTab.IsChecked;
            ProfileManager.CurrentProfile.HoldDownKeyAltToCloseAnchored = _holdDownKeyAlt.IsChecked;

            ProfileManager.CurrentProfile.CloseAllAnchoredGumpsInGroupWithRightClick =
                _closeAllAnchoredGumpsWithRClick.IsChecked;

            ProfileManager.CurrentProfile.HoldShiftForContext = _holdShiftForContext.IsChecked;
            ProfileManager.CurrentProfile.HoldAltToMoveGumps = _holdAltToMoveGumps.IsChecked;
            ProfileManager.CurrentProfile.HoldShiftToSplitStack = _holdShiftToSplitStack.IsChecked;
            ProfileManager.CurrentProfile.CloseHealthBarType = _healtbarType.SelectedIndex;
            ProfileManager.CurrentProfile.HideScreenshotStoredInMessage = _hideScreenshotStoredInMessage.IsChecked;

            if (ProfileManager.CurrentProfile.DrawRoofs == _drawRoofs.IsChecked)
            {
                ProfileManager.CurrentProfile.DrawRoofs = !_drawRoofs.IsChecked;

                Client.Game.GetScene<GameScene>()?.UpdateMaxDrawZ(true);
            }

            if (ProfileManager.CurrentProfile.TopbarGumpIsDisabled != _enableTopbar.IsChecked)
            {
                if (_enableTopbar.IsChecked)
                {
                    UIManager.GetGump<TopBarGump>()?.Dispose();
                }
                else
                {
                    TopBarGump.Create();
                }

                ProfileManager.CurrentProfile.TopbarGumpIsDisabled = _enableTopbar.IsChecked;
            }

            if (ProfileManager.CurrentProfile.EnableCaveBorder != _enableCaveBorder.IsChecked)
            {
                StaticFilters.CleanCaveTextures();
                ProfileManager.CurrentProfile.EnableCaveBorder = _enableCaveBorder.IsChecked;
            }

            if (ProfileManager.CurrentProfile.TreeToStumps != _treeToStumps.IsChecked)
            {
                StaticFilters.CleanTreeTextures();
                ProfileManager.CurrentProfile.TreeToStumps = _treeToStumps.IsChecked;
            }

            ProfileManager.CurrentProfile.FieldsType = _fieldsType.SelectedIndex;
            ProfileManager.CurrentProfile.HideVegetation = _hideVegetation.IsChecked;
            ProfileManager.CurrentProfile.NoColorObjectsOutOfRange = _noColorOutOfRangeObjects.IsChecked;
            ProfileManager.CurrentProfile.UseCircleOfTransparency = _useCircleOfTransparency.IsChecked;

            if (ProfileManager.CurrentProfile.CircleOfTransparencyRadius != _circleOfTranspRadius.Value)
            {
                ProfileManager.CurrentProfile.CircleOfTransparencyRadius = _circleOfTranspRadius.Value;
                CircleOfTransparency.Create(ProfileManager.CurrentProfile.CircleOfTransparencyRadius);
            }

            ProfileManager.CurrentProfile.CircleOfTransparencyType = _cotType.SelectedIndex;
            ProfileManager.CurrentProfile.StandardSkillsGump = _useStandardSkillsGump.IsChecked;

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

            ProfileManager.CurrentProfile.ShowNewMobileNameIncoming = _showMobileNameIncoming.IsChecked;
            ProfileManager.CurrentProfile.ShowNewCorpseNameIncoming = _showCorpseNameIncoming.IsChecked;
            ProfileManager.CurrentProfile.GridLootType = _gridLoot.SelectedIndex;
            ProfileManager.CurrentProfile.SallosEasyGrab = _sallosEasyGrab.IsChecked;
            ProfileManager.CurrentProfile.PartyInviteGump = _partyInviteGump.IsChecked;
            ProfileManager.CurrentProfile.UseObjectsFading = _objectsFading.IsChecked;
            ProfileManager.CurrentProfile.TextFading = _textFading.IsChecked;
            ProfileManager.CurrentProfile.UseSmoothBoatMovement = _use_smooth_boat_movement.IsChecked;

            if (ProfileManager.CurrentProfile.ShowHouseContent != _showHouseContent.IsChecked)
            {
                ProfileManager.CurrentProfile.ShowHouseContent = _showHouseContent.IsChecked;
                NetClient.Socket.Send(new PShowPublicHouseContent(ProfileManager.CurrentProfile.ShowHouseContent));
            }


            // sounds
            ProfileManager.CurrentProfile.EnableSound = _enableSounds.IsChecked;
            ProfileManager.CurrentProfile.EnableMusic = _enableMusic.IsChecked;
            ProfileManager.CurrentProfile.EnableFootstepsSound = _footStepsSound.IsChecked;
            ProfileManager.CurrentProfile.EnableCombatMusic = _combatMusic.IsChecked;
            ProfileManager.CurrentProfile.ReproduceSoundsInBackground = _musicInBackground.IsChecked;
            ProfileManager.CurrentProfile.SoundVolume = _soundsVolume.Value;
            ProfileManager.CurrentProfile.MusicVolume = _musicVolume.Value;
            Settings.GlobalSettings.LoginMusicVolume = _loginMusicVolume.Value;
            Settings.GlobalSettings.LoginMusic = _loginMusic.IsChecked;

            Client.Game.Scene.Audio.UpdateCurrentMusicVolume();
            Client.Game.Scene.Audio.UpdateCurrentSoundsVolume();

            if (!ProfileManager.CurrentProfile.EnableMusic)
            {
                Client.Game.Scene.Audio.StopMusic();
            }

            if (!ProfileManager.CurrentProfile.EnableSound)
            {
                Client.Game.Scene.Audio.StopSounds();
            }

            // speech
            ProfileManager.CurrentProfile.ScaleSpeechDelay = _scaleSpeechDelay.IsChecked;
            ProfileManager.CurrentProfile.SpeechDelay = _sliderSpeechDelay.Value;
            ProfileManager.CurrentProfile.SpeechHue = _speechColorPickerBox.Hue;
            ProfileManager.CurrentProfile.EmoteHue = _emoteColorPickerBox.Hue;
            ProfileManager.CurrentProfile.YellHue = _yellColorPickerBox.Hue;
            ProfileManager.CurrentProfile.WhisperHue = _whisperColorPickerBox.Hue;
            ProfileManager.CurrentProfile.PartyMessageHue = _partyMessageColorPickerBox.Hue;
            ProfileManager.CurrentProfile.GuildMessageHue = _guildMessageColorPickerBox.Hue;
            ProfileManager.CurrentProfile.AllyMessageHue = _allyMessageColorPickerBox.Hue;
            ProfileManager.CurrentProfile.ChatMessageHue = _chatMessageColorPickerBox.Hue;

            if (ProfileManager.CurrentProfile.ActivateChatAfterEnter != _chatAfterEnter.IsChecked)
            {
                UIManager.SystemChat.IsActive = !_chatAfterEnter.IsChecked;
                ProfileManager.CurrentProfile.ActivateChatAfterEnter = _chatAfterEnter.IsChecked;
            }

            ProfileManager.CurrentProfile.ActivateChatAdditionalButtons = _chatAdditionalButtonsCheckbox.IsChecked;
            ProfileManager.CurrentProfile.ActivateChatShiftEnterSupport = _chatShiftEnterCheckbox.IsChecked;
            ProfileManager.CurrentProfile.SaveJournalToFile = _saveJournalCheckBox.IsChecked;

            // video
            ProfileManager.CurrentProfile.EnableDeathScreen = _enableDeathScreen.IsChecked;
            ProfileManager.CurrentProfile.EnableBlackWhiteEffect = _enableBlackWhiteEffect.IsChecked;

            Client.Game.Scene.Camera.ZoomIndex = _sliderZoom.Value;
            ProfileManager.CurrentProfile.DefaultScale = Client.Game.Scene.Camera.Zoom;
            ProfileManager.CurrentProfile.EnableMousewheelScaleZoom = _zoomCheckbox.IsChecked;
            ProfileManager.CurrentProfile.RestoreScaleAfterUnpressCtrl = _restorezoomCheckbox.IsChecked;

            if (!CUOEnviroment.IsOutlands && _use_old_status_gump.IsChecked != ProfileManager.CurrentProfile.UseOldStatusGump)
            {
                StatusGumpBase status = StatusGumpBase.GetStatusGump();

                ProfileManager.CurrentProfile.UseOldStatusGump = _use_old_status_gump.IsChecked;

                if (status != null)
                {
                    status.Dispose();
                    UIManager.Add(StatusGumpBase.AddStatusGump(status.ScreenCoordinateX, status.ScreenCoordinateY));
                }
            }


            int.TryParse(_gameWindowWidth.Text, out int gameWindowSizeWidth);
            int.TryParse(_gameWindowHeight.Text, out int gameWindowSizeHeight);

            if (gameWindowSizeWidth != ProfileManager.CurrentProfile.GameWindowSize.X ||
                gameWindowSizeHeight != ProfileManager.CurrentProfile.GameWindowSize.Y)
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

            if (gameWindowPositionX != ProfileManager.CurrentProfile.GameWindowPosition.X ||
                gameWindowPositionY != ProfileManager.CurrentProfile.GameWindowPosition.Y)
            {
                if (vp != null)
                {
                    vp.Location = ProfileManager.CurrentProfile.GameWindowPosition =
                        new Point(gameWindowPositionX, gameWindowPositionY);
                }
            }

            if (ProfileManager.CurrentProfile.GameWindowLock != _gameWindowLock.IsChecked)
            {
                if (vp != null)
                {
                    vp.CanMove = !_gameWindowLock.IsChecked;
                }

                ProfileManager.CurrentProfile.GameWindowLock = _gameWindowLock.IsChecked;
            }

            if (_gameWindowFullsize.IsChecked && (gameWindowPositionX != -5 || gameWindowPositionY != -5))
            {
                if (ProfileManager.CurrentProfile.GameWindowFullSize == _gameWindowFullsize.IsChecked)
                {
                    _gameWindowFullsize.IsChecked = false;
                }
            }

            if (ProfileManager.CurrentProfile.GameWindowFullSize != _gameWindowFullsize.IsChecked)
            {
                Point n = Point.Zero, loc = Point.Zero;

                if (_gameWindowFullsize.IsChecked)
                {
                    if (vp != null)
                    {
                        n = vp.ResizeGameWindow
                            (new Point(Client.Game.Window.ClientBounds.Width, Client.Game.Window.ClientBounds.Height));

                        loc = ProfileManager.CurrentProfile.GameWindowPosition = vp.Location = new Point(-5, -5);
                    }
                }
                else
                {
                    if (vp != null)
                    {
                        n = vp.ResizeGameWindow(new Point(600, 480));
                        loc = vp.Location = ProfileManager.CurrentProfile.GameWindowPosition = new Point(20, 20);
                    }
                }

                _gameWindowPositionX.SetText(loc.X.ToString());
                _gameWindowPositionY.SetText(loc.Y.ToString());
                _gameWindowWidth.SetText(n.X.ToString());
                _gameWindowHeight.SetText(n.Y.ToString());

                ProfileManager.CurrentProfile.GameWindowFullSize = _gameWindowFullsize.IsChecked;
            }

            if (ProfileManager.CurrentProfile.WindowBorderless != _windowBorderless.IsChecked)
            {
                ProfileManager.CurrentProfile.WindowBorderless = _windowBorderless.IsChecked;
                Client.Game.SetWindowBorderless(_windowBorderless.IsChecked);
            }

            ProfileManager.CurrentProfile.UseAlternativeLights = _altLights.IsChecked;
            ProfileManager.CurrentProfile.UseCustomLightLevel = _enableLight.IsChecked;
            ProfileManager.CurrentProfile.LightLevel = (byte) (_lightBar.MaxValue - _lightBar.Value);

            if (_enableLight.IsChecked)
            {
                World.Light.Overall = ProfileManager.CurrentProfile.LightLevel;
                World.Light.Personal = 0;
            }
            else
            {
                World.Light.Overall = World.Light.RealOverall;
                World.Light.Personal = World.Light.RealPersonal;
            }

            ProfileManager.CurrentProfile.UseColoredLights = _useColoredLights.IsChecked;
            ProfileManager.CurrentProfile.UseDarkNights = _darkNights.IsChecked;
            ProfileManager.CurrentProfile.ShadowsEnabled = _enableShadows.IsChecked;
            ProfileManager.CurrentProfile.AuraUnderFeetType = _auraType.SelectedIndex;
            ProfileManager.CurrentProfile.FilterType = _filterType.SelectedIndex;

            Client.Game.IsMouseVisible =
                Settings.GlobalSettings.RunMouseInASeparateThread = _runMouseInSeparateThread.IsChecked;

            ProfileManager.CurrentProfile.AuraOnMouse = _auraMouse.IsChecked;
            ProfileManager.CurrentProfile.PartyAura = _partyAura.IsChecked;
            ProfileManager.CurrentProfile.PartyAuraHue = _partyAuraColorPickerBox.Hue;
            ProfileManager.CurrentProfile.HideChatGradient = _hideChatGradient.IsChecked;

            // fonts
            ProfileManager.CurrentProfile.ForceUnicodeJournal = _forceUnicodeJournal.IsChecked;
            byte _fontValue = _fontSelectorChat.GetSelectedFont();
            ProfileManager.CurrentProfile.OverrideAllFonts = _overrideAllFonts.IsChecked;
            ProfileManager.CurrentProfile.OverrideAllFontsIsUnicode = _overrideAllFontsIsUnicodeCheckbox.SelectedIndex == 1;

            if (ProfileManager.CurrentProfile.ChatFont != _fontValue)
            {
                ProfileManager.CurrentProfile.ChatFont = _fontValue;
                UIManager.SystemChat.TextBoxControl.Font = _fontValue;
            }

            // combat
            ProfileManager.CurrentProfile.InnocentHue = _innocentColorPickerBox.Hue;
            ProfileManager.CurrentProfile.FriendHue = _friendColorPickerBox.Hue;
            ProfileManager.CurrentProfile.CriminalHue = _crimialColorPickerBox.Hue;
            ProfileManager.CurrentProfile.AnimalHue = _genericColorPickerBox.Hue;
            ProfileManager.CurrentProfile.EnemyHue = _enemyColorPickerBox.Hue;
            ProfileManager.CurrentProfile.MurdererHue = _murdererColorPickerBox.Hue;
            ProfileManager.CurrentProfile.EnabledCriminalActionQuery = _queryBeforAttackCheckbox.IsChecked;
            ProfileManager.CurrentProfile.EnabledBeneficialCriminalActionQuery = _queryBeforeBeneficialCheckbox.IsChecked;
            ProfileManager.CurrentProfile.CastSpellsByOneClick = _castSpellsByOneClick.IsChecked;
            ProfileManager.CurrentProfile.BuffBarTime = _buffBarTime.IsChecked;

            ProfileManager.CurrentProfile.BeneficHue = _beneficColorPickerBox.Hue;
            ProfileManager.CurrentProfile.HarmfulHue = _harmfulColorPickerBox.Hue;
            ProfileManager.CurrentProfile.NeutralHue = _neutralColorPickerBox.Hue;
            ProfileManager.CurrentProfile.EnabledSpellHue = _spellColoringCheckbox.IsChecked;
            ProfileManager.CurrentProfile.EnabledSpellFormat = _spellFormatCheckbox.IsChecked;
            ProfileManager.CurrentProfile.SpellDisplayFormat = _spellFormatBox.Text;

            // macros
            Client.Game.GetScene<GameScene>().Macros.Save();

            // counters

            bool before = ProfileManager.CurrentProfile.CounterBarEnabled;
            ProfileManager.CurrentProfile.CounterBarEnabled = _enableCounters.IsChecked;
            ProfileManager.CurrentProfile.CounterBarCellSize = _cellSize.Value;
            ProfileManager.CurrentProfile.CounterBarRows = int.Parse(_rows.Text);
            ProfileManager.CurrentProfile.CounterBarColumns = int.Parse(_columns.Text);
            ProfileManager.CurrentProfile.CounterBarHighlightOnUse = _highlightOnUse.IsChecked;

            ProfileManager.CurrentProfile.CounterBarHighlightAmount = int.Parse(_highlightAmount.Text);
            ProfileManager.CurrentProfile.CounterBarAbbreviatedAmount = int.Parse(_abbreviatedAmount.Text);
            ProfileManager.CurrentProfile.CounterBarHighlightOnAmount = _highlightOnAmount.IsChecked;
            ProfileManager.CurrentProfile.CounterBarDisplayAbbreviatedAmount = _enableAbbreviatedAmount.IsChecked;

            CounterBarGump counterGump = UIManager.GetGump<CounterBarGump>();

            counterGump?.SetLayout
            (
                ProfileManager.CurrentProfile.CounterBarCellSize, ProfileManager.CurrentProfile.CounterBarRows,
                ProfileManager.CurrentProfile.CounterBarColumns
            );


            if (before != ProfileManager.CurrentProfile.CounterBarEnabled)
            {
                if (counterGump == null)
                {
                    if (ProfileManager.CurrentProfile.CounterBarEnabled)
                    {
                        UIManager.Add
                        (
                            new CounterBarGump
                            (
                                200, 200, ProfileManager.CurrentProfile.CounterBarCellSize,
                                ProfileManager.CurrentProfile.CounterBarRows, ProfileManager.CurrentProfile.CounterBarColumns
                            )
                        );
                    }
                }
                else
                {
                    counterGump.IsEnabled = counterGump.IsVisible = ProfileManager.CurrentProfile.CounterBarEnabled;
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
            ProfileManager.CurrentProfile.DisableDefaultHotkeys = _disableDefaultHotkeys.IsChecked;
            ProfileManager.CurrentProfile.DisableArrowBtn = _disableArrowBtn.IsChecked;
            ProfileManager.CurrentProfile.DisableTabBtn = _disableTabBtn.IsChecked;
            ProfileManager.CurrentProfile.DisableCtrlQWBtn = _disableCtrlQWBtn.IsChecked;
            ProfileManager.CurrentProfile.DisableAutoMove = _disableAutoMove.IsChecked;
            ProfileManager.CurrentProfile.AutoOpenDoors = _autoOpenDoors.IsChecked;
            ProfileManager.CurrentProfile.SmoothDoors = _smoothDoors.IsChecked;
            ProfileManager.CurrentProfile.AutoOpenCorpses = _autoOpenCorpse.IsChecked;
            ProfileManager.CurrentProfile.AutoOpenCorpseRange = int.Parse(_autoOpenCorpseRange.Text);
            ProfileManager.CurrentProfile.CorpseOpenOptions = _autoOpenCorpseOptions.SelectedIndex;
            ProfileManager.CurrentProfile.SkipEmptyCorpse = _skipEmptyCorpse.IsChecked;

            ProfileManager.CurrentProfile.EnableDragSelect = _enableDragSelect.IsChecked;
            ProfileManager.CurrentProfile.DragSelectModifierKey = _dragSelectModifierKey.SelectedIndex;
            ProfileManager.CurrentProfile.DragSelectHumanoidsOnly = _dragSelectHumanoidsOnly.IsChecked;

            ProfileManager.CurrentProfile.OverrideContainerLocation = _overrideContainerLocation.IsChecked;
            ProfileManager.CurrentProfile.OverrideContainerLocationSetting = _overrideContainerLocationSetting.SelectedIndex;

            ProfileManager.CurrentProfile.ShowTargetRangeIndicator = _showTargetRangeIndicator.IsChecked;


            bool updateHealthBars = ProfileManager.CurrentProfile.CustomBarsToggled != _customBars.IsChecked;
            ProfileManager.CurrentProfile.CustomBarsToggled = _customBars.IsChecked;

            if (updateHealthBars)
            {
                if (ProfileManager.CurrentProfile.CustomBarsToggled)
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

            ProfileManager.CurrentProfile.CBBlackBGToggled = _customBarsBBG.IsChecked;
            ProfileManager.CurrentProfile.SaveHealthbars = _saveHealthbars.IsChecked;


            // infobar
            ProfileManager.CurrentProfile.ShowInfoBar = _showInfoBar.IsChecked;
            ProfileManager.CurrentProfile.InfoBarHighlightType = _infoBarHighlightType.SelectedIndex;


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

            if (ProfileManager.CurrentProfile.ShowInfoBar)
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
            int containerScale = ProfileManager.CurrentProfile.ContainersScale;

            if ((byte) _containersScale.Value != containerScale || ProfileManager.CurrentProfile.ScaleItemsInsideContainers !=
                _containerScaleItems.IsChecked)
            {
                containerScale = ProfileManager.CurrentProfile.ContainersScale = (byte) _containersScale.Value;
                UIManager.ContainerScale = containerScale / 100f;
                ProfileManager.CurrentProfile.ScaleItemsInsideContainers = _containerScaleItems.IsChecked;

                foreach (ContainerGump resizableGump in UIManager.Gumps.OfType<ContainerGump>())
                {
                    resizableGump.RequestUpdateContents();
                }
            }

            ProfileManager.CurrentProfile.UseLargeContainerGumps = _useLargeContianersGumps.IsChecked;
            ProfileManager.CurrentProfile.DoubleClickToLootInsideContainers = _containerDoubleClickToLoot.IsChecked;
            ProfileManager.CurrentProfile.RelativeDragAndDropItems = _relativeDragAnDropItems.IsChecked;
            ProfileManager.CurrentProfile.HighlightContainerWhenSelected = _highlightContainersWhenMouseIsOver.IsChecked;


            // tooltip
            ProfileManager.CurrentProfile.UseTooltip = _use_tooltip.IsChecked;
            ProfileManager.CurrentProfile.TooltipTextHue = _tooltip_font_hue.Hue;
            ProfileManager.CurrentProfile.TooltipDelayBeforeDisplay = _delay_before_display_tooltip.Value;
            ProfileManager.CurrentProfile.TooltipBackgroundOpacity = _tooltip_background_opacity.Value;
            ProfileManager.CurrentProfile.TooltipDisplayZoom = _tooltip_zoom.Value;
            ProfileManager.CurrentProfile.TooltipFont = _tooltip_font_selector.GetSelectedFont();

            ProfileManager.CurrentProfile?.Save(UIManager.Gumps.OfType<Gump>().Where(s => s.CanBeSaved).Reverse().ToList());
        }

        internal void UpdateVideo()
        {
            _gameWindowWidth.SetText(ProfileManager.CurrentProfile.GameWindowSize.X.ToString());
            _gameWindowHeight.SetText(ProfileManager.CurrentProfile.GameWindowSize.Y.ToString());
            _gameWindowPositionX.SetText(ProfileManager.CurrentProfile.GameWindowPosition.X.ToString());
            _gameWindowPositionY.SetText(ProfileManager.CurrentProfile.GameWindowPosition.Y.ToString());
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