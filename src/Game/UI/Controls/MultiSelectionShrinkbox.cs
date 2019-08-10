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

using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Renderer;
using ClassicUO.Utility;

namespace ClassicUO.Game.UI.Controls
{
    internal class MultiSelectionShrinkbox : Control
    {
        private readonly GumpPic _arrow;
        private readonly int _buttongroup;
        private readonly ushort _buttonimg;

        private readonly EditableLabel _label;
        //private readonly Label _label;
        //this particular list will be used when inside a scroll area or similar situations where you want to nest a multi selection shrinkbox inside another one,
        //so that when the parent is deactivated, all the child will be made non visible
        private readonly List<MultiSelectionShrinkbox> _nestedBoxes = new List<MultiSelectionShrinkbox>();

        private readonly bool _useArrow2;
        //private NiceButton[] _buttons;
        private int[] _correspondence;
        private readonly GumpPicTiled _decoration;
        private bool _opened;
        //private GumpPic[] _pics;
        private int _selectedIndex;

        public MultiSelectionShrinkbox(int x, int y, int width, string indextext, Control[] items, ushort hue = 0x0453, bool unicode = false, byte font = 9, int group = 0, ushort button = 0, bool useArrow2 = false) : this(x, y, width, indextext, hue, unicode, font, group, button, useArrow2)
        {
            SetItemsValue(items);
        }

        public MultiSelectionShrinkbox(int x, int y, int width, string indextext, Dictionary<int, Control> items, ushort hue = 0x0453, bool unicode = false, byte font = 9, int group = 0, ushort button = 0, bool useArrow2 = false) : this(x, y, width, indextext, hue, unicode, font, group, button, useArrow2)
        {
            SetItemsValue(items);
        }

        public MultiSelectionShrinkbox(int x, int y, int width, string text, ushort hue, byte font, bool unicode,
                                       bool userArrow2 = false) : this(x, y, width, text, hue, unicode, font, 0, 0, userArrow2)
        {
        }

        private MultiSelectionShrinkbox(int x, int y, int width, string indextext, ushort hue, bool unicode, byte font, int group, ushort button, bool userArrow2)
        {
            WantUpdateSize = false;
            X = x;
            Y = y;
            _buttonimg = button;
            _buttongroup = group;
            Width = width;
            _useArrow2 = userArrow2;


            //_label = new Label(indextext, unicode, hue, font: font, align: TEXT_ALIGN_TYPE.TS_LEFT)
            //{
            //    X = 18,
            //    Y = 0
            //};


            //_label = new TextBox(font, 32, maxWidth: width, width: width, isunicode: unicode, hue: hue)
            //{
            //    X = 18,
            //    Y = 0,
            //    Width = tw,
            //    Height = th,
            //    IsEditable = false,
            //    Text = indextext
            //};

            _label = new EditableLabel(indextext, font, hue, unicode, width, FontStyle.None)
            {
                X = 18
            };

            _label.MouseUp += (senderr, e) =>
            {
                if (!IsEditable)
                    return;

                EditStateStart.Raise(_label);

                _label.SetEditable(true);
            };

            int xx = _label.X + _label.Width + 5;
            int hh = FileManager.Gumps.GetTexture(0x0835)?.Height ?? 0;
            int decWidth = width - xx - 10;

            if (decWidth < 0)
                decWidth = 0;
            _decoration = new GumpPicTiled(xx, (_label.Height >> 1) - (hh >> 1), decWidth, hh, 0x0835);

            Add(_decoration);
            Add(_label);
            Height = _label.Height;


            Add(_arrow = new GumpPic(1, 1, (ushort) (userArrow2 ? 0x0827 : 0x15E1), 0) {ContainsByBounds = true});

            _arrow.MouseUp += (sender, state) =>
            {
                if (state.Button == MouseButton.Left) Opened = !_opened;
            };
        }


        public string LabelText => _label.Text;

        public bool IsEditing => _label.GetEditable();

        internal bool Opened
        {
            get => _opened;
            set
            {
                if (_opened != value)
                {
                    _opened = value;

                    if (_opened)
                    {
                        _arrow.Graphic = (ushort) (_useArrow2 ? 0x0826 : 0x15E2);
                        OnBeforeContextMenu?.Invoke(this, null);
                        GenerateButtons();

                        foreach (MultiSelectionShrinkbox msb in _nestedBoxes)
                        {
                            msb.IsVisible = true;
                            msb.OnPageChanged();
                        }
                    }
                    else
                    {
                        _arrow.Graphic = (ushort) (_useArrow2 ? 0x0827 : 0x15E1);
                        ClearButtons();
                        Height = _label.Height;
                        OnAfterContextMenu?.Invoke(this, null);

                        foreach (MultiSelectionShrinkbox msb in _nestedBoxes)
                        {
                            msb.IsVisible = false;
                            msb.OnPageChanged();
                        }

                        foreach (Control control in Items) control.IsVisible = false;
                    }

                    Parent?.OnPageChanged();
                }
            }
        }

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                _selectedIndex = value;

