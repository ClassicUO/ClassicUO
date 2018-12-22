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
using System.Collections.Generic;
using System.Linq;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;

namespace ClassicUO.Game.Gumps.UIGumps.CharCreation
{
    internal class CreateCharAppearanceGump : Gump
    {
        private PlayerMobile _character;
        private Combobox _hairCombobox, _facialCombobox;
        private Label _hairLabel, _facialLabel;
        private readonly RadioButton _maleRadio;
        private readonly RadioButton _femaleRadio;
        private readonly RadioButton _humanRadio;
        private readonly RadioButton _elfRadio;
        private readonly RadioButton _gargoyleRadio;
        private readonly TextBox _nameTextBox;
        private PaperDollInteractable _paperDoll;
        private readonly Dictionary<Layer, Tuple<int, Hue>> CurrentColorOption = new Dictionary<Layer, Tuple<int, Hue>>();
        private readonly Dictionary<Layer, int> CurrentOption = new Dictionary<Layer, int>
        {
            {
                Layer.Hair, 1
            },
            {
                Layer.Beard, 1
            }
        };

        public CreateCharAppearanceGump() : base(0, 0)
        {
            AddChildren(new ResizePic(0x0E10)
            {
                X = 82, Y = 125, Width = 151, Height = 310
            }, 1);
            AddChildren(new GumpPic(280, 53, 0x0709, 0), 1);
            AddChildren(new GumpPic(240, 73, 0x070A, 0), 1);
            AddChildren(new GumpPicTiled(248, 73, 215, 16, 0x070B), 1);
            AddChildren(new GumpPic(463, 73, 0x070C, 0), 1);
            AddChildren(new GumpPic(238, 98, 0x0708, 0), 1);

            AddChildren(new ResizePic(0x0E10)
            {
                X = 475, Y = 125, Width = 151, Height = 310
            }, 1);

            // Male/Female Radios
            AddChildren(_maleRadio = new RadioButton(0, 0x0768, 0x0767)
            {
                X = 425, Y = 435
            }, 1);
            _maleRadio.ValueChanged += Genre_ValueChanged;

            AddChildren(_femaleRadio = new RadioButton(0, 0x0768, 0x0767)
            {
                X = 425, Y = 455
            }, 1);
            _femaleRadio.ValueChanged += Genre_ValueChanged;

            AddChildren(new Button((int) Buttons.MaleButton, 0x0710, 0x0712, 0x0711)
            {
                X = 445, Y = 435, ButtonAction = ButtonAction.Activate
            }, 1);

            AddChildren(new Button((int) Buttons.FemaleButton, 0x070D, 0x070F, 0x070E)
            {
                X = 445, Y = 455, ButtonAction = ButtonAction.Activate
            }, 1);

            AddChildren(_nameTextBox = new TextBox(5, 32, 300, 300, false, hue: 1)
            {
                X = 257, Y = 65, Width = 300, Height = 20
            }, 1);
            _nameTextBox.SetText(string.Empty);

            // Races
            AddChildren(_humanRadio = new RadioButton(1, 0x0768, 0x0767)
            {
                X = 180, Y = 435
            }, 1);

            AddChildren(new Button((int) Buttons.HumanButton, 0x0702, 0x0704, 0x0703)
            {
                X = 200, Y = 435, ButtonAction = ButtonAction.Activate
            }, 1);
            _humanRadio.ValueChanged += Race_ValueChanged;

            AddChildren(_elfRadio = new RadioButton(1, 0x0768, 0x0767)
            {
                X = 180, Y = 455
            }, 1);

            AddChildren(new Button((int) Buttons.ElfButton, 0x0705, 0x0707, 0x0706)
            {
                X = 200, Y = 455, ButtonAction = ButtonAction.Activate
            }, 1);
            _elfRadio.ValueChanged += Race_ValueChanged;

            if (FileManager.ClientVersion >= ClientVersions.CV_60144)
            {
                AddChildren(_gargoyleRadio = new RadioButton(1, 0x0768, 0x0767)
                {
                    X = 60, Y = 435
                }, 1);

                AddChildren(new Button((int) Buttons.GargoyleButton, 0x07D3, 0x07D5, 0x07D4)
                {
                    X = 80, Y = 435, ButtonAction = ButtonAction.Activate
                }, 1);
                _gargoyleRadio.ValueChanged += Race_ValueChanged;
            }

            // Prev/Next
            AddChildren(new Button((int) Buttons.Prev, 0x15A1, 0x15A3, 0x15A2)
            {
                X = 586, Y = 445, ButtonAction = ButtonAction.Activate
            }, 1);

            AddChildren(new Button((int) Buttons.Next, 0x15A4, 0x15A6, 0x15A5)
            {
                X = 610, Y = 445, ButtonAction = ButtonAction.Activate
            }, 1);
            _maleRadio.IsChecked = true;
            _humanRadio.IsChecked = true;
        }

