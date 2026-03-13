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
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Game.Scenes;
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
        private GothicStyleCombobox _hairCombobox, _facialCombobox;
        private UOLabel _hairLabel, _facialLabel;
        private readonly StbTextBox _nameTextBox;
        private PaperDollInteractable _paperDoll;
        private readonly GothicStyleButtonLogin _nextButton;
        private readonly Dictionary<Layer, Tuple<int, ushort>> CurrentColorOption = new Dictionary<Layer, Tuple<int, ushort>>();
        private GothicStyleButtonLogin button;
        private GothicStyleButtonLogin buttonMale;
        private GothicStyleButtonLogin buttonFemale;
        private GothicStyleButtonLogin buttonHuman;
        private GothicStyleButtonLogin buttonElf;
        private GothicStyleButtonLogin buttonGargolye;
        private readonly ProfessionInfo _Parent;
        private GothicStyleSliderBar[] _attributeSliders;
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
        private GothicStyleCombobox[] _skillsCombobox;
        private GothicStyleSliderBar[] _skillSliders;
        private List<SkillEntry> _skillList;
        private List<Control> _professionSkillsLabels = new List<Control>();
        private ProfessionInfo _displayedProfession;
        private GothicStyleCombobox _cityCombobox;
        private const int SECTION_PADDING = 16;
        private static readonly int CENTER_PANEL_X = LoginLayoutHelper.X(260);
        private static readonly int CENTER_PANEL_WIDTH = LoginLayoutHelper.W(558);
        private static readonly int CENTER_PANEL_MID = CENTER_PANEL_X + CENTER_PANEL_WIDTH / 2;
        private static readonly int CENTER_X = CENTER_PANEL_X;
        private static readonly int RIGHT_X = LoginLayoutHelper.X(845);
        private const int PAPERDOLL_WIDTH = 150;
        private const int RACE_PAPERDOLL_MARGIN = 16;
        private static readonly int RACE_SECTION_Y = LoginLayoutHelper.Y(165);
        private const int PAPERDOLL_OFFSET_RIGHT = 56;
        private static readonly int PAPERDOLL_X = CENTER_X + SECTION_PADDING + 100 + RACE_PAPERDOLL_MARGIN + PAPERDOLL_OFFSET_RIGHT;
        private static readonly int PAPERDOLL_Y = RACE_SECTION_Y;
        private const int RACE_BUTTON_MARGIN = 12;
        private static readonly int GENDER_SECTION_Y = LoginLayoutHelper.Y(428);
        private static readonly int ATTR_SECTION_Y = LoginLayoutHelper.Y(468);
        private const int SKILLS_BELOW_ATTR_GAP = 14;
        private static readonly int SKILLS_SECTION_Y = ATTR_SECTION_Y + 22 + 30 + SKILLS_BELOW_ATTR_GAP;
        private static readonly int NEXT_BUTTON_X = LoginLayoutHelper.X(874);
        private static readonly int NEXT_BUTTON_Y = LoginLayoutHelper.Y(680);
        private const int RIGHT_COMBO_WIDTH = 120;
        private static readonly int RIGHT_LABEL_Y_HAIR = LoginLayoutHelper.Y(91);
        private static readonly int RIGHT_COMBO_Y_HAIR = LoginLayoutHelper.Y(111);
        private static readonly int RIGHT_LABEL_Y_FACIAL = LoginLayoutHelper.Y(141);
        private static readonly int RIGHT_COMBO_Y_FACIAL = LoginLayoutHelper.Y(161);
        private static readonly int RIGHT_COLOR_BASE = LoginLayoutHelper.Y(191);
        private static readonly int RIGHT_COLOR_STEP = LoginLayoutHelper.H(42);

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

            if (info.Type != ProfessionLoader.PROF_TYPE.CATEGORY && info.Name != "Advanced")
            {
                ClearProfessionSkillsDisplay();
                DisplayProfessionSkills(info);
            }
            else
            {
                ClearProfessionSkillsDisplay();
            }

            InputStatus(0, (int)_character.Strength);
            InputStatus(1, (int)_character.Intelligence);
            InputStatus(2, (int)_character.Dexterity);
          
        }

        public void InputStatus(int array, int value)
        {
            _attributeSliders[array].Value = value;
        }

        private void ClearProfessionSkillsDisplay()
        {
            foreach (var c in _professionSkillsLabels)
                c?.Dispose();
            _professionSkillsLabels.Clear();
            _displayedProfession = null;
        }

        private void DisplayProfessionSkills(ProfessionInfo info)
        {
            _displayedProfession = info;
            int n = CharCreationGump._skillsCount;
            const int ComboWidth = 120;
            const int ComboHeight = 26;
            const int RowGap = 10;
            const int ColGap = 24;
            int colsPerRow = 2;
            int totalWidth = colsPerRow * ComboWidth + (colsPerRow - 1) * ColGap;
            int posXBase = CENTER_X + (CENTER_PANEL_WIDTH - totalWidth) / 2;

            for (int i = 0; i < n; i++)
            {
                int row = i / 2;
                int col = i % 2;
                int posY = SKILLS_SECTION_Y + row * (ComboHeight + RowGap);
                int posX = posXBase + col * (ComboWidth + ColGap);

                int skillIndex = info.SkillDefVal[i, 0];
                int skillValue = info.SkillDefVal[i, 1];
                var skillEntry = SkillsLoader.Instance.Skills.Find(s => s.Index == skillIndex);
                string skillName = skillEntry?.Name ?? $"Skill {skillIndex}";

                var combo = new GothicStyleCombobox(posX, posY, ComboWidth, ComboHeight, new[] { skillName }, 0);
                ApplyGothicRedTheme(combo);
                combo.AcceptMouseInput = false;
                Add(combo);
                _professionSkillsLabels.Add(combo);

                var valueLabel = new UOLabel($"{skillValue}", 1, UOLabelHue.Text, Assets.TEXT_ALIGN_TYPE.TS_LEFT, 28) { X = posX + ComboWidth + 4, Y = posY + 4 };
                Add(valueLabel);
                _professionSkillsLabels.Add(valueLabel);
            }
        }

        private void RemoveSkillControls()
        {
            if (_skillsCombobox != null)
            {
                foreach (var c in _skillsCombobox)
                    c?.Dispose();
            }
            if (_skillSliders != null)
            {
                foreach (var s in _skillSliders)
                    s?.Dispose();
            }
            _skillsCombobox = null;
            _skillSliders = null;
        }

        public void DisplaySkills()
        {
            if (_showSkills)
            {
                int n = CharCreationGump._skillsCount;
                const int ComboWidth = 140;
                const int ComboHeight = 26;
                const int LabelHeight = 32;
                const int SliderWidth = 120;
                const int RowGap = 12;
                const int ColGap = 24;
                int colsPerRow = (n + 1) / 2;
                int totalComboWidth = colsPerRow * ComboWidth + (colsPerRow - 1) * ColGap;
                int skillXBase = CENTER_X + (CENTER_PANEL_WIDTH - totalComboWidth) / 2;

                _skillSliders = new GothicStyleSliderBar[n];
                _skillsCombobox = new GothicStyleCombobox[n];
                var skillNames = _skillList.Select(s => s.Name).ToArray();

                for (int i = 0; i < n; i++)
                {
                    int row = i / 2;
                    int col = i % 2;
                    int skillY = SKILLS_SECTION_Y + row * (ComboHeight + LabelHeight + RowGap);
                    int posX = skillXBase + col * (ComboWidth + ColGap);
                    var combo = new GothicStyleCombobox(posX, skillY, ComboWidth, ComboHeight, skillNames, -1);
                    ApplyGothicRedTheme(combo);
                    _skillsCombobox[i] = combo;
                    Add(combo);
                    Add(_skillSliders[i] = new GothicStyleSliderBar(posX, skillY + ComboHeight + 4, SliderWidth, 0, 50, ProfessionInfo._VoidSkills[i, 1], true));
                }

                for (int i = 0; i < _skillSliders.Length; i++)
                {
                    for (int j = 0; j < _skillSliders.Length; j++)
                    {
                        if (i != j)
                            _skillSliders[i].AddParisSlider(_skillSliders[j]);
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

            Color panelBg = Color.Black;
            Color centerBg = Color.Black;
            Color rightBg = Color.Black;

            Add(new RoundedColorBox(LoginLayoutHelper.W(220), LoginLayoutHelper.H(768), panelBg, 14) { X = LoginLayoutHelper.X(30), Y = 0 });
            Add(new RoundedColorBox(LoginLayoutHelper.W(558), LoginLayoutHelper.H(768), centerBg, 14) { X = LoginLayoutHelper.X(260), Y = 0 });
            Add(new RoundedColorBox(LoginLayoutHelper.W(160), LoginLayoutHelper.H(768), rightBg, 14) { X = LoginLayoutHelper.X(829), Y = 0 });

            for (int i = 0; i < professions.Count; i++)
            {
                Add
                (
                    new ProfessionInfoGump(professions[i])
                    {
                        X = 0,
                        Y = LoginLayoutHelper.Y(78 + i * 70),
                        Selected = SelectProfession
                    }
                );
            }

            int NameInputWidth = LoginLayoutHelper.W(219);
            Add(new UOLabel("Character Name", 1, UOLabelHue.Accent, Assets.TEXT_ALIGN_TYPE.TS_CENTER, NameInputWidth, FontStyle.BlackBorder) { X = CENTER_PANEL_MID - NameInputWidth / 2, Y = LoginLayoutHelper.Y(44) });
            Add(new RoundedColorBox(LoginLayoutHelper.W(221), LoginLayoutHelper.H(26), new Color(80, 20, 20), 6) { X = CENTER_PANEL_MID - NameInputWidth / 2 - 1, Y = LoginLayoutHelper.Y(71) });
            Add(new RoundedColorBox(LoginLayoutHelper.W(217), LoginLayoutHelper.H(22), new Color(28, 28, 28), 4) { X = CENTER_PANEL_MID - NameInputWidth / 2 + 1, Y = LoginLayoutHelper.Y(73) });
            Add(new FullBlendControl { X = CENTER_PANEL_MID - NameInputWidth / 2 + 2, Y = LoginLayoutHelper.Y(74), Width = NameInputWidth - 4, Height = LoginLayoutHelper.H(20), Hue = 0x801 });

            Add(_nextButton = new GothicStyleButtonLogin(LoginLayoutHelper.X(30), NEXT_BUTTON_Y, LoginLayoutHelper.W(120), LoginLayoutHelper.H(40), "BACK", null, 16));
            _nextButton.OnClick += () => OnButtonClick(5);

            Add(button = new GothicStyleButtonLogin(NEXT_BUTTON_X, NEXT_BUTTON_Y, LoginLayoutHelper.W(120), LoginLayoutHelper.H(40), "NEXT", null, 16));
            button.OnClick += () => OnButtonClick(6);

            Add(_nameTextBox = new StbTextBox(5, 16, NameInputWidth - 4, false, hue: 0x0481, style: FontStyle.Fixed, align: Assets.TEXT_ALIGN_TYPE.TS_CENTER) { X = CENTER_PANEL_MID - NameInputWidth / 2 + 2, Y = LoginLayoutHelper.Y(74), Width = NameInputWidth - 4, Height = LoginLayoutHelper.H(20) }, 1);

            var quitButton = new Button(0, 0x1589, 0x158B, 0x158A)
            {
                X = LoginLayoutHelper.WindowWidth - 44,
                Y = 0,
                ButtonAction = ButtonAction.Activate,
                AcceptKeyboardInput = false,
                LocalSerial = 100
            };
            quitButton.MouseUp += (s, e) => { if (e.Button == MouseButtonType.Left) Client.Game.Exit(); };
            Add(quitButton);

            int RaceX = CENTER_X + SECTION_PADDING;
            int RaceY2 = RACE_SECTION_Y + 26 + RACE_BUTTON_MARGIN;
            int RaceY3 = RaceY2 + 26 + RACE_BUTTON_MARGIN;
            const int RaceBtnW = 100;
            Add(buttonHuman = new GothicStyleButtonLogin(RaceX, RACE_SECTION_Y, RaceBtnW, 26, "HUMAN", null, 14));
            buttonHuman.OnClick += () => OnButtonClick(2);
            Add(buttonElf = new GothicStyleButtonLogin(RaceX, RaceY2, RaceBtnW, 26, "ELF", null, 14));
            buttonElf.OnClick += () => OnButtonClick(3);
            if (Client.Version >= ClientVersion.CV_60144)
            {
                Add(buttonGargolye = new GothicStyleButtonLogin(RaceX, RaceY3, RaceBtnW, 26, "GARGOLYE", null, 14));
                buttonGargolye.OnClick += () => OnButtonClick(4);
            }

            const int GenderBtnW = 95;
            const int GenderGap = 12;
            Add(buttonMale = new GothicStyleButtonLogin(CENTER_PANEL_MID - GenderBtnW - GenderGap / 2, GENDER_SECTION_Y, GenderBtnW, 28, "MALE ♂", null, 14));
            buttonMale.OnClick += () => OnButtonClick(0);
            Add(buttonFemale = new GothicStyleButtonLogin(CENTER_PANEL_MID + GenderGap / 2, GENDER_SECTION_Y, GenderBtnW, 28, "FEMALE ♀", null, 14));
            buttonFemale.OnClick += () => OnButtonClick(1);

            if (!CUOEnviroment.IsOutlands)
            {
                var loginScene = Client.Game.GetScene<LoginScene>();
                if (loginScene?.Cities != null && loginScene.Cities.Length > 0)
                {
                    var cityNames = new string[loginScene.Cities.Length];
                    for (int i = 0; i < loginScene.Cities.Length; i++)
                    {
                        var c = loginScene.GetCity(i);
                        cityNames[i] = c?.City ?? $"City {i}";
                    }
                    Add(new UOLabel("Sele City to start", 1, UOLabelHue.Accent, Assets.TEXT_ALIGN_TYPE.TS_LEFT, 120) { X = RIGHT_X, Y = 409 });
                    _cityCombobox = new GothicStyleCombobox(RIGHT_X, 429, 120, 25, cityNames, 0);
                    ApplyGothicRedTheme(_cityCombobox);
                    Add(_cityCombobox, 1);
                }
            }

            int AttrLabelY = ATTR_SECTION_Y;
            int AttrSliderY = ATTR_SECTION_Y + 22;
            const int AttrSliderW = 100;
            int AttrSlotWidth = (CENTER_PANEL_WIDTH - SECTION_PADDING * 2) / 3;
            int AttrOffset = (AttrSlotWidth - AttrSliderW) / 2;
            int AttrX1 = CENTER_X + SECTION_PADDING + AttrOffset;
            int AttrX2 = AttrX1 + AttrSlotWidth;
            int AttrX3 = AttrX2 + AttrSlotWidth;
            Add(new UOLabel(ClilocLoader.Instance.GetString(3000111), 1, UOLabelHue.Accent, Assets.TEXT_ALIGN_TYPE.TS_LEFT, 120) { X = AttrX1, Y = AttrLabelY });
            Add(new UOLabel(ClilocLoader.Instance.GetString(3000112), 1, UOLabelHue.Accent, Assets.TEXT_ALIGN_TYPE.TS_LEFT, 120) { X = AttrX2, Y = AttrLabelY });
            Add(new UOLabel(ClilocLoader.Instance.GetString(3000113), 1, UOLabelHue.Accent, Assets.TEXT_ALIGN_TYPE.TS_LEFT, 120) { X = AttrX3, Y = AttrLabelY });

            _attributeSliders = new GothicStyleSliderBar[3];
            Add(_attributeSliders[0] = new GothicStyleSliderBar(AttrX1, AttrSliderY, AttrSliderW, 10, 60, ProfessionInfo._VoidStats[0], true));
            Add(_attributeSliders[1] = new GothicStyleSliderBar(AttrX2, AttrSliderY, AttrSliderW, 10, 60, ProfessionInfo._VoidStats[1], true));
            Add(_attributeSliders[2] = new GothicStyleSliderBar(AttrX3, AttrSliderY, AttrSliderW, 10, 60, ProfessionInfo._VoidStats[2], true));

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


        private const int MIN_ADVANCED_SKILLS = 3;

        private bool ValidateValues()
        {
            if (_skillsCombobox == null || _skillsCombobox.Length == 0)
                return true;
            var selected = _skillsCombobox.Where(s => s.SelectedIndex >= 0).Select(s => s.SelectedIndex).ToList();
            if (selected.Count < MIN_ADVANCED_SKILLS)
            {
                UIManager.GetGump<CharCreationGump>()?.ShowMessage(Client.Version <= ClientVersion.CV_5090 ? ResGumps.YouMustHaveThreeUniqueSkillsChosen : ClilocLoader.Instance.GetString(1080032));
                return false;
            }
            if (selected.Count > CharCreationGump._skillsCount)
            {
                UIManager.GetGump<CharCreationGump>()?.ShowMessage(ClilocLoader.Instance.GetString(1080032));
                return false;
            }
            int distinctCount = selected.Distinct().Count();
            if (distinctCount != selected.Count)
            {
                UIManager.GetGump<CharCreationGump>()?.ShowMessage(ClilocLoader.Instance.GetString(1080032));
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

        private static void ApplyGothicRedTheme(GothicStyleCombobox combo)
        {
            combo.BaseColor = Color.DarkRed;
            combo.HighlightColor = new Color(180, 50, 50);
            combo.ShadowColor = new Color(80, 15, 15);
            combo.TextColor = Color.White;
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


            Add(_hairLabel = new UOLabel(ClilocLoader.Instance.GetString(race == RaceType.GARGOYLE ? 1112309 : 3000121), 1, UOLabelHue.Accent, Assets.TEXT_ALIGN_TYPE.TS_LEFT, 300, FontStyle.BlackBorder) { X = RIGHT_X, Y = RIGHT_LABEL_Y_HAIR });
            _hairCombobox = new GothicStyleCombobox(RIGHT_X, RIGHT_COMBO_Y_HAIR, RIGHT_COMBO_WIDTH, 25, content.Labels, CurrentOption[Layer.Hair]);
            ApplyGothicRedTheme(_hairCombobox);
            Add(_hairCombobox, 1);

            _hairCombobox.OnSelectionChanged += (sender, index) => Hair_OnOptionSelected(sender, index);

            if (!_characterInfo.IsFemale && race != RaceType.ELF)
            {
                content = CharacterCreationValues.GetFacialHairComboContent(race);

                Add(_facialLabel = new UOLabel(ClilocLoader.Instance.GetString(race == RaceType.GARGOYLE ? 1112511 : 3000122), 1, UOLabelHue.Accent, Assets.TEXT_ALIGN_TYPE.TS_LEFT, 300, FontStyle.BlackBorder) { X = RIGHT_X, Y = RIGHT_LABEL_Y_FACIAL });
                _facialCombobox = new GothicStyleCombobox(RIGHT_X, RIGHT_COMBO_Y_FACIAL, RIGHT_COMBO_WIDTH, 25, content.Labels, CurrentOption[Layer.Beard]);
                ApplyGothicRedTheme(_facialCombobox);
                Add(_facialCombobox, 1);

                _facialCombobox.OnSelectionChanged += (sender, index) => Facial_OnOptionSelected(sender, index);
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
                RIGHT_X,
                RIGHT_COLOR_BASE,
                pallet,
                Layer.Invalid,
                3000183,
                8,
                pallet.Length >> 3
            );

            AddCustomColorPicker
            (
                RIGHT_X,
                RIGHT_COLOR_BASE + RIGHT_COLOR_STEP,
                null,
                Layer.Shirt,
                3000440,
                10,
                20
            );

            if (race != RaceType.GARGOYLE)
            {
                AddCustomColorPicker
                (
                    RIGHT_X,
                    RIGHT_COLOR_BASE + RIGHT_COLOR_STEP * 2,
                    null,
                    Layer.Pants,
                    3000441,
                    10,
                    20
                );
            }

            pallet = CharacterCreationValues.GetHairPallet(race);
            AddCustomColorPicker
            (
                RIGHT_X,
                RIGHT_COLOR_BASE + RIGHT_COLOR_STEP * 3,
                pallet,
                Layer.Hair,
                race == RaceType.GARGOYLE ? 1112322 : 3000184,
                8,
                pallet.Length >> 3
            );

            if (!_characterInfo.IsFemale && race != RaceType.ELF)
            {
                pallet = CharacterCreationValues.GetHairPallet(race);
                AddCustomColorPicker
                (
                    RIGHT_X,
                    RIGHT_COLOR_BASE + RIGHT_COLOR_STEP * 4,
                    pallet,
                    Layer.Beard,
                    race == RaceType.GARGOYLE ? 1112512 : 3000446,
                    8,
                    pallet.Length >> 3
                );
            }

            CreateCharacter(_characterInfo.IsFemale, race);

            UpdateEquipments();

            Add(_paperDoll = new PaperDollInteractable(PAPERDOLL_X, PAPERDOLL_Y, _character, null) { AcceptMouseInput = false }, 1);

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
                    _character.Name = _nameTextBox.Text.Trim();

                    if (!charCreationGump.HasProfessionSelected)
                    {
                        charCreationGump.ShowMessage("You must select a profession.");
                        break;
                    }

                    if (!ValidateCharacter(_character))
                    {
                        break;
                    }

                    charCreationGump.SetCharacter(_character);

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

                    int? cityIndex = null;
                    if (_cityCombobox != null && _cityCombobox.SelectedIndex >= 0)
                        cityIndex = _cityCombobox.SelectedIndex;
                    charCreationGump.SetAttributes(true, cityIndex);

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



                Add(new UOLabel(ClilocLoader.Instance.GetString(label), 1, 32, Assets.TEXT_ALIGN_TYPE.TS_LEFT, 300, Game.FontStyle.BlackBorder) { X = 0, Y = 0 });


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
                            RIGHT_X,
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
