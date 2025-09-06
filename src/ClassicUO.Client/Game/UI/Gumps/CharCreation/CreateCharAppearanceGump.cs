#region license

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
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Game.UI.Gumps.Login;
using System.IO;
using Microsoft.Xna.Framework;
using Cyotek.Drawing.BitmapFont;
using ClassicUO.Resources;

namespace ClassicUO.Game.UI.Gumps.CharCreation
{
    internal class CreateCharAppearanceGump : Gump
    {
        struct CharacterInfo
        {
            public bool IsFemale;
            public RaceType Race;
        }

        private PlayerMobile _character;
        private CharacterInfo _characterInfo;
        private readonly Button _maleRadio, _femaleRadio;
        private Combobox _hairCombobox, _facialCombobox;
        private TextBox _hairLabel, _facialLabel;
        private readonly StbTextBox _nameTextBox;
        private PaperDollInteractable _paperDoll;
        private readonly ImageButton _nextButton;
        private readonly Dictionary<Layer, Tuple<int, ushort>> CurrentColorOption = new Dictionary<Layer, Tuple<int, ushort>>();
        private ImageButton button;
        private ImageButton buttonMale;
        private ImageButton buttonFemale;
        private ImageButton buttonHuman;
        private ImageButton buttonElf;
        private ImageButton buttonGargolye;
        private readonly ProfessionInfo _Parent;
        private HSliderBar[] _attributeSliders;
        private HSliderBar[] _hairSliders;
        private bool _showSkills = false;
        private readonly Dictionary<Layer, int> CurrentOption = new Dictionary<Layer, int>
        {
            {
                Layer.Hair, 1
            },
            {
                Layer.Beard, 0
            }
        };
        private Combobox[] _skillsCombobox;
        private HSliderBar[] _skillSliders;
        private List<SkillEntry> _skillList;

        public void SelectProfession(ProfessionInfo info)
        {

            if (info.Type == ProfessionLoader.PROF_TYPE.CATEGORY && ProfessionLoader.Instance.Professions.TryGetValue(info, out List<ProfessionInfo> list) && list != null)
            {
                Parent.Add(new CreateCharProfessionGump(info));
                Parent.Remove(this);
            }
            else
            {
                CharCreationGump charCreationGump = UIManager.GetGump<CharCreationGump>();
                charCreationGump?.SetCharacter(_character);

                charCreationGump?.SetProfession(info);
                
            }

            if (!_showSkills && info.Name == "Advanced")
            {
                _showSkills = true;
                DisplaySkills();

            }

            if (_showSkills && info.Name != "Advanced")
            {
                _showSkills = false;

                RemoveSkillControls();
            }

            InputStatus(0, (int)_character.Strength);
            InputStatus(1, (int)_character.Intelligence);
            InputStatus(2, (int)_character.Dexterity);
          
        }

        public void InputStatus(int array, int value)
        {
            _attributeSliders[array].Value = value;
        }

        private void RemoveSkillControls()
        {
            if (_skillsCombobox != null)
            {
                foreach (var comboBox in _skillsCombobox)
                {
                    if (comboBox != null)
                        comboBox.Dispose(); // Remove o combobox da interface
                }
            }

            if (_skillSliders != null)
            {
                foreach (var slider in _skillSliders)
                {
                    if (slider != null)
                        slider.Dispose(); // Remove o slider da interface
                }
            }

            _skillsCombobox = null; // Limpa a referência
            _skillSliders = null;   // Limpa a referência
        }

        public void DisplaySkills()
        {
            if (_showSkills)
            {
                int y = 612;
                int x = 370;
                const int spacingX = 200;
                const int spacingY = 70;

                _skillSliders = new HSliderBar[CharCreationGump._skillsCount];
                _skillsCombobox = new Combobox[CharCreationGump._skillsCount];

                var skillNames = _skillList.Select(s => s.Name).ToArray();

                for (int i = 0; i < CharCreationGump._skillsCount; i++)
                {
                    int column = i % 2;
                    int row = i / 2;

                    int posX = x + (column * spacingX);
                    int posY = y + (row * spacingY);

                    Add
                    (
                        _skillsCombobox[i] = new Combobox
                        (
                            posX,
                            posY,
                            182,
                            skillNames,
                            -1,
                            200,
                            false,
                            "Click here"
                        )
                    );

                    Add
                    (
                        _skillSliders[i] = new HSliderBar
                        (
                            posX,
                            posY + 32,
                            93,
                            0,
                            50,
                            ProfessionInfo._VoidSkills[i, 1],
                            HSliderBarStyle.MetalWidgetRecessedBar,
                            true
                        )
                    );
                }

                // Adicionar sliders em pares para controle mútuo
                for (int i = 0; i < _skillSliders.Length; i++)
                {
                    for (int j = 0; j < _skillSliders.Length; j++)
                    {
                        if (i != j)
                        {
                            _skillSliders[i].AddParisSlider(_skillSliders[j]);
                        }
                    }
                }
            }
        }


