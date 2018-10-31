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
using ClassicUO.IO.Resources;

namespace ClassicUO.Game.Gumps.UIGumps
{
    public class OptionsGump : Gump
    {
        private readonly Settings _settings;
        private Checkbox _checkboxSound, _checkboxMusic, _checboxFootstepsSound, _checkboxPlayCombatMusic, _checkboxPlaySoundsInBackground, _checkboxHighlightGameObjects, _checkboxUseTooltips, _checkboxSmoothMovement;
        private ColorPickerGump _colorPickerGump;
        private ColorPickerBox _colorPickerTooltipText;
        private HSliderBar _sliderSound, _sliderMusic, _sliderFPS, _sliderDelayAppearTooltips;

        public OptionsGump() : base(0, 0)
        {
            CanMove = true;
            AcceptMouseInput = false;
            _settings = Service.Get<Settings>();

            // base
            AddChildren(new ResizePic(0x0A28)
            {
                X = 40, Y = 0, Width = 550, Height = 450
            });

            // left
            AddChildren(new Button((int) Buttons.SoundAndMusic, 0x00DA, 0x00DA)
            {
                X = 0, Y = 45, ButtonAction = ButtonAction.SwitchPage, ToPage = 1
            });

            AddChildren(new Button((int) Buttons.Configuration, 0x00DC, 0x00DC)
            {
                X = 0, Y = 111, ButtonAction = ButtonAction.SwitchPage, ToPage = 2
            });

            AddChildren(new Button((int) Buttons.Language, 0x00DE, 0x00DE)
            {
                X = 0, Y = 177, ButtonAction = ButtonAction.SwitchPage, ToPage = 3
            });

            AddChildren(new Button((int) Buttons.Chat, 0x00E0, 0x00E0)
            {
                X = 0, Y = 243, ButtonAction = ButtonAction.SwitchPage, ToPage = 4
            });

            AddChildren(new Button((int) Buttons.Macro, 0x00ED, 0x00ED)
            {
                X = 0, Y = 309, ButtonAction = ButtonAction.SwitchPage, ToPage = 5
            });

            // right
            AddChildren(new Button((int) Buttons.Interface, 0x00E2, 0x00E2)
            {
                X = 576, Y = 45, ButtonAction = ButtonAction.SwitchPage, ToPage = 6
            });

            AddChildren(new Button((int) Buttons.Display, 0x00E4, 0x00E4)
            {
                X = 576, Y = 111, ButtonAction = ButtonAction.SwitchPage, ToPage = 7
            });

            AddChildren(new Button((int) Buttons.Reputation, 0x00E6, 0x00E6)
            {
                X = 576, Y = 177, ButtonAction = ButtonAction.SwitchPage, ToPage = 8
            });

            AddChildren(new Button((int) Buttons.Misc, 0x00E8, 0x00E8)
            {
                X = 576, Y = 243, ButtonAction = ButtonAction.SwitchPage, ToPage = 9
            });

            AddChildren(new Button((int) Buttons.FilterOptions, 0x00EB, 0x00EB)
            {
                X = 576, Y = 309, ButtonAction = ButtonAction.SwitchPage, ToPage = 10
            });

            // bottom
            AddChildren(new Button((int) Buttons.Cancel, 0x00F3, 0x00F1, 0x00F2)
            {
                X = 154, Y = 405, ButtonAction = ButtonAction.Activate, ToPage = 0
            });

            AddChildren(new Button((int) Buttons.Apply, 0x00EF, 0x00F0, 0x00EE)
            {
                X = 248, Y = 405, ButtonAction = ButtonAction.Activate, ToPage = 0
            });

            AddChildren(new Button((int) Buttons.Default, 0x00F6, 0x00F4, 0x00F5)
            {
                X = 346, Y = 405, ButtonAction = ButtonAction.Activate, ToPage = 0
            });

            AddChildren(new Button((int) Buttons.Ok, 0x00F9, 0x00F8, 0x00F7)
            {
                X = 443, Y = 405, ButtonAction = ButtonAction.Activate, ToPage = 0
            });
            BuildPage1();
            BuildPage2();
            BuildPage3();
            BuildPage4();
            BuildPage5();
            BuildPage6();
            BuildPage7();
            BuildPage8();
            BuildPage9();
            BuildPage10();
            ChangePage(2);
        }

