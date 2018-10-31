using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;
using System;
using System.Collections.Generic;
using System.Runtime;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Gumps.UIGumps.CharCreation
{
    class CreateCharAppearanceGump : Gump
    {
        private class ComboContent
        {
            private int[] _labels;
            private int[] _ids;

            public string[] Labels => _labels.Select(o => IO.Resources.Cliloc.GetString(o)).ToArray();
            public int GetGraphic(int index) => _ids[index];

            public ComboContent(int[] labels, int[] ids)
            {
                _labels = labels;
                _ids = ids;
            }
        }

        private static readonly ComboContent MALE_HAIR = new ComboContent(
            new int[] { 3000340, 3000341, 3000342, 3000343, 3000344, 3000345, 3000346, 3000347, 3000348, 3000349 },
            new int[] { 0, 8251, 8252, 8253, 8260, 8261, 8266, 8263, 8264, 8265 });

        private static readonly ComboContent FACIAL_HAIR = new ComboContent(
            new int[] { 3000340, 3000351, 3000352, 3000353, 3000354, 1011060, 1011061, 3000357 },
            new int[] { 0, 8256, 8254, 8255, 8257, 8267, 8268, 8269 });

        private static readonly ComboContent FEMALE_HAIR = new ComboContent(
            new int[] { 3000340, 3000341, 3000342, 3000343, 3000344, 3000345, 3000346, 3000347, 3000349, 3000350 },
            new int[] { 0, 8251, 8252, 8253, 8260, 8261, 8266, 8263, 8265, 8262 });

        private static readonly Dictionary<Genre, Dictionary<Layer, ComboContent>> GENRE_MAPPING = new Dictionary<Genre, Dictionary<Layer, ComboContent>>
        {
            { Genre.Male, new Dictionary<Layer, ComboContent> { { Layer.Hair, MALE_HAIR }, { Layer.Beard, FACIAL_HAIR } } },
            { Genre.Female, new Dictionary<Layer, ComboContent> { { Layer.Hair, FEMALE_HAIR } } }
        };

        private Dictionary<Layer, int> CurrentOption = new Dictionary<Layer, int>()
        {
            { Layer.Hair, 1 }, { Layer.Beard, 1 }
        };

        private Dictionary<Layer, Tuple<int, Hue>> CurrentColorOption = new Dictionary<Layer, Tuple<int, Hue>>();
        
        private PlayerMobile _character;
        private RadioButton _maleRadio, _femaleRadio;
        private Combobox _hairCombobox, _facialCombobox;
        private Label _hairLabel, _facialLabel;
        private PaperDollInteractable _paperDoll;
        TextBox _nameTextBox;

        public CreateCharAppearanceGump()
            : base(0, 0)
        {
            AddChildren(new ResizePic(0x0E10) { X = 82, Y = 125, Width = 151, Height = 310 });
            AddChildren(new GumpPic(280, 53, 0x0709, 0));
            AddChildren(new GumpPic(240, 73, 0x070A, 0));
            AddChildren(new GumpPicTiled(248, 73, 215, 16, 0x070B));
            AddChildren(new GumpPic(463, 73, 0x070C, 0));
            
            AddChildren(new GumpPic(238, 98, 0x0708, 0));
            AddChildren(new ResizePic(0x0E10) { X = 475, Y = 125, Width = 151, Height = 310 });
            
            // Male/Female Radios
            AddChildren(_maleRadio = new RadioButton(0, 0x0768, 0x0767) { X = 425, Y = 435 });
            _maleRadio.ValueChanged += genre_ValueChanged;
            AddChildren(_femaleRadio = new RadioButton(0, 0x0768, 0x0767) { X = 425, Y = 455 });
            _femaleRadio.ValueChanged += genre_ValueChanged;

            AddChildren(new Button((int)Buttons.MaleButton, 0x0710, 0x0712, 0x0711) { X = 445, Y = 435, ButtonAction = ButtonAction.Activate });
            AddChildren(new Button((int)Buttons.FemaleButton, 0x070D, 0x070F, 0x070E) { X = 445, Y = 455, ButtonAction = ButtonAction.Activate });
            
            AddChildren(_nameTextBox = new TextBox(5, 32, 300, 300, false, hue: 1) { X = 257, Y = 65, Width = 300, Height = 20 });
            _nameTextBox.SetText(string.Empty);

            AddChildren(new Button((int)Buttons.Prev, 0x15A1, 0x15A3, over: 0x15A2) { X = 586, Y = 445, ButtonAction = ButtonAction.Activate });
            AddChildren(new Button((int)Buttons.Next, 0x15A4, 0x15A6, over: 0x15A5) { X = 610, Y = 445, ButtonAction = ButtonAction.Activate });

            _maleRadio.IsChecked = true;
        }
        
        private PlayerMobile CreateCharacter(Genre genre, RaceType race)
        {
            var character  = new PlayerMobile(0);
            if (genre == Genre.Female)
                character.Flags |= Flags.Female;

            character.Race = race;

            if (genre == Genre.Female)
            {
                character.Equipment[(int)Layer.Shoes] = CreateItem(0x1710, 0x0384);
                character.Equipment[(int)Layer.Pants] = CreateItem(0x1531, CurrentColorOption[Layer.Pants].Item2);
                character.Equipment[(int)Layer.Shirt] = CreateItem(0x1518, CurrentColorOption[Layer.Shirt].Item2);
            }
            else
            {
                character.Equipment[(int)Layer.Shoes] = CreateItem(0x1710, 0x0384);
                character.Equipment[(int)Layer.Pants] = CreateItem(0x152F, CurrentColorOption[Layer.Pants].Item2);
                character.Equipment[(int)Layer.Shirt] = CreateItem(0x1518, CurrentColorOption[Layer.Shirt].Item2);
            }
            
            return character;
        }

        private void UpdateEquipments()
        {
            Genre genre = _maleRadio.IsChecked ? Genre.Male : Genre.Female;
            Layer layer;
            ComboContent content;

            _character.Hue = CurrentColorOption[Layer.Invalid].Item2;

            if (genre == Genre.Male)
            {
                layer = Layer.Beard;
                content = GENRE_MAPPING[genre][layer];
                _character.Equipment[(int)layer] = CreateItem(content.GetGraphic(CurrentOption[layer]), CurrentColorOption[layer].Item2);
            }

            layer = Layer.Hair;
            content = GENRE_MAPPING[genre][layer];
            _character.Equipment[(int)layer] = CreateItem(content.GetGraphic(CurrentOption[layer]), CurrentColorOption[layer].Item2);
        }
        
        private void genre_ValueChanged(object sender, EventArgs e)
        {
            HandleGenreChange();
        }
        
        private void HandleGenreChange()
        {
            Genre genre = _maleRadio.IsChecked ? Genre.Male : Genre.Female;
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
            
            ComboContent content;

            // Hair
            content = GENRE_MAPPING[genre][Layer.Hair];
            AddChildren(_hairLabel = new Label(IO.Resources.Cliloc.GetString(3000121), false, 0x07F4, font: 9) { X = 98, Y = 142 });
            AddChildren(_hairCombobox = new Combobox(97, 155, 120, content.Labels, CurrentOption[Layer.Hair]));
            _hairCombobox.OnOptionSelected += Hair_OnOptionSelected;

            // Facial Hair
            if (genre == Genre.Male)
            {
                content = GENRE_MAPPING[genre][Layer.Beard];
                AddChildren(_facialLabel = new Label(IO.Resources.Cliloc.GetString(3000122), false, 0x07F4, font: 9) { X = 98, Y = 186 });
                AddChildren(_facialCombobox = new Combobox(97, 199, 120, content.Labels, CurrentOption[Layer.Beard]));
                _facialCombobox.OnOptionSelected += Facial_OnOptionSelected;
            }
            else
            {
                _facialCombobox = null;
                _facialLabel = null;
            }
            
            // Skin
            ushort[] pallet = Data.CharacterCreationValues.HumanSkinTone;
            AddCustomColorPicker(489, 141, pallet, Layer.Invalid, 3000183, 8, pallet.Length / 8);

            // Shirt Color
            AddCustomColorPicker(489, 183, null, Layer.Shirt, 3000440, 10, 20);


            // Pants Color
            AddCustomColorPicker(489, 225, null, Layer.Pants, 3000441, 10, 20);
            
            // Hair
            pallet = Data.CharacterCreationValues.HumanHairColor;
            AddCustomColorPicker(489, 267, pallet, Layer.Hair, 3000184, 8, pallet.Length / 8);
            
            if (genre == Genre.Male)
            {
                // Facial
                pallet = Data.CharacterCreationValues.HumanHairColor;
                AddCustomColorPicker(489, 309, pallet, Layer.Beard, 3000446, 8, pallet.Length / 8);
            }

            _character = CreateCharacter(genre, RaceType.HUMAN);
            UpdateEquipments();
            AddChildren(_paperDoll = new PaperDollInteractable(262, 135, _character) { AcceptMouseInput = false });
        }

        private void AddCustomColorPicker(int x, int y, ushort[] pallet, Layer layer, int clilocLabel, int rows, int columns)
        {
            CustomColorPicker colorPicker;
            AddChildren(colorPicker = new CustomColorPicker(layer, clilocLabel, pallet, rows, columns)
            {
                X = x,
                Y = y
            });
            if (!CurrentColorOption.ContainsKey(layer))
                CurrentColorOption[layer] = new Tuple<int, Hue>(0, colorPicker.HueSelected);
            else
                colorPicker.SetSelectedIndex(CurrentColorOption[layer].Item1);

            colorPicker.ColorSelected += Teste_ColorSelected;
        }

        private void Teste_ColorSelected(object sender, ColorSelectedEventArgs e)
        {
            CurrentColorOption[e.Layer] = new Tuple<int, Hue>(e.SelectedIndex, e.SelectedHue);

            if (e.Layer != Layer.Invalid)
                _character.Equipment[(int)e.Layer].Hue = e.SelectedHue;
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
            switch ((Buttons)buttonID)
            {
                case Buttons.FemaleButton:
                    _femaleRadio.IsChecked = true;
                    break;
                case Buttons.MaleButton:
                    _maleRadio.IsChecked = true;
                    break;
                case Buttons.Next:
                    _character.Name = _nameTextBox.Text;
                    Service.Get<CharCreationGump>().SetCharacter(_character);
                    break;
            }

            base.OnButtonClick(buttonID);
        }
        
        private Item CreateItem(int id, Hue hue)
        {
            if (id == 0)
                return null;

            return new Item(0)
            {
                Graphic = (ushort)id,
                Hue = hue
            };
        }

        private enum Buttons
        {
            MaleButton,
            FemaleButton,
            Prev,
            Next
        }

        private enum Genre
        {
            Male, Female
        }

        private class ColorSelectedEventArgs: EventArgs
        {
            public Layer Layer { get; set; }
            public ushort[] Pallet { get; private set; }
            public int SelectedIndex { get; private set; }
            public ushort SelectedHue => Pallet != null ? (ushort)(Pallet[SelectedIndex] + 1) : (ushort)0;

            public ColorSelectedEventArgs(Layer layer, ushort[] pallet, int selectedIndex)
            {
                Layer = layer;
                Pallet = pallet;
                SelectedIndex = selectedIndex;
            }
        }

        private class CustomColorPicker: GumpControl
        {
            Layer _layer;
            int _cellW, _cellH, _columns;
            ColorPickerBox _colorPicker;
            ColorPickerBox _colorPickerBox;

            public Hue HueSelected => (ushort)(_colorPickerBox.SelectedHue + 1);

            public event EventHandler<ColorSelectedEventArgs> ColorSelected;

            public CustomColorPicker(Layer layer, int label, ushort[] pallet, int rows, int columns)
            {
                Width = 121;
                Height = 25;
                _cellW = 125 / columns;
                _cellH = 280 / rows;
                _columns = columns;
                _layer = layer;
                
                AddChildren(new Label(IO.Resources.Cliloc.GetString(label), false, 0x07F4, font: 9) { X = 0, Y = 0 });
                AddChildren(_colorPicker = new ColorPickerBox(1, 15, 1, 1, 121, 23, pallet));
                _colorPicker.MouseClick += ColorPicker_MouseClick;

                _colorPickerBox = new ColorPickerBox(489, 141, rows, columns, _cellW, _cellH, pallet);
                _colorPickerBox.MouseEnter += _colorPicker_MouseMove;
            }

            public void SetSelectedIndex(int index)
            {
                _colorPickerBox.SelectedIndex = index;
                ColorPickerBox_Selected(this, new EventArgs());
            }

            private void _colorPicker_MouseMove(object sender, Input.MouseEventArgs e)
            {
                int column = e.X / _cellW;
                int row = e.Y / _cellH;

                var selectedIndex = row * _columns + column;
                ColorSelected?.Invoke(this, new ColorSelectedEventArgs(_layer, _colorPickerBox.GeneratedHues, selectedIndex));
            }

            private void ColorPickerBox_Selected(object sender, EventArgs e)
            {
                var color = (ushort)(_colorPickerBox.SelectedHue + 1);
                _colorPicker.SetHue(color);
                Parent?.RemoveChildren(_colorPickerBox);
            }
            
            private void ColorPicker_MouseClick(object sender, Input.MouseEventArgs e)
            {
                if (e.Button == Input.MouseButton.Left)
                {
                    Parent?.AddChildren(_colorPickerBox);
                    _colorPickerBox.ColorSelectedIndex += ColorPickerBox_Selected;
                }
            }
        }
    }
}
