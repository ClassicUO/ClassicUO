#region license
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
using System.IO;
using System.Linq;
using System.Xml;

using ClassicUO.Configuration;
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
    internal class StandardSkillsGump : Gump
    {
        private readonly SkillControl[] _allSkillControls;
        private readonly GumpPic _bottomComment;
        private readonly GumpPic _bottomLine;

        private readonly ScrollArea _container;
        private readonly SkillNameComparer _instance = new SkillNameComparer();
        private readonly Button _newGroupButton;
        private readonly ExpandableScroll _scrollArea;
        private readonly Label _skillsLabelSum;
        internal Checkbox _checkReal, _checkCaps;
        private const int _diffY = 22;
        private GumpPic _gumpPic;
        private HitBox _hitBox;
        private bool _isMinimized;

        private readonly List<SkillsGroupControl> _skillsControl = new List<SkillsGroupControl>();

        public StandardSkillsGump() : base(Constants.SKILLSTD_LOCALSERIAL, 0)
        {
            AcceptMouseInput = false;
            CanMove = true;
            CanCloseWithRightClick = true;

            Height = 200 + _diffY;

            Add(_gumpPic = new GumpPic(160, 0, 0x82D, 0));
            _gumpPic.MouseDoubleClick += _picBase_MouseDoubleClick;
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

            _allSkillControls = new SkillControl[UOFileManager.Skills.SkillsCount];


            if (World.Player != null)
            {
                ushort currentIndex = 0;
                int currentY = 0;

                foreach (var g in SkillsGroupManager2.Groups)
                {
                    SkillsGroupControl control = new SkillsGroupControl(g, 0, 0);
                    control.IsMinimized = true;
                    _skillsControl.Add(control);
                    var child = new ScrollAreaItem();
                    child.Add(control);
                    child.Y = currentY;
                    child.WantUpdateSize = true;
                    _container.Add(child);

                    int count = g.Count;

                    for (int i = 0; i < count; i++)
                    {
                        byte index = g.GetSkill(i);

                        if (index < UOFileManager.Skills.SkillsCount)
                        {
                            control.AddSkill(index, 0, i * 17);
                        }
                    }

                    currentY += 19;

                    if (!control.IsMinimized)
                    {
                        currentY += count * 17;
                    }

                    currentIndex++;
                }
            }

            _hitBox = new HitBox(160, 0, 23, 24);
            Add(_hitBox);
            _hitBox.MouseUp += _hitBox_MouseUp;
        }

        public override GUMP_TYPE GumpType => GUMP_TYPE.GT_SKILLMENU;

        public bool IsMinimized
        {
            get => _isMinimized;
            set
            {
                if (_isMinimized != value)
                {
                    _isMinimized = value;

                    _gumpPic.Graphic = value ? (ushort)0x839 : (ushort) 0x82D;

                    if (value)
                    {
                        _gumpPic.X = 0;
                    }
                    else
                    {
                        _gumpPic.X = 160;
                    }

                    foreach (var c in Children)
                    {
                        c.IsVisible = !value;
                    }

                    _gumpPic.IsVisible = true;
                    WantUpdateSize = true;
                }
            }
        }


        private void _picBase_MouseDoubleClick(object sender, MouseDoubleClickEventArgs e)
        {
            if (e.Button == MouseButtonType.Left && IsMinimized)
            {
                IsMinimized = false;
            }
        }

        private void _hitBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtonType.Left && !IsMinimized)
            {
                IsMinimized = true;
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            if (buttonID == 0)
            {
                SkillsGroup g = new SkillsGroup();
                g.Name = "New Group";

                SkillsGroupManager2.Add(g);

                var control = new SkillsGroupControl(g, 0, 0);
                _skillsControl.Add(control);
                control.IsMinimized = !g.IsMaximized;
                _container.Add(control);
                UpdateElementsPosition();
            }
            else if (buttonID == 1000)
            {
                UpdateElementsPosition();
            }
        }

        private void UpdateElementsPosition()
        {
            ushort index = 0;
            int currentY = 0;

            foreach (var c in _skillsControl)
            {
                c.Parent.Y = currentY;
                c.Parent.WantUpdateSize = true;
                currentY += 19;

                if (!c.IsMinimized)
                {
                    currentY += c.Count * 17;
                }

                index++;
            }
        }

        protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            //if (key == SDL.SDL_Keycode.SDLK_DELETE)
            //{
            //    for (int i = 0; i < _boxes.Count; i++)
            //    {
            //        var box = _boxes[i];

            //        if (box.IsEditing)
            //        {
            //            if (i == 0)
            //            {
            //                UIManager.Add(new MessageBoxGump(200, 150, "Cannot delete this group.", null));
            //                break;
            //            }

            //            if (SkillsGroupManager.RemoveGroup(box.LabelText))
            //            {
            //                foreach (var child in box.FindControls<SkillControl>())
            //                {
            //                    _boxes[0].AddItem(child);
            //                }

            //                _boxes[0].Items.Sort((a, b) =>
            //                {
            //                    var s0 = (SkillControl)a;
            //                    var s1 = (SkillControl)b;

            //                    var skill0 = World.Player.Skills[s0.SkillIndex];
            //                    var skill1 = World.Player.Skills[s1.SkillIndex];

            //                    return skill0.Name.CompareTo(skill1.Name);
            //                });

            //                _boxes[0].GenerateButtons();

            //                box.Children.Clear();
            //                _container.Remove(box);
            //                _boxes.RemoveAt(i);
            //            }

            //            break;
            //        }
            //    }
            //}
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

            base.Update(totalMS, frameMS);
        }


        public void ForceUpdate(int skillIndex)
        {
            if (skillIndex < _allSkillControls.Length)
                _allSkillControls[skillIndex]?.UpdateSkillValue(this);
            _skillsLabelSum.Text = World.Player.Skills.Sum(s => _checkReal.IsChecked ? s.Base : s.Value).ToString("F1");
        }

        private void UpdateGump(object sender, EventArgs e)
        {
            for (int i = 0; i < _allSkillControls.Length; i++) 
                _allSkillControls[i]?.UpdateSkillValue(this);
            _skillsLabelSum.Text = World.Player.Skills.Sum(s => _checkReal.IsChecked ? s.Base : s.Value).ToString("F1");
        }

        public override void Save(BinaryWriter writer)
        {
            //base.Save(writer);
            //writer.Write(_scrollArea.SpecialHeight);

            //writer.Write(_boxes.Count);

            //for (int i = 0; i < _boxes.Count; i++) 
            //    writer.Write(_boxes[i].Opened);
            //writer.Write(IsMinimized);
        }

        public override void Restore(BinaryReader reader)
        {
            //base.Restore(reader);

            //if (Configuration.Profile.GumpsVersion == 2)
            //{
            //    reader.ReadUInt32();
            //    _isMinimized = reader.ReadBoolean();
            //}

            //_scrollArea.Height = _scrollArea.SpecialHeight = reader.ReadInt32();

            //int count = reader.ReadInt32();

            //for (int i = 0; i < count; i++)
            //{
            //    bool opened = reader.ReadBoolean();

            //    if (i < _boxes.Count)
            //        _boxes[i].Opened = opened;
            //}

            //if (Profile.GumpsVersion >= 3)
            //{
            //    _isMinimized = reader.ReadBoolean();
            //}
        }

        public override void Save(XmlTextWriter writer)
        {
            //base.Save(writer);
            //writer.WriteAttributeString("isminimized", IsMinimized.ToString());
            //writer.WriteAttributeString("height", _scrollArea.SpecialHeight.ToString());

            //writer.WriteStartElement("groups");

            //for (int i = 0; i < _boxes.Count; i++)
            //{
            //    writer.WriteStartElement("group");
            //    writer.WriteAttributeString("isopen", _boxes[i].Opened.ToString());
            //    writer.WriteEndElement();
            //}

            //writer.WriteEndElement();
        }

        public override void Restore(XmlElement xml)
        {
            //base.Restore(xml);
            //_scrollArea.Height = _scrollArea.SpecialHeight = int.Parse(xml.GetAttribute("height"));

            //XmlElement groupsXml = xml["groups"];

            //if (groupsXml != null)
            //{
            //    int index = 0;
            //    foreach (XmlElement groupXml in groupsXml.GetElementsByTagName("group"))
            //    {
            //        if (index >= 0 && index < _boxes.Count)
            //            _boxes[index++].Opened = bool.Parse(groupXml.GetAttribute("isopen"));
            //    }
            //}
        }

        public override void Dispose()
        {
            base.Dispose();

            foreach (var c in _skillsControl)
            {
                c.Dispose();
            }

            _skillsControl.Clear();
        }

        private class SkillNameComparer : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                if (x >= UOFileManager.Skills.Skills.Count || y >= UOFileManager.Skills.Skills.Count)
                    return 0;

                return UOFileManager.Skills.Skills[x].Name.CompareTo(UOFileManager.Skills.Skills[y].Name);
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
                    button.MouseUp += (ss, e) => { if (IsVisible) GameActions.UseSkill(skillIndexIndex); };
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
                    if (IsVisible && e.Button == MouseButtonType.Left)
                    {
                        byte slock = (byte)skill.Lock;

                        if (slock < 2)
                            slock++;
                        else
                            slock = 0;

                        skill.Lock = (Lock)slock;

                        GameActions.ChangeSkillLockStatus((ushort)skill.Index, slock);

                        ushort graphic = GetLockValue(skill.Lock);
                        _lock.Graphic = graphic;
                        _lock.Texture = UOFileManager.Gumps.GetTexture(graphic);
                    }
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

                        return 0xFFFF;
                }
            }

           
            protected override void OnMouseDown(int x, int y, MouseButtonType button)
            {
                if (button == MouseButtonType.Left)
                    CanMove = false;
            }

            protected override void OnMouseOver(int x, int y)
            {
                //if (CanMove)
                //    return;

                //var c = UIManager.MouseOverControl;

                //if (c != null && c != this)
                //{
                //    var p = c.Parent;

                //    while (p != null)
                //    {
                //        if (p is MultiSelectionShrinkbox box)
                //        {
                //            if (box.LabelText != Group)
                //            {
                //                SkillsGroupManager.MoveSkillToGroup(Group, box.LabelText, _skillIndex);

                //                int index = -1;

                //                foreach (SkillControl skillControl in box.Items.OfType<SkillControl>())
                //                {
                //                    index++;

                //                    if (skillControl._skillIndex > _skillIndex) break;
                //                }

                //                _parent.Remove(this);
                //                box.AddItem(this, index);

                //                _parent = box;
                //                Group = box.LabelText;
                //            }

                //            break;
                //        }

                //        p = p.Parent;
                //    }
                //}

                //if (!(c?.RootParent is StandardSkillsGump))
                //{
                //    uint serial = (uint) (World.Player + _skillIndex + 1);

                //    UIManager.GetGump<SkillButtonGump>(serial)?.Dispose();

                //    SkillButtonGump skillButtonGump = new SkillButtonGump(World.Player.Skills[_skillIndex], Mouse.Position.X, Mouse.Position.Y);
                //    UIManager.Add(skillButtonGump);
                //    Rectangle rect = UOFileManager.Gumps.GetTexture(0x24B8).Bounds;
                //    UIManager.AttemptDragControl(skillButtonGump, new Point(Mouse.Position.X + (rect.Width >> 1), Mouse.Position.Y + (rect.Height >> 1)), true);
                //}

                //base.OnMouseOver(x, y);
            }

            protected override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                if (button == MouseButtonType.Left)
                    CanMove = true;
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                ResetHueVector();

                if (!CanMove) batcher.Draw2D(Texture2DCache.GetTexture(Color.Wheat), x, y, Width, Height, ref _hueVector);

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
                    _lock.Texture = UOFileManager.Gumps.GetTexture(graphic);
                }
            }
        }






        private class SkillsGroupControl : Control
        {
            private bool _isMinimized;
            private readonly Button _button;
            private readonly TextBox _textbox;
            private readonly GumpPic _gumpPic;

            private readonly List<SkillItemControl> _skills = new List<SkillItemControl>();

            public SkillsGroupControl(SkillsGroup group, int x, int y)
            {
                X = x;
                Y = y;

               
                _button = new Button(1000, 0x0827, 0x0827, 0x0827)
                {
                    ButtonAction = ButtonAction.Activate,
                    ContainsByBounds = true
                };
                Add(_button);

                Add(_textbox = new TextBox(6, 32, 0, 0, false)
                {
                    Text = group.Name,
                });

                _gumpPic = new GumpPic(0, 0, 0x0835, 0);
                Add(_gumpPic);

                IsMinimized = !group.IsMaximized;
                CanMove = true;
                AcceptMouseInput = true;
                WantUpdateSize = true;
            }

            public bool IsMinimized
            {
                get => _isMinimized;
                set
                {
                    ushort graphic = (ushort) (value ? 0x0827 : 0x826);

                    _button.ButtonGraphicNormal = graphic;
                    _button.ButtonGraphicOver = graphic;
                    _button.ButtonGraphicPressed = graphic;

                    foreach (SkillItemControl c in _skills)
                    {
                        c.IsVisible = !value;
                    }

                    _isMinimized = value;
                    WantUpdateSize = true;
                }
            }

            public int Count => _skills.Count;

            public void AddSkill(int index, int x, int y)
            {
                _skills.Add(new SkillItemControl(index, x + 19, y));
            }

            public void UpdateDataPosition()
            {
                int y = 0;

                foreach (var c in _skills)
                {
                    c.Y = y;
                    y += 17;
                }
            }

            public override void OnButtonClick(int buttonID)
            {
                if (buttonID == 1000)
                {
                    IsMinimized = !IsMinimized;
                    base.OnButtonClick(buttonID);
                }
            }


            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                ResetHueVector();

                base.Draw(batcher, x, y);

                bool drawOrnament = true;

                //if (UIManager.KeyboardFocusControl == _textbox)
                //{
                //    drawOrnament = false;
                //    batcher.Draw2D(Texture2DCache.GetTexture(Color.Gray),)
                //}



                if (drawOrnament)
                {
                    //int xx = 11 + _textbox.Texture.Width;
                    //int width = 215 - xx;

                    //if (xx > 0)
                    //{
                    //    _gumpPic.Width = width;
                    //    _gumpPic.Draw(batcher, _gumpPic.X + xx, _gumpPic.Y + 5);
                    //}
                }

                if (!IsMinimized && _skills.Count != 0)
                {
                    foreach (var c in _skills)
                    {
                        if (c.IsVisible)
                            c.Draw(batcher, c.X + x, c.Y + y);
                    }
                }


                return true;
            }

            public override void Dispose()
            {
                base.Dispose();

                foreach (SkillItemControl c in _skills)
                {
                    c.Dispose();
                }

                _skills.Clear();
            }
        }

        private class SkillItemControl : Control
        {
            private Lock _status;
            private readonly Button _buttonUse, _buttonStatus;
            private readonly Label _name, _value;

            public SkillItemControl(int index, int x, int y)
            {
                Index = index;
                X = x;
                Y = y;

                if (index < 0 || index >= UOFileManager.Skills.Skills.Count)
                {
                    Dispose();
                    return;
                }

                var skill = World.Player.Skills[Index];

                if (skill != null)
                {
                    if (skill.IsClickable)
                    {
                        _buttonUse = new Button(0,
                                             0x0837,
                                             0x0838,
                                             0x0838)
                        {
                            ButtonAction = ButtonAction.Activate,
                            X = 8
                        };

                        Add(_buttonUse);
                    }

                    _status = skill.Lock;

                    ushort graphic = GetStatusButtonGraphic();
                    _buttonStatus = new Button(0,
                                               graphic,
                                               graphic,
                                               graphic)
                    {
                        ButtonAction = ButtonAction.Activate,
                        X = 251,
                        ContainsByBounds = true
                    };
                    Add(_buttonStatus);

                    Add(_name = new Label(skill.Name, false, 0x0288, font: 9));
                    _name.X = 22;

                    Add(_value = new Label("", false, 0x0288, font: 9));

                    UpdateValueText(false, false);
                }
                else 
                    Dispose();


                Width = 255;
                Height = 17;
                WantUpdateSize = false;
                AcceptMouseInput = true;
                CanMove = true;
            }

            public readonly int Index;


            public override void OnButtonClick(int buttonID)
            {
                if (buttonID == 0)
                {

                }
            }

            public void SetStatus(Lock status)
            {
                _status = status;
                ushort graphic = GetStatusButtonGraphic();

                _buttonStatus.ButtonGraphicNormal = graphic;
                _buttonStatus.ButtonGraphicOver = graphic;
                _buttonStatus.ButtonGraphicPressed = graphic;
            }

            private void UpdateValueText(bool showReal, bool showCap)
            {
                var skill = World.Player.Skills[Index];

                if (skill != null)
                {
                    double val = skill.Base;

                    if (showReal)
                    {
                        val = skill.Value;
                    }
                    else if (showCap)
                    {
                        val = skill.Cap;
                    }

                    _value.Text = $"{val:F1}";
                    _value.X = 250 - _value.X;
                }
            }

            private ushort GetStatusButtonGraphic()
            {
                switch (_status)
                {
                    default:
                    case Lock.Up: 
                        return 0x0984;
                    case Lock.Down:
                        return 0x0986;
                    case Lock.Locked: 
                        return 0x082C;
                }
            }
        }
    }
}