        private void BuildPage1()
        {
            AddChildren(new GumpPic(0, 45, 0x00D9, 0)
            {
                CanMove = false
            }, 1);

            Label label = new Label("Sound and Music", true, 0, 460, align: TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = 84, Y = 22
            };
            AddChildren(label, 1);

            label = new Label("These settings affect the sound and music you will hear while playing Ultima Online.", true, 0, 500)
            {
                X = 64, Y = 44
            };
            AddChildren(label, 1);

            _checkboxSound = new Checkbox(0x00D2, 0x00D3, "Sound On/Off")
            {
                X = 64, Y = 90, IsChecked = _settings.Sound
            };
            AddChildren(_checkboxSound, 1);

            label = new Label("Sound Volume", true, 0)
            {
                X = 64, Y = 112
            };
            AddChildren(label, 1);
            _sliderSound = new HSliderBar(64, 133, 90, 0, 255, _settings.SoundVolume, HSliderBarStyle.MetalWidgetRecessedBar, true);
            AddChildren(_sliderSound, 1);

            _checkboxMusic = new Checkbox(0x00D2, 0x00D3, "Music On/Off")
            {
                X = 64, Y = 151, IsChecked = _settings.Music
            };
            AddChildren(_checkboxMusic, 1);

            label = new Label("Music volume", true, 0)
            {
                X = 64, Y = 173
            };
            AddChildren(label, 1);
            _sliderMusic = new HSliderBar(64, 194, 90, 0, 255, _settings.MusicVolume, HSliderBarStyle.MetalWidgetRecessedBar, true);
            AddChildren(_sliderMusic, 1);

            _checboxFootstepsSound = new Checkbox(0x00D2, 0x00D3, "Play footsteps sound")
            {
                X = 64, Y = 212
            };
            AddChildren(_checboxFootstepsSound, 1);

            _checkboxPlayCombatMusic = new Checkbox(0x00D2, 0x00D3, "Play combat music")
            {
                X = 64, Y = 232
            };
            AddChildren(_checkboxPlayCombatMusic, 1);

            _checkboxPlaySoundsInBackground = new Checkbox(0x00D2, 0x00D3, "Play sounds in background")
            {
                X = 64, Y = 252
            };
            AddChildren(_checkboxPlaySoundsInBackground, 1);
        }

        private void BuildPage2()
        {
            AddChildren(new GumpPic(0, 111, 0x00DB, 0)
            {
                CanMove = false
            }, 2);

            Label label = new Label("ClassicUO configuration", true, 0, 460, align: TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = 84, Y = 22
            };
            AddChildren(label, 2);
            ScrollArea scrollArea = new ScrollArea(64, 90, 500, 300, true);
            AddChildren(scrollArea, 2);

            label = new Label("FPS:", true, 0)
            {
                X = 0, Y = 0
            };
            scrollArea.AddChildren(label);
            _sliderFPS = new HSliderBar(0, 21, 90, 15, 250, _settings.MaxFPS, HSliderBarStyle.MetalWidgetRecessedBar, true);
            scrollArea.AddChildren(_sliderFPS);

            _checkboxHighlightGameObjects = new Checkbox(0x00D2, 0x00D3, "Highlight game objects")
            {
                X = 0, Y = 41, IsChecked = _settings.HighlightGameObjects
            };
            scrollArea.AddChildren(_checkboxHighlightGameObjects);

            _checkboxSmoothMovement = new Checkbox(0x00D2, 0x00D3, "Smooth movement")
            {
                X = 0, Y = 61, IsChecked = _settings.SmoothMovement
            };
            scrollArea.AddChildren(_checkboxSmoothMovement);
            int y = 81;

            for (int i = 0; i < 400; i++)
            {
                Checkbox ck = new Checkbox(0x00D2, 0x00D3, "TRY " + i)
                {
                    Y = y
                };
                ck.ValueChanged += (sender, e) => Console.WriteLine("PRESSED: " + ck.Text);
                scrollArea.AddChildren(ck);
                y += 20;
            }

            y += 20;

            for (int i = 0; i < 40; i++)
            {
                Button ck = new Button((int) Buttons.Ok + i + 1, 0x00F9, 0x00F8, 0x00F7)
                {
                    X = 34, Y = y, ButtonAction = ButtonAction.Activate
                };
                ck.MouseClick += (sender, e) => Console.WriteLine("PRESSED: " + ck.ButtonID);
                scrollArea.AddChildren(ck);
                y += ck.Height;
            }
        }

