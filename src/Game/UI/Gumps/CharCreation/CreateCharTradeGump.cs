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

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps.Login;
using ClassicUO.IO;

namespace ClassicUO.Game.UI.Gumps.CharCreation
{
    internal class CreateCharTradeGump : Gump
    {
        private readonly HSliderBar[] _attributeSliders;
        private readonly HSliderBar[] _skillSliders;
        private readonly PlayerMobile _character;
        private readonly Combobox[] _skills;

        public CreateCharTradeGump(PlayerMobile character, ProfessionInfo profession) : base(0, 0)
        {
            _character = character;

            foreach (var skill in _character.Skills)
                _character.UpdateSkill(skill.Index, 0, 0, Lock.Locked, 0);

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
            Add(new Label(FileManager.Cliloc.GetString(3000326), false, 0x0386, font: 2)
            {
                X = 148, Y = 132
            });

            // strength, dexterity, intelligence
            Add(new Label(FileManager.Cliloc.GetString(3000111), false, 1, font: 1)
            {
                X = 158, Y = 170
            });

            Add(new Label(FileManager.Cliloc.GetString(3000112), false, 1, font: 1)
            {
                X = 158, Y = 250
            });

            Add(new Label(FileManager.Cliloc.GetString(3000113), false, 1, font: 1)
            {
                X = 158, Y = 330
            });

            // sliders for attributes
            _attributeSliders = new HSliderBar[3];
            var values = FileManager.ClientVersion >= ClientVersions.CV_70160 ? 15 : 10;
            Add(_attributeSliders[0] = new HSliderBar(164, 196, 93, 10, 60, 60, HSliderBarStyle.MetalWidgetRecessedBar, true));
            Add(_attributeSliders[1] = new HSliderBar(164, 276, 93, 10, 60, values, HSliderBarStyle.MetalWidgetRecessedBar, true));
            Add(_attributeSliders[2] = new HSliderBar(164, 356, 93, 10, 60, values, HSliderBarStyle.MetalWidgetRecessedBar, true));
            var skillCount = 3;
            var initialValue = 50;

            if (FileManager.ClientVersion >= ClientVersions.CV_70160)
            {
                skillCount = 4;
                initialValue = 30;
            }

            string[] skillList = FileManager.Skills.SkillNames;
            int y = 172;
            _skillSliders = new HSliderBar[skillCount];
            _skills = new Combobox[skillCount];

            for (var i = 0; i < skillCount; i++)
            {
                if (FileManager.ClientVersion < ClientVersions.CV_70160 && i == 2)
                    initialValue = 0;
                Add(_skills[i] = new Combobox(344, y, 182, skillList, -1, 200, false, "Click here"));
                Add(_skillSliders[i] = new HSliderBar(344, y + 32, 93, 0, 50, initialValue, HSliderBarStyle.MetalWidgetRecessedBar, true));
                y += 70;
            }

			if (profession.Skills.Any())
			{
				for (int i = 0; i < skillCount; i++)
					_skillSliders[i].Value = 0;

				int GetSkillIndex(string name)
				{
					/* Not sure if other cases exist. 
					 * 7.0.20.0 has a specific function to convert string -> index for each skill in prof.txt. */
					if (String.Equals(name, "Blacksmith", StringComparison.CurrentCulture))
						name = "Blacksmithy";
					else if (String.Equals(name, "AnimalLore", StringComparison.CurrentCulture))
						name = "Animal Lore";
					else if (String.Equals(name, "ItemID", StringComparison.CurrentCulture))
						name = "Item Identification";
					else if (String.Equals(name, "ArmsLore", StringComparison.CurrentCulture))
						name = "Arms Lore";
					else if (String.Equals(name, "Bowcraft", StringComparison.CurrentCulture))
						name = "Bowcraft/Fletching";
					else if (String.Equals(name, "DetectHidden", StringComparison.CurrentCulture))
						name = "Detecting Hidden";
					else if (String.Equals(name, "Enticement", StringComparison.CurrentCulture))
						name = "Discordance";
					else if (String.Equals(name, "EvaluateIntelligence", StringComparison.CurrentCulture))
						name = "Evaluating Intelligence";
					else if (String.Equals(name, "ForensicEvaluation", StringComparison.CurrentCulture))
						name = "Forensic Evaluation";
					else if (String.Equals(name, "ResistingSpells", StringComparison.CurrentCulture))
						name = "Resisting Spells";
					else if (String.Equals(name, "SpiritSpeak", StringComparison.CurrentCulture))
						name = "Spirit Speak";
					else if (String.Equals(name, "AnimalTaming", StringComparison.CurrentCulture))
						name = "Animal Taming";
					else if (String.Equals(name, "TasteIdentification", StringComparison.CurrentCulture))
						name = "Taste Identification";
					else if (String.Equals(name, "MaceFighting", StringComparison.CurrentCulture))
						name = "Mace Fighting";
					else if (String.Equals(name, "Disarm", StringComparison.CurrentCulture))
						name = "Remove Trap";

					return Array.IndexOf(skillList, name);
				}

				var skillIndex = 0;
				foreach (var skillKVP in profession.Skills)
				{
					var skillCombo = _skills[skillIndex];
					var skillSlider = _skillSliders[skillIndex];

					var index = GetSkillIndex(skillKVP.Key);

					if (index > 0)
					{
						skillCombo.SelectedIndex = index;
						skillSlider.Value = skillKVP.Value;
					}

					skillIndex++;
				}
			}

			if (profession.Stats.Count > 0)
			{
				if (profession.Stats.TryGetValue("Str", out var str))
					_attributeSliders[0].Value = str;

				if (profession.Stats.TryGetValue("Dex", out var dex))
					_attributeSliders[1].Value = dex;

				if (profession.Stats.TryGetValue("Int", out var intell))
					_attributeSliders[2].Value = intell;
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
            var charCreationGump = Engine.UI.GetByLocalSerial<CharCreationGump>();

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
                                _character.UpdateSkill(_skills[i].SelectedIndex, (ushort) _skillSliders[i].Value, 0, Lock.Locked, 0);
                        }

                        _character.Strength = (ushort) _attributeSliders[0].Value;
                        _character.Dexterity = (ushort) _attributeSliders[1].Value;
                        _character.Intelligence = (ushort) _attributeSliders[2].Value;

						charCreationGump.SetAttributes();
					}

                    break;
            }

            base.OnButtonClick(buttonID);
        }

        private bool ValidateValues()
        {
            if (_skills.All(s => s.SelectedIndex >= 0))
            {
                int duplicated = _skills.GroupBy(o => o.SelectedIndex).Count(o => o.Count<Combobox>() > 1);

                if (duplicated > 0)
                {
                    Engine.UI.GetByLocalSerial<CharCreationGump>()?.ShowMessage(FileManager.Cliloc.GetString(1080032));

                    return false;
                }
            }
            else
            {
                Engine.UI.GetByLocalSerial<CharCreationGump>()?.ShowMessage(FileManager.Cliloc.GetString(1080032));

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