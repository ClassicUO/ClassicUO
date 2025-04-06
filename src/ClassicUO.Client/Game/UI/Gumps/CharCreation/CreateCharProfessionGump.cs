// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Configuration;
using System.Collections.Generic;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Sdk.Assets;
using ClassicUO.Game.Services;

namespace ClassicUO.Game.UI.Gumps.CharCreation
{
    internal class CreateCharProfessionGump : Gump
    {
        private readonly ProfessionInfo _Parent;

        public CreateCharProfessionGump(World world, ProfessionInfo parent = null) : base(world, 0, 0)
        {
            _Parent = parent;

            if (parent == null || !ServiceProvider.Get<UOService>().FileManager.Professions.Professions.TryGetValue(parent, out var professions) || professions == null)
            {
                professions = new List<ProfessionInfo>(ServiceProvider.Get<UOService>().FileManager.Professions.Professions.Keys);
            }

            /* Build the gump */
            Add
            (
                new ResizePic(2600)
                {
                    X = 100,
                    Y = 80,
                    Width = 470,
                    Height = 372
                }
            );

            Add(new GumpPic(291, 42, 0x0589, 0));
            Add(new GumpPic(214, 58, 0x058B, 0));
            Add(new GumpPic(300, 51, 0x15A9, 0));

            ClilocLoader localization = ServiceProvider.Get<UOService>().FileManager.Clilocs;

            bool isAsianLang = string.Compare(Settings.GlobalSettings.Language, "CHT", StringComparison.InvariantCultureIgnoreCase) == 0 ||
                string.Compare(Settings.GlobalSettings.Language, "KOR", StringComparison.InvariantCultureIgnoreCase) == 0 ||
                string.Compare(Settings.GlobalSettings.Language, "JPN", StringComparison.InvariantCultureIgnoreCase) == 0;

            bool unicode = isAsianLang;
            byte font = (byte)(isAsianLang ? 1 : 2);
            ushort hue = (ushort)(isAsianLang ? 0xFFFF : 0x0386);

            Add
            (
                new Label(localization.GetString(3000326, "Choose a Trade for Your Character"), unicode, hue, font: font)
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
                    new ProfessionInfoGump(professions[i])
                    {
                        X = 145 + cx * 195,
                        Y = 168 + cy * 70,

                        Selected = SelectProfession
                    }
                );
            }

            Add
            (
                new Button((int) Buttons.Prev, 0x15A1, 0x15A3, 0x15A2)
                {
                    X = 586,
                    Y = 445,
                    ButtonAction = ButtonAction.Activate
                }
            );
        }

        public void SelectProfession(ProfessionInfo info)
        {
            if (info.Type == ProfessionLoader.PROF_TYPE.CATEGORY && ServiceProvider.Get<UOService>().FileManager.Professions.Professions.TryGetValue(info, out var list) && list != null)
            {
                Parent?.Add(new CreateCharProfessionGump(World, info));
                Parent?.Remove(this);
            }
            else
            {
                var charCreationGump = ServiceProvider.Get<UIService>().GetGump<CharCreationGump>();

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
                        Parent?.Add(new CreateCharProfessionGump(World));
                        Parent?.Remove(this);
                    }
                    else
                    {
                        Parent?.Remove(this);
                        var charCreationGump = ServiceProvider.Get<UIService>().GetGump<CharCreationGump>();
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

        public ProfessionInfoGump(ProfessionInfo info)
        {
            _info = info;

            ClilocLoader localization = ServiceProvider.Get<UOService>().FileManager.Clilocs;

            ResizePic background = new ResizePic(3000)
            {
                Width = 175,
                Height = 34
            };

            background.SetTooltip(localization.GetString(info.Description), 250);

            Add(background);

            Add
            (
                new Label(localization.GetString(info.Localization), true, 0x00, font: 1)
                {
                    X = 7,
                    Y = 8
                }
            );

            Add(new GumpPic(121, -12, info.Graphic, 0));
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