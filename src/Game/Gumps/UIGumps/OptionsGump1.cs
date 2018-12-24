#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
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

using ClassicUO.Configuration;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.UIGumps
{
    internal class OptionsGump1 : Gump
    {

        // general
        private HSliderBar _sliderFPS;
        private Checkbox _highlightObjects, _smoothMovements, _enablePathfind, _alwaysRun, _preloadMaps, _showHpMobile, _highlightByState;
        private Combobox _hpComboBox;

        // sounds

        // speech
        private Checkbox _scaleSpeechDelay;
        private HSliderBar _sliderSpeechDelay;
        private ColorBox _speechColorPickerBox, _emoteColorPickerBox, _partyMessageColorPickerBox, _guildMessageColorPickerBox, _allyMessageColorPickerBox;

        // combat
        private ColorBox _innocentColorPickerBox, _friendColorPickerBox, _crimialColorPickerBox, _genericColorPickerBox, _enemyColorPickerBox, _murdererColorPickerBox;
        private Checkbox _queryBeforAttackCheckbox;


        public OptionsGump1() : base(0, 0)
        {
            AddChildren(new ResizePic( /*0x2436*/ /*0x2422*/ /*0x9C40*/ 9200 /*0x53*/ /*0xE10*/)
            {
                Width = 600, Height = 500
            });

            //AddChildren(new GameBorder(0, 0, 600, 400, 4));

            //AddChildren(new GumpPicTiled(4, 4, 600 - 8, 400 - 8, 0x0A40) { IsTransparent = false});

            //AddChildren(new ResizePic(0x2436) { X = 20, Y = 20, Width = 150, Height = 460 });

            //AddChildren(new LeftButton() { X = 40, Y = 40 });
            ScrollArea leftArea = new ScrollArea(10, 10, 160, 480, true);
            ScrollAreaItem item = new ScrollAreaItem();

            item.AddChildren(new Button(0, 0x9C5, 0x9C5, 0x9C5, "General", 1, true, 14, 24)
            {
                Y = 30, FontCenter = true, ButtonAction = ButtonAction.SwitchPage, ToPage = 1
            });

            item.AddChildren(new Button(0, 0x9C5, 0x9C5, 0x9C5, "Sounds", 1, true, 14, 24)
            {
                Y = 30 * 2, FontCenter = true, ButtonAction = ButtonAction.SwitchPage, ToPage = 2
            });

            item.AddChildren(new Button(0, 0x9C5, 0x9C5, 0x9C5, "Video", 1, true, 14, 24)
            {
                Y = 30 * 3, FontCenter = true, ButtonAction = ButtonAction.SwitchPage, ToPage = 3
            });

            item.AddChildren(new Button(0, 0x9C5, 0x9C5, 0x9C5, "Commands", 1, true, 14, 24)
            {
                Y = 30 * 4, FontCenter = true, ButtonAction = ButtonAction.SwitchPage, ToPage = 4
            });

            item.AddChildren(new Button(0, 0x9C5, 0x9C5, 0x9C5, "Tooltip", 1, true, 14, 24)
            {
                Y = 30 * 5, FontCenter = true, ButtonAction = ButtonAction.SwitchPage, ToPage = 5
            });

            item.AddChildren(new Button(0, 0x9C5, 0x9C5, 0x9C5, "Fonts", 1, true, 14, 24)
            {
                Y = 30 * 6, FontCenter = true, ButtonAction = ButtonAction.SwitchPage, ToPage = 6
            });

            item.AddChildren(new Button(0, 0x9C5, 0x9C5, 0x9C5, "Speech", 1, true, 14, 24)
            {
                Y = 30 * 7, FontCenter = true, ButtonAction = ButtonAction.SwitchPage, ToPage = 7
            });

            item.AddChildren(new Button(0, 0x9C5, 0x9C5, 0x9C5, "Combat", 1, true, 14, 24)
            {
                Y = 30 * 8, FontCenter = true, ButtonAction = ButtonAction.SwitchPage, ToPage = 8
            });
            leftArea.AddChildren(item);
            AddChildren(leftArea);
            int offsetX = 60;
            int offsetY = 60;

            AddChildren(new Button((int) Buttons.Cancel, 0x00F3, 0x00F1, 0x00F2)
            {
                X = 154 + offsetX, Y = 405 + offsetY, ButtonAction = ButtonAction.Activate
            });

            AddChildren(new Button((int) Buttons.Apply, 0x00EF, 0x00F0, 0x00EE)
            {
                X = 248 + offsetX, Y = 405 + offsetY, ButtonAction = ButtonAction.Activate
            });

            AddChildren(new Button((int) Buttons.Default, 0x00F6, 0x00F4, 0x00F5)
            {
                X = 346 + offsetX, Y = 405 + offsetY, ButtonAction = ButtonAction.Activate
            });

            AddChildren(new Button((int) Buttons.Ok, 0x00F9, 0x00F8, 0x00F7)
            {
                X = 443 + offsetX, Y = 405 + offsetY, ButtonAction = ButtonAction.Activate
            });
            AcceptMouseInput = false;
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
            ScrollArea rightArea = new ScrollArea(190, 60, 390, 380, true);

            // FPS
            ScrollAreaItem fpsItem = new ScrollAreaItem();
            Label text = new Label("- FPS:", true, 1);
            fpsItem.AddChildren(text);
            _sliderFPS = new HSliderBar(40, 5, 250, 15, 250, Engine.Profile.Current.MaxFPS, HSliderBarStyle.MetalWidgetRecessedBar, true, 1);
            fpsItem.AddChildren(_sliderFPS);
            rightArea.AddChildren(fpsItem);

            // Highlight
            _highlightObjects = new Checkbox(0x00D2, 0x00D3, "Highlight game objects", 1)
            {
                Y = 10, IsChecked = Engine.Profile.Current.HighlightGameObjects
            };
            rightArea.AddChildren(_highlightObjects);

            // smooth movements
            _smoothMovements = new Checkbox(0x00D2, 0x00D3, "Smooth movements", 1)
            {
                IsChecked = Engine.Profile.Current.SmoothMovements
            };
            rightArea.AddChildren(_smoothMovements);

            _enablePathfind = new Checkbox(0x00D2, 0x00D3, "Enable pathfinding", 1)
            {
                IsChecked = Engine.Profile.Current.EnablePathfind
            };
            rightArea.AddChildren(_enablePathfind);

            _alwaysRun = new Checkbox(0x00D2, 0x00D3, "Always run", 1)
            {
                IsChecked = Engine.Profile.Current.AlwaysRun
            };
            rightArea.AddChildren(_alwaysRun);

            // preload maps
            _preloadMaps = new Checkbox(0x00D2, 0x00D3, "Preload maps (it increases the RAM usage)", 1)
            {
                IsChecked = Engine.GlobalSettings.PreloadMaps
            };
            rightArea.AddChildren(_preloadMaps);

            // show % hp mobile
            ScrollAreaItem hpAreaItem = new ScrollAreaItem();

            text = new Label("- Mobiles HP", true, 1)
            {
                Y = 10
            };
            hpAreaItem.AddChildren(text);

            _showHpMobile = new Checkbox(0x00D2, 0x00D3, "Show HP", 1)
            {
                X = 25, Y = 30, IsChecked = Engine.Profile.Current.ShowMobilesHP
            };
            hpAreaItem.AddChildren(_showHpMobile);
            int mode = Engine.Profile.Current.MobileHPType;

            if (mode < 0 || mode > 2)
                mode = 0;

            _hpComboBox = new Combobox(200, 30, 150, new[]
            {
                "Percentage", "Line", "Both"
            }, mode);
            hpAreaItem.AddChildren(_hpComboBox);
            rightArea.AddChildren(hpAreaItem);

            // highlight character by flags
            ScrollAreaItem highlightByFlagsItem = new ScrollAreaItem();

            text = new Label("- Mobiles status", true, 1)
            {
                Y = 10
            };
            highlightByFlagsItem.AddChildren(text);

            _highlightByState = new Checkbox(0x00D2, 0x00D3, "Highlight by state\n(poisoned, yellow hits, paralyzed)", 1)
            {
                X = 25, Y = 30, IsChecked = Engine.Profile.Current.HighlightMobilesByFlags
            };
            highlightByFlagsItem.AddChildren(_highlightByState);
            rightArea.AddChildren(highlightByFlagsItem);
            AddChildren(rightArea, PAGE);
        }

        private void BuildSounds()
        {
            const int PAGE = 2;
            ScrollArea rightArea = new ScrollArea(190, 60, 390, 380, true);

            Checkbox soundCheckbox = new Checkbox(0x00D2, 0x00D3, "Sounds", 1)
            {
                IsChecked = Engine.Profile.Current.EnableSound
            };
            rightArea.AddChildren(soundCheckbox);
            ScrollAreaItem item = new ScrollAreaItem();
            Label text = new Label("- Sounds volume:", true, 0, 0, 1);

            HSliderBar sliderVolume = new HSliderBar(40, 5, 180, 0, 255, Engine.Profile.Current.SoundVolume, HSliderBarStyle.MetalWidgetRecessedBar, true, 1)
            {
                X = 120
            };
            item.AddChildren(text);
            item.AddChildren(sliderVolume);
            rightArea.AddChildren(item);

            Checkbox musicCheckbox = new Checkbox(0x00D2, 0x00D3, "Music", 1)
            {
                IsChecked = Engine.Profile.Current.EnableMusic
            };
            rightArea.AddChildren(musicCheckbox);
            item = new ScrollAreaItem();
            text = new Label("- Music volume:", true, 0, 0, 1);

            HSliderBar sliderMusicVolume = new HSliderBar(40, 5, 180, 0, 255, Engine.Profile.Current.MusicVolume, HSliderBarStyle.MetalWidgetRecessedBar, true, 1)
            {
                X = 120
            };
            item.AddChildren(text);
            item.AddChildren(sliderMusicVolume);
            rightArea.AddChildren(item);
            item = new ScrollAreaItem();

            Checkbox footstepsCheckbox = new Checkbox(0x00D2, 0x00D3, "Footsteps sound", 1)
            {
                Y = 30, IsChecked = Engine.Profile.Current.EnableFootstepsSound
            };
            item.AddChildren(footstepsCheckbox);
            rightArea.AddChildren(item);

            Checkbox combatMusicCheckbox = new Checkbox(0x00D2, 0x00D3, "Combat music", 1)
            {
                IsChecked = Engine.Profile.Current.EnableCombatMusic
            };
            rightArea.AddChildren(combatMusicCheckbox);

            Checkbox backgroundMusicCheckbox = new Checkbox(0x00D2, 0x00D3, "Reproduce music when ClassicUO is not focussed", 1)
            {
                IsChecked = Engine.Profile.Current.ReproduceSoundsInBackground
            };
            rightArea.AddChildren(backgroundMusicCheckbox);
            AddChildren(rightArea, PAGE);
        }

        private void BuildVideo()
        {
            const int PAGE = 3;
            ScrollArea rightArea = new ScrollArea(190, 60, 390, 380, true);
            ScrollAreaItem item = new ScrollAreaItem();
            AddChildren(rightArea, PAGE);
        }

        private void BuildCommands()
        {
            const int PAGE = 4;
            ScrollArea rightArea = new ScrollArea(190, 60, 390, 380, true);
            ScrollAreaItem item = new ScrollAreaItem();
            AddChildren(rightArea, PAGE);
        }

        private void BuildTooltip()
        {
            const int PAGE = 5;
            ScrollArea rightArea = new ScrollArea(190, 60, 390, 380, true);
            ScrollAreaItem item = new ScrollAreaItem();
            AddChildren(rightArea, PAGE);
        }

        private void BuildFonts()
        {
            const int PAGE = 6;
            ScrollArea rightArea = new ScrollArea(190, 60, 390, 380, true);
            ScrollAreaItem item = new ScrollAreaItem();
            AddChildren(rightArea, PAGE);
        }

        private void BuildSpeech()
        {
            const int PAGE = 7;
            ScrollArea rightArea = new ScrollArea(190, 60, 390, 380, true);
            ScrollAreaItem item = new ScrollAreaItem();

            _scaleSpeechDelay = new Checkbox(0x00D2, 0x00D3, "Scale speech delay by length", 1)
            {
                IsChecked = Engine.Profile.Current.ScaleSpeechDelay
            };
            item.AddChildren(_scaleSpeechDelay);
            rightArea.AddChildren(item);
            item = new ScrollAreaItem();
            Label text = new Label("- Speech delay:", true, 1);
            item.AddChildren(text);
            _sliderSpeechDelay = new HSliderBar(100, 5, 150, 1, 1000, Engine.Profile.Current.SpeechDelay, HSliderBarStyle.MetalWidgetRecessedBar, true, 1);
            item.AddChildren(_sliderSpeechDelay);
            rightArea.AddChildren(item);

            _speechColorPickerBox = CreateClickableColorBox(rightArea, 0, 30, Engine.Profile.Current.SpeechHue, "Speech color", 20, 30);
            _emoteColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.EmoteHue, "Emote color", 20, 0);
            _partyMessageColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.PartyMessageHue, "Party message color", 20, 0);
            _guildMessageColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.GuildMessageHue, "Guild message color", 20, 0);
            _allyMessageColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.AllyMessageHue, "Alliance message color", 20, 0);

            AddChildren(rightArea, PAGE);
        }

        private void BuildCombat()
        {
            const int PAGE = 8;
            ScrollArea rightArea = new ScrollArea(190, 60, 390, 380, true);

            _innocentColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.InnocentHue, "Innocent color", 20, 0);
            _friendColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.FriendHue, "Friend color", 20, 0);
            _crimialColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.CriminalHue, "Criminal color", 20, 0);
            _genericColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.AnimalHue, "Animal color", 20, 0);
            _murdererColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.MurdererHue, "Murderer color", 20, 0);
            _enemyColorPickerBox = CreateClickableColorBox(rightArea, 0, 0, Engine.Profile.Current.EnemyHue, "Enemy color", 20, 0);


            ScrollAreaItem item = new ScrollAreaItem();

            _queryBeforAttackCheckbox = new Checkbox(0x00D2, 0x00D3, "Query before attack", 1)
            {
                Y = 30,
                IsChecked = Engine.Profile.Current.EnabledCriminalActionQuery
            };
            item.AddChildren(_queryBeforAttackCheckbox);

            rightArea.AddChildren(item);
            AddChildren(rightArea, PAGE);
        }


        public override void OnButtonClick(int buttonID)
        {
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
                    _highlightObjects.IsChecked = true;
                    _smoothMovements.IsChecked = true;
                    _enablePathfind.IsChecked = true;
                    _alwaysRun.IsChecked = false;
                    _showHpMobile.IsChecked = false;
                    _hpComboBox.SelectedIndex = 0;
                    _highlightByState.IsChecked = true;
                    break;
                case 2: // sounds
                    
                    break;
                case 3: // video

                    break;
                case 4: // commands

                    break;
                case 5: // tooltip

                    break;
                case 6: // fonts
                    
                    
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
            Engine.Profile.Current.MaxFPS = Engine.FpsLimit = _sliderFPS.Value;
            Engine.Profile.Current.HighlightGameObjects = _highlightObjects.IsChecked;
            Engine.Profile.Current.SmoothMovements = _smoothMovements.IsChecked;
            Engine.Profile.Current.EnablePathfind = _enablePathfind.IsChecked;
            Engine.Profile.Current.AlwaysRun = _alwaysRun.IsChecked;
            Engine.Profile.Current.ShowMobilesHP = _showHpMobile.IsChecked;
            Engine.Profile.Current.HighlightMobilesByFlags = _highlightByState.IsChecked;
            Engine.Profile.Current.MobileHPType = _hpComboBox.SelectedIndex;

            // sounds

            // speech
            Engine.Profile.Current.ScaleSpeechDelay = _scaleSpeechDelay.IsChecked;
            Engine.Profile.Current.SpeechDelay = _sliderSpeechDelay.Value;
            Engine.Profile.Current.SpeechHue = _speechColorPickerBox.Hue;
            Engine.Profile.Current.EmoteHue = _emoteColorPickerBox.Hue;
            Engine.Profile.Current.PartyMessageHue = _partyMessageColorPickerBox.Hue;
            Engine.Profile.Current.GuildMessageHue = _guildMessageColorPickerBox.Hue;
            Engine.Profile.Current.AllyMessageHue = _allyMessageColorPickerBox.Hue;

            //// combat
            Engine.Profile.Current.InnocentHue = _innocentColorPickerBox.Hue;
            Engine.Profile.Current.FriendHue = _friendColorPickerBox.Hue;
            Engine.Profile.Current.CriminalHue = _crimialColorPickerBox.Hue;
            Engine.Profile.Current.AnimalHue = _genericColorPickerBox.Hue;
            Engine.Profile.Current.EnemyHue = _enemyColorPickerBox.Hue;
            Engine.Profile.Current.MurdererHue = _murdererColorPickerBox.Hue;
            Engine.Profile.Current.EnabledCriminalActionQuery = _queryBeforAttackCheckbox.IsChecked;
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
            MurdererColor
        }

        private ClickableColorBox CreateClickableColorBox(ScrollArea area, int x, int y, ushort hue, string text, int labelX, int labelY)
        {
            ScrollAreaItem item = new ScrollAreaItem();

            uint color = 0xFF7F7F7F;

            if (hue != 0xFFFF)
                color = FileManager.Hues.GetPolygoneColor(12, hue);

            ClickableColorBox box = new ClickableColorBox(x, y, 13, 14, hue, color);
            item.AddChildren(box);

            item.AddChildren(new Label(text, true, 1)
            {
                X = labelX, Y = labelY
            });
            area.AddChildren(item);
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
                    ColorPickerGump pickerGump = new ColorPickerGump(100, 100, s => SetColor(s, FileManager.Hues.GetPolygoneColor(CELL, s)));
                    Engine.UI.Add(pickerGump);
                }
            }
        }

    }
}