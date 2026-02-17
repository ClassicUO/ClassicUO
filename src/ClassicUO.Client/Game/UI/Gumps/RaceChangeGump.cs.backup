using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Network;
using System;
using System.Collections.Generic;

namespace ClassicUO.Game.UI.Gumps
{
    internal class RaceChangeGump : Gump
    {
        private bool isFemale { get; } = false;
        private RaceType selectedRace { get; } = RaceType.HUMAN;
        private PlayerMobile fakeMobile;
        private CustomPaperDollGump paperDollInteractable;
        private readonly Dictionary<Layer, Tuple<int, ushort>> CurrentColorOption = new Dictionary<Layer, Tuple<int, ushort>>();
        private readonly Dictionary<Layer, int> CurrentOption = new Dictionary<Layer, int>()
         {
            {
                Layer.Hair, 1
            },
            {
                Layer.Beard, 0
            }
        };
        private Item hair, beard;

        #region
        private ushort raceTextGraphic
        {
            get
            {
                switch (selectedRace)
                {
                    case RaceType.HUMAN:
                        return 0x702;
                    case RaceType.ELF:
                        return 0x705;
                    case RaceType.GARGOYLE:
                        return 0x7D4;
                }
                return 0;
            }
        }
        private int raceTextWidth
        {
            get
            {
                switch (selectedRace)
                {
                    case RaceType.HUMAN:
                        return 79;
                    case RaceType.ELF:
                        return 79;
                    case RaceType.GARGOYLE:
                        return 99;
                }
                return 0;
            }
        }
        private ushort genderTextGraphic
        {
            get
            {
                return (ushort)(isFemale ? 0x70D : 0x710);
            }
        }
        #endregion

        public RaceChangeGump(World world, bool isFemale, byte race) : base(world, 0, 0)
        {
            if (race <= 0 || race > (int)RaceType.GARGOYLE)
            {
                //Invalid race byte
                Dispose();
                return;
            }

            selectedRace = (RaceType)race;
            this.isFemale = isFemale;

            X = 50;
            Y = 50;
            CanMove = false;
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;

            BuildGump();

            WantUpdateSize = true;
        }

        private void BuildGump()
        {
            #region Background elements
            Add
            (
                new ResizePic(0x0E10)
                {
                    X = 0,
                    Y = 0,
                    Width = 595,
                    Height = 400
                },
                1
            ); //Main background

            Add
            (
                new ResizePic(0x0E10)
                {
                    X = 25,
                    Y = 45,
                    Width = 151,
                    Height = 310
                },
                1
            ); //Left side, hair style etc

            Add
            (
                new ResizePic(0x0E10)
                {
                    X = 419,
                    Y = 45,
                    Width = 151,
                    Height = 310
                },
                1
            ); //Right side tone/colors
            #endregion

            Add(new GumpPic(176 - raceTextWidth, 360, raceTextGraphic, 0)); //non-functional "Button" that says Human, Elf, or Gargoyle

            Add(new GumpPic(419, 360, genderTextGraphic, 0)); //non-functional "Button" that says Male or Female

            Button confirmButton;
            Add(confirmButton = new Button(0, 0x15A4, 0x15A6, 0x15A5) { X = 560, Y = 360 }); //Button to confirm, in classic client it is an arrow pointing right.
            confirmButton.MouseUp += ConfirmButton_MouseUp;

            //Add hair styles
            BuildHairStyles(40, 60);

            //Add color pickers
            BuildColorOptions(434, 60);

            //Create fake character
            CreateCharacter();
            UpdateEquipments();

            //Add the main paperdoll graphic
            Add(new GumpPic(185, 25, 0x708, 0));
            Add
            (
                paperDollInteractable = new CustomPaperDollGump(this, 210, 75, fakeMobile, hair, beard)
                {
                    AcceptMouseInput = false
                }
            );

            paperDollInteractable.RequestUpdate();

            Add(new GumpPic(211, 15, 0x769, 0)); //Character Race Changer text
        }

