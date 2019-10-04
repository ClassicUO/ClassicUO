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
using System.IO;
using System.Linq;

using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

using SDL2;

namespace ClassicUO.Game.UI.Gumps
{
    internal class StandardSkillsGump : MinimizableGump
    {
        internal override GumpPic Iconized { get; } = new GumpPic(0, 0, 0x839, 0);
        internal override HitBox IconizerArea { get; } = new HitBox(160, 0, 23, 24);
        private readonly SkillControl[] _allSkillControls;
        private readonly GumpPic _bottomComment;
        private readonly GumpPic _bottomLine;

        private readonly List<MultiSelectionShrinkbox> _boxes = new List<MultiSelectionShrinkbox>();
        private readonly ScrollArea _container;
        private readonly SkillNameComparer _instance = new SkillNameComparer();
        private readonly Button _newGroupButton;
        private readonly ExpandableScroll _scrollArea;
        private readonly Label _skillsLabelSum;
        internal Checkbox _checkReal, _checkCaps;
        private const int _diffY = 22;

        public StandardSkillsGump() : base(Constants.SKILLSTD_LOCALSERIAL, 0)
        {
            CanBeSaved = true;
            AcceptMouseInput = false;
            CanMove = true;
            Height = 200 + _diffY;

            Add(new GumpPic(160, 0, 0x82D, 0));
            _scrollArea = new ExpandableScroll(0, _diffY, Height, 0x1F40)
            {
                TitleGumpID = 0x0834,
                AcceptMouseInput = true
            };

            Add(_scrollArea);

            Add(new GumpPic(50, 35 + _diffY, 0x082B, 0));
            Add(_bottomLine = new GumpPic(50, Height - 98, 0x082B, 0));
            Add(_bottomComment = new GumpPic(25, Height - 85, 0x0836, 0));

            _container = new ScrollArea(22, 45 + _diffY + _bottomLine.Height - 10, _scrollArea.Width - 14,
                                        _scrollArea.Height - (83 + _diffY), false) {AcceptMouseInput = true, CanMove = true};
            Add(_container);
            Add(_skillsLabelSum = new Label(World.Player.Skills.Sum(s => s.Value).ToString("F1"), false, 600, 0, 3) {X = _bottomComment.X + _bottomComment.Width + 5, Y = _bottomComment.Y - 5});

            //new group
            Add(_newGroupButton = new Button(0, 0x083A, 0x083A, 0x083A)
            {
                X = 60,
                Y = Height,
                ContainsByBounds = true,
                ButtonAction = ButtonAction.Activate
            });
            Add(_checkReal = new Checkbox(0x938, 0x939, " - Show Real", 1, 0x0386, false) {X = _newGroupButton.X + _newGroupButton.Width + 30, Y = _newGroupButton.Y - 6});
            Add(_checkCaps = new Checkbox(0x938, 0x939, " - Show Caps", 1, 0x0386, false) {X = _newGroupButton.X + _newGroupButton.Width + 30, Y = _newGroupButton.Y + 7});
            _checkReal.ValueChanged += UpdateGump;
            _checkCaps.ValueChanged += UpdateGump;

            _allSkillControls = new SkillControl[FileManager.Skills.SkillsCount];

            foreach (KeyValuePair<string, List<int>> k in SkillsGroupManager.Groups)
                AddSkillsToGroup(k.Key, k.Value.OrderBy(s => s, _instance).ToList());

        }


        public override void OnButtonClick(int buttonID)
        {
            if (buttonID == 0)
            {
                string group = "New Group";

                if (SkillsGroupManager.AddNewGroup(group))
                    AddSkillsToGroup(group, SkillsGroupManager.GetSkillsInGroup(group));
            }
        }

        private void AddSkillsToGroup(string group, List<int> skills)
        {
            MultiSelectionShrinkbox box = new MultiSelectionShrinkbox(0, 0, _container.Width - 30, group, 0, 6, false, true)
            {
                CanMove = true,
                IsEditable = true
            };

            box.EditStateStart += (ss, e) =>
            {
                Control p = _container;
                var items = p.FindControls<ScrollAreaItem>().SelectMany(s => s.Children.OfType<MultiSelectionShrinkbox>());

                foreach (var item in items)
                {
                    foreach (EditableLabel c in item.FindControls<EditableLabel>()) c.SetEditable(false);
                }
            };

            box.EditStateEnd += (ss, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.BackupText) && !string.IsNullOrWhiteSpace(e.Text)) SkillsGroupManager.ReplaceGroup(e.BackupText, e.Text);
            };

