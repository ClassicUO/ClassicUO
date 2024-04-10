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

using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ClassicUO.Game.UI.Gumps
{
    internal class SkillGumpAdvanced : Gump
    {
        private const int WIDTH = 400;

        private readonly Dictionary<Buttons, string> _buttonsToSkillsValues = new Dictionary<
            Buttons,
            string
        >
        {
            { Buttons.SortName, "Name" },
            { Buttons.SortReal, "Base" },
            { Buttons.SortBase, "Value" },
            { Buttons.SortCap, "Cap" },
            { Buttons.SortLock, "Lock" }
        };

        private readonly DataBox _databox;
        private readonly List<SkillListEntry> _skillListEntries = new List<SkillListEntry>();

        public static bool Dragging;

        private bool _sortAsc;
        private string _sortField;
        private readonly GumpPic _sortOrderIndicator;
        private double _totalReal,
            _totalValue;
        private bool _updateSkillsNeeded;
        private Button resizeDrag;
        private Area BottomArea;
        private int dragStartH;
        private Label real, value;
        private AlphaBlendControl background;

        private ScrollArea area;

        public SkillGumpAdvanced() : base(0, 0)
        {
            _totalReal = 0;
            _totalValue = 0;
            CanMove = true;
            AcceptMouseInput = true;
            WantUpdateSize = false;

            Width = WIDTH;
            Height = 310;
            if (ProfileManager.CurrentProfile != null)
                Height = ProfileManager.CurrentProfile.AdvancedSkillsGumpHeight;

            Add
            (background =
                new AlphaBlendControl(0.65f)
                {
                    X = 1,
                    Y = 1,
                    Width = WIDTH - 1,
                    Height = Height - 1
                }
            );

            area = new ScrollArea
            (
                5,
                40,
                WIDTH - 10,
                Height - 60,
                true
            )
            {
                AcceptMouseInput = true
            };

            Add(area);

            _databox = new DataBox(0, 0, 1, 1);
            _databox.WantUpdateSize = true;

            area.Add(_databox);

            NiceButton _;
            Add
            (_ =
                new NiceButton
                (
                    5,
                    5,
                    180,
                    25,
                    ButtonAction.Activate,
                    ResGumps.Name
                )
                {
                    ButtonParameter = (int)Buttons.SortName,
                    IsSelected = true
                }
            );

            Add
            (_ =
                new NiceButton
                (
                    _.X + _.Width + 10,
                    _.Y,
                    50,
                    25,
                    ButtonAction.Activate,
                    ResGumps.Real
                )
                {
                    ButtonParameter = (int)Buttons.SortReal,
                }
            );

            Add
            (_ =
                new NiceButton
                (
                    _.X + _.Width,
                    _.Y,
                    50,
                    25,
                    ButtonAction.Activate,
                    ResGumps.Base
                )
                {
                    ButtonParameter = (int)Buttons.SortBase,
                }
            );

            Add
            (_ =
                new NiceButton
                (
                    _.X + _.Width,
                    _.Y,
                    50,
                    25,
                    ButtonAction.Activate,
                    ResGumps.Cap
                )
                {
                    ButtonParameter = (int)Buttons.SortCap,
                }
            );

            Add
            (_ =
                new NiceButton
                (
                    _.X + _.Width,
                    _.Y,
                    50,
                    25,
                    ButtonAction.Activate,
                    "Lock"
                )
                {
                    ButtonParameter = (int)Buttons.SortLock,
                }
            );

            Add
            (
                new Line
                (
                    area.X,
                    area.Y - 1,
                    area.Width,
                    1,
                    0xFFFFFFFF
                )
            );

            //Add
            //(bottomLine =
            //    new Line
            //    (
            //        area.X,
            //        area.Height + area.Y - 1,
            //        area.Width,
            //        1,
            //        0xFFFFFFFF
            //    )
            //);
            BottomArea = new Area()
            {
                X = 1,
                Y = area.Height + area.Y - 1,
                //AcceptMouseInput = true,
                WantUpdateSize = false,
                Width = Width,
                Height = 20
            };
            Checkbox showGrp;
            BottomArea.Add(showGrp = new Checkbox
            (
                0x00D2,
                0x00D3,
                "Show Groups",
                0xFF,
                1153
            ));
            showGrp.IsChecked = SkillsGroupManager.IsActive;
            showGrp.ValueChanged += (sender, e) =>
            {
                SkillsGroupManager.IsActive = showGrp.IsChecked;
                ForceUpdate();
                SkillsGroupManager.Save();
            };




            Add(BottomArea);

            Add(_sortOrderIndicator = new GumpPic(0, 0, 0x985, 0));
            OnButtonClick((int)Buttons.SortName);

            Add(resizeDrag = new Button(0, 0x837, 0x838, 0x838));
            resizeDrag.MouseDown += ResizeDrag_MouseDown;
            resizeDrag.MouseUp += ResizeDrag_MouseUp;
            resizeDrag.X = Width - 10;
            resizeDrag.Y = Height - 10;
        }

        public override GumpType GumpType => GumpType.SkillMenu;

        public override void OnButtonClick(int buttonID)
        {
            if (_buttonsToSkillsValues.TryGetValue((Buttons)buttonID, out string fieldValue))
            {
                if (_sortField == fieldValue)
                {
                    _sortAsc = !_sortAsc;
                }

                _sortField = fieldValue;
            }

            if (FindControls<NiceButton>().Any(s => s.ButtonParameter == buttonID))
            {
                NiceButton btn = FindControls<NiceButton>()
                    .First(s => s.ButtonParameter == buttonID);

                ushort g = (ushort)(_sortAsc ? 0x985 : 0x983);

                _sortOrderIndicator.Graphic = g;
                _sortOrderIndicator.X = btn.X + btn.Width - 15;
                _sortOrderIndicator.Y = btn.Y + 5;
            }

            _updateSkillsNeeded = true;
        }

        private void BuildGump()
        {
            _totalReal = 0;
            _totalValue = 0;
            _databox.Clear();

            foreach (SkillListEntry entry in _skillListEntries)
            {
                entry.Clear();
                entry.Dispose();
            }

            _skillListEntries.Clear();
            PropertyInfo pi = typeof(Skill).GetProperty(_sortField);

            if (SkillsGroupManager.IsActive)
            {
                SkillsGroupManager.Groups.Sort((s1, s2) =>
                {
                    var m1 = Regex.Match(s1.Name, "^\\d+");
                    var m2 = Regex.Match(s2.Name, "^\\d+");
                    if (!m1.Success || !m2.Success)
                    {
                        return s1.Name.CompareTo(s2.Name);
                    }

                    if (!int.TryParse(m1.Value, out var v1) || !int.TryParse(m2.Value, out var v2))
                    {
                        return s1.Name.CompareTo(s2.Name);
                    }
                    return v1.CompareTo(v2);

                });
                if (_sortAsc)
                {
                    SkillsGroupManager.Groups.Reverse();
                }

                foreach (SkillsGroup g in SkillsGroupManager.Groups)
                {
                    var skillEntries = new List<SkillListEntry>();
                    var a = new Area();
                    a.AcceptMouseInput = true;
                    a.WantUpdateSize = false;
                    a.CanMove = true;
                    a.Height = 26;
                    a.Width = Width - 26;
                    a.Tag = g.IsMaximized;
                    a.MouseUp += (sender, e) =>
                    {
                        g.IsMaximized = !g.IsMaximized;
                        var _a = (Area)sender;
                        var newState = !(bool)_a.Tag;
                        _a.Tag = newState;
                        foreach (var entry in skillEntries)
                        {
                            entry.IsVisible = newState;
                        }
                        _databox.WantUpdateSize = true;
                        _databox.ReArrangeChildren();
                    };


                    var skills = new List<Skill>();
                    for (int i = 0; i < g.Count; i++)
                    {
                        byte index = g.GetSkill(i);
                        if (index < SkillsLoader.Instance.SkillsCount)
                        {
                            skills.Add(World.Player.Skills[index]);
                        }
                    }

                    skills = skills.OrderBy(s => pi.GetValue(s, null)).ToList();
                    if (_sortAsc)
                    {
                        skills.Reverse();
                    }

                    var grpReal = skills.Sum(s => s.Base);
                    var grpVal = skills.Sum(s => s.Value);
                    _totalReal += grpReal;
                    _totalValue += grpVal;
                    ;

                    foreach (var s in skills)
                    {
                        skillEntries.Add(new SkillListEntry(s));
                    }
                    a.Add
                    (
                            new ResizePic(0x0BB8)
                            {
                                X = 1,
                                Y = 3,
                                Width = 180,
                                Height = 22
                            }
                    );
                    StbTextBox _textbox;
                    a.Add
                    (
                        _textbox = new StbTextBox
                        (
                            3,
                            -1,
                            200,
                            false,
                            FontStyle.Fixed
                        )
                        {
                            X = 5,
                            Y = 3,
                            Width = 180,
                            Height = 17,
                            IsEditable = false
                        }
                    );

                    _textbox.SetText(g.Name);
                    _textbox.IsEditable = false;

                    _textbox.MouseDown += (s, e) =>
                    {
                        if (!g.IsMaximized)
                        {
                            a.InvokeMouseUp(e.Location, e.Button);
                        }
                        UIManager.KeyboardFocusControl = _textbox;
                        _textbox.SetKeyboardFocus();
                        _textbox.IsEditable = true;
                        _textbox.AllowSelection = true;
                    };

                    _textbox.FocusLost += (s, e) =>
                    {
                        _textbox.IsEditable = false;
                        _textbox.AllowSelection = false;
                        UIManager.KeyboardFocusControl = null;
                        UIManager.SystemChat.SetFocus();
                    };
                    _textbox.TextChanged += (s, e) =>
                    {
                        g.Name = _textbox.Text;
                    };
                    a.Add(new Label(grpReal.ToString("F1"), true, 1153) { X = 205, Y = 3 });
                    a.Add(new Label(grpVal.ToString("F1"), true, 1153) { X = 255, Y = 3 });

                    _databox.Add(a);
                    foreach (var entry in skillEntries)
                    {
                        entry.IsVisible = g.IsMaximized;
                        _skillListEntries.Add(entry);
                        _databox.Add(entry);
                    }
                }
            }
            else
            {
                List<Skill> sortSkills = new List<Skill>(World.Player.Skills.OrderBy(x => pi.GetValue(x, null)));
                if (_sortAsc)
                {
                    sortSkills.Reverse();
                }
                foreach (Skill skill in sortSkills)
                {
                    _totalReal += skill.Base;
                    _totalValue += skill.Value;
                    _skillListEntries.Add(new SkillListEntry(skill));
                }
                foreach (var entry in _skillListEntries)
                {
                    _databox.Add(entry);
                }
            }



            _databox.WantUpdateSize = true;
            _databox.ReArrangeChildren();

            Add(real = new Label(_totalReal.ToString("F1"), true, 1153) { X = 205, Y = Height - 20 });
            Add(value = new Label(_totalValue.ToString("F1"), true, 1153) { X = 255, Y = Height - 20 });
        }


        private void ResizeDrag_MouseUp(object sender, Input.MouseEventArgs e)
        {
            Dragging = false;
        }

        private void ResizeDrag_MouseDown(object sender, Input.MouseEventArgs e)
        {
            dragStartH = Height;
            Dragging = true;
        }

        public override void Update()
        {
            base.Update();

            if (_updateSkillsNeeded)
            {
                foreach (Label label in Children.OfType<Label>())
                {
                    label.Dispose();
                }

                BuildGump();

                _updateSkillsNeeded = false;
            }

            int steps = Mouse.LDragOffset.Y;

            if (Dragging && steps != 0)
            {
                Height = dragStartH + steps;
                if (Height < 170)
                    Height = 170;
                ProfileManager.CurrentProfile.AdvancedSkillsGumpHeight = Height;

                area.Height = Height - 60;
                background.Height = Height - 1;
                _databox.WantUpdateSize = true;
                resizeDrag.Y = Height - 11;
                real.Y = Height - 20;
                value.Y = Height - 20;
                BottomArea.Y = area.Height + area.Y - 1;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

            batcher.DrawRectangle(
                SolidColorTextureCache.GetTexture(Color.Gray),
                x,
                y,
                Width,
                Height,
                hueVector
            );

            return base.Draw(batcher, x, y);
        }

        public void ForceUpdate()
        {
            _updateSkillsNeeded = true;
        }

        private enum Buttons
        {
            SortName = 1,
            SortReal = 2,
            SortBase = 3,
            SortCap = 4,
            SortLock = 5,
        }
    }


    internal class SkillListEntry : Control
    {
        private readonly Button _activeUse;
        private readonly Skill _skill;
        public SkillListEntry(Skill skill)
        {
            Height = 20;
            Label skillName = new Label(skill.Name, true, 1153, font: 3);
            Label skillValueBase = new Label(skill.Base.ToString(), true, 1153, font: 3);
            Label skillValue = new Label(skill.Value.ToString(), true, 1153, font: 3);
            Label skillCap = new Label(skill.Cap.ToString(), true, 1153, font: 3);

            _skill = skill;
            CanMove = !_skill.IsClickable;

            if (skill.IsClickable)
            {
                Add
                (
                    _activeUse = new Button((int)Buttons.ActiveSkillUse, 0x837, 0x838)
                    {
                        X = 0,
                        Y = 4,
                        ButtonAction = ButtonAction.Activate
                    }
                );
            }

            skillName.X = 20;
            Add(skillName);

            skillValueBase.X = 205;
            Add(skillValueBase);

            skillValue.X = 255;
            Add(skillValue);

            skillCap.X = 305;
            Add(skillCap);

            GumpPic loc = new GumpPic(355, 4, (ushort)(skill.Lock == Lock.Up ? 0x983 : skill.Lock == Lock.Down ? 0x985 : 0x82C), 0);

            Add(loc);

            loc.MouseUp += (sender, e) =>
            {
                switch (_skill.Lock)
                {
                    case Lock.Up:
                        _skill.Lock = Lock.Down;
                        GameActions.ChangeSkillLockStatus((ushort)_skill.Index, (byte)Lock.Down);
                        loc.Graphic = 0x985;

                        break;

                    case Lock.Down:
                        _skill.Lock = Lock.Locked;
                        GameActions.ChangeSkillLockStatus((ushort)_skill.Index, (byte)Lock.Locked);
                        loc.Graphic = 0x82C;

                        break;

                    case Lock.Locked:
                        _skill.Lock = Lock.Up;
                        GameActions.ChangeSkillLockStatus((ushort)_skill.Index, (byte)Lock.Up);
                        loc.Graphic = 0x983;

                        break;
                }
            };
        }

        protected override void OnDragBegin(int x, int y)
        {
            if (_skill.IsClickable && Mouse.LButtonPressed && !SkillGumpAdvanced.Dragging && !Keyboard.Ctrl)
            {
                GetSpellFloatingButton(_skill.Index)?.Dispose();

                ref readonly var gumpInfo = ref Client.Game.Gumps.GetGump(0x24B8);

                SkillButtonGump skillButtonGump = new SkillButtonGump(
                    _skill,
                    Mouse.LClickPosition.X + (gumpInfo.UV.Width >> 1),
                    Mouse.LClickPosition.Y + (gumpInfo.UV.Height >> 1)
                );

                UIManager.Add(skillButtonGump);
                UIManager.AttemptDragControl(skillButtonGump, true);
            }
            else
            {
                base.OnDragBegin(x, y);
            }
        }

        protected override void OnDragEnd(int x, int y)
        {
            if (!_skill.IsClickable)
                base.OnDragEnd(x, y);
        }

        protected override void OnMouseOver(int x, int y)
        {
            base.OnMouseOver(x, y);

            if (Mouse.LButtonPressed && Math.Abs(Mouse.LDragOffset.X) >= Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS || Math.Abs(Mouse.LDragOffset.Y) >= Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS)
            {
                InvokeDragBegin(Mouse.Position);
            }
        }

        private static SkillButtonGump GetSpellFloatingButton(int id)
        {
            for (LinkedListNode<Gump> i = UIManager.Gumps.Last; i != null; i = i.Previous)
            {
                if (i.Value is SkillButtonGump g && g.SkillID == id)
                {
                    return g;
                }
            }

            return null;
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((Buttons)buttonID)
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
