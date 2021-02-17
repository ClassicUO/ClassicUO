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

using System.Linq;
using ClassicUO.Data;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.IO.Resources;
using ClassicUO.Resources;

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

            foreach (Skill skill in _character.Skills)
            {
                skill.ValueFixed = 0;
                skill.BaseFixed = 0;
                skill.CapFixed = 0;
                skill.Lock = Lock.Locked;
            }

            Add
            (
                new ResizePic(2600)
                {
                    X = 100, Y = 80, Width = 470, Height = 372
                }
            );

            // center menu with fancy top
            // public GumpPic(AControl parent, int x, int y, int gumpID, int hue)
            Add(new GumpPic(291, 42, 0x0589, 0));
            Add(new GumpPic(214, 58, 0x058B, 0));
            Add(new GumpPic(300, 51, 0x15A9, 0));

            // title text
            //TextLabelAscii(AControl parent, int x, int y, int font, int hue, string text, int width = 400)
            Add
            (
                new Label(ClilocLoader.Instance.GetString(3000326), false, 0x0386, font: 2)
                {
                    X = 148, Y = 132
                }
            );

            // strength, dexterity, intelligence
            Add
            (
                new Label(ClilocLoader.Instance.GetString(3000111), false, 1, font: 1)
                {
                    X = 158, Y = 170
                }
            );

            Add
            (
                new Label(ClilocLoader.Instance.GetString(3000112), false, 1, font: 1)
                {
                    X = 158, Y = 250
                }
            );

            Add
            (
                new Label(ClilocLoader.Instance.GetString(3000113), false, 1, font: 1)
                {
                    X = 158, Y = 330
                }
            );

            // sliders for attributes
            _attributeSliders = new HSliderBar[3];

            Add
            (
                _attributeSliders[0] = new HSliderBar
                (
                    164,
                    196,
                    93,
                    10,
                    60,
                    ProfessionInfo._VoidStats[0],
                    HSliderBarStyle.MetalWidgetRecessedBar,
                    true
                )
            );

            Add
            (
                _attributeSliders[1] = new HSliderBar
                (
                    164,
                    276,
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
                    164,
                    356,
                    93,
                    10,
                    60,
                    ProfessionInfo._VoidStats[2],
                    HSliderBarStyle.MetalWidgetRecessedBar,
                    true
                )
            );

            string[] skillList = SkillsLoader.Instance.SortedSkills.Select(s => s.Index == 52 || s.Index == 47 || s.Index == 53 && (World.ClientFeatures.Flags & CharacterListFlags.CLF_SAMURAI_NINJA) == 0 || s.Index == 54 ? "" : s.Name).ToArray();

            int y = 172;
            _skillSliders = new HSliderBar[CharCreationGump._skillsCount];
            _skills = new Combobox[CharCreationGump._skillsCount];

            for (int i = 0; i < CharCreationGump._skillsCount; i++)
            {
                Add
                (
                    _skills[i] = new Combobox
                    (
                        344,
                        y,
                        182,
                        skillList,
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
                        344,
                        y + 32,
                        93,
                        0,
                        50,
                        ProfessionInfo._VoidSkills[i, 1],
                        HSliderBarStyle.MetalWidgetRecessedBar,
                        true
                    )
                );

                y += 70;
            }

            Add
            (
                new Button((int) Buttons.Prev, 0x15A1, 0x15A3, 0x15A2)
                {
                    X = 586, Y = 445, ButtonAction = ButtonAction.Activate
                }
            );

            Add
            (
                new Button((int) Buttons.Next, 0x15A4, 0x15A6, 0x15A5)
                {
                    X = 610, Y = 445, ButtonAction = ButtonAction.Activate
                }
            );

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

        public override void OnButtonClick(int buttonID)
        {
            CharCreationGump charCreationGump = UIManager.GetGump<CharCreationGump>();

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
                                Skill skill = _character.Skills[SkillsLoader.Instance.SortedSkills[_skills[i].SelectedIndex].Index];

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
                UIManager.GetGump<CharCreationGump>()?.ShowMessage(Client.Version <= ClientVersion.CV_5090 ? ResGumps.YouMustHaveThreeUniqueSkillsChosen : ClilocLoader.Instance.GetString(1080032));

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