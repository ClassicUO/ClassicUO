// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Resources;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace ClassicUO.Game.UI.Gumps
{
    internal partial class CounterBarGump : ResizableGump
    {
        private const int MIN_SIZE = 30;
        private const int MAX_SIZE = 80;
        private static readonly int BORDER_LEFT = 2;
        private static readonly int BORDER_RIGHT = 2;
        private static readonly int BORDER_TOP = 2;
        private static readonly int BORDER_BOTTOM = 2;

        private static readonly ushort HELP_TEXT_HUE = 0x32; // light yellow

        private AlphaBlendControl _background;
        private Label _helpTextLabel;

        private DataBox _dataBox;
        private int _rectSize;
        private ScissorControl _scissor;

        public bool ReadOnly
        {
            get => !ShowBorder;
            set => ShowBorder = !value || _helpTextLabel != null;
            // It makes no sense to put the bar into read-only mode
            // while the text is shown to drop an item there to get started
        }

        public CounterBarGump(World world) : base(world, 0, 0, 50, 50, 0, 0, 0)
        {
            ContextMenu = ConfigureContextMenu(new ContextMenuControl(this));

            ResizeCompleted += CounterBarGump_ResizeCompleted;
        }

        private void CounterBarGump_ResizeCompleted(object sender, ResizeCompletedEventArgs e)
        {
            SnapToGrid();
        }

        private void SnapToGrid()
        {
            int desiredWidth = Width;
            int desiredHeight = Height;

            if (_helpTextLabel != null)
            {
                desiredWidth = Math.Max(desiredWidth, 6 * _rectSize + BORDER_LEFT + BORDER_RIGHT); // wide enough to show the help Text
            }

            int tooWide = (desiredWidth - BoderSize * 2) % _rectSize;
            int tooHigh = (desiredHeight - BoderSize * 2) % _rectSize;

            if (tooWide > 0 || tooHigh > 0)
            {
                ResizeWindow(new Point(desiredWidth - tooWide, desiredHeight - tooHigh));
            }
        }

        private ContextMenuControl ConfigureContextMenu(ContextMenuControl control)
        {
            control.Add(ResGumps.Add, AddPlaceholder);
            if (ReadOnly)
            {
                control.Add(ResGumps.CounterReadonlyOn, ToggleReadOnly);
            }
            else
            {
                control.Add(ResGumps.CounterReadonlyOff, ToggleReadOnly);
            }

            return control;
        }

        private void ToggleReadOnly()
        {
            ReadOnly = !ReadOnly;

            ContextMenu = ConfigureContextMenu(new ContextMenuControl(this));

            _dataBox.Children.ForEach(child => { (child as CounterItem)?.ConfigureContextMenu(); });
        }

        public CounterBarGump(
            World world,
            int x,
            int y,
            int rectSize = MIN_SIZE
        ) : this(world)
        {
            X = x;
            Y = y;

            SetCellSize(rectSize);

            BuildGump();
        }

        private void AddPlaceholder()
        {
            _dataBox.Add(new CounterItem(this, 0, 0, 0));
            SetupLayout();
        }

        public void SetCellSize(int size)
        {
            if (size != _rectSize)
            {
                if (size < MIN_SIZE)
                {
                    size = MIN_SIZE;
                }
                else if (size > MAX_SIZE)
                {
                    size = MAX_SIZE;
                }
                _rectSize = size;

                MinH = size + BoderSize * 2;
                MinW = size + BoderSize * 2;

                SnapToGrid();
                SetupLayout();
            }
        }

        public override GumpType GumpType => GumpType.CounterBar;

        private void BuildGump()
        {
            CanMove = true;
            AcceptMouseInput = true;
            AcceptKeyboardInput = false;
            CanCloseWithRightClick = false;
            WantUpdateSize = false;
            int borderSize = BoderSize;

            Add(_background = new AlphaBlendControl(0.7f) { X = borderSize, Y = borderSize, Width = Width - borderSize * 2, Height = Height - borderSize * 2 });

            Add(_scissor = new ScissorControl(true, borderSize, borderSize, 0, 0));
            _dataBox = new DataBox(borderSize, borderSize, 0, 0);
            Add(_dataBox);
            Add(new ScissorControl(false));
            _dataBox.WantUpdateSize = true;

            ResizeWindow(new Point(Width, Height));
            SnapToGrid();
            OnResize();
        }

        protected override void OnDragBegin(int x, int y)
        {
            if (UIManager.MouseOverControl?.RootParent == this)
            {
                UIManager.MouseOverControl.InvokeDragBegin(new Point(x, y));
            }

            base.OnDragBegin(x, y);
        }

        protected override void OnDragEnd(int x, int y)
        {
            if (UIManager.MouseOverControl?.RootParent == this)
            {
                UIManager.MouseOverControl.InvokeDragEnd(new Point(x, y));
            }

            base.OnDragEnd(x, y);
        }

        public override void OnResize()
        {
            base.OnResize();

            if (_background != null)
            {
                SetupLayout();
            }
        }

        private void SetupLayout()
        {
            if (_background == null)
            {
                return;
            }

            int width = Width - this.BoderSize * 2;
            int height = Height - this.BoderSize * 2;
            _background.Width = width;
            _background.Height = height;
            _dataBox.Width = 0;
            _dataBox.Height = 0;
            _dataBox.WantUpdateSize = true;

            _scissor.Width = width;
            _scissor.Height = height;

            int x = 0;
            int y = 0;

            if (_dataBox?.Children.Count == 0 && _helpTextLabel == null)
            {
                Add(_helpTextLabel = new Label(ResGumps.CounterEmptyHelpText, true, HELP_TEXT_HUE)
                {
                    X = BORDER_LEFT * 4,
                    Y = BORDER_TOP * 4,
                    Width = width - (BORDER_LEFT - BORDER_RIGHT) * 4,
                    Height = height - (BORDER_TOP - BORDER_BOTTOM) * 4
                });

                SnapToGrid();
            }

            if (_dataBox?.Children.Count > 0 && _helpTextLabel != null)
            {
                _helpTextLabel.Dispose();
                _helpTextLabel = null;
            }

            for (int i = 0; i < _dataBox.Children.Count; i++)
            {
                CounterItem c = _dataBox.Children[i] as CounterItem;
                if (c != null && !c.IsDisposed)
                {
                    c.X = x + BORDER_LEFT;
                    c.Y = y + BORDER_TOP;
                    c.Width = _rectSize - BORDER_LEFT - BORDER_RIGHT;
                    c.Height = _rectSize - BORDER_TOP - BORDER_BOTTOM;

                    x += _rectSize;

                    if (x + _rectSize > width)
                    {
                        x = 0;
                        y += _rectSize;
                    }

                    continue;
                }
            }
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                ToggleReadOnly();
                return true;
            }

            return false;
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                if (Client.Game.UO.GameCursor.ItemHold.Enabled && Client.Game.UO.GameCursor.ItemHold.Graphic != 0)
                {
                    if (!ReadOnly)
                    {
                        CounterItem item = new CounterItem(this, Client.Game.UO.GameCursor.ItemHold.Graphic, Client.Game.UO.GameCursor.ItemHold.Hue, 0);
                        _dataBox.Add(item);
                    }
                    GameActions.DropItem(Client.Game.UO.GameCursor.ItemHold.Serial, Client.Game.UO.GameCursor.ItemHold.X, Client.Game.UO.GameCursor.ItemHold.Y, 0, Client.Game.UO.GameCursor.ItemHold.Container);

                    SetupLayout();

                    return;
                }
            }

            base.OnMouseUp(x, y, button);
        }

        internal void UseSlot(string slotString)
        {
            if (!string.IsNullOrEmpty(slotString) && ushort.TryParse(slotString, out ushort slot))
            {
                // slot index is 1-based since we have to assume the average user is non-technical
                // everything else would be confusing

                if (_dataBox.Children.Skip(slot - 1).FirstOrDefault() is CounterItem item)
                {
                    item.Use();
                }
                else
                {
                    GameActions.Print(World, string.Format(ResGumps.CounterErrorSlotNotFound, slotString));
                }
            }
            else
            {
                GameActions.Print(World, string.Format(ResGumps.CounterErrorSlotNotValid, slotString));
            }
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);

            writer.WriteAttributeString("rectsize", _rectSize.ToString());
            writer.WriteAttributeString("width", Width.ToString());
            writer.WriteAttributeString("height", Height.ToString());
            writer.WriteAttributeString("readonly", ReadOnly.ToString());

            IEnumerable<CounterItem> controls = FindControls<CounterItem>();

            writer.WriteStartElement("controls");

            foreach (CounterItem control in _dataBox.Children.FindAll(c => c is CounterItem).Cast<CounterItem>())
            {
                writer.WriteStartElement("control");
                writer.WriteAttributeString("graphic", control.Graphic.ToString());
                if (control.Hue != null)
                {
                    writer.WriteAttributeString("hue", control.Hue.Value.ToString());
                }
                writer.WriteAttributeString("compareto", control.CompareTo.ToString());
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            SetCellSize(int.Parse(xml.GetAttribute("rectsize")));


            if (!int.TryParse(xml.GetAttribute("width"), out int width))
            {
                if (int.TryParse(xml.GetAttribute("columns"), out int columns)) //legacy
                {
                    width = columns * _rectSize + BoderSize * 2; // outer border
                }
                else
                {
                    width = 200;
                }
            }

            if (!int.TryParse(xml.GetAttribute("height"), out int height))
            {
                if (int.TryParse(xml.GetAttribute("rows"), out int rows)) //legacy
                {
                    height = rows * _rectSize + BoderSize * 2; // outer border
                }
                else
                {
                    height = MAX_SIZE;
                }
            }

            if (bool.TryParse(xml.GetAttribute("readonly"), out bool isReadOnly))
            {
                ReadOnly = isReadOnly;
            }

            BuildGump();

            XmlElement controlsXml = xml["controls"];

            if (controlsXml != null)
            {
                foreach (XmlElement controlXml in controlsXml.GetElementsByTagName("control"))
                {
                    ushort graphic = ushort.Parse(controlXml.GetAttribute("graphic"));

                    if (graphic == 0)
                    {
                        _dataBox.Add(new CounterItem(this, 0, 0, 0));
                        continue;
                    }
                    if (!int.TryParse(controlXml.GetAttribute("compareto"), out int compareTo))
                    {
                        compareTo = 0;
                    }

                    string hue = controlXml.GetAttribute("hue");

                    CounterItem c = new(this, graphic, string.IsNullOrEmpty(hue) ? null : ushort.Parse(controlXml.GetAttribute("hue")), compareTo);

                    _dataBox.Add(c);
                }
            }

            IsEnabled = IsVisible = ProfileManager.CurrentProfile.CounterBarEnabled;

            // resize only after items have been added
            // because an empty counter bar will always receive a minimum width for the help text
            ResizeWindow(new Point(width, height));

            SetupLayout();
        }
    }
}
