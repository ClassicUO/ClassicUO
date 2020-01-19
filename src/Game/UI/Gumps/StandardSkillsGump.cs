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
using ClassicUO.Network;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

using SDL2;

namespace ClassicUO.Game.UI.Gumps
{
    internal class StandardSkillsGump : Gump
    {
        private readonly GumpPic _bottomComment;
        private readonly GumpPic _bottomLine;

        private readonly ScrollArea _container;
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
            _checkReal.ValueChanged += UpdateSkillsValues;
            _checkCaps.ValueChanged += UpdateSkillsValues;



            if (World.Player != null)
            {
                foreach (var g in SkillsGroupManager2.Groups)
                {
                    SkillsGroupControl control = new SkillsGroupControl(g, 3, 3)
                    {
                        IsMinimized = true, 
                    };

                    _skillsControl.Add(control);
                    _container.Add(control);

                    int count = g.Count;

                    for (int i = 0; i < count; i++)
                    {
                        byte index = g.GetSkill(i);

                        if (index < UOFileManager.Skills.SkillsCount)
                        {
                            control.AddSkill(index, 0, 17 + i * 17);
                        }
                    }
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
                SkillsGroup g = new SkillsGroup
                {
                    Name = "New Group"
                };

                SkillsGroupManager2.Add(g);

                var control = new SkillsGroupControl(g, 3, 3);
                _skillsControl.Add(control);
                control.IsMinimized = !g.IsMaximized;
                _container.Add(control);
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


        public void Update(int skillIndex)
        {
            foreach (var c in _skillsControl)
            {
                if (c.UpdateSkillValue(skillIndex, _checkReal.IsChecked, _checkCaps.IsChecked))
                {
                    break;
                }
            }
         
            SumTotalSkills();
        }

        private void UpdateSkillsValues(object sender, EventArgs e)
        {
            Checkbox checkbox = (Checkbox) sender;

            if (_checkReal.IsChecked && _checkCaps.IsChecked)
            {
                if (checkbox == _checkReal)
                    _checkCaps.IsChecked = false;
                else
                    _checkReal.IsChecked = false;
            }

            foreach (var c in _skillsControl)
            {
                c.UpdateAllSkillsValues(_checkReal.IsChecked, _checkCaps.IsChecked);
            }

            SumTotalSkills();
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

        private void SumTotalSkills()
        {
            _skillsLabelSum.Text = World.Player.Skills.Sum(s => _checkReal.IsChecked ? s.Base : s.Value).ToString("F1");
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
            base.Save(writer);
            writer.WriteAttributeString("isminimized", IsMinimized.ToString());
            writer.WriteAttributeString("height", _scrollArea.SpecialHeight.ToString());

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
            base.Restore(xml);
            _scrollArea.Height = _scrollArea.SpecialHeight = int.Parse(xml.GetAttribute("height"));

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


        private class SkillsGroupControl : Control
        {
            private bool _isMinimized;
            private readonly Button _button;
            private readonly TextBox _textbox;
            private readonly GumpPicTiled _gumpPic;
            private readonly DataBox _box;
            private readonly SkillsGroup _group;

            private readonly List<SkillItemControl> _skills = new List<SkillItemControl>();

            public SkillsGroupControl(SkillsGroup group, int x, int y)
            {
                CanMove = false;
                AcceptMouseInput = true;
                WantUpdateSize = true;

                X = x;
                Y = y;

                _group = group;

                _button = new Button(1000, 0x0827, 0x0827, 0x0827)
                {
                    ButtonAction = ButtonAction.Activate,
                    ContainsByBounds = true
                };
                Add(_button);

                int width = UOFileManager.Fonts.GetWidthASCII(6, group.Name);

                Add(_textbox = new TextBox(6, 32, 0, width, false)
                {
                    X = 16,
                    Y = -5,
                    Text = group.Name,
                    Width = width,
                    Height = 17
                });

                int xx = width + 11 + 16;

                _gumpPic = new GumpPicTiled(0x0835)
                {
                    X = xx,
                    Y = 5,
                    Width = 215 - xx,
                };
                Add(_gumpPic);

                Add(_box = new DataBox(0, 0, 0, 0));

                IsMinimized = !group.IsMaximized;
            }


            public int Count => _skills.Count;

            public bool IsMinimized
            {
                get => _isMinimized;
                set
                {
                    ushort graphic = (ushort) (value ? 0x0827 : 0x826);

                    _button.ButtonGraphicNormal = graphic;
                    _button.ButtonGraphicOver = graphic;
                    _button.ButtonGraphicPressed = graphic;


                    _box.IsVisible = !value;

                    _isMinimized = value;
                    WantUpdateSize = true;
                }
            }






            public void AddSkill(int index, int x, int y)
            {
                var c = new SkillItemControl(index, x, y);
                _skills.Add(c);
                _box.Add(c);
                _box.WantUpdateSize = true;
                WantUpdateSize = true;
            }

            public void UpdateAllSkillsValues(bool showReal, bool showCaps)
            {
                foreach (SkillItemControl skill in _skills)
                {
                    skill.UpdateValueText(showReal, showCaps);
                }
            }

            public bool UpdateSkillValue(int index, bool showReal, bool showCaps)
            {
                foreach (var c in _skills)
                {
                    if (c.Index == index && index >= 0 && index < World.Player.Skills.Length)
                    {
                        Skill skill = World.Player.Skills[index];
                        if (skill == null)
                            return true;

                        c.UpdateValueText(showReal, showCaps);
                        c.SetStatus(skill.Lock);
                        return true;
                    }
                }

                return false;
            }

            protected override void OnMouseOver(int x, int y)
            {
                if (UIManager.LastControlMouseDown(MouseButtonType.Left) is SkillItemControl skillControl)
                {
                    if (skillControl
                       .Parent // databox
                       .Parent // skillgruop
                        != this)
                    {
                        SkillsGroupControl originalGroup = (SkillsGroupControl) skillControl.Parent.Parent;

                        if (originalGroup != null)
                        {
                            // remove from original control the skillcontrol
                            if (originalGroup._skills.Remove(skillControl))
                            {
                                int index = skillControl.Index;

                                // insert skillcontrol at the right index
                                int currIndex = _skills.Count - 1;

                                while (currIndex >= 0)
                                {
                                    if (_skills[currIndex].Index < index)
                                        break;
                                    currIndex--;
                                }

                                if (currIndex < 0)
                                    currIndex = 0;

                                originalGroup._box.Remove(skillControl);

                                _skills.Insert(currIndex, skillControl);
                                _box.Children.Insert(currIndex, skillControl);
                                skillControl.Parent = _box;

                                // update gump positions
                                UpdateSkillsPosition();
                                originalGroup.UpdateSkillsPosition();

                                // update gruop manager
                                originalGroup._group.Remove((byte) index);
                                _group.Add((byte) index);
                                _group.Sort();
                            }
                        }

                    }
                }

                base.OnMouseOver(x, y);
            }


            public override void OnButtonClick(int buttonID)
            {
                if (buttonID == 1000)
                {
                    IsMinimized = !IsMinimized;
                }
            }

            private void UpdateSkillsPosition()
            {
                int currY = 17;
                foreach (var c in _skills)
                {
                    c.Y = currY;
                    currY += 17;
                }

                _box.WantUpdateSize = true;
                WantUpdateSize = true;
            }
        }




        private class SkillItemControl : Control
        {
            private Lock _status;
            private readonly Button _buttonStatus;
            private readonly Label _value;

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
                        Button buttonUse = new Button(0,
                                                       0x0837,
                                                       0x0838,
                                                       0x0838)
                        {
                            ButtonAction = ButtonAction.Activate,
                            X = 8
                        };

                        Add(buttonUse);
                    }

                    _status = skill.Lock;

                    ushort graphic = GetStatusButtonGraphic();
                    _buttonStatus = new Button(1,
                                               graphic,
                                               graphic,
                                               graphic)
                    {
                        ButtonAction = ButtonAction.Activate,
                        X = 251,
                        ContainsByBounds = true
                    };
                    Add(_buttonStatus);

                    Label name;
                    Add(name = new Label(skill.Name, false, 0x0288, font: 9));
                    name.X = 22;

                    Add(_value = new Label("", false, 0x0288, font: 9));

                    UpdateValueText(false, false);
                }
                else 
                    Dispose();


                Width = 255;
                Height = 17;
                WantUpdateSize = true;
                AcceptMouseInput = true;
                CanMove = false;
            }

            public readonly int Index;


            public override void OnButtonClick(int buttonID)
            {
                if (buttonID == 0) // use
                {
                    GameActions.UseSkill(Index);
                }
                else if (buttonID == 1) // change status
                {
                    if (World.Player == null)
                        return;

                    Skill skill = World.Player.Skills[Index];
                    byte newStatus = (byte) skill.Lock;

                    if (newStatus < 2)
                        newStatus++;
                    else
                        newStatus = 0;

                    NetClient.Socket.Send(new PSkillsStatusChangeRequest((ushort) Index, newStatus));

                    skill.Lock = (Lock) newStatus;
                    SetStatus((Lock) newStatus);
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

            public void UpdateValueText(bool showReal, bool showCap)
            {
                if (World.Player == null || Index < 0 || Index >= World.Player.Skills.Length)
                    return;

                var skill = World.Player.Skills[Index];

                if (skill != null)
                {
                    double val = skill.Value;

                    if (showReal)
                    {
                        val = skill.Base;
                    }
                    else if (showCap)
                    {
                        val = skill.Cap;
                    }

                    _value.Text = $"{val:F1}";
                    _value.X = 250 - _value.Width;
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


            protected override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                if (button != MouseButtonType.Left)
                    return;

                UIManager.GameCursor.IsDraggingCursorForced = false;

                if (UIManager.IsMouseOverWorld && UIManager.LastControlMouseDown(MouseButtonType.Left) == this)
                {
                    UIManager.GetGump<SkillButtonGump>((uint) Index)?.Dispose();

                    if (Index >= 0 && Index < World.Player.Skills.Length)
                        UIManager.Add(new SkillButtonGump(World.Player.Skills[Index], Mouse.Position.X - 44, Mouse.Position.Y - 22));
                }
            }

            protected override void OnMouseDown(int x, int y, MouseButtonType button)
            {
                if (button == MouseButtonType.Left)
                {
                    UIManager.GameCursor.IsDraggingCursorForced = true;
                }
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                ResetHueVector();

                if (UIManager.LastControlMouseDown(MouseButtonType.Left) == this)
                {
                    batcher.Draw2D(Texture2DCache.GetTexture(Color.Wheat), x, y, Width, Height, ref _hueVector);
                }

                return base.Draw(batcher, x, y);
            }
        }
    }
}