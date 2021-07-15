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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using Microsoft.Xna.Framework;
using SDL2;

namespace ClassicUO.Game.UI.Gumps
{
    internal class StandardSkillsGump : Gump
    {
        private const int _diffY = 22;

        private readonly ScrollArea _area;
        private readonly GumpPic _bottomComment;
        private readonly GumpPic _bottomLine;
        private readonly DataBox _container;
        private readonly GumpPic _gumpPic;
        private readonly HitBox _hitBox;
        private bool _isMinimized;
        private readonly Button _newGroupButton;
        private readonly ExpandableScroll _scrollArea;

        private readonly List<SkillsGroupControl> _skillsControl = new List<SkillsGroupControl>();
        private readonly Label _skillsLabelSum;
        private readonly NiceButton _resetGroups;

        public StandardSkillsGump() : base(0, 0)
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

            _area = new ScrollArea
            (
                22,
                45 + _diffY + _bottomLine.Height - 10,
                _scrollArea.Width - 14,
                _scrollArea.Height - (83 + _diffY),
                false
            ) { AcceptMouseInput = true, CanMove = true };

            Add(_area);

            _container = new DataBox(0, 0, 1, 1);
            _container.WantUpdateSize = true;
            _container.AcceptMouseInput = true;
            _container.CanMove = true;

            _area.Add(_container);

            Add
            (
                _skillsLabelSum = new Label
                (
                    World.Player.Skills.Sum(s => s.Value).ToString("F1"),
                    false,
                    600,
                    0,
                    3
                ) { X = _bottomComment.X + _bottomComment.Width + 5, Y = _bottomComment.Y - 5 }
            );

            //new group
            int x = 60;

            Add
            (
                _newGroupButton = new Button(0, 0x083A, 0x083A, 0x083A)
                {
                    X = x,
                    Y = Height,
                    ContainsByBounds = true,
                    ButtonAction = ButtonAction.Activate
                }
            );

            Add
            (
                _checkReal = new Checkbox
                (
                    0x938,
                    0x939,
                    ResGumps.ShowReal,
                    1,
                    0x0386,
                    false
                ) { X = _newGroupButton.X + _newGroupButton.Width + 30, Y = _newGroupButton.Y - 6 }
            );

            Add
            (
                _checkCaps = new Checkbox
                (
                    0x938,
                    0x939,
                    ResGumps.ShowCaps,
                    1,
                    0x0386,
                    false
                ) { X = _newGroupButton.X + _newGroupButton.Width + 30, Y = _newGroupButton.Y + 7 }
            );

            _checkReal.ValueChanged += UpdateSkillsValues;
            _checkCaps.ValueChanged += UpdateSkillsValues;


            LoadSkills();

            Add(_resetGroups = new NiceButton(_scrollArea.X + 25, _scrollArea.Y + 7, 100, 18,
                                              ButtonAction.Activate, ResGumps.ResetGroups,
                                              unicode: false,
                                              font: 6)
            {
                ButtonParameter = 1,
                IsSelectable = false,
                //Alpha = 1f
            });

            _hitBox = new HitBox(160, 0, 23, 24);
            Add(_hitBox);
            _hitBox.MouseUp += _hitBox_MouseUp;

            _container.ReArrangeChildren();
        }

        public override GumpType GumpType => GumpType.SkillMenu;

        public bool IsMinimized
        {
            get => _isMinimized;
            set
            {
                if (_isMinimized != value)
                {
                    _isMinimized = value;

                    _gumpPic.Graphic = value ? (ushort) 0x839 : (ushort) 0x82D;

                    if (value)
                    {
                        _gumpPic.X = 0;
                    }
                    else
                    {
                        _gumpPic.X = 160;
                    }

                    foreach (Control c in Children)
                    {
                        c.IsVisible = !value;
                    }

                    _gumpPic.IsVisible = true;
                    WantUpdateSize = true;

                    _container.WantUpdateSize = true;
                    _container.ReArrangeChildren();
                }
            }
        }

