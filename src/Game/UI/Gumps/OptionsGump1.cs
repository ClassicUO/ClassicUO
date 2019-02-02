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
using System.Linq;
using System.ComponentModel.Design;

using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

using ClassicUO.Network;
using System.Diagnostics;
using System.IO;

using ClassicUO.Game.Managers;
using ClassicUO.IO.Resources;

using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Gumps
{
    internal class OptionsGump1 : Gump
    {

        // general
        private HSliderBar _sliderFPS, _sliderFPSLogin, _circleOfTranspRadius;
        private Checkbox _highlightObjects, /*_smoothMovements,*/ _enablePathfind, _alwaysRun, _preloadMaps, _showHpMobile, _highlightByState, _drawRoofs, _treeToStumps, _hideVegetation, _noColorOutOfRangeObjects, _useCircleOfTransparency, _enableTopbar, _holdDownKeyTab, _caveToTile;
        private Combobox _hpComboBox;
        private RadioButton _fieldsToTile, _staticFields, _normalFields;

        // sounds
        private Checkbox _enableSounds, _enableMusic, _footStepsSound, _combatMusic, _musicInBackground, _loginMusic;
        private HSliderBar _soundsVolume, _musicVolume, _loginMusicVolume;

        // speech
        private Checkbox _scaleSpeechDelay;
        private HSliderBar _sliderSpeechDelay;
        private ColorBox _speechColorPickerBox, _emoteColorPickerBox, _partyMessageColorPickerBox, _guildMessageColorPickerBox, _allyMessageColorPickerBox;

        // video
        private Checkbox _debugControls, _zoom, _savezoom;
        private Combobox _shardType;

        private Checkbox _gameWindowLock;
        private Checkbox _gameWindowFullsize;

        // GameWindowSize
        private TextBox _gameWindowWidth;
        private TextBox _gameWindowHeight;
        // GameWindowPosition
        private TextBox _gameWindowPositionX;
        private TextBox _gameWindowPositionY;

        // fonts
        private FontSelector _fontSelectorChat;

        // combat
        private ColorBox _innocentColorPickerBox, _friendColorPickerBox, _crimialColorPickerBox, _genericColorPickerBox, _enemyColorPickerBox, _murdererColorPickerBox;
        private Checkbox _queryBeforAttackCheckbox;

        private const byte FONT = 0xFF;
        private const ushort HUE_FONT = 999;

        const int WIDTH = 700;
        const int HEIGHT = 500;


        public OptionsGump1() : base(0, 0)
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

            TextureControl tc = new TextureControl()
            {
                X = 150 + (WIDTH - 150 - 350) / 2,
                Y = (HEIGHT - 365) / 2,
                Width = w,
                Height = h,
                Alpha = 0.95f,
                IsTransparent =  true,
                ScaleTexture = false
            };
            
            tc.Texture = new SpriteTexture(w, h);
            tc.Texture.SetData(pixels);
            Add(tc);

         
            Add(new NiceButton(10, 10, 140, 25, ButtonAction.SwitchPage, "Generals") { IsSelected = true, ToPage = 1 } );
            Add(new NiceButton(10, 10 + 30 * 1, 140, 25, ButtonAction.SwitchPage, "Sounds") { ToPage = 2 });
            Add(new NiceButton(10, 10 + 30 * 2, 140, 25, ButtonAction.SwitchPage, "Video") { ToPage = 3 });
            Add(new NiceButton(10, 10 + 30 * 3, 140, 25, ButtonAction.SwitchPage, "Macro") { ToPage = 4 });
            Add(new NiceButton(10, 10 + 30 * 4, 140, 25, ButtonAction.SwitchPage, "Tooltip") { ToPage = 5 });
            Add(new NiceButton(10, 10 + 30 * 5, 140, 25, ButtonAction.SwitchPage, "Fonts") { ToPage = 6 });
            Add(new NiceButton(10, 10 + 30 * 6, 140, 25, ButtonAction.SwitchPage, "Speech") { ToPage = 7 });
            Add(new NiceButton(10, 10 + 30 * 7, 140, 25, ButtonAction.SwitchPage, "Combat") { ToPage = 8 });


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

            //// smooth movements
            //_smoothMovements = new Checkbox(0x00D2, 0x00D3, "Smooth movements", 1)
            //{
            //    IsChecked = Engine.Profile.Current.SmoothMovements
            //};
            //rightArea.AddChildren(_smoothMovements);

            _enablePathfind = CreateCheckBox(rightArea, "Enable pathfinding", Engine.Profile.Current.EnablePathfind, 0, 0);
            _alwaysRun = CreateCheckBox(rightArea, "Always run", Engine.Profile.Current.AlwaysRun, 0, 0);
            _preloadMaps = CreateCheckBox(rightArea, "Preload maps (it increases the RAM usage)", Engine.GlobalSettings.PreloadMaps, 0, 0);
            _enableTopbar = CreateCheckBox(rightArea, "Disable the Menu Bar", Engine.Profile.Current.TopbarGumpIsDisabled, 0, 0);
            _holdDownKeyTab = CreateCheckBox(rightArea, "Hold down TAB key for combat", Engine.Profile.Current.HoldDownKeyTab, 0, 0);

            // show % hp mobile
            ScrollAreaItem hpAreaItem = new ScrollAreaItem();

            text = new Label("- Mobiles HP", true, HUE_FONT, font: FONT)
            {
                Y = 10
            };
            hpAreaItem.Add(text);

            _showHpMobile = new Checkbox(0x00D2, 0x00D3, "Show HP", FONT, HUE_FONT, true)
            {
                X = 25, Y = 30, IsChecked = Engine.Profile.Current.ShowMobilesHP
            };
            hpAreaItem.Add(_showHpMobile);
            int mode = Engine.Profile.Current.MobileHPType;

            if (mode < 0 || mode > 2)
                mode = 0;

            _hpComboBox = new Combobox(200, 30, 150, new[]
            {
                "Percentage", "Line", "Both"
            }, mode);
            hpAreaItem.Add(_hpComboBox);
            rightArea.Add(hpAreaItem);

            // highlight character by flags

            ScrollAreaItem highlightByFlagsItem = new ScrollAreaItem();

            text = new Label("- Mobiles status", true, HUE_FONT, font: FONT)
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


            _drawRoofs = CreateCheckBox(rightArea, "Draw roofs", Engine.Profile.Current.DrawRoofs, 0, 20);
            _treeToStumps = CreateCheckBox(rightArea, "Tree to stumps", Engine.Profile.Current.TreeToStumps, 0, 0);
            _caveToTile = CreateCheckBox(rightArea, "Cave to floor tile", Engine.Profile.Current.CaveToTile, 0, 0);
            _hideVegetation = CreateCheckBox(rightArea, "Hide vegetation", Engine.Profile.Current.HideVegetation, 0, 0);

            hpAreaItem = new ScrollAreaItem();
            text = new Label("- Fields: ", true, HUE_FONT, font: FONT)
            {
                Y = 10,
            };
            hpAreaItem.Add(text);


            _normalFields = new RadioButton(0, 0x00D0, 0x00D1, "Normal fields", FONT, HUE_FONT, true)
            {
                X = 25,
                Y = 30,
                IsChecked = Engine.Profile.Current.FieldsType == 0,
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
            _enableSounds = CreateCheckBox(rightArea, "Sounds", Engine.Profile.Current.EnableSound, 0, 0);

            ScrollAreaItem item = new ScrollAreaItem();
            Label text = new Label("- Sounds volume:", true, HUE_FONT, 0, FONT);

            _soundsVolume = new HSliderBar(150, 5, 180, 0, 100, Engine.Profile.Current.SoundVolume, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT, true);
            item.Add(text);
            item.Add(_soundsVolume);
            rightArea.Add(item);


            _enableMusic = CreateCheckBox(rightArea, "Music", Engine.Profile.Current.EnableMusic, 0, 0);

           
            item = new ScrollAreaItem();
            text = new Label("- Music volume:", true, HUE_FONT, 0, FONT);

            _musicVolume = new HSliderBar(150, 5, 180, 0, 100, Engine.Profile.Current.MusicVolume, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT, true);

            item.Add(text);
            item.Add(_musicVolume);
            rightArea.Add(item);

            _footStepsSound = CreateCheckBox(rightArea, "Footsteps sound", Engine.Profile.Current.EnableFootstepsSound, 0, 30);
            _combatMusic = CreateCheckBox(rightArea, "Combat music", Engine.Profile.Current.EnableCombatMusic, 0, 0);
            _musicInBackground = CreateCheckBox(rightArea, "Reproduce music when ClassicUO is not focused", Engine.Profile.Current.ReproduceSoundsInBackground, 0, 0);


            _loginMusic = CreateCheckBox(rightArea, "Login music", Engine.GlobalSettings.LoginMusic, 0, 40);

            item = new ScrollAreaItem();
            text = new Label("- Login music volume:", true, HUE_FONT, 0, FONT);
            _loginMusicVolume = new HSliderBar(150, 5, 180, 0, 100, Engine.GlobalSettings.LoginMusicVolume, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT, true);
            item.Add(text);
            item.Add(_loginMusicVolume);
            rightArea.Add(item);

            Add(rightArea, PAGE);
        }

        private void BuildVideo()
        {
            const int PAGE = 3;

            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);
            _debugControls = CreateCheckBox(rightArea, "Debugging mode", Engine.GlobalSettings.Debug, 0, 0);
            _zoom = CreateCheckBox(rightArea, "Enable in game zoom scaling", Engine.Profile.Current.EnableScaleZoom, 0, 0);
            _savezoom = CreateCheckBox(rightArea, "Save scale after close game", Engine.Profile.Current.SaveScaleAfterClose, 0, 0);
            _gameWindowFullsize = CreateCheckBox(rightArea, "Always use fullsize game window", Engine.Profile.Current.GameWindowFullSize, 0, 0);


            ScrollAreaItem item = new ScrollAreaItem();
            Label text = new Label("- Status gump type:", true, HUE_FONT, 0, FONT)
            {
                Y = 30
            };

            item.Add(text);

            _shardType = new Combobox(text.Width + 20, text.Y, 100, new[] { "Modern", "Old", "Outlands" })
            {
                SelectedIndex = Engine.GlobalSettings.ShardType
            };
            item.Add(_shardType);
            rightArea.Add(item);


            item = new ScrollAreaItem();

            _gameWindowWidth = CreateInputField(item, new TextBox(1, 5, 80, 80, false)
            {
                Text = Engine.Profile.Current.GameWindowSize.X.ToString(),
                X = 10,
                Y = 105,
                Width = 50,
                Height = 30,
                NumericOnly = true
            }, "Game Play Window Size: ");

            _gameWindowHeight = CreateInputField(item, new TextBox(1, 5, 80, 80, false)
            {
                Text = Engine.Profile.Current.GameWindowSize.Y.ToString(),
                X = 80,
                Y = 105,
                Width = 50,
                Height = 30,
                NumericOnly = true
            });

            _gameWindowLock = new Checkbox(0x00D2, 0x00D3, "Lock game window moving/resizing", FONT, HUE_FONT, true)
            {
                X = 140,
                Y = 100,
                IsChecked = Engine.Profile.Current.GameWindowLock
            };

            item.Add(_gameWindowLock);

            _gameWindowPositionX = CreateInputField(item, new TextBox(1, 5, 80, 80, false)
            {
                Text = Engine.Profile.Current.GameWindowPosition.X.ToString(),
                X = 10,
                Y = 160,
                Width = 50,
                Height = 30,
                NumericOnly = true
            }, "Game Play Window Position: ");

            _gameWindowPositionY = CreateInputField(item, new TextBox(1, 5, 80, 80, false)
            {
                Text = Engine.Profile.Current.GameWindowPosition.Y.ToString(),
                X = 80,
                Y = 160,
                Width = 50,
                Height = 30,
                NumericOnly = true
            });



            rightArea.Add(item);

            Add(rightArea, PAGE);
        }


        private MacroControl _macroControl;

        private void BuildCommands()
        {
            const int PAGE = 4;
            ScrollArea rightArea = new ScrollArea(190, 52 + 25 + 4, 150, 360, true);


            NiceButton addButton = new NiceButton(190, 20, 130, 20, ButtonAction.Activate, "New macro") { IsSelectable = false, ToPage = (int) Buttons.NewMacro };

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
                        ToPage = (int) Buttons.Last + 1 + rightArea.Children.Count,
                    });

                    nb.IsSelected = true;


                    _macroControl?.Dispose();

                    _macroControl = new MacroControl(name)
                    {
                        X = 400,
                        Y = 20,
                    };

                    Add(_macroControl, PAGE);

                    nb.MouseClick += (sss, eee) =>
                    {
                        _macroControl?.Dispose();

                        _macroControl = new MacroControl(name)
                        {
                            X = 400,
                            Y = 20,
                        };

                        Add(_macroControl, PAGE);
                    };
                });

                Engine.UI.Add(dialog);
            };

            Add(addButton, PAGE);


            NiceButton delButton = new NiceButton(190, 52, 130, 20, ButtonAction.Activate, "Delete macro") {IsSelectable = false, ToPage = (int) Buttons.DeleteMacro};

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

                        if (rightArea.Children.OfType<ScrollAreaItem>().All(s => s.IsDisposed))
                        {
                            _macroControl?.Dispose();
                        }

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
                    ToPage = (int)Buttons.Last + 1 + rightArea.Children.Count,
                });

                nb.IsSelected = true;

                nb.MouseClick += (sss, eee) =>
                {
                    _macroControl?.Dispose();

                    _macroControl = new MacroControl(macro.Name)
                    {
                        X = 400,
                        Y = 20,
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
            Label text = new Label("Chat font:", true, HUE_FONT, 0, FONT);

            rightArea.Add(text);

            _fontSelectorChat = new FontSelector() { X = 20 };
            rightArea.Add(_fontSelectorChat);

            Add(rightArea, PAGE);
        }

        private void BuildSpeech()
        {
            const int PAGE = 7;
            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);
            ScrollAreaItem item = new ScrollAreaItem();

            _scaleSpeechDelay = new Checkbox(0x00D2, 0x00D3, "Scale speech delay by length", FONT, HUE_FONT, true)
            {
                IsChecked = Engine.Profile.Current.ScaleSpeechDelay
            };
            item.Add(_scaleSpeechDelay);
            rightArea.Add(item);
            item = new ScrollAreaItem();
            Label text = new Label("- Speech delay:", true, HUE_FONT, font: FONT);
            item.Add(text);
            _sliderSpeechDelay = new HSliderBar(100, 5, 150, 1, 1000, Engine.Profile.Current.SpeechDelay, HSliderBarStyle.MetalWidgetRecessedBar, true, FONT, HUE_FONT, true);
            item.Add(_sliderSpeechDelay);
            rightArea.Add(item);

            _speechColorPickerBox = CreateClickableColorBox(rightArea, 0, 30, Engine.Profile.Current.SpeechHue, "Speech color", 20, 30);
            _emoteColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.EmoteHue, "Emote color", 20, 0);
            _partyMessageColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.PartyMessageHue, "Party message color", 20, 0);
            _guildMessageColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.GuildMessageHue, "Guild message color", 20, 0);
            _allyMessageColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.AllyMessageHue, "Alliance message color", 20, 0);

            Add(rightArea, PAGE);
        }

        private void BuildCombat()
        {
            const int PAGE = 8;
            ScrollArea rightArea = new ScrollArea(190, 20, WIDTH - 210, 420, true);
            _innocentColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.InnocentHue, "Innocent color", 20, 0);
            _friendColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.FriendHue, "Friend color", 20, 0);
            _crimialColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.CriminalHue, "Criminal color", 20, 0);
            _genericColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.AnimalHue, "Animal color", 20, 0);
            _murdererColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.MurdererHue, "Murderer color", 20, 0);
            _enemyColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.EnemyHue, "Enemy color", 20, 0);

            _queryBeforAttackCheckbox = CreateCheckBox(rightArea, "Query before attack", Engine.Profile.Current.EnabledCriminalActionQuery, 0, 30);

            Add(rightArea, PAGE);
        }

        public void UpdateVideo()
        {
            BuildVideo();
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
                    //_smoothMovements.IsChecked = true;
                    _enablePathfind.IsChecked = true;
                    _alwaysRun.IsChecked = false;
                    _showHpMobile.IsChecked = false;
                    _hpComboBox.SelectedIndex = 0;
                    _highlightByState.IsChecked = true;
                    _drawRoofs.IsChecked = true;
                    _treeToStumps.IsChecked = false;
                    _caveToTile.IsChecked = false;
                    _hideVegetation.IsChecked = false;
                    _normalFields.IsChecked = true;
                    _staticFields.IsChecked = false;
                    _fieldsToTile.IsChecked = false;
                    _noColorOutOfRangeObjects.IsChecked = false;
                    _circleOfTranspRadius.Value = 5;
                    _useCircleOfTransparency.IsChecked = false;
                    _preloadMaps.IsChecked = false;
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
                    break;
                case 3: // video
                    _debugControls.IsChecked = false;
                    _zoom.IsChecked = false;
                    _savezoom.IsChecked = false;
                    _shardType.SelectedIndex = 0;
                    _gameWindowWidth.Text = "640";
                    _gameWindowHeight.Text = "480";
                    _gameWindowPositionX.Text = "10";
                    _gameWindowPositionY.Text = "10";
                    _gameWindowLock.IsChecked = false;
                    _gameWindowFullsize.IsChecked = false;
                    Engine.SceneManager.GetScene<GameScene>().Scale = 1;
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
                    _speechColorPickerBox.SetColor(0x02B2,  FileManager.Hues.GetPolygoneColor(12, 0x02B2));
                    _emoteColorPickerBox.SetColor(0x0021, FileManager.Hues.GetPolygoneColor(12, 0x0021));
                    _partyMessageColorPickerBox.SetColor(0x0044, FileManager.Hues.GetPolygoneColor(12, 0x0044));
                    _guildMessageColorPickerBox.SetColor(0x0044, FileManager.Hues.GetPolygoneColor(12, 0x0044));
                    _allyMessageColorPickerBox.SetColor(0x0057, FileManager.Hues.GetPolygoneColor(12, 0x0057));
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
            }
        }

        private void Apply()
        {
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

            if (Engine.Profile.Current.DrawRoofs != _drawRoofs.IsChecked)
            {
                Engine.Profile.Current.DrawRoofs = _drawRoofs.IsChecked;
                Engine.SceneManager.GetScene<GameScene>()?.UpdateMaxDrawZ(true);
            }

            if (Engine.Profile.Current.TopbarGumpIsDisabled != _enableTopbar.IsChecked)
            {
                if (_enableTopbar.IsChecked)
                {
                    Engine.UI.GetByLocalSerial<TopBarGump>()?.Dispose();
                }
                else
                    TopBarGump.Create();

                Engine.Profile.Current.TopbarGumpIsDisabled = _enableTopbar.IsChecked;
            }

            Engine.Profile.Current.TreeToStumps = _treeToStumps.IsChecked;
            Engine.Profile.Current.CaveToTile = _caveToTile.IsChecked;

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

            // video
            Engine.GlobalSettings.Debug = _debugControls.IsChecked;
            Engine.Profile.Current.EnableScaleZoom = _zoom.IsChecked;

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

            int GameWindowSizeWidth = 640;
            int GameWindowSizeHeight = 480;

            int.TryParse(_gameWindowWidth.Text, out GameWindowSizeWidth);
            int.TryParse(_gameWindowHeight.Text, out GameWindowSizeHeight);

            if (GameWindowSizeWidth != Engine.Profile.Current.GameWindowSize.X || GameWindowSizeHeight != Engine.Profile.Current.GameWindowSize.Y)
            {
                WorldViewportGump e = Engine.UI.GetByLocalSerial<WorldViewportGump>();
                Point n = e.ResizeWindow(new Point(GameWindowSizeWidth, GameWindowSizeHeight));

                _gameWindowWidth.Text = n.X.ToString();
                _gameWindowHeight.Text = n.Y.ToString();
            }

            int GameWindowPositionX = 20;
            int GameWindowPositionY = 20;

            int.TryParse(_gameWindowPositionX.Text, out GameWindowPositionX);
            int.TryParse(_gameWindowPositionY.Text, out GameWindowPositionY);

            if (GameWindowPositionX != Engine.Profile.Current.GameWindowPosition.X || GameWindowPositionY != Engine.Profile.Current.GameWindowPosition.Y)
            {
                Point n = new Point(GameWindowPositionX, GameWindowPositionY);

                WorldViewportGump e = Engine.UI.GetByLocalSerial<WorldViewportGump>();
                e.Location = n;

                Engine.Profile.Current.GameWindowPosition = n;
            }

            if (Engine.Profile.Current.GameWindowLock != _gameWindowLock.IsChecked)
            {
                if (_gameWindowLock.IsChecked)
                {
                    // lock
                    WorldViewportGump e = Engine.UI.GetByLocalSerial<WorldViewportGump>();
                    e.CanMove = false;
                }
                else
                {
                    // unlock
                    WorldViewportGump e = Engine.UI.GetByLocalSerial<WorldViewportGump>();
                    e.CanMove = true;
                }
                Engine.Profile.Current.GameWindowLock = _gameWindowLock.IsChecked;
            }
            
            if (_gameWindowFullsize.IsChecked != Engine.Profile.Current.GameWindowFullSize)
            {
                if (_gameWindowFullsize.IsChecked)
                {
                    WorldViewportGump e = Engine.UI.GetByLocalSerial<WorldViewportGump>();
                    e.ResizeWindow(new Point(Engine.WindowWidth, Engine.WindowHeight));
                    e.Location = new Point(-5, -5);
                }
                else
                {
                    WorldViewportGump e = Engine.UI.GetByLocalSerial<WorldViewportGump>();
                    e.ResizeWindow(new Point(640, 480));
                    e.Location = new Point(20, 20);
                }
                Engine.Profile.Current.GameWindowFullSize = _gameWindowFullsize.IsChecked;
            }

            if (_savezoom.IsChecked != Engine.Profile.Current.SaveScaleAfterClose)
            {
                if (!_savezoom.IsChecked)
                    Engine.Profile.Current.ScaleZoom = 1f;

                Engine.Profile.Current.SaveScaleAfterClose = _savezoom.IsChecked;
            }

            UpdateVideo();

            // fonts
            Engine.Profile.Current.ChatFont = _fontSelectorChat.GetSelectedFont();

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
        }

        private Texture2D _edge;

        public override bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
        {
            if (_edge == null)
            {
                _edge = new Texture2D(batcher.GraphicsDevice, 1, 1, false , SurfaceFormat.Color);
                _edge.SetData(new Color[] { Color.Gray });
            }

            batcher.DrawRectangle(_edge, new Rectangle(position.X, position.Y, Width, Height), Vector3.Zero);
            return base.Draw(batcher, position, hue);
        }

        public override void Dispose()
        {
            _edge?.Dispose();
            base.Dispose();
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

        class ClickableColorBox : ColorBox
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

            public override bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
            {
                batcher.Draw2D(_background, new Point(position.X - 3, position.Y - 3), Vector3.Zero);
                return base.Draw(batcher, position, hue);
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

        class FontSelector : Control
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
                            IsChecked =  Engine.Profile.Current.ChatFont == i
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

                    if (b != null && b.IsChecked)
                    {
                        return i;
                    }
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