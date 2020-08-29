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

using ClassicUO.Game.Managers;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassicUO.Game.UI.Controls
{
    internal class StbTextBoxCombo : StbTextBox
    {
        private string[] _items;
        private int _selectedIndex;
        private GumpPic _arrow;
        private readonly byte _font;
        private readonly int _maxWidth;
        private readonly int _maxComboHeight;
        private ComboboxContextMenu _contextMenu;
        private const ushort ARROW_UP = 253;
        private const ushort ARROW_DOWN = 252;

        public StbTextBoxCombo(string[] items, byte font, int max_char_count = -1, int maxWidth = 0, bool isunicode = true, FontStyle style = FontStyle.None, ushort hue = 0, TEXT_ALIGN_TYPE align = TEXT_ALIGN_TYPE.TS_LEFT, int maxComboHeight = 100) :
            base(font, max_char_count, maxWidth, isunicode, style, hue, align)
        {
            _maxWidth = maxWidth;
            _maxComboHeight = maxComboHeight;
            _font = font;
            SetItems(items);
        }

        public StbTextBoxCombo(List<string> parts, string[] lines) : base(parts, lines)
        {
        }

        internal void SetItems(string[] items)
        {
            _items = items;
            if (_items.Length > 0 && _arrow == null)
            {
                Add(_arrow = new GumpPic(_maxWidth - 6, 4, ARROW_UP, 0));
            }
        }

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                _selectedIndex = value;

                if (_items != null)
                {
                    //Cleanup Context Menu
                    _contextMenu.OnItemSelected -= ItemSelectedHandler;
                    UIManager.GetGump<ComboboxContextMenu>()?.Dispose();
                    _contextMenu = null;

                    Text = _items[_selectedIndex];
                    CaretIndex = Text.Length;
                    SetKeyboardFocus();
                    OnOptionSelected?.Invoke(this, value);
                }
            }
        }

        private void ItemSelectedHandler(object sender, int value)
        {
            SelectedIndex = value;
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button != MouseButtonType.Left)
            {
                return;
            }

            if (_arrow != null)
            {
                _arrow.Graphic = ARROW_UP;
                if (x > _arrow.X && x < _arrow.X + _arrow.Width && y > _arrow.Y && y < _arrow.Y + _arrow.Height)
                {
                    OnBeforeContextMenu?.Invoke(this, null);
                    _contextMenu = new ComboboxContextMenu(_items, Width - 9, _maxComboHeight, _font, 20)
                    {
                        X = ScreenCoordinateX - 6,
                        Y = ScreenCoordinateY + Height + 5
                    };
                    _contextMenu.OnItemSelected += ItemSelectedHandler;
                    if (_contextMenu.Height + ScreenCoordinateY > Client.Game.Window.ClientBounds.Height)
                    {
                        _contextMenu.Y -= _contextMenu.Height + ScreenCoordinateY - Client.Game.Window.ClientBounds.Height;
                    }

                    UIManager.Add(_contextMenu);
                }
            }

            base.OnMouseUp(x, y, button);
        }
        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            if (button != MouseButtonType.Left)
            {
                return;
            }

            if (_arrow != null && x > _arrow.X && x < _arrow.X + _arrow.Width && y > _arrow.Y && y < _arrow.Y + _arrow.Height)
            {
                _arrow.Graphic = ARROW_DOWN;
            }

            base.OnMouseDown(x, y, button);
        }

        internal string GetSelectedItem => Text;

        internal uint GetItemsLength => (uint)_items.Length;

        public event EventHandler<int> OnOptionSelected;
        public event EventHandler OnBeforeContextMenu;
    }
}