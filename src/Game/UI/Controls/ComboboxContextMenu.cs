using ClassicUO.Input;
using System;
using System.Linq;

namespace ClassicUO.Game.UI.Controls
{
    internal class ComboboxContextMenu : Control
    {
        private readonly byte _font;

        public event EventHandler<int> OnItemSelected;

        public ComboboxContextMenu(string[] items, int minWidth, int maxHeight, byte font, int verticalSpacing = 15)
        {
            _font = font;
            ResizePic background;
            Add(background = new ResizePic(0x0BB8));
            HoveredLabel[] labels = new HoveredLabel[items.Length];
            int index = 0;

            for (int i = 0; i < items.Length; i++)
            {
                string item = items[i];

                if (item == null)
                {
                    item = string.Empty;
                }

                HoveredLabel label = new HoveredLabel(item, false, 0x0453, 0x0453, 0x0453, font: _font)
                {
                    X = 2,
                    Y = index * verticalSpacing,
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
        }

        private void Label_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtonType.Left)
            {
                OnItemSelected?.Invoke(this, (int)((Label)sender).Tag);
            }
        }
    }
}