        /// <summary>
        /// Build hair options for race change gump.
        /// </summary>
        /// <param name="x">The starting point for these ui elements</param>
        /// <param name="y">The starting point for these ui elements</param>
        private void BuildHairStyles(int x, int y)
        {
            #region TextSetup
            bool isAsianLang = string.Compare(Settings.GlobalSettings.Language, "CHT", StringComparison.InvariantCultureIgnoreCase) == 0 ||
                string.Compare(Settings.GlobalSettings.Language, "KOR", StringComparison.InvariantCultureIgnoreCase) == 0 ||
                string.Compare(Settings.GlobalSettings.Language, "JPN", StringComparison.InvariantCultureIgnoreCase) == 0;

            bool unicode = isAsianLang;
            byte font = (byte)(isAsianLang ? 3 : 9);
            ushort hue = (ushort)(isAsianLang ? 0xFFFF : 0);
            #endregion

            CharacterCreationValues.ComboContent content = CharacterCreationValues.GetHairComboContent(isFemale, selectedRace);

            CurrentOption[Layer.Beard] = 0;
            CurrentOption[Layer.Hair] = 0;

            #region Hair style
            Add
            (
                new Label(Client.Game.UO.FileManager.Clilocs.GetString(selectedRace == RaceType.GARGOYLE ? 1112309 : 3000121), unicode, hue, font: font)
                {
                    X = x + 1,
                    Y = y
                }
            );
            y += 15;

            Combobox hair;
            Add
            (hair =
                new Combobox
                (
                    x,
                    y,
                    120,
                    content.Labels,
                    CurrentOption[Layer.Hair]
                )
            );
            hair.OnOptionSelected += (s, e) =>
            {
                CurrentOption[Layer.Hair] = e;
                UpdateEquipments();
                paperDollInteractable.RequestUpdate();
            };
            y += 30;
            #endregion

            #region Facial Hair
            if (!isFemale && selectedRace != RaceType.ELF)
            {
                content = CharacterCreationValues.GetFacialHairComboContent(selectedRace);

                Add
                (
                    new Label(Client.Game.UO.FileManager.Clilocs.GetString(selectedRace == RaceType.GARGOYLE ? 1112511 : 3000122), unicode, hue, font: font)
                    {
                        X = x + 1,
                        Y = y
                    }
                );
                y += 15;

                Combobox facialHair;
                Add
                (facialHair =
                    new Combobox
                    (
                        x,
                        y,
                        120,
                        content.Labels,
                        CurrentOption[Layer.Beard]
                    )
                );
                facialHair.OnOptionSelected += (s, e) =>
                {
                    CurrentOption[Layer.Beard] = e;
                    UpdateEquipments();
                    paperDollInteractable.RequestUpdate();
                };
            }
            #endregion
        }

        /// <summary>
        /// Build color options for race change gump
        /// </summary>
        /// <param name="x">The starting point for these ui elements</param>
        /// <param name="y">The starting point for these ui elements</param>
        private void BuildColorOptions(int x, int y)
        {
            ushort[] pallet = CharacterCreationValues.GetSkinPallet(selectedRace);

            AddCustomColorPicker
            (
                x,
                y,
                pallet,
                Layer.Invalid,
                3000183,
                8,
                pallet.Length >> 3
            );
            y += 42;

            // Hair
            pallet = CharacterCreationValues.GetHairPallet(selectedRace);

            AddCustomColorPicker
            (
                x,
                y,
                pallet,
                Layer.Hair,
                selectedRace == RaceType.GARGOYLE ? 1112322 : 3000184,
                8,
                pallet.Length >> 3
            );
            y += 42;

            if (!isFemale && selectedRace != RaceType.ELF)
            {
                // Facial
                pallet = CharacterCreationValues.GetHairPallet(selectedRace);

                AddCustomColorPicker
                (
                    x,
                    y,
                    pallet,
                    Layer.Beard,
                    selectedRace == RaceType.GARGOYLE ? 1112512 : 3000446,
                    8,
                    pallet.Length >> 3
                );
            }
        }

