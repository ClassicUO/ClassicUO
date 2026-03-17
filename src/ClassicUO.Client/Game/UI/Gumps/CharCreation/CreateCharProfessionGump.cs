// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Configuration;
using System.Collections.Generic;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Assets;

namespace ClassicUO.Game.UI.Gumps.CharCreation
{
    internal class CreateCharProfessionGump : Gump
    {
        private readonly ProfessionInfo _Parent;

        public CreateCharProfessionGump(World world, ProfessionInfo parent = null) : base(world, 0, 0)
        {
            _Parent = parent;

            if (parent == null || !World.Context.Game.UO.FileManager.Professions.Professions.TryGetValue(parent, out List<ProfessionInfo> professions) || professions == null)
            {
                professions = new List<ProfessionInfo>(World.Context.Game.UO.FileManager.Professions.Professions.Keys);
            }

            /* Build the gump */
            Add
            (
                new ResizePic(2600, World.Context)
                {
                    X = 100,
                    Y = 80,
                    Width = 470,
                    Height = 372
                }
            );

            Add(new GumpPic(291, 42, 0x0589, 0, World.Context));
            Add(new GumpPic(214, 58, 0x058B, 0, World.Context));
            Add(new GumpPic(300, 51, 0x15A9, 0, World.Context));

            ClilocLoader localization = World.Context.Game.UO.FileManager.Clilocs;

            bool isAsianLang = string.Compare(World.Settings.Language, "CHT", StringComparison.InvariantCultureIgnoreCase) == 0 ||
                string.Compare(World.Settings.Language, "KOR", StringComparison.InvariantCultureIgnoreCase) == 0 ||
                string.Compare(World.Settings.Language, "JPN", StringComparison.InvariantCultureIgnoreCase) == 0;

            bool unicode = isAsianLang;
            byte font = (byte)(isAsianLang ? 1 : 2);
            ushort hue = (ushort)(isAsianLang ? 0xFFFF : 0x0386);

            Add
            (
                new Label(World.Context, localization.GetString(3000326, "Choose a Trade for Your Character"), unicode, hue, font: font)
                {
                    X = 158,
                    Y = 132
                }
            );

            for (int i = 0; i < professions.Count; i++)
            {
                int cx = i % 2;
                int cy = i >> 1;

                Add
                (
                    new ProfessionInfoGump(World.Context, professions[i])
                    {
                        X = 145 + cx * 195,
                        Y = 168 + cy * 70,

                        Selected = SelectProfession
                    }
                );
            }

            Add
            (
                new Button(World.Context, (int) Buttons.Prev, 0x15A1, 0x15A3, 0x15A2)
                {
                    X = 586,
                    Y = 445,
                    ButtonAction = ButtonAction.Activate
                }
            );
        }

        public void SelectProfession(ProfessionInfo info)
        {
            if (info.Type == ProfessionLoader.PROF_TYPE.CATEGORY && World.Context.Game.UO.FileManager.Professions.Professions.TryGetValue(info, out List<ProfessionInfo> list) && list != null)
            {
                Parent.Add(new CreateCharProfessionGump(World, info));
                Parent.Remove(this);
            }
            else
            {
                CharCreationGump charCreationGump = World.Context.UI.GetGump<CharCreationGump>();

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
                        Parent.Add(new CreateCharProfessionGump(World));
                        Parent.Remove(this);
                    }
                    else
                    {
                        Parent.Remove(this);
                        CharCreationGump charCreationGump = World.Context.UI.GetGump<CharCreationGump>();
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

        public ProfessionInfoGump(GameContext context, ProfessionInfo info) : base(context)
        {
            _info = info;

            ClilocLoader localization = context.Game.UO.FileManager.Clilocs;

            ResizePic background = new ResizePic(3000, Context)
            {
                Width = 175,
                Height = 34
            };

            background.SetTooltip(localization.GetString(info.Description), 250);

            Add(background);

            Add
            (
                new Label(Context, localization.GetString(info.Localization), true, 0x00, font: 1)
                {
                    X = 7,
                    Y = 8
                }
            );

            Add(new GumpPic(121, -12, info.Graphic, 0, Context));
        }

        public Action<ProfessionInfo> Selected;

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            base.OnMouseUp(x, y, button);

            if (button == MouseButtonType.Left)
            {
                Selected?.Invoke(_info);
            }
        }
    }
}