        internal Checkbox _checkReal, _checkCaps;


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
                    Name = ResGumps.NewGroup
                };

                SkillsGroupManager.Add(g);

                SkillsGroupControl control = new SkillsGroupControl(g, 3, 3);
                _skillsControl.Add(control);
                _container.Add(control);
                control.IsMinimized = !g.IsMaximized;

                _container.WantUpdateSize = true;
                _container.ReArrangeChildren();
            }
            else if (buttonID == 1) // reset
            {
                UIManager.Add(
                    new MessageBoxGump(300, 200,
                                       "Skills will be placed in default groups.\nDo you want reset all groups?",
                                               b =>
                                               {
                                                   if (b)
                                                   {
                                                       _skillsControl.Clear();
                                                       _container.Clear();

                                                       SkillsGroupManager.Groups.Clear();
                                                       SkillsGroupManager.MakeDefault();

                                                       LoadSkills();

                                                       _container.WantUpdateSize = true;
                                                       _container.ReArrangeChildren();
                                                   }
                                               }, false, MessageButtonType.OK_CANCEL));
            }
        }

        private void LoadSkills()
        {
            if (World.Player != null)
            {
                foreach (SkillsGroup g in SkillsGroupManager.Groups)
                {
                    SkillsGroupControl control = new SkillsGroupControl(g, 3, 3);
                    _skillsControl.Add(control);
                    _container.Add(control);

                    control.IsMinimized = true;

                    int count = g.Count;

                    for (int i = 0; i < count; i++)
                    {
                        byte index = g.GetSkill(i);

                        if (index < SkillsLoader.Instance.SkillsCount)
                        {
                            control.AddSkill(index, 0, 17 + i * 17);
                        }
                    }
                }
            }
        }

        public override void Update(double totalTime, double frameTime)
        {
            WantUpdateSize = true;

            bool wantUpdate = _container.WantUpdateSize;

            _bottomLine.Y = Height - 98;
            _bottomComment.Y = Height - 85;
            _area.Height = _container.Height = Height - (150 + _diffY);
            _newGroupButton.Y = Height - 52;
            _skillsLabelSum.Y = _bottomComment.Y + 2;
            _checkReal.Y = _newGroupButton.Y - 6;
            _checkCaps.Y = _newGroupButton.Y + 7;


            base.Update(totalTime, frameTime);

            if (wantUpdate)
            {
                _container.ReArrangeChildren();
            }
        }


        public void Update(int skillIndex)
        {
            foreach (SkillsGroupControl c in _skillsControl)
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
                {
                    _checkCaps.IsChecked = false;
                }
                else
                {
                    _checkReal.IsChecked = false;
                }
            }

            foreach (SkillsGroupControl c in _skillsControl)
            {
                c.UpdateAllSkillsValues(_checkReal.IsChecked, _checkCaps.IsChecked);
            }

            SumTotalSkills();
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);
            writer.WriteAttributeString("isminimized", IsMinimized.ToString());
            writer.WriteAttributeString("height", _scrollArea.SpecialHeight.ToString());
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);
            _scrollArea.Height = _scrollArea.SpecialHeight = int.Parse(xml.GetAttribute("height"));
        }

        private void SumTotalSkills()
        {
            _skillsLabelSum.Text = World.Player.Skills.Sum(s => _checkReal.IsChecked ? s.Base : s.Value).ToString("F1");
        }


        private class SkillsGroupControl : Control
        {
            private readonly DataBox _box;
            private readonly Button _button;
            private readonly SkillsGroup _group;
            private readonly GumpPicTiled _gumpPic;
            private bool _isMinimized;

            private readonly List<SkillItemControl> _skills = new List<SkillItemControl>();
            private byte _status;
            private readonly StbTextBox _textbox;

            public SkillsGroupControl(SkillsGroup group, int x, int y)
            {
                CanMove = false;
                AcceptMouseInput = true;
                WantUpdateSize = true;
                AcceptKeyboardInput = true;

                X = x;
                Y = y;
                Width = 200;
                Height = 20;

                _group = group;

                _button = new Button(1000, 0x0827, 0x0827, 0x0827)
                {
                    ButtonAction = ButtonAction.Activate,
                    ContainsByBounds = true,
                    IsVisible = false
                };

                Add(_button);

                int width = FontsLoader.Instance.GetWidthASCII(6, group.Name);

                Add
                (
                    _textbox = new StbTextBox
                    (
                        6,
                        -1,
                        200,
                        false,
                        FontStyle.Fixed
                    )
                    {
                        X = 16,
                        Y = -3,
                        Width = 200,
                        Height = 17,
                        IsEditable = false
                    }
                );

                _textbox.SetText(group.Name);

                int xx = width + 11 + 16;

                _gumpPic = new GumpPicTiled(0x0835)
                {
                    X = xx,
                    Y = 5,
                    Width = 215 - xx,
                    AcceptMouseInput = false
                };

                Add(_gumpPic);

                Add(_box = new DataBox(0, 0, 0, 0));

                _textbox.IsEditable = false;

                _textbox.MouseDown += (s, e) =>
                {
                    if (_textbox.IsEditable && _status == 2)
                    {
                        return;
                    }

                    _status++;

                    if (_status >= 3)
                    {
                        _status = 0;
                    }

                    switch (_status)
                    {
                        default:
                        case 0:
                            _gumpPic.IsVisible = true;
                            _textbox.IsEditable = false;
                            _textbox.AllowSelection = false;
                            UIManager.KeyboardFocusControl = this;
                            UIManager.SystemChat.SetFocus();

                            break;

                        case 1:
                            _gumpPic.IsVisible = true;
                            _textbox.IsEditable = false;
                            _textbox.AllowSelection = false;
                            UIManager.KeyboardFocusControl = this;

                            //UIManager.SystemChat.SetFocus();
                            break;

                        case 2:
                            _gumpPic.IsVisible = false;
                            _textbox.IsEditable = true;
                            _textbox.AllowSelection = true;
                            UIManager.KeyboardFocusControl = _textbox;
                            _textbox.SetKeyboardFocus();

                            break;
                    }
                };

                _textbox.FocusLost += (s, e) =>
                {
                    _status = 0;
                    _gumpPic.IsVisible = true;
                    _textbox.IsEditable = false;
                    _textbox.AllowSelection = false;
                    UIManager.KeyboardFocusControl = null;
                    UIManager.SystemChat.SetFocus();
                };
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
                    _box.WantUpdateSize = true;

                    Parent.WantUpdateSize = true;

                    _isMinimized = value;
                    WantUpdateSize = true;
                }
            }


            public void AddSkill(int index, int x, int y)
            {
                SkillItemControl c = new SkillItemControl(index, x, y);
                _skills.Add(c);
                _box.Add(c);
                _box.WantUpdateSize = true;
                WantUpdateSize = true;

                if (!_button.IsVisible)
                {
                    _button.IsVisible = true;
                }
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
                foreach (SkillItemControl c in _skills)
                {
                    if (c.Index == index && index >= 0 && index < World.Player.Skills.Length)
                    {
                        Skill skill = World.Player.Skills[index];

                        if (skill == null)
                        {
                            return true;
                        }

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
                    if (skillControl.Parent // databox
                                    .Parent // skillgruop
                        != this)
                    {
                        SkillsGroupControl originalGroup = (SkillsGroupControl) skillControl.Parent.Parent;

                        if (originalGroup != null)
                        {
                            // remove from original control the skillcontrol
                            if (!_group.Contains((byte) skillControl.Index))
                            {
                                byte index = (byte) skillControl.Index;

                                originalGroup._skills.Remove(skillControl);

                                // update groups
                                originalGroup._group.Remove(index);
                                _group.Add(index);
                                _group.Sort();

                                originalGroup._button.IsVisible = originalGroup._skills.Count != 0;

                                // insert skillcontrol at the right index
                                int itemCount = _group.Count;

                                for (int i = 0; i < itemCount; i++)
                                {
                                    if (_group.GetSkill(i) == index)
                                    {
                                        _skills.Insert(i, skillControl);
                                        _box.Insert(i, skillControl);

                                        if (!_button.IsVisible)
                                        {
                                            _button.IsVisible = true;
                                        }

                                        break;
                                    }
                                }

                                // update gump positions
                                UpdateSkillsPosition();
                                originalGroup.UpdateSkillsPosition();
                            }
                        }
                    }
                }

                base.OnMouseOver(x, y);
            }

            public override void OnKeyboardReturn(int textID, string text)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    text = ResGumps.NoName;
                    _textbox.SetText(text);
                }

                int width = FontsLoader.Instance.GetWidthASCII(6, text);
                int xx = width + 11 + 16;

                if (xx > 0)
                {
                    _gumpPic.IsVisible = true;
                    _gumpPic.X = xx;
                    _gumpPic.Width = 215 - xx;
                }
                else
                {
                    _gumpPic.IsVisible = false;
                }

                UIManager.KeyboardFocusControl = null;
                UIManager.SystemChat.SetFocus();

                _group.Name = text;

                base.OnKeyboardReturn(textID, text);
            }

            protected override void OnKeyUp(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
            {
                base.OnKeyUp(key, mod);

                if (key == SDL.SDL_Keycode.SDLK_DELETE && _status == 1)
                {
                    if (SkillsGroupManager.Remove(_group) && RootParent is StandardSkillsGump gump)
                    {
                        SkillsGroupControl first = gump._skillsControl[0];

                        while (_box.Children.Count != 0)
                        {
                            SkillItemControl skillControl = (SkillItemControl) _box.Children[0];

                            int itemCount = first._group.Count;

                            for (int i = 0; i < itemCount; i++)
                            {
                                if (first._group.GetSkill(i) == skillControl.Index)
                                {
                                    first._skills.Insert(i, skillControl);
                                    first._box.Insert(i, skillControl);

                                    if (!first._button.IsVisible)
                                    {
                                        first._button.IsVisible = true;
                                    }

                                    break;
                                }
                            }
                        }

                        _skills.Clear();
                        Dispose();

                        first.UpdateSkillsPosition();
                    }
                }
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

                foreach (SkillItemControl c in _skills)
                {
                    c.Y = currY;
                    currY += 17;
                }

                _box.WantUpdateSize = true;
                WantUpdateSize = true;
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                ResetHueVector();

                if (_status == 2)
                {
                    batcher.Draw2D
                    (
                        SolidColorTextureCache.GetTexture(Color.Beige),
                        x,
                        y,
                        Width,
                        17,
                        ref HueVector
                    );
                }
                else if (_status == 1)
                {
                    batcher.Draw2D
                    (
                        SolidColorTextureCache.GetTexture(Color.Bisque),
                        x + 16,
                        y,
                        200,
                        17,
                        ref HueVector
                    );
                }

                return base.Draw(batcher, x, y);
            }
        }

        private class SkillItemControl : Control
        {
            private readonly Button _buttonStatus;
            private Lock _status;
            private readonly Label _value;


            public SkillItemControl(int index, int x, int y)
            {
                Index = index;
                X = x;
                Y = y;

                if (index < 0 || index >= SkillsLoader.Instance.Skills.Count)
                {
                    Dispose();

                    return;
                }

                Skill skill = World.Player.Skills[Index];

                if (skill != null)
                {
                    if (skill.IsClickable)
                    {
                        Button buttonUse = new Button(0, 0x0837, 0x0838, 0x0838)
                        {
                            ButtonAction = ButtonAction.Activate,
                            X = 8
                        };

                        Add(buttonUse);
                    }

                    _status = skill.Lock;

                    ushort graphic = GetStatusButtonGraphic();

                    _buttonStatus = new Button(1, graphic, graphic, graphic)
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
                {
                    Dispose();

                    return;
                }


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
                    {
                        return;
                    }

                    Skill skill = World.Player.Skills[Index];
                    byte newStatus = (byte) skill.Lock;

                    if (newStatus < 2)
                    {
                        newStatus++;
                    }
                    else
                    {
                        newStatus = 0;
                    }

                    NetClient.Socket.Send_SkillStatusChangeRequest((ushort)Index, newStatus);

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
                {
                    return;
                }

                Skill skill = World.Player.Skills[Index];

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
                    case Lock.Up: return 0x0984;

                    case Lock.Down: return 0x0986;

                    case Lock.Locked: return 0x082C;
                }
            }

            protected override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                if (button != MouseButtonType.Left)
                {
                    return;
                }

                UIManager.GameCursor.IsDraggingCursorForced = false;

                if (UIManager.LastControlMouseDown(MouseButtonType.Left) == this && World.Player.Skills[Index].IsClickable)
                {
                    if (UIManager.MouseOverControl == null || UIManager.MouseOverControl.RootParent != RootParent)
                    {
                        GetSpellFloatingButton(Index)?.Dispose();

                        if (Index >= 0 && Index < World.Player.Skills.Length)
                        {
                            UIManager.Add(new SkillButtonGump(World.Player.Skills[Index], Mouse.Position.X - 44, Mouse.Position.Y - 22));
                        }
                    }
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
                    batcher.Draw2D
                    (
                        SolidColorTextureCache.GetTexture(Color.Wheat),
                        x,
                        y,
                        Width,
                        Height,
                        ref HueVector
                    );
                }

                return base.Draw(batcher, x, y);
            }
        }
    }
}