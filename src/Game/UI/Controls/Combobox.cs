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
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class Combobox : Control
    {
        private readonly byte _font;
        private readonly string[] _items;
        private readonly Label _label;
        private readonly int _maxHeight;
        private int _selectedIndex;

        public Combobox
        (
            int x,
            int y,
            int width,
            string[] items,
            int selected = -1,
            int maxHeight = 200,
            bool showArrow = true,
            string emptyString = "",
            byte font = 9
        )
        {
            X = x;
            Y = y;
            Width = width;
            Height = 25;
            SelectedIndex = selected;
            _font = font;
            _items = items;
            _maxHeight = maxHeight;

            Add
            (
                new ResizePic(0x0BB8)
                {
                    Width = width, Height = Height
                }
            );

            string initialText = selected > -1 ? items[selected] : emptyString;

            Add
            (
                _label = new Label(initialText, false, 0x0453, font: _font)
                {
                    X = 2, Y = 5
                }
            );

            if (showArrow)
            {
                Add(new GumpPic(width - 18, 2, 0x00FC, 0));
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
                    _label.Text = _items[value];

                    OnOptionSelected?.Invoke(this, value);
                }
            }
        }


        public event EventHandler<int> OnOptionSelected;


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


        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button != MouseButtonType.Left)
            {
                return;
            }

            int comboY = ScreenCoordinateY + Offset.Y;

            if (comboY < 0)
            {
                comboY = 0;
            }
            else if (comboY + _maxHeight > Client.Game.Window.ClientBounds.Height)
            {
                comboY = Client.Game.Window.ClientBounds.Height - _maxHeight;
            }

            UIManager.Add
            (
                new ComboboxGump
                    (ScreenCoordinateX, comboY, Width, _maxHeight, _items, _font, this)
            );

            base.OnMouseUp(x, y, button);
        }

        private class ComboboxGump : Gump
        {
            private const int ELEMENT_HEIGHT = 15;


            private readonly Combobox _combobox;

            public ComboboxGump
            (
                int x,
                int y,
                int width,
                int maxHeight,
                string[] items,
                byte font,
                Combobox combobox
            ) : base(0, 0)
            {
                CanMove = false;
                AcceptMouseInput = true;
                X = x;
                Y = y;

                IsModal = true;
                LayerOrder = UILayer.Over;
                ModalClickOutsideAreaClosesThisControl = true;

                _combobox = combobox;

                ResizePic background;
                Add(background = new ResizePic(0x0BB8));
                background.AcceptMouseInput = false;

                HoveredLabel[] labels = new HoveredLabel[items.Length];

                for (int i = 0; i < items.Length; i++)
                {
                    string item = items[i];

                    if (item == null)
                    {
                        item = string.Empty;
                    }

                    HoveredLabel label = new HoveredLabel(item, false, 0x0453, 0x0453, 0x0453, font: font)
                    {
                        X = 2,
                        Y = i * ELEMENT_HEIGHT,
                        DrawBackgroundCurrentIndex = true,
                        IsVisible = item.Length != 0,
                        Tag = i
                    };

                    label.MouseUp += LabelOnMouseUp;

                    labels[i] = label;
                }

                int totalHeight = Math.Min(maxHeight, labels.Max(o => o.Y + o.Height));
                int maxWidth = Math.Max(width, labels.Max(o => o.X + o.Width));

                ScrollArea area = new ScrollArea(0, 0, maxWidth + 15, totalHeight, true);

                foreach (HoveredLabel label in labels)
                {
                    label.Width = maxWidth;
                    area.Add(label);
                }

                Add(area);

                background.Width = maxWidth;
                background.Height = totalHeight;
            }

            private void LabelOnMouseUp(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtonType.Left)
                {
                    _combobox.SelectedIndex = (int) ((Label) sender).Tag;

                    Dispose();
                }
            }
        }
    }
}