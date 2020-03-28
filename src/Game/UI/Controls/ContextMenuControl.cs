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
using System.Runtime.InteropServices;

using ClassicUO.Game.Managers;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    class ContextMenuControl
    {
        private readonly List<ContextMenuItemEntry> _items;

        public ContextMenuControl()
        {
            _items = new List<ContextMenuItemEntry>();
        }

        public void Add(string text, Action action, bool canBeSelected = false, bool defaultValue = false)
        {
            _items.Add(new ContextMenuItemEntry(text, action, canBeSelected, defaultValue));
        }

        public void Add(ContextMenuItemEntry entry)
        {
            _items.Add(entry);
        }

        public void Add(string text, List<ContextMenuItemEntry> entries)
        {
            _items.Add(new ContextMenuItemEntry(text)
            {
                Items = entries
            });
        }

        public void Show()
        {
            UIManager.ShowContextMenu(null);

            if (_items.Count == 0)
                return;

            UIManager.ShowContextMenu
            (
                new ContextMenuShowMenu(_items)
                {
                    X = Mouse.Position.X + 5,
                    Y = Mouse.Position.Y - 20
                }
            );
        }
    }

    sealed class ContextMenuItemEntry
    {
        public ContextMenuItemEntry(string text, Action action = null, bool canBeSelected = false, bool defaultValue = false)
        {
            Text = text;
            Action = action;
            CanBeSelected = canBeSelected;
            IsSelected = defaultValue;
        }

        public readonly Action Action;
        public readonly string Text;
        public readonly bool CanBeSelected;
        public bool IsSelected;
        public List<ContextMenuItemEntry> Items = new List<ContextMenuItemEntry>();

        public void Add(ContextMenuItemEntry subEntry)
        {
            Items.Add(subEntry);
        }
    }




    class ContextMenuShowMenu : Control
    {
        private readonly AlphaBlendControl _background;
        private List<ContextMenuShowMenu> _subMenus;


        public ContextMenuShowMenu(List<ContextMenuItemEntry> list)
        {
            WantUpdateSize = true;
            ControlInfo.ModalClickOutsideAreaClosesThisControl = true;
            ControlInfo.IsModal = true;
            ControlInfo.Layer = UILayer.Over;

            CanMove = false;
            AcceptMouseInput = true;



            _background = new AlphaBlendControl(0.3f);
            Add(_background);

            int y = 0;

            for (int i = 0; i < list.Count; i++)
            {
                var item = new ContextMenuItem(this, list[i]);
                if (i > 0)
                {
                    item.Y = y;
                }

                if (_background.Width < item.Width)
                {
                    _background.Width = item.Width;
                }

                _background.Height += item.Height;

                Add(item);

                y += item.Height;
            }


            foreach (var mitem in FindControls<ContextMenuItem>())
            {
                if (mitem.Width < _background.Width)
                    mitem.Width = _background.Width;
            }
        }

        

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);
            WantUpdateSize = true;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ResetHueVector();
            batcher.DrawRectangle(Texture2DCache.GetTexture(Color.Gray), x - 1, y - 1, _background.Width + 1, _background.Height + 1, ref _hueVector);
            return base.Draw(batcher, x, y);
        }

        public override bool Contains(int x, int y)
        {
            if (_background.Bounds.Contains(x, y))
                return true;

            if (_subMenus != null)
            {
                foreach (ContextMenuShowMenu menu in _subMenus)
                {
                    if (menu.Contains(x - menu.X, y - menu.Y))
                        return true;
                }
            }

            return  false;
        }

        private class ContextMenuItem : Control
        {
            private readonly Label _label;
            private readonly GumpPic _selectedPic;
            private readonly ContextMenuItemEntry _entry;
            private readonly ContextMenuShowMenu _subMenu;


            private static readonly RenderedText _moreMenuLabel = RenderedText.Create(">", 1150, isunicode: true, style: FontStyle.BlackBorder);


            public ContextMenuItem(ContextMenuShowMenu parent, ContextMenuItemEntry entry)
            {
                CanCloseWithRightClick = false;
                _entry = entry;

                _label = new Label(entry.Text, true, 1150, 0, style: FontStyle.BlackBorder)
                {
                    X = 25,
                };
                Add(_label);


                if (entry.CanBeSelected)
                {
                    _selectedPic = new GumpPic(3, 0, 0x838, 0)
                    {
                        IsVisible = entry.IsSelected,
                        IsEnabled = false
                    };
                    Add(_selectedPic);
                }

                Height = 25;


                _label.Y = (Height >> 1) - (_label.Height >> 1);

                if (_selectedPic != null)
                {
                    //_label.X = _selectedPic.X + _selectedPic.Width + 6;
                    _selectedPic.Y = (Height >> 1) - (_selectedPic.Height >> 1);
                }

                Width = _label.X + _label.Width + 20;

                if (Width < 100)
                    Width = 100;

                // it is a bit tricky, but works :D 
                if (_entry.Items != null && _entry.Items.Count != 0)
                {
                    _subMenu = new ContextMenuShowMenu(_entry.Items);
                    parent.Add(_subMenu);
                    if (parent._subMenus == null)
                        parent._subMenus = new List<ContextMenuShowMenu>();
                    parent._subMenus.Add(_subMenu);
                }

                WantUpdateSize = false;
            }



            public override void Update(double totalMS, double frameMS)
            {
                base.Update(totalMS, frameMS);

                if (Width > _label.Width)
                    _label.Width = Width;

                if (_subMenu != null)
                {
                    _subMenu.X = Width;
                    _subMenu.Y = Y;

                    if (MouseIsOver)
                    {
                        _subMenu.IsVisible = true;
                    }
                    else
                    {
                        var p = UIManager.MouseOverControl?.Parent;

                        while (p != null)
                        {
                            if (p == _subMenu)
                            {
                                break;
                            }
                            p = p.Parent;
                        }

                        _subMenu.IsVisible = p != null;
                    }

                }

            }

            protected override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                if (button == MouseButtonType.Left)
                {
                    _entry.Action?.Invoke();

                    RootParent?.Dispose();

                    if (_entry.CanBeSelected)
                    {
                        _entry.IsSelected = !_entry.IsSelected;
                        _selectedPic.IsVisible = _entry.IsSelected;
                    }

                    Mouse.CancelDoubleClick = true;
                    Mouse.LastLeftButtonClickTime = 0;
                    base.OnMouseUp(x, y, button);
                }
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                if (!string.IsNullOrWhiteSpace(_label.Text) && MouseIsOver)
                {
                    ResetHueVector();

                    batcher.Draw2D(Texture2DCache.GetTexture(Color.Gray), x + 2, y + 5, Width - 4, Height - 10, ref _hueVector);
                }

                base.Draw(batcher, x, y);

                if (_entry.Items != null && _entry.Items.Count != 0)
                {
                    _moreMenuLabel.Draw(batcher, x + Width - _moreMenuLabel.Width, y + (Height >> 1) - (_moreMenuLabel.Height >> 1) - 1);
                }

                return true;
            }
        }
    }
}
