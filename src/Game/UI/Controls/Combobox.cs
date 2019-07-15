﻿#region license

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

using ClassicUO.Input;

namespace ClassicUO.Game.UI.Controls
{
    internal class Combobox : Control
    {
        private readonly byte _font;
        private readonly Label _label;
        private readonly int _maxHeight;
        private string[] _items;
        private int _selectedIndex;

        public Combobox(int x, int y, int width, string[] items, int selected = -1, int maxHeight = 0, bool showArrow = true, string emptyString = "", byte font = 9)
        {
            X = x;
            Y = y;
            Width = width;
            Height = 25;
            SelectedIndex = selected;
            _items = items;
            _maxHeight = maxHeight;
            _font = font;

            Add(new ResizePic(0x0BB8)
            {
                Width = width, Height = Height
            });
            string initialText = selected > -1 ? items[selected] : emptyString;

            Add(_label = new Label(initialText, false, 0x0453, font: _font)
            {
                X = 2, Y = 5
            });

            if (showArrow)
                Add(new GumpPic(width - 18, 2, 0x00FC, 0));
        }

        public bool IsOpen { get; set; }

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                _selectedIndex = value;

                if (_items != null)
                {
                    _label.Text = _items[value];
                    Engine.UI.Remove<ComboboxContextMenu>();
                    OnOptionSelected?.Invoke(this, value);
                }
            }
        }

        internal string GetSelectedItem => _label.Text;

        internal uint GetItemsLength => (uint) _items.Length;

        internal void SetItemsValue(string[] items)
        {
            _items = items;
        }

        public event EventHandler<int> OnOptionSelected;
        public event EventHandler OnBeforeContextMenu;


        protected override void OnMouseUp(int x, int y, MouseButton button)
        {
            OnBeforeContextMenu?.Invoke(this, null);

            var contextMenu = new ComboboxContextMenu(this, _items, Width, _maxHeight)
            {
                X = ScreenCoordinateX,
                Y = ScreenCoordinateY
            };
            if (contextMenu.Height + ScreenCoordinateY > Engine.WindowHeight) contextMenu.Y -= contextMenu.Height + ScreenCoordinateY - Engine.WindowHeight;
            Engine.UI.Add(contextMenu);
            base.OnMouseUp(x, y, button);
        }

        private class ComboboxContextMenu : Control
        {
            private readonly Combobox _box;

            public ComboboxContextMenu(Combobox box, string[] items, int minWidth, int maxHeight)
            {
                _box = box;
                ResizePic background;
                Add(background = new ResizePic(0x0BB8));
                Label[] labels = new Label[items.Length];
                var index = 0;

                foreach (var item in items)
                {
                    var label = new HoveredLabel(item, false, 0x0453, 0x024C, font: _box._font)
                    {
                        X = 2,
                        Y = index * 15,
                        Tag = index
                    };
                    label.MouseUp += Label_MouseUp;
                    labels[index] = label;
                    index++;
                }

                var totalHeight = labels.Max(o => o.Y + o.Height);
                var maxWidth = Math.Max(minWidth, labels.Max(o => o.X + o.Width));

                if (maxHeight != 0 && totalHeight > maxHeight)
                {
                    var scrollArea = new ScrollArea(0, 0, maxWidth + 15, maxHeight, true);

                    foreach (var label in labels)
                    {
                        label.Y = 0;
                        label.Width = maxWidth;
                        scrollArea.Add(label);
                    }

                    Add(scrollArea);
                    background.Height = maxHeight;
                }
                else
                {
                    foreach (var label in labels)
                    {
                        label.Width = maxWidth;
                        Add(label);
                    }

                    background.Height = totalHeight;
                }

                background.Width = maxWidth;
                Height = background.Height;
                ControlInfo.IsModal = true;
                ControlInfo.Layer = UILayer.Over;
                ControlInfo.ModalClickOutsideAreaClosesThisControl = true;
            }

            private void Label_MouseUp(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButton.Left)
                    _box.SelectedIndex = (int) ((Label) sender).Tag;
            }
        }
    }
}