﻿#region license
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
using System.Collections.Generic;
using ClassicUO.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;

namespace ClassicUO.Game.UI.Gumps.CharCreation
{
    internal class CreateCharProfessionGump : Gump
    {
        private readonly ProfessionInfo _Parent;

        public CreateCharProfessionGump(ProfessionInfo parent = null) : base(0, 0)
        {
            _Parent = parent;
            if (parent == null || !ProfessionLoader.Instance.Professions.TryGetValue(parent, out List<ProfessionInfo> professions) || professions == null) professions = new List<ProfessionInfo>(ProfessionLoader.Instance.Professions.Keys);

            /* Build the gump */
            Add(new ResizePic(2600)
            {
                X = 100,
                Y = 80,
                Width = 470,
                Height = 372
            });

            Add(new GumpPic(291, 42, 0x0589, 0));
            Add(new GumpPic(214, 58, 0x058B, 0));
            Add(new GumpPic(300, 51, 0x15A9, 0));

            ClilocLoader localization = ClilocLoader.Instance;

            Add(new Label(localization.GetString(3000326, "Choose a Trade for Your Character"), false, 0x0386, font: 2)
            {
                X = 158,
                Y = 132
            });

            for (int i = 0; i < professions.Count; i++)
            {
                int cx = i % 2;
                int cy = i >> 1;

                Add(new ProfessionInfoGump(professions[i])
                {
                    X = 145 + cx * 195,
                    Y = 168 + cy * 70,

                    Selected = SelectProfession
                });
            }

            Add(new Button((int) Buttons.Prev, 0x15A1, 0x15A3, 0x15A2)
            {
                X = 586,
                Y = 445,
                ButtonAction = ButtonAction.Activate
            });
        }

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

                charCreationGump?.SetProfession(info);
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((Buttons) buttonID)
            {
                case Buttons.Prev:

                {
                    if (_Parent != null && _Parent.TopLevel)
                    {
                        Parent.Add(new CreateCharProfessionGump());
                        Parent.Remove(this);
                    }
                    else
                    {
                        Parent.Remove(this);
                        CharCreationGump charCreationGump = UIManager.GetGump<CharCreationGump>();
                        charCreationGump?.StepBack();
                    }

                    break;
                }
            }

            base.OnButtonClick(buttonID);
        }

        private enum Buttons
        {
            Prev
        }
    }

    internal class ProfessionInfoGump : Control
    {
        private readonly ProfessionInfo _info;

        public Action<ProfessionInfo> Selected;

        public ProfessionInfoGump(ProfessionInfo info)
        {
            _info = info;

            ClilocLoader localization = ClilocLoader.Instance;

            ResizePic background = new ResizePic(3000)
            {
                Width = 175,
                Height = 34
            };
            background.SetTooltip(localization.GetString(info.Description), 250);

            Add(background);

            Add(new Label(localization.GetString(info.Localization), true, 0x00, font: 1)
            {
                X = 7,
                Y = 8
            });

            Add(new GumpPic(121, -12, info.Graphic, 0));
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            base.OnMouseUp(x, y, button);
            if (button == MouseButtonType.Left) Selected?.Invoke(_info);
        }
    }

    internal class ProfessionInfo
    {
        internal static readonly int[,] _VoidSkills = new int[4, 2] {{0, InitialSkillValue}, {0, InitialSkillValue}, {0, Client.Version < ClientVersion.CV_70160 ? 0 : InitialSkillValue}, {0, InitialSkillValue } };
        internal static readonly int[] _VoidStats = new int[3] {60, RemainStatValue, RemainStatValue};
        public static int InitialSkillValue => Client.Version >= ClientVersion.CV_70160 ? 30 : 50;
        public static int RemainStatValue => Client.Version >= ClientVersion.CV_70160 ? 15 : 10;
        public string Name { get; set; }
        public string TrueName { get; set; }
        public int Localization { get; set; }
        public int Description { get; set; }
        public int DescriptionIndex { get; set; }
        public ProfessionLoader.PROF_TYPE Type { get; set; }

        public ushort Graphic { get; set; }

        public bool TopLevel { get; set; }
        public int[,] SkillDefVal { get; set; } = _VoidSkills;
        public int[] StatsVal { get; set; } = _VoidStats;
        public List<string> Childrens { get; set; }
    }
}