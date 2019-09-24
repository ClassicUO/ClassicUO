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
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class SkillGumpAdvanced : Gump
    {
        private const int WIDTH = 500;
        private const int HEIGHT = 360;

        private readonly Dictionary<Buttons, string> _buttonsToSkillsValues = new Dictionary<Buttons, string>
        {
            {Buttons.SortName, "Name"},
            {Buttons.SortReal, "Base"},
            {Buttons.SortBase, "Value"},
            {Buttons.SortCap, "Cap"}
        };

        private readonly ScrollArea _scrollArea;
        private readonly List<SkillListEntry> _skillListEntries = new List<SkillListEntry>();
        private readonly GumpPic _sortOrderIndicator;


        private bool _sortAsc;
        private string _sortField;
        private double _totalReal, _totalValue;
        private bool _updateSkillsNeeded;

        public SkillGumpAdvanced() : base(0, 0)
        {
            _totalReal = 0;
            _totalValue = 0;
            CanBeSaved = true;
            CanMove = true;
            AcceptMouseInput = true;
            WantUpdateSize = false;

            Width = WIDTH;
            Height = HEIGHT;

            Add(new AlphaBlendControl(0.05f)
            {
                X = 1,
                Y = 1,
                Width = WIDTH - 2,
                Height = HEIGHT - 2
            });

            _scrollArea = new ScrollArea(20, 60, WIDTH - 40, 250, true)
            {
                AcceptMouseInput = true
            };
            Add(_scrollArea);

            Add(new NiceButton(10, 10, 180, 25, ButtonAction.Activate, "Name")
            {
                ButtonParameter = (int) Buttons.SortName,
                IsSelected = true,
                X = 40,
                Y = 25
            });

            Add(new NiceButton(10, 10, 80, 25, ButtonAction.Activate, "Real")
            {
                ButtonParameter = (int) Buttons.SortReal,
                X = 220,
                Y = 25
            });

            Add(new NiceButton(10, 10, 80, 25, ButtonAction.Activate, "Base")
            {
                ButtonParameter = (int) Buttons.SortBase,
                X = 300,
                Y = 25
            });

            Add(new NiceButton(10, 10, 80, 25, ButtonAction.Activate, "Cap")
            {
                ButtonParameter = (int) Buttons.SortCap,
                X = 380,
                Y = 25
            });

            Add(new Line(20, 60, 435, 1, 0xFFFFFFFF));
            Add(new Line(20, 310, 435, 1, 0xFFFFFFFF));

            Add(_sortOrderIndicator = new GumpPic(0, 0, 0x985, 0));
            OnButtonClick((int) Buttons.SortName);
        }

        public override void OnButtonClick(int buttonID)
        {
            if (_buttonsToSkillsValues.TryGetValue((Buttons) buttonID, out string fieldValue))
            {
                if (_sortField == fieldValue)
                    _sortAsc = !_sortAsc;

                _sortField = fieldValue;
            }

            if (FindControls<NiceButton>().Any(s => s.ButtonParameter == buttonID))
            {
                NiceButton btn = FindControls<NiceButton>().First(s => s.ButtonParameter == buttonID);
                Graphic g = (Graphic) (_sortAsc ? 0x985 : 0x983);

                _sortOrderIndicator.Graphic = g;
                _sortOrderIndicator.Texture = FileManager.Gumps.GetTexture(g);
                _sortOrderIndicator.X = btn.X + btn.Width - 15;
                _sortOrderIndicator.Y = btn.Y + 5;
            }

            _updateSkillsNeeded = true;
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

            var pi = typeof(Skill).GetProperty(_sortField);
            List<Skill> sortSkills = new List<Skill>(World.Player.Skills.OrderBy(x => pi.GetValue(x, null)));

            if (_sortAsc)
                sortSkills.Reverse();

            foreach (Skill skill in sortSkills)
            {
                _totalReal += skill.Base;
                _totalValue += skill.Value;

                Label skillName = new Label(skill.Name, true, 1153, font: 3);
                Label skillValueBase = new Label(skill.Base.ToString(), true, 1153, font: 3);
                Label skillValue = new Label(skill.Value.ToString(), true, 1153, font: 3);
                Label skillCap = new Label(skill.Cap.ToString(), true, 1153, font: 3);

                _skillListEntries.Add(new SkillListEntry(skillName, skillValueBase, skillValue, skillCap, skill));
            }

            foreach (SkillListEntry t in _skillListEntries)
                _scrollArea.Add(t);

            Add(new Label("Total: ", true, 1153) {X = 40, Y = 320});
            Add(new Label(_totalReal.ToString(), true, 1153) {X = 220, Y = 320});
            Add(new Label(_totalValue.ToString(), true, 1153) {X = 300, Y = 320});
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (_updateSkillsNeeded)
            {
                foreach (var label in Children.OfType<Label>())
                    label.Dispose();

                OnInitialize();

                _updateSkillsNeeded = false;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ResetHueVector();

            batcher.DrawRectangle(Textures.GetTexture(Color.Gray), x, y, Width, Height, ref _hueVector);

            return base.Draw(batcher, x, y);
        }


        public void ForceUpdate() => _updateSkillsNeeded = true;

        private enum Buttons
        {
            SortName = 1,
            SortReal = 2,
            SortBase = 3,
            SortCap = 4
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
            skillValueBase.X = 200;

            Add(skillValueBase);
            skillValue.X = 280;

            Add(skillValue);
            skillCap.X = 360;

            Add(skillCap);

            GumpPic loc = new GumpPic(425, 4, (Graphic) (skill.Lock == Lock.Up ? 0x983 : skill.Lock == Lock.Down ? 0x985 : 0x82C), 0);
            Add(loc);

            loc.MouseUp += (sender, e) =>
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

                if (Engine.UI.GetGump<SkillButtonGump>(serial) != null)
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