        private PlayerMobile CreateCharacter(bool isFemale, RaceType race)
        {
            var character = new PlayerMobile(0);

            if (isFemale)
                character.Flags |= Flags.Female;
            character.Race = race;

            if (race == RaceType.GARGOYLE)
                character.Equipment[(int) Layer.Shirt] = CreateItem(0x4001, CurrentColorOption[Layer.Shirt].Item2);
            else if (isFemale)
            {
                character.Equipment[(int) Layer.Shoes] = CreateItem(0x1710, 0x0384);
                character.Equipment[(int) Layer.Pants] = CreateItem(0x1531, CurrentColorOption[Layer.Pants].Item2);
                character.Equipment[(int) Layer.Shirt] = CreateItem(0x1518, CurrentColorOption[Layer.Shirt].Item2);
            }
            else
            {
                character.Equipment[(int) Layer.Shoes] = CreateItem(0x1710, 0x0384);
                character.Equipment[(int) Layer.Pants] = CreateItem(0x152F, CurrentColorOption[Layer.Pants].Item2);
                character.Equipment[(int) Layer.Shirt] = CreateItem(0x1518, CurrentColorOption[Layer.Shirt].Item2);
            }

            return character;
        }

        private void UpdateEquipments()
        {
            bool isFemale = _femaleRadio.IsChecked;
            var race = GetSelectedRace();
            Layer layer;
            CharacterCreationValues.ComboContent content;
            _character.Hue = CurrentColorOption[Layer.Invalid].Item2;

            if (!isFemale && race != RaceType.ELF)
            {
                layer = Layer.Beard;
                content = CharacterCreationValues.GetFacialHairComboContent(race);
                _character.Equipment[(int) layer] = CreateItem(content.GetGraphic(CurrentOption[layer]), CurrentColorOption[layer].Item2);
            }

            layer = Layer.Hair;
            content = CharacterCreationValues.GetHairComboContent(isFemale, race);
            _character.Equipment[(int) layer] = CreateItem(content.GetGraphic(CurrentOption[layer]), CurrentColorOption[layer].Item2);
        }

        private void Race_ValueChanged(object sender, EventArgs e)
        {
            CurrentColorOption.Clear();
            HandleGenreChange();
        }

        private void Genre_ValueChanged(object sender, EventArgs e)
        {
            HandleGenreChange();
        }

