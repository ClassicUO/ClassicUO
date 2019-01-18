#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
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

using ClassicUO.Game.Data;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class SkillGumpAdvanced : Gump
    {
        private const int WIDTH = 500;
        private const int HEIGHT = 400;

        private readonly ScrollArea _scrollArea;
        private readonly List<SkillListEntry> _skillListEntries = new List<SkillListEntry>();
        private double _totalReal, _totalValue;
        private bool _updateSkillsNeeded;

        public SkillGumpAdvanced() : base(0, 0)
        {
            _totalReal = 0;
            _totalValue = 0;
            CanBeSaved = true;
            CanMove = true;
            AcceptMouseInput = false;
            Add(new GameBorder(0, 0, WIDTH, HEIGHT, 4));

            Add(new GumpPicTiled(4, 4, WIDTH - 8, HEIGHT - 8, 0x0A40)
            {
                IsTransparent = true,
                Alpha = 0.5f,
            });

            Add(new GumpPicTiled(4, 4, WIDTH - 8, HEIGHT - 8, 0x0A40)
            {
                IsTransparent = true,
                Alpha = 0.5f,
            });

            _scrollArea = new ScrollArea(20, 60, WIDTH - 40, 250, true)
            {
                AcceptMouseInput = true
            };
            Add(_scrollArea);

            Add(new Label("Skill", true, 1153)
            {
                X = 20, Y = 25
            });

            Add(new Label("Real", true, 1153)
            {
                X = 220, Y = 25
            });

            Add(new Label("Base", true, 1153)
            {
                X = 300, Y = 25
            });

            Add(new Label("Cap", true, 1153)
            {
                X = 380, Y = 25
            });

            //======================================================================================
            Add(new Line(20, 60, 435, 1, 0xFFFFFFFF));
            Add(new Line(20, 310, 435, 1, 0xFFFFFFFF));

            Add(new Label("Total Skill(Real): ", true, 1153)
            {
                X = 30, Y = 320
            });

            Add(new Label("Total Skill(Base): ", true, 1153)
            {
                X = 30, Y = 345
            });
            World.Player.SkillsChanged += OnSkillChanged;
        }

        protected override void OnInitialize()
        {
            _totalReal = 0;
            _totalValue = 0;
            _scrollArea.Clear();

            foreach (SkillListEntry entry in _skillListEntries)
            {
                entry.Clear();
                entry.Dispose();
            }

            _skillListEntries.Clear();

            foreach (Skill skill in World.Player.Skills)
            {
                _totalReal += skill.Base;
                _totalValue += skill.Value;
                Label skillName = new Label(skill.Name, true, 1153, font: 3); //3
                Label skillValueBase = new Label(skill.Base.ToString(), true, 1153, font: 3);
                Label skillValue = new Label(skill.Value.ToString(), true, 1153, font: 3);
                Label skillCap = new Label(skill.Cap.ToString(), true, 1153, font: 3);
                _skillListEntries.Add(new SkillListEntry(skillName, skillValueBase, skillValue, skillCap, skill));
            }

            for (int i = 0; i < _skillListEntries.Count; i++) _scrollArea.Add(_skillListEntries[i]);

            Add(new Label(_totalReal.ToString(), true, 1153)
            {
                X = 170, Y = 320
            });

            Add(new Label(_totalValue.ToString(), true, 1153)
            {
                X = 170, Y = 345
            });
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (_updateSkillsNeeded)
            {
                OnInitialize();
                _updateSkillsNeeded = false;
            }
        }

        public override void Dispose()
        {
            World.Player.SkillsChanged -= OnSkillChanged;
            base.Dispose();
        }

        private void OnSkillChanged(object sender, EventArgs args)
        {
            _updateSkillsNeeded = true;
        }
    }

    internal class SkillListEntry : Control
    {
        private readonly Button _activeUse;
        private readonly Skill _skill;

        public SkillListEntry(Label skillname, Label skillvaluebase, Label skillvalue, Label skillcap, Skill skill)
        {
            Height = 20;
            Label skillName = skillname;
            Label skillValueBase = skillvaluebase;
            Label skillValue = skillvalue;
            Label skillCap = skillcap;
            _skill = skill;
            skillName.X = 20;

            if (skill.IsClickable)
            {
                Add(_activeUse = new Button((int) Buttons.ActiveSkillUse, 0x837, 0x838)
                {
                    X = 0, Y = 4, ButtonAction = ButtonAction.Activate
                });
            }

            Add(skillName);
            //======================
            skillValueBase.X = 200;
            Add(skillValueBase);
            //======================
            skillValue.X = 280;
            Add(skillValue);
            //======================
            skillCap.X = 360;
            Add(skillCap);
            GumpPic loc = new GumpPic(425, 4, (Graphic) (skill.Lock == Lock.Up ? 0x983 : skill.Lock == Lock.Down ? 0x985 : 0x82C), 0);
            Add(loc);

            loc.MouseClick += (sender, e) =>
            {
                switch (_skill.Lock)
                {
                    case Lock.Up:
                        _skill.Lock = Lock.Down;
                        GameActions.ChangeSkillLockStatus((ushort) _skill.Index, (byte) Lock.Down);
                        loc.Graphic = 0x985;
                        loc.Texture = FileManager.Gumps.GetTexture(0x985);

                        break;
                    case Lock.Down:
                        _skill.Lock = Lock.Locked;
                        GameActions.ChangeSkillLockStatus((ushort) _skill.Index, (byte) Lock.Locked);
                        loc.Graphic = 0x82C;
                        loc.Texture = FileManager.Gumps.GetTexture(0x82C);

                        break;
                    case Lock.Locked:
                        _skill.Lock = Lock.Up;
                        GameActions.ChangeSkillLockStatus((ushort) _skill.Index, (byte) Lock.Up);
                        loc.Graphic = 0x983;
                        loc.Texture = FileManager.Gumps.GetTexture(0x983);

                        break;
                }
            };
        }

        protected override void OnDragBegin(int x, int y)
        {
            if (_skill.IsClickable && Mouse.LButtonPressed)
            {
                uint serial = (uint) (World.Player + _skill.Index + 1);

                if (Engine.UI.GetByLocalSerial<SkillButtonGump>(serial) != null)
                    Engine.UI.Remove<SkillButtonGump>(serial);

                SkillButtonGump skillButtonGump = new SkillButtonGump(_skill, Mouse.Position.X, Mouse.Position.Y);
                Engine.UI.Add(skillButtonGump);
                Rectangle rect = FileManager.Gumps.GetTexture(0x24B8).Bounds;
                Engine.UI.AttemptDragControl(skillButtonGump, new Point(Mouse.Position.X + (rect.Width >> 1), Mouse.Position.Y + (rect.Height >> 1)), true);
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((Buttons) buttonID)
            {
                case Buttons.ActiveSkillUse:
                    GameActions.UseSkill(_skill.Index);

                    break;
            }
        }

        private enum Buttons
        {
            ActiveSkillUse = 1
        }
    }
}