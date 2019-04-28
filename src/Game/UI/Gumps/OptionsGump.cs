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
        private ScrollAreaItem _activeChatArea;
        //private Combobox _counterLayout;
        private HSliderBar _cellSize;

        // video
        private Checkbox _debugControls, _enableDeathScreen, _enableBlackWhiteEffect, _enableLight, _enableShadows;

        //counters
        private Checkbox _enableCounters, _highlightOnUse;

        //experimental
        private Checkbox _enableSelectionArea;

        // sounds
        private Checkbox _enableSounds, _enableMusic, _footStepsSound, _combatMusic, _musicInBackground, _loginMusic;
        private RadioButton _fieldsToTile, _staticFields, _normalFields;

        // fonts
        private FontSelector _fontSelectorChat;
        private TextBox _gameWindowHeight;

        private Checkbox _gameWindowLock, _gameWindowFullsize;
        // GameWindowPosition
        private TextBox _gameWindowPositionX;
        private TextBox _gameWindowPositionY;

        // GameWindowSize
        private TextBox _gameWindowWidth;
        private Checkbox _highlightObjects, /*_smoothMovements,*/ _enablePathfind, _alwaysRun, _preloadMaps, _showHpMobile, _highlightByState, _drawRoofs, _treeToStumps, _hideVegetation, _noColorOutOfRangeObjects, _useCircleOfTransparency, _enableTopbar, _holdDownKeyTab, _holdDownKeyAlt, _chatAfterEnter, _chatIgnodeHotkeysCheckbox, _chatIgnodeHotkeysPluginsCheckbox, _chatAdditionalButtonsCheckbox, _chatShiftEnterCheckbox, _enableCaveBorder;
        private Combobox _hpComboBox, _healtbarType;

        // combat
        private ColorBox _innocentColorPickerBox, _friendColorPickerBox, _crimialColorPickerBox, _genericColorPickerBox, _enemyColorPickerBox, _murdererColorPickerBox;
        private HSliderBar _lightBar;

        private MacroControl _macroControl;
        private Checkbox _queryBeforAttackCheckbox;
        private Checkbox _restorezoomCheckbox, _savezoomCheckbox, _zoomCheckbox;
        private TextBox _rows, _columns;

        // speech
        private Checkbox _scaleSpeechDelay;
        private Combobox _shardType, _auraType;
        // general
        private HSliderBar _sliderFPS, _sliderFPSLogin, _circleOfTranspRadius;
        private HSliderBar _sliderSpeechDelay;
        private HSliderBar _soundsVolume, _musicVolume, _loginMusicVolume;
        private ColorBox _speechColorPickerBox, _emoteColorPickerBox, _partyMessageColorPickerBox, _guildMessageColorPickerBox, _allyMessageColorPickerBox;

        private ScrollAreaItem _windowSizeArea;
        private ScrollAreaItem _zoomSizeArea;

        public OptionsGump() : base(0, 0)
        {
            Add(new AlphaBlendControl(0.05f)
            {
                X = 1,
                Y = 1,
                Width = WIDTH - 2,
                Height = HEIGHT - 2
            });

            Stream stream = typeof(Engine).Assembly.GetManifestResourceStream("ClassicUO.cuologo.png");
            Texture2D.TextureDataFromStreamEXT(stream, out int w, out int h, out byte[] pixels, 350, 365);

            TextureControl tc = new TextureControl
            {
                X = 150 + (WIDTH - 150 - 350) / 2,
                Y = (HEIGHT - 365) / 2,
                Width = w,
                Height = h,
                Alpha = 0.95f,
                IsTransparent = true,
                ScaleTexture = false,
                Texture = new SpriteTexture(w, h)
            };

            tc.Texture.SetData(pixels);
            Add(tc);

            Add(new NiceButton(10, 10, 140, 25, ButtonAction.SwitchPage, "General") {IsSelected = true, ButtonParameter = 1});
            Add(new NiceButton(10, 10 + 30 * 1, 140, 25, ButtonAction.SwitchPage, "Sounds") {ButtonParameter = 2});
            Add(new NiceButton(10, 10 + 30 * 2, 140, 25, ButtonAction.SwitchPage, "Video") {ButtonParameter = 3});
            Add(new NiceButton(10, 10 + 30 * 3, 140, 25, ButtonAction.SwitchPage, "Macro") {ButtonParameter = 4});
            Add(new NiceButton(10, 10 + 30 * 4, 140, 25, ButtonAction.SwitchPage, "Tooltip") {ButtonParameter = 5});
            Add(new NiceButton(10, 10 + 30 * 5, 140, 25, ButtonAction.SwitchPage, "Fonts") {ButtonParameter = 6});
            Add(new NiceButton(10, 10 + 30 * 6, 140, 25, ButtonAction.SwitchPage, "Speech") {ButtonParameter = 7});
            Add(new NiceButton(10, 10 + 30 * 7, 140, 25, ButtonAction.SwitchPage, "Combat") {ButtonParameter = 8});
            Add(new NiceButton(10, 10 + 30 * 8, 140, 25, ButtonAction.SwitchPage, "Counters") { ButtonParameter = 9 });
            Add(new NiceButton(10, 10 + 30 * 9, 140, 25, ButtonAction.SwitchPage, "Experimental") { ButtonParameter = 10 });

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

            ChangePage(1);
        }

        private void BuildGeneral()
        {
            const int PAGE = 1;
            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            // FPS
            ScrollAreaItem fpsItem = new ScrollAreaItem();
            Label text = new Label("- FPS:", true, HUE_FONT, font: FONT);
            fpsItem.Add(text);
            _sliderFPS = new HSliderBar(80, 5, 250, 15, 250, Engine.Profile.Current.MaxFPS, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT, true);
            fpsItem.Add(_sliderFPS);
            rightArea.Add(fpsItem);

            fpsItem = new ScrollAreaItem();
            text = new Label("- Login FPS:", true, HUE_FONT, font: FONT);
            fpsItem.Add(text);
            _sliderFPSLogin = new HSliderBar(80, 5, 250, 15, 250, Engine.GlobalSettings.MaxLoginFPS, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT, true);
            fpsItem.Add(_sliderFPSLogin);
            rightArea.Add(fpsItem);

            // Highlight    
            _highlightObjects = CreateCheckBox(rightArea, "Highlight game objects", Engine.Profile.Current.HighlightGameObjects, 0, 10);

            _enablePathfind = CreateCheckBox(rightArea, "Enable pathfinding", Engine.Profile.Current.EnablePathfind, 0, 0);
            _alwaysRun = CreateCheckBox(rightArea, "Always run", Engine.Profile.Current.AlwaysRun, 0, 0);
            _preloadMaps = CreateCheckBox(rightArea, "Preload maps (it increases the RAM usage)", Engine.GlobalSettings.PreloadMaps, 0, 0);
            _enableTopbar = CreateCheckBox(rightArea, "Disable the Menu Bar", Engine.Profile.Current.TopbarGumpIsDisabled, 0, 0);
            _holdDownKeyTab = CreateCheckBox(rightArea, "Hold TAB key for combat", Engine.Profile.Current.HoldDownKeyTab, 0, 0);
            _holdDownKeyAlt = CreateCheckBox(rightArea, "Hold ALT key + right click to close Anchored gumps", Engine.Profile.Current.HoldDownKeyAltToCloseAnchored, 0, 0);

            ScrollAreaItem hpAreaItem = new ScrollAreaItem();

            _showHpMobile = new Checkbox(0x00D2, 0x00D3, "Show HP", FONT, HUE_FONT, true)
            {
                X = 0, Y = 10, IsChecked = Engine.Profile.Current.ShowMobilesHP
            };
            hpAreaItem.Add(_showHpMobile);
            int mode = Engine.Profile.Current.MobileHPType;

            if (mode < 0 || mode > 2)
                mode = 0;

            _hpComboBox = new Combobox(_showHpMobile.Bounds.Right + 10, 10, 150, new[]
            {
                "Percentage", "Line", "Both"
            }, mode);
            hpAreaItem.Add(_hpComboBox);

            mode = Engine.Profile.Current.CloseHealthBarType;

            if (mode < 0 || mode > 2)
                mode = 0;

            text = new Label("Close healthbar gump when:", true, HUE_FONT, font: FONT)
            {
                Y = _hpComboBox.Bounds.Bottom + 20
            };
            hpAreaItem.Add(text);

            _healtbarType = new Combobox(text.Bounds.Right + 10, _hpComboBox.Bounds.Bottom + 20, 150, new[]
            {
                "None", "Mobile Out of Range", "Mobile is Dead"
            }, mode);
            hpAreaItem.Add(_healtbarType);

            rightArea.Add(hpAreaItem);

            // highlight character by flags

            ScrollAreaItem highlightByFlagsItem = new ScrollAreaItem();

            text = new Label("- Mobile Status", true, HUE_FONT, font: FONT)
            {
                Y = 10
            };
            highlightByFlagsItem.Add(text);

            _highlightByState = new Checkbox(0x00D2, 0x00D3, "Highlight by state\n(poisoned, yellow hits, paralyzed)", FONT, HUE_FONT, true)
            {
                X = 25, Y = 30, IsChecked = Engine.Profile.Current.HighlightMobilesByFlags
            };
            highlightByFlagsItem.Add(_highlightByState);
            rightArea.Add(highlightByFlagsItem);


            _drawRoofs = CreateCheckBox(rightArea, "Hide roof tiles", !Engine.Profile.Current.DrawRoofs, 0, 20);
            _treeToStumps = CreateCheckBox(rightArea, "Tree to stumps", Engine.Profile.Current.TreeToStumps, 0, 0);
            _hideVegetation = CreateCheckBox(rightArea, "Hide vegetation", Engine.Profile.Current.HideVegetation, 0, 0);
            _enableCaveBorder = CreateCheckBox(rightArea, "Mark cave tiles", Engine.Profile.Current.EnableCaveBorder, 0, 0);

            hpAreaItem = new ScrollAreaItem();

            text = new Label("- Fields: ", true, HUE_FONT, font: FONT)
            {
                Y = 10
            };
            hpAreaItem.Add(text);

            _normalFields = new RadioButton(0, 0x00D0, 0x00D1, "Normal fields", FONT, HUE_FONT, true)
            {
                X = 25,
                Y = 30,
                IsChecked = Engine.Profile.Current.FieldsType == 0
            };
            hpAreaItem.Add(_normalFields);

            _staticFields = new RadioButton(0, 0x00D0, 0x00D1, "Static fields", FONT, HUE_FONT, true)
            {
                X = 25,
                Y = 30 + _normalFields.Height,
                IsChecked = Engine.Profile.Current.FieldsType == 1
            };
            hpAreaItem.Add(_staticFields);

            _fieldsToTile = new RadioButton(0, 0x00D0, 0x00D1, "Tile fields", FONT, HUE_FONT, true)
            {
                X = 25,
                Y = 30 + _normalFields.Height * 2,
                IsChecked = Engine.Profile.Current.FieldsType == 2
            };
            hpAreaItem.Add(_fieldsToTile);

            rightArea.Add(hpAreaItem);

            _noColorOutOfRangeObjects = CreateCheckBox(rightArea, "No color for object out of range", Engine.Profile.Current.NoColorObjectsOutOfRange, 0, 0);

            hpAreaItem = new ScrollAreaItem();

            text = new Label("- Circle of Transparency:", true, HUE_FONT, font: FONT)
            {
                Y = 10
            };
            hpAreaItem.Add(text);

            _circleOfTranspRadius = new HSliderBar(160, 15, 100, Constants.MIN_CIRCLE_OF_TRANSPARENCY_RADIUS, Constants.MAX_CIRCLE_OF_TRANSPARENCY_RADIUS, Engine.Profile.Current.CircleOfTransparencyRadius, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT, true);
            hpAreaItem.Add(_circleOfTranspRadius);

            _useCircleOfTransparency = new Checkbox(0x00D2, 0x00D3, "Enable circle of transparency", FONT, HUE_FONT, true)
            {
                X = 25,
                Y = 30,
                IsChecked = Engine.Profile.Current.UseCircleOfTransparency
            };
            hpAreaItem.Add(_useCircleOfTransparency);

            rightArea.Add(hpAreaItem);
            Add(rightArea, PAGE);
        }

        private void BuildSounds()
        {
            const int PAGE = 2;

            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            ScrollAreaItem item = new ScrollAreaItem();

            _enableSounds = new Checkbox(0x00D2, 0x00D3, "Sounds", FONT, HUE_FONT, true)
            {
                IsChecked = Engine.Profile.Current.EnableSound
            };
            _enableSounds.ValueChanged += (sender, e) => { _soundsVolume.IsVisible = _enableSounds.IsChecked; };
            item.Add(_enableSounds);
            _soundsVolume = new HSliderBar(90, 0, 180, 0, 100, Engine.Profile.Current.SoundVolume, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT, true);

            item.Add(_soundsVolume);
            rightArea.Add(item);

            item = new ScrollAreaItem();

            _enableMusic = new Checkbox(0x00D2, 0x00D3, "Music", FONT, HUE_FONT, true)
            {
                IsChecked = Engine.Profile.Current.EnableMusic
            };
            _enableMusic.ValueChanged += (sender, e) => { _musicVolume.IsVisible = _enableMusic.IsChecked; };
            item.Add(_enableMusic);

            _musicVolume = new HSliderBar(90, 0, 180, 0, 100, Engine.Profile.Current.MusicVolume, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT, true);

            item.Add(_musicVolume);
            rightArea.Add(item);


            _footStepsSound = CreateCheckBox(rightArea, "Play Footsteps", Engine.Profile.Current.EnableFootstepsSound, 0, 30);
            _combatMusic = CreateCheckBox(rightArea, "Combat music", Engine.Profile.Current.EnableCombatMusic, 0, 0);
            _musicInBackground = CreateCheckBox(rightArea, "Reproduce music when ClassicUO is not focused", Engine.Profile.Current.ReproduceSoundsInBackground, 0, 0);

            _loginMusic = CreateCheckBox(rightArea, "Login music", Engine.GlobalSettings.LoginMusic, 0, 40);

            item = new ScrollAreaItem();
            Label text = new Label("- Login music volume:", true, HUE_FONT, 0, FONT);
            _loginMusicVolume = new HSliderBar(150, 5, 180, 0, 100, Engine.GlobalSettings.LoginMusicVolume, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT, true);
            item.Add(text);
            item.Add(_loginMusicVolume);
            rightArea.Add(item);

            _soundsVolume.IsVisible = _enableSounds.IsChecked;
            _musicVolume.IsVisible = _enableMusic.IsChecked;

            Add(rightArea, PAGE);
        }

        private void BuildVideo()
        {
            const int PAGE = 3;

            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);
            Label text;

            _debugControls = CreateCheckBox(rightArea, "Debugging mode", Engine.GlobalSettings.Debug, 0, 0);

            // [BLOCK] game size
            {
                _gameWindowFullsize = new Checkbox(0x00D2, 0x00D3, "Always use fullsize game window", FONT, HUE_FONT, true)
                {
                    IsChecked = Engine.Profile.Current.GameWindowFullSize
                };
                _gameWindowFullsize.ValueChanged += (sender, e) => { _windowSizeArea.IsVisible = !_gameWindowFullsize.IsChecked; };

                rightArea.Add(_gameWindowFullsize);

                _windowSizeArea = new ScrollAreaItem();

                _gameWindowLock = new Checkbox(0x00D2, 0x00D3, "Lock game window moving/resizing", FONT, HUE_FONT, true)
                {
                    X = 20,
                    Y = 15,
                    IsChecked = Engine.Profile.Current.GameWindowLock
                };

                _windowSizeArea.Add(_gameWindowLock);

                text = new Label("Game Play Window Size: ", true, HUE_FONT, 0, FONT)
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

                text = new Label("Game Play Window Position: ", true, HUE_FONT, 0, FONT)
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
                _zoomCheckbox = new Checkbox(0x00D2, 0x00D3, "Enable in game zoom scaling (Ctrl + Scroll)", FONT, HUE_FONT, true)
                {
                    IsChecked = Engine.Profile.Current.EnableScaleZoom
                };
                _zoomCheckbox.ValueChanged += (sender, e) => { _zoomSizeArea.IsVisible = _zoomCheckbox.IsChecked; };

                rightArea.Add(_zoomCheckbox);

                _zoomSizeArea = new ScrollAreaItem();

                _savezoomCheckbox = new Checkbox(0x00D2, 0x00D3, "Save scale after exit", FONT, HUE_FONT, true)
                {
                    X = 20,
                    Y = 15,
                    IsChecked = Engine.Profile.Current.SaveScaleAfterClose
                };
                _zoomSizeArea.Add(_savezoomCheckbox);

                _restorezoomCheckbox = new Checkbox(0x00D2, 0x00D3, "Releasing Ctrl Restores Scale", FONT, HUE_FONT, true)
                {
                    X = 20,
                    Y = 35,
                    IsChecked = Engine.Profile.Current.RestoreScaleAfterUnpressCtrl
                };
                _zoomSizeArea.Add(_restorezoomCheckbox);

                rightArea.Add(_zoomSizeArea);
                _zoomSizeArea.IsVisible = _zoomCheckbox.IsChecked;
            }

            _enableDeathScreen = CreateCheckBox(rightArea, "Enable Death Screen", Engine.Profile.Current.EnableDeathScreen, 0, 10);
            _enableBlackWhiteEffect = CreateCheckBox(rightArea, "Black & White mode for dead player", Engine.Profile.Current.EnableBlackWhiteEffect, 0, 0);

            ScrollAreaItem item = new ScrollAreaItem();

            text = new Label("- Status gump type:", true, HUE_FONT, 0, FONT)
            {
                Y = 30
            };

            item.Add(text);

            _shardType = new Combobox(text.Width + 20, text.Y, 100, new[] {"Modern", "Old", "Outlands"})
            {
                SelectedIndex = Engine.GlobalSettings.ShardType
            };
            item.Add(_shardType);
            rightArea.Add(item);

            item = new ScrollAreaItem();

            _enableLight = new Checkbox(0x00D2, 0x00D3, "Light level", FONT, HUE_FONT, true)
            {
                Y = 20,
                IsChecked = Engine.Profile.Current.UseCustomLightLevel
            };
            _lightBar = new HSliderBar(_enableLight.Width + 10, 20, 250, 0, 0x1E, 0x1E - Engine.Profile.Current.LightLevel, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT, true);

            item.Add(_enableLight);
            item.Add(_lightBar);
            rightArea.Add(item);


            _enableShadows = new Checkbox(0x00D2, 0x00D3, "Shadows", FONT, HUE_FONT, true)
            {
                IsChecked = Engine.Profile.Current.ShadowsEnabled
            };
            rightArea.Add(_enableShadows);


            item = new ScrollAreaItem();

            text = new Label("- Aura under feet:", true, HUE_FONT, 0, FONT)
            {
                Y = 10
            };
            item.Add(text);

            _auraType = new Combobox(text.Width + 20, text.Y, 100, new[] {"None", "Warmode", "Ctrl+Shift", "Always"})
            {
                SelectedIndex = Engine.Profile.Current.AuraUnderFeetType
            };
            item.Add(_auraType);
            rightArea.Add(item);


            Add(rightArea, PAGE);
        }

        private void BuildCommands()
        {
            const int PAGE = 4;

            ScrollArea rightArea = new ScrollArea(190, 52 + 25 + 4, 150, 360, true);
            NiceButton addButton = new NiceButton(190, 20, 130, 20, ButtonAction.Activate, "New macro") {IsSelectable = false, ButtonParameter = (int) Buttons.NewMacro};

            addButton.MouseClick += (sender, e) =>
            {
                EntryDialog dialog = new EntryDialog(250, 150, "Macro name:", name =>
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

                    nb.MouseClick += (sss, eee) =>
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

            NiceButton delButton = new NiceButton(190, 52, 130, 20, ButtonAction.Activate, "Delete macro") {IsSelectable = false, ButtonParameter = (int) Buttons.DeleteMacro};

            delButton.MouseClick += (ss, ee) =>
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

                nb.MouseClick += (sss, eee) =>
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
            Label text = new Label("Speech font:", true, HUE_FONT, 0, FONT);

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

            _scaleSpeechDelay = new Checkbox(0x00D2, 0x00D3, "Scale speech delay by length", FONT, HUE_FONT, true)
            {
                IsChecked = Engine.Profile.Current.ScaleSpeechDelay
            };
            _scaleSpeechDelay.ValueChanged += (sender, e) => { _sliderSpeechDelay.IsVisible = !_sliderSpeechDelay.IsVisible; };
            rightArea.Add(_scaleSpeechDelay);

            _sliderSpeechDelay = new HSliderBar(0, 0, 300, 1, 1000, Engine.Profile.Current.SpeechDelay, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT, true);
            rightArea.Add(_sliderSpeechDelay);

            // [BLOCK] activate chat
            {
                _chatAfterEnter = new Checkbox(0x00D2, 0x00D3, "Activate chat after `Enter` pressing", FONT, HUE_FONT, true)
                {
                    Y = 15,
                    IsChecked = Engine.Profile.Current.ActivateChatAfterEnter
                };
                _chatAfterEnter.ValueChanged += (sender, e) => { _activeChatArea.IsVisible = _chatAfterEnter.IsChecked; };

                rightArea.Add(_chatAfterEnter);

                _activeChatArea = new ScrollAreaItem();

                _chatAdditionalButtonsCheckbox = new Checkbox(0x00D2, 0x00D3, "Additional buttons activate chat: ! ; : ? / \\ , . [ | -", FONT, HUE_FONT, true)
                {
                    X = 20,
                    Y = 15,
                    IsChecked = Engine.Profile.Current.ActivateChatAdditionalButtons
                };
                _activeChatArea.Add(_chatAdditionalButtonsCheckbox);

                _chatShiftEnterCheckbox = new Checkbox(0x00D2, 0x00D3, "Shift+Enter send message without closing chat", FONT, HUE_FONT, true)
                {
                    X = 20,
                    Y = 35,
                    IsChecked = Engine.Profile.Current.ActivateChatShiftEnterSupport
                };
                _activeChatArea.Add(_chatShiftEnterCheckbox);

                var text = new Label("If chat active - ignores hotkeys from:", true, HUE_FONT, 0, FONT)
                {
                    X = 20,
                    Y = 60
                };

                _activeChatArea.Add(text);

                _chatIgnodeHotkeysCheckbox = new Checkbox(0x00D2, 0x00D3, "Client (macro system)", FONT, HUE_FONT, true)
                {
                    X = 40,
                    Y = 85,
                    IsChecked = Engine.Profile.Current.ActivateChatIgnoreHotkeys
                };
                _activeChatArea.Add(_chatIgnodeHotkeysCheckbox);

                _chatIgnodeHotkeysPluginsCheckbox = new Checkbox(0x00D2, 0x00D3, "Plugins (Razor)", FONT, HUE_FONT, true)
                {
                    X = 40,
                    Y = 105,
                    IsChecked = Engine.Profile.Current.ActivateChatIgnoreHotkeysPlugins
                };
                _activeChatArea.Add(_chatIgnodeHotkeysPluginsCheckbox);

                rightArea.Add(_activeChatArea);

                _activeChatArea.IsVisible = _chatAfterEnter.IsChecked;
            }

            _speechColorPickerBox = CreateClickableColorBox(rightArea, 0, 20, Engine.Profile.Current.SpeechHue, "Speech Color", 20, 20);
            _emoteColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.EmoteHue, "Emote Color", 20, 0);
            _partyMessageColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.PartyMessageHue, "Party Message Color", 20, 0);
            _guildMessageColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.GuildMessageHue, "Guild Message Color", 20, 0);
            _allyMessageColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.AllyMessageHue, "Alliance Message Color", 20, 0);

            _sliderSpeechDelay.IsVisible = _scaleSpeechDelay.IsChecked;

            Add(rightArea, PAGE);
        }

        private void BuildCombat()
        {
            const int PAGE = 8;
            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);
            _innocentColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.InnocentHue, "Innocent Color", 20, 0);
            _friendColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.FriendHue, "Friend Color", 20, 0);
            _crimialColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.CriminalHue, "Criminal Color", 20, 0);
            _genericColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.AnimalHue, "Animal Color", 20, 0);
            _murdererColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.MurdererHue, "Murderer Color", 20, 0);
            _enemyColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.EnemyHue, "Enemy Color", 20, 0);

            _queryBeforAttackCheckbox = CreateCheckBox(rightArea, "Query before attack", Engine.Profile.Current.EnabledCriminalActionQuery, 0, 30);

            Add(rightArea, PAGE);
        }

        private void BuildCounters()
        {
            const int PAGE = 9;
            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            _enableCounters = CreateCheckBox(rightArea, "Enable Counters", Engine.Profile.Current.CounterBarEnabled, 0, 0);
            _highlightOnUse = CreateCheckBox(rightArea, "Highlight On Use", Engine.Profile.Current.CounterBarHighlightOnUse, 0, 0);


            ScrollAreaItem item = new ScrollAreaItem();

            Label text = new Label("Counter Layout:", true, HUE_FONT, font: FONT)
            {
                Y = _highlightOnUse.Bounds.Bottom + 5
            };
            item.Add(text);
            //_counterLayout = new Combobox(text.Bounds.Right + 10, _highlightOnUse.Bounds.Bottom + 5, 150, new[] { "Horizontal", "Vertical" }, Engine.Profile.Current.CounterBarIsVertical ? 1 : 0);
            //item.Add(_counterLayout);
            rightArea.Add(item);


            item = new ScrollAreaItem();

            text = new Label("Cell size:", true, HUE_FONT, font: FONT)
            {
                X = 10,
                Y = 10
            };
            item.Add(text);

            _cellSize = new HSliderBar(text.X + text.Width + 10, text.Y + 5, 80, 30, 80, Engine.Profile.Current.CounterBarCellSize, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT, true);
            item.Add(_cellSize);
            rightArea.Add(item);

            item = new ScrollAreaItem();

            _rows = CreateInputField(item, new TextBox(FONT, 5, 80, 80, true)
            {
                X = 20,
                Y = _cellSize.Y + _cellSize.Height + 25,
                Width = 50,
                Height = 30,
                NumericOnly = true,
                Text = Engine.Profile.Current.CounterBarRows.ToString()
            }, "Rows:");

            _columns = CreateInputField(item, new TextBox(FONT, 5, 80, 80, true)
            {
                X = _rows.X + _rows.Width + 30,
                Y = _cellSize.Y + _cellSize.Height + 25,
                Width = 50,
                Height = 30,
                NumericOnly = true,
                Text = Engine.Profile.Current.CounterBarColumns.ToString()
            }, "Columns:");

            rightArea.Add(item);

            Add(rightArea, PAGE);
        }

        private void BuildExperimental()
        {
            const int PAGE = 10;
            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);

            _enableSelectionArea = CreateCheckBox(rightArea, "Enable Selection Area", Engine.Profile.Current.EnableSelectionArea, 0, 0);

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

                //case Buttons.SpeechColor: break;
                //case Buttons.EmoteColor: break;
                //case Buttons.PartyMessageColor: break;
                //case Buttons.GuildMessageColor: break;
                //case Buttons.AllyMessageColor: break;
                //case Buttons.InnocentColor: break;
                //case Buttons.FriendColor: break;
                //case Buttons.CriminalColor: break;
                //case Buttons.EnemyColor: break;
                //case Buttons.MurdererColor: break;
            }
        }

        private void SetDefault()
        {
            switch (ActivePage)
            {
                case 1: // general
                    _sliderFPS.Value = 60;
                    _sliderFPSLogin.Value = 60;
                    _highlightObjects.IsChecked = true;
                    _enableTopbar.IsChecked = false;
                    _holdDownKeyTab.IsChecked = true;
                    _holdDownKeyAlt.IsChecked = true;

                    //_smoothMovements.IsChecked = true;
                    _enablePathfind.IsChecked = false;
                    _alwaysRun.IsChecked = false;
                    _showHpMobile.IsChecked = false;
                    _hpComboBox.SelectedIndex = 0;
                    _highlightByState.IsChecked = true;
                    _drawRoofs.IsChecked = true;
                    _enableCaveBorder.IsChecked = false;
                    _treeToStumps.IsChecked = false;
                    _hideVegetation.IsChecked = false;
                    _normalFields.IsChecked = true;
                    _staticFields.IsChecked = false;
                    _fieldsToTile.IsChecked = false;
                    _noColorOutOfRangeObjects.IsChecked = false;
                    _circleOfTranspRadius.Value = 5;
                    _useCircleOfTransparency.IsChecked = false;
                    _preloadMaps.IsChecked = false;
                    _healtbarType.SelectedIndex = 0;

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

                    _enableShadows.IsChecked = true;
                    _auraType.SelectedIndex = 0;

                    _windowSizeArea.IsVisible = !_gameWindowFullsize.IsChecked;
                    _zoomSizeArea.IsVisible = _zoomCheckbox.IsChecked;

                    break;
                case 4: // commands

                    break;
                case 5: // tooltip

                    break;
                case 6: // fonts
                    _fontSelectorChat.SetSelectedFont(0);

                    break;
                case 7: // speech
                    _scaleSpeechDelay.IsChecked = true;
                    _sliderSpeechDelay.Value = 100;
                    _speechColorPickerBox.SetColor(0x02B2, FileManager.Hues.GetPolygoneColor(12, 0x02B2));
                    _emoteColorPickerBox.SetColor(0x0021, FileManager.Hues.GetPolygoneColor(12, 0x0021));
                    _partyMessageColorPickerBox.SetColor(0x0044, FileManager.Hues.GetPolygoneColor(12, 0x0044));
                    _guildMessageColorPickerBox.SetColor(0x0044, FileManager.Hues.GetPolygoneColor(12, 0x0044));
                    _allyMessageColorPickerBox.SetColor(0x0057, FileManager.Hues.GetPolygoneColor(12, 0x0057));

                    _chatAfterEnter.IsChecked = false;

                    Engine.UI.SystemChat.IsActive = !_chatAfterEnter.IsChecked;

                    _chatIgnodeHotkeysCheckbox.IsChecked = true;
                    _chatIgnodeHotkeysPluginsCheckbox.IsChecked = true;
                    _chatAdditionalButtonsCheckbox.IsChecked = true;
                    _chatShiftEnterCheckbox.IsChecked = true;
                    _activeChatArea.IsVisible = _chatAfterEnter.IsChecked;

                    break;
                case 8: // combat
                    _innocentColorPickerBox.SetColor(0x005A, FileManager.Hues.GetPolygoneColor(12, 0x005A));
                    _friendColorPickerBox.SetColor(0x0044, FileManager.Hues.GetPolygoneColor(12, 0x0044));
                    _crimialColorPickerBox.SetColor(0x03B2, FileManager.Hues.GetPolygoneColor(12, 0x03B2));
                    _genericColorPickerBox.SetColor(0x03B2, FileManager.Hues.GetPolygoneColor(12, 0x03B2));
                    _murdererColorPickerBox.SetColor(0x0023, FileManager.Hues.GetPolygoneColor(12, 0x0023));
                    _enemyColorPickerBox.SetColor(0x0031, FileManager.Hues.GetPolygoneColor(12, 0x0031));
                    _queryBeforAttackCheckbox.IsChecked = true;

                    break;
                case 9:
                    _enableCounters.IsChecked = false;
                    _highlightOnUse.IsChecked = false;
                    _columns.Text = "1";
                    _rows.Text = "1";
                    _cellSize.Value = 40;

                    break;

                case 10:
                    _enableSelectionArea.IsChecked = false;

                    break;
            }
        }

        private void Apply()
        {
            WorldViewportGump vp = Engine.UI.GetByLocalSerial<WorldViewportGump>();

            // general
            Engine.GlobalSettings.PreloadMaps = _preloadMaps.IsChecked;
            Engine.Profile.Current.MaxFPS = Engine.FpsLimit = _sliderFPS.Value;
            Engine.GlobalSettings.MaxLoginFPS = _sliderFPSLogin.Value;
            Engine.Profile.Current.HighlightGameObjects = _highlightObjects.IsChecked;
            //Engine.Profile.Current.SmoothMovements = _smoothMovements.IsChecked;
            Engine.Profile.Current.EnablePathfind = _enablePathfind.IsChecked;
            Engine.Profile.Current.AlwaysRun = _alwaysRun.IsChecked;
            Engine.Profile.Current.ShowMobilesHP = _showHpMobile.IsChecked;
            Engine.Profile.Current.HighlightMobilesByFlags = _highlightByState.IsChecked;
            Engine.Profile.Current.MobileHPType = _hpComboBox.SelectedIndex;
            Engine.Profile.Current.HoldDownKeyTab = _holdDownKeyTab.IsChecked;
            Engine.Profile.Current.HoldDownKeyAltToCloseAnchored = _holdDownKeyAlt.IsChecked;
            Engine.Profile.Current.CloseHealthBarType = _healtbarType.SelectedIndex;

            if (Engine.Profile.Current.DrawRoofs == _drawRoofs.IsChecked)
            {
                Engine.Profile.Current.DrawRoofs = !_drawRoofs.IsChecked;
                Engine.SceneManager.GetScene<GameScene>()?.UpdateMaxDrawZ(true);
            }

            if (Engine.Profile.Current.TopbarGumpIsDisabled != _enableTopbar.IsChecked)
            {
                if (_enableTopbar.IsChecked)
                    Engine.UI.GetByLocalSerial<TopBarGump>()?.Dispose();
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
            Engine.Profile.Current.FieldsType = _normalFields.IsChecked ? 0 : _staticFields.IsChecked ? 1 : _fieldsToTile.IsChecked ? 2 : 0;
            Engine.Profile.Current.HideVegetation = _hideVegetation.IsChecked;
            Engine.Profile.Current.NoColorObjectsOutOfRange = _noColorOutOfRangeObjects.IsChecked;
            Engine.Profile.Current.UseCircleOfTransparency = _useCircleOfTransparency.IsChecked;
            Engine.Profile.Current.CircleOfTransparencyRadius = _circleOfTranspRadius.Value;

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
            Engine.Profile.Current.PartyMessageHue = _partyMessageColorPickerBox.Hue;
            Engine.Profile.Current.GuildMessageHue = _guildMessageColorPickerBox.Hue;
            Engine.Profile.Current.AllyMessageHue = _allyMessageColorPickerBox.Hue;

            if (Engine.Profile.Current.ActivateChatAfterEnter != _chatAfterEnter.IsChecked)
            {
                Engine.UI.SystemChat.IsActive = !_chatAfterEnter.IsChecked;
                Engine.Profile.Current.ActivateChatAfterEnter = _chatAfterEnter.IsChecked;
            }

            Engine.Profile.Current.ActivateChatIgnoreHotkeys = _chatIgnodeHotkeysCheckbox.IsChecked;
            Engine.Profile.Current.ActivateChatIgnoreHotkeysPlugins = _chatIgnodeHotkeysPluginsCheckbox.IsChecked;
            Engine.Profile.Current.ActivateChatAdditionalButtons = _chatAdditionalButtonsCheckbox.IsChecked;
            Engine.Profile.Current.ActivateChatShiftEnterSupport = _chatShiftEnterCheckbox.IsChecked;

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
                if (vp != null)
                    vp.Location = Engine.Profile.Current.GameWindowPosition = new Point(gameWindowPositionX, gameWindowPositionY);

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
                World.Light.Overall = Engine.Profile.Current.LightLevel;
            else
            {
                World.Light.Overall = World.Light.RealOverall;
                World.Light.Personal = World.Light.RealPersonal;
            }


            Engine.Profile.Current.ShadowsEnabled = _enableShadows.IsChecked;
            Engine.Profile.Current.AuraUnderFeetType = _auraType.SelectedIndex;


            // fonts
            var _fontValue = _fontSelectorChat.GetSelectedFont();

            if (Engine.Profile.Current.ChatFont != _fontValue)
            {
                Engine.Profile.Current.ChatFont = _fontValue;
                WorldViewportGump viewport = Engine.UI.GetByLocalSerial<WorldViewportGump>();
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

            // macros
            Engine.Profile.Current.Macros = Engine.SceneManager.GetScene<GameScene>().Macros.GetAllMacros().ToArray();


            // counters

            bool before = Engine.Profile.Current.CounterBarEnabled;
            Engine.Profile.Current.CounterBarEnabled = _enableCounters.IsChecked;
            Engine.Profile.Current.CounterBarCellSize = _cellSize.Value;
            Engine.Profile.Current.CounterBarRows = int.Parse(_rows.Text);
            Engine.Profile.Current.CounterBarColumns = int.Parse(_columns.Text);
            Engine.Profile.Current.CounterBarHighlightOnUse = _highlightOnUse.IsChecked;


            CounterBarGump counterGump = Engine.UI.GetByLocalSerial<CounterBarGump>();

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

            Engine.Profile.Current?.Save(Engine.UI.Gumps.OfType<Gump>().Where(s => s.CanBeSaved).Reverse().ToList());
        }

        internal void UpdateVideo()
        {
            WorldViewportGump gump = Engine.UI.GetByLocalSerial<WorldViewportGump>();
            _gameWindowWidth.Text = gump.Width.ToString();
            _gameWindowHeight.Text = gump.Height.ToString();
            _gameWindowPositionX.Text = gump.X.ToString();
            _gameWindowPositionY.Text = gump.Y.ToString();
        }

        public override bool Draw(Batcher2D batcher, int x, int y)
        {
            batcher.DrawRectangle(Textures.GetTexture(Color.Gray), x, y, Width, Height, Vector3.Zero);

            return base.Draw(batcher, x, y);
        }

        private TextBox CreateInputField(ScrollAreaItem area, TextBox elem, string label = null)
        {
            area.Add(new ResizePic(0x0BB8)
            {
                X = elem.X - 10,
                Y = elem.Y - 5,
                Width = elem.Width + 10,
                Height = elem.Height - 7
            });

            area.Add(elem);

            if (label != null)
            {
                Label text = new Label(label, true, HUE_FONT, 0, FONT)
                {
                    X = elem.X - 10,
                    Y = elem.Y - 30
                };
                area.Add(text);
            }

            return elem;
        }

        private Checkbox CreateCheckBox(ScrollArea area, string text, bool ischecked, int x, int y)
        {
            Checkbox box = new Checkbox(0x00D2, 0x00D3, text, FONT, HUE_FONT, true)
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

            item.Add(new Label(text, true, HUE_FONT, font: FONT)
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

        private class ClickableColorBox : ColorBox
        {
            private const int CELL = 12;

            private readonly SpriteTexture _background;

            public ClickableColorBox(int x, int y, int w, int h, ushort hue, uint color) : base(w, h, hue, color)
            {
                X = x + 3;
                Y = y + 3;
                WantUpdateSize = false;

                _background = FileManager.Gumps.GetTexture(0x00D4);
            }

            public override void Update(double totalMS, double frameMS)
            {
                _background.Ticks = (long) totalMS;

                base.Update(totalMS, frameMS);
            }

            public override bool Draw(Batcher2D batcher, int x, int y)
            {
                batcher.Draw2D(_background, x - 3, y - 3, Vector3.Zero);

                return base.Draw(batcher, x, y);
            }

            protected override void OnMouseClick(int x, int y, MouseButton button)
            {
                if (button == MouseButton.Left)
                {
                    ColorPickerGump pickerGump = new ColorPickerGump(0, 0, 100, 100, s => SetColor(s, FileManager.Hues.GetPolygoneColor(CELL, s)));
                    Engine.UI.Add(pickerGump);
                }
            }
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
                        Add(_buttons[i] = new RadioButton(0, 0x00D0, 0x00D1, "That's ClassicUO!", i, HUE_FONT, true)
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