        private void HandleGenreChange()
        {
            bool isFemale = _femaleRadio.IsChecked;
            var race = GetSelectedRace();
            CurrentOption[Layer.Beard] = 1;
            CurrentOption[Layer.Hair] = 1;

            if (_paperDoll != null)
                RemoveChildren(_paperDoll);

            if (_hairCombobox != null)
            {
                RemoveChildren(_hairCombobox);
                RemoveChildren(_hairLabel);
            }

            if (_facialCombobox != null)
            {
                RemoveChildren(_facialCombobox);
                RemoveChildren(_facialLabel);
            }

            foreach (var customPicker in Children.OfType<CustomColorPicker>().ToList())
                RemoveChildren(customPicker);
            CharacterCreationValues.ComboContent content;

            // Hair
            content = CharacterCreationValues.GetHairComboContent(isFemale, race);

            AddChildren(_hairLabel = new Label(FileManager.Cliloc.GetString(race == RaceType.GARGOYLE ? 1112309 : 3000121), false, 0x07F4, font: 9)
            {
                X = 98, Y = 142
            }, 1);
            AddChildren(_hairCombobox = new Combobox(97, 155, 120, content.Labels, CurrentOption[Layer.Hair]), 1);
            _hairCombobox.OnOptionSelected += Hair_OnOptionSelected;

            // Facial Hair
            if (!isFemale && race != RaceType.ELF)
            {
                content = CharacterCreationValues.GetFacialHairComboContent(race);

                AddChildren(_facialLabel = new Label(FileManager.Cliloc.GetString(race == RaceType.GARGOYLE ? 1112511 : 3000122), false, 0x07F4, font: 9)
                {
                    X = 98, Y = 186
                }, 1);
                AddChildren(_facialCombobox = new Combobox(97, 199, 120, content.Labels, CurrentOption[Layer.Beard]), 1);
                _facialCombobox.OnOptionSelected += Facial_OnOptionSelected;
            }
            else
            {
                _facialCombobox = null;
                _facialLabel = null;
            }

            // Skin
            ushort[] pallet = CharacterCreationValues.GetSkinPallet(race);
            AddCustomColorPicker(489, 141, pallet, Layer.Invalid, 3000183, 8, pallet.Length >> 3);

            // Shirt Color
            AddCustomColorPicker(489, 183, null, Layer.Shirt, 3000440, 10, 20);

            // Pants Color
            if (race != RaceType.GARGOYLE)
                AddCustomColorPicker(489, 225, null, Layer.Pants, 3000441, 10, 20);

            // Hair
            pallet = CharacterCreationValues.GetHairPallet(race);
            AddCustomColorPicker(489, 267, pallet, Layer.Hair, race == RaceType.GARGOYLE ? 1112322 : 3000184, 8, pallet.Length >> 3);

            if (!isFemale && race != RaceType.ELF)
            {
                // Facial
                pallet = CharacterCreationValues.GetHairPallet(race);
                AddCustomColorPicker(489, 309, pallet, Layer.Beard, race == RaceType.GARGOYLE ? 1112512 : 3000446, 8, pallet.Length >> 3);
            }

            _character = CreateCharacter(isFemale, race);
            UpdateEquipments();

            AddChildren(_paperDoll = new PaperDollInteractable(262, 135, _character)
            {
                AcceptMouseInput = false
            }, 1);
        }

        private void AddCustomColorPicker(int x, int y, ushort[] pallet, Layer layer, int clilocLabel, int rows, int columns)
        {
            CustomColorPicker colorPicker;

            AddChildren(colorPicker = new CustomColorPicker(layer, clilocLabel, pallet, rows, columns)
            {
                X = x, Y = y
            }, 1);

            if (!CurrentColorOption.ContainsKey(layer))
                CurrentColorOption[layer] = new Tuple<int, Hue>(0, colorPicker.HueSelected);
            else
                colorPicker.SetSelectedIndex(CurrentColorOption[layer].Item1);
            colorPicker.ColorSelected += ColorPicker_ColorSelected;
        }

        private void ColorPicker_ColorSelected(object sender, ColorSelectedEventArgs e)
        {
            CurrentColorOption[e.Layer] = new Tuple<int, Hue>(e.SelectedIndex, e.SelectedHue);

            if (e.Layer != Layer.Invalid)
                _character.Equipment[(int) e.Layer].Hue = e.SelectedHue;
            else
                _character.Hue = e.SelectedHue;
            _paperDoll.Update();
        }

        private void Facial_OnOptionSelected(object sender, int e)
        {
            CurrentOption[Layer.Beard] = e;
            UpdateEquipments();
            _paperDoll.Update();
        }

        private void Hair_OnOptionSelected(object sender, int e)
        {
            CurrentOption[Layer.Hair] = e;
            UpdateEquipments();
            _paperDoll.Update();
        }

        public override void OnButtonClick(int buttonID)
        {
            var charCreationGump = Engine.UI.GetByLocalSerial<CharCreationGump>();

            switch ((Buttons) buttonID)
            {
                case Buttons.FemaleButton:
                    _femaleRadio.IsChecked = true;

                    break;
                case Buttons.MaleButton:
                    _maleRadio.IsChecked = true;

                    break;
                case Buttons.HumanButton:
                    _humanRadio.IsChecked = true;

                    break;
                case Buttons.ElfButton:
                    _elfRadio.IsChecked = true;

                    break;
                case Buttons.GargoyleButton:
                    _gargoyleRadio.IsChecked = true;

                    break;
                case Buttons.Next:
                    _character.Name = _nameTextBox.Text;

                    if (ValidateCharacter(_character))
                        charCreationGump.SetCharacter(_character);

                    break;
                case Buttons.Prev:
                    charCreationGump.StepBack();

                    break;
            }

            base.OnButtonClick(buttonID);
        }

