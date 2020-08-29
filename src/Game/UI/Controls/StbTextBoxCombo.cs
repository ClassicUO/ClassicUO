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
		private const ushort ARROW_UP = 253;
		private const ushort ARROW_DOWN = 252;

		public StbTextBoxCombo(string[] items, byte font, int max_char_count = -1, int maxWidth = 0, bool isunicode = true, FontStyle style = FontStyle.None, ushort hue = 0, TEXT_ALIGN_TYPE align = TEXT_ALIGN_TYPE.TS_LEFT) : 
			base(font, max_char_count, maxWidth, isunicode, style, hue, align)
		{
			_maxWidth = maxWidth;
			_font = font;
			SetItems(items);
		}

		public StbTextBoxCombo(List<string> parts, string[] lines) : base(parts, lines)
		{
		}

		internal void SetItems(string[] items)
		{
			_items = items;
			if(_items.Length > 0 && _arrow == null)
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
					UIManager.GetGump<TextboxComboContextMenu>()?.Dispose();
					Text = _items[_selectedIndex];
					CaretIndex = Text.Length;
					SetKeyboardFocus();
					OnOptionSelected?.Invoke(this, value);
				}
			}
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
					TextboxComboContextMenu contextMenu = new TextboxComboContextMenu(_items, Width - 9, 100, _font, selectedIndex => SelectedIndex = selectedIndex)
					{
						X = ScreenCoordinateX - 6,
						Y = ScreenCoordinateY + Height + 5
					};
					if (contextMenu.Height + ScreenCoordinateY > Client.Game.Window.ClientBounds.Height)
					{
						contextMenu.Y -= contextMenu.Height + ScreenCoordinateY - Client.Game.Window.ClientBounds.Height;
					}

					UIManager.Add(contextMenu);
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


	internal class TextboxComboContextMenu : Control
	{
		private readonly Action<int> _setIndex;
		public TextboxComboContextMenu(string[] items, int minWidth, int maxHeight, byte font, Action<int> setIndex)
		{
			ResizePic background;
			Add(background = new ResizePic(0x0BB8));
			HoveredLabel[] labels = new HoveredLabel[items.Length];
			int index = 0;

			for (int i = 0; i < items.Length; i++)
			{
				string item = items[i];

				if (item == null)
					item = string.Empty;

				HoveredLabel label = new HoveredLabel(item, false, 0x0453, 0x0453, 0x0453, font: font)
				{
					X = 0,
					Y = index * 20,
					Height = 25,
					Tag = index,
					DrawBackgroundCurrentIndex = true,
					IsVisible = item.Length != 0
				};
				label.MouseUp += Label_MouseUp;
				labels[index++] = label;
			}

			int totalHeight = labels.Max(o => o.Y + o.Height);
			int maxWidth = Math.Max(minWidth, labels.Max(o => o.X + o.Width));

			if (maxHeight != 0 && totalHeight > maxHeight)
			{
				ScrollArea scrollArea = new ScrollArea(0, 0, maxWidth + 15, maxHeight, true);
				foreach (HoveredLabel label in labels)
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
				foreach (HoveredLabel label in labels)
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
			_setIndex = setIndex;
		}

		private void Label_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtonType.Left)
				_setIndex((int)((Label)sender).Tag);
		}
	}
}