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
using System.Linq;
using System.Collections.Generic;

using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;

namespace ClassicUO.Game.UI.Controls
{
    internal class MultiSelectionShrinkbox : Control
    {
        //this particular list will be used when inside a scroll area or similar situations where you want to nest a multi selection shrinkbox inside another one,
        //so that when the parent is deactivated, all the child will be made non visible
        private readonly List<MultiSelectionShrinkbox> _nestedBoxes = new List<MultiSelectionShrinkbox>();
        private readonly Label _label;
        private readonly ushort _buttonimg;
        private readonly int _buttongroup;
        private string[] _items;
        private NiceButton[] _buttons;
        private GumpPic[] _pics = null;
        private int _selectedIndex;
        private GumpPic _arrow;
        private bool _opened = false;

        internal bool NestBox(MultiSelectionShrinkbox box)
        {
            Control c = Parent;
            while(c != null)
            {
                if (c is ScrollAreaItem area)
                {
                    _nestedBoxes.Add(box);
                    if (!_opened)
                        box.IsVisible = false;
                    box.Width = Width - box.X;
                    area.Add(box);
                    return true;
                }
                c = c.Parent;
            }
            return false;
        }

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
                        _arrow.Graphic = 0x15E2;
                        OnBeforeContextMenu?.Invoke(this, null);
                        GenerateButtons();
                        foreach (MultiSelectionShrinkbox msb in _nestedBoxes)
                        {
                            msb.Y = Y + Height;
                            msb.IsVisible = true;
                            msb.OnPageChanged();
                        }
                    }
                    else
                    {
                        _arrow.Graphic = 0x15E1;
                        ClearButtons();
                        Height = _label.Height;
                        OnAfterContextMenu?.Invoke(this, null);
                        foreach (MultiSelectionShrinkbox msb in _nestedBoxes)
                        {
                            msb.IsVisible = false;
                            msb.OnPageChanged();
                        }
                    }
                    Parent?.OnPageChanged();
                }
            }
        }

        public MultiSelectionShrinkbox(int x, int y, int width, string indextext, string[] items, ushort hue = 0x0453, bool unicode = false, byte font = 9, int group = 0, ushort button = 0)
        {
            WantUpdateSize = false;
            X = x;
            Y = y;
            _buttonimg = button;
            _buttongroup = group;
            Width = width;

            Add(_label = new Label(indextext, unicode, hue, font: font, align: TEXT_ALIGN_TYPE.TS_LEFT)
            {
                X = 18,
                Y = 0
            });
            Height = _label.Height;

            Add(_arrow = new GumpPic(1, 1, 0x15E1, 0));
            _arrow.MouseClick += (sender, state) =>
            { Opened = !_opened; };

            SetItemsValue(items);
        }

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                _selectedIndex = value;

                if (_items != null && _selectedIndex >= 0 && _selectedIndex < _items.Length)
                {
                    OnOptionSelected?.Invoke(this, value);
                }
            }
        }

        internal uint GetItemsLength => (uint)_items.Length;

        internal void SetItemsValue(string[] items)
        {
            _items = items;
            if (_opened)
                GenerateButtons();
        }

        private void GenerateButtons()
        {
            ClearButtons();
            _buttons = new NiceButton[_items.Length];
            if (_buttonimg > 0)
                _pics = new GumpPic[_items.Length];

            var index = 0;
            int width = 0;
            int height = 0;
            int lh = _label.Height + 2;

            foreach (string item in _items)
            {
                int w, h;
                if (_label.Unicode)
                    w = FileManager.Fonts.GetWidthUnicode(_label.Font, item);
                else
                    w = FileManager.Fonts.GetWidthASCII(_label.Font, item);
                if (w > width)
                {
                    if (_label.Unicode)
                        h = FileManager.Fonts.GetHeightUnicode(_label.Font, item, w, TEXT_ALIGN_TYPE.TS_LEFT, 0x0);
                    else
                        h = FileManager.Fonts.GetHeightASCII(_label.Font, item, w, TEXT_ALIGN_TYPE.TS_LEFT, 0x0);
                    width = w;
                    height = h;
                }
            }

            foreach (var item in _items)
            {
                var but = new NiceButton(20, index * height + lh, width, height, ButtonAction.Activate, item, _buttongroup, TEXT_ALIGN_TYPE.TS_LEFT) { Tag = index };
                if (_buttonimg > 0)
                {
                    Add(_pics[index] = new GumpPic(6, index * height + lh + 2, _buttonimg, 0));
                }
                but.MouseClick += Selection_MouseClick;
                _buttons[index] = but;
                Add(but);
                index++;
            }

            var totalHeight = _buttons.Max(o => o.Y + o.Height);

            Height = totalHeight + 2;//this is for extra spacing
        }

        private void ClearButtons()
        {
            if (_buttons != null)
            {
                for (int i = _buttons.Length - 1; i >= 0; --i)
                {
                    _buttons[i]?.Dispose();
                    _buttons[i] = null;
                }
            }
            if (_pics != null)
            {
                for (int i = _pics.Length - 1; i >= 0; --i)
                {
                    _pics[i]?.Dispose();
                    _pics[i] = null;
                }
            }
        }

        private void Selection_MouseClick(object sender, MouseEventArgs e)
        {
            SelectedIndex = (int)((Control)sender).Tag;
        }

        public event EventHandler<int> OnOptionSelected;
        public event EventHandler OnBeforeContextMenu;
        public event EventHandler OnAfterContextMenu;

        protected override bool OnMouseDoubleClick(int x, int y, MouseButton button)
        {
            Opened = !_opened;
            return base.OnMouseDoubleClick(x, y, button);
        }

        public override void OnPageChanged()
        {
            Parent?.OnPageChanged();
        }
    }
}
