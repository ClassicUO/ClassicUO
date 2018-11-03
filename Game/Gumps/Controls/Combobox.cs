using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Input;

namespace ClassicUO.Game.Gumps.Controls
{
    class Combobox: GumpControl
    {
        private Label _label;
        private string[] _items;
        private int _maxHeight;

        public event EventHandler<int> OnOptionSelected;
        public bool IsOpen { get; set; }
        public int SelectedIndex { get; private set; }

        public Combobox(int x, int y, int width, string[] items, int selected = -1, int maxHeight = 0, bool showArrow = true, string emptyString = "")
        {
            X = x;
            Y = y;
            Width = width;
            Height = 25;
            SelectedIndex = selected;

            _items = items;
            _maxHeight = maxHeight;

            AddChildren(new ResizePic(0x0BB8) { Width = width, Height = Height });

            string initialText = selected > -1 ? items[selected] : emptyString;
            AddChildren(_label = new Label(initialText, false, 0x0453, font: 9, align: IO.Resources.TEXT_ALIGN_TYPE.TS_LEFT) { X = 2, Y = 5 });

            if (showArrow)
                AddChildren(new GumpPic(width - 18, 2, 0x00FC, 0));
        }

        private void _contextMenu_OnOptionSelected(object sender, int e)
        {
            _label.Text = _items[e];
            SelectedIndex = e;

            UIManager.Remove<ComboboxContextMenu>();
            OnOptionSelected?.Invoke(this, e);
        }

        protected override void OnMouseClick(int x, int y, MouseButton button)
        {
            var contextMenu = new ComboboxContextMenu(_items, Width, _maxHeight) { X = ScreenCoordinateX, Y = ScreenCoordinateY };
            if (contextMenu.Height + ScreenCoordinateY > UIManager.Height)
            {
                contextMenu.Y -= contextMenu.Height + ScreenCoordinateY - UIManager.Height;
            }

            contextMenu.OnOptionSelected += _contextMenu_OnOptionSelected;
            UIManager.Add(contextMenu);

            base.OnMouseClick(x, y, button);
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);
        }

        private class ComboboxContextMenu: GumpControl
        {
            private ResizePic _background;

            public event EventHandler<int> OnOptionSelected;

            public ComboboxContextMenu(string[] items, int minWidth, int maxHeight)
            {
                AddChildren(_background = new ResizePic(0x0BB8));

                Label[] labels = new Label[items.Length];
                 var index = 0;
                foreach (var item in items)
                {
                    var label = new HoveredLabel(item, false, 0x0453, 0x024C, font: 9, align: IO.Resources.TEXT_ALIGN_TYPE.TS_LEFT)
                    {
                        X = 2,
                        Y = index * 15,
                        Tag = index
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
                        scrollArea.AddChildren(label);

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

            private void Label_MouseClick(object sender, Input.MouseEventArgs e)
            {
                OnOptionSelected?.Invoke(this, (int)((Label)sender).Tag);
            }

            public override void Update(double totalMS, double frameMS)
            {
                base.Update(totalMS, frameMS);
            }
        }
    }
}
