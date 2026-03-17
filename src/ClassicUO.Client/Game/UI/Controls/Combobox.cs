// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using System;
using System.Linq;

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
            GameContext context,
            int x,
            int y,
            int width,
            string[] items,
            int selected = -1,
            int maxHeight = 200,
            bool showArrow = true,
            string emptyString = "",
            byte font = 9
        ) : base(context)
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
                new ResizePic(0x0BB8, context)
                {
                    Width = width, Height = Height
                }
            );

            string initialText = selected > -1 ? items[selected] : emptyString;

            bool isAsianLang = string.Compare(Context?.Settings?.Language, "CHT", StringComparison.InvariantCultureIgnoreCase) == 0 ||
                string.Compare(Context?.Settings?.Language, "KOR", StringComparison.InvariantCultureIgnoreCase) == 0 ||
                string.Compare(Context?.Settings?.Language, "JPN", StringComparison.InvariantCultureIgnoreCase) == 0;

            bool unicode = isAsianLang;
            byte font1 = (byte)(isAsianLang ? 1 : _font);

            Add
            (
                _label = new Label(context, initialText, unicode, 0x0453, font: font1)
                {
                    X = 2, Y = 5
                }
            );

            if (showArrow)
            {
                Add(new GumpPic(width - 18, 2, 0x00FC, 0, context));
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


        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            float layerDepth = layerDepthRef;
            renderLists.AddGumpNoAtlas
            (
                (batcher) =>
                {
                    // work-around to allow clipping children
                    RenderLists comboBoxRenderLists = new();
                    base.AddToRenderLists(comboBoxRenderLists, x, y, ref layerDepth);

                    if (batcher.ClipBegin(x, y, Width, Height))
                    {
                        comboBoxRenderLists.DrawRenderLists(batcher, sbyte.MaxValue);
                        batcher.ClipEnd();
                    }
                    return true;
                }
            );



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
            else if (comboY + _maxHeight > Context.Game.ClientBounds.Height)
            {
                comboY = Context.Game.ClientBounds.Height - _maxHeight;
            }

            Context.UI.Add
            (
                new ComboboxGump
                (
                    // might crash
                    (RootParent as Gump).World,
                    ScreenCoordinateX,
                    comboY,
                    Width,
                    _maxHeight,
                    _items,
                    _font,
                    this
                )
            );

            base.OnMouseUp(x, y, button);
        }

        private class ComboboxGump : Gump
        {
            private const int ELEMENT_HEIGHT = 15;


            private readonly Combobox _combobox;

            public ComboboxGump
            (
                World world,
                int x,
                int y,
                int width,
                int maxHeight,
                string[] items,
                byte font,
                Combobox combobox
            ) : base(world, 0, 0)
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
                Add(background = new ResizePic(0x0BB8, World.Context));
                background.AcceptMouseInput = false;

                HoveredLabel[] labels = new HoveredLabel[items.Length];

                bool isAsianLang = string.Compare(World.Settings.Language, "CHT", StringComparison.InvariantCultureIgnoreCase) == 0 ||
                    string.Compare(World.Settings.Language, "KOR", StringComparison.InvariantCultureIgnoreCase) == 0 ||
                    string.Compare(World.Settings.Language, "JPN", StringComparison.InvariantCultureIgnoreCase) == 0;

                bool unicode = isAsianLang;
                byte font1 = (byte)(isAsianLang ? 1 : font);

                for (int i = 0; i < items.Length; i++)
                {
                    string item = items[i];

                    if (item == null)
                    {
                        item = string.Empty;
                    }

                    HoveredLabel label = new HoveredLabel
                    (
                        World.Context,
                        item,
                        unicode,
                        0x0453,
                        0x0453,
                        0x0453,
                        font: font1
                    )
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

                ScrollArea area = new ScrollArea
                (
                    World.Context,
                    0,
                    0,
                    maxWidth + 15,
                    totalHeight,
                    true
                );

                foreach (HoveredLabel label in labels)
                {
                    label.Width = maxWidth;
                    area.Add(label);
                }

                Add(area);

                background.Width = maxWidth;
                background.Height = totalHeight;
            }


            public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
            {
                float layerDepth = layerDepthRef + 100f; // Combo list should be over other gumps
                renderLists.AddGumpNoAtlas
                (
                    (batcher) =>
                    {
                        // work-around to allow clipping children
                        RenderLists comboBoxRenderLists = new();
                        base.AddToRenderLists(comboBoxRenderLists, x, y, ref layerDepth);

                        if (batcher.ClipBegin(x, y, Width, Height))
                        {
                            comboBoxRenderLists.DrawRenderLists(batcher, sbyte.MaxValue);
                            batcher.ClipEnd();
                        }
                        return true;
                    }
                );

                return true;
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
