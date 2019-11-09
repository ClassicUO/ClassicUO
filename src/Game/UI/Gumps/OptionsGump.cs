#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering
//	new technologies.
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
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Renderer;

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

        private static UOTexture _logoTexture2D;
        private ScrollAreaItem _activeChatArea;
        private Combobox _autoOpenCorpseOptions;
        private TextBox _autoOpenCorpseRange;
        private Checkbox _buffBarTime,_castSpellsByOneClick, _queryBeforAttackCheckbox, _spellColoringCheckbox, _spellFormatCheckbox;
        private HSliderBar _cellSize;

        // video
        private Checkbox _debugControls, _enableDeathScreen, _enableBlackWhiteEffect, _altLights, _enableLight, _enableShadows, _auraMouse, _xBR, _runMouseInSeparateThread, _useColoredLights, _darkNights, _partyAura;
        private ScrollAreaItem _defaultHotkeysArea, _autoOpenCorpseArea, _dragSelectArea;
        private Combobox _dragSelectModifierKey;
        private HSliderBar _brighlight;

        //counters
        private Checkbox _enableCounters, _highlightOnUse, _highlightOnAmount, _enableAbbreviatedAmount;
        private Checkbox _enableDragSelect, _dragSelectHumanoidsOnly;
        private TextBox _rows, _columns, _highlightAmount, _abbreviatedAmount;

        //experimental
        private Checkbox _enableSelectionArea, _debugGumpIsDisabled, _restoreLastGameSize, _autoOpenDoors, _autoOpenCorpse, _disableTabBtn, _disableCtrlQWBtn, _disableDefaultHotkeys, _disableArrowBtn, _overrideContainerLocation, _smoothDoors, _showTargetRangeIndicator, _customBars, _customBarsBBG;
        private Combobox _overrideContainerLocationSetting;

        // sounds
        private Checkbox _enableSounds, _enableMusic, _footStepsSound, _combatMusic, _musicInBackground, _loginMusic;

        // fonts
        private FontSelector _fontSelectorChat;
        private TextBox _gameWindowHeight;
        private Checkbox _overrideAllFonts;
        private Combobox _overrideAllFontsIsUnicodeCheckbox;

        private Checkbox _gameWindowLock, _gameWindowFullsize;
        // GameWindowPosition
        private TextBox _gameWindowPositionX;
        private TextBox _gameWindowPositionY;

        // GameWindowSize
        private TextBox _gameWindowWidth;
        private Combobox _gridLoot;
        private Checkbox _highlightObjects, /*_smoothMovements,*/ _enablePathfind, _useShiftPathfind, _alwaysRun, _alwaysRunUnlessHidden, _showHpMobile, _highlightByState, _drawRoofs, _treeToStumps, _hideVegetation, _noColorOutOfRangeObjects, _useCircleOfTransparency, _enableTopbar, _holdDownKeyTab, _holdDownKeyAlt, _chatAfterEnter, _chatAdditionalButtonsCheckbox, _chatShiftEnterCheckbox, _enableCaveBorder;
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
        private Combobox _shardType, _auraType;

        // network
        private Checkbox _showNetStats;

        // general
        private HSliderBar _sliderFPS, _sliderFPSLogin, _circleOfTranspRadius;
        private HSliderBar _sliderSpeechDelay;
        private HSliderBar _soundsVolume, _musicVolume, _loginMusicVolume;
        private ColorBox _speechColorPickerBox, _emoteColorPickerBox, _yellColorPickerBox, _whisperColorPickerBox, _partyMessageColorPickerBox, _guildMessageColorPickerBox, _allyMessageColorPickerBox, _partyAuraColorPickerBox;
        private ColorBox _poisonColorPickerBox, _paralyzedColorPickerBox, _invulnerableColorPickerBox;
        private TextBox _spellFormatBox;
        private Checkbox _useStandardSkillsGump, _showMobileNameIncoming, _showCorpseNameIncoming;
        private Checkbox _holdShiftForContext, _holdShiftToSplitStack, _reduceFPSWhenInactive, _sallosEasyGrab, _partyInviteGump, _objectsFading;

        //VendorGump Size Option
        private ArrowNumbersTextBox _vendorGumpSize;

        private ScrollAreaItem _windowSizeArea;
        private ScrollAreaItem _zoomSizeArea;


        // containers
        private HSliderBar _containersScale;
        private Checkbox _containerScaleItems, _containerDoubleClickToLoot;

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

            Add(new NiceButton(10, 10, 140, 25, ButtonAction.SwitchPage, "General") {IsSelected = true, ButtonParameter = 1});
            Add(new NiceButton(10, 10 + 30 * 1, 140, 25, ButtonAction.SwitchPage, "Sound") {ButtonParameter = 2});
            Add(new NiceButton(10, 10 + 30 * 2, 140, 25, ButtonAction.SwitchPage, "Video") {ButtonParameter = 3});
            Add(new NiceButton(10, 10 + 30 * 3, 140, 25, ButtonAction.SwitchPage, "Macros") {ButtonParameter = 4});
            //Add(new NiceButton(10, 10 + 30 * 4, 140, 25, ButtonAction.SwitchPage, "Tooltip") {ButtonParameter = 5});
            Add(new NiceButton(10, 10 + 30 * 4, 140, 25, ButtonAction.SwitchPage, "Fonts") {ButtonParameter = 6});
            Add(new NiceButton(10, 10 + 30 * 5, 140, 25, ButtonAction.SwitchPage, "Speech") {ButtonParameter = 7});
            Add(new NiceButton(10, 10 + 30 * 6, 140, 25, ButtonAction.SwitchPage, "Combat / Spells") {ButtonParameter = 8});
            Add(new NiceButton(10, 10 + 30 * 7, 140, 25, ButtonAction.SwitchPage, "Counters") {ButtonParameter = 9});
            Add(new NiceButton(10, 10 + 30 * 8, 140, 25, ButtonAction.SwitchPage, "Experimental") {ButtonParameter = 10});
            Add(new NiceButton(10, 10 + 30 * 9, 140, 25, ButtonAction.SwitchPage, "Network") {ButtonParameter = 11});
            Add(new NiceButton(10, 10 + 30 * 10, 140, 25, ButtonAction.SwitchPage, "Info Bar") { ButtonParameter = 12 });
            Add(new NiceButton(10, 10 + 30 * 11, 140, 25, ButtonAction.SwitchPage, "Containers") { ButtonParameter = 13 });


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

            BuildGeneral();
            BuildSounds();
            BuildVideo();
            BuildCommands();
            BuildFonts();
            BuildSpeech();
            BuildCombat();
            BuildTooltip();
            BuildCounters();
            BuildExperimental();
            BuildNetwork();
            BuildInfoBar();
            BuildContainers();

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
            _sliderFPS = new HSliderBar(text.X + 90, 5, 250, Constants.MIN_FPS, Constants.MAX_FPS, ProfileManager.Current.MaxFPS, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT);
            fpsItem.Add(_sliderFPS);
            rightArea.Add(fpsItem);

            fpsItem = new ScrollAreaItem();
            text = new Label("- Login FPS:", true, HUE_FONT);
            fpsItem.Add(text);
            _sliderFPSLogin = new HSliderBar(text.X + 90, 5, 250, Constants.MIN_FPS, Constants.MAX_FPS, Settings.GlobalSettings.MaxLoginFPS, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT);
            fpsItem.Add(_sliderFPSLogin);
            rightArea.Add(fpsItem);


            _reduceFPSWhenInactive = CreateCheckBox(rightArea, "Reduce FPS when game is inactive", ProfileManager.Current.ReduceFPSWhenInactive, 0, 5);

            _highlightObjects = CreateCheckBox(rightArea, "Highlight game objects", ProfileManager.Current.HighlightGameObjects, 0, 20);
            _enablePathfind = CreateCheckBox(rightArea, "Enable pathfinding", ProfileManager.Current.EnablePathfind, 0, 0);
            _useShiftPathfind = CreateCheckBox(rightArea, "Use SHIFT for pathfinding", ProfileManager.Current.UseShiftToPathfind, 0, 0);

            ScrollAreaItem alwaysRunItem = new ScrollAreaItem();
            _alwaysRun = new Checkbox(0x00D2, 0x00D3, "Always run", FONT, HUE_FONT)
            {
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

            _enableTopbar = CreateCheckBox(rightArea, "Disable the Menu Bar", ProfileManager.Current.TopbarGumpIsDisabled, 0, 0);
            _holdDownKeyTab = CreateCheckBox(rightArea, "Hold TAB key for combat", ProfileManager.Current.HoldDownKeyTab, 0, 0);
            _holdDownKeyAlt = CreateCheckBox(rightArea, "Hold ALT key + right click to close Anchored gumps", ProfileManager.Current.HoldDownKeyAltToCloseAnchored, 0, 0);
            _holdShiftForContext = CreateCheckBox(rightArea, "Hold Shift for Context Menus", ProfileManager.Current.HoldShiftForContext, 0, 0);
            _holdShiftToSplitStack = CreateCheckBox(rightArea, "Hold Shift to split stack of items", ProfileManager.Current.HoldShiftToSplitStack, 0, 0);
            _highlightByState = CreateCheckBox(rightArea, "Highlight by state (poisoned, yellow hits, paralyzed)", ProfileManager.Current.HighlightMobilesByFlags, 0, 0);
            _poisonColorPickerBox = CreateClickableColorBox(rightArea, 20, 5, ProfileManager.Current.PoisonHue, "Poisoned Color", 40, 5);
            _paralyzedColorPickerBox = CreateClickableColorBox(rightArea, 20, 0, ProfileManager.Current.ParalyzedHue, "Paralyzed Color", 40, 0);
            _invulnerableColorPickerBox = CreateClickableColorBox(rightArea, 20, 0, ProfileManager.Current.InvulnerableHue, "Invulnerable Color", 40, 0);
            _noColorOutOfRangeObjects = CreateCheckBox(rightArea, "No color for object out of range", ProfileManager.Current.NoColorObjectsOutOfRange, 0, 5);
            _objectsFading = CreateCheckBox(rightArea, "Objects fading", ProfileManager.Current.UseObjectsFading, 0, 0);
            _useStandardSkillsGump = CreateCheckBox(rightArea, "Use standard skills gump", ProfileManager.Current.StandardSkillsGump, 0, 0);
            _showMobileNameIncoming = CreateCheckBox(rightArea, "Show incoming new mobiles", ProfileManager.Current.ShowNewMobileNameIncoming, 0, 0);
            _showCorpseNameIncoming = CreateCheckBox(rightArea, "Show incoming new corpses", ProfileManager.Current.ShowNewCorpseNameIncoming, 0, 0);
            _sallosEasyGrab = CreateCheckBox(rightArea, "Sallos easy grab", ProfileManager.Current.SallosEasyGrab, 0, 0);
            _partyInviteGump = CreateCheckBox(rightArea, "Show gump for party invites", ProfileManager.Current.PartyInviteGump, 0, 0);

            fpsItem = new ScrollAreaItem();

            text = new Label("Grid Loot", true, HUE_FONT)
            {
                Y = _showCorpseNameIncoming.Bounds.Bottom + 10
            };
            _gridLoot = new Combobox(text.X + text.Width + 10, text.Y, 200, new[] {"None", "Grid loot only", "Both"}, ProfileManager.Current.GridLootType);

            fpsItem.Add(text);
            fpsItem.Add(_gridLoot);

            rightArea.Add(fpsItem);

            ScrollAreaItem item = new ScrollAreaItem();

            _useCircleOfTransparency = new Checkbox(0x00D2, 0x00D3, "Enable circle of transparency", FONT, HUE_FONT)
            {
                Y = 20,
                IsChecked = ProfileManager.Current.UseCircleOfTransparency
            };
            _useCircleOfTransparency.ValueChanged += (sender, e) => { _circleOfTranspRadius.IsVisible = _useCircleOfTransparency.IsChecked; };
            item.Add(_useCircleOfTransparency);
            _circleOfTranspRadius = new HSliderBar(210, _useCircleOfTransparency.Y + 5, 200, Constants.MIN_CIRCLE_OF_TRANSPARENCY_RADIUS, Constants.MAX_CIRCLE_OF_TRANSPARENCY_RADIUS, ProfileManager.Current.CircleOfTransparencyRadius, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT);
            item.Add(_circleOfTranspRadius);
            rightArea.Add(item);


            _drawRoofs = CreateCheckBox(rightArea, "Hide roof tiles", !ProfileManager.Current.DrawRoofs, 0, 15);
            _treeToStumps = CreateCheckBox(rightArea, "Tree to stumps", ProfileManager.Current.TreeToStumps, 0, 0);
            _hideVegetation = CreateCheckBox(rightArea, "Hide vegetation", ProfileManager.Current.HideVegetation, 0, 0);
            _enableCaveBorder = CreateCheckBox(rightArea, "Mark cave tiles", ProfileManager.Current.EnableCaveBorder, 0, 0);


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
                IsChecked = ProfileManager.Current.EnableSound
            };
            _enableSounds.ValueChanged += (sender, e) => { _soundsVolume.IsVisible = _enableSounds.IsChecked; };
            item.Add(_enableSounds);
            _soundsVolume = new HSliderBar(90, 0, 180, 0, 100, ProfileManager.Current.SoundVolume, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT);
            item.Add(_soundsVolume);
            rightArea.Add(item);


            item = new ScrollAreaItem();

            _enableMusic = new Checkbox(0x00D2, 0x00D3, "Music", FONT, HUE_FONT)
            {
                IsChecked = ProfileManager.Current.EnableMusic
            };
            _enableMusic.ValueChanged += (sender, e) => { _musicVolume.IsVisible = _enableMusic.IsChecked; };
            item.Add(_enableMusic);
            _musicVolume = new HSliderBar(90, 0, 180, 0, 100, ProfileManager.Current.MusicVolume, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT);
            item.Add(_musicVolume);
            rightArea.Add(item);


            item = new ScrollAreaItem();

            _loginMusic = new Checkbox(0x00D2, 0x00D3, "Login music", FONT, HUE_FONT)
            {
                IsChecked = Settings.GlobalSettings.LoginMusic
            };
            _loginMusic.ValueChanged += (sender, e) => { _loginMusicVolume.IsVisible = _loginMusic.IsChecked; };
            item.Add(_loginMusic);
            _loginMusicVolume = new HSliderBar(90, 0, 180, 0, 100, Settings.GlobalSettings.LoginMusicVolume, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT);
            item.Add(_loginMusicVolume);
            rightArea.Add(item);


            _footStepsSound = CreateCheckBox(rightArea, "Play Footsteps", ProfileManager.Current.EnableFootstepsSound, 0, 15);
            _combatMusic = CreateCheckBox(rightArea, "Combat music", ProfileManager.Current.EnableCombatMusic, 0, 0);
            _musicInBackground = CreateCheckBox(rightArea, "Reproduce sounds and music when ClassicUO is not focused", ProfileManager.Current.ReproduceSoundsInBackground, 0, 0);


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

            _debugControls = CreateCheckBox(rightArea, "Debugging mode", Settings.GlobalSettings.Debug, 0, 0);

            // [BLOCK] game size
            {
                _gameWindowFullsize = new Checkbox(0x00D2, 0x00D3, "Always use fullsize game window", FONT, HUE_FONT)
                {
                    IsChecked = ProfileManager.Current.GameWindowFullSize
                };
                _gameWindowFullsize.ValueChanged += (sender, e) => { _windowSizeArea.IsVisible = !_gameWindowFullsize.IsChecked; };

                rightArea.Add(_gameWindowFullsize);

                _windowSizeArea = new ScrollAreaItem();

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
                _zoomCheckbox = new Checkbox(0x00D2, 0x00D3, "Enable in game zoom scaling (Ctrl + Scroll)", FONT, HUE_FONT)
                {
                    IsChecked = ProfileManager.Current.EnableScaleZoom
                };
                _zoomCheckbox.ValueChanged += (sender, e) => { _zoomSizeArea.IsVisible = _zoomCheckbox.IsChecked; };

                rightArea.Add(_zoomCheckbox);

                _zoomSizeArea = new ScrollAreaItem();

                _savezoomCheckbox = new Checkbox(0x00D2, 0x00D3, "Save scale after exit", FONT, HUE_FONT)
                {
                    X = 20,
                    Y = 15,
                    IsChecked = ProfileManager.Current.SaveScaleAfterClose
                };
                _zoomSizeArea.Add(_savezoomCheckbox);

                _restorezoomCheckbox = new Checkbox(0x00D2, 0x00D3, "Releasing Ctrl Restores Scale", FONT, HUE_FONT)
                {
                    X = 20,
                    Y = 35,
                    IsChecked = ProfileManager.Current.RestoreScaleAfterUnpressCtrl
                };
                _zoomSizeArea.Add(_restorezoomCheckbox);

                rightArea.Add(_zoomSizeArea);
                _zoomSizeArea.IsVisible = _zoomCheckbox.IsChecked;
            }

            _enableDeathScreen = CreateCheckBox(rightArea, "Enable Death Screen", ProfileManager.Current.EnableDeathScreen, 0, 10);
            _enableBlackWhiteEffect = CreateCheckBox(rightArea, "Black & White mode for dead player", ProfileManager.Current.EnableBlackWhiteEffect, 0, 0);

            ScrollAreaItem item = new ScrollAreaItem();

            text = new Label("- Status gump type:", true, HUE_FONT)
            {
                Y = 30
            };

            item.Add(text);

            _shardType = new Combobox(text.Width + 20, text.Y, 100, new[] {"Modern", "Old", "Outlands"})
            {
                SelectedIndex = Settings.GlobalSettings.ShardType
            };
            item.Add(_shardType);
            rightArea.Add(item);

            item = new ScrollAreaItem();
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
            ScrollAreaItem _normalLightsArea = new ScrollAreaItem();

            _altLights = CreateCheckBox(rightArea, "Alternative lights", ProfileManager.Current.UseAlternativeLights, 0, 0);
            _altLights.ValueChanged += (sender, e) =>
            {
                _normalLightsArea.IsVisible = !_altLights.IsChecked;
            };
            _normalLightsArea.IsVisible = !_altLights.IsChecked;
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

            _normalLightsArea.Add(_enableLight);
            _normalLightsArea.Add(_lightBar);
            _normalLightsArea.Add(_darkNights);
            rightArea.Add(_normalLightsArea);
            rightArea.Add(item);

            _useColoredLights = CreateCheckBox(rightArea, "Use colored lights", ProfileManager.Current.UseColoredLights, 0, 0);

            _enableShadows = new Checkbox(0x00D2, 0x00D3, "Shadows", FONT, HUE_FONT)
            {
                IsChecked = ProfileManager.Current.ShadowsEnabled
            };
            rightArea.Add(_enableShadows);


            item = new ScrollAreaItem();

            text = new Label("- Aura under feet:", true, HUE_FONT)
            {
                Y = 10
            };
            item.Add(text);

            _auraType = new Combobox(text.Width + 20, text.Y, 100, new[] {"None", "Warmode", "Ctrl+Shift", "Always"})
            {
                SelectedIndex = ProfileManager.Current.AuraUnderFeetType
            };
            item.Add(_auraType);
            rightArea.Add(item);

            _partyAura = CreateCheckBox(rightArea, "Custom color aura for party members", ProfileManager.Current.PartyAura, 0, 0);
            _partyAuraColorPickerBox = CreateClickableColorBox(rightArea, 20, 5, ProfileManager.Current.PartyAuraHue, "Party Aura Color", 40, 5);
            _runMouseInSeparateThread = CreateCheckBox(rightArea, "Run mouse in a separate thread", Settings.GlobalSettings.RunMouseInASeparateThread, 0, 5);
            _auraMouse = CreateCheckBox(rightArea, "Aura on mouse target", ProfileManager.Current.AuraOnMouse, 0, 0);
            _xBR = CreateCheckBox(rightArea, "Use xBR effect [BETA]", ProfileManager.Current.UseXBR, 0, 0);

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

                    MacroManager manager = CUOEnviroment.Client.GetScene<GameScene>().Macros;
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

                        MacroButtonGump macroButtonGump = new MacroButtonGump(control.Macro, Mouse.Position.X, Mouse.Position.Y);
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
                            CUOEnviroment.Client.GetScene<GameScene>().Macros.RemoveMacro(control.Macro);
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

            foreach (Macro macro in CUOEnviroment.Client.GetScene<GameScene>().Macros.GetAllMacros())
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

                    MacroButtonGump macroButtonGump = new MacroButtonGump(macro, Mouse.Position.X, Mouse.Position.Y);
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

        private void BuildTooltip()
        {
            const int PAGE = 5;
            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);
            ScrollAreaItem item = new ScrollAreaItem();
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



            Label text = new Label("Speech font:", true, HUE_FONT)
            {
                Y = 20,
            };
            rightArea.Add(text);

            _fontSelectorChat = new FontSelector
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

            _saveJournalCheckBox = CreateCheckBox(rightArea, "Save Journal to file in game folder", false, 0, 0);
            _saveJournalCheckBox.ValueChanged += (o, e) => { World.Journal.CreateWriter(_saveJournalCheckBox.IsChecked); };
            _saveJournalCheckBox.IsChecked = ProfileManager.Current.SaveJournalToFile;

            // [BLOCK] activate chat
            {
                _chatAfterEnter = new Checkbox(0x00D2, 0x00D3, "Press `Enter` to activate chat", FONT, HUE_FONT)
                {
                    Y = 0,
                    IsChecked = ProfileManager.Current.ActivateChatAfterEnter
                };
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

            _speechColorPickerBox = CreateClickableColorBox(rightArea, 0, 20, ProfileManager.Current.SpeechHue, "Speech Color", 20, 20);
            _emoteColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, ProfileManager.Current.EmoteHue, "Emote Color", 20, 0);
            _yellColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, ProfileManager.Current.YellHue, "Yell Color", 20, 0);
            _whisperColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, ProfileManager.Current.WhisperHue, "Whisper Color", 20, 0);
            _partyMessageColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, ProfileManager.Current.PartyMessageHue, "Party Message Color", 20, 0);
            _guildMessageColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, ProfileManager.Current.GuildMessageHue, "Guild Message Color", 20, 0);
            _allyMessageColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, ProfileManager.Current.AllyMessageHue, "Alliance Message Color", 20, 0);

            _sliderSpeechDelay.IsVisible = _scaleSpeechDelay.IsChecked;

            Add(rightArea, PAGE);
        }

        private void BuildCombat()
        {
            const int PAGE = 8;

            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            _queryBeforAttackCheckbox = CreateCheckBox(rightArea, "Query before attack", ProfileManager.Current.EnabledCriminalActionQuery, 0, 0);
            _spellFormatCheckbox = CreateCheckBox(rightArea, "Enable Overhead Spell Format", ProfileManager.Current.EnabledSpellFormat, 0, 0);
            _spellColoringCheckbox = CreateCheckBox(rightArea, "Enable Overhead Spell Hue", ProfileManager.Current.EnabledSpellHue, 0, 0);
            _castSpellsByOneClick = CreateCheckBox(rightArea, "Cast spells by one click", ProfileManager.Current.CastSpellsByOneClick, 0, 0);
            _buffBarTime = CreateCheckBox(rightArea, "Show buff duration", ProfileManager.Current.BuffBarTime, 0, 0);

            _innocentColorPickerBox = CreateClickableColorBox(rightArea, 0, 20, ProfileManager.Current.InnocentHue, "Innocent Color", 20, 20);
            _friendColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, ProfileManager.Current.FriendHue, "Friend Color", 20, 0);
            _crimialColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, ProfileManager.Current.CriminalHue, "Criminal Color", 20, 0);
            _genericColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, ProfileManager.Current.AnimalHue, "Animal Color", 20, 0);
            _murdererColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, ProfileManager.Current.MurdererHue, "Murderer Color", 20, 0);
            _enemyColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, ProfileManager.Current.EnemyHue, "Enemy Color", 20, 0);

            _beneficColorPickerBox = CreateClickableColorBox(rightArea, 0, 20, ProfileManager.Current.BeneficHue, "Benefic Spell Hue", 20, 20);
            _harmfulColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, ProfileManager.Current.HarmfulHue, "Harmful Spell Hue", 20, 0);
            _neutralColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, ProfileManager.Current.NeutralHue, "Neutral Spell Hue", 20, 0);

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

            _enableCounters = CreateCheckBox(rightArea, "Enable Counters", ProfileManager.Current.CounterBarEnabled, 0, 0);
            _highlightOnUse = CreateCheckBox(rightArea, "Highlight On Use", ProfileManager.Current.CounterBarHighlightOnUse, 0, 0);
            _enableAbbreviatedAmount = CreateCheckBox(rightArea, "Enable abbreviated amount values when amount is or exceeds", ProfileManager.Current.CounterBarDisplayAbbreviatedAmount, 0, 0);

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

            _highlightOnAmount = CreateCheckBox(rightArea, "Highlight red when amount is below", ProfileManager.Current.CounterBarHighlightOnAmount, 0, 0);

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
            const int PAGE = 10;
            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            _enableSelectionArea = CreateCheckBox(rightArea, "Enable Text Selection Area", ProfileManager.Current.EnableSelectionArea, 0, 0);

            _debugGumpIsDisabled = CreateCheckBox(rightArea, "Disable Debug Gump", ProfileManager.Current.DebugGumpIsDisabled, 0, 0);
            _restoreLastGameSize = CreateCheckBox(rightArea, "Disable automatic maximize. Restore windows size after re-login", ProfileManager.Current.RestoreLastGameSize, 0, 0);

            _autoOpenDoors = CreateCheckBox(rightArea, "Auto Open Doors", ProfileManager.Current.AutoOpenDoors, 0, 0);
            _smoothDoors = CreateCheckBox(rightArea, "Smooth doors", ProfileManager.Current.SmoothDoors, 20, 5);

            _autoOpenCorpseArea = new ScrollAreaItem();

            _autoOpenCorpse = CreateCheckBox(rightArea, "Auto Open Corpses", ProfileManager.Current.AutoOpenCorpses, 0, 5);
            _autoOpenCorpse.ValueChanged += (sender, e) => { _autoOpenCorpseArea.IsVisible = _autoOpenCorpse.IsChecked; };

            _autoOpenCorpseRange = CreateInputField(_autoOpenCorpseArea, new TextBox(FONT, 2, 80, 80)
            {
                X = 20,
                Y = _cellSize.Y + _cellSize.Height - 15,
                Width = 50,
                Height = 30,
                NumericOnly = true,
                Text = ProfileManager.Current.AutoOpenCorpseRange.ToString()
            }, "Corpse Open Range:");

            /* text = new Label("- Aura under feet:", true, HUE_FONT, 0, FONT)
            {
                Y = 10
            };
            item.Add(text);

            _auraType = new Combobox(text.Width + 20, text.Y, 100, new[] {"None", "Warmode", "Ctrl+Shift", "Always"})
            {
                SelectedIndex = ProfileManager.Current.AuraUnderFeetType
            };*/
            var text = new Label("Corpse Open Options:", true, HUE_FONT)
            {
                Y = _autoOpenCorpseRange.Y + 30,
                X = 10
            };
            _autoOpenCorpseArea.Add(text);

            _autoOpenCorpseOptions = new Combobox(text.Width + 20, text.Y, 150, new[]
            {
                "None", "Not Targeting", "Not Hiding", "Both"
            })
            {
                SelectedIndex = ProfileManager.Current.CorpseOpenOptions
            };
            _autoOpenCorpseArea.Add(_autoOpenCorpseOptions);

            rightArea.Add(_autoOpenCorpseArea);

            // [BLOCK] disable hotkeys
            {
                _disableDefaultHotkeys = new Checkbox(0x00D2, 0x00D3, "Disable default UO hotkeys", FONT, HUE_FONT)
                {
                    Y = 0,
                    IsChecked = ProfileManager.Current.DisableDefaultHotkeys
                };
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
                _defaultHotkeysArea.Add(_disableCtrlQWBtn);

                rightArea.Add(_defaultHotkeysArea);

                _defaultHotkeysArea.IsVisible = _disableDefaultHotkeys.IsChecked;
            }

            _enableDragSelect = CreateCheckBox(rightArea, "Enable drag-select to open health bars", ProfileManager.Current.EnableDragSelect, 0, 0);

            _dragSelectArea = new ScrollAreaItem();

            text = new Label("Drag-select modifier key", true, HUE_FONT)
            {
                X = 20
            };
            _dragSelectArea.Add(text);

            _dragSelectModifierKey = new Combobox(text.Width + 80, text.Y, 100, new[] {"None", "Ctrl", "Shift"})
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


            ScrollAreaItem _containerGumpLocation = new ScrollAreaItem();

            _overrideContainerLocation = new Checkbox(0x00D2, 0x00D3, "Override container gump location", FONT, HUE_FONT, true)
            {
                IsChecked = ProfileManager.Current.OverrideContainerLocation,
            };

            _overrideContainerLocationSetting = new Combobox(_overrideContainerLocation.Width + 20, 0, 200, new[] { "Near container position", "Top right", "Last dragged position" }, ProfileManager.Current.OverrideContainerLocationSetting);

            _containerGumpLocation.Add(_overrideContainerLocation);
            _containerGumpLocation.Add(_overrideContainerLocationSetting);

            rightArea.Add(_containerGumpLocation);

            _showTargetRangeIndicator = new Checkbox(0x00D2, 0x00D3, "Show target range indicator", FONT, HUE_FONT, true)
            {
                IsChecked = ProfileManager.Current.ShowTargetRangeIndicator,
            };

            rightArea.Add(_showTargetRangeIndicator);

            _customBars = CreateCheckBox(rightArea, "Use Custom Health Bars", ProfileManager.Current.CustomBarsToggled, 0, 5);
            _customBarsBBG = CreateCheckBox(rightArea, "Use All Black Backgrounds", ProfileManager.Current.CBBlackBGToggled, 20, 5);

            Add(rightArea, PAGE);

            _autoOpenCorpseArea.IsVisible = _autoOpenCorpse.IsChecked;
            _dragSelectArea.IsVisible = _enableDragSelect.IsChecked;
        }

        private void BuildNetwork()
        {
            const int PAGE = 11;

            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            _showNetStats = CreateCheckBox(rightArea, "Show network stats", ProfileManager.Current.ShowNetworkStats, 0, 0);

            Add(rightArea, PAGE);
        }

        private void BuildInfoBar()
        {
            const int PAGE = 12;

            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            _showInfoBar = CreateCheckBox(rightArea, "Show Info Bar", ProfileManager.Current.ShowInfoBar, 0, 0);


            ScrollAreaItem _infoBarHighlightScrollArea = new ScrollAreaItem();

            _infoBarHighlightScrollArea.Add(new Label("Data highlight type:", true, 999));
            _infoBarHighlightType = new Combobox(130, 0, 150, new[] { "Text color", "Colored bars" }, ProfileManager.Current.InfoBarHighlightType);
            _infoBarHighlightScrollArea.Add(_infoBarHighlightType);

            rightArea.Add(_infoBarHighlightScrollArea);


            NiceButton nb = new NiceButton(0, 10, 90, 20, ButtonAction.Activate, "+ Add item", 0, IO.Resources.TEXT_ALIGN_TYPE.TS_LEFT) { ButtonParameter = 999 };
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


            InfoBarManager ibmanager = CUOEnviroment.Client.GetScene<GameScene>().InfoBars;

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
            const int PAGE = 13;

            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            ScrollAreaItem item = new ScrollAreaItem();

            Label text = new Label("- Container scale:",true, HUE_FONT, font: FONT);
            item.Add(text);

            _containersScale = new HSliderBar(text.X + text.Width + 10, text.Y + 5, 200, Constants.MIN_CONTAINER_SIZE_PERC, Constants.MAX_CONTAINER_SIZE_PERC, ProfileManager.Current.ContainersScale, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT);
            item.Add(_containersScale);

            rightArea.Add(item);

            _containerScaleItems = CreateCheckBox(rightArea, "Scale items inside containers", ProfileManager.Current.ScaleItemsInsideContainers, 0, 20);
            _containerDoubleClickToLoot = CreateCheckBox(rightArea, "Double click to loot items inside containers", ProfileManager.Current.DoubleClickToLootInsideContainers, 0, 0);

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
                    _sliderFPSLogin.Value = 60;
                    _reduceFPSWhenInactive.IsChecked = false;
                    _highlightObjects.IsChecked = true;
                    _enableTopbar.IsChecked = false;
                    _holdDownKeyTab.IsChecked = true;
                    _holdDownKeyAlt.IsChecked = true;
                    _holdShiftForContext.IsChecked = false;
                    _holdShiftToSplitStack.IsChecked = false;
                    _enablePathfind.IsChecked = false;
                    _useShiftPathfind.IsChecked = false;
                    _alwaysRun.IsChecked = false;
                    _alwaysRunUnlessHidden.IsChecked = false;
                    _showHpMobile.IsChecked = false;
                    _hpComboBox.SelectedIndex = 0;
                    _hpComboBoxShowWhen.SelectedIndex = 0;
                    _highlightByState.IsChecked = true;
                    _poisonColorPickerBox.SetColor(0x0044, FileManager.Hues.GetPolygoneColor(12, 0x0044));
                    _paralyzedColorPickerBox.SetColor(0x014C, FileManager.Hues.GetPolygoneColor(12, 0x014C));
                    _invulnerableColorPickerBox.SetColor(0x0030, FileManager.Hues.GetPolygoneColor(12, 0x0030));
                    _drawRoofs.IsChecked = true;
                    _enableCaveBorder.IsChecked = false;
                    _treeToStumps.IsChecked = false;
                    _hideVegetation.IsChecked = false;
                    _noColorOutOfRangeObjects.IsChecked = false;
                    _circleOfTranspRadius.Value = 5;
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
                    _objectsFading.IsChecked = true;

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
                    _debugControls.IsChecked = false;
                    _zoomCheckbox.IsChecked = false;
                    _savezoomCheckbox.IsChecked = false;
                    _restorezoomCheckbox.IsChecked = false;
                    _shardType.SelectedIndex = 0;
                    _gameWindowWidth.Text = "600";
                    _gameWindowHeight.Text = "480";
                    _gameWindowPositionX.Text = "20";
                    _gameWindowPositionY.Text = "20";
                    _gameWindowLock.IsChecked = false;
                    _gameWindowFullsize.IsChecked = false;
                    _enableDeathScreen.IsChecked = true;
                    _enableBlackWhiteEffect.IsChecked = true;
                    CUOEnviroment.Client.GetScene<GameScene>().Scale = 1;
                    ProfileManager.Current.RestoreScaleValue = ProfileManager.Current.ScaleZoom = 1f;
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
                    _partyAura.IsChecked = true;
                    _partyAuraColorPickerBox.SetColor(0x0044, FileManager.Hues.GetPolygoneColor(12, 0x0044));

                    _windowSizeArea.IsVisible = !_gameWindowFullsize.IsChecked;
                    _zoomSizeArea.IsVisible = _zoomCheckbox.IsChecked;

                    break;

                case 4: // commands
                    break;

                case 5: // tooltip
                    break;

                case 6: // fonts
                    _fontSelectorChat.SetSelectedFont(0);
                    _overrideAllFonts.IsChecked = false;
                    _overrideAllFontsIsUnicodeCheckbox.SelectedIndex = 1;
                    break;

                case 7: // speech
                    _scaleSpeechDelay.IsChecked = true;
                    _sliderSpeechDelay.Value = 100;
                    _speechColorPickerBox.SetColor(0x02B2, FileManager.Hues.GetPolygoneColor(12, 0x02B2));
                    _emoteColorPickerBox.SetColor(0x0021, FileManager.Hues.GetPolygoneColor(12, 0x0021));
                    _yellColorPickerBox.SetColor(0x0021, FileManager.Hues.GetPolygoneColor(12, 0x0021));
                    _whisperColorPickerBox.SetColor(0x0033, FileManager.Hues.GetPolygoneColor(12, 0x0033));
                    _partyMessageColorPickerBox.SetColor(0x0044, FileManager.Hues.GetPolygoneColor(12, 0x0044));
                    _guildMessageColorPickerBox.SetColor(0x0044, FileManager.Hues.GetPolygoneColor(12, 0x0044));
                    _allyMessageColorPickerBox.SetColor(0x0057, FileManager.Hues.GetPolygoneColor(12, 0x0057));
                    _chatAfterEnter.IsChecked = false;
                    UIManager.SystemChat.IsActive = !_chatAfterEnter.IsChecked;
                    _chatAdditionalButtonsCheckbox.IsChecked = true;
                    _chatShiftEnterCheckbox.IsChecked = true;
                    _activeChatArea.IsVisible = _chatAfterEnter.IsChecked;
                    _saveJournalCheckBox.IsChecked = false;

                    break;

                case 8: // combat
                    _innocentColorPickerBox.SetColor(0x005A, FileManager.Hues.GetPolygoneColor(12, 0x005A));
                    _friendColorPickerBox.SetColor(0x0044, FileManager.Hues.GetPolygoneColor(12, 0x0044));
                    _crimialColorPickerBox.SetColor(0x03B2, FileManager.Hues.GetPolygoneColor(12, 0x03B2));
                    _genericColorPickerBox.SetColor(0x03B2, FileManager.Hues.GetPolygoneColor(12, 0x03B2));
                    _murdererColorPickerBox.SetColor(0x0023, FileManager.Hues.GetPolygoneColor(12, 0x0023));
                    _enemyColorPickerBox.SetColor(0x0031, FileManager.Hues.GetPolygoneColor(12, 0x0031));
                    _queryBeforAttackCheckbox.IsChecked = true;
                    _castSpellsByOneClick.IsChecked = false;
                    _buffBarTime.IsChecked = false;
                    _beneficColorPickerBox.SetColor(0x0059, FileManager.Hues.GetPolygoneColor(12, 0x0059));
                    _harmfulColorPickerBox.SetColor(0x0020, FileManager.Hues.GetPolygoneColor(12, 0x0020));
                    _neutralColorPickerBox.SetColor(0x03B1, FileManager.Hues.GetPolygoneColor(12, 0x03B1));
                    _spellFormatBox.SetText("{power} [{spell}]");
                    _spellColoringCheckbox.IsChecked = false;
                    _spellFormatCheckbox.IsChecked = false;

                    break;

                case 9:
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

                case 10:
                    _enableSelectionArea.IsChecked = false;
                    _debugGumpIsDisabled.IsChecked = false;
                    _restoreLastGameSize.IsChecked = false;
                    _disableDefaultHotkeys.IsChecked = false;
                    _disableArrowBtn.IsChecked = false;
                    _disableTabBtn.IsChecked = false;
                    _disableCtrlQWBtn.IsChecked = false;
                    _enableDragSelect.IsChecked = false;
                    _overrideContainerLocation.IsChecked = false;
                    _overrideContainerLocationSetting.SelectedIndex = 0;
                    _dragSelectHumanoidsOnly.IsChecked = false;
                    _showTargetRangeIndicator.IsChecked = false;
                    _customBars.IsChecked = false;
                    _customBarsBBG.IsChecked = false;

                    break;

                case 11:
                    _showNetStats.IsChecked = false;

                    break;

                case 12:

                    break;

                case 13:
                    _containersScale.Value = 100;
                    _containerScaleItems.IsChecked = false;
                    _containerDoubleClickToLoot.IsChecked = false;
                    break;
            }
        }

        private void Apply()
        {
            WorldViewportGump vp = UIManager.GetGump<WorldViewportGump>();

            // general
            //ProfileManager.Current.MaxFPS = Engine.FpsLimit = _sliderFPS.Value;
            Settings.GlobalSettings.MaxLoginFPS = _sliderFPSLogin.Value;
            ProfileManager.Current.HighlightGameObjects = _highlightObjects.IsChecked;
            ProfileManager.Current.ReduceFPSWhenInactive = _reduceFPSWhenInactive.IsChecked;
            //ProfileManager.Current.SmoothMovements = _smoothMovements.IsChecked;
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
            ProfileManager.Current.HoldShiftForContext = _holdShiftForContext.IsChecked;
            ProfileManager.Current.HoldShiftToSplitStack = _holdShiftToSplitStack.IsChecked;
            ProfileManager.Current.CloseHealthBarType = _healtbarType.SelectedIndex;

            if (ProfileManager.Current.DrawRoofs == _drawRoofs.IsChecked)
            {
                ProfileManager.Current.DrawRoofs = !_drawRoofs.IsChecked;
                CUOEnviroment.Client.GetScene<GameScene>()?.UpdateMaxDrawZ(true);
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
                ProfileManager.Current.EnableCaveBorder = _enableCaveBorder.IsChecked;
                FileManager.Art.ClearCaveTextures();
            }

            ProfileManager.Current.TreeToStumps = _treeToStumps.IsChecked;
            ProfileManager.Current.FieldsType = _fieldsType.SelectedIndex;
            ProfileManager.Current.HideVegetation = _hideVegetation.IsChecked;
            ProfileManager.Current.NoColorObjectsOutOfRange = _noColorOutOfRangeObjects.IsChecked;
            ProfileManager.Current.UseCircleOfTransparency = _useCircleOfTransparency.IsChecked;
            ProfileManager.Current.CircleOfTransparencyRadius = _circleOfTranspRadius.Value;

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

            CUOEnviroment.Client.Scene.Audio.UpdateCurrentMusicVolume();

            if (!ProfileManager.Current.EnableMusic)
                CUOEnviroment.Client.Scene.Audio.StopMusic();

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

            Settings.GlobalSettings.Debug = _debugControls.IsChecked;

            if (ProfileManager.Current.EnableScaleZoom != _zoomCheckbox.IsChecked)
            {
                if (!_zoomCheckbox.IsChecked)
                    CUOEnviroment.Client.GetScene<GameScene>().Scale = 1;

                ProfileManager.Current.EnableScaleZoom = _zoomCheckbox.IsChecked;
            }

            ProfileManager.Current.SaveScaleAfterClose = _savezoomCheckbox.IsChecked;

            if (_restorezoomCheckbox.IsChecked != ProfileManager.Current.RestoreScaleAfterUnpressCtrl)
            {
                if (_restorezoomCheckbox.IsChecked)
                    ProfileManager.Current.RestoreScaleValue = CUOEnviroment.Client.GetScene<GameScene>().Scale;

                ProfileManager.Current.RestoreScaleAfterUnpressCtrl = _restorezoomCheckbox.IsChecked;
            }

            if (Settings.GlobalSettings.ShardType != _shardType.SelectedIndex)
            {
                var status = StatusGumpBase.GetStatusGump();

                Settings.GlobalSettings.ShardType = _shardType.SelectedIndex;

                if (status != null)
                {
                    status.Dispose();
                    StatusGumpBase.AddStatusGump(status.ScreenCoordinateX, status.ScreenCoordinateY);
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
                        n = vp.ResizeGameWindow(new Point(CUOEnviroment.Client.Window.ClientBounds.Width, CUOEnviroment.Client.Window.ClientBounds.Height));
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
            CUOEnviroment.Client.IsMouseVisible = Settings.GlobalSettings.RunMouseInASeparateThread = _runMouseInSeparateThread.IsChecked;
            ProfileManager.Current.AuraOnMouse = _auraMouse.IsChecked;
            ProfileManager.Current.UseXBR = _xBR.IsChecked;
            ProfileManager.Current.PartyAura = _partyAura.IsChecked;
            ProfileManager.Current.PartyAuraHue = _partyAuraColorPickerBox.Hue;


            // fonts
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
            ProfileManager.Current.CastSpellsByOneClick = _castSpellsByOneClick.IsChecked;
            ProfileManager.Current.BuffBarTime = _buffBarTime.IsChecked;

            ProfileManager.Current.BeneficHue = _beneficColorPickerBox.Hue;
            ProfileManager.Current.HarmfulHue = _harmfulColorPickerBox.Hue;
            ProfileManager.Current.NeutralHue = _neutralColorPickerBox.Hue;
            ProfileManager.Current.EnabledSpellHue = _spellColoringCheckbox.IsChecked;
            ProfileManager.Current.EnabledSpellFormat = _spellFormatCheckbox.IsChecked;
            ProfileManager.Current.SpellDisplayFormat = _spellFormatBox.Text;

            // macros
            ProfileManager.Current.Macros = CUOEnviroment.Client.GetScene<GameScene>().Macros.GetAllMacros().ToArray();

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
            ProfileManager.Current.EnableSelectionArea = _enableSelectionArea.IsChecked;
            ProfileManager.Current.RestoreLastGameSize = _restoreLastGameSize.IsChecked;

            // Reset nested checkboxes if parent checkbox is unchecked
            if (!_disableDefaultHotkeys.IsChecked)
            {
                _disableArrowBtn.IsChecked = false;
                _disableTabBtn.IsChecked = false;
                _disableCtrlQWBtn.IsChecked = false;
            }

            // NOTE: Keep these assignments AFTER the code above that resets nested checkboxes if parent checkbox is unchecked
            ProfileManager.Current.DisableDefaultHotkeys = _disableDefaultHotkeys.IsChecked;
            ProfileManager.Current.DisableArrowBtn = _disableArrowBtn.IsChecked;
            ProfileManager.Current.DisableTabBtn = _disableTabBtn.IsChecked;
            ProfileManager.Current.DisableCtrlQWBtn = _disableCtrlQWBtn.IsChecked;

            if (ProfileManager.Current.DebugGumpIsDisabled != _debugGumpIsDisabled.IsChecked)
            {
                DebugGump debugGump = UIManager.GetGump<DebugGump>();

                if (_debugGumpIsDisabled.IsChecked)
                {
                    if (debugGump != null)
                        debugGump.IsVisible = false;
                }
                else
                {
                    if (debugGump == null)
                    {
                        debugGump = new DebugGump
                        {
                            X = ProfileManager.Current.DebugGumpPosition.X,
                            Y = ProfileManager.Current.DebugGumpPosition.Y
                        };
                        UIManager.Add(debugGump);
                    }
                    else
                    {
                        debugGump.IsVisible = true;
                        debugGump.SetInScreen();
                    }
                }

                ProfileManager.Current.DebugGumpIsDisabled = _debugGumpIsDisabled.IsChecked;
            }

            ProfileManager.Current.AutoOpenDoors = _autoOpenDoors.IsChecked;
            ProfileManager.Current.SmoothDoors = _smoothDoors.IsChecked;
            ProfileManager.Current.AutoOpenCorpses = _autoOpenCorpse.IsChecked;
            ProfileManager.Current.AutoOpenCorpseRange = int.Parse(_autoOpenCorpseRange.Text);
            ProfileManager.Current.CorpseOpenOptions = _autoOpenCorpseOptions.SelectedIndex;

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

            // network
            ProfileManager.Current.ShowNetworkStats = _showNetStats.IsChecked;

            // infobar
            ProfileManager.Current.ShowInfoBar = _showInfoBar.IsChecked;
            ProfileManager.Current.InfoBarHighlightType = _infoBarHighlightType.SelectedIndex;


            InfoBarManager ibmanager = CUOEnviroment.Client.GetScene<GameScene>().InfoBars;
            ibmanager.Clear();

            for (int i = 0; i < _infoBarBuilderControls.Count; i++)
            {
                if (!_infoBarBuilderControls[i].IsDisposed)
                    ibmanager.AddItem(new InfoBarItem(_infoBarBuilderControls[i].LabelText, _infoBarBuilderControls[i].Var, _infoBarBuilderControls[i].Hue));
            }

            ProfileManager.Current.InfoBarItems = ibmanager.GetInfoBars().ToArray();


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
                    resizableGump.ForceUpdate();
                }
            }

            ProfileManager.Current.DoubleClickToLootInsideContainers = _containerDoubleClickToLoot.IsChecked;

            ProfileManager.Current?.Save(UIManager.Gumps.OfType<Gump>().Where(s => s.CanBeSaved).Reverse().ToList());
        }

        internal void UpdateVideo()
        {
            WorldViewportGump gump = UIManager.GetGump<WorldViewportGump>();
            _gameWindowWidth.Text = gump.Width.ToString();
            _gameWindowHeight.Text = gump.Height.ToString();
            _gameWindowPositionX.Text = gump.X.ToString();
            _gameWindowPositionY.Text = gump.Y.ToString();
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ResetHueVector();

            batcher.DrawRectangle(Textures.GetTexture(Color.Gray), x, y, Width, Height, ref _hueVector);

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
                color = FileManager.Hues.GetPolygoneColor(12, hue);

            ClickableColorBox box = new ClickableColorBox(x, y, 13, 14, hue, color);
            item.Add(box);

            item.Add(new Label(text, true, HUE_FONT)
            {
                X = labelX, Y = labelY
            });
            area.Add(item);

            return box;
        }

        private enum Buttons
        {
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
            private readonly RadioButton[] _buttons = new RadioButton[20];

            public FontSelector()
            {
                CanMove = false;
                CanCloseWithRightClick = false;

                int y = 0;

                for (byte i = 0; i < 20; i++)
                {
                    if (FileManager.Fonts.UnicodeFontExists(i))
                    {
                        Add(_buttons[i] = new RadioButton(0, 0x00D0, 0x00D1, "That's ClassicUO!", i, HUE_FONT)
                        {
                            Y = y,
                            Tag = i,
                            IsChecked = ProfileManager.Current.ChatFont == i
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
                _buttons[index].IsChecked = true;
            }
        }
    }
}
