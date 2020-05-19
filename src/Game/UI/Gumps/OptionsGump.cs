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
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Gumps
{
    internal class OptionsGump : Gump
    {
        private const byte FONT = 0xFF;
        private const ushort HUE_FONT = 999;
        private const int SPACE_Y = 2;
        private const int WIDTH = 700;
        private const int HEIGHT = 500;

        private static UOTexture _logoTexture2D;
        private ScrollAreaItem _activeChatArea;
        private Combobox _autoOpenCorpseOptions;
        private TextBox _autoOpenCorpseRange;
        private Checkbox _buffBarTime,_castSpellsByOneClick, _queryBeforAttackCheckbox, _queryBeforeBeneficialCheckbox, _spellColoringCheckbox, _spellFormatCheckbox;
        private HSliderBar _cellSize;

        // video
        private Checkbox _use_old_status_gump, _windowBorderless, _enableDeathScreen, _enableBlackWhiteEffect, _altLights, _enableLight, _enableShadows, _auraMouse, _xBR, _runMouseInSeparateThread, _useColoredLights, _darkNights, _partyAura, _hideChatGradient;
        private ScrollAreaItem _defaultHotkeysArea, _autoOpenCorpseArea, _dragSelectArea;
        private Combobox _dragSelectModifierKey;
        private HSliderBar _brighlight, _sliderZoom;
        private Combobox _auraType;


        //counters
        private Checkbox _enableCounters, _highlightOnUse, _highlightOnAmount, _enableAbbreviatedAmount;
        private Checkbox _enableDragSelect, _dragSelectHumanoidsOnly;
        private TextBox _rows, _columns, _highlightAmount, _abbreviatedAmount;

        //experimental
        private Checkbox  _autoOpenDoors, _autoOpenCorpse, _skipEmptyCorpse, _disableTabBtn, _disableCtrlQWBtn, _disableDefaultHotkeys, _disableArrowBtn, _disableAutoMove, _overrideContainerLocation, _smoothDoors, _showTargetRangeIndicator, _customBars, _customBarsBBG, _saveHealthbars;
        private Combobox _overrideContainerLocationSetting;
        private Checkbox _use_smooth_boat_movement;

        // sounds
        private Checkbox _enableSounds, _enableMusic, _footStepsSound, _combatMusic, _musicInBackground, _loginMusic;

        // fonts
        private FontSelector _fontSelectorChat;
        private TextBox _gameWindowHeight;
        private Checkbox _overrideAllFonts;
        private Combobox _overrideAllFontsIsUnicodeCheckbox;
        private Checkbox _forceUnicodeJournal;

        private Checkbox _gameWindowLock, _gameWindowFullsize;
        // GameWindowPosition
        private TextBox _gameWindowPositionX;
        private TextBox _gameWindowPositionY;

        // GameWindowSize
        private TextBox _gameWindowWidth;
        private Combobox _gridLoot;
        private Checkbox _highlightObjects, /*_smoothMovements,*/ _enablePathfind, _useShiftPathfind, _alwaysRun, _alwaysRunUnlessHidden, _showHpMobile, _highlightByState, _drawRoofs, _treeToStumps, _hideVegetation, _noColorOutOfRangeObjects, _useCircleOfTransparency, _enableTopbar, _holdDownKeyTab, _holdDownKeyAlt, _closeAllAnchoredGumpsWithRClick, _chatAfterEnter, _chatAdditionalButtonsCheckbox, _chatShiftEnterCheckbox, _enableCaveBorder;
        private Combobox _hpComboBox, _healtbarType, _fieldsType, _hpComboBoxShowWhen;

        // combat & spells
        private ColorBox _innocentColorPickerBox, _friendColorPickerBox, _crimialColorPickerBox, _genericColorPickerBox, _enemyColorPickerBox, _murdererColorPickerBox, _neutralColorPickerBox, _beneficColorPickerBox, _harmfulColorPickerBox;
        private HSliderBar _lightBar;

        // macro
        private MacroControl _macroControl;
        private Checkbox _restorezoomCheckbox, _savezoomCheckbox, _zoomCheckbox;

        // infobar
        private List<InfoBarBuilderControl> _infoBarBuilderControls;
        private Checkbox _showInfoBar;
        private Combobox _infoBarHighlightType;

        // speech
        private Checkbox _scaleSpeechDelay, _saveJournalCheckBox;
        private NiceButton _randomizeColorsButton;

        // general
        private HSliderBar _sliderFPS, _circleOfTranspRadius;
        private HSliderBar _sliderSpeechDelay;
        private HSliderBar _soundsVolume, _musicVolume, _loginMusicVolume;
        private ColorBox _speechColorPickerBox, _emoteColorPickerBox, _yellColorPickerBox, _whisperColorPickerBox, _partyMessageColorPickerBox, _guildMessageColorPickerBox, _allyMessageColorPickerBox, _chatMessageColorPickerBox, _partyAuraColorPickerBox;
        private ColorBox _poisonColorPickerBox, _paralyzedColorPickerBox, _invulnerableColorPickerBox;
        private TextBox _spellFormatBox;
        private Checkbox _useStandardSkillsGump, _showMobileNameIncoming, _showCorpseNameIncoming;
        private Checkbox _holdShiftForContext, _holdShiftToSplitStack, _reduceFPSWhenInactive, _sallosEasyGrab, _partyInviteGump, _objectsFading, _textFading, _holdAltToMoveGumps;
        private Checkbox _showHouseContent;
        private Combobox _cotType;

        //VendorGump Size Option
        private ArrowNumbersTextBox _vendorGumpSize;

        private ScrollAreaItem _windowSizeArea;
        private ScrollAreaItem _zoomSizeArea;


        // containers
        private HSliderBar _containersScale;
        private Checkbox _containerScaleItems, _containerDoubleClickToLoot, _relativeDragAnDropItems, _useLargeContianersGumps;

        public OptionsGump() : base(0, 0)
        {
            Add(new AlphaBlendControl(0.05f)
            {
                X = 1,
                Y = 1,
                Width = WIDTH - 2,
                Height = HEIGHT - 2
            });


            TextureControl tc = new TextureControl
            {
                X = 150 + ((WIDTH - 150 - 350) >> 1),
                Y = (HEIGHT - 365) >> 1,
                Width = LogoTexture.Width,
                Height = LogoTexture.Height,
                Alpha = 0.95f,
                ScaleTexture = false,
                Texture = LogoTexture
            };

            Add(tc);

            int i = 0;
            Add(new NiceButton(10, 10 + (30 * (i++)), 140, 25, ButtonAction.SwitchPage, "General") {IsSelected = true, ButtonParameter = 1});
            Add(new NiceButton(10, 10 + (30 * (i++)), 140, 25, ButtonAction.SwitchPage, "Sound") {ButtonParameter = 2});
            Add(new NiceButton(10, 10 + (30 * (i++)), 140, 25, ButtonAction.SwitchPage, "Video") {ButtonParameter = 3});
            Add(new NiceButton(10, 10 + (30 * (i++)), 140, 25, ButtonAction.SwitchPage, "Macros") {ButtonParameter = 4});
            Add(new NiceButton(10, 10 + (30 * (i++)), 140, 25, ButtonAction.SwitchPage, "Tooltip") {ButtonParameter = 5});
            Add(new NiceButton(10, 10 + (30 * (i++)), 140, 25, ButtonAction.SwitchPage, "Fonts") {ButtonParameter = 6});
            Add(new NiceButton(10, 10 + (30 * (i++)), 140, 25, ButtonAction.SwitchPage, "Speech") {ButtonParameter = 7});
            Add(new NiceButton(10, 10 + (30 * (i++)), 140, 25, ButtonAction.SwitchPage, "Combat-Spells") {ButtonParameter = 8});
            Add(new NiceButton(10, 10 + (30 * (i++)), 140, 25, ButtonAction.SwitchPage, "Counters") {ButtonParameter = 9});
            Add(new NiceButton(10, 10 + (30 * (i++)), 140, 25, ButtonAction.SwitchPage, "Info Bar") { ButtonParameter = 10 });
            Add(new NiceButton(10, 10 + (30 * (i++)), 140, 25, ButtonAction.SwitchPage, "Containers") { ButtonParameter = 11 });
            Add(new NiceButton(10, 10 + (30 * (i++)), 140, 25, ButtonAction.SwitchPage, "Experimental") { ButtonParameter = 12 });


            Add(new Line(160, 5, 1, HEIGHT - 10, Color.Gray.PackedValue));

            int offsetX = 60;
            int offsetY = 60;

            Add(new Line(160, 405 + 35 + 1, WIDTH - 160, 1, Color.Gray.PackedValue));

            Add(new Button((int) Buttons.Cancel, 0x00F3, 0x00F1, 0x00F2)
            {
                X = 154 + offsetX, Y = 405 + offsetY, ButtonAction = ButtonAction.Activate
            });

            Add(new Button((int) Buttons.Apply, 0x00EF, 0x00F0, 0x00EE)
            {
                X = 248 + offsetX, Y = 405 + offsetY, ButtonAction = ButtonAction.Activate
            });

            Add(new Button((int) Buttons.Default, 0x00F6, 0x00F4, 0x00F5)
            {
                X = 346 + offsetX, Y = 405 + offsetY, ButtonAction = ButtonAction.Activate
            });

            Add(new Button((int) Buttons.Ok, 0x00F9, 0x00F8, 0x00F7)
            {
                X = 443 + offsetX, Y = 405 + offsetY, ButtonAction = ButtonAction.Activate
            });

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

        private static UOTexture LogoTexture
        {
            get
            {
                if (_logoTexture2D == null || _logoTexture2D.IsDisposed)
                {
                    Stream stream = typeof(CUOEnviroment).Assembly.GetManifestResourceStream("ClassicUO.cuologo.png");
                    Texture2D.TextureDataFromStreamEXT(stream, out int w, out int h, out byte[] pixels, 350, 365);

                    _logoTexture2D = new UOTexture32(w, h);
                    _logoTexture2D.SetData(pixels);
                }

                return _logoTexture2D;
            }
        }

        private void BuildGeneral()
        {
            const int PAGE = 1;

            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            ScrollAreaItem fpsItem = new ScrollAreaItem();
            Label text = new Label("- FPS:", true, HUE_FONT);
            fpsItem.Add(text);
            _sliderFPS = new HSliderBar(text.X + 90, 5, 250, Constants.MIN_FPS, Constants.MAX_FPS, Settings.GlobalSettings.FPS, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT);
            fpsItem.Add(_sliderFPS);
            rightArea.Add(fpsItem);


            _reduceFPSWhenInactive = CreateCheckBox(rightArea, "Reduce FPS when game is inactive", ProfileManager.Current.ReduceFPSWhenInactive, 0, SPACE_Y);

            _highlightObjects = CreateCheckBox(rightArea, "Highlight game objects", ProfileManager.Current.HighlightGameObjects, 0, 20 + SPACE_Y);
            _enablePathfind = CreateCheckBox(rightArea, "Enable pathfinding", ProfileManager.Current.EnablePathfind, 0, SPACE_Y);
            _useShiftPathfind = CreateCheckBox(rightArea, "Use SHIFT for pathfinding", ProfileManager.Current.UseShiftToPathfind, 0, SPACE_Y);

            ScrollAreaItem alwaysRunItem = new ScrollAreaItem();
            _alwaysRun = new Checkbox(0x00D2, 0x00D3, "Always run", FONT, HUE_FONT)
            {
                Y = SPACE_Y,
                IsChecked = ProfileManager.Current.AlwaysRun
            };
            rightArea.Add(_alwaysRun);
            _alwaysRun.ValueChanged += (sender, e) => { alwaysRunItem.IsVisible = _alwaysRun.IsChecked; };

            _alwaysRunUnlessHidden = new Checkbox(0x00D2, 0x00D3, "Unless hidden", FONT, HUE_FONT)
            {
                X = 20,
                Y = 5,
                IsChecked = ProfileManager.Current.AlwaysRunUnlessHidden
            };
            _alwaysRunUnlessHidden.Height += 5;
            alwaysRunItem.Add(_alwaysRunUnlessHidden);
            rightArea.Add(alwaysRunItem);

            alwaysRunItem.IsVisible = _alwaysRun.IsChecked;

            _enableTopbar = CreateCheckBox(rightArea, "Disable the Menu Bar", ProfileManager.Current.TopbarGumpIsDisabled, 0, SPACE_Y);
            _holdDownKeyTab = CreateCheckBox(rightArea, "Hold TAB key for combat", ProfileManager.Current.HoldDownKeyTab, 0, SPACE_Y);
            _holdDownKeyAlt = CreateCheckBox(rightArea, "Hold ALT key + right click to close Anchored gumps", ProfileManager.Current.HoldDownKeyAltToCloseAnchored, 0, SPACE_Y);
            _closeAllAnchoredGumpsWithRClick = CreateCheckBox(rightArea, "Close all Anchored gumps when right click on a group", ProfileManager.Current.CloseAllAnchoredGumpsInGroupWithRightClick, 0, SPACE_Y);
            _holdAltToMoveGumps = CreateCheckBox(rightArea, "Hold ALT key to move gumps", ProfileManager.Current.HoldAltToMoveGumps, 0, SPACE_Y);
            _holdShiftForContext = CreateCheckBox(rightArea, "Hold Shift for Context Menus", ProfileManager.Current.HoldShiftForContext, 0, SPACE_Y);
            _holdShiftToSplitStack = CreateCheckBox(rightArea, "Hold Shift to split stack of items", ProfileManager.Current.HoldShiftToSplitStack, 0, SPACE_Y);
            _highlightByState = CreateCheckBox(rightArea, "Highlight by state (poisoned, yellow hits, paralyzed)", ProfileManager.Current.HighlightMobilesByFlags, 0, SPACE_Y);
            _poisonColorPickerBox = CreateClickableColorBox(rightArea, 20, SPACE_Y, ProfileManager.Current.PoisonHue, "Poisoned Color", 40, SPACE_Y);
            _paralyzedColorPickerBox = CreateClickableColorBox(rightArea, 20, SPACE_Y, ProfileManager.Current.ParalyzedHue, "Paralyzed Color", 40, SPACE_Y);
            _invulnerableColorPickerBox = CreateClickableColorBox(rightArea, 20, SPACE_Y, ProfileManager.Current.InvulnerableHue, "Invulnerable Color", 40, SPACE_Y);
            _noColorOutOfRangeObjects = CreateCheckBox(rightArea, "No color for object out of range", ProfileManager.Current.NoColorObjectsOutOfRange, 0, SPACE_Y);
            _objectsFading = CreateCheckBox(rightArea, "Objects alpha fading", ProfileManager.Current.UseObjectsFading, 0, SPACE_Y);
            _textFading = CreateCheckBox(rightArea, "Text alpha fading", ProfileManager.Current.TextFading, 0, SPACE_Y);
            _useStandardSkillsGump = CreateCheckBox(rightArea, "Use standard skills gump", ProfileManager.Current.StandardSkillsGump, 0, SPACE_Y);
            _showMobileNameIncoming = CreateCheckBox(rightArea, "Show incoming new mobiles", ProfileManager.Current.ShowNewMobileNameIncoming, 0, SPACE_Y);
            _showCorpseNameIncoming = CreateCheckBox(rightArea, "Show incoming new corpses", ProfileManager.Current.ShowNewCorpseNameIncoming, 0, SPACE_Y);
            _sallosEasyGrab = CreateCheckBox(rightArea, "Sallos easy grab", ProfileManager.Current.SallosEasyGrab, 0, SPACE_Y);
            _partyInviteGump = CreateCheckBox(rightArea, "Show gump for party invites", ProfileManager.Current.PartyInviteGump, 0, SPACE_Y);          
            _showHouseContent = CreateCheckBox(rightArea, "Show houses content", ProfileManager.Current.ShowHouseContent, 0, SPACE_Y);
            _showHouseContent.IsVisible = Client.Version >= ClientVersion.CV_70796;
            _customBars = CreateCheckBox(rightArea, "Use Custom Health Bars", ProfileManager.Current.CustomBarsToggled, 0, SPACE_Y);
            _customBarsBBG = CreateCheckBox(rightArea, "Use All Black Backgrounds", ProfileManager.Current.CBBlackBGToggled, 20, SPACE_Y);
            _saveHealthbars = CreateCheckBox(rightArea, "Save healthbars on logout", ProfileManager.Current.SaveHealthbars, 0, SPACE_Y);
            _showTargetRangeIndicator = CreateCheckBox(rightArea, "Show target range indicator", ProfileManager.Current.ShowTargetRangeIndicator, 0, SPACE_Y);
            _enableDragSelect = CreateCheckBox(rightArea, "Enable drag-select to open health bars", ProfileManager.Current.EnableDragSelect, 0, SPACE_Y);
            _dragSelectArea = new ScrollAreaItem();
            text = new Label("Drag-select modifier key", true, HUE_FONT)
            {
                X = 20
            };
            _dragSelectArea.Add(text);
            _dragSelectModifierKey = new Combobox(text.Width + 80, text.Y, 100, new[] { "None", "Ctrl", "Shift" })
            {
                SelectedIndex = ProfileManager.Current.DragSelectModifierKey
            };
            _dragSelectArea.Add(_dragSelectModifierKey);
            _dragSelectHumanoidsOnly = new Checkbox(0x00D2, 0x00D3, "Select humanoids only", FONT, HUE_FONT, true)
            {
                IsChecked = ProfileManager.Current.DragSelectHumanoidsOnly,
                X = 20,
                Y = 20
            };
            _dragSelectArea.Add(_dragSelectHumanoidsOnly);
            _enableDragSelect.ValueChanged += (sender, e) => { _dragSelectArea.IsVisible = _enableDragSelect.IsChecked; };
            rightArea.Add(_dragSelectArea);
            _use_smooth_boat_movement = CreateCheckBox(rightArea, "Smooth boat movements", ProfileManager.Current.UseSmoothBoatMovement, 0, SPACE_Y);
            _use_smooth_boat_movement.IsVisible = Client.Version >= ClientVersion.CV_7090;
            _autoOpenDoors = CreateCheckBox(rightArea, "Auto Open Doors", ProfileManager.Current.AutoOpenDoors, 0, SPACE_Y);
            _smoothDoors = CreateCheckBox(rightArea, "Smooth doors", ProfileManager.Current.SmoothDoors, 20, SPACE_Y);
            _autoOpenCorpseArea = new ScrollAreaItem();
            _autoOpenCorpse = CreateCheckBox(rightArea, "Auto Open Corpses", ProfileManager.Current.AutoOpenCorpses, 0, SPACE_Y);
            _autoOpenCorpse.ValueChanged += (sender, e) => { _autoOpenCorpseArea.IsVisible = _autoOpenCorpse.IsChecked; };
            _skipEmptyCorpse = new Checkbox(0x00D2, 0x00D3, "Skip empty corpses", FONT, HUE_FONT)
            {
                X = 20,
                Y = 5,
                IsChecked = ProfileManager.Current.SkipEmptyCorpse
            };
            _autoOpenCorpseArea.Add(_skipEmptyCorpse);
            _autoOpenCorpseRange = CreateInputField(_autoOpenCorpseArea, new TextBox(FONT, 2, 80, 80)
            {
                X = 30,
                Y = _skipEmptyCorpse.Y + _skipEmptyCorpse.Height,
                Width = 50,
                Height = 30,
                NumericOnly = true,
                Text = ProfileManager.Current.AutoOpenCorpseRange.ToString()
            }, "Corpse Open Range:");
            text = new Label("Corpse Open Options:", true, HUE_FONT)
            {
                Y = _autoOpenCorpseRange.Y + _autoOpenCorpseRange.Height + 5,
                X = 25
            };
            _autoOpenCorpseArea.Add(text);
            _autoOpenCorpseOptions = new Combobox(text.X + text.Width + 5, text.Y, 150, new[]
            {
                "None", "Not Targeting", "Not Hiding", "Both"
            })
            {
                SelectedIndex = ProfileManager.Current.CorpseOpenOptions
            };
            _autoOpenCorpseArea.Add(_autoOpenCorpseOptions);

            _autoOpenCorpseArea.Y = SPACE_Y;
            rightArea.Add(_autoOpenCorpseArea);


            ScrollAreaItem item = new ScrollAreaItem();
            item.Y = SPACE_Y;
            _useCircleOfTransparency = new Checkbox(0x00D2, 0x00D3, "Enable circle of transparency", FONT, HUE_FONT)
            {
                Y = 0,
                IsChecked = ProfileManager.Current.UseCircleOfTransparency
            };
            item.Add(_useCircleOfTransparency);
            _circleOfTranspRadius = new HSliderBar(_useCircleOfTransparency.X + _useCircleOfTransparency.Width + 10, _useCircleOfTransparency.Y + 5, 200, Constants.MIN_CIRCLE_OF_TRANSPARENCY_RADIUS, Constants.MAX_CIRCLE_OF_TRANSPARENCY_RADIUS, ProfileManager.Current.CircleOfTransparencyRadius, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT);
            item.Add(_circleOfTranspRadius);

            var textT = new Label("Transparency type:", true, HUE_FONT)
            {
                X = 20,
                Y = _circleOfTranspRadius.Y + 5
            };
            item.Add(textT);

            int cottypeindex = ProfileManager.Current.CircleOfTransparencyType;
            var cotTypes = new[] { "Full", "Gradient" };

            if (cottypeindex < 0 || cottypeindex > cotTypes.Length)
                cottypeindex = 0;

            _cotType = new Combobox(textT.X + textT.Width + 20, 45, 150, cotTypes, cottypeindex, emptyString: cotTypes[cottypeindex]);
            item.Add(_cotType);
            _useCircleOfTransparency.ValueChanged += (sender, e) => { textT.IsVisible = _cotType.IsVisible = _circleOfTranspRadius.IsVisible = _useCircleOfTransparency.IsChecked; };
            textT.IsVisible = _cotType.IsVisible = _circleOfTranspRadius.IsVisible = _useCircleOfTransparency.IsChecked;
            rightArea.Add(item);


            _drawRoofs = CreateCheckBox(rightArea, "Hide roof tiles", !ProfileManager.Current.DrawRoofs, 0, SPACE_Y);
            _treeToStumps = CreateCheckBox(rightArea, "Tree to stumps", ProfileManager.Current.TreeToStumps, 0, SPACE_Y);
            _hideVegetation = CreateCheckBox(rightArea, "Hide vegetation", ProfileManager.Current.HideVegetation, 0, SPACE_Y);
            _enableCaveBorder = CreateCheckBox(rightArea, "Mark cave tiles", ProfileManager.Current.EnableCaveBorder, 0, SPACE_Y);


            fpsItem = new ScrollAreaItem();

            text = new Label("Grid Loot", true, HUE_FONT)
            {
                Y = _showCorpseNameIncoming.Bounds.Bottom + 5 + SPACE_Y
            };
            _gridLoot = new Combobox(text.X + text.Width + 10, text.Y, 200, new[] {"None", "Grid loot only", "Both"}, ProfileManager.Current.GridLootType);

            fpsItem.Add(text);
            fpsItem.Add(_gridLoot);

            rightArea.Add(fpsItem);

            _autoOpenCorpseArea.IsVisible = _autoOpenCorpse.IsChecked;
            _dragSelectArea.IsVisible = _enableDragSelect.IsChecked;
            
            ScrollAreaItem hpAreaItem = new ScrollAreaItem();

            _showHpMobile = new Checkbox(0x00D2, 0x00D3, "Show HP", FONT, HUE_FONT)
            {
                X = 0, Y = 20, IsChecked = ProfileManager.Current.ShowMobilesHP
            };

            hpAreaItem.Add(_showHpMobile);

            int mode = ProfileManager.Current.MobileHPType;

            if (mode < 0 || mode > 2)
                mode = 0;

            _hpComboBox = new Combobox(_showHpMobile.Bounds.Right + 10, 20, 150, new[]
            {
                "Percentage", "Line", "Both"
            }, mode);
            hpAreaItem.Add(_hpComboBox);

            text = new Label("mode:", true, HUE_FONT)
            {
                X = _showHpMobile.Bounds.Right + 170,
                Y = 20
            };
            hpAreaItem.Add(text);

            mode = ProfileManager.Current.MobileHPShowWhen;

            if (mode != 0 && mode > 2)
                mode = 0;

            _hpComboBoxShowWhen = new Combobox(text.Bounds.Right + 10, 20, 150, new[]
            {
                "Always", "Less than 100%", "Smart"
            }, mode);
            hpAreaItem.Add(_hpComboBoxShowWhen);

            mode = ProfileManager.Current.CloseHealthBarType;

            if (mode < 0 || mode > 2)
                mode = 0;

            text = new Label("Close healthbar gump when:", true, HUE_FONT)
            {
                Y = _hpComboBox.Bounds.Bottom + 10
            };
            hpAreaItem.Add(text);

            _healtbarType = new Combobox(text.Bounds.Right + 10, _hpComboBox.Bounds.Bottom + 10, 150, new[]
            {
                "None", "Mobile Out of Range", "Mobile is Dead"
            }, mode);
            hpAreaItem.Add(_healtbarType);

            text = new Label("Fields: ", true, HUE_FONT)
            {
                Y = _hpComboBox.Bounds.Bottom + 45
            };
            hpAreaItem.Add(text);

            mode = ProfileManager.Current.FieldsType;

            if (mode < 0 || mode > 2)
                mode = 0;

            _fieldsType = new Combobox(text.Bounds.Right + 10, _hpComboBox.Bounds.Bottom + 45, 150, new[]
            {
                "Normal fields", "Static fields", "Tile fields"
            }, mode);

            hpAreaItem.Add(_fieldsType);
            rightArea.Add(hpAreaItem);

            _circleOfTranspRadius.IsVisible = _useCircleOfTransparency.IsChecked;

            hpAreaItem = new ScrollAreaItem();
            Control c = new Label("Shop Gump Size (multiple of 60): ", true, HUE_FONT) {Y = 10};
            hpAreaItem.Add(c);
            hpAreaItem.Add(_vendorGumpSize = new ArrowNumbersTextBox(c.Width + 5, 10, 60, 60, 60, 240, FONT, hue: 1) {Text = ProfileManager.Current.VendorGumpHeight.ToString(), Tag = ProfileManager.Current.VendorGumpHeight});
            rightArea.Add(hpAreaItem);

            Add(rightArea, PAGE);
        }

        private void BuildSounds()
        {
            const int PAGE = 2;

            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);


            ScrollAreaItem item = new ScrollAreaItem();
           
            _enableSounds = new Checkbox(0x00D2, 0x00D3, "Sounds", FONT, HUE_FONT)
            {
                IsChecked = ProfileManager.Current.EnableSound,
                Y = SPACE_Y
            };
            _enableSounds.ValueChanged += (sender, e) => { _soundsVolume.IsVisible = _enableSounds.IsChecked; };
            item.Add(_enableSounds);
            _soundsVolume = new HSliderBar(90, SPACE_Y + 5, 180, 0, 100, ProfileManager.Current.SoundVolume, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT);
            item.Add(_soundsVolume);
            rightArea.Add(item);


            item = new ScrollAreaItem();
            item.Y = SPACE_Y;
            _enableMusic = new Checkbox(0x00D2, 0x00D3, "Music", FONT, HUE_FONT)
            {
                IsChecked = ProfileManager.Current.EnableMusic,
                Y = SPACE_Y
            };
            _enableMusic.ValueChanged += (sender, e) => { _musicVolume.IsVisible = _enableMusic.IsChecked; };
            item.Add(_enableMusic);
            _musicVolume = new HSliderBar(90, SPACE_Y + 5, 180, 0, 100, ProfileManager.Current.MusicVolume, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT);
            item.Add(_musicVolume);
            rightArea.Add(item);


            item = new ScrollAreaItem();
            item.Y = SPACE_Y;
            _loginMusic = new Checkbox(0x00D2, 0x00D3, "Login music", FONT, HUE_FONT)
            {
                IsChecked = Settings.GlobalSettings.LoginMusic,
                Y = SPACE_Y
            };
            _loginMusic.ValueChanged += (sender, e) => { _loginMusicVolume.IsVisible = _loginMusic.IsChecked; };
            item.Add(_loginMusic);
            _loginMusicVolume = new HSliderBar(90, SPACE_Y + 5, 180, 0, 100, Settings.GlobalSettings.LoginMusicVolume, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT);
            item.Add(_loginMusicVolume);
            rightArea.Add(item);


            _footStepsSound = CreateCheckBox(rightArea, "Play Footsteps", ProfileManager.Current.EnableFootstepsSound, 0, 10 + SPACE_Y);
            _combatMusic = CreateCheckBox(rightArea, "Combat music", ProfileManager.Current.EnableCombatMusic, 0, SPACE_Y);
            _musicInBackground = CreateCheckBox(rightArea, "Reproduce sounds and music when ClassicUO is not focused", ProfileManager.Current.ReproduceSoundsInBackground, 0, SPACE_Y);


            _loginMusicVolume.IsVisible = _loginMusic.IsChecked;
            _soundsVolume.IsVisible = _enableSounds.IsChecked;
            _musicVolume.IsVisible = _enableMusic.IsChecked;

            Add(rightArea, PAGE);
        }

        private void BuildVideo()
        {
            const int PAGE = 3;

            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);
            Label text;

            _windowBorderless = CreateCheckBox(rightArea, "Borderless window", ProfileManager.Current.WindowBorderless, 0, SPACE_Y);

            // [BLOCK] game size
            {
                _gameWindowFullsize = CreateCheckBox(rightArea, "Always use fullsize game window", ProfileManager.Current.GameWindowFullSize, 0, SPACE_Y);
                _gameWindowFullsize.ValueChanged += (sender, e) => { _windowSizeArea.IsVisible = !_gameWindowFullsize.IsChecked; };

                _windowSizeArea = new ScrollAreaItem();
                _windowSizeArea.Y = SPACE_Y;
                _gameWindowLock = new Checkbox(0x00D2, 0x00D3, "Lock game window moving/resizing", FONT, HUE_FONT)
                {
                    X = 20,
                    Y = 15,
                    IsChecked = ProfileManager.Current.GameWindowLock
                };
                _windowSizeArea.Add(_gameWindowLock);

                text = new Label("Game Play Window Size: ", true, HUE_FONT)
                {
                    X = 20,
                    Y = 40
                };
                _windowSizeArea.Add(text);

                _gameWindowWidth = CreateInputField(_windowSizeArea, new TextBox(FONT, 5, 80, 80)
                {
                    Text = ProfileManager.Current.GameWindowSize.X.ToString(),
                    X = 30,
                    Y = 70,
                    Width = 50,
                    Height = 30,
                    UNumericOnly = true
                }, "");

                _gameWindowHeight = CreateInputField(_windowSizeArea, new TextBox(FONT, 5, 80, 80)
                {
                    Text = ProfileManager.Current.GameWindowSize.Y.ToString(),
                    X = 100,
                    Y = 70,
                    Width = 50,
                    Height = 30,
                    UNumericOnly = true
                });

                text = new Label("Game Play Window Position: ", true, HUE_FONT)
                {
                    X = 205,
                    Y = 40
                };
                _windowSizeArea.Add(text);

                _gameWindowPositionX = CreateInputField(_windowSizeArea, new TextBox(FONT, 5, 80, 80)
                {
                    Text = ProfileManager.Current.GameWindowPosition.X.ToString(),
                    X = 215,
                    Y = 70,
                    Width = 50,
                    Height = 30,
                    NumericOnly = true
                });

                _gameWindowPositionY = CreateInputField(_windowSizeArea, new TextBox(FONT, 5, 80, 80)
                {
                    Text = ProfileManager.Current.GameWindowPosition.Y.ToString(),
                    X = 285,
                    Y = 70,
                    Width = 50,
                    Height = 30,
                    NumericOnly = true
                });

                rightArea.Add(_windowSizeArea);
                _windowSizeArea.IsVisible = !_gameWindowFullsize.IsChecked;
            }

            // [BLOCK] scale
            {

                ScrollAreaItem scaleSlider = new ScrollAreaItem();
                scaleSlider.Y = SPACE_Y;
                Label zoomSliderText = new Label("Default zoom (5 for standard zoom):", true, HUE_FONT);
                scaleSlider.Add(zoomSliderText);
                _sliderZoom = new HSliderBar(zoomSliderText.X, zoomSliderText.Height + 5, 250, 0, 20, Client.Game.GetScene<GameScene>().ScalePos, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT);
                scaleSlider.Add(_sliderZoom);
                rightArea.Add(scaleSlider);

                _zoomCheckbox = new Checkbox(0x00D2, 0x00D3, "Enable mousewheel for in game zoom scaling (Ctrl + Scroll)", FONT, HUE_FONT)
                {
                    IsChecked = ProfileManager.Current.EnableMousewheelScaleZoom,
                    Y = 5
                };
                _zoomCheckbox.ValueChanged += (sender, e) => { _zoomSizeArea.IsVisible = _zoomCheckbox.IsChecked; };

                rightArea.Add(_zoomCheckbox);

                _zoomSizeArea = new ScrollAreaItem();
                _zoomSizeArea.Y = SPACE_Y;
                _restorezoomCheckbox = new Checkbox(0x00D2, 0x00D3, "Releasing Ctrl Restores Scale", FONT, HUE_FONT)
                {
                    X = 20,
                    Y = 5,
                    IsChecked = ProfileManager.Current.RestoreScaleAfterUnpressCtrl
                };
                _zoomSizeArea.Add(_restorezoomCheckbox);

                rightArea.Add(_zoomSizeArea);

                _zoomSizeArea.IsVisible = _zoomCheckbox.IsChecked;
            }

            _enableDeathScreen = CreateCheckBox(rightArea, "Enable Death Screen", ProfileManager.Current.EnableDeathScreen, 0, SPACE_Y + 10);
            _enableBlackWhiteEffect = CreateCheckBox(rightArea, "Black & White mode for dead player", ProfileManager.Current.EnableBlackWhiteEffect, 0, SPACE_Y);
            _use_old_status_gump = CreateCheckBox(rightArea, "Use old status gump", ProfileManager.Current.UseOldStatusGump, 0, SPACE_Y);
            _use_old_status_gump.IsVisible = !CUOEnviroment.IsOutlands; // outlands

            //ScrollAreaItem item = new ScrollAreaItem();

            //text = new Label("- Status gump type:", true, HUE_FONT)
            //{
            //    Y = 30
            //};

            //item.Add(text);

            //_shardType = new Combobox(text.Width + 20, text.Y, 100, new[] {"Modern", "Old", "Outlands"})
            //{
            //    SelectedIndex = Settings.GlobalSettings.ShardType
            //};
            //_shardType.IsVisible = Settings.GlobalSettings.ShardType == 2;
            //item.Add(_shardType);
            //rightArea.Add(item);

            ScrollAreaItem item = new ScrollAreaItem();
            item.Y = 30;
            text = new Label("- Brighlight:", true, HUE_FONT)
            {
                Y = 30,
                IsVisible = false,
            };
            _brighlight = new HSliderBar(text.Width + 10, text.Y + 5, 250, 0, 100, (int) (ProfileManager.Current.Brighlight * 100f), HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT);
            _brighlight.IsVisible = false;
            item.Add(text);
            item.Add(_brighlight);
            rightArea.Add(item);

            item = new ScrollAreaItem();
            ScrollAreaItem lightscrollitem = new ScrollAreaItem();
            lightscrollitem.Y = SPACE_Y;

            _altLights = CreateCheckBox(rightArea, "Alternative lights", ProfileManager.Current.UseAlternativeLights, 0, SPACE_Y);
            _altLights.ValueChanged += (sender, e) =>
            {
                lightscrollitem.IsVisible = !_altLights.IsChecked;
            };
            lightscrollitem.IsVisible = !_altLights.IsChecked;
            _altLights.SetTooltip( "Sets light level to max but still renders lights" );

            _enableLight = new Checkbox(0x00D2, 0x00D3, "Light level", FONT, HUE_FONT)
            {
                IsChecked = ProfileManager.Current.UseCustomLightLevel
            };

            _lightBar = new HSliderBar(_enableLight.Width + 10, _enableLight.Y + 5, 250, 0, 0x1E, 0x1E - ProfileManager.Current.LightLevel, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT);

            _darkNights = new Checkbox(0x00D2, 0x00D3, "Dark nights", FONT, HUE_FONT)
            {
                Y = _enableLight.Height,
                IsChecked = ProfileManager.Current.UseDarkNights
            };

            lightscrollitem.Add(_enableLight);
            lightscrollitem.Add(_lightBar);
            lightscrollitem.Add(_darkNights);
            rightArea.Add(lightscrollitem);
            rightArea.Add(item);

            _useColoredLights = CreateCheckBox(rightArea, "Use colored lights", ProfileManager.Current.UseColoredLights, 0, SPACE_Y);
            _enableShadows = CreateCheckBox(rightArea, "Shadows", ProfileManager.Current.ShadowsEnabled, 0, SPACE_Y);
            

            item = new ScrollAreaItem();
            item.Y = SPACE_Y;
            text = new Label("- Aura under feet:", true, HUE_FONT);
            item.Add(text);

            _auraType = new Combobox(text.Width + 20, text.Y, 100, new[] {"None", "Warmode", "Ctrl+Shift", "Always"})
            {
                SelectedIndex = ProfileManager.Current.AuraUnderFeetType
            };
            item.Add(_auraType);
            rightArea.Add(item);

            _partyAura = CreateCheckBox(rightArea, "Custom color aura for party members", ProfileManager.Current.PartyAura, 0, SPACE_Y);
            _partyAuraColorPickerBox = CreateClickableColorBox(rightArea, 20, SPACE_Y, ProfileManager.Current.PartyAuraHue, "Party Aura Color", 40, SPACE_Y);
            _runMouseInSeparateThread = CreateCheckBox(rightArea, "Run mouse in a separate thread", Settings.GlobalSettings.RunMouseInASeparateThread, 0, SPACE_Y);
            _auraMouse = CreateCheckBox(rightArea, "Aura on mouse target", ProfileManager.Current.AuraOnMouse, 0, SPACE_Y);
            _xBR = CreateCheckBox(rightArea, "Use xBR effect [BETA]", ProfileManager.Current.UseXBR, 0, SPACE_Y);
            _hideChatGradient = CreateCheckBox(rightArea, "Hide Chat Gradient", ProfileManager.Current.HideChatGradient, 0, SPACE_Y);

            Add(rightArea, PAGE);
        }


        private void BuildCommands()
        {
            const int PAGE = 4;

            ScrollArea rightArea = new ScrollArea(190, 52 + 25 + 4, 150, 360, true);
            NiceButton addButton = new NiceButton(190, 20, 130, 20, ButtonAction.Activate, "New macro") {IsSelectable = false, ButtonParameter = (int) Buttons.NewMacro};

            addButton.MouseUp += (sender, e) =>
            {
                EntryDialog dialog = new EntryDialog(250, 150, "Macro name:", name =>
                {
                    if (string.IsNullOrWhiteSpace(name))
                        return;

                    MacroManager manager = Client.Game.GetScene<GameScene>().Macros;
                    List<Macro> macros = manager.GetAllMacros();

                    if (macros.Any(s => s.Name == name))
                        return;

                    NiceButton nb;

                    rightArea.Add(nb = new NiceButton(0, 0, 130, 25, ButtonAction.Activate, name)
                    {
                        ButtonParameter = (int) Buttons.Last + 1 + rightArea.Children.Count
                    });

                    nb.IsSelected = true;

                    _macroControl?.Dispose();

                    _macroControl = new MacroControl(name)
                    {
                        X = 400,
                        Y = 20
                    };

                    Add(_macroControl, PAGE);

                    nb.DragBegin += (sss, eee) =>
                    {
                        if (UIManager.IsDragging || Math.Max(Math.Abs(Mouse.LDroppedOffset.X), Math.Abs(Mouse.LDroppedOffset.Y)) < 5
                            || nb.ScreenCoordinateX > Mouse.LDropPosition.X || nb.ScreenCoordinateX < Mouse.LDropPosition.X - nb.Width
                            || nb.ScreenCoordinateY > Mouse.LDropPosition.Y || nb.ScreenCoordinateY + nb.Height < Mouse.LDropPosition.Y)
                            return;

                        MacroCollectionControl control = _macroControl.FindControls<MacroCollectionControl>().SingleOrDefault();

                        if (control == null)
                            return;

                        UIManager.Gumps.OfType<MacroButtonGump>().FirstOrDefault(s => s._macro == control.Macro)?.Dispose();

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
                })
                {
                    CanCloseWithRightClick = true
                };
                UIManager.Add(dialog);
            };

            Add(addButton, PAGE);

            NiceButton delButton = new NiceButton(190, 52, 130, 20, ButtonAction.Activate, "Delete macro") {IsSelectable = false, ButtonParameter = (int) Buttons.DeleteMacro};

            delButton.MouseUp += (ss, ee) =>
            {
                NiceButton nb = rightArea.FindControls<ScrollAreaItem>()
                                         .SelectMany(s => s.Children.OfType<NiceButton>())
                                         .SingleOrDefault(a => a.IsSelected);

                if (nb != null)
                {
                    QuestionGump dialog = new QuestionGump("Do you want\ndelete it?", b =>
                    {
                        if (!b)
                            return;

                        nb.Parent.Dispose();

                        if (_macroControl != null)
                        {
                            MacroCollectionControl control = _macroControl.FindControls<MacroCollectionControl>().SingleOrDefault();

                            if (control == null)
                                return;

                            UIManager.Gumps.OfType<MacroButtonGump>().FirstOrDefault(s => s._macro == control.Macro)?.Dispose();
                            Client.Game.GetScene<GameScene>().Macros.RemoveMacro(control.Macro);
                        }

                        if (rightArea.Children.OfType<ScrollAreaItem>().All(s => s.IsDisposed)) _macroControl?.Dispose();
                    });
                    UIManager.Add(dialog);
                }
            };

            Add(delButton, PAGE);
            Add(new Line(190, 52 + 25 + 2, 150, 1, Color.Gray.PackedValue), PAGE);
            Add(rightArea, PAGE);
            Add(new Line(191 + 150, 21, 1, 418, Color.Gray.PackedValue), PAGE);

            foreach (Macro macro in Client.Game.GetScene<GameScene>().Macros.GetAllMacros())
            {
                NiceButton nb;

                rightArea.Add(nb = new NiceButton(0, 0, 130, 25, ButtonAction.Activate, macro.Name)
                {
                    ButtonParameter = (int) Buttons.Last + 1 + rightArea.Children.Count
                });

                nb.IsSelected = true;

                nb.DragBegin += (sss, eee) =>
                {
                    if (UIManager.IsDragging || Math.Max(Math.Abs(Mouse.LDroppedOffset.X), Math.Abs(Mouse.LDroppedOffset.Y)) < 5
                        || nb.ScreenCoordinateX > Mouse.LDropPosition.X || nb.ScreenCoordinateX < Mouse.LDropPosition.X - nb.Width
                        || nb.ScreenCoordinateY > Mouse.LDropPosition.Y || nb.ScreenCoordinateY + nb.Height < Mouse.LDropPosition.Y)
                            return;

                    UIManager.Gumps.OfType<MacroButtonGump>().FirstOrDefault(s => s._macro == macro)?.Dispose();

                    MacroButtonGump macroButtonGump = new MacroButtonGump(macro, Mouse.LDropPosition.X, Mouse.LDropPosition.Y);
                    UIManager.Add(macroButtonGump);
                    UIManager.AttemptDragControl(macroButtonGump, new Point(Mouse.Position.X + (macroButtonGump.Width >> 1), Mouse.Position.Y + (macroButtonGump.Height >> 1)), true);
                };

                nb.MouseUp += (sss, eee) =>
                {
                    _macroControl?.Dispose();

                    _macroControl = new MacroControl(macro.Name)
                    {
                        X = 400,
                        Y = 20
                    };

                    Add(_macroControl, PAGE);
                };
            }
        }

        private Checkbox _use_tooltip;
        private HSliderBar _delay_before_display_tooltip, _tooltip_zoom, _tooltip_background_opacity;
        private FontSelector _tooltip_font_selector;
        private ColorBox _tooltip_font_hue;

        private void BuildTooltip()
        {
            const int PAGE = 5;
            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            _use_tooltip = CreateCheckBox(rightArea, "Use tooltip", ProfileManager.Current.UseTooltip, 0, SPACE_Y);
          
            ScrollAreaItem item = new ScrollAreaItem();

            Label text = new Label("Delay before display:", true, HUE_FONT)
            {
                X = 0,
                Y = SPACE_Y + 10
            };
            item.Add(text);
            _delay_before_display_tooltip = new HSliderBar(20, text.Y + text.Height + SPACE_Y, 200, 0, 1000, ProfileManager.Current.TooltipDelayBeforeDisplay, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT);
            item.Add(_delay_before_display_tooltip);
            rightArea.Add(item);

            item = new ScrollAreaItem();
            text = new Label("Tooltip zoom:", true, HUE_FONT)
            {
                X = 0,
                Y = SPACE_Y + 10
            };
            item.Add(text);
            _tooltip_zoom = new HSliderBar(20, text.Y + text.Height + SPACE_Y, 200, 100, 200, ProfileManager.Current.TooltipDisplayZoom, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT);
            item.Add(_tooltip_zoom);
            rightArea.Add(item);

            item = new ScrollAreaItem();
            text = new Label("Tooltip background opacity:", true, HUE_FONT)
            {
                X = 0,
                Y = SPACE_Y + 10
            };
            item.Add(text);
            _tooltip_background_opacity = new HSliderBar(20, text.Y + text.Height + SPACE_Y, 200, 0, 100, ProfileManager.Current.TooltipBackgroundOpacity, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT);
            item.Add(_tooltip_background_opacity);
            rightArea.Add(item);


            _tooltip_font_hue = CreateClickableColorBox(rightArea, 0, SPACE_Y + 20, ProfileManager.Current.TooltipTextHue, "Tooltip font hue", 0, 0);


            item = new ScrollAreaItem();
            text = new Label("Tooltip font:", true, HUE_FONT)
            {
                X = 0,
                Y = SPACE_Y + 10
            };
            item.Add(text);
            _tooltip_font_selector = new FontSelector(7, ProfileManager.Current.TooltipFont, "Tooltip font!")
            {
                X = 20,
                Y = text.Y + text.Height + SPACE_Y
            };
            item.Add(_tooltip_font_selector);
            rightArea.Add(item);


            Add(rightArea, PAGE);
        }

        private void BuildFonts()
        {
            const int PAGE = 6;

            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            ScrollAreaItem item = new ScrollAreaItem();

            _overrideAllFonts = new Checkbox(0x00D2, 0x00D3, "Override game font", FONT, HUE_FONT)
            {
                IsChecked = ProfileManager.Current.OverrideAllFonts
            };

            _overrideAllFontsIsUnicodeCheckbox = new Combobox(_overrideAllFonts.Width + 5, _overrideAllFonts.Y, 100, new[]
            {
                "ASCII", "Unicode"
            }, ProfileManager.Current.OverrideAllFontsIsUnicode ? 1 : 0)
            {
                IsVisible = _overrideAllFonts.IsChecked
            };

            _overrideAllFonts.ValueChanged += (ss, ee) => { _overrideAllFontsIsUnicodeCheckbox.IsVisible = _overrideAllFonts.IsChecked; };

            item.Add(_overrideAllFonts);
            item.Add(_overrideAllFontsIsUnicodeCheckbox);

            rightArea.Add(item);

            _forceUnicodeJournal = CreateCheckBox(rightArea, "Force Unicode in journal", ProfileManager.Current.ForceUnicodeJournal, 0, SPACE_Y);


            Label text = new Label("Speech font:", true, HUE_FONT)
            {
                Y = 20,
            };

            rightArea.Add(text);

            _fontSelectorChat = new FontSelector(20, ProfileManager.Current.ChatFont, "That's ClassicUO!")
                {X = 20};
            rightArea.Add(_fontSelectorChat);

            Add(rightArea, PAGE);
        }

        private void BuildSpeech()
        {
            const int PAGE = 7;
            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            ScrollAreaItem item = new ScrollAreaItem();

            _scaleSpeechDelay = new Checkbox(0x00D2, 0x00D3, "Scale speech delay", FONT, HUE_FONT)
            {
                IsChecked = ProfileManager.Current.ScaleSpeechDelay
            };
            _scaleSpeechDelay.ValueChanged += (sender, e) => { _sliderSpeechDelay.IsVisible = _scaleSpeechDelay.IsChecked; };
            item.Add(_scaleSpeechDelay);
            _sliderSpeechDelay = new HSliderBar(150, 1, 180, 0, 1000, ProfileManager.Current.SpeechDelay, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT);
            item.Add(_sliderSpeechDelay);
            rightArea.Add(item);

            _saveJournalCheckBox = CreateCheckBox(rightArea, "Save Journal to file in game folder", ProfileManager.Current.SaveJournalToFile, 0, SPACE_Y);

            if (!ProfileManager.Current.SaveJournalToFile)
            {
                World.Journal.CloseWriter();
            }

            // [BLOCK] activate chat
            {
                _chatAfterEnter = CreateCheckBox(rightArea, "Active chat when pressing ENTER", ProfileManager.Current.ActivateChatAfterEnter, 0, SPACE_Y);
                _chatAfterEnter.ValueChanged += (sender, e) => { _activeChatArea.IsVisible = _chatAfterEnter.IsChecked; };
                rightArea.Add(_chatAfterEnter);

                _activeChatArea = new ScrollAreaItem();

                _chatAdditionalButtonsCheckbox = new Checkbox(0x00D2, 0x00D3, "Use additional buttons to activate chat: ! ; : / \\ , . [ | -", FONT, HUE_FONT)
                {
                    X = 20,
                    Y = 15,
                    IsChecked = ProfileManager.Current.ActivateChatAdditionalButtons
                };
                _activeChatArea.Add(_chatAdditionalButtonsCheckbox);

                _chatShiftEnterCheckbox = new Checkbox(0x00D2, 0x00D3, "Use `Shift+Enter` to send message without closing chat", FONT, HUE_FONT)
                {
                    X = 20,
                    Y = 35,
                    IsChecked = ProfileManager.Current.ActivateChatShiftEnterSupport
                };
                _activeChatArea.Add(_chatShiftEnterCheckbox);

                _activeChatArea.IsVisible = _chatAfterEnter.IsChecked;

                rightArea.Add(_activeChatArea);
            }

            

            _randomizeColorsButton = new NiceButton(0, 20 + SPACE_Y, 140, 25, ButtonAction.Activate, "Randomize speech hues") { ButtonParameter = (int)Buttons.Disabled };
            _randomizeColorsButton.MouseUp += (sender, e) =>
            {
                if (e.Button != MouseButtonType.Left)
                    return;
                var speechHue = (ushort)RandomHelper.GetValue(2, 1001); //this seems to be the acceptable hue range for chat messages,
                var emoteHue = (ushort)RandomHelper.GetValue(2, 1001); //taken from POL source code.
                var yellHue = (ushort)RandomHelper.GetValue(2, 1001);
                var whisperHue = (ushort)RandomHelper.GetValue(2, 1001);
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

            _speechColorPickerBox = CreateClickableColorBox(rightArea, 0, SPACE_Y, ProfileManager.Current.SpeechHue, "Speech Color", 20, 20 + SPACE_Y);
            _emoteColorPickerBox = CreateClickableColorBox(rightArea, 0, SPACE_Y, ProfileManager.Current.EmoteHue, "Emote Color", 20, SPACE_Y);
            _yellColorPickerBox = CreateClickableColorBox(rightArea, 0, SPACE_Y, ProfileManager.Current.YellHue, "Yell Color", 20, SPACE_Y);
            _whisperColorPickerBox = CreateClickableColorBox(rightArea, 0, SPACE_Y, ProfileManager.Current.WhisperHue, "Whisper Color", 20, SPACE_Y);

            _partyMessageColorPickerBox = CreateClickableColorBox(rightArea, 0, 20 + SPACE_Y, ProfileManager.Current.PartyMessageHue, "Party Message Color", 20, 0);
            _guildMessageColorPickerBox = CreateClickableColorBox(rightArea, 0, SPACE_Y, ProfileManager.Current.GuildMessageHue, "Guild Message Color", 20, 0);
            _allyMessageColorPickerBox = CreateClickableColorBox(rightArea, 0, SPACE_Y, ProfileManager.Current.AllyMessageHue, "Alliance Message Color", 20, 0);
            _chatMessageColorPickerBox = CreateClickableColorBox(rightArea, 0, SPACE_Y, ProfileManager.Current.ChatMessageHue, "Chat Message Color", 20, 0);

            _sliderSpeechDelay.IsVisible = _scaleSpeechDelay.IsChecked;

            Add(rightArea, PAGE);
        }

        private void BuildCombat()
        {
            const int PAGE = 8;

            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            _queryBeforAttackCheckbox = CreateCheckBox(rightArea, "Query before attack", ProfileManager.Current.EnabledCriminalActionQuery, 0, SPACE_Y);
            _queryBeforeBeneficialCheckbox = CreateCheckBox(rightArea, "Query before beneficial criminal action", ProfileManager.Current.EnabledBeneficialCriminalActionQuery, 0, SPACE_Y);
            _queryBeforeBeneficialCheckbox.SetTooltip("Query before performing beneficial acts on Murderers, Criminals, Grays (Monsters/Animals)");
            _spellFormatCheckbox = CreateCheckBox(rightArea, "Enable Overhead Spell Format", ProfileManager.Current.EnabledSpellFormat, 0, SPACE_Y);
            _spellColoringCheckbox = CreateCheckBox(rightArea, "Enable Overhead Spell Hue", ProfileManager.Current.EnabledSpellHue, 0, SPACE_Y);
            _castSpellsByOneClick = CreateCheckBox(rightArea, "Cast spells by one click", ProfileManager.Current.CastSpellsByOneClick, 0, SPACE_Y);
            _buffBarTime = CreateCheckBox(rightArea, "Show buff duration", ProfileManager.Current.BuffBarTime, 0, SPACE_Y);

            _innocentColorPickerBox = CreateClickableColorBox(rightArea, 0, 20 + SPACE_Y, ProfileManager.Current.InnocentHue, "Innocent Color", 20, 20 + SPACE_Y);
            _friendColorPickerBox = CreateClickableColorBox(rightArea, 0, SPACE_Y, ProfileManager.Current.FriendHue, "Friend Color", 20, SPACE_Y);
            _crimialColorPickerBox = CreateClickableColorBox(rightArea, 0, SPACE_Y, ProfileManager.Current.CriminalHue, "Criminal Color", 20, SPACE_Y);
            _genericColorPickerBox = CreateClickableColorBox(rightArea, 0, SPACE_Y, ProfileManager.Current.AnimalHue, "Animal Color", 20, SPACE_Y);
            _murdererColorPickerBox = CreateClickableColorBox(rightArea, 0, SPACE_Y, ProfileManager.Current.MurdererHue, "Murderer Color", 20, SPACE_Y);
            _enemyColorPickerBox = CreateClickableColorBox(rightArea, 0, SPACE_Y, ProfileManager.Current.EnemyHue, "Enemy Color", 20, SPACE_Y);

            _beneficColorPickerBox = CreateClickableColorBox(rightArea, 0, 20 + SPACE_Y, ProfileManager.Current.BeneficHue, "Benefic Spell Hue", 20, 20 + SPACE_Y);
            _harmfulColorPickerBox = CreateClickableColorBox(rightArea, 0, SPACE_Y, ProfileManager.Current.HarmfulHue, "Harmful Spell Hue", 20, SPACE_Y);
            _neutralColorPickerBox = CreateClickableColorBox(rightArea, 0, SPACE_Y, ProfileManager.Current.NeutralHue, "Neutral Spell Hue", 20, SPACE_Y);

            ScrollAreaItem it = new ScrollAreaItem();

            _spellFormatBox = CreateInputField(it, new TextBox(FONT, 30, 200, 200)
            {
                Text = ProfileManager.Current.SpellDisplayFormat,
                X = 0,
                Y = 20,
                Width = 200,
                Height = 30
            }, " Spell Overhead format: ({power} for powerword - {spell} for spell name)", rightArea.Width - 20);

            rightArea.Add(it);

            Add(rightArea, PAGE);
        }

        private void BuildCounters()
        {
            const int PAGE = 9;
            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            _enableCounters = CreateCheckBox(rightArea, "Enable Counters", ProfileManager.Current.CounterBarEnabled, 0, SPACE_Y);
            _highlightOnUse = CreateCheckBox(rightArea, "Highlight On Use", ProfileManager.Current.CounterBarHighlightOnUse, 0, SPACE_Y);
            _enableAbbreviatedAmount = CreateCheckBox(rightArea, "Enable abbreviated amount values when amount is or exceeds", ProfileManager.Current.CounterBarDisplayAbbreviatedAmount, 0, SPACE_Y);

            ScrollAreaItem item = new ScrollAreaItem();

            _abbreviatedAmount = CreateInputField(item, new TextBox(FONT, -1, 80, 80)
            {
                X = _enableAbbreviatedAmount.X + 30,
                Y = 10,
                Width = 50,
                Height = 30,
                NumericOnly = true,
                Text = ProfileManager.Current.CounterBarAbbreviatedAmount.ToString()
            });

            rightArea.Add(item);

            _highlightOnAmount = CreateCheckBox(rightArea, "Highlight red when amount is below", ProfileManager.Current.CounterBarHighlightOnAmount, 0, SPACE_Y);

            item = new ScrollAreaItem();

            _highlightAmount = CreateInputField(item, new TextBox(FONT, 2, 80, 80)
            {
                X = _highlightOnAmount.X + 30,
                Y = 10,
                Width = 50,
                Height = 30,
                NumericOnly = true,
                Text = ProfileManager.Current.CounterBarHighlightAmount.ToString()
            });

            rightArea.Add(item);

            item = new ScrollAreaItem();

            Label text = new Label("Counter Layout:", true, HUE_FONT)
            {
                Y = _highlightOnUse.Bounds.Bottom + 5
            };
            item.Add(text);
            //_counterLayout = new Combobox(text.Bounds.Right + 10, _highlightOnUse.Bounds.Bottom + 5, 150, new[] { "Horizontal", "Vertical" }, ProfileManager.Current.CounterBarIsVertical ? 1 : 0);
            //item.Add(_counterLayout);
            rightArea.Add(item);


            item = new ScrollAreaItem();

            text = new Label("Cell size:", true, HUE_FONT)
            {
                X = 10,
                Y = 10
            };
            item.Add(text);

            _cellSize = new HSliderBar(text.X + text.Width + 10, text.Y + 5, 80, 30, 80, ProfileManager.Current.CounterBarCellSize, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT);
            item.Add(_cellSize);
            rightArea.Add(item);

            item = new ScrollAreaItem();

            _rows = CreateInputField(item, new TextBox(FONT, 5, 80, 80)
            {
                X = 20,
                Y = _cellSize.Y + _cellSize.Height + 25,
                Width = 50,
                Height = 30,
                NumericOnly = true,
                Text = ProfileManager.Current.CounterBarRows.ToString()
            }, "Rows:");

            _columns = CreateInputField(item, new TextBox(FONT, 5, 80, 80)
            {
                X = _rows.X + _rows.Width + 30,
                Y = _cellSize.Y + _cellSize.Height + 25,
                Width = 50,
                Height = 30,
                NumericOnly = true,
                Text = ProfileManager.Current.CounterBarColumns.ToString()
            }, "Columns:");

            rightArea.Add(item);

            Add(rightArea, PAGE);
        }

        private void BuildExperimental()
        {
            const int PAGE = 12;
            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            

            // [BLOCK] disable hotkeys
            {
                _disableDefaultHotkeys = CreateCheckBox(rightArea, "Disable default UO hotkeys", ProfileManager.Current.DisableDefaultHotkeys, 0, SPACE_Y);
                _disableDefaultHotkeys.ValueChanged += (sender, e) => { _defaultHotkeysArea.IsVisible = _disableDefaultHotkeys.IsChecked; };

                rightArea.Add(_disableDefaultHotkeys);

                _defaultHotkeysArea = new ScrollAreaItem();

                _disableArrowBtn = new Checkbox(0x00D2, 0x00D3, "Disable arrows & numlock arrows (player moving)", FONT, HUE_FONT)
                {
                    X = 20,
                    Y = 5,
                    IsChecked = ProfileManager.Current.DisableArrowBtn
                };
                _defaultHotkeysArea.Add(_disableArrowBtn);

                _disableTabBtn = new Checkbox(0x00D2, 0x00D3, "Disable TAB (toggle warmode)", FONT, HUE_FONT)
                {
                    X = 20,
                    Y = 25,
                    IsChecked = ProfileManager.Current.DisableTabBtn
                };
                _defaultHotkeysArea.Add(_disableTabBtn);

                _disableCtrlQWBtn = new Checkbox(0x00D2, 0x00D3, "Disable Ctrl + Q/W (messageHistory)", FONT, HUE_FONT)
                {
                    X = 20,
                    Y = 45,
                    IsChecked = ProfileManager.Current.DisableCtrlQWBtn
                };

                _disableAutoMove = new Checkbox(0x00D2, 0x00D3, "Disable Right+Left Click Automove", FONT, HUE_FONT)
                {
                    X = 20,
                    Y = 45,
                    IsChecked = ProfileManager.Current.DisableAutoMove
                };
                _defaultHotkeysArea.Add(_disableAutoMove);

                rightArea.Add(_defaultHotkeysArea);

                _defaultHotkeysArea.IsVisible = _disableDefaultHotkeys.IsChecked;
            }

            
            Add(rightArea, PAGE);
        }

        private void BuildInfoBar()
        {
            const int PAGE = 10;

            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            _showInfoBar = CreateCheckBox(rightArea, "Show Info Bar", ProfileManager.Current.ShowInfoBar, 0, SPACE_Y);


            ScrollAreaItem _infoBarHighlightScrollArea = new ScrollAreaItem();

            _infoBarHighlightScrollArea.Add(new Label("Data highlight type:", true, 999));
            _infoBarHighlightType = new Combobox(130, 0, 150, new[] { "Text color", "Colored bars" }, ProfileManager.Current.InfoBarHighlightType);
            _infoBarHighlightScrollArea.Add(_infoBarHighlightType);

            rightArea.Add(_infoBarHighlightScrollArea);


            NiceButton nb = new NiceButton(0, 10, 90, 20, ButtonAction.Activate, "+ Add item", 0, IO.Resources.TEXT_ALIGN_TYPE.TS_LEFT)
            {
                ButtonParameter = -1,
                IsSelectable = true,
                IsSelected = true,
            };
            nb.MouseUp += (sender, e) =>
            {
                InfoBarBuilderControl ibbc = new InfoBarBuilderControl(new InfoBarItem("", InfoBarVars.HP, 0x3B9));
                _infoBarBuilderControls.Add(ibbc);
                rightArea.Add(ibbc);
            };
            rightArea.Add(nb);


            ScrollAreaItem _infobarBuilderLabels = new ScrollAreaItem();

            _infobarBuilderLabels.Add(new Label("Label", true, 999) { Y = 15 });
            _infobarBuilderLabels.Add(new Label("Color", true, 999) { X = 150, Y = 15 });
            _infobarBuilderLabels.Add(new Label("Data", true, 999) { X = 200, Y = 15 });

            rightArea.Add(_infobarBuilderLabels);
            rightArea.Add(new Line(0, 0, rightArea.Width, 1, Color.Gray.PackedValue));
            rightArea.Add(new Line(0, 0, rightArea.Width, 5, Color.Black.PackedValue));


            InfoBarManager ibmanager = Client.Game.GetScene<GameScene>().InfoBars;

            List<InfoBarItem> _infoBarItems = ibmanager.GetInfoBars();

            _infoBarBuilderControls = new List<InfoBarBuilderControl>();

            for (int i = 0; i < _infoBarItems.Count; i++)
            {
                InfoBarBuilderControl ibbc = new InfoBarBuilderControl(_infoBarItems[i]);
                _infoBarBuilderControls.Add(ibbc);
                rightArea.Add(ibbc);
            }

            Add(rightArea, PAGE);
        }

        private void BuildContainers()
        {
            const int PAGE = 11;

            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            ScrollAreaItem item = new ScrollAreaItem();

            Label text = new Label("- Container scale:",true, HUE_FONT, font: FONT);
            item.Add(text);

            _containersScale = new HSliderBar(text.X + text.Width + 10, text.Y + 5, 200, Constants.MIN_CONTAINER_SIZE_PERC, Constants.MAX_CONTAINER_SIZE_PERC, ProfileManager.Current.ContainersScale, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT);
            item.Add(_containersScale);

            rightArea.Add(item);

            _containerScaleItems = CreateCheckBox(rightArea, "Scale items inside containers", ProfileManager.Current.ScaleItemsInsideContainers, 0, 20 + SPACE_Y);
            _useLargeContianersGumps = CreateCheckBox(rightArea, "Use large containers gump", ProfileManager.Current.UseLargeContainerGumps, 0, SPACE_Y);
            _useLargeContianersGumps.IsVisible = Client.Version >= ClientVersion.CV_706000;
             _containerDoubleClickToLoot = CreateCheckBox(rightArea, "Double click to loot items inside containers", ProfileManager.Current.DoubleClickToLootInsideContainers, 0, SPACE_Y);
            _relativeDragAnDropItems = CreateCheckBox(rightArea, "Relative drag and drop items in containers", ProfileManager.Current.RelativeDragAndDropItems, 0, SPACE_Y);

            item = new ScrollAreaItem();
            _overrideContainerLocation = new Checkbox(0x00D2, 0x00D3, "Override container gump location", FONT, HUE_FONT, true)
            {
                IsChecked = ProfileManager.Current.OverrideContainerLocation,
                Y = SPACE_Y
            };
            _overrideContainerLocationSetting = new Combobox(_overrideContainerLocation.Width + 20, 0, 200, new[] { "Near container position", "Top right", "Last dragged position", "Remember every container" }, ProfileManager.Current.OverrideContainerLocationSetting);

            item.Add(_overrideContainerLocation);
            item.Add(_overrideContainerLocationSetting);
            rightArea.Add(item);


            item = new ScrollAreaItem();
            NiceButton button = new NiceButton(0, SPACE_Y + 30, 130, 30, ButtonAction.Activate, "Rebuild containers.txt", 0)
            {
                ButtonParameter = -1,
                IsSelectable = true,
                IsSelected = true
            };
            button.MouseUp += (sender, e) =>
            {
                ContainerManager.BuildContainerFile(true);
            };
            item.Add(button);
            rightArea.Add(item);

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
                    _vendorGumpSize.Text = "60";
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
                    _savezoomCheckbox.IsChecked = false;
                    _restorezoomCheckbox.IsChecked = false;
                    _use_old_status_gump.IsChecked = false;
                    _gameWindowWidth.Text = "600";
                    _gameWindowHeight.Text = "480";
                    _gameWindowPositionX.Text = "20";
                    _gameWindowPositionY.Text = "20";
                    _gameWindowLock.IsChecked = false;
                    _gameWindowFullsize.IsChecked = false;
                    _enableDeathScreen.IsChecked = true;
                    _enableBlackWhiteEffect.IsChecked = true;
                    Client.Game.GetScene<GameScene>().Scale = 1;
                    ProfileManager.Current.DefaultScale = 1f;
                    _lightBar.Value = 0;
                    _enableLight.IsChecked = false;
                    _useColoredLights.IsChecked = false;
                    _darkNights.IsChecked = false;
                    _brighlight.Value = 0;
                    _enableShadows.IsChecked = true;
                    _auraType.SelectedIndex = 0;
                    _runMouseInSeparateThread.IsChecked = true;
                    _auraMouse.IsChecked = true;
                    _xBR.IsChecked = true;
                    _hideChatGradient.IsChecked = false;
                    _partyAura.IsChecked = true;
                    _partyAuraColorPickerBox.SetColor(0x0044, HuesLoader.Instance.GetPolygoneColor(12, 0x0044));

                    _windowSizeArea.IsVisible = !_gameWindowFullsize.IsChecked;
                    _zoomSizeArea.IsVisible = _zoomCheckbox.IsChecked;

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
                    _activeChatArea.IsVisible = _chatAfterEnter.IsChecked;
                    _saveJournalCheckBox.IsChecked = false;

                    break;

                case 8: // combat
                    _innocentColorPickerBox.SetColor(0x005A, HuesLoader.Instance.GetPolygoneColor(12, 0x005A));
                    _friendColorPickerBox.SetColor(0x0044, HuesLoader.Instance.GetPolygoneColor(12, 0x0044));
                    _crimialColorPickerBox.SetColor(0x03B2, HuesLoader.Instance.GetPolygoneColor(12, 0x03B2));
                    _genericColorPickerBox.SetColor(0x03B2, HuesLoader.Instance.GetPolygoneColor(12, 0x03B2));
                    _murdererColorPickerBox.SetColor(0x0023, HuesLoader.Instance.GetPolygoneColor(12, 0x0023));
                    _enemyColorPickerBox.SetColor(0x0031, HuesLoader.Instance.GetPolygoneColor(12, 0x0031));
                    _queryBeforAttackCheckbox.IsChecked = true;
                    _queryBeforeBeneficialCheckbox.IsChecked = false;
                    _castSpellsByOneClick.IsChecked = false;
                    _buffBarTime.IsChecked = false;
                    _beneficColorPickerBox.SetColor(0x0059, HuesLoader.Instance.GetPolygoneColor(12, 0x0059));
                    _harmfulColorPickerBox.SetColor(0x0020, HuesLoader.Instance.GetPolygoneColor(12, 0x0020));
                    _neutralColorPickerBox.SetColor(0x03B1, HuesLoader.Instance.GetPolygoneColor(12, 0x03B1));
                    _spellFormatBox.SetText("{power} [{spell}]");
                    _spellColoringCheckbox.IsChecked = false;
                    _spellFormatCheckbox.IsChecked = false;

                    break;

                case 9: // counters
                    _enableCounters.IsChecked = false;
                    _highlightOnUse.IsChecked = false;
                    _enableAbbreviatedAmount.IsChecked = false;
                    _columns.Text = "1";
                    _rows.Text = "1";
                    _cellSize.Value = 40;
                    _highlightOnAmount.IsChecked = false;
                    _highlightAmount.Text = "5";
                    _abbreviatedAmount.Text = "1000";

                    break;

                case 10: // info bar


                    break;

                case 11: // containers
                    _containersScale.Value = 100;
                    _containerScaleItems.IsChecked = false;
                    _useLargeContianersGumps.IsChecked = false;
                    _containerDoubleClickToLoot.IsChecked = false;
                    _relativeDragAnDropItems.IsChecked = false;
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

            if (ProfileManager.Current.DrawRoofs == _drawRoofs.IsChecked)
            {
                ProfileManager.Current.DrawRoofs = !_drawRoofs.IsChecked;
                Client.Game.GetScene<GameScene>()?.UpdateMaxDrawZ(true);
            }

            if (ProfileManager.Current.TopbarGumpIsDisabled != _enableTopbar.IsChecked)
            {
                if (_enableTopbar.IsChecked)
                    UIManager.GetGump<TopBarGump>()?.Dispose();
                else
                    TopBarGump.Create();

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

            ProfileManager.Current.VendorGumpHeight = (int) _vendorGumpSize.Tag;
            ProfileManager.Current.StandardSkillsGump = _useStandardSkillsGump.IsChecked;

            if (_useStandardSkillsGump.IsChecked)
            {
                var newGump = UIManager.GetGump<SkillGumpAdvanced>();

                if (newGump != null)
                {
                    UIManager.Add(new StandardSkillsGump
                                      {X = newGump.X, Y = newGump.Y});
                    newGump.Dispose();
                }
            }
            else
            {
                var standardGump = UIManager.GetGump<StandardSkillsGump>();

                if (standardGump != null)
                {
                    UIManager.Add(new SkillGumpAdvanced
                                      {X = standardGump.X, Y = standardGump.Y});
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
                Client.Game.Scene.Audio.StopMusic();

            if (!ProfileManager.Current.EnableSound)
                Client.Game.Scene.Audio.StopSounds();

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

            Client.Game.GetScene<GameScene>().ScalePos = _sliderZoom.Value;
            ProfileManager.Current.DefaultScale = Client.Game.GetScene<GameScene>().Scale;
            ProfileManager.Current.EnableMousewheelScaleZoom = _zoomCheckbox.IsChecked;
            ProfileManager.Current.RestoreScaleAfterUnpressCtrl = _restorezoomCheckbox.IsChecked;

            if (!CUOEnviroment.IsOutlands && _use_old_status_gump.IsChecked != ProfileManager.Current.UseOldStatusGump)
            {
                var status = StatusGumpBase.GetStatusGump();

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

                    _gameWindowWidth.Text = n.X.ToString();
                    _gameWindowHeight.Text = n.Y.ToString();
                }
            }

            int.TryParse(_gameWindowPositionX.Text, out int gameWindowPositionX);
            int.TryParse(_gameWindowPositionY.Text, out int gameWindowPositionY);

            if (gameWindowPositionX != ProfileManager.Current.GameWindowPosition.X || gameWindowPositionY != ProfileManager.Current.GameWindowPosition.Y)
            {
                if (vp != null)
                    vp.Location = ProfileManager.Current.GameWindowPosition = new Point(gameWindowPositionX, gameWindowPositionY);
            }

            if (ProfileManager.Current.GameWindowLock != _gameWindowLock.IsChecked)
            {
                if (vp != null) vp.CanMove = !_gameWindowLock.IsChecked;
                ProfileManager.Current.GameWindowLock = _gameWindowLock.IsChecked;
            }

            if (_gameWindowFullsize.IsChecked && (gameWindowPositionX != -5 || gameWindowPositionY != -5))
            {
                if (ProfileManager.Current.GameWindowFullSize == _gameWindowFullsize.IsChecked)
                    _gameWindowFullsize.IsChecked = false;
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

                _gameWindowPositionX.Text = loc.X.ToString();
                _gameWindowPositionY.Text = loc.Y.ToString();
                _gameWindowWidth.Text = n.X.ToString();
                _gameWindowHeight.Text = n.Y.ToString();

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

            ProfileManager.Current.Brighlight = _brighlight.Value / 100f;

            ProfileManager.Current.ShadowsEnabled = _enableShadows.IsChecked;
            ProfileManager.Current.AuraUnderFeetType = _auraType.SelectedIndex;
            Client.Game.IsMouseVisible = Settings.GlobalSettings.RunMouseInASeparateThread = _runMouseInSeparateThread.IsChecked;
            ProfileManager.Current.AuraOnMouse = _auraMouse.IsChecked;
            ProfileManager.Current.UseXBR = _xBR.IsChecked;
            ProfileManager.Current.PartyAura = _partyAura.IsChecked;
            ProfileManager.Current.PartyAuraHue = _partyAuraColorPickerBox.Hue;
            ProfileManager.Current.HideChatGradient = _hideChatGradient.IsChecked;

            // fonts
            ProfileManager.Current.ForceUnicodeJournal = _forceUnicodeJournal.IsChecked;
            var _fontValue = _fontSelectorChat.GetSelectedFont();
            ProfileManager.Current.OverrideAllFonts = _overrideAllFonts.IsChecked;
            ProfileManager.Current.OverrideAllFontsIsUnicode = _overrideAllFontsIsUnicodeCheckbox.SelectedIndex == 1;
            if (ProfileManager.Current.ChatFont != _fontValue)
            {
                ProfileManager.Current.ChatFont = _fontValue;
                WorldViewportGump viewport = UIManager.GetGump<WorldViewportGump>();
                viewport?.ReloadChatControl(new SystemChatControl(5, 5, ProfileManager.Current.GameWindowSize.X, ProfileManager.Current.GameWindowSize.Y));
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
            Client.Game.GetScene<GameScene>().Macros.Save();

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

            counterGump?.SetLayout(ProfileManager.Current.CounterBarCellSize,
                                   ProfileManager.Current.CounterBarRows,
                                   ProfileManager.Current.CounterBarColumns);


            if (before != ProfileManager.Current.CounterBarEnabled)
            {
                if (counterGump == null)
                {
                    if (ProfileManager.Current.CounterBarEnabled)
                        UIManager.Add(new CounterBarGump(200, 200, ProfileManager.Current.CounterBarCellSize, ProfileManager.Current.CounterBarRows, ProfileManager.Current.CounterBarColumns));
                }
                else
                    counterGump.IsEnabled = counterGump.IsVisible = ProfileManager.Current.CounterBarEnabled;
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
                    var hbgstandard = UIManager.Gumps.OfType<HealthBarGump>().ToList();

                    foreach (var healthbar in hbgstandard)
                    {
                        UIManager.Add(new HealthBarGumpCustom(healthbar.LocalSerial) {X = healthbar.X, Y = healthbar.Y});
                        healthbar.Dispose();
                    }
                }
                else
                {
                    var hbgcustom = UIManager.Gumps.OfType<HealthBarGumpCustom>().ToList();

                    foreach (var customhealthbar in hbgcustom)
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


            InfoBarManager ibmanager = Client.Game.GetScene<GameScene>().InfoBars;
            ibmanager.Clear();

            for (int i = 0; i < _infoBarBuilderControls.Count; i++)
            {
                if (!_infoBarBuilderControls[i].IsDisposed)
                    ibmanager.AddItem(new InfoBarItem(_infoBarBuilderControls[i].LabelText, _infoBarBuilderControls[i].Var, _infoBarBuilderControls[i].Hue));
            }
            ibmanager.Save();

            InfoBarGump infoBarGump = UIManager.GetGump<InfoBarGump>();

            if (ProfileManager.Current.ShowInfoBar)
            {
                if (infoBarGump == null)
                {
                    UIManager.Add(new InfoBarGump() { X = 300, Y = 300 });
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
                containerScale = ProfileManager.Current.ContainersScale = (byte)_containersScale.Value;
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



            // tooltip
            ProfileManager.Current.UseTooltip = _use_tooltip.IsChecked;
            ProfileManager.Current.TooltipTextHue = _tooltip_font_hue.Hue;
            ProfileManager.Current.TooltipDelayBeforeDisplay = _delay_before_display_tooltip.Value;
            ProfileManager.Current.TooltipBackgroundOpacity = _tooltip_background_opacity.Value;
            ProfileManager.Current.TooltipDisplayZoom = _tooltip_zoom.Value;
            ProfileManager.Current.TooltipFont = _tooltip_font_selector.GetSelectedFont();



            ProfileManager.Current?.Save(UIManager.Gumps.OfType<Gump>().Where(s => s.CanBeSaved).Reverse().ToList());
        }

        internal void UpdateVideo()
        {            
            _gameWindowWidth.Text = ProfileManager.Current.GameWindowSize.X.ToString();
            _gameWindowHeight.Text = ProfileManager.Current.GameWindowSize.Y.ToString();
            _gameWindowPositionX.Text = ProfileManager.Current.GameWindowPosition.X.ToString();
            _gameWindowPositionY.Text = ProfileManager.Current.GameWindowPosition.Y.ToString();
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ResetHueVector();

            batcher.DrawRectangle(Texture2DCache.GetTexture(Color.Gray), x, y, Width, Height, ref _hueVector);

            return base.Draw(batcher, x, y);
        }

        private TextBox CreateInputField(ScrollAreaItem area, TextBox elem, string label = null, int maxWidth = 0)
        {
            if (label != null)
            {
                Label text = new Label(label, true, HUE_FONT, maxWidth)
                {
                    X = elem.X - 10,
                    Y = elem.Y
                };

                elem.Y += text.Height;
                area.Add(text);
            }

            area.Add(new ResizePic(0x0BB8)
            {
                X = elem.X - 5,
                Y = elem.Y - 2,
                Width = elem.Width + 10,
                Height = elem.Height - 7
            });

            area.Add(elem);

            return elem;
        }

        private Checkbox CreateCheckBox(ScrollArea area, string text, bool ischecked, int x, int y)
        {
            Checkbox box = new Checkbox(0x00D2, 0x00D3, text, FONT, HUE_FONT)
            {
                IsChecked = ischecked
            };

            if (x != 0)
            {
                ScrollAreaItem item = new ScrollAreaItem();
                box.X = x;
                box.Y = y;

                item.Add(box);
                area.Add(item);
            }
            else
            {
                box.Y = y;

                area.Add(box);
            }

            return box;
        }

        private ClickableColorBox CreateClickableColorBox(ScrollArea area, int x, int y, ushort hue, string text, int labelX, int labelY)
        {
            ScrollAreaItem item = new ScrollAreaItem();

            uint color = 0xFF7F7F7F;

            if (hue != 0xFFFF)
                color = HuesLoader.Instance.GetPolygoneColor(12, hue);

            ClickableColorBox box = new ClickableColorBox(x, y, 13, 14, hue, color);
            item.Add(box);

            item.Add(new Label(text, true, HUE_FONT)
            {
                X = box.X + box.Width + 5,
                Y = y,
            });
            area.Add(item);

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
                        Add(_buttons[i] = new RadioButton(0, 0x00D0, 0x00D1, markup, i, HUE_FONT)
                        {
                            Y = y,
                            Tag = i,
                            IsChecked = current_font_index == i
                        });

                        y += 25;
                    }
                }
            }

            public byte GetSelectedFont()
            {
                for (byte i = 0; i < _buttons.Length; i++)
                {
                    RadioButton b = _buttons[i];

                    if (b != null && b.IsChecked) return i;
                }

                return 0xFF;
            }

            public void SetSelectedFont(int index)
            {
                if (index >= 0 && index < _buttons.Length && _buttons[index] != null)
                    _buttons[index].IsChecked = true;
            }
        }
    }
}