        public CreateCharAppearanceGump() : base(0, 0)
        {
            ProfessionInfo parent = null;
            _Parent = parent;

            if (parent == null || !ProfessionLoader.Instance.Professions.TryGetValue(parent, out List<ProfessionInfo> professions) || professions == null)
            {
                professions = new List<ProfessionInfo>(ProfessionLoader.Instance.Professions.Keys);
            }

            UIManager.Add(new CharacterSelectionBackground());

            Add
              (
                 new AlphaBlendControl
                 {
                     X = 130,
                     Y = 0,
                     Width = 220,
                     Height = 768,
                     Hue = 0 
                 }
              );

            Add
              (
                 new AlphaBlendControl
                 {
                     X = 365,
                     Y = 0,
                     Width = 354,
                     Height = 768,
                     Hue = 0
                 }
              );

            Add
            (
               new AlphaBlendControl
               {
                   X = 739,
                   Y = 0,
                   Width = 160,
                   Height = 768,
                   Hue = 0x0000
               }
            );

            for (int i = 0; i < professions.Count; i++)
            {
                int cx = i % 7;
                int cy = i >> 1;

                Add
                (
                    new ProfessionInfoGump(professions[i])
                    {
                        X = 25,
                        Y = 78 + i * 70,

                        Selected = SelectProfession
                    }
                );
            }

            Add
               (
                   new TextBox("Character Name", TrueTypeLoader.EMBEDDED_FONT, 22, 300, Color.DarkRed, strokeEffect: true) { X = 465, Y = 44, AcceptMouseInput = false }

               );


            Add
           (
              new FullBlendControl
              {
                  X = 448,
                  Y = 73,
                  Width = 215,
                  Height = 20,
                  Hue = 0x801
              }
           );


            // Male/Female Radios

            Add(buttonMale = new ImageButton(
                384,
                264,
                Path.Combine(CUOEnviroment.ExecutablePath, "ExternalImages", "btn_normal_male.png"),
                Path.Combine(CUOEnviroment.ExecutablePath, "ExternalImages", "btn_pressed_prev.png"),
                Path.Combine(CUOEnviroment.ExecutablePath, "ExternalImages", "btn_hover_male.png")
            ));

            buttonMale.OnButtonClick += () =>
            {
                OnButtonClick(0);
            };

            Add(buttonFemale = new ImageButton(
               384,
               290,
               Path.Combine(CUOEnviroment.ExecutablePath, "ExternalImages", "btn_normal_female.png"),
               Path.Combine(CUOEnviroment.ExecutablePath, "ExternalImages", "btn_pressed_prev.png"),
               Path.Combine(CUOEnviroment.ExecutablePath, "ExternalImages", "btn_hover_female.png")
           ));

            buttonFemale.OnButtonClick += () =>
            {
                OnButtonClick(1);
            };

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
                    X = 450, Y = 69, Width = 215, Height = 20
                    //ValidationRules = (uint) (TEXT_ENTRY_RULES.LETTER | TEXT_ENTRY_RULES.SPACE)
                },
                1
            );

            // Races

            Add(buttonHuman = new ImageButton(
               384,
               154,
               Path.Combine(CUOEnviroment.ExecutablePath, "ExternalImages", "btn_normal_human.png"),
               Path.Combine(CUOEnviroment.ExecutablePath, "ExternalImages", "btn_normal_human.png"),
               Path.Combine(CUOEnviroment.ExecutablePath, "ExternalImages", "btn_normal_human.png")
           ));

            buttonHuman.OnButtonClick += () =>
            {
                OnButtonClick(2);
            };