        private bool ValidateCharacter(PlayerMobile character)
        {
            if (string.IsNullOrEmpty(character.Name))
            {
                Engine.UI.GetByLocalSerial<CharCreationGump>().ShowMessage(FileManager.Cliloc.GetString(3000612));

                return false;
            }

            return true;
        }

        private RaceType GetSelectedRace()
        {
            if (_humanRadio.IsChecked)
                return RaceType.HUMAN;

            if (_elfRadio.IsChecked)
                return RaceType.ELF;

            if (_gargoyleRadio.IsChecked)
                return RaceType.GARGOYLE;

            return RaceType.HUMAN;
        }

        private Item CreateItem(int id, Hue hue)
        {
            if (id == 0)
                return null;

            return new Item(0)
            {
                Graphic = (ushort) id, Hue = hue
            };
        }

        private class ComboContent
        {
            private readonly int[] _ids;
            private readonly int[] _labels;

            public ComboContent(int[] labels, int[] ids)
            {
                _labels = labels;
                _ids = ids;
            }

            public string[] Labels => _labels.Select(o => FileManager.Cliloc.GetString(o)).ToArray();

            public int GetGraphic(int index)
            {
                return _ids[index];
            }
        }

        private enum Buttons
        {
            MaleButton,
            FemaleButton,
            HumanButton,
            ElfButton,
            GargoyleButton,
            Prev,
            Next
        }

        private class ColorSelectedEventArgs : EventArgs
        {
            public ColorSelectedEventArgs(Layer layer, ushort[] pallet, int selectedIndex)
            {
                Layer = layer;
                Pallet = pallet;
                SelectedIndex = selectedIndex;
            }

            public Layer Layer { get; }

            public ushort[] Pallet { get; }

            public int SelectedIndex { get; }

            public ushort SelectedHue => Pallet != null ? (ushort) (Pallet[SelectedIndex] + 1) : (ushort) 0;
        }

        private class CustomColorPicker : Control
        {
            private readonly int _cellW;
            private readonly int _cellH;
            private readonly int _columns;
            private readonly ColorPickerBox _colorPicker;
            private readonly ColorPickerBox _colorPickerBox;
            private readonly Layer _layer;

            public CustomColorPicker(Layer layer, int label, ushort[] pallet, int rows, int columns)
            {
                Width = 121;
                Height = 25;
                _cellW = 125 / columns;
                _cellH = 280 / rows;
                _columns = columns;
                _layer = layer;

                AddChildren(new Label(FileManager.Cliloc.GetString(label), false, 0x07F4, font: 9)
                {
                    X = 0, Y = 0
                });
                AddChildren(_colorPicker = new ColorPickerBox(1, 15, 1, 1, 121, 23, pallet));
                _colorPicker.MouseClick += ColorPicker_MouseClick;
                _colorPickerBox = new ColorPickerBox(489, 141, rows, columns, _cellW, _cellH, pallet);
                _colorPickerBox.MouseOver += _colorPicker_MouseMove;
            }

            public Hue HueSelected => (ushort) (_colorPickerBox.SelectedHue + 1);

            public event EventHandler<ColorSelectedEventArgs> ColorSelected;

            public void SetSelectedIndex(int index)
            {
                _colorPickerBox.SelectedIndex = index;
                ColorPickerBox_Selected(this, new EventArgs());
            }

            private void _colorPicker_MouseMove(object sender, MouseEventArgs e)
            {
                int column = e.X / _cellW;
                int row = e.Y / _cellH;
                var selectedIndex = row * _columns + column;
                ColorSelected?.Invoke(this, new ColorSelectedEventArgs(_layer, _colorPickerBox.GeneratedHues, selectedIndex));
            }

            private void ColorPickerBox_Selected(object sender, EventArgs e)
            {
                var color = (ushort) (_colorPickerBox.SelectedHue + 1);
                _colorPicker.SetHue(color);
                Parent?.RemoveChildren(_colorPickerBox);
            }

            private void ColorPicker_MouseClick(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButton.Left)
                {
                    Parent?.AddChildren(_colorPickerBox);
                    _colorPickerBox.ColorSelectedIndex += ColorPickerBox_Selected;
                }
            }
        }
    }
}