        /// <summary>
        /// Must be called *after* color options have been set up.
        /// </summary>
        private void CreateCharacter()
        {
            #region Create a fake character to use for the gump
            if (fakeMobile == null || fakeMobile.IsDestroyed)
            {
                fakeMobile = new PlayerMobile(World, 0);
            }

            LinkedObject first = fakeMobile.Items;

            while (first != null)
            {
                LinkedObject next = first.Next;

                World.RemoveItem((Item)first, true);

                first = next;
            }

            fakeMobile.Clear();
            fakeMobile.Race = selectedRace;
            fakeMobile.IsFemale = isFemale;

            if (isFemale)
            {
                fakeMobile.Flags |= Flags.Female;
            }
            else
            {
                fakeMobile.Flags &= ~Flags.Female;
            }
            #endregion

            if (selectedRace == RaceType.ELF)
            {
                fakeMobile.Graphic = (ushort)(isFemale ? 0x025E : 0x025D);
            }
            else
            {
                fakeMobile.Graphic = (ushort)(isFemale ? 0x0191 : 0x0190);
            }

            hair = CreateItem(0x4000_0000 + (int)Layer.Hair, 0, Layer.Hair);
            if (!isFemale && selectedRace != RaceType.ELF)
            {
                beard = CreateItem(0x4000_0000 + (int)Layer.Beard, 0, Layer.Beard);
            }
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
                    this,
                    layer,
                    clilocLabel,
                    pallet,
                    rows,
                    columns
                )
                {
                    X = x,
                    Y = y
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

            switch (e.Layer)
            {
                case Layer.Beard:
                    beard.Hue = e.SelectedHue;
                    break;
                case Layer.Hair:
                    hair.Hue = e.SelectedHue;
                    break;
                case Layer.Invalid:
                    fakeMobile.Hue = e.SelectedHue;
                    break;
            }

            paperDollInteractable.RequestUpdate();
        }

