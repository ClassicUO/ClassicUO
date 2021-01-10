﻿#region license

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
using System.Linq;
using ClassicUO.Data;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Gumps.CharCreation
{
    internal class CreateCharAppearanceGump : Gump
    {
        private PlayerMobile _character;
        private readonly RadioButton _elfRadio;
        private readonly RadioButton _femaleRadio;
        private readonly RadioButton _gargoyleRadio;
        private Combobox _hairCombobox, _facialCombobox;
        private Label _hairLabel, _facialLabel;
        private readonly RadioButton _humanRadio;
        private readonly RadioButton _maleRadio;
        private readonly StbTextBox _nameTextBox;
        private PaperDollInteractable _paperDoll;
        private readonly Button _nextButton;
        private readonly Dictionary<Layer, Tuple<int, ushort>> CurrentColorOption = new Dictionary<Layer, Tuple<int, ushort>>();
        private readonly Dictionary<Layer, int> CurrentOption = new Dictionary<Layer, int>
        {
            {
                Layer.Hair, 1
            },
            {
                Layer.Beard, 0
            }
        };

        public CreateCharAppearanceGump() : base(0, 0)
        {
            Add
            (
                new ResizePic(0x0E10)
                {
                    X = 82, Y = 125, Width = 151, Height = 310
                },
                1
            );

            Add(new GumpPic(280, 53, 0x0709, 0), 1);
            Add(new GumpPic(240, 73, 0x070A, 0), 1);

            Add
            (
                new GumpPicTiled
                (
                    248,
                    73,
                    215,
                    16,
                    0x070B
                ),
                1
            );

            Add(new GumpPic(463, 73, 0x070C, 0), 1);
            Add(new GumpPic(238, 98, 0x0708, 0), 1);

            Add
            (
                new ResizePic(0x0E10)
                {
                    X = 475, Y = 125, Width = 151, Height = 310
                },
                1
            );

            // Male/Female Radios
            Add
            (
                _maleRadio = new RadioButton(0, 0x0768, 0x0767)
                {
                    X = 425, Y = 435
                },
                1
            );

            _maleRadio.ValueChanged += Genre_ValueChanged;

            Add
            (
                _femaleRadio = new RadioButton(0, 0x0768, 0x0767)
                {
                    X = 425, Y = 455
                },
                1
            );

            _femaleRadio.ValueChanged += Genre_ValueChanged;

            Add
            (
                new Button((int) Buttons.MaleButton, 0x0710, 0x0712, 0x0711)
                {
                    X = 445, Y = 435, ButtonAction = ButtonAction.Activate
                },
                1
            );

            Add
            (
                new Button((int) Buttons.FemaleButton, 0x070D, 0x070F, 0x070E)
                {
                    X = 445, Y = 455, ButtonAction = ButtonAction.Activate
                },
                1
            );

            Add
            (
                _nameTextBox = new StbTextBox
                (
                    5,
                    16,
                    200,
                    false,
                    hue: 1,
                    style: FontStyle.Fixed
                )
                {
                    X = 257, Y = 65, Width = 200, Height = 20
                    //ValidationRules = (uint) (TEXT_ENTRY_RULES.LETTER | TEXT_ENTRY_RULES.SPACE)
                },
                1
            );

            // Races
            Add
            (
                _humanRadio = new RadioButton(1, 0x0768, 0x0767)
                {
                    X = 180, Y = 435
                },
                1
            );

            Add
            (
                new Button((int) Buttons.HumanButton, 0x0702, 0x0704, 0x0703)
                {
                    X = 200, Y = 435, ButtonAction = ButtonAction.Activate
                },
                1
            );

            _humanRadio.ValueChanged += Race_ValueChanged;

            Add
            (
                _elfRadio = new RadioButton(1, 0x0768, 0x0767)
                {
                    X = 180, Y = 455
                },
                1
            );

            Add
            (
                new Button((int) Buttons.ElfButton, 0x0705, 0x0707, 0x0706)
                {
                    X = 200, Y = 455, ButtonAction = ButtonAction.Activate
                },
                1
            );

            _elfRadio.ValueChanged += Race_ValueChanged;

            if (Client.Version >= ClientVersion.CV_60144)
            {
                Add
                (
                    _gargoyleRadio = new RadioButton(1, 0x0768, 0x0767)
                    {
                        X = 60, Y = 435
                    },
                    1
                );

                Add
                (
                    new Button((int) Buttons.GargoyleButton, 0x07D3, 0x07D5, 0x07D4)
                    {
                        X = 80, Y = 435, ButtonAction = ButtonAction.Activate
                    },
                    1
                );

                _gargoyleRadio.ValueChanged += Race_ValueChanged;
            }

            // Prev/Next
            Add
            (
                new Button((int) Buttons.Prev, 0x15A1, 0x15A3, 0x15A2)
                {
                    X = 586, Y = 445, ButtonAction = ButtonAction.Activate
                },
                1
            );

            Add
            (
                _nextButton = new Button((int) Buttons.Next, 0x15A4, 0x15A6, 0x15A5)
                {
                    X = 610, Y = 445, ButtonAction = ButtonAction.Activate
                },
                1
            );

            _maleRadio.IsChecked = true;
            _humanRadio.IsChecked = true;
        }

        private void CreateCharacter(bool isFemale, RaceType race)
        {
            if (_character == null)
            {
                _character = new PlayerMobile(1);
                World.Mobiles.Add(_character);
            }

            LinkedObject first = _character.Items;

            while (first != null)
            {
                LinkedObject next = first.Next;

                World.RemoveItem((Item) first, true);

                first = next;
            }

            _character.Clear();
            _character.Race = race;
            _character.IsFemale = isFemale;

            if (isFemale)
            {
                _character.Flags |= Flags.Female;
            }

            switch (race)
            {
                case RaceType.GARGOYLE:
                    _character.Graphic = isFemale ? (ushort) 0x029B : (ushort) 0x029A;

                    Item it = CreateItem(0x4001, CurrentColorOption[Layer.Shirt].Item2, Layer.Robe);

                    _character.PushToBack(it);

                    break;

                case RaceType.ELF when isFemale:
                    _character.Graphic = 0x025E;
                    it = CreateItem(0x1710, 0x0384, Layer.Shoes);
                    _character.PushToBack(it);

                    it = CreateItem(0x1531, CurrentColorOption[Layer.Pants].Item2, Layer.Skirt);

                    _character.PushToBack(it);

                    it = CreateItem(0x1518, CurrentColorOption[Layer.Shirt].Item2, Layer.Shirt);

                    _character.PushToBack(it);

                    break;

                case RaceType.ELF:
                    _character.Graphic = 0x025D;
                    it = CreateItem(0x1710, 0x0384, Layer.Shoes);
                    _character.PushToBack(it);

                    it = CreateItem(0x152F, CurrentColorOption[Layer.Pants].Item2, Layer.Pants);

                    _character.PushToBack(it);

                    it = CreateItem(0x1518, CurrentColorOption[Layer.Shirt].Item2, Layer.Shirt);

                    _character.PushToBack(it);

                    break;

                default:

                {
                    if (isFemale)
                    {
                        _character.Graphic = 0x0191;
                        it = CreateItem(0x1710, 0x0384, Layer.Shoes);
                        _character.PushToBack(it);

                        it = CreateItem(0x1531, CurrentColorOption[Layer.Pants].Item2, Layer.Skirt);

                        _character.PushToBack(it);

                        it = CreateItem(0x1518, CurrentColorOption[Layer.Shirt].Item2, Layer.Shirt);

                        _character.PushToBack(it);
                    }
                    else
                    {
                        _character.Graphic = 0x0190;
                        it = CreateItem(0x1710, 0x0384, Layer.Shoes);
                        _character.PushToBack(it);

                        it = CreateItem(0x152F, CurrentColorOption[Layer.Pants].Item2, Layer.Pants);

                        _character.PushToBack(it);

                        it = CreateItem(0x1518, CurrentColorOption[Layer.Shirt].Item2, Layer.Shirt);

                        _character.PushToBack(it);
                    }

                    break;
                }
            }
        }

        private void UpdateEquipments()
        {
            bool isFemale = _femaleRadio.IsChecked;
            RaceType race = GetSelectedRace();
            Layer layer;
            CharacterCreationValues.ComboContent content;

            _character.Hue = CurrentColorOption[Layer.Invalid].Item2;

            if (!isFemale && race != RaceType.ELF)
            {
                layer = Layer.Beard;
                content = CharacterCreationValues.GetFacialHairComboContent(race);

                Item iti = CreateItem(content.GetGraphic(CurrentOption[layer]), CurrentColorOption[layer].Item2, layer);

                _character.PushToBack(iti);
            }

            layer = Layer.Hair;
            content = CharacterCreationValues.GetHairComboContent(isFemale, race);

            Item it = CreateItem(content.GetGraphic(CurrentOption[layer]), CurrentColorOption[layer].Item2, layer);

            _character.PushToBack(it);
        }

        private void Race_ValueChanged(object sender, EventArgs e)
        {
            CurrentColorOption.Clear();
            HandleGenreChange();
            RaceType race = GetSelectedRace();
            CharacterListFlags flags = World.ClientFeatures.Flags;
            LockedFeatureFlags locks = World.ClientLockedFeatures.Flags;

            bool allowElf = (flags & CharacterListFlags.CLF_ELVEN_RACE) != 0 && (locks & LockedFeatureFlags.MondainsLegacy) != 0;

            bool allowGarg = (locks & LockedFeatureFlags.StygianAbyss) != 0;

            if (race == RaceType.ELF && !allowElf)
            {
                _nextButton.IsEnabled = false;
                _nextButton.Hue = 944;
            }
            else if (race == RaceType.GARGOYLE && !allowGarg)
            {
                _nextButton.IsEnabled = false;
                _nextButton.Hue = 944;
            }
            else
            {
                _nextButton.IsEnabled = true;
                _nextButton.Hue = 0;
            }
        }

        private void Genre_ValueChanged(object sender, EventArgs e)
        {
            HandleGenreChange();
        }

        private void HandleGenreChange()
        {
            bool isFemale = _femaleRadio.IsChecked;
            RaceType race = GetSelectedRace();
            CurrentOption[Layer.Beard] = 0;
            CurrentOption[Layer.Hair] = 1;

            if (_paperDoll != null)
            {
                Remove(_paperDoll);
            }

            if (_hairCombobox != null)
            {
                Remove(_hairCombobox);
                Remove(_hairLabel);
            }

            if (_facialCombobox != null)
            {
                Remove(_facialCombobox);
                Remove(_facialLabel);
            }

            foreach (CustomColorPicker customPicker in Children.OfType<CustomColorPicker>().ToList())
            {
                Remove(customPicker);
            }

            // Hair
            CharacterCreationValues.ComboContent content = CharacterCreationValues.GetHairComboContent(isFemale, race);

            Add
            (
                _hairLabel = new Label(ClilocLoader.Instance.GetString(race == RaceType.GARGOYLE ? 1112309 : 3000121), false, 0, font: 9)
                {
                    X = 98, Y = 142
                },
                1
            );

            Add
            (
                _hairCombobox = new Combobox
                (
                    97,
                    155,
                    120,
                    content.Labels,
                    CurrentOption[Layer.Hair]
                ),
                1
            );

            _hairCombobox.OnOptionSelected += Hair_OnOptionSelected;

            // Facial Hair
            if (!isFemale && race != RaceType.ELF)
            {
                content = CharacterCreationValues.GetFacialHairComboContent(race);

                Add
                (
                    _facialLabel = new Label(ClilocLoader.Instance.GetString(race == RaceType.GARGOYLE ? 1112511 : 3000122), false, 0, font: 9)
                    {
                        X = 98, Y = 186
                    },
                    1
                );

                Add
                (
                    _facialCombobox = new Combobox
                    (
                        97,
                        199,
                        120,
                        content.Labels,
                        CurrentOption[Layer.Beard]
                    ),
                    1
                );

                _facialCombobox.OnOptionSelected += Facial_OnOptionSelected;
            }
            else
            {
                _facialCombobox = null;
                _facialLabel = null;
            }

            // Skin
            ushort[] pallet = CharacterCreationValues.GetSkinPallet(race);

            AddCustomColorPicker
            (
                489,
                141,
                pallet,
                Layer.Invalid,
                3000183,
                8,
                pallet.Length >> 3
            );

            // Shirt Color
            AddCustomColorPicker
            (
                489,
                183,
                null,
                Layer.Shirt,
                3000440,
                10,
                20
            );

            // Pants Color
            if (race != RaceType.GARGOYLE)
            {
                AddCustomColorPicker
                (
                    489,
                    225,
                    null,
                    Layer.Pants,
                    3000441,
                    10,
                    20
                );
            }

            // Hair
            pallet = CharacterCreationValues.GetHairPallet(race);

            AddCustomColorPicker
            (
                489,
                267,
                pallet,
                Layer.Hair,
                race == RaceType.GARGOYLE ? 1112322 : 3000184,
                8,
                pallet.Length >> 3
            );

            if (!isFemale && race != RaceType.ELF)
            {
                // Facial
                pallet = CharacterCreationValues.GetHairPallet(race);

                AddCustomColorPicker
                (
                    489,
                    309,
                    pallet,
                    Layer.Beard,
                    race == RaceType.GARGOYLE ? 1112512 : 3000446,
                    8,
                    pallet.Length >> 3
                );
            }

            CreateCharacter(isFemale, race);

            UpdateEquipments();

            Add
            (
                _paperDoll = new PaperDollInteractable(262, 135, _character, null)
                {
                    AcceptMouseInput = false
                },
                1
            );

            _paperDoll.Update();
        }

        private void AddCustomColorPicker
        (
            int x,
            int y,
            ushort[] pallet,
            Layer layer,
            int clilocLabel,
            int rows,
            int columns
        )
        {
            CustomColorPicker colorPicker;

            Add
            (
                colorPicker = new CustomColorPicker
                (
                    layer,
                    clilocLabel,
                    pallet,
                    rows,
                    columns
                )
                {
                    X = x, Y = y
                },
                1
            );

            if (!CurrentColorOption.ContainsKey(layer))
            {
                CurrentColorOption[layer] = new Tuple<int, ushort>(0, colorPicker.HueSelected);
            }
            else
            {
                colorPicker.SetSelectedIndex(CurrentColorOption[layer].Item1);
            }

            colorPicker.ColorSelected += ColorPicker_ColorSelected;
        }

        private void ColorPicker_ColorSelected(object sender, ColorSelectedEventArgs e)
        {
            if (e.SelectedIndex == 0xFFFF)
            {
                return;
            }

            CurrentColorOption[e.Layer] = new Tuple<int, ushort>(e.SelectedIndex, e.SelectedHue);

            if (e.Layer != Layer.Invalid)
            {
                Item item;

                if (_character.Race == RaceType.GARGOYLE && e.Layer == Layer.Shirt)
                {
                    item = _character.FindItemByLayer(Layer.Robe);
                }
                else
                {
                    item = _character.FindItemByLayer(e.Layer);
                }

                if (item != null)
                {
                    item.Hue = e.SelectedHue;
                }
            }
            else
            {
                _character.Hue = e.SelectedHue;
            }

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
            CharCreationGump charCreationGump = UIManager.GetGump<CharCreationGump>();

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
                    {
                        charCreationGump.SetCharacter(_character);
                    }

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
                UIManager.GetGump<CharCreationGump>()?.ShowMessage(ClilocLoader.Instance.GetString(3000612));

                return false;
            }

            return true;
        }

        private RaceType GetSelectedRace()
        {
            if (_humanRadio.IsChecked)
            {
                return RaceType.HUMAN;
            }

            if (_elfRadio != null && _elfRadio.IsChecked)
            {
                return RaceType.ELF;
            }

            if (_gargoyleRadio != null && _gargoyleRadio.IsChecked)
            {
                return RaceType.GARGOYLE;
            }

            return RaceType.HUMAN;
        }

        private Item CreateItem(int id, ushort hue, Layer layer)
        {
            Item existsItem = _character.FindItemByLayer(layer);

            if (existsItem != null)
            {
                World.RemoveItem(existsItem, true);
                _character.Remove(existsItem);
            }

            if (id == 0)
            {
                return null;
            }

            // This is a workaround to avoid to see naked guy
            // We are simulating server objects into World.Items map.
            Item item = World.GetOrCreateItem(0x4000_0000 + (uint) layer); // use layer as unique Serial
            _character.Remove(item);
            item.Graphic = (ushort) id;
            item.Hue = hue;
            item.Layer = layer;
            item.Container = _character;
            //

            return item;
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

            private ushort[] Pallet { get; }

            public int SelectedIndex { get; }

            public ushort SelectedHue => Pallet != null && SelectedIndex >= 0 && SelectedIndex < Pallet.Length ? (ushort) (Pallet[SelectedIndex] + 1) : (ushort) 0xFFFF;
        }

        private class CustomColorPicker : Control
        {
            //private readonly ColorBox _box;
            private readonly int _cellH;
            private readonly int _cellW;
            private readonly ColorPickerBox _colorPicker;
            private ColorPickerBox _colorPickerBox;
            private readonly int _columns, _rows;
            private readonly Layer _layer;
            private readonly ushort[] _pallet;

            public CustomColorPicker(Layer layer, int label, ushort[] pallet, int rows, int columns)
            {
                Width = 121;
                Height = 25;
                _cellW = 125 / columns;
                _cellH = 280 / rows;
                _columns = columns;
                _rows = rows;
                _layer = layer;
                _pallet = pallet;

                Add
                (
                    new Label(ClilocLoader.Instance.GetString(label), false, 0, font: 9)
                    {
                        X = 0,
                        Y = 0
                    }
                );

                Add
                (
                    _colorPicker = new ColorPickerBox
                    (
                        1,
                        15,
                        1,
                        1,
                        121,
                        23,
                        pallet
                    )
                );

                _colorPicker.MouseUp += ColorPicker_MouseClick;

                _colorPickerBox = new ColorPickerBox
                (
                    489,
                    141,
                    _rows,
                    _columns,
                    _cellW,
                    _cellH,
                    _pallet
                )
                {
                    IsModal = true,
                    LayerOrder = UILayer.Over,
                    ModalClickOutsideAreaClosesThisControl = true
                };

                _colorPickerBox.MouseUp += ColorPickerBoxOnMouseUp;
            }

            public ushort HueSelected => (ushort) (_colorPickerBox.SelectedHue + 1);

            private void ColorPickerBoxOnMouseUp(object sender, MouseEventArgs e)
            {
                int column = e.X / _cellW;
                int row = e.Y / _cellH;
                int selectedIndex = row * _columns + column;

                if (selectedIndex >= 0 && selectedIndex < _colorPickerBox.Hues.Length)
                {
                    ColorSelected?.Invoke(this, new ColorSelectedEventArgs(_layer, _colorPickerBox.Hues, selectedIndex));
                }
            }

            public event EventHandler<ColorSelectedEventArgs> ColorSelected;

            public void SetSelectedIndex(int index)
            {
                _colorPickerBox.SelectedIndex = index;
                ColorPickerBox_Selected(this, EventArgs.Empty);
            }

            private void ColorPickerBox_Selected(object sender, EventArgs e)
            {
                _colorPicker.SetHue(_colorPickerBox.SelectedHue);
                _colorPickerBox?.Dispose();
                //_colorPickerBox = null;
            }

            private void ColorPicker_MouseClick(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtonType.Left)
                {
                    //Parent?.Add(_colorPickerBox);
                    _colorPickerBox?.Dispose();
                    _colorPickerBox = null;

                    _colorPickerBox = new ColorPickerBox
                    (
                        489,
                        141,
                        _rows,
                        _columns,
                        _cellW,
                        _cellH,
                        _pallet
                    )
                    {
                        IsModal = true,
                        LayerOrder = UILayer.Over,
                        ModalClickOutsideAreaClosesThisControl = true
                    };

                    _colorPickerBox.MouseUp += ColorPickerBoxOnMouseUp;
                    _colorPickerBox.ColorSelectedIndex += ColorPickerBox_Selected;

                    UIManager.Add(_colorPickerBox);
                }
            }
        }
    }
}