                if (Items != null && _selectedIndex >= 0 && _selectedIndex < Items.Count) OnOptionSelected?.Invoke(this, value);
            }
        }

        public int SelectedItem => _correspondence != null && _selectedIndex >= 0 && _selectedIndex < _correspondence.Length ? _correspondence[_selectedIndex] : _selectedIndex;

        internal uint GetItemsLength => (uint) Items.Count;

        public List<Control> Items { get; private set; }

        public event EventHandler<EditableLabel> EditStateStart, EditStateEnd;

        public override void OnKeyboardReturn(int textID, string text)
        {
            if (_label.GetEditable())
            {
                _label.SetEditable(false);

                _decoration.X = _label.X + _label.Width + 5;
                _decoration.Width = Width - 10 - _decoration.X;

                if (_decoration.Width < 0)
                    _decoration.Width = 0;

                EditStateEnd.Raise(_label);
            }

            base.OnKeyboardReturn(textID, text);
        }

        public void SetEditableLabelState(bool edit)
        {
            if (IsEditable)
                _label.IsEditable = edit;
        }

        internal bool NestBox(MultiSelectionShrinkbox box)
        {
            if (_nestedBoxes.Contains(box))
                return false;

            Control c = Parent;

            while (c != null)
            {
                if (c is ScrollArea area)
                {
                    _arrow.IsVisible = true;
                    _nestedBoxes.Add(box);
                    box.Width = Width - box.X;
                    area.Add(box);
                    if (!_opened) box.IsVisible = false;
                    box.OnPageChanged();

                    return true;
                }

                c = c.Parent;
            }

            return false;
        }


        public void AddItem(Control t, int index = -1)
        {
            t.IsVisible = Opened;
            Add(t);

            if (index >= 0 && index < Items.Count)
                Items.Insert(index, t);
            else
                Items.Add(t);


            if (_opened)
                GenerateButtons();
            _arrow.IsVisible = Items.Count > 0 || _nestedBoxes.Count > 0;
        }

        public override void Remove(Control c)
        {
            Items.Remove(c);

            base.Remove(c);

            if (_opened)
                GenerateButtons();
            _arrow.IsVisible = Items.Count > 0 || _nestedBoxes.Count > 0;
        }

        internal void SetItemsValue(Control[] items)
        {
            Items = items.ToList();
            _correspondence = null;

            if (_opened)
                GenerateButtons();
            _arrow.IsVisible = items.Length > 0 || _nestedBoxes.Count > 0;

            Items.ForEach(s =>
            {
                s.IsVisible = false;
                Add(s);
            });
        }

        internal void SetItemsValue(Dictionary<int, Control> items)
        {
            Items = items.Select(o => o.Value).ToList();
            _correspondence = items.Select(o => o.Key).ToArray();

            Items.ForEach(s =>
            {
                s.IsVisible = false;
                Add(s);
            });

            if (_opened)
                GenerateButtons();
            _arrow.IsVisible = items.Count > 0 || _nestedBoxes.Count > 0;
        }

        public void GenerateButtons()
        {
            if (!_opened)
                return;

            //ClearButtons();
            //_buttons = new NiceButton[_items.Length];

            //if (_buttonimg > 0)
            //    _pics = new GumpPic[_items.Length];

            var index = 0;
            //int width = 0;
            int height = 0;
            int lh = _label.Height + 2;

            //foreach (var item in _items)
            //{
            //    int w, h;

            //    w = item.Width;
            //    h = item.Height;
            //    height = h + 2;


            //    //if (_label.Unicode)
            //    //    w = FileManager.Fonts.GetWidthUnicode(_label.Font, item);
            //    //else
            //    //    w = FileManager.Fonts.GetWidthASCII(_label.Font, item);

            //    //if (w > width)
            //    //{
            //    //    if (_label.Unicode)
            //    //        h = FileManager.Fonts.GetHeightUnicode(_label.Font, item, w, TEXT_ALIGN_TYPE.TS_LEFT, 0x0);
            //    //    else
            //    //        h = FileManager.Fonts.GetHeightASCII(_label.Font, item, w, TEXT_ALIGN_TYPE.TS_LEFT, 0x0);
            //    //    width = w;
            //    //    height = h + 2;
            //    //}
            //}

            foreach (Control control in Items)
            {
                control.IsVisible = true;
                control.X = 6;
                control.Y = height + lh;
                height += control.Height;

                index++;
            }

            //foreach (var item in _items)
            //{
            //    var but = new NiceButton(20, index * height + lh, width, height, ButtonAction.Activate, item, _buttongroup, TEXT_ALIGN_TYPE.TS_LEFT) {Tag = index};
            //    if (_buttonimg > 0)
            //        Add(_pics[index] = new GumpPic(6, index * height + lh + 2, _buttonimg, 0));
            //    but.MouseClick += Selection_MouseClick;
            //    _buttons[index] = but;
            //    Add(but);
            //    index++;
            //}

            var totalHeight = Items.Sum(o => o.Height);

            Height = totalHeight + lh;

            Parent.WantUpdateSize = true;
        }

        private void ClearButtons()
        {
            //if (_buttons != null)
            //{
            //    for (int i = _buttons.Length - 1; i >= 0; --i)
            //    {
            //        _buttons[i]?.Dispose();
            //        _buttons[i] = null;
            //    }
            //}

            //if (_pics != null)
            //{
            //    for (int i = _pics.Length - 1; i >= 0; --i)
            //    {
            //        _pics[i]?.Dispose();
            //        _pics[i] = null;
            //    }
            //}
        }

        private void Selection_MouseClick(object sender, MouseEventArgs e)
        {
            SelectedIndex = (int) ((Control) sender).Tag;
        }

        public event EventHandler<int> OnOptionSelected;
        public event EventHandler OnBeforeContextMenu;
        public event EventHandler OnAfterContextMenu;

        protected override bool OnMouseDoubleClick(int x, int y, MouseButton button)
        {
            if (_label.Bounds.Contains(Mouse.Position.X - ScreenCoordinateX, Mouse.Position.Y - ScreenCoordinateY) && button == MouseButton.Left)
                Opened = !_opened;

            return base.OnMouseDoubleClick(x, y, button);
        }

        public override void OnPageChanged()
        {
            Parent?.OnPageChanged();
        }

        public void SetIndexText(string text)
        {
            if (!string.IsNullOrEmpty(text))
                _label.Text = text;
        }
    }
}