        private void ConfirmButton_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == Input.MouseButtonType.Left)
            {
                if (!isFemale && selectedRace != RaceType.ELF) //Has beard
                {
                    NetClient.Socket.Send_ChangeRaceRequest(
                        CurrentColorOption[Layer.Invalid].Item2,
                        (ushort)CharacterCreationValues.GetHairComboContent(isFemale, selectedRace).GetGraphic(CurrentOption[Layer.Hair]),
                        CurrentColorOption[Layer.Hair].Item2,
                        (ushort)CharacterCreationValues.GetFacialHairComboContent(selectedRace).GetGraphic(CurrentOption[Layer.Beard]),
                        CurrentColorOption[Layer.Beard].Item2
                    );
                }
                else //No beard
                {
                    NetClient.Socket.Send_ChangeRaceRequest(
                        CurrentColorOption[Layer.Invalid].Item2,
                        (ushort)CharacterCreationValues.GetHairComboContent(isFemale, selectedRace).GetGraphic(CurrentOption[Layer.Hair]),
                        CurrentColorOption[Layer.Hair].Item2,
                        0,
                        0
                    );
                }

                //Cleanup
                if (hair != null)
                {
                    World.RemoveItem(hair, true);
                }
                if (beard != null)
                {
                    World.RemoveItem(beard, true);
                }
                Dispose();
            }
        }

        private void UpdateEquipments()
        {
            Layer layer;
            CharacterCreationValues.ComboContent content;

            fakeMobile.Hue = CurrentColorOption[Layer.Invalid].Item2;

            if (beard != null)
            {
                layer = Layer.Beard;
                content = CharacterCreationValues.GetFacialHairComboContent(selectedRace);

                beard.Graphic = (ushort)content.GetGraphic(CurrentOption[layer]);
                beard.Hue = CurrentColorOption[layer].Item2;
            }

            layer = Layer.Hair;
            content = CharacterCreationValues.GetHairComboContent(isFemale, selectedRace);

            hair.Graphic = (ushort)content.GetGraphic(CurrentOption[layer]);
            hair.Hue = CurrentColorOption[layer].Item2;
        }

        private Item CreateItem(int id, ushort hue, Layer layer)
        {
            if (id == 0)
            {
                return null;
            }

            Item item = World.GetOrCreateItem(0x4000_0000 + (uint)layer); // use layer as unique Serial
            item.Graphic = (ushort)id;
            item.Hue = hue;
            item.Layer = layer;
            //

            return item;
        }

        #region Classes
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

            public ushort SelectedHue => Pallet != null && SelectedIndex >= 0 && SelectedIndex < Pallet.Length ? Pallet[SelectedIndex] : (ushort)0xFFFF;
        }

        private class CustomColorPicker : Control
        {
            //private readonly ColorBox _box;
            private readonly int _cellH;
            private readonly int _cellW;
            private readonly ColorBox _colorPicker;
            private ColorPickerBox _colorPickerBox;
            private readonly int _columns, _rows;
            private int _lastSelectedIndex;
            private readonly Layer _layer;
            private readonly ushort[] _pallet;
            private readonly RaceChangeGump _gump;

            public CustomColorPicker(RaceChangeGump gump, Layer layer, int label, ushort[] pallet, int rows, int columns)
            {
                _gump = gump;
                Width = 121;
                Height = 25;
                _cellW = 125 / columns;
                _cellH = 280 / rows;
                _columns = columns;
                _rows = rows;
                _layer = layer;
                _pallet = pallet;

                bool isAsianLang = string.Compare(Settings.GlobalSettings.Language, "CHT", StringComparison.InvariantCultureIgnoreCase) == 0 ||
                    string.Compare(Settings.GlobalSettings.Language, "KOR", StringComparison.InvariantCultureIgnoreCase) == 0 ||
                    string.Compare(Settings.GlobalSettings.Language, "JPN", StringComparison.InvariantCultureIgnoreCase) == 0;

                bool unicode = isAsianLang;
                byte font = (byte)(isAsianLang ? 3 : 9);
                ushort hue = (ushort)(isAsianLang ? 0xFFFF : 0);

                Add
                (
                    new Label(Client.Game.UO.FileManager.Clilocs.GetString(label), unicode, hue, font: font)
                    {
                        X = 0,
                        Y = 0
                    }
                );

                Add
                (
                    _colorPicker = new ColorBox(121, 23, (ushort)((pallet?[0] ?? 1) + 1))
                    {
                        X = 1,
                        Y = 15
                    }
                );

                _colorPicker.MouseUp += ColorPicker_MouseClick;
            }

            public ushort HueSelected => _colorPicker.Hue;

            public event EventHandler<ColorSelectedEventArgs> ColorSelected;

            public void SetSelectedIndex(int index)
            {
                if (_colorPickerBox != null)
                {
                    _colorPickerBox.SelectedIndex = index;

                    SetCurrentHue();
                }
            }

            private void SetCurrentHue()
            {
                _colorPicker.Hue = _colorPickerBox.SelectedHue;
                _lastSelectedIndex = _colorPickerBox.SelectedIndex;

                _colorPickerBox.Dispose();
            }

            private void ColorPickerBoxOnMouseUp(object sender, MouseEventArgs e)
            {
                int column = e.X / _cellW;
                int row = e.Y / _cellH;
                int selectedIndex = row * _columns + column;

                if (selectedIndex >= 0 && selectedIndex < _colorPickerBox.Hues.Length)
                {
                    ColorSelected?.Invoke(this, new ColorSelectedEventArgs(_layer, _colorPickerBox.Hues, selectedIndex));
                    SetCurrentHue();
                }
            }

            private void ColorPicker_MouseClick(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtonType.Left)
                {
                    _colorPickerBox?.Dispose();
                    _colorPickerBox = null;

                    if (_colorPickerBox == null)
                    {
                        _colorPickerBox = new ColorPickerBox
                        (
                            _gump.World,
                            485,
                            109,
                            _rows,
                            _columns,
                            _cellW,
                            _cellH,
                            _pallet
                        )
                        {
                            IsModal = true,
                            LayerOrder = UILayer.Over,
                            ModalClickOutsideAreaClosesThisControl = true,
                            ShowLivePreview = false,
                            SelectedIndex = _lastSelectedIndex
                        };

                        UIManager.Add(_colorPickerBox);

                        _colorPickerBox.ColorSelectedIndex += ColorPickerBoxOnColorSelectedIndex;
                        _colorPickerBox.MouseUp += ColorPickerBoxOnMouseUp;
                    }
                }
            }

            private void ColorPickerBoxOnColorSelectedIndex(object sender, EventArgs e)
            {
                ColorSelected?.Invoke(this, new ColorSelectedEventArgs(_layer, _colorPickerBox.Hues, _colorPickerBox.SelectedIndex));
            }
        }

        /// <summary>
        /// Partially custom paperdoll gump required, when in-game the fake character created gets automatically removed and breaks the original paperdoll gump.
        /// </summary>
        private class CustomPaperDollGump : PaperDollInteractable
        {
            private readonly Gump _gump;
            private readonly Mobile playerMobile;
            private Item hair;
            private Item beard;
            private bool requestUpdate = false;

            public CustomPaperDollGump(Gump gump, int x, int y, Mobile playerMobile, Item hair, Item beard) : base(x, y, playerMobile, new PaperDollGump(gump.World))
            {
                _gump = gump;
                this.playerMobile = playerMobile;
                this.hair = hair;
                this.beard = beard;
            }

            private void UpdateUI()
            {
                if (IsDisposed)
                {
                    return;
                }

                Mobile mobile = playerMobile;

                if (mobile == null)
                {
                    Dispose();

                    return;
                }

                Clear();

                #region Add the base gump - the semi-naked paper doll.
                ushort body;
                ushort hue = mobile.Hue;

                if (mobile.Graphic == 0x0191 || mobile.Graphic == 0x0193)
                {
                    body = 0x000D;
                }
                else if (mobile.Graphic == 0x025D)
                {
                    body = 0x000E;
                }
                else if (mobile.Graphic == 0x025E)
                {
                    body = 0x000F;
                }
                else if (mobile.Graphic == 0x029A || mobile.Graphic == 0x02B6)
                {
                    body = 0x029A;
                }
                else if (mobile.Graphic == 0x029B || mobile.Graphic == 0x02B7)
                {
                    body = 0x0299;
                }
                else if (mobile.Graphic == 0x04E5)
                {
                    body = 0xC835;
                }
                else if (mobile.Graphic == 0x03DB)
                {
                    body = 0x000C;
                    hue = 0x03EA;
                }
                else if (mobile.IsFemale)
                {
                    body = 0x000D;
                }
                else
                {
                    body = 0x000C;
                }

                // body
                Add
                (
                    new GumpPic(0, 0, body, hue)
                    {
                        IsPartialHue = true
                    }
                );


                if (mobile.Graphic == 0x03DB)
                {
                    Add
                    (
                        new GumpPic(0, 0, 0xC72B, mobile.Hue)
                        {
                            AcceptMouseInput = true,
                            IsPartialHue = true
                        }
                    );
                }
                #endregion

                // equipment

                if (hair != null)
                {
                    ushort id = GetAnimID(mobile.Graphic, hair.Graphic, hair.ItemData.AnimID, mobile.IsFemale);

                    Add
                    (
                        new GumpPicEquipment
                        (
                            _gump,
                            hair.Serial,
                            0,
                            0,
                            id,
                            (ushort)(hair.Hue & 0x3FFF),
                            Layer.Hair
                        )
                        {
                            AcceptMouseInput = true,
                            IsPartialHue = hair.ItemData.IsPartialHue,
                            CanLift = false
                        }
                    );
                }

                if (beard != null)
                {
                    ushort id = GetAnimID(mobile.Graphic, beard.Graphic, beard.ItemData.AnimID, mobile.IsFemale);

                    Add
                    (
                        new GumpPicEquipment
                        (
                            _gump,
                            beard.Serial,
                            0,
                            0,
                            id,
                            (ushort)(beard.Hue & 0x3FFF),
                            Layer.Beard
                        )
                        {
                            AcceptMouseInput = true,
                            IsPartialHue = beard.ItemData.IsPartialHue,
                            CanLift = false
                        }
                    );
                }
            }

            public new void RequestUpdate()
            {
                requestUpdate = true;
            }

            public override void Update()
            {
                if (requestUpdate)
                {
                    UpdateUI();
                    requestUpdate = false;
                }
            }
        }
        #endregion
    }
}
