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
using System.Linq;

using ClassicUO.Game.Managers;
using ClassicUO.Input;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class Combobox : Control
    {
        private ComboboxContextMenu _contextMenu;
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
            {
                Add(new GumpPic(width - 18, 2, 0x00FC, 0));
            }
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
                    CleanupContextMenu();
                    _label.Text = _items[value];
                    OnOptionSelected?.Invoke(this, value);
                }
            }
        }



        private void CleanupContextMenu()
        {
            _contextMenu.OnItemSelected -= ItemSelectedHandler;
            _contextMenu.Dispose();
        }

        internal string GetSelectedItem => _label.Text;

        internal uint GetItemsLength => (uint) _items.Length;

        internal void SetItemsValue(string[] items)
        {
            _items = items;
        }

        public event EventHandler<int> OnOptionSelected;
        public event EventHandler OnBeforeContextMenu;

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            Rectangle scissor = ScissorStack.CalculateScissors(Matrix.Identity, x, y, Width, Height);

            if (ScissorStack.PushScissors(batcher.GraphicsDevice, scissor))
            {
                batcher.EnableScissorTest(true);
                base.Draw(batcher, x, y);
                batcher.EnableScissorTest(false);
                ScissorStack.PopScissors(batcher.GraphicsDevice);
            }

            return true; 
        }

        protected void ItemSelectedHandler(object sender, int selected)
        {
            SelectedIndex = selected;
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button != MouseButtonType.Left)
                return;

            OnBeforeContextMenu?.Invoke(this, null);

            _contextMenu = new ComboboxContextMenu(_items, Width, _maxHeight, _font)
            {
                X = ScreenCoordinateX,
                Y = ScreenCoordinateY
            };
            _contextMenu.OnItemSelected += ItemSelectedHandler;

            if (_contextMenu.Height + ScreenCoordinateY > Client.Game.Window.ClientBounds.Height)
            {
                _contextMenu.Y -= _contextMenu.Height + ScreenCoordinateY - Client.Game.Window.ClientBounds.Height;
            }
            UIManager.Add(_contextMenu);
            base.OnMouseUp(x, y, button);
        }
    }
}