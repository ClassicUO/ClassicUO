#region license

// Copyright (c) 2021, andreakarasho
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class GridContainer : ResizableGump
    {
        private static int _lastX = 100, _lastY = 100;
        private readonly AlphaBlendControl _background;
        private readonly Item _container;
        private const int X_SPACING = 1, Y_SPACING = 1;
        private static int GRID_ITEM_SIZE = (int)Math.Round(50 * UIManager.ContainerScale);
        private float _lastGridItemScale = UIManager.ContainerScale;
        private const int BORDER_WIDTH = 4;
        private static int DEFAULT_WIDTH =
            (BORDER_WIDTH * 2)     //The borders around the container, one on the left and one on the right
            + 15                   //The width of the scroll bar
            + (GRID_ITEM_SIZE * 4) //How many items to fit in left to right
            + (X_SPACING * 4)      //Spacing between each grid item(x4 items)
            + 6;                   //Because the border acts weird
        private static int DEFAULT_HEIGHT = 27 + (BORDER_WIDTH * 2) + (GRID_ITEM_SIZE + Y_SPACING) * 4;
        private readonly Label _containerNameLabel;
        private GridScrollArea _scrollArea;
        private int _lastWidth = DEFAULT_WIDTH, _lastHeight = DEFAULT_HEIGHT;
        private readonly StbTextBox _searchBox;
        private readonly GumpPic _openRegularGump, _helpToolTip, _quickDropBackpack;
        public readonly ushort OgContainerGraphic;

        private Item _dragSlotItem, _dragSlotContainer;
        private bool _dragSlotEnabled = false;
        /// <summary>
        /// Grid position, Item serial
        /// </summary>
        private Dictionary<int, uint> lockedSpots = new Dictionary<int, uint>();

        public GridContainer(uint local, ushort ogContainer) : base(DEFAULT_WIDTH, DEFAULT_HEIGHT, DEFAULT_WIDTH, DEFAULT_HEIGHT, local, 0)
        {
            #region SET VARS
            OgContainerGraphic = ogContainer;
            _container = World.Items.Get(local);

            if (_container == null)
            {
                Dispose();
                return;
            }

            Mobile m = World.Mobiles.Get(_container.RootContainer);
            if (m != null)
            {
                if (m.NotorietyFlag == NotorietyFlag.Invulnerable && m.Serial != World.Player.Serial)
                {
                    OpenOldContainer(ogContainer);
                }
            }

            X = _lastX;
            Y = _lastY;

            CanMove = true;
            AcceptMouseInput = true;
            WantUpdateSize = true;
            CanCloseWithRightClick = true;
            #endregion

            #region background
            _background = new AlphaBlendControl();
            _background.Width = Width - (BORDER_WIDTH * 2);
            _background.Height = Height - (BORDER_WIDTH * 2);
            _background.X = BORDER_WIDTH;
            _background.Y = BORDER_WIDTH;
            _background.Alpha = (float)ProfileManager.CurrentProfile.ContainerOpacity / 100;
            _background.Hue = ProfileManager.CurrentProfile.AltGridContainerBackgroundHue;
            #endregion

            #region TOP BAR AREA
            _containerNameLabel = new Label(GetContainerName(), true, 0x0481)
            {
                X = BORDER_WIDTH,
                Y = -20
            };

            _searchBox = new StbTextBox(0xFF, 20, 100, true, FontStyle.Solid, 0x0481)
            {
                X = BORDER_WIDTH,
                Y = BORDER_WIDTH,
                Multiline = false,
                Width = 150,
                Height = 20
            };
            _searchBox.TextChanged += (sender, e) => { updateItems(); };
            _searchBox.DragBegin += (sender, e) => { InvokeDragBegin(e.Location); };

            var regularGumpIcon = GumpsLoader.Instance.GetGumpTexture(5839, out var bounds);
            _openRegularGump = new GumpPic(_background.Width - 25 - BORDER_WIDTH, BORDER_WIDTH, regularGumpIcon == null ? (ushort)1209 : (ushort)5839, 0);
            _openRegularGump.MouseUp += (sender, e) =>
            {
                OpenOldContainer(_container);
            };
            _openRegularGump.MouseEnter += (sender, e) => { _openRegularGump.Graphic = regularGumpIcon == null ? (ushort)1210 : (ushort)5840; };
            _openRegularGump.MouseExit += (sender, e) => { _openRegularGump.Graphic = regularGumpIcon == null ? (ushort)1209 : (ushort)5839; };
            _openRegularGump.SetTooltip("Open the original style container");

            var quickDropIcon = GumpsLoader.Instance.GetGumpTexture(1625, out var bounds1);
            _quickDropBackpack = new GumpPic(Width - _openRegularGump.Width - 20 - BORDER_WIDTH, BORDER_WIDTH, quickDropIcon == null ? (ushort)1209 : (ushort)1625, 0);
            _quickDropBackpack.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left && _quickDropBackpack.MouseIsOver)
                {
                    if (Client.Game.GameCursor.ItemHold.Enabled)
                    {
                        GameActions.DropItem(Client.Game.GameCursor.ItemHold.Serial, 0xFFFF, 0xFFFF, 0, World.Player.FindItemByLayer(Layer.Backpack));
                        InvalidateContents = true;
                        UpdateContents();
                    }
                }
                else if (e.Button == MouseButtonType.Right)
                {
                    InvokeMouseCloseGumpWithRClick();
                }
            };
            _quickDropBackpack.MouseEnter += (sender, e) =>
            {
                if (Client.Game.GameCursor.ItemHold.Enabled) _quickDropBackpack.Graphic = quickDropIcon == null ? (ushort)1210 : (ushort)1626;
            };
            _quickDropBackpack.MouseExit += (sender, e) => { _quickDropBackpack.Graphic = quickDropIcon == null ? (ushort)1209 : (ushort)1625; };
            _quickDropBackpack.SetTooltip("Drop an item here to send it to your backpack.");

            _helpToolTip = new GumpPic(_background.Width - _openRegularGump.Width - _quickDropBackpack.Width - 16 - BORDER_WIDTH, BORDER_WIDTH, 22153, 0);
            _helpToolTip.MouseEnter += (sender, e) => { _helpToolTip.Graphic = 22154; };
            _helpToolTip.MouseExit += (sender, e) => { _helpToolTip.Graphic = 22153; };
            _helpToolTip.SetTooltip(
                "Ctrl + Click a slot -> Click another slot to lock that item into a specific slot." +
                "<br>Use the corner button to open the original style gump."
                );

            #endregion

            #region Scroll Area
            _scrollArea = new GridScrollArea(
                _background.X,
                _containerNameLabel.Height + _background.Y + 1,
                _background.Width - BORDER_WIDTH,
                _background.Height - BORDER_WIDTH - (_containerNameLabel.Height + 1)
                );
            _scrollArea.AcceptMouseInput = true;
            _scrollArea.CanMove = true;
            _scrollArea.ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways;
            _scrollArea.MouseUp += _scrollArea_MouseUp;
            _scrollArea.DragBegin += _scrollArea_DragBegin;
            #endregion

            #region Add controls
            Add(_background);
            Add(_containerNameLabel);
            Add(new AlphaBlendControl(0.5f)
            {
                Hue = 0x0481,
                X = _searchBox.X,
                Y = _searchBox.Y,
                Width = _searchBox.Width,
                Height = _searchBox.Height
            }); //Search box background
            Add(_searchBox);
            Add(_helpToolTip);
            Add(_openRegularGump);
            Add(_quickDropBackpack);
            Add(_scrollArea);
            #endregion

            ResizeWindow(new Point(_lastWidth, _lastHeight));
            InvalidateContents = true;
        }
        public override GumpType GumpType => GumpType.GridContainer;

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);
            writer.WriteAttributeString("ogContainer", OgContainerGraphic.ToString());
            writer.WriteAttributeString("width", Width.ToString());
            writer.WriteAttributeString("height", Height.ToString());

            writer.WriteStartElement("lockedSlots");
            foreach (var slot in lockedSpots)
            {
                writer.WriteStartElement("lockedSlot");
                writer.WriteAttributeString("key", slot.Key.ToString());
                writer.WriteAttributeString("serial", slot.Value.ToString());
                writer.WriteEndElement();
            }
            writer.WriteEndElement();

        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);
            int rW = int.Parse(xml.GetAttribute("width"));
            int rH = int.Parse(xml.GetAttribute("height"));

            foreach (XmlElement ele in xml["lockedSlots"])
            {
                int key;
                if (int.TryParse(ele.GetAttribute("key"), out key))
                {
                    uint serial;
                    if (uint.TryParse(ele.GetAttribute("serial"), out serial))
                    {
                        lockedSpots.Add(key, serial);
                    }
                }
            }
            ResizeWindow(new Point(rW, rH));
            InvalidateContents = true;
        }

        private void _scrollArea_DragBegin(object sender, MouseEventArgs e)
        {
            InvokeDragBegin(e.Location);
        }

        private void _scrollArea_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtonType.Left && _scrollArea.MouseIsOver)
            {
                if (Client.Game.GameCursor.ItemHold.Enabled)
                {
                    GameActions.DropItem(Client.Game.GameCursor.ItemHold.Serial, 0xFFFF, 0xFFFF, 0, _container.Serial);
                    InvalidateContents = true;
                    UpdateContents();
                }
                else if (TargetManager.IsTargeting)
                {
                    TargetManager.Target(_container.Serial);
                }
            }
            else if (e.Button == MouseButtonType.Right)
            {
                InvokeMouseCloseGumpWithRClick();
            }
        }

        private ContainerGump GetOriginalContainerGump(uint serial)
        {
            ContainerGump container = UIManager.GetGump<ContainerGump>(serial);
            if (_container == null || _container.IsDestroyed) return null;

            ushort graphic = OgContainerGraphic;
            if (Client.Version >= Utility.ClientVersion.CV_706000 && ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.UseLargeContainerGumps)
            {
                GumpsLoader loader = GumpsLoader.Instance;

                switch (graphic)
                {
                    case 0x0048:
                        if (loader.GetGumpTexture(0x06E8, out _) != null)
                        {
                            graphic = 0x06E8;
                        }

                        break;

                    case 0x0049:
                        if (loader.GetGumpTexture(0x9CDF, out _) != null)
                        {
                            graphic = 0x9CDF;
                        }

                        break;

                    case 0x0051:
                        if (loader.GetGumpTexture(0x06E7, out _) != null)
                        {
                            graphic = 0x06E7;
                        }

                        break;

                    case 0x003E:
                        if (loader.GetGumpTexture(0x06E9, out _) != null)
                        {
                            graphic = 0x06E9;
                        }

                        break;

                    case 0x004D:
                        if (loader.GetGumpTexture(0x06EA, out _) != null)
                        {
                            graphic = 0x06EA;
                        }

                        break;

                    case 0x004E:
                        if (loader.GetGumpTexture(0x06E6, out _) != null)
                        {
                            graphic = 0x06E6;
                        }

                        break;

                    case 0x004F:
                        if (loader.GetGumpTexture(0x06E5, out _) != null)
                        {
                            graphic = 0x06E5;
                        }

                        break;

                    case 0x004A:
                        if (loader.GetGumpTexture(0x9CDD, out _) != null)
                        {
                            graphic = 0x9CDD;
                        }

                        break;

                    case 0x0044:
                        if (loader.GetGumpTexture(0x9CE3, out _) != null)
                        {
                            graphic = 0x9CE3;
                        }

                        break;
                }
            }

            if (container != null)
            {
                ContainerManager.CalculateContainerPosition(serial, graphic);
                container.X = ContainerManager.X;
                container.Y = ContainerManager.Y;
                container.InvalidateContents = true;
                return container;
            }
            else
            {
                ContainerManager.CalculateContainerPosition(serial, graphic);
                return new ContainerGump(_container.Serial, graphic, true)
                {
                    X = ContainerManager.X,
                    Y = ContainerManager.Y,
                    InvalidateContents = true
                };
            }
        }
        private void OpenOldContainer(uint serial)
        {
            ContainerGump container = GetOriginalContainerGump(serial);
            if (container == null)
                return;

            UIManager.Add(container);
        }

        private void updateItems()
        {
            #region VARS
            int x = X_SPACING;
            int y = Y_SPACING;
            int count = 0;
            int line = 1;
            #endregion

            //Remove previous items from view
            foreach (Control child in _scrollArea.Children)
                if (child is GridItem)
                    child.Dispose();

            //Container doesn't exist or has no items
            if (_container == null || _container.Items == null)
            {
                InvalidateContents = false;
                return;
            }

            #region Convert items into a sorted list
            List<Item> contents = new List<Item>();
            for (LinkedObject i = _container.Items; i != null; i = i.Next)
            {
                contents.Add((Item)i);
            }
            List<Item> sortedContents = contents.OrderBy((x) => x.Graphic).ToList();
            #endregion

            #region Filter contents via search box
            if (_searchBox.Text != "")
            {
                if (ProfileManager.CurrentProfile.GridContainerSearchMode == 0) //Hide search mode
                {
                    List<Item> filteredContents = new List<Item>();
                    foreach (Item i in sortedContents)
                    {
                        if (i == null)
                            continue;
                        if (i.Name == null)
                            continue;

                        if (i.Name.ToLower().Contains(_searchBox.Text.ToLower()))
                        {
                            filteredContents.Add(i);
                            continue;
                        }
                        if (World.OPL.TryGetNameAndData(i.Serial, out string name, out string data))
                        {
                            if (data != null)
                                if (data.ToLower().Contains(_searchBox.Text.ToLower()))
                                    filteredContents.Add(i);
                        }
                    }
                    sortedContents = filteredContents;
                }
            }
            #endregion

            #region Sort Locked Slots
            Dictionary<int, Item> addAfter = new Dictionary<int, Item>();
            int sortedCount = sortedContents.Count;
            foreach (var spot in lockedSpots.OrderBy((x) => x.Key))
            {
                Item item = World.Items.Get(spot.Value);
                if (item == null) //Locked item doesn't appear to exist in the client, ignoring it
                    continue;
                int index = sortedContents.IndexOf(item);
                if (index != -1) //The item exists
                {
                    if (spot.Key < sortedCount) //The items locked spot is less than the container count, can't lock an item in a spot that doesn't exist
                    {
                        sortedContents.RemoveAt(index);
                        addAfter.Add(spot.Key, item);
                    }
                    else //If the locked slot doesn't exist(ex: 5 items in the container, this item was locked at slot 7), add to the end of the list
                    {
                        sortedContents.RemoveAt(index);
                        sortedContents.Add(item);
                    }
                }
            }

            foreach (var item in addAfter)
            {
                sortedContents.Insert(item.Key, item.Value);
            }
            #endregion

            #region Add the grid items to the gump
            foreach (Item it in sortedContents)
            {
                GridItem gridItem = new GridItem(it, GRID_ITEM_SIZE, _container, this, count);

                if (lockedSpots.Values.Contains(it))
                    gridItem.ItemGridLocked = true;

                if (x + GRID_ITEM_SIZE + X_SPACING >= _scrollArea.Width - 14) //14 is the scroll bar width
                {
                    x = X_SPACING;
                    ++line;

                    y += gridItem.Height + Y_SPACING;
                }

                if (ProfileManager.CurrentProfile.GridContainerSearchMode == 1 && _searchBox.Text.Length > 0)
                {
                    if (it.Name != null && it.Name.ToLower().Contains(_searchBox.Text.ToLower()))
                    {
                        gridItem.Hightlight = true;
                    }
                    else if (World.OPL.TryGetNameAndData(it.Serial, out string name, out string data))
                    {
                        if (data != null)
                            if (data.ToLower().Contains(_searchBox.Text.ToLower()))
                                gridItem.Hightlight = true;
                    }
                }

                gridItem.X = x + X_SPACING;
                gridItem.Y = y;
                _scrollArea.Add(gridItem);

                x += gridItem.Width + X_SPACING;
                ++count;
            }
            #endregion

            InvalidateContents = false;
        }

        protected override void UpdateContents()
        {
            if (InvalidateContents && !IsDisposed && IsVisible)
            {
                _background.Alpha = (float)ProfileManager.CurrentProfile.ContainerOpacity / 100;
                updateItems();
            }
        }

        public override void Dispose()
        {
            _lastX = X;
            _lastY = Y;

            base.Dispose();
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (!IsVisible || IsDisposed)
            {
                return false;
            }

            base.Draw(batcher, x, y);
            return true;
        }

        public override void Update()
        {
            if (_container == null || _container.IsDestroyed || _container.OnGround && _container.Distance > 3)
            {
                Dispose();
                return;
            }

            if (IsDisposed)
            {
                return;
            }

            base.Update();

            if ((_lastWidth != Width || _lastHeight != Height) || _lastGridItemScale != UIManager.ContainerScale)
            {
                _lastGridItemScale = UIManager.ContainerScale;
                GRID_ITEM_SIZE = (int)Math.Round(50 * UIManager.ContainerScale);
                _background.Width = Width - (BORDER_WIDTH * 2);
                _background.Height = Height - (BORDER_WIDTH * 2);
                _scrollArea.Width = _background.Width - BORDER_WIDTH;
                _scrollArea.Height = _background.Height - BORDER_WIDTH - (_containerNameLabel.Height + 1);
                _openRegularGump.X = Width - _openRegularGump.Width - BORDER_WIDTH;
                _quickDropBackpack.X = Width - _openRegularGump.Width - _quickDropBackpack.Width - BORDER_WIDTH;
                _helpToolTip.X = Width - _helpToolTip.Width - _openRegularGump.Width - _quickDropBackpack.Width - BORDER_WIDTH;
                _lastHeight = Height;
                _lastWidth = Width;
                RequestUpdateContents();
            }

            if (_container != null && !_container.IsDestroyed && UIManager.MouseOverControl != null && (UIManager.MouseOverControl == this || UIManager.MouseOverControl.RootParent == this))
            {
                SelectedObject.Object = _container;
            }
        }

        private string GetContainerName()
        {
            return _container.Name?.Length > 0 ? _container.Name : "a container";
        }

        private class GridItem : Control
        {
            private readonly HitBox _hit;
            private bool mousePressedWhenEntered = false;
            private readonly Item _item, _container;
            private readonly GridContainer _gridContainer;
            public bool ItemGridLocked = false;
            private readonly int slot;
            private GridContainerPreview _preview;
            private GumpPic _lockIcon;
            public bool Hightlight = false;

            public GridItem(uint serial, int size, Item container, GridContainer gridContainer, int count)
            {
                #region VARS
                slot = count;
                _container = container;
                _gridContainer = gridContainer;
                LocalSerial = serial;
                _item = World.Items.Get(serial);
                CanMove = false;
                WantUpdateSize = false;
                #endregion

                if (_item == null)
                {
                    return;
                }

                AlphaBlendControl background = new AlphaBlendControl(0.25f);
                background.Width = size;
                background.Height = size;
                Width = Height = size;
                Add(background);

                int itemAmt = (_item.ItemData.IsStackable ? _item.Amount : 1);
                if (itemAmt > 1)
                {
                    Label _count = new Label(itemAmt.ToString(), true, 0x0481, align: TEXT_ALIGN_TYPE.TS_LEFT, maxwidth: size);
                    _count.X = 1;
                    _count.Y = size - _count.Height;

                    Add(_count);
                }

                _hit = new HitBox(0, 0, size, size, null, 0f);
                Add(_hit);

                _hit.SetTooltip(_item);

                _hit.MouseEnter += _hit_MouseEnter;
                _hit.MouseExit += _hit_MouseExit;
                _hit.MouseUp += _hit_MouseUp;
                _hit.MouseDoubleClick += _hit_MouseDoubleClick;

                _lockIcon = new GumpPic(Width - 10, 1, 0x082C, 0)
                {
                    AcceptMouseInput = true,
                };
                HitBox lockIconHit = new HitBox(0, 0, _lockIcon.Width, _lockIcon.Height, "Unlock this slot");
                lockIconHit.MouseUp += (o, e) =>
                {
                    if (_gridContainer.lockedSpots.Values.Contains(_item.Serial))
                    {
                        _gridContainer.lockedSpots.Remove(_gridContainer.lockedSpots.First((x) => x.Value == _item.Serial).Key);
                        _gridContainer.RequestUpdateContents();
                    }
                };
                _lockIcon.Add(lockIconHit);
                _lockIcon.IsVisible = ItemGridLocked;
                Add(_lockIcon);
            }

            private void _hit_MouseDoubleClick(object sender, MouseDoubleClickEventArgs e)
            {
                if (e.Button != MouseButtonType.Left || TargetManager.IsTargeting)
                {
                    return;
                }
                if (!Keyboard.Ctrl && ProfileManager.CurrentProfile.DoubleClickToLootInsideContainers && _item != null && !_item.IsDestroyed && !_item.ItemData.IsContainer && _container != World.Player.FindItemByLayer(Layer.Backpack) && !_item.IsLocked && _item.IsLootable)
                {
                    GameActions.GrabItem(_item, _item.Amount);
                }
                else
                {
                    GameActions.DoubleClick(LocalSerial);
                }
                e.Result = true;
            }

            private void AddLockedItemSlot(Item item, int specificSlot)
            {
                if (_gridContainer.lockedSpots.Values.Contains(item.Serial)) //Is this item already locked? Lets remove it from lock status for now
                {
                    int removeSlot = _gridContainer.lockedSpots.First((x) => x.Value == item).Key;
                    _gridContainer.lockedSpots.Remove(removeSlot);
                }

                if (_gridContainer.lockedSpots.ContainsKey(specificSlot)) //Is the slot they wanted this item in already taken? Lets remove that item
                    _gridContainer.lockedSpots.Remove(specificSlot);
                _gridContainer.lockedSpots.Add(specificSlot, item.Serial); //Now we add this item at the desired slot
                _gridContainer.RequestUpdateContents(); //Let the client know the contents have been changed so it can redraw them.
            }

            private void _hit_MouseUp(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtonType.Left)
                {
                    if (_gridContainer._dragSlotEnabled)
                    {
                        if (_gridContainer._dragSlotContainer == _container)
                        {
                            AddLockedItemSlot(_gridContainer._dragSlotItem, slot);
                            ItemGridLocked = true;
                            _gridContainer.InvalidateContents = true;
                        }
                        _gridContainer._dragSlotEnabled = false;
                    }
                    else if (Keyboard.Ctrl)
                    {
                        _gridContainer._dragSlotContainer = _container;
                        _gridContainer._dragSlotItem = _item;
                        _gridContainer._dragSlotEnabled = true;
                    }
                    else if (Client.Game.GameCursor.ItemHold.Enabled)
                    {
                        if (_item.ItemData.IsContainer)
                        {
                            Rectangle containerBounds = ContainerManager.Get(_item.Graphic).Bounds;
                            GameActions.DropItem(Client.Game.GameCursor.ItemHold.Serial, containerBounds.X / 2, containerBounds.Y / 2, 0, _item.Serial);
                        }
                        else if (_item.ItemData.IsStackable && _item.Graphic == Client.Game.GameCursor.ItemHold.Graphic)
                        {
                            GameActions.DropItem(Client.Game.GameCursor.ItemHold.Serial, _item.X, _item.Y, 0, _item.Serial);
                        }
                        else
                        {
                            Rectangle containerBounds = ContainerManager.Get(_container.Graphic).Bounds;
                            GameActions.DropItem(Client.Game.GameCursor.ItemHold.Serial, containerBounds.X / 2, containerBounds.Y / 2, 0, _container.Serial);
                        }
                        _gridContainer.InvalidateContents = true;
                        _gridContainer.UpdateContents();
                    }
                    else if (TargetManager.IsTargeting)
                    {
                        TargetManager.Target(_item);
                    }
                    else
                    {
                        Point offset = Mouse.LDragOffset;
                        if (Math.Abs(offset.X) < Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS && Math.Abs(offset.Y) < Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS)
                        {
                            DelayedObjectClickManager.Set(_item.Serial, _gridContainer.X, _gridContainer.Y - 80, Time.Ticks + Mouse.MOUSE_DELAY_DOUBLE_CLICK);
                        }
                    }
                }
            }

            private void _hit_MouseExit(object sender, MouseEventArgs e)
            {
                if (Mouse.LButtonPressed && !mousePressedWhenEntered)
                {
                    Point offset = Mouse.LDragOffset;
                    if (Math.Abs(offset.X) >= Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS || Math.Abs(offset.Y) >= Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS)
                    {
                        GameActions.PickUp(_item, e.X, e.Y);
                    }
                }

                GridContainerPreview g;
                while ((g = UIManager.GetGump<GridContainerPreview>()) != null)
                {
                    g.Dispose();
                }
            }

            private void _hit_MouseEnter(object sender, MouseEventArgs e)
            {
                if (Mouse.LButtonPressed)
                    mousePressedWhenEntered = true;
                else
                    mousePressedWhenEntered = false;
                if (_item != null)
                    if (_item.ItemData.IsContainer && _item.Items != null)
                    {
                        _preview = new GridContainerPreview(_item, Mouse.Position.X, Mouse.Position.Y);
                        UIManager.Add(_preview);
                    }
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                _lockIcon.IsVisible = ItemGridLocked;

                base.Draw(batcher, x, y);

                Item item = World.Items.Get(LocalSerial);

                Vector3 hueVector;

                if (item != null)
                {
                    var texture = ArtLoader.Instance.GetStaticTexture(item.DisplayedGraphic, out var bounds);
                    var rect = ArtLoader.Instance.GetRealArtBounds(item.DisplayedGraphic);

                    hueVector = ShaderHueTranslator.GetHueVector(item.Hue, item.ItemData.IsPartialHue, 1f);

                    Point originalSize = new Point(_hit.Width, _hit.Height);
                    Point point = new Point();

                    if (rect.Width < _hit.Width)
                    {
                        if (ProfileManager.CurrentProfile.ScaleItemsInsideContainers)
                            originalSize.X = (ushort)(rect.Width * UIManager.ContainerScale);
                        else
                            originalSize.X = rect.Width;

                        point.X = (_hit.Width >> 1) - (originalSize.X >> 1);
                    }

                    if (rect.Height < _hit.Height)
                    {
                        if (ProfileManager.CurrentProfile.ScaleItemsInsideContainers)
                            originalSize.Y = (ushort)(rect.Height * UIManager.ContainerScale);
                        else
                            originalSize.Y = rect.Height;

                        point.Y = (_hit.Height >> 1) - (originalSize.Y >> 1);
                    }

                    batcher.Draw
                    (
                        texture,
                        new Rectangle
                        (
                            x + point.X,
                            y + point.Y + _hit.Y,
                            originalSize.X,
                            originalSize.Y
                        ),
                        new Rectangle
                        (
                            bounds.X + rect.X,
                            bounds.Y + rect.Y,
                            rect.Width,
                            rect.Height
                        ),
                        hueVector
                    );
                }

                hueVector = ShaderHueTranslator.GetHueVector(0);

                Color borderColor = Color.Gray;
                if (ItemGridLocked)
                    borderColor = Color.Blue;
                if (Hightlight)
                    borderColor = Color.Yellow;

                batcher.DrawRectangle
                (
                    SolidColorTextureCache.GetTexture(borderColor),
                    x,
                    y,
                    Width,
                    Height,
                    hueVector
                );

                if (_hit.MouseIsOver)
                {
                    hueVector.Z = 0.3f;

                    batcher.Draw
                    (
                        SolidColorTextureCache.GetTexture(Color.Yellow),
                        new Rectangle
                        (
                            x + 1,
                            y,
                            Width - 1,
                            Height
                        ),
                        hueVector
                    );

                    hueVector.Z = 1;
                }

                return true;
            }
        }

        private class GridScrollArea : Control
        {
            private readonly ScrollBarBase _scrollBar;
            private int _lastWidth;
            private int _lastHeight;
            private int _lastScrollValue = 0;

            public GridScrollArea
            (
                int x,
                int y,
                int w,
                int h,
                int scroll_max_height = -1
            )
            {
                X = x;
                Y = y;
                Width = w;
                Height = h;
                _lastWidth = w;
                _lastHeight = h;

                _scrollBar = new ScrollBar(Width - 14, 0, Height);


                ScrollMaxHeight = scroll_max_height;

                _scrollBar.MinValue = 0;
                _scrollBar.MaxValue = scroll_max_height >= 0 ? scroll_max_height : Height;
                _scrollBar.Parent = this;

                AcceptMouseInput = true;
                WantUpdateSize = false;
                CanMove = true;
                ScrollbarBehaviour = ScrollbarBehaviour.ShowWhenDataExceedFromView;
            }


            public int ScrollMaxHeight { get; set; } = -1;
            public ScrollbarBehaviour ScrollbarBehaviour { get; set; }
            public int ScrollValue => _scrollBar.Value;
            public int ScrollMinValue => _scrollBar.MinValue;
            public int ScrollMaxValue => _scrollBar.MaxValue;


            public Rectangle ScissorRectangle;


            public override void Update()
            {
                base.Update();

                CalculateScrollBarMaxValue();

                if (Width != _lastWidth || Height != _lastHeight)
                {
                    _scrollBar.X = Width - 14;
                    _scrollBar.Height = Height;
                    _lastWidth = Width;
                    _lastHeight = Height;
                }

                if (ScrollbarBehaviour == ScrollbarBehaviour.ShowAlways)
                {
                    _scrollBar.IsVisible = true;
                }
                else if (ScrollbarBehaviour == ScrollbarBehaviour.ShowWhenDataExceedFromView)
                {
                    _scrollBar.IsVisible = _scrollBar.MaxValue > _scrollBar.MinValue;
                }
            }

            public void Scroll(bool isup)
            {
                if (isup)
                {
                    _scrollBar.Value -= _scrollBar.ScrollStep;
                    _lastScrollValue = _scrollBar.Value;
                }
                else
                {
                    _scrollBar.Value += _scrollBar.ScrollStep;
                    _lastScrollValue = _scrollBar.Value;
                }
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                _scrollBar.Draw(batcher, x + _scrollBar.X, y + _scrollBar.Y);

                if (batcher.ClipBegin(x + ScissorRectangle.X, y + ScissorRectangle.Y, Width - 14 + ScissorRectangle.Width, Height + ScissorRectangle.Height))
                {
                    for (int i = 1; i < Children.Count; i++)
                    {
                        Control child = Children[i];

                        if (!child.IsVisible)
                        {
                            continue;
                        }

                        int finalY = y + child.Y - _scrollBar.Value + ScissorRectangle.Y;

                        child.Draw(batcher, x + child.X, finalY);
                    }

                    batcher.ClipEnd();
                }

                return true;
            }


            protected override void OnMouseWheel(MouseEventType delta)
            {
                switch (delta)
                {
                    case MouseEventType.WheelScrollUp:
                        _scrollBar.Value -= _scrollBar.ScrollStep;

                        break;

                    case MouseEventType.WheelScrollDown:
                        _scrollBar.Value += _scrollBar.ScrollStep;

                        break;
                }
            }

            public override void Clear()
            {
                for (int i = 1; i < Children.Count; i++)
                {
                    Children[i].Dispose();
                }
            }

            private void CalculateScrollBarMaxValue()
            {
                _scrollBar.Height = ScrollMaxHeight >= 0 ? ScrollMaxHeight : Height;
                bool maxValue = _scrollBar.Value == _scrollBar.MaxValue && _scrollBar.MaxValue != 0;

                int startX = 0, startY = 0, endX = 0, endY = 0;

                for (int i = 1; i < Children.Count; i++)
                {
                    Control c = Children[i];

                    if (c.IsVisible && !c.IsDisposed)
                    {
                        if (c.X < startX)
                        {
                            startX = c.X;
                        }

                        if (c.Y < startY)
                        {
                            startY = c.Y;
                        }

                        if (c.Bounds.Right > endX)
                        {
                            endX = c.Bounds.Right;
                        }

                        if (c.Bounds.Bottom > endY)
                        {
                            endY = c.Bounds.Bottom;
                        }
                    }
                }

                int width = Math.Abs(startX) + Math.Abs(endX);
                int height = Math.Abs(startY) + Math.Abs(endY) - _scrollBar.Height;
                height = Math.Max(0, height - (-ScissorRectangle.Y + ScissorRectangle.Height));

                if (height > 0)
                {
                    _scrollBar.MaxValue = height;

                    if (maxValue)
                    {
                        _scrollBar.Value = _scrollBar.MaxValue;
                    }
                }
                else
                {
                    _scrollBar.Value = _scrollBar.MaxValue = 0;
                }

                _scrollBar.UpdateOffset(0, Offset.Y);

                for (int i = 1; i < Children.Count; i++)
                {
                    Children[i].UpdateOffset(0, -_scrollBar.Value + ScissorRectangle.Y);
                }
            }
        }

        private class GridContainerPreview : Gump
        {
            private readonly AlphaBlendControl _background;
            private readonly Item _container;

            private const int WIDTH = 170;
            private const int HEIGHT = 150;
            private const int GRIDSIZE = 50;

            public GridContainerPreview(uint serial, int x, int y) : base(serial, 0)
            {
                _container = World.Items.Get(serial);
                if (_container == null)
                {
                    Dispose();
                    return;
                }

                X = x - WIDTH - 20;
                Y = y - HEIGHT - 20;
                _background = new AlphaBlendControl();
                _background.Width = WIDTH;
                _background.Height = HEIGHT;

                CanCloseWithRightClick = true;
                Add(_background);
                InvalidateContents = true;
            }

            protected override void UpdateContents()
            {
                base.UpdateContents();
                if (InvalidateContents && !IsDisposed && IsVisible)
                {
                    if (_container != null && _container.Items != null)
                    {
                        int currentCount = 0, lastX = 0, lastY = 0;
                        for (LinkedObject i = _container.Items; i != null; i = i.Next)
                        {

                            Item item = (Item)i;
                            if (item == null)
                                continue;

                            if (currentCount > 8)
                                break;

                            StaticPic gridItem = new StaticPic(item.DisplayedGraphic, item.Hue);
                            gridItem.X = lastX;
                            if (gridItem.X + GRIDSIZE > WIDTH)
                            {
                                gridItem.X = 0;
                                lastX = 0;
                                lastY += GRIDSIZE;

                            }
                            lastX += GRIDSIZE;
                            gridItem.Y = lastY;
                            //gridItem.Width = GRIDSIZE;
                            //gridItem.Height = GRIDSIZE;
                            Add(gridItem);

                            currentCount++;


                        }
                    }
                }
            }

            public override void Update()
            {
                if (_container == null || _container.IsDestroyed || _container.OnGround && _container.Distance > 3)
                {
                    Dispose();

                    return;
                }

                base.Update();

                if (IsDisposed)
                {
                    return;
                }

                base.Update();
            }
        }
    }
}