        private void BuildPage3()
        {
            AddChildren(new GumpPic(0, 177, 0x00DD, 0)
            {
                CanMove = false
            }, 3);

            Label label = new Label("Language", true, 0, 460, align: TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = 84, Y = 22
            };
            AddChildren(label, 3);

            label = new Label("The language you use when playing UO is obtained from your OS.", true, 0, 480)
            {
                X = 64, Y = 44
            };
            AddChildren(label, 3);

            _checkboxUseTooltips = new Checkbox(0x00D2, 0x00D3, "Use tooltips")
            {
                X = 64, Y = 90, IsChecked = _settings.UseTooltips
            };
            AddChildren(_checkboxUseTooltips, 3);

            label = new Label("Delay before tooltip appears", true, 0)
            {
                X = 64, Y = 112
            };
            AddChildren(label, 3);
            _sliderDelayAppearTooltips = new HSliderBar(64, 133, 90, 0, 5000, _settings.DelayAppearTooltips, HSliderBarStyle.MetalWidgetRecessedBar, true);
            AddChildren(_sliderDelayAppearTooltips, 3);

            Button button = new Button((int) Buttons.TextColor, 0x00D4, 0x00D4)
            {
                X = 64, Y = 151, ButtonAction = ButtonAction.Activate, ToPage = 3
            };

            button.MouseClick += (sender, e) =>
            {
                ColorPickerGump pickerGump = new ColorPickerGump(100, 100, s => _colorPickerTooltipText.SetHue(s));
                UIManager.Add(pickerGump);
            };
            AddChildren(button, 3);
            uint color = 0xFF7F7F7F;
            if (_settings.TooltipsTextColor != 0xFFFF) color = Hues.RgbaToArgb((Hues.GetPolygoneColor(12, _settings.TooltipsTextColor) << 8) | 0xFF);
            _colorPickerTooltipText = new ColorPickerBox(67, 154, 1, 1, 13, 14);
            _colorPickerTooltipText.SetHue(color);
            AddChildren(_colorPickerTooltipText, 3);

            label = new Label("Color of tooltips text", true, 0)
            {
                X = 88, Y = 151
            };
            AddChildren(label, 3);

            AddChildren(new Button((int) Buttons.TextFont, 0x00D0, 0x00D0)
            {
                X = 64, Y = 173, ButtonAction = ButtonAction.Activate, ToPage = 3
            }, 3);

            label = new Label("Font for tooltips", true, 0)
            {
                X = 88, Y = 173
            };
            AddChildren(label, 3);
        }

        private void BuildPage4()
        {
            AddChildren(new GumpPic(0, 243, 0x00DF, 0)
            {
                CanMove = false
            }, 4);

            Label label = new Label("Chat", true, 0, 460, align: TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = 84, Y = 22
            };
            AddChildren(label, 4);

            label = new Label("These settings affect the interface display for the chat system.", true, 0)
            {
                X = 64, Y = 44
            };
            AddChildren(label, 4);
        }

        private void BuildPage5()
        {
            AddChildren(new GumpPic(0, 309, 0x00EC, 0)
            {
                CanMove = false
            }, 5);
        }

        private void BuildPage6()
        {
            AddChildren(new GumpPic(576, 45, 0x00E1, 0)
            {
                CanMove = false
            }, 6);
        }

        private void BuildPage7()
        {
            AddChildren(new GumpPic(576, 111, 0x00E3, 0)
            {
                CanMove = false
            }, 7);
        }

        private void BuildPage8()
        {
            AddChildren(new GumpPic(576, 177, 0x00E5, 0)
            {
                CanMove = false
            }, 8);
        }

        private void BuildPage9()
        {
            AddChildren(new GumpPic(576, 243, 0x00E7, 0)
            {
                CanMove = false
            }, 9);
        }

        private void BuildPage10()
        {
            AddChildren(new GumpPic(576, 309, 0x00EA, 0)
            {
                CanMove = false
            }, 10);
        }

        private void ApplySettings()
        {
            _settings.Sound = _checkboxSound.IsChecked;
            _settings.SoundVolume = _sliderSound.Value;
            _settings.Music = _checkboxMusic.IsChecked;
            _settings.MusicVolume = _sliderMusic.Value;
            _settings.UseTooltips = _checkboxUseTooltips.IsChecked;
            _settings.DelayAppearTooltips = _sliderDelayAppearTooltips.Value;
            _settings.MaxFPS = Service.Get<GameLoop>().MaxFPS = _sliderFPS.Value;
            _settings.HighlightGameObjects = _checkboxHighlightGameObjects.IsChecked;
            _settings.SmoothMovement = _checkboxSmoothMovement.IsChecked;
        }

        private void RestoreDefaultSettings()
        {
        }

        public override void OnButtonClick(int buttonID)
        {
            if (buttonID > (int) Buttons.Ok)
                return;

            switch ((Buttons) buttonID)
            {
                case Buttons.SoundAndMusic:

                    break;
                case Buttons.Configuration:

                    break;
                case Buttons.Language:

                    break;
                case Buttons.Chat:

                    break;
                case Buttons.Macro:

                    break;
                case Buttons.Interface:

                    break;
                case Buttons.Display:

                    break;
                case Buttons.Reputation:

                    break;
                case Buttons.Misc:

                    break;
                case Buttons.FilterOptions:

                    break;
                case Buttons.TextColor:

                    break;
                case Buttons.TextFont:

                    break;
                case Buttons.Cancel:
                    Dispose();

                    break;
                case Buttons.Apply:
                    ApplySettings();

                    break;
                case Buttons.Default:
                    RestoreDefaultSettings();

                    break;
                case Buttons.Ok:
                    ApplySettings();
                    Dispose();

                    break;
                default:

                    throw new ArgumentOutOfRangeException(nameof(buttonID), buttonID, null);
            }
        }

        private enum Buttons
        {
            SoundAndMusic,
            Configuration,
            Language,
            Chat,
            Macro,
            Interface,
            Display,
            Reputation,
            Misc,
            FilterOptions,
            TextColor,
            TextFont,
            Cancel,
            Apply,
            Default,
            Ok
        }
    }
}