            Add(buttonElf = new ImageButton(
               504,
               154,
               Path.Combine(CUOEnviroment.ExecutablePath, "ExternalImages", "btn_normal_elf.png"),
               Path.Combine(CUOEnviroment.ExecutablePath, "ExternalImages", "btn_normal_elf.png"),
               Path.Combine(CUOEnviroment.ExecutablePath, "ExternalImages", "btn_normal_elf.png")
            ));

            buttonElf.OnButtonClick += () =>
            {
                OnButtonClick(3);
            };

            if (Client.Version >= ClientVersion.CV_60144)
            {

                Add(buttonGargolye = new ImageButton(
                   604,
                   154,
                   Path.Combine(CUOEnviroment.ExecutablePath, "ExternalImages", "btn_normal_gargolye.png"),
                   Path.Combine(CUOEnviroment.ExecutablePath, "ExternalImages", "btn_normal_gargolye.png"),
                   Path.Combine(CUOEnviroment.ExecutablePath, "ExternalImages", "btn_normal_gargolye.png")
                ));

                buttonGargolye.OnButtonClick += () =>
                {
                    OnButtonClick(4);
                };
            }



            // Prev/Next
            Add(_nextButton = new ImageButton(
               30,
               680,
               Path.Combine(CUOEnviroment.ExecutablePath, "ExternalImages", "btn_normal_prev.png"),
               Path.Combine(CUOEnviroment.ExecutablePath, "ExternalImages", "btn_pressed_prev.png"),
               Path.Combine(CUOEnviroment.ExecutablePath, "ExternalImages", "btn_hover_prev.png")
           ));

            _nextButton.OnButtonClick += () =>
            {
                OnButtonClick(5);
            };

            Add(button = new ImageButton(
               920,
               680,
               Path.Combine(CUOEnviroment.ExecutablePath, "ExternalImages", "btn_normal_next.png"),
               Path.Combine(CUOEnviroment.ExecutablePath, "ExternalImages", "btn_pressed_next.png"),
               Path.Combine(CUOEnviroment.ExecutablePath, "ExternalImages", "btn_hover_next.png")
           ));

            button.OnButtonClick += () =>
            {
                OnButtonClick(6);
            };



            // strength, dexterity, intelligence

            Add
               (
                   new TextBox(ClilocLoader.Instance.GetString(3000111), TrueTypeLoader.EMBEDDED_FONT, 16, 300, Color.DarkRed, strokeEffect: false) { X = 375, Y = 544, AcceptMouseInput = false }

               );

            Add
              (
                  new TextBox(ClilocLoader.Instance.GetString(3000112), TrueTypeLoader.EMBEDDED_FONT, 16, 300, Color.DarkRed, strokeEffect: false) { X = 495, Y = 544, AcceptMouseInput = false }

              );

            Add
              (
                  new TextBox(ClilocLoader.Instance.GetString(3000113), TrueTypeLoader.EMBEDDED_FONT, 16, 300, Color.DarkRed, strokeEffect: false) { X = 608, Y = 544, AcceptMouseInput = false }

              );


            // sliders for attributes
            _attributeSliders = new HSliderBar[3];

            Microsoft.Xna.Framework.Color cor = Microsoft.Xna.Framework.Color.DarkRed;
            ushort corConvertida = (ushort)(cor.R << 8 | cor.G);

            Add
            (
                _attributeSliders[0] = new HSliderBar
                (
                    375,
                    564,
                    93,
                    10,
                    60,
                    ProfessionInfo._VoidStats[0],
                    HSliderBarStyle.MetalWidgetRecessedBar,
                    true,
                    color: corConvertida
                )
            ); 

            Add
            (
                _attributeSliders[1] = new HSliderBar
                (
                    495,
                    564,
                    93,
                    10,
                    60,
                    ProfessionInfo._VoidStats[1],
                    HSliderBarStyle.MetalWidgetRecessedBar,
                    true
                )
            );

            Add
            (
                _attributeSliders[2] = new HSliderBar
                (
                    608,
                    564,
                    93,
                    10,
                    60,
                    ProfessionInfo._VoidStats[2],
                    HSliderBarStyle.MetalWidgetRecessedBar,
                    true
                )
            );

            var clientFlags = World.ClientLockedFeatures.Flags;
            
