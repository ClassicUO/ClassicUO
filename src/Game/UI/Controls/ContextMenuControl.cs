using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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


        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (Parent != null)
            {
                Width = Parent.Width;
                Height = Parent.Height;
            }
        }

        protected override void OnMouseUp(int x, int y, MouseButton button)
        {
            if (button != MouseButton.Right)
            {
                base.OnMouseUp(x, y, button);
                return;
            }

            _menu?.Dispose();

            _menu = new ContextMenuShowMenu(_items)
            {
                X = Mouse.Position.X,
                Y = Mouse.Position.Y
            };
            Engine.UI.Add(_menu);
        }

        public void Add(string text, Action action, bool canBeSelected = false, bool defaultValue = false)
        {
            _items.Add(new ContextMenuItemEntry(text, action, canBeSelected, defaultValue));
        }

        public override void Add(Control c, int page = 0)
        {
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
                batcher.DrawRectangle(Textures.GetTexture(Color.Gray), x - 1, y - 1, Width + 1, Height + 1, ref _hueVector);
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
        private uint _timeHover;
        private readonly ContextMenuItemEntry _entry;


        public ContextMenuItem(ContextMenuItemEntry entry)
        {
            _entry = entry;

            _label = new Label(entry.Text, true, 1150, 0, style: FontStyle.BlackBorder)
            {
                X = 10,
            };
            Add(_label);

            if (entry.CanBeSelected)
            {
                _selectedPic = new GumpPic(0, 0, 0x838, 0);
                _selectedPic.Initialize();
                _selectedPic.IsVisible = entry.IsSelected;
                Add(_selectedPic);
            }

            Height = 25;


            _label.Y = (Height >> 1) - (_label.Height >> 1);

            if (_selectedPic != null)
            {
                _label.X = _selectedPic.X + _selectedPic.Width + 5;
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

        protected override void OnMouseUp(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left)
            {
                _entry.Action?.Invoke();

                Parent?.Dispose();

                if (_entry.CanBeSelected)
                {
                    _entry.IsSelected = !_entry.IsSelected;
                    _selectedPic.IsVisible = _entry.IsSelected;
                }
            }
            base.OnMouseUp(x, y, button);
        }


        protected override void OnMouseOver(int x, int y)
        {
            if (_timeHover < Engine.Ticks)
            {

            }

            base.OnMouseOver(x, y);
        }


        protected override void OnMouseEnter(int x, int y)
        {
            _timeHover = Engine.Ticks + 500;
            base.OnMouseEnter(x, y);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (!string.IsNullOrWhiteSpace(_label.Text) && MouseIsOver)
            {
                ResetHueVector();
                batcher.Draw2D(Textures.GetTexture(Color.Gray), x, y, Width, Height, ref _hueVector);
            }

            return base.Draw(batcher, x, y);
        }
    }
}
