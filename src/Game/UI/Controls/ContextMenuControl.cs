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
        private readonly List<Tuple<string, Action>> _items;
        private ContextMenuShowMenu _menu;

        public ContextMenuControl()
        {
            CanMove = true;
            AcceptMouseInput = true;

            _items = new List<Tuple<string, Action>>();

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

        public void Add(string text, Action action)
        {
            _items.Add(Tuple.Create(text, action));
        }

        public override void Add(Control c, int page = 0)
        {
        }

        private class ContextMenuShowMenu : Control
        {
            public ContextMenuShowMenu(List<Tuple<string, Action>> list)
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
                    var t = list[i];
                    var item = new ContextMenuItem(t.Item1, t.Item2);
                    if (i > 0)
                    {
                        item.Y = y;
                    }

                    if (background.Width < item.Width)
                        background.Width = item.Width;

                    background.Height += item.Height;

                    Add(item);

                    y += item.Height;
                }

                Width = background.Width;
                Height = background.Height;
            }
        }
    }


    class ContextMenuItem : Control
    {
        private readonly Action _action;

        public ContextMenuItem(string text, Action callback)
        {
            _action = callback;
            var label = new HoveredLabel(text, true, 1150, 1150, style: FontStyle.BlackBorder)
            {
                X = 3,
                DrawBackgroundCurrentIndex = true
            };
            label.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButton.Left)
                {
                    _action?.Invoke();

                    Parent?.Dispose();
                }
            };
            Add(label);

            Width = label.Width = 100;
            Height = label.Height + 2;

            WantUpdateSize = false;
        }
    }
}
