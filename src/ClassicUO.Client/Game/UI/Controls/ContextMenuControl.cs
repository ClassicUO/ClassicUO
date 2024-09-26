#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace ClassicUO.Game.UI.Controls
{
    public class ContextMenuControl
    {
        private readonly List<ContextMenuItemEntry> _items;
        private readonly Gump _gump;

        public ContextMenuControl(Gump gump)
        {
            _items = new List<ContextMenuItemEntry>();
            _gump = gump;
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
            _items.Add
            (
                new ContextMenuItemEntry(text)
                {
                    Items = entries
                }
            );
        }

        public void Show()
        {
            UIManager.ShowContextMenu(null);

            if (_items.Count == 0)
            {
                return;
            }

            UIManager.ShowContextMenu
            (
                new ContextMenuShowMenu(_gump.World, _items)
            );
        }

        public void Dispose()
        {
            UIManager.ShowContextMenu(null);
            _items.Clear();
        }
    }

    public class ContextMenuItemEntry
    {
        public ContextMenuItemEntry(string text, Action action = null, bool canBeSelected = false, bool defaultValue = false)
        {
            Text = text;
            Action = action;
            CanBeSelected = canBeSelected;
            IsSelected = defaultValue;
        }

        public readonly Action Action;
        public readonly bool CanBeSelected;
        public bool IsSelected;
        public List<ContextMenuItemEntry> Items = new List<ContextMenuItemEntry>();
        public readonly string Text;

        public void Add(ContextMenuItemEntry subEntry)
        {
            Items.Add(subEntry);
        }
    }


    public class ContextMenuShowMenu : Gump
    {
        private readonly AlphaBlendControl _background;
        private List<ContextMenuShowMenu> _subMenus;


        public ContextMenuShowMenu(World world, List<ContextMenuItemEntry> list) : base(world, 0, 0)
        {
            WantUpdateSize = true;
            ModalClickOutsideAreaClosesThisControl = true;
            IsModal = true;
            LayerOrder = UILayer.Over;

            CanMove = false;
            AcceptMouseInput = true;


            _background = new AlphaBlendControl(0.7f);
            Add(_background);

            int y = 0;

            for (int i = 0; i < list.Count; i++)
            {
                ContextMenuItem item = new ContextMenuItem(this, list[i]);

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

            X = Mouse.Position.X + 5;
            Y = Mouse.Position.Y - 20;

            if (X + _background.Width > Client.Game.Window.ClientBounds.Width)
            {
                X = Client.Game.Window.ClientBounds.Width - _background.Width;
            }

            if (Y + _background.Height > Client.Game.Window.ClientBounds.Height)
            {
                Y = Client.Game.Window.ClientBounds.Height - _background.Height;
            }

            if (Y < Client.Game.Window.ClientBounds.Y)
            {
                Y = 0;
            }

            foreach (ContextMenuItem mitem in FindControls<ContextMenuItem>())
            {
                if (mitem.Width < _background.Width)
                {
                    mitem.Width = _background.Width;
                }
            }
        }


        public override void Update()
        {
            base.Update();
            WantUpdateSize = true;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

            batcher.DrawRectangle
            (
                SolidColorTextureCache.GetTexture(Color.Gray),
                x - 1,
                y - 1,
                _background.Width + 1,
                _background.Height + 1,
                hueVector
            );

            return base.Draw(batcher, x, y);
        }

        public override bool Contains(int x, int y)
        {
            if (_background.Bounds.Contains(x, y))
            {
                return true;
            }

            if (_subMenus != null)
            {
                foreach (ContextMenuShowMenu menu in _subMenus)
                {
                    if (menu.Contains(x - menu.X, y - menu.Y))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private class ContextMenuItem : Control
        {
            private static readonly RenderedText _moreMenuLabel = RenderedText.Create(">", 0xFFFF, isunicode: true, style: FontStyle.BlackBorder);
            private readonly ContextMenuItemEntry _entry;
            private readonly Label _label;
            private readonly GumpPic _selectedPic;
            private readonly ContextMenuShowMenu _subMenu;
            private readonly ContextMenuShowMenu _gump;


            public ContextMenuItem(ContextMenuShowMenu parent, ContextMenuItemEntry entry)
            {
                _gump = parent;
                CanCloseWithRightClick = false;
                _entry = entry;

                _label = new Label
                (
                    entry.Text,
                    true,
                    0xFFFF,
                    0,
                    style: FontStyle.BlackBorder
                )
                {
                    X = 25
                };

                Add(_label);


                _selectedPic = new GumpPic(3, 0, 0x838, 0)
                {
                    IsVisible = entry.IsSelected,
                    IsEnabled = false
                };

                Add(_selectedPic);

                Height = 25;


                _label.Y = (Height >> 1) - (_label.Height >> 1);

                if (_selectedPic != null)
                {
                    //_label.X = _selectedPic.X + _selectedPic.Width + 6;
                    _selectedPic.Y = (Height >> 1) - (_selectedPic.Height >> 1);
                }

                Width = _label.X + _label.Width + 20;

                if (Width < 100)
                {
                    Width = 100;
                }

                // it is a bit tricky, but works :D 
                if (_entry.Items != null && _entry.Items.Count != 0)
                {
                    _subMenu = new ContextMenuShowMenu(_gump.World, _entry.Items);
                    parent.Add(_subMenu);

                    if (parent._subMenus == null)
                    {
                        parent._subMenus = new List<ContextMenuShowMenu>();
                    }

                    parent._subMenus.Add(_subMenu);
                }

                WantUpdateSize = false;
            }


            public override void Update()
            {
                base.Update();

                if (Width > _label.Width)
                {
                    _label.Width = Width;
                }

                if (_selectedPic != null)
                {
                    _selectedPic.IsVisible = _entry.IsSelected;
                }

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
                        Control p = UIManager.MouseOverControl?.Parent;

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
                    Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

                    batcher.Draw
                    (
                        SolidColorTextureCache.GetTexture(Color.Gray),
                        new Rectangle
                        (
                            x + 2,
                            y + 5,
                            Width - 4,
                            Height - 10
                        ),
                        hueVector
                    );
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