            _skillList = SkillsLoader.Instance.SortedSkills
                         .Where(s =>
                                     // All standard client versions ignore these skills by defualt
                                     //s.Index != 26 && // MagicResist
                                     s.Index != 47 && // Stealth
                                     s.Index != 48 && // RemoveTrap
                                     s.Index != 54 && // Spellweaving
                                     (_character != null && _character.Race == RaceType.GARGOYLE || s.Index != 57) // Throwing for gargoyle only
                                 )
                          .Where(s =>
                                    clientFlags.HasFlag(LockedFeatureFlags.ExpansionAOS) ||
                                    (
                                        s.Index != 51 && // Chivlary
                                        s.Index != 50 && // Focus
                                        s.Index != 49    // Necromancy
                                    )
                                )

                          .Where(s =>
                                    clientFlags.HasFlag(LockedFeatureFlags.ExpansionSE) ||
                                    (
                                        s.Index != 52 && // Bushido
                                        s.Index != 53    // Ninjitsu
                                    )
                                )

                          .Where(s =>
                                    clientFlags.HasFlag(LockedFeatureFlags.ExpansionSA) ||
                                    (
                                        s.Index != 55 && // Mysticism
                                        s.Index != 56    // Imbuing
                                    )
                                )
                         .ToList();
             // do not include archer if it's a gargoyle
            if (_character != null && _character.Race == RaceType.GARGOYLE)
            {
                var archeryEntry = _skillList.FirstOrDefault(s => s.Index == 31);
                if (archeryEntry != null)
                {
                    _skillList.Remove(archeryEntry);
                }
            }
             

            _skillList = SkillsLoader.Instance.SortedSkills.ToList();

           

            var skillNames = _skillList.Select(s => s.Name).ToArray();

            int y = 612;
            int x = 370;
            const int spacingX = 200; 
            const int spacingY = 70;  
          
            

            for (int i = 0; i < _attributeSliders.Length; i++)
            {
                for (int j = 0; j < _attributeSliders.Length; j++)
                {
                    if (i != j)
                    {
                        _attributeSliders[i].AddParisSlider(_attributeSliders[j]);
                    }
                }
            }


            _characterInfo.IsFemale = false;
            _characterInfo.Race = RaceType.HUMAN;

            HandleGenreChange();
            HandleRaceChanged();

            _character.Name = _nameTextBox.Text;
            CharCreationGump charCreationGump = UIManager.GetGump<CharCreationGump>();

