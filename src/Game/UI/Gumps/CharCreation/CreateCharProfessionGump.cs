using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using System;
using System.Collections.Generic;

namespace ClassicUO.Game.UI.Gumps.CharCreation
{
    internal class CreateCharProfessionGump : Gump
    {
        private readonly ProfessionInfo _Parent;
        public CreateCharProfessionGump(ProfessionInfo parent = null) : base(0, 0)
        {
            _Parent = parent;
            if (parent == null || !FileManager.Profession.Professions.TryGetValue(parent, out List<ProfessionInfo> professions) || professions == null)
            {
                professions = new List<ProfessionInfo>(FileManager.Profession.Professions.Keys);
            }

            /* Build the gump */
            Add(new ResizePic(2600)
            {
                X = 100,
                Y = 80,
                Width = 470,
                Height = 372,
            });

            Add(new GumpPic(291, 42, 0x0589, 0));
            Add(new GumpPic(214, 58, 0x058B, 0));
            Add(new GumpPic(300, 51, 0x15A9, 0));

            ClilocLoader localization = FileManager.Cliloc;

            Add(new Label(localization.Translate(3000326), false, 0x0386, font: 2)
            {
                X = 158,
                Y = 132,
            });

            for (int i = 0; i < professions.Count; i++)
            {
                int cx = i % 2;
                int cy = i / 2;

                Add(new ProfessionInfoGump(professions[i])
                {
                    X = 145 + (cx * 195),
                    Y = 168 + (cy * 70),

                    Selected = SelectProfession,
                });
            }

            Add(new Button((int)Buttons.Prev, 0x15A1, 0x15A3, 0x15A2)
            {
                X = 586,
                Y = 445,
                ButtonAction = ButtonAction.Activate
            });
        }

        public void SelectProfession(ProfessionInfo info)
        {
            if (info.Type == ProfessionLoader.PROF_TYPE.CATEGORY && FileManager.Profession.Professions.TryGetValue(info, out List<ProfessionInfo> list) && list != null)
            {
                Parent.Add(new CreateCharProfessionGump(info));
                Parent.Remove(this);
            }
            else
            {
                CharCreationGump charCreationGump = Engine.UI.GetByLocalSerial<CharCreationGump>();

                if (charCreationGump != null)
                {
                    charCreationGump.SetProfession(info);
                }
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((Buttons)buttonID)
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
                        CharCreationGump charCreationGump = Engine.UI.GetByLocalSerial<CharCreationGump>();
                        charCreationGump?.StepBack();
                    }
                    break;
                }
            }

            base.OnButtonClick(buttonID);
        }

        private enum Buttons
        {
            Prev,
        }
    }

    internal class ProfessionInfoGump : Control
    {
        private ProfessionInfo _info;

        public Action<ProfessionInfo> Selected;

        public ProfessionInfoGump(ProfessionInfo info)
        {
            _info = info;

            ClilocLoader localization = FileManager.Cliloc;

            ResizePic background = new ResizePic(3000)
            {
                Width = 175,
                Height = 34,
            };
            background.SetTooltip(localization.Translate(info.Description), 250);

            Add(background);

            Add(new Label(localization.Translate(info.Localization), true, 0x00, font: 1)
            {
                X = 7,
                Y = 8,
            });

            Add(new GumpPic(121, -12, info.Graphic, 0));
        }

        protected override void OnMouseClick(int x, int y, MouseButton button)
        {
            base.OnMouseClick(x, y, button);

            if (button == MouseButton.Left)
            {
                Selected?.Invoke(_info);
            }
        }
    }

    internal class ProfessionInfo
    {
        public string Name { get; set; }
        public string TrueName { get; set; }
        public int Localization { get; set; }
        public int Description { get; set; }
        public int DescriptionIndex { get; set; }
        public ProfessionLoader.PROF_TYPE Type { get; set; }

        public Graphic Graphic { get; set; }

        public bool TopLevel { get; set; }

        private static readonly int[,] _VoidSkills = new int[4, 2] { { 0, 0 }, { 1, 0 }, { 2, 0 }, { 3, 0 } };
        public int[,] SkillDefVal { get; set; } = _VoidSkills;
        private static readonly int[] _VoidStats = new int[3] { 0, 0, 0 };
        public int[] StatsVal { get; set; } = _VoidStats;
        public List<string> Childrens { get; set; }
        public Dictionary<string, int> Skills { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> Stats { get; set; } = new Dictionary<string, int>();
    }
}
