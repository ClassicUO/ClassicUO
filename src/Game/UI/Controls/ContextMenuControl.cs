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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.Managers;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    class ContextMenuControl : Control
    {
        private readonly List<ContextMenuItemEntry> _items;
        private ContextMenuShowMenu _menu;

        public ContextMenuControl()
        {
            CanMove = true;
            AcceptMouseInput = true;

            _items = new List<ContextMenuItemEntry>();

            WantUpdateSize = false;
        }


        //public override void Update(double totalMS, double frameMS)
        //{
        //    base.Update(totalMS, frameMS);

        //    //if (Parent != null)
        //    //{
        //    //    Width = Parent.Width;
        //    //    Height = Parent.Height;
        //    //}
        //}

        //protected override void OnMouseUp(int x, int y, MouseButtonType button)
        //{
        //    if (button != MouseButtonType.Right)
        //    {
        //        base.OnMouseUp(x, y, button);
        //        return;
        //    }

        //    _menu?.Dispose();

        //    _menu = new ContextMenuShowMenu(_items)
        //    {
        //        X = Mouse.Position.X,
        //        Y = Mouse.Position.Y
        //    };
        //    UIManager.Add(_menu);
        //}

        public void Add(string text, Action action, bool canBeSelected = false, bool defaultValue = false)
        {
            _items.Add(new ContextMenuItemEntry(text, action, canBeSelected, defaultValue));
        }

        public override void Add(Control c, int page = 0)
        {
        }

        public void Show()
        {
            _menu?.Dispose();

            if (_items.Count == 0)
                return;

            _menu = new ContextMenuShowMenu(_items)
            {
                X = Mouse.Position.X,
                Y = Mouse.Position.Y
            };
            UIManager.Add(_menu);
        }

        private class ContextMenuShowMenu : Control
        {
            public ContextMenuShowMenu(List<ContextMenuItemEntry> list)
            {
                WantUpdateSize = false;
                ControlInfo.ModalClickOutsideAreaClosesThisControl = true;
                ControlInfo.IsModal = true;
                ControlInfo.Layer = UILayer.Over;
    
                CanMove = true;
                AcceptMouseInput = true;



                AlphaBlendControl background = new AlphaBlendControl(0.3f);
                Add(background);

                int y = 0;

                for (int i = 0; i < list.Count; i++)
                {
                    var item = new ContextMenuItem(list[i]);
                    if (i > 0)
                    {
                        item.Y = y;
                    }

                    if (background.Width < item.Width)
                    {
                        background.Width = item.Width;
                    }

                    background.Height += item.Height;

                    Add(item);

                    y += item.Height;
                }


                foreach (var mitem in FindControls<ContextMenuItem>())
                {
                    if (mitem.Width < background.Width)
                        mitem.Width = background.Width;
                }


                Width = background.Width;
                Height = background.Height;
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                ResetHueVector();
                batcher.DrawRectangle(Texture2DCache.GetTexture(Color.Gray), x - 1, y - 1, Width + 1, Height + 1, ref _hueVector);
                return base.Draw(batcher, x, y);
            }
        }
    }


    sealed class ContextMenuItemEntry
    {
        public ContextMenuItemEntry(string text, Action action, bool canBeSelected, bool defaultValue)
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
    }

    class ContextMenuItem : Control
    {
        private readonly Label _label;
        private readonly GumpPic _selectedPic;
        private readonly ContextMenuItemEntry _entry;


        public ContextMenuItem(ContextMenuItemEntry entry)
        {
            CanCloseWithRightClick = false;
            _entry = entry;

            _label = new Label(entry.Text, true, 1150, 0, style: FontStyle.BlackBorder)
            {
                X = 10,
            };
            Add(_label);

            if (entry.CanBeSelected)
            {
                _selectedPic = new GumpPic(0, 0, 0x838, 0);
                _selectedPic.IsVisible = entry.IsSelected;
                Add(_selectedPic);
            }

            Height = 25;


            _label.Y = (Height >> 1) - (_label.Height >> 1);

            if (_selectedPic != null)
            {
                _label.X = _selectedPic.X + _selectedPic.Width + 6;
                _selectedPic.Y = (Height >> 1) - (_selectedPic.Height >> 1);
            }
            Width = _label.X + _label.Width + 3;

            WantUpdateSize = false;
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (Width > _label.Width)
                _label.Width = Width;
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                _entry.Action?.Invoke();

                Parent?.Dispose();

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

            return base.Draw(batcher, x, y);
        }
    }
}
