#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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
using System.Linq;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.IO;
using ClassicUO.IO.Resources;

namespace ClassicUO.Game.UI.Gumps.CharCreation
{
    internal class CreateCharTradeGump : Gump
    {
        private readonly HSliderBar[] _attributeSliders;
        private readonly PlayerMobile _character;
        private readonly Combobox[] _skills;
        private readonly HSliderBar[] _skillSliders;

        public CreateCharTradeGump(PlayerMobile character, ProfessionInfo profession) : base(0, 0)
        {
            _character = character;

            foreach (var skill in _character.Skills)
            {
                skill.ValueFixed = 0;
                skill.BaseFixed = 0;
                skill.CapFixed = 0;
                skill.Lock = Lock.Locked;
            }

            Add(new ResizePic(2600)
            {
                X = 100, Y = 80, Width = 470, Height = 372
            });

            // center menu with fancy top
            // public GumpPic(AControl parent, int x, int y, int gumpID, int hue)
            Add(new GumpPic(291, 42, 0x0589, 0));
            Add(new GumpPic(214, 58, 0x058B, 0));
            Add(new GumpPic(300, 51, 0x15A9, 0));

            // title text
            //TextLabelAscii(AControl parent, int x, int y, int font, int hue, string text, int width = 400)
            Add(new Label(ClilocLoader.Instance.GetString(3000326), false, 0x0386, font: 2)
            {
                X = 148, Y = 132
            });

            // strength, dexterity, intelligence
            Add(new Label(ClilocLoader.Instance.GetString(3000111), false, 1, font: 1)
            {
                X = 158, Y = 170
            });

            Add(new Label(ClilocLoader.Instance.GetString(3000112), false, 1, font: 1)
            {
                X = 158, Y = 250
            });

            Add(new Label(ClilocLoader.Instance.GetString(3000113), false, 1, font: 1)
            {
                X = 158, Y = 330
            });

            // sliders for attributes
            _attributeSliders = new HSliderBar[3];
            Add(_attributeSliders[0] = new HSliderBar(164, 196, 93, 10, 60, ProfessionInfo._VoidStats[0], HSliderBarStyle.MetalWidgetRecessedBar, true));
            Add(_attributeSliders[1] = new HSliderBar(164, 276, 93, 10, 60, ProfessionInfo._VoidStats[1], HSliderBarStyle.MetalWidgetRecessedBar, true));
            Add(_attributeSliders[2] = new HSliderBar(164, 356, 93, 10, 60, ProfessionInfo._VoidStats[2], HSliderBarStyle.MetalWidgetRecessedBar, true));

            string[] skillList = SkillsLoader.Instance.SortedSkills.Select(s => ( 
                                                                                    (s.Index == 52 || 
                                                                                     s.Index == 53 && (World.ClientFeatures.Flags & CharacterListFlags.CLF_SAMURAI_NINJA) == 0)) ||
                                                                                s.Index == 54 ? "" : s.Name).ToArray();

            int y = 172;
            _skillSliders = new HSliderBar[CharCreationGump._skillsCount];
            _skills = new Combobox[CharCreationGump._skillsCount];

            for (var i = 0; i < CharCreationGump._skillsCount; i++)
            {
                Add(_skills[i] = new Combobox(344, y, 182, skillList, -1, 200, false, "Click here"));
                Add(_skillSliders[i] = new HSliderBar(344, y + 32, 93, 0, 50, ProfessionInfo._VoidSkills[i, 1], HSliderBarStyle.MetalWidgetRecessedBar, true));
                y += 70;
            }

            Add(new Button((int) Buttons.Prev, 0x15A1, 0x15A3, 0x15A2)
            {
                X = 586, Y = 445, ButtonAction = ButtonAction.Activate
            });

            Add(new Button((int) Buttons.Next, 0x15A4, 0x15A6, 0x15A5)
            {
                X = 610, Y = 445, ButtonAction = ButtonAction.Activate
            });

            for (int i = 0; i < _attributeSliders.Length; i++)
            {
                for (int j = 0; j < _attributeSliders.Length; j++)
                {
                    if (i != j)
                        _attributeSliders[i].AddParisSlider(_attributeSliders[j]);
                }
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

        public override void OnButtonClick(int buttonID)
        {
            var charCreationGump = UIManager.GetGump<CharCreationGump>();

            switch ((Buttons) buttonID)
            {
                case Buttons.Prev:
                    charCreationGump.StepBack();

                    break;

                case Buttons.Next:

                    if (ValidateValues())
                    {
                        for (int i = 0; i < _skills.Length; i++)
                        {
                            if (_skills[i].SelectedIndex != -1)
                            {
                                var skill = _character.Skills[SkillsLoader.Instance.SortedSkills[_skills[i].SelectedIndex].Index];
                                skill.ValueFixed = (ushort) _skillSliders[i].Value;
                                skill.BaseFixed = 0;
                                skill.CapFixed = 0;
                                skill.Lock = Lock.Locked;
                            }
                        }

                        _character.Strength = (ushort) _attributeSliders[0].Value;
                        _character.Intelligence = (ushort) _attributeSliders[1].Value;
                        _character.Dexterity = (ushort) _attributeSliders[2].Value;

                        charCreationGump.SetAttributes(true);
                    }

                    break;
            }

            base.OnButtonClick(buttonID);
        }

        private bool ValidateValues()
        {
            if (_skills.All(s => s.SelectedIndex >= 0))
            {
                int duplicated = _skills.GroupBy(o => o.SelectedIndex).Count(o => o.Count() > 1);

                if (duplicated > 0)
                {
                    UIManager.GetGump<CharCreationGump>()?.ShowMessage(ClilocLoader.Instance.GetString(1080032));

                    return false;
                }
            }
            else
            {
                UIManager.GetGump<CharCreationGump>()?.ShowMessage(ClilocLoader.Instance.GetString(1080032));

                return false;
            }

            return true;
        }

        private enum Buttons
        {
            Prev,
            Next
        }
    }
}