            if (ValidateCharacter(_character))
            {
                charCreationGump.SetCharacter(_character);
            }
        }


       

        private bool ValidateValues()
        {
            if (_skillsCombobox.All(s => s.SelectedIndex >= 0))
            {
                int duplicated = _skillsCombobox.GroupBy(o => o.SelectedIndex).Count(o => o.Count() > 1);

                if (duplicated > 0)
                {
                    UIManager.GetGump<CharCreationGump>()?.ShowMessage(ClilocLoader.Instance.GetString(1080032));

                    return false;
                }
            }
            else
            {
                UIManager.GetGump<CharCreationGump>()?.ShowMessage(Client.Version <= ClientVersion.CV_5090 ? ResGumps.YouMustHaveThreeUniqueSkillsChosen : ClilocLoader.Instance.GetString(1080032));

                return false;
            }

            return true;
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
            else
            {
                _character.Flags &= ~Flags.Female;
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

                        it = CreateItem(0x1531, CurrentColorOption[Layer.Pants].Item2, Layer.Pants);

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
            RaceType race = _characterInfo.Race;
            Layer layer;
            CharacterCreationValues.ComboContent content;

            _character.Hue = CurrentColorOption[Layer.Invalid].Item2;

            if (!_characterInfo.IsFemale && race != RaceType.ELF)
            {
                layer = Layer.Beard;
                content = CharacterCreationValues.GetFacialHairComboContent(race);

                Item iti = CreateItem(content.GetGraphic(CurrentOption[layer]), CurrentColorOption[layer].Item2, layer);

                _character.PushToBack(iti);
            }

            layer = Layer.Hair;
            content = CharacterCreationValues.GetHairComboContent(_characterInfo.IsFemale, race);

            Item it = CreateItem(content.GetGraphic(CurrentOption[layer]), CurrentColorOption[layer].Item2, layer);

            _character.PushToBack(it);
        }

        private void HandleRaceChanged()
        {
            CurrentColorOption.Clear();
            HandleGenreChange();
            RaceType race = _characterInfo.Race;
            CharacterListFlags flags = World.ClientFeatures.Flags;
            LockedFeatureFlags locks = World.ClientLockedFeatures.Flags;

            bool allowElf = (flags & CharacterListFlags.CLF_ELVEN_RACE) != 0 && locks.HasFlag(LockedFeatureFlags.ML);
            bool allowGarg = locks.HasFlag(LockedFeatureFlags.SA);

            if (race == RaceType.ELF && !allowElf)
            {
                _nextButton.IsEnabled = false;
            }
            else if (race == RaceType.GARGOYLE && !allowGarg)
            {
                _nextButton.IsEnabled = false;
            }
            else
            {
                _nextButton.IsEnabled = true;
            }
        }

        private void HandleGenreChange()
        {
            RaceType race = _characterInfo.Race;

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
            CharacterCreationValues.ComboContent content = CharacterCreationValues.GetHairComboContent(_characterInfo.IsFemale, race);

            bool isAsianLang = string.Compare(Settings.GlobalSettings.Language, "CHT", StringComparison.InvariantCultureIgnoreCase) == 0 ||
                string.Compare(Settings.GlobalSettings.Language, "KOR", StringComparison.InvariantCultureIgnoreCase) == 0 ||
                string.Compare(Settings.GlobalSettings.Language, "JPN", StringComparison.InvariantCultureIgnoreCase) == 0;

            bool unicode = isAsianLang;
            byte font = (byte)(isAsianLang ? 3 : 9);
            ushort hue = (ushort)(isAsianLang ? 0xFFFF : 0);


            Add(
                _hairLabel =  new TextBox(ClilocLoader.Instance.GetString(race == RaceType.GARGOYLE ? 1112309 : 3000121), TrueTypeLoader.EMBEDDED_FONT, 16, 300, Color.DarkRed, strokeEffect: true) { X = 755, Y = 91, AcceptMouseInput = false }
            );

            Add
            (
                _hairCombobox = new Combobox
                (
                    755,
                    111,
                    120,
                    content.Labels,
                    CurrentOption[Layer.Hair]
                ),
                1
            );

            _hairCombobox.OnOptionSelected += Hair_OnOptionSelected;

            // Facial Hair
            if (!_characterInfo.IsFemale && race != RaceType.ELF)
            {
                content = CharacterCreationValues.GetFacialHairComboContent(race);

                Add(
                    _facialLabel = new TextBox(ClilocLoader.Instance.GetString(race == RaceType.GARGOYLE ? 1112511 : 3000122), TrueTypeLoader.EMBEDDED_FONT, 16, 300, Color.DarkRed, strokeEffect: true) { X = 755, Y = 141, AcceptMouseInput = false }
                );

                Add
                (
                    _facialCombobox = new Combobox
                    (
                        755,
                        161,
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

            

            var margin = 50;

            AddCustomColorPicker
            (
                755,
                141 + margin,
                pallet,
                Layer.Invalid,
                3000183,
                8,
                pallet.Length >> 3
            );

            // Shirt Color
            AddCustomColorPicker
            (
                755,
                183 + margin,
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
                    755,
                    225 + margin,
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
                755,
                267 + margin,
                pallet,
                Layer.Hair,
                race == RaceType.GARGOYLE ? 1112322 : 3000184,
                8,
                pallet.Length >> 3
            );

            if (!_characterInfo.IsFemale && race != RaceType.ELF)
            {
                // Facial
                pallet = CharacterCreationValues.GetHairPallet(race);

                AddCustomColorPicker
                (
                    755,
                    309 + margin,
                    pallet,
                    Layer.Beard,
                    race == RaceType.GARGOYLE ? 1112512 : 3000446,
                    8,
                    pallet.Length >> 3
                );
            }

            CreateCharacter(_characterInfo.IsFemale, race);

            UpdateEquipments();

            Add
            (
                _paperDoll = new PaperDollInteractable(445, 200, _character, null)
                {
                    AcceptMouseInput = false
                },
                1
            );

            _paperDoll.RequestUpdate();
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
                    item = _character.FindItemByLayer(_characterInfo.IsFemale && e.Layer == Layer.Pants ? Layer.Skirt : e.Layer);
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

            _paperDoll.RequestUpdate();
        }

        private void Facial_OnOptionSelected(object sender, int e)
        {
            CurrentOption[Layer.Beard] = e;
            UpdateEquipments();
            _paperDoll.RequestUpdate();
        }

        private void Hair_OnOptionSelected(object sender, int e)
        {
            CurrentOption[Layer.Hair] = e;
            UpdateEquipments();
            _paperDoll.RequestUpdate();
        }

        public override void OnButtonClick(int buttonID)
        {
            CharCreationGump charCreationGump = UIManager.GetGump<CharCreationGump>();


            switch ((Buttons) buttonID)
            {
                case Buttons.FemaleButton:
                    _characterInfo.IsFemale = true;

                    HandleGenreChange();

                    break;

                case Buttons.MaleButton:

                    _characterInfo.IsFemale = false;

                    HandleGenreChange();

                    break;

                case Buttons.HumanButton:

                    _characterInfo.Race = RaceType.HUMAN;

                    

                    HandleRaceChanged();
                    

                    break;

                case Buttons.ElfButton:

                    _characterInfo.Race = RaceType.ELF;

                    

                     HandleRaceChanged();
                    

                    break;

                case Buttons.GargoyleButton:

                    _characterInfo.Race = RaceType.GARGOYLE;

                    
                     HandleRaceChanged();
                    

                    break;

                case Buttons.Next:
                    _character.Name = _nameTextBox.Text;

                    if (ValidateCharacter(_character))
                    {
                        charCreationGump.SetCharacter(_character);
                    }

                    if (_showSkills)
                    {
                        if (ValidateValues())
                        {
                            for (int i = 0; i < _skillsCombobox.Length; i++)
                            {
                                if (_skillsCombobox[i].SelectedIndex != -1)
                                {
                                    Skill skill = _character.Skills[_skillList[_skillsCombobox[i].SelectedIndex].Index];
                                    skill.ValueFixed = (ushort)_skillSliders[i].Value;
                                    skill.BaseFixed = 0;
                                    skill.CapFixed = 0;
                                    skill.Lock = Lock.Locked;
                                }
                            }

                           

                            
                        }

                    }

                    _character.Strength = (ushort)_attributeSliders[0].Value;
                    _character.Intelligence = (ushort)_attributeSliders[1].Value;
                    _character.Dexterity = (ushort)_attributeSliders[2].Value;

                    charCreationGump.SetAttributes(true);

                    break;

                case Buttons.Prev:
                    charCreationGump.StepBack();

                    break;
            }

            base.OnButtonClick(buttonID);
        }

        private bool ValidateCharacter(PlayerMobile character)
        {
            int invalid = Validate(character.Name);
            if (invalid > 0)
            {
                UIManager.GetGump<CharCreationGump>()?.ShowMessage(ClilocLoader.Instance.GetString(invalid));

                return false;
            }

            return true;
        }

        public static int Validate(string name)
        {
            return Validate(name, 2, 16, true, false, true, 1, _SpaceDashPeriodQuote, Client.Version >= ClientVersion.CV_5020 ? _Disallowed : new string[] { }, _StartDisallowed);
        }

        public static int Validate(string name, int minLength, int maxLength, bool allowLetters, bool allowDigits, bool noExceptionsAtStart, int maxExceptions, char[] exceptions, string[] disallowed, string[] startDisallowed)
        {
            if (string.IsNullOrEmpty(name) || name.Length < minLength)
                return 3000612;
            else if (name.Length > maxLength)
                return 3000611;

            int exceptCount = 0;

            name = name.ToLowerInvariant();

            if (!allowLetters || !allowDigits || (exceptions.Length > 0 && (noExceptionsAtStart || maxExceptions < int.MaxValue)))
            {
                for (int i = 0; i < name.Length; ++i)
                {
                    char c = name[i];

                    if (c >= 'a' && c <= 'z')
                    {
                        exceptCount = 0;
                    }
                    else if (c >= '0' && c <= '9')
                    {
                        if (!allowDigits)
                            return 3000611;

                        exceptCount = 0;
                    }
                    else
                    {
                        bool except = false;

                        for (int j = 0; !except && j < exceptions.Length; ++j)
                            if (c == exceptions[j])
                                except = true;

                        if (!except || (i == 0 && noExceptionsAtStart))
                            return 3000611;

                        if (exceptCount++ == maxExceptions)
                            return 3000611;
                    }
                }
            }

            for (int i = 0; i < disallowed.Length; ++i)
            {
                int indexOf = name.IndexOf(disallowed[i]);

                if (indexOf == -1)
                    continue;

                bool badPrefix = (indexOf == 0);

                for (int j = 0; !badPrefix && j < exceptions.Length; ++j)
                    badPrefix = (name[indexOf - 1] == exceptions[j]);

                if (!badPrefix)
                    continue;

                bool badSuffix = ((indexOf + disallowed[i].Length) >= name.Length);

                for (int j = 0; !badSuffix && j < exceptions.Length; ++j)
                    badSuffix = (name[indexOf + disallowed[i].Length] == exceptions[j]);

                if (badSuffix)
                    return 3000611;
            }

            for (int i = 0; i < startDisallowed.Length; ++i)
            {
                if (name.StartsWith(startDisallowed[i]))
                    return 3000611;
            }

            return 0;
        }

        private static readonly char[] _SpaceDashPeriodQuote = new char[]
        {
            ' ', '-', '.', '\''
        };

        private static string[] _StartDisallowed = new string[]
        {
            "seer",
            "counselor",
            "gm",
            "admin",
            "lady",
            "lord"
        };

        private static readonly string[] _Disallowed = new string[]
        {
            "jigaboo",
            "chigaboo",
            "wop",
            "kyke",
            "kike",
            "tit",
            "spic",
            "prick",
            "piss",
            "lezbo",
            "lesbo",
            "felatio",
            "dyke",
            "dildo",
            "chinc",
            "chink",
            "cunnilingus",
            "cum",
            "cocksucker",
            "cock",
            "clitoris",
            "clit",
            "ass",
            "hitler",
            "penis",
            "nigga",
            "nigger",
            "klit",
            "kunt",
            "jiz",
            "jism",
            "jerkoff",
            "jackoff",
            "goddamn",
            "fag",
            "blowjob",
            "bitch",
            "asshole",
            "dick",
            "pussy",
            "snatch",
            "cunt",
            "twat",
            "shit",
            "fuck",
            "tailor",
            "smith",
            "scholar",
            "rogue",
            "novice",
            "neophyte",
            "merchant",
            "medium",
            "master",
            "mage",
            "lb",
            "journeyman",
            "grandmaster",
            "fisherman",
            "expert",
            "chef",
            "carpenter",
            "british",
            "blackthorne",
            "blackthorn",
            "beggar",
            "archer",
            "apprentice",
            "adept",
            "gamemaster",
            "frozen",
            "squelched",
            "invulnerable",
            "osi",
            "origin"
        };

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

            Item item = World.GetOrCreateItem(0x4000_0000 + (uint) layer);
            _character.Remove(item);
            item.Graphic = (ushort) id;
            item.Hue = hue;
            item.Layer = layer;
            item.Container = _character;

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

            public ushort SelectedHue => Pallet != null && SelectedIndex >= 0 && SelectedIndex < Pallet.Length ? Pallet[SelectedIndex] : (ushort) 0xFFFF;
        }

        private class CustomColorPicker : Control
        {
            private readonly int _cellH;
            private readonly int _cellW;
            private readonly ColorBox _colorPicker;
            private ColorPickerBox _colorPickerBox;
            private readonly int _columns, _rows;
            private int _lastSelectedIndex;
            private readonly Layer _layer;
            private readonly ushort[] _pallet;

            public CustomColorPicker(Layer layer, int label, ushort[] pallet, int rows, int columns)
            {
                Width = 50;
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
                     new TextBox(ClilocLoader.Instance.GetString(label), TrueTypeLoader.EMBEDDED_FONT, 16, 300, Color.DarkRed, strokeEffect: true) { X = 0, Y = 0, AcceptMouseInput = false }

                 );


                Add
                (
                    _colorPicker = new ColorBox(121, 23, (ushort) ((pallet?[0] ?? 1) + 1))
                    {
                        X = 1,
                        Y = 17
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
                            755,
                            420,
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
    }
}
