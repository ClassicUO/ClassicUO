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
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
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
        private static int GRID_ITEM_SIZE { get { return (int)Math.Round(50 * (ProfileManager.CurrentProfile.GridContainersScale / 100f)); } }
        private float _lastGridItemScale = (ProfileManager.CurrentProfile.GridContainersScale / 100f);
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
        private readonly GumpPic _openRegularGump, _quickDropBackpack, _sortContents;
        public readonly ushort OgContainerGraphic;

        private GridSlotManager gridSlotManager;

        private static GridSaveSystem gridSaveSystem = new GridSaveSystem();

        public GridContainer(uint local, ushort ogContainer) : base(DEFAULT_WIDTH, DEFAULT_HEIGHT, GetWidth(2), GRID_ITEM_SIZE + BORDER_WIDTH + 31, local, 0)
        {
            #region SET VARS
            Point savedSize = gridSaveSystem.GetLastSize(LocalSerial);
            _lastWidth = Width = savedSize.X;
            _lastHeight = Height = savedSize.Y;
            Point lastPos = gridSaveSystem.GetLastPosition(LocalSerial);
            _lastX = X = lastPos.X;
            _lastY = Y = lastPos.Y;
            AnchorType = ProfileManager.CurrentProfile.EnableGridContainerAnchor ? ANCHOR_TYPE.NONE : ANCHOR_TYPE.DISABLED;

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

            _searchBox = new StbTextBox(0xFF, 20, 150, true, FontStyle.None, 0x0481)
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
            _openRegularGump.SetTooltip("Open the original style container.");

            var quickDropIcon = GumpsLoader.Instance.GetGumpTexture(1625, out var bounds1);
            _quickDropBackpack = new GumpPic(Width - _openRegularGump.Width - 20 - BORDER_WIDTH, BORDER_WIDTH, quickDropIcon == null ? (ushort)1209 : (ushort)1625, 0);
            _quickDropBackpack.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left && _quickDropBackpack.MouseIsOver)
                {
                    if (Client.Game.GameCursor.ItemHold.Enabled)
                    {
                        GameActions.DropItem(Client.Game.GameCursor.ItemHold.Serial, 0xFFFF, 0xFFFF, 0, World.Player.FindItemByLayer(Layer.Backpack));
                        RequestUpdateContents();
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

            _sortContents = new GumpPic(_quickDropBackpack.X - 20, BORDER_WIDTH, 1210, 0);
            _sortContents.MouseUp += (sender, e) => { updateItems(true); };
            _sortContents.MouseEnter += (sender, e) => { _sortContents.Graphic = 1209; };
            _sortContents.MouseExit += (sender, e) => { _sortContents.Graphic = 1210; };
            _sortContents.SetTooltip("Sort this container.");
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
            _searchBox.Add(new AlphaBlendControl(0.5f)
            {
                Hue = 0x0481,
                Width = _searchBox.Width,
                Height = _searchBox.Height
            });
            Add(_searchBox);
            Add(_openRegularGump);
            Add(_quickDropBackpack);
            Add(_sortContents);
            Add(_scrollArea);
            #endregion

            gridSlotManager = new GridSlotManager(local, this, _scrollArea, gridSaveSystem.GetItemSlots(LocalSerial)); //Must come after scroll area

            ResizeWindow(new Point(_lastWidth, _lastHeight));
        }
        public override GumpType GumpType => GumpType.GridContainer;

        private static int GetWidth(int columns)
        {
            return (BORDER_WIDTH * 2)     //The borders around the container, one on the left and one on the right
            + 15                   //The width of the scroll bar
            + (GRID_ITEM_SIZE * columns) //How many items to fit in left to right
            + (X_SPACING * columns)      //Spacing between each grid item(x4 items)
            + 6;                   //Because the border acts weird
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);

            gridSaveSystem.SaveContainer(LocalSerial, gridSlotManager.ItemPositions, Width, Height, X, Y);

            writer.WriteAttributeString("ogContainer", OgContainerGraphic.ToString());
            writer.WriteAttributeString("width", Width.ToString());
            writer.WriteAttributeString("height", Height.ToString());
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);
            int rW = int.Parse(xml.GetAttribute("width"));
            int rH = int.Parse(xml.GetAttribute("height"));

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

        private void updateItems(bool overrideSort = false)
        {
            //Container doesn't exist or has no items
            if (_container == null)
            {
                InvalidateContents = false;
                return;
            }

            List<Item> sortedContents = ProfileManager.CurrentProfile.GridContainerSearchMode == 0 ? gridSlotManager.SearchResults(_searchBox.Text) : GridSlotManager.GetItemsInContainer(_container);
            gridSlotManager.RebuildContainer(sortedContents, _searchBox.Text, overrideSort);
            _containerNameLabel.Text = GetContainerName();
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

            if (gridSlotManager.ItemPositions.Count > 0 && !_container.IsCorpse)
                gridSaveSystem.SaveContainer(LocalSerial, gridSlotManager.ItemPositions, Width, Height, X, Y);

            base.Dispose();
        }

        public override void Update()
        {
            base.Update();

            if (IsDisposed)
                return;


            if (_container == null || _container.IsDestroyed)
            {
                Dispose();
                return;
            }

            if (_container.IsCorpse)
            {
                if (_container.Distance > 3)
                {
                    Dispose();
                    return;
                }
            }

            if ((_lastWidth != Width || _lastHeight != Height) || _lastGridItemScale != GRID_ITEM_SIZE)
            {
                _lastGridItemScale = GRID_ITEM_SIZE;
                _background.Width = Width - (BORDER_WIDTH * 2);
                _background.Height = Height - (BORDER_WIDTH * 2);
                _scrollArea.Width = _background.Width - BORDER_WIDTH;
                _scrollArea.Height = _background.Height - BORDER_WIDTH - (_containerNameLabel.Height + 1);
                _openRegularGump.X = Width - _openRegularGump.Width - BORDER_WIDTH;
                _quickDropBackpack.X = _openRegularGump.X - _quickDropBackpack.Width;
                _sortContents.X = _quickDropBackpack.X - _sortContents.Width;
                _lastHeight = Height;
                _lastWidth = Width;
                _searchBox.Width = Math.Min(Width - (BORDER_WIDTH * 2) - _openRegularGump.Width - _quickDropBackpack.Width - _sortContents.Width, 150);
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
            private readonly HitBox hit;
            private bool mousePressedWhenEntered = false;
            private readonly Item container;
            private Item _item;
            private readonly GridContainer gridContainer;
            public bool ItemGridLocked = false;
            private readonly int slot;
            private GridContainerPreview preview;
            Label count;
            AlphaBlendControl background;

            public bool Hightlight = false;
            public Item SlotItem { get { return _item; } set { _item = value; LocalSerial = value.Serial; } }

            public GridItem(uint serial, int size, Item _container, GridContainer gridContainer, int count)
            {
                #region VARS
                slot = count;
                container = _container;
                this.gridContainer = gridContainer;
                LocalSerial = serial;
                _item = World.Items.Get(serial);
                CanMove = true;
                #endregion

                background = new AlphaBlendControl(0.25f);
                background.Width = size;
                background.Height = size;
                Width = Height = size;
                Add(background);

                hit = new HitBox(0, 0, size, size, null, 0f);
                Add(hit);

                SetGridItem(_item);


                hit.MouseEnter += _hit_MouseEnter;
                hit.MouseExit += _hit_MouseExit;
                hit.MouseUp += _hit_MouseUp;
                hit.MouseDoubleClick += _hit_MouseDoubleClick;
            }



            public void Resize()
            {
                Width = GRID_ITEM_SIZE;
                Height = GRID_ITEM_SIZE;
                hit.Width = GRID_ITEM_SIZE;
                hit.Height = GRID_ITEM_SIZE;
                background.Width = GRID_ITEM_SIZE;
                background.Height = GRID_ITEM_SIZE;
            }

            public void SetGridItem(Item item)
            {
                if (item == null)
                {
                    _item = null;
                    LocalSerial = 0;
                    hit.ClearTooltip();
                    Hightlight = false;
                    count = null;
                }
                else
                {
                    _item = item;
                    LocalSerial = item.Serial;
                    int itemAmt = (_item.ItemData.IsStackable ? _item.Amount : 1);
                    if (itemAmt > 1)
                    {
                        count = new Label(itemAmt.ToString(), true, 0x0481, align: TEXT_ALIGN_TYPE.TS_LEFT, maxwidth: Width - 3);
                        count.X = 1;
                        count.Y = Height - count.Height;
                    }
                    hit.SetTooltip(_item);
                }
            }

            private void _hit_MouseDoubleClick(object sender, MouseDoubleClickEventArgs e)
            {
                if (e.Button != MouseButtonType.Left || TargetManager.IsTargeting || _item == null)
                {
                    return;
                }
                if (!Keyboard.Ctrl && ProfileManager.CurrentProfile.DoubleClickToLootInsideContainers && _item != null && !_item.IsDestroyed && !_item.ItemData.IsContainer && container != World.Player.FindItemByLayer(Layer.Backpack) && !_item.IsLocked && _item.IsLootable)
                {
                    GameActions.GrabItem(_item, _item.Amount);
                }
                else
                {
                    GameActions.DoubleClick(LocalSerial);
                }
                e.Result = true;
            }

            private void _hit_MouseUp(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtonType.Left)
                {
                    if (Client.Game.GameCursor.ItemHold.Enabled)
                    {
                        if (_item != null && _item.ItemData.IsContainer)
                        {
                            Rectangle containerBounds = ContainerManager.Get(_item.Graphic).Bounds;
                            GameActions.DropItem(Client.Game.GameCursor.ItemHold.Serial, containerBounds.X / 2, containerBounds.Y / 2, 0, _item.Serial);
                        }
                        else if (_item != null && _item.ItemData.IsStackable && _item.Graphic == Client.Game.GameCursor.ItemHold.Graphic)
                        {
                            GameActions.DropItem(Client.Game.GameCursor.ItemHold.Serial, _item.X, _item.Y, 0, _item.Serial);
                        }
                        else
                        {
                            Rectangle containerBounds = ContainerManager.Get(container.Graphic).Bounds;
                            gridContainer.gridSlotManager.AddLockedItemSlot(Client.Game.GameCursor.ItemHold.Serial, slot);
                            GameActions.DropItem(Client.Game.GameCursor.ItemHold.Serial, containerBounds.X / 2, containerBounds.Y / 2, 0, container.Serial);
                        }
                    }
                    else if (TargetManager.IsTargeting)
                    {
                        if (_item != null)
                            TargetManager.Target(_item);
                        else
                            TargetManager.Target(container);
                    }
                    else if (_item != null)
                    {
                        Point offset = Mouse.LDragOffset;
                        if (Math.Abs(offset.X) < Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS && Math.Abs(offset.Y) < Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS)
                        {
                            DelayedObjectClickManager.Set(_item.Serial, gridContainer.X, gridContainer.Y - 80, Time.Ticks + Mouse.MOUSE_DELAY_DOUBLE_CLICK);
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
                        if (_item != null)
                            GameActions.PickUp(_item, e.X, e.Y);
                        else
                            UIManager.AttemptDragControl(gridContainer);
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
                {
                    if (_item.ItemData.IsContainer && _item.Items != null && ProfileManager.CurrentProfile.GridEnableContPreview)
                    {
                        preview = new GridContainerPreview(_item, Mouse.Position.X, Mouse.Position.Y);
                        UIManager.Add(preview);
                    }
                    hit.SetTooltip(_item);
                    if (_item.ItemData.Layer > 0)
                    {
                        Item compItem = World.Player.FindItemByLayer((Layer)_item.ItemData.Layer);
                        if (compItem != null && _item.Serial != compItem.Serial)
                        {
                            if (World.OPL.TryGetNameAndData(compItem.Serial, out string name, out string data))
                                if (World.OPL.TryGetNameAndData(_item.Serial, out string name1, out string data1))
                                {
                                    string newToolTip = "<basefont color=\"yellow\">This Item<br>" +
                                        name1 + "<br><basefont color=\"#FFFFFFFF\">" +
                                        data1 +
                                        "<basefont color=\"orange\"><br><br>Equiped Item<br>" +
                                        name + "<basefont color=\"#FFFFFFFF\"><br>" + 
                                        data;
                                    hit.ClearTooltip();
                                    hit.SetTooltip(newToolTip);
                                }
                        }
                    }
                }
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                base.Draw(batcher, x, y);

                Item item = World.Items.Get(LocalSerial);

                Vector3 hueVector;

                hueVector = ShaderHueTranslator.GetHueVector(ProfileManager.CurrentProfile.GridBorderHue, false, (float)ProfileManager.CurrentProfile.GridBorderAlpha / 100);

                if (ItemGridLocked)
                    hueVector.X = 0x1;
                if (Hightlight)
                {
                    hueVector.X = 0x34;
                    hueVector.Z = 1;
                }

                batcher.DrawRectangle
                (
                    SolidColorTextureCache.GetTexture(Color.White),
                    x,
                    y,
                    Width,
                    Height,
                    hueVector
                );

                if (hit.MouseIsOver && _item != null)
                {
                    hueVector.Z = 0.3f;

                    batcher.Draw
                    (
                        SolidColorTextureCache.GetTexture(Color.White),
                        new Rectangle
                        (
                            x + 1,
                            y,
                            Width - 1,
                            Height
                        ),
                        hueVector
                    );
                }

                if (item != null)
                {
                    var texture = ArtLoader.Instance.GetStaticTexture(item.DisplayedGraphic, out var bounds);
                    var rect = ArtLoader.Instance.GetRealArtBounds(item.DisplayedGraphic);

                    hueVector = ShaderHueTranslator.GetHueVector(item.Hue, item.ItemData.IsPartialHue, 1f);

                    Point originalSize = new Point(hit.Width, hit.Height);
                    Point point = new Point();

                    if (rect.Width < hit.Width)
                    {
                        if (ProfileManager.CurrentProfile.GridContainerScaleItems)
                            originalSize.X = (ushort)(rect.Width * (ProfileManager.CurrentProfile.GridContainersScale / 100f));
                        else
                            originalSize.X = rect.Width;

                        point.X = (hit.Width >> 1) - (originalSize.X >> 1);
                    }

                    if (rect.Height < hit.Height)
                    {
                        if (ProfileManager.CurrentProfile.GridContainerScaleItems)
                            originalSize.Y = (ushort)(rect.Height * (ProfileManager.CurrentProfile.GridContainersScale / 100f));
                        else
                            originalSize.Y = rect.Height;

                        point.Y = (hit.Height >> 1) - (originalSize.Y >> 1);
                    }

                    batcher.Draw
                    (
                        texture,
                        new Rectangle
                        (
                            x + point.X,
                            y + point.Y + hit.Y,
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
                    if (count != null)
                        count.Draw(batcher, x + count.X, y + count.Y);
                }





                return true;
            }
        }

        private class GridSlotManager
        {
            private Dictionary<int, GridItem> gridSlots = new Dictionary<int, GridItem>();
            private Item container;
            private List<Item> containerContents;
            private int amount = 125;
            private Control area;
            private Dictionary<int, uint> itemPositions;

            public Dictionary<int, GridItem> GridSlots { get { return gridSlots; } }
            public List<Item> ContainerContents { get { return containerContents; } }
            public Dictionary<int, uint> ItemPositions { get { return itemPositions; } }

            public GridSlotManager(uint thisContainer, GridContainer gridContainer, Control controlArea, Dictionary<int, uint> lockedItems)
            {
                #region VARS
                area = controlArea;
                itemPositions = lockedItems;
                container = World.Items.Get(thisContainer);
                UpdateItems();
                if (containerContents.Count > 125)
                    amount = containerContents.Count;
                #endregion

                for (int i = 0; i < amount; i++)
                {
                    GridItem GI = new GridItem(0, GRID_ITEM_SIZE, container, gridContainer, i);
                    gridSlots.Add(i, GI);
                    area.Add(GI);
                }
            }

            public void AddLockedItemSlot(uint serial, int specificSlot)
            {
                if (ItemPositions.Values.Contains(serial)) //Is this item already locked? Lets remove it from lock status for now
                {
                    int removeSlot = ItemPositions.First((x) => x.Value == serial).Key;
                    ItemPositions.Remove(removeSlot);
                }

                if (ItemPositions.ContainsKey(specificSlot)) //Is the slot they wanted this item in already taken? Lets remove that item
                    ItemPositions.Remove(specificSlot);
                ItemPositions.Add(specificSlot, serial); //Now we add this item at the desired slot
            }

            public void RebuildContainer(List<Item> filteredItems, string searchText = "", bool overrideSort = false)
            {
                SetGridPositions();
                foreach (var slot in gridSlots)
                {
                    slot.Value.SetGridItem(null);
                }

                foreach (var spot in itemPositions)
                {
                    Item i = World.Items.Get(spot.Value);
                    if (i != null)
                        if (filteredItems.Contains(i) && !overrideSort)
                        {
                            if (spot.Key < gridSlots.Count)
                            {
                                gridSlots[spot.Key].SetGridItem(i);
                                filteredItems.Remove(i);
                            }
                        }
                }

                foreach (Item i in filteredItems)
                {
                    foreach (var slot in gridSlots)
                    {
                        if (slot.Value.SlotItem != null)
                            continue;
                        slot.Value.SetGridItem(i);
                        AddLockedItemSlot(i, slot.Key);
                        break;
                    }
                }

                foreach (var slot in gridSlots)
                {
                    if (slot.Value.SlotItem != null && ProfileManager.CurrentProfile.GridContainerSearchMode == 1 && searchText.Length > 0)
                    {
                        if (slot.Value.SlotItem.Name != null && slot.Value.SlotItem.Name.ToLower().Contains(searchText.ToLower()))
                        {
                            slot.Value.Hightlight = true;
                        }
                        else if (World.OPL.TryGetNameAndData(slot.Value.SlotItem.Serial, out string name, out string data))
                        {
                            if (data != null)
                                if (data.ToLower().Contains(searchText.ToLower()))
                                    slot.Value.Hightlight = true;
                        }
                    }
                }
            }

            private void SetGridPositions()
            {
                int x = X_SPACING, y = 0;
                foreach (var slot in gridSlots)
                {
                    if (x + GRID_ITEM_SIZE + X_SPACING >= area.Width - 14) //14 is the scroll bar width
                    {
                        x = X_SPACING;
                        y += GRID_ITEM_SIZE + Y_SPACING;
                    }
                    slot.Value.X = x + X_SPACING;
                    slot.Value.Y = y;
                    slot.Value.Resize();
                    x += GRID_ITEM_SIZE + X_SPACING;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="search"></param>
            /// <returns>List of items matching the search result, or all items if search is blank/profile does has hide search mode disabled</returns>
            public List<Item> SearchResults(string search)
            {
                UpdateItems(); //Why is this here? Because the server sends the container before it sends the data with it so sometimes we get empty containers without reloading the contents
                if (search != "")
                {
                    if (ProfileManager.CurrentProfile.GridContainerSearchMode == 0) //Hide search mode
                    {
                        List<Item> filteredContents = new List<Item>();
                        foreach (Item i in containerContents)
                        {
                            if (i == null)
                                continue;
                            if (i.Name == null)
                                continue;

                            if (i.Name.ToLower().Contains(search.ToLower()))
                            {
                                filteredContents.Add(i);
                                continue;
                            }
                            if (World.OPL.TryGetNameAndData(i.Serial, out string name, out string data))
                            {
                                if (data != null)
                                    if (data.ToLower().Contains(search.ToLower()))
                                        filteredContents.Add(i);
                            }
                        }
                        return filteredContents;
                    }
                }
                return containerContents;
            }

            public void UpdateItems()
            {
                containerContents = GetItemsInContainer(container);
            }

            public static List<Item> GetItemsInContainer(Item _container)
            {
                List<Item> contents = new List<Item>();
                for (LinkedObject i = _container.Items; i != null; i = i.Next)
                {
                    Item item = (Item)i;
                    var layer = (Layer)item.ItemData.Layer;

                    if (_container.IsCorpse && item.Layer > 0 && !Constants.BAD_CONTAINER_LAYERS[(int)layer])
                    {
                        continue;
                    }
                    if (item.ItemData.IsWearable && (layer == Layer.Face || layer == Layer.Beard || layer == Layer.Hair))
                    {
                        continue;
                    }

                    contents.Add(item);
                }
                return contents.OrderBy((x) => x.Graphic).ToList();
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

        private class GridSaveSystem
        {
            /// <summary>
            /// Time cutoff in seconds
            /// 60*60 = 1 hour
            ///      * 24 = 1 day
            ///          * 60 = ~2 month
            /// </summary>
            private const long TIME_CUTOFF = ((60 * 60) * 24) * 60;
            private string gridSavePath = Path.Combine(ProfileManager.ProfilePath, "GridContainers.xml");
            private XDocument saveDocument;
            private XElement rootElement;
            private bool enabled = false;

            public GridSaveSystem()
            {
                if (!SaveFileCheck())
                {
                    enabled = false;
                    return;
                }

                try
                {
                    saveDocument = XDocument.Load(gridSavePath);
                }
                catch
                {
                    saveDocument = new XDocument();
                }

                rootElement = saveDocument.Element("grid_gumps");
                if (rootElement == null)
                {
                    saveDocument.Add(new XElement("grid_gumps"));
                    rootElement = saveDocument.Root;
                }
                enabled = true;
            }

            public bool SaveContainer(uint serial, Dictionary<int, uint> lockedSpots, int width, int height, int lastX = 100, int lastY = 100)
            {
                if (!enabled)
                    return false;

                XElement thisContainer = rootElement.Element("container_" + serial.ToString());
                if (thisContainer == null)
                {
                    thisContainer = new XElement("container_" + serial.ToString());
                    rootElement.Add(thisContainer);
                }
                else
                    thisContainer.RemoveNodes();

                thisContainer.SetAttributeValue("last_opened", DateTimeOffset.Now.ToUnixTimeSeconds().ToString());
                thisContainer.SetAttributeValue("width", width.ToString());
                thisContainer.SetAttributeValue("height", height.ToString());
                thisContainer.SetAttributeValue("lastX", lastX.ToString());
                thisContainer.SetAttributeValue("lastY", lastY.ToString());

                foreach (var slot in lockedSpots)
                {
                    XElement item_slot = new XElement("item");
                    item_slot.SetAttributeValue("serial", slot.Value.ToString());
                    item_slot.SetAttributeValue("slot", slot.Key.ToString());
                    thisContainer.Add(item_slot);
                }
                RemoveOldContainers();

                saveDocument.Save(gridSavePath);

                return true;
            }

            public Dictionary<int, uint> GetItemSlots(uint container)
            {
                Dictionary<int, uint> itemSlots = new Dictionary<int, uint>();

                XElement thisContainer = rootElement.Element("container_" + container.ToString());
                if (thisContainer != null)
                {
                    foreach (XElement itemSlot in thisContainer.Elements("item"))
                    {
                        XAttribute slot, serial;
                        slot = itemSlot.Attribute("slot");
                        serial = itemSlot.Attribute("serial");
                        if (slot != null && serial != null)
                        {
                            int slotV;
                            uint serialV;

                            if (int.TryParse(slot.Value, out slotV))
                                if (uint.TryParse(serial.Value, out serialV))
                                    itemSlots.Add(slotV, serialV);
                        }
                    }
                }

                return itemSlots;
            }

            public Point GetLastSize(uint container)
            {
                Point lastSize = new Point(GridContainer.DEFAULT_WIDTH, GridContainer.DEFAULT_HEIGHT);

                XElement thisContainer = rootElement.Element("container_" + container.ToString());
                if (thisContainer != null)
                {
                    XAttribute width, height;
                    width = thisContainer.Attribute("width");
                    height = thisContainer.Attribute("height");
                    if (width != null && height != null)
                    {
                        int.TryParse(width.Value, out lastSize.X);
                        int.TryParse(height.Value, out lastSize.Y);
                    }
                }

                return lastSize;
            }

            public Point GetLastPosition(uint container)
            {
                Point LastPos = new Point(100, 100);

                XElement thisContainer = rootElement.Element("container_" + container.ToString());
                if (thisContainer != null)
                {
                    XAttribute lastX, lastY;
                    lastX = thisContainer.Attribute("lastX");
                    lastY = thisContainer.Attribute("lastY");
                    if (lastX != null && lastY != null)
                    {
                        int.TryParse(lastX.Value, out LastPos.X);
                        int.TryParse(lastY.Value, out LastPos.Y);
                    }
                }

                return LastPos;
            }

            private void RemoveOldContainers()
            {
                long cutOffTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - TIME_CUTOFF;
                List<XElement> removeMe = new List<XElement>();
                foreach (XElement container in rootElement.Elements())
                {
                    XAttribute lastOpened = container.Attribute("last_opened");
                    if (lastOpened != null)
                    {
                        long lo = cutOffTime;
                        long.TryParse(lastOpened.Value, out lo);

                        if (lo < cutOffTime)
                            removeMe.Add(container);
                    }
                }
                foreach (XElement container in removeMe)
                    container.Remove();
            }

            private bool SaveFileCheck()
            {
                try
                {
                    if (!File.Exists(gridSavePath))
                        File.Create(gridSavePath);
                }
                catch
                {
                    Console.WriteLine("Could not create file: " + gridSavePath);
                    return false;
                }
                return true;
            }
        }
    }
}