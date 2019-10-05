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

using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        private Checkbox _castSpellsByOneClick, _queryBeforAttackCheckbox, _spellColoringCheckbox, _spellFormatCheckbox;
        private HSliderBar _cellSize;

        // video
        private Checkbox _debugControls, _enableDeathScreen, _enableBlackWhiteEffect, _enableLight, _enableShadows, _auraMouse, _xBR, _runMouseInSeparateThread, _useColoredLights, _darkNights, _partyAura;
        private ScrollAreaItem _defaultHotkeysArea, _autoOpenCorpseArea, _dragSelectArea;
        private Combobox _dragSelectModifierKey;
        private HSliderBar _brighlight;

        //counters
        private Checkbox _enableCounters, _highlightOnUse, _highlightOnAmount, _enableAbbreviatedAmount;
        private Checkbox _enableDragSelect, _dragSelectHumanoidsOnly;
        private TextBox _rows, _columns, _highlightAmount, _abbreviatedAmount;

        //experimental
        private Checkbox _enableSelectionArea, _debugGumpIsDisabled, _restoreLastGameSize, _autoOpenDoors, _autoOpenCorpse, _disableTabBtn, _disableCtrlQWBtn, _disableDefaultHotkeys, _disableArrowBtn, _overrideContainerLocation, _smoothDoors, _showTargetRangeIndicator;
        private Combobox _overrideContainerLocationSetting, _language;

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
        private Checkbox _highlightObjects, /*_smoothMovements,*/ _enablePathfind, _alwaysRun, _showHpMobile, _highlightByState, _drawRoofs, _treeToStumps, _hideVegetation, _noColorOutOfRangeObjects, _useCircleOfTransparency, _enableTopbar, _holdDownKeyTab, _holdDownKeyAlt, _chatAfterEnter, _chatAdditionalButtonsCheckbox, _chatShiftEnterCheckbox, _enableCaveBorder;
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
        private Checkbox _holdShiftForContext, _holdShiftToSplitStack, _reduceFPSWhenInactive, _sallosEasyGrab, _partyInviteGump;

        //VendorGump Size Option
        private ArrowNumbersTextBox _vendorGumpSize;

        private ScrollAreaItem _windowSizeArea;
        private ScrollAreaItem _zoomSizeArea;


        // containers
        private HSliderBar _containersScale;
        private Checkbox _containerScaleItems;

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

            Add(new NiceButton(10, 10, 140, 25, ButtonAction.SwitchPage, FileManager.Language.Dict["UI_Options_MainBtn_General"]) {IsSelected = true, ButtonParameter = 1});
            Add(new NiceButton(10, 10 + 30 * 1, 140, 25, ButtonAction.SwitchPage, FileManager.Language.Dict["UI_Options_MainBtn_Sounds"]) {ButtonParameter = 2});
            Add(new NiceButton(10, 10 + 30 * 2, 140, 25, ButtonAction.SwitchPage, FileManager.Language.Dict["UI_Options_MainBtn_Video"]) {ButtonParameter = 3});
            Add(new NiceButton(10, 10 + 30 * 3, 140, 25, ButtonAction.SwitchPage, FileManager.Language.Dict["UI_Options_MainBtn_Macro"]) {ButtonParameter = 4});
            //Add(new NiceButton(10, 10 + 30 * 4, 140, 25, ButtonAction.SwitchPage, "Tooltip") {ButtonParameter = 5});
            Add(new NiceButton(10, 10 + 30 * 4, 140, 25, ButtonAction.SwitchPage, FileManager.Language.Dict["UI_Options_MainBtn_Fonts"]) {ButtonParameter = 6});
            Add(new NiceButton(10, 10 + 30 * 5, 140, 25, ButtonAction.SwitchPage, FileManager.Language.Dict["UI_Options_MainBtn_Speech"]) {ButtonParameter = 7});
            Add(new NiceButton(10, 10 + 30 * 6, 140, 25, ButtonAction.SwitchPage, FileManager.Language.Dict["UI_Options_MainBtn_Combat/Spells"]) {ButtonParameter = 8});
            Add(new NiceButton(10, 10 + 30 * 7, 140, 25, ButtonAction.SwitchPage, FileManager.Language.Dict["UI_Options_MainBtn_Counters"]) {ButtonParameter = 9});
            Add(new NiceButton(10, 10 + 30 * 8, 140, 25, ButtonAction.SwitchPage, FileManager.Language.Dict["UI_Options_MainBtn_Experimental"]) {ButtonParameter = 10});
            Add(new NiceButton(10, 10 + 30 * 9, 140, 25, ButtonAction.SwitchPage, FileManager.Language.Dict["UI_Options_MainBtn_Network"]) {ButtonParameter = 11});
            Add(new NiceButton(10, 10 + 30 * 10, 140, 25, ButtonAction.SwitchPage, FileManager.Language.Dict["UI_Options_MainBtn_InfoBar"]) { ButtonParameter = 12 });
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
                    Stream stream = typeof(Engine).Assembly.GetManifestResourceStream("ClassicUO.cuologo.png");
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
            Label text = new Label(FileManager.Language.Dict["UI_Options_General_FPS"], true, HUE_FONT);
            fpsItem.Add(text);
            _sliderFPS = new HSliderBar(text.X + 90, 5, 250, Constants.MIN_FPS, Constants.MAX_FPS, Engine.Profile.Current.MaxFPS, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT);
            fpsItem.Add(_sliderFPS);
            rightArea.Add(fpsItem);

            fpsItem = new ScrollAreaItem();
            text = new Label(FileManager.Language.Dict["UI_Options_General_FPSLogin"], true, HUE_FONT);
            fpsItem.Add(text);
            _sliderFPSLogin = new HSliderBar(text.X + 90, 5, 250, Constants.MIN_FPS, Constants.MAX_FPS, Engine.GlobalSettings.MaxLoginFPS, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT);
            fpsItem.Add(_sliderFPSLogin);
            rightArea.Add(fpsItem);


            _reduceFPSWhenInactive = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_General_ReduceFPSWhenInactive"], Engine.Profile.Current.ReduceFPSWhenInactive, 0, 5);

            _highlightObjects = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_General_HighlightGameObjects"], Engine.Profile.Current.HighlightGameObjects, 0, 20);
            _enablePathfind = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_General_EnablePathfind"], Engine.Profile.Current.EnablePathfind, 0, 0);
            _alwaysRun = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_General_AlwaysRun"], Engine.Profile.Current.AlwaysRun, 0, 0);
            _enableTopbar = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_General_TopbarGumpIsDisabled"], Engine.Profile.Current.TopbarGumpIsDisabled, 0, 0);
            _holdDownKeyTab = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_General_HoldDownKeyTab"], Engine.Profile.Current.HoldDownKeyTab, 0, 0);
            _holdDownKeyAlt = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_General_HoldDownKeyAltToCloseAnchored"], Engine.Profile.Current.HoldDownKeyAltToCloseAnchored, 0, 0);
            _holdShiftForContext = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_General_HoldShiftForContext"], Engine.Profile.Current.HoldShiftForContext, 0, 0);
            _holdShiftToSplitStack = CreateCheckBox(rightArea, "Hold Shift to split stack of items", Engine.Profile.Current.HoldShiftToSplitStack, 0, 0);
            _highlightByState = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_General_HighlightMobilesByFlags"], Engine.Profile.Current.HighlightMobilesByFlags, 0, 0);
            _poisonColorPickerBox = CreateClickableColorBox(rightArea, 20, 5, Engine.Profile.Current.PoisonHue, FileManager.Language.Dict["UI_Options_General_PoisonHue"], 40, 5);
            _paralyzedColorPickerBox = CreateClickableColorBox(rightArea, 20, 0, Engine.Profile.Current.ParalyzedHue, FileManager.Language.Dict["UI_Options_General_ParalyzedHue"], 40, 0);
            _invulnerableColorPickerBox = CreateClickableColorBox(rightArea, 20, 0, Engine.Profile.Current.InvulnerableHue, FileManager.Language.Dict["UI_Options_General_InvulnerableHue"], 40, 0);
            _noColorOutOfRangeObjects = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_General_NoColorObjectsOutOfRange"], Engine.Profile.Current.NoColorObjectsOutOfRange, 0, 5);
            _useStandardSkillsGump = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_General_StandardSkillsGump"], Engine.Profile.Current.StandardSkillsGump, 0, 0);
            _showMobileNameIncoming = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_General_ShowNewMobileNameIncoming"], Engine.Profile.Current.ShowNewMobileNameIncoming, 0, 0);
            _showCorpseNameIncoming = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_General_ShowNewCorpseNameIncoming"], Engine.Profile.Current.ShowNewCorpseNameIncoming, 0, 0);
            _sallosEasyGrab = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_General_SallosEasyGrab"], Engine.Profile.Current.SallosEasyGrab, 0, 0);
            _partyInviteGump = CreateCheckBox(rightArea, "Show gump for party invites", Engine.Profile.Current.PartyInviteGump, 0, 0);

            fpsItem = new ScrollAreaItem();

            text = new Label(FileManager.Language.Dict["UI_Options_General_GridLoot"], true, HUE_FONT)
            {
                Y = _showCorpseNameIncoming.Bounds.Bottom + 10
            };
            _gridLoot = new Combobox(text.X + text.Width + 10, text.Y, 200, new[] { FileManager.Language.Dict["UI_Options_General_GridLoot_None"], FileManager.Language.Dict["UI_Options_General_GridLoot_GridLootOnly"], FileManager.Language.Dict["UI_Options_General_GridLoot_Both"] }, Engine.Profile.Current.GridLootType);

            fpsItem.Add(text);
            fpsItem.Add(_gridLoot);

            rightArea.Add(fpsItem);

            ScrollAreaItem item = new ScrollAreaItem();

            _useCircleOfTransparency = new Checkbox(0x00D2, 0x00D3, FileManager.Language.Dict["UI_Options_General_UseCircleOfTransparency"], FONT, HUE_FONT)
            {
                Y = 20,
                IsChecked = Engine.Profile.Current.UseCircleOfTransparency
            };
            _useCircleOfTransparency.ValueChanged += (sender, e) => { _circleOfTranspRadius.IsVisible = _useCircleOfTransparency.IsChecked; };
            item.Add(_useCircleOfTransparency);
            _circleOfTranspRadius = new HSliderBar(210, _useCircleOfTransparency.Y + 5, 50, Constants.MIN_CIRCLE_OF_TRANSPARENCY_RADIUS, Constants.MAX_CIRCLE_OF_TRANSPARENCY_RADIUS, Engine.Profile.Current.CircleOfTransparencyRadius, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT);
            item.Add(_circleOfTranspRadius);
            rightArea.Add(item);


            _drawRoofs = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_General_DrawRoofs"], !Engine.Profile.Current.DrawRoofs, 0, 15);
            _treeToStumps = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_General_TreeToStumps"], Engine.Profile.Current.TreeToStumps, 0, 0);
            _hideVegetation = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_General_HideVegetation"], Engine.Profile.Current.HideVegetation, 0, 0);
            _enableCaveBorder = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_General_EnableCaveBorder"], Engine.Profile.Current.EnableCaveBorder, 0, 0);


            ScrollAreaItem hpAreaItem = new ScrollAreaItem();

            _showHpMobile = new Checkbox(0x00D2, 0x00D3, FileManager.Language.Dict["UI_Options_General_ShowMobilesHP"], FONT, HUE_FONT)
            {
                X = 0, Y = 20, IsChecked = Engine.Profile.Current.ShowMobilesHP
            };

            hpAreaItem.Add(_showHpMobile);

            int mode = Engine.Profile.Current.MobileHPType;

            if (mode < 0 || mode > 2)
                mode = 0;

            _hpComboBox = new Combobox(_showHpMobile.Bounds.Right + 10, 20, 150, new[]
            {
                 FileManager.Language.Dict["UI_Options_General_ShowMobilesHP_Percentage"], FileManager.Language.Dict["UI_Options_General_ShowMobilesHP_Line"], FileManager.Language.Dict["UI_Options_General_ShowMobilesHP_Both"]
            }, mode);
            hpAreaItem.Add(_hpComboBox);

            text = new Label(FileManager.Language.Dict["UI_Options_General_ShowMobilesHP_MobileHPShowWhen"], true, HUE_FONT)
            {
                X = _showHpMobile.Bounds.Right + 170,
                Y = 20
            };
            hpAreaItem.Add(text);

            mode = Engine.Profile.Current.MobileHPShowWhen;

            if (mode != 0 && mode > 2)
                mode = 0;

            _hpComboBoxShowWhen = new Combobox(text.Bounds.Right + 10, 20, 150, new[]
            {
                FileManager.Language.Dict["UI_Options_General_ShowMobilesHP_MobileHPShowWhen_Always"], FileManager.Language.Dict["UI_Options_General_ShowMobilesHP_MobileHPShowWhen_LessThan100%"], FileManager.Language.Dict["UI_Options_General_ShowMobilesHP_MobileHPShowWhen_Smart"]
            }, mode);
            hpAreaItem.Add(_hpComboBoxShowWhen);

            mode = Engine.Profile.Current.CloseHealthBarType;

            if (mode < 0 || mode > 2)
                mode = 0;

            text = new Label(FileManager.Language.Dict["UI_Options_General_CloseHealthbarGumpWhen"], true, HUE_FONT)
            {
                Y = _hpComboBox.Bounds.Bottom + 10
            };
            hpAreaItem.Add(text);

            _healtbarType = new Combobox(text.Bounds.Right + 10, _hpComboBox.Bounds.Bottom + 10, 150, new[]
            {
                FileManager.Language.Dict["UI_Options_General_CloseHealthbarGumpWhen_None"], FileManager.Language.Dict["UI_Options_General_CloseHealthbarGumpWhen_MobileOutOfRange"], FileManager.Language.Dict["UI_Options_General_CloseHealthbarGumpWhen_MobileIsDead"]
            }, mode);
            hpAreaItem.Add(_healtbarType);

            text = new Label(FileManager.Language.Dict["UI_Options_General_FieldsType"], true, HUE_FONT)
            {
                Y = _hpComboBox.Bounds.Bottom + 45
            };
            hpAreaItem.Add(text);

            mode = Engine.Profile.Current.FieldsType;

            if (mode < 0 || mode > 2)
                mode = 0;

            _fieldsType = new Combobox(text.Bounds.Right + 10, _hpComboBox.Bounds.Bottom + 45, 150, new[]
            {
                FileManager.Language.Dict["UI_Options_General_FieldsType_NormalFields"], FileManager.Language.Dict["UI_Options_General_FieldsType_StaticFields"], FileManager.Language.Dict["UI_Options_General_FieldsType_TileFields"]
            }, mode);

            hpAreaItem.Add(_fieldsType);
            rightArea.Add(hpAreaItem);

            _circleOfTranspRadius.IsVisible = _useCircleOfTransparency.IsChecked;

            hpAreaItem = new ScrollAreaItem();
            Control c = new Label(FileManager.Language.Dict["UI_Options_General_ShopGumpSize"], true, HUE_FONT) {Y = 10};
            hpAreaItem.Add(c);
            hpAreaItem.Add(_vendorGumpSize = new ArrowNumbersTextBox(c.Width + 5, 10, 60, 60, 60, 240, FONT, hue: 1) {Text = Engine.Profile.Current.VendorGumpHeight.ToString(), Tag = Engine.Profile.Current.VendorGumpHeight});
            rightArea.Add(hpAreaItem);

            Add(rightArea, PAGE);
        }

        private void BuildSounds()
        {
            const int PAGE = 2;

            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);


            ScrollAreaItem item = new ScrollAreaItem();

            _enableSounds = new Checkbox(0x00D2, 0x00D3, FileManager.Language.Dict["UI_Options_Sounds_Sounds"], FONT, HUE_FONT)
            {
                IsChecked = Engine.Profile.Current.EnableSound
            };
            _enableSounds.ValueChanged += (sender, e) => { _soundsVolume.IsVisible = _enableSounds.IsChecked; };
            item.Add(_enableSounds);
            _soundsVolume = new HSliderBar(90, 0, 180, 0, 100, Engine.Profile.Current.SoundVolume, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT);
            item.Add(_soundsVolume);
            rightArea.Add(item);


            item = new ScrollAreaItem();

            _enableMusic = new Checkbox(0x00D2, 0x00D3, FileManager.Language.Dict["UI_Options_Sounds_Music"], FONT, HUE_FONT)
            {
                IsChecked = Engine.Profile.Current.EnableMusic
            };
            _enableMusic.ValueChanged += (sender, e) => { _musicVolume.IsVisible = _enableMusic.IsChecked; };
            item.Add(_enableMusic);
            _musicVolume = new HSliderBar(90, 0, 180, 0, 100, Engine.Profile.Current.MusicVolume, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT);
            item.Add(_musicVolume);
            rightArea.Add(item);


            item = new ScrollAreaItem();

            _loginMusic = new Checkbox(0x00D2, 0x00D3, FileManager.Language.Dict["UI_Options_Sounds_LoginMusic"], FONT, HUE_FONT)
            {
                IsChecked = Engine.GlobalSettings.LoginMusic
            };
            _loginMusic.ValueChanged += (sender, e) => { _loginMusicVolume.IsVisible = _loginMusic.IsChecked; };
            item.Add(_loginMusic);
            _loginMusicVolume = new HSliderBar(90, 0, 180, 0, 100, Engine.GlobalSettings.LoginMusicVolume, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT);
            item.Add(_loginMusicVolume);
            rightArea.Add(item);


            _footStepsSound = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_Sounds_Footsteps"], Engine.Profile.Current.EnableFootstepsSound, 0, 15);
            _combatMusic = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_Sounds_CombatMusic"], Engine.Profile.Current.EnableCombatMusic, 0, 0);
            _musicInBackground = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_Sounds_ReproduceSoundsInBackground"], Engine.Profile.Current.ReproduceSoundsInBackground, 0, 0);


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

            _debugControls = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_Video_Debug"], Engine.GlobalSettings.Debug, 0, 0);

            // [BLOCK] game size
            {
                _gameWindowFullsize = new Checkbox(0x00D2, 0x00D3, FileManager.Language.Dict["UI_Options_Video_GameWindowFullSize"], FONT, HUE_FONT)
                {
                    IsChecked = Engine.Profile.Current.GameWindowFullSize
                };
                _gameWindowFullsize.ValueChanged += (sender, e) => { _windowSizeArea.IsVisible = !_gameWindowFullsize.IsChecked; };

                rightArea.Add(_gameWindowFullsize);

                _windowSizeArea = new ScrollAreaItem();

                _gameWindowLock = new Checkbox(0x00D2, 0x00D3, FileManager.Language.Dict["UI_Options_Video_GameWindowLock"], FONT, HUE_FONT)
                {
                    X = 20,
                    Y = 15,
                    IsChecked = Engine.Profile.Current.GameWindowLock
                };

                _windowSizeArea.Add(_gameWindowLock);

                text = new Label(FileManager.Language.Dict["UI_Options_Video_GameWindowSize"], true, HUE_FONT)
                {
                    X = 20,
                    Y = 40
                };
                _windowSizeArea.Add(text);

                _gameWindowWidth = CreateInputField(_windowSizeArea, new TextBox(FONT, 5, 80, 80)
                {
                    Text = Engine.Profile.Current.GameWindowSize.X.ToString(),
                    X = 30,
                    Y = 70,
                    Width = 50,
                    Height = 30,
                    UNumericOnly = true
                }, "");

                _gameWindowHeight = CreateInputField(_windowSizeArea, new TextBox(FONT, 5, 80, 80)
                {
                    Text = Engine.Profile.Current.GameWindowSize.Y.ToString(),
                    X = 100,
                    Y = 70,
                    Width = 50,
                    Height = 30,
                    UNumericOnly = true
                });

                text = new Label(FileManager.Language.Dict["UI_Options_Video_GameWindowPosition"], true, HUE_FONT)
                {
                    X = 205,
                    Y = 40
                };
                _windowSizeArea.Add(text);

                _gameWindowPositionX = CreateInputField(_windowSizeArea, new TextBox(FONT, 5, 80, 80)
                {
                    Text = Engine.Profile.Current.GameWindowPosition.X.ToString(),
                    X = 215,
                    Y = 70,
                    Width = 50,
                    Height = 30,
                    NumericOnly = true
                });

                _gameWindowPositionY = CreateInputField(_windowSizeArea, new TextBox(FONT, 5, 80, 80)
                {
                    Text = Engine.Profile.Current.GameWindowPosition.Y.ToString(),
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
                _zoomCheckbox = new Checkbox(0x00D2, 0x00D3, FileManager.Language.Dict["UI_Options_Video_EnableScaleZoom"], FONT, HUE_FONT)
                {
                    IsChecked = Engine.Profile.Current.EnableScaleZoom
                };
                _zoomCheckbox.ValueChanged += (sender, e) => { _zoomSizeArea.IsVisible = _zoomCheckbox.IsChecked; };

                rightArea.Add(_zoomCheckbox);

                _zoomSizeArea = new ScrollAreaItem();

                _savezoomCheckbox = new Checkbox(0x00D2, 0x00D3, FileManager.Language.Dict["UI_Options_Video_SaveScaleAfterClose"], FONT, HUE_FONT)
                {
                    X = 20,
                    Y = 15,
                    IsChecked = Engine.Profile.Current.SaveScaleAfterClose
                };
                _zoomSizeArea.Add(_savezoomCheckbox);

                _restorezoomCheckbox = new Checkbox(0x00D2, 0x00D3, FileManager.Language.Dict["UI_Options_Video_RestoreScaleAfterUnpressCtrl"], FONT, HUE_FONT)
                {
                    X = 20,
                    Y = 35,
                    IsChecked = Engine.Profile.Current.RestoreScaleAfterUnpressCtrl
                };
                _zoomSizeArea.Add(_restorezoomCheckbox);

                rightArea.Add(_zoomSizeArea);
                _zoomSizeArea.IsVisible = _zoomCheckbox.IsChecked;
            }

            _enableDeathScreen = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_Video_EnableDeathScreen"], Engine.Profile.Current.EnableDeathScreen, 0, 10);
            _enableBlackWhiteEffect = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_Video_EnableBlackWhiteEffect"], Engine.Profile.Current.EnableBlackWhiteEffect, 0, 0);

            ScrollAreaItem item = new ScrollAreaItem();

            text = new Label(FileManager.Language.Dict["UI_Options_Video_ShardType"], true, HUE_FONT)
            {
                Y = 30
            };

            item.Add(text);

            _shardType = new Combobox(text.Width + 20, text.Y, 100, new[] { FileManager.Language.Dict["UI_Options_Video_ShardType_Modern"], FileManager.Language.Dict["UI_Options_Video_ShardType_Old"], FileManager.Language.Dict["UI_Options_Video_ShardType_Outlands"] })
            {
                SelectedIndex = Engine.GlobalSettings.ShardType
            };
            item.Add(_shardType);
            rightArea.Add(item);

            item = new ScrollAreaItem();
            item.Y = 30;
            text = new Label(FileManager.Language.Dict["UI_Options_Video_Brighlight"], true, HUE_FONT)
            {
                Y = 30,
                IsVisible = false,
            };
            _brighlight = new HSliderBar(text.Width + 10, text.Y + 5, 250, 0, 100, (int) (Engine.Profile.Current.Brighlight * 100f), HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT);
            _brighlight.IsVisible = false;
            item.Add(text);
            item.Add(_brighlight);
            rightArea.Add(item);

            item = new ScrollAreaItem();
            _enableLight = new Checkbox(0x00D2, 0x00D3, FileManager.Language.Dict["UI_Options_Video_LightLevel"], FONT, HUE_FONT)
            {
                IsChecked = Engine.Profile.Current.UseCustomLightLevel
            };
            _lightBar = new HSliderBar(_enableLight.Width + 10, _enableLight.Y + 5, 250, 0, 0x1E, 0x1E - Engine.Profile.Current.LightLevel, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT);

            item.Add(_enableLight);
            item.Add(_lightBar);
            rightArea.Add(item);

            _useColoredLights = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_Video_UseColoredLights"], Engine.Profile.Current.UseColoredLights, 0, 0);
            _darkNights = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_Video_UseDarkNights"], Engine.Profile.Current.UseDarkNights, 0, 0);

            _enableShadows = new Checkbox(0x00D2, 0x00D3, FileManager.Language.Dict["UI_Options_Video_ShadowsEnabled"], FONT, HUE_FONT)
            {
                IsChecked = Engine.Profile.Current.ShadowsEnabled
            };
            rightArea.Add(_enableShadows);


            item = new ScrollAreaItem();

            text = new Label(FileManager.Language.Dict["UI_Options_Video_AuraUnderFeet"], true, HUE_FONT)
            {
                Y = 10
            };
            item.Add(text);

            _auraType = new Combobox(text.Width + 20, text.Y, 100, new[] { FileManager.Language.Dict["UI_Options_Video_AuraUnderFeet_None"], FileManager.Language.Dict["UI_Options_Video_AuraUnderFeet_Warmode"], FileManager.Language.Dict["UI_Options_Video_AuraUnderFeet_Ctrl+Shift"], FileManager.Language.Dict["UI_Options_Video_AuraUnderFeet_Always"] })
            {
                SelectedIndex = Engine.Profile.Current.AuraUnderFeetType
            };
            item.Add(_auraType);
            rightArea.Add(item);

            _partyAura = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_Video_PartyAura"], Engine.Profile.Current.PartyAura, 0, 0);
            _partyAuraColorPickerBox = CreateClickableColorBox(rightArea, 20, 5, Engine.Profile.Current.PartyAuraHue, FileManager.Language.Dict["UI_Options_Video_PartyAuraHue"], 40, 5);
            _runMouseInSeparateThread = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_Video_RunMouseInASeparateThread"], Engine.GlobalSettings.RunMouseInASeparateThread, 0, 5);
            _auraMouse = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_Video_AuraOnMouse"], Engine.Profile.Current.AuraOnMouse, 0, 0);
            _xBR = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_Video_UseXBR"], Engine.Profile.Current.UseXBR, 0, 0);

            Add(rightArea, PAGE);
        }
        
        private void BuildCommands()
        {
            const int PAGE = 4;

            ScrollArea rightArea = new ScrollArea(190, 52 + 25 + 4, 150, 360, true);
            NiceButton addButton = new NiceButton(190, 20, 130, 20, ButtonAction.Activate, FileManager.Language.Dict["UI_Options_Macro_NewMacro"]) {IsSelectable = false, ButtonParameter = (int) Buttons.NewMacro};

            addButton.MouseUp += (sender, e) =>
            {
                EntryDialog dialog = new EntryDialog(250, 150, FileManager.Language.Dict["UI_Options_Macro_MacroName"], name =>
                {
                    if (string.IsNullOrWhiteSpace(name))
                        return;

                    MacroManager manager = Engine.SceneManager.GetScene<GameScene>().Macros;
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
                Engine.UI.Add(dialog);
            };

            Add(addButton, PAGE);

            NiceButton delButton = new NiceButton(190, 52, 130, 20, ButtonAction.Activate, FileManager.Language.Dict["UI_Options_Macro_DeleteMacro"]) {IsSelectable = false, ButtonParameter = (int) Buttons.DeleteMacro};

            delButton.MouseUp += (ss, ee) =>
            {
                NiceButton nb = rightArea.FindControls<ScrollAreaItem>()
                                         .SelectMany(s => s.Children.OfType<NiceButton>())
                                         .SingleOrDefault(a => a.IsSelected);

                if (nb != null)
                {
                    QuestionGump dialog = new QuestionGump(FileManager.Language.Dict["UI_Options_Macro_QuestionText"], b =>
                    {
                        if (!b)
                            return;

                        nb.Parent.Dispose();

                        if (_macroControl != null)
                        {
                            MacroCollectionControl control = _macroControl.FindControls<MacroCollectionControl>().SingleOrDefault();

                            if (control == null)
                                return;

                            Engine.SceneManager.GetScene<GameScene>().Macros.RemoveMacro(control.Macro);
                        }

                        if (rightArea.Children.OfType<ScrollAreaItem>().All(s => s.IsDisposed)) _macroControl?.Dispose();
                    });
                    Engine.UI.Add(dialog);
                }
            };

            Add(delButton, PAGE);
            Add(new Line(190, 52 + 25 + 2, 150, 1, Color.Gray.PackedValue), PAGE);
            Add(rightArea, PAGE);
            Add(new Line(191 + 150, 21, 1, 418, Color.Gray.PackedValue), PAGE);

            foreach (Macro macro in Engine.SceneManager.GetScene<GameScene>().Macros.GetAllMacros())
            {
                NiceButton nb;

                rightArea.Add(nb = new NiceButton(0, 0, 130, 25, ButtonAction.Activate, macro.Name)
                {
                    ButtonParameter = (int) Buttons.Last + 1 + rightArea.Children.Count
                });

                nb.IsSelected = true;

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

            _overrideAllFonts = new Checkbox(0x00D2, 0x00D3, FileManager.Language.Dict["UI_Options_Fonts_OverrideAllFonts"], FONT, HUE_FONT)
            {
                IsChecked = Engine.Profile.Current.OverrideAllFonts
            };

            _overrideAllFontsIsUnicodeCheckbox = new Combobox(_overrideAllFonts.Width + 5, _overrideAllFonts.Y, 100, new[]
            {
                FileManager.Language.Dict["UI_Options_Fonts_Encoded_ASCII"], FileManager.Language.Dict["UI_Options_Fonts_Encoded_Unicode"]
            }, Engine.Profile.Current.OverrideAllFontsIsUnicode ? 1 : 0)
            {
                IsVisible = _overrideAllFonts.IsChecked
            };
            _overrideAllFonts.ValueChanged += (ss, ee) => { _overrideAllFontsIsUnicodeCheckbox.IsVisible = _overrideAllFonts.IsChecked; };

            item.Add(_overrideAllFonts);
            item.Add(_overrideAllFontsIsUnicodeCheckbox);
            rightArea.Add(item);



            Label text = new Label(FileManager.Language.Dict["UI_Options_Fonts_SpeechFont"], true, HUE_FONT)
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

            _scaleSpeechDelay = new Checkbox(0x00D2, 0x00D3, FileManager.Language.Dict["UI_Options_Speech_ScaleSpeechDelay"], FONT, HUE_FONT)
            {
                IsChecked = Engine.Profile.Current.ScaleSpeechDelay
            };
            _scaleSpeechDelay.ValueChanged += (sender, e) => { _sliderSpeechDelay.IsVisible = _scaleSpeechDelay.IsChecked; };
            item.Add(_scaleSpeechDelay);
            _sliderSpeechDelay = new HSliderBar(150, 1, 180, 0, 1000, Engine.Profile.Current.SpeechDelay, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT);
            item.Add(_sliderSpeechDelay);
            rightArea.Add(item);

            _saveJournalCheckBox = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_Speech_SaveJournalToFile"], false, 0, 0);
            _saveJournalCheckBox.ValueChanged += (o, e) => { World.Journal.CreateWriter(_saveJournalCheckBox.IsChecked); };
            _saveJournalCheckBox.IsChecked = Engine.Profile.Current.SaveJournalToFile;

            // [BLOCK] activate chat
            {
                _chatAfterEnter = new Checkbox(0x00D2, 0x00D3, FileManager.Language.Dict["UI_Options_Speech_ActivateChatAfterEnter"], FONT, HUE_FONT)
                {
                    Y = 0,
                    IsChecked = Engine.Profile.Current.ActivateChatAfterEnter
                };
                _chatAfterEnter.ValueChanged += (sender, e) => { _activeChatArea.IsVisible = _chatAfterEnter.IsChecked; };
                rightArea.Add(_chatAfterEnter);

                _activeChatArea = new ScrollAreaItem();

                _chatAdditionalButtonsCheckbox = new Checkbox(0x00D2, 0x00D3, FileManager.Language.Dict["UI_Options_Speech_ActivateChatAdditionalButtons"], FONT, HUE_FONT)
                {
                    X = 20,
                    Y = 15,
                    IsChecked = Engine.Profile.Current.ActivateChatAdditionalButtons
                };
                _activeChatArea.Add(_chatAdditionalButtonsCheckbox);

                _chatShiftEnterCheckbox = new Checkbox(0x00D2, 0x00D3, FileManager.Language.Dict["UI_Options_Speech_ActivateChatShiftEnterSupport"], FONT, HUE_FONT)
                {
                    X = 20,
                    Y = 35,
                    IsChecked = Engine.Profile.Current.ActivateChatShiftEnterSupport
                };
                _activeChatArea.Add(_chatShiftEnterCheckbox);

                //var text = new Label(FileManager.Language.Dict["UI_Options_Speech_ActivateChatIgnoreHotkeys"], true, HUE_FONT)
                //{
                //    X = 20,
                //    Y = 60
                //};

                //_activeChatArea.Add(text);

                //_chatIgnodeHotkeysCheckbox = new Checkbox(0x00D2, 0x00D3, FileManager.Language.Dict["UI_Options_Speech_ActivateChatIgnoreHotkeys_Client"], FONT, HUE_FONT)
                //{
                //    X = 40,
                //    Y = 85,
                //    IsChecked = Engine.Profile.Current.ActivateChatIgnoreHotkeys
                //};
                //_activeChatArea.Add(_chatIgnodeHotkeysCheckbox);

                //_chatIgnodeHotkeysPluginsCheckbox = new Checkbox(0x00D2, 0x00D3, FileManager.Language.Dict["UI_Options_Speech_ActivateChatIgnoreHotkeys_Plugins"], FONT, HUE_FONT)
                //{
                //    X = 40,
                //    Y = 105,
                //    IsChecked = Engine.Profile.Current.ActivateChatIgnoreHotkeysPlugins
                //};
                //_activeChatArea.Add(_chatIgnodeHotkeysPluginsCheckbox);

                rightArea.Add(_activeChatArea);
            }

            _speechColorPickerBox = CreateClickableColorBox(rightArea, 0, 20, Engine.Profile.Current.SpeechHue, FileManager.Language.Dict["UI_Options_Speech_SpeechHue"], 20, 20);
            _emoteColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.EmoteHue, FileManager.Language.Dict["UI_Options_Speech_EmoteHue"], 20, 0);
            _yellColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.YellHue, FileManager.Language.Dict["UI_Options_Speech_YellHue"], 20, 0);
            _whisperColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.WhisperHue, FileManager.Language.Dict["UI_Options_Speech_WhisperHue"], 20, 0);
            _partyMessageColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.PartyMessageHue, FileManager.Language.Dict["UI_Options_Speech_PartyMessageHue"], 20, 0);
            _guildMessageColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.GuildMessageHue, FileManager.Language.Dict["UI_Options_Speech_GuildMessageHue"], 20, 0);
            _allyMessageColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.AllyMessageHue, FileManager.Language.Dict["UI_Options_Speech_AllyMessageHue"], 20, 0);

            _sliderSpeechDelay.IsVisible = _scaleSpeechDelay.IsChecked;

            Add(rightArea, PAGE);
        }

        private void BuildCombat()
        {
            const int PAGE = 8;

            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            _queryBeforAttackCheckbox = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_Combat_EnabledCriminalActionQuery"], Engine.Profile.Current.EnabledCriminalActionQuery, 0, 0);
            _spellFormatCheckbox = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_Combat_EnabledSpellFormat"], Engine.Profile.Current.EnabledSpellFormat, 0, 0);
            _spellColoringCheckbox = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_Combat_EnabledSpellHue"], Engine.Profile.Current.EnabledSpellHue, 0, 0);
            _castSpellsByOneClick = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_Combat_CastSpellsByOneClick"], Engine.Profile.Current.CastSpellsByOneClick, 0, 0);

            _innocentColorPickerBox = CreateClickableColorBox(rightArea, 0, 20, Engine.Profile.Current.InnocentHue, FileManager.Language.Dict["UI_Options_Combat_InnocentHue"], 20, 20);
            _friendColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.FriendHue, FileManager.Language.Dict["UI_Options_Combat_FriendHue"], 20, 0);
            _crimialColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.CriminalHue, FileManager.Language.Dict["UI_Options_Combat_CriminalHue"], 20, 0);
            _genericColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.AnimalHue, FileManager.Language.Dict["UI_Options_Combat_AnimalHue"], 20, 0);
            _murdererColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.MurdererHue, FileManager.Language.Dict["UI_Options_Combat_MurdererHue"], 20, 0);
            _enemyColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.EnemyHue, FileManager.Language.Dict["UI_Options_Combat_EnemyHue"], 20, 0);

            _beneficColorPickerBox = CreateClickableColorBox(rightArea, 0, 20, Engine.Profile.Current.BeneficHue, FileManager.Language.Dict["UI_Options_Combat_BeneficHue"], 20, 20);
            _harmfulColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.HarmfulHue, FileManager.Language.Dict["UI_Options_Combat_HarmfulHue"], 20, 0);
            _neutralColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.NeutralHue, FileManager.Language.Dict["UI_Options_Combat_NeutralHue"], 20, 0);

            ScrollAreaItem it = new ScrollAreaItem();

            _spellFormatBox = CreateInputField(it, new TextBox(FONT, 30, 200, 200)
            {
                Text = Engine.Profile.Current.SpellDisplayFormat,
                X = 0,
                Y = 20,
                Width = 200,
                Height = 30
            }, FileManager.Language.Dict["UI_Options_Combat_SpellDisplayFormat"], rightArea.Width - 20);

            rightArea.Add(it);

            Add(rightArea, PAGE);
        }

        private void BuildCounters()
        {
            const int PAGE = 9;
            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            _enableCounters = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_Counters_CounterBarEnabled"], Engine.Profile.Current.CounterBarEnabled, 0, 0);
            _highlightOnUse = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_Counters_CounterBarHighlightOnUse"], Engine.Profile.Current.CounterBarHighlightOnUse, 0, 0);
            _enableAbbreviatedAmount = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_Counters_CounterBarDisplayAbbreviatedAmount"], Engine.Profile.Current.CounterBarDisplayAbbreviatedAmount, 0, 0);

            ScrollAreaItem item = new ScrollAreaItem();

            _abbreviatedAmount = CreateInputField(item, new TextBox(FONT, -1, 80, 80)
            {
                X = _enableAbbreviatedAmount.X + 30,
                Y = 10,
                Width = 50,
                Height = 30,
                NumericOnly = true,
                Text = Engine.Profile.Current.CounterBarAbbreviatedAmount.ToString()
            });

            rightArea.Add(item);

            _highlightOnAmount = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_Counters_CounterBarHighlightOnAmount"], Engine.Profile.Current.CounterBarHighlightOnAmount, 0, 0);

            item = new ScrollAreaItem();

            _highlightAmount = CreateInputField(item, new TextBox(FONT, 2, 80, 80)
            {
                X = _highlightOnAmount.X + 30,
                Y = 10,
                Width = 50,
                Height = 30,
                NumericOnly = true,
                Text = Engine.Profile.Current.CounterBarHighlightAmount.ToString()
            });

            rightArea.Add(item);

            item = new ScrollAreaItem();

            Label text = new Label(FileManager.Language.Dict["UI_Options_Counters_CounterLayout"], true, HUE_FONT)
            {
                Y = _highlightOnUse.Bounds.Bottom + 5
            };
            item.Add(text);
            //_counterLayout = new Combobox(text.Bounds.Right + 10, _highlightOnUse.Bounds.Bottom + 5, 150, new[] { "Horizontal", "Vertical" }, Engine.Profile.Current.CounterBarIsVertical ? 1 : 0);
            //item.Add(_counterLayout);
            rightArea.Add(item);


            item = new ScrollAreaItem();

            text = new Label(FileManager.Language.Dict["UI_Options_Counters_CellSize"], true, HUE_FONT)
            {
                X = 10,
                Y = 10
            };
            item.Add(text);

            _cellSize = new HSliderBar(text.X + text.Width + 10, text.Y + 5, 80, 30, 80, Engine.Profile.Current.CounterBarCellSize, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT);
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
                Text = Engine.Profile.Current.CounterBarRows.ToString()
            }, FileManager.Language.Dict["UI_Options_Counters_Rows"]);

            _columns = CreateInputField(item, new TextBox(FONT, 5, 80, 80)
            {
                X = _rows.X + _rows.Width + 30,
                Y = _cellSize.Y + _cellSize.Height + 25,
                Width = 50,
                Height = 30,
                NumericOnly = true,
                Text = Engine.Profile.Current.CounterBarColumns.ToString()
            }, FileManager.Language.Dict["UI_Options_Counters_Columns"]);

            rightArea.Add(item);

            Add(rightArea, PAGE);
        }

        private void BuildExperimental()
        {
            const int PAGE = 10;
            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            _enableSelectionArea = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_Experimental_EnableSelectionArea"], Engine.Profile.Current.EnableSelectionArea, 0, 0);

            _debugGumpIsDisabled = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_Experimental_DebugGumpIsDisabled"], Engine.Profile.Current.DebugGumpIsDisabled, 0, 0);
            _restoreLastGameSize = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_Experimental_RestoreLastGameSize"], Engine.Profile.Current.RestoreLastGameSize, 0, 0);

            _autoOpenDoors = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_Experimental_AutoOpenDoors"], Engine.Profile.Current.AutoOpenDoors, 0, 0);
            _smoothDoors = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_Experimental_SmoothDoors"], Engine.Profile.Current.SmoothDoors, 20, 5);

            _autoOpenCorpseArea = new ScrollAreaItem();

            _autoOpenCorpse = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_Experimental_AutoOpenCorpses"], Engine.Profile.Current.AutoOpenCorpses, 0, 5);
            _autoOpenCorpse.ValueChanged += (sender, e) => { _autoOpenCorpseArea.IsVisible = _autoOpenCorpse.IsChecked; };

            _autoOpenCorpseRange = CreateInputField(_autoOpenCorpseArea, new TextBox(FONT, 2, 80, 80)
            {
                X = 20,
                Y = _cellSize.Y + _cellSize.Height - 15,
                Width = 50,
                Height = 30,
                NumericOnly = true,
                Text = Engine.Profile.Current.AutoOpenCorpseRange.ToString()
            }, FileManager.Language.Dict["UI_Options_Experimental_AutoOpenCorpseRange"]);

            /* text = new Label("- Aura under feet:", true, HUE_FONT, 0, FONT)
            {
                Y = 10
            };
            item.Add(text);

            _auraType = new Combobox(text.Width + 20, text.Y, 100, new[] {"None", "Warmode", "Ctrl+Shift", "Always"})
            {
                SelectedIndex = Engine.Profile.Current.AuraUnderFeetType
            };*/
            var text = new Label(FileManager.Language.Dict["UI_Options_Experimental_CorpseOpenOptions"], true, HUE_FONT)
            {
                Y = _autoOpenCorpseRange.Y + 30,
                X = 10
            };
            _autoOpenCorpseArea.Add(text);

            _autoOpenCorpseOptions = new Combobox(text.Width + 20, text.Y, 150, new[]
            {
                FileManager.Language.Dict["UI_Options_Experimental_CorpseOpenOptions_None"], FileManager.Language.Dict["UI_Options_Experimental_CorpseOpenOptions_NotTargeting"], FileManager.Language.Dict["UI_Options_Experimental_CorpseOpenOptions_NotHiding"], FileManager.Language.Dict["UI_Options_Experimental_CorpseOpenOptions_Both"]
            })
            {
                SelectedIndex = Engine.Profile.Current.CorpseOpenOptions
            };
            _autoOpenCorpseArea.Add(_autoOpenCorpseOptions);

            rightArea.Add(_autoOpenCorpseArea);

            // [BLOCK] disable hotkeys
            {
                _disableDefaultHotkeys = new Checkbox(0x00D2, 0x00D3, FileManager.Language.Dict["UI_Options_Experimental_DisableDefaultHotkeys"], FONT, HUE_FONT)
                {
                    Y = 0,
                    IsChecked = Engine.Profile.Current.DisableDefaultHotkeys
                };
                _disableDefaultHotkeys.ValueChanged += (sender, e) => { _defaultHotkeysArea.IsVisible = _disableDefaultHotkeys.IsChecked; };

                rightArea.Add(_disableDefaultHotkeys);

                _defaultHotkeysArea = new ScrollAreaItem();

                _disableArrowBtn = new Checkbox(0x00D2, 0x00D3, FileManager.Language.Dict["UI_Options_Experimental_DisableArrowBtn"], FONT, HUE_FONT)
                {
                    X = 20,
                    Y = 5,
                    IsChecked = Engine.Profile.Current.DisableArrowBtn
                };
                _defaultHotkeysArea.Add(_disableArrowBtn);

                _disableTabBtn = new Checkbox(0x00D2, 0x00D3, FileManager.Language.Dict["UI_Options_Experimental_DisableTabBtn"], FONT, HUE_FONT)
                {
                    X = 20,
                    Y = 25,
                    IsChecked = Engine.Profile.Current.DisableTabBtn
                };
                _defaultHotkeysArea.Add(_disableTabBtn);

                _disableCtrlQWBtn = new Checkbox(0x00D2, 0x00D3, FileManager.Language.Dict["UI_Options_Experimental_DisableCtrlQWBtn"], FONT, HUE_FONT)
                {
                    X = 20,
                    Y = 45,
                    IsChecked = Engine.Profile.Current.DisableCtrlQWBtn
                };
                _defaultHotkeysArea.Add(_disableCtrlQWBtn);

                rightArea.Add(_defaultHotkeysArea);

                _defaultHotkeysArea.IsVisible = _disableDefaultHotkeys.IsChecked;
            }

            _enableDragSelect = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_Experimental_EnableDragSelect"], Engine.Profile.Current.EnableDragSelect, 0, 0);

            _dragSelectArea = new ScrollAreaItem();

            text = new Label(FileManager.Language.Dict["UI_Options_Experimental_EnableDragSelect_Key"], true, HUE_FONT)
            {
                X = 20
            };
            _dragSelectArea.Add(text);

            _dragSelectModifierKey = new Combobox(text.Width + 80, text.Y, 100, new[] { FileManager.Language.Dict["UI_Options_Experimental_EnableDragSelect_Key_None"], FileManager.Language.Dict["UI_Options_Experimental_EnableDragSelect_Key_Ctrl"], FileManager.Language.Dict["UI_Options_Experimental_EnableDragSelect_Key_Shift"] })
            {
                SelectedIndex = Engine.Profile.Current.DragSelectModifierKey
            };
            _dragSelectArea.Add(_dragSelectModifierKey);

            _dragSelectHumanoidsOnly = new Checkbox(0x00D2, 0x00D3, FileManager.Language.Dict["UI_Options_Experimental_EnableDragSelect_DragSelectHumanoidsOnly"], FONT, HUE_FONT, true)
            {
                IsChecked = Engine.Profile.Current.DragSelectHumanoidsOnly,
                X = 20,
                Y = 20
            };
            _dragSelectArea.Add(_dragSelectHumanoidsOnly);

            _enableDragSelect.ValueChanged += (sender, e) => { _dragSelectArea.IsVisible = _enableDragSelect.IsChecked; };

            rightArea.Add(_dragSelectArea);


            ScrollAreaItem _containerGumpLocation = new ScrollAreaItem();

            _overrideContainerLocation = new Checkbox(0x00D2, 0x00D3, FileManager.Language.Dict["UI_Options_Experimental_OverrideContainerLocation"], FONT, HUE_FONT, true)
            {
                IsChecked = Engine.Profile.Current.OverrideContainerLocation,
            };

            _overrideContainerLocationSetting = new Combobox(_overrideContainerLocation.Width + 20, 0, 200, new[] { FileManager.Language.Dict["UI_Options_Experimental_OverrideContainerLocation_NearContainerPosition"], FileManager.Language.Dict["UI_Options_Experimental_OverrideContainerLocation_TopRight"], FileManager.Language.Dict["UI_Options_Experimental_OverrideContainerLocation_LastDraggedPosition"] }, Engine.Profile.Current.OverrideContainerLocationSetting);

            _containerGumpLocation.Add(_overrideContainerLocation);
            _containerGumpLocation.Add(_overrideContainerLocationSetting);

            rightArea.Add(_containerGumpLocation);

            _showTargetRangeIndicator = new Checkbox(0x00D2, 0x00D3, "Show target range indicator", FONT, HUE_FONT, true)
            {
                IsChecked = Engine.Profile.Current.ShowTargetRangeIndicator,
            };

            rightArea.Add(_showTargetRangeIndicator);

            ScrollAreaItem langItem = new ScrollAreaItem();
            Label langText = new Label(FileManager.Language.Dict["UI_Options_General_Language"], true, HUE_FONT)
            {
                Y = 20
            };
            langItem.Add(langText);
            _language = new Combobox(langText.X + langText.Width + 10, langText.Y - 2, 200, FileManager.Language.Language.ToArray(), FileManager.Language.Language.FindIndex(l => l.Equals(Engine.GlobalSettings.Language.ToUpper())));
            langItem.Add(_language);

            rightArea.Add(langItem);

            Add(rightArea, PAGE);

            _autoOpenCorpseArea.IsVisible = _autoOpenCorpse.IsChecked;
            _dragSelectArea.IsVisible = _enableDragSelect.IsChecked;
        }

        private void BuildNetwork()
        {
            const int PAGE = 11;

            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            _showNetStats = CreateCheckBox(rightArea, FileManager.Language.Dict["UI_Options_Network_ShowNetworkStats"], Engine.Profile.Current.ShowNetworkStats, 0, 0);

            Add(rightArea, PAGE);
        }

        private void BuildInfoBar()
        {
            const int PAGE = 12;

            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            _showInfoBar = CreateCheckBox(rightArea, "Show Info Bar", Engine.Profile.Current.ShowInfoBar, 0, 0);


            ScrollAreaItem _infoBarHighlightScrollArea = new ScrollAreaItem();

            _infoBarHighlightScrollArea.Add(new Label("Data highlight type:", true, 999));
            _infoBarHighlightType = new Combobox(130, 0, 150, new[] { "Text color", "Colored bars" }, Engine.Profile.Current.InfoBarHighlightType);
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


            InfoBarManager ibmanager = Engine.SceneManager.GetScene<GameScene>().InfoBars;

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

            _containersScale = new HSliderBar(text.X + text.Width + 10, text.Y + 5, 200, Constants.MIN_CONTAINER_SIZE_PERC, Constants.MAX_CONTAINER_SIZE_PERC, Engine.Profile.Current.ContainersScale, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT);
            item.Add(_containersScale);

            rightArea.Add(item);

            _containerScaleItems = CreateCheckBox(rightArea, "Scale items inside containers", Engine.Profile.Current.ScaleItemsInsideContainers, 0, 20);

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
                    _alwaysRun.IsChecked = false;
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
                    Engine.SceneManager.GetScene<GameScene>().Scale = 1;
                    Engine.Profile.Current.RestoreScaleValue = Engine.Profile.Current.ScaleZoom = 1f;
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
                    Engine.UI.SystemChat.IsActive = !_chatAfterEnter.IsChecked;
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

                    break;

                case 11:
                    _showNetStats.IsChecked = false;

                    break;

                case 12:

                    break;

                case 13:
                    _containersScale.Value = 100;
                    _containerScaleItems.IsChecked = false;
                    break;
            }
        }

        private void Apply()
        {
            WorldViewportGump vp = Engine.UI.GetGump<WorldViewportGump>();

            // general
            Engine.Profile.Current.MaxFPS = Engine.FpsLimit = _sliderFPS.Value;
            Engine.GlobalSettings.MaxLoginFPS = _sliderFPSLogin.Value;
            Engine.Profile.Current.HighlightGameObjects = _highlightObjects.IsChecked;
            Engine.Profile.Current.ReduceFPSWhenInactive = _reduceFPSWhenInactive.IsChecked;
            //Engine.Profile.Current.SmoothMovements = _smoothMovements.IsChecked;
            Engine.Profile.Current.EnablePathfind = _enablePathfind.IsChecked;
            Engine.Profile.Current.AlwaysRun = _alwaysRun.IsChecked;
            Engine.Profile.Current.ShowMobilesHP = _showHpMobile.IsChecked;
            Engine.Profile.Current.HighlightMobilesByFlags = _highlightByState.IsChecked;
            Engine.Profile.Current.PoisonHue = _poisonColorPickerBox.Hue;
            Engine.Profile.Current.ParalyzedHue = _paralyzedColorPickerBox.Hue;
            Engine.Profile.Current.InvulnerableHue = _invulnerableColorPickerBox.Hue;
            Engine.Profile.Current.MobileHPType = _hpComboBox.SelectedIndex;
            Engine.Profile.Current.MobileHPShowWhen = _hpComboBoxShowWhen.SelectedIndex;
            Engine.Profile.Current.HoldDownKeyTab = _holdDownKeyTab.IsChecked;
            Engine.Profile.Current.HoldDownKeyAltToCloseAnchored = _holdDownKeyAlt.IsChecked;
            Engine.Profile.Current.HoldShiftForContext = _holdShiftForContext.IsChecked;
            Engine.Profile.Current.HoldShiftToSplitStack = _holdShiftToSplitStack.IsChecked;
            Engine.Profile.Current.CloseHealthBarType = _healtbarType.SelectedIndex;

            if (Engine.Profile.Current.DrawRoofs == _drawRoofs.IsChecked)
            {
                Engine.Profile.Current.DrawRoofs = !_drawRoofs.IsChecked;
                Engine.SceneManager.GetScene<GameScene>()?.UpdateMaxDrawZ(true);
            }

            if (Engine.Profile.Current.TopbarGumpIsDisabled != _enableTopbar.IsChecked)
            {
                if (_enableTopbar.IsChecked)
                    Engine.UI.GetGump<TopBarGump>()?.Dispose();
                else
                    TopBarGump.Create();

                Engine.Profile.Current.TopbarGumpIsDisabled = _enableTopbar.IsChecked;
            }

            if (Engine.Profile.Current.EnableCaveBorder != _enableCaveBorder.IsChecked)
            {
                Engine.Profile.Current.EnableCaveBorder = _enableCaveBorder.IsChecked;
                FileManager.Art.ClearCaveTextures();
            }

            Engine.Profile.Current.TreeToStumps = _treeToStumps.IsChecked;
            Engine.Profile.Current.FieldsType = _fieldsType.SelectedIndex;
            Engine.Profile.Current.HideVegetation = _hideVegetation.IsChecked;
            Engine.Profile.Current.NoColorObjectsOutOfRange = _noColorOutOfRangeObjects.IsChecked;
            Engine.Profile.Current.UseCircleOfTransparency = _useCircleOfTransparency.IsChecked;
            Engine.Profile.Current.CircleOfTransparencyRadius = _circleOfTranspRadius.Value;

            Engine.Profile.Current.VendorGumpHeight = (int) _vendorGumpSize.Tag;
            Engine.Profile.Current.StandardSkillsGump = _useStandardSkillsGump.IsChecked;

            if (_useStandardSkillsGump.IsChecked)
            {
                var newGump = Engine.UI.GetGump<SkillGumpAdvanced>();

                if (newGump != null)
                {
                    Engine.UI.Add(new StandardSkillsGump
                                      {X = newGump.X, Y = newGump.Y});
                    newGump.Dispose();
                }
            }
            else
            {
                var standardGump = Engine.UI.GetGump<StandardSkillsGump>();

                if (standardGump != null)
                {
                    Engine.UI.Add(new SkillGumpAdvanced
                                      {X = standardGump.X, Y = standardGump.Y});
                    standardGump.Dispose();
                }
            }

            Engine.Profile.Current.ShowNewMobileNameIncoming = _showMobileNameIncoming.IsChecked;
            Engine.Profile.Current.ShowNewCorpseNameIncoming = _showCorpseNameIncoming.IsChecked;
            Engine.Profile.Current.GridLootType = _gridLoot.SelectedIndex;
            Engine.Profile.Current.SallosEasyGrab = _sallosEasyGrab.IsChecked;
            Engine.Profile.Current.PartyInviteGump = _partyInviteGump.IsChecked;


            // sounds
            Engine.Profile.Current.EnableSound = _enableSounds.IsChecked;
            Engine.Profile.Current.EnableMusic = _enableMusic.IsChecked;
            Engine.Profile.Current.EnableFootstepsSound = _footStepsSound.IsChecked;
            Engine.Profile.Current.EnableCombatMusic = _combatMusic.IsChecked;
            Engine.Profile.Current.ReproduceSoundsInBackground = _musicInBackground.IsChecked;
            Engine.Profile.Current.SoundVolume = _soundsVolume.Value;
            Engine.Profile.Current.MusicVolume = _musicVolume.Value;
            Engine.GlobalSettings.LoginMusicVolume = _loginMusicVolume.Value;
            Engine.GlobalSettings.LoginMusic = _loginMusic.IsChecked;

            Engine.SceneManager.CurrentScene.Audio.UpdateCurrentMusicVolume();

            if (!Engine.Profile.Current.EnableMusic)
                Engine.SceneManager.CurrentScene.Audio.StopMusic();

            // speech
            Engine.Profile.Current.ScaleSpeechDelay = _scaleSpeechDelay.IsChecked;
            Engine.Profile.Current.SpeechDelay = _sliderSpeechDelay.Value;
            Engine.Profile.Current.SpeechHue = _speechColorPickerBox.Hue;
            Engine.Profile.Current.EmoteHue = _emoteColorPickerBox.Hue;
            Engine.Profile.Current.YellHue = _yellColorPickerBox.Hue;
            Engine.Profile.Current.WhisperHue = _whisperColorPickerBox.Hue;
            Engine.Profile.Current.PartyMessageHue = _partyMessageColorPickerBox.Hue;
            Engine.Profile.Current.GuildMessageHue = _guildMessageColorPickerBox.Hue;
            Engine.Profile.Current.AllyMessageHue = _allyMessageColorPickerBox.Hue;

            if (Engine.Profile.Current.ActivateChatAfterEnter != _chatAfterEnter.IsChecked)
            {
                Engine.UI.SystemChat.IsActive = !_chatAfterEnter.IsChecked;
                Engine.Profile.Current.ActivateChatAfterEnter = _chatAfterEnter.IsChecked;
            }

            Engine.Profile.Current.ActivateChatAdditionalButtons = _chatAdditionalButtonsCheckbox.IsChecked;
            Engine.Profile.Current.ActivateChatShiftEnterSupport = _chatShiftEnterCheckbox.IsChecked;
            Engine.Profile.Current.SaveJournalToFile = _saveJournalCheckBox.IsChecked;

            // video
            Engine.Profile.Current.EnableDeathScreen = _enableDeathScreen.IsChecked;
            Engine.Profile.Current.EnableBlackWhiteEffect = _enableBlackWhiteEffect.IsChecked;

            Engine.GlobalSettings.Debug = _debugControls.IsChecked;

            if (Engine.Profile.Current.EnableScaleZoom != _zoomCheckbox.IsChecked)
            {
                if (!_zoomCheckbox.IsChecked)
                    Engine.SceneManager.GetScene<GameScene>().Scale = 1;

                Engine.Profile.Current.EnableScaleZoom = _zoomCheckbox.IsChecked;
            }

            Engine.Profile.Current.SaveScaleAfterClose = _savezoomCheckbox.IsChecked;

            if (_restorezoomCheckbox.IsChecked != Engine.Profile.Current.RestoreScaleAfterUnpressCtrl)
            {
                if (_restorezoomCheckbox.IsChecked)
                    Engine.Profile.Current.RestoreScaleValue = Engine.SceneManager.GetScene<GameScene>().Scale;

                Engine.Profile.Current.RestoreScaleAfterUnpressCtrl = _restorezoomCheckbox.IsChecked;
            }

            if (Engine.GlobalSettings.ShardType != _shardType.SelectedIndex)
            {
                var status = StatusGumpBase.GetStatusGump();

                Engine.GlobalSettings.ShardType = _shardType.SelectedIndex;

                if (status != null)
                {
                    status.Dispose();
                    StatusGumpBase.AddStatusGump(status.ScreenCoordinateX, status.ScreenCoordinateY);
                }
            }

            int.TryParse(_gameWindowWidth.Text, out int gameWindowSizeWidth);
            int.TryParse(_gameWindowHeight.Text, out int gameWindowSizeHeight);

            if (gameWindowSizeWidth != Engine.Profile.Current.GameWindowSize.X || gameWindowSizeHeight != Engine.Profile.Current.GameWindowSize.Y)
            {
                if (vp != null)
                {
                    Point n = vp.ResizeWindow(new Point(gameWindowSizeWidth, gameWindowSizeHeight));

                    _gameWindowWidth.Text = n.X.ToString();
                    _gameWindowHeight.Text = n.Y.ToString();
                }
            }

            int.TryParse(_gameWindowPositionX.Text, out int gameWindowPositionX);
            int.TryParse(_gameWindowPositionY.Text, out int gameWindowPositionY);

            if (gameWindowPositionX != Engine.Profile.Current.GameWindowPosition.X || gameWindowPositionY != Engine.Profile.Current.GameWindowPosition.Y)
            {
                if (vp != null)
                    vp.Location = Engine.Profile.Current.GameWindowPosition = new Point(gameWindowPositionX, gameWindowPositionY);
            }

            if (Engine.Profile.Current.GameWindowLock != _gameWindowLock.IsChecked)
            {
                if (vp != null) vp.CanMove = !_gameWindowLock.IsChecked;
                Engine.Profile.Current.GameWindowLock = _gameWindowLock.IsChecked;
            }

            if (_gameWindowFullsize.IsChecked && (gameWindowPositionX != -5 || gameWindowPositionY != -5))
            {
                if (Engine.Profile.Current.GameWindowFullSize == _gameWindowFullsize.IsChecked)
                    _gameWindowFullsize.IsChecked = false;
            }

            if (Engine.Profile.Current.GameWindowFullSize != _gameWindowFullsize.IsChecked)
            {
                Point n = Point.Zero, loc = Point.Zero;

                if (_gameWindowFullsize.IsChecked)
                {
                    if (vp != null)
                    {
                        n = vp.ResizeWindow(new Point(Engine.WindowWidth, Engine.WindowHeight));
                        loc = Engine.Profile.Current.GameWindowPosition = vp.Location = new Point(-5, -5);
                    }
                }
                else
                {
                    if (vp != null)
                    {
                        n = vp.ResizeWindow(new Point(600, 480));
                        loc = vp.Location = Engine.Profile.Current.GameWindowPosition = new Point(20, 20);
                    }
                }

                _gameWindowPositionX.Text = loc.X.ToString();
                _gameWindowPositionY.Text = loc.Y.ToString();
                _gameWindowWidth.Text = n.X.ToString();
                _gameWindowHeight.Text = n.Y.ToString();

                Engine.Profile.Current.GameWindowFullSize = _gameWindowFullsize.IsChecked;
            }

            Engine.Profile.Current.UseCustomLightLevel = _enableLight.IsChecked;
            Engine.Profile.Current.LightLevel = (byte) (_lightBar.MaxValue - _lightBar.Value);

            if (_enableLight.IsChecked)
            {
               World.Light.Overall = Engine.Profile.Current.LightLevel;
               World.Light.Personal = 0;
            }
            else
            {
                World.Light.Overall = World.Light.RealOverall;
                World.Light.Personal = World.Light.RealPersonal;
            }

            Engine.Profile.Current.UseColoredLights = _useColoredLights.IsChecked;
            Engine.Profile.Current.UseDarkNights = _darkNights.IsChecked;

            Engine.Profile.Current.Brighlight = _brighlight.Value / 100f;

            Engine.Profile.Current.ShadowsEnabled = _enableShadows.IsChecked;
            Engine.Profile.Current.AuraUnderFeetType = _auraType.SelectedIndex;
            Engine.Instance.IsMouseVisible = Engine.GlobalSettings.RunMouseInASeparateThread = _runMouseInSeparateThread.IsChecked;
            Engine.Profile.Current.AuraOnMouse = _auraMouse.IsChecked;
            Engine.Profile.Current.UseXBR = _xBR.IsChecked;
            Engine.Profile.Current.PartyAura = _partyAura.IsChecked;
            Engine.Profile.Current.PartyAuraHue = _partyAuraColorPickerBox.Hue;


            // fonts
            var _fontValue = _fontSelectorChat.GetSelectedFont();
            Engine.Profile.Current.OverrideAllFonts = _overrideAllFonts.IsChecked;
            Engine.Profile.Current.OverrideAllFontsIsUnicode = _overrideAllFontsIsUnicodeCheckbox.SelectedIndex == 1;
            if (Engine.Profile.Current.ChatFont != _fontValue)
            {
                Engine.Profile.Current.ChatFont = _fontValue;
                WorldViewportGump viewport = Engine.UI.GetGump<WorldViewportGump>();
                viewport?.ReloadChatControl(new SystemChatControl(5, 5, Engine.Profile.Current.GameWindowSize.X, Engine.Profile.Current.GameWindowSize.Y));
            }

            // combat
            Engine.Profile.Current.InnocentHue = _innocentColorPickerBox.Hue;
            Engine.Profile.Current.FriendHue = _friendColorPickerBox.Hue;
            Engine.Profile.Current.CriminalHue = _crimialColorPickerBox.Hue;
            Engine.Profile.Current.AnimalHue = _genericColorPickerBox.Hue;
            Engine.Profile.Current.EnemyHue = _enemyColorPickerBox.Hue;
            Engine.Profile.Current.MurdererHue = _murdererColorPickerBox.Hue;
            Engine.Profile.Current.EnabledCriminalActionQuery = _queryBeforAttackCheckbox.IsChecked;
            Engine.Profile.Current.CastSpellsByOneClick = _castSpellsByOneClick.IsChecked;

            Engine.Profile.Current.BeneficHue = _beneficColorPickerBox.Hue;
            Engine.Profile.Current.HarmfulHue = _harmfulColorPickerBox.Hue;
            Engine.Profile.Current.NeutralHue = _neutralColorPickerBox.Hue;
            Engine.Profile.Current.EnabledSpellHue = _spellColoringCheckbox.IsChecked;
            Engine.Profile.Current.EnabledSpellFormat = _spellFormatCheckbox.IsChecked;
            Engine.Profile.Current.SpellDisplayFormat = _spellFormatBox.Text;

            // macros
            Engine.Profile.Current.Macros = Engine.SceneManager.GetScene<GameScene>().Macros.GetAllMacros().ToArray();

            // counters

            bool before = Engine.Profile.Current.CounterBarEnabled;
            Engine.Profile.Current.CounterBarEnabled = _enableCounters.IsChecked;
            Engine.Profile.Current.CounterBarCellSize = _cellSize.Value;
            Engine.Profile.Current.CounterBarRows = int.Parse(_rows.Text);
            Engine.Profile.Current.CounterBarColumns = int.Parse(_columns.Text);
            Engine.Profile.Current.CounterBarHighlightOnUse = _highlightOnUse.IsChecked;

            Engine.Profile.Current.CounterBarHighlightAmount = int.Parse(_highlightAmount.Text);
            Engine.Profile.Current.CounterBarAbbreviatedAmount = int.Parse(_abbreviatedAmount.Text);
            Engine.Profile.Current.CounterBarHighlightOnAmount = _highlightOnAmount.IsChecked;
            Engine.Profile.Current.CounterBarDisplayAbbreviatedAmount = _enableAbbreviatedAmount.IsChecked;

            CounterBarGump counterGump = Engine.UI.GetGump<CounterBarGump>();

            counterGump?.SetLayout(Engine.Profile.Current.CounterBarCellSize,
                                   Engine.Profile.Current.CounterBarRows,
                                   Engine.Profile.Current.CounterBarColumns);


            if (before != Engine.Profile.Current.CounterBarEnabled)
            {
                if (counterGump == null)
                {
                    if (Engine.Profile.Current.CounterBarEnabled)
                        Engine.UI.Add(new CounterBarGump(200, 200, Engine.Profile.Current.CounterBarCellSize, Engine.Profile.Current.CounterBarRows, Engine.Profile.Current.CounterBarColumns));
                }
                else
                    counterGump.IsEnabled = counterGump.IsVisible = Engine.Profile.Current.CounterBarEnabled;
            }

            // experimental
            Engine.Profile.Current.EnableSelectionArea = _enableSelectionArea.IsChecked;
            Engine.Profile.Current.RestoreLastGameSize = _restoreLastGameSize.IsChecked;

            // Reset nested checkboxes if parent checkbox is unchecked
            if (!_disableDefaultHotkeys.IsChecked)
            {
                _disableArrowBtn.IsChecked = false;
                _disableTabBtn.IsChecked = false;
                _disableCtrlQWBtn.IsChecked = false;
            }

            // NOTE: Keep these assignments AFTER the code above that resets nested checkboxes if parent checkbox is unchecked
            Engine.Profile.Current.DisableDefaultHotkeys = _disableDefaultHotkeys.IsChecked;
            Engine.Profile.Current.DisableArrowBtn = _disableArrowBtn.IsChecked;
            Engine.Profile.Current.DisableTabBtn = _disableTabBtn.IsChecked;
            Engine.Profile.Current.DisableCtrlQWBtn = _disableCtrlQWBtn.IsChecked;

            if (Engine.Profile.Current.DebugGumpIsDisabled != _debugGumpIsDisabled.IsChecked)
            {
                DebugGump debugGump = Engine.UI.GetGump<DebugGump>();

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
                            X = Engine.Profile.Current.DebugGumpPosition.X,
                            Y = Engine.Profile.Current.DebugGumpPosition.Y
                        };
                        Engine.UI.Add(debugGump);
                    }
                    else
                    {
                        debugGump.IsVisible = true;
                        debugGump.SetInScreen();
                    }
                }

                Engine.Profile.Current.DebugGumpIsDisabled = _debugGumpIsDisabled.IsChecked;
            }

            Engine.Profile.Current.AutoOpenDoors = _autoOpenDoors.IsChecked;
            Engine.Profile.Current.SmoothDoors = _smoothDoors.IsChecked;
            Engine.Profile.Current.AutoOpenCorpses = _autoOpenCorpse.IsChecked;
            Engine.Profile.Current.AutoOpenCorpseRange = int.Parse(_autoOpenCorpseRange.Text);
            Engine.Profile.Current.CorpseOpenOptions = _autoOpenCorpseOptions.SelectedIndex;

            Engine.Profile.Current.EnableDragSelect = _enableDragSelect.IsChecked;
            Engine.Profile.Current.DragSelectModifierKey = _dragSelectModifierKey.SelectedIndex;
            Engine.Profile.Current.DragSelectHumanoidsOnly = _dragSelectHumanoidsOnly.IsChecked;

            Engine.Profile.Current.OverrideContainerLocation = _overrideContainerLocation.IsChecked;
            Engine.Profile.Current.OverrideContainerLocationSetting = _overrideContainerLocationSetting.SelectedIndex;
            Engine.Profile.Current.ShowTargetRangeIndicator = _showTargetRangeIndicator.IsChecked;

            Engine.GlobalSettings.Language = _language.GetSelectedItem;
            FileManager.Language.Load(Engine.GlobalSettings.Language);
            Engine.GlobalSettings.Save();
            // network
            Engine.Profile.Current.ShowNetworkStats = _showNetStats.IsChecked;

            // infobar
            Engine.Profile.Current.ShowInfoBar = _showInfoBar.IsChecked;
            Engine.Profile.Current.InfoBarHighlightType = _infoBarHighlightType.SelectedIndex;


            InfoBarManager ibmanager = Engine.SceneManager.GetScene<GameScene>().InfoBars;
            ibmanager.Clear();

            for (int i = 0; i < _infoBarBuilderControls.Count; i++)
            {
                if (!_infoBarBuilderControls[i].IsDisposed)
                    ibmanager.AddItem(new InfoBarItem(_infoBarBuilderControls[i].LabelText, _infoBarBuilderControls[i].Var, _infoBarBuilderControls[i].Hue));
            }

            Engine.Profile.Current.InfoBarItems = ibmanager.GetInfoBars().ToArray();


            InfoBarGump infoBarGump = Engine.UI.GetGump<InfoBarGump>();

            if (Engine.Profile.Current.ShowInfoBar)
            {
                if (infoBarGump == null)
                {
                    Engine.UI.Add(new InfoBarGump() { X = 300, Y = 300 });
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
            int containerScale = Engine.Profile.Current.ContainersScale;

            if ((byte) _containersScale.Value != containerScale || Engine.Profile.Current.ScaleItemsInsideContainers != _containerScaleItems.IsChecked)
            {
                containerScale = Engine.Profile.Current.ContainersScale = (byte)_containersScale.Value;
                Engine.UI.ContainerScale = containerScale / 100f;
                Engine.Profile.Current.ScaleItemsInsideContainers = _containerScaleItems.IsChecked;

                foreach (ContainerGump resizableGump in Engine.UI.Gumps.OfType<ContainerGump>())
                {
                    resizableGump.ForceUpdate();
                }
            }

            Engine.Profile.Current?.Save(Engine.UI.Gumps.OfType<Gump>().Where(s => s.CanBeSaved).Reverse().ToList());
        }

        internal void UpdateVideo()
        {
            WorldViewportGump gump = Engine.UI.GetGump<WorldViewportGump>();
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
                            IsChecked = Engine.Profile.Current.ChatFont == i
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