            _container.Add(box);

            SkillControl[] controls = new SkillControl[skills.Count];
            int idx = 0;

            foreach (var skill in skills)
            {
                var c = new SkillControl(skill, box.Width - 15, group, box)
                {
                    Width = box.Width - 15
                };
                controls[idx++] = c;
                _allSkillControls[skill] = c;
            }

            box.SetItemsValue(controls);

            _boxes.Add(box);
        }

        protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            if (key == SDL.SDL_Keycode.SDLK_DELETE)
            {
                for (int i = 0; i < _boxes.Count; i++)
                {
                    var box = _boxes[i];

                    if (box.IsEditing)
                    {
                        if (i == 0)
                        {
                            Engine.UI.Add(new MessageBoxGump(200, 150, "Cannot delete this group.", null));
                            break;
                        }

                        if (SkillsGroupManager.RemoveGroup(box.LabelText))
                        {
                            foreach (var child in box.FindControls<SkillControl>())
                            {
                                _boxes[0].AddItem(child);
                            }

                            _boxes[0].Items.Sort((a, b) =>
                            {
                                var s0 = (SkillControl)a;
                                var s1 = (SkillControl)b;

                                var skill0 = World.Player.Skills[s0.SkillIndex];
                                var skill1 = World.Player.Skills[s1.SkillIndex];

                                return skill0.Name.CompareTo(skill1.Name);
                            });

                            _boxes[0].GenerateButtons();

                            box.Children.Clear();
                            _container.Remove(box);
                            _boxes.RemoveAt(i);
                        }

                        break;
                    }
                }
            }
        }


        public override void Update(double totalMS, double frameMS)
        {
            WantUpdateSize = true;

            _bottomLine.Y = Height - 98;
            _bottomComment.Y = Height - 85;
            _container.Height = Height - (150 + _diffY);
            _newGroupButton.Y = Height - 52;
            _skillsLabelSum.Y = _bottomComment.Y + 2;
            _checkReal.Y = _newGroupButton.Y - 6;
            _checkCaps.Y = _newGroupButton.Y + 7;

            _container.ForceUpdate();

            base.Update(totalMS, frameMS);
        }

        public void ForceUpdate(int skillIndex)
        {
            if (skillIndex < _allSkillControls.Length)
                _allSkillControls[skillIndex]?.UpdateSkillValue(Engine.UI.GetGump<StandardSkillsGump>());
            _skillsLabelSum.Text = World.Player.Skills.Sum(s => _checkReal.IsChecked ? s.Base : s.Value).ToString("F1");
        }

        private void UpdateGump(object sender, EventArgs e)
        {
            StandardSkillsGump skg = Engine.UI.GetGump<StandardSkillsGump>();
            for (int i = 0; i < _allSkillControls.Length; i++) _allSkillControls[i]?.UpdateSkillValue(skg);
            _skillsLabelSum.Text = World.Player.Skills.Sum(s => _checkReal.IsChecked ? s.Base : s.Value).ToString("F1");
        }

        public override void Save(BinaryWriter writer)
        {
            base.Save(writer);
            writer.Write(_scrollArea.SpecialHeight);

            writer.Write(_boxes.Count);

            for (int i = 0; i < _boxes.Count; i++) writer.Write(_boxes[i].Opened);
        }

        public override void Restore(BinaryReader reader)
        {
            base.Restore(reader);
            _scrollArea.Height = _scrollArea.SpecialHeight = reader.ReadInt32();

            int count = reader.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                bool opened = reader.ReadBoolean();

                if (i < _boxes.Count)
                    _boxes[i].Opened = opened;
            }
        }

        private class SkillNameComparer : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                if (x >= FileManager.Skills.SkillNames.Length || y >= FileManager.Skills.SkillNames.Length)
                    return 0;

                return FileManager.Skills.SkillNames[x].CompareTo(FileManager.Skills.SkillNames[y]);
            }
        }

        private class SkillControl : Control
        {
            private readonly Label _labelValue;
            private readonly int _skillIndex;
            private readonly GumpPic _lock;
            private MultiSelectionShrinkbox _parent;

            public SkillControl(int skillIndexIndex, int maxWidth, string group, MultiSelectionShrinkbox parent)
            {
                AcceptMouseInput = true;
                CanMove = true;

                _parent = parent;

                Skill skill = World.Player.Skills[skillIndexIndex];
                _skillIndex = skillIndexIndex;

                if (skill.IsClickable)
                {
                    Button button = new Button(0, 0x0837, 0x0838, 0x0837);
                    button.MouseUp += (ss, e) => { GameActions.UseSkill(skillIndexIndex); };
                    Add(button);
                }

                Label label = new Label(skill.Name, false, 0x0288, maxWidth, 9)
                {
                    X = 12
                };
                Add(label);

                _labelValue = new Label(skill.Value.ToString("F1"), false, 0x0288, maxWidth - 10, 9, align: TEXT_ALIGN_TYPE.TS_RIGHT);
                Add(_labelValue);


                _lock = new GumpPic(maxWidth - 8, 1, GetLockValue(skill.Lock), 0) {AcceptMouseInput = true};

                _lock.MouseUp += (sender, e) =>
                {
                    byte slock = (byte) skill.Lock;

                    if (slock < 2)
                        slock++;
                    else
                        slock = 0;

                    skill.Lock = (Lock) slock;

                    GameActions.ChangeSkillLockStatus((ushort) skill.Index, slock);

                    ushort graphic = GetLockValue(skill.Lock);
                    _lock.Graphic = graphic;
                    _lock.Texture = FileManager.Gumps.GetTexture(graphic);
                };
                Add(_lock);

                WantUpdateSize = false;

                Width = maxWidth;
                Height = label.Height;
                Group = group;
            }

            public string Group { get; private set; }

            public int SkillIndex => _skillIndex;

            private static ushort GetLockValue(Lock lockStatus)
            {
                switch (lockStatus)
                {
                    case Lock.Up:

                        return 0x0984;

                    case Lock.Down:

                        return 0x0986;

                    case Lock.Locked:

                        return 0x082C;

                    default:

                        return Graphic.INVALID;
                }
            }

           
            protected override void OnMouseDown(int x, int y, MouseButton button)
            {
                CanMove = false;
            }

            protected override void OnMouseOver(int x, int y)
            {
                if (CanMove)
                    return;

                var c = Engine.UI.MouseOverControl;

                if (c != null && c != this)
                {
                    var p = c.Parent;

                    while (p != null)
                    {
                        if (p is MultiSelectionShrinkbox box)
                        {
                            if (box.LabelText != Group)
                            {
                                SkillsGroupManager.MoveSkillToGroup(Group, box.LabelText, _skillIndex);

                                int index = -1;

                                foreach (SkillControl skillControl in box.Items.OfType<SkillControl>())
                                {
                                    index++;

                                    if (skillControl._skillIndex > _skillIndex) break;
                                }

                                _parent.Remove(this);
                                box.AddItem(this, index);

                                _parent = box;
                                Group = box.LabelText;
                            }

                            break;
                        }

                        p = p.Parent;
                    }
                }

                if (!(c?.RootParent is StandardSkillsGump))
                {
                    uint serial = (uint) (World.Player + _skillIndex + 1);

                    if (Engine.UI.GetGump<SkillButtonGump>(serial) != null)
                        Engine.UI.Remove<SkillButtonGump>(serial);

                    SkillButtonGump skillButtonGump = new SkillButtonGump(World.Player.Skills[_skillIndex], Mouse.Position.X, Mouse.Position.Y);
                    Engine.UI.Add(skillButtonGump);
                    Rectangle rect = FileManager.Gumps.GetTexture(0x24B8).Bounds;
                    Engine.UI.AttemptDragControl(skillButtonGump, new Point(Mouse.Position.X + (rect.Width >> 1), Mouse.Position.Y + (rect.Height >> 1)), true);
                }

                base.OnMouseOver(x, y);
            }

            protected override void OnMouseUp(int x, int y, MouseButton button)
            {
                CanMove = true;
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                ResetHueVector();

                if (!CanMove) batcher.Draw2D(Textures.GetTexture(Color.Wheat), x, y, Width, Height, ref _hueVector);

                return base.Draw(batcher, x, y);
            }


            public void UpdateSkillValue(StandardSkillsGump skg)
            {
                Skill skill = World.Player.Skills[_skillIndex];

                if (skill != null)
                {
                    _labelValue.Text = (skg == null || skg._checkCaps.IsChecked ? skill.Cap : skg._checkReal.IsChecked ? skill.Base : skill.Value).ToString("F1");


                    ushort graphic = GetLockValue(skill.Lock);
                    _lock.Graphic = graphic;
                    _lock.Texture = FileManager.Gumps.GetTexture(graphic);
                }
            }
        }
    }
}