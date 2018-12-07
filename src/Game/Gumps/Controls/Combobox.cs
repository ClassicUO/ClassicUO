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
using System.Linq;

using ClassicUO.Input;
using ClassicUO.IO.Resources;

namespace ClassicUO.Game.Gumps.Controls
{
    internal class Combobox : Control
    {
        private readonly string[] _items;
        private readonly Label _label;
        private readonly int _maxHeight;

        public Combobox(int x, int y, int width, string[] items, int selected = -1, int maxHeight = 0, bool showArrow = true, string emptyString = "")
        {
            X = x;
            Y = y;
            Width = width;
            Height = 25;
            SelectedIndex = selected;
            _items = items;
            _maxHeight = maxHeight;

            AddChildren(new ResizePic(0x0BB8)
            {
                Width = width, Height = Height
            });
            string initialText = selected > -1 ? items[selected] : emptyString;

            AddChildren(_label = new Label(initialText, false, 0x0453, font: 9, align: TEXT_ALIGN_TYPE.TS_LEFT)
            {
                X = 2, Y = 5
            });

            if (showArrow)
                AddChildren(new GumpPic(width - 18, 2, 0x00FC, 0));
        }

        public bool IsOpen { get; set; }

        public int SelectedIndex { get; private set; }

        public event EventHandler<int> OnOptionSelected;

        private void _contextMenu_OnOptionSelected(object sender, int e)
        {
            _label.Text = _items[e];
            SelectedIndex = e;
            Engine.UI.Remove<ComboboxContextMenu>();
            OnOptionSelected?.Invoke(this, e);
        }

        protected override void OnMouseClick(int x, int y, MouseButton button)
        {
            var contextMenu = new ComboboxContextMenu(_items, Width, _maxHeight)
            {
                X = ScreenCoordinateX, Y = ScreenCoordinateY
            };
            if (contextMenu.Height + ScreenCoordinateY > Engine.UI.Height) contextMenu.Y -= contextMenu.Height + ScreenCoordinateY - Engine.UI.Height;
            contextMenu.OnOptionSelected += _contextMenu_OnOptionSelected;
            Engine.UI.Add(contextMenu);
            base.OnMouseClick(x, y, button);
        }

        private class ComboboxContextMenu : Control
        {
            private readonly ResizePic _background;

            public ComboboxContextMenu(string[] items, int minWidth, int maxHeight)
            {
                AddChildren(_background = new ResizePic(0x0BB8));
                Label[] labels = new Label[items.Length];
                var index = 0;

                foreach (var item in items)
                {
                    var label = new HoveredLabel(item, false, 0x0453, 0x024C, font: 9, align: TEXT_ALIGN_TYPE.TS_LEFT)
                    {
                        X = 2, Y = index * 15, Tag = index
                    };
                    label.MouseClick += Label_MouseClick;
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
                        scrollArea.AddChildren(label);
                    }

                    AddChildren(scrollArea);
                    _background.Height = maxHeight;
                }
                else
                {
                    foreach (var label in labels)
                        AddChildren(label);
                    _background.Height = totalHeight;
                }

                _background.Width = maxWidth;
                Height = _background.Height;
                ControlInfo.IsModal = true;
                ControlInfo.Layer = UILayer.Over;
                ControlInfo.ModalClickOutsideAreaClosesThisControl = true;
            }

            public event EventHandler<int> OnOptionSelected;

            private void Label_MouseClick(object sender, MouseEventArgs e)
            {
                OnOptionSelected?.Invoke(this, (int) ((Label) sender).Tag